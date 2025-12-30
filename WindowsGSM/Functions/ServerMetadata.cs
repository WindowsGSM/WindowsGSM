using System;
using System.Diagnostics;
using WindowsGSM.Functions;

namespace WindowsGSM.Functions
{
    public enum ServerStatus
    {
        Started = 0,
        Starting = 1,
        Stopped = 2,
        Stopping = 3,
        Restarted = 4,
        Restarting = 5,
        Updated = 6,
        Updating = 7,
        Backuped = 8,
        Backuping = 9,
        Restored = 10,
        Restoring = 11,
        Deleting = 12
    }

    public class ServerMetadata
    {
        public ServerStatus ServerStatus = ServerStatus.Stopped;
        public Process Process;
        public IntPtr MainWindow;
        public ServerConsole ServerConsole;

        // Basic Game Server Settings
        public bool AutoRestart;
        public bool AutoStart;
        public bool AutoUpdate;
        public bool UpdateOnStart;
        public bool BackupOnStart;

        // Discord Alert Settings
        public bool DiscordAlert;
        public string DiscordMessage;
        public string DiscordWebhook;
        public bool AutoRestartAlert;
        public bool AutoStartAlert;
        public bool AutoUpdateAlert;
        public bool RestartCrontabAlert;
        public bool CrashAlert;

        // Restart Crontab Settings
        public bool RestartCrontab;
        public string CrontabFormat;

        // Game Server Start Priority and Affinity
        public string CPUPriority;
        public string CPUAffinity;

        public bool EmbedConsole;
        public bool AutoScroll;
    }
}
