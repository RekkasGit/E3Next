﻿using E3Core.Classes;
using E3Core.Data;
using E3Core.Settings;
using E3Core.Utility;
using MonoCore;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Xml.Linq;

namespace E3Core.Processors
{
    /// <summary>
    /// Contains all the logic to make your toons assist you.
    /// </summary>
    public static class Assist
    {
		[ExposedData("Assist", "AllowControl")]
		public static bool AllowControl = false;
		[ExposedData("Assist", "IsAssisting")]
        public static Boolean IsAssisting = false;
		[ExposedData("Assist", "AssistTargetID")]
		public static Int32 AssistTargetID = 0;

        public static long LastAssistEndedTimestamp = 0;

        private static Logging _log = E3.Log;
        private static IMQ MQ = E3.MQ;
        private static ISpawns _spawns = E3.Spawns;
		[ExposedData("Assist", "RangeTypes")]
		private static List<string> _rangeTypes = new List<string>() { "Ranged", "Autofire" };
		[ExposedData("Assist", "MeleeTypes")]
		private static List<string> _meleeTypes = new List<string>() { "Melee","AutoAttack" };
		[ExposedData("Assist", "AssistDistanceTypes")]
		private static List<string> _assistDistanceTypes = new List<string> { "MaxMelee", "Off" };
		[ExposedData("Assist", "AssistDistance")]
		public static Int32 _assistDistance = 0;
		[ExposedData("Assist", "AssistIsEnraged")]
		private static bool _assistIsEnraged = false;
        private static Dictionary<string, Action> _stickSwitch;
        private static HashSet<Int32> _offAssistIgnore = new HashSet<Int32>();
		//private static Data.Spell _divineStun = new Data.Spell("Divine Stun");
		//private static Data.Spell _terrorOfDiscord = new Data.Spell("Terror of Discord");
		[ExposedData("Assist", "TankTypes")]
		private static List<string> _tankTypes = new List<string>() { "WAR", "PAL", "SHD" };

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
            ProcessCombat();
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

                bool manualControl = e3util.IsManualControl();

                bool isCorpse = MQ.Query<bool>($"${{Spawn[id {AssistTargetID}].Type.Equal[Corpse]}}");
                if (isCorpse)
                {
                    AssistOff();
                    return;
                }
                //changing targets while auto assisting
                if (AssistTargetID != targetId && E3.GeneralSettings.Assists_AutoAssistEnabled && AllowControl && !e3util.TargetIsPCOrPCPet())
                {
                    AssistTargetID = targetId;
                    MQ.Cmd("/assistme");
                }
                //else if the user  has this window focused don't do anything if they have changed targets or what not.
                else if (manualControl && targetId >0) return;

