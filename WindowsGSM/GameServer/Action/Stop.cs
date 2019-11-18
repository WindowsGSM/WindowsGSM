using System.Threading.Tasks;
using System.Diagnostics;

namespace WindowsGSM.GameServer.Action
{
    class Stop
    {
        private readonly GameServerTable server;
        public string Error = "";
        public string Notice = "";

        public Stop(GameServerTable server)
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
