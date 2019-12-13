using System;
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace WindowsGSM.GameServer
{
    class CS
    {
        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        private readonly string ServerID;

        private string Param;
        public string Error;

        public const string FullName = "Counter-Strike: 1.6 Dedicated Server";

        public string port = "27015";
        public string defaultmap = "de_dust2";
        public string maxplayers = "24";
        public string additional = "";

        public CS(string serverid)
        {
            ServerID = serverid;
        }

        public void CreateServerCFG(string hostname, string rcon_password)
        {
            string serverConfigPath = Functions.Path.GetServerFiles(ServerID) + @"\cstrike\server.cfg";

            File.Create(serverConfigPath).Dispose();

            using (TextWriter textwriter = new StreamWriter(serverConfigPath))
            {
                textwriter.WriteLine("hostname \"" + hostname + "\"");
                textwriter.WriteLine("rcon_password \"" + rcon_password + "\"");
                textwriter.WriteLine("sv_password \"\"");
                textwriter.WriteLine("sv_aim \"0\"");
                textwriter.WriteLine("pausable  \"0\"");
                textwriter.WriteLine("sv_maxspeed \"320\"");
            }

            string txtPath = Functions.Path.GetServerFiles(ServerID) + @"\steam_appid.txt";

            File.Create(txtPath).Dispose();

            using (TextWriter textwriter = new StreamWriter(txtPath))
            {
                textwriter.WriteLine("10");
            }
        }

        public void SetParameter(string ip, string port, string map, string maxplayers, string gslt, string additional)
        {
            Param = "-console -game cstrike";
            Param += String.Format("{0}", String.IsNullOrEmpty(ip) ? "" : $" -ip {ip}");
            Param += String.Format("{0}", String.IsNullOrEmpty(port) ? "" : $" -port {port}");
            Param += String.Format("{0}", String.IsNullOrEmpty(maxplayers) ? "" : $" -maxplayers {maxplayers}");
            Param += String.Format("{0}", String.IsNullOrEmpty(gslt) ? "" : $" +sv_setsteamaccount {gslt}");
            Param += String.Format("{0}", String.IsNullOrEmpty(additional) ? "" : $" {additional}");
            Param += String.Format("{0}", String.IsNullOrEmpty(map) ? "" : $" +map {map}");
        }

        public (Process Process, string Error, string Notice) Start()
        {
            string workingDir = Functions.Path.GetServerFiles(ServerID);
            string hldsPath = workingDir + @"\hlds.exe";

            if (!File.Exists(hldsPath))
            {
                return (null, "hlds.exe not found (" + hldsPath + ")", "");
            }

            if (string.IsNullOrWhiteSpace(Param))
            {
                return (null, "Start Parameter not set", "");
            }

            string serverConfigPath = workingDir + @"\cstrike\server.cfg";
            if (!File.Exists(serverConfigPath))
            {
                return (null, "", "server.cfg not found (" + serverConfigPath + ")");
            }

            WindowsFirewall firewall = new WindowsFirewall("hlds.exe", hldsPath);
            if (!firewall.IsRuleExist())
            {
                firewall.AddRule();
            }

            Process p = new Process();
            p.StartInfo.WorkingDirectory = workingDir;
            p.StartInfo.FileName = hldsPath;
            p.StartInfo.Arguments = Param;
            p.StartInfo.WindowStyle = ProcessWindowStyle.Minimized;
            p.Start();

            return (p, "", "");
        }

        public async Task<bool> Stop(Process p)
        {
            SetForegroundWindow(p.MainWindowHandle);
            SendKeys.SendWait("quit");
            SendKeys.SendWait("{ENTER}");
            SetForegroundWindow(Process.GetCurrentProcess().MainWindowHandle);

            int attempt = 0;
            while (attempt++ < 10)
            {
                if (p != null && p.HasExited)
                {
                    return true;
                }

                await Task.Delay(1000);
            }

            return false;
        }

        public async Task<Process> Install()
        {
            Installer.SteamCMD steamCMD = new Installer.SteamCMD();
            steamCMD.SetParameter(null, null, Functions.Path.GetServerFiles(ServerID), "", "90", true);

            if (!await steamCMD.Download())
            {
                Error = steamCMD.GetError();
                return null;
            }

            Process process = steamCMD.Run();
            if (process == null)
            {
                Error = steamCMD.GetError();
                return null;
            }

            return process;
        }

        public async Task<bool> Update()
        {
            Installer.SteamCMD steamCMD = new Installer.SteamCMD();
            steamCMD.SetParameter(null, null, Functions.Path.GetServerFiles(ServerID), "", "90", false);

            if (!await steamCMD.Download())
            {
                Error = steamCMD.GetError();
                return false;
            }

            Process pSteamCMD = steamCMD.Run();
            if (pSteamCMD == null)
            {
                Error = steamCMD.GetError();
                return false;
            }

            await Task.Run(() => pSteamCMD.WaitForExit());

            if (pSteamCMD.ExitCode != 0)
            {
                Error = "Exit code: " + pSteamCMD.ExitCode.ToString();
                return false;
            }

            return true;
        }
    }
}
