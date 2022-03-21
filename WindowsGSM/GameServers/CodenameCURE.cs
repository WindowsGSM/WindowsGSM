using WindowsGSM.GameServers.Configs;
using WindowsGSM.GameServers.Engines;

namespace WindowsGSM.GameServers
{
    public class CodenameCURE : SourceEngine
    {
        public override string Name => "Codename CURE Dedicated Server";

        public override string ImageSource => $"/images/games/{nameof(CodenameCURE)}.jpg";

        public override IConfig Config { get; set; } = new Configuration()
        {
            ClassName = nameof(CodenameCURE),
            Start =
            {
                StartParameter = "-console -game cure -ip 0.0.0.0 -port 27015 +map cbe_bunker",
            },
            SteamCMD =
            {
                Game = "cure",
                AppId = "383410",
                Username = "anonymous",
            },
        };
    }
}
