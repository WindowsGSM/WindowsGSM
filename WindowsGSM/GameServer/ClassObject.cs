using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WindowsGSM.GameServer
{
    class ClassObject
    {
        public static dynamic Get(string serverGame, Functions.ServerConfig serverData)
        {
            switch (serverGame)
            {
                case (CSGO.FullName): return new CSGO(serverData);
                case (GMOD.FullName): return new GMOD(serverData);
                case (TF2.FullName): return new TF2(serverData);
                case (MCPE.FullName): return new MCPE(serverData);
                case (RUST.FullName): return new RUST(serverData);
                case (CS.FullName): return new CS(serverData);
                case (CSCZ.FullName): return new CSCZ(serverData);
                case (HL2DM.FullName): return new HL2DM(serverData);
                case (L4D2.FullName): return new L4D2(serverData);
                case (MC.FullName): return new MC(serverData);
                case (GTA5.FullName): return new GTA5(serverData);
                case (_7DTD.FullName): return new _7DTD(serverData);
                case (MORDHAU.FullName): return new MORDHAU(serverData);
                default: return null;
            }
        }
    }
}
