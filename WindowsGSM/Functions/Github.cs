using System.Threading.Tasks;
using System.IO;
using System.Net;
using System;

namespace WindowsGSM.Functions
{
    //Link: https://github.com/WindowsGSM/Game-Server-Configs

    static class Github
    {
        public static async Task<bool> DownloadGameServerConfig(string filePath, string gameFullName)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(filePath));

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
    }
}
