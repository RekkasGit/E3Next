namespace E3NextConfigEditor
{
	partial class AddkeyValue
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
			this.keyTextBox = new System.Windows.Forms.TextBox();
			this.valueTextBox = new System.Windows.Forms.TextBox();
			this.keyLabel = new System.Windows.Forms.Label();
			this.valueLable = new System.Windows.Forms.Label();
			this.okButton = new Krypton.Toolkit.KryptonButton();
			this.cancelButton = new Krypton.Toolkit.KryptonButton();
			this.SuspendLayout();
			// 
			// keyTextBox
			// 
			this.keyTextBox.Font = new System.Drawing.Font("Microsoft Sans Serif", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.keyTextBox.Location = new System.Drawing.Point(40, 30);
			this.keyTextBox.Name = "keyTextBox";
			this.keyTextBox.Size = new System.Drawing.Size(469, 29);
			this.keyTextBox.TabIndex = 0;
			// 
			// valueTextBox
			// 
			this.valueTextBox.Font = new System.Drawing.Font("Microsoft Sans Serif", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.valueTextBox.Location = new System.Drawing.Point(40, 94);
			this.valueTextBox.Name = "valueTextBox";
			this.valueTextBox.Size = new System.Drawing.Size(469, 29);
			this.valueTextBox.TabIndex = 1;
			// 
			// keyLabel
			// 
			this.keyLabel.AutoSize = true;
			this.keyLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.keyLabel.Location = new System.Drawing.Point(36, 3);
			this.keyLabel.Name = "keyLabel";
			this.keyLabel.Size = new System.Drawing.Size(42, 24);
			this.keyLabel.TabIndex = 2;
			this.keyLabel.Text = "Key";
			// 
			// valueLable
			// 
			this.valueLable.AutoSize = true;
			this.valueLable.Font = new System.Drawing.Font("Microsoft Sans Serif", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.valueLable.Location = new System.Drawing.Point(36, 64);
			this.valueLable.Name = "valueLable";
			this.valueLable.Size = new System.Drawing.Size(59, 24);
			this.valueLable.TabIndex = 3;
			this.valueLable.Text = "Value";
			// 
			// okButton
			// 
			this.okButton.Location = new System.Drawing.Point(107, 135);
			this.okButton.Name = "okButton";
			this.okButton.Size = new System.Drawing.Size(90, 25);
			this.okButton.TabIndex = 4;
			this.okButton.Values.Text = "OK";
			this.okButton.Click += new System.EventHandler(this.okButton_Click);
			// 
			// cancelButton
			// 
			this.cancelButton.Location = new System.Drawing.Point(302, 135);
			this.cancelButton.Name = "cancelButton";
			this.cancelButton.Size = new System.Drawing.Size(90, 25);
			this.cancelButton.TabIndex = 5;
			this.cancelButton.Values.Text = "Cancel";
			this.cancelButton.Click += new System.EventHandler(this.cancelButton_Click);
			// 
			// AddkeyValue
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(538, 172);
			this.Controls.Add(this.cancelButton);
			this.Controls.Add(this.okButton);
			this.Controls.Add(this.valueLable);
			this.Controls.Add(this.keyLabel);
			this.Controls.Add(this.valueTextBox);
			this.Controls.Add(this.keyTextBox);
			this.Name = "AddkeyValue";
			this.PaletteMode = Krypton.Toolkit.PaletteMode.Office2010BlackDarkMode;
			this.Text = "AddkeyValue";
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.TextBox keyTextBox;
		private System.Windows.Forms.TextBox valueTextBox;
		private System.Windows.Forms.Label keyLabel;
		private System.Windows.Forms.Label valueLable;
		private Krypton.Toolkit.KryptonButton okButton;
		private Krypton.Toolkit.KryptonButton cancelButton;
	}
}