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
	public partial class TTSConfig : Form,IDisposable
	{
		System.Windows.Forms.ToolTip _toolTipBrief = new System.Windows.Forms.ToolTip();
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

			_toolTipBrief.SetToolTip(checkBox_tts_breifmode, "Only use the inner text when talking");
			_toolTipBrief.SetToolTip(checkBox_channel_mobspells, "Warning this only gets mobs with 1+ spaces in their name. If a single name you will have to use PC with a regex filter.");
			_toolTipBrief.SetToolTip(checkBox_channel_pcspells, "Warning USE FILTERS! This only gets mobs with NO spaces in their name. Generally this is PCs but it can be named NPCS as well.");

		}
		private void buttonOK_Click(object sender, EventArgs e)
		{

		}

		private void button1_Click(object sender, EventArgs e)
		{

		}
		void IDisposable.Dispose()
		{
			_toolTipBrief.Dispose();
			// do the same for all other disposable objects your repository has created.
		}
	}
}
