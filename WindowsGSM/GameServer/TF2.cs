namespace WindowsGSM.GameServer
{
    class TF2 : Engine.Source
    {
        public const string FullName = "Team Fortress 2 Dedicated Server";
        public override string Defaultmap { get { return "cp_badlands"; } }
        public override string Game { get { return "tf"; } }
        public override string AppId { get { return "232250"; } }

        public TF2(Functions.ServerConfig serverData) : base(serverData)
        {
            base.serverData = serverData;
        }
    }
}
