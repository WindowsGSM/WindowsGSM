using System.Threading.Tasks;
using System.IO;
using System.Net;

namespace WindowsGSM.Functions
{
    //Link: https://github.com/WindowsGSM/Game-Server-Configs

    class Github
    {
        public static async Task<bool> DownloadGameServerConfig(string filePath, string gameFullName, string fileName)
        {
            Directory.CreateDirectory(filePath.Replace(fileName, ""));

            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }

            using (WebClient webClient = new WebClient())
            {
                await webClient.DownloadFileTaskAsync($"https://github.com/WindowsGSM/Game-Server-Configs/raw/master/{gameFullName.Replace(":", "")}/{fileName}", filePath);
            }

            return File.Exists(filePath);
        }

        public static async Task<bool> DownloadMahAppsMetroDll()
        {
            string filePath = MainWindow.WGSM_PATH + @"\MahApps.Metro.dll";

            using (WebClient webClient = new WebClient())
            {
                await webClient.DownloadFileTaskAsync("https://github.com/WindowsGSM/WindowsGSM/raw/master/packages/MahApps.Metro.1.6.5/lib/net47/MahApps.Metro.dll", filePath);
            }

            return File.Exists(filePath);
        }
    }
}
