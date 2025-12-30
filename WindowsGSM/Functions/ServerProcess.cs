using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace WindowsGSM.Functions
{
    public class ServerProcess
    {
        public async Task<bool> KillProcessAsync(int pid)
        {
            try
            {
                await Task.Run(() =>
                {
                    Process process = Process.GetProcessById(pid);
                    process.Kill();
                });
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to kill process {pid}", ex);
                return false;
            }
        }
    }
}
