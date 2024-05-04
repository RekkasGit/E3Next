namespace E3NextConfigEditor
{
	partial class AddValue
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
			this.cancelButton = new Krypton.Toolkit.KryptonButton();
			this.okButton = new Krypton.Toolkit.KryptonButton();
			this.lableDescription = new System.Windows.Forms.Label();
			this.valueTextBox = new System.Windows.Forms.TextBox();
			this.SuspendLayout();
			// 
			// cancelButton
			// 
			this.cancelButton.Location = new System.Drawing.Point(286, 100);
			this.cancelButton.Name = "cancelButton";
			this.cancelButton.Size = new System.Drawing.Size(90, 25);
			this.cancelButton.TabIndex = 9;
			this.cancelButton.Values.Text = "Cancel";
			this.cancelButton.Click += new System.EventHandler(this.cancelButton_Click);
			// 
			// okButton
			// 
			this.okButton.Location = new System.Drawing.Point(90, 100);
			this.okButton.Name = "okButton";
			this.okButton.Size = new System.Drawing.Size(90, 25);
			this.okButton.TabIndex = 8;
			this.okButton.Values.Text = "OK";
			this.okButton.Click += new System.EventHandler(this.okButton_Click);
			// 
			// lableDescription
			// 
			this.lableDescription.AutoSize = true;
			this.lableDescription.Font = new System.Drawing.Font("Microsoft Sans Serif", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.lableDescription.Location = new System.Drawing.Point(179, 30);
			this.lableDescription.Name = "lableDescription";
			this.lableDescription.Size = new System.Drawing.Size(128, 24);
			this.lableDescription.TabIndex = 7;
			this.lableDescription.Text = "Melody Name";
			// 
			// valueTextBox
			// 
			this.valueTextBox.Font = new System.Drawing.Font("Microsoft Sans Serif", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.valueTextBox.Location = new System.Drawing.Point(22, 57);
			this.valueTextBox.Name = "valueTextBox";
			this.valueTextBox.Size = new System.Drawing.Size(469, 29);
			this.valueTextBox.TabIndex = 6;
			this.valueTextBox.KeyDown += new System.Windows.Forms.KeyEventHandler(this.valueTextBox_KeyDown);
			// 
			// AddValue
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(518, 159);
			this.Controls.Add(this.cancelButton);
			this.Controls.Add(this.okButton);
			this.Controls.Add(this.lableDescription);
			this.Controls.Add(this.valueTextBox);
			this.Name = "AddValue";
			this.PaletteMode = Krypton.Toolkit.PaletteMode.Office2010BlackDarkMode;
			this.Text = "AddValue";
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private Krypton.Toolkit.KryptonButton cancelButton;
		private Krypton.Toolkit.KryptonButton okButton;
		private System.Windows.Forms.TextBox valueTextBox;
		public System.Windows.Forms.Label lableDescription;
	}
}