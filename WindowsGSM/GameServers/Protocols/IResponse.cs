namespace WindowsGSM.GameServers.Protocols
{
    public interface IResponse
    {
        public string Name { get; set; }

        public string MapName { get; set; }

        public int Player { get; set; }

        public int MaxPlayer { get; set; }

        public int Bot { get; set; }

        public DateTime DateTime { get; set; }
    }
}
