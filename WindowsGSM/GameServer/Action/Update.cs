using System.Threading.Tasks;

namespace WindowsGSM.GameServer.Action
{
    class Update
    {
        private readonly Functions.ServerTable server;
        public string Error = "";
        public string Notice = "";

        public Update(Functions.ServerTable server)
        {
            this.server = server;
        }

        public async Task<bool> Run()
        {
            bool updated = false;

            switch (server.Game)
            {
                case (GameServer.CSGO.FullName):
                    {
                        GameServer.CSGO gameServer = new GameServer.CSGO(server.ID);
                        updated = await gameServer.Update();
                        Error = gameServer.Error;
                        break;
                    }
                case (GameServer.GMOD.FullName):
                    {
                        GameServer.GMOD gameServer = new GameServer.GMOD(server.ID);
                        updated = await gameServer.Update();
                        Error = gameServer.Error;
                        break;
                    }
                case (GameServer.TF2.FullName):
                    {
                        GameServer.TF2 gameServer = new GameServer.TF2(server.ID);
                        updated = await gameServer.Update();
                        Error = gameServer.Error;
                        break;
                    }
                case (GameServer.MCPE.FullName):
                    {
                        GameServer.MCPE gameServer = new GameServer.MCPE(server.ID);
                        updated = await gameServer.Update();
                        Error = gameServer.Error;
                        break;
                    }
                case (GameServer.RUST.FullName):
                    {
                        GameServer.RUST gameServer = new GameServer.RUST(server.ID);
                        updated = await gameServer.Update();
                        Error = gameServer.Error;
                        break;
                    }
                case (GameServer.CS.FullName):
                    {
                        GameServer.CS gameServer = new GameServer.CS(server.ID);
                        updated = await gameServer.Update();
                        Error = gameServer.Error;
                        break;
                    }
                case (GameServer.CSCZ.FullName):
                    {
                        GameServer.CSCZ gameServer = new GameServer.CSCZ(server.ID);
                        updated = await gameServer.Update();
                        Error = gameServer.Error;
                        break;
                    }
                case (GameServer.HL2DM.FullName):
                    {
                        GameServer.HL2DM gameServer = new GameServer.HL2DM(server.ID);
                        updated = await gameServer.Update();
                        Error = gameServer.Error;
                        break;
                    }
                case (GameServer.L4D2.FullName):
                    {
                        GameServer.L4D2 gameServer = new GameServer.L4D2(server.ID);
                        updated = await gameServer.Update();
                        Error = gameServer.Error;
                        break;
                    }
                case (GameServer.MC.FullName):
                    {
                        GameServer.MC gameServer = new GameServer.MC(server.ID);
                        updated = await gameServer.Update();
                        Error = gameServer.Error;
                        break;
                    }
                case (GameServer.GTA5.FullName):
                    {
                        GameServer.GTA5 gameServer = new GameServer.GTA5(server.ID);
                        updated = await gameServer.Update();
                        Error = gameServer.Error;
                        break;
                    }
                case (GameServer._7DTD.FullName):
                    {
                        GameServer._7DTD gameServer = new GameServer._7DTD(server.ID);
                        updated = await gameServer.Update();
                        Error = gameServer.Error;
                        break;
                    }
                default: break;
            }

            return updated;
        }
    }
}
