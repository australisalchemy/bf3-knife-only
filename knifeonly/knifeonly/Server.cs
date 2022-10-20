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
using System.Net.Battlefield3;
using System.Collections.ObjectModel;

namespace System.Net.Battlefield3
{
    /// <summary>
    /// Contains information relating to server settings.
    /// </summary>
    /// <remarks>
    /// Some of these properties have a value threshold, and some of these properties are ReadOnly when the server is ranked.<para />
    /// Some validation is done by the library, but it is best to consult the RCON protocol documentation.
    /// </remarks>
    public class Server
    {
        static Globalization.CultureInfo c = Globalization.CultureInfo.GetCultureInfo("en-US");

        RconClient Client;

        internal Server(RconClient client)
        {
            Client = client;
        }

        #region Helper Methods
        Packet GetServerInfo()
        {
            return Client.InternalSendRequest("serverInfo");
        }
        string GetServerInfo(int index)
        {
            Packet p = GetServerInfo();
            if (p.Success() && p.Words.Count > index)
                return p.Words[index];
            return null;
        }

        //this is used to get information after team scores, where 0 is target score (at least at the time of this comment)
        string GetServerInfo2(int index)
        {
            Packet p = GetServerInfo();
            if (p.Success())
            {
                int totalScores = p.Words[8].ToInt32();

                if (p.Words.Count > (index + 9 + totalScores)) //skips over the team scores
                    return p.Words[index];
            }
            return null;
        }

        //this is a quick way to parse vars.* getters that expect boolean values
        bool? GetVarsBool(string var)
        {
            string varResult = this[var];
            if (varResult == null) return null;

            bool result;
            if (bool.TryParse(varResult, out result)) return result;
            return null; //varResult probably an error value
        }

        //quick wway to set
        void SetVarsBool(string var, bool? value)
        {
            this[var] = (value ?? false).ToString(c);
        }

        //return -1 if fail
        int GetVarsInt(string var)
        {
            string varResult = this[var];
            if (varResult == null) return -1;

            int result;
            if (int.TryParse(varResult, out result)) return result;
            return -1;
        }

        void SetVarsInt(string var, int value)
        {
            this[var] = value.ToStringG();
        }

        double GetVarsDouble(string var)
        {
            string varResult = this[var];
            if (varResult == null) return -1;

            double result;
            if (double.TryParse(varResult, out result)) return result;
            return -1.0;
        }

        void SetVarsDouble(string var, double value)
        {
            this[var] = value.ToString(c);
        }
        #endregion

        /// <summary>
        /// Gets or sets the <see cref="Server">Server's</see> specified vars.* property.
        /// </summary>
        /// <param name="var">The property, such as friendlyFire, that will be used.</param>
        /// <returns>The value of the property or null (Nothing in Visual Basic) if the request fails.</returns>
        /// <remarks>
        /// This indexer is here for forwards compatibility. The use of <see cref="Server">Server's</see> built-in
        /// properties is recommended.
        /// </remarks>
        public string this[string var]
        {
            get
            {
                Packet p = Client.InternalSendRequest("vars.{0}".Format2(var));

                if (p.Success())
                {
                    return p.Words[1];
                }
                return null;
            }

            set
            {
                Client.InternalSendRequest("vars.{0}".Format2(var), value);
            }
        }
        #region serverInfo (and setters for vars.serverName and maxPlayers)
        //NOTE: if serverinfo and a vars.* contain the same information, serverinfo should be used to allow non-loggedin
        //clients to get the information.
        /// <summary>
        /// Gets or sets the <see cref="Server">Server's</see> name.
        /// </summary>
        /// <value>The <see cref="Server">Server's</see> name or null if the request fails.</value>
        public string ServerName
        {
            get
            {
                return GetServerInfo(1);
            }

            set
            {
                if (!value.HasValue()) throw new ArgumentNullException("value", "value must not be null or String.Empty.");
                this["serverName"] = value;
            }
        }

