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
        private static string _installPath = MainWindow.WGSM_PATH + @"\installer\steamcmd\";
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

            _param += $" +force_install_dir \"{install_dir}\"" + (String.IsNullOrWhiteSpace(set_config) ? "" : $" {set_config}") + $" +app_update {app_id}" + (validate ? " validate" : "");
            
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
            string exePath = Path.Combine(_installPath, "steamcmd.exe");
            if (!File.Exists(exePath))
            {
                //If steamcmd.exe not exists, download steamcmd.exe
                if (!await Download())
                {
                    Error = "Fail to download steamcmd.exe";
                    return null;
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
                    Arguments = _param,
                    WindowStyle = ProcessWindowStyle.Minimized
                }
            };
            p.Start();

            return p;
        }
    }
}
