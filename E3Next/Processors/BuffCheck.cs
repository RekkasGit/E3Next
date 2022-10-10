using E3Core.Classes;
using E3Core.Data;
using E3Core.Settings;
using E3Core.Utility;
using MonoCore;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Net.Security;

namespace E3Core.Processors
{
    public static class BuffCheck
    {


        public static Logging _log = E3._log;
        private static IMQ MQ = E3.MQ;
        private static ISpawns _spawns = E3._spawns;
        //needs to be refreshed every so often in case of dispels
        //maybe after combat?
        public static Dictionary<Int32, SpellTimer> _buffTimers = new Dictionary<Int32, SpellTimer>();
        private static Int64 _nextBotBuffRefresh = 0;
        private static Int64 _nextBotRefreshTimeInterval = 2000;
        private static Int64 _nextInstantBuffRefresh = 0;
        private static Int64 _nextInstantRefreshTimeInterval = 250;
        private static List<Int32> _keyList = new List<int>();
        private static Int64 _printoutTimer;

        private static Int64 _nextBuffCheck = 0;
        private static Int64 _nextBuffCheckInterval = 1000;
        public static void Init()
        {
            RegisterEvents();
        }
        private static void RegisterEvents()
        {

           
        }
       
       
        [AdvSettingInvoke]
        public static void Check_Buffs()
        {
            //instant buffs have their own shouldcheck, need it snappy so check quickly.
            BuffInstant(E3._characterSettings.InstantBuffs);

            if (!e3util.ShouldCheck(ref _nextBuffCheck,_nextBuffCheckInterval)) return;

            RefresBuffCacheForBots();
            string combatState = MQ.Query<string>("${Me.CombatState}");
            bool moving = MQ.Query<bool>("${Me.Moving}");


            e3util.PrintTimerStatus(_buffTimers, ref _printoutTimer, "Buffs");

            if (Assist._assistTargetID>0 || combatState=="COMBAT")
            {
                BuffBots(E3._characterSettings.CombatBuffs);

            }
            else if(!moving)
            {
                BuffBots(E3._characterSettings.SelfBuffs);
                BuffBots(E3._characterSettings.BotBuffs);
                BuffBots(E3._characterSettings.PetBuffs,true);
                //TODO: Auras
            }


        }
        private static void BuffInstant(List<Data.Spell> buffs)
        {
            if (!e3util.ShouldCheck(ref _nextInstantBuffRefresh, _nextInstantRefreshTimeInterval)) return;
            //self only, instacast buffs only
            Int32 id = MQ.Query<Int32>("${Me.ID}");
            foreach(var spell in buffs)
            {
                bool hasBuff = MQ.Query<bool>($"${{Bool[${{Me.Buff[{spell.SpellName}]}}]}}");
                bool hasSong = false;
                if (!hasBuff)
                {
                    hasSong = MQ.Query<bool>($"${{Bool[${{Me.Song[{spell.SpellName}]}}]}}");
                }

                bool hasCheckFor = false;
                if (!String.IsNullOrWhiteSpace(spell.CheckFor))
                {
                    hasCheckFor = MQ.Query<bool>($"${{Bool[${{Me.Buff[{spell.CheckFor}]}}]}}");
                    if (hasCheckFor)
                    {
                        continue;   
                    }
                    hasCheckFor = MQ.Query<bool>($"${{Bool[${{Me.Song[{spell.CheckFor}]}}]}}");
                    if (hasCheckFor)
                    {
                        continue;
                    }

                }
                if (!String.IsNullOrWhiteSpace(spell.Ifs))
                {
                    if (!MQ.Query<bool>($"${{If[{spell.Ifs},TRUE,FALSE]}}"))
                    {
                        continue;
                    }
                }
                if (!(hasBuff || hasSong))
                {
                    bool willStack = MQ.Query<bool>($"${{Spell[{spell.SpellName}].WillLand}}");
                    if (willStack && Casting.CheckReady(spell) && Casting.checkMana(spell))
                    {
                        Casting.Cast(id, spell);
                       
                    }
                }
            }
        }
        private static void BuffBots(List<Data.Spell> buffs, bool usePets=false)
        { 
            foreach(var spell in buffs)
            {
                Spawn s;

                string target = E3._currentName;
                if(!String.IsNullOrWhiteSpace(spell.CastTarget))
                {
                    if(spell.CastTarget.Equals("Self", StringComparison.OrdinalIgnoreCase))
                    {
                        target = E3._currentName;
                    }
                    else
                    {
                        target = spell.CastTarget;

                    }
                }

                if (_spawns.TryByName(target, out s))
                {
                    if (usePets && s.PetID > 0)
                    {
                        Spawn ts;
                        if (_spawns.TryByID(s.PetID, out ts))
                        {
                            s = ts;
                        }
                    }
                    SpellTimer st;
                    if (_buffTimers.TryGetValue(s.ID, out st))
                    {
                        Int64 timestamp;
                        if (st._timestamps.TryGetValue(spell.SpellID, out timestamp))
                        {
                            if (Core._stopWatch.ElapsedMilliseconds < timestamp)
                            {
                                //buff is still on the player, kick off
                                continue;
                            }
                        }
                    }
                    if (!String.IsNullOrWhiteSpace(spell.Ifs))
                    {
                        if (!MQ.Query<bool>($"${{If[{spell.Ifs},TRUE,FALSE]}}"))
                        {
                            //ifs failed do a 30 sec`retry

                            //UpdateBuffTimers(s.ID, spell, 30 * 1000, true);
                            continue;
                        }
                    }
                  
                    if (s.CleanName == E3._currentName)
                    {
                        //self buffs!
                        bool hasBuff = MQ.Query<bool>($"${{Bool[${{Me.Buff[{spell.SpellName}]}}]}}");
                        bool hasSong = false;
                        if (!hasBuff)
                        {
                            hasSong = MQ.Query<bool>($"${{Bool[${{Me.Song[{spell.SpellName}]}}]}}");
                        }

                        bool hasCheckFor = false;
                        if (!String.IsNullOrWhiteSpace(spell.CheckFor))
                        {
                            hasCheckFor = MQ.Query<bool>($"${{Bool[${{Me.Buff[{spell.CheckFor}]}}]}}");
                            if (!hasCheckFor)
                            {
                                hasCheckFor = MQ.Query<bool>($"${{Bool[${{Me.Song[{spell.CheckFor}]}}]}}");
                                if (hasCheckFor)
                                {
                                    Int64 buffDuration = MQ.Query<Int64>($"${{Me.Song[{spell.CheckFor}].Duration}}");
                                    if (buffDuration < 1000)
                                    {
                                        buffDuration = 1000;
                                    }
                                    //don't let the refresh update this
                                    UpdateBuffTimers(s.ID, spell, buffDuration);
                                    continue;
                                }
                            }
                            else
                            {
                                Int64 buffDuration = MQ.Query<Int64>($"${{Me.Buff[{spell.CheckFor}].Duration}}");
                                if (buffDuration < 1000)
                                {
                                    buffDuration = 1000;
                                }
                                UpdateBuffTimers(s.ID, spell, buffDuration);
                                continue;
                            }


                        }
                        if (!(hasBuff || hasSong))
                        {
                            bool willStack = MQ.Query<bool>($"${{Spell[{spell.SpellName}].WillLand}}");
                            if (willStack && Casting.CheckReady(spell) && Casting.checkMana(spell))
                            {
                                var result = Casting.Cast(s.ID, spell, Heals.SomeoneNeedsHealing);
                                if (result != CastReturn.CAST_SUCCESS)
                                {
                                    //possibly some kind of issue/blocking. set a 120 sec timer to try and recast later.
                                    UpdateBuffTimers(s.ID, spell, 120 * 1000, true);
                                }
                                else
                                {
                                    //lets verify what we have.
                                    MQ.Delay(100);
                                    Int64 timeLeftInMS = Casting.TimeLeftOnMyBuff(spell);
                                    if (timeLeftInMS < 0)
                                    {
                                        //some issue, lets wait
                                        timeLeftInMS = 120 * 1000;
                                    }
                                    UpdateBuffTimers(s.ID, spell, timeLeftInMS);
                                }
                            }
                            else if (!willStack)
                            {
                                //won't stack don't check back for awhile
                                UpdateBuffTimers(s.ID, spell, spell.Duration);
                            }
                            else
                            {
                                //we don't have mana for this? or ifs failed? chill for 12 sec.
                                UpdateBuffTimers(s.ID, spell, 12 * 1000,true);
                            }
                        }
                        else
                        {
                            //they have the buff, update the time
                            Int64 timeLeftInMS = Casting.TimeLeftOnMyBuff(spell);
                            if (timeLeftInMS < 0)
                            {
                                //some issue, lets wait
                                timeLeftInMS = 120 * 1000;
                                UpdateBuffTimers(s.ID, spell, timeLeftInMS,true);
                            }
                            else
                            {
                                UpdateBuffTimers(s.ID, spell, timeLeftInMS);
                            }
                           
                            continue;
                        }
                    }
                    else
                    {
                        //someone other than us.
                        //if its a netbots, we initially do target, then have the cache refreshed

                        int currentid = MQ.Query<Int32>("${Target.ID}");
                        //need to change target to be sure ifs run correctly.
                        Casting.TrueTarget(s.ID);
                        MQ.Delay(2000, "${Target.BuffsPopulated}");
                        MQ.Delay(100);
                        try
                        {   //double ifs check, so if their if included Target, we have it
                            if (!String.IsNullOrWhiteSpace(spell.Ifs))
                            {
                                if (!MQ.Query<bool>($"${{If[{spell.Ifs},TRUE,FALSE]}}"))
                                {
                                    //ifs failed do a 30 sec retry, so we don't keep swapping targets
                                    UpdateBuffTimers(s.ID, spell, 30 * 1000, true);
                                    continue;
                                }
                            }
                            //greater than 0, so we don't get things like shrink that don't have a duration
                            bool isShortDuration = spell.DurationTotalSeconds <= 90 && spell.DurationTotalSeconds > 0;

                            if (isShortDuration)
                            {
                                //we cannot do target based checks if a short duration type.
                                //have to do netbots
                                //looks live you get it in the target area. 

                                bool botInZone = false;
                                botInZone = E3._bots.InZone(spell.CastTarget);
                                if (!botInZone)
                                {   //not one of our buffs uhh, try and cast and see if we get a non success message.
                                    if (Casting.CheckReady(spell) && Casting.checkMana(spell))
                                    {
                                        var result = Casting.Cast(s.ID, spell, Heals.SomeoneNeedsHealing);
                                        if (result != CastReturn.CAST_SUCCESS)
                                        {
                                            //possibly some kind of issue/blocking. set a 90 sec timer to try and recast later.
                                            UpdateBuffTimers(s.ID, spell, 90 * 1000, true);
                                        }
                                        else
                                        {
                                            UpdateBuffTimers(s.ID, spell, spell.Duration);
                                        }
                                    }
                                    continue;


                                }
                                else
                                {

                                    //its one of our bots, we can directly access short buffs
                                    if (!String.IsNullOrWhiteSpace(spell.CheckFor))
                                    {
                                        bool hasCheckFor = E3._bots.HasShortBuff(spell.CastTarget, spell.CheckForID);
                                        //can't check for target song buffs, be aware. will have to check netbots. 
                                        if (hasCheckFor)
                                        {
                                            //can't see the time, just set it for this time to recheck
                                            //6 seconds
                                            UpdateBuffTimers(s.ID, spell, 6 * 1000);
                                            continue;
                                        }

                                    }

                                    bool hasBuff = E3._bots.HasShortBuff(spell.CastTarget, spell.SpellID);

                                    if (!hasBuff)
                                    {
                                        if (Casting.CheckReady(spell) && Casting.checkMana(spell))
                                        {
                                            //then we can cast!
                                            var result = Casting.Cast(s.ID, spell, Heals.SomeoneNeedsHealing);
                                            if (result != CastReturn.CAST_SUCCESS)
                                            {
                                                //possibly some kind of issue/blocking. set a 90 sec timer to try and recast later.
                                                UpdateBuffTimers(s.ID, spell, 90 * 1000, true);
                                                continue;
                                            }
                                            else
                                            {
                                                //lets verify what we have on that target.
                                                UpdateBuffTimers(s.ID, spell, spell.Duration);
                                                continue;
                                            }
                                        }
                                    }
                                    else
                                    {
                                        //has the buff, no clue how much time is left, set a 6 sec retry.
                                        UpdateBuffTimers(s.ID, spell, 6000, true);
                                        continue;
                                    }


                                }

                            }
                            else
                            {

                                Int64 timeLeftInMS = Casting.TimeLeftOnTargetBuff(spell);

                                if (timeLeftInMS < 30000)
                                {
                                    if (Casting.CheckReady(spell) && Casting.checkMana(spell))
                                    {
                                        var result = Casting.Cast(s.ID, spell, Heals.SomeoneNeedsHealing);
                                        if (result != CastReturn.CAST_SUCCESS)
                                        {
                                            //possibly some kind of issue/blocking. set a 120 sec timer to try and recast later.
                                            UpdateBuffTimers(s.ID, spell, 120 * 1000, true);
                                            continue;
                                        }
                                        else
                                        {
                                            if (spell.Duration > 0)
                                            {
                                                //lets verify what we have on that target.
                                                Casting.TrueTarget(s.ID);
                                                MQ.Delay(2000, "${Target.BuffsPopulated}");
                                                MQ.Delay(100);
                                                timeLeftInMS = Casting.TimeLeftOnTargetBuff(spell);
                                                if (timeLeftInMS < 0)
                                                {
                                                    timeLeftInMS = 120 * 1000;
                                                    UpdateBuffTimers(s.ID, spell, timeLeftInMS, true);

                                                }
                                                else
                                                {
                                                    UpdateBuffTimers(s.ID, spell, timeLeftInMS);
                                                }

                                                continue;
                                            }
                                            else
                                            {   //stuff like shrink
                                                //UpdateBuffTimers(s.ID, spell, Int32.MaxValue, true);
                                                continue;
                                            }

                                        }
                                    }

                                }
                                else
                                {
                                    UpdateBuffTimers(s.ID, spell, timeLeftInMS);
                                    continue;
                                }
                            }

                        }
                        finally
                        {
                          
                                //put it back to where it was
                                Casting.TrueTarget(currentid, true);

                            
                        }
                     

                    }
                }
            }
        }
        private static Int64 GetBuffTimer(Int32 mobid, Data.Spell spell)
        {
            SpellTimer s;
            if (_buffTimers.TryGetValue(mobid, out s))
            {
                if (!s._timestamps.ContainsKey(spell.SpellID))
                {
                    return -1;
                }

                return s._timestamps[spell.SpellID];

            }
            else
            {
                return -1;
            }
        }
        public static void RefresBuffCacheForBots()
        {
            if (Core._stopWatch.ElapsedMilliseconds > _nextBotBuffRefresh)
            {
                //this is so we can get up to date buff data from the bots, without having to target/etc.
                foreach (var kvp in _buffTimers)
                {

                    Int32 userID = kvp.Key;
                    Spawn s;
                    if (_spawns.TryByID(userID, out s))
                    {
                        List<Int32> list = E3._bots.BuffList(s.Name);
                        if (list.Count == 0)
                        {
                            continue;
                        }
                        //this is one of our bots!
                        //doing it this way to not generate garbage by creating new lists.
                        _keyList.Clear();
                        foreach (var pair in kvp.Value._timestamps)
                        {
                            if (!list.Contains(pair.Key))
                            {
                                _keyList.Add(pair.Key);
                            }
                        }
                        foreach (var key in _keyList)
                        {
                            if (!kvp.Value._lockedtimestamps.ContainsKey(key))
                            {
                                kvp.Value._timestamps[key] = 0;
                            }

                        }
                    }
                }
                _nextBotBuffRefresh = Core._stopWatch.ElapsedMilliseconds + _nextBotRefreshTimeInterval;
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="mobid"></param>
        /// <param name="spell"></param>
        /// <param name="timeLeftInMS"></param>
        /// <param name="locked">Means the buff cache cannot override it</param>
        private static void UpdateBuffTimers(Int32 mobid, Data.Spell spell, Int64 timeLeftInMS, bool locked=false)
        {
            SpellTimer s;
            //if we have no time left, as it was not found, just set it to 0 in ours
        
            if (_buffTimers.TryGetValue(mobid, out s))
            {
                if (!s._timestamps.ContainsKey(spell.SpellID))
                {
                    s._timestamps.Add(spell.SpellID, 0);
                }

                s._timestamps[spell.SpellID] = Core._stopWatch.ElapsedMilliseconds + timeLeftInMS;

                if(locked)
                {
                    if (!s._lockedtimestamps.ContainsKey(spell.SpellID))
                    {
                        s._lockedtimestamps.Add(spell.SpellID,timeLeftInMS);
                    }
                }
                else
                {
                    if (s._lockedtimestamps.ContainsKey(spell.SpellID))
                    {
                        s._lockedtimestamps.Remove(spell.SpellID);
                    }
                }

            }
            else
            {
                SpellTimer ts = SpellTimer.Aquire();
                ts._mobID = mobid;

                ts._timestamps.Add(spell.SpellID, Core._stopWatch.ElapsedMilliseconds + timeLeftInMS);
                _buffTimers.Add(mobid, ts);
                if (locked)
                {
                    if (!ts._lockedtimestamps.ContainsKey(spell.SpellID))
                    {
                        ts._lockedtimestamps.Add(spell.SpellID, timeLeftInMS);
                    }
                }
                else
                {
                    if (ts._lockedtimestamps.ContainsKey(spell.SpellID))
                    {
                        ts._lockedtimestamps.Remove(spell.SpellID);
                    }
                }
            }
        }
        

    }
}
