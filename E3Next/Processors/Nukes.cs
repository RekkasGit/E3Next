using E3Core.Classes;
using E3Core.Data;
using E3Core.Settings;
using E3Core.Utility;
using MonoCore;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;

namespace E3Core.Processors
{
    public static class Nukes
    {
        public static Logging _log = E3._log;
        private static IMQ MQ = E3.MQ;
        private static ISpawns _spawns = E3._spawns;
        private static Double _nukeDelayTimeStamp;
        private static Double _stunDelayTimeStamp;

        [AdvSettingInvoke]
        public static void Check_Stuns()
        {

            Cast_Instasnt(E3._characterSettings.Stuns, ref _stunDelayTimeStamp);
        }
        [AdvSettingInvoke]
        public static void Check_Nukes()
        {

            Cast_Instasnt(E3._characterSettings.Nukes, ref _nukeDelayTimeStamp);
        }
        private static void Cast_Instasnt(List<Data.Spell> spells, ref Double delayTimeStamp)
        {
            if (Assist._assistTargetID > 0)
            {
                //we should be assisting, check_AssistStatus, verifies its not a corpse.

                Spawn s;
                if (_spawns.TryByID(Assist._assistTargetID, out s))
                {
                    bool giftOfManaSet = false;
                    bool giftOfMana = false;

                    foreach (var spell in spells)
                    {
                        //check Ifs on the spell
                        if (!String.IsNullOrWhiteSpace(spell.Ifs))
                        {
                            if (!MQ.Query<bool>($"${{If[{spell.Ifs},TRUE,FALSE]}}"))
                            {
                                //failed check, onto the next
                                continue;
                            }
                        }
                        //can't cast if it isn't ready
                        if (Casting.InRange(Assist._assistTargetID, spell) && Casting.CheckReady(spell) && Casting.CheckMana(spell))
                        {
                            //we should have a valid target via check_assistStatus
                            if (spell.Delay > 0 && delayTimeStamp > 0 && Core._stopWatch.ElapsedMilliseconds < delayTimeStamp)
                            {
                                //delay has been specified, skip this spell
                                continue;

                            }
                            //reset delay timestamp
                            if (spell.Delay > 0)
                            {
                                delayTimeStamp = 0;
                            }


                            if (spell.GiftOfMana)
                            {
                                //can only cast if we have gift of mana. do we have it?
                                if (!(giftOfManaSet))
                                {
                                    giftOfMana = (MQ.Query<bool>("${Me.Song[Gift of Mana].ID}") || MQ.Query<bool>("${Me.Song[Celestial Gift].ID}") || MQ.Query<bool>("${Me.Song[Celestial Boon].ID}"));
                                    giftOfManaSet = true;
                                }
                                if (!giftOfMana)
                                {
                                    continue;
                                }

                            }
                            //aggro checks
                            if (spell.NoAggro)
                            {
                                if (MQ.Query<bool>("${Me.TargetOfTarget.CleanName.Equal[${Me.CleanName}]}"))
                                {
                                    continue;
                                }
                            }
                            if (spell.PctAggro > 0 && MQ.Query<Int32>("${Me.PctAggro}") > spell.PctAggro)
                            {
                                continue;
                            }
                            //end aggro checks

                            //check for buff
                            if (!String.IsNullOrWhiteSpace(spell.CastIF))
                            {
                                if (!MQ.Query<bool>($"${{Bool[${{Target.Buff[{spell.CastIF}]}}]}}"))
                                {
                                    //doesn't have the buff we want
                                    continue;
                                }
                            }

                            if (s.Distance < spell.MyRange)
                            {

                                CastReturn result = Casting.Cast(Assist._assistTargetID, spell, Heals.SomeoneNeedsHealing);
                                if (result == CastReturn.CAST_INTERRUPTFORHEAL)
                                {
                                    return;
                                }
                                if (result == CastReturn.CAST_SUCCESS)
                                {
                                    //if the spell is a delay time, lets make sure all other delay types are blocked for the
                                    //delay time
                                    if (spell.Delay > 0)
                                    {
                                        delayTimeStamp = Core._stopWatch.ElapsedMilliseconds + (spell.Delay * 1000);
                                    }
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
