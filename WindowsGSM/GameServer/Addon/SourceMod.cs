using System.IO;
using System.IO.Compression;
using System.Net;
using System.Threading.Tasks;

namespace WindowsGSM.GameServer.Addon
{
    class SourceMod
    {
        public static async Task<bool> Install(string serverId, string modFolder)
        {
            string version = "1.10";
            string path = Functions.ServerPath.GetServerFiles(serverId, modFolder);

            try
            {
                WebClient webClient = new WebClient();
                string fileName = await webClient.DownloadStringTaskAsync($"https://sm.alliedmods.net/smdrop/{version}/sourcemod-latest-windows");
                await webClient.DownloadFileTaskAsync($"https://sm.alliedmods.net/smdrop/{version}/{fileName}", Path.Combine(path, fileName));
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
