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
            string configpath = MainWindow.WGSM_PATH + @"\servers\" + serverid + @"\configs\WindowsGSM.cfg";
            if (File.Exists(configpath))
            {
                foreach (string line in File.ReadLines(configpath))
                {
                    if (line.Contains("servergame=\""))
                    {
                        ServerGame = line.Replace("servergame=\"", "").Replace("\"", "");
                    }
                    else if(line.Contains("servername=\""))
                    {
                        ServerName = line.Replace("servername=\"", "").Replace("\"", "");
                    }
                    else if (line.Contains("serverip=\""))
                    {
                        ServerIP = line.Replace("serverip=\"", "").Replace("\"", "");
                    }
                    else if (line.Contains("serverport=\""))
                    {
                        ServerPort = line.Replace("serverport=\"", "").Replace("\"", "");
                    }
                    else if (line.Contains("servermap=\""))
                    {
                        ServerMap = line.Replace("servermap=\"", "").Replace("\"", "");
                    }
                    else if (line.Contains("servermaxplayer=\""))
                    {
                        ServerMaxPlayer = line.Replace("servermaxplayer=\"", "").Replace("\"", "");
                    }
                    else if (line.Contains("servergslt=\""))
                    {
                        ServerGSLT = line.Replace("servergslt=\"", "").Replace("\"", "");
                    }
                    else if (line.Contains("serverparam=\""))
                    {
                        ServerParam = line.Replace("serverparam=\"", "").Replace("\"", "");
                    }
                    else if (line.Contains("autorestart=\""))
                    {
                        string Bool = line.Replace("autorestart=\"", "").Replace("\"", "");
                        AutoRestart = (Bool == "1") ? true : false;
                    }
                    else if (line.Contains("updateonstart=\""))
                    {
                        string Bool = line.Replace("updateonstart=\"", "").Replace("\"", "");
                        UpdateOnStart = (Bool == "1") ? true : false;
                    }
                    else if (line.Contains("discordalert=\""))
                    {
                        string Bool = line.Replace("discordalert=\"", "").Replace("\"", "");
                        DiscordAlert = (Bool == "1") ? true : false;
                    }
                    else if (line.Contains("discordwebhook=\""))
                    {
                        DiscordWebhook = line.Replace("discordwebhook=\"", "").Replace("\"", "");
                    }
                }
            }
        }

        public bool CreateWindowsGSMConfig(string servergame, string servername, string serverip, string serverport, string servermap, string servermaxplayer, string servergslt, string serverparam)
        {
            string configpath = MainWindow.WGSM_PATH + @"\servers\" + ServerID + @"\configs\WindowsGSM.cfg";
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
            string serverid_dir = MainWindow.WGSM_PATH + @"\servers\" + ServerID;

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
            string serverid_dir = MainWindow.WGSM_PATH + @"\servers\" + ServerID;
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
            string configpath = MainWindow.WGSM_PATH + @"\servers\" + ServerID + @"\configs\WindowsGSM.cfg";
            return File.Exists(configpath);
        }
    }
}
