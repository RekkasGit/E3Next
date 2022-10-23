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
    public static class Ranger
    {
        public static Logging _log = E3._log;
        private static IMQ MQ = E3.MQ;
        private static ISpawns _spawns = E3._spawns;

        private static Int64 _nextAggroCheck = 0;
        private static Int64 _nextAggroRefreshTimeInterval = 1000;

        [AdvSettingInvoke]
        public static void Check_RangerAggro()
        {
            if (!e3util.ShouldCheck(ref _nextAggroCheck, _nextAggroRefreshTimeInterval)) return;

            if (Assist._isAssisting)
            {

                //lets check our aggro.
                Int32 aggroPct = MQ.Query<Int32>("${Target.PctAggro}");

                if (aggroPct > 95)
                {
                    E3._bots.Broadcast($"\ag<check_RangerAggro> \awI have stolen aggro again ({aggroPct}%), canceling assist and casting jolt. Recall assist to reengage");
                    MQ.Cmd("/beep");
                    Assist.AssistOff();
                }

            }
            else
            {
                bool isAggressive = MQ.Query<bool>("${Target.Aggressive}");
                if(isAggressive)
                {

                    Spell s;
                    if(Spell._loadedSpellsByName.TryGetValue("Jolt",out s))
                    {
                        s = new Spell("Jolt");
                    }
                    if (Casting.CheckReady(s) && Casting.CheckMana(s))
                    {
                        Casting.Cast(0, s);
                       
                    }
                }

            }



        }


    }
}
