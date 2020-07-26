using System;
using System.Windows;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Linq;
using System.IO;
using Microsoft.Win32;

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
            bool forceStart = false, showCrashHint = false, commandLine = false;
            foreach (string arg in e.Args)
            {
                switch (arg.ToLower())
                {
                    case "/forcestart": forceStart = true; break;
                    case "/showcrashhint": showCrashHint = true; break;
                    case "/cli": commandLine = true; break;
                }
            }

            if (!forceStart)
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
            }

            string version = string.Concat(System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString().Reverse().Skip(2).Reverse());

            AppDomain.CurrentDomain.UnhandledException += (s, args) =>
            {
                string logPath = Path.Combine(Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName), "Logs");
                Directory.CreateDirectory(logPath);

                string logFile = Path.Combine(logPath, $"CRASH_{DateTime.Now.ToString("yyyyMMdd")}.log");
                File.AppendAllText(logFile, $"WindowsGSM v{version}{(commandLine ? " - CLI version" : "")}\n\n" + args.ExceptionObject.ToString());

                RegistryKey key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\WindowsGSM");
                if (key != null)
                {
                    bool shouldRestart = ((key.GetValue("RestartOnCrash") ?? false).ToString() == "True") ? true : false;
                    if (shouldRestart)
                    {
                        Process p = new Process()
                        {
                            StartInfo =
                            {
                                WorkingDirectory = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName),
                                FileName = "cmd.exe",
                                Arguments = $"/C echo WindowsGSM will auto restart after 10 seconds... & echo Close this windows to cancel... & ping 0 -w 1000 -n 10 > NUL & start WindowsGSM.exe /ForceStart /ShowCrashHint{(commandLine ? " /CLI" : "")}",
                                UseShellExecute = false
                            }
                        };
                        p.Start();
                        Process.GetCurrentProcess().Kill();
                    }
                }
            };

            if (commandLine)
            {
                MainConsole.Show();
                Console.Title = $"WindowsGSM v{version} - CLI version";
            }
            else
            {
                MainWindow mainwindow = new MainWindow(showCrashHint);
                mainwindow.Show();
            }
        }
    }
}
