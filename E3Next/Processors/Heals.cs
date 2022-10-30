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
    public class Heals : BaseProcessor
    {
        public static ISpawns _spawns = E3._spawns;

        private static Int64 _nextHealCheck = 0;
        private static Int64 _nextHealCheckInterval = 250;

        [AdvSettingInvoke]
        public static void Check_Heals()
        {
            if (E3._isInvis) return;
            if (!Basics.InCombat())
            {
                if (!e3util.ShouldCheck(ref _nextHealCheck, _nextHealCheckInterval)) return;

            }
            //using (_log.Trace())
            {
                //grabbing these values now and reusing them
                Int32 currentMana = MQ.Query<Int32>("${Me.CurrentMana}");
                Int32 pctMana = MQ.Query<Int32>("${Me.PctMana}");
                if (E3._characterSettings.HealTanks.Count > 0 && E3._characterSettings.HealTankTargets.Count > 0)
                {
                    HealTanks(currentMana, pctMana);
                    if (E3._actionTaken)
                    {   //update values
                        currentMana = MQ.Query<Int32>("${Me.CurrentMana}");
                        pctMana = MQ.Query<Int32>("${Me.PctMana}");
                    }
                }
                if (!E3._actionTaken && E3._characterSettings.HealXTarget.Count > 0)
                {
                    HealXTargets(currentMana, pctMana);
                }
                if (!E3._actionTaken) GroupHeals(currentMana, pctMana);
                if (!E3._actionTaken) HealImportant(currentMana, pctMana);
                if (!E3._actionTaken) HealAll(currentMana, pctMana);
                if (!E3._actionTaken) HoTTanks(currentMana, pctMana);
                if (!E3._actionTaken) HoTImportant(currentMana, pctMana);
                if (!E3._actionTaken) HoTAll(currentMana, pctMana);
                if (!E3._actionTaken) HealPets(currentMana, pctMana);
                if (!E3._actionTaken) HoTPets(currentMana, pctMana);
            }
        }



        public static bool HealTanks(Int32 currentMana, Int32 pctMana)
        {
            if (E3._characterSettings.WhoToHeal.Contains("Tanks"))
            {
                return Heal(currentMana, pctMana, E3._characterSettings.HealTankTargets, E3._characterSettings.HealTanks);
            }
            return false;
        }
        public static bool HealImportant(Int32 currentMana, Int32 pctMana)
        {
            if (E3._characterSettings.WhoToHeal.Contains("ImportantBots"))
            {
                return Heal(currentMana, pctMana, E3._characterSettings.HealImportantBotTargets, E3._characterSettings.HealImportantBots);
            }
            return false;
        }
        public static bool HealXTargets(Int32 currentMana, Int32 pctMana, bool JustCheck = false)
        {
            if (!E3._characterSettings.WhoToHeal.Contains("XTargets"))
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
                foreach (var spell in E3._characterSettings.HealXTarget)
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
                                E3._actionTaken = true;
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
            if (E3._characterSettings.WhoToHoT.Contains("Pets"))
            {
                HealOverTime(currentMana, pctMana, E3._characterSettings.HealPetOwners, E3._characterSettings.HealOverTime);
            }
        }
        public static void HealPets(Int32 currentMana, Int32 pctMana)
        {
            if (E3._characterSettings.WhoToHeal.Contains("Pets"))
            {
                Heal(currentMana, pctMana, E3._characterSettings.HealPetOwners, E3._characterSettings.HealPets);

            }
        }
        public static void HoTAll(Int32 currentMana, Int32 pctMana)
        {
            if (E3._characterSettings.WhoToHoT.Contains("All"))
            {
                List<string> targets = E3._bots.BotsConnected();
                HealOverTime(currentMana, pctMana, targets, E3._characterSettings.HealOverTime);
            }
        }
        public static void HoTImportant(Int32 currentMana, Int32 pctMana)
        {
            if (E3._characterSettings.WhoToHoT.Contains("ImportantBots"))
            {
                HealOverTime(currentMana, pctMana, E3._characterSettings.HealImportantBotTargets, E3._characterSettings.HealOverTime);
            }
        }
        public static void HoTTanks(Int32 currentMana, Int32 pctMana)
        {
            if (E3._characterSettings.WhoToHoT.Contains("Tanks"))
            {
                HealOverTime(currentMana, pctMana, E3._characterSettings.HealTankTargets, E3._characterSettings.HealOverTime);
            }
        }
        public static void HealAll(Int32 currentMana, Int32 pctMana)
        {
            if (E3._characterSettings.WhoToHeal.Contains("All"))
            {
                //get a list from netbots
                List<string> targets = E3._bots.BotsConnected();
                Heal(currentMana, pctMana, targets, E3._characterSettings.HealAll);
            }
        }
        public static void GroupHeals(Int32 currentMana, Int32 pctMana)
        {
            foreach (var spell in E3._characterSettings.HealGroup)
            {
                Int32 numberNeedingHeal = MQ.Query<Int32>($"${{Group.Injured[{spell.HealPct}]}}");
                if (numberNeedingHeal > 2)
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
                        E3._actionTaken = true;
                        return;
                    }

                }
            }
        }
        /// <summary>
        /// used as an action to determine if a spell should be interrupted in case someone needs a heal.
        /// </summary>
        /// <returns>true if a heal is needed, otherwise false</returns>
        public static bool SomeoneNeedsHealing(Int32 currentMana, Int32 pctMana)
        {
            if (!((E3._currentClass & Data.Class.Priest) == E3._currentClass))
            {
                return false;
            }

            //Int32 currentMana = MQ.Query<Int32>("${Me.CurrentMana}");
            //Int32 pctMana = MQ.Query<Int32>("${Me.PctMana}");
            if (E3._characterSettings.WhoToHeal.Contains("Tanks"))
            {
                if (Heal(currentMana, pctMana, E3._characterSettings.HealTankTargets, E3._characterSettings.HealTanks, false, true))
                {
                    return true;
                }
            }
            if (E3._characterSettings.WhoToHeal.Contains("ImportantBots"))
            {
                if (Heal(currentMana, pctMana, E3._characterSettings.HealImportantBotTargets, E3._characterSettings.HealImportantBots, false, true))
                {
                    return true;
                }
            }
            if (E3._characterSettings.HealXTarget.Count > 0)
            {
                if (HealXTargets(currentMana, pctMana, true))
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
                        if (targetType == "PC")
                        {
                            //check group data

                            Int32 groupMemberIndex = MQ.Query<Int32>($"${{Group.Member[{name}].Index}}");

                            if (groupMemberIndex > 0)
                            {
                                Int32 pctHealth = MQ.Query<Int32>($"${{Group.Member[{groupMemberIndex}].Spawn.CurrentHPs}}");

                                if (pctHealth < 1)
                                {
                                    //dead, no sense in casting. check the next person
                                    continue;
                                }
                                foreach (var spell in spells)
                                {

                                    if (!String.IsNullOrWhiteSpace(spell.CheckFor))
                                    {

                                        if (E3._bots.BuffList(name).Contains(spell.CheckForID))
                                        {
                                            //they have the buff, kick out
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
                                            if (Casting.CheckReady(spell))
                                            {
                                                if (Casting.Cast(targetID, spell) == CastReturn.CAST_FIZZLE)
                                                {
                                                    currentMana = MQ.Query<Int32>("${Me.CurrentMana}");
                                                    pctMana = MQ.Query<Int32>("${Me.PctMana}");
                                                    goto recastSpell;
                                                }
                                                E3._actionTaken = true;
                                                return true;
                                            }
                                        }
                                    }
                                }
                            }
                            //check netbots
                            bool botInZone = E3._bots.InZone(name);
                            if (botInZone)
                            {
                                //they are a netbots and they are in zone
                                Int32 pctHealth = E3._bots.PctHealth(name);
                                foreach (var spell in spells)
                                {
                                    if (!String.IsNullOrWhiteSpace(spell.CheckFor))
                                    {
                                        if (E3._bots.BuffList(name).Contains(spell.CheckForID))
                                        {
                                            //they have the buff, kick out
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
                                    if (targetDistance < spell.MyRange)
                                    {
                                        if (pctHealth < spell.HealPct)
                                        {
                                            if (JustCheck) return true;
                                            //should cast a heal!
                                            if (Casting.CheckReady(spell))
                                            {
                                                if (Casting.Cast(targetID, spell) == CastReturn.CAST_FIZZLE)
                                                {
                                                    currentMana = MQ.Query<Int32>("${Me.CurrentMana}");
                                                    pctMana = MQ.Query<Int32>("${Me.PctMana}");
                                                    goto recastSpell;
                                                }
                                                E3._actionTaken = true;
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
                                bool botInZone = E3._bots.InZone(name);
                                if (botInZone)
                                {
                                    //they are a netbots and they are in zone
                                    Int32 pctHealth = E3._bots.PctHealth(name);
                                    foreach (var spell in spells)
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
                                        if (Casting.InRange(targetID, spell))
                                        {
                                            if (pctHealth <= spell.HealPct)
                                            {
                                                if (!E3._bots.HasShortBuff(name, spell.SpellID))
                                                {
                                                    if (Casting.CheckReady(spell))
                                                    {
                                                        if (Casting.Cast(targetID, spell) == CastReturn.CAST_FIZZLE)
                                                        {
                                                            currentMana = MQ.Query<Int32>("${Me.CurrentMana}");
                                                            pctMana = MQ.Query<Int32>("${Me.PctMana}");
                                                            goto recastSpell;
                                                        }
                                                        E3._actionTaken = true;
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

    }
}
