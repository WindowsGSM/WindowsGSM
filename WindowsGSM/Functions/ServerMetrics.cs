        public static async Task<string> CollectMetricsAsync(string serverId)
        {
            try
            {
                return await Task.Run(() =>
                {
                    var serverConfig = new ServerConfig(serverId);
                    string metricsReport = $"Metrics Report for Server {serverId}\n" +
                                           $"CPU Usage: {GetCPUUsage()}%\n" +
                                           $"Memory Usage: {GetMemoryUsage()} MB\n";

                    return metricsReport;
                });
            }
            catch (Exception ex)
            {
                return $"Error collecting metrics: {ex.Message}";
            }
        }

        private static int GetCPUUsage()
        {
            // Simulated CPU usage value
            return new Random().Next(1, 100);
        }

        private static int GetMemoryUsage()
        {
            // Simulated memory usage value
            return new Random().Next(100, 10000);
        }