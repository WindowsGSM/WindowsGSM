namespace WindowsGSM.GameServers.Configs
{
    public interface IStartConfig
    {
        public string StartPath { get; set; }

        public string StartParameter { get; set; }

        public string ConsoleMode { get; set; }
    }
}
