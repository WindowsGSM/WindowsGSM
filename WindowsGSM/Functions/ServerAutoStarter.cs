        public static async Task<bool> AutoStartAsync(string serverId)
        {
            try
            {
                await Task.Run(async () =>
                {
                    var serverConfig = new ServerConfig(serverId);
                    if (serverConfig.AutoStart)
                    {
                        await ServerStart.StartServerAsync(serverId);
                    }
                });
                return true;
            }
            catch
            {
                return false;
            }
        }