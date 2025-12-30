using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.IO.Compression;
using WindowsGSM.GameServer.Data;
using WindowsGSM;

namespace WindowsGSM.Functions
{
    public static class ServerManager
    {
        public static readonly Dictionary<int, ServerMetadata> ServerMetadata = new Dictionary<int, ServerMetadata>();
        public static List<PluginMetadata> PluginsList = new List<PluginMetadata>();
        private static readonly object _serverMetadataLock = new object();

        // Events for UI updates
        public static event Action<string, string, string> OnServerStatusChanged;
        public static event Action<string, string> OnLog;

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, uint nCmdShow);
        private const uint SW_MINIMIZE = 6;

        public static void Initialize(int maxServer)
        {
            for (int i = 0; i < maxServer; i++)
            {
                ServerMetadata[i] = new ServerMetadata
                {
                    ServerStatus = ServerStatus.Stopped,
                    ServerConsole = new ServerConsole(i)
                };
            }
        }

        public static ServerMetadata GetServerMetadata(object serverId)
        {
            if (serverId == null) return null;
            
            lock (_serverMetadataLock)
            {
                return ServerMetadata.TryGetValue(int.Parse(serverId.ToString()), out var s) ? s : null;
            }
        }

        private static void Log(string serverId, string message)
        {
            OnLog?.Invoke(serverId, message);
        }

        private static void SetServerStatus(string serverId, string status, string pid = null)
        {
            OnServerStatusChanged?.Invoke(serverId, status, pid);
        }

        public static async Task StartServer(ServerTable server, string notes = "")
        {
            if (GetServerMetadata(server.ID).ServerStatus != ServerStatus.Stopped) { return; }

            string error = string.Empty;
            if (!string.IsNullOrWhiteSpace(server.IP) && !IsValidIPAddress(server.IP))
            {
                error += " IP address is not valid.";
            }

            if (!string.IsNullOrWhiteSpace(server.Port) && !IsValidPort(server.Port))
            {
                error += " Port number is not valid.";
            }

            if (error != string.Empty)
            {
                Log(server.ID, "Server: Fail to start");
                Log(server.ID, "[ERROR]" + error);
                return;
            }

            Process p = GetServerMetadata(server.ID).Process;
            if (p != null) { return; }

            ServerMetadata[int.Parse(server.ID)].ServerStatus = ServerStatus.Starting;
            Log(server.ID, "Action: Start" + notes);
            SetServerStatus(server.ID, "Starting");

            var gameServer = await BeginStartServer(server);
            if (gameServer == null)
            {
                ServerMetadata[int.Parse(server.ID)].ServerStatus = ServerStatus.Stopped;
                Log(server.ID, "Server: Fail to start");
                SetServerStatus(server.ID, "Stopped");
                return;
            }

            ServerMetadata[int.Parse(server.ID)].ServerStatus = ServerStatus.Started;
            Log(server.ID, "Server: Started");
            if (!string.IsNullOrWhiteSpace(gameServer.Notice))
            {
                Log(server.ID, "[Notice] " + gameServer.Notice);
            }
            SetServerStatus(server.ID, "Started", ServerCache.GetPID(server.ID).ToString());
        }

        public static async Task StopServer(ServerTable server)
        {
            if (GetServerMetadata(server.ID).ServerStatus == ServerStatus.Stopped) { return; }

            ServerMetadata[int.Parse(server.ID)].ServerStatus = ServerStatus.Stopping;
            Log(server.ID, "Action: Stop");
            SetServerStatus(server.ID, "Stopping");

            var gameServer = Class.Get(server.Game, pluginList: PluginsList);
            if (gameServer != null)
            {
                Process p = GetServerMetadata(server.ID).Process;
                await gameServer.Stop(p);
            }

            ServerMetadata[int.Parse(server.ID)].ServerStatus = ServerStatus.Stopped;
            Log(server.ID, "Server: Stopped");
            SetServerStatus(server.ID, "Stopped");
            ServerMetadata[int.Parse(server.ID)].Process = null;
        }

        public static async Task RestartServer(ServerTable server)
        {
            await StopServer(server);
            await StartServer(server);
        }

