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

        [ClassInvoke(Data.Class.All)]
        public static void Check_Xtargets()
        {

            if (Enabled)
            {
                _spawns.RefreshList();
                if (MobToAttack > 0)
                {
                    Spawn ts;
                    if (_spawns.TryByID(MobToAttack, out ts))
                    {
                        //is it still alive?
                        if (ts.TypeDesc == "Corpse")
                        {
                            //its dead jim
                            MobToAttack = 0;

                        }
                    }

                }
                //lets see if we have anything on xtarget that is valid
                if (MobToAttack == 0)
                {
                    foreach (var s in _spawns.Get().OrderBy(x => x.Distance))
                    {
                        //find all mobs that are close
                        if (!s.Targetable) continue;
                        if (!s.Aggressive) continue;
                        if (!MQ.Query<bool>($"${{Spawn[npc id {s.ID}].LineOfSight}}")) continue;
                        if (s.Distance > 60) break;//mob is too far away, and since it is ordered, kick out.
                        if (s.TypeDesc == "Corpse") continue;
                        if (s.Name.Contains("'s pet'")) continue;
                        if (s.Name.IndexOf("Chest", StringComparison.OrdinalIgnoreCase) > -1) continue;
                        if (s.Name.IndexOf("a box", StringComparison.OrdinalIgnoreCase) > -1) continue;
                        if (s.Name.IndexOf("crate", StringComparison.OrdinalIgnoreCase) > -1) continue;
                        if (s.Name.IndexOf("hollow_tree", StringComparison.OrdinalIgnoreCase) > -1) continue;
                        if (s.Name.IndexOf("wooden box", StringComparison.OrdinalIgnoreCase) > -1) continue;
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
                            Assist.AssistOn(mobId);
                            MQ.Delay(500);
                            E3.Bots.BroadcastCommandToGroup($"/assistme {mobId}");
                        }
                    }
                }

               
            }
        }
    }
}
