using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Forms;
using MenuItem = System.Windows.Controls.MenuItem;
using MessageBox = System.Windows.MessageBox;
using Application = System.Windows.Application;
using Control = System.Windows.Controls.Control;
using System.Runtime.InteropServices;
using WSL_Manager.External;
using System.Threading.Tasks;

namespace WSL_Manager
{
    public partial class MainWindow : Window
    {
        public const string WslManagerVersion = "v1.1.1";
        private Updater updater;

        private WindowsVersionManager windowsVersionManager;
        private LxRunOfflineInterface lxRunOfflineInterface;
        private WslInterface wslInterface;

        private List<DistroData> wslDistroDataList;
        private bool allowRefresh = true;
        private DistroData selectedDistroData;

        // 30 sec
        private const int wslConsoleTimeout = 300;

        public MainWindow()
        {
            InitializeComponent();

            updater = new Updater(WslManagerVersion);

            CheckUpdate();

            windowsVersionManager = new WindowsVersionManager();

            if (windowsVersionManager.CurrentVersion.Version < WindowsVersion.V2004.Version)
            {
                MessageBox.Show(this,
                        $@"You are running Windows 10 " + windowsVersionManager.CurrentVersion.Version
                        + " some features may be locked or missing. Update to V2004 or later for full functionality.", "Warning");
            }

            this.Title += " " + WslManagerVersion + " - V" + windowsVersionManager.CurrentVersion.Version + " - "
                + (windowsVersionManager.CurrentVersion.Version >= WindowsVersion.V2004.Version ? "WSL 2" : "WSL 1");

            wslInterface = new WslInterface(windowsVersionManager);
            lxRunOfflineInterface = new LxRunOfflineInterface("External\\LxRunOffline.exe");

            RefreshWslData();

            System.Windows.Threading.DispatcherTimer refreshTimer = new System.Windows.Threading.DispatcherTimer();
            refreshTimer.Tick += refreshTimerTick;
            refreshTimer.Interval = new TimeSpan(0, 0, 30);
            refreshTimer.Start();
        }

        private void CheckUpdate()
        {
            string url = Task.Run(() => updater.CheckForUpdateAsync()).Result;
            if (url != null)
            {
                var response = MessageBox.Show(this,
                "New Version Available. Do you want to download it?", "Update", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (response != MessageBoxResult.Yes)
                    return;
                System.Diagnostics.Process.Start(url);
            }
        }

        public static string GetImageKey(string distroName)
        {
            string eval = (distroName ?? string.Empty).ToUpperInvariant().Trim();

            if (eval.Contains("DEBIAN"))
                return "debian";

            if (eval.Contains("UBUNTU"))
                return "ubuntu";

            if (eval.Contains("SLES") || eval.Contains("SUSE"))
                return "suse";

            if (eval.Contains("KALI"))
                return "kali";

            return "linux";
        }

        private void refreshTimerTick(object sender, EventArgs e)
        {
            if(allowRefresh)
                RefreshWslData();
        }

        private void RefreshWslData()
        {
            if (wslDistroDataList == null)
                wslDistroDataList = new List<DistroData>();

            string[] distroNames = lxRunOfflineInterface.GetDistroList();

            if (distroNames == null)
                return;

            string[] runningDistros = wslInterface.GetRunningDistros();

            foreach(DistroData distroItem in wslDistroDataList.ToList())
            {
                if (!distroNames.Any(distroItem.DistroName.Equals))
                    wslDistroDataList.Remove(distroItem);
            }

            for (int i = 0; i < distroNames.Length; i++)
            {
                if (wslDistroDataList.Any(d => d.DistroName == distroNames[i]))
                {
                    DistroData wslDistroData = wslDistroDataList.Find(d => d.DistroName == distroNames[i]);
                    wslDistroData.DistroWslVersion = lxRunOfflineInterface.GetDistroWslVersion(distroNames[i]);

                    if (windowsVersionManager.runningDistroCheckSupported)
                    {
                        if (runningDistros.Any(distroNames[i].Equals))
                            wslDistroData.DistroState = "Running";
                        else
                            wslDistroData.DistroState = "Stopped";
                    }
                    else
                    {
                        wslDistroData.DistroState = "Unknown";
                    }
                }
                else
                {
                    DistroData wslDistroData = new DistroData();
                    wslDistroData.DistroImage = "icons/" + GetImageKey(distroNames[i]) + ".png";

                    wslDistroData.DistroName = distroNames[i];
                    wslDistroData.DistroWslVersion = lxRunOfflineInterface.GetDistroWslVersion(distroNames[i]);

                    if (windowsVersionManager.runningDistroCheckSupported)
                    {
                        if (runningDistros.Any(distroNames[i].Equals))
                            wslDistroData.DistroState = "Running";
                        else
                            wslDistroData.DistroState = "Stopped";
                    }
                    else
                    {
                        wslDistroData.DistroState = "Unknown";
                    }

                    wslDistroDataList.Add(wslDistroData);
                }
            }
            distroList.ItemsSource = wslDistroDataList;
            distroList.Items.Refresh();
        }

        [DllImport("user32.dll")]
        static extern int SetWindowText(IntPtr hWnd, string text);

        private void StartDistro(string distroName)
        {
            Process p = lxRunOfflineInterface.RunDistro(distroName, false, false);

            if(windowsVersionManager.CurrentVersion.Version >= WindowsVersion.V1903.Version)
            {
                int timeoutCounter = 0;

                while (!(wslInterface.GetRunningDistros().Any(distroName.Equals)) && timeoutCounter < wslConsoleTimeout)
                {
                    System.Threading.Thread.Sleep(100);
                    timeoutCounter++;
                }
                System.Threading.Thread.Sleep(1000);
                SetWindowText(p.MainWindowHandle, distroName + " Console");
            }

            RefreshWslData();
        }

        private void DistroListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            StartDistro(((DistroData)distroList.SelectedItem).DistroName);
        }

