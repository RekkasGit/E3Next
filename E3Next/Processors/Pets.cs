using E3Core.Data;
using E3Core.Settings;
using E3Core.Utility;
using MonoCore;
using System;
using System.Collections.Generic;
using System.Diagnostics.SymbolStore;
using System.Linq;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace E3Core.Processors
{
    public static class Pets
    {
        public static Logging _log = E3.Log;
        private static IMQ MQ = E3.MQ;
		[ExposedData("Pets", "PetMaxShrink")]
		private static bool _petMaxShrink = false;
        private static Int32 _petMaxShrinkID = 0;
        private static Int64 _nextPetCheck = 0;
        private static Int64 _nextPetCheckInterval = 1000;
		[ExposedData("Pets", "PetShrinkSpells")]
		private static List<string> _petShrinkSpells = new List<string>() { "Diminutive Companion", "Gemstone of Dark Flame", "Symbol of Ancient Summoning", "Tiny Companion",  };

        [SubSystemInit]
        public static void Pets_Init()
        {
            RegisterEvents();
        }

        private static void RegisterEvents()
        {
        }

        public static void Reset()
        {
            _petMaxShrink = false;
            _petMaxShrinkID = 0;

            //try out hold if on enc, then after 3 sec try ghold
			if (E3.CurrentClass == Class.Enchanter)
            {
				MQ.Cmd("/squelch /pet hold on");
				MQ.Cmd("/timed 30 /squelch /pet ghold on");
			}
            else
            {
				MQ.Cmd("/timed 30 /squelch /pet ghold on");
			}
         
		}

        [AdvSettingInvoke]
        public static void Check_Pets()
        {
            if (E3.IsInvis) return;
            if (Basics.AmIDead()) return;
            if (!e3util.ShouldCheck(ref _nextPetCheck, _nextPetCheckInterval)) return;

            Int32 petId = MQ.Query<Int32>("${Me.Pet.ID}");


            if (petId > 0)
            {
                CheckPetHeal(petId);
                CheckPetShrink(petId);
                CheckPetBuffs();
            }

			if (Basics.InCombat() && !E3.CharacterSettings.Pet_SummonCombat)
			{
				return;
			}
			if (petId<1)
			{	
				CheckPetSummon(ref petId);
			}
		}

        public static void removeBuffsIfNecessary(List<Spell> buffs) {
            foreach (var buff in buffs) {
		    if (!String.IsNullOrWhiteSpace(buff.Ifs))
                {
                    if (!Casting.Ifs(buff.Ifs))
                    {
                        continue;
                    }
                }
                var buffIndex = MQ.Query<int>($"${{Me.Pet.Buff[{buff.SpellName}]}}");
                if (buffIndex > 0)
                {
                    MQ.Cmd($"/removebuff -pet {buff.SpellName}");
                }
            }
        }

        public static void CheckPetBuffs()
        {
            Pets.removeBuffsIfNecessary(E3.CharacterSettings.BlockedPetBuffs);
        }

        private static void CheckPetSummon(ref Int32 petID)
        {
            if (petID == 0 && E3.CharacterSettings.PetSpell.Count > 0)
            {
                bool castPet = false;
                //we have no pet, do we have a pet configurd to summon?               
                foreach (var spell in E3.CharacterSettings.PetSpell)
                {
					if (!spell.Enabled) continue;
					if (!String.IsNullOrWhiteSpace(spell.Ifs))
                    {
                        if (!Casting.Ifs(spell))
                        {
                            continue;
                        }
                    }

                    if (Casting.CheckMana(spell) && Casting.CheckReady(spell))
                    {
                        Casting.Cast(0, spell);
                        castPet = true;
                        break;
                    }
                }
                if(castPet)
                {
                    //wait for the pet to appear
                    MQ.Delay(1000);
                    petID = MQ.Query<Int32>("${Me.Pet.ID}");
                    if (petID > 0)
                    {
						MQ.Cmd("/squelch /pet hold on");
						MQ.Cmd("/squelch /pet ghold on");
                    }
                }
              
            }
        }
        private static void CheckPetHeal(Int32 petID)
        {
            Int32 pctHps = MQ.Query<Int32>("${Me.Pet.PctHPs}");
            Int32 pctMendHps = E3.CharacterSettings.Pet_MendPercent;
            if (pctHps < pctMendHps)
            {
                if (MQ.Query<bool>("${Me.AltAbilityReady[Replenish Companion]}"))
                {
                    Spell s;
                    if (!Spell.LoadedSpellsByName.TryGetValue("Replenish Companion", out s))
                    {
                        s = new Spell("Replenish Companion");
                    }
                    if (s.SpellID > 0)
                    {
                        Casting.Cast(petID, s);
                        return;
                    }
                }
            }
            foreach (var spell in E3.CharacterSettings.PetHeals)
            {
				if (!spell.Enabled) continue;
				if (pctHps <= spell.HealPct)
                {
                    if (Casting.CheckMana(spell) && Casting.CheckReady(spell))
                    {
                        Casting.Cast(petID, spell);
                        return;
                    }
                }
            }
        }

        private static void CheckPetShrink(Int32 petID)
        {
            if (!E3.CharacterSettings.Pet_AutoShrink) return;
            if (petID != _petMaxShrinkID)
            {
                _petMaxShrinkID = petID;
                _petMaxShrink = false;
            }
            double petHeight = MQ.Query<double>("${Me.Pet.Height}");


            if (_petMaxShrink) return;

            if (petHeight > 1 && petHeight != 0)
            {
                foreach (var spellName in _petShrinkSpells)
                {
					Spell s;
                    if (!Spell.LoadedSpellsByName.TryGetValue(spellName, out s))
                    {
                        s = new Spell(spellName);
                    }
                    if (s.SpellID > 0 && s.CastType != CastingType.None)
                    {
                        Casting.Cast(petID, s);
                        MQ.Delay(300);
                        double newPetHeight = MQ.Query<double>("${Me.Pet.Height}");

                        if (newPetHeight == petHeight)
                        {
                            _petMaxShrink = true;
                            _petMaxShrinkID = petID;
                        }

                        return;
                    }
                }
            }
        }
    }
}
