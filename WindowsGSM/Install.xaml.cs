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

            textbox_installlog.Text = "";

            string servername = textbox_name.Text;
            string servergame = selectedgame.Name;

            serverConfig.CreateServerDirectory();

            dynamic gameServer = GameServer.Data.Class.Get(servergame, serverConfig);
            pInstaller = await gameServer.Install();

            if (pInstaller != null)
            {
                //Wait installer exit. Example: steamcmd.exe
                await Task.Run(() =>
                {
                    var reader = pInstaller.StandardOutput;
                    while (!reader.EndOfStream)
                    {
                        var nextLine = reader.ReadLine();
                        if (nextLine.Contains("Logging in user "))
                        {
                            nextLine += System.Environment.NewLine + "Please send the Login Token:";
                        }

                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            textbox_installlog.AppendText(nextLine + System.Environment.NewLine);
                            textbox_installlog.ScrollToEnd();
                        });
                    }

                    pInstaller?.WaitForExit();
                });
            }

            if (gameServer.IsInstallValid())
            {
                serverConfig.ServerIP = serverConfig.GetIPAddress();
                serverConfig.ServerPort = serverConfig.GetAvailablePort(gameServer.Port, gameServer.PortIncrements);

                // Create WindowsGSM.cfg
                serverConfig.SetData(servergame, servername, gameServer);
                serverConfig.CreateWindowsGSMConfig();

                // Create WindowsGSM.cfg and game server config
                try
                { 
                    gameServer = GameServer.Data.Class.Get(servergame, serverConfig);
                    gameServer.CreateServerCFG();
                }
                catch
                {
                    // ignore
                }

                Application.Current.Dispatcher.Invoke(() =>
                {
                    MainWindow WindowsGSM = (MainWindow)Application.Current.MainWindow;
                    WindowsGSM.LoadServerTable();
                    WindowsGSM.Log(serverConfig.ServerID, "Install: Success");

                    if (WindowsGSM.MahAppSwitch_SendStatistics.IsChecked ?? false)
                    {
                        var analytics = new Functions.GoogleAnalytics();
                        analytics.SendGameServerInstall(serverConfig.ServerID, servergame);
                    }

                    Close();
                });
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

        private void Button_SetAccount_Click(object sender, RoutedEventArgs e)
        {
            var steamCMD = new Installer.SteamCMD();
            steamCMD.CreateUserDataTxtIfNotExist();

            string userDataPath = Path.Combine(MainWindow.WGSM_PATH, @"installer\steamcmd\userData.txt");

            if (File.Exists(userDataPath))
            {
                Process.Start("notepad.exe", userDataPath);
            }
        }

        private void Button_SendToken_Click(object sender, RoutedEventArgs e)
        {
            if (pInstaller != null)
            {
                pInstaller.StandardInput.WriteLine(textBox_token.Text);
            }

            textBox_token.Text = "";
        }
    }
}
