
namespace E3NextUI
{
    partial class DynamicButtonEditor
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
			this.textBoxCommands = new System.Windows.Forms.TextBox();
			this.label1 = new System.Windows.Forms.Label();
			this.textBoxName = new System.Windows.Forms.TextBox();
			this.label2 = new System.Windows.Forms.Label();
			this.buttonOK = new System.Windows.Forms.Button();
			this.buttonCancel = new System.Windows.Forms.Button();
			this.label3 = new System.Windows.Forms.Label();
			this.checkBoxHotkeyAlt = new System.Windows.Forms.CheckBox();
			this.checkBoxHotkeyCtrl = new System.Windows.Forms.CheckBox();
			this.comboBoxKeyValues = new System.Windows.Forms.ComboBox();
			this.checkBoxHotkeyEat = new System.Windows.Forms.CheckBox();
			this.checkBoxHotkeyShift = new System.Windows.Forms.CheckBox();
			this.SuspendLayout();
			// 
			// textBoxCommands
			// 
			this.textBoxCommands.Location = new System.Drawing.Point(12, 118);
			this.textBoxCommands.Multiline = true;
			this.textBoxCommands.Name = "textBoxCommands";
			this.textBoxCommands.Size = new System.Drawing.Size(360, 272);
			this.textBoxCommands.TabIndex = 1;
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.label1.Location = new System.Drawing.Point(12, 28);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(66, 24);
			this.label1.TabIndex = 1;
			this.label1.Text = "Name:";
			// 
			// textBoxName
			// 
			this.textBoxName.Font = new System.Drawing.Font("Microsoft Sans Serif", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.textBoxName.Location = new System.Drawing.Point(84, 28);
			this.textBoxName.Name = "textBoxName";
			this.textBoxName.Size = new System.Drawing.Size(150, 29);
			this.textBoxName.TabIndex = 0;
			// 
			// label2
			// 
			this.label2.AutoSize = true;
			this.label2.Font = new System.Drawing.Font("Microsoft Sans Serif", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.label2.ForeColor = System.Drawing.Color.Red;
			this.label2.Location = new System.Drawing.Point(238, 25);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(18, 24);
			this.label2.TabIndex = 3;
			this.label2.Text = "*";
			// 
			// buttonOK
			// 
			this.buttonOK.Location = new System.Drawing.Point(47, 397);
			this.buttonOK.Name = "buttonOK";
			this.buttonOK.Size = new System.Drawing.Size(87, 31);
			this.buttonOK.TabIndex = 2;
			this.buttonOK.Text = "OK";
			this.buttonOK.UseVisualStyleBackColor = true;
			this.buttonOK.Click += new System.EventHandler(this.button1_Click);
			// 
			// buttonCancel
			// 
			this.buttonCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.buttonCancel.Location = new System.Drawing.Point(242, 397);
			this.buttonCancel.Name = "buttonCancel";
			this.buttonCancel.Size = new System.Drawing.Size(83, 31);
			this.buttonCancel.TabIndex = 3;
			this.buttonCancel.Text = "Cancel";
			this.buttonCancel.UseVisualStyleBackColor = true;
			this.buttonCancel.Click += new System.EventHandler(this.buttonCancel_Click);
			// 
			// label3
			// 
			this.label3.AutoSize = true;
			this.label3.Font = new System.Drawing.Font("Microsoft Sans Serif", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.label3.Location = new System.Drawing.Point(12, 64);
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size(73, 24);
			this.label3.TabIndex = 4;
			this.label3.Text = "Hotkey:";
			// 
			// checkBoxHotkeyAlt
			// 
			this.checkBoxHotkeyAlt.Location = new System.Drawing.Point(224, 63);
			this.checkBoxHotkeyAlt.Name = "checkBoxHotkeyAlt";
			this.checkBoxHotkeyAlt.Size = new System.Drawing.Size(53, 24);
			this.checkBoxHotkeyAlt.TabIndex = 6;
			this.checkBoxHotkeyAlt.Text = "Alt";
			this.checkBoxHotkeyAlt.UseVisualStyleBackColor = true;
			// 
			// checkBoxHotkeyCtrl
			// 
			this.checkBoxHotkeyCtrl.Location = new System.Drawing.Point(283, 63);
			this.checkBoxHotkeyCtrl.Name = "checkBoxHotkeyCtrl";
			this.checkBoxHotkeyCtrl.Size = new System.Drawing.Size(53, 24);
			this.checkBoxHotkeyCtrl.TabIndex = 7;
			this.checkBoxHotkeyCtrl.Text = "Ctrl";
			this.checkBoxHotkeyCtrl.UseVisualStyleBackColor = true;
			// 
			// comboBoxKeyValues
			// 
			this.comboBoxKeyValues.FormattingEnabled = true;
			this.comboBoxKeyValues.Location = new System.Drawing.Point(84, 66);
			this.comboBoxKeyValues.Name = "comboBoxKeyValues";
			this.comboBoxKeyValues.Size = new System.Drawing.Size(121, 21);
			this.comboBoxKeyValues.TabIndex = 8;
			// 
			// checkBoxHotkeyEat
			// 
			this.checkBoxHotkeyEat.Location = new System.Drawing.Point(283, 93);
			this.checkBoxHotkeyEat.Name = "checkBoxHotkeyEat";
			this.checkBoxHotkeyEat.Size = new System.Drawing.Size(53, 24);
			this.checkBoxHotkeyEat.TabIndex = 9;
			this.checkBoxHotkeyEat.Text = "Eat";
			this.checkBoxHotkeyEat.UseVisualStyleBackColor = true;
			// 
			// checkBoxHotkeyShift
			// 
			this.checkBoxHotkeyShift.Location = new System.Drawing.Point(224, 93);
			this.checkBoxHotkeyShift.Name = "checkBoxHotkeyShift";
			this.checkBoxHotkeyShift.Size = new System.Drawing.Size(53, 24);
			this.checkBoxHotkeyShift.TabIndex = 10;
			this.checkBoxHotkeyShift.Text = "Shift";
			this.checkBoxHotkeyShift.UseVisualStyleBackColor = true;
			// 
			// DynamicButtonEditor
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.buttonCancel;
			this.ClientSize = new System.Drawing.Size(385, 438);
			this.ControlBox = false;
			this.Controls.Add(this.checkBoxHotkeyShift);
			this.Controls.Add(this.checkBoxHotkeyEat);
			this.Controls.Add(this.comboBoxKeyValues);
			this.Controls.Add(this.checkBoxHotkeyCtrl);
			this.Controls.Add(this.checkBoxHotkeyAlt);
			this.Controls.Add(this.label3);
			this.Controls.Add(this.buttonCancel);
			this.Controls.Add(this.buttonOK);
			this.Controls.Add(this.label2);
			this.Controls.Add(this.textBoxName);
			this.Controls.Add(this.label1);
			this.Controls.Add(this.textBoxCommands);
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "DynamicButtonEditor";
			this.Text = "Edit Button";
			this.ResumeLayout(false);
			this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Button buttonOK;
        private System.Windows.Forms.Button buttonCancel;
        public System.Windows.Forms.TextBox textBoxCommands;
        public System.Windows.Forms.TextBox textBoxName;
		private System.Windows.Forms.Label label3;
		public System.Windows.Forms.CheckBox checkBoxHotkeyAlt;
		public System.Windows.Forms.CheckBox checkBoxHotkeyCtrl;
		public System.Windows.Forms.ComboBox comboBoxKeyValues;
		public System.Windows.Forms.CheckBox checkBoxHotkeyEat;
		public System.Windows.Forms.CheckBox checkBoxHotkeyShift;
	}
}