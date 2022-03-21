using WindowsGSM.GameServers.Configs;
using WindowsGSM.GameServers.Engines;

namespace WindowsGSM.GameServers
{
    public class ClassicOffensive : SourceEngine
    {
        public override string Name => "Classic Offensive Dedicated Server";

        public override string ImageSource => $"/images/games/{nameof(ClassicOffensive)}.jpg";

        public override IConfig Config { get; set; } = new Configuration()
        {
            ClassName = nameof(ClassicOffensive),
            Start =
            {
                StartParameter = "",
            },
            Backup =
            {
                Entries =
                {
                    "csgo\\addons",
                    "csgo\\cfg",
                    "csgo\\maps",
                },
            },
            SteamCMD =
            {
                Game = "csgo",
                AppId = "601660",
                Username = "anonymous",
            },
        };
    }
}
