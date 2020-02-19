namespace WindowsGSM.GameServer
{
    class DODS : Engine.Source
    {
        public const string FullName = "Day of Defeat: Source Dedicated Server";
        public override string Defaultmap { get { return "dod_anzio"; } }
        public override string Game { get { return "dod"; } }
        public override string AppId { get { return "232290"; } }

        public DODS(Functions.ServerConfig serverData) : base(serverData)
        {
            base.serverData = serverData;
        }
    }
}