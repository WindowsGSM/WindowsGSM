using System;
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Net;
using Newtonsoft.Json.Linq;
using System.Text.RegularExpressions;

namespace WindowsGSM.GameServer
{
    class MC
    {
        private readonly Functions.ServerConfig _serverData;

        public string Error;
        public string Notice;

        public const string FullName = "Minecraft: Java Edition Server";
        public string StartPath = "";
        public bool ToggleConsole = false;

        public string port = "25565";
        public string defaultmap = "world";
        public string maxplayers = "20";
        public string additional = "-Xmx1024M -Xms1024M -XX:+UseG1GC";

        private enum Java : int
        {
            NotInstall = 0,
            InstalledGlobal = 1, //(java)
            InstalledAbsolute = 2 //Path: (C:\Program Files (x86)\Java\jre1.8.0_231\bin\java.exe)
        }

        public MC(Functions.ServerConfig serverData)
        {
            _serverData = serverData;
        }

        public async void CreateServerCFG()
        {
            //Create server.properties
            string configPath = Functions.Path.GetServerFiles(_serverData.ServerID, "server.properties");
            if (await Functions.Github.DownloadGameServerConfig(configPath, FullName))
            {
                string serverPort = _serverData.GetAvailablePort(this.port.ToString());
                string configText = File.ReadAllText(configPath);
                configText = configText.Replace("{{serverPort}}", serverPort);
                configText = configText.Replace("{{maxplayers}}", maxplayers);
                configText = configText.Replace("{{rconPort}}", (Int32.Parse(serverPort) + 10).ToString());
                configText = configText.Replace("{{serverIP}}", _serverData.ServerIP);
                configText = configText.Replace("{{defaultmap}}", defaultmap);
                configText = configText.Replace("{{rcon_password}}", _serverData.GetRCONPassword());
                configText = configText.Replace("{{serverName}}", _serverData.ServerName);
                File.WriteAllText(configPath, configText);
            }
        }

        public async Task<Process> Start()
        {
            Java isJavaInstalled = IsJavaJREInstalled();
            if (isJavaInstalled == Java.NotInstall)
            {
                Error = "Java is not installed";
                return null;
            }

            string workingDir = Functions.Path.GetServerFiles(_serverData.ServerID);

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

            string javaPath = (isJavaInstalled == Java.InstalledGlobal) ? "java" : "C:\\Program Files (x86)\\Java\\jre1.8.0_231\\bin\\java.exe";

            if (isJavaInstalled == Java.InstalledAbsolute)
            {
                WindowsFirewall firewall = new WindowsFirewall("java.exe", javaPath);
                if (!await firewall.IsRuleExist())
                {
                    firewall.AddRule();
                }
            }

            Process p = new Process
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

            return p;
        }

        public async Task Stop(Process p)
        {
            await Task.Run(() =>
            {
                p.StandardInput.WriteLine("stop");
            });
        }

        public async Task<Process> Install()
        {
            //EULA
            MessageBoxResult result = System.Windows.MessageBox.Show("By continuing you are indicating your agreement to the EULA.\n(https://account.mojang.com/documents/minecraft_eula)", "Agreement to the EULA", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result != MessageBoxResult.Yes)
            {
                Error = "Disagree to the EULA";
                return null;
            }

            //Install JAVA if not installed
            if (IsJavaJREInstalled() == 0)
            {
                //Java
                result = System.Windows.MessageBox.Show("Java is not installed\n\nWould you like to install?", "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result != MessageBoxResult.Yes)
                {
                    Error = "Java is not installed";
                    return null;
                }

                if (!await DownloadJavaJRE())
                {
                    return null;
                }
            }

            try
            {
                WebClient webClient = new WebClient();
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
                };

                if (packageUrl == null)
                {
                    Error = $"Fail to fetch packageUrl from {manifestUrl}";
                    return null;
                }

                //packageUrl example: https://launchermeta.mojang.com/v1/packages/6876d19c096de56d1aa2cf434ec6b0e66e0aba00/1.15.json
                var packageJson = webClient.DownloadString(packageUrl);

                //serverJarUrl example: https://launcher.mojang.com/v1/objects/e9f105b3c5c7e85c7b445249a93362a22f62442d/server.jar
                string serverJarUrl = JObject.Parse(packageJson)["downloads"]["server"]["url"].ToString();

                webClient.DownloadFileCompleted += InitiateServerJar;
                webClient.DownloadFileAsync(new Uri(serverJarUrl), Functions.Path.GetServerFiles(_serverData.ServerID, "server.jar"));
            }
            catch
            {
                Error = $"Fail to install {FullName}";
                return null;
            }

            return null;
        }

        public async Task<bool> Update()
        {
            //Install JAVA if not installed
            if (IsJavaJREInstalled() == Java.NotInstall)
            {
                if (!await DownloadJavaJRE())
                {
                    return false;
                }
            }

            string serverJarPath = Functions.Path.GetServerFiles(_serverData.ServerID, "server.jar");
            if (File.Exists(serverJarPath))
            {
                try
                {
                    File.Delete(serverJarPath);
                }
                catch
                {

                }
            }

            if (File.Exists(serverJarPath))
            {
                Error = "Fail to delete server.jar";
                return false;
            }

            try
            {
                WebClient webClient = new WebClient();
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
                    return false;
                }

                //packageUrl example: https://launchermeta.mojang.com/v1/packages/6876d19c096de56d1aa2cf434ec6b0e66e0aba00/1.15.json
                var packageJson = webClient.DownloadString(packageUrl);

                //serverJarUrl example: https://launcher.mojang.com/v1/objects/e9f105b3c5c7e85c7b445249a93362a22f62442d/server.jar
                string serverJarUrl = JObject.Parse(packageJson)["downloads"]["server"]["url"].ToString();

                webClient.DownloadFileCompleted += InitiateServerJar;
                webClient.DownloadFileAsync(new Uri(serverJarUrl), serverJarPath);
            }
            catch
            {
                Error = $"Fail to install {FullName}";
                return false;
            }

