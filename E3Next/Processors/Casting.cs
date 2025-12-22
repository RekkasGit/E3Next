using E3Core.Data;
using E3Core.Server;
using E3Core.Utility;
using MonoCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.ServiceModel.Dispatcher;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace E3Core.Processors
{
	public static class Casting
	{

		public static string _lastSuccesfulCast = String.Empty;
		public static Logging _log = E3.Log;
		private static IMQ MQ = E3.MQ;
		public static Dictionary<Int32, Int64> _gemRecastLockForMem = new Dictionary<int, long>();
		public static Dictionary<Int32, ResistCounter> ResistCounters = new Dictionary<Int32, ResistCounter>();
		public static Dictionary<Int32, Int32> _currentSpellGems = new Dictionary<int, int>();


		public static Int64 _currentSpellGemsLastRefresh = 0;
		private static ISpawns _spawns = E3.Spawns;
		private static Logging.LogLevels _previousLogLevel = Logging.LogLevels.Error;
		private static Int64 _lastSpellCastTimeStamp = 0;

		public static CastReturn Cast(int targetID, Data.Spell spell, Func<Spell, Int32, Int32, bool> interruptCheck = null, bool isNowCast = false, bool isEmergency = false)
		{
			if (e3util.IsActionBlockingWindowOpen())
			{
				return CastReturn.CAST_BLOCKINGWINDOWOPEN;
			}
			//this isn't a nowcast but we have one ready to be processed, kick out
			if (!isNowCast && !isEmergency && NowCast.IsNowCastInQueue())
			{
				//we have a nowcast ready to be processed
				Interrupt();
				return CastReturn.CAST_INTERRUPTED;
			}

			bool navActive = false;
			bool navPaused = false;
			bool e3PausedNav = false;
			Int32 currentMana = 0;
			Int32 pctMana = 0;

			currentMana = MQ.Query<Int32>("${Me.CurrentMana}");
			pctMana = MQ.Query<Int32>("${Me.PctMana}");


			if (MQ.Query<bool>("${Cursor.ID}"))
			{
				e3util.ClearCursor();
			}
			if (spell.Debug)
			{
				_previousLogLevel = Logging.MinLogLevelTolog;
				Logging.MinLogLevelTolog = Logging.DefaultLogLevel;

			}
			try
			{
				if (spell.NoTarget)
				{
					targetID = 0;
				}

				if (targetID == 0)
				{
					//means don't change current target
					targetID = MQ.Query<Int32>("${Target.ID}");
					if (targetID < 1)
					{
						if (spell.SpellType == "Detrimental" && spell.TargetType == "Single")
						{
							return CastReturn.CAST_UNKNOWN;
						}

						targetID = E3.CurrentId;
					}
				}

				if (targetID < 1)
				{
					if (!(spell.TargetType == "Self" || spell.TargetType == "Group v1" || spell.TargetType == "Group v2" || spell.TargetType == "PB AE"))
					{
						MQ.Write($"Invalid targetId for Casting. {targetID}");
						E3.ActionTaken = true;
						return CastReturn.CAST_NOTARGET;
					}
				}

				//if this is a non bard, as we are not casting and its just an /alt activate, kick it off so it can queue up quickly. 
				if (E3.CurrentClass != Class.Bard && spell.CastType == CastingType.AA && spell.MyCastTime <= 500 && !IsCasting())
				{
					if (!(spell.TargetType == "Self" || spell.TargetType == "Group v1"))
					{
						TrueTarget(targetID);

					}
					String targetName = String.Empty;

					if (_spawns.TryByID(targetID, out var s))
					{

						//targets of 0 means keep current target
						if (targetID > 0)
						{
							targetName = s.CleanName;
						}
						else
						{
							targetName = MQ.Query<string>($"${{Spawn[id ${{Target.ID}}].CleanName}}");
						}
						MQ.Write($"\ag{spell.CastName} \am{targetName} \ao{targetID}");
					}
					BeforeEventCheck(spell);
					BeforeSpellCheck(spell, targetID);
					MQ.Cmd($"/alt activate {spell.CastID}");
					MQ.Delay(300); //necessary to keep things... in order
					AfterSpellCheck(spell, targetID);
					AfterEventCheck(spell);
					UpdateAAInCooldown(spell);
					E3.ActionTaken = true;
					spell.LastCastTimeStamp = Core.StopWatch.ElapsedMilliseconds;
					spell.LastAssistTimeStampForCast = Assist.LastAssistStartedTimeStamp;
					///allow the player to 'tweak' this value.
					if (E3.CharacterSettings.Misc_DelayAfterCastWindowDropsForSpellCompletion > 0)
					{
						MQ.Delay(E3.CharacterSettings.Misc_DelayAfterCastWindowDropsForSpellCompletion);
					}
					if (spell.AfterCastCompletedDelay > 0)
					{
						MQ.Delay(spell.AfterCastCompletedDelay);
					}
					
					return CastReturn.CAST_SUCCESS;
				}
				//bard can cast insta cast items while singing, they be special.
				else if (E3.CurrentClass == Class.Bard && spell.NoMidSongCast == false && spell.MyCastTime <= 500 && (spell.CastType == CastingType.Item || spell.CastType == CastingType.AA || spell.CastType == Data.CastingType.Ability))
				{
					//instant cast item, can cast while singing
					//note bards are special and cast do insta casts while doing normal singing. they have their own 
					//sing area, so only go here to do item/aa casts while singing. can't do IsCasting checks as it will catch
					//on the singing... so just kick out and assume all is well.
					if (_spawns.TryByID(targetID, out var s))
					{

						String targetName = String.Empty;
						//targets of 0 means keep current target
						if (targetID > 0)
						{
							targetName = s.CleanName;
						}
						else
						{
							targetName = MQ.Query<string>($"${{Spawn[id ${{Target.ID}}].CleanName}}");
						}
						TrueTarget(targetID);

						//this lets bard kick regardless of current song status, otherwise will wait until between songs to kick
						string abilityToCheck = spell.CastName;
						if (spell.CastType == Data.CastingType.Ability && abilityToCheck.Equals("Kick", StringComparison.OrdinalIgnoreCase))
						{
							BeforeEventCheck(spell);
							MQ.Write($"\ag{spell.CastName} \am{targetName} \ao{targetID}");
							MQ.Cmd($"/doability \"{spell.CastName}\"");
							spell.LastCastTimeStamp = Core.StopWatch.ElapsedMilliseconds;
							spell.LastAssistTimeStampForCast = Assist.LastAssistStartedTimeStamp;
							if (!E3.CharacterSettings.Misc_EnchancedRotationSpeed) MQ.Delay(300);
							if (spell.AfterCastCompletedDelay > 0)
							{
								MQ.Delay(spell.AfterCastCompletedDelay);
							}
							AfterEventCheck(spell);
							return CastReturn.CAST_SUCCESS;
						}
						MQ.Write($"\agBardCast {spell.CastName} \at{spell.SpellID} \am{targetName} \ao{targetID} \aw({spell.MyCastTime / 1000}sec)");
						if (spell.CastType == CastingType.AA)
						{
							BeforeEventCheck(spell);
							MQ.Cmd($"/alt activate {spell.CastID}");
							spell.LastCastTimeStamp = Core.StopWatch.ElapsedMilliseconds;
							spell.LastAssistTimeStampForCast = Assist.LastAssistStartedTimeStamp;
							if (!E3.CharacterSettings.Misc_EnchancedRotationSpeed) MQ.Delay(300);
							UpdateAAInCooldown(spell);
							if (spell.AfterCastCompletedDelay > 0)
							{
								MQ.Delay(spell.AfterCastCompletedDelay);
							}
							AfterEventCheck(spell);
							E3.ActionTaken = true;
							return CastReturn.CAST_SUCCESS;
						}
						if (spell.CastType == CastingType.Item)
						{
							BeforeEventCheck(spell);
							//else its an item
							MQ.Cmd($"/useitem \"{spell.CastName}\"", 300);
							spell.LastCastTimeStamp = Core.StopWatch.ElapsedMilliseconds;
							spell.LastAssistTimeStampForCast = Assist.LastAssistStartedTimeStamp;
							UpdateItemInCooldown(spell);
							if (spell.AfterCastCompletedDelay > 0)
							{
								MQ.Delay(spell.AfterCastCompletedDelay);
							}
							AfterEventCheck(spell);
							E3.ActionTaken = true;
							return CastReturn.CAST_SUCCESS;
						}
					}
					else
					{
						return CastReturn.CAST_NOTARGET;
					}
				}
				else if (E3.CurrentClass == Class.Bard && spell.CastType == CastingType.Spell)
				{
					Sing(targetID, spell);
					Int32 delay = (int)MQ.Query<int>("${Me.CastTimeLeft}") + Classes.Bard.BardLatency();
					MQ.Delay(delay);
					return CastReturn.CAST_SUCCESS;
				}
				else
				{
					//block on waiting for the spell window to close
					while (IsCasting())
					{
						MQ.Delay(50);

						//if (E3.IsPaused())
						//{
						//	Interrupt();
						//	return CastReturn.CAST_INTERRUPTED;
						//}

						if (!isEmergency && Heals.SomeoneNeedEmergencyHealing(currentMana, pctMana))
						{
							E3.Bots.Broadcast($"Interrupting [{spell.CastName}] for Emergency Heal.");
							Interrupt();
							E3.ActionTaken = true;
							//fire of emergency heal asap! checks targets in network and xtarget
							Heals.SomeoneNeedEmergencyHealing(currentMana, pctMana, true);
							return CastReturn.CAST_INTERRUPTFORHEAL;
						}
						if (!isEmergency && Heals.SomeoneNeedEmergencyHealingGroup(currentMana, pctMana))
						{

							E3.Bots.Broadcast($"Interrupting [{spell.CastName}] for Emergency Group Heal.");
							Interrupt();
							E3.ActionTaken = true;
							//fire of emergency heal asap!
							//checks group members
							Heals.SomeoneNeedEmergencyHealingGroup(currentMana, pctMana, true);
							return CastReturn.CAST_INTERRUPTFORHEAL;
						}

						if (!isNowCast && !isEmergency && NowCast.IsNowCastInQueue())
						{
							//we have a nowcast ready to be processed
							Interrupt();
							return CastReturn.CAST_INTERRUPTED;
						}
						if (EventProcessor.CommandListQueueHasCommand("/backoff"))
						{
							EventProcessor.ProcessEventsInQueues("/backoff");
							Interrupt();
							if (!IsCasting()) return CastReturn.CAST_INTERRUPTED;

						}
						if (EventProcessor.CommandListQueueHasCommand("/assistme"))
						{
							Int32 tAssistID = Assist.AssistTargetID;

							EventProcessor.ProcessEventsInQueues("/assistme");

							if (tAssistID > 0 && Assist.AssistTargetID > 0 && tAssistID != Assist.AssistTargetID)
							{
								Interrupt();
								if (!IsCasting()) return CastReturn.CAST_INTERRUPTED;
							}


						}
						if (EventProcessor.CommandListQueueHasCommand("/throne"))
						{
							EventProcessor.ProcessEventsInQueues("/throne");
							Interrupt();
							if (!IsCasting()) return CastReturn.CAST_INTERRUPTED;


						}
						if (EventProcessor.CommandListQueueHasCommand("/followme"))
						{
							EventProcessor.ProcessEventsInQueues("/followme");
							Interrupt();
							if (!IsCasting()) return CastReturn.CAST_INTERRUPTED;

						}

						//process any commands we need to process from the UI
						NetMQServer.SharedDataClient.ProcessCommands();
						PubClient.ProcessRequests();

					}
				}


				CastReturn returnValue = CastReturn.CAST_RESIST;

				//using (_log.Trace())
				{


					if (_spawns.TryByID(targetID, out var s))
					{

						String targetName = String.Empty;
						//targets of 0 means keep current target
						if (targetID > 0)
						{
							targetName = s.CleanName;
						}
						else
						{
							targetName = MQ.Query<string>($"${{Spawn[id ${{Target.ID}}].CleanName}}");
						}
						_log.Write($"TargetName:{targetName}");
						//why we should not cast.. for whatever reason.
						#region validation checks
						if (!isNowCast && MQ.Query<bool>("${Me.Invis}"))
						{

							E3.ActionTaken = true;

							_log.Write($"SkipCast-Invis ${spell.CastName} {targetName} : {targetID}");
							return CastReturn.CAST_INVIS;

						}

						if (!String.IsNullOrWhiteSpace(spell.Reagent))
						{

							_log.Write($"Checking for reagent required for spell cast:{targetName} value:{spell.Reagent}");
							//spell requires a regent, lets check if we have it.
							Int32 itemCount = MQ.Query<Int32>($"${{FindItemCount[={spell.Reagent}]}}");
							if (itemCount < 1)
							{
								spell.ReagentOutOfStock = true;
								_log.Write($"Cannot cast [{spell.CastName}], I do not have any [{spell.Reagent}], removing this spell from array. Restock for this spell to cast again.", Logging.LogLevels.Error);
								E3.Bots.Broadcast($"Cannot cast [{spell.CastName}], I do not have any [{spell.Reagent}], removing this spell from array. Restock for this spell to cast again.");
								e3util.Beep();
								return CastReturn.CAST_REAGENT;
							}
							else
							{
								_log.Write($"Reagent found!");

							}

						}

						_log.Write("Checking for zoning...");
						if (Zoning.CurrentZone.Id != MQ.Query<Int32>("${Zone.ID}"))
						{
							_log.Write("Currently zoning, delaying for 1second");
							//we are zoning, we need to chill for a bit.
							MQ.Delay(1000);
							return CastReturn.CAST_ZONING;
						}

						_log.Write("Checking for Feigning....");
						if (MQ.Query<bool>("${Me.Feigning}") && String.Compare(spell.CastName,"Mend",true)!=0)
						{
							E3.Bots.Broadcast($"skipping [{spell.CastName}] , i am feigned.");
							MQ.Delay(200);
							return CastReturn.CAST_FEIGN;
						}
						_log.Write("Checking for Open spell book....");
						if (MQ.Query<bool>("${Window[SpellBookWnd].Open}"))
						{
							if (!e3util.IsManualControl())
							{
								MQ.Cmd("/stand");
							}
							else
							{
								E3.ActionTaken = true;
								E3.Bots.Broadcast($"skipping [{spell.CastName}] , spellbook is open.");
								MQ.Delay(200);
								return CastReturn.CAST_SPELLBOOKOPEN;
							}

						}
						_log.Write("Checking for Open corpse....");
						if (MQ.Query<bool>("${Corpse.Open}"))
						{
							E3.ActionTaken = true;
							E3.Bots.Broadcast($"skipping [{spell.CastName}] , I have a corpse open.");
							MQ.Delay(200);
							return CastReturn.CAST_CORPSEOPEN;
						}
						_log.Write("Checking for LoS for non beneficial...");
						if (!spell.SpellType.Contains("Beneficial"))
						{
							_log.Write("Checking for LoS for non disc and not self...");
							if (!(spell.CastType.Equals("Disc") && spell.TargetType.Equals("Self")))
							{
								_log.Write("Checking for LoS for non PB AE and Self...");
								if (!(spell.TargetType.Equals("PB AE") || spell.TargetType.Equals("Self")))
								{
									_log.Write("Checking for LoS if target has LoS...");
									if (!MQ.Query<bool>($"${{Spawn[id {targetID}].LineOfSight}}"))
									{
										_log.Write($"I cannot see {targetName}");
										MQ.Write($"SkipCast-LoS {spell.CastName} ${spell.CastID} {targetName} {targetID}");
										return CastReturn.CAST_CANNOTSEE;

									}
								}
							}
						}
						#endregion
						//now to get the target
						_log.Write("Checking to see if we need to aquire a target for non self /pbaoe");
						if (spell.TargetType != "PB AE" && spell.TargetType != "Self")
						{
							if (Basics.InCombat() && targetID != Assist.AssistTargetID && MQ.Query<bool>("${Stick.Active}"))
							{
								MQ.Cmd("/stick pause");
							}
							if (!TrueTarget(targetID))
							{
								E3.Bots.Broadcast($"Spell Target failure for targetid:{targetID} for spell {spell.SpellName}");
								return CastReturn.CAST_NOTARGET;
							}
						}
						if (spell.SpellType.Equals("Detrimental") && (spell.TargetType != "PB AE" && spell.TargetType != "Self"))
						{
							TrueTarget(targetID);
							bool isCorpse = MQ.Query<bool>("${Target.Type.Equal[Corpse]}");

							if (isCorpse || !MQ.Query<bool>("${Target.ID}"))
							{
								//shouldn't nuke dead things
								Assist.AssistOff();
								Interrupt();
								return CastReturn.CAST_INTERRUPTED;
							}
						}

						BeforeEventCheck(spell);

						//remove item from cursor before casting
						_log.Write("Checking for item on cursor...");
						if (MQ.Query<bool>("${Cursor.ID}"))
						{
							MQ.Write($"Issuing auto inventory on {MQ.Query<string>("${Cursor}")} for spell: {spell.CastName}");
							e3util.ClearCursor();
						}

						BeforeSpellCheck(spell, targetID);

						//From here, we actually start casting the spell. 
						_log.Write("Checking for spell type to run logic...");
						if (spell.CastType == Data.CastingType.Disc)
						{
							_log.Write("Doing disc based logic checks...");
							if (MQ.Query<bool>("${Me.ActiveDisc.ID}") && spell.TargetType.Equals("Self"))
							{
								return CastReturn.CAST_ACTIVEDISC;

							}
							else
							{
								//activate disc!
								if (TrueTarget(targetID))
								{
									E3.ActionTaken = true;

									MQ.Write($"\ag{spell.CastName} \at{spell.SpellID} \am{targetName} \ao{targetID} \aw({spell.MyCastTime / 1000}sec)");
									MQ.Cmd($"/disc {spell.CastName}");
									UpdateDiscInCooldown(spell);
									if (spell.TargetType.Equals("Self"))
									{
										MQ.Delay(300);
									}
									returnValue = CastReturn.CAST_SUCCESS;
									goto startCasting;

								}
								else
								{
									returnValue = CastReturn.CAST_NOTARGET;
									return returnValue;
								}

							}

						}
						else if (spell.CastType == Data.CastingType.Ability)
						{

							string abilityToCheck = spell.CastName;

							if (abilityToCheck.Equals("Slam", StringComparison.OrdinalIgnoreCase))
							{
								abilityToCheck = "Bash";
							}

							if (!MQ.Query<bool>($"${{Me.AbilityReady[{abilityToCheck}]}}"))
							{
								return CastReturn.CAST_NOTREADY;

							}
							_log.Write("Doing Ability based logic checks...");
							//to deal with a slam bug
							if (spell.CastName.Equals("Slam", StringComparison.OrdinalIgnoreCase))
							{
								_log.Write("Doing Ability:Slam based logic checks...");
								if (MQ.Query<bool>("${Window[ActionsAbilitiesPage].Child[AAP_FirstAbilityButton].Text.Equal[Slam]}"))
								{
									MQ.Write($"\ag{spell.CastName} \am{targetName} \ao{targetID}");
									MQ.Cmd("/doability 1");
									UpdateAbilityInCooldown(spell);
								}
								else if (MQ.Query<bool>("${Window[ActionsAbilitiesPage].Child[AAP_SecondAbilityButton].Text.Equal[Slam]}"))
								{
									MQ.Write($"\ag{spell.CastName} \am{targetName} \ao{targetID}");
									MQ.Cmd("/doability 2");
									UpdateAbilityInCooldown(spell);
								}
								else if (MQ.Query<bool>("${Window[ActionsAbilitiesPage].Child[AAP_ThirdAbilityButton].Text.Equal[Slam]}"))
								{
									MQ.Write($"\ag{spell.CastName} \am{targetName} \ao{targetID}");
									MQ.Cmd("/doability 3");
									UpdateAbilityInCooldown(spell);
								}
								else if (MQ.Query<bool>("${Window[ActionsAbilitiesPage].Child[AAP_FourthAbilityButton].Text.Equal[Slam]}"))
								{
									MQ.Write($"\ag{spell.CastName} \am{targetName} \ao{targetID}");
									MQ.Cmd("/doability 4");
									UpdateAbilityInCooldown(spell);
								}
								else if (MQ.Query<bool>("${Window[ActionsAbilitiesPage].Child[AAP_FourthAbilityButton].Text.Equal[Slam]}"))
								{
									MQ.Write($"\ag{spell.CastName} \am{targetName} \ao{targetID}");
									MQ.Cmd("/doability 5");
									UpdateAbilityInCooldown(spell);
								}
								else if (MQ.Query<bool>("${Window[ActionsAbilitiesPage].Child[AAP_FifthAbilityButton].Text.Equal[Slam]}"))
								{
									MQ.Write($"\ag{spell.CastName} \am{targetName} \ao{targetID}");
									MQ.Cmd("/doability 5");
									UpdateAbilityInCooldown(spell);
								}
								else if (MQ.Query<bool>("${Window[ActionsAbilitiesPage].Child[AAP_SixthAbilityButton].Text.Equal[Slam]}"))
								{
									MQ.Write($"\ag{spell.CastName} \am{targetName} \ao{targetID}");
									MQ.Cmd("/doability 6");
									UpdateAbilityInCooldown(spell);
								}
								else
								{
									return CastReturn.CAST_INVALID;
								}
							}
							else
							{
								MQ.Write($"\ag{spell.CastName} \am{targetName} \ao{targetID}");
								MQ.Cmd($"/doability \"{spell.CastName}\"");
								UpdateAbilityInCooldown(spell);
							}

							MQ.Delay(300, $"${{Me.AbilityReady[{spell.CastName}]}}");


						}
						else
						{
							//Spell, AA, Items
							_log.Write("Doing Spell based logic checks...");


							if (spell.MyCastTime > 500)
							{

								if (MQ.Query<bool>("${AdvPath.Following}") && E3.Following) MQ.Cmd("/squelch /afollow off");
								if (MQ.Query<bool>("${MoveTo.Moving}") && E3.Following) MQ.Cmd("/moveto off");
								MQ.Cmd("/stick pause");
								navActive = MQ.Query<bool>("${Navigation.Active}");
								navPaused = MQ.Query<bool>("${Navigation.Paused}");
								e3PausedNav = false;
								if (navActive && !navPaused)
								{
									MQ.Cmd("/nav pause");
									e3PausedNav = true;
								}
								MQ.Delay(300, "${If[${Me.Moving},false,true]}");

							}

							_log.Write("Doing Spell:TargetType based logic checks...");
							if (spell.TargetType.Equals("Self") || spell.TargetType.Equals("PB AE"))
							{

								//clear our target if your trying to nuke yoruself
								if (spell.SpellType.Equals("Detrimental") && MQ.Query<Int32>("${Target.ID}") == E3.CurrentId)
								{
									TrueTarget(0, true);

								}

								if (spell.CastType == Data.CastingType.Spell)
								{
									PubServer.AddTopicMessage("${Casting}", $"{spell.CastName} on {targetName}");
									PubServer.AddTopicMessage("${Me.Casting}", spell.CastName);
									MQ.Write($"\ag{spell.CastName} \at{spell.SpellID} \am{targetName} \ao{targetID} \aw({spell.MyCastTime / 1000}sec)");
									MQ.Cmd($"/cast \"{spell.CastName}\"");
									if (!E3.CharacterSettings.Misc_EnchancedRotationSpeed) MQ.Delay(300);
									//give time for the casting bar to actulaly appear
									if (spell.MyCastTime > 500)
									{
										MQ.Delay(500);
									}

								}
								else
								{
									if (spell.CastType == CastingType.AA)
									{
										PubServer.AddTopicMessage("${Casting}", $"{spell.CastName} on {targetName}");
										PubServer.AddTopicMessage("${Me.Casting}", spell.CastName);
										MQ.Write($"\ag{spell.CastName} \at{spell.SpellID} \am{targetName} \ao{targetID} \aw({spell.MyCastTime / 1000}sec)");

										//MQ.Cmd($"/casting \"{spell.CastName}|alt\"");
										MQ.Cmd($"/alt activate {spell.AAID}");
										UpdateAAInCooldown(spell);

										if (!E3.CharacterSettings.Misc_EnchancedRotationSpeed) MQ.Delay(300);
										//give time for the casting bar to actulaly appear
										if (spell.MyCastTime > 500)
										{
											MQ.Delay(500);
										}
									}
									else
									{
										PubServer.AddTopicMessage("${Casting}", $"{spell.CastName} on {targetName}");
										PubServer.AddTopicMessage("${Me.Casting}", spell.CastName);
										MQ.Write($"\ag{spell.CastName} \at{spell.SpellID} \am{targetName} \ao{targetID} \aw({spell.MyCastTime / 1000}sec)");

										//else its an item
										//MQ.Cmd($"/casting \"{spell.CastName}|{spell.CastType.ToString()}\"");
										MQ.Cmd($"/useitem \"{spell.CastName}\"");
										UpdateItemInCooldown(spell);
										if (!E3.CharacterSettings.Misc_EnchancedRotationSpeed) MQ.Delay(300);
										//give time for the casting bar to actulaly appear
										if (spell.MyCastTime > 500)
										{
											MQ.Delay(500);
										}
									}
								}
							}
							else
							{
								if (spell.CastType == Data.CastingType.Spell)
								{
									PubServer.AddTopicMessage("${Casting}", $"{spell.CastName} on {targetName}");
									PubServer.AddTopicMessage("${Me.Casting}", spell.CastName);
									MQ.Write($"\ag{spell.CastName} \at{spell.SpellID} \am{targetName} \ao{targetID} \aw({spell.MyCastTime / 1000}sec)");
									//MQ.Cmd($"/casting \"{spell.CastName}|{spell.SpellGem}\" \"-targetid|{targetID}\"");
									MQ.Cmd($"/cast \"{spell.CastName}\"");
									if (!E3.CharacterSettings.Misc_EnchancedRotationSpeed) MQ.Delay(300);
									//give time for the casting bar to actulaly appear
									if (spell.MyCastTime > 500)
									{
										MQ.Delay(500);
									}
								}
								else
								{
									PubServer.AddTopicMessage("${Casting}", $"{spell.CastName} on {targetName}");
									PubServer.AddTopicMessage("${Me.Casting}", spell.CastName);
									MQ.Write($"\ag{spell.CastName} \at{spell.SpellID} \am{targetName} \ao{targetID} \aw({spell.MyCastTime / 1000}sec)");
									if (spell.CastType == CastingType.AA)
									{
										//MQ.Cmd($"/casting \"{spell.CastName}|alt\" \"-targetid|{targetID}\"");
										MQ.Cmd($"/alt activate {spell.AAID}");
										if (!E3.CharacterSettings.Misc_EnchancedRotationSpeed) MQ.Delay(300);
										UpdateAAInCooldown(spell);
										//give time for the casting bar to actulaly appear
										if (spell.MyCastTime > 500)
										{
											MQ.Delay(500);
										}
									}
									else
									{
										//else its an item
										PubServer.AddTopicMessage("${Me.Casting}", spell.CastName);
										//MQ.Cmd($"/casting \"{spell.CastName}|item\" \"-targetid|{targetID}\"");
										MQ.Cmd($"/useitem \"{spell.CastName}\"");
										UpdateItemInCooldown(spell);
										if (!E3.CharacterSettings.Misc_EnchancedRotationSpeed) MQ.Delay(300);
										//give time for the casting bar to actulaly appear
										if (spell.MyCastTime > 500)
										{
											MQ.Delay(500);
										}
									}
								}
							}
						}

					startCasting:
						spell.LastCastTimeStamp = Core.StopWatch.ElapsedMilliseconds;
						spell.LastAssistTimeStampForCast = Assist.LastAssistStartedTimeStamp;
						//in case a spell was interrupted before this one, clear anything out.
						ClearInterruptChecks();

						//needed for heal interrupt check

						currentMana = MQ.Query<Int32>("${Me.CurrentMana}");
						pctMana = MQ.Query<Int32>("${Me.PctMana}");

						if (spell.AfterCastDelay > 0) MQ.Delay(spell.AfterCastDelay);
						_log.Write("\ag Going into main While loop for spell casting bar...");
						while (IsCasting())
						{
							//_log.Write("\am In main cast window loop...");
							e3util.ProcessE3BCCommands();
							if(!isNowCast)
							{
								e3util.ProcessNowCastCommandsForOthers();
							}
							//means that we didn't fizzle and are now casting the spell

							//these are outside the no interrupt check
							if (!isEmergency && Heals.SomeoneNeedEmergencyHealing(currentMana, pctMana))
							{
								E3.Bots.Broadcast($"\arInterrupting \aw[\ag{spell.CastName}\aw] \agfor Emergency Heal.");
								Interrupt();
								E3.ActionTaken = true;
								//fire of emergency heal asap! checks targets in network and xtarget
								Heals.SomeoneNeedEmergencyHealing(currentMana, pctMana, true);
								return CastReturn.CAST_INTERRUPTFORHEAL;
							}
							if (!isEmergency && Heals.SomeoneNeedEmergencyHealingGroup(currentMana, pctMana))
							{

								E3.Bots.Broadcast($"\arInterrupting \aw[\ag{spell.CastName}\aw] \agfor Emergency Group Heal.");
								Interrupt();
								E3.ActionTaken = true;
								//fire of emergency heal asap!
								//checks group members
								Heals.SomeoneNeedEmergencyHealingGroup(currentMana, pctMana, true);
								return CastReturn.CAST_INTERRUPTFORHEAL;
							}


							if (!spell.NoInterrupt)
							{
								//if detremental or a buff, interrupt for healing if necessary
								if ((spell.SpellType.Equals("Detrimental") || spell.Duration > 0) && Heals.SomeoneNeedsHealing(null, currentMana, pctMana))
								{
									MQ.Write($"\arInterrupting \aw[\ag{spell.CastName}\aw] \agfor Healing.");
									Interrupt();
									E3.ActionTaken = true;
									//fire of emergency heal asap!
									//checks group members
									Heals.SomeoneNeedsHealing(null, currentMana, pctMana, true);
									return CastReturn.CAST_INTERRUPTFORHEAL;
								}

								if (interruptCheck != null && interruptCheck(spell, currentMana, pctMana))
								{
									Interrupt();
									E3.ActionTaken = true;
									E3.Bots.Broadcast($@"\arInterrupting \aw[\ag{spell.CastName}\aw] because of interrupt check.");
									return CastReturn.CAST_INTERRUPTED;
								}

								//check to see if there is a nowcast queued up, if so we need to kickout.
								if (!isNowCast && !isEmergency && NowCastReady())
								{
									//we have a nowcast ready to be processed
									Interrupt();
									return CastReturn.CAST_INTERRUPTED;
								}
								//check if we need to process any events,if healing tho, ignore. 
								if ((spell.SpellType.Equals("Detrimental") || spell.Duration > 0) || E3.CurrentClass == Class.Bard)
								{
									if (EventProcessor.CommandListQueueHasCommand("/backoff"))
									{
										EventProcessor.ProcessEventsInQueues("/backoff");
										if (!IsCasting()) return CastReturn.CAST_INTERRUPTED;
									}
									//in case the user sends out e3bc commands while casting


									if (EventProcessor.CommandListQueueHasCommand("/backoff"))
									{
										EventProcessor.ProcessEventsInQueues("/backoff");
										if (!IsCasting()) return CastReturn.CAST_INTERRUPTED;

									}
									if (EventProcessor.CommandListQueueHasCommand("/assistme"))
									{
										Int32 tAssistID = Assist.AssistTargetID;

										EventProcessor.ProcessEventsInQueues("/assistme");

										if (tAssistID > 0 && Assist.AssistTargetID > 0 && tAssistID != Assist.AssistTargetID)
										{
											Interrupt();
											if (!IsCasting()) return CastReturn.CAST_INTERRUPTED;
										}
									}
									if (EventProcessor.CommandListQueueHasCommand("/followme"))
									{
										EventProcessor.ProcessEventsInQueues("/followme");
										if (!IsCasting()) return CastReturn.CAST_INTERRUPTED;
									}
									if (E3.CurrentClass == Class.Druid || E3.CurrentClass == Class.Wizard)
									{
										if (EventProcessor.CommandListQueueHasCommand("/evac"))
										{
											Interrupt();
											EventProcessor.ProcessEventsInQueues("/evac");
											if (!IsCasting()) return CastReturn.CAST_INTERRUPTED;
										}
									}
								}
							}
							if (spell.SpellType.Equals("Detrimental") && (spell.TargetType != "PB AE" && spell.TargetType != "Self"))
							{
								TrueTarget(targetID);
								bool isCorpse = MQ.Query<bool>("${Target.Type.Equal[Corpse]}");

								if (isCorpse || !MQ.Query<bool>("${Target.ID}"))
								{
									//shouldn't nuke dead things
									Assist.AssistOff();
									Interrupt();
									return CastReturn.CAST_INTERRUPTED;
								}
							}

							//process any commands we need to process from the UI or just basic commands from other bots/drivers
							NetMQServer.SharedDataClient.ProcessCommands();
							PubClient.ProcessRequests();
							MQ.Delay(50);

							//if (E3.IsPaused())
							//{
							//	Interrupt();
							//	return CastReturn.CAST_INTERRUPTED;
							//}

							if (e3util.IsShuttingDown())
							{
								Interrupt();
								EventProcessor.ProcessEventsInQueues("/shutdown");
								return CastReturn.CAST_INTERRUPTED;
							}
							if (!isNowCast && MQ.Query<bool>("${Me.Invis}"))
							{
								Interrupt();
								return CastReturn.CAST_INVIS;
							}
							//get updated information after delays
							E3.StateUpdates();

						}

						//sometimes the cast isn't fully complete even if the window is done
						///allow the player to 'tweak' this value.
						if (E3.CharacterSettings.Misc_DelayAfterCastWindowDropsForSpellCompletion > 0)
						{
							MQ.Delay(E3.CharacterSettings.Misc_DelayAfterCastWindowDropsForSpellCompletion);
						}
						if (spell.AfterCastCompletedDelay > 0)
						{
							MQ.Delay(spell.AfterCastCompletedDelay);
						}


						returnValue = CheckForReist(spell,isNowCast);

						if (returnValue == CastReturn.CAST_SUCCESS)
						{
							_lastSuccesfulCast = spell.CastName;
							//clear the spell counter for this pell on this mob?
							if (ResistCounters.ContainsKey(targetID))
							{
								if (ResistCounters[targetID].SpellCounters.ContainsKey(spell.SpellID))
								{
									ResistCounters[targetID].SpellCounters[spell.SpellID] = 0;
								}
							}
						}
						else if (returnValue == CastReturn.CAST_RESIST || returnValue == CastReturn.CAST_TAKEHOLD)
						{
							if (!ResistCounters.ContainsKey(targetID))
							{
								ResistCounter tresist = ResistCounter.Aquire();
								ResistCounters.Add(targetID, tresist);
							}
							ResistCounter resist = ResistCounters[targetID];
							if (!resist.SpellCounters.ContainsKey(spell.SpellID))
							{
								resist.SpellCounters.Add(spell.SpellID, 0);
							}
							resist.SpellCounters[spell.SpellID]++;

						}
						else if (returnValue == CastReturn.CAST_IMMUNE)
						{
							if (!ResistCounters.ContainsKey(targetID))
							{
								ResistCounter tresist = ResistCounter.Aquire();
								ResistCounters.Add(targetID, tresist);
							}
							ResistCounter resist = ResistCounters[targetID];
							if (!resist.SpellCounters.ContainsKey(spell.SpellID))
							{
								resist.SpellCounters.Add(spell.SpellID, 0);
							}
							resist.SpellCounters[spell.SpellID] = 99;
						}
						//MQ.Write($"{spell.CastName} Result:{returnValue.ToString()}");

						AfterSpellCheck(spell, targetID);
						AfterEventCheck(spell);
						//TODO: bard resume twist
						//start the GCD
						if (spell.CastType == Data.CastingType.Spell)
						{
							_lastSpellCastTimeStamp = Core.StopWatch.ElapsedMilliseconds;
						}
						E3.ActionTaken = true;
						//clear out the queues for the resist counters as they may have a few that lagged behind.
						ClearResistChecks();
						return returnValue;

					}
					MQ.Write($"\arInvalid targetId for Casting. {targetID}");
					E3.ActionTaken = true;
					return CastReturn.CAST_NOTARGET;
				}
			}
			finally
			{
				//send message to the ui to clear their casting information
				PubServer.AddTopicMessage("${Casting}", String.Empty);
				PubServer.AddTopicMessage("${Me.Casting}", String.Empty);
				//unpause any stick command that may be paused
				MQ.Cmd("/stick unpause");
				//resume navigation.
				if (e3PausedNav)
				{
					navPaused = MQ.Query<bool>("${Navigation.Paused}");
					if (navPaused)
					{
						MQ.Cmd("/nav pause");
					}
				}
				if (spell.Debug)
				{
					Logging.MinLogLevelTolog = _previousLogLevel;

				}
			}
		}

		private static void BeforeEventCheck(Spell spell)
		{
			_log.Write("Checking BeforeEvent...");
			if (!String.IsNullOrWhiteSpace(spell.BeforeEvent))
			{
				_log.Write($"Doing BeforeEvent:{spell.BeforeEvent}");
				string tevent = Ifs_Results(spell.BeforeEvent);

				bool internalComand = false;

				//if we don't have any tlo calls
				if (!tevent.Contains("${"))
				{
					foreach (var pair in EventProcessor.CommandList)
					{
						string compareCommandTo = pair.Key;
						if (tevent.Contains(" "))
						{
							compareCommandTo = pair.Value.commandwithSpace;
						}
						if (tevent.StartsWith(compareCommandTo, StringComparison.OrdinalIgnoreCase))
						{
							internalComand = true;
							//no need to send this to mq if its our own command, just drop it into the queues to be processed. 
							EventProcessor.ProcessInternalCommandAndExecute(tevent, pair.Value.command);
							break;
						}
					}
				}
				
				if (!internalComand)
				{
					MQ.Cmd($"/docommand {tevent}");
				}
				if (spell.BeforeEvent.StartsWith("/exchange", StringComparison.OrdinalIgnoreCase)) MQ.Delay(500);
				if (spell.BeforeEventDelay > 0) MQ.Delay(spell.BeforeEventDelay);
			}

		}
		private static void AfterEventCheck(Spell spell)
		{

			//after event, after all things are done


			_log.Write($"Checking AfterEvent...[{spell.AfterEvent}]");
			if (!String.IsNullOrWhiteSpace(spell.AfterEvent))
			{
				if (spell.AfterEventDelay > 0) MQ.Delay(spell.AfterEventDelay);

				_log.Write($"Doing AfterEvent:{spell.AfterEvent}");
				string tevent = Ifs_Results(spell.AfterEvent);

				bool internalComand = false;
				if (!tevent.Contains("${"))
				{
					foreach (var pair in EventProcessor.CommandList)
					{
						string compareCommandTo = pair.Key;
						if (tevent.Contains(" "))
						{
							compareCommandTo = pair.Value.commandwithSpace;
						}
						if (tevent.StartsWith(compareCommandTo, StringComparison.OrdinalIgnoreCase))
						{
							internalComand = true;
							EventProcessor.ProcessInternalCommandAndExecute(tevent, pair.Value.command);
							break;
						}
					}
				}
				if (!internalComand)
				{
					MQ.Cmd($"/docommand {tevent}");
				}
			}
		}
		private static void AfterSpellCheck(Spell spell, Int32 targetID)
		{
			//is an after spell configured? lets do that now.
			_log.Write("Checking AfterSpell...");
			if (!String.IsNullOrWhiteSpace(spell.AfterSpell))
			{
				

				if (spell.AfterSpellDelay > 0) MQ.Delay(spell.AfterSpellDelay);

				if (spell.AfterSpellData == null)
				{
					spell.AfterSpellData = new Data.Spell(spell.AfterSpell);
				}

				_log.Write("Doing AfterSpell:{spell.AfterSpell}");

				//we may have just cast a spell, and may be in global cooldown
				if (spell.CastType == CastingType.Spell)
				{
					Int32 maxTries = 0;
					while (InGlobalCooldown())
					{
						MQ.Delay(50);
						maxTries++;
						if (maxTries > 40) break;
					}
				}
				if (CheckMana(spell.AfterSpellData) && CheckReady(spell.AfterSpellData))
				{
					e3util.ClearCursor();
				retrycast:
					Int32 retryCounter = 0;
					if (Casting.Cast(targetID, spell.AfterSpellData) == CastReturn.CAST_FIZZLE)
					{
						retryCounter++;
						if (retryCounter > 5)
						{
							return;
						}
						goto retrycast;

					}
				}
			}
		}
		private static void BeforeSpellCheck(Spell spell, Int32 targetID)
		{
			_log.Write("Checking BeforeSpell...");
			if (!String.IsNullOrWhiteSpace(spell.BeforeSpell))
			{

				if (spell.BeforeSpellData == null)
				{
					spell.BeforeSpellData = new Data.Spell(spell.BeforeSpell);
				}
				//Wait for GCD if spell
				//we may have just cast a spell, and may be in global cooldown
				if (spell.CastType == CastingType.Spell)
				{
					Int32 maxTries = 0;
					while (InGlobalCooldown())
					{
						MQ.Delay(50);
						maxTries++;
						if (maxTries > 40) break;
					}
				}
				if (CheckMana(spell.BeforeSpellData) && CheckReady(spell.BeforeSpellData))
				{
				retrycast:
					if (Casting.Cast(targetID, spell.BeforeSpellData) == CastReturn.CAST_FIZZLE)
					{
						goto retrycast;
					}
				}
				_log.Write($"Doing BeforeSpell:{spell.BeforeSpell}");
				if (spell.BeforeSpellDelay > 0) MQ.Delay(spell.BeforeSpellDelay);
			}
		}
		private static bool NowCastReady()
		{
			if (((EventProcessor.CommandList.ContainsKey("/nowcast") && EventProcessor.CommandListQueueHasCommand("/nowcast")) || PubClient.NowCastInQueue()))
			{
				return true;
			}
			return false;
		}
		public static void Sing(Int32 targetid, Data.Spell spell)
		{

			if (E3.CurrentClass != Data.Class.Bard) return;
			//Stop following for spell/item/aa with a cast time > 0 MyCastTime, unless im a bard
			//anything under 300 is insta cast

			if (targetid > 0)
			{
				TrueTarget(targetid);
			}

			if (spell.CastType == CastingType.Spell)
			{
				//if (MQ.Query<bool>($"${{Bool[${{Me.Book[{spell.CastName}]}}]}}"))
				{

					MQ.Cmd("/stopsong");

					if (!String.IsNullOrWhiteSpace(spell.BeforeEvent))
					{
						_log.Write($"Doing BeforeEvent:{spell.BeforeEvent}");
						MQ.Cmd($"/docommand {spell.BeforeEvent}");
						//if (spell.BeforeEvent.StartsWith("/exchange", StringComparison.OrdinalIgnoreCase)) MQ.Delay(300);
						//if (spell.BeforeEvent.StartsWith("/bando", StringComparison.OrdinalIgnoreCase)) MQ.Delay(300);
					}
					Int32 retryCounter = 0;
				retrysong:
					MQ.Cmd("/stopsong");
					PubServer.AddTopicMessage("${Me.Casting}", spell.CastName);
					MQ.Cmd($"/cast \"{spell.CastName}\"");
					if (spell.MyCastTime > 500)
					{
						MQ.Delay(300, IsCasting);
						if (e3util.IsEQLive())
						{
							if (IsCasting())
							{
								//on live the cast window comes up on a missed note, so we check just for a bit to make sure so we can recast. 
								MQ.Delay(500);
							}
						}
					}
					//sometimes the cast isn't fully complete even if the window is done
					///allow the player to 'tweak' this value.
					if (E3.CharacterSettings.Misc_DelayAfterCastWindowDropsForSpellCompletion > 0)
					{
						MQ.Delay(E3.CharacterSettings.Misc_DelayAfterCastWindowDropsForSpellCompletion);
					}
					if (!IsCasting())
					{
						if (retryCounter < 5)
						{
							retryCounter++;
							goto retrysong;
						}
					}

					//after event, after all things are done               
					if (!String.IsNullOrWhiteSpace(spell.AfterEvent))
					{
						_log.Write($"Doing AfterEvent:{spell.AfterEvent}");
						MQ.Cmd($"/docommand {spell.AfterEvent}");
					}


				}
			}
			else if (spell.CastType == CastingType.Item)
			{
				if (spell.MyCastTime > 500)
				{
					MQ.Cmd("/stopsong", 100);
				}
				// special exception for this item
				var luteName = "Lute of the Flowing Waters";
				if (string.Equals(spell.CastName, luteName))
				{
					var chorusSpell = MQ.Query<string>("${Me.Song[Chorus]}");
					if (!string.Equals("NULL", chorusSpell))
					{
						var stacks = MQ.Query<bool>($"${{Spell[{spell.SpellName}].StacksWith[{chorusSpell}]}}");
						if (!stacks)
						{
							return;
						}
					}
				}
				if (!String.IsNullOrWhiteSpace(spell.BeforeEvent))
				{
					_log.Write($"Doing BeforeEvent:{spell.BeforeEvent}");
					MQ.Cmd($"/docommand {spell.BeforeEvent}");
					if (spell.BeforeEvent.StartsWith("/exchange", StringComparison.OrdinalIgnoreCase)) MQ.Delay(300);
				}
				PubServer.AddTopicMessage("${Me.Casting}", spell.CastName);
				MQ.Cmd($"/useitem \"{spell.CastName}\"", 300);
				//after event, after all things are done               
				if (!String.IsNullOrWhiteSpace(spell.AfterEvent))
				{
					_log.Write($"Doing AfterEvent:{spell.AfterEvent}");
					MQ.Cmd($"/docommand {spell.AfterEvent}");
				}
			}
			else if (spell.CastType == CastingType.AA)
			{
				if (spell.MyCastTime > 500)
				{
					MQ.Cmd("/stopsong", 100);
				}
				if (!String.IsNullOrWhiteSpace(spell.BeforeEvent))
				{
					_log.Write($"Doing BeforeEvent:{spell.BeforeEvent}");
					MQ.Cmd($"/docommand {spell.BeforeEvent}");
					if (spell.BeforeEvent.StartsWith("/exchange", StringComparison.OrdinalIgnoreCase)) MQ.Delay(300);
				}
				PubServer.AddTopicMessage("${Me.Casting}", spell.CastName);

				//MQ.Cmd($"/casting \"{spell.CastName}\" alt", 300);
				MQ.Cmd($"/alt activate {spell.AAID}", 300);
				//after event, after all things are done               
				if (!String.IsNullOrWhiteSpace(spell.AfterEvent))
				{
					_log.Write($"Doing AfterEvent:{spell.AfterEvent}");
					MQ.Cmd($"/docommand {spell.AfterEvent}");
				}
			}
		}
		public static bool IsSpellMemed(string spellName)
		{
			foreach (Int32 spellid in _currentSpellGems.Values)
			{
				if (spellid > 0)
				{
					string spellGemName = MQ.Query<string>($"${{Spell[{spellid}]}}");

					if (spellGemName.Equals(spellName, StringComparison.OrdinalIgnoreCase))
					{
						return true;

					}
				}
			}
			return false;
		}
		public static void MemorizeAllSpells()
		{
			MQ.Cmd("/squelch /windowstate SpellBookWnd open", 1000);
			foreach (Spell s in E3.CharacterSettings.Nukes)
			{
				if (!SpellBookWndOpen()) return;
				MemorizeSpell(s, true);
			}
			foreach (Spell s in E3.CharacterSettings.Dots_OnCommand)
			{
				if (!SpellBookWndOpen()) return;
				MemorizeSpell(s, true);
			}
			foreach (Spell s in E3.CharacterSettings.Dots_Assist)
			{
				if (!SpellBookWndOpen()) return;
				MemorizeSpell(s, true);
			}
			foreach (Spell s in E3.CharacterSettings.Debuffs_Command)
			{
				if (!SpellBookWndOpen()) return;
				MemorizeSpell(s, true);
			}
			foreach (Spell s in E3.CharacterSettings.Debuffs_OnAssist)
			{
				if (!SpellBookWndOpen()) return;
				MemorizeSpell(s, true);
			}
			foreach (Spell s in E3.CharacterSettings.HealTanks)
			{
				if (!SpellBookWndOpen()) return;
				MemorizeSpell(s, true);
			}
			foreach (Spell s in E3.CharacterSettings.HealImportantBots)
			{
				if (!SpellBookWndOpen()) return;
				MemorizeSpell(s, true);
			}
			foreach (Spell s in E3.CharacterSettings.HealTanks)
			{
				if (!SpellBookWndOpen()) return;
				MemorizeSpell(s, true);
			}
			foreach (Spell s in E3.CharacterSettings.HealAll)
			{
				if (!SpellBookWndOpen()) return;
				MemorizeSpell(s, true);
			}

			MQ.Cmd("/stand");
		}
		public static bool SpellBookWndOpen()
		{
			return MQ.Query<bool>("${Window[SpellBookWnd].Open}");

		}

		public static bool BuffNotReady(Data.Spell spell)
		{
			if (spell.CastType == CastingType.Spell && spell.SpellInBook && Casting.SpellMemorized(spell) && Casting.SpellInCooldown(spell)) return true;
			if (spell.CastType == CastingType.Item && Casting.ItemInCooldown(spell)) return true;

			return false;
		}
		public static bool SpellMemorized(Data.Spell spell)
		{
			foreach (var spellid in _currentSpellGems.Values)
			{
				if (spellid == spell.SpellID && spellid != 0)
				{
					return true;
				}
			}

			return false;
		}
		public static bool AnySpellMemorized()
		{
			foreach (var spellid in _currentSpellGems.Values)
			{
				if (spellid != 0)
				{
					return true;
				}
			}
			return false;
		}
		public static bool MemorizeSpell(Data.Spell spell,bool ignoreWait=false)
		{

			//don't try and mem a spell if you are max aggro on anything as it will auto crit you.
			if (Basics.InCombat() && (E3.CurrentClass & Data.Class.Tank) == E3.CurrentClass) return false;
			if (e3util.GetXtargetMaxAggro() == 100) return false;

		
			if (!(spell.CastType == CastingType.Spell && spell.SpellInBook))
			{
				//we can't mem this just return true
				return true;
			}
			//if no spell gem is set, set it.
			if (spell.SpellGem == 0)
			{
				spell.SpellGem = E3.GeneralSettings.Casting_DefaultSpellGem;
			}
			foreach (var spellid in _currentSpellGems.Values)
			{
				if (spellid == spell.SpellID && spellid != 0)
				{
					return true;
				}
			}

			Int32 spellID;
			if (_currentSpellGems.TryGetValue(spell.SpellGem, out spellID))
			{
				if (spell.SpellID == spellID)
				{
					//already memed, exit.
					return true;
				}
			}

			//memorize may fail if there is a gem "Lockout" time period, where
			//We JUST memed a spell so its protected to be used for a period of its recast time.
			if (_gemRecastLockForMem.ContainsKey(spell.SpellGem))
			{
				//there is a spell lock possibly on this gem, check
				if (_gemRecastLockForMem[spell.SpellGem] > Core.StopWatch.ElapsedMilliseconds)
				{
					//this is still locked, return false
					return false;
				}
			}
			MQ.Write($"\aySpell not memed, meming \ag{spell.SpellName} \ayin \awGEM:{spell.SpellGem}");
			MQ.Cmd($"/memspell {spell.SpellGem} \"{spell.SpellName}\"");
			MQ.Delay(15000, $"${{Me.Gem[{spell.SpellGem}].Name.Equal[{spell.SpellName}]}} || !${{Window[SpellBookWnd].Open}}");
			if(!ignoreWait)
			{
				//sanity check that we stand in case something went wrong
				//we do it in the ignorewait, because if we do ignore wait they already will do the 
				//sit/stand as we are meming lots of spells at once. 
				MQ.Cmd("/stand");
				MQ.Delay(3000, $"${{Me.SpellReady[${{Me.Gem[{spell.SpellGem}].Name}}]}}");
			}

			//make double sure the collectio has this spell gem. maybe purchased AA for new slots?
			if (!_gemRecastLockForMem.ContainsKey(spell.SpellGem))
			{
				_gemRecastLockForMem.Add(spell.SpellGem, 0);
			}
			_gemRecastLockForMem[spell.SpellGem] = Core.StopWatch.ElapsedMilliseconds + spell.RecastTime + 2000;

			//update spellgem collection
			if (!_currentSpellGems.ContainsKey(spell.SpellGem))
			{
				_currentSpellGems.Add(spell.SpellGem, spell.SpellID);
			}
			_currentSpellGems[spell.SpellGem] = spell.SpellID;

			return true;
		}

		public static Boolean CheckMana(Data.Spell spell)
		{
			if (!spell.Initialized) spell.ReInit();

			Int32 currentMana = MQ.Query<Int32>("${Me.CurrentMana}");
			Int32 pctMana = MQ.Query<Int32>("${Me.PctMana}");
			if (currentMana >= spell.Mana)
			{
				if (spell.MaxMana > 0)
				{
					if (pctMana > spell.MaxMana)
					{
						return false;
					}
				}
				if (pctMana >= spell.MinMana)
				{

					return true;

				}
			}
			return false;
		}
		public static void Interrupt()
		{
			_lastSpellCastTimeStamp = 0;
			_log.Write("\arINTERRUPTING\ag Spell..");
			if (!IsCasting()) return;
			bool onMount = MQ.Query<bool>("${Me.Mount.ID}");
			if (onMount && e3util.IsEQEMU())
			{
				//can't interrupt on emu.
				if (E3.CharacterSettings.Misc_DismountOnInterrupt)
				{
					MQ.Cmd("/dismount");
				}
				else
				{
					//have to wait for the spell to be done
					while (IsCasting())
					{
						MQ.Delay(50);
					}
					return;
				}
			}
			MQ.Cmd("/stopcast");
			//we will get an interrupt event queued up, so we need to clear it out. 
		
		}
		public static Boolean IsCasting()
		{
			if (MQ.Query<bool>("${Window[CastingWindow].Open}"))
			{
				//MQ.Delay(0);
				return true;
			}

			return false;
		}
		public static Boolean IsNotCasting()
		{
			return !IsCasting();
		}
		public static Boolean InGlobalCooldown()
		{
			//pure melee don't have 
			if ((E3.CurrentClass & Class.PureMelee) == E3.CurrentClass) return false;
			if (E3.CurrentClass == Class.Bard)
			{
				return false;
			}
			if (_lastSpellCastTimeStamp + 1500 > Core.StopWatch.ElapsedMilliseconds)
			{
				return true;
			}

			if(!AnySpellMemorized())
			{
				return false;
			}
			if (MQ.Query<bool>("${Me.SpellReady[${Me.Gem[1].Name}]}") || MQ.Query<bool>("${Me.SpellReady[${Me.Gem[2].Name}]}") || MQ.Query<bool>("${Me.SpellReady[${Me.Gem[3].Name}]}") || MQ.Query<bool>("${Me.SpellReady[${Me.Gem[4].Name}]}") || MQ.Query<bool>("${Me.SpellReady[${Me.Gem[5].Name}]}") || MQ.Query<bool>("${Me.SpellReady[${Me.Gem[6].Name}]}") || MQ.Query<bool>("${Me.SpellReady[${Me.Gem[7].Name}]}") || MQ.Query<bool>("${Me.SpellReady[${Me.Gem[8].Name}]}"))
			{
				return false;
			}
			return true;
		}

		private static System.Collections.Generic.Dictionary<String, Int64> _ItemCooldownLookup = new Dictionary<string, long>() { { "Invocation Rune: Vulka's Chant of Lightning", 18000 }, { "Invocation Glyph: Vulka's Chant of Lightning", 12000 } };
		private static System.Collections.Generic.Dictionary<String, Int64> _ItemsInCooldown = new Dictionary<string, long>() { };
		private static System.Collections.Generic.Dictionary<String, Int64> _AAInCooldown = new Dictionary<string, long>() { };
		private static System.Collections.Generic.Dictionary<String, Int64> _DiscInCooldown = new Dictionary<string, long>() { };
		private static System.Collections.Generic.Dictionary<String, Int64> _AbilityInCooldown = new Dictionary<string, long>() { };

		public static void UpdateAbilityInCooldown(Data.Spell spell)
		{
			string abilityToCheck = spell.CastName;
			//check to see if its one of the items we are tracking
			if (!_AbilityInCooldown.ContainsKey(spell.CastName))
			{
				_AbilityInCooldown.Add(spell.CastName, 0);
			}
			_AbilityInCooldown[spell.CastName] = Core.StopWatch.ElapsedMilliseconds;
		}
		public static bool AbilityInCooldown(Data.Spell spell)
		{
			if (_AbilityInCooldown.ContainsKey(spell.CastName))
			{
				//going to hard code a 1 sec cooldown on all Dsics's to allow time for the client to get updated info for ability ready
				Int64 timestampOfLastCast = _AbilityInCooldown[spell.CastName];
				Int64 numberOfMilliSecondCooldown = 1000;
				if (Core.StopWatch.ElapsedMilliseconds - timestampOfLastCast < numberOfMilliSecondCooldown)
				{
					//still in cooldown
					return true;
				}
			}
			string abilityToCheck = spell.CastName;
			//work around due to MQ bug with Slam
			if (abilityToCheck.Equals("Slam", StringComparison.OrdinalIgnoreCase))
			{
				abilityToCheck = "Bash";
			}
			if (MQ.Query<bool>($"${{Me.AbilityReady[{abilityToCheck}]}}"))
			{
				return false;
			}
			
			return true;
		}
		public static void UpdateDiscInCooldown(Data.Spell spell)
		{
			//check to see if its one of the items we are tracking
			if (!_DiscInCooldown.ContainsKey(spell.CastName))
			{
				_DiscInCooldown.Add(spell.CastName, 0);
			}
			_DiscInCooldown[spell.CastName] = Core.StopWatch.ElapsedMilliseconds;
		}
		public static bool DiscInCooldown(Data.Spell spell)
		{
			if (_DiscInCooldown.ContainsKey(spell.CastName))
			{
				//going to hard code a 1 sec cooldown on all Dsics's to allow time for the client to get updated info for ability ready
				Int64 timestampOfLastCast = _DiscInCooldown[spell.CastName];
				Int64 numberOfMilliSecondCooldown = 1000;
				if (Core.StopWatch.ElapsedMilliseconds - timestampOfLastCast < numberOfMilliSecondCooldown)
				{
					//still in cooldown
					return true;
				}
			}
			if (spell.SpellID == 8001) return false;

			if (MQ.Query<Int32>($"${{Me.CombatAbilityTimer[{spell.CastName}]}}") == 0)
			{
				return false;
			}
			if (MQ.Query<bool>($"${{Me.CombatAbilityReady[{spell.CastName}]}}"))
			{
				return false;
			}
			
			return true;
		}

		public static void UpdateAAInCooldown(Data.Spell spell)
		{
			//check to see if its one of the items we are tracking
			if (!_AAInCooldown.ContainsKey(spell.CastName))
			{
				_AAInCooldown.Add(spell.CastName, 0);
			}
			_AAInCooldown[spell.CastName] = Core.StopWatch.ElapsedMilliseconds;
		}


		public static bool AAInCooldown(Data.Spell spell)
		{
			if (_AAInCooldown.ContainsKey(spell.CastName))
			{
				//going to hard code a 1 sec cooldown on all AA's to allow time for the client to get updated info for ability ready
				Int64 timestampOfLastCast = _AAInCooldown[spell.CastName];
				Int64 numberOfMilliSecondCooldown = 1000;
				if (Core.StopWatch.ElapsedMilliseconds - timestampOfLastCast < numberOfMilliSecondCooldown)
				{
					//still in cooldown
					return true;
				}
			}
			if (MQ.Query<bool>($"${{Me.AltAbilityReady[{spell.CastName}]}}"))
			{
				return false;
			}
			return true;
		}

		public static void UpdateItemInCooldown(Data.Spell spell)
		{
			if (!_ItemsInCooldown.ContainsKey(spell.CastName))
			{
				_ItemsInCooldown.Add(spell.CastName, 0);
			}
			_ItemsInCooldown[spell.CastName] = Core.StopWatch.ElapsedMilliseconds;
		}
		public static bool ItemInCooldown(Data.Spell spell)
		{
			//if one of the special items we are checking in cooldown (augs mostly)
			if (_ItemCooldownLookup.ContainsKey(spell.CastName))
			{
				if (!_ItemsInCooldown.ContainsKey(spell.CastName))
				{
					return false;
				}
				else
				{
					//we have it in cooldown, lets check if its greater than what we have 
					Int64 timestampOfLastCast = _ItemsInCooldown[spell.CastName];
					Int64 numberOfMilliSecondCooldown = _ItemCooldownLookup[spell.CastName];
					if (Core.StopWatch.ElapsedMilliseconds - timestampOfLastCast < numberOfMilliSecondCooldown)
					{
						//still in cooldown
						return true;
					}
					else
					{
						return false;
					}
				}
			}
			else
			{
				if (!_ItemsInCooldown.ContainsKey(spell.CastName))
				{
					if (MQ.Query<bool>($"${{Me.ItemReady[={spell.CastName}]}}"))
					{
						return false;
					}
					return true;
				}
				else
				{
					//minium of a 1 sec cooldown on items, to make sure their cooldown is respected
					Int64 timestampOfLastCast = _ItemsInCooldown[spell.CastName];
					Int64 numberOfMilliSecondCooldown = 1000;
					if (Core.StopWatch.ElapsedMilliseconds - timestampOfLastCast < numberOfMilliSecondCooldown)
					{
						//still in cooldown
						return true;
					}
					else
					{
						if (MQ.Query<bool>($"${{Me.ItemReady[={spell.CastName}]}}"))
						{
							return false;
						}
					}
				}
				
			}
			return true;
		}


		public static bool SpellInCooldown(Data.Spell spell)
		{
			Int32 gemCooldown = MQ.Query<Int32>($"${{Me.GemTimer[{spell.CastName}]}}");
			//_log.Write($@"SpellInCooldown for spell: {spell.CastName} checking gem timer [{gemCooldown}].");
			//if (SpellInSharedCooldown(spell)) return true;
			//_log.Write($"Checking if spell is ready on {spell.CastName}");
			if(gemCooldown ==0)
			{
				//_log.Write($@"{spell.CastName} gem timer is zero.");
				//check if we are out of stock still
				if(spell.ReagentOutOfStock)
				{
					Int32 itemCount = MQ.Query<Int32>($"${{FindItemCount[={spell.Reagent}]}}");
					if (itemCount<1) return true;
				}
				//_log.Write($"CheckReady Success! on {spell.CastName}");
				return false;
			}

			return true;
		}
		private static Dictionary<string, List<string>> _sharedCooldownLookup = new Dictionary<string, List<string>>() {
			{ "Miasmic Spear", new List<string>() { "Spear of Muram" } },
			{ "Spear of Muram", new List<string>() { "Miasmic Spear" } },
			{ "Focused Hail of Arrows", new List<string>() { "Hail of Arrows" } },
			{ "Hail of Arrows", new List<string>() { "Focused Hail of Arrows" } },
			{ "Mana Flare", new List<string>() { "Mana Recursion" } },
			{ "Mana Recursion", new List<string>() { "Mana Flare" } }
		};
		private static bool SpellInSharedCooldown(Spell spell)
		{
			if (!_sharedCooldownLookup.ContainsKey(spell.CastName)) return false;

			if (MQ.Query<bool>($"${{Bool[${{Me.Gem[{spell.CastName}]}}]}}"))
			{
				if (!MQ.Query<bool>($"${{Me.SpellReady[{spell.CastName}]}}")) { return true; }
				foreach (string spellName in _sharedCooldownLookup[spell.CastName])
				{
					if (MQ.Query<bool>($"${{Bool[${{Me.Gem[{spellName}]}}]}}"))
					{
						if (!MQ.Query<bool>($"${{Me.SpellReady[{spellName}]}}")) { return true; }
					}
				}
			}
			return false;
		}


		public static Boolean CheckReady(Data.Spell spell, bool skipCastCheck = false, bool skipGCDCheck=false)
		{
			if (spell == null) return false;

			if (spell.RecastDelay>0)
			{
				if(spell.LastAssistTimeStampForCast!=Assist.LastAssistStartedTimeStamp)
				{
					//different time stamp for an assist, we can zero out the lastcastimetamp
					//this will get set on the next cast
					spell.LastCastTimeStamp = 0;
				}

				//if a timestamp was set			
				if(spell.LastCastTimeStamp>0)
				{
					if ((Core.StopWatch.ElapsedMilliseconds - spell.LastCastTimeStamp) < spell.RecastDelay)
					{
						return false;
					}
				}
			}

			
			if (!spell.Enabled) return false;
			if (!spell.Initialized) spell.ReInit();

			if (e3util.IsActionBlockingWindowOpen())
			{
				return false;
			}
			if (spell.MinAggro > 0 || spell.MaxAggro > 0)
			{
				Int32 pctAggro = MQ.Query<Int32>("${Me.PctAggro}");
				if (spell.MinAggro > 0 && pctAggro < spell.MinAggro)
				{
					return false;
				}
				if (spell.MaxAggro > 0 && pctAggro >= spell.MaxAggro)
				{
					return false;
				}
			}
			//if your stunned nothing is ready
			if (MQ.Query<bool>("${Me.Stunned}"))
			{
				return false;
			}
			if (spell.CastType == CastingType.None) return false;
			//do we need to memorize it?
			if ((spell.CastType == CastingType.Spell || spell.CastType == CastingType.Item || spell.CastType == CastingType.AA) && MQ.Query<bool>("${Debuff.Silenced}")) return false;

			//_log.Write($"CheckReady on {spell.CastName}");
			if(!skipCastCheck)
			{
				if (E3.CurrentClass != Data.Class.Bard)
				{
					while (IsCasting())
					{
						MQ.Delay(20);
					}
				}
			}

			bool returnValue = false;
			if (spell.CastType == Data.CastingType.Spell && spell.SpellInBook)
			{
				_log.Write("Checking spell in book cooldown...");
				//do we already have it memed?
				bool spellMemed = false;
				foreach (var spellid in _currentSpellGems.Values)
				{
					if (spellid == spell.SpellID && spellid != 0)
					{
						spellMemed = true;
						break;
					}
				}

				//if not memed, and we are not currently tanking
				//mem the spell or try to.
				if (!spellMemed)
				{
					//lets not sit while we have 100% aggro on a mob , crits be bad
					Int32 pctAggro = MQ.Query<Int32>("${Me.PctAggro}");

					if (pctAggro == 100)
					{
						//don't try and mem a spell while tanking
						return false;
					}
					if (!MemorizeSpell(spell))
					{
						return false;
					}
				}
				if (!skipGCDCheck)
				{
					_log.Write("Checking global cool cooldown...");
					if (InGlobalCooldown())
					{
						_log.Write("In GCD returning false");
						return false;
					}
				}

				if (!SpellInCooldown(spell))
				{
					_log.Write("NOT Spell cooldown returning true");
					return true;
				}
				
			}
			else if (spell.CastType == Data.CastingType.Item)
			{
				if (!ItemInCooldown(spell))
				{
					return true;
				}
			}
			else if (spell.CastType == Data.CastingType.AA)
			{
				if (!AAInCooldown(spell))
				{
					return true;
				}

			}
			else if (spell.CastType == Data.CastingType.Disc)
			{
				if(!DiscInCooldown(spell))
				{
					return true;
				}
			}
			else if (spell.CastType == Data.CastingType.Ability)
			{
				if(!AbilityInCooldown(spell))
				{
					return true;
				}
			}

			return returnValue;
		}
		public static bool InRange(Int32 targetId, Data.Spell spell)
		{
			if (!spell.Initialized) spell.ReInit(); 

			if (spell.MyRange == 0) return true;

			Spawn s;
			if (_spawns.TryByID(targetId, out s))
			{
				double targetDistance = s.Distance;
				if (targetDistance <= spell.MyRange)
				{
					return true;
				}
			}
			return false;
		}

		public static Dictionary<string, string> VarsetValues = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		[SubSystemInit]
		public static void Casting_InitCommands()
		{

			EventProcessor.RegisterCommand("/e3resetcounters", (x) =>
			{
				Casting.ResetResistCounters();
				E3.Bots.Broadcast("Resetting resist counters...");
			});
			EventProcessor.RegisterCommand("/e3varset", (x) =>
			{
				//key/value
				if (x.args.Count > 1)
				{
					string key = x.args[0];
					string value = x.args[1];
					if (VarsetValues.Count > 0)
					{
						foreach (var vkey in VarsetValues.Keys)
						{
							if (value.IndexOf($"({vkey})", 0, StringComparison.OrdinalIgnoreCase) > -1)
							{

								value = value.ReplaceInsensitive($"({vkey})", $"({VarsetValues[vkey]})");
							}
						}
					}
					if (!VarsetValues.ContainsKey(key))
					{
						VarsetValues.Add(key, value);
					}
					else
					{
						VarsetValues[key] = value;
					}
				}
			});
			EventProcessor.RegisterCommand("/e3varbool", (x) =>
			{
				//key/value
				if (x.args.Count > 1)
				{
					string key = x.args[0];
					string value = x.args[1];

					if (VarsetValues.Count > 0)
					{
						foreach (var vkey in VarsetValues.Keys)
						{
							if (value.IndexOf($"({vkey})", 0, StringComparison.OrdinalIgnoreCase) > -1)
							{

								value = value.ReplaceInsensitive($"({vkey})", $"({VarsetValues[vkey]})");
							}
						}
					}
					value = Ifs(value).ToString();


					if (!VarsetValues.ContainsKey(key))
					{
						VarsetValues.Add(key, value);
					}
					else
					{
						VarsetValues[key] = value;
					}
				}
			});
			EventProcessor.RegisterCommand("/e3varcalc", (x) =>
			{
				//key/value
				if (x.args.Count > 1)
				{
					string key = x.args[0];
					string value = x.args[1];
					if (VarsetValues.Count > 0)
					{
						foreach (var vkey in VarsetValues.Keys)
						{
							if (value.IndexOf($"({vkey})", 0, StringComparison.OrdinalIgnoreCase) > -1)
							{

								value = value.ReplaceInsensitive($"({vkey})", $"({VarsetValues[vkey]})");
							}
						}
					}
					value = Ifs_Results(value);
					value = MQ.Query<double>($"${{Math.Calc[{value}]}}").ToString();

					if (!VarsetValues.ContainsKey(key))
					{
						VarsetValues.Add(key, value);
					}
					else
					{
						VarsetValues[key] = value;
					}
				}
			});
			EventProcessor.RegisterCommand("/e3varclear", (x) =>
			{
				//key
				if (x.args.Count > 0)
				{
					string key = x.args[0];
					if (key == "all")
					{
						VarsetValues.Clear();
					}
					else
					{
						VarsetValues.Remove(key);
					}
				}
			});
			EventProcessor.RegisterCommand("/e3varlist", (x) =>
			{
				if (VarsetValues.Count == 0)
				{
					E3.Bots.Broadcast("No vars set.");
				}
				foreach (var pair in VarsetValues)
				{
					E3.Bots.Broadcast($"{pair.Key} = {pair.Value}");
				}
			});
			EventProcessor.RegisterCommand("/e3varvalue", (x) =>
			{

				if (x.args.Count == 1)
				{
					if (VarsetValues.ContainsKey(x.args[0]))
					{
						E3.MQ.Cmd($"/varset E3N_var {VarsetValues[x.args[0]]}");
						return;
					}
				}

				E3.MQ.Cmd("/varset E3N_var NULL");

			});
		}
		public static bool Ifs(Data.Spell spell)
		{
			return Ifs(spell.Ifs);
		}
		private static StringBuilder _ifsStringBuilder = new StringBuilder(); 
		public static bool Ifs(string IfsExpression)
		{
			if (!String.IsNullOrWhiteSpace(IfsExpression))
			{
				string tIF = Ifs_Results(IfsExpression);
				return MQ.Query<bool>($"${{If[{tIF},TRUE,FALSE]}}");
			}
			return true;
		}

		public static string Ifs_Results(string IfsExpression)
		{
			string tIF = IfsExpression;

			if (VarsetValues.Count > 0)
			{
				foreach (var key in VarsetValues.Keys)
				{
					if (tIF.IndexOf($"${{{key}}}", 0, StringComparison.OrdinalIgnoreCase) > -1)
					{

						tIF = tIF.ReplaceInsensitive($"${{{key}}}", VarsetValues[key]);
					}
				}
			}
			var parsedData = E3.CharacterSettings.ParsedData;
			var section = parsedData.Sections["Ifs"];
			if (section != null)
			{
				foreach (var key in section)
				{
					if (tIF.IndexOf(key.KeyName, 0, StringComparison.OrdinalIgnoreCase) > -1)
					{
						var tkeyName = $"${{{key.KeyName}}}";

						if (tIF.IndexOf(tkeyName, 0, StringComparison.OrdinalIgnoreCase) > -1)
						{
							tIF = tIF.ReplaceInsensitive(tkeyName, key.Value);
						}
					}
				}
			}
			//we need to do this again in case the ifs drags in new values
			if (VarsetValues.Count > 0)
			{
				foreach (var key in VarsetValues.Keys)
				{
					if (tIF.IndexOf($"${{{key}}}", 0, StringComparison.OrdinalIgnoreCase) > -1)
					{

						tIF = tIF.ReplaceInsensitive($"${{{key}}}", VarsetValues[key]);
					}
				}
			}

			//to deal with an issue of ( and [ in the parser
			if (tIF.Contains(@"\["))
			{
				//settings shouldn't have [, if we do they should be ( or ) instead
				tIF = tIF.Replace(@"\[", "(").Replace(@"\]", ")");
			}

			//dynamic lookup via reflection
			//${E3N.Settings.Header.Key}
			if (tIF.IndexOf("${E3N.Settings",0,StringComparison.OrdinalIgnoreCase)>-1)
			{

				foreach (var pair in E3.CharacterSettings.SettingsReflectionLookup)
				{
					if (tIF.IndexOf(pair.Key, 0, StringComparison.OrdinalIgnoreCase) > -1)
					{
						var field = pair.Value;
						if (field.IsGenericList(typeof(String)))
						{

							List<string> fieldValue = (List<string>)field.GetValue(E3.CharacterSettings);
							string finallist = string.Join(",", fieldValue);
							tIF = tIF.ReplaceInsensitive(pair.Key, finallist);
						}
						else if (field.IsGenericList(typeof(Int32)))
						{
							List<Int32> fieldValue = (List<Int32>)field.GetValue(E3.CharacterSettings);
							string finallist = string.Join(",", fieldValue);
							tIF = tIF.ReplaceInsensitive(pair.Key, finallist);
						}
						else if (field.IsGenericList(typeof(Spell)))
						{
							List<Spell> fieldValue = (List<Spell>)field.GetValue(E3.CharacterSettings);
							_ifsStringBuilder.Clear();
							foreach (var spell in fieldValue)
							{
								if(_ifsStringBuilder.Length==0)
								{
									_ifsStringBuilder.Append(spell.CastName);
								}
								else
								{
									_ifsStringBuilder.Append(","+spell.CastName);
								}
							}
							tIF = tIF.ReplaceInsensitive(pair.Key, _ifsStringBuilder.ToString());
						}
						else if (field.IsGenericList(typeof(Int64)))
						{
							List<Int64> fieldValue = (List<Int64>)field.GetValue(E3.CharacterSettings);
							string finallist = string.Join(",", fieldValue);
							tIF = tIF.ReplaceInsensitive(pair.Key, finallist);
						}
						else
						{
							tIF = tIF.ReplaceInsensitive(pair.Key, pair.Value.GetValue(E3.CharacterSettings).ToString());

						}
						
					}
				}
			}
			if (tIF.IndexOf("${E3N.State.Bots.", 0, StringComparison.OrdinalIgnoreCase) > -1)
			{

				foreach (var pair in Setup.ExposedDataReflectionLookup)
				{
					if (tIF.IndexOf(pair.Key, 0, StringComparison.OrdinalIgnoreCase) > -1)
					{
						var field = pair.Value;
						if (field.IsGenericList(typeof(String)))
						{

							List<string> fieldValue = (List<string>)field.GetValue((SharedDataBots)E3.Bots);
							string finallist = string.Join(",", fieldValue);
							tIF = tIF.ReplaceInsensitive(pair.Key, finallist);
						}
						else if (field.IsGenericList(typeof(Int32)))
						{
							List<Int32> fieldValue = (List<Int32>)field.GetValue(E3.Bots);
							string finallist = string.Join(",", fieldValue);
							tIF = tIF.ReplaceInsensitive(pair.Key, finallist);
						}
						else if (field.IsGenericList(typeof(Spell)))
						{
							List<Spell> fieldValue = (List<Spell>)field.GetValue(E3.Bots);
							_ifsStringBuilder.Clear();
							foreach (var spell in fieldValue)
							{
								if (_ifsStringBuilder.Length == 0)
								{
									_ifsStringBuilder.Append(spell.CastName);
								}
								else
								{
									_ifsStringBuilder.Append("," + spell.CastName);
								}
							}
							tIF = tIF.ReplaceInsensitive(pair.Key, _ifsStringBuilder.ToString());
						}
						else if (field.IsGenericList(typeof(Int64)))
						{
							List<Int64> fieldValue = (List<Int64>)field.GetValue(E3.Bots);
							string finallist = string.Join(",", fieldValue);
							tIF = tIF.ReplaceInsensitive(pair.Key, finallist);
						}
						else
						{
							tIF = tIF.ReplaceInsensitive(pair.Key, pair.Value.GetValue(E3.Bots).ToString());

						}

					}
				}
			}
			else if (tIF.IndexOf("${E3N.State", 0, StringComparison.OrdinalIgnoreCase) > -1)
			{
				foreach (var pair in Setup.ExposedDataReflectionLookup)
				{
					var field = pair.Value;
					//if(field.IsStatic)
					{
						if (tIF.IndexOf(pair.Key, 0, StringComparison.OrdinalIgnoreCase) > -1)
						{
							//we are pulling static data, so pass a null to get it. 
							if (field.IsGenericList(typeof(String)))
							{
								List<string> fieldValue = (List<string>)field.GetValue(null);
								string finallist = string.Join(",", fieldValue);
								tIF = tIF.ReplaceInsensitive(pair.Key, finallist);
							}
							else if (field.IsGenericList(typeof(Spell)))
							{
								List<Spell> fieldValue = (List<Spell>)field.GetValue(null);
								_ifsStringBuilder.Clear();
								foreach (var spell in fieldValue)
								{
									if (_ifsStringBuilder.Length == 0)
									{
										_ifsStringBuilder.Append(spell.CastName);
									}
									else
									{
										_ifsStringBuilder.Append("," + spell.CastName);
									}
								}
								tIF = tIF.ReplaceInsensitive(pair.Key, _ifsStringBuilder.ToString());
							}
							else if (field.IsGenericList(typeof(Int32)))
							{
								List<Int32> fieldValue = (List<Int32>)field.GetValue(null);
								string finallist = string.Join(",", fieldValue);
								tIF = tIF.ReplaceInsensitive(pair.Key, finallist);
							}
							else if (field.IsGenericList(typeof(Int64)))
							{
								List<Int64> fieldValue = (List<Int64>)field.GetValue(null);
								string finallist = string.Join(",", fieldValue);
								tIF = tIF.ReplaceInsensitive(pair.Key, finallist);
							}
							else if (field.IsGenericDictonary(typeof(string), typeof(Burn)))
							{
								//{E3N.State.Burn.Key}}
								//get the last section of the 
								string[] keylookupArray = pair.Key.Split('.');
								string keytoUse = keylookupArray[3].Replace("}", "");

								Dictionary<string, Burn> fieldValue = (Dictionary<string, Burn>)field.GetValue(E3.CharacterSettings);
								if (fieldValue.TryGetValue(keytoUse, out var tburn))
								{
									tIF = tIF.ReplaceInsensitive(pair.Key, tburn.Active.ToString());
								}

							}
							else
							{
								tIF = tIF.ReplaceInsensitive(pair.Key, field.GetValue(null).ToString());

							}
						}
					}
				}
			}

			//need to do some legacy compatability checksraibles that were used in Ifs.
			if (tIF.IndexOf("${Assisting}", 0, StringComparison.OrdinalIgnoreCase) > -1)
			{
				//lets replace it with TRUE/FALSE
				tIF = tIF.ReplaceInsensitive("${Assisting}", Assist.IsAssisting.ToString());
			}

			Ifs_E3Bots(ref tIF);

			//need to do some legacy compatability checksraibles that were used in Ifs.
			if (tIF.IndexOf("${PBAEON}", 0, StringComparison.OrdinalIgnoreCase) > -1)
			{
				//lets replace it with TRUE/FALSE
				tIF = tIF.ReplaceInsensitive("${PBAEON}", Nukes.PBAEEnabled.ToString());
			}
			//if (tIF.IndexOf("${E3N.State.ClearTargets}", 0, StringComparison.OrdinalIgnoreCase) > -1)
			//{
			//	//lets replace it with TRUE/FALSE
			//	tIF = tIF.ReplaceInsensitive("${E3N.State.ClearTargets}", ClearXTargets.Enabled.ToString());
			//}
			//if (tIF.IndexOf("${E3N.State.IsLootOn}", 0, StringComparison.OrdinalIgnoreCase) > -1)
			//{
			//	//lets replace it with TRUE/FALSE
			//	tIF = tIF.ReplaceInsensitive("${E3N.State.IsLootOn}", E3.CharacterSettings.Misc_AutoLootEnabled.ToString());
			//}
			if (tIF.IndexOf("${AssistTarget}", 0, StringComparison.OrdinalIgnoreCase) > -1)
			{
				//lets replace it with TRUE/FALSE
				tIF = tIF.ReplaceInsensitive("${AssistTarget}", Assist.AssistTargetID.ToString());
			}
			if (tIF.IndexOf("${AssistType}", 0, StringComparison.OrdinalIgnoreCase) > -1)
			{
				tIF = tIF.ReplaceInsensitive("${AssistType}", E3.CharacterSettings.Assist_Type);
			}
			if (tIF.IndexOf("${use_QUICKBurns}", 0, StringComparison.OrdinalIgnoreCase) > -1)
			{
				if(E3.CharacterSettings.BurnCollection.TryGetValue("Quick Burn", out var burn))
				{
					tIF = tIF.ReplaceInsensitive("${use_QUICKBurns}", burn.Active.ToString());
				}
				else
				{
					tIF = tIF.ReplaceInsensitive("${use_QUICKBurns}","False");
				}
			}
			if (tIF.IndexOf("${use_LONGBurns}", 0, StringComparison.OrdinalIgnoreCase) > -1)
			{
				if (E3.CharacterSettings.BurnCollection.TryGetValue("Long Burn", out var burn))
				{
					tIF = tIF.ReplaceInsensitive("${use_LONGBurns}", burn.Active.ToString());
				}
				else
				{
					tIF = tIF.ReplaceInsensitive("${use_LONGBurns}", "False");
				}
			}
			if (tIF.IndexOf("${use_FULLBurns}", 0, StringComparison.OrdinalIgnoreCase) > -1)
			{
				if (E3.CharacterSettings.BurnCollection.TryGetValue("Full Burn", out var burn))
				{
					tIF = tIF.ReplaceInsensitive("${use_FULLBurns}", burn.Active.ToString());
				}
				else
				{
					tIF = tIF.ReplaceInsensitive("${use_FULLBurns}", "False");
				}
			}
			if (tIF.IndexOf("${use_EPICBurns}", 0, StringComparison.OrdinalIgnoreCase) > -1)
			{
				if (E3.CharacterSettings.BurnCollection.TryGetValue("Epic", out var burn))
				{
					tIF = tIF.ReplaceInsensitive("${use_EPICBurns}", burn.Active.ToString());
				}
				else
				{
					tIF = tIF.ReplaceInsensitive("${use_EPICBurns}", "False");
				}
				
			}
			if (tIF.IndexOf("${use_Swarms}", 0, StringComparison.OrdinalIgnoreCase) > -1)
			{
				if (E3.CharacterSettings.BurnCollection.TryGetValue("Swarm", out var burn))
				{
					tIF = tIF.ReplaceInsensitive("${use_Swarms}", burn.Active.ToString());
				}
				else
				{
					tIF = tIF.ReplaceInsensitive("${use_Swarms}", "False");
				}
			}
			if (tIF.IndexOf("${charmTarget}", 0, StringComparison.OrdinalIgnoreCase) > -1)
			{
				//lets replace it with TRUE/FALSE
				tIF = tIF.ReplaceInsensitive("${charmTarget}", "false");
			}
			if (tIF.IndexOf("${NotCombat}", 0, StringComparison.OrdinalIgnoreCase) > -1)
			{
				//lets replace it with TRUE/FALSE
				tIF = tIF.ReplaceInsensitive("${NotCombat}", (!Basics.InCombat()).ToString());
			}
			if (tIF.IndexOf("${InCombat}", 0, StringComparison.OrdinalIgnoreCase) > -1)
			{
				//lets replace it with TRUE/FALSE
				tIF = tIF.ReplaceInsensitive("${InCombat}", (Basics.InCombat()).ToString());
			}
			if (tIF.IndexOf("${StandingStillForTimePeriod}", 0, StringComparison.OrdinalIgnoreCase) > -1)
			{
				//lets replace it with TRUE/FALSE
				tIF = tIF.ReplaceInsensitive("${StandingStillForTimePeriod}", (Movement.StandingStillForTimePeriod()).ToString());
			}
			if (tIF.IndexOf("${NotStandingStillForTimePeriod}", 0, StringComparison.OrdinalIgnoreCase) > -1)
			{
				//lets replace it with TRUE/FALSE
				tIF = tIF.ReplaceInsensitive("${NotStandingStillForTimePeriod}", (!Movement.StandingStillForTimePeriod()).ToString());
			}
			if (tIF.IndexOf("${IsSafeZone}", 0, StringComparison.OrdinalIgnoreCase) > -1)
			{
				//lets replace it with TRUE/FALSE
				tIF = tIF.ReplaceInsensitive("${IsSafeZone}", (Zoning.CurrentZone.IsSafeZone).ToString());
			}
			if (tIF.IndexOf("${IsNotSafeZone}", 0, StringComparison.OrdinalIgnoreCase) > -1)
			{
				//lets replace it with TRUE/FALSE
				tIF = tIF.ReplaceInsensitive("${IsNotSafeZone}", (!Zoning.CurrentZone.IsSafeZone).ToString());
			}
			//StandingStillForTimePeriod()

			return tIF;
		}

		static Regex _e3buffexistsRegEx = new Regex(@"\$\{E3BuffExists\[([A-Za-z0-9 _]+),([A-Za-z0-9 _]+)\]\}", RegexOptions.Compiled);
		static Regex _e3BotsRegEx = new Regex(@"\$\{E3Bots\[([A-Za-z0-9 _]+)\]\.([A-Za-z0-9 _]+)\}", RegexOptions.Compiled);
		static Regex _e3BotsBuffsRegEx = new Regex(@"\$\{E3Bots\[([A-Za-z0-9 _]+)\]\.Buffs\[([A-Za-z0-9 _]+)\]\.([A-Za-z0-9]+)\}", RegexOptions.Compiled);
		static Regex _e3BotsQuery = new Regex(@"\$\{E3Bots\[([A-Za-z0-9 _]+)\]\.Query\[([A-Za-z0-9 _]+)\]\}", RegexOptions.Compiled);
		//
		//to replace the NetBots functionality of query data in the ini files
		//a bit of regex hell while trying to be somewhat efficent

		
		public static void Ifs_E3Bots(ref string tIF)
		{
			
			
			//do we need to run ANY of the E3Bots regex?, quick n dirty check
			if (tIF.IndexOf("${E3Bots[", 0, StringComparison.OrdinalIgnoreCase) > -1)
			{
				string replaceValue = "";
				MatchCollection matches;
				//do we need to run any of the query regexes?, quick n dirty check
				if (tIF.IndexOf(".Query[", 0, StringComparison.OrdinalIgnoreCase) > -1)
				{
					replaceValue = "";
					////\$\{E3Bots\[([A-Za-z0-9 _]+)\]\.Query\([A-Za-z]+)\]\}
					//${E3Bots[Rekken].Query[SomeKeyValue]}
					// gruop0: ${E3Bots[Rekken].Query[SomeKeyValue]}
					// group1: Rekken
					// group2: SomeKeyValue
					matches = _e3BotsQuery.Matches(tIF);
					foreach (Match match in matches)
					{
						if (match.Success && match.Groups.Count > 0)
						{
							//${E3Bots[Rekken].Query[SomeKeyValue]}
							string replaceString = match.Groups[0].Value;
							string replacevalue = replaceString;
							//Rekken
							string targetname = match.Groups[1].Value;
							//SomeKeyValue
							string keyValue = match.Groups[2].Value;
							keyValue = "${Data." + keyValue + "}"; //data format for custom keys
							replaceValue = "";
							string result = E3.Bots.Query(targetname, keyValue);
							if (result != "NULL")
							{
								replaceValue = result;
							}


							//check to see if some modification was done
							if (replaceString != replaceValue)
							{
								tIF = tIF.ReplaceInsensitive(replaceString, replaceValue);
							}
						}

					}
				}
				//do we need to run any of the buff regexes? quick n dirty check
				if (tIF.IndexOf(".Buffs[", 0, StringComparison.OrdinalIgnoreCase) > -1)
				{
					matches = _e3BotsBuffsRegEx.Matches(tIF);

					foreach (Match match in matches)
					{
						if (match.Success && match.Groups.Count > 0)
						{
							//${E3Bots[Rekken].Buffs[Hand of Conviction].ID}
							//${E3Bots[Rekken].Buffs[Hand of Conviction].Duration}
							string replaceString = match.Groups[0].Value;
							replaceValue = replaceString;
							//Rekken
							string targetname = match.Groups[1].Value;
							//buffname
							string buffName = match.Groups[2].Value;
							//ID,Duration,etc
							string query = match.Groups[3].Value;
							if (query == "ID")
							{
								replaceValue = "0";
								List<Int32> buffList = E3.Bots.BuffList(targetname);
								Int32 spellID = Spell.SpellIDLookup(buffName);
								if (spellID > 0)
								{
									if (buffList.Contains(spellID))
									{
										replaceValue = spellID.ToString();
									}
								}
							}
							if (query == "Duration")
							{
								replaceValue = "0";
								CharacterBuffs buffInfo = E3.Bots.GetBuffInformation(targetname);

								if (buffInfo != null)
								{
									Int32 spellID = Spell.SpellIDLookup(buffName);
									if (buffInfo.BuffDurations.TryGetValue(spellID, out var buffDuration))
									{
										replaceValue = buffDuration.ToString();
									}
								}
							}
							//check to see if some modification was done
							if (replaceString != replaceValue)
							{
								tIF = tIF.ReplaceInsensitive(replaceString, replaceValue);
							}
						}
					}
				}
			
				//time for the rest of the regexs , the above should have already done their work.
				replaceValue = "";
				////\$\{E3Bots\[([A-Za-z0-9 _]+)\]\.([A-Za-z]+)\}
				//${E3Bots[Rekken].Hps} && ${E3Bots[Rekken].Hps}
				// gruop0: ${E3Bots[Rekken].Hps}
				// group1: Rekken
				// group2: Hps
				matches = _e3BotsRegEx.Matches(tIF);
				foreach (Match match in matches)
				{
					if (match.Success && match.Groups.Count > 0)
					{
						//${E3Bots[Rekken].CurrentHPs} 
						string replaceString = match.Groups[0].Value;
						string replacevalue = replaceString;
						//Rekken
						string targetname = match.Groups[1].Value;

						if (String.Equals("LOCAL_NAME",targetname,StringComparison.OrdinalIgnoreCase))
						{
							targetname = E3.CurrentName;
						}

						//CurrentHps
						string query = match.Groups[2].Value;

						replaceValue = "0";
						if (query == "PctHPs"|| query == "PctMana" || query == "PctEndurance")
						{
							replaceValue = "100";
						}
						//string startTime = E3.Bots.Query(user, "${Me.Memory_CSharpStartTime}");
						string result = E3.Bots.Query(targetname, $"${{Me.{query}}}");
						if (result != "NULL")
						{
							replaceValue = result;
						}
						//check to see if some modification was done
						if (replaceString != replaceValue)
						{
							tIF = tIF.ReplaceInsensitive(replaceString, replaceValue);
						}

					}
				}
				
				
			}
			else if (tIF.IndexOf("${E3Bots.ConnectedClients}", 0, StringComparison.OrdinalIgnoreCase) > -1)
			{

				tIF = tIF.ReplaceInsensitive("${E3Bots.ConnectedClients}", String.Join(",", E3.Bots.BotsConnected()));

			}
			else if (tIF.IndexOf("${E3Bots.ConnectedClientsCount}", 0, StringComparison.OrdinalIgnoreCase) > -1)
			{

				tIF = tIF.ReplaceInsensitive("${E3Bots.ConnectedClientsCount}", E3.Bots.BotsConnected().Count.ToString());

			}
			else if (tIF.IndexOf("${E3BuffExists[", 0, StringComparison.OrdinalIgnoreCase) > -1)
			{
				//need to do some legacy compatability checksraibles that were used in Ifs.
				bool replaceValue = false;
				//time for some regex

				var matchs = _e3buffexistsRegEx.Matches(tIF);

				foreach (Match match in matchs)
				{
					if (match.Success && match.Groups.Count > 0)
					{
						string replaceString = match.Groups[0].Value;
						string buffname = match.Groups[1].Value;
						string targetname = match.Groups[2].Value;

						List<Int32> buffList = E3.Bots.BuffList(targetname);
						Int32 spellID = MQ.Query<Int32>($"${{Spell[{buffname}].ID}}");
						if (spellID > 0)
						{
							if (buffList.Contains(spellID))
							{
								replaceValue = true;
							}
						}
						tIF = tIF.ReplaceInsensitive(replaceString, replaceValue.ToString());
					}
				}

			}
		}
		public static bool TrueTarget(Int32 targetID, bool allowClear = false)
		{
			//0 means don't change target
			if (allowClear && targetID == 0)
			{
				MQ.Cmd("/squelch /target clear");
				return true;
			}
			else
			{
				if (targetID == 0) return false;

			}

			_log.Write("Trying to Aquire true target on :" + targetID);

			if (MQ.Query<Int32>("${Target.ID}") == targetID) return true;

			//now to get the target
			if (MQ.Query<Int32>($"${{SpawnCount[id {targetID}]}}") > 0)
			{
				//try 3 times
				for (Int32 i = 0; i < 3; i++)
				{
					MQ.Cmd($"/target id {targetID}");
					MQ.Delay(300, $"${{Target.ID}}=={targetID}");
					//swapping targets turn off autofire
					if (MQ.Query<bool>("${Me.AutoFire}"))
					{
						MQ.Cmd("/autofire");
						//delay is needed to give time for it to actually process
						MQ.Delay(1000);
					}
					if (MQ.Query<Int32>("${Target.ID}") == targetID)
					{
						return true;

					}
					e3util.YieldToEQ();
				}
				return false;
			}
			else
			{
				if (allowClear)
				{
					MQ.Cmd("/squelch /target clear");
					return false;
				}
				//MQ.Write("TrueTarget has no spawncount");
				return false;
			}

		}

		public static void ResetResistCounters()
		{
			//put them back in their object pools
			foreach (var kvp in Casting.ResistCounters)
			{
				kvp.Value.Dispose();
			}
			ResistCounters.Clear();

		}
		[SubSystemInit]
		public static void Casting_Init()
		{
			RegisterEventsCasting();
			RegisterEventsCastResults();
			RefreshGemCache();
		}

		public static void RefreshGemCache()
		{
			if ((E3.CurrentClass & Class.PureMelee) == E3.CurrentClass)
			{
				//class doesn't have spells
				return;
			}

			if (Core.StopWatch.ElapsedMilliseconds < _currentSpellGemsLastRefresh)
			{
				return;
			}
			_currentSpellGemsLastRefresh = Core.StopWatch.ElapsedMilliseconds + 2000;
			//need to get all the spellgems setup

			for (int i = 1; i < 13; i++)
			{
				Int32 spellID = MQ.Query<Int32>($"${{Me.Gem[{i}].ID}}");

				string spellName = MQ.Query<string>($"${{Me.Gem[{i}]}}");
				if (!_currentSpellGems.ContainsKey(i))
				{
					_currentSpellGems.Add(i, spellID);
				}
				_currentSpellGems[i] = spellID;

			}
		}
		static void RegisterEventsCasting()
		{


		}
		public static void ClearInterruptChecks()
		{
			Double endtime = 0;
			CheckForResistByName("CAST_INTERRUPTED", endtime);
		}
		public static void ClearResistChecks()
		{
			MQ.Delay(100);
			Double endtime = 0;
			CheckForResistByName("CAST_TAKEHOLD", endtime);
			CheckForResistByName("CAST_RESIST", endtime);
			CheckForResistByName("CAST_FIZZLE", endtime);
			CheckForResistByName("CAST_IMMUNE", endtime);
			CheckForResistByName("CAST_INTERRUPTED", endtime);
		}
		public static CastReturn CheckForReist(Data.Spell spell, bool isNowCast)
		{
			//it takes time to wait for a spell resist, up to 2-400 millieconds.
			//basically 0 or is non detrimental to not resist, mostly nukes/spells
			//what this buys us is a much faster nuke/heal cycle, at the expense of checking for their resist status
			//tho debuffs/dots/buffs its more important as we have to keep track of timers, so we will pay the cost of waiting for resist checks.
			//always check for resists/whatever if its a nowcast.
			if (spell.Duration == 0 && !isNowCast)
			{
				return CastReturn.CAST_SUCCESS;
			}

			Double endtime = Core.StopWatch.Elapsed.TotalMilliseconds + 500;
			while (endtime > Core.StopWatch.Elapsed.TotalMilliseconds)
			{

				//string result = MQ.Query<string>("${Cast.Result}");

				//if (result != "CAST_SUCCESS")
				//{
				//    CastReturn r = CastReturn.CAST_INTERRUPTED;
				//    Enum.TryParse<CastReturn>(result, out r);
				//    return r;
				//}
				//frankly sometimes mq2cast is bad about getting events. do it ourselves as well
				if (CheckForResistByName("CAST_TAKEHOLD", endtime)) return CastReturn.CAST_TAKEHOLD;
				if (CheckForResistByName("CAST_RESIST", endtime)) return CastReturn.CAST_RESIST;
				if (CheckForResistByName("CAST_FIZZLE", endtime)) return CastReturn.CAST_FIZZLE;
				if (CheckForResistByName("CAST_IMMUNE", endtime)) return CastReturn.CAST_IMMUNE;
				//if (CheckForResistByName("CAST_COLLAPSE", endtime)) return CastReturn.CAST_COLLAPSE;
				//if (CheckForResistByName("CAST_CANNOTSEE", endtime)) return CastReturn.CAST_NOTARGET;
				//if (CheckForResistByName("CAST_COMPONENTS", endtime)) return CastReturn.CAST_COMPONENTS;
				//if (CheckForResistByName("CAST_DISTRACTED", endtime)) return CastReturn.CAST_DISTRACTED;
				if (CheckForResistByName("CAST_INTERRUPTED", endtime)) return CastReturn.CAST_INTERRUPTED;
				//if (CheckForResistByName("CAST_NOTARGET", endtime)) return CastReturn.CAST_NOTARGET;
				//if (CheckForResistByName("CAST_OUTDOORS", endtime)) return CastReturn.CAST_DISTRACTED;
				MQ.Delay(100);

			}
			//assume success at this point.
			return CastReturn.CAST_SUCCESS;
		}
		public static Int64 TimeLeftOnMySpell(Data.Spell spell)
		{

			for (Int32 i = 1; i < (e3util.MobMaxDebuffSlots+1); i++)
			{
				Int32 buffID = MQ.Query<Int32>($"${{Target.Buff[{i}].ID}}");

				if (spell.SpellID == buffID)
				{
					//check if its mine
					string casterName = MQ.Query<string>($"${{Target.Buff[{i}].Caster}}");
					if (E3.CurrentName == casterName)
					{
						//its my spell!
						Int64 millisecondsLeft = MQ.Query<Int64>($"${{Target.BuffDuration[{i}]}}");
						return millisecondsLeft;
					}
				}
			}
			return 0;
		}
		public static Int64 TimeLeftOnTargetBuff(Data.Spell spell)
		{
			Int64 millisecondsLeft = MQ.Query<Int64>($"${{Target.Buff[{spell.SpellName}].Duration}}");

			if (millisecondsLeft == 0)
			{
				bool spellExists = MQ.Query<bool>($"${{Spell[{spell.SpellName}]}}");
				//doing this as -1 is a default 'bad' value for NULL, but in here a neg duration means perma.
				if (spellExists)
				{
					//check to see if its a perm buff
					Int32 duration = MQ.Query<Int32>($"${{Spell[{spell.SpellName}].Duration}}");
					if (duration < 0)
					{
						millisecondsLeft = Int32.MaxValue;
					}
				}
			}
			return millisecondsLeft;
		}
		public static Int64 TimeLeftOnMyPetBuff(Data.Spell spell)
		{

			Int64 millisecondsLeft = 0;
			int buffIndex = MQ.Query<Int32>($"${{Me.Pet.Buff[{spell.SpellName}]}}");


			if (buffIndex > 0)
			{
				millisecondsLeft = MQ.Query<Int64>($"${{Me.Pet.Buff[{buffIndex}].Duration}}");
				if(millisecondsLeft<0)
				{
					//perma buff?
					millisecondsLeft = Int32.MaxValue;
				}
				//if (millisecondsLeft == 0)
				//{
				//	//check if perma spell
				//	Int32 duration = MQ.Query<Int32>($"${{Spell[{spell.SpellName}].Duration}}");
				//	if (duration < 0)
				//	{
				//		millisecondsLeft = Int32.MaxValue;
				//	}
				//}
			}
			return millisecondsLeft;
		}
		public static Int64 TimeLeftOnMyBuff(Data.Spell spell)
		{
			Int64 millisecondsLeft = 0;
			bool buffExists = MQ.Query<bool>($"${{Me.Buff[{spell.SpellName}]}}");
			if (buffExists)
			{
				millisecondsLeft = MQ.Query<Int64>($"${{Me.Buff[{spell.SpellName}].Duration}}");
				if (millisecondsLeft == 0)
				{
					//check if perma spell
					Int32 duration = MQ.Query<Int32>($"${{Spell[{spell.SpellName}].Duration}}");
					if (duration < 0)
					{
						millisecondsLeft = Int32.MaxValue;
					}
				}
			}
			else
			{
				buffExists = MQ.Query<bool>($"${{Me.Song[{spell.SpellName}]}}");
				if (buffExists)
				{
					millisecondsLeft = MQ.Query<Int64>($"${{Me.Song[{spell.SpellName}].Duration}}");
					if (millisecondsLeft == 0)
					{
						//check if perma spell
						Int32 duration = MQ.Query<Int32>($"${{Spell[{spell.SpellName}].Duration}}");
						if (duration < 0)
						{
							millisecondsLeft = Int32.MaxValue;
						}
					}
				}

			}

			return millisecondsLeft;
		}
		private static bool CheckForResistByName(string name, Double time)
		{
			if (EventProcessor.EventList[name].queuedEvents.Count > 0)
			{
				while (EventProcessor.EventList[name].queuedEvents.Count > 0)
				{
					EventProcessor.EventMatch e;
					EventProcessor.EventList[name].queuedEvents.TryDequeue(out e);
				}

				return true;
			}
			return false;
		}
		static void RegisterEventsCastResults()
		{
			List<String> r = new List<string>();
			//r.Add("Your gate is too unstable, and collapses.*");
			//EventProcessor.RegisterEvent("CAST_COLLAPSE", r, (x) => {
			//    //not doing anything, casting code will remove this from the collection if it detects
			//    //so this will never be called.
			//});

			//r = new List<string>();
			//r.Add("You cannot see your target.");
			//EventProcessor.RegisterEvent("CAST_CANNOTSEE", r, (x) => {
			//});

			//r = new List<string>();
			//r.Add("You need to play a.+ for this song.");
			//EventProcessor.RegisterEvent("CAST_COMPONENTS", r, (x) => {
			//});

			//r = new List<string>();
			//r.Add("You are too distracted to cast a spell now.");
			//r.Add("You can't cast spells while invulnerable.");
			//r.Add("You *CANNOT* cast spells, you have been silenced.");
			//EventProcessor.RegisterEvent("CAST_DISTRACTED", r, (x) => {
			//});

			r = new List<string>();
			r.Add("Your target has no mana to affect.");
			r.Add("Your target looks unaffected.");
			r.Add("Your target is immune to changes in its attack speed.");
			r.Add("Your target is immune to changes in its run speed.");
			r.Add("Your target is immune to snare spells.");
			r.Add("Your target cannot be mesmerized.");
			r.Add("Your target looks unaffected.");
			EventProcessor.RegisterEvent("CAST_IMMUNE", r, (x) =>
			{
			});


			r = new List<string>();
			r.Add("Your .+ is interrupted.");
			r.Add("Your spell is interrupted.");
			r.Add("Your casting has been interrupted.");
			EventProcessor.RegisterEvent("CAST_INTERRUPTED", r, (x) =>
			{
			});

			r = new List<string>();
			r.Add("Your spell fizzles.");
			r.Add("Your .+ spell fizzles.");
			r.Add(@"You miss a note, bringing your song to a close\.");
			EventProcessor.RegisterEvent("CAST_FIZZLE", r, (x) =>
			{
			});

			//r = new List<string>();
			//r.Add("You must first select a target for this spell.");
			//r.Add("This spell only works on.*");
			//r.Add("You must first target a group member.");
			//EventProcessor.RegisterEvent("CAST_NOTARGET", r, (x) => {
			//});

			//r = new List<string>();
			//r.Add("This spell does not work here.");
			//r.Add("You can only cast this spell in the outdoors.");
			//r.Add("You can not summon a mount here.");
			//r.Add("You must have both the Horse Models and your current Luclin Character Model enabled to summon a mount.");
			//EventProcessor.RegisterEvent("CAST_OUTDOORS", r, (x) => {
			//});

			r = new List<string>();
			r.Add(@"Your target resisted the .+ spell\.");
			//TODO deal with live vs non live
			//r.Add(".+ resisted your .+\!"); //for live?
			//r.Add(".+ avoided your .+!"); //for live?
			EventProcessor.RegisterEvent("CAST_RESIST", r, (x) =>
			{
			});

			//r = new List<string>();
			//r.Add("You can't cast spells while stunned.");
			////TODO deal with live vs non live
			////r.Add(".+ resisted your .+\!"); //for live?
			////r.Add(".+ avoided your .+!"); //for live?
			//EventProcessor.RegisterEvent("CAST_STUNNED", r, (x) => {
			//});

			//r = new List<string>();
			//r.Add("You can't cast spells while stunned.");
			////TODO deal with live vs non live
			////r.Add(".+ resisted your .+\!"); //for live?
			////r.Add(".+ avoided your .+!"); //for live?
			//EventProcessor.RegisterEvent("CAST_STUNNED", r, (x) => {
			//});

			r = new List<string>();
			r.Add(@" spell did not take hold. \(Blocked by");
			r.Add(@" did not take hold on .+ \(Blocked by");
			r.Add(@"Your spell did not take hold\.");
			r.Add(@"Your spell would not have taken hold\.");
			r.Add(@"Your spell is too powerful for your intended target\.");
			EventProcessor.RegisterEvent("CAST_TAKEHOLD", r, (x) =>
			{
			});


		}
	}


	/*
    | CAST_CANCELLED       | Spell was cancelled by ducking (either manually or because mob died) |
    | CAST_CANNOTSEE       | You can't see your target                                            |
    | CAST_IMMUNE          | Target is immune to this spell                                       |
    | CAST_INTERRUPTED     | Casting was interrupted and exceeded the given time limit            |
    | CAST_INVIS           | You were invis, and noInvis is set to true                           |
    | CAST_NOTARGET        | You don't have a target selected for this spell                      |
    | CAST_NOTMEMMED       | Spell is not memmed and you gem to mem was not specified             |
    | CAST_NOTREADY        | AA ability or spell is not ready yet                                 |
    | CAST_OUTOFMANA       | You don't have enough mana for this spell!                           |
    | CAST_OUTOFRANGE      | Target is out of range                                               |
    | CAST_RESIST          | Your spell was resisted!                                             |
    | CAST_SUCCESS         | Your spell was cast successfully! (yay)                              |
    | CAST_UNKNOWN         | Spell/Item/Ability was not found                                     |
    | CAST_COLLAPSE        | Gate Collapsed                                                       |
    | CAST_TAKEHOLD        | Spell not hold                                                       |
    | CAST_FIZZLE          | Spell Fizzle                                                         |
    | CAST_INVISIBLE       | NOT Casting Invis                                                    |
    | CAST_RECOVER	       | Spell not Recovered yet!                                             |
    | CAST_STUNNED	       | Stunned                                                              |
    | CAST_STANDIG	       | Not Standing                                                         |
    | CAST_DISTRACTED      | To Distracted ( spell book open )                                    |
    | CAST_COMPONENTS| Missing Component													      |
     
     */
	public enum CastReturn
	{
		CAST_CANCELLED,
		CAST_CANNOTSEE,
		CAST_IMMUNE,
		CAST_INTERRUPTED,
		CAST_INVIS,
		CAST_NOTARGET,
		CAST_NOTMEMMED,
		CAST_NOTREADY,
		CAST_OUTOFMANA,
		CAST_OUTOFRANGE,
		CAST_RESIST,
		CAST_SUCCESS,
		CAST_UNKNOWN,
		CAST_COLLAPSE,
		CAST_TAKEHOLD,
		CAST_FIZZLE,
		CAST_INVISIBLE,
		CAST_RECOVER,
		CAST_STUNNED,
		CAST_STANDIG,
		CAST_DISTRACTED,
		CAST_COMPONENTS,
		CAST_REAGENT,
		CAST_ZONING,
		CAST_FEIGN,
		CAST_SPELLBOOKOPEN,
		CAST_BLOCKINGWINDOWOPEN,
		CAST_ACTIVEDISC,
		CAST_INTERRUPTFORHEAL,
		CAST_CORPSEOPEN,
		CAST_INVALID,
		CAST_IFFAILURE
	}

}
