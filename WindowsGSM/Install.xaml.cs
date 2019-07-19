using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Net;
using System.Net.Sockets;

namespace WindowsGSM
{
    /// <summary>
    /// Interaction logic for InstallServer.xaml
    /// </summary>
    public partial class Install : Window
    {
        //Get ServerID for this install
        private readonly Functions.ServerConfig serverConfig = new Functions.ServerConfig(null);

        //Store Installer Process, such as steamcmd.exe
        private Process pInstaller;

        public Install()
        {
            InitializeComponent();

            if (string.IsNullOrWhiteSpace(serverConfig.ServerID))
            {
                Close();
            }
            else
            {
                Show();
            }

            Title = "WindowsGSM - Install (ID: " + serverConfig.ServerID + ")";
        }

        private async void Button_install_Click(object sender, RoutedEventArgs e)
        {
            if (pInstaller != null)
            {
                if (!pInstaller.HasExited)
                {
                    pInstaller.Kill();
                }

                pInstaller = null;
            }

            if (button_install.Content.Equals("Cancel Installation"))
            {
                serverConfig.DeleteServerDirectory();

                button_install.Content = "Install";

                return;
            }

            Images.Row selectedgame = comboBox.SelectedItem as Images.Row;
            label_gamewarn.Content = (selectedgame == null) ? "Please select a game server" : "";
            label_namewarn.Content = (string.IsNullOrWhiteSpace(textbox_name.Text)) ? "Server name cannot be null" : "";

            if (string.IsNullOrWhiteSpace(textbox_name.Text) || selectedgame == null)
            {
                return;
            }

            string installPath = MainWindow.WGSM_PATH + @"\servers\" + serverConfig.ServerID + @"\serverfiles";
            if (Directory.Exists(installPath))
            {
                try
                {
                    Directory.Delete(installPath, true);
                }
                catch
                {
                    System.Windows.Forms.MessageBox.Show(installPath + " is not accessible!", "ERROR", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error);
                    return;
                }
            }

            //Installation start
            textbox_name.IsEnabled = false;
            comboBox.IsEnabled = false;
            progressbar_progress.IsIndeterminate = true;

            textblock_progress.Text = "Installing";
            button_install.Content = "Cancel Installation";

            string servername = textbox_name.Text;
            string servergame = selectedgame.Name;

            bool IsInstallSuccess = false;
            switch (servergame)
            {
                case ("Counter-Strike: Global Offensive Dedicated Server"):
                    {
                        GameServer.CSGO server = new GameServer.CSGO(serverConfig.ServerID);
                        pInstaller = await server.Install();
                        IsInstallSuccess = await server.IsInstallSuccess();

                        if (IsInstallSuccess)
                        {
                            serverConfig.CreateServerDirectory();
                            serverConfig.CreateWindowsGSMConfig(servergame, servername, GetIPAddress(), GetAvailablePort(server.port), server.defaultmap, server.maxplayers, "", server.additional);

                            server.CreateServerCFG(servername, GetRCONPassword());
                        }

                        break;
                    }
                case ("Garry's Mod Dedicated Server"):
                    {
                        GameServer.GMOD server = new GameServer.GMOD(serverConfig.ServerID);
                        pInstaller = await server.Install();
                        IsInstallSuccess = await server.IsInstallSuccess();

                        if (IsInstallSuccess)
                        {
                            serverConfig.CreateServerDirectory();
                            serverConfig.CreateWindowsGSMConfig(servergame, servername, GetIPAddress(), GetAvailablePort(server.port), server.defaultmap, server.maxplayers, "", server.additional);

                            server.CreateServerCFG(servername, GetRCONPassword());
                        }

                        break;
                    }
                case ("Team Fortress 2 Dedicated Server"):
                    {
                        GameServer.TF2 server = new GameServer.TF2(serverConfig.ServerID);
                        pInstaller = await server.Install();
                        IsInstallSuccess = await server.IsInstallSuccess();

                        if (IsInstallSuccess)
                        {
                            serverConfig.CreateServerDirectory();
                            serverConfig.CreateWindowsGSMConfig(servergame, servername, GetIPAddress(), GetAvailablePort(server.port), server.defaultmap, server.maxplayers, "", server.additional);

                            server.CreateServerCFG(servername, GetRCONPassword());
                        }

                        break;
                    }
                case ("Minecraft Server"): break;
            }

            if (IsInstallSuccess)
            {
                MainWindow WindowsGSM = (MainWindow)System.Windows.Application.Current.MainWindow;

                Table row = new Table
                {
                    ID = serverConfig.ServerID,
                    Game = serverConfig.ServerGame,
                    Icon = Images.ServerIcon.ResourceManager.GetString(servergame),
                    Status = "Stopped",
                    Name = serverConfig.ServerName,
                    IP = serverConfig.ServerIP,
                    Port = serverConfig.ServerPort,
                    Defaultmap = serverConfig.ServerMap,
                    Maxplayers = serverConfig.ServerMaxPlayer
                };
                WindowsGSM.ServerGrid.Items.Add(row);

                WindowsGSM.Log(serverConfig.ServerID, "Install: Success");

                Close();
            }
            else
            {
                textbox_name.IsEnabled = true;
                comboBox.IsEnabled = true;
                progressbar_progress.IsIndeterminate = false;

                textblock_progress.Text = "[ERROR] Fail to install";
                button_install.Content = "Install";
            }
        }

        private static string GetIPAddress()
        {
            using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0))
            {
                socket.Connect("8.8.8.8", 65530);
                IPEndPoint endPoint = socket.LocalEndPoint as IPEndPoint;
                return endPoint.Address.ToString();
            }
        }

        private static string GetAvailablePort(string defaultport)
        {
            MainWindow WindowsGSM = (MainWindow)System.Windows.Application.Current.MainWindow;

            int[] portlist = new int[WindowsGSM.ServerGrid.Items.Count];

            for (int i = 0; i < WindowsGSM.ServerGrid.Items.Count; i++)
            {
                Table row = WindowsGSM.ServerGrid.Items[i] as Table;
                portlist[i] = Int32.Parse((string.IsNullOrWhiteSpace(row.Port)) ? "0" : row.Port);
            }

            Array.Sort(portlist);

            int port = Int32.Parse(defaultport);
            for (int i = 0; i < WindowsGSM.ServerGrid.Items.Count; i++)
            {
                if (port == portlist[i])
                {
                    port++;
                }

                //SourceTV port 27020
                if (port == 27020)
                {
                    port++;
                }
            }

            return port.ToString();
        }

        private static string GetRCONPassword()
        {
            string allowedChars = "abcdefghijkmnopqrstuvwxyzABCDEFGHJKLMNOPQRSTUVWXYZ0123456789!@$?_-";
            char[] chars = new char[12];
            Random rd = new Random();

            for (int i = 0; i < 12; i++)
            {
                chars[i] = allowedChars[rd.Next(0, allowedChars.Length)];
            }

            return new string(chars);
        }
    }
}
