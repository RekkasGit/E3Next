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
        private static Int64 _nexRCureCheckInterval = 250;


        public static void Init()
        {
            _RaidentCure = new Data.Spell("Radiant Cure");
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

            //raidient cure cast
            if (MQ.Query<bool>("${Me.AltAbilityReady[Radiant Cure]}"))
            {
                //find out how many are sick in our group
                Int32 groupCount = MQ.Query<Int32>("${Group}");
                groupCount++;//count ourselves in the group
                //spell here is the spell debuff we are looking for
                foreach (var spell in E3._characterSettings.RadiantCure)
                {
                    Int32 numberSick = 0;

                    //see if any  of our members have it.
                    for (Int32 i = 0; i < groupCount; i++)
                    {
                        Int32 id = MQ.Query<Int32>($"${{Group.Member[{i}].ID}}");
                        Spawn s;
                        if (_spawns.TryByID(id, out s))
                        {
                            if(E3._bots.BuffList(s.ClassName).Contains(spell.SpellID))
                            {
                                if (s.Distance < _RaidentCure.MyRange)
                                {
                                    numberSick++;

                                }
                            }
                        }
                    }
                    if(numberSick>=spell.MinSick)
                    {
                        CastRadiantCure();
                        return;
                    }
                }
            }
            //end R-CURE
        }

        private static void CastRadiantCure()
        {
            //check the event queue
            EventProcessor.ProcessEventsInQueues("/CastingRadiantCure");
            if(_shouldCastCure)
            {
                E3._bots.BroadcastCommandToOthers("/CastingRadiantCure FALSE");
                //did we find enough sick people? if so, cast cure.
                Casting.Cast(0, _RaidentCure);
                E3._bots.BroadcastCommandToOthers("/CastingRadiantCure TRUE");
            }
        }
    }
}