        /// <summary>
        /// Gets the number of <see cref="Player">Players</see> currently in the <see cref="Server"/>.
        /// </summary>
        /// <value>The number of <see cref="Player">Players</see> currently in the <see cref="Server"/>
        /// or -1 if the request failed.
        /// </value>
        public int PlayerCount
        {
            get
            {
                return GetServerInfo(2).ToInt32();
            }
        }

        /// <summary>
        /// Gets or sets the maximum number of <see cref="Player">Players</see> allowed in the <see cref="Server"/>.
        /// </summary>
        /// <value>
        /// The maximum number of <see cref="Player">Players</see> allowed in the <see cref="Server"/> or -1 if the
        /// request failed.
        /// </value>
        public int MaxPlayers
        {
            get
            {
                return GetServerInfo(3).ToInt32();
            }

            set
            {
                if (value < 1 || value > 64)
                    throw new ArgumentOutOfRangeException("value", "value must be greater than 0 and less than or equal to 64.");
                this["maxPlayers"] = value.ToStringG();
            }
        }

        //Map info is skipped because it's in MapCollection

        /// <summary>
        /// Gets the scores of each team ID. This uses a one-based index because team IDs start at one.
        /// </summary>
        /// <value>
        /// A <see cref="ReadOnlyCollection{T}" /> containing the team scores, where index 0 is not used (-1) and the
        /// other indices are the team IDs. Also returns null if the request failed.
        /// </value>
        public ReadOnlyCollection<int> TeamScores
        {
            get
            {
                Packet serverInfo = GetServerInfo();
                if (serverInfo == null) return null;

                int totalScores = serverInfo.Words[8].ToInt32();

                //will be 1-based because team IDs are 1-based
                List<int> scores = new List<int>(totalScores + 1);
                scores.Add(-1);

                for (int i = 0; i < totalScores; i++)
                    scores.Add((int)Convert.ToSingle(serverInfo.Words[9 + i], c)); //sometimes the score is a float

                return new ReadOnlyCollection<int>(scores);
            }
        }

        /// <summary>
        /// Gets the score that will cause the round to end when reached.
        /// </summary>
        /// <value>
        /// The score that will cause the round to end when reached or -1 if the request failed.
        /// </value>
        public int TargetScore
        {
            get
            {
                return GetServerInfo2(0).ToInt32();
            }
        }

        /// <summary>
        /// Gets the online state of the <see cref="Server"/>.
        /// </summary>
        /// <value>The online state of the <see cref="Server"/> or null if the request fails.</value>
        /// <remarks>
        /// This value is currently unknown and will always be <see cref="String.Empty"/> or null if the request fails.
        /// </remarks>
        public string OnlineState
        {
            get
            {
                return GetServerInfo2(1);
            }
        }

        /// <summary>
        /// Gets whether or not the server is ranked.
        /// </summary>
        /// <value>True if the server is ranked or null if the request fails.</value>
        public bool? Ranked
        {
            get
            {
                string ranked = GetServerInfo2(2);
                if (ranked == null) return null;
                return bool.Parse(ranked);
            }
        }

        /// <summary>
        /// Gets whether or not PunkBuster is enabled.
        /// </summary>
        /// <value>True if PunkBuster is enabled or null if the request fails.</value>
        public bool? PunkBusterEnabled
        {
            get
            {
                string pb = GetServerInfo2(3);
                if (pb == null) return null;
                return bool.Parse(pb);
            }
        }

        /// <summary>
        /// Gets whether or not there is a password on the <see cref="Server"/>.
        /// </summary>
        /// <value>True if there is a password on the <see cref="Server"/> or null if the request fails.</value>
        public bool? PasswordEnabled
        {
            get
            {
                string p = GetServerInfo2(4);
                if (p == null) return null;
                return bool.Parse(p);
            }
        }

        /// <summary>
        /// Gets the total amount of time the <see cref="Server"/> has been up.
        /// </summary>
        /// <value>The total amount of time the <see cref="Server"/> has been up or <see cref="TimeSpan.Zero"/>
        /// if the request fails.
        /// </value>
        public TimeSpan Uptime
        {
            get
            {
                int sec = GetServerInfo2(5).ToInt32();
                return sec == -1 ? TimeSpan.Zero : new TimeSpan(0, 0, sec);
            }
        }

