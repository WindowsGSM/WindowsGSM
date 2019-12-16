using System;
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net;

namespace WindowsGSM.Installer
{
    class SteamCMD
    {
        private string _param;
        public string Error;

        public async Task<bool> Download()
        {
            string installPath = MainWindow.WGSM_PATH + @"\installer\steamcmd";
            if (!Directory.Exists(installPath))
            {
                Directory.CreateDirectory(installPath);
            }

            string exePath = installPath + @"\steamcmd.exe";
            if (File.Exists(exePath))
            {
                return true;
            }

            string installer = "https://steamcdn-a.akamaihd.net/client/installer/steamcmd.zip";
            string zipPath = installPath + @"\steamcmd.zip";

            try
            {
                WebClient webClient = new WebClient();
                webClient.DownloadFileCompleted += ExtractSteamCMD;
                webClient.DownloadFileAsync(new Uri(installer), zipPath);
            }
            catch
            {
                Error = "Fail to download steamcmd.exe";
                return false;
            }

            bool isDownloaded = false;
            while (!isDownloaded)
            {
                if (!File.Exists(zipPath) && File.Exists(exePath))
                {
                    isDownloaded = true;
                    break;
                }

                await Task.Delay(1000);
            }

            return isDownloaded;
        }

        private async void ExtractSteamCMD(object sender, System.ComponentModel.AsyncCompletedEventArgs e)
        {
            string installPath = MainWindow.WGSM_PATH + @"\installer\steamcmd";
            string zipPath = installPath + @"\steamcmd.zip";

            if (File.Exists(zipPath))
            {
                await Task.Run(() => ZipFile.ExtractToDirectory(zipPath, installPath));

                File.Delete(zipPath);
            }
        }

        public void SetParameter(string steamuser, string steampass, string install_dir, string set_config, string app_id, bool validate)
        {
            if (steamuser == null && steampass == null)
            {
                _param = "+login anonymous";
            }
            else
            {
                //REMARK: Not tested
                _param = "+login " + steamuser + " " + steampass;
            }

            _param += $" +force_install_dir \"{install_dir}\"";

            _param += (String.IsNullOrWhiteSpace(set_config) ? "" : " " + set_config) + $" +app_update {app_id}" + (validate ? " validate" : "");
            if (app_id == "90")
            {
                //Install 4 more times if hlds.exe
                for (int i = 0; i < 4; i++)
                {
                    _param += $" +app_update {app_id}" + (validate ? " validate" : "");
                }
            }

            _param += " +quit";
        }

        public async Task<Process> Run()
        {
            string exePath = Path.Combine(MainWindow.WGSM_PATH, @"installer\steamcmd\steamcmd.exe");

            if (!File.Exists(exePath))
            {
                Error = $"steamcmd.exe not found ({exePath})";
                return null;
            }

            WindowsFirewall firewall = new WindowsFirewall("steamcmd.exe", exePath);
            if (!await firewall.IsRuleExist())
            {
                firewall.AddRule();
            }

            Process p = new Process();
            p.StartInfo.FileName = exePath;
            p.StartInfo.Arguments = _param;
            p.StartInfo.WindowStyle = ProcessWindowStyle.Minimized;
            p.Start();

            return p;
        }
    }
}
