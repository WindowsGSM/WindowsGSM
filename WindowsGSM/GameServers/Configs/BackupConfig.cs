using WindowsGSM.Attributes;

namespace WindowsGSM.GameServers.Configs
{
    public class BackupConfig
    {
        [TextField(Label = "Backup Files Directory", HelperText = "Backup files directory, if it is empty, default backup path will be used", Placeholder = "Backup files directory override")]
        public string Directory { get; set; } = string.Empty;

        [TextField(Label = "Backup Entries", HelperText = "Backup directory and file relative paths")]
        public List<string> Entries { get; set; } = new();
    }
}
