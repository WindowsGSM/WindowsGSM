namespace WindowsGSM.GameServer
{
    class GMOD : Valve.SRCDS
    {
        public const string FullName = "Garry's Mod Dedicated Server";
        public override string defaultmap { get { return "gm_construct"; } }
        public override string additional { get { return "-tickrate 66 +gamemode sandbox +host_workshop_collection"; } }
        public override string game { get { return "garrysmod"; } }
        public override string appId { get { return "4020"; } }

        public GMOD(Functions.ServerConfig serverData) : base(serverData)
        {
            base.serverData = serverData;
        }
    }
}
