using WindowsGSM.GameServers.Configs;
using WindowsGSM.GameServers.Engines;

namespace WindowsGSM.GameServers
{
    /// <summary>
    /// Blade Symphony Dedicated Server
    /// </summary>
    public class BladeSymphony : SourceEngine
    {
        public override string Name => "Blade Symphony Dedicated Server";

        public override string ImageSource => $"/images/games/{nameof(BladeSymphony)}.jpg";

        public override IConfig Config { get; set; } = new Configuration()
        {
            ClassName = nameof(BladeSymphony),
            Start =
            {
                StartPath = "bin\\win64\\srcds.exe",
                StartParameter = "-console +maxplayers 16 +map duel_winter"
            },
            Backup =
            {
                Entries =
                {
                    "berimbau\\addons",
                    "berimbau\\cfg",
                    "berimbau\\maps"
                }
            },
            SteamCMD =
            {
                AppId = "228780",
                Username = "anonymous"
            }
        };
    }
}
