﻿using E3Core.Data;
using E3Core.Processors;
using E3Core.Settings;
using E3Core.Utility;

using MonoCore;

using System;


namespace E3Core.Classes
{
    /// <summary>
    /// Properties and methods specific to the ranger class
    /// </summary>
    public static class Ranger
    {
        private static Logging _log = E3.Log;
        private static IMQ MQ = E3.MQ;
        private static ISpawns _spawns = E3.Spawns;

        private static Int64 _nextAggroCheck = 0;
        private static Int64 _nextAggroRefreshTimeInterval = 1000;

        /// <summary>
        /// Checks aggro level and drops it if necessary.
        /// </summary>
        [AdvSettingInvoke]
        public static void Check_RangerAggro()
        {
            if (!e3util.ShouldCheck(ref _nextAggroCheck, _nextAggroRefreshTimeInterval)) return;

            if (Assist.IsAssisting)
            {

                //lets check our aggro.
                Int32 aggroPct = MQ.Query<Int32>("${Target.PctAggro}");
                Int32 pctHps = MQ.Query<Int32>("${Target.PctHPs}");
                if (aggroPct > 95 && pctHps < 98)
                {
                    Spell s;
                    if (!Spell.LoadedSpellsByName.TryGetValue("Cover Tracks", out s))
                    {
                        s = new Spell("Cover Tracks");
                    }
                    if (MQ.Query<bool>("${Target.Named}") && Casting.CheckReady(s) && Casting.CheckMana(s))
                    {
                        Casting.Cast(0, s);
                        return;
                    }
                    E3.Bots.Broadcast($"\ag<check_RangerAggro> \awI have stolen aggro again ({aggroPct}%), Delaying for a bit till agro is below 75% or 5 seconds");
                    Int32 assistid = Assist.AssistTargetID;
                    Assist.AssistOff();
                    Int32 counter = 0;
                    while (MQ.Query<Int32>("${Target.PctAggro}") >= 75 && counter < 50)
                    {
                        MQ.Delay(100);
                        counter++;
                    }
                    Assist.AssistOn(assistid, Zoning.CurrentZone.Id);

                }

            }

        }


    }
}
