using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.IO;
using System.Net;
using System.IO.Compression;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.Win32;
using MahApps.Metro;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using System.Windows.Media.Imaging;
using Newtonsoft.Json.Linq;

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

        public static readonly string WGSM_VERSION = "v1.7.0";
        public static readonly int MAX_SERVER = 100;
        public static readonly string WGSM_PATH = Process.GetCurrentProcess().MainModule.FileName.Replace(@"\WindowsGSM.exe", "");

        //public static readonly string WGSM_PATH = @"D:\WindowsGSMtest2";

        private readonly NotifyIcon notifyIcon;

        private Install InstallWindow;
        private Import ImportWindow;

        private static readonly ServerStatus[] g_iServerStatus = new ServerStatus[MAX_SERVER + 1];

        private static readonly Process[] g_Process = new Process[MAX_SERVER + 1];

        private static readonly string[] g_SteamGSLT = new string[MAX_SERVER + 1];
        private static readonly string[] g_AdditionalParam = new string[MAX_SERVER + 1];

        private static readonly bool[] g_bAutoRestart = new bool[MAX_SERVER + 1];
        private static readonly bool[] g_bAutoStart = new bool[MAX_SERVER + 1];
        private static readonly bool[] g_bUpdateOnStart = new bool[MAX_SERVER + 1];

        private static readonly bool[] g_bDiscordAlert = new bool[MAX_SERVER + 1];
        private static readonly string[] g_DiscordWebhook = new string[MAX_SERVER + 1];

        private static string g_DonorType = "";

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
                key.SetValue("StartOnBoot", "False");
                key.SetValue("DonorTheme", "False");
                key.SetValue("DonorAuthKey", "");
                key.SetValue("Height", "540");
                key.SetValue("Width", "960");

                MahAppSwitch_HardWareAcceleration.IsChecked = true;
                MahAppSwitch_UIAnimation.IsChecked = true;
                MahAppSwitch_DarkTheme.IsChecked = false;
                MahAppSwitch_StartOnBoot.IsChecked = false;
                MahAppSwitch_DonorTheme.IsChecked = false;
            }
            else
            {
                MahAppSwitch_HardWareAcceleration.IsChecked = ((key.GetValue("HardWareAcceleration") ?? false).ToString() == "True") ? true : false;
                MahAppSwitch_UIAnimation.IsChecked = ((key.GetValue("UIAnimation") ?? false).ToString() == "True") ? true : false;
                MahAppSwitch_DarkTheme.IsChecked = ((key.GetValue("DarkTheme") ?? false).ToString() == "True") ? true : false;
                MahAppSwitch_StartOnBoot.IsChecked = ((key.GetValue("StartOnBoot") ?? false).ToString() == "True") ? true : false;
                MahAppSwitch_DonorTheme.IsChecked = ((key.GetValue("DonorTheme") ?? false).ToString() == "True") ? true : false;

                if (MahAppSwitch_DonorTheme.IsChecked ?? false)
                {
                    string authKey = (key.GetValue("DonorAuthKey") == null) ? "" : key.GetValue("DonorAuthKey").ToString();
                    if (!String.IsNullOrWhiteSpace(authKey))
                    {
#pragma warning disable 4014
                        ActivateDonorTheme(authKey);
#pragma warning restore
                    }
                }

                Height = (key.GetValue("Height") == null) ? 540 : double.Parse(key.GetValue("Height").ToString());
                Width = (key.GetValue("Width") == null) ? 960 : double.Parse(key.GetValue("Width").ToString());
            }
            key.Close();

            RenderOptions.ProcessRenderMode = (MahAppSwitch_HardWareAcceleration.IsChecked ?? false) ? System.Windows.Interop.RenderMode.SoftwareOnly : System.Windows.Interop.RenderMode.Default;
            WindowTransitionsEnabled = MahAppSwitch_UIAnimation.IsChecked ?? false;
            ThemeManager.ChangeAppTheme(App.Current, (MahAppSwitch_DarkTheme.IsChecked ?? false) ? "BaseDark" : "BaseLight");
            //Not required - it is set by windows settings
            //SetStartOnBoot(MahAppSwitch_StartOnBoot.IsChecked ?? false);

            notifyIcon = new NotifyIcon
            {
                BalloonTipTitle = "WindowsGSM",
                BalloonTipText = "WindowsGSM is running in the background",
                Text = "WindowsGSM",
                BalloonTipIcon = ToolTipIcon.Info
            };

            Stream iconStream = System.Windows.Application.GetResourceStream(new Uri("pack://application:,,,/Images/WindowsGSM.ico")).Stream;
            if (iconStream != null)
            {
                notifyIcon.Icon = new System.Drawing.Icon(iconStream);
            }

            notifyIcon.BalloonTipClicked += OnBalloonTipClick;
            notifyIcon.MouseClick += NotifyIcon_MouseClick;

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

            AutoStartServer();
        }

        private void RefreshServerList_Click(object sender, RoutedEventArgs e)
        {
            LoadServerTable();
        }

        public void LoadServerTable()
        {
            Function.ServerTable selectedrow = (Function.ServerTable)ServerGrid.SelectedItem;

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

                Function.ServerTable row = new Function.ServerTable
                {
                    ID = i.ToString(),
                    Game = serverConfig.ServerGame,
                    Icon = "/WindowsGSM;component/" + GameServer.Data.Icon.ResourceManager.GetString(serverConfig.ServerGame),
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
                g_bAutoStart[i] = serverConfig.AutoStart;
                g_bUpdateOnStart[i] = serverConfig.UpdateOnStart;
                g_bDiscordAlert[i] = serverConfig.DiscordAlert;
                g_DiscordWebhook[i] = serverConfig.DiscordWebhook;
            }

            grid_action.Visibility = (ServerGrid.Items.Count != 0) ? Visibility.Visible : Visibility.Hidden;
        }

        private void AutoStartServer()
        {
            int num_row = ServerGrid.Items.Count;
            for (int i = 0; i < num_row; i++)
            {
                Function.ServerTable server = (Function.ServerTable)ServerGrid.Items[i];
                if (g_bAutoStart[Int32.Parse(server.ID)])
                {
                    GameServer_Start(server);
                }
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            //Save height and width
            RegistryKey key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\WindowsGSM", true);
            if (key != null)
            {
                key.SetValue("Height", Height.ToString());
                key.SetValue("Width", Width.ToString());
                key.Close();
            }

            //Shutdown all server before WindowsGSM close
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

            if (!hasServerRunning) { return; }

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
            Function.ServerTable row = (Function.ServerTable)ServerGrid.SelectedItem;

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
                button_Status.Background = (g_iServerStatus[Int32.Parse(row.ID)] == ServerStatus.Started) ? System.Windows.Media.Brushes.LimeGreen : System.Windows.Media.Brushes.Orange;
                textBox_ServerGame.Text = row.Game;

                button_autorestart.Content = (g_bAutoRestart[Int32.Parse(row.ID)]) ? "TRUE" : "FALSE";
                button_autorestart.Background = (g_bAutoRestart[Int32.Parse(row.ID)]) ? System.Windows.Media.Brushes.LimeGreen : System.Windows.Media.Brushes.Red;

                button_autostart.Content = (g_bAutoStart[Int32.Parse(row.ID)]) ? "TRUE" : "FALSE";
                button_autostart.Background = (g_bAutoStart[Int32.Parse(row.ID)]) ? System.Windows.Media.Brushes.LimeGreen : System.Windows.Media.Brushes.Red;

                button_updateonstart.Content = (g_bUpdateOnStart[Int32.Parse(row.ID)]) ? "TRUE" : "FALSE";
                button_updateonstart.Background = (g_bUpdateOnStart[Int32.Parse(row.ID)]) ? System.Windows.Media.Brushes.LimeGreen : System.Windows.Media.Brushes.Red;

                button_discordalert.Content = (g_bDiscordAlert[Int32.Parse(row.ID)]) ? "TRUE" : "FALSE";
                button_discordalert.Background = (g_bDiscordAlert[Int32.Parse(row.ID)]) ? System.Windows.Media.Brushes.LimeGreen : System.Windows.Media.Brushes.Red;

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
                    servergame = GameServer.Data.List.ResourceManager.GetString((++i).ToString());
                    if (servergame == null)
                    {
                        break;
                    }

                    var row = new Images.Row { Image = "/WindowsGSM;component/" + GameServer.Data.Icon.ResourceManager.GetString(servergame), Name = servergame };
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
                    servergame = GameServer.Data.List.ResourceManager.GetString((++i).ToString());
                    if (servergame == null)
                    {
                        break;
                    }

                    var row = new Images.Row { Image = "/WindowsGSM;component/" + GameServer.Data.Icon.ResourceManager.GetString(servergame), Name = servergame };
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
            Function.ServerTable server = (Function.ServerTable)ServerGrid.SelectedItem;
            if (server == null) { return; }

            if (g_iServerStatus[Int32.Parse(server.ID)] != ServerStatus.Stopped) { return; }

            MessageBoxResult result = System.Windows.MessageBox.Show("Do you want to delete this server?\n(There is no comeback)", "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result != MessageBoxResult.Yes) { return; }

            WindowsFirewall firewall = new WindowsFirewall(null, Functions.Path.Get(server.ID));
            await firewall.RemoveRuleEx();

            await GameServer_Delete(server);
        }

        private async void Button_DiscordWebhookTest_Click(object sender, RoutedEventArgs e)
        {
            Function.ServerTable row = (Function.ServerTable)ServerGrid.SelectedItem;
            if (row == null) { return; }

            if (!g_bDiscordAlert[Int32.Parse(row.ID)]) { return; }

            Discord.Webhook webhook = new Discord.Webhook(g_DiscordWebhook[Int32.Parse(row.ID)], g_DonorType);
            await webhook.Send(row.ID, row.Game, "Webhook Test Alert", row.Name, row.IP, row.Port);
        }

        private void Button_ServerCommand_Click(object sender, RoutedEventArgs e)
        {
            string command = textbox_servercommand.Text;
            textbox_servercommand.Text = "";

            if (string.IsNullOrWhiteSpace(command)) { return; }

            Function.ServerTable server = (Function.ServerTable)ServerGrid.SelectedItem;
            if (server == null) { return; }

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
            Function.ServerTable server = (Function.ServerTable)ServerGrid.SelectedItem;
            if (server == null) { return; }

            //Reload WindowsGSM.cfg on start
            int i = Int32.Parse(server.ID);
            Functions.ServerConfig serverConfig = new Functions.ServerConfig(i.ToString());
            g_SteamGSLT[i] = serverConfig.ServerGSLT;
            g_AdditionalParam[i] = serverConfig.ServerParam;
            g_bAutoRestart[i] = serverConfig.AutoRestart;
            g_bAutoStart[i] = serverConfig.AutoStart;
            g_bUpdateOnStart[i] = serverConfig.UpdateOnStart;
            g_bDiscordAlert[i] = serverConfig.DiscordAlert;
            g_DiscordWebhook[i] = serverConfig.DiscordWebhook;

            GameServer_Start(server);
        }

        private void Actions_Stop_Click(object sender, RoutedEventArgs e)
        {
            Function.ServerTable server = (Function.ServerTable)ServerGrid.SelectedItem;
            if (server == null) { return; }

            GameServer_Stop(server);
        }

        private void Actions_Restart_Click(object sender, RoutedEventArgs e)
        {
            Function.ServerTable server = (Function.ServerTable)ServerGrid.SelectedItem;
            if (server == null) { return; }

            GameServer_Restart(server);
        }

        private void Actions_ToggleConsole_Click(object sender, RoutedEventArgs e)
        {
            Function.ServerTable row = (Function.ServerTable)ServerGrid.SelectedItem;
            if (row == null) { return; }

            string serverid = row.ID.ToString();

            Process p = g_Process[Int32.Parse(serverid)];
            if (p == null) { return; }

            IntPtr hWnd = p.MainWindowHandle;
            ShowWindow(hWnd, (ShowWindow(hWnd, WindowShowStyle.Hide)) ? (WindowShowStyle.Hide) : (WindowShowStyle.ShowNormal));
        }

        private async void Actions_Update_Click(object sender, RoutedEventArgs e)
        {
            Function.ServerTable server = (Function.ServerTable)ServerGrid.SelectedItem;
            if (server == null) { return; }

            if (g_iServerStatus[Int32.Parse(server.ID)] != ServerStatus.Stopped) { return; }

            MessageBoxResult result = System.Windows.MessageBox.Show("Do you want to update this server?", "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result != MessageBoxResult.Yes) { return; }

            await GameServer_Update(server);
        }

        private async void Actions_Backup_Click(object sender, RoutedEventArgs e)
        {
            Function.ServerTable server = (Function.ServerTable)ServerGrid.SelectedItem;
            if (server == null) { return; }

            if (g_iServerStatus[Int32.Parse(server.ID)] != ServerStatus.Stopped) { return; }

            MessageBoxResult result = System.Windows.MessageBox.Show("Do you want to backup on this server?", "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result != MessageBoxResult.Yes) { return; }

            await GameServer_Backup(server);
        }

        private async void Actions_RestoreBackup_Click(object sender, RoutedEventArgs e)
        {
            Function.ServerTable server = (Function.ServerTable)ServerGrid.SelectedItem;
            if (server == null) { return; }

            if (g_iServerStatus[Int32.Parse(server.ID)] != ServerStatus.Stopped) { return; }

            MessageBoxResult result = System.Windows.MessageBox.Show("Do you want to restore backup on this server?\n(All server files will be overwritten)", "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result != MessageBoxResult.Yes) { return; }

            await GameServer_RestoreBackup(server);
        }

        private async void GameServer_Start(Function.ServerTable server)
        {
            if (g_iServerStatus[Int32.Parse(server.ID)] != ServerStatus.Stopped) { return; }

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
                Log(server.ID, "Server: Fail to start");
                Log(server.ID, "[ERROR]" + error);

                return;
            }

            if (g_Process[Int32.Parse(server.ID)] != null) { return; }

            Process p = g_Process[Int32.Parse(server.ID)];
            if (p != null) { return; }

            if (g_bUpdateOnStart[Int32.Parse(server.ID)])
            {
                await GameServer_Update(server);
            }

            //Begin Start
            g_iServerStatus[Int32.Parse(server.ID)] = ServerStatus.Starting;
            Log(server.ID, "Action: Start");
            SetServerStatus(server, "Starting");

            GameServer.Action.Start gameServerAction = new GameServer.Action.Start(server, g_SteamGSLT[Int32.Parse(server.ID)], g_AdditionalParam[Int32.Parse(server.ID)]);
            p = await gameServerAction.Run();

            Activate();

            //Fail to start
            if (p == null)
            {
                g_iServerStatus[Int32.Parse(server.ID)] = ServerStatus.Stopped;
                Log(server.ID, "Server: Fail to start");
                Log(server.ID, "[ERROR] " + gameServerAction.Error);
                SetServerStatus(server, "Stopped");

                return;
            }

            g_Process[Int32.Parse(server.ID)] = p;

            await Task.Run(() =>
            {
                try
                {
                    p.WaitForInputIdle();
                }
                catch
                {
                    //Wait until Window pop out
                    int count = 0;
                    while (p.MainWindowHandle == IntPtr.Zero && count < 100)
                    {
                        p.Refresh();

                        count++;
                        Task.Delay(100);
                    }

                    ShowWindow(p.MainWindowHandle, WindowShowStyle.Hide);
                }

                if (server.Game == GameServer.RUST.FullName || server.Game == GameServer._7DTD.FullName)
                {
                    while (!p.HasExited && !ShowWindow(p.MainWindowHandle, WindowShowStyle.Hide)) { }
                }
            });

            //An error may occur on ShowWindow if not adding this 
            if (p.HasExited)
            {
                g_Process[Int32.Parse(server.ID)] = null;

                g_iServerStatus[Int32.Parse(server.ID)] = ServerStatus.Stopped;
                Log(server.ID, "Server: Fail to start");
                Log(server.ID, "[ERROR] Exit Code: " + p.ExitCode.ToString());
                SetServerStatus(server, "Stopped");

                return;
            }

            ShowWindow(p.MainWindowHandle, WindowShowStyle.Hide);

            g_iServerStatus[Int32.Parse(server.ID)] = ServerStatus.Started;
            Log(server.ID, "Server: Started");
            if (!string.IsNullOrWhiteSpace(gameServerAction.Notice))
            {
                Log(server.ID, "[Notice] " + gameServerAction.Notice);
            }
            SetServerStatus(server, "Started");

            if (g_bDiscordAlert[Int32.Parse(server.ID)])
            {
                Discord.Webhook webhook = new Discord.Webhook(g_DiscordWebhook[Int32.Parse(server.ID)], g_DonorType);
                await webhook.Send(server.ID, server.Game, "Started", server.Name, server.IP, server.Port);
            }

            StartServerCrashDetector(server);
        }

        private async void GameServer_Stop(Function.ServerTable server)
        {
            if (g_iServerStatus[Int32.Parse(server.ID)] != ServerStatus.Started) { return; }

            Process p = g_Process[Int32.Parse(server.ID)];
            if (p == null) { return; }

            //Begin stop
            g_iServerStatus[Int32.Parse(server.ID)] = ServerStatus.Stopping;
            Log(server.ID, "Action: Stop");
            SetServerStatus(server, "Stopping");

            g_Process[Int32.Parse(server.ID)] = null;

            GameServer.Action.Stop gameServerAction = new GameServer.Action.Stop(server);
            bool stopGracefully = await gameServerAction.Run(p);
            Log(server.ID, "Server: Stopped");
            if (!stopGracefully)
            {
                Log(server.ID, "[NOTICE] Server fail to stop gracefully");
            }
            g_iServerStatus[Int32.Parse(server.ID)] = ServerStatus.Stopped;
            SetServerStatus(server, "Stopped");

            if (g_bDiscordAlert[Int32.Parse(server.ID)])
            {
                Discord.Webhook webhook = new Discord.Webhook(g_DiscordWebhook[Int32.Parse(server.ID)], g_DonorType);
                await webhook.Send(server.ID, server.Game, "Stopped", server.Name, server.IP, server.Port);
            }
        }

        private async void GameServer_Restart(Function.ServerTable server)
        {
            if (g_iServerStatus[Int32.Parse(server.ID)] != ServerStatus.Started) { return; }

            Process p = g_Process[Int32.Parse(server.ID)];
            if (p == null) { return; }

            g_Process[Int32.Parse(server.ID)] = null;

            //Begin Restart
            g_iServerStatus[Int32.Parse(server.ID)] = ServerStatus.Restarting;
            Log(server.ID, "Action: Restart");
            SetServerStatus(server, "Restarting");

            GameServer.Action.Restart gameServerAction = new GameServer.Action.Restart(server, g_SteamGSLT[Int32.Parse(server.ID)], g_AdditionalParam[Int32.Parse(server.ID)]);
            p = await gameServerAction.Run(p);

            Activate();

            //Fail to start
            if (p == null)
            {
                g_iServerStatus[Int32.Parse(server.ID)] = ServerStatus.Stopped;
                Log(server.ID, "Server: Fail to restart");
                Log(server.ID, "[ERROR] " + gameServerAction.Error);
                SetServerStatus(server, "Stopped");

                return;
            }

            g_Process[Int32.Parse(server.ID)] = p;

            await Task.Run(() =>
            {
                try
                {
                    p.WaitForInputIdle();
                }
                catch
                {
                    //Wait until Window pop out
                    int count = 0;
                    while (p.MainWindowHandle == IntPtr.Zero && count < 100)
                    {
                        p.Refresh();

                        count++;
                        Task.Delay(100);
                    }

                    ShowWindow(p.MainWindowHandle, WindowShowStyle.Hide);
                }

                if (server.Game == "Rust Dedicated Server")
                {
                    while (!p.HasExited && !ShowWindow(p.MainWindowHandle, WindowShowStyle.Hide)) { }
                }
            });

            //An error may occur on ShowWindow if not adding this 
            if (p.HasExited)
            {
                g_Process[Int32.Parse(server.ID)] = null;

                g_iServerStatus[Int32.Parse(server.ID)] = ServerStatus.Stopped;
                Log(server.ID, "Server: Fail to start");
                Log(server.ID, "[ERROR] Exit Code: " + p.ExitCode.ToString());
                SetServerStatus(server, "Stopped");

                return;
            }

            ShowWindow(p.MainWindowHandle, WindowShowStyle.Hide);

            g_iServerStatus[Int32.Parse(server.ID)] = (int)ServerStatus.Started;
            Log(server.ID, "Server: Restarted");
            if (!string.IsNullOrWhiteSpace(gameServerAction.Notice))
            {
                Log(server.ID, "[Notice] " + gameServerAction.Notice);
            }
            SetServerStatus(server, "Started");

            if (g_bDiscordAlert[Int32.Parse(server.ID)])
            {
                Discord.Webhook webhook = new Discord.Webhook(g_DiscordWebhook[Int32.Parse(server.ID)], g_DonorType);
                await webhook.Send(server.ID, server.Game, "Restarted", server.Name, server.IP, server.Port);
            }

            StartServerCrashDetector(server);
        }

        private async Task<bool> GameServer_Update(Function.ServerTable server)
        {
            if (g_iServerStatus[Int32.Parse(server.ID)] != ServerStatus.Stopped)
            {
                return false;
            }

            //Begin Update
            g_iServerStatus[Int32.Parse(server.ID)] = ServerStatus.Updating;
            Log(server.ID, "Action: Update");
            SetServerStatus(server, "Updating");

            GameServer.Action.Update gameServerAction = new GameServer.Action.Update(server);
            bool updated = await gameServerAction.Run();

            Activate();

            if (updated)
            {
                Log(server.ID, "Server: Updated");
            }
            else
            {
                Log(server.ID, "Server: Fail to update");
                Log(server.ID, "[ERROR] " + gameServerAction.Error);
            }

            g_iServerStatus[Int32.Parse(server.ID)] = ServerStatus.Stopped;
            SetServerStatus(server, "Stopped");

            return true;
        }

        private async Task<bool> GameServer_Backup(Function.ServerTable server)
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
                    Log(server.ID, "Server: Fail to backup");
                    Log(server.ID, "[ERROR] Fail to delete old backup");
                    SetServerStatus(server, "Stopped");

                    return false;
                }
            }

            await Task.Run(() => ZipFile.CreateFromDirectory(startPath, zipFile));

            if (!File.Exists(zipFile))
            {
                g_iServerStatus[Int32.Parse(server.ID)] = ServerStatus.Stopped;
                Log(server.ID, "Server: Fail to backup");
                Log(server.ID, "[ERROR] Cannot create zipfile");
                SetServerStatus(server, "Stopped");

                return false;
            }

            g_iServerStatus[Int32.Parse(server.ID)] = ServerStatus.Stopped;
            Log(server.ID, "Server: Backuped");
            SetServerStatus(server, "Stopped");

            return true;
        }

        private async Task<bool> GameServer_RestoreBackup(Function.ServerTable server)
        {
            if (g_iServerStatus[Int32.Parse(server.ID)] != ServerStatus.Stopped)
            {
                return false;
            }

            string zipFile = WGSM_PATH + @"\backups\" + server.ID + @"\backup-id-" + server.ID + ".zip";
            string extractPath = WGSM_PATH + @"\servers\" + server.ID;

            if (!File.Exists(zipFile))
            {
                Log(server.ID, "Server: Fail to restore backup");
                Log(server.ID, "[ERROR] Backup not found");

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
                    Log(server.ID, "Server: Fail to restore backup");
                    Log(server.ID, "[ERROR] Extract path is not accessible");

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

        private async Task<bool> GameServer_Delete(Function.ServerTable server)
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

                await Task.Delay(100);

                if (Directory.Exists(serverPath))
                {
                    Log(server.ID, "Server: Fail to delete server");
                    Log(server.ID, "[ERROR] Directory is not accessible");

                    return false;
                }
            }

            Log(server.ID, "Server: Deleted server");

            LoadServerTable();

            return true;
        }

        private async void StartServerCrashDetector(Function.ServerTable server)
        {
            Process p = g_Process[Int32.Parse(server.ID)];

            while (g_iServerStatus[Int32.Parse(server.ID)] == ServerStatus.Started)
            {
                if (p != null && p.HasExited)
                {
                    g_iServerStatus[Int32.Parse(server.ID)] = ServerStatus.Stopped;
                    Log(server.ID, "Server: Crashed");
                    //Log(server.ID, "[WARNING] Exit Code: " + g_Process[Int32.Parse(server.ID)].ExitCode.ToString());
                    SetServerStatus(server, "Stopped");

                    g_Process[Int32.Parse(server.ID)] = null;

                    if (g_bDiscordAlert[Int32.Parse(server.ID)])
                    {
                        Discord.Webhook webhook = new Discord.Webhook(g_DiscordWebhook[Int32.Parse(server.ID)], g_DonorType);
                        await webhook.Send(server.ID, server.Game, "Crashed", server.Name, server.IP, server.Port);
                    }

                    if (g_bAutoRestart[Int32.Parse(server.ID)])
                    {
                        GameServer_Start(server);
                    }

                    break;
                }

                await Task.Delay(1000);
            }
        }

        private void SetServerStatus(Function.ServerTable server, string status)
        {
            server.Status = status;

            for (int i = 0; i < ServerGrid.Items.Count; i++)
            {
                Function.ServerTable temp = ServerGrid.Items[i] as Function.ServerTable;
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
            Function.ServerTable row = null;

            for (int i = 0; i < ServerGrid.Items.Count; i++)
            {
                row = ServerGrid.Items[i] as Function.ServerTable;
                if (row.ID == serverid)
                {
                    break;
                }
            }

            if (row == null) { return; }

            string log = DateTime.Now.ToString("MM/dd/yyyy-HH:mm:ss") + $": [{row.Name}](#{serverid})-" + logtext + Environment.NewLine;

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

        private void SendCommand(Function.ServerTable server, string command)
        {
            Process p = g_Process[Int32.Parse(server.ID)];
            if (p == null) { return; }

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
            Function.ServerTable row = (Function.ServerTable)ServerGrid.SelectedItem;
            if (row == null) { return; }

            string path = WGSM_PATH + @"\backups\" + row.ID;
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            Process.Start(path);
        }

        private void Browse_ServerConfigs_Click(object sender, RoutedEventArgs e)
        {
            Function.ServerTable row = (Function.ServerTable)ServerGrid.SelectedItem;
            if (row == null) { return; }

            string path = WGSM_PATH + @"\servers\" + row.ID + @"\configs";
            if (Directory.Exists(path))
            {
                Process.Start(path);
            }
        }

        private void Browse_ServerFiles_Click(object sender, RoutedEventArgs e)
        {
            Function.ServerTable row = (Function.ServerTable)ServerGrid.SelectedItem;
            if (row == null) { return; }

            string path = WGSM_PATH + @"\servers\" + row.ID + @"\serverfiles";
            if (Directory.Exists(path))
            {
                Process.Start(path);
            }
        }

        private void Button_Patreon_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("https://www.patreon.com/WindowsGSM/");
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

        private void StartOnBoot_IsCheckedChanged(object sender, EventArgs e)
        {
            RegistryKey key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\WindowsGSM", true);
            if (key != null)
            {
                key.SetValue("StartOnBoot", (MahAppSwitch_StartOnBoot.IsChecked ?? false).ToString());
                key.Close();
            }

            SetStartOnBoot(MahAppSwitch_StartOnBoot.IsChecked ?? false);
        }

        private void SetStartOnBoot(bool enable)
        {
            string taskName = "WindowsGSM";
            string wgsmPath = Process.GetCurrentProcess().MainModule.FileName;
            if (enable)
            {
                Process.Start("schtasks", $"/create /tn {taskName} /tr \"{wgsmPath}\" /sc onlogon /rl HIGHEST /f");
            }
            else
            {
                Process.Start("schtasks", $"/delete /tn {taskName} /f");
            }
        }

        private async void DonorTheme_IsCheckedChanged(object sender, EventArgs e)
        {
            RegistryKey key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\WindowsGSM", true);

            //If switch is checked
            if (!MahAppSwitch_DonorTheme.IsChecked ?? false)
            {
                SetDonorTheme();
                key.SetValue("DonorTheme", (MahAppSwitch_DonorTheme.IsChecked ?? false).ToString());
                key.Close();
                return;
            }

            //If switch is not checked
            key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\WindowsGSM", true);
            string authKey = (key.GetValue("DonorAuthKey") == null) ? "" : key.GetValue("DonorAuthKey").ToString();

            MetroDialogSettings settings = new MetroDialogSettings
            {
                AffirmativeButtonText = "Activate",
                DefaultText = authKey
            };

            authKey = await this.ShowInputAsync("Donor Theme (Patreon)", "Please enter the activation key.", settings);

            //If pressed cancel or key is null or whitespace
            if (String.IsNullOrWhiteSpace(authKey))
            {
                MahAppSwitch_DonorTheme.IsChecked = false;
                key.Close();
                return;
            }

            var controller = await this.ShowProgressAsync("Authenticating...", "Please wait...");
            controller.SetIndeterminate();
            bool success = await ActivateDonorTheme(authKey);
            await controller.CloseAsync();

            if (success)
            {
                key.SetValue("DonorTheme", "True");
                key.SetValue("DonorAuthKey", authKey);
                await this.ShowMessageAsync("Success!", "Thanks for your donation! Here is your Donor Theme.");
            }
            else
            {
                key.SetValue("DonorTheme", "False");
                key.SetValue("DonorAuthKey", "");
                await this.ShowMessageAsync("Fail to activate.", "Please visit https://windowsgsm.com/patreon/ to get the key.");

                MahAppSwitch_DonorTheme.IsChecked = false;
            }
            key.Close();
        }

        private async Task<bool> ActivateDonorTheme(string authKey)
        {
            try
            {
                using (WebClient webClient = new WebClient())
                {
                    string json = await webClient.DownloadStringTaskAsync($"https://windowsgsm.com/patreon/patreonAuth.php?auth={authKey}");
                    bool success = (JObject.Parse(json)["success"].ToString() == "True") ? true : false;

                    if (success)
                    {
                        string name = JObject.Parse(json)["name"].ToString();
                        Title = $"WindowsGSM {WGSM_VERSION} - Patreon: {name}";

                        string type = JObject.Parse(json)["type"].ToString();
                        SetDonorTheme(type);

                        g_DonorType = type;

                        return true;
                    }
                }
            }
            catch
            {

            }

            return false;
        }

        private void SetDonorTheme(string type = "")
        {
            //Set theme
            var theme = ThemeManager.GetAppTheme((MahAppSwitch_DarkTheme.IsChecked ?? false) ? "BaseDark" : "BaseLight");
            var color = "Teal";
            switch (type)
            {
                case "BRONZE":
                    color = "Orange";
                    break;
                case "GOLD":
                    color = "Amber";
                    break;
                case "EMERALD":
                    color = "Emerald";
                    break;
            }
            ThemeManager.ChangeAppStyle(App.Current, ThemeManager.GetAccent(color), theme);

            //Set icon
            string uriPath = "pack://application:,,,/Images/WindowsGSM";
            switch (type)
            {
                case "BRONZE":
                case "GOLD":
                case "EMERALD":
                    uriPath += $"-{type}";
                    break;
            }
            uriPath += ".ico";
            Uri iconUri = new Uri(uriPath, UriKind.RelativeOrAbsolute);
            this.Icon = BitmapFrame.Create(iconUri);

            //Set notify icon
            Stream iconStream = System.Windows.Application.GetResourceStream(new Uri(uriPath)).Stream;
            if (iconStream != null)
            {
                notifyIcon.Icon = new System.Drawing.Icon(iconStream);
            }
        }

        private void Help_OnlineDocumentation_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("https://github.com/BattlefieldDuck/WindowsGSM/wiki");
        }

        private void Help_ReportIssue_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("https://github.com/BattlefieldDuck/WindowsGSM/issues");
        }

        private void Help_CheckForUpdate_Click(object sender, RoutedEventArgs e)
        {
            string messageText = "Your WindowsGSM is up to date.";

            string latestVersion = GetLatestVersion();
            if (latestVersion != WGSM_VERSION)
            {
                messageText = "A new version of WindowsGSM is available, would you like to browse the release page?";

                MessageBoxResult result = System.Windows.MessageBox.Show("Current version: " + WGSM_VERSION + "\nLatest version: " + latestVersion + "\n\n" + messageText, "Check for Update", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes)
                {
                    Process.Start("https://github.com/BattlefieldDuck/WindowsGSM/releases");
                }
            }
            else
            {
                System.Windows.MessageBox.Show("Current version: " + WGSM_VERSION + "\nLatest version: " + latestVersion + "\n\n" + messageText, "Check for Update", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private string GetLatestVersion()
        {
            HttpWebRequest webRequest = System.Net.WebRequest.Create("https://api.github.com/repos/BattlefieldDuck/WindowsGSM/releases/latest") as HttpWebRequest;
            if (webRequest != null)
            {
                webRequest.Method = "GET";
                webRequest.UserAgent = "Anything";
                webRequest.ServicePoint.Expect100Continue = false;

                try
                {
                    using (StreamReader responseReader = new StreamReader(webRequest.GetResponse().GetResponseStream()))
                    {
                        string json = responseReader.ReadToEnd();
                        string version = JObject.Parse(json)["tag_name"].ToString();

                        return version;
                    }
                }
                catch
                {
                    return null;
                }
            }

            return null;
        }

        private void Tools_GlobalServerListCheck_Click(object sender, RoutedEventArgs e)
        {
            Function.ServerTable row = (Function.ServerTable)ServerGrid.SelectedItem;
            if (row == null) { return; }

            if (row.Game == GameServer.MCPE.FullName || row.Game == GameServer.MC.FullName)
            {
                Log(row.ID, "This feature is not applicable on " + row.Game);
                return;
            }

            string publicIP = GetPublicIP();
            if (publicIP == null)
            {
                Log(row.ID, "Fail to check. Reason: Fail to get the public ip.");
                return;
            }

            string port = row.Port;

            string messageText = "Server Name: " + row.Name + "\nPublic IP: " + publicIP + "\nServer Port: " + port;

            if (IsServerOnSteamServerList(publicIP, port))
            {
                System.Windows.MessageBox.Show(messageText + "\n\nResult: Online\n\nYour server is on the global server list!", "Global Server List Check", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                System.Windows.MessageBox.Show(messageText + "\n\nResult: Offline\n\nYour server is not on the global server list.", "Global Server List Check", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private string GetPublicIP()
        {
            try
            {
                using (WebClient webClient = new WebClient())
                {
                    return webClient.DownloadString("https://ipinfo.io/ip").Replace("\n", "");
                }
            }
            catch
            {
                return null;
            }
        }

        private bool IsServerOnSteamServerList(string publicIP, string port)
        {
            HttpWebRequest webRequest = System.Net.WebRequest.Create("http://api.steampowered.com/ISteamApps/GetServersAtAddress/v0001?addr=" + publicIP + "&format=json") as HttpWebRequest;
            if (webRequest != null)
            {
                webRequest.Method = "GET";
                webRequest.UserAgent = "Anything";
                webRequest.ServicePoint.Expect100Continue = false;

                try
                {
                    using (StreamReader responseReader = new StreamReader(webRequest.GetResponse().GetResponseStream()))
                    {
                        string json = responseReader.ReadToEnd();
                        string matchString = "\"addr\":\"" + publicIP + ":" + port + "\"";

                        if (json.Contains(matchString))
                        {
                            return true;
                        }
                    }
                }
                catch
                {
                    return false;
                }
            }

            return false;
        }

        private void Window_StateChanged(object sender, EventArgs e)
        {
        }

        private void OnBalloonTipClick(object sender, EventArgs e)
        {
        }

        private void NotifyIcon_MouseClick(Object sender, System.Windows.Forms.MouseEventArgs e)
        {
            notifyIcon.Visible = false;

            WindowState = WindowState.Normal;
            Show();
        }

        private void Button_Hide_Click(object sender, RoutedEventArgs e)
        {
            Hide();
            notifyIcon.Visible = true;
            notifyIcon.ShowBalloonTip(0);
            notifyIcon.Visible = false;
            notifyIcon.Visible = true;
        }
    }
}
