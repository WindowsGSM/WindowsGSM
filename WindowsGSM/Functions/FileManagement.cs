using ICSharpCode.SharpZipLib.GZip;
using ICSharpCode.SharpZipLib.Tar;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;

namespace WindowsGSM.Functions
{
    public static class FileManagement
    {
        /// <summary>
        /// Extract .tar.gz file async
        /// </summary>
        /// <param name="sourceArchiveFileName"></param>
        /// <param name="destinationDirectoryName"></param>
        /// <returns></returns>
        public static async Task<bool> ExtractTarGZ(string sourceArchiveFileName, string destinationDirectoryName)
        {
            return await Task.Run(() =>
            {
                try
                {
                    using (Stream inStream = File.OpenRead(sourceArchiveFileName))
                    using (Stream gzipStream = new GZipInputStream(inStream))
                    using (TarArchive tarArchive = TarArchive.CreateInputTarArchive(gzipStream))
                    {
                        tarArchive.ExtractContents(destinationDirectoryName);
                    }

                    return true;
                }
                catch
                {
                    return false;
                }
            });
        }

        /// <summary>
        /// Extract .zip file async
        /// </summary>
        /// <param name="sourceArchiveFileName"></param>
        /// <param name="destinationDirectoryName"></param>
        /// <returns></returns>
        public static async Task<bool> ExtractZip(string sourceArchiveFileName, string destinationDirectoryName)
        {
            return await Task.Run(() =>
            {
                try
                {
                    ZipFile.ExtractToDirectory(sourceArchiveFileName, destinationDirectoryName);
                    return true;
                }
                catch
                {
                    return false;
                }
            });
        }

        /// <summary>
        /// Delete file async
        /// </summary>
        /// <param name="sourceArchiveFileName"></param>
        /// <param name="destinationDirectoryName"></param>
        /// <returns></returns>
        public static async Task<bool> DeleteAsync(string path)
        {
            return await Task.Run(() =>
            {
                try
                {
                    File.Delete(path);
                    return true;
                }
                catch
                {
                    return false;
                }
            });
        }
    }
}
