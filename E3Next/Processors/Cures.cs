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
        private static Int64 _nexRCureCheckInterval = 1000;

        public static void Init()
        {
            _RaidentCure = new Data.Spell("Radiant Cure");
            RegisterEvents();
        }

        private static void RegisterEvents()
        {
            EventProcessor.RegisterCommand("/CastingRadiantCure", (x) =>
            {
                if (x.args.Count >0)
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
            if (!E3._actionTaken) CheckNormalCureAll();

        }
        private static void CheckNormalCureAll()
        {
            foreach (var spell in E3._characterSettings.CureAll)
            {
                if (Casting.CheckReady(spell) && Casting.CheckMana(spell))
                {
                    foreach (var id in Basics._groupMembers)
                    {
                         Spawn s;
                        if (_spawns.TryByID(id, out s))
                        {
                            if (E3._bots.BuffList(s.CleanName).Contains(spell.SpellID))
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

                    foreach(var id in Basics._groupMembers)
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

        private static void CheckNormalCures()
        {
            foreach(var spell in E3._characterSettings.Cures)
            {
                if(Casting.CheckReady(spell) && Casting.CheckMana(spell))
                {
                    Spawn s;
                    if (_spawns.TryByName(spell.CastTarget, out s))
                    {
                        if (s.Distance<spell.MyRange && E3._bots.BuffList(s.CleanName).Contains(spell.SpellID))
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
            if(_shouldCastCure)
            {
                E3._bots.BroadcastCommandToGroup("/CastingRadiantCure FALSE");
                //did we find enough sick people? if so, cast cure.
                Casting.Cast(0, _RaidentCure);
                E3._bots.BroadcastCommandToGroup("/CastingRadiantCure TRUE");
            }
        }
    }
}
