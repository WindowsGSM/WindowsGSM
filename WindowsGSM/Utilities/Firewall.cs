using NetFwTypeLib;

namespace WindowsGSM.Utilities
{
    public class Firewall
    {
        /*
        private static readonly INetFwMgr _netFwMgr = (INetFwMgr)Activator.CreateInstance(Type.GetTypeFromProgID("HNetCfg.FwMgr"));

        public static Task AddApplication(string path)
        {
            return AddApplication(path, Path.GetFileName(path));
        }

        public static Task AddApplication(string path, string name)
        {
            return Task.Run(() =>
            {
                var app = (INetFwAuthorizedApplication)Activator.CreateInstance(Type.GetTypeFromProgID("HNetCfg.FwAuthorizedApplication"));
                app.Name = name;
                app.ProcessImageFileName = path;
                app.Enabled = true;
                app.Scope = NET_FW_SCOPE_.NET_FW_SCOPE_ALL;
                
                _netFwMgr.LocalPolicy.CurrentProfile.AuthorizedApplications.Add(app);
            }); 
        }

        public static Task RemoveApplication(string path)
        {
            return Task.Run(() =>
            {
                _netFwMgr.LocalPolicy.CurrentProfile.AuthorizedApplications.Remove(path);
            });
        }
        */
    }
}
