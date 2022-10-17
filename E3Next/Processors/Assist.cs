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

        public static Boolean _isAssisting = false;
        public static Int32 _assistTargetID = 0;
        public static Int32 _assistStickDistance = 10;
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
        private static bool _allowControl = false;

        private static Int64 _nextAssistCheck = 0;
        private static Int64 _nextAssistCheckInterval = 500;
        public static void Init()
        {
            RegisterEvents();
           
            //E3._bots.SetupAliases();
        }

        public static void Process()
        {
            Burns.UseBurns();
            Check_AssistStatus();
            
        }

        //this can be invoked via advanced settings loop
     
       
        public static void Check_AssistStatus()
        {
            if (!e3util.ShouldCheck(ref _nextAssistCheck, _nextAssistCheckInterval)) return;

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
                        if (!_allowControl && !MQ.Query<bool>("${Stick.Active}"))
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
                                if (MQ.Query<double>("${Target.Distance}")>14 || !(MQ.Query<bool>("${Select[${Me.Inventory[Offhand].Type},Shield]}") || MQ.Query<bool>("${Me.AltAbility[2 Hand Bash]}")))
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
                                        if (!MQ.Query<bool>($"${{If[{ability.Ifs},TRUE,FALSE]}}"))
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
            _allowControl = false;
            _assistTargetID = 0;
            if (MQ.Query<bool>("${Stick.Status.Equal[ON]}")) MQ.Cmd("/squelch /stick off");

          
          
            _offAssistIgnore.Clear();
       
            Casting.ResetResistCounters();
            //put them back in their object pools
            DebuffDot.Reset();
            Burns.Reset();

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
                        _assistDistance = 25;
                    }
                    if(!_allowControl)
                    {
                        StickToAssistTarget();

                    }

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

        
        private static void RegisterEvents()
        {
             EventProcessor.RegisterCommand("/assistme", (x) =>
            {
                if (x.args.Count == 0)
                {
                    Int32 targetID = MQ.Query<Int32>("${Target.ID}");
                    _allowControl = true;
                   
                    AssistOn(targetID);
                    E3._bots.BroadcastCommandToGroup($"/assistme {targetID}");
                }
                else
                {
                    Int32 mobid;
                    if (Int32.TryParse(x.args[0], out mobid))
                    {
                        _allowControl = false;
                        AssistOn(mobid);
                    }
                }
            });
            EventProcessor.RegisterCommand("/backoff", (x) =>
            {
                AssistOff();
                if (x.args.Count == 0)
                {     //we are telling people to back off
                    E3._bots.BroadcastCommandToGroup($"/backoff all");
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
                if (_isAssisting&& !_allowControl)
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
