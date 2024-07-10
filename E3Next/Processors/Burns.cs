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

        /// Contains timestamp when the burn started
        private static Dictionary<string, Int64> _burnsStartTimestampDictionary = new Dictionary<string, Int64>();
        /// Contains how long to run the burn for, if passed in as param
        private static Dictionary<string, Int32> _burnsTimeoutDictionary = new Dictionary<string, Int32>();
        /// Contains if burn is in use
        private static Dictionary<string, bool> _burnsUseDictionary = new Dictionary<string, bool>();

        [SubSystemInit]
        public static void Init()
        {
            RegisterEpicAndAnguishBP();
            RegisterSwarmpets();
            RegisterEvents();
        }

        public static void Reset()
        {
            _burnsStartTimestampDictionary = new Dictionary<string, Int64>();
            _burnsTimeoutDictionary = new Dictionary<string, Int32>();
            _burnsUseDictionary = new Dictionary<string, bool>();

            foreach (var burn in E3.CharacterSettings.BurnsDictionary) {
                _burnsStartTimestampDictionary[burn.Key] = 0;
                _burnsTimeoutDictionary[burn.Key] = 0;
                _burnsUseDictionary[burn.Key] = false;
            }
        }
        private static void RegisterEvents()
        {
            EventProcessor.RegisterCommand("/e3burns", (x) => {
                if (x.args.Count > 0)
                {
                    var burnName = x.args[0].ToLower();
                    x.args.RemoveAt(0);
                    ProcessBurnRequest("/e3burns", x, burnName);
                }
            });
            EventProcessor.RegisterCommand("/swarmpets", (x) =>
            {
                ProcessBurnRequest("/e3burns", x, "swarmpets");
            });
            EventProcessor.RegisterCommand("/epicburns", (x) =>
            {
                ProcessBurnRequest("/e3burns", x, "epic burn");
            });
            EventProcessor.RegisterCommand("/quickburns", (x) =>
            {
                ProcessBurnRequest("/e3burns", x, "quick burn");
            });
            EventProcessor.RegisterCommand("/fullburns", (x) =>
            {
                ProcessBurnRequest("/e3burns", x, "full burn");
            });
            EventProcessor.RegisterCommand("/longburns", (x) =>
            {
                ProcessBurnRequest("/e3burns", x, "long burn");
            });
        }

        public static void UseBurns()
        {
            //lets check if there are any events in the queue that we need to check on. 
            EventProcessor.ProcessEventsInQueues("/quickburns");
			EventProcessor.ProcessEventsInQueues("/epicburns");
			EventProcessor.ProcessEventsInQueues("/fullburns");
			EventProcessor.ProcessEventsInQueues("/longburns");
			EventProcessor.ProcessEventsInQueues("/swarmpets");
            EventProcessor.ProcessEventsInQueues("/e3burns");
			CheckTimeouts();

            foreach (var burn in E3.CharacterSettings.BurnsDictionary) {
                UseBurn(burn.Value, burn.Key);
            }
        }

        public static void CheckTimeouts()
        {
            List<string> burnsToProcess = new List<string>();

            /// Make copy of keys, since we'll be modifying burnsUseDictionary
            foreach (var val in _burnsUseDictionary)
            {
                if (val.Value)
                {
                    burnsToProcess.Add(val.Key);
                }
            }

            foreach (var burnName in burnsToProcess) {
                CheckTimeouts_SubCheck(burnName);
            }
        }
        private static void CheckTimeouts_SubCheck(string burnName)
        {
            if (!_burnsUseDictionary[burnName]) { return; }

            if (!_burnsStartTimestampDictionary.TryGetValue(burnName, out long keyTimeoutTimeStamp)) {
                keyTimeoutTimeStamp = 0;
            }
            if (!_burnsTimeoutDictionary.TryGetValue(burnName, out Int32 keyTimeoutForBurn)) {
                keyTimeoutForBurn = 0;
            }

            if (keyTimeoutTimeStamp > 0) 
            {   //turn off after 60 seconds
                if (keyTimeoutTimeStamp + (keyTimeoutForBurn * 1000) < Core.StopWatch.ElapsedMilliseconds) {
                    E3.Bots.Broadcast($"Turning off {burnName} due to timeout of : {keyTimeoutForBurn}");
                    _burnsUseDictionary[burnName] = false;
                    _burnsStartTimestampDictionary[burnName] = 0;
                    _burnsTimeoutDictionary[burnName] = 0;
                }
            }
        }

        private static void UseBurn(List<Data.Spell> burnList, string burnName)
        {
            if (!Assist.IsAssisting) return;
            _burnsUseDictionary.TryGetValue(burnName, out bool use);
            if (use)
            {
                Int32 previousTarget = MQ.Query<Int32>("${Target.ID}");
                foreach (var burn in burnList)
                {
                    if (MQ.Query<Int32>("${Me.CurrentHPs}") < 1) return; //can't burn if dead
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
                    bool shouldContinue = false;
					if (burn.CheckForCollection.Count > 0)
					{
						foreach (var checkforItem in burn.CheckForCollection.Keys)
						{
							if (MQ.Query<bool>($"${{Bool[${{Me.Buff[{checkforItem}]}}]}}") || MQ.Query<bool>($"${{Bool[${{Me.Song[{checkforItem}]}}]}}"))
							{
                                shouldContinue = true;
								break;
							}
						}
						if (shouldContinue) { continue; }
					}

                    if (Casting.CheckReady(burn))
                    {
                        if (burn.CastType == Data.CastingType.Disc)
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
                        var chatOutput = $"{burnName}: {burn.CastName}";
                        //so you don't target other groups or your pet for burns if your target happens to be on them.
						if(!String.IsNullOrWhiteSpace(burn.CastTarget) && _spawns.TryByName(burn.CastTarget, out var spelltarget))
						{

							Casting.Cast(spelltarget.ID, burn);
							if (previousTarget > 0)
							{
								Int32 currentTarget = MQ.Query<Int32>("${Target.ID}");
								if (previousTarget != currentTarget)
								{
									Casting.TrueTarget(previousTarget);
								}
							}
							E3.Bots.Broadcast(chatOutput);

						}
                        else if (((isMyPet) || (targetPC && !isGroupMember)) && (burn.TargetType == "Group v1" || burn.TargetType == "Group v2"))
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
                            E3.Bots.Broadcast(chatOutput);
                        }
                        else
                        {
                            Casting.Cast(0, burn);
                            E3.Bots.Broadcast(chatOutput);
                        }
                    }
                }

            }
        }

        private static void ProcessBurnRequest(string command, EventProcessor.CommandMatch x, string burnName){
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
                       _burnsUseDictionary[burnName] = true;
                        if (timeout > 0)
                        {
                            _burnsStartTimestampDictionary[burnName] = Core.StopWatch.ElapsedMilliseconds;
                            _burnsTimeoutDictionary[burnName] = timeout;
                        }
                        else
                        {
                            _burnsStartTimestampDictionary[burnName] = 0;
                            _burnsTimeoutDictionary[burnName] = 0;
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
                        E3.Bots.BroadcastCommandToGroup($"{command} \"{burnName}\" {targetID} timeout={timeout}", x);
                    }
                    else
                    {
                        E3.Bots.BroadcastCommandToGroup($"{command} \"{burnName}\" {targetID}", x);
                    }
                    if (!e3util.FilterMe(x))
                    {
                       _burnsUseDictionary[burnName] = true;
                        if (timeout > 0)
                        {
                            _burnsStartTimestampDictionary[burnName] = Core.StopWatch.ElapsedMilliseconds;
                            _burnsTimeoutDictionary[burnName] = timeout;
                        }
                        else
                        {
                            _burnsStartTimestampDictionary[burnName] = 0;
                            _burnsTimeoutDictionary[burnName] = 0;
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
            var epicWeaponName = "";
            var anguishBPName = "";

            foreach (string name in _epicList)
            {
                if (MQ.Query<Int32>($"${{FindItemCount[={name}]}}") > 0)
                {
                    epicWeaponName = name;
                }
            }

            foreach (string name in _anguishBPList)
            {
                if (MQ.Query<Int32>($"${{FindItemCount[={name}]}}") > 0)
                {
                    anguishBPName = name;
                }
            }

            if (!String.IsNullOrWhiteSpace(epicWeaponName))
            {
                if(!E3.CharacterSettings.BurnsDictionary.TryGetValue("epic burn", out List<Spell> list)) {
                    list = new List<Spell>();
                    E3.CharacterSettings.BurnsDictionary["epic burn"] = list;
                }
                list.Add(new Data.Spell(epicWeaponName));
            }

            if (!String.IsNullOrWhiteSpace(anguishBPName))
            {
                if(!E3.CharacterSettings.BurnsDictionary.TryGetValue("epic burn", out List<Spell> list)) {
                    list = new List<Spell>();
                    E3.CharacterSettings.BurnsDictionary["epic burn"] = list;
                }
                list.Add(new Data.Spell(anguishBPName));
            }

        }
        private static void RegisterSwarmpets()
        {
            var swarmPets = new List<Spell>();
            foreach (string pet in _swarmPetList)
            {
                Data.Spell tSpell;
                if (MQ.Query<bool>($"${{Me.AltAbility[{pet}]}}"))
                {
                    tSpell = new Spell(pet);
                    swarmPets.Add(tSpell);
                    continue;
                }
                if (MQ.Query<Int32>($"${{FindItemCount[={pet}]}}") > 0)
                {
                    tSpell = new Spell(pet);
                    swarmPets.Add(tSpell);
                }
            }
            if (swarmPets.Count > 0) {
                if(!E3.CharacterSettings.BurnsDictionary.TryGetValue("swarmpets", out List<Spell> list)) {
                    list = new List<Spell>();
                    E3.CharacterSettings.BurnsDictionary["swarmpets"] = list;
                }
                list.AddRange(swarmPets);
            }
        }
        private static List<string> _swarmPetList = new List<string>() {"Servant of Ro", "Host of the Elements",
            "Swarm of Decay","Rise of Bones","Graverobber's Icon","Soulwhisper","Deathwhisper",
            "Wake the Dead","Spirit Call", "Shattered Gnoll Slayer", "Call of Xuzl","Song of Stone",
            "Tarnished Skeleton Key","Celestial Hammer","Graverobber's Icon","Battered Smuggler's Barrel",
            "Phantasmal Opponent","Spirits of Nature", "Nature's Guardian"
        };

        private static List<string> _anguishBPList = new List<string>() {
            "Bladewhisper Chain Vest of Journeys",
            "Farseeker's Plate Chestguard of Harmony",
            "Wrathbringer's Chain Chestguard of the Vindicator",
            "Savagesoul Jerkin of the Wilds",
            "Glyphwielder's Tunic of the Summoner",
            "Whispering Tunic of Shadows",
            "Ritualchanter's Tunic of the Ancestors",
            "Deadeye's Ascendant Vest of Journeys",
            "Farseeker's Ascendant Chestguard of Harmony",
            "Wrathbringer's Ascendant Chestguard of the Vindicator",
            "Savagesoul's Ascendant Jerkin of the Wilds",
            "Glyphwielder's Ascendant Tunic of the Summoner",
            "Whisperer's Ascendant Tunic of Shadows",
            "Ritualchanter's Ascendant Tunic of the Ancestors",
        };

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