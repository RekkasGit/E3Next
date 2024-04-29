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
	public partial class AddMelody : Krypton.Toolkit.KryptonForm
	{
		public string Value = String.Empty;

		public AddMelody()
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
	}
}
