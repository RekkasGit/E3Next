
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
            this.labelPlayer = new System.Windows.Forms.Label();
            this.labelPlayerName = new System.Windows.Forms.Label();
            this.labelHP = new System.Windows.Forms.Label();
            this.labelHPTotal = new System.Windows.Forms.Label();
            this.richTextBoxConsole = new System.Windows.Forms.RichTextBox();
            this.richTextBoxMQConsole = new System.Windows.Forms.RichTextBox();
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.textBoxConsoleInput = new System.Windows.Forms.TextBox();
            this.splitContainer2 = new System.Windows.Forms.SplitContainer();
            this.richTextBoxMelee = new System.Windows.Forms.RichTextBox();
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
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer2)).BeginInit();
            this.splitContainer2.Panel1.SuspendLayout();
            this.splitContainer2.Panel2.SuspendLayout();
            this.splitContainer2.SuspendLayout();
            this.SuspendLayout();
            // 
            // labelPlayer
            // 
            this.labelPlayer.AutoSize = true;
            this.labelPlayer.Font = new System.Drawing.Font("Microsoft Sans Serif", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelPlayer.ForeColor = System.Drawing.SystemColors.MenuHighlight;
            this.labelPlayer.Location = new System.Drawing.Point(13, 13);
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
            this.labelHP.Location = new System.Drawing.Point(43, 49);
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
            this.labelHPTotal.Location = new System.Drawing.Point(93, 48);
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
            this.richTextBoxConsole.Size = new System.Drawing.Size(595, 236);
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
            this.richTextBoxMQConsole.Size = new System.Drawing.Size(305, 236);
            this.richTextBoxMQConsole.TabIndex = 5;
            this.richTextBoxMQConsole.Text = "";
            // 
            // splitContainer1
            // 
            this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.splitContainer1.Location = new System.Drawing.Point(0, 373);
            this.splitContainer1.Name = "splitContainer1";
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.richTextBoxMQConsole);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.textBoxConsoleInput);
            this.splitContainer1.Panel2.Controls.Add(this.richTextBoxConsole);
            this.splitContainer1.Size = new System.Drawing.Size(904, 236);
            this.splitContainer1.SplitterDistance = 305;
            this.splitContainer1.TabIndex = 6;
            // 
            // textBoxConsoleInput
            // 
            this.textBoxConsoleInput.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.textBoxConsoleInput.Location = new System.Drawing.Point(0, 216);
            this.textBoxConsoleInput.Name = "textBoxConsoleInput";
            this.textBoxConsoleInput.Size = new System.Drawing.Size(595, 20);
            this.textBoxConsoleInput.TabIndex = 5;
            this.textBoxConsoleInput.KeyDown += new System.Windows.Forms.KeyEventHandler(this.textBoxConsoleInput_KeyDown);
            // 
            // splitContainer2
            // 
            this.splitContainer2.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.splitContainer2.Location = new System.Drawing.Point(0, 167);
            this.splitContainer2.Name = "splitContainer2";
            // 
            // splitContainer2.Panel1
            // 
            this.splitContainer2.Panel1.Controls.Add(this.richTextBoxMelee);
            // 
            // splitContainer2.Panel2
            // 
            this.splitContainer2.Panel2.Controls.Add(this.richTextBoxSpells);
            this.splitContainer2.Size = new System.Drawing.Size(904, 206);
            this.splitContainer2.SplitterDistance = 306;
            this.splitContainer2.TabIndex = 7;
            // 
            // richTextBoxMelee
            // 
            this.richTextBoxMelee.BackColor = System.Drawing.Color.Black;
            this.richTextBoxMelee.Dock = System.Windows.Forms.DockStyle.Fill;
            this.richTextBoxMelee.ForeColor = System.Drawing.Color.White;
            this.richTextBoxMelee.Location = new System.Drawing.Point(0, 0);
            this.richTextBoxMelee.Name = "richTextBoxMelee";
            this.richTextBoxMelee.Size = new System.Drawing.Size(306, 206);
            this.richTextBoxMelee.TabIndex = 0;
            this.richTextBoxMelee.Text = "";
            // 
            // richTextBoxSpells
            // 
            this.richTextBoxSpells.BackColor = System.Drawing.Color.Black;
            this.richTextBoxSpells.Dock = System.Windows.Forms.DockStyle.Fill;
            this.richTextBoxSpells.ForeColor = System.Drawing.Color.LightSkyBlue;
            this.richTextBoxSpells.Location = new System.Drawing.Point(0, 0);
            this.richTextBoxSpells.Name = "richTextBoxSpells";
            this.richTextBoxSpells.Size = new System.Drawing.Size(594, 206);
            this.richTextBoxSpells.TabIndex = 0;
            this.richTextBoxSpells.Text = "";
            // 
            // buttonResetParse
            // 
            this.buttonResetParse.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonResetParse.Location = new System.Drawing.Point(817, 138);
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
            this.labelTotalDamage.Location = new System.Drawing.Point(256, 97);
            this.labelTotalDamage.Name = "labelTotalDamage";
            this.labelTotalDamage.Size = new System.Drawing.Size(74, 13);
            this.labelTotalDamage.TabIndex = 9;
            this.labelTotalDamage.Text = "TotalDamage:";
            // 
            // labelTotalDamageValue
            // 
            this.labelTotalDamageValue.AutoSize = true;
            this.labelTotalDamageValue.Location = new System.Drawing.Point(336, 97);
            this.labelTotalDamageValue.Name = "labelTotalDamageValue";
            this.labelTotalDamageValue.Size = new System.Drawing.Size(13, 13);
            this.labelTotalDamageValue.TabIndex = 10;
            this.labelTotalDamageValue.Text = "0";
            // 
            // labelTotalTime
            // 
            this.labelTotalTime.AutoSize = true;
            this.labelTotalTime.Location = new System.Drawing.Point(273, 119);
            this.labelTotalTime.Name = "labelTotalTime";
            this.labelTotalTime.Size = new System.Drawing.Size(57, 13);
            this.labelTotalTime.TabIndex = 11;
            this.labelTotalTime.Text = "TotalTime:";
            // 
            // labelTotalTimeValue
            // 
            this.labelTotalTimeValue.AutoSize = true;
            this.labelTotalTimeValue.Location = new System.Drawing.Point(336, 119);
            this.labelTotalTimeValue.Name = "labelTotalTimeValue";
            this.labelTotalTimeValue.Size = new System.Drawing.Size(13, 13);
            this.labelTotalTimeValue.TabIndex = 12;
            this.labelTotalTimeValue.Text = "0";
            // 
            // labelYourDamageValue
            // 
            this.labelYourDamageValue.AutoSize = true;
            this.labelYourDamageValue.Location = new System.Drawing.Point(336, 21);
            this.labelYourDamageValue.Name = "labelYourDamageValue";
            this.labelYourDamageValue.Size = new System.Drawing.Size(13, 13);
            this.labelYourDamageValue.TabIndex = 14;
            this.labelYourDamageValue.Text = "0";
            // 
            // labelYourDamage
            // 
            this.labelYourDamage.AutoSize = true;
            this.labelYourDamage.Location = new System.Drawing.Point(255, 21);
            this.labelYourDamage.Name = "labelYourDamage";
            this.labelYourDamage.Size = new System.Drawing.Size(75, 13);
            this.labelYourDamage.TabIndex = 13;
            this.labelYourDamage.Text = "Your Damage:";
            // 
            // labelPetDamageValue
            // 
            this.labelPetDamageValue.AutoSize = true;
            this.labelPetDamageValue.Location = new System.Drawing.Point(336, 41);
            this.labelPetDamageValue.Name = "labelPetDamageValue";
            this.labelPetDamageValue.Size = new System.Drawing.Size(13, 13);
            this.labelPetDamageValue.TabIndex = 16;
            this.labelPetDamageValue.Text = "0";
            // 
            // labelPetDamage
            // 
            this.labelPetDamage.AutoSize = true;
            this.labelPetDamage.Location = new System.Drawing.Point(261, 41);
            this.labelPetDamage.Name = "labelPetDamage";
            this.labelPetDamage.Size = new System.Drawing.Size(69, 13);
            this.labelPetDamage.TabIndex = 15;
            this.labelPetDamage.Text = "Pet Damage:";
            // 
            // labelYourDamageShieldValue
            // 
            this.labelYourDamageShieldValue.AutoSize = true;
            this.labelYourDamageShieldValue.Location = new System.Drawing.Point(336, 64);
            this.labelYourDamageShieldValue.Name = "labelYourDamageShieldValue";
            this.labelYourDamageShieldValue.Size = new System.Drawing.Size(13, 13);
            this.labelYourDamageShieldValue.TabIndex = 18;
            this.labelYourDamageShieldValue.Text = "0";
            // 
            // labelYourDamageShield
            // 
            this.labelYourDamageShield.AutoSize = true;
            this.labelYourDamageShield.Location = new System.Drawing.Point(262, 64);
            this.labelYourDamageShield.Name = "labelYourDamageShield";
            this.labelYourDamageShield.Size = new System.Drawing.Size(68, 13);
            this.labelYourDamageShield.TabIndex = 17;
            this.labelYourDamageShield.Text = "DS Damage:";
            // 
            // labelDamageToYouValue
            // 
            this.labelDamageToYouValue.AutoSize = true;
            this.labelDamageToYouValue.Location = new System.Drawing.Point(598, 21);
            this.labelDamageToYouValue.Name = "labelDamageToYouValue";
            this.labelDamageToYouValue.Size = new System.Drawing.Size(13, 13);
            this.labelDamageToYouValue.TabIndex = 20;
            this.labelDamageToYouValue.Text = "0";
            // 
            // labelDamageToYou
            // 
            this.labelDamageToYou.AutoSize = true;
            this.labelDamageToYou.Location = new System.Drawing.Point(500, 21);
            this.labelDamageToYou.Name = "labelDamageToYou";
            this.labelDamageToYou.Size = new System.Drawing.Size(92, 13);
            this.labelDamageToYou.TabIndex = 19;
            this.labelDamageToYou.Text = "Damage To YOU:";
            // 
            // labelHealingYouValue
            // 
            this.labelHealingYouValue.AutoSize = true;
            this.labelHealingYouValue.Location = new System.Drawing.Point(598, 46);
            this.labelHealingYouValue.Name = "labelHealingYouValue";
            this.labelHealingYouValue.Size = new System.Drawing.Size(13, 13);
            this.labelHealingYouValue.TabIndex = 22;
            this.labelHealingYouValue.Text = "0";
            // 
            // labelHealingYou
            // 
            this.labelHealingYou.AutoSize = true;
            this.labelHealingYou.Location = new System.Drawing.Point(500, 46);
            this.labelHealingYou.Name = "labelHealingYou";
            this.labelHealingYou.Size = new System.Drawing.Size(88, 13);
            this.labelHealingYou.TabIndex = 21;
            this.labelHealingYou.Text = "Healing To YOU:";
            // 
            // labelYourDamageDPSValue
            // 
            this.labelYourDamageDPSValue.AutoSize = true;
            this.labelYourDamageDPSValue.Location = new System.Drawing.Point(393, 21);
            this.labelYourDamageDPSValue.Name = "labelYourDamageDPSValue";
            this.labelYourDamageDPSValue.Size = new System.Drawing.Size(13, 13);
            this.labelYourDamageDPSValue.TabIndex = 23;
            this.labelYourDamageDPSValue.Text = "0";
            // 
            // labelPetDamageDPSValue
            // 
            this.labelPetDamageDPSValue.AutoSize = true;
            this.labelPetDamageDPSValue.Location = new System.Drawing.Point(393, 41);
            this.labelPetDamageDPSValue.Name = "labelPetDamageDPSValue";
            this.labelPetDamageDPSValue.Size = new System.Drawing.Size(13, 13);
            this.labelPetDamageDPSValue.TabIndex = 24;
            this.labelPetDamageDPSValue.Text = "0";
            // 
            // labelDamageShieldDPSValue
            // 
            this.labelDamageShieldDPSValue.AutoSize = true;
            this.labelDamageShieldDPSValue.Location = new System.Drawing.Point(393, 64);
            this.labelDamageShieldDPSValue.Name = "labelDamageShieldDPSValue";
            this.labelDamageShieldDPSValue.Size = new System.Drawing.Size(13, 13);
            this.labelDamageShieldDPSValue.TabIndex = 25;
            this.labelDamageShieldDPSValue.Text = "0";
            // 
            // labelTotalDamageDPSValue
            // 
            this.labelTotalDamageDPSValue.AutoSize = true;
            this.labelTotalDamageDPSValue.Location = new System.Drawing.Point(393, 97);
            this.labelTotalDamageDPSValue.Name = "labelTotalDamageDPSValue";
            this.labelTotalDamageDPSValue.Size = new System.Drawing.Size(13, 13);
            this.labelTotalDamageDPSValue.TabIndex = 26;
            this.labelTotalDamageDPSValue.Text = "0";
            // 
            // labelPetNameValue
            // 
            this.labelPetNameValue.AutoSize = true;
            this.labelPetNameValue.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelPetNameValue.ForeColor = System.Drawing.Color.Crimson;
            this.labelPetNameValue.Location = new System.Drawing.Point(92, 100);
            this.labelPetNameValue.Name = "labelPetNameValue";
            this.labelPetNameValue.Size = new System.Drawing.Size(51, 20);
            this.labelPetNameValue.TabIndex = 28;
            this.labelPetNameValue.Text = "None";
            // 
            // labelPetName
            // 
            this.labelPetName.AutoSize = true;
            this.labelPetName.Font = new System.Drawing.Font("Microsoft Sans Serif", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelPetName.ForeColor = System.Drawing.SystemColors.MenuHighlight;
            this.labelPetName.Location = new System.Drawing.Point(40, 97);
            this.labelPetName.Name = "labelPetName";
            this.labelPetName.Size = new System.Drawing.Size(46, 24);
            this.labelPetName.TabIndex = 27;
            this.labelPetName.Text = "Pet:";
            // 
            // E3UI
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoSize = true;
            this.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.ClientSize = new System.Drawing.Size(904, 609);
            this.ControlBox = false;
            this.Controls.Add(this.labelPetNameValue);
            this.Controls.Add(this.labelPetName);
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
    }
}

