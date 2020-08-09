using System;
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Net;
using Newtonsoft.Json.Linq;
using System.Text.RegularExpressions;
using WindowsGSM.Functions;

namespace WindowsGSM.GameServer
{
    class MC
    {
        private readonly Functions.ServerConfig _serverData;

        public string Error;
        public string Notice;

        public const string FullName = "Minecraft: Java Edition Server";
        public string StartPath = string.Empty;
        public bool AllowsEmbedConsole = true;
        public int PortIncrements = 1;
        public dynamic QueryMethod = new Query.UT3();

        public string Port = "25565";
        public string QueryPort = "25565";
        public string Defaultmap = "world";
        public string Maxplayers = "20";
        public string Additional = "-Xmx1024M -Xms1024M";

        public MC(Functions.ServerConfig serverData)
        {
            _serverData = serverData;
        }

        public async void CreateServerCFG()
        {
            //Create server.properties
            string configPath = Functions.ServerPath.GetServersServerFiles(_serverData.ServerID, "server.properties");
            if (await Functions.Github.DownloadGameServerConfig(configPath, FullName))
            {
                string configText = File.ReadAllText(configPath);
                configText = configText.Replace("{{serverPort}}", _serverData.ServerPort);
                configText = configText.Replace("{{maxplayers}}", Maxplayers);
                configText = configText.Replace("{{rconPort}}", (int.Parse(_serverData.ServerPort) + 10).ToString());
                configText = configText.Replace("{{serverIP}}", _serverData.ServerIP);
                configText = configText.Replace("{{defaultmap}}", Defaultmap);
                configText = configText.Replace("{{rcon_password}}", _serverData.GetRCONPassword());
                configText = configText.Replace("{{serverName}}", _serverData.ServerName);
                File.WriteAllText(configPath, configText);
            }
        }

