using E3Core.Data;
using E3Core.Processors;
using E3Core.Settings;
using E3Core.Utility;
using MonoCore;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms.VisualStyles;

namespace E3Core.Classes
{
    /// <summary>
    /// Properties and methods specific to the magician class
    /// </summary>
    public static class Magician
    {
        private static IMQ MQ = E3.MQ;
        private static ISpawns _spawns = E3.Spawns;
        private static List<string> _cleanbags = E3.CharacterSettings.CleanBags;

        //___Hardcoded Bags  for backwards compatability___
        private static List<string> _hardcodedbags = (E3.CharacterSettings.CleanBags == null || !E3.CharacterSettings.CleanBags.Any() || E3.CharacterSettings.CleanBags.Any(string.IsNullOrWhiteSpace)) ?
         new List<string>
         {
             "Folded Pack of Spectral Armaments",
             "Folded Pack of Spectral Plate",
             "Folded Pack of Enibik's Heirlooms"
         } :
         new List<string>();

        //___Hardcoded Spell|item|Identifers for backwards compatability___
        private static List<string> _hardcodedItems = (E3.CharacterSettings.spiIni == null || !E3.CharacterSettings.spiIni.Any() || E3.CharacterSettings.spiIni.All(string.IsNullOrWhiteSpace)) ?
        new List<string>
         {
             "Grant Spectral Armaments|Summoned: Fist of Flame|Fire",
             "Grant Spectral Armaments|Summoned: Orb of Chilling Water|Water",
             "Grant Spectral Armaments|Summoned: Buckler of Draining Defense|Shield",
             "Grant Spectral Armaments|Summoned: Short Sword of Warding|Taunt",
             "Grant Spectral Armaments|Summoned: Mace of Temporal Distortion|Slow",
             "Grant Spectral Armaments|Summoned: Spear of Maliciousness|Malo",
             "Grant Spectral Armaments|Summoned: Wand of Dismissal|Dispel",
             "Grant Spectral Armaments|Summoned: Tendon Carver|Snare",
             "Grant Spectral Plate|Folded Pack of Spectral Plate|none",
             "Grant Enibik's Heirlooms|Folded Pack of Enibik's Heirlooms|none"
         } :
        new List<string>();

        //___Pulling Summoned Pet Items from E3.CharacterSettings___
        public class SpellItem
        {
            public string Spell { get; set; }
            public string Item { get; set; }
            public string Identifier { get; set; }
        }
        private static Dictionary<string, List<SpellItem>> _spiMap = new Dictionary<string, List<SpellItem>>();
        static Magician()
        {
            if (E3.CharacterSettings.AutoPetDebug) E3.Bots.Broadcast($"\amDebug: Magician: spiIni Dictionary: {E3.CharacterSettings.spiIni}");

            //Will Add hard coded items to the spiIni Dictionary if the above it true otherwise it will just use the E3.CharacterSettings.spiIni
            var allItems = _hardcodedItems.Concat(E3.CharacterSettings.spiIni);

            if (E3.CharacterSettings.spiIni == null || !E3.CharacterSettings.spiIni.Any() || E3.CharacterSettings.spiIni.All(string.IsNullOrWhiteSpace) && E3.CharacterSettings.AutoPetDebug) E3.Bots.Broadcast($"\amDebug: Magician: spiIni Dictionary with hardcode: {allItems}");

            // Split the entire string into separate entries
            foreach (var spiIni in allItems)
            {
                if (E3.CharacterSettings.AutoPetDebug) E3.Bots.Broadcast($"\amspiIni: {spiIni}");
                var data = spiIni.Split('|');
                if (data.Length == 3)
                {
                    var spiSpell = data[0].Trim();
                    var spiItem = data[1].Trim();
                    var identifier = data[2].Trim();
                    var spellItem = new SpellItem { Spell = spiSpell, Item = spiItem, Identifier = identifier };
                    if (!_spiMap.ContainsKey(spiSpell))
                    {
                        _spiMap[spiSpell] = new List<SpellItem>();
                    }
                    _spiMap[spiSpell].Add(spellItem);
                }
            }
        }

