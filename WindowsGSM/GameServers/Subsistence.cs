using WindowsGSM.Attributes;
using WindowsGSM.GameServers.Components;
using WindowsGSM.GameServers.Configs;
using WindowsGSM.GameServers.Protocols;
using WindowsGSM.Utilities;

namespace WindowsGSM.GameServers
{
    /// <summary>
    /// Subsistence Dedicated Server
    /// </summary>
    public class Subsistence : IGameServer
    {
        public class StartConfig : IStartConfig
        {
            [TextField(Label = "Start Path", Required = true)]
            public string StartPath { get; set; } = "Binaries\\Win64\\UDK.exe";

            [TextField(Label = "Start Parameter")]
            public string StartParameter { get; set; } = "server coldmap1?steamsockets -log";
        }

        public class Configuration : IConfig, ISteamCMDConfig, IProtocolConfig
        {
            public string LocalVersion { get; set; } = string.Empty;

            public string ClassName => nameof(Subsistence);

            public Guid Guid { get; set; }

            [TabPanel(Text = "Basic")]
            public BasicConfig Basic { get; set; } = new();

            [TabPanel(Text = "Advanced")]
            public AdvancedConfig Advanced { get; set; } = new();

            [TabPanel(Text = "Backup")]
            public BackupConfig Backup { get; set; } = new()
            {
                Entries =
                {
                    "UDKGame"
                }
            };

            [TabPanel(Text = "Start")]
            public StartConfig Start { get; set; } = new();

            [TabPanel(Text = "SteamCMD")]
            public SteamCMDConfig SteamCMD { get; set; } = new()
            {
                AppId = "1362640",
                Username = "anonymous"
            };

            [TabPanel(Text = "Protocol")]
            public ProtocolConfig Protocol { get; set; } = new()
            {
                QueryPort = 27015
            };
        }

        public string Name => "Subsistence Dedicated Server";

        public string ImageSource => $"/images/games/{nameof(Subsistence)}.jpg";

        public IProtocol? Protocol => new SourceProtocol();

        public IConfig Config { get; set; } = new Configuration();

        public Status Status { get; set; }

        public ProcessEx Process { get; set; } = new();

        public Task<List<string>> GetVersions() => SteamCMD.GetVersions(this);

        public async Task Install(string version)
        {
            await SteamCMD.Start(this);

            // Once downloaded, the app needs to be run once to generate the UDK*.ini files
            await Start();
            await Stop();
        }

        public Task Update(string version) => SteamCMD.Start(this);

        public Task Start()
        {
            Configuration config = (Configuration)Config;

            Process.UseWindowed(new()
            {
                WorkingDirectory = config.Basic.Directory,
                FileName = Path.Combine(config.Basic.Directory, config.Start.StartPath),
                Arguments = config.Start.StartParameter
            });

            return Process.Start();
        }

        public async Task Stop()
        {
            Process.Kill();

            bool exited = await Process.WaitForExit(5000);

            if (!exited)
            {
                throw new Exception("Process fail to stop");
            }
        }
    }
}
