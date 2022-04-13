using System.Diagnostics;
using WindowsGSM.Services;
using WindowsGSM.Utilities;

namespace WindowsGSM.BatchScripts
{
    /// <summary>
    /// Batch Script
    /// </summary>
    public class BatchScript
    {
        public static readonly string BatchScriptsPath = Path.Combine(GameServerService.BasePath, "BatchScripts");

        /// <summary>
        /// Run batch script async
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="arguments"></param>
        /// <returns>batch script output</returns>
        public static async Task<string> RunAsync(string fileName, string arguments)
        {
            Process batchProcess = new()
            {
                StartInfo =
                {
                    FileName = Path.Combine(BatchScriptsPath, fileName),
                    Arguments = arguments,
                    RedirectStandardOutput = true
                }
            };

            await TaskEx.Run(() => batchProcess.Start());

            return await batchProcess.StandardOutput.ReadToEndAsync();
        }

        /// <summary>
        /// Run batch script async
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns>batch script output</returns>
        public static Task<string> RunAsync(string fileName)
        {
            return RunAsync(fileName, string.Empty);
        }
    }
}
