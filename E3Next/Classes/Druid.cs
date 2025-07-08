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
        private const int CheetahBuffID = 23581;
        private static Logging _log = E3.Log;
        private static IMQ MQ = E3.MQ;
        private static ISpawns _spawns = E3.Spawns;

        private static Int64 _nextAutoCheetaCheck;
        private static Data.Spell _cheetaSpell = new Spell("Communion of the Cheetah");

        /// <summary>
        /// Checks and re-applies Communion of the Cheetah if necessary.
        /// </summary>
        [ClassInvoke(Data.Class.Druid)]
        public static void AutoCheeta()
        {
            if (E3.IsInvis) return;
			if (Heals.IgnoreHealTargets.Count > 0) return;
			if (!e3util.ShouldCheck(ref _nextAutoCheetaCheck, 1000)) return;
            if (E3.CharacterSettings.Druid_AutoCheetah)
            {

                bool needToCast = false;
                //lets get group members
                List<string> memberNames = E3.Bots.BotsConnected();
               foreach (int memberid in Basics.GroupMembers)
                {
                    Spawn s;
                    if (_spawns.TryByID(memberid,out s))
                    {
                        if(memberNames.Contains(s.CleanName))
                        {
                            List<Int32> buffList = E3.Bots.BuffList(s.CleanName);
                            //_log.Write($"Bufflist for {s.CleanName}:" + String.Join(",", buffList));
                            if (!buffList.Contains(CheetahBuffID))
                            {
                                needToCast = true;
                                break;
                            }
                        }
                    }
                }
                Int32 totalSecondsLeft = MQ.Query<Int32>("${Me.Buff[Spirit of Cheetah].Duration.TotalSeconds}");
                if (totalSecondsLeft < 10)
                {
                    needToCast = true;
                }
                
                if (needToCast)
                {
                    if (Casting.CheckReady(_cheetaSpell))
                    {
                        bool haveBardSong = MQ.Query<bool>("${Me.Buff[Selo's Sonata].ID}") || MQ.Query<bool>("${Me.Buff[Selo's Accelerating Chorus].ID}");
                        if (!haveBardSong)
                        {
                            if (MQ.Query<int>("${Target.ID}") == MQ.Query<int>("${Me.Pet.ID}"))
                            {
                                MQ.Cmd("/target clear");
                            }

                            Casting.Cast(E3.CurrentId, _cheetaSpell);
                        }
                    }
                }
            }
        }
    }
}