        /// <summary>
        /// Gets the amount of time since the round has started.
        /// </summary>
        /// <value>The amount of time since the round has started or <see cref="TimeSpan.Zero"/>
        /// if the request fails.</value>
        public TimeSpan RoundTime
        {
            get
            {
                int sec = GetServerInfo2(6).ToInt32();
                return sec == -1 ? TimeSpan.Zero : new TimeSpan(0, 0, sec);
            }
        }

        /// <summary>
        /// Gets the IP:Port of the <see cref="Server"/>.
        /// </summary>
        /// <value>The IP:Port of the <see cref="Server"/> or null if the request fails.</value>
        public string ServerIP
        {
            get
            {
                return GetServerInfo2(7);
            }
        }

        /// <summary>
        /// Gets the version of PunkBuster.
        /// </summary>
        /// <value>The version of PunkBuster used or null if the request fails.</value>
        public string PunkBusterVersion
        {
            get
            {
                return GetServerInfo2(8);
            }
        }

        /// <summary>
        /// Gets whether or not the join queue is enabled.
        /// </summary>
        /// <value>True if the join queue is enabled or null if the request fails.</value>
        public bool? JoinQueueEnabled
        {
            get
            {
                string queue = GetServerInfo2(9);

                //do some checking because this field was blank when this code was written
                if (queue == null) return null;
                if (!queue.HasValue()) return true; //assume it's enabled because it can't be turned off atm anyway
                return bool.Parse(queue);
            }
        }

        /// <summary>
        /// Gets the region of the <see cref="Server"/>.
        /// </summary>
        /// <value>The region of the <see cref="Server"/> or null if the request fails.</value>
        public string Region
        {
            get
            {
                return GetServerInfo2(10);
            }
        }

        /// <summary>
        /// Gets the ping site nearest to the <see cref="Server"/>.
        /// </summary>
        /// <value>The ping site nearest to the <see cref="Server"/> or null if the request fails.</value>
        public string PingSite
        {
            get
            {
                return GetServerInfo2(11);
            }
        }

        /// <summary>
        /// Gets the country the <see cref="Server"/> is located in.
        /// </summary>
        /// <value>The country the <see cref="Server"/> is located in or null if the request fails.</value>
        public string Country
        {
            get
            {
                return GetServerInfo2(12);
            }
        }
        #endregion

        #region vars.*
        //serverName in serverinfo

        /// <summary>
        /// Gets the password of the <see cref="Server"/>.
        /// </summary>
        public string Password
        {
            get
            {
                //dont use indexer because this will fail on ranked servers not due to timeout
                Packet pw = Client.SendRequest("vars.gamePassword");

                if (pw == null) return null; //timeout
                if (!pw.Success()) return ""; //ranked, meaning no password
                return pw.Words[1];
            }
            //set not allowed outside of startup.txt
        }

        /// <summary>
        /// The same as <see cref="Password"/>. Gets the password of the <see cref="Server"/>.
        /// </summary>
        public string GamePassword
        {
            get { return Password; }
        }

        /// <summary>
        /// Gets or sets whether or not the <see cref="Server"/> will autobalance.
        /// </summary>
        public bool? AutoBalance
        {
            get { return GetVarsBool("autoBalance"); }

            set { SetVarsBool("autoBalance", value); }
        }

        /// <summary>
        /// Gets or sets whether friendly fire is enabled.
        /// </summary>
        public bool? FriendlyFire
        {
            get { return GetVarsBool("friendlyFire"); }

            set { SetVarsBool("friendlyFire", value); }
        }

        //maxplayers in serverinfo

        /// <summary>
        /// Gets or sets whether or not the killcam is enabled.
        /// </summary>
        public bool? KillCam
        {
            get { return GetVarsBool("killCam"); }

            set { SetVarsBool("killCam", value); }
        }

        /// <summary>
        /// Gets or sets whether or not the minimap is enabled.
        /// </summary>
        public bool? MiniMap
        {
            get
            {
                return GetVarsBool("miniMap");
            }

            set { SetVarsBool("miniMap", value); }
        }

