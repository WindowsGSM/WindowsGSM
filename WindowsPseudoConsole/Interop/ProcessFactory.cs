using System.Runtime.InteropServices;
using WindowsPseudoConsole.Interop.Definitions;

namespace WindowsPseudoConsole.Interop
{
    internal static class ProcessFactory
    {
        public static Process Start(string command, string workingDirectory, IntPtr attributes, IntPtr hPC)
        {
            var startupInfo = ConfigureProcessThread(hPC, attributes);
            var processInfo = RunProcess(ref startupInfo, command, workingDirectory);
            return new Process(startupInfo, processInfo);
        }

        private static StartInfoExtended ConfigureProcessThread(IntPtr hPC, IntPtr attributes)
        {
            // this method implements the behavior described in https://docs.microsoft.com/en-us/windows/console/creating-a-pseudoconsole-session#preparing-for-creation-of-the-child-process

            var lpSize = IntPtr.Zero;
            var success = ProcessApi.InitializeProcThreadAttributeList(
                lpAttributeList: IntPtr.Zero,
                dwAttributeCount: 1,
                dwFlags: 0,
                lpSize: ref lpSize
            );

            if (success || lpSize == IntPtr.Zero) // we're not expecting `success` here, we just want to get the calculated lpSize
            {
                throw InteropException.CreateWithInnerHResultException("Could not calculate the number of bytes for the attribute list.");
            }

            var startupInfo = new StartInfoExtended();
            startupInfo.StartupInfo.cb = Marshal.SizeOf<StartInfoExtended>();
            startupInfo.lpAttributeList = Marshal.AllocHGlobal(lpSize);

            success = ProcessApi.InitializeProcThreadAttributeList(
                lpAttributeList: startupInfo.lpAttributeList,
                dwAttributeCount: 1,
                dwFlags: 0,
                lpSize: ref lpSize
            );

            if (!success)
            {
                throw InteropException.CreateWithInnerHResultException("Could not set up attribute list.");
            }

            success = ProcessApi.UpdateProcThreadAttribute(
                lpAttributeList: startupInfo.lpAttributeList,
                dwFlags: 0,
                attribute: attributes,
                lpValue: hPC,
                cbSize: (IntPtr)IntPtr.Size,
                lpPreviousValue: IntPtr.Zero,
                lpReturnSize: IntPtr.Zero
            );

            if (!success)
            {
                throw InteropException.CreateWithInnerHResultException("Could not set pseudoconsole thread attribute.");
            }

            return startupInfo;
        }

        private static ProcessInfo RunProcess(ref StartInfoExtended sInfoEx, string commandLine, string currentDirectory)
        {
            int securityAttributeSize = Marshal.SizeOf<SecurityAttributes>();
            var pSec = new SecurityAttributes { nLength = securityAttributeSize };
            var tSec = new SecurityAttributes { nLength = securityAttributeSize };
            var success = ProcessApi.CreateProcess(
                lpApplicationName: null,
                lpCommandLine: commandLine,
                lpProcessAttributes: ref pSec,
                lpThreadAttributes: ref tSec,
                bInheritHandles: false,
                dwCreationFlags: Constants.EXTENDED_STARTUPINFO_PRESENT,
                lpEnvironment: IntPtr.Zero,
                lpCurrentDirectory: currentDirectory,
                lpStartupInfo: ref sInfoEx,
                lpProcessInformation: out ProcessInfo pInfo
            );

            if (!success)
            {
                throw InteropException.CreateWithInnerHResultException("Could not create process.");
            }

            return pInfo;
        }
    }
}
