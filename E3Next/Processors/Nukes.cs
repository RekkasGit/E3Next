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
        public static Logging _log = E3.Log;
        private static IMQ MQ = E3.MQ;
        private static ISpawns _spawns = E3.Spawns;
        private static Double _nukeDelayTimeStamp;
        private static Double _stunDelayTimeStamp;
        private static Double _pbaeDelayTimeStamp;


        public static bool PBAEEnabled = false;

        public static void Reset()
        {
            PBAEEnabled = false;
        }

        [SubSystemInit()]
        public static void Init()
        {
            RegisterEvents();
        }
        private static void RegisterEvents()
        {

            EventProcessor.RegisterCommand("/pbaeon", (x) =>
            {
                bool hasAllFlag = e3util.HasAllFlag(x);

                if (E3.CharacterSettings.PBAE.Count > 0)
                {
                    PBAEEnabled = true;
                    E3.Bots.Broadcast("Enabling PBAE");
                   
                }
                if (x.args.Count == 0)
                {
                    if (hasAllFlag)
                    {
                        E3.Bots.BroadcastCommand($"/pbaeon all");
                    }
                    else
                    {
                        E3.Bots.BroadcastCommandToGroup($"/pbaeon all");
                    }
                }

            });

            EventProcessor.RegisterCommand("/pbaeoff", (x) =>
            {
                if (E3.CharacterSettings.PBAE.Count > 0)
                {
                    PBAEEnabled = false;
                    E3.Bots.Broadcast("Disabling PBAE");
                   
                }
                if (x.args.Count == 0)
                {
                    E3.Bots.BroadcastCommand($"/pbaeoff all");
                }
            });
        }


        [ClassInvoke(Data.Class.All)]
        public static void CheckPBAE()
        {
            if (PBAEEnabled  && E3.CharacterSettings.PBAE.Count>0&& MQ.Query<bool>($"!${{Bool[${{SpawnCount[npc radius {E3.GeneralSettings.Assists_AEThreatRange}]}}]}}"))
            {
                E3.Bots.Broadcast($"\aoDisabiling PBAE as no more mobs in {E3.GeneralSettings.Assists_AEThreatRange} radius");
                PBAEEnabled = false;
            }
            if (PBAEEnabled)
            {
                Cast_PBAE(E3.CharacterSettings.PBAE, ref _pbaeDelayTimeStamp);
            }
        }

        [AdvSettingInvoke]
        public static void Check_Stuns()
        {

            Cast_Instasnt(E3.CharacterSettings.Stuns, ref _stunDelayTimeStamp);
        }
        [AdvSettingInvoke]
        public static void Check_Nukes()
        {

            Cast_Instasnt(E3.CharacterSettings.Nukes, ref _nukeDelayTimeStamp);
        }
        private static void Cast_Instasnt(List<Data.Spell> spells, ref Double delayTimeStamp)
        {
            if (Assist.AssistTargetID > 0)
            {
                //we should be assisting, check_AssistStatus, verifies its not a corpse.
                using (_log.Trace())
                {
                    Spawn s;
                    if (_spawns.TryByID(Assist.AssistTargetID, out s))
                    {
                        bool giftOfManaSet = false;
                        bool giftOfMana = false;

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
                            //can't cast if it isn't ready
                            if (Casting.InRange(Assist.AssistTargetID, spell) && Casting.CheckReady(spell) && Casting.CheckMana(spell))
                            {
                                //we should have a valid target via check_assistStatus
                                if (spell.Delay > 0 && delayTimeStamp > 0 && Core.StopWatch.ElapsedMilliseconds < delayTimeStamp)
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

                                    CastReturn result = Casting.Cast(Assist.AssistTargetID, spell, Heals.SomeoneNeedsHealing);
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
                                            delayTimeStamp = Core.StopWatch.ElapsedMilliseconds + (spell.Delay * 1000);
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
        private static void Cast_PBAE(List<Data.Spell> spells, ref Double delayTimeStamp)
        {

            //we should be assisting, check_AssistStatus, verifies its not a corpse.
            using (_log.Trace())
            {

                bool giftOfManaSet = false;
                bool giftOfMana = false;

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
                    //can't cast if it isn't ready
                    if (Casting.CheckReady(spell) && Casting.CheckMana(spell))
                    {
                        //we should have a valid target via check_assistStatus
                        if (spell.Delay > 0 && delayTimeStamp > 0 && Core.StopWatch.ElapsedMilliseconds < delayTimeStamp)
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
                
                    
                        CastReturn result = Casting.Cast(0, spell, Heals.SomeoneNeedsHealing);
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
                                delayTimeStamp = Core.StopWatch.ElapsedMilliseconds + (spell.Delay * 1000);
                            }
                            return;
                        }
                    }
                }
            }
        }
    }
}
