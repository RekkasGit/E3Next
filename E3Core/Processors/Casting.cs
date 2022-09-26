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


        public static Logging _log = Core._log;
        private static IMQ MQ = Core.mqInstance;


        public static CastReturn Cast(int targetID, Data.Spell spell)
        {

            using (_log.Trace())
            {
                //why we should not cast.. for whatever reason.
                #region validation checks
                if (MQ.Query<bool>("${Me.Invis}"))
                {

                    E3._actionTaken = true;
                    var targetName = MQ.Query<string>($"${{Spawn[id {targetID}].Name}}");
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


                if(E3._zoneID != MQ.Query<Int32>("${Zone.ID}"))
                {
                    //we are zoning, we need to chill for a bit.
                    MQ.Delay(1000);
                    return CastReturn.CAST_ZONING;
                }

                if(MQ.Query<bool>("${Me.Feigning}"))
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
                if(spell.SpellType.Equals("Beneficial"))
                {
                    if(!spell.CastType.Equals("Disc") && spell.TargetType.Equals("Self"))
                    {
                        if(!(spell.TargetType.Equals("PB AE") || spell.TargetType.Equals("Self")))
                        {
                            if(!MQ.Query<bool>(""))
                            {

                            }
                        }
                    }
                }
                #endregion

                return CastReturn.CAST_SUCCESS;
            }
            

        }

        public static void Init()
        {




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
        CAST_SPELLBOOKOPEN

    }
}
