using OpenGSQ.Protocols;
using WindowsGSM.GameServers.Configs;
using WindowsGSM.Utilities;

namespace WindowsGSM.GameServers.Protocols
{
    public class GameSpy4Protocol : IProtocol
    {
        public async Task<IResponse> Query(IProtocolConfig protocolConfig)
        {
            GameSpy4 gameSpy4 = new(protocolConfig.Protocol.IPAddress, protocolConfig.Protocol.QueryPort);
            GameSpy4.Status status = await TaskEx.Run(() => gameSpy4.GetStatus());

            ProtocolResponse protocolResponse = new()
            {
                Name = status.Info.TryGetValue("hostname", out string? name) ? name : string.Empty,
                MapName = status.Info.TryGetValue("mapname", out string? mapName) ? mapName : status.Info.TryGetValue("map", out mapName) ? mapName : string.Empty,
                Player = status.Info.TryGetValue("numplayers", out string? player) ? int.Parse(player) : 0,
                MaxPlayer = status.Info.TryGetValue("maxplayers", out string? maxplayer) ? int.Parse(maxplayer) : 0,
                Bot = 0,
            };

            return protocolResponse;
        }
    }
}
