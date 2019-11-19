using System.Diagnostics;

namespace WindowsGSM.GameServer.Action
{
    class Start
    {
        private readonly GameServerTable server;
        private readonly string gslt = "";
        private readonly string additionalParam = "";
        public string Error = "";
        public string Notice = "";

        public Start(GameServerTable server, string gslt, string additionalParam)
        {
            this.server = server;
            this.gslt = gslt;
            this.additionalParam = additionalParam;
        }

        public Process Run()
        {
            Process process = null;

            switch (server.Game)
            {
                case (GameServer.CSGO.FullName):
                    {
                        GameServer.CSGO gameServer = new GameServer.CSGO(server.ID);
                        gameServer.SetParameter(server.IP, server.Port, server.Defaultmap, server.Maxplayers, gslt, additionalParam);
                        (process, Error, Notice) = gameServer.Start();

                        break;
                    }
                case (GameServer.GMOD.FullName):
                    {
                        GameServer.GMOD gameServer = new GameServer.GMOD(server.ID);
                        gameServer.SetParameter(server.IP, server.Port, server.Defaultmap, server.Maxplayers, gslt, additionalParam);
                        (process, Error, Notice) = gameServer.Start();

                        break;
                    }
                case (GameServer.TF2.FullName):
                    {
                        GameServer.TF2 gameServer = new GameServer.TF2(server.ID);
                        gameServer.SetParameter(server.IP, server.Port, server.Defaultmap, server.Maxplayers, gslt, additionalParam);
                        (process, Error, Notice) = gameServer.Start();

                        break;
                    }
                case (GameServer.MCPE.FullName):
                    {
                        GameServer.MCPE gameServer = new GameServer.MCPE(server.ID);
                        (process, Error, Notice) = gameServer.Start();

                        break;
                    }
                case (GameServer.RUST.FullName):
                    {
                        GameServer.RUST gameServer = new GameServer.RUST(server.ID);
                        gameServer.SetParameter(server.IP, server.Port, server.Defaultmap, server.Maxplayers);
                        (process, Error, Notice) = gameServer.Start();

                        break;
                    }
                case (GameServer.CS.FullName):
                    {
                        GameServer.CS gameServer = new GameServer.CS(server.ID);
                        gameServer.SetParameter(server.IP, server.Port, server.Defaultmap, server.Maxplayers, gslt, additionalParam);
                        (process, Error, Notice) = gameServer.Start();

                        break;
                    }
                case (GameServer.CSCZ.FullName):
                    {
                        GameServer.CSCZ gameServer = new GameServer.CSCZ(server.ID);
                        gameServer.SetParameter(server.IP, server.Port, server.Defaultmap, server.Maxplayers, gslt, additionalParam);
                        (process, Error, Notice) = gameServer.Start();

                        break;
                    }
                default: break;
            }

            return process;
        }
    }
}
