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
    public static class Assist
    {
        public static Logging _log = E3._log;
        private static IMQ MQ = E3.MQ;
        private static ISpawns _spawns = E3._spawns;

        private static bool use_FULLBurns = false;
        private static bool use_QUICKBurns = false;
        private static bool use_EPICBurns = false;
        private static bool use_LONGBurns = false;
        private static bool use_Swarms = false;
      
        public static Boolean _isAssisting = false;
        public static Int32 _assistTargetID = 0;
        public static Int32 _assistStickDistance = 10;
        public static List<Data.Spell> _epicWeapon = new List<Data.Spell>();
        public static List<Data.Spell> _anguishBP = new List<Data.Spell>();
        public static string _epicWeaponName = String.Empty;
        public static string _anguishBPName = String.Empty;
        private static IList<string> _rangeTypes = new List<string>() { "Ranged", "Autofire" };
        private static IList<string> _meleeTypes = new List<string>() { "Melee" };
        private static IList<string> _assistDistanceTypes = new List<string> { "MaxMelee", "off" };
        public static Int32 _assistDistance = 0;
        public static bool _assistIsEnraged = false;
        private static Dictionary<string, Action> _stickSwitch;
        private static HashSet<Int32> _offAssistIgnore = new HashSet<Int32>();
        private static Data.Spell _divineStun = new Data.Spell("Divine Stun");
        private static Data.Spell _terrorOfDiscord = new Data.Spell("Terror of Discord");
        private static IList<string> _tankTypes = new List<string>() { "WAR", "PAL", "SK" };
        private static Double _nukeDelayTimeStamp;
        
        private static Dictionary<Int32, SpellTimer> _debuffdotTimers = new Dictionary<Int32, SpellTimer>();

        private static HashSet<Int32> _mobsToDot = new HashSet<int>();
        private static List<Int32> _deadMobs = new List<int>();
        private static HashSet<Int32> _mobsToDebuff = new HashSet<int>();
        private static Int64 _printoutTimer = 0;


        public static void Init()
        {
            RegisterEvents();
            RegisterEpicAndAnguishBP();
            //E3._bots.SetupAliases();
        }

        public static void Process()
        {
            UseBurns();
            Check_AssistStatus();


            if(_printoutTimer < Core._stopWatch.ElapsedMilliseconds)
            {
                foreach (var kvp in _debuffdotTimers)
                {
                    foreach (var kvp2 in kvp.Value._timestamps)
                    {
                        Data.Spell spell;
                        if (Spell._loadedSpells.TryGetValue(kvp2.Key, out spell))
                        {
                            MQ.Write($"mobid:{kvp.Value._mobID} spellid:{spell.CastName} timeleft:{(kvp2.Value - Core._stopWatch.ElapsedMilliseconds) / 1000} seconds");

                        }
                        else
                        {
                            MQ.Write($"mobid:{kvp.Value._mobID} spellid:{kvp2.Key} timeleft:{(kvp2.Value - Core._stopWatch.ElapsedMilliseconds) / 1000} seconds");

                        }

                    }
                }
                _printoutTimer = Core._stopWatch.ElapsedMilliseconds + 10000;

            }

        }
        //this can be invoked via advanced settings loop
        [AdvSettingInvoke]
        public static void Check_Nukes()
        {

            if (_assistTargetID > 0)
            {
                //we should be assisting, check_AssistStatus, verifies its not a corpse.

                Spawn s;
                if (_spawns.TryByID(_assistTargetID, out s))
                {
                    bool giftOfManaSet = false;
                    bool giftOfMana = false;

                    foreach (var spell in E3._characterSettings.Nukes)
                    {
                        //check Ifs on the spell
                        if (!String.IsNullOrWhiteSpace(spell.Ifs))
                        {
                            if (!MQ.Query<bool>($"${{Bool[{spell.Ifs}]}}"))
                            {
                                //failed check, onto the next
                                continue;
                            }
                        }
                        //can't cast if it isn't ready
                        if (Casting.CheckReady(spell) && Casting.checkMana(spell))
                        {
                            //we should have a valid target via check_assistStatus
                            if (spell.Delay > 0 && _nukeDelayTimeStamp > 0 && Core._stopWatch.ElapsedMilliseconds < _nukeDelayTimeStamp)
                            {
                                //delay has been specified, skip this spell
                                continue;

                            }
                            //reset delay timestamp
                            if (spell.Delay > 0)
                            {
                                _nukeDelayTimeStamp = 0;
                            }


                            if (spell.GiftOfMana)
                            {
                                //can only cast if we have gift of mana. do we have it?
                                if (!(giftOfManaSet))
                                {
                                    giftOfMana = (MQ.Query<bool>("${Me.Song[Gift of Mana].ID}") || MQ.Query<bool>("${Me.Song[Celestial Gift].ID}") || MQ.Query<bool>("${Me.Song[Celestial Boon].ID}"));
                                    giftOfManaSet = true;
                                }
                                if (!giftOfMana)
                                {
                                    continue;
                                }

                            }
                            //aggro checks
                            if (spell.NoAggro)
                            {
                                if (MQ.Query<bool>("${Me.TargetOfTarget.CleanName.Equal[${Me.CleanName}]}"))
                                {
                                    continue;
                                }
                            }
                            if (spell.PctAggro>0 && MQ.Query<Int32>("${Me.PctAggro}") > spell.PctAggro)
                            {
                                continue;
                            }
                            //end aggro checks

                            //check for buff
                            if (!String.IsNullOrWhiteSpace(spell.CastIF))
                            {
                                if (!MQ.Query<bool>($"${{Bool[${{Target.Buff[{spell.CastIF}]}}]}}"))
                                {
                                    //doesn't have the buff we want
                                    continue;
                                }
                            }

                            if (s.Distance < spell.MyRange)
                            {
                                 
                                CastReturn result = Casting.Cast(_assistTargetID, spell,Heals.SomeoneNeedsHealing);
                                if(result== CastReturn.CAST_INTERRUPTFORHEAL)
                                {
                                    return;
                                }
                                if (result == CastReturn.CAST_SUCCESS)
                                {
                                    //if the spell is a delay time, lets make sure all other delay types are blocked for the
                                    //delay time
                                    if (spell.Delay > 0)
                                    {
                                        _nukeDelayTimeStamp = Core._stopWatch.ElapsedMilliseconds + (spell.Delay * 1000);
                                    }
                                    return;
                                }

                            }
                            
                        }
                    }
                }
            }
        }
        [AdvSettingInvoke]
        public static void Check_Debuffs()
        {

           

            if (_assistTargetID > 0)
            {
                CastLongTermSpell(_assistTargetID, E3._characterSettings.Debuffs_OnAssist);
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
        }
        [AdvSettingInvoke]
        public static void check_Dots()
        {

            if(_assistTargetID>0)
            {
                CastLongTermSpell(_assistTargetID, E3._characterSettings.Dots_Assist);
                if (E3._actionTaken) return;
            }
            
            foreach (var mobid in _mobsToDot)
            {  
                CastLongTermSpell(mobid, E3._characterSettings.Dots_OnCommand);
                if (E3._actionTaken) return;
            }
            foreach(var mobid in _deadMobs)
            {
                _mobsToDot.Remove(mobid);
                _mobsToDebuff.Remove(mobid);
            }
            if (_deadMobs.Count > 0) _deadMobs.Clear();
        }
        private static void CastLongTermSpell(Int32 mobid, List<Data.Spell> spells)
        {
           
            foreach(var spell in spells)
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
                        if (counters > 3)
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
                    MQ.Delay(500, "${Target.BuffsPopulated}");
                    //check if the if condition works
                    if (!String.IsNullOrWhiteSpace(spell.Ifs))
                    {
                        if (!MQ.Query<bool>($"${{Bool[{spell.Ifs}]}}"))
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
                            if(buffDuration<1000)
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
                    ////Okay lesson about EQ resist messages and timers for debuffs
                    //// you don't know if a spell resits unless the server tells you.. so this result above? somewhat unreliable.
                    //// The reason for this is, it takes X amount of time to come back from the server , and that X is unreliable as heck. 
                    //// So... for debuffs we are going to do this. If the target you have has the buff, grab its timer from the buffs object
                    //// for total time as as the Duration TLO can be unreliable depending on dot focus duration.  as in it says 72  sec when its 92 sec.
                    Casting.TrueTarget(mobid);

                    MQ.Delay(500, "${Target.BuffsPopulated}");
                    //// we also have the situation where over 55> buffs on the ROF2 client cannot be viewed, but up to 85 or so work. 
                    //// we are going to have to loop through the buffs and set dot timers
                    //// if under 55< we will evict off the timer that we think we should have if we do
                    //// if over 55> we will update but not evict... best we can do. so if a dot goes over the 55 buff cap
                    //// but we get an invalid resist message... well... the client is going to assume it landed and set a timer for it. 
                    //// Most of the time this won't happen, but sometimes.. well.. ya. not much I can do.

                    //delay to release back to MQ to get a proper buffcount
                    MQ.Delay(100);
                    Int32 buffCount = MQ.Query<Int32>("${Target.BuffCount}");
                    MQ.Write($"Debuff/Dot Debuff Count:"+buffCount);
                    //lets just update our cache with what is on the mob.
                    Int64 timeLeftInMS = Casting.TimeLeftOnMySpell(spell);
                    MQ.Write($"Debuff/Dot Time Left on spell:" + timeLeftInMS);


                    if (buffCount<55)
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
                            if(result == CastReturn.CAST_RESIST)
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

                MQ.Write($"Debuff/Dot setting timestamp wait time:{timeLeftInMS}");
                s._timestamps[spell.SpellID] = Core._stopWatch.ElapsedMilliseconds + timeLeftInMS;


            }
            else
            {
                SpellTimer ts = SpellTimer.Aquire();
                ts._mobID = mobid;

                MQ.Write($"Debuff/Dot setting timestamp wait time:{timeLeftInMS}");

                ts._timestamps.Add(spell.SpellID, Core._stopWatch.ElapsedMilliseconds + timeLeftInMS);
                _debuffdotTimers.Add(mobid, ts);
            }
        }
        public static void Check_AssistStatus()
        {

            if (_assistTargetID == 0) return;

            Int32 targetId = MQ.Query<Int32>("${Target.ID}");
            if (targetId != _assistTargetID)
            {
                //different target? most likely manual control kicked in, update our assist target.
                _assistTargetID = targetId;

            }

            Spawn s;
            if (_spawns.TryByID(_assistTargetID, out s))
            {

                if (s.TypeDesc == "Corpse")
                {
                    //its dead jim
                    AssistOff();
                    return;
                }

                if (MQ.Query<bool>("${Me.Feigning}") && (E3._currentClass & Data.Class.FeignDeathClass) != E3._currentClass)
                {
                    MQ.Cmd("/stand");
                    return;
                }

                //if range/melee
                if (_rangeTypes.Contains(E3._characterSettings.Assist_Type, StringComparer.OrdinalIgnoreCase) || _meleeTypes.Contains(E3._characterSettings.Assist_Type, StringComparer.OrdinalIgnoreCase))
                {
                    //if melee
                    if (_meleeTypes.Contains(E3._characterSettings.Assist_Type, StringComparer.OrdinalIgnoreCase))
                    {
                        //we are melee lets check for enrage
                        if (_assistIsEnraged && MQ.Query<bool>("${Me.Combat}"))
                        {
                            MQ.Cmd("/attack off");
                            return;
                        }

                        if (MQ.Query<bool>("${Me.AutoFire}"))
                        {
                            //turn off autofire
                            MQ.Cmd("/autofire");
                            //delay is needed to give time for it to actually process
                            MQ.Delay(1000);
                        }

                        if (!MQ.Query<bool>("${Me.Combat}"))
                        {
                            MQ.Cmd("/attack on");
                        }

                        //are we sticking?
                        if (!MQ.Query<bool>("${Stick.Active}"))
                        {
                            StickToAssistTarget();
                        }

                    }
                    else
                    {
                        //we be ranged!
                        MQ.Cmd($"/squelch /face fast id {_assistTargetID}");

                        if (MQ.Query<Decimal>("${Target.Distance}") > 200)
                        {
                            MQ.Cmd("/squelch /stick moveback 195");
                        }

                        if (!MQ.Query<bool>("${Me.AutoFire}"))
                        {
                            //delay is needed to give time for it to actually process
                            MQ.Delay(1000);
                            //turn on autofire
                            MQ.Cmd("/autofire");
                        }
                    }
                    //call combat abilites
                    CombatAbilties();

                }

             

            }
            else if (_assistTargetID > 0)
            {
                //can't find the mob, yet we have an assistID? remove assist.
                AssistOff();
                return;
            }

            UseBurns();
           
        }
        public static void UseBurns()
        {
            UseBurn(_epicWeapon, use_EPICBurns);
            UseBurn(_anguishBP, use_EPICBurns);
            UseBurn(E3._characterSettings.QuickBurns, use_QUICKBurns);
            UseBurn(E3._characterSettings.FullBurns, use_FULLBurns);
            UseBurn(E3._characterSettings.LongBurns, use_LONGBurns);

        }
        private static void UseBurn(List<Data.Spell> burnList, bool use)
        {
            if(use)
            {
                foreach (var burn in burnList)
                {
                    if(Casting.CheckReady(burn))
                    {
                        if(burn.CastType== Data.CastType.Disc)
                        {
                            if(burn.TargetType=="Self")
                            {
                                if(MQ.Query<bool>("${Me.ActiveDisc.ID}"))
                                {
                                    continue;

                                }
                            }
                        }
                        Casting.Cast(0,burn);
                    }
                }
            }
        }
        public static void CombatAbilties()
        {
            //can we find our target?
            Spawn s;
            if (_spawns.TryByID(_assistTargetID, out s))
            {
                //yes we can, lets grab our current agro
                Int32 pctAggro = MQ.Query<Int32>("${Me.PctAggro}");
                // just use smarttaunt instead of old taunt logic
                if (E3._characterSettings.Assist_SmartTaunt || E3._characterSettings.Assist_TauntEnabled)
                {
                    if (pctAggro < 100)
                    {
                        Int32 targetOfTargetID = MQ.Query<Int32>("${Me.TargetOfTarget}");
                        if (targetOfTargetID > 0)
                        {
                            Spawn tt;
                            if (_spawns.TryByID(targetOfTargetID, out tt))
                            {
                                //if not a tank on target of target, taunt it!
                                if (!_tankTypes.Contains(tt.ClassShortName))
                                {
                                    if (MQ.Query<bool>("${Me.AbilityReady[Taunt]}"))
                                    {
                                        MQ.Cmd("/doability Taunt");

                                        MQ.Broadcast($"Taunting {s.CleanName}: {tt.ClassShortName} - {tt.CleanName} has agro and not a tank");

                                    }
                                    else if (MQ.Query<bool>("${Me.AltAbilityReady[Divine Stun]}"))
                                    {
                                        if (Casting.CheckReady(_divineStun))
                                        {
                                            Casting.Cast(_assistTargetID, _divineStun);
                                        }

                                    }
                                    else if (MQ.Query<bool>("${Me.SpellReady[Terror of Discord]}"))
                                    {
                                        if (Casting.CheckReady(_terrorOfDiscord))
                                        {
                                            Casting.Cast(_assistTargetID, _terrorOfDiscord);
                                        }

                                    }
                                }
                            }
                        }
                    }
                }
                //end smart taunt

                //rogue/bards are special
                if (E3._currentClass == Data.Class.Rogue && E3._characterSettings.Rogue_AutoEvade)
                {
                    Rogue.AutoEvade();
                }

                //lets do our abilities!
                foreach (var ability in E3._characterSettings.MeleeAbilities)
                {
                    //why even check, if its not ready?
                    if (Casting.CheckReady(ability))
                    {
                        if (!String.IsNullOrWhiteSpace(ability.CastIF))
                        {
                            if (!MQ.Query<bool>($"${{Bool[${{Target.Buff[{ability.CastIF}]}}]}}"))
                            {
                                //doesn't have the buff we want
                                continue;
                            }
                        }
                        if (!String.IsNullOrWhiteSpace(ability.CheckFor))
                        {
                            if (MQ.Query<bool>($"${{Bool[${{Target.Buff[{ability.CheckFor}]}}]}}"))
                            {
                                //has the buff already
                                continue;
                            }
                        }

                        if (pctAggro < ability.PctAggro)
                        {
                            continue;
                        }

                        if (ability.CastType == Data.CastType.Ability)
                        {

                            if (ability.CastName == "Bash")
                            {
                                //check if we can actually bash
                                if (!(MQ.Query<bool>("${Select[${Me.Inventory[Offhand].Type},Shield]}") || MQ.Query<bool>("${Me.AltAbility[2 Hand Bash]}")))
                                {
                                    continue;
                                }
                            }

                            Casting.Cast(_assistTargetID, ability);
                        }
                        else if (ability.CastType == Data.CastType.AA)
                        {
                            Casting.Cast(_assistTargetID, ability);
                        }
                        else if (ability.CastType == Data.CastType.Disc)
                        {

                            Int32 endurance = MQ.Query<Int32>("${Me.Endurance}");
                            Int32 enduranceCost = MQ.Query<Int32>($"${{Spell[{ability.CastName}].EnduranceCost}}");
                            Int32 minEndurnace = ability.MinEnd;
                            Int32 pctEndurance = MQ.Query<Int32>("${Me.PctEndurance}");

                            if (pctEndurance >= minEndurnace)
                            {
                                if (endurance > enduranceCost)
                                {
                                    if (String.IsNullOrWhiteSpace(ability.Ifs))
                                    {
                                        if (!MQ.Query<bool>($"${{Bool[{ability.Ifs}]}}"))
                                        {
                                            continue;
                                        }
                                    }
                                    if (MQ.Query<bool>("${Me.ActiveDisc.ID}"))
                                    {
                                        if (ability.TargetType == "Self")
                                        {
                                            Casting.Cast(0, ability);
                                        }
                                    }

                                }
                            }
                        }
                    }
                }
            }
        }
        public static void AssistOff()
        {


            if (MQ.Query<bool>("${Window[CastingWindow].Open}")) MQ.Cmd("/interrupt");
            if (MQ.Query<bool>("${Me.Combat}")) MQ.Cmd("/attack off");
            if (MQ.Query<bool>("${Me.AutoFire}")) MQ.Cmd("/autofire off");
            if (MQ.Query<Int32>("${Me.Pet.ID}") > 0) MQ.Cmd("/squelch /pet back off");
            MQ.Delay(500, "${Bool[!${Me.Combat} && !${Me.AutoFire}]}");
            _isAssisting = false;
            _assistTargetID = 0;
            if (MQ.Query<bool>("${Stick.Status.Equal[ON]}")) MQ.Cmd("/squelch /stick off");

            use_FULLBurns = false;
            use_QUICKBurns = false;
            use_EPICBurns = false;
            use_LONGBurns = false;
            use_Swarms = false;
          
            _offAssistIgnore.Clear();
            _mobsToDot.Clear();
            _mobsToDebuff.Clear();
            Casting.ResetResistCounters();
            //put them back in their object pools
            foreach (var kvp in _debuffdotTimers)
            {
                kvp.Value.Dispose();
            }
            _debuffdotTimers.Clear();

            //issue follow
            if (Basics._following)
            {
                Basics.AcquireFollow();
            }
        }
        public static void AssistOn(Int32 mobID)
        {
            //clear in case its not reset by other means
            //or you want to attack in enrage
            _assistIsEnraged = false;

            if (mobID == 0)
            {
                //something wrong with the assist, kickout
                MQ.Broadcast("Cannot assist, improper mobid");
                return;
            }
            Spawn s;
            if (_spawns.TryByID(mobID, out s))
            {
               

                if (s.TypeDesc == "Corpse")
                {
                    MQ.Broadcast("Cannot assist, a corpse");
                    return;
                }
                if (!(s.TypeDesc == "NPC" || s.TypeDesc == "Pet"))
                {
                    MQ.Broadcast("Cannot assist, not a NPC or Pet");
                    return;
                }

                if (s.Distance3D > E3._generalSettings.Assists_MaxEngagedDistance)
                {
                    MQ.Broadcast($"{s.CleanName} is too far away.");
                    return;
                }

                if (MQ.Query<bool>("${Me.Feigning}"))
                {
                    MQ.Cmd("/stand");
                }


                

                Spawn folTarget;

                if (_spawns.TryByID(Basics._followTargetID, out folTarget))
                {
                    if (Basics._following && folTarget.Distance3D > 100 && MQ.Query<bool>("${Me.Moving}"))
                    {
                        //using a delay in awhile loop, use query for realtime info
                        while (MQ.Query<bool>("${Me.Moving}") && MQ.Query<Decimal>($"${{Spawn[id {Basics._followTargetID}].Distance3D}}") > 100)
                        {
                            MQ.Delay(100);
                            //wait us to get close to our follow target and then we can engage
                        }
                    }
                }

                if (MQ.Query<bool>("${Stick.Active}")) MQ.Cmd("/squelch /stick off");
                if (MQ.Query<bool>("${AdvPath.Following}")) MQ.Cmd("/squelch /afollow off ");

                _isAssisting = true;
                _assistTargetID = mobID;
                if (MQ.Query<Int32>("${Target.ID}") != _assistTargetID)
                {
                    if (!Casting.TrueTarget(_assistTargetID))
                    {
                        //could not target
                        MQ.Broadcast("\arCannot assist, Could not target");
                        return;
                    }
                }

                MQ.Cmd($"/face fast id {_assistTargetID}");

                if (MQ.Query<Int32>("${Me.Pet.ID}") > 0)
                {
                    MQ.Cmd($"/pet attack {_assistTargetID}");
                }

                //IF MELEE/Ranged
                if (_meleeTypes.Contains(E3._characterSettings.Assist_Type, StringComparer.OrdinalIgnoreCase))
                {
                    if (_assistDistanceTypes.Contains(E3._characterSettings.Assist_MeleeDistance, StringComparer.OrdinalIgnoreCase))
                    {
                        _assistDistance = (int)(s.MaxRangeTo * 0.75);
                    }
                    else
                    {
                        if (!Int32.TryParse(E3._characterSettings.Assist_MeleeDistance, out _assistDistance))
                        {
                            _assistDistance = (int)(s.MaxRangeTo * 0.75);
                        }
                    }
                    //make sure its not too out of bounds
                    if (_assistDistance > 25 || _assistDistance < 1)
                    {
                        _assistTargetID = 25;
                    }
                    StickToAssistTarget();

                    if (E3._currentClass == Data.Class.Rogue)
                    {
                        Rogue.RogueStrike();

                    }
                    MQ.Cmd("/attack on");

                }
                else if (_rangeTypes.Contains(E3._characterSettings.Assist_Type, StringComparer.OrdinalIgnoreCase))
                {
                    if (!MQ.Query<bool>("${Me.AutoFire}"))
                    {
                        MQ.Delay(1000);
                        MQ.Cmd("/autofire");
                    }

                    if (E3._characterSettings.Assist_Type.Equals("Ranged"))
                    {
                        if (E3._characterSettings.Assist_RangeDistance.Equals("Clamped"))
                        {   //so we don't calc multiple times
                            double distance = s.Distance;
                            if (distance >= 30 && distance <= 200)
                            {
                                MQ.Cmd($"/squelch /stick hold moveback {distance}");
                            }
                            else
                            {
                                if (distance > 200) MQ.Cmd("/squelch /stick hold moveback 195");
                                if (distance < 30) MQ.Cmd("/squelch /stick hold moveback 35");
                            }

                        }
                        else
                        {
                            MQ.Cmd($"/squelch /stick hold moveback {E3._characterSettings.Assist_RangeDistance}");
                        }
                    }
                }
            }
        }
        public static void DotsOn(Int32 mobid)
        {
            if(!_mobsToDot.Contains(mobid))
            {
                _mobsToDot.Add(mobid);
            }
        }
        public static void DebuffsOn(Int32 mobid)
        {
            if (!_mobsToDebuff.Contains(mobid))
            {
                _mobsToDebuff.Add(mobid);
            }
        }
        private static void StickToAssistTarget()
        {
            //needed a case insensitive switch, that was easy to read, thus this.
            string sp = E3._characterSettings.Assist_MeleeStickPoint;
            if (_stickSwitch == null)
            {
                var stw = new Dictionary<string, Action>(10, StringComparer.OrdinalIgnoreCase);
                stw.Add("behind", () =>
                {

                    MQ.Cmd("/stick snaproll uw");
                    MQ.Delay(200, $"${{Bool[${{Stick.Behind}} && ${{Stick.Stopped}}]}}");
                    MQ.Cmd($"/squelch /stick hold moveback behind {_assistDistance} uw");
                });
                stw.Add("front", () =>
                {

                    MQ.Cmd($"/stick hold front {_assistDistance} uw");
                    MQ.Delay(200, "${Stick.Stopped}");

                });
                stw.Add("behindonce", () =>
                {

                    MQ.Cmd("/stick snaproll uw");
                    MQ.Delay(200, $"${{Bool[${{Stick.Behind}} && ${{Stick.Stopped}}]}}");
                    MQ.Cmd($"/squelch /stick hold moveback behindonce {_assistDistance} uw");
                });
                stw.Add("pin", () =>
                {

                    MQ.Cmd("/stick snaproll uw");
                    MQ.Delay(200, $"${{Bool[${{Stick.Behind}} && ${{Stick.Stopped}}]}}");
                    MQ.Cmd($"/squelch /stick hold moveback pin {_assistDistance} uw");
                });
                stw.Add("!front", () =>
                {

                    MQ.Cmd("/stick snaproll uw");
                    MQ.Delay(200, $"${{Bool[${{Stick.Behind}} && ${{Stick.Stopped}}]}}");
                    MQ.Cmd($"/squelch /stick hold moveback !front {_assistDistance} uw");
                });
                _stickSwitch = stw;
            }
            Action action;
            if (_stickSwitch.TryGetValue(sp, out action))
            {
                action();
            }
            else
            {   //defaulting to behind
                _stickSwitch["behind"]();
            }

        }

        private static void RegisterEpicAndAnguishBP()
        {
            foreach (string name in _epicList)
            {
                if (MQ.Query<Int32>($"${{FindItemCount[={name}]}}") > 0)
                {
                    _epicWeaponName = name;
                }
            }

            foreach (string name in _anguishBPList)
            {
                if (MQ.Query<Int32>($"${{FindItemCount[={name}]}}") > 0)
                {
                    _anguishBPName = name;
                }
            }

            if(!String.IsNullOrWhiteSpace(_epicWeaponName))
            {
                _epicWeapon.Add(new Data.Spell(_epicWeaponName));
            }
            if (!String.IsNullOrWhiteSpace(_anguishBPName))
            {
                _anguishBP.Add(new Data.Spell(_anguishBPName));
            }

        }
        private static void RegisterEvents()
        {

            e3Utility.RegisterCommandWithTarget("/assistme", AssistOn);
            e3Utility.RegisterCommandWithTarget("/dotson", DotsOn);
            e3Utility.RegisterCommandWithTarget("/dot", DotsOn);
            e3Utility.RegisterCommandWithTarget("/debuffson", DebuffsOn);
            e3Utility.RegisterCommandWithTarget("/debuff", DebuffsOn);

            EventProcessor.RegisterCommand("/debuffsoff", (x) =>
            {
                _mobsToDebuff.Clear();
               
            });
            EventProcessor.RegisterCommand("/dotsoff", (x) =>
            {
                _mobsToDot.Clear();

            });

            EventProcessor.RegisterCommand("/backoff", (x) =>
            {
                //we are telling people to follow us
                E3._bots.BroadcastCommandToOthers($"/backoff");
                AssistOff();
            });
            EventProcessor.RegisterCommand("/backoff", (x) =>
            {
                //we are telling people to follow us
                E3._bots.BroadcastCommandToOthers($"/backoff");
                AssistOff();
            });
            EventProcessor.RegisterCommand("/swarmpets", (x) =>
            {
                ProcessBurnRequest("/swarmpets", x, ref use_Swarms);
            });
            EventProcessor.RegisterCommand("/epicburns", (x) =>
            {
                ProcessBurnRequest("/epicburns", x, ref use_EPICBurns);
            });
            EventProcessor.RegisterCommand("/quickburns", (x) =>
            {
                ProcessBurnRequest("/quickburns", x, ref use_QUICKBurns);

            });
            EventProcessor.RegisterCommand("/fullburns", (x) =>
            {
                ProcessBurnRequest("/fullburns", x, ref use_FULLBurns);

            });
            EventProcessor.RegisterCommand("/longburns", (x) =>
            {
                ProcessBurnRequest("/longburns", x, ref use_LONGBurns);

            });

            e3Utility.RegisterCommandWithTarget("/e3offassistignore", (x)=> { _offAssistIgnore.Add(x); });

            EventProcessor.RegisterEvent("EnrageOn", "(.)+ has become ENRAGED.", (x) =>
            {
                if (x.match.Groups.Count > 1)
                {
                    string mobName = x.match.Groups[1].Value;
                    if (MQ.Query<Int32>("${Target.ID}") == MQ.Query<Int32>($"${{Spawn[{mobName}].ID}}"))
                    {
                        _assistIsEnraged = true;
                        if (MQ.Query<Int32>("${Me.Pet.ID}") > 0)
                        {
                            MQ.Cmd("/pet back off");
                        }
                    }
                }
            });
            EventProcessor.RegisterEvent("EnrageOff", "(.)+  is no longer enraged.", (x) =>
            {
                if (x.match.Groups.Count > 1)
                {
                    string mobName = x.match.Groups[1].Value;
                    if (MQ.Query<Int32>("${Target.ID}") == MQ.Query<Int32>($"${{Spawn[{mobName}].ID}}"))
                    {
                        _assistIsEnraged = false;
                        if (MQ.Query<Int32>("${Me.Pet.ID}") > 0)
                        {
                            MQ.Cmd("/pet attack");
                        }
                    }
                }
            });
            EventProcessor.RegisterEvent("GetCloser", "Your target is too far away, get closer!", (x) =>
            {
                if (_isAssisting)
                {
                    if (_assistStickDistance > 5)
                    {
                        _assistStickDistance -= 3;
                        if (MQ.Query<bool>("${Stick.Active}"))
                        {
                            //turn off autofire
                            StickToAssistTarget();
                        }
                    }
                }

            });

        }
        private static void ProcessBurnRequest(string command, EventProcessor.CommandMatch x, ref bool burnType)
        {
            Int32 mobid;
            if (x.args.Count > 0)
            {
                if (Int32.TryParse(x.args[1], out mobid))
                {
                    burnType = true;
                }
                else
                {
                    MQ.Broadcast($"\aNeed a valid target to {command}.");
                }
            }
            else
            {
                Int32 targetID = MQ.Query<Int32>("${Target.ID}");
                if (targetID > 0)
                {
                    //we are telling people to follow us
                    E3._bots.BroadcastCommandToOthers($"{command} {targetID}");
                    burnType = true;
                }
                else
                {
                    MQ.Write($"\aNeed a target to {command}");
                }
            }
        }
        private static List<string> _anguishBPList = new List<string>() {
            "Bladewhisper Chain Vest of Journeys",
            "Farseeker's Plate Chestguard of Harmony",
            "Wrathbringer's Chain Chestguard of the Vindicator",
            "Savagesoul Jerkin of the Wilds",
            "Glyphwielder's Tunic of the Summoner",
            "Whispering Tunic of Shadows",
            "Ritualchanter's Tunic of the Ancestors"};

        private static List<string> _epicList = new List<string>() {
            "Prismatic Dragon Blade",
            "Blade of Vesagran",
            "Raging Taelosian Alloy Axe",
            "Vengeful Taelosian Blood Axe",
            "Staff of Living Brambles",
            "Staff of Everliving Brambles",
            "Fistwraps of Celestial Discipline",
            "Transcended Fistwraps of Immortality",
            "Redemption",
            "Nightbane, Sword of the Valiant",
            "Heartwood Blade",
            "Heartwood Infused Bow",
            "Aurora, the Heartwood Blade",
            "Aurora, the Heartwood Bow",
            "Fatestealer",
            "Nightshade, Blade of Entropy",
            "Innoruuk's Voice",
            "Innoruuk's Dark Blessing",
            "Crafted Talisman of Fates",
            "Blessed Spiritstaff of the Heyokah",
            "Staff of Prismatic Power",
            "Staff of Phenomenal Power",
            "Soulwhisper",
            "Deathwhisper"};
    }
}