        /// <summary>
        /// Gets or sets whether or not the HUD is enabled.
        /// </summary>
        public bool? Hud
        {
            get
            {
                return GetVarsBool("hud");
            }

            set { SetVarsBool("hud", value); }
        }

        /// <summary>
        /// Gets or sets whether or not crosshairs are enabled.
        /// </summary>
        public bool? Crosshair
        {
            get
            {
                return GetVarsBool("crossHair");
            }

            set { SetVarsBool("crossHair", value); }
        }

        /// <summary>
        /// Gets or sets whether or not spotted targets show up in the 3D world.
        /// </summary>
        public bool? WorldSpotting
        {
            get
            {
                return GetVarsBool("3dSpotting");
            }

            set { SetVarsBool("3dSpotting", value); }
        }

        /// <summary>
        /// Gets or sets whether or not spotted targets show up in the minimap.
        /// </summary>
        public bool? MiniMapSpotting
        {
            get { return GetVarsBool("miniMapSpotting"); }

            set { SetVarsBool("miniMapSpotting", value); }
        }

        /// <summary>
        /// Gets or sets whether or not nametags are shown.
        /// </summary>
        public bool? NameTag
        {
            get { return GetVarsBool("nameTag"); }

            set { SetVarsBool("nameTag", value); }
        }

        /// <summary>
        /// Gets or sets whether or not the 3rd person camera on vehicles is enabled.
        /// </summary>
        public bool? ThirdPersonCamera
        {
            get { return GetVarsBool("3pCam"); }

            set { SetVarsBool("3pCam", value); }
        }

        /// <summary>
        /// Gets or sets whether or not <see cref="Player">Players</see> regenerate health.
        /// </summary>
        public bool? RegenerateHealth
        {
            get { return GetVarsBool("regenerateHealth"); }

            set { SetVarsBool("regenerateHealth", value); }
        }

        /// <summary>
        /// Gets or sets how many team kills it takes for the offender to be kicked.
        /// </summary>
        public int TeamKillCountLimit
        {
            get { return GetVarsInt("teamKillCountForKick"); }

            set
            {
                if (value < 0) throw new ArgumentOutOfRangeException("value", "value must be greater than or equal to 0.");

                SetVarsInt("teamKillCountForKick", value);
            }
        }

        /// <summary>
        /// The same as <see cref="TeamKillCountLimit"/>. Gets or sets how many team kills it takes for the offender to be kicked.
        /// </summary>
        public int TeamKillCountForKick
        {
            get { return TeamKillCountLimit; }

            set
            {
                TeamKillCountLimit = value;
            }
        }

        /// <summary>
        /// Gets or sets the numeric limit to how much a <see cref="Player"/> can team kill before being kicked.
        /// </summary>
        public double TeamKillValueLimit
        {
            get { return GetVarsDouble("teamKillValueForKick"); }

            set
            {
                if (value < 0.0) throw new ArgumentOutOfRangeException("value", "value must be greater than or equal to 0.0.");

                SetVarsDouble("teamKillValueForKick", value);
            }
        }

        /// <summary>
        /// The same as <see cref="TeamKillValueLimit"/>. Gets or sets the numeric limit to how much a <see cref="Player"/> can team kill before being kicked.
        /// </summary>
        public double TeamKillValueForKick
        {
            get { return TeamKillValueLimit; }

            set { TeamKillValueLimit = value; }
        }

        /// <summary>
        /// Gets or sets how much the numeric value for team killing increases when a <see cref="Player"/> team kills.
        /// </summary>
        public double TeamKillValueIncrease
        {
            get { return GetVarsDouble("teamKillValueIncrease"); }

            set
            {
                if (value <= 0.0) throw new ArgumentOutOfRangeException("value", "value must be greater than 0.0.");

                SetVarsDouble("teamKillValueIncrease", value);
            }
        }

