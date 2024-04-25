using E3NextConfigEditor.Client;
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
			_tloClient = new DealerClient(64440);




			string jsonresponse = _tloClient.RequestData("${E3.AA.ListAll}");

			Debug.WriteLine(jsonresponse);


		}
	}
}
