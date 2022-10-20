/*
Bf3Rcon.NET, provides a .NET implementation of BF3's RCON interface.
Copyright (C) 2011 agentwite, Timi, Unseen, AlCapwn

This program is free software; you can redistribute it and/or
modify it under the terms of the GNU General Public License
as published by the Free Software Foundation; either version 2
of the License, or (at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program; if not, write to the Free Software
Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.

You can contact us at http://bf3rcon.codeplex.com/.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;

namespace System.Net.Battlefield3
{
    /// <summary>
    /// Represents an RCON protocol packet.
    /// </summary>
    public class Packet //going with a class over struct because the size could get large with all of its data
        : MarshalByRefObject
    {
        //TODO: add a limit to the byte size of a packet, depending on what the protocol docs say.

        /// <summary>
        /// Gets or sets the <see cref="Header"/> of the <see cref="Packet"/>.
        /// </summary>
        /// <value>The <see cref="Header"/> of the <see cref="Packet"/>.</value>
        public Header Header { get; set; }

        /// <summary>
        /// Gets or sets whether or not the <see cref="Packet"/> was sent by the server.
        /// </summary>
        /// <value>True if the <see cref="Packet"/> was sent by the server.</value>
        public bool IsFromServer
        {
            get { return Header.IsFromServer; }
            set
            {
                Header h = Header;
                h.IsFromServer = value;
            }
        }

        /// <summary>
        /// Gets or sets whether or not the <see cref="Packet"/> is responding to another <see cref="Packet"/>.
        /// </summary>
        /// <value>True if the <see cref="Packet"/> is responding to another <see cref="Packet"/>.</value>
        public bool IsResponse
        {
            get { return Header.IsResponse; }
            set
            {
                Header h = Header;
                h.IsResponse = value;
            }
        }

        /// <summary>
        /// Gets or sets the number uniquely identifying the <see cref="Packet"/> or set of <see cref="Packet">Packets</see>.
        /// </summary>
        /// <value>An integer uniquely identifying the <see cref="Packet"/>.</value>
        /// <remarks>
        /// If <see cref="IsResponse"/> is true, the <see cref="Sequence"/> will match that of an original <see cref="Packet"/>.<para />
        /// For instance, when sending a command to get player data, the server will respond with the same <see cref="Sequence"/>.
        /// </remarks>
        public int Sequence
        {
            get { return Header.Sequence; }
            set
            {
                Header h = Header;
                h.Sequence = value;
            }
        }

        internal List<string> WordList;
        ReadOnlyCollection<string> _Words; // this will wrap the list above when set in ctors
        /// <summary>
        /// Gets or sets the word list of the <see cref="Packet"/>.
        /// </summary>
        /// <value>The words of the <see cref="Packet"/>.</value>
        public ReadOnlyCollection<string> Words { get { return _Words; } }

        /// <summary>
        /// Converts the <see cref="Packet"/> into its encoded form.
        /// </summary>
        /// <returns>A byte array containing the <see cref="Packet"/>.</returns>
        public byte[] ToByteArray()
        {
            return EncodePacket(this).ToArray();
        }

        /// <summary>
        /// Gets the number of bytes of the <see cref="Packet"/>.
        /// </summary>
        /// <returns>An integer value indicating the number of bytes in the <see cref="Packet"/>.</returns>
        public int NumberOfBytes
        {
            get
            {
                int numberOfBytes = 12; //for the first three ints in a packet
                foreach (var word in Words)
                {
                    numberOfBytes += word.Length;
                    numberOfBytes += 5; // 4 for the int determining length and 1 for a null byte at the end
                }

                return numberOfBytes;
            }
        }

        #region Ctors
        /// <summary>
        /// Creates a blank instance of <see cref="Packet"/>.
        /// </summary>
        public Packet()
        {
            Header = new Header();
            WordList = new List<string>();
            _Words = new ReadOnlyCollection<string>(WordList);
        }

        /// <summary>
        /// Creates an instance of <see cref="Packet"/>.
        /// </summary>
        /// <param name="data">A raw <see cref="Packet"/> that is to be loaded.</param>
        /// <param name="leftoverData">The data the packet didn't use.</param>
        /// <remarks>Enough room is kept in <paramref name="leftoverData" /> for another read by RconClient.</remarks>
        internal Packet(IEnumerable<byte> data, List<byte> leftoverData)
        {
            List<byte> dataList = (data as List<byte>) ?? new List<byte>(data);

            Packet packet = DecodePacket(dataList, leftoverData);

            Header = packet.Header;
            WordList = new List<string>(packet.Words);
            _Words = new ReadOnlyCollection<string>(WordList);
        }

        /// <summary>
        /// Creates an instance of <see cref="Packet"/>.
        /// </summary>
        /// <param name="isFromServer">Whether or not the packet originates from the server.</param>
        /// <param name="isResponse">Whether or not the packet is a response to another one.</param>
        /// <param name="sequence">The sequence number of the packet.</param>
        /// <param name="words">The <see cref="Packet">Packet's</see> words.</param>
        public Packet(bool isFromServer, bool isResponse, int sequence, params string[] words)
        {
            Header = new Header(isFromServer, isResponse, sequence);
            WordList = new List<string>(words);
            _Words = new ReadOnlyCollection<string>(WordList);
        }

        /// <summary>
        /// Creates an instance of <see cref="Packet"/>.
        /// </summary>
        /// <param name="header">The header of the <see cref="Packet"/>.</param>
        /// <param name="words">The <see cref="Packet">Packet's</see> words.</param>
        public Packet(Header header, params string[] words)
        {
            Header = header;
            WordList = new List<string>(words);
            _Words = new ReadOnlyCollection<string>(WordList);
        }

        internal Packet(Header header, List<string> words)
        {
            Header = header;
            WordList = words;
            _Words = new ReadOnlyCollection<string>(words);
        }

        /// <summary>
        /// Creates an instance of <see cref="Packet"/>.
        /// </summary>
        /// <param name="header">The <see cref="Header"/> of the <see cref="Packet"/>.</param>
        public Packet(Header header)
        {
            Header = header;
            WordList = new List<string>();
            _Words = new ReadOnlyCollection<string>(WordList);
        }
        #endregion

        #region Protocol Code
        // This code provides the methods used by the basic RCON protocol, with some improvements.
        // Note that these methods are commented poorly because they are translations of python
        // code that wasn't commented at all.
        // Also, the use of uint over int is somewhat ambiguous. The way the protocol works
        // has uint usage preferred, but, practially, either can be used.
        // Uint usage has been changed to int usage, but may need to be changed back later

        // Also note that this is translated from python code that uses strings
        // instead of byte arrays, so there may be some errors.

        /// <summary>
        /// Converts an integer into bytes.
        /// </summary>
        /// <param name="num">The target integer.</param>
        /// <returns>A byte array containing an integer.</returns>
        /// <remarks>This method is here to imitate the python scripts.</remarks>
        static byte[] ToInt32(int num)
        {
            return BitConverter.GetBytes(num);
        }

        /// <summary>
        /// Converts a byte list into an integer.
        /// </summary>
        /// <param name="bytes">The target byte list.</param>
        /// <param name="offset">The zero-index offset at which to start in the byte array.</param>
        /// <returns>An integer containing the value in the byte array.</returns>
        /// <remarks>This method is here to imitate the python scripts, to an extent.</remarks>
        static int DecodeInt32(List<byte> bytes, int offset = 0)
        {
            // ensure the byte array has at least 4 bytes, and do it efficiently.
            // bitconverter will work if > 4 bytes
            if (bytes.Count >= 4) return BitConverter.ToInt32(new byte[] {
                bytes[0 + offset], bytes[1 + offset], bytes[2 + offset], bytes[3 + offset]
            }, 0);

            byte[] newBytes = new byte[4];
            for (int i = 0; i < bytes.Count; i++)
                newBytes[i] = bytes[i];

            return BitConverter.ToInt32(newBytes, 0);
        }

        /// <summary>
        /// Converts a byte array into an integer.
        /// </summary>
        /// <param name="bytes">The target byte array.</param>
        /// <param name="offset">The zero-index offset at which to start in the byte array.</param>
        /// <returns>An integer containing the value in the byte array.</returns>
        /// <remarks>This method is here to imitate the python scripts, to an extent.</remarks>
        static int DecodeInt32(byte[] bytes, int offset = 0)
        {
            // ensure the byte array has at least 4 bytes, and do it efficiently.
            // bitconverter will work if > 4 bytes
            if (bytes.Length >= 4) return BitConverter.ToInt32(bytes, offset);

            byte[] newBytes = new byte[4];
            for (int i = 0; i < bytes.Length; i++)
                newBytes[i] = bytes[i];

            return BitConverter.ToInt32(newBytes, 0);
        }

        /// <summary>
        /// Encodes the header of a packet.
        /// </summary>
        /// <param name="isFromServer">Whether or not the packet originates from the server.</param>
        /// <param name="isResponse">Whether or not the packet is a response to another one.</param>
        /// <param name="sequence">The sequence number of the packet.</param>
        /// <returns>A byte array containing the encoded header.</returns>
        static byte[] EncodeHeader(bool isFromServer, bool isResponse, int sequence)
        {
            return ToInt32(
                sequence & 0x3fffffff |
                (isFromServer ? unchecked((int)0x80000000) : 0) |
                (isResponse ? 0x40000000 : 0)
                );
        }

        ///// <summary>
        ///// Encodes the header of a packet from a <see cref="Header" /> object.
        ///// </summary>
        ///// <param name="header">The <see cref="Header" /> that is to be encoded.</param>
        ///// <returns>A byte array containing the encoded header.</returns>
        //static byte[] EncodeHeader(Header header)
        //{
        //    return EncodeHeader(header.IsFromServer, header.IsResponse, header.Sequence);
        //}

        /// <summary>
        /// Decodes the header of an encoded header.
        /// </summary>
        /// <param name="data">The raw data of the header or packet.</param>
        /// <returns>A <see cref="Header"/> object containing the values in the header.</returns>
        /// <remarks>The raw data for the header or packet can be supplied.</remarks>
        static Header DecodeHeader(List<byte> data)
        {
            // Since Decodeint32 only reads the first 4 bytes, the data param can be the whole packet
            int header = DecodeInt32(data);

            return new Header((header & 0x80000000) > 0, (header & 0x40000000) > 0, header & 0x3fffffff);
        }

        /// <summary>
        /// Encodes the words of a packet into a byte list.
        /// </summary>
        /// <param name="words">The words that are to be encoded.</param>
        /// <returns>A byte array containing the number of words and the encoded words.</returns>
        static List<byte> EncodeWords(List<string> words)
        {
            // For efficiency, we'll calculate the capacity of the list first, rather than have it expand later
            // Running two loops here is more efficient than expanding the list's array and possibly more than turning a
            // linked list into an array
            int capacity = 0;
            foreach (var word in words)
            {
                capacity += word.Length;
                capacity += 5; // 4 for the int determining length and 1 for a null byte at the end
            }

            List<byte> bytes = new List<byte>(capacity);
            foreach (var word in words)
            {
                bytes.AddRange(BitConverter.GetBytes(word.Length));

                // I never got this to work before, so i'll get the bytes manually. this is probably about the same
                // as the code below would do, anyway
                //bytes.AddRange(Encoding.UTF8.GetBytes(word));
                foreach (var c in word)
                    bytes.Add((byte)c);

                bytes.Add(0);
            }

            return bytes;
        }

        /// <summary>
        /// Decodes the words that have been encoded.
        /// </summary>
        /// <param name="data">The raw data of the words or packet.</param>
        /// <param name="numberOfWords">Optional. The number of words in a packet.</param>
        /// <param name="offset">The zero-index of where to begin decoding. If a packet is specified for
        /// <paramref name="data"/>, then offset should be 12.</param>
        /// <returns>A string array containing the decoded words.</returns>
        static List<string> DecodeWords(List<byte> data, int numberOfWords = 0, int offset = 0)
        {
            if (offset > data.Count || offset < 0) throw new ArgumentOutOfRangeException("offset",
                "offset is less than zero or greater than the length of data minus 1.");

            List<string> words = new List<string>(numberOfWords >= 0 ? numberOfWords : 0);
            int wordLength = 0; //used in loop below

            for (int i = offset; i < data.Count; i++) //start at 4 because 
            {
                wordLength = DecodeInt32(data, i);

                StringBuilder word = new StringBuilder(wordLength);

                i += 4; //read first 4 in wordLength

                int iCopy = i;

                while (i < iCopy + wordLength)
                {
                    word.Append((char)data[i]);
                    i++;
                }
                //at this point, i is at the null byte, so i++ goes to the first byte of the length

                words.Add(word.ToString());
            }

            return words;
        }

        /// <summary>
        /// Encodes a packet into raw data.
        /// </summary>
        /// <param name="isFromServer">Whether or not the packet originates from the server.</param>
        /// <param name="isResponse">Whether or not the packet is a response to another one.</param>
        /// <param name="sequence">The sequence number of the packet.</param>
        /// <param name="words">The packet's words.</param>
        /// <returns>A byte array containing a packet.</returns>
        static List<byte> EncodePacket(bool isFromServer, bool isResponse, int sequence, List<string> words)
        {
            List<byte> encodedWords = EncodeWords(words);

            //calculate the capacity of the list for efficiency
            int capacity = encodedWords.Count + 12; //12 is for the first 3 ints: header, packet size, num words

            List<byte> bytes = new List<byte>(capacity);

            bytes.AddRange(EncodeHeader(isFromServer, isResponse, sequence));
            bytes.AddRange(ToInt32(capacity));
            bytes.AddRange(ToInt32(words.Count));
            bytes.AddRange(encodedWords);

            return bytes;
        }

        /// <summary>
        /// Encodes a packet into raw data.
        /// </summary>
        /// <param name="header">The header of the packet.</param>
        /// <param name="words">The packet's words.</param>
        /// <returns>A byte array containing a packet.</returns>
        static List<byte> EncodePacket(Header header, List<string> words)
        {
            return EncodePacket(header.IsFromServer, header.IsResponse, header.Sequence, words);
        }

        /// <summary>
        /// Encodes a packet into raw data.
        /// </summary>
        /// <param name="packet">The packet that is to be encoded.</param>
        /// <returns>A byte array containing a packet.</returns>
        static List<byte> EncodePacket(Packet packet)
        {
            return EncodePacket(packet.Header, packet.WordList);
        }

        /// <summary>
        /// Decodes a packet from raw data and also gives the data not needed by the packet.
        /// </summary>
        /// <param name="data">The data that is to be decoded.</param>
        /// <param name="leftoverData">The leftover data that will be part of another packet.</param>
        /// <returns>
        /// A <see cref="Packet"/> containing the decoded data or null (Nothing in VB) if
        /// a complete packet wasn't contained in the <paramref name="data"/>.
        /// </returns>
        /// <remarks>
        /// It is possible that the leftover data contains another whole packet.<para />
        /// If there is not a complete packet in <paramref name="data"/>, then <paramref name="leftoverData"/>
        /// will contain all of the <paramref name="data"/>.
        /// </remarks>
        static Packet DecodePacket(List<byte> data, List<byte> leftoverData)
        {
            int newCapacity;
            if (!ContainsCompletePacket(data))
            {
                newCapacity = data.Count + RconClient.BufferSize;
                if (newCapacity > leftoverData.Capacity) leftoverData.Capacity = newCapacity;

                //clear old data
                leftoverData.Clear();

                leftoverData.AddRange(data);
                return null;
            }

            //get packet size
            int packetSize = DecodeInt32(data, 4);

            List<byte> fullPacket = new List<byte>(packetSize);

            //get the full packet
            for (int i = 0; i < packetSize; i++)
                fullPacket.Add(data[i]);

            //make the list large enough to support the leftover data plus a new read
            newCapacity = data.Count - packetSize + RconClient.BufferSize;
            if (newCapacity > leftoverData.Capacity) leftoverData.Capacity = newCapacity;

            //clear leftoverdata now. dont do it before this because leftoverdata and data may be the same.
            leftoverData.Clear();

            //get leftover data
            for (int i = packetSize; i < data.Count; i++)
                leftoverData.Add(data[i]);

            //decodeheader only reads the first 4 bytes, and the words begin at the 13th byte
            return new Packet(DecodeHeader(fullPacket), DecodeWords(fullPacket, 0, 12));
        }

        ///// <summary>
        ///// Decodes a packet from raw data and also gives the data not needed by the packet.
        ///// </summary>
        ///// <param name="data">The data that is to be decoded.</param>
        ///// <returns>
        ///// A <see cref="Packet"/> containing the decoded data or null (Nothing in VB) if
        ///// a complete packet wasn't contained in the <paramref name="data"/>.
        ///// </returns>
        //static Packet DecodePacket(byte[] data)
        //{
        //    //rather than just base this off the last method, save it some time and do only 1 loop
        //    if (!ContainsCompletePacket(data)) return null;

        //    //get packet size
        //    int packetSize = DecodeInt32(data, 4);

        //    byte[] fullPacket = new byte[packetSize];

        //    //get the full packet
        //    for (int i = 0; i < packetSize; i++)
        //        fullPacket[i] = data[i];

        //    //decodeheader only reads the first 4 bytes, and the words begin at the 13th byte
        //    return new Packet(DecodeHeader(fullPacket), DecodeWords(fullPacket, 0, 12));
        //}

        ///// <summary>
        ///// Determines whether the raw data contains a complete packet.
        ///// </summary>
        ///// <param name="data">The data that is to be inspected.</param>
        ///// <param name="bytesRead">The number of bytes read, representing the actual data in the array.</param>
        ///// <returns>True if the <paramref name="data"/> contains a complete packet.</returns>
        //static internal bool ContainsCompletePacket(byte[] data, int bytesRead = -1) //is internal because it's also used by RconClient
        //{
        //    //if data was read from a buffer, bytesRead will have been set to the number of bytes read
        //    //if it's still the default of -1, data is assumed to be containing only the data
        //    if (bytesRead < 0) bytesRead = data.Length;

        //    return bytesRead >= 8 && bytesRead >= DecodeInt32(data, 4);
        //}

        /// <summary>
        /// Determines whether the raw data contains a complete packet.
        /// </summary>
        /// <param name="data">The data that is to be inspected.</param>
        /// <returns>True if the <paramref name="data"/> contains a complete packet.</returns>
        static internal bool ContainsCompletePacket(List<byte> data) //is internal because it's also used by RconClient
        {
            return data.Count >= 8 && data.Count >= DecodeInt32(data.ToArray(), 4);
        }
        #endregion

    }
}
