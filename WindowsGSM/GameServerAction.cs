using System;
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;

namespace WindowsGSM
{
    class GameServerAction
    {
        private readonly GameServerTable server;
        private readonly string gslt = "";
        private readonly string additionalParam = "";
        private readonly Functions.ServerConfig serverConfig;
        public string Error;
        public string Notice;

        //Install, Import
        public GameServerAction(Functions.ServerConfig serverConfig)
        {
            this.serverConfig = serverConfig;
        }

        //Stop, Update
        public GameServerAction(GameServerTable server)
        {
            this.server = server;
        }

        //Start, Restart
        public GameServerAction(GameServerTable server, string gslt, string additionalParam)
        {
            this.server = server;
            this.gslt = gslt;
            this.additionalParam = additionalParam;
        }

        public Process Start()
        {
            Process process = null;

            switch (server.Game)
            {
                case ("Counter-Strike: Global Offensive Dedicated Server"):
                    {
                        GameServer.CSGO gameServer = new GameServer.CSGO(server.ID);
                        gameServer.SetParameter(server.IP, server.Port, server.Defaultmap, server.Maxplayers, gslt, additionalParam);
                        (process, Error, Notice) = gameServer.Start();

                        break;
                    }
                case ("Garry's Mod Dedicated Server"):
                    {
                        GameServer.GMOD gameServer = new GameServer.GMOD(server.ID);
                        gameServer.SetParameter(server.IP, server.Port, server.Defaultmap, server.Maxplayers, gslt, additionalParam);
                        (process, Error, Notice) = gameServer.Start();

                        break;
                    }
                case ("Team Fortress 2 Dedicated Server"):
                    {
                        GameServer.TF2 gameServer = new GameServer.TF2(server.ID);
                        gameServer.SetParameter(server.IP, server.Port, server.Defaultmap, server.Maxplayers, gslt, additionalParam);
                        (process, Error, Notice) = gameServer.Start();

                        break;
                    }
                case ("Minecraft Pocket Edition Server | PocketMine-MP"):
                    {
                        GameServer.MCPE gameServer = new GameServer.MCPE(server.ID);
                        (process, Error, Notice) = gameServer.Start();

                        break;
                    }
                case ("Rust Dedicated Server"):
                    {
                        GameServer.RUST gameServer = new GameServer.RUST(server.ID);
                        gameServer.SetParameter(server.IP, server.Port, server.Defaultmap, server.Maxplayers);
                        (process, Error, Notice) = gameServer.Start();

                        break;
                    }
                default: break;
            }

            return process;
        }

