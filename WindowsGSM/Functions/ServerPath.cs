using System.IO;

namespace WindowsGSM.Functions
{
    public static class ServerPath
    {
        public static class FolderName
        {
            public static string Bin = "bin";
            public static string Backups = "backups";
            public static string Installer = "installer";
            public static string Logs = "logs";
            public static string Plugins = "plugins";
            public static string Servers = "servers";
            public static string Configs = "configs";
            public static string Cache = "cache";
            public static string Serverfiles = "serverfiles";
        }

        public static void CreateAndFixDirectories()
        {
            Directory.CreateDirectory(Get(FolderName.Backups));
            Directory.CreateDirectory(Get(FolderName.Bin));
            Directory.CreateDirectory(Get(FolderName.Configs));
            Directory.CreateDirectory(Get(FolderName.Logs));
            Directory.CreateDirectory(Get(FolderName.Plugins));
            Directory.CreateDirectory(Get(FolderName.Servers));
        }

        public static string Get(string path1 = "", string path2 = "", string path3 = "")
        {
            return Path.Combine(MainWindow.WGSM_PATH, path1, path2, path3);
        }

        public static string GetBackups(string serverId)
        {
            var backupConfig = new BackupConfig(serverId);
            if (Directory.Exists(backupConfig.BackupLocation))
            {
                return backupConfig.BackupLocation;
            }

            string defaultBackupPath = Path.Combine(MainWindow.WGSM_PATH, FolderName.Backups, serverId);
            Directory.CreateDirectory(defaultBackupPath);
            return defaultBackupPath;
        }

        public static string GetBin(string path1 = "", string path2 = "", string path3 = "")
        {
            return Path.Combine(MainWindow.WGSM_PATH, FolderName.Bin, path1, path2, path3);
        }

        public static string GetInstaller(string path = "") // Unused
        {
            return Path.Combine(MainWindow.WGSM_PATH, FolderName.Installer, path);
        }

        public static string GetLogs(string path1 = "", string path2 = "", string path3 = "")
        {
            return Path.Combine(MainWindow.WGSM_PATH, FolderName.Logs, path1, path2, path3);
        }

        public static string GetPlugins(string path1 = "", string path2 = "", string path3 = "")
        {
            return Path.Combine(MainWindow.WGSM_PATH, FolderName.Plugins, path1, path2, path3);
        }

        public static string GetServers(string serverId, string path1 = "", string path2 = "")
        {
            return Path.Combine(MainWindow.WGSM_PATH, FolderName.Servers, serverId, path1, path2);
        }

        public static string GetServersConfigs(string serverId, string path1 = "", string path2 = "")
        {
            return Path.Combine(GetServers(serverId), FolderName.Configs, path1, path2);
        }

        public static string GetServersCache(string serverId, string path1 = "", string path2 = "")
        {
            return Path.Combine(GetServers(serverId), FolderName.Cache, path1, path2);
        }

        public static string GetServersServerFiles(string serverId, string path1 = "", string path2 = "", string path3 = "")
        {
            return Path.Combine(GetServers(serverId), FolderName.Serverfiles, path1, path2, path3);
        }
    }
}
