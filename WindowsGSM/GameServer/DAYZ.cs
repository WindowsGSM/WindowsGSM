using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;

namespace WindowsGSM.GameServer
{
    /// <summary>
    /// 
    /// Note:
    /// I have tested the input output thing.
    /// 
    /// RedirectStandardInput:  NO WORKING
    /// RedirectStandardOutput: NO WORKING
    /// RedirectStandardError:  NO WORKING
    /// SendKeys Input Method:  NO WORKING
    /// 
    /// Therefore, traditional method is used. ToggleConsole = true;
    /// 
    /// </summary>
    class DAYZ
    {
        private readonly Functions.ServerConfig _serverData;

        public string Error;
        public string Notice;

        public const string FullName = "DayZ Dedicated Server";
        public string StartPath = "DayZServer_x64.exe";
        public bool ToggleConsole = true;
        public int PortIncrements = 2;
        public dynamic QueryMethod = null;

        public string Port = "2302";
        public string QueryPort = "2303";
        public string Defaultmap = "dayzOffline.chernarusplus";
        public string Maxplayers = "60";
        public string Additional = "-config=serverDZ.cfg -doLogs -adminLog -netLog";

        public DAYZ(Functions.ServerConfig serverData)
        {
            _serverData = serverData;
        }

        public async void CreateServerCFG()
        {
            //Download serverDZ.cfg
            string configPath = Functions.ServerPath.GetServersServerFiles(_serverData.ServerID, "serverDZ.cfg");
            if (await Functions.Github.DownloadGameServerConfig(configPath, FullName))
            {
                string configText = File.ReadAllText(configPath);
                configText = configText.Replace("{{hostname}}", _serverData.ServerName);
                configText = configText.Replace("{{maxplayers}}", Maxplayers);
                File.WriteAllText(configPath, configText);
            }
        }

        public async Task<Process> Start()
        {
            // Use DZSALModServer.exe if the exe exist, otherwise use original
            string dzsaPath = Functions.ServerPath.GetServersServerFiles(_serverData.ServerID, "DZSALModServer.exe");
            if (File.Exists(dzsaPath))
            {
                StartPath = "DZSALModServer.exe";

                WindowsFirewall firewall = new WindowsFirewall(StartPath, dzsaPath);
                if (!await firewall.IsRuleExist())
                {
                    firewall.AddRule();
                }
            }
            else
            {
                string serverPath = Functions.ServerPath.GetServersServerFiles(_serverData.ServerID, StartPath);
                if (!File.Exists(serverPath))
                {
                    Error = $"{StartPath} not found ({serverPath})";
                    return null;
                }
            }

            string configPath = Functions.ServerPath.GetServersServerFiles(_serverData.ServerID, "serverDZ.cfg");
            if (!File.Exists(configPath))
            {
                Notice = $"{Path.GetFileName(configPath)} not found ({configPath})";
            }

            string param = $" {_serverData.ServerParam}";
            param += string.IsNullOrEmpty(_serverData.ServerIP) ? "" : $" -ip={_serverData.ServerIP}";
            param += string.IsNullOrEmpty(_serverData.ServerPort) ? "" : $" -port={_serverData.ServerPort}";

            string modPath = Functions.ServerPath.GetServersConfigs(_serverData.ServerID, "DayZActivatedMods.cfg");
            if (File.Exists(modPath))
            {
                string modParam = "";
                foreach (string modName in File.ReadLines(modPath))
                {
                    modParam += $"{modName.Trim()};";
                }

                if (!string.IsNullOrWhiteSpace(modParam))
                {
                    param += $" \"-mod={modParam}\"";
                }
            }

            Process p = new Process
            {
                StartInfo =
                {
                    WorkingDirectory = Functions.ServerPath.GetServersServerFiles(_serverData.ServerID),
                    FileName = Functions.ServerPath.GetServersServerFiles(_serverData.ServerID, StartPath),
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
                p.Kill();
            });
        }

        public async Task<Process> Install()
        {
            var steamCMD = new Installer.SteamCMD();
            Process p = await steamCMD.Install(_serverData.ServerID, "", "223350", true, loginAnonymous: false);
            Error = steamCMD.Error;

            return p;
        }

        public async Task<bool> Update(bool validate = false)
        {
            var steamCMD = new Installer.SteamCMD();
            bool updateSuccess = await steamCMD.Update(_serverData.ServerID, "", "223350", validate, loginAnonymous: false);
            Error = steamCMD.Error;

            return updateSuccess;
        }

        public bool IsInstallValid()
        {
            return File.Exists(Functions.ServerPath.GetServersServerFiles(_serverData.ServerID, StartPath));
        }

        public bool IsImportValid(string path)
        {
            string importPath = Path.Combine(path, StartPath);
            Error = $"Invalid Path! Fail to find {Path.GetFileName(StartPath)}";
            return File.Exists(importPath);
        }

        public string GetLocalBuild()
        {
            var steamCMD = new Installer.SteamCMD();
            return steamCMD.GetLocalBuild(_serverData.ServerID, "223350");
        }

        public async Task<string> GetRemoteBuild()
        {
            var steamCMD = new Installer.SteamCMD();
            return await steamCMD.GetRemoteBuild("223350");
        }
    }
}
