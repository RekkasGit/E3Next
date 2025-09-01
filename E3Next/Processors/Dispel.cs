using E3Core.Settings;
using E3Core.Utility;
using MonoCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace E3Core.Processors
{
    public static class Dispel
    {
        public static Logging _log = E3.Log;
        private static IMQ MQ = E3.MQ;
        private static ISpawns _spawns = E3.Spawns;
        private static Int64 _nextDispelCheck = 0;
		[ExposedData("Dispel", "DispelCheckInterval")]
		private static Int64 _nextDispelCheckInterval = 500;

		public static HashSet<Int32> _mobsToDispel = new HashSet<int>();

		[SubSystemInit]
		public static void Init_Dispel()
		{

			e3util.RegisterCommandWithTarget("/e3dispel", DispelOn);
			EventProcessor.RegisterCommand("/e3dispeloff", (x) =>
			{
				_mobsToDispel.Clear();
				if (x.args.Count == 0)
				{
					//we are telling people to back off
					E3.Bots.BroadcastCommandToGroup($"/e3dispeloff all", x);
				}

			});
		}


        public static void Reset()
        {
            _mobsToDispel.Clear();
        }

		public static void DispelOn(Int32 mobid)
		{
			if (!_mobsToDispel.Contains(mobid))
			{
				_mobsToDispel.Add(mobid);
			}
		}

		[ClassInvoke(Data.Class.All)]
        public static void CheckDispel()
        {

            if (E3.CharacterSettings.Dispels.Count == 0) return;
            
			if(!E3.CurrentInCombat)
			{
				if (!e3util.ShouldCheck(ref _nextDispelCheck, _nextDispelCheckInterval)) return;
			}
			if (Assist.IsAssisting)
			{
				CheckDispelCurrentTarget();
			}
			if (_mobsToDispel.Count > 0)
			{
				//lets not do this if in manual control
				if (!e3util.IsManualControl())
				{
					foreach (var target in _mobsToDispel.ToList())
					{
						if (!_spawns.TryByID(target, out var s))
						{
							//get rid of dead mobs
							_mobsToDispel.Remove(target);
						}
						//most dispels are 200 ranage, ignore if its too far away, also check line of sight to th emob.
						if (s.Distance3D<200 && MQ.Query<bool>($"${{Spawn[id {s.ID}].LineOfSight}}") && Casting.TrueTarget(s.ID))
						{
							if (!CheckDispelCurrentTarget())
							{
								E3.Bots.Broadcast($"\agRemoving mob: [\ap{s.CleanName}\ag] (\aw{s.ID}\ag) from Dispel list, no longer has dispelable buffs.");
								_mobsToDispel.Remove(target);
							}
						}
					}
				}
			}
		}
		private static bool CheckDispelCurrentTarget()
		{

			//person in manual control and they are not on the assist target, chill.
			Int32 targetId = MQ.Query<Int32>("${Target.ID}");
			if (targetId != Assist.AssistTargetID && e3util.IsManualControl())
			{
				return false;
			}
			if (Casting.TrueTarget(targetId))
			{
				MQ.Delay(1000, "${Target.BuffsPopulated}");
				//let the game process to make sure we have a valid beneficial
				if (MQ.Query<bool>("${Target.Beneficial.ID}"))
				{
					Int32 buffCount = 55;
					for (Int32 i = 1; i <= buffCount; i++)
					{
						bool beneficial = MQ.Query<bool>($"${{Target.Buff[{i}].Beneficial}}");
						if (beneficial)
						{
							if (MQ.FeatureEnabled(MQFeature.TLO_Dispellable))
							{
								bool buffDispellable = MQ.Query<bool>($"${{Target.Buff[{i}].Dispellable}}");
								if (!buffDispellable) continue;
							}
							else
							{
								string buffCategory = MQ.Query<string>($"${{Target.Buff[{i}].Category}}");
								if (buffCategory == "Disciplines") continue;
							}

							string buffName = MQ.Query<string>($"${{Target.Buff[{i}]}}");
							Int32 buffID = MQ.Query<Int32>($"${{Target.Buff[{i}].ID}}");
							//now to check if its beneifical for real
							foreach (var ignore in E3.CharacterSettings.DispelIgnore)
							{
								if (ignore.SpellID == buffID)
								{
									beneficial = false;
									break;
								}
							}
							if (!beneficial) continue;

							if (beneficial)
							{
								foreach (var spell in E3.CharacterSettings.Dispels)
								{
									//now have it as a target, need to check its beneficial buffs
									if (Casting.InRange(targetId,spell) && Casting.CheckMana(spell) && Casting.CheckReady(spell))
									{
										Casting.Cast(0, spell);
									}
								}
								//returning true so that it can be called again and eventually return false when there are no more buffs on the mob
								return true;
							}
						}
					}
				}
			}
			return false;
		}
	}
}
