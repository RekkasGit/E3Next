using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.Integration;

namespace E3NextConfigEditor
{
	public partial class ConfigTextViewer : Krypton.Toolkit.KryptonForm
	{
		ICSharpCode.AvalonEdit.TextEditor _textEditor = new ICSharpCode.AvalonEdit.TextEditor();
		public ConfigTextViewer()
		{
			InitializeComponent();
			
			_textEditor.ShowLineNumbers = true;
			_textEditor.FontFamily = new System.Windows.Media.FontFamily("Consolas");
			_textEditor.FontSize = 14f;

			//WPF context menu
			System.Windows.Controls.ContextMenu menu = new System.Windows.Controls.ContextMenu();
			System.Windows.Controls.MenuItem item = new System.Windows.Controls.MenuItem();
			item.Header = "select spell";
			item.Click += Item_Click;
			menu.Items.Add(item);

			_textEditor.ContextMenu = menu;
			ElementHost host = new ElementHost();
			host.Dock = DockStyle.Fill;
			host.Child = _textEditor;
			this.Controls.Add(host);

		}

		private void Item_Click(object sender, EventArgs e)
		{
			var editor = ConfigEditor._spellEditor;
			ConfigEditor.InitEditor(ref editor, ConfigEditor._spellDataOrganized);

			if (editor.ShowDialog() == DialogResult.OK)
			{
				if (editor.SelectedSpell != null)
				{

					string textToReplace = _textEditor.SelectedText;
					string currentText = _textEditor.Text.Replace(textToReplace,editor.SelectedSpell.CastName);
					_textEditor.Text = currentText;

				}
			}
		}

		public String FileToShow
		{
			set
			{
				if (File.Exists(value))
				{
					_textEditor.Load(value);
				}
			}
		}

		private void textContextMenu_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
		{

		}
	}
}
