using System;
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;

namespace WindowsGSM.GameServer
{
    /// <summary>
    /// 
    /// Note:
    /// Mordhau Dedicated Server is very special, the console doesn't allow any input, only output.
    /// Server owner are required to port forward three ports which is unique.
    /// 
    /// RedirectStandardOutput works, but there is a problem figure out by ! AssaultLine who is a server owner of Mordhau community.
    /// when changing a map to a custom map, the redirect output is stucked in p.OutputDataReceived and the whole WindowsGSM and Mordhau freeze.
    /// Therefore, I give up to use RedirectStandardOutput and use the traditional method to handle this server which is ToggleConsole=true.
    /// Although this may not cool as the server output is not within WindowsGSM, but that is the only choice to keep Mordhau stable.
    /// 
    /// The freezing issue is cause by heavy loading of custom map and output deadlocked, I think there is no fix for this until Mordhau developer fix the between output and load.
    /// The issue can reproduce by change ToggleConsole=false. Then start a server and join the server, type ChangeMap <custommap> command in the terminal in Mordhau, the freeze issue occur.
    /// 
    /// </summary>
    class MORDHAU
    {
        private readonly Functions.ServerConfig _serverData;

        public string Error;
        public string Notice;

        public const string FullName = "Mordhau Dedicated Server";
        public bool ToggleConsole = true;

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
                    WindowStyle = ProcessWindowStyle.Hidden,
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

            string shipExeFile = "MordhauServer-Win64-Shipping.exe";
            string shipExePath = Functions.Path.GetServerFiles(_serverData.ServerID, @"Mordhau\Binaries\Win64", shipExeFile);
            WindowsFirewall firewall = new WindowsFirewall(shipExeFile, shipExePath);
            if (!await firewall.IsRuleExist())
            {
                firewall.AddRule();
            }

            if (!File.Exists(shipExePath))
            {
                Error = $"{shipExeFile} not found ({shipExePath})";
                return null;
            }

            string configFile = "Game.ini";
            string configPath = Functions.Path.GetServerFiles(_serverData.ServerID, @"Mordhau\Saved\Config\WindowsServer", configFile);
            if (!File.Exists(configPath))
            {
                Notice = $"{configFile} not found ({configPath})";
            }

            string param = string.IsNullOrWhiteSpace(_serverData.ServerMap) ? "" : $"{_serverData.ServerMap}";
            param += string.IsNullOrWhiteSpace(_serverData.ServerIP) ? "" : $" -MultiHome={_serverData.ServerIP}";
            param += string.IsNullOrWhiteSpace(_serverData.ServerPort) ? "" : $" -Port={_serverData.ServerPort}";
            param += $" {_serverData.ServerParam}" + ((ToggleConsole) ? " -log" : "");

            Process p;
            if (ToggleConsole)
            {
                p = new Process
                {
                    StartInfo =
                    {
                        WorkingDirectory = Functions.Path.GetServerFiles(_serverData.ServerID),
                        FileName = shipExePath,
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
                        WorkingDirectory = Functions.Path.GetServerFiles(_serverData.ServerID),
                        FileName = shipExePath,
                        Arguments = param,
                        WindowStyle = ProcessWindowStyle.Hidden,
                        CreateNoWindow = true,
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
            }
            
            return p;
        }

        public async Task Stop(Process p)
        {
            await Task.Run(() =>
            {
                if (ToggleConsole)
                {
                    p.CloseMainWindow();
                }
                else
                {
                    p.Kill();
                }
            });
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
