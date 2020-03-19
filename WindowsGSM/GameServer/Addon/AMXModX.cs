using System.IO;
using System.IO.Compression;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace WindowsGSM.GameServer.Addon
{
    class AMXModX
    {
        public static async Task<bool> Install(string serverId, string modFolder)
        {
            string path = Functions.ServerPath.GetServersServerFiles(serverId, modFolder);

            // Install AMX MOD X
            if (!await Install_AMXMODX(path))
            {
                return false;
            }

            // Create MetaMod Directory
            string MMDllPath = Path.Combine(path, "addons", "metamod", "dlls");
            Directory.CreateDirectory(MMDllPath);

            // Install MetaMod-P
            if (!await Install_MetaMod_P(MMDllPath))
            {
                return false;
            }

            // Edit liblist.gam
            string liblistPath = Path.Combine(path, "liblist.gam");
            File.WriteAllText(liblistPath, Regex.Replace(File.ReadAllText(liblistPath), "gamedll.\".*?\"", @"gamedll ""addons\metamod\dlls\metamod.dll"""));

            // Create plugins.ini
            File.WriteAllText(Path.Combine(path, "addons", "metamod", "plugins.ini"), "win32 addons/amxmodx/dlls/amxmodx_mm.dll");

            return true;
        }

        private static async Task<bool> Install_AMXMODX(string path)
        {
            string version = "1.10";

            try
            {
                WebClient webClient = new WebClient();
                string fileName = await webClient.DownloadStringTaskAsync($"https://www.amxmodx.org/amxxdrop/{version}/amxmodx-latest-base-windows");
                string filePath = Path.Combine(path, fileName.Trim());
                await webClient.DownloadFileTaskAsync($"https://www.amxmodx.org/amxxdrop/{version}/{fileName}", filePath);
                await Task.Run(() => { try { ZipFile.ExtractToDirectory(filePath, path); } catch { } });
                await Task.Run(() => { try { File.Delete(filePath); } catch { } });
            }
            catch
            {
                return false;
            }

            return true;
        }

        private static async Task<bool> Install_MetaMod_P(string MMFolder)
        {
            string version = "1.21p37";

            try
            {
                WebClient webClient = new WebClient();
                string fileName = $"metamod-p-{version}-windows.zip";
                string filePath = Path.Combine(MMFolder, fileName);
                await webClient.DownloadFileTaskAsync($"https://downloads.sourceforge.net/project/metamod-p/Metamod-P%20Binaries/{version}/{fileName}", filePath);
                await Task.Run(() => { try { ZipFile.ExtractToDirectory(filePath, MMFolder); } catch { } });
                await Task.Run(() => { try { File.Delete(filePath); } catch { } });
            }
            catch
            {
                return false;
            }

            return true;
        }
    }
}
