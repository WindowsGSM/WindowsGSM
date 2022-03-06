using System.Runtime.InteropServices;

namespace WindowsPseudoConsole.Interop.Definitions
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct SecurityAttributes
    {
        public int nLength;
        public IntPtr lpSecurityDescriptor;
        [MarshalAs(UnmanagedType.Bool)]
        public bool bInheritHandle;

        public static readonly SecurityAttributes Zero = new SecurityAttributes
        {
            nLength = Marshal.SizeOf(typeof(SecurityAttributes)),
            bInheritHandle = true,
            lpSecurityDescriptor = IntPtr.Zero
        };
    }
}
