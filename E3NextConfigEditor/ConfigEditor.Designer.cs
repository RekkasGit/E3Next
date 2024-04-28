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
			this.sectionComboBox = new ComponentFactory.Krypton.Toolkit.KryptonComboBox();
			this.sectionComboBox_ButtonSpecAny1 = new ComponentFactory.Krypton.Toolkit.ButtonSpecAny();
			this.sectionComboBox_ButtonSpecAny2 = new ComponentFactory.Krypton.Toolkit.ButtonSpecAny();
			this.subsectionComboBox = new ComponentFactory.Krypton.Toolkit.KryptonComboBox();
			this.subsectionComboBox_buttonSpecAny1 = new ComponentFactory.Krypton.Toolkit.ButtonSpecAny();
			this.subsectionComboBox_buttonSpecAny2 = new ComponentFactory.Krypton.Toolkit.ButtonSpecAny();
			this.valuesListBox = new ComponentFactory.Krypton.Toolkit.KryptonListBox();
			this.propertyGrid = new System.Windows.Forms.PropertyGrid();
			this.kryptonContextMenu1 = new ComponentFactory.Krypton.Toolkit.KryptonContextMenu();
			this.kryptonContextMenuItems1 = new ComponentFactory.Krypton.Toolkit.KryptonContextMenuItems();
			this.kryptonContextMenuItems2 = new ComponentFactory.Krypton.Toolkit.KryptonContextMenuItems();
			this.kryptonContextMenuItem1 = new ComponentFactory.Krypton.Toolkit.KryptonContextMenuItem();
			this.kryptonContextMenuItems3 = new ComponentFactory.Krypton.Toolkit.KryptonContextMenuItems();
			this.kryptonContextMenuItem2 = new ComponentFactory.Krypton.Toolkit.KryptonContextMenuItem();
			this.kryptonContextMenuItem3 = new ComponentFactory.Krypton.Toolkit.KryptonContextMenuItem();
			this.kryptonContextMenuItem4 = new ComponentFactory.Krypton.Toolkit.KryptonContextMenuItem();
			this.kryptonContextMenuItem5 = new ComponentFactory.Krypton.Toolkit.KryptonContextMenuItem();
			this.kryptonContextMenuSeparator1 = new ComponentFactory.Krypton.Toolkit.KryptonContextMenuSeparator();
			this.kryptonContextMenuHeading1 = new ComponentFactory.Krypton.Toolkit.KryptonContextMenuHeading();
			this.kryptonCommand_Delete = new ComponentFactory.Krypton.Toolkit.KryptonCommand();
			((System.ComponentModel.ISupportInitialize)(this.sectionComboBox)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.subsectionComboBox)).BeginInit();
			this.SuspendLayout();
			// 
			// sectionComboBox
			// 
			this.sectionComboBox.ButtonSpecs.AddRange(new ComponentFactory.Krypton.Toolkit.ButtonSpecAny[] {
            this.sectionComboBox_ButtonSpecAny1,
            this.sectionComboBox_ButtonSpecAny2});
			this.sectionComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.sectionComboBox.DropDownWidth = 131;
			this.sectionComboBox.ItemStyle = ComponentFactory.Krypton.Toolkit.ButtonStyle.Standalone;
			this.sectionComboBox.Location = new System.Drawing.Point(12, 12);
			this.sectionComboBox.Name = "sectionComboBox";
			this.sectionComboBox.PaletteMode = ComponentFactory.Krypton.Toolkit.PaletteMode.Office2010Black;
			this.sectionComboBox.Size = new System.Drawing.Size(288, 21);
			this.sectionComboBox.TabIndex = 0;
			this.sectionComboBox.SelectedIndexChanged += new System.EventHandler(this.sectionComboBox_SelectedIndexChanged);
			// 
			// sectionComboBox_ButtonSpecAny1
			// 
			this.sectionComboBox_ButtonSpecAny1.Type = ComponentFactory.Krypton.Toolkit.PaletteButtonSpecStyle.ArrowLeft;
			this.sectionComboBox_ButtonSpecAny1.UniqueName = "07150ABD249746AA3BBF969B010258EE";
			this.sectionComboBox_ButtonSpecAny1.Click += new System.EventHandler(this.sectionComboBox_ButtonSpecAny1_Click);
			// 
			// sectionComboBox_ButtonSpecAny2
			// 
			this.sectionComboBox_ButtonSpecAny2.Type = ComponentFactory.Krypton.Toolkit.PaletteButtonSpecStyle.ArrowRight;
			this.sectionComboBox_ButtonSpecAny2.UniqueName = "22E191AD9E654F4EDC85CBB96002CD5E";
			this.sectionComboBox_ButtonSpecAny2.Click += new System.EventHandler(this.sectionComboBox_ButtonSpecAny2_Click);
			// 
			// subsectionComboBox
			// 
			this.subsectionComboBox.ButtonSpecs.AddRange(new ComponentFactory.Krypton.Toolkit.ButtonSpecAny[] {
            this.subsectionComboBox_buttonSpecAny1,
            this.subsectionComboBox_buttonSpecAny2});
			this.subsectionComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.subsectionComboBox.DropDownWidth = 131;
			this.subsectionComboBox.ItemStyle = ComponentFactory.Krypton.Toolkit.ButtonStyle.Standalone;
			this.subsectionComboBox.Location = new System.Drawing.Point(12, 39);
			this.subsectionComboBox.Name = "subsectionComboBox";
			this.subsectionComboBox.PaletteMode = ComponentFactory.Krypton.Toolkit.PaletteMode.Office2010Black;
			this.subsectionComboBox.Size = new System.Drawing.Size(288, 21);
			this.subsectionComboBox.TabIndex = 1;
			this.subsectionComboBox.SelectedIndexChanged += new System.EventHandler(this.subsectionComboBox_SelectedIndexChanged);
			// 
			// subsectionComboBox_buttonSpecAny1
			// 
			this.subsectionComboBox_buttonSpecAny1.Type = ComponentFactory.Krypton.Toolkit.PaletteButtonSpecStyle.ArrowLeft;
			this.subsectionComboBox_buttonSpecAny1.UniqueName = "07150ABD249746AA3BBF969B010258EE";
			this.subsectionComboBox_buttonSpecAny1.Click += new System.EventHandler(this.subsectionComboBox_buttonSpecAny1_Click);
			// 
			// subsectionComboBox_buttonSpecAny2
			// 
			this.subsectionComboBox_buttonSpecAny2.Type = ComponentFactory.Krypton.Toolkit.PaletteButtonSpecStyle.ArrowRight;
			this.subsectionComboBox_buttonSpecAny2.UniqueName = "22E191AD9E654F4EDC85CBB96002CD5E";
			this.subsectionComboBox_buttonSpecAny2.Click += new System.EventHandler(this.subsectionComboBox_buttonSpecAny2_Click);
			// 
			// valuesListBox
			// 
			this.valuesListBox.AllowDrop = true;
			this.valuesListBox.KryptonContextMenu = this.kryptonContextMenu1;
			this.valuesListBox.Location = new System.Drawing.Point(13, 81);
			this.valuesListBox.Name = "valuesListBox";
			this.valuesListBox.Size = new System.Drawing.Size(287, 357);
			this.valuesListBox.TabIndex = 2;
			this.valuesListBox.SelectedIndexChanged += new System.EventHandler(this.valuesListBox_SelectedIndexChanged);
			this.valuesListBox.DragDrop += new System.Windows.Forms.DragEventHandler(this.valuesListBox_DragDrop);
			this.valuesListBox.DragOver += new System.Windows.Forms.DragEventHandler(this.valuesListBox_DragOver);
			this.valuesListBox.MouseDown += new System.Windows.Forms.MouseEventHandler(this.valuesListBox_MouseDown);
			this.valuesListBox.MouseHover += new System.EventHandler(this.valuesListBox_MouseHover);
			this.valuesListBox.MouseMove += new System.Windows.Forms.MouseEventHandler(this.valuesListBox_MouseMove);
			this.valuesListBox.MouseUp += new System.Windows.Forms.MouseEventHandler(this.valuesListBox_MouseUp);
			// 
			// propertyGrid
			// 
			this.propertyGrid.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.propertyGrid.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.propertyGrid.Location = new System.Drawing.Point(306, 12);
			this.propertyGrid.Name = "propertyGrid";
			this.propertyGrid.Size = new System.Drawing.Size(482, 481);
			this.propertyGrid.TabIndex = 3;
			this.propertyGrid.ToolbarVisible = false;
			// 
			// kryptonContextMenu1
			// 
			this.kryptonContextMenu1.Items.AddRange(new ComponentFactory.Krypton.Toolkit.KryptonContextMenuItemBase[] {
            this.kryptonContextMenuItems1});
			this.kryptonContextMenu1.PaletteMode = ComponentFactory.Krypton.Toolkit.PaletteMode.SparkleBlue;
			this.kryptonContextMenu1.Opening += new System.ComponentModel.CancelEventHandler(this.kryptonContextMenu1_Opening);
			// 
			// kryptonContextMenuItems1
			// 
			this.kryptonContextMenuItems1.Items.AddRange(new ComponentFactory.Krypton.Toolkit.KryptonContextMenuItemBase[] {
            this.kryptonContextMenuItem2,
            this.kryptonContextMenuItem3,
            this.kryptonContextMenuItem4,
            this.kryptonContextMenuSeparator1,
            this.kryptonContextMenuItem5});
			// 
			// kryptonContextMenuItem1
			// 
			this.kryptonContextMenuItem1.Text = "Menu Item";
			// 
			// kryptonContextMenuItem2
			// 
			this.kryptonContextMenuItem2.Text = "Add Spell";
			// 
			// kryptonContextMenuItem3
			// 
			this.kryptonContextMenuItem3.Text = "Add AA";
			// 
			// kryptonContextMenuItem4
			// 
			this.kryptonContextMenuItem4.Text = "Add Disc";
			// 
			// kryptonContextMenuItem5
			// 
			this.kryptonContextMenuItem5.KryptonCommand = this.kryptonCommand_Delete;
			this.kryptonContextMenuItem5.Text = "Delete";
			// 
			// kryptonContextMenuHeading1
			// 
			this.kryptonContextMenuHeading1.ExtraText = "";
			// 
			// kryptonCommand_Delete
			// 
			this.kryptonCommand_Delete.Text = "Delete";
			this.kryptonCommand_Delete.Execute += new System.EventHandler(this.kryptonCommand_Delete_Execute);
			// 
			// ConfigEditor
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(800, 505);
			this.Controls.Add(this.propertyGrid);
			this.Controls.Add(this.valuesListBox);
			this.Controls.Add(this.subsectionComboBox);
			this.Controls.Add(this.sectionComboBox);
			this.DoubleBuffered = true;
			this.Name = "ConfigEditor";
			this.Text = "Form1";
			this.Load += new System.EventHandler(this.ConfigEditor_Load);
			((System.ComponentModel.ISupportInitialize)(this.sectionComboBox)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.subsectionComboBox)).EndInit();
			this.ResumeLayout(false);

		}

		#endregion

		private ComponentFactory.Krypton.Toolkit.KryptonComboBox sectionComboBox;
		private ComponentFactory.Krypton.Toolkit.ButtonSpecAny sectionComboBox_ButtonSpecAny1;
		private ComponentFactory.Krypton.Toolkit.ButtonSpecAny sectionComboBox_ButtonSpecAny2;
		private ComponentFactory.Krypton.Toolkit.KryptonComboBox subsectionComboBox;
		private ComponentFactory.Krypton.Toolkit.ButtonSpecAny subsectionComboBox_buttonSpecAny1;
		private ComponentFactory.Krypton.Toolkit.ButtonSpecAny subsectionComboBox_buttonSpecAny2;
		private ComponentFactory.Krypton.Toolkit.KryptonListBox valuesListBox;
		private System.Windows.Forms.PropertyGrid propertyGrid;
		private ComponentFactory.Krypton.Toolkit.KryptonContextMenu kryptonContextMenu1;
		private ComponentFactory.Krypton.Toolkit.KryptonContextMenuItems kryptonContextMenuItems1;
		private ComponentFactory.Krypton.Toolkit.KryptonContextMenuItem kryptonContextMenuItem2;
		private ComponentFactory.Krypton.Toolkit.KryptonContextMenuItem kryptonContextMenuItem3;
		private ComponentFactory.Krypton.Toolkit.KryptonContextMenuItem kryptonContextMenuItem4;
		private ComponentFactory.Krypton.Toolkit.KryptonContextMenuItem kryptonContextMenuItem5;
		private ComponentFactory.Krypton.Toolkit.KryptonContextMenuItems kryptonContextMenuItems2;
		private ComponentFactory.Krypton.Toolkit.KryptonContextMenuItem kryptonContextMenuItem1;
		private ComponentFactory.Krypton.Toolkit.KryptonContextMenuItems kryptonContextMenuItems3;
		private ComponentFactory.Krypton.Toolkit.KryptonContextMenuSeparator kryptonContextMenuSeparator1;
		private ComponentFactory.Krypton.Toolkit.KryptonContextMenuHeading kryptonContextMenuHeading1;
		private ComponentFactory.Krypton.Toolkit.KryptonCommand kryptonCommand_Delete;
	}
}

