using WindowsGSM.GameServers.Configs;
using WindowsGSM.GameServers.Engines;

namespace WindowsGSM.GameServers
{
    public class AlienSwarm : SourceEngine
    {
        public override string Name => "Alien Swarm Dedicated Server";

        public override string ImageSource => $"/images/games/{nameof(AlienSwarm)}.jpg";

        public override IConfig Config { get; set; } = new Configuration()
        {
            ClassName = nameof(AlienSwarm),
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
                AppId = "635",
            },
        };
    }
}
