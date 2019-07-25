using System.Diagnostics;
using System.IO;
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
        private readonly GameServerAction gameServerAction;

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

            gameServerAction = new GameServerAction(serverConfig);
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

            pInstaller = await gameServerAction.Install(servergame);
            bool IsSuccess = await gameServerAction.IsInstallSuccess(pInstaller, servergame, servername);

            if (IsSuccess)
            {
                MainWindow WindowsGSM = (MainWindow)System.Windows.Application.Current.MainWindow;

                GameServerTable row = new GameServerTable
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
                    textblock_progress.Text = "Fail to install";
                }

                button_install.Content = "Install";
            }
        }
    }
}
