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
using System.Xml.Linq;

namespace E3Core.Processors
{
    public static class InterruptCast
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
            EventProcessor.RegisterCommand("/interruptcast", (x) =>
            {
                if (x.args.Count > 1)
                {
                    //interruptcast person "spell name" targetid
                    //interruptcast me "spell name" targetid
                    //interruptcast rockn "spell name" targetid
                    //interruptcast all "spell name"
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
                            E3.Bots.BroadcastCommandToGroup($"/interruptcast me \"{spell}\" {targetid}",x);
                            castResult = InterruptCastSpell(spell, targetid);
                        }
                        else
                        {
                            E3.Bots.BroadcastCommandToGroup($"/interruptcast me \"{spell}\"", x);
                            castResult = InterruptCastSpell(spell, 0);

                        }

                    }
                    else if (user.Equals("me", StringComparison.OrdinalIgnoreCase))
                    {
                        if (targetid > 0)
                        {
                            castResult = InterruptCastSpell(spell, targetid);
                        }
                        else
                        {
                            castResult = InterruptCastSpell(spell, 0);
                        }
                    }
                    else
                    {
                        if (targetid > 0)
                        {
                            //send this to a person!
                            E3.Bots.BroadcastCommandToPerson(user, $"/interruptcast me \"{spell}\" {targetid}");
                        }
                        else
                        {
                            //send this to a person!
                            E3.Bots.BroadcastCommandToPerson(user, $"/interruptcast me \"{spell}\"");


                        }
                    }

                    if (castResult != CastReturn.CAST_SUCCESS)
                    {
                        E3.Bots.Broadcast($"\arInterruptcast of {spell} unsuccessful due to {castResult}!");
                    }
                }
            });

        }

        private static CastReturn InterruptCastSpell(string spellName, Int32 targetid)
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
                    if (Casting.IsCasting())
                    {
                        E3.Bots.Broadcast($"\arInterruptcast interrupted a spell!");
                        MQ.Cmd("/stopcast");
                    }
                    if (MQ.Query<Int32>("${Me.CurrentMana}") > 0)
                    {
                        while (Casting.InGlobalCooldown())
                        {
                            E3.Bots.Broadcast($"\arInGlobalCooldown True, waiting 100 ms");
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
					
					if (spell.CheckForCollection.Count > 0)
					{
						foreach (var checkforItem in spell.CheckForCollection.Keys)
						{
							Casting.TrueTarget(targetid);
							if (MQ.Query<bool>($"${{Bool[${{Target.Buff[{checkforItem}]}}]}}"))
							{
								return CastReturn.CAST_TAKEHOLD;
							}
						}
						
					}
				recast:
					if (!Casting.CheckReady(spell))
                    {
                        return CastReturn.CAST_NOTREADY;
                    }
                    if(!Casting.InRange(targetid,spell))
                    {
                        return CastReturn.CAST_OUTOFRANGE;
                    }
					if (!Casting.CheckMana(spell))
					{
						return CastReturn.CAST_OUTOFMANA;
					}
			        var returnValue = Casting.Cast(targetid, spell, null, true);
					if(returnValue== CastReturn.CAST_FIZZLE)
                    {
                        goto recast;
                    }
                    return returnValue;
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
