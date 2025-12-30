        public static async Task<bool> HandleCrashAsync(string serverId)
        {
            try
            {
                await Task.Run(async () =>
                {
                    var serverConfig = new ServerConfig(serverId);
                    if (serverConfig.CrashAlert && !string.IsNullOrWhiteSpace(serverConfig.DiscordWebhook))
                    {
                        string crashMessage = $"Server {serverId} has crashed. Attempting to restart...";
                        await DiscordWebhook.SendWebhookAsync(serverConfig.DiscordWebhook, crashMessage);
                    }

                    await ServerRestart.RestartServerAsync(serverId);
                });
                return true;
            }
            catch
            {
                return false;
            }
        }