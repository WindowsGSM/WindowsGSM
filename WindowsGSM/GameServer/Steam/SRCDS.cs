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
    /// srcds.exe works almost perfect on WindowsGSM.
    /// The one that cause it not perfect is RedirectStandardInput=true will cause an Engine Error - CTextConsoleWin32::GetLine: !GetNumberOfConsoleInputEvents
    /// Therefore, SendKeys Input Method is used.
    /// 
    /// RedirectStandardInput:  NO WORKING
    /// RedirectStandardOutput: YES (Used)
    /// RedirectStandardError:  YES (Used)
    /// SendKeys Input Method:  YES (Used)
    /// 
    /// Classes that used SRCDS.cs for Start
    /// 
    /// CSGO.cs
    /// GMOD.cs
    /// HL2DM.cs
    /// L4D2.cs
    /// TF2.cs
    /// 
    /// </summary>
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

            Process p = new Process
            {
                StartInfo =
                {
                    WorkingDirectory = setWorkingDirectory ? workingDir : "",
                    FileName = srcdsPath,
                    Arguments = param,
                    WindowStyle = ProcessWindowStyle.Hidden,
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

        public static async Task Stop(Process p)
        {
            await Task.Run(() =>
            {
                SetForegroundWindow(p.MainWindowHandle);
                SendKeys.SendWait("quit");
                SendKeys.SendWait("{ENTER}");
                SetForegroundWindow(Process.GetCurrentProcess().MainWindowHandle);
            });
        }

        public async Task<Process> Install(string appId)
        {
            Installer.SteamCMD steamCMD = new Installer.SteamCMD();
            steamCMD.SetParameter(null, null, Functions.Path.GetServerFiles(_serverId), "", appId, true);

            Process process = await steamCMD.Run();
            Error = steamCMD.Error;

            return process;
        }

        public async Task<bool> Update(string appId)
        {
            Installer.SteamCMD steamCMD = new Installer.SteamCMD();
            steamCMD.SetParameter(null, null, Functions.Path.GetServerFiles(_serverId), "", appId, false);

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
