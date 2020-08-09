using System;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using Wsl_Manager.External;
using static WSL_Manager.MainWindow;

namespace WSL_Manager
{
    public partial class PropertiesWindow : Window
    {

        private LxRunOfflineInterface lxRunOfflineInterface;

        public PropertiesWindow(LxRunOfflineInterface lxRunOfflineInterface, DistroItem distroData)
        {
            InitializeComponent();
            this.lxRunOfflineInterface = lxRunOfflineInterface;

            this.Title = distroData.DistroName;
            DistroName.Content = distroData.DistroName;
            DistroImage.Source = new BitmapImage(new Uri(distroData.DistroImage, UriKind.Relative));
            string defaultDistro = lxRunOfflineInterface.GetDefaultDistro();
            DistroDefault.Content = defaultDistro == distroData.DistroName ? "Yes" : "No";
            string distroDir = lxRunOfflineInterface.GetDistroDir(distroData.DistroName);
            DistroLocation.Text = distroDir;
            DistroState.Content = distroData.DistroState;
            DistroWslVersion.Content = distroData.DistroState;
            DistroSize.Content = (DirSize(new DirectoryInfo(distroDir)) / 1024 / 1024) + " MB";
            SummaryText.Text = lxRunOfflineInterface.GetDistroSummary(distroData.DistroName).Replace("\t", "").Replace("  ", "");
        }

        public static long DirSize(DirectoryInfo d)
        {
            long size = 0;
            // Add file sizes.
            FileInfo[] fis = d.GetFiles();
            foreach (FileInfo fi in fis)
            {
                size += fi.Length;
            }
            // Add subdirectory sizes.
            DirectoryInfo[] dis = d.GetDirectories();
            foreach (DirectoryInfo di in dis)
            {
                size += DirSize(di);
            }
            return size;
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
