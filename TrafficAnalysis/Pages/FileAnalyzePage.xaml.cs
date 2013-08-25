using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Research.DynamicDataDisplay;
using Microsoft.Research.DynamicDataDisplay.Charts;
using Microsoft.Research.DynamicDataDisplay.DataSources;
using Microsoft.Research.DynamicDataDisplay.ViewportRestrictions;
using Microsoft.WindowsAPICodePack.Dialogs;
using TrafficAnalysis.DeviceDataSource;

namespace TrafficAnalysis.Pages
{
    /// <summary>
    /// FileAnalyzePage.xaml 的交互逻辑
    /// </summary>
    public partial class FileAnalyzePage : UserControl, ITabPage
    {
        #region Fields

        private IFileStatisticSource Fsource = new FileAnalyze();

        private string filePath;
        #endregion

        public FileAnalyzePage(string file)
        {
            filePath = file;
            InitializeComponent();
            InitFileAnlyze();
            InitGraph();
        }

        #region Initialize
        private void InitFileAnlyze()
        {
            anaAppSeries.ItemsSource = new Dictionary<string, double>();
            anaNetSeries.ItemsSource = new Dictionary<string, double>();
            anaTransSeries.ItemsSource = new Dictionary<string, double>();
        }

        private void InitGraph()
        {
            TimeLine.Children.Remove(TimeLine.MouseNavigation);
            var axis = TimeLine.MainHorizontalAxis as HorizontalTimeSpanAxis;
            Func<double, string> mapping = val => axis.ConvertFromDouble(val).ToString(@"hh\:mm\:ss\.fff");
            LineMin.XTextMapping = mapping;
            LineMax.XTextMapping = mapping;

            LineMin.PropertyChanged += SelValueChanged;
            LineMax.PropertyChanged += SelValueChanged;

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
        }
        #endregion

        #region Analyze

        #region ProgressValue Property
        /// <summary>
        /// Gets or sets progress value showing in busyindicator.
        /// </summary>
        public int ProgressValue
        {
            get { return (int)GetValue(ProgressValueProperty); }
            set { SetValue(ProgressValueProperty, value); }
        }

        /// <summary>
        /// Identifies ProgressValue dependency property.
        /// </summary>
        public static readonly DependencyProperty ProgressValueProperty =
                            DependencyProperty.Register(
                                            "ProgressValue",
                                            typeof(int),
                                            typeof(FileAnalyzePage),
                                            new UIPropertyMetadata(0));
        #endregion

        /// <summary>
        /// Load file in a background thread and show a busyindicator.
        /// </summary>
        public void Load()
        {
            BackgroundWorker worker = new BackgroundWorker();
            worker.WorkerReportsProgress = true;
            worker.DoWork += (o, ea) =>
            {
                IFileStatisticSource fsource = (IFileStatisticSource)ea.Argument;
                ProgressChangedEventHandler h = (sender, e) =>
                {
                    worker.ReportProgress(e.ProgressPercentage, e.UserState);
                };
                fsource.ProgressChanged += h;
                
                try
                {
                    fsource.Load(filePath);
                }
                finally
                {
                    fsource.ProgressChanged -= h;
                }
            };

            worker.ProgressChanged += (o, e) =>
            {
                ProgressValue = e.ProgressPercentage;
                busyIndicator.BusyContent = e.UserState;
            };

            worker.RunWorkerCompleted += (o, ea) =>
            {
                if (ea.Error != null)
                {
                    MessageBox.Show(ea.Error.Message);
                    MainWindow.CloseDocument.Execute(TItem, this);
                    busyIndicator.IsBusy = false;
                    return;
                }

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

                if (TimeLine.Viewport.Restrictions.Count > 1)
                {
                    TimeLine.Viewport.Restrictions.RemoveAt(1);
                    TimeLineInnerPps.Viewport.Restrictions.RemoveAt(1);
                }
                TimeLine.Viewport.Restrictions.Add(new DomainRestriction(new DataRect(0, 0, w, hb)));
                TimeLineInnerPps.Viewport.Restrictions.Add(new DomainRestriction(new DataRect(0, 0, w, hp)));
                busyIndicator.IsBusy = false;
            };

            busyIndicator.IsBusy = true;
            worker.RunWorkerAsync(Fsource);
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
        #endregion

        #region Event Handlers
        void SelValueChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName.Equals("CurrentValue"))
            {
                var datetimeaxis = TimeLine.MainHorizontalAxis as HorizontalTimeSpanAxis;
                TimeSpan t1 = datetimeaxis.ConvertFromDouble(LineMin.CurrentValue);
                TimeSpan t2 = datetimeaxis.ConvertFromDouble(LineMax.CurrentValue);
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
        }

        
        #endregion

        #region Public
        
        public void ReassembleTCP()
        {
            var dlg = new CommonOpenFileDialog();
            dlg.IsFolderPicker = true;

            var res = dlg.ShowDialog();

            if (res == CommonFileDialogResult.Ok)
            {
                Fsource.TcpStreamReassemble(dlg.FileName);
            }
        }
        #endregion

        #region ITabPage Members
        public void OnTabItemAttached(MainWindow window, TabItem tItem)
        {
            TItem = tItem;
            Window = window;

            tItem.Header = Header;

            if (IncreaseLoadCnt())
                Window.FileAnalyzeTabGroup.Visibility = Visibility.Visible;

            // Load the file async.
            Load();
        }

        public void OnTabItemDetaching(MainWindow window, TabItem tItem)
        {
            if(DecreaseLoadCnt())
                Window.FileAnalyzeTabGroup.Visibility = Visibility.Collapsed;
        }

        public TabItem TItem { get; private set; }
        public MainWindow Window { get; private set; }
        public string Header { get { return System.IO.Path.GetFileName(filePath); } }
        #endregion

        #region Static Members

        // Fix bug: FileAnalyzeTabGroup contextrual ribbon tab donot appear correctly.
        static int loadCnt = 0;
        static bool IncreaseLoadCnt()
        {
            loadCnt++;
            return loadCnt == 1;
        }
        static bool DecreaseLoadCnt()
        {
            loadCnt--;
            return loadCnt == 0;
        }
        // End fix
        #endregion
    }
}
