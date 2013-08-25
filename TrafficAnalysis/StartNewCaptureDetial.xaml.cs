using System;
using System.Collections.Generic;
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
using System.Windows.Shapes;
using PcapDotNet.Core;
using TrafficAnalysis.DeviceDataSource;

namespace TrafficAnalysis
{
    /// <summary>
    /// StartNewCaptureDetial.xaml 的交互逻辑
    /// </summary>
    public partial class StartNewCaptureDetial : Window
    {
        DeviceDes des;

        public StartNewCaptureDetial(DeviceDes des)
        {
            this.des = des;
            InitializeComponent();
        }

        public DumpOptions Options { get; private set; }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            TimeSpan durance = TimeSpan.MaxValue;
            if (TotalDurance.Value.HasValue)
            {
                DateTime d = (DateTime) TotalDurance.Value;
                DateTime d2 = new DateTime(d.Year, d.Month, d.Day);
                durance = d - d2;
            }

            Options = new DumpOptions()
            {
                Path = pathBox.Text,
                Count = (useTotalCount.IsChecked ?? false) ? (TotalCnt.Value ?? int.MaxValue) : int.MaxValue,
                Durance = (useTotalDurance.IsChecked ?? false) ? durance : TimeSpan.MaxValue,

            };
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {

        }

        private void browseBtn_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.FileName = "new_capture";
            dlg.DefaultExt = ".pcap";
            dlg.Filter = "Libpcap capture file (.pcap)|*.pcap";

            Nullable<bool> res = dlg.ShowDialog();

            if (res == true)
            {
                pathBox.Text = dlg.FileName;
            }
        }
    }
}