                //deal with dead mob
                if (targetId < 1)
                {  
                    if (!Casting.TrueTarget(AssistTargetID))
                    {
                        AssistOff();
                        return;
                    }
                    //just sanity check code, will normally not get here
                    isCorpse = MQ.Query<bool>("${Target.Type.Equal[Corpse]}");
                    if (isCorpse)
                    {
                        AssistOff();
                        return;
                    }
                }
                else if (targetId != AssistTargetID)
                {
                   //somehow we are not on the proper target and not in manual control and not the issuer of /assistme, put us back on target.
                    if(!AllowControl)
                    {
                        Casting.TrueTarget(AssistTargetID);
                    }
                }
            }
        }
        public static void ProcessCombat()
        {
            if (AssistTargetID == 0) return;
            Spawn s;
            Int32 targetId = MQ.Query<Int32>("${Target.ID}");
            bool manualControl = e3util.IsManualControl();

            if (targetId != AssistTargetID && manualControl) return;

            _spawns.RefreshList();
            if (_spawns.TryByID(AssistTargetID, out s))
            {
                if (MQ.Query<bool>("${Me.Feigning}"))
                {
					if (E3.CharacterSettings.IfFDStayDown) return;
					MQ.Cmd("/stand");
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
                        if (!AllowControl && !_assistIsEnraged)
                        {
                            if (!MQ.Query<bool>("${Me.Combat}"))
                            {
                                MQ.Cmd("/attack on");
                            }
                        }

                        if(!E3.CharacterSettings.Assist_Type.Equals("AutoAttack", StringComparison.OrdinalIgnoreCase))
                        {
							//are we sticking?
							if (!AllowControl && (!MQ.Query<bool>("${Stick.Active}") || MQ.Query<string>("${Stick.Status}") == "PAUSED"))
							{
								StickToAssistTarget();
							}
						}
                       

                    }
                    else
                    {
                        //we be ranged!
                        if (!AllowControl)
                        {
                            if(e3util.IsEQLive())
                            {
								MQ.Cmd($"/squelch fast id {AssistTargetID}",500);
							}
                            else
                            {
								MQ.Cmd($"/squelch /face fast id {AssistTargetID}");
							}
                           
                            if (MQ.Query<Decimal>("${Target.Distance}") > 200)
                            {
                                MQ.Cmd("/squelch /stick moveback 195");
                            }
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
        /// <summary>
        /// Uses combat abilities.
        /// </summary>
        public static void CombatAbilties()
        {
			if (MQ.Query<bool>("${Me.Feigning}"))
			{
				if (E3.CharacterSettings.IfFDStayDown) return;
				MQ.Cmd("/stand");
			}
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
                        Int32 targetOfTargetID = MQ.Query<Int32>("${Me.TargetOfTarget.ID}");
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
						bool shouldContinue = false;
						if (ability.CheckForCollection.Count > 0)
						{
							foreach (var checkforItem in ability.CheckForCollection.Keys)
							{
								if (MQ.Query<bool>($"${{Bool[${{Target.Buff[{checkforItem}]}}]}}"))
								{
									shouldContinue = true;
									break;
								}
							}
							if (shouldContinue) { continue; }
						}
					
                        if (pctAggro < ability.PctAggro)
                        {
                            continue;
                        }

                        if (ability.CastType == Data.CastingType.Ability)
                        {

                            if(String.Equals(ability.CastName,"Bash",StringComparison.OrdinalIgnoreCase))
                            {
                                //check if we can actually bash
                                if (MQ.Query<double>("${Target.Distance}") > 15 || !(MQ.Query<bool>("${Select[${Me.Inventory[Offhand].Type},Shield]}") || MQ.Query<bool>("${Me.AltAbility[2 Hand Bash]}")))
                                {
                                    continue;
                                }
                            }

                            if (String.Equals(ability.CastName, "Slam", StringComparison.OrdinalIgnoreCase))
                            {
                                //check if we can actually bash
                                if (MQ.Query<double>("${Target.Distance}") > 15)
                                {
                                    continue;
                                }
                            }
                            if (String.Equals(ability.CastName, "Kick", StringComparison.OrdinalIgnoreCase))
                            {
                                //check if we can actually kick
                                if (MQ.Query<double>("${Target.Distance}") > 15)
                                {
                                    continue;
                                }
                            }
                           
                            Casting.Cast(AssistTargetID, ability);
                        }
                        else if (ability.CastType == Data.CastingType.AA)
                        {

                            Casting.Cast(AssistTargetID, ability);
                        }
                        else if (ability.CastType == Data.CastingType.Disc)
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
                                        if (!MQ.Query<bool>("${Me.ActiveDisc.ID}"))
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
                        else if (ability.CastType == Data.CastingType.Item)
                        {
                            Casting.Cast(AssistTargetID, ability);
                        }
                    }
					if (E3.ActionTaken)
					{
						return;
					}
				}
            }
        }


        [ClassInvoke(Data.Class.All)]
        public static void CheckAutoAssist()
        {
            if (E3.GeneralSettings.Assists_AutoAssistEnabled)
            {
                if (AssistTargetID > 0) return;
                if (!MQ.Query<bool>("${Target.ID}")) return;
                if (!MQ.Query<bool>("${Me.Combat}")) return;
                Int32 mobid = MQ.Query<Int32>("${Target.ID}");
                if (_spawns.TryByID(mobid, out var spawn))
                {
                    if (spawn.Aggressive && spawn.TypeDesc != "Corpse")
                    {
                        Int32 targetHPPct = MQ.Query<Int32>("${Target.PctHPs}");
                        if (targetHPPct > 0 && targetHPPct <= E3.CharacterSettings.Assist_AutoAssistPercent)
                        {
                            MQ.Cmd("/assistme");
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
            LastAssistEndedTimestamp = Core.StopWatch.ElapsedMilliseconds;
            //add 1 seconds before we follow check again, to handle /cleartarget assist spam
            Movement._nextFollowCheck = Core.StopWatch.ElapsedMilliseconds + 1000;


		}

        /// <summary>
        /// Turns assist on.
        /// </summary>
        /// <param name="mobID">The mob identifier.</param>
        public static void AssistOn(Int32 mobID, Int32 zoneId)
        {

            if (zoneId != Zoning.CurrentZone.Id) return;
			
           
			//clear in case its not reset by other means
			//or you want to attack in enrage
			_assistIsEnraged = false;

            if (mobID == 0)
            {
                //something wrong with the assist, kickout
                E3.Bots.Broadcast("Cannot assist, improper MOB ID. Please get a valid target.");
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
				bool amIStanding = MQ.Query<bool>("${Me.Standing}");
				if (MQ.Query<bool>("${Me.Feigning}"))
                {
                    //if (E3.CharacterSettings.IfFDStayDown) return;
                    MQ.Cmd("/stand");
                }else
                {
					if (!amIStanding)
					{
						MQ.Cmd("/stand");
					}
				}

                Spawn folTarget;

                if (_spawns.TryByName(Movement.FollowTargetName, out folTarget))
                {
                    if (Movement.Following && folTarget.Distance3D > 100 && MQ.Query<bool>("${Me.Moving}"))
                    {
                        //using a delay in awhile loop, use query for realtime info
                        Int32 counter = 0;
                        while (MQ.Query<bool>("${Me.Moving}") && MQ.Query<Decimal>($"${{Spawn[{Movement.FollowTargetName}].Distance3D}}") > 100)
                        {
                            MQ.Delay(100);
                            counter++;
                            //if we have tried more than 3 seconds, stop and kick out.
                            if(counter>30)
                            {
                                E3.Bots.Broadcast("\arERROR:\ag Tried to move to target, took longer than 3 seconds, possibly not at the target. Turning off Assist");
                                AssistOff();
                                return;
                            }
                            //wait us to get close to our follow target and then we can engage
                        }
                    }
                }

                Movement.PauseMovement();


                IsAssisting = true;
                AssistTargetID = mobID;
                if (MQ.Query<Int32>("${Target.ID}") != AssistTargetID)
                {
                    MQ.Write("AssistOn Fix TargetID:" + AssistTargetID);
                    if (!Casting.TrueTarget(AssistTargetID))
                    {
                        //could not target
                        E3.Bots.Broadcast("\arCannot assist, Could not target");
                        return;
                    }
                }

                //rogues have discs that they need to be sneaking/invisiable for
				if (String.IsNullOrWhiteSpace(E3.CharacterSettings.Rogue_SneakAttack))
				{
					MQ.Cmd("/makemevisible");

				}
				

				if (!AllowControl)
                {
                    if(e3util.IsEQLive())
					{
                        //don't want to appear 'bot' like by always facing the mob
                        //stick for melee should keep them facing th emob
                        //as well as ranged has face commands but casters shouldn't care
                        if(!((E3.CurrentClass & Class.Caster) == E3.CurrentClass || (E3.CurrentClass & Class.Priest) == E3.CurrentClass)|| (E3.CharacterSettings.Assist_Type.Equals("AutoAttack", StringComparison.OrdinalIgnoreCase)))
                        {
							MQ.Cmd($"/face id {AssistTargetID}", 500);
						}

					}
                    else
                    {
						MQ.Cmd($"/face fast id {AssistTargetID}");
					}
                  
                }

                if (MQ.Query<Int32>("${Me.Pet.ID}") > 0)
                {
                    MQ.Cmd($"/pet attack {AssistTargetID}");
                     
                }
				if (e3util.IsEQLive())
				{
					MQ.Cmd("/pet swarm");
				}
				//IF MELEE/Ranged
				if (_meleeTypes.Contains(E3.CharacterSettings.Assist_Type, StringComparer.OrdinalIgnoreCase))
                {
                    if (_assistDistanceTypes.Contains(E3.CharacterSettings.Assist_MeleeDistance, StringComparer.OrdinalIgnoreCase))
                    {
                        _assistDistance = (int)(s.MaxRangeTo);
                    }
                    else
                    {
                        if (!Int32.TryParse(E3.CharacterSettings.Assist_MeleeDistance, out _assistDistance))
                        {
                            _assistDistance = (int)(s.MaxRangeTo);
                        }
                    }
                    //make sure its not too out of bounds
                    if (_assistDistance > 33)
                    {
                        _assistDistance = 33;
                    }
					if (_assistDistance < 1)
					{
						_assistDistance = 25;
					}
                    if (!E3.CharacterSettings.Assist_Type.Equals("AutoAttack", StringComparison.OrdinalIgnoreCase))
                    {
                        if (!AllowControl)
                        {
                            StickToAssistTarget();

                        }
                    }
                    if (E3.CurrentClass == Data.Class.Rogue && !String.IsNullOrWhiteSpace(E3.CharacterSettings.Rogue_SneakAttack))
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

                    if (!AllowControl && E3.CharacterSettings.Assist_Type.Equals("Ranged"))
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

           // E3.Bots.Broadcast("Setting assist range to: " + _assistDistance);
           
            if (_stickSwitch == null)
            {
                var stw = new Dictionary<string, Action>(10, StringComparer.OrdinalIgnoreCase);
                stw.Add("behind", () =>
                {
                    string delayedStrafeOption = " delaystrafe";
					if (!E3.CharacterSettings.Assist_DelayStrafeEnabled) delayedStrafeOption = String.Empty;
				    MQ.Cmd("/stick snaproll uw");
                    MQ.Delay(500);
                    MQ.Delay(2000, $"${{Bool[${{Stick.Behind}} && ${{Stick.Stopped}}]}}");
                    MQ.Cmd($"/squelch /stick hold moveback behind {_assistDistance} uw{delayedStrafeOption}");
                });
                stw.Add("front", () =>
                {

                    MQ.Cmd($"/stick hold front {_assistDistance} uw");
                    MQ.Delay(200, "${Stick.Stopped}");

                });
                stw.Add("behindonce", () =>
                {
					string delayedStrafeOption = " delaystrafe";
					if (!E3.CharacterSettings.Assist_DelayStrafeEnabled) delayedStrafeOption = String.Empty;

					MQ.Cmd("/stick snaproll uw");
                    MQ.Delay(500);
                    MQ.Delay(2000, $"${{Bool[${{Stick.Behind}} && ${{Stick.Stopped}}]}}");
                    MQ.Cmd($"/squelch /stick hold moveback behindonce {_assistDistance} uw{delayedStrafeOption}");
                });
                stw.Add("pin", () =>
                {
					string delayedStrafeOption = " delaystrafe";
                    if (!E3.CharacterSettings.Assist_DelayStrafeEnabled) delayedStrafeOption = String.Empty;

					MQ.Cmd("/stick snaproll uw");
                    MQ.Delay(500);
                    MQ.Delay(2000, $"${{Bool[${{Stick.Behind}} && ${{Stick.Stopped}}]}}");
                    MQ.Cmd($"/squelch /stick hold moveback pin {_assistDistance} uw{delayedStrafeOption}");
                });
                stw.Add("!front", () =>
                {
                    MQ.Cmd("/stick snaproll uw");
                    MQ.Delay(2000, $"${{Bool[${{Stick.Behind}} && ${{Stick.Stopped}}]}}");
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

               bool ignoreme = false;
               if(x.args.Contains("/ignoreme"))
               {
                   ignoreme = true;
                   x.args.Remove("/ignoreme");
               }

              
               //Rez.Reset();
               if (x.args.Count == 0)
               {

                   Int32 targetID = MQ.Query<Int32>("${Target.ID}");

                   if (targetID == E3.CurrentId)
                   {
                       E3.Bots.Broadcast("I cannot assist on myself.");
                       return;
                   }
                   if(!ignoreme)
                   {
                       if (!e3util.FilterMe(x))
                       {
                           if (targetID != AssistTargetID)
                           {
                               AssistOff();
                              
                           }
						   AllowControl = true;
						   AssistOn(targetID, Zoning.CurrentZone.Id);
					   }
                   }
                   else
                   {
                       //we are asking to ignore ourself, but might want to send out our pet still
                       if (MQ.Query<Int32>("${Me.Pet.ID}") > 0)
                       {
                           MQ.Cmd($"/pet attack {targetID}");
						  
					   }
					   if (e3util.IsEQLive())
					   {
						   MQ.Cmd("/pet swarm");
					   }
				   }
                   E3.Bots.BroadcastCommandToGroup($"/assistme {targetID} {Zoning.CurrentZone.Id}", x);


               }
               else if (!e3util.FilterMe(x))
               {
                   Int32 mobid;
                   Int32 zoneid;

                   if (Int32.TryParse(x.args[0], out mobid))
                   {
                        //make sure the target is in the same zone we are in
                       if (Int32.TryParse(x.args[1], out zoneid))
                       {
                           if (mobid != AssistTargetID)
						   {
							   AssistOff();
				           }
                           AllowControl = false;
						   if (e3util.IsEQLive())
						   {
							   //random delay so it isn't quite so ovious
                               if((E3.CurrentClass & Class.Priest)!=E3.CurrentClass)
                               {
                                   //if not a priest/healer, lets chill for 30-400ms
								  // MQ.Delay(E3.Random.Next(30, 400));

							   }

						   }
						   AssistOn(mobid, zoneid);

                       }
                   }
               }
           });

            EventProcessor.RegisterCommand("/cleartargets", (x) =>
            {
                ClearXTargets.FaceTarget = true;
                ClearXTargets.StickTarget = false;

                if (x.args.Count == 0)
                {
                    ClearXTargets.MobToAttack = 0;
                    AssistOff();
                    E3.Bots.BroadcastCommandToGroup($"/backoff all",x);
					ClearXTargets.Filters.Clear();
					if (x.filters.Count > 0)
                    {
                        ClearXTargets.Filters.Clear();
                        ClearXTargets.Filters.AddRange(x.filters);
                    }
                    ClearXTargets.HasAllFlag = x.hasAllFlag;
                    ClearXTargets.Enabled = true;
                    ClearXTargets.FaceTarget = true;
                    ClearXTargets.StickTarget = false;

                }
                else if (x.args.Count == 1 && x.args[0] == "off")
                {
                    AssistOff();
                    ClearXTargets.Enabled = false;
                    ClearXTargets.Filters.Clear();
                    ClearXTargets.HasAllFlag = false;
                    E3.Bots.BroadcastCommandToGroup($"/backoff all",x);
                }
                else if (x.args.Count >= 1)
                {
                    ClearXTargets.MobToAttack = 0;
                    AssistOff();
                    E3.Bots.BroadcastCommandToGroup($"/backoff all", x);
                    if (x.filters.Count > 0)
                    {
                        ClearXTargets.Filters.Clear();
                        ClearXTargets.Filters.AddRange(x.filters);
                    }
                    ClearXTargets.HasAllFlag = x.hasAllFlag;
                    foreach (var argValue in x.args)
                    {
                        if (argValue.Equals("noface", StringComparison.OrdinalIgnoreCase))
                        {
                            ClearXTargets.FaceTarget = false;
                        }
                        else if (argValue.Equals("stick", StringComparison.OrdinalIgnoreCase))
                        {
                            ClearXTargets.StickTarget = true;
                        }
                    }

                    ClearXTargets.Enabled = true;

                }

            });
            EventProcessor.RegisterCommand("/assisttype", (x) =>
            {


                if (x.args.Count > 1)
                {
                    string user = x.args[0];
                    string assisttype = x.args[1];
                    if (_meleeTypes.Contains(assisttype, StringComparer.OrdinalIgnoreCase) || _rangeTypes.Contains(assisttype, StringComparer.OrdinalIgnoreCase))
                    {
                        E3.Bots.BroadcastCommandToPerson(user, $"/assisttype {assisttype}");

                    }
                }
                else if (x.args.Count == 1)
                {
                    string assisttype = x.args[0];
                    if (_meleeTypes.Contains(assisttype, StringComparer.OrdinalIgnoreCase) || _rangeTypes.Contains(assisttype, StringComparer.OrdinalIgnoreCase))
                    {
                        E3.CharacterSettings.Assist_Type = assisttype;
                        E3.Bots.Broadcast("\agChanging assist type to :\ao" + assisttype);
                    }
                }

            });
            EventProcessor.RegisterCommand("/backoff", (x) =>
            {
                if (!e3util.FilterMe(x))
                {
                    if(E3.CurrentClass!=Class.Bard)
                    {
                        Casting.Interrupt();
                    }
                    AssistOff();
                    Burns.Reset();
                    DebuffDot.Reset();
                    Movement.AcquireFollow();

                }
                if (x.args.Count == 0)
                {     
                    //we are telling people to back off
                    E3.Bots.BroadcastCommandToGroup($"/backoff all", x);
                }

            });
            EventProcessor.RegisterCommand("/backoffme", (x) =>
            {
                    if (E3.CurrentClass != Class.Bard)
                    {
                        Casting.Interrupt();
                    }
                    AssistOff();
                    Burns.Reset();
                    DebuffDot.Reset();
                    Movement.AcquireFollow();

            });
            e3util.RegisterCommandWithTarget("/e3offassistignore", (x) => { _offAssistIgnore.Add(x); });
            EventProcessor.RegisterEvent("EnrageOn", "(.+) has become ENRAGED.", (x) =>
            {
                if (x.match.Groups.Count > 1)
                {
                    string mobName = x.match.Groups[1].Value;
                    if (MQ.Query<string>("${Target.CleanName}") == mobName)
                    {
                        if (E3.GeneralSettings.AttackOffOnEnrage || E3.CharacterSettings.Assist_BackOffOnEnrage)
                        {
                            E3.Bots.Broadcast("Enabling Assist Is Enraged");
                            _assistIsEnraged = true;
							
						}
                        if(E3.CharacterSettings.Assist_PetBackOffOnenrage)
                        {
							if (MQ.Query<Int32>("${Me.Pet.ID}") > 0)
							{
								MQ.Cmd("/pet back off");
							}

						}
					}
                }
            });
            EventProcessor.RegisterEvent("EnrageOff", "(.+) is no longer enraged.", (x) =>
            {
                if (x.match.Groups.Count > 1)
                {
                    string mobName = x.match.Groups[1].Value;
                    if (MQ.Query<string>("${Target.CleanName}") == mobName)
                    {
                        if (_assistIsEnraged)
                        {
                            E3.Bots.Broadcast("Disabling Assist Is Enraged");
                        }
                        _assistIsEnraged = false;
                        if (MQ.Query<Int32>("${Me.Pet.ID}") > 0)
                        {
                            MQ.Cmd("/pet attack");
						
						}
						if (e3util.IsEQLive())
						{
							MQ.Cmd("/pet swarm");
						}
					}
                }
            });
            EventProcessor.RegisterEvent("GetCloser", "Your target is too far away, get closer!", (x) =>
            {
                if (IsAssisting && !AllowControl && MQ.Query<string>("${Stick.Status}") != "PAUSED")
                {
                    if (_assistDistance > 5)
                    {
                        _assistDistance -= 3;
                        if (MQ.Query<bool>("${Stick.Active}"))
                        {
                            StickToAssistTarget();
                            //cleaar out any events that are stilll queued up.
                            if (EventProcessor.EventList.ContainsKey("GetCloser"))
                            {
                                var queuedEvents = EventProcessor.EventList["GetCloser"].queuedEvents;
                                while (queuedEvents.Count > 0)
                                {
                                    queuedEvents.TryDequeue(out var output);
                                }


                            }
                        }
                    }
                }

            });
            EventProcessor.RegisterEvent("CannotSee", "You cannot see your target.", (x) =>
            {
                if (IsAssisting && !AllowControl && MQ.Query<string>("${Stick.Status}") != "PAUSED")
                {
                    if (AssistTargetID > 0)
                    {

						if (e3util.IsEQLive())
						{
							MQ.Cmd($"/squelch /face id {AssistTargetID}",500);

						}
						else
						{
							MQ.Cmd($"/squelch /face fast id {AssistTargetID}");
						}
						

                    }

                    if (MQ.Query<bool>("${Stick.Active}"))
                    {
                        StickToAssistTarget();
                        //cleaar out any events that are stilll queued up.
                        if (EventProcessor.EventList.ContainsKey("CannotSee"))
                        {
                            var queuedEvents = EventProcessor.EventList["CannotSee"].queuedEvents;
                            while (queuedEvents.Count > 0)
                            {
                                queuedEvents.TryDequeue(out var output);
                            }


                        }
                    }
                }

            });
        }



    }
}
