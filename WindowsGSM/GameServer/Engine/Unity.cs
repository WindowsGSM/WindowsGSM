using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WindowsGSM.GameServer.Engine
{
    public class Unity
    {
        public Functions.ServerConfig serverData;

        public Unity(Functions.ServerConfig serverData)
        {
            this.serverData = serverData;
        }
    }
}
