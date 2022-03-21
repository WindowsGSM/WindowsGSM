using WindowsGSM.GameServers.Configs;
using WindowsGSM.GameServers.Engines;

namespace WindowsGSM.GameServers
{
    public class ASRD : SourceEngine
    {
        public override string Name => "Alien Swarm: Reactive Drop Dedicated Server";

        public override string ImageSource => $"/images/games/{nameof(ASRD)}.jpg";

        public override IConfig Config { get; set; } = new Configuration()
        {
            ClassName = nameof(ASRD),
            Start =
            {
                StartParameter = "-console -game swarm +map lobby -maxplayers 4 -autoupdate",
            },
            Backup =
            {
                Entries =
                {

                },
            },
            SteamCMD =
            {
                Game = "swarm",
                AppId = "582400",
                Username = "anonymous",
            },
        };
    }
}
