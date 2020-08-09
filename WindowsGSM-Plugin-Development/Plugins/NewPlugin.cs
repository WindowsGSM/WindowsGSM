using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using WindowsGSM.Functions;
using WindowsGSM.GameServer.Query;
using Newtonsoft.Json.Linq;

namespace WindowsGSM.Plugins
{
    public class NewPlugin
    {
        // - Plugin Details
        public Plugin Plugin = new Plugin
        {
            name = "WindowsGSM.NewPlugin", // WindowsGSM.XXXX
            author = "Your name",
            description = "description",
            version = "1.0",
            url = "https://github.com/BattlefieldDuck", // Github repository link (Best practice)
            color = "#ffffff" // Color Hex
        };


        // - Standard Constructor and properties
        public NewPlugin(ServerConfig serverData) => _serverData = serverData;
        private readonly ServerConfig _serverData;
        public string Error, Notice;


        // - Game server Fixed variables
        public string StartPath => "paper.jar"; // Game server start path
        public string FullName = "Minecraft: Paper Server"; // Game server FullName
        public bool AllowsEmbedConsole = true;  // Does this server support output redirect?
        public int PortIncrements = 1; // This tells WindowsGSM how many ports should skip after installation
        public object QueryMethod = new UT3(); // Query method should be use on current server type. Accepted value: null or new A2S() or new FIVEM() or new UT3()


        // - Game server default values
        public string Port = "25565"; // Default port
        public string QueryPort = "25565"; // Default query port
        public string Defaultmap = "world"; // Default map name
        public string Maxplayers = "20"; // Default maxplayers
        public string Additional = ""; // Additional server start parameter


