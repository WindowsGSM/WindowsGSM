using System;
using System.IO;
using System.Net;
using System.Net.Sockets;

namespace WindowsGSM.GameServer.Action
{
    class Import
    {
        private readonly Functions.ServerConfig serverConfig;
        public string Error = "";
        public string Notice = "";

        public Import(Functions.ServerConfig serverConfig)
        {
            this.serverConfig = serverConfig;
        }

        public void CreateServerConfigs(string serverGame, string serverName)
        {
            switch (serverGame)
            {
                case (GameServer.CSGO.FullName):
                    {
                        GameServer.CSGO gameServer = new GameServer.CSGO(serverConfig.ServerID);
                        serverConfig.CreateServerDirectory();
                        serverConfig.CreateWindowsGSMConfig(serverGame, serverName, GetIPAddress(), GetAvailablePort(gameServer.port), gameServer.defaultmap, gameServer.maxplayers, "", gameServer.additional);

                        break;
                    }
                case (GameServer.GMOD.FullName):
                    {
                        GameServer.GMOD gameServer = new GameServer.GMOD(serverConfig.ServerID);
                        serverConfig.CreateServerDirectory();
                        serverConfig.CreateWindowsGSMConfig(serverGame, serverName, GetIPAddress(), GetAvailablePort(gameServer.port), gameServer.defaultmap, gameServer.maxplayers, "", gameServer.additional);

                        break;
                    }
                case (GameServer.TF2.FullName):
                    {
                        GameServer.TF2 gameServer = new GameServer.TF2(serverConfig.ServerID);
                        serverConfig.CreateServerDirectory();
                        serverConfig.CreateWindowsGSMConfig(serverGame, serverName, GetIPAddress(), GetAvailablePort(gameServer.port), gameServer.defaultmap, gameServer.maxplayers, "", gameServer.additional);

                        break;
                    }
                case (GameServer.MCPE.FullName):
                    {
                        GameServer.MCPE gameServer = new GameServer.MCPE(serverConfig.ServerID);
                        serverConfig.CreateServerDirectory();
                        serverConfig.CreateWindowsGSMConfig(serverGame, serverName, GetIPAddress(), GetAvailablePort(gameServer.port), gameServer.defaultmap, gameServer.maxplayers, "", gameServer.additional);

                        break;
                    }
                case (GameServer.RUST.FullName):
                    {
                        GameServer.RUST gameServer = new GameServer.RUST(serverConfig.ServerID);
                        serverConfig.CreateServerDirectory();
                        serverConfig.CreateWindowsGSMConfig(serverGame, serverName, GetIPAddress(), GetAvailablePort(gameServer.port), gameServer.defaultmap, gameServer.maxplayers, "", gameServer.additional);

                        break;
                    }
                case (GameServer.CS.FullName):
                    {
                        GameServer.CS gameServer = new GameServer.CS(serverConfig.ServerID);
                        serverConfig.CreateServerDirectory();
                        serverConfig.CreateWindowsGSMConfig(serverGame, serverName, GetIPAddress(), GetAvailablePort(gameServer.port), gameServer.defaultmap, gameServer.maxplayers, "", gameServer.additional);

                        break;
                    }
                case (GameServer.CSCZ.FullName):
                    {
                        GameServer.CSCZ gameServer = new GameServer.CSCZ(serverConfig.ServerID);
                        serverConfig.CreateServerDirectory();
                        serverConfig.CreateWindowsGSMConfig(serverGame, serverName, GetIPAddress(), GetAvailablePort(gameServer.port), gameServer.defaultmap, gameServer.maxplayers, "", gameServer.additional);

                        break;
                    }
                case (GameServer.HL2DM.FullName):
                    {
                        GameServer.HL2DM gameServer = new GameServer.HL2DM(serverConfig.ServerID);
                        serverConfig.CreateServerDirectory();
                        serverConfig.CreateWindowsGSMConfig(serverGame, serverName, GetIPAddress(), GetAvailablePort(gameServer.port), gameServer.defaultmap, gameServer.maxplayers, "", gameServer.additional);

                        break;
                    }
                case (GameServer.L4D2.FullName):
                    {
                        GameServer.L4D2 gameServer = new GameServer.L4D2(serverConfig.ServerID);
                        serverConfig.CreateServerDirectory();
                        serverConfig.CreateWindowsGSMConfig(serverGame, serverName, GetIPAddress(), GetAvailablePort(gameServer.port), gameServer.defaultmap, gameServer.maxplayers, "", gameServer.additional);

                        break;
                    }
                case (GameServer.MC.FullName):
                    {
                        GameServer.MC gameServer = new GameServer.MC(serverConfig.ServerID);
                        serverConfig.CreateServerDirectory();
                        serverConfig.CreateWindowsGSMConfig(serverGame, serverName, GetIPAddress(), GetAvailablePort(gameServer.port), gameServer.defaultmap, gameServer.maxplayers, "", gameServer.additional);

                        break;

                    }
                case (GameServer.GTA5.FullName):
                    {
                        GameServer.GTA5 gameServer = new GameServer.GTA5(serverConfig.ServerID);
                        serverConfig.CreateServerDirectory();
                        serverConfig.CreateWindowsGSMConfig(serverGame, serverName, GetIPAddress(), GetAvailablePort(gameServer.port), gameServer.defaultmap, gameServer.maxplayers, "", gameServer.additional);

                        break;
                    }
                case (GameServer._7DTD.FullName):
                    {
                        GameServer._7DTD gameServer = new GameServer._7DTD(serverConfig.ServerID);
                        serverConfig.CreateServerDirectory();
                        serverConfig.CreateWindowsGSMConfig(serverGame, serverName, GetIPAddress(), GetAvailablePort(gameServer.port), gameServer.defaultmap, gameServer.maxplayers, "", gameServer.additional);

                        break;
                    }
            }
        }

