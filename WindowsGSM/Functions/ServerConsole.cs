using System;
using System.Diagnostics;
using System.Collections;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace WindowsGSM.Functions
{
    public class ServerConsole
    {
        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        private static readonly int MAX_LINE = 50;
        private readonly ArrayList _arrayList = new ArrayList(MAX_LINE);
        private readonly string _serverId;
        private int _lineNumber = 0;

        public ServerConsole(string serverId)
        {
            _serverId = serverId;
        }

        public void AddOutput(object sender, DataReceivedEventArgs args)
        {
            MainWindow.g_ServerConsoles[Int32.Parse(_serverId)].Add(args.Data);
        }

        public static bool IsToggleable(string serverGame)
        {
            dynamic gameServer = GameServer.ClassObject.Get(serverGame, null);
            return gameServer.ToggleConsole;
        }

        public void Input(Process process, string text)
        {
            if (!process.HasExited)
            {
                try
                {
                    process.StandardInput.WriteLine(text);
                    Add(text);
                }
                catch
                {
                    SetForegroundWindow(process.MainWindowHandle);
                    SendKeys.SendWait(text);
                    SendKeys.SendWait("{ENTER}");
                    SetForegroundWindow(Process.GetCurrentProcess().MainWindowHandle);
                }
            }
        }

        public void InputFor7DTD(Process process, string text)
        {
            if (!process.HasExited)
            {
                SetForegroundWindow(process.MainWindowHandle);
                SendKeys.SendWait("{TAB}");
                SendKeys.SendWait(text);
                SendKeys.SendWait("{TAB}");
                SendKeys.SendWait(text);
                SendKeys.SendWait("{ENTER}");
                SetForegroundWindow(Process.GetCurrentProcess().MainWindowHandle);
            }
        }

        public void Clear()
        {
            _arrayList.Clear();
        }

        public string Get()
        {
            /*
                Note:
                Don't use foreach loop, StringBUilder
                It will cause error
            */

            string output = "";
            for (int i = 0; i < _arrayList.Count; i++)
            {
                if (_arrayList[i] != null)
                {
                    output += _arrayList[i] + Environment.NewLine;
                }
            }

            return output;
        }

        public string GetPreviousCommand()
        {
            --_lineNumber;
            return (_arrayList.Count == 0) ? "" : _arrayList[GetLineNumber()].ToString();
        }

        public string GetNextCommand()
        {
            ++_lineNumber;
            return (_arrayList.Count == 0) ? "" : _arrayList[GetLineNumber()].ToString();
        }

        private int GetLineNumber()
        {
            if (_lineNumber < 0)
            {
                _lineNumber = 0;
            }
            else if (_lineNumber >= _arrayList.Count)
            {
                _lineNumber = (_arrayList.Count <= 0) ? 0 : _arrayList.Count - 1;
            }

            return _lineNumber;
        }

        public void Add(string text)
        {
            if (_serverId == "0")
            {
                _lineNumber = _arrayList.Count + 1;

                if (_arrayList.Count > 0 && text == _arrayList[_arrayList.Count - 1].ToString())
                {
                    return;
                }
            }

            _arrayList.Add(text);

            if (_arrayList.Count > MAX_LINE)
            {
                _arrayList.RemoveAt(0);
            }

            if (_serverId != "0")
            {
                Refresh(_serverId);
            }
        }

        public static void Refresh(string serverId)
        {
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                var WindowsGSM = (MainWindow)System.Windows.Application.Current.MainWindow;
                if (WindowsGSM == null) { return; }
                var selectedRow = (ServerTable)WindowsGSM.ServerGrid.SelectedItem;

                if (selectedRow.ID == serverId)
                {
                    WindowsGSM.console.Text = MainWindow.g_ServerConsoles[Int32.Parse(serverId)].Get();
                    WindowsGSM.console.ScrollToEnd();
                }
            });
        }
    }
}
