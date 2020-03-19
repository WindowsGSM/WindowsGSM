using System;
using System.Diagnostics;
using System.Collections;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace WindowsGSM.Functions
{
    public class ServerConsole
    {
        [DllImport("user32.dll")]
        static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        private static readonly int MAX_LINE = 200;
        private readonly List<string> _consoleList = new List<string>();
        private readonly string _serverId;
        private int _lineNumber = 0;

        public ServerConsole(string serverId)
        {
            _serverId = serverId;
        }

        public void AddOutput(object sender, DataReceivedEventArgs args)
        {
            MainWindow.g_ServerConsoles[int.Parse(_serverId)].Add(args.Data);
        }

        public async void Input(Process process, string text, IntPtr mainWindow)
        {
            await Task.Run(() =>
            {
                if (!process.HasExited)
                {
                    if (process.StartInfo.RedirectStandardInput)
                    {
                        try
                        {
                            process.StandardInput.WriteLine(text);
                            Add(text);
                        }
                        catch
                        {
                            //ignore
                        }
                    }
                    else
                    {
                        SetForegroundWindow(mainWindow);
                        var current = GetForegroundWindow();
                        var wgsmWindow = Process.GetCurrentProcess().MainWindowHandle;
                        if (current != wgsmWindow)
                        {
                            SendWaitToMainWindow(text);
                            SendWaitToMainWindow("{ENTER}");
                            SetForegroundWindow(wgsmWindow);
                        }
                    }
                }
            });
        }

        public void InputFor7DTD(Process process, string text, IntPtr mainWindow)
        {
            if (!process.HasExited)
            {
                SetForegroundWindow(mainWindow);
                var current = GetForegroundWindow();
                var wgsmWindow = Process.GetCurrentProcess().MainWindowHandle;
                if (current != wgsmWindow)
                {
                    SendWaitToMainWindow("{TAB}");
                    SendWaitToMainWindow(text);
                    SendWaitToMainWindow("{TAB}");
                    SendWaitToMainWindow(text);
                    SendWaitToMainWindow("{ENTER}");
                    SetForegroundWindow(wgsmWindow);
                }
            }
        }

        public void Clear()
        {
            _consoleList.Clear();
        }

        public string Get()
        {
            return string.Join(Environment.NewLine, _consoleList.ToArray());
        }

        public string GetPreviousCommand()
        {
            --_lineNumber;
            return (_consoleList.Count == 0) ? "" : _consoleList[GetLineNumber()].ToString();
        }

        public string GetNextCommand()
        {
            ++_lineNumber;
            return (_consoleList.Count == 0) ? "" : _consoleList[GetLineNumber()].ToString();
        }

        private int GetLineNumber()
        {
            if (_lineNumber < 0)
            {
                _lineNumber = 0;
            }
            else if (_lineNumber >= _consoleList.Count)
            {
                _lineNumber = (_consoleList.Count <= 0) ? 0 : _consoleList.Count - 1;
            }

            return _lineNumber;
        }

        public void Add(string text)
        {
            if (_serverId == "0")
            {
                _lineNumber = _consoleList.Count + 1;

                if (_consoleList.Count > 0 && text == _consoleList[_consoleList.Count - 1].ToString())
                {
                    return;
                }
            }

            _consoleList.Add(text);
            if (_consoleList.Count > MAX_LINE)
            {
                _consoleList.RemoveAt(0);
            }

            if (_serverId != "0")
            {
                Refresh(_serverId);
            }
        }

        private void Refresh(string serverId)
        {
            if (System.Windows.Application.Current == null) { return; }

            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                var WindowsGSM = (MainWindow)System.Windows.Application.Current.MainWindow;
                if (WindowsGSM == null) { return; }

                WindowsGSM.RefreshConsoleList(serverId);
            });
        }

        public static void SendWaitToMainWindow(string keys)
        {
            try
            {
                SendKeys.SendWait(keys);
            }
            catch
            {
                /*
                    System.ComponentModel.Win32Exception (0x80004005): Access is denied
                    at System.Windows.Forms.SendKeys.SendInput(Byte[] oldKeyboardState, Queue previousEvents)
                    at System.Windows.Forms.SendKeys.Send(String keys, Control control, Boolean wait)
                    at System.Windows.Forms.SendKeys.SendWait(String keys)

                    This error may happen in Windows Server R2, UAC problem, not sure how to fix

                    https://github.com/WindowsGSM/WindowsGSM/issues/14
                */
            }
        }
    }
}
