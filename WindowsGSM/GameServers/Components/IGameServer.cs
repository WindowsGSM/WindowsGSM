using System.Runtime.InteropServices;
using WindowsGSM.Extensions;
using WindowsGSM.GameServers.Configs;
using WindowsGSM.GameServers.Mods;
using WindowsGSM.GameServers.Protocols;
using WindowsGSM.Services;
using WindowsGSM.Utilities;
using static WindowsGSM.GameServers.Components.SteamCMD;
using ILogger = Serilog.ILogger;

namespace WindowsGSM.GameServers.Components
{
    /// <summary>
    /// Game Server Interface
    /// </summary>
    public interface IGameServer : IVersionable
    {
        /// <summary>
        /// Game Server Name
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Game Server Image Source
        /// </summary>
        public string ImageSource { get; }

        /// <summary>
        /// Game Server Query Protocol
        /// </summary>
        public IProtocol? Protocol { get; }

        /// <summary>
        /// Game Server Logger
        /// </summary>
        public ILogger Logger { get; set; }

        /// <summary>
        /// Game Server Configuration
        /// </summary>
        public IConfig Config { get; set; }

        /// <summary>
        /// Game Server Status
        /// </summary>
        public Status Status { get; set; }

        /// <summary>
        /// Game Server Process
        /// </summary>
        public ProcessEx Process { get; set; }

        public Task Install(string version);

        public Task Update(string version);

        public Task Start();

        public Task Stop();

        #region IGameServer Functions

