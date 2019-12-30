using System;
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;
using System.Net;

namespace WindowsGSM.GameServer
{
    class _7DTD
    {
        private readonly string _serverId;

        private string _param;
        public string Error;
        public string Notice;

        public const string FullName = "7 Days to Die Dedicated Server";

        public string port = "26900";
        public string defaultmap = "Navezgane";
        public string maxplayers = "8";
        public string additional = "";

        public _7DTD(string serverid)
        {
            _serverId = serverid;
        }

        public async void CreateServerCFG(string hostname, string rcon_password, string port)
        {
            try
            {
                using (WebClient webClient = new WebClient())
                {
                    //Download serverconfig.xml
                    string configPath = Functions.Path.GetServerFiles(_serverId, "serverconfig.xml");
                    await webClient.DownloadFileTaskAsync($"https://github.com/WindowsGSM/Game-Server-Configs/raw/master/{FullName}/serverconfig.xml", configPath);
                    string configText = File.ReadAllText(configPath);
                    configText = configText.Replace("{{hostname}}", hostname);
                    configText = configText.Replace("{{rcon_password}}", rcon_password);
                    configText = configText.Replace("{{port}}", port);
                    configText = configText.Replace("{{telnetPort}}", (Int32.Parse(port) - Int32.Parse(this.port) + 8081).ToString());
                    configText = configText.Replace("{{maxplayers}}", maxplayers);
                    File.WriteAllText(configPath, configText);
                }
            }
            catch
            {

            }

            //Create steam_appid.txt
            string txtPath = Functions.Path.GetServerFiles(_serverId, "steam_appid.txt");
            File.WriteAllText(txtPath, "251570");
        }

        public void SetParameter(string additional)
        {
            _param = $"start 7DaysToDieServer -quit -batchmode -nographics -configfile=serverconfig.xml -dedicated {additional}";
        }

        public async Task<Process> Start()
        {
            string configPath = Functions.Path.GetServerFiles(_serverId, "serverconfig.xml");
            if (!File.Exists(configPath))
            {
                Notice = $"serverconfig.xml not found ({configPath})";
            }

            Steam.SRCDS srcds = new Steam.SRCDS(_serverId);
            Process p = await srcds.Start(_param, "7DaysToDieServer.exe", false);
            Error = srcds.Error;

            return p;
        }

        public static async Task<bool> Stop(Process p)
        {
            return await Steam.SRCDS.Stop(p, sendCloseMessage: true);
        }

        public async Task<Process> Install()
        {
            Steam.SRCDS srcds = new Steam.SRCDS(_serverId);
            Process p = await srcds.Install("294420");
            Error = srcds.Error;

            return p;
        }

        public async Task<bool> Update()
        {
            Steam.SRCDS srcds = new Steam.SRCDS(_serverId);
            bool success = await srcds.Update("294420");
            Error = srcds.Error;

            return success;
        }
    }
}
