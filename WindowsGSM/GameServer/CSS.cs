using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WindowsGSM.GameServer
{
    class CSS : Type.SRCDS
    {
        public const string FullName = "Counter-Strike: Source Dedicated Server";
        public override string defaultmap { get { return "de_dust2"; } }
        public override string additional { get { return "-tickrate 64"; } }
        public override string Game { get { return "cstrike"; } }
        public override string AppId { get { return "232330"; } }

        public CSS(Functions.ServerConfig serverData) : base(serverData)
        {
            base.serverData = serverData;
        }
    }
}
