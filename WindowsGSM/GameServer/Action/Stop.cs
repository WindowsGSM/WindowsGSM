using System.Threading.Tasks;
using System.Diagnostics;

namespace WindowsGSM.GameServer.Action
{
    class Stop
    {
        private readonly Function.ServerTable server;
        public string Error = "";
        public string Notice = "";

        public Stop(Function.ServerTable server)
        {
            this.server = server;
        }

        public async Task<bool> Run(Process process)
        {
            switch (server.Game)
            {
                case (GameServer.CSGO.FullName):
                    {
                        GameServer.CSGO gameServer = new GameServer.CSGO(server.ID);
                        await gameServer.Stop(process);

                        break;
                    }
                case (GameServer.GMOD.FullName):
                    {
                        GameServer.GMOD gameServer = new GameServer.GMOD(server.ID);
                        await gameServer.Stop(process);

                        break;
                    }
                case (GameServer.TF2.FullName):
                    {
                        GameServer.TF2 gameServer = new GameServer.TF2(server.ID);
                        await gameServer.Stop(process);

                        break;
                    }
                case (GameServer.MCPE.FullName):
                    {
                        GameServer.MCPE gameServer = new GameServer.MCPE(server.ID);
                        await gameServer.Stop(process);

                        break;
                    }
                case (GameServer.RUST.FullName):
                    {
                        GameServer.RUST gameServer = new GameServer.RUST(server.ID);
                        await gameServer.Stop(process);

                        break;
                    }
                case (GameServer.CS.FullName):
                    {
                        GameServer.CS gameServer = new GameServer.CS(server.ID);
                        await gameServer.Stop(process);

                        break;
                    }
                case (GameServer.CSCZ.FullName):
                    {
                        GameServer.CSCZ gameServer = new GameServer.CSCZ(server.ID);
                        await gameServer.Stop(process);

                        break;
                    }
                case (GameServer.HL2DM.FullName):
                    {
                        GameServer.HL2DM gameServer = new GameServer.HL2DM(server.ID);
                        await gameServer.Stop(process);

                        break;
                    }
                case (GameServer.L4D2.FullName):
                    {
                        GameServer.L4D2 gameServer = new GameServer.L4D2(server.ID);
                        await gameServer.Stop(process);

                        break;
                    }
                default: return true;
            }

            if (!process.HasExited)
            {
                process.Kill();
            }

            return true;
        }
    }
}
