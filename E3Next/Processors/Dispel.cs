using E3Core.Settings;
using E3Core.Utility;
using MonoCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace E3Core.Processors
{
    public static class Dispel
    {
        public static Logging _log = E3.Log;
        private static IMQ MQ = E3.Mq;
        private static ISpawns _spawns = E3.Spawns;
        private static Int64 _nextDispelCheck = 0;
        private static Int64 _nextDispelCheckInterval = 500;

        [ClassInvoke(Data.Class.All)]
        public static void CheckDispel()
        {
            if (!Assist._isAssisting) return;
            if (E3.CharacterSettings.Dispels.Count == 0) return;

            if (!e3util.ShouldCheck(ref _nextDispelCheck, _nextDispelCheckInterval)) return;

            if (Casting.TrueTarget(Assist._assistTargetID))
            {
                if (MQ.Query<bool>("${Target.Beneficial.ID}"))
                {
                    Int32 buffCount=55;
                  
                    for (Int32 i = 1; i <= buffCount; i++)
                    {
                        bool beneficial = MQ.Query<bool>($"${{Target.Buff[{i}].Beneficial}}");
                        if (beneficial)
                        {
                            string buffName = MQ.Query<string>($"${{Target.Buff[{i}]}}");
                            Int32 buffID = MQ.Query<Int32>($"${{Target.Buff[{i}].ID}}");
                            //now to check if its beneifical for real
                            foreach (var ignore in E3.CharacterSettings.DispelIgnore)
                            {
                                if (ignore.SpellID == buffID)
                                {
                                    beneficial = false;
                                    break;
                                }
                            }
                            if (!beneficial) continue;

                            string buffCategory = MQ.Query<string>($"${{Target.Buff[{i}].Category}}");
                            
                            if (buffCategory == "Disciplines") continue;

                            if (beneficial)
                            {
                                foreach (var spell in E3.CharacterSettings.Dispels)
                                {
                                    //now have it as a target, need to check its beneficial buffs
                                    if (Casting.CheckReady(spell) && Casting.CheckMana(spell))
                                    {
                                        Casting.Cast(0, spell);
                                        return;
                                    }
                                }

                            }
                        }
                    }

                }

            }

        }
    }
}
