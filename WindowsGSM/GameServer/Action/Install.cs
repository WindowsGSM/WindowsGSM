using System;
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;

namespace WindowsGSM.GameServer.Action
{
    class Install
    {
        private readonly Functions.ServerConfig serverConfig;
        public string Error = "";
        public string Notice = "";

        public Install(Functions.ServerConfig serverConfig)
        {
            this.serverConfig = serverConfig;
        }

        public async Task<Process> Run(string serverGame)
        {
            switch (serverGame)
            {
                case (GameServer.CSGO.FullName):
                    {
                        GameServer.CSGO gameServer = new GameServer.CSGO(serverConfig.ServerID);
                        return await gameServer.Install();
                    }
                case (GameServer.GMOD.FullName):
                    {
                        GameServer.GMOD gameServer = new GameServer.GMOD(serverConfig.ServerID);
                        return await gameServer.Install();
                    }
                case (GameServer.TF2.FullName):
                    {
                        GameServer.TF2 gameServer = new GameServer.TF2(serverConfig.ServerID);
                        return await gameServer.Install();
                    }
                case (GameServer.MCPE.FullName):
                    {
                        GameServer.MCPE gameServer = new GameServer.MCPE(serverConfig.ServerID);
                        await gameServer.Install();

                        return null;
                    }
                case (GameServer.RUST.FullName):
                    {
                        GameServer.RUST gameServer = new GameServer.RUST(serverConfig.ServerID);
                        return await gameServer.Install();
                    }
                case (GameServer.CS.FullName):
                    {
                        GameServer.CS gameServer = new GameServer.CS(serverConfig.ServerID);
                        return await gameServer.Install();
                    }
                case (GameServer.CSCZ.FullName):
                    {
                        GameServer.CSCZ gameServer = new GameServer.CSCZ(serverConfig.ServerID);
                        return await gameServer.Install();
                    }
                case (GameServer.HL2DM.FullName):
                    {
                        GameServer.HL2DM gameServer = new GameServer.HL2DM(serverConfig.ServerID);
                        return await gameServer.Install();
                    }
                case (GameServer.L4D2.FullName):
                    {
                        GameServer.L4D2 gameServer = new GameServer.L4D2(serverConfig.ServerID);
                        return await gameServer.Install();
                    }
                case (GameServer.MC.FullName):
                    {
                        GameServer.MC gameServer = new GameServer.MC(serverConfig.ServerID);
                        await gameServer.Install();

                        return null;
                    }
                default: break;
            }

            return null;
        }

