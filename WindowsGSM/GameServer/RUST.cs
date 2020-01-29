using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;

namespace WindowsGSM.GameServer
{
    /// <summary>
    /// 
    /// Notes:
    /// Rust server is the most user-unfriendly server in my opinion. Both RedirectStandardInput or RedirectStandardOutput cannot use on WindowsGSM.
    /// RedirectStandardOutput is possible but it will break the input, if used both, the server can run successfully but the input become useless again.
    /// 
    /// The solution for this is don't use neither RedirectStandardInput nor RedirectStandardOutput.
    /// Just use the traditional method to handle the server.
    /// 
    /// </summary>
    class RUST
    {
        private readonly Functions.ServerConfig _serverData;

        public string Error;
        public string Notice;

        public const string FullName = "Rust Dedicated Server";
        public bool ToggleConsole = true;

        public string port = "28015";
        public string defaultmap = "Procedural Map";
        public string maxplayers = "50";
        public string additional = "";

        public RUST(Functions.ServerConfig serverData)
        {
            _serverData = serverData;
        }

        public async void CreateServerCFG()
        {
            //Download server.cfg
            string configPath = Functions.Path.GetServerFiles(_serverData.ServerID, "server.cfg");
            if (await Functions.Github.DownloadGameServerConfig(configPath, FullName, "server.cfg"))
            {
                string configText = File.ReadAllText(configPath);
                configText = configText.Replace("{{hostname}}", _serverData.ServerName);
                configText = configText.Replace("{{rcon_password}}", _serverData.GetRCONPassword());
                configText = configText.Replace("{{port}}", _serverData.GetAvailablePort(port));
                File.WriteAllText(configPath, configText);
            }
        }

        public async Task<Process> Start()
        {
            string configPath = Functions.Path.GetServerFiles(_serverData.ServerID, "server.cfg");
            if (!File.Exists(configPath))
            {
                Notice = $"server.cfg not found ({configPath})";
            }

            string workingDir = Functions.Path.GetServerFiles(_serverData.ServerID);
            string srcdsPath = Path.Combine(workingDir, "RustDedicated.exe");

            string param = $"-nographics -batchmode -silent-crashes +server.ip {_serverData.ServerIP} +server.port {_serverData.ServerPort} +server.level \"{_serverData.ServerMap}\" +server.maxplayers {_serverData.ServerMaxPlayer} ";

            foreach (string line in File.ReadLines(configPath))
            {
                param += line + " ";
            }

            param.TrimEnd();

            WindowsFirewall firewall = new WindowsFirewall("RustDedicated.exe", srcdsPath);
            if (!await firewall.IsRuleExist())
            {
                firewall.AddRule();
            }

            Process p = new Process
            {
                StartInfo =
                {
                    WorkingDirectory = workingDir,
                    FileName = srcdsPath,
                    Arguments = param,
                },
                EnableRaisingEvents = true
            };
            p.Start();

            return p;
        }

        public async Task<bool> Stop(Process p)
        {
            return await Steam.SRCDS.Stop(p);
        }

        public async Task<Process> Install()
        {
            Steam.SRCDS srcds = new Steam.SRCDS(_serverData.ServerID);
            Process p = await srcds.Install("258550");
            Error = srcds.Error;

            return p;
        }

        public async Task<bool> Update()
        {
            Steam.SRCDS srcds = new Steam.SRCDS(_serverData.ServerID);
            bool success = await srcds.Update("258550");
            Error = srcds.Error;

            return success;
        }

        public bool IsInstallValid()
        {
            string exeFile = "RustDedicated.exe";
            string exePath = Functions.Path.GetServerFiles(_serverData.ServerID, exeFile);

            return File.Exists(exePath);
        }

        public bool IsImportValid(string path)
        {
            string exeFile = "RustDedicated.exe";
            string exePath = Path.Combine(path, exeFile);

            Error = $"Invalid Path! Fail to find {exeFile}";
            return File.Exists(exePath);
        }

        public string GetLocalBuild()
        {
            var steamCMD = new Installer.SteamCMD();
            return steamCMD.GetLocalBuild(_serverData.ServerID, "258550");
        }

        public async Task<string> GetRemoteBuild()
        {
            var steamCMD = new Installer.SteamCMD();
            return await steamCMD.GetRemoteBuild("258550");
        }
    }
}
