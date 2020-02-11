using System;
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Collections;
using System.Linq;

namespace WindowsGSM.GameServer
{
    /// <summary>
    /// 
    /// Note:
    /// I personally don't play Space engineers and I have no experience on this game, even on the game server.
    /// If anyone is the specialist or having a experience on Space Engineers server. Feel feel to edit this and pull request in Github.
    /// 
    /// </summary>
    class SE
    {
        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        private readonly Functions.ServerConfig _serverData;

        public string Error;
        public string Notice;

        public const string FullName = "Space Engineers Dedicated Server";
        public string StartPath = @"DedicatedServer64\SpaceEngineersDedicated.exe";
        public bool ToggleConsole = false;

        public string port = "27016";
        public string defaultmap = "WindowsGSM_World";
        public string maxplayers = "4";
        public string additional = "";

        public SE(Functions.ServerConfig serverData)
        {
            _serverData = serverData;
        }

        public async void CreateServerCFG()
        {
            /*
             * The configs is created under %APPDATA% points to C:\Users\(Username)\AppData\Roaming\  Environment.GetEnvironmentVariable("APPDATA")
             */

            /*
            string serverFilePath = Functions.Path.GetServerFiles(_serverData.ServerID);
            Directory.CreateDirectory(Path.Combine(serverFilePath, "Instance"));
            Directory.CreateDirectory(Path.Combine(serverFilePath, "Instance", "cache"));
            Directory.CreateDirectory(Path.Combine(serverFilePath, "Instance", "Mods"));
            Directory.CreateDirectory(Path.Combine(serverFilePath, "Instance", "Saves"));

            string configPath = Functions.Path.GetServerFiles(_serverData.ServerID, "Instance", "SpaceEngineers-Dedicated.cfg");
            if (await Functions.Github.DownloadGameServerConfig(configPath, FullName))
            {
                string configText = File.ReadAllText(configPath);
                configText = configText.Replace("{{maxplayers}}", maxplayers);
                configText = configText.Replace("{{LoadWorld}}", Path.Combine(serverFilePath, "Instance", "Saves", defaultmap));
                configText = configText.Replace("{{ip}}", _serverData.ServerIP);
                configText = configText.Replace("{{port}}", _serverData.GetAvailablePort(port));
                configText = configText.Replace("{{ServerName}}", _serverData.ServerName);
                configText = configText.Replace("{{WorldName}}", defaultmap);
                File.WriteAllText(configPath, configText);
            }
            */
        }

        public async Task<Process> Start()
        {
            /*
            string configFile = "SpaceEngineers-Dedicated.cfg";
            string configPath = Functions.Path.GetServerFiles(_serverData.ServerID, "Config", configFile);
            if (!File.Exists(configPath))
            {
                Notice = $"{configFile} not found ({configPath})";
            }
            */

            string param = (ToggleConsole ? "-console" : "-noconsole") + " -ignorelastsession";
            param += (string.IsNullOrEmpty(_serverData.ServerIP)) ? "" : $" -ip {_serverData.ServerIP}";
            param += (string.IsNullOrEmpty(_serverData.ServerPort)) ? "" : $" -port {_serverData.ServerPort}";
            param += $" {_serverData.ServerParam}";

            Process p = new Process
            {
                StartInfo =
                {
                    FileName = Functions.ServerPath.GetServerFiles(_serverData.ServerID, StartPath),
                    Arguments = param,
                    WindowStyle = ProcessWindowStyle.Minimized,
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                },
                EnableRaisingEvents = true
            };
            /*
             * I had tried to change APPDATA value but fail... seems there is no way to change config dir...
             */
            //Environment.CurrentDirectory = Functions.Path.GetServerFiles(_serverData.ServerID, "Instance");
            //Environment.SetEnvironmentVariable("APPDATA", Functions.Path.GetServerFiles(_serverData.ServerID, "Instance"), EnvironmentVariableTarget.User);
            //Environment.appdata
            //p.StartInfo.EnvironmentVariables["APPDATA"] = Functions.Path.GetServerFiles(_serverData.ServerID, "Instance");
            //p.StartInfo.EnvironmentVariables.Add("AppData", Functions.Path.GetServerFiles(_serverData.ServerID, "Instance"));
            //p.StartInfo.Environment["APPDATA"] = Functions.Path.GetServerFiles(_serverData.ServerID, "Config");
            var serverConsole = new Functions.ServerConsole(_serverData.ServerID);
            p.OutputDataReceived += serverConsole.AddOutput;
            p.ErrorDataReceived += serverConsole.AddOutput;
            p.Start();
            p.BeginOutputReadLine();
            p.BeginErrorReadLine();

            return p;
        }

        public async Task Stop(Process p)
        {
            await Task.Run(() =>
            {
                if (ToggleConsole)
                {
                    SetForegroundWindow(p.MainWindowHandle);
                    SendKeys.SendWait("^(c)");
                    SendKeys.SendWait("^(c)");
                    SetForegroundWindow(Process.GetCurrentProcess().MainWindowHandle);
                }
                else
                {
                    /*  Base on https://www.spaceengineersgame.com/dedicated-servers.html
                     * 
                     *  C:\WINDOWS\system32 > TASKKILL /pid 26500
                     *  SUCCESS: Sent termination signal to the process with PID 26500.
                     * 
                     *  But the process still exist.... Therefore, p.Kill(); is used
                     * 

                    Process taskkill = new Process
                    {
                        StartInfo =
                        {
                            FileName = "TASKKILL",
                            Arguments = $"/PID {p.Id}",
                            Verb = "runas",
                            UseShellExecute = false,
                            CreateNoWindow = true
                        }
                    };
                    taskkill.Start();
                    */

                    p.Kill();
                }
            });
        }

        public async Task<Process> Install()
        {
            var steamCMD = new Installer.SteamCMD();
            Process p = await steamCMD.Install(_serverData.ServerID, "", "298740");
            Error = steamCMD.Error;

            return p;
        }

        public async Task<bool> Update(bool validate = false)
        {
            var steamCMD = new Installer.SteamCMD();
            bool updateSuccess = await steamCMD.Update(_serverData.ServerID, "", "298740", validate);
            Error = steamCMD.Error;

            return updateSuccess;
        }

        public bool IsInstallValid()
        {
            return File.Exists(Functions.ServerPath.GetServerFiles(_serverData.ServerID, StartPath));
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
            return steamCMD.GetLocalBuild(_serverData.ServerID, "298740");
        }

        public async Task<string> GetRemoteBuild()
        {
            var steamCMD = new Installer.SteamCMD();
            return await steamCMD.GetRemoteBuild("298740");
        }

        public string GetQueryPort()
        {
            return _serverData.ServerPort;
        }
    }
}
