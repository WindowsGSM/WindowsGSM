using WindowsGSM.GameServers.Configs;
using WindowsGSM.GameServers.Engines;

namespace WindowsGSM.GameServers
{
    /// <summary>
    /// Counter-Strike: Global Offensive Dedicated Server
    /// </summary>
    public class CSGO : SourceEngine
    {
        public override string Name => "Counter-Strike: Global Offensive Dedicated Server";

        public override string ImageSource => $"/images/games/{nameof(CSGO)}.jpg";

        public override IConfig Config { get; set; } = new Configuration()
        {
            ClassName = nameof(CSGO),
            Start =
            {
                StartParameter = "-console -game csgo -ip 0.0.0.0 -port 27015 -maxplayers 24 -usercon +game_type 0 +game_mode 0 +mapgroup mg_active +map de_dust2"
            },
            SteamCMD =
            {
                Game = "csgo",
                AppId = "740",
                Username = "anonymous"
            }
        };
    }
}
