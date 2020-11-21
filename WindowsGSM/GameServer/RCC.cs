namespace WindowsGSM.GameServer
{
    class RCC : Engine.GoldSource
    {
        public const string FullName = "Ricochet Dedicated Server";
        public override string Defaultmap { get { return "rc_arena"; } }
        public override string Game { get { return "ricochet"; } }
        public override string AppId { get { return "60"; } }

        public RCC(Functions.ServerConfig serverData) : base(serverData)
        {
            base.serverData = serverData;
        }
    }
}
