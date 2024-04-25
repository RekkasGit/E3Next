using E3Core.Processors;
using E3Core.Utility;
using E3NextConfigEditor.Client;
using MonoCore;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace E3NextConfigEditor
{
	public partial class ConfigEditor : Form
	{
		public static DealerClient _tloClient;
		public ConfigEditor()
		{


			InitializeComponent();
			_tloClient = new DealerClient(56725);
			IMQ _mqClient = new MQ.MQClient(_tloClient);

			byte[] result = _tloClient.RequestRawData("${E3.AA.ListAll}");
			SpellDataList aas = SpellDataList.Parser.ParseFrom(result);

			result = _tloClient.RequestRawData("${E3.SpellBook.ListAll}");
			SpellDataList bookSpells = SpellDataList.Parser.ParseFrom(result);

			result = _tloClient.RequestRawData("${E3.Discs.ListAll}");
			SpellDataList discs = SpellDataList.Parser.ParseFrom(result);


			E3.MQ = _mqClient;
			E3.Log = new Logging(E3.MQ); ;
			E3.CurrentName = _mqClient.Query<string>("${Me.CleanName}");
			E3.ServerName = e3util.FormatServerName(_mqClient.Query<string>("${MacroQuest.Server}"));

			_tloClient.RequestData("${E3.TLO.BulkBegin}");
			//create settings files here
			bool mergeUpdates = false;
			E3.CharacterSettings = new E3Core.Settings.CharacterSettings(mergeUpdates);
			_tloClient.RequestData("${E3.TLO.BulkEnd}");


			Debug.WriteLine("TEST");

		}
	}
}
