namespace WindowsGSM.GameServer
{
    class NMRIH : Type.SRCDS
    {
        public const string FullName = "No More Room in Hell Dedicated Server";
        public override string defaultmap { get { return "nmo_broadway"; } }
        public override string Game { get { return "nmrih"; } }
        public override string AppId { get { return "317670"; } }

        public NMRIH(Functions.ServerConfig serverData) : base(serverData)
        {
            base.serverData = serverData;
        }
    }
}
