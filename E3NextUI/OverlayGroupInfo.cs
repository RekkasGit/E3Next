using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Text;
using System.Linq;
using System.Runtime.InteropServices;
using System.ServiceModel.Configuration;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace E3NextUI
{
	public partial class OverlayGroupInfo : Form
	{
		#region moveviaform
		//needed to move via the form
		public const int WM_NCLBUTTONDOWN = 0xA1;
		public const int HT_CAPTION = 0x2;
		[DllImportAttribute("user32.dll")]
		public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
		[DllImportAttribute("user32.dll")]
		public static extern bool ReleaseCapture();
		//end move via form code
		#endregion
		private delegate void SetInformationDelegate(Label control,string data);

		private void label1_MouseDown(object sender, MouseEventArgs e)
		{
			ReleaseCapture();
			SendMessage(this.Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
		}
		public OverlayGroupInfo()
		{
			InitializeComponent();
			InitComponentManually();
			this.BackColor = Color.LimeGreen;
			this.TransparencyKey = Color.LimeGreen;
			TopMost = true;
			this.ControlBox = false;
			this.Text = String.Empty;
			this.FormBorderStyle = FormBorderStyle.None;
		}
		private void InitComponentManually()
		{
			this.label_target1_info.Paint += new System.Windows.Forms.PaintEventHandler(this.label_target1_info_Paint);
			this.label_target2_info.Paint += new System.Windows.Forms.PaintEventHandler(this.label_target2_info_Paint);
			this.label_target3_info.Paint += new System.Windows.Forms.PaintEventHandler(this.label_target3_info_Paint);
			this.label_target4_info.Paint += new System.Windows.Forms.PaintEventHandler(this.label_target4_info_Paint);
			this.label_target5_info.Paint += new System.Windows.Forms.PaintEventHandler(this.label_target5_info_Paint);
			this.label_target6_info.Paint += new System.Windows.Forms.PaintEventHandler(this.label_target6_info_Paint);

			this.label_name1_aatotal.Paint += new System.Windows.Forms.PaintEventHandler(this.label_name1_aatotal_Paint);
			this.label_name2_aatotal.Paint += new System.Windows.Forms.PaintEventHandler(this.label_name2_aatotal_Paint);
			this.label_name3_aatotal.Paint += new System.Windows.Forms.PaintEventHandler(this.label_name3_aatotal_Paint);
			this.label_name4_aatotal.Paint += new System.Windows.Forms.PaintEventHandler(this.label_name4_aatotal_Paint);
			this.label_name5_aatotal.Paint += new System.Windows.Forms.PaintEventHandler(this.label_name5_aatotal_Paint);
			this.label_name6_aatotal.Paint += new System.Windows.Forms.PaintEventHandler(this.label_name6_aatotal_Paint);

			this.label_casting1_info.Paint += new System.Windows.Forms.PaintEventHandler(this.label_casting1_info_Paint);
			this.label_casting2_info.Paint += new System.Windows.Forms.PaintEventHandler(this.label_casting2_info_Paint);
			this.label_casting3_info.Paint += new System.Windows.Forms.PaintEventHandler(this.label_casting3_info_Paint);
			this.label_casting4_info.Paint += new System.Windows.Forms.PaintEventHandler(this.label_casting4_info_Paint);
			this.label_casting5_info.Paint += new System.Windows.Forms.PaintEventHandler(this.label_casting5_info_Paint);
			this.label_casting6_info.Paint += new System.Windows.Forms.PaintEventHandler(this.label_casting6_info_Paint);

			this.label_dps1.Paint += new System.Windows.Forms.PaintEventHandler(this.label_dps1_Paint);
			this.label_dps2.Paint += new System.Windows.Forms.PaintEventHandler(this.label_dps2_Paint);
			this.label_dps3.Paint += new System.Windows.Forms.PaintEventHandler(this.label_dps3_Paint);
			this.label_dps4.Paint += new System.Windows.Forms.PaintEventHandler(this.label_dps4_Paint);
			this.label_dps5.Paint += new System.Windows.Forms.PaintEventHandler(this.label_dps5_Paint);
			this.label_dps6.Paint += new System.Windows.Forms.PaintEventHandler(this.label_dps6_Paint);

			this.label_dps1_total.Paint += new System.Windows.Forms.PaintEventHandler(this.label_dps1_total_Paint);
			this.label_dps2_total.Paint += new System.Windows.Forms.PaintEventHandler(this.label_dps2_total_Paint);
			this.label_dps3_total.Paint += new System.Windows.Forms.PaintEventHandler(this.label_dps3_total_Paint);
			this.label_dps4_total.Paint += new System.Windows.Forms.PaintEventHandler(this.label_dps4_total_Paint);
			this.label_dps5_total.Paint += new System.Windows.Forms.PaintEventHandler(this.label_dps5_total_Paint);
			this.label_dps6_total.Paint += new System.Windows.Forms.PaintEventHandler(this.label_dps6_total_Paint);
		}
		protected override void OnPaintBackground(PaintEventArgs e)
		{
			e.Graphics.FillRectangle(Brushes.LimeGreen, e.ClipRectangle);
		}
		private void PaintLabel(Label label, PaintEventArgs e)
		{
			e.Graphics.Clear(label.BackColor);

			using (var sf = new StringFormat())
			using (var br = new SolidBrush(label.ForeColor))
			{
				sf.Alignment = sf.LineAlignment = StringAlignment.Near;
				e.Graphics.TextRenderingHint = TextRenderingHint.SingleBitPerPixelGridFit;
				e.Graphics.DrawString(label.Text, label.Font, br,
				label.ClientRectangle, sf);
			}
		}
		private void label_name1_Paint(object sender, PaintEventArgs e)
		{
			//https://stackoverflow.com/questions/70209408/winform-transparent-background-text-outline-have-the-form-background-color
			PaintLabel(label_name1, e);
		}
		private void label_name2_Paint(object sender, PaintEventArgs e)
		{
			PaintLabel(label_name2, e);
		}

		private void label_name3_Paint(object sender, PaintEventArgs e)
		{
			PaintLabel(label_name3, e);
		}

		private void label_name4_Paint(object sender, PaintEventArgs e)
		{
			PaintLabel(label_name4, e);
		}

		private void label_name5_Paint(object sender, PaintEventArgs e)
		{
			PaintLabel(label_name5, e);
			
		}

		private void label_name6_Paint(object sender, PaintEventArgs e)
		{
			PaintLabel(label_name6, e);
		}

		private void label_target1_Paint(object sender, PaintEventArgs e)
		{
			PaintLabel(label_target1, e);
		}

		private void label_target2_Paint(object sender, PaintEventArgs e)
		{
			PaintLabel(label_target2, e);
		}

		private void label_target3_Paint(object sender, PaintEventArgs e)
		{
			PaintLabel(label_target3, e);
		}

		private void label_target4_Paint(object sender, PaintEventArgs e)
		{
			PaintLabel(label_target4, e);
		}

		private void label_target5_Paint(object sender, PaintEventArgs e)
		{
			PaintLabel(label_target5, e);
		}

		private void label_target6_Paint(object sender, PaintEventArgs e)
		{
			PaintLabel(label_target6, e);
		}

		private void label_target1_info_Paint(object sender, PaintEventArgs e)
		{
			PaintLabel(label_target1_info, e);
		}
		private void label_target2_info_Paint(object sender, PaintEventArgs e)
		{
			PaintLabel(label_target2_info, e);
		}
		private void label_target3_info_Paint(object sender, PaintEventArgs e)
		{
			PaintLabel(label_target3_info, e);
		}
		private void label_target4_info_Paint(object sender, PaintEventArgs e)
		{
			PaintLabel(label_target4_info, e);
		}
		private void label_target5_info_Paint(object sender, PaintEventArgs e)
		{
			PaintLabel(label_target5_info, e);
		}
		private void label_target6_info_Paint(object sender, PaintEventArgs e)
		{
			PaintLabel(label_target6_info, e);
		}
		private void label_name1_aatotal_Paint(object sender, PaintEventArgs e)
		{
			PaintLabel(label_name1_aatotal, e);
		}
		private void label_name2_aatotal_Paint(object sender, PaintEventArgs e)
		{
			PaintLabel(label_name2_aatotal, e);
		}
		private void label_name3_aatotal_Paint(object sender, PaintEventArgs e)
		{
			PaintLabel(label_name3_aatotal, e);
		}
		private void label_name4_aatotal_Paint(object sender, PaintEventArgs e)
		{
			PaintLabel(label_name4_aatotal, e);
		}
		private void label_name5_aatotal_Paint(object sender, PaintEventArgs e)
		{
			PaintLabel(label_name5_aatotal, e);
		}
		private void label_name6_aatotal_Paint(object sender, PaintEventArgs e)
		{
			PaintLabel(label_name6_aatotal, e);
		}
		private void label_casting1_info_Paint(object sender, PaintEventArgs e)
		{
			PaintLabel(label_casting1_info, e);
		}
		private void label_casting2_info_Paint(object sender, PaintEventArgs e)
		{
			PaintLabel(label_casting2_info, e);
		}
		private void label_casting3_info_Paint(object sender, PaintEventArgs e)
		{
			PaintLabel(label_casting3_info, e);
		}
		private void label_casting4_info_Paint(object sender, PaintEventArgs e)
		{
			PaintLabel(label_casting4_info, e);
		}
		private void label_casting5_info_Paint(object sender, PaintEventArgs e)
		{
			PaintLabel(label_casting5_info, e);
		}
		private void label_casting6_info_Paint(object sender, PaintEventArgs e)
		{
			PaintLabel(label_casting6_info, e);
		}
		private void label_dps1_Paint(object sender, PaintEventArgs e)
		{
			PaintLabel(label_dps1, e);
		}
		private void label_dps2_Paint(object sender, PaintEventArgs e)
		{
			PaintLabel(label_dps2, e);
		}
		private void label_dps3_Paint(object sender, PaintEventArgs e)
		{
			PaintLabel(label_dps3, e);
		}
		private void label_dps4_Paint(object sender, PaintEventArgs e)
		{
			PaintLabel(label_dps4, e);
		}
		private void label_dps5_Paint(object sender, PaintEventArgs e)
		{
			PaintLabel(label_dps5, e);
		}
		private void label_dps6_Paint(object sender, PaintEventArgs e)
		{
			PaintLabel(label_dps6, e);
		}
		private void label_dps1_total_Paint(object sender, PaintEventArgs e)
		{
			PaintLabel(label_dps1_total, e);
		}
		private void label_dps2_total_Paint(object sender, PaintEventArgs e)
		{
			PaintLabel(label_dps2_total, e);
		}
		private void label_dps3_total_Paint(object sender, PaintEventArgs e)
		{
			PaintLabel(label_dps3_total, e);
		}
		private void label_dps4_total_Paint(object sender, PaintEventArgs e)
		{
			PaintLabel(label_dps4_total, e);
		}
		private void label_dps5_total_Paint(object sender, PaintEventArgs e)
		{
			PaintLabel(label_dps5_total, e);
		}
		private void label_dps6_total_Paint(object sender, PaintEventArgs e)
		{
			PaintLabel(label_dps6_total, e);
		}
		public void SetOverlayLabelData(Label control, string data)
		{
			if (control.Text == data || control.Text==String.Empty && data=="NULL") return;
			if (this.InvokeRequired)
			{
				this.Invoke(new SetInformationDelegate(SetOverlayLabelData), new object[] { control,data });
			}
			else
			{
				if(data=="NULL")
				{
					control.Text = String.Empty;

				}
				else
				{
					control.Text = data;
				}
			}
		}

	}
}

