using System;
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;

namespace WindowsGSM.GameServer
{
    class GMOD
    {
        private readonly Functions.ServerConfig _serverData;

        public string Error;
        public string Notice;

        public const string FullName = "Garry's Mod Dedicated Server";
        public string StartPath = "srcds.exe";
        public bool ToggleConsole = false;

        public string port = "27015";
        public string defaultmap = "gm_construct";
        public string maxplayers = "24";
        public string additional = "-tickrate 66 +gamemode sandbox +host_workshop_collection";

        public GMOD(Functions.ServerConfig serverData)
        {
            _serverData = serverData;
        }

        public async void CreateServerCFG()
        {
            //Download server.cfg
            string configPath = Functions.Path.GetServerFiles(_serverData.ServerID, @"garrysmod\cfg\server.cfg");
            if (await Functions.Github.DownloadGameServerConfig(configPath, FullName))
            {
                string configText = File.ReadAllText(configPath);
                configText = configText.Replace("{{hostname}}", _serverData.ServerName);
                configText = configText.Replace("{{rcon_password}}", _serverData.GetRCONPassword());
                File.WriteAllText(configPath, configText);
            }
        }

        public async Task<Process> Start()
        {
            string configPath = Functions.Path.GetServerFiles(_serverData.ServerID, @"garrysmod\cfg\server.cfg");
            if (!File.Exists(configPath))
            {
                Notice = $"server.cfg not found ({configPath})";
            }

            string param = "-console -game garrysmod";
            param += String.Format("{0}", String.IsNullOrEmpty(_serverData.ServerIP) ? "" : $" -ip {_serverData.ServerIP}");
            param += String.Format("{0}", String.IsNullOrEmpty(_serverData.ServerPort) ? "" : $" -port {_serverData.ServerPort}");
            param += String.Format("{0}", String.IsNullOrEmpty(_serverData.ServerMaxPlayer) ? "" : $" -maxplayers {_serverData.ServerMaxPlayer}");
            param += String.Format("{0}", String.IsNullOrEmpty(_serverData.ServerGSLT) ? "" : $" +sv_setsteamaccount {_serverData.ServerGSLT}");
            param += String.Format("{0}", String.IsNullOrEmpty(_serverData.ServerParam) ? "" : $" {_serverData.ServerParam}");
            param += String.Format("{0}", String.IsNullOrEmpty(_serverData.ServerMap) ? "" : $" +map {_serverData.ServerMap}");

            Steam.SRCDS srcds = new Steam.SRCDS(_serverData.ServerID);
            //Garry's Mod server should set working directory to null because the workshop data only works when set to false
            Process p = await srcds.Start(param, setWorkingDirectory: false);
            Error = srcds.Error;

            return p;
        }

        public async Task Stop(Process p)
        {
            await Steam.SRCDS.Stop(p);
        }

        public async Task<Process> Install()
        {
            Steam.SRCDS srcds = new Steam.SRCDS(_serverData.ServerID);
            Process p = await srcds.Install("4020");
            Error = srcds.Error;

            return p;
        }

        public async Task<bool> Update()
        {
            Steam.SRCDS srcds = new Steam.SRCDS(_serverData.ServerID);
            bool success = await srcds.Update("4020");
            Error = srcds.Error;

            return success;
        }

        public bool IsInstallValid()
        {
            string srcdsFile = "srcds.exe";
            string srcdsPath = Functions.Path.GetServerFiles(_serverData.ServerID, srcdsFile);

            return File.Exists(srcdsPath);
        }

        public bool IsImportValid(string path)
        {
            string srcdsFile = "srcds.exe";
            string srcdsPath = Path.Combine(path, srcdsFile);

            Error = $"Invalid Path! Fail to find {srcdsFile}";
            return File.Exists(srcdsPath);
        }

        public string GetLocalBuild()
        {
            var steamCMD = new Installer.SteamCMD();
            return steamCMD.GetLocalBuild(_serverData.ServerID, "4020");
        }

        public async Task<string> GetRemoteBuild()
        {
            var steamCMD = new Installer.SteamCMD();
            return await steamCMD.GetRemoteBuild("4020");
        }

        public string GetQueryPort()
        {
            return _serverData.ServerPort;
        }
    }
}
