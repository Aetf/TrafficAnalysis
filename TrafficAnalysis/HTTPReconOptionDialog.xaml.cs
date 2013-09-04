using System.Collections.ObjectModel;
using System.Windows;
using TrafficAnalysis.Util;

namespace TrafficAnalysis
{
    /// <summary>
    /// HTTPReconOptionDialog.xaml 的交互逻辑
    /// </summary>
    public partial class HTTPReconOptionDialog : Window
    {
        public HTTPReconOptionDialog()
        {
            InitializeComponent();

            Keywords = new ObservableCollection<string>();

            keywordListBox.ItemsSource = Keywords;
        }

        public ObservableCollection<string> Keywords { get; private set; }

        string keyword { get; set; }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (keywordTextBox.Text.Length == 0)
                return;

            Keywords.Add(keywordTextBox.Text);
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }
    }
}
