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
        public static Logging _log = E3._log;
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

                    if (user.Equals("all", StringComparison.OrdinalIgnoreCase))
                    {
                        if (targetid > 0)
                        {
                            E3._bots.BroadcastCommandToGroup($"/nowcast me \"{spell}\" {targetid}");
                            NowCastSpell(spell, targetid);
                        }
                        else
                        {
                              E3._bots.BroadcastCommandToGroup($"/nowcast me \"{spell}\"");
                            NowCastSpell(spell, 0);

                        }

                    }
                    else if (user.Equals("me", StringComparison.OrdinalIgnoreCase))
                    {
                        if (targetid > 0)
                        {
                            NowCastSpell(spell, targetid);
                        }
                        else
                        {
                            NowCastSpell(spell, 0);
                        }
                    }
                    else
                    {
                        if (targetid > 0)
                        {
                            //send this to a person!
                            E3._bots.BroadcastCommandToPerson(user, $"/nowcast me \"{spell}\" {targetid}");

                        }
                        else
                        {
                            //send this to a person!
                            E3._bots.BroadcastCommandToPerson(user, $"/nowcast me \"{spell}\"");


                        }
                    }

                }
            });

        }

        private static void NowCastSpell(string spellName, Int32 targetid)
        {
            string realSpell = string.Empty;
            if (BegForBuffs._spellAliases.TryGetValue(spellName, out realSpell))
            {
                spellName = realSpell;
            }
            Spell spell = new Spell(spellName);
            
            if(spell.SpellID>0)
            {

                //wait for GCD to be over.
                bool wasCasting = false;
                while (Casting.IsCasting())
                {
                    wasCasting = true;
                    MQ.Delay(50);
                }
                if(wasCasting)
                {
                    MQ.Delay(600);
                }
                if (MQ.Query<Int32>("${Me.CurrentMana}") >0)
                {
                    while (Casting.InGlobalCooldown())
                    {
                        MQ.Delay(100);
                    }
                }

                if(Casting.InRange(targetid, spell) && Casting.CheckReady(spell) && Casting.CheckMana(spell))
                {
                    Casting.Cast(targetid, spell, null, true);
                }
                else
                {
                    //spell isn't quite ready yet pause for 1.5 sec
                }
            }
        }
    }
}
