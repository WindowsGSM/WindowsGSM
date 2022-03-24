using WindowsGSM.GameServers.Configs;
using WindowsGSM.GameServers.Engines;

namespace WindowsGSM.GameServers
{
    /// <summary>
    /// Codename CURE Dedicated Server
    /// </summary>
    public class CodenameCURE : SourceEngine
    {
        public override string Name => "Codename CURE Dedicated Server";

        public override string ImageSource => $"/images/games/{nameof(CodenameCURE)}.jpg";

        public override IConfig Config { get; set; } = new Configuration()
        {
            ClassName = nameof(CodenameCURE),
            Start =
            {
                StartParameter = "-console -game cure -ip 0.0.0.0 -port 27015 +map cbe_bunker"
            },
            SteamCMD =
            {
                Game = "cure",
                AppId = "383410",
                Username = "anonymous"
            }
        };

        public override async Task Install(string version)
        {
            await base.Install(version);

            // In the "cure/cfg" directory rename the "server.cfg.example" file to "server.cfg"
            string curePath = Path.Combine(Config.Basic.Directory, "cure");
            string oldFileName = "server.cfg.example";
            string newFileName = "server.cfg";

            File.Move(Path.Combine(curePath, oldFileName), Path.Combine(curePath, newFileName));
        }
    }
}
