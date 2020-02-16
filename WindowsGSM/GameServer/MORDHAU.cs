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
        public string StartPath = @"Mordhau\Binaries\Win64\MordhauServer-Win64-Shipping.exe";
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
            string configPath = Functions.ServerPath.GetServerFiles(_serverData.ServerID, @"Mordhau\Saved\Config\WindowsServer\Game.ini");
            Directory.CreateDirectory(Path.GetDirectoryName(configPath));

            //Download Game.ini
            if (await Functions.Github.DownloadGameServerConfig(configPath, FullName))
            {
                string configText = File.ReadAllText(configPath);
                configText = configText.Replace("{{hostname}}", _serverData.ServerName);
                configText = configText.Replace("{{rcon_password}}", _serverData.GetRCONPassword());
                File.WriteAllText(configPath, configText);
            }

            //Edit WindowsGSM.cfg
            string configFile = Functions.ServerPath.GetConfigs(_serverData.ServerID, "WindowsGSM.cfg");
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
            string exePath = Functions.ServerPath.GetServerFiles(_serverData.ServerID, exeFile);
            if (!File.Exists(exePath))
            {
                Error = $"{exeFile} not found ({exePath})";
                return null;
            }

            string shipExeFile = "MordhauServer-Win64-Shipping.exe";
            string shipExePath = Functions.ServerPath.GetServerFiles(_serverData.ServerID, @"Mordhau\Binaries\Win64", shipExeFile);
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
            string configPath = Functions.ServerPath.GetServerFiles(_serverData.ServerID, @"Mordhau\Saved\Config\WindowsServer", configFile);
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
                p.Kill();
            });
        }

        public async Task<Process> Install()
        {
            var steamCMD = new Installer.SteamCMD();
            Process p = await steamCMD.Install(_serverData.ServerID, "", "629800");
            Error = steamCMD.Error;

            return p;
        }

        public async Task<bool> Update(bool validate = false)
        {
            var steamCMD = new Installer.SteamCMD();
            bool updateSuccess = await steamCMD.Update(_serverData.ServerID, "", "629800", validate);
            Error = steamCMD.Error;

            return updateSuccess;
        }

        public bool IsInstallValid()
        {
            string exeFile = "MordhauServer.exe";
            string exePath = Functions.ServerPath.GetServerFiles(_serverData.ServerID, exeFile);

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

        public string GetQueryPort()
        {
            string configFile = Functions.ServerPath.GetConfigs(_serverData.ServerID, "WindowsGSM.cfg");

            //Get -QueryPort value in serverparam
            if (File.Exists(configFile))
            {
                string[] lines = File.ReadAllLines(configFile);

                foreach (string line in lines)
                {
                    Debug.WriteLine(line);

                    string[] keyvalue = line.Split(new char[] { '=' }, 2);
                    if (keyvalue.Length == 2)
                    {
                        if ("serverparam" == keyvalue[0])
                        {
                            string param = keyvalue[1].Trim('\"');
                            string[] settings = param.Split(' ');

                            foreach (string setting in settings)
                            {
                                string[] key = setting.Split(new char[] { '=' }, 2);
                                if (key.Length == 2)
                                {
                                    if ("-QueryPort" == key[0])
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
