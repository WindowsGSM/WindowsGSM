using WindowsGSM.GameServers.Configs;
using WindowsGSM.GameServers.Engines;

namespace WindowsGSM.GameServers
{
    /// <summary>
    /// BrainBread 2 Dedicated Server
    /// </summary>
    public class BB2 : SourceEngine
    {
        public override string Name => "BrainBread 2 Dedicated Server";

        public override string ImageSource => $"/images/games/{nameof(BB2)}.jpg";

        public override IConfig Config { get; set; } = new Configuration()
        {
            ClassName = nameof(BB2),
            Start =
            {
                StartParameter = "-console -game brainbread2 -ip 0.0.0.0 -port 27015 -maxplayers 12 +map bbc_factory"
            },
            Backup =
            {
                Entries =
                {
                    "brainbread2\\addons",
                    "brainbread2\\cfg",
                    "brainbread2\\maps"
                },
            },
            SteamCMD =
            {
                Game = "brainbread2",
                AppId = "475370",
                Username = "anonymous"
            },
        };
    }
}
