        public static async Task<bool> AutoUpdateAsync(string serverId)
        {
            try
            {
                await Task.Run(async () =>
                {
                    var serverConfig = new ServerConfig(serverId);
                    if (serverConfig.AutoUpdate)
                    {
                        await ServerUpdate.UpdateServerFilesAsync(serverId, serverConfig.ServerGame);
                    }
                });
                return true;
            }
            catch
            {
                return false;
            }
        }