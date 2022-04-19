using System.Text;
using System.Text.Json.Nodes;
using WindowsGSM.Attributes;
using WindowsGSM.GameServers.Components;
using WindowsGSM.GameServers.Configs;
using WindowsGSM.GameServers.Protocols;
using WindowsGSM.Utilities;
using ILogger = Serilog.ILogger;

namespace WindowsGSM.GameServers
{
    /// <summary>
    /// Minecraft: Bedrock Edition (PocketMine-MP)
    /// </summary>
    public class PMMP : IGameServer
    {
        public class StartConfig : IStartConfig
        {
            [TextField(Label = "Start Path", HelperText = "Path to start the application.", Required = true)]
            public string StartPath { get; set; } = "bin\\php\\php.exe";

            [TextField(Label = "Start Parameter", HelperText = "Command-line arguments to use when starting the application.")]
            public string StartParameter { get; set; } = "--no-wizard";

            [RadioGroup(Text = "Console Type")]
            [Radio(Option = "Pseudo Console")]
            [Radio(Option = "Redirect")]
            [Radio(Option = "Windowed")]
            public string ConsoleMode { get; set; } = "Pseudo Console";
        }

        public class InstallConfig
        {
            [TextField(Label = "PHP Download Url", HelperText = "PHP download url for PMMP", Required = true)]
            public string PhpDownloadUrl { get; set; } = "https://jenkins.pmmp.io/job/PHP-8.0-Aggregate/lastStableBuild/artifact/PHP-8.0-Windows-x64.zip";

            [CheckBox(Label = "I accept the terms of PocketMine-MP’s license. You can read the full text of the license on [GitHub](https://github.com/pmmp/PocketMine-MP/blob/master/LICENSE).", Required = true, RequiredError = "You must agree")]
            public bool PocketMineMPLicense { get; set; }
        }

        public class Configuration : IConfig, IProtocolConfig
        {
            public string LocalVersion { get; set; } = string.Empty;

            public string ClassName => nameof(PMMP);

            public Guid Guid { get; set; }

            [TabPanel(Text = "Basic")]
            public BasicConfig Basic { get; set; } = new();

            [TabPanel(Text = "Advanced")]
            public AdvancedConfig Advanced { get; set; } = new();

            [TabPanel(Text = "Backup")]
            public BackupConfig Backup { get; set; } = new();

            [TabPanel(Text = "Start")]
            public StartConfig Start { get; set; } = new();

            [TabPanel(Text = "Install")]
            public InstallConfig Install { get; set; } = new();

            [TabPanel(Text = "Protocol")]
            public ProtocolConfig Protocol { get; set; } = new()
            {
                QueryPort = 19132
            };
        }

        public string Name => "Minecraft: Bedrock Edition (PocketMine-MP)";

        public string ImageSource => $"/images/games/{nameof(PMMP)}.jpg";

        public IProtocol? Protocol => new GameSpy4Protocol();

        public ILogger Logger { get; set; } = default!;

        public IConfig Config { get; set; } = new Configuration();

        public Status Status { get; set; }

        public ProcessEx Process { get; set; } = new();

        public async Task<List<string>> GetVersions()
        {
            using HttpClient httpClient = new();
            httpClient.DefaultRequestHeaders.Add("User-Agent", "request");

            using HttpResponseMessage response = await httpClient.GetAsync("https://api.github.com/repos/pmmp/PocketMine-MP/releases");
            response.EnsureSuccessStatusCode();

            List<JsonNode> releases = await response.Content.ReadFromJsonAsync<List<JsonNode>>() ?? new();

            // Get releases tag_name where release assets contain .phar file
            List<string> versions = releases
                .Where(x => x["tag_name"] != null && x["assets"] != null && x["assets"]!.AsArray().Where(x => x?["name"]?.ToString().EndsWith(".phar") == true).Count() == 1)
                .Select(x => x["tag_name"]!.ToString()!)
                .ToList();

            return versions;
        }

        public Task Install(string version) => Update(version);

        public async Task Update(string version)
        {
            using HttpClient httpClient = new();
            httpClient.DefaultRequestHeaders.Add("User-Agent", "request");

            // Download PHP-8.0-Windows-x64.zip
            string zipPath = Path.Combine(Config.Basic.Directory, "PHP-Windows-x64.zip");
            using HttpResponseMessage response = await httpClient.GetAsync(((Configuration)Config).Install.PhpDownloadUrl);
            response.EnsureSuccessStatusCode();
            await FileEx.DeleteIfExistsAsync(zipPath);

            using (FileStream fs = new(zipPath, FileMode.CreateNew))
            {
                await response.Content.CopyToAsync(fs);
            }

            // Extract then delete zip
            await FileEx.ExtractZip(zipPath, Config.Basic.Directory, true);
            await FileEx.DeleteAsync(zipPath);

            // Download PocketMine-MP.phar
            string pharPath = Path.Combine(Config.Basic.Directory, "PocketMine-MP.phar");
            using HttpResponseMessage response2 = await httpClient.GetAsync("https://api.github.com/repos/pmmp/PocketMine-MP/releases");
            response2.EnsureSuccessStatusCode();

            List<JsonNode> releases = await response2.Content.ReadFromJsonAsync<List<JsonNode>>() ?? new();

            // Get .phar file browser_download_url
            string downloadUrl = releases
                .Where(x => x["tag_name"]?.ToString() == version)
                .Select(x => x["assets"]!.AsArray().Where(x => x!["name"]!.ToString().EndsWith(".phar")).Select(x => x!["browser_download_url"]!.ToString()).First())
                .First();

            using HttpResponseMessage response3 = await httpClient.GetAsync(downloadUrl);
            response3.EnsureSuccessStatusCode();
            await FileEx.DeleteIfExistsAsync(pharPath);

            using (FileStream fs = new(pharPath, FileMode.CreateNew))
            {
                await response3.Content.CopyToAsync(fs);
            }
        }

        public Task Start()
        {
            Configuration config = (Configuration)Config;

            if (config.Start.ConsoleMode == "Pseudo Console")
            {
                Process.UsePseudoConsole(new()
                {
                    WorkingDirectory = config.Basic.Directory,
                    FileName = Path.Combine(config.Basic.Directory, config.Start.StartPath),
                    Arguments = $"\"{Path.Combine(config.Basic.Directory, "PocketMine-MP.phar")}\" {config.Start.StartParameter}"
                });
            }
            else if (config.Start.ConsoleMode == "Redirect")
            {
                Process.UseRedirect(new()
                {
                    WorkingDirectory = config.Basic.Directory,
                    FileName = Path.Combine(config.Basic.Directory, config.Start.StartPath),
                    Arguments = $"PocketMine-MP.phar {config.Start.StartParameter}",
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    StandardOutputEncoding = Encoding.UTF8,
                    StandardErrorEncoding = Encoding.UTF8,
                    RedirectStandardInput = true,
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
                    Arguments = $"PocketMine-MP.phar {config.Start.StartParameter}"
                });
            }

            return Process.Start();
        }

        public async Task Stop()
        {
            await Process.WriteLine("stop");

            bool exited = await Process.WaitForExit((int)TimeSpan.FromSeconds(10).TotalMilliseconds);

            if (!exited)
            {
                throw new Exception("Process fail to stop");
            }
        }
    }
}
