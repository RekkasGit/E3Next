using E3Core.Data;
using E3Core.Settings;
using E3Core.Utility;
using MonoCore;
using System;
using System.Collections.Generic;
using System.Windows.Forms;


namespace E3Core.Processors
{
	public static class SeverSpecific
	{

		private static IMQ MQ = E3.MQ;
		private static ISpawns _spawns = E3.Spawns;
		
		//Note you can override commands and regex's using
		//OverrideRegisteredEvent
		//OverrideCommandMethod
		//as this is initialized last, all commands/events should be reisgered before this init is called. 

		public static void SeverSpecific_Init()
		{
			MQ.Write("Checking for server specific code..");
			if (E3.ServerName == "EQ_Might")
			{
				MQ.Write("Loading EQ_Might specific code!");
				Init_EQMight();
			}
			else if(E3.ServerName=="Lazarus")
			{
				MQ.Write("Loading Project Lazarus specific code!");

				Init_Lazarus();
			}
			else
			{
				MQ.Write("None found for this server.");
			}
		}
		public static void Init_Lazarus()
		{

			EventProcessor.RegisterCommand("/e3laz-count-nv", (x) =>
			{
				List<string> items = new List<string>() {"Sword of the Celestial Dawn","Glass Key to the Nowhere Door","Blacksalt Compass","Coldfire Lantern","Fragment of the Maestra","the bone violin",
				"Phantom's Bride Doll","Eternal Jack-o-Lantern","Shroud of the Forgotten King","Withered Rose","Map of Midnight", "The Ash Crown","The Witch's Bell","Fragment of Vzith","Crown of Radiant Dominion","Hat of the Forsaken Jester",
				"Obsidian Chalice","the crimson dice","death quill","mirror of the last gaze","Scythe of Silence","Quill of Tomorrow","Ancient Alpha Skull","charred obulus relic"};

				Int32 totalCount = 0;
				foreach(string item in items)
				{
					totalCount+=Inventory.FindItemCompact(item);

				}
				E3.Bots.Broadcast($"Total NV Count: {totalCount}");


				Int32 dream = Inventory.FindItemCompact("Scarlet Dream Essence");
				E3.Bots.Broadcast($"Total Scarlet Dream Essence Count: {dream}");
				Int32 pureDream = Inventory.FindItemCompact("Pure Dream Essence");
				E3.Bots.Broadcast($"Total Pure Dream Essence Count: {pureDream}");
				Int32 pristineDream = Inventory.FindItemCompact("Pristine Dream Essence");
				E3.Bots.Broadcast($"Total Pristine Dream Essence Count: {pristineDream}");
				Int32 perfectDream = Inventory.FindItemCompact("Perfect Dream Essenc");
				E3.Bots.Broadcast($"Total Pristine Dream Essence Count: {perfectDream}");

			});



		}
		public static void Init_EQMight()
		{


			#region eq_might_temple
			EventProcessor.RegisterCommand("/temple", (x) =>
			{
				if (x.args.Count == 0)
				{
					E3.Bots.BroadcastCommandToGroup("/temple me", x);
					MQ.Delay(400);
					MQ.Cmd("/alt act 12620");
				}

				if (x.args.Count == 1)
				{
					string target = x.args[0];
					if (target.Equals("me"))
					{
						Casting.Interrupt();
						MQ.Delay(500);
						MQ.Cmd("/alt act 12620");
						MQ.Delay(500);
						if (E3.CurrentClass == Class.Bard)
						{
							MQ.Delay(17000);
						}
						else
						{
							MQ.Delay(20000, Casting.IsNotCasting);
						}
					}
					else
					{
						E3.Bots.BroadcastCommandToPerson(target, "/temple me");
					}
				}
			});
			#endregion
			EventProcessor.RegisterEvent("EQ_Might_Groupme", "(.+) tells you, 'groupme'", (x) =>
			{
				if (x.match.Groups.Count > 1)
				{
					MQ.Cmd($"/invite {x.match.Groups[1].Value}");
				}
			});
			var equipPetEvents = new List<string> { "(.+) tells you, 'equip_pet'", "(.+) tells you, 'equip_pet (.+)'", "(.+) tells the group, 'equip_pet (.+)'", };
			EventProcessor.RegisterEvent("EQ_Might_EquipPet", equipPetEvents, (x) =>
			{
				string _petrequester;

				if (x.match.Groups.Count <= 1)
				{
					return;
				}

				var equip_pet_classes = new HashSet<Class> { Class.Magician, Class.Enchanter, Class.Beastlord, Class.Necromancer };

				if (!equip_pet_classes.Contains(E3.CurrentClass))
				{
					_petrequester = x.match.Groups[1].ToString();
					MQ.Cmd($"/t {_petrequester} My Class can't provide pet equipment!");
					return;
				}
				if (equip_pet_classes.Contains(E3.CurrentClass))
				{

					if (Basics.InCombat())
					{
						_petrequester = x.match.Groups[1].ToString();
						MQ.Cmd($"/t {_petrequester} Can't give your pet gear - I'm casting over here!!");
						return;
					}

					_petrequester = x.match.Groups[1].ToString();

					Spawn s;
					//[AoC] EQMight Specific

					if (!_spawns.TryByName(_petrequester, out var s1))
					{
						MQ.Cmd($"/t {_petrequester} You aren't in the same zone as me!");
						return;
					}



					if (_spawns.TryByName(_petrequester, out var requesterSpawn))
					{
						var theirPetId = requesterSpawn.PetID;
						if (theirPetId < 0)
						{
							MQ.Cmd($"/t {_petrequester} You don't have a pet to equip!");
							return;
						}

						if (_spawns.TryByID(theirPetId, out var petSpawn))
						{
							if (petSpawn.Distance > 60)
							{
								MQ.Cmd($"/t {_petrequester} Your pet is too far away!");
								return;
							}
							if (petSpawn.Level == 1)
							{

								MQ.Cmd($"/t {_petrequester} Your pet is just a familiar!");
								return;
							}
						}
						else
						{
							MQ.Cmd($"/t {_petrequester} Cannot find your pet in zone!");
							return;

						}



						if (Casting.TrueTarget(theirPetId))
						{

							// so we can move back
							var currentX = MQ.Query<double>("${Me.X}");
							var currentY = MQ.Query<double>("${Me.Y}");
							var currentZ = MQ.Query<double>("${Me.Z}");
							//give them the mask
							string thingToAask = "mask of mardu";
							string MessagetoAask = string.Concat("/useitem ", thingToAask);
							MQ.Cmd($"{MessagetoAask}");
							MQ.Delay(500);
							e3util.GiveItemOnCursorToTarget(false, false);
							MQ.Delay(1000);

							if (E3.CurrentClass != Class.Enchanter)
							{
								//Give them weapons
								string thingToAask2 = "Legendary Toxic Edge Earring";
								string MessagetoAask2 = string.Concat("/useitem ", thingToAask2);
								MQ.Cmd($"{MessagetoAask2}");
								MQ.Delay(500);
								e3util.GiveItemOnCursorToTarget(false, false);
								MQ.Delay(1000);

								//Give them a second weapon
								MQ.Cmd($"{MessagetoAask2}");
								MQ.Delay(500);
								e3util.GiveItemOnCursorToTarget(false, false);
								MQ.Delay(1000);
							}
							if (E3.CurrentClass == Class.Enchanter)
							{
								//Give them weapons
								string thingToAask2 = "Artifact of Toxic Edge";
								string MessagetoAask2 = string.Concat("/useitem ", thingToAask2);
								MQ.Cmd($"{MessagetoAask2}");
								MQ.Delay(500);
								e3util.GiveItemOnCursorToTarget(false, false);
								MQ.Delay(1000);

								//Give them a second weapon
								MQ.Cmd($"{MessagetoAask2}");
								MQ.Delay(500);
								e3util.GiveItemOnCursorToTarget(false, false);
								MQ.Delay(1000);
							}


							//give them the belt
							string thingToAask3 = "Legendary Goblin Mask of Stability";
							string MessagetoAask3 = string.Concat("/useitem ", thingToAask3);
							MQ.Cmd($"{MessagetoAask3}");
							MQ.Delay(500);
							e3util.GiveItemOnCursorToTarget(false, false);
							MQ.Delay(1000);
							MQ.Cmd($"/t {_petrequester} Pet equipped! You're good to go!");

							//At the end, move back to starting loc
							e3util.TryMoveToLoc(currentX, currentY, currentZ);
						}
					}

					if (E3.CharacterSettings.IgnorePetWeaponRequests)
					{
						MQ.Cmd($"/t {_petrequester} Sorry, I am not currently accepting requests for pet weapons even from Guild Mates");
						return;
					}
				}
			});


			EventProcessor.RegisterCommand("/equip_self", (x) =>
			{
				if (Basics.InCombat())
				{

					MQ.Cmd($"/g Can't give my pet gear - I'm in combat over here!!");
					return;
				}

				if (!Basics.InCombat())
				{
					var myPetId = MQ.Query<int>("${Me.Pet.ID}");
					if (Casting.TrueTarget(myPetId))
					{
						//give them the mask
						string thingToAask = "mask of mardu";
						string MessagetoAask = string.Concat("/useitem ", thingToAask);
						MQ.Cmd($"{MessagetoAask}");
						MQ.Delay(500);
						e3util.GiveItemOnCursorToTarget(false, false);
						MQ.Delay(1000);

						if (E3.CurrentClass != Class.Enchanter)
						{
							//Give them weapons
							string thingToAask2 = "Legendary Toxic Edge Earring";
							string MessagetoAask2 = string.Concat("/useitem ", thingToAask2);
							MQ.Cmd($"{MessagetoAask2}");
							MQ.Delay(500);
							e3util.GiveItemOnCursorToTarget(false, false);
							MQ.Delay(1000);

							//Give them a second weapon
							MQ.Cmd($"{MessagetoAask2}");
							MQ.Delay(500);
							e3util.GiveItemOnCursorToTarget(false, false);
							MQ.Delay(1000);

						}
						if (E3.CurrentClass == Class.Enchanter)
						{
							//Give them weapons
							string thingToAask2 = "Artifact of Toxic Edge";
							string MessagetoAask2 = string.Concat("/useitem ", thingToAask2);
							MQ.Cmd($"{MessagetoAask2}");
							MQ.Delay(500);
							e3util.GiveItemOnCursorToTarget(false, false);
							MQ.Delay(1000);

							//Give them a second weapon
							MQ.Cmd($"{MessagetoAask2}");
							MQ.Delay(500);
							e3util.GiveItemOnCursorToTarget(false, false);
							MQ.Delay(1000);

						}
						//give them the belt
						string thingToAask3 = "Legendary Goblin Mask of Stability";
						string MessagetoAask3 = string.Concat("/useitem ", thingToAask3);
						MQ.Cmd($"{MessagetoAask3}");
						MQ.Delay(500);
						Casting.TrueTarget(myPetId);
						e3util.GiveItemOnCursorToTarget(false, false);
						MQ.Delay(1000);


					}
				}
			});

		}
	}
}
