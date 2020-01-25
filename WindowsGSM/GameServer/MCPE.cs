using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net;

namespace WindowsGSM.GameServer
{
    class MCPE
    {
        private readonly string _serverId;

        public string Error;

        public const string FullName = "Minecraft: Pocket Edition Server (PocketMine-MP)";
        public const bool ToggleConsole = false;

        public string port = "19132";
        public string defaultmap = "world";
        public string maxplayers = "20";
        public string additional = "";

        public MCPE(string serverid)
        {
            _serverId = serverid;
        }

        public async void CreateServerCFG(string serverName, string serverPort, string rcon_password)
        {
            //Download server.properties
            string configPath = Functions.Path.GetServerFiles(_serverId, "server.properties");
            if (await Functions.Github.DownloadGameServerConfig(configPath, FullName, "server.properties"))
            {
                string configText = File.ReadAllText(configPath);
                configText = configText.Replace("{{hostname}}", serverName);
                configText = configText.Replace("{{rcon_password}}", rcon_password);
                configText = configText.Replace("{{port}}", serverPort);
                configText = configText.Replace("{{maxplayers}}", maxplayers);
                File.WriteAllText(configPath, configText);
            }
        }

        public async Task<Process> Start()
        {
            string workingDir = Functions.Path.GetServerFiles(_serverId);

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

            WindowsFirewall firewall = new WindowsFirewall("php.exe", phpPath);
            if (!await firewall.IsRuleExist())
            {
                firewall.AddRule();
            }

            Process p = new Process
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
                }
            };
            var serverConsole = new Functions.ServerConsole(_serverId);
            p.OutputDataReceived += serverConsole.AddOutput;
            p.ErrorDataReceived += serverConsole.AddOutput;
            p.Start();
            p.BeginOutputReadLine();
            p.BeginErrorReadLine();

            return p;
        }

        public static async Task<bool> Stop(Process p)
        {
            p.StandardInput.WriteLine("stop");

            for (int i = 0; i < 10; i++)
            {
                if (p.HasExited) { return true; }
                await Task.Delay(1000);
            }

            return false;
        }

        public async Task<bool> Install()
        {
            string serverFilesPath = Functions.Path.GetServerFiles(_serverId);

            //Download PHP-7.3-Windows-x64.zip
            string fileName = "PHP-7.3-Windows-x64.zip";
            string installUrl = "https://jenkins.pmmp.io/job/PHP-7.3-Aggregate/lastSuccessfulBuild/artifact/PHP-7.3-Windows-x64.zip";
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
                return false;
            }

            //Download PocketMine-MP.phar
            fileName = "PocketMine-MP.phar";
            installUrl = "https://jenkins.pmmp.io/job/PocketMine-MP/lastSuccessfulBuild/artifact/PocketMine-MP.phar";
            try
            {
                WebClient webClient = new WebClient();
                await webClient.DownloadFileTaskAsync(installUrl, Path.Combine(serverFilesPath, fileName));
            }
            catch
            {
                Error = $"Fail to download {fileName}";
                return false;
            }

            return true;
        }

        public async Task<bool> Update()
        {
            string fileName = "PocketMine-MP.phar";
            string installUrl = "https://jenkins.pmmp.io/job/PocketMine-MP/lastSuccessfulBuild/artifact/PocketMine-MP.phar";
            string PMMPPath = Functions.Path.GetServerFiles(_serverId, fileName);

            //Delete PocketMine-MP.phar
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
    }
}
