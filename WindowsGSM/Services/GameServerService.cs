using Microsoft.Extensions.Hosting.WindowsServices;
using Serilog;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text.Json;
using WindowsGSM.GameServers.Components;
using WindowsGSM.GameServers.Configs;
using WindowsGSM.GameServers.Mods;
using WindowsGSM.GameServers.Protocols;
using WindowsGSM.Utilities;

namespace WindowsGSM.Services
{
    public class GameServerService : IHostedService, IDisposable
    {
        public static readonly bool IsWindowsService = WindowsServiceHelpers.IsWindowsService();

        public static readonly string BasePath = Path.GetDirectoryName(Environment.ProcessPath)!;
        public static readonly string BackupsPath = Path.Combine(BasePath, "backups");
        public static readonly string ConfigsPath = Path.Combine(BasePath, "configs");
        public static readonly string LogsPath = Path.Combine(BasePath, "logs");
        public static readonly string ServersPath = Path.Combine(BasePath, "servers");

        public static event Action? GameServersHasChanged;
        public static void InvokeGameServersHasChanged() => GameServersHasChanged?.Invoke();

        public static List<IGameServer> Instances { get; private set; } = new();

        public static List<IGameServer> GameServers => Assembly.GetExecutingAssembly().GetTypes()
            .Where(x => x.GetInterfaces().Contains(typeof(IGameServer)) && !x.IsAbstract)
            .Select(x => (Activator.CreateInstance(x) as IGameServer)!).OrderBy(x => x.Name).ToList();

        public static List<IMod> Mods => Assembly.GetExecutingAssembly().GetTypes()
            .Where(x => x.GetInterfaces().Contains(typeof(IMod)) && !x.IsAbstract)
            .Select(x => (Activator.CreateInstance(x) as IMod)!).ToList();

        public interface IVersions
        {
            public List<string> Versions { get; set; }

            public DateTime DateTime { get; set; }
        }

        public class VersionData : IVersions
        {
            public List<string> Versions { get; set; } = new();

            public DateTime DateTime { get; set; }
        }

        public static ConcurrentDictionary<Type, IVersions> Versions { get; private set; } = new();
        public static ConcurrentDictionary<Guid, IResponse> Responses { get; private set; } = new();

        private readonly ILogger<GameServerService> _logger;
        private Timer? _versionsTimer, _protocolTimer;

        public GameServerService(ILogger<GameServerService> logger)
        {
            _logger = logger;

            Directory.CreateDirectory(BackupsPath);
            Directory.CreateDirectory(ConfigsPath);
            Directory.CreateDirectory(LogsPath);
            Directory.CreateDirectory(ServersPath);

            InitializeInstances();
            AutoStartInstances();
        }

        private static async void AutoStartInstances()
        {
            await Parallel.ForEachAsync(Instances.Where(x => x.Status == Status.Stopped && x.Config.Advanced.AutoStart), async (gameServer, token) =>
            {
                try
                {
                    await gameServer.StartAsync();
                }
                catch
                {

                }
            });
        }

        private static void InitializeInstances()
        {
            if (StorageService.TryGetItem("ServerGuids", out List<string>? guids))
            {
                foreach (string guid in guids)
                {
                    string configPath = Path.Combine(ConfigsPath, $"{guid}.json");

                    if (TryDeserialize(configPath, out IGameServer? gameServer) && !Instances.Select(x => x.Config.Guid).Contains(gameServer.Config.Guid))
                    {
                        AddInstance(gameServer);
                    }
                }
            }

            foreach (string configPath in Directory.GetFiles(ConfigsPath, "*.json", SearchOption.TopDirectoryOnly))
            {
                if (TryDeserialize(configPath, out IGameServer? gameServer) && !Instances.Select(x => x.Config.Guid).Contains(gameServer.Config.Guid))
                {
                    AddInstance(gameServer);
                }
            }

            UpdateServerGuids();
        }

        public static void UpdateServerGuids()
        {
            StorageService.SetItem("ServerGuids", Instances.Select(x => x.Config.Guid.ToString()));
        }

        public static void AddInstance(IGameServer gameServer)
        {
            Instances.Add(gameServer);

            string logPath = Path.Combine(LogsPath, gameServer.Config.Guid.ToString());
            Directory.CreateDirectory(logPath);

            gameServer.Logger = new LoggerConfiguration().WriteTo.File(Path.Combine(logPath, "log.txt"), rollingInterval: RollingInterval.Day, rollOnFileSizeLimit: true).CreateLogger();
            gameServer.Status = string.IsNullOrEmpty(gameServer.Config.LocalVersion) ? Status.NotInstalled : Status.Stopped; 
            gameServer.Process.Exited += async (exitCode) => await OnGameServerExited(gameServer);

            GameServersHasChanged?.Invoke();
        }

        public static void RemoveInstance(IGameServer gameServer)
        {
            Instances.Remove(gameServer);
            GameServersHasChanged?.Invoke();
        }

