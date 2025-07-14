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
            if (Basics.GroupMembers.Count <5) return;
            if (!Assist.IsAssisting) return;
            //lets check our aggro.
            Int32 aggroPct = MQ.Query<Int32>("${Target.PctAggro}");
            Int32 pctHps = MQ.Query<Int32>("${Target.PctHPs}");
            if (aggroPct > 95 && pctHps<98)
            {
                Spell s;
                if (!Spell.LoadedSpellsByName.TryGetValue("Cover Tracks", out s))
                {
                    s = new Spell("Cover Tracks");
                }
                if (MQ.Query<bool>("${Target.Named}") && Casting.CheckMana(s) && Casting.CheckReady(s))
                {
                    Casting.Cast(0, s);
                    return;
                }
                E3.Bots.Broadcast($"\ag<check_RangerAggro> \awI have stolen aggro again ({aggroPct}%), Delaying for a bit till agro is below 85% or 5 seconds");
                Int32 assistid = Assist.AssistTargetID;
				bool allowControl = Assist.AllowControl;
                Assist.AssistOff();
                Int32 counter = 0;
                while (MQ.Query<Int32>("${Target.PctAggro}") >= 85 && counter<50)
                {
                    MQ.Delay(100);
                    counter++;
                }
				Assist.AllowControl = allowControl;
                Assist.AssistOn(assistid, Zoning.CurrentZone.Id);
                    
            }
        }
    }
}
