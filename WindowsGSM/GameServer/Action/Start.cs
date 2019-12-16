using System.Threading.Tasks;
using System.Diagnostics;

namespace WindowsGSM.GameServer.Action
{
    class Start
    {
        private readonly Function.ServerTable server;
        private readonly string gslt = "";
        private readonly string additionalParam = "";
        public string Error = "";
        public string Notice = "";

        public Start(Function.ServerTable server, string gslt, string additionalParam)
        {
            this.server = server;
            this.gslt = gslt;
            this.additionalParam = additionalParam;
        }

        public async Task<Process> Run()
        {
            Process process = null;

            switch (server.Game)
            {
                case (GameServer.CSGO.FullName):
                    {
                        GameServer.CSGO gameServer = new GameServer.CSGO(server.ID);
                        gameServer.SetParameter(server.IP, server.Port, server.Defaultmap, server.Maxplayers, gslt, additionalParam);
                        process = await gameServer.Start();
                        Error = gameServer.Error;
                        Notice = gameServer.Notice;

                        break;
                    }
                case (GameServer.GMOD.FullName):
                    {
                        GameServer.GMOD gameServer = new GameServer.GMOD(server.ID);
                        gameServer.SetParameter(server.IP, server.Port, server.Defaultmap, server.Maxplayers, gslt, additionalParam);
                        process = await gameServer.Start();
                        Error = gameServer.Error;
                        Notice = gameServer.Notice;

                        break;
                    }
                case (GameServer.TF2.FullName):
                    {
                        GameServer.TF2 gameServer = new GameServer.TF2(server.ID);
                        gameServer.SetParameter(server.IP, server.Port, server.Defaultmap, server.Maxplayers, gslt, additionalParam);
                        process = await gameServer.Start();
                        Error = gameServer.Error;
                        Notice = gameServer.Notice;

                        break;
                    }
                case (GameServer.MCPE.FullName):
                    {
                        GameServer.MCPE gameServer = new GameServer.MCPE(server.ID);
                        process = await gameServer.Start();
                        Error = gameServer.Error;

                        break;
                    }
                case (GameServer.RUST.FullName):
                    {
                        GameServer.RUST gameServer = new GameServer.RUST(server.ID);
                        gameServer.SetParameter(server.IP, server.Port, server.Defaultmap, server.Maxplayers);
                        process = await gameServer.Start();
                        Error = gameServer.Error;
                        Notice = gameServer.Notice;

                        break;
                    }
                case (GameServer.CS.FullName):
                    {
                        GameServer.CS gameServer = new GameServer.CS(server.ID);
                        gameServer.SetParameter(server.IP, server.Port, server.Defaultmap, server.Maxplayers, gslt, additionalParam);
                        process = await gameServer.Start();
                        Error = gameServer.Error;
                        Notice = gameServer.Notice;

                        break;
                    }
                case (GameServer.CSCZ.FullName):
                    {
                        GameServer.CSCZ gameServer = new GameServer.CSCZ(server.ID);
                        gameServer.SetParameter(server.IP, server.Port, server.Defaultmap, server.Maxplayers, gslt, additionalParam);
                        process = await gameServer.Start();
                        Error = gameServer.Error;
                        Notice = gameServer.Notice;

                        break;
                    }
                case (GameServer.HL2DM.FullName):
                    {
                        GameServer.HL2DM gameServer = new GameServer.HL2DM(server.ID);
                        gameServer.SetParameter(server.IP, server.Port, server.Defaultmap, server.Maxplayers, gslt, additionalParam);
                        process = await gameServer.Start();
                        Error = gameServer.Error;
                        Notice = gameServer.Notice;

                        break;
                    }
                case (GameServer.L4D2.FullName):
                    {
                        GameServer.L4D2 gameServer = new GameServer.L4D2(server.ID);
                        gameServer.SetParameter(server.IP, server.Port, server.Defaultmap, server.Maxplayers, gslt, additionalParam);
                        process = await gameServer.Start();
                        Error = gameServer.Error;
                        Notice = gameServer.Notice;

                        break;
                    }
                case (GameServer.MC.FullName):
                    {
                        GameServer.MC gameServer = new GameServer.MC(server.ID);
                        gameServer.SetParameter(additionalParam);
                        process = await gameServer.Start();
                        Error = gameServer.Error;
                        Notice = gameServer.Notice;

                        break;
                    }
                default: break;
            }

            return process;
        }
    }
}
