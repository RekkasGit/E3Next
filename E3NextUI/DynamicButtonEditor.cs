using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace E3NextUI
{
    public partial class DynamicButtonEditor : Form
	{
		static List<string> keyboardKeys = new List<string>();
		static DynamicButtonEditor()
        {
	        //initlize the values from the enum
            foreach (Keys keyValue in (Keys[])Enum.GetValues(typeof(System.Windows.Forms.Keys)))
			{
                keyboardKeys.Add(keyValue.ToString());
			}

		}
		public DynamicButtonEditor()
        {
            InitializeComponent();


            comboBoxKeyValues.DataSource = keyboardKeys;

           
		}

		private void button1_Click(object sender, EventArgs e)
        {
            if(String.IsNullOrWhiteSpace(this.textBoxName.Text))
            {
                System.Windows.Forms.MessageBox.Show("Please enter a name or cancel");
                return;
            }

            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void buttonCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();

        }

		
	}
}
