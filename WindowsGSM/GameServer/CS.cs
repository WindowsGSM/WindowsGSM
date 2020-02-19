namespace WindowsGSM.GameServer
{
    class CS : Engine.GoldSource
    {
        public const string FullName = "Counter-Strike: 1.6 Dedicated Server";
        public override string Defaultmap { get { return "de_dust2"; } }
        public override string Game { get { return "cstrike"; } }
        public override string AppId { get { return "10"; } }

        public CS(Functions.ServerConfig serverData) : base(serverData)
        {
            base.serverData = serverData;
        }
    }
}
