        public static async Task<bool> CreateBackupAsync(string sourcePath, string backupPath)
        {
            try
            {
                await Task.Run(() =>
                {
                    if (Directory.Exists(sourcePath))
                    {
                        if (!Directory.Exists(backupPath))
                        {
                            Directory.CreateDirectory(backupPath);
                        }

                        foreach (string dirPath in Directory.GetDirectories(sourcePath, "*", SearchOption.AllDirectories))
                        {
                            Directory.CreateDirectory(dirPath.Replace(sourcePath, backupPath));
                        }

                        foreach (string filePath in Directory.GetFiles(sourcePath, "*.*", SearchOption.AllDirectories))
                        {
                            File.Copy(filePath, filePath.Replace(sourcePath, backupPath), true);
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