using E3Core.Processors;
using E3Core.Settings;
using System;
using E3Core.Classes;
using E3Core.Data;
using E3Core.Utility;
using MonoCore;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using Microsoft.Win32;

namespace E3Core.Classes
{
    /// <summary>
    /// Properties and methods specific to the cleric class
    /// </summary>
    public static class Cleric
    {
        private static Logging _log = E3.Log;
        private static IMQ MQ = E3.Mq;
        private static ISpawns _spawns = E3.Spawns;
        private static bool _isInit = false;
        private static long _nextRezCheck = 0;
        private static long _nextRezCheckInterval = 10000;
        private static List<string> _resSpellList = new List<string>()
        {
            "Blessing of Resurrection",
            "Water Sprinkler of Nem Ankh",
            "Reviviscence",
            "Token of Resurrection",
            "Spiritual Awakening",
            "Resurrection",
            "Restoration",
            "Resuscitate",
            "Renewal",
            "Revive",
            "Reparation"
        };

        private static List<Spell> _currentRezSpells = new List<Spell>();

        /// <summary>
        /// Initializes this instance.
        /// </summary>
        [ClassInvoke(Class.Cleric)]
        public static void Init()
        {
            if (_isInit) return;
            InitRezSpells();
            _isInit = true;
        }

        [ClassInvoke(Class.Cleric)]
        public static void AutoRez()
        {
            if (!e3util.ShouldCheck(ref _nextRezCheck, _nextRezCheckInterval)) return;
            foreach (var corpse in WaitForRez.CreateCorpseList())
            {
                var currentMana = MQ.Query<int>("${Me.CurrentMana}");
                var pctMana = MQ.Query<int>("${Me.PctMana}");
                if (Heals.SomeoneNeedsHealing(currentMana, pctMana))
                {
                    return;
                }

                if (_spawns.TryByID(corpse, out var spawn))
                {
                    if (Casting.TrueTarget(spawn.ID))
                    {
                        MQ.Cmd($"/t {spawn.DiplayName} Wait4Rez");
                        MQ.Delay(100);
                        MQ.Cmd("/corpse");
                        foreach (var spell in _currentRezSpells)
                        {
                            if (Casting.CheckReady(spell) && Casting.CheckMana(spell))
                            {
                                Casting.Cast(spawn.ID, spell);
                                break;
                            }
                        }
                    }
                }
            }
        }

        private static void InitRezSpells()
        {
            foreach (var spellName in _resSpellList)
            {
                if (MQ.Query<bool>($"${{FindItem[={spellName}]}}") || MQ.Query<bool>($"${{Me.AltAbility[{spellName}]}}") || MQ.Query<bool>($"${{Me.Book[{spellName}]}}"))
                {
                    _currentRezSpells.Add(new Spell(spellName));
                }
            }
        }
    }
}
