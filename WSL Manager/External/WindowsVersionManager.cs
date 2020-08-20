using Microsoft.Win32;

namespace WSL_Manager.External
{
    public class WindowsVersionManager
    {
        public WindowsVersion CurrentVersion;

        public bool runningDistroCheckSupported;

        public WindowsVersionManager()
        {
            int releaseId = int.Parse(Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion", "ReleaseId", "").ToString());

            if(releaseId == WindowsVersion.V2009.Version)
                CurrentVersion = WindowsVersion.V2009;
            else if (releaseId == WindowsVersion.V2004.Version)
                CurrentVersion = WindowsVersion.V2004;
            else if (releaseId == WindowsVersion.V1909.Version)
                CurrentVersion = WindowsVersion.V1909;
            else if (releaseId == WindowsVersion.V1903.Version)
                CurrentVersion = WindowsVersion.V1903;
            else if (releaseId == WindowsVersion.V1809.Version)
                CurrentVersion = WindowsVersion.V1809;
            else if (releaseId == WindowsVersion.V1803.Version)
                CurrentVersion = WindowsVersion.V1803;
            else
                CurrentVersion = new WindowsVersion(releaseId);


            if (CurrentVersion.Version >= WindowsVersion.V1903.Version)
                runningDistroCheckSupported = true;
            else
                runningDistroCheckSupported = false;
        }
    }
}
