﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.DataVisualization.Charting;
using System.Windows.Controls.Ribbon;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Controls.DataVisualization.Charting;
using Microsoft.Research.DynamicDataDisplay;
using Microsoft.Research.DynamicDataDisplay.Charts;
using Microsoft.Research.DynamicDataDisplay.DataSources;
using Microsoft.Research.DynamicDataDisplay.ViewportRestrictions;
using TrafficAnalysis.ChartEx;
using TrafficAnalysis.DeviceDataSource;
using TrafficAnalysis.Util;
using TrafficAnalysis.Pages;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace TrafficAnalysis
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : RibbonWindow
    {
        #region Fields

        internal IStatisticsSource Ssource = new MonitorPcap();

        private ObservableCollection<ITabPage> pages = new ObservableCollection<ITabPage>();

        #endregion

        public MainWindow()
        {
            InitializeComponent();

            Tabs.ItemsSource = pages;
            pages.CollectionChanged += pages_CollectionChanged;

            BindingCommands();
        }

        #region Initialize

        private void BindingCommands()
        {
            CommandBindings.Add(new CommandBinding(CloseDocument, (o, e) => pages.Remove((ITabPage)Tabs.ItemContainerGenerator.ItemFromContainer((TabItem)e.Parameter))));
            CommandBindings.Add(new CommandBinding(ActivateDocument, (o, e) =>
            {
                Tabs.SelectedItem = e.Parameter;
                UI.OverflowTabHeaderObserver.EnsureActiveTabVisible(Tabs);
            }));
            CommandBindings.Add(new CommandBinding(NewFluxAnalyze, (o, e) =>
            {
                OpenNewFluxAnalyze();
            }));
        }

        #endregion

        #region Analyze

        static private string[] bpsUnit = new string[]{"bps", "Kbps", "Mbps", "Gbps", "Tbps", "Pbps", "Ebps"};
        internal void FormatBpsSpeed(double bps)
        {
#if DEBUG
            Console.WriteLine("bps:" + bps);
#endif
            int multiper = 0;
            while (bps > 1000)
            {
                bps /= 1000;
                multiper++;
            }
            int i = (int) bps;
            int r = (int) ((bps - i) * 100);
            bpsLabel1.Content = i.ToString();
            bpsLabel2.Content = "." + r.ToString("D2");
            bpsLabel3.Content = bpsUnit[multiper];
        }

        static private string[] ppsUnit = new string[]{"pps", "Kpps", "Mpps", "Gpps", "Tpps", "Ppps", "Epps"};
        internal void FormatPpsSpeed(double pps)
        {
#if DEBUG
            Console.WriteLine("pps:" + pps);
#endif
            int multiper = 0;
            while (pps > 1000)
            {
                pps /= 1000;
                multiper++;
            }
            int i = (int)pps;
            int r = (int) ((pps - i) * 100);
            ppsLabel1.Content = i.ToString();
            ppsLabel2.Content = "." + r.ToString("D2");
            ppsLabel3.Content = ppsUnit[multiper];
        }

        #endregion

        #region Public Methods
        /// <summary>
        /// Open a new Flux file to analyze
        /// </summary>
        public void OpenNewFluxAnalyze()
        {
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();

            Nullable<bool> res = dlg.ShowDialog();

            if (res == true)
            {
                FileAnalyzePage page = new FileAnalyzePage(dlg.FileName);
                pages.Add(page);
                ActivateDocument.Execute(this, page);

                Tabs.SelectedIndex = pages.Count - 1;
                page.Load();
            }
        }
        #endregion

        #region Event Handlers
        private void pages_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
            case NotifyCollectionChangedAction.Add:
                foreach (ITabPage page in e.NewItems)
                {
                    var ti = (TabItem) (Tabs.ItemContainerGenerator.ContainerFromItem(page));
                    page.OnTabItemAttached(this, ti);
                }
                break;
            case NotifyCollectionChangedAction.Remove:
                foreach (ITabPage page in e.OldItems)
                {
                    var ti = (TabItem)(Tabs.ItemContainerGenerator.ContainerFromItem(page));
                    page.OnTabItemDetaching(this, ti);
                }
                break;
            case NotifyCollectionChangedAction.Replace:
            case NotifyCollectionChangedAction.Reset:
                foreach (ITabPage page in e.NewItems)
                {
                    var ti = (TabItem)(Tabs.ItemContainerGenerator.ContainerFromItem(page));
                    page.OnTabItemAttached(this, ti);
                }
                foreach (ITabPage page in e.OldItems)
                {
                    var ti = (TabItem)(Tabs.ItemContainerGenerator.ContainerFromItem(page));
                    page.OnTabItemDetaching(this, ti);
                }
                break;
            default:
                break;
            }
        }

        private void RibbonWindow_Loaded(object sender, RoutedEventArgs e)
        {
            DeviceList.ItemsSource = Ssource.DeviceList;

            if (Ssource.DeviceList.Count != 0)
            {
                DeviceList.SelectedIndex = 0;
            }

            ITabPage dm = new DetialMonitorPage();
            pages.Add(dm);
        }

        private void RibbonWindow_Closed(object sender, EventArgs e)
        {
            DeviceDes[] items = Ssource.MonitoringList.ToArray();
            foreach (var des in items)
            {
                Ssource.StopCapture(des);
            }
        }

        private void DeviceList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            DeviceDes des = DeviceList.SelectedValue as DeviceDes;
            ShowInMonitor.IsChecked = Ssource.MonitoringList.Contains(des);
        }

        private void ShowInMonitor_Checked(object sender, RoutedEventArgs e)
        {
            DeviceDes des = DeviceList.SelectedValue as DeviceDes;
            Ssource.StartCapture(des);
        }

        private void ShowInMonitor_Unchecked(object sender, RoutedEventArgs e)
        {
            DeviceDes des = DeviceList.SelectedValue as DeviceDes;
            Ssource.StopCapture(des);
        }

        private void CaptureThis_Checked(object sender, RoutedEventArgs e)
        {
            DeviceDes des = DeviceList.SelectedValue as DeviceDes;
        }

        private void CaptureThis_Unchecked(object sender, RoutedEventArgs e)
        {
            DeviceDes des = DeviceList.SelectedValue as DeviceDes;
        }

        private void RibbonButton_Click(object sender, RoutedEventArgs e)
        {
        }

        private void RibbonSplitButton_Click(object sender, RoutedEventArgs e)
        {
            return;
        }

        #endregion

        #region Commands
        public static readonly RoutedCommand CloseDocument = new RoutedCommand();
        public static readonly RoutedCommand ActivateDocument = new RoutedCommand();
        public static readonly RoutedCommand NewFluxAnalyze = new RoutedCommand();
        #endregion
    }
}
