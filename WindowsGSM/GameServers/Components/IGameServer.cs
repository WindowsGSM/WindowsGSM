using System.Runtime.InteropServices;
using WindowsGSM.Extensions;
using WindowsGSM.GameServers.Configs;
using WindowsGSM.GameServers.Protocols;
using WindowsGSM.Services;
using WindowsGSM.Utilities;

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
            try
            {
                UpdateStatus(Status.Starting);

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
            catch
            {
                UpdateStatus(Status.Stopped);
                throw;
            }

            IResponse? response = await QueryAsync();

            if (response != null)
            {
                GameServerService.Responses[Config.Guid] = response;
            }

            UpdateStatus(Status.Started);
        }

        /// <summary>
        /// Stop game server with status update
        /// </summary>
        /// <returns></returns>
        public async Task StopAsync()
        {
            UpdateStatus(Status.Stopping);

            try
            {
                await Stop();

                GameServerService.Responses.Remove(Config.Guid, out _);
            }
            catch
            {
                UpdateStatus(Status.Started);
                throw;
            }

            UpdateStatus(Status.Stopped);
        }

        /// <summary>
        /// Restart game server with status update
        /// </summary>
        /// <returns></returns>
        public async Task RestartAsync()
        {
            UpdateStatus(Status.Restarting);

            try
            {
                await Stop();
            }
            catch
            {
                UpdateStatus(Status.Started);
                throw;
            }

            try
            {
                await Start();
            }
            catch
            {
                UpdateStatus(Status.Stopped);
                throw;
            }

            UpdateStatus(Status.Started);
        }

        /// <summary>
        /// Kill game server with status update
        /// </summary>
        /// <returns></returns>
        public async Task KillAsync()
        {
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
            catch
            {
                UpdateStatus(Status.Stopped);
                throw;
            }

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
