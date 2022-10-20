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

namespace System.Net.Battlefield3
{
    /// <summary>
    /// Represents the header of an RCON packet.
    /// </summary>
    public struct Header
    {
        /// <summary>
        /// Gets or sets whether or not the <see cref="Packet"/> was sent by the server.
        /// </summary>
        /// <value>True if the <see cref="Packet"/> was sent by the server.</value>
        public bool IsFromServer { get; set; }

        /// <summary>
        /// Gets or sets whether or not the <see cref="Packet"/> is responding to another <see cref="Packet"/>.
        /// </summary>
        /// <value>True if the <see cref="Packet"/> is responding to another <see cref="Packet"/>.</value>
        public bool IsResponse { get; set; }

        /// <summary>
        /// Gets or sets the number uniquely identifying the <see cref="Packet"/> or set of <see cref="Packet">Packets</see>.
        /// </summary>
        /// <value>An integer uniquely identifying the <see cref="Packet"/>.</value>
        /// <remarks>
        /// If <see cref="IsResponse"/> is true, the <see cref="Sequence"/> will match that of an original <see cref="Packet"/>.<para />
        /// For instance, when sending a command to get player data, the server will respond with the same <see cref="Sequence"/>.
        /// </remarks>
        public int Sequence { get; set; }

        /// <summary>
        /// Creates a protocol <see cref="Header"/> object..
        /// </summary>
        /// <param name="isFromServer">Whether or not the <see cref="Packet"/> is from the server.</param>
        /// <param name="isResponse">Whether or not the <see cref="Packet"/> is responding to another one.</param>
        /// <param name="sequence">The sequence number of the <see cref="Packet"/>.</param>
        public Header(bool isFromServer, bool isResponse, int sequence) : this()
        {
            IsFromServer = isFromServer;
            IsResponse = isResponse;
            Sequence = sequence;
        }

        /// <summary>
        /// Specifies whether this <see cref="Header"/> is the same as the specified object.
        /// </summary>
        /// <param name="obj">The object to test.</param>
        /// <returns>True if this <see cref="Header"/> is the same as the object.</returns>
        public override bool Equals(object obj)
        {
            if (!(obj is Header)) return false;

            Header h = (Header)obj;

            return h.IsFromServer == IsFromServer && h.IsResponse == IsResponse && h.Sequence == Sequence;
        }

        /// <summary>
        /// Returns the hash code of this <see cref="Header"/>.
        /// </summary>
        /// <returns>An integer value specifying the hash value for this <see cref="Header"/>.</returns>
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        /// <summary>
        /// Compares two <see cref="Header"/> objects.
        /// </summary>
        /// <param name="firstHeader">The first <see cref="Header"/>.</param>
        /// <param name="secondHeader">The second <see cref="Header"/>.</param>
        /// <returns>True if both headers contain the same values.</returns>
        public static bool operator ==(Header firstHeader, Header secondHeader)
        {
            return firstHeader.Equals(secondHeader);
        }

        /// <summary>
        /// Compares two <see cref="Header"/> objects.
        /// </summary>
        /// <param name="firstHeader">The first <see cref="Header"/>.</param>
        /// <param name="secondHeader">The second <see cref="Header"/>.</param>
        /// <returns>True if both headers do not contain the same values.</returns>
        public static bool operator !=(Header firstHeader, Header secondHeader)
        {
            return !(firstHeader == secondHeader);
        }
    }
}
