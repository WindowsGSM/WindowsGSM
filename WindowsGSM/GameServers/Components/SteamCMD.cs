using System.Text;
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

                await FileEx.ExtractZip(zipPath, directory);
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
    }
}
