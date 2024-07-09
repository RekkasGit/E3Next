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

        private static DateTime _lastBow = DateTime.Now;

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
                if (aggroPct > 95 && pctHps<98)
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
					bool allowControl = Assist.AllowControl;
                    Assist.AssistOff();
                    Int32 counter = 0;
                    while (MQ.Query<Int32>("${Target.PctAggro}") >= 75 && counter<50)
                    {
                        MQ.Delay(100);
                        counter++;
                    }
					Assist.AllowControl = allowControl;
                    Assist.AssistOn(assistid, Zoning.CurrentZone.Id);
                    
                }

            }
            
        }

        [SubSystemInit] 
        public static void Init()
        {
            var patterns = new List<string> { @"hit (.+) for (.+)", @"You try to hit (.+), but miss" };
            EventProcessor.RegisterEvent("youhit", patterns, x =>
            {
                if (Assist.WhiteListedRangers.Any(ranger => ranger.Equals(E3.CurrentName, StringComparison.OrdinalIgnoreCase)) && E3.CharacterSettings.Ranger_EnabledBullshittery2) {
                    _lastBow = DateTime.Now;
                    //MQ.Write("\aoBowing done - switching to melee");
                    E3.CharacterSettings.Assist_Type = "Melee";
                }
            });
        }

        [ClassInvoke(Class.Ranger)]
        public static void SwapAssistType()
        {
            if (Assist.WhiteListedRangers.Any(ranger => ranger.Equals(E3.CurrentName, StringComparison.OrdinalIgnoreCase)) && E3.CharacterSettings.Ranger_EnabledBullshittery2) {
                var autofire = MQ.Query<bool>("${Me.AutoFire}");
                var autoAttack = MQ.Query<bool>("${Me.Combat}");
                if (!autofire && !autoAttack) return;
                //if (!e3util.ShouldCheck(ref _nextSwapCheck, _nextSwapRefreshTimeInterval)) return;
                var timeSinceLastBow = (DateTime.Now - _lastBow).TotalMilliseconds;
                if (timeSinceLastBow > E3.CharacterSettings.Ranger_SecondDelayOne && !autofire)
                {
                    //MQ.Write($"\agTime since last bow: {timeSinceLastBow} - switching to autofire");
                    E3.CharacterSettings.Assist_Type = "Autofire";
                }
            }
        }


    }
}
