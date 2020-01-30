using System;
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;

namespace WindowsGSM.GameServer
{
    class CS
    {
        private readonly Functions.ServerConfig _serverData;

        public string Error;
        public string Notice;

        public const string FullName = "Counter-Strike: 1.6 Dedicated Server";
        public bool ToggleConsole = false;

        public string port = "27015";
        public string defaultmap = "de_dust2";
        public string maxplayers = "24";
        public string additional = "";

        public CS(Functions.ServerConfig serverData)
        {
            _serverData = serverData;
        }

        public async void CreateServerCFG()
        {
            //Download server.cfg
            string configPath = Functions.Path.GetServerFiles(_serverData.ServerID, @"cstrike\server.cfg");
            if (await Functions.Github.DownloadGameServerConfig(configPath, FullName, "server.cfg"))
            {
                string configText = File.ReadAllText(configPath);
                configText = configText.Replace("{{hostname}}", _serverData.ServerName);
                configText = configText.Replace("{{rcon_password}}", _serverData.GetRCONPassword());
                File.WriteAllText(configPath, configText);
            }

            //Create steam_appid.txt
            string txtPath = Functions.Path.GetServerFiles(_serverData.ServerID, "steam_appid.txt");
            File.WriteAllText(txtPath, "10");
        }

        public async Task<Process> Start()
        {
            string configPath = Functions.Path.GetServerFiles(_serverData.ServerID, @"cstrike\server.cfg");
            if (!File.Exists(configPath))
            {
                Notice = $"server.cfg not found ({configPath})";
            }

            string param = "-console -game cstrike";
            param += String.Format("{0}", String.IsNullOrEmpty(_serverData.ServerIP) ? "" : $" -ip {_serverData.ServerIP}");
            param += String.Format("{0}", String.IsNullOrEmpty(_serverData.ServerPort) ? "" : $" -port {_serverData.ServerPort}");
            param += String.Format("{0}", String.IsNullOrEmpty(_serverData.ServerMaxPlayer) ? "" : $" -maxplayers {_serverData.ServerMaxPlayer}");
            param += String.Format("{0}", String.IsNullOrEmpty(_serverData.ServerGSLT) ? "" : $" +sv_setsteamaccount {_serverData.ServerGSLT}");
            param += String.Format("{0}", String.IsNullOrEmpty(_serverData.ServerParam) ? "" : $" {_serverData.ServerParam}");
            param += String.Format("{0}", String.IsNullOrEmpty(_serverData.ServerMap) ? "" : $" +map {_serverData.ServerMap}");

            Steam.HLDS hlds = new Steam.HLDS(_serverData.ServerID);
            Process p = await hlds.Start(param);
            Error = hlds.Error;

            return p;
        }

        public async Task Stop(Process p)
        {
            await Steam.HLDS.Stop(p);
        }

        public async Task<Process> Install()
        {
            Steam.HLDS hlds = new Steam.HLDS(_serverData.ServerID);
            Process p = await hlds.Install("", "90");
            Error = hlds.Error;

            return p;
        }

        public async Task<bool> Update()
        {
            Steam.HLDS hlds = new Steam.HLDS(_serverData.ServerID);
            bool success = await hlds.Update("", "90");
            Error = hlds.Error;

            return success;
        }

        public bool IsInstallValid()
        {
            string hldsFile = "hlds.exe";
            string hldsPath = Functions.Path.GetServerFiles(_serverData.ServerID, hldsFile);

            return File.Exists(hldsPath);
        }

        public bool IsImportValid(string path)
        {
            string hldsFile = "hlds.exe";
            string hldsPath = Path.Combine(path, hldsFile);

            Error = $"Invalid Path! Fail to find {hldsFile}";
            return File.Exists(hldsPath);
        }

        public string GetLocalBuild()
        {
            var steamCMD = new Installer.SteamCMD();
            return steamCMD.GetLocalBuild(_serverData.ServerID, "10");
        }

        public async Task<string> GetRemoteBuild()
        {
            var steamCMD = new Installer.SteamCMD();
            return await steamCMD.GetRemoteBuildHLDS("10");
        }
    }
}
