using System;
using System.Windows;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Linq;
using System.IO;

namespace WindowsGSM
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        protected override void OnStartup(StartupEventArgs e)
        {
            //LINQ query for windowsgsm old processes
            var wgsm = (from p in Process.GetProcesses()
                        where ((Predicate<Process>)(p_ =>
                        {
                            try
                            {
                                return p_.MainModule.FileName.Equals(Process.GetCurrentProcess().MainModule.FileName);
                            }
                            catch
                            {
                                return false;
                            }
                        }))(p)
                        select p).ToList();

            //Display the opened WindowsGSM
            foreach (var process in wgsm)
            {
                if (process.Id != Process.GetCurrentProcess().Id)
                {
                    System.Media.SystemSounds.Beep.Play();
                    MessageBox.Show("Another instance is already running", "WindowsGSM already running", MessageBoxButton.OK, MessageBoxImage.Warning);
                    SetForegroundWindow(process.MainWindowHandle);
                    Process.GetCurrentProcess().Kill();
                }
            }

            AppDomain.CurrentDomain.UnhandledException += (s, args) =>
            {
                MessageBox.Show("Unhandled Exception: " + args.ExceptionObject, "Crash - Please screenshot this", MessageBoxButton.OK, MessageBoxImage.Error);

                string logPath = Path.Combine(Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName), "Logs");
                Directory.CreateDirectory(logPath);

                string logFile = Path.Combine(logPath, $"CRASH_{DateTime.Now.ToString("yyyyMMdd")}.log");
                if (!File.Exists(logFile))
                {
                    File.Create(logFile).Dispose();
                }

                File.AppendAllText(logFile, args.ExceptionObject.ToString());
            };

            MainWindow mainwindow = new MainWindow();
            mainwindow.Show();
        }
    }
}
