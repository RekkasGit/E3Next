using E3NextUI.Server;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace E3NextUI
{
    public partial class E3UI : Form
    {

        public E3UI()
        {
            InitializeComponent();
           
            _stopWatch.Start();
            string[] args = Environment.GetCommandLineArgs();
            _pubClient = new PubClient();
            Int32 port = Int32.Parse(args[1]);
            _pubClient.Start(port);

            SetDoubleBuffered(richTextBoxConsole);
            _consoleTask = Task.Factory.StartNew(() => { ProcessConsole(); }, CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
        }
        public static void SetDoubleBuffered(System.Windows.Forms.Control c)
        {
            //Taxes: Remote Desktop Connection and painting
            //http://blogs.msdn.com/oldnewthing/archive/2006/01/03/508694.aspx
            if (System.Windows.Forms.SystemInformation.TerminalServerSession)
                return;

            System.Reflection.PropertyInfo aProp =
                  typeof(System.Windows.Forms.Control).GetProperty(
                        "DoubleBuffered",
                        System.Reflection.BindingFlags.NonPublic |
                        System.Reflection.BindingFlags.Instance);

            aProp.SetValue(c, true, null);
        }
        private static System.Diagnostics.Stopwatch _stopWatch = new System.Diagnostics.Stopwatch();
        private static volatile bool _shouldProcess = true;
        public static System.Collections.Concurrent.ConcurrentQueue<string> _consoleLines = new System.Collections.Concurrent.ConcurrentQueue<string>();
        Task _consoleTask;
        static Int64 _nextConsoleProcess;
        private bool _consoleDataDirty = false;
        private object _objLock = new object();
        private PubClient _pubClient;
        
        private  void ProcessConsole()
        {
           
            while(_shouldProcess)
            {
                ProcessConsoleUI();
                System.Threading.Thread.Sleep(100);
            }
        }
        private delegate void ProcessConsoleUIDelegate();
        public void ProcessConsoleUI()
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new ProcessConsoleUIDelegate(ProcessConsoleUI),null);
            }
            else
            {
                if (_nextConsoleProcess < _stopWatch.ElapsedMilliseconds)
                {
                    lock (_objLock)
                    {
                        if (_consoleDataDirty)
                        {
                            //delete from the top if needed
                            string[] lines = richTextBoxConsole.Lines;
                            if (lines.Length>100)
                            {
                                richTextBoxConsole.ReadOnly = false;
                                richTextBoxConsole.SelectionStart = richTextBoxConsole.GetFirstCharIndexFromLine(0);
                                //ending length
                                Int32 endLength = 0;
                                for(Int32 i =0;i<50;i++)
                                {
                                    endLength = this.richTextBoxConsole.Lines[i].Length + 1;

                                }
                                richTextBoxConsole.SelectionLength = endLength;
                                richTextBoxConsole.SelectedText = String.Empty;
                                richTextBoxConsole.ReadOnly = true;

                            }
                            while(_consoleLines.Count>0)
                            {
                                string line;
                                if(_consoleLines.TryDequeue(out line))
                                {
                                    richTextBoxConsole.AppendText(line+"\r\n");
                                    richTextBoxConsole.SelectionStart = richTextBoxConsole.Text.Length;
                                    richTextBoxConsole.ScrollToCaret();
                                }
                            }
                            
                            _nextConsoleProcess = _stopWatch.ElapsedMilliseconds + 100;
                            _consoleDataDirty = false;
                        }
                    }
                }
            }
        }
        private delegate void ToggleShowDelegate();
        public void ToggleShow()
        {
            if(this.InvokeRequired)
            {
                this.Invoke(new ToggleShowDelegate(ToggleShow),null);
            }
            else
            {
                if(this.Visible)
                {
                    this.Visible = false;
                }
                else
                {
                    this.Visible = true;
                }
            }
        }

        private delegate void SetPlayerNameDelegate(string name);
        public void SetPlayerName(string name)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new SetPlayerNameDelegate(SetPlayerName), new object[] { name });
            }
            else
            {
                labelPlayerName.Text = name;
            }
        }
        private delegate void SetPlayerHPDelegate(string name);
        public void SetPlayerHP(string value)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new SetPlayerHPDelegate(SetPlayerHP), new object[] { value });
            }
            else
            {
                labelHPTotal.Text = value;
            }
        }
        private delegate void AddConsoleLineDelegate(string name);
        public void AddConsoleLine(string value)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new AddConsoleLineDelegate(AddConsoleLine), new object[] { value });
            }
            else
            {
                lock(_objLock)
                {
                    _consoleDataDirty = true;
                    _consoleLines.Enqueue(value);
                    if(_consoleLines.Count>300)
                    {
                        string outLine;
                        _consoleLines.TryDequeue(out outLine);
                    }
                }
              

            }
        }
        private void button1_Click(object sender, EventArgs e)
        {
            Form2 ft = new Form2();
            ft.Show();
        }

        private void E3UI_Load(object sender, EventArgs e)
        {

        }
        public void Shutdown()
        {
            _shouldProcess = false;
        }
    }
}
