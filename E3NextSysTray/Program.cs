using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace E3NextSysTray
{
	internal static class Program
	{
		private static string appGuid = "Global\\e849926a-932e-4395-959a-e09210bc44bf";
		// Import the necessary Windows API functions
		[DllImport("kernel32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool AllocConsole();

		[DllImport("kernel32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool FreeConsole();
		[DllImport("kernel32.dll")]
		private static extern IntPtr GetConsoleWindow();

		public static bool IsConsoleConnected()
		{
			// If it returns Zero, there is no console attached
			return GetConsoleWindow() != IntPtr.Zero;
		}
		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		static void Main()
		{
			
			//if (!System.Diagnostics.Debugger.IsAttached)
			//{
			//	string currentExe = Process.GetCurrentProcess().MainModule.ModuleName;
			//	if (currentExe!= "E3NextSysTray_Working.exe" && File.Exists("E3NextSysTray_Working.exe"))
			//	{
			//		return;
			//	}
			//}

			using (Mutex mutex = new Mutex(false, appGuid))
			{
				// If another instance is already running, exit immediately
				if (!mutex.WaitOne(TimeSpan.Zero, true))
				{
				//	MessageBox.Show("Application is already running.", "Notice", MessageBoxButtons.OK, MessageBoxIcon.Information);
					return;
				}
				Application.EnableVisualStyles();
				Application.SetCompatibleTextRenderingDefault(false);
				// Run the application via a custom ApplicationContext
				Application.Run(new TrayApplicationContext());
			}

		}
	}
}
