using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Wsl_Manager.External
{
    public class WslInterface
    {
        private string wslPath;

        public WslInterface(string wslPath)
        {
            this.wslPath = wslPath;
        }

        private void ExecuteCommand(string command)
        {
            var proc = new ProcessStartInfo();

            proc.UseShellExecute = false;
            proc.FileName = wslPath;
            proc.Verb = "runas";
            proc.Arguments = command;
            proc.WindowStyle = ProcessWindowStyle.Hidden;
            proc.CreateNoWindow = true;

            var process = Process.Start(proc);
            process.WaitForExit();
            process.Close();
        }

        private String ExecuteCommandWithOutput(string command)
        {
            var proc = new ProcessStartInfo();

            proc.UseShellExecute = false;
            proc.FileName = wslPath;
            proc.Verb = "runas";
            proc.Arguments = command;
            proc.WindowStyle = ProcessWindowStyle.Hidden;
            proc.StandardOutputEncoding = Encoding.Unicode;
            proc.RedirectStandardOutput = true;
            proc.CreateNoWindow = true;

            var process = Process.Start(proc);
            string output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();
            process.Close();

            return output;
        }

        /*public void RunDistro(string distroName)
        {
            ExecuteCommand("--distribution " + distroName);
        }*/

        public void TerminateDistro(string distroName)
        {
            ExecuteCommand("--terminate " + distroName);
        }

        public void TerminateAllDistros()
        {
            ExecuteCommand("--shutdown");
        }

        public void SetVersion(string distroName, int targetVersion)
        {
            ExecuteCommand("--set-version " + distroName + " " + targetVersion.ToString());
        }

        public void SetDefaultVersion(int targetVersion)
        {
            ExecuteCommand("--set-default-version " + targetVersion.ToString());
        }

        public string[] GetRunningDistros()
        {
            List<String> output = ExecuteCommandWithOutput("--list --running").Split('\n').Select(p => p.Trim()).ToList();
            output.RemoveAt(0);
            output.RemoveAt(output.Count - 1);
            for (int i = 0; i < output.Count; i++)
            {
                if (output[i].Contains(" "))
                    output[i] = output[i].Split(' ')[0];
            }
            return output.ToArray();
        }

        public void OpenConsole()
        {
            var proc = new ProcessStartInfo();

            proc.FileName = "cmd.exe";
            proc.Verb = "runas";
            proc.Arguments = " /k wsl --help";
            proc.WindowStyle = ProcessWindowStyle.Normal;

            Process.Start(proc);
        }
    }
}
