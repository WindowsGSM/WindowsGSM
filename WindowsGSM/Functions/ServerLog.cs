        public static async Task<bool> WriteLogAsync(string logPath, string message)
        {
            try
            {
                await File.AppendAllTextAsync(logPath, message + Environment.NewLine);
                return true;
            }
            catch
            {
                return false;
            }
        }