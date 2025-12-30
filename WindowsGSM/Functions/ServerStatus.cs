using System.Threading.Tasks;

namespace WindowsGSM.Functions
{
    public static class ServerStatusHelper
    {
        public static async Task<bool> UpdateStatusAsync(string serverId, string status)
        {
            try
            {
                await Task.Run(() =>
                {
                    ServerManager.ServerMetadata[int.Parse(serverId)].ServerStatus = (ServerStatus)System.Enum.Parse(typeof(ServerStatus), status);
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
