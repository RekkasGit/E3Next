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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ConfigEditor));
			this.sectionComboBox = new Krypton.Toolkit.KryptonComboBox();
			this.sectionComboBox_ButtonSpecAny1 = new Krypton.Toolkit.ButtonSpecAny();
			this.sectionComboBox_ButtonSpecAny2 = new Krypton.Toolkit.ButtonSpecAny();
			this.subsectionComboBox = new Krypton.Toolkit.KryptonComboBox();
			this.subsectionComboBox_buttonSpecAny1 = new Krypton.Toolkit.ButtonSpecAny();
			this.subsectionComboBox_buttonSpecAny2 = new Krypton.Toolkit.ButtonSpecAny();
			this.valuesListBox = new Krypton.Toolkit.KryptonListBox();
			this.valueListContextMenu = new Krypton.Toolkit.KryptonContextMenu();
			this.kryptonContextMenuHeading2 = new Krypton.Toolkit.KryptonContextMenuHeading();
			this.kryptonContextMenuItems1 = new Krypton.Toolkit.KryptonContextMenuItems();
			this.kryptonContextMenuItem2 = new Krypton.Toolkit.KryptonContextMenuItem();
			this.valueList_AddSpell = new Krypton.Toolkit.KryptonCommand();
			this.kryptonContextMenuItem3 = new Krypton.Toolkit.KryptonContextMenuItem();
			this.valueList_AddAA = new Krypton.Toolkit.KryptonCommand();
			this.kryptonContextMenuItem10 = new Krypton.Toolkit.KryptonContextMenuItem();
			this.valueList_AddItem = new Krypton.Toolkit.KryptonCommand();
			this.kryptonContextMenuItem4 = new Krypton.Toolkit.KryptonContextMenuItem();
			this.valueList_AddDisc = new Krypton.Toolkit.KryptonCommand();
			this.kryptonContextMenuItem7 = new Krypton.Toolkit.KryptonContextMenuItem();
			this.valueList_AddSkill = new Krypton.Toolkit.KryptonCommand();
			this.kryptonContextMenuItem6 = new Krypton.Toolkit.KryptonContextMenuItem();
			this.valueList_AddKeyValue = new Krypton.Toolkit.KryptonCommand();
			this.kryptonContextMenuItem14 = new Krypton.Toolkit.KryptonContextMenuItem();
			this.valueList_AddValue = new Krypton.Toolkit.KryptonCommand();
			this.kryptonContextMenuItem8 = new Krypton.Toolkit.KryptonContextMenuItem();
			this.valueList_AddDynamicMelody = new Krypton.Toolkit.KryptonCommand();
			this.kryptonContextMenuItem9 = new Krypton.Toolkit.KryptonContextMenuItem();
			this.valueList_AddMelodyIf = new Krypton.Toolkit.KryptonCommand();
			this.kryptonContextMenuItem11 = new Krypton.Toolkit.KryptonContextMenuItem();
			this.valueList_ReplaceSpell = new Krypton.Toolkit.KryptonCommand();
			this.kryptonContextMenuItem15 = new Krypton.Toolkit.KryptonContextMenuItem();
			this.valueList_CloneSpell = new Krypton.Toolkit.KryptonCommand();
			this.kryptonContextMenuItem5 = new Krypton.Toolkit.KryptonContextMenuItem();
			this.valueList_Delete = new Krypton.Toolkit.KryptonCommand();
			this.propertyGrid = new System.Windows.Forms.PropertyGrid();
			this.kryptonContextMenuItems2 = new Krypton.Toolkit.KryptonContextMenuItems();
			this.kryptonContextMenuItem1 = new Krypton.Toolkit.KryptonContextMenuItem();
			this.kryptonContextMenuItems3 = new Krypton.Toolkit.KryptonContextMenuItems();
			this.kryptonContextMenuHeading1 = new Krypton.Toolkit.KryptonContextMenuHeading();
			this.saveButton = new Krypton.Toolkit.KryptonButton();
			this.kryptonContextMenuItems4 = new Krypton.Toolkit.KryptonContextMenuItems();
			this.kryptonContextMenuItem12 = new Krypton.Toolkit.KryptonContextMenuItem();
			this.kryptonContextMenuItem13 = new Krypton.Toolkit.KryptonContextMenuItem();
			this.donateButton = new Krypton.Toolkit.KryptonButton();
			this.viewFileButton = new Krypton.Toolkit.KryptonButton();
			this.tvSection = new System.Windows.Forms.TreeView();
			this.label1 = new System.Windows.Forms.Label();
			this.label2 = new System.Windows.Forms.Label();
			this.labelClass = new System.Windows.Forms.Label();
			this.labelLevel = new System.Windows.Forms.Label();
			this.labelName = new System.Windows.Forms.Label();
			this.label4 = new System.Windows.Forms.Label();
			this.labelServer = new System.Windows.Forms.Label();
			this.label5 = new System.Windows.Forms.Label();
			((System.ComponentModel.ISupportInitialize)(this.sectionComboBox)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.subsectionComboBox)).BeginInit();
			this.SuspendLayout();
			// 
			// sectionComboBox
			// 
			this.sectionComboBox.ButtonSpecs.Add(this.sectionComboBox_ButtonSpecAny1);
			this.sectionComboBox.ButtonSpecs.Add(this.sectionComboBox_ButtonSpecAny2);
			this.sectionComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.sectionComboBox.DropDownWidth = 131;
			this.sectionComboBox.IntegralHeight = false;
			this.sectionComboBox.ItemStyle = Krypton.Toolkit.ButtonStyle.Standalone;
			this.sectionComboBox.Location = new System.Drawing.Point(565, 12);
			this.sectionComboBox.Name = "sectionComboBox";
			this.sectionComboBox.Size = new System.Drawing.Size(204, 21);
			this.sectionComboBox.TabIndex = 0;
			this.sectionComboBox.SelectedIndexChanged += new System.EventHandler(this.sectionComboBox_SelectedIndexChanged);
			// 
			// sectionComboBox_ButtonSpecAny1
			// 
			this.sectionComboBox_ButtonSpecAny1.Type = Krypton.Toolkit.PaletteButtonSpecStyle.ArrowLeft;
			this.sectionComboBox_ButtonSpecAny1.UniqueName = "07150ABD249746AA3BBF969B010258EE";
			this.sectionComboBox_ButtonSpecAny1.Click += new System.EventHandler(this.sectionComboBox_ButtonSpecAny1_Click);
			// 
			// sectionComboBox_ButtonSpecAny2
			// 
			this.sectionComboBox_ButtonSpecAny2.Type = Krypton.Toolkit.PaletteButtonSpecStyle.ArrowRight;
			this.sectionComboBox_ButtonSpecAny2.UniqueName = "22E191AD9E654F4EDC85CBB96002CD5E";
			this.sectionComboBox_ButtonSpecAny2.Click += new System.EventHandler(this.sectionComboBox_ButtonSpecAny2_Click);
			// 
			// subsectionComboBox
			// 
			this.subsectionComboBox.ButtonSpecs.Add(this.subsectionComboBox_buttonSpecAny1);
			this.subsectionComboBox.ButtonSpecs.Add(this.subsectionComboBox_buttonSpecAny2);
			this.subsectionComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.subsectionComboBox.DropDownWidth = 131;
			this.subsectionComboBox.IntegralHeight = false;
			this.subsectionComboBox.ItemStyle = Krypton.Toolkit.ButtonStyle.Standalone;
			this.subsectionComboBox.Location = new System.Drawing.Point(565, 39);
			this.subsectionComboBox.Name = "subsectionComboBox";
			this.subsectionComboBox.Size = new System.Drawing.Size(204, 21);
			this.subsectionComboBox.TabIndex = 1;
			this.subsectionComboBox.SelectedIndexChanged += new System.EventHandler(this.subsectionComboBox_SelectedIndexChanged);
			// 
			// subsectionComboBox_buttonSpecAny1
			// 
			this.subsectionComboBox_buttonSpecAny1.Type = Krypton.Toolkit.PaletteButtonSpecStyle.ArrowLeft;
			this.subsectionComboBox_buttonSpecAny1.UniqueName = "07150ABD249746AA3BBF969B010258EE";
			this.subsectionComboBox_buttonSpecAny1.Click += new System.EventHandler(this.subsectionComboBox_buttonSpecAny1_Click);
			// 
			// subsectionComboBox_buttonSpecAny2
			// 
			this.subsectionComboBox_buttonSpecAny2.Type = Krypton.Toolkit.PaletteButtonSpecStyle.ArrowRight;
			this.subsectionComboBox_buttonSpecAny2.UniqueName = "22E191AD9E654F4EDC85CBB96002CD5E";
			this.subsectionComboBox_buttonSpecAny2.Click += new System.EventHandler(this.subsectionComboBox_buttonSpecAny2_Click);
			// 
			// valuesListBox
			// 
			this.valuesListBox.AllowDrop = true;
			this.valuesListBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left)));
			this.valuesListBox.KryptonContextMenu = this.valueListContextMenu;
			this.valuesListBox.Location = new System.Drawing.Point(260, 66);
			this.valuesListBox.Name = "valuesListBox";
			this.valuesListBox.Size = new System.Drawing.Size(287, 447);
			this.valuesListBox.TabIndex = 2;
			this.valuesListBox.SelectedIndexChanged += new System.EventHandler(this.valuesListBox_SelectedIndexChanged);
			this.valuesListBox.DragDrop += new System.Windows.Forms.DragEventHandler(this.valuesListBox_DragDrop);
			this.valuesListBox.DragOver += new System.Windows.Forms.DragEventHandler(this.valuesListBox_DragOver);
			this.valuesListBox.MouseDown += new System.Windows.Forms.MouseEventHandler(this.valuesListBox_MouseDown);
			// 
			// valueListContextMenu
			// 
			this.valueListContextMenu.Items.AddRange(new Krypton.Toolkit.KryptonContextMenuItemBase[] {
            this.kryptonContextMenuHeading2,
            this.kryptonContextMenuItems1});
			this.valueListContextMenu.PaletteMode = Krypton.Toolkit.PaletteMode.Office2010BlackDarkMode;
			this.valueListContextMenu.StateDisabled.ItemShortcutText.ShortText.Color1 = System.Drawing.Color.Red;
			this.valueListContextMenu.StateDisabled.ItemShortcutText.ShortText.Color2 = System.Drawing.Color.Red;
			this.valueListContextMenu.StateDisabled.ItemTextAlternate.ShortText.Color1 = System.Drawing.Color.Red;
			this.valueListContextMenu.StateDisabled.ItemTextAlternate.ShortText.Color2 = System.Drawing.Color.Red;
			this.valueListContextMenu.StateDisabled.ItemTextStandard.LongText.Color1 = System.Drawing.Color.Red;
			this.valueListContextMenu.StateDisabled.ItemTextStandard.ShortText.Color1 = System.Drawing.Color.Red;
			this.valueListContextMenu.Opening += new System.ComponentModel.CancelEventHandler(this.valueListContextMenu_Opening);
			// 
			// kryptonContextMenuHeading2
			// 
			this.kryptonContextMenuHeading2.ExtraText = "";
			this.kryptonContextMenuHeading2.Text = "Options";
			// 
			// kryptonContextMenuItems1
			// 
			this.kryptonContextMenuItems1.Items.AddRange(new Krypton.Toolkit.KryptonContextMenuItemBase[] {
            this.kryptonContextMenuItem2,
            this.kryptonContextMenuItem3,
            this.kryptonContextMenuItem10,
            this.kryptonContextMenuItem4,
            this.kryptonContextMenuItem7,
            this.kryptonContextMenuItem6,
            this.kryptonContextMenuItem14,
            this.kryptonContextMenuItem8,
            this.kryptonContextMenuItem9,
            this.kryptonContextMenuItem11,
            this.kryptonContextMenuItem15,
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
			// kryptonContextMenuItem10
			// 
			this.kryptonContextMenuItem10.KryptonCommand = this.valueList_AddItem;
			this.kryptonContextMenuItem10.Text = "Add Item";
			// 
			// valueList_AddItem
			// 
			this.valueList_AddItem.Text = "Add Item";
			this.valueList_AddItem.Execute += new System.EventHandler(this.valueList_AddItem_Execute);
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
			// kryptonContextMenuItem7
			// 
			this.kryptonContextMenuItem7.KryptonCommand = this.valueList_AddSkill;
			this.kryptonContextMenuItem7.Text = "Add Skill";
			// 
			// valueList_AddSkill
			// 
			this.valueList_AddSkill.Text = "Add Skill";
			this.valueList_AddSkill.Execute += new System.EventHandler(this.valueList_AddSkill_Execute);
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
			// kryptonContextMenuItem14
			// 
			this.kryptonContextMenuItem14.KryptonCommand = this.valueList_AddValue;
			this.kryptonContextMenuItem14.Text = "Add Value";
			// 
			// valueList_AddValue
			// 
			this.valueList_AddValue.Text = "Add Value";
			this.valueList_AddValue.Execute += new System.EventHandler(this.valueList_AddValue_Execute);
			// 
			// kryptonContextMenuItem8
			// 
			this.kryptonContextMenuItem8.KryptonCommand = this.valueList_AddDynamicMelody;
			this.kryptonContextMenuItem8.Text = "Add Melody";
			// 
			// valueList_AddDynamicMelody
			// 
			this.valueList_AddDynamicMelody.Text = "Add Melody";
			this.valueList_AddDynamicMelody.Execute += new System.EventHandler(this.valueList_AddDynamicMelody_Execute);
			// 
			// kryptonContextMenuItem9
			// 
			this.kryptonContextMenuItem9.KryptonCommand = this.valueList_AddMelodyIf;
			this.kryptonContextMenuItem9.Text = "Add MelodyIf";
			// 
			// valueList_AddMelodyIf
			// 
			this.valueList_AddMelodyIf.Text = "Add MelodyIf";
			this.valueList_AddMelodyIf.Execute += new System.EventHandler(this.valueList_AddMelodyIf_Execute);
			// 
			// kryptonContextMenuItem11
			// 
			this.kryptonContextMenuItem11.KryptonCommand = this.valueList_ReplaceSpell;
			this.kryptonContextMenuItem11.Text = "Replace Spell";
			// 
			// valueList_ReplaceSpell
			// 
			this.valueList_ReplaceSpell.Text = "Replace Spell";
			this.valueList_ReplaceSpell.TextLine1 = "Copy data to new Spell";
			this.valueList_ReplaceSpell.Execute += new System.EventHandler(this.valueList_ReplaceSpell_Execute);
			// 
			// kryptonContextMenuItem15
			// 
			this.kryptonContextMenuItem15.KryptonCommand = this.valueList_CloneSpell;
			this.kryptonContextMenuItem15.Text = "Clone Spell";
			// 
			// valueList_CloneSpell
			// 
			this.valueList_CloneSpell.Text = "Clone Spell";
			this.valueList_CloneSpell.TextLine1 = "Copy data to new Spell";
			this.valueList_CloneSpell.Execute += new System.EventHandler(this.valueList_CloneSpell_Execute);
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
			this.propertyGrid.Location = new System.Drawing.Point(553, 66);
			this.propertyGrid.Name = "propertyGrid";
			this.propertyGrid.Size = new System.Drawing.Size(598, 447);
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
			// saveButton
			// 
			this.saveButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.saveButton.Location = new System.Drawing.Point(292, 519);
			this.saveButton.Name = "saveButton";
			this.saveButton.Size = new System.Drawing.Size(90, 25);
			this.saveButton.TabIndex = 4;
			this.saveButton.Values.Text = "Save";
			this.saveButton.Click += new System.EventHandler(this.saveButton_Click);
			// 
			// kryptonContextMenuItem12
			// 
			this.kryptonContextMenuItem12.Text = "Replace with Spell";
			// 
			// kryptonContextMenuItem13
			// 
			this.kryptonContextMenuItem13.Text = "Replace with AA";
			// 
			// donateButton
			// 
			this.donateButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.donateButton.Location = new System.Drawing.Point(1079, 519);
			this.donateButton.Name = "donateButton";
			this.donateButton.PaletteMode = Krypton.Toolkit.PaletteMode.Office2007BlackDarkMode;
			this.donateButton.Size = new System.Drawing.Size(72, 25);
			this.donateButton.TabIndex = 5;
			this.donateButton.Values.Text = "Donate";
			this.donateButton.Click += new System.EventHandler(this.donateButton_Click);
			// 
			// viewFileButton
			// 
			this.viewFileButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.viewFileButton.Location = new System.Drawing.Point(408, 519);
			this.viewFileButton.Name = "viewFileButton";
			this.viewFileButton.Size = new System.Drawing.Size(90, 25);
			this.viewFileButton.TabIndex = 6;
			this.viewFileButton.Values.Text = "View Text";
			this.viewFileButton.Click += new System.EventHandler(this.viewFileButton_Click);
			// 
			// tvSection
			// 
			this.tvSection.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left)));
			this.tvSection.Location = new System.Drawing.Point(16, 66);
			this.tvSection.Name = "tvSection";
			this.tvSection.Size = new System.Drawing.Size(238, 448);
			this.tvSection.TabIndex = 7;
			this.tvSection.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(this.tvSection_AfterSelect);
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.label1.Location = new System.Drawing.Point(256, 9);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(55, 24);
			this.label1.TabIndex = 8;
			this.label1.Text = "Class";
			// 
			// label2
			// 
			this.label2.AutoSize = true;
			this.label2.Font = new System.Drawing.Font("Microsoft Sans Serif", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.label2.Location = new System.Drawing.Point(256, 36);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(55, 24);
			this.label2.TabIndex = 9;
			this.label2.Text = "Level";
			// 
			// labelClass
			// 
			this.labelClass.AutoSize = true;
			this.labelClass.Font = new System.Drawing.Font("Microsoft Sans Serif", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.labelClass.ForeColor = System.Drawing.SystemColors.Highlight;
			this.labelClass.Location = new System.Drawing.Point(317, 9);
			this.labelClass.Name = "labelClass";
			this.labelClass.Size = new System.Drawing.Size(25, 24);
			this.labelClass.TabIndex = 10;
			this.labelClass.Text = "...";
			// 
			// labelLevel
			// 
			this.labelLevel.AutoSize = true;
			this.labelLevel.Font = new System.Drawing.Font("Microsoft Sans Serif", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.labelLevel.ForeColor = System.Drawing.SystemColors.Highlight;
			this.labelLevel.Location = new System.Drawing.Point(317, 36);
			this.labelLevel.Name = "labelLevel";
			this.labelLevel.Size = new System.Drawing.Size(25, 24);
			this.labelLevel.TabIndex = 11;
			this.labelLevel.Text = "...";
			// 
			// labelName
			// 
			this.labelName.AutoSize = true;
			this.labelName.Font = new System.Drawing.Font("Microsoft Sans Serif", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.labelName.ForeColor = System.Drawing.SystemColors.Highlight;
			this.labelName.Location = new System.Drawing.Point(73, 12);
			this.labelName.Name = "labelName";
			this.labelName.Size = new System.Drawing.Size(25, 24);
			this.labelName.TabIndex = 13;
			this.labelName.Text = "...";
			// 
			// label4
			// 
			this.label4.AutoSize = true;
			this.label4.Font = new System.Drawing.Font("Microsoft Sans Serif", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.label4.Location = new System.Drawing.Point(12, 12);
			this.label4.Name = "label4";
			this.label4.Size = new System.Drawing.Size(61, 24);
			this.label4.TabIndex = 12;
			this.label4.Text = "Name";
			// 
			// labelServer
			// 
			this.labelServer.AutoSize = true;
			this.labelServer.Font = new System.Drawing.Font("Microsoft Sans Serif", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.labelServer.ForeColor = System.Drawing.SystemColors.Highlight;
			this.labelServer.Location = new System.Drawing.Point(73, 39);
			this.labelServer.Name = "labelServer";
			this.labelServer.Size = new System.Drawing.Size(25, 24);
			this.labelServer.TabIndex = 15;
			this.labelServer.Text = "...";
			// 
			// label5
			// 
			this.label5.AutoSize = true;
			this.label5.Font = new System.Drawing.Font("Microsoft Sans Serif", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.label5.Location = new System.Drawing.Point(12, 39);
			this.label5.Name = "label5";
			this.label5.Size = new System.Drawing.Size(65, 24);
			this.label5.TabIndex = 14;
			this.label5.Text = "Server";
			// 
			// ConfigEditor
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.AutoScroll = true;
			this.ClientSize = new System.Drawing.Size(1191, 556);
			this.Controls.Add(this.labelServer);
			this.Controls.Add(this.label5);
			this.Controls.Add(this.labelName);
			this.Controls.Add(this.label4);
			this.Controls.Add(this.labelLevel);
			this.Controls.Add(this.labelClass);
			this.Controls.Add(this.label2);
			this.Controls.Add(this.label1);
			this.Controls.Add(this.tvSection);
			this.Controls.Add(this.viewFileButton);
			this.Controls.Add(this.donateButton);
			this.Controls.Add(this.saveButton);
			this.Controls.Add(this.propertyGrid);
			this.Controls.Add(this.valuesListBox);
			this.Controls.Add(this.subsectionComboBox);
			this.Controls.Add(this.sectionComboBox);
			this.DoubleBuffered = true;
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.Name = "ConfigEditor";
			this.PaletteMode = Krypton.Toolkit.PaletteMode.Office2010BlackDarkMode;
			this.Text = "Form1";
			this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.ConfigEditor_FormClosing);
			this.Load += new System.EventHandler(this.ConfigEditor_Load);
			((System.ComponentModel.ISupportInitialize)(this.sectionComboBox)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.subsectionComboBox)).EndInit();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private Krypton.Toolkit.KryptonComboBox sectionComboBox;
		private Krypton.Toolkit.ButtonSpecAny sectionComboBox_ButtonSpecAny1;
		private Krypton.Toolkit.ButtonSpecAny sectionComboBox_ButtonSpecAny2;
		private Krypton.Toolkit.KryptonComboBox subsectionComboBox;
		private Krypton.Toolkit.ButtonSpecAny subsectionComboBox_buttonSpecAny1;
		private Krypton.Toolkit.ButtonSpecAny subsectionComboBox_buttonSpecAny2;
		private Krypton.Toolkit.KryptonListBox valuesListBox;
		private System.Windows.Forms.PropertyGrid propertyGrid;
		private Krypton.Toolkit.KryptonContextMenu valueListContextMenu;
		private Krypton.Toolkit.KryptonContextMenuItems kryptonContextMenuItems1;
		private Krypton.Toolkit.KryptonContextMenuItem kryptonContextMenuItem2;
		private Krypton.Toolkit.KryptonContextMenuItem kryptonContextMenuItem3;
		private Krypton.Toolkit.KryptonContextMenuItem kryptonContextMenuItem4;
		private Krypton.Toolkit.KryptonContextMenuItem kryptonContextMenuItem5;
		private Krypton.Toolkit.KryptonContextMenuItems kryptonContextMenuItems2;
		private Krypton.Toolkit.KryptonContextMenuItem kryptonContextMenuItem1;
		private Krypton.Toolkit.KryptonContextMenuItems kryptonContextMenuItems3;
		private Krypton.Toolkit.KryptonContextMenuHeading kryptonContextMenuHeading1;
		private Krypton.Toolkit.KryptonCommand valueList_Delete;
		private Krypton.Toolkit.KryptonCommand valueList_AddSpell;
		private Krypton.Toolkit.KryptonCommand valueList_AddAA;
		private Krypton.Toolkit.KryptonCommand valueList_AddDisc;
		private Krypton.Toolkit.KryptonCommand valueList_AddKeyValue;
		private Krypton.Toolkit.KryptonContextMenuItem kryptonContextMenuItem6;
		private Krypton.Toolkit.KryptonContextMenuHeading kryptonContextMenuHeading2;
		private Krypton.Toolkit.KryptonCommand valueList_AddSkill;
		private Krypton.Toolkit.KryptonContextMenuItem kryptonContextMenuItem7;
		private Krypton.Toolkit.KryptonCommand valueList_AddMelodyIf;
		private Krypton.Toolkit.KryptonCommand valueList_AddDynamicMelody;
		private Krypton.Toolkit.KryptonContextMenuItem kryptonContextMenuItem8;
		private Krypton.Toolkit.KryptonContextMenuItem kryptonContextMenuItem9;
		private Krypton.Toolkit.KryptonButton saveButton;
		private Krypton.Toolkit.KryptonCommand valueList_AddItem;
		private Krypton.Toolkit.KryptonContextMenuItem kryptonContextMenuItem10;
		private Krypton.Toolkit.KryptonContextMenuItem kryptonContextMenuItem11;
		private Krypton.Toolkit.KryptonContextMenuItems kryptonContextMenuItems4;
		private Krypton.Toolkit.KryptonContextMenuItem kryptonContextMenuItem12;
		private Krypton.Toolkit.KryptonContextMenuItem kryptonContextMenuItem13;
		private Krypton.Toolkit.KryptonCommand valueList_ReplaceSpell;
		private Krypton.Toolkit.KryptonCommand valueList_AddValue;
		private Krypton.Toolkit.KryptonContextMenuItem kryptonContextMenuItem14;
		private Krypton.Toolkit.KryptonButton donateButton;
		private Krypton.Toolkit.KryptonButton viewFileButton;
		private Krypton.Toolkit.KryptonCommand valueList_CloneSpell;
		private Krypton.Toolkit.KryptonContextMenuItem kryptonContextMenuItem15;
        private System.Windows.Forms.TreeView tvSection;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label labelClass;
        private System.Windows.Forms.Label labelLevel;
        private System.Windows.Forms.Label labelName;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label labelServer;
        private System.Windows.Forms.Label label5;
    }
}

