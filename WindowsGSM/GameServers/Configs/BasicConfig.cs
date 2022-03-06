using WindowsGSM.Attributes;

namespace WindowsGSM.GameServers.Configs
{
    public class BasicConfig
    {
        [TextField(Label = "Name", HelperText = "Server Display Name", Required = true)]
        public string Name { get; set; } = string.Empty;

        [TextField(Label = "Directory", HelperText = "Server Files Directory", Required = true, FolderBrowser = true)]
        public string Directory { get; set; } = string.Empty;
    }
}
