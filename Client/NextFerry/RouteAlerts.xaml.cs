﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Microsoft.Phone.Controls;

namespace NextFerry
{
    public partial class Alerts : PhoneApplicationPage
    {
        private Route r = null;

        public Alerts()
        {
            InitializeComponent();
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            r = RoutePage.findRoute(this, r);
            RoutePage.manageBackPointer(this);
            pageTitle.Text = r.eastTerminal.name + " / " + r.westTerminal.name;
            alertList.ItemsSource = r.alerts;
            AlertManager.newAlerts += gotsome;
        }

        protected override void OnNavigatedFrom(System.Windows.Navigation.NavigationEventArgs e)
        {
            AlertManager.newAlerts -= gotsome;
            foreach (Alert a in r.alerts)
            {
                a.read = true;
            }
        }

        public void gotsome(Object sender, Object args)
        {
            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                alertList.ItemsSource = r.alerts;
            });
        }

        private void gotoSchedule(object sender, System.Windows.Input.GestureEventArgs e)
        {
            string urlWithData = string.Format("/RoutePage.xaml?route={0}", r.wbName);
            NavigationService.Navigate(new Uri(urlWithData, UriKind.Relative));
        }
    }
}