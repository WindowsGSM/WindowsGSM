using System;
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Text.RegularExpressions;
using System.Linq;

namespace WindowsGSM.Installer
{
    class SteamCMD
    {
        private static readonly string _installPath = Path.Combine(MainWindow.WGSM_PATH, @"installer\steamcmd");
        private static readonly string _userDataPath = Path.Combine(_installPath, "userData.txt");
        private string _param;
        public string Error;

        public SteamCMD()
        {
            if (!Directory.Exists(_installPath))
            {
                Directory.CreateDirectory(_installPath);
            }
        }

        private async Task<bool> Download()
        {
            string exePath = Path.Combine(_installPath, "steamcmd.exe");
            if (File.Exists(exePath))
            {
                return true;
            }

            string installUrl = "https://steamcdn-a.akamaihd.net/client/installer/steamcmd.zip";
            string zipPath = Path.Combine(_installPath, "steamcmd.zip");

            try
            {
                WebClient webClient = new WebClient();
                await webClient.DownloadFileTaskAsync(installUrl, zipPath);

                //Extract steamcmd.zip and delete the zip
                await Task.Run(() => ZipFile.ExtractToDirectory(zipPath, _installPath));
                await Task.Run(() => File.Delete(zipPath));
            }
            catch
            {
                Error = "Fail to download steamcmd.exe";
                return false;
            }

            return true;
        }

        public static void CreateUserDataTxtIfNotExist()
        {
            if (!File.Exists(_userDataPath))
            {
                File.Create(_userDataPath).Dispose();

                using (TextWriter textwriter = new StreamWriter(_userDataPath))
                {
                    textwriter.WriteLine("// For security and compatibility reasons, WindowsGSM suggests you to create a new steam account.");
                    textwriter.WriteLine("// More info: (https://docs.windowsgsm.com/installer/steamcmd)");
                    textwriter.WriteLine("// ");
                    textwriter.WriteLine("// Username and password - No Steam Guard (Supported + Recommanded)");
                    textwriter.WriteLine("// Username and password - Steam Guard via Email (Supported)");
                    textwriter.WriteLine("// Username and password - Steam Guard via Smartphone (NOT Supported)");
                    textwriter.WriteLine("// ");
                    textwriter.WriteLine("steamUser=\"\"");
                    textwriter.WriteLine("steamPass=\"\"");
                }
            }
        }

        public void SetParameter(string installDir, string set_config, string appId, bool validate, bool loginAnonymous = true)
        {
            if (loginAnonymous)
            {
                _param = "+login anonymous";
            }
            else
            {
                string steamUser = null, steamPass = null;

                if (File.Exists(_userDataPath))
                {
                    string[] lines = File.ReadAllLines(_userDataPath);

                    foreach (string line in lines)
                    {
                        if (line[0] == '/' && line[1] == '/')
                        {
                            continue;
                        }

                        string[] keyvalue = line.Split(new char[] { '=' }, 2);
                        if (keyvalue[0] == "steamUser")
                        {
                            steamUser = keyvalue[1].Trim('\"');
                        }
                        else if(keyvalue[0] == "steamPass")
                        {
                            steamPass = keyvalue[1].Trim('\"');
                        }
                    }
                }
                else
                {
                    CreateUserDataTxtIfNotExist();
                }

                if (string.IsNullOrWhiteSpace(steamUser) || string.IsNullOrWhiteSpace(steamPass))
                {
                    _param = null;
                    return;
                }

                _param = $"+login \"{steamUser}\" \"{steamPass}\"";
            }

            _param += $" +force_install_dir \"{installDir}\"" + (String.IsNullOrWhiteSpace(set_config) ? "" : $" {set_config}") + $" +app_update {appId}" + (validate ? " validate" : "");
            
            if (appId == "90")
            {
                //Install 4 more times if hlds.exe
                for (int i = 0; i < 4; i++)
                {
                    _param += $" +app_update {appId}" + (validate ? " validate" : "");
                }
            }

            _param += " +quit";
        }

