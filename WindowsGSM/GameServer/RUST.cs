using System;
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;

namespace WindowsGSM.GameServer
{
    class RUST
    {
        private readonly string _serverId;

        private string _param;
        public string Error;
        public string Notice;

        public const string FullName = "Rust Dedicated Server";

        public string port = "28015";
        public string defaultmap = "Procedural Map";
        public string maxplayers = "50";
        public string additional = "";

        public RUST(string serverid)
        {
            _serverId = serverid;
        }

        public void CreateServerCFG(string hostname, string rcon_password, string port)
        {
            string configPath = Functions.Path.GetServerFiles(_serverId, "server.cfg");

            File.Create(configPath).Dispose();

            using (TextWriter textwriter = new StreamWriter(configPath))
            {
                textwriter.WriteLine("+rcon.ip 0.0.0.0");
                textwriter.WriteLine($"+rcon.port \"{port}\"");
                textwriter.WriteLine($"+rcon.password \"{rcon_password}\"");
                textwriter.WriteLine("+rcon.web 1");
                textwriter.WriteLine("+server.tickrate 10");
                textwriter.WriteLine($"+server.hostname \"{hostname}\"");
                textwriter.WriteLine("+server.description \"Rust Dedicated Server - Manage by WindowsGSM\\n\\nEdit server.cfg\"");
                textwriter.WriteLine("+server.url \"https://github.com/BattlefieldDuck/WindowsGSM\"");
                textwriter.WriteLine("+server.headerimage \"\"");
                textwriter.WriteLine("+server.identity \"server1\"");
                textwriter.WriteLine("+server.seed 689777");
                textwriter.WriteLine("+server.maxplayers 50");
                textwriter.WriteLine("+server.worldsize 3000");
                textwriter.WriteLine("+server.saveinterval 600");
                textwriter.WriteLine("-logfile \"server.log\"");
            }
        }

        public void SetParameter(string ip, string port, string map, string maxplayers)
        {
            _param = $"-batchmode +server.ip {ip} +server.port {port} +server.level \"{map}\" +server.maxplayers {maxplayers} ";

            string configPath = Functions.Path.GetServerFiles(_serverId, "server.cfg");
            if (File.Exists(configPath))
            {
                foreach (string line in File.ReadLines(configPath))
                {
                    _param += line + " ";
                }
            }

            _param.TrimEnd();
        }

        public async Task<Process> Start()
        {
            string configPath = Functions.Path.GetServerFiles(_serverId, "server.cfg");
            if (!File.Exists(configPath))
            {
                Notice = $"server.cfg not found ({configPath})";
            }

            Steam.SRCDS srcds = new Steam.SRCDS(_serverId);
            Process p = await srcds.Start(_param, "RustDedicated.exe");
            Error = srcds.Error;

            return p;
        }

        public static async Task<bool> Stop(Process p)
        {
            return await Steam.SRCDS.Stop(p);
        }

        public async Task<Process> Install()
        {
            Steam.SRCDS srcds = new Steam.SRCDS(_serverId);
            Process p = await srcds.Install("258550");
            Error = srcds.Error;

            return p;
        }

        public async Task<bool> Update()
        {
            Steam.SRCDS srcds = new Steam.SRCDS(_serverId);
            bool success = await srcds.Update("258550");
            Error = srcds.Error;

            return success;
        }
    }
}
