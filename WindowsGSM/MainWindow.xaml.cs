using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.IO;
using System.IO.Compression;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.Win32;
using MahApps.Metro;
using MahApps.Metro.Controls;

namespace WindowsGSM
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow
    {
        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, WindowShowStyle nCmdShow);

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        private enum WindowShowStyle : uint
        {
            Hide = 0,
            ShowNormal = 1,
            Show = 5
        }

        private enum ServerStatus
        {
            Started = 0,
            Starting = 1,
            Stopped = 2,
            Stopping = 3,
            Restarted = 4,
            Restarting = 5,
            Updated = 6,
            Updating = 7,
            Backuped = 8,
            Backuping = 9,
            Restored = 10,
            Restoring = 11
        }

        public static readonly string WGSM_VERSION = "v1.0.4";
        public static readonly int MAX_SERVER = 100;
        public static readonly string WGSM_PATH = Process.GetCurrentProcess().MainModule.FileName.Replace(@"\WindowsGSM.exe", "");
        //public static readonly string WGSM_PATH = @"D:\WindowsGSMtest2";

        private Install InstallWindow;
        private Import ImportWindow;

        private static readonly ServerStatus[] g_iServerStatus = new ServerStatus[MAX_SERVER + 1];

        private static readonly Process[] g_Process = new Process[MAX_SERVER + 1];

        private static readonly string[] g_SteamGSLT = new string[MAX_SERVER + 1];
        private static readonly string[] g_AdditionalParam = new string[MAX_SERVER + 1];

        private static readonly bool[] g_bAutoRestart = new bool[MAX_SERVER + 1];
        private static readonly bool[] g_bUpdateOnStart = new bool[MAX_SERVER + 1];

        private static readonly bool[] g_bDiscordAlert = new bool[MAX_SERVER + 1];
        private static readonly string[] g_DiscordWebhook = new string[MAX_SERVER + 1];

        public MainWindow()
        {
            InitializeComponent();

            Title = "WindowsGSM " + WGSM_VERSION;

            RegistryKey key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\WindowsGSM");
            if (key == null)
            {
                key = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\WindowsGSM");
                key.SetValue("HardWareAcceleration", "True");
                key.SetValue("UIAnimation", "True");
                key.SetValue("DarkTheme", "False");

                MahAppSwitch_HardWareAcceleration.IsChecked = true;
                MahAppSwitch_UIAnimation.IsChecked = true;
                MahAppSwitch_DarkTheme.IsChecked = false;
            }
            else
            {
                MahAppSwitch_HardWareAcceleration.IsChecked = ((key.GetValue("HardWareAcceleration") ?? false).ToString() == "True") ? true : false;
                MahAppSwitch_UIAnimation.IsChecked = ((key.GetValue("UIAnimation") ?? false).ToString() == "True") ? true : false;
                MahAppSwitch_DarkTheme.IsChecked = ((key.GetValue("DarkTheme") ?? false).ToString() == "True") ? true : false;
            }
            key.Close();

            RenderOptions.ProcessRenderMode = (MahAppSwitch_HardWareAcceleration.IsChecked ?? false) ? System.Windows.Interop.RenderMode.SoftwareOnly : System.Windows.Interop.RenderMode.Default;
            WindowTransitionsEnabled = MahAppSwitch_UIAnimation.IsChecked ?? false;
            ThemeManager.ChangeAppTheme(App.Current, (MahAppSwitch_DarkTheme.IsChecked ?? false) ? "BaseDark" : "BaseLight");

            //Set All server status to stopped
            for (int i = 0; i < MAX_SERVER; i++)
            {
                g_iServerStatus[i] = ServerStatus.Stopped;
            }

            LoadServerTable();

            if (ServerGrid.Items.Count > 0)
            {
                ServerGrid.SelectedItem = ServerGrid.Items[0];
            }
        }

        private void RefreshServerList_Click(object sender, RoutedEventArgs e)
        {
            LoadServerTable();
        }

        public void LoadServerTable()
        {
            Table selectedrow = (Table)ServerGrid.SelectedItem;

            int num_row = ServerGrid.Items.Count;
            for (int i = 0; i < num_row; i++)
            {
                ServerGrid.Items.RemoveAt(0);
            }

            //Add server to datagrid
            for (int i = 1; i <= MAX_SERVER; i++)
            {
                string serverid_path = WGSM_PATH + @"\servers\" + i.ToString();
                if (!Directory.Exists(serverid_path))
                {
                    continue;
                }

                Functions.ServerConfig serverConfig = new Functions.ServerConfig(i.ToString());

                if (!serverConfig.IsWindowsGSMConfigExist())
                {
                    continue;
                }

                string status;
                switch (g_iServerStatus[i])
                {
                    case ServerStatus.Started: status = "Started"; break;
                    case ServerStatus.Starting: status = "Starting"; break;
                    case ServerStatus.Stopped: status = "Stopped"; break;
                    case ServerStatus.Stopping: status = "Stopping"; break;
                    case ServerStatus.Restarted: status = "Restarted"; break;
                    case ServerStatus.Restarting: status = "Restarting"; break;
                    case ServerStatus.Updated: status = "Updated"; break;
                    case ServerStatus.Updating: status = "Updating"; break;
                    case ServerStatus.Backuped: status = "Backuped"; break;
                    case ServerStatus.Backuping: status = "Backuping"; break;
                    case ServerStatus.Restored: status = "Restored"; break;
                    case ServerStatus.Restoring: status = "Restoring"; break;
                    default: status = ""; break;
                }

                Table row = new Table
                {
                    ID = i.ToString(),
                    Game = serverConfig.ServerGame,
                    Icon = Images.ServerIcon.ResourceManager.GetString(serverConfig.ServerGame),
                    Status = status,
                    Name = serverConfig.ServerName,
                    IP = serverConfig.ServerIP,
                    Port = serverConfig.ServerPort,
                    Defaultmap = serverConfig.ServerMap,
                    Maxplayers = serverConfig.ServerMaxPlayer
                };

                ServerGrid.Items.Add(row);  

                if (selectedrow != null)
                {
                    if (selectedrow.ID == row.ID)
                    {
                        ServerGrid.SelectedItem = row;
                    }
                }

                g_SteamGSLT[i] = serverConfig.ServerGSLT;
                g_AdditionalParam[i] = serverConfig.ServerParam;
                g_bAutoRestart[i] = serverConfig.AutoRestart;
                g_bUpdateOnStart[i] = serverConfig.UpdateOnStart;
                g_bDiscordAlert[i] = serverConfig.DiscordAlert;
                g_DiscordWebhook[i] = serverConfig.DiscordWebhook;
            }

            grid_action.Visibility = (ServerGrid.Items.Count != 0) ? Visibility.Visible : Visibility.Hidden;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            bool hasServerRunning = false;
            for (int i = 0; i <= MAX_SERVER; i++)
            {
                if (g_Process[i] != null)
                {
                    if (!g_Process[i].HasExited)
                    {
                        hasServerRunning = true;

                        break;
                    }
                }
            }

            if (!hasServerRunning)
            {
                return;
            }

            MessageBoxResult result = System.Windows.MessageBox.Show("Are you sure to quit?\n(All game servers will be stopped)", "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result != MessageBoxResult.Yes)
            {
                e.Cancel = true;

                return;
            }

            for (int i = 0; i <= MAX_SERVER; i++)
            {
                if (g_Process[i] == null)
                {
                    continue;
                }

                if (!g_Process[i].HasExited)
                {
                    SetForegroundWindow(g_Process[i].MainWindowHandle);
                    SendKeys.SendWait("stop");
                    SendKeys.SendWait("{ENTER}");
                    SendKeys.SendWait("{ENTER}");

                    if (!g_Process[i].HasExited)
                    {
                        g_Process[i].Kill();
                    }
                }
            }
        }

        private void DataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Table row = (Table)ServerGrid.SelectedItem;

            if (row != null)
            {
                if (g_iServerStatus[Int32.Parse(row.ID)] == ServerStatus.Stopped)
                {
                    button_Start.IsEnabled = true;
                    button_Stop.IsEnabled = false;
                    button_Restart.IsEnabled = false;
                    button_Console.IsEnabled = false;
                    button_Update.IsEnabled = true;
                    button_Backup.IsEnabled = true;

                    textbox_servercommand.IsEnabled = false;
                    button_servercommand.IsEnabled = false;
                }
                else if (g_iServerStatus[Int32.Parse(row.ID)] == ServerStatus.Started)
                {
                    button_Start.IsEnabled = false;
                    button_Stop.IsEnabled = true;
                    button_Restart.IsEnabled = true;
                    button_Console.IsEnabled = true;
                    button_Update.IsEnabled = false;
                    button_Backup.IsEnabled = false;

                    textbox_servercommand.IsEnabled = true;
                    button_servercommand.IsEnabled = true;
                }
                else
                {
                    button_Start.IsEnabled = false;
                    button_Stop.IsEnabled = false;
                    button_Restart.IsEnabled = false;
                    button_Console.IsEnabled = false;
                    button_Update.IsEnabled = false;
                    button_Backup.IsEnabled = false;

                    textbox_servercommand.IsEnabled = false;
                    button_servercommand.IsEnabled = false;
                }

                button_Status.Content = row.Status.ToUpper();
                button_Status.Background = (g_iServerStatus[Int32.Parse(row.ID)] == ServerStatus.Started) ? Brushes.LimeGreen : Brushes.Orange;
                textBox_ServerGame.Text = row.Game;

                button_autorestart.Content = (g_bAutoRestart[Int32.Parse(row.ID)]) ? "TRUE" : "FALSE";
                button_autorestart.Background = (g_bAutoRestart[Int32.Parse(row.ID)]) ? Brushes.LimeGreen : Brushes.Red;

                button_updateonstart.Content = (g_bUpdateOnStart[Int32.Parse(row.ID)]) ? "TRUE" : "FALSE";
                button_updateonstart.Background = (g_bUpdateOnStart[Int32.Parse(row.ID)]) ? Brushes.LimeGreen : Brushes.Red;

                button_discordalert.Content = (g_bDiscordAlert[Int32.Parse(row.ID)]) ? "TRUE" : "FALSE";
                button_discordalert.Background = (g_bDiscordAlert[Int32.Parse(row.ID)]) ? Brushes.LimeGreen : Brushes.Red;

                button_discordtest.IsEnabled = (g_bDiscordAlert[Int32.Parse(row.ID)]) ? true : false;
            }
        }

        private void Install_Click(object sender, RoutedEventArgs e)
        {
            if (InstallWindow == null && ImportWindow == null)
            {
                InstallWindow = new Install();
                InstallWindow.Closed += new EventHandler(InstallWindow_Closed);

                //Add games to ComboBox
                int i = 0;
                string servergame = "";
                while (servergame != null)
                {
                    servergame = GameServerList.ResourceManager.GetString((++i).ToString());
                    if (servergame == null)
                    {
                        break;
                    }

                    var row = new Images.Row { Image = Images.ServerIcon.ResourceManager.GetString(servergame), Name = servergame };
                    InstallWindow.comboBox.Items.Add(row);
                }
            }
            else
            {
                if (InstallWindow != null)
                {
                    InstallWindow.Activate();
                    InstallWindow.WindowState = WindowState.Normal;
                }
                else if (ImportWindow != null)
                {
                    ImportWindow.Activate();
                    ImportWindow.WindowState = WindowState.Normal;
                }
            }
        }

        private void InstallWindow_Closed(object sender, EventArgs e)
        {
            InstallWindow = null;
        }

        private void Import_Click(object sender, RoutedEventArgs e)
        {
            if (InstallWindow == null && ImportWindow == null)
            {
                ImportWindow = new Import();
                ImportWindow.Closed += new EventHandler(ImportWindow_Closed);

                //Add games to ComboBox
                int i = 0;
                string servergame = "";
                while (servergame != null)
                {
                    servergame = GameServerList.ResourceManager.GetString((++i).ToString());
                    if (servergame == null)
                    {
                        break;
                    }

                    var row = new Images.Row { Image = Images.ServerIcon.ResourceManager.GetString(servergame), Name = servergame };
                    ImportWindow.comboBox.Items.Add(row);
                }
            }
            else
            {
                if (InstallWindow != null)
                {
                    InstallWindow.Activate();
                    InstallWindow.WindowState = WindowState.Normal;
                }
                else if (ImportWindow != null)
                {
                    ImportWindow.Activate();
                    ImportWindow.WindowState = WindowState.Normal;
                }
            }
        }

        private void ImportWindow_Closed(object sender, EventArgs e)
        {
            ImportWindow = null;
        }

        private async void Delete_Click(object sender, RoutedEventArgs e)
        {
            Table server = (Table)ServerGrid.SelectedItem;
            if (server == null)
            {
                return;
            }

            if (g_iServerStatus[Int32.Parse(server.ID)] != ServerStatus.Stopped)
            {
                return;
            }

            MessageBoxResult result = System.Windows.MessageBox.Show("Do you want to delete this server?\n(There is no comeback)", "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result != MessageBoxResult.Yes)
            {
                return;
            }

            await GameServer_Delete(server);
        }

        private async void Button_DiscordWebhookTest_Click(object sender, RoutedEventArgs e)
        {
           Table row = (Table)ServerGrid.SelectedItem;
            if (row == null)
            {
                return;
            }

            if (!g_bDiscordAlert[Int32.Parse(row.ID)])
            {
                return;
            }

            Functions.Discord.Webhook webhook = new Functions.Discord.Webhook(g_DiscordWebhook[Int32.Parse(row.ID)]);
            await webhook.Send(row.ID, row.Game, "Webhook Test Alert", row.Name, row.IP, row.Port);
        }

        private void Button_ServerCommand_Click(object sender, RoutedEventArgs e)
        {
            string command = textbox_servercommand.Text;
            textbox_servercommand.Text = "";

            if (string.IsNullOrWhiteSpace(command))
            {
                return;
            }

            Table server = (Table)ServerGrid.SelectedItem;
            if (server == null)
            {
                return;
            }

            SendCommand(server, command);
        }

        private void Textbox_ServerCommand_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                Button_ServerCommand_Click(this, new RoutedEventArgs());
            }
        }

        private void Actions_Start_Click(object sender, RoutedEventArgs e)
        {
            Table server = (Table)ServerGrid.SelectedItem;
            if (server == null)
            {
                return;
            }

            GameServer_Start(server);
        }

        private void Actions_Stop_Click(object sender, RoutedEventArgs e)
        {
            Table server = (Table)ServerGrid.SelectedItem;
            if (server == null)
            {
                return;
            }

            GameServer_Stop(server);
        }

        private void Actions_Restart_Click(object sender, RoutedEventArgs e)
        {
            Table server = (Table)ServerGrid.SelectedItem;
            if (server == null)
            {
                return;
            }

            GameServer_Restart(server);
        }

        private void Actions_ToggleConsole_Click(object sender, RoutedEventArgs e)
        {
            Table row = (Table)ServerGrid.SelectedItem;
            if (row == null)
            {
                return;
            }

            string serverid = row.ID.ToString();

            Process p = g_Process[Int32.Parse(serverid)];
            if (p == null)
            {
                return;
            }

            IntPtr hWnd = p.MainWindowHandle;
            ShowWindow(hWnd, (ShowWindow(hWnd, WindowShowStyle.Hide)) ? (WindowShowStyle.Hide) : (WindowShowStyle.ShowNormal));
        }

        private async void Actions_Update_Click(object sender, RoutedEventArgs e)
        {
            Table server = (Table)ServerGrid.SelectedItem;
            if (server == null)
            {
                return;
            }

            if (g_iServerStatus[Int32.Parse(server.ID)] != ServerStatus.Stopped)
            {
                return;
            }

            MessageBoxResult result = System.Windows.MessageBox.Show("Do you want to update this server?", "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result != MessageBoxResult.Yes)
            {
                return;
            }

            await GameServer_Update(server);
        }

        private async void Actions_Backup_Click(object sender, RoutedEventArgs e)
        {
            Table server = (Table)ServerGrid.SelectedItem;
            if (server == null)
            {
                return;
            }

            if (g_iServerStatus[Int32.Parse(server.ID)] != ServerStatus.Stopped)
            {
                return;
            }

            MessageBoxResult result = System.Windows.MessageBox.Show("Do you want to backup on this server?", "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result != MessageBoxResult.Yes)
            {
                return;
            }

            await GameServer_Backup(server);
        }

        private async void Actions_RestoreBackup_Click(object sender, RoutedEventArgs e)
        {
            Table server = (Table)ServerGrid.SelectedItem;
            if (server == null)
            {
                return;
            }

            if (g_iServerStatus[Int32.Parse(server.ID)] != ServerStatus.Stopped)
            {
                return;
            }

            MessageBoxResult result = System.Windows.MessageBox.Show("Do you want to restore backup on this server?\n(All server files will be overwritten)", "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result != MessageBoxResult.Yes)
            {
                return;
            }

            await GameServer_RestoreBackup(server);
        }

        private async void GameServer_Start(Table server)
        {
            if (g_iServerStatus[Int32.Parse(server.ID)] != ServerStatus.Stopped)
            {
                return;
            }

            string error = "";
            if (!IsValidIPAddress(server.IP))
            {
                error += " IP address is not valid.";
            }

            if (!IsValidPort(server.Port))
            {
                error += " Port number is not valid.";
            }

            if (error != "")
            {
                Log(server.ID, "Server: Fail to start [ERROR]" + error);
                return;
            }

            if (g_Process[Int32.Parse(server.ID)] != null)
            {
                return;
            }

            Process p = g_Process[Int32.Parse(server.ID)];
            if (p != null)
            {
                return;
            }

            if (g_bUpdateOnStart[Int32.Parse(server.ID)])
            {
                await GameServer_Update(server);
            }

            //Begin Start
            g_iServerStatus[Int32.Parse(server.ID)] = ServerStatus.Starting;
            Log(server.ID, "Action: Start");
            SetServerStatus(server, "Starting");

            GameServerAction gameServerAction = new GameServerAction(server, g_SteamGSLT[Int32.Parse(server.ID)], g_AdditionalParam[Int32.Parse(server.ID)]);
            p = gameServerAction.Start();
            
            Activate();

            //Fail to start
            if (p == null)
            {
                g_iServerStatus[Int32.Parse(server.ID)] = ServerStatus.Stopped;
                Log(server.ID, "Server: Fail to start [ERROR] " + gameServerAction.Error);
                SetServerStatus(server, "Stopped");
                return;
            }

            g_Process[Int32.Parse(server.ID)] = p;

            if (p.MainWindowHandle != IntPtr.Zero)
            {
                await Task.Run(() => p.WaitForInputIdle());
            }
            else
            {
                await Task.Delay(1000);
            }

            //An error may occur on ShowWindow if not adding this 
            if (p.HasExited)
            {
                g_Process[Int32.Parse(server.ID)] = null;

                g_iServerStatus[Int32.Parse(server.ID)] = ServerStatus.Stopped;
                Log(server.ID, "Server: Fail to start [ERROR] " + gameServerAction.Error);
                SetServerStatus(server, "Stopped");

                return;
            }

            ShowWindow(p.MainWindowHandle, WindowShowStyle.Hide);

            g_iServerStatus[Int32.Parse(server.ID)] = ServerStatus.Started;
            Log(server.ID, "Server: Started");
            SetServerStatus(server, "Started");

            if (g_bDiscordAlert[Int32.Parse(server.ID)])
            {
                Functions.Discord.Webhook webhook = new Functions.Discord.Webhook(g_DiscordWebhook[Int32.Parse(server.ID)]);
                await webhook.Send(server.ID, server.Game, "Started", server.Name, server.IP, server.Port);
            }

            StartServerCrashDetector(server);
        }

        private async void GameServer_Stop(Table server)
        {
            if (g_iServerStatus[Int32.Parse(server.ID)] != ServerStatus.Started)
            {
                return;
            }

            Process p = g_Process[Int32.Parse(server.ID)];
            if (p == null)
            {
                return;
            }

            //Begin stop
            g_iServerStatus[Int32.Parse(server.ID)] = ServerStatus.Stopping;
            Log(server.ID, "Action: Stop");
            SetServerStatus(server, "Stopping");

            g_Process[Int32.Parse(server.ID)] = null;

            GameServerAction gameServerAction = new GameServerAction(server);
            if (await gameServerAction.Stop(p))
            {
                Log(server.ID, "Server: Stopped");
            }
            else
            {
                Log(server.ID, "Server: Stopped [NOTICE] Server fail to stop peacefully");
            }

            g_iServerStatus[Int32.Parse(server.ID)] = ServerStatus.Stopped;
            SetServerStatus(server, "Stopped");

            if (g_bDiscordAlert[Int32.Parse(server.ID)])
            {
                Functions.Discord.Webhook webhook = new Functions.Discord.Webhook(g_DiscordWebhook[Int32.Parse(server.ID)]);
                await webhook.Send(server.ID, server.Game, "Stopped", server.Name, server.IP, server.Port);
            }
        }

        private async void GameServer_Restart(Table server)
        {
            if (g_iServerStatus[Int32.Parse(server.ID)] != ServerStatus.Started)
            {
                return;
            }

            Process p = g_Process[Int32.Parse(server.ID)];
            if (p == null)
            {
                return;
            }

            g_Process[Int32.Parse(server.ID)] = null;

            //Begin Restart
            g_iServerStatus[Int32.Parse(server.ID)] = ServerStatus.Restarting;
            Log(server.ID, "Action: Restart");
            SetServerStatus(server, "Restarting");

            GameServerAction gameServerAction = new GameServerAction(server, g_SteamGSLT[Int32.Parse(server.ID)], g_AdditionalParam[Int32.Parse(server.ID)]);
            p = await gameServerAction.Restart(p);

            Activate();

            //Fail to start
            if (p == null)
            {
                g_iServerStatus[Int32.Parse(server.ID)] = ServerStatus.Stopped;
                Log(server.ID, "Server: Fail to restart [ERROR] " + gameServerAction.Error);
                SetServerStatus(server, "Stopped");

                return;
            }

            g_Process[Int32.Parse(server.ID)] = p;

            if (p.MainWindowHandle != IntPtr.Zero)
            {
                await Task.Run(() => p.WaitForInputIdle());
            }
            else
            {
                await Task.Delay(1000);
            }

            //An error may occur on ShowWindow if not adding this 
            if (p.HasExited)
            {
                g_Process[Int32.Parse(server.ID)] = null;

                g_iServerStatus[Int32.Parse(server.ID)] = ServerStatus.Stopped;
                Log(server.ID, "Server: Fail to restart [ERROR] " + gameServerAction.Error);
                SetServerStatus(server, "Stopped");

                return;
            }

            ShowWindow(p.MainWindowHandle, WindowShowStyle.Hide);

            g_iServerStatus[Int32.Parse(server.ID)] = (int)ServerStatus.Started;
            Log(server.ID, "Server: Restarted");
            SetServerStatus(server, "Started");

            if (g_bDiscordAlert[Int32.Parse(server.ID)])
            {
                Functions.Discord.Webhook webhook = new Functions.Discord.Webhook(g_DiscordWebhook[Int32.Parse(server.ID)]);
                await webhook.Send(server.ID, server.Game, "Restarted", server.Name, server.IP, server.Port);
            }

            StartServerCrashDetector(server);
        }

        private async Task<bool> GameServer_Update(Table server)
        {
            if (g_iServerStatus[Int32.Parse(server.ID)] != ServerStatus.Stopped)
            {
                return false;
            }

            //Begin Update
            g_iServerStatus[Int32.Parse(server.ID)] = ServerStatus.Updating;
            Log(server.ID, "Action: Update");
            SetServerStatus(server, "Updating");

            GameServerAction gameServerAction = new GameServerAction(server);
            bool updated = await gameServerAction.Update();

            Activate();

            if (updated)
            {
                Log(server.ID, "Server: Updated");
            }
            else
            {
                Log(server.ID, "Server: Fail to update [ERROR] " + gameServerAction.Error);
            }

            g_iServerStatus[Int32.Parse(server.ID)] = ServerStatus.Stopped;
            SetServerStatus(server, "Stopped");

            return true;
        }

        private async Task<bool> GameServer_Backup(Table server)
        {
            if (g_iServerStatus[Int32.Parse(server.ID)] != ServerStatus.Stopped)
            {
                return false;
            }

            //Begin backup
            g_iServerStatus[Int32.Parse(server.ID)] = ServerStatus.Backuping;
            Log(server.ID, "Action: Backup");
            SetServerStatus(server, "Backuping");

            string startPath = WGSM_PATH + @"\servers\" + server.ID;
            string zipPath = WGSM_PATH + @"\backups\" + server.ID;
            string zipFile = zipPath + @"\backup-id-" + server.ID + ".zip";

            if (!Directory.Exists(zipPath))
            {
                Directory.CreateDirectory(zipPath);
            }

            if (File.Exists(zipFile))
            {
                await Task.Run(() =>
                {
                    try
                    {
                        File.Delete(zipFile);
                    }
                    catch
                    {

                    }
                });

                if (File.Exists(zipFile))
                {
                    g_iServerStatus[Int32.Parse(server.ID)] = ServerStatus.Stopped;
                    Log(server.ID, "Server: Fail to backup [ERROR] Fail to delete old backup");
                    SetServerStatus(server, "Stopped");

                    return false;
                }
            }

            await Task.Run(() => ZipFile.CreateFromDirectory(startPath, zipFile));

            if (!File.Exists(zipFile))
            {
                g_iServerStatus[Int32.Parse(server.ID)] = ServerStatus.Stopped;
                Log(server.ID, "Server: Fail to backup [ERROR] Cannot create zipfile");
                SetServerStatus(server, "Stopped");

                return false;
            }

            g_iServerStatus[Int32.Parse(server.ID)] = ServerStatus.Stopped;
            Log(server.ID, "Server: Backuped");
            SetServerStatus(server, "Stopped");

            return true;
        }

        private async Task<bool> GameServer_RestoreBackup(Table server)
        {
            if (g_iServerStatus[Int32.Parse(server.ID)] != ServerStatus.Stopped)
            {
                return false;
            }

            string zipFile = WGSM_PATH + @"\backups\" + server.ID + @"\backup-id-" + server.ID + ".zip";
            string extractPath = WGSM_PATH + @"\servers\" + server.ID;

            if (!File.Exists(zipFile))
            {
                Log(server.ID, "Server: Fail to restore backup [ERROR] Backup not found");

                return false;
            }

            if (Directory.Exists(extractPath))
            {
                await Task.Run(() =>
                {
                    try
                    {
                        Directory.Delete(extractPath, true);
                    }
                    catch
                    {

                    }
                });

                if (Directory.Exists(extractPath))
                {
                    Log(server.ID, "Server: Fail to restore backup [ERROR] Extract path is not accessible");

                    return false;
                }
            }

            //Begin backup
            g_iServerStatus[Int32.Parse(server.ID)] = ServerStatus.Restoring;
            Log(server.ID, "Action: Restore Backup");
            SetServerStatus(server, "Restoring");

            await Task.Run(() => ZipFile.ExtractToDirectory(zipFile, extractPath));

            g_iServerStatus[Int32.Parse(server.ID)] = ServerStatus.Stopped;
            Log(server.ID, "Server: Restored");
            SetServerStatus(server, "Stopped");

            return true;
        }

        private async Task<bool> GameServer_Delete(Table server)
        {
            if (g_iServerStatus[Int32.Parse(server.ID)] != ServerStatus.Stopped)
            {
                return false;
            }

            string serverPath = WGSM_PATH + @"\servers\" + server.ID;
            if (Directory.Exists(serverPath))
            {
                await Task.Run(() =>
                {
                    try
                    {
                        Directory.Delete(serverPath, true);
                    }
                    catch
                    {

                    }
                });

                if (Directory.Exists(serverPath))
                {
                    Log(server.ID, "Server: Fail to delete server [ERROR] Directory is not accessible");

                    return false;
                }
            }

            Log(server.ID, "Server: Deleted server");

            LoadServerTable();

            return true;
        }

        private async void StartServerCrashDetector(Table server)
        {
            if (await IsServerCrashed(server.ID))
            {
                g_iServerStatus[Int32.Parse(server.ID)] = ServerStatus.Stopped;
                Log(server.ID, "Server: Crashed [WARNING] Server crashed");
                SetServerStatus(server, "Stopped");

                if (g_bDiscordAlert[Int32.Parse(server.ID)])
                {
                    Functions.Discord.Webhook webhook = new Functions.Discord.Webhook(g_DiscordWebhook[Int32.Parse(server.ID)]);
                    await webhook.Send(server.ID, server.Game, "Crashed", server.Name, server.IP, server.Port);
                }

                if (g_bAutoRestart[Int32.Parse(server.ID)])
                {
                    GameServer_Start(server);
                }
            }
        }

        private async Task<bool> IsServerCrashed(string serverid)
        {
            int id = Int32.Parse(serverid);

            while (g_Process[id] != null)
            {
                if (g_Process[id].HasExited)
                {
                    g_Process[id] = null;

                    return true;
                }
               
                await Task.Delay(1000);
            }

            return false;
        }

        private void SetServerStatus(Table server, string status)
        {
            server.Status = status;

            for (int i = 0; i < ServerGrid.Items.Count; i++)
            {
                Table temp = ServerGrid.Items[i] as Table;
                if (server.ID == temp.ID)
                {
                    if (ServerGrid.SelectedItem == ServerGrid.Items[i])
                    {
                        ServerGrid.Items.RemoveAt(i);
                        ServerGrid.Items.Insert(i, server);
                        ServerGrid.SelectedItem = ServerGrid.Items[i];
                    }
                    else
                    {
                        ServerGrid.Items.RemoveAt(i);
                        ServerGrid.Items.Insert(i, server);
                    }

                    break;
                }
            }
        }

        public void Log(string serverid, string logtext)
        {
            Table row = null;

            for (int i = 0; i < ServerGrid.Items.Count; i++)
            {
                row = ServerGrid.Items[i] as Table;
                if (row.ID == serverid)
                {
                    break;
                }
            }

            if (row == null)
            {
                return;
            }

            string log = DateTime.Now.ToString("MM/dd/yyyy - HH:mm:ss") + ": [" + row.Name + "]" + "(ID: " + serverid + ") - " + logtext + Environment.NewLine;

            string logPath = WGSM_PATH + "/logs/";
            if (!Directory.Exists(logPath))
            {
                Directory.CreateDirectory(logPath);
            }

            string log_file = WGSM_PATH + "/logs/L" + DateTime.Now.ToString("yyyyMMdd") + ".log";
            if (!File.Exists(log_file))
            {
                File.Create(log_file).Dispose();
            }

            File.AppendAllText(log_file, log);

            console.Text += log;
            console.ScrollToEnd();
        }

        private void Button_ClearLog_Click(object sender, RoutedEventArgs e)
        {
            console.Text = "";
        }

        private void SendCommand(Table server, string command)
        {
            Process p = g_Process[Int32.Parse(server.ID)];

            if (p == null)
            {
                return;
            }

            if (command.Trim() == "quit")
            {
                GameServer_Stop(server);
                return;
            }

            if (command.Trim() == "_restart")
            {
                GameServer_Restart(server);
                return;
            }

            SetForegroundWindow(p.MainWindowHandle);
            SendKeys.SendWait(command);
            SendKeys.SendWait("{ENTER}");
            SetForegroundWindow(Process.GetCurrentProcess().MainWindowHandle);
        }

        private static bool IsValidIPAddress(string ip)
        {
            if (String.IsNullOrWhiteSpace(ip))
            {
                return false;
            }

            string[] splitValues = ip.Split('.');
            if (splitValues.Length != 4)
            {
                return false;
            }

            return splitValues.All(r => byte.TryParse(r, out byte tempForParsing));
        }

        private static bool IsValidPort(string port)
        {
            if (!Int32.TryParse(port, out int portnum))
            {
                return false;
            }

            return portnum > 1 && portnum < 65535;
        }

        private void Browse_ServerBackups_Click(object sender, RoutedEventArgs e)
        {
            Table row = (Table)ServerGrid.SelectedItem;
            if (row == null)
            {
                return;
            }

            string path = WGSM_PATH + @"\backups\" + row.ID;
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            Process.Start(path);
        }

        private void Browse_ServerConfigs_Click(object sender, RoutedEventArgs e)
        {
            Table row = (Table)ServerGrid.SelectedItem;
            if (row == null)
            {
                return;
            }

            string path = WGSM_PATH + @"\servers\" + row.ID + @"\configs";
            if (Directory.Exists(path))
            {
                Process.Start(path);
            }
        }

        private void Browse_ServerFiles_Click(object sender, RoutedEventArgs e)
        {
            Table row = (Table)ServerGrid.SelectedItem;
            if (row == null)
            {
                return;
            }

            string path = WGSM_PATH + @"\servers\" + row.ID + @"\serverfiles";
            if (Directory.Exists(path))
            {
                Process.Start(path);
            }
        }

        private void Button_Github_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("https://github.com/BattlefieldDuck/WindowsGSM");
        }

        private void Button_Settings_Click(object sender, RoutedEventArgs e)
        {
            this.RightWindowCommandsOverlayBehavior = WindowCommandsOverlayBehavior.HiddenTitleBar;
            this.WindowButtonCommandsOverlayBehavior = WindowCommandsOverlayBehavior.HiddenTitleBar;

            MahAppFlyout.IsOpen = !MahAppFlyout.IsOpen;
        }

        private void HardWareAcceleration_IsCheckedChanged(object sender, EventArgs e)
        {
            RegistryKey key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\WindowsGSM", true);
            if (key != null)
            {
                key.SetValue("HardWareAcceleration", (MahAppSwitch_HardWareAcceleration.IsChecked ?? false).ToString());
                key.Close();
            }

            RenderOptions.ProcessRenderMode = (MahAppSwitch_HardWareAcceleration.IsChecked ?? false) ? System.Windows.Interop.RenderMode.SoftwareOnly : System.Windows.Interop.RenderMode.Default;
        }

        private void UIAnimation_IsCheckedChanged(object sender, EventArgs e)
        {
            RegistryKey key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\WindowsGSM", true);
            if (key != null)
            {
                key.SetValue("UIAnimation", (MahAppSwitch_UIAnimation.IsChecked ?? false).ToString());
                key.Close();
            }

            WindowTransitionsEnabled = MahAppSwitch_UIAnimation.IsChecked ?? false;
        }

        private void DarkTheme_IsCheckedChanged(object sender, EventArgs e)
        {
            RegistryKey key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\WindowsGSM", true);
            if (key != null)
            {
                key.SetValue("DarkTheme", (MahAppSwitch_DarkTheme.IsChecked ?? false).ToString());
                key.Close();
            }

            ThemeManager.ChangeAppTheme(App.Current, (MahAppSwitch_DarkTheme.IsChecked ?? false) ? "BaseDark" : "BaseLight");
        }
    }
}
