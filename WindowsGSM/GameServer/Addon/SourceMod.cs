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
            string version = "1.11";
            string path = Functions.ServerPath.GetServersServerFiles(serverId, modFolder);

            try
            {
                using (WebClient webClient = new WebClient())
                {
                    string fileName = await webClient.DownloadStringTaskAsync($"https://sm.alliedmods.net/smdrop/{version}/sourcemod-latest-windows");
                    await webClient.DownloadFileTaskAsync($"https://sm.alliedmods.net/smdrop/{version}/{fileName}", Path.Combine(path, fileName));
                    await Task.Run(() => { try { ZipFile.ExtractToDirectory(Path.Combine(path, fileName), path); } catch { } });
                    await Task.Run(() => { try { File.Delete(Path.Combine(path, fileName)); } catch { } });
                }
            }
            catch
            {
                return false;
            }

            return await Install_MetaMod_Source(serverId, modFolder);
        }

        private static async Task<bool> Install_MetaMod_Source(string serverId, string modFolder)
        {
            string version = "1.11";
            string path = Functions.ServerPath.GetServersServerFiles(serverId, modFolder);

            try
            {
                using (WebClient webClient = new WebClient())
                {
                    string fileName = await webClient.DownloadStringTaskAsync($"https://mms.alliedmods.net/mmsdrop/{version}/mmsource-latest-windows");
                    await webClient.DownloadFileTaskAsync($"https://mms.alliedmods.net/mmsdrop/{version}/{fileName}", Path.Combine(path, fileName));
                    await Task.Run(() => { try { ZipFile.ExtractToDirectory(Path.Combine(path, fileName), path); } catch { } });
                    await Task.Run(() => { try { File.Delete(Path.Combine(path, fileName)); } catch { } });
                }

                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
