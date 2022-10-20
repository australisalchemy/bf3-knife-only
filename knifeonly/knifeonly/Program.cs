using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Battlefield3;
using System.IO;
using System.ComponentModel;
using System.Diagnostics;

/* 
	Knife Only 1.3 TODO List:

	Change the formatting of when a banned weapon is used <-- FIXED
    Program breaks when it tries to reconnect, just hangs. Allow it to crash, for it to reopen! <-- FIXED
    
    
    
*/

namespace knifeonly
{
    class Program
    {
        #region Variables

        public static RconClient rcCli = new RconClient();
        public static List<Player> playerList = new List<Player>();
        public static string mapName;
        private static string password;
        public static List<Player> pList = new List<Player>();
        public static Dictionary<Player, int> pDictionary = new Dictionary<Player, int>();
        public static string bitLength = string.Empty;

        #endregion

        #region LogType Enum

        enum LogType
        {
            BAN,
            KICK,
            ROUND,
            OTHER,
            KILL,
            CHAT,
            ERROR
        }

        #endregion

        static void Main(string[] args)
        {
            try
            {
                Console.Title = "RoflCorp Knife Only - v1.2 - Battlefield 3 - For use on RoflCorp Battlefield 3 servers only. "+ bitLength;
                Console.WriteLine("RoflCorp Knife Only for Battlefield 3." + bitLength);
                Console.WriteLine("Copyright (c) RoflCorp 2011. All rights reserved.");
                Console.WriteLine("For use on RoflCorp Battlefield 3 servers only.\n");
                Console.WriteLine("Announcing Knife Mod...\n");
                rcCli.SendRequest("admin.say", "RoflCorp Knife Only v1.2 started!", "all");
                #region Arguments
                try
                {
                    serverPassword = args[0]; // password goes in the args
                }
                catch
                {
                    Console.WriteLine("Invalid or no password set. Please make a shortcut and add the server password at the end of the executable path.");
                }
                #endregion
                rcCli.Address = "203.46.105.24";            // hard coded address
                rcCli.Port = 22155;                         // hard coded port
                rcCli.Connect();
                rcCli.Connected += new EventHandler(rcCli_Connected);
                rcCli.ConnectError += new EventHandler<ConnectErrorEventArgs>(rcCli_ConnectError);
                rcCli.PlayerKilled += new EventHandler<PlayerKilledEventArgs>(rcCli_PlayerKilled);
                rcCli.Disconnected += new EventHandler<DisconnectedEventArgs>(rcCli_Disconnected);
                rcCli.PlayerChat += new EventHandler<PlayerChatEventArgs>(rcCli_PlayerChat);
                rcCli.LevelLoaded += new EventHandler<LevelLoadedEventArgs>(rcCli_LevelLoaded);
                rcCli.RoundOver += new EventHandler(rcCli_RoundOver);
                rcCli.PlayerLeft += new EventHandler<PlayerEventArgs>(rcCli_PlayerLeft);
                rcCli.PingTimeout = 60;     // 60 minutes before timeout
                Console.Read();
            }
            catch
            {
                Console.WriteLine(getTime() + "Please restart the program!");
            }
        }

        #region Event Handlers
        static void rcCli_PlayerLeft(object sender, PlayerEventArgs e)
        {
            try
            {
                Console.WriteLine(getTime() + e.Player.Name + " left the server.");
            }
            catch
            {
                Console.WriteLine(getTime() + "error! PlayLeft() event..");
            }
        }

        static void rcCli_RoundOver(object sender, EventArgs e)
        {
            Console.WriteLine("The round is over! Starting a new one...");
            onRoundEnd(); // list all the players and their scores.
        }

        static void rcCli_LevelLoaded(object sender, LevelLoadedEventArgs e)
        {
            #region Map names
            if (e.Level == "MP_001")
            {
                mapName = "Grand Bazaar";
            }
            else if (e.Level == "MP_003")
            {
                mapName = "Tehran Highway";
            }
            else if (e.Level == "MP_007")
            {
                mapName = "Caspian Border";
            }
            else if (e.Level == "MP_011")
            {
                mapName = "Seine Crossing";
            }
            else if (e.Level == "MP_012")
            {
                mapName = "Operation Firestorm";
            }
            else if (e.Level == "MP_013")
            {
                mapName = "Damavand Peak";
            }
            else if (e.Level == "MP_018")
            {
                mapName = "Kharg Island";
            }
            else if (e.Level == "MP_Subway")
            {
                mapName = "Operation Métro";
            }
            else
            {
                mapName = "Unknown";
            }
            #endregion

            Console.WriteLine("Server changed map to [" + mapName + "] Game mode: [" + e.GameMode + "]");
        }

