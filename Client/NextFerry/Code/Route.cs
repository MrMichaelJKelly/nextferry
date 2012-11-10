﻿using System;
using System.Net;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Threading;

namespace NextFerry
{
    /// <summary>
    /// Everything we know about ferry route.  This includes both the "static" part (identification)
    /// and the "dynamic" part (current schedule)
    /// </summary>
    public class Route : INotifyPropertyChanged
    {
        #region state
        // change notified properties
        private bool _display;
        public bool display
        {
            get { return _display; }
            set { if (value != _display) { _display = value; OnChanged("display"); } }
        }

        private Schedule _weekday = new Schedule();
        public Schedule weekday
        {
            get { return _weekday; }
            set {
                _weekday = value;
                OnChanged("weekday");
                if (!useWeekendScheduleToday)
                {
                    stateRefresh();
                    OnChanged("departuresToday");
                    OnChanged("recentPastDepartures");
                    OnChanged("futureDepartures");
                }
            }
        }


        private Schedule _weekend = new Schedule();
        public Schedule weekend
        {
            get { return _weekend; }
            set {
                _weekend = value;
                OnChanged("weekend");
                if (useWeekendScheduleToday)
                {
                    stateRefresh();
                    OnChanged("departuresToday");
                    OnChanged("recentPastDepartures");
                    OnChanged("futureDepartures");
                }
            }
        }

        /// <summary>
        /// A setter for use from other threads.  Synchronous if the wait
        /// parameter is true.   (Note that synchronous means waiting for
        /// all the changed events to be processed too, not just the actual assignment.
        /// Not ideal, but it will work.)
        /// </summary>
        public void setScheduleMT(Schedule news, bool wait)
        {
            ManualResetEvent wh = null;
            if (wait)
                wh = new ManualResetEvent(false);

            Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    try
                    {
                        if (news.isWeekend)
                            this.weekend = news;
                        else
                            this.weekday = news;
                    }
                    finally
                    {
                        if (wait)
                            wh.Set();
                    }
                });

