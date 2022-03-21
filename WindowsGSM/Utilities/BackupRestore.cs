using System.IO.Compression;
using WindowsGSM.GameServers;
using WindowsGSM.Services;

namespace WindowsGSM.Utilities
{
    public static class BackupRestore
    {
        public static string GetBackupPath(IGameServer gameServer)
        {
            string backupPath = string.IsNullOrWhiteSpace(gameServer.Config.Backup.Directory) ? Path.Combine(GameServerService.BackupsPath, gameServer.Config.Guid.ToString()) : gameServer.Config.Backup.Directory;
            Directory.CreateDirectory(backupPath);

            return backupPath;
        }

        public static Task CreateBackupZip(IGameServer gameServer, string sourceDirectoryName)
        {
            string backupFile = Path.Combine(GetBackupPath(gameServer), $"{DateTime.Now:yyyyMMddTHHmmss}.zip");

            return TaskEx.Run(() => ZipFile.CreateFromDirectory(sourceDirectoryName, backupFile));
        }

        public static async Task CreateBackupZip(IGameServer gameServer, List<string> entries) // Should check all entries are valid before run, entries in full path
        {
            string backupFile = Path.Combine(GetBackupPath(gameServer), $"{DateTime.Now:yyyyMMddTHHmmss}.zip");
            using FileStream zipFile = File.Open(backupFile, FileMode.Create);
            using ZipArchive archive = new(zipFile, ZipArchiveMode.Create);

            foreach (string entry in entries)
            {
                if (File.GetAttributes(entry).HasFlag(FileAttributes.Directory))
                {
                    await TaskEx.Run(() => archive.CreateEntry(entry));
                }
                else
                {
                    await TaskEx.Run(() => archive.CreateEntryFromFile(entry, Path.GetFileName(entry)));
                }
            }
        }

        public static Task PerformFullBackup(IGameServer gameServer)
        {
            return CreateBackupZip(gameServer, gameServer.Config.Basic.Directory);
        }

        public static Task<List<string>> GetBackupFiles()
        {
            return Task.FromResult(new List<string>());
        }

        public static Task PerformRestore(IGameServer gameServer, string backupFileName)
        {
            string backupFile = Path.Combine(GetBackupPath(gameServer), backupFileName);

            return FileEx.ExtractZip(backupFile, gameServer.Config.Basic.Directory, true);
        }
    }
}
