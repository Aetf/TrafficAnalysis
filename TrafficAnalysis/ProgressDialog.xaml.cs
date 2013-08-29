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
using System.ComponentModel;

namespace TrafficAnalysis
{
    /// <summary>
    /// ProgressDialog.xaml 的交互逻辑
    /// </summary>
    public partial class ProgressDialog : Window
    {
        public ProgressDialog()
        {
            InitializeComponent();
            _worker.WorkerReportsProgress = true;
            _worker.WorkerSupportsCancellation = true;
        }

        public double ProgressValue
        {
            get { return progressBar.Value; }
            set { progressBar.Value = value; }
        }

        public string ProgressString
        {
            get { return progressText.Text; }
            set { progressText.Text = value; }
        }

        public void AddProgressChangedHandler(ProgressChangedEventHandler h)
        {
            _worker.ProgressChanged += h;
        }

        public void RemoveProgressChangedHandler(ProgressChangedEventHandler h)
        {
            _worker.ProgressChanged -= h;
        }

        private BackgroundWorker _worker = new BackgroundWorker();
        public DoWorkEventHandler Works;

        public new void ShowDialog()
        {
            ShowDialog(null);
            return;
        }

        public void ShowDialog(object argument)
        {
            _worker.DoWork += Works;
            _worker.RunWorkerCompleted += (o, e) => Close();

            Show();

            _worker.RunWorkerAsync(new Tuple<BackgroundWorker, object>(_worker, argument));

            return;
        }
    }
}
