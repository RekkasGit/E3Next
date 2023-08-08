using E3Core.Classes;
using E3Core.Data;
using E3Core.Settings;
using E3Core.Utility;
using MonoCore;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace E3Core.Processors
{
    public static class Burns
    {
        public static Logging _log = E3.Log;
        private static IMQ MQ = E3.MQ;
        private static ISpawns _spawns = E3.Spawns;

        public static bool use_FULLBurns = false;
        public static bool use_QUICKBurns = false;
        public static bool use_EPICBurns = false;
        public static bool use_LONGBurns = false;
        public static bool use_Swarms = false;
        public static List<Data.Spell> _epicWeapon = new List<Data.Spell>();
        public static List<Data.Spell> _anguishBP = new List<Data.Spell>();
        public static List<Data.Spell> _swarmPets = new List<Spell>();
        public static string _epicWeaponName = String.Empty;
        public static string _anguishBPName = String.Empty;
        private static Int64 _nextBurnCheck = 0;
        private static Int64 _nextBurnCheckInterval = 50;


        private static Int64 _quickburnStartTimeStamp = 0;
        private static Int32 _quickburnTimeout = 0;
        private static Int64 _longburnStartTimeStamp = 0;
        private static Int32 _longburnTimeout = 0;
        private static Int64 _fullburnStartTimeStamp = 0;
        private static Int32 _fullburnTimeout = 0;
        private static Int64 _swarmStartTimeStamp = 0;
        private static Int32 _swarmburnTimeout = 0;
        private static Int64 _epicStartTimeStamp = 0;
        private static Int32 _epicburnTimeout = 0;



        [SubSystemInit]
        public static void Init()
        {
            RegisterEpicAndAnguishBP();
            RegisterSwarppets();
            RegisterEvents();
        }

        public static void Reset()
        {
            use_FULLBurns = false;
            use_QUICKBurns = false;
            use_EPICBurns = false;
            use_LONGBurns = false;
            use_Swarms = false;
            _quickburnStartTimeStamp = 0;
            _quickburnTimeout = 0;
            _longburnStartTimeStamp = 0;
            _longburnTimeout = 0;
            _fullburnStartTimeStamp = 0;
            _fullburnTimeout = 0;
            _swarmStartTimeStamp = 0;
            _swarmburnTimeout = 0;
            _epicStartTimeStamp = 0;
            _epicburnTimeout = 0;
        }
        private static void RegisterEvents()
        {
            EventProcessor.RegisterCommand("/swarmpets", (x) =>
            {
                ProcessBurnRequest("/swarmpets", x, ref use_Swarms, ref _swarmburnTimeout, ref _swarmStartTimeStamp);
            });
            EventProcessor.RegisterCommand("/epicburns", (x) =>
            {
                ProcessBurnRequest("/epicburns", x, ref use_EPICBurns, ref _epicburnTimeout, ref _epicStartTimeStamp);
            });
            EventProcessor.RegisterCommand("/quickburns", (x) =>
            {
                ProcessBurnRequest("/quickburns", x, ref use_QUICKBurns, ref _quickburnTimeout, ref _quickburnStartTimeStamp);

            });
            EventProcessor.RegisterCommand("/fullburns", (x) =>
            {
                ProcessBurnRequest("/fullburns", x, ref use_FULLBurns, ref _fullburnTimeout, ref _fullburnStartTimeStamp);

            });
            EventProcessor.RegisterCommand("/longburns", (x) =>
            {
                ProcessBurnRequest("/longburns", x, ref use_LONGBurns, ref _longburnTimeout, ref _longburnStartTimeStamp);
            });


        }

        public static void UseBurns()
        {
            //  if (!e3util.ShouldCheck(ref _nextBurnCheck, _nextBurnCheckInterval)) return;
            //lets check if there are any events in the queue that we need to check on. 
            EventProcessor.ProcessEventsInQueues("/quickburns");
			EventProcessor.ProcessEventsInQueues("/epicburns");
			EventProcessor.ProcessEventsInQueues("/fullburns");
			EventProcessor.ProcessEventsInQueues("/longburns");
			CheckTimeouts();

            UseBurn(_epicWeapon, use_EPICBurns, "EpicBurns");
            UseBurn(_anguishBP, use_EPICBurns, "AnguishBPBurns");
            UseBurn(E3.CharacterSettings.QuickBurns, use_QUICKBurns, nameof(E3.CharacterSettings.QuickBurns));
            UseBurn(E3.CharacterSettings.FullBurns, use_FULLBurns, nameof(E3.CharacterSettings.FullBurns));
            UseBurn(E3.CharacterSettings.LongBurns, use_LONGBurns, nameof(E3.CharacterSettings.LongBurns));
            UseBurn(_swarmPets, use_Swarms, "SwarmPets");

        }

        public static void CheckTimeouts()
        {
           
            if (use_QUICKBurns) CheckTimeouts_SubCheck(ref use_QUICKBurns, ref _quickburnTimeout, ref _quickburnStartTimeStamp, "QuickBurns");
            if (use_LONGBurns) CheckTimeouts_SubCheck(ref use_LONGBurns, ref _longburnTimeout, ref _longburnStartTimeStamp, "LongBurns");
            if (use_FULLBurns) CheckTimeouts_SubCheck(ref use_FULLBurns, ref _fullburnTimeout, ref _fullburnStartTimeStamp, "FullBurns");
            if (use_EPICBurns) CheckTimeouts_SubCheck(ref use_EPICBurns, ref _epicburnTimeout, ref _epicStartTimeStamp,"EpicBurns");
            if (use_Swarms) CheckTimeouts_SubCheck(ref use_Swarms, ref _swarmburnTimeout, ref _swarmStartTimeStamp, "SwarmPets");

        }
        private static void CheckTimeouts_SubCheck(ref bool burnType, ref Int32 timeoutForBurn, ref Int64 timeoutTimeStamp, string name)
        {
            if (burnType && timeoutTimeStamp > 0)
            {   //turn off after 60 seconds
                if (timeoutTimeStamp + (timeoutForBurn * 1000) < Core.StopWatch.ElapsedMilliseconds)
                {
                    E3.Bots.Broadcast($"Turning off {name} due to timeout of : {timeoutForBurn}");
                    burnType = false;
                    timeoutTimeStamp = 0;
                    timeoutForBurn = 0;
                }
            }
        }
        private static void UseBurn(List<Data.Spell> burnList, bool use, string burnType)
        {
            if (!Assist.IsAssisting) return;

            if (use)
            {
                Int32 previousTarget = MQ.Query<Int32>("${Target.ID}");
                foreach (var burn in burnList)
                {
                    //can't do gathering dusk if not in combat, skip it
                    if (burn.SpellName == "Gathering Dusk" && !Basics.InGameCombat()) continue;
                    if (burn.TargetType == "Pet" && MQ.Query<int>("${Me.Pet.ID}") < 1) continue;

                    if (!String.IsNullOrWhiteSpace(burn.Ifs))
                    {
                        if (!Casting.Ifs(burn))
                        {
                            continue;
                        }
                    }
                    if (!String.IsNullOrWhiteSpace(burn.CheckFor))
                    {
                        if (MQ.Query<bool>($"${{Bool[${{Me.Buff[{burn.CheckFor}]}}]}}") || MQ.Query<bool>($"${{Bool[${{Me.Song[{burn.CheckFor}]}}]}}"))
                        {
                            continue;
                        }
                    }

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

                        bool targetPC = false;
                        bool isMyPet = false;
                        bool isGroupMember = false;
                        if (_spawns.TryByID(previousTarget, out var spawn))
                        {
                            Int32 groupMemberIndex = MQ.Query<Int32>($"${{Group.Member[{spawn.CleanName}].Index}}");
                            if (groupMemberIndex > 0) isGroupMember = true;
                            targetPC = (spawn.TypeDesc == "PC");
                            isMyPet = (previousTarget == MQ.Query<Int32>("${Me.Pet.ID}"));

                        }
                        var chatOutput = $"/g {burnType}: {burn.CastName}";
                        //so you don't target other groups or your pet for burns if your target happens to be on them.
                        if (((isMyPet) || (targetPC && !isGroupMember)) && (burn.TargetType == "Group v1" || burn.TargetType == "Group v2"))
                        {
                            Casting.Cast(E3.CurrentId, burn);
                            if (previousTarget > 0)
                            {
                                Int32 currentTarget = MQ.Query<Int32>("${Target.ID}");
                                if (previousTarget != currentTarget)
                                {
                                    Casting.TrueTarget(previousTarget);
                                }
                            }
                            MQ.Cmd(chatOutput);
                        }
                        else
                        {
                            Casting.Cast(0, burn);
                            MQ.Cmd(chatOutput);
                        }
                    }
                }

            }
        }
        private static void ProcessBurnRequest(string command, EventProcessor.CommandMatch x, ref bool burnType, ref Int32 timeoutForBurn, ref Int64 timeoutTimeStamp)
        {
            Int32 mobid;

            Int32 timeout = 0;

            bool containsTimeout = false;
            foreach (var value in x.args)
            {
                if (value.StartsWith("timeout=", StringComparison.OrdinalIgnoreCase))
                {
                    containsTimeout = true;
                    break;
                }
            }

            if (containsTimeout)
            {
                List<string> newargs = new List<string>();
                foreach (var value in x.args)
                {
                    if (!value.StartsWith("timeout=", StringComparison.OrdinalIgnoreCase))
                    {
                        newargs.Add(value);
                    }
                    else
                    {
                        //have a timeout specified
                        string tmpTimeout = value.Split('=')[1];
                        Int32.TryParse(tmpTimeout, out timeout);
                    }
                }
                x.args.Clear();
                x.args.AddRange(newargs);
                newargs.Clear();
            }

            if (x.args.Count > 0)
            {
                if (Int32.TryParse(x.args[0], out mobid))
                {
                    if (!e3util.FilterMe(x))
                    {
                       
                        burnType = true;
                        if (timeout > 0)
                        {
                            timeoutForBurn = timeout;
                            timeoutTimeStamp = Core.StopWatch.ElapsedMilliseconds;
                        }
                        else
                        {
                            timeoutForBurn = 0;
                            timeoutTimeStamp = 0;
                        }
                    }
                }
                else
                {
                    E3.Bots.Broadcast($"\arNeed a valid target to {command}.");
                }
            }
            else
            {
                Int32 targetID = MQ.Query<Int32>("${Target.ID}");
                if (targetID > 0)
                {
                   
                    if(timeout>0)
                    {
                        E3.Bots.BroadcastCommandToGroup($"{command} {targetID} timeout={timeout}", x);
                    }
                    else
                    {
                        E3.Bots.BroadcastCommandToGroup($"{command} {targetID}", x);
                    }
                    if (!e3util.FilterMe(x))
                    {
                        burnType = true;
                        if (timeout > 0)
                        {
                            timeoutForBurn = timeout;
                            timeoutTimeStamp = Core.StopWatch.ElapsedMilliseconds;
                        }
                        else
                        {
                            timeoutForBurn = 0;
                            timeoutTimeStamp = 0;
                        }
                    }
                }
                else
                {
                    MQ.Write($"\arNeed a target to {command}");
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
        private static void RegisterSwarppets()
        {
            foreach (string pet in _swarmPetList)
            {
                Data.Spell tSpell;
                if (MQ.Query<bool>($"${{Me.AltAbility[{pet}]}}"))
                {
                    tSpell = new Spell(pet);
                    _swarmPets.Add(tSpell);
                    continue;
                }
                if (MQ.Query<Int32>($"${{FindItemCount[={pet}]}}") > 0)
                {
                    tSpell = new Spell(pet);
                    _swarmPets.Add(tSpell);
                }
            }
        }
        private static List<string> _swarmPetList = new List<string>() {"Servant of Ro","Host of the Elements",
         "Swarm of Decay","Rise of Bones","Graverobber's Icon","Soulwhisper","Deathwhisper",
         "Wake the Dead","Spirit Call", "Shattered Gnoll Slayer", "Call of Xuzl","Song of Stone",
         "Tarnished Skeleton Key","Celestial Hammer","Graverobber's Icon","Battered Smuggler's Barrel",
         "Phantasmal Opponent","Projection of Piety","Spirits of Nature", "Nature's Guardian"
        };
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
