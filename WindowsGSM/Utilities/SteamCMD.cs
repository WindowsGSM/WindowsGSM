using System.Text;
using WindowsGSM.Games;

namespace WindowsGSM.Utilities
{
    public static class SteamCMD
    {
        /// <summary>
        /// Get SteamCMD Command-Line Parameter
        /// </summary>
        /// <param name="gameServer"></param>
        /// <returns></returns>
        public static string GetParameter(IGameServer gameServer)
        {
            StringBuilder @string = new();
            @string.Append($"+force_install_dir \"{gameServer.Config.Basic.Directory}\" ");

            SteamCMDConfig steamCMD = ((ISteamCMD)gameServer.Config).Create;
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

        public static Task Start(IGameServer gameServer)
        {
            string parameter = GetParameter(gameServer);

            gameServer.Process.UsePseudoConsole(new()
            {
                WorkingDirectory = @"D:\WindowsGSMtest2\Installer\steamcmd", //gameServer.Config.Basic.Directory,
                FileName = @"D:\WindowsGSMtest2\Installer\steamcmd\steamcmd.exe", //Path.Combine(gameServer.Config.Basic.Directory, gameServer.Config.Start.StartPath),
                Arguments = parameter,
            });

            return gameServer.Process.Start();
        }
    }
}
