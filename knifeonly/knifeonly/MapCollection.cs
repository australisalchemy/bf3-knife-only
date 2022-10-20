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
using System.Globalization;
using System.Collections.ObjectModel;

namespace System.Net.Battlefield3
{
    /// <summary>
    /// Provides a wrapper around RCON's maplist functionality, allowing enumeration and manipulation of the maplist, as 
    /// well as the ability to end and change rounds and set the next map.
    /// </summary>
    /// <remarks>
    /// For comparison operations, such as <see cref="Contains"/>, <see cref="Map">Maps</see> will match when their 
    /// <see cref="Map.Name"/> and <see cref="Map.Mode"/> match.
    /// </remarks>
    public sealed class MapCollection : MarshalByRefObject, IList<Map>
    {
        //note that maplist manipulation uses synchronous requests because it's to the benefit of the programmer
        //to be able to know right when manipulation is done (at the end of the method)
        RconClient Client;

        internal MapCollection(RconClient client)
        {
            Client = client;
        }

        /// <summary>
        /// Gets the <see cref="Map"/> at the specified <paramref name="index"/>.
        /// </summary>
        /// <param name="index">The zero-based index of the <see cref="Map"/> to get.</param>
        /// <returns>The <see cref="Map"/> at that index or null if the query timed out.</returns>
        public Map this[int index]
        {
            get
            {
                return (this as IList<Map>)[index];
            }
        }

        /// <summary>
        /// Gets or sets the next <see cref="Map"/> in the <see cref="MapCollection"/>.
        /// </summary>
        /// <value>The next <see cref="Map"/> or null (Nothing in Visual Basic) if the request fails.</value>
        /// <remarks>
        /// When setting the next <see cref="Map"/>, only <see cref="Map">Maps</see> created by the same
        /// <see cref="MapCollection"/> can be used.
        /// </remarks>
        public Map NextMap
        {
            get
            {
                Packet p = Client.InternalSendRequest("mapList.getMapIndices");
                if (!p.Success()) return null;

                List<Map> maps = GetMaps();
                if (maps == null) return null;

                return maps[p.Words[2].ToInt32()];
            }

            set
            {
                if (!Client.IsLoggedOn || value.Index(this) == -1) return;

                Client.InternalSendRequest("mapList.setNextMapIndex", value.Index(this).ToStringG());
            }
        }

        /// <summary>
        /// Sets the next <see cref="Map"/> in the <see cref="MapCollection"/>.
        /// </summary>
        /// <param name="index">The zero-based index of the next <see cref="Map"/>.</param>
        /// <returns>True if the request succeeds or false if the request fails.</returns>
        public bool SetNextMap(int index)
        {
            if (index < 0 || index >= Count)
                throw new ArgumentOutOfRangeException("index",
                    "index must be greater than or equal to zero and less than Count.");

            return Client.InternalSendRequest("mapList.setNextMapIndex", index.ToStringG()).Success();
        }

        /// <summary>
        /// Gets the current <see cref="Map"/>.
        /// </summary>
        /// <value>The current <see cref="Map"/>.</value>
        public Map CurrentMap
        {
            get
            {
                Packet p = Client.InternalSendRequest("mapList.getMapIndices");
                if (p.Success())
                    return this[p.Words[1].ToInt32()];

                return null;
            }
        }

        /// <summary>
        /// The <see cref="Map"/> is changed to the next round. If the last round is the one being skipped, the <see cref="Map"/>
        /// is changed.
        /// </summary>
        /// <returns>True if the request succeeds or false if the request times out or request fails.</returns>
        public bool RunNextRound()
        {
            return Client.InternalSendRequest("mapList.runNextRound").Success();
        }

        /// <summary>
        /// Restarts the current round.
        /// </summary>
        /// <returns>True if the request succeeds or false if the request times out of request fails.</returns>
        public bool RestartRound()
        {
            return Client.InternalSendRequest("mapList.runNextRound").Success();
        }

        /// <summary>
        /// Ends the round, with the specified <paramref name="team"/> winning.
        /// </summary>
        /// <param name="team">The team that will win the round.</param>
        /// <returns>True if the request succeeds or false if the request times out or request fails.</returns>
        public bool EndRound(int team)
        {
            if (team < 1 || team > 16)
                throw new ArgumentOutOfRangeException("team",
                    "team must be greater than or equal to zero and less than or equal to 16.");

            return Client.InternalSendRequest("mapList.runNextRound").Success();
        }

        #region Private Methods
        Packet GetMapPacket()
        {
            return Client.IsLoggedOn ? Client.InternalSendRequest("mapList.list") : null;
        }

