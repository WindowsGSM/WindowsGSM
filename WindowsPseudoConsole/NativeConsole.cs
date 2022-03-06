using WindowsPseudoConsole.Interop;
using WindowsPseudoConsole.Interop.Definitions;

namespace WindowsPseudoConsole
{
    /// <summary>
    /// Native Console
    /// </summary>
    public class NativeConsole : IDisposable
    {
        private IntPtr handle;
        private bool isDisposed;
        private Pipe stdOut, stdErr, stdIn;

        /// <summary>
        /// Native Console
        /// </summary>
        /// <param name="hidden"></param>
        public NativeConsole(bool hidden = true)
        {
            Initialise(hidden);
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        ~NativeConsole()
        {
            Dispose(false);
        }

        /// <summary>
        /// StdOut
        /// </summary>
        public FileStream Output { get; private set; }

        /// <summary>
        /// StdErr
        /// </summary>
        public FileStream Error { get; private set; }

        /// <summary>
        /// StdIn
        /// </summary>
        public FileStream Input { get; private set; }

        /// <summary>
        /// Send CtrlEvent to the console
        /// </summary>
        /// <param name="ctrlEvent"></param>
        public static void SendCtrlEvent(CtrlEvent ctrlEvent)
        {
            ConsoleApi.GenerateConsoleCtrlEvent(ctrlEvent, 0);
        }

        /// <summary>
        /// Register OnClose Action
        /// </summary>
        /// <param name="action"></param>
        public static void RegisterOnCloseAction(Action action)
        {
            RegisterCtrlEventFunction((ctrlEvent) =>
            {
                if (ctrlEvent == CtrlEvent.CtrlClose)
                {
                    action();
                }

                return false;
            });
        }

        /// <summary>
        /// Register Ctrl Event Function
        /// </summary>
        /// <param name="function"></param>
        public static void RegisterCtrlEventFunction(CtrlEventDelegate function)
        {
            ConsoleApi.SetConsoleCtrlHandler(function, true);
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing)
        {
            if (isDisposed)
            {
                return;
            }

            isDisposed = true;

            if (disposing)
            {
                Input.Dispose();
                Output.Dispose();
                Error.Dispose();
            }

            ConsoleApi.FreeConsole();
            ReleaseUnmanagedResources();
        }

        private void ReleaseUnmanagedResources()
        {
            stdIn.Dispose();
            stdOut.Dispose();
            stdErr.Dispose();
        }

        private void Initialise(bool hidden)
        {
            if (!ConsoleApi.AllocConsole())
            {
                throw InteropException.CreateWithInnerHResultException("Could not allocate console. You may need to FreeConsole first.");
            }

            handle = ConsoleApi.GetConsoleWindow();

            if (handle != IntPtr.Zero)
            {
                ConsoleApi.ShowWindow(handle, hidden ? ShowState.SwHide : ShowState.SwShowDefault);
            }

            RegisterOnCloseAction(ReleaseUnmanagedResources);

            CreateStdOutPipe();
            CreateStdErrPipe();
            CreateStdInPipe();
        }

        private void CreateStdOutPipe()
        {
            stdOut = new Pipe();
            if (!ConsoleApi.SetStdHandle(StdHandle.OutputHandle, stdOut.Write.DangerousGetHandle()))
            {
                throw InteropException.CreateWithInnerHResultException("Could not redirect STDOUT.");
            }
            Output = new FileStream(stdOut.Read, FileAccess.Read);
        }

        private void CreateStdErrPipe()
        {
            stdErr = new Pipe();
            if (!ConsoleApi.SetStdHandle(StdHandle.ErrorHandle, stdErr.Write.DangerousGetHandle()))
            {
                throw InteropException.CreateWithInnerHResultException("Could not redirect STDERR.");
            }
            Error = new FileStream(stdErr.Read, FileAccess.Read);
        }

        private void CreateStdInPipe()
        {
            stdIn = new Pipe();
            if (!ConsoleApi.SetStdHandle(StdHandle.InputHandle, stdIn.Read.DangerousGetHandle()))
            {
                throw InteropException.CreateWithInnerHResultException("Could not redirect STDIN.");
            }
            Input = new FileStream(stdIn.Write, FileAccess.Write);
        }
    }
}
