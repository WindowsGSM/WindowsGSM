using System;
using System.Threading.Tasks;
using System.Diagnostics;

namespace WindowsGSM
{
    class WindowsFirewall
    {
        private readonly string Name;
        private readonly string Path;

        public WindowsFirewall(string name, string path)
        {
            Name = name;
            Path = path;
        }

        public async Task<bool> IsRuleExist()
        {
            return await Task.Run(() =>
            {
                try
                {
                    dynamic netFwMgr = Activator.CreateInstance(Type.GetTypeFromProgID("HNetCfg.FwMgr"));

                    foreach (dynamic app in netFwMgr.LocalPolicy.CurrentProfile.AuthorizedApplications)
                    {
                        if (app.ProcessImageFileName.ToLower() == Path.ToLower())
                        {
                            return true;
                        }
                    }
                }
                catch
                {
                    return false;
                }

                return false;
            });
        }

        public async Task<bool> AddRule()
        {
            return await Task.Run(() =>
            {
                try
                {
                    dynamic netFwMgr = Activator.CreateInstance(Type.GetTypeFromProgID("HNetCfg.FwMgr"));
                    dynamic app = Activator.CreateInstance(Type.GetTypeFromProgID("HNetCfg.FwAuthorizedApplication"));
                    app.Name = Name;
                    app.ProcessImageFileName = Path;
                    app.Enabled = true;
                    netFwMgr.LocalPolicy.CurrentProfile.AuthorizedApplications.Add(app);
                    return true;
                }
                catch
                {
                    return false;
                }
            });
        }

        public void RemoveRule()
        {
            try
            {
                dynamic netFwMgr = Activator.CreateInstance(Type.GetTypeFromProgID("HNetCfg.FwMgr"));
                netFwMgr.LocalPolicy.CurrentProfile.AuthorizedApplications.Remove(Path);
            }
            catch
            {
                // ignore
            }
        }

        //Remove the firewall rule by similar path
        public async void RemoveRuleEx()
        {
            await Task.Run(() =>
            {
                try
                {
                    dynamic netFwMgr = Activator.CreateInstance(Type.GetTypeFromProgID("HNetCfg.FwMgr"));

                    foreach (dynamic app in netFwMgr.LocalPolicy.CurrentProfile.AuthorizedApplications)
                    {
                        string filename = app.ProcessImageFileName.ToLower();
                        if (filename.Contains(Path.ToLower()))
                        {
                            netFwMgr.LocalPolicy.CurrentProfile.AuthorizedApplications.Remove(app.ProcessImageFileName);
                        }
                    }
                }
                catch
                {
                    // ignore
                }
            });
        }

        public static async Task<bool> AddFirewallRuleAsync(string ruleName, string executablePath, string direction)
        {
            try
            {
                string arguments = $"advfirewall firewall add rule name=\"{ruleName}\" dir={direction} action=allow program=\"{executablePath}\" enable=yes";
                var processStartInfo = new ProcessStartInfo("netsh", arguments)
                {
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using (var process = Process.Start(processStartInfo))
                {
                    process.WaitForExit();
                }

                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
