using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text.Json;
using WindowsGSM.Extensions;
using WindowsGSM.GameServers;
using WindowsGSM.GameServers.Components;
using WindowsGSM.GameServers.Configs;
using WindowsGSM.GameServers.Mods;
using WindowsGSM.GameServers.Protocols;
using WindowsGSM.Utilities;

namespace WindowsGSM.Services
{
    public class GameServerService : IHostedService, IDisposable
    {
        public static readonly string BasePath = Path.GetDirectoryName(Environment.ProcessPath)!;
        public static readonly string BackupsPath = Path.Combine(BasePath, "backups");
        public static readonly string ConfigsPath = Path.Combine(BasePath, "configs");
        public static readonly string ServersPath = Path.Combine(BasePath, "servers");

        public static event Action? GameServersHasChanged;
        public static void InvokeGameServersHasChanged() => GameServersHasChanged?.Invoke();

        public List<IGameServer> Instances { get; private set; } = new();

        public List<IGameServer> GameServers => Assembly.GetExecutingAssembly().GetTypes()
            .Where(x => x.GetInterfaces().Contains(typeof(IGameServer)) && !x.IsAbstract)
            .Select(x => (Activator.CreateInstance(x) as IGameServer)!).OrderBy(x => x.Name).ToList();

        public List<IMod> Mods => Assembly.GetExecutingAssembly().GetTypes()
            .Where(x => x.GetInterfaces().Contains(typeof(IMod)) && !x.IsAbstract)
            .Select(x => (Activator.CreateInstance(x) as IMod)!).ToList();

        public class VersionData
        {
            public List<string> Versions { get; set; } = new();

            public DateTime DateTime { get; set; }
        }

        private readonly Dictionary<Type, VersionData> _versions = new();
        private readonly Dictionary<Type, VersionData> _modVersions = new();
        private readonly Dictionary<Type, IResponse> _responses = new();

        private readonly ILogger<GameServerService> _logger;
        private Timer? _versionsTimer, _protocolTimer;

        public GameServerService(ILogger<GameServerService> logger)
        {
            _logger = logger;

            Directory.CreateDirectory(BackupsPath);
            Directory.CreateDirectory(ConfigsPath);
            Directory.CreateDirectory(ServersPath);

            InitializeInstances();
            AutoStartInstances();
        }

        private async void AutoStartInstances()
        {
            await Parallel.ForEachAsync(Instances.Where(x => x.Status == Status.Stopped && x.Config.Advanced.AutoStart), async (gameServer, token) =>
            {
                try
                {
                    await Start(gameServer);
                }
                catch
                {

                }
            });
        }

        private void InitializeInstances()
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

        private void UpdateServerGuids()
        {
            StorageService.SetItem("ServerGuids", Instances.Select(x => x.Config.Guid.ToString()));
        }

        private void AddInstance(IGameServer gameServer)
        {
            Instances.Add(gameServer);

            gameServer.Status = string.IsNullOrEmpty(gameServer.Config.LocalVersion) ? Status.NotInstalled : Status.Stopped;
            gameServer.Process.Exited += async (exitCode) => await OnGameServerExited(gameServer);

            GameServersHasChanged?.Invoke();
        }

        private async Task OnGameServerExited(IGameServer gameServer)
        {
            if (gameServer.Status == Status.Started && gameServer.Config.Advanced.RestartOnCrash)
            {
                gameServer.UpdateStatus(Status.Restarting);

                _responses.Remove(gameServer.GetType());

                await Task.Delay(5000);
                await Start(gameServer);
            }
            else
            {
                gameServer.UpdateStatus(Status.Stopped);
            }
        }

        /// <summary>
        /// 
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

        public async Task Install(IGameServer gameServer, string version)
        {
            if (!gameServer.Config.Exists())
            {
                Directory.CreateDirectory(gameServer.Config.Basic.Directory);

                await gameServer.Config.Update();

                AddInstance(gameServer);
                UpdateServerGuids();
            }

            gameServer.UpdateStatus(Status.Installing);

            Exception? exception = null;

            try
            { 
                await gameServer.Install(version);
            }
            catch (Exception e)
            {
                exception = e;
            }

            if (string.IsNullOrEmpty(gameServer.Config.LocalVersion))
            {
                gameServer.UpdateStatus(Status.NotInstalled);
            }
            else
            {
                gameServer.UpdateStatus(Status.Stopped);
            }

            exception.ThrowIfNotNull();
        }

