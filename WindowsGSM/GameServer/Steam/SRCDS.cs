using System;
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace WindowsGSM.GameServer.Steam
{
    class SRCDS
    {
        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        private readonly string _serverId;

        public string Error;

        public SRCDS(string serverId)
        {
            _serverId = serverId;
        }

        public async Task<Process> Start(string param, string customSrcdsName = "srcds.exe", bool setWorkingDirectory = true)
        {
            string workingDir = Functions.Path.GetServerFiles(_serverId);
            string srcdsPath = Path.Combine(workingDir, customSrcdsName);

            if (!File.Exists(srcdsPath))
            {
                Error = $"{customSrcdsName} not found ({srcdsPath})";
                return null;
            }

            if (string.IsNullOrWhiteSpace(param))
            {
                Error = "Start Parameter not set";
                return null;
            }

            WindowsFirewall firewall = new WindowsFirewall(customSrcdsName, srcdsPath);
            if (!await firewall.IsRuleExist())
            {
                firewall.AddRule();
            }

            Process p = new Process();
            if (setWorkingDirectory)
            {
                p.StartInfo.WorkingDirectory = workingDir;
            }
            p.StartInfo.FileName = srcdsPath;
            p.StartInfo.Arguments = param;
            p.StartInfo.WindowStyle = ProcessWindowStyle.Minimized;
            p.Start();

            return p;
        }

        public static async Task<bool> Stop(Process p, bool sendCloseMessage = false)
        {
            SetForegroundWindow(p.MainWindowHandle);
            if (sendCloseMessage)
            {
                p.CloseMainWindow();
            }
            else
            {
                SendKeys.SendWait("quit");
            }
            SendKeys.SendWait("{ENTER}");
            SetForegroundWindow(Process.GetCurrentProcess().MainWindowHandle);

            for (int i = 0; i < 10; i++)
            {
                if (p != null && p.HasExited)
                {
                    return true;
                }

                await Task.Delay(1000);
            }

            return false;
        }

        public async Task<Process> Install(string appId)
        {
            Installer.SteamCMD steamCMD = new Installer.SteamCMD();
            steamCMD.SetParameter(null, null, Functions.Path.GetServerFiles(_serverId), "", appId, true);

            if (!await steamCMD.Download())
            {
                Error = steamCMD.Error;
                return null;
            }

            Process process = await steamCMD.Run();
            if (process == null)
            {
                Error = steamCMD.Error;
                return null;
            }

            return process;
        }

        public async Task<bool> Update(string appId)
        {
            Installer.SteamCMD steamCMD = new Installer.SteamCMD();
            steamCMD.SetParameter(null, null, Functions.Path.GetServerFiles(_serverId), "", appId, false);

            if (!await steamCMD.Download())
            {
                Error = steamCMD.Error;
                return false;
            }

            Process pSteamCMD = await steamCMD.Run();
            if (pSteamCMD == null)
            {
                Error = steamCMD.Error;
                return false;
            }

            await Task.Run(() => pSteamCMD.WaitForExit());

            if (pSteamCMD.ExitCode != 0)
            {
                Error = "Exit code: " + pSteamCMD.ExitCode.ToString();
                return false;
            }

            return true;
        }
    }
}
