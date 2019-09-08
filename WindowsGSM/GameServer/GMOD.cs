using System;
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace WindowsGSM.GameServer
{
    class GMOD
    {
        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        private readonly string ServerID;

        private string Param;
        public string Error;

        public string port = "27015";
        public string defaultmap = "gm_construct";
        public string maxplayers = "24";
        public string additional = "-tickrate 66 +gamemode sandbox +host_workshop_collection";

        public GMOD(string serverid)
        {
            ServerID = serverid;
        }

        public void CreateServerCFG(string hostname, string rcon_password)
        {
            string serverConfigPath = Functions.Path.GetServerFiles(ServerID) + @"\garrysmod\cfg\server.cfg";
            
            File.Create(serverConfigPath).Dispose();

            using (TextWriter textwriter = new StreamWriter(serverConfigPath))
            {
                textwriter.WriteLine("hostname \"" + hostname + "\"");
                textwriter.WriteLine("rcon_password \"" + rcon_password + "\"");
                textwriter.WriteLine("sv_password \"\"");
                textwriter.WriteLine("net_maxfilesize \"64\"");
                textwriter.WriteLine("sv_downloadurl \"\"");
                textwriter.WriteLine("sv_loadingurl \"\"");
                textwriter.WriteLine("exec banned_user.cfg");
                textwriter.WriteLine("exec banned_ip.cfg");
            }
        }

        public void SetParameter(string ip, string port, string map, string maxplayers, string gslt, string additional)
        {
            Param = "-console -game garrysmod -ip " + ip + " -port " + port + " +map " + map + " -maxplayers " + maxplayers + " +sv_setsteamaccount " + gslt + " " + additional;
        }

        public (Process Process, string Error, string Notice) Start()
        {
            string workingDir = Functions.Path.GetServerFiles(ServerID);
            string srcdsPath = workingDir + @"\srcds.exe";

            if (!File.Exists(srcdsPath))
            {
                return (null, "srcds.exe not found (" + srcdsPath + ")", "");
            }

            if (string.IsNullOrWhiteSpace(Param))
            {
                return (null, "Start Parameter not set", "");
            }

            string serverConfigPath = workingDir + @"\garrysmod\cfg\server.cfg";
            if (!File.Exists(serverConfigPath))
            {
                return (null, "", "server.cfg not found (" + serverConfigPath + ")");
            }

            Process p = new Process();
            p.StartInfo.WorkingDirectory = workingDir;
            p.StartInfo.FileName = srcdsPath;
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
            steamCMD.SetParameter(null, null, Functions.Path.GetServerFiles(ServerID), "4020", true);

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
            steamCMD.SetParameter(null, null, Functions.Path.GetServerFiles(ServerID), "4020", false);

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