using System;
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;

namespace WindowsGSM.GameServer
{
    class CSCZ
    {
        private readonly string _serverId;

        private string _param;
        public string Error;
        public string Notice;

        public const string FullName = "Counter-Strike: Condition Zero Dedicated Server";

        public string port = "27015";
        public string defaultmap = "de_dust2";
        public string maxplayers = "24";
        public string additional = "";

        public CSCZ(string serverid)
        {
            _serverId = serverid;
        }

        public void CreateServerCFG(string hostname, string rcon_password)
        {
            string configPath = Functions.Path.GetServerFiles(_serverId, @"czero\server.cfg");

            File.Create(configPath).Dispose();

            using (TextWriter textwriter = new StreamWriter(configPath))
            {
                textwriter.WriteLine($"hostname \"{hostname}\"");
                textwriter.WriteLine($"rcon_password \"{rcon_password}\"");
                textwriter.WriteLine($"sv_password \"\"");
            }

            string txtPath = Functions.Path.GetServerFiles(_serverId, "steam_appid.txt");

            File.Create(txtPath).Dispose();

            using (TextWriter textwriter = new StreamWriter(txtPath))
            {
                textwriter.WriteLine("80");
            }
        }

        public void SetParameter(string ip, string port, string map, string maxplayers, string gslt, string additional)
        {
            _param = "-console -game czero";
            _param += String.Format("{0}", String.IsNullOrEmpty(ip) ? "" : $" -ip {ip}");
            _param += String.Format("{0}", String.IsNullOrEmpty(port) ? "" : $" -port {port}");
            _param += String.Format("{0}", String.IsNullOrEmpty(maxplayers) ? "" : $" -maxplayers {maxplayers}");
            _param += String.Format("{0}", String.IsNullOrEmpty(gslt) ? "" : $" +sv_setsteamaccount {gslt}");
            _param += String.Format("{0}", String.IsNullOrEmpty(additional) ? "" : $" {additional}");
            _param += String.Format("{0}", String.IsNullOrEmpty(map) ? "" : $" +map {map}");
        }

        public async Task<Process> Start()
        {
            string configPath = Functions.Path.GetServerFiles(_serverId, @"czero\server.cfg");
            if (!File.Exists(configPath))
            {
                Notice = $"server.cfg not found ({configPath})";
            }

            Steam.HLDS hlds = new Steam.HLDS(_serverId);
            Process p = await hlds.Start(_param);
            Error = hlds.Error;

            return p;
        }

        public static async Task<bool> Stop(Process p)
        {
            return await Steam.HLDS.Stop(p);
        }

        public async Task<Process> Install()
        {
            Steam.HLDS hlds = new Steam.HLDS(_serverId);
            Process p = await hlds.Install("90 mod czero", "90");
            Error = hlds.Error;

            return p;
        }

        public async Task<bool> Update()
        {
            Steam.HLDS hlds = new Steam.HLDS(_serverId);
            bool success = await hlds.Update("90 mod czero", "90");
            Error = hlds.Error;

            return success;
        }
    }
}
