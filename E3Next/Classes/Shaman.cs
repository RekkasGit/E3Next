using E3Core.Processors;
using E3Core.Settings;
using System;
using E3Core.Classes;
using E3Core.Data;
using E3Core.Utility;
using MonoCore;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Web.UI;

namespace E3Core.Classes
{
    /// <summary>
    /// Properties and methods specific to the shaman class
    /// </summary>
    public static class Shaman
    {

        private static Logging _log = E3.Log;
        private static IMQ MQ = E3.MQ;
        private static ISpawns _spawns = E3.Spawns;

        private static Int64 _nextAggroCheck = 0;
        private static Int64 _nextAggroRefreshTimeInterval = 1000;
        private static Int64 _nextTotemCheck = 0;
        private static Int64 _nextTotemRefreshTimeInterval = 3000;

        private static Int32 _maxAggroCap = 90;

		[SubSystemInit]
		public static void Shaman_Init()
		{
			RegisterEvents();
		}
        public static void RegisterEvents()
        {
			EventProcessor.RegisterCommand("/e3autocanni", (x) =>
			{
				//swap them
				e3util.ToggleBooleanSetting(ref E3.CharacterSettings.AutoCanni, "Auto Canni", x.args);
	
			});

		}
        [ClassInvoke(Data.Class.Shaman)]
        public static void AutoCanni()
        {
			//don't canni if we are moving/following
			if (E3.CharacterSettings.AutoCanni && Movement.StandingStillForTimePeriod())
			{
				
				foreach (var canniSpell in E3.CharacterSettings.CanniSpell)
				{
					int pctMana = MQ.Query<int>("${Me.PctMana}");
					var pctHps = MQ.Query<int>("${Me.PctHPs}");
					int currentHps = MQ.Query<int>("${Me.CurrentHPs}");
					var minhpThreashold = canniSpell.MinHPTotal;
					if (minhpThreashold > 0)
					{
						if (currentHps < minhpThreashold)
						{
							continue;
						}
					}
					if (!Casting.Ifs(canniSpell))
                    {
                        continue;
                    }
					if (Casting.CheckReady(canniSpell))
					{
						var hpThresholdDefined = canniSpell.MinHP > 0;
						var manaThresholdDefined = canniSpell.MaxMana > 0;
                      
						bool castCanniSpell = false;
						bool hpThresholdMet = false;
						bool manaThresholdMet = false;

                        

						if (hpThresholdDefined)
						{
							if (pctHps > canniSpell.MinHP)
							{
								hpThresholdMet = true;
							}
						}

						if (manaThresholdDefined)
						{
							if (pctMana < canniSpell.MaxMana)
							{
								manaThresholdMet = true;
							}
						}

						if (hpThresholdDefined && manaThresholdDefined)
						{
							castCanniSpell = hpThresholdMet && manaThresholdMet;
						}
						else if (hpThresholdDefined && !manaThresholdDefined)
						{
							castCanniSpell = hpThresholdMet;
						}
						else if (manaThresholdDefined && !hpThresholdDefined)
						{
							castCanniSpell = manaThresholdMet;
						}
						else
						{
							castCanniSpell = true;
						}

						if (castCanniSpell)
						{
							var result = Casting.Cast(0, canniSpell,Heals.SomeoneNeedsHealing);
							if (result == CastReturn.CAST_SUCCESS)
							{
								break;
							}
                            if(result==CastReturn.CAST_INTERRUPTFORHEAL)
                            {
                                return;
                            }
						}
					}

				}


			}
		}
		/// <summary>
		/// Checks aggro level and drops it if necessary.
		/// </summary>
		[AdvSettingInvoke]
        public static void Check_ShamanAggro()
        {

            if (!e3util.ShouldCheck(ref _nextAggroCheck, _nextAggroRefreshTimeInterval)) return;

            using(_log.Trace())
            {
                using(_log.Trace("TotemDrop"))
                {
                    if(BuffCheck.HasBuff("Inconspicuous Totem"))
                    {
                        BuffCheck.DropBuff("Inconspicuous Totem");
                    }
                }
                Int32 currentAggro = 0;
                Int32 tempMaxAggro = 0;
                using (_log.Trace("XTargetCheck"))
                {
                    for (Int32 i = 1; i <= 13; i++)
                    {
                        bool autoHater = MQ.Query<bool>($"${{Me.XTarget[{i}].TargetType.Equal[Auto Hater]}}");
                        if (!autoHater) continue;
                        Int32 mobId = MQ.Query<Int32>($"${{Me.XTarget[{i}].ID}}");
                        if (mobId > 0)
                        {
                            Spawn s;
                            if (_spawns.TryByID(mobId, out s))
                            {
                                if (s.Aggressive)
                                {
                                    currentAggro = MQ.Query<Int32>($"${{Me.XTarget[{i}].PctAggro}}");
                                    if (tempMaxAggro < currentAggro)
                                    {
                                        tempMaxAggro = currentAggro;
                                    }
                                }
                            }
                        }
                    }
                }
               
                if (tempMaxAggro > _maxAggroCap)
                {

                    if (!Assist.IsAssisting) return;

                    Spell s;
                    if (!Spell.LoadedSpellsByName.TryGetValue("Inconspicuous Totem", out s))
                    {
                        s = new Spell("Inconspicuous Totem");
                    }
                    if (Casting.CheckReady(s) && Casting.CheckMana(s))
                    {
                        Casting.Cast(0, s);
                        MQ.Delay(400);
                        BuffCheck.DropBuff("Inconspicuous Totem");
                        return;
                    }

                }
            }
        
        }

        /// <summary>
        /// Uses malos totem if necessary.
        /// </summary>
        [AdvSettingInvoke]
        public static void Check_MalosTotem()
        {
            if (!e3util.ShouldCheck(ref _nextTotemCheck, _nextTotemRefreshTimeInterval)) return;
            if(Movement.AnchorTarget>0)
            {
                using (_log.Trace())
                {
                    bool idolUp = MQ.Query<bool>("${Bool[${Spawn[Spirit Idol]}]}");

                    if (!idolUp)
                    {
                        Spell s;                        
                        if (!Spell.LoadedSpellsByName.TryGetValue("Idol of Malos", out s))
                        {
                            s = new Spell($"Idol of Malos/Gem|{E3.CharacterSettings.MalosTotemSpellGem}");
                        }
                        if (Casting.CheckReady(s) && Casting.CheckMana(s))
                        {
                            Casting.Cast(0, s);
                            return;
                        }
                    }
                }
                
            }


        }
    }
}
