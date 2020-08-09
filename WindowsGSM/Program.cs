using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using WindowsGSM.Functions;

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

            string roslynBase = Path.Combine(Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName), ServerPath.FolderName.Bin);
            Directory.CreateDirectory(roslynBase);
            if (!Directory.Exists(Path.Combine(roslynBase, "roslyn")))
            {
                string roslynZipPath = Path.Combine(roslynBase, "roslyn.zip");
                if (!File.Exists(roslynZipPath) || new FileInfo(roslynZipPath).Length != 7529158) // Latest roslyn.zip byte size is 7529158
                {
                    File.WriteAllBytes(roslynZipPath, Properties.Resources.roslyn);
                    ZipFile.ExtractToDirectory(roslynZipPath, roslynBase);
                    File.Delete(roslynZipPath);
                }
            }

            string ntsJsonPath = Path.Combine(Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName), ServerPath.FolderName.Bin, "Newtonsoft.Json.dll");
            if (!File.Exists(ntsJsonPath) || new FileInfo(ntsJsonPath).Length != 700336) // Latest Newtonsoft.Json.dll byte size is 700336
            {
                File.WriteAllBytes(ntsJsonPath, Properties.Resources.Newtonsoft_Json);
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
