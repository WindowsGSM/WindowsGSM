        public static async Task<bool> SendAlertAsync(string serverId, string message)
        {
            try
            {
                await Task.Run(async () =>
                {
                    var serverConfig = new ServerConfig(serverId);
                    if (!string.IsNullOrWhiteSpace(serverConfig.DiscordWebhook))
                    {
                        await DiscordWebhook.SendWebhookAsync(serverConfig.DiscordWebhook, message);
                    }
                });
                return true;
            }
            catch
            {
                return false;
            }
        }