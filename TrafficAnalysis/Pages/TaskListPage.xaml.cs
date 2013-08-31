using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using TrafficAnalysis.DeviceDataSource;

namespace TrafficAnalysis.Pages
{
    /// <summary>
    /// TaskList.xaml 的交互逻辑
    /// </summary>
    public partial class TaskListPage : UserControl, ITabPage
    {
        ObservableCollection<ICaptureDescreption> ccbList = new ObservableCollection<ICaptureDescreption>();

        public TaskListPage()
        {
            InitializeComponent();
            lvCTasks.ItemsSource = ccbList;
            ccbList.CollectionChanged += ccbList_CollectionChanged;
        }

        #region Event Handlers
        void ccbList_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if(e.Action == NotifyCollectionChangedAction.Add)
            {
                CaptureControlBlock ccb = e.NewItems[0] as CaptureControlBlock;
                CommandBindings.Add(new CommandBinding(ccb.CancelTaskCommand,
                    (o, args) =>
                    {
                        ccb.Cancellation.Cancel();
                    }));

                ccb.CaptureTask.ContinueWith(
                    (o) =>
                    {
                        lvCTasks.Dispatcher.Invoke(() => ccbList.Remove(ccb));
                    });
            }
        }

        void window_CaptureTaskCreated(object sender, CaptureEventArgs args)
        {
            ccbList.Add(args.ControlBlock);
        }
        #endregion


        #region ITabPage Members
        public void OnTabItemAttached(MainWindow window, TabItem tItem)
        {
            TItem = tItem;
            Window = window;

            tItem.Header = Header;
            window.CaptureTaskStarted += window_CaptureTaskCreated;
        }

        public void OnTabItemDetaching(MainWindow window, TabItem tItem)
        {
            window.CaptureTaskStarted -= window_CaptureTaskCreated;
        }

        public TabItem TItem { get; private set; }

        public MainWindow Window { get; private set; }

        public string Header { get { return "任务列表"; } }

        public object TypeIdentity { get { return typeIdentity; } }
        #endregion

        #region Statics
        static readonly string typeIdentity;

        static TaskListPage()
        {
            typeIdentity = "TrafficAnalysis.Pages.TaskListPage";
            MainWindow.NoClosePage.Add(typeIdentity);
        }
        #endregion
    }
}
