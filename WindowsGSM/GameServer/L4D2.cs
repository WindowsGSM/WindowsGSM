namespace WindowsGSM.GameServer
{
    class L4D2 : Type.SRCDS
    {
        public const string FullName = "Left 4 Dead 2 Dedicated Server";
        public override string defaultmap { get { return "c1m1_hotel"; } }
        public override string Game { get { return "left4dead2"; } }
        public override string AppId { get { return "222860"; } }

        public L4D2(Functions.ServerConfig serverData) : base(serverData)
        {
            base.serverData = serverData;
        }
    }
}
