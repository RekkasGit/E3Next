
namespace E3NextUI
{
    partial class E3UI
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(E3UI));
            this.labelPlayer = new System.Windows.Forms.Label();
            this.labelPlayerName = new System.Windows.Forms.Label();
            this.labelHP = new System.Windows.Forms.Label();
            this.labelHPTotal = new System.Windows.Forms.Label();
            this.richTextBoxConsole = new System.Windows.Forms.RichTextBox();
            this.richTextBoxMQConsole = new System.Windows.Forms.RichTextBox();
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.richTextBoxMelee = new System.Windows.Forms.RichTextBox();
            this.textBoxConsoleInput = new System.Windows.Forms.TextBox();
            this.splitContainer2 = new System.Windows.Forms.SplitContainer();
            this.richTextBoxSpells = new System.Windows.Forms.RichTextBox();
            this.buttonResetParse = new System.Windows.Forms.Button();
            this.labelTotalDamage = new System.Windows.Forms.Label();
            this.labelTotalDamageValue = new System.Windows.Forms.Label();
            this.labelTotalTime = new System.Windows.Forms.Label();
            this.labelTotalTimeValue = new System.Windows.Forms.Label();
            this.labelYourDamageValue = new System.Windows.Forms.Label();
            this.labelYourDamage = new System.Windows.Forms.Label();
            this.labelPetDamageValue = new System.Windows.Forms.Label();
            this.labelPetDamage = new System.Windows.Forms.Label();
            this.labelYourDamageShieldValue = new System.Windows.Forms.Label();
            this.labelYourDamageShield = new System.Windows.Forms.Label();
            this.labelDamageToYouValue = new System.Windows.Forms.Label();
            this.labelDamageToYou = new System.Windows.Forms.Label();
            this.labelHealingYouValue = new System.Windows.Forms.Label();
            this.labelHealingYou = new System.Windows.Forms.Label();
            this.labelYourDamageDPSValue = new System.Windows.Forms.Label();
            this.labelPetDamageDPSValue = new System.Windows.Forms.Label();
            this.labelDamageShieldDPSValue = new System.Windows.Forms.Label();
            this.labelTotalDamageDPSValue = new System.Windows.Forms.Label();
            this.labelPetNameValue = new System.Windows.Forms.Label();
            this.labelPetName = new System.Windows.Forms.Label();
            this.buttonPauseConsoles = new System.Windows.Forms.Button();
            this.labelInCombat = new System.Windows.Forms.Label();
            this.labelInCombatValue = new System.Windows.Forms.Label();
            this.labelManaCurrent = new System.Windows.Forms.Label();
            this.labelMana = new System.Windows.Forms.Label();
            this.labelStaminaValue = new System.Windows.Forms.Label();
            this.labelStaminia = new System.Windows.Forms.Label();
            this.labelCastingValue = new System.Windows.Forms.Label();
            this.labelCasting = new System.Windows.Forms.Label();
            this.labelHealingByYou = new System.Windows.Forms.Label();
            this.labelHealingByYouValue = new System.Windows.Forms.Label();
            this.pbCollapseConsole = new System.Windows.Forms.PictureBox();
            this.panel1 = new System.Windows.Forms.Panel();
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer2)).BeginInit();
            this.splitContainer2.Panel1.SuspendLayout();
            this.splitContainer2.Panel2.SuspendLayout();
            this.splitContainer2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pbCollapseConsole)).BeginInit();
            this.panel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // labelPlayer
            // 
            this.labelPlayer.AutoSize = true;
            this.labelPlayer.Font = new System.Drawing.Font("Microsoft Sans Serif", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelPlayer.ForeColor = System.Drawing.SystemColors.MenuHighlight;
            this.labelPlayer.Location = new System.Drawing.Point(14, 13);
            this.labelPlayer.Name = "labelPlayer";
            this.labelPlayer.Size = new System.Drawing.Size(74, 24);
            this.labelPlayer.TabIndex = 0;
            this.labelPlayer.Text = "Player:";
            // 
            // labelPlayerName
            // 
            this.labelPlayerName.AutoSize = true;
            this.labelPlayerName.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelPlayerName.ForeColor = System.Drawing.Color.Crimson;
            this.labelPlayerName.Location = new System.Drawing.Point(93, 16);
            this.labelPlayerName.Name = "labelPlayerName";
            this.labelPlayerName.Size = new System.Drawing.Size(93, 20);
            this.labelPlayerName.TabIndex = 1;
            this.labelPlayerName.Text = "YourName";
            // 
            // labelHP
            // 
            this.labelHP.AutoSize = true;
            this.labelHP.Font = new System.Drawing.Font("Microsoft Sans Serif", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelHP.ForeColor = System.Drawing.SystemColors.MenuHighlight;
            this.labelHP.Location = new System.Drawing.Point(13, 46);
            this.labelHP.Name = "labelHP";
            this.labelHP.Size = new System.Drawing.Size(44, 24);
            this.labelHP.TabIndex = 2;
            this.labelHP.Text = "HP:";
            // 
            // labelHPTotal
            // 
            this.labelHPTotal.AutoSize = true;
            this.labelHPTotal.Font = new System.Drawing.Font("Microsoft Sans Serif", 15.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelHPTotal.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(192)))), ((int)(((byte)(0)))));
            this.labelHPTotal.Location = new System.Drawing.Point(63, 45);
            this.labelHPTotal.Name = "labelHPTotal";
            this.labelHPTotal.Size = new System.Drawing.Size(25, 25);
            this.labelHPTotal.TabIndex = 3;
            this.labelHPTotal.Text = "0";
            // 
            // richTextBoxConsole
            // 
            this.richTextBoxConsole.BackColor = System.Drawing.Color.Black;
            this.richTextBoxConsole.Dock = System.Windows.Forms.DockStyle.Fill;
            this.richTextBoxConsole.Font = new System.Drawing.Font("Microsoft Sans Serif", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.richTextBoxConsole.ForeColor = System.Drawing.Color.Lime;
            this.richTextBoxConsole.Location = new System.Drawing.Point(0, 0);
            this.richTextBoxConsole.Name = "richTextBoxConsole";
            this.richTextBoxConsole.ReadOnly = true;
            this.richTextBoxConsole.ScrollBars = System.Windows.Forms.RichTextBoxScrollBars.None;
            this.richTextBoxConsole.Size = new System.Drawing.Size(629, 236);
            this.richTextBoxConsole.TabIndex = 4;
            this.richTextBoxConsole.Text = "";
            // 
            // richTextBoxMQConsole
            // 
            this.richTextBoxMQConsole.BackColor = System.Drawing.Color.Black;
            this.richTextBoxMQConsole.Dock = System.Windows.Forms.DockStyle.Fill;
            this.richTextBoxMQConsole.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.richTextBoxMQConsole.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(192)))), ((int)(((byte)(192)))));
            this.richTextBoxMQConsole.Location = new System.Drawing.Point(0, 0);
            this.richTextBoxMQConsole.Name = "richTextBoxMQConsole";
            this.richTextBoxMQConsole.ScrollBars = System.Windows.Forms.RichTextBoxScrollBars.None;
            this.richTextBoxMQConsole.Size = new System.Drawing.Size(628, 206);
            this.richTextBoxMQConsole.TabIndex = 5;
            this.richTextBoxMQConsole.Text = "";
            // 
            // splitContainer1
            // 
            this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.splitContainer1.Location = new System.Drawing.Point(0, 485);
            this.splitContainer1.Name = "splitContainer1";
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.richTextBoxMelee);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.textBoxConsoleInput);
            this.splitContainer1.Panel2.Controls.Add(this.richTextBoxConsole);
            this.splitContainer1.Size = new System.Drawing.Size(954, 236);
            this.splitContainer1.SplitterDistance = 321;
            this.splitContainer1.TabIndex = 6;
            // 
            // richTextBoxMelee
            // 
            this.richTextBoxMelee.BackColor = System.Drawing.Color.Black;
            this.richTextBoxMelee.Dock = System.Windows.Forms.DockStyle.Fill;
            this.richTextBoxMelee.ForeColor = System.Drawing.Color.White;
            this.richTextBoxMelee.Location = new System.Drawing.Point(0, 0);
            this.richTextBoxMelee.Name = "richTextBoxMelee";
            this.richTextBoxMelee.ScrollBars = System.Windows.Forms.RichTextBoxScrollBars.None;
            this.richTextBoxMelee.Size = new System.Drawing.Size(321, 236);
            this.richTextBoxMelee.TabIndex = 0;
            this.richTextBoxMelee.Text = "";
            // 
            // textBoxConsoleInput
            // 
            this.textBoxConsoleInput.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.textBoxConsoleInput.Location = new System.Drawing.Point(0, 216);
            this.textBoxConsoleInput.Name = "textBoxConsoleInput";
            this.textBoxConsoleInput.Size = new System.Drawing.Size(629, 20);
            this.textBoxConsoleInput.TabIndex = 5;
            this.textBoxConsoleInput.KeyDown += new System.Windows.Forms.KeyEventHandler(this.textBoxConsoleInput_KeyDown);
            // 
            // splitContainer2
            // 
            this.splitContainer2.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.splitContainer2.Location = new System.Drawing.Point(0, 279);
            this.splitContainer2.Name = "splitContainer2";
            // 
            // splitContainer2.Panel1
            // 
            this.splitContainer2.Panel1.Controls.Add(this.richTextBoxSpells);
            // 
            // splitContainer2.Panel2
            // 
            this.splitContainer2.Panel2.Controls.Add(this.richTextBoxMQConsole);
            this.splitContainer2.Size = new System.Drawing.Size(954, 206);
            this.splitContainer2.SplitterDistance = 322;
            this.splitContainer2.TabIndex = 7;
            // 
            // richTextBoxSpells
            // 
            this.richTextBoxSpells.BackColor = System.Drawing.Color.Black;
            this.richTextBoxSpells.Dock = System.Windows.Forms.DockStyle.Fill;
            this.richTextBoxSpells.ForeColor = System.Drawing.Color.LightSkyBlue;
            this.richTextBoxSpells.Location = new System.Drawing.Point(0, 0);
            this.richTextBoxSpells.Name = "richTextBoxSpells";
            this.richTextBoxSpells.ScrollBars = System.Windows.Forms.RichTextBoxScrollBars.None;
            this.richTextBoxSpells.Size = new System.Drawing.Size(322, 206);
            this.richTextBoxSpells.TabIndex = 0;
            this.richTextBoxSpells.Text = "";
            // 
            // buttonResetParse
            // 
            this.buttonResetParse.Location = new System.Drawing.Point(649, 9);
            this.buttonResetParse.Name = "buttonResetParse";
            this.buttonResetParse.Size = new System.Drawing.Size(75, 23);
            this.buttonResetParse.TabIndex = 8;
            this.buttonResetParse.Text = "ResetParse";
            this.buttonResetParse.UseVisualStyleBackColor = true;
            this.buttonResetParse.Click += new System.EventHandler(this.buttonResetParse_Click);
            // 
            // labelTotalDamage
            // 
            this.labelTotalDamage.AutoSize = true;
            this.labelTotalDamage.Location = new System.Drawing.Point(256, 90);
            this.labelTotalDamage.Name = "labelTotalDamage";
            this.labelTotalDamage.Size = new System.Drawing.Size(74, 13);
            this.labelTotalDamage.TabIndex = 9;
            this.labelTotalDamage.Text = "TotalDamage:";
            // 
            // labelTotalDamageValue
            // 
            this.labelTotalDamageValue.AutoSize = true;
            this.labelTotalDamageValue.Location = new System.Drawing.Point(336, 90);
            this.labelTotalDamageValue.Name = "labelTotalDamageValue";
            this.labelTotalDamageValue.Size = new System.Drawing.Size(13, 13);
            this.labelTotalDamageValue.TabIndex = 10;
            this.labelTotalDamageValue.Text = "0";
            // 
            // labelTotalTime
            // 
            this.labelTotalTime.AutoSize = true;
            this.labelTotalTime.Location = new System.Drawing.Point(273, 112);
            this.labelTotalTime.Name = "labelTotalTime";
            this.labelTotalTime.Size = new System.Drawing.Size(57, 13);
            this.labelTotalTime.TabIndex = 11;
            this.labelTotalTime.Text = "TotalTime:";
            // 
            // labelTotalTimeValue
            // 
            this.labelTotalTimeValue.AutoSize = true;
            this.labelTotalTimeValue.Location = new System.Drawing.Point(336, 112);
            this.labelTotalTimeValue.Name = "labelTotalTimeValue";
            this.labelTotalTimeValue.Size = new System.Drawing.Size(13, 13);
            this.labelTotalTimeValue.TabIndex = 12;
            this.labelTotalTimeValue.Text = "0";
            // 
            // labelYourDamageValue
            // 
            this.labelYourDamageValue.AutoSize = true;
            this.labelYourDamageValue.Location = new System.Drawing.Point(336, 14);
            this.labelYourDamageValue.Name = "labelYourDamageValue";
            this.labelYourDamageValue.Size = new System.Drawing.Size(13, 13);
            this.labelYourDamageValue.TabIndex = 14;
            this.labelYourDamageValue.Text = "0";
            // 
            // labelYourDamage
            // 
            this.labelYourDamage.AutoSize = true;
            this.labelYourDamage.Location = new System.Drawing.Point(255, 14);
            this.labelYourDamage.Name = "labelYourDamage";
            this.labelYourDamage.Size = new System.Drawing.Size(75, 13);
            this.labelYourDamage.TabIndex = 13;
            this.labelYourDamage.Text = "Your Damage:";
            // 
            // labelPetDamageValue
            // 
            this.labelPetDamageValue.AutoSize = true;
            this.labelPetDamageValue.Location = new System.Drawing.Point(336, 34);
            this.labelPetDamageValue.Name = "labelPetDamageValue";
            this.labelPetDamageValue.Size = new System.Drawing.Size(13, 13);
            this.labelPetDamageValue.TabIndex = 16;
            this.labelPetDamageValue.Text = "0";
            // 
            // labelPetDamage
            // 
            this.labelPetDamage.AutoSize = true;
            this.labelPetDamage.Location = new System.Drawing.Point(261, 34);
            this.labelPetDamage.Name = "labelPetDamage";
            this.labelPetDamage.Size = new System.Drawing.Size(69, 13);
            this.labelPetDamage.TabIndex = 15;
            this.labelPetDamage.Text = "Pet Damage:";
            // 
            // labelYourDamageShieldValue
            // 
            this.labelYourDamageShieldValue.AutoSize = true;
            this.labelYourDamageShieldValue.Location = new System.Drawing.Point(336, 57);
            this.labelYourDamageShieldValue.Name = "labelYourDamageShieldValue";
            this.labelYourDamageShieldValue.Size = new System.Drawing.Size(13, 13);
            this.labelYourDamageShieldValue.TabIndex = 18;
            this.labelYourDamageShieldValue.Text = "0";
            // 
            // labelYourDamageShield
            // 
            this.labelYourDamageShield.AutoSize = true;
            this.labelYourDamageShield.Location = new System.Drawing.Point(262, 57);
            this.labelYourDamageShield.Name = "labelYourDamageShield";
            this.labelYourDamageShield.Size = new System.Drawing.Size(68, 13);
            this.labelYourDamageShield.TabIndex = 17;
            this.labelYourDamageShield.Text = "DS Damage:";
            // 
            // labelDamageToYouValue
            // 
            this.labelDamageToYouValue.AutoSize = true;
            this.labelDamageToYouValue.Location = new System.Drawing.Point(598, 14);
            this.labelDamageToYouValue.Name = "labelDamageToYouValue";
            this.labelDamageToYouValue.Size = new System.Drawing.Size(13, 13);
            this.labelDamageToYouValue.TabIndex = 20;
            this.labelDamageToYouValue.Text = "0";
            // 
            // labelDamageToYou
            // 
            this.labelDamageToYou.AutoSize = true;
            this.labelDamageToYou.Location = new System.Drawing.Point(496, 14);
            this.labelDamageToYou.Name = "labelDamageToYou";
            this.labelDamageToYou.Size = new System.Drawing.Size(92, 13);
            this.labelDamageToYou.TabIndex = 19;
            this.labelDamageToYou.Text = "Damage To YOU:";
            // 
            // labelHealingYouValue
            // 
            this.labelHealingYouValue.AutoSize = true;
            this.labelHealingYouValue.Location = new System.Drawing.Point(598, 39);
            this.labelHealingYouValue.Name = "labelHealingYouValue";
            this.labelHealingYouValue.Size = new System.Drawing.Size(13, 13);
            this.labelHealingYouValue.TabIndex = 22;
            this.labelHealingYouValue.Text = "0";
            // 
            // labelHealingYou
            // 
            this.labelHealingYou.AutoSize = true;
            this.labelHealingYou.Location = new System.Drawing.Point(500, 39);
            this.labelHealingYou.Name = "labelHealingYou";
            this.labelHealingYou.Size = new System.Drawing.Size(88, 13);
            this.labelHealingYou.TabIndex = 21;
            this.labelHealingYou.Text = "Healing To YOU:";
            // 
            // labelYourDamageDPSValue
            // 
            this.labelYourDamageDPSValue.AutoSize = true;
            this.labelYourDamageDPSValue.Location = new System.Drawing.Point(393, 14);
            this.labelYourDamageDPSValue.Name = "labelYourDamageDPSValue";
            this.labelYourDamageDPSValue.Size = new System.Drawing.Size(13, 13);
            this.labelYourDamageDPSValue.TabIndex = 23;
            this.labelYourDamageDPSValue.Text = "0";
            // 
            // labelPetDamageDPSValue
            // 
            this.labelPetDamageDPSValue.AutoSize = true;
            this.labelPetDamageDPSValue.Location = new System.Drawing.Point(393, 34);
            this.labelPetDamageDPSValue.Name = "labelPetDamageDPSValue";
            this.labelPetDamageDPSValue.Size = new System.Drawing.Size(13, 13);
            this.labelPetDamageDPSValue.TabIndex = 24;
            this.labelPetDamageDPSValue.Text = "0";
            // 
            // labelDamageShieldDPSValue
            // 
            this.labelDamageShieldDPSValue.AutoSize = true;
            this.labelDamageShieldDPSValue.Location = new System.Drawing.Point(393, 57);
            this.labelDamageShieldDPSValue.Name = "labelDamageShieldDPSValue";
            this.labelDamageShieldDPSValue.Size = new System.Drawing.Size(13, 13);
            this.labelDamageShieldDPSValue.TabIndex = 25;
            this.labelDamageShieldDPSValue.Text = "0";
            // 
            // labelTotalDamageDPSValue
            // 
            this.labelTotalDamageDPSValue.AutoSize = true;
            this.labelTotalDamageDPSValue.Location = new System.Drawing.Point(393, 90);
            this.labelTotalDamageDPSValue.Name = "labelTotalDamageDPSValue";
            this.labelTotalDamageDPSValue.Size = new System.Drawing.Size(13, 13);
            this.labelTotalDamageDPSValue.TabIndex = 26;
            this.labelTotalDamageDPSValue.Text = "0";
            // 
            // labelPetNameValue
            // 
            this.labelPetNameValue.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.labelPetNameValue.AutoSize = true;
            this.labelPetNameValue.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelPetNameValue.ForeColor = System.Drawing.Color.Crimson;
            this.labelPetNameValue.Location = new System.Drawing.Point(270, 7);
            this.labelPetNameValue.Name = "labelPetNameValue";
            this.labelPetNameValue.Size = new System.Drawing.Size(51, 20);
            this.labelPetNameValue.TabIndex = 28;
            this.labelPetNameValue.Text = "None";
            // 
            // labelPetName
            // 
            this.labelPetName.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.labelPetName.AutoSize = true;
            this.labelPetName.Font = new System.Drawing.Font("Microsoft Sans Serif", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelPetName.ForeColor = System.Drawing.SystemColors.MenuHighlight;
            this.labelPetName.Location = new System.Drawing.Point(229, 4);
            this.labelPetName.Name = "labelPetName";
            this.labelPetName.Size = new System.Drawing.Size(46, 24);
            this.labelPetName.TabIndex = 27;
            this.labelPetName.Text = "Pet:";
            // 
            // buttonPauseConsoles
            // 
            this.buttonPauseConsoles.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.buttonPauseConsoles.Location = new System.Drawing.Point(3, 32);
            this.buttonPauseConsoles.Name = "buttonPauseConsoles";
            this.buttonPauseConsoles.Size = new System.Drawing.Size(118, 23);
            this.buttonPauseConsoles.TabIndex = 29;
            this.buttonPauseConsoles.Text = "Pause Consoles";
            this.buttonPauseConsoles.UseVisualStyleBackColor = true;
            this.buttonPauseConsoles.Click += new System.EventHandler(this.buttonPauseConsoles_Click);
            // 
            // labelInCombat
            // 
            this.labelInCombat.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.labelInCombat.AutoSize = true;
            this.labelInCombat.Font = new System.Drawing.Font("Microsoft Sans Serif", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelInCombat.ForeColor = System.Drawing.SystemColors.MenuHighlight;
            this.labelInCombat.Location = new System.Drawing.Point(167, 31);
            this.labelInCombat.Name = "labelInCombat";
            this.labelInCombat.Size = new System.Drawing.Size(110, 24);
            this.labelInCombat.TabIndex = 31;
            this.labelInCombat.Text = "In Combat:";
            // 
            // labelInCombatValue
            // 
            this.labelInCombatValue.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.labelInCombatValue.AutoSize = true;
            this.labelInCombatValue.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelInCombatValue.ForeColor = System.Drawing.Color.Red;
            this.labelInCombatValue.Location = new System.Drawing.Point(273, 34);
            this.labelInCombatValue.Name = "labelInCombatValue";
            this.labelInCombatValue.Size = new System.Drawing.Size(48, 20);
            this.labelInCombatValue.TabIndex = 32;
            this.labelInCombatValue.Text = "false";
            // 
            // labelManaCurrent
            // 
            this.labelManaCurrent.AutoSize = true;
            this.labelManaCurrent.Font = new System.Drawing.Font("Microsoft Sans Serif", 15.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelManaCurrent.ForeColor = System.Drawing.Color.Blue;
            this.labelManaCurrent.Location = new System.Drawing.Point(63, 69);
            this.labelManaCurrent.Name = "labelManaCurrent";
            this.labelManaCurrent.Size = new System.Drawing.Size(25, 25);
            this.labelManaCurrent.TabIndex = 34;
            this.labelManaCurrent.Text = "0";
            // 
            // labelMana
            // 
            this.labelMana.AutoSize = true;
            this.labelMana.Font = new System.Drawing.Font("Microsoft Sans Serif", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelMana.ForeColor = System.Drawing.SystemColors.MenuHighlight;
            this.labelMana.Location = new System.Drawing.Point(13, 70);
            this.labelMana.Name = "labelMana";
            this.labelMana.Size = new System.Drawing.Size(46, 24);
            this.labelMana.TabIndex = 33;
            this.labelMana.Text = "MP:";
            // 
            // labelStaminaValue
            // 
            this.labelStaminaValue.AutoSize = true;
            this.labelStaminaValue.Font = new System.Drawing.Font("Microsoft Sans Serif", 15.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelStaminaValue.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(192)))), ((int)(((byte)(192)))), ((int)(((byte)(0)))));
            this.labelStaminaValue.Location = new System.Drawing.Point(63, 92);
            this.labelStaminaValue.Name = "labelStaminaValue";
            this.labelStaminaValue.Size = new System.Drawing.Size(25, 25);
            this.labelStaminaValue.TabIndex = 36;
            this.labelStaminaValue.Text = "0";
            // 
            // labelStaminia
            // 
            this.labelStaminia.AutoSize = true;
            this.labelStaminia.Font = new System.Drawing.Font("Microsoft Sans Serif", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelStaminia.ForeColor = System.Drawing.SystemColors.MenuHighlight;
            this.labelStaminia.Location = new System.Drawing.Point(13, 93);
            this.labelStaminia.Name = "labelStaminia";
            this.labelStaminia.Size = new System.Drawing.Size(42, 24);
            this.labelStaminia.TabIndex = 35;
            this.labelStaminia.Text = "SP:";
            // 
            // labelCastingValue
            // 
            this.labelCastingValue.AutoSize = true;
            this.labelCastingValue.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelCastingValue.ForeColor = System.Drawing.Color.DarkCyan;
            this.labelCastingValue.Location = new System.Drawing.Point(426, 187);
            this.labelCastingValue.Name = "labelCastingValue";
            this.labelCastingValue.Size = new System.Drawing.Size(0, 20);
            this.labelCastingValue.TabIndex = 38;
            // 
            // labelCasting
            // 
            this.labelCasting.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.labelCasting.AutoSize = true;
            this.labelCasting.Font = new System.Drawing.Font("Microsoft Sans Serif", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelCasting.ForeColor = System.Drawing.SystemColors.MenuHighlight;
            this.labelCasting.Location = new System.Drawing.Point(352, 30);
            this.labelCasting.Name = "labelCasting";
            this.labelCasting.Size = new System.Drawing.Size(85, 24);
            this.labelCasting.TabIndex = 37;
            this.labelCasting.Text = "Casting:";
            // 
            // labelHealingByYou
            // 
            this.labelHealingByYou.AutoSize = true;
            this.labelHealingByYou.Location = new System.Drawing.Point(501, 63);
            this.labelHealingByYou.Name = "labelHealingByYou";
            this.labelHealingByYou.Size = new System.Drawing.Size(87, 13);
            this.labelHealingByYou.TabIndex = 39;
            this.labelHealingByYou.Text = "Healing By YOU:";
            // 
            // labelHealingByYouValue
            // 
            this.labelHealingByYouValue.AutoSize = true;
            this.labelHealingByYouValue.Location = new System.Drawing.Point(598, 63);
            this.labelHealingByYouValue.Name = "labelHealingByYouValue";
            this.labelHealingByYouValue.Size = new System.Drawing.Size(13, 13);
            this.labelHealingByYouValue.TabIndex = 40;
            this.labelHealingByYouValue.Text = "0";
            // 
            // pbCollapseConsole
            // 
            this.pbCollapseConsole.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.pbCollapseConsole.Image = ((System.Drawing.Image)(resources.GetObject("pbCollapseConsole.Image")));
            this.pbCollapseConsole.Location = new System.Drawing.Point(923, 42);
            this.pbCollapseConsole.Name = "pbCollapseConsole";
            this.pbCollapseConsole.Size = new System.Drawing.Size(28, 26);
            this.pbCollapseConsole.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.pbCollapseConsole.TabIndex = 41;
            this.pbCollapseConsole.TabStop = false;
            this.pbCollapseConsole.Click += new System.EventHandler(this.pbCollapseConsole_Click);
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.buttonPauseConsoles);
            this.panel1.Controls.Add(this.pbCollapseConsole);
            this.panel1.Controls.Add(this.labelCasting);
            this.panel1.Controls.Add(this.labelPetName);
            this.panel1.Controls.Add(this.labelPetNameValue);
            this.panel1.Controls.Add(this.labelInCombat);
            this.panel1.Controls.Add(this.labelInCombatValue);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.panel1.Location = new System.Drawing.Point(0, 208);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(954, 71);
            this.panel1.TabIndex = 42;
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.ColumnCount = 5;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.tableLayoutPanel1.Location = new System.Drawing.Point(430, 93);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 2;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel1.Size = new System.Drawing.Size(518, 100);
            this.tableLayoutPanel1.TabIndex = 43;
            // 
            // E3UI
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoSize = true;
            this.ClientSize = new System.Drawing.Size(954, 721);
            this.ControlBox = false;
            this.Controls.Add(this.tableLayoutPanel1);
            this.Controls.Add(this.panel1);
            this.Controls.Add(this.labelHealingByYouValue);
            this.Controls.Add(this.labelHealingByYou);
            this.Controls.Add(this.labelCastingValue);
            this.Controls.Add(this.labelStaminaValue);
            this.Controls.Add(this.labelStaminia);
            this.Controls.Add(this.labelManaCurrent);
            this.Controls.Add(this.labelMana);
            this.Controls.Add(this.labelTotalDamageDPSValue);
            this.Controls.Add(this.labelDamageShieldDPSValue);
            this.Controls.Add(this.labelPetDamageDPSValue);
            this.Controls.Add(this.labelYourDamageDPSValue);
            this.Controls.Add(this.labelHealingYouValue);
            this.Controls.Add(this.labelHealingYou);
            this.Controls.Add(this.labelDamageToYouValue);
            this.Controls.Add(this.labelDamageToYou);
            this.Controls.Add(this.labelYourDamageShieldValue);
            this.Controls.Add(this.labelYourDamageShield);
            this.Controls.Add(this.labelPetDamageValue);
            this.Controls.Add(this.labelPetDamage);
            this.Controls.Add(this.labelYourDamageValue);
            this.Controls.Add(this.labelYourDamage);
            this.Controls.Add(this.labelTotalTimeValue);
            this.Controls.Add(this.labelTotalTime);
            this.Controls.Add(this.labelTotalDamageValue);
            this.Controls.Add(this.labelTotalDamage);
            this.Controls.Add(this.buttonResetParse);
            this.Controls.Add(this.splitContainer2);
            this.Controls.Add(this.splitContainer1);
            this.Controls.Add(this.labelHPTotal);
            this.Controls.Add(this.labelHP);
            this.Controls.Add(this.labelPlayerName);
            this.Controls.Add(this.labelPlayer);
            this.Name = "E3UI";
            this.Text = "E3UI";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.E3UI_FormClosing);
            this.Load += new System.EventHandler(this.E3UI_Load);
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel2.ResumeLayout(false);
            this.splitContainer1.Panel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
            this.splitContainer1.ResumeLayout(false);
            this.splitContainer2.Panel1.ResumeLayout(false);
            this.splitContainer2.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer2)).EndInit();
            this.splitContainer2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.pbCollapseConsole)).EndInit();
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label labelPlayer;
        private System.Windows.Forms.Label labelPlayerName;
        private System.Windows.Forms.Label labelHP;
        private System.Windows.Forms.Label labelHPTotal;
        private System.Windows.Forms.RichTextBox richTextBoxConsole;
        private System.Windows.Forms.RichTextBox richTextBoxMQConsole;
        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.SplitContainer splitContainer2;
        private System.Windows.Forms.RichTextBox richTextBoxMelee;
        private System.Windows.Forms.RichTextBox richTextBoxSpells;
        private System.Windows.Forms.TextBox textBoxConsoleInput;
        private System.Windows.Forms.Button buttonResetParse;
        private System.Windows.Forms.Label labelTotalDamage;
        private System.Windows.Forms.Label labelTotalDamageValue;
        private System.Windows.Forms.Label labelTotalTime;
        private System.Windows.Forms.Label labelTotalTimeValue;
        private System.Windows.Forms.Label labelYourDamageValue;
        private System.Windows.Forms.Label labelYourDamage;
        private System.Windows.Forms.Label labelPetDamageValue;
        private System.Windows.Forms.Label labelPetDamage;
        private System.Windows.Forms.Label labelYourDamageShieldValue;
        private System.Windows.Forms.Label labelYourDamageShield;
        private System.Windows.Forms.Label labelDamageToYouValue;
        private System.Windows.Forms.Label labelDamageToYou;
        private System.Windows.Forms.Label labelHealingYouValue;
        private System.Windows.Forms.Label labelHealingYou;
        private System.Windows.Forms.Label labelYourDamageDPSValue;
        private System.Windows.Forms.Label labelPetDamageDPSValue;
        private System.Windows.Forms.Label labelDamageShieldDPSValue;
        private System.Windows.Forms.Label labelTotalDamageDPSValue;
        private System.Windows.Forms.Label labelPetNameValue;
        private System.Windows.Forms.Label labelPetName;
        private System.Windows.Forms.Button buttonPauseConsoles;
        private System.Windows.Forms.Label labelInCombat;
        private System.Windows.Forms.Label labelInCombatValue;
        private System.Windows.Forms.Label labelManaCurrent;
        private System.Windows.Forms.Label labelMana;
        private System.Windows.Forms.Label labelStaminaValue;
        private System.Windows.Forms.Label labelStaminia;
        private System.Windows.Forms.Label labelCastingValue;
        private System.Windows.Forms.Label labelCasting;
        private System.Windows.Forms.Label labelHealingByYou;
        private System.Windows.Forms.Label labelHealingByYouValue;
        private System.Windows.Forms.PictureBox pbCollapseConsole;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
    }
}

