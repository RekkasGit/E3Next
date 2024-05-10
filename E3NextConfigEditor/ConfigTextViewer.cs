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
		
			ElementHost host = new ElementHost();
			host.Dock = DockStyle.Fill;
			host.Child = _textEditor;
			this.Controls.Add(host);

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
	}
}
