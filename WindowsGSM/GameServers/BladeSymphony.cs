using WindowsGSM.GameServers.Configs;
using WindowsGSM.GameServers.Engines;

namespace WindowsGSM.GameServers
{
    public class BladeSymphony : SourceEngine
    {
        public override string Name => "Blade Symphony Dedicated Server";

        public override string ImageSource => $"/images/games/{nameof(BladeSymphony)}.jpg";

        public override IConfig Config { get; set; } = new Configuration()
        {
            ClassName = nameof(BladeSymphony),
            Start =
            {
                StartParameter = "",
            },
            Backup =
            {
                Entries =
                {
                    "tf\\addons",
                    "tf\\cfg",
                    "tf\\maps",
                },
            },
            SteamCMD =
            {
                Game = "tf",
                AppId = "228780",
                Username = "anonymous",
            },
        };
    }
}
