using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace E3NextConfigEditor.Themese
{
    public static class DefaultMode
    {
        public static void ChangeTheme(System.Windows.Forms.Form form, Control.ControlCollection container)
        {
            Int32 enabled = 0;
            try
            {
                //old version of windows doesn't work
                if (DwmSetWindowAttribute(form.Handle, 19, new[] { enabled }, 4) != 0)
                {
                    //try the newer version
                    DwmSetWindowAttribute(form.Handle, 20, new[] { enabled }, 4);
                }

            }
            catch (Exception)
            {
                //eat whatever exception
            }
            form.BackColor = SystemColors.Control;
            if (form.ForeColor == System.Drawing.Color.LightGray)
            {
                form.ForeColor = SystemColors.ControlText;
            }
            ChangeThemeRecursive(container);
        }
        public static void ChangeThemeRecursive(Control.ControlCollection container)
        {
            foreach (Control component in container)
            {
                if (component is Button)
                {

                    component.BackColor = SystemColors.Control;
                    component.ForeColor = SystemColors.ControlText;

                }
                else if (component is MenuStrip)
                {
                    component.BackColor = SystemColors.Control;
                    if (component.ForeColor == System.Drawing.Color.LightGray)
                    {
                        component.ForeColor = SystemColors.ControlText; 
                    }
                }
                else if (component is Panel)
                {
                    ChangeThemeRecursive(component.Controls);
                    component.BackColor = SystemColors.Control;
                    if (component.ForeColor == System.Drawing.Color.LightGray)
                    {
                        component.ForeColor = component.ForeColor = SystemColors.ControlText;
                    }
                }
                else if (component is Label)
                {
                    ChangeThemeRecursive(component.Controls);
                    component.BackColor = SystemColors.Control;
                    if (component.ForeColor == System.Drawing.Color.LightGray)
                    {
                        component.ForeColor = component.ForeColor = SystemColors.ControlText;
                    }
                }
            }

        }
        [DllImport("DwmApi")] //System.Runtime.InteropServices
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, int[] attrValue, int attrSize);

    }
}

