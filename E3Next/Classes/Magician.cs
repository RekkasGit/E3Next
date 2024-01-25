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
        //For the backwards compatibility of lazuras code. Allows Laz users to decide if they want to use the new code or the old code. Default is On.
        private static bool _LazurasMageImport = E3.GeneralSettings.LazurasMageImport;
        //Pulling from E3.CharacterSettings for CleanBags
        private static List<string> _cleanbags = E3.CharacterSettings.CleanBags;

        //
        //___Project Lazarus Hard Coded Bags___
        //
        private static List<string> _lazhardcodedbags = new List<string>();
        public static void lazbag()
        {
            if (MQ.Query<String>("${EverQuest.Server}") == "Project Lazarus" && (_LazurasMageImport))
            {
                _lazhardcodedbags = new List<string>
                {
                    "Clean Bags=Folded Pack of Spectral Armaments",
                    "Clean Bags=Folded Pack of Spectral Plate",
                    "Clean Bags=Folded Pack of Enibik's Heirlooms"
                };
            }
        }
        //
        //__^Project Lazarus Hard Coded Bags^__
        //

        //
        //___Project Lazarus Hard Spell|item|Identifers___
        //
        private static List<string> _lazhardcodedItems = new List<string>();
        public static void lazitems()
        {
            if (MQ.Query<String>("${EverQuest.Server}") == "Project Lazarus" && (_LazurasMageImport))
            {
                _lazhardcodedItems = new List<string>
                {
                    "Summoned Pet Item=Grant Spectral Armaments|Summoned: Fist of Flame|Fire",
                    "Summoned Pet Item=Grant Spectral Armaments|Summoned: Orb of Chilling Water|Water",
                    "Summoned Pet Item=Grant Spectral Armaments|Summoned: Buckler of Draining Defense|Shield",
                    "Summoned Pet Item=Grant Spectral Armaments|Summoned: Short Sword of Warding|Taunt",
                    "Summoned Pet Item=Grant Spectral Armaments|Summoned: Mace of Temporal Distortion|Slow",
                    "Summoned Pet Item=Grant Spectral Armaments|Summoned: Spear of Maliciousness|Malo",
                    "Summoned Pet Item=Grant Spectral Armaments|Summoned: Wand of Dismissal|Dispel",
                    "Summoned Pet Item=Grant Spectral Armaments|Summoned: Tendon Carver|Snare",
                    "Summoned Pet Item=Grant Spectral Plate|Folded Pack of Spectral Plate|none",
                    "Summoned Pet Item=Grant Enibik's Heirlooms|Folded Pack of Enibik's Heirlooms|none"
                };
            }
        }
        //
        //__^Project Lazarus Hard Spell|item|Identifers^__
        //

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

            //Will Add lazuras hard coded items to the spiIni Dictionary if the above it true otherwise it will just use the E3.CharacterSettings.spiIni
            var allItems = _lazhardcodedItems.Concat(E3.CharacterSettings.spiIni);

            if (E3.CharacterSettings.AutoPetDebug && MQ.Query<String>("${EverQuest.Server}") == "Project Laz") E3.Bots.Broadcast($"\amDebug: Magician: spiIni Dictionary with Laz hardcode: {allItems}");

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
        //__^Pulling Summoned Pet Items from E3.CharacterSettings^__

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

            //Providing List of Pet Item Identifiers to Requester
            var armPetList = new List<string> { "(.+) tells you, 'armpetlist'", "(.+) tells you, 'armpetlist'", "(.+) tells the group, 'armpetlist'", };
            EventProcessor.RegisterEvent("ArmPetList", armPetList, (x) =>
            {
                if (e3util.IsShuttingDown() || E3.IsPaused()) return;

                //___Process Request Requirements___
                if (E3.CharacterSettings.AutoPetDebug)  E3.Bots.Broadcast("\acPet List Request Event Started");
                _requester = x.match.Groups[1].ToString();
                string configuredIdentifiers = string.Join(", ", _spiMap.Values.SelectMany(list => list.Select(item => item.Identifier)).Distinct());
                //__^Process Request Requirements^__

                //Send Tell of Pet Item Identifiers to Requester
                MQ.Cmd($"/t {_requester} My current configured Pet Item Identifiers are: {configuredIdentifiers}.");
                if (E3.CharacterSettings.AutoPetDebug)  E3.Bots.Broadcast("\acPet List Request Event Finished");

            });

            var armPetEvents = new List<string> { "(.+) tells you, 'armpet'", "(.+) tells you, 'armpet (.+)'", "(.+) tells the group, 'armpet (.+)'", };
            EventProcessor.RegisterEvent("ArmPet", armPetEvents, (x) =>
            {
                if (e3util.IsShuttingDown() || E3.IsPaused()) return;

                //___Process Request Requirements___
                E3.Bots.Broadcast("\agPet Equipment Request Event Started");
                int xCount = x.match.Groups.Count;
                _requester = x.match.Groups[1].ToString();
                if (E3.CharacterSettings.AutoPetDebug) E3.Bots.Broadcast($"\amDebug: PER: x.match.Groups.Count: {xCount}");
                if (E3.CharacterSettings.AutoPetDebug) E3.Bots.Broadcast($"\amDebug: PER: Requestername: {_requester}");
                string[] identArray = x.match.Groups[2].ToString().Split(new char[] { ',', '|' });
                List<string> identifiers = identArray.ToList();
                //__^Process Request Requirements^__

                //___Identifier Checks___
                if (x.match.Groups.Count <= 2)
                {
                    E3.Bots.Broadcast($"\ayWARN: PER: {_requester} did not provide any identifiers. Ending {_requester} Request");
                    MQ.Cmd($"/t {_requester} You did not provide any identifiers. Message me armpetlist if you need a list of identifiers.");
                    return;
                }

                bool allIdentifiersExist = identifiers.All(id => _spiMap.Values.Any(spellItemList => spellItemList.Any(spellItem => spellItem.Identifier == id)));

                if (!allIdentifiersExist)
                {
                    string configuredIdentifiers = string.Join(", ", _spiMap.Values.SelectMany(list => list.Select(item => item.Identifier)).Distinct());
                    E3.Bots.Broadcast($"\ayWarn: PER: A requested identifiers from {_requester} does not exist. Ending {_requester} Request");
                    E3.Bots.Broadcast($"\amDebug: Requested Identifiers: {string.Join(", ", identifiers)} | Current Configed Indentifiers: {configuredIdentifiers}.");
                    MQ.Cmd($"/t {_requester} One or more identifiers you requested are currently not configured.My current configured identifiers are: {configuredIdentifiers}.");
                    return;
                }
                //__^Identifier Checks^__

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
                //__^PreRequester Checks^__

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
                //__^Requester Checks and Method Call^__

                MQ.Cmd($"/t {_requester} Arming Pet had finished, Happy Hunting");
                E3.Bots.Broadcast("\agArmpet Event process Finished");
                CleanUp();
                LazCleanUp();

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
            //__^MY PET^__

            //___BOT PETS___
            foreach (var botsettings in E3.CharacterSettings.PetWeapons)
            {
                var parts = botsettings.Split('/');
                var bot = parts[0];
                E3.Bots.Broadcast($"\ayDebug: Processing bot: {bot}");

                if (_spawns.TryByName(bot, out var ownerSpawn))
                {
                    if (e3util.IsShuttingDown() || E3.IsPaused()) return;

                    if (string.Equals(ownerSpawn.Name, E3.CurrentName)) continue;

                    var botName = ownerSpawn.CleanName;
                    var petId = ownerSpawn.PetID;
                    var petSpawn = _spawns.Get().FirstOrDefault(w => w.ID == petId);

                    if (petId < 0)
                    {
                        E3.Bots.Broadcast($"\arDebug: {botName} doesn't have a pet to summon!");
                        continue;
                    }

                    if (petSpawn == null)
                    {
                        E3.Bots.Broadcast($"\arDebug: {botName} doesn't have a pet to summon!");
                        continue;
                    }

                    if (petSpawn.Distance > 50)
                    {
                        E3.Bots.Broadcast($"\arDebug: {botName}'s pet is too far away!");
                        continue;
                    }

                    if (petSpawn.Level == 1)
                    {
                        E3.Bots.Broadcast($"\arDebug: {botName}'s pet is just a familiar!");
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
            //__^BOT PETS^__
            
            E3.Bots.Broadcast("\agArmPets finished.");
            CleanUp();
            LazCleanUp();
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

            // Get Pet ID
            //_spawns.TryByName(characterName, out var characterSpawn);
            //var PetId = characterSpawn.PetID;

            // Get the identifiers for the current character
            var identifiers = characterSetting.Split('/')[1].Split(new char[] { ',', '|' });
            if (E3.CharacterSettings.AutoPetDebug) E3.Bots.Broadcast($"\amDebug: IFC: Identifiers for {characterName}: {string.Join(", ", identifiers)}");


            // Cast spells for each identifier
            foreach (var identifier in identifiers)
            {
                bool isLazHardcodeIdentifier = _lazhardcodedItems.Contains(identifier);

                foreach (var entry in _spiMap)
                {
                    // Run LazCleanUp bag process if a non-lazhardcode identifier is detected
                    if (!isLazHardcodeIdentifier)
                    {
                        LazCleanUp();
                        isLazHardcodeIdentifier = true; // Set the flag to true to avoid running LazCleanUp multiple times
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
            E3.Bots.Broadcast($"\awDebug: IdentForCharacter Finshed for {characterName}.");
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
                bool isLazHardcodeIdentifier = _lazhardcodedItems.Contains(identifier);

                foreach (var entry in _spiMap)
                {
                    // Run LazCleanUp bag process if a non-lazhardcode identifier is detected
                    if (!isLazHardcodeIdentifier)
                    {
                        LazCleanUp();
                        isLazHardcodeIdentifier = true; // Set the flag to true to avoid running LazCleanUp multiple times
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

        private static void LazCleanUp()
        {
            if (E3.CharacterSettings.AutoPetDebug) E3.Bots.Broadcast($"\acDebug: LazCleanUp process Started.");

            foreach (var item in _lazhardcodedbags)
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

            if (E3.CharacterSettings.AutoPetDebug) E3.Bots.Broadcast($"\acDebug: LazCleanUp process Finished.");
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
                    E3.Bots.Broadcast("No free inventory space and auto pet weapons is on - toggling off so inventory space can be freed up");
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

            var dskGloveItem = "Glyphwielder's Ascendant Gloves of the Summoner";
            var hasDskGloves = MQ.Query<bool>($"${{FindItem[{dskGloveItem}]}}");
            if (hasDskGloves)
            {
                MQ.Cmd($"/useitem {dskGloveItem}");
            }

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
            var weaponsToEquip = new List<string>();
            _weaponMap.TryGetValue(weapons[0], out var primary);
            _weaponMap.TryGetValue(weapons[1], out var secondary);

            if (primary != null) weaponsToEquip.Add(primary);
            if (secondary != null) weaponsToEquip.Add(secondary);

            try
            {
                foreach (var weapon in weaponsToEquip)
                {
                    if (!CheckForWeapon(weapon)) return false;

                    if (Casting.TrueTarget(petId))
                    {
                        PickUpWeapon(weapon);
                        e3util.GiveItemOnCursorToTarget(false, false);
                    }
                    else
                    {
                        return false;
                    }
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

        private static bool CheckForWeapon(string weapon)
        {
            var found = MQ.Query<bool>($"${{FindItem[={weapon}]}}");

            if (!found)
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
            var bag = _armorOrHeirloomBag;
            while (MQ.Query<int>($"${{FindItemCount[={bag}]}}") > 0)
            {
                if (!DestroyIfEmpty(bag)) return false;
            }

            bag = "Huge Disenchanted Backpack";
            while (MQ.Query<int>($"${{FindItemCount[={bag}]}}") > 0)
            {
                if (!DestroyIfEmpty(bag)) return false;
            }

            bool DestroyIfEmpty(string containerName)
            {
                var itemSlot = MQ.Query<int>($"${{FindItem[={containerName}].ItemSlot}}");
                var itemSlot2 = MQ.Query<int>($"${{FindItem[={containerName}].ItemSlot2}}");
                // it's in another container
                if (itemSlot2 >= 0)
                {
                    MQ.Cmd($"/nomodkey /itemnotify in {_inventorySlotToPackMap[itemSlot]} {itemSlot + 1} leftmouseup");
                    if (!e3util.ValidateCursor(MQ.Query<int>($"${{FindItem[={containerName}].ID}}")))
                    {
                        E3.Bots.Broadcast($"\arUnexpected item on cursor when trying to destroy {containerName}");
                        return false;
                    }

                    MQ.Cmd("/destroy");
                    return true;
                }

                if (MQ.Query<int>($"${{InvSlot[{itemSlot}].Item.Items}}") == 0)
                {
                    MQ.Cmd($"/nomodkey /itemnotify {itemSlot} leftmouseup");
                    MQ.Delay(1000, "${Cursor.ID}");
                    if (!e3util.ValidateCursor(MQ.Query<int>($"${{FindItem[={containerName}].ID}}")))
                    {
                        E3.Bots.Broadcast($"\arUnexpected item on cursor when trying to destroy {containerName}");
                        return false;
                    }

                    MQ.Cmd("/destroy");
                    return true;
                }

                return false;
            }

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
    }
}
