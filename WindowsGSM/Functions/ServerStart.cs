        public static async Task<bool> StartServerAsync(string serverId)
        {
            try
            {
                await Task.Run(async () =>
                {
                    var serverConfig = new ServerConfig(serverId);
                    var gameServer = GameServer.Data.Class.Get(serverConfig.ServerGame, serverConfig);
                    await gameServer.Start();
                });
                return true;
            }
            catch
            {
                return false;
            }
        }