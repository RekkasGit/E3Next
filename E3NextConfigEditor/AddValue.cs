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
	public partial class AddValue : Krypton.Toolkit.KryptonForm
	{
		public string Value = String.Empty;

		public AddValue()
		{
			InitializeComponent();
		}

		private void cancelButton_Click(object sender, EventArgs e)
		{
			this.DialogResult = DialogResult.Cancel;
			Close();
		}

		private void okButton_Click(object sender, EventArgs e)
		{
			if (!String.IsNullOrWhiteSpace(valueTextBox.Text))
			{
				Value = valueTextBox.Text;
				this.DialogResult = DialogResult.OK;
				Close();
			}
		}

		private void valueTextBox_KeyDown(object sender, KeyEventArgs e)
		{
			if (!String.IsNullOrWhiteSpace(valueTextBox.Text))
			{
				if (e.KeyCode == Keys.Enter)
				{
					e.SuppressKeyPress = true;
					Value = valueTextBox.Text;
					this.DialogResult = DialogResult.OK;
					Close();

				}
				e.Handled = true;
			}
		}
	}
}
