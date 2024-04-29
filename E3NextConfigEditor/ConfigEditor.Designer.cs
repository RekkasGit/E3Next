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
			this.components = new System.ComponentModel.Container();
			this.sectionComboBox = new ComponentFactory.Krypton.Toolkit.KryptonComboBox();
			this.sectionComboBox_ButtonSpecAny1 = new ComponentFactory.Krypton.Toolkit.ButtonSpecAny();
			this.sectionComboBox_ButtonSpecAny2 = new ComponentFactory.Krypton.Toolkit.ButtonSpecAny();
			this.subsectionComboBox = new ComponentFactory.Krypton.Toolkit.KryptonComboBox();
			this.subsectionComboBox_buttonSpecAny1 = new ComponentFactory.Krypton.Toolkit.ButtonSpecAny();
			this.subsectionComboBox_buttonSpecAny2 = new ComponentFactory.Krypton.Toolkit.ButtonSpecAny();
			this.valuesListBox = new ComponentFactory.Krypton.Toolkit.KryptonListBox();
			this.valueListContextMenu = new ComponentFactory.Krypton.Toolkit.KryptonContextMenu();
			this.kryptonContextMenuItems1 = new ComponentFactory.Krypton.Toolkit.KryptonContextMenuItems();
			this.kryptonContextMenuItem2 = new ComponentFactory.Krypton.Toolkit.KryptonContextMenuItem();
			this.valueList_AddSpell = new ComponentFactory.Krypton.Toolkit.KryptonCommand();
			this.kryptonContextMenuItem3 = new ComponentFactory.Krypton.Toolkit.KryptonContextMenuItem();
			this.valueList_AddAA = new ComponentFactory.Krypton.Toolkit.KryptonCommand();
			this.kryptonContextMenuItem4 = new ComponentFactory.Krypton.Toolkit.KryptonContextMenuItem();
			this.valueList_AddDisc = new ComponentFactory.Krypton.Toolkit.KryptonCommand();
			this.kryptonContextMenuItem6 = new ComponentFactory.Krypton.Toolkit.KryptonContextMenuItem();
			this.valueList_AddKeyValue = new ComponentFactory.Krypton.Toolkit.KryptonCommand();
			this.kryptonContextMenuItem5 = new ComponentFactory.Krypton.Toolkit.KryptonContextMenuItem();
			this.valueList_Delete = new ComponentFactory.Krypton.Toolkit.KryptonCommand();
			this.propertyGrid = new System.Windows.Forms.PropertyGrid();
			this.kryptonContextMenuItems2 = new ComponentFactory.Krypton.Toolkit.KryptonContextMenuItems();
			this.kryptonContextMenuItem1 = new ComponentFactory.Krypton.Toolkit.KryptonContextMenuItem();
			this.kryptonContextMenuItems3 = new ComponentFactory.Krypton.Toolkit.KryptonContextMenuItems();
			this.kryptonContextMenuHeading1 = new ComponentFactory.Krypton.Toolkit.KryptonContextMenuHeading();
			this.kryptonBorderEdge2 = new ComponentFactory.Krypton.Toolkit.KryptonBorderEdge();
			this.kryptonPalette1 = new ComponentFactory.Krypton.Toolkit.KryptonPalette(this.components);
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
			this.valuesListBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left)));
			this.valuesListBox.KryptonContextMenu = this.valueListContextMenu;
			this.valuesListBox.Location = new System.Drawing.Point(13, 66);
			this.valuesListBox.Name = "valuesListBox";
			this.valuesListBox.Size = new System.Drawing.Size(287, 627);
			this.valuesListBox.TabIndex = 2;
			this.valuesListBox.SelectedIndexChanged += new System.EventHandler(this.valuesListBox_SelectedIndexChanged);
			this.valuesListBox.DragDrop += new System.Windows.Forms.DragEventHandler(this.valuesListBox_DragDrop);
			this.valuesListBox.DragOver += new System.Windows.Forms.DragEventHandler(this.valuesListBox_DragOver);
			this.valuesListBox.MouseDown += new System.Windows.Forms.MouseEventHandler(this.valuesListBox_MouseDown);
			this.valuesListBox.MouseHover += new System.EventHandler(this.valuesListBox_MouseHover);
			this.valuesListBox.MouseMove += new System.Windows.Forms.MouseEventHandler(this.valuesListBox_MouseMove);
			this.valuesListBox.MouseUp += new System.Windows.Forms.MouseEventHandler(this.valuesListBox_MouseUp);
			// 
			// valueListContextMenu
			// 
			this.valueListContextMenu.Items.AddRange(new ComponentFactory.Krypton.Toolkit.KryptonContextMenuItemBase[] {
            this.kryptonContextMenuItems1});
			this.valueListContextMenu.PaletteMode = ComponentFactory.Krypton.Toolkit.PaletteMode.SparkleBlue;
			this.valueListContextMenu.StateDisabled.ItemShortcutText.ShortText.Color1 = System.Drawing.Color.Red;
			this.valueListContextMenu.StateDisabled.ItemShortcutText.ShortText.Color2 = System.Drawing.Color.Red;
			this.valueListContextMenu.StateDisabled.ItemTextAlternate.ShortText.Color1 = System.Drawing.Color.Red;
			this.valueListContextMenu.StateDisabled.ItemTextAlternate.ShortText.Color2 = System.Drawing.Color.Red;
			this.valueListContextMenu.StateDisabled.ItemTextStandard.LongText.Color1 = System.Drawing.Color.Red;
			this.valueListContextMenu.StateDisabled.ItemTextStandard.ShortText.Color1 = System.Drawing.Color.Red;
			this.valueListContextMenu.Opening += new System.ComponentModel.CancelEventHandler(this.valueListContextMenu_Opening);
			// 
			// kryptonContextMenuItems1
			// 
			this.kryptonContextMenuItems1.Items.AddRange(new ComponentFactory.Krypton.Toolkit.KryptonContextMenuItemBase[] {
            this.kryptonContextMenuItem2,
            this.kryptonContextMenuItem3,
            this.kryptonContextMenuItem4,
            this.kryptonContextMenuItem6,
            this.kryptonContextMenuItem5});
			// 
			// kryptonContextMenuItem2
			// 
			this.kryptonContextMenuItem2.KryptonCommand = this.valueList_AddSpell;
			this.kryptonContextMenuItem2.Text = "Add Spell";
			// 
			// valueList_AddSpell
			// 
			this.valueList_AddSpell.Text = "Add Spell";
			this.valueList_AddSpell.Execute += new System.EventHandler(this.valueList_AddSpell_Execute);
			// 
			// kryptonContextMenuItem3
			// 
			this.kryptonContextMenuItem3.KryptonCommand = this.valueList_AddAA;
			this.kryptonContextMenuItem3.Text = "Add AA";
			// 
			// valueList_AddAA
			// 
			this.valueList_AddAA.Text = "Add AA";
			this.valueList_AddAA.Execute += new System.EventHandler(this.valueList_AddAA_Execute);
			// 
			// kryptonContextMenuItem4
			// 
			this.kryptonContextMenuItem4.KryptonCommand = this.valueList_AddDisc;
			this.kryptonContextMenuItem4.Text = "Add Disc";
			// 
			// valueList_AddDisc
			// 
			this.valueList_AddDisc.Text = "Add Disc";
			this.valueList_AddDisc.Execute += new System.EventHandler(this.valueList_AddDisc_Execute);
			// 
			// kryptonContextMenuItem6
			// 
			this.kryptonContextMenuItem6.KryptonCommand = this.valueList_AddKeyValue;
			this.kryptonContextMenuItem6.Text = "Add Key/Value";
			// 
			// valueList_AddKeyValue
			// 
			this.valueList_AddKeyValue.Text = "Add Key/Value";
			this.valueList_AddKeyValue.Execute += new System.EventHandler(this.valueList_AddKeyValue_Execute);
			// 
			// kryptonContextMenuItem5
			// 
			this.kryptonContextMenuItem5.KryptonCommand = this.valueList_Delete;
			this.kryptonContextMenuItem5.Text = "Delete";
			// 
			// valueList_Delete
			// 
			this.valueList_Delete.Text = "Delete";
			this.valueList_Delete.Execute += new System.EventHandler(this.valueList_Delete_Execute);
			// 
			// propertyGrid
			// 
			this.propertyGrid.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.propertyGrid.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.propertyGrid.Location = new System.Drawing.Point(306, 12);
			this.propertyGrid.Name = "propertyGrid";
			this.propertyGrid.Size = new System.Drawing.Size(993, 681);
			this.propertyGrid.TabIndex = 3;
			this.propertyGrid.ToolbarVisible = false;
			this.propertyGrid.SizeChanged += new System.EventHandler(this.propertyGrid_SizeChanged);
			// 
			// kryptonContextMenuItem1
			// 
			this.kryptonContextMenuItem1.Text = "Menu Item";
			// 
			// kryptonContextMenuHeading1
			// 
			this.kryptonContextMenuHeading1.ExtraText = "";
			// 
			// kryptonBorderEdge2
			// 
			this.kryptonBorderEdge2.Location = new System.Drawing.Point(0, 0);
			this.kryptonBorderEdge2.Name = "kryptonBorderEdge2";
			this.kryptonBorderEdge2.Size = new System.Drawing.Size(50, 1);
			this.kryptonBorderEdge2.Text = "kryptonBorderEdge2";
			// 
			// kryptonPalette1
			// 
			this.kryptonPalette1.ContextMenu.StateDisabled.ItemShortcutText.ShortText.Color1 = System.Drawing.Color.Red;
			this.kryptonPalette1.ContextMenu.StateDisabled.ItemShortcutText.ShortText.Color2 = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(128)))), ((int)(((byte)(0)))));
			this.kryptonPalette1.ContextMenu.StateDisabled.ItemTextAlternate.LongText.Color1 = System.Drawing.Color.Red;
			this.kryptonPalette1.ContextMenu.StateDisabled.ItemTextAlternate.LongText.Color2 = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(128)))), ((int)(((byte)(0)))));
			this.kryptonPalette1.ContextMenu.StateDisabled.ItemTextAlternate.ShortText.Color1 = System.Drawing.Color.Red;
			this.kryptonPalette1.ContextMenu.StateDisabled.ItemTextAlternate.ShortText.Color2 = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(128)))), ((int)(((byte)(0)))));
			this.kryptonPalette1.ContextMenu.StateDisabled.ItemTextStandard.ShortText.Color1 = System.Drawing.Color.Red;
			this.kryptonPalette1.ContextMenu.StateDisabled.ItemTextStandard.ShortText.Color2 = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(128)))), ((int)(((byte)(0)))));
			// 
			// ConfigEditor
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(1311, 705);
			this.Controls.Add(this.kryptonBorderEdge2);
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
			this.PerformLayout();

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
		private ComponentFactory.Krypton.Toolkit.KryptonContextMenu valueListContextMenu;
		private ComponentFactory.Krypton.Toolkit.KryptonContextMenuItems kryptonContextMenuItems1;
		private ComponentFactory.Krypton.Toolkit.KryptonContextMenuItem kryptonContextMenuItem2;
		private ComponentFactory.Krypton.Toolkit.KryptonContextMenuItem kryptonContextMenuItem3;
		private ComponentFactory.Krypton.Toolkit.KryptonContextMenuItem kryptonContextMenuItem4;
		private ComponentFactory.Krypton.Toolkit.KryptonContextMenuItem kryptonContextMenuItem5;
		private ComponentFactory.Krypton.Toolkit.KryptonContextMenuItems kryptonContextMenuItems2;
		private ComponentFactory.Krypton.Toolkit.KryptonContextMenuItem kryptonContextMenuItem1;
		private ComponentFactory.Krypton.Toolkit.KryptonContextMenuItems kryptonContextMenuItems3;
		private ComponentFactory.Krypton.Toolkit.KryptonContextMenuHeading kryptonContextMenuHeading1;
		private ComponentFactory.Krypton.Toolkit.KryptonCommand valueList_Delete;
		private ComponentFactory.Krypton.Toolkit.KryptonCommand valueList_AddSpell;
		private ComponentFactory.Krypton.Toolkit.KryptonBorderEdge kryptonBorderEdge2;
		private ComponentFactory.Krypton.Toolkit.KryptonCommand valueList_AddAA;
		private ComponentFactory.Krypton.Toolkit.KryptonCommand valueList_AddDisc;
		private ComponentFactory.Krypton.Toolkit.KryptonCommand valueList_AddKeyValue;
		private ComponentFactory.Krypton.Toolkit.KryptonContextMenuItem kryptonContextMenuItem6;
		private ComponentFactory.Krypton.Toolkit.KryptonPalette kryptonPalette1;
	}
}

