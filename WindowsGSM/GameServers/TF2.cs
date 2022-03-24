using WindowsGSM.GameServers.Configs;
using WindowsGSM.GameServers.Engines;

namespace WindowsGSM.GameServers
{
    /// <summary>
    /// Team Fortress 2 Dedicated Server
    /// </summary>
    public class TF2 : SourceEngine
    {
        public override string Name => "Team Fortress 2 Dedicated Server";

        public override string ImageSource => $"/images/games/{nameof(TF2)}.jpg";

        public override IConfig Config { get; set; } = new Configuration()
        {
            ClassName = nameof(TF2),
            Start =
            {
                StartParameter = "-console -game tf -ip 0.0.0.0 -port 27015 -maxplayers 24 +map cp_badlands -nocrashdialog -nohltv"
            },
            Backup =
            {
                Entries =
                {
                    "tf\\addons",
                    "tf\\cfg",
                    "tf\\maps"
                }
            },
            SteamCMD =
            {
                Game = "tf",
                AppId = "232250",
                Username = "anonymous"
            }
        };
    }
}
