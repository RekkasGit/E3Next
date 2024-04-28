namespace E3NextConfigEditor
{
	partial class AddSpellEditor
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
			this.spellTreeView = new ComponentFactory.Krypton.Toolkit.KryptonTreeView();
			this.addSpellPropertyGrid = new System.Windows.Forms.PropertyGrid();
			this.SuspendLayout();
			// 
			// spellTreeView
			// 
			this.spellTreeView.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left)));
			this.spellTreeView.Location = new System.Drawing.Point(12, 12);
			this.spellTreeView.Name = "spellTreeView";
			this.spellTreeView.Size = new System.Drawing.Size(361, 490);
			this.spellTreeView.TabIndex = 0;
			this.spellTreeView.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(this.spellTreeView_AfterSelect);
			// 
			// addSpellPropertyGrid
			// 
			this.addSpellPropertyGrid.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.addSpellPropertyGrid.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.addSpellPropertyGrid.Location = new System.Drawing.Point(379, 13);
			this.addSpellPropertyGrid.Name = "addSpellPropertyGrid";
			this.addSpellPropertyGrid.Size = new System.Drawing.Size(498, 489);
			this.addSpellPropertyGrid.TabIndex = 1;
			this.addSpellPropertyGrid.ToolbarVisible = false;
			// 
			// AddSpellEditor
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(889, 514);
			this.Controls.Add(this.addSpellPropertyGrid);
			this.Controls.Add(this.spellTreeView);
			this.Name = "AddSpellEditor";
			this.Text = "AddSpellEditor";
			this.ResumeLayout(false);

		}

		#endregion

		private ComponentFactory.Krypton.Toolkit.KryptonTreeView spellTreeView;
		private System.Windows.Forms.PropertyGrid addSpellPropertyGrid;
	}
}