        private void Switch_Loaded(object sender, RoutedEventArgs e)
        {
            selectedDistroData = (DistroData)distroList.SelectedItem;

            var menuItem = (MenuItem)e.OriginalSource;

            menuItem.Header = "Switch To WSL " + (selectedDistroData.DistroWslVersion == 1 ? 2 : 1);

            if (windowsVersionManager.CurrentVersion.Version < WindowsVersion.V2004.Version)
                menuItem.IsEnabled = false;
        }

        private void Open_Click(object sender, RoutedEventArgs e)
        {
            StartDistro(selectedDistroData.DistroName);
        }

        private void OpenFolder()
        {
            var startInfo = new ProcessStartInfo(
                System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "explorer.exe"),
                lxRunOfflineInterface.GetDistroDir(selectedDistroData.DistroName))
            {
                UseShellExecute = false,
            };

            Process.Start(startInfo);
        }

        private void OpenFolder_Click(object sender, RoutedEventArgs e)
        {
            OpenFolder();
        }

        private void Explore_Click(object sender, RoutedEventArgs e)
        {
            if (windowsVersionManager.CurrentVersion.Version >= WindowsVersion.V1903.Version)
            {
                lxRunOfflineInterface.RunDistro(selectedDistroData.DistroName, false, true);

                while (!(wslInterface.GetRunningDistros().Any(selectedDistroData.DistroName.Equals)))
                    System.Threading.Thread.Sleep(100);

                var startInfo = new ProcessStartInfo(
                System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "explorer.exe"),
                $@"\\wsl$\{selectedDistroData.DistroName}")
                {
                    UseShellExecute = false,
                };

                Process.Start(startInfo);

                RefreshWslData();
            }
            else
            {
                OpenFolder();
            }
        }

        private void Mount_Click(object sender, RoutedEventArgs e)
        {
            lxRunOfflineInterface.RunDistro(selectedDistroData.DistroName, true, false);
            if (windowsVersionManager.CurrentVersion.Version >= WindowsVersion.V1903.Version)
            {
                while (!(wslInterface.GetRunningDistros().Any(selectedDistroData.DistroName.Equals)))
                    System.Threading.Thread.Sleep(100);
            }

            RefreshWslData();
        }

        private void Rename_Click(object sender, RoutedEventArgs e)
        {
            string folder = lxRunOfflineInterface.GetDistroDir(selectedDistroData.DistroName);

            string newDistroName = Microsoft.VisualBasic.Interaction.InputBox("No spaces or special characters.",
                                   "New Distro Name",
                                   "default",
                                   -1, -1);
            newDistroName = newDistroName.Trim();

            if (newDistroName.Length == 0)
            {
                MessageBox.Show(this, "Empty name", "Error");
                return;
            }

            if (lxRunOfflineInterface.GetDistroList().Any(newDistroName.Equals))
            {
                MessageBox.Show(this, "Distro name: " + newDistroName + " already exists. Please try again.", "Error");
                return;
            }

            wslInterface.TerminateAllDistros();

            lxRunOfflineInterface.UnregisterDistro(selectedDistroData.DistroName);
            lxRunOfflineInterface.RegisterDistro(newDistroName, folder);

            RefreshWslData();
        }

        private void Move_Click(object sender, RoutedEventArgs e)
        {
            string folder = SelectFolderDialog();

            if (folder == null)
                return;

            if (Directory.EnumerateFileSystemEntries(folder).Any())
            {
                MessageBox.Show(this,
                "Make sure the selected directory is empty.",
                "Error");
                return;
            }

            FreezeApp();

            MessageBox.Show(this, "Don't close this window. Moving distro, please wait.", "Warning");

            wslInterface.TerminateAllDistros();

            lxRunOfflineInterface.MoveDistro(selectedDistroData.DistroName, folder);

            MessageBox.Show(this, "Distro moved.", "Success");

            UnFreezeApp();

            RefreshWslData();
        }

        private string SelectFolderDialog()
        {
            using (var fbd = new FolderBrowserDialog())
            {
                DialogResult result = fbd.ShowDialog();
                if (result == System.Windows.Forms.DialogResult.OK && !string.IsNullOrWhiteSpace(fbd.SelectedPath))
                {
                    return fbd.SelectedPath;
                }
            }
            return null;
        }

        private void Duplicate_Click(object sender, RoutedEventArgs e)
        {
            string folder = SelectFolderDialog();

            if (folder == null)
                return;

            if (Directory.EnumerateFileSystemEntries(folder).Any())
            {
                MessageBox.Show(this,
                "Make sure the selected directory is empty.",
                "Error");
                return;
            }

            string newDistroName = Microsoft.VisualBasic.Interaction.InputBox("No spaces or special characters.",
                                               "New Distro Name",
                                               "default",
                                               -1, -1);
            newDistroName = newDistroName.Trim();

            if (newDistroName.Length == 0)
            {
                MessageBox.Show(this, "Empty name", "Error");
                return;
            }

            if (lxRunOfflineInterface.GetDistroList().Any(newDistroName.Equals))
            {
                MessageBox.Show(this, "Distro name: " + newDistroName + " already exists. Please try again.", "Error");
                return;
            }

            FreezeApp();

            MessageBox.Show(this, "Don't close this window. Duplicating distro, please wait.");

            wslInterface.TerminateAllDistros();

            lxRunOfflineInterface.DuplicateDistro(selectedDistroData.DistroName, folder, newDistroName);

            MessageBox.Show(this, "Distro duplicated.");

            UnFreezeApp();

            RefreshWslData();
        }

        private void Export_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Coming soon...");
        }

        private void Switch_Click(object sender, RoutedEventArgs e)
        {
            int targetVer = selectedDistroData.DistroWslVersion == 1 ? 2 : 1;

            var response = MessageBox.Show(this,
                "Are you sure you want to convert: " + selectedDistroData.DistroName + " to WSL version " + targetVer,
                "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (response != MessageBoxResult.Yes)
                return;

            FreezeApp();

            MessageBox.Show(this, "Converting distro. Don't close this window.", "Please Wait");

            wslInterface.TerminateDistro(selectedDistroData.DistroName);
            wslInterface.SetVersion(selectedDistroData.DistroName, targetVer);

            MessageBox.Show(this, "Distro converted.", "Success");

            UnFreezeApp();

            RefreshWslData();
        }

        private void Unregister_Click(object sender, RoutedEventArgs e)
        {
            var response = MessageBox.Show(this,
                "Are you sure you want to unregister this distro?",
                "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (response != MessageBoxResult.Yes)
                return;

            wslInterface.TerminateDistro(selectedDistroData.DistroName);
            lxRunOfflineInterface.UnregisterDistro(selectedDistroData.DistroName);
            RefreshWslData();
        }

        private void Terminate_Click(object sender, RoutedEventArgs e)
        {
            wslInterface.TerminateDistro(selectedDistroData.DistroName);
            RefreshWslData();
        }

        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(this, "Coming Soon...");
            /*var response = MessageBox.Show(this,
                "Are you sure you want to delete: " + selectedDistroData.DistroName,
                "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (response != MessageBoxResult.Yes)
                return;

            FreezeApp();

            wslInterface.TerminateAllDistros();
            string path = lxRunOfflineInterface.GetDistroDir(selectedDistroData.DistroName);

            if (path.Contains("\\AppData\\Local\\Packages\\"))
            {
                MessageBox.Show(this,
                "Can't delete distro from original install location (\\AppData\\Local\\Packages\\). Try deleting from Microsoft Store.",
                "Error");
                UnFreezeApp();
                return;
            }

            System.IO.DirectoryInfo directory = new DirectoryInfo(path);

            lxRunOfflineInterface.UnregisterDistro(selectedDistroData.DistroName);

            directory.Delete(true);

            UnFreezeApp();*/
        }

        private void Properties_Click(object sender, RoutedEventArgs e)
        {
            PropertiesWindow propertiesWindow = new PropertiesWindow(lxRunOfflineInterface, selectedDistroData);
            propertiesWindow.Show();
        }

        private void NewDistro_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("ms-windows-store://search/?query=Linux");
        }

        private void Import_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Coming soon...");
        }

        private void TerminateAll_Click(object sender, RoutedEventArgs e)
        {
            wslInterface.TerminateAllDistros();
            RefreshWslData();
        }

        private void Register_Click(object sender, RoutedEventArgs e)
        {
            string folder = SelectFolderDialog();

            if (folder == null)
                return;

            string newDistroName = Microsoft.VisualBasic.Interaction.InputBox("No spaces or special characters.",
                                               "New Distro Name",
                                               "default",
                                               -1, -1);
            newDistroName = newDistroName.Trim();

            if (newDistroName.Length == 0)
            {
                MessageBox.Show(this, "Empty name", "Error");
                return;
            }

            if (lxRunOfflineInterface.GetDistroList().Any(newDistroName.Equals))
            {
                MessageBox.Show(this, "Distro name: " + newDistroName + " already exists. Please try again.", "Error");
                return;
            }
            wslInterface.TerminateAllDistros();
            lxRunOfflineInterface.RegisterDistro(newDistroName, folder);
            RefreshWslData();
        }

        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            RefreshWslData();
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void LxRunOfflineConsole_Click(object sender, RoutedEventArgs e)
        {
            lxRunOfflineInterface.OpenConsole();
        }

        private void WslConsole_Click(object sender, RoutedEventArgs e)
        {
            wslInterface.OpenConsole();
        }

        private void OverrideToWSL2_Click(object sender, RoutedEventArgs e)
        {
            windowsVersionManager.CurrentVersion.Version = WindowsVersion.V2004.Version;

            MessageBox.Show(this, "Windows version management overridden to WSL 2, only use if you really have WSL 2." +
                " Resets after WSL Manager Restart", "Warning");
        }

        private void Documentation_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(new ProcessStartInfo("https://docs.microsoft.com/en-us/windows/wsl/about") { UseShellExecute = true });
        }

        private void Website_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(new ProcessStartInfo("https://github.com/visdauas/WSL-Manager") { UseShellExecute = true });
        }

        private void UpdateCheck_Click(object sender, RoutedEventArgs e)
        {
            CheckUpdate();
        }

        private void About_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(this,
                    $@"WSL Manager " + WslManagerVersion
                    + $@" by visdauas
Repository: https://github.com/visdauas/WSL-Manager

Original Repository: https://www.github.com/rkttu/WSL-DistroManager
Icons: https://www.icons8.com",
                    "About");
        }

        private void FreezeApp()
        {
            allowRefresh = false;
            EnumVisual(this, false);
        }

        private void UnFreezeApp()
        {
            allowRefresh = true;
            EnumVisual(this, true);

        }

        public void EnumVisual(Visual visual, bool enable)
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(visual); i++)
            {
                Visual childVisual = (Visual)VisualTreeHelper.GetChild(visual, i);
                if(VisualTreeHelper.GetChildrenCount(childVisual) > 0)
                    EnumVisual(childVisual, enable);

                if(childVisual is Control)
                {
                    Control ctrl = (Control)childVisual;
                    ctrl.IsEnabled = enable;
                }
            }
        }
    }
}