        // - Create a default cfg for the game server after installation
        public async void CreateServerCFG()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"motd={_serverData.ServerName}");
            sb.AppendLine($"server-port={_serverData.ServerPort}");
            sb.AppendLine("enable-query=true");
            sb.AppendLine($"query.port={_serverData.ServerQueryPort}");
            sb.AppendLine($"rcon.port={int.Parse(_serverData.ServerPort) + 10}");
            sb.AppendLine($"rcon.password={ _serverData.GetRCONPassword()}");
            File.WriteAllText(ServerPath.GetServersServerFiles(_serverData.ServerID, "server.properties"), sb.ToString());
        }


        // - Start server function, return its Process to WindowsGSM
        public async Task<Process> Start()
        {
            // Check Java exists
            var javaPath = JavaHelper.FindJavaExecutableAbsolutePath();
            if (javaPath.Length == 0)
            {
                Error = "Java is not installed";
                return null;
            }

            // Prepare start parameter
            var param = new StringBuilder($"{_serverData.ServerParam} -jar {StartPath} nogui");

            // Prepare Process
            var p = new Process
            {
                StartInfo =
                {
                    WorkingDirectory = ServerPath.GetServersServerFiles(_serverData.ServerID),
                    FileName = javaPath,
                    Arguments = param.ToString(),
                    WindowStyle = ProcessWindowStyle.Minimized,
                    UseShellExecute = false
                },
                EnableRaisingEvents = true
            };

            // Set up Redirect Input and Output to WindowsGSM Console if EmbedConsole is on
            if (AllowsEmbedConsole)
            {
                p.StartInfo.CreateNoWindow = true;
                p.StartInfo.RedirectStandardInput = true;
                p.StartInfo.RedirectStandardOutput = true;
                p.StartInfo.RedirectStandardError = true;
                var serverConsole = new ServerConsole(_serverData.ServerID);
                p.OutputDataReceived += serverConsole.AddOutput;
                p.ErrorDataReceived += serverConsole.AddOutput;

                // Start Process
                try
                {
                    p.Start();
                }
                catch (Exception e)
                {
                    Error = e.Message;
                    return null; // return null if fail to start
                }

                p.BeginOutputReadLine();
                p.BeginErrorReadLine();
                return p;
            }

            // Start Process
            try
            {
                p.Start();
                return p;
            }
            catch (Exception e)
            {
                Error = e.Message;
                return null; // return null if fail to start
            }
        }


        // - Stop server function
        public async Task Stop(Process p)
        {
            await Task.Run(() =>
            {
                if (p.StartInfo.RedirectStandardInput)
                {
                    // Send "stop" command to StandardInput stream if EmbedConsole is on
                    p.StandardInput.WriteLine("stop");
                }
                else
                {
                    // Send "stop" command to game server process MainWindow
                    ServerConsole.SendMessageToMainWindow(p.MainWindowHandle, "stop");
                }
            });
        }


        // - Install server function
        public async Task<Process> Install()
        {
            // EULA agreement
            var agreedPrompt = await UI.CreateYesNoPromptV1("Agreement to the EULA", "By continuing you are indicating your agreement to the EULA.\n(https://account.mojang.com/documents/minecraft_eula)", "Agree", "Decline");
            if (!agreedPrompt)
            { 
                Error = "Disagree to the EULA";
                return null;
            }

            // Install Java if not installed
            if (!JavaHelper.IsJREInstalled())
            {
                var taskResult = await JavaHelper.DownloadJREToServer(_serverData.ServerID);
                if (!taskResult.installed)
                {
                    Error = taskResult.error;
                    return null;
                }
            }

            // Try getting the latest version and build
            var build = await GetRemoteBuild(); // "1.16.1/133"
            if (string.IsNullOrWhiteSpace(build)) { return null; }

            // Download the latest paper.jar to /serverfiles
            try
            {
                using (var webClient = new WebClient())
                {
                    await webClient.DownloadFileTaskAsync($"https://papermc.io/api/v1/paper/{build}/download", ServerPath.GetServersServerFiles(_serverData.ServerID, StartPath));
                }
            }
            catch (Exception e)
            {
                Error = e.Message;
                return null;
            }

            // Create eula.txt
            var eulaFile = ServerPath.GetServersServerFiles(_serverData.ServerID, "eula.txt");
            File.WriteAllText(eulaFile, "#By changing the setting below to TRUE you are indicating your agreement to our EULA (https://account.mojang.com/documents/minecraft_eula).\neula=true");

            return null;
        }


        // - Update server function
        public async Task<Process> Update()
        {
            // Delete the old paper.jar
            var paperJar = ServerPath.GetServersServerFiles(_serverData.ServerID, StartPath);
            if (File.Exists(paperJar))
            {
                if (await Task.Run(() =>
                {
                    try
                    {
                        File.Delete(paperJar);
                        return true;
                    }
                    catch (Exception e)
                    {
                        Error = e.Message;
                        return false;
                    }
                }))
                {
                    return null;
                }
            }

            // Try getting the latest version and build
            var build = await GetRemoteBuild(); // "1.16.1/133"
            if (string.IsNullOrWhiteSpace(build)) { return null; }

            // Download the latest paper.jar to /serverfiles
            try
            {
                using (var webClient = new WebClient())
                {
                    await webClient.DownloadFileTaskAsync($"https://papermc.io/api/v1/paper/{build}/download", ServerPath.GetServersServerFiles(_serverData.ServerID, StartPath));
                }
            }
            catch (Exception e)
            {
                Error = e.Message;
                return null;
            }

            return null;
        }


        // - Check if the installation is successful
        public bool IsInstallValid()
        {
            // Check paper.jar exists
            return File.Exists(ServerPath.GetServersServerFiles(_serverData.ServerID, StartPath));
        }


        // - Check if the directory contains paper.jar for import
        public bool IsImportValid(string path)
        {
            // Check paper.jar exists
            var exePath = Path.Combine(path, StartPath);
            Error = $"Invalid Path! Fail to find {StartPath}";
            return File.Exists(exePath);
        }


        // - Get Local server version
        public string GetLocalBuild()
        {
            // Get local version and build by version_history.json
            const string VERSION_JSON_FILE = "version_history.json";
            var versionJsonFile = ServerPath.GetServersServerFiles(_serverData.ServerID, VERSION_JSON_FILE);
            if (!File.Exists(versionJsonFile))
            {
                Error = $"{VERSION_JSON_FILE} does not exist";
                return string.Empty;
            }

            var json = File.ReadAllText(versionJsonFile);
            var text = JObject.Parse(json)["currentVersion"].ToString(); // "git-Paper-131 (MC: 1.16.1)"
            var match = new Regex(@"git-Paper-(\d{1,}) \(MC: (.{1,})\)").Match(text);
            var build = match.Groups[1].Value; // "131"
            var version = match.Groups[2].Value; // "1.16.1"

            return $"{version}/{build}";
        }


        // - Get Latest server version
        public async Task<string> GetRemoteBuild()
        {
            // Get latest version and build at https://papermc.io/api/v1/paper
            try
            {
                using (var webClient = new WebClient())
                {
                    var version = JObject.Parse(await webClient.DownloadStringTaskAsync("https://papermc.io/api/v1/paper"))["versions"][0].ToString(); // "1.16.1"
                    var build = JObject.Parse(await webClient.DownloadStringTaskAsync($"https://papermc.io/api/v1/paper/{version}"))["builds"]["latest"].ToString(); // "133"
                    return $"{version}/{build}";
                }
            }
            catch
            {
                Error = "Fail to get remote version and build";
                return string.Empty;
            }
        }
    }
}
