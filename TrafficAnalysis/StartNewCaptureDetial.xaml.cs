using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Globalization;
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
using TrafficAnalysis.Util;

namespace TrafficAnalysis
{
    /// <summary>
    /// StartNewCaptureDetial.xaml 的交互逻辑
    /// </summary>
    public partial class StartNewCaptureDetial : Window
    {
        public StartNewCaptureDetial(DeviceDes des)
        {
            this.des = des;
            Options = new DumpOptions();

            InitializeComponent();

            filterBinding.ValidationRules.Add(new BerkeleyPacketFilterValidationRule()
            {
                DeviceDescription = des
            });
        }

        DeviceDes des;

        public DumpOptions Options { get; private set; }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            if (!this.IsValid())
                return;

            TimeSpan durance = TimeSpan.MaxValue;
            if (TotalDurance.Value.HasValue)
            {
                DateTime d = (DateTime)TotalDurance.Value;
                DateTime d2 = new DateTime(d.Year, d.Month, d.Day);
                durance = d - d2;
            }

            Options.Count = (useTotalCount.IsChecked ?? false) ? (TotalCnt.Value ?? int.MaxValue) : int.MaxValue;
            Options.Durance = (useTotalDurance.IsChecked ?? false) ? durance : TimeSpan.MaxValue;
            Options.Path = pathBox.Text;

            DialogResult = true;
        }

        private void browseBtn_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();
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

    public class BerkeleyPacketFilterValidationRule : ValidationRule
    {
        public DeviceDes DeviceDescription { get; set; }

        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            var dev = LivePacketDevice.AllLocalMachine.FirstOrDefault(d => d.Name.Equals(DeviceDescription.Name));
            var str = (string)value;

            try
            {
                using (PacketCommunicator communicator = dev.Open(
                    65535, PacketDeviceOpenAttributes.Promiscuous,
                    250
                    ))
                {
                    communicator.CreateFilter(str);
                }
            }
            catch (ArgumentException ex)
            {
                return new ValidationResult(false, ex.Message);
            }
            
            return new ValidationResult(true, null);
        }

    }
}
