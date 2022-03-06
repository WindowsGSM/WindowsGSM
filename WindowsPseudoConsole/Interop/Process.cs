using System.Runtime.InteropServices;
using WindowsPseudoConsole.Interop.Definitions;

namespace WindowsPseudoConsole.Interop
{
    internal class Process : IDisposable
    {
        private bool disposed = false;

        public Process(StartInfoExtended startupInfo, ProcessInfo processInfo)
        {
            StartupInfo = startupInfo;
            ProcessInfo = processInfo;
        }

        ~Process()
        {
            Dispose(false);
        }

        public StartInfoExtended StartupInfo { get; }

        public ProcessInfo ProcessInfo { get; }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
            {
                return;
            }

            // Free the attribute list
            if (StartupInfo.lpAttributeList != IntPtr.Zero)
            {
                ProcessApi.DeleteProcThreadAttributeList(StartupInfo.lpAttributeList);
                Marshal.FreeHGlobal(StartupInfo.lpAttributeList);
            }

            // Close process and thread handles
            if (ProcessInfo.hProcess != IntPtr.Zero)
            {
                ConsoleApi.CloseHandle(ProcessInfo.hProcess);
            }
            if (ProcessInfo.hThread != IntPtr.Zero)
            {
                ConsoleApi.CloseHandle(ProcessInfo.hThread);
            }

            disposed = true;
        }
    }
}
