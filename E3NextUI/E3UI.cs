using E3NextUI.Server;
using E3NextUI.Settings;
using E3NextUI.Themese;
using E3NextUI.Util;
using Octokit;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Reflection;
using System.IO;


namespace E3NextUI
{



    public partial class E3UI : Form
    {
		public const int WM_NCLBUTTONDOWN = 0xA1;
		public const int HT_CAPTION = 0x2;
		[DllImportAttribute("user32.dll")]
		public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
		[DllImportAttribute("user32.dll")]
		public static extern bool ReleaseCapture();
		public static string Version = "v1.46";
        public static System.Diagnostics.Stopwatch _stopWatch = new System.Diagnostics.Stopwatch();
        public static volatile bool ShouldProcess = true;

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
        public static TextBoxInfo Console;
        public static TextBoxInfo MQConsole;
        public static TextBoxInfo MeleeConsole;
        public static TextBoxInfo SpellConsole;
        public static string PlayerName;
        public static string ServerName = string.Empty;
	
	    public static Int32 _parentProcess;
        public static object _objectLock = new object();
        public static GeneralSettings _genSettings;
        public static bool _buttonMode = false;
        public static bool _textToSpeachMode = false;
        public Image _collapseConsoleImage;
        public Image _uncollapseConsoleImage;
        public Image _collapseDynamicButtonImage;
        public Image _uncollapseDynamicButtonImage;
        public static String _playerHP;
        public static String _playerMP;
        public static String _playerSP;
        private globalKeyboardHook _globalKeyboard;
        public static string _currentWindowName = "NULL";
        public static object _currentWindowLock = new object();
        private FormBorderStyle _startingStyle;
		//resizing stuff for when in buttonmode
		//https://stackoverflow.com/questions/2575216/how-to-move-and-resize-a-form-without-a-border
		private const int
	    HTLEFT = 10,
	    HTRIGHT = 11,
	    HTTOP = 12,
	    HTTOPLEFT = 13,
	    HTTOPRIGHT = 14,
	    HTBOTTOM = 15,
	    HTBOTTOMLEFT = 16,
	    HTBOTTOMRIGHT = 17;
		const int _ = 10; // you can rename this variable if you like
		Rectangle Top { get { return new Rectangle(0, 0, this.ClientSize.Width, _); } }
		Rectangle Left { get { return new Rectangle(0, 0, _, this.ClientSize.Height); } }
		Rectangle Bottom { get { return new Rectangle(0, this.ClientSize.Height - _, this.ClientSize.Width, _); } }
		Rectangle Right { get { return new Rectangle(this.ClientSize.Width - _, 0, _, this.ClientSize.Height); } }

		Rectangle TopLeft { get { return new Rectangle(0, 0, _, _); } }
		Rectangle TopRight { get { return new Rectangle(this.ClientSize.Width - _, 0, _, _); } }
		Rectangle BottomLeft { get { return new Rectangle(0, this.ClientSize.Height - _, _, _); } }
		Rectangle BottomRight { get { return new Rectangle(this.ClientSize.Width - _, this.ClientSize.Height - _, _, _); } }

		//end resizing stuff for buttonmode
		public E3UI()
        {
            InitializeComponent();
            _startingStyle = this.FormBorderStyle;
			SetStyle(ControlStyles.ResizeRedraw, true); // this is to avoid visual artifacts

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
                        PlayerName = _tloClient.RequestData("${Me.CleanName}");
                        ServerName = _tloClient.RequestData("${MacroQuest.Server}");
                        this.Text = $"E3UI ({PlayerName})({ServerName})({Version})";
                        labelPlayerName.Text = PlayerName;
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
				_globalKeyboard = new globalKeyboardHook((uint)_parentProcess);
				_globalKeyboard.KeyDown += new KeyEventHandler(globalKeyboard_KeyDown);
				_globalKeyboard.KeyUp += new KeyEventHandler(globalKeyboard_KeyUp);

			}

            _genSettings = new GeneralSettings(configFolder, PlayerName,ServerName);
            _genSettings.LoadData();


           // if(_genSettings.StartLocationX>0 || _genSettings.StartLocationY>0)
            {
                this.StartPosition = FormStartPosition.Manual;
                var point = new Point(_genSettings.StartLocationX, _genSettings.StartLocationY);
               this.Location = point;
                var size = new Size(_genSettings.Width, _genSettings.Height);
                this.DesktopBounds = new System.Drawing.Rectangle(point, size);
          
            }

            if (_genSettings.UseDarkMode)
            {
                darkModeMenuItem.Checked = true;
            }
            if (_genSettings.UseOverlay)
            {
                overlayOntopToolStripMenuItem.Checked = true;
                TopMost = true;
            }

            LoadDynamicButtons();
            SetDoubleBuffered(richTextBoxConsole);
            SetDoubleBuffered(richTextBoxMQConsole);
            SetDoubleBuffered(richTextBoxMelee);
            SetDoubleBuffered(richTextBoxSpells);

            Console = new TextBoxInfo() { textBox = richTextBoxConsole };
            MQConsole = new TextBoxInfo() { textBox = richTextBoxMQConsole };
            MeleeConsole = new TextBoxInfo() { textBox = richTextBoxMelee };
            SpellConsole = new TextBoxInfo() { textBox = richTextBoxSpells };
    
