using E3Core.Data;
using E3Core.Settings;
using E3Core.Utility;
using MonoCore;
using System;
using System.Collections.Generic;
namespace E3Core.Processors
{
	public static class Charm
	{

		public static Logging _log = E3.Log;
		private static IMQ MQ = E3.MQ;
		private static ISpawns _spawns = E3.Spawns;


		private static Int64 _charmTimer = 0;
		private static int _charmTargetId = 0;

		private static long _nextCharmCheck = 0;
		private static long _nextCharmRefreshTimeInterval = 500;
		private static Int32 _charmDebuffDelay = 4000;

		[SubSystemInit]
		public static void Init()
		{
			InitCharm();
		}
		private static void InitCharm()
		{

			EventProcessor.RegisterCommand("/charmon", x =>
			{



				if (E3.CharacterSettings.Charm_CharmSpell == null)
				{
					E3.Bots.Broadcast($"\agCharm spell not set in INI file.");
					return;
				}
				if (!IsCheckCharmConfigured())
				{
					E3.Bots.Broadcast($"\agcheck_Charm not found in the advanced ini for this class.");
					return;
				}

				if (x.args.Count > 0)
				{
					//they supplied a target
					Int32.TryParse(x.args[0], out _charmTargetId);
				}
				if (_charmTargetId == 0)
				{
					_charmTargetId = MQ.Query<int>("${Target.ID}");

				}
				if (_charmTargetId == 0 || !CharmTargetIsValid())
				{
					E3.Bots.Broadcast($"\agNot a valid charm target.");
					return;
				}

				E3.Bots.Broadcast($"\agSetting ${{Spawn[{_charmTargetId}].CleanName}} as charm target");

				CharmProcess();
			});

			EventProcessor.RegisterCommand("/charmoff", x => _charmTargetId = 0);

			EventProcessor.RegisterEvent("ZonedCharm", @"You have entered (.+)\.", (x) =>
			{
				//means we have zoned.
				_charmTargetId = 0;
			});

		}
		private static bool IsCheckCharmConfigured()
		{
			List<string> _methodsToInvokeAsStrings;
			if (AdvancedSettings.ClassMethodsAsStrings.TryGetValue(E3.CurrentShortClassString, out _methodsToInvokeAsStrings))
			{
				foreach (var methodName in _methodsToInvokeAsStrings)
				{
					if (String.Compare(methodName, "check_charm", true) == 0)
					{
						return true;
					}
				}
			}
			return false;
		}

