using MudBlazor;
using WindowsGSM.Attributes;
using WindowsGSM.GameServers.Components;

namespace WindowsGSM.GameServers.Configs
{
    public class SteamCMDConfig
    {
        [TextField(Label = "SteamCMD Path", HelperText = "steamcmd.exe will be downloaded if steamcmd.exe does not exist.", Required = true, FolderBrowser = true)]
        public string Path { get; set; } = SteamCMD.FileName;

        [TextField(Label = "Game", HelperText = "Product Name")]
        public string Game { get; set; } = string.Empty;

        [TextField(Label = "App Id", HelperText = "Game App Id", Required = true)]
        public string AppId { get; set; } = string.Empty;

        [TextField(Label = "Server App Id", HelperText = "Server App Id", Required = true)]
        public string ServerAppId { get; set; } = string.Empty;

        [TextField(Label = "Steam Username", HelperText = "Steam account username", Required = true)]
        public string Username { get; set; } = string.Empty;

        [TextField(Label = "Steam Password", HelperText = "Steam account password", InputType = InputType.Password)]
        public string Password { get; set; } = string.Empty;

        [TextField(Label = ".maFile Path")]
        public string MaFilePath { get; set; } = string.Empty;

        [TextField(Label = "Create Parameter", HelperText = "steamcmd.exe parameter (Create)", Required = true)]
        public string CreateParameter { get; set; } = string.Empty;

        [TextField(Label = "Update Parameter", HelperText = "steamcmd.exe parameter (Update)", Required = true)]
        public string UpdateParameter { get; set; } = string.Empty;

        [RadioGroup(Text = "Console Type", HelperText = "steamcmd.exe console mode")]
        [Radio(Option = "Pseudo Console")]
        [Radio(Option = "Redirect Standard Input/Output")]
        [Radio(Option = "Windowed")]
        public string ConsoleMode { get; set; } = "Pseudo Console";
    }
}
