using E3Core.Data;
using MonoCore;
using System;
using System.Collections.Generic;
using System.Diagnostics.SymbolStore;
using System.Linq;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace E3Core.Processors
{
    public static class NowCast
    {
        public static Logging _log = E3.Log;
        private static IMQ MQ = E3.MQ;
        [SubSystemInit]
        public static void Init()
        {
            RegisterEvents();
        }

        private static void RegisterEvents()
        {
            EventProcessor.RegisterCommand("/nowcast", (x) =>
            {
                if (x.args.Count > 1)
                {
                    //nowcast person "spell name" targetid
                    //nowcast me "spell name" targetid
                    //nowcast rockn "spell name" targetid
                    //nowcast all "spell name"
                    Int32 targetid = 0;
                    string user = string.Empty;
                    string spell = string.Empty;

                    user = x.args[0];
                    spell = x.args[1];
                    if(x.args.Count>2)
                    {
                        Int32.TryParse(x.args[2], out targetid);
                    }

                    CastReturn castResult = CastReturn.CAST_SUCCESS;
                    if (user.Equals("all", StringComparison.OrdinalIgnoreCase))
                    {
                        if (targetid > 0)
                        {
                            E3.Bots.BroadcastCommandToGroup($"/nowcast me \"{spell}\" {targetid}",x);
                            castResult = NowCastSpell(spell, targetid);
                        }
                        else
                        {
                            E3.Bots.BroadcastCommandToGroup($"/nowcast me \"{spell}\"", x);
                            castResult = NowCastSpell(spell, 0);

                        }

                    }
                    else if (user.Equals("me", StringComparison.OrdinalIgnoreCase))
                    {
                        if (targetid > 0)
                        {
                            castResult = NowCastSpell(spell, targetid);
                        }
                        else
                        {
                            castResult = NowCastSpell(spell, 0);
                        }
                    }
                    else
                    {
                        if (targetid > 0)
                        {
                            //send this to a person!
                            E3.Bots.BroadcastCommandToPerson(user, $"/nowcast me \"{spell}\" {targetid}");
                        }
                        else
                        {
                            //send this to a person!
                            E3.Bots.BroadcastCommandToPerson(user, $"/nowcast me \"{spell}\"");


                        }
                    }

                    if (castResult != CastReturn.CAST_SUCCESS)
                    {
                        E3.Bots.Broadcast($"\arNowcast of {spell} unsuccessful due to {castResult}!");
                    }
                }
            });

        }

        public static bool IsNowCastInQueue()
        {
            if(EventProcessor.CommandList["/nowcast"].queuedEvents.Count > 0)
            {
                return true;
            }
            return false;
        }
        private static CastReturn NowCastSpell(string spellName, Int32 targetid)
        {
            Int32 orgTargetID = MQ.Query<Int32>("${Target.ID}");

            try
            {
                string realSpell = string.Empty;
                if (BegForBuffs.SpellAliases.TryGetValue(spellName, out realSpell))
                {
                    spellName = realSpell;
                }
                Spell spell = new Spell(spellName, E3.CharacterSettings.ParsedData);

                if (spell.SpellID > 0)
                {

                    //wait for GCD to be over.
                    bool wasCasting = false;
                    while (Casting.IsCasting())
                    {
                        wasCasting = true;
                        MQ.Delay(50);
                    }
                    if (wasCasting)
                    {
                        MQ.Delay(600);
                    }
                    if (MQ.Query<Int32>("${Me.CurrentMana}") > 0)
                    {
                        while (Casting.InGlobalCooldown())
                        {
                            MQ.Delay(100);
                        }
                    }

                    if (targetid == 0)
                    {
                        targetid = E3.CurrentId;
                    }
                    if (!String.IsNullOrWhiteSpace(spell.Ifs))
                    {
                        Casting.TrueTarget(targetid);
                        if (!Casting.Ifs(spell))
                        {
                            return CastReturn.CAST_IFFAILURE;
                        }
                    }
                    if (!String.IsNullOrWhiteSpace(spell.CheckFor))
                    {
                        Casting.TrueTarget(targetid);
                        if (MQ.Query<bool>($"${{Bool[${{Target.Buff[{spell.CheckFor}]}}]}}"))
                        {
                            return CastReturn.CAST_TAKEHOLD;
                        }
                    }
				    recast:
					if (Casting.InRange(targetid, spell) && Casting.CheckReady(spell) && Casting.CheckMana(spell))
                    {
                        
                        var returnValue = Casting.Cast(targetid, spell, null, true);
						if(returnValue== CastReturn.CAST_FIZZLE)
                        {
                            goto recast;
                        }
                        return returnValue;
                    }
                    else
                    {
                        //spell isn't quite ready yet pause for 1.5 sec
                    }
                }

                return CastReturn.CAST_INVALID;
            }
            finally
            { 
                //put the target back to where it was
                Int32 currentTargetID = MQ.Query<Int32>("${Target.ID}");
                if (orgTargetID > 0 && currentTargetID != orgTargetID)
                {
                    bool orgTargetCorpse = MQ.Query<bool>($"${{Spawn[id {orgTargetID}].Type.Equal[Corpse]}}");
                    if (!orgTargetCorpse)
                    {
                        Casting.TrueTarget(orgTargetID);
                    }
                }

            }
            
        }
    }
}
