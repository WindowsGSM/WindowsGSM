using System.Diagnostics;
using System.Text;
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
            PseudoConsole, RedirectStandard, Windowed
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

        public void UseRedirectStandard(ProcessStartInfo processStartInfo)
        {
            Mode = ConsoleType.RedirectStandard;

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
                ProcessInfo processInfo = _pseudoConsole.Start();
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

                if (Mode == ConsoleType.RedirectStandard)
                {
                    _process.Start();

                    if (_process.StartInfo.RedirectStandardOutput)
                    {
                        _process.BeginOutputReadLine();
                    }
                }
                else
                {
                    Process batchProcess = new()
                    {
                        StartInfo =
                        {
                            FileName = Path.Combine(GameServerService.BasePath, "ProcessEx.Windowed.bat"),
                            Arguments = $"\"{_process.StartInfo.FileName} {_process.StartInfo.Arguments}\" \"{_process.StartInfo.WorkingDirectory}\"",
                            UseShellExecute = false,
                            RedirectStandardOutput = true,
                        }
                    };

                    batchProcess.Start();

                    string output = await batchProcess.StandardOutput.ReadToEndAsync();
                    string pidString = output.TrimEnd().Split(new[] { '\n' }).Last();

                    if (int.TryParse(pidString, out int pid))
                    {
                        _process = Process.GetProcessById(pid);
                        _process.EnableRaisingEvents = true;
                        _process.Exited += (s, e) => Exited?.Invoke(_process.ExitCode);
                    }
                    else
                    {
                        throw new Exception("Process fail to start");
                    }
                }
                
                if (_process != null)
                {
                    if (Mode == ConsoleType.Windowed || !_process.StartInfo.CreateNoWindow)
                    {
                        while (!_process.HasExited && !DllImport.ShowWindow(_process.MainWindowHandle, DllImport.WindowShowStyle.Minimize));
                    }

                    await Task.Run(() =>
                    {
                        try
                        {
                            _process.WaitForInputIdle();
                        }
                        catch (InvalidOperationException)
                        {
                            // The process does not have a graphical interface.
                            // Ignore this exception
                        }
                    });

                    if (Mode == ConsoleType.Windowed || !_process.StartInfo.CreateNoWindow)
                    {
                        DllImport.ShowWindow(_process.MainWindowHandle, DllImport.WindowShowStyle.Hide);
                    }
                }
            }
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

        public void WriteLine(string data)
        {
            if (Mode == ConsoleType.PseudoConsole)
            {
                _pseudoConsole?.WriteLine(data);
            }
            else if (_process != null)
            {
                if (Mode == ConsoleType.RedirectStandard && _process.StartInfo.RedirectStandardInput)
                {
                    _process.StandardInput.WriteLine(data);
                    AddOutput(data + "\r\n");
                }
                else// if (Mode == ConsoleType.Windowed)
                {
                    SendMessage(_process.MainWindowHandle, data);
                    DllImport.PostMessage(_process.MainWindowHandle, 0x0100, (IntPtr)13, (IntPtr)(0 << 29 | 0));
                }
            }
        }

        public void Write(string data)
        {
            if (Mode == ConsoleType.PseudoConsole)
            {
                _pseudoConsole?.Write(data);
            }
            else if (_process != null)
            {
                if (Mode == ConsoleType.RedirectStandard && _process.StartInfo.RedirectStandardInput)
                {
                    if (data[0] == 13)
                    {
                        _process.StandardInput.WriteLine();
                        AddOutput("\r\n");
                    }
                    else
                    {
                        _process.StandardInput.Write(data);
                        AddOutput(data);
                    }
                }
                else// if (Mode == ConsoleType.Windowed)
                {
                    SendMessage(_process.MainWindowHandle, data);
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

        private static void SendMessage(IntPtr windowHandle, string message)
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
