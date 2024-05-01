using Krypton.Toolkit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace E3NextConfigEditor
{
	internal static class Program
	{
		static SplashScreen _splashScreen;
		static KryptonForm _mainForm;
		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		static void Main()
		{
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);

			_splashScreen = new SplashScreen();
			_splashScreen.StartPosition = FormStartPosition.CenterScreen;
			var splashThread = new Thread(new ThreadStart(
				() => Application.Run(_splashScreen)));

			splashThread.SetApartmentState(ApartmentState.STA);
			splashThread.Start();

			ConfigEditor._splashScreen = _splashScreen;
			_mainForm = new ConfigEditor();
			_mainForm.Load += _mainForm_Load;
			Application.Run(_mainForm);
		}

		private static void _mainForm_Load(object sender, EventArgs e)
		{
			if (_splashScreen != null && !_splashScreen.Disposing && !_splashScreen.IsDisposed)
				_splashScreen.Invoke(new Action(() => _splashScreen.Close()));
			_mainForm.TopMost = true;
			_mainForm.Activate();
			_mainForm.TopMost = false;
		}
	}
}
