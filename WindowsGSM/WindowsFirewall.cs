using System;
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

        public bool IsRuleExist()
        {
            INetFwMgr netFwMgr = (INetFwMgr)Activator.CreateInstance(Type.GetTypeFromProgID("HNetCfg.FwMgr"));

            foreach (INetFwAuthorizedApplication app in netFwMgr.LocalPolicy.CurrentProfile.AuthorizedApplications)
            {
                if (app.ProcessImageFileName.ToLower() == Path.ToLower())
                {
                    return true;
                }
            }

            return false;
        }

        public void AddRule()
        {
            INetFwMgr netFwMgr = (INetFwMgr)Activator.CreateInstance(Type.GetTypeFromProgID("HNetCfg.FwMgr"));
            INetFwAuthorizedApplication app = (INetFwAuthorizedApplication)Activator.CreateInstance(Type.GetTypeFromProgID("HNetCfg.FwAuthorizedApplication"));

            app.Name = Name;
            app.ProcessImageFileName = Path;
            app.Enabled = true;

            netFwMgr.LocalPolicy.CurrentProfile.AuthorizedApplications.Add(app);
        }

        public void RemoveRule()
        {
            INetFwMgr netFwMgr = (INetFwMgr)Activator.CreateInstance(Type.GetTypeFromProgID("HNetCfg.FwMgr"));
            netFwMgr.LocalPolicy.CurrentProfile.AuthorizedApplications.Remove(Path);
        }

        //Remove the firewall rule by similar path
        public void RemoveRuleEx()
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
    }
}