        /// <summary>
        /// Gets or sets how much the numeric value for team killing is decreased per second.
        /// </summary>
        public double TeamKillValueDecay
        {
            get { return GetVarsDouble("teamKillValueDecreasePerSecond"); }

            set
            {
                if (value <= 0.0) throw new ArgumentOutOfRangeException("value", "value must be greater than 0.0.");

                SetVarsDouble("teamKillValueDecreasePerSecond", value);
            }
        }

        /// <summary>
        /// The same as <see cref="TeamKillValueDecay"/>. Gets or sets how much the numeric value for team killing is decreased per second.
        /// </summary>
        public double TeamKillValueDecreasePerSecond
        {
            get { return TeamKillValueDecay; }

            set { TeamKillValueDecay = value; }
        }

        /// <summary>
        /// Gets or sets how many team kills result in kicks, per <see cref="Player"/>, before the <see cref="Player"/> is banned permanently.
        /// </summary>
        public int TeamKillKickLimit
        {
            get { return GetVarsInt("teamKillKickForBan"); }

            set
            {
                if (value < 0) throw new ArgumentOutOfRangeException("value", "value must be greater than or equal to 0.");

                SetVarsInt("teamKillKickForBan", value);
            }
        }

        /// <summary>
        /// The same as <see cref="TeamKillKickLimit"/>. Gets or sets how many team kills result in kicks, per <see cref="Player"/>, before the <see cref="Player"/> is banned permanently.
        /// </summary>
        public int TeamKillKickForBan
        {
            get { return TeamKillKickLimit; }

            set { TeamKillKickLimit = value; }
        }

        /// <summary>
        /// Gets or sets how many seconds a <see cref="Player"/> can be idle before being kicked.
        /// </summary>
        public int IdleTimeout
        {
            get { return GetVarsInt("idleTimeout"); }

            set
            {
                if (value < 0) throw new ArgumentOutOfRangeException("value", "value must be greater than or equal to 0.");

                SetVarsInt("idleTimeout", value);
            }
        }

        /// <summary>
        /// Gets or sets how many rounds a <see cref="Player"/> will be banned when kicked for being idle.
        /// </summary>
        public int IdleBanRounds
        {
            get { return GetVarsInt("idleBanRounds"); }

            set
            {
                if (value < 0) throw new ArgumentOutOfRangeException("value", "value must be greater than or equal to 0.");

                SetVarsInt("idleBanRounds", value);
            }
        }

        /// <summary>
        /// Gets or sets how many <see cref="Player">Players</see> are required for the game to start.
        /// </summary>
        public int StartCount
        {
            get { return GetVarsInt("roundStartPlayerCount"); }

            set
            {
                if (value < 1) throw new ArgumentOutOfRangeException("value", "value must be greater than 0.");

                SetVarsInt("roundStartPlayerCount", value);
            }
        }

        /// <summary>
        /// The same as <see cref="StartCount"/>. Gets or sets how many <see cref="Player">Players</see> are required for the game to start.
        /// </summary>
        public int RoundStartPlayerCount
        {
            get { return StartCount; }

            set { StartCount = value; }
        }

        /// <summary>
        /// Gets or sets the value for how many <see cref="Player">Players</see> are required to force a round restart.
        /// </summary>
        public int RestartCount
        {
            get { return GetVarsInt("roundRestartPlayerCount"); }

            set
            {
                if (value < 0) throw new ArgumentOutOfRangeException("value", "value must be greater than or equal to 0.");

                SetVarsInt("roundRestartPlayerCount", value);
            }
        }

        /// <summary>
        /// The same as <see cref="RestartCount"/>. Gets or sets the value for how many <see cref="Player">Players</see> are required to force a round restart.
        /// </summary>
        public int RoundRestartPlayerCount
        {
            get { return RestartCount; }

            set { RestartCount = value; }
        }

        /// <summary>
        /// Gets or sets whether or not vehicles are allowed to spawn.
        /// </summary>
        public bool? VehicleSpawnAllowed
        {
            get { return GetVarsBool("vehicleSpawnAllowed"); }

            set { SetVarsBool("vehicleSpawnAllowed", value); }
        }

