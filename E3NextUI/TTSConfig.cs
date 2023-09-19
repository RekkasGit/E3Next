using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Speech.Synthesis;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace E3NextUI
{
	public partial class TTSConfig : Form
	{
	
		public List<string> _voices = new List<string>();
		static TTSConfig()
		{

		
		}
			

		public TTSConfig()
		{
			InitializeComponent();
			using (SpeechSynthesizer _synth = new SpeechSynthesizer())
			{
				var voices = _synth.GetInstalledVoices();
				foreach (var voice in voices)
				{
					if (voice.Enabled)
					{
						//voice.VoiceInfo.Name;
						_voices.Add(voice.VoiceInfo.Name);
					}
				}
				comboBox_tts_voices.DataSource = _voices;
			}
		}
		private void buttonOK_Click(object sender, EventArgs e)
		{

		}

		private void button1_Click(object sender, EventArgs e)
		{

		}
	}
}
