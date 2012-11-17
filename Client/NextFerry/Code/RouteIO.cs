﻿using System;
using System.Windows;
using System.IO;
using System.IO.IsolatedStorage;

namespace NextFerry
{
    /// <summary>
    /// Parse and store schedules in IsolatedStorage.
    /// </summary>
    public static class RouteIO
    {
        // We get the ferrry schedule from a web service, and store it locally.
        // The schedule format is simple text: a sequence of lines of the form:
        //
        //      bainbridge,wd,330,370,...
        //
        // This is the the route name, two chars telling if this is a west/east and weekday/weekend schedule,
        // and then a list of departure times (in minutes past midnight).
        //
        // When we read the schedule file, we update AllRoutes accordingly.

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
                System.Diagnostics.Debug.WriteLine("deserialize: |" + line + "|");
                // Skip comments and empty lines.
                if (line.Length < 2) continue;
                if (line.StartsWith("//")) continue;

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
        /// Delete the cache.  Does <strong>not</strong> clear data from AllRoutes.
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
