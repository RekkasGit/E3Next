using E3NextConfigEditor.MQ;
using Krypton.Toolkit;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
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
		static Image _e3nImage;
		static byte[] _e3nImageBytes;
		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		static void Main()
		{
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);
	
			_splashScreen = new SplashScreen();

			SetSplashImage();
			
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
		private static void SetSplashImage()
		{
			try
			{
				using (var filestream = File.OpenRead("E3Next.png"))
				{
					_e3nImageBytes = new byte[filestream.Length];
					filestream.Read(_e3nImageBytes, 0, (Int32)filestream.Length);
					//do stuff 
				}

				using (var stream = new MemoryStream(_e3nImageBytes))
				{
					_e3nImage = Image.FromStream(stream);
				}
				_splashScreen.e3nextPictureBox.Image = _e3nImage;
			}
			catch (Exception ex)
			{
				System.Windows.Forms.MessageBox.Show(ex.Message);
			}


		}
		private static void _mainForm_Load(object sender, EventArgs e)
		{
			if (_splashScreen != null && !_splashScreen.Disposing && !_splashScreen.IsDisposed)
			{
				
				_splashScreen.Invoke(new Action(() => _splashScreen.e3nextPictureBox.Image = null));
				_splashScreen.Invoke(new Action(() => _splashScreen.Close()));

			}
			_mainForm.TopMost = true;
			_mainForm.Activate();
			Task.Delay(2000).ContinueWith(t => _mainForm.Invoke(new Action(() =>
			{
				_mainForm.TopMost = false;
			}
			)));
			
		
		}
	}
}
