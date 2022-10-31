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
    /// Properties and methods specific to the druid class
    /// </summary>
    public static class Druid
    {
        private static Logging _log = E3._log;
        private static IMQ MQ = E3.MQ;
        private static ISpawns _spawns = E3._spawns;

        private static Int64 _nextAutoCheetaCheck;
        private static Data.Spell _cheetaSpell = new Spell("Communion of the Cheetah");

        /// <summary>
        /// Checks and re-applies Communion of the Cheetah if necessary.
        /// </summary>
        [ClassInvoke(Data.Class.Druid)]
        public static void AutoCheeta()
        {
            if (!e3util.ShouldCheck(ref _nextAutoCheetaCheck, 1000)) return;
            if (E3._characterSettings.Druid_AutoCheeta)
            {
               if(Casting.CheckReady(_cheetaSpell))
               {
                    bool haveBardSong = MQ.Query<bool>("${Me.Buff[Selo's Sonata].ID}") || MQ.Query<bool>("${Me.Buff[Selo's Accelerating Chorus].ID}");
                    if (!haveBardSong)
                    {
                        Int32 totalSecondsLeft = MQ.Query<Int32>("${Me.Buff[Spirit of Cheetah].Duration.TotalSeconds}");
                        if (totalSecondsLeft < 10)
                        {
                            Casting.Cast(0, _cheetaSpell);
                        }
                    }
               }
            }
        }
    }
}
