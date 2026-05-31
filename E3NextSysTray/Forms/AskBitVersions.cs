using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace E3NextSysTray.Forms
{
	internal class AskBitVersions
	{
		public static string Show(string title, string message, string btn1Text, string btn2Text)
		{
			using (Form form = new Form())
			{
				Label label = new Label();
				Button button1 = new Button();
				Button button2 = new Button();

				form.Text = title;
				label.Text = message;
				button1.Text = btn1Text;
				button2.Text = btn2Text;

				// Set Results
				button1.DialogResult = DialogResult.Yes;
				button2.DialogResult = DialogResult.No;

				// 1. Form Styling & Center Screen
				form.ClientSize = new Size(400, 150);
				form.StartPosition = FormStartPosition.CenterScreen; // Forces center of monitor
				form.FormBorderStyle = FormBorderStyle.FixedDialog;
				form.MaximizeBox = false;
				form.MinimizeBox = false;

				// 2. Label Layout
				label.AutoSize = false;
				label.TextAlign = ContentAlignment.MiddleCenter; // Centers text inside label
				label.Dock = DockStyle.Top;
				label.Height = 80;

				// 3. Button Layout (Centered)
				int buttonWidth = 100;
				int buttonHeight = 30;
				int spacing = 10;

				// Calculate starting X to center the pair of buttons
				int totalButtonsWidth = (buttonWidth * 2) + spacing;
				int startX = (form.ClientSize.Width - totalButtonsWidth) / 2;
				int buttonY = 90;

				button1.SetBounds(startX, buttonY, buttonWidth, buttonHeight);
				button2.SetBounds(startX + buttonWidth + spacing, buttonY, buttonWidth, buttonHeight);

				// Add controls
				form.Controls.AddRange(new Control[] { label, button1, button2 });
				form.AcceptButton = button1;
				form.CancelButton = button2;

				return (form.ShowDialog() == DialogResult.Yes) ? btn1Text : btn2Text;
			}
		}
	}
}