        List<Map> GetMaps()
        {
            Packet mapInfo = GetMapPacket();
            Packet roundInfo = Client.InternalSendRequest("mapList.getRounds"); //will be used to get current round
            Packet indicesInfo = Client.InternalSendRequest("mapList.getMapIndices"); //contains indices for current map
            if (mapInfo == null) return null;

            ReadOnlyCollection<string> words = mapInfo.Words;

            //get values for current map and round info. if timeout (packets == null), then dont put that info in map
            int currentMap = indicesInfo == null ? -1 : indicesInfo.Words[1].ToInt32();
            int roundsElapsed = roundInfo == null  ? -1 : roundInfo.Words[1].ToInt32();

            //start parsing the packet
            int numberOfMaps = words[1].ToInt32();
            int wordsPerMap = words[2].ToInt32();

            List<Map> maps = new List<Map>(numberOfMaps);

            //loop through words to get each map
            for (int i = 0; i < numberOfMaps; i++)
            {
                int firstElement = 3 + i * wordsPerMap; //3 is from "OK" and the first two params
                maps.Add(new Map(
                    RawValueToMapName(words[firstElement]),
                    RawValueToMapMode(words[firstElement + 1]),
                    words[firstElement + 2].ToInt32(),
                    currentMap == i ? roundsElapsed + 1 : 0, //if this map is the current map, set rounds elapsed
                    i,
                    this
                    ));
            }

            return maps;
        }

        void Save()
        {
            Client.InternalSendRequest("mapList.save");
        }
        #endregion

        #region Converting from raw values
        //the methods for converting to raw values are in the Map class
        //they're internal now because Server uses them

        internal static MapName RawValueToMapName(string map)
        {
            //special case
            if (map == "MP_Subway") return MapName.OperationMetro;

            //the rest are numerical
            MapName name;
            int tempInt; //will be used to see if the map is a known map
            if (!Enum.TryParse(map.Substring(3), out name) ||//use 3 because map will be MP_xxx
                int.TryParse(name.ToString(), out tempInt))  //should not be a number
                return MapName.Unknown;

            return name;
        }

        internal static MapMode RawValueToMapMode(string mode)
        {
            Type t = typeof(MapMode);
            //use reflection to compare raw values
            foreach (var field in typeof(MapMode).GetFields())
            {
                if (field.IsStatic) //in an enum, only the enum's members are static
                {
                    //compare attribute to mode
                    if (mode ==
                        (field.GetCustomAttributes(typeof(RawValueAttribute), false)[0] as RawValueAttribute).RawValue) //gets raw value
                        return (MapMode)Enum.Parse(t, field.Name); //if they're equal, return the mode
                }
            }

            return MapMode.Unknown;
        }
        #endregion

        #region IList
        /// <summary>
        /// Gets the index of the <see cref="MapCollection"/>-made <see cref="Map"/>,
        /// or searches for the specified <see cref="Map"/> and returns
        /// the zero-based index where the <see cref="Map">Map's</see> <see cref="Map.Name"/> and <see cref="Map.Mode"/> 
        /// match within the <see cref="MapCollection"/>.
        /// </summary>
        /// <param name="item">The <see cref="Map"/> that will be searched for.</param>
        /// <returns>The zero-based index where the <see cref="Map">Map's</see> <see cref="Map.Name"/> and 
        /// <see cref="Map.Mode"/> match within the <see cref="MapCollection"/> or -1 if the request times out.</returns>
        public int IndexOf(Map item)
        {
            if (item.Index(this) > -1) return item.Index(this);

            List<Map> maps = GetMaps();
            if (maps == null) return -1;

            return maps.IndexOf(item);
        }

        /// <summary>
        /// Inserts the <see cref="Map"/> into the maplist at the specified <paramref name="index"/>.
        /// </summary>
        /// <param name="index">The zero-based index at which the <see cref="Map"/> should be inserted.</param>
        /// <param name="item">The <see cref="Map"/> that is to be inserted.</param>
        public void Insert(int index, Map item)
        {
            if (!Client.IsLoggedOn) return;

            if (index < 0 || index > Count)
                throw new ArgumentOutOfRangeException("index",
                    "index must be greater than or equal to zero and less than or equal to Count.");

            if (item.Mode == MapMode.Unknown || item.Name == MapName.Unknown)
                throw new ArgumentException("item must be a known map and mode.", "item");

            if (item == null) throw new ArgumentNullException("item", "item must not be null.");

            //send command
            Client.InternalSendRequest("mapList.add", item.RawName, item.RawMode, item.TotalRounds.ToStringG(), index.ToStringG());
            Save();
        }

        /// <summary>
        /// Removed the <see cref="Map"/> at the specified <paramref name="index"/> from the <see cref="MapCollection"/>.
        /// </summary>
        /// <param name="index">The zero-based index of the <see cref="Map"/> to remove.</param>
        public void RemoveAt(int index)
        {
            if (!Client.IsLoggedOn) return;

            if (index < 0 || index >= Count)
                throw new ArgumentOutOfRangeException("index",
                    "index must be greater than or equal to zero and less than Count.");

            //send command
            Client.InternalSendRequest("mapList.remove", index.ToStringG());
            Save();
        }

