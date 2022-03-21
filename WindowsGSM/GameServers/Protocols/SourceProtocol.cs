using OpenGSQ.Protocols;
using WindowsGSM.GameServers.Configs;
using WindowsGSM.Utilities;

namespace WindowsGSM.GameServers.Protocols
{
    public class SourceProtocol : IProtocol
    {
        public async Task<IResponse> Query(IProtocolConfig protocolConfig)
        {
            Source source = new(protocolConfig.Protocol.IPAddress, protocolConfig.Protocol.QueryPort);
            Source.IResponse response = await TaskEx.Run(() => source.GetInfo());

            ProtocolResponse protocolResponse = new()
            {
                Name = response.Name,
                MapName = response.Map,
                Player = response.Players,
                MaxPlayer = response.MaxPlayers,
                Bot = response.Bots,
            };

            return protocolResponse;
        }
    }
}
