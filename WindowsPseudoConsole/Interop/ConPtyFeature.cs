using Microsoft.Win32.SafeHandles;
using WindowsPseudoConsole.Interop.Definitions;

namespace WindowsPseudoConsole.Interop
{
    internal static class ConPtyFeature
    {
        private static readonly object locker = new object();
        private static bool? isVirtualTerminalEnabled;
        private static bool? isVirtualTerminalConsoleSequeceEnabled;

        public static bool IsVirtualTerminalEnabled
        {
            get
            {
                if (isVirtualTerminalEnabled.HasValue)
                {
                    return isVirtualTerminalEnabled.Value;
                }

                // You must be running Windows 1903 (build >= 10.0.18362.0) or later to run ConPTY terminal
                // System.Runtime.InteropServices.RuntimeInformation.OSDescription;
                isVirtualTerminalEnabled = Environment.OSVersion.Platform == PlatformID.Win32NT
                    && Environment.OSVersion.Version >= new Version(6, 2, 9200);

                return isVirtualTerminalEnabled ?? false;
            }
        }

        public static bool IsVirtualTerminalConsoleSequeceEnabled
        {
            get
            {
                if (isVirtualTerminalConsoleSequeceEnabled.HasValue)
                {
                    return isVirtualTerminalConsoleSequeceEnabled.Value;
                }

                TryEnableVirtualTerminalConsoleSequenceProcessing();
                return isVirtualTerminalConsoleSequeceEnabled ?? false;
            }
        }

        public static void ThrowIfVirtualTerminalIsNotEnabled()
        {
            if (!IsVirtualTerminalEnabled)
            {
                throw new InteropException("A virtual terminal is not enabled, you must be running Windows 1903 (build >= 10.0.18362.0) or later.");
            }
        }

        public static void TryEnableVirtualTerminalConsoleSequenceProcessing()
        {
            if (isVirtualTerminalConsoleSequeceEnabled.HasValue)
            {
                return;
            }

            lock (locker)
            {
                if (isVirtualTerminalConsoleSequeceEnabled.HasValue)
                {
                    return;
                }

                try
                {
                    SetConsoleModeToVirtualTerminal();
                    isVirtualTerminalConsoleSequeceEnabled = true;
                }
                catch
                {
                    isVirtualTerminalConsoleSequeceEnabled = false;
                    throw;
                }
            }
        }

        private static void SetConsoleModeToVirtualTerminal()
        {
            SafeFileHandle stdIn = ConsoleApi.GetStdHandle(StdHandle.InputHandle);
            if (!ConsoleApi.GetConsoleMode(stdIn, out uint outConsoleMode))
            {
                throw InteropException.CreateWithInnerHResultException("Could not get console mode.");
            }

            outConsoleMode |= Constants.ENABLE_VIRTUAL_TERMINAL_PROCESSING | Constants.DISABLE_NEWLINE_AUTO_RETURN;
            if (!ConsoleApi.SetConsoleMode(stdIn, outConsoleMode))
            {
                throw InteropException.CreateWithInnerHResultException("Could not enable virtual terminal processing.");
            }
        }
    }
}
