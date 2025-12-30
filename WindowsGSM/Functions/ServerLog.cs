using System;
using System.IO;
using System.Threading.Tasks;

namespace WindowsGSM.Functions
{
    public static class ServerLog
    {
        public static async Task<bool> WriteLogAsync(string logPath, string message)
        {
            try
            {
                await File.AppendAllTextAsync(logPath, message + Environment.NewLine);
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to write log", ex);
                return false;
            }
        }
    }
}
