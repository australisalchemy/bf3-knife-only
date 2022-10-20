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
using System.Reflection;
using System.ComponentModel;

namespace System.Net.Battlefield3
{
    /// <summary>
    /// Represents an entry in a server's maplist, containing the map, mode, and rounds.
    /// </summary>
    [Serializable()]
    public class Map :
        IEquatable<Map>
    {
        /// <summary>
        /// Gets the name of the <see cref="Map"/>.
        /// </summary>
        /// <value>The <see cref="MapName"/> of the <see cref="Map"/>.</value>
        public MapName Name { get; private set; }

        /// <summary>
        /// Gets the friendly, human-readable name of the <see cref="Map"/>.
        /// </summary>
        /// <value>The human-readable name of the <see cref="Map"/>.</value>
        public string FriendlyName
        {
            get
            {
                //it's safe to get the desc this way because it's programmed to always have only one desc attribute
                return (typeof(MapName).GetMember(Name.ToString())[0] //an array of memberinfo, where the 0th is the one we want
                    .GetCustomAttributes(typeof(DescriptionAttribute), false)[0] //an array of attributes, where the 0th is the one we want
                    as DescriptionAttribute) //this is the attribute
                    .Description; //the description
            }
        }

        //dont use reflection to get the rawvalues because reflection is slower
        /// <summary>
        /// Gets the raw name of the <see cref="Map"/> that the server understands.
        /// </summary>
        /// <value>The raw name of the <see cref="Map"/>.</value>
        public string RawName
        {
            get
            {
                if (Name == MapName.Unknown) return null; //unknown maps dont come out to any maps
                else if (Name == MapName.OperationMetro) return "MP_Subway"; //this map isn't numerical
                else return "MP_{0:000}".Format2((int)Name);
            }
        }

        /// <summary>
        /// Gets the gamemode of the <see cref="Map"/>.
        /// </summary>
        /// <value>The <see cref="MapMode"/> of the <see cref="Map"/>.</value>
        public MapMode Mode { get; private set; }

        /// <summary>
        /// Gets the friendly, human-readable gamemode of the <see cref="Map"/>.
        /// </summary>
        /// <value>The human-readable gamemode of the <see cref="Map"/>.</value>
        public string FriendlyMode
        {
            get
            {
                MemberInfo[] m = typeof(MapMode).GetMember(Mode.ToString());

                if (m.Length == 0) return "Unknown"; //unknown map

                //it's safe to get the desc this way because it's programmed to always have only one desc attribute
                return (typeof(MapMode).GetMember(Mode.ToString())[0] //an array of memberinfo, where the 0th is the one we want
                    .GetCustomAttributes(typeof(DescriptionAttribute), false)[0] //an array of attributes, where the 1st is the one we want
                    as DescriptionAttribute) //this is the attribute
                    .Description; //the description
            }
        }

        //reflection is done on this raw value because there's no pattern to go by
        /// <summary>
        /// Gets the raw gamemode of the <see cref="Map"/> that the server understands.
        /// </summary>
        /// <value>The raw gamemode of the <see cref="Map"/>.</value>
        public string RawMode
        {
            get
            {
                return (typeof(MapMode).GetMember(Name.ToString())[0] //an array of memberinfo, where the 0th is the one we want
                    .GetCustomAttributes(typeof(RawValueAttribute), false)[0] //an array of attributes, where the 1st is the one we want
                    as RawValueAttribute) //this is the attribute
                    .RawValue; //the raw value
            }
        }

        /// <summary>
        /// Gets the total rounds to be played for the <see cref="Map"/>.
        /// </summary>
        /// <value>The total number of rounds to be played on the <see cref="Map"/>.</value>
        public int TotalRounds { get; private set; }

        /// <summary>
        /// Gets the rounds that have been played so far on the <see cref="Map"/>.
        /// </summary>
        /// <value>The current round being played on the <see cref="Map"/>, or 0 if it's not being played.</value>
        public int CurrentRound { get; private set; }

        /// <summary>
        /// Create a new <see cref="Map"/> that can be added to <see cref="RconClient.Maps"/>.
        /// </summary>
        /// <param name="name">The name of the <see cref="Map"/>.</param>
        /// <param name="mode">The mode of the <see cref="Map"/>.</param>
        /// <param name="rounds">The total rounds to be played on the <see cref="Map"/>.</param>
        public Map(MapName name, MapMode mode, int rounds)
        {
            Name = name;
            Mode = mode;
            TotalRounds = rounds;
            CurrentRound = 0;
        }

        //used to match up internally-created Maps with their index in the very same mapcollection
        int _Index = -1;
        [NonSerialized()]MapCollection _MapCollection;
        internal int Index(MapCollection parent)
        {
            if (object.ReferenceEquals(_MapCollection, parent))
            {
                return _Index;
            }
            return -1;
        }

        internal Map(MapName name, MapMode mode, int rounds, int currentRound, int index, MapCollection maps) : this(name, mode, rounds)
        {
            CurrentRound = currentRound;
            _Index = index;
            _MapCollection = maps;
        }

        /// <summary>
        /// Specifies whether this <see cref="Map"/> is the same as the specified <see cref="Map"/>.
        /// </summary>
        /// <param name="other">The <see cref="Map"/> to test.</param>
        /// <returns>True if this <see cref="Map"/> is the same as the other <see cref="Map"/> by 
        /// <see cref="Name"/> and <see cref="Mode"/>.</returns>
        public bool Equals(Map other)
        {
            return Name == other.Name && Mode == other.Mode;
        }

        /// <summary>
        /// Specifies whether this <see cref="Map"/> is the same as the specified object.
        /// </summary>
        /// <param name="obj">The object to test.</param>
        /// <returns>True if this <see cref="Map"/> is the same as the object.</returns>
        public override bool Equals(object obj)
        {
            if (obj == null) return base.Equals(obj);

            Map m = obj as Map;
            if (m == null) return false;
            return Equals(m);
        }

        /// <summary>
        /// Returns the hash code of this <see cref="Map"/>.
        /// </summary>
        /// <returns>An integer value specifying the hash value for this <see cref="Map"/>.</returns>
        public override int GetHashCode()
        {
            return (RawName + RawMode).GetHashCode();
        }

        /// <summary>
        /// Converts the value of this instance to it's equivalent string representation <para />
        /// (<see cref="FriendlyName"/>, <see cref="FriendlyMode"/> <see cref="CurrentRound"/>/<see cref="TotalRounds"/>).
        /// </summary>
        /// <returns>The string representation of the <see cref="Player"/>.</returns>
        public override string ToString()
        {
            return "{0}, {1} {2}/{3}".Format2(FriendlyName, FriendlyMode, CurrentRound, TotalRounds);
        }
    }
}
