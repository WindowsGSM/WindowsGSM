        public static async Task<bool> SendNotificationAsync(string serverId, string notificationMessage)
        {
            try
            {
                await Task.Run(async () =>
                {
                    var serverConfig = new ServerConfig(serverId);
                    if (!string.IsNullOrWhiteSpace(serverConfig.DiscordWebhook))
                    {
                        await DiscordWebhook.SendWebhookAsync(serverConfig.DiscordWebhook, notificationMessage);
                    }
                });
                return true;
            }
            catch
            {
                return false;
            }
        }