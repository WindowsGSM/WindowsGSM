using System;
using System.IO;

namespace WindowsGSM.Functions
{
    public static class Logger
    {
        private static readonly string LogPath = Path.Combine(ServerPath.Get(ServerPath.FolderName.Logs), "WindowsGSM.log");
        private static readonly object _lock = new object();

        public static void Log(string message)
        {
            try
            {
                lock (_lock)
                {
                    string logEntry = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - {message}{Environment.NewLine}";
                    File.AppendAllText(LogPath, logEntry);
                    System.Diagnostics.Debug.WriteLine(message);
                }
            }
            catch
            {
                // Ignore logging errors to prevent recursive failures
            }
        }

        public static void Error(string message, Exception ex = null)
        {
            string errorMessage = $"ERROR: {message}";
            if (ex != null)
            {
                errorMessage += $" | Exception: {ex.Message} | StackTrace: {ex.StackTrace}";
            }
            Log(errorMessage);
        }

        public static void Info(string message)
        {
            Log($"INFO: {message}");
        }

        public static void Warn(string message)
        {
            Log($"WARN: {message}");
        }
    }
}
