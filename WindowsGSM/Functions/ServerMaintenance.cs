        public static async Task<bool> PerformMaintenanceAsync(string serverId)
        {
            try
            {
                await Task.Run(async () =>
                {
                    var serverConfig = new ServerConfig(serverId);
                    if (serverConfig.AutoUpdate)
                    {
                        await ServerUpdate.UpdateServerFilesAsync(serverId, serverConfig.ServerGame);
                    }

                    if (serverConfig.BackupOnStart)
                    {
                        string backupPath = ServerPath.GetBackups(serverId);
                        string serverFilesPath = ServerPath.GetServersServerFiles(serverId);
                        await ServerBackup.CreateBackupAsync(serverFilesPath, backupPath);
                    }
                });
                return true;
            }
            catch
            {
                return false;
            }
        }