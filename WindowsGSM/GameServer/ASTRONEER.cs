using System.IO;
using System.Diagnostics;
using System.Threading.Tasks;

namespace WindowsGSM.GameServer
{
    class ASTRONEER
    {
        private readonly Functions.ServerConfig _serverData;

        public string Error;
        public string Notice;

        public const string FullName = "Astroneer Dedicated Server";
        public string StartPath = "Astro\\Binaries\\Win64\\AstroServer-Win64-Shipping.exe";
        public bool AllowsEmbedConsole = false;
        public int PortIncrements = 1;
        public dynamic QueryMethod = new Query.A2S();

        public string Port = "7777";
        public string QueryPort = "7777";
        public string Defaultmap = "map";
        public string Maxplayers = "4";
        public string Additional = string.Empty;

        public string AppId = "728470";

        public ASTRONEER(Functions.ServerConfig serverData)
        {
            _serverData = serverData;
        }

        public async void CreateServerCFG()
        {
            //Download ini files
            string AstroServerSettings = Functions.ServerPath.GetServersServerFiles(_serverData.ServerID, "Astro\\Saved\\Config\\WindowsServer\\AstroServerSettings.ini");

            //Checking for saved directory that contains INI files.
            string workingDir = Functions.ServerPath.GetServersServerFiles(_serverData.ServerID);
            if (!Directory.Exists(workingDir + "\\Astro\\Saved"))
            {
                Directory.CreateDirectory(workingDir + "\\Astro\\Saved");
                Directory.CreateDirectory(workingDir + "\\Astro\\Saved\\Config");
                Directory.CreateDirectory(workingDir + "\\Astro\\Saved\\Config\\WindowsServer");
            }

            if (await Functions.Github.DownloadGameServerConfig(AstroServerSettings, FullName))
            {
                string configText = File.ReadAllText(AstroServerSettings);
                configText = configText.Replace("{{serverip}}", _serverData.ServerIP);
                configText = configText.Replace("{{console_pw}}", _serverData.GetRCONPassword());
                File.WriteAllText(AstroServerSettings, configText);
            }

            string Engine = Functions.ServerPath.GetServersServerFiles(_serverData.ServerID, "Astro\\Saved\\Config\\WindowsServer\\Engine.ini");
            if (await Functions.Github.DownloadGameServerConfig(Engine, FullName))
            {
                string configText = File.ReadAllText(Engine);
                configText = configText.Replace("{{port}}", _serverData.ServerPort);
                File.WriteAllText(Engine, configText);
            }
        }

        public async Task<Process> Start()
        {
            string AstroServerSettings = Functions.ServerPath.GetServersServerFiles(_serverData.ServerID, "Astro\\Saved\\Config\\WindowsServer\\AstroServerSettings.ini");
            string Engine = Functions.ServerPath.GetServersServerFiles(_serverData.ServerID, "Astro\\Saved\\Config\\WindowsServer\\Engine.ini");

            if (!File.Exists(AstroServerSettings) && !File.Exists(Engine))
            {
                Notice = $"Server ini files not found ({AstroServerSettings} or {Engine})";
                CreateServerCFG();
            }

            string workingDir = Functions.ServerPath.GetServersServerFiles(_serverData.ServerID);
            string runPath = Path.Combine(workingDir, "Astro\\Binaries\\Win64\\AstroServer-Win64-Shipping.exe");

            string param = "-log";
            QueryPort = Port;

            param += $" {_serverData.ServerParam}";

            Process p = new Process
            {
                StartInfo =
                {
                    WorkingDirectory = workingDir,
                    FileName = runPath,
                    Arguments = param.TrimEnd(),
                    WindowStyle = ProcessWindowStyle.Minimized,
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true

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
                Functions.ServerConsole.SendMessageToMainWindow(p.MainWindowHandle, "quit");
            });
        }

        public async Task<Process> Install()
        {
            var steamCMD = new Installer.SteamCMD();
            Process p = await steamCMD.Install(_serverData.ServerID, string.Empty, AppId);
            Error = steamCMD.Error;

            return p;
        }

        public async Task<Process> Update(bool validate = false, string custom = null)
        {
            var (p, error) = await Installer.SteamCMD.UpdateEx(_serverData.ServerID, AppId, validate, custom: custom);
            Error = error;
            return p;
        }

        public bool IsInstallValid()
        {
            return File.Exists(Functions.ServerPath.GetServersServerFiles(_serverData.ServerID, StartPath));
        }

        public bool IsImportValid(string path)
        {
            Error = $"Invalid Path! Fail to find {StartPath}";
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
