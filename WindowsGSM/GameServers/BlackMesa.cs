using WindowsGSM.GameServers.Configs;
using WindowsGSM.GameServers.Engines;

namespace WindowsGSM.GameServers
{
    public class BlackMesa : SourceEngine
    {
        public override string Name => "Black Mesa Dedicated Server";

        public override string ImageSource => $"/images/games/{nameof(BlackMesa)}.jpg";

        public override IConfig Config { get; set; } = new Configuration()
        {
            ClassName = nameof(BlackMesa),
            Start =
            {
                StartParameter = "-console -game bms +map dm_bounce -maxplayers 20",
            },
            Backup =
            {
                Entries =
                {

                },
            },
            SteamCMD =
            {
                Game = "bms",
                AppId = "346680",
                Username = "anonymous",
            },
        };
    }
}