        private static Dictionary<int, string> _inventorySlotToPackMap = new Dictionary<int, string>
        {
            {23, "pack1" },
            {24, "pack2" },
            {25, "pack3" },
            {26, "pack4" },
            {27, "pack5" },
            {28, "pack6" },
            {29, "pack7" },
            {30, "pack8" },
            {31, "pack9" },
            {32, "pack10" },
        };

        private static string _requester;

        private static long _nextInventoryCheck = 0;
        private static long _nextInventoryCheckInterval = 5000;
        private static long _nextCheckAllPetsEquipped = 0;
        private static long _nextCheckAllPetsEquippedInterval = 5000;

        private static int[] GetEnchanterPrimaryWeaponIds()
        {
            int[] baseIds = { 10702, 10653, 10648, 41, 60 };
            string additionalIdsString = E3.CharacterSettings.AdditionalIDsString;

            if (string.IsNullOrEmpty(additionalIdsString))
            {
                return baseIds;
            }

            int[] additionalIds = additionalIdsString
                .Split(',')
                .Select(int.Parse)
                .ToArray();

            return baseIds.Concat(additionalIds).ToArray();
        }

        private static readonly int[] EnchanterPetPrimaryWeaponIds = GetEnchanterPrimaryWeaponIds();

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

            //Provides List of Pet Item Identifiers to Requester
            var armPetList = new List<string> { "(.+) tells you, 'armpetlist'", "(.+) tells you, 'armpetlist'", "(.+) tells the group, 'armpetlist'", };
            EventProcessor.RegisterEvent("ArmPetList", armPetList, (x) =>
            {
                if (e3util.IsShuttingDown() || E3.IsPaused()) return;

                //___Process Request Requirements___
                if (E3.CharacterSettings.AutoPetDebug)  E3.Bots.Broadcast("\acPet List Request Event Started");
                _requester = x.match.Groups[1].ToString();
                string configuredIdentifiers = string.Join(", ", _spiMap.Values.SelectMany(list => list.Select(item => $"[{item.Identifier}]")).Distinct());

                if (E3.CharacterSettings.AutoPetDebug) E3.Bots.Broadcast($"\acConfigured Identifiers: {configuredIdentifiers}");

                //Send Tell of Pet Item Identifiers to Requester
                MQ.Cmd($"/t {_requester} My current configured Pet Item Identifiers are: {configuredIdentifiers}.");
                if (E3.CharacterSettings.AutoPetDebug)  E3.Bots.Broadcast("\acPet List Request Event Finished");

            });

