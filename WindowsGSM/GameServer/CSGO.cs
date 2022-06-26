namespace WindowsGSM.GameServer
{
    class CSGO : Engine.Source
    {
        public const string FullName = "Counter-Strike: Global Offensive Dedicated Server";
        public override string Defaultmap { get { return "de_dust2"; } }
        public override string Additional { get { return "-tickrate 64 -usercon +game_type 0 +game_mode 0 +mapgroup mg_active -nocrashdialog"; } }
        public override string Game { get { return "csgo"; } }
        public override string AppId { get { return "740"; } }

        public CSGO(Functions.ServerConfig serverData) : base(serverData)
        {
            base.serverData = serverData;
        }
    }
}
