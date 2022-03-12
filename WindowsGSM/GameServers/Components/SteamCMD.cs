using System.Text;
using System.Text.RegularExpressions;
using WindowsGSM.GameServers.Configs;
using WindowsGSM.Services;
using WindowsGSM.Utilities;

namespace WindowsGSM.GameServers.Components
{
    public static class SteamCMD
    {
        public static readonly string FileName = Path.Combine(GameServerService.BasePath, "steamcmd", "steamcmd.exe");

        /// <summary>
        /// Get SteamCMD Command-Line Parameter
        /// </summary>
        /// <param name="gameServer"></param>
        /// <returns></returns>
        public static string GetParameter(IGameServer gameServer)
        {
            StringBuilder @string = new();
            @string.Append($"+force_install_dir \"{gameServer.Config.Basic.Directory}\" ");

            SteamCMDConfig steamCMD = ((ISteamCMDConfig)gameServer.Config).SteamCMD;
            @string.Append($"+login {(steamCMD.Username == "anonymous" ? "anonymous" : $"\"{steamCMD.Username}\" \"{steamCMD.Password}\"")} ");

            // TODO: maFile 

            // Install 4 more times if hlds.exe (steamCMD.AppId = 90)
            int count = steamCMD.AppId == "90" ? 4 : 1;

            for (int i = 0; i < count; i++)
            {
                @string.Append($"{(gameServer.Status == Status.Creating ? steamCMD.CreateParameter : steamCMD.UpdateParameter)} ");
            }

            @string.Append("+quit");

            return @string.ToString();
        }

        /// <summary>
        /// Start steamcmd.exe
        /// </summary>
        /// <param name="gameServer"></param>
        /// <param name="updateLocalVersion"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static async Task Start(IGameServer gameServer, bool updateLocalVersion = false)
        {
            SteamCMDConfig steamCMD = ((ISteamCMDConfig)gameServer.Config).SteamCMD;

            string directory = Path.GetDirectoryName(steamCMD.Path)!;
            Directory.CreateDirectory(directory);

            if (!File.Exists(steamCMD.Path))
            {
                using HttpClient httpClient = new();
                using HttpResponseMessage response = await httpClient.GetAsync("https://steamcdn-a.akamaihd.net/client/installer/steamcmd.zip");
                response.EnsureSuccessStatusCode();

                string zipPath = Path.Combine(directory, "steamcmd.zip");

                using (FileStream fs = new(zipPath, FileMode.CreateNew))
                {
                    await response.Content.CopyToAsync(fs);
                }

                await FileEx.ExtractZip(zipPath, directory, true);
                await FileEx.DeleteAsync(zipPath);
            }

            string parameter = GetParameter(gameServer);

            if (steamCMD.ConsoleMode == "Pseudo Console")
            {
                gameServer.Process.UsePseudoConsole(new()
                {
                    WorkingDirectory = directory,
                    FileName = steamCMD.Path,
                    Arguments = parameter,
                });
            }
            else if (steamCMD.ConsoleMode == "Redirect Standard Input/Output")
            {
                gameServer.Process.UseRedirectStandard(new()
                {
                    WorkingDirectory = directory,
                    FileName = steamCMD.Path,
                    Arguments = parameter,
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                });
            }
            else
            {
                gameServer.Process.UseWindowed(new()
                {
                    WorkingDirectory = directory,
                    FileName = steamCMD.Path,
                    Arguments = parameter,
                });
            }

            await gameServer.Process.Start();

            // Wait until steamcmd.exe exit
            await gameServer.Process.WaitForExit(-1);

            if (gameServer.Process.ExitCode != 0)
            {
                throw new Exception($"SteamCMD error: Exit Code {gameServer.Process.ExitCode}");
            }

            if (updateLocalVersion)
            {
                gameServer.Config.LocalVersion = await gameServer.GetLocalVersion();
                await gameServer.Config.Update();
            }
        }

        /// <summary>
        /// Get Local Build Id from appmanifest.acf
        /// </summary>
        /// <param name="gameServer"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static async Task<string> GetLocalBuildId(IGameServer gameServer)
        {
            SteamCMDConfig steamCMD = ((ISteamCMDConfig)gameServer.Config).SteamCMD;
            string appId = steamCMD.ServerAppId;

            string content = await File.ReadAllTextAsync(Path.Combine(gameServer.Config.Basic.Directory, "steamapps", $"appmanifest_{appId}.acf"));
            Regex regex = new("\"buildid\"\\s+\"(\\S*)\"");
            MatchCollection matches = regex.Matches(content);

            if (matches.Count <= 0)
            {
                throw new Exception("Could not find the local build id");
            }

            return matches[0].Groups[1].Value;
        }

        /// <summary>
        /// Get Public Build Id from AppInfo
        /// </summary>
        /// <param name="gameServer"></param>
        /// <returns>Public Build Id</returns>
        /// <exception cref="Exception"></exception>
        public static async Task<string> GetPublicBuildId(IGameServer gameServer)
        {
            string content = await GetAppInfoJson(gameServer);
            Regex regex = new("\"branches\":\\s*{\\s*\"public\":\\s*{\\s*\"buildid\":\\s*\"(\\S*)\"");
            MatchCollection matches = regex.Matches(content);

            if (matches.Count <= 0)
            {
                throw new Exception("Could not find the public build id");
            }

            return matches[0].Groups[1].Value;
        }

        /// <summary>
        /// Get AppInfo Json
        /// </summary>
        /// <param name="gameServer"></param>
        /// <returns>Json string</returns>
        public static async Task<string> GetAppInfoJson(IGameServer gameServer)
        {
            SteamCMDConfig steamCMD = ((ISteamCMDConfig)gameServer.Config).SteamCMD;
            string appId = steamCMD.ServerAppId;

            using HttpClient httpClient = new();
            using HttpResponseMessage response = await httpClient.GetAsync($"https://raw.githubusercontent.com/WindowsGSM/SteamAppInfo/main/AppInfo/{appId}.json");
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsStringAsync();
        }
    }
}
