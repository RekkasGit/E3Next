using E3Core.Classes;
using E3Core.Data;
using E3Core.Settings;
using E3Core.Utility;
using MonoCore;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;

namespace E3Core.Processors
{
    public static class ClearXTargets
    {
        public static Logging _log = E3.Log;
        private static IMQ MQ = E3.MQ;
        private static ISpawns _spawns = E3.Spawns;

		[ExposedData("ClearXTargets", "Enabled")]
		public static bool Enabled = false;
		[ExposedData("ClearXTargets", "MobToAttack")]
		public static Int32 MobToAttack = 0;
		[ExposedData("ClearXTargets", "FaceTarget")]
		public static bool FaceTarget = false;
        public static List<string> Filters = new List<string>();
		[ExposedData("ClearXTargets", "HasAllFlag")]
		public static bool HasAllFlag = false;
		[ExposedData("ClearXTargets", "StickTarget")]
		public static bool StickTarget = false;
		[ExposedData("ClearXTargets", "UseMyTarget")]
		public static bool UseMyTarget = false;
		[ExposedData("ClearXTargets", "FindLowestHPTarget")]
		public static bool FindLowestHPTarget = false;
		[ExposedData("ClearXTargets", "FindHighestHPTarget")]
		public static bool FindHighestHPTarget = false;

		[SubSystemInit]
		public static void ClearTargets_Init()
		{
			RegisterEvents();

		}
		private static void RegisterEvents()
        {
			EventProcessor.RegisterCommand("/cleartargets", (x) =>
			{
				ClearXTargets.FaceTarget = true;
				ClearXTargets.StickTarget = false;

				if (x.args.Count == 0)
				{
					ClearXTargets.MobToAttack = 0;
					Assist.AssistOff();
					E3.Bots.BroadcastCommandToGroup($"/backoff all", x);
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
					ClearXTargets.UseMyTarget = false;

				}
				else if (x.args.Count == 1 && x.args[0] == "off")
				{
					Assist.AssistOff();
					ClearXTargets.Enabled = false;
					ClearXTargets.Filters.Clear();
					ClearXTargets.HasAllFlag = false;
					E3.Bots.BroadcastCommandToGroup($"/backoff all", x);
				}
				else if (x.args.Count >= 1)
				{
					ClearXTargets.MobToAttack = 0;
					Assist.AssistOff();
					E3.Bots.BroadcastCommandToGroup($"/backoff all", x);
					if (x.filters.Count > 0)
					{
						ClearXTargets.Filters.Clear();
						ClearXTargets.Filters.AddRange(x.filters);
					}
					ClearXTargets.HasAllFlag = x.hasAllFlag;
					ClearXTargets.UseMyTarget = false;
					ClearXTargets.FaceTarget = true;
					ClearXTargets.StickTarget = false;

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
						else if (argValue.Equals("usemytarget", StringComparison.OrdinalIgnoreCase))
						{
							ClearXTargets.UseMyTarget = true;
							ClearXTargets.FindLowestHPTarget = false;
							ClearXTargets.FindHighestHPTarget = false;
						}
						else if (argValue.Equals("FindLowestHPTarget", StringComparison.OrdinalIgnoreCase))
						{
							ClearXTargets.FindLowestHPTarget = true;
							ClearXTargets.FindHighestHPTarget = false;
							ClearXTargets.UseMyTarget = false;
						}
						else if (argValue.Equals("FindHighestHPTarget", StringComparison.OrdinalIgnoreCase))
						{
							ClearXTargets.FindHighestHPTarget = true;
							ClearXTargets.FindLowestHPTarget = false;
							ClearXTargets.UseMyTarget = false;
						}
					}

					ClearXTargets.Enabled = true;

				}

			});
		}

