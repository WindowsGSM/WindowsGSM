        public static async Task<bool> UpdateStatusAsync(string serverId, string status)
        {
            try
            {
                await Task.Run(() =>
                {
                    MainWindow._serverMetadata[int.Parse(serverId)].Status = status;
                });
                return true;
            }
            catch
            {
                return false;
            }
        }