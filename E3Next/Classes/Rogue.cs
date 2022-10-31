using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using E3Core.Processors;
using MonoCore;

namespace E3Core.Classes
{
    /// <summary>
    /// Properties and methods specific to the rogue class
    /// </summary>
    public static class Rogue
    {
        private static Logging _log = E3._log;
        private static IMQ MQ = E3.MQ;
        private static ISpawns _spawns = E3._spawns;
        private static Data.Spell _rogueSneakAttack = null;

        /// <summary>
        /// Performs a sneak attack.
        /// </summary>
        public static void RogueStrike()
        {
            using(_log.Trace())
            {
                string sneakattack = E3._characterSettings.Rogue_SneakAttack;

                if (_rogueSneakAttack == null)
                {
                    _rogueSneakAttack = new Data.Spell(sneakattack);
                }

                if (_rogueSneakAttack.CastType != Data.CastType.None)
                {
                    if (MQ.Query<bool>($"${{Me.CombatAbilityReady[{sneakattack}]}}") && MQ.Query<bool>($"${{Me.AbilityReady[Backstab]}}"))
                    {
                        if (MQ.Query<bool>("${Me.Invis}") && MQ.Query<bool>("${Me.Sneaking}") && !MQ.Query<bool>("${Me.ActiveDisc.ID}"))
                        {
                            Int32 endurance = MQ.Query<Int32>("${Me.Endurance}");
                            Int32 enduranceCost = MQ.Query<Int32>($"${{Spell[{sneakattack}].EnduranceCost}}");
                            Int32 minEndurnace = _rogueSneakAttack.MinEnd;
                            Int32 pctEndurance = MQ.Query<Int32>("${Me.PctEndurance}");

                            if (endurance > enduranceCost && pctEndurance > minEndurnace)
                            {
                                MQ.Cmd($"/disc {sneakattack}");
                                MQ.Delay(500, "${Bool[${Me.ActiveDisc.ID}]}");
                                MQ.Delay(300);
                                MQ.Cmd("/doability Backstab");
                                MQ.Delay(100);
                            }
                        }
                    }
                }
            }
            
        }

        /// <summary>
        /// Evades if over the specified aggro threshold from the toon's ini.
        /// </summary>
        public static void AutoEvade()
        {
            using(_log.Trace())
            {
                if (MQ.Query<Int32>("${Me.PctAggro}") > E3._characterSettings.Rogue_EvadePct)
                {
                    if (MQ.Query<bool>("${Me.AbilityReady[Hide]}"))
                    {
                        if (!MQ.Query<bool>("${Bool[${Me.ActiveDisc.ID}]}"))
                        {
                            MQ.Cmd("/attack off");
                            MQ.Delay(1000, "${Bool[!${Me.Combat}]}");
                            MQ.Delay(500);
                            MQ.Cmd("/doability Hide");
                            MQ.Delay(500, "${Me.Invis}");
                            MQ.Cmd("/attack on");
                        }
                    }
                }
            }
        }
    }
}