        private static async Task OnGameServerExited(IGameServer gameServer)
        {
            if (gameServer.Status == Status.Started && gameServer.Config.Advanced.RestartOnCrash)
            {
                gameServer.UpdateStatus(Status.Restarting);

                Responses.Remove(gameServer.Config.Guid, out _);

                await Task.Delay(5000);
                await gameServer.StartAsync();
            }
            else
            {
                gameServer.UpdateStatus(Status.Stopped);
            }
        }

        /// <summary>
        /// Get new BasicConfig with name and directory initalized
        /// </summary>
        /// <param name="guid"></param>
        /// <returns></returns>
        public BasicConfig GetNewBasicConfig(Guid guid)
        { 
            string[] names = Instances.Select(x => x.Config.Basic.Name).ToArray();
            int number = Instances.Count;

            while (names.Contains($"WindowsGSM - Server #{++number}"));

            return new()
            {
                Name = $"WindowsGSM - Server #{number}",
                Directory = Path.Combine(ServersPath, guid.ToString()),
            };
        }

        public async Task Backup(IGameServer gameServer)
        {
            gameServer.UpdateStatus(Status.Backuping);

            try
            {
                await BackupRestore.PerformFullBackup(gameServer);
            }
            catch
            {
                gameServer.UpdateStatus(Status.Stopped);
                throw;
            }

            gameServer.UpdateStatus(Status.Stopped);
        }

        public async Task Restore(IGameServer gameServer, string fileName)
        {
            gameServer.UpdateStatus(Status.Restoring);

            try
            {
                await BackupRestore.PerformRestore(gameServer, fileName);
            }
            catch
            {
                gameServer.UpdateStatus(Status.Stopped);
                throw;
            }

            gameServer.UpdateStatus(Status.Stopped);
        }

        public IVersions? GetVersions(IVersionable versionable)
        {
            return Versions.GetValueOrDefault(versionable.GetType());
        }

        public IResponse? GetResponse(IGameServer gameServer)
        {
            return Responses.GetValueOrDefault(gameServer.Config.Guid);
        }

        /// <summary>
        /// Try Deserialize
        /// </summary>
        /// <param name="json"></param>
        /// <param name="server"></param>
        /// <returns></returns>
        private static bool TryDeserialize(string path, [NotNullWhen(true)] out IGameServer? server)
        {
            try
            {
                server = Deserialize(File.ReadAllText(path));
                return true;
            }
            catch
            {
                server = null;
                return false;
            }
        }

        /// <summary>
        /// Deserialize Config Json
        /// </summary>
        /// <param name="json"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        private static IGameServer Deserialize(string json)
        {
            Dictionary<string, object> config = JsonSerializer.Deserialize<Dictionary<string, object>>(json)!;
            IGameServer gameServer = (IGameServer)Activator.CreateInstance(Type.GetType($"WindowsGSM.GameServers.{config["ClassName"]}")!)!;
            gameServer.Config = (IConfig)JsonSerializer.Deserialize(json, gameServer.Config.GetType())!;

            return gameServer;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _versionsTimer = new Timer(FetchLatestVersions, null, TimeSpan.Zero, TimeSpan.FromSeconds(60 * 5));
            _protocolTimer = new Timer(QueryGameServers, null, TimeSpan.Zero, TimeSpan.FromSeconds(10));

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _versionsTimer?.Change(Timeout.Infinite, 0);
            _protocolTimer?.Change(Timeout.Infinite, 0);

            return Task.CompletedTask;
        }
        
        private async void FetchLatestVersions(object? state)
        {
            List<IVersionable> versionables = new();
            versionables.AddRange(GameServers);
            versionables.AddRange(Mods);

            await Parallel.ForEachAsync(versionables, async (versionable, token) =>
            {
                Type type = versionable.GetType();

                try
                {
                    VersionData version = new()
                    {
                        Versions = await versionable.GetVersions(),
                        DateTime = DateTime.Now,
                    };

                    Versions[type] = version;

                    _logger.LogInformation($"GetVersions {version.Versions[0]} ({type.Name})");
                }
                catch (Exception e)
                {
                    _logger.LogError($"Fail to GetVersions ({type.Name}) {e}");
                }
            });
        }

        private async void QueryGameServers(object? state)
        {
            List<IGameServer> queryList = Instances.Where(x => x.Protocol != null && x.Status == Status.Started).ToList();

            _logger.LogInformation($"Query Servers: Start {queryList.Count}");

            await Parallel.ForEachAsync(queryList, async (gameServer, token) =>
            {
                try
                {
                    Responses[gameServer.Config.Guid] = await gameServer.Protocol!.Query((IProtocolConfig)gameServer.Config);
                }
                catch
                {

                }
            });

            _logger.LogInformation($"Query Servers: Done");
        }
        
        /// <inheritdoc/>
        public void Dispose()
        {
            _versionsTimer?.Dispose();
            _protocolTimer?.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
