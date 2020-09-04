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

            CurrentVersion = new WindowsVersion(releaseId);

            if (CurrentVersion.Version >= WindowsVersion.V1903.Version)
                runningDistroCheckSupported = true;
            else
                runningDistroCheckSupported = false;
        }
    }
}
