namespace WindowsGSM.GameServer
{
    class DOD : Engine.GoldSource
    {
        public const string FullName = "Day of Defeat Dedicated Server";
        public override string Defaultmap { get { return "dod_Anzio"; } }
        public override string Game { get { return "dod"; } }
        public override string AppId { get { return "30"; } }

        public DOD(Functions.ServerConfig serverData) : base(serverData)
        {
            base.serverData = serverData;
        }
    }
}