        public async Task<bool> Stop(Process process)
        {
            switch (server.Game)
            {
                case ("Counter-Strike: Global Offensive Dedicated Server"):
                    {
                        GameServer.CSGO gameServer = new GameServer.CSGO(server.ID);
                        await gameServer.Stop(process);

                        break;
                    }
                case ("Garry's Mod Dedicated Server"):
                    {
                        GameServer.GMOD gameServer = new GameServer.GMOD(server.ID);
                        await gameServer.Stop(process);

                        break;
                    }
                case ("Team Fortress 2 Dedicated Server"):
                    {
                        GameServer.TF2 gameServer = new GameServer.TF2(server.ID);
                        await gameServer.Stop(process);

                        break;
                    }
                case ("Minecraft Pocket Edition Server | PocketMine-MP"):
                    {
                        GameServer.MCPE gameServer = new GameServer.MCPE(server.ID);
                        await gameServer.Stop(process);

                        break;
                    }
                case ("Rust Dedicated Server"):
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

        public async Task<Process> Restart(Process process)
        {
            await Stop(process);

            return Start();
        }

        //Return installer process. Example: SteamCMD
        public async Task<Process> Install(string serverGame)
        {
            switch (serverGame)
            {
                case ("Counter-Strike: Global Offensive Dedicated Server"):
                    {
                        GameServer.CSGO gameServer = new GameServer.CSGO(serverConfig.ServerID);
                        return await gameServer.Install();
                    }
                case ("Garry's Mod Dedicated Server"):
                    {
                        GameServer.GMOD gameServer = new GameServer.GMOD(serverConfig.ServerID);
                        return await gameServer.Install();
                    }
                case ("Team Fortress 2 Dedicated Server"):
                    {
                        GameServer.TF2 gameServer = new GameServer.TF2(serverConfig.ServerID);
                        return await gameServer.Install();
                    }
                case ("Minecraft Pocket Edition Server | PocketMine-MP"):
                    {
                        GameServer.MCPE gameServer = new GameServer.MCPE(serverConfig.ServerID);
                        await gameServer.Install();

                        return null;
                    }
                case ("Rust Dedicated Server"):
                    {
                        GameServer.RUST gameServer = new GameServer.RUST(serverConfig.ServerID);
                        return await gameServer.Install();
                    }
                default: break;
            }

            return null;
        }

        public async Task<bool> IsInstallSuccess(Process process, string serverGame, string serverName)
        {
            if (process != null)
            {
                await Task.Run(() => process.WaitForExit());
            }
               
            switch (serverGame)
            {
                case ("Counter-Strike: Global Offensive Dedicated Server"):
                case ("Garry's Mod Dedicated Server"):
                case ("Team Fortress 2 Dedicated Server"):
                    {
                        string srcdsPath = Functions.Path.GetServerFiles(serverConfig.ServerID, "srcds.exe");
                        if (File.Exists(srcdsPath))
                        {
                            CreateServerConfigs(serverGame, serverName, true);

                            return true;
                        }

                        return false;
                    }
                case ("Minecraft Pocket Edition Server | PocketMine-MP"):
                    {
                        string PHPPath = Functions.Path.GetServerFiles(serverConfig.ServerID, @"bin\php\php.exe");
                        string PMMPPath = Functions.Path.GetServerFiles(serverConfig.ServerID, "PocketMine-MP.phar");
                        if (File.Exists(PHPPath) && File.Exists(PMMPPath))
                        {
                            CreateServerConfigs(serverGame, serverName, true);

                            return true;
                        }

                        return false;
                    }
                case ("Rust Dedicated Server"):
                    {
                        string rustPath = Functions.Path.GetServerFiles(serverConfig.ServerID, "RustDedicated.exe");
                        if (File.Exists(rustPath))
                        {
                            CreateServerConfigs(serverGame, serverName, true);

                            return true;
                        }

                        return false;
                    }
            }

            return false;
        }

        public async Task<bool> Update()
        {
            bool updated = false;

            switch (server.Game)
            {
                case ("Counter-Strike: Global Offensive Dedicated Server"):
                    {
                        GameServer.CSGO gameServer = new GameServer.CSGO(server.ID);
                        updated = await gameServer.Update();
                        Error = gameServer.Error;
                        break;
                    }
                case ("Garry's Mod Dedicated Server"):
                    {
                        GameServer.GMOD gameServer = new GameServer.GMOD(server.ID);
                        updated = await gameServer.Update();
                        Error = gameServer.Error;
                        break;
                    }
                case ("Team Fortress 2 Dedicated Server"):
                    {
                        GameServer.TF2 gameServer = new GameServer.TF2(server.ID);
                        updated = await gameServer.Update();
                        Error = gameServer.Error;
                        break;
                    }
                case ("Minecraft Pocket Edition Server | PocketMine-MP"):
                    {
                        GameServer.MCPE gameServer = new GameServer.MCPE(server.ID);
                        updated = await gameServer.Update();
                        Error = gameServer.Error;
                        break;
                    }
                case ("Rust Dedicated Server"):
                    {
                        GameServer.RUST gameServer = new GameServer.RUST(server.ID);
                        updated = await gameServer.Update();
                        Error = gameServer.Error;
                        break;
                    }
                default: break;
            }

            return updated;
        }

        public void CreateServerConfigs(string serverGame, string serverName, bool isInstall)
        {
            switch (serverGame)
            {
                case ("Counter-Strike: Global Offensive Dedicated Server"):
                    {
                        GameServer.CSGO gameServer = new GameServer.CSGO(serverConfig.ServerID);
                        serverConfig.CreateServerDirectory();
                        serverConfig.CreateWindowsGSMConfig(serverGame, serverName, GetIPAddress(), GetAvailablePort(gameServer.port), gameServer.defaultmap, gameServer.maxplayers, "", gameServer.additional);

                        if (isInstall)
                        {
                            gameServer.CreateServerCFG(serverName, GetRCONPassword());
                        }

                        break;
                    }
                case ("Garry's Mod Dedicated Server"):
                    {
                        GameServer.GMOD gameServer = new GameServer.GMOD(serverConfig.ServerID);
                        serverConfig.CreateServerDirectory();
                        serverConfig.CreateWindowsGSMConfig(serverGame, serverName, GetIPAddress(), GetAvailablePort(gameServer.port), gameServer.defaultmap, gameServer.maxplayers, "", gameServer.additional);

                        if (isInstall)
                        {
                            gameServer.CreateServerCFG(serverName, GetRCONPassword());
                        }

                        break;
                    }
                case ("Team Fortress 2 Dedicated Server"):
                    {
                        GameServer.TF2 gameServer = new GameServer.TF2(serverConfig.ServerID);
                        serverConfig.CreateServerDirectory();
                        serverConfig.CreateWindowsGSMConfig(serverGame, serverName, GetIPAddress(), GetAvailablePort(gameServer.port), gameServer.defaultmap, gameServer.maxplayers, "", gameServer.additional);

                        if (isInstall)
                        {
                            gameServer.CreateServerCFG(serverName, GetRCONPassword());
                        }

                        break;
                    }
                case ("Minecraft Pocket Edition Server | PocketMine-MP"):
                    {
                        GameServer.MCPE gameServer = new GameServer.MCPE(serverConfig.ServerID);
                        serverConfig.CreateServerDirectory();

                        string port = GetAvailablePort(gameServer.port);
                        serverConfig.CreateWindowsGSMConfig(serverGame, serverName, GetIPAddress(), port, gameServer.defaultmap, gameServer.maxplayers, "", gameServer.additional);

                        if (isInstall)
                        {
                            gameServer.CreateServerCFG(serverName, port, GetRCONPassword());
                        }

                        break;
                    }
                case ("Rust Dedicated Server"):
                    {
                        GameServer.RUST gameServer = new GameServer.RUST(serverConfig.ServerID);
                        serverConfig.CreateServerDirectory();

                        string port = GetAvailablePort(gameServer.port);
                        serverConfig.CreateWindowsGSMConfig(serverGame, serverName, GetIPAddress(), port, gameServer.defaultmap, gameServer.maxplayers, "", gameServer.additional);

                        if (isInstall)
                        {
                            gameServer.CreateServerCFG(serverName, GetRCONPassword(), port);
                        }

                        break;
                    }
            }
        }

        public bool CanImport(string serverGame, string serverDir)
        {
            //Check is the path contain game server files
            switch (serverGame)
            {
                case ("Counter-Strike: Global Offensive Dedicated Server"):
                case ("Garry's Mod Dedicated Server"):
                case ("Team Fortress 2 Dedicated Server"):
                    {
                        string srcdsPath = Path.Combine(serverDir, "srcds.exe");
                        if (File.Exists(srcdsPath))
                        {
                            return true;
                        }
                        else
                        {
                            Error = "Invalid Path! Fail to find srcds.exe";
                            return false;
                        }
                    }
                case ("Minecraft Pocket Edition Server | PocketMine-MP"):
                    {
                        string PHPPath = Path.Combine(serverDir, @"bin\php\php.exe");
                        string PMMPPath = Path.Combine(serverDir, "PocketMine-MP.phar");
                        string startPath = Path.Combine(serverDir, "start.cmd");

                        if (File.Exists(PHPPath) && File.Exists(PMMPPath) && File.Exists(startPath))
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
                            else if(!File.Exists(startPath))
                            {
                                Error = "Invalid Path! Fail to find start.cmd";
                            }

                            return false;
                        }
                    }
                case ("Rust Dedicated Server"):
                    {
                        string rustPath = Path.Combine(serverDir, "RustDedicated.exe");
                        if (File.Exists(rustPath))
                        {
                            return true;
                        }
                        else
                        {
                            Error = "Invalid Path! Fail to find RustDedicated.exe";
                            return false;
                        }
                    }
                default: break;
            }

            return false;
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
                GameServerTable row = WindowsGSM.ServerGrid.Items[i] as GameServerTable;
                portlist[i] = Int32.Parse((string.IsNullOrWhiteSpace(row.Port)) ? "0" : row.Port);
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