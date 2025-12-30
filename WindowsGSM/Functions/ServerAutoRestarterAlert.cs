        public static async Task<bool> SendAutoRestartAlertAsync(string serverId)
        {
            try
            {
                await Task.Run(async () =>
                {
                    var serverConfig = new ServerConfig(serverId);
                    if (serverConfig.AutoRestartAlert && !string.IsNullOrWhiteSpace(serverConfig.DiscordWebhook))
                    {
                        string alertMessage = $"Server {serverId} has been automatically restarted.";
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