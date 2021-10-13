using System.Collections.Generic;
using WindowsGSM.Functions;

namespace WindowsGSM.GameServer.Data
{
    static class Class
    {
        public static dynamic Get(string serverGame, ServerConfig serverData = null, List<PluginMetadata> pluginList = null)
        {
            switch (serverGame)
            {
                case CSGO.FullName: return new CSGO(serverData);
                case GMOD.FullName: return new GMOD(serverData);
                case TF2.FullName: return new TF2(serverData);
                case MCPE.FullName: return new MCPE(serverData);
                case RUST.FullName: return new RUST(serverData);
                case CS.FullName: return new CS(serverData);
                case CSCZ.FullName: return new CSCZ(serverData);
                case HL2DM.FullName: return new HL2DM(serverData);
                case L4D2.FullName: return new L4D2(serverData);
                case MC.FullName: return new MC(serverData);
                case GTA5.FullName: return new GTA5(serverData);
                case SDTD.FullName: return new SDTD(serverData);
                case MORDHAU.FullName: return new MORDHAU(serverData);
                case SE.FullName: return new SE(serverData);
                case DAYZ.FullName: return new DAYZ(serverData);
                case MCBE.FullName: return new MCBE(serverData);
                case OLOW.FullName: return new OLOW(serverData);
                case CSS.FullName: return new CSS(serverData);
                case INS.FullName: return new INS(serverData);
                case NMRIH.FullName: return new NMRIH(serverData);
                case ARKSE.FullName: return new ARKSE(serverData);
                case ZPS.FullName: return new ZPS(serverData);
                case DODS.FullName: return new DODS(serverData);
                case SW.FullName: return new SW(serverData);
                case ROK.FullName: return new ROK(serverData);
                case HEAT.FullName: return new HEAT(serverData);
                case BW.FullName: return new BW(serverData);
                case ONSET.FullName: return new ONSET(serverData);
                case EGS.FullName: return new EGS(serverData);
                case UNT.FullName: return new UNT(serverData); 
                case AVORION.FullName: return new AVORION(serverData);
                case CE.FullName: return new CE(serverData);
                case INSS.FullName: return new INSS(serverData);
                case DOD.FullName: return new DOD(serverData);
                case DMC.FullName: return new DMC(serverData);
                case HLOF.FullName: return new HLOF(serverData);
                case RCC.FullName: return new RCC(serverData);
                case TFC.FullName: return new TFC(serverData);
                case TF.FullName: return new TF(serverData);
                case SQ.FullName: return new SQ(serverData);
                case BT.FullName: return new BT(serverData);
                case PS.FullName: return new PS(serverData);
                case ROR2.FullName: return new ROR2(serverData);
                case ECO.FullName: return new ECO(serverData);
                case VTS.FullName: return new VTS(serverData);
                case SDK2013.FullName: return new SDK2013(serverData);
                default: // Load Plugin
                {
                    if (pluginList == null) { return null; }

                    foreach (var plugin in pluginList)
                    {
                        if (plugin.IsLoaded && plugin.FullName == serverGame)
                        {
                            return PluginManagement.GetPluginClass(plugin, serverData);
                        }
                    }

                    return null;
                };
            }
        }
    }
}
