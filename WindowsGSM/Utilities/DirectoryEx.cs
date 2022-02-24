using WindowsGSM.Services;

namespace WindowsGSM.Utilities
{
    public static class DirectoryEx
    {
        /// <summary>
        /// Delete Directory
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static Task DeleteAsync(string path)
        {
            return DeleteAsync(path, false);
        }

        /// <summary>
        /// Delete Directory
        /// </summary>
        /// <param name="path"></param>
        /// <param name="recursive"></param>
        /// <returns></returns>
        public static Task DeleteAsync(string path, bool recursive)
        {
            return TaskEx.Run(() => Directory.Delete(path, recursive));
        }

        /// <summary>
        /// Create a unique temporary directory
        /// </summary>
        /// <returns>Directory path</returns>
        public static string CreateTemporaryDirectory()
        {
            string directory = Path.Combine(GameServerService.BasePath, "temps", Guid.NewGuid().ToString());
            Directory.CreateDirectory(directory);
            return directory;
        }

        /// <summary>
        /// Moves a file or a directory and its contents to a new location.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="target"></param>
        public static async Task MoveAsync(string sourceDirName, string destDirName, bool overwrite)
        {
            if (!overwrite)
            {
                await TaskEx.Run(() => Directory.Move(sourceDirName, destDirName));

                return;
            }

            string sourcePath = sourceDirName.TrimEnd('\\', ' ');
            string targetPath = destDirName.TrimEnd('\\', ' ');
            var files = Directory.EnumerateFiles(sourcePath, "*", SearchOption.AllDirectories).GroupBy(s => Path.GetDirectoryName(s));

            foreach (var folder in files)
            {
                string targetFolder = folder.Key!.Replace(sourcePath, targetPath);
                Directory.CreateDirectory(targetFolder);

                foreach (var file in folder)
                {
                    string targetFile = Path.Combine(targetFolder, Path.GetFileName(file));

                    if (File.Exists(targetFile))
                    {
                        await FileEx.DeleteAsync(targetFile);
                    }

                    File.Move(file, targetFile);
                }
            }

            await DeleteAsync(sourceDirName, true);
        }
    }
}
