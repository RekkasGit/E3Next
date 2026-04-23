using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace E3NextSysTray
{
	internal static class Program
	{
		private static string appGuid = "Global\\e849926a-932e-4395-959a-e09210bc44bf";

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
