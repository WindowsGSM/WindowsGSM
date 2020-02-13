using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WindowsGSM.GameServer
{
    class INS : Type.SRCDS
    {
        public const string FullName = "Insurgency Dedicated Server";
        public override string defaultmap { get { return "market skirmish"; } }
        public override string additional { get { return "-usercon"; } }
        public override string Game { get { return "insurgency"; } }
        public override string AppId { get { return "237410"; } }

        public INS(Functions.ServerConfig serverData) : base(serverData)
        {
            base.serverData = serverData;
        }
    }
}
