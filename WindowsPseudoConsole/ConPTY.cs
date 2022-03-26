using System.Text;
using System.Text.RegularExpressions;
using WindowsPseudoConsole.Interop.Definitions;
using System;

namespace WindowsPseudoConsole
{
    /// <summary>
    /// Pseudo Console (ConPTY)
    /// </summary>
    public class ConPTY
    {
        /// <summary>
        /// Occurs when console title received
        /// </summary>
        public event EventHandler<string>? TitleReceived;

        /// <summary>
        /// Occurs each time console writes a line.
        /// </summary>
        public event EventHandler<string>? OutputDataReceived;

        /// <summary>
        /// Occurs when the console exits.
        /// </summary>
        public event EventHandler<int>? Exited;

        /// <summary>
        /// Working directory. Default: <see cref="Directory.GetCurrentDirectory()"/>
        /// </summary>
        public string WorkingDirectory { get; set; } = Directory.GetCurrentDirectory();

        /// <summary>
        /// Gets or sets the application or document to start.
        /// </summary>
        public string FileName { get; set; } = string.Empty;

        /// <summary>
        /// Arguments that pass to the console. Default: <see cref="string.Empty"/>
        /// </summary>
        public string Arguments { get; set; } = string.Empty;

        /// <summary>
        /// Filtering out ANSI escape sequences on <see cref="OutputDataReceived"/>. Default: <see langword="false"/>
        /// </summary>
        public bool FilterControlSequences { get; set; } = false;

        private Terminal? terminal;
        private Stream? inputStream;
        private bool disposed;

        /// <summary>
        /// Start pseudo console
        /// </summary>
        public ProcessInfo Start(short width = 120, short height = 30)
        {
            if (WorkingDirectory == null)
            {
                throw new Exception("WorkingDirectory is not set");
            }

            // Start pseudo console
            terminal = new Terminal();
            ProcessInfo processInfo = terminal.Start($"{FileName}{(string.IsNullOrEmpty(Arguments) ? string.Empty : $" {Arguments}")}", WorkingDirectory, width, height);

            // Save the inputStream
            inputStream = terminal.Input;

            // Read pseudo console output in the background
            Task.Run(() => ReadConPtyOutput(terminal.Output));

            // Wait the pseudo console exit in the background
            Task.Run(() =>
            {
                terminal.WaitForExit();

                // Call Exited event with exit code
                Exited?.Invoke(this, terminal.TryGetExitCode(out uint exitCode) ? (int)exitCode : -1);
            });

            return processInfo;
        }

        /// <summary>
        /// Resize pseudo console
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        public void Resize(short width, short height)
        {
            terminal?.Resize(width, height);
        }

        /// <summary>
        /// Write data to the console.
        /// </summary>
        /// <param name="data"></param>
        public void Write(char data) => Write(data.ToString());

        /// <summary>
        /// Write data to the console.
        /// </summary>
        /// <param name="data"></param>
        public void Write(string data)
        {
            var bytes = Encoding.UTF8.GetBytes(data);
            inputStream?.Write(bytes, 0, bytes.Length);
            inputStream?.Flush();
        }

        /// <summary>
        /// Write data to the console, followed by a break line character.
        /// </summary>
        /// <param name="data"></param>
        public void WriteLine(string data) => Write($"{data}\x0D");

        /// <summary>
        /// Write data to the console.
        /// </summary>
        /// <param name="data"></param>
        public Task WriteAsync(char data) => WriteAsync(data.ToString());

        /// <summary>
        /// Write data to the console.
        /// </summary>
        /// <param name="data"></param>
        public async Task WriteAsync(string data)
        {
            var bytes = Encoding.UTF8.GetBytes(data);

            if (inputStream != null)
            {
                await inputStream.WriteAsync(bytes);
                await inputStream.FlushAsync();
            }
        }

        /// <summary>
        /// Write data to the console, followed by a break line character.
        /// </summary>
        /// <param name="data"></param>
        public Task WriteLineAsync(string data) => WriteAsync($"{data}\x0D");

        private async Task ReadConPtyOutput(Stream output)
        {
            var regex = new Regex(@"\x1B(?:[@-Z\\-_]|\[[0-?]*[ -/]*[@-~])");

            try
            {
                using var reader = new StreamReader(output);
                char[] buffer = new char[1024];

                while (true)
                {
                    int readed = reader.Read(buffer, 0, buffer.Length);

                    if (readed > 0)
                    {
                        var outputData = new string(buffer.Take(readed).ToArray());

                        OutputDataReceived?.Invoke(this, FilterControlSequences ? regex.Replace(outputData, string.Empty) : outputData);
                    }

                    await Task.Delay(1).ConfigureAwait(false);
                }
            }
            catch (ObjectDisposedException)
            {
                // Disposed
            }
        }

        /// <summary>
        /// Release the resources
        /// </summary>
        /// <param name="disposing"></param>
        protected void Dispose(bool disposing)
        {
            if (disposed)
            {
                return;
            }

            disposed = true;

            if (disposing)
            {
                
            }

            terminal?.Dispose();
            inputStream?.Dispose();
        }

        /// <summary>
        /// Release the resources
        /// </summary>
        ~ConPTY()
        {
            Dispose(disposing: false);
        }

        /// <summary>
        /// Release the resources
        /// </summary>
        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
