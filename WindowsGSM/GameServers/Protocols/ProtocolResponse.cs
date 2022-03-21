namespace WindowsGSM.GameServers.Protocols
{
    public class ProtocolResponse : IResponse
    {
        public string Name { get; set; } = string.Empty;

        public string MapName { get; set; } = string.Empty;

        public int Player { get; set; }

        public int MaxPlayer { get; set; }

        public int Bot { get; set; }

        public DateTime DateTime { get; set; } = DateTime.Now;
    }
}
