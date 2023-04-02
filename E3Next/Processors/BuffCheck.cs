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


        public static Logging _log = E3.Log;
        private static IMQ MQ = E3.MQ;
        private static ISpawns _spawns = E3.Spawns;
        //needs to be refreshed every so often in case of dispels
        //maybe after combat?
        public static Dictionary<Int32, SpellTimer> _buffTimers = new Dictionary<Int32, SpellTimer>();
        private static Int64 _nextBotCacheCheckTime = 0;
        private static Int64 nextBotCacheCheckTimeInterval = 1000;
        private static Int64 _nextInstantBuffRefresh = 0;
        private static Int64 _nextInstantRefreshTimeInterval = 250;
        private static List<Int32> _keyList = new List<int>();
        //private static Int64 _printoutTimer;
        private static Data.Spell _selectAura = null;
        private static Int64 _nextBuffCheck = 0;
        private static Int64 _nextBuffCheckInterval = 250;
        private static List<Int32> _xpBuffs = new List<int>() { 42962 /*xp6*/, 42617 /*xp5*/, 42616 /*xp4*/};
        private static List<Int32> _gmBuffs = new List<int>() { 34835, 35989, 35361, 25732, 34567, 36838, 43040, 36266, 36423 };
        private static Int64 _nextBlockBuffCheck = 0;
        private static Int64 _nextBlockBuffCheckInterval = 1000;
        static bool _initAuras = false;

        [SubSystemInit]
        public static void Init()
        {
            RegisterEvents();
        }
        private static void RegisterEvents()
        {

            EventProcessor.RegisterCommand("/dropbuff", (x) =>
            {
                if (x.args.Count > 0)
                {
                    string buffToDrop = x.args[0];
                    DropBuff(buffToDrop);
                    E3.Bots.BroadcastCommand($"/removebuff {buffToDrop}");
                }
            });

          
            EventProcessor.RegisterCommand("/blockbuff", (x) =>
            {
                if (x.args.Count > 0)
                {
                    string command = x.args[0];

                    if (command == "add")
                    {
                        if (x.args.Count > 1)
                        {
                            string spellName = x.args[1];

                            BlockBuffAdd(spellName);
                        }
                    }
                    else if (command == "remove")
                    {
                        if (x.args.Count > 1)
                        {
                            string spellName = x.args[1];
                            BlockBuffRemove(spellName);
                        }
                    }
                    else if (command == "list")
                    {
                        MQ.Write("\aoBlocked Spell List");
                        MQ.Write("\aw==================");
                        foreach (var spell in E3.CharacterSettings.BockedBuffs)
                        {
                            MQ.Write("\at" + spell.SpellName);
                        }
                    }
                }
            });
        }
        public static void BlockBuffRemove(string spellName)
        {
            List<Spell> newList = E3.CharacterSettings.BockedBuffs.Where(y => !y.SpellName.Equals(spellName, StringComparison.OrdinalIgnoreCase)).ToList();
            E3.CharacterSettings.BockedBuffs = newList;
            E3.CharacterSettings.SaveData();

        }
        public static void BlockBuffAdd(string spellName)
        {
            //check if it exists
            bool exists = false;
            foreach (var spell in E3.CharacterSettings.BockedBuffs)
            {

                if (spell.SpellName.Equals(spellName, StringComparison.OrdinalIgnoreCase))
                {
                    exists = true;
                }
            }
            if (!exists)
            {
                Spell s = new Spell(spellName);
                if (s.SpellID > 0)
                {
                    E3.CharacterSettings.BockedBuffs.Add(s);
                    E3.CharacterSettings.SaveData();
                }
            }
        }
        public static Boolean DropBuff(string buffToDrop)
        {
            //first look for exact match
            Int32 buffID = MQ.Query<Int32>($"${{Spell[{buffToDrop}].ID}}");
            if (buffID <1)
            {
                //lets look for a partial match.
                for (Int32 i = 1; i <= 40; i++)
                {
                    string buffName = MQ.Query<String>($"${{Me.Buff[{i}]}}");
                    if (buffName.IndexOf(buffToDrop, StringComparison.OrdinalIgnoreCase) > -1)
                    {
                        //it matches 
                        buffID = MQ.Query<Int32>($"${{Spell[{buffName}].ID}}");
                        //make sure the partial isn't a bottle.
                        if (_xpBuffs.Contains(buffID))
                        {
                            break;
                        }
                    }

                }
                //did we find it?
                if (buffID <1)
                {
                    for (Int32 i = 1; i <= 25; i++)
                    {
                        string buffName = MQ.Query<String>($"${{Me.Song[{i}]}}");
                        if (buffName.IndexOf(buffToDrop, StringComparison.OrdinalIgnoreCase) >-1)
                        {
                            //it matches 
                            buffID = MQ.Query<Int32>($"${{Spell[{buffName}].ID}}");
                            if (_xpBuffs.Contains(buffID))
                            {
                                break;
                            }
                        }
                    }
                }
            }

            if (buffID > 0)
            {
                MQ.Cmd($"/removebuff {buffToDrop}");
                return true;
            }
            return false;
        }
        public static Boolean HasBuff(string buffName)
        {
            bool hasBuff  = MQ.Query<bool>($"${{Me.Buff[{buffName}].ID}}");
            if(!hasBuff)
            {
                hasBuff = MQ.Query<bool>($"${{Me.Song[{buffName}].ID}}");
            }
            return hasBuff;
        }
        public static Boolean DropBuff(Int32 buffId)
        {
            //first look for exact match
            string buffName = String.Empty;
            if (buffName == String.Empty)
            {
                //lets look for a partial match.
                for (Int32 i = 1; i <= 40; i++)
                {
                    Int32 tbuffId = MQ.Query<Int32>($"${{Me.Buff[{i}].ID}}");
                    if (tbuffId == buffId)
                    {
                        buffName = MQ.Query<string>($"${{Me.Buff[{i}]}}");
                        break;
                    }

                }
                //did we find it?
                if (buffName == String.Empty)
                {
                    for (Int32 i = 1; i <= 25; i++)
                    {
                        Int32 tbuffId = MQ.Query<Int32>($"${{Me.Song[{i}].ID}}");
                        if (tbuffId == buffId)
                        {
                            buffName = MQ.Query<string>($"${{Me.Song[{i}]}}");
                            break;
                        }

                    }
                }

            }

            if (buffName != String.Empty)
            {
                MQ.Cmd($"/removebuff {buffName}");
                return true;
            }
            return false;
        }
        [ClassInvoke(Data.Class.All)]
        public static void Check_BlockedBuffs()
        {
            if (!e3util.ShouldCheck(ref _nextBlockBuffCheck, _nextBlockBuffCheckInterval)) return;
            foreach (var spell in E3.CharacterSettings.BockedBuffs)
            {
                if (spell.SpellID > 0)
                {
                    if (MQ.Query<bool>($"${{Me.Buff[{spell.CastName}]}}") || MQ.Query<bool>($"${{Me.Song[{spell.CastName}]}}"))
                    {
                        BuffCheck.DropBuff(spell.CastName);
                    }
                }
            }
            //shoving this here for now
            if (E3.CharacterSettings.Misc_RemoveTorporAfterCombat)
            {
                //auto remove torpor if not in combat and full health
                if (MQ.Query<Int32>("${Me.PctHPs}") > 95 && !Basics.InCombat())
                {
                    if (MQ.Query<bool>("${Me.Song[Transcendent Torpor]}"))
                    {
                        DropBuff("Transcendent Torpor");
                    }
                    if (MQ.Query<bool>("${Me.Song[Torpor]}"))
                    {
                        DropBuff("Torpor");
                    }
                }
            }
        }

        [AdvSettingInvoke]
        public static void Check_Buffs()
        {
            if (E3.IsInvis) return;


            //RefresBuffCacheForBots();
            //instant buffs have their own shouldcheck, need it snappy so check quickly.
            //BuffInstant(E3.CharacterSettings.InstantBuffs);

            if (!e3util.ShouldCheck(ref _nextBuffCheck, _nextBuffCheckInterval)) return;
            if (Basics.AmIDead()) return;

            using (_log.Trace())
            {

                if (Assist.IsAssisting || Nukes.PBAEEnabled)
                {
                    BuffBots(E3.CharacterSettings.CombatBuffs);
                }

                if ((!Movement.IsMoving() && String.IsNullOrWhiteSpace(Movement.FollowTargetName))|| Movement.StandingStillForTimePeriod())
                {
                    if(!Basics.InCombat())
                    {
                        if (!E3.ActionTaken) BuffAuras();
                        if (!E3.ActionTaken) BuffBots(E3.CharacterSettings.SelfBuffs);
                        if (!E3.ActionTaken) BuffBots(E3.CharacterSettings.BotBuffs);
                        if (!E3.ActionTaken) BuffBots(E3.CharacterSettings.PetBuffs, true);
                        
                    }
                }
                
            }

        }
        [AdvSettingInvoke]
        public static void check_CombatBuffs()
        {
            if (Assist.IsAssisting || Nukes.PBAEEnabled)
            {
                BuffBots(E3.CharacterSettings.CombatBuffs);
            }
        }
        public static void BuffInstant(List<Data.Spell> buffs)
        {
            if (E3.IsInvis) return;
            if (e3util.IsActionBlockingWindowOpen()) return;
            if (!e3util.ShouldCheck(ref _nextInstantBuffRefresh, _nextInstantRefreshTimeInterval)) return;
            //self only, instacast buffs only
            Int32 id = E3.CurrentId;
            foreach (var spell in buffs)
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
                    if (!Casting.Ifs(spell))
                    {
                        continue;
                    }
                }
                if (!(hasBuff || hasSong))
                {
                    bool willStack = MQ.Query<bool>($"${{Spell[{spell.SpellName}].WillLand}}");
                    if (willStack && Casting.CheckReady(spell) && Casting.CheckMana(spell))
                    {
                        if (spell.TargetType == "Self" || spell.TargetType == "Group v1")
                        {
                            Casting.Cast(0, spell);

                        }
                        else
                        {
                            if(Casting.InRange(id, spell))
                            {
                                Casting.Cast(id, spell);
                            }
                   
                        }

                    }
                }
            }
        }
        private static void BuffBots(List<Data.Spell> buffs, bool usePets = false)
        {
            if (e3util.IsActionBlockingWindowOpen()) return;
            foreach (var spell in buffs)
            {
                Spawn s;
                Spawn master = null; 

                string target = E3.CurrentName;
                if (!String.IsNullOrWhiteSpace(spell.CastTarget))
                {
                    if (spell.CastTarget.Equals("Self", StringComparison.OrdinalIgnoreCase))
                    {
                        target = E3.CurrentName;
                    }
                    else
                    {
                        target = spell.CastTarget;
                        if (string.Equals(spell.TargetType, "Single in Group", StringComparison.OrdinalIgnoreCase))
                        {
                            if (!_spawns.TryByName(target, out var spawn))
                            {
                                continue;
                            }

                            if (!Basics.GroupMembers.Any() || !Basics.GroupMembers.Contains(spawn.ID))
                            {
                                continue;
                            }
                        }
                    }
                }

                if (_spawns.TryByName(target, out s))
                {
                    if (usePets && s.PetID < 1)
                    {
                        continue;
                    }

                    if (usePets && s.PetID > 0)
                    {
                        Spawn ts;
                        if (_spawns.TryByID(s.PetID, out ts))
                        {
                            master = s;
                            s = ts;
                        }
                    }

                    SpellTimer st;
                    if (_buffTimers.TryGetValue(s.ID, out st))
                    {
                        Int64 timestamp;
                        if (st.Timestamps.TryGetValue(spell.SpellID, out timestamp))
                        {
                            if (Core.StopWatch.ElapsedMilliseconds < timestamp)
                            {
                                //buff is still on the player, kick off
                                continue;
                            }
                        }
                    }
                    if (!String.IsNullOrWhiteSpace(spell.Ifs))
                    {
                        if (!Casting.Ifs(spell))
                        {
                            //ifs failed do a 30 sec`retry

                            UpdateBuffTimers(s.ID, spell, 1500, true);
                            continue;
                        }
                    }
                    if (!Casting.InRange(s.ID, spell))
                    {
                        continue;
                    }
                    if (s.ID == E3.CurrentId)
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
                                    UpdateBuffTimers(s.ID, spell, 1500);
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
                                UpdateBuffTimers(s.ID, spell, 1500);
                                continue;
                            }


                        }
                        if (!(hasBuff || hasSong))
                        {
                            bool willStack = MQ.Query<bool>($"${{Spell[{spell.SpellName}].WillLand}}");
                            if (willStack && Casting.CheckReady(spell) && Casting.CheckMana(spell))
                            {
                                CastReturn result;
                                if (spell.TargetType == "Self" || spell.TargetType == "Group v1" || spell.TargetType == "Group v2")
                                {
                                    result = Casting.Cast(0, spell, Heals.SomeoneNeedsHealing);
                                }
                                else
                                {
                                    result = Casting.Cast(s.ID, spell, Heals.SomeoneNeedsHealing);
                                }

                                if (result == CastReturn.CAST_INTERRUPTED || result == CastReturn.CAST_INTERRUPTFORHEAL || result == CastReturn.CAST_FIZZLE)
                                {
                                    return;
                                }
                                if (result != CastReturn.CAST_SUCCESS)
                                {
                                    //possibly some kind of issue/blocking. set a 60 sec timer to try and recast later.
                                    UpdateBuffTimers(s.ID, spell, 60 * 1000, true);
                                }
                                else
                                {
                                    //lets verify what we have.
                                    //MQ.Delay(100);
                                    //Int64 timeLeftInMS = Casting.TimeLeftOnMyBuff(spell);
                                    //if (timeLeftInMS < 0)
                                    //{
                                    //    //some issue, lets wait
                                    //    timeLeftInMS = 60 * 1000;
                                    //}
                                    UpdateBuffTimers(s.ID, spell, 1500);
                                }
                                return;
                            }
                            else if (!willStack)
                            {
                                //won't stack don't check back for awhile
                                UpdateBuffTimers(s.ID, spell, 1500);
                            }
                            else
                            {
                                //we don't have mana for this? or ifs failed? chill for 12 sec.
                                UpdateBuffTimers(s.ID, spell, 12 * 1000, true);
                            }
                        }
                        else
                        {
                            //they have the buff, update the time
                            //Int64 timeLeftInMS = Casting.TimeLeftOnMyBuff(spell);
                            //if (timeLeftInMS < 0)
                            //{
                            //    //some issue, lets wait
                            //    timeLeftInMS = 120 * 1000;
                            //    UpdateBuffTimers(s.ID, spell, timeLeftInMS, true);
                            //}
                            //else
                            //{
                            //    UpdateBuffTimers(s.ID, spell, timeLeftInMS);
                            //}
                            UpdateBuffTimers(s.ID, spell, 1500);
                            continue;
                        }
                    }
                    else if (s.ID == MQ.Query<Int32>("${Me.Pet.ID}"))
                    {
                        //its my pet
                        Int32 buffCount = MQ.Query<Int32>("${Me.Pet.BuffCount}");

                        bool hasBuff = false;

                        if(buffCount<31)
                        {
                            hasBuff = MQ.Query<bool>($"${{Bool[${{Me.Pet.Buff[{spell.SpellName}]}}]}}");
                        }
                       
                        bool hasCheckFor = false;
                        bool hasCachedCheckFor = false;
                        if (!String.IsNullOrWhiteSpace(spell.CheckFor))
                        {
                            hasCheckFor = MQ.Query<bool>($"${{Bool[${{Me.Pet.Buff[{spell.CheckFor}]}}]}}");
                            hasCachedCheckFor = MQ.Query<bool>($"${{Bool[${{Spawn[${{Me.Pet.ID}}].Buff[{spell.CheckFor}]}}]}}");
                            if (hasCheckFor || hasCachedCheckFor)
                            {

                                UpdateBuffTimers(s.ID, spell, 1500);
                                continue;
                            }
                        }
                        if (!(hasBuff))
                        {
                            bool willStack = MQ.Query<bool>($"${{Spell[{spell.SpellName}].WillLandPet}}");
                            if (willStack && Casting.CheckReady(spell) && Casting.CheckMana(spell))
                            {
                                CastReturn result;

                                result = Casting.Cast(s.ID, spell, Heals.SomeoneNeedsHealing);
                                if (result == CastReturn.CAST_INTERRUPTED || result == CastReturn.CAST_INTERRUPTFORHEAL || result == CastReturn.CAST_FIZZLE)
                                {
                                    return;
                                }
                                if (result != CastReturn.CAST_SUCCESS)
                                {
                                    //possibly some kind of issue/blocking. set a 120 sec timer to try and recast later.
                                    UpdateBuffTimers(s.ID, spell, 60 * 1000, true);
                                }
                                else
                                {
                                    //lets verify what we have.
                                    MQ.Delay(100);
                                    if (buffCount < 31)
                                    {
                                        UpdateBuffTimers(s.ID, spell, 1500);
                                    }
                                    else
                                    {
                                        UpdateBuffTimers(s.ID, spell, (spell.DurationTotalSeconds * 1000));
                                    }
                                   
                                }
                                return;
                            }
                            else if (!willStack)
                            {
                                //won't stack don't check back for awhile
                                UpdateBuffTimers(s.ID, spell, 1500);
                            }
                            else
                            {
                                //we don't have mana for this? or ifs failed? chill for 12 sec.
                                UpdateBuffTimers(s.ID, spell, 12 * 1000, true);
                            }
                        }
                        else
                        {
                            //they have the buff, update the time
                            UpdateBuffTimers(s.ID, spell, 1500);
                            continue;
                        }

                    }
                    else
                    {
                        //someone other than us.
                        //if its a netbots, we initially do target, then have the cache refreshed
                        //using a func here so that we can swap out the logic of Pet buff vs normal buffs
                        Func<String,List<Int32>> findBuffList = E3.Bots.BuffList;
                        if (usePets)
                        {
                            findBuffList = E3.Bots.PetBuffList;
                        }

                        bool isABot = E3.Bots.BotsConnected().Contains(spell.CastTarget, StringComparer.OrdinalIgnoreCase);
                        if (isABot)
                        {

                            //its one of our bots, we can directly access short buffs
                            if (!String.IsNullOrWhiteSpace(spell.CheckFor))
                            {
                                bool hasCheckFor = findBuffList(spell.CastTarget).Contains(spell.CheckForID);
                                //can't check for target song buffs, be aware. will have to check netbots. 
                                if (hasCheckFor)
                                {
                                    //can't see the time, just set it for this time to recheck
                                    //6 seconds
                                    UpdateBuffTimers(s.ID, spell, 1500);
                                    continue;
                                }

                            }

                            bool hasBuff = hasBuff = findBuffList(spell.CastTarget).Contains(spell.SpellID);

                            if (!hasBuff)
                            {

                                if (Casting.CheckReady(spell) && Casting.CheckMana(spell))
                                {
                                    Casting.TrueTarget(s.ID);
                                    MQ.Delay(2000, "${Target.BuffsPopulated}");
                                    bool willStack = MQ.Query<bool>($"${{Spell[{spell.SpellName}].StacksTarget}}");
                                    if (willStack)
                                    {
                                        //then we can cast!
                                        var result = Casting.Cast(s.ID, spell, Heals.SomeoneNeedsHealing);
                                        if (result == CastReturn.CAST_INTERRUPTED || result == CastReturn.CAST_INTERRUPTFORHEAL || result == CastReturn.CAST_FIZZLE)
                                        {
                                            return;
                                        }
                                        if (result != CastReturn.CAST_SUCCESS)
                                        {
                                            //possibly some kind of issue/blocking.
                                            UpdateBuffTimers(s.ID, spell, 10000, true);
                                        }
                                        else
                                        {
                                            //lets verify what we have on that target.
                                            UpdateBuffTimers(s.ID, spell, 1500, true);

                                        }
                                        return;
                                    }
                                    else
                                    {
                                        //won't stack don't check back for awhile
                                        UpdateBuffTimers(s.ID, spell, 1500);
                                    }
                                }
                                else
                                {   //spell not ready
                                    UpdateBuffTimers(s.ID, spell, 1500);

                                }
                            }
                            else
                            {
                                //has the buff
                                UpdateBuffTimers(s.ID, spell, 1500, true);
                                continue;
                            }

                        }
                        else
                        {
                            //its someone not in our buff group, do it the hacky way.
                            Casting.TrueTarget(s.ID);
                            MQ.Delay(2000, "${Target.BuffsPopulated}");

                            bool willStack = MQ.Query<bool>($"${{Spell[{spell.SpellName}].StacksTarget}}");
                            //MQ.Write($"Will stack:{spell.SpellName}:" + willStack);
                            if (!willStack)
                            {
                                //won't stack don't check back for awhile
                                UpdateBuffTimers(s.ID, spell, 30 * 1000);
                            }
                            //double ifs check, so if their if included Target, we have it
                            if (!String.IsNullOrWhiteSpace(spell.Ifs))
                            {
                                if (!Casting.Ifs(spell))
                                {
                                    MQ.Write($"Failed if:{spell.SpellName}:");

                                    //ifs failed do a 30 sec retry, so we don't keep swapping targets
                                    UpdateBuffTimers(s.ID, spell, 30 * 1000, true);
                                    continue;
                                }
                            }
                            //greater than 0, so we don't get things like shrink that don't have a duration
                            bool isShortDuration = spell.DurationTotalSeconds <= 60 && spell.DurationTotalSeconds > 0;

                            if (isShortDuration)
                            {
                                //we cannot do target based checks if a short duration type.
                                //have to do netbots
                                //looks live you get it in the target area. 


                                //not one of our buffs uhh, try and cast and see if we get a non success message.
                                if (Casting.CheckReady(spell) && Casting.CheckMana(spell))
                                {
                                    var result = Casting.Cast(s.ID, spell, Heals.SomeoneNeedsHealing);
                                    if (result == CastReturn.CAST_INTERRUPTED || result == CastReturn.CAST_INTERRUPTFORHEAL || result == CastReturn.CAST_FIZZLE)
                                    {
                                        return;
                                    }
                                    if (result != CastReturn.CAST_SUCCESS)
                                    {
                                        //possibly some kind of issue/blocking. set a N sec timer to try and recast later.
                                        UpdateBuffTimers(s.ID, spell, 60 * 1000, true);
                                    }
                                    else
                                    {
                                        UpdateBuffTimers(s.ID, spell, spell.Duration);
                                    }
                                    return;
                                }
                                continue;

                            }
                            else
                            {

                                Int64 timeLeftInMS = Casting.TimeLeftOnTargetBuff(spell);

                                if (timeLeftInMS < 30000)
                                {
                                    if (Casting.CheckReady(spell) && Casting.CheckMana(spell))
                                    {
                                        var result = Casting.Cast(s.ID, spell, Heals.SomeoneNeedsHealing);
                                        if (result == CastReturn.CAST_INTERRUPTED || result == CastReturn.CAST_INTERRUPTFORHEAL || result == CastReturn.CAST_FIZZLE)
                                        {
                                            return;
                                        }
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

                    }
                }
            }
            // Casting.TrueTarget(currentid, true);
        }

        private static void BuffAuras()
        {
            if (!E3.CharacterSettings.Buffs_CastAuras) return;
            if (e3util.IsActionBlockingWindowOpen()) return;
            if (_selectAura == null)
            {
                if (!_initAuras)
                {
                    foreach (var aura in _auraList)
                    {
                        if (MQ.Query<bool>($"${{Me.CombatAbility[{aura}]}}")) _selectAura = new Spell(aura);
                        if (MQ.Query<bool>($"${{Me.Book[{aura}]}}")) _selectAura = new Spell(aura);
                        if (MQ.Query<bool>($"${{Me.AltAbility[{aura}]}}")) _selectAura = new Spell(aura);
                    }
                    _initAuras = true;
                    if (_selectAura != null)
                    {
                        _selectAura.SpellName = _selectAura.SpellName.Replace("'s", "s");
                    }
                }
            }
            //we have something we want on!
            if (_selectAura != null)
            {
                string currentAura = MQ.Query<string>("${Me.Aura[1]}");
                if (currentAura != "NULL")
                {
                    //we already have an aura, check if its different
                    if (currentAura.Equals(_selectAura.SpellName, StringComparison.OrdinalIgnoreCase))
                    {
                        //don't need to do anything
                        return;
                    }
                    //else remove it as we are putting on something else.
                    MQ.Cmd($"/removeaura {currentAura}");
                }

                //need to put on new aura
                Int32 meID = E3.CurrentId;
                if (_selectAura.CastType == CastType.Spell)
                {
                    //this is a spell, need to mem, then cast. 
                    if (Casting.CheckReady(_selectAura) && Casting.CheckMana(_selectAura))
                    {
                        Casting.Cast(meID, _selectAura);
                    }


                }
                else if (_selectAura.CastType == CastType.Disc)
                {
                    Int32 endurance = MQ.Query<Int32>("${Me.Endurance}");
                    if (_selectAura.EnduranceCost < endurance)
                    {
                        //alt ability or disc, just cast
                        Casting.Cast(meID, _selectAura);
                    }
                }
                else
                {
                    //this is a spell, need to mem, then cast. 
                    if (Casting.CheckReady(_selectAura))
                    {
                        Casting.Cast(meID, _selectAura);
                    }
                }


            }


        }
        //order is important, last one wins in stacking
        private static List<string> _auraList = new List<string>() {
            "Myrmidon's Aura",
            "Champion's Aura",
            "Disciple's Aura",
            "Master's Aura",
            "Aura of Rage",
            "Bloodlust Aura",
            "Aura of Insight",
            "Aura of the Muse",
            "Aura of the Zealot",
            "Aura of the Pious",
            "Aura of Divinity",
            "Aura of the Grove",
            "Aura of Life",
            "Beguiler's Aura",
            "Illusionist's Aura",
            "Twincast Aura",
            "Holy Aura",
            "Blessed Aura",
            "Spirit Mastery",
            "Auroria Mastery"};

        private static Int64 GetBuffTimer(Int32 mobid, Data.Spell spell)
        {
            SpellTimer s;
            if (_buffTimers.TryGetValue(mobid, out s))
            {
                if (!s.Timestamps.ContainsKey(spell.SpellID))
                {
                    return -1;
                }

                return s.Timestamps[spell.SpellID];

            }
            else
            {
                return -1;
            }
        }
        //used to just store removed items, keep it around to not create garbage
        private static List<Int32> _refreshBuffCacheRemovedItems = new List<int>();
        public static void RefresBuffCacheForBots()
        {
            if (Core.StopWatch.ElapsedMilliseconds > _nextBotCacheCheckTime)
            {
                //this is so we can get up to date buff data from the bots, without having to target/etc.
                _refreshBuffCacheRemovedItems.Clear();
                //_spawns.RefreshList();
                foreach (var kvp in _buffTimers)
                {

                    Int32 userID = kvp.Key;
                    Spawn s;
                    if (_spawns.TryByID(userID, out s))
                    {
                        List<Int32> list = E3.Bots.BuffList(s.Name);
                        if (list.Count == 0)
                        {
                            continue;
                        }
                        //this is one of our bots!
                        //doing it this way to not generate garbage by creating new lists.
                        _keyList.Clear();
                        foreach (var pair in kvp.Value.Timestamps)
                        {
                            if (!list.Contains(pair.Key))
                            {
                                _keyList.Add(pair.Key);
                            }
                        }
                        foreach (var key in _keyList)
                        {
                            if (!kvp.Value.Lockedtimestamps.ContainsKey(key))
                            {
                                kvp.Value.Timestamps[key] = 0;
                            }

                        }
                    }
                    else
                    {
                        //remove them from the collection.
                        _refreshBuffCacheRemovedItems.Add(kvp.Key);
                    }
                }
                foreach (Int32 removedItem in _refreshBuffCacheRemovedItems)
                {
                    if (_buffTimers.ContainsKey(removedItem))
                    {
                        _buffTimers[removedItem].Dispose();
                        _buffTimers.Remove(removedItem);
                    }
                }
                _refreshBuffCacheRemovedItems.Clear();
                _nextBotCacheCheckTime = Core.StopWatch.ElapsedMilliseconds + nextBotCacheCheckTimeInterval;
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="mobid"></param>
        /// <param name="spell"></param>
        /// <param name="timeLeftInMS"></param>
        /// <param name="locked">Means the buff cache cannot override it</param>
        private static void UpdateBuffTimers(Int32 mobid, Data.Spell spell, Int64 timeLeftInMS, bool locked = false)
        {
            SpellTimer s;
            //if we have no time left, as it was not found, just set it to 0 in ours

            if (_buffTimers.TryGetValue(mobid, out s))
            {
                if (!s.Timestamps.ContainsKey(spell.SpellID))
                {
                    s.Timestamps.Add(spell.SpellID, 0);
                }

                s.Timestamps[spell.SpellID] = Core.StopWatch.ElapsedMilliseconds + timeLeftInMS;

                if (locked)
                {
                    if (!s.Lockedtimestamps.ContainsKey(spell.SpellID))
                    {
                        s.Lockedtimestamps.Add(spell.SpellID, timeLeftInMS);
                    }
                }
                else
                {
                    if (s.Lockedtimestamps.ContainsKey(spell.SpellID))
                    {
                        s.Lockedtimestamps.Remove(spell.SpellID);
                    }
                }

            }
            else
            {
                SpellTimer ts = SpellTimer.Aquire();
                ts.MobID = mobid;

                ts.Timestamps.Add(spell.SpellID, Core.StopWatch.ElapsedMilliseconds + timeLeftInMS);
                _buffTimers.Add(mobid, ts);
                if (locked)
                {
                    if (!ts.Lockedtimestamps.ContainsKey(spell.SpellID))
                    {
                        ts.Lockedtimestamps.Add(spell.SpellID, timeLeftInMS);
                    }
                }
                else
                {
                    if (ts.Lockedtimestamps.ContainsKey(spell.SpellID))
                    {
                        ts.Lockedtimestamps.Remove(spell.SpellID);
                    }
                }
            }
        }


    }
}
