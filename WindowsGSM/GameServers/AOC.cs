using WindowsGSM.GameServers.Configs;
using WindowsGSM.GameServers.Engines;

namespace WindowsGSM.GameServers
{
    public class AOC : SourceEngine
    {
        public override string Name => "Age of Chivalry Dedicated Server";

        public override string ImageSource => $"/images/games/{nameof(AOC)}.jpg";

        public override IConfig Config { get; set; } = new Configuration()
        {
            ClassName = nameof(AOC),
            Start =
            {
                StartParameter = "-console -game ageofchivalry +ip 0.0.0.0 -port 27015 +maxplayers 32 +map aoc_siege",
            },
            Backup =
            {
                Entries =
                {
                    
                },
            },
            SteamCMD =
            {
                Game = "ageofchivalry",
                AppId = "17515",
                Username = "anonymous",
            },
        };
    }
}
