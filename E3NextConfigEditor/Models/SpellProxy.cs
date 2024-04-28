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
	public class SpellProxy
	{
		private Spell _spell;
		public SpellProxy(E3Core.Data.Spell spell) 
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
		[Category("Spell Data")]
		[Description("Description")]
		public String Description
		{
			get { return _spell.Description; }

		}
		[Category("Spell Data")]
		[Description("ResistType")]
		public String ResistType
		{
			get { return _spell.ResistType; }

		}
		[Category("Spell Data")]
		[Description("Resist Adjustment")]
		public Int32 ResistAdj
		{
			get { return _spell.ResistAdj; }

		}
		[Category("Flags")]
		[Description("Prevent this from being interrupted")]
		public bool NoInterrupt
		{
			get { return _spell.NoInterrupt; }
			set { _spell.NoInterrupt = value; }
		}

	}
	public class SpellDataProxy
	{
		private SpellData _spell;
		public SpellDataProxy(SpellData spell)
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
		[Category("Spell Data")]
		[Description("Description")]
		public String Description
		{
			get { return _spell.Description; }

		}
		[Category("Spell Data")]
		[Description("ResistType")]
		public String ResistType
		{
			get { return _spell.ResistType; }

		}
		[Category("Spell Data")]
		[Description("Resist Adjustment")]
		public Int32 ResistAdj
		{
			get { return _spell.ResistAdj; }

		}
		[Category("Spell Data")]
		[Description("Level")]
		public Int32 Level
		{
			get { return _spell.Level; }

		}
		[Category("Flags")]
		[Description("Prevent this from being interrupted")]
		public bool NoInterrupt
		{
			get { return _spell.NoInterrupt; }
			set { _spell.NoInterrupt = value; }
		}

	}
	public class SpellRequestDataProxy:SpellProxy
	{
		private SpellRequest _spell;

		public SpellRequestDataProxy(SpellRequest spell) : base(spell)
		{
			_spell = spell;

		}
	}
}
