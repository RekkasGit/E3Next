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
			this.spellTreeView = new Krypton.Toolkit.KryptonTreeView();
			this.addSpellPropertyGrid = new System.Windows.Forms.PropertyGrid();
			this.addSpellButton = new Krypton.Toolkit.KryptonButton();
			this.cancelSpellButton = new Krypton.Toolkit.KryptonButton();
			this.searchTextBox = new System.Windows.Forms.TextBox();
			this.searchButton = new Krypton.Toolkit.KryptonButton();
			this.SuspendLayout();
			// 
			// spellTreeView
			// 
			this.spellTreeView.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left)));
			this.spellTreeView.ItemHeight = 22;
			this.spellTreeView.Location = new System.Drawing.Point(12, 13);
			this.spellTreeView.Name = "spellTreeView";
			this.spellTreeView.Size = new System.Drawing.Size(319, 616);
			this.spellTreeView.TabIndex = 0;
			this.spellTreeView.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(this.spellTreeView_AfterSelect);
			// 
			// addSpellPropertyGrid
			// 
			this.addSpellPropertyGrid.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.addSpellPropertyGrid.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.addSpellPropertyGrid.Location = new System.Drawing.Point(337, 13);
			this.addSpellPropertyGrid.Name = "addSpellPropertyGrid";
			this.addSpellPropertyGrid.Size = new System.Drawing.Size(623, 582);
			this.addSpellPropertyGrid.TabIndex = 1;
			this.addSpellPropertyGrid.ToolbarVisible = false;
			this.addSpellPropertyGrid.SizeChanged += new System.EventHandler(this.addSpellPropertyGrid_SizeChanged);
			// 
			// addSpellButton
			// 
			this.addSpellButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.addSpellButton.Location = new System.Drawing.Point(347, 603);
			this.addSpellButton.Name = "addSpellButton";
			this.addSpellButton.Size = new System.Drawing.Size(90, 25);
			this.addSpellButton.TabIndex = 2;
			this.addSpellButton.Values.Text = "Add";
			this.addSpellButton.Click += new System.EventHandler(this.addSpellButton_Click);
			// 
			// cancelSpellButton
			// 
			this.cancelSpellButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.cancelSpellButton.Location = new System.Drawing.Point(458, 604);
			this.cancelSpellButton.Name = "cancelSpellButton";
			this.cancelSpellButton.Size = new System.Drawing.Size(90, 25);
			this.cancelSpellButton.TabIndex = 3;
			this.cancelSpellButton.Values.Text = "Cancel";
			this.cancelSpellButton.Click += new System.EventHandler(this.cancelSpellButton_Click);
			// 
			// searchTextBox
			// 
			this.searchTextBox.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.searchTextBox.Location = new System.Drawing.Point(568, 604);
			this.searchTextBox.Name = "searchTextBox";
			this.searchTextBox.Size = new System.Drawing.Size(298, 26);
			this.searchTextBox.TabIndex = 4;
			this.searchTextBox.KeyDown += new System.Windows.Forms.KeyEventHandler(this.searchTextBox_KeyDown);
			// 
			// searchButton
			// 
			this.searchButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.searchButton.Location = new System.Drawing.Point(872, 605);
			this.searchButton.Name = "searchButton";
			this.searchButton.Size = new System.Drawing.Size(90, 25);
			this.searchButton.TabIndex = 5;
			this.searchButton.Values.Text = "Search";
			this.searchButton.Click += new System.EventHandler(this.searchButton_Click);
			// 
			// AddSpellEditor
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(972, 641);
			this.Controls.Add(this.searchButton);
			this.Controls.Add(this.searchTextBox);
			this.Controls.Add(this.cancelSpellButton);
			this.Controls.Add(this.addSpellButton);
			this.Controls.Add(this.addSpellPropertyGrid);
			this.Controls.Add(this.spellTreeView);
			this.Name = "AddSpellEditor";
			this.PaletteMode = Krypton.Toolkit.PaletteMode.Office2010BlackDarkMode;
			this.Text = "Add Spell/Disc/AA Editor";
			this.Load += new System.EventHandler(this.AddSpellEditor_Load);
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private Krypton.Toolkit.KryptonTreeView spellTreeView;
		private System.Windows.Forms.PropertyGrid addSpellPropertyGrid;
		private Krypton.Toolkit.KryptonButton addSpellButton;
		private Krypton.Toolkit.KryptonButton cancelSpellButton;
		private System.Windows.Forms.TextBox searchTextBox;
		private Krypton.Toolkit.KryptonButton searchButton;
	}
}