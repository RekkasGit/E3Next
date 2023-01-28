using E3Core.Classes;
using E3Core.Data;
using E3Core.Settings;
using E3Core.Utility;
using MonoCore;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;

namespace E3Core.Processors
{
    public static class ClearXTargets
    {
        public static Logging _log = E3.Log;
        private static IMQ MQ = E3.MQ;
        private static ISpawns _spawns = E3.Spawns;

        public static bool Enabled = false;
        public static Int32 MobToAttack = 0;
        public static bool FaceTarget = false;
        public static List<string> Filters = new List<string>();
        public static bool StickTarget = false;

        [ClassInvoke(Data.Class.All)]
        public static void Check_Xtargets()
        {
            if (Enabled)
            {
                e3util.YieldToEQ();
                _spawns.RefreshList();
                if (MobToAttack > 0)
                {
                    if (_spawns.TryByID(MobToAttack, out var ts))
                    {
                        //is it still alive?
                        if (ts.TypeDesc == "Corpse") MobToAttack = 0;//its dead jim
                    }
                    else
                    {
                        MobToAttack = 0;
                    }
                }
                //lets see if we have anything on xtarget that is valid
                if (MobToAttack == 0)
                {
                    foreach (var s in _spawns.Get().OrderBy(x => x.Distance))
                    {
                        //find all mobs that are close
                        if (s.TypeDesc != "NPC") continue;
                        if (!s.Targetable) continue;
                        if (!s.Aggressive) continue;
                        if (!MQ.Query<bool>($"${{Spawn[npc id {s.ID}].LineOfSight}}")) continue;
                        if (s.Distance > 60) break;//mob is too far away, and since it is ordered, kick out.
                        //its valid to attack!
                        MobToAttack = s.ID;
                        break;
                    }
                    if (MobToAttack == 0)
                    {
                        //we are done, stop killing
                        Enabled = false;
                        MQ.Write("\agClear Targets complete.");
                        return;
                    }

                    //mobs to attack will be sorted by distance.
                    if (MobToAttack > 0)
                    {
                        //pop it off and start assisting.
                        Int32 mobId = MobToAttack;
                        Spawn s;
                        if (_spawns.TryByID(mobId, out s))
                        {
                            MQ.Write("\agClear Targets: \aoIssuing Assist.");
                            Assist.AllowControl = true;
                            Assist.AssistOn(mobId, Zoning.CurrentZone.Id);
                            if (FaceTarget)
                            {
                                MQ.Cmd("/face fast");
                            }
                            if (StickTarget)
                            {
                                
                                MQ.Cmd($"/squelch /stick {E3.CharacterSettings.Assist_MeleeStickPoint} {E3.CharacterSettings.Assist_MeleeDistance}");
                            }
                            MQ.Delay(500);
                            if (Filters.Count > 0)
                            {
                                E3.Bots.BroadcastCommandToGroup($"/assistme {mobId} {Zoning.CurrentZone.Id} {string.Join(" ", Filters)}");
                            }
                            else
                            {
                                E3.Bots.BroadcastCommandToGroup($"/assistme {mobId} {Zoning.CurrentZone.Id}");
                            }
                        }
                        else
                        {
                            MobToAttack = 0;
                        }
                    }
                }


            }
        }
    }
}
