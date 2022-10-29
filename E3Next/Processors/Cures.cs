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
    public static class Cures
    {

        public static Logging _log = E3._log;
        private static IMQ MQ = E3.MQ;
        private static ISpawns _spawns = E3._spawns;
        private static Data.Spell _RaidentCure;
        private static bool _shouldCastCure = true;
        private static Int64 _nextRCureCheck = 0;
        private static Int64 _nexRCureCheckInterval = 500;

        [SubSystemInit]
        public static void Init()
        {
            _RaidentCure = new Data.Spell("Radiant Cure");
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
        //There are two types of cures
        //counter cures
        //buff cures.
        [AdvSettingInvoke]
        public static void Check_Cures()
        {

            if (!e3util.ShouldCheck(ref _nextRCureCheck, _nexRCureCheckInterval)) return;
            if (!E3._actionTaken) CheckRaident();
            if (!E3._actionTaken) CheckNormalCures();
            if (!E3._actionTaken) CheckCounterCures();
            if (!E3._actionTaken) CheckNormalCureAll();

        }
        private static void CheckNormalCureAll()
        {
            foreach (var spell in E3._characterSettings.CureAll)
            {

                foreach (var id in Basics._groupMembers)
                {
                    Spawn s;
                    if (_spawns.TryByID(id, out s))
                    {
                        if (E3._bots.BuffList(s.CleanName).Contains(spell.CheckForID))
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
        private static void CheckRaident()
        {
            //raidient cure cast
            if (MQ.Query<bool>("${Me.AltAbilityReady[Radiant Cure]}"))
            {
                //spell here is the spell debuff we are looking for
                foreach (var spell in E3._characterSettings.RadiantCure)
                {
                    Int32 numberSick = 0;

                    foreach (var id in Basics._groupMembers)
                    {
                        Spawn s;
                        if (_spawns.TryByID(id, out s))
                        {
                            if (E3._bots.BuffList(s.CleanName).Contains(spell.SpellID))
                            {
                                if (s.Distance < _RaidentCure.MyRange)
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

            if (CheckCounterCure(E3._characterSettings.CurseCounterCure, E3._characterSettings.CurseCounterIgnore)) return;
            if (CheckCounterCure(E3._characterSettings.PosionCounterCure, E3._characterSettings.PosionCounterIgnore)) return;
            if (CheckCounterCure(E3._characterSettings.DiseaseCounterCure, E3._characterSettings.DiseaseCounterIgnore)) return;

        }
        private static bool CheckCounterCure(List<Data.Spell> curesSpells, List<Data.Spell> ignoreSpells)
        {
            foreach (var spell in curesSpells)
            {

                //check each member of the group for counters
                List<string> targets = E3._bots.BotsConnected();
                foreach (var target in targets)
                {
                    Spawn s;
                    if (_spawns.TryByName(target, out s))
                    {
                        Int32 counters = E3._bots.CursedCounters(target);
                        if (counters > 0 && s.Distance < spell.MyRange)
                        {
                            //check and make sure they don't have one of the 'ignored debuffs'
                            List<Int32> badbuffs = E3._bots.BuffList(s.CleanName);

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
            foreach (var spell in E3._characterSettings.Cures)
            {
                Spawn s;
                if (_spawns.TryByName(spell.CastTarget, out s))
                {
                    if (s.Distance < spell.MyRange && E3._bots.BuffList(s.CleanName).Contains(spell.CheckForID))
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
                E3._bots.BroadcastCommandToGroup("/CastingRadiantCure FALSE");
                //did we find enough sick people? if so, cast cure.
                Casting.Cast(0, _RaidentCure);
                E3._bots.BroadcastCommandToGroup("/CastingRadiantCure TRUE");
            }
        }
    }
}
