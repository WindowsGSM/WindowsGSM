using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WindowsGSM.Functions
{
    static class Path
    {
        public static string Get(string serverid)
        {
            return System.IO.Path.Combine(MainWindow.WGSM_PATH, "servers", serverid);
        }

        public static string GetConfigs(string serverid, string path = "")
        {
            return System.IO.Path.Combine(Get(serverid), "configs", path);
        }

        public static string GetServerFiles(string serverid, string path = "")
        {
            return System.IO.Path.Combine(Get(serverid), "serverfiles", path);
        }
    }
}
