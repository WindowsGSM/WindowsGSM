using System;
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Collections.Generic;

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

        public string port = "2302";
        public string defaultmap = "dayzOffline.chernarusplus";
        public string maxplayers = "60";
        public string additional = "-doLogs -adminLog -netLog";

        public DAYZ(Functions.ServerConfig serverData)
        {
            _serverData = serverData;
        }

        public async void CreateServerCFG()
        {
            //Download serverDZ.cfg
            string configPath = Functions.ServerPath.GetServerFiles(_serverData.ServerID, "serverDZ.cfg");
            if (await Functions.Github.DownloadGameServerConfig(configPath, FullName))
            {
                string configText = File.ReadAllText(configPath);
                configText = configText.Replace("{{hostname}}", _serverData.ServerName);
                configText = configText.Replace("{{maxplayers}}", maxplayers);
                File.WriteAllText(configPath, configText);
            }
        }

        public async Task<Process> Start()
        {
            string configPath = Functions.ServerPath.GetServerFiles(_serverData.ServerID, "serverDZ.cfg");
            if (!File.Exists(configPath))
            {
                Error = $"{Path.GetFileName(configPath)} not found ({configPath})";
                return null;
            }

            string param = $"DayZServer_x64.exe -config=serverDZ.cfg";
            param += string.IsNullOrEmpty(_serverData.ServerPort) ? "" : $" -port {_serverData.ServerPort}";
            param += $" {_serverData.ServerParam}";

            Process p = new Process
            {
                StartInfo =
                {
                    WorkingDirectory = Functions.ServerPath.GetServerFiles(_serverData.ServerID),
                    FileName = Functions.ServerPath.GetServerFiles(_serverData.ServerID, StartPath),
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
            Installer.SteamCMD steamCMD = new Installer.SteamCMD();
            steamCMD.SetParameter(Functions.ServerPath.GetServerFiles(_serverData.ServerID), "", "223350", true, loginAnonymous: false);

            Process process = await steamCMD.Run();
            Error = steamCMD.Error;

            return process;
        }

        public async Task<bool> Update()
        {
            Installer.SteamCMD steamCMD = new Installer.SteamCMD();
            steamCMD.SetParameter(Functions.ServerPath.GetServerFiles(_serverData.ServerID), "", "223350", false, loginAnonymous: false);

            Process pSteamCMD = await steamCMD.Run();
            if (pSteamCMD == null)
            {
                Error = steamCMD.Error;
                return false;
            }

            await Task.Run(() => pSteamCMD.WaitForExit());

            if (pSteamCMD.ExitCode != 0)
            {
                Error = "Exit code: " + pSteamCMD.ExitCode.ToString();
                return false;
            }

            return true;
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
            return steamCMD.GetLocalBuild(_serverData.ServerID, "223350");
        }

        public async Task<string> GetRemoteBuild()
        {
            var steamCMD = new Installer.SteamCMD();
            return await steamCMD.GetRemoteBuild("223350");
        }

        public string GetQueryPort()
        {
            return _serverData.ServerPort;
        }
    }
}
