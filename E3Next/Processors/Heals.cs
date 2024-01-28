using E3Core.Data;
using E3Core.Settings;
using E3Core.Utility;
using IniParser;
using MonoCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace E3Core.Processors
{
	public class Heals : BaseProcessor
	{
		public static ISpawns _spawns = E3.Spawns;

		private static Int64 _nextHealCheck = 0;
		private static Int64 _nextHealCheckInterval = 250;
		private static bool _useEQGroupDataForHeals = true;
		private static Data.Spell _orbOfShadowsSpell = null;
		private static Data.Spell _orbOfSoulsSpell = null;



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

			if (!Basics.InCombat())
			{
				if (!e3util.ShouldCheck(ref _nextHealCheck, _nextHealCheckInterval)) return;

			}
			using (_log.Trace())
			{
				//grabbing these values now and reusing them
				Int32 currentMana = MQ.Query<Int32>("${Me.CurrentMana}");
				Int32 pctMana = MQ.Query<Int32>("${Me.PctMana}");
				Int32 targetID = MQ.Query<Int32>("${Target.ID}");
				if (E3.CharacterSettings.HealTanks.Count > 0 && E3.CharacterSettings.HealTankTargets.Count > 0)
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
					HealXTargets(currentMana, pctMana);
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
		public static bool HealXTargets(Int32 currentMana, Int32 pctMana, bool JustCheck = false)
		{
			if (!E3.CharacterSettings.WhoToHeal.Contains("XTargets"))
			{
				return false;
			}
			//find the lowest health xtarget
			const Int32 XtargetMax = 12;
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
			//found someone to heal
			if (lowestHealthTargetid > 0 && currentLowestHealth < 95)
			{
				foreach (var spell in E3.CharacterSettings.HealXTarget)
				{
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
							if (JustCheck) return true;
							if (Casting.CheckReady(spell))
							{

								if (Casting.Cast(lowestHealthTargetid, spell) == CastReturn.CAST_FIZZLE)
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
				if(spell.HealthMax<100 && spell.HealthMax>=pctHealth)
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
		public static bool SomeoneNeedsHealing(Spell spell,Int32 currentMana, Int32 pctMana)
		{
			if (!((E3.CurrentClass & Data.Class.Priest) == E3.CurrentClass))
			{
				return false;
			}

			//Int32 currentMana = MQ.Query<Int32>("${Me.CurrentMana}");
			//Int32 pctMana = MQ.Query<Int32>("${Me.PctMana}");
			if (E3.CharacterSettings.WhoToHeal.Contains("Tanks"))
			{
				if (Heal(currentMana, pctMana, E3.CharacterSettings.HealTankTargets, E3.CharacterSettings.HealTanks, false, true))
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
				if (HealXTargets(currentMana, pctMana, true))
				{
					return true;
				}
			}
			if (E3.CharacterSettings.HealParty.Count > 0)
			{
				if (HealParty(currentMana, pctMana, E3.CharacterSettings.HealParty, true))
				{
					return true;
				}
			}
			return false;
		}
		private static bool Heal(Int32 currentMana, Int32 pctMana, List<string> targets, List<Data.Spell> spells, bool healPets = false, bool JustCheck = false)
		{
			//using (_log.Trace())
			{

				foreach (var name in targets)
				{
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
												if (JustCheck) return true;
											
												if (E3.CharacterSettings.HealAutoNecroOrbs)
												{
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
												if (Casting.CheckReady(spell) && Casting.CheckMana(spell))
												{
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
							//if a pet and we are here, kick out.
							if (healPets) return false;

							//check netbots
							bool isABot = E3.Bots.BotsConnected().Contains(name, StringComparer.OrdinalIgnoreCase);
							if (isABot)
							{
								//they are a bot and they are in zone
								Int32 pctHealth = E3.Bots.PctHealth(name);
								foreach (var spell in spells)
								{
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
											if (JustCheck) return true;
											//should cast a heal!
											if (Casting.CheckReady(spell) && Casting.CheckMana(spell))
											{
												if (Casting.Cast(targetID, spell, TargetDoesNotNeedHeals) == CastReturn.CAST_FIZZLE)
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
												if (JustCheck) return true;
												//should cast a heal!
												if (Casting.CheckReady(spell) && Casting.CheckMana(spell))
												{
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
													if (Casting.CheckReady(spell) && Casting.CheckMana(spell))
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
				if (pctHps < spell.HealPct)
				{
					if (!String.IsNullOrWhiteSpace(spell.Ifs))
					{
						if (!Casting.Ifs(spell))
						{
							continue;
						}
					}
					if (Casting.CheckReady(spell) && Casting.CheckMana(spell))
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
