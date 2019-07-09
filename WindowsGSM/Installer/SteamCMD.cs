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
        private string Serverid { get; set; }
        private string Param { get; set; }
        private string Error { get; set; }

        public SteamCMD(string serverid)
        {
            this.Serverid = serverid;
        }

        public void SetParameter(string steamuser, string steampass, string install_dir, string app_id, bool validate)
        {
            if (steamuser == null && steampass == null)
            {
                Param = "+login anonymous";
            }
            else
            {
                Param = "+login " + steamuser + " " + steampass;
            }

            Param += " +force_install_dir \"" + install_dir + "\" +app_update " + app_id;
            if (validate) Param += " validate";

            Param += " +quit";
        }

        public async Task<bool> Download()
        {
            string installpath = MainWindow.WGSM_PATH + @"\installer\steamcmd";
            if (!Directory.Exists(installpath)) Directory.CreateDirectory(installpath);

            string exepath = installpath + @"\steamcmd.exe";

            if (File.Exists(exepath))
            {
                return true;
            }

            string installer = "https://steamcdn-a.akamaihd.net/client/installer/steamcmd.zip";
            string zippath = installpath + @"\steamcmd.zip";

            try
            {
                WebClient webClient = new WebClient();
                webClient.DownloadFileCompleted += ExtractSteamCMD;
                webClient.DownloadFileAsync(new Uri(installer), zippath);
            }
            catch
            {
                Error = "Fail to download steamcmd.exe";
                return false;
            }

            bool isDownloaded = false;
            while (!isDownloaded)
            {
                if (!File.Exists(zippath) && File.Exists(exepath))
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
            string installpath = MainWindow.WGSM_PATH + @"\installer\steamcmd";
            string zippath = installpath + @"\steamcmd.zip";

            if (File.Exists(zippath))
            {
                await Task.Run(() => ZipFile.ExtractToDirectory(zippath, installpath));

                File.Delete(zippath);
            }
        }

        public Process Run()
        {
            string exepath = MainWindow.WGSM_PATH + @"\installer\steamcmd\steamcmd.exe";

            if (!File.Exists(exepath))
            {
                Error = "steamcmd.exe not found (" + exepath + ")";
                return null;
            }

            Process p = new Process();
            p.StartInfo.FileName = exepath;
            p.StartInfo.Arguments = Param;
            p.StartInfo.WindowStyle = ProcessWindowStyle.Minimized;
            p.Start();

            return p;
        }

        public bool IsSrcdsExist()
        {
            string srcdspath = MainWindow.WGSM_PATH + @"\servers\" + Serverid + @"\serverfiles\srcds.exe";
            return File.Exists(srcdspath);
        }

        public string GetError()
        {
            return Error;
        }
    }
}
