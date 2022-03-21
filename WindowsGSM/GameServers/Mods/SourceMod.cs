using System.Text.RegularExpressions;
using WindowsGSM.GameServers.Configs;
using WindowsGSM.Utilities;

namespace WindowsGSM.GameServers.Mods
{
    public class SourceMod : IMod
    {
        public string Name => nameof(SourceMod);

        public string Description => "SourceMod (SM) is an HL2 mod which allows you to write modifications for Half-Life 2 with the Small scripting language.";

        public Type ConfigType => typeof(ISourceModConfig);

        public string GetLocalVersion(IGameServer gameServer) => ((ISourceModConfig)gameServer.Config).SourceModLocalVersion;

        public async Task<List<string>> GetVersions()
        {
            using HttpClient httpClient = new();
            using HttpResponseMessage response = await httpClient.GetAsync("https://sm.alliedmods.net/smdrop/");
            response.EnsureSuccessStatusCode();

            string content = await response.Content.ReadAsStringAsync();
            Regex regex = new("href=\"(\\d+\\.\\d+)");
            MatchCollection matches = regex.Matches(content); // Match href="1.10
            List<Version> versions = matches.Select(x => new Version(x.Groups[1].Value)).ToList(); // Values [1.10, 1.11, 1.7, 1.8, 1.9]
            versions.Sort();
            versions.Reverse();

            using HttpResponseMessage response2 = await httpClient.GetAsync("https://www.sourcemod.net/downloads.php?branch=stable");
            response2.EnsureSuccessStatusCode();

            content = await response2.Content.ReadAsStringAsync();
            regex = new("\\?branch=(\\d+\\.\\d+)");
            matches = regex.Matches(content); // Match ?branch=1.10
            string stableVersion = matches.Last().Groups[1].Value; // Value 1.10

            return new List<string> { stableVersion }.Concat(versions.Select(x => x.ToString())).Distinct().ToList();
        }

        public async Task Install(IGameServer gameServer, string version)
        {
            string modFolder = ((ISteamCMDConfig)gameServer.Config).SteamCMD.Game;
            string temporaryDirectory = await DownloadAndExtractZip(version, Path.Combine(gameServer.Config.Basic.Directory, modFolder));

            // Delete temporary directory
            await DirectoryEx.DeleteAsync(temporaryDirectory, true);

            // Update version
            ((ISourceModConfig)gameServer.Config).SourceModLocalVersion = version;
            await gameServer.Config.Update();
        }

        public async Task Update(IGameServer gameServer, string version)
        {
            string temporaryDirectory = await DownloadAndExtractZip(version);

            // Upgrade https://wiki.alliedmods.net/Upgrading_sourcemod
            string modFolder = ((ISteamCMDConfig)gameServer.Config).SteamCMD.Game;
            string newPath = Path.Combine(temporaryDirectory, "addons", "sourcemod");
            string oldPath = Path.Combine(gameServer.Config.Basic.Directory, modFolder, "addons", "sourcemod");
            string[] folders = { "bin", "extensions", "gamedata", "plugins", "translations" };

            // Overwrite the folders
            foreach (string folder in folders)
            {
                await DirectoryEx.MoveAsync(Path.Combine(newPath, folder), Path.Combine(oldPath, folder), true);
            }

            // Delete temporary directory
            await DirectoryEx.DeleteAsync(temporaryDirectory, true);

            // Update version
            ((ISourceModConfig)gameServer.Config).SourceModLocalVersion = version;
            await gameServer.Config.Update();
        }

        public async Task Delete(IGameServer gameServer)
        {
            string modFolder = ((ISteamCMDConfig)gameServer.Config).SteamCMD.Game;
            string modPath = Path.Combine(gameServer.Config.Basic.Directory, modFolder);

            // Delete folders and files
            await DirectoryEx.DeleteIfExistsAsync(Path.Combine(modPath, "addons", "sourcemod"), true);
            await FileEx.DeleteIfExistsAsync(Path.Combine(modPath, "addons", "metamod", "sourcemod.vdf"));
            await DirectoryEx.DeleteIfExistsAsync(Path.Combine(modPath, "cfg", "sourcemod"), true);

            // Update version
            ((ISourceModConfig)gameServer.Config).SourceModLocalVersion = string.Empty;
            await gameServer.Config.Update();
        }

        /// <summary>
        /// Download and Extract Zip
        /// </summary>
        /// <param name="version"></param>
        /// <param name="extractPath"></param>
        /// <returns>Temporary directory</returns>
        private static async Task<string> DownloadAndExtractZip(string version, string? extractPath = null)
        {
            using HttpClient httpClient = new();

            // Get latest windows sourcemod version file name
            using HttpResponseMessage response = await httpClient.GetAsync($"https://sm.alliedmods.net/smdrop/{version}/sourcemod-latest-windows");
            response.EnsureSuccessStatusCode();
            string latest = await response.Content.ReadAsStringAsync();

            // Download zip
            using HttpResponseMessage response2 = await httpClient.GetAsync($"https://sm.alliedmods.net/smdrop/{version}/{latest}");
            response2.EnsureSuccessStatusCode();
            string temporaryDirectory = DirectoryEx.CreateTemporaryDirectory();
            string zipPath = Path.Combine(temporaryDirectory, latest);

            using (FileStream fs = new(zipPath, FileMode.CreateNew))
            {
                await response2.Content.CopyToAsync(fs);
            }

            // Extract zip
            await FileEx.ExtractZip(zipPath, extractPath ?? temporaryDirectory, true);

            return temporaryDirectory;
        }
    }
}
