using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;

namespace WindowsGSM.GameServer
{
    /// <summary>
    /// 
    /// Note:
    /// The server format is similar to MORDHAU server
    /// 
    /// </summary>
    class OLOW : Engine.UnrealEngine
    {
        private readonly Functions.ServerConfig _serverData;

        public string Error;
        public string Notice;

        public const string FullName = "Outlaws of the Old West Dedicated Server";
        public string StartPath = @"Outlaws\Binaries\Win64\OutlawsServer-Win64-Shipping.exe";
        public bool ToggleConsole = false;
        public int PortIncrements = 2;

        public string Port = "27374";
        public string Defaultmap = "/Game/Maps/MainMap/MainMap";
        public string Maxplayers = "24";
        public string Additional = "-Type=PVP -queryport={{queryport}} -ServerPassword=\"\" -AdminPassword=\"\"";

        public OLOW(Functions.ServerConfig serverData)
        {
            _serverData = serverData;
        }

        public async void CreateServerCFG()
        {
            //No server config

            //Edit WindowsGSM.cfg
            string configFile = Functions.ServerPath.GetConfigs(_serverData.ServerID, "WindowsGSM.cfg");
            if (File.Exists(configFile))
            {
                string configText = File.ReadAllText(configFile);
                configText = configText.Replace("{{queryport}}", (Int32.Parse(_serverData.ServerPort) - 359).ToString());
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

            string param = string.IsNullOrWhiteSpace(_serverData.ServerMap) ? "" : $"{_serverData.ServerMap}";
            param += string.IsNullOrWhiteSpace(_serverData.ServerPort) ? "" : $" -port {_serverData.ServerPort}";
            param += string.IsNullOrWhiteSpace(_serverData.ServerName) ? "" : $" -servername \"{_serverData.ServerName}\"";
            param += string.IsNullOrWhiteSpace(_serverData.ServerMaxPlayer) ? "" : $" -PlayerCount {_serverData.ServerMaxPlayer}";
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
            Process p = await steamCMD.Install(_serverData.ServerID, "", "915070");
            Error = steamCMD.Error;

            return p;
        }

        public async Task<bool> Update(bool validate = false)
        {
            var steamCMD = new Installer.SteamCMD();
            bool updateSuccess = await steamCMD.Update(_serverData.ServerID, "", "915070", validate);
            Error = steamCMD.Error;

            return updateSuccess;
        }

        public bool IsInstallValid()
        {
            return File.Exists(Functions.ServerPath.GetServerFiles(_serverData.ServerID, StartPath));
        }

        public bool IsImportValid(string path)
        {
            string exePath = Path.Combine(path, StartPath);
            Error = $"Invalid Path! Fail to find {Path.GetFileName(exePath)}";
            return File.Exists(exePath);
        }

        public string GetLocalBuild()
        {
            var steamCMD = new Installer.SteamCMD();
            return steamCMD.GetLocalBuild(_serverData.ServerID, "915070");
        }

        public async Task<string> GetRemoteBuild()
        {
            var steamCMD = new Installer.SteamCMD();
            return await steamCMD.GetRemoteBuild("915070");
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
                                    if ("-queryport" == key[0])
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
