using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using E3Core.Processors;
using MonoCore;

namespace E3Core.Classes
{
    public static class Rogue
    {
        public static Logging _log = E3._log;
        private static IMQ MQ = E3.MQ;
        private static ISpawns _spawns = E3._spawns;
        private static Data.Spell _rogueSneakAttack = null;
        public static void RogueStrike()
        {
            string sneakattack = E3._characterSettings.Rogue_SneakAttack;

            if(_rogueSneakAttack==null)
            {
                _rogueSneakAttack = new Data.Spell(sneakattack);
            }

            if(_rogueSneakAttack.CastType!= Data.CastType.None)
            {
                if (MQ.Query<bool>($"${{Me.CombatAbilityReady[{sneakattack}]}}") && MQ.Query<bool>($"${{Me.AbilityReady[Backstab]}}"))
                {
                    if(MQ.Query<bool>("${Me.Invis}") && MQ.Query<bool>("${Me.Sneaking}") && !MQ.Query<bool>("${Me.ActiveDisc.ID}"))
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
}