        static void rcCli_PlayerChat(object sender, PlayerChatEventArgs e)
        {
            try
            {
                if (e.Source == "Server")
                {
                    Console.WriteLine(getTime() + "[Server]" + e.Player + " > " + e.Message.ToString());
                    //Log(getTime(), LogType.CHAT, "Admin ", e.Message, "");
                }
                else
                {
                    Console.WriteLine(getTime() + "[" + e.Player + "]" + " > " + e.Message.ToString());
                    //Log(getTime(), LogType.CHAT, e.Player.Name, e.Message, "");
                }
            }
            catch
            {
                Console.WriteLine("PlayerChat event failed!");
            }
        }

        static void rcCli_Disconnected(object sender, DisconnectedEventArgs e)
        {
            Console.WriteLine("Server Disconnected!... Closing App");
            Log(getTime(), LogType.ERROR, "Server Connection Lost", " Closed App!", "");
            Environment.Exit(0);    // close app
        }

        static void rcCli_PlayerKilled(object sender, PlayerKilledEventArgs e)
        {
            string isHeadshot;
            int sayCount = 0;
            sayCount = sayCount + 1;
            #region ifStatement
            if (e.Headshot == true)
            {
                isHeadshot = "[Headshot]";
            }
            else
            {
                isHeadshot = "";
            }
            #endregion

            try
            {
                if (e.Weapon.Contains("Knife") || e.Weapon.Contains("Melee") || e.Weapon.Contains("Suicide") || e.Weapon.Contains("Defib") || e.Weapon.Contains("Killed") || e.Weapon.Contains("SoldierCollision") || e.Weapon.Contains("Repair Tool"))
                {
                    if (e.Weapon.Contains("Suicide"))
                    {
                        Console.WriteLine(getTime() + "[" + e.Victim.Name + "]" + " died.");
                    }
                    //Log(getTime(), LogType.KILL, "[" + e.Attacker.Name + "]", "[" + e.Victim.Name + "]", e.Weapon);
                }
                else
                {
                    if (e.Attacker.Name == e.Victim.Name)
                    {
                        Console.WriteLine(getTime() + e.Victim.Name + " died. " + e.Weapon);
                    }
                    else
                    {
                        // nothing!
                    }
                    // punish the player
                    punishPlayer(e.Attacker, e.Weapon, 1);
                }
                Console.WriteLine(getTime() + "[" +e.Attacker.Name+ "]" + " killed " + "[" + e.Victim.Name + "]" + " with [" + e.Weapon + "] " + isHeadshot);
            }
            catch (Exception ex)
            {
                //Console.WriteLine("There seems to be an error! Please send r3df411 the 'debug.txt' file.\n");
                Log(getTime(), LogType.ERROR, ex.Source, ex.Message, "");
                //Console.WriteLine(ex.Message);
            }
        }

        static void rcCli_ConnectError(object sender, ConnectErrorEventArgs e)
        {
            Console.WriteLine("Server not on! Closing app!");
            Environment.Exit(0);        // exit app
        }

        static void rcCli_Connected(object sender, EventArgs e)
        {
            try
            {
                rcCli.LogOn(serverPassword, true);
            }
            catch
            {
                Console.WriteLine("Invalid or no password set. Please make a shortcut and add the server password at the end of the executable path.");
            }
        }

        #endregion

