using System;
using System.IO;
using System.Threading.Tasks;

namespace WindowsGSM.Functions
{
    public static class DirectoryManagement
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
