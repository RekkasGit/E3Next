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

		[DllImport("kernel32.dll")]
		public static extern bool GetBinaryType(string lpApplicationName, out uint lpBinaryType);

		public const uint SCS_32BIT_BINARY = 0;
		public const uint SCS_64BIT_BINARY = 6;

		public static int CheckBitness(string path)
		{
			if (GetBinaryType(path, out uint type))
			{
				if (type == SCS_64BIT_BINARY) return 64;
				else if (type == SCS_32BIT_BINARY) return 32;
			}
			return -1;
		}
		public static int GetNativeMachineType(string fileName)
		{
			if(File.Exists(fileName))
			{
				using (var fs = new System.IO.FileStream(fileName, System.IO.FileMode.Open, System.IO.FileAccess.Read))
				using (var reader = new System.IO.BinaryReader(fs))
				{
					// Check for MZ signature
					if (reader.ReadUInt16() != 0x5A4D) return -1;

					// Seek to PE header offset (found at 0x3C)
					fs.Seek(0x3C, System.IO.SeekOrigin.Begin);
					uint peHeaderOffset = reader.ReadUInt32();

					// Move to the Machine field in the COFF header (PE signature + 4 bytes)
					fs.Seek(peHeaderOffset + 4, System.IO.SeekOrigin.Begin);
					ushort machine = reader.ReadUInt16();

					if (machine == 0x14c) return 32;
					if (machine == 0x14c) return 64;
					return -1;
				}

			}

			return - 1;
		}
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
