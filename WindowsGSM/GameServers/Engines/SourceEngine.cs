using System.Text;
using WindowsGSM.Attributes;
using WindowsGSM.GameServers.Components;
using WindowsGSM.GameServers.Configs;
using WindowsGSM.GameServers.Protocols;
using WindowsGSM.Utilities;

namespace WindowsGSM.GameServers.Engines
{
    public abstract class SourceEngine : IGameServer
    {
        public class StartConfig : IStartConfig
        {
            [TextField(Label = "Start Path", Required = true)]
            public string StartPath { get; set; } = "srcds.exe";

            [TextField(Label = "Start Parameter")]
            public string StartParameter { get; set; } = string.Empty;

            [RadioGroup(Text = "Console Mode")]
            [Radio(Option = "Redirect")]
            [Radio(Option = "Windowed")]
            public string ConsoleMode { get; set; } = "Redirect";
        }

        public class Configuration : IConfig, ISteamCMDConfig, IProtocolConfig, IMetaModConfig, ISourceModConfig
        {
            public string LocalVersion { get; set; } = string.Empty;

            public string ClassName { get; init; } = string.Empty;

            public Guid Guid { get; set; }

            [TabPanel(Text = "Basic")]
            public BasicConfig Basic { get; set; } = new();

            [TabPanel(Text = "Advanced")]
            public AdvancedConfig Advanced { get; set; } = new();

            [TabPanel(Text = "Backup")]
            public BackupConfig Backup { get; set; } = new();

            [TabPanel(Text = "Start")]
            public StartConfig Start { get; set; } = new();

            [TabPanel(Text = "SteamCMD")]
            public SteamCMDConfig SteamCMD { get; set; } = new();

            [TabPanel(Text = "Protocol")]
            public ProtocolConfig Protocol { get; set; } = new()
            {
                QueryPort = 27015
            };

            public string MetaModLocalVersion { get; set; } = string.Empty;

            public string SourceModLocalVersion { get; set; } = string.Empty;
        }

        public virtual string Name => string.Empty;

        public virtual string ImageSource => string.Empty;

        public virtual IProtocol? Protocol => new SourceProtocol();

        public virtual IConfig Config { get; set; } = new Configuration();

        public Status Status { get; set; }

        public ProcessEx Process { get; set; } = new();

        public Task<bool> Detect(string path)
        {
            return TaskEx.Run(() =>
            {
                return Directory.Exists(Path.Combine(path, ((ISteamCMDConfig)Config).SteamCMD.Game)) &&
                File.Exists(Path.Combine(path, "srcds.exe")) &&
                File.Exists(Path.Combine(path, "steam_appid.txt")) &&
                File.ReadAllText(Path.Combine(path, "steam_appid.txt")).Equals(((ISteamCMDConfig)Config).SteamCMD.AppId);
            });
        }

        public virtual Task<List<string>> GetVersions() => SteamCMD.GetVersions(this);

        public virtual Task Install(string version) => SteamCMD.Start(this);

        public virtual Task Update(string version) => SteamCMD.Start(this);

        public virtual Task Start()
        {
            Configuration config = (Configuration)Config;

            if (config.Start.ConsoleMode == "Redirect")
            {
                Process.UseRedirect(new()
                {
                    WorkingDirectory = config.Basic.Directory,
                    FileName = Path.Combine(config.Basic.Directory, config.Start.StartPath),
                    Arguments = config.Start.StartParameter,
                    StandardOutputEncoding = Encoding.UTF8,
                    StandardErrorEncoding = Encoding.UTF8,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                });
            }
            else
            {
                Process.UseWindowed(new()
                {
                    WorkingDirectory = config.Basic.Directory,
                    FileName = Path.Combine(config.Basic.Directory, config.Start.StartPath),
                    Arguments = config.Start.StartParameter
                });
            }

            return Process.Start();
        }

        public virtual async Task Stop()
        {
            Process.WriteLine("quit");

            bool exited = await Process.WaitForExit(5000);

            if (!exited)
            {
                throw new Exception("Process fail to stop");
            }
        }
    }
}
