using WindowsGSM.Attributes;
using WindowsGSM.GameServers.Components;
using WindowsGSM.GameServers.Configs;
using WindowsGSM.Utilities;

namespace WindowsGSM.GameServers
{
    public class Subsistence : IGameServer
    {
        public class StartConfig : IStartConfig
        {
            [TextField(Label = "Start Path", Required = true)]
            public string StartPath { get; set; } = "Binaries/Win64/UDK.exe";

            [TextField(Label = "Start Parameter")]
            public string StartParameter { get; set; } = "server coldmap1?steamsockets -log";

            [RadioGroup(Text = "Console Type")]
            [Radio(Option = "Windowed")]
            public string ConsoleMode { get; set; } = "Windowed";
        }

        public class Configuration : IConfig, ISteamCMDConfig
        {
            public string LocalVersion { get; set; } = string.Empty;

            public string ClassName => nameof(Subsistence);

            public Guid Guid { get; set; }

            [TabPanel(Text = "Basic")]
            public BasicConfig Basic { get; set; } = new();

            [TabPanel(Text = "Advanced")]
            public AdvancedConfig Advanced { get; set; } = new();

            [TabPanel(Text = "Start")]
            public StartConfig Start { get; set; } = new();

            [TabPanel(Text = "SteamCMD")]
            public SteamCMDConfig SteamCMD { get; set; } = new()
            {
                AppId = "418030",
                ServerAppId = "1362640",
                Username = "anonymous",
                CreateParameter = "+app_update 1362640 validate",
                UpdateParameter = "+app_update 1362640",
            };
        }

        public string Name => "Subsistence Dedicated Server";

        public string ImageSource => $"/images/games/{nameof(Subsistence)}.jpg";

        public IConfig Config { get; set; } = new Configuration();
        public Status Status { get; set; }
        public ProcessEx Process { get; set; } = new();

        public Task Backup()
        {
            throw new NotImplementedException();
        }

        public Task Restore()
        {
            throw new NotImplementedException();
        }

        public Task Create() => SteamCMD.Start(this, updateLocalVersion: true);

        public Task Update() => SteamCMD.Start(this, updateLocalVersion: true);

        public Task Start()
        {
            Configuration config = (Configuration)Config;

            Process.UseWindowed(new()
            {
                WorkingDirectory = config.Basic.Directory,
                FileName = Path.Combine(config.Basic.Directory, config.Start.StartPath),
                Arguments = config.Start.StartParameter,
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

        public Task<string> GetLocalVersion() => SteamCMD.GetLocalBuildId(this);

        public async Task<List<string>> GetVersions() => new() { await SteamCMD.GetPublicBuildId(this) };
    }
}
