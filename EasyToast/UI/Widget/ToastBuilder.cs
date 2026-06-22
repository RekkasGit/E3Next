using System.Drawing;
using System.Windows.Forms;
using System.Enums;

namespace System.UI.Widget
{
	/// <summary>
	/// Make your own custom Toast Notification by the Builder
	/// </summary>
	public class ToastBuilder
	{
		private readonly Toast _toast;

		public ToastBuilder()
		{
			_toast = new Toast();
		}

		/// <summary>
		/// Set caption for toast
		/// </summary>
		/// <param name="toast">toast</param>
		/// <param name="text">Text data to display</param>
		/// <returns></returns>
		public ToastBuilder SetCaption(string caption)
		{
			_toast.Caption = caption;
			return this;
		}
		public ToastBuilder SetAnimation(Animation ani)
		{
			_toast.Animation = ani;
			return this;
		}
	
		/// <summary>
		/// Set description for Toast
		/// </summary>
		/// <param name="description"></param>
		/// <returns></returns>
		public ToastBuilder SetDescription(string description)
		{
			_toast.Description = description?.Trim() ?? string.Empty;
			return this;
		}

		/// <summary>
		/// Set duration time of Toast
		/// </summary>
		/// <param name="toast"></param>
		/// <param name="duration"></param>
		/// <returns></returns>
		public ToastBuilder SetDuration(UInt16 duration)
		{
			_toast.Duration = duration;
			return this;
		}

		/// <summary>
		/// Set muting mode for Toast
		/// </summary>
		/// <param name="muting"></param>
		/// <returns></returns>
		public ToastBuilder SetMuting(bool muting = false)
		{
			_toast.IsMuted = muting;
			return this;
		}

		public ToastBuilder SetThumbnail(Image image)
		{
			_toast.Thumbnail = image;
			
			return this;
		}
		public ToastBuilder SetAppThumbnail(Image image)
		{
			_toast.ThumbnailApp = image;
			return this;
		}
		/// <summary>
		/// Build the final toast
		/// </summary>
		/// <returns></returns>
		public Toast Build()
		{
			return _toast;
		}
	}
}
