        public static async Task<string> RunDiagnosticsAsync(string serverId)
        {
            try
            {
                return await Task.Run(() =>
                {
                    var serverConfig = new ServerConfig(serverId);
                    string diagnosticsReport = $"Diagnostics Report for Server {serverId}\n" +
                                               $"Server Name: {serverConfig.ServerName}\n" +
                                               $"Server IP: {serverConfig.ServerIP}\n" +
                                               $"Server Port: {serverConfig.ServerPort}\n";

                    return diagnosticsReport;
                });
            }
            catch (Exception ex)
            {
                return $"Error running diagnostics: {ex.Message}";
            }
        }