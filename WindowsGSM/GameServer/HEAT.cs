using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System;

namespace WindowsGSM.GameServer
{
    class HEAT : Engine.Unity
    {
        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        private readonly Functions.ServerConfig _serverData;

        public string Error;
        public string Notice;

        public const string FullName = "Heat Dedicated Server";
        public string StartPath = "Heat.exe";
        public bool ToggleConsole = true;
        public int PortIncrements = 1;
        public dynamic QueryMethod = null;

        public string Port = "7450";
        public string QueryPort = "7450";
        public string Defaultmap = "America";
        public string Maxplayers = "40";
        public string Additional = "";

        public string AppId = "996600";

        public HEAT(Functions.ServerConfig serverData)
        {
            _serverData = serverData;
        }

        public async void CreateServerCFG()
        {
            //Download ServerSettings.cfg
            string configPath = Functions.ServerPath.GetServersServerFiles(_serverData.ServerID, "Configuration", "ServerSettings.cfg");
            if (await Functions.Github.DownloadGameServerConfig(configPath, FullName))
            {
                string configText = File.ReadAllText(configPath);
                configText = configText.Replace("{{ServerName}}", _serverData.ServerName);
                configText = configText.Replace("{{Maxplayers}}", Maxplayers);
                configText = configText.Replace("{{ServerIP}}", _serverData.ServerIP);
                configText = configText.Replace("{{ServerPort}}", _serverData.ServerPort);
                File.WriteAllText(configPath, configText);
            }
        }

        public async Task<Process> Start()
        {
            string exePath = Functions.ServerPath.GetServersServerFiles(_serverData.ServerID, StartPath);
            if (!File.Exists(exePath))
            {
                Error = $"{Path.GetFileName(exePath)} not found ({exePath})";
                return null;
            }

            string serverPath = Functions.ServerPath.GetServersServerFiles(_serverData.ServerID, "Server.exe");
            if (!File.Exists(serverPath))
            {
                Error = $"{Path.GetFileName(serverPath)} not found ({serverPath})";
                return null;
            }

            string param = $"-batchmode -nographics -silent-crashes {_serverData.ServerParam}";

            Process p = new Process
            {
                StartInfo =
                {
                    WorkingDirectory = Functions.ServerPath.GetServersServerFiles(_serverData.ServerID),
                    FileName = serverPath,
                    Arguments = param,
                    WindowStyle = ProcessWindowStyle.Minimized
                },
                EnableRaisingEvents = true
            };
            p.Start();

            return p;
        }

        public async Task Stop(Process p)
        {
            await Task.Run(() =>
            {
                SetForegroundWindow(p.MainWindowHandle);
                Functions.ServerConsole.SendWaitToMainWindow("/shutdown");
                Functions.ServerConsole.SendWaitToMainWindow("{ENTER}");
                Task.Delay(5000); //Wait 5 second for auto close
            });
        }

        public async Task<Process> Install()
        {
            var steamCMD = new Installer.SteamCMD();
            Process p = await steamCMD.Install(_serverData.ServerID, "", AppId, true);
            Error = steamCMD.Error;

            return p;
        }

        public async Task<bool> Update(bool validate = false)
        {
            var steamCMD = new Installer.SteamCMD();
            bool updateSuccess = await steamCMD.Update(_serverData.ServerID, "", AppId, validate);
            Error = steamCMD.Error;

            return updateSuccess;
        }

        public bool IsInstallValid()
        {
            return File.Exists(Functions.ServerPath.GetServersServerFiles(_serverData.ServerID, StartPath));
        }

        public bool IsImportValid(string path)
        {
            Error = $"Invalid Path! Fail to find {Path.GetFileName(StartPath)}";
            return File.Exists(Path.Combine(path, StartPath));
        }

        public string GetLocalBuild()
        {
            var steamCMD = new Installer.SteamCMD();
            return steamCMD.GetLocalBuild(_serverData.ServerID, AppId);
        }

        public async Task<string> GetRemoteBuild()
        {
            var steamCMD = new Installer.SteamCMD();
            return await steamCMD.GetRemoteBuild(AppId);
        }
    }
}
