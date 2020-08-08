using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;
using System;

namespace WindowsGSM.GameServer
{
    /// <summary>
    /// 
    /// Notes:
    /// 7 Days to Die Dedicated Server has a special console template which, when RedirectStandardOutput=true, the console output is still working.
    /// The console output seems have 3 channels, RedirectStandardOutput catch the first channel, RedirectStandardError catch the second channel, the third channel left on the game server console.
    /// Moreover, it has his input bar on the bottom so a normal sendkey method is not working.
    /// We need to send a {TAB} => (Send text) => {TAB} => (Send text) => {ENTER} to make the input cursor is on the input bar and send the command successfully.
    /// 
    /// RedirectStandardInput:  NO WORKING
    /// RedirectStandardOutput: YES (Used)
    /// RedirectStandardError:  YES (Used)
    /// SendKeys Input Method:  YES (Used)
    /// 
    /// There are two methods to shutdown this special server
    /// 1. {TAB} => (Send shutdown) => {TAB} => (Send shutdown) => {ENTER}
    /// 2. p.CloseMainWindow(); => {ENTER}
    /// 
    /// The second one is used.
    /// 
    /// </summary>
    class SDTD
    {
        private readonly Functions.ServerConfig _serverData;

        public string Error;
        public string Notice;

        public const string FullName = "7 Days to Die Dedicated Server";
        public string StartPath = "7DaysToDieServer.exe";
        public bool AllowsEmbedConsole = true;
        public int PortIncrements = 1;
        public dynamic QueryMethod = null;

        public string Port = "26900";
        public string QueryPort = "26900";
        public string Defaultmap = "Navezgane";
        public string Maxplayers = "8";
        public string Additional = string.Empty;

        public string AppId = "294420";

        public SDTD(Functions.ServerConfig serverData)
        {
            _serverData = serverData;
        }

        public async void CreateServerCFG()
        {
            //Download serverconfig.xml
            string configPath = Functions.ServerPath.GetServersServerFiles(_serverData.ServerID, "serverconfig.xml");
            if (await Functions.Github.DownloadGameServerConfig(configPath, FullName))
            {
                string configText = File.ReadAllText(configPath);
                configText = configText.Replace("{{hostname}}", _serverData.ServerName);
                configText = configText.Replace("{{rcon_password}}", _serverData.GetRCONPassword());
                configText = configText.Replace("{{port}}", _serverData.ServerPort);
                configText = configText.Replace("{{telnetPort}}", (int.Parse(_serverData.ServerPort) - int.Parse(Port) + 8081).ToString());
                configText = configText.Replace("{{maxplayers}}", Maxplayers);
                File.WriteAllText(configPath, configText);
            }

            //Create steam_appid.txt
            string txtPath = Functions.ServerPath.GetServersServerFiles(_serverData.ServerID, "steam_appid.txt");
            File.WriteAllText(txtPath, "251570");
        }

        public async Task<Process> Start()
        {
            string exeName = "7DaysToDieServer.exe";
            string workingDir = Functions.ServerPath.GetServersServerFiles(_serverData.ServerID);
            string exePath = Path.Combine(workingDir, exeName);

            if (!File.Exists(exePath))
            {
                Error = $"{exeName} not found ({exePath})";
                return null;
            }

            string configPath = Functions.ServerPath.GetServersServerFiles(_serverData.ServerID, "serverconfig.xml");
            if (!File.Exists(configPath))
            {
                Notice = $"serverconfig.xml not found ({configPath})";
            }

            string logFile = @"7DaysToDieServer_Data\output_log_dedi__" + DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss") + ".txt";
            string param = $"-logfile \"{Path.Combine(workingDir, logFile)}\" -quit -batchmode -nographics -configfile=serverconfig.xml -dedicated {_serverData.ServerParam}";

            WindowsFirewall firewall = new WindowsFirewall(exeName, exePath);
            if (!await firewall.IsRuleExist())
            {
                await firewall.AddRule();
            }

            Process p;
            if (!AllowsEmbedConsole)
            {
                p = new Process
                {
                    StartInfo =
                    {
                        WorkingDirectory = workingDir,
                        FileName = exePath,
                        Arguments = param,
                        WindowStyle = ProcessWindowStyle.Minimized
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
                        WorkingDirectory = workingDir,
                        FileName = exePath,
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
            }

            return p;
        }

        public async Task Stop(Process p)
        {
            await Task.Run(() =>
            {
                Functions.ServerConsole.SetMainWindow(p.MainWindowHandle);
                p.CloseMainWindow();
                Functions.ServerConsole.SendWaitToMainWindow("{ENTER}");
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
            string exeFile = "7DaysToDieServer.exe";
            string exePath = Functions.ServerPath.GetServersServerFiles(_serverData.ServerID, exeFile);

            return File.Exists(exePath);
        }

        public bool IsImportValid(string path)
        {
            string exeFile = "7DaysToDieServer.exe";
            string exePath = Path.Combine(path, exeFile);

            Error = $"Invalid Path! Fail to find {exeFile}";
            return File.Exists(exePath);
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
