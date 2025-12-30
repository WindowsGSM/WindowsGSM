using System.Threading.Tasks;
using WindowsGSM.GameServer;

namespace WindowsGSM.Functions
{
    public static class ServerRestart
    {
        public static async Task<bool> RestartServerAsync(string serverId)
        {
            try
            {
                await Task.Run(async () =>
                {
                    var serverProcess = new ServerProcess();
                    int pid = await ServerCache.GetPIDAsync(serverId);
                    if (pid > 0)
                    {
                        await serverProcess.KillProcessAsync(pid);
                    }

                    var serverConfig = new ServerConfig(serverId);
                    var gameServer = GameServer.Data.Class.Get(serverConfig.ServerGame, serverConfig);
                    await gameServer.Start();
                });
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