            return true;
        }

        public bool IsInstallValid()
        {
            string jarFile = "server.jar";
            string jarPath = Functions.Path.GetServerFiles(_serverData.ServerID, jarFile);

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
            string logPath = Functions.Path.GetServerFiles(_serverData.ServerID, "logs", logFile);

            if (!File.Exists(logPath))
            {
                Error = $"{logFile} is missing.";
                return "";
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
            return "";
        }

        public async Task<string> GetRemoteBuild()
        {
            try
            {
                WebClient webClient = new WebClient();
                string remoteUrl = "https://launchermeta.mojang.com/mc/game/version_manifest.json";
                string html = await webClient.DownloadStringTaskAsync(remoteUrl);

                Regex regex = new Regex("\"latest\":.{\"release\":.\"(.*?)\"");
                var matches = regex.Matches(html);

                if (matches.Count == 1 && matches[0].Groups.Count == 2)
                {
                    return matches[0].Groups[1].Value;
                }
            }
            catch
            {

            }

            Error = $"Fail to get remote build";
            return "";
        }

        public string GetQueryPort()
        {
            return _serverData.ServerPort;
        }

        private static Java IsJavaJREInstalled()
        {
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo("cmd.exe");
                psi.RedirectStandardOutput = true;
                psi.RedirectStandardError = true;
                psi.UseShellExecute = false;
                psi.CreateNoWindow = true;
                psi.Arguments = "/c java -version";
                Process p = Process.Start(psi);
                string output = p.StandardOutput.ReadToEnd();
                string error = p.StandardError.ReadToEnd();

                if (output.Contains("java version") || error.Contains("java version"))
                {
                    return Java.InstalledGlobal;
                }
            }
            catch
            {

            }

            try
            {
                ProcessStartInfo psi = new ProcessStartInfo("cmd.exe");
                psi.RedirectStandardOutput = true;
                psi.RedirectStandardError = true;
                psi.UseShellExecute = false;
                psi.CreateNoWindow = true;
                psi.Arguments = "/c \"C:\\Program Files (x86)\\Java\\jre1.8.0_231\\bin\\java.exe\" -version";
                Process p = Process.Start(psi);
                string output = p.StandardOutput.ReadToEnd();
                string error = p.StandardError.ReadToEnd();

                if (output.Contains("java version") || error.Contains("java version"))
                {
                    return Java.InstalledAbsolute;
                }
            }
            catch
            {

            }

            return Java.NotInstall;
        }

        private async Task<bool> DownloadJavaJRE()
        {
            string serverFilesPath = Functions.Path.GetServerFiles(_serverData.ServerID);
            string filename = "jre-8u231-windows-i586-iftw.exe";
            string installer = "https://javadl.oracle.com/webapps/download/AutoDL?BundleId=240725_5b13a193868b4bf28bcb45c792fce896";

            //Download jre-8u231-windows-i586-iftw.exe from https://www.java.com/en/download/manual.jsp
            string jrePath = Path.Combine(serverFilesPath, filename);
            try
            {
                WebClient webClient = new WebClient();

                //Run jre-8u231-windows-i586-iftw.exe to install Java
                await webClient.DownloadFileTaskAsync(installer, jrePath);
                string installPath = Functions.Path.GetServerFiles(_serverData.ServerID);
                string javaPath = @"C:\Program Files (x86)\Java\jre1.8.0_231";
                ProcessStartInfo psi = new ProcessStartInfo(jrePath);
                psi.WorkingDirectory = installPath;
                psi.Arguments = $"INSTALL_SILENT=Enable INSTALLDIR=\"{javaPath}\"";
                Process p = new Process
                {
                    StartInfo = psi,
                    EnableRaisingEvents = true
                };
                p.Start();
                p.Exited += (object sender, EventArgs e) => 
                {
                    try
                    {
                        //Delete the jre-8u231-windows-i586-iftw.exe after installation
                        File.Delete(jrePath);
                    }
                    catch
                    {
                        //Ignore
                    }
                };      
            }
            catch
            {
                Error = "Fail to download " + filename;
                return false;
            }

            return true;
        }

        private void InitiateServerJar(object sender, System.ComponentModel.AsyncCompletedEventArgs e)
        {
            //Create eula.txt
            string eulaPath = Functions.Path.GetServerFiles(_serverData.ServerID, "eula.txt");
            File.Create(eulaPath).Dispose();

            using (TextWriter textwriter = new StreamWriter(eulaPath))
            {
                textwriter.WriteLine("#By changing the setting below to TRUE you are indicating your agreement to our EULA (https://account.mojang.com/documents/minecraft_eula).");
                textwriter.WriteLine("#Generated by WindowsGSM.exe");
                textwriter.WriteLine("eula=true");
            }
        }
    }
}
