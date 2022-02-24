using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection;
using System.Text.Json;
using WindowsGSM.Games;
using WindowsGSM.Utilities;

namespace WindowsGSM.Services
{
    public class GameServerService : IHostedService, IDisposable
    {
        private static readonly string _basePath = Path.GetDirectoryName(Environment.ProcessPath)!;
        private static readonly string _configsPath = Path.Combine(_basePath, "configs");
        private static readonly string _serversPath = Path.Combine(_basePath, "servers");

        public static string BasePath => _basePath;

        public static string ConfigsPath => _configsPath;

        public static string ServersPath => _serversPath;

        public List<IGameServer> GameServers = new();

        public Dictionary<Type, string> RemoteVersions = new();

        public event Action? GameServersHasChanged;

        private readonly ILogger<GameServerService> _logger;
        private Timer? _timer;

        public GameServerService(ILogger<GameServerService> logger)
        {
            _logger = logger;

            Directory.CreateDirectory(_configsPath);
            Refresh();
        }

        public void SetStatus(IGameServer gameServer, Status status)
        {
            gameServer.Status = status;
            GameServersHasChanged?.Invoke();
        }
        /*
        public string? GetRemoteVersion(IGameServer gameServer)
        {


            gameServer.Config.ClassName
        }*/

        public BasicConfig GetNewBasicConfig(Guid guid)
        { 
            string[] names = GameServers.Select(x => x.Config.Basic.Name).ToArray();
            int number = 0;

            while (names.Contains($"WindowsGSM - Server #{++number}"));

            return new BasicConfig
            {
                Name = $"WindowsGSM - Server #{number}",
                Directory = Path.Combine(_serversPath, guid.ToString()),
            };
        }

        public List<IGameServer> GetSupportedGameServers()
        {
            return Assembly.GetExecutingAssembly().GetTypes()
                .Where(x => x.GetInterfaces().Contains(typeof(IGameServer)))
                .Select(x => (Activator.CreateInstance(x) as IGameServer)!).ToList();
        }

        public async Task Create(IGameServer gameServer)
        {
            Directory.CreateDirectory(gameServer.Config.Basic.Directory);

            gameServer.Config.Update();

            GameServers.Add(gameServer);
            GameServersHasChanged?.Invoke();

            try
            {
                SetStatus(gameServer, Status.Creating);
                await gameServer.Create();
                SetStatus(gameServer, Status.Stopped);
            }
            catch
            {
                SetStatus(gameServer, Status.Stopped);
                throw;
            }
        }

        public async Task Update(IGameServer gameServer)
        {
            try
            {
                SetStatus(gameServer, Status.Updating);
                await gameServer.Update();
                SetStatus(gameServer, Status.Stopped);
            }
            catch
            {
                SetStatus(gameServer, Status.Stopped);
                throw;
            }
        }

        public async Task Delete(IGameServer gameServer)
        {
            try
            {
                SetStatus(gameServer, Status.Deleting);

                if (Directory.Exists(gameServer.Config.Basic.Directory))
                {
                    await DirectoryEx.DeleteAsync(gameServer.Config.Basic.Directory, true);
                }
                
                gameServer.Config.Delete();
                GameServers.Remove(gameServer);
                GameServersHasChanged?.Invoke();
            }
            catch
            {
                SetStatus(gameServer, Status.Stopped);
                throw;
            }
        }

        public async Task Start(IGameServer gameServer)
        {
            try
            {
                SetStatus(gameServer, Status.Starting);
                await gameServer.Start();
                SetStatus(gameServer, Status.Started);
            }
            catch
            {
                SetStatus(gameServer, Status.Stopped);
                throw;
            }
        }

        public async Task Stop(IGameServer gameServer)
        {
            try
            {
                SetStatus(gameServer, Status.Stopping);
                await gameServer.Stop();
                SetStatus(gameServer, Status.Stopped);
            }
            catch
            {
                SetStatus(gameServer, Status.Started);
                throw;
            }
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
                SetStatus(gameServer, Status.Killing);
                await Task.Run(() => gameServer.Process.Kill());
                SetStatus(gameServer, Status.Stopped);
            }
            catch
            {
                SetStatus(gameServer, Status.Stopped);
                throw;
            }
        }

        public void Refresh()
        {
            foreach (string filePath in Directory.GetFiles(_configsPath, "*.json", SearchOption.TopDirectoryOnly))
            {
                if (TryDeserialize(File.ReadAllText(filePath), out IGameServer? server))
                {
                    server.Process.Exited += (exitCode) =>
                    {
                        Debug.WriteLine($"Exit: {exitCode}");
                        SetStatus(server, Status.Stopped);
                    };
                    GameServers.Add(server);
                }
            }
        }

        public bool TryCreateInstance(Type type, [NotNullWhen(true)] out IGameServer? gameServer)
        {
            gameServer = Activator.CreateInstance(type) as IGameServer;

            return gameServer != null;
        }



        public List<IGameServer> LoadAll()
        {
            List<IGameServer> gameServers = new();

            foreach (string filePath in Directory.GetFiles(_configsPath, "*.json", SearchOption.TopDirectoryOnly))
            {
                if (TryDeserialize(File.ReadAllText(filePath), out IGameServer? server))
                {
                    gameServers.Add(server);
                }
            }

            return gameServers;
        }

        public IGameServer Load(Guid guid)
        {
            return Deserialize(File.ReadAllText(Path.Combine(_configsPath, $"{guid}.json")));
        }

        private bool TryDeserialize(string json, [NotNullWhen(true)] out IGameServer? server)
        {
            try
            {
                server = Deserialize(json);
                return true;
            }
            catch
            {
                server = null;
                return false;
            }
        }

        private IGameServer Deserialize(string json)
        {
            Dictionary<string, object>? config = JsonSerializer.Deserialize<Dictionary<string, object>>(json);

            return config?["ClassName"].ToString() switch
            {
                nameof(MCBE) => new MCBE { Config = JsonSerializer.Deserialize<MCBE.Configuration>(json)! },
                nameof(TF2) => new TF2 { Config = JsonSerializer.Deserialize<TF2.Configuration>(json)! },
                _ => throw new Exception("Invalid JSON config file"),
            };
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _timer = new Timer(DoWork, null, TimeSpan.Zero, TimeSpan.FromSeconds(60 * 5));

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _timer?.Change(Timeout.Infinite, 0);

            return Task.CompletedTask;
        }

        private async void DoWork(object? state)
        {
            IEnumerable<IGameServer> gameServers = GameServers.DistinctBy(x => x.GetType());

            foreach (IGameServer gameServer in gameServers)
            {
                try
                {
                    RemoteVersions[gameServer.GetType()] = await gameServer.GetLatestVersion();
                    _logger.LogInformation(RemoteVersions[gameServer.GetType()]);
                }
                catch
                {

                }
                
            }

            foreach (var gameServer in gameServers)
            {
                _logger.LogInformation("Timed Hosted Service is working. Count: {Name} {f}", gameServer.GetType().Name, gameServers.Count());
            }
        }

        public void Dispose()
        {
            _timer?.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
