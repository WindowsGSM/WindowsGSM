using System.Collections.Generic;
using System.Threading.Tasks;

namespace WindowsGSM.Functions
{
    public interface IServerManager
    {
        int GetServerCount();
        int GetStartedServerCount();
        int GetActivePlayers();
        List<(string id, string state, string server)> GetServerList();
        bool IsServerExist(string id);
        ServerStatus GetServerStatus(string id);
        string GetServerName(string id);
        Task<bool> StartServerById(string id, string adminId, string adminName);
        Task<bool> StopServerById(string id, string adminId, string adminName);
        Task<bool> RestartServerById(string id, string adminId, string adminName);
        Task<bool> SendCommandById(string id, string command, string adminId, string adminName);
        Task<bool> BackupServerById(string id, string adminId, string adminName);
        Task<bool> UpdateServerById(string id, string adminId, string adminName);
    }
}
