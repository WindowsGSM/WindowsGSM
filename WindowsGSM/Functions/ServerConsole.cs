using System;
using System.Text;
using System.Diagnostics;
using System.Collections;
using System.Windows.Documents;
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

        public int LineNumber = 0;

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
            switch (serverGame)
            {
                case GameServer._7DTD.FullName: return GameServer._7DTD.ToggleConsole;
                case GameServer.CS.FullName: return GameServer.CS.ToggleConsole;
                case GameServer.CSCZ.FullName: return GameServer.CSCZ.ToggleConsole;
                case GameServer.CSGO.FullName: return GameServer.CSGO.ToggleConsole;
                case GameServer.GMOD.FullName: return GameServer.GMOD.ToggleConsole;
                case GameServer.GTA5.FullName: return GameServer.GTA5.ToggleConsole;
                case GameServer.HL2DM.FullName: return GameServer.HL2DM.ToggleConsole;
                case GameServer.L4D2.FullName: return GameServer.L4D2.ToggleConsole;
                case GameServer.MC.FullName: return GameServer.MC.ToggleConsole;
                case GameServer.MCPE.FullName: return GameServer.MCPE.ToggleConsole;
                case GameServer.RUST.FullName: return GameServer.RUST.ToggleConsole;
                case GameServer.TF2.FullName: return GameServer.TF2.ToggleConsole;
            }

            return true;
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
            StringBuilder sb = new StringBuilder();

            foreach (object line in _arrayList)
            {
                if (line != null)
                {
                    sb.AppendLine(line.ToString());
                }
            }

            return sb.ToString() ?? "";
        }

        public string GetPreviousCommand()
        {
            --LineNumber;
            return (_arrayList.Count == 0) ? "" : _arrayList[GetLineNumber()].ToString();
        }

        public string GetNextCommand()
        {
            ++LineNumber;
            return (_arrayList.Count == 0) ? "" : _arrayList[GetLineNumber()].ToString();
        }

        private int GetLineNumber()
        {
            if (LineNumber < 0)
            {
                LineNumber = 0;
                System.Media.SystemSounds.Asterisk.Play();
            }
            else if (LineNumber >= _arrayList.Count)
            {
                LineNumber = (_arrayList.Count <= 0) ? 0 : _arrayList.Count - 1;
                System.Media.SystemSounds.Asterisk.Play();
            }

            return LineNumber;
        }

        public void Add(string text)
        {
            if (_serverId == "0")
            {
                LineNumber = _arrayList.Count + 1;

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

            Refresh(_serverId);
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
                    WindowsGSM.console.Document.Blocks.Clear();
                    WindowsGSM.console.Document.Blocks.Add(new Paragraph(new Run(MainWindow.g_ServerConsoles[Int32.Parse(serverId)].Get())));
                    WindowsGSM.console.ScrollToEnd();
                }
            });
        }
    }
}
