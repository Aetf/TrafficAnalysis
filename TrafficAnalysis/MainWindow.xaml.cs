using System;
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

namespace TrafficAnalysis
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : RibbonWindow
    {
        #region Fields
        private IStatisticsSource Ssource = new MonitorPcap();

        private IFileStatisticSource Fsource = new FileAnalyze();

        private Dictionary<string, DeviceStatisticsHelper> helpers = new Dictionary<string, DeviceStatisticsHelper>();

        private DispatcherTimer refreshTimer;

        private double chartwidthfactor = 50;

        SelectionLine lineMin;

        SelectionLine lineMax;
        #endregion

        public MainWindow()
        {
            InitializeComponent();
        }

        #region Initialize

        private void InitGraphs()
        {
            #region 监控
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
            #endregion

            #region 文件分析
            TimeLine.Children.Remove(TimeLine.MouseNavigation);
            var axis = TimeLine.MainHorizontalAxis as HorizontalTimeSpanAxis;

            lineMin = new SelectionLine
            {
                LineStrokeThickness = 3.5,
                XTextMapping = val => axis.ConvertFromDouble(val).ToString(@"hh\:mm\:ss\.fff")
            };
            lineMin.PropertyChanged += SelLineMin_Changed;

            lineMax = new SelectionLine
            {
                LineStrokeThickness = 3.5,
                XTextMapping = val => axis.ConvertFromDouble(val).ToString(@"hh\:mm\:ss\.fff")
            };
            lineMax.PropertyChanged += SelLineMax_Changed;

            TimeLine.Children.Add(lineMin);
            TimeLine.Children.Add(lineMax);

            //TimeLineInnerPps.ViewportBindingConverter = new InjectedPlotterHorizontalSyncConverter(TimeLineInnerPps);
            //TimeLineInnerPps.SetViewportBinding = true;
            // Note:
            // Above two lines do not work as excepted.
            // TimeLineInnerPps.Viewport.Visible stuck to default value (0,0)-> 1,1
            // after SetViewportBinding = true
            // I haven't figured out the reason.
            // However in TwoIndependentAxis project in DevSample, they work properly.
            // This is a workaround that just use default converter with scaled Y axis.
            TimeLineInnerPps.SetViewportBinding = true;
            
            #endregion
        }

        private void ResetLineChart(ChartPlotter plotter)
        {

        }

        private void InitStatistics()
        {
            refreshTimer = new DispatcherTimer();
            refreshTimer.Interval = TimeSpan.FromMilliseconds(250);
            refreshTimer.Tick += ReadStatistics;
        }

        private void InitFileAnlyze()
        {
            anaAppSeries.ItemsSource = new Dictionary<string, double>();
            anaNetSeries.ItemsSource = new Dictionary<string, double>();
            anaTransSeries.ItemsSource = new Dictionary<string, double>();
        }

        #endregion

        #region Analyze

        private void ReadStatistics(object sender, EventArgs e)
        {
            double tbps = 0;
            double tpps = 0;
            int cnt = 0;

            foreach (var devName in helpers.Keys)
            {
                if (helpers[devName].Shown)
                {
                    StatisticsInfo info = Ssource.Statistics[devName];
                    helpers[devName].ChangeTo(info);

                    tbps += info.Bps;
                    tpps += info.Pps;
                    cnt++;
                }
            }
            AdjustChart();

            tbps /= cnt;
            tpps /= cnt;

            FormatBpsSpeed(tbps);
            FormatPpsSpeed(tpps);
            //bpsLabel.Content = tbps.ToString();
            //ppsLabel.Content = tpps.ToString();
        }

        /// <summary>
        /// Move line chart and remove needless points
        /// </summary>
        private void AdjustChart()
        {

        }

        /// <summary>
        /// Calculate and refresh file info for file analyze
        /// </summary>
        /// <param name="from"></param>
        /// <param name="to"></param>
        private void CalculateInfo(TimeSpan from, TimeSpan to)
        {
            if (!Fsource.FileLoaded)
                return;

            try
            {
                RangeStatisticsInfo info = Fsource.Query(from, to);
#if DEBUG
                Console.WriteLine(info.ToString());
#endif

                TotalPacketsLabel.Content = info.TotalCnt;
                TotalSizeLabel.Content = info.TotalLen + "Bit" + (info.TotalLen == 1 ? "" : "s");
                AverageBPS.Content = info.TotalLen / info.Duration.TotalSeconds;
                AveragePPS.Content = info.TotalCnt / info.Duration.TotalSeconds;
                anaNetSeries.ItemsSource = info.NetworkLayer;
                anaTransSeries.ItemsSource = info.TransportLayer;
                anaAppSeries.ItemsSource = info.ApplicationLayer;
            }
            catch (InvalidOperationException)
            {
                // File	not loaded. Should not happen. Ignore.
            }
        }

        static private string[] bpsUnit = new string[]{"bps", "Kbps", "Mbps", "Gbps", "Tbps", "Pbps", "Ebps"};
        private void FormatBpsSpeed(double bps)
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
        private void FormatPpsSpeed(double pps)
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

        #region Add/Remove Device

        private void SetupDevice(DeviceDes des)
        {
            if (CentralGraph.Children.Count < 1)
            {
                refreshTimer.Start();
                ResetLineChart(BpsChart);
                ResetLineChart(PpsChart);
            }
            Ssource.StartCapture(des);

            AddDeviceToChart(des);
        }

        private void UnSetupDevice(DeviceDes des)
        {
            if (!helpers[des.Name].Shown)
                return;

            RemoveDeviceFromChart(des);

            Ssource.StopCapture(des);

            if (CentralGraph.Children.Count == 0)
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

        #region Event Handlers

        private void RibbonWindow_Loaded(object sender, RoutedEventArgs e)
        {
            DeviceList.ItemsSource = Ssource.DeviceList;

            foreach(DeviceDes des in Ssource.DeviceList)
            {
                helpers[des.Name] = new DeviceStatisticsHelper(BpsChart, PpsChart, des);
                helpers[des.Name].MaxPoints = chartwidthfactor;
            }

            if (Ssource.DeviceList.Count != 0)
                DeviceList.SelectedIndex = 0;

            InitStatistics();
            InitFileAnlyze();

            InitGraphs();
        }

        private void FluxFilePath_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();

            Nullable<bool> res = dlg.ShowDialog();

            if (res == true)
            {
                FluxFilePath.Text = dlg.FileName;
            }
        }

        private void RibbonWindow_Closed(object sender, EventArgs e)
        {
            foreach (DeviceStatisticsHelper helper in helpers.Values)
            {
                if (helper.Shown)
                    UnSetupDevice(helper.Device);
            }
        }

        private void DeviceList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            DeviceDes des = DeviceList.SelectedValue as DeviceDes;
            ShowInMonitor.IsChecked = helpers[des.Name].Shown;
            CaptureThis.IsChecked = helpers[des.Name].CaptureThis;
        }

        private void ShowInMonitor_Checked(object sender, RoutedEventArgs e)
        {
            DeviceDes des = DeviceList.SelectedValue as DeviceDes;
            if (!helpers[des.Name].Shown)
            {
                helpers[des.Name].Shown = true;
                SetupDevice(des);
            }
        }

        private void ShowInMonitor_Unchecked(object sender, RoutedEventArgs e)
        {
            DeviceDes des = DeviceList.SelectedValue as DeviceDes;
            if (helpers[des.Name].Shown)
            {
                UnSetupDevice(des);
                helpers[des.Name].Shown = false;
            }
        }

        private void CaptureThis_Checked(object sender, RoutedEventArgs e)
        {
            DeviceDes des = DeviceList.SelectedValue as DeviceDes;

            foreach (DeviceStatisticsHelper helper in helpers.Values)
            {
                if (helper.CaptureThis)
                    helper.CaptureThis = false;
            }
            helpers[des.Name].CaptureThis = true;
        }

        private void CaptureThis_Unchecked(object sender, RoutedEventArgs e)
        {
            DeviceDes des = DeviceList.SelectedValue as DeviceDes;
            helpers[des.Name].CaptureThis = false;
        }

        private void RibbonButton_Click(object sender, RoutedEventArgs e)
        {
            string path = FluxFilePath.Text;
            try
            {
                Fsource.Load(path);

                TimeLineInnerPps.Children.RemoveAll<LineGraph>();
                TimeLine.Children.RemoveAll<LineGraph>();

                var axis = TimeLine.MainHorizontalAxis as HorizontalTimeSpanAxis;
                double w = axis.ConvertToDouble(Fsource.Latest - Fsource.Earliest);
                double hb = Fsource.BpsList.Max(pair => pair.Value);
                double hp = Fsource.PpsList.Max(pair => pair.Value);

                var bpsds = new EnumerableDataSource<KeyValuePair<TimeSpan, double>>(Fsource.BpsList);
                bpsds.SetXYMapping(pair => new Point(axis.ConvertToDouble(pair.Key), pair.Value));
                var ppsds = new EnumerableDataSource<KeyValuePair<TimeSpan, double>>(Fsource.PpsList);
                ppsds.SetXYMapping(pair => new Point(axis.ConvertToDouble(pair.Key), pair.Value));

                TimeLine.AddLineGraph(bpsds, Colors.Blue, 2, "bps");
                TimeLineInnerPps.AddLineGraph(ppsds, Colors.Red, 2, "pps");

                TimeLineInnerPps.SetVerticalTransform(0, 0, hb, hp);

                // I don't why, but the visible will initially become negative if not do this.
                TimeLine.Viewport.Visible = new DataRect(0, 0, w, hb);

                TimeLine.Viewport.Restrictions.Add(new DomainRestriction(new DataRect(0, 0, w, hb)));
                TimeLineInnerPps.Viewport.Restrictions.Add(new DomainRestriction(new DataRect(0, 0, w, hp)));
            }
            catch (ArgumentException)
            {
                MessageBox.Show("非法的文件格式或文件不存在！");
            }
        }

        private void RibbonSplitButton_Click(object sender, RoutedEventArgs e)
        {
            return;
        }

        private void SelLineMin_Changed(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName.Equals("CurrentValue"))
            {
                RefreshSelectedInterval();
            }
        }

        private void SelLineMax_Changed(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName.Equals("CurrentValue"))
            {
                RefreshSelectedInterval();
            }
        }

        private void RefreshSelectedInterval()
        {
            var datetimeaxis = TimeLine.MainHorizontalAxis as HorizontalTimeSpanAxis;
            TimeSpan t1 = datetimeaxis.ConvertFromDouble(lineMin.CurrentValue);
            TimeSpan t2 = datetimeaxis.ConvertFromDouble(lineMax.CurrentValue);
            if (t2 < t1)
            {
                TimeSpan t3 = t1;
                t1 = t2;
                t2 = t3;
            }

            FromTime.Text = t1.ToString(@"hh\:mm\:ss\.fff");
            ToTime.Text = t2.ToString(@"hh\:mm\:ss\.fff");
            CalculateInfo(t1, t2);
        }

        #endregion
    }
}
