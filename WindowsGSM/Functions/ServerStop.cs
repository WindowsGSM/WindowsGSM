        public static async Task<bool> StopServerAsync(string serverId)
        {
            try
            {
                await Task.Run(async () =>
                {
                    int pid = await ServerCache.GetPIDAsync(serverId);
                    if (pid > 0)
                    {
                        var serverProcess = new ServerProcess();
                        await serverProcess.KillProcessAsync(pid);
                    }
                });
                return true;
            }
            catch
            {
                return false;
            }
        }