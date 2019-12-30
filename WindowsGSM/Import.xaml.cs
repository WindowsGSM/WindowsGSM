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
        private readonly GameServer.Action.Import gameServerAction;

        private Process pImporter;

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
            textbox_name.Text = "WindowsGSM - Server #" + serverConfig.ServerID;

            gameServerAction = new GameServer.Action.Import(serverConfig);
        }

        private async void Button_Import_Click(object sender, RoutedEventArgs e)
        {
            if (pImporter != null)
            {
                if (!pImporter.HasExited)
                {
                    pImporter.Kill();
                }

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
            if (string.IsNullOrWhiteSpace(textbox_name.Text) || selectedgame == null || !Directory.Exists(textbox_ServerDir.Text))
            {
                return;
            }

            string servername = textbox_name.Text;
            string servergame = selectedgame.Name;

            if (!gameServerAction.CanImport(servergame, textbox_ServerDir.Text))
            {
                label_ServerDirWarn.Content = gameServerAction.Error;

                return;
            }

            string installPath = MainWindow.WGSM_PATH + @"\servers\" + serverConfig.ServerID + @"\serverfiles";
            if (Directory.Exists(installPath))
            {
                await Task.Run(() =>
                {
                    try
                    {
                        Directory.Delete(installPath, true);
                    }
                    catch
                    {

                    }
                });

                if (Directory.Exists(installPath))
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
            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = xcopyPath,
                Arguments = string.Format("\"{0}\" \"{1}\" /E /I /-Y", textbox_ServerDir.Text, installPath),
                WindowStyle = ProcessWindowStyle.Minimized
            };
            
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
            {
                if (pImporter.ExitCode == 0)
                {
                    isImportSuccess = true;
                }
            }

            if (isImportSuccess)
            {
                gameServerAction.CreateServerConfigs(servergame, servername);

                MainWindow WindowsGSM = (MainWindow)System.Windows.Application.Current.MainWindow;

                Function.ServerTable row = new Function.ServerTable
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
    }
}
