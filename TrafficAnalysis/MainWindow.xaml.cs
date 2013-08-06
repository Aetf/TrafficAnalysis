using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.DataVisualization.Charting;
using System.Windows.Controls.Ribbon;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using Controls.DataVisualization.Charting;
using Microsoft.Research.DynamicDataDisplay;
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

        private Dictionary<string, DeviceStatisticsHelper> helpers = new Dictionary<string, DeviceStatisticsHelper>();

        private DispatcherTimer refreshTimer;
        #endregion

        public MainWindow()
        {
            InitializeComponent();
        }

        #region Initialize

        private void InitGraphs()
        {
            ResetLineChart(BpsChart);
            ResetLineChart(PpsChart);
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

        #endregion

        #region Refresh

        private void ReadStatistics(object sender, EventArgs e)
        {
            foreach (var devName in helpers.Keys)
            {
                if (helpers[devName].Shown)
                {
                    StatisticsInfo info = Ssource.Statistics[devName];
                    helpers[devName].ChangeTo(info);
                }
            }
            AdjustChart();
        }

        /// <summary>
        /// Move line chart and remove needless points
        /// </summary>
        private void AdjustChart()
        {

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
            }
        }

        protected void AddDeviceToChart(DeviceDes des)
        {
            // Pie Chart
            FrameworkElement chart = CreatePieCharts(helpers[des.Name]);
            chart.Tag = des.Name;

            CentralGraph.Children.Add(chart);

            // Line Chart
            BpsChart.AddLineGraph(helpers[des.Name].Bps, ColorGen.GetColor(), 2, des.FriendlyName);
            PpsChart.AddLineGraph(helpers[des.Name].Pps, ColorGen.GetColor(), 2, des.FriendlyName);
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
            pchart.Title = "Application Layer";
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
            }

            if (Ssource.DeviceList.Count != 0)
                DeviceList.SelectedIndex = 0;

            InitStatistics();

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

        #endregion
    }
}
