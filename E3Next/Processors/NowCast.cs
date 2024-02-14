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
					bool shouldForce = x.args.Contains("force", StringComparer.OrdinalIgnoreCase);
					
                    user = x.args[0];
                    spell = x.args[1];
                    if(x.args.Count>2)
                    {
                        Int32.TryParse(x.args[2], out targetid);
                    }

                    CastReturn castResult = CastReturn.CAST_SUCCESS;
                    if (user.Equals("all", StringComparison.OrdinalIgnoreCase))
                    {
                        string broadcastCommand = $"/nowcast me \"{spell}\"";
                        if (targetid > 0)
                        {
                            broadcastCommand += $" {targetid}";
                        }
                        if (shouldForce)
                        {
                            broadcastCommand += " force";
                        }
                        E3.Bots.BroadcastCommandToGroup(broadcastCommand, x);
                        castResult = NowCastSpell(spell, targetid, shouldForce);
                    }
                    else if (user.Equals("me", StringComparison.OrdinalIgnoreCase))
                    {
                        castResult = NowCastSpell(spell, targetid, shouldForce);
                    }
                    else
                    {
                        string broadcastCommand = $"/nowcast me \"{spell}\"";
                        if (targetid > 0)
                        {
                            broadcastCommand += $" {targetid}";
                        }
                        if (shouldForce)
                        {
                            broadcastCommand += " force";
                        }
                        E3.Bots.BroadcastCommandToPerson(user, broadcastCommand);
                    }

                    if (castResult != CastReturn.CAST_SUCCESS)
                    {
                        E3.Bots.Broadcast($"\arNowcast of {spell} unsuccessful due to {castResult}!");
                        if (castResult== CastReturn.CAST_NOTREADY)
                        {
                            Basics.PrintE3TReport(new Spell(spell));
                        }
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
        private static CastReturn NowCastSpell(string spellName, Int32 targetid, bool shouldForce = false)
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
                    if (Casting.IsCasting() && shouldForce)
                    {
                        MQ.Cmd("/stopcast");
                    }
                    while (Casting.IsCasting() && !shouldForce)
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
