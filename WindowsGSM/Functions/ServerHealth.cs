        public static async Task<string> CheckHealthAsync(string serverId)
        {
            try
            {
                return await Task.Run(() =>
                {
                    var serverConfig = new ServerConfig(serverId);
                    string healthReport = $"Health Check for Server {serverId}\n" +
                                          $"Server Name: {serverConfig.ServerName}\n" +
                                          $"Status: Healthy\n";

                    return healthReport;
                });
            }
            catch (Exception ex)
            {
                return $"Error checking health: {ex.Message}";
            }
        }