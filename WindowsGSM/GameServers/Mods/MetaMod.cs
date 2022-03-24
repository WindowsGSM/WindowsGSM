using System.Text.RegularExpressions;
using WindowsGSM.GameServers.Components;
using WindowsGSM.GameServers.Configs;
using WindowsGSM.Utilities;

namespace WindowsGSM.GameServers.Mods
{
    public class MetaMod : IMod
    {
        public string Name => nameof(MetaMod);

        public string Description => "Metamod:Source is a C++ plugin environment for Half-Life 2.";

        public Type ConfigType => typeof(IMetaModConfig);

        public string GetLocalVersion(IGameServer gameServer) => ((IMetaModConfig)gameServer.Config).MetaModLocalVersion;

        public async Task<List<string>> GetVersions()
        {
            using HttpClient httpClient = new();
            using HttpResponseMessage response = await httpClient.GetAsync("https://mms.alliedmods.net/mmsdrop/");
            response.EnsureSuccessStatusCode();

            string content = await response.Content.ReadAsStringAsync();
            Regex regex = new("href=\"(\\d+\\.\\d+)");
            MatchCollection matches = regex.Matches(content); // Match href="1.10
            List<Version> versions = matches.Select(x => new Version(x.Groups[1].Value)).ToList(); // Values [1.10, 1.11, 1.7, 1.8, 1.9]
            versions.Sort();
            versions.Reverse();

            using HttpResponseMessage response2 = await httpClient.GetAsync("https://www.sourcemm.net/downloads.php/?branch=stable");
            response2.EnsureSuccessStatusCode();

            content = await response2.Content.ReadAsStringAsync();
            regex = new("\\?branch=(\\d+\\.\\d+)");
            matches = regex.Matches(content); // Match ?branch=1.11
            string stableVersion = matches.Last().Groups[1].Value; // Value 1.11

            return new List<string> { stableVersion }.Concat(versions.Select(x => x.ToString())).Distinct().ToList();
        }

        public async Task Install(IGameServer gameServer, string version)
        {
            using HttpClient httpClient = new();

            // Get latest windows sourcemod version file name
            using HttpResponseMessage response = await httpClient.GetAsync($"https://mms.alliedmods.net/mmsdrop/{version}/mmsource-latest-windows");
            response.EnsureSuccessStatusCode();
            string latest = await response.Content.ReadAsStringAsync();

            // Download zip
            using HttpResponseMessage response2 = await httpClient.GetAsync($"https://mms.alliedmods.net/mmsdrop/{version}/{latest}");
            response2.EnsureSuccessStatusCode();
            string temporaryDirectory = DirectoryEx.CreateTemporaryDirectory();
            string zipPath = Path.Combine(temporaryDirectory, latest);

            using (FileStream fs = new(zipPath, FileMode.CreateNew))
            {
                await response2.Content.CopyToAsync(fs);
            }

            // Extract zip
            string modFolder = ((ISteamCMDConfig)gameServer.Config).SteamCMD.Game;
            await FileEx.ExtractZip(zipPath, Path.Combine(gameServer.Config.Basic.Directory, modFolder), true);

            // Delete temporary directory
            await DirectoryEx.DeleteAsync(temporaryDirectory, true);

            ((IMetaModConfig)gameServer.Config).MetaModLocalVersion = version;
            await gameServer.Config.Update();
        }

        public Task Update(IGameServer gameServer, string version) => Install(gameServer, version);

        public async Task Delete(IGameServer gameServer)
        {
            string modFolder = ((ISteamCMDConfig)gameServer.Config).SteamCMD.Game;
            string addonPath = Path.Combine(gameServer.Config.Basic.Directory, modFolder, "addons");

            await DirectoryEx.DeleteIfExistsAsync(Path.Combine(addonPath, "metamod", "bin"), true);
            await FileEx.DeleteIfExistsAsync(Path.Combine(addonPath, "metamod", "metaplugins.ini"));
            await FileEx.DeleteIfExistsAsync(Path.Combine(addonPath, "metamod", "README.txt"));
            await FileEx.DeleteIfExistsAsync(Path.Combine(addonPath, "metamod.vdf"));
            await FileEx.DeleteIfExistsAsync(Path.Combine(addonPath, "metamod_x64.vdf"));

            ((IMetaModConfig)gameServer.Config).MetaModLocalVersion = string.Empty;
            await gameServer.Config.Update();
        }
    }
}
