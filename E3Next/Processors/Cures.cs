using E3Core.Data;
using E3Core.Settings;
using E3Core.Utility;
using MonoCore;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;

namespace E3Core.Processors
{
    /// <summary>
    /// Processor for automated curing of debuffs.
    /// </summary>
    public static class Cures
    {
        public static Logging Log = E3.Log;
        private static IMQ MQ = E3.MQ;
        private static ISpawns _spawns = E3.Spawns;
        private static Spell _radiantCure;
        private static bool _shouldCastCure = true;
        private static Int64 _nextRCureCheck = 0;
        private static Int64 _nexRCureCheckInterval = 500;

        /// <summary>
        /// Initializes this instance.
        /// </summary>
        [SubSystemInit]
        public static void Init()
        {
            _radiantCure = new Spell("Radiant Cure");
            RegisterEvents();
        }

        private static void RegisterEvents()
        {
            EventProcessor.RegisterCommand("/CastingRadiantCure", (x) =>
            {
                if (x.args.Count > 0)
                {
                    Boolean.TryParse(x.args[0], out _shouldCastCure);
                }
            });
        }
      
        /// <summary>
        /// Checks if any cures are needed. There are two types of cures: counter cures and buff cures
        /// </summary>
        [AdvSettingInvoke]
        public static void Check_Cures()
        {

            if (!e3util.ShouldCheck(ref _nextRCureCheck, _nexRCureCheckInterval)) return;
            if (!E3.ActionTaken) CheckRadiant();
            if (!E3.ActionTaken) CheckNormalCures();
            if (!E3.ActionTaken) CheckCounterCures();
            if (!E3.ActionTaken) CheckNormalCureAll();

        }
        private static void CheckNormalCureAll()
        {
            foreach (var spell in E3.CharacterSettings.CureAll)
            {

                foreach (var id in Basics.GroupMembers)
                {
                    Spawn s;
                    if (_spawns.TryByID(id, out s))
                    {
                        if (E3.Bots.BuffList(s.CleanName).Contains(spell.CheckForID))
                        {
                            if (Casting.CheckReady(spell) && Casting.CheckMana(spell))
                            {
                                Casting.Cast(s.ID, spell);
                                return;
                            }
                        }
                    }
                }

            }
        }
        private static void CheckRadiant()
        {
            //raidient cure cast
            if (MQ.Query<bool>("${Me.AltAbilityReady[Radiant Cure]}"))
            {
                //spell here is the spell debuff we are looking for
                foreach (var spell in E3.CharacterSettings.RadiantCure)
                {
                    Int32 numberSick = 0;

                    foreach (var id in Basics.GroupMembers)
                    {
                        Spawn s;
                        if (_spawns.TryByID(id, out s))
                        {
                            if (E3.Bots.BuffList(s.CleanName).Contains(spell.SpellID))
                            {
                                if (s.Distance < _radiantCure.MyRange)
                                {
                                    numberSick++;

                                }
                            }
                        }
                    }
                    if (numberSick >= spell.MinSick)
                    {
                        CastRadiantCure();
                        return;
                    }
                }
            }
            //end R-CURE
        }
        private static void CheckCounterCures()
        {

            if (CheckCounterCure(E3.CharacterSettings.CurseCounterCure, E3.CharacterSettings.CurseCounterIgnore, E3.Bots.BaseCursedCounters)) return;
            if (CheckCounterCure(E3.CharacterSettings.PoisonCounterCure, E3.CharacterSettings.PoisonCounterIgnore, E3.Bots.BasePoisonedCounters)) return;
            if (CheckCounterCure(E3.CharacterSettings.DiseaseCounterCure, E3.CharacterSettings.DiseaseCounterIgnore, E3.Bots.BaseDiseasedCounters)) return;

        }
        private static bool CheckCounterCure(List<Spell> curesSpells, List<Spell> ignoreSpells, Func<string, int> counterFunc)
        {
            foreach (var spell in curesSpells)
            {

                //check each member of the group for counters
                foreach (var target in E3.Bots.BotsConnected())
                {
                    Spawn s;
                    if (_spawns.TryByName(target, out s))
                    {
                        Int32 counters = counterFunc(target);
                        if (counters > 0 && s.Distance < spell.MyRange)
                        {
                            //check and make sure they don't have one of the 'ignored debuffs'
                            List<Int32> badbuffs = E3.Bots.BuffList(s.CleanName);

                            bool foundBadBuff = false;
                            foreach (var bb in ignoreSpells)
                            {
                                if (badbuffs.Contains(bb.SpellID))
                                {
                                    foundBadBuff = true;
                                    break;
                                }
                            }
                            if (foundBadBuff) continue;
                            if (Casting.InRange(s.ID,spell) && Casting.CheckReady(spell) && Casting.CheckMana(spell))
                            {
                                Casting.Cast(s.ID, spell);
                                return true;
                            }
                        }
                    }
                }

            }
            return false;

        }
        private static void CheckNormalCures()
        {
            foreach (var spell in E3.CharacterSettings.Cures)
            {
                Spawn s;
                if (_spawns.TryByName(spell.CastTarget, out s))
                {
                    if (s.Distance < spell.MyRange && E3.Bots.BuffList(s.CleanName).Contains(spell.CheckForID))
                    {
                        if (Casting.InRange(s.ID, spell) && Casting.CheckReady(spell) && Casting.CheckMana(spell))
                        {
                            Casting.Cast(s.ID, spell);
                            return;
                        }
                    }
                }


            }
        }
        private static void CastRadiantCure()
        {

            //check the event queue
            EventProcessor.ProcessEventsInQueues("/CastingRadiantCure");
            if (_shouldCastCure)
            {
                E3.Bots.BroadcastCommandToGroup("/CastingRadiantCure FALSE");
                //did we find enough sick people? if so, cast cure.
                Casting.Cast(0, _radiantCure);
                E3.Bots.BroadcastCommandToGroup("/CastingRadiantCure TRUE");
            }
        }
    }
}
