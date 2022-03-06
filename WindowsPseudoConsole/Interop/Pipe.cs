using Microsoft.Win32.SafeHandles;
using System.Runtime.InteropServices;
using WindowsPseudoConsole.Interop.Definitions;

namespace WindowsPseudoConsole.Interop
{
    internal class Pipe : IDisposable
    {
        private SafeFileHandle write;
        private SafeFileHandle read;

        public Pipe()
            : this(SecurityAttributes.Zero) { }

        public Pipe(SecurityAttributes securityAttributes)
        {
            if (!ConsoleApi.CreatePipe(out read, out write, ref securityAttributes, 0))
            {
                throw new InteropException("Failed to create pipe.",
                    Marshal.GetExceptionForHR(Marshal.GetHRForLastWin32Error())!);
            }
        }

        ~Pipe()
        {
            Dispose(false);
        }

        public SafeFileHandle Read => read;

        public SafeFileHandle Write => write;

        public void MakeReadNoninheritable(IntPtr processHandle)
        {
            MakeHandleNoninheritable(ref read, processHandle);
        }

        public void MakeWriteNoninheritable(IntPtr processHandle)
        {
            MakeHandleNoninheritable(ref write, processHandle);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            read.Dispose();
            write.Dispose();
        }

        private void MakeHandleNoninheritable(ref SafeFileHandle handler, IntPtr processHandle)
        {
            // Create noninheritable read handle and close the inheritable read handle.
            IntPtr handleClone;
            if (!ConsoleApi.DuplicateHandle(
                    processHandle,
                    handler.DangerousGetHandle(),
                    processHandle,
                    out handleClone,
                    0,
                    false,
                    Constants.DUPLICATE_SAME_ACCESS))
            {
                throw InteropException.CreateWithInnerHResultException("Couldn't duplicate the handle.");
            }

            SafeFileHandle toRelease = handler;
            handler = new SafeFileHandle(handleClone, true);
            toRelease.Dispose();
        }
    }
}
