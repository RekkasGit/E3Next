using System;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Media;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Enums;
using System.Threading;

namespace System.UI.Widget
{
	public partial class FrmToast : Form
	{
        private readonly int _horizontalMargin;

		private readonly int _verticalMargin;

		private bool _shown;

		private bool _isMuted = false;

		private Animation _animation;

		private const int AW_SLIDE = 0X40000;

		private const int AW_HOR_POSITIVE = 0X1;

		private const int AW_HOR_NEGATIVE = 0X2;

		private const int AW_HIDE = 0x00010000;

		private const int AW_ACTIVATE = 0x00020000;

		private const int AW_BLEND = 0X80000;

		private const int AW_CENTER = 0x00000010;

		private Int32 _counter = 2;

		private Int32 _duration;

		private Toast _toast;

		internal CancellationToken CancellationToken { get; set; }

		public bool IsAsync = false;
		public bool HideInsteadOfClose = false;

		public Theme Theme = Theme.Dark;

		public CloseStye CloseStyle = CloseStye.ButtonAndClickEntire;
		[DllImport("user32")]
		private static extern bool AnimateWindow(IntPtr hwnd, int time, int flags);

		internal FrmToast()
		{
			InitializeComponent();
			_horizontalMargin = 10;
			_verticalMargin = 10;
			
			var workingArea = Screen.GetWorkingArea(this);

			Location = new Point(workingArea.Right - Size.Width - _horizontalMargin,
				workingArea.Bottom - Size.Height - _verticalMargin);
		}

		internal Toast Toast
		{
			get => _toast;
			set => _toast = value;
		}

		public bool IsShown => _shown;
		public bool TimerDisabled = false;
		[DefaultValue(1)]
		internal Int32 Duration
		{
			get => _duration;
			set { _duration = value; if (_duration < 0) TimerDisabled = true; _counter = Duration; }
		}

		[DefaultValue(Animation.FADE)]
		internal Animation Animation
		{
			get => _animation;
			set => _animation = value;
		}

		[DefaultValue("")]
		public string Caption
		{
			get => lblCaption.Text;
			set => lblCaption.Text = value?.Trim() ?? string.Empty;
		}

		[DefaultValue("")]
		public string Description
        {
			get => lblDescription.Text;
			set => lblDescription.Text = value?.Trim() ?? string.Empty;

		}

		public Image Thumbnails
		{
			get => picImage.Image;
			set
			{
				picImage.Image = value;
				Invalidate();
				if (value != null)
				{
					HasImage = true;
				}
				
			} 
		}


		public Image AppThumbnail
		{
			get => picAppOwnerIcon.Image;
			set
			{
				picAppOwnerIcon.Image = value;
				Invalidate();
				if (value != null)
				{
					HasAppImage = true;
				}

			}
		}

        public bool HasImage { get; private set; }
		public bool HasAppImage { get; private set; }

		[DefaultValue(false)]
		internal bool IsMuted
		{
			get => _isMuted;
			set => _isMuted = value;
		}

		internal int HorizontalMargin => _horizontalMargin;
		internal int VerticalMargin => _verticalMargin;

		private async void FrmToast_Load(object sender, EventArgs e)
		{
			if (IsAsync)
			{
				await Task.Yield();
			}

			switch (CloseStyle)
			{
				case CloseStye.ClickEntire:
					textContainer.Panel1Collapsed = true;
					break;
				case CloseStye.Button:
					textContainer.Panel1Collapsed = false;
					break;
				case CloseStye.ButtonAndClickEntire:
					textContainer.Panel1Collapsed = false;
					break;
			}

			_counter = _duration;
			
			if (!_isMuted)
			{
				//PlaySound();
			}
			SetTheme();
			switch (_animation)
			{
				case Animation.FADE:
					FadeIn();
					break;
				case Animation.SLIDE:
					AnimateWindow(Handle, 250, AW_SLIDE | AW_HOR_NEGATIVE | AW_ACTIVATE);
					break;
			}
			
		}

		private async void PlaySound()
		{
			await Task.Factory.StartNew(() =>
			{

				Stream sound = Properties.Resources.notificationSound;
				var player = new SoundPlayer(sound);
				player.Play();
			});
		}