        public static async Task<bool> UpdateServer(ServerTable server, string notes = "", bool validate = false)
        {
            if (GetServerMetadata(server.ID).ServerStatus != ServerStatus.Stopped)
            {
                return false;
            }

            ServerMetadata[int.Parse(server.ID)].ServerStatus = ServerStatus.Updating;
            Log(server.ID, "Action: Update" + notes);
            SetServerStatus(server.ID, "Updating");

            var (p, remoteVersion, gameServer) = await BeginUpdateServer(server, silenceCheck: validate, forceUpdate: true, validate: validate);

            if (p == null && string.IsNullOrEmpty(gameServer.Error))
            {
                Log(server.ID, $"Server: Updated {(validate ? "Validate " : string.Empty)}({remoteVersion})");
            }
            else if (p != null)
            {
                await Task.Run(() => { p.WaitForExit(); });
                Log(server.ID, $"Server: Updated {(validate ? "Validate " : string.Empty)}({remoteVersion})");
            }
            else
            {
                Log(server.ID, "Server: Fail to update");
                Log(server.ID, "[ERROR] " + gameServer.Error);
            }

            ServerMetadata[int.Parse(server.ID)].ServerStatus = ServerStatus.Stopped;
            SetServerStatus(server.ID, "Stopped");

            return true;
        }

        public static async Task<bool> BackupServer(ServerTable server, string notes = "")
        {
            if (GetServerMetadata(server.ID).ServerStatus != ServerStatus.Stopped)
            {
                return false;
            }

            ServerMetadata[int.Parse(server.ID)].ServerStatus = ServerStatus.Backuping;
            Log(server.ID, "Action: Backup" + notes);
            SetServerStatus(server.ID, "Backuping");

            await EndAllRunningProcess(server.ID);
            await Task.Delay(1000);

            string backupLocation = ServerPath.GetBackups(server.ID);
            if (!Directory.Exists(backupLocation))
            {
                ServerMetadata[int.Parse(server.ID)].ServerStatus = ServerStatus.Stopped;
                Log(server.ID, "Server: Fail to backup");
                Log(server.ID, "[ERROR] Backup location not found");
                SetServerStatus(server.ID, "Stopped");
                return false;
            }

            string zipFileName = $"WGSM-Backup-Server-{server.ID}-";

            var backupConfig = new BackupConfig(server.ID);
            foreach (var fi in new DirectoryInfo(backupLocation).GetFiles("*.zip").Where(x => x.Name.Contains(zipFileName)).OrderByDescending(x => x.LastWriteTime).Skip(backupConfig.MaximumBackups - 1))
            {
                string ex = string.Empty;
                await Task.Run(() =>
                {
                    try
                    {
                        fi.Delete();
                    }
                    catch (Exception e)
                    {
                        ex = e.Message;
                    }
                });

                if (ex != string.Empty)
                {
                    ServerMetadata[int.Parse(server.ID)].ServerStatus = ServerStatus.Stopped;
                    Log(server.ID, "Server: Fail to backup");
                    Log(server.ID, $"[ERROR] {ex}");
                    SetServerStatus(server.ID, "Stopped");
                    return false;
                }
            }

            string startPath = ServerPath.GetServers(server.ID);
            string zipFile = Path.Combine(ServerPath.GetBackups(server.ID), $"{zipFileName}{DateTime.Now.ToString("yyyyMMddHHmmss")}.zip");

            string error = string.Empty;
            await Task.Run(() =>
            {
                try
                {
                    ZipFile.CreateFromDirectory(startPath, zipFile);
                }
                catch (Exception e)
                {
                    error = e.Message;
                }
            });

            if (error != string.Empty)
            {
                ServerMetadata[int.Parse(server.ID)].ServerStatus = ServerStatus.Stopped;
                Log(server.ID, "Server: Fail to backup");
                Log(server.ID, $"[ERROR] {error}");
                SetServerStatus(server.ID, "Stopped");

                return false;
            }

            ServerMetadata[int.Parse(server.ID)].ServerStatus = ServerStatus.Stopped;
            Log(server.ID, "Server: Backuped");
            SetServerStatus(server.ID, "Stopped");

            return true;
        }

