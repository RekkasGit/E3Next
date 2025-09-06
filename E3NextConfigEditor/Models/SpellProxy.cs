using E3Core.Data;
using Google.Protobuf.WellKnownTypes;
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
		[Description("Spell Type")]
		public string SpellType
		{
			get { return _spell.SpellType; }

		}
		[Category("Spell Data")]
		[Description("Spell Target Type")]
		public string TargetType
		{
			get { return _spell.TargetType; }

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
		[Category("Spell Data")]
		[Description("Recast")]
		public Int32 Recast
		{
			get { return _spell.RecastTime; }

		}
		[Category("Spell Slot Data")]
		[Description("Slot01")]
		public string Slot01
		{
			get { return _spell.SpellEffects.Count > 0 ? _spell.SpellEffects[0] : String.Empty;}

		}
		[Category("Spell Slot Data")]
		[Description("Slot02")]
		public string Slot02
		{
			get { return _spell.SpellEffects.Count > 1 ? _spell.SpellEffects[1] : String.Empty; }

		}
		[Category("Spell Slot Data")]
		[Description("Slot03")]
		public string Slot03
		{
			get { return _spell.SpellEffects.Count > 2 ? _spell.SpellEffects[2] : String.Empty; }

		}
		[Category("Spell Slot Data")]
		[Description("Slot04")]
		public string Slot04
		{
			get { return _spell.SpellEffects.Count > 3 ? _spell.SpellEffects[3] : String.Empty; }

		}
		[Category("Spell Slot Data")]
		[Description("Slot05")]
		public string Slot05
		{
			get { return _spell.SpellEffects.Count > 4 ? _spell.SpellEffects[4] : String.Empty; }

		}
		[Category("Spell Slot Data")]
		[Description("Slot06")]
		public string Slot06
		{
			get { return _spell.SpellEffects.Count > 5 ? _spell.SpellEffects[5] : String.Empty; }

		}
		[Category("Spell Slot Data")]
		[Description("Slot07")]
		public string Slot07
		{
			get { return _spell.SpellEffects.Count > 6 ? _spell.SpellEffects[6] : String.Empty; }

		}
		[Category("Spell Slot Data")]
		[Description("Slot08")]
		public string Slot08
		{
			get { return _spell.SpellEffects.Count > 7 ? _spell.SpellEffects[7] : String.Empty; }

		}
		[Category("Spell Slot Data")]
		[Description("Slot09")]
		public string Slot09
		{
			get { return _spell.SpellEffects.Count > 8 ? _spell.SpellEffects[8] : String.Empty; }

		}
		[Category("Spell Slot Data")]
		[Description("Slot10")]
		public string Slot10
		{
			get { return _spell.SpellEffects.Count > 9 ? _spell.SpellEffects[9] : String.Empty; }

		}
		[Category("Spell Slot Data")]
		[Description("Slot11")]
		public string Slot11
		{
			get { return _spell.SpellEffects.Count > 10 ? _spell.SpellEffects[10] : String.Empty; }

		}
		[Category("Spell Slot Data")]
		[Description("Slot12")]
		public string Slot12
		{
			get { return _spell.SpellEffects.Count > 11 ? _spell.SpellEffects[11] : String.Empty; }

		}
		[Category("Flags")]
		[Description("PctAggro you have to be at before this ability will fire")]
		public Int32 PctAggro
		{
			get { return _spell.PctAggro; }
			set { _spell.PctAggro = value; }
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
		public string Ifs
		{
			get { return _spell.IfsKeys; }
			set { _spell.IfsKeys = value; }
		}
		[Category("Flags")]
		[Description("Delay, in seconds")]
		public string Delay
		{
			get { 
				
				
				return $"{_spell.Delay}"; 
			
			
			
			}
			set { 
				
				string tvalue = value;
				bool isMinute = false;
				if (value.EndsWith("s", StringComparison.OrdinalIgnoreCase))
				{
					tvalue = tvalue.Substring(0, value.Length - 1);
				}
				else if (value.EndsWith("m", StringComparison.OrdinalIgnoreCase))
				{
					isMinute = true;
					tvalue = tvalue.Substring(0, value.Length - 1);
				}
				_spell.Delay = Int32.Parse(tvalue);
				if (isMinute)
				{
					_spell.Delay=_spell.Delay * 60;
				}

			}
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
		[Description("Class short name, comma seperated. Classes to exclude in your buff if your using GBots/Bots")]
		public string ExcludedClasses
		{
			get { return String.Join(",", _spell.ExcludedClasses.ToList()); }
			set
			{
				if (!String.IsNullOrWhiteSpace(value))
				{
					string[] splitArray = value.Split(',');
					_spell.ExcludedClasses.Clear();
					foreach (var spell in splitArray)
					{
						_spell.ExcludedClasses.Add(spell);
					}
				}
				else
				{
					_spell.ExcludedClasses.Clear();
				}
			}
		}
		[Description("Toon Name, comma seperated. Names to exclude in your buff if your using GBots/Bots")]
		public string ExcludedNames
		{
			get { return String.Join(",", _spell.ExcludedNames.ToList()); }
			set
			{
				if (!String.IsNullOrWhiteSpace(value))
				{
					string[] splitArray = value.Split(',');
					_spell.ExcludedNames.Clear();
					foreach (var spell in splitArray)
					{
						_spell.ExcludedNames.Add(spell);
					}
				}
				else
				{
					_spell.ExcludedNames.Clear();
				}
			}
		}
		[Category("Spell Gem Flags")]
		[Description("Spell Gem")]
		public Int32 SpellGem
		{
			get { return _spell.SpellGem; }
			set { _spell.SpellGem = value; }
		}
		[Category("Spell Target Flags")]
		[Description("Spell Target. Self or Name of toon is valid")]
		public String CastTarget
		{
			get { return _spell.CastTarget; }
			set { _spell.CastTarget = value; }
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
		public string BeforeSpell
		{
			get { return _spell.BeforeSpell; }
			set { _spell.BeforeSpell = value; }
		}
		[Category("Flags")]
		[Description("After Event Name, follows normal heircy rules")]
		public string AfterEvent
		{
			get { return _spell.AfterEventKeys; }
			set { _spell.AfterEventKeys = value; }
		}
		[Category("Flags")]
		[Description("Before Event Name, follows normal heircy rules")]
		public string BeforeEvent
		{
			get { return _spell.BeforeEventKeys; }
			set { _spell.BeforeEventKeys = value; }
		}
		[Category("Flags")]
		[Description("After Event Delay in milliseconds")]
		public Int32 AfterEventDelay
		{
			get { return _spell.AfterEventDelay; }
			set { _spell.AfterEventDelay = value; }
		}
		[Category("Flags")]
		[Description("After Spell Delay in milliseconds")]
		public Int32 AfterSpellDelay
		{
			get { return _spell.AfterSpellDelay; }
			set { _spell.AfterSpellDelay = value; }
		}
		
		[Category("Flags")]
		[Description("Before Event Delay in milliseconds")]
		public Int32 BeforeEventDelay
		{
			get { return _spell.BeforeEventDelay; }
			set { _spell.BeforeEventDelay = value; }
		}
		[Category("Flags")]
		[Description("Before Spell Delay in milliseconds")]
		public Int32 BeforeSpellDelay
		{
			get { return _spell.BeforeSpellDelay; }
			set { _spell.BeforeSpellDelay = value; }
		}
		[Category("Flags")]
		[Description("After Spell Delay in milliseconds. This is after cast and before the spell window closes.")]
		public Int32 AfterCastDelay
		{
			get { return _spell.AfterCastDelay; }
			set { _spell.AfterCastDelay = value; }
		}
		[Category("Flags")]
		[Description("After Spell Delay in milliseconds. This is after cast and after the spell window closes.")]
		public Int32 AfterCastCompletedDelay
		{
			get { return _spell.AfterCastCompletedDelay; }
			set { _spell.AfterCastCompletedDelay = value; }
		}
	
		[Category("Flags")]
		[Description("Give a no target hint to E3N to not swap targets to use the spell.")]
		public bool NoTarget
		{
			get { return _spell.NoTarget; }
			set { _spell.NoTarget = value; }
		}
		[Category("Flags")]
		[Description("Do not execute if you have aggro on the current target")]
		public bool NoAggro
		{
			get { return _spell.NoAggro; }
			set { _spell.NoAggro = value; }
		}
		[Category("Flags")]
		[Description("Max Tries to execute on a debuff/dot")]
		public Int32 MaxTries
		{
			get { return _spell.MaxTries; }
			set { _spell.MaxTries = value; }
		}
		[Category("Flags")]
		[Description("Zone to Enable in. Honetly not sure this works!")]
		public string Zone
		{
			get { return _spell.Zone; }
			set { _spell.Zone = value; }
		}
		[Category("Heal Flags")]
		[Description("For heals, Heal Percentage you start casting")]
		public Int32 HealPct
		{
			get { return _spell.HealPct; }
			set { _spell.HealPct = value; }
		}
		[Category("Heal Flags")]
		[Description("For Heals, cancel heal if above this health level")]
		public Int32 HealthMax
		{
			get { return _spell.HealthMax; }
			set { _spell.HealthMax = value; }
		}
		[Category("Cure Flags")]
		[Description("Min sickness before cast, only used for cures")]
		public Int32 MinSick
		{
			get { return _spell.MinSick; }
			set { _spell.MinSick = value; }
		}
		[Category("Flags")]
		[Description("Minimum mana level before try and cast the spell")]
		public Int32 MinMana
		{
			get { return _spell.MinMana; }
			set { _spell.MinMana = value; }
		}
		[Category("Flags")]
		[Description("Minimum hp % level before try and cast the spell")]
		public Int32 MinHP
		{
			get { return _spell.MinHP; }
			set { _spell.MinHP = value; }
		}
		[Category("Flags")]
		[Description("Minimum HP Total before try and cast the spell")]
		public Int32 MinHPTotal
		{
			get { return _spell.MinHPTotal; }
			set { _spell.MinHPTotal = value; }
		}
		[Category("Flags")]
		[Description("Min duration before recast in seconds")]
		public Int64 MinDurationBeforeRecast
		{
			get { return _spell.MinDurationBeforeRecast /1000; }
			set { _spell.MinDurationBeforeRecast = value * 1000; }
		}
		[Category("Flags")]
		[Description("Don't cast spell if you are above this mana level")]
		public Int32 MaxMana
		{
			get { return _spell.MaxMana; }
			set { _spell.MaxMana = value; }
		}
		[Category("Flags")]
		[Description("Song min time left before recasting song, in seconds")]
		public Int32 SongRefreshTime
		{
			get { return _spell.SongRefreshTime; }
			set { _spell.SongRefreshTime = value; }
		}
		[Category("Flags")]
		[Description("Min endurance before you try and cast an ability")]
		public Int32 MinEnd
		{
			get { return _spell.MinEnd; }
			set { _spell.MinEnd = value; }
		}
		[Category("Flags")]
		[Description("Don't use internal stacking rules for debuffs")]
		public bool IgnoreStackRules
		{
			get { return _spell.IgnoreStackRules; }
			set { _spell.IgnoreStackRules = value; }
		}
		[Category("Flags")]
		[Description("Cast if this debuff/debuff exists")]
		public string CastIf
		{
			get { return _spell.CastIF; }
			set { _spell.CastIF = value; }
		}
		[Category("Flags")]
		[Description("Cast type, valid are Spell/AA/Item/Disc")]
		public string CastType
		{
			get { return _spell.CastType.ToString(); }
			
		}
		[Category("Flags")]
		[Description("Cast type, valid are Spell/AA/Item/Disc. Use None disable")]
		public string CastTypeOverride
		{
			get { return _spell.CastTypeOverride.ToString(); }
			set { System.Enum.TryParse(value, true, out _spell.CastTypeOverride); }
		}
		[Category("Spell Enabled")]
		[Description("if disabled, the spell will not be cast")]
		public bool Enabled
		{
			get { return _spell.Enabled; }
			set { _spell.Enabled = value; }
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
		[Description("Spell Type")]
		public string SpellType
		{
			get { return _spell.SpellType; }

		}
		[Category("Spell Data")]
		[Description("Spell Target Type")]
		public string TargetType
		{
			get { return _spell.TargetType; }

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
		[Category("Spell Data")]
		[Description("Recast")]
		public Int32 Recast
		{
			get { return _spell.RecastTime; }

		}
		[Category("Spell Slot Data")]
		[Description("Slot01")]
		public string Slot01
		{
			get { return _spell.SpellEffects.Count > 0 ? _spell.SpellEffects[0] : String.Empty; }

		}
		[Category("Spell Slot Data")]
		[Description("Slot02")]
		public string Slot02
		{
			get { return _spell.SpellEffects.Count > 1 ? _spell.SpellEffects[1] : String.Empty; }

		}
		[Category("Spell Slot Data")]
		[Description("Slot03")]
		public string Slot03
		{
			get { return _spell.SpellEffects.Count > 2 ? _spell.SpellEffects[2] : String.Empty; }

		}
		[Category("Spell Slot Data")]
		[Description("Slot04")]
		public string Slot04
		{
			get { return _spell.SpellEffects.Count > 3 ? _spell.SpellEffects[3] : String.Empty; }

		}
		[Category("Spell Slot Data")]
		[Description("Slot05")]
		public string Slot05
		{
			get { return _spell.SpellEffects.Count > 4 ? _spell.SpellEffects[4] : String.Empty; }

		}
		[Category("Spell Slot Data")]
		[Description("Slot06")]
		public string Slot06
		{
			get { return _spell.SpellEffects.Count > 5 ? _spell.SpellEffects[5] : String.Empty; }

		}
		[Category("Spell Slot Data")]
		[Description("Slot07")]
		public string Slot07
		{
			get { return _spell.SpellEffects.Count > 6 ? _spell.SpellEffects[6] : String.Empty; }

		}
		[Category("Spell Slot Data")]
		[Description("Slot08")]
		public string Slot08
		{
			get { return _spell.SpellEffects.Count > 7 ? _spell.SpellEffects[7] : String.Empty; }

		}
		[Category("Spell Slot Data")]
		[Description("Slot09")]
		public string Slot09
		{
			get { return _spell.SpellEffects.Count > 8 ? _spell.SpellEffects[8] : String.Empty; }

		}
		[Category("Spell Slot Data")]
		[Description("Slot10")]
		public string Slot10
		{
			get { return _spell.SpellEffects.Count > 9 ? _spell.SpellEffects[9] : String.Empty; }

		}
		[Category("Spell Slot Data")]
		[Description("Slot11")]
		public string Slot11
		{
			get { return _spell.SpellEffects.Count > 10 ? _spell.SpellEffects[10] : String.Empty; }

		}
		[Category("Spell Slot Data")]
		[Description("Slot12")]
		public string Slot12
		{
			get { return _spell.SpellEffects.Count > 11 ? _spell.SpellEffects[11] : String.Empty; }

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
		public string Ifs
		{
			get { return _spell.IfsKeys; }
			set { _spell.IfsKeys = value; }
		}
		[Category("Flags")]
		[Description("Give a no target hint to E3N to not swap targets to use the spell.")]
		public bool NoTarget
		{
			get { return _spell.NoTarget; }
			set { _spell.NoTarget = value; }
		}
		[Category("Flags")]
		[Description("Do not execute if you have aggro on the current target")]
		public bool NoAggro
		{
			get { return _spell.NoAggro; }
			set { _spell.NoAggro = value; }
		}
		[Category("Flags")]
		[Description("Max Tries to execute on a debuff/dot")]
		public Int32 MaxTries
		{
			get { return _spell.MaxTries; }
			set { _spell.MaxTries = value; }
		}
		[Category("Flags")]
		[Description("Check for, comma seperated. For Detremental it is the debuff on the mob. For Buffs/songs its the buff on you.")]
		public string CheckFor
		{
			get { return String.Join(",", _spell.CheckForCollection.ToList()); }
			set
			{
				if (!String.IsNullOrWhiteSpace(value))
				{
					string[] splitArray = value.Split(',');
					_spell.CheckForCollection.Clear();
					foreach (var spell in splitArray)
					{
						_spell.CheckForCollection.Add(spell);
					}
				}
				else
				{
					_spell.CheckForCollection.Clear();
				}
			}
		}
		[Description("Class short name, comma seperated. Classes to exclude in your buff if your using GBots/Bots")]
		public string ExcludedClasses
		{
			get { return String.Join(",", _spell.ExcludedClasses.ToList()); }
			set
			{
				if (!String.IsNullOrWhiteSpace(value))
				{
					string[] splitArray = value.Split(',');
					_spell.ExcludedClasses.Clear();
					foreach (var spell in splitArray)
					{
						_spell.ExcludedClasses.Add(spell);
					}
				}
				else
				{
					_spell.ExcludedClasses.Clear();
				}
			}
		}
		[Category("Spell Gem Flags")]
		[Description("Spell Gem")]
		public Int32 SpellGem
		{
			get { return _spell.SpellGem; }
			set { _spell.SpellGem = value; }
		}
		[Category("Spell Target Flags")]
		[Description("Spell Target. Self or Name of toon is valid")]
		public String CastTarget
		{
			get { return _spell.CastTarget; }
			set { _spell.CastTarget = value; }
		}
		[Category("Spell Enabled")]
		[Description("if disabled, the spell will not be cast")]
		public bool Enabled
		{
			get { return _spell.Enabled; }
			set { _spell.Enabled = value; }
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
		public string BeforeSpell
		{
			get { return _spell.BeforeSpell; }
			set { _spell.BeforeSpell = value; }
		}
		[Category("Flags")]
		[Description("Song min time left before recasting song, in seconds")]
		public Int32 SongRefreshTime
		{
			get { return _spell.SongRefreshTime; }
			set { _spell.SongRefreshTime = value; }
		}
		[Category("Flags")]
		[Description("After Event Name, follows normal heircy rules")]
		public string AfterEvent
		{
			get { return _spell.AfterEventKeys; }
			set { _spell.AfterEventKeys = value; }
		}
		[Category("Flags")]
		[Description("After Event Delay in milliseconds")]
		public Int32 AfterEventDelay
		{
			get { return _spell.AfterEventDelay; }
			set { _spell.AfterEventDelay = value; }
		}
		[Category("Flags")]
		[Description("After Spell Delay in milliseconds")]
		public Int32 AfterSpellDelay
		{
			get { return _spell.AfterSpellDelay; }
			set { _spell.AfterSpellDelay = value; }
		}

		[Category("Flags")]
		[Description("Before Event Delay in milliseconds")]
		public Int32 BeforeEventDelay
		{
			get { return _spell.BeforeEventDelay; }
			set { _spell.BeforeEventDelay = value; }
		}
		[Category("Flags")]
		[Description("Before Spell Delay in milliseconds")]
		public Int32 BeforeSpellDelay
		{
			get { return _spell.BeforeSpellDelay; }
			set { _spell.BeforeSpellDelay = value; }
		}
		[Category("Flags")]
		[Description("After Spell Delay in milliseconds. This is after cast and before the spell window closes.")]
		public Int32 AfterCastDelay
		{
			get { return _spell.AfterCastDelay; }
			set { _spell.AfterCastDelay = value; }
		}
		[Category("Flags")]
		[Description("After Spell Delay in milliseconds. This is after cast and after the spell window closes.")]
		public Int32 AfterCastCompletedDelay
		{
			get { return _spell.AfterCastCompletedDelay; }
			set { _spell.AfterCastCompletedDelay = value; }
		}
		[Category("Flags")]
		[Description("Before Event Name, follows normal heircy rules")]
		public string BeforeEvent
		{
			get { return _spell.BeforeEventKeys; }
			set { _spell.BeforeEventKeys = value; }
		}
		[Category("Flags")]
		[Description("Zone to Enable in. Honetly not sure this works!")]
		public string Zone
		{
			get { return _spell.Zone; }
			set { _spell.Zone = value; }
		}
		[Category("Flags")]
		[Description("Delay, in seconds")]
		public string Delay
		{
			get
			{
				return $"{_spell.Delay}";
			}
			set
			{

				string tvalue = value;
				bool isMinute = false;
				if (value.EndsWith("s", StringComparison.OrdinalIgnoreCase))
				{
					tvalue = tvalue.Substring(0, value.Length - 1);
				}
				else if (value.EndsWith("m", StringComparison.OrdinalIgnoreCase))
				{
					isMinute = true;
					tvalue = tvalue.Substring(0, value.Length - 1);
				}
				_spell.Delay = Int32.Parse(tvalue);
				if (isMinute)
				{
					_spell.Delay = _spell.Delay * 60;
				}
			}
		}
		[Category("Heal Flags")]
		[Description("For heals, Heal Percentage you start casting")]
		public Int32 HealPct
		{
			get { return _spell.HealPct; }
			set { _spell.HealPct = value; }
		}
		[Category("Heal Flags")]
		[Description("For Heals, cancel heal if above this health level")]
		public Int32 HealthMax
		{
			get { return _spell.HealthMax; }
			set { _spell.HealthMax = value; }
		}
		[Category("Cure Flags")]
		[Description("Min sickness before cast, only used for cures")]
		public Int32 MinSick
		{
			get { return _spell.MinSick; }
			set { _spell.MinSick = value; }
		}
		[Category("Flags")]
		[Description("PctAggro you have to be at before this ability will fire")]
		public Int32 PctAggro
		{
			get { return _spell.PctAggro; }
			set { _spell.PctAggro = value; }
		}
		[Category("Flags")]
		[Description("Minimum mana level before try and cast the spell")]
		public Int32 MinMana
		{
			get { return _spell.MinMana; }
			set { _spell.MinMana = value; }
		}
		[Category("Flags")]
		[Description("Minimum hp % level before try and cast the spell")]
		public Int32 MinHP
		{
			get { return _spell.MinHP; }
			set { _spell.MinHP = value; }
		}
		[Category("Flags")]
		[Description("Minimum HP Total before try and cast the spell")]
		public Int32 MinHPTotal
		{
			get { return _spell.MinHPTotal; }
			set { _spell.MinHPTotal = value; }
		}
		[Category("Flags")]
		[Description("Don't cast spell if you are above this mana level")]
		public Int32 MaxMana
		{
			get { return _spell.MaxMana; }
			set { _spell.MaxMana = value; }
		}
		[Category("Flags")]
		[Description("Min duration before recast in seconds")]
		public Int64 MinDurationBeforeRecast
		{
			get { return _spell.MinDurationBeforeRecast / 1000; }
			set { _spell.MinDurationBeforeRecast = value * 1000; }
		}
		[Category("Flags")]
		[Description("Min endurance before you try and cast an ability")]
		public Int32 MinEnd
		{
			get { return _spell.MinEnd; }
			set { _spell.MinEnd = value; }
		}
		[Category("Flags")]
		[Description("Don't use internal stacking rules for debuffs")]
		public bool IgnoreStackRules
		{
			get { return _spell.IgnoreStackRules; }
			set { _spell.IgnoreStackRules = value; }
		}
		[Category("Flags")]
		[Description("Cast if this debuff/debuff exists")]
		public string CastIf
		{
			get { return _spell.CastIF; }
			set { _spell.CastIF = value; }
		}
		[Category("Flags")]
		[Description("Cast type, valid are Spell/AA/Item/Disc")]
		public string CastType
		{
			get { return _spell.CastType.ToString(); }
			
		}
		[Category("Flags")]
		[Description("Cast type, valid are Spell/AA/Item/Disc. Use None disable")]
		public string CastTypeOverride
		{
			get { return _spell.CastTypeOverride.ToString(); }
			set
			{
				CastingType tempType = CastingType.None;
				if (System.Enum.TryParse(value, true, out tempType))
				{
					_spell.CastTypeOverride = (SpellData.Types.CastingType)tempType;
				}

			}
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
