using System.Runtime.InteropServices;

namespace WindowsGSM.Utilities
{
    internal static class DllImport
    {
        public enum WindowShowStyle : uint
        {
            Hide = 0,
            ShowNormal = 1,
            Show = 5,
            Minimize = 6,
            ShowMinNoActivate = 7
        }

        [DllImport("user32.dll")]
        public static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        public static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = false)]
        public static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        public static extern bool ShowWindow(IntPtr hWnd, WindowShowStyle nCmdShow);

        [DllImport("user32.dll")]
        public static extern int SetWindowText(IntPtr hWnd, string windowName);
    }
}
