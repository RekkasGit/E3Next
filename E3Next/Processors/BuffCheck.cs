using E3Core.Classes;
using E3Core.Data;
using E3Core.Server;
using E3Core.Settings;
using E3Core.Utility;
using MonoCore;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.ServiceModel.Configuration;
using System.Xml.Linq;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

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

		private static Int64 _nextGroupBuffRequestCheckTime = 0;
		private static Int64 _nextGroupBuffRequestCheckTimeInterval = 1000;
		private static Int64 _nextRaidBuffRequestCheckTime = 0;
		private static Int64 _nextRaidBuffRequestCheckTimeInterval = 1000;
		private static Int64 _nextStackBuffRequestCheckTime = 0;
		private static Int64 _nextStackBuffRequestCheckTimeInterval = 1000;

		private static Int64 _nextBandoBuffCheck = 0;
		private static Int64 _nextBandoBuffCheckInterval = 1000;

		private static Int64 _nextBotCacheCheckTime = 0;
		private static Int64 _nextBotCacheCheckTimeInterval = 1000;
		private static Int64 _nextInstantBuffRefresh = 0;
		private static Int64 _nextInstantRefreshTimeInterval = 250;
		private static List<Int32> _keyList = new List<int>();
		//private static Int64 _printoutTimer;
		private static Data.Spell _selectAura = null;
		private static Int64 _nextBuffCheck = 0;
		[ExposedData("BuffCheck", "BuffCheckInterval")]
		private static Int64 _nextBuffCheckInterval = 1000;
		[ExposedData("BuffCheck", "XPBuffs")]
		private static List<Int32> _xpBuffs = new List<int>() { 42962 /*xp6*/, 42617 /*xp5*/, 42616 /*xp4*/};
		[ExposedData("BuffCheck", "GMBuffs")]
		private static List<Int32> _gmBuffs = new List<int>() { 34835, 35989, 35361, 25732, 34567, 36838, 43040, 36266, 36423 };
		private static Int64 _nextBlockBuffCheck = 0;
		[ExposedData("BuffCheck", "BlockBuffCheckInterval")]
		private static Int64 _nextBlockBuffCheckInterval = 250;
		[ExposedData("BuffCheck", "InitAuras")]
		static bool _initAuras = false;

		public static void AddToBuffCheckTimer(int millisecondsToAdd)
		{
			_nextBuffCheck = Core.StopWatch.ElapsedMilliseconds + millisecondsToAdd;
		}

		[SubSystemInit]
		public static void BuffCheck_Init()
		{
			RegisterEvents();
		}
		public static void Reset()
		{
			_initAuras = false;
			_selectAura = null;
			foreach (var pair in _buffTimers)
			{
				pair.Value.Dispose();

			}
			_buffTimers.Clear();
		}
		private static void RegisterEvents()
		{

			EventProcessor.RegisterCommand("/dropbuff", (x) =>
			{
				if (x.args.Count > 0)
				{
					string buffToDrop = x.args[0];
					DropBuff(buffToDrop);
					E3.Bots.BroadcastCommandToGroup($"/removebuff {buffToDrop}");
				}
			});
			EventProcessor.RegisterCommand("/dropbuffid", (x) =>
			{
				if (x.args.Count > 0)
				{
					Int32 buffToDrop;
					if (Int32.TryParse(x.args[0], out buffToDrop))
					{
						DropBuff(buffToDrop);
						E3.Bots.BroadcastCommand($"/removebuff {buffToDrop}");

					}
				}
			});
			EventProcessor.RegisterCommand("/e3clearbufftimers", (x) =>
			{
				E3.Bots.Broadcast("Clearing buff timers");
				ClearBuffTimers();

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
						foreach (var spell in E3.CharacterSettings.BlockedBuffs)
						{
							MQ.Write("\at" + spell.SpellName);
						}
					}
				}
			});
		}
		public static void BlockBuffRemove(string spellName)
		{
			List<Spell> newList = E3.CharacterSettings.BlockedBuffs.Where(y => !y.SpellName.Equals(spellName, StringComparison.OrdinalIgnoreCase)).ToList();
			E3.CharacterSettings.BlockedBuffs = newList;
			E3.CharacterSettings.SaveData();

		}
		public static void BlockBuffAdd(string spellName)
		{
			//check if it exists
			bool exists = false;
			foreach (var spell in E3.CharacterSettings.BlockedBuffs)
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
					E3.CharacterSettings.BlockedBuffs.Add(s);
					E3.CharacterSettings.SaveData();
				}
			}
		}
		public static Boolean DropBuff(string buffToDrop)
		{
			//first look for exact match
			Int32 buffID = MQ.Query<Int32>($"${{Spell[{buffToDrop}].ID}}");
			if (buffID < 1)
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
				if (buffID < 1)
				{
					for (Int32 i = 1; i <= 25; i++)
					{
						string buffName = MQ.Query<String>($"${{Me.Song[{i}]}}");
						if (buffName.IndexOf(buffToDrop, StringComparison.OrdinalIgnoreCase) > -1)
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
			bool hasBuff = MQ.Query<bool>($"${{Me.Buff[{buffName}].ID}}");
			if (!hasBuff)
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


			foreach (var spell in E3.CharacterSettings.BlockedBuffs)
			{

				if (!String.IsNullOrWhiteSpace(spell.Ifs))
				{
					if (!Casting.Ifs(spell))
					{
						continue;
					}
				}

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
					if (MQ.Query<bool>("${Me.Song[Transcendent Torpor]}") || MQ.Query<bool>("${Me.Buff[Transcendent Torpor]}"))
					{
						DropBuff("Transcendent Torpor");
					}
					if (MQ.Query<bool>("${Me.Song[Torpor]}") || MQ.Query<bool>("${Me.Buff[Torpor]}"))
					{
						DropBuff("Torpor");
					}
				}
			}
		}
		[ClassInvoke(Data.Class.All)]
		public static void Check_GroupBuffRequests()
		{

			if (E3.IsInvis) return;
			if (!e3util.ShouldCheck(ref _nextGroupBuffRequestCheckTime, _nextGroupBuffRequestCheckTimeInterval)) return;

			foreach (var spell in E3.CharacterSettings.GroupBuffRequests)
			{
				if (!spell.Enabled) continue;
				if (spell.LastRequestTimeStamp > 0)
				{
					if ((Core.StopWatch.ElapsedMilliseconds - spell.LastRequestTimeStamp) < 15000) continue;
				}
				if (_spawns.TryByName(spell.CastTarget, out var spawn))
				{
					if (!String.IsNullOrWhiteSpace(spell.Ifs))
					{
						if (!Casting.Ifs(spell))
						{
							continue;
						}
					}
					if (spawn.Distance > 199) continue;
					//self buffs!
					Int32 groupMemberIndex = MQ.Query<Int32>($"${{Group.Member[{spell.CastTarget}].Index}}");
					if (groupMemberIndex > 0)
					{
						bool hasBuff = MQ.Query<bool>($"${{Bool[${{Me.Buff[{spell.SpellName}]}}]}}");

						if (!hasBuff)
						{
							//request it then
							MQ.Cmd($"/gsay {spell.SpellName}");
							spell.LastRequestTimeStamp = Core.StopWatch.ElapsedMilliseconds;
						}
					}
				}
			}

		}
		[ClassInvoke(Data.Class.All)]
		public static void Check_StackBuffRequests()
		{
			if (E3.IsInvis) return;
			if (!e3util.ShouldCheck(ref _nextStackBuffRequestCheckTime, _nextStackBuffRequestCheckTimeInterval)) return;

			foreach (var spell in E3.CharacterSettings.StackBuffRequest)
			{
				if (!spell.Enabled) continue;
				if (!e3util.ShouldCheck(ref spell.StackIntervalNextCheck, spell.StackIntervalCheck)) continue;

                if (!String.IsNullOrWhiteSpace(spell.Ifs))
                {
                    if (!Casting.Ifs(spell))
                    {
                        continue;
                    }
                }

                bool haveBuff = MQ.Query<bool>($"${{Bool[${{Me.Buff[{spell.SpellName}]}}]}}");
				if (haveBuff) continue;

				//allow for other buff checks
				if(spell.CheckForCollection.Count>0)
				{
					foreach(var spellName in spell.CheckForCollection.Keys)
					{
						haveBuff = MQ.Query<bool>($"${{Bool[${{Me.Buff[{spellName}]}}]}}");
						if (haveBuff) goto endStackChecks;

					}
				}

				List<string> castersInGroup = E3.Bots.BotsConnected();

				foreach (var caster in spell.StackRequestTargets)
				{
					if (!castersInGroup.Contains(caster)) continue;
					//make sure they are in zone
					if (MQ.Query<Int32>($"${{Spawn[{caster}].ID}}") < 1) continue;

					Int64 timeTillNextCast;
					if (!spell.StackSpellCooldown.TryGetValue(caster, out timeTillNextCast))
					{
						timeTillNextCast = -1;
					}

					if (timeTillNextCast < Core.StopWatch.ElapsedMilliseconds)
					{
						//we can cast,maybe.

						string thingToAask = spell.CastName;
						if(!String.IsNullOrWhiteSpace(spell.StackRequestItem))
						{
							thingToAask = spell.StackRequestItem;
						}

						E3.Bots.BroadcastCommandToPerson(caster, $"/nowcast me \"{thingToAask}\" ${{Me.ID}}");
						Int64 recastDelay = spell.RecastTime;
						if (spell.StackRecastDelay>0)
						{
							recastDelay = spell.StackRecastDelay;
						}
						if (spell.StackSpellCooldown.ContainsKey(caster))
						{

							spell.StackSpellCooldown[caster] = Core.StopWatch.ElapsedMilliseconds + recastDelay;
						}
						else
						{
							spell.StackSpellCooldown.Add(caster, Core.StopWatch.ElapsedMilliseconds + recastDelay);
						}
						break;
					}
				}
				endStackChecks:
				continue;
			}
		}
		[ClassInvoke(Data.Class.All)]
		public static void Check_RaidBuffRequests()
		{

			if (E3.IsInvis) return;
			if (!e3util.ShouldCheck(ref _nextRaidBuffRequestCheckTime, _nextRaidBuffRequestCheckTimeInterval)) return;

			foreach (var spell in E3.CharacterSettings.RaidBuffRequests)
			{
				if (spell.LastRequestTimeStamp > 0)
				{
					if ((Core.StopWatch.ElapsedMilliseconds - spell.LastRequestTimeStamp) < 15000) continue;
				}

				if (_spawns.TryByName(spell.CastTarget, out var spawn))
				{
					if (!String.IsNullOrWhiteSpace(spell.Ifs))
					{
						if (!Casting.Ifs(spell))
						{
							continue;
						}
					}
					if (spawn.Distance > 199) continue;
					//self buffs!
					var inRaid = MQ.Query<bool>($"${{Raid.Member[{spell.CastTarget}]}}");

					if (inRaid)
					{
						bool hasBuff = MQ.Query<bool>($"${{Bool[${{Me.Buff[{spell.SpellName}]}}]}}");

						if (!hasBuff)
						{
							//request it then
							MQ.Cmd($"/rsay {spell.CastTarget}:{spell.SpellName}");
							spell.LastRequestTimeStamp = Core.StopWatch.ElapsedMilliseconds;
						}
					}
				}
			}

		}
		[AdvSettingInvoke]
		public static void Check_Buffs()
		{
			if (E3.IsInvis) return;
			if (Heals.IgnoreHealTargets.Count > 0) return;
			//e3util.PrintTimerStatus(_buffTimers, ref _printoutTimer, "Buff timers");
			//instant buffs have their own shouldcheck, need it snappy so check quickly.
			if(!E3.CurrentInCombat)
			{
				if (!e3util.ShouldCheck(ref _nextBuffCheck, _nextBuffCheckInterval)) return;
			}
			//using (_log.Trace("Buffs-CheckDeath"))
			{
				if (Basics.AmIDead()) return;
				if (e3util.IsManualControl() && !Basics.InCombat() && !MQ.Query<bool>("${Me.Standing}")) return;
				if (E3.IsMoving && !Assist.IsAssisting) return;
			}

			Int32 targetID = MQ.Query<Int32>("${Target.ID}");
			try
			{
				using (_log.Trace())
				{
					
					/*if you are NOT following someone, and not moving, it will buff instantly. 
					If you ARE following someone and not moving for 10+ seconds, it will buff.*/
					if (((String.IsNullOrWhiteSpace(Movement.FollowTargetName) && String.IsNullOrWhiteSpace(Movement.ChaseTargetName))) || Movement.StandingStillForTimePeriod() || Assist.IsAssisting)
					{
						if (E3.CurrentInCombat || Nukes.PBAEEnabled)
						{
							BuffBots(E3.CharacterSettings.CombatBuffs);
							BuffBots(E3.CharacterSettings.CombatPetBuffs, true);
							BuffBots(E3.CharacterSettings.CombatPetOwnerBuffs, true);
						}

						if (!E3.CurrentInCombat)
						{
							//using(_log.Trace("Buffs-Aura"))
							{
								if (!E3.ActionTaken) BuffAuras();

							}
							//using (_log.Trace("Buffs-Self"))
							{
								if (!E3.ActionTaken) BuffBots(E3.CharacterSettings.SelfBuffs);

							}
							//using (_log.Trace("Buffs-Bot"))
							{
								if (!E3.ActionTaken) BuffBots(E3.CharacterSettings.BotBuffs);

							}
							//using (_log.Trace("Buffs-Pet"))
							{
								if (!E3.ActionTaken) BuffBots(E3.CharacterSettings.PetBuffs, true);
								if (!E3.ActionTaken) BuffBots(E3.CharacterSettings.PetOwnerBuffs, true);
							}

						}
					}

				}
			}
			finally
			{
				e3util.PutOriginalTargetBackIfNeeded(targetID);
			}


		}
		[AdvSettingInvoke]
		public static void check_CombatBuffs()
		{

			if (Assist.IsAssisting || Nukes.PBAEEnabled)
			{
				Int32 targetID = MQ.Query<Int32>("${Target.ID}");

				BuffBots(E3.CharacterSettings.CombatBuffs);
				//put the target back to where it was
				e3util.PutOriginalTargetBackIfNeeded(targetID);
			}


		}
		public static void BuffInstant(List<Data.Spell> buffs)
		{
			if (E3.IsInvis) return;
			if (e3util.IsActionBlockingWindowOpen()) return;
			if (!e3util.ShouldCheck(ref _nextInstantBuffRefresh, _nextInstantRefreshTimeInterval)) return;
			//self only, instacast buffs only
			Int32 id = E3.CurrentId;

			Int32 targetID = MQ.Query<Int32>("${Target.ID}");

			if (Assist.AssistTargetID > 0)
			{
				//if we are assisting, see if we shoudl skip buffs if under manual control
				if (targetID != Assist.AssistTargetID && e3util.IsManualControl())
				{
					return;
				}
			}
			try
			{
				foreach (var spell in buffs)
				{
					bool hasBuff = MQ.Query<bool>($"${{Bool[${{Me.Buff[{spell.SpellName}]}}]}}");
					bool hasSong = false;
					if (!hasBuff)
					{
						hasSong = MQ.Query<bool>($"${{Bool[${{Me.Song[{spell.SpellName}]}}]}}");
					}

					bool hasCheckFor = false;
					bool shouldContinue = false;
					if(spell.CheckForCollection.Count>0)
					{
						foreach(var checkforItem in spell.CheckForCollection.Keys)
						{
							hasCheckFor = MQ.Query<bool>($"${{Bool[${{Me.Buff[{checkforItem}]}}]}}");
							if (hasCheckFor)
							{
								shouldContinue = true;
								break;
							}
							hasCheckFor = MQ.Query<bool>($"${{Bool[${{Me.Song[{checkforItem}]}}]}}");
							if (hasCheckFor)
							{
								shouldContinue = true;
								break;
							}
						}
						if(shouldContinue) { continue; }
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
						if (willStack && Casting.CheckMana(spell) && Casting.CheckReady(spell))
						{
							if (spell.TargetType == "Self" || spell.TargetType == "Group v1")
							{
								Casting.Cast(0, spell);

							}
							else
							{
								if (Casting.InRange(id, spell))
								{
									Casting.Cast(id, spell);
								}

							}

						}
					}
				}
			}
			finally
			{
				e3util.PutOriginalTargetBackIfNeeded(targetID);
			}

		}
	
		private static void BuffBots(List<Data.Spell> buffs, bool usePets = false)
		{
			

			foreach (var spell in buffs)
			{
				if (e3util.IsActionBlockingWindowOpen()) return;
				if (e3util.IsRezDiaglogBoxOpen()) return;
				//if it the target is one of our base class short names, check all bots and their short name type for possible targets.
				if (String.Equals(spell.CastTarget,"bots",StringComparison.OrdinalIgnoreCase))
				{
					foreach (var name in E3.Bots.BotsConnected())
					{
						if (_spawns.TryByName(name, out var s))
						{
							if (spell.ExcludedClasses.Contains(s.ClassShortName)) { continue; }
							if(spell.ExcludedNames.Contains(s.CleanName)) { continue; }	
							string previousTarget = spell.CastTarget;
							try
							{
								spell.CastTarget = name;
								//change the name
								if (BuffBots_SingleBuff(spell, usePets) == BuffBots_ReturnType.ExitOut)
								{
									return;
								}
							}
							finally
							{
								spell.CastTarget = previousTarget;
							}
						}
					}
				}
				if (String.Equals(spell.CastTarget, "gbots", StringComparison.OrdinalIgnoreCase))
				{
					foreach (var name in E3.Bots.BotsConnected())
					{
						if (_spawns.TryByName(name, out var s))
						{
							if (spell.ExcludedClasses.Contains(s.ClassShortName)) { continue; }
							if (spell.ExcludedNames.Contains(s.CleanName)) { continue; }

							Int32 groupMemberIndex = MQ.Query<Int32>($"${{Group.Member[{name}].Index}}");
							if (groupMemberIndex < 0)
							{
								//ignore it
								continue;
							}
							string previousTarget = spell.CastTarget;
							try
							{
								spell.CastTarget = name;
								//change the name
								if (BuffBots_SingleBuff(spell, usePets) == BuffBots_ReturnType.ExitOut)
								{
									return;
								}
							}
							finally
							{
								spell.CastTarget = previousTarget;
							}
						}
					}
				}
				else if (EQClasses.ClassShortNamesLookup.Contains(spell.CastTarget))
				{
					//check for class name types
					foreach (var name in E3.Bots.BotsConnected())
					{
						if (_spawns.TryByName(name, out var s))
						{
							string classShortName = s.ClassShortName;
							if (spell.CastTarget.Equals(classShortName, StringComparison.OrdinalIgnoreCase))
							{
								string previousTarget = spell.CastTarget;
								try
								{
									spell.CastTarget = name;
									//change the name
									if (BuffBots_SingleBuff(spell, usePets) == BuffBots_ReturnType.ExitOut)
									{
										return;
									}
								}
								finally
								{
									spell.CastTarget = previousTarget;
								}
							}
							
						}
					}
				}
				else
				{
					if (BuffBots_SingleBuff(spell, usePets) == BuffBots_ReturnType.ExitOut)
					{
						return;
					}
				}
			}
		}
		//this was created as a big method was broken up that used a lot of continues/returns
	    //so that we could call it in a single buff manner. kinda a hack, but works.
		public enum BuffBots_ReturnType
		{
			Continue,
			ExitOut,
		}
		private static BuffBots_ReturnType BuffBots_SingleBuff(Data.Spell spell,bool usePets = false)
		{
			if (!spell.Enabled) return BuffBots_ReturnType.Continue;

			if (spell.Debug) _log.Write($"Buffs-Spell-{spell.CastName}", Logging.LogLevels.Error);
			//using (_log.Trace($"Buffs-Spell-{spell.CastName}"))
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
								return BuffBots_ReturnType.Continue;
							}

							if (!Basics.GroupMembers.Any() || !Basics.GroupMembers.Contains(spawn.ID))
							{
								return BuffBots_ReturnType.Continue;
							}
						}
					}
				}

				if (Heals.IgnoreHealTargets.Count>1) return BuffBots_ReturnType.Continue;

				if (_spawns.TryByName(target, out s))
				{
					if (usePets && s.PetID < 1)
					{
						return BuffBots_ReturnType.Continue;
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
					if (!String.IsNullOrWhiteSpace(spell.Ifs))
					{
						if (!Casting.Ifs(spell))
						{
							//ifs failed do a 30 sec`retry

							UpdateBuffTimers(s.ID, spell, 1500, -1, true);
							return BuffBots_ReturnType.Continue;
						}
					}


					if (!Casting.InRange(s.ID, spell))
					{
						return BuffBots_ReturnType.Continue;
					}
					if (s.ID == E3.CurrentId)
					{


						bool hasCheckFor = false;
						bool shouldContinue = false;
						if (spell.CheckForCollection.Count > 0)
						{
							foreach (var checkforItem in spell.CheckForCollection.Keys)
							{
								hasCheckFor = MQ.Query<bool>($"${{Bool[${{Me.Buff[{checkforItem}]}}]}}");
								if (!hasCheckFor)
								{
									hasCheckFor = MQ.Query<bool>($"${{Bool[${{Me.Song[{checkforItem}]}}]}}");
									if (hasCheckFor)
									{
										Int64 buffDuration = MQ.Query<Int64>($"${{Me.Song[{checkforItem}].Duration}}");
										if (buffDuration < 1000)
										{
											buffDuration = 1000;
										}
										//don't let the refresh update this
										UpdateBuffTimers(s.ID, spell, 3000, buffDuration, true);
										shouldContinue = true;
										break;
									}
								}
								else
								{
									Int64 buffDuration = MQ.Query<Int64>($"${{Me.Buff[{checkforItem}].Duration}}");
									if (buffDuration < 1000)
									{
										buffDuration = 1000;
									}
									UpdateBuffTimers(s.ID, spell, 3000, buffDuration, true);
									shouldContinue = true;
									break;
								}
							}
							if (shouldContinue) { return BuffBots_ReturnType.Continue; }
						}
						//Is the buff still good? if so, skip

						if (Casting.BuffNotReady(spell)) return BuffBots_ReturnType.Continue;
						
						if (BuffTimerIsGood(spell, s, usePets))
						{
							return BuffBots_ReturnType.Continue;
						}
						bool willStack = MQ.Query<bool>($"${{Spell[{spell.SpellName}].WillLand}}");
						if (willStack && Casting.CheckMana(spell) && Casting.CheckReady(spell))
						{
							CastReturn result;
						recastSpell:
							if (spell.TargetType == "Self" || spell.TargetType == "Group v1" || spell.TargetType == "Group v2")
							{
								result = Casting.Cast(0, spell);
							}
							else
							{
								result = Casting.Cast(s.ID, spell);
							}
							if (result == CastReturn.CAST_FIZZLE)
							{
								goto recastSpell;
							}
							if (result == CastReturn.CAST_INTERRUPTED || result == CastReturn.CAST_INTERRUPTFORHEAL)
							{
								return BuffBots_ReturnType.ExitOut;
							}
							if (result != CastReturn.CAST_SUCCESS)
							{
								//possibly some kind of issue/blocking. set a 60 sec timer to try and recast later.
								UpdateBuffTimers(s.ID, spell, 60 * 1000, -1, true);
							}
							else
							{
								//lets verify what we have.
								MQ.Delay(300);
								Int64 timeLeftInMS = Casting.TimeLeftOnMyBuff(spell);
								UpdateBuffTimers(s.ID, spell, timeLeftInMS, timeLeftInMS);
							}
							return BuffBots_ReturnType.Continue;
						}
						else if (!willStack)
						{
							//won't stack don't check back for awhile, be sure to lock the timer so that it will fully play out.
							UpdateBuffTimers(s.ID, spell, 12 * 1000, -1, true);
						}
						else
						{
							//we don't have mana for this? or ifs failed? chill for 12 sec., be sure to lock the timer so that it will fully play out.
							UpdateBuffTimers(s.ID, spell, 12 * 1000, -1, true);
						}

					}
					else if (s.ID == MQ.Query<Int32>("${Me.Pet.ID}"))
					{
						//its my pet

						bool hasCheckFor = false;
						bool hasCachedCheckFor = false;
						bool shouldContinue = false;
						if (spell.CheckForCollection.Count > 0)
						{
							foreach (var checkforItem in spell.CheckForCollection.Keys)
							{
								hasCheckFor = MQ.Query<bool>($"${{Bool[${{Me.Pet.Buff[{checkforItem}]}}]}}");
								hasCachedCheckFor = MQ.Query<bool>($"${{Bool[${{Spawn[${{Me.Pet.ID}}].Buff[{checkforItem}]}}]}}");
								if (hasCheckFor || hasCachedCheckFor)
								{

									UpdateBuffTimers(s.ID, spell, 3000, -1, true);
									shouldContinue = true;
									break;
								}
							}
							if (shouldContinue) { return BuffBots_ReturnType.Continue; }
						}
						if (Casting.BuffNotReady(spell)) return BuffBots_ReturnType.Continue;
						//Is the buff still good? if so, skip
						if (BuffTimerIsGood(spell, s, usePets))
						{
							return BuffBots_ReturnType.Continue;
						}
						bool willStack = MQ.Query<bool>($"${{Spell[{spell.SpellName}].WillLandPet}}");
					recastSpell:
						if (willStack && Casting.CheckMana(spell) && Casting.CheckReady(spell))
						{
							CastReturn result;

							result = Casting.Cast(s.ID, spell);
							if (result == CastReturn.CAST_FIZZLE)
							{
								goto recastSpell;
							}
							if (result == CastReturn.CAST_INTERRUPTED || result == CastReturn.CAST_INTERRUPTFORHEAL)
							{
								return BuffBots_ReturnType.ExitOut;
							}
							if (result != CastReturn.CAST_SUCCESS)
							{
								//possibly some kind of issue/blocking. set a 120 sec timer to try and recast later.
								UpdateBuffTimers(s.ID, spell, 60 * 1000, -1, true);
							}
							else
							{
								//lets verify what we have.

								Int32 buffCount = MQ.Query<Int32>("${Me.Pet.BuffCount}");
								if (buffCount < 31)
								{
									MQ.Delay(300);
									Int64 timeLeftInMS = Casting.TimeLeftOnMyPetBuff(spell);
									UpdateBuffTimers(s.ID, spell, timeLeftInMS, timeLeftInMS);
								}
								else
								{
									UpdateBuffTimers(s.ID, spell, (spell.DurationTotalSeconds * 1000), (spell.DurationTotalSeconds * 1000), true);
								}

							}
							return BuffBots_ReturnType.ExitOut;
						}
						else if (!willStack)
						{
							//won't stack don't check back for awhile
							UpdateBuffTimers(s.ID, spell, 12 * 1000, -1, true);
						}
						else
						{
							//we don't have mana for this? or ifs failed? chill for 12 sec.
							UpdateBuffTimers(s.ID, spell, 12 * 1000, -1, true);
						}
					}
					else
					{
						//someone other than us.
						//if its a netbots, we initially do target, then have the cache refreshed
						//using a func here so that we can swap out the logic of Pet buff vs normal buffs
						Func<String, List<Int32>> findBuffList = E3.Bots.BuffList;
						if (usePets)
						{
							findBuffList = E3.Bots.PetBuffList;
						}

						bool isABot = E3.Bots.BotsConnected().Contains(spell.CastTarget, StringComparer.OrdinalIgnoreCase);

						if (isABot)
						{

							bool shouldContinue = false;
							if (spell.CheckForCollection.Count > 0)
							{
								foreach (var checkforItem in spell.CheckForCollection.Keys)
								{
									//keys are check for spell names, the value is the spell id

									bool hasCheckFor = findBuffList(spell.CastTarget).Contains(spell.CheckForCollection[checkforItem]);
									//can't check for target song buffs, be aware. will have to check netbots. 
									if (hasCheckFor)
									{
										//can't see the time, just set it for this time to recheck
										//3 seconds
										UpdateBuffTimers(s.ID, spell, 3000, -1, true);
										shouldContinue = true;
										break;
									}
								}
								if (shouldContinue)
								{
									return BuffBots_ReturnType.Continue;
								}
							}
							if (Casting.BuffNotReady(spell)) return BuffBots_ReturnType.Continue;
							//Is the buff still good? if so, skip
							if (BuffTimerIsGood(spell, s, usePets))
							{
								return BuffBots_ReturnType.Continue;
							}

							Casting.TrueTarget(s.ID);
							MQ.Delay(2000, "${Target.BuffsPopulated}");
							bool willStack = MQ.Query<bool>($"${{Spell[{spell.SpellName}].StacksTarget}}");
							if (!willStack)
							{
								UpdateBuffTimers(s.ID, spell, 15000, 15000, true);
								return BuffBots_ReturnType.Continue;
							}
						recastSpell:
							if (willStack && Casting.CheckMana(spell) && Casting.CheckReady(spell))
							{

								//E3.Bots.Broadcast($"{spell.CastTarget} is missing the buff {spell.CastName} with id:{spell.SpellID}. current list:{String.Join(",",list)}");
								//then we can cast!
								var result = Casting.Cast(s.ID, spell);
								if (result == CastReturn.CAST_FIZZLE)
								{
									goto recastSpell;
								}
								if (result == CastReturn.CAST_INTERRUPTED || result == CastReturn.CAST_INTERRUPTFORHEAL)
								{
									return BuffBots_ReturnType.ExitOut;
								}
								if (result != CastReturn.CAST_SUCCESS)
								{
									//possibly some kind of issue/blocking.
									UpdateBuffTimers(s.ID, spell, 12000, -1, true);
								}
								else
								{
									MQ.Delay(300);
									Int64 timeLeftInMS = Casting.TimeLeftOnTargetBuff(spell);
									//lets verify what we have on that target.
									UpdateBuffTimers(s.ID, spell, timeLeftInMS, timeLeftInMS);

								}
								return BuffBots_ReturnType.ExitOut;
							}
							else
							{
								//spell not ready 
								UpdateBuffTimers(s.ID, spell, 15000, 15000, true, true);

							}
						}
						else
						{
							if (Casting.BuffNotReady(spell)) return BuffBots_ReturnType.Continue;
							//Is the buff still good? if so, skip
							if (BuffTimerIsGood(spell, s, usePets))
							{
								return BuffBots_ReturnType.Continue;
							}
							if (!Casting.CheckReady(spell)) return BuffBots_ReturnType.Continue;
							//its someone not in our buff group, do it the hacky way.
							Casting.TrueTarget(s.ID);

							//greater than 0, so we don't get things like shrink that don't have a duration
							bool isShortDuration = spell.IsShortBuff;

							if (!isShortDuration || spell.CheckForCollection.Count > 0)
							{
								//we can't see the short duration buffs anyway, so no need to delay.
								MQ.Delay(2000, "${Target.BuffsPopulated}");
							}

							bool shouldContinue = false;
							if (spell.CheckForCollection.Count > 0)
							{
								foreach (var checkforItem in spell.CheckForCollection.Keys)
								{
									Int64 timeinMS = MQ.Query<Int64>($"${{Target.Buff[${{Spell[{checkforItem}]}}].Duration}}");
									if (timeinMS > 0)
									{
										//they have the check for
										UpdateBuffTimers(s.ID, spell, timeinMS, timeinMS, true);
										shouldContinue = true;
										break;
									}
								}
								if (shouldContinue) { return BuffBots_ReturnType.Continue; }
							}
							if (!isShortDuration || spell.CheckForCollection.Count > 0)
							{
								bool willStack = MQ.Query<bool>($"${{Spell[{spell.SpellName}].StacksTarget}}");
								//MQ.Write($"Will stack:{spell.SpellName}:" + willStack);
								if (!willStack)
								{
									//won't stack don't check back for awhile
									UpdateBuffTimers(s.ID, spell, 30 * 1000, -1, true);
								}
							}
							//double ifs check, so if their if included Target, we have it
							if (!String.IsNullOrWhiteSpace(spell.Ifs))
							{
								if (!Casting.Ifs(spell))
								{
									//ifs failed do a 30 sec retry, so we don't keep swapping targets
									UpdateBuffTimers(s.ID, spell, 30 * 1000, -1, true);
									return BuffBots_ReturnType.Continue;
								}
							}
							if (isShortDuration)
							{
							//we cannot do target based checks if a short duration type.

							//not one of our buffs uhh, try and cast and see if we get a non success message.
							recastSpell:
								if (Casting.CheckMana(spell) && Casting.CheckReady(spell))
								{
									var result = Casting.Cast(s.ID, spell);

									if (result == CastReturn.CAST_FIZZLE)
									{
										goto recastSpell;
									}

									if (result == CastReturn.CAST_INTERRUPTED || result == CastReturn.CAST_INTERRUPTFORHEAL)
									{
										return BuffBots_ReturnType.ExitOut;
									}
									if (result != CastReturn.CAST_SUCCESS)
									{
										//possibly some kind of issue/blocking. set a N sec timer to try and recast later.
										UpdateBuffTimers(s.ID, spell, 60 * 1000, -1, true);
									}
									else
									{
										UpdateBuffTimers(s.ID, spell, spell.DurationTotalSeconds * 1000, spell.DurationTotalSeconds * 1000, true);
									}
									return BuffBots_ReturnType.ExitOut;
								}
								return BuffBots_ReturnType.Continue;

							}
							else
							{
								Int64 timeLeftInMS = -1;
								timeLeftInMS = Casting.TimeLeftOnTargetBuff(spell);

								if (timeLeftInMS < 15000)
								{
								recastSpell:
									if (Casting.CheckMana(spell) && Casting.CheckReady(spell))
									{
										var result = Casting.Cast(s.ID, spell);
										if (result == CastReturn.CAST_FIZZLE)
										{
											goto recastSpell;
										}
										if (result == CastReturn.CAST_INTERRUPTED || result == CastReturn.CAST_INTERRUPTFORHEAL)
										{
											return BuffBots_ReturnType.ExitOut;
										}
										if (result != CastReturn.CAST_SUCCESS)
										{
											//possibly some kind of issue/blocking. set a 120 sec timer to try and recast later.
											UpdateBuffTimers(s.ID, spell, 120 * 1000, -1, true);
											return BuffBots_ReturnType.Continue;
										}
										else
										{
											if (spell.Duration > 0)
											{
												//lets verify what we have on that target.
												Casting.TrueTarget(s.ID);
												MQ.Delay(2000, "${Target.BuffsPopulated}");
												MQ.Delay(300);
												timeLeftInMS = Casting.TimeLeftOnTargetBuff(spell);
												if (timeLeftInMS < 0)
												{
													timeLeftInMS = 120 * 1000;
													UpdateBuffTimers(s.ID, spell, timeLeftInMS, timeLeftInMS, true);

												}
												else
												{
													UpdateBuffTimers(s.ID, spell, timeLeftInMS, timeLeftInMS);
												}

												return BuffBots_ReturnType.Continue;
											}
											else
											{   //stuff like shrink
												//UpdateBuffTimers(s.ID, spell, Int32.MaxValue, true);
												return BuffBots_ReturnType.Continue;
											}
										}
									}
								}
								else
								{
									UpdateBuffTimers(s.ID, spell, timeLeftInMS, timeLeftInMS);
									return BuffBots_ReturnType.Continue;
								}
							}
						}
					}
				}
			}
			return BuffBots_ReturnType.Continue;
		}
		
		private static Dictionary<string, CharacterBuffs> _characterBuffs = new Dictionary<string, CharacterBuffs>();

		private static bool BuffTimerIsGood_CheckLocalData(Data.Spell spell, Spawn s, Func<Data.Spell,Int64> TimeLeftFunction, bool updateTImers = false)
		{
		   //its US!

			Int64 timeinMS = TimeLeftFunction(spell);
			if (spell.Debug) _log.Write($"Buffs-Spell-{spell.CastName} timeleft:{timeinMS}", Logging.LogLevels.Error);
			if (timeinMS < 1)
			{
				return false;
			}
			if (spell.MinDurationBeforeRecast > 0)
			{

				if (timeinMS < spell.MinDurationBeforeRecast)
				{
					return false;
				}

			}
			if (updateTImers && timeinMS > 0)
			{
				UpdateBuffTimers(s.ID, spell, timeinMS, timeinMS);
			}
			return true;
			
		}

		private static bool BuffTimerIsGood_CheckBotData(Data.Spell spell, Spawn s, bool usePets)
		{
			string keyToUse = "${Me.BuffInfo}";
			if (usePets)
			{
				keyToUse = "${Me.PetBuffInfo}";
			}
			if (spell.Debug) _log.Write($"Buffs-Spell-{spell.CastName} Checking bot data", Logging.LogLevels.Error);
			string keyNameToUse = s.Name; //to get the pet or the owner
			if(!NetMQServer.SharedDataClient.TopicUpdates.ContainsKey(spell.CastTarget))
			{
				//don't have it registered, asssume good for now.
				return true;
			}
			var userTopics = NetMQServer.SharedDataClient.TopicUpdates[spell.CastTarget];
			//check to see if it has been filled out yet.
			if (!userTopics.ContainsKey(keyToUse))
			{
				//don't have the data yet kick out and assume everything is ok.
				return true;
			}
			//we have the data, lets check on it. 
			//we don't have it in our memeory, so lets add it
			lock (userTopics[keyToUse])
			{
				if (!_characterBuffs.ContainsKey(keyNameToUse))
				{
					var buffInfo = CharacterBuffs.Aquire();
					e3util.BuffInfoToDictonary(userTopics[keyToUse].Data, buffInfo.BuffDurations);
					buffInfo.LastUpdate = userTopics[keyToUse].LastUpdate;
					_characterBuffs.Add(keyNameToUse, buffInfo);
				}
				//do we have updated information that is newer than what we already have?
				if (userTopics[keyToUse].LastUpdate > _characterBuffs[keyNameToUse].LastUpdate)
				{
					//new info, lets update!
					var buffInfo = _characterBuffs[keyNameToUse];
					e3util.BuffInfoToDictonary(userTopics[keyToUse].Data, buffInfo.BuffDurations);
					buffInfo.LastUpdate = userTopics[keyToUse].LastUpdate;
					
				}
			}
				
			//done with updates, now lets check the data.
			//pets have a cap of MaxPetBuffSlots if equal to or greater, we just can't buff because well.. we can't see it!
			if (usePets && _characterBuffs[keyNameToUse].BuffDurations.Count >= e3util.MaxPetBuffSlots)
			{
				return true;//assume that its on we can't see past the buff count of 30
			}
			if (_characterBuffs[keyNameToUse].BuffDurations.ContainsKey(spell.SpellID))
			{
				
				//check if the duratino is ok
				Int64 timeLeft = _characterBuffs[keyNameToUse].BuffDurations[spell.SpellID];
				if (timeLeft <= (spell.MinDurationBeforeRecast))
				{
					return false;
				}
				else
				{
					UpdateBuffTimers(s.ID, spell, timeLeft, timeLeft);
					return true;
				}
			}
			//doesn't have the buff, or its expired
			return false;
		}
		public static bool BuffTimerIsGood(Data.Spell spell, Spawn s, bool usePets)
		{
			SpellTimer st;
			if (_buffTimers.TryGetValue(s.ID, out st))
			{
				if (spell.Debug) _log.Write($"Buffs-Spell-{spell.CastName} have buff timer", Logging.LogLevels.Error);
				Int64 timestamp;
				if (st.Timestamps.TryGetValue(spell.SpellID, out timestamp))
				{
					//our timer says the buff is still good, but lets make sure in case of dispel.
					if (Core.StopWatch.ElapsedMilliseconds < timestamp)
					{
						//is this timestamp locked?
						if(st.Lockedtimestamps.TryGetValue(spell.SpellID,out var lockedtimestamp))
						{
							if(lockedtimestamp<Core.StopWatch.ElapsedMilliseconds)
							{
								//means this lock is no longer valuid
								st.Lockedtimestamps.Remove(spell.SpellID);
							}
							else
							{
								//we have a locked time stamp, say its still good
								return true;
							}
						}

						//easy to check on just ourself
						if (s.ID == E3.CurrentId)
						{
							return BuffTimerIsGood_CheckLocalData(spell, s,Casting.TimeLeftOnMyBuff);
							
						}
						else if (usePets && s.ID == MQ.Query<Int32>("${Me.Pet.ID}"))
						{
							return BuffTimerIsGood_CheckLocalData(spell, s, Casting.TimeLeftOnMyPetBuff);
						}
						else
						{   //if a bot, check to see if the buff still exists
							bool isABot = E3.Bots.BotsConnected().Contains(s.CleanName, StringComparer.OrdinalIgnoreCase);
							if (isABot)
							{
								//register the user to get their buff data if its not already there
								return BuffTimerIsGood_CheckBotData(spell,s, usePets);
							}
							else
							{
								//its not part of our bot network, we just have to assume that its good.
								return true;
							}
						}
					}
					else
					{
						//our data shows that the time has elapsed, lets be sure.
						if (s.ID == E3.CurrentId)
						{   //its US!

							return BuffTimerIsGood_CheckLocalData(spell, s, Casting.TimeLeftOnMyBuff,true);
						}
						else if (usePets && s.ID == MQ.Query<Int32>("${Me.Pet.ID}"))
						{
							return BuffTimerIsGood_CheckLocalData(spell, s, Casting.TimeLeftOnMyPetBuff,true);
						}
						else if (E3.Bots.BotsConnected().Contains(s.CleanName, StringComparer.OrdinalIgnoreCase))
						{
							return BuffTimerIsGood_CheckBotData(spell, s, usePets);
						}
						else
						{
							//its not a bot or our pet or a bots pet. someone else.
							if (Casting.TrueTarget(s.ID))
							{
								MQ.Delay(2000, "${Target.BuffsPopulated}");
								Int64 timeinMS = Casting.TimeLeftOnTargetBuff(spell);
								if (timeinMS < 1)
								{
									//buff doesn't exist
									return false;
								}
								if (timeinMS <= (spell.MinDurationBeforeRecast))
								{
									return false;
								}
								if (timeinMS > 0)
								{
									UpdateBuffTimers(s.ID, spell, timeinMS, timeinMS);
									return true;
								}
							}
						}
					}
				}
				else
				{
					//we have an entry for the mob but no entry for the spell ID in question
					//so we have to create one. 
					if (s.ID == E3.CurrentId)
					{   //its US!

						return BuffTimerIsGood_CheckLocalData(spell, s, Casting.TimeLeftOnMyBuff, true);

					}
					else if (usePets && s.ID == MQ.Query<Int32>("${Me.Pet.ID}"))
					{
						//is our pet
						return BuffTimerIsGood_CheckLocalData(spell, s, Casting.TimeLeftOnMyPetBuff, true);

					}
					else if (E3.Bots.BotsConnected().Contains(s.CleanName, StringComparer.OrdinalIgnoreCase))
					{
						//bot
						return BuffTimerIsGood_CheckBotData(spell, s, usePets);
					}
					else
					{
						// someone else
						// by targeting and getting the information

						if (Casting.TrueTarget(s.ID))
						{
							MQ.Delay(2000, "${Target.BuffsPopulated}");
							Int64 timeinMS = Casting.TimeLeftOnTargetBuff(spell);
							if (timeinMS < 1)
							{
								//buff doesn't exist
								return false;
							}
							if (timeinMS <= (spell.MinDurationBeforeRecast))
							{
								return false;
							}
							UpdateBuffTimers(s.ID, spell, timeinMS, timeinMS);


						}
					}
					return true;
				}
			}
			else
			{
				if (s.ID == E3.CurrentId)
				{   //its US!

					return BuffTimerIsGood_CheckLocalData(spell, s, Casting.TimeLeftOnMyBuff, true);

				}
				else if (usePets && s.ID == MQ.Query<Int32>("${Me.Pet.ID}"))
				{
					//is our pet
					return BuffTimerIsGood_CheckLocalData(spell, s, Casting.TimeLeftOnMyPetBuff, true);
				}
				else if (E3.Bots.BotsConnected().Contains(spell.CastTarget, StringComparer.OrdinalIgnoreCase))
				{
					return BuffTimerIsGood_CheckBotData(spell, s, usePets);
				}
				else
				{
					// by targeting and getting the information
					if (Casting.TrueTarget(s.ID))
					{
						MQ.Delay(2000, "${Target.BuffsPopulated}");
						Int64 timeinMS = Casting.TimeLeftOnTargetBuff(spell);
						if (timeinMS < 1)
						{
							//buff doesn't exist
							return false;
						}
						if (timeinMS <= (spell.MinDurationBeforeRecast))
						{
							return false;
						}
						UpdateBuffTimers(s.ID, spell, timeinMS, timeinMS);
					}
				}
				return true;
			}
			return false;
		}
		private static void BuffAuras()
		{
			if (!E3.CharacterSettings.Buffs_CastAuras) return;
			if (e3util.IsActionBlockingWindowOpen()) return;


			if (E3.CharacterSettings.Buffs_Auras.Count > 0)
			{
				_selectAura = E3.CharacterSettings.Buffs_Auras[0];
			}

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
							return;
				}

				//need to put on new aura
				Int32 meID = E3.CurrentId;
				if (_selectAura.CastType == CastingType.Spell)
				{
					//this is a spell, need to mem, then cast. 
					if (Casting.CheckMana(_selectAura) && Casting.CheckReady(_selectAura))
					{
						Casting.Cast(meID, _selectAura);
					}


				}
				else if (_selectAura.CastType == CastingType.Disc)
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
			"Aura of the Artist",
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
				_nextBotCacheCheckTime = Core.StopWatch.ElapsedMilliseconds + _nextBotCacheCheckTimeInterval;
			}
		}


		public static void ClearBuffTimers()
		{
			foreach(var pair in _buffTimers)
			{

				pair.Value.Dispose();
			}
			_buffTimers.Clear();

		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="mobid"></param>
		/// <param name="spell"></param>
		/// <param name="timeLeftInMS"></param>
		/// <param name="locked">Means the buff cache cannot override it</param>
		private static void UpdateBuffTimers(Int32 mobid, Data.Spell spell, Int64 timeLeftInMS, Int64 realDurationLeft, bool locked = false, bool ignoreRealDuration = false)
		{
			SpellTimer s;
			//if we have no time left, as it was not found, just set it to 0 in ours

			if (_buffTimers.TryGetValue(mobid, out s))
			{
				if (!s.Timestamps.ContainsKey(spell.SpellID))
				{
					s.Timestamps.Add(spell.SpellID, 0);
					s.TimestampBySpellDuration.Add(spell.SpellID, 0);
				}

				s.Timestamps[spell.SpellID] = Core.StopWatch.ElapsedMilliseconds + timeLeftInMS;
				if (!ignoreRealDuration)
				{
					s.TimestampBySpellDuration[spell.SpellID] = Core.StopWatch.ElapsedMilliseconds + realDurationLeft;

				}
				if (locked)
				{
					if (!s.Lockedtimestamps.ContainsKey(spell.SpellID))
					{
						s.Lockedtimestamps.Add(spell.SpellID, Core.StopWatch.ElapsedMilliseconds + timeLeftInMS);
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
				ts.TimestampBySpellDuration.Add(spell.SpellID, spell.DurationTotalSeconds * 1000);
				_buffTimers.Add(mobid, ts);
				if (locked)
				{
					if (!ts.Lockedtimestamps.ContainsKey(spell.SpellID))
					{
						ts.Lockedtimestamps.Add(spell.SpellID, Core.StopWatch.ElapsedMilliseconds + timeLeftInMS);
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
		[ClassInvoke(Data.Class.All)]
		public static void Check_BuffBando()
		{

			//ask our group for DI from DI sticks.
			if (!e3util.ShouldCheck(ref _nextBandoBuffCheck, _nextBandoBuffCheckInterval)) return;

			if (!E3.CharacterSettings.BandoBuff_Enabled) return;
			if (String.IsNullOrWhiteSpace(E3.CharacterSettings.BandoBuff_BuffName)) return;
			if (String.IsNullOrWhiteSpace(E3.CharacterSettings.BandoBuff_BandoName)) return;
			if (String.IsNullOrWhiteSpace(E3.CharacterSettings.BandoBuff_BandoNameWithoutBuff)) return;

			bool hasBuff = true;
			bool buffWillStack = MQ.Query<bool>($"${{Spell[{E3.CharacterSettings.BandoBuff_BuffName}].WillLand}}");

			if (E3.CharacterSettings.BandoBuff_BuffName != String.Empty && buffWillStack)
			{
				hasBuff = MQ.Query<bool>($"${{Bool[${{Me.Buff[{E3.CharacterSettings.BandoBuff_BuffName}]}}]}}");
				if (!hasBuff)
				{
					hasBuff = MQ.Query<bool>($"${{Bool[${{Me.Song[{E3.CharacterSettings.BandoBuff_BuffName}]}}]}}");
				}
			}

			//Debuff code, only activate if either, we cannot land the buff configured or it doesn't exist.
			//hasbuff is true by default
			if (hasBuff && Basics.InCombat() && MQ.Query<Int32>("${Target.ID}") > 0)
			{
				bool hasDebuff = MQ.Query<bool>($"${{Bool[${{Target.Buff[{E3.CharacterSettings.BandoBuff_DebuffName}]}}]}}");
				if (!hasDebuff)
				{
					bool bandoWithoutDeBuffIsActive = MQ.Query<bool>($"${{Me.Bandolier[{E3.CharacterSettings.BandoBuff_BandoNameWithoutDeBuff}].Active}}");

					if(!bandoWithoutDeBuffIsActive)
					{
						bool willStack = MQ.Query<bool>($"${{Spell[{E3.CharacterSettings.BandoBuff_DebuffName}].StacksTarget}}");

						if (willStack)
						{
							E3.Bots.Broadcast($"Swapping to {E3.CharacterSettings.BandoBuff_BandoNameWithoutDeBuff}");

							MQ.Cmd($"/bando activate {E3.CharacterSettings.BandoBuff_BandoNameWithoutDeBuff}");
							return;
						}
					}
				}
			}

			bool bandoWhenBuffIsActive = MQ.Query<bool>($"${{Me.Bandolier[{E3.CharacterSettings.BandoBuff_BandoName}].Active}}");
			bool bandoWithoutBuffIsActive = MQ.Query<bool>($"${{Me.Bandolier[{E3.CharacterSettings.BandoBuff_BandoNameWithoutBuff}].Active}}");

			if (hasBuff)
			{
				if (!bandoWhenBuffIsActive)
				{
					E3.Bots.Broadcast($"Swapping to {E3.CharacterSettings.BandoBuff_BandoName}");
					MQ.Cmd($"/bando activate {E3.CharacterSettings.BandoBuff_BandoName}");
				}
			}
			else
			{
				if (!bandoWithoutBuffIsActive)
				{
					E3.Bots.Broadcast($"Swapping to {E3.CharacterSettings.BandoBuff_BandoNameWithoutBuff}");

					MQ.Cmd($"/bando activate {E3.CharacterSettings.BandoBuff_BandoNameWithoutBuff}");
				}
			}
		}
		private static Int64 _nextLazEncEpicBuffCheck = 0;
		private static Int64 _nextLazEncEpicBuffCheckInterval = 1000;
		[ClassInvoke(Data.Class.All)]
		public static void CheckLazEncEpicBuff()
		{
			if (!e3util.ShouldCheck(ref _nextLazEncEpicBuffCheck, _nextLazEncEpicBuffCheckInterval)) return;

			if (!E3.CharacterSettings.ManaStone_UseForLazarusEncEpicBuff) return;

			if (e3util.IsEQLive()) return;

			if (e3util.IsEQEMU() && E3.ServerName == "Lazarus")
			{
				if (MQ.Query<bool>("${Me.Invis}")) return;
				if (E3.CharacterSettings.ManaStone_ExceptionZones.Contains(Zoning.CurrentZone.ShortName)) return;

				//using (_log.Trace())
				{
					if (E3.IsInvis) return;
					if (Basics.AmIDead()) return;
					if (e3util.IsEQLive()) return;
					//if (!Basics.InCombat()) return;

					/*
					 *  ${Bool[${Math.Calc[${Me.Song[Mana Convergence IV].Duration}<10000]}]})
					 */
					Int32 pctAggro = MQ.Query<Int32>("${Me.PctAggro}");
					if (pctAggro > 99) return;
					bool hasManaStone = MQ.Query<bool>("${Bool[${FindItem[=Manastone]}]}");
					bool hasSongBuffv1 = MQ.Query<bool>("${Bool[${Me.Song[Mana Convergence I]}]}");
					bool hasSongBuffv2 = MQ.Query<bool>("${Bool[${Me.Song[Mana Convergence II]}]}");
					bool hasSongBuffv3 = MQ.Query<bool>("${Bool[${Me.Song[Mana Convergence III]}]}");
					bool hasSongBuffv4 = MQ.Query<bool>("${Bool[${Me.Song[Mana Convergence IV]}]}");
					Int32 songv4MaxDuration = 8000;

					Int64 Songv4Duration = 0;
					if (hasSongBuffv4)
					{
						Songv4Duration = MQ.Query<Int32>("${Me.Song[Mana Convergence IV].Duration}");

					}
					if (!hasManaStone) return;
					if (!(hasSongBuffv1 || hasSongBuffv2 || hasSongBuffv3 || hasSongBuffv4)) return;



					int pctMana = MQ.Query<int>("${Me.PctMana}");
					var pctHps = MQ.Query<int>("${Me.PctHPs}");
					int currentHps = MQ.Query<int>("${Me.CurrentHPs}");
					int minHP = 70;

					if (E3.CharacterSettings.ManaStone_ExceptionZones.Contains(Zoning.CurrentZone.ShortName)) return;

					if (E3.CharacterSettings.ManaStone_ExceptionMQQuery.Count > 0)
					{
						foreach (var query in E3.CharacterSettings.ManaStone_ExceptionMQQuery)
						{
							if (String.IsNullOrEmpty(query)) continue;

							if (Casting.Ifs(query))
							{
								return;
							}
						}
					}

					pctHps = MQ.Query<int>("${Me.PctHPs}");
					if (pctHps < minHP) return;


					string manastoneName = "Manastone";
					Int32 totalClicksToTry = 5;
					Int32 delayBetweenClicks = 20;
					Int32 maxLoop = 25;
					if (hasManaStone && (hasSongBuffv1 || hasSongBuffv2 || hasSongBuffv3 || hasSongBuffv4))
					{
						string manastoneCommand = $"/useitem \"{manastoneName}\"";

						if (hasSongBuffv4)
						{
							if (Songv4Duration > songv4MaxDuration) return;
						}

						MQ.Write("\agUsing Manastone for Laz Enc Epic...");
						pctHps = MQ.Query<int>("${Me.PctHPs}");
						pctMana = MQ.Query<int>("${Me.PctMana}");
						int currentLoop = 0;
						while (pctHps > minHP && (hasSongBuffv1 || hasSongBuffv2 || hasSongBuffv3 || hasSongBuffv4))
						{
							currentLoop++;
							int currentMana = MQ.Query<int>("${Me.CurrentMana}");

							if (hasSongBuffv4)
							{
								if (Songv4Duration > songv4MaxDuration) return;
							}

							for (int i = 0; i < totalClicksToTry; i++)
							{
								MQ.Cmd(manastoneCommand);
							}
							//allow mq to have the commands sent to the server
							MQ.Delay(delayBetweenClicks);
							NetMQServer.SharedDataClient.ProcessCommands();
							PubClient.ProcessRequests();
							if (EventProcessor.CommandListQueueHasCommand("/followme"))
							{
								return;
							}
							if (EventProcessor.CommandListQueueHasCommand("/chaseme"))
							{
								return;
							}
							if (MQ.Query<bool>("${Me.Invis}")) return;
							if ((E3.CurrentClass & Class.Priest) == E3.CurrentClass && Basics.InCombat())
							{
								if (Heals.SomeoneNeedsHealing(null, currentMana, pctMana))
								{
									return;
								}
							}
							if (currentLoop > maxLoop)
							{
								return;
							}
							e3util.YieldToEQ();
							pctHps = MQ.Query<int>("${Me.PctHPs}");
							pctMana = MQ.Query<int>("${Me.PctMana}");
							hasSongBuffv1 = MQ.Query<bool>("${Bool[${Me.Song[Mana Convergence I]}]}");
							hasSongBuffv2 = MQ.Query<bool>("${Bool[${Me.Song[Mana Convergence II]}]}");
							hasSongBuffv3 = MQ.Query<bool>("${Bool[${Me.Song[Mana Convergence III]}]}");
							hasSongBuffv4 = MQ.Query<bool>("${Bool[${Me.Song[Mana Convergence IV]}]}");
							if (hasSongBuffv4)
							{
								Songv4Duration = MQ.Query<Int32>("${Me.Song[Mana Convergence IV].Duration}");
							}
						}
					}
				}


			}


		}

	}
}
