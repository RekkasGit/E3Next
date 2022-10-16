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

        public static Logging _log = E3._log;
        private static IMQ MQ = E3.MQ;
        private static Int64 _nextPetCheck = 0;
        private static Int64 _nextPetCheckInterval = 1000;

        public static void Init()
        {
            RegisterEvents();
        }

        private static void RegisterEvents()
        {




        }
        [ClassInvoke(Data.Class.PetClass)]
        public static void Check_Pets()
        { 
            if (!e3util.ShouldCheck(ref _nextPetCheck, _nextPetCheckInterval)) return;
            //don't check if invs
            if (E3._isInvis) return;

            Int32 petId = MQ.Query<Int32>("${Me.Pet.ID}");

            CheckPetSummon(ref petId);

            if(petId>0)
            {
                CheckPetHeal(petId);
                CheckPetShrink(petId);
            }



        }
        private static void CheckPetSummon( ref Int32 petID)
        {
            if (petID == 0 && E3._characterSettings.PetSpell.Count>0)
            {
                //we have no pet, do we have a pet configurd to summon?
               
                foreach (var spell in E3._characterSettings.PetSpell)
                {
                    if (Casting.CheckReady(spell) && Casting.CheckMana(spell))
                    {
                        Casting.Cast(0, spell);
                        break;
                    }
                }
               
                //wait for the pet to appear
                MQ.Delay(1000);
                petID = MQ.Query<Int32>("${Me.Pet.ID}");
                if (petID > 0)
                {
                    MQ.Cmd("/squelch /pet ghold on");
                }
            }
        }
        private static void CheckPetHeal(Int32 petID)
        {

            Int32 pctHps = MQ.Query<Int32>("${Me.Pet.PctHPs}");
            Int32 pctMendHps = E3._characterSettings.Pet_MendPercent;
            if(pctHps<pctMendHps)
            {
                if(MQ.Query<bool>("${Me.AltAbilityReady[Replenish Companion]}"))
                {
                    Spell s;
                    if(!Spell._loadedSpellsByName.TryGetValue("Replenish Companion", out s))
                    {
                        s = new Spell("Replenish Companion");
                    }
                    if(s.SpellID>0)
                    {
                        Casting.Cast(petID, s);
                        return;
                    }
                }
               
            }
            foreach (var spell in E3._characterSettings.PetHeals)
            {
                if (pctHps <= spell.HealPct)
                {
                    if(Casting.CheckReady(spell) && Casting.CheckMana(spell))
                    {
                        Casting.Cast(petID, spell);
                        return;
                    }
                }
            }
        }
        private static void CheckPetShrink(Int32 petID)
        {

            double petHeight = MQ.Query<double>("${Me.Pet.Height}");

            if(petHeight>1 && petHeight!=0)
            {
                foreach (var spellName in _petShrinkSpells)
                {
                    Spell s;
                    if (!Spell._loadedSpellsByName.TryGetValue(spellName, out s))
                    {
                        s = new Spell(spellName);
                    }
                    if (s.SpellID > 0 && s.CastType!= CastType.None)
                    {
                        Casting.Cast(petID, s);
                        return;
                    }
                }
            }
         
        }
        private static List<string> _petShrinkSpells = new List<string>() { "Gemstone of Dark Flame","Symbol of Ancient Summoning", "Tiny Companion" };

    }
}
