using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Timers;
using System.IO;

namespace KOLaunch
{
    class Program
    {
        public static Timer pCheck = new Timer();
        public static StreamWriter sw = new StreamWriter("KOLog.txt", true);
        public static int checkinTimes = 0;
        public static long checkTimes = 0; // int was too small
        protected static Process p = new Process();
        public static Timer pRuntime = new Timer();
        public static long ticks = 0;                   
        public static long minutes = 0;
        public static long hours = 0;
        public static long days = 0;
        public static long time = 0;
        public static string totalTime = string.Empty;
        public static string bitLength = string.Empty; 

        static void Main(string[] args)
        {
            #region Process Architecture

            if (IntPtr.Size == 4)
            {
                bitLength = "X86";
            }
            else if (IntPtr.Size == 8)
            {
                bitLength = "X64";
            }
            else
            {
                bitLength = "X128";
            }

            #endregion

            pCheck.Elapsed += new ElapsedEventHandler(pCheck_Elapsed);
            pRuntime.Elapsed += new ElapsedEventHandler(pRuntime_Elapsed);
            Console.Title = "Knife Only Launcher - v1.0 - For use with RoflCorp Battlefield 3 servers only.";
            Console.WriteLine("RoflCorp Knife Only Launcher for Knife Only. " + bitLength);
            Console.WriteLine("Copyright (c) RoflCorp 2011. All rights reserved.");
            Console.WriteLine("For use on RoflCorp Battlefield 3 servers only.\n");

            p.EnableRaisingEvents = true;
            p.Exited += new EventHandler(p_Exited);
            p.ErrorDataReceived += new DataReceivedEventHandler(p_ErrorDataReceived);

            // Process timer
            pCheck.Interval = 20000; // every 20 seconds
            pCheck.Enabled = true;
            pCheck.Start();

            // Uptime counter
            pRuntime.Interval = 1000; // every second
            pRuntime.Enabled = true;
            pRuntime.Start();

            try
            {
                p.StartInfo.Arguments = args[0];
                p.StartInfo.FileName = "knifeonly_" + bitLength + ".exe";   // run either X86 (if this is X86) or X64 (if this is X64)
                p.Start(); // start the process
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }


            Console.WriteLine("Knife Only Started at: " + DateTime.Now.ToString("HH:mm:ss"));
            Console.WriteLine("Press any key to exit.");
            Console.ReadKey();
        }
        static void pRuntime_Elapsed(object sender, ElapsedEventArgs e)
        {
            time++; // increase the timer (by 1 second intervals)
            if (time < 60)         // seconds
            {
                totalTime = time + "s";
                if (time >= 59)             // minutes
                {
                    time = 0;
                    minutes++; // add a minute
                    totalTime = minutes + "m";
                    if (minutes >= 59)          // hours
                    {
                        time = 0;
                        minutes = 0;
                        hours++; // add an hour
                        totalTime = hours + "h " + minutes + "m";
                        if (hours >= 23)            // days
                        {
                            time = 0;
                            hours = 0;
                            minutes = 0;
                            days++; // add a day!
                            totalTime = days + "d " + hours + "h " + minutes + "m";
                        }
                    }
                }
            }
            else
            {
                Console.WriteLine("Error!");
            }
            Console.Title = "Knife Only Launcher - v1.0 | Run time: " + days.ToString() + "d " + hours.ToString() + "h " + minutes.ToString() + "m " + time.ToString() + "s - " + bitLength;
        }
        static void p_ErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            p.Kill(); // kill the process
            p.Start(); // restart the process
            LogError(DateTime.Now.ToString("HH:mm:ss "), "p has crashed!" + " Uptime: " + days + "d " + hours + "h " + minutes + "m " + time + "s");
            Console.WriteLine("Process crashed");
        }
        static void p_Exited(object sender, EventArgs e)
        {
            LogError(DateTime.Now.ToString("HH:mm:ss "), "p has closed!" + " Uptime: " + days + "d " + hours + "h " + minutes + "m " + time + "s");
            Console.WriteLine("KnifeOnly has closed.");
            p.Start(); // start the process
        }
        static void pCheck_Elapsed(object sender, ElapsedEventArgs e)
        {
            checkinTimes = checkinTimes + 20; // add a tick for every 20 seconds!

            #region Count
            if (checkinTimes > 19)
            {
                checkinTimes = 0; // reset to zero
                checkTimes++; // increment by one, for every 20.
            }
            #endregion

            if (p.HasExited == false)
            {
                if (p.Responding)
                {
                    // is responding
                    Console.WriteLine("Process is working | C: " + checkTimes);
                }
                else
                { 
                        p.CloseMainWindow();
                        p.Kill(); // end the process
                        Console.WriteLine("Process crashed! Restarting...");
                        LogError(DateTime.Now.ToString("HH:mm:ss "), "p has closed!" + " Uptime: " + days + "d " + hours + "h " + minutes + "m " + time + "s");
                        checkinTimes = 0;
                        p.Start(); // start the process
                        Console.WriteLine("Process started again!");
                }
            }
            else
            {
                Console.WriteLine("Knife Only is not running! :(");
            }
        }
        static void Program_Exited(object sender, EventArgs e)
        {
            Console.WriteLine("WerFault.exe killed!\n");
        }
        static void LogError(string time, string notes)
        {
            try
            {
                sw.WriteLine("Crashed at: " + time, " Logged at: " + DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss"));
            }
            catch
            {
               Console.WriteLine("Logging failed! " + DateTime.Now.ToString("HH:mm:ss"));
            }
            finally
            {
                if (sw != null)
                {
                    sw.Close();
                }
            }
        }

    }
}
