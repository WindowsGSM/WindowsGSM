using Microsoft.Win32.SafeHandles;
using System.Runtime.InteropServices;
using WindowsPseudoConsole.Interop.Definitions;

namespace WindowsPseudoConsole.Interop
{
    internal static class ConPtyApi
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern int CreatePseudoConsole(
            Coordinates size,
            SafeFileHandle hInput,
            SafeFileHandle hOutput,
            uint dwFlags,
            out IntPtr phPC);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern int ResizePseudoConsole(IntPtr hPC, Coordinates size);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern int ClosePseudoConsole(IntPtr hPC);
    }
}