        /// <summary>
        /// Gets or sets how long it takes for vehicles to spawn, in percent of the normal time.
        /// </summary>
        public int VehicleSpawnDelay
        {
            get { return GetVarsInt("vehicleSpawnDelay"); }

            set
            {
                if (value < 0 || value > 100)
                    throw new ArgumentOutOfRangeException("value",
                    "value must be greater than or equal to 0 and less than or equal to 100.");

                SetVarsInt("vehicleSpawnDelay", value);
            }
        }

        /// <summary>
        /// Gets or sets how much health a <see cref="Player"/> has, in percent of the normal health.
        /// </summary>
        public int SoldierHealth
        {
            get { return GetVarsInt("soldierHealth"); }

            set
            {
                //assuming range for unranked
                if (value < 1 || value > 100)
                    throw new ArgumentOutOfRangeException("value", "value must be greater than 0 and less than or equal to 100.");

                SetVarsInt("soldierHealth", value);
            }
        }

        /// <summary>
        /// Gets or sets how long it takes <see cref="Player">Players</see> to respawn, in percent of the normal time.
        /// </summary>
        public int PlayerRespawnTime
        {
            get { return GetVarsInt("playerRespawnTime"); }

            set
            {
                if (value < 0 || value > 100)
                    throw new ArgumentOutOfRangeException("value",
                    "value must be greater than or equal to 0 and less than or equal to 100.");

                SetVarsInt("playerRespawnTime", value);
            }
        }

        /// <summary>
        /// Gets or sets how long a revived <see cref="Player"/> has until death, in percent of the normal time.
        /// </summary>
        public int PlayerManDownTime
        {
            get { return GetVarsInt("playerManDownTime"); }

            set
            {
                //not sure about range on unranked
                if (value < 0 || value > 100)
                    throw new ArgumentOutOfRangeException("value",
                    "value must be greater than or equal to 0 and less than or equal to 100.");

                SetVarsInt("playerManDownTime", value);
            }
        }

        /// <summary>
        /// Gets or sets how much damage bullets do, in percent of the normal damage.
        /// </summary>
        public int BulletDamage
        {
            get { return GetVarsInt("bulletDamage"); }

            set
            {
                //assuming range for unranked
                if (value < 1 || value > 100)
                    throw new ArgumentOutOfRangeException("value", "value must be greater than 0 and less than or equal to 100.");

                SetVarsInt("bulletDamage", value);
            }
        }

        /// <summary>
        /// Gets or sets the number of tickets per round, in percent of the normal tickets.
        /// </summary>
        public int TicketCount
        {
            get { return GetVarsInt("gameModeCounter"); }

            set
            {
                //assuming min; didnt test for max
                if (value < 1)
                    throw new ArgumentOutOfRangeException("value", "value must be greater than 0.");

                SetVarsInt("gameModeCounter", value);
            }
        }

        /// <summary>
        /// The same as <see cref="TicketCount"/>. Gets or sets the number of tickets per round, in percent of the normal tickets.
        /// </summary>
        public int GameModeCounter
        {
            get { return TicketCount; }

            set { TicketCount = value; }
        }

        /// <summary>
        /// Gets or sets whether or not <see cref="Player">Players</see> can only spawn on the squad leader, not other members.
        /// </summary>
        public bool? OnlySquadLeaderSpawn
        {
            get { return GetVarsBool("onlySquadLeaderSpawn"); }

            set { SetVarsBool("onlySquadLeaderSpawn", value); }
        }

        /// <summary>
        /// Gets or sets whether all unlocks are available to <see cref="Player">Players</see>.
        /// </summary>
        public bool? Unlocks
        {
            get { return GetVarsBool("allUnlocksUnlocked"); }

            set { SetVarsBool("allUnlocksUnlocked", value); }
        }

        /// <summary>
        /// The same as <see cref="Unlocks"/>. Gets or sets whether all unlocks are available to <see cref="Player">Players</see>.
        /// </summary>
        public bool? AllUnlocksUnlocked
        {
            get { return Unlocks; }

            set { Unlocks = value; }
        }
        #endregion
    }
}