        public static async Task<bool> RestoreBackup(ServerTable server, string backupFile)
        {
            if (GetServerMetadata(server.ID).ServerStatus != ServerStatus.Stopped)
            {
                return false;
            }

            string backupLocation = ServerPath.GetBackups(server.ID);
            string backupPath = Path.Combine(backupLocation, backupFile);
            if (!File.Exists(backupPath))
            {
                Log(server.ID, "Server: Fail to restore backup");
                Log(server.ID, "[ERROR] Backup not found");
                return false;
            }

            ServerMetadata[int.Parse(server.ID)].ServerStatus = ServerStatus.Restoring;
            Log(server.ID, "Action: Restore Backup");
            SetServerStatus(server.ID, "Restoring");

            string extractPath = ServerPath.GetServers(server.ID);
            if (Directory.Exists(extractPath))
            {
                string ex = string.Empty;
                await Task.Run(() =>
                {
                    try
                    {
                        Directory.Delete(extractPath, true);
                    }
                    catch (Exception e)
                    {
                        ex = e.Message;
                    }
                });

                if (ex != string.Empty)
                {
                    ServerMetadata[int.Parse(server.ID)].ServerStatus = ServerStatus.Stopped;
                    Log(server.ID, "Server: Fail to restore backup");
                    Log(server.ID, $"[ERROR] {ex}");
                    SetServerStatus(server.ID, "Stopped");
                    return false;
                }
            }

            string error = string.Empty;
            await Task.Run(() =>
            {
                try
                {
                    ZipFile.ExtractToDirectory(backupPath, extractPath);
                }
                catch (Exception e)
                {
                    error = e.Message;
                }
            });

            if (error != string.Empty)
            {
                ServerMetadata[int.Parse(server.ID)].ServerStatus = ServerStatus.Stopped;
                Log(server.ID, "Server: Fail to restore backup");
                Log(server.ID, $"[ERROR] {error}");
                SetServerStatus(server.ID, "Stopped");
                return false;
            }

            ServerMetadata[int.Parse(server.ID)].ServerStatus = ServerStatus.Stopped;
            Log(server.ID, "Server: Restored");
            SetServerStatus(server.ID, "Stopped");

            return true;
        }

        public static async Task<bool> DeleteServer(ServerTable server)
        {
            if (GetServerMetadata(server.ID).ServerStatus != ServerStatus.Stopped)
            {
                return false;
            }

            ServerMetadata[int.Parse(server.ID)].ServerStatus = ServerStatus.Deleting;
            Log(server.ID, "Action: Delete");
            SetServerStatus(server.ID, "Deleting");

            var firewall = new WindowsFirewall(null, ServerPath.GetServers(server.ID));
            firewall.RemoveRuleEx();

            await EndAllRunningProcess(server.ID);
            await Task.Delay(1000);

            string serverPath = ServerPath.GetServers(server.ID);

            await Task.Run(() =>
            {
                try
                {
                    if (Directory.Exists(serverPath))
                    {
                        Directory.Delete(serverPath, true);
                    }
                }
                catch
                {

                }
            });

            await Task.Delay(1000);

            if (Directory.Exists(serverPath))
            {
                string wgsmCfgPath = ServerPath.GetServersConfigs(server.ID, "WindowsGSM.cfg");
                if (File.Exists(wgsmCfgPath))
                {
                    Log(server.ID, "Server: Fail to delete server");
                    Log(server.ID, "[ERROR] Directory is not accessible");

                    ServerMetadata[int.Parse(server.ID)].ServerStatus = ServerStatus.Stopped;
                    SetServerStatus(server.ID, "Stopped");

                    return false;
                }
            }

            Log(server.ID, "Server: Deleted server");

            ServerMetadata[int.Parse(server.ID)].ServerStatus = ServerStatus.Stopped;
            SetServerStatus(server.ID, "Stopped");

            return true;
        }

        private static async Task<dynamic> BeginStartServer(ServerTable server)
        {
            dynamic gameServer = Class.Get(server.Game, new ServerConfig(server.ID), PluginsList);
            if (gameServer == null) { return null; }

            await EndAllRunningProcess(server.ID);
            await Task.Delay(500);

            string startPath = ServerPath.GetServersServerFiles(server.ID, gameServer.StartPath);
            if (!string.IsNullOrWhiteSpace(gameServer.StartPath))
            {
                WindowsFirewall firewall = new WindowsFirewall(Path.GetFileName(startPath), startPath);
                if (!await firewall.IsRuleExist())
                {
                    await firewall.AddRule();
                }
            }

            gameServer.AllowsEmbedConsole = GetServerMetadata(server.ID).EmbedConsole;
            Process p = await gameServer.Start();

            if (p == null)
            {
                ServerMetadata[int.Parse(server.ID)].ServerStatus = ServerStatus.Stopped;
                Log(server.ID, "Server: Fail to start");
                Log(server.ID, "[ERROR] " + gameServer.Error);
                SetServerStatus(server.ID, "Stopped");
                return null;
            }

            ServerMetadata[int.Parse(server.ID)].Process = p;
            p.Exited += (sender, e) => OnGameServerExited(server);

            await Task.Run(() =>
            {
                try
                {
                    if (!p.StartInfo.CreateNoWindow)
                    {
                        while (!p.HasExited && !ShowWindow(p.MainWindowHandle, SW_MINIMIZE))
                        {
                            Task.Delay(1000).Wait();
                        }

                        // Save MainWindow handle
                        ServerMetadata[int.Parse(server.ID)].MainWindow = p.MainWindowHandle;
                    }
                    
                    p.WaitForInputIdle();
                }
                catch
                {
                    // ignore
                }
            });

            return gameServer;
        }

