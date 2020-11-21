namespace WindowsGSM.GameServer
{
    class DMC : Engine.GoldSource
    {
        public const string FullName = "Deathmatch Classic Dedicated Server";
        public override string Defaultmap { get { return "dcdm5"; } }
        public override string Game { get { return "dmc"; } }
        public override string AppId { get { return "40"; } }

        public DMC(Functions.ServerConfig serverData) : base(serverData)
        {
            base.serverData = serverData;
        }
    }
}
