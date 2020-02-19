namespace WindowsGSM.GameServer
{
    class HL2DM : Engine.Source
    {
        public const string FullName = "Half-Life 2: Deathmatch Dedicated Server";
        public override string Defaultmap { get { return "dm_runoff"; } }
        public override string Additional { get { return "+mp_teamplay 1"; } }
        public override string Game { get { return "hl2mp"; } }
        public override string AppId { get { return "232370"; } }

        public HL2DM(Functions.ServerConfig serverData) : base(serverData)
        {
            base.serverData = serverData;
        }
    }
}
