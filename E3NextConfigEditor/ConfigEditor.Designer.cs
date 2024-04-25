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
			this.kryptonListBox1 = new ComponentFactory.Krypton.Toolkit.KryptonListBox();
			this.SuspendLayout();
			// 
			// kryptonListBox1
			// 
			this.kryptonListBox1.Items.AddRange(new object[] {
            "Nukes",
            "Burns",
            "Heals"});
			this.kryptonListBox1.ItemStyle = ComponentFactory.Krypton.Toolkit.ButtonStyle.Gallery;
			this.kryptonListBox1.Location = new System.Drawing.Point(32, 13);
			this.kryptonListBox1.Name = "kryptonListBox1";
			this.kryptonListBox1.Size = new System.Drawing.Size(161, 377);
			this.kryptonListBox1.TabIndex = 0;
			// 
			// ConfigEditor
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(800, 450);
			this.Controls.Add(this.kryptonListBox1);
			this.Name = "ConfigEditor";
			this.Text = "Form1";
			this.ResumeLayout(false);

		}

		#endregion

		private ComponentFactory.Krypton.Toolkit.KryptonListBox kryptonListBox1;
	}
}

