using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace E3NextUI.Themese
{
    public enum DWMWINDOWATTRIBUTE : uint
    {
        DWMWA_NCRENDERING_ENABLED,
        DWMWA_NCRENDERING_POLICY,
        DWMWA_TRANSITIONS_FORCEDISABLED,
        DWMWA_ALLOW_NCPAINT,
        DWMWA_CAPTION_BUTTON_BOUNDS,
        DWMWA_NONCLIENT_RTL_LAYOUT,
        DWMWA_FORCE_ICONIC_REPRESENTATION,
        DWMWA_FLIP3D_POLICY,
        DWMWA_EXTENDED_FRAME_BOUNDS,
        DWMWA_HAS_ICONIC_BITMAP,
        DWMWA_DISALLOW_PEEK,
        DWMWA_EXCLUDED_FROM_PEEK,
        DWMWA_CLOAK,
        DWMWA_CLOAKED,
        DWMWA_FREEZE_REPRESENTATION,
        DWMWA_PASSIVE_UPDATE_MODE,
        DWMWA_USE_HOSTBACKDROPBRUSH,
        DWMWA_USE_IMMERSIVE_DARK_MODE = 20,
        DWMWA_WINDOW_CORNER_PREFERENCE = 33,
        DWMWA_BORDER_COLOR,
        DWMWA_CAPTION_COLOR,
        DWMWA_TEXT_COLOR,
        DWMWA_VISIBLE_FRAME_BORDER_THICKNESS,
        DWMWA_SYSTEMBACKDROP_TYPE,
        DWMWA_LAST
    }
    public static class DarkMode
    {
        public static void ChangeTheme(System.Windows.Forms.Form form, Control.ControlCollection container)
        {
            var preference = Convert.ToInt32(true);
            DwmSetWindowAttribute(form.Handle,
                                  DWMWINDOWATTRIBUTE.DWMWA_USE_IMMERSIVE_DARK_MODE,
                                  ref preference, sizeof(uint));

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
                else if(component is MenuStrip)
                {
                    component.BackColor = Color.FromArgb(60, 60, 60);
                    if (component.ForeColor == SystemColors.ControlText)
                    {
                        component.ForeColor = System.Drawing.Color.LightGray;
                    }
                }
                else if (component is Panel)
                {
                    ChangeThemeRecursive(component.Controls);
                    component.BackColor = Color.FromArgb(60, 60, 60);
                    if (component.ForeColor== SystemColors.ControlText)
                    {
                        component.ForeColor = System.Drawing.Color.LightGray;
                    }
                }
                else if (component is Label)
                {
                    ChangeThemeRecursive(component.Controls);
                    component.BackColor = Color.FromArgb(60, 60, 60);
                    if (component.ForeColor == SystemColors.ControlText)
                    {
                        component.ForeColor = System.Drawing.Color.LightGray;
                    }
                }
            }

        }
        [DllImport("dwmapi.dll", CharSet = CharSet.Unicode, PreserveSig = false)]
        public static extern void DwmSetWindowAttribute(IntPtr hwnd,
                                                DWMWINDOWATTRIBUTE attribute,
                                                ref int pvAttribute,
                                                uint cbAttribute);

    }
}