		private static void CharmProcess()
		{
			if(E3.CharacterSettings.Charm_CharmSpell==null) return;

			if (MQ.Query<int>("${Me.Pet.ID}") == _charmTargetId) return;
			//enchanter names are invisable men, should probably just change this to petid for other charmers
			if (string.Equals(MQ.Query<string>("${Me.Pet.Race.Name}"), "Invisible Man"))
			{
				MQ.Cmd("/pet get lost");
				MQ.Delay(1000, "!${Me.Pet.ID}");
			}
			if (E3.CharacterSettings.Charm_PeelTank != String.Empty)
			{
				foreach (var spell in E3.CharacterSettings.Charm_PeelTankAggroAbility)
				{
					E3.Bots.BroadcastCommandToPerson(E3.CharacterSettings.Charm_PeelTank, $"/nowcast me \"{spell.CastName}\" {_charmTargetId}");
				}
			}

			E3.Bots.Broadcast($"\agPreparing to charm ${{Spawn[{_charmTargetId}].CleanName}}");
			bool debuffsAreCasting = false;
			if (E3.CharacterSettings.Charm_PeelSnarePerson != String.Empty) 
			{
				foreach(var spell in E3.CharacterSettings.Charm_PeelSnareSpell)
				{
					E3.Bots.BroadcastCommandToPerson(E3.CharacterSettings.Charm_PeelSnarePerson, $"/nowcast me \"{spell.CastName}\" {_charmTargetId}");
					debuffsAreCasting = true;
				}
			}
			//in case the mage is the debuff + pet peel, issue pet attack before debuff
			if (E3.CharacterSettings.Charm_PeelPetOwner != String.Empty) E3.Bots.BroadcastCommandToPerson(E3.CharacterSettings.Charm_PeelPetOwner, $"/pet attack {_charmTargetId}");

			if (E3.CharacterSettings.Charm_PeelDebuffPerson != String.Empty)
			{
				foreach(var spell in E3.CharacterSettings.Charm_PeelDebuffSpells)
				{
					E3.Bots.BroadcastCommandToPerson(E3.CharacterSettings.Charm_PeelDebuffPerson, $"/nowcast me \"{spell.CastName}\" {_charmTargetId}");
					debuffsAreCasting = true;
				}
			}
			if (E3.CharacterSettings.Charm_PeelHealer != String.Empty) 
			{
				foreach (var spell in E3.CharacterSettings.Charm_PeelHealerHeal)
				{
					E3.Bots.BroadcastCommandToPerson(E3.CharacterSettings.Charm_PeelHealer, $"/nowcast me \"{spell.CastName}\" {E3.CurrentId}");

				}
			}

			MQ.Cmd("/dropinvis");
			MQ.Cmd($"/face fast id {_charmTargetId}");
			MQ.Cmd("/beep");
			if (E3.CharacterSettings.Charm_PeelTank != String.Empty)
			{
				E3.Bots.BroadcastCommandToPerson(E3.CharacterSettings.Charm_PeelTank, "/popup Charm Break!");

			}
			Casting.TrueTarget(_charmTargetId);

			foreach (var spell in E3.CharacterSettings.Charm_CharmOhShitSpells)
			{
				if (!Casting.Ifs(spell)) continue;

				if (!Casting.CheckReady(spell)) continue;
				var result = Casting.Cast(_charmTargetId, spell);
				if (result != CastReturn.CAST_RESIST) break;
			}
			foreach (var spell in E3.CharacterSettings.Charm_SelfDebuffSpells)
			{
				if (!Casting.Ifs(spell)) continue;

				Casting.Cast(_charmTargetId, spell);
			}



			E3.Bots.Broadcast($"\agWaiting 4s for debuffs to happen");
			if(debuffsAreCasting)
			{
				MQ.Delay(_charmDebuffDelay);

			}
			E3.Bots.Broadcast($"\agDebuffs should have landed; attempting to charm");
			if (Casting.CheckReady(E3.CharacterSettings.Charm_CharmSpell))
			{
				var castResult = Casting.Cast(_charmTargetId, E3.CharacterSettings.Charm_CharmSpell);
				MQ.Delay(200);
				var petId = MQ.Query<int>("${Me.Pet.ID}");

				if (petId > 0)
				{
					_charmTimer = Core.StopWatch.ElapsedMilliseconds;
					MQ.Cmd("/pet back off", 300);
					MQ.Cmd("/pet follow", 300);
					E3.Bots.Broadcast($"\agSuccessfully charmed ${{Spawn[{_charmTargetId}].CleanName}}; let's hope it lasts longer than last time");
				}
			}
		}
		private static bool CharmTargetIsValid()
		{
			Spawn s;
			if (!E3.Spawns.TryByID(_charmTargetId, out s))
			{
				return false;
			}
			if (s.TypeDesc == "Corpse")
			{
				return false;
			}
			return true;
		}

		[AdvSettingInvoke]
		public static void check_Charm()
		{
			if (E3.CharacterSettings.Charm_CharmSpell == null) return;

			if (_charmTargetId == 0) return;
			if (!e3util.ShouldCheck(ref _nextCharmCheck, _nextCharmRefreshTimeInterval)) return;

			if (!CharmTargetIsValid())
			{
				
				// it's dead jim
				E3.Bots.Broadcast($"\arDisabling charm; {_charmTargetId} is no longer a valid npc");
				MQ.Cmd("/beep");
				_charmTargetId = 0;
				return;
			}
			Int32 petid = MQ.Query<int>("${Me.Pet.ID}");
			if (petid < 1)
			{
				CharmProcess();
				return;
			}
			if (MQ.Query<int>("${Me.Pet.ID}") > 0)
			{
				foreach (var buff in E3.CharacterSettings.Charm_BadPetBuffs)
				{
					var buffIndex = MQ.Query<int>($"${{Me.Pet.Buff[{buff.SpellName}]}}");
					if (buffIndex > 0)
					{
						MQ.Cmd($"/notify PIW_BuffWindow PIW_PetBuff{buffIndex - 1}_Button leftmouseup");
					}
				}
			}
			if (MQ.Query<int>("${Me.Pet.ID}") == _charmTargetId)
			{
				if (MQ.Query<bool>($"${{Bool[${{Me.PetBuff[{E3.CharacterSettings.Charm_CharmSpell.CastName}]}}]}}"))
				{
					Int32 charmDuration = MQ.Query<Int32>($"${{Pet.BuffDuration[{E3.CharacterSettings.Charm_CharmSpell.CastName}].TotalSeconds}}");
					if (charmDuration > 60)
					{
						if (_charmTimer - Core.StopWatch.ElapsedMilliseconds < 18)
						{
							// E3.Bots.Broadcast($"My charm fades in {charmDuration} sec");
							_charmTimer = Core.StopWatch.ElapsedMilliseconds;
						}
					}
					else if (charmDuration <= 30 && charmDuration > 9)
					{

						E3.Bots.Broadcast($"\ag My charm fades in {charmDuration} sec");
						_charmTimer = Core.StopWatch.ElapsedMilliseconds;

					}
					else if (charmDuration <= 9)
					{
						E3.Bots.Broadcast($"\ar My charm fades in {charmDuration} sec");

					}
				}
			}
		}

	}
}
