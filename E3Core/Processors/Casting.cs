using MonoCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace E3Core.Processors
{
    public static class Casting
    {

        public static string _lastSuccesfulCast = String.Empty;
        public static Logging _log = Core._log;
        private static IMQ MQ = Core.mqInstance;


        public static CastReturn Cast(int targetID, Data.Spell spell)
        {

            //TODO: Add bard logic back in
            using (_log.Trace())
            {
                var targetName = MQ.Query<string>($"${{Spawn[id {targetID}].CleanName}}");
                //why we should not cast.. for whatever reason.
                #region validation checks
                if (MQ.Query<bool>("${Me.Invis}"))
                {

                    E3._actionTaken = true;

                    _log.Write($"SkipCast-Invis ${spell.CastName} {targetName} : {targetID}");
                    return CastReturn.CAST_INVIS;

                }

                if (!String.IsNullOrWhiteSpace(spell.Reagent))
                {
                    //spell requires a regent, lets check if we have it.
                    Int32 itemCount = MQ.Query<Int32>($"${{FindItemCount[={spell.Reagent}]}}");
                    if (itemCount < 1)
                    {
                        spell.RegentOutOfStock = true;
                        _log.Write($"Cannot cast [{spell.CastName}], I do not have any [{spell.Reagent}], removing this spell from array. Restock and Reload Macro", Logging.LogLevels.Error);
                        MQ.Cmd("/beep");
                        return CastReturn.CAST_REAGENT;
                    }

                }


                if (E3._zoneID != MQ.Query<Int32>("${Zone.ID}"))
                {
                    //we are zoning, we need to chill for a bit.
                    MQ.Delay(1000);
                    return CastReturn.CAST_ZONING;
                }

                if (MQ.Query<bool>("${Me.Feigning}"))
                {
                    MQ.Broadcast($"skipping [{spell.CastName}] , i am feigned.");
                    MQ.Delay(200);
                    return CastReturn.CAST_FEIGN;
                }
                if (MQ.Query<bool>("${Window[SpellBookWnd].Open}"))
                {
                    E3._actionTaken = true;
                    MQ.Broadcast($"skipping [{spell.CastName}] , spellbook is open.");
                    MQ.Delay(200);
                    return CastReturn.CAST_SPELLBOOKOPEN;
                }
                if (MQ.Query<bool>("${Corpse.Open}"))
                {
                    E3._actionTaken = true;
                    MQ.Broadcast($"skipping [{spell.CastName}] , I have a corpse open.");
                    MQ.Delay(200);
                    return CastReturn.CAST_SPELLBOOKOPEN;
                }
                if (spell.SpellType.Equals("Beneficial"))
                {
                    if (!spell.CastType.Equals("Disc") && spell.TargetType.Equals("Self"))
                    {
                        if (!(spell.TargetType.Equals("PB AE") || spell.TargetType.Equals("Self")))
                        {
                            if (!MQ.Query<bool>($"${{Spawn[id {targetID}].LineOfSight}}"))
                            {
                                _log.Write($"I cannot see {targetName}");
                                MQ.Write($"SkipCast-LoS {spell.CastName} ${spell.CastID} {targetName} {targetID}");
                                return CastReturn.CAST_CANNOTSEE;

                            }
                        }
                    }
                }
                #endregion
                //now to get the target
                if (spell.TargetType != "PB AE" && spell.TargetType != "Self")
                {
                    TrueTarget(targetID);
                }

                if (!String.IsNullOrWhiteSpace(spell.BeforeEvent))
                {
                    MQ.Cmd($"/docommand {spell.BeforeEvent}");
                }


                if (!String.IsNullOrWhiteSpace(spell.BeforeSpell))
                {
                    if (spell.BeforeSpellData == null)
                    {
                        spell.BeforeSpellData = new Data.Spell(spell.BeforeSpell);
                    }
                    Casting.Cast(targetID, spell.BeforeSpellData);
                }

                //remove item from cursor before casting
                if (MQ.Query<bool>("${Cursor.ID}"))
                {
                    MQ.Write("Issuing auto inventory on ${Cursor} for spell: ${pendingCast}");
                    MQ.Cmd("/autoinventory");
                }


                //From here, we actually start casting the spell. 

                if (spell.CastType == Data.CastType.Disc)
                {
                    if (MQ.Query<bool>("${Me.ActiveDisc.ID}") && spell.TargetType.Equals("Self"))
                    {
                        return CastReturn.CAST_ACTIVEDISC;

                    }
                    else
                    {
                        //activate disc!
                        TrueTarget(targetID);
                        E3._actionTaken = true;
                        MQ.Cmd($"/disc {spell.CastName}");
                        MQ.Delay(300);
                        return CastReturn.CAST_SUCCESS;
                    }

                }
                else if (spell.CastType == Data.CastType.Ability && MQ.Query<bool>($"${{Me.AbilityReady[{spell.CastName}]}}"))
                {
                    //to deal with a slam bug
                    if (spell.CastName.Equals("Slam"))
                    {
                        if (MQ.Query<bool>("${Window[ActionsAbilitiesPage].Child[AAP_FirstAbilityButton].Text.Equal[Slam]}"))
                        {

                            MQ.Cmd("/doability 1");
                        }
                        else if (MQ.Query<bool>("${Window[ActionsAbilitiesPage].Child[AAP_SecondAbilityButton].Text.Equal[Slam]}"))
                        {
                            MQ.Cmd("/doability 2");
                        }
                        else if (MQ.Query<bool>("${Window[ActionsAbilitiesPage].Child[AAP_ThirdAbilityButton].Text.Equal[Slam]}"))
                        {
                            MQ.Cmd("/doability 3");
                        }
                        else if (MQ.Query<bool>("${Window[ActionsAbilitiesPage].Child[AAP_FourthAbilityButton].Text.Equal[Slam]}"))
                        {
                            MQ.Cmd("/doability 4");
                        }
                        else if (MQ.Query<bool>("${Window[ActionsAbilitiesPage].Child[AAP_FourthAbilityButton].Text.Equal[Slam]}"))
                        {
                            MQ.Cmd("/doability 5");
                        }
                        else if (MQ.Query<bool>("${Window[ActionsAbilitiesPage].Child[AAP_FifthAbilityButton].Text.Equal[Slam]}"))
                        {
                            MQ.Cmd("/doability 5");
                        }
                        else if (MQ.Query<bool>("${Window[ActionsAbilitiesPage].Child[AAP_SixthAbilityButton].Text.Equal[Slam]}"))
                        {
                            MQ.Cmd("/doability 6");
                        }
                    }
                    else
                    {
                        MQ.Cmd($"/doability \"{spell.CastName}\"");
                    }

                    MQ.Delay(300, $"${{Me.AbilityReady[{spell.CastName}]}}");


                }
                else
                {
                    //Spell, AA, Items

                    //Stop following for spell/item/aa with a cast time > 0 MyCastTime, unless im a bard
                    //anything under 300 is insta cast
                    if (spell.MyCastTime > 300 && E3._currentClass != Data.Class.Bard)
                    {
                        if (MQ.Query<bool>("${Stick.Status.Equal[on]}")) MQ.Cmd("/squelch /stick pause");
                        if (MQ.Query<bool>("${AdvPath.Following}") && E3._following) MQ.Cmd("/squelch /afollow off");
                        if (MQ.Query<bool>("${MoveTo.Moving}") && E3._following) MQ.Cmd("/moveto off");

                        MQ.Delay(300, "${Bool[!${Me.Moving}}");

                    }

                    //TODO: SWAP ITEM

                    E3._actionTaken = true;

                    if (spell.CastType == Data.CastType.Item)
                    {

                        //TODO: Recast logic

                    }


                    if (E3._currentClass == Data.Class.Bard && MQ.Query<bool>($"${{Bool[${{Me.Book[{spell.CastName}]}}]}}"))
                    {
                        MQ.Cmd($"/cast \"{spell.CastName}\"");
                        MQ.Delay(500, "${Window[CastingWindow].Open}");
                        if (!MQ.Query<bool>("${Window[CastingWindow].Open}"))
                        {
                            MQ.Write("Issuing stopcast as cast window isn't open");
                            MQ.Cmd("/stopcast");
                            MQ.Delay(100);
                            MQ.Cmd($"/cast \"{spell.CastName}\"");
                            //wait for spell cast
                            MQ.Delay(300);

                        }

                        return CastReturn.CAST_SUCCESS;
                    }
                    else
                    {
                        if (spell.TargetType.Equals("Self"))
                        {
                            if (spell.CastType == Data.CastType.Spell)
                            {
                                MQ.Cmd($"/casting \"{spell.CastName}|{spell.SpellGem}\"");
                            }
                            else
                            {
                                MQ.Cmd($"/casting \"{spell.CastName}|{spell.CastType.ToString()}\"");
                            }
                        }
                        else
                        {
                            if (spell.CastType == Data.CastType.Spell)
                            {
                                MQ.Cmd($"/casting \"{spell.CastName}|{spell.SpellGem}\" \"-targetid|{targetID}\"");
                            }
                            else
                            {
                                MQ.Cmd($"/casting \"{spell.CastName}|{spell.CastType.ToString()}\" \"-targetid|{targetID}\"");
                            }
                        }
                        //wait for spell cast
                        MQ.Delay(300);
                    }
                    WaitForPossibleFizzle(spell);

                }

                cast_still_pending:

                while(MQ.Query<bool>("${Window[CastingWindow].Open}")) 
                { 
                    //means that we didn't fizzle and are now casting the spell







                }

                //spell is either complete or interrupted
                //need to wait for a period to get our results. 
                MQ.Delay(1500, $"${{Bool[!${{Cast.Status.Find[C]}}]}}");


                string castResult = MQ.Query<string>("${Cast.Result}");

                CastReturn returnValue = CastReturn.CAST_INTERRUPTED;
                Enum.TryParse<CastReturn>(castResult, out returnValue);



                if (returnValue == CastReturn.CAST_SUCCESS)
                {
                    _lastSuccesfulCast = spell.CastName;
                }
                else if (returnValue == CastReturn.CAST_RESIST)
                {
                    //TODO: Add timers/counters
                }
                else if (returnValue==CastReturn.CAST_TAKEHOLD)
                {
                    //TODO: Add timers
                }
                else if(returnValue == CastReturn.CAST_IMMUNE)
                {
                    //TODO: deal with immunity
                }


                //is an after spell configured? lets do that now.
                if (!String.IsNullOrWhiteSpace(spell.AfterSpell))
                {
                    if (spell.AfterSpellData == null)
                    {
                        spell.AfterSpellData = new Data.Spell(spell.AfterSpell);
                    }
                    Casting.Cast(targetID, spell.AfterSpellData);
                }
                //after event, after all things are done               
                if (!String.IsNullOrWhiteSpace(spell.AfterEvent))
                {
                    MQ.Cmd($"/docommand {spell.AfterEvent}");
                }

                if (Basics._following && Assist._isAssisting) Basics.AcquireFollow();

                //TODO: bard resume twist


                return returnValue;
                
            }


        }
        private static void WaitForPossibleFizzle(Data.Spell spell)
        {
            if (spell.MyCastTime > 500)
            {
                Int32 counter = 0;
                while (!MQ.Query<bool>("${Window[CastingWindow].Open}") && counter < 20)
                {
                    if (MQ.Query<bool>("${Cast.Result.Equal[CAST_FIZZLE]}"))
                    {
                        //window not open yet, but we have a fizzle set..breaking
                        return;
                    }
                    MQ.Delay(100);
                    counter++;
                }
            }
        }
        public static void Init()
        {




        }
        public static void TrueTarget(Int32 targetID)
        {
            if (MQ.Query<Int32>("${Target.ID}") == targetID) return;
            //now to get the target
            if (MQ.Query<Int32>($"${{SpawnCount[id {targetID}]}}") > 0)
            {
                MQ.Cmd($"/target id {targetID}");
                MQ.Delay(300, $"${{Target.ID}}=={targetID}");

            }
            else
            {
                MQ.Write("TrueTarget has no spawncount");
            }

        }

    }
    /*
    | CAST_CANCELLED       | Spell was cancelled by ducking (either manually or because mob died) |
    | CAST_CANNOTSEE       | You can't see your target                                            |
    | CAST_IMMUNE          | Target is immune to this spell                                       |
    | CAST_INTERRUPTED     | Casting was interrupted and exceeded the given time limit            |
    | CAST_INVIS           | You were invis, and noInvis is set to true                           |
    | CAST_NOTARGET        | You don't have a target selected for this spell                      |
    | CAST_NOTMEMMED       | Spell is not memmed and you gem to mem was not specified             |
    | CAST_NOTREADY        | AA ability or spell is not ready yet                                 |
    | CAST_OUTOFMANA       | You don't have enough mana for this spell!                           |
    | CAST_OUTOFRANGE      | Target is out of range                                               |
    | CAST_RESIST          | Your spell was resisted!                                             |
    | CAST_SUCCESS         | Your spell was cast successfully! (yay)                              |
    | CAST_UNKNOWN         | Spell/Item/Ability was not found                                     |
    | CAST_COLLAPSE        | Gate Collapsed                                                       |
    | CAST_TAKEHOLD        | Spell not hold                                                       |
    | CAST_FIZZLE          | Spell Fizzle                                                         |
    | CAST_INVISIBLE       | NOT Casting Invis                                                    |
    | CAST_RECOVER	       | Spell not Recovered yet!                                             |
    | CAST_STUNNED	       | Stunned                                                              |
    | CAST_STANDIG	       | Not Standing                                                         |
    | CAST_DISTRACTED      | To Distracted ( spell book open )                                    |
    | CAST_COMPONENTS| Missing Component													      |
     
     */
    public enum CastReturn
    {
        CAST_CANCELLED,
        CAST_CANNOTSEE,
        CAST_IMMUNE,
        CAST_INTERRUPTED,
        CAST_INVIS,
        CAST_NOTARGET,
        CAST_NOTMEMMED,
        CAST_NOTREADY,
        CAST_OUTOFMANA,
        CAST_OUTOFRANGE,
        CAST_RESIST,
        CAST_SUCCESS,
        CAST_UNKNOWN,
        CAST_COLLAPSE,
        CAST_TAKEHOLD,
        CAST_FIZZLE,
        CAST_INVISIBLE,
        CAST_RECOVER,
        CAST_STUNNED,
        CAST_STANDIG,
        CAST_DISTRACTED,
        CAST_COMPONENTS,
        CAST_REAGENT,
        CAST_ZONING,
        CAST_FEIGN,
        CAST_SPELLBOOKOPEN,
        CAST_ACTIVEDISC

    }
}
