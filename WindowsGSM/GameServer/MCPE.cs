using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System;

namespace WindowsGSM.GameServer
{
    class MCPE
    {
        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        private readonly Functions.ServerConfig _serverData;

        public string Error;
        public string Notice;

        public const string FullName = "Minecraft: Pocket Edition Server (PocketMine-MP)";
        public string StartPath = @"bin\php\php.exe";
        public bool ToggleConsole = false;

        public string port = "19132";
        public string defaultmap = "world";
        public string maxplayers = "20";
        public string additional = "";

        public MCPE(Functions.ServerConfig serverData)
        {
            _serverData = serverData;
        }

        public async void CreateServerCFG()
        {
            //Download server.properties
            string configPath = Functions.ServerPath.GetServerFiles(_serverData.ServerID, "server.properties");
            if (await Functions.Github.DownloadGameServerConfig(configPath, FullName))
            {
                string configText = File.ReadAllText(configPath);
                configText = configText.Replace("{{hostname}}", _serverData.ServerName);
                configText = configText.Replace("{{rcon_password}}", _serverData.GetRCONPassword());
                configText = configText.Replace("{{port}}", _serverData.ServerPort);
                configText = configText.Replace("{{maxplayers}}", maxplayers);
                File.WriteAllText(configPath, configText);
            }
        }

        public async Task<Process> Start()
        {
            string workingDir = Functions.ServerPath.GetServerFiles(_serverData.ServerID);

            string phpPath = Path.Combine(workingDir, @"bin\php\php.exe");
            if (!File.Exists(phpPath))
            {
                Error = $"php.exe not found ({phpPath})";
                return null;
            }

            string PMMPPath = Path.Combine(workingDir, "PocketMine-MP.phar");
            if (!File.Exists(PMMPPath))
            {
                Error = $"PocketMine-MP.phar not found ({PMMPPath})";
                return null;
            }

            string serverConfigPath = Path.Combine(workingDir, "server.properties");
            if (!File.Exists(serverConfigPath))
            {
                Error = $"server.properties not found ({serverConfigPath})";
                return null;
            }

            Process p;
            if (ToggleConsole)
            {
                p = new Process
                {
                    StartInfo =
                    {
                        WorkingDirectory = workingDir,
                        FileName = phpPath,
                        Arguments = @"-c bin\php PocketMine-MP.phar",
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
                        WorkingDirectory = workingDir,
                        FileName = phpPath,
                        Arguments = @"-c bin\php PocketMine-MP.phar",
                        WindowStyle = ProcessWindowStyle.Minimized,
                        CreateNoWindow = true,
                        UseShellExecute = false,
                        RedirectStandardInput = true,
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
                if (p.StartInfo.RedirectStandardInput)
                {
                    p.StandardInput.WriteLine("stop");
                }
                else
                {
                    SetForegroundWindow(p.MainWindowHandle);
                    SendKeys.SendWait("stop");
                    SendKeys.SendWait("{ENTER}");
                    SetForegroundWindow(Process.GetCurrentProcess().MainWindowHandle);
                }
            });
        }

        public async Task<Process> Install()
        {
            string serverFilesPath = Functions.ServerPath.GetServerFiles(_serverData.ServerID);

            //Download PHP-7.3-Windows-x64.zip
            string fileName = "PHP-7.3-Windows-x64.zip";
            string installUrl = "https://jenkins.pmmp.io/job/PHP-7.3-Aggregate/lastStableBuild/artifact/PHP-7.3-Windows-x64.zip";
            string PHPzipPath = Path.Combine(serverFilesPath, fileName);
            try
            {
                WebClient webClient = new WebClient();
                await webClient.DownloadFileTaskAsync(installUrl, PHPzipPath);

                //Extract PHP-7.3-Windows-x64.zip and delete the zip
                await Task.Run(() => ZipFile.ExtractToDirectory(PHPzipPath, serverFilesPath));
                await Task.Run(() => File.Delete(PHPzipPath));
            }
            catch
            {
                Error = $"Fail to download {fileName}";
                return null;
            }

            //Download PocketMine-MP.phar
            fileName = "PocketMine-MP.phar";
            installUrl = "https://jenkins.pmmp.io/job/PocketMine-MP/lastStableBuild/artifact/PocketMine-MP.phar";
            try
            {
                WebClient webClient = new WebClient();
                await webClient.DownloadFileTaskAsync(installUrl, Path.Combine(serverFilesPath, fileName));
            }
            catch
            {
                Error = $"Fail to download {fileName}";
                return null;
            }

            return null;
        }

        public async Task<bool> Update()
        {
            //Delete PocketMine-MP.phar
            string fileName = "PocketMine-MP.phar";
            string PMMPPath = Functions.ServerPath.GetServerFiles(_serverData.ServerID, fileName);
            try
            {
                if (File.Exists(PMMPPath))
                {
                    File.Delete(PMMPPath);
                }
            }
            catch
            {
                Error = $"Fail to delete {fileName}";
                return false;
            }

            //Download PocketMine-MP.phar
            string installUrl = "https://jenkins.pmmp.io/job/PocketMine-MP/lastStableBuild/artifact/PocketMine-MP.phar";
            try
            {
                WebClient webClient = new WebClient();
                await webClient.DownloadFileTaskAsync(installUrl, PMMPPath);
            }
            catch
            {
                Error = $"Fail to download {fileName}";
                return false;
            }

            return true;
        }

        public string GetQueryPort()
        {
            return _serverData.ServerPort;
        }

        public bool IsInstallValid()
        {
            string PHPPath = Functions.ServerPath.GetServerFiles(_serverData.ServerID, @"bin\php\php.exe");
            string PMMPPath = Functions.ServerPath.GetServerFiles(_serverData.ServerID, "PocketMine-MP.phar");
            return File.Exists(PHPPath) && File.Exists(PMMPPath);
        }

        public bool IsImportValid(string path)
        {
            string PMMPFile = "PocketMine-MP.phar";
            string PMMPPath = Path.Combine(path, PMMPFile);

            Error = $"Invalid Path! Fail to find {PMMPFile}";
            return File.Exists(PMMPPath);
        }

        public string GetLocalBuild()
        {
            string PMMPFile = "PocketMine-MP.phar";
            string PMMPPath = Functions.ServerPath.GetServerFiles(_serverData.ServerID, PMMPFile);

            if (!File.Exists(PMMPPath))
            {
                Error = $"{PMMPFile} is missing.";
                return "";
            }

            using (StreamReader sr = File.OpenText(PMMPPath))
            {
                string s = string.Empty;
                while ((s = sr.ReadLine()) != null)
                {
                    if (s.Contains("const BUILD_NUMBER"))
                    {
                        Regex regex = new Regex("\\d+");
                        return regex.Match(s).Value;
                    }
                }
            }

            Error = $"Fail to get local build";
            return "";
        }

        public async Task<string> GetRemoteBuild()
        {
            try
            {
                WebClient webClient = new WebClient();
                string remoteUrl = "https://jenkins.pmmp.io/job/PocketMine-MP/lastStableBuild/artifact/build_info.json";
                string html = await webClient.DownloadStringTaskAsync(remoteUrl);
                Regex regex = new Regex("\"build_number\":\\D{0,}(.*?),");
                var matches = regex.Matches(html);

                if (matches.Count == 1 && matches[0].Groups.Count == 2)
                {
                    return matches[0].Groups[1].Value;
                }
            }
            catch
            {
                //ignore
            }

            Error = $"Fail to get remote build";
            return "";
        }
    }
}
