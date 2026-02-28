using E3Core.Data;
using E3Core.Settings;
using E3Core.Utility;
using MonoCore;
using System;
using System.Collections.Generic;
using System.Windows.Forms;


namespace E3Core.Processors
{
	public static class ServerSpecific
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
			else if (E3.ServerName == "Lazarus")
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
				foreach (string item in items)
				{
					totalCount += Inventory.FindItemCompact(item);

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

			#region eq_might_gate
			EventProcessor.RegisterCommand("/e3gate", (x) =>
			{
				if (x.args.Count == 0)
				{
					E3.Bots.BroadcastCommandToGroup("/e3gate me", x);
					MQ.Delay(400);
					MQ.Cmd("/alt act 1217");
				}

				if (x.args.Count == 1)
				{
					string target = x.args[0];
					if (target.Equals("me"))
					{
						Casting.Interrupt();
						MQ.Delay(500);
						MQ.Cmd("/alt act 1217");
						MQ.Delay(500);
						if (E3.CurrentClass == Class.Bard)
						{
							MQ.Delay(5000);
						}
						else
						{
							MQ.Delay(10000, Casting.IsNotCasting);
						}
					}
					else
					{
						E3.Bots.BroadcastCommandToPerson(target, "/e3gate me");
					}
				}
			});
			#endregion

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
				// disable beeps until we finish
				E3.GeneralSettings.General_BeepNotifications = false;

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

					//[AoC] EQMight Specific

					if (!_spawns.TryByName(_petrequester, out var s1))
					{
						MQ.Cmd($"/t {_petrequester} You aren't in the same zone as me!");
						return;
					}



					if (_spawns.TryByName(_petrequester, out var requesterSpawn))
					{
						var theirPetId = requesterSpawn.PetID;
						var myPetId = MQ.Query<int>("${Me.Pet.ID}");

						if (myPetId == theirPetId)
						{

							return;
						}

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
							int my_level = int.Parse(MQ.Query<string>("${Me.Level}"));
							string thingToAask = String.Empty;

							if (MQ.Query<bool>("${Me.ItemReady[Mightforged Mask of Mowcha]}") && (my_level > 68))
							{
								thingToAask = "Mightforged Mask of Mowcha";
							}
							else if (MQ.Query<bool>("${Me.ItemReady[Ancient Mask of Mowcha]}") && (my_level > 68))
							{
								thingToAask = "Ancient Mask of Mowcha";
							}
							else if (MQ.Query<bool>("${Me.ItemReady[Legendary Mask of Mowcha]}") && (my_level > 68))
							{
								thingToAask = "Legendary Mask of Mowcha";
							}
							else if (MQ.Query<bool>("${Me.ItemReady[Miranda's Mask]}"))
							{
								thingToAask = "Miranda's Mask";
							}
							else
							{ thingToAask = "mask of mardu"; }


							if (MQ.Query<bool>($"${{FindItem[{thingToAask}]}}"))
							{
								string MessagetoAask = string.Concat("/useitem ", thingToAask);
								MQ.Cmd($"{MessagetoAask}");
								MQ.Delay(500);
								//Add a 3 second delay if the clicky is mask of mowcha
								if ((thingToAask == "Mightforged Mask of Mowcha")
								|| (thingToAask == "Legendary Mask of Mowcha")
								|| (thingToAask == "Ancient Mask of Mowcha"))
								{
									MQ.Delay(1000);
									MQ.Delay(1000);
									MQ.Delay(1000);
								}
								Casting.TrueTarget(theirPetId);
								MQ.Delay(200);
								e3util.GiveItemOnCursorToTarget(false, false);
								MQ.Delay(1000);

							}

							else if ((my_level > 69) && (E3.CurrentClass == Class.Magician))
							{
								MQ.Cmd("/cast \"Summon Muzzle of Mowcha\"");

								MQ.Delay(1000);
								while (Casting.IsCasting())
								{
									MQ.Delay(50);
								}
								Casting.TrueTarget(theirPetId);
								MQ.Delay(200);
								e3util.GiveItemOnCursorToTarget(false, false);
								MQ.Delay(1000);
							}


							//Give them weapons
							string thingToAask2 = "Artifact of Toxic Edge";
							//  if (MQ.Query<bool>($"${{FindItem[{thingToAask2}]}}"))
							if (MQ.Query<bool>("${Me.ItemReady[Artifact of Toxic Edge]}"))
							{
								thingToAask2 = "Artifact of Toxic Edge";
							}
							else if (MQ.Query<bool>("${Me.ItemReady[Legendary Toxic Edge Earring]}"))
							{
								thingToAask2 = "Legendary Toxic Edge Earring";
							}
							else if (MQ.Query<bool>("${Me.ItemReady[Toxic Edge Earring]}"))
							{

								thingToAask2 = "Toxic Edge Earring";
							}
							else if (MQ.Query<bool>("${Me.ItemReady[Artifact of Baat]}"))
							{
								thingToAask2 = "Artifact of Baat";
							}

							else if (MQ.Query<bool>("${Me.ItemReady[Legendary Gloves of Strongboom]}"))
							{
								thingToAask2 = "Legendary Gloves of Strongboom";
							}

							else if (MQ.Query<bool>("${Me.ItemReady[Gloves of Strongboom]}"))
							{
								thingToAask2 = "Gloves of Strongboom";
							}
							else
							{ thingToAask2 = "Gloves of Ixiblat"; }




							string MessagetoAask2 = string.Concat("/useitem ", thingToAask2);


							if (MQ.Query<bool>($"${{FindItem[{thingToAask2}]}}"))
							{
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

							if (MQ.Query<bool>("${Me.ItemReady[Legendary Goblin Mask of Stability]}"))
							{
								thingToAask3 = "Legendary Goblin Mask of Stability";
							}
							else
							{ thingToAask3 = "Goblin Mask of Stability"; }



							string MessagetoAask3 = string.Concat("/useitem ", thingToAask3);

							if (MQ.Query<bool>($"${{FindItem[{thingToAask3}]}}"))
							{
								MQ.Cmd($"{MessagetoAask3}");
								MQ.Delay(500);
								e3util.GiveItemOnCursorToTarget(false, false);
								MQ.Delay(1000);
							}

							//Give them Mage Armor items
							string thingToAask4 = "Ancestral Girdle of the High Summoner";

							if (MQ.Query<bool>("${Me.ItemReady[Ancestral Girdle of the High Summoner]}"))
							{
								thingToAask4 = "Ancestral Girdle of the High Summoner";
							}
							else if (MQ.Query<bool>("${Me.ItemReady[Ancient Girdle of the High Summoner]}"))
							{
								thingToAask4 = "Ancient Girdle of the High Summoner";
							}

							else if (MQ.Query<bool>("${Me.ItemReady[Girdle of the High Summoner]}"))
							{
								thingToAask4 = "Girdle of the High Summoner";
							}



							string MessagetoAask4 = string.Concat("/useitem ", thingToAask4);

							if (MQ.Query<bool>($"${{FindItem[{thingToAask4}]}}"))
							{
								MQ.Cmd($"{MessagetoAask4}");
								MQ.Delay(500);
								while (Casting.IsCasting())
								{
									MQ.Delay(50);
								}
								e3util.GiveItemOnCursorToTarget(false, false);
								MQ.Delay(1000);
							}

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

					//re-enable beeps at the end
					E3.GeneralSettings.General_BeepNotifications = true;

					//delete the mage armor bag if it is still on cursor
					MQ.Cmd($"/if (${{Select[${{Cursor.ID}},17310]}}>0) /destroy");

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

						int my_level = int.Parse(MQ.Query<string>("${Me.Level}"));
						string thingToAask = "Mightforged Mask of Mowcha";
						int mask_found = 0;

						if (MQ.Query<bool>("${Me.ItemReady[Mightforged Mask of Mowcha]}") && (my_level > 68))
						{
							mask_found = 1;

						}

						else if (MQ.Query<bool>("${Me.ItemReady[Ancient Mask of Mowcha]}") && (my_level > 68))
						{
							mask_found = 1;
							thingToAask = "Ancient Mask of Mowcha";
						}

						else if (MQ.Query<bool>("${Me.ItemReady[Legendary Mask of Mowcha]}") && (my_level > 68))
						{
							mask_found = 1;
							thingToAask = "Legendary Mask of Mowcha";
						}

						else if (MQ.Query<bool>("${Me.ItemReady[Miranda's Mask]}"))
						{
							mask_found = 1;
							thingToAask = "Miranda's Mask";
						}
						else
						{ thingToAask = "mask of mardu"; }


						string _MuzzleSpell = "Summon Muzzle of Mowcha";

						if (MQ.Query<bool>($"${{FindItem[{thingToAask}]}}"))
						{
							string MessagetoAask = string.Concat("/useitem ", thingToAask);
							MQ.Cmd($"{MessagetoAask}");
							//Add a 3 second delay if the clicky is mask of mowcha
							if ((thingToAask == "Mightforged Mask of Mowcha")
							|| (thingToAask == "Legendary Mask of Mowcha")
							|| (thingToAask == "Ancient Mask of Mowcha"))
							{
								MQ.Delay(1000);
								while (Casting.IsCasting())
								{
									MQ.Delay(50);
								}
							}
							MQ.Delay(1000);
							e3util.GiveItemOnCursorToTarget(false, false);
							MQ.Delay(1000);

						}

						else if ((my_level > 69) && (E3.CurrentClass == Class.Magician))
						{
							MQ.Cmd("/cast \"Summon Muzzle of Mowcha\"");

							MQ.Delay(1000);

							while (Casting.IsCasting())
							{
								MQ.Delay(50);
							}
							Casting.TrueTarget(myPetId);
							MQ.Delay(1000);
							e3util.GiveItemOnCursorToTarget(false, false);
							MQ.Delay(1000);
						}


						//Give them weapons
						int weapon_found = 0;
						string thingToAask2 = "Artifact of Toxic Edge";
						//  if (MQ.Query<bool>($"${{FindItem[{thingToAask2}]}}"))
						if (MQ.Query<bool>("${Me.ItemReady[Artifact of Toxic Edge]}"))
						{
							weapon_found = 1;
							thingToAask2 = "Artifact of Toxic Edge";
						}


						else if ((weapon_found == 0) && MQ.Query<bool>("${Me.ItemReady[Legendary Toxic Edge Earring]}"))
						{
							weapon_found = 1;
							thingToAask2 = "Legendary Toxic Edge Earring";
						}

						else if ((weapon_found == 0) && MQ.Query<bool>("${Me.ItemReady[Toxic Edge Earring]}"))
						{
							weapon_found = 1;
							thingToAask2 = "Toxic Edge Earring";
						}


						else if ((weapon_found == 0) && MQ.Query<bool>("${Me.ItemReady[Artifact of Baat]}"))
						{
							weapon_found = 1;
							thingToAask2 = "Artifact of Baat";
						}

						else if ((weapon_found == 0) && MQ.Query<bool>("${Me.ItemReady[Legendary Gloves of Strongboom]}"))
						{
							weapon_found = 1;
							thingToAask2 = "Legendary Gloves of Strongboom";
						}

						else if ((weapon_found == 0) && MQ.Query<bool>("${Me.ItemReady[Gloves of Strongboom]}"))
						{
							weapon_found = 1;
							thingToAask2 = "Gloves of Strongboom";
						}

						else
						{ thingToAask2 = "Gloves of Ixiblat"; }

						string MessagetoAask2 = string.Concat("/useitem ", thingToAask2);
						if (MQ.Query<bool>($"${{FindItem[{thingToAask2}]}}"))
						{
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
						if (MQ.Query<bool>("${Me.ItemReady[Legendary Goblin Mask of Stability]}"))
						{
							thingToAask3 = "Legendary Goblin Mask of Stability";
						}
						else
						{ thingToAask3 = "Goblin Mask of Stability"; }



						string MessagetoAask3 = string.Concat("/useitem ", thingToAask3);

						if (MQ.Query<bool>($"${{FindItem[{thingToAask3}]}}"))
						{
							MQ.Cmd($"{MessagetoAask3}");
							MQ.Delay(500);
							e3util.GiveItemOnCursorToTarget(false, false);
							MQ.Delay(1000);
						}

						//Give them Mage Armor items
						string thingToAask4 = "Ancestral Girdle of the High Summoner";

						if (MQ.Query<bool>("${Me.ItemReady[Ancestral Girdle of the High Summoner]}"))
						{
							thingToAask4 = "Ancestral Girdle of the High Summoner";
						}
						else if (MQ.Query<bool>("${Me.ItemReady[Ancient Girdle of the High Summoner]}"))
						{
							thingToAask4 = "Ancient Girdle of the High Summoner";
						}
						else if (MQ.Query<bool>("${Me.ItemReady[Girdle of the High Summoner]}"))
						{
							thingToAask4 = "Girdle of the High Summoner";
						}



						string MessagetoAask4 = string.Concat("/useitem ", thingToAask4);

						if (MQ.Query<bool>($"${{FindItem[{thingToAask4}]}}"))
						{
							MQ.Cmd($"{MessagetoAask4}");
							MQ.Delay(500);
							while (Casting.IsCasting())
							{
								MQ.Delay(50);
							}
							e3util.GiveItemOnCursorToTarget(false, false);
							MQ.Delay(1000);
						}

						//delete the mage armor bag if it is still on cursor
						MQ.Cmd($"/if (${{Select[${{Cursor.ID}},17310]}}>0) /destroy");


					}
				}
			});
		}
	}
}
