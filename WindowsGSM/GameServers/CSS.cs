using WindowsGSM.GameServers.Configs;
using WindowsGSM.GameServers.Engines;

namespace WindowsGSM.GameServers
{
    public class CSS : SourceEngine
    {
        public override string Name => "Counter-Strike: Source Dedicated Server";

        public override string ImageSource => $"/images/games/{nameof(CSS)}.jpg";

        public override IConfig Config { get; set; } = new Configuration()
        {
            ClassName = nameof(CSS),
            Start =
            {
                StartParameter = "-console -game cstrike -secure -ip 0.0.0.0 -port 27015 -maxplayers 22 +map de_dust",
            },
            Backup =
            {
                Entries =
                {
                    "cstrike\\addons",
                    "cstrike\\cfg",
                    "cstrike\\maps",
                },
            },
            SteamCMD =
            {
                Game = "cstrike",
                AppId = "232330",
                Username = "anonymous",
            },
        };
    }
}
