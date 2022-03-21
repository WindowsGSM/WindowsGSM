using WindowsGSM.GameServers.Configs;
using WindowsGSM.GameServers.Engines;

namespace WindowsGSM.GameServers
{
    public class L4D2 : SourceEngine
    {
        public override string Name => "Left 4 Dead 2 Dedicated Server";

        public override string ImageSource => $"/images/games/{nameof(L4D2)}.jpg";

        public override IConfig Config { get; set; } = new Configuration()
        {
            ClassName = nameof(L4D2),
            Start =
            {
                StartParameter = "-console -game left4dead2 -ip 0.0.0.0 -port 27015 -maxplayers 24 +map c1m1_hotel",
            },
            Backup =
            {
                Entries =
                {
                    "left4dead2\\addons",
                    "left4dead2\\cfg",
                    "left4dead2\\maps",
                },
            },
            SteamCMD =
            {
                Game = "left4dead2",
                AppId = "222860",
                Username = "anonymous",
            },
        };
    }
}
