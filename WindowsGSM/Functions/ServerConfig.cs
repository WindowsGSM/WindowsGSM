using System.IO;

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
        public bool UpdateOnStart;
        public bool DiscordAlert;
        public string DiscordWebhook;

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
            string configpath = Functions.Path.GetConfigs(serverid, "WindowsGSM.cfg");
            if (File.Exists(configpath))
            {
                foreach (string line in File.ReadLines(configpath))
                {
                    string[] keyvalue = line.Split('=');
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
                            case "updateonstart": UpdateOnStart = (keyvalue[1] == "1") ? true : false; break;
                            case "discordalert": DiscordAlert = (keyvalue[1] == "1") ? true : false; break;
                            case "discordwebhook": DiscordWebhook = keyvalue[1]; break;
                        }
                    }
                }
            }
        }

        public bool CreateWindowsGSMConfig(string servergame, string servername, string serverip, string serverport, string servermap, string servermaxplayer, string servergslt, string serverparam)
        {
            string configpath = Functions.Path.GetConfigs(ServerID, "WindowsGSM.cfg");
            if (!File.Exists(configpath))
            {
                File.Create(configpath).Dispose();

                using (TextWriter textwriter = new StreamWriter(configpath))
                {
                    textwriter.WriteLine("servergame=\"" + servergame + "\"");
                    textwriter.WriteLine("servername=\"" + servername + "\"");
                    textwriter.WriteLine("serverip=\"" + serverip + "\"");
                    textwriter.WriteLine("serverport=\"" + serverport + "\"");
                    textwriter.WriteLine("servermap=\"" + servermap + "\"");
                    textwriter.WriteLine("servermaxplayer=\"" + servermaxplayer + "\"");
                    textwriter.WriteLine("servergslt=\"" + servergslt + "\"");
                    textwriter.WriteLine("serverparam=\"" + serverparam + "\"");
                    textwriter.WriteLine("");
                    textwriter.WriteLine("autorestart=\"1\"");
                    textwriter.WriteLine("updateonstart=\"0\"");
                    textwriter.WriteLine("");
                    textwriter.WriteLine("discordalert=\"0\"");
                    textwriter.WriteLine("discordwebhook=\"\"");
                }

                ServerGame = servergame;
                ServerName = servername;
                ServerIP = serverip;
                ServerPort = serverport;
                ServerMap = servermap;
                ServerMaxPlayer = servermaxplayer;
                ServerGSLT = servergslt;
                ServerParam = serverparam;
                AutoRestart = true;
                UpdateOnStart = false;
                DiscordAlert = false;
                DiscordWebhook = "";

                return true;
            }

            return false;
        }

        public void CreateServerDirectory()
        {
            string serverid_dir = Functions.Path.Get(ServerID);
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
            string serverid_dir = Functions.Path.Get(ServerID);
            if (Directory.Exists(serverid_dir) && ServerID != null && ServerID != "")
            {
                try
                {
                    Directory.Delete(serverid_dir, true);
                }
                catch
                {
                    return false;
                }

                return true;
            }

            return false;
        }

        public bool IsWindowsGSMConfigExist()
        {
            string configpath = Functions.Path.GetConfigs(ServerID, "WindowsGSM.cfg");
            return File.Exists(configpath);
        }
    }
}