		[ClassInvoke(Data.Class.All)]
        public static void Check_Xtargets()
        {
            if (Enabled)
            {
                e3util.YieldToEQ();
                _spawns.RefreshList();
                if (MobToAttack > 0)
                {
                    if (_spawns.TryByID(MobToAttack, out var ts))
                    {
                        //is it still alive?
                        if (ts.TypeDesc == "Corpse") MobToAttack = 0;//its dead jim
                    }
                    else
                    {
                        MobToAttack = 0;
                    }
                }
                //lets see if we have anything on xtarget that is valid
                if (MobToAttack == 0)
                {
					//first check to see if our driver already has a target
					if(UseMyTarget)
					{
						Int32 targetedMobID = MQ.Query<Int32>("${Target.ID}");
						if (targetedMobID > 0)
						{
							if (_spawns.TryByID(targetedMobID, out var tmob))
							{
								if (tmob.TypeDesc == "NPC" && tmob.Targetable && tmob.Aggressive)
								{
									MobToAttack = tmob.ID;
								}
							}
						}
					}
					if (FindLowestHPTarget)
					{
						MobToAttack = e3util.GetXtargetLowestHP();
					}
					else if (FindHighestHPTarget)
					{
						MobToAttack = e3util.GetXtargetHighestHP();
					}
					if (MobToAttack<1)
					{
						foreach (var s in _spawns.Get().OrderBy(x => x.Distance3D))
						{
							//find all mobs that are close
							if (s.TypeDesc != "NPC") continue;
							if (!s.Targetable) continue;
							if (!s.Aggressive) continue;
							if (string.IsNullOrWhiteSpace(s.CleanName)) continue; //no name, possibly swarm pet
							if (s.CleanName.EndsWith("s pet")) continue;
							if (!MQ.Query<bool>($"${{Spawn[npc id {s.ID}].LineOfSight}}")) continue;
							if (s.Distance3D > 60) break;//mob is too far away, and since it is ordered, kick out.
													   //its valid to attack!
							MobToAttack = s.ID;
							break;
						}
					}
					if (MobToAttack <=0)
                    {
                        //we are done, stop killing
                        Enabled = false;
                        MQ.Write("\agClear Targets complete.");
                        return;
                    }

                    //mobs to attack will be sorted by distance.
                    if (MobToAttack > 0)
                    {
                        //pop it off and start assisting.
                        Int32 mobId = MobToAttack;
                        Spawn s;
                        if (_spawns.TryByID(mobId, out s))
                        {
                            MQ.Write($"\agClear Targets: \aoIssuing Assist on {s.DisplayName} with id:{s.ID}.");
                            Assist.AllowControl = true;
                            Assist.AssistOn(s.ID, Zoning.CurrentZone.Id);
                            if (FaceTarget)
                            {
                                if(e3util.IsEQLive())
                                {
									MQ.Cmd("/face",500);
								}
                                else
                                {
									MQ.Cmd("/face fast");
								}
                               
                            }
                            if (StickTarget)
                            {
								//MQ.Write($"Setting stick with :/squelch /stick {E3.CharacterSettings.Assist_MeleeStickPoint} {Assist._assistDistance}");
                                MQ.Cmd($"/squelch /stick {E3.CharacterSettings.Assist_MeleeStickPoint} {Assist._assistDistance}");
                            }
                            MQ.Delay(500);

                            MQ.Cmd("/attack on");
                            if(HasAllFlag)
                            {
                                if (Filters.Count > 0)
                                {
                                    E3.Bots.BroadcastCommand($"/assistme {mobId} {Zoning.CurrentZone.Id} \"{string.Join(" ", Filters)}\"");
                                }
                                else
                                {
                                    E3.Bots.BroadcastCommand($"/assistme {mobId} {Zoning.CurrentZone.Id}");
                                }
                            }
                            else
                            {
                                if (Filters.Count > 0)
                                {
                                    E3.Bots.BroadcastCommandToGroup($"/assistme {mobId} {Zoning.CurrentZone.Id} \"{string.Join(" ", Filters)}\"");
                                }
                                else
                                {
                                    E3.Bots.BroadcastCommandToGroup($"/assistme {mobId} {Zoning.CurrentZone.Id}");
                                }
                            }

                        }
                        else
                        {
                            MobToAttack = 0;
                        }
                    }
                }


            }
        }
    }
}
