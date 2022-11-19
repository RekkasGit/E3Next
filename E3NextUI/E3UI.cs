using E3NextUI.Server;
using E3NextUI.Settings;
using E3NextUI.Themese;
using E3NextUI.Util;
using Octokit;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;
using Ionic.Zip;
using System.Reflection;

namespace E3NextUI
{



    public partial class E3UI : Form
    {
        public static string Version = "v1.0.2-beta";
        public static System.Diagnostics.Stopwatch _stopWatch = new System.Diagnostics.Stopwatch();
        public static volatile bool _shouldProcess = true;

        Task _consoleTask;
        Task _consoleMQTask;
        Task _consoleMeleeTask;
        Task _consoleSpellTask;
        Task _updateParse;
        Task _globalUpdate;
        private object _objLock = new object();
        public static DealerClient _tloClient;
        private PubClient _pubClient;
        private PubServer _pubServer;
        public static TextBoxInfo _console;
        public static TextBoxInfo _mqConsole;
        public static TextBoxInfo _meleeConsole;
        public static TextBoxInfo _spellConsole;
        public static string _playerName;
        public static Int32 _parentProcess;
        public static object _objectLock = new object();
        public static GeneralSettings _genSettings;
        public Image _collapseConsoleImage;
        public Image _uncollapseConsoleImage;
        public Image _collapseDynamicButtonImage;
        public Image _uncollapseDynamicButtonImage;
        public static String _playerHP;
        public static String _playerMP;
        public static String _playerSP;
       

