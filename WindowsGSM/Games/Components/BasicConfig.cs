using WindowsGSM.Attributes;

namespace WindowsGSM.Games
{
    public class BasicConfig
    {
        [TextField(Label = "Name", HelperText = "Server Display Name", Required = true)]
        public string Name { get; set; } = string.Empty;

        [TextField(Label = "Directory", Required = true, FolderBrowser = true)]
        public string Directory { get; set; } = string.Empty;
    }
}
