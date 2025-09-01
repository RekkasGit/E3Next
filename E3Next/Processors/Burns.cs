using E3Core.Data;
using E3Core.Utility;
using MonoCore;
using System;
using System.Collections.Generic;

namespace E3Core.Processors
{
	public class Burn
	{
		public string Name = String.Empty;
		public List<Data.Spell> ItemsToBurn = new List<Spell>();
		public Int64 StartTimeStamp = 0;
		public Int32 Timeout = 0;
		public bool Active = false;
	}
    public static class Burns
    {
        public static Logging _log = E3.Log;
        private static IMQ MQ = E3.MQ;
        private static ISpawns _spawns = E3.Spawns;

	    
        [SubSystemInit]
        public static void Burns_Init()
        {
			RegisterEpicAndAnguishBP();
			RegisterSwarmpets();
			RegisterEvents();
        }
        public static void Reset()
        {
			RegisterEpicAndAnguishBP();
			RegisterSwarmpets();
			foreach (var pair in E3.CharacterSettings.BurnCollection)
			{
				Burn burn = pair.Value;
				burn.Active = false;
				burn.StartTimeStamp = 0;
				burn.Timeout = 0;
			}
		}
        private static void RegisterEvents()
        {
			EventProcessor.RegisterCommand("/e3burns", (x) => {

				if (x.args.Count>0)
				{
					string burnToUseKey = x.args[0].Trim();
					if (E3.CharacterSettings.BurnCollection.TryGetValue(burnToUseKey, out var burnToUse))
					{
						ProcessBurnRequest(x, burnToUse);
					}
					else
					{
						//we don't have this burn locally, so just pass in an empty one
						Burn tburn = new Burn();
						tburn.Name = burnToUseKey;
						ProcessBurnRequest(x, tburn);
					}
				}
			});

          
            EventProcessor.RegisterCommand("/epicburns", (x) =>
            {
				if (E3.CharacterSettings.BurnCollection.TryGetValue("Epic", out var burnToUse))
				{
					ProcessBurnRequest(x, burnToUse);
				}
            });
            EventProcessor.RegisterCommand("/quickburns", (x) =>
            {
				if (E3.CharacterSettings.BurnCollection.TryGetValue("Quick Burn", out var burnToUse))
				{
					ProcessBurnRequest(x, burnToUse);
				}

			});
            EventProcessor.RegisterCommand("/fullburns", (x) =>
            {
				if (E3.CharacterSettings.BurnCollection.TryGetValue("Full Burn", out var burnToUse))
				{
					ProcessBurnRequest(x, burnToUse);
				}

			});
            EventProcessor.RegisterCommand("/longburns", (x) =>
            {
				if (E3.CharacterSettings.BurnCollection.TryGetValue("Long Burn", out var burnToUse))
				{
					ProcessBurnRequest(x, burnToUse);
				}
			});
			EventProcessor.RegisterCommand("/swarmpets", (x) =>
			{
				if (E3.CharacterSettings.BurnCollection.TryGetValue("Swarm", out var burnToUse))
				{
					ProcessBurnRequest(x, burnToUse);
				}
			});

		}
		private static void ProcessBurnRequest(EventProcessor.CommandMatch x, Burn burn)
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

			if (x.args.Count > 1)
			{
				if (Int32.TryParse(x.args[1], out mobid))
				{
					if (!e3util.FilterMe(x))
					{

						burn.Active = true;
						if (timeout > 0)
						{
							burn.Timeout = timeout;
							burn.StartTimeStamp = Core.StopWatch.ElapsedMilliseconds;
						}
						else
						{
							burn.Timeout = 0;
							burn.StartTimeStamp = 0;
						}
					}
				}
				else
				{
					E3.Bots.Broadcast($"\arNeed a valid target to {burn.Name}.");
				}
			}
			else
			{
				Int32 targetID = MQ.Query<Int32>("${Target.ID}");
				if (targetID > 0)
				{

					if (timeout > 0)
					{
						E3.Bots.BroadcastCommandToGroup($"/e3burns \"{burn.Name}\" {targetID} timeout={timeout}", x);
					}
					else
					{
						E3.Bots.BroadcastCommandToGroup($"/e3burns \"{burn.Name}\" {targetID}", x);
					}

					if (!e3util.FilterMe(x))
					{
						burn.Active = true;
						if (timeout > 0)
						{
							burn.Timeout = timeout;
							burn.StartTimeStamp = Core.StopWatch.ElapsedMilliseconds;
						}
						else
						{
							burn.Timeout = 0;
							burn.StartTimeStamp = 0;
						}
					}
				}
				else
				{
					MQ.Write($"\arNeed a target to {burn.Name}");
				}
			}
		}
		public static void UseBurns()
        {
            //  if (!e3util.ShouldCheck(ref _nextBurnCheck, _nextBurnCheckInterval)) return;
            //lets check if there are any events in the queue that we need to check on. 
            EventProcessor.ProcessEventsInQueues("/quickburns");
			EventProcessor.ProcessEventsInQueues("/epicburns");
			EventProcessor.ProcessEventsInQueues("/fullburns");
			EventProcessor.ProcessEventsInQueues("/longburns");
			EventProcessor.ProcessEventsInQueues("/e3burns");
			CheckTimeouts();


			foreach (var pair in E3.CharacterSettings.BurnCollection)
			{
				Burn burn = pair.Value;
				if(burn.Active)
				{
					UseBurn(burn);
				}
			}
        }