        /// <summary>
        /// Start game server with status update
        /// </summary>
        /// <returns></returns>
        public async Task StartAsync()
        {
            Logger.Information("Server starting...");
            UpdateStatus(Status.Starting);

            try
            {
                

                await Start();

                if (Process.Process != null)
                {
                    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    {
                        Process.Process.ProcessorAffinity = (IntPtr)Config.Advanced.ProcessorAffinity;
                    }
                    
                    Process.Process.PriorityClass = ProcessPriorityClassExtensions.FromString(Config.Advanced.ProcessPriority);
                }

                if (Status != Status.Starting)
                {
                    throw new Exception("Server crashed while starting");
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Server failed to start");
                UpdateStatus(Status.Stopped);

                throw;
            }

            IResponse? response = await QueryAsync();

            if (response != null)
            {
                GameServerService.Responses[Config.Guid] = response;
            }

            Logger.Information("Server started");
            UpdateStatus(Status.Started);
        }

        /// <summary>
        /// Stop game server with status update
        /// </summary>
        /// <returns></returns>
        public async Task StopAsync()
        {
            Logger.Information("Server stopping...");
            UpdateStatus(Status.Stopping);

            try
            {
                await Stop();

                GameServerService.Responses.Remove(Config.Guid, out _);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Server failed to stop");

                UpdateStatus(Status.Started);
                throw;
            }

            Logger.Information("Server stopped");

            UpdateStatus(Status.Stopped);
        }

        /// <summary>
        /// Restart game server with status update
        /// </summary>
        /// <returns></returns>
        public async Task RestartAsync()
        {
            Logger.Information("Server restarting...");
            UpdateStatus(Status.Restarting);

            try
            {
                await Stop();
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Server failed to stop");
                UpdateStatus(Status.Started);

                throw;
            }

            try
            {
                await Start();
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Server failed to start");
                UpdateStatus(Status.Stopped);

                throw;
            }

            Logger.Information("Server restarted...");
            UpdateStatus(Status.Started);
        }

        /// <summary>
        /// Kill game server with status update
        /// </summary>
        /// <returns></returns>
        public async Task KillAsync()
        {
            Logger.Information("Server killing...");
            UpdateStatus(Status.Killing);

            try
            {  
                await TaskEx.Run(() => Process.Kill());

                if (!await Process.WaitForExit(5000))
                {
                    throw new Exception($"Process ID: {Process.Id}");
                }

                GameServerService.Responses.Remove(Config.Guid, out _);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Server failed to kill");
                UpdateStatus(Status.Stopped);

                throw;
            }

            Logger.Information("Server killed...");
            UpdateStatus(Status.Stopped);
        }

        /// <summary>
        /// Install game server with status update
        /// </summary>
        /// <param name="version"></param>
        /// <returns></returns>
        public async Task InstallAsync(string version)
        {
            if (!Config.Exists())
            {
                await Config.Update();

                GameServerService.AddInstance(this);
                GameServerService.UpdateServerGuids();
            }

            Directory.CreateDirectory(Config.Basic.Directory);

            Logger.Information("Server installing...");
            UpdateStatus(Status.Installing);

            try
            {
                await Install(version);

                Config.LocalVersion = version;
                await Config.Update();
            }
            catch (BuildIdMismatchException ex)
            {
                Logger.Warning($"Server current version is {ex.Message} instead of {version}");

                Config.LocalVersion = ex.Message;
                await Config.Update();
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Server failed to install");
                UpdateStatus(Status.NotInstalled);

                throw;
            }

            UpdateStatus(Status.Stopped);
        }

        /// <summary>
        /// Update game server with status update
        /// </summary>
        /// <param name="version"></param>
        /// <returns></returns>
        public async Task UpdateAsync(string version)
        {
            Logger.Information("Server updating...");
            UpdateStatus(Status.Updating);

            try
            {
                await Update(version);

                Config.LocalVersion = version;
                await Config.Update();
            }
            catch (BuildIdMismatchException ex)
            {
                Logger.Warning($"Server current version is {ex.Message} instead of {version}");

                Config.LocalVersion = ex.Message;
                await Config.Update();
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Server failed to update");
                UpdateStatus(Status.Stopped);

                throw;
            }

            Logger.Information("Server updated");
            UpdateStatus(Status.Stopped);
        }

        /// <summary>
        /// Delete game server with status update
        /// </summary>
        /// <returns></returns>
        public async Task DeleteAsync()
        {
            Logger.Information("Server deleting...");
            UpdateStatus(Status.Deleting);

            try
            {
                await DirectoryEx.DeleteIfExistsAsync(Config.Basic.Directory, true);
                await Config.Delete();

                Logger.Information("Server deleted");
                GameServerService.RemoveInstance(this);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Server failed to delete");
                UpdateStatus(Status.Stopped);

                throw;
            }
        }

        public async Task InstallModAsync(IMod mod, string version)
        {
            Logger.Information($"{mod.Name} installing...");
            UpdateStatus(Status.InstallingMod);

            try
            {
                await mod.Install(this, version);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"{mod.Name} failed to install");
                UpdateStatus(Status.Stopped);

                throw;
            }

            Logger.Information($"{mod.Name} installed");
            UpdateStatus(Status.Stopped);
        }

        public async Task UpdateModAsync(IMod mod, string version)
        {
            Logger.Information($"{mod.Name} updating...");
            UpdateStatus(Status.UpdatingMod);

            try
            {
                await mod.Update(this, version);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"{mod.Name} failed to update");
                UpdateStatus(Status.Stopped);

                throw;
            }

            Logger.Information($"{mod.Name} updated");
            UpdateStatus(Status.Stopped);
        }

        public async Task DeleteModAsync(IMod mod)
        {
            Logger.Information($"{mod.Name} deleting...");
            UpdateStatus(Status.DeletingMod);

            try
            {
                await mod.Delete(this);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"{mod.Name} failed to delete");
                UpdateStatus(Status.Stopped);

                throw;
            }

            Logger.Information($"{mod.Name} deleted");
            UpdateStatus(Status.Stopped);
        }

        /// <summary>
        /// Query game server
        /// </summary>
        /// <returns></returns>
        public async Task<IResponse?> QueryAsync()
        {
            try
            {
                if (Protocol != null)
                {
                    return await Protocol.Query((IProtocolConfig)Config) ?? null;
                }
            }
            catch
            {
                // Fail to query the game server
            }

            return null;
        }

        /// <summary>
        /// Update game server status
        /// </summary>
        /// <param name="status"></param>
        public void UpdateStatus(Status status)
        {
            Status = status;
            GameServerService.InvokeGameServersHasChanged();
        }

        #endregion
    }
}
