namespace E3NextSysTray.Forms
{
	partial class PatchNotes
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
			this.richTextNotes = new System.Windows.Forms.RichTextBox();
			this.SuspendLayout();
			// 
			// richTextNotes
			// 
			this.richTextNotes.Location = new System.Drawing.Point(12, 12);
			this.richTextNotes.Name = "richTextNotes";
			this.richTextNotes.ReadOnly = true;
			this.richTextNotes.Size = new System.Drawing.Size(776, 426);
			this.richTextNotes.TabIndex = 0;
			this.richTextNotes.TabStop = false;
			this.richTextNotes.Text = "";
			// 
			// PatchNotes
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(800, 450);
			this.Controls.Add(this.richTextNotes);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.Name = "PatchNotes";
			this.Text = "PathNotes";
			this.ResumeLayout(false);

		}

		#endregion

		public System.Windows.Forms.RichTextBox richTextNotes;
	}
}