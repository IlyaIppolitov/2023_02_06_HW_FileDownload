using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
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
using LibFileDownload;

namespace FileDownload
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            pbDownload.Minimum = 0;
            pbDownload.Maximum = 1;
            pbDownload.Value = 0;
        }

        public string downloadFileName = @"https://github.com/rodion-m/SystemProgrammingCourse2022/raw/master/files/payments_19mb.zip";
        public string saveFileName = @"D:\FilesToRead\payments_19mb.zip";
        CancellationTokenSource? cts;

        private async void buttonDownload_Click(object sender, RoutedEventArgs e)
        {
            cts = new CancellationTokenSource();
            pbDownload.Value = 0;
            var progressStatus = new DownloadToFile.ProgressStatus();
            progressStatus.Notify += UpdateProgressBar;

            try
            {
                await DownloadToFile.DownloadFileToPathAsync(downloadFileName, saveFileName, cts.Token, progressStatus);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void buttonCancel_Click(object sender, RoutedEventArgs e)
        {
            cts.Cancel();
            cts.Dispose();
        }

        private void UpdateProgressBar(DownloadToFile.ProgressStatus sender, DownloadToFile.ProgressStatusEventArgs e)
        {
            Dispatcher.Invoke(() => pbDownload.Value = e.CurStatus);
            if (pbDownload.Value == pbDownload.Maximum) MessageBox.Show("Загрзука успешно завершена");
        }
    }
}