            _consoleTask = Task.Factory.StartNew(() => { ProcessConsoleUI(Console); }, CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
            _consoleMQTask = Task.Factory.StartNew(() => { ProcessConsoleUI(MQConsole); }, CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
            _consoleMeleeTask = Task.Factory.StartNew(() => { ProcessConsoleUI(MeleeConsole); }, CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
            _consoleSpellTask = Task.Factory.StartNew(() => { ProcessConsoleUI(SpellConsole); }, CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
            _updateParse = Task.Factory.StartNew(() => { ProcessParse(); }, CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);

            _globalUpdate = Task.Factory.StartNew(() => { GlobalTimer(); }, CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);

           
        }
		protected override void WndProc(ref Message message)
		{
			base.WndProc(ref message);

			if (message.Msg == 0x84) // WM_NCHITTEST
			{
				var cursor = this.PointToClient(Cursor.Position);

				if (TopLeft.Contains(cursor)) message.Result = (IntPtr)HTTOPLEFT;
				else if (TopRight.Contains(cursor)) message.Result = (IntPtr)HTTOPRIGHT;
				else if (BottomLeft.Contains(cursor)) message.Result = (IntPtr)HTBOTTOMLEFT;
				else if (BottomRight.Contains(cursor)) message.Result = (IntPtr)HTBOTTOMRIGHT;

				else if (Top.Contains(cursor)) message.Result = (IntPtr)HTTOP;
				else if (Left.Contains(cursor)) message.Result = (IntPtr)HTLEFT;
				else if (Right.Contains(cursor)) message.Result = (IntPtr)HTRIGHT;
				else if (Bottom.Contains(cursor)) message.Result = (IntPtr)HTBOTTOM;
			}
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
            ShouldProcess = false;
        }



        #region dynamicButtons
        private void LoadDynamicButtons()
        {
            Int32 row = 5;
            Int32 col = 5;
            for(Int32 i=0;i<(row*col);i++)
            {
                var b = new System.Windows.Forms.Button();
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

			dyanmicButtonsLoadKeyBoardShortcuts();
		}
        void dyanmicButtonsLoadKeyBoardShortcuts()
        {
			_globalKeyboard.HookedKeys.Clear();
			//register the keys
			foreach (var pair in _genSettings.DynamicButtons)
			{

				if (pair.Value.Hotkey == String.Empty) continue;
				Keys key;
				Enum.TryParse(pair.Value.Hotkey, out key);

				if (!_globalKeyboard.HookedKeys.Contains(key))
				{
					_globalKeyboard.HookedKeys.Add(key);
				}
			}
		}
        void dynamicButtonRightClick(object sender, MouseEventArgs e)
        {
            var b = sender as System.Windows.Forms.Button;
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
						edit.checkBoxHotkeyAlt.Checked = db.HotKeyAlt;
                        edit.checkBoxHotkeyShift.Checked = db.HotKeyShift;
						edit.checkBoxHotkeyCtrl.Checked = db.HotKeyCtrl;
                        edit.checkBoxHotkeyEat.Checked = db.HotKeyEat;

                        if(!String.IsNullOrWhiteSpace(db.Hotkey))
                        {
							edit.comboBoxKeyValues.SelectedItem = db.Hotkey;
						}
						if (edit.ShowDialog() == DialogResult.OK)
                        {
							if (!String.IsNullOrWhiteSpace(edit.textBoxName.Text))
							{
								UpdateDyanmicButton(edit, b);
								_genSettings.SaveData();

							}
							else
							{
								_genSettings.DynamicButtons.Remove(b.Name);
								b.Text = b.Name.Replace("dynamicButton_", "");

							}
							dyanmicButtonsLoadKeyBoardShortcuts();
						}
					}
                }
            }
           
        }
		private void globalKeyboard_KeyUp(object sender, KeyEventArgs e)
		{


			Debug.WriteLine("Debug");

		}

		private void globalKeyboard_KeyDown(object sender, KeyEventArgs e)
		{
            lock(_currentWindowLock)
            {
				//one of the keys we are looking for!
				if (_currentWindowName.Equals("IMGUI", StringComparison.OrdinalIgnoreCase))
				{
					//they are typing in game, do not capture events.
					return;
				}
				if (_currentWindowName.EndsWith("SearchTextEdit", StringComparison.OrdinalIgnoreCase))
				{
					//they are typing in game, do not capture events.
					return;
				}
				if (_currentWindowName.EndsWith("Input", StringComparison.OrdinalIgnoreCase))
				{
					//they are typing in game, do not capture events.
					return;
				}
				if (_currentWindowName.Equals("CW_ChatInput", StringComparison.OrdinalIgnoreCase))
				{
					//they are typing in game, do not capture events.
					return;
				}
				if (_currentWindowName.Equals("QTYW_SliderInput", StringComparison.OrdinalIgnoreCase))
				{
					//they are typing in game, do not capture events.
					return;
				}
			}
			
			foreach (var pair in _genSettings.DynamicButtons)
			{

				if (pair.Value.Hotkey == String.Empty) continue;
				Keys key;
				Enum.TryParse(pair.Value.Hotkey.ToString(), out key);

				if (key==e.KeyCode)
				{ 
                    if(e.Modifiers==Keys.Alt && !pair.Value.HotKeyAlt)
                    {
                        continue;
                    }
					if (e.Modifiers == Keys.Control && !pair.Value.HotKeyCtrl)
					{
						continue;
					}

					if (e.Modifiers == Keys.Shift && !pair.Value.HotKeyShift)
					{
						continue;
					}
					if (e.Modifiers == Keys.Shift && !pair.Value.HotKeyShift)
					{
						continue;
					}

					if (pair.Value.HotKeyAlt && e.Modifiers!= Keys.Alt)
                    {
                        continue;
                    }
					if (pair.Value.HotKeyCtrl && e.Modifiers != Keys.Control)
					{
						continue;
					}
                    if(pair.Value.HotKeyEat)
                    {
						e.Handled = true;
					}
					foreach (var command in pair.Value.Commands)
					{
						Server.PubServer.PubCommands.Enqueue(command);
					}
				}
			}

		}
		void dynamicButtonClick(object sender, EventArgs e)
        {
            var b = sender as System.Windows.Forms.Button;
            if (b != null)
            {
                if (_genSettings.DynamicButtons.TryGetValue(b.Name, out var db))
                {
                    foreach(var command in db.Commands)
                    {
                        Server.PubServer.PubCommands.Enqueue(command);
                    }
                    SetForground(_parentProcess);
				}
                else
                {
                    //edit the button
                    var edit = new DynamicButtonEditor();
                    edit.StartPosition = FormStartPosition.CenterParent;
                    if (edit.ShowDialog() == DialogResult.OK)
                    {
                        if(!String.IsNullOrWhiteSpace(edit.textBoxName.Text))
						{
                            UpdateDyanmicButton(edit,b);
							_genSettings.SaveData();
							

						}
                        else
                        {
							_genSettings.DynamicButtons.Remove(b.Name);
                            b.Text = b.Name.Replace("dynamicButton_", "");

						}
						dyanmicButtonsLoadKeyBoardShortcuts();
					}
                }
            }
        }

        private void UpdateDyanmicButton(DynamicButtonEditor edit, System.Windows.Forms.Button b)
        {
			DynamicButton tdb = new DynamicButton();
			tdb.Name = edit.textBoxName.Text;
			string[] lines = edit.textBoxCommands.Text.Split(new string[] { Environment.NewLine }, StringSplitOptions.None);
			tdb.Commands = new List<string>(lines);

			if (!_genSettings.DynamicButtons.ContainsKey(b.Name))
			{
				_genSettings.DynamicButtons.Add(b.Name, tdb);
			}
			_genSettings.DynamicButtons[b.Name] = tdb;

			tdb.HotKeyAlt = edit.checkBoxHotkeyAlt.Checked;
            tdb.HotKeyShift = edit.checkBoxHotkeyShift.Checked;
			tdb.HotKeyCtrl = edit.checkBoxHotkeyCtrl.Checked;
			tdb.HotKeyEat = edit.checkBoxHotkeyEat.Checked;
			string text = (string)edit.comboBoxKeyValues.SelectedItem;
			if (text != "None")
			{
				tdb.Hotkey = text;
			}
			else
			{
				tdb.Hotkey = String.Empty;
			}
			b.Text = tdb.Name;
		}
        private void GlobalTimer()
        {
            while(ShouldProcess)
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
                    this.DesktopBounds = new System.Drawing.Rectangle(point, size);

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
                this.DesktopBounds = new System.Drawing.Rectangle(point, size);
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
            ShouldProcess = false;
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
                    //Brings the form to top
                    TopMost = true;
                    //Set form's topmost status back to whatever it was
                    TopMost =_genSettings.UseOverlay;
                }
            }
        }
        #endregion

