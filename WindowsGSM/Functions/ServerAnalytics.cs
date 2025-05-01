        public static async Task<bool> LogAnalyticsAsync(string serverId, string eventType)
        {
            try
            {
                await Task.Run(() =>
                {
                    string logPath = ServerPath.GetLogs("analytics.log");
                    string logEntry = $"{DateTime.UtcNow}: Server {serverId} - Event: {eventType}";
                    File.AppendAllText(logPath, logEntry + Environment.NewLine);
                });
                return true;
            }
            catch
            {
                return false;
            }
        }