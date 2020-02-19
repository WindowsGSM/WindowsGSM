using System;
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;

namespace WindowsGSM.GameServer
{
    class ARKSE : Engine.UnrealEngine
    {
        private readonly Functions.ServerConfig _serverData;

        public string Error;
        public string Notice;

        public const string FullName = "ARK: Survival Evolved Dedicated Server";
        public string StartPath = @"ShooterGame\Binaries\Win64\ShooterGameServer.exe";
        public bool ToggleConsole = true;
        public int PortIncrements = 2;

        public string Port = "7777";
        public string Defaultmap = "TheIsland";
        public string Maxplayers = "16";
        public string Additional = "?QueryPort={{queryport}}";

        public ARKSE(Functions.ServerConfig serverData)
        {
            _serverData = serverData;
        }

        public async void CreateServerCFG()
        {
            //No config file seems

            //Edit WindowsGSM.cfg
            string configFile = Functions.ServerPath.GetConfigs(_serverData.ServerID, "WindowsGSM.cfg");
            if (File.Exists(configFile))
            {
                string configText = File.ReadAllText(configFile);
                configText = configText.Replace("{{queryport}}", (int.Parse(_serverData.ServerPort) + 19238).ToString());
                File.WriteAllText(configFile, configText);
            }
        }

        public async Task<Process> Start()
        {
            string shipExePath = Functions.ServerPath.GetServerFiles(_serverData.ServerID, StartPath);
            if (!File.Exists(shipExePath))
            {
                Error = $"{Path.GetFileName(shipExePath)} not found ({shipExePath})";
                return null;
            }

            string param = string.IsNullOrWhiteSpace(_serverData.ServerMap) ? "" : _serverData.ServerMap;
            param += "?listen";
            param += string.IsNullOrWhiteSpace(_serverData.ServerName) ? "" : $"?SessionName=\"{_serverData.ServerName}\"";
            param += string.IsNullOrWhiteSpace(_serverData.ServerIP) ? "" : $"?MultiHome={_serverData.ServerIP}";
            param += string.IsNullOrWhiteSpace(_serverData.ServerPort) ? "" : $"?Port={_serverData.ServerPort}";
            param += string.IsNullOrWhiteSpace(_serverData.ServerMaxPlayer) ? "" : $"?MaxPlayers={_serverData.ServerMaxPlayer}";
            param += $"{_serverData.ServerParam} -server -log";

            Process p = new Process
            {
                StartInfo =
                {
                    FileName = shipExePath,
                    Arguments = param,
                    WindowStyle = ProcessWindowStyle.Minimized,
                    UseShellExecute = false
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
                p.Kill();
            });
        }

        public async Task<Process> Install()
        {
            var steamCMD = new Installer.SteamCMD();
            Process p = await steamCMD.Install(_serverData.ServerID, "", "376030");
            Error = steamCMD.Error;

            return p;
        }

        public async Task<bool> Update(bool validate = false)
        {
            var steamCMD = new Installer.SteamCMD();
            bool updateSuccess = await steamCMD.Update(_serverData.ServerID, "", "376030", validate);
            Error = steamCMD.Error;

            return updateSuccess;
        }

        public bool IsInstallValid()
        {
            return File.Exists(Functions.ServerPath.GetServerFiles(_serverData.ServerID, StartPath));
        }

        public bool IsImportValid(string path)
        {
            string exePath = Path.Combine(path, "PackageInfo.bin");
            Error = $"Invalid Path! Fail to find {Path.GetFileName(exePath)}";
            return File.Exists(exePath);
        }

        public string GetLocalBuild()
        {
            var steamCMD = new Installer.SteamCMD();
            return steamCMD.GetLocalBuild(_serverData.ServerID, "376030");
        }

        public async Task<string> GetRemoteBuild()
        {
            var steamCMD = new Installer.SteamCMD();
            return await steamCMD.GetRemoteBuild("376030");
        }

        public string GetQueryPort()
        {
            string configFile = Functions.ServerPath.GetConfigs(_serverData.ServerID, "WindowsGSM.cfg");

            //Get ?QueryPort value in serverparam
            if (File.Exists(configFile))
            {
                string[] lines = File.ReadAllLines(configFile);

                foreach (string line in lines)
                {
                    string[] keyvalue = line.Split(new char[] { '=' }, 2);
                    if (keyvalue.Length == 2)
                    {
                        if ("serverparam" == keyvalue[0])
                        {
                            string param = keyvalue[1].Trim('\"');
                            string[] settings = param.Split('?');

                            foreach (string setting in settings)
                            {
                                string[] key = setting.Split(new char[] { '=' }, 2);
                                if (key.Length == 2)
                                {
                                    if ("QueryPort" == key[0])
                                    {
                                        return key[1];
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return null;
        }
    }
}
