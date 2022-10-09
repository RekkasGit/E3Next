using E3Core.Classes;
using E3Core.Data;
using E3Core.Settings;
using E3Core.Utility;
using MonoCore;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;

namespace E3Core.Processors
{
    public static class DebuffDot
    {
        public static Logging _log = E3._log;
        private static IMQ MQ = E3.MQ;
        private static ISpawns _spawns = E3._spawns;
        public static Dictionary<Int32, SpellTimer> _debuffdotTimers = new Dictionary<Int32, SpellTimer>();
        public static HashSet<Int32> _mobsToDot = new HashSet<int>();
        public static HashSet<Int32> _mobsToDebuff = new HashSet<int>();
        public static List<Int32> _deadMobs = new List<int>();
        public static Int64 _printoutTimer = 0;
        private static Int64 _nextDebuffCheck = 0;
        private static Int64 _nextDebuffCheckInterval = 1000;
        private static Int64 _nextDoTCheck = 0;
        private static Int64 _nextDoTCheckInterval = 1000;

        public static void Init()
        {
            RegisterEvents();
        }
        public static void Reset()
        {
            _mobsToDot.Clear();
            _mobsToDebuff.Clear();
            foreach (var kvp in _debuffdotTimers)
            {
                kvp.Value.Dispose();
            }
            _debuffdotTimers.Clear();
        }
        [AdvSettingInvoke]
        public static void Check_Debuffs()
        {
            if (!ShouldDebuffCheck()) return;
            if (Assist._assistTargetID > 0)
            {
                CastLongTermSpell(Assist._assistTargetID, E3._characterSettings.Debuffs_OnAssist);
                if (E3._actionTaken) return;
            }
            foreach (var mobid in _mobsToDebuff)
            {

                CastLongTermSpell(mobid, E3._characterSettings.Debuffs_Command);
                if (E3._actionTaken) return;
            }
            foreach (var mobid in _deadMobs)
            {
                _mobsToDot.Remove(mobid);
                _mobsToDebuff.Remove(mobid);
            }
            if (_deadMobs.Count > 0) _deadMobs.Clear();

            //put us back to our assist target
            Int32 targetId = MQ.Query<Int32>("${Target.ID}");
            if (targetId != Assist._assistTargetID)
            {
                Casting.TrueTarget(Assist._assistTargetID);

            }

        }
        [AdvSettingInvoke]
        public static void check_Dots()
        {
            if (!ShouldDoTCheck()) return;
            if (Assist._assistTargetID > 0)
            {
                CastLongTermSpell(Assist._assistTargetID, E3._characterSettings.Dots_Assist);
                if (E3._actionTaken) return;
            }

            foreach (var mobid in _mobsToDot)
            {
                CastLongTermSpell(mobid, E3._characterSettings.Dots_OnCommand);
                if (E3._actionTaken) return;
            }
            foreach (var mobid in _deadMobs)
            {
                _mobsToDot.Remove(mobid);
                _mobsToDebuff.Remove(mobid);
            }
            if (_deadMobs.Count > 0) _deadMobs.Clear();

            //put us back to our assist target
            Int32 targetId = MQ.Query<Int32>("${Target.ID}");
            if (targetId != Assist._assistTargetID)
            {
                Casting.TrueTarget(Assist._assistTargetID);

            }
        }
        public static void DotsOn(Int32 mobid)
        {
            if (!_mobsToDot.Contains(mobid))
            {
                _mobsToDot.Add(mobid);
            }
        }
        private static void RegisterEvents()
        {

            e3Utility.RegisterCommandWithTarget("/dotson", DotsOn);
            e3Utility.RegisterCommandWithTarget("/dot", DotsOn);
            EventProcessor.RegisterCommand("/debuffsoff", (x) =>
            {
                _mobsToDebuff.Clear();
                if (x.args.Count == 0)
                {
                    //we are telling people to back off
                    E3._bots.BroadcastCommandToOthers($"/debuffsoff all");
                }

            });
            EventProcessor.RegisterCommand("/dotsoff", (x) =>
            {
                _mobsToDot.Clear();
                if (x.args.Count == 0)
                {
                    //we are telling people to back off
                    E3._bots.BroadcastCommandToOthers($"/dotsoff all");
                }

            });
            e3Utility.RegisterCommandWithTarget("/debuffson", DebuffsOn);
            e3Utility.RegisterCommandWithTarget("/debuff", DebuffsOn);
        }
        public static void DebuffsOn(Int32 mobid)
        {
            if (!_mobsToDebuff.Contains(mobid))
            {
                _mobsToDebuff.Add(mobid);
            }
        }
        private static void CastLongTermSpell(Int32 mobid, List<Data.Spell> spells)
        {

            foreach (var spell in spells)
            {
                //do we already have a timer on this spell?
                SpellTimer s;
                if (_debuffdotTimers.TryGetValue(mobid, out s))
                {
                    Int64 timestamp;
                    if (s._timestamps.TryGetValue(spell.SpellID, out timestamp))
                    {
                        if (Core._stopWatch.ElapsedMilliseconds < timestamp)
                        {
                            //debuff/dot is still on the mob, kick off
                            continue;
                        }
                    }
                }
                ResistCounter r;
                if (Casting._resistCounters.TryGetValue(mobid, out r))
                {
                    //have resist counters on this mob, lets check if this spell is on the list
                    Int32 counters;
                    if (r._spellCounters.TryGetValue(spell.SpellID, out counters))
                    {
                        if (counters > spell.MaxTries)
                        {   //mob is resistant to this spell, kick out. 
                            continue;
                        }
                    }
                }
                if (Casting.CheckReady(spell) && Casting.checkMana(spell))
                {

                    //lets make sure the buffs/debuffs are there
                    if (!Casting.TrueTarget(mobid))
                    {
                        //can't target it, so kick out for this mob
                        _deadMobs.Add(mobid);
                        return;
                    }
                    if (MQ.Query<bool>($"${{Bool[${{Spawn[id {mobid}].Type.Equal[Corpse]}}]}}"))
                    {
                        _deadMobs.Add(mobid);
                        return;
                        //its dead jim, leave it be

                    }
                    MQ.Delay(2000, "${Target.BuffsPopulated}");
                    //check if the if condition works
                    if (!String.IsNullOrWhiteSpace(spell.Ifs))
                    {
                        if (!MQ.Query<bool>($"${{If[{spell.Ifs},TRUE,FALSE]}}"))
                        {
                            continue;
                        }
                    }
                    if (!String.IsNullOrWhiteSpace(spell.CastIF))
                    {
                        if (!MQ.Query<bool>($"${{Bool[${{Target.Buff[{spell.CastIF}]}}]}}"))
                        {
                            //doesn't have the buff we want
                            continue;
                        }
                    }
                    if (!String.IsNullOrWhiteSpace(spell.CheckFor))
                    {
                        if (MQ.Query<bool>($"${{Bool[${{Target.Buff[{spell.CheckFor}]}}]}}"))
                        {
                            //has the buff already
                            //lets set the timer for it so we dont' have to keep targeting it.
                            Int64 buffDuration = MQ.Query<Int64>($"${{Target.BuffDuration[{spell.CheckFor}]}}");
                            if (buffDuration < 1000)
                            {
                                buffDuration = 1000;
                            }
                            UpdateDotDebuffTimers(mobid, spell, buffDuration);
                            continue;
                        }
                    }
                    var result = Casting.Cast(mobid, spell, Heals.SomeoneNeedsHealing);
                    if (result == CastReturn.CAST_INTERRUPTFORHEAL)
                    {
                        return;
                    }
                    ////Okay lesson about EQ resist messages and timers for debuffs/buffs
                    //// you don't know if a spell resits unless the server tells you.. so this result above? somewhat unreliable.
                    //// The reason for this is, it takes X amount of time to come back from the server , and that X is unreliable as heck. 
                    //// So... for debuffs we are going to do this. If the target you have has the buff, grab its timer from the buffs object
                    //// for total time as as the Duration TLO can be unreliable depending on dot focus duration.  as in it says 72  sec when its 92 sec.
                    Casting.TrueTarget(mobid);

                    MQ.Delay(2000, "${Target.BuffsPopulated}");
                    //// we also have the situation where over 55> buffs on the ROF2 client cannot be viewed, but up to 85 or so work. 
                    //// we are going to have to loop through the buffs and set dot timers
                    //// if under 55< we will evict off the timer that we think we should have if we do
                    //// if over 55> we will update but not evict... best we can do. so if a dot goes over the 55 buff cap
                    //// but we get an invalid resist message... well... the client is going to assume it landed and set a timer for it. 
                    //// Most of the time this won't happen, but sometimes.. well.. ya. not much I can do.

                    //delay to release back to MQ to get a proper buffcount
                    MQ.Delay(100);
                    Int32 buffCount = MQ.Query<Int32>("${Target.BuffCount}");
                    //lets just update our cache with what is on the mob.
                    Int64 timeLeftInMS = Casting.TimeLeftOnMySpell(spell);
                    if (buffCount < 55)
                    {
                        UpdateDotDebuffTimers(mobid, spell, timeLeftInMS);
                    }
                    else
                    {
                        Int64 totalTimeToWait;
                        if (timeLeftInMS > 0)
                        {
                            totalTimeToWait = timeLeftInMS;
                        }
                        else
                        {
                            if (result != CastReturn.CAST_SUCCESS)
                            {
                                //zero it out
                                totalTimeToWait = 0;
                            }
                            else
                            {
                                totalTimeToWait = (spell.DurationTotalSeconds * 1000);
                            }
                        }
                        UpdateDotDebuffTimers(mobid, spell, totalTimeToWait);
                    }
                    //onto the next debuff/dot!
                }
            }
        }
        private static void UpdateDotDebuffTimers(Int32 mobid, Data.Spell spell, Int64 timeLeftInMS)
        {
            SpellTimer s;
            //if we have no time left, as it was not found, just set it to 0 in ours
            if (_debuffdotTimers.TryGetValue(mobid, out s))
            {
                if (!s._timestamps.ContainsKey(spell.SpellID))
                {
                    s._timestamps.Add(spell.SpellID, 0);
                }

                s._timestamps[spell.SpellID] = Core._stopWatch.ElapsedMilliseconds + timeLeftInMS;

            }
            else
            {
                SpellTimer ts = SpellTimer.Aquire();
                ts._mobID = mobid;

                ts._timestamps.Add(spell.SpellID, Core._stopWatch.ElapsedMilliseconds + timeLeftInMS);
                _debuffdotTimers.Add(mobid, ts);
            }
        }
        public static void PrintDotDebuffStatus()
        {
            //Printing out debuff timers
            if (_printoutTimer < Core._stopWatch.ElapsedMilliseconds)
            {
                if (_debuffdotTimers.Count > 0)
                {
                    MQ.Write("\atCurrent Debuff/Dots");
                    MQ.Write("\aw===================");


                }

                foreach (var kvp in _debuffdotTimers)
                {
                    foreach (var kvp2 in kvp.Value._timestamps)
                    {
                        Data.Spell spell;
                        if (Spell._loadedSpells.TryGetValue(kvp2.Key, out spell))
                        {
                            Spawn s;
                            if (_spawns.TryByID(kvp.Value._mobID, out s))
                            {
                                MQ.Write($"\ap{s.CleanName} \aw: \ag{spell.CastName} \aw: {(kvp2.Value - Core._stopWatch.ElapsedMilliseconds) / 1000} seconds");

                            }



                        }
                        else
                        {
                            Spawn s;
                            if (_spawns.TryByID(kvp.Value._mobID, out s))
                            {
                                MQ.Write($"\ap{s.CleanName} \aw: \agspellid:{kvp2.Key} \aw: {(kvp2.Value - Core._stopWatch.ElapsedMilliseconds) / 1000} seconds");

                            }

                        }

                    }
                }
                if (_debuffdotTimers.Count > 0)
                {
                    MQ.Write("\aw===================");

                }
                _printoutTimer = Core._stopWatch.ElapsedMilliseconds + 10000;

            }
        }
        private static bool ShouldDebuffCheck()
        {
            if (Core._stopWatch.ElapsedMilliseconds < _nextDebuffCheck)
            {
                return false;
            }
            else
            {
                _nextDebuffCheck = Core._stopWatch.ElapsedMilliseconds + _nextDebuffCheckInterval;
                return true;
            }
        }
        private static bool ShouldDoTCheck()
        {
            if (Core._stopWatch.ElapsedMilliseconds < _nextDoTCheck)
            {
                return false;
            }
            else
            {
                _nextDoTCheck = Core._stopWatch.ElapsedMilliseconds + _nextDoTCheckInterval;
                return true;
            }
        }
    }

}