        public async Task<Process> Start()
        {
            string javaPath = JavaHelper.FindJavaExecutableAbsolutePath();
            if (javaPath.Length == 0)
            {
                Error = "Java is not installed";
                return null;
            }

            string workingDir = Functions.ServerPath.GetServersServerFiles(_serverData.ServerID);

            string serverJarPath = Path.Combine(workingDir, "server.jar");
            if (!File.Exists(serverJarPath))
            {
                Error = $"server.jar not found ({serverJarPath})";
                return null;
            }

            string configPath = Path.Combine(workingDir, "server.properties");
            if (!File.Exists(configPath))
            {
                Notice = $"server.properties not found ({configPath}). Generated a new one.";
            }

            WindowsFirewall firewall = new WindowsFirewall("java.exe", javaPath);
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
                        FileName = javaPath,
                        Arguments = $"{_serverData.ServerParam} -jar server.jar nogui",
                        WindowStyle = ProcessWindowStyle.Minimized,
                        UseShellExecute = false
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
                        FileName = javaPath,
                        Arguments = $"{_serverData.ServerParam} -jar server.jar nogui",
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
                    Functions.ServerConsole.SendMessageToMainWindow(p.MainWindowHandle, "stop");
                }
            });
        }

        public async Task<Process> Install()
        {
            //EULA
            MessageBoxResult result = MessageBox.Show("By continuing you are indicating your agreement to the EULA.\n(https://account.mojang.com/documents/minecraft_eula)", "Agreement to the EULA", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result != MessageBoxResult.Yes)
            {
                Error = "Disagree to the EULA";
                return null;
            }

            //Install JAVA if not installed
            if (!JavaHelper.IsJREInstalled())
            {
                //Java
                result = MessageBox.Show("Java is not installed\n\nWould you like to install?", "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result != MessageBoxResult.Yes)
                {
                    Error = "Java is not installed";
                    return null;
                }

                JavaHelper.JREDownloadTaskResult taskResult = await JavaHelper.DownloadJREToServer(_serverData.ServerID);
                if (!taskResult.installed)
                {
                    Error = taskResult.error;
                    return null;
                }
            }

            try
            {
                using (WebClient webClient = new WebClient())
                {
                    const string manifestUrl = "https://launchermeta.mojang.com/mc/game/version_manifest.json";
                    string versionJson = await webClient.DownloadStringTaskAsync(manifestUrl);
                    string latesetVersion = JObject.Parse(versionJson)["latest"]["release"].ToString();
                    var versionObject = JObject.Parse(versionJson)["versions"];
                    string packageUrl = null;

                    foreach (var obj in versionObject)
                    {
                        if (obj["id"].ToString() == latesetVersion)
                        {
                            packageUrl = obj["url"].ToString();
                            break;
                        }
                    }

                    if (packageUrl == null)
                    {
                        Error = $"Fail to fetch packageUrl from {manifestUrl}";
                        return null;
                    }

                    //packageUrl example: https://launchermeta.mojang.com/v1/packages/6876d19c096de56d1aa2cf434ec6b0e66e0aba00/1.15.json
                    var packageJson = await webClient.DownloadStringTaskAsync(packageUrl);

                    //serverJarUrl example: https://launcher.mojang.com/v1/objects/e9f105b3c5c7e85c7b445249a93362a22f62442d/server.jar
                    string serverJarUrl = JObject.Parse(packageJson)["downloads"]["server"]["url"].ToString();
                    await webClient.DownloadFileTaskAsync(serverJarUrl, Functions.ServerPath.GetServersServerFiles(_serverData.ServerID, "server.jar"));

                    //Create eula.txt
                    string eulaPath = Functions.ServerPath.GetServersServerFiles(_serverData.ServerID, "eula.txt");
                    File.Create(eulaPath).Dispose();

                    using (TextWriter textwriter = new StreamWriter(eulaPath))
                    {
                        textwriter.WriteLine("#By changing the setting below to TRUE you are indicating your agreement to our EULA (https://account.mojang.com/documents/minecraft_eula).");
                        textwriter.WriteLine("#Generated by WindowsGSM.exe");
                        textwriter.WriteLine("eula=true");
                    }
                }
            }
            catch
            {
                Error = $"Fail to install {FullName}";
                return null;
            }

            return null;
        }

        public async Task<Process> Update()
        {
            //Install JAVA if not installed
            if (!JavaHelper.IsJREInstalled())
            {
                JavaHelper.JREDownloadTaskResult taskResult = await JavaHelper.DownloadJREToServer(_serverData.ServerID);
                if (!taskResult.installed)
                {
                    Error = taskResult.error;
                    return null;
                }
            }

            string serverJarPath = Functions.ServerPath.GetServersServerFiles(_serverData.ServerID, "server.jar");
            if (File.Exists(serverJarPath))
            {
                try
                {
                    File.Delete(serverJarPath);
                }
                catch
                {
                    Error = "Fail to delete server.jar";
                    return null;
                }
            }

            try
            {
                using (WebClient webClient = new WebClient())
                {
                    const string manifestUrl = "https://launchermeta.mojang.com/mc/game/version_manifest.json";
                    string versionJson = webClient.DownloadString(manifestUrl);
                    string latesetVersion = JObject.Parse(versionJson)["latest"]["release"].ToString();
                    var versionObject = JObject.Parse(versionJson)["versions"];
                    string packageUrl = null;

                    foreach (var obj in versionObject)
                    {
                        if (obj["id"].ToString() == latesetVersion)
                        {
                            packageUrl = obj["url"].ToString();
                            break;
                        }
                    }

                    if (packageUrl == null)
                    {
                        Error = $"Fail to fetch packageUrl from {manifestUrl}";
                        return null;
                    }

                    //packageUrl example: https://launchermeta.mojang.com/v1/packages/6876d19c096de56d1aa2cf434ec6b0e66e0aba00/1.15.json
                    var packageJson = webClient.DownloadString(packageUrl);

                    //serverJarUrl example: https://launcher.mojang.com/v1/objects/e9f105b3c5c7e85c7b445249a93362a22f62442d/server.jar
                    string serverJarUrl = JObject.Parse(packageJson)["downloads"]["server"]["url"].ToString();
                    await webClient.DownloadFileTaskAsync(serverJarUrl, Functions.ServerPath.GetServersServerFiles(_serverData.ServerID, "server.jar"));

                    //Create eula.txt
                    string eulaPath = Functions.ServerPath.GetServersServerFiles(_serverData.ServerID, "eula.txt");
                    File.Create(eulaPath).Dispose();

                    using (TextWriter textwriter = new StreamWriter(eulaPath))
                    {
                        textwriter.WriteLine("#By changing the setting below to TRUE you are indicating your agreement to our EULA (https://account.mojang.com/documents/minecraft_eula).");
                        textwriter.WriteLine("#Generated by WindowsGSM.exe");
                        textwriter.WriteLine("eula=true");
                    }
                }
            }
            catch
            {
                Error = $"Fail to install {FullName}";
                return null;
            }

            return null;
        }

        public bool IsInstallValid()
        {
            string jarFile = "server.jar";
            string jarPath = Functions.ServerPath.GetServersServerFiles(_serverData.ServerID, jarFile);

            return File.Exists(jarPath);
        }

        public bool IsImportValid(string path)
        {
            string jarFile = "server.jar";
            string jarPath = Path.Combine(path, jarFile);

            Error = $"Invalid Path! Fail to find {jarFile}";
            return File.Exists(jarPath);
        }

        public string GetLocalBuild()
        {
            string logFile = "latest.log";
            string logPath = Functions.ServerPath.GetServersServerFiles(_serverData.ServerID, "logs", logFile);

            if (!File.Exists(logPath))
            {
                Error = $"{logFile} is missing.";
                return string.Empty;
            }

            FileStream fileStream = new FileStream(logPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            StreamReader streamReader = new StreamReader(fileStream);

            while (!streamReader.EndOfStream)
            {
                string line = streamReader.ReadLine();
                if (line.Contains("] [Server thread/INFO]: Starting minecraft server version"))
                {
                    Regex regex = new Regex("\\d+\\.\\d+\\.\\d+");
                    return regex.Match(line).Value;
                }
            }

            streamReader.Close();
            fileStream.Close();

            Error = $"Fail to get local build";
            return string.Empty;
        }

        public async Task<string> GetRemoteBuild()
        {
            try
            {
                using (WebClient webClient = new WebClient())
                {
                    string remoteUrl = "https://launchermeta.mojang.com/mc/game/version_manifest.json";
                    string html = await webClient.DownloadStringTaskAsync(remoteUrl);

                    Regex regex = new Regex("\"latest\":.{\"release\":.\"(.*?)\"");
                    var matches = regex.Matches(html);

                    if (matches.Count == 1 && matches[0].Groups.Count == 2)
                    {
                        return matches[0].Groups[1].Value;
                    }
                }
            }
            catch
            {
                //ignore
            }

            Error = $"Fail to get remote build";
            return string.Empty;
        }
    }
}
