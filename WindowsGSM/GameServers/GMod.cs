using WindowsGSM.GameServers.Configs;
using WindowsGSM.GameServers.Engines;

namespace WindowsGSM.GameServers
{
    public class GMod : SourceEngine
    {
        public override string Name => "Garry's Mod Dedicated Server";

        public override string ImageSource => $"/images/games/{nameof(GMod)}.jpg";

        public override IConfig Config { get; set; } = new Configuration()
        {
            ClassName = nameof(GMod),
            Start =
            {
                StartParameter = "-console -game garrysmod -ip 0.0.0.0 -port 27015 -maxplayers 24 +map gm_construct -tickrate 66 +gamemode sandbox",
            },
            Backup =
            {
                Entries =
                {
                    "garrysmod\\addons",
                    "garrysmod\\cfg",
                    "garrysmod\\maps",
                },
            },
            SteamCMD =
            {
                Game = "garrysmod",
                AppId = "4020",
                Username = "anonymous",
            },
        };
    }
}
