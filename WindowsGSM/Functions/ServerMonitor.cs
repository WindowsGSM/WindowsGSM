using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace WindowsGSM.Functions
{
    public static class ServerMonitor
    {
        public static async Task MonitorServerAsync(string serverId)
        {
            try
            {
                await Task.Run(async () =>
                {
                    while (true)
                    {
                        int pid = await ServerCache.GetPIDAsync(serverId);
                        if (pid > 0)
                        {
                            try
                            {
                                var process = Process.GetProcessById(pid);
                                if (process.HasExited)
                                {
                                    await ServerRestart.RestartServerAsync(serverId);
                                }
                            }
                            catch (ArgumentException)
                            {
                                await ServerRestart.RestartServerAsync(serverId);
                            }
                            catch
                            {
                                // Ignore other errors
                            }
                        }
                        else
                        {
                            await ServerRestart.RestartServerAsync(serverId);
                        }

                        await Task.Delay(5000);
                    }
                });
            }
            catch (Exception ex)
            {
                Logger.Error("Error monitoring server", ex);
            }
        }
    }
}
