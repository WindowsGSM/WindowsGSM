using System;
using System.Threading.Tasks;
using System.Windows;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Windows.Forms;
using System.Diagnostics;

namespace WindowsGSM
{
    /// <summary>
    /// Interaction logic for Import.xaml
    /// </summary>
    public partial class Import : Window
    {
        //Get ServerID for this import
        private readonly Functions.ServerConfig serverConfig = new Functions.ServerConfig(null);

        private Process pImporter = null;

        public Import()
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

            Title = "WindowsGSM - Import (ID: " + serverConfig.ServerID + ")";

            //Add games to ComboBox
            int i = 0;
            string servergame = "";
            while (servergame != null)
            {
                servergame = GameServerList.ResourceManager.GetString((++i).ToString());
                if (servergame == null) break;

                var row = new Images.Row { Image = Images.ServerIcon.ResourceManager.GetString(servergame), Name = servergame };
                comboBox.Items.Add(row);
            }
        }

        private async void Button_Import_Click(object sender, RoutedEventArgs e)
        {
            if (pImporter != null)
            {
                if (!pImporter.HasExited) pImporter.Kill();

                pImporter = null;
            }

            if (button_Import.Content.Equals("Cancel Import"))
            {
                serverConfig.DeleteServerDirectory();

                textbox_name.IsEnabled = true;
                comboBox.IsEnabled = true;
                textbox_ServerDir.IsEnabled = true;
                button_Browse.IsEnabled = true;
                progressbar_progress.IsIndeterminate = false;

                textblock_progress.Text = "[ERROR] Fail to import";
                button_Import.Content = "Import";

                return;
            }

            Images.Row selectedgame = comboBox.SelectedItem as Images.Row;
            label_gamewarn.Content = (selectedgame == null) ? "Please select a game server" : "";
            label_namewarn.Content = (string.IsNullOrWhiteSpace(textbox_name.Text)) ? "Server name cannot be null" : "";
            label_ServerDirWarn.Content = (!Directory.Exists(textbox_ServerDir.Text)) ? "Server Dir is invalid" : "";
            if (string.IsNullOrWhiteSpace(textbox_name.Text) || selectedgame == null || !Directory.Exists(textbox_ServerDir.Text)) return;

            string servername = textbox_name.Text;
            string servergame = selectedgame.Name;

            //Check is the path contain game server files
            switch (servergame)
            {
                case ("Counter-Strike: Global Offensive Dedicated Server"):
                case ("Team Fortress 2 Dedicated Server"):
                    {
                        string srcdsPath = textbox_ServerDir.Text + @"\srcds.exe";
                        if (!File.Exists(srcdsPath))
                        {
                            label_ServerDirWarn.Content = "Invalid Path! Fail to find srcds.exe";

                            return;
                        }

                        break;
                    }
                case ("Minecraft Server"): break;
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

            //Import start
            textbox_name.IsEnabled = false;
            comboBox.IsEnabled = false;
            textbox_ServerDir.IsEnabled = false;
            button_Browse.IsEnabled = false;
            progressbar_progress.IsIndeterminate = true;

            textblock_progress.Text = "Importing";
            button_Import.Content = "Cancel Import";

            bool isImportSuccess = false;

            string xcopyPath = Environment.GetEnvironmentVariable("WINDIR") + @"\System32\xcopy.exe";
            ProcessStartInfo psi = new ProcessStartInfo(xcopyPath);
            psi.Arguments = string.Format("\"{0}\" \"{1}\" /E /I /-Y", textbox_ServerDir.Text, installPath);
            psi.WindowStyle = ProcessWindowStyle.Minimized;

            pImporter = Process.Start(psi);

            await Task.Run(() => pImporter.WaitForExit());

            if (pImporter == null)
            {
                textbox_name.IsEnabled = true;
                comboBox.IsEnabled = true;
                textbox_ServerDir.IsEnabled = true;
                button_Browse.IsEnabled = true;
                progressbar_progress.IsIndeterminate = false;

                textblock_progress.Text = "[ERROR] Fail to import";
                button_Import.Content = "Import";

                return;
            }

            if (pImporter.HasExited)
            if (pImporter.ExitCode == 0)
            {
                isImportSuccess = true;
            }

            if (isImportSuccess)
            {
                switch (servergame)
                {
                    case ("Counter-Strike: Global Offensive Dedicated Server"): break;
                    case ("Team Fortress 2 Dedicated Server"):
                        {
                            GameServer.TF2 tf2server = new GameServer.TF2();
                            serverConfig.CreateServerDirectory();
                            serverConfig.CreateWindowsGSMConfig(servergame, servername, GetIPAddress(), GetAvailablePort(tf2server.port), tf2server.defaultmap, tf2server.maxplayers, "");

                            break;
                        }
                    case ("Minecraft Server"): break;
                }

                MainWindow WindowsGSM = (MainWindow)System.Windows.Application.Current.MainWindow;

                Table row = new Table();
                row.ID = serverConfig.ServerID;
                row.Game = serverConfig.ServerGame;
                row.Icon = Images.ServerIcon.ResourceManager.GetString(servergame);
                row.Status = "Stopped";
                row.Name = serverConfig.ServerName;
                row.IP = serverConfig.ServerIP;
                row.Port = serverConfig.ServerPort;
                row.Defaultmap = serverConfig.ServerMap;
                row.Maxplayers = serverConfig.ServerMaxPlayer;
                WindowsGSM.ServerGrid.Items.Add(row);

                WindowsGSM.Log(serverConfig.ServerID, "Import: Success");

                Close();
            }
            else
            {
                textbox_name.IsEnabled = true;
                comboBox.IsEnabled = true;
                textbox_ServerDir.IsEnabled = true;
                button_Browse.IsEnabled = true;
                progressbar_progress.IsIndeterminate = false;

                textblock_progress.Text = "[ERROR] Fail to import";
                button_Import.Content = "Import";
            }
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

        private string GetIPAddress()
        {
            using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0))
            {
                socket.Connect("8.8.8.8", 65530);
                IPEndPoint endPoint = socket.LocalEndPoint as IPEndPoint;
                return endPoint.Address.ToString();
            }
        }

        private string GetAvailablePort(string defaultport)
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
    }
}