            var armPetEvents = new List<string> { "(.+) tells you, 'armpet'", "(.+) tells you, 'armpet (.+)'", "(.+) tells the group, 'armpet (.+)'", };
            EventProcessor.RegisterEvent("ArmPet", armPetEvents, (x) =>
            {
                if (e3util.IsShuttingDown() || E3.IsPaused()) return;

                //___Process Request Requirements___
                if (E3.CharacterSettings.AutoPetDebug) E3.Bots.Broadcast("\agPet Equipment Request Event Started");
                int xCount = x.match.Groups.Count;
                _requester = x.match.Groups[1].ToString();
                if (E3.CharacterSettings.AutoPetDebug) E3.Bots.Broadcast($"\amDebug: PER: x.match.Groups.Count: {xCount}");
                if (E3.CharacterSettings.AutoPetDebug) E3.Bots.Broadcast($"\amDebug: PER: Requestername: {_requester}");
                string[] identArray = x.match.Groups[2].ToString().Split(new char[] { ',', '|' });
                List<string> identifiers = identArray.ToList();

                //___Identifier Checks___
                if (x.match.Groups.Count <= 2)
                {
                    E3.Bots.Broadcast($"\ayWARN: PER: {_requester} did not provide any identifiers. Ending {_requester} Request");
                    MQ.Cmd($"/t {_requester} You did not provide any identifiers. Message me armpetlist if you need a list of identifiers.");
                    return;
                }

                bool allIdentifiersExist = identifiers.All(id => _spiMap.Values.Any(spellItemList => spellItemList.Any(spellItem => spellItem.Identifier.Equals(id, StringComparison.OrdinalIgnoreCase))));

                if (!allIdentifiersExist)
                {
                    string configuredIdentifiers = string.Join(", ", _spiMap.Values.SelectMany(list => list.Select(item => $"[{item.Identifier}]")).Distinct());
                    E3.Bots.Broadcast($"\ayWarn: PER: A requested identifiers from {_requester} does not exist. Ending {_requester} Request");
                    if (E3.CharacterSettings.AutoPetDebug) E3.Bots.Broadcast($"\amDebug: Requested Identifiers: {string.Join(", ", identifiers)} | Current Configed Indentifiers: {configuredIdentifiers}.");
                    MQ.Cmd($"/t {_requester} One or more identifiers you requested are currently not configured.My current configured identifiers are: {configuredIdentifiers}.");
                    return;
                }                

                //___PreRequester Checks___
                if (E3.CharacterSettings.IgnorePetWeaponRequests)
                {
                    MQ.Cmd($"/t {_requester} Sorry, I am not currently accepting requests for pet weapons");
                    return;
                }

                if (E3.CurrentClass != Class.Magician)
                {
                    MQ.Cmd($"/t {_requester} Only magicians can give out pet weapons!");
                    return;
                }

                if(!CheckInventory())
                {
                    MQ.Cmd($"/t {_requester} I don't have any inventory space to give you a pet weapon!");
                    E3.Bots.Broadcast($"\arERROR: PER: Inventory is full, Canceling Request for {_requester}");
                    return;
                }                

                //___Requester Checks and Method Call___
                if (_spawns.TryByName(_requester, out var spawn))
                {
                    var petId = MQ.Query<int>($"${{Spawn[{spawn.Name}].Pet.ID}}");

                    if (petId <= 0)
                    {
                        E3.Bots.Broadcast($"\ayWARN: PER: {_requester} has no pet. Ending thier Request");
                        MQ.Cmd($"/t {_requester} You don't have a pet to equip!");
                        return;
                    }

                    if (_spawns.Get().First(w => w.ID == petId).Distance > 50)
                    {
                        E3.Bots.Broadcast($"\ayWARN: PER: {_requester} is too far away or thier pet is. Ending thier Request");
                        MQ.Cmd($"/t {_requester} Your pet is too far away!");
                        return;
                    }

                    if (_spawns.Get().First(w => w.ID == petId).Level == 1)
                    {
                        E3.Bots.Broadcast($"\ayWARN: PER: {_requester} asking to arm a familiar, not supported. Ending thier Request");
                        MQ.Cmd($"/t {_requester} Your pet is just a familiar!");
                        return;
                    }

                    MQ.Cmd($"/t {_requester} Arming Pet Please Give me a moment, If you move more then 30y this will fail");
                    if (e3util.IsShuttingDown() || E3.IsPaused()) return;
                    IdentForRequester(_requester, identifiers, petId);
                    if (e3util.IsShuttingDown() || E3.IsPaused()) return;
                }
                else
                {
                    MQ.Cmd($"/t {_requester} I couldn't find your pet!");
                }                

                MQ.Cmd($"/t {_requester} Arming Pet had finished, Happy Hunting");
                if (E3.CharacterSettings.AutoPetDebug) E3.Bots.Broadcast("\agArmpet Event process Finished");
                CleanUp();
                HCCleanUp();

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
            if (CheckAllPetsEquipped()) return;
            if (!CheckInventory()) return;

            ArmPets();
        }

        public static void ArmPets()
        {
            E3.Bots.Broadcast("\agArmPets started.");
            if (MQ.Query<int>("${Cursor.ID}") > 0)
            {
                if (!e3util.ClearCursor())
                {
                    E3.Bots.Broadcast("\arI was unable to clear my cursor so I cannot continue.");
                }
            }

            //___MY PET___
            var primary = MQ.Query<int>("${Me.Pet.Primary}");
            var myPetId = MQ.Query<int>("${Me.Pet.ID}");
            if (myPetId > 0 && primary == 0)
            {
                if (E3.CharacterSettings.AutoPetDebug) E3.Bots.Broadcast($"\amPet ID: {myPetId}, Primary: {primary}");
                if (E3.CharacterSettings.AutoPetDebug) E3.Bots.Broadcast($"\am_spiMap count: {_spiMap.Count}");

                if (e3util.IsShuttingDown() || E3.IsPaused()) return;
                var myBotName = MQ.Query<string>("${Me.Name}");
                if (E3.CharacterSettings.AutoPetDebug) E3.Bots.Broadcast($"\amDebug: AP: About to call IdentForCharacter for myBotName");
                IdentForCharacter(myBotName, myPetId);
                if (e3util.IsShuttingDown() || E3.IsPaused()) return;
            }            

            //___BOT PETS___
            foreach (var botsettings in E3.CharacterSettings.PetWeapons)
            {
                var parts = botsettings.Split('/');
                var bot = parts[0];
                if (E3.CharacterSettings.AutoPetDebug) E3.Bots.Broadcast($"\ayDebug: Processing bot: {bot}");

                if (_spawns.TryByName(bot, out var ownerSpawn))
                {
                    if (e3util.IsShuttingDown() || E3.IsPaused()) return;

                    if (string.Equals(ownerSpawn.Name, E3.CurrentName)) continue;

                    var botName = ownerSpawn.CleanName;
                    var petId = ownerSpawn.PetID;
                    var petSpawn = _spawns.Get().FirstOrDefault(w => w.ID == petId);

                    if (petId < 0)
                    {
                        E3.Bots.Broadcast($"\ayWarn: {botName} doesn't have a pet to summon!");
                        continue;
                    }

                    if (petSpawn == null)
                    {
                        E3.Bots.Broadcast($"\ayWarn: {botName} doesn't have a pet to summon!");
                        continue;
                    }

                    if (petSpawn.Distance > 50)
                    {
                        E3.Bots.Broadcast($"\ayWarn: {botName}'s pet is too far away!");
                        continue;
                    }

                    if (petSpawn.Level == 1)
                    {
                        E3.Bots.Broadcast($"\ayWarn: {botName}'s pet is just a familiar!");
                        continue;
                    }

                    var theirPetPrimary = MQ.Query<int>($"${{Spawn[{ownerSpawn.Name}].Pet.Primary}}");
                    if (theirPetPrimary == 0 || EnchanterPetPrimaryWeaponIds.Contains(theirPetPrimary))
                    {
                        if (e3util.IsShuttingDown() || E3.IsPaused()) return;
                        if (E3.CharacterSettings.AutoPetDebug) E3.Bots.Broadcast($"\ayDebug: AP: About to call IdentForCharacter for bot: {botName}");                        
                        IdentForCharacter(botName, petId);
                        if (e3util.IsShuttingDown() || E3.IsPaused()) return;
                    }
                }
            }            
            
            E3.Bots.Broadcast("\agArmPets finished.");
            CleanUp();
            HCCleanUp();
        }

        public static void IdentForCharacter(string characterName, int PetId)
        {
            //My Current Location to moveback to later
            var currentX = MQ.Query<double>("${Me.X}");
            var currentY = MQ.Query<double>("${Me.Y}");
            var currentZ = MQ.Query<double>("${Me.Z}");

            E3.Bots.Broadcast($"\awDebug: IdentForCharacter started for {characterName}.");
            
            // Parse the INI setting
            var characterSetting = E3.CharacterSettings.PetWeapons.FirstOrDefault(s => s.StartsWith(characterName + "/"));

            if (characterSetting == null)
            {
                E3.Bots.Broadcast($"\arNo Identifiers found for character {characterName}.");
                return;
            }

            // Get the identifiers for the current character
            var identifiers = characterSetting.Split('/')[1].Split(new char[] { ',', '|' });
            if (E3.CharacterSettings.AutoPetDebug) E3.Bots.Broadcast($"\amDebug: IFC: Identifiers for {characterName}: {string.Join(", ", identifiers)}");


            // Cast spells for each identifier
            foreach (var identifier in identifiers)
            {
                bool isHardcodeIdentifier = _hardcodedItems.Contains(identifier);

                foreach (var entry in _spiMap)
                {
                    // Run HarcCode CleanUp bag process if a non-hardcode identifier is detected
                    if (!isHardcodeIdentifier)
                    {
                        HCCleanUp();
                        isHardcodeIdentifier = true;
                    }

                    var spiSpell = entry.Key;
                    foreach (var spellItem in entry.Value.Where(si => si.Identifier.Equals(identifier, StringComparison.OrdinalIgnoreCase)))
                    {
                        if (E3.CharacterSettings.AutoPetDebug) E3.Bots.Broadcast($"\amDebug: IFC: Sending casting request for Spell: {spiSpell}, Item: {spellItem.Item}, for PetID: {PetId}.");
                        spiCastSpell(spiSpell, PetId, spellItem.Item);
                    }
                }
            }

            MQ.Delay(250);
            e3util.TryMoveToLoc(currentX, currentY, currentZ);
            if (E3.CharacterSettings.AutoPetDebug) E3.Bots.Broadcast($"\awDebug: IdentForCharacter Finshed for {characterName}.");
        }

        public static void IdentForRequester(string requesterName, List<string> identifiers, int PetId)
        {
            //My Current Location to moveback to later
            var currentX = MQ.Query<double>("${Me.X}");
            var currentY = MQ.Query<double>("${Me.Y}");
            var currentZ = MQ.Query<double>("${Me.Z}");

            if (E3.CharacterSettings.AutoPetDebug) E3.Bots.Broadcast($"\awDebug: IdentForRequester Started for {requesterName}.");

            foreach (var identifier in identifiers)
            {
                bool isHardcodeIdentifier = _hardcodedItems.Contains(identifier);

                foreach (var entry in _spiMap)
                {
                    // Run Hardcode CleanUp bag process if a non-hardcode identifier is detected
                    if (!isHardcodeIdentifier)
                    {
                        HCCleanUp();
                        isHardcodeIdentifier = true;
                    }

                    var spiSpell = entry.Key;
                    foreach (var spellItem in entry.Value.Where(si => si.Identifier.Equals(identifier, StringComparison.OrdinalIgnoreCase)))
                    {
                        if (E3.CharacterSettings.AutoPetDebug) E3.Bots.Broadcast($"\amDebug: IFR: Sending casting request for Spell: {spiSpell}, Item: {spellItem.Item}, for PetID: {PetId}.");
                        spiCastSpell(spiSpell, PetId, spellItem.Item);
                    }
                }

            }

            MQ.Delay(250);
            e3util.TryMoveToLoc(currentX, currentY, currentZ);
            if (E3.CharacterSettings.AutoPetDebug) E3.Bots.Broadcast($"\awDebug: IdentForRequester Finished for {requesterName}.");
        }


        private static void spiCastSpell(string spiSpell,int PetId, string spiItem)
        {
            if (E3.CharacterSettings.AutoPetDebug) E3.Bots.Broadcast($"\acDebug: spiCastSpell process Started.");
            
            var spell = new Spell(spiSpell);
            string item = spiItem;
            bool itemfound = MQ.Query<bool>($"${{FindItem[={item}]}}");
            Int32 castAttempts = 0;
            Casting.TrueTarget(PetId);
            if (itemfound)
            {
                if (E3.CharacterSettings.AutoPetDebug) E3.Bots.Broadcast($"\amDebug: SCS: Item found. No casting required.");
                spiGiveItemToPet(PetId, item);
            }
            else
            {
                if (Casting.CheckReady(spell) && Casting.CheckMana(spell))
                {
                    if (E3.CharacterSettings.AutoPetDebug) E3.Bots.Broadcast($"\amDebug: SCS: Casting started for Spell: {spiSpell}, Item: {spiItem}, for PetID: {PetId}.");
                    while (Casting.Cast(PetId, spell) == CastReturn.CAST_FIZZLE)
                    {
                        if (castAttempts >= 5)
                        {
                            E3.Bots.Broadcast($"\ar Your spell: {spell} has failed 5 times. Stopping Summon Attempts.");
                            break;
             
                        }
                        MQ.Delay(1500);
                        castAttempts++;
                    }
                }
                spiGiveItemToPet(PetId, item);
            }
            if (E3.CharacterSettings.AutoPetDebug) E3.Bots.Broadcast($"\acDebug: spiCastSpell process Finished.");

        }

        private static void spiGiveItemToPet(int PetId, string item)
        {
            if (E3.CharacterSettings.AutoPetDebug) E3.Bots.Broadcast($"\acDebug: spiGiveItemToPet process Started.");

            var foundArmorBag = MQ.Query<bool>($"${{FindItem[={item}]}}");
            var oncursor = MQ.Query<string>($"${{Cursor.Name}}");
            if (oncursor == item)
            {
                Casting.TrueTarget(PetId);
                e3util.GiveItemOnCursorToTarget(false, false);
                if (E3.CharacterSettings.AutoPetDebug) E3.Bots.Broadcast($"\amDebug: SGITP: Item Already on Cursor, No Pickup Process Needed.");
            }
            else if (oncursor != item)
            {
                MQ.Cmd("/autoinventory");
                MQ.Delay(250);
                PickUpItem(item);
                Casting.TrueTarget(PetId);
                e3util.GiveItemOnCursorToTarget(false, false);
            }
            else
            {
                if (foundArmorBag)
                {
                    PickUpItem(item);
                    Casting.TrueTarget(PetId);
                    e3util.GiveItemOnCursorToTarget(false, false);
                }
            }

            if (E3.CharacterSettings.AutoPetDebug) E3.Bots.Broadcast($"\amDebug: SGITP: Item: {item} was giving to PetId: {PetId} .");
            if (E3.CharacterSettings.AutoPetDebug) E3.Bots.Broadcast($"\acDebug: spiGiveItemToPet process Finished.");
        }

        private static void PickUpItem(string item)
        {
            if (E3.CharacterSettings.AutoPetDebug) E3.Bots.Broadcast($"\acDebug: PickUpItem process Started.");

            var itemSlot = MQ.Query<int>($"${{FindItem[{item}].ItemSlot}}");
            var itemSlot2 = MQ.Query<int>($"${{FindItem[{item}].ItemSlot2}}");
            var packSlot = itemSlot - 22;
            var inPackSlot = itemSlot2 + 1;

            MQ.Cmd($"/nomodkey /itemnotify in pack{packSlot} {inPackSlot} leftmouseup");

            if (E3.CharacterSettings.AutoPetDebug) E3.Bots.Broadcast($"\amDebug: PI: Item: {item} Picked Up from pack: {packSlot} slot: {inPackSlot} .");
            if (E3.CharacterSettings.AutoPetDebug) E3.Bots.Broadcast($"\acDebug: PickUpItem process Finished.");
        }

        private static void CleanUp()
        {
            if (E3.CharacterSettings.AutoPetDebug) E3.Bots.Broadcast($"\acDebug: CleanUp process Started.");

            foreach (var item in _cleanbags)
            {                
                if (MQ.Query<int>($"${{FindItemCount[{item}]}}") > 0)
                {
                    PickUpItem(item);
                    if (item == MQ.Query<string>($"${{Cursor.Name}}"))
                    {
                        MQ.Cmd("/destroy");
                    }
                    if (E3.CharacterSettings.AutoPetDebug) E3.Bots.Broadcast($"\amDebug: CU: Item: {item} was destroyed.");
                }
            }

            if (E3.CharacterSettings.AutoPetDebug) E3.Bots.Broadcast($"\acDebug: CleanUp process Finished.");
        }

        private static void HCCleanUp()
        {
            if (E3.CharacterSettings.AutoPetDebug) E3.Bots.Broadcast($"\acDebug: HCCleanUp process Started.");

            foreach (var item in _hardcodedbags)
            {                
                if (MQ.Query<int>($"${{FindItemCount[{item}]}}") > 0)
                {
                    PickUpItem(item);
                    if (item == MQ.Query<string>($"${{Cursor.Name}}"))
                    {
                        MQ.Cmd("/destroy");
                    }
                    if (E3.CharacterSettings.AutoPetDebug) E3.Bots.Broadcast($"\amDebug: CU: Item: {item} was destroyed.");
                }
            }

            if (E3.CharacterSettings.AutoPetDebug) E3.Bots.Broadcast($"\acDebug: HCCleanUp process Finished.");
        }

        /// <summary>
        /// Keeps an inventory slot open for summoned shit.
        /// </summary>
        [ClassInvoke(Data.Class.Magician)]
        public static void KeepOpenInvSlot()
        {
            if (!E3.CharacterSettings.KeepOpenInventorySlot) return;
            if (Basics.InCombat()) return;
            if (!e3util.ShouldCheck(ref _nextInventoryCheck, _nextInventoryCheckInterval)) return;

            var slotToKeepOpen = "pack10";

            // if we have no open inventory slots, return
            var freeInv = MQ.Query<int>("${Me.FreeInventory}");
            if (freeInv == 0)
            {
                if (E3.CharacterSettings.AutoPetWeapons)
                {
                    E3.Bots.Broadcast("\arError: No free inventory space and auto pet weapons is on - toggling off so inventory space can be freed up");
                    E3.CharacterSettings.AutoPetWeapons = false;
                }

                return;
            }

            // check if there's anything there
            var slotQueryResult = MQ.Query<string>($"${{Me.Inventory[{slotToKeepOpen}]}}");
            if (slotQueryResult == "NULL") return;

            // find a spot to move it to
            var containerWithOpenSpace = 0;
            for (int i = 1; i <= 9; i++)
            {
                var containerSlots = MQ.Query<int>($"${{Me.Inventory[pack{i}].Container}}");
                if (containerSlots == 0) continue;

                var containerItemCount = Math.Abs(MQ.Query<int>($"${{InvSlot[pack{i}].Item.Items}}"));
                if (containerItemCount < containerSlots)
                {
                    containerWithOpenSpace = i;
                    break;
                }
            }

            // find out if it's a container or an item
            var bagQueryResult = MQ.Query<int>($"${{Me.Inventory[{slotToKeepOpen}].Container}}");
            if (bagQueryResult == 0)
            {
                // it's an item; find the first open container and move it there
                MQ.Cmd($"/shiftkey /itemnotify \"{slotQueryResult}\" leftmouseup");
                var slotsInContainer = MQ.Query<int>($"${{Me.Inventory[pack{containerWithOpenSpace}].Container}}");
                for (int i = 1; i <= slotsInContainer; i++)
                {
                    var item = MQ.Query<string>($"${{Me.Inventory[pack{containerWithOpenSpace}].Item[{i}]}}");
                    if (string.Equals(item, "NULL", StringComparison.OrdinalIgnoreCase))
                    {
                        MQ.Cmd($"/nomodkey /itemnotify in pack{containerWithOpenSpace} {i} leftmouseup");
                        MQ.Delay(1000, "!${Cursor.ID}");
                        break;
                    }
                }
            }
            else
            {
                // it's a container - move it if it's empty
                if (MQ.Query<int>($"${{InvSlot[{slotToKeepOpen}].Item.Items}}") == 0)
                {
                    MQ.Cmd($"/itemnotify \"{slotToKeepOpen}\" leftmouseup");
                    var slotsInContainer = MQ.Query<int>($"${{Me.Inventory[pack{containerWithOpenSpace}].Container}}");
                    for (int i = 1; i <= slotsInContainer; i++)
                    {
                        var item = MQ.Query<string>($"${{Me.Inventory[pack{containerWithOpenSpace}].Item[{i}]}}");
                        if (string.Equals(item, "NULL", StringComparison.OrdinalIgnoreCase))
                        {
                            MQ.Cmd($"/nomodkey /itemnotify in pack{containerWithOpenSpace} {i} leftmouseup");
                            MQ.Delay(1000, "!${Cursor.ID}");
                            break;
                        }
                    }
                }
            }
        }

        private static bool CheckInventory()
        {
            int containerWithOpenSpace = -1;
            int slotToMoveFrom = -1;
            bool hasOpenInventorySlot = false;

            // check top level inventory slots 
            for (int i = 1; i <= 10; i++)
            {
                var item = MQ.Query<string>($"${{Me.Inventory[pack{i}]}}");
                if (item == "NULL")
                {
                    hasOpenInventorySlot = true;
                    break;
                }
            }

            // if no top level slot open, find out if we have containers with space
            if (!hasOpenInventorySlot)
            {
                for (int i = 1; i <= 10; i++)
                {
                    var containerSlotCount = MQ.Query<int>($"${{Me.Inventory[pack{i}].Container}}");
                    if (containerSlotCount == 0) continue;
                    var itemsInContainer = MQ.Query<int>($"${{InvSlot[pack{i}].Item.Items}}");
                    if (itemsInContainer == containerSlotCount) continue;

                    containerWithOpenSpace = i;
                    break;
                }

                for (int i = 10; i >= 1; i--)
                {
                    var containerSlotCount = MQ.Query<int>($"${{Me.Inventory[pack{i}].Container}}");
                    if (containerSlotCount <= 0)
                    {
                        slotToMoveFrom = i;
                        break;
                    }

                    var itemsInContainer = MQ.Query<int>($"${{InvSlot[pack{i}].Item.Items}}");
                    if (itemsInContainer == 0)
                    {
                        slotToMoveFrom = i;
                    }
                }
            }

            var freeInventory = MQ.Query<int>("${Me.FreeInventory}");
            if (freeInventory > 0 && containerWithOpenSpace > 0 && slotToMoveFrom > 0)
            {
                MQ.Cmd($"/shiftkey /itemnotify pack{slotToMoveFrom} leftmouseup");
                MQ.Delay(250);

                if (MQ.Query<bool>("${Window[QuantityWnd].Open}"))
                {
                    MQ.Cmd("/nomodkey /notify QuantityWnd QTYW_Accept_Button leftmouseup");
                }
                MQ.Delay(1000, "${Cursor.ID}");
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

        private static bool CheckAllPetsEquipped()
        {
            if (!e3util.ShouldCheck(ref _nextCheckAllPetsEquipped, _nextCheckAllPetsEquippedInterval)) return true;
            if (E3.CharacterSettings.AutoPetDebug) { E3.Bots.Broadcast("\amDebug: CheckAllPetsEquipped() has been called"); };

            bool allPetsEquipped = true;
            var myPetPrimary = MQ.Query<int>("${Me.Pet.Primary}");

            if (E3.CharacterSettings.AutoPetDebug) E3.Bots.Broadcast($"\amDebug: CAPE: PetWeapons count: {E3.CharacterSettings.PetWeapons.Count}");

            foreach (var botsettings in E3.CharacterSettings.PetWeapons)
            {
                var parts = botsettings.Split('/');
                var bot = parts[0];

                bool found = _spawns.TryByName(bot, out var ownerSpawnCh);
                if (E3.CharacterSettings.AutoPetDebug) E3.Bots.Broadcast($"\amDebug: CAPE: Trying to find bot {bot}, success: {found}");

                if (_spawns.TryByName(bot, out var ownerSpawn))
                {
                    var theirPetPrimary = MQ.Query<int>($"${{Spawn[{ownerSpawn.Name}].Pet.Primary}}");
                    if (theirPetPrimary == 0 || EnchanterPetPrimaryWeaponIds.Contains(theirPetPrimary))
                    {
                        // If any pet is not equipped or has the Enchanter's primary weapon, set the flag to false
                        if (E3.CharacterSettings.AutoPetDebug) E3.Bots.Broadcast($"\amDebug: CAPE: Bot {bot} is not properly equipped");

                        allPetsEquipped = false;
                        break; // Exit the loop early since we already know not all pets are equipped
                    }
                }
            }

            if (myPetPrimary == 0 || !allPetsEquipped)
            {
                allPetsEquipped = false;
            }
            return allPetsEquipped;
        }
    }
}
