﻿using System;
using System.Net;
using System.Windows;
using System.IO;
using System.IO.IsolatedStorage;

namespace NextFerry
{
    /// <summary>
    /// Auxiliary functionality to fetching schedules from the server and 
    /// storing them in IsolatedStorage
    /// </summary>
    public static class RouteIO
    {
        // We get the ferrry schedule from an Azure service, and store it locally.
        // The schedule format is simple text: a sequence of lines of the form:
        //
        //      bainbridge,wd,330,370,...
        //
        // This is the the route name, two chars telling if this is a west/east and weekday/weekend schedule,
        // and then a list of departure times (in minutes past midnight).
        //
        // When we read the schedule file, we update AllRoutes accordingly.

        public static void getUpdate()
        {
            // if there's no network we don't do anything
            if (((App)Application.Current).usingNetwork)
            {
                string appVersion = ((App)Application.Current).appVersion;
                string cacheVersion = AppSettings.cacheVersion;
                NextFerryServer.Service1Client client = null;

                try
                {
                    client = new NextFerryServer.Service1Client();
                    client.GetSchedulesCompleted += processServerSchedule;
                    client.GetSchedulesAsync(appVersion, cacheVersion);
                }
                catch (Exception e)
                {
                    System.Diagnostics.Debug.WriteLine("Error accessing Server: " + e.Message);
                }
                finally
                {
                    if (null != client)
                    {
                        client.CloseAsync();
                    }
                }
            }
        }


        public static void processServerSchedule(Object sender, EventArgs args)
        {
            string answer = ((NextFerryServer.GetSchedulesCompletedEventArgs)args).Result;
            int count = 0;

            System.Diagnostics.Debug.WriteLine("Received " + answer.Length + " bytes from Ferry Server");
            if (answer.Length > 0)
            {
                // Try to parse it.
                try
                {
                    count = deserialize(new StringReader(answer));
                }
                catch (Exception e)
                {
                    // Something went wrong. Restore the old cached value and exit.
                    // TODO: ...and if there was no cached value?  could end up with partial junk?
                    System.Diagnostics.Debug.WriteLine("Parse error in Ferry Server response: " + e.Message);

                    readCache();
                    return;
                }
                System.Diagnostics.Debug.WriteLine("Read " + count + " records from Ferry Server");
                if (count == Routes.AllRoutes.Count * 2)
                {
                    // complete read: save a local copy and update our state.
                    writeCache(answer);
                    AppSettings.cacheVersion = DateTime.Today.ToShortDateString();
                }
                // else do nothing: we show what we read but don't store it.
            }
        }

        private readonly static IsolatedStorageFile myStore = IsolatedStorageFile.GetUserStoreForApplication();
        private const string scheduleFile = "CachedFerrySchedules.txt";  // where on disk to store it.

        /// <summary>
        /// Keep a local copy of the new schedule.
        /// </summary>
        public static void writeCache(string newtext)
        {
            System.Diagnostics.Debug.WriteLine("writing cache");
            using (IsolatedStorageFileStream stream = new IsolatedStorageFileStream(scheduleFile, FileMode.Create, myStore))
            {
                using (StreamWriter writer = new StreamWriter(stream))
                {
                    writer.Write(newtext);
                }
            }
        }

        /// <summary>
        /// Reads cached state and updates AllRoutes accordingly.
        /// </summary>
        public static void readCache()
        {
            if (myStore.FileExists(scheduleFile))
            {
                try
                {
                    using (var isoFileStream = new IsolatedStorageFileStream(scheduleFile, FileMode.Open, myStore))
                    {
                        using (var isoFileReader = new StreamReader(isoFileStream))
                        {
                            deserialize(isoFileReader);
                        }
                    }
                    System.Diagnostics.Debug.WriteLine("readCache succeeded");
                }
                catch (Exception e)
                {
                    System.Diagnostics.Debug.WriteLine("readCache failed: " + e.Message);
                }
            }
        }


        public static int deserialize(TextReader s)
        {
            int count = 0;
            while (true)
            {
                string line = s.ReadLine();
                if (line == null) break;
                //System.Diagnostics.Debug.WriteLine("deserialize: " + line);
                if (line.StartsWith("//")) continue;  // Allows us to add comments in the file when testing, etc.

                parseLine(line);
                count++;
            }
            return count;
        }


        private static void parseLine(string line)
        {
            // name, code, departure times...
            string[] data = line.Split(',');
            int len = data.Length;

            if (len < 5) // heuristic: all ferry lines have at least 3 departures per day
                throw new ArgumentException("line too short (" + len + ")");

            string name = data[0];
            string code = data[1];

            if (code.Length != 2)
                throw new ArgumentException("invalid code '" + code + "'");
            bool iswest = (code[0] == 'w');
            bool isweekend = (code[1] == 'e');

            Schedule news = new Schedule();
            news.isWeekend = isweekend;

            // unpack the times
            for (int i = 4; i < len; i++)
                news.times.Add(new DepartureTime(int.Parse(data[i])));

            Route r = Routes.getRoute(name, iswest ? "wb" : "eb");
            if (r == null)
                throw new ArgumentException("unexpected route name");

            // Update AllRoutes.
            System.Diagnostics.Debug.WriteLine("updating " + r.name + "/" + r.direction + "/" + news.isWeekend);
            r.setScheduleMT(news, true);
        }

        /// <summary>
        /// Deleted the cache.  Does <strong>not</strong> clear data from AllRoutes.
        /// </summary>
        public static void deleteCache()
        {
            try
            {
                AppSettings.cacheVersion = "";
                myStore.DeleteFile(scheduleFile);
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine("Unable to clear cache: " + e.Message);
            }
        }

        public static string cacheStatus()
        {
            if (myStore.FileExists(scheduleFile))
                return "Cached as of " + myStore.GetCreationTime(scheduleFile).ToString("G");
            else
                return "Cache not present.";
        }
    }
}