        private static async Task<(Process, string, dynamic)> BeginUpdateServer(ServerTable server, bool silenceCheck, bool forceUpdate, bool validate = false, string custum = null)
        {
            dynamic gameServer = Class.Get(server.Game, new ServerConfig(server.ID), PluginsList);

            string localVersion = gameServer.GetLocalBuild();
            if (string.IsNullOrWhiteSpace(localVersion) && !silenceCheck)
            {
                Log(server.ID, $"[NOTICE] {gameServer.Error}");
            }

            string remoteVersion = await gameServer.GetRemoteBuild();
            if (string.IsNullOrWhiteSpace(remoteVersion) && !silenceCheck)
            {
                Log(server.ID, $"[NOTICE] {gameServer.Error}");
            }

            if (!silenceCheck)
            {
                Log(server.ID, $"Checking: Version ({localVersion}) => ({remoteVersion})");
            }

            if ((!string.IsNullOrWhiteSpace(localVersion) && !string.IsNullOrWhiteSpace(remoteVersion) && localVersion != remoteVersion) || forceUpdate)
            {
                try
                {
                    return (await gameServer.Update(validate, custum), remoteVersion, gameServer);
                }
                catch
                {
                    return (await gameServer.Update(), remoteVersion, gameServer);
                }
            }

            return (null, remoteVersion, gameServer);
        }

        private static async void OnGameServerExited(ServerTable server)
        {
            Process p = GetServerMetadata(server.ID).Process;
            if (p != null)
            {
                p.Exited -= (sender, e) => OnGameServerExited(server);
                ServerMetadata[int.Parse(server.ID)].Process = null;
                p.Dispose();
            }

            if (GetServerMetadata(server.ID).ServerStatus == ServerStatus.Stopping)
            {
                return;
            }

            // Crash detected
            ServerMetadata[int.Parse(server.ID)].ServerStatus = ServerStatus.Stopped;
            Log(server.ID, "Server: Stopped (Crashed)");
            SetServerStatus(server.ID, "Stopped");

            var metadata = GetServerMetadata(server.ID);
            if (metadata.DiscordAlert && metadata.CrashAlert)
            {
                var webhook = new DiscordWebhook(metadata.DiscordWebhook, metadata.DiscordMessage); 
                await webhook.Send(server.ID, server.Game, "Crashed", server.Name, server.IP, server.Port);
            }

            if (metadata.AutoRestart)
            {
                Log(server.ID, "Server: Restarting...");
                await StartServer(server, " | Restart on Crash");
            }
        }

        private static async Task EndAllRunningProcess(string serverId)
        {
            await Task.Run(() =>
            {
                var processes = (from p in Process.GetProcesses()
                                 where ((Predicate<Process>)(p_ =>
                                 {
                                     try
                                     {
                                         return p_.MainModule.FileName.Contains(Path.Combine(ServerPath.WGSM_PATH, "servers", serverId) + "\\");
                                     }
                                     catch
                                     {
                                         return false;
                                     }
                                 }))(p)
                                 select p).ToList();

                foreach (var process in processes)
                {
                    try
                    {
                        process.Kill();
                    }
                    catch
                    {
                        //ignore
                    }
                }
            });
        }

        private static bool IsValidIPAddress(string ip)
        {
            if (string.IsNullOrWhiteSpace(ip)) { return false; }
            string[] splitValues = ip.Split('.');
            if (splitValues.Length != 4) { return false; }
            return splitValues.All(r => byte.TryParse(r, out byte tempForParsing));
        }

        private static bool IsValidPort(string port)
        {
            if (string.IsNullOrWhiteSpace(port)) { return false; }
            return ushort.TryParse(port, out ushort _);
        }
    }
}