            if (wait)
                wh.WaitOne();
        }


        // these are immutable, so we don't actually do notification
        public string direction { get; private set; }
        public string name { get; private set; }
        public int sourceCode { get; private set; }
        public int destCode { get; private set; }
        public bool useWeekendScheduleToday { get; private set; }
        // note we assume that the app does not remain open for hours on end, hence isWeekend does not change.

        public Route(string dir, string name, int src, int dest)
        {
            direction = dir;
            this.name = name;
            sourceCode = src;
            destCode = dest;
            useWeekendScheduleToday = initWeekendHoliday();
            AppSettings.PropertyChanged += appSettingsChanged;
        }

        #endregion

        #region magic properties
        // These properties are computed on demand.

        public IEnumerable<DepartureTime> departuresToday
        {
            get { return useWeekendScheduleToday ? weekend.times : weekday.times ; }
        }

        /// <summary>
        /// Return a set of the most recent departures.
        /// </summary>
        public IEnumerable<DepartureTime> recentPastDepartures
        {
            get { return Departures.between(departuresToday, DepartureTime.Now - 50, DepartureTime.Now); }
        }

        public IEnumerable<DepartureTime> futureDepartures
        {
            get { return Departures.after(departuresToday, DepartureTime.Now); }
        }

        #endregion

        #region change notification event

        public event PropertyChangedEventHandler PropertyChanged;
        public void OnChanged(string s)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(s));
            }
        }

        #endregion

        #region state refresh
        /// <summary>
        /// Update state changes that occur due to the passage of time.   Basically means computing the "goodness"
        /// of departure times.    In theory this could include things like distance, wait times or travel conditions....
        /// </summary>
        public void stateRefresh()
        {
            int now = DepartureTime.Now;
            int tooSoon = now + AppSettings.terminalTravelTime;
            int dontcare = tooSoon + 120;

            foreach (DepartureTime d in departuresToday)
            {
                if (d.value < now)
                    d.goodness = DepartureTime.TooLate;
                else if (d.value < tooSoon)
                    d.goodness = DepartureTime.TooSoon;
                else if (d.value < dontcare )
                    d.goodness = DepartureTime.Good;
                else
                    d.goodness = DepartureTime.TooFar;
            }
        }

        public void appSettingsChanged(Object sender, PropertyChangedEventArgs e)
        {
            if (String.Equals(e.PropertyName, AppSettings.KterminalTravelTime))
                stateRefresh();
        }

        #endregion

        #region utilities

        /// <summary>
        /// Remove schedules.  Used to clean up/reload.  Must be called on UI thread.
        /// </summary>
        public void clearSchedules()
        {
            weekday = new Schedule();
            weekend = new Schedule();
        }
        
        /// <summary>
        /// Return the terminal on the eastern end of the route.
        /// </summary>
        public Terminal eastTerminal()
        {
            return Terminal.lookup(direction[0] == 'w' ? sourceCode : destCode);
        }


        /// <summary>
        /// Return the terminal on the eastern end of the route.
        /// </summary>
        public Terminal westTerminal()
        {
            return Terminal.lookup(direction[0] == 'w' ? destCode : sourceCode);
        }


        /// <summary>
        /// Return true if the weekend schedule should be used today, i.e. today is a weekend or holiday.
        /// </summary>
        /// <returns></returns>
        private bool initWeekendHoliday()
        {
            DateTime today = DateTime.Today;
            return today.DayOfWeek == DayOfWeek.Saturday
                   || today.DayOfWeek == DayOfWeek.Sunday
                   || (Holiday.isHoliday(today) && (String.Equals(this.name, "bainbridge") ||
                                                    String.Equals(this.name, "pt defiance") ||
                                                    String.Equals(this.name, "mukilteo")));
            // yup, only some of the routes have holiday schedules.
            // I didn't know that either until I was trying to code this up...
        }
        #endregion
    }

    /// <summary>
    /// A target schedule for a specific route and weekday or weekend, valid through the expiration date.
    /// Schedules are immutable once fully initialized, so notifications happen a level up.
    /// </summary>
    public class Schedule
    {
        public bool isWeekend { get; set; }
        public List<DepartureTime> times { get; set; }
        public DateTime expiration { get; set; }

        private static DateTime longAgo = new DateTime(1900, 1, 1);
        public Schedule()
        {
            times = new List<DepartureTime>();
            expiration = longAgo;
        }
    }

    /// <summary>
    /// Represents the end-point of a ferry route.
    /// </summary>
    public class Terminal
    {
        public int code { get; set; }   // codes as used in the WSDOT's web services.
        public string name { get; set; }
        // TODO: add location?

        public static Terminal lookup(int code)
        {
            if (AllTerminals == null) initAllTerminals();
            return AllTerminals[code];
        }

        private static Dictionary<int, Terminal> AllTerminals = null;
        private static void initAllTerminals()
        {
            AllTerminals = new Dictionary<int, Terminal>();
            AllTerminals.Add(1, new Terminal { code = 1, name = "Anacortes" });
            AllTerminals.Add(3, new Terminal { code = 3, name = "Bainbridge" });
            AllTerminals.Add(4, new Terminal { code = 4, name = "Bremerton" });
            AllTerminals.Add(5, new Terminal { code = 5, name = "Clinton" });
            AllTerminals.Add(11, new Terminal { code = 11, name = "Coupville" });
            AllTerminals.Add(8, new Terminal { code = 8, name = "Edmonds" });
            AllTerminals.Add(9, new Terminal { code = 9, name = "Fauntleroy" });
            AllTerminals.Add(10, new Terminal { code = 10, name = "Friday Harbor" });
            AllTerminals.Add(12, new Terminal { code = 12, name = "Kingston" });
            AllTerminals.Add(13, new Terminal { code = 13, name = "Lopez Island" });
            AllTerminals.Add(14, new Terminal { code = 14, name = "Mukilteo" });
            AllTerminals.Add(15, new Terminal { code = 15, name = "Orcas Island" });
            AllTerminals.Add(16, new Terminal { code = 16, name = "Point Defiance" });
            AllTerminals.Add(17, new Terminal { code = 17, name = "Port Townsend" });
            AllTerminals.Add(7, new Terminal { code = 7, name = "Seattle" });
            AllTerminals.Add(18, new Terminal { code = 18, name = "Shaw Island" });
            AllTerminals.Add(20, new Terminal { code = 20, name = "Southworth" });
            AllTerminals.Add(21, new Terminal { code = 21, name = "Tahlequah" });
            AllTerminals.Add(22, new Terminal { code = 22, name = "Vashon Island" });
        }
    }
}