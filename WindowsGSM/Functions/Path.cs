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

        public static string GetConfigs(string serverid, string path1 = "")
        {
            return System.IO.Path.Combine(Get(serverid), "configs", path1);
        }

        public static string GetServerFiles(string serverid)
        {
            return System.IO.Path.Combine(Get(serverid), "serverfiles");
        }

        public static string GetServerFiles(string serverid, string path1)
        {
            return System.IO.Path.Combine(Get(serverid), "serverfiles", path1);
        }

        public static string GetServerFiles(string serverid, string path1, string path2)
        {
            return System.IO.Path.Combine(Get(serverid), "serverfiles", path1, path2);
        }
    }
}