        public E3UI()
        {
            InitializeComponent();
            _collapseConsoleImage = (Image)pbCollapseConsoleButtons.Image.Clone();
            pbCollapseConsoleButtons.Image.RotateFlip(RotateFlipType.Rotate180FlipNone);
            _uncollapseConsoleImage = (Image)pbCollapseConsoleButtons.Image.Clone();


            _collapseDynamicButtonImage = (Image)pbCollapseDynamicButtons.Image.Clone();
            _collapseDynamicButtonImage.RotateFlip(RotateFlipType.Rotate270FlipNone);
            _uncollapseDynamicButtonImage = (Image)pbCollapseDynamicButtons.Image.Clone();
            _uncollapseDynamicButtonImage.RotateFlip(RotateFlipType.Rotate90FlipNone);
            pbCollapseDynamicButtons.Image = (Image)_uncollapseDynamicButtonImage.Clone();

            SetCurrentProcessExplicitAppUserModelID("E3.E3UI.1");
            _stopWatch.Start();
            string[] args = Environment.GetCommandLineArgs();


            string configFolder = "";

            if (args.Length > 1)
            {
                Int32 port = Int32.Parse(args[2]);
                //get this first as its used in the regex for parsing for name.
                _tloClient = new DealerClient(port);
                if (_tloClient != null)
                {
                    lock (_tloClient)
                    {
                        _playerName = _tloClient.RequestData("${Me.CleanName}");
                        this.Text = $"E3UI ({_playerName})";
                        labelPlayerName.Text = _playerName;
                        configFolder = _tloClient.RequestData("${MacroQuest.Path[config]}");
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

            _genSettings = new GeneralSettings(configFolder, _playerName);
            _genSettings.LoadData();


            if(_genSettings.StartLocationX>0 || _genSettings.StartLocationY>0)
            {
                this.StartPosition = FormStartPosition.Manual;
                var point = new Point(_genSettings.StartLocationX, _genSettings.StartLocationY);
                var size = new Size(_genSettings.Width, _genSettings.Height);
                this.DesktopBounds = new Rectangle(point, size);
          
            }

            if (_genSettings.UseDarkMode)
            {
                darkModeMenuItem.Checked = true;
            }

            LoadDynamicButtons();
            SetDoubleBuffered(richTextBoxConsole);
            SetDoubleBuffered(richTextBoxMQConsole);
            SetDoubleBuffered(richTextBoxMelee);
            SetDoubleBuffered(richTextBoxSpells);

            _console = new TextBoxInfo() { textBox = richTextBoxConsole };
            _mqConsole = new TextBoxInfo() { textBox = richTextBoxMQConsole };
            _meleeConsole = new TextBoxInfo() { textBox = richTextBoxMelee };
            _spellConsole = new TextBoxInfo() { textBox = richTextBoxSpells };
    
            _consoleTask = Task.Factory.StartNew(() => { ProcessConsoleUI(_console); }, CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
            _consoleMQTask = Task.Factory.StartNew(() => { ProcessConsoleUI(_mqConsole); }, CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
            _consoleMeleeTask = Task.Factory.StartNew(() => { ProcessConsoleUI(_meleeConsole); }, CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
            _consoleSpellTask = Task.Factory.StartNew(() => { ProcessConsoleUI(_spellConsole); }, CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
            _updateParse = Task.Factory.StartNew(() => { ProcessParse(); }, CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);

            _globalUpdate = Task.Factory.StartNew(() => { GlobalTimer(); }, CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);

           
        }

        private delegate void GlobalDelegate();
        public void GlobalUIProcess()
        {
            int currentX = this.DesktopBounds.X;
            int currentY = this.DesktopBounds.Y;
            int height = this.DesktopBounds.Height;
            int width = this.DesktopBounds.Width;

            if (currentX != _genSettings.StartLocationX || currentY != _genSettings.StartLocationY || width != _genSettings.Width | height != _genSettings.Height)
            {
                _genSettings.StartLocationX = currentX;
                _genSettings.StartLocationY = currentY;
                _genSettings.Width = width;
                _genSettings.Height = height;
                _genSettings.SaveData();
            }
        }

        public void Shutdown()
        {
            _shouldProcess = false;
        }



        #region dynamicButtons
        private void LoadDynamicButtons()
        {
            Int32 row = 5;
            Int32 col = 5;
            for(Int32 i=0;i<(row*col);i++)
            {
                var b = new Button();
                b.Name = $"dynamicButton_{i + 1}";
                if(_genSettings.DynamicButtons.TryGetValue(b.Name, out var db))
                {
                    b.Text = db.Name;
                }else
                {   
                    b.Text = (i + 1).ToString();

                }
                b.Click += dynamicButtonClick;
                b.MouseDown += dynamicButtonRightClick;
                b.Dock = DockStyle.Fill;
                tableLayoutPanelDynamicButtons.Controls.Add(b);
            }


        }
        void dynamicButtonRightClick(object sender, MouseEventArgs e)
        {
            var b = sender as Button;
            if(b!=null)
            {
                if (e.Button == MouseButtons.Right)
                {
                    if(_genSettings.DynamicButtons.TryGetValue(b.Name,out var db))
                    {
                        //already exists.
                        var edit = new DynamicButtonEditor();
                        edit.StartPosition = FormStartPosition.CenterParent;
                        edit.textBoxName.Text = b.Text;
                        edit.textBoxCommands.Text = String.Join("\r\n",db.Commands);

                        if (edit.ShowDialog() == DialogResult.OK)
                        {
                            db.Name = edit.textBoxName.Text;
                            string[] lines = edit.textBoxCommands.Text.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);

                            db.Commands.Clear();
                            foreach (var line in lines)
                            {
                                if(!String.IsNullOrWhiteSpace(line))
                                {
                                    db.Commands.Add(line);
                                }
                            }
                         
                            _genSettings.SaveData();
                            b.Text = db.Name;

                        }
                    }
                }
            }
        }
        void dynamicButtonClick(object sender, EventArgs e)
        {
            var b = sender as Button;
            if (b != null)
            {
                if (_genSettings.DynamicButtons.TryGetValue(b.Name, out var db))
                {
                    foreach(var command in db.Commands)
                    {
                        Server.PubServer._pubCommands.Enqueue(command);
                    }
                }
                else
                {
                    //edit the button
                    var edit = new DynamicButtonEditor();
                    edit.StartPosition = FormStartPosition.CenterParent;
                    if (edit.ShowDialog() == DialogResult.OK)
                    {
                        DynamicButton tdb = new DynamicButton();
                        tdb.Name=edit.textBoxName.Text;
                        string[] lines = edit.textBoxCommands.Text.Split(new string[] { Environment.NewLine },StringSplitOptions.None);
                        tdb.Commands = new List<string>(lines);

                        if(!_genSettings.DynamicButtons.ContainsKey(b.Name))
                        {
                            _genSettings.DynamicButtons.Add(b.Name, tdb);
                        }
                        _genSettings.DynamicButtons[b.Name]= tdb;
                        _genSettings.SaveData();
                        b.Text = tdb.Name;
                        
                    }
                }
            }
        }
        private void GlobalTimer()
        {
            while(_shouldProcess)
            {
                if (_parentProcess > 0)
                {
                    if (!ProcessExists(_parentProcess))
                    {
                        System.Windows.Forms.Application.Exit();
                    }
                }
                if (this.IsHandleCreated)
                {
                    //check to see if our position has changed.
                    this.Invoke(new GlobalDelegate(GlobalUIProcess), null);
                }
                System.Threading.Thread.Sleep(1000);
            }
        }
        private void pbCollapseDynamicButtons_Click(object sender, EventArgs e)
        {
            ToggleButtons();
        }
        private void ToggleButtons(bool ignoreWidth = false)
        {
            int BorderWidth = (this.Width - this.ClientSize.Width) / 2;

            if (tableLayoutPanelDynamicButtons.Visible)
            {
                tableLayoutPanelDynamicButtons.Visible = false;
                if (!ignoreWidth)
                {
                    Int32 newWidth = BorderWidth + (panelStatusPannel2.Width) + 10;
                    var point = new Point(this.DesktopBounds.X, this.DesktopBounds.Y);
                    var size = new Size(newWidth, this.DesktopBounds.Height);
                    this.DesktopBounds = new Rectangle(point, size);

                }
                pbCollapseDynamicButtons.Image = (Image)_collapseDynamicButtonImage;
                _genSettings.DynamicButtonsCollapsed = true;
                _genSettings.SaveData();
            }
            else
            {

                Int32 newWidth = BorderWidth + (panelStatusPannel2.Width) + tableLayoutPanelDynamicButtons.Width + 10;
                var point = new Point(this.DesktopBounds.X, this.DesktopBounds.Y);
                var size = new Size(newWidth, this.DesktopBounds.Height);
                this.DesktopBounds = new Rectangle(point, size);
                tableLayoutPanelDynamicButtons.Visible = true;
                pbCollapseDynamicButtons.Image = (Image)_uncollapseDynamicButtonImage;
                _genSettings.DynamicButtonsCollapsed = false;
                _genSettings.SaveData();

            }
        }
        private void button1_Click(object sender, EventArgs e)
        {
            DynamicButtonEditor ft = new DynamicButtonEditor();
            ft.Show();
        }
        #endregion
      
        #region formevents
        private void darkModeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (_genSettings.UseDarkMode)
            {
                DefaultMode.ChangeTheme(this, this.Controls);
                _genSettings.UseDarkMode = false;
                darkModeMenuItem.Checked = false;
                this.Opacity = this.Opacity - 0.001;
                System.Windows.Forms.Application.DoEvents();
                this.Opacity = 100;
                _genSettings.SaveData();
            }
            else
            {
                DarkMode.ChangeTheme(this, this.Controls);
                _genSettings.UseDarkMode = true;
                darkModeMenuItem.Checked = true;
                this.Opacity = this.Opacity - 0.001;
                System.Windows.Forms.Application.DoEvents();
                this.Opacity = 100;
                _genSettings.SaveData();
            }
        }
        private void E3UI_Load(object sender, EventArgs e)
        {
            if (_genSettings.ConsoleCollapsed)
            {
                ToggleConsoles(true);
            }
            if(_genSettings.DynamicButtonsCollapsed)
            {
                ToggleButtons(true);
            }
            if(_genSettings.UseDarkMode)
            {
                Themese.DarkMode.ChangeTheme(this, this.Controls);
            }

        }
        private void E3UI_FormClosing(object sender, FormClosingEventArgs e)
        {
            //set the variable that will stop all the while loops
            _shouldProcess = false;
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
        #endregion

        #region parseAndUIUpdates
        private void ProcessParse()
        {
            while (_shouldProcess)
            {
             
                if(this.IsHandleCreated)
                {
                    this.Invoke(new ProcesssBaseParseDelegate(ProcesssBaseParse), null);

                }
                System.Threading.Thread.Sleep(500);
            }
        }
        private delegate void ProcesssBaseParseDelegate();
        private void ProcesssBaseParse()
        {
         
            //lets get the data from the line parser.
            

            if(labelHPTotal.Text!=_playerHP)
            {
                labelHPTotal.Text = _playerHP;
            }
            if (labelManaCurrent.Text != _playerMP)
            {
                labelManaCurrent.Text = _playerMP;
            }
            if (labelStaminaValue.Text != _playerSP)
            {
                labelStaminaValue.Text = _playerSP;
            }

            labelInCombatValue.Text = LineParser._currentlyCombat.ToString();
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

                Int64 healingByYou = LineParser._healingByYou.Sum();
                labelHealingByYouValue.Text = healingByYou.ToString("N0");

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
        private delegate void SetPlayerDataDelegate(string name);
        public void SetPlayerName(string name)
        {
            _playerName = name;
        }
        public void SetPlayerHP(string value)
        {
            _playerHP = value;
        }
        public void SetPlayerMP(string value)
        {
            _playerMP = value;

        }
        public void SetPlayerSP(string value)
        {
            _playerSP = value;
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
        #endregion

        #region Consoles
        private void pbCollapseConsoleButtons_Click(object sender, EventArgs e)
        {
            ToggleConsoles();
        }
        private void buttonPauseConsoles_Click(object sender, EventArgs e)
        {
            lock (_spellConsole)
            {
                if (_spellConsole.isPaused)
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
        private void buttonResetParse_Click(object sender, EventArgs e)
        {
            LineParser.Reset();
        }

        private void ToggleConsoles(bool ignoreHeight = false)
        {
            int BorderWidth = (this.Width - this.ClientSize.Width) / 2;
            int TitlebarHeight = this.Height - this.ClientSize.Height - 2 * BorderWidth;

            if (splitContainer2.Visible)
            {
                splitContainer2.Visible = false;
                splitContainer1.Visible = false;

                if (!ignoreHeight)
                {
                    //need to collapse the window height to deal with the size poofing



                    Int32 newHeight = TitlebarHeight + BorderWidth + panelStatusPannel2.Height + panelMain.Height + menuStrip1.Height + 20;
                    var point = new Point(this.DesktopBounds.X, this.DesktopBounds.Y);
                    var size = new Size(this.DesktopBounds.Width, newHeight);
                    this.DesktopBounds = new Rectangle(point, size);

                }

                pbCollapseConsoleButtons.Image = (Image)_collapseConsoleImage.Clone();

                _genSettings.ConsoleCollapsed = true;
                _genSettings.SaveData();

            }
            else
            {

                Int32 newHeight = TitlebarHeight + BorderWidth + panelStatusPannel2.Height + panelMain.Height + menuStrip1.Height + 20 + (splitContainer2.Height + splitContainer1.Height);
                var point = new Point(this.DesktopBounds.X, this.DesktopBounds.Y);
                var size = new Size(this.DesktopBounds.Width, newHeight);
                this.DesktopBounds = new Rectangle(point, size);
                splitContainer2.Visible = true;
                splitContainer1.Visible = true;
                _genSettings.ConsoleCollapsed = false;
                pbCollapseConsoleButtons.Image = (Image)_uncollapseConsoleImage.Clone();
                _genSettings.SaveData();


            }
        }

        private void ProcessConsoleUI(TextBoxInfo textInfo)
        {
            while (_shouldProcess)
            {
                if(this.IsHandleCreated)
                {
                    this.Invoke(new ProcessBaseUIDelegate(ProcessBaseConsoleUI), new object[] { textInfo });

                }
                System.Threading.Thread.Sleep(500);
            }

        }


        private delegate void ProcessBaseUIDelegate(TextBoxInfo textInfo);
        private void ProcessBaseConsoleUI(TextBoxInfo ti)
        {
                if(!ti.textBox.Visible) return;

                lock (ti)
                {
                    if (ti.isPaused || !ti.textBox.Visible) return;

                    if (ti.nextProcess < _stopWatch.ElapsedMilliseconds)
                    {
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
        public void AddConsoleLine(string value, TextBoxInfo ti)
        {
            lock (ti)
            {
                ti.isDirty = true;
                ti.consoleBuffer.PushFront(value);
            }
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

        #endregion

      

        /// <summary>
        /// used to check if our parent process dies, so that we can close as well.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        private bool ProcessExists(int id)
        {
            return Process.GetProcesses().Any(x => x.Id == id);
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
        [DllImport("shell32.dll", SetLastError = true)]
        static extern void SetCurrentProcessExplicitAppUserModelID([MarshalAs(UnmanagedType.LPWStr)] string AppID);

        private void checkUpdatesToolStripMenuItem_Click(object sender, EventArgs e)
        {

            string exePath = Assembly.GetExecutingAssembly().CodeBase.Replace("file:///", "").Replace("/", "\\").Replace(@"\E3NextUI.exe", "");

            exePath = exePath.Substring(0, exePath.LastIndexOf(@"\")+1);



            GitHubClient client = new GitHubClient(new ProductHeaderValue("E3NextUpdater"));
            var releases = client.Repository.Release.GetAll("RekkasGit", "E3Next");
            releases.Wait();
            var latest = releases.Result[0];

            if(latest.TagName!=Version)
            {
                var mb = new MessageBox();
                mb.StartPosition = FormStartPosition.CenterParent;
                mb.Text = "Upgrade E3";
                mb.lblMessage.Text = "Do you wish to upgrade to: " + latest.TagName + "?";
                
                if (mb.ShowDialog() == DialogResult.OK)
                {
                    //do something

                    var edit = new Update();
                    edit.StartPosition = FormStartPosition.CenterParent;
                    edit.textBoxInstallPath.Text = exePath;
                    edit.client = client;
                    edit.latestID = latest.Id;
                    edit.ShowDialog();

                }
               
            }
            else
            {
                var mb = new MessageBox();
                mb.StartPosition = FormStartPosition.CenterParent;
                mb.Text = "Upgrade E3";
                mb.lblMessage.Text = "You are on the latest version: " + Version;
                mb.buttonOkayOnly.Visible = true;
                mb.buttonOK.Visible = false;
                mb.buttonCancel.Visible = false;
                mb.ShowDialog();
                return;
            }
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