        public static void CheckTimeouts()
        {
			foreach (var pair in E3.CharacterSettings.BurnCollection)
			{
				Burn burn = pair.Value;
				CheckTimeouts_SubCheck(burn);
			}
			
        }
		private static void CheckTimeouts_SubCheck(Burn burn)
		{
			if (burn.Active && burn.StartTimeStamp > 0)
			{   //turn off after 60 seconds
				if ((burn.StartTimeStamp + (burn.Timeout * 1000)) < Core.StopWatch.ElapsedMilliseconds)
				{
					E3.Bots.Broadcast($"Turning off {burn.Name} due to timeout of : {burn.Timeout}");
					burn.Active = false;
					burn.StartTimeStamp = 0;
					burn.Timeout = 0;
				}
			}
		}
	    private static void UseBurn(Burn burnToUse)
        {
			if (!Assist.IsAssisting)
			{
				E3.Bots.Broadcast($"Not assisting, disabling Burn:{burnToUse.Name}");
				burnToUse.Active = false;
				return;
			}

			Int32 initialTarget = MQ.Query<Int32>("${Target.ID}");
			bool isManualControl = e3util.IsManualControl();


			if (burnToUse.Active)
            {	

				Int32 previousTarget = initialTarget;
				Int32 petId = MQ.Query<Int32>("${Me.Pet.ID}");

				foreach (var burn in burnToUse.ItemsToBurn)
                {
					BuffCheck.Check_BlockedBuffs();
					if(petId>0)
					{
						Pets.CheckPetBuffs();

					}
					if (MQ.Query<Int32>("${Me.CurrentHPs}") < 1) return; //can't burn if dead
                    if (burn.TargetType == "Pet" && petId < 1) continue;

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
					
						if (!isManualControl && Assist.AssistTargetID != initialTarget && !burn.SpellType.Contains("Beneficial"))
						{
							//hotfix to possibly work around the issue of swarm type pets on healers who might not have the assist target , targeted.
							//if it works, need to restructure this a bit.
							Casting.TrueTarget(Assist.AssistTargetID);
							previousTarget = Assist.AssistTargetID;
							
						}
						else if (isManualControl && Assist.AssistTargetID != initialTarget)
						{
							//don't burn unless its our assist target
							return;

						}
						if (Casting.InRange(previousTarget, burn))
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
							var chatOutput = $"{burnToUse.Name}: {burn.CastName}";
							//so you don't target other groups or your pet for burns if your target happens to be on them.
							if (!String.IsNullOrWhiteSpace(burn.CastTarget) && _spawns.TryByName(burn.CastTarget, out var spelltarget))
							{

								if (Casting.Cast(spelltarget.ID, burn) == CastReturn.CAST_INTERRUPTFORHEAL)
								{
									return;
								}
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
								if (Casting.Cast(E3.CurrentId, burn) == CastReturn.CAST_INTERRUPTFORHEAL)
								{
									return;
								}

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

								if (Casting.Cast(0, burn) == CastReturn.CAST_INTERRUPTFORHEAL)
								{
									return;
								}
								E3.Bots.Broadcast(chatOutput);

							}
						}	
                    }
					
				}

            }
        }
        private static void RegisterEpicAndAnguishBP()
        {
			if (!E3.CharacterSettings.BurnCollection.TryGetValue("Epic", out var burn))
			{
				string epicWeaponName = String.Empty;
				foreach (string name in _epicList)
				{
					if (MQ.Query<Int32>($"${{FindItemCount[={name}]}}") > 0)
					{
						epicWeaponName = name;
					}
				}
				string anguishBPName = String.Empty;
				foreach (string name in _anguishBPList)
				{
					if (MQ.Query<Int32>($"${{FindItemCount[={name}]}}") > 0)
					{
						anguishBPName = name;
					}
				}
				List<Spell> epicCollection = new List<Spell>();
				if (!String.IsNullOrWhiteSpace(epicWeaponName))
				{
					epicCollection.Add(new Data.Spell(epicWeaponName));
				}
				if (!String.IsNullOrWhiteSpace(anguishBPName))
				{
					epicCollection.Add(new Data.Spell(anguishBPName));
				}
				
				Burn newBurn = new Burn();
				newBurn.Name = "Epic";
				newBurn.ItemsToBurn = epicCollection;
				E3.CharacterSettings.BurnCollection.Add("Epic", newBurn);
				
			}
				
        }
        private static void RegisterSwarmpets()
		{ 
			
			if(!E3.CharacterSettings.BurnCollection.TryGetValue("Swarm",out var burn))
			{
				List<Data.Spell> swarmPets = new List<Spell>();
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
				Burn newBurn = new Burn();
				newBurn.Name = "Swarm";
				newBurn.ItemsToBurn = swarmPets;
				E3.CharacterSettings.BurnCollection.Add("Swarm", newBurn);
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
