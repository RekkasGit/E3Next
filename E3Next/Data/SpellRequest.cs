using IniParser.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace E3Core.Data
{
    public class SpellRequest : Spell
    {
        public SpellRequest()
        {
            
        }
        public SpellRequest(string spellName, IniData parsedData = null):base(spellName, parsedData)
        {

        }
		public static new SpellRequest FromProto(SpellData source)
		{
			SpellRequest r = new SpellRequest();
			r.AfterEvent = source.AfterEvent;
			r.AfterSpell = source.AfterSpell;
			r.AllowSpellSwap = source.AllowSpellSwap;
			r.BeforeEvent = source.BeforeEvent;
			r.BeforeSpell = source.BeforeSpell;
			r.CastID = source.CastID;
			r.CastIF = source.CastIF;
			r.CastInvis = source.CastInvis;
			r.CastName = source.CastName;
			r.CastTarget = source.CastTarget;
			r.CastType = (CastingType)source.CastType;
			r.Category = source.Category;
			r.Debug = source.Debug;
			r.Delay = source.Delay;
			r.AfterCastCompletedDelay = source.AfterCastCompletedDelay;
			r.Duration = source.Duration;
			r.DurationTotalSeconds = source.DurationTotalSeconds;
			r.EnduranceCost = source.EnduranceCost;
			r.GiftOfMana = source.GiftOfMana;
			r.GiveUpTimer = source.GiveUpTimer;
			r.HealPct = source.HealPct;
			r.HealthMax = source.HealthMax;
			r.Ifs = source.Ifs;
			r.IfsKeys = source.IfsKeys;
			r.IgnoreStackRules = source.IgnoreStackRules;
			r.InitName = source.InitName;
			r.IsDebuff = source.IsDebuff;
			r.IsDoT = source.IsDoT;
			r.IsBuff = source.IsBuff;
			r.IsShortBuff = source.IsShortBuff;
			r.ItemMustEquip = source.ItemMustEquip;
			r.Mana = source.Mana;
			r.MaxMana = source.MaxMana;
			r.MaxTries = source.MaxTries;
			r.MinDurationBeforeRecast = source.MinDurationBeforeRecast;
			r.MinEnd = source.MinEnd;
			r.MinHP = source.MinHP;
			r.MinMana = source.MinMana;
			r.MinSick = source.MinSick;
			r.Mode = source.Mode;
			r.MyCastTime = (Decimal)source.MyCastTime;
			r.MyCastTimeInSeconds = (Decimal)source.MyCastTimeInSeconds;
			r.MyRange = source.MyRange;
			r.NoAggro = source.NoAggro;
			r.NoBurn = source.NoBurn;
			r.NoEarlyRecast = source.NoEarlyRecast;
			r.NoInterrupt = source.NoInterrupt;
			r.NoMidSongCast = source.NoMidSongCast;
			r.NoStack = source.NoStack;
			r.NoTarget = source.NoTarget;
			r.PctAggro = source.PctAggro;
			r.Reagent = source.Reagent;
			r.ReagentOutOfStock = source.ReagentOutOfStock;
			r.RecastTime = source.RecastTime;
			r.RecoveryTime = (Decimal)source.RecoveryTime;
			r.Rotate = source.Rotate;
			r.SpellGem = source.SpellGem;
			r.SpellIcon = source.SpellIcon;
			r.SpellID = source.SpellID;
			r.SpellInBook = source.SpellInBook;
			r.SpellName = source.SpellName;
			r.SpellType = source.SpellType;
			r.StackRecastDelay = source.StackRecastDelay;
			r.StackRequestItem = source.StackRequestItem;
			r.StackRequestTargets.AddRange(source.StackRequestTargets);
			r.Subcategory = source.Subcategory;
			r.TargetType = source.TargetType;
			r.TriggerSpell = source.TriggerSpell;
			r.Zone = source.Zone;
			r.Level = source.Level;
			r.Description = source.Description;
			r.ResistType = source.ResistType;
			r.ResistAdj = source.ResistAdj;
			r.CastTypeOverride = (CastingType)source.CastTypeOverride;
			foreach (var entry in source.CheckForCollection)
			{
				if (!r.CheckForCollection.ContainsKey(entry))
				{
					r.CheckForCollection.Add(entry, 0);
				}
			}
			r.Enabled = source.Enabled;

			return r;
		}
		public Int64 LastRequestTimeStamp;
    }
}
