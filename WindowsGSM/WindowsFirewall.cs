using System;
using System.Threading.Tasks;
using NetFwTypeLib;

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
                    INetFwMgr netFwMgr = (INetFwMgr)Activator.CreateInstance(Type.GetTypeFromProgID("HNetCfg.FwMgr"));

                    foreach (INetFwAuthorizedApplication app in netFwMgr.LocalPolicy.CurrentProfile.AuthorizedApplications)
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
                    var netFwMgr = (INetFwMgr)Activator.CreateInstance(Type.GetTypeFromProgID("HNetCfg.FwMgr"));
                    var app = (INetFwAuthorizedApplication)Activator.CreateInstance(Type.GetTypeFromProgID("HNetCfg.FwAuthorizedApplication"));
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
                INetFwMgr netFwMgr = (INetFwMgr)Activator.CreateInstance(Type.GetTypeFromProgID("HNetCfg.FwMgr"));
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
                    INetFwMgr netFwMgr = (INetFwMgr)Activator.CreateInstance(Type.GetTypeFromProgID("HNetCfg.FwMgr"));

                    foreach (INetFwAuthorizedApplication app in netFwMgr.LocalPolicy.CurrentProfile.AuthorizedApplications)
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
    }
}
