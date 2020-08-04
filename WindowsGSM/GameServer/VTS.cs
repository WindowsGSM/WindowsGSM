using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using WindowsGSM.Functions;

namespace WindowsGSM.GameServer
{
    class VTS
    {
        private readonly ServerConfig _serverData;

        public string Error;
        public string Notice;

        public const string FullName = "Vintage Story Dedicated Server";
        public string StartPath = "VintageStoryServer.exe";
        public bool AllowsEmbedConsole = true;
        public int PortIncrements = 1;
        public dynamic QueryMethod = null;

        public string Port = "42420";
        public string QueryPort = "42420";
        public string Defaultmap = "default";
        public string Maxplayers = "16";
        public string Additional = "--dataPath ./data";

        public VTS(ServerConfig serverData)
        {
            _serverData = serverData;
        }

        public async void CreateServerCFG()
        {
            //Download serverconfig.json
            var replaceValues = new List<(string, string)>()
            {
                ("{{ServerName}}", _serverData.ServerName),
                ("{{Port}}", _serverData.ServerPort),
                ("{{MaxClients}}", _serverData.ServerMaxPlayer)
            };

            await Github.DownloadGameServerConfig(ServerPath.GetServersServerFiles(_serverData.ServerID, "data", "serverconfig.json"), FullName, replaceValues);
        }

        public async Task<Process> Start()
        {
            string exePath = ServerPath.GetServersServerFiles(_serverData.ServerID, StartPath);
            if (!File.Exists(exePath))
            {
                Error = $"{Path.GetFileName(exePath)} not found ({exePath})";
                return null;
            }

            Process p = new Process
            {
                StartInfo =
                {
                    WorkingDirectory = Directory.GetParent(exePath).FullName,
                    FileName = exePath,
                    Arguments = _serverData.ServerParam,
                    WindowStyle = ProcessWindowStyle.Minimized,
                    UseShellExecute = false
                },
                EnableRaisingEvents = true
            };

            if (AllowsEmbedConsole)
            {
                p.StartInfo.CreateNoWindow = true;
                p.StartInfo.RedirectStandardInput = true;
                p.StartInfo.RedirectStandardOutput = true;
                p.StartInfo.RedirectStandardError = true;
                var serverConsole = new ServerConsole(_serverData.ServerID);
                p.OutputDataReceived += serverConsole.AddOutput;
                p.ErrorDataReceived += serverConsole.AddOutput;
                p.Start();
                p.BeginOutputReadLine();
                p.BeginErrorReadLine();
                return p;
            }

            p.Start();
            return p;
        }

        public async Task Stop(Process p)
        {
            await Task.Run(() =>
            {
                if (p.StartInfo.RedirectStandardInput)
                {
                    p.StandardInput.WriteLine("/stop");
                }
                else
                {
                    ServerConsole.SendMessageToMainWindow(p.MainWindowHandle, "/stop");
                }
            });
        }

        public async Task<Process> Install()
        {
            string version = await GetRemoteBuild();
            if (version == null) { return null; }
            string tarName = $"vs_server_{version}.tar.gz";
            string address = $"https://cdn.vintagestory.at/gamefiles/stable/{tarName}";
            string tarPath = ServerPath.GetServersServerFiles(_serverData.ServerID, tarName);

            // Download vs_server_{version}.tar.gz from https://cdn.vintagestory.at/gamefiles/stable/
            using (WebClient webClient = new WebClient())
            {
                try { await webClient.DownloadFileTaskAsync(address, tarPath); } 
                catch
                {
                    Error = $"Fail to download {tarName}";
                    return null;
                }
            }

            // Extract vs_server_{version}.tar.gz
            if (!await FileManagement.ExtractTarGZ(tarPath, Directory.GetParent(tarPath).FullName))
            {
                Error = $"Fail to extract {tarName}";
                return null;
            }

            // Delete vs_server_{version}.tar.gz, leave it if fail to delete
            await FileManagement.DeleteAsync(tarPath);

            return null;
        }

        public async Task<bool> Update()
        {
            // Backup the data folder
            string dataPath = ServerPath.GetServersServerFiles(_serverData.ServerID, "data");
            string tempPath = ServerPath.GetServers(_serverData.ServerID, "__temp");
            bool needBackup = Directory.Exists(dataPath);
            if (needBackup)
            {
                if (Directory.Exists(tempPath))
                {
                    if (!await Functions.DirectoryManagement.DeleteAsync(tempPath, true))
                    {
                        Error = $"Fail to delete the temp folder";
                        return false;
                    }
                }

                if (!await Task.Run(() =>
                {
                    try
                    {
                        Microsoft.VisualBasic.FileIO.FileSystem.CopyDirectory(dataPath, tempPath);
                        return true;
                    }
                    catch (Exception e)
                    {
                        Error = e.Message;
                        return false;
                    }
                }))
                {
                    return false;
                }
            }

            // Delete the serverfiles folder
            if (!await DirectoryManagement.DeleteAsync(ServerPath.GetServersServerFiles(_serverData.ServerID), true))
            {
                Error = $"Fail to delete the serverfiles";
                return false;
            }

            // Recreate the serverfiles folder
            Directory.CreateDirectory(ServerPath.GetServersServerFiles(_serverData.ServerID));

            if (needBackup)
            {
                // Restore the data folder
                if (!await Task.Run(() =>
                {
                    try
                    {
                        Microsoft.VisualBasic.FileIO.FileSystem.CopyDirectory(tempPath, dataPath);
                        return true;
                    }
                    catch (Exception e)
                    {
                        Error = e.Message;
                        return false;
                    }
                }))
                {
                    return false;
                }

                await DirectoryManagement.DeleteAsync(tempPath, true);
            }

            // Update the server by install again
            await Install();

            // Return is valid
            return IsInstallValid();
        }

        public bool IsInstallValid()
        {
            string exePath = ServerPath.GetServersServerFiles(_serverData.ServerID, StartPath);
            return File.Exists(exePath);
        }

        public bool IsImportValid(string path)
        {
            string exePath = Path.Combine(path, StartPath);
            Error = $"Invalid Path! Fail to find {StartPath}";
            return File.Exists(exePath);
        }

        public string GetLocalBuild()
        {
            // Get local version in VintageStoryServer.exe
            string exePath = ServerPath.GetServersServerFiles(_serverData.ServerID, StartPath);
            if (!File.Exists(exePath))
            {
                Error = $"{StartPath} is missing.";
                return string.Empty;
            }

            return FileVersionInfo.GetVersionInfo(exePath).ProductVersion; // return "1.12.14"
        }

        public async Task<string> GetRemoteBuild()
        {
            // Get latest build in https://aur.archlinux.org/cgit/aur.git/log/?h=vintagestory with regex
            try
            {
                using (WebClient webClient = new WebClient())
                {
                    string html = await webClient.DownloadStringTaskAsync("https://aur.archlinux.org/cgit/aur.git/log/?h=vintagestory");
                    Regex regex = new Regex(@"(\d{1,}\.\d{1,}\.\d{1,})<\/a>"); // Match "1.12.14</a>"
                    return regex.Match(html).Groups[1].Value; // Get first group -> "1.12.14"
                }
            }
            catch
            {
                Error = "Fail to get remote build";
                return string.Empty;
            }
        }
    }
}
