using System;
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Net;
using System.Text.RegularExpressions;

namespace WindowsGSM.GameServer
{
    class GTA5
    {
        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        private readonly string _serverId;

        private string _param;
        public string Error;
        public string Notice;

        public const string FullName = "Grand Theft Auto V Dedicated Server (FiveM)";
        public const bool ToggleConsole = false;

        public string port = "30120";
        public string defaultmap = "fivem-map-skater";
        public string maxplayers = "32";
        public string additional = "+exec server.cfg";

        public GTA5(string serverid)
        {
            _serverId = serverid;
        }

        public async void CreateServerCFG(string hostname, string rcon_password, string ip, string port)
        {
            //Download server.cfg
            string configPath = Functions.Path.GetServerFiles(_serverId, @"cfx-server-data-master\server.cfg");
            if (await Functions.Github.DownloadGameServerConfig(configPath, FullName, "server.cfg"))
            {
                string configText = File.ReadAllText(configPath);
                configText = configText.Replace("{{hostname}}", hostname);
                configText = configText.Replace("{{rcon_password}}", rcon_password);
                configText = configText.Replace("{{ip}}", ip);
                configText = configText.Replace("{{port}}", port);
                configText = configText.Replace("{{maxplayers}}", maxplayers);
                File.WriteAllText(configPath, configText);
            }

            //Download sample logo
            string logoPath = Functions.Path.GetServerFiles(_serverId, @"cfx-server-data-master\myLogo.png");
            await Functions.Github.DownloadGameServerConfig(logoPath, FullName, @"cfx-server-data-master\myLogo.png");
        }

        public void SetParameter(string additional)
        {
            _param = additional;
        }

        public async Task<Process> Start()
        {
            string fxServerPath = Functions.Path.GetServerFiles(_serverId, @"server\FXServer.exe");
            if (!File.Exists(fxServerPath))
            {
                Error = $"FXServer.exe not found ({fxServerPath})";
                return null;
            }

            string citizenPath = Functions.Path.GetServerFiles(_serverId, @"server\citizen");
            if (!Directory.Exists(citizenPath))
            {
                Error = $"Directory citizen not found ({citizenPath})";
                return null;
            }

            string serverDataPath = Functions.Path.GetServerFiles(_serverId, "cfx-server-data-master");
            if (!Directory.Exists(serverDataPath))
            {
                Error = $"Directory cfx-server-data-master not found ({serverDataPath})";
                return null;
            }

            string configPath = Path.Combine(serverDataPath, "server.cfg");
            if (!File.Exists(configPath))
            {
                Notice = $"server.cfg not found ({configPath})";
            }

            WindowsFirewall firewall = new WindowsFirewall("FXServer.exe", fxServerPath);
            if (!await firewall.IsRuleExist())
            {
                firewall.AddRule();
            }

            Process p = new Process
            {
                StartInfo =
                {
                    WorkingDirectory = serverDataPath,
                    FileName = fxServerPath,
                    Arguments = $"+set citizen_dir \"{citizenPath}\" {_param}",
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
            p.StandardInput.WriteLine("quit");

            for (int i = 0; i < 10; i++)
            {
                if (p != null && p.HasExited)
                {
                    return true;
                }

                await Task.Delay(1000);
            }

            return false;
        }

        public async Task<Process> Install()
        {
            try
            {
                using (WebClient webClient = new WebClient())
                {
                    string html = await webClient.DownloadStringTaskAsync("https://runtime.fivem.net/artifacts/fivem/build_server_windows/master/");
                    Regex regex = new Regex(@"[0-9]{4}-[ -~][^\s]{39}");
                    var matches = regex.Matches(html);

                    if (matches.Count <= 0)
                    {
                        return null;
                    }

                    //Match 1 is the latest recommended
                    string recommended = regex.Match(html).ToString();

                    //Download server.zip and extract then delete server.zip
                    string serverPath = Functions.Path.GetServerFiles(_serverId, "server");
                    Directory.CreateDirectory(serverPath);
                    string zipPath = Path.Combine(serverPath, "server.zip");
                    await webClient.DownloadFileTaskAsync($"https://runtime.fivem.net/artifacts/fivem/build_server_windows/master/{recommended}/server.zip", zipPath);
                    await Task.Run(() =>
                    {
                        try
                        {
                            ZipFile.ExtractToDirectory(zipPath, serverPath);
                        }
                        catch
                        {
                            Error = "Path too long";
                        }
                    });
                    await Task.Run(() => File.Delete(zipPath));

                    //Create FiveM-version.txt and write the downloaded version with hash
                    File.WriteAllText(Functions.Path.GetServerFiles(_serverId, "FiveM-version.txt"), recommended);

                    //Download cfx-server-data-master and extract to folder cfx-server-data-master then delete cfx-server-data-master.zip
                    zipPath = Functions.Path.GetServerFiles(_serverId, "cfx-server-data-master.zip");
                    await webClient.DownloadFileTaskAsync("https://github.com/citizenfx/cfx-server-data/archive/master.zip", zipPath);
                    await Task.Run(() => ZipFile.ExtractToDirectory(zipPath, Functions.Path.GetServerFiles(_serverId)));
                    await Task.Run(() => File.Delete(zipPath));
                }

                return null;
            }
            catch
            {
                return null;
            }
        }

        public async Task<bool> Update()
        {
            //Get current version of FiveM
            string versionPath = Functions.Path.GetServerFiles(_serverId, "FiveM-version.txt");
            string version = File.Exists(versionPath) ? File.ReadAllText(versionPath) : "";

            try
            {
                using (WebClient webClient = new WebClient())
                {
                    string html = await webClient.DownloadStringTaskAsync("https://runtime.fivem.net/artifacts/fivem/build_server_windows/master/");
                    Regex regex = new Regex(@"[0-9]{4}-[ -~][^\s]{39}");
                    var matches = regex.Matches(html);

                    if (matches.Count <= 0)
                    {
                        Error = "Fail to get in https://runtime.fivem.net/artifacts/fivem/build_server_windows/master/";
                        return false;
                    }

                    //Match 1 is the latest recommended
                    string recommended = regex.Match(html).ToString();

                    if (version == recommended)
                    {
                        return true;
                    }

                    //Download server.zip and extract then delete server.zip
                    string serverPath = Functions.Path.GetServerFiles(_serverId, "server");
                    await Task.Run(() =>
                    {
                        try
                        {
                            Directory.Delete(serverPath, true);
                        }
                        catch
                        {

                        }
                    });

                    if (Directory.Exists(serverPath))
                    {
                        return false;
                    }

                    string zipPath = Path.Combine(serverPath, "server.zip");
                    await webClient.DownloadFileTaskAsync($"https://runtime.fivem.net/artifacts/fivem/build_server_windows/master/{recommended}/server.zip", zipPath);
                    await Task.Run(() =>
                    {
                        try
                        {
                            ZipFile.ExtractToDirectory(zipPath, serverPath);
                        }
                        catch
                        {
                            Error = "Path too long";
                        }
                    });
                    await Task.Run(() => File.Delete(zipPath));

                    //Create FiveM-version.txt and write the downloaded version with hash
                    File.WriteAllText(Functions.Path.GetServerFiles(_serverId, "FiveM-version.txt"), recommended);
                }

                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
