using E3NextUI.Server;
using E3NextUI.Util;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace E3NextUI
{



    public partial class E3UI : Form
    {
        public static System.Diagnostics.Stopwatch _stopWatch = new System.Diagnostics.Stopwatch();
        public static volatile bool _shouldProcess = true;

        Task _consoleTask;
        Task _consoleMQTask;
        Task _consoleMeleeTask;
        Task _consoleSpellTask;
        Task _updateParse;
        private object _objLock = new object();
        public static DealerClient _dealClient;
        private PubClient _pubClient;
        private PubServer _pubServer;
        public static TextBoxInfo _console;
        public static TextBoxInfo _mqConsole;
        public static TextBoxInfo _meleeConsole;
        public static TextBoxInfo _spellConsole;
        public static string CharacterName;
        public static Int32 _parentProcess;
        public static object _objectLock = new object();



        public E3UI()
        {
            InitializeComponent();

            _stopWatch.Start();
            string[] args = Environment.GetCommandLineArgs();
            //AsyncIO.ForceDotNet.Force();
            if (args.Length > 1)
            {
                Int32 port = Int32.Parse(args[2]);
                //get this first as its used in the regex for parsing for name.
                _dealClient = new DealerClient(port);
                if (_dealClient != null)
                {
                    lock (_dealClient)
                    {
                        CharacterName = _dealClient.RequestData("${Me.CleanName}");
                        labelPlayerName.Text = CharacterName;

                    }
                }
                _pubClient = new PubClient();
                port = Int32.Parse(args[1]);
                _pubClient.Start(port);
                port = Int32.Parse(args[3]);
                _pubServer = new PubServer();
                _pubServer.Start(port);
                _parentProcess = Int32.Parse(args[4]);

            }
            SetDoubleBuffered(richTextBoxConsole);
            SetDoubleBuffered(richTextBoxMQConsole);
            SetDoubleBuffered(richTextBoxMelee);
            SetDoubleBuffered(richTextBoxSpells);

            _console = new TextBoxInfo() { textBox = richTextBoxConsole };
            _mqConsole = new TextBoxInfo() { textBox = richTextBoxMQConsole };
            _meleeConsole = new TextBoxInfo() { textBox = richTextBoxMelee };
            _spellConsole = new TextBoxInfo() { textBox = richTextBoxSpells };
    
            _consoleTask = Task.Factory.StartNew(() => { ProcessUI(_console); }, CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
            _consoleMQTask = Task.Factory.StartNew(() => { ProcessUI(_mqConsole); }, CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
            _consoleMeleeTask = Task.Factory.StartNew(() => { ProcessUI(_meleeConsole); }, CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
            _consoleSpellTask = Task.Factory.StartNew(() => { ProcessUI(_spellConsole); }, CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
            _updateParse = Task.Factory.StartNew(() => { ProcessParse(); }, CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);


        }
  
       
        private void ProcessParse()
        {
            while (_shouldProcess)
            {
                ProcesssBaseParse();
                
                if (_parentProcess > 0)
                {
                    if (!ProcessExists(_parentProcess))
                    {
                        Application.Exit();

                    }
                }

                System.Threading.Thread.Sleep(500);
            }
        }
        private delegate void ProcesssBaseParseDelegate();
        private void ProcesssBaseParse()
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new ProcesssBaseParseDelegate(ProcesssBaseParse), null);
            }
            else
            {
                //lets get the data from the line parser.
                lock (_objectLock)
                {
                    labelInCombatValue.Text = LineParser._currentlyCombat.ToString();
                }
                lock (LineParser._objectLock)
                {
                    if (!String.IsNullOrWhiteSpace(LineParser.PetName))
                    {
                        labelPetNameValue.Text = LineParser.PetName;
                    }
                    Int64 yourDamageTotal = LineParser._yourDamage.Sum();
                    labelYourDamageValue.Text = yourDamageTotal.ToString("N0");

                    Int64 petDamageTotal = LineParser._yourPetDamage.Sum();
                    labelPetDamageValue.Text = petDamageTotal.ToString("N0");


                    Int64 dsDamage = LineParser._yourDamageShieldDamage.Sum();
                    labelYourDamageShieldValue.Text = dsDamage.ToString("N0");

                    Int64 totalDamage = yourDamageTotal + petDamageTotal + dsDamage;
                    labelTotalDamageValue.Text = totalDamage.ToString("N0");

                    Int64 damageToyou = LineParser._damageToYou.Sum();
                    labelDamageToYouValue.Text = damageToyou.ToString("N0");

                    Int64 healingToyou = LineParser._healingToYou.Sum();
                    labelHealingYouValue.Text = healingToyou.ToString("N0");

                    //need to find the start of each colleciton
                    //and end of each collection taking the lowest of start
                    //and highest of end
                    Int64 startTime = 0;
                    Int64 endTime = 0;
                    if (LineParser._yourDamage.Count > 0)
                    {
                        if (startTime > LineParser._yourDamageTime[0] || startTime == 0)
                        {
                            startTime = LineParser._yourDamageTime[0];
                        }
                        if (endTime < LineParser._yourDamageTime[LineParser._yourDamageTime.Count - 1])
                        {
                            endTime = LineParser._yourDamageTime[LineParser._yourDamageTime.Count - 1];
                        }
                    }
                    if (LineParser._yourPetDamage.Count > 0)
                    {
                        if (startTime > LineParser._yourPetDamage[0] || startTime == 0)
                        {
                            startTime = LineParser._yourPetDamageTime[0];
                        }
                        if (endTime < LineParser._yourPetDamageTime[LineParser._yourPetDamageTime.Count - 1])
                        {
                            endTime = LineParser._yourPetDamageTime[LineParser._yourPetDamageTime.Count - 1];
                        }
                    }
                    if (LineParser._yourDamageShieldDamage.Count > 0)
                    {
                        if (startTime > LineParser._yourDamageShieldDamageTime[0] || startTime == 0)
                        {
                            startTime = LineParser._yourDamageShieldDamageTime[0];
                        }
                        if (endTime < LineParser._yourDamageShieldDamageTime[LineParser._yourDamageShieldDamageTime.Count - 1])
                        {
                            endTime = LineParser._yourDamageShieldDamageTime[LineParser._yourDamageShieldDamageTime.Count - 1];
                        }
                    }
                    Int64 totalTime = (endTime - startTime) / 1000;

                    labelTotalTimeValue.Text = (totalTime) + " seconds";

                    if (totalTime == 0) totalTime = 1;
                    Int64 totalDPS = totalDamage / totalTime;
                    Int64 yourDPS = yourDamageTotal / totalTime;
                    Int64 petDPS = petDamageTotal / totalTime;
                    Int64 dsDPS = dsDamage / totalTime;

                    labelTotalDamageDPSValue.Text = totalDPS.ToString("N0") + " dps";
                    labelYourDamageDPSValue.Text = yourDPS.ToString("N0") + " dps";
                    labelPetDamageDPSValue.Text = petDPS.ToString("N0") + " dps";
                    labelDamageShieldDPSValue.Text = dsDPS.ToString("N0") + " dps";

                }
            }
        }
        private void ProcessUI(TextBoxInfo textInfo)
        {
            while (_shouldProcess)
            {
                ProcessBaseUI(textInfo);
                System.Threading.Thread.Sleep(500);
            }

        }


        private delegate void ProcessBaseUIDelegate(TextBoxInfo textInfo);
        private void ProcessBaseUI(TextBoxInfo ti)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new ProcessBaseUIDelegate(ProcessBaseUI), new object[] { ti });
            }
            else
            {
                lock (ti)
                {
                    if (ti.nextProcess < _stopWatch.ElapsedMilliseconds)
                    {
                        if (ti.isPaused) return;

                        if (ti.isDirty)
                        {
                        
                            ti.sb.Clear();
                            Int32 count = ti.consoleBuffer.Size;
                            if (count > 50) count = 50;
                            for(Int32 i =count-1;i>=0;i--)
                            {
                                ti.sb.AppendLine(ti.consoleBuffer[i]);
                            }
                           
                            ti.textBox.Text = ti.sb.ToString();
                            ti.textBox.SelectionStart = ti.textBox.Text.Length;
                            ti.textBox.ScrollToCaret();
                            ti.nextProcess = _stopWatch.ElapsedMilliseconds + 100;
                            ti.isDirty = false;
                        }
                    }
                }
            }
        }


        private delegate void ToggleShowDelegate();
        public void ToggleShow()
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new ToggleShowDelegate(ToggleShow), null);
            }
            else
            {

                if (this.Visible)
                {
                    this.Visible = false; // Hide form window.

                }
                else
                {
                    this.Visible = true;
                    //Keeps the current topmost status of form
                    bool top = TopMost;
                    //Brings the form to top
                    TopMost = true;
                    //Set form's topmost status back to whatever it was
                    TopMost = top;
                }
            }
        }

        private delegate void SetPlayerDataDelegate(string name);
        public void SetPlayerName(string name)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new SetPlayerDataDelegate(SetPlayerName), new object[] { name });
            }
            else
            {
                labelPlayerName.Text = name;
            }
        }
        public void SetPlayerHP(string value)
        {
            if (value == labelHPTotal.Text) return;
            if (this.InvokeRequired)
            {
                this.Invoke(new SetPlayerDataDelegate(SetPlayerHP), new object[] { value });
            }
            else
            {
                labelHPTotal.Text = value;
            }
        }
        public void SetPlayerMP(string value)
        {
            if (value == labelManaCurrent.Text) return;
            if (this.InvokeRequired)
            {
                this.Invoke(new SetPlayerDataDelegate(SetPlayerMP), new object[] { value });
            }
            else
            {
                labelManaCurrent.Text = value;
            }
        }
        public void SetPlayerSP(string value)
        {
            if (value == labelStaminaValue.Text) return;
            if (this.InvokeRequired)
            {
                this.Invoke(new SetPlayerDataDelegate(SetPlayerSP), new object[] { value });
            }
            else
            {
                labelStaminaValue.Text = value;
            }
        }
        public void SetPlayerCasting(string value)
        {
            if (value == labelStaminaValue.Text) return;
            if (this.InvokeRequired)
            {
                this.Invoke(new SetPlayerDataDelegate(SetPlayerCasting), new object[] { value });
            }
            else
            {
                labelCastingValue.Text = value;
            }
        }
        public void AddConsoleLine(string value, TextBoxInfo ti)
        {
            lock (ti)
            {
                ti.isDirty = true;
                ti.consoleBuffer.PushFront(value);
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Form2 ft = new Form2();
            ft.Show();
        }

        public void Shutdown()
        {
            _shouldProcess = false;
        }

        private void textBoxConsoleInput_KeyDown(object sender, KeyEventArgs e)
        {
        if (e.KeyData == Keys.Enter)
            {
                //grab the data and send a command
                //do this to stop the 'ding' sound
                e.SuppressKeyPress = true;
                string value = ((TextBox)sender).Text;
                if (value.StartsWith("/"))
                {
                    PubServer._pubCommands.Enqueue(value);

                }
                ((TextBox)sender).Text = String.Empty;
            }

        }

        private void E3UI_FormClosing(object sender, FormClosingEventArgs e)
        {
            //set the variable that will stop all the while loops
            _shouldProcess = false;
        }

        private void buttonResetParse_Click(object sender, EventArgs e)
        {
            LineParser.Reset();
        }
      
        /// <summary>
        /// used to check if our parent process dies, so that we can close as well.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        private bool ProcessExists(int id)
        {
            return Process.GetProcesses().Any(x => x.Id == id);
        }

        private void buttonPauseConsoles_Click(object sender, EventArgs e)
        {
            lock (_spellConsole)
            { 
                if(_spellConsole.isPaused)
                {
                    buttonPauseConsoles.Text = "Pause Consoles";
                }
                else
                {
                    buttonPauseConsoles.Text = "Resume Consoles";

                }
            }
            //pause all the consoles
            PauseConsole(_spellConsole);
            PauseConsole(_meleeConsole);
            PauseConsole(_console);
            PauseConsole(_mqConsole);
            //print out the buffers to the text boxes
        }
        private void PauseConsole(TextBoxInfo ti)
        {
            lock (ti)
            {
                ti.isPaused = !ti.isPaused;
                if (_spellConsole.isPaused)
                {

                    ti.sb.Clear();
                    Int32 count = ti.consoleBuffer.Size;

                    for (Int32 i = count - 1; i >= 0; i--)
                    {
                        ti.sb.AppendLine(ti.consoleBuffer[i]);
                    }
                    ti.textBox.ScrollBars = RichTextBoxScrollBars.Both;
                    ti.textBox.Text = ti.sb.ToString();
                    ti.textBox.SelectionStart = ti.textBox.Text.Length;
                    ti.textBox.ScrollToCaret();
                    ti.nextProcess = _stopWatch.ElapsedMilliseconds + 100;
                    ti.isDirty = false;

                }
                else
                {
                    ti.textBox.ScrollBars = RichTextBoxScrollBars.None;
                    ti.isDirty = true;
                }
            }
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
    }
    public class TextBoxInfo
    {
        public System.Text.StringBuilder sb = new StringBuilder();
        public RichTextBox textBox;
        public bool isDirty;
        public bool isPaused = false;
        public Int64 nextProcess;
        public CircularBuffer<string> consoleBuffer = new CircularBuffer<string>(1000);
    }

}
