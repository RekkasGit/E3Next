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
		[Category("Flags")]
		[Description("Ifs Keys to be used, comma seperated")]
		public string IfsKeys
		{
			get { return _spell.IfsKeys; }
			set { _spell.IfsKeys = value; }
		}
		[Category("Flags")]
		[Description("Zone to be active in")]
		public string Zone
		{
			get { return _spell.Zone; }
			set { _spell.Zone = value; }
		}
		[Category("Flags")]
		[Description("Check for, comma seperated. For Detremental it is the debuff on the mob. For Buffs/songs its the buff on you.")]
		public string CheckFor
		{
			get { return String.Join(",", _spell.CheckForCollection.Keys.ToList()); }
			set
			{
				if(!String.IsNullOrWhiteSpace(value))
				{
					string[] splitArray = value.Split(',');
					_spell.CheckForCollection.Clear();
					foreach(var spell in splitArray)
					{
						_spell.CheckForCollection.Add(spell, 0);
					}
				}
				else
				{
					_spell.CheckForCollection.Clear();
				}
			}
		}
		[Category("Flags")]
		[Description("Spell Gem")]
		public Int32 SpellGem
		{
			get { return _spell.SpellGem; }
			set { _spell.SpellGem = value; }
		}
		[Category("Flags")]
		[Description("Heal Percentage, only valid for heal sections")]
		public Int32 HealPct
		{
			get { return _spell.HealPct; }
			set { _spell.HealPct = value; }
		}
		[Category("Flags")]
		[Description("After Spell Name, follows normal heircy rules")]
		public string AfterSpell
		{
			get { return _spell.AfterSpell; }
			set { _spell.AfterSpell = value; }
		}
		[Category("Flags")]
		[Description("Before Spell Name, follows normal heircy rules")]
		public string Before
		{
			get { return _spell.BeforeSpell; }
			set { _spell.BeforeSpell = value; }
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
