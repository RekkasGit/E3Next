using E3Core.Data;
using System.ComponentModel;


namespace E3NextConfigEditor.Models
{
	public class MelodyIfProxy
	{

		private MelodyIfs _melodyIf;
		public MelodyIfProxy(MelodyIfs spell)
		{
			_melodyIf = spell;

		}
		[Category("Melody Data")]
		[Description("Spell Name")]
		public string MelodyName
		{
			get { return _melodyIf.MelodyName; }

		}
		[Category("Melody Ifs Data")]
		[Description("Melody If Key Name")]
		public string IfsKey
		{
			get { return _melodyIf.MelodyIfName; }
			set { _melodyIf.MelodyIfName = value; }
		}
	}
}
