using System;
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;


namespace WindowsGSM.GameServer
{
    class RUST
    {
        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        private readonly string ServerID;

        private string Param;
        public string Error;
        public string Notice;

        public string port = "28015";
        public string defaultmap = "Procedural Map";
        public string maxplayers = "50";
        public string additional = "";

        public RUST(string serverid)
        {
            ServerID = serverid;
        }

        public void CreateServerCFG(string hostname, string rcon_password, string port)
        {
            string serverConfigPath = MainWindow.WGSM_PATH + @"\servers\" + ServerID + @"\serverfiles\server.cfg";

            File.Create(serverConfigPath).Dispose();

            using (TextWriter textwriter = new StreamWriter(serverConfigPath))
            {
                textwriter.WriteLine("+rcon.ip 0.0.0.0");
                textwriter.WriteLine("+rcon.port \"" + port + "\"");
                textwriter.WriteLine("+rcon.password \"" + rcon_password + "\"");
                textwriter.WriteLine("+rcon.web 1");
                textwriter.WriteLine("+server.tickrate 10");
                textwriter.WriteLine("+server.hostname \"" + hostname + "\"");
                textwriter.WriteLine("+server.description \"Rust Dedicated Server - Manage by WindowsGSM\\n\\nEdit \"");
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
            Param = "-batchmode +server.ip " + ip + " +server.port " + port + " +server.level \"" + map + "\" +server.maxplayers " + maxplayers + " ";

            string serverConfigPath = MainWindow.WGSM_PATH + @"\servers\" + ServerID + @"\serverfiles\server.cfg";
            if (File.Exists(serverConfigPath))
            {
                foreach (string line in File.ReadLines(serverConfigPath))
                {
                    Param += line + " ";
                }
            }

            Param.TrimEnd();
        }

        public Process Start()
        {
            string workingDir = MainWindow.WGSM_PATH + @"\servers\" + ServerID + @"\serverfiles";
            string rustPath = workingDir + @"\RustDedicated.exe";

            if (!File.Exists(rustPath))
            {
                Error = "RustDedicated.exe not found (" + rustPath + ")";
                return null;
            }

            if (string.IsNullOrWhiteSpace(Param))
            {
                Error = "Start Parameter not set";
                return null;
            }

            string serverConfigPath = MainWindow.WGSM_PATH + @"\servers\" + ServerID + @"\serverfiles\server.cfg";

            if (!File.Exists(serverConfigPath))
            {
                Notice = "server.cfg not found (" + serverConfigPath + ")";
            }

            Process p = new Process();
            p.StartInfo.WorkingDirectory = workingDir;
            p.StartInfo.FileName = rustPath;
            p.StartInfo.Arguments = Param;
            p.StartInfo.WindowStyle = ProcessWindowStyle.Minimized;
            p.Start();

            return p;
        }

        public async Task<bool> Stop(Process p)
        {
            SetForegroundWindow(p.MainWindowHandle);
            SendKeys.SendWait("quit");
            SendKeys.SendWait("{ENTER}");
            SetForegroundWindow(Process.GetCurrentProcess().MainWindowHandle);

            bool stopped = false;
            int attempt = 0;
            while (attempt < 10)
            {
                if (p != null)
                {
                    if (p.HasExited)
                    {
                        stopped = true;
                        break;
                    }
                }

                attempt++;

                await Task.Delay(1000);
            }

            return stopped;
        }

        public async Task<Process> Install()
        {
            string serverFilesPath = MainWindow.WGSM_PATH + @"\servers\" + ServerID + @"\serverfiles";

            Installer.SteamCMD steamCMD = new Installer.SteamCMD();
            steamCMD.SetParameter(null, null, serverFilesPath, "258550", true);

            if (!await steamCMD.Download())
            {
                Error = steamCMD.GetError();
                return null;
            }

            Process pSteamCMD = steamCMD.Run();
            if (pSteamCMD == null)
            {
                Error = steamCMD.GetError();
                return null;
            }

            return pSteamCMD;
        }

        public async Task<bool> Update()
        {
            string serverFilesPath = MainWindow.WGSM_PATH + @"\servers\" + ServerID + @"\serverfiles";

            Installer.SteamCMD steamCMD = new Installer.SteamCMD();
            steamCMD.SetParameter(null, null, serverFilesPath, "258550", false);

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

            if (pSteamCMD.ExitCode == 0)
            {
                return true;
            }

            Error = "Exit code: " + pSteamCMD.ExitCode.ToString();
            return false;
        }
    }
}
