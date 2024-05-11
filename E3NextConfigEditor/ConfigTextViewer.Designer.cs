namespace E3NextConfigEditor
{
	partial class ConfigTextViewer
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
			this.components = new System.ComponentModel.Container();
			this.toolStripMenuItem1 = new System.Windows.Forms.ToolStripMenuItem();
			this.textContextMenu = new System.Windows.Forms.ContextMenuStrip(this.components);
			this.textContextMenu.SuspendLayout();
			this.SuspendLayout();
			// 
			// toolStripMenuItem1
			// 
			this.toolStripMenuItem1.Name = "toolStripMenuItem1";
			this.toolStripMenuItem1.Size = new System.Drawing.Size(180, 22);
			this.toolStripMenuItem1.Text = "test_item";
			// 
			// textContextMenu
			// 
			this.textContextMenu.Font = new System.Drawing.Font("Segoe UI", 9F);
			this.textContextMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripMenuItem1});
			this.textContextMenu.Name = "textContextMenu";
			this.textContextMenu.Size = new System.Drawing.Size(181, 48);
			this.textContextMenu.ItemClicked += new System.Windows.Forms.ToolStripItemClickedEventHandler(this.textContextMenu_ItemClicked);
			// 
			// ConfigTextViewer
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(800, 450);
			this.Name = "ConfigTextViewer";
			this.PaletteMode = Krypton.Toolkit.PaletteMode.Office2010BlackDarkMode;
			this.Text = "Text Viewer";
			this.textContextMenu.ResumeLayout(false);
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem1;
		private System.Windows.Forms.ContextMenuStrip textContextMenu;
	}
}