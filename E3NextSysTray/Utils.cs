using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace E3NextSysTray
{
	internal class Utils
	{
		public static Icon BytesToIcon(byte[] bytes)
		{
			// Pass the byte array directly into a memory stream
			using (MemoryStream ms = new MemoryStream(bytes))
			{
				// Generate and return the Icon object
				return new Icon(ms);
			}
		}
		public static byte[] BitmapToBytes(Bitmap bitmap)
		{
			using (MemoryStream ms = new MemoryStream())
			{
				// Choose the format based on your needs (Png, Jpeg, etc.)
				bitmap.Save(ms, ImageFormat.Png);
				return ms.ToArray();
			}
		}
	}
}
