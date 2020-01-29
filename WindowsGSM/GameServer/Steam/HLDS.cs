using System;
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace WindowsGSM.GameServer.Steam
{
    /// <summary>
    /// 
    /// Notes:
    /// hlds.exe works almost perfect on WindowsGSM. RedirectStandardInput doesn't work on this. Therefore, SendKeys Input Method is used.
    /// 
    /// RedirectStandardInput:  NO WORKING
    /// RedirectStandardOutput: YES (Used)
    /// RedirectStandardError:  YES (Used)
    /// SendKeys Input Method:  YES (Used)
    /// 
    /// Classes that used hlds.cs for Start
    /// 
    /// CS.cs
    /// CSCZ.cs
    /// 
    /// </summary>
    class HLDS
    {
        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        private readonly string _serverId;

        public string Error;

        public HLDS(string serverId)
        {
            _serverId = serverId;
        }

        public async Task<Process> Start(string param)
        {
            string workingDir = Functions.Path.GetServerFiles(_serverId);
            string hldsPath = Path.Combine(workingDir, "hlds.exe");

            if (!File.Exists(hldsPath))
            {
                Error = $"hlds.exe not found ({hldsPath})";
                return null;
            }

            if (string.IsNullOrWhiteSpace(param))
            {
                Error = "Start Parameter not set";
                return null;
            }

            WindowsFirewall firewall = new WindowsFirewall("hlds.exe", hldsPath);
            if (!await firewall.IsRuleExist())
            {
                firewall.AddRule();
            }

            Process p = new Process
            {
                StartInfo =
                {
                    WorkingDirectory = workingDir,
                    FileName = hldsPath,
                    Arguments = param,
                    WindowStyle = ProcessWindowStyle.Minimized,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                },
                EnableRaisingEvents = true
            };
            var serverConsole = new Functions.ServerConsole(_serverId);
            p.OutputDataReceived += serverConsole.AddOutput;
            p.ErrorDataReceived += serverConsole.AddOutput;
            p.Start();
            p.BeginOutputReadLine();
            p.BeginErrorReadLine();

            return p;
        }

        public static async Task<bool> Stop(Process p)
        {
            SetForegroundWindow(p.MainWindowHandle);
            SendKeys.SendWait("quit");
            SendKeys.SendWait("{ENTER}");
            SetForegroundWindow(Process.GetCurrentProcess().MainWindowHandle);

            for (int i = 0; i < 10; i++)
            {
                if (p.HasExited) { return true; }
                await Task.Delay(1000);
            }

            return false;
        }

        public async Task<Process> Install(string appSetConfig, string appId)
        {
            Installer.SteamCMD steamCMD = new Installer.SteamCMD();
            steamCMD.SetParameter(null, null, Functions.Path.GetServerFiles(_serverId), $"+app_set_config {appSetConfig}", appId, true);

            Process process = await steamCMD.Run();
            Error = steamCMD.Error;

            return process;
        }

        public async Task<bool> Update(string appSetConfig, string appId)
        {
            Installer.SteamCMD steamCMD = new Installer.SteamCMD();
            steamCMD.SetParameter(null, null, Functions.Path.GetServerFiles(_serverId), $"+app_set_config {appSetConfig}", appId, false);

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
