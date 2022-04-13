using Microsoft.Extensions.Hosting.WindowsServices;
using System.Diagnostics;
using System.Text;
using WindowsGSM.BatchScripts;
using WindowsGSM.Services;
using WindowsPseudoConsole;
using WindowsPseudoConsole.Interop.Definitions;

namespace WindowsGSM.Utilities
{
    /// <summary>
    /// ProcessEx
    /// </summary>
    public class ProcessEx
    {
        public enum ConsoleType
        {
            PseudoConsole, Redirect, Windowed
        }

        private Process? _process;
        private ConPTY? _pseudoConsole;
        private readonly StringBuilder _output = new();

        public Process? Process => _process;

        public int? Id
        {
            get
            {
                try
                {
                    return _process != null && !_process.HasExited ? _process.Id : null;
                }
                catch
                {
                    return null;
                }
            }
        }

        public int? ExitCode
        {
            get
            {
                try
                {
                    return _process != null && _process.HasExited ? _process.ExitCode : null;
                }
                catch
                {
                    return null;
                }
            }
        }

        public DateTime? StartTime
        {
            get
            {
                try
                {
                    return _process != null && !_process.HasExited ? _process.StartTime : null;
                }
                catch
                {
                    return null;
                }
            }
        }

        public string Output => _output.ToString();

        public ConsoleType? Mode { get; private set; }

        public event Action<string>? OutputDataReceived;
        public event Action<int>? Exited;
        public event Action? Cleared;

        public void UsePseudoConsole(ProcessStartInfo processStartInfo)
        {
            Mode = ConsoleType.PseudoConsole;

            _pseudoConsole = new()
            {
                WorkingDirectory = processStartInfo.WorkingDirectory,
                FileName = processStartInfo.FileName,
                Arguments = processStartInfo.Arguments,
            };
            _pseudoConsole.OutputDataReceived += (s, data) => AddOutput(data);
            _pseudoConsole.Exited += (sender, _) => (sender as ConPTY)?.Dispose();
        }

        public void UseRedirect(ProcessStartInfo processStartInfo)
        {
            Mode = ConsoleType.Redirect;

            _process = new()
            {
                StartInfo = processStartInfo,
            };
            _process.EnableRaisingEvents = true;
            _process.OutputDataReceived += (s, e) =>
            {
                if (e.Data != null)
                {
                    AddOutput(e.Data + "\r\n");
                }
            };
            _process.ErrorDataReceived += (s, e) =>
            {
                if (e.Data != null)
                {
                    AddOutput("ERROR: " + e.Data + "\r\n");
                }
            };
            _process.Exited += (s, e) => Exited?.Invoke(_process.ExitCode);
        }

        public void UseWindowed(ProcessStartInfo processStartInfo)
        {
            Mode = ConsoleType.Windowed;

            _process = new()
            {
                StartInfo = processStartInfo,
            };
        }

        public async Task Start()
        {
            ClearOutput();

            if (Mode == ConsoleType.PseudoConsole && _pseudoConsole != null)
            {
                ProcessInfo processInfo = _pseudoConsole.Start(1000);
                _process = Process.GetProcessById(processInfo.dwProcessId);
                _process.EnableRaisingEvents = true;
                _process.Exited += (s, e) => Exited?.Invoke(_process.ExitCode);
            }
            else
            {
                if (_process == null)
                {
                    throw new Exception("Process is null");
                }

                if (Mode == ConsoleType.Redirect)
                {
                    await TaskEx.Run(() => _process.Start());

                    if (_process.StartInfo.RedirectStandardOutput)
                    {
                        _process.BeginOutputReadLine();
                    }

                    if (_process.StartInfo.RedirectStandardError)
                    {
                        _process.BeginErrorReadLine();
                    }
                }
                else
                {
                    string arguments = $"\"{_process.StartInfo.FileName} {_process.StartInfo.Arguments}\" \"{_process.StartInfo.WorkingDirectory}\"";
                    string output = await BatchScript.RunAsync("ProcessEx.Windowed.bat", arguments);
                    string pidString = output.TrimEnd().Split(new[] { '\n' }).Last();

                    if (int.TryParse(pidString, out int pid))
                    {
                        _process = await TaskEx.Run(() => Process.GetProcessById(pid));
                        _process.EnableRaisingEvents = true;
                        _process.Exited += (s, e) => Exited?.Invoke(_process.ExitCode);
                    }
                    else
                    {
                        throw new Exception("Process fail to start");
                    }
                }
                
                if (_process != null && !WindowsServiceHelpers.IsWindowsService())
                {
                    if (Mode == ConsoleType.Windowed || !_process.StartInfo.CreateNoWindow)
                    {
                        while (!_process.HasExited && !DllImport.ShowWindow(_process.MainWindowHandle, DllImport.WindowShowStyle.Minimize))
                        {
                            await Task.Delay(1);
                        }
                    }

                    try
                    {
                        await TaskEx.Run(() => _process.WaitForInputIdle());
                    }
                    catch (InvalidOperationException)
                    {
                        // The process does not have a graphical interface.
                        // Ignore this exception
                    }

                    if (Mode == ConsoleType.Windowed || !_process.StartInfo.CreateNoWindow)
                    {
                        DllImport.ShowWindow(_process.MainWindowHandle, DllImport.WindowShowStyle.Hide);
                    }
                }
            }
        }