		private void SetTheme()
		{
			switch (Theme)
			{
				case Theme.Dark:
					lblCaption.ForeColor = ThemeBuilder.BuiltinScheme.DarkScheme.GetForegroundColor();
					btnClose.ForeColor = ThemeBuilder.BuiltinScheme.DarkScheme.GetForegroundColor();
					BackColor = ThemeBuilder.BuiltinScheme.DarkScheme.GetBackgroundColor();
					btnClose.FlatAppearance.BorderColor = ThemeBuilder.BuiltinScheme.DarkScheme.GetBackgroundColor();
					break;
				case Theme.Light:
					lblCaption.ForeColor = ThemeBuilder.BuiltinScheme.LightScheme.GetForegroundColor();
					btnClose.ForeColor = ThemeBuilder.BuiltinScheme.LightScheme.GetForegroundColor();
					BackColor = ThemeBuilder.BuiltinScheme.LightScheme.GetBackgroundColor();
					btnClose.FlatAppearance.BorderColor = ThemeBuilder.BuiltinScheme.LightScheme.GetBackgroundColor();
					break;
				case Theme.PrimaryLight:
					lblCaption.ForeColor = ThemeBuilder.BuiltinScheme.PrimaryLightScheme.GetForegroundColor();
					btnClose.ForeColor = ThemeBuilder.BuiltinScheme.PrimaryLightScheme.GetForegroundColor();
					BackColor = ThemeBuilder.BuiltinScheme.PrimaryLightScheme.GetBackgroundColor();
					btnClose.FlatAppearance.BorderColor = ThemeBuilder.BuiltinScheme.PrimaryLightScheme.GetBackgroundColor();
					break;
				case Theme.SuccessLight:
					lblCaption.ForeColor = ThemeBuilder.BuiltinScheme.SuccessLightScheme.GetForegroundColor();
					btnClose.ForeColor = ThemeBuilder.BuiltinScheme.SuccessLightScheme.GetForegroundColor();
					BackColor = ThemeBuilder.BuiltinScheme.SuccessLightScheme.GetBackgroundColor();
					btnClose.FlatAppearance.BorderColor = ThemeBuilder.BuiltinScheme.SuccessLightScheme.GetBackgroundColor();
					break;
				case Theme.WarningLight:
					lblCaption.ForeColor = ThemeBuilder.BuiltinScheme.WarningLightScheme.GetForegroundColor();
					btnClose.ForeColor = ThemeBuilder.BuiltinScheme.WarningLightScheme.GetForegroundColor();
					BackColor = ThemeBuilder.BuiltinScheme.WarningLightScheme.GetBackgroundColor();
					btnClose.FlatAppearance.BorderColor = ThemeBuilder.BuiltinScheme.WarningLightScheme.GetBackgroundColor();
					break;
				case Theme.ErrorLight:
					lblCaption.ForeColor = ThemeBuilder.BuiltinScheme.ErrorLightScheme.GetForegroundColor();
					btnClose.ForeColor = ThemeBuilder.BuiltinScheme.ErrorLightScheme.GetForegroundColor();
					BackColor = ThemeBuilder.BuiltinScheme.ErrorLightScheme.GetBackgroundColor();
					btnClose.FlatAppearance.BorderColor = ThemeBuilder.BuiltinScheme.ErrorLightScheme.GetBackgroundColor();
					break;
				case Theme.PrimaryDark:
					lblCaption.ForeColor = ThemeBuilder.BuiltinScheme.PrimaryDarkScheme.GetForegroundColor();
					btnClose.ForeColor = ThemeBuilder.BuiltinScheme.PrimaryDarkScheme.GetForegroundColor();
					BackColor = ThemeBuilder.BuiltinScheme.PrimaryDarkScheme.GetBackgroundColor();
					btnClose.FlatAppearance.BorderColor = ThemeBuilder.BuiltinScheme.PrimaryDarkScheme.GetBackgroundColor();
					break;
				case Theme.SuccessDark:
					lblCaption.ForeColor = ThemeBuilder.BuiltinScheme.SuccessDarkScheme.GetForegroundColor();
					btnClose.ForeColor = ThemeBuilder.BuiltinScheme.SuccessDarkScheme.GetForegroundColor();
					BackColor = ThemeBuilder.BuiltinScheme.SuccessDarkScheme.GetBackgroundColor();
					btnClose.FlatAppearance.BorderColor = ThemeBuilder.BuiltinScheme.SuccessDarkScheme.GetBackgroundColor();
					break;
				case Theme.WarningDark:
					lblCaption.ForeColor = ThemeBuilder.BuiltinScheme.WarningDarkScheme.GetForegroundColor();
					btnClose.ForeColor = ThemeBuilder.BuiltinScheme.WarningDarkScheme.GetForegroundColor();
					BackColor = ThemeBuilder.BuiltinScheme.WarningDarkScheme.GetBackgroundColor();
					btnClose.FlatAppearance.BorderColor = ThemeBuilder.BuiltinScheme.WarningDarkScheme.GetBackgroundColor();
					break;
				case Theme.ErrorDark:
					lblCaption.ForeColor = ThemeBuilder.BuiltinScheme.ErrorDarkScheme.GetForegroundColor();
					btnClose.ForeColor = ThemeBuilder.BuiltinScheme.ErrorDarkScheme.GetForegroundColor();
					BackColor = ThemeBuilder.BuiltinScheme.ErrorDarkScheme.GetBackgroundColor();
					btnClose.FlatAppearance.BorderColor = ThemeBuilder.BuiltinScheme.ErrorDarkScheme.GetBackgroundColor();
					break;
				case Theme.Custom:
					if (ThemeBuilder.CustomScheme == null)
					{
						throw new NullReferenceException($"You must create your scheme before set custom theme. Use ${nameof(ThemeBuilder.CreateCustomScheme)}() to create a custom scheme");
					}
					else
					{
						lblCaption.ForeColor = ThemeBuilder.CustomScheme.GetForegroundColor();
						btnClose.ForeColor = ThemeBuilder.CustomScheme.GetForegroundColor();
						BackColor = ThemeBuilder.CustomScheme.GetBackgroundColor();
						btnClose.FlatAppearance.BorderColor = ThemeBuilder.CustomScheme.GetBackgroundColor();
					}
					break;
			}
		}

