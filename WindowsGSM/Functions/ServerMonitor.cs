        public static async Task MonitorServerAsync(string serverId)
        {
            try
            {
                await Task.Run(async () =>
                {
                    while (true)
                    {
                        int pid = await ServerCache.GetPIDAsync(serverId);
                        if (pid <= 0 || !Process.GetProcesses().Any(p => p.Id == pid))
                        {
                            await ServerRestart.RestartServerAsync(serverId);
                        }

                        await Task.Delay(5000);
                    }
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error monitoring server: {ex.Message}");
            }
        }