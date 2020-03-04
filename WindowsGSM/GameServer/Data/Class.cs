namespace WindowsGSM.GameServer.Data
{
    static class Class
    {
        public static dynamic Get(string serverGame, Functions.ServerConfig serverData)
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
                default: return null;
            }
        }
    }
}
