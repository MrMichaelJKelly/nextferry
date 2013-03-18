﻿using System;
using System.IO;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using System.Windows.Threading;

namespace NextFerry
{

    public partial class MainPage : PhoneApplicationPage
    {
        private ObservableCollection<Route> displayRoutes = new ObservableCollection<Route>();
        private DispatcherTimer travelTimeWatcher = new DispatcherTimer();
        private DispatcherTimer scheduleWatcher = new DispatcherTimer();

        #region initialization
        public MainPage()
        {
            InitializeComponent();

            ((App)Application.Current).theMainPage = this;
 
            // Some top down initialization...
            DepartureTime.tooLateStyle = (Style)this.Resources["tooLateStyle"];
            DepartureTime.riskyStyle = (Style)this.Resources["riskyStyle"];
            DepartureTime.goodStyle = (Style)this.Resources["goodStyle"];
            DepartureTime.defaultStyle = (Style)this.Resources["defaultStyle"];

            Route.alertStyleNone = (Style)this.Resources["iconAbsent"];
            Route.alertStyleNormal = (Style)this.Resources["icon"];
            Route.alertStyleUnread = (Style)this.Resources["iconBright"];

            list1.ItemsSource = displayRoutes;
            list3.ItemsSource = displayRoutes;

            WP7Contrib.Diagnostics.RuntimeDebug.Initialize(true, false, "denisesandbox@mailup.net", "");

            AlertManager.newAlerts += newAlertsArrived;

            initTTWatcher();
            initScheduleWatcher();
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            SystemTray.BackgroundColor = (Color)this.Resources["PugetGreyColor"];
            SystemTray.ForegroundColor = (Color)this.Resources["MilkySkyColor"];

            // We could do something fancy and LINQ --- this is easier and lighter-weight
            displayRoutes.Clear();
            foreach (Route r in RouteManager.AllRoutes)
            {
                if (r.display)
                    displayRoutes.Add(r);
            }

            if (AppSettings.displayWB)
                switchToWB(this, null);
            else
                switchToEB(this, null);

            if (AppSettings.useLocation)
                travelTimeWatcher.Start();

            newAlertsArrived(null, null);
        }

        protected override void OnNavigatedFrom(System.Windows.Navigation.NavigationEventArgs e)
        {
            travelTimeWatcher.Stop();
        }
        #endregion

        #region user actions
        private void switchToWB(object sender, RoutedEventArgs e)
        {
            AppSettings.displayWB = true;
            buttonWB.Style = (Style)this.Resources["toggleSelected"];
            buttonEB.Style = (Style)this.Resources["toggleUnselected"];
            ewsign.Opacity = 0;
            ewsign.Text = "west";
            fadeIn.Begin();
        }

        private void switchToEB(object sender, RoutedEventArgs e)
        {
            AppSettings.displayWB = false;
            buttonWB.Style = (Style)this.Resources["toggleUnselected"];
            buttonEB.Style = (Style)this.Resources["toggleSelected"];
            ewsign.Opacity = 0;
            ewsign.Text = "east";
            fadeIn.Begin();
        }


        private void gotoRoutePage(object sender, System.Windows.Input.GestureEventArgs e)
        {          
            // Figure out which item we were on (thank you msdn code samples!)
            FrameworkElement item = (FrameworkElement)e.OriginalSource;
            Route r = (Route)item.DataContext;
            if (r == null) return;

            string urlWithData = string.Format("/RoutePage.xaml?route={0}", r.wbName);
            NavigationService.Navigate(new Uri(urlWithData, UriKind.Relative));
        }

        private void gotoSettings(object sender, EventArgs e)
        {
            NavigationService.Navigate(new Uri("/Settings.xaml", UriKind.Relative));
        }

        private void gotoChoose(object sender, EventArgs e)
        {
            NavigationService.Navigate(new Uri("/Choose.xaml", UriKind.Relative));
        }

        private void gotoHelp(object sender, EventArgs e)
        {
            NavigationService.Navigate(new Uri("/help.xaml", UriKind.Relative));
        }
        #endregion

