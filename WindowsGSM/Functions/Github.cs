using System.Threading.Tasks;
using System.IO;
using System.Net;
using System;

namespace WindowsGSM.Functions
{
    //Link: https://github.com/WindowsGSM/Game-Server-Configs

    class Github
    {
        public static async Task<bool> DownloadGameServerConfig(string filePath, string gameFullName)
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }

            try
            {
                using (WebClient webClient = new WebClient())
                {
                    await webClient.DownloadFileTaskAsync($"https://github.com/WindowsGSM/Game-Server-Configs/raw/master/{gameFullName.Replace(":", "")}/{System.IO.Path.GetFileName(filePath)}", filePath);
                }
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine($"Github.DownloadGameServerConfig {e}");
            }

            return File.Exists(filePath);
        }

        public static async Task<bool> DownloadMahAppsMetroDll()
        {
            string filePath = MainWindow.WGSM_PATH + @"\MahApps.Metro.dll";

            try
            {
                using (WebClient webClient = new WebClient())
                {
                    await webClient.DownloadFileTaskAsync("https://github.com/WindowsGSM/WindowsGSM/raw/master/packages/MahApps.Metro.1.6.5/lib/net47/MahApps.Metro.dll", filePath);
                }
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine($"Github.DownloadMahAppsMetroDll {e}");
            }

            return File.Exists(filePath);
        }
    }
}
