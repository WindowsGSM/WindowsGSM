using System;
using System.IO;
using System.Net;
using System.Net.Sockets;

namespace WindowsGSM.Functions
{
    class ServerConfig
    {
        public string ServerID;
        public string ServerGame;
        public string ServerName;
        public string ServerIP;
        public string ServerPort;
        public string ServerMap;
        public string ServerMaxPlayer;
        public string ServerGSLT;
        public string ServerParam;
        public bool AutoRestart;
        public bool AutoStart;
        public bool AutoUpdate;
        public bool UpdateOnStart;
        public bool DiscordAlert;
        public string DiscordWebhook;
        public bool RestartCrontab;
        public string CrontabFormat;

        public ServerConfig(string serverid)
        {
            //Get next available ServerID
            if (serverid == null || serverid == "")
            {
                for (int id = 1; id <= MainWindow.MAX_SERVER; id++)
                {
                    string serverid_dir = MainWindow.WGSM_PATH + @"\servers\" + id.ToString();
                    if (Directory.Exists(serverid_dir))
                    {
                        string config = MainWindow.WGSM_PATH + @"\servers\" + id.ToString() + @"\configs\WindowsGSM.cfg";
                        if (!File.Exists(config))
                        {
                            ServerID = id.ToString();
                            break;
                        }
                    }
                    else
                    {
                        ServerID = id.ToString();
                        break;
                    }
                }

                return;
            }

            ServerID = serverid;

            //Get values from configpath
            string configpath = Functions.ServerPath.GetConfigs(serverid, "WindowsGSM.cfg");
            if (File.Exists(configpath))
            {
                foreach (string line in File.ReadLines(configpath))
                {
                    string[] keyvalue = line.Split(new char[] { '=' }, 2);

                    if (keyvalue.Length == 2)
                    {
                        keyvalue[1] = keyvalue[1].Trim('\"');

                        switch (keyvalue[0])
                        {
                            case "servergame": ServerGame = keyvalue[1]; break;
                            case "servername": ServerName = keyvalue[1]; break;
                            case "serverip": ServerIP = keyvalue[1]; break;
                            case "serverport": ServerPort = keyvalue[1]; break;
                            case "servermap": ServerMap = keyvalue[1]; break;
                            case "servermaxplayer": ServerMaxPlayer = keyvalue[1]; break;
                            case "servergslt": ServerGSLT = keyvalue[1]; break;
                            case "serverparam": ServerParam = keyvalue[1]; break;
                            case "autorestart": AutoRestart = (keyvalue[1] == "1") ? true : false; break;
                            case "autostart": AutoStart = (keyvalue[1] == "1") ? true : false; break;
                            case "autoupdate": AutoUpdate = (keyvalue[1] == "1") ? true : false; break;
                            case "updateonstart": UpdateOnStart = (keyvalue[1] == "1") ? true : false; break;
                            case "discordalert": DiscordAlert = (keyvalue[1] == "1") ? true : false; break;
                            case "discordwebhook": DiscordWebhook = keyvalue[1]; break;
                            case "restartcrontab": RestartCrontab = (keyvalue[1] == "1") ? true : false; break;
                            case "crontabformat": CrontabFormat = keyvalue[1]; break;
                        }
                    }
                }
            }
        }

        public bool CreateWindowsGSMConfig(string servergame, string servername, string serverip, string serverport, string servermap, string servermaxplayer, string servergslt, string serverparam)
        {
            CreateServerDirectory();

            string configpath = Functions.ServerPath.GetConfigs(ServerID, "WindowsGSM.cfg");
            if (!File.Exists(configpath))
            {
                ServerGame = servergame;
                ServerName = servername;
                ServerIP = serverip;
                ServerPort = serverport;
                ServerMap = servermap;
                ServerMaxPlayer = servermaxplayer;
                ServerGSLT = servergslt;
                ServerParam = serverparam;
                AutoRestart = false;
                AutoStart = false;
                AutoUpdate = false;
                UpdateOnStart = false;
                DiscordAlert = false;
                DiscordWebhook = "";
                RestartCrontab = false;
                CrontabFormat = "0 6 * * *";

                File.Create(configpath).Dispose();

                using (TextWriter textwriter = new StreamWriter(configpath))
                {
                    textwriter.WriteLine($"servergame=\"{ServerGame}\"");
                    textwriter.WriteLine($"servername=\"{ServerName}\"");
                    textwriter.WriteLine($"serverip=\"{ServerIP}\"");
                    textwriter.WriteLine($"serverport=\"{ServerPort}\"");
                    textwriter.WriteLine($"servermap=\"{ServerMap}\"");
                    textwriter.WriteLine($"servermaxplayer=\"{ServerMaxPlayer}\"");
                    textwriter.WriteLine($"servergslt=\"{ServerGSLT}\"");
                    textwriter.WriteLine($"serverparam=\"{ServerParam}\"");
                    textwriter.WriteLine("");
                    textwriter.WriteLine("autorestart=\"0\"");
                    textwriter.WriteLine("autostart=\"0\"");
                    textwriter.WriteLine("autoupdate=\"0\"");
                    textwriter.WriteLine("updateonstart=\"0\"");
                    textwriter.WriteLine("");
                    textwriter.WriteLine("discordalert=\"0\"");
                    textwriter.WriteLine($"discordwebhook=\"{DiscordWebhook}\"");
                    textwriter.WriteLine("");
                    textwriter.WriteLine("restartcrontab=\"0\"");
                    textwriter.WriteLine($"crontabformat=\"{CrontabFormat}\"");
                }

                return true;
            }

            return false;
        }