        public bool CanImport(string serverGame, string serverDir)
        {
            string exeName = "";

            //Check is the path contain game server files
            switch (serverGame)
            {
                case (GameServer.CSGO.FullName):
                case (GameServer.GMOD.FullName):
                case (GameServer.TF2.FullName):
                case (GameServer.HL2DM.FullName):
                case (GameServer.L4D2.FullName):
                    exeName = "srcds.exe";
                    break;
                case (GameServer.MCPE.FullName):
                    {
                        string PHPPath = Path.Combine(serverDir, @"bin\php\php.exe");
                        string PMMPPath = Path.Combine(serverDir, "PocketMine-MP.phar");

                        if (File.Exists(PHPPath) && File.Exists(PMMPPath))
                        {
                            return true;
                        }
                        else
                        {
                            if (!File.Exists(PHPPath))
                            {
                                Error = "Invalid Path! Fail to find php.exe";
                            }
                            else if (!File.Exists(PMMPPath))
                            {
                                Error = "Invalid Path! Fail to find PocketMine-MP.phar";
                            }

                            return false;
                        }
                    }
                case (GameServer.RUST.FullName):
                    exeName = "RustDedicated.exe";
                    break;
                case (GameServer.CS.FullName):
                case (GameServer.CSCZ.FullName):
                    exeName = "hlds.exe";
                    break;
                case (GameServer.MC.FullName):
                    exeName = "server.jar";
                    break;
                case (GameServer.GTA5.FullName):
                    exeName = @"server\FXServer.exe";
                    break;
                case (GameServer._7DTD.FullName):
                    exeName = "7DaysToDieServer.exe";
                    break;
                default: break;
            }

            string exePath = Path.Combine(serverDir, exeName);
            if (File.Exists(exePath))
            {
                return true;
            }
            else
            {
                Error = $"Invalid Path! Fail to find {exeName}";
                return false;
            }
        }

        private static string GetIPAddress()
        {
            using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0))
            {
                socket.Connect("8.8.8.8", 65530);
                IPEndPoint endPoint = socket.LocalEndPoint as IPEndPoint;
                return endPoint.Address.ToString();
            }
        }

        private static string GetAvailablePort(string defaultport)
        {
            MainWindow WindowsGSM = (MainWindow)System.Windows.Application.Current.MainWindow;

            int[] portlist = new int[WindowsGSM.ServerGrid.Items.Count];

            for (int i = 0; i < WindowsGSM.ServerGrid.Items.Count; i++)
            {
                Functions.ServerTable row = WindowsGSM.ServerGrid.Items[i] as Functions.ServerTable;
                portlist[i] = Int32.Parse(string.IsNullOrWhiteSpace(row.Port) ? "0" : row.Port);
            }

            Array.Sort(portlist);

            int port = Int32.Parse(defaultport);
            for (int i = 0; i < WindowsGSM.ServerGrid.Items.Count; i++)
            {
                if (port == portlist[i])
                {
                    port++;
                }

                //SourceTV port 27020
                if (port == 27020)
                {
                    port++;
                }
            }

            return port.ToString();
        }
    }
}
