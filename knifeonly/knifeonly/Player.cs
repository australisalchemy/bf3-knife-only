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
using System.Globalization;

namespace System.Net.Battlefield3
{
    /// <summary>
    /// Contains data regarding players in a Battlefield 3 server.
    /// </summary>
    public class Player : MarshalByRefObject, IEquatable<Player>
    {
        RconClient _Parent;

        //note that this dictionary will constantly be reassigned on each listPlayers
        //to ensure accuracy and update things such as score and ping
        internal Dictionary<string, string> Properties;

        //this is used for the tostrings and such to satisfy fxcop
        static CultureInfo c = CultureInfo.GetCultureInfo("en-US");

        // the rconclient will supply itself and the properties listed by a listPlayers
        internal Player(RconClient parent, Dictionary<string,string> properties)
        {
            this._Parent = parent;
            this.Properties = properties;
        }

        /// <summary>
        /// Gets the parent <see cref="RconClient"/> of the <see cref="Player"/>.
        /// </summary>
        /// <value>The parent <see cref="RconClient"/> of the <see cref="Player"/>.</value>
        public RconClient Parent { get { return _Parent; } }

        /// <summary>
        /// Gets a <see cref="Player"/> property as defined in the RCON documentation.
        /// </summary>
        /// <param name="property">The name of the property.</param>
        /// <returns>The property as a string.</returns>
        /// <remarks>
        /// The casing of <paramref name="property"/> must match the casing in the RCON documentation.
        /// </remarks>
        public string this[string property]
        {
            get
            {
                if (!Properties.ContainsKey(property)) return null;
                return Properties[property];
            }
        }

        
#region Properties
        /// <summary>
        /// Gets the name of the <see cref="Player"/>.
        /// </summary>
        /// <value>A string containing the name of the <see cref="Player"/>.</value>
        public string Name { get { return this["name"]; } }

        /// <summary>
        /// Gets the GUID of the <see cref="Player"/>.
        /// </summary>
        /// <value>A string containing the GUID of the <see cref="Player"/>.</value>
        public string Guid
        {
            get { return this["guid"]; }
        }

        /// <summary>
        /// Gets the team of the <see cref="Player"/>.
        /// </summary>
        /// <value>An integer containing the team of the <see cref="Player"/>.</value>
        public int TeamId
        {
            get { return this["teamId"].ToInt32(); }
        }

        /// <summary>
        /// Gets the squad of the <see cref="Player"/>.
        /// </summary>
        /// <value>An integer containing the squad of the <see cref="Player"/>.</value>
        public int SquadId
        {
            get { return this["squadId"].ToInt32(); }
        }

        /// <summary>
        /// Gets the number of kills of the <see cref="Player"/>.
        /// </summary>
        /// <value>An integer containing the number of kills of the <see cref="Player"/>.</value>
        public int Kills
        {
            get { return this["kills"].ToInt32(); }
        }

        /// <summary>
        /// Gets the number of deaths of the <see cref="Player"/>.
        /// </summary>
        /// <value>An integer containing the number of deaths of the <see cref="Player"/>.</value>
        public int Deaths
        {
            get { return this["deaths"].ToInt32(); }
        }

        /// <summary>
        /// Gets the score of the <see cref="Player"/>.
        /// </summary>
        /// <value>An integer containing the score of the player.</value>
        public int Score { get { return Convert.ToInt32(this["score"], c); } }

        ///// <summary>
        ///// The ping of the <see cref="Player"/>.
        ///// </summary>
        ///// <value>An integer containing the ping of the player.</value>
        //public int Ping { get { return Convert.ToInt32(this["ping"], c); } }

        /// <summary>
        /// Gets the IP address of the <see cref="Player"/>, if available.
        /// </summary>
        /// <value>The IP address of the <see cref="Player"/> or <see cref="String.Empty"/> if not available.</value>
        /// <remarks>
        /// The IP address is parsed from PunkBuster, rather than the player list, so it is possible the IP address
        /// won't be available upon request.
        /// </remarks>
        public string IPAddress
        {
            get
            {
                string ip;
                if (Parent.PunkBusterIPDictionary.TryGetValue(Name, out ip)) return ip;
                return string.Empty;
            }
        }
#endregion

#region Methods
        /// <summary>
        /// Changes the squad and team of the <see cref="Player"/>.
        /// </summary>
        /// <param name="team">The new team of the <see cref="Player"/>.</param>
        /// <param name="squad">The new squad of the <see cref="Player"/>.</param>
        /// <param name="forceKill">If true, forces the <see cref="Player"/> to die before switching.</param>
        /// <remarks>
        /// This method will fail if <paramref name="forceKill"/> is false and the <see cref="Player"/> is alive.
        /// </remarks>
        public void ChangeSquad(int team, int squad, bool forceKill)
        {
            _Parent.SendAsynchronousRequest("admin.movePlayer", Name, team.ToStringG(), squad.ToStringG(), forceKill.ToString(c));
        }

