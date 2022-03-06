namespace WindowsPseudoConsole.Interop
{
    internal static class Constants
    {
        public const uint PROC_THREAD_ATTRIBUTE_PSEUDOCONSOLE = 0x00020016;
        public const uint ENABLE_VIRTUAL_TERMINAL_PROCESSING = 0x0004;
        public const uint DISABLE_NEWLINE_AUTO_RETURN = 0x0008;
        public const uint EXTENDED_STARTUPINFO_PRESENT = 0x00080000;
        public const uint DUPLICATE_SAME_ACCESS = 0x00000002;
    }
}
