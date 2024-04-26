using E3Core.Data;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace E3NextConfigEditor.Models
{
	public class SpellDataProxy
	{
		private Spell _spell;
		public SpellDataProxy(E3Core.Data.Spell spell) 
		{
			_spell = spell;
		
		}
		[Category("Spell Data")]
		[Description("Spell Name")]
		public string SpellName
		{
			get { return _spell.SpellName; }
			
		}
		[Category("Spell Data")]
		[Description("Icon assoicated with the spell data")]
		public Int32 SpellIcon
		{
			get { return _spell.SpellIcon; }

		}

		[Category("Flags")]
		[Description("Prevent this from being interrupted")]
		public bool NoInterrupt
		{
			get { return _spell.NoInterrupt; }
			set { _spell.NoInterrupt = value; }
		}

	}
}
