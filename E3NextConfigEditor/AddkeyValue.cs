using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace E3NextConfigEditor
{
	public partial class AddkeyValue : Krypton.Toolkit.KryptonForm
	{
		public string Key = String.Empty;
		public string Value = String.Empty;

		public AddkeyValue()
		{
			InitializeComponent();
		}

		public void SetKeyLabel(string value)
		{
			keyLabel.Text = value;
		}
		public void SetValueLabel(string value)
		{
			valueLable.Text = value;
		}
		private void cancelButton_Click(object sender, EventArgs e)
		{
			this.DialogResult = DialogResult.Cancel;
			Close();
		}

		private void okButton_Click(object sender, EventArgs e)
		{
			if (!String.IsNullOrWhiteSpace(keyTextBox.Text) && !String.IsNullOrWhiteSpace(valueTextBox.Text))
			{
				Key = keyTextBox.Text;
				Value = valueTextBox.Text;
				this.DialogResult = DialogResult.OK;
				Close();
			}
		}
	}
}
