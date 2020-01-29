using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;

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
            textbox_name.Text = "WindowsGSM - Server #" + serverConfig.ServerID;
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

                textbox_name.IsEnabled = true;
                comboBox.IsEnabled = true;
                progressbar_progress.IsIndeterminate = false;
                textblock_progress.Text = "Fail to install";
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
            else
            {
                Directory.CreateDirectory(installPath);
            }

            //Installation start
            textbox_name.IsEnabled = false;
            comboBox.IsEnabled = false;
            progressbar_progress.IsIndeterminate = true;

            textblock_progress.Text = "Installing";
            button_install.Content = "Cancel Installation";

            string servername = textbox_name.Text;
            string servergame = selectedgame.Name;

            serverConfig.CreateServerDirectory();

            dynamic gameServer = GameServer.ClassObject.Get(servergame, serverConfig);
            pInstaller = await gameServer.Install();

            if (pInstaller != null)
            {
                //Wait installer exit. Example: steamcmd.exe
                await Task.Run(() => pInstaller.WaitForExit());
            }

            if (gameServer.IsInstallValid())
            {
                //Create WindowsGSM.cfg
                serverConfig.CreateWindowsGSMConfig(servergame, servername, serverConfig.GetIPAddress(), serverConfig.GetAvailablePort(gameServer.port), gameServer.defaultmap, gameServer.maxplayers, "", gameServer.additional);

                //Create game server config
                gameServer = GameServer.ClassObject.Get(servergame, serverConfig);
                gameServer.CreateServerCFG();

                MainWindow WindowsGSM = (MainWindow)System.Windows.Application.Current.MainWindow;

                Functions.ServerTable row = new Functions.ServerTable
                {
                    ID = serverConfig.ServerID,
                    Game = serverConfig.ServerGame,
                    Icon = "/WindowsGSM;component/" + GameServer.Data.Icon.ResourceManager.GetString(servergame),
                    Status = "Stopped",
                    Name = serverConfig.ServerName,
                    IP = serverConfig.ServerIP,
                    Port = serverConfig.ServerPort,
                    Defaultmap = serverConfig.ServerMap,
                    Maxplayers = serverConfig.ServerMaxPlayer
                };
                WindowsGSM.ServerGrid.Items.Add(row);
                WindowsGSM.LoadServerTable();
                WindowsGSM.Log(serverConfig.ServerID, "Install: Success");

                Close();
            }
            else
            {
                textbox_name.IsEnabled = true;
                comboBox.IsEnabled = true;
                progressbar_progress.IsIndeterminate = false;

                if (pInstaller != null)
                {
                    textblock_progress.Text = "Fail to install [ERROR] Exit code: " + pInstaller.ExitCode.ToString();
                }
                else
                {
                    textblock_progress.Text = $"Fail to install {gameServer.Error}";
                }

                button_install.Content = "Install";
            }
        }
    }
}
