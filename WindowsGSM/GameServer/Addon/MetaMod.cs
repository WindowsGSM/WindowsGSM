using System.IO;
using System.IO.Compression;
using System.Net;
using System.Threading.Tasks;

namespace WindowsGSM.GameServer.Addon
{
    class MetaMod
    {
        public static async Task<bool> Install(string serverId, string modFolder)
        {
            string version = "1.10";
            string path = Functions.ServerPath.GetServerFiles(serverId, modFolder);

            try
            {
                WebClient webClient = new WebClient();
                string fileName = await webClient.DownloadStringTaskAsync($"https://mms.alliedmods.net/mmsdrop/{version}/mmsource-latest-windows");
                await webClient.DownloadFileTaskAsync($"https://mms.alliedmods.net/mmsdrop/{version}/{fileName}", Path.Combine(path, fileName));
                await Task.Run(() => { try { ZipFile.ExtractToDirectory(Path.Combine(path, fileName), path); } catch { } });
                await Task.Run(() => { try { File.Delete(Path.Combine(path, fileName)); } catch { } });

                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
