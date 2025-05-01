        public static async Task<bool> SendAutoStartAlertAsync(string serverId)
        {
            try
            {
                await Task.Run(async () =>
                {
                    var serverConfig = new ServerConfig(serverId);
                    if (serverConfig.AutoStartAlert && !string.IsNullOrWhiteSpace(serverConfig.DiscordWebhook))
                    {
                        string alertMessage = $"Server {serverId} has been automatically started.";
                        await DiscordWebhook.SendWebhookAsync(serverConfig.DiscordWebhook, alertMessage);
                    }
                });
                return true;
            }
            catch
            {
                return false;
            }
        }