        #region parseAndUIUpdates
        private void ProcessParse()
        {
            while (ShouldProcess)
            {
             
                try
                {
					if (this.IsHandleCreated)
					{
						this.Invoke(new ProcesssBaseParseDelegate(ProcesssBaseParse), null);

					}

				}
				catch (Exception ex) 
                {
                    ; Debug.WriteLine(ex.Message);                
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

            labelInCombatValue.Text = LineParser.CurrentlyCombat.ToString();
            lock (LineParser._objectLock)
            {
                if (!String.IsNullOrWhiteSpace(LineParser.PetName))
                {
                    labelPetNameValue.Text = LineParser.PetName;
                }
				if (!String.IsNullOrWhiteSpace(LineParser.MercName))
				{
					labelMercNameValue.Text = LineParser.MercName;
				}
				Int64 yourDamageTotal = LineParser.YourDamage.Sum();
                labelYourDamageValue.Text = yourDamageTotal.ToString("N0");

                Int64 petDamageTotal = LineParser.YourPetDamage.Sum();
                labelPetDamageValue.Text = petDamageTotal.ToString("N0");

				Int64 mercDamageTotal = LineParser.YourMercDamage.Sum();
				labelMercDamageValue.Text = mercDamageTotal.ToString("N0");

				Int64 dsDamage = LineParser.YourDamageShieldDamage.Sum();
                labelYourDamageShieldValue.Text = dsDamage.ToString("N0");

                Int64 totalDamage = yourDamageTotal + petDamageTotal + dsDamage + mercDamageTotal;
                labelTotalDamageValue.Text = totalDamage.ToString("N0");

                Int64 damageToyou = LineParser.DamageToYou.Sum();
                labelDamageToYouValue.Text = damageToyou.ToString("N0");

                Int64 healingToyou = LineParser.HealingToYou.Sum();
                labelHealingYouValue.Text = healingToyou.ToString("N0");

                Int64 healingByYou = LineParser.HealingByYou.Sum();
                labelHealingByYouValue.Text = healingByYou.ToString("N0");

                //need to find the start of each colleciton
                //and end of each collection taking the lowest of start
                //and highest of end
                Int64 startTime = 0;
                Int64 endTime = 0;
                if (LineParser.YourDamage.Count > 0)
                {
                    if (startTime > LineParser.YourDamageTime[0] || startTime == 0)
                    {
                        startTime = LineParser.YourDamageTime[0];
                    }
                    if (endTime < LineParser.YourDamageTime[LineParser.YourDamageTime.Count - 1])
                    {
                        endTime = LineParser.YourDamageTime[LineParser.YourDamageTime.Count - 1];
                    }
                }
                if (LineParser.YourPetDamage.Count > 0)
                {
                    if (startTime > LineParser.YourPetDamage[0] || startTime == 0)
                    {
                        startTime = LineParser.YourPetDamageTime[0];
                    }
                    if (endTime < LineParser.YourPetDamageTime[LineParser.YourPetDamageTime.Count - 1])
                    {
                        endTime = LineParser.YourPetDamageTime[LineParser.YourPetDamageTime.Count - 1];
                    }
                }
				if (LineParser.YourMercDamage.Count > 0)
				{
					if (startTime > LineParser.YourMercDamage[0] || startTime == 0)
					{
						startTime = LineParser.YourMercDamageTime[0];
					}
					if (endTime < LineParser.YourMercDamageTime[LineParser.YourMercDamageTime.Count - 1])
					{
						endTime = LineParser.YourMercDamageTime[LineParser.YourMercDamageTime.Count - 1];
					}
				}
				if (LineParser.YourDamageShieldDamage.Count > 0)
                {
                    if (startTime > LineParser.YourDamageShieldDamageTime[0] || startTime == 0)
                    {
                        startTime = LineParser.YourDamageShieldDamageTime[0];
                    }
                    if (endTime < LineParser.YourDamageShieldDamageTime[LineParser.YourDamageShieldDamageTime.Count - 1])
                    {
                        endTime = LineParser.YourDamageShieldDamageTime[LineParser.YourDamageShieldDamageTime.Count - 1];
                    }
                }
                Int64 totalTime = (endTime - startTime) / 1000;

                labelTotalTimeValue.Text = (totalTime) + " seconds";

                if (totalTime == 0) totalTime = 1;
                Int64 totalDPS = totalDamage / totalTime;
                Int64 yourDPS = yourDamageTotal / totalTime;
                Int64 petDPS = petDamageTotal / totalTime;
                Int64 mercDPS = mercDamageTotal / totalTime;
                Int64 dsDPS = dsDamage / totalTime;

                labelTotalDamageDPSValue.Text = totalDPS.ToString("N0") + " dps";
                labelYourDamageDPSValue.Text = yourDPS.ToString("N0") + " dps";
                labelPetDamageDPSValue.Text = petDPS.ToString("N0") + " dps";
                labelDamageShieldDPSValue.Text = dsDPS.ToString("N0") + " dps";
                labelMercDamageDPSValue.Text = mercDPS.ToString("N0") + " dps";

			}
            
        }
        private delegate void SetPlayerDataDelegate(string name);
        public void SetPlayerName(string name)
        {
            PlayerName = name;
        }
        public void SetPlayerHP(string value)
        {
            if(Int32.TryParse(value,out var result))
            {
                _playerHP = result.ToString("N0");
            }
            else
            {
				_playerHP = value;
			}
            
        }
        public void SetPlayerMP(string value)
        {
			if (Int32.TryParse(value, out var result))
			{
				_playerMP = result.ToString("N0");
			}
			else
			{
				_playerMP = value;
			}
		

        }
        public void SetPlayerSP(string value)
        {
			if (Int32.TryParse(value, out var result))
			{
				_playerSP = result.ToString("N0");
			}
			else
			{
				_playerSP = value;
			}
		
        }
        public void SetPlayerCasting(string value)
        {
            if (value == labelCastingValue.Text) return;
            if (this.InvokeRequired)
            {
                this.Invoke(new SetPlayerDataDelegate(SetPlayerCasting), new object[] { value });
            }
            else
            {
                labelCastingValue.Text = value;
            }
        }
		public void SetCurrentWindow(string value)
		{
			if (value == _currentWindowName) return;
            lock(_currentWindowLock)
            {
				_currentWindowName = value;
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
            lock (SpellConsole)
            {
                if (SpellConsole.isPaused)
                {
                    buttonPauseConsoles.Text = "Pause Consoles";
                }
                else
                {
                    buttonPauseConsoles.Text = "Resume Consoles";

                }
            }
            //pause all the consoles
            PauseConsole(SpellConsole);
            PauseConsole(MeleeConsole);
            PauseConsole(Console);
            PauseConsole(MQConsole);
            //print out the buffers to the text boxes
        }
        private void PauseConsole(TextBoxInfo ti)
        {
            lock (ti)
            {
                ti.isPaused = !ti.isPaused;
                if (SpellConsole.isPaused)
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
                    this.DesktopBounds = new System.Drawing.Rectangle(point, size);

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
                this.DesktopBounds = new System.Drawing.Rectangle(point, size);
                splitContainer2.Visible = true;
                splitContainer1.Visible = true;
                _genSettings.ConsoleCollapsed = false;
                pbCollapseConsoleButtons.Image = (Image)_uncollapseConsoleImage.Clone();
                _genSettings.SaveData();


            }
        }

        private void ProcessConsoleUI(TextBoxInfo textInfo)
        {
            while (ShouldProcess)
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
                string value = ((System.Windows.Forms.TextBox)sender).Text;
                if (value.StartsWith("/"))
                {
                    PubServer.PubCommands.Enqueue(value);

                }
                ((System.Windows.Forms.TextBox)sender).Text = String.Empty;
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
		private void SetForground(int id)
		{
			Process p  = Process.GetProcessById(id);
			if (p!=null)
            {
				SetForegroundWindow(p.MainWindowHandle);
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
        [DllImport("shell32.dll", SetLastError = true)]
        static extern void SetCurrentProcessExplicitAppUserModelID([MarshalAs(UnmanagedType.LPWStr)] string AppID);
		[DllImport("user32.dll", SetLastError = true)]
		private static extern bool SetForegroundWindow(IntPtr hwnd);

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

        private void hideToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ToggleShow();
        }

        private void overlayOntopToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (_genSettings.UseOverlay)
            {
                overlayOntopToolStripMenuItem.Checked = false;
                _genSettings.UseOverlay = false;
                TopMost = false;
                this.Opacity = this.Opacity - 0.001;
                System.Windows.Forms.Application.DoEvents();
                this.Opacity = 100;
                _genSettings.SaveData();
            }
            else
            {
                overlayOntopToolStripMenuItem.Checked = true;
                _genSettings.UseOverlay = true;
                TopMost = true;
                this.Opacity = this.Opacity - 0.001;
                System.Windows.Forms.Application.DoEvents();
                this.Opacity = 100;
                _genSettings.SaveData();
            }
        }

        private void donateToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var mb = new MessageBox();
            mb.StartPosition = FormStartPosition.CenterParent;
            mb.Text = "Donate for Github and Pizza (Paypal)";
            mb.lblMessage.Text = "If you wish to donate, please use friends and family. Otherwise it will be returned.";

            if (mb.ShowDialog() == DialogResult.OK)
            {
                System.Diagnostics.Process.Start("https://www.paypal.com/paypalme/RekkaSoftware");

            }
        }

        string _prevString = String.Empty;
		private void buttonModeToolStripMenuItem_Click(object sender, EventArgs e)
		{
            if(_buttonMode)
            {
                this.Text = _prevString;
                _buttonMode = false;
                this.FormBorderStyle = _startingStyle;
				panelMain.Show();
				panelStatusPannel2.Show();
				panelButtons.Location = new Point(736, 24);

			}
			else
            {
                _prevString = this.Text;
				_buttonMode = true;
                this.ControlBox = false;
                this.Text = String.Empty;
                this.FormBorderStyle= FormBorderStyle.None;
				panelMain.Hide();
				panelStatusPannel2.Hide();
				panelButtons.Location = new Point(0, 24);

			}
			buttonModeToolStripMenuItem.Checked = _buttonMode;
		}

		private void textToSpeachToolStripMenuItem_Click(object sender, EventArgs e)
		{

		}

		private void settingsToolStripMenuItem1_Click(object sender, EventArgs e)
		{

            using (TTSConfig config = new TTSConfig())
            {
				if (config._voices.Count == 0)
				{
					//no voices to configure, warn and kickout
					var mb = new MessageBox();
					mb.StartPosition = FormStartPosition.CenterParent;
					mb.Text = "No voices Found";
					mb.lblMessage.Text = "No voices found on the system, sorry :(";
					mb.ShowDialog();
					return;
				}

				config.StartPosition = FormStartPosition.CenterParent;

				config.checkBox_channel_auction.Checked = _genSettings.TTS_ChannelAuctionEnabled;
				config.checkBox_channel_gsay.Checked = _genSettings.TTS_ChannelGroupEnabled;
				config.checkBox_channel_guild.Checked = _genSettings.TTS_ChannelGuildEnabled;
				config.checkBox_channel_ooc.Checked = _genSettings.TTS_ChannelOOCEnabled;
				config.checkBox_channel_raid.Checked = _genSettings.TTS_ChannelRaidEnabled;
				config.checkBox_channel_say.Checked = _genSettings.TTS_ChannelSayEnabled;
				config.checkBox_channel_tell.Checked = _genSettings.TTS_ChannelTellEnabled;
				config.checkBox_channel_shout.Checked = _genSettings.TTS_ChannelShoutEnabled;
				config.checkBox_channel_mobspells.Checked = _genSettings.TTS_ChannelMobSpellsEnabled;
				config.checkBox_channel_pcspells.Checked = _genSettings.TTS_ChannelPCSpellsEnabled;

				config.checkBox_tts_enabled.Checked = _genSettings.TTS_Enabled;
				config.checkBox_tts_breifmode.Checked = _genSettings.TTS_BriefMode;
				config.textBox_tts_regex.Text = _genSettings.TTS_RegEx;
				config.textBox_tts_regex_exclude.Text = _genSettings.TTS_RegExExclude;
				config.numericUpDown_tts_wordlimit.Value = _genSettings.TTS_CharacterLimit;


				if (!String.IsNullOrWhiteSpace(_genSettings.TTS_Voice))
				{
					config.comboBox_tts_voices.SelectedItem = _genSettings.TTS_Voice;
				}

				config.trackBar_tts_speed.Value = _genSettings.TTS_Speed;
				config.trackBar_tts_volume.Value = _genSettings.TTS_Volume;


				if (config.ShowDialog() == DialogResult.OK)
				{
					_genSettings.TTS_ChannelAuctionEnabled = config.checkBox_channel_auction.Checked;
					_genSettings.TTS_ChannelGroupEnabled = config.checkBox_channel_gsay.Checked;
					_genSettings.TTS_ChannelGuildEnabled = config.checkBox_channel_guild.Checked;
					_genSettings.TTS_ChannelOOCEnabled = config.checkBox_channel_ooc.Checked;
					_genSettings.TTS_ChannelRaidEnabled = config.checkBox_channel_raid.Checked;
					_genSettings.TTS_ChannelSayEnabled = config.checkBox_channel_say.Checked;
					_genSettings.TTS_ChannelTellEnabled = config.checkBox_channel_tell.Checked;
					_genSettings.TTS_ChannelShoutEnabled = config.checkBox_channel_shout.Checked;
					_genSettings.TTS_ChannelMobSpellsEnabled = config.checkBox_channel_mobspells.Checked;
					_genSettings.TTS_ChannelPCSpellsEnabled = config.checkBox_channel_pcspells.Checked;
					_genSettings.TTS_Enabled = config.checkBox_tts_enabled.Checked;
					_genSettings.TTS_BriefMode = config.checkBox_tts_breifmode.Checked;
					_genSettings.TTS_RegEx = config.textBox_tts_regex.Text;
					_genSettings.TTS_RegExExclude = config.textBox_tts_regex_exclude.Text;
					_genSettings.TTS_Voice = (String)config.comboBox_tts_voices.SelectedItem;
					_genSettings.TTS_CharacterLimit = (Int32)config.numericUpDown_tts_wordlimit.Value;
					_genSettings.TTS_Speed = config.trackBar_tts_speed.Value;
					_genSettings.TTS_Volume = config.trackBar_tts_volume.Value;
					_genSettings.SaveData();
				}
			}
		}

		private void unlockEvaVoiceToolStripMenuItem_Click(object sender, EventArgs e)
		{
			var mb = new MessageBox();
			using (TTSConfig config = new TTSConfig())
            {
                if (config._voices.Contains("Microsoft Eva Mobile"))
                {

                    mb.StartPosition = FormStartPosition.CenterParent;
                    mb.Text = "Already done!";
                    mb.lblMessage.Text = "This is already unlocked for you! :)";
                    mb.buttonOkayOnly.Visible = true;
                    mb.buttonOK.Visible = false;
                    mb.buttonCancel.Visible = false;
                    mb.ShowDialog();
                    return;
                }
            }

            string TTSFolder = @"C:\\Windows\\Speech_OneCore\\Engines\\TTS\\en-US";
			if (!Directory.Exists(TTSFolder))
            {
				//TTS not setup/installed on windows?
				mb.StartPosition = FormStartPosition.CenterParent;
				mb.Text = "Sorry you don't have eva avilable :(";
				mb.lblMessage.Text = $"Sorry cannot find the en-US TTS folder at:{TTSFolder}";
				mb.buttonOkayOnly.Visible = true;
				mb.buttonOK.Visible = false;
				mb.buttonCancel.Visible = false;
				mb.ShowDialog();
				return;

			}

            //directory exists, lets check for the eva files.
            string searchPattern = "M1033Eva*";
			string[] fileNames = System.IO.Directory.GetFiles(TTSFolder, searchPattern);


            if(fileNames.Length==0)
            {
				//TTS not setup/installed on windows?
				mb.StartPosition = FormStartPosition.CenterParent;
				mb.Text = "Sorry you don't have eva avilable :(";
				mb.lblMessage.Text = $"Sorry cannot find the eva engine files (M1033Eva*) at:{TTSFolder}";
				mb.buttonOkayOnly.Visible = true;
				mb.buttonOK.Visible = false;
				mb.buttonCancel.Visible = false;
				mb.ShowDialog();
				return;
			}

			mb.StartPosition = FormStartPosition.CenterParent;
			mb.Text = "Please select a folder";
			mb.lblMessage.Text = "You will be asked to select a folder. \r\nWe will save a registery file you will need to double click on. Default is in the E3N settings area.";
			mb.buttonOkayOnly.Visible = true;
			mb.buttonOK.Visible = false;
			mb.buttonCancel.Visible = false;
			mb.ShowDialog();

			mb.StartPosition = FormStartPosition.CenterParent;
			mb.Text = "Please select a folder";
			mb.lblMessage.Text = "Note After the reg update it will require a restart to show up.";
			mb.buttonOkayOnly.Visible = true;
			mb.buttonOK.Visible = false;
			mb.buttonCancel.Visible = false;
			mb.ShowDialog();

			SaveFileDialog sd = new SaveFileDialog();
            sd.Filter = "Registery|*.reg";
            sd.Title = "Save Registery File";
            sd.FileName = "eva_unlock.reg";
            sd.InitialDirectory = _genSettings.GetFolderPath();

            if (sd.ShowDialog() == DialogResult.OK)
            {
                if (!String.IsNullOrEmpty(sd.FileName))
                {
                    
                    //sd filename now has the full path
                    System.IO.File.WriteAllText(sd.FileName, _evaUnlockString);
                    Int32 indexOfLastSlash = sd.FileName.LastIndexOf("\\");
                    string directory = sd.FileName.Substring(0, indexOfLastSlash);
                    ProcessStartInfo startInfo = new ProcessStartInfo()
					{
						Arguments = directory,
						FileName = "explorer.exe"
				    };

				    Process.Start(startInfo);

			    }
            }

		}
		#region unlockString
		string _evaUnlockString = @"Windows Registry Editor Version 5.00

[HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Speech\Voices\Tokens\MSTTS_V110_enUS_EvaM]
@=""Microsoft Eva Mobile - English (United States)""
""409""=""Microsoft Eva Mobile - English (United States)""
""CLSID""=""{179F3D56-1B0B-42B2-A962-59B7EF59FE1B}""
""LangDataPath""=hex(2):25,00,77,00,69,00,6e,00,64,00,69,00,72,00,25,00,5c,00,53,\
  00,70,00,65,00,65,00,63,00,68,00,5f,00,4f,00,6e,00,65,00,43,00,6f,00,72,00,\
  65,00,5c,00,45,00,6e,00,67,00,69,00,6e,00,65,00,73,00,5c,00,54,00,54,00,53,\
  00,5c,00,65,00,6e,00,2d,00,55,00,53,00,5c,00,4d,00,53,00,54,00,54,00,53,00,\
  4c,00,6f,00,63,00,65,00,6e,00,55,00,53,00,2e,00,64,00,61,00,74,00,00,00
""VoicePath""=hex(2):25,00,77,00,69,00,6e,00,64,00,69,00,72,00,25,00,5c,00,53,00,\
  70,00,65,00,65,00,63,00,68,00,5f,00,4f,00,6e,00,65,00,43,00,6f,00,72,00,65,\
  00,5c,00,45,00,6e,00,67,00,69,00,6e,00,65,00,73,00,5c,00,54,00,54,00,53,00,\
  5c,00,65,00,6e,00,2d,00,55,00,53,00,5c,00,4d,00,31,00,30,00,33,00,33,00,45,\
  00,76,00,61,00,00,00

[HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Speech\Voices\Tokens\MSTTS_V110_enUS_EvaM\Attributes]
""Age""=""Adult""
""Gender""=""Female""
""Version""=""11.0""
""Language""=""409""
""Name""=""Microsoft Eva Mobile""
""SharedPronunciation""=""""
""Vendor""=""Microsoft""
""DataVersion""=""11.0.2013.1022""

[HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Speech_OneCore\Voices\Tokens\MSTTS_V110_enUS_EvaM]
@=""Microsoft Eva Mobile - English (United States)""
""409""=""Microsoft Eva Mobile - English (United States)""
""CLSID""=""{179F3D56-1B0B-42B2-A962-59B7EF59FE1B}""
""LangDataPath""=hex(2):25,00,77,00,69,00,6e,00,64,00,69,00,72,00,25,00,5c,00,53,\
  00,70,00,65,00,65,00,63,00,68,00,5f,00,4f,00,6e,00,65,00,43,00,6f,00,72,00,\
  65,00,5c,00,45,00,6e,00,67,00,69,00,6e,00,65,00,73,00,5c,00,54,00,54,00,53,\
  00,5c,00,65,00,6e,00,2d,00,55,00,53,00,5c,00,4d,00,53,00,54,00,54,00,53,00,\
  4c,00,6f,00,63,00,65,00,6e,00,55,00,53,00,2e,00,64,00,61,00,74,00,00,00
""VoicePath""=hex(2):25,00,77,00,69,00,6e,00,64,00,69,00,72,00,25,00,5c,00,53,00,\
  70,00,65,00,65,00,63,00,68,00,5f,00,4f,00,6e,00,65,00,43,00,6f,00,72,00,65,\
  00,5c,00,45,00,6e,00,67,00,69,00,6e,00,65,00,73,00,5c,00,54,00,54,00,53,00,\
  5c,00,65,00,6e,00,2d,00,55,00,53,00,5c,00,4d,00,31,00,30,00,33,00,33,00,45,\
  00,76,00,61,00,00,00

[HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Speech_OneCore\Voices\Tokens\MSTTS_V110_enUS_EvaM\Attributes]
""Age""=""Adult""
""Gender""=""Female""
""Version""=""11.0""
""Language""=""409""
""Name""=""Microsoft Eva Mobile""
""SharedPronunciation""=""""
""Vendor""=""Microsoft""

[HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Microsoft\SPEECH\Voices\Tokens\MSTTS_V110_enUS_EvaM]
@=""Microsoft Eva Mobile - English (United States)""
""409""=""Microsoft Eva Mobile - English (United States)""
""CLSID""=""{179F3D56-1B0B-42B2-A962-59B7EF59FE1B}""
""LangDataPath""=hex(2):25,00,77,00,69,00,6e,00,64,00,69,00,72,00,25,00,5c,00,53,\
  00,70,00,65,00,65,00,63,00,68,00,5f,00,4f,00,6e,00,65,00,43,00,6f,00,72,00,\
  65,00,5c,00,45,00,6e,00,67,00,69,00,6e,00,65,00,73,00,5c,00,54,00,54,00,53,\
  00,5c,00,65,00,6e,00,2d,00,55,00,53,00,5c,00,4d,00,53,00,54,00,54,00,53,00,\
  4c,00,6f,00,63,00,65,00,6e,00,55,00,53,00,2e,00,64,00,61,00,74,00,00,00
""VoicePath""=hex(2):25,00,77,00,69,00,6e,00,64,00,69,00,72,00,25,00,5c,00,53,00,\
  70,00,65,00,65,00,63,00,68,00,5f,00,4f,00,6e,00,65,00,43,00,6f,00,72,00,65,\
  00,5c,00,45,00,6e,00,67,00,69,00,6e,00,65,00,73,00,5c,00,54,00,54,00,53,00,\
  5c,00,65,00,6e,00,2d,00,55,00,53,00,5c,00,4d,00,31,00,30,00,33,00,33,00,45,\
  00,76,00,61,00,00,00

[HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Microsoft\SPEECH\Voices\Tokens\MSTTS_V110_enUS_EvaM\Attributes]
""Age""=""Adult""
""Gender""=""Female""
""Version""=""11.0""
""Language""=""409""
""Name""=""Microsoft Eva Mobile""
""SharedPronunciation""=""""
""Vendor""=""Microsoft""
""DataVersion""=""11.0.2013.1022""

[HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Microsoft\SPEECH\Voices\Tokens\MSTTS_V110_enUS_EvaM]
@=""Microsoft Eva Mobile - English (United States)""
""409""=""Microsoft Eva Mobile - English (United States)""
""CLSID""=""{179F3D56-1B0B-42B2-A962-59B7EF59FE1B}""
""LangDataPath""=hex(2):25,00,77,00,69,00,6e,00,64,00,69,00,72,00,25,00,5c,00,53,\
  00,70,00,65,00,65,00,63,00,68,00,5f,00,4f,00,6e,00,65,00,43,00,6f,00,72,00,\
  65,00,5c,00,45,00,6e,00,67,00,69,00,6e,00,65,00,73,00,5c,00,54,00,54,00,53,\
 00,5c,00,65,00,6e,00,2d,00,55,00,53,00,5c,00,4d,00,53,00,54,00,54,00,53,00,\
  4c,00,6f,00,63,00,65,00,6e,00,55,00,53,00,2e,00,64,00,61,00,74,00,00,00
""VoicePath""=hex(2):25,00,77,00,69,00,6e,00,64,00,69,00,72,00,25,00,5c,00,53,00,\
  70,00,65,00,65,00,63,00,68,00,5f,00,4f,00,6e,00,65,00,43,00,6f,00,72,00,65,\
  00,5c,00,45,00,6e,00,67,00,69,00,6e,00,65,00,73,00,5c,00,54,00,54,00,53,00,\
  5c,00,65,00,6e,00,2d,00,55,00,53,00,5c,00,4d,00,31,00,30,00,33,00,33,00,45,\
  00,76,00,61,00,00,00

[HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Microsoft\SPEECH\Voices\Tokens\MSTTS_V110_enUS_EvaM\Attributes]
""Age""=""Adult""
""Gender""=""Female""
""Version""=""11.0""
""Language""=""409""
""Name""=""Microsoft Eva Mobile""
""SharedPronunciation""=""""
""Vendor""=""Microsoft""

[HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Microsoft\Speech_OneCore\Voices\Tokens\MSTTS_V110_enUS_EvaM]
@=""Microsoft Eva Mobile - English (United States)""
""409""=""Microsoft Eva Mobile - English (United States)""
""CLSID""=""{179F3D56-1B0B-42B2-A962-59B7EF59FE1B}""
""LangDataPath""=hex(2):25,00,77,00,69,00,6e,00,64,00,69,00,72,00,25,00,5c,00,53,\
  00,70,00,65,00,65,00,63,00,68,00,5f,00,4f,00,6e,00,65,00,43,00,6f,00,72,00,\
  65,00,5c,00,45,00,6e,00,67,00,69,00,6e,00,65,00,73,00,5c,00,54,00,54,00,53,\
  00,5c,00,65,00,6e,00,2d,00,55,00,53,00,5c,00,4d,00,53,00,54,00,54,00,53,00,\
  4c,00,6f,00,63,00,65,00,6e,00,55,00,53,00,2e,00,64,00,61,00,74,00,00,00
""VoicePath""=hex(2):25,00,77,00,69,00,6e,00,64,00,69,00,72,00,25,00,5c,00,53,00,\
  70,00,65,00,65,00,63,00,68,00,5f,00,4f,00,6e,00,65,00,43,00,6f,00,72,00,65,\
  00,5c,00,45,00,6e,00,67,00,69,00,6e,00,65,00,73,00,5c,00,54,00,54,00,53,00,\
  5c,00,65,00,6e,00,2d,00,55,00,53,00,5c,00,4d,00,31,00,30,00,33,00,33,00,45,\
  00,76,00,61,00,00,00

[HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Microsoft\Speech_OneCore\Voices\Tokens\MSTTS_V110_enUS_EvaM\Attributes]
""Age""=""Adult""
""Gender""=""Female""
""Version""=""11.0""
""Language""=""409""
""Name""=""Microsoft Eva Mobile""
""SharedPronunciation""=""""
""Vendor""=""Microsoft""";
		#endregion

		private void menuStrip1_MouseDown(object sender, MouseEventArgs e)
		{
			ReleaseCapture();
			SendMessage(this.Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);

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
