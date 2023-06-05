using E3Core.Data;
using E3Core.Processors;
using E3Core.Settings;
using E3Core.Utility;
using MonoCore;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace E3Core.Classes
{
    /// <summary>
    /// Properties and methods specific to the magician class
    /// </summary>
    public static class Magician
    {
        private static IMQ MQ = E3.MQ;
        private static ISpawns _spawns = E3.Spawns;
        private static string _weaponSpell = "Grant Spectral Armaments";
        private static string _weaponItem = "Folded Pack of Spectral Armaments";
        private static string _armorSpell = "Grant Spectral Plate";
        private static string _armorItem = "Folded Pack of Spectral Plate";
        private static string _focusSpell = "Grant Enibik's Heirlooms";
        private static string _focusItem = "Folded Pack of Enibik's Heirlooms";
        private static string _weaponBag = "Pouch of Quellious";
        private static string _armorOrHeirloomBag = "Phantom Satchel";
        private static Dictionary<string, string> _weaponMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) {
            {"Fire", "Summoned: Fist of Flame"},
            {"Water", "Summoned: Orb of Chilling Water" },
            {"Shield", "Summoned: Buckler of Draining Defense" },
            {"Taunt", "Summoned: Short Sword of Warding" },
            {"Slow", "Summoned: Mace of Temporal Distortion" },
            {"Malo", "Summoned: Spear of Maliciousness" },
            {"Dispel", "Summoned: Wand of Dismissal" },
            {"Snare", "Summoned: Tendon Carver" },
        };

        private static Dictionary<string, string> _summonedItemMap = new Dictionary<string, string>
        {
            {_weaponSpell, _weaponItem },
            {_armorSpell, _armorItem },
            {_focusSpell, _focusItem },
        };

        private static string _requester;

        private static long _nextWeaponCheck = 0;
        private static long _nextWeaponCheckInterval = 10000;
        private static bool _isExternalRequest = false;

        private const int EnchanterPetPrimaryWeaponId = 10702;

        /// <summary>
        /// Accepts a pet equipment request.
        /// </summary>
        [SubSystemInit]
        public static void PetEquipmentRequest()
        {
            if (E3.CurrentClass != Class.Magician)
            {
                return;
            }

            var armPetEvents = new List<string> { "(.+) tells you, 'armpet'", "(.+) tells you, 'armpet (.+)'", "(.+) tells the group, 'armpet (.+)'", };
            EventProcessor.RegisterEvent("ArmPet", armPetEvents, (x) =>
            {
                if (x.match.Groups.Count <= 1)
                {
                    return;
                }

                _requester = x.match.Groups[1].ToString();
                if (E3.CurrentClass != Class.Magician)
                {
                    MQ.Cmd($"/t {_requester} Only magicians can give out pet weapons!");
                    return;
                }

                _isExternalRequest = !E3.Bots.BotsConnected().Contains(_requester);               
                var weaponSplit = x.match.Groups[2].ToString().Split('|');
                if (!_isExternalRequest && weaponSplit.Length != 2) 
                {
                    if (E3.CharacterSettings.PetWeapons.TryGetValue(_requester, out var weaponConfig))
                    {
                        weaponSplit = weaponConfig.Split('|');
                    }
                }

                if (weaponSplit.Count() != 2)
                {
                    MQ.Cmd($"/t {_requester} Invalid request. The request must be in the format of armpet Primary|Secondary");
                    return;
                }

                if (!_weaponMap.TryGetValue(weaponSplit[0], out _))
                {
                    MQ.Cmd($"/t {_requester} Invalid primary weapon selection. Valid values are {string.Join(", ", _weaponMap.Keys)}");
                    return;
                }

                if (!_weaponMap.TryGetValue(weaponSplit[1], out _))
                {
                    MQ.Cmd($"/t {_requester} Invalid secondary weapon selection. Valid values are {string.Join(", ", _weaponMap.Keys)}");
                    return;
                }

                if(_spawns.TryByName(_requester, out var requesterSpawn))
                {
                    var theirPetId = requesterSpawn.PetID;
                    if(theirPetId < 0)
                    {
                        MQ.Cmd($"/t {_requester} You don't have a pet to equip!");
                        return;
                    }

                    if (_spawns.Get().First(w => w.ID == theirPetId).Distance > 50)
                    {
                        MQ.Cmd($"/t {_requester} Your pet is too far away!");
                        return;
                    }

                    if (_spawns.Get().First(w => w.ID == theirPetId).Level == 1)
                    {
                        MQ.Cmd($"/t {_requester} Your pet is just a familiar!");
                        return;
                    }

                    ArmPet(theirPetId, $"{weaponSplit[0]}|{weaponSplit[1]}");
                }
            });

            armPetEvents = new List<string> { "(.+) tells you, 'armpets'", "(.+) tells the group, 'armpets'", };
            EventProcessor.RegisterEvent("ArmPets", armPetEvents, x =>
            {
                E3.Bots.Broadcast("I hear you I hear you one moment please....");
                _requester = x.match.Groups[1].ToString();
                if (!E3.Bots.BotsConnected().Contains(_requester))
                {
                    MQ.Cmd($"/t {_requester} the ArmPets command is only valid on your own bot network");
                    return;
                }

                ArmPets();
            });
        }


        /// <summary>
        /// Checks pets for items and re-equips if necessary.
        /// </summary>
        [ClassInvoke(Data.Class.Magician)]
        public static void AutoArmPets()
        {
            if (Basics.InCombat()) return;
            if (!E3.CharacterSettings.AutoPetWeapons) return;
            if (!e3util.ShouldCheck(ref _nextWeaponCheck, _nextWeaponCheckInterval)) return;

            ArmPets();
        }

        public static void ArmPets()
        {
            if (MQ.Query<int>("${Cursor.ID}") > 0)
            {
                if (!e3util.ClearCursor())
                {
                    E3.Bots.Broadcast("\arI was unable to clear my cursor so I cannot continue.");
                }
            }

            // my pet
            var primary = MQ.Query<int>("${Me.Pet.Primary}");
            var myPetId = MQ.Query<int>("${Me.Pet.ID}");
            if (myPetId > 0 && primary == 0)
            {
                E3.CharacterSettings.PetWeapons.TryGetValue(E3.CurrentName, out var weapons);
                if (e3util.IsShuttingDown() || E3.IsPaused()) return;
                ArmPet(myPetId, weapons);
                if (e3util.IsShuttingDown() || E3.IsPaused()) return;
            }

            // bot pets
            foreach (var kvp in E3.CharacterSettings.PetWeapons)
            {
                if (_spawns.TryByName(kvp.Key, out var ownerSpawn))
                {
                    if (e3util.IsShuttingDown() || E3.IsPaused()) return;

                    if (string.Equals(ownerSpawn.Name, E3.CurrentName)) continue;
                    var theirPetId = ownerSpawn.PetID;
                    if (theirPetId < 0)
                    {
                        continue;
                    }

                    var theirPetDistance = MQ.Query<double>($"${{Spawn[{ownerSpawn.Name}].Pet.Distance}}");
                    if (theirPetDistance > 50)
                    {
                        continue;
                    }
                    
                    var theirPetLevel = MQ.Query<int>($"${{Spawn[{ownerSpawn.Name}].Pet.Level}}");
                    if (theirPetLevel == 1)
                    {
                        continue;
                    }

                    var theirPetPrimary = MQ.Query<int>($"${{Spawn[{ownerSpawn.Name}].Pet.Primary}}");
                    if (theirPetPrimary == 0 || theirPetPrimary == EnchanterPetPrimaryWeaponId)
                    {
                        ArmPet(theirPetId, kvp.Value);
                    }
                }
            }
        }

        private static void ArmPet(int petId, string weapons)
        {
            // so we can move back
            var currentX = MQ.Query<double>("${Me.X}");
            var currentY = MQ.Query<double>("${Me.Y}");
            var currentZ = MQ.Query<double>("${Me.Z}");

            if (!GiveWeapons(petId, weapons ?? "Water|Fire"))
            {
                if (_isExternalRequest)
                {
                    MQ.Cmd($"/t {_requester} There was an issue with pet weapon summoning and we are unable to continue.");
                }
                else
                {
                    E3.Bots.Broadcast("\arThere was an issue with pet weapon summoning and we are unable to continue.");
                }

                // move back to my original location
                e3util.TryMoveToLoc(currentX, currentY, currentZ);
                _isExternalRequest = false;

                return;
            }
			Casting.TrueTarget(petId);
           
			var spell = new Spell(_armorSpell);
            Int32 castAttempts = 0;
            if(Casting.CheckReady(spell) && Casting.CheckMana(spell))
			{
				while(Casting.Cast(petId, spell) == CastReturn.CAST_FIZZLE)
				{
                    if (castAttempts > 7) break;
					MQ.Delay(1500);
                    castAttempts++;
				}
			}
			castAttempts = 0;
			spell = new Spell(_focusSpell);
			if (Casting.CheckReady(spell) && Casting.CheckMana(spell))
			{
				while (Casting.Cast(petId, spell) == CastReturn.CAST_FIZZLE)
				{
					if (castAttempts > 7) break;
					MQ.Delay(1500);
					castAttempts++;
				}
			}
			//if (!GiveOther(petId, _armorSpell)) return;
			//if (!GiveOther(petId, _focusSpell)) return;


			var pet = _spawns.Get().FirstOrDefault(f => f.ID == petId);
            if (pet != null)
            {
                if (_isExternalRequest)
                {
                    MQ.Cmd($"/t {_requester} Finished arming {pet.CleanName}");
                }
                else
                {
                    E3.Bots.Broadcast($"\agFinishing arming {pet.CleanName}");
                }
            }

            // move back to my original location
            e3util.TryMoveToLoc(currentX, currentY, currentZ);
            _isExternalRequest = false;
        }

        private static bool GiveWeapons(int petId, string weaponString)
        {
            var weapons = weaponString.Split('|');
            _weaponMap.TryGetValue(weapons[0], out var primary);
            _weaponMap.TryGetValue(weapons[1], out var secondary);

            try
            {
				if (!CheckForWeapons(primary, secondary))
				{
					return false;
				}

				if (Casting.TrueTarget(petId))
				{
					PickUpWeapon(primary);
					e3util.GiveItemOnCursorToTarget(false, false);
					if (!CheckForWeapons(primary, secondary))
					{
						return false;
					}
					Casting.TrueTarget(petId);
					PickUpWeapon(secondary);
					e3util.GiveItemOnCursorToTarget(false);
				}
				else
				{
					return false;
				}
			}
            finally
            {
                //clean up after outselves
				var foundWeaponBag = MQ.Query<bool>($"${{FindItem[={_weaponBag}]}}");
				if (foundWeaponBag)
				{
					MQ.Cmd($"/nomodkey /itemnotify \"{_weaponBag}\" leftmouseup");
					MQ.Delay(1000, "${Cursor.ID}");
					if (!e3util.ValidateCursor(MQ.Query<int>($"${{FindItem[={_weaponBag}].ID}}")))
					{
						E3.Bots.Broadcast($"\arUnexpected item on cursor when trying to destroy {_weaponBag}");
					}
                    else
                    {
						MQ.Cmd("/destroy");
					}
				}
			}
            
			
			return true;
        }

        private static bool CheckForWeapons(string primary, string secondary)
        {
            var foundPrimary = MQ.Query<bool>($"${{FindItem[={primary}]}}");
            var foundSecondary = MQ.Query<bool>($"${{FindItem[={secondary}]}}");

            if (!foundPrimary || !foundSecondary)
            {
                var foundWeaponBag = MQ.Query<bool>($"${{FindItem[={_weaponBag}]}}");
                if (foundWeaponBag)
                {
                    MQ.Cmd($"/nomodkey /itemnotify \"{_weaponBag}\" leftmouseup");
                    MQ.Delay(1000, "${Cursor.ID}");
                    if (!e3util.ValidateCursor(MQ.Query<int>($"${{FindItem[={_weaponBag}].ID}}")))
                    {
                        E3.Bots.Broadcast($"\arUnexpected item on cursor when trying to destroy {_weaponBag}");
                        return false;
                    }

                    MQ.Cmd("/destroy");
                }
                else
                {
                    if (!CheckInventory())
                    {
                        if (_isExternalRequest)
                        {
                            MQ.Cmd($"/t {_requester} I was unable to free up inventory space to fulfill your request.");
                        }
                        else
                        {
                            E3.Bots.Broadcast("\arUnable to free up inventory space to arm pets.");
                        }

                        return false;
                    }
                }

                var summonResult = SummonItem(_weaponSpell, true);
                if (!summonResult.success)
                {
                    E3.Bots.Broadcast($"\ar{summonResult.error}");
                    return false;
                }
            }

            return true;
        }

        private static void PickUpWeapon(string weaponName)
        {
            var itemSlot = MQ.Query<int>($"${{FindItem[{weaponName}].ItemSlot}}");
            var itemSlot2 = MQ.Query<int>($"${{FindItem[{weaponName}].ItemSlot2}}");
            var packSlot = itemSlot - 22;
            var inPackSlot = itemSlot2 + 1;

            MQ.Cmd($"/nomodkey /itemnotify in pack{packSlot} {inPackSlot} leftmouseup");
        }

        private static bool GiveOther(int petId, string spell)
        {
            _summonedItemMap.TryGetValue(spell, out var item);
            var foundSummonedItem = MQ.Query<bool>($"${{FindItem[={item}]}}");
            if (!foundSummonedItem)
            {
                var summonResult = SummonItem(spell, false);
                if (!summonResult.success)
                {
                    E3.Bots.Broadcast($"\ar{summonResult.error}");
                    return false;
                }
            }
            else
            {
                MQ.Cmd($"/nomodkey /itemnotify \"{item}\" rightmouseup");
                MQ.Delay(3000, "${Cursor.ID}");
            }

            if (Casting.TrueTarget(petId))
            {
                e3util.GiveItemOnCursorToTarget(false);
                return true;
            }

            return false;
        }

        private static (bool success, string error) SummonItem(string itemToSummon, bool inventoryTheSummonedItem)
        {
            var id = E3.CurrentId;
            Casting.TrueTarget(id);
            var spell = new Spell(itemToSummon);
            if (Casting.CheckReady(spell))
            {
                int cursorId = 0;
                // try several times to summon
                for (int i = 1; i <= 5; i++)
                {
                    Casting.Cast(id, spell);
                    e3util.YieldToEQ();
                    cursorId = MQ.Query<int>("${Cursor.ID}");
                    if (cursorId > 0) break;
                }

                if (cursorId == 0)
                {
                    return (false, "Unable to complete spell cast");
                }

                e3util.ClearCursor();

                if (_summonedItemMap.TryGetValue(itemToSummon, out var summonedItem))
                {
                    MQ.Cmd($"/nomodkey /itemnotify \"{summonedItem}\" rightmouseup");
                    MQ.Delay(3000, "${Cursor.ID}");
                    if (inventoryTheSummonedItem)
                    {
                        e3util.ClearCursor();
                    }
                }

                return (true, null);
            }
            else
            {
                return (false, $"Unable to cast {itemToSummon} because it wasn't ready");
            }
        }

        private static bool CheckInventory()
        {
            // clean up any leftovers
            var summonedItemCount = MQ.Query<int>($"${{FindItemCount[={_armorOrHeirloomBag}]}}");
            for (int i = 1; i <= summonedItemCount; i++)
            {
                MQ.Cmd($"/nomodkey /itemnotify \"{_armorOrHeirloomBag}\" leftmouseup");
                MQ.Delay(1000, "${Cursor.ID}");
                if(!e3util.ValidateCursor(MQ.Query<int>($"${{FindItem[={_armorOrHeirloomBag}].ID}}")))
                {
                    E3.Bots.Broadcast($"\arUnexpected item on cursor when trying to destroy {_armorOrHeirloomBag}");
                    return false;
                }

                MQ.Cmd("/destroy");
            }

            var bag = "Huge Disenchanted Backpack";
            summonedItemCount = MQ.Query<int>($"${{FindItemCount[={bag}]}}");
            for (int i = 1; i <= summonedItemCount; i++)
            {
                MQ.Cmd($"/nomodkey /itemnotify \"{bag}\" leftmouseup");
                MQ.Delay(1000, "${Cursor.ID}");
                if (!e3util.ValidateCursor(MQ.Query<int>($"${{FindItem[={bag}].ID}}")))
                {
                    E3.Bots.Broadcast($"\arUnexpected item on cursor when trying to destroy {bag}");
                    return false;
                }

                MQ.Cmd("/destroy");
            }

            int containerWithOpenSpace = -1;
            int slotToMoveFrom = -1;
            bool hasOpenInventorySlot = false;

            // see if we need to do anything
            for (int i = 1; i <= 10; i++)
            {
                var currentSlot = i;
                var containerSlots = MQ.Query<int>($"${{Me.Inventory[pack{i}].Container}}");
                var containerItemCount = MQ.Query<int>($"${{InvSlot[pack{i}].Item.Items}}");

                // the slot is empty, we're good!
                if (containerSlots == -1)
                {
                    slotToMoveFrom = -1;
                    hasOpenInventorySlot = true;
                    break;
                }

                // it's an empty bag
                if (containerItemCount == 0)
                {
                    slotToMoveFrom = i;
                    break;
                }

                if (containerSlots - containerItemCount > 0)
                {
                    containerWithOpenSpace = i;
                }

                // it's not a container, OR it's an empty container, we might have to move it
                if (containerSlots == 0 || (containerSlots > 0 && containerItemCount == 0))
                {
                    slotToMoveFrom = currentSlot;
                }
            }

            var freeInventory = MQ.Query<int>("${Me.FreeInventory}");
            if (freeInventory > 0 && containerWithOpenSpace > 0 && slotToMoveFrom > 0)
            {
                MQ.Cmd($"/nomodkey /itemnotify pack{slotToMoveFrom} leftmouseup");
                MQ.Delay(250);

                if (MQ.Query<bool>("${Window[QuantityWnd].Open}"))
                {
                    MQ.Cmd("/nomodkey /notify QuantityWnd QTYW_Accept_Button leftmouseup");
                }
                MQ.Delay(1000, "${Cursor.ID}");
            }

            freeInventory = MQ.Query<int>("${Me.FreeInventory}");
            if (freeInventory > 0)
            {
                hasOpenInventorySlot = true;
            }

            if (MQ.Query<bool>("${Cursor.ID}") && containerWithOpenSpace > 0)
            {
                var slots = MQ.Query<int>($"${{Me.Inventory[pack{containerWithOpenSpace}].Container}}");
                for (int i = 1; i <= slots; i++)
                {
                    var item = MQ.Query<string>($"${{Me.Inventory[pack{containerWithOpenSpace}].Item[{i}]}}");
                    if (string.Equals(item, "NULL", StringComparison.OrdinalIgnoreCase))
                    {
                        MQ.Cmd($"/nomodkey /itemnotify in pack{containerWithOpenSpace} {i} leftmouseup");
                        MQ.Delay(1000, "!${Cursor.ID}");
                        hasOpenInventorySlot = true;
                        break;
                    }
                }

                // no room at the inn anymore, just put it back
                if (MQ.Query<bool>("${Cursor.ID}"))
                {
                    e3util.ClearCursor();
                }
            }

            return hasOpenInventorySlot;
        }
    }
}
