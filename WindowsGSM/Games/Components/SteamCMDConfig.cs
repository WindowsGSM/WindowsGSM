using MudBlazor;
using WindowsGSM.Attributes;

namespace WindowsGSM.Games
{
    public class SteamCMDConfig : ISteamCMDConfig
    {
        [TextField(Label = "Product Name")]
        public string ProductName { get; set; } = string.Empty;

        [TextField(Label = "App Id", Required = true)]
        public string AppId { get; set; } = string.Empty;

        [TextField(Label = "Server App Id", Required = true)]
        public string ServerAppId { get; set; } = string.Empty;

        [TextField(Label = "Steam Username", Required = true)]
        public string Username { get; set; } = string.Empty;

        [TextField(Label = "Steam Password", InputType = InputType.Password)]
        public string Password { get; set; } = string.Empty;

        [TextField(Label = ".maFile Path")]
        public string MaFilePath { get; set; } = string.Empty;

        [TextField(Label = "Create Parameter", Required = true)]
        public string CreateParameter { get; set; } = string.Empty;

        [TextField(Label = "Update Parameter", Required = true)]
        public string UpdateParameter { get; set; } = string.Empty;
    }
}
