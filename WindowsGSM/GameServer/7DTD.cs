using System;
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using System.Runtime.InteropServices;

namespace WindowsGSM.GameServer
{
    /// <summary>
    /// 
    /// Notes:
    /// 7 Days to Die Dedicated Server has a special console template which, when RedirectStandardOutput=true, the console output is still working.
    /// The console output seems have 3 channels, RedirectStandardOutput catch the first channel, RedirectStandardError catch the second channel, the third channel left on the game server console.
    /// Moreover, it has his input bar on the bottom so a normal sendkey method is not working.
    /// We need to send a {TAB} => (Send text) => {TAB} => (Send text) => {ENTER} to make the input cursor is on the input bar and send the command successfully.
    /// 
    /// RedirectStandardInput:  NO WORKING
    /// RedirectStandardOutput: YES (Used)
    /// RedirectStandardError:  YES (Used)
    /// SendKeys Input Method:  YES (Used)
    /// 
    /// There are two methods to shutdown this special server
    /// 1. {TAB} => (Send shutdown) => {TAB} => (Send shutdown) => {ENTER}
    /// 2. p.CloseMainWindow(); => {ENTER}
    /// 
    /// The second one is used.
    /// 
    /// </summary>
    class _7DTD
    {
        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        private readonly string _serverId;

        private string _param;
        public string Error;
        public string Notice;

        public const string FullName = "7 Days to Die Dedicated Server";
        public const bool ToggleConsole = true;

        public string port = "26900";
        public string defaultmap = "Navezgane";
        public string maxplayers = "8";
        public string additional = "";

        public _7DTD(string serverid)
        {
            _serverId = serverid;
        }

        public async void CreateServerCFG(string hostname, string rcon_password, string port)
        {
            //Download serverconfig.xml
            string configPath = Functions.Path.GetServerFiles(_serverId, "serverconfig.xml");
            if (await Functions.Github.DownloadGameServerConfig(configPath, FullName, "serverconfig.xml"))
            {
                string configText = File.ReadAllText(configPath);
                configText = configText.Replace("{{hostname}}", hostname);
                configText = configText.Replace("{{rcon_password}}", rcon_password);
                configText = configText.Replace("{{port}}", port);
                configText = configText.Replace("{{telnetPort}}", (Int32.Parse(port) - Int32.Parse(this.port) + 8081).ToString());
                configText = configText.Replace("{{maxplayers}}", maxplayers);
                File.WriteAllText(configPath, configText);
            }

            //Create steam_appid.txt
            string txtPath = Functions.Path.GetServerFiles(_serverId, "steam_appid.txt");
            File.WriteAllText(txtPath, "251570");
        }

        public void SetParameter(string additional)
        {
            _param = $"start 7DaysToDieServer -quit -batchmode -nographics -configfile=serverconfig.xml -dedicated {additional}";
        }

        public async Task<Process> Start()
        {
            string exeName = "7DaysToDieServer.exe";
            string workingDir = Functions.Path.GetServerFiles(_serverId);
            string exePath = Path.Combine(workingDir, exeName);

            if (!File.Exists(exePath))
            {
                Error = $"{exeName} not found ({exePath})";
                return null;
            }

            string configPath = Functions.Path.GetServerFiles(_serverId, "serverconfig.xml");
            if (!File.Exists(configPath))
            {
                Notice = $"serverconfig.xml not found ({configPath})";
            }

            WindowsFirewall firewall = new WindowsFirewall(exeName, exePath);
            if (!await firewall.IsRuleExist())
            {
                firewall.AddRule();
            }

            Process p = new Process
            {
                StartInfo =
                {
                    FileName = exePath,
                    Arguments = _param,
                    WindowStyle = ProcessWindowStyle.Minimized,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                },
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
            p.CloseMainWindow();
            SendKeys.SendWait("{ENTER}");
            SetForegroundWindow(Process.GetCurrentProcess().MainWindowHandle);

            for (int i = 0; i < 10; i++)
            {
                if (p.HasExited) { return true; }
                await Task.Delay(1000);
            }

            return false;
        }

        public async Task<Process> Install()
        {
            Steam.SRCDS srcds = new Steam.SRCDS(_serverId);
            Process p = await srcds.Install("294420");
            Error = srcds.Error;

            return p;
        }

        public async Task<bool> Update()
        {
            Steam.SRCDS srcds = new Steam.SRCDS(_serverId);
            bool success = await srcds.Update("294420");
            Error = srcds.Error;

            return success;
        }
    }
}
