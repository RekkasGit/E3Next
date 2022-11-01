using E3Core.Processors;
using E3Core.Settings;
using System;
using E3Core.Classes;
using E3Core.Data;
using E3Core.Utility;
using MonoCore;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;


namespace E3Core.Classes
{
    /// <summary>
    /// Properties and methods specific to the ranger class
    /// </summary>
    public static class Ranger
    {
        private static Logging _log = E3.Log;
        private static IMQ MQ = E3.Mq;
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

            if (Assist._isAssisting)
            {

                //lets check our aggro.
                Int32 aggroPct = MQ.Query<Int32>("${Target.PctAggro}");
                Int32 pctHps = MQ.Query<Int32>("${Target.PctHPs}");
                if (aggroPct > 95 && pctHps<98)
                {
                    Spell s;
                    if (!Spell._loadedSpellsByName.TryGetValue("Cover Tracks", out s))
                    {
                        s = new Spell("Cover Tracks");
                    }
                    if (Casting.CheckReady(s) && Casting.CheckMana(s))
                    {
                        Casting.Cast(0, s);
                        return;
                    }
                    E3.Bots.Broadcast($"\ag<check_RangerAggro> \awI have stolen aggro again ({aggroPct}%), Pausing for 5 seconds then going to reengage");
                    Int32 assistid = Assist._assistTargetID;
                    Assist.AssistOff();
                    MQ.Delay(5000);
                    Assist.AssistOn(assistid);
                    
                }

            }
            
        }


    }
}
