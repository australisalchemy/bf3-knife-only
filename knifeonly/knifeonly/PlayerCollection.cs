/*
BF3Rcon.NET, provides a .NET implementation of BF3's RCON interface.
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
using System.Globalization;
using System.Collections;

namespace System.Net.Battlefield3
{
    /// <summary>
    /// Contains all of the <see cref="Player">Players</see> in the server an <see cref="RconClient"/> is connected to.
    /// </summary>
    public sealed class PlayerCollection : MarshalByRefObject, IList<Player>
    {
        const int MaxPlayerCount = 64;
        RconClient Client;
        Packet Packet;
        int Offset = 1; //the offset to which the playerinfo block starts

        //this will allow rconclient to create playercollections for events that supply playerinfo (optional)
        internal PlayerCollection(RconClient client, Packet packet = null, int offset = 1)
        {
            Client = client;
            Packet = packet;
            Offset = offset;
        }

        #region Helper Methods
        Packet GetPacket()
        {
            return Packet != null ? Packet : Client.InternalSendRequest(Client.PlayerInfoCommand, "all");
        }

        //this will be called, being supplied the packet
        List<string> GetParameters(Packet packet)
        {
            //rconclient-imported vars
            ReadOnlyCollection<string> words = packet.Words;
            int offset = Offset;

            int numberOfParameters = words[offset].ToInt32();
            List<string> parameterNames = new List<string>(numberOfParameters);
            for (int i = 0; i < numberOfParameters; i++)
                parameterNames.Add(words[i + offset + 1]);

            return parameterNames;
        }

        IEnumerable<Player> GetAllPlayers()
        {
            Packet playerInfo = GetPacket();

            if (playerInfo.Success())
            {
                //these vars are from code ported from RconClient to match the prior method's signature
                RconClient client = Client; //redundant I know D:, but too lazy for find/replace
                ReadOnlyCollection<string> words = playerInfo.Words;
                int offset = Offset;

                //get the parameters (properties is probably a better word)
                List<string> parameterNames = GetParameters(playerInfo);
                int numberOfParameters = parameterNames.Count;

                int numberOfPlayers = words[offset + numberOfParameters + 1].ToInt32();

                int offset2 = numberOfParameters + offset + 2; //to shorten code to where the players start

                //get the players
                for (int i = 0; i < numberOfPlayers; i++)
                {
                    Dictionary<string, string> playerProperties = new Dictionary<string, string>(numberOfParameters);

                    //add properties to dict
                    for (int j = 0; j < numberOfParameters; j++)
                        playerProperties[parameterNames[j]] = words[(i * numberOfParameters) + offset2 + j];

                    Player newPlayer = new Player(client, playerProperties);

                    yield return newPlayer;
                }
            }
            yield break;
        }
        #endregion

        /// <summary>
        /// Gets the <see cref="Player"/> at the specified index in the <see cref="PlayerCollection"/>.
        /// </summary>
        /// <param name="index">The index of the <see cref="Player"/>.</param>
        /// <returns>The <see cref="Player"/> at the specified index or null if the request times out.</returns>
        public Player this[int index]
        {
            get
            {
                if (index < 0) throw new ArgumentOutOfRangeException("index", "index must be greather than or equal to zero.");

                Packet playerInfo = GetPacket();

                if (playerInfo.Success())
                {
                    //these vars are from code ported from RconClient to match the prior method's signature
                    RconClient client = Client; //redundant I know D:, but too lazy for find/replace
                    ReadOnlyCollection<string> words = playerInfo.Words;
                    int offset = Offset;

                    //get the parameters (properties is probably a better word)
                    List<string> parameterNames = GetParameters(playerInfo);
                    int numberOfParameters = parameterNames.Count;

                    int numberOfPlayers = words[offset + numberOfParameters + 1].ToInt32();

                    if (index >= numberOfPlayers)
                        throw new ArgumentOutOfRangeException("index",
                            "index must be greather than or equal to zero and less than or equal to Count.");

                    Dictionary<string, string> playerProperties = new Dictionary<string, string>(numberOfParameters);

                    for (int i = 0; i < numberOfParameters; i++)
                    {
                        playerProperties[parameterNames[i]] =
                            words[numberOfParameters + offset + 2 + //gets start of players
                            index * numberOfParameters + //gets target player
                            i]; //get current property
                    }

                    return new Player(client, playerProperties);
                }
                return null;
            }
            //internal set { PlayerCollection[index] = value; }
        }

        /// <summary>
        /// Gets the <see cref="Player"/> with the matching name.
        /// </summary>
        /// <param name="name">The name of the <see cref="Player"/>.</param>
        /// <returns>A <see cref="Player"/> with a matching name or null if the request times out.</returns>
        public Player this[string name]
        {
            get
            {
                Packet playerInfo = GetPacket();

                if (playerInfo.Success())
                {
                    //these vars are from code ported from RconClient to match the prior method's signature
                    RconClient client = Client; //redundant I know D:, but too lazy for find/replace
                    ReadOnlyCollection<string> words = playerInfo.Words;
                    int offset = Offset;

                    //get the parameters (properties is probably a better word)
                    List<string> parameterNames = GetParameters(playerInfo);
                    int numberOfParameters = parameterNames.Count;

                    int numberOfPlayers = words[offset + numberOfParameters + 1].ToInt32();

                    int offset2 = numberOfParameters + offset + 2; //to shorten code to where the players start

                    //get the players
                    for (int i = 0; i < numberOfPlayers; i++)
                    {
                        //the name will be the 1st prop, so get it first
                        string name2 = words[(i * numberOfParameters) + offset2];

                        if (name == name2)
                        {
                            Dictionary<string, string> playerProperties = new Dictionary<string, string>(numberOfParameters);

                            //add name
                            playerProperties["name"] = name2;

                            //add properties to dict
                            for (int j = 1; j < numberOfParameters; j++) //j=1 because name is already gotten
                                playerProperties[parameterNames[j]] = words[(i * numberOfParameters) + offset2 + j];

                            Player newPlayer = new Player(client, playerProperties);

                            return newPlayer;
                        }
                    }
                    throw new ArgumentException("A Player with the specified name does not exist.", "name");
                }
                return null;
            }
        }

        /// <summary>
        /// Determines if the <see cref="Player"/> exists by name.
        /// </summary>
        /// <param name="name">The name of the <see cref="Player"/>.</param>
        /// <returns>True if the <see cref="Player"/> is in the <see cref="PlayerCollection"/>.</returns>
        public bool PlayerExists(string name)
        {
            Player notImportantBecauseThisIsJustToTestWhatTryGetPlayerReturns;
            return TryGetPlayer(name, out notImportantBecauseThisIsJustToTestWhatTryGetPlayerReturns);
        }

        /// <summary>
        /// Gets a <see cref="Player"/> by name.
        /// </summary>
        /// <param name="name">The name of the <see cref="Player"/>.</param>
        /// <param name="player">
        /// The <see cref="Player"/> with the specified name, or null (Nothing in VB)
        /// if the <see cref="Player"/> wasn't found will, be stored here.
        /// </param>
        /// <returns>True if the <see cref="Player"/> was found.</returns>
        public bool TryGetPlayer(string name, out Player player)
        {
            try
            {
                player = this[name];
            }
            catch (ArgumentException)
            {
                player = null;
                return false;
            }
            return true;
        }

        #region IList<Player> Members

        /// <summary>
        /// Searches for the specified <see cref="Player"/> and returns
        /// the zero-based index where the names match within the <see cref="PlayerCollection"/>.
        /// </summary>
        /// <param name="item">The <see cref="Player"/> that will be searched for.</param>
        /// <returns>The zero-based index where the specified <see cref="Player">Player's</see> name matched one
        /// in the <see cref="PlayerCollection"/>.</returns>
        public int IndexOf(Player item)
        {
            //find players that are the same and get index
            var possiblePlayerIndexes =
                GetAllPlayers().Select((p, i) => new { Index = i, Player = p }).Where(p => p.Player.Equals(item)).Select(p => p.Index);

            if (possiblePlayerIndexes.Count() > 0) return possiblePlayerIndexes.First();
            return -1;
        }

        void IList<Player>.Insert(int index, Player item)
        {
            throw new NotSupportedException("PlayerCollection is read-only.");
        }

        void IList<Player>.RemoveAt(int index)
        {
            throw new NotSupportedException("PlayerCollection is read-only.");
        }

        Player IList<Player>.this[int index]
        {
            get
            {
                return this[index];
                //return PlayerCollection[index];
            }
            set
            {
                throw new NotSupportedException("PlayerCollection is read-only.");
            }
        }
        #endregion

        #region ICollection<Player> Members

        void ICollection<Player>.Add(Player item)
        {
            throw new NotSupportedException("PlayerCollection is read-only.");
        }

        void ICollection<Player>.Clear()
        {
            throw new NotSupportedException("PlayerCollection is read-only.");
        }

        /// <summary>
        /// Determines whether the specified <see cref="Player"/> has the same name
        /// as another in the <see cref="PlayerCollection"/>.
        /// </summary>
        /// <param name="item">The <see cref="Player"/> to locate in the <see cref="PlayerCollection"/>.</param>
        /// <returns>True if the specified <see cref="Player"/> is in the <see cref="PlayerCollection"/>.</returns>
        public bool Contains(Player item)
        {
            return GetAllPlayers().Contains(item);
            //return PlayerCollection.Contains(item);
        }

        /// <summary>
        /// Copies the entire <see cref="PlayerCollection"/> to a compatible array, starting at the specified index of the target array.
        /// </summary>
        /// <param name="array">The array that will hold the <see cref="Player">Players</see>.</param>
        /// <param name="arrayIndex">The zero-based index in the <paramref name="array"/> where copying will begin.</param>
        public void CopyTo(Player[] array, int arrayIndex)
        {
            // these were made to make exceptions more specific to PlayerCollection, but getting exceptions
            // from List<Player> is probably safer
            //if (array == null) throw new NullReferenceException("array is a null reference (Nothing in Visual Basic).");
            //if (index < 0) throw new IndexOutOfRangeException("index is less than zero.");
            //if (Count > array.Length - index) throw new ArgumentException("The number of Players in the PlayerCollection is greater than the available space from index to the end of the destination array.");

            GetAllPlayers().ToList().CopyTo(array, arrayIndex);
            //PlayerCollection.CopyTo(array, index);
        }

        /// <summary>
        /// Gets the number of <see cref="Player">Players</see> in the <see cref="PlayerCollection"/>.
        /// </summary>
        /// <value>
        /// The number of <see cref="Player">Players</see> in the <see cref="PlayerCollection"/>
        /// or -1 if the request times out.</value>
        public int Count
        {
            get
            {
                Packet playerInfo = GetPacket();
                if (playerInfo.Success())
                {
                    return playerInfo.Words
                        [
                            playerInfo.Words[Offset].ToInt32() + //number of parameters
                            Offset + 1 //next word is number of players
                        ].ToInt32();
                }

                return -1;
            }
        }

        // note that even though it's readonly, internal methods will allow the list to be edited

        /// <summary>
        /// Gets whether or not the <see cref="PlayerCollection"/> is read-only.
        /// </summary>
        /// <value>True because only the <see cref="RconClient"/> may modify a <see cref="PlayerCollection"/>.</value>
        public bool IsReadOnly
        {
            get { return true; }
        }

        bool ICollection<Player>.Remove(Player item)
        {
            throw new NotSupportedException("PlayerCollection is read-only.");
        }

        #endregion

        #region IEnumerable<Player> Members
        /// <summary>
        /// Returns an enumerator that iterates through the <see cref="PlayerCollection"/>.
        /// </summary>
        /// <returns>An IEnumerator(Player) that iterates through the <see cref="PlayerCollection"/>.</returns>
        public IEnumerator<Player> GetEnumerator()
        {
            return GetAllPlayers().GetEnumerator();
            //return PlayerCollection.GetEnumerator();
        }

        #endregion

        #region IEnumerable Members
        /// <summary>
        /// Returns an enumerator that iterates through the <see cref="PlayerCollection"/>.
        /// </summary>
        /// <returns>An <see cref="T:System.Collections.Generic.IEnumerator`1">IEnumerator(Player)</see> that iterates through the <see cref="PlayerCollection"/>.</returns>
        Collections.IEnumerator Collections.IEnumerable.GetEnumerator()
        {
            return GetAllPlayers().GetEnumerator();
            //return PlayerCollection.GetEnumerator();
        }

        #endregion
    }
}
