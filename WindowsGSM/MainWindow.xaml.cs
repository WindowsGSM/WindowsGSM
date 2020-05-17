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
using Newtonsoft.Json.Linq;
using NCrontab;
using System.Collections.Generic;
using System.Collections;
using LiveCharts;
using LiveCharts.Wpf;

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
        private static extern int SetWindowText(IntPtr hWnd, string windowName);

        private enum WindowShowStyle : uint
        {
            Hide = 0,
            ShowNormal = 1,
            Show = 5,
            Minimize = 6,
            ShowMinNoActivate = 7
        }

        public enum ServerStatus
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
            Restoring = 11,
            Deleting = 12
        }

        public static readonly string WGSM_VERSION = "v" + string.Concat(System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString().Reverse().Skip(2).Reverse());
        public static readonly int MAX_SERVER = 50;
        public static readonly string WGSM_PATH = Process.GetCurrentProcess().MainModule.FileName.Replace(@"\WindowsGSM.exe", "");

        private readonly NotifyIcon notifyIcon;

        private Process Installer;

        private static readonly ServerStatus[] g_iServerStatus = new ServerStatus[MAX_SERVER + 1];

        private static readonly Process[] g_Process = new Process[MAX_SERVER + 1];
        private static readonly IntPtr[] g_MainWindow = new IntPtr[MAX_SERVER + 1];

        private static readonly bool[] g_bAutoRestart = new bool[MAX_SERVER + 1];
        private static readonly bool[] g_bAutoStart = new bool[MAX_SERVER + 1];
        private static readonly bool[] g_bAutoUpdate = new bool[MAX_SERVER + 1];
        private static readonly bool[] g_bUpdateOnStart = new bool[MAX_SERVER + 1];

        private static readonly bool[] g_bDiscordAlert = new bool[MAX_SERVER + 1];
        private static readonly string[] g_DiscordMessage = new string[MAX_SERVER + 1];
        private static readonly string[] g_DiscordWebhook = new string[MAX_SERVER + 1];
        private static readonly bool[] g_bAutoRestartAlert = new bool[MAX_SERVER + 1];
        private static readonly bool[] g_bAutoStartAlert = new bool[MAX_SERVER + 1];
        private static readonly bool[] g_bAutoUpdateAlert = new bool[MAX_SERVER + 1];
        private static readonly bool[] g_bRestartCrontabAlert = new bool[MAX_SERVER + 1];
        private static readonly bool[] g_bCrashAlert = new bool[MAX_SERVER + 1];

        private static readonly bool[] g_bRestartCrontab = new bool[MAX_SERVER + 1];
        private static readonly string[] g_CrontabFormat = new string[MAX_SERVER + 1];

        private static readonly bool[] g_bEmbedConsole = new bool[MAX_SERVER + 1];

        private static readonly string[] g_CPUPriority = new string[MAX_SERVER + 1];
        private static readonly string[] g_CPUAffinity = new string[MAX_SERVER + 1];

        private readonly List<System.Windows.Controls.CheckBox> _checkBoxes = new List<System.Windows.Controls.CheckBox>();

        private string g_DonorType = string.Empty;

        private readonly DiscordBot.Bot g_DiscordBot = new DiscordBot.Bot();

        public static Functions.ServerConsole[] g_ServerConsoles = new Functions.ServerConsole[MAX_SERVER + 1];

        public MainWindow()
        {
            //Add SplashScreen
            SplashScreen splashScreen = new SplashScreen("Images/SplashScreen.png");
            splashScreen.Show(false, true);

            InitializeComponent();

            //Close SplashScreen
            splashScreen.Close(new TimeSpan(0, 0, 1));

            Title = $"WindowsGSM {WGSM_VERSION}";

            ThemeManager.Accents.ToList().ForEach(delegate (Accent accent)
            {
                comboBox_Themes.Items.Add(accent.Name);
            });

            RegistryKey key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\WindowsGSM");
            if (key == null)
            {
                key = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\WindowsGSM");
                key.SetValue("HardWareAcceleration", "True");
                key.SetValue("UIAnimation", "True");
                key.SetValue("DarkTheme", "False");
                key.SetValue("StartOnBoot", "False");
                key.SetValue("DonorTheme", "False");
                key.SetValue("DonorColor", "Blue");
                key.SetValue("DonorAuthKey", "");
                key.SetValue("SendStatistics", "True");
                key.SetValue("Height", "540");
                key.SetValue("Width", "960");
                key.SetValue("DiscordBotAutoStart", "False");

                MahAppSwitch_HardWareAcceleration.IsChecked = true;
                MahAppSwitch_UIAnimation.IsChecked = true;
                MahAppSwitch_DarkTheme.IsChecked = false;
                MahAppSwitch_StartOnBoot.IsChecked = false;
                MahAppSwitch_DonorConnect.IsChecked = false;
                comboBox_Themes.Text = "Blue";
                MahAppSwitch_SendStatistics.IsChecked = true;
                MahAppSwitch_DiscordBotAutoStart.IsChecked = false;
            }
            else
            {
                MahAppSwitch_HardWareAcceleration.IsChecked = ((key.GetValue("HardWareAcceleration") ?? true).ToString() == "True") ? true : false;
                MahAppSwitch_UIAnimation.IsChecked = ((key.GetValue("UIAnimation") ?? true).ToString() == "True") ? true : false;
                MahAppSwitch_DarkTheme.IsChecked = ((key.GetValue("DarkTheme") ?? false).ToString() == "True") ? true : false;
                MahAppSwitch_StartOnBoot.IsChecked = ((key.GetValue("StartOnBoot") ?? false).ToString() == "True") ? true : false;
                MahAppSwitch_DonorConnect.IsChecked = ((key.GetValue("DonorTheme") ?? false).ToString() == "True") ? true : false;
                var theme = ThemeManager.GetAccent((key.GetValue("DonorColor") ?? string.Empty).ToString());
                comboBox_Themes.Text = (theme == null || !(MahAppSwitch_DonorConnect.IsChecked ?? false)) ? "Blue" : theme.Name;
                MahAppSwitch_SendStatistics.IsChecked = ((key.GetValue("SendStatistics") ?? true).ToString() == "True") ? true : false;
                MahAppSwitch_DiscordBotAutoStart.IsChecked = ((key.GetValue("DiscordBotAutoStart") ?? false).ToString() == "True") ? true : false;

                if (MahAppSwitch_DonorConnect.IsChecked ?? false)
                {
                    string authKey = (key.GetValue("DonorAuthKey") == null) ? string.Empty : key.GetValue("DonorAuthKey").ToString();
                    if (!string.IsNullOrWhiteSpace(authKey))
                    {
#pragma warning disable 4014
                        AuthenticateDonor(authKey);
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
            if (MahAppSwitch_DiscordBotAutoStart.IsChecked ?? false)
            {
                AutoStartDiscordBot();
            }

            // Add items to Set Affinity Flyout
            for (int i = 0; i < Environment.ProcessorCount; i++)
            {
                StackPanel stackPanel = new StackPanel
                {
                    Orientation = System.Windows.Controls.Orientation.Horizontal,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(15, 0, 0, 0)
                };

                _checkBoxes.Add(new System.Windows.Controls.CheckBox());
                _checkBoxes[i].Focusable = false;
                var label = new System.Windows.Controls.Label
                {
                    Content = $"CPU {i}",
                    Padding = new Thickness(0, 5, 0, 5)
                };

                stackPanel.Children.Add(_checkBoxes[i]);
                stackPanel.Children.Add(label);
                StackPanel_SetAffinity.Children.Add(stackPanel);
            }

            // Add click listener on each checkBox
            foreach (var checkBox in _checkBoxes)
            {
                checkBox.Click += (sender, e) =>
                {
                    var server = (Functions.ServerTable)ServerGrid.SelectedItem;
                    if (server == null) { return; }

                    CheckPrioity:
                    string priority = string.Empty;
                    for (int i = _checkBoxes.Count - 1; i >= 0; i--)
                    {
                        priority += (_checkBoxes[i].IsChecked ?? false) ? "1" : "0";
                    }

                    if (!priority.Contains("1"))
                    {
                        checkBox.IsChecked = true;
                        goto CheckPrioity;
                    }

                    textBox_SetAffinity.Text = Functions.CPU.Affinity.GetAffinityValidatedString(priority);

                    g_CPUAffinity[int.Parse(server.ID)] = priority;
                    Functions.ServerConfig.SetSetting(server.ID, "cpuaffinity", priority);

                    if (g_Process[int.Parse(server.ID)] != null && !g_Process[int.Parse(server.ID)].HasExited)
                    {
                        g_Process[int.Parse(server.ID)].ProcessorAffinity = Functions.CPU.Affinity.GetAffinityIntPtr(priority);
                    }
                };
            }

            notifyIcon = new NotifyIcon
            {
                BalloonTipTitle = "WindowsGSM",
                BalloonTipText = "WindowsGSM is running in the background",
                Text = "WindowsGSM",
                BalloonTipIcon = ToolTipIcon.Info,
                Visible = true
            };

            Stream iconStream = System.Windows.Application.GetResourceStream(new Uri("pack://application:,,,/Images/WindowsGSM-Icon.ico")).Stream;
            if (iconStream != null)
            {
                notifyIcon.Icon = new System.Drawing.Icon(iconStream);
            }

            notifyIcon.BalloonTipClicked += OnBalloonTipClick;
            notifyIcon.MouseClick += NotifyIcon_MouseClick;

            //Add games to ComboBox
            SortedList sortedList = new SortedList();
            List<DictionaryEntry> gameName = GameServer.Data.Icon.ResourceManager.GetResourceSet(System.Globalization.CultureInfo.CurrentUICulture, true, true).Cast<DictionaryEntry>().ToList();
            gameName.ForEach(delegate (DictionaryEntry entry) { sortedList.Add(entry.Key, entry.Value); });
            label_GameServerCount.Content = $"{gameName.Count} game servers supported.";
            for (int i = 0; i < sortedList.Count; i++)
            {
                var row = new Images.Row
                {
                    Image = "/WindowsGSM;component/" + sortedList.GetByIndex(i),
                    Name = sortedList.GetKey(i).ToString()
                };

                comboBox_InstallGameServer.Items.Add(row);
                comboBox_ImportGameServer.Items.Add(row);
            }

            for (int i = 0; i <= MAX_SERVER; i++)
            {
                g_iServerStatus[i] = ServerStatus.Stopped;
                g_ServerConsoles[i] = new Functions.ServerConsole(i.ToString());
            }

            LoadServerTable();

            if (ServerGrid.Items.Count > 0)
            {
                ServerGrid.SelectedItem = ServerGrid.Items[0];
            }

            foreach (Functions.ServerTable server in ServerGrid.Items.Cast<Functions.ServerTable>().ToList())
            {
                int pid = Functions.ServerCache.GetPID(server.ID);
                if (pid != -1)
                {
                    Process p = null;
                    try
                    {
                        p = Process.GetProcessById(pid);
                    }
                    catch
                    {
                        continue;
                    }

                    string pName = Functions.ServerCache.GetProcessName(server.ID);
                    if (!string.IsNullOrWhiteSpace(pName) && p.ProcessName == pName)
                    {
                        g_Process[int.Parse(server.ID)] = p;
                        g_iServerStatus[int.Parse(server.ID)] = ServerStatus.Started;
                        SetServerStatus(server, "Started");

                        g_MainWindow[int.Parse(server.ID)] = Functions.ServerCache.GetWindowsIntPtr(server.ID);
                        p.Exited += (sender, e) => OnGameServerExited(server);

                        StartAutoUpdateCheck(server);
                        StartRestartCrontabCheck(server);
                        StartSendHeartBeat(server);
                        StartQuery(server);
                    }
                }
            }

            AutoStartServer();

            if (MahAppSwitch_SendStatistics.IsChecked ?? false)
            {
                SendGoogleAnalytics();
            }

            StartConsoleRefresh();

            StartDashBoardRefresh();
        }

        public void LoadServerTable()
        {
            string[] livePlayerData = new string[MAX_SERVER + 1];
            foreach (Functions.ServerTable item in ServerGrid.Items)
            {
                livePlayerData[int.Parse(item.ID)] = item.Maxplayers;
            }

            var selectedrow = (Functions.ServerTable)ServerGrid.SelectedItem;
            ServerGrid.Items.Clear();

            //Add server to datagrid
            for (int i = 1; i <= MAX_SERVER; i++)
            {
                string serverid_path = Path.Combine(WGSM_PATH, "Servers", i.ToString());
                if (!Directory.Exists(serverid_path)) { continue; }

                string configpath = Functions.ServerPath.GetServersConfigs(i.ToString(), "WindowsGSM.cfg");
                if (!File.Exists(configpath)) { continue; }

                var serverConfig = new Functions.ServerConfig(i.ToString());

                //If Game server not exist return
                if (GameServer.Data.Class.Get(serverConfig.ServerGame) == null) { continue; }

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
                    case ServerStatus.Deleting: status = "Deleteing"; break;
                    default:
                        {
                            g_iServerStatus[i] = ServerStatus.Stopped;
                            status = "Stopped";
                            break;
                        }
                }

                try
                {
                    var server = new Functions.ServerTable
                    {
                        ID = i.ToString(),
                        Game = serverConfig.ServerGame,
                        Icon = "/WindowsGSM;component/" + GameServer.Data.Icon.ResourceManager.GetString(serverConfig.ServerGame),
                        Status = status,
                        Name = serverConfig.ServerName,
                        IP = serverConfig.ServerIP,
                        Port = serverConfig.ServerPort,
                        QueryPort = serverConfig.ServerQueryPort,
                        Defaultmap = serverConfig.ServerMap,
                        Maxplayers = (g_iServerStatus[i] != ServerStatus.Started) ? serverConfig.ServerMaxPlayer : livePlayerData[i]
                    };

                    g_bAutoRestart[i] = serverConfig.AutoRestart;
                    g_bAutoStart[i] = serverConfig.AutoStart;
                    g_bAutoUpdate[i] = serverConfig.AutoUpdate;
                    g_bUpdateOnStart[i] = serverConfig.UpdateOnStart;
                    g_bDiscordAlert[i] = serverConfig.DiscordAlert;
                    g_DiscordMessage[i] = serverConfig.DiscordMessage;
                    g_DiscordWebhook[i] = serverConfig.DiscordWebhook;
                    g_bRestartCrontab[i] = serverConfig.RestartCrontab;
                    g_CrontabFormat[i] = serverConfig.CrontabFormat;
                    g_bEmbedConsole[i] = serverConfig.EmbedConsole;
                    g_bAutoStartAlert[i] = serverConfig.AutoStartAlert;
                    g_bAutoRestartAlert[i] = serverConfig.AutoRestartAlert;
                    g_bAutoUpdateAlert[i] = serverConfig.AutoUpdateAlert;
                    g_bRestartCrontabAlert[i] = serverConfig.RestartCrontabAlert;
                    g_bCrashAlert[i] = serverConfig.CrashAlert;
                    g_CPUPriority[i] = serverConfig.CPUPriority;
                    g_CPUAffinity[i] = serverConfig.CPUAffinity;

                    ServerGrid.Items.Add(server);

                    if (selectedrow != null)
                    {
                        if (selectedrow.ID == server.ID)
                        {
                            ServerGrid.SelectedItem = server;
                        }
                    }
                }
                catch
                {

                }
            }

            grid_action.Visibility = (ServerGrid.Items.Count != 0) ? Visibility.Visible : Visibility.Hidden;
        }

        private async void AutoStartDiscordBot()
        {
            switch_DiscordBot.IsChecked = await g_DiscordBot.Start();
            DiscordBotLog("Discord Bot " + ((switch_DiscordBot.IsChecked ?? false) ? "started." : "fail to start. Reason: Bot Token is invalid."));
        }

        private async void AutoStartServer()
        {
            List<Functions.ServerTable> servers = new List<Functions.ServerTable>();
            foreach (var item in ServerGrid.Items) { servers.Add((Functions.ServerTable)item); }

            foreach (var server in servers)
            {
                int serverId = int.Parse(server.ID);

                if (g_bAutoStart[serverId])
                {
                    await GameServer_Start(server, " | Auto Start");

                    if (g_iServerStatus[serverId] == ServerStatus.Started)
                    {
                        if (g_bDiscordAlert[serverId] && g_bAutoStartAlert[serverId])
                        {
                            var webhook = new Functions.DiscordWebhook(g_DiscordWebhook[serverId], g_DiscordMessage[serverId], g_DonorType);
                            await webhook.Send(server.ID, server.Game, "Started | Auto Start", server.Name, server.IP, server.Port);
                        }
                    }
                }
            }
        }

        private async void StartConsoleRefresh()
        {
            while (true)
            {
                await Task.Delay(10);
                var row = (Functions.ServerTable)ServerGrid.SelectedItem;
                if (row != null)
                {
                    string text = g_ServerConsoles[int.Parse(row.ID)].Get();
                    if (text.Length != console.Text.Length && text != console.Text)
                    {
                        console.Text = text;
                        if (!console.IsMouseOver)
                        {
                            console.ScrollToEnd();
                        }
                    }
                }
            }
        }

        private async void StartDashBoardRefresh()
        {
            var system = new Functions.SystemMetrics();

            // Get CPU info and Set
            await Task.Run(() => system.GetCPUStaticInfo());
            dashboard_cpu_type.Content = system.CPUType;

            // Get RAM info and Set
            await Task.Run(() => system.GetRAMStaticInfo());
            dashboard_ram_type.Content = system.RAMType;

            // Get Disk info and Set
            await Task.Run(() => system.GetDiskStaticInfo());
            dashboard_disk_name.Content = $"({system.DiskName})";
            dashboard_disk_type.Content = system.DiskType;

            while (true)
            {
                dashboard_cpu_bar.Value = await Task.Run(() => system.GetCPUUsage());
                dashboard_cpu_bar.Value = (dashboard_cpu_bar.Value > 100.0) ? 100.0 : dashboard_cpu_bar.Value;
                dashboard_cpu_usage.Content = $"{dashboard_cpu_bar.Value}%";

                dashboard_ram_bar.Value = await Task.Run(() => system.GetRAMUsage());
                dashboard_ram_bar.Value = (dashboard_ram_bar.Value > 100.0) ? 100.0 : dashboard_ram_bar.Value;
                dashboard_ram_usage.Content = $"{string.Format("{0:0.00}", dashboard_ram_bar.Value)}%";
                dashboard_ram_ratio.Content = Functions.SystemMetrics.GetMemoryRatioString(dashboard_ram_bar.Value, system.RAMTotalSize);

                dashboard_disk_bar.Value = await Task.Run(() => system.GetDiskUsage());
                dashboard_disk_bar.Value = (dashboard_disk_bar.Value > 100.0) ? 100.0 : dashboard_disk_bar.Value;
                dashboard_disk_usage.Content = $"{string.Format("{0:0.00}", dashboard_disk_bar.Value)}%";
                dashboard_disk_ratio.Content = Functions.SystemMetrics.GetDiskRatioString(dashboard_disk_bar.Value, system.DiskTotalSize);

                dashboard_servers_bar.Value = ServerGrid.Items.Count * 100.0 / MAX_SERVER;
                dashboard_servers_bar.Value = (dashboard_servers_bar.Value > 100.0) ? 100.0 : dashboard_servers_bar.Value;
                dashboard_servers_usage.Content = $"{string.Format("{0:0.00}", dashboard_servers_bar.Value)}%";
                dashboard_servers_ratio.Content = $"{ServerGrid.Items.Count}/{MAX_SERVER}";

                int startedCount = GetStartedServerCount();
                dashboard_started_bar.Value = ServerGrid.Items.Count == 0 ? 0 : startedCount * 100.0 / ServerGrid.Items.Count;
                dashboard_started_bar.Value = (dashboard_started_bar.Value > 100.0) ? 100.0 : dashboard_started_bar.Value;
                dashboard_started_usage.Content = $"{string.Format("{0:0.00}", dashboard_started_bar.Value)}%";
                dashboard_started_ratio.Content = $"{startedCount}/{ServerGrid.Items.Count}";

                dashboard_players_count.Content = GetActivePlayers().ToString();

                Refresh_DashBoard_LiveChart();

                await Task.Delay(2000);
            }
        }

        public int GetStartedServerCount()
        {
            return ServerGrid.Items.Cast<Functions.ServerTable>().Where(s => s.Status == "Started").Count();
        }

        public int GetActivePlayers()
        {
            return ServerGrid.Items.Cast<Functions.ServerTable>().Where(s => s.Maxplayers != null && s.Maxplayers.Contains('/')).Sum(s => int.Parse(s.Maxplayers.Split('/')[0]));
        }

        private void Refresh_DashBoard_LiveChart()
        {
            // List<(ServerType, PlayerCount)> Example: ("Ricochet Dedicated Server", 0)
            List<(string, int)> typePlayers = ServerGrid.Items.Cast<Functions.ServerTable>()
                .Where(s => s.Status == "Started" && s.Maxplayers != null && s.Maxplayers.Contains("/"))
                .Select(s => (type: s.Game, players: int.Parse(s.Maxplayers.Split('/')[0])))
                .GroupBy(s => s.type)
                .Select(s => (type: s.Key, players: s.Sum(p => p.players)))
                .ToList();

            // Ajust the maxvalue of axis Y base on PlayerCount
            if (typePlayers.Count > 0)
            {
                int maxValue = typePlayers.Select(s => s.Item2).Max() + 5;
                livechart_players_axisY.MaxValue = (maxValue > 10) ? maxValue : 10;
            }

            // Update the column data if updated, if ServerType doesn't exist remove
            for (int i = 0; i < livechart_players.Series.Count; i++)
            {
                if (typePlayers.Select(t => t.Item1).Contains(livechart_players.Series[i].Title))
                {
                    int currentPlayers = typePlayers.Where(t => t.Item1 == livechart_players.Series[i].Title).Select(t => t.Item2).FirstOrDefault();
                    if (((ChartValues<int>)livechart_players.Series[i].Values)[0] != currentPlayers)
                    {
                        livechart_players.Series[i].Values[0] = currentPlayers;
                    }

                    typePlayers.Remove((livechart_players.Series[i].Title, currentPlayers));
                }
                else
                {
                    livechart_players.Series.RemoveAt(i--);
                }
            }

            // Add ServerType Series if not exist
            foreach ((string, int) item in typePlayers)
            {
                livechart_players.Series.Add(new ColumnSeries
                {
                    Title = item.Item1,
                    Values = new ChartValues<int> { item.Item2 }
                });
            }
        }

        private async void SendGoogleAnalytics()
        {
            var analytics = new Functions.GoogleAnalytics();
            analytics.SendWindowsOS();
            analytics.SendWindowsGSMVersion();
            analytics.SendProcessorName();
            analytics.SendRAM();
            analytics.SendDisk();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // Save height and width
            RegistryKey key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\WindowsGSM", true);
            if (key != null)
            {
                key.SetValue("Height", Height.ToString());
                key.SetValue("Width", Width.ToString());
                key.Close();
            }

            // Stop Discord Bot
            g_DiscordBot.Stop().ConfigureAwait(false);
        }

        private void DataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ServerGrid.SelectedIndex != -1)
            {
                DataGrid_RefreshElements();
            }
        }

        private void DataGrid_RefreshElements()
        {
            var row = (Functions.ServerTable)ServerGrid.SelectedItem;

            if (row != null)
            {
                Console.WriteLine("Datagrid Changed");

                if (g_iServerStatus[int.Parse(row.ID)] == ServerStatus.Stopped)
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
                else if (g_iServerStatus[int.Parse(row.ID)] == ServerStatus.Started)
                {
                    button_Start.IsEnabled = false;
                    button_Stop.IsEnabled = true;
                    button_Restart.IsEnabled = true;
                    Process p = g_Process[int.Parse(row.ID)];
                    button_Console.IsEnabled = (p == null || p.HasExited) ? false : !(p.StartInfo.CreateNoWindow || p.StartInfo.RedirectStandardOutput);
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

                switch (g_iServerStatus[int.Parse(row.ID)])
                {
                    case ServerStatus.Restarting:
                    case ServerStatus.Restarted:
                    case ServerStatus.Started:
                    case ServerStatus.Starting:
                    case ServerStatus.Stopping:
                        button_Kill.IsEnabled = true;
                        break;
                    default: button_Kill.IsEnabled = false; break;
                }

                button_ManageAddons.IsEnabled = Functions.ServerAddon.IsGameSupportManageAddons(row.Game);
                if (g_iServerStatus[int.Parse(row.ID)] == ServerStatus.Deleting || g_iServerStatus[int.Parse(row.ID)] == ServerStatus.Restoring)
                {
                    button_ManageAddons.IsEnabled = false;
                }

                slider_ProcessPriority.Value = Functions.CPU.Priority.GetPriorityInteger(g_CPUPriority[int.Parse(row.ID)]);
                textBox_ProcessPriority.Text = Functions.CPU.Priority.GetPriorityByInteger((int)slider_ProcessPriority.Value);

                textBox_SetAffinity.Text = Functions.CPU.Affinity.GetAffinityValidatedString(g_CPUAffinity[int.Parse(row.ID)]);
                string affinity = new string(textBox_SetAffinity.Text.Reverse().ToArray());
                for (int i = 0; i < _checkBoxes.Count; i++)
                {
                    _checkBoxes[i].IsChecked = affinity[i] == '1';
                }

                button_Status.Content = row.Status.ToUpper();
                button_Status.Background = (g_iServerStatus[int.Parse(row.ID)] == ServerStatus.Started) ? System.Windows.Media.Brushes.LimeGreen : System.Windows.Media.Brushes.Orange;

                switch_restartcrontab.IsChecked = g_bRestartCrontab[int.Parse(row.ID)];
                switch_embedconsole.IsChecked = g_bEmbedConsole[int.Parse(row.ID)];
                switch_autorestart.IsChecked = g_bAutoRestart[int.Parse(row.ID)];
                switch_autostart.IsChecked = g_bAutoStart[int.Parse(row.ID)];
                switch_autoupdate.IsChecked = g_bAutoUpdate[int.Parse(row.ID)];
                switch_updateonstart.IsChecked = g_bUpdateOnStart[int.Parse(row.ID)];

                button_discordtest.IsEnabled = g_bDiscordAlert[int.Parse(row.ID)] ? true : false;

                textBox_restartcrontab.Text = g_CrontabFormat[int.Parse(row.ID)];
                textBox_nextcrontab.Text = CrontabSchedule.TryParse(g_CrontabFormat[int.Parse(row.ID)])?.GetNextOccurrence(DateTime.Now).ToString("ddd, MM/dd/yyyy HH:mm:ss");

                MahAppSwitch_AutoStartAlert.IsChecked = g_bAutoStartAlert[int.Parse(row.ID)];
                MahAppSwitch_AutoRestartAlert.IsChecked = g_bAutoRestartAlert[int.Parse(row.ID)];
                MahAppSwitch_AutoUpdateAlert.IsChecked = g_bAutoUpdateAlert[int.Parse(row.ID)];
                MahAppSwitch_RestartCrontabAlert.IsChecked = g_bRestartCrontabAlert[int.Parse(row.ID)];
                MahAppSwitch_CrashAlert.IsChecked = g_bCrashAlert[int.Parse(row.ID)];
            }
        }

        private void Install_Click(object sender, RoutedEventArgs e)
        {
            if (ServerGrid.Items.Count >= MAX_SERVER)
            {
                System.Media.SystemSounds.Beep.Play();
                return;
            }

            MahAppFlyout_InstallGameServer.IsOpen = true;

            if (!progressbar_InstallProgress.IsIndeterminate)
            {
                textbox_InstallServerName.IsEnabled = true;
                comboBox_InstallGameServer.IsEnabled = true;
                progressbar_InstallProgress.IsIndeterminate = false;
                textblock_InstallProgress.Text = string.Empty;
                button_Install.IsEnabled = true;

                ComboBox_InstallGameServer_SelectionChanged(sender, null);

                var newServerConfig = new Functions.ServerConfig(null);
                textbox_InstallServerName.Text = $"WindowsGSM - Server #{newServerConfig.ServerID}";
            }
        }

        private async void Button_Install_Click(object sender, RoutedEventArgs e)
        {
            if (Installer != null)
            {
                if (!Installer.HasExited) { Installer.Kill(); }
                Installer = null;
            }

            var selectedgame = (Images.Row)comboBox_InstallGameServer.SelectedItem;
            if (string.IsNullOrWhiteSpace(textbox_InstallServerName.Text) || selectedgame == null) { return; }

            var newServerConfig = new Functions.ServerConfig(null);
            string installPath = Functions.ServerPath.GetServersServerFiles(newServerConfig.ServerID);
            if (Directory.Exists(installPath))
            {
                try
                {
                    Directory.Delete(installPath, true);
                }
                catch
                {
                    System.Windows.Forms.MessageBox.Show(installPath + " is not accessible!", "ERROR", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }

            Directory.CreateDirectory(installPath);

            //Installation start
            textbox_InstallServerName.IsEnabled = false;
            comboBox_InstallGameServer.IsEnabled = false;
            progressbar_InstallProgress.IsIndeterminate = true;
            textblock_InstallProgress.Text = "Installing";
            button_Install.IsEnabled = false;
            textbox_InstallLog.Text = string.Empty;

            string servername = textbox_InstallServerName.Text;
            string servergame = selectedgame.Name;

            newServerConfig.CreateServerDirectory();

            dynamic gameServer = GameServer.Data.Class.Get(servergame, newServerConfig);
            Installer = await gameServer.Install();

            if (Installer != null)
            {
                //Wait installer exit. Example: steamcmd.exe
                await Task.Run(() =>
                {
                    var reader = Installer.StandardOutput;
                    while (!reader.EndOfStream)
                    {
                        var nextLine = reader.ReadLine();
                        if (nextLine.Contains("Logging in user "))
                        {
                            nextLine += Environment.NewLine + "Please send the Login Token:";
                        }

                        System.Windows.Application.Current.Dispatcher.Invoke(() =>
                        {
                            textbox_InstallLog.AppendText(nextLine + Environment.NewLine);
                            textbox_InstallLog.ScrollToEnd();
                        });
                    }

                    Installer?.WaitForExit();
                });
            }

            if (gameServer.IsInstallValid())
            {
                newServerConfig.ServerIP = newServerConfig.GetIPAddress();
                newServerConfig.ServerPort = newServerConfig.GetAvailablePort(gameServer.Port, gameServer.PortIncrements);

                // Create WindowsGSM.cfg
                newServerConfig.SetData(servergame, servername, gameServer);
                newServerConfig.CreateWindowsGSMConfig();

                // Create WindowsGSM.cfg and game server config
                try
                {
                    gameServer = GameServer.Data.Class.Get(servergame, newServerConfig);
                    gameServer.CreateServerCFG();
                }
                catch
                {
                    // ignore
                }

                LoadServerTable();
                Log(newServerConfig.ServerID, "Install: Success");

                MahAppFlyout_InstallGameServer.IsOpen = false;
                textbox_InstallServerName.IsEnabled = true;
                comboBox_InstallGameServer.IsEnabled = true;
                progressbar_InstallProgress.IsIndeterminate = false;

                if (MahAppSwitch_SendStatistics.IsChecked ?? false)
                {
                    var analytics = new Functions.GoogleAnalytics();
                    analytics.SendGameServerInstall(newServerConfig.ServerID, servergame);
                }
            }
            else
            {
                textbox_InstallServerName.IsEnabled = true;
                comboBox_InstallGameServer.IsEnabled = true;
                progressbar_InstallProgress.IsIndeterminate = false;

                if (Installer != null)
                {
                    textblock_InstallProgress.Text = "Fail to install [ERROR] Exit code: " + Installer.ExitCode.ToString();
                }
                else
                {
                    textblock_InstallProgress.Text = $"Fail to install {gameServer.Error}";
                }
            }
        }

        private void ComboBox_InstallGameServer_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Set the elements visibility of Install Server Flyout
            var selectedgame = (Images.Row)comboBox_InstallGameServer.SelectedItem;
            button_InstallSetAccount.IsEnabled = false;
            textBox_InstallToken.Visibility = Visibility.Hidden;
            button_InstallSendToken.Visibility = Visibility.Hidden;
            if (selectedgame == null) { return; }

            try
            {
                dynamic gameServer = GameServer.Data.Class.Get(selectedgame.Name);
                if (gameServer.requireSteamAccount)
                {
                    button_InstallSetAccount.IsEnabled = true;
                    textBox_InstallToken.Visibility = Visibility.Visible;
                    button_InstallSendToken.Visibility = Visibility.Visible;
                }
            }
            catch
            {
                // ignore
            }
        }

        private void Button_SetAccount_Click(object sender, RoutedEventArgs e)
        {
            var steamCMD = new Installer.SteamCMD();
            steamCMD.CreateUserDataTxtIfNotExist();

            string userDataPath = Path.Combine(WGSM_PATH, @"installer\steamcmd\userData.txt");
            if (File.Exists(userDataPath))
            {
                Process.Start(userDataPath);
            }
        }

        private void Button_SendToken_Click(object sender, RoutedEventArgs e)
        {
            if (Installer != null)
            {
                Installer.StandardInput.WriteLine(textBox_InstallToken.Text);
            }

            textBox_InstallToken.Text = string.Empty;
        }

        private void Import_Click(object sender, RoutedEventArgs e)
        {
            if (ServerGrid.Items.Count >= MAX_SERVER)
            {
                System.Media.SystemSounds.Beep.Play();
                return;
            }

            MahAppFlyout_ImportGameServer.IsOpen = true;

            if (!progressbar_ImportProgress.IsIndeterminate)
            {
                textbox_ImportServerName.IsEnabled = true;
                comboBox_ImportGameServer.IsEnabled = true;
                progressbar_ImportProgress.IsIndeterminate = false;
                textblock_ImportProgress.Text = string.Empty;
                button_Import.Content = "Import";

                var newServerConfig = new Functions.ServerConfig(null);
                textbox_ImportServerName.Text = $"WindowsGSM - Server #{newServerConfig.ServerID}";
            }
        }

        private async void Button_Import_Click(object sender, RoutedEventArgs e)
        {
            var selectedgame = (Images.Row)comboBox_ImportGameServer.SelectedItem;
            label_ServerDirWarn.Content = Directory.Exists(textbox_ServerDir.Text) ? string.Empty : "Server Dir is invalid";
            if (string.IsNullOrWhiteSpace(textbox_ImportServerName.Text) || selectedgame == null) { return; }

            string servername = textbox_ImportServerName.Text;
            string servergame = selectedgame.Name;

            var newServerConfig = new Functions.ServerConfig(null);
            dynamic gameServer = GameServer.Data.Class.Get(servergame, newServerConfig);

            if (!gameServer.IsImportValid(textbox_ServerDir.Text))
            {
                label_ServerDirWarn.Content = gameServer.Error;
                return;
            }

            string importPath = Functions.ServerPath.GetServersServerFiles(newServerConfig.ServerID);
            if (Directory.Exists(importPath))
            {
                try
                {
                    Directory.Delete(importPath, true);
                }
                catch
                {
                    System.Windows.Forms.MessageBox.Show(importPath + " is not accessible!", "ERROR", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }

            //Import start
            textbox_ImportServerName.IsEnabled = false;
            comboBox_ImportGameServer.IsEnabled = false;
            textbox_ServerDir.IsEnabled = false;
            button_Browse.IsEnabled = false;
            progressbar_ImportProgress.IsIndeterminate = true;
            textblock_ImportProgress.Text = "Importing";

            string sourcePath = textbox_ServerDir.Text;
            string importLog = await Task.Run(() =>
            {
                try
                {
                    Microsoft.VisualBasic.FileIO.FileSystem.CopyDirectory(sourcePath, importPath);

                    // Scary error while moving the directory, some files may lost - Risky
                    //Microsoft.VisualBasic.FileIO.FileSystem.MoveDirectory(sourcePath, importPath);

                    // This doesn't work on cross drive - Not working on cross drive
                    //Directory.Move(sourcePath, importPath);

                    return null;
                }
                catch (Exception ex)
                {
                    return ex.Message;         
                }
            });

            if (importLog != null)
            {
                textbox_ImportServerName.IsEnabled = true;
                comboBox_ImportGameServer.IsEnabled = true;
                textbox_ServerDir.IsEnabled = true;
                button_Browse.IsEnabled = true;
                progressbar_ImportProgress.IsIndeterminate = false;
                textblock_ImportProgress.Text = "[ERROR] Fail to import";
                System.Windows.MessageBox.Show($"Fail to copy the directory.\n{textbox_ServerDir.Text}\nto\n{importPath}\n\nYou may install a new server and copy the old servers file to the new server.\n\nException: {importLog}", "ERROR", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Create WindowsGSM.cfg
            newServerConfig.SetData(servergame, servername, gameServer);
            newServerConfig.CreateWindowsGSMConfig();

            LoadServerTable();
            Log(newServerConfig.ServerID, "Import: Success");

            MahAppFlyout_ImportGameServer.IsOpen = false;
            textbox_ImportServerName.IsEnabled = true;
            comboBox_ImportGameServer.IsEnabled = true;
            textbox_ServerDir.IsEnabled = true;
            button_Browse.IsEnabled = true;
            progressbar_ImportProgress.IsIndeterminate = false;
            textblock_ImportProgress.Text = string.Empty;
        }

        private void Button_Browse_Click(object sender, RoutedEventArgs e)
        {
            FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog();
            folderBrowserDialog.ShowDialog();

            if (!string.IsNullOrWhiteSpace(folderBrowserDialog.SelectedPath))
            {
                textbox_ServerDir.Text = folderBrowserDialog.SelectedPath;
            }
        }

        private async void Delete_Click(object sender, RoutedEventArgs e)
        {
            var server = (Functions.ServerTable)ServerGrid.SelectedItem;
            if (server == null) { return; }

            if (g_iServerStatus[Int32.Parse(server.ID)] != ServerStatus.Stopped) { return; }

            MessageBoxResult result = System.Windows.MessageBox.Show("Do you want to delete this server?\n(There is no comeback)", "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result != MessageBoxResult.Yes) { return; }

            await GameServer_Delete(server);
        }

        private async void Button_DiscordEdit_Click(object sender, RoutedEventArgs e)
        {
            var server = (Functions.ServerTable)ServerGrid.SelectedItem;
            if (server == null) { return; }

            string webhookUrl = Functions.ServerConfig.GetSetting(server.ID, Functions.SettingName.DiscordWebhook);

            var settings = new MetroDialogSettings
            {
                AffirmativeButtonText = "Save",
                DefaultText = webhookUrl
            };

            webhookUrl = await this.ShowInputAsync("Discord Webhook URL", "Please enter the discord webhook url.", settings);
            if (webhookUrl == null) { return; } //If pressed cancel

            g_DiscordWebhook[Int32.Parse(server.ID)] = webhookUrl;
            Functions.ServerConfig.SetSetting(server.ID, Functions.SettingName.DiscordWebhook, webhookUrl);
        }

        private async void Button_DiscordSetMessage_Click(object sender, RoutedEventArgs e)
        {
            var server = (Functions.ServerTable)ServerGrid.SelectedItem;
            if (server == null) { return; }

            string message = Functions.ServerConfig.GetSetting(server.ID, Functions.SettingName.DiscordMessage);

            var settings = new MetroDialogSettings
            {
                AffirmativeButtonText = "Save",
                DefaultText = message
            };

            message = await this.ShowInputAsync("Discord Custom Message", "Please enter the custom message.\n\nExample ping message <@discorduserid>:\n<@348921660361146380>", settings);
            if (message == null) { return; } //If pressed cancel

            g_DiscordMessage[int.Parse(server.ID)] = message;
            Functions.ServerConfig.SetSetting(server.ID, Functions.SettingName.DiscordMessage, message);
        }

        private async void Button_DiscordWebhookTest_Click(object sender, RoutedEventArgs e)
        {
            var server = (Functions.ServerTable)ServerGrid.SelectedItem;
            if (server == null) { return; }

            int serverId = int.Parse(server.ID);
            if (!g_bDiscordAlert[serverId]) { return; }

            var webhook = new Functions.DiscordWebhook(g_DiscordWebhook[serverId], g_DiscordMessage[serverId], g_DonorType);
            await webhook.Send(server.ID, server.Game, "Webhook Test Alert", server.Name, server.IP, server.Port);
        }

        private void Button_ServerCommand_Click(object sender, RoutedEventArgs e)
        {
            string command = textbox_servercommand.Text;
            textbox_servercommand.Text = string.Empty;

            if (string.IsNullOrWhiteSpace(command)) { return; }

            var server = (Functions.ServerTable)ServerGrid.SelectedItem;
            if (server == null) { return; }

            SendCommand(server, command);
        }

        private void Textbox_ServerCommand_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                if (textbox_servercommand.Text.Length != 0)
                {
                    g_ServerConsoles[0].Add(textbox_servercommand.Text);
                }

                Button_ServerCommand_Click(this, new RoutedEventArgs());
            }
        }

        private void Textbox_ServerCommand_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.IsDown && e.Key == Key.Up)
            {
                e.Handled = true;
                textbox_servercommand.Text = g_ServerConsoles[0].GetPreviousCommand();
            }
            else if (e.IsDown && e.Key == Key.Down)
            {
                e.Handled = true;
                textbox_servercommand.Text = g_ServerConsoles[0].GetNextCommand();
            }
        }

        #region Actions - Button Click
        private async void Actions_Start_Click(object sender, RoutedEventArgs e)
        {
            var server = (Functions.ServerTable)ServerGrid.SelectedItem;
            if (server == null) { return; }

            //Reload WindowsGSM.cfg on start
            int i = int.Parse(server.ID);
            var serverConfig = new Functions.ServerConfig(i.ToString());
            g_bAutoRestart[i] = serverConfig.AutoRestart;
            g_bAutoStart[i] = serverConfig.AutoStart;
            g_bAutoUpdate[i] = serverConfig.AutoUpdate;
            g_bUpdateOnStart[i] = serverConfig.UpdateOnStart;
            g_bDiscordAlert[i] = serverConfig.DiscordAlert;
            g_DiscordMessage[i] = serverConfig.DiscordMessage;
            g_DiscordWebhook[i] = serverConfig.DiscordWebhook;
            g_bRestartCrontab[i] = serverConfig.RestartCrontab;
            g_CrontabFormat[i] = serverConfig.CrontabFormat;
            g_bEmbedConsole[i] = serverConfig.EmbedConsole;
            g_bAutoStartAlert[i] = serverConfig.AutoStartAlert;
            g_bAutoRestartAlert[i] = serverConfig.AutoRestartAlert;
            g_bAutoUpdateAlert[i] = serverConfig.AutoUpdateAlert;
            g_bRestartCrontabAlert[i] = serverConfig.RestartCrontabAlert;
            g_bCrashAlert[i] = serverConfig.CrashAlert;
            g_CPUPriority[i] = serverConfig.CPUPriority;
            g_CPUAffinity[i] = serverConfig.CPUAffinity;

            await GameServer_Start(server);
        }

        private async void Actions_Stop_Click(object sender, RoutedEventArgs e)
        {
            var server = (Functions.ServerTable)ServerGrid.SelectedItem;
            if (server == null) { return; }

            await GameServer_Stop(server);
        }

        private async void Actions_Restart_Click(object sender, RoutedEventArgs e)
        {
            var server = (Functions.ServerTable)ServerGrid.SelectedItem;
            if (server == null) { return; }

            await GameServer_Restart(server);
        }

        private async void Actions_Kill_Click(object sender, RoutedEventArgs e)
        {
            var server = (Functions.ServerTable)ServerGrid.SelectedItem;
            if (server == null) { return; }

            switch (g_iServerStatus[int.Parse(server.ID)])
            {
                case ServerStatus.Restarting:
                case ServerStatus.Restarted:
                case ServerStatus.Started:
                case ServerStatus.Starting:
                case ServerStatus.Stopping:
                    Process p = g_Process[int.Parse(server.ID)];
                    if (p != null && !p.HasExited)
                    {
                        Log(server.ID, "Actions: Kill");
                        p.Kill();

                        g_iServerStatus[int.Parse(server.ID)] = ServerStatus.Stopped;
                        Log(server.ID, "Server: Killed");
                        SetServerStatus(server, "Stopped");
                        g_ServerConsoles[int.Parse(server.ID)].Clear();
                        g_Process[int.Parse(server.ID)] = null;
                    }

                    break;
            }
        }

        private void Actions_ToggleConsole_Click(object sender, RoutedEventArgs e)
        {
            var server = (Functions.ServerTable)ServerGrid.SelectedItem;
            if (server == null) { return; }

            Process p = g_Process[int.Parse(server.ID)];
            if (p == null) { return; }

            //If console is useless, return
            if (p.StartInfo.RedirectStandardOutput) { return; }

            IntPtr hWnd = g_MainWindow[int.Parse(server.ID)];
            ShowWindow(hWnd, ShowWindow(hWnd, WindowShowStyle.Hide) ? WindowShowStyle.Hide : WindowShowStyle.ShowNormal);
        }

        private async void Actions_StartAllServers_Click(object sender, RoutedEventArgs e)
        {
            List<Functions.ServerTable> servers = new List<Functions.ServerTable>();
            foreach (var item in ServerGrid.Items) { servers.Add((Functions.ServerTable)item); }

            foreach (var server in servers)
            {
                if (g_iServerStatus[int.Parse(server.ID)] == ServerStatus.Stopped)
                {
                    await GameServer_Start(server);
                }
            }
        }

        private async void Actions_StartServersWithAutoStartEnabled_Click(object sender, RoutedEventArgs e)
        {
            List<Functions.ServerTable> servers = new List<Functions.ServerTable>();
            foreach (var item in ServerGrid.Items) { servers.Add((Functions.ServerTable)item); }

            foreach (var server in servers)
            {
                if (g_iServerStatus[int.Parse(server.ID)] == ServerStatus.Stopped && g_bAutoStart[int.Parse(server.ID)])
                {
                    await GameServer_Start(server);
                }
            }
        }

        private async void Actions_StopAllServers_Click(object sender, RoutedEventArgs e)
        {
            List<Functions.ServerTable> servers = new List<Functions.ServerTable>();
            foreach (var item in ServerGrid.Items) { servers.Add((Functions.ServerTable)item); }

            foreach (var server in servers)
            {
                if (g_iServerStatus[int.Parse(server.ID)] == ServerStatus.Started)
                {
                    await GameServer_Stop(server);
                }
            }
        }

        private async void Actions_RestartAllServers_Click(object sender, RoutedEventArgs e)
        {
            List<Functions.ServerTable> servers = new List<Functions.ServerTable>();
            foreach (var item in ServerGrid.Items) { servers.Add((Functions.ServerTable)item); }

            foreach (var server in servers)
            {
                if (g_iServerStatus[int.Parse(server.ID)] == ServerStatus.Started)
                {
                    await GameServer_Restart(server);
                }
            }
        }

        private async void Actions_Update_Click(object sender, RoutedEventArgs e)
        {
            var server = (Functions.ServerTable)ServerGrid.SelectedItem;
            if (server == null) { return; }

            if (g_iServerStatus[int.Parse(server.ID)] != ServerStatus.Stopped) { return; }

            MessageBoxResult result = System.Windows.MessageBox.Show("Do you want to update this server?", "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result != MessageBoxResult.Yes) { return; }

            await GameServer_Update(server);
        }

        private async void Actions_UpdateValidate_Click(object sender, RoutedEventArgs e)
        {
            var server = (Functions.ServerTable)ServerGrid.SelectedItem;
            if (server == null) { return; }

            if (g_iServerStatus[int.Parse(server.ID)] != ServerStatus.Stopped) { return; }

            MessageBoxResult result = System.Windows.MessageBox.Show("Do you want to validate this server?", "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result != MessageBoxResult.Yes) { return; }

            await GameServer_Update(server, notes: " | Validate", validate: true);
        }

        private async void Actions_Backup_Click(object sender, RoutedEventArgs e)
        {
            var server = (Functions.ServerTable)ServerGrid.SelectedItem;
            if (server == null) { return; }

            if (g_iServerStatus[int.Parse(server.ID)] != ServerStatus.Stopped) { return; }

            MessageBoxResult result = System.Windows.MessageBox.Show("Do you want to backup on this server?", "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result != MessageBoxResult.Yes) { return; }

            await GameServer_Backup(server);
        }

        private async void Actions_RestoreBackup_Click(object sender, RoutedEventArgs e)
        {
            var server = (Functions.ServerTable)ServerGrid.SelectedItem;
            if (server == null) { return; }

            if (g_iServerStatus[int.Parse(server.ID)] != ServerStatus.Stopped) { return; }

            MessageBoxResult result = System.Windows.MessageBox.Show("Do you want to restore backup on this server?\n(All server files will be overwritten)", "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result != MessageBoxResult.Yes) { return; }

            await GameServer_RestoreBackup(server);
        }

        private void Actions_ManageAddons_Click(object sender, RoutedEventArgs e)
        {
            var server = (Functions.ServerTable)ServerGrid.SelectedItem;
            if (server == null) { return; }

            ListBox_ManageAddons_Refresh();

            MahAppFlyout_DiscordAlert.IsOpen = false;
            MahAppFlyout_SetAffinity.IsOpen = false;
            MahAppFlyout_ManageAddons.IsOpen = !MahAppFlyout_ManageAddons.IsOpen;
        }
        #endregion

        private void ListBox_ManageAddonsLeft_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (listBox_ManageAddonsLeft.SelectedItem != null)
            { 
                var server = (Functions.ServerTable)ServerGrid.SelectedItem;
                if (server == null) { return; }

                string item = listBox_ManageAddonsLeft.SelectedItem.ToString();
                listBox_ManageAddonsLeft.Items.Remove(listBox_ManageAddonsLeft.Items[listBox_ManageAddonsLeft.SelectedIndex]);
                listBox_ManageAddonsRight.Items.Add(item);
                var serverAddon = new Functions.ServerAddon(server.ID, server.Game);
                serverAddon.AddToRight(listBox_ManageAddonsRight.Items.OfType<string>().ToList(), item);

                ListBox_ManageAddons_Refresh();

                foreach (var selected in listBox_ManageAddonsRight.Items)
                {
                    if (selected.ToString() == item)
                    {
                        listBox_ManageAddonsRight.SelectedItem = selected;
                    }
                }
            }
        }

        private void ListBox_ManageAddonsRight_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (listBox_ManageAddonsRight.SelectedItem != null)
            {
                var server = (Functions.ServerTable)ServerGrid.SelectedItem;
                if (server == null) { return; }

                string item = listBox_ManageAddonsRight.SelectedItem.ToString();
                listBox_ManageAddonsRight.Items.Remove(listBox_ManageAddonsRight.Items[listBox_ManageAddonsRight.SelectedIndex]);
                listBox_ManageAddonsLeft.Items.Add(item);
                var serverAddon = new Functions.ServerAddon(server.ID, server.Game);
                serverAddon.AddToLeft(listBox_ManageAddonsRight.Items.OfType<string>().ToList(), item);

                ListBox_ManageAddons_Refresh();

                foreach (var selected in listBox_ManageAddonsLeft.Items)
                {
                    if (selected.ToString() == item)
                    {
                        listBox_ManageAddonsLeft.SelectedItem = selected;
                    }
                }
            }
        }

        private void ListBox_ManageAddons_Refresh()
        {
            var server = (Functions.ServerTable)ServerGrid.SelectedItem;
            if (server == null) { return; }

            var serverAddon = new Functions.ServerAddon(server.ID, server.Game);
            label_ManageAddonsName.Content = server.Name;
            label_ManageAddonsGame.Content = server.Game;
            label_ManageAddonsType.Content = serverAddon.GetModsName();

            listBox_ManageAddonsLeft.Items.Clear();
            foreach (string item in serverAddon.GetLeftListBox())
            {
                listBox_ManageAddonsLeft.Items.Add(item);
            }

            listBox_ManageAddonsRight.Items.Clear();
            foreach (string item in serverAddon.GetRightListBox())
            {
                listBox_ManageAddonsRight.Items.Add(item);
            }
        }

        private async Task<dynamic> Server_BeginStart(Functions.ServerTable server)
        {
            dynamic gameServer = GameServer.Data.Class.Get(server.Game, new Functions.ServerConfig(server.ID));
            if (gameServer == null) { return null; }

            //End All Running Process
            await EndAllRunningProcess(server.ID);
            await Task.Delay(500);

            //Add Start File to WindowsFirewall before start
            string startPath = Functions.ServerPath.GetServersServerFiles(server.ID, gameServer.StartPath);
            if (!string.IsNullOrWhiteSpace(gameServer.StartPath))
            {
                WindowsFirewall firewall = new WindowsFirewall(Path.GetFileName(startPath), startPath);
                if (!await firewall.IsRuleExist())
                {
                    firewall.AddRule();
                }
            }

            gameServer.ToggleConsole = !g_bEmbedConsole[int.Parse(server.ID)];
            Process p = await gameServer.Start();

            //Fail to start
            if (p == null)
            {
                g_iServerStatus[int.Parse(server.ID)] = ServerStatus.Stopped;
                Log(server.ID, "Server: Fail to start");
                Log(server.ID, "[ERROR] " + gameServer.Error);
                SetServerStatus(server, "Stopped");

                return null;
            }

            g_Process[int.Parse(server.ID)] = p;
            p.Exited += (sender, e) => OnGameServerExited(server);

            await Task.Run(() =>
            {
                try
                {
                    if (!p.StartInfo.CreateNoWindow)
                    {
                        while (!p.HasExited && !ShowWindow(p.MainWindowHandle, WindowShowStyle.Minimize))
                        {
                            //Debug.WriteLine("Try Setting ShowMinNoActivate Console Window");
                        }

                        Debug.WriteLine("Set ShowMinNoActivate Console Window");

                        //Save MainWindow
                        g_MainWindow[int.Parse(server.ID)] = p.MainWindowHandle;
                    }

                    p.WaitForInputIdle();

                    if (!p.StartInfo.CreateNoWindow)
                    {
                        ShowWindow(p.MainWindowHandle, WindowShowStyle.Hide);
                    }
                }
                catch
                {
                    Debug.WriteLine("No Window require to hide");
                }
            });

            //An error may occur on ShowWindow if not adding this 
            if (p == null || p.HasExited)
            {
                g_Process[int.Parse(server.ID)] = null;

                g_iServerStatus[int.Parse(server.ID)] = ServerStatus.Stopped;
                Log(server.ID, "Server: Fail to start");
                Log(server.ID, "[ERROR] Exit Code: " + p.ExitCode.ToString());
                SetServerStatus(server, "Stopped");

                return null;
            }

            // Set Priority
            p = Functions.CPU.Priority.SetProcessWithPriority(p, Functions.CPU.Priority.GetPriorityInteger(g_CPUPriority[int.Parse(server.ID)]));

            // Set Affinity
            p.ProcessorAffinity = Functions.CPU.Affinity.GetAffinityIntPtr(Functions.CPU.Affinity.GetAffinityValidatedString(g_CPUAffinity[int.Parse(server.ID)]));

            // Save Cache
            Functions.ServerCache.SavePID(server.ID, p.Id);
            Functions.ServerCache.SaveProcessName(server.ID, p.ProcessName);
            Functions.ServerCache.SaveWindowsIntPtr(server.ID, g_MainWindow[int.Parse(server.ID)]);

            SetWindowText(p.MainWindowHandle, server.Name);

            ShowWindow(p.MainWindowHandle, WindowShowStyle.Hide);

            StartAutoUpdateCheck(server);

            StartRestartCrontabCheck(server);

            StartSendHeartBeat(server);

            StartQuery(server);

            if (MahAppSwitch_SendStatistics.IsChecked ?? false)
            {
                var analytics = new Functions.GoogleAnalytics();
                analytics.SendGameServerStart(server.ID, server.Game);
            }

            return gameServer;
        }

        private async Task<bool> Server_BeginStop(Functions.ServerTable server, Process p)
        {
            g_Process[int.Parse(server.ID)] = null;

            dynamic gameServer = GameServer.Data.Class.Get(server.Game);
            await gameServer.Stop(p);

            for (int i = 0; i < 10; i++)
            {
                if (p == null || p.HasExited) { break; }
                await Task.Delay(1000);
            }

            g_ServerConsoles[int.Parse(server.ID)].Clear();

            // Save Cache
            Functions.ServerCache.SavePID(server.ID, -1);
            Functions.ServerCache.SaveProcessName(server.ID, string.Empty);
            Functions.ServerCache.SaveWindowsIntPtr(server.ID, (IntPtr)0);

            if (p != null && !p.HasExited)
            {
                p.Kill();
                return false;
            }

            return true;
        }

        private async Task<(bool, string, dynamic)> Server_BeginUpdate(Functions.ServerTable server, bool silenceCheck, bool forceUpdate, bool validate = false)
        {
            dynamic gameServer = GameServer.Data.Class.Get(server.Game, new Functions.ServerConfig(server.ID));

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
                    return (await gameServer.Update(validate), remoteVersion, gameServer);
                }
                catch
                {
                    return (await gameServer.Update(), remoteVersion, gameServer);
                }
            }

            return (true, remoteVersion, gameServer);
        }

        #region Actions - Game Server
        private async Task GameServer_Start(Functions.ServerTable server, string notes = "")
        {
            if (g_iServerStatus[int.Parse(server.ID)] != ServerStatus.Stopped) { return; }

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

            Process p = g_Process[int.Parse(server.ID)];
            if (p != null) { return; }

            if (g_bUpdateOnStart[int.Parse(server.ID)])
            {
                await GameServer_Update(server, " | Update on Start");
            }

            g_iServerStatus[int.Parse(server.ID)] = ServerStatus.Starting;
            Log(server.ID, "Action: Start" + notes);
            SetServerStatus(server, "Starting");

            var gameServer = await Server_BeginStart(server);
            if (gameServer == null)
            {
                g_iServerStatus[int.Parse(server.ID)] = ServerStatus.Stopped;
                Log(server.ID, "Server: Fail to start");
                SetServerStatus(server, "Stopped");
                return;
            }

            g_iServerStatus[int.Parse(server.ID)] = ServerStatus.Started;
            Log(server.ID, "Server: Started");
            if (!string.IsNullOrWhiteSpace(gameServer.Notice))
            {
                Log(server.ID, "[Notice] " + gameServer.Notice);
            }
            SetServerStatus(server, "Started");
        }

        private async Task GameServer_Stop(Functions.ServerTable server)
        {
            if (g_iServerStatus[int.Parse(server.ID)] != ServerStatus.Started) { return; }

            Process p = g_Process[int.Parse(server.ID)];
            if (p == null) { return; }

            //Begin stop
            g_iServerStatus[int.Parse(server.ID)] = ServerStatus.Stopping;
            Log(server.ID, "Action: Stop");
            SetServerStatus(server, "Stopping");

            bool stopGracefully = await Server_BeginStop(server, p);

            Log(server.ID, "Server: Stopped");
            if (!stopGracefully)
            {
                Log(server.ID, "[NOTICE] Server fail to stop gracefully");
            }
            g_iServerStatus[int.Parse(server.ID)] = ServerStatus.Stopped;
            SetServerStatus(server, "Stopped");
        }

        private async Task GameServer_Restart(Functions.ServerTable server)
        {
            if (g_iServerStatus[Int32.Parse(server.ID)] != ServerStatus.Started) { return; }

            Process p = g_Process[Int32.Parse(server.ID)];
            if (p == null) { return; }

            g_Process[Int32.Parse(server.ID)] = null;

            //Begin Restart
            g_iServerStatus[Int32.Parse(server.ID)] = ServerStatus.Restarting;
            Log(server.ID, "Action: Restart");
            SetServerStatus(server, "Restarting");

            await Server_BeginStop(server, p);

            await Task.Delay(1000);

            var gameServer = await Server_BeginStart(server);
            if (gameServer == null)
            {
                g_iServerStatus[Int32.Parse(server.ID)] = ServerStatus.Stopped;
                SetServerStatus(server, "Stopped");
                return;
            }

            g_iServerStatus[Int32.Parse(server.ID)] = ServerStatus.Started;
            Log(server.ID, "Server: Restarted");
            if (!string.IsNullOrWhiteSpace(gameServer.Notice))
            {
                Log(server.ID, "[Notice] " + gameServer.Notice);
            }
            SetServerStatus(server, "Started");
        }
       
        private async Task<bool> GameServer_Update(Functions.ServerTable server, string notes = "", bool validate = false)
        {
            if (g_iServerStatus[int.Parse(server.ID)] != ServerStatus.Stopped)
            {
                return false;
            }

            //Begin Update
            g_iServerStatus[int.Parse(server.ID)] = ServerStatus.Updating;
            Log(server.ID, "Action: Update" + notes);
            SetServerStatus(server, "Updating");

            (bool updated, string remoteVersion, dynamic gameServer) = await Server_BeginUpdate(server, silenceCheck: validate, forceUpdate: true, validate: validate);

            if (updated)
            {
                Log(server.ID, $"Server: Updated {(validate ? "Validate " : string.Empty)}({remoteVersion})");
            }
            else
            {
                Log(server.ID, "Server: Fail to update");
                Log(server.ID, "[ERROR] " + gameServer.Error);
            }

            g_iServerStatus[int.Parse(server.ID)] = ServerStatus.Stopped;
            SetServerStatus(server, "Stopped");

            return true;
        }

        private async Task<bool> GameServer_Backup(Functions.ServerTable server)
        {
            if (g_iServerStatus[Int32.Parse(server.ID)] != ServerStatus.Stopped)
            {
                return false;
            }

            //Begin backup
            g_iServerStatus[Int32.Parse(server.ID)] = ServerStatus.Backuping;
            Log(server.ID, "Action: Backup");
            SetServerStatus(server, "Backuping");

            //End All Running Process
            await EndAllRunningProcess(server.ID);
            await Task.Delay(1000);

            string startPath = Functions.ServerPath.GetServers(server.ID);
            string zipPath = Functions.ServerPath.GetBackups(server.ID);
            string zipFile = Path.Combine(zipPath, $"Backup-id-{server.ID}.zip");

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

        private async Task<bool> GameServer_RestoreBackup(Functions.ServerTable server)
        {
            if (g_iServerStatus[Int32.Parse(server.ID)] != ServerStatus.Stopped)
            {
                return false;
            }

            string zipFile = WGSM_PATH + @"\Backups\" + server.ID + @"\backup-id-" + server.ID + ".zip";
            string extractPath = WGSM_PATH + @"\Servers\" + server.ID;

            if (!File.Exists(zipFile))
            {
                Log(server.ID, "Server: Fail to restore backup");
                Log(server.ID, "[ERROR] Backup not found");

                return false;
            }

            //End All Running Process
            await EndAllRunningProcess(server.ID);
            await Task.Delay(1000);

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

            g_iServerStatus[Int32.Parse(server.ID)] = ServerStatus.Restoring;
            Log(server.ID, "Action: Restore Backup");
            SetServerStatus(server, "Restoring");

            await Task.Run(() => ZipFile.ExtractToDirectory(zipFile, extractPath));

            g_iServerStatus[Int32.Parse(server.ID)] = ServerStatus.Stopped;
            Log(server.ID, "Server: Restored");
            SetServerStatus(server, "Stopped");

            return true;
        }

        private async Task<bool> GameServer_Delete(Functions.ServerTable server)
        {
            if (g_iServerStatus[int.Parse(server.ID)] != ServerStatus.Stopped)
            {
                return false;
            }

            //Begin delete
            g_iServerStatus[int.Parse(server.ID)] = ServerStatus.Deleting;
            Log(server.ID, "Action: Delete");
            SetServerStatus(server, "Deleting");

            //Remove firewall rule
            var firewall = new WindowsFirewall(null, Functions.ServerPath.GetServers(server.ID));
            firewall.RemoveRuleEx();

            //End All Running Process
            await EndAllRunningProcess(server.ID);
            await Task.Delay(1000);

            string serverPath = Functions.ServerPath.GetServers(server.ID);

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
                string wgsmCfgPath = Functions.ServerPath.GetServersConfigs(server.ID, "WindowsGSM.cfg");
                if (File.Exists(wgsmCfgPath))
                {
                    Log(server.ID, "Server: Fail to delete server");
                    Log(server.ID, "[ERROR] Directory is not accessible");

                    g_iServerStatus[int.Parse(server.ID)] = ServerStatus.Stopped;
                    SetServerStatus(server, "Stopped");

                    return false;
                }
            }

            Log(server.ID, "Server: Deleted server");

            g_iServerStatus[int.Parse(server.ID)] = ServerStatus.Stopped;
            SetServerStatus(server, "Stopped");

            LoadServerTable();

            return true;
        }
        #endregion

        private async void OnGameServerExited(Functions.ServerTable server)
        {
            if (System.Windows.Application.Current == null) { return; }

            await System.Windows.Application.Current.Dispatcher.Invoke(async () =>
            {
                int serverId = int.Parse(server.ID);

                if (g_iServerStatus[serverId] == ServerStatus.Started)
                {
                    bool autoRestart = g_bAutoRestart[serverId];
                    g_iServerStatus[serverId] = autoRestart ? ServerStatus.Restarting : ServerStatus.Stopped;
                    Log(server.ID, "Server: Crashed");
                    SetServerStatus(server, autoRestart ? "Restarting" : "Stopped");

                    if (g_bDiscordAlert[serverId] && g_bCrashAlert[serverId])
                    {
                        var webhook = new Functions.DiscordWebhook(g_DiscordWebhook[serverId], g_DiscordMessage[serverId], g_DonorType);
                        await webhook.Send(server.ID, server.Game, "Crashed", server.Name, server.IP, server.Port);
                    }

                    g_Process[serverId] = null;

                    if (autoRestart)
                    {
                        await Task.Delay(1000);

                        var gameServer = await Server_BeginStart(server);
                        if (gameServer == null)
                        {
                            g_iServerStatus[serverId] = ServerStatus.Stopped;
                            return;
                        }

                        g_iServerStatus[serverId] = ServerStatus.Started;
                        Log(server.ID, "Server: Started | Auto Restart");
                        if (!string.IsNullOrWhiteSpace(gameServer.Notice))
                        {
                            Log(server.ID, "[Notice] " + gameServer.Notice);
                        }
                        SetServerStatus(server, "Started");

                        if (g_bDiscordAlert[serverId] && g_bAutoRestartAlert[serverId])
                        {
                            var webhook = new Functions.DiscordWebhook(g_DiscordWebhook[serverId], g_DiscordMessage[serverId], g_DonorType);
                            await webhook.Send(server.ID, server.Game, "Restarted | Auto Restart", server.Name, server.IP, server.Port);
                        }
                    }
                }
            });
        }

        const int UPDATE_INTERVAL_MINUTE = 30;
        private async void StartAutoUpdateCheck(Functions.ServerTable server)
        {
            int serverId = int.Parse(server.ID);

            //Save the process of game server
            Process p = g_Process[serverId];

            dynamic gameServer = GameServer.Data.Class.Get(server.Game, new Functions.ServerConfig(server.ID));

            string localVersion = gameServer.GetLocalBuild();

            while (p != null && !p.HasExited)
            {
                await Task.Delay(60000 * UPDATE_INTERVAL_MINUTE);

                if (!g_bAutoUpdate[serverId] || g_iServerStatus[serverId] == ServerStatus.Updating)
                {
                    continue;
                }

                if (p == null || p.HasExited) { break; }

                //Try to get local build again if not found just now
                if (string.IsNullOrWhiteSpace(localVersion))
                {
                    localVersion = gameServer.GetLocalBuild();
                }

                //Get remote build
                string remoteVersion = await gameServer.GetRemoteBuild();

                //Continue if success to get localVersion and remoteVersion
                if (!string.IsNullOrWhiteSpace(localVersion) && !string.IsNullOrWhiteSpace(remoteVersion))
                {
                    if (g_iServerStatus[serverId] != ServerStatus.Started)
                    {
                        break;
                    }

                    Log(server.ID, $"Checking: Version ({localVersion}) => ({remoteVersion})");

                    if (localVersion != remoteVersion)
                    {
                        g_Process[serverId] = null;

                        //Begin stop
                        g_iServerStatus[serverId] = ServerStatus.Stopping;
                        SetServerStatus(server, "Stopping");

                        //Stop the server
                        await Server_BeginStop(server, p);

                        if (p != null && !p.HasExited)
                        {
                            p.Kill();
                        }

                        g_iServerStatus[serverId] = ServerStatus.Updating;
                        SetServerStatus(server, "Updating");

                        //Update the server
                        bool updated = await gameServer.Update();

                        if (updated)
                        {
                            Log(server.ID, $"Server: Updated ({remoteVersion})");

                            if (g_bDiscordAlert[serverId] && g_bAutoUpdateAlert[serverId])
                            {
                                var webhook = new Functions.DiscordWebhook(g_DiscordWebhook[serverId], g_DiscordMessage[serverId], g_DonorType);
                                await webhook.Send(server.ID, server.Game, "Updated | Auto Update", server.Name, server.IP, server.Port);
                            }
                        }
                        else
                        {
                            Log(server.ID, "Server: Fail to update");
                            Log(server.ID, "[ERROR] " + gameServer.Error);
                        }

                        //Start the server
                        g_iServerStatus[serverId] = ServerStatus.Starting;
                        SetServerStatus(server, "Starting");

                        var gameServerStart = await Server_BeginStart(server);
                        if (gameServerStart == null) { return; }

                        g_iServerStatus[serverId] = ServerStatus.Started;
                        SetServerStatus(server, "Started");

                        break;
                    }
                }
                else if (string.IsNullOrWhiteSpace(localVersion))
                {
                    Log(server.ID, $"[NOTICE] Fail to get local build.");
                }
                else if (string.IsNullOrWhiteSpace(remoteVersion))
                {
                    Log(server.ID, $"[NOTICE] Fail to get remote build.");
                }
            }
        }

        private async void StartRestartCrontabCheck(Functions.ServerTable server)
        {
            int serverId = int.Parse(server.ID);

            //Save the process of game server
            Process p = g_Process[serverId];

            while (p != null && !p.HasExited)
            {
                //If not enable return
                if (!g_bRestartCrontab[serverId])
                {
                    await Task.Delay(1000);

                    continue;
                }

                //Try get next DataTime restart
                DateTime? crontabTime = CrontabSchedule.TryParse(g_CrontabFormat[serverId])?.GetNextOccurrence(DateTime.Now);

                //Delay 1 second for later compare
                await Task.Delay(1000);

                //Return if crontab expression is invalid 
                if (crontabTime == null) { continue; }

                //If now >= crontab time
                if (DateTime.Compare(DateTime.Now, crontabTime ?? DateTime.Now) >= 0)
                {
                    //Update the next crontab
                    var currentRow = (Functions.ServerTable)ServerGrid.SelectedItem;
                    if (currentRow.ID == server.ID)
                    {
                        textBox_nextcrontab.Text = CrontabSchedule.TryParse(g_CrontabFormat[serverId])?.GetNextOccurrence(DateTime.Now).ToString("ddd, MM/dd/yyyy HH:mm:ss");
                    }

                    if (p == null || p.HasExited)
                    {
                        break;
                    }

                    //Restart the server
                    if (g_iServerStatus[serverId] == ServerStatus.Started)
                    {
                        g_Process[serverId] = null;

                        //Begin Restart
                        g_iServerStatus[serverId] = ServerStatus.Restarting;
                        Log(server.ID, "Action: Restart");
                        SetServerStatus(server, "Restarting");

                        await Server_BeginStop(server, p);
                        var gameServer = await Server_BeginStart(server);
                        if (gameServer == null) { return; }

                        g_iServerStatus[serverId] = ServerStatus.Started;
                        Log(server.ID, "Server: Restarted | Restart Crontab");
                        if (!string.IsNullOrWhiteSpace(gameServer.Notice))
                        {
                            Log(server.ID, "[Notice] " + gameServer.Notice);
                        }
                        SetServerStatus(server, "Started");

                        if (g_bDiscordAlert[serverId] && g_bRestartCrontabAlert[serverId])
                        {
                            var webhook = new Functions.DiscordWebhook(g_DiscordWebhook[serverId], g_DiscordMessage[serverId], g_DonorType);
                            await webhook.Send(server.ID, server.Game, "Restarted | Restart Crontab", server.Name, server.IP, server.Port);
                        }

                        break;
                    }
                }
            }
        }

        private async void StartSendHeartBeat(Functions.ServerTable server)
        {
            //Save the process of game server
            Process p = g_Process[int.Parse(server.ID)];

            while (p != null && !p.HasExited)
            {
                if (MahAppSwitch_SendStatistics.IsChecked ?? false)
                {
                    var analytics = new Functions.GoogleAnalytics();
                    analytics.SendGameServerHeartBeat(server.Game, server.Name);
                }

                await Task.Delay(300000);
            }
        }

        private async void StartQuery(Functions.ServerTable server)
        {
            if (string.IsNullOrWhiteSpace(server.IP) || string.IsNullOrWhiteSpace(server.QueryPort)) { return; }

            // Check the server support Query Method
            dynamic gameServer = GameServer.Data.Class.Get(server.Game);
            if (gameServer == null) { return; }
            if (gameServer.QueryMethod == null) { return; }

            // Save the process of game server
            Process p = g_Process[int.Parse(server.ID)];

            // Query server every 5 seconds
            while (p != null && !p.HasExited)
            {
                if (g_iServerStatus[int.Parse(server.ID)] == ServerStatus.Stopped)
                {
                    break;
                }

                if (!IsValidIPAddress(server.IP) || !IsValidPort(server.QueryPort))
                {
                    continue;
                }

                dynamic query = gameServer.QueryMethod;
                query.SetAddressPort(server.IP, int.Parse(server.QueryPort));
                string players = await query.GetPlayersAndMaxPlayers();

                if (players != null)
                {
                    server.Maxplayers = players;

                    for (int i = 0; i < ServerGrid.Items.Count; i++)
                    {
                        if (server.ID == ((Functions.ServerTable)ServerGrid.Items[i]).ID)
                        {
                            int selectedIndex = ServerGrid.SelectedIndex;
                            ServerGrid.Items[i] = server;
                            ServerGrid.SelectedIndex = selectedIndex;
                            ServerGrid.Items.Refresh();
                            break;
                        }
                    }
                }
               
                await Task.Delay(5000);
            }
        }

        private async Task EndAllRunningProcess(string serverId)
        {
            await Task.Run(() =>
            {
                //LINQ query for windowsgsm old processes
                var processes = (from p in Process.GetProcesses()
                                 where ((Predicate<Process>)(p_ =>
                                 {
                                     try
                                     {
                                         return p_.MainModule.FileName.Contains(Path.Combine(WGSM_PATH, "Servers", serverId) + "\\");
                                     }
                                     catch
                                     {
                                         return false;
                                     }
                                 }))(p)
                                 select p).ToList();

                // Kill all processes
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

        private void SetServerStatus(Functions.ServerTable server, string status)
        {
            server.Status = status;

            if (server.Status != "Started" && server.Maxplayers.Contains('/'))
            {
                var serverConfig = new Functions.ServerConfig(server.ID);
                server.Maxplayers = serverConfig.ServerMaxPlayer;
            }

            for (int i = 0; i < ServerGrid.Items.Count; i++)
            {
                if (server.ID == ((Functions.ServerTable)ServerGrid.Items[i]).ID)
                {
                    int selectedIndex = ServerGrid.SelectedIndex;
                    ServerGrid.Items[i] = server;
                    ServerGrid.SelectedIndex = selectedIndex;
                    ServerGrid.Items.Refresh();
                    break;
                }
            }

            DataGrid_RefreshElements();
        }

        public void Log(string serverId, string logText)
        {
            string log = $"[{DateTime.Now.ToString("MM/dd/yyyy-HH:mm:ss")}][#{serverId}] {logText}" + Environment.NewLine;
            string logPath = Functions.ServerPath.GetLogs();
            Directory.CreateDirectory(logPath);

            string logFile = Path.Combine(logPath, $"L{DateTime.Now.ToString("yyyyMMdd")}.log");
            File.AppendAllText(logFile, log);

            textBox_wgsmlog.AppendText(log);
            textBox_wgsmlog.Text = RemovedOldLog(textBox_wgsmlog.Text);
            textBox_wgsmlog.ScrollToEnd();
        }

        public void DiscordBotLog(string logText)
        {
            string log = $"[{DateTime.Now.ToString("MM/dd/yyyy-HH:mm:ss")}] {logText}" + Environment.NewLine;
            string logPath = Functions.ServerPath.GetLogs();
            Directory.CreateDirectory(logPath);

            string logFile = Path.Combine(logPath, $"L{DateTime.Now.ToString("yyyyMMdd")}-DiscordBot.log");
            File.AppendAllText(logFile, log);

            textBox_DiscordBotLog.AppendText(log);
            textBox_DiscordBotLog.Text = RemovedOldLog(textBox_DiscordBotLog.Text);
            textBox_DiscordBotLog.ScrollToEnd();
        }

        private string RemovedOldLog(string logText)
        {
            const int MAX_LOG_LINE = 50;
            int lineCount = logText.Count(f => f == '\n');
            return (lineCount > MAX_LOG_LINE) ? string.Join("\n", logText.Split('\n').Skip(lineCount - MAX_LOG_LINE).ToArray()) : logText;
        }

        private void Button_ClearServerConsole_Click(object sender, RoutedEventArgs e)
        {
            var server = (Functions.ServerTable)ServerGrid.SelectedItem;
            if (server == null) { return; }

            g_ServerConsoles[int.Parse(server.ID)].Clear();
            console.Clear();
        }

        private void Button_ClearWGSMLog_Click(object sender, RoutedEventArgs e)
        {
            textBox_wgsmlog.Clear();
        }

        private void SendCommand(Functions.ServerTable server, string command)
        {
            Process p = g_Process[int.Parse(server.ID)];
            if (p == null) { return; }

            if (server.Game == GameServer.SDTD.FullName)
            {
                g_ServerConsoles[int.Parse(server.ID)].InputFor7DTD(p, command, g_MainWindow[int.Parse(server.ID)]);
                return;
            }

            textbox_servercommand.Focusable = false;
            g_ServerConsoles[int.Parse(server.ID)].Input(p, command, g_MainWindow[int.Parse(server.ID)]);
            textbox_servercommand.Focusable = true;
            console.ScrollToEnd();
        }

        private static bool IsValidIPAddress(string ip)
        {
            if (string.IsNullOrWhiteSpace(ip))
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
            if (!int.TryParse(port, out int portnum))
            {
                return false;
            }

            return portnum > 1 && portnum < 65535;
        }

        #region Menu - Browse
        private void Browse_ServerBackups_Click(object sender, RoutedEventArgs e)
        {
            var server = (Functions.ServerTable)ServerGrid.SelectedItem;
            if (server == null) { return; }

            string path = Functions.ServerPath.GetBackups(server.ID);
            Directory.CreateDirectory(path);

            Process.Start(path);
        }

        private void Browse_ServerConfigs_Click(object sender, RoutedEventArgs e)
        {
            var server = (Functions.ServerTable)ServerGrid.SelectedItem;
            if (server == null) { return; }

            string path = Functions.ServerPath.GetServersConfigs(server.ID);
            if (Directory.Exists(path))
            {
                Process.Start(path);
            }
        }

        private void Browse_ServerFiles_Click(object sender, RoutedEventArgs e)
        {
            var server = (Functions.ServerTable)ServerGrid.SelectedItem;
            if (server == null) { return; }

            string path = Functions.ServerPath.GetServersServerFiles(server.ID);
            if (Directory.Exists(path))
            {
                Process.Start(path);
            }
        }
        #endregion

        #region Top Bar Button
        private void Button_Website_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("https://windowsgsm.com/");
        }

        private void Button_Discord_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("https://discord.gg/bGc7t2R");
        }

        private void Button_Patreon_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("https://www.patreon.com/WindowsGSM/");
        }

        private void Button_Settings_Click(object sender, RoutedEventArgs e)
        {
            MahAppFlyout_DiscordAlert.IsOpen = false;
            MahAppFlyout_SetAffinity.IsOpen = false;
            MahAppFlyout_Settings.IsOpen = !MahAppFlyout_Settings.IsOpen;
        }

        private void Button_Hide_Click(object sender, RoutedEventArgs e)
        {
            Hide();
            notifyIcon.Visible = true;
            notifyIcon.ShowBalloonTip(0);
            notifyIcon.Visible = false;
            notifyIcon.Visible = true;
        }
        #endregion

        #region Settings Flyout
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

        private void StartOnLogin_IsCheckedChanged(object sender, EventArgs e)
        {
            RegistryKey key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\WindowsGSM", true);
            if (key != null)
            {
                key.SetValue("StartOnBoot", (MahAppSwitch_StartOnBoot.IsChecked ?? false).ToString());
                key.Close();
            }

            SetStartOnBoot(MahAppSwitch_StartOnBoot.IsChecked ?? false);
        }

        private void SendStatistics_IsCheckedChanged(object sender, EventArgs e)
        {
            RegistryKey key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\WindowsGSM", true);
            if (key != null)
            {
                key.SetValue("SendStatistics", (MahAppSwitch_SendStatistics.IsChecked ?? false).ToString());
                key.Close();
            }
        }

        private void SetStartOnBoot(bool enable)
        {
            string taskName = "WindowsGSM";
            string wgsmPath = Process.GetCurrentProcess().MainModule.FileName;

            Process schtasks = new Process
            {
                StartInfo =
                {
                    FileName = "schtasks",
                    Arguments = enable ? $"/create /tn {taskName} /tr \"{wgsmPath}\" /sc onlogon /rl HIGHEST /f" : $"/delete /tn {taskName} /f",
                    CreateNoWindow = true,
                    UseShellExecute = false
                }
            };
            schtasks.Start();
        }
        #endregion

        #region Donor Connect
        private async void DonorConnect_IsCheckedChanged(object sender, EventArgs e)
        {
            RegistryKey key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\WindowsGSM", true);

            //If switch is checked
            if (!MahAppSwitch_DonorConnect.IsChecked ?? false)
            {
                g_DonorType = string.Empty;
                comboBox_Themes.Text = "Blue";
                comboBox_Themes.IsEnabled = false;

                //Set theme
                AppTheme theme = ThemeManager.GetAppTheme((MahAppSwitch_DarkTheme.IsChecked ?? false) ? "BaseDark" : "BaseLight");
                ThemeManager.ChangeAppStyle(System.Windows.Application.Current, ThemeManager.GetAccent(comboBox_Themes.SelectedItem.ToString()), theme);

                key.SetValue("DonorTheme", (MahAppSwitch_DonorConnect.IsChecked ?? false).ToString());
                key.SetValue("DonorColor", "Blue");
                key.Close();
                return;
            }

            //If switch is not checked
            key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\WindowsGSM", true);
            string authKey = (key.GetValue("DonorAuthKey") == null) ? string.Empty : key.GetValue("DonorAuthKey").ToString();

            var settings = new MetroDialogSettings
            {
                AffirmativeButtonText = "Activate",
                DefaultText = authKey
            };

            authKey = await this.ShowInputAsync("Donor Connect (Patreon)", "Please enter the activation key.", settings);

            //If pressed cancel or key is null or whitespace
            if (String.IsNullOrWhiteSpace(authKey))
            {
                MahAppSwitch_DonorConnect.IsChecked = false;
                key.Close();
                return;
            }

            ProgressDialogController controller = await this.ShowProgressAsync("Authenticating...", "Please wait...");
            controller.SetIndeterminate();
            (bool success, string name) = await AuthenticateDonor(authKey);
            await controller.CloseAsync();

            if (success)
            {
                key.SetValue("DonorTheme", "True");
                key.SetValue("DonorAuthKey", authKey);
                await this.ShowMessageAsync("Success!", $"Thanks for your donation {name}, your support help us a lot!\nYou can choose any theme you like on the Settings!");
            }
            else
            {
                key.SetValue("DonorTheme", "False");
                key.SetValue("DonorAuthKey", "");
                await this.ShowMessageAsync("Fail to activate.", "Please visit https://windowsgsm.com/patreon/ to get the key.");

                MahAppSwitch_DonorConnect.IsChecked = false;
            }
            key.Close();
        }

        private async Task<(bool, string)> AuthenticateDonor(string authKey)
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
                        string type = JObject.Parse(json)["type"].ToString();

                        g_DonorType = type;
                        g_DiscordBot.SetDonorType(g_DonorType);
                        comboBox_Themes.IsEnabled = true;

                        return (true, name);
                    }
                    else
                    {                
                        MahAppSwitch_DonorConnect.IsChecked = false;

                        //Set theme
                        AppTheme theme = ThemeManager.GetAppTheme((MahAppSwitch_DarkTheme.IsChecked ?? false) ? "BaseDark" : "BaseLight");
                        ThemeManager.ChangeAppStyle(System.Windows.Application.Current, ThemeManager.GetAccent("Blue"), theme);
                    }
                }
            }
            catch
            {
                // ignore
            }

            return (false, string.Empty);
        }

        private void ComboBox_Themes_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            RegistryKey key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\WindowsGSM", true);
            key.SetValue("DonorColor", comboBox_Themes.SelectedItem.ToString());
            key.Close();

            //Set theme
            AppTheme theme = ThemeManager.GetAppTheme((MahAppSwitch_DarkTheme.IsChecked ?? false) ? "BaseDark" : "BaseLight");
            ThemeManager.ChangeAppStyle(System.Windows.Application.Current, ThemeManager.GetAccent(comboBox_Themes.SelectedItem.ToString()), theme);
        }
        #endregion

        #region Menu - Help
        private void Help_OnlineDocumentation_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("https://docs.windowsgsm.com");
        }

        private void Help_ReportIssue_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("https://github.com/WindowsGSM/WindowsGSM/issues");
        }

        private async void Help_SoftwareUpdates_Click(object sender, RoutedEventArgs e)
        {
            ProgressDialogController controller = await this.ShowProgressAsync("Checking updates...", "Please wait...");
            controller.SetIndeterminate();
            string latestVersion = await GetLatestVersion();
            await controller.CloseAsync();

            if (latestVersion == WGSM_VERSION)
            {
                await this.ShowMessageAsync("Software Updates", "WindowsGSM is up to date.");
                return;
            }
            else
            {
                var settings = new MetroDialogSettings
                {
                    AffirmativeButtonText = "Update",
                    DefaultButtonFocus = MessageDialogResult.Affirmative
                };

                var result = await this.ShowMessageAsync("Software Updates", $"Version {latestVersion} is available, would you like to update now?\n\nWarning: All servers will be shutdown!", MessageDialogStyle.AffirmativeAndNegative, settings);

                if (result.ToString().Equals("Affirmative"))
                {
                    string installPath = Functions.ServerPath.GetInstaller();
                    Directory.CreateDirectory(installPath);

                    string filePath = Path.Combine(installPath, "WindowsGSM-Updater.exe");

                    if (!File.Exists(filePath))
                    {
                        //Download WindowsGSM-Updater.exe
                        controller = await this.ShowProgressAsync("Downloading WindowsGSM-Updater...", "Please wait...");
                        controller.SetIndeterminate();
                        bool success = await DownloadWindowsGSMUpdater();
                        await controller.CloseAsync();
                    }

                    if (File.Exists(filePath))
                    {
                        //Kill all the server
                        for (int i = 0; i <= MAX_SERVER; i++)
                        {
                            if (g_Process[i] == null)
                            {
                                continue;
                            }

                            if (!g_Process[i].HasExited)
                            {
                                g_Process[i].Kill();
                            }
                        }

                        //Run WindowsGSM-Updater.exe
                        Process updater = new Process
                        {
                            StartInfo =
                            {
                                WorkingDirectory = installPath,
                                FileName = filePath,
                                Arguments = "-autostart -forceupdate"
                            }
                        };
                        updater.Start();

                        Close();
                    }
                    else
                    {
                        await this.ShowMessageAsync("Software Updates", $"Fail to download WindowsGSM-Updater.exe");
                    }
                }
            }
        }

        private async Task<string> GetLatestVersion()
        {
            var webRequest = WebRequest.Create("https://api.github.com/repos/BattlefieldDuck/WindowsGSM/releases/latest") as HttpWebRequest;
            if (webRequest != null)
            {
                webRequest.Method = "GET";
                webRequest.UserAgent = "Anything";
                webRequest.ServicePoint.Expect100Continue = false;

                try
                {
                    var response = await webRequest.GetResponseAsync();
                    using (var responseReader = new StreamReader(response.GetResponseStream()))
                    {
                        string json = responseReader.ReadToEnd();
                        string version = JObject.Parse(json)["tag_name"].ToString();

                        return version;
                    }
                }
                catch
                {
                    //ignore
                }
            }

            return null;
        }

        private async Task<bool> DownloadWindowsGSMUpdater()
        {
            string filePath = Path.Combine(WGSM_PATH, "installer", "WindowsGSM-Updater.exe");

            try
            {
                using (WebClient webClient = new WebClient())
                {
                    await webClient.DownloadFileTaskAsync("https://github.com/WindowsGSM/WindowsGSM-Updater/releases/latest/download/WindowsGSM-Updater.exe", filePath);
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine($"Github.WindowsGSM-Updater.exe {e}");
            }

            return File.Exists(filePath);
        }

        private async void Help_AboutWindowsGSM_Click(object sender, RoutedEventArgs e)
        {
            var settings = new MetroDialogSettings
            {
                AffirmativeButtonText = "Patreon",
                NegativeButtonText = "Ok",
                DefaultButtonFocus = MessageDialogResult.Negative                
            };

            var result = await this.ShowMessageAsync("About WindowsGSM", $"Product:\t\tWindowsGSM\nVersion:\t\t{WGSM_VERSION.Substring(1)}\nCreator:\t\tTatLead\n\nIf you like WindowsGSM, consider becoming a Patron!", MessageDialogStyle.AffirmativeAndNegative, settings);

            if (result.ToString() == "Affirmative")
            {
                Process.Start("https://www.patreon.com/WindowsGSM/");
            }
        }
        #endregion

        #region Menu - Tools
        private void Tools_GlobalServerListCheck_Click(object sender, RoutedEventArgs e)
        {
            var row = (Functions.ServerTable)ServerGrid.SelectedItem;
            if (row == null) { return; }

            if (row.Game == GameServer.MCPE.FullName || row.Game == GameServer.MC.FullName)
            {
                Log(row.ID, $"This feature is not applicable on {row.Game}");
                return;
            }

            string publicIP = GetPublicIP();
            if (publicIP == null)
            {
                Log(row.ID, "Fail to check. Reason: Fail to get the public ip.");
                return;
            }

            string messageText = $"Server Name: {row.Name}\nPublic IP: {publicIP}\nQuery Port: {row.QueryPort}";
            if (Tools.GlobalServerList.IsServerOnSteamServerList(publicIP, row.QueryPort))
            {
                System.Windows.MessageBox.Show(messageText + "\n\nResult: Online\n\nYour server is on the global server list!", "Global Server List Check", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                System.Windows.MessageBox.Show(messageText + "\n\nResult: Offline\n\nYour server is not on the global server list.", "Global Server List Check", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void Tool_InstallAMXModXMetamodP_Click(object sender, RoutedEventArgs e)
        {
            var server = (Functions.ServerTable)ServerGrid.SelectedItem;
            if (server == null) { return; }

            bool? existed = Tools.InstallAddons.IsAMXModXAndMetaModPExists(server);
            if (existed == null)
            {
                await this.ShowMessageAsync("Tools - Install AMX Mod X & MetaMod-P", $"Doesn't support on {server.Game} (ID: {server.ID})");
                return;
            }

            if (existed == true)
            {
                await this.ShowMessageAsync("Tools - Install AMX Mod X & MetaMod-P", $"Already Installed (ID: {server.ID})");
                return;
            }

            var result = await this.ShowMessageAsync("Tools - Install AMX Mod X & MetaMod-P", $"Are you sure to install? (ID: {server.ID})", MessageDialogStyle.AffirmativeAndNegative);
            if (result == MessageDialogResult.Affirmative)
            {
                ProgressDialogController controller = await this.ShowProgressAsync("Installing...", "Please wait...");
                controller.SetIndeterminate();
                bool installed = await Tools.InstallAddons.AMXModXAndMetaModP(server);
                await controller.CloseAsync();

                string message = installed ? $"Installed successfully" : $"Fail to install";
                await this.ShowMessageAsync("Tools - Install AMX Mod X & MetaMod-P", $"{message} (ID: {server.ID})");
            }
        }

        private async void Tools_InstallSourcemodMetamod_Click(object sender, RoutedEventArgs e)
        {
            var server = (Functions.ServerTable)ServerGrid.SelectedItem;
            if (server == null) { return; }

            bool? existed = Tools.InstallAddons.IsSourceModAndMetaModExists(server);
            if (existed == null)
            {
                await this.ShowMessageAsync("Tools - Install SourceMod & MetaMod", $"Doesn't support on {server.Game} (ID: {server.ID})");
                return;
            }

            if (existed == true)
            {
                await this.ShowMessageAsync("Tools - Install SourceMod & MetaMod", $"Already Installed (ID: {server.ID})");
                return;
            }

            var result = await this.ShowMessageAsync("Tools - Install SourceMod & MetaMod", $"Are you sure to install? (ID: {server.ID})", MessageDialogStyle.AffirmativeAndNegative);
            if (result == MessageDialogResult.Affirmative)
            {
                ProgressDialogController controller = await this.ShowProgressAsync("Installing...", "Please wait...");
                controller.SetIndeterminate();
                bool installed = await Tools.InstallAddons.SourceModAndMetaMod(server);
                await controller.CloseAsync();

                string message = installed ? $"Installed successfully" : $"Fail to install";
                await this.ShowMessageAsync("Tools - Install SourceMod & MetaMod", $"{message} (ID: {server.ID})");
            }
        }

        private async void Tools_InstallDayZSALModServer_Click(object sender, RoutedEventArgs e)
        {
            var server = (Functions.ServerTable)ServerGrid.SelectedItem;
            if (server == null) { return; }

            bool? existed = Tools.InstallAddons.IsDayZSALModServerExists(server);
            if (existed == null)
            {
                await this.ShowMessageAsync("Tools - Install DayZSAL Mod Server", $"Doesn't support on {server.Game} (ID: {server.ID})");
                return;
            }

            if (existed == true)
            {
                await this.ShowMessageAsync("Tools - Install DayZSAL Mod Server", $"Already Installed (ID: {server.ID})");
                return;
            }

            var result = await this.ShowMessageAsync("Tools - Install DayZSAL Mod Server", $"Are you sure to install? (ID: {server.ID})", MessageDialogStyle.AffirmativeAndNegative);
            if (result == MessageDialogResult.Affirmative)
            {
                ProgressDialogController controller = await this.ShowProgressAsync("Installing...", "Please wait...");
                controller.SetIndeterminate();
                bool installed = await Tools.InstallAddons.DayZSALModServer(server);
                await controller.CloseAsync();

                string message = installed ? $"Installed successfully" : $"Fail to install";
                await this.ShowMessageAsync("Tools - Install DayZSAL Mod Server", $"{message} (ID: {server.ID})");
            }
        }
        #endregion

        private string GetPublicIP()
        {
            try
            {
                using (WebClient webClient = new WebClient())
                {
                    return webClient.DownloadString("https://ipinfo.io/ip").Replace("\n", string.Empty);
                }
            }
            catch
            {
                return null;
            }
        }

        private void OnBalloonTipClick(object sender, EventArgs e)
        {
        }

        private void NotifyIcon_MouseClick(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            if (IsVisible)
            {
                Hide();
            }
            else
            {
                WindowState = WindowState.Normal;
                Show();
            }
        }

        #region Left Buttom Grid
        private void Slider_ProcessPriority_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            var server = (Functions.ServerTable)ServerGrid.SelectedItem;
            if (server == null) { return; }

            g_CPUPriority[int.Parse(server.ID)] = ((int)slider_ProcessPriority.Value).ToString();
            Functions.ServerConfig.SetSetting(server.ID, Functions.SettingName.CPUPriority, g_CPUPriority[int.Parse(server.ID)]);
            textBox_ProcessPriority.Text = Functions.CPU.Priority.GetPriorityByInteger((int)slider_ProcessPriority.Value);

            if (g_Process[int.Parse(server.ID)] != null && !g_Process[int.Parse(server.ID)].HasExited)
            {
                g_Process[int.Parse(server.ID)] = Functions.CPU.Priority.SetProcessWithPriority(g_Process[int.Parse(server.ID)], (int)slider_ProcessPriority.Value);
            }
        }

        private void Button_SetAffinity_Click(object sender, RoutedEventArgs e)
        {
            var server = (Functions.ServerTable)ServerGrid.SelectedItem;
            if (server == null) { return; }

            MahAppFlyout_DiscordAlert.IsOpen = false;
            MahAppFlyout_SetAffinity.IsOpen = !MahAppFlyout_SetAffinity.IsOpen;
        }

        private void Button_RestartCrontab_Click(object sender, RoutedEventArgs e)
        {
            var server = (Functions.ServerTable)ServerGrid.SelectedItem;
            if (server == null) { return; }

            g_bRestartCrontab[int.Parse(server.ID)] = Functions.ServerConfig.ToggleSetting(server.ID, Functions.SettingName.RestartCrontab);
            switch_restartcrontab.IsChecked = g_bRestartCrontab[int.Parse(server.ID)];
        }

        private void Button_EmbedConsole_Click(object sender, RoutedEventArgs e)
        {
            var server = (Functions.ServerTable)ServerGrid.SelectedItem;
            if (server == null) { return; }

            g_bEmbedConsole[int.Parse(server.ID)] = Functions.ServerConfig.ToggleSetting(server.ID, Functions.SettingName.EmbedConsole);
            switch_embedconsole.IsChecked = g_bEmbedConsole[int.Parse(server.ID)];
        }

        private void Button_AutoRestart_Click(object sender, RoutedEventArgs e)
        {
            var server = (Functions.ServerTable)ServerGrid.SelectedItem;
            if (server == null) { return; }

            g_bAutoRestart[int.Parse(server.ID)] = Functions.ServerConfig.ToggleSetting(server.ID, Functions.SettingName.AutoRestart);
            switch_autorestart.IsChecked = g_bAutoRestart[int.Parse(server.ID)];
        }

        private void Button_AutoStart_Click(object sender, RoutedEventArgs e)
        {
            var server = (Functions.ServerTable)ServerGrid.SelectedItem;
            if (server == null) { return; }

            g_bAutoStart[int.Parse(server.ID)] = Functions.ServerConfig.ToggleSetting(server.ID, Functions.SettingName.AutoStart);
            switch_autostart.IsChecked = g_bAutoStart[int.Parse(server.ID)];
        }

        private void Button_AutoUpdate_Click(object sender, RoutedEventArgs e)
        {
            var server = (Functions.ServerTable)ServerGrid.SelectedItem;
            if (server == null) { return; }

            g_bAutoUpdate[int.Parse(server.ID)] = Functions.ServerConfig.ToggleSetting(server.ID, Functions.SettingName.AutoUpdate);
            switch_autoupdate.IsChecked = g_bAutoUpdate[int.Parse(server.ID)];
        }

        private async void Button_DiscordAlertSettings_Click(object sender, RoutedEventArgs e)
        {
            var server = (Functions.ServerTable)ServerGrid.SelectedItem;
            if (server == null) { return; }

            MahAppFlyout_SetAffinity.IsOpen = false;
            MahAppFlyout_DiscordAlert.IsOpen = !MahAppFlyout_DiscordAlert.IsOpen;
        }

        private void Button_UpdateOnStart_Click(object sender, RoutedEventArgs e)
        {
            var server = (Functions.ServerTable)ServerGrid.SelectedItem;
            if (server == null) { return; }

            g_bUpdateOnStart[int.Parse(server.ID)] = Functions.ServerConfig.ToggleSetting(server.ID, Functions.SettingName.UpdateOnStart);
            switch_updateonstart.IsChecked = g_bUpdateOnStart[int.Parse(server.ID)];
        }

        private void Button_DiscordAlert_Click(object sender, RoutedEventArgs e)
        {
            var server = (Functions.ServerTable)ServerGrid.SelectedItem;
            if (server == null) { return; }

            g_bDiscordAlert[int.Parse(server.ID)] = Functions.ServerConfig.ToggleSetting(server.ID, Functions.SettingName.DiscordAlert);
            switch_discordalert.IsChecked = g_bDiscordAlert[int.Parse(server.ID)];
            button_discordtest.IsEnabled = (g_bDiscordAlert[int.Parse(server.ID)]) ? true : false;
        }

        private async void Button_CrontabEdit_Click(object sender, RoutedEventArgs e)
        {
            var server = (Functions.ServerTable)ServerGrid.SelectedItem;
            if (server == null) { return; }

            string crontabFormat = Functions.ServerConfig.GetSetting(server.ID, Functions.SettingName.CrontabFormat);

            var settings = new MetroDialogSettings
            {
                AffirmativeButtonText = "Save",
                DefaultText = crontabFormat
            };

            crontabFormat = await this.ShowInputAsync("Crontab Format", "Please enter the crontab expressions", settings);
            if (crontabFormat == null) { return; } //If pressed cancel

            g_CrontabFormat[int.Parse(server.ID)] = crontabFormat;
            Functions.ServerConfig.SetSetting(server.ID, Functions.SettingName.CrontabFormat, crontabFormat);

            textBox_restartcrontab.Text = crontabFormat;
            textBox_nextcrontab.Text = CrontabSchedule.TryParse(crontabFormat)?.GetNextOccurrence(DateTime.Now).ToString("ddd, MM/dd/yyyy HH:mm:ss");
        }
        #endregion

        #region Switches
        private void Switch_AutoStartAlert_Click(object sender, RoutedEventArgs e)
        {
            var server = (Functions.ServerTable)ServerGrid.SelectedItem;
            if (server == null) { return; }

            g_bAutoStartAlert[int.Parse(server.ID)] = Functions.ServerConfig.ToggleSetting(server.ID, Functions.SettingName.AutoStartAlert);
            MahAppSwitch_AutoStartAlert.IsChecked = g_bAutoStartAlert[int.Parse(server.ID)];
        }

        private void Switch_AutoRestartAlert_Click(object sender, RoutedEventArgs e)
        {
            var server = (Functions.ServerTable)ServerGrid.SelectedItem;
            if (server == null) { return; }

            g_bAutoRestartAlert[int.Parse(server.ID)] = Functions.ServerConfig.ToggleSetting(server.ID, Functions.SettingName.AutoRestartAlert);
            MahAppSwitch_AutoRestartAlert.IsChecked = g_bAutoRestartAlert[int.Parse(server.ID)];
        }

        private void Switch_AutoUpdateAlert_Click(object sender, RoutedEventArgs e)
        {
            var server = (Functions.ServerTable)ServerGrid.SelectedItem;
            if (server == null) { return; }

            g_bAutoUpdateAlert[int.Parse(server.ID)] = Functions.ServerConfig.ToggleSetting(server.ID, Functions.SettingName.AutoUpdateAlert);
            MahAppSwitch_AutoUpdateAlert.IsChecked = g_bAutoUpdateAlert[int.Parse(server.ID)];
        }

        private void Switch_RestartCrontabAlert_Click(object sender, RoutedEventArgs e)
        {
            var server = (Functions.ServerTable)ServerGrid.SelectedItem;
            if (server == null) { return; }

            g_bRestartCrontabAlert[int.Parse(server.ID)] = Functions.ServerConfig.ToggleSetting(server.ID, Functions.SettingName.RestartCrontabAlert);
            MahAppSwitch_RestartCrontabAlert.IsChecked = g_bRestartCrontabAlert[int.Parse(server.ID)];
        }

        private void Switch_CrashAlert_Click(object sender, RoutedEventArgs e)
        {
            var server = (Functions.ServerTable)ServerGrid.SelectedItem;
            if (server == null) { return; }

            g_bCrashAlert[int.Parse(server.ID)] = Functions.ServerConfig.ToggleSetting(server.ID, Functions.SettingName.CrashAlert);
            MahAppSwitch_CrashAlert.IsChecked = g_bCrashAlert[int.Parse(server.ID)];
        }
        #endregion

        private async void Window_Activated(object sender, EventArgs e)
        {
            if (MahAppFlyout_ManageAddons.IsOpen)
            {
                ListBox_ManageAddons_Refresh();
            }

            // Fix the windows cannot toggle issue because of LoadServerTable
            await Task.Delay(1);

            if (ShowActivated)
            {
                LoadServerTable();
            }
        }

        #region Discord Bot
        private async void Switch_DiscordBot_Click(object sender, RoutedEventArgs e)
        {
            if (switch_DiscordBot.IsChecked ?? false)
            {
                switch_DiscordBot.IsEnabled = false;
                switch_DiscordBot.IsChecked = await g_DiscordBot.Start();
                DiscordBotLog("Discord Bot " + ((switch_DiscordBot.IsChecked ?? false) ? "started." : "fail to start. Reason: Bot Token is invalid."));
                switch_DiscordBot.IsEnabled = true;
            }
            else
            {
                switch_DiscordBot.IsEnabled = false;
                await g_DiscordBot.Stop();
                DiscordBotLog("Discord Bot stopped.");
                switch_DiscordBot.IsEnabled = true;
            }
        }
        
        private void Button_DiscordBotPrefixEdit_Click(object sender, RoutedEventArgs e)
        {
            if (button_DiscordBotPrefixEdit.Content.ToString() == "Edit")
            {
                button_DiscordBotPrefixEdit.Content = "Save";
                textBox_DiscordBotPrefix.IsEnabled = true;
                textBox_DiscordBotPrefix.Focus();
                textBox_DiscordBotPrefix.SelectAll();
            }
            else
            {
                button_DiscordBotPrefixEdit.Content = "Edit";
                textBox_DiscordBotPrefix.IsEnabled = false;
                DiscordBot.Configs.SetBotPrefix(textBox_DiscordBotPrefix.Text);
                label_DiscordBotCommands.Content = DiscordBot.Configs.GetCommandsList();
            }
        }

        private void Button_DiscordBotTokenEdit_Click(object sender, RoutedEventArgs e)
        {
            if (button_DiscordBotTokenEdit.Content.ToString() == "Edit")
            {
                rectangle_DiscordBotTokenSpoiler.Visibility = Visibility.Hidden;
                button_DiscordBotTokenEdit.Content = "Save";
                textBox_DiscordBotToken.IsEnabled = true;
                textBox_DiscordBotToken.Focus();
                textBox_DiscordBotToken.SelectAll();
            }
            else
            {
                rectangle_DiscordBotTokenSpoiler.Visibility = Visibility.Visible;
                button_DiscordBotTokenEdit.Content = "Edit";
                textBox_DiscordBotToken.IsEnabled = false;
                DiscordBot.Configs.SetBotToken(textBox_DiscordBotToken.Text);
            }
        }

        private void button_DiscordBotDashboardEdit_Click(object sender, RoutedEventArgs e)
        {
            if (button_DiscordBotDashboardEdit.Content.ToString() == "Edit")
            {
                button_DiscordBotDashboardEdit.Content = "Save";
                textBox_DiscordBotDashboard.IsEnabled = true;
                textBox_DiscordBotDashboard.Focus();
                textBox_DiscordBotDashboard.SelectAll();
            }
            else
            {
                button_DiscordBotDashboardEdit.Content = "Edit";
                textBox_DiscordBotDashboard.IsEnabled = false;
                DiscordBot.Configs.SetDashboardChannel(textBox_DiscordBotDashboard.Text);
            }
        }

        private void NumericUpDown_DiscordRefreshRate_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double?> e)
        {
            double rate = numericUpDown_DiscordRefreshRate.Value ?? 5;
            DiscordBot.Configs.SetDashboardRefreshRate((int)rate);
        }

        private async void Button_DiscordBotAddID_Click(object sender, RoutedEventArgs e)
        {
            var settings = new MetroDialogSettings
            {
                AffirmativeButtonText = "Add"
            };

            string newAdminID = await this.ShowInputAsync("Add Admin ID", "Please enter the discord user ID.", settings);
            if (newAdminID == null) { return; } //If pressed cancel

            var adminList = DiscordBot.Configs.GetBotAdminList();
            adminList.Add((newAdminID, "0"));
            DiscordBot.Configs.SetBotAdminList(adminList);
            Refresh_DiscordBotAdminList(listBox_DiscordBotAdminList.SelectedIndex);
        }

        private async void Button_DiscordBotEditServerID_Click(object sender, RoutedEventArgs e)
        {
            var adminListItem = (DiscordBot.AdminListItem)listBox_DiscordBotAdminList.SelectedItem;
            if (adminListItem == null) { return; }

            var settings = new MetroDialogSettings
            {
                AffirmativeButtonText = "Save",
                DefaultText = adminListItem.ServerIds
            };

            string example = "0 - Grant All servers Permission.\n\nExamples:\n0\n1,2,3,4,5\n";
            string newServerIds = await this.ShowInputAsync($"Edit Server IDs ({adminListItem.AdminId})", $"Please enter the server Ids where admin has access to the server.\n{example}", settings);
            if (newServerIds == null) { return; } //If pressed cancel

            var adminList = DiscordBot.Configs.GetBotAdminList();
            for (int i = 0; i < adminList.Count; i++)
            {
                if (adminList[i].Item1 == adminListItem.AdminId)
                {
                    adminList.RemoveAt(i);
                    adminList.Insert(i, (adminListItem.AdminId, newServerIds.Trim()));
                    break;
                }
            }
            DiscordBot.Configs.SetBotAdminList(adminList);
            Refresh_DiscordBotAdminList(listBox_DiscordBotAdminList.SelectedIndex);
        }

        private void Button_DiscordBotRemoveID_Click(object sender, RoutedEventArgs e)
        {
            if (listBox_DiscordBotAdminList.SelectedIndex >= 0)
            {
                var adminList = DiscordBot.Configs.GetBotAdminList();
                try
                {
                    adminList.RemoveAt(listBox_DiscordBotAdminList.SelectedIndex);
                }
                catch
                {
                    Console.WriteLine($"Fail to delete item {listBox_DiscordBotAdminList.SelectedIndex} in adminIDs.txt");
                }
                DiscordBot.Configs.SetBotAdminList(adminList);

                listBox_DiscordBotAdminList.Items.Remove(listBox_DiscordBotAdminList.Items[listBox_DiscordBotAdminList.SelectedIndex]);
            }
        }

        public void Refresh_DiscordBotAdminList(int selectIndex = 0)
        {
            listBox_DiscordBotAdminList.Items.Clear();
            foreach ((string adminID, string serverIDs) in DiscordBot.Configs.GetBotAdminList())
            {
                listBox_DiscordBotAdminList.Items.Add(new DiscordBot.AdminListItem { AdminId = adminID, ServerIds = serverIDs });
            }
            listBox_DiscordBotAdminList.SelectedIndex = listBox_DiscordBotAdminList.Items.Count >= 0 ? selectIndex : -1;
        }

        public int GetServerCount()
        {
            return ServerGrid.Items.Count;
        }

        public List<(string, string, string)> GetServerList()
        {
            var list = new List<(string, string, string)>();

            for (int i = 0; i < ServerGrid.Items.Count; i++)
            {
                var server = (Functions.ServerTable)ServerGrid.Items[i];
                list.Add((server.ID, server.Status, server.Name));
            }

            return list;
        }

        public bool IsServerExist(string serverId)
        {
            for (int i = 0; i < ServerGrid.Items.Count; i++)
            {
                var server = (Functions.ServerTable)ServerGrid.Items[i];
                if (server.ID == serverId) { return true; }
            }

            return false;
        }

        public ServerStatus GetServerStatus(string serverId)
        {
            return g_iServerStatus[int.Parse(serverId)];
        }

        public string GetServerName(string serverId)
        {
            var server = GetServerTableById(serverId);
            return (server == null) ? string.Empty : server.Name;
        }

        private Functions.ServerTable GetServerTableById(string serverId)
        {
            for (int i = 0; i < ServerGrid.Items.Count; i++)
            {
                var server = (Functions.ServerTable)ServerGrid.Items[i];
                if (server.ID == serverId) { return server; }
            }

            return null;
        }

        public async Task<bool> StartServerById(string serverId, string adminID, string adminName)
        {
            var server = GetServerTableById(serverId);
            if (server == null) { return false; }

            DiscordBotLog($"Discord: Receive START action | {adminName} ({adminID})");
            await GameServer_Start(server);
            return g_iServerStatus[int.Parse(serverId)] == ServerStatus.Started;
        }

        public async Task<bool> StopServerById(string serverId, string adminID, string adminName)
        {
            var server = GetServerTableById(serverId);
            if (server == null) { return false; }

            DiscordBotLog($"Discord: Receive STOP action | {adminName} ({adminID})");
            await GameServer_Stop(server);
            return g_iServerStatus[int.Parse(serverId)] == ServerStatus.Stopped;
        }

        public async Task<bool> RestartServerById(string serverId, string adminID, string adminName)
        {
            var server = GetServerTableById(serverId);
            if (server == null) { return false; }

            DiscordBotLog($"Discord: Receive RESTART action | {adminName} ({adminID})");
            await GameServer_Restart(server);
            return g_iServerStatus[int.Parse(serverId)] == ServerStatus.Started;
        }

        public async Task<bool> SendCommandById(string serverId, string command, string adminID, string adminName)
        {
            var server = GetServerTableById(serverId);
            if (server == null) { return false; }

            DiscordBotLog($"Discord: Receive SEND action | {adminName} ({adminID}) | {command}");
            SendCommand(server, command);
            return true;
        }

        private void Switch_DiscordBotAutoStart_Click(object sender, RoutedEventArgs e)
        {
            RegistryKey key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\WindowsGSM", true);
            if (key != null)
            {
                key.SetValue("DiscordBotAutoStart", (MahAppSwitch_DiscordBotAutoStart.IsChecked ?? false).ToString());
                key.Close();
            }
        }

        private void Button_DiscordBotInvite_Click(object sender, RoutedEventArgs e)
        {
            string inviteLink = g_DiscordBot.GetInviteLink();
            if (!string.IsNullOrWhiteSpace(inviteLink))
            {
                Process.Start(g_DiscordBot.GetInviteLink());
            }
        }
        #endregion

        private void HamburgerMenu_ItemClick(object sender, ItemClickEventArgs e)
        {
            HamburgerMenuControl.IsPaneOpen = false;

            hMenu_Home.Visibility = (HamburgerMenuControl.SelectedIndex == 0) ? Visibility.Visible : Visibility.Hidden;
            hMenu_Dashboard.Visibility = (HamburgerMenuControl.SelectedIndex == 1) ? Visibility.Visible : Visibility.Hidden;
            hMenu_Discordbot.Visibility = (HamburgerMenuControl.SelectedIndex == 2) ? Visibility.Visible : Visibility.Hidden;

            if (HamburgerMenuControl.SelectedIndex == 2)
            {
                label_DiscordBotCommands.Content = DiscordBot.Configs.GetCommandsList();
                button_DiscordBotPrefixEdit.Content = "Edit";
                textBox_DiscordBotPrefix.IsEnabled = false;
                textBox_DiscordBotPrefix.Text = DiscordBot.Configs.GetBotPrefix();

                button_DiscordBotTokenEdit.Content = "Edit";
                textBox_DiscordBotToken.IsEnabled = false;
                textBox_DiscordBotToken.Text = DiscordBot.Configs.GetBotToken();
                textBox_DiscordBotDashboard.Text = DiscordBot.Configs.GetDashboardChannel();
                numericUpDown_DiscordRefreshRate.Value = DiscordBot.Configs.GetDashboardRefreshRate();

                Refresh_DiscordBotAdminList(listBox_DiscordBotAdminList.SelectedIndex);

                if (listBox_DiscordBotAdminList.Items.Count > 0 && listBox_DiscordBotAdminList.SelectedItem == null)
                {
                    listBox_DiscordBotAdminList.SelectedItem = listBox_DiscordBotAdminList.Items[0];
                }
            }
        }

        private async void HamburgerMenu_Loaded(object sender, RoutedEventArgs e)
        {
            HamburgerMenuControl.Visibility = Visibility.Visible;
            hMenu_Home.Visibility = Visibility.Visible;
            hMenu_Dashboard.Visibility = Visibility.Hidden;
            hMenu_Discordbot.Visibility = Visibility.Hidden;

            await Task.Delay(1); // Delay 0.001 sec due to a bug
            HamburgerMenuControl.SelectedIndex = 0;
        }
    }
}
