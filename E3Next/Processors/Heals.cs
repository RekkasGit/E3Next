using E3Core.Data;
using E3Core.Settings;
using E3Core.Utility;
using IniParser;
using MonoCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace E3Core.Processors
{
	public class Heals : BaseProcessor
	{
		public static ISpawns _spawns = E3.Spawns;

		private static Int64 _nextHealCheck = 0;
		private static Int64 _nextXTargetPlayersCheck = 0;
		[ExposedData("Heals", "HealCheckInterval")]
		private static Int64 _nextHealCheckInterval = 250;
		private static Int64 _nextXTargetPlayersInterval = 2000;

		[ExposedData("Heals", "UseEQGroupDataForHeals")]
		private static bool _useEQGroupDataForHeals = true;
		private static Data.Spell _orbOfShadowsSpell = null;
		private static Data.Spell _orbOfSoulsSpell = null;
		public static HashSet<string> IgnoreHealTargets = new HashSet<string>(StringComparer.OrdinalIgnoreCase);


		[SubSystemInit]
		public static void Init_Heals()
		{
			List<string> pattern  =new List<string>() { $@"(.+) tells the raid,\s+'Pulling'", @"(.+) tells the group,\s+'Pulling'" };
			EventProcessor.RegisterEvent("PullingIgnoreHeals", pattern, (x) => {

				if (x.match.Groups.Count > 1)
				{
					string user = x.match.Groups[1].Value;
					if(!IgnoreHealTargets.Contains(user))
					{
						IgnoreHealTargets.Add(user);
					}
					E3.Bots.Broadcast($"\arIgnore Healing \ag for \ap{user}\ag till next assist or in combat.");
					
				}
			});
			pattern = new List<string>() { $@"(.+) tells the raid,\s+'PullingOff'", @"(.+) tells the group,\s+'PullingOff'" };
			EventProcessor.RegisterEvent("PullingIgnoreHealsClear", pattern, (x) => {

				if (x.match.Groups.Count > 1)
				{
					string user = x.match.Groups[1].Value;
					if (IgnoreHealTargets.Contains(user))
					{
						IgnoreHealTargets.Remove(user);
						E3.Bots.Broadcast($"\arIgnore Healing \ag Removing \ap{user}\ag from ignore list.");
					}
		
				}
			});

			EventProcessor.RegisterCommand("/e3xtarget", (x) => {


				if (x.args.Count > 0 && E3.Bots.BotsConnected().Contains(x.args[0], StringComparer.OrdinalIgnoreCase) && !x.args[0].Equals(E3.CurrentName, StringComparison.OrdinalIgnoreCase))
				{
					var toon01 = x.args[0];
					x.args.RemoveAt(0);
					string restOfCommand = e3util.ArgsToCommand(x.args);
					E3.Bots.BroadcastCommandToPerson(toon01, $"/e3xtarget {restOfCommand}");
					
				}
				else
				{
					if(x.args.Count>0)
					{
						//this is so we don't take over the xtarget functionality unless its been used.
						_useXTargetCommand = true;
						//first we need to know what type of command this is
						string command = x.args[0].ToLower();
						if (command == "add")
						{
							if (x.args.Count < 2) return;
							string target = x.args[1].ToLower();
							if (!_XTargetSetupUsers.Contains(target))
							{
								_XTargetSetupUsers.Add(target);
							}
						}
						else if (command == "remove")
						{
							if (x.args.Count < 2) return;
							string target = x.args[1].ToLower();
							if (_XTargetSetupUsers.Contains(target))
							{
								_XTargetSetupUsers.Remove(target);
							}
						}
						else if (command == "add-tanks")
						{
							var tanks = e3util.GetRaidTanks();
							foreach (var tank in tanks)
							{
								if (!_XTargetSetupUsers.Contains(tank))
								{
									_XTargetSetupUsers.Add(tank);
								}
							}
						}
						else if (command == "remove-tanks")
						{
							var tanks = e3util.GetRaidTanks();
							foreach (var tank in tanks)
							{
								if (_XTargetSetupUsers.Contains(tank))
								{
									_XTargetSetupUsers.Remove(tank);
								}
							}
						}
						else if (command == "add-heals")
						{
							var tanks = e3util.GetRaidHealers();
							foreach (var tank in tanks)
							{
								if (!_XTargetSetupUsers.Contains(tank))
								{
									_XTargetSetupUsers.Add(tank);
								}
							}
						}
						else if (command == "remove-heals")
						{
							var tanks = e3util.GetRaidHealers();
							foreach (var tank in tanks)
							{
								if (_XTargetSetupUsers.Contains(tank))
								{
									_XTargetSetupUsers.Remove(tank);
								}
							}
						}
						else if (command == "clear")
						{
							_XTargetSetupUsers.Clear();
						}
						else if (command == "manual")
						{
							_useXTargetCommand = false;//turn auto off
						}
						else if (command == "auto")
						{
							_useXTargetCommand = true;//turn auto on
						}
					}
				}
			}, "setup for xtarget healing and what not");
		}

		private static bool _useXTargetCommand = false;
		public static HashSet<String> _XTargetSetupUsers = new HashSet<String>(StringComparer.OrdinalIgnoreCase);

		[ClassInvoke(Class.All)]
		public static void Check_XTargetPlayers()
		{
			if (!_useXTargetCommand) return;
			if (!e3util.ShouldCheck(ref _nextXTargetPlayersCheck, _nextXTargetPlayersInterval)) return;

			//get players with their slot ID's
			var xtargetPlayers = e3util.GetXTargetPlayers();
			HashSet<Int32> freeSlots = new HashSet<int>(20) { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13 };
			Queue<Int32> freeSlotQueue = new Queue<int>();
			foreach( var x in xtargetPlayers )
			{
				freeSlots.Remove(x.Value);	
			}
			foreach (var slot in freeSlots) {
				freeSlotQueue.Enqueue(slot);
			}

			foreach (String player in _XTargetSetupUsers)
			{
				//see if there are any missing
				if(!xtargetPlayers.ContainsKey(player))
				{
					if(_spawns.TryByName(player, out var s))
					{

						if(Casting.TrueTarget(s.ID))
						{
							if(freeSlotQueue.Count > 0)
							{
								Int32 slotToUse = freeSlotQueue.Dequeue();
								MQ.Cmd($"/xtarget set {slotToUse} {player}");
								E3.Bots.Broadcast($"Adding \am{player}\aw to slot {slotToUse} xtarget!");
							}
						}
					}
				}
			}

			foreach (var x in xtargetPlayers)
			{
				if(!_XTargetSetupUsers.Contains(x.Key))
				{
					Int32 slotToUse = x.Value;
					MQ.Cmd($"/xtarget set {slotToUse} AH");
					E3.Bots.Broadcast($"Removing \am{x.Key}\aw from xtarget slot {slotToUse}!");
				}
			}
		}
		[AdvSettingInvoke]
		public static void Check_Heals()
		{
			//don't heal if invs, don't heal if not assiting yet naving around
			if (E3.IsInvis) return;
			//if configured to not heal while naving check to see if we are naving
			if (!E3.GeneralSettings.General_HealWhileNavigating)
			{
				if (!Assist.IsAssisting && Movement.IsNavigating())
				{
					return;
				}
			}


			bool inCombat = E3.CurrentInCombat;

			//reset ignored targets once in combat
			if (inCombat && IgnoreHealTargets.Count>0)
			{
				E3.Bots.Broadcast($"\arIgnore Healing \ag Clearing users from list.");
				IgnoreHealTargets.Clear();
			}

			if (!inCombat)
			{
				if (!e3util.ShouldCheck(ref _nextHealCheck, _nextHealCheckInterval)) return;

			}
			using (_log.Trace())
			{
				//grabbing these values now and reusing them
				Int32 currentMana = MQ.Query<Int32>("${Me.CurrentMana}");
				Int32 pctMana = MQ.Query<Int32>("${Me.PctMana}");
				Int32 targetID = MQ.Query<Int32>("${Target.ID}");


				//check for Emergency heals
				Heals.SomeoneNeedEmergencyHealingGroup(currentMana, pctMana, true);
				if(!E3.ActionTaken) Heals.SomeoneNeedEmergencyHealing(currentMana, pctMana, true);

				if (!E3.ActionTaken && E3.CharacterSettings.HealTanks.Count > 0 && E3.CharacterSettings.HealTankTargets.Count > 0)
				{
					HealTanks(currentMana, pctMana);
					if (E3.ActionTaken)
					{   //update values
						currentMana = MQ.Query<Int32>("${Me.CurrentMana}");
						pctMana = MQ.Query<Int32>("${Me.PctMana}");
					}
				}
				if (!E3.ActionTaken && E3.CharacterSettings.HealXTarget.Count > 0)
				{
					HealXTargets(E3.CharacterSettings.HealXTarget,currentMana, pctMana);
				}
				if (!E3.ActionTaken) GroupHeals(currentMana, pctMana);
				if (!E3.ActionTaken) HealImportant(currentMana, pctMana);
				if (!E3.ActionTaken) HealParty(currentMana, pctMana);
				if (!E3.ActionTaken) HealAll(currentMana, pctMana);
				if (!E3.ActionTaken) HoTTanks(currentMana, pctMana);
				if (!E3.ActionTaken) HoTImportant(currentMana, pctMana);
				if (!E3.ActionTaken) HoTAll(currentMana, pctMana);
				if (!E3.ActionTaken) HealPets(currentMana, pctMana);
				if (!E3.ActionTaken) HoTPets(currentMana, pctMana);

				e3util.PutOriginalTargetBackIfNeeded(targetID);
			}
		}
		public static bool HealTanks(Int32 currentMana, Int32 pctMana)
		{
			if (E3.CharacterSettings.WhoToHeal.Contains("Tanks"))
			{
				return Heal(currentMana, pctMana, E3.CharacterSettings.HealTankTargets, E3.CharacterSettings.HealTanks);
			}
			return false;
		}
		public static bool HealImportant(Int32 currentMana, Int32 pctMana)
		{
			if (E3.CharacterSettings.WhoToHeal.Contains("ImportantBots"))
			{
				return Heal(currentMana, pctMana, E3.CharacterSettings.HealImportantBotTargets, E3.CharacterSettings.HealImportantBots);
			}
			return false;
		}
		public static bool HealParty(Int32 currentMana, Int32 pctMana)
		{
			if (E3.CharacterSettings.HealParty.Count > 0)
			{
				return HealParty(currentMana, pctMana, E3.CharacterSettings.HealParty);
			}
			return false;
		}
		public static bool HealXTargets(List<Spell> spellsToUse, Int32 currentMana, Int32 pctMana, bool JustCheck = false, bool isEmergency = false)
		{
			if (!E3.CharacterSettings.WhoToHeal.Contains("XTargets"))
			{
				return false;
			}
			//find the lowest health xtarget
			Int32 XtargetMax = e3util.XtargetMax;
			//dealing with index of 1.
			Int32 currentLowestHealth = 100;
			Int32 lowestHealthTargetid = -1;
			double lowestHealthTargetDistance = -1;
			for (Int32 x = 1; x <= XtargetMax; x++)
			{

				if (!MQ.Query<bool>($"${{Me.XTarget[{x}].TargetType.Equal[Specific PC]}}")) continue;
				Int32 targetID = MQ.Query<Int32>($"${{Me.XTarget[{x}].ID}}");
				if (targetID > 0)
				{
					//check to see if they are in zone.
					Spawn s;
					if (_spawns.TryByID(targetID, out s))
					{
						if (!IgnoreHealTargets.Contains(s.CleanName))
						{
							if (s.TypeDesc != "Corpse")
							{
								if (s.Distance < 200)
								{
									Int32 pctHealth = MQ.Query<Int32>($"${{Me.XTarget[{x}].PctHPs}}");
									if (pctHealth <= currentLowestHealth)
									{
										currentLowestHealth = pctHealth;
										lowestHealthTargetid = targetID;
										lowestHealthTargetDistance = s.Distance;
									}
								}
							}
						}
					}
				}
			}
			//found someone to heal
			if (lowestHealthTargetid > 0 && currentLowestHealth < 95)
			{
				foreach (var spell in spellsToUse)
				{
					if (!spell.Enabled) continue;
					//check Ifs on the spell
					if (!String.IsNullOrWhiteSpace(spell.Ifs))
					{
						if (!Casting.Ifs(spell))
						{
							//failed check, onto the next
							continue;
						}
					}
				recastSpell:
					if (spell.Mana > currentMana)
					{
						//mana cost too high
						continue;
					}
					if (spell.MinMana > pctMana)
					{
						//mana is set too high, can't cast
						continue;
					}
					if (lowestHealthTargetDistance < spell.MyRange)
					{
						if (currentLowestHealth < spell.HealPct)
						{
							
							if (Casting.CheckMana(spell) && Casting.CheckReady(spell,JustCheck,JustCheck))
							{
								if (JustCheck) return true;

								if (Casting.Cast(lowestHealthTargetid, spell,null,false,isEmergency) == CastReturn.CAST_FIZZLE)
								{
									currentMana = MQ.Query<Int32>("${Me.CurrentMana}");
									pctMana = MQ.Query<Int32>("${Me.PctMana}");
									goto recastSpell;
								}
								E3.ActionTaken = true;
								return true;
							}
						}
					}
				}
			}
			return false;
		}


		public static void HoTPets(Int32 currentMana, Int32 pctMana)
		{
			if (E3.CharacterSettings.WhoToHoT.Contains("Pets"))
			{
				HealOverTime(currentMana, pctMana, E3.CharacterSettings.HealPetOwners, E3.CharacterSettings.HealOverTime);
			}
		}
		public static void HealPets(Int32 currentMana, Int32 pctMana)
		{
			if (E3.CharacterSettings.WhoToHeal.Contains("Pets"))
			{
				Heal(currentMana, pctMana, E3.CharacterSettings.HealPetOwners, E3.CharacterSettings.HealPets, true);

			}
		}
		public static void HoTAll(Int32 currentMana, Int32 pctMana)
		{
			if (E3.CharacterSettings.WhoToHoT.Contains("All"))
			{
				List<string> targets = E3.Bots.BotsConnected();
				HealOverTime(currentMana, pctMana, targets, E3.CharacterSettings.HealOverTime);
			}
		}
		public static void HoTImportant(Int32 currentMana, Int32 pctMana)
		{
			if (E3.CharacterSettings.WhoToHoT.Contains("ImportantBots"))
			{
				HealOverTime(currentMana, pctMana, E3.CharacterSettings.HealImportantBotTargets, E3.CharacterSettings.HealOverTime);
			}
		}
		public static void HoTTanks(Int32 currentMana, Int32 pctMana)
		{
			if (E3.CharacterSettings.WhoToHoT.Contains("Tanks"))
			{
				HealOverTime(currentMana, pctMana, E3.CharacterSettings.HealTankTargets, E3.CharacterSettings.HealOverTime);
			}
		}
		public static void HealAll(Int32 currentMana, Int32 pctMana)
		{
			if (E3.CharacterSettings.WhoToHeal.Contains("All"))
			{
				//get a list from netbots
				List<string> targets = E3.Bots.BotsConnected();
				Heal(currentMana, pctMana, targets, E3.CharacterSettings.HealAll);

			}
		}
		public static void GroupHeals(Int32 currentMana, Int32 pctMana)
		{
			foreach (var spell in E3.CharacterSettings.HealGroup)
			{
				if (!spell.Enabled) continue;
				//check Ifs on the spell
				if (!String.IsNullOrWhiteSpace(spell.Ifs))
				{
					if (!Casting.Ifs(spell))
					{
						//failed check, onto the next
						continue;
					}
				}
				Int32 numberNeedingHeal = MQ.Query<Int32>($"${{Group.Injured[{spell.HealPct}]}}");
				if (numberNeedingHeal >= E3.CharacterSettings.HealGroup_NumberOfInjuredMembers)
				{
				recastSpell:
					if (spell.Mana > currentMana)
					{
						//mana cost too high
						continue;
					}
					if (spell.MinMana > pctMana)
					{
						//mana is set too high, can't cast
						continue;
					}
					if (Casting.CheckReady(spell))
					{
						if (Casting.Cast(0, spell) == CastReturn.CAST_FIZZLE)
						{
							currentMana = MQ.Query<Int32>("${Me.CurrentMana}");
							pctMana = MQ.Query<Int32>("${Me.PctMana}");
							goto recastSpell;
						}
						E3.ActionTaken = true;
						return;
					}

				}
			}
		}

		public static bool TargetDoesNotNeedHeals(Spell spell,Int32 currentMana, Int32 pctMana)
		{
			//Int32 currentMana = MQ.Query<Int32>("${Me.CurrentMana}");
			//Int32 pctMana = MQ.Query<Int32>("${Me.PctMana}");
			Int32 pctHealth = MQ.Query<Int32>("${Target.PctHPs}");
			if (spell != null)
			{
				if(spell.HealthMax<100 && spell.HealthMax<=pctHealth)
				{
					E3.Bots.Broadcast($"Health Max set, {spell.CastTarget} does not need health, canceling {spell.SpellName}.");
					return true;
				}
			}
			return false;
		}

		/// <summary>
		/// used as an action to determine if a spell should be interrupted in case someone needs a heal.
		/// </summary>
		/// <returns>true if a heal is needed, otherwise false</returns>
		public static bool SomeoneNeedsHealing(Spell spell, Int32 currentMana, Int32 pctMana, bool healIfNeeded = false)
		{
			if (!((E3.CurrentClass & Data.Class.Priest) == E3.CurrentClass))
			{
				return false;
			}

			bool justCheck = true;
			if (healIfNeeded)
			{
				justCheck = false;
			}
			if (E3.CharacterSettings.WhoToHeal.Contains("Tanks"))
			{
				if (Heal(currentMana, pctMana, E3.CharacterSettings.HealTankTargets, E3.CharacterSettings.HealTanks, false, justCheck))
				{
					return true;
				}
			}
			if (E3.CharacterSettings.WhoToHeal.Contains("ImportantBots"))
			{
				if (Heal(currentMana, pctMana, E3.CharacterSettings.HealImportantBotTargets, E3.CharacterSettings.HealImportantBots, false, true))
				{
					return true;
				}
			}
			if (E3.CharacterSettings.HealXTarget.Count > 0)
			{
				if (HealXTargets(E3.CharacterSettings.HealXTarget, currentMana, pctMana, justCheck))
				{
					return true;
				}
			}
			if (E3.CharacterSettings.HealParty.Count > 0)
			{
				if (HealParty(currentMana, pctMana, E3.CharacterSettings.HealParty, justCheck))
				{
					return true;
				}
			}
			return false;
		}
		public static bool SomeoneNeedEmergencyHealing(Int32 currentMana, Int32 pctMana, bool CastIfNeed = false)
		{
			if (Zoning.CurrentZone.IsSafeZone) return false;

			foreach (var spell in E3.CharacterSettings.Heal_EmergencyHeals)
			{
				if (!spell.Enabled) continue;
				string target = spell.CastTarget;

				if (IgnoreHealTargets.Contains(target)) continue;

				if (!String.IsNullOrWhiteSpace(spell.Ifs))
				{
					if (!Casting.Ifs(spell))
					{
						//failed check, onto the next
						continue;
					}
				}

				Int32 pctHealth = 0;
				if (E3.Bots.IsMyBot(target))
				{
					pctHealth = E3.Bots.PctHealth(target);
				}
				else
				{
					//not our bot
					continue;
				}
				if(_spawns.TryByName(target,out var s))
				{
					if (!Casting.InRange(s.ID, spell))
					{
						continue;
					}
				}
				else
				{
					continue;
				}

				if(CastIfNeed && spell.CastType== CastingType.Spell)
				{
					while (Casting.InGlobalCooldown())
					{
						MQ.Delay(10);
					}
				}

				if (Casting.CheckMana(spell) && Casting.CheckReady(spell, true, !CastIfNeed))
				{
					if (pctHealth < spell.HealPct && pctHealth != 0)
					{
						if (CastIfNeed)
						{
							E3.Bots.Broadcast($"\agTrying to Casting Emergency Heal. \aw[\ag{spell.CastName}\aw] \agTarget:\ap{target} \agPctHealth:{pctHealth}");
							Heal(currentMana, pctMana, new List<string> { target },new List<Spell>() {spell}, false, false, true);
							return true;
						}

						return true;
					}
				}
			}
			return false;
		}
		public static bool SomeoneNeedEmergencyHealingGroup(Int32 currentMana, Int32 pctMana, bool CastIfNeeded = false)
		{
			if (E3.CharacterSettings.Heal_EmergencyGroupHeals.Count == 0) return false;
			if (Zoning.CurrentZone.IsSafeZone) return false;

			Int32 groupmemberCount = MQ.Query<Int32>("${Group.Members}");

			for(Int32 i = 0;i<=groupmemberCount;i++)
			{
				Int32 pctHealth = 0;
				string name = MQ.Query<string>($"${{Group.Member[{i}].Name}}");

				if (IgnoreHealTargets.Contains(name)) continue;

				if (E3.Bots.IsMyBot(name))
				{
					//lets look up their health
					pctHealth = E3.Bots.PctHealth(name);
				}
				else
				{
					//have to do a normal health check
					pctHealth = MQ.Query<Int32>($"${{Group.Member[{i}].Spawn.CurrentHPs}}");
				}
				foreach (Spell spell in E3.CharacterSettings.Heal_EmergencyGroupHeals)
				{
					if (!String.IsNullOrWhiteSpace(spell.Ifs))
					{
						if (!Casting.Ifs(spell))
						{
							//failed check, onto the next
							continue;
						}
					}
					if (_spawns.TryByName(name, out var s))
					{
						if (!Casting.InRange(s.ID, spell))
						{
							continue;
						}
					}
					else
					{
						continue;
					}
					if(Casting.CheckMana(spell) && Casting.CheckReady(spell, true, !CastIfNeeded))
					{
						if (pctHealth < spell.HealPct)
						{
							if (CastIfNeeded)
							{
								E3.Bots.Broadcast($"\agTrying to Cast Emergency Heal Group. \aw[\ag{spell.CastName}\aw]\ag Target:\ap{name} \agPctHealth:{pctHealth}");
								Heal(currentMana, pctMana, new List<string> { name }, new List<Spell>() {spell}, false, false, true);
							}
							return true;
						}

					}
				}
			}
		
			return false;
		}
		private static bool Heal(Int32 currentMana, Int32 pctMana, List<string> targets, List<Data.Spell> spells, bool healPets = false, bool JustCheck = false, bool isEmergency = false)
		{
			//using (_log.Trace())
			{

				foreach (var name in targets)
				{
					if (IgnoreHealTargets.Contains(name)) continue;

					Int32 targetID = 0;
					Spawn s;
					if (_spawns.TryByName(name, out s))
					{
						targetID = healPets ? s.PetID : s.ID;

						if (s.ID != targetID)
						{
							if (!_spawns.TryByID(targetID, out s))
							{
								//can't find pet, skip
								continue;
							}
						}
						double targetDistance = s.Distance;
						string targetType = s.TypeDesc;

						//first lets check the distance.
						bool inRange = false;
						foreach (var spell in spells)
						{
							if (!spell.Enabled) continue;
							if (Casting.InRange(targetID, spell))
							{
								inRange = true;
								break;
							}
						}
						if (!inRange)
						{   //no spells in range next target
							continue;
						}
						//in range
						if (targetType == "PC" || targetType == "Pet")
						{
							//check group data
							if (_useEQGroupDataForHeals || healPets)
							{
								Int32 groupMemberIndex = MQ.Query<Int32>($"${{Group.Member[{name}].Index}}");

								if (groupMemberIndex > 0)
								{
									Int32 pctHealth = 0;
									if (healPets)
									{
										pctHealth = MQ.Query<Int32>($"${{Group.Member[{groupMemberIndex}].Spawn.Pet.CurrentHPs}}");
									}
									else
									{
										pctHealth = MQ.Query<Int32>($"${{Group.Member[{groupMemberIndex}].Spawn.CurrentHPs}}");
									}

									if (pctHealth < 1)
									{
										//dead, no sense in casting. check the next person
										continue;
									}
									foreach (var spell in spells)
									{
										if (!spell.Enabled) continue;
										//check Ifs on the spell
										if (!String.IsNullOrWhiteSpace(spell.Ifs))
										{
											if (!Casting.Ifs(spell))
											{
												//failed check, onto the next
												continue;
											}
										}

										bool shouldContinue = false;
										if (spell.CheckForCollection.Count>0)
										{
											var bufflist = E3.Bots.BuffList(name);
											foreach (var checkforItem in spell.CheckForCollection.Keys)
											{
												if (bufflist.Contains(spell.CheckForCollection[checkforItem]))
												{
													shouldContinue = true;
													break;
												}
											}
											if (shouldContinue) { continue; }
										}

									recastSpell:
										if (spell.Mana > currentMana)
										{
											//mana cost too high
											continue;
										}
										if (spell.MinMana > pctMana)
										{
											//mana is set too high, can't cast
											continue;
										}


										if (Casting.InRange(targetID, spell))
										{
											if (pctHealth < spell.HealPct)
											{
												if (E3.CharacterSettings.HealAutoNecroOrbs && !JustCheck)
												{
													if (_orbOfShadowsSpell == null && MQ.Query<bool>($"${{Me.ItemReady[Orb of the Sanguine]}}"))
													{
														_orbOfShadowsSpell = new Data.Spell("Orb of the Sanguine");
													}
													if (_orbOfShadowsSpell == null && MQ.Query<bool>($"${{Me.ItemReady[Orb of Shadows]}}"))
													{
														_orbOfShadowsSpell = new Data.Spell("Orb of Shadows");
													}
													if (_orbOfSoulsSpell == null && MQ.Query<bool>($"${{Me.ItemReady[Orb of Souls]}}"))
													{
														_orbOfSoulsSpell = new Data.Spell("Orb of Souls");
													}
													if (_orbOfShadowsSpell!=null && Casting.CheckReady(_orbOfShadowsSpell))
													{
														Casting.Cast(targetID, _orbOfShadowsSpell,null,false);
													}
													if (_orbOfSoulsSpell != null && Casting.CheckReady(_orbOfSoulsSpell))
													{
														Casting.Cast(targetID, _orbOfSoulsSpell);
													}
												}
												//should cast a heal!
												if (Casting.CheckMana(spell) && Casting.CheckReady(spell, JustCheck, JustCheck))
												{
													if (JustCheck)
													{
														_log.Write($"eq group {name} pct:{pctHealth} forcing interrupt");
														return true;
													}

													if (Casting.Cast(targetID, spell, TargetDoesNotNeedHeals,false,isEmergency) == CastReturn.CAST_FIZZLE)
													{
														currentMana = MQ.Query<Int32>("${Me.CurrentMana}");
														pctMana = MQ.Query<Int32>("${Me.PctMana}");
														goto recastSpell;
													}
													E3.ActionTaken = true;
													return true;
												}
											}
										}
									}
								}
							}
							//if a pet and we are here, kick out.
							if (healPets) continue;

							//check netbots
							bool isABot = E3.Bots.BotsConnected().Contains(name, StringComparer.OrdinalIgnoreCase);
							if (isABot)
							{
								//they are a bot and they are in zone
								Int32 pctHealth = E3.Bots.PctHealth(name);

								foreach (var spell in spells)
								{
									if (!spell.Enabled) continue;
									//check Ifs on the spell
									if (!String.IsNullOrWhiteSpace(spell.Ifs))
									{
										if (!Casting.Ifs(spell))
										{
											//failed check, onto the next
											continue;
										}
									}
									bool shouldContinue = false;
									if (spell.CheckForCollection.Count > 0)
									{
										var bufflist = E3.Bots.BuffList(name);
										foreach (var checkforItem in spell.CheckForCollection.Keys)
										{
											if (bufflist.Contains(spell.CheckForCollection[checkforItem]))
											{
												shouldContinue = true;
												break;
											}
										}
										if (shouldContinue) { continue; }
									}
								recastSpell:
									if (spell.Mana > currentMana)
									{
										//mana cost too high
										continue;
									}
									if (spell.MinMana > pctMana)
									{
										//mana is set too high, can't cast
										continue;
									}
									if (targetDistance < spell.MyRange)
									{
										if (pctHealth < spell.HealPct)
										{
										
											//should cast a heal!
											if (Casting.CheckMana(spell) && Casting.CheckReady(spell, JustCheck, JustCheck))
											{
												if (JustCheck)
												{
													_log.Write($"e3n bot {name} pct:{pctHealth} forcing interrupt");
													return true;
												}
											
												if (Casting.Cast(targetID, spell, TargetDoesNotNeedHeals,false,isEmergency) == CastReturn.CAST_FIZZLE)
												{
													currentMana = MQ.Query<Int32>("${Me.CurrentMana}");
													pctMana = MQ.Query<Int32>("${Me.PctMana}");
													goto recastSpell;
												}
												E3.ActionTaken = true;
												return true;
											}
										}
									}
								}
							}

						}

					}
				}

				return false;
			}
		}
		private static bool HealParty(Int32 currentMana, Int32 pctMana, List<Data.Spell> spells, bool healPets = false, bool JustCheck = false)
		{
			//using (_log.Trace())
			{

				foreach (var name in Basics.GroupMembers)
				{
					
					Int32 targetID = 0;
					Spawn s;
					if (_spawns.TryByID(name, out s))
					{
						if (IgnoreHealTargets.Contains(s.CleanName)) continue;

						targetID = healPets ? s.PetID : s.ID;

						if (s.ID != targetID)
						{
							if (!_spawns.TryByID(targetID, out s))
							{
								//can't find pet, skip
								continue;
							}
						}
						double targetDistance = s.Distance;
						string targetType = s.TypeDesc;

						//first lets check the distance.
						bool inRange = false;
						foreach (var spell in spells)
						{
							if (!spell.Enabled) continue;
							if (Casting.InRange(targetID, spell))
							{
								inRange = true;
								break;
							}
						}
						if (!inRange)
						{   //no spells in range next target
							continue;
						}
						//in range
						if (targetType == "PC" || targetType == "Pet")
						{
							//check group data
							if (_useEQGroupDataForHeals || healPets)
							{
								Int32 groupMemberIndex = MQ.Query<Int32>($"${{Group.Member[{s.Name}].Index}}");

								if (groupMemberIndex > 0)
								{
									Int32 pctHealth = 0;
									if (healPets)
									{
										pctHealth = MQ.Query<Int32>($"${{Group.Member[{groupMemberIndex}].Spawn.Pet.CurrentHPs}}");
									}
									else
									{
										pctHealth = MQ.Query<Int32>($"${{Group.Member[{groupMemberIndex}].Spawn.CurrentHPs}}");
									}

									if (pctHealth < 1)
									{
										//dead, no sense in casting. check the next person
										continue;
									}
									foreach (var spell in spells)
									{
										if (!spell.Enabled) continue;
										//check Ifs on the spell
										if (!String.IsNullOrWhiteSpace(spell.Ifs))
										{
											if (!Casting.Ifs(spell))
											{
												//failed check, onto the next
												continue;
											}
										}

									recastSpell:
										if (spell.Mana > currentMana)
										{
											//mana cost too high
											continue;
										}
										if (spell.MinMana > pctMana)
										{
											//mana is set too high, can't cast
											continue;
										}

										if (Casting.InRange(targetID, spell))
										{
											if (pctHealth < spell.HealPct)
											{
												//should cast a heal!
												if (Casting.CheckMana(spell) && Casting.CheckReady(spell, JustCheck, JustCheck))
												{
													if (JustCheck) return true;
													if (Casting.Cast(targetID, spell) == CastReturn.CAST_FIZZLE)
													{
														currentMana = MQ.Query<Int32>("${Me.CurrentMana}");
														pctMana = MQ.Query<Int32>("${Me.PctMana}");
														goto recastSpell;
													}
													E3.ActionTaken = true;
													return true;
												}
											}
										}
									}
								}
							}
						}
					}
				}
				return false;
			}
		}
		private static void HealOverTime(Int32 currentMana, Int32 pctMana, List<string> targets, List<Data.Spell> spells, bool healPets = false)
		{
			//using (_log.Trace())
			{
				foreach (var name in targets)
				{
					if (IgnoreHealTargets.Contains(name)) continue;

					Int32 targetID = 0;
					Spawn s;
					if (_spawns.TryByName(name, out s))
					{
						targetID = healPets ? s.PetID : s.ID;

						if (s.ID != targetID)
						{
							if (!_spawns.TryByID(targetID, out s))
							{
								//can't find pet, skip
								continue;
							}
						}
						//they are in zone and have an id
						if (targetID > 0)
						{
							double targetDistance = s.Distance;
							string targetType = s.TypeDesc;

							//first lets check the distance.
							bool inRange = false;
							foreach (var spell in spells)
							{
								if (!spell.Enabled) continue;
								if (Casting.InRange(targetID, spell))
								{
									inRange = true;
									break;
								}
							}
							if (!inRange)
							{   //no spells in range next target
								continue;
							}
							//in range
							if (targetType == "PC")
							{
								//check bots
								bool isABot = E3.Bots.BotsConnected().Contains(name, StringComparer.OrdinalIgnoreCase);
								if (isABot)
								{
									//they are a netbots and they are in zone
									Int32 pctHealth = E3.Bots.PctHealth(name);
									foreach (var spell in spells)
									{
										if (!spell.Enabled) continue;
										//check Ifs on the spell
										if (!String.IsNullOrWhiteSpace(spell.Ifs))
										{
											if (!Casting.Ifs(spell))
											{
												//failed check, onto the next
												continue;
											}
										}

                                        bool shouldContinue = false;
                                        if (spell.CheckForCollection.Count > 0)
                                        {
                                            var bufflist = E3.Bots.BuffList(name);
                                            foreach (var checkforItem in spell.CheckForCollection.Keys)
                                            {
                                                if (bufflist.Contains(spell.CheckForCollection[checkforItem]))
                                                {
                                                    shouldContinue = true;
                                                    break;
                                                }
                                            }
                                            if (shouldContinue) { continue; }
                                        }
                                    recastSpell:
										if (spell.Mana > currentMana)
										{
											//mana cost too high
											continue;
										}
										if (spell.MinMana > pctMana)
										{
											//mana is set too high, can't cast
											continue;
										}
										if (Casting.InRange(targetID, spell))
										{
											if (pctHealth <= spell.HealPct)
											{
												if (!E3.Bots.HasShortBuff(name, spell.SpellID))
												{
													if (Casting.CheckMana(spell) && Casting.CheckReady(spell))
													{
														if (Casting.Cast(targetID, spell) == CastReturn.CAST_FIZZLE)
														{
															currentMana = MQ.Query<Int32>("${Me.CurrentMana}");
															pctMana = MQ.Query<Int32>("${Me.PctMana}");
															goto recastSpell;
														}
														E3.ActionTaken = true;
														return;
													}
												}
											}
										}
									}
								}
							}
						}
					}
				}
			}
		}

		public static bool Check_LifeSupport(bool JustCheck = false)
		{
			if (Zoning.CurrentZone.IsSafeZone) return false;
			Int32 pctHps = E3.PctHPs;
			Int32 myID = E3.CurrentId;
			Int32 targetID = MQ.Query<Int32>("${Target.ID}");
			foreach (var spell in E3.CharacterSettings.LifeSupport)
			{
				if (!spell.Enabled) continue;
				if (pctHps < spell.HealPct)
				{
					if (!String.IsNullOrWhiteSpace(spell.Ifs))
					{
						if (!Casting.Ifs(spell))
						{
							continue;
						}
					}

					bool shouldContinue = false;
					if (spell.CheckForCollection.Count > 0)
					{
						var bufflist = E3.Bots.BuffList(E3.CurrentName);
						foreach (var checkforItem in spell.CheckForCollection.Keys)
						{
							if (bufflist.Contains(spell.CheckForCollection[checkforItem]))
							{
								shouldContinue = true;
								break;
							}
						}
						if (shouldContinue) { continue; }
					}
					if (Casting.CheckMana(spell) && Casting.CheckReady(spell))
					{
						if (JustCheck) return true;
						Int32 targetIDToUse = myID;
						//don't change your target if you are using a self healing item
						if (spell.CastName.IndexOf("Divine Healing", 0, StringComparison.OrdinalIgnoreCase) > -1) targetIDToUse = 0;
						if (spell.CastName.IndexOf("Sanguine Mind Crystal", 0, StringComparison.OrdinalIgnoreCase) > -1) targetIDToUse = 0;
						Casting.Cast(targetIDToUse, spell);
						if (targetID > 0)
						{
							Casting.TrueTarget(targetID, true);
						}
						return true;
					}
				}
			}
			return false;
		}
	}
}
