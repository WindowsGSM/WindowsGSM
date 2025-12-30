        public static async Task<bool> ApplySecurityPatchAsync(string serverId, string patchDetails)
        {
            try
            {
                await Task.Run(() =>
                {
                    string logPath = ServerPath.GetLogs("security.log");
                    string logEntry = $"{DateTime.UtcNow}: Applied security patch to Server {serverId} - Details: {patchDetails}";
                    File.AppendAllText(logPath, logEntry + Environment.NewLine);
                });
                return true;
            }
            catch
            {
                return false;
            }
        }