        /// <summary>
        /// Changes the squad and team of the <see cref="Player"/>.
        /// </summary>
        /// <param name="team">The new team of the <see cref="Player"/>.</param>
        /// <param name="squad">The new squad of the <see cref="Player"/>.</param>
        /// <remarks>
        /// This method will kill the <see cref="Player"/> to ensure success.
        /// </remarks>
        public void ChangeSquad(int team, int squad)
        { ChangeSquad(team, squad, true); }

        /// <summary>
        /// Changes the squad of the <see cref="Player"/>.
        /// </summary>
        /// <param name="squad">The new squad of the <see cref="Player"/>.</param>
        /// <param name="forceKill">If true, forces the <see cref="Player"/> to die before switching.</param>
        /// <remarks>
        /// This method will fail if <paramref name="forceKill"/> is false and the <see cref="Player"/> is alive.
        /// </remarks>
        public void ChangeSquad(int squad, bool forceKill)
        { ChangeSquad(TeamId, squad, forceKill); }

        /// <summary>
        /// Changes the squad of the <see cref="Player"/>.
        /// </summary>
        /// <param name="squad">The new squad of the <see cref="Player"/>.</param>
        /// <remarks>
        /// This method will kill the <see cref="Player"/> to ensure success.
        /// </remarks>
        public void ChangeSquad(int squad)
        { ChangeSquad(squad, true); }

        /// <summary>
        /// Changes the team of the <see cref="Player"/>.
        /// </summary>
        /// <param name="team">The new team of the <see cref="Player"/>.</param>
        /// <param name="forceKill">If true, forces the <see cref="Player"/> to die before switching.</param>
        /// <remarks>
        /// This method will fail if <paramref name="forceKill"/> is false and the <see cref="Player"/> is alive.
        /// </remarks>
        public void ChangeTeam(int team, bool forceKill)
        { ChangeSquad(team, 0, forceKill); }

        /// <summary>
        /// Changes the team of the <see cref="Player"/>.
        /// </summary>
        /// <param name="team">The new teams of the <see cref="Player"/>.</param>
        /// <remarks>
        /// This method will kill the <see cref="Player"/> to ensure success.
        /// </remarks>
        public void ChangeTeam(int team)
        { ChangeTeam(team, true); }

        /// <summary>
        /// Kills the <see cref="Player"/>.
        /// </summary>
        public void Kill()
        {
            _Parent.SendAsynchronousRequest("admin.killPlayer", Name);
        }

        /// <summary>
        /// Kicks the <see cref="Player"/>.
        /// </summary>
        public void Kick()
        {
            _Parent.SendAsynchronousRequest("admin.kickPlayer", Name);
        }

        /// <summary>
        /// Kicks the <see cref="Player"/> with the specified <paramref name="reason"/>.
        /// </summary>
        /// <param name="reason">The reason to kick the <see cref="Player"/>.</param>
        public void Kick(string reason)
        {
            _Parent.SendAsynchronousRequest("admin.kickPlayer", Name, reason);
        }

        /// <summary>
        /// Bans the <see cref="Player"/> permanently.
        /// </summary>
        public void PermanentBan()
        {
            _Parent.SendAsynchronousRequest("banList.add", "name", Name, "perm");
        }

        /// <summary>
        /// Bans the <see cref="Player"/> permanently by the specified <see cref="BanTargetType"/>.
        /// </summary>
        /// <param name="target">What part of the <see cref="Player"/> will be banned.</param>
        /// <remarks>
        /// The IP address of a <see cref="Player"/> will only be available when PunkBuster sends it.
        /// </remarks>
        public void PermanentBan(BanTargetType target)
        {
            string[] t = TargetToString(target, this);
            _Parent.SendAsynchronousRequest("banList.add", t[0], t[1], "perm");
        }

        /// <summary>
        /// Bans the <see cref="Player"/> for a round.
        /// </summary>
        /// <param name="rounds">The number of rounds to ban the <see cref="Player"/>.</param>
        public void RoundBan(int rounds)
        {
            _Parent.SendAsynchronousRequest("banList.add", "name", Name, "rounds", rounds.ToStringG());
        }

        /// <summary>
        /// Bans the <see cref="Player"/> for a round by the specified <see cref="BanTargetType"/>.
        /// </summary>
        /// <param name="rounds">The number of rounds to ban the <see cref="Player"/>.</param>
        /// <param name="target">What part of the <see cref="Player"/> will be banned.</param>
        /// <remarks>
        /// The IP address of a <see cref="Player"/> will only be available when PunkBuster sends it.
        /// </remarks>
        public void RoundBan(int rounds, BanTargetType target)
        {            
            string[] t = TargetToString(target, this);
            _Parent.SendAsynchronousRequest("banList.add", t[0], t[1], "rounds", rounds.ToStringG());
        }

        /// <summary>
        /// Temporarily bans the <see cref="Player"/> for the specified number of seconds.
        /// </summary>
        /// <param name="seconds">The number of seconds the <see cref="Player"/> will be banned.</param>
        public void TemporaryBan(int seconds)
        {
            _Parent.SendAsynchronousRequest("banList.add", "name", Name, "seconds", seconds.ToStringG());
        }

        /// <summary>
        /// Temporarily bans the <see cref="Player"/> for the specified number of seconds.
        /// </summary>
        /// <param name="target">What part of the <see cref="Player"/> will be banned.</param>
        /// <param name="seconds">The number of seconds the <see cref="Player"/> will be banned.</param>
        /// <remarks>
        /// The IP address of a <see cref="Player"/> will only be available when PunkBuster sends it.
        /// </remarks>
        public void TemporaryBan(BanTargetType target, int seconds)
        {
            if (target == BanTargetType.IPAddress) throw new NotImplementedException("IP banning is not yet supported using these Player commands.");

            string[] t = TargetToString(target, this);
            _Parent.SendAsynchronousRequest("banList.add", t[0], t[1], "seconds", seconds.ToStringG());
        }

        //this returns the target type as a string and the value of the target as the type
        static string[] TargetToString(BanTargetType target, Player player)
        {
            if (target == BanTargetType.Guid) return new string[2] { "guid", player.Guid };
            if (target == BanTargetType.IPAddress) return new string[2] { "ip", player.IPAddress };
            if (target == BanTargetType.Name) return new string[2] { "name", player.Name };
            return new string[0];
        }
#endregion

        /// <summary>
        /// Converts the value of this instance to it's equivalent string representation (<see cref="Name"/>).
        /// </summary>
        /// <returns>The <see cref="Name"/> of the <see cref="Player"/>.</returns>
        public override string ToString()
        {
            return Name;
        }

        /// <summary>
        /// Returns the hash code of this <see cref="Player"/>.
        /// </summary>
        /// <returns>An integer value specifying the hash value for this <see cref="Player"/>.</returns>
        public override int GetHashCode()
        {
            return Name.GetHashCode();
        }

        /// <summary>
        /// Specifies whether this <see cref="Player"/> is the same as the specified object.
        /// </summary>
        /// <param name="obj">The object to test.</param>
        /// <returns>True if this <see cref="Player"/> is the same as the object.</returns>
        public override bool Equals(object obj)
        {
            if (obj == null) return base.Equals(obj);

            Player p = obj as Player;
            if (p == null) return false;
            return Equals(p);
        }

        /// <summary>
        /// Compares two <see cref="Player"/> objects.
        /// </summary>
        /// <param name="firstPlayer">The first <see cref="Player"/>.</param>
        /// <param name="secondPlayer">The second <see cref="Player"/>.</param>
        /// <returns>True if both players have the same name.</returns>
        public static bool operator ==(Player firstPlayer, Player secondPlayer)
        {
            return (firstPlayer as object == null || secondPlayer as object == null) ? object.Equals(firstPlayer, secondPlayer) : firstPlayer.Equals(secondPlayer);
        }

        /// <summary>
        /// Compares two <see cref="Player"/> objects.
        /// </summary>
        /// <param name="firstPlayer">The first <see cref="Player"/>.</param>
        /// <param name="secondPlayer">The second <see cref="Player"/>.</param>
        /// <returns>True if both players do not have the same name.</returns>
        public static bool operator !=(Player firstPlayer, Player secondPlayer)
        {
            return !(firstPlayer == secondPlayer);
        }

        #region IEquatable<Player> Members

        /// <summary>
        /// Specifies whether this <see cref="Player"/> is the same as the specified <see cref="Player"/>.
        /// </summary>
        /// <param name="other">The <see cref="Player"/> to test.</param>
        /// <returns>True if this <see cref="Player"/> is the same as the other <see cref="Player"/>.</returns>
        public bool Equals(Player other)
        {
            return other != null && other.Name == Name;
        }

        #endregion
    }
}
