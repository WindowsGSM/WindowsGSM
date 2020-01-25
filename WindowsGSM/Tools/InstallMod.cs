using System.Threading.Tasks;
using System.IO;
using System.Net;
using System.IO.Compression;

namespace WindowsGSM.Tools
{
    /// <summary>
    /// 
    /// Implement in the future... - 1/25/2020 9:54 PM
    /// 
    /// </summary>
    class InstallMod
    {
        public static async Task<bool> SourceMod(string path)
        {
            string version = "1.10";

            try
            {
                WebClient webClient = new WebClient();
                string fileName = webClient.DownloadString($"https://sm.alliedmods.net/smdrop/{version}/sourcemod-latest-windows");
                await webClient.DownloadFileTaskAsync("https://sm.alliedmods.net/smdrop/{version}/{fileName}", Path.Combine(path, fileName));

                //Extract sourcemod-1.10.0-git6460-windows.zip and delete the zip
                await Task.Run(() => ZipFile.ExtractToDirectory(Path.Combine(path, fileName), path));
                await Task.Run(() => File.Delete(Path.Combine(path, fileName)));

                return true;
            }
            catch
            {
                return false;
            }
        }

        public static async Task<bool> Metamod(string path)
        {
            string version = "1.10";

            try
            {
                WebClient webClient = new WebClient();
                string fileName = webClient.DownloadString($"https://mms.alliedmods.net/mmsdrop/{version}/mmsource-latest-windows");
                await webClient.DownloadFileTaskAsync("https://mms.alliedmods.net/mmsdrop/{version}/{fileName}", Path.Combine(path, fileName));

                //Extract sourcemod-1.10.0-git6460-windows.zip and delete the zip
                await Task.Run(() => ZipFile.ExtractToDirectory(Path.Combine(path, fileName), path));
                await Task.Run(() => File.Delete(Path.Combine(path, fileName)));

                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
