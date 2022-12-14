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
    /// <summary>
    /// Contains all the logic to make your toons assist you.
    /// </summary>
    public static class Assist
    {
        public static bool AllowControl = false;
        public static Boolean IsAssisting = false;
        public static Int32 AssistTargetID = 0;

        private static Logging _log = E3.Log;
        private static IMQ MQ = E3.MQ;
        private static ISpawns _spawns = E3.Spawns;
        private static Int32 _assistStickDistance = 10;
        private static IList<string> _rangeTypes = new List<string>() { "Ranged", "Autofire" };
        private static IList<string> _meleeTypes = new List<string>() { "Melee" };
        private static IList<string> _assistDistanceTypes = new List<string> { "MaxMelee", "off" };
        private static Int32 _assistDistance = 0;
        private static bool _assistIsEnraged = false;
        private static Dictionary<string, Action> _stickSwitch;
        private static HashSet<Int32> _offAssistIgnore = new HashSet<Int32>();
        private static Data.Spell _divineStun = new Data.Spell("Divine Stun");
        private static Data.Spell _terrorOfDiscord = new Data.Spell("Terror of Discord");
        private static IList<string> _tankTypes = new List<string>() { "WAR", "PAL", "SK" };

        /// <summary>
        /// Initializes this instance.
        /// </summary>
        [SubSystemInit]
        public static void Init()
        {
            RegisterEvents();
           
        }

        /// <summary>
        /// Processes this instance.
        /// </summary>
        public static void Process()
        {           
            CheckAssistStatus();            
        }

        /// <summary>
        /// Resets this instance.
        /// </summary>
        public static void Reset()
        {
            _offAssistIgnore.Clear();
            Casting.ResetResistCounters();
            //put them back in their object pools
            DebuffDot.Reset();
            Burns.Reset();
            AssistOff();

        }

        /// <summary>
        /// Checks the assist status.
        /// </summary>
        public static void CheckAssistStatus()
        {
            
           // if (!e3util.ShouldCheck(ref _nextAssistCheck, _nextAssistCheckInterval)) return;
            
            using (_log.Trace())
            {
                if (AssistTargetID == 0) return;

                Int32 targetId = MQ.Query<Int32>("${Target.ID}");

                if (targetId <1)
                {
                    bool isCorpse = MQ.Query<bool>($"${{Spawn[id {AssistTargetID}].Type.Equal[Corpse]}}");
                    if (isCorpse)
                    {
                        AssistOff();
                        return;
                    }
                    if (!Casting.TrueTarget(AssistTargetID))
                    {
                        AssistOff();
                        return;
                    }

                    isCorpse = MQ.Query<bool>("${Target.Type.Equal[Corpse]}");
                    if(isCorpse)
                    {
                        AssistOff();
                        return;
                    }

                }
                else if (targetId != AssistTargetID)
                {

                    Spawn ct;
                    _spawns.RefreshList();
                    if (_spawns.TryByID(targetId, out ct))
                    {
                        if (AllowControl && targetId!=E3.CurrentId)
                        {
                            AssistTargetID = targetId;
                        }
                        else
                        {
                            Casting.TrueTarget(AssistTargetID);
                        }

                    }

                }

                Spawn s;
                if (_spawns.TryByID(AssistTargetID, out s))
                {
                    bool isCorpse = MQ.Query<bool>($"${{Spawn[id {AssistTargetID}].Type.Equal[Corpse]}}");
                    if (isCorpse)
                    {
                        //its dead jim
                        AssistOff();
                        return;
                    }

                    if (MQ.Query<bool>("${Me.Feigning}") && (E3.CurrentClass & Data.Class.FeignDeathClass) != E3.CurrentClass)
                    {
                        MQ.Cmd("/stand");
                        return;
                    }

                    //if range/melee
                    if (_rangeTypes.Contains(E3.CharacterSettings.Assist_Type, StringComparer.OrdinalIgnoreCase) || _meleeTypes.Contains(E3.CharacterSettings.Assist_Type, StringComparer.OrdinalIgnoreCase))
                    {
                        //if melee
                        if (_meleeTypes.Contains(E3.CharacterSettings.Assist_Type, StringComparer.OrdinalIgnoreCase))
                        {
                            //we are melee lets check for enrage
                            if (_assistIsEnraged && MQ.Query<bool>("${Me.Combat}") && !MQ.Query<bool>("${Stick.Behind}"))
                            {
                                MQ.Cmd("/attack off");
                                return;
                            }

                            if (MQ.Query<bool>("${Me.AutoFire}"))
                            {
                                MQ.Delay(1000);
                                //turn off autofire
                                MQ.Cmd("/autofire");
                                //delay is needed to give time for it to actually process
                                MQ.Delay(1000);
                            }
                            if (!AllowControl)
                            {
                                if (!MQ.Query<bool>("${Me.Combat}"))
                                {
                                    MQ.Cmd("/attack on");
                                }
                            }


                            //are we sticking?
                            if (!AllowControl && !MQ.Query<bool>("${Stick.Active}"))
                            {
                                StickToAssistTarget();
                            }

                        }
                        else
                        {
                            //we be ranged!
                            MQ.Cmd($"/squelch /face fast id {AssistTargetID}");

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
                                //delay is needed to give time for it to actually process
                                MQ.Delay(1000);
                            }
                        }
                        //call combat abilites
                        CombatAbilties();

                    }



                }
                else if (AssistTargetID > 0)
                {
                    //can't find the mob, yet we have an assistID? remove assist.
                    AssistOff();
                    return;
                }
            }
        }

        /// <summary>
        /// Uses combat abilities.
        /// </summary>
        public static void CombatAbilties()
        {
            //can we find our target?
            Spawn s;
            if (_spawns.TryByID(AssistTargetID, out s))
            {
                //yes we can, lets grab our current agro
                Int32 pctAggro = MQ.Query<Int32>("${Me.PctAggro}");
                // just use smarttaunt instead of old taunt logic
                if (E3.CharacterSettings.Assist_SmartTaunt || E3.CharacterSettings.Assist_TauntEnabled)
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

                                        E3.Bots.Broadcast($"Taunting {s.CleanName}: {tt.ClassShortName} - {tt.CleanName} has agro and not a tank");

                                    }
                                    else if (MQ.Query<bool>("${Me.AltAbilityReady[Divine Stun]}"))
                                    {
                                        if (Casting.CheckReady(_divineStun))
                                        {
                                            Casting.Cast(AssistTargetID, _divineStun);
                                        }

                                    }
                                    else if (MQ.Query<bool>("${Me.SpellReady[Terror of Discord]}"))
                                    {
                                        if (Casting.CheckReady(_terrorOfDiscord))
                                        {
                                            Casting.Cast(AssistTargetID, _terrorOfDiscord);
                                        }

                                    }
                                }
                            }
                        }
                    }
                }
                //end smart taunt

                //rogue/bards are special
                if (E3.CurrentClass == Data.Class.Rogue && E3.CharacterSettings.Rogue_AutoEvade)
                {
                    Rogue.AutoEvade();
                }

                //lets do our abilities!
                foreach (var ability in E3.CharacterSettings.MeleeAbilities)
                {
                    //why even check, if its not ready?
                    if (Casting.CheckReady(ability))
                    {

                        if (!String.IsNullOrWhiteSpace(ability.Ifs))
                        {
                            if (!Casting.Ifs(ability))
                            {
                                continue;
                            }
                        }

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
                                if (MQ.Query<double>("${Target.Distance}")>14 || !(MQ.Query<bool>("${Select[${Me.Inventory[Offhand].Type},Shield]}") || MQ.Query<bool>("${Me.AltAbility[2 Hand Bash]}")))
                                {
                                    continue;
                                }
                            }
                            if (ability.CastName == "Kick")
                            {
                                //check if we can actually kick
                                if (MQ.Query<double>("${Target.Distance}") > 14)
                                {
                                    continue;
                                }
                            }

                            Casting.Cast(AssistTargetID, ability);
                        }
                        else if (ability.CastType == Data.CastType.AA)
                        {

                            Casting.Cast(AssistTargetID, ability);
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
                                    
                                    if (ability.TargetType == "Self")
                                    {
                                        if(!MQ.Query<bool>("${Me.ActiveDisc.ID}"))
                                        {
                                            Casting.Cast(0, ability);
                                        }
                                    }
                                    else
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

        /// <summary>
        /// Turns assist off.
        /// </summary>
        public static void AssistOff()
        {


            if (Casting.IsCasting()) MQ.Cmd("/interrupt");
            if (MQ.Query<bool>("${Me.Combat}")) MQ.Cmd("/attack off");
            if (MQ.Query<bool>("${Me.AutoFire}"))
            {
                MQ.Cmd("/autofire"); 
                MQ.Delay(1000);
            }
            if (MQ.Query<Int32>("${Me.Pet.ID}") > 0) MQ.Cmd("/squelch /pet back off");
            IsAssisting = false;
            AllowControl = false;
            AssistTargetID = 0;
            _assistIsEnraged = false;
            if (MQ.Query<bool>("${Stick.Status.Equal[ON]}")) MQ.Cmd("/squelch /stick off");
           

            if (!Basics.InCombat())
            {
                _offAssistIgnore.Clear();
                Casting.ResetResistCounters();
                //put them back in their object pools
                DebuffDot.Reset();
                Burns.Reset();
            }

        }

        /// <summary>
        /// Turns assist on.
        /// </summary>
        /// <param name="mobID">The mob identifier.</param>
        public static void AssistOn(Int32 mobID)
        {
           
            //clear in case its not reset by other means
            //or you want to attack in enrage
            _assistIsEnraged = false;
           
            if (mobID == 0)
            {
                //something wrong with the assist, kickout
                E3.Bots.Broadcast("Cannot assist, improper mobid");
                return;
            }
            Spawn s;
            if (_spawns.TryByID(mobID, out s))
            {
               

                if (s.TypeDesc == "Corpse")
                {
                    E3.Bots.Broadcast("Cannot assist, a corpse");
                    return;
                }
                if (!(s.TypeDesc == "NPC" || s.TypeDesc == "Pet"))
                {
                    E3.Bots.Broadcast("Cannot assist, not a NPC or Pet");
                    return;
                }

                if (s.Distance3D > E3.GeneralSettings.Assists_MaxEngagedDistance)
                {
                    E3.Bots.Broadcast($"{s.CleanName} is too far away.");
                    return;
                }

                if (MQ.Query<bool>("${Me.Feigning}"))
                {
                    MQ.Cmd("/stand");
                }

                Spawn folTarget;

                if (_spawns.TryByName(Movement.FollowTargetName, out folTarget))
                {
                    if (Movement.Following && folTarget.Distance3D > 100 && MQ.Query<bool>("${Me.Moving}"))
                    {
                        //using a delay in awhile loop, use query for realtime info
                        while (MQ.Query<bool>("${Me.Moving}") && MQ.Query<Decimal>($"${{Spawn[{Movement.FollowTargetName}].Distance3D}}") > 100)
                        {
                            MQ.Delay(100);
                            //wait us to get close to our follow target and then we can engage
                        }
                    }
                }

                if (MQ.Query<bool>("${Stick.Active}")) MQ.Cmd("/squelch /stick off");
                if (MQ.Query<bool>("${AdvPath.Following}")) MQ.Cmd("/squelch /afollow off ");
                if (Movement.Following) Movement.Following = false;


                IsAssisting = true;
                AssistTargetID = mobID;
                if (MQ.Query<Int32>("${Target.ID}") != AssistTargetID)
                {
                    if (!Casting.TrueTarget(AssistTargetID))
                    {
                        //could not target
                        E3.Bots.Broadcast("\arCannot assist, Could not target");
                        return;
                    }
                }

                if (!AllowControl)
                {
                    MQ.Cmd($"/face fast id {AssistTargetID}");
                }

                if (MQ.Query<Int32>("${Me.Pet.ID}") > 0)
                {
                    MQ.Cmd($"/pet attack {AssistTargetID}");
                }

                //IF MELEE/Ranged
                if (_meleeTypes.Contains(E3.CharacterSettings.Assist_Type, StringComparer.OrdinalIgnoreCase))
                {
                    if (_assistDistanceTypes.Contains(E3.CharacterSettings.Assist_MeleeDistance, StringComparer.OrdinalIgnoreCase))
                    {
                        _assistDistance = (int)(s.MaxRangeTo * 0.75);
                    }
                    else
                    {
                        if (!Int32.TryParse(E3.CharacterSettings.Assist_MeleeDistance, out _assistDistance))
                        {
                            _assistDistance = (int)(s.MaxRangeTo * 0.75);
                        }
                    }
                    //make sure its not too out of bounds
                    if (_assistDistance > 25 || _assistDistance < 1)
                    {
                        _assistDistance = 25;
                    }
                    if(!AllowControl)
                    {
                        StickToAssistTarget();

                    }

                    if (E3.CurrentClass == Data.Class.Rogue)
                    {
                        Rogue.RogueStrike();

                    }
                    MQ.Cmd("/attack on");

                }
                else if (_rangeTypes.Contains(E3.CharacterSettings.Assist_Type, StringComparer.OrdinalIgnoreCase))
                {
                    if (!MQ.Query<bool>("${Me.AutoFire}"))
                    {
                        MQ.Delay(1000);
                        MQ.Cmd("/autofire");
                        MQ.Delay(1000);
                    }

                    if (E3.CharacterSettings.Assist_Type.Equals("Ranged"))
                    {
                        if (E3.CharacterSettings.Assist_RangeDistance.Equals("Clamped"))
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
                            MQ.Cmd($"/squelch /stick hold moveback {E3.CharacterSettings.Assist_RangeDistance}");
                        }
                    }
                }
            }
        }
       
        private static void StickToAssistTarget()
        {
            //needed a case insensitive switch, that was easy to read, thus this.
            string sp = E3.CharacterSettings.Assist_MeleeStickPoint;
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

        
        private static void RegisterEvents()
        {
             EventProcessor.RegisterCommand("/assistme", (x) =>
            {
                //clear in case its not reset by other means
                //or you want to attack in enrage
                _assistIsEnraged = false;
                MQ.Cmd("/makemevisible");
                //Rez.Reset();
                if (x.args.Count == 0)
                {
                  
                    Int32 targetID = MQ.Query<Int32>("${Target.ID}");

                    if (targetID == E3.CurrentId)
                    {
                        E3.Bots.Broadcast("I cannot assist on myself.");
                        return;
                    }
                    if(targetID!=AssistTargetID)
                    {
                        AssistOff();
                        AllowControl = true;
                        AssistOn(targetID);

                    }
                    E3.Bots.BroadcastCommandToGroup($"/assistme {targetID}",x);
                }
                else if (!e3util.FilterMe(x))
                {
                    Int32 mobid;
                    if (Int32.TryParse(x.args[0], out mobid))
                    {
                        if (mobid == AssistTargetID) return;
                        AssistOff();
                        AllowControl = false;
                        AssistOn(mobid);
                    }
                }
            });
            EventProcessor.RegisterCommand("/cleartargets", (x) =>
            {
                if(x.args.Count==0)
                {
                    ClearXTargets.MobToAttack=0;
                    AssistOff();
                    E3.Bots.BroadcastCommandToGroup($"/backoff all");
                    ClearXTargets.Enabled = true;

                } 
                else if (x.args.Count == 1 && x.args[0] == "off")
                {
                    AssistOff();
                    ClearXTargets.Enabled = false;
                    E3.Bots.BroadcastCommandToGroup($"/backoff all");
                }

            });
            EventProcessor.RegisterCommand("/backoff", (x) =>
            {   
                if (!e3util.FilterMe(x))
                {
                    AssistOff();
                    Burns.Reset();
                    DebuffDot.Reset();

                }
                if (x.args.Count == 0)
                {     //we are telling people to back off
                    E3.Bots.BroadcastCommandToGroup($"/backoff all",x);
                }
            });
            e3util.RegisterCommandWithTarget("/e3offassistignore", (x)=> { _offAssistIgnore.Add(x); });
            EventProcessor.RegisterEvent("EnrageOn", "(.)+ has become ENRAGED.", (x) =>
            {
                if (x.match.Groups.Count > 1)
                {
                    string mobName = x.match.Groups[1].Value;
                    if (MQ.Query<Int32>("${Target.ID}") == MQ.Query<Int32>($"${{Spawn[{mobName}].ID}}"))
                    {
                        if (E3.GeneralSettings.AttackOffOnEnrage)
                        {
                            _assistIsEnraged = true;
                        }

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
                if (IsAssisting&& !AllowControl)
                {
                    if (_assistStickDistance > 5)
                    {
                        _assistStickDistance -= 3;
                        if (MQ.Query<bool>("${Stick.Active}"))
                        {
                            StickToAssistTarget();
                        }
                    }
                }

            });
        }
       

       
    }
}
