using Microsoft.AspNetCore.WebUtilities;
using MudBlazor;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using WindowsGSM.Attributes;
using WindowsGSM.GameServers.Components;
using WindowsGSM.GameServers.Configs;
using WindowsGSM.GameServers.Mods;
using WindowsGSM.Utilities;
using WindowsGSM.Utilities.Steamworks;

namespace WindowsGSM.GameServers
{
    public class TF2 : IGameServer
    {
        public class StartConfig : IStartConfig
        {
            [TextField(Label = "Start Path", Required = true)]
            public string StartPath { get; set; } = "srcds.exe";

            [TextField(Label = "Start Parameter")]
            public string StartParameter { get; set; } = "-console -game tf -ip 0.0.0.0 -port 27015 -maxplayers 24 +map cp_badlands -nocrashdialog -nohltv";

            [RadioGroup(Text = "Console Type")]
            [Radio(Option = "Redirect Standard Input/Output")]
            [Radio(Option = "Windowed")]
            public string ConsoleMode { get; set; } = "Redirect Standard Input/Output";

            [NumericField(Label = "Query Port", Required = true, Min = 0, Max = 65535)]
            public int QueryPort { get; set; } = 27015;

            [TextField(Label = "Game Server Login Token (GSLT)", InputType = InputType.Password)]
            public string GSLT { get; set; } = string.Empty;
        }

        public class Configuration : IConfig, ISteamCMDConfig, ISourceModConfig
        {
            public string LocalVersion { get; set; } = string.Empty;

            public string ClassName => nameof(TF2);

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
                Game = "tf",
                AppId = "440",
                ServerAppId = "232250",
                Username = "anonymous",
                CreateParameter = "+app_update 232250 validate",
                UpdateParameter = "+app_update 232250",
            };

            [TabPanel(Text = "SourceMod")]
            public SourceModConfig SourceMod { get; set; } = new();
        }

        public string Name => "Team Fortress 2";

        public string ImageSource => $"/images/games/{nameof(TF2).ToLower()}.jpg";

        public IConfig Config { get; set; } = new Configuration();

        public Status Status { get; set; }

        public ProcessEx Process { get; set; } = new();

        public Task<bool> Detect(string path)
        {
            return Task.Run(() =>
            {
                return Directory.Exists(Path.Combine(path, "tf")) &&
                File.Exists(Path.Combine(path, "srcds.exe")) &&
                File.Exists(Path.Combine(path, "steam_appid.txt")) &&
                File.ReadAllText(Path.Combine(path, "steam_appid.txt")).Equals("440");
            });
        }

        public Task Backup()
        {
            throw new NotImplementedException();
        }

        public Task Restore()
        {
            throw new NotImplementedException();
        }

        public Task Create()
        {
            return SteamCMD.Start(this, updateLocalVersion: true);
        }

        public Task Update()
        {
            return SteamCMD.Start(this, updateLocalVersion: true);
        }
        
        public Task Start()
        {
            Configuration config = (Configuration)Config;

            if (config.Start.ConsoleMode == "Redirect Standard Input/Output")
            {
                Process.UseRedirectStandard(new()
                {
                    WorkingDirectory = config.Basic.Directory,
                    FileName = Path.Combine(config.Basic.Directory, config.Start.StartPath),
                    Arguments = config.Start.StartParameter,
                    UseShellExecute = false,
                    StandardOutputEncoding = Encoding.UTF8,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                });
            }
            else
            {
                Process.UseWindowed(new()
                {
                    WorkingDirectory = config.Basic.Directory,
                    FileName = Path.Combine(config.Basic.Directory, config.Start.StartPath),
                    Arguments = config.Start.StartParameter,
                    UseShellExecute = false,
                });
            }

            return Process.Start();
        }

        public async Task Stop()
        {
            Process.WriteLine("quit");

            bool exited = await Process.WaitForExit(5000);

            if (!exited)
            {
                throw new Exception("Process fail to stop");
            }
        }

        public Task<string> GetLocalVersion()
        {
            Configuration config = (Configuration)Config;

            string path = Path.Combine(config.Basic.Directory, config.SteamCMD.Game, "steam.inf");
            string text = File.ReadAllText(path);

            Regex regex = new(@"PatchVersion=(\S+)");
            Match match = regex.Match(text);

            string version = match.Groups[1].Value;

            return Task.FromResult(version);
        }

        public async Task<string> GetLatestVersion()
        {
            Configuration config = (Configuration)Config;

            Dictionary<string, string?> queryString = new()
            {
                ["appid"] = config.SteamCMD.AppId,
                ["version"] = "0",
            };

            string requestUri = QueryHelpers.AddQueryString("https://api.steampowered.com/ISteamApps/UpToDateCheck/v0001", queryString);

            using HttpClient httpClient = new();
            using HttpResponseMessage response = await httpClient.GetAsync(requestUri);
            response.EnsureSuccessStatusCode();

            string content = await response.Content.ReadAsStringAsync();

            var apiResponse = JsonSerializer.Deserialize<ResponseWrapper<SteamApps.UpToDateCheck>>(content) ?? throw new Exception("Fail to Deserialize JSON");
            string version = apiResponse.Response?.RequiredVersion?.ToString() ?? throw new Exception("Fail to get RequiredVersion");

            return version;
        }
    }
}
