using WindowsGSM.GameServers.Configs;
using WindowsGSM.GameServers.Engines;

namespace WindowsGSM.GameServers
{
    /// <summary>
    /// Alien Swarm: Reactive Drop Dedicated Server
    /// </summary>
    public class ASRD : SourceEngine
    {
        public override string Name => "Alien Swarm: Reactive Drop Dedicated Server";

        public override string ImageSource => $"/images/games/{nameof(ASRD)}.jpg";

        public override IConfig Config { get; set; } = new Configuration()
        {
            ClassName = nameof(ASRD),
            Start =
            {
                StartParameter = "-console -game reactivedrop +map lobby -maxplayers 4"
            },
            Backup =
            {
                Entries =
                {
                    "reactivedrop\\addons",
                    "reactivedrop\\cfg",
                    "reactivedrop\\maps"
                },
            },
            SteamCMD =
            {
                Game = "reactivedrop",
                AppId = "582400",
                Username = "anonymous"
            }
        };
    }
}
