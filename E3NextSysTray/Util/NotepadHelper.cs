using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace E3NextSysTray.Util
{
	public static class NotepadHelper
	{
		[DllImport("user32.dll", EntryPoint = "SetWindowText")]
		private static extern int SetWindowText(IntPtr hWnd, string text);

		[DllImport("user32.dll", EntryPoint = "FindWindowEx")]
		private static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string lpszClass, string lpszWindow);

		[DllImport("User32.dll", EntryPoint = "SendMessage")]
		private static extern int SendMessage(IntPtr hWnd, int uMsg, int wParam, string lParam);

		public static void ShowMessage(string message = null, string title = null)
		{
			string oldNotepadPath = @"C:\Windows\SysWOW64\notepad.exe";

			// Fallback to System32 if running on a 32-bit native OS environment
			if (!System.IO.File.Exists(oldNotepadPath))
			{
				oldNotepadPath = @"C:\Windows\System32\notepad.exe";
			}

			var startInfo = new ProcessStartInfo(oldNotepadPath);
			startInfo.UseShellExecute = false;
			Process notepad = Process.Start(startInfo);
			if (notepad != null)
			{
				notepad.WaitForInputIdle();

				if (!string.IsNullOrEmpty(title))
				{
					SetWindowText(notepad.MainWindowHandle, title);
				}

				if (!string.IsNullOrEmpty(message))
				{
					IntPtr child = FindWindowEx(notepad.MainWindowHandle, new IntPtr(0), "Edit", null);
					SendMessage(child, 0x000C, 0, message);
				}
			}
		}
	}
}
