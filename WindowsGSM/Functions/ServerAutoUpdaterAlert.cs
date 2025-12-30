        public static async Task<bool> SendAutoUpdateAlertAsync(string serverId)
        {
            try
            {
                await Task.Run(async () =>
                {
                    var serverConfig = new ServerConfig(serverId);
                    if (serverConfig.AutoUpdateAlert && !string.IsNullOrWhiteSpace(serverConfig.DiscordWebhook))
                    {
                        string alertMessage = $"Server {serverId} has been automatically updated.";
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