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

        public void UpdateStatus(Status status)
        {
            Status = status;
            GameServerService.InvokeGameServersHasChanged();
        }
    }
}