		private void FrmToast_Shown(object sender, EventArgs e)
		{
			_shown = true;
			tmrClose.Start();
		}
		private void MyForm_VisibleChanged(object sender, EventArgs e)
		{
			// Check if the form is now hidden
			if (!this.Visible)
			{
				_shown = false;
				tmrClose.Stop();
			}
			else
			{
				_shown = true;
			}
		}
		private void FrmToast_FormClosing(object sender, FormClosingEventArgs e)
		{
			switch (_animation)
			{
				case Animation.FADE:
					AnimateWindow(Handle, 250, AW_BLEND | AW_HIDE);
					break;
				case Animation.SLIDE:
					AnimateWindow(Handle, 250, AW_SLIDE | AW_HOR_POSITIVE | AW_HIDE);
					break;
			}
		}

		private void ToastContentClick(object sender, EventArgs e)
        {
			switch (CloseStyle)
			{
				case CloseStye.ClickEntire:

				case CloseStye.ButtonAndClickEntire:
					if (HideInsteadOfClose) Hide();
					else { Close(); }
					break;
				case CloseStye.Button:
					return;
			}
		}
		public void CloseToast()
		{
			tmrClose.Stop();
			if (HideInsteadOfClose) Hide();
			else { Close(); }
		
		}
		public void HideToast()
		{
			tmrClose.Stop();
			Hide();
		}
		public void ShowToast()
		{
			tmrClose.Start();
			Show();
		}
		private void TmrClose_Tick(object sender, EventArgs e)
		{
			if (TimerDisabled) return;
			_counter--;
			if (_counter != 0) return;

			//tmrClose.Stop();
			if (HideInsteadOfClose) Hide();
			else { Close(); }
		}

		private async void FadeIn()
		{
			Opacity = 0;
			while (Opacity < 1.0)
			{
				await Task.Delay(3);
				Opacity += 0.05;
			}
			Opacity = 1;
		}

		private SizeF CalculateString()
		{
			if (string.IsNullOrEmpty(lblCaption.Text)) return SizeF.Empty;
			using (var g = CreateGraphics())
			{
				var size = g.MeasureString(lblCaption.Text, lblCaption.Font);
				return size;
			}
		}

		private void FrmToast_FormClosed(object sender, FormClosedEventArgs e)
		{
			if(!HideInsteadOfClose)
			{
				//if (picAppOwnerIcon != null) { picAppOwnerIcon.Dispose(); }
				//if (picImage != null) { picImage.Dispose(); }
				ToastManager.ToastCollection.Remove(_toast);
			}
		}

		private void BtnClose_Click(object sender, EventArgs e)
		{
			if (HideInsteadOfClose) Hide();
			else { Close(); }
		}
	}
}
