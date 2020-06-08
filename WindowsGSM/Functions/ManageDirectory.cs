using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WindowsGSM.Functions
{
    static class ManageDirectory
    {
        public static async Task<bool> DeleteAsync(string path, bool recursive)
        {
            return await Task.Run(() =>
            {
                try
                {
                    Directory.Delete(path, recursive);
                    return true;
                }
                catch (Exception e)
                {
                    return false;
                }
            });
        }
    }
}
