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
        private static IMQ MQ = E3.Mq;
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

            EventProcessor.RegisterEvent("EquipPets", "(.+) tells you, 'equippet (.+)'", (x) =>
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

                var weaponSplit = x.match.Groups[2].ToString().Split('|');
                if (weaponSplit.Count() != 2)
                {
                    MQ.Cmd($"/t {_requester} Invalid request. The request must be in the format of 'equippet Primary|Secondary'");
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

                    EquipPet(theirPetId, $"{weaponSplit[0]}|{weaponSplit[1]}", true);
                }
            });
        }

        /// <summary>
        /// Checks pets for items and re-equips if necessary.
        /// </summary>
        [ClassInvoke(Data.Class.Magician)]
        public static void EquipPets()
        {
            if (Assist._isAssisting) return;
            if (!E3.CharacterSettings.AutoPetWeapons) return;

            // my pet
            var primary = MQ.Query<int>("${Me.Pet.Primary}");
            var myPetId = MQ.Query<int>("${Me.Pet.ID}");
            if (myPetId > 0 && primary == 0)
            {
                E3.CharacterSettings.PetWeapons.TryGetValue(E3.CurrentName, out var weapons);
                EquipPet(myPetId, weapons, false);
            }

            // bot pets
            foreach (var kvp in E3.CharacterSettings.PetWeapons)
            {
                if (_spawns.TryByName(kvp.Key, out var ownerSpawn))
                {
                    if (string.Equals(ownerSpawn.Name, E3.CurrentName)) continue;
                    var theirPetId = ownerSpawn.PetID;
                    if (theirPetId < 0) continue;
                    var theirPetPrimary = MQ.Query<int>($"${{Spawn[{ownerSpawn.Name}].Pet.Primary}}");
                    if (theirPetPrimary == 0)
                    {
                        EquipPet(theirPetId, kvp.Value, false);
                    }
                }
            }
        }

        private static void EquipPet(int petId, string weapons, bool isExternalRequest)
        {
            if (!CheckInventory())
            {
                if (isExternalRequest)
                {
                    MQ.Cmd($"/t {_requester} I was unable to free up inventory space to fulfill your request.");
                }
                else
                {
                    E3.Bots.Broadcast("\arUnable to free up inventory space to arm pets.");
                }

                return;
            }

            // so we can move back
            var currentX = MQ.Query<double>("${Me.X}");
            var currentY = MQ.Query<double>("${Me.Y}");

            GiveWeapons(petId, weapons ?? "Water|Fire");
            GiveArmor(petId);
            GiveFocusItems(petId);

            // move back to my original location
            e3util.TryMoveToLoc(currentX, currentY);
        }

        private static void GiveWeapons(int petId, string weaponString)
        {
            var weapons = weaponString.Split('|');
            _weaponMap.TryGetValue(weapons[0], out var primary);
            _weaponMap.TryGetValue(weapons[1], out var secondary);

            var foundPrimary = MQ.Query<bool>($"${{FindItem[={primary}]}}");
            var foundSecondary = MQ.Query<bool>($"${{FindItem[={secondary}]}}");
            if (!foundPrimary || !foundSecondary)
            {
                var foundWeaponBag = MQ.Query<bool>($"${{FindItem[={_weaponBag}]}}");
                if (foundWeaponBag)
                {
                    MQ.Cmd($"/itemnotify \"{_weaponBag}\" leftmouseup");
                    MQ.Delay(1000, "${Cursor.ID}");
                    MQ.Cmd("/destroy");
                }

                SummonItem(_weaponSpell, true);
            }

            if (Casting.TrueTarget(petId))
            {
                MQ.Cmd($"/itemnotify \"{primary}\" leftmouseup");
                e3util.GiveItemOnCursorToTarget(false);
                MQ.Delay(250);
                if (Casting.TrueTarget(petId))
                {
                    Casting.TrueTarget(petId);
                }

                MQ.Cmd($"/itemnotify \"{secondary}\" leftmouseup");
                e3util.GiveItemOnCursorToTarget(false);
            }
        }

        private static void GiveArmor(int petId)
        {
            var foundSummonedItem = MQ.Query<bool>($"${{FindItem[={_armorItem}]}}");
            if (!foundSummonedItem)
            {
                SummonItem(_armorSpell, false);
            }

            if (Casting.TrueTarget(petId))
            {
                e3util.GiveItemOnCursorToTarget(false);
            }
        }

        private static void GiveFocusItems(int petId)
        {
            var foundSummonedItem = MQ.Query<bool>($"${{FindItem[={_focusItem}]}}");
            if (!foundSummonedItem)
            {
                SummonItem(_focusSpell, false);
            }

            if (Casting.TrueTarget(petId))
            {
                e3util.GiveItemOnCursorToTarget(false);
            }
        }

        private static void SummonItem(string itemToSummon, bool inventoryTheSummonedItem)
        {
            var id = E3.CurrentId;
            if (Casting.TrueTarget(id))
            {
                var spell = new Spell(itemToSummon);
                if (Casting.CheckReady(spell))
                {
                    Casting.Cast(id, spell);

                    MQ.Delay(1000, "${Cursor.ID}");
                    e3util.ClearCursor();

                    if (_summonedItemMap.TryGetValue(itemToSummon, out var summonedItem))
                    {
                        MQ.Cmd($"/itemnotify \"{summonedItem}\" rightmouseup");
                        MQ.Delay(3000, "${Cursor.ID}");
                        if (inventoryTheSummonedItem)
                        {
                            e3util.ClearCursor();
                        }
                    }
                }
                else
                {
                    E3.Bots.Broadcast($"\arUnable to cast {itemToSummon} because it wasn't ready");
                }
            }
        }

        private static bool CheckInventory()
        {
            // clean up any leftovers
            var summonedItemCount = MQ.Query<int>($"${{FindItemCount[={_armorOrHeirloomBag}]}}");
            for (int i = 1; i <= summonedItemCount; i++)
            {
                MQ.Cmd($"/itemnotify \"{_armorOrHeirloomBag}\" leftmouseup");
                MQ.Delay(1000, "${Cursor.ID}");
                MQ.Cmd("/destroy");
            }

            var bag = "Huge Disenchanted Backpack";
            summonedItemCount = MQ.Query<int>($"${{FindItemCount[={bag}]}}");
            for (int i = 1; i <= summonedItemCount; i++)
            {
                MQ.Cmd($"/itemnotify \"{bag}\" leftmouseup");
                MQ.Delay(1000, "${Cursor.ID}");
                MQ.Cmd("/destroy");
            }

            int containerWithOpenSpace = -1;
            int slotToMoveFrom = -1;
            bool hasOpenInventorySlot = false;

            // see if we have an open slot in a bag 
            for (int i = 1; i <= 10; i++)
            {
                var containerSlots = MQ.Query<int>($"${{Me.Inventory[pack{i}].Container}}");
                var containerItemCount = MQ.Query<int>($"${{InvSlot[pack{i}].Item.Items}}");
                if (containerSlots - containerItemCount > 0)
                {
                    containerWithOpenSpace = i;
                    break;
                }
            }

            // see if we have an open bag slot
            for (int i = 1; i <= 10; i++)
            {
                var currentSlot = i;
                var containerSlots = MQ.Query<int>($"${{Me.Inventory[pack{i}].Container}}");
                // the slot is empty, we're good!
                if (containerSlots == -1)
                {
                    slotToMoveFrom = -1;
                    hasOpenInventorySlot = true;
                    break;
                }

                // it's not a container, we might have to move it
                if (containerSlots == 0)
                {
                    slotToMoveFrom = currentSlot;
                }
            }

            var freeInventory = MQ.Query<int>("${Me.FreeInventory}");
            if (freeInventory > 0 && containerWithOpenSpace > 0 && slotToMoveFrom > 0)
            {
                MQ.Cmd($"/itemnotify pack{slotToMoveFrom} leftmouseup");
                MQ.Delay(250);

                if (MQ.Query<bool>("${Window[QuantityWnd].Open}"))
                {
                    MQ.Cmd("/notify QuantityWnd QTYW_Accept_Button leftmouseup");
                }
                MQ.Delay(1000, "${Cursor.ID}");
            }

            freeInventory = MQ.Query<int>("${Me.FreeInventory");
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
                        MQ.Cmd($"/itemnotify in pack{containerWithOpenSpace} {i} leftmouseup");
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