        public async Task Update(IGameServer gameServer, string version)
        {
            gameServer.UpdateStatus(Status.Updating);

            try
            {
                await gameServer.Update(version);
            }
            catch
            {
                gameServer.UpdateStatus(Status.Stopped);
                throw;
            }

            gameServer.UpdateStatus(Status.Stopped);
        }

        public async Task Delete(IGameServer gameServer)
        {
            gameServer.UpdateStatus(Status.Deleting);

            try
            {
                await DirectoryEx.DeleteIfExistsAsync(gameServer.Config.Basic.Directory, true);
                await gameServer.Config.Delete();
                Instances.Remove(gameServer);
                GameServersHasChanged?.Invoke();
            }
            catch
            {
                gameServer.UpdateStatus(Status.Stopped);
                throw;
            }
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

        /*
        public Task Run(IGameServer gameServer, Operation operation, string? param1 = null, IMod? mod = null)
        {
            return operation switch
            {
                Operation.Start => Start(gameServer),
                Operation.Stop => Stop(gameServer),
                Operation.Restart => Restart(gameServer),
                Operation.Kill => Kill(gameServer),
                Operation.Install => Install(gameServer, param1),
                Operation.Update => Update(gameServer, param1),
                Operation.Delete => Delete(gameServer),
                Operation.Backup => Backup(gameServer),
                Operation.Restore => Restore(gameServer, param1),
                Operation.InstallMod => InstallMod(gameServer, mod, param1),
                Operation.UpdateMod => UpdateMod(gameServer, mod, param1),
                Operation.DeleteMod => DeleteMod(gameServer, mod),
                _ => Task.CompletedTask,
            };
        }*/

        public async Task Start(IGameServer gameServer)
        {
            try
            {
                gameServer.UpdateStatus(Status.Starting);

                await gameServer.Start();

                if (gameServer.Process.Process != null)
                {

#pragma warning disable CA1416 // Validate platform compatibility
                    gameServer.Process.Process.ProcessorAffinity = (IntPtr)gameServer.Config.Advanced.ProcessorAffinity;
#pragma warning restore CA1416 // Validate platform compatibility
                    gameServer.Process.Process.PriorityClass = ProcessPriorityClassExtensions.FromString(gameServer.Config.Advanced.ProcessPriority);
                }

                if (gameServer.Status != Status.Starting)
                {
                    throw new Exception("Server crashed while starting");
                }
            }
            catch
            {
                gameServer.UpdateStatus(Status.Stopped);
                throw;
            }

            if (gameServer.Protocol != null)
            {
                try
                {
                    _responses[gameServer.GetType()] = await gameServer.Protocol.Query((IProtocolConfig)gameServer.Config);
                }
                catch
                {
                    // Fail to query the game server
                }
            }



            gameServer.UpdateStatus(Status.Started);
        }

        public async Task Stop(IGameServer gameServer)
        {
            gameServer.UpdateStatus(Status.Stopping);

            try
            {
                await gameServer.Stop();

                _responses.Remove(gameServer.GetType());
            }
            catch
            {
                gameServer.UpdateStatus(Status.Started);
                throw;
            }

            gameServer.UpdateStatus(Status.Stopped);
        }

        public async Task Restart(IGameServer gameServer)
        {
            await Stop(gameServer);
            await Start(gameServer);
        }

        public async Task Kill(IGameServer gameServer)
        {
            try
            {
                gameServer.UpdateStatus(Status.Killing);

                await TaskEx.Run(() => gameServer.Process.Kill());

                if (!await gameServer.Process.WaitForExit(5000))
                {
                    throw new Exception($"Process ID: {gameServer.Process.Id}");
                }

                _responses.Remove(gameServer.GetType());

                gameServer.UpdateStatus(Status.Stopped);
            }
            catch
            {
                gameServer.UpdateStatus(Status.Stopped);
                throw;
            }
        }

        public async Task InstallMod(IGameServer gameServer, IMod mod, string version)
        {
            try
            {
                gameServer.UpdateStatus(Status.InstallingMod);

                await mod.Install(gameServer, version);

                gameServer.UpdateStatus(Status.Stopped);
            }
            catch
            {
                gameServer.UpdateStatus(Status.Stopped);
                throw;
            }
        }

        public async Task UpdateMod(IGameServer gameServer, IMod mod, string version)
        {
            try
            {
                gameServer.UpdateStatus(Status.UpdatingMod);

                await mod.Update(gameServer, version);

                gameServer.UpdateStatus(Status.Stopped);
            }
            catch
            {
                gameServer.UpdateStatus(Status.Stopped);
                throw;
            }
        }

        public async Task DeleteMod(IGameServer gameServer, IMod mod)
        {
            try
            {
                gameServer.UpdateStatus(Status.DeletingMod);

                await mod.Delete(gameServer);

                gameServer.UpdateStatus(Status.Stopped);
            }
            catch
            {
                gameServer.UpdateStatus(Status.Stopped);
                throw;
            }
        }

        public (List<string>, DateTime?) GetVersions(IGameServer gameServer)
        {
            Type type = gameServer.GetType();

            return _versions.ContainsKey(type) ? (_versions[type].Versions, _versions[type].DateTime) : (new(), null);
        }

        public (List<string>, DateTime?) GetVersions(IMod mod)
        {
            Type type = mod.GetType();

            return _modVersions.ContainsKey(type) ? (_modVersions[type].Versions, _modVersions[type].DateTime) : (new(), null);
        }

        public (List<string>, DateTime?) GetVersions(Type modConfigType)
        {
            IMod mod = Mods.Where(x => x.ConfigType == modConfigType).First();
            Type type = mod.GetType();

            return _modVersions.ContainsKey(type) ? (_modVersions[type].Versions, _modVersions[type].DateTime) : (new(), null);
        }

        public IResponse? GetResponse(IGameServer gameServer)
        {
            Type type = gameServer.GetType();

            return _responses.ContainsKey(type) ? _responses[type] : null;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="json"></param>
        /// <param name="server"></param>
        /// <returns></returns>
        private bool TryDeserialize(string path, [NotNullWhen(true)] out IGameServer? server)
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
        private IGameServer Deserialize(string json)
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
            foreach (IGameServer gameServer in GameServers)
            {
                Type type = gameServer.GetType();

                try
                { 
                    _versions[type] = new()
                    {
                        Versions = await gameServer.GetVersions(),
                        DateTime = DateTime.Now,
                    };

                    _logger.LogInformation($"GetVersions {_versions[type].Versions[0]} ({type.Name})");
                }
                catch (Exception e)
                {
                    _logger.LogError($"Fail to GetVersions ({type.Name}) {e}");
                }
            }

            foreach (IMod mod in Mods)
            {
                Type type = mod.GetType();

                try
                {
                    _modVersions[type] = new()
                    {
                        Versions = await mod.GetVersions(),
                        DateTime = DateTime.Now,
                    };

                    _logger.LogInformation($"GetVersions {_modVersions[type].Versions[0]} ({type.Name})");
                }
                catch (Exception e)
                {
                    _logger.LogError($"Fail to GetVersions ({type.Name}) {e}");
                }
            }
        }

        private async void QueryGameServers(object? state)
        {
            List<IGameServer> queryList = Instances.Where(x => x.Protocol != null && x.Status == Status.Started).ToList();

            _logger.LogInformation($"Query Servers: Start {queryList.Count}");

            await Parallel.ForEachAsync(queryList, async (gameServer, token) =>
            {
                try
                {
                    _responses[gameServer.GetType()] = await gameServer.Protocol!.Query((IProtocolConfig)gameServer.Config);
                }
                catch
                {

                }
            });

            _logger.LogInformation($"Query Servers: Done");
        }

        /// <summary>
        /// 
        /// </summary>
        public void Dispose()
        {
            _versionsTimer?.Dispose();
            _protocolTimer?.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
