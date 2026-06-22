using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using System.Enums;
using System.Collections.Generic;

namespace System.UI.Widget
{
	public class Toast
	{
		#region Private fields
		public readonly FrmToast FrmToast = new FrmToast();
		#endregion

		#region properties


		/// <summary>
		/// Gets unique ID of Toast
		/// </summary>
		public string Guid { get; }

		/// <summary>
		/// Gets or sets caption of Toast
		/// </summary>
		[DefaultValue("")]
		public string Caption { get { return FrmToast.Caption; } set { FrmToast.Caption = value; } }

		/// <summary>
		/// Gets or sets description of Toast
		/// </summary>
		[DefaultValue("")]
		public string Description { get { return FrmToast.Description; } set { FrmToast.Description = value; } }

		/// <summary>
		/// Gets or sets timeout duration of Toas
		/// </summary>
		[DefaultValue(1)]
		public Int32 Duration { get { return FrmToast.Duration; } set { FrmToast.Duration = value; } }

		[DefaultValue(false)]
		internal bool IsMuted { get { return FrmToast.IsMuted; } set { FrmToast.IsMuted = value; } }

		[DefaultValue(Animation.FADE)]
		internal Animation Animation { get { return FrmToast.Animation; } set { FrmToast.Animation = value; } }

		internal Image Thumbnail { get { return FrmToast.Thumbnails; } set { FrmToast.Thumbnails = value; } }
		internal Image ThumbnailApp { get { return FrmToast.AppThumbnail; } set { FrmToast.AppThumbnail = value; } }

		[DefaultValue(Position.BottomRight)]
		internal Position Position { get; set; } = Position.BottomRight;

		[DefaultValue(Theme.Dark)]
		internal Theme ThemeStyle { get { return FrmToast.Theme; } set { FrmToast.Theme = value; } }

		[DefaultValue(CloseStye.ButtonAndClickEntire)]
		internal CloseStye CloseStyle { get { return FrmToast.CloseStyle; } set { FrmToast.CloseStyle = value; } }

		internal ColorScheme CustomThem { get; set; }

		#endregion

		#region Constructors

		/// <summary>
		/// Construct an empty Toast object. You must sets View before you can call Show().
		/// </summary>
		/// <param name="window">Containter form. Usually MainForm.</param>
		internal Toast()
		{
			Guid = Utils.GetGuid();
			FrmToast.Toast = this;
		}

		#endregion

		#region Public methods

		/// <summary>
		/// Display the Toast for the specified configuration.
		/// <exception cref="ArgumentException">Thrown when Text property is null or empty</exception>
		/// </summary>
		/// 
	
		public void Show()
		{
			InternalDisplayToast();
		}

		/// <summary>
		/// Display the Toast asynchronously for the specified configuration.
		/// <exception cref="ArgumentException">Thrown when Text property is null or empty</exception>
		/// </summary>
		public void ShowAsync()
		{
			InternalDisplayToast(true);
		}

		/// <summary>
		/// Close the Toast if it's showing, or don't show it if it isn't showing yet. You do not normally have to call this. Normally Toast will disappear on its own after the appropriate duration.
		/// </summary>
		public void Cancel()
		{
			if(FrmToast.IsShown)
				FrmToast.Close();
			else
				throw new InvalidOperationException("You cannot cancel toast displaying when it doesn't display");
		}

		/// <summary>
		/// Get current horizontal margin of toast
		/// </summary>
		/// <returns></returns>
		public int GetHorizontalMargin()
		{
			return FrmToast.HorizontalMargin;
		}

		/// <summary>
		/// Get current vertical margin of toast
		/// </summary>
		/// <returns></returns>
		public int GetVerticalMargin()
		{
			return FrmToast.VerticalMargin;
		}

		#endregion

		#region Private methods
		public bool HasShown = false;
		private void InternalDisplayToast(bool async = false)
		{
			
			if (!HasShown)
			{
				FrmToast.IsAsync = async;
				ToastManager.CurrentToast = this;
				FrmToast.Click += FrmToast_Click;
				FrmToast.MouseHover += FrmToast_MouseHover;
				FrmToast.FormClosed += FrmToast_FormClosed;
				ToastManager.AddToCollection();
			}
			HasShown = true;
			this.FrmToast.Show();
		}

