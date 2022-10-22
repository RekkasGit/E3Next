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
        public static Logging _log = E3._log;
        private static IMQ MQ = E3.MQ;
        private static ISpawns _spawns = E3._spawns;

        public static bool _enabled = false;
        public static Queue<Int32> _mobsToAttack = new Queue<int>();

        [ClassInvoke(Data.Class.All)]
        public static void Check_Xtargets()
        {
            if (_enabled && !Assist._isAssisting)
            {
                //lets see if we have anything on xtarget that is valid
               
                _spawns.RefreshList();
                foreach (var s in _spawns.Get().OrderBy(x=>x.Distance))
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
                    _mobsToAttack.Enqueue(s.ID);
                    break;
                }
                if(_mobsToAttack.Count==0)
                {
                    //we are done, stop killing
                    _enabled = false;
                    MQ.Write("\agClear Targets complete.");
                    return;
                }
                
                //mobs to attack will be sorted by distance.
                if (_mobsToAttack.Count > 0)
                {
                    //pop it off and start assisting.
                    Int32 mobId = _mobsToAttack.Dequeue();
                    Spawn s;
                    if(_spawns.TryByID(mobId, out s))
                    {
                        Assist._allowControl = true;
                        Assist.AssistOn(mobId);
                        E3._bots.BroadcastCommandToGroup($"/assistme {mobId}");
                    }
                }
            }
        }
    }
}
