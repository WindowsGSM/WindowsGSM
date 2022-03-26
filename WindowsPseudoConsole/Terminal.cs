using Microsoft.Win32.SafeHandles;
using WindowsPseudoConsole.Interop;
using WindowsPseudoConsole.Interop.Definitions;

namespace WindowsPseudoConsole
{
    /// <summary>
    /// Terminal
    /// </summary>
    public class Terminal : IDisposable
    {
        private Pipe input;
        private Pipe output;
        private PseudoConsole console;
        private Process process;
        private bool disposed;

        /// <summary>
        /// Terminal
        /// </summary>
        public Terminal()
        {
            ConPtyFeature.ThrowIfVirtualTerminalIsNotEnabled();

            if (ConsoleApi.GetConsoleWindow() != IntPtr.Zero)
            {
                ConPtyFeature.TryEnableVirtualTerminalConsoleSequenceProcessing();
            }
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        ~Terminal()
        {
            Dispose(false);
        }

        /// <summary>
        /// Input Stream
        /// </summary>
        public FileStream Input { get; private set; }

        /// <summary>
        /// Output Stream
        /// </summary>
        public FileStream Output { get; private set; }

        /// <summary>
        /// Starts the console
        /// </summary>
        /// <param name="shellCommand"></param>
        /// <param name="consoleWidth"></param>
        /// <param name="consoleHeight"></param>
        /// <returns></returns>
        public ProcessInfo Start(string shellCommand, string workingDirectory, short consoleWidth, short consoleHeight)
        {
            input = new Pipe();
            output = new Pipe();

            console = PseudoConsole.Create(input.Read, output.Write, consoleWidth, consoleHeight);
            process = ProcessFactory.Start(shellCommand, workingDirectory, PseudoConsole.PseudoConsoleThreadAttribute, console.Handle);

            Input = new FileStream(input.Write, FileAccess.Write);
            Output = new FileStream(output.Read, FileAccess.Read);

            return process.ProcessInfo;
        }

        /// <summary>
        /// Resize the console
        /// </summary>
        /// <param name="consoleWidth"></param>
        /// <param name="consoleHeight"></param>
        public void Resize(short consoleWidth, short consoleHeight)
        {
            console?.Resize(consoleWidth, consoleHeight);
        }

        /// <summary>
        /// Immediately stops the associated console.
        /// </summary>
        public void Kill()
        {
            console?.Dispose();
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public WaitHandle BuildWaitHandler()
        {
            return new AutoResetEvent(false)
            {
                SafeWaitHandle = new SafeWaitHandle(process.ProcessInfo.hProcess, ownsHandle: false)
            };
        }

        /// <summary>
        /// Instructs the console to wait indefinitely for the associated process to exit.
        /// </summary>
        public void WaitForExit()
        {
            BuildWaitHandler().WaitOne(Timeout.Infinite);
        }

        /// <summary>
        /// Try get ExitCode
        /// </summary>
        /// <param name="exitCode"></param>
        /// <returns></returns>
        public bool TryGetExitCode(out uint exitCode)
        {
            return ProcessApi.GetExitCodeProcess(process.ProcessInfo.hProcess, out exitCode);
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
            {
                return;
            }

            disposed = true;
            process?.Dispose();
            console?.Dispose();

            if (disposing)
            {
                Input?.Dispose();
                Output?.Dispose();
            }

            input?.Dispose();
            output?.Dispose();
        }
    }
}
