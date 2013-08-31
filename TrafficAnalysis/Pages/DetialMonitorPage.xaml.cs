using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.DataVisualization.Charting;
using System.Windows.Media;
using System.Windows.Threading;
using Controls.DataVisualization.Charting;
using Microsoft.Research.DynamicDataDisplay;
using Microsoft.Research.DynamicDataDisplay.ViewportRestrictions;
using TrafficAnalysis.DeviceDataSource;
using TrafficAnalysis.Util;

namespace TrafficAnalysis.Pages
{
    /// <summary>
    /// UserControl1.xaml 的交互逻辑
    /// </summary>
    public partial class DetialMonitorPage : UserControl, ITabPage, INotifyPropertyChanged
    {
        public DetialMonitorPage()
        {
            InitializeComponent();
            InitStatistics();
            InitGraph();
        }

        #region Fields
        private IDeviceSource _Ssource;
        public IDeviceSource Ssource
        {
            get { return _Ssource; }
            set
            {
                if (value == null)
                {
                    helpers.Clear();
                }
                else if (value != Ssource)
                {
                    _Ssource = value;
                    helpers.Clear();
                    foreach (DeviceDes des in _Ssource.DeviceList)
                    {
                        helpers[des.Name] = new DeviceStatisticsHelper(BpsChart, PpsChart, des);
                        helpers[des.Name].MaxPoints = chartwidthfactor;
                    }
                }
            }
        }

        private Dictionary<string, DeviceStatisticsHelper> helpers = new Dictionary<string, DeviceStatisticsHelper>();

        private DispatcherTimer refreshTimer;

        private double chartwidthfactor = 50;
        #endregion

        #region Properties

        #region public double TotalBPS
        private double _totalBPS;
        public double TotalBPS
        {
            get { return _totalBPS; }
            private set
            {
                var old = _totalBPS;
                _totalBPS = value;
                if (old != _totalBPS)
                    OnPropertyChanged("TotalBPS");
            }
        }
        #endregion

        #region public double TotalPPS
        private double _totalPPS;
        public double TotalPPS
        {
            get { return _totalPPS; }
            private set
            {
                var old = _totalPPS;
                _totalPPS = value;
                if (old != _totalPPS)
                    OnPropertyChanged("TotalPPS");
            }
        }
        #endregion

