using System.Text.RegularExpressions;
using WindowsGSM.GameServers.Configs;
using WindowsGSM.Utilities;

namespace WindowsGSM.GameServers.Mods
{
    public class SourceMod : IMod
    {
        public async Task<(List<string>, string)> GetVersions()
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

            return (versions.Select(x => x.ToString()).ToList(), stableVersion);
        }

        public async Task Create(IGameServer gameServer, string version)
        {
            using HttpClient httpClient = new();
            
            // Get latest windows sourcemod version file name
            using HttpResponseMessage response = await httpClient.GetAsync($"https://sm.alliedmods.net/smdrop/{version}/sourcemod-latest-windows");
            response.EnsureSuccessStatusCode();
            string latest = await response.Content.ReadAsStringAsync();

            // Download zip
            using HttpResponseMessage response2 = await httpClient.GetAsync($"https://sm.alliedmods.net/smdrop/{version}/{latest}");
            response2.EnsureSuccessStatusCode();
            string zipPath = Path.Combine(gameServer.Config.Basic.Directory, latest);

            using (FileStream fs = new(zipPath, FileMode.CreateNew))
            {
                await response2.Content.CopyToAsync(fs);
            }

            // Extract then delete zip
            string modFolder = ((ISteamCMDConfig)gameServer.Config).SteamCMD.Game;
            await FileEx.ExtractZip(zipPath, Path.Combine(gameServer.Config.Basic.Directory, modFolder));
            await FileEx.DeleteAsync(zipPath);

            ((ISourceModConfig)gameServer.Config).SourceMod.LocalVersion = version;
            await gameServer.Config.Update();
        }

        public Task Update(IGameServer gameServer, string version)
        {
            throw new NotImplementedException();
        }

        public Task Delete(IGameServer gameServer)
        {
            throw new NotImplementedException();
        }

        public bool Exists(IGameServer gameServer)
        {
            string modFolder = ((ISteamCMDConfig)gameServer.Config).SteamCMD.Game;

            return Directory.Exists(Path.Combine(gameServer.Config.Basic.Directory, modFolder, "addons", "sourcemod", "bin"));
        }
    }
}