        static void Log(string logTime, LogType lType, string information1, string information2, string information3)
        {
            try
            {
                string filename = string.Empty;
                if (lType == LogType.BAN)
                {
                    filename = "banlist.txt";
                }
                else if (lType == LogType.KICK)
                {
                    filename = "kicklist.txt";
                }
                else if (lType == LogType.ERROR)
                {
                    filename = "debug.txt";
                }
                else if (lType == LogType.CHAT)
                {
                    filename = "chatlist.txt";
                }
                else if (lType == LogType.KILL)
                {
                    filename = "killslist.txt";
                }
                else if (lType == LogType.ROUND)
                {
                    filename = "roundlist.txt";
                }
                else
                {
                    filename = "knifeonly.txt";
                }
                StreamWriter sw = new StreamWriter(filename, true);

                if (lType == LogType.KICK)
                {
                    try
                    {
                        // TIME \t TYPE \t PLAYER \t REASON
                        sw.WriteLine(getDate() + logTime + "\t" + lType.ToString() + "\t" + information1 + "\t" + information2 + "\n");
                    }
                    catch
                    {
                        Console.WriteLine(logTime + "couldn't write the file. Maybe it is in use?");
                    }
                    finally
                    {
                        if (sw != null)
                        {
                            sw.Close();
                        }
                    }
                }
                else if (lType == LogType.BAN)
                {
                    try
                    {
                        // TIME \t TYPE \t PLAYER \t WEAPON
                        sw.WriteLine(getDate() + logTime + "\t" + lType.ToString() + "\t" + information1 + "\t" + information2 + "\n");
                    }
                    catch
                    {
                        Console.WriteLine(getTime() + "couldn't write the file. Maybe it is in use?");
                    }
                    finally
                    {
                        if (sw != null)
                        {
                            sw.Close();
                        }
                    }
                }
                else if (lType == LogType.ROUND)
                {
                    try{
                                        // TIME \t TYPE \t PLAYER LIST \t MAP
                        sw.WriteLine(getDate() + logTime + "\t" + lType.ToString() + "\t" + information1 + "\t\t" + information2 + "\n");
                    }
                    catch
                    {
                        Console.WriteLine(logTime + "couldn't write the file. Maybe it is in use?");
                    }
                    finally
                    {
                        if (sw != null)
                        {
                            sw.Close();
                        }
                    }
                }
                else if (lType == LogType.KILL)
                {
                    try
                    {
                        // TIME \t TYPE \t ATTACKER killed VICTIM with WEAPON
                        sw.WriteLine(getDate() + logTime + "\t" + lType.ToString() + "\t" + information1 + " killed " + information2 + " with " + information3);
                    }
                    catch
                    {
                        Console.WriteLine(logTime + "couldn't write the file. Maybe it is in use?");
                    }
                    finally
                    {
                        if (sw != null)
                        {
                            sw.Close();
                        }
                    }
                }
                else if (lType == LogType.CHAT)
                {
                    try
                    {
                        // TIME \t TYPE \t SOURCE \t MESSAGE
                        sw.WriteLine(getDate() + logTime + "\t" + lType.ToString() + "\t" + information1 + " said: " + information2 + "\n");
                    }
                    catch
                    {
                        Console.WriteLine(getTime() + "couldn't write the file. Maybe it is in use?");
                    }
                    finally
                    {
                        if (sw != null)
                        {
                            sw.Close();
                        }
                    }
                }
                else if (lType == LogType.ERROR)
                {
                    try
                    {
                        // TIME \t TYPE \t EXCEPTION \t MESSAGE
                        sw.WriteLine(getDate() + logTime + "\t" + lType.ToString() + "\t" + information1 + " said " + information2 + "\n");
                    }
                    catch
                    {
                        Console.WriteLine(getTime() + "couldn't write the file. Maybe it is in use?");
                    }
                    finally
                    {
                        if (sw != null)
                        {
                            sw.Close();
                        }
                    }
                }
                else
                {
                    sw.WriteLine(getDate() + logTime + "Nothing was set.");
                }
            }
            catch
            {
                Console.WriteLine(getTime() + "couldn't write the file. Maybe it is in use?");
            }
        }

        static string serverPassword
        {
            get { return password; }
            set
            {
                password = value;
            }
        }

        static string getTime()
        {
            return DateTime.Now.ToString("HH:mm:ss ");
        }

        static string getDate()
        {
            return DateTime.Now.ToString("dd-mm-yyyy ");
        }

        #region Punish Players
        public static void kickPlayer(Player p, string weapon)
        {
            try
            {
                p.Kick("No shooting.");
                Console.WriteLine(getTime() + "[" + p.Name + "]" + " used " + weapon + " Kicking: " + p.Name);
                Log(getTime(), LogType.KICK, "[" + p.Name + "]" + " used ", weapon, "");
            }
            catch
            {
                Console.WriteLine(getTime() + "kickPlayer() failed!");
            }
        }

        public static void banPlayer(Player p, string weapon, int rounds)
        {
            try
            {
                p.RoundBan(1);
                Console.WriteLine(getTime() + "[" + p.Name + "]" + " used " + weapon + " Banning: " + p.Name);
                Log(getTime(), LogType.BAN, "[" + p.Name + "]" + " used ", weapon, "");

            }
            catch
            {
                Console.WriteLine(getTime() + "banPlayer() failed!");
            }
        }

        public static void punishPlayer(Player p, string weapon, int kickTimes)
        {
            try
            {
                pList.Add(p);

                // add a player to the dictionary
                foreach (Player pName in pList)
                {
                    if (pDictionary.ContainsKey(pName))        // if the list contains the same person twice, ban the player for a round. THEN clear the persons name :)
                    {
                        rcCli.SendRequest("admin.say", pName + " has been banned until the end of the round for using an unallowed weapon ", "all");
                        // DUPLICATE FOUND!
                        banPlayer(pName, weapon, 1);
                        //clear persons name
                        pDictionary.Remove(pName);  // banning until end of round! might aswell remove them :)
                    }
                    else
                    {
                        if (kickTimes == 1)
                        {
                            pDictionary.Add(pName, value: 0);
                            kickPlayer(p, weapon); // kick the player
                            rcCli.SendRequest("admin.say", pName + " has been kicked for using an unallowed weapon ", "all");
                        }
                        else
                        {
                            Console.WriteLine(getTime() + "you should ban " + pName + " manually!");
                        }
                    }
                }
                pList.Remove(p); // remove the player from the list
            }
            catch
            {
                Console.WriteLine(getTime() + " punishPlayer() failed!");
            }
        }
        #endregion

        #region Server Information

        static void onRoundEnd()
        {
            Console.WriteLine("Round over! Players were:\n");

            foreach (Player p in rcCli.Players)
            {
                Console.WriteLine("\n" + p.Name + "\t" + p.Score);
                Log(getTime(), LogType.ROUND, p.Name, p.Score.ToString() + "\n\n", " ENDGAME");
            }
        }

        #endregion
    }
}