        #region travel time management
        // We have a ping-pong like arrangement between receiving travel times and showing warnings,
        // and the timer is the ping-pong ball in between them.
        // When we receive travel times, we turn off any warning, and turn the timer on
        // When the timer goes off, it turns the warning on and turns itself off

        private TimeSpan shortInterval = new TimeSpan(0, 0, 10);
        private TimeSpan longInterval = new TimeSpan(0, 3, 0);

        private void initTTWatcher()
        {
            // create the timer here
            // start it in OnNavigatedTo (and timesReceived)
            travelTimeWatcher.Interval = shortInterval;
            travelTimeWatcher.Tick += (o, a) =>
                {
                    if (AppSettings.useLocation)
                    {
                        addMessage("Waiting for travel times");
                    }
                    // The timer will be turned back on when travel times arrive.
                    travelTimeWatcher.Stop();
                };

            LocationMonitor.NewTravelTimes += timesReceived;
        }

        /// <summary>
        /// What to do when some new travel times arrive.
        /// </summary>
        private void timesReceived(TravelTimeEventArgs args)
        {
            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                if (args.traveltimes.Count > 0)
                    removeMessage();
                else
                    addWarning("Travel times not available");

                foreach (Terminal t in Terminal.AllTerminals)
                {
                    if (args.traveltimes.ContainsKey(t.code))
                    {
                        t.setTT(args.traveltimes[t.code]);
                    }
                    else
                    {
                        t.clearTT();
                    }
                }
                RouteManager.updateDisplay();


                // After the first times are received, set the watch interval to longer
                travelTimeWatcher.Interval = longInterval;
                travelTimeWatcher.Start();
            });
        }

        private void initScheduleWatcher()
        {
            // Schedule watcher is a one-time event to make sure we are actually
            // displaying a schedule to the user.  If not, we show an error message.
            // This should only occur if (a) there is no cached version of the schedule
            // and (b) there is no network access.
            scheduleWatcher.Interval = new TimeSpan(0,2,0);
            scheduleWatcher.Tick += (o, a) =>
                {
                    scheduleWatcher.Stop();
                    scheduleWatcher = null;
                    if ( ! RouteManager.haveSchedules() )
                        this.nonetwork.Visibility = System.Windows.Visibility.Visible;
                };
            scheduleWatcher.Start();
        }

        #endregion

        #region message display
        // These must be called carefully --- there is no locking

        public void addMessage(string contents)
        {
            messageText.Text = contents;
            warning.Visibility = System.Windows.Visibility.Collapsed;
            message.Visibility = System.Windows.Visibility.Visible;
        }

        public void addWarning(string contents)
        {
            messageText.Text = contents;
            warning.Visibility = System.Windows.Visibility.Visible;
            message.Visibility = System.Windows.Visibility.Visible;
        }

        public void removeMessage()
        {
            message.Visibility = System.Windows.Visibility.Collapsed;
        }
        #endregion

        #region alerts

        private void newAlertsArrived(Object sender, EventArgs args)
        {
            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                foreach (Route r in RouteManager.AllRoutes)
                {
                    r.updateAlertStyle();
                }
            });
        }
    }

    // I just figured out you can get some of the benefits of a view 
    // cheaply by using partial classes... Factors relevant code
    // closer together.

    public partial class Route : INotifyPropertyChanged
    {
        internal static Style alertStyleNone;
        internal static Style alertStyleNormal;
        internal static Style alertStyleUnread;

        /// <summary>
        /// Style to use to display alerts, based on whether we have any or not.
        /// </summary>
        public Style alertStyle { get; private set; }

        /// <summary>
        /// Recalculate alert state.  Causes change notification
        /// </summary>
        internal void updateAlertStyle()
        {
            Style newstyle =
                (this.hasAlerts ? (this.hasNewAlerts ? alertStyleUnread : alertStyleNormal)
                                : alertStyleNone);

            if (newstyle != alertStyle)
            {
                alertStyle = newstyle;
                OnChanged("alertStyle");
            }
        }
    }
    #endregion
}
