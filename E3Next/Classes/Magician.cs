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
        private static Dictionary<string, string> _weaponMap = new Dictionary<string, string> {
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

        /// <summary>
        /// Accepts a pet equipment request.
        /// </summary>
        [SubSystemInit]
        public static void PetEquipmentRequest()
        {
            EventProcessor.RegisterEvent("EquipPets", "(.+) tells you, 'equippet (.+)'", (x) =>
            {
                if (!(x.match.Groups.Count > 1)) return;
                var textInfo = new CultureInfo("en-US", false).TextInfo;
                var requester = x.match.Groups[1].ToString();
                if (E3.CurrentClass != Class.Magician)
                {
                    MQ.Cmd($"/t {requester} Only magicians can give out pet weapons!");
                    return;
                }

                var weapons = textInfo.ToTitleCase(x.match.Groups[2].ToString());
                var weaponSplit = weapons.Split('|');
                if (weaponSplit.Count() != 2)
                {
                    MQ.Cmd($"/t {requester} Invalid request. The request must be in the format of 'equippet Primary|Secondary'");
                    return;
                }

                if (!_weaponMap.TryGetValue(textInfo.ToTitleCase(weaponSplit[0]), out _))
                {
                    MQ.Cmd($"/t {requester} Invalid primary weapon selection. Valid values are {string.Join(", ", _weaponMap.Keys)}");
                    return;
                }

                if (!_weaponMap.TryGetValue(textInfo.ToTitleCase(weaponSplit[1]), out _))
                {
                    MQ.Cmd($"/t {requester} Invalid secondary weapon selection. Valid values are {string.Join(", ", _weaponMap.Keys)}");
                    return;
                }

                if(_spawns.TryByName(requester, out var requesterSpawn))
                {
                    var theirPetId = requesterSpawn.PetID;
                    if(theirPetId < 0)
                    {
                        MQ.Cmd($"/t {requester} You don't have a pet to equip!");
                        return;
                    }

                    if (_spawns.Get().First(w => w.ID == theirPetId).Distance > 50)
                    {
                        MQ.Cmd($"/t {requester} Your pet is too far away!");
                        return;
                    }

                    EquipPet(theirPetId, $"{weaponSplit[0]}|{weaponSplit[1]}");
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

            // clean up any leftovers
            var summonedItemCount = MQ.Query<int>($"${{FindItemCount[={_armorOrHeirloomBag}]}}");
            for (int i = 1; i<=summonedItemCount; i++)
            {
                MQ.Cmd($"/itemnotify \"{_armorOrHeirloomBag}\" leftmouseup");
                MQ.Delay(1000, "${Cursor.ID}");
                MQ.Cmd("/destroy");
            }

            var bag = "Huge Disenchanted Backpack";
            summonedItemCount = MQ.Query<int>($"${{FindItemCount[={bag}]}}");
            for (int i = 1; i<=summonedItemCount; i++)
            {
                MQ.Cmd($"/itemnotify \"{bag}\" leftmouseup");
                MQ.Delay(1000, "${Cursor.ID}");
                MQ.Cmd("/destroy");
            }

            // my pet
            var primary = MQ.Query<int>("${Me.Pet.Primary}");
            var myPetId = MQ.Query<int>("${Me.Pet.ID}");
            if (myPetId > 0 && primary == 0)
            {
                E3.CharacterSettings.PetWeapons.TryGetValue(E3.CurrentName, out var weapons);
                EquipPet(myPetId, weapons);
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
                        EquipPet(theirPetId, kvp.Value);
                    }
                }
            }
        }

        private static void EquipPet(int petId, string weapons)
        {
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
                Casting.TrueTarget(petId);
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
            if (Casting.TrueTarget(E3.CurrentId))
            {
                if (Casting.Cast(E3.CurrentId, new Data.Spell(itemToSummon)) == CastReturn.CAST_SUCCESS)
                {
                    MQ.Delay(1000, "${Cursor.ID}");
                    e3util.ClearCursor();
                }

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
        }
    }
}
