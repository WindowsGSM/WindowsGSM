        public static async Task<bool> UpdateServerFilesAsync(string serverId, string appId)
        {
            try
            {
                await Task.Run(() =>
                {
                    var steamCMD = new Installer.SteamCMD();
                    steamCMD.Update(serverId, appId);
                });
                return true;
            }
            catch
            {
                return false;
            }
        }