        public void Resize(short width, short height)
        {
            _pseudoConsole?.Resize(width, height);
        }

        public void Kill()
        {
            _process?.Kill();
        }

        /// <summary>
        /// Instructs the System.Diagnostics.Process component to wait the specified number of milliseconds for the associated process to exit.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public Task<bool> WaitForExit(int milliseconds)
        {
            if (_process == null)
            {
                throw new Exception("Process not found");
            }

            return Task.Run(() => _process.WaitForExit(milliseconds));
        }

        public async Task WriteLine(string data)
        {
            if (Mode == ConsoleType.PseudoConsole)
            {
                if (_pseudoConsole != null)
                {
                    await _pseudoConsole.WriteLineAsync(data);
                }
            }
            else if (_process != null)
            {
                if (Mode == ConsoleType.Redirect && _process.StartInfo.RedirectStandardInput)
                {
                    await _process.StandardInput.WriteLineAsync(data);
                    AddOutput(data + "\r\n");
                }
                else// if (Mode == ConsoleType.Windowed)
                {
                    await Task.Run(() => SendMessage(_process.MainWindowHandle, data, true));
                }
            }
        }

        public async Task Write(string data)
        {
            if (Mode == ConsoleType.PseudoConsole)
            {
                if (_pseudoConsole != null)
                {
                    await _pseudoConsole.WriteAsync(data);
                }
            }
            else if (_process != null)
            {
                if (Mode == ConsoleType.Redirect && _process.StartInfo.RedirectStandardInput)
                {
                    if (data[0] == 13)
                    {
                        await _process.StandardInput.WriteLineAsync();
                        AddOutput("\r\n");
                    }
                    else
                    {
                        await _process.StandardInput.WriteLineAsync(data);
                        AddOutput(data);
                    }
                }
                else// if (Mode == ConsoleType.Windowed)
                {
                    await Task.Run(() => SendMessage(_process.MainWindowHandle, data));
                }
            }
        }

        public void ToggleWindow()
        {
            if (_process == null)
            {
                return;
            }

            bool t = DllImport.ShowWindow(_process.MainWindowHandle, DllImport.WindowShowStyle.Hide);
            DllImport.ShowWindow(_process.MainWindowHandle, t ? DllImport.WindowShowStyle.Hide : DllImport.WindowShowStyle.ShowNormal);
            DllImport.SetForegroundWindow(_process.MainWindowHandle);
        }

        private static void SendMessage(IntPtr windowHandle, string message, bool sendEnter = false)
        {
            // Here is a minor error on PostMessage, when it sends repeated char, some char may disappear. Example: send 1111111, windows may receive 1111 or 11111
            for (int i = 0; i < message.Length; i++)
            {
                // This is the solution for the error stated above
                if (i > 0 && message[i] == message[i - 1])
                {
                    // Send a None key, break the repeat bug
                    DllImport.PostMessage(windowHandle, 0x0100, (IntPtr)0, (IntPtr)0);
                }

                DllImport.PostMessage(windowHandle, 0x0102, (IntPtr)message[i], (IntPtr)0);
            }

            if (sendEnter)
            {
                DllImport.PostMessage(windowHandle, 0x0100, (IntPtr)13, (IntPtr)(0 << 29 | 0));
            }
        }

        private void AddOutput(string data)
        {
            _output.Append(data);
            OutputDataReceived?.Invoke(data);
        }

        private void ClearOutput()
        {
            _output.Clear();
            Cleared?.Invoke();
        }
    }
}
