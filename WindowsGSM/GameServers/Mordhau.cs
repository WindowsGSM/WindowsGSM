using OpenGSQ.Protocols;
using WindowsGSM.Attributes;
using WindowsGSM.GameServers.Components;
using WindowsGSM.GameServers.Configs;
using WindowsGSM.GameServers.Protocols;
using WindowsGSM.Utilities;

namespace WindowsGSM.GameServers
{
    public class Mordhau : IGameServer
    {
        public class MordhauProtocol : SourceProtocol
        {
            public new async Task<IResponse> Query(IProtocolConfig protocolConfig)
            {
                Source source = new(protocolConfig.Protocol.IPAddress, protocolConfig.Protocol.QueryPort);
                Source.SourceResponse response = (Source.SourceResponse)await TaskEx.Run(() => source.GetInfo());

                int player = response.Keywords.Split(",").Where(x => x.Split(":")[0] == "B").Select(x => int.Parse(x.Split(":")[1])).FirstOrDefault();

                ProtocolResponse protocolResponse = new()
                {
                    Name = response.Name,
                    MapName = response.Map,
                    Player = player,
                    MaxPlayer = response.MaxPlayers,
                    Bot = response.Bots
                };

                return protocolResponse;
            }
        }

        public class StartConfig : IStartConfig
        {
            [TextField(Label = "Start Path", Required = true)]
            public string StartPath { get; set; } = "Mordhau\\Binaries\\Win64\\MordhauServer-Win64-Shipping.exe";

            [TextField(Label = "Start Parameter")]
            public string StartParameter { get; set; } = "FFA_ThePit -Port=7777 -QueryPort=27015 -BeaconPort=15000 -log";

            [RadioGroup(Text = "Console Mode")]
            [Radio(Option = "Redirect")]
            [Radio(Option = "Windowed")]
            public string ConsoleMode { get; set; } = "Redirect";
        }

        public class Configuration : IConfig, ISteamCMDConfig, IProtocolConfig
        {
            public string LocalVersion { get; set; } = string.Empty;

            public string ClassName { get; init; } = string.Empty;

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
                    "Mordhau\\Saved"
                }
            };

            [TabPanel(Text = "Start")]
            public StartConfig Start { get; set; } = new();

            [TabPanel(Text = "SteamCMD")]
            public SteamCMDConfig SteamCMD { get; set; } = new()
            {
                AppId = "629800",
                Username = "anonymous"
            };

            [TabPanel(Text = "Protocol")]
            public ProtocolConfig Protocol { get; set; } = new()
            {
                QueryPort = 27015
            };
        }

        public string Name => "MORDHAU Dedicated Server";

        public string ImageSource => $"/images/games/{nameof(Mordhau)}.jpg";

        public IProtocol? Protocol => new MordhauProtocol();

        public IConfig Config { get; set; } = new Configuration();

        public Status Status { get; set; }

        public ProcessEx Process { get; set; } = new();

        public Task<List<string>> GetVersions() => SteamCMD.GetVersions(this);

        public async Task Install(string version)
        {
            await SteamCMD.Start(this);

            // Once downloaded, the app needs to be run once to generate the config files
            await Start();
            await Stop();

            // Edit Game.ini config
            string path = Path.Combine(Config.Basic.Directory, "Mordhau", "Saved", "Config", "WindowsServer", "Game.ini");

            // Set bAdvertiseServerViaSteam to true
            if (File.Exists(path))
            {
                string text = await File.ReadAllTextAsync(path);
                string contents = text.Replace("bAdvertiseServerViaSteam=False", "bAdvertiseServerViaSteam=True");

                await File.WriteAllTextAsync(path, contents);
            }
        }

        public Task Update(string version) => SteamCMD.Start(this);

        public Task Start()
        {
            Configuration config = (Configuration)Config;

            if (config.Start.ConsoleMode == "Redirect")
            {
                Process.UseRedirect(new()
                {
                    WorkingDirectory = config.Basic.Directory,
                    FileName = Path.Combine(config.Basic.Directory, config.Start.StartPath),
                    Arguments = config.Start.StartParameter,
                    UseShellExecute = false,
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
