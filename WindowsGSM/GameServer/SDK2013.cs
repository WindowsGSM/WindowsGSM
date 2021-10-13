namespace WindowsGSM.GameServer
{
    class SDK2013 : Engine.Source
    {
        public const string FullName = "Source SDK Base 2013 Dedicated Server";
        public override string Defaultmap { get { return "<set me>"; } }
        public override string Game { get { return ""; } }
        public override string AppId { get { return "244310"; } }
        public override string Additional { get { return "-game <set me>"; } }

        public SDK2013(Functions.ServerConfig serverData) : base(serverData)
        {
            base.serverData = serverData;
        }
    }
}