		private void FrmToast_FormClosed(object sender, FormClosedEventArgs e)
		{
			OnClosed?.Invoke(this, EventArgs.Empty);
		}

		private void FrmToast_MouseHover(object sender, EventArgs e)
		{
			OnHover?.Invoke(this, e);
		}

		private void FrmToast_Click(object sender, EventArgs e)
		{
			OnClick?.Invoke(this, e);
		}

		#endregion

		#region Public static methods

		/// <summary>
		/// Build a simplest Toast with Text only.
		/// </summary>
		/// <param name="window">Containter form. Usually MainForm.</param>
		/// <param name="text">Text to display. Required not null or empty.</param>
		/// <returns>Toast has been create but not yet display. Use Show() to display it.</returns>
		public static Toast Build(string caption)
		{
			var toast = new Toast()
			{
				Caption = caption,
				Description = string.Empty
			};
			return toast;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="window"></param>
		/// <param name="caption"></param>
		/// <param name="description"></param>
		/// <returns></returns>
		public static Toast Build(string caption, string description)
		{
			var toast = new Toast()
			{
				Caption = caption,
				Description = description
			};
			return toast;
		}

		/// <summary>
		/// Build a Toast with custom duration and animation.
		/// </summary>
		/// <param name="window">Containter form. Usually MainForm.</param>
		/// <param name="caption">Text to display. Required not null or empty.</param>
		/// <param name="duration">How long to display. SHORT is 2 seconds and LONG is 3 seconds.</param>
		/// <param name="animation">Toast transition animation style. Use both fading and sliding animation style.</param>
		/// <returns>Toast has been create but not yet display. Use Show() or ShowAsync() to display it.</returns>
		public static Toast Build(string caption, UInt16 duration, Animation animation)
		{
			var toast = new Toast()
			{
				Caption = caption,
				Duration = duration,
				Animation = animation
			};

			return toast;
		}

		/// <summary>
		/// Build Toast with custom duration length.
		/// </summary>
		/// <param name="window">Containter form. Usually MainForm.</param>
		/// <param name="caption">Text to display. Required not null or empty.</param>
		/// <param name="description">Description</param>
		/// <param name="duration">How long to display. SHORT is 2 seconds and LONG is 3 seconds.</param>
		/// <returns>Toast has been create but not yet display. Use Show() or ShowAsync() to display it.</returns>
		public static Toast Build(string caption, string description, UInt16 duration)
		{
			var toast = new Toast()
			{
				Caption = caption,
				Duration = duration,
				Description = description
			};

			return toast;
		}

		/// <summary>
		/// Build a Toast with custom animation, duration, sound
		/// </summary>
		/// <param name="window">Containter form. Usually MainForm.</param>
		/// <param name="caption">Text to display. Required not null or empty.</param>
		/// <param name="duration">How long to display. SHORT is 2 seconds and LONG is 3 seconds.</param>
		/// <param name="animation">Toast transition animation style. Use both fading and sliding animation style.</param>
		/// <param name="muting">Set sound state. Muting or not. Sound using Windows 10 default notification sound</param>
		/// <returns>Toast has been create but not yet display. Use Show() or ShowAsync() to display it.</returns>
		public static Toast Build(string caption, Animation animation, UInt16 duration, bool muting)
		{
			var toast = new Toast()
			{
				Caption = caption,
				Animation = animation,
				Duration = duration,
				IsMuted = muting
			};

			return toast;
		}

		/// <summary>
		/// Build a Toast with custom animation
		/// </summary>
		/// <param name="window">Containter form. Usually MainForm.</param>
		/// <param name="caption">Text to display. Required not null or empty.</param>
		/// <param name="animation">Toast transition animation style. Use both fading and sliding animation style.</param>
		/// <returns>Toast has been create but not yet display. Use Show() or ShowAsync() to display it.</returns>
		public static Toast Build(string caption, Animation animation)
		{
			var toast = new Toast()
			{
				Caption = caption,
				Animation = animation
			};

			return toast;
		}

		/// <summary>
		/// Build a simple Toast with specific sound status
		/// </summary>
		/// <param name="window">Containter form. Usually MainForm.</param>
		/// <param name="caption">Text to display. Required not null or empty.</param>
		/// <param name="muting">Set sound state. Muting or not. Sound using Windows 10 default notification sound</param>
		/// <returns>Toast has been create but not yet display. Use Show() or ShowAsync() to display it.</returns>
		public static Toast Build(string caption, bool muting)
		{
			var toast = new Toast()
			{
				Caption = caption,
				IsMuted = muting
			};

			return toast;
		}

		/// <summary>
		/// Build a Toast with text and thumbnails and custom duration, animation
		/// </summary>
		/// <param name="window">Containter form. Usually MainForm.</param>
		/// <param name="caption">Text to display. Required not null or empty.</param>
		/// <param name="thumbnail">Thumbnail image to display in Toast. Required image have MINIMUM SIZE 64x64 pixels for best display</param>
		/// <param name="duration">How long to display. SHORT is 2 seconds and LONG is 3 seconds.</param>
		/// <param name="animation">Toast transition animation style. Use both fading and sliding animation style.</param>
		/// <returns>Toast has been create but not yet display. Use Show() to display it</returns>
		public static Toast Build(string caption, Image thumbnail, UInt16 duration, Animation animation)
		{
			var toast = new Toast()
			{
				Caption = caption,
				Thumbnail = thumbnail,
				Duration = duration,
				Animation = animation
			};
			return toast;
		}

		/// <summary>
		/// Build a simple Toast with thumbnail and custom duration, animation and sound
		/// </summary>
		/// <param name="window">Containter form. Usually MainForm.</param>
		/// <param name="caption">Text to display. Required not null or empty.</param>
		/// <param name="thumbnail">Thumbnail image to display in Toast. Required image have MINIMUM SIZE 64x64 pixels for best display</param>
		/// <param name="duration">How long to display. SHORT is 2 seconds and LONG is 3 seconds.</param>
		/// <param name="animation">Toast transition animation style. Use both fading and sliding animation style.</param>
		/// <param name="muting"></param>
		/// <returns>Toast has been create but not yet display. Use Show() to display it</returns>
		public static Toast Build(string caption, Image thumbnail, UInt16 duration,
			Animation animation, bool muting)
		{
			var toast = new Toast()
			{
				Caption = caption,
				Thumbnail = thumbnail,
				Duration = duration,
				Animation = animation,
				IsMuted = muting
			};
			return toast;
		}

		/// <summary>
		/// Build a simple Toast with thumbnail
		/// </summary>
		/// <param name="window">Containter form. Usually MainForm.</param>
		/// <param name="text">Text to display. Required not null or empty.</param>
		/// <param name="thumbnail">Thumbnail image to display in Toast. Required image have MINIMUM SIZE 64x64 pixels for best display</param>
		/// <returns></returns>
		public static Toast Build(string caption, Image thumbnail)
		{
			var toast = new Toast()
			{
				Caption = caption,
				Thumbnail = thumbnail
			};
			return toast;
		}

		/// <summary>
		/// Build a simple Toast with thumbnail and custom duration
		/// </summary>
		/// <param name="window">Containter form. Usually MainForm.</param>
		/// <param name="caption">Text to display. Required not null or empty.</param>
		/// <param name="thumbnail">Thumbnail image to display in Toast. Required image have MINIMUM SIZE 64x64 pixels for best display</param>
		/// <param name="duration">How long to display. SHORT is 2 seconds and LONG is 3 seconds.</param>
		/// <returns></returns>
		public static Toast Build(string caption, Image thumbnail, UInt16 duration)
		{
			var toast = new Toast()
			{
				Caption = caption,
				Thumbnail = thumbnail,
				Duration = duration
			};
			return toast;
		}

		#endregion

		#region Events

		public delegate void ClickEventHandler(object sender, EventArgs e);

		public event ClickEventHandler OnClick;

		public delegate void HoverEventHandler(object sender, EventArgs e);

		public event HoverEventHandler OnHover;

		public delegate void CloseEventHandler(object sender, EventArgs e);

		public event CloseEventHandler OnClosed;

		#endregion
	}
}