        #region PropertyChanged Event
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            if (this.PropertyChanged != null)
            {
                this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
        #endregion

        #endregion

        #region Initialize

        private void InitStatistics()
        {
            refreshTimer = new DispatcherTimer();
            refreshTimer.Interval = TimeSpan.FromMilliseconds(250);
            refreshTimer.Tick += ReadStatistics;
        }

        private void InitGraph()
        {
            // Set horizontal and vertical clip factor to different values.
            ViewportRestrictionCallback enlargeCallback = (proposedBound) =>
            {
                if (proposedBound.IsEmpty)
                {
                    return proposedBound;
                }
                return CoordinateUtilities.RectZoom(proposedBound, proposedBound.GetCenter(), 1, 1.05);
            };
            BpsChart.ViewportClipToBoundsEnlargeFactor = 1;
            PpsChart.ViewportClipToBoundsEnlargeFactor = 1;
            BpsChart.Viewport.FitToViewRestrictions
                .Add(new InjectionDelegateRestriction(BpsChart.Viewport, enlargeCallback));
            PpsChart.Viewport.FitToViewRestrictions
                .Add(new InjectionDelegateRestriction(PpsChart.Viewport, enlargeCallback));

            // Disable zoom and pan
            BpsChart.Children.Remove(BpsChart.MouseNavigation);
            BpsChart.Children.Remove(BpsChart.KeyboardNavigation);
            BpsChart.Children.Remove(BpsChart.HorizontalAxisNavigation);
            BpsChart.Children.Remove(BpsChart.VerticalAxisNavigation);
            BpsChart.DefaultContextMenu.RemoveFromPlotter();

            PpsChart.Children.Remove(PpsChart.MouseNavigation);
            PpsChart.Children.Remove(PpsChart.KeyboardNavigation);
            PpsChart.Children.Remove(PpsChart.HorizontalAxisNavigation);
            PpsChart.Children.Remove(PpsChart.VerticalAxisNavigation);
            PpsChart.DefaultContextMenu.RemoveFromPlotter();

            ResetLineChart(BpsChart);
            ResetLineChart(PpsChart);
        }

        private void ResetLineChart(ChartPlotter plotter)
        {

        }

        #endregion

        #region Add/Remove Device

        private void SetupDevice(DeviceDes des)
        {
            if (!refreshTimer.IsEnabled)
            {
                refreshTimer.Start();
                ResetLineChart(BpsChart);
                ResetLineChart(PpsChart);
            }

            AddDeviceToChart(des);
        }

        private void UnSetupDevice(DeviceDes des)
        {
            RemoveDeviceFromChart(des);

            if (Ssource.MonitoringList.Count == 0)
            {
                refreshTimer.Stop();
                DeviceStatisticsHelper.startTime = -1;
            }
        }

        protected void AddDeviceToChart(DeviceDes des)
        {
            // Pie Chart
            FrameworkElement chart = CreatePieCharts(helpers[des.Name]);
            chart.Tag = des.Name;

            CentralGraph.Children.Add(chart);

            // Line Chart
            Color c = ColorGen.GetColor();
            BpsChart.AddLineGraph(helpers[des.Name].Bps, c, 2, des.FriendlyName);
            PpsChart.AddLineGraph(helpers[des.Name].Pps, c, 2, des.FriendlyName);
            BpsChart.FitToView();
            PpsChart.FitToView();
        }

        protected void RemoveDeviceFromChart(DeviceDes des)
        {
            // Pie Chart
            for (int i = 0; i != CentralGraph.Children.Count; i++)
            {
                FrameworkElement chart = CentralGraph.Children[i] as FrameworkElement;
                if (chart != null && chart.Tag.Equals(des.Name))
                {
                    CentralGraph.Children.RemoveAt(i);
                    break;
                }
            }

            // Line Chart
            var bpsline = BpsChart.Children.OfType<LineGraph>()
                .Where(x => x.Description.Brief.Equals(des.FriendlyName)).Single();
            BpsChart.Children.Remove(bpsline);
            var ppsline = PpsChart.Children.OfType<LineGraph>()
                .Where(x => x.Description.Brief.Equals(des.FriendlyName)).Single();
            PpsChart.Children.Remove(ppsline);
        }

        #endregion

        #region Create GUI controls

        private FrameworkElement CreatePieCharts(DeviceStatisticsHelper info)
        {
            Grid chartgrid = new Grid();
            chartgrid.RowDefinitions.Add(new RowDefinition());
            chartgrid.RowDefinitions.Add(new RowDefinition());
            chartgrid.ColumnDefinitions.Add(new ColumnDefinition());
            chartgrid.ColumnDefinitions.Add(new ColumnDefinition());

            Chart chart = new Chart();
            chart.Title = info.Device.FriendlyName + " - Network Layer";
            chart.Style = (Style)FindResource("compactChart");
            chart.LegendStyle = (System.Windows.Style)FindResource("HiddenLegend");
            BarSeries bs = new BarSeries();
            bs.IndependentValuePath = "Key";
            bs.DependentValuePath = "Value";
            bs.ItemsSource = info.NetworkLayer;
            chart.Series.Add(bs);
            chartgrid.Children.Add(chart);
            chart.SetValue(Grid.RowProperty, 0);
            chart.SetValue(Grid.ColumnProperty, 0);
            chart.SetValue(Grid.ColumnSpanProperty, 2);

            LabeledPieChart pchart = new LabeledPieChart();
            pchart.Style = (Style)FindResource("compactLabeledPieChart");
            LabeledPieSeries ps = new LabeledPieSeries();
            pchart.Title = info.Device.FriendlyName + " - Transport Layer";
            ps.IndependentValuePath = "Key";
            ps.DependentValuePath = "Value";
            ps.ItemsSource = info.TransportLayer;
            ps.PieChartLabelItemTemplate = (DataTemplate)FindResource("pieChartLabelDataTemplate");
            ps.LabelDisplayMode = DisplayMode.AutoMixed;
            pchart.Series.Add(ps);
            chartgrid.Children.Add(pchart);
            pchart.SetValue(Grid.RowProperty, 1);
            pchart.SetValue(Grid.ColumnProperty, 0);

            pchart = new LabeledPieChart();
            pchart.Style = (Style)FindResource("compactLabeledPieChart");
            ps = new LabeledPieSeries();
            pchart.Title = info.Device.FriendlyName + " - Application Layer";
            ps.IndependentValuePath = "Key";
            ps.DependentValuePath = "Value";
            ps.ItemsSource = info.ApplicationLayer;
            ps.PieChartLabelItemTemplate = (DataTemplate)FindResource("pieChartLabelDataTemplate");
            ps.LabelDisplayMode = DisplayMode.AutoMixed;
            pchart.Series.Add(ps);
            chartgrid.Children.Add(pchart);
            pchart.SetValue(Grid.RowProperty, 1);
            pchart.SetValue(Grid.ColumnProperty, 1);

            return chartgrid;
        }

        #endregion

        #region Statistics

        private void ReadStatistics(object sender, EventArgs e)
        {
            double tbps = 0;
            double tpps = 0;
            int cnt = 0;

            foreach (var des in Ssource.MonitoringList)
            {
                var devName = des.Name;
                StatisticsInfo info = Ssource.Statistics[devName];
                helpers[devName].ChangeTo(info);

                tbps += info.Bps;
                tpps += info.Pps;
                cnt++;
            }
            AdjustChart();

            tbps /= cnt;
            tpps /= cnt;

            TotalBPS = tbps;
            TotalPPS = tpps;
            Window.FormatBpsSpeed(tbps);
            Window.FormatPpsSpeed(tpps);
        }

        /// <summary>
        /// Move line chart and remove needless points
        /// </summary>
        private void AdjustChart()
        {

        }



        #endregion

        #region Event Handlers

        private void DetialMonitorPage_Loaded(object sender, RoutedEventArgs e)
        {
            
        }

        private void MonitoringListChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
            case NotifyCollectionChangedAction.Add:
                foreach (DeviceDes des in e.NewItems)
                    SetupDevice(des);
                break;
            case NotifyCollectionChangedAction.Remove:
                foreach (DeviceDes des in e.OldItems)
                    UnSetupDevice(des);
                break;
            default:
                break;
            }
        }

        void Tabs_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            
        }

        #endregion

        #region ITabPage Members
        public void OnTabItemAttached(MainWindow window, TabItem tItem)
        {
            TItem = tItem;
            Window = window;
            Ssource = window.Ssource;

            tItem.Header = Header;
            Ssource.MonitoringList.DeviceListChanged += MonitoringListChanged;
            Window.Tabs.SelectionChanged += Tabs_SelectionChanged;
        }

        public void OnTabItemDetaching(MainWindow window, TabItem tItem)
        {
            Ssource.MonitoringList.DeviceListChanged -= MonitoringListChanged;
        }

        public TabItem TItem { get; private set; }
        public MainWindow Window { get; private set; }
        public string Header { get { return "详细监控"; } }
        public object TypeIdentity { get { return typeIdentity; } }
        #endregion

        #region Statics
        readonly static string typeIdentity;

        static DetialMonitorPage()
        {
            typeIdentity = "TrafficAnalysis.Pages.DetialMonitorPage";
            MainWindow.NoClosePage.Add(typeIdentity);
        }
        #endregion

    }
}
