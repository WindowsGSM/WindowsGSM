using System.IO;

namespace WindowsGSM.Functions
{
    static class ServerPath
    {
        public static string Get(string path = "")
        {
            return Path.Combine(MainWindow.WGSM_PATH, path);
        }

        public static string GetBackups(string serverId)
        {
            var backupConfig = new BackupConfig(serverId);
            if (Directory.Exists(backupConfig.BackupLocation))
            {
                return backupConfig.BackupLocation;
            }

            string defaultBackupPath = Path.Combine(MainWindow.WGSM_PATH, "Backups", serverId);
            Directory.CreateDirectory(defaultBackupPath);
            return defaultBackupPath;
        }

        public static string GetInstaller(string path = "")
        {
            return Path.Combine(MainWindow.WGSM_PATH, "Installer", path);
        }

        public static string GetLogs()
        {
            return Path.Combine(MainWindow.WGSM_PATH, "Logs");
        }

        public static string GetServers(string serverid, string path1 = "", string path2 = "")
        {
            return Path.Combine(MainWindow.WGSM_PATH, "servers", serverid, path1, path2);
        }

        public static string GetServersConfigs(string serverid, string path1 = "", string path2 = "")
        {
            return Path.Combine(GetServers(serverid), "configs", path1, path2);
        }

        public static string GetServersCache(string serverid, string path1 = "", string path2 = "")
        {
            return Path.Combine(GetServers(serverid), "cache", path1, path2);
        }

        public static string GetServersServerFiles(string serverid, string path1 = "", string path2 = "")
        {
            return Path.Combine(GetServers(serverid), "serverfiles", path1, path2);
        }
    }
}
