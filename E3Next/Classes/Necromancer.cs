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

namespace E3Core.Classes
{
    /// <summary>
    /// Properties and methods specific to the necromancer class
    /// </summary>
    public static class Necromancer
    {
        private static Logging _log = E3.Log;
        private static IMQ MQ = E3.MQ;
        private static ISpawns _spawns = E3.Spawns;

        private static Int64 _nextAggroCheck = 0;
        private static Int64 _nextAggroRefreshTimeInterval = 1000;
        private static Int32 _maxAggroCap = 75;

        [SubSystemInit]
        public static void Necromancer_Init()
        {
            if(E3.CurrentClass!=Class.Necromancer) return;

            EventProcessor.RegisterEvent("NecroFDBreak", "You are no longer feigning death, because a spell hit you.", (x) =>
            {

                if (MQ.Query<bool>("${Me.Feigning}"))
                {
                    E3.Bots.Broadcast("My Feign was broken, retrying...");
                    MQ.Cmd("/stand");

                    //recast FD
                      
                    Spell s;
                    if (!Spell.LoadedSpellsByName.TryGetValue("Improved Death Peace", out s))
                    {
                        s = new Spell("Improved Death Peace");
                    }
                    if (Casting.CheckMana(s) && Casting.CheckReady(s))
                    {
                        Casting.Cast(0, s);
                          

                    }
                    else if (!Spell.LoadedSpellsByName.TryGetValue("Death Peace", out s))
                    {
                        s = new Spell("Death Peace");
                        Casting.Cast(0, s);
                          
                    }
                }
            });
            
        }

        [AdvSettingInvoke]
        public static void Check_NecroFD()
        {

            if (!Basics.InCombat()) return;
            //allow people to run to their death if they have the window focused. 
            if (e3util.IsManualControl()) return;
			if (Basics.GroupMembers.Count < 5) return;


			Int32 GroupSize = MQ.Query<Int32>("${Group}");
            Int32 GroupInZone = MQ.Query<Int32>("${Group.Present}");
            Spell s;

            if (GroupSize - GroupInZone > 1)
            {
                Assist.AssistOff();
                bool FD = false;
                if (!Spell.LoadedSpellsByName.TryGetValue("Improved Death Peace", out s))
                {
                    s = new Spell("Improved Death Peace");
                }
                if (Casting.CheckMana(s) && Casting.CheckReady(s))
                {
                    Casting.Cast(0, s);
                    FD = true;
              
                }
                else if (!Spell.LoadedSpellsByName.TryGetValue("Death Peace", out s))
                {
                    s = new Spell("Death Peace");
                    Casting.Cast(0, s);
                    FD = true;
                }

                if (FD)
                {
                    E3.Bots.Broadcast("<Check_NecroFD>Two people are dead in group, FDing and staying down. Issue reassist when ready.");
                }
            }
        }

        /// <summary>
        /// Checks aggro level and drops it if necessary.
        /// </summary>
        [AdvSettingInvoke]
        public static void Check_NecroAggro()
        {
            if (!e3util.ShouldCheck(ref _nextAggroCheck, _nextAggroRefreshTimeInterval)) return;

            //if manual control, kickout
            if (e3util.IsManualControl()) return;
			if (Basics.GroupMembers.Count < 5) return;

			//if already FD, kickout
			if (MQ.Query<bool>("${Me.Feigning}")) return;

            Int32 currentAggro = 0;
            Int32 tempMaxAggro = 0;

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
                            if(tempMaxAggro<currentAggro)
                            {
                                tempMaxAggro = currentAggro;
                            }
                        }
                    }
                }
            }
            if(tempMaxAggro>_maxAggroCap)
            {
                
                Spell s;
                if(e3util.IsEQEMU())
				{
					if (!Spell.LoadedSpellsByName.TryGetValue("Improved Death Peace", out s))
					{
						s = new Spell("Improved Death Peace");
					}
					if (Casting.CheckMana(s) && Casting.CheckReady(s))
					{
						Casting.Cast(0, s);
						//check to see if we can stand based off the # of group members.
						Int32 GroupSize = MQ.Query<Int32>("${Group}");
						Int32 GroupInZone = MQ.Query<Int32>("${Group.Present}");

						if (GroupSize - GroupInZone > 0)
						{
							Assist.AssistOff();
							E3.Bots.Broadcast("<CheckNecroAggro> Have agro, someone is dead, staying down. Issue reassist when ready.");


						}
						else
						{
							MQ.Cmd("/stand");
							return;
						}

					}

				}
               
                if (!Spell.LoadedSpellsByName.TryGetValue("Death Peace", out s))
                {
                    s = new Spell("Death Peace");
                }
                if (Casting.CheckMana(s) && Casting.CheckReady(s))
                {
                    Casting.Cast(0, s);
                    //check to see if we can stand based off the # of group members.
                    Int32 GroupSize = MQ.Query<Int32>("${Group}");
                    Int32 GroupInZone = MQ.Query<Int32>("${Group.Present}");

                    if (GroupSize - GroupInZone > 0)
                    {
                        Assist.AssistOff();
                        E3.Bots.Broadcast("<CheckNecroAggro> Have agro, someone is dead, staying down. Issue reassist when ready.");
                    }
                    else
                    {
                        MQ.Cmd("/stand");
                        return;
                    }
                    return;
                }

            } 
            
        }

    }
}