        public async Task<bool> IsSuccess(Process process, string serverGame, string serverName)
        {
            if (process != null)
            {
                await Task.Run(() => process.WaitForExit());
            }

            switch (serverGame)
            {
                case (GameServer.CSGO.FullName):
                case (GameServer.GMOD.FullName):
                case (GameServer.TF2.FullName):
                case (GameServer.HL2DM.FullName):
                case (GameServer.L4D2.FullName):
                    {
                        string srcdsPath = Functions.Path.GetServerFiles(serverConfig.ServerID, "srcds.exe");
                        if (File.Exists(srcdsPath))
                        {
                            CreateServerConfigs(serverGame, serverName);

                            return true;
                        }

                        return false;
                    }
                case (GameServer.MCPE.FullName):
                    {
                        string PHPPath = Functions.Path.GetServerFiles(serverConfig.ServerID, @"bin\php\php.exe");
                        string PMMPPath = Functions.Path.GetServerFiles(serverConfig.ServerID, "PocketMine-MP.phar");
                        if (File.Exists(PHPPath) && File.Exists(PMMPPath))
                        {
                            CreateServerConfigs(serverGame, serverName);

                            return true;
                        }

                        return false;
                    }
                case (GameServer.RUST.FullName):
                    {
                        string rustPath = Functions.Path.GetServerFiles(serverConfig.ServerID, "RustDedicated.exe");
                        if (File.Exists(rustPath))
                        {
                            CreateServerConfigs(serverGame, serverName);

                            return true;
                        }

                        return false;
                    }
                case (GameServer.CS.FullName):
                    {
                        string hldsPath = Functions.Path.GetServerFiles(serverConfig.ServerID, "hlds.exe");
                        if (File.Exists(hldsPath))
                        {
                            string serverConfigPath = Functions.Path.GetServerFiles(serverConfig.ServerID, "cstrike");
                            if (!Directory.Exists(serverConfigPath))
                            {
                                return false;
                            }

                            CreateServerConfigs(serverGame, serverName);

                            return true;
                        }

                        return false;
                    }
                case (GameServer.CSCZ.FullName):
                    {
                        string hldsPath = Functions.Path.GetServerFiles(serverConfig.ServerID, "hlds.exe");
                        if (File.Exists(hldsPath))
                        {
                            string serverConfigPath = Functions.Path.GetServerFiles(serverConfig.ServerID, "czero");
                            if (!Directory.Exists(serverConfigPath))
                            {
                                return false;
                            }

                            CreateServerConfigs(serverGame, serverName);

                            return true;
                        }

                        return false;
                    }
                case (GameServer.MC.FullName):
                    {
                        string serverPath = Functions.Path.GetServerFiles(serverConfig.ServerID, "server.jar");
                        if (File.Exists(serverPath))
                        {
                            CreateServerConfigs(serverGame, serverName);

                            return true;
                        }

                        return false;
                    }
            }

            return false;
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
                        gameServer.CreateServerCFG(serverName, GetRCONPassword());

                        break;
                    }
                case (GameServer.GMOD.FullName):
                    {
                        GameServer.GMOD gameServer = new GameServer.GMOD(serverConfig.ServerID);
                        serverConfig.CreateServerDirectory();
                        serverConfig.CreateWindowsGSMConfig(serverGame, serverName, GetIPAddress(), GetAvailablePort(gameServer.port), gameServer.defaultmap, gameServer.maxplayers, "", gameServer.additional);
                        gameServer.CreateServerCFG(serverName, GetRCONPassword());

                        break;
                    }
                case (GameServer.TF2.FullName):
                    {
                        GameServer.TF2 gameServer = new GameServer.TF2(serverConfig.ServerID);
                        serverConfig.CreateServerDirectory();
                        serverConfig.CreateWindowsGSMConfig(serverGame, serverName, GetIPAddress(), GetAvailablePort(gameServer.port), gameServer.defaultmap, gameServer.maxplayers, "", gameServer.additional);
                        gameServer.CreateServerCFG(serverName, GetRCONPassword());

                        break;
                    }
                case (GameServer.MCPE.FullName):
                    {
                        GameServer.MCPE gameServer = new GameServer.MCPE(serverConfig.ServerID);
                        serverConfig.CreateServerDirectory();

                        string port = GetAvailablePort(gameServer.port);
                        serverConfig.CreateWindowsGSMConfig(serverGame, serverName, GetIPAddress(), port, gameServer.defaultmap, gameServer.maxplayers, "", gameServer.additional);
                        gameServer.CreateServerCFG(serverName, port, GetRCONPassword());

                        break;
                    }
                case (GameServer.RUST.FullName):
                    {
                        GameServer.RUST gameServer = new GameServer.RUST(serverConfig.ServerID);
                        serverConfig.CreateServerDirectory();

                        string port = GetAvailablePort(gameServer.port);
                        serverConfig.CreateWindowsGSMConfig(serverGame, serverName, GetIPAddress(), port, gameServer.defaultmap, gameServer.maxplayers, "", gameServer.additional);
                        gameServer.CreateServerCFG(serverName, GetRCONPassword(), port);

                        break;
                    }
                case (GameServer.CS.FullName):
                    {
                        GameServer.CS gameServer = new GameServer.CS(serverConfig.ServerID);
                        serverConfig.CreateServerDirectory();
                        serverConfig.CreateWindowsGSMConfig(serverGame, serverName, GetIPAddress(), GetAvailablePort(gameServer.port), gameServer.defaultmap, gameServer.maxplayers, "", gameServer.additional);
                        gameServer.CreateServerCFG(serverName, GetRCONPassword());

                        break;
                    }
                case (GameServer.CSCZ.FullName):
                    {
                        GameServer.CSCZ gameServer = new GameServer.CSCZ(serverConfig.ServerID);
                        serverConfig.CreateServerDirectory();
                        serverConfig.CreateWindowsGSMConfig(serverGame, serverName, GetIPAddress(), GetAvailablePort(gameServer.port), gameServer.defaultmap, gameServer.maxplayers, "", gameServer.additional);
                        gameServer.CreateServerCFG(serverName, GetRCONPassword());

                        break;
                    }
                case (GameServer.HL2DM.FullName):
                    {
                        GameServer.HL2DM gameServer = new GameServer.HL2DM(serverConfig.ServerID);
                        serverConfig.CreateServerDirectory();
                        serverConfig.CreateWindowsGSMConfig(serverGame, serverName, GetIPAddress(), GetAvailablePort(gameServer.port), gameServer.defaultmap, gameServer.maxplayers, "", gameServer.additional);
                        gameServer.CreateServerCFG(serverName, GetRCONPassword());

                        break;
                    }
                case (GameServer.L4D2.FullName):
                    {
                        GameServer.L4D2 gameServer = new GameServer.L4D2(serverConfig.ServerID);
                        serverConfig.CreateServerDirectory();
                        serverConfig.CreateWindowsGSMConfig(serverGame, serverName, GetIPAddress(), GetAvailablePort(gameServer.port), gameServer.defaultmap, gameServer.maxplayers, "", gameServer.additional);
                        gameServer.CreateServerCFG(serverName, GetRCONPassword());

                        break;
                    }
                case (GameServer.MC.FullName):
                    {
                        GameServer.MC gameServer = new GameServer.MC(serverConfig.ServerID);
                        serverConfig.CreateServerDirectory();

                        string ip = GetIPAddress();
                        string port = GetAvailablePort(gameServer.port);
                        serverConfig.CreateWindowsGSMConfig(serverGame, serverName, ip, port, gameServer.defaultmap, gameServer.maxplayers, "", gameServer.additional);
                        gameServer.CreateServerCFG(serverName, ip, port, GetRCONPassword());

                        break;
                    }
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
                Function.ServerTable row = WindowsGSM.ServerGrid.Items[i] as Function.ServerTable;
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

        private static string GetRCONPassword()
        {
            string allowedChars = "abcdefghijkmnopqrstuvwxyzABCDEFGHJKLMNOPQRSTUVWXYZ0123456789!@$?_-";
            char[] chars = new char[12];
            Random rd = new Random();

            for (int i = 0; i < 12; i++)
            {
                chars[i] = allowedChars[rd.Next(0, allowedChars.Length)];
            }

            return new string(chars);
        }
    }
}
