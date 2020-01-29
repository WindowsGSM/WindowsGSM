using System;
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;

namespace WindowsGSM.GameServer
{
    class MORDHAU
    {
        private readonly Functions.ServerConfig _serverData;

        public string Error;
        public string Notice;

        public const string FullName = "Mordhau Dedicated Server";
        public bool ToggleConsole = false;

        public string port = "7777";
        public string defaultmap = "FFA_ThePit";
        public string maxplayers = "16";
        public string additional = "-BeaconPort={{beaconport}} -QueryPort={{queryport}}";

        public MORDHAU(Functions.ServerConfig serverData)
        {
            _serverData = serverData;
        }

        public async void CreateServerCFG()
        {
            string shipExePath = Functions.Path.GetServerFiles(_serverData.ServerID, @"Mordhau\Binaries\Win64\MordhauServer-Win64-Shipping.exe");
            WindowsFirewall firewall = new WindowsFirewall("MordhauServer-Win64-Shipping.exe", shipExePath);
            if (!await firewall.IsRuleExist())
            {
                firewall.AddRule();
            }

            //Run MordhauServer.exe to let it create the default files
            Process p = new Process
            {
                StartInfo =
                {
                    WorkingDirectory = Functions.Path.GetServerFiles(_serverData.ServerID),
                    FileName = shipExePath,
                    WindowStyle = ProcessWindowStyle.Minimized,
                    CreateNoWindow = true,
                    UseShellExecute = false
                }
            };
            p.Start();

            string configPath = Functions.Path.GetServerFiles(_serverData.ServerID, @"Mordhau\Saved\Config\WindowsServer\Game.ini");
            await Task.Run(() =>
            {
                int tries = 0;
                while (!File.Exists(configPath) && tries < 100)
                {
                    tries++;
                    Task.Delay(500);
                }
            });

            if (!p.HasExited)
            {
                p.Kill();
            }

            //Download Game.ini
            if (await Functions.Github.DownloadGameServerConfig(configPath, FullName, "Game.ini"))
            {
                string configText = File.ReadAllText(configPath);
                configText = configText.Replace("{{hostname}}", _serverData.ServerName);
                configText = configText.Replace("{{rcon_password}}", _serverData.GetRCONPassword());
                File.WriteAllText(configPath, configText);
            }

            //Edit WindowsGSM.cfg
            string configFile = Functions.Path.GetConfigs(_serverData.ServerID, "WindowsGSM.cfg");
            if (File.Exists(configFile))
            {
                string configText = File.ReadAllText(configFile);
                configText = configText.Replace("{{beaconport}}", (Int32.Parse(_serverData.ServerPort) + 7223).ToString());
                configText = configText.Replace("{{queryport}}", (Int32.Parse(_serverData.ServerPort) + 19238).ToString());
                File.WriteAllText(configFile, configText);
            }     
        }

        public async Task<Process> Start()
        {
            string exeFile = "MordhauServer.exe";
            string exePath = Functions.Path.GetServerFiles(_serverData.ServerID, exeFile);
            if (!File.Exists(exePath))
            {
                Error = $"{exeFile} not found ({exePath})";
                return null;
            }

            string shipExePath = Functions.Path.GetServerFiles(_serverData.ServerID, @"Mordhau\Binaries\Win64\MordhauServer-Win64-Shipping.exe");
            WindowsFirewall firewall = new WindowsFirewall("MordhauServer-Win64-Shipping.exe", shipExePath);
            if (!await firewall.IsRuleExist())
            {
                firewall.AddRule();
            }

            if (!File.Exists(shipExePath))
            {
                Error = $"MordhauServer-Win64-Shipping.exe not found ({shipExePath})";
                return null;
            }

            string configPath = Functions.Path.GetServerFiles(_serverData.ServerID, @"Mordhau\Saved\Config\WindowsServer\Game.ini");
            if (!File.Exists(configPath))
            {
                Notice = $"Game.ini not found ({configPath})";
            }

            string param = $"Mordhau {_serverData.ServerMap} -log -MultiHome={_serverData.ServerIP} -Port={_serverData.ServerPort} {_serverData.ServerParam}";

            Process p = new Process
            {
                StartInfo =
                {
                    WorkingDirectory = Functions.Path.GetServerFiles(_serverData.ServerID),
                    FileName = shipExePath,
                    Arguments = param,
                    WindowStyle = ProcessWindowStyle.Minimized,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                },
                EnableRaisingEvents = true
            };
            var serverConsole = new Functions.ServerConsole(_serverData.ServerID);
            p.OutputDataReceived += serverConsole.AddOutput;
            p.ErrorDataReceived += serverConsole.AddOutput;
            p.Start();
            p.BeginOutputReadLine();
            p.BeginErrorReadLine();

            return p;
        }

        public async Task<bool> Stop(Process p)
        {
            p.CloseMainWindow();

            for (int i = 0; i < 10; i++)
            {
                if (p.HasExited) { return true; }
                await Task.Delay(1000);
            }

            return false;
        }

        public async Task<Process> Install()
        {
            Steam.SRCDS srcds = new Steam.SRCDS(_serverData.ServerID);
            Process p = await srcds.Install("629800");
            Error = srcds.Error;

            return p;
        }

        public async Task<bool> Update()
        {
            Steam.SRCDS srcds = new Steam.SRCDS(_serverData.ServerID);
            bool success = await srcds.Update("629800");
            Error = srcds.Error;

            return success;
        }

        public bool IsInstallValid()
        {
            string exeFile = "MordhauServer.exe";
            string exePath = Functions.Path.GetServerFiles(_serverData.ServerID, exeFile);

            return File.Exists(exePath);
        }

        public bool IsImportValid(string path)
        {
            string exeFile = "MordhauServer.exe";
            string exePath = Path.Combine(path, exeFile);

            Error = $"Invalid Path! Fail to find {exeFile}";
            return File.Exists(exePath);
        }

        public string GetLocalBuild()
        {
            var steamCMD = new Installer.SteamCMD();
            return steamCMD.GetLocalBuild(_serverData.ServerID, "629800");
        }

        public async Task<string> GetRemoteBuild()
        {
            var steamCMD = new Installer.SteamCMD();
            return await steamCMD.GetRemoteBuild("629800");
        }
    }
}
