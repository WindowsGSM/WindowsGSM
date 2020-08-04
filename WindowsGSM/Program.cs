using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Reflection;
using System.Windows;
using System.Windows.Documents;

namespace WindowsGSM
{
    public class Program
    {
        [STAThread]
        public static void Main()
        {
            // Due to a weird error, MahApps.Metro.dll cannot be embed inside WindowsGSM.exe,
            // therefore, MahApps.Metro.dll is stored in Resources, and copy the file to the WindowsGSM directory.
            string mahAppPath = Path.Combine(Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName), "MahApps.Metro.dll");
            if (!File.Exists(mahAppPath) || new FileInfo(mahAppPath).Length != 3425392) // Latest MahApps.Metro.dll byte size is 3425392
            {
                File.WriteAllBytes(mahAppPath, Properties.Resources.MahApps_Metro);
            }

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
