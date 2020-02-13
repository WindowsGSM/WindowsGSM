using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System;

namespace WindowsGSM.GameServer
{
    class MCBE
    {
        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        private readonly Functions.ServerConfig _serverData;

        public string Error;
        public string Notice;

        public const string FullName = "Minecraft: Bedrock Edition Server";
        public string StartPath = "bedrock_server.exe";
        public bool ToggleConsole = false;

        public string port = "19132";
        public string defaultmap = "Bedrock level";
        public string maxplayers = "10";
        public string additional = "";

        public MCBE(Functions.ServerConfig serverData)
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
                configText = configText.Replace("{{server-name}}", _serverData.ServerName);
                configText = configText.Replace("{{max-players}}", maxplayers);
                string tempPort = _serverData.ServerPort;
                configText = configText.Replace("{{server-port}}", tempPort);
                configText = configText.Replace("{{server-portv6}}", (System.Int32.Parse(tempPort)+1).ToString());
                configText = configText.Replace("{{level-name}}", defaultmap);
                File.WriteAllText(configPath, configText);
            }
        }

        public async Task<Process> Start()
        {
            string workingDir = Functions.ServerPath.GetServerFiles(_serverData.ServerID);

            string exePath = Path.Combine(workingDir, StartPath);
            if (!File.Exists(exePath))
            {
                Error = $"{Path.GetFileName(exePath)} not found ({exePath})";
                return null;
            }

            string serverConfigPath = Path.Combine(workingDir, "server.properties");
            if (!File.Exists(serverConfigPath))
            {
                Error = $"{Path.GetFileName(serverConfigPath)} not found ({serverConfigPath})";
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
                        FileName = exePath,
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
                        FileName = exePath,
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
            //EULA and Privacy Policy
            MessageBoxResult result = System.Windows.MessageBox.Show("By continuing you are indicating your agreement to the Minecraft End User License Agreement and Privacy Policy.\nEULA: (https://minecraft.net/terms)\nPrivacy Policy: (https://go.microsoft.com/fwlink/?LinkId=521839)", "Agreement to the EULA", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result != MessageBoxResult.Yes)
            {
                Error = "Disagree to the EULA and Privacy Policy";
                return null;
            }

            try
            {
                using (WebClient webClient = new WebClient())
                {
                    string html = await webClient.DownloadStringTaskAsync("https://www.minecraft.net/en-us/download/server/bedrock/");
                    Regex regex = new Regex(@"https:\/\/minecraft\.azureedge\.net\/bin-win\/(bedrock-server-(.*?)\.zip)");
                    var matches = regex.Matches(html);

                    if (matches.Count <= 0)
                    {
                        return null;
                    }

                    string downloadUrl = matches[0].Value; //https://minecraft.azureedge.net/bin-win/bedrock-server-1.14.21.0.zip
                    string fileName = matches[0].Groups[1].Value; //bedrock-server-1.14.21.0.zip
                    string version = matches[0].Groups[2].Value; //1.14.21.0

                    //Download zip and extract then delete zip
                    string zipPath = Functions.ServerPath.GetServerFiles(_serverData.ServerID, fileName);
                    await webClient.DownloadFileTaskAsync(downloadUrl, zipPath);
                    await Task.Run(() => ZipFile.ExtractToDirectory(zipPath, Functions.ServerPath.GetServerFiles(_serverData.ServerID)));
                    await Task.Run(() => File.Delete(zipPath));

                    //Create MCBE-version.txt and write the version
                    File.WriteAllText(Functions.ServerPath.GetServerFiles(_serverData.ServerID, "MCBE-version.txt"), version);
                }
            }
            catch
            {
                //ignore
            }

            return null;
        }

        public async Task<bool> Update()
        {
            try
            {
                using (WebClient webClient = new WebClient())
                {
                    string remoteBuild = await GetRemoteBuild();

                    string html = await webClient.DownloadStringTaskAsync("https://www.minecraft.net/en-us/download/server/bedrock/");
                    Regex regex = new Regex(@"https:\/\/minecraft\.azureedge\.net\/bin-win\/(bedrock-server-(.*?)\.zip)");
                    var matches = regex.Matches(html);

                    if (matches.Count <= 0)
                    {
                        return false;
                    }

                    string downloadUrl = matches[0].Value; //https://minecraft.azureedge.net/bin-win/bedrock-server-1.14.21.0.zip
                    string fileName = matches[0].Groups[1].Value; //bedrock-server-1.14.21.0.zip
                    string version = matches[0].Groups[2].Value; //1.14.21.0

                    string tempPath = Functions.ServerPath.GetServerFiles(_serverData.ServerID, "__temp");

                    //Delete old __temp folder
                    if (Directory.Exists(tempPath))
                    {
                        await Task.Run(() => Directory.Delete(tempPath, true));
                    }

                    Directory.CreateDirectory(tempPath);

                    //Download zip and extract then delete zip - install to __temp folder
                    string zipPath = Path.Combine(tempPath, fileName);
                    await webClient.DownloadFileTaskAsync(downloadUrl, zipPath);
                    await Task.Run(() => ZipFile.ExtractToDirectory(zipPath, tempPath));
                    await Task.Run(() => File.Delete(zipPath));

                    string serverFilesPath = Functions.ServerPath.GetServerFiles(_serverData.ServerID);

                    //Delete old folder and files
                    await Task.Run(() =>
                    {
                        Directory.Delete(Path.Combine(serverFilesPath, "behavior_packs"), true);
                        Directory.Delete(Path.Combine(serverFilesPath, "definitions"), true);
                        Directory.Delete(Path.Combine(serverFilesPath, "resource_packs"), true);
                        Directory.Delete(Path.Combine(serverFilesPath, "structures"), true);
                        File.Delete(Path.Combine(serverFilesPath, "bedrock_server.exe"));
                        File.Delete(Path.Combine(serverFilesPath, "bedrock_server.pdb"));
                        File.Delete(Path.Combine(serverFilesPath, "release-notes.txt"));
                    });

                    //Move folder and files
                    await Task.Run(() =>
                    {
                        Directory.Move(Path.Combine(serverFilesPath, "__temp", "behavior_packs"), Path.Combine(serverFilesPath, "behavior_packs"));
                        Directory.Move(Path.Combine(serverFilesPath, "__temp", "definitions"), Path.Combine(serverFilesPath, "definitions"));
                        Directory.Move(Path.Combine(serverFilesPath, "__temp", "resource_packs"), Path.Combine(serverFilesPath, "resource_packs"));
                        Directory.Move(Path.Combine(serverFilesPath, "__temp", "structures"), Path.Combine(serverFilesPath, "structures"));
                        File.Move(Path.Combine(serverFilesPath, "__temp", "bedrock_server.exe"), Path.Combine(serverFilesPath, "bedrock_server.exe"));
                        File.Move(Path.Combine(serverFilesPath, "__temp", "bedrock_server.pdb"), Path.Combine(serverFilesPath, "bedrock_server.pdb"));
                        File.Move(Path.Combine(serverFilesPath, "__temp", "release-notes.txt"), Path.Combine(serverFilesPath, "release-notes.txt"));
                    });

                    //Delete __temp folder
                    await Task.Run(() => Directory.Delete(tempPath, true));

                    //Create MCBE-version.txt and write the version
                    File.WriteAllText(Functions.ServerPath.GetServerFiles(_serverData.ServerID, "MCBE-version.txt"), version);
                }

                return true;
            }
            catch (System.Exception e)
            {
                Error = e.ToString();
                return false;
            }
        }

        public string GetQueryPort()
        {
            return _serverData.ServerPort;
        }

        public bool IsInstallValid()
        {
            return File.Exists(Functions.ServerPath.GetServerFiles(_serverData.ServerID, StartPath));
        }

        public bool IsImportValid(string path)
        {
            Error = $"Invalid Path! Fail to find {StartPath}";
            return File.Exists(Path.Combine(path, StartPath));
        }

        public string GetLocalBuild()
        {
            string versionPath = Functions.ServerPath.GetServerFiles(_serverData.ServerID, "MCBE-version.txt");
            Error = $"Fail to get local build";
            return File.Exists(versionPath) ? File.ReadAllText(versionPath) : "";
        }

        public async Task<string> GetRemoteBuild()
        {
            try
            {
                using (WebClient webClient = new WebClient())
                {
                    string html = await webClient.DownloadStringTaskAsync("https://www.minecraft.net/en-us/download/server/bedrock/");
                    Regex regex = new Regex(@"https:\/\/minecraft\.azureedge\.net\/bin-win\/(bedrock-server-(.*?)\.zip)");
                    var matches = regex.Matches(html);

                    if (matches.Count > 0)
                    {
                        return matches[0].Groups[2].Value; //1.14.21.0
                    }
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
