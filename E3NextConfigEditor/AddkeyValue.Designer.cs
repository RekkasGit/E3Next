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
			this.label1 = new System.Windows.Forms.Label();
			this.label2 = new System.Windows.Forms.Label();
			this.okButton = new Krypton.Toolkit.KryptonButton();
			this.cancelButton = new Krypton.Toolkit.KryptonButton();
			this.SuspendLayout();
			// 
			// keyTextBox
			// 
			this.keyTextBox.Font = new System.Drawing.Font("Microsoft Sans Serif", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.keyTextBox.Location = new System.Drawing.Point(81, 30);
			this.keyTextBox.Name = "keyTextBox";
			this.keyTextBox.Size = new System.Drawing.Size(469, 29);
			this.keyTextBox.TabIndex = 0;
			// 
			// valueTextBox
			// 
			this.valueTextBox.Font = new System.Drawing.Font("Microsoft Sans Serif", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.valueTextBox.Location = new System.Drawing.Point(81, 76);
			this.valueTextBox.Name = "valueTextBox";
			this.valueTextBox.Size = new System.Drawing.Size(469, 29);
			this.valueTextBox.TabIndex = 1;
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.label1.Location = new System.Drawing.Point(12, 30);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(42, 24);
			this.label1.TabIndex = 2;
			this.label1.Text = "Key";
			// 
			// label2
			// 
			this.label2.AutoSize = true;
			this.label2.Font = new System.Drawing.Font("Microsoft Sans Serif", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.label2.Location = new System.Drawing.Point(12, 76);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(59, 24);
			this.label2.TabIndex = 3;
			this.label2.Text = "Value";
			// 
			// okButton
			// 
			this.okButton.Location = new System.Drawing.Point(149, 119);
			this.okButton.Name = "okButton";
			this.okButton.Size = new System.Drawing.Size(90, 25);
			this.okButton.TabIndex = 4;
			this.okButton.Values.Text = "OK";
			this.okButton.Click += new System.EventHandler(this.okButton_Click);
			// 
			// cancelButton
			// 
			this.cancelButton.Location = new System.Drawing.Point(345, 119);
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
			this.ClientSize = new System.Drawing.Size(602, 156);
			this.Controls.Add(this.cancelButton);
			this.Controls.Add(this.okButton);
			this.Controls.Add(this.label2);
			this.Controls.Add(this.label1);
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
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Label label2;
		private Krypton.Toolkit.KryptonButton okButton;
		private Krypton.Toolkit.KryptonButton cancelButton;
	}
}