using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;

namespace WindowsGSM.GameServer
{
    /// <summary>
    /// 
    /// Notes:
    /// Rust server is the most user-unfriendly server in my opinion. Both RedirectStandardInput or RedirectStandardOutput cannot use on WindowsGSM.
    /// RedirectStandardOutput is possible but it will break the input, if used both, the server can run successfully but the input become useless again.
    /// 
    /// The solution for this is don't use either RedirectStandardInput or RedirectStandardOutput.
    /// Just use the traditional method to handle the server.
    /// 
    /// </summary>
    class RUST
    {
        private readonly string _serverId;

        private string _param;
        public string Error;
        public string Notice;

        public const string FullName = "Rust Dedicated Server";
        public const bool ToggleConsole = true;

        public string port = "28015";
        public string defaultmap = "Procedural Map";
        public string maxplayers = "50";
        public string additional = "";

        public RUST(string serverid)
        {
            _serverId = serverid;
        }

        public async void CreateServerCFG(string hostname, string rcon_password, string port)
        {
            //Download server.cfg
            string configPath = Functions.Path.GetServerFiles(_serverId, "server.cfg");
            if (await Functions.Github.DownloadGameServerConfig(configPath, FullName, "server.cfg"))
            {
                string configText = File.ReadAllText(configPath);
                configText = configText.Replace("{{hostname}}", hostname);
                configText = configText.Replace("{{rcon_password}}", rcon_password);
                configText = configText.Replace("{{port}}", port);
                File.WriteAllText(configPath, configText);
            }
        }

        public void SetParameter(string ip, string port, string map, string maxplayers)
        {
            _param = $"-nographics -batchmode -silent-crashes +server.ip {ip} +server.port {port} +server.level \"{map}\" +server.maxplayers {maxplayers} ";

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

            string workingDir = Functions.Path.GetServerFiles(_serverId);
            string srcdsPath = Path.Combine(workingDir, "RustDedicated.exe");

            WindowsFirewall firewall = new WindowsFirewall("RustDedicated.exe", srcdsPath);
            if (!await firewall.IsRuleExist())
            {
                firewall.AddRule();
            }

            Process p = new Process
            {
                StartInfo =
                {
                    WorkingDirectory = workingDir,
                    FileName = srcdsPath,
                    Arguments = _param,
                },
            };
            p.Start();

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
