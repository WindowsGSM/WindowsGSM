namespace WindowsGSM.Games
{
    public interface ISteamCMDConfig
    {
        public string ProductName { get; set; }

        public string AppId { get; set; }

        public string ServerAppId { get; set; }

        public string Username { get; set; }

        public string Password { get; set; }

        public string MaFilePath { get; set; }

        public string CreateParameter { get; set; }

        public string UpdateParameter { get; set; }
    }
}
