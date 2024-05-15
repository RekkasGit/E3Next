using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using E3Core.Processors;
using E3Core.Settings;
using E3Core.Utility;
using MonoCore;

namespace E3Core.Classes
{
    /// <summary>
    /// Properties and methods specific to the rogue class
    /// </summary>
    public static class Rogue
    {
        private static Logging _log = E3.Log;
        private static IMQ MQ = E3.MQ;
        private static ISpawns _spawns = E3.Spawns;
        private static Data.Spell _rogueSneakAttack = null;
        private static long _nextHideCheck = 0;
        private static long _nextHideCheckInterval = 1000;
		private static bool _isInit = false;

		[ClassInvoke(Data.Class.Rogue)]
		public static void Init()
		{
			if (_isInit) return;
			RegisterCommands();
			_isInit = true;
		}
		public static void RegisterCommands()
		{
			EventProcessor.RegisterCommand("/e3rogue-autohide", (x) =>
			{
				if (x.args.Count > 0)
				{
					if (x.args[0].Equals("off", StringComparison.OrdinalIgnoreCase))
					{
						E3.Bots.Broadcast("Turning off Rogue Auto Hide");
						E3.CharacterSettings.Rogue_AutoHide = false;
					}
				}
				else
				{
					E3.Bots.Broadcast("Turning on Rogue Auto Hide");
					E3.CharacterSettings.Rogue_AutoHide = true;
				}
			});
		}

		/// <summary>
		/// Performs a sneak attack.
		/// </summary>
		public static void RogueStrike()
        {
            using(_log.Trace())
            {
                string sneakattack = E3.CharacterSettings.Rogue_SneakAttack;

                if (_rogueSneakAttack == null)
                {
                    _rogueSneakAttack = new Data.Spell(sneakattack);
                }

                if (_rogueSneakAttack.CastType != Data.CastingType.None)
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

        [ClassInvoke(Data.Class.Rogue)]
        public static void AutoHide()
        {
			if (E3.ActionTaken) return;
            if (!E3.CharacterSettings.Rogue_AutoHide) return;
            if (!e3util.ShouldCheck(ref _nextHideCheck, _nextHideCheckInterval)) return;
            if (MQ.Query<bool>("${Me.Invis}")) return;
            if (MQ.Query<bool>("${Me.Moving}")) return;
            if (Zoning.CurrentZone.IsSafeZone) return;
            if (Basics.InCombat()) return;


            var sneakQuery = "${Me.Sneaking}";
            if (!MQ.Query<bool>(sneakQuery) && MQ.Query<bool>("${Me.AbilityReady[Sneak]"))
            {
                MQ.Cmd("/doability sneak");
                MQ.Delay(1000, sneakQuery);
            }

            if (MQ.Query<bool>(sneakQuery) && MQ.Query<bool>("${Me.AbilityReady[Hide]"))
            {
                BuffCheck.Check_Buffs();
                MQ.Cmd("/doability hide");
            }
        }

        /// <summary>
        /// Evades if over the specified aggro threshold from the toon's ini.
        /// </summary>
        public static void AutoEvade()
        {
            using(_log.Trace())
            {
                if (MQ.Query<Int32>("${Me.PctAggro}") > E3.CharacterSettings.Rogue_EvadePct)
                {
                    if (MQ.Query<bool>("${Me.AbilityReady[Hide]}"))
                    {
                        MQ.Cmd("/attack off");
                        MQ.Delay(1000, "${If[${Me.Combat},FALSE,TRUE]}");
                        MQ.Delay(500);
                        MQ.Cmd("/doability Hide");
                        MQ.Delay(1500, "${Me.Invis}");
                        MQ.Cmd("/attack on");

                    }
                }
            }
        }
    }
}
