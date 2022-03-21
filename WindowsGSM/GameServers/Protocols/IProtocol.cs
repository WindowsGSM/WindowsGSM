using WindowsGSM.GameServers.Configs;

namespace WindowsGSM.GameServers.Protocols
{
    public interface IProtocol
    {
        public Task<IResponse> Query(IProtocolConfig protocolConfig);
    }
}
