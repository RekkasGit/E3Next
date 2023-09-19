namespace E3NextUI
{
	partial class TTSConfig
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
			this.checkBox_channel_ooc = new System.Windows.Forms.CheckBox();
			this.checkBox_channel_guild = new System.Windows.Forms.CheckBox();
			this.checkBox_channel_gsay = new System.Windows.Forms.CheckBox();
			this.checkBox_channel_raid = new System.Windows.Forms.CheckBox();
			this.checkBox_channel_say = new System.Windows.Forms.CheckBox();
			this.checkBox_channel_auction = new System.Windows.Forms.CheckBox();
			this.groupBox1 = new System.Windows.Forms.GroupBox();
			this.checkBox_channel_tell = new System.Windows.Forms.CheckBox();
			this.textBox_tts_regex = new System.Windows.Forms.TextBox();
			this.label1 = new System.Windows.Forms.Label();
			this.trackBar_tts_volume = new System.Windows.Forms.TrackBar();
			this.label2 = new System.Windows.Forms.Label();
			this.trackBar_tts_speed = new System.Windows.Forms.TrackBar();
			this.label3 = new System.Windows.Forms.Label();
			this.comboBox_tts_voices = new System.Windows.Forms.ComboBox();
			this.label4 = new System.Windows.Forms.Label();
			this.checkBox_tts_enabled = new System.Windows.Forms.CheckBox();
			this.buttonOK = new System.Windows.Forms.Button();
			this.button1 = new System.Windows.Forms.Button();
			this.groupBox1.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.trackBar_tts_volume)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.trackBar_tts_speed)).BeginInit();
			this.SuspendLayout();
			// 
			// checkBox_channel_ooc
			// 
			this.checkBox_channel_ooc.AutoSize = true;
			this.checkBox_channel_ooc.Location = new System.Drawing.Point(16, 23);
			this.checkBox_channel_ooc.Name = "checkBox_channel_ooc";
			this.checkBox_channel_ooc.Size = new System.Drawing.Size(49, 17);
			this.checkBox_channel_ooc.TabIndex = 0;
			this.checkBox_channel_ooc.Text = "OOC";
			this.checkBox_channel_ooc.UseVisualStyleBackColor = true;
			// 
			// checkBox_channel_guild
			// 
			this.checkBox_channel_guild.AutoSize = true;
			this.checkBox_channel_guild.Location = new System.Drawing.Point(15, 46);
			this.checkBox_channel_guild.Name = "checkBox_channel_guild";
			this.checkBox_channel_guild.Size = new System.Drawing.Size(50, 17);
			this.checkBox_channel_guild.TabIndex = 1;
			this.checkBox_channel_guild.Text = "Guild";
			this.checkBox_channel_guild.UseVisualStyleBackColor = true;
			// 
			// checkBox_channel_gsay
			// 
			this.checkBox_channel_gsay.AutoSize = true;
			this.checkBox_channel_gsay.Location = new System.Drawing.Point(15, 69);
			this.checkBox_channel_gsay.Name = "checkBox_channel_gsay";
			this.checkBox_channel_gsay.Size = new System.Drawing.Size(55, 17);
			this.checkBox_channel_gsay.TabIndex = 2;
			this.checkBox_channel_gsay.Text = "Group";
			this.checkBox_channel_gsay.UseVisualStyleBackColor = true;
			// 
			// checkBox_channel_raid
			// 
			this.checkBox_channel_raid.AutoSize = true;
			this.checkBox_channel_raid.Location = new System.Drawing.Point(71, 69);
			this.checkBox_channel_raid.Name = "checkBox_channel_raid";
			this.checkBox_channel_raid.Size = new System.Drawing.Size(48, 17);
			this.checkBox_channel_raid.TabIndex = 3;
			this.checkBox_channel_raid.Text = "Raid";
			this.checkBox_channel_raid.UseVisualStyleBackColor = true;
			// 
			// checkBox_channel_say
			// 
			this.checkBox_channel_say.AutoSize = true;
			this.checkBox_channel_say.Location = new System.Drawing.Point(71, 23);
			this.checkBox_channel_say.Name = "checkBox_channel_say";
			this.checkBox_channel_say.Size = new System.Drawing.Size(44, 17);
			this.checkBox_channel_say.TabIndex = 4;
			this.checkBox_channel_say.Text = "Say";
			this.checkBox_channel_say.UseVisualStyleBackColor = true;
			// 
			// checkBox_channel_auction
			// 
			this.checkBox_channel_auction.AutoSize = true;
			this.checkBox_channel_auction.Location = new System.Drawing.Point(71, 46);
			this.checkBox_channel_auction.Name = "checkBox_channel_auction";
			this.checkBox_channel_auction.Size = new System.Drawing.Size(62, 17);
			this.checkBox_channel_auction.TabIndex = 5;
			this.checkBox_channel_auction.Text = "Auction";
			this.checkBox_channel_auction.UseVisualStyleBackColor = true;
			// 
			// groupBox1
			// 
			this.groupBox1.Controls.Add(this.checkBox_channel_tell);
			this.groupBox1.Controls.Add(this.checkBox_channel_say);
			this.groupBox1.Controls.Add(this.checkBox_channel_auction);
			this.groupBox1.Controls.Add(this.checkBox_channel_ooc);
			this.groupBox1.Controls.Add(this.checkBox_channel_guild);
			this.groupBox1.Controls.Add(this.checkBox_channel_raid);
			this.groupBox1.Controls.Add(this.checkBox_channel_gsay);
			this.groupBox1.Location = new System.Drawing.Point(12, 12);
			this.groupBox1.Name = "groupBox1";
			this.groupBox1.Size = new System.Drawing.Size(151, 117);
			this.groupBox1.TabIndex = 6;
			this.groupBox1.TabStop = false;
			this.groupBox1.Text = "Channels";
			// 
			// checkBox_channel_tell
			// 
			this.checkBox_channel_tell.AutoSize = true;
			this.checkBox_channel_tell.Location = new System.Drawing.Point(15, 91);
			this.checkBox_channel_tell.Name = "checkBox_channel_tell";
			this.checkBox_channel_tell.Size = new System.Drawing.Size(43, 17);
			this.checkBox_channel_tell.TabIndex = 6;
			this.checkBox_channel_tell.Text = "Tell";
			this.checkBox_channel_tell.UseVisualStyleBackColor = true;
			// 
			// textBox_tts_regex
			// 
			this.textBox_tts_regex.Location = new System.Drawing.Point(13, 184);
			this.textBox_tts_regex.Name = "textBox_tts_regex";
			this.textBox_tts_regex.Size = new System.Drawing.Size(456, 20);
			this.textBox_tts_regex.TabIndex = 7;
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(14, 159);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(131, 13);
			this.label1.TabIndex = 8;
			this.label1.Text = "Regular Expression Match";
			// 
			// trackBar_tts_volume
			// 
			this.trackBar_tts_volume.Location = new System.Drawing.Point(192, 25);
			this.trackBar_tts_volume.Maximum = 100;
			this.trackBar_tts_volume.Name = "trackBar_tts_volume";
			this.trackBar_tts_volume.Size = new System.Drawing.Size(277, 45);
			this.trackBar_tts_volume.TabIndex = 9;
			this.trackBar_tts_volume.Value = 50;
			// 
			// label2
			// 
			this.label2.AutoSize = true;
			this.label2.Location = new System.Drawing.Point(204, 6);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(66, 13);
			this.label2.TabIndex = 10;
			this.label2.Text = "TTS Volume";
			// 
			// trackBar_tts_speed
			// 
			this.trackBar_tts_speed.Location = new System.Drawing.Point(192, 76);
			this.trackBar_tts_speed.Minimum = -10;
			this.trackBar_tts_speed.Name = "trackBar_tts_speed";
			this.trackBar_tts_speed.Size = new System.Drawing.Size(277, 45);
			this.trackBar_tts_speed.TabIndex = 11;
			// 
			// label3
			// 
			this.label3.AutoSize = true;
			this.label3.Location = new System.Drawing.Point(204, 57);
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size(62, 13);
			this.label3.TabIndex = 12;
			this.label3.Text = "TTS Speed";
			// 
			// comboBox_tts_voices
			// 
			this.comboBox_tts_voices.FormattingEnabled = true;
			this.comboBox_tts_voices.Location = new System.Drawing.Point(192, 133);
			this.comboBox_tts_voices.Name = "comboBox_tts_voices";
			this.comboBox_tts_voices.Size = new System.Drawing.Size(277, 21);
			this.comboBox_tts_voices.TabIndex = 13;
			// 
			// label4
			// 
			this.label4.AutoSize = true;
			this.label4.Location = new System.Drawing.Point(189, 117);
			this.label4.Name = "label4";
			this.label4.Size = new System.Drawing.Size(34, 13);
			this.label4.TabIndex = 14;
			this.label4.Text = "Voice";
			// 
			// checkBox_tts_enabled
			// 
			this.checkBox_tts_enabled.AutoSize = true;
			this.checkBox_tts_enabled.Location = new System.Drawing.Point(17, 135);
			this.checkBox_tts_enabled.Name = "checkBox_tts_enabled";
			this.checkBox_tts_enabled.Size = new System.Drawing.Size(65, 17);
			this.checkBox_tts_enabled.TabIndex = 15;
			this.checkBox_tts_enabled.Text = "Enabled";
			this.checkBox_tts_enabled.UseVisualStyleBackColor = true;
			// 
			// buttonOK
			// 
			this.buttonOK.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.buttonOK.Location = new System.Drawing.Point(117, 213);
			this.buttonOK.Name = "buttonOK";
			this.buttonOK.Size = new System.Drawing.Size(87, 31);
			this.buttonOK.TabIndex = 16;
			this.buttonOK.Text = "OK";
			this.buttonOK.UseVisualStyleBackColor = true;
			this.buttonOK.Click += new System.EventHandler(this.buttonOK_Click);
			// 
			// button1
			// 
			this.button1.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.button1.Location = new System.Drawing.Point(268, 213);
			this.button1.Name = "button1";
			this.button1.Size = new System.Drawing.Size(87, 31);
			this.button1.TabIndex = 17;
			this.button1.Text = "Cancel";
			this.button1.UseVisualStyleBackColor = true;
			this.button1.Click += new System.EventHandler(this.button1_Click);
			// 
			// TTSConfig
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(510, 260);
			this.Controls.Add(this.button1);
			this.Controls.Add(this.buttonOK);
			this.Controls.Add(this.checkBox_tts_enabled);
			this.Controls.Add(this.label4);
			this.Controls.Add(this.comboBox_tts_voices);
			this.Controls.Add(this.label3);
			this.Controls.Add(this.trackBar_tts_speed);
			this.Controls.Add(this.label2);
			this.Controls.Add(this.trackBar_tts_volume);
			this.Controls.Add(this.label1);
			this.Controls.Add(this.textBox_tts_regex);
			this.Controls.Add(this.groupBox1);
			this.Name = "TTSConfig";
			this.Text = "TTSConfig";
			this.groupBox1.ResumeLayout(false);
			this.groupBox1.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this.trackBar_tts_volume)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.trackBar_tts_speed)).EndInit();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion
		private System.Windows.Forms.GroupBox groupBox1;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.Label label3;
		private System.Windows.Forms.Label label4;
		public System.Windows.Forms.ComboBox comboBox_tts_voices;
		private System.Windows.Forms.Button buttonOK;
		private System.Windows.Forms.Button button1;
		public System.Windows.Forms.CheckBox checkBox_channel_ooc;
		public System.Windows.Forms.CheckBox checkBox_channel_guild;
		public System.Windows.Forms.CheckBox checkBox_channel_gsay;
		public System.Windows.Forms.CheckBox checkBox_channel_raid;
		public System.Windows.Forms.CheckBox checkBox_channel_say;
		public System.Windows.Forms.CheckBox checkBox_channel_auction;
		public System.Windows.Forms.TextBox textBox_tts_regex;
		public System.Windows.Forms.CheckBox checkBox_tts_enabled;
		public System.Windows.Forms.CheckBox checkBox_channel_tell;
		public System.Windows.Forms.TrackBar trackBar_tts_volume;
		public System.Windows.Forms.TrackBar trackBar_tts_speed;
	}
}