using System.Threading.Tasks;
using System.IO;
using System.Net;
using System.IO.Compression;

namespace WindowsGSM.Tools
{
    class InstallAddons
    {
        public static bool? IsAMXModXAndMetaModPExists(Functions.ServerTable server)
        {
            dynamic gameServer = GameServer.Data.Class.Get(server.Game, null);
            if (!(gameServer is GameServer.Engine.GoldSource))
            {
                // Game Type not supported
                return null;
            }

            string MMPath = Functions.ServerPath.GetServersServerFiles(server.ID, gameServer.Game, "addons\\metamod.dll");
            return Directory.Exists(MMPath);
        }

        public static async Task<bool> AMXModXAndMetaModP(Functions.ServerTable server)
        {
            try
            {
                dynamic gameServer = GameServer.Data.Class.Get(server.Game, null);
                return await GameServer.Addon.AMXModX.Install(server.ID, modFolder: gameServer.Game);
            }
            catch
            {
                return false;
            }
        }

        public static bool? IsSourceModAndMetaModExists(Functions.ServerTable server)
        {
            dynamic gameServer = GameServer.Data.Class.Get(server.Game, null);
            if (!(gameServer is GameServer.Engine.Source))
            {
                // Game Type not supported
                return null;
            }

            string SMPath = Functions.ServerPath.GetServersServerFiles(server.ID, gameServer.Game, "addons\\sourcemod");
            return Directory.Exists(SMPath);
        }

        public static async Task<bool> SourceModAndMetaMod(Functions.ServerTable server)
        {
            try
            {
                dynamic gameServer = GameServer.Data.Class.Get(server.Game, null);
                return await GameServer.Addon.SourceMod.Install(server.ID, modFolder: gameServer.Game);
            }
            catch
            {
                return false;
            }
        }

        public static bool? IsDayZSALModServerExists(Functions.ServerTable server)
        {
            if (server.Game != GameServer.DAYZ.FullName)
            {
                // Game Type not supported
                return null;
            }

            string exePath = Functions.ServerPath.GetServersServerFiles(server.ID, "DZSALModServer.exe");
            return File.Exists(exePath);
        }

        public static async Task<bool> DayZSALModServer(Functions.ServerTable server)
        {
            try
            {
                string zipPath = Functions.ServerPath.GetServersServerFiles(server.ID, "dzsalmodserver.zip");

                WebClient webClient = new WebClient();
                await webClient.DownloadFileTaskAsync("http://dayzsalauncher.com/releases/dzsalmodserver.zip", zipPath);
                await Task.Run(() => { try { ZipFile.ExtractToDirectory(zipPath, Functions.ServerPath.GetServersServerFiles(server.ID)); } catch { } });
                await Task.Run(() => { try { File.Delete(zipPath); } catch { } });

                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