        public void CreateServerDirectory()
        {
            string serverid_dir = Functions.ServerPath.Get(ServerID);
            if (!Directory.Exists(serverid_dir))
            {
                Directory.CreateDirectory(serverid_dir);
            }

            if (!Directory.Exists(serverid_dir + @"\configs"))
            {
                Directory.CreateDirectory(serverid_dir + @"\configs");
            }

            if (!Directory.Exists(serverid_dir + @"\serverfiles"))
            {
                Directory.CreateDirectory(serverid_dir + @"\serverfiles");
            }
        }

        public bool DeleteServerDirectory()
        {
            string serverid_dir = Functions.ServerPath.Get(ServerID);
            if (Directory.Exists(serverid_dir) && ServerID != null && ServerID != "")
            {
                try
                {
                    Directory.Delete(serverid_dir, true);

                    return true;
                }
                catch
                {
                    return false;
                }
            }

            return false;
        }

        public bool IsWindowsGSMConfigExist()
        {
            string configpath = ServerPath.GetConfigs(ServerID, "WindowsGSM.cfg");
            return File.Exists(configpath);
        }

        public string GetIPAddress()
        {
            using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0))
            {
                socket.Connect("8.8.8.8", 65530);
                IPEndPoint endPoint = socket.LocalEndPoint as IPEndPoint;
                return endPoint.Address.ToString();
            }
        }

        public string GetAvailablePort(string defaultport)
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

        public string GetRCONPassword()
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

        public static bool ToggleSetting(string serverId, string settingName)
        {
            string configFile = ServerPath.GetConfigs(serverId, "WindowsGSM.cfg");

            if (File.Exists(configFile))
            {
                bool? returnBool = null;

                //Read the config lines
                string[] lines = File.ReadAllLines(configFile);

                //Overwrite the config file
                File.Create(configFile).Dispose();

                //Create the TextWriter
                using (TextWriter textwriter = new StreamWriter(configFile))
                {
                    //Write all lines
                    foreach (string line in lines)
                    {
                        string[] keyvalue = line.Split(new char[] { '=' }, 2);
                        if (keyvalue.Length == 2 && settingName == keyvalue[0])
                        {
                            keyvalue[1] = keyvalue[1].Trim('\"');
                            returnBool = (keyvalue[1] == "1") ? false : true;
                            string nextBool = (keyvalue[1] == "1") ? "0" : "1";
                            textwriter.WriteLine($"{keyvalue[0]}=\"{nextBool}\"");
                        }
                        else
                        {
                            textwriter.WriteLine(line);
                        }
                    }

                    if (returnBool == null)
                    {
                        returnBool = true;
                        textwriter.WriteLine($"{settingName}=\"1\"");
                    }
                }

                return returnBool ?? true;
            }

            return false;
        }

        public static string GetSetting(string serverId, string settingName)
        {
            string configFile = ServerPath.GetConfigs(serverId, "WindowsGSM.cfg");

            if (File.Exists(configFile))
            {
                //Read the config lines
                string[] lines = File.ReadAllLines(configFile);

                //Read all lines
                foreach (string line in lines)
                {
                    string[] keyvalue = line.Split(new char[] { '=' }, 2);
                    if (keyvalue.Length == 2)
                    {
                        if (settingName == keyvalue[0])
                        {
                            return keyvalue[1].Trim('\"');
                        }
                    }
                }
            }

            return "";
        }

        public static void SetSetting(string serverId, string settingName, string data)
        {
            string configFile = ServerPath.GetConfigs(serverId, "WindowsGSM.cfg");

            if (File.Exists(configFile))
            {
                bool saved = false;

                //Read the config lines
                string[] lines = File.ReadAllLines(configFile);

                //Overwrite the config file
                File.Create(configFile).Dispose();

                //Create the TextWriter
                using (TextWriter textwriter = new StreamWriter(configFile))
                {
                    //Write lines
                    foreach (string line in lines)
                    {
                        string[] keyvalue = line.Split(new char[] { '=' }, 2);
                        if (keyvalue.Length == 2 && settingName == keyvalue[0])
                        {
                            textwriter.WriteLine($"{settingName}=\"{data}\"");
                            saved = true;
                        }
                        else
                        {
                            textwriter.WriteLine(line);
                        }
                    }

                    if (!saved)
                    {
                        textwriter.WriteLine($"{settingName}=\"{data}\"");
                    }
                }
            }
        }
    }
}