        Map IList<Map>.this[int index]
        {
            get
            {
                List<Map> maps = GetMaps();
                if (maps == null) return null;

                if (index < 0 || index >= maps.Count)
                    throw new ArgumentOutOfRangeException("index",
                        "index must be greater than or equal to zero and less than MapCollection.Count");
                return maps[index];
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        /// <summary>
        /// Adds a <see cref="Map"/> to the end of the <see cref="MapCollection"/>.
        /// </summary>
        /// <param name="item">The <see cref="Map"/> to be added to the end of the <see cref="MapCollection"/>.</param>
        public void Add(Map item)
        {
            if (!Client.IsLoggedOn) return;

            if (item.Mode == MapMode.Unknown || item.Name == MapName.Unknown)
                throw new ArgumentException("item must be a known map and mode.", "item");

            if (item == null) throw new ArgumentNullException("item", "item must not be null.");

            //send command
            Client.InternalSendRequest("mapList.add", item.RawName, item.RawMode, item.TotalRounds.ToStringG());
            Save();
        }

        /// <summary>
        /// Removes all <see cref="Map">Maps</see> from the <see cref="MapCollection"/>.
        /// </summary>
        public void Clear()
        {
            if (!Client.IsLoggedOn) return;

            Client.InternalSendRequest("mapList.clear");
            Save();
        }

        /// <summary>
        /// Determines whether a <see cref="Map"/> is in the maplist
        /// </summary>
        /// <param name="item">The <see cref="Map"/> to locate in the maplist.</param>
        /// <returns>True if the <see cref="Map">Map's</see> <see cref="Map.Name"/> and 
        /// <see cref="Map.Mode"/> match within the <see cref="MapCollection"/> or returns false if the request fails.</returns>
        public bool Contains(Map item)
        {
            List<Map> maps = GetMaps();
            if (maps == null) return false;

            return maps.Contains(item);
        }

        void ICollection<Map>.CopyTo(Map[] array, int arrayIndex)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Gets the number of <see cref="Map">Maps</see> in the <see cref="MapCollection"/>.
        /// </summary>
        /// <value>The number of <see cref="Map">Maps</see> in the <see cref="MapCollection"/> or -1 if the request fails.</value>
        public int Count
        {
            get
            {
                Packet p = GetMapPacket();
                if (!p.Success()) return -1;

                return p.Words[1].ToInt32(); //first word of the block is number of maps
            }
        }

        /// <summary>
        /// Gets whether or not the <see cref="MapCollection"/> is read-only.
        /// </summary>
        /// <value>True if <see cref="RconClient.IsLoggedOn"/> is true.</value>
        public bool IsReadOnly
        {
            get { return Client.IsLoggedOn; }
        }

        /// <summary>
        /// Removes the <see cref="Map"/> from the <see cref="MapCollection"/>.
        /// </summary>
        /// <param name="item">The <see cref="Map"/> to be removed.</param>
        /// <returns>
        /// True if the removal succeeds or false if the <see cref="Map"/> did not come from the <see cref="MapCollection"/>
        /// and the query to find a matching <see cref="Map"/> failed.
        /// </returns>
        /// <remarks>
        /// If the <see cref="Map"/> came from the <see cref="MapCollection"/>, each <see cref="Map"/> stores its index 
        /// (note that the index could change after a period of time if modified outside of the original
        /// <see cref="RconClient"/>). In this case, the stored index is used. If the index does not exist, the first 
        /// occurrence of a matching <see cref="Map"/> is used.
        /// </remarks>
        public bool Remove(Map item)
        {
            if (!Client.IsLoggedOn) return false;

            int index = item.Index(this) > -1 ? item.Index(this) : GetMaps().IndexOf(item); //get the index from the map or from the first occurrence of the map
            if (index == -1) return false; //return false if there is no map

            bool result = Client.InternalSendRequest("mapList.remove", index.ToStringG()).Success();
            Save();

            return result;
        }

        /// <summary>
        /// Returns an enumerator that iterates through the <see cref="MapCollection"/>.
        /// </summary>
        /// <returns>An <see cref="T:System.Collections.Generic.IEnumerator`1">IEnumerator(Map)</see> that iterates through the <see cref="MapCollection"/>.</returns>
        public IEnumerator<Map> GetEnumerator()
        {
            List<Map> maps = GetMaps();

            if (maps == null) return Enumerable.Empty<Map>().GetEnumerator();

            return GetMaps().GetEnumerator();
        }

        /// <summary>
        /// Returns an enumerator that iterates through the <see cref="MapCollection"/>.
        /// </summary>
        /// <returns>An <see cref="T:System.Collections.IEnumerator"/> that iterates through the <see cref="MapCollection"/>.</returns>
        Collections.IEnumerator Collections.IEnumerable.GetEnumerator()
        {
            List<Map> maps = GetMaps();

            if (maps == null) return Enumerable.Empty<Map>().GetEnumerator();

            return GetMaps().GetEnumerator();
        }
        #endregion
    }
}
