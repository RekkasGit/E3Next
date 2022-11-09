using E3Core.Classes;
using E3Core.Data;
using E3Core.Settings;
using E3Core.Utility;
using MonoCore;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;

namespace E3Core.Processors
{
    public static class Burns
    {
        public static Logging _log = E3.Log;
        private static IMQ MQ = E3.Mq;
        private static ISpawns _spawns = E3.Spawns;

        public static bool use_FULLBurns = false;
        public static bool use_QUICKBurns = false;
        public static bool use_EPICBurns = false;
        public static bool use_LONGBurns = false;
        public static bool use_Swarms = false;
        public static List<Data.Spell> _epicWeapon = new List<Data.Spell>();
        public static List<Data.Spell> _anguishBP = new List<Data.Spell>();
        public static string _epicWeaponName = String.Empty;
        public static string _anguishBPName = String.Empty;
        private static Int64 _nextBurnCheck = 0;
        private static Int64 _nextBurnCheckInterval = 500;

        [SubSystemInit]
        public static void Init()
        {
            RegisterEpicAndAnguishBP();
            RegisterEvents();
        }

        public static void Reset()
        {
            use_FULLBurns = false;
            use_QUICKBurns = false;
            use_EPICBurns = false;
            use_LONGBurns = false;
            use_Swarms = false;
        }
        private static void RegisterEvents()
        {
            EventProcessor.RegisterCommand("/swarmpets", (x) =>
            {
                ProcessBurnRequest("/swarmpets", x, ref use_Swarms);
            });
            EventProcessor.RegisterCommand("/epicburns", (x) =>
            {
                ProcessBurnRequest("/epicburns", x, ref use_EPICBurns);
            });
            EventProcessor.RegisterCommand("/quickburns", (x) =>
            {
                ProcessBurnRequest("/quickburns", x, ref use_QUICKBurns);

            });
            EventProcessor.RegisterCommand("/fullburns", (x) =>
            {
                ProcessBurnRequest("/fullburns", x, ref use_FULLBurns);

            });
            EventProcessor.RegisterCommand("/longburns", (x) =>
            {
                ProcessBurnRequest("/longburns", x, ref use_LONGBurns);

            });


        }
        
        public static void UseBurns()
        {
            if (!e3util.ShouldCheck(ref _nextBurnCheck, _nextBurnCheckInterval)) return;

            UseBurn(_epicWeapon, use_EPICBurns);
            UseBurn(_anguishBP, use_EPICBurns);
            UseBurn(E3.CharacterSettings.QuickBurns, use_QUICKBurns);
            UseBurn(E3.CharacterSettings.FullBurns, use_FULLBurns);
            UseBurn(E3.CharacterSettings.LongBurns, use_LONGBurns);

        }
        private static void UseBurn(List<Data.Spell> burnList, bool use)
        {
            if (!Assist._isAssisting) return;

            if (use)
            {
                foreach (var burn in burnList)
                {
                    //can't do gathering dusk if not in combat, skip it
                    if (burn.SpellName == "Gathering Dusk" && !Basics.InGameCombat()) continue;

                    if (Casting.CheckReady(burn))
                    {
                        if (burn.CastType == Data.CastType.Disc)
                        {
                            if (burn.TargetType == "Self")
                            {
                                if (MQ.Query<bool>("${Me.ActiveDisc.ID}"))
                                {
                                    continue;

                                }
                            }
                        }
                        Casting.Cast(0, burn);
                    }
                }
            }
        }
        private static void ProcessBurnRequest(string command, EventProcessor.CommandMatch x, ref bool burnType)
        {
            Int32 mobid;
            if (x.args.Count > 0)
            {
                if (Int32.TryParse(x.args[0], out mobid))
                {
                    burnType = true;
                }
                else
                {
                    E3.Bots.Broadcast($"\aNeed a valid target to {command}.");
                }
            }
            else
            {
                Int32 targetID = MQ.Query<Int32>("${Target.ID}");
                if (targetID > 0)
                {
                    //we are telling people to follow us
                    E3.Bots.BroadcastCommandToGroup($"{command} {targetID}");
                    burnType = true;
                }
                else
                {
                    MQ.Write($"\aNeed a target to {command}");
                }
            }
        }
        private static void RegisterEpicAndAnguishBP()
        {
            foreach (string name in _epicList)
            {
                if (MQ.Query<Int32>($"${{FindItemCount[={name}]}}") > 0)
                {
                    _epicWeaponName = name;
                }
            }

            foreach (string name in _anguishBPList)
            {
                if (MQ.Query<Int32>($"${{FindItemCount[={name}]}}") > 0)
                {
                    _anguishBPName = name;
                }
            }

            if (!String.IsNullOrWhiteSpace(_epicWeaponName))
            {
                _epicWeapon.Add(new Data.Spell(_epicWeaponName));
            }
            if (!String.IsNullOrWhiteSpace(_anguishBPName))
            {
                _anguishBP.Add(new Data.Spell(_anguishBPName));
            }

        }
        private static List<string> _anguishBPList = new List<string>() {
            "Bladewhisper Chain Vest of Journeys",
            "Farseeker's Plate Chestguard of Harmony",
            "Wrathbringer's Chain Chestguard of the Vindicator",
            "Savagesoul Jerkin of the Wilds",
            "Glyphwielder's Tunic of the Summoner",
            "Whispering Tunic of Shadows",
            "Ritualchanter's Tunic of the Ancestors"};

        private static List<string> _epicList = new List<string>() {
            "Prismatic Dragon Blade",
            "Blade of Vesagran",
            "Raging Taelosian Alloy Axe",
            "Vengeful Taelosian Blood Axe",
            "Staff of Living Brambles",
            "Staff of Everliving Brambles",
            "Fistwraps of Celestial Discipline",
            "Transcended Fistwraps of Immortality",
            "Redemption",
            "Nightbane, Sword of the Valiant",
            "Heartwood Blade",
            "Heartwood Infused Bow",
            "Aurora, the Heartwood Blade",
            "Aurora, the Heartwood Bow",
            "Fatestealer",
            "Nightshade, Blade of Entropy",
            "Innoruuk's Voice",
            "Innoruuk's Dark Blessing",
            "Crafted Talisman of Fates",
            "Blessed Spiritstaff of the Heyokah",
            "Staff of Prismatic Power",
            "Staff of Phenomenal Power",
            "Soulwhisper",
            "Deathwhisper"};
    }
}
