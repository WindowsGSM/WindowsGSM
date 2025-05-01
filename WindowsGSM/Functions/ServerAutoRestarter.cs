        public static async Task<bool> AutoRestartAsync(string serverId)
        {
            try
            {
                await Task.Run(async () =>
                {
                    var serverConfig = new ServerConfig(serverId);
                    if (serverConfig.AutoRestart)
                    {
                        await ServerRestart.RestartServerAsync(serverId);
                    }
                });
                return true;
            }
            catch
            {
                return false;
            }
        }