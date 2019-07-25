using System;
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.IO.Compression;
using System.Net;

namespace WindowsGSM.GameServer
{
    class MCPE
    {
        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        private readonly string ServerID;

        public string Error;
        public string Notice;

        public string port = "19132";
        public string defaultmap = "world";
        public string maxplayers = "20";
        public string additional = "";

        public MCPE(string serverid)
        {
            ServerID = serverid;
        }

        public void CreateServerCFG(string serverName, string serverPort, string rcon_password)
        {
            string serverConfigPath = MainWindow.WGSM_PATH + @"\servers\" + ServerID + @"\serverfiles\server.properties";

            File.Create(serverConfigPath).Dispose();

            using (TextWriter textwriter = new StreamWriter(serverConfigPath))
            {
                textwriter.WriteLine("#Properties Config file");
                textwriter.WriteLine("#Generate by WindowsGSM.exe");
                textwriter.WriteLine("language=eng");
                textwriter.WriteLine("motd=" + serverName);
                textwriter.WriteLine("server-name=" + serverName);
                textwriter.WriteLine("server-port=" + serverPort);
                textwriter.WriteLine("gamemode=0");
                textwriter.WriteLine("max-players=20");
                textwriter.WriteLine("spawn-protection=16");
                textwriter.WriteLine("white-list=off");
                textwriter.WriteLine("enable-query=on");
                textwriter.WriteLine("enable-rcon=off");
                textwriter.WriteLine("announce-player-achievements=on");
                textwriter.WriteLine("force-gamemode=off");
                textwriter.WriteLine("hardcore=off");
                textwriter.WriteLine("pvp=on");
                textwriter.WriteLine("difficulty=1");
                textwriter.WriteLine("generator-settings=");
                textwriter.WriteLine("level-name=world");
                textwriter.WriteLine("level-seed=");
                textwriter.WriteLine("level-type=DEFAULT");
                textwriter.WriteLine("rcon.password=" + rcon_password);
                textwriter.WriteLine("auto-save=on");
                textwriter.WriteLine("view-distance=8");
                textwriter.WriteLine("xbox-auth=on");
            }
        }

        public Process Start()
        {
            string workingDir = MainWindow.WGSM_PATH + @"\servers\" + ServerID + @"\serverfiles";
            string phpPath = workingDir + @"\bin\php\php.exe";
            if (!File.Exists(phpPath))
            {
                Error = "php.exe not found (" + phpPath + ")";
                return null;
            }

            string PMMPPath = workingDir + @"\PocketMine-MP.phar";
            if (!File.Exists(PMMPPath))
            {
                Error = "PocketMine-MP.phar not found (" + PMMPPath + ")";
                return null;
            }

            string serverConfigPath = workingDir + @"\server.properties";
            if (!File.Exists(serverConfigPath))
            {
                Error = "server.properties not found (" + serverConfigPath + ")";
                return null;
            }

            Process p = new Process();
            p.StartInfo.WorkingDirectory = workingDir;
            p.StartInfo.FileName = phpPath;
            p.StartInfo.Arguments = @"-c bin\php PocketMine-MP.phar";
            p.StartInfo.WindowStyle = ProcessWindowStyle.Minimized;
            p.Start();

            return p;
        }

        public async Task<bool> Stop(Process p)
        {
            SetForegroundWindow(p.MainWindowHandle);
            SendKeys.SendWait("stop");
            SendKeys.SendWait("{ENTER}");
            SendKeys.SendWait("{ENTER}");

            bool stopped = false;
            int attempt = 0;
            while (attempt < 10)
            {
                if (p != null)
                {
                    SetForegroundWindow(p.MainWindowHandle);
                    SendKeys.SendWait("{ENTER}");

                    if (p.HasExited)
                    {
                        stopped = true;
                        break;
                    }
                }

                attempt++;

                await Task.Delay(100);
            }

            SetForegroundWindow(Process.GetCurrentProcess().MainWindowHandle);

            return stopped;
        }

        public async Task<bool> Install()
        {
            string serverFilesPath = MainWindow.WGSM_PATH + @"\servers\" + ServerID + @"\serverfiles";

            //Download PHP-7.2-Windows-x64
            string installer = "https://jenkins.pmmp.io/job/PHP-7.2-Aggregate/lastSuccessfulBuild/artifact/PHP-7.2-Windows-x64.zip";
            string PHPzipPath = serverFilesPath + @"\PHP-7.2-Windows-x64.zip";
            try
            {
                WebClient webClient = new WebClient();
                webClient.DownloadFileCompleted += ExtractPHP;
                webClient.DownloadFileAsync(new Uri(installer), PHPzipPath);
            }
            catch
            {
                Error = "Fail to download PHP-7.2";
                return false;
            }

            string PHPPath = MainWindow.WGSM_PATH + @"\servers\" + ServerID + @"\serverfiles\bin\php\php.exe";
            bool isDownloaded = false;
            while (!isDownloaded)
            {
                if (!File.Exists(PHPzipPath) && File.Exists(PHPPath))
                {
                    isDownloaded = true;
                    break;
                }

                await Task.Delay(1000);
            }

            //Download PocketMine-MP.phar
            installer = "https://jenkins.pmmp.io/job/PocketMine-MP/lastSuccessfulBuild/artifact/PocketMine-MP.phar";
            string PMMPPath = serverFilesPath + @"\PocketMine-MP.phar";
            try
            {
                WebClient webClient = new WebClient();
                webClient.DownloadFileAsync(new Uri(installer), PMMPPath);
            }
            catch
            {
                Error = "Fail to download PocketMine-MP.phar";
                return false;
            }

            isDownloaded = false;
            while (!isDownloaded)
            {
                if (File.Exists(PMMPPath))
                {
                    isDownloaded = true;
                    break;
                }

                await Task.Delay(1000);
            }

            return true;
        }

        private async void ExtractPHP(object sender, System.ComponentModel.AsyncCompletedEventArgs e)
        {
            string installPath = MainWindow.WGSM_PATH + @"\servers\" + ServerID + @"\serverfiles";
            string zipPath = installPath + @"\PHP-7.2-Windows-x64.zip";

            if (File.Exists(zipPath))
            {
                await Task.Run(() => ZipFile.ExtractToDirectory(zipPath, installPath));
                await Task.Run(() => File.Delete(zipPath));
            }
        }

        public async Task<bool> Update()
        {
            //Download PocketMine-MP.phar
            string installer = "https://jenkins.pmmp.io/job/PocketMine-MP/lastSuccessfulBuild/artifact/PocketMine-MP.phar";
            string PMMPPath = MainWindow.WGSM_PATH + @"\servers\" + ServerID + @"\serverfiles\PocketMine-MP.phar";
            
            if (File.Exists(PMMPPath))
            {
                await Task.Run(() =>
                {
                    try
                    {
                        File.Delete(PMMPPath);
                    }
                    catch
                    {
                        
                    }
                });

                if (File.Exists(PMMPPath))
                {
                    Error = "Fail to delete PocketMine-MP.phar";
                    return false;
                }
            }

            try
            {
                WebClient webClient = new WebClient();
                webClient.DownloadFileAsync(new Uri(installer), PMMPPath);
            }
            catch
            {
                Error = "Fail to download PocketMine-MP.phar";
                return false;
            }

            bool isDownloaded = false;
            while (!isDownloaded)
            {
                if (File.Exists(PMMPPath))
                {
                    isDownloaded = true;
                    break;
                }

                await Task.Delay(1000);
            }

            return isDownloaded;
        }
    }
}
