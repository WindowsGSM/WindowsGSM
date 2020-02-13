using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Reflection;

namespace WindowsGSM
{
    public class Program
    {
        [STAThread]
        public static void Main()
        {
            #region Install MahApps.Metro.dll
            string mahappsPath = Path.Combine(Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName), "MahApps.Metro.dll");
            bool shouldInstall = false;

            //Check file size
            if (File.Exists(mahappsPath))
            {
                if (new FileInfo(mahappsPath).Length < 1097216)
                {
                    File.Delete(mahappsPath);
                    shouldInstall = true;
                }
            }

            if (!File.Exists(mahappsPath) || shouldInstall)
            {
                try
                {
                    using (WebClient webClient = new WebClient())
                    {
                        webClient.DownloadFile("https://github.com/WindowsGSM/WindowsGSM/raw/master/packages/MahApps.Metro.1.6.5/lib/net47/MahApps.Metro.dll", mahappsPath);
                    }
                }
                catch
                {
                    File.Delete(mahappsPath);
                    return;
                }

                while (new FileInfo(mahappsPath).Length < 1097216) { }
            }
            #endregion

            AppDomain.CurrentDomain.AssemblyResolve += (sender, args) =>
            {
                var resourceName = Assembly.GetExecutingAssembly().GetName().Name + ".ReferencesEx." + new AssemblyName(args.Name).Name + ".dll";

                using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName))
                {
                    if (stream != null)
                    {
                        var assemblyData = new byte[stream.Length];
                        stream.Read(assemblyData, 0, assemblyData.Length);
                        return Assembly.Load(assemblyData);
                    }
                }

                return null;
            };

            App.Main();
        }
    }
}
