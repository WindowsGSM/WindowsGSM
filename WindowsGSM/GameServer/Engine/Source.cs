using System;
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Runtime.InteropServices;
using System.Windows.Forms;

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
/// </summary>

namespace WindowsGSM.GameServer.Engine
{
    class Source
    {
        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        public Functions.ServerConfig serverData;

        public string Error;
        public string Notice;

        public string StartPath = "srcds.exe";
        public bool ToggleConsole = false;
        public int PortIncrements = 1;

        public virtual string Port { get { return "27015"; } }
        public virtual string Defaultmap { get { return ""; } }
        public virtual string Maxplayers { get { return "24"; } }
        public virtual string Additional { get { return "-nocrashdialog"; } }

        public virtual string Game { get { return ""; } }
        public virtual string AppId { get { return ""; } }

        public Source(Functions.ServerConfig serverData)
        {
            this.serverData = serverData;
        }

        public async Task<Process> Start()
        {
            string srcdsPath = Functions.ServerPath.GetServerFiles(serverData.ServerID, StartPath);
            if (!File.Exists(srcdsPath))
            {
                Error = $"{StartPath} not found ({srcdsPath})";
                return null;
            }

            string configPath = Functions.ServerPath.GetServerFiles(serverData.ServerID, Game, "cfg/server.cfg");
            if (!File.Exists(configPath))
            {
                Notice = $"server.cfg not found ({configPath})";
            }

            StringBuilder sb = new StringBuilder();
            sb.Append($"-console -game {Game}");
            sb.Append(string.IsNullOrWhiteSpace(serverData.ServerIP) ? "" : $" -ip {serverData.ServerIP}");
            sb.Append(string.IsNullOrWhiteSpace(serverData.ServerPort) ? "" : $" -port {serverData.ServerPort}");
            sb.Append(string.IsNullOrWhiteSpace(serverData.ServerMaxPlayer) ? "" : $" -maxplayers {serverData.ServerMaxPlayer}");
            sb.Append(string.IsNullOrWhiteSpace(serverData.ServerGSLT) ? "" : $" +sv_setsteamaccount {serverData.ServerGSLT}");
            sb.Append(string.IsNullOrWhiteSpace(serverData.ServerParam) ? "" : $" {serverData.ServerParam}");
            sb.Append(string.IsNullOrWhiteSpace(serverData.ServerMap) ? "" : $" +map {serverData.ServerMap}");
            string param = sb.ToString();

            Process p;
            if (ToggleConsole)
            {
                p = new Process
                {
                    StartInfo =
                    {
                        FileName = srcdsPath,
                        Arguments = param,
                        WindowStyle = ProcessWindowStyle.Minimized,
                    },
                    EnableRaisingEvents = true
                };
                p.Start();
            }
            else
            {
                p = new Process
                {
                    StartInfo =
                    {
                        FileName = srcdsPath,
                        Arguments = param,
                        WindowStyle = ProcessWindowStyle.Minimized,
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true
                    },
                    EnableRaisingEvents = true
                };
                var serverConsole = new Functions.ServerConsole(serverData.ServerID);
                p.OutputDataReceived += serverConsole.AddOutput;
                p.ErrorDataReceived += serverConsole.AddOutput;
                p.Start();
                p.BeginOutputReadLine();
                p.BeginErrorReadLine();
            }

            return p;
        }

        public async Task Stop(Process p)
        {
            await Task.Run(() =>
            {
                SetForegroundWindow(p.MainWindowHandle);
                Functions.ServerConsole.SendWaitToMainWindow("quit");
                Functions.ServerConsole.SendWaitToMainWindow("{ENTER}");
            });
        }

        public async void CreateServerCFG()
        {
            //Download server.cfg
            string configPath = Functions.ServerPath.GetServerFiles(serverData.ServerID, Game, "cfg/server.cfg");
            if (await Functions.Github.DownloadGameServerConfig(configPath, serverData.ServerGame))
            {
                string configText = File.ReadAllText(configPath);
                configText = configText.Replace("{{hostname}}", serverData.ServerName);
                configText = configText.Replace("{{rcon_password}}", serverData.GetRCONPassword());
                File.WriteAllText(configPath, configText);
            }
        }

        public async Task<Process> Install()
        {
            var steamCMD = new Installer.SteamCMD();
            Process p = await steamCMD.Install(serverData.ServerID, "", AppId, true);
            Error = steamCMD.Error;

            return p;
        }

        public async Task<bool> Update(bool validate = false)
        {
            var steamCMD = new Installer.SteamCMD();
            bool updateSuccess = await steamCMD.Update(serverData.ServerID, "", AppId, validate);
            Error = steamCMD.Error;

            return updateSuccess;
        }

        public bool IsInstallValid()
        {
            return File.Exists(Functions.ServerPath.GetServerFiles(serverData.ServerID, StartPath));
        }

        public bool IsImportValid(string path)
        {
            Error = $"Invalid Path! Fail to find {StartPath}";
            return File.Exists(Path.Combine(path, StartPath));
        }

        public string GetLocalBuild()
        {
            var steamCMD = new Installer.SteamCMD();
            return steamCMD.GetLocalBuild(serverData.ServerID, AppId);
        }

        public async Task<string> GetRemoteBuild()
        {
            var steamCMD = new Installer.SteamCMD();
            return await steamCMD.GetRemoteBuild(AppId);
        }

        public string GetQueryPort()
        {
            return serverData.ServerPort;
        }
    }
}