        public async Task<Process> Run()
        {
            string exeFile = "steamcmd.exe";
            string exePath = Path.Combine(_installPath, exeFile);
            if (!File.Exists(exePath))
            {
                //If steamcmd.exe not exists, download steamcmd.exe
                if (!await Download())
                {
                    Error = $"Fail to download {exeFile}";
                    return null;
                }
            }

            if (_param == null)
            {
                Error = "Steam account is not set";
                return null;
            }

            WindowsFirewall firewall = new WindowsFirewall(exeFile, exePath);
            if (!await firewall.IsRuleExist())
            {
                firewall.AddRule();
            }

            Process p = new Process
            {
                StartInfo =
                {
                    WorkingDirectory = _installPath,
                    FileName = exePath,
                    //FileName = "cmd.exe",
                    Arguments = _param + " > log.txt",
                    //Arguments = $"/c steamcmd.exe {_param}",
                    WindowStyle = ProcessWindowStyle.Minimized,
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardInput = true,
                    StandardOutputEncoding = System.Text.Encoding.UTF8,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                },
                EnableRaisingEvents = true
            };
            p.Start();

            return p;
        }

        public string GetLocalBuild(string serverId, string appId)
        {
            string manifestFile = $"appmanifest_{appId}.acf";
            string manifestPath = Path.Combine(Functions.Path.GetServerFiles(serverId), "steamapps", manifestFile);

            if (!File.Exists(manifestPath))
            {
                Error = $"{manifestFile} is missing.";
                return "";
            }

            string text = File.ReadAllText(manifestPath);
            Regex regex = new Regex("\"buildid\".{1,}\"(.*?)\"");
            var matches = regex.Matches(text);

            if (matches.Count != 1 || matches[0].Groups.Count != 2)
            {
                Error = $"Fail to get local build";
                return "";
            }

            return matches[0].Groups[1].Value;
        }

        public async Task<string> GetRemoteBuild(string appId)
        {
            string exePath = Path.Combine(_installPath, "steamcmd.exe");
            if (!File.Exists(exePath))
            {
                //If steamcmd.exe not exists, download steamcmd.exe
                if (!await Download())
                {
                    Error = "Fail to download steamcmd.exe";
                    return "";
                }
            }

            WindowsFirewall firewall = new WindowsFirewall("steamcmd.exe", exePath);
            if (!await firewall.IsRuleExist())
            {
                firewall.AddRule();
            }

            Process p = new Process
            {
                StartInfo =
                {
                    FileName = exePath,
                    Arguments = $"+login anonymous +app_info_update 1 +app_info_print {appId} +quit",
                    WindowStyle = ProcessWindowStyle.Minimized,
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true
                }
            };
            p.Start();

            string output = await p.StandardOutput.ReadToEndAsync();
            Regex regex = new Regex("\"public\"\r\n.{0,}{\r\n.{0,}\"buildid\".{1,}\"(.*?)\"");
            var matches = regex.Matches(output);

            if (matches.Count != 1 || matches[0].Groups.Count != 2)
            {
                Error = $"Fail to get remote build";
                return "";
            }

            return matches[0].Groups[1].Value;
        }

        public async Task<string> GetRemoteBuildHLDS(string appId)
        {
            string exePath = Path.Combine(_installPath, "steamcmd.exe");
            if (!File.Exists(exePath))
            {
                //If steamcmd.exe not exists, download steamcmd.exe
                if (!await Download())
                {
                    Error = "Fail to download steamcmd.exe";
                    return "";
                }
            }

            WindowsFirewall firewall = new WindowsFirewall("steamcmd.exe", exePath);
            if (!await firewall.IsRuleExist())
            {
                firewall.AddRule();
            }

            Process p = new Process
            {
                StartInfo =
                {
                    FileName = exePath,
                    //Sometimes it fails to get if appID < 90
                    Arguments = $"+login anonymous +app_info_update 1 +app_info_print {appId} +app_info_print {appId} +app_info_print {appId} +app_info_print {appId} +quit",
                    WindowStyle = ProcessWindowStyle.Minimized,
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true
                }
            };
            p.Start();

            string output = await p.StandardOutput.ReadToEndAsync();
            Regex regex = new Regex("\"public\"\r\n.{0,}{\r\n.{0,}\"buildid\".{1,}\"(.*?)\"");
            var matches = regex.Matches(output);

            if (matches.Count < 1 || matches[1].Groups.Count < 2)
            {
                Error = $"Fail to get remote build";
                return "";
            }

            return matches[0].Groups[1].Value;
        }
    }
}
