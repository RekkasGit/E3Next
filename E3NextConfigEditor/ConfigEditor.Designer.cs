namespace E3NextConfigEditor
{
	partial class ConfigEditor
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
			this.sectionListBox = new ComponentFactory.Krypton.Toolkit.KryptonListBox();
			this.SuspendLayout();
			// 
			// sectionListBox
			// 
			this.sectionListBox.Items.AddRange(new object[] {
            "Nukes",
            "Heals",
            "Burns"});
			this.sectionListBox.ItemStyle = ComponentFactory.Krypton.Toolkit.ButtonStyle.Standalone;
			this.sectionListBox.Location = new System.Drawing.Point(13, 13);
			this.sectionListBox.Name = "sectionListBox";
			this.sectionListBox.Size = new System.Drawing.Size(131, 425);
			this.sectionListBox.TabIndex = 0;
			// 
			// Form1
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(800, 450);
			this.Controls.Add(this.sectionListBox);
			this.Name = "Form1";
			this.Text = "Form1";
			this.ResumeLayout(false);

		}

		#endregion

		private ComponentFactory.Krypton.Toolkit.KryptonListBox sectionListBox;
	}
}

