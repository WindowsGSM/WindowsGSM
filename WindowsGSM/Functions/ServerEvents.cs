        public static async Task<bool> LogEventAsync(string serverId, string eventType, string eventDetails)
        {
            try
            {
                await Task.Run(() =>
                {
                    string logPath = ServerPath.GetLogs("events.log");
                    string logEntry = $"{DateTime.UtcNow}: Server {serverId} - Event: {eventType} - Details: {eventDetails}";
                    File.AppendAllText(logPath, logEntry + Environment.NewLine);
                });
                return true;
            }
            catch
            {
                return false;
            }
        }