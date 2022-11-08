using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace E3NextUI.Themese
{
    public static class DefaultMode
    {
        public static void ChangeTheme(Control.ControlCollection container)
        {
            foreach (Control component in container)
            {
                if (component is Panel)
                {
                    ChangeTheme(component.Controls);
                    component.BackColor = SystemColors.Control;
                    if (component.ForeColor == System.Drawing.Color.LightGray)
                    {
                        component.ForeColor = SystemColors.ControlText;
                    }
                }
                else if (component is Button)
                {
                    ChangeTheme(component.Controls);
                    component.BackColor = SystemColors.Control;
                    if (component.ForeColor == System.Drawing.Color.LightGray)
                    {
                        component.ForeColor = SystemColors.ControlText;
                    }
                }
                else if (component is Label)
                {
                    ChangeTheme(component.Controls);
                    component.BackColor = SystemColors.Control;
                    if (component.ForeColor == System.Drawing.Color.LightGray)
                    {
                        component.ForeColor = SystemColors.ControlText;
                    }
                }
            }

        }
    }
}
