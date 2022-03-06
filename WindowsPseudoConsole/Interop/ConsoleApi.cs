using Microsoft.Win32.SafeHandles;
using System.Runtime.InteropServices;
using WindowsPseudoConsole.Interop.Definitions;

namespace WindowsPseudoConsole.Interop
{
    internal partial class ConsoleApi
    {
        [DllImport("kernel32", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool AllocConsole();

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool CloseHandle(IntPtr hHandle);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool CreatePipe(
            out SafeFileHandle hReadPipe,
            out SafeFileHandle hWritePipe,
            ref SecurityAttributes lpPipeAttributes,
            uint nSize);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool DuplicateHandle(IntPtr hSourceProcessHandle,
           IntPtr hSourceHandle,
           IntPtr hTargetProcessHandle,
           out IntPtr lpTargetHandle,
           uint dwDesiredAccess,
           bool bInheritHandle,
           uint dwOptions);

        [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool FreeConsole();

        [DllImport("kernel32.dll")]
        public static extern IntPtr GetConsoleWindow();

        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern bool GetConsoleMode(SafeFileHandle handle, out uint mode);

        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern bool SetConsoleMode(SafeFileHandle hConsoleHandle, uint mode);

        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern SafeFileHandle GetStdHandle(StdHandle nStdHandle);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetStdHandle(StdHandle nStdHandle, IntPtr hHandle);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool ShowWindow(IntPtr hWnd, ShowState nCmdShow);

        [DllImport("kernel32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GenerateConsoleCtrlEvent(CtrlEvent dwCtrlEvent, uint dwProcessGroupId);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool SetConsoleCtrlHandler(CtrlEventDelegate callback, bool add);
    }
}
