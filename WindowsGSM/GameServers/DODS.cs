using WindowsGSM.GameServers.Configs;
using WindowsGSM.GameServers.Engines;

namespace WindowsGSM.GameServers
{
    public class DODS : SourceEngine
    {
        public override string Name => "Day of Defeat: Source Dedicated Server";

        public override string ImageSource => $"/images/games/{nameof(DODS)}.jpg";

        public override IConfig Config { get; set; } = new Configuration()
        {
            ClassName = nameof(DODS),
            Start =
            {
                StartParameter = "-console -game dod -ip 0.0.0.0 -port 27015 -maxplayers 24 +map dod_anzio",
            },
            SteamCMD =
            {
                Game = "dod",
                AppId = "232290",
                Username = "anonymous",
            },
        };
    }
}
