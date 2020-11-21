namespace WindowsGSM.GameServer
{
    class HLOF : Engine.GoldSource
    {
        public const string FullName = "Half-Life: Opposing Force Dedicated Server";
        public override string Defaultmap { get { return "op4_bootcamp"; } }
        public override string Game { get { return "gearbox"; } }
        public override string AppId { get { return "50"; } }

        public HLOF(Functions.ServerConfig serverData) : base(serverData)
        {
            base.serverData = serverData;
        }
    }
}
