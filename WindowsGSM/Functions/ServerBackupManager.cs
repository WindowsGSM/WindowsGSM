        public static async Task<bool> ManageBackupsAsync(string serverId)
        {
            try
            {
                await Task.Run(async () =>
                {
                    var backupConfig = new BackupConfig(serverId);
                    string backupPath = backupConfig.BackupLocation;

                    if (Directory.Exists(backupPath))
                    {
                        var backupDirectories = Directory.GetDirectories(backupPath);
                        if (backupDirectories.Length > backupConfig.MaximumBackups)
                        {
                            var oldestBackup = backupDirectories.OrderBy(d => Directory.GetCreationTime(d)).First();
                            await backupConfig.DeleteBackupAsync(oldestBackup);
                        }
                    }
                });
                return true;
            }
            catch
            {
                return false;
            }
        }