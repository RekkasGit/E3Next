using E3Core.Utility;
using IniParser;
using IniParser.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using E3Core.Processors;
using MonoCore;

namespace E3Core.Settings.FeatureSettings
{
    public class SymbolItemsDataFile : BaseSettings
    {
        private static readonly string _fileName = "Lazarus_SymbolItems.ini";

        // Planar Symbols
        private static readonly HashSet<string> _planarSymbolItems = new HashSet<string>
        {
            // p3 golems
            "Earring of Xaoth Kor", "Ethereal Destroyer", "Faceguard of Frenzy", "Fiery Crystal Guard",
            "Mask of Strategic Insight", "Pauldrons of Purity", "Timeless Coral Greatsword",

            // saryn
            "Cap of Flowing Time", "Edge of Eternity", "Girdle of Intense Durability", "Gloves of the Unseen",
            "Ring of Evasion", "Runewarded Belt", "Shroud of Provocation", "Symbol of the Planemasters",
            "Time's Antithesis", "Veil of Lost Hopes",

            // tz
            "Amulet of Crystal Dreams", "Band of Prismatic Focus", "Bracer of Precision", "Circlet of Flowing Time",
            "Cloak of the Falling Skies", "Hopebringer", "Mantle of Deadly Precision", "Serpent of Vindication",
            "Tactician's Shield", "Winged Storm Boots",

            // terris
            "Armguards of the Brute", "Cape of Endless Torment", "Coif of Flowing Time", "Cudgel of Venomous Hatred",
            "Earring of Corporeal Essence", "Hammer of Hours", "Orb of Clinging Death",
            "Talisman of Tainted Energy", "Vanazir, Dreamer's Despair",

            // vz
            "Bow of the Tempest", "Cord of Potential", "Earring of Temporal Solstice", "Globe of Mystical Protection",
            "Hammer of Holy Vengeance", "Helm of Flowing Time", "Shinai of the Ancients", "Shoes of Fleeting Fury",
            "Temporal Chainmail Sleeves", "Wand of Temporal Power",

            // bert
            "Belt of Temporal Bindings", "Boots of Despair", "Celestial Cloak", "Collar of Catastrophe",
            "Eye of Dreams", "Greatblade of Chaos", "Leggings of Furious Might", "Pulsing Onyx Ring",
            "Symbol of Ancient Summoning", "Timespinner, Blade of the Hunter", "Veil of the Inferno",

            // cazic
            "Belt of Tidal Energy", "Cloak of Retribution", "Earring of Unseen Horrors", "Greaves of Furious Might",
            "Mask of Simplicity", "Padded Tigerskin Gloves", "Staff of Transcendence", "Timestone Adorned Ring",
            "Wand of Impenetrable Force", "Wristband of Echoed Thoughts", "Zealot's Spiked Bracer",

            // inny
            "Barrier of Freezing Winds", "Bracer of Timeless Rage", "Earring of Celestial Energy", "Girdle of Stability",
            "Gloves of Airy Mists", "Jagged Timeforged Blade", "Mantle of Pure Spirit", "Necklace of Eternal Visions",
            "Serrated Dart of Energy", "Shroud of Survival", "Songblade of the Eternal",

            // rz
            "Band of Primordial Energy", "Darkblade of the Warlord", "Greatstaff of Power", "Pants of Furious Might",
            "Pauldrons of Devastation", "Platinum Cloak of War", "Ring of Thunderous Forces", "Sandals of Empowerment",
            "Shield of Strife", "Ton Po's Mystical Pouch", "Visor of the Berserker",

            // quarm
            "Bracer of the Inferno", "Cord of Temporal Weavings", "Earring of Influxed Gravity", "Earthen Bracer of Fortitude",
            "Ethereal Silk Leggings", "Hammer of the Timeweaver", "Prismatic Ring of Resistance", "Shawl of Eternal Forces",
            "Shroud of Eternity", "Silver Hoop of Speed", "Spool of Woven Time", "Stone of Flowing Time",
            "Talisman of the Elements", "Whorl of Unnatural Forces", "Wristband of Icy Vengeance", 
            
            // bps
            "Timeless Breastplate Mold", "Timeless Chain Tunic Pattern", "Timeless Leather Tunic Pattern", 
            "Timeless Silk Robe Pattern"
        };

        // Taelosian Symbols
        private static readonly HashSet<string> _taelosianSymbolItems = new HashSet<string>
        {
            // txevu - ukun bloodfeaster
            "Ukun-Hide Armplates of Mortification", "Carved Bone Gauntlets", "Cloak of the Penumbra",
            "Golden Half Mask of Convalescence", "Hardened Bone Spike", "Skullcap of Contemplation",
            "Woven Chain Boots of Strife",

            // txevu - Ixt Hsek Syat
            "Chain Wraps of the Dark Master", "Earring of Incessant Conflict", "Forlorn Mantle of Shadows",
            "Halberd of Endless Pain", "Shadowy Coif of Condemnation", "Sleeves of Cognitive Resonance",
            "Stained Fur Mask", "Steel Boots of the Slayer",

            // txevu - Ancient Cragbeast Matriarch
            "Azure Trinket of Despair", "Flayed-Skin Spiked Boots", "Globe of Dancing Light", "Hardened Scale Vambraces",
            "Headband of the Endless Night", "Lizard Skin Wardrums", "Shroud of Pandemonium",
            "Silken Gloves of the Chaos",

            // txevu - Ikaav Nysf Lleiv
            "Armguards of Envy", "Bloodfire Cabochon", "Crown of the Forsaken", "Earring of the Starless Night",
            "Scepter of Forbidden Knowledge", "Silken Slippers of Discordant Magic", "Staff of Shattered Dreams",
            "Suede Gloves of Creation",

            // txevu - High Priest Nkosi Bakari
            "Barrier of Serenity", "Bow of the Whispering", "Bulwark of Living Stone", "Caduceus of Retribution",
            "Jade Effigy of Trushar", "Lute of False Worship", "Mask of Eternity", "Ring of Celestial Harmony",
            "Staff of Revealed Secrets", "Verge of the Mindless Servant",

            // txevu - Zun`Muram Tkarish Zyk
            "Aegis of Discord", "Band of Solid Shadow", "Blackstone Figurine", "Cape of Woven Steel",
            "Edge of Chaos", "Gem-Studded Band of Struggle", "Gemstone of Dark Flame", "Kaftan of Embroidered Light",
            "Longsword of Execration", "Muramite's Heavy Shackles", "Shroud of Ceaseless Might",
            "Spiked Steel Baton", "Wristband of Chaotic Warfare",

            // tacvi - Pixtt Xxeric Kex
            "Bracer of Grievous Harm", "Glinting Onyx of Might", "Glyphed Sandstone of Idealism",
            "Ragestone of Hateful Thoughts", "Shimmering Granite", "Wristguard of Chaotic Essence",
            "Xxeric's Battleworn Bracer", "Xxeric's Warbraid",
            
            // tacvi - Pixtt Kretv Krakxt
            "Bulwark of Lost Souls", "Death's Head Mace", "Earring of Pain Deliverance",
            "Golden Idol of Destruction", "Ring of Organic Darkness", "Sleeves of Malefic Rapture",
            "Vambraces of Eternal Twilight",

            // tacvi - Pixtt Riel Tavas
            "Aegis of Midnight", "Armband of Writhing Shadow", "Armguards of Insidious Corruption",
            "Mask of the Void", "Ring of the Serpent", "Ruby of Determined Assault", "Tome of Discordant Magic",
            
            // tacvi - Zun`Muram Kvxe Pirik
            "Bloodstone Blade of the Zun'Muram", "Brutish Blade of Balance", "Gauntlets of Malicious Intent",
            "Girdle of the Zun'Muram", "Luxurious Satin Slippers", "Pauldron of Dark Auspices",
            "Scepter of Incantations", "Weighted Hammer of Conviction",

            // tacvi - Zun`Muram Mordl Delt
            "Blade of Natural Turmoil", "Cloak of Nightmarish Visions", "Dagger of Evil Summons",
            "Jagged Axe of Uncontrolled Rage", "Nightmarish Boots of Conflict", "Runed Gauntlets of the Void",
            "Shroud of the Legion", "Zun'Muram's Spear of Doom",

            // tacvi - Zun`Muram Shaldn Boc
            "Gloves of Wicked Ambition", "Hammer of Delusions", "Kelp-Covered Hammer", "Loop of Entropic Hues",
            "Mantle of Corruption", "Mindreaper Club", "Supple Slippers of the Stargazer",
            "Zun'Muram's Scepter of Chaos",

            // tacvi - Zun`Muram Yihst Vor
            "Boots of Captive Screams", "Dagger of Death", "Deathblade of the Zun'Muram",
            "Discordant Dagger of Night", "Gloves of Coalesced Flame", "Pendant of Discord",
            "Rapier of Somber Notes", "Xxeric's Matted-Fur Mask",
            
            // tacvi - Tunat`Muram Cuu Vauax
            "Blade Warstone", "Dark Tunic of the Enslavers", "Drape of the Merciless Slaver",
            "Greaves of the Dark Ritualist", "Greaves of the Tunat'Muram", "Jagged Glowing Prism",
            "Lightning Prism of Swordplay", "Merciless Enslaver's Britches", "Solid Stone of the Iron Fist",
            "Tunat'Muram's Bloodied Greaves", "Tunat'Muram's Chainmail of Pain",
            "Tunat'Muram's Chestplate of Agony", "Worked Granite of Sundering"
        };

        public static Dictionary<string, string> PlanarSymbols = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        public static Dictionary<string, string> TaelosianSymbols = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        [SubSystemInit]
        public static void Init()
        {
            if (!(e3util.IsEQEMU() && E3.ServerName == "Lazarus")) return;

            RegisterEvents();

            try
            {
                LoadData();
            }
            catch (Exception ex)
            {
                MQ.Write("Exception loading SymbolItems data file. Error:" + ex.Message + " stack:" + ex.StackTrace);
            }
        }
        
        private static void RegisterEvents()
        {
            // Planar Symbol turn in
            EventProcessor.RegisterCommand("/e3popturnin", (x) =>
            {
                if (MQ.Query<bool>("${Zone.ShortName.NotEqual[poknowledge]}"))
                {
                    E3.Bots.Broadcast($"\arYou must be in the Plane of Knowledge to use this.");
                    return;
                }

                // lets reload the file to make sure we got manual changes to file
                LoadData();

                // navigate to Klorg
                MQ.Cmd("/nav spawn klorg | distance=10");
                MQ.Delay(30000, "${Spawn[klorg].Distance3D} <= 15");
                AutoSymbols("klorg");
            });

            // Taelosian Symbol turn In
            EventProcessor.RegisterCommand("/e3godturnin", (x) =>
            {
                if (MQ.Query<bool>("${Zone.ShortName.NotEqual[poknowledge]}"))
                {
                    E3.Bots.Broadcast($"\arYou must be in the Plane of Knowledge to use this.");
                    return;
                }
                
                // lets reload the file to make sure we got manual changes to file
                LoadData();

                // navigate to Zenma
                MQ.Cmd("/nav spawn zenma | distance=10");
                MQ.Delay(30000, "${Spawn[zenma].Distance3D} <= 15");
                AutoSymbols("zenma");
            });
        }

        public static void LoadData()
        {
            string fileNameFullPath = GetSettingsFilePath(_fileName);

            if (!File.Exists(fileNameFullPath))
            {
                if (!Directory.Exists(_configFolder + _settingsFolder))
                {
                    Directory.CreateDirectory(_configFolder + _settingsFolder);
                }
                //file straight up doesn't exist, lets create it
                using (FileStream fs = File.Create(fileNameFullPath))
                {

                }
            }
            else
            {
                //File already exists, may need to merge in new items lets check

                FileIniDataParser fileIniData = e3util.CreateIniParser();
                _log.Write($"Reading Symbol Items Settings: {fileNameFullPath}");
                var parsedData = fileIniData.ReadFile(fileNameFullPath);

                // Planar Symbols
                if (parsedData.Sections.ContainsSection("Planar Symbols"))
                {
                    var section = parsedData["Planar Symbols"];
                    foreach (var keyData in section)
                    {
                        var value = keyData.Value;
                        if (value != "Trade" && value != "Keep")
                        {
                            value = "Trade";
                        }
                        PlanarSymbols[keyData.KeyName] = value;
                    }
                }

                // Taelosian Symbols
                if (parsedData.Sections.ContainsSection("Taelosian Symbols"))
                {
                    var section = parsedData["Taelosian Symbols"];
                    foreach (var keyData in section)
                    {
                        var value = keyData.Value;
                        if (value != "Trade" && value != "Keep")
                        {
                            value = "Trade";
                        }
                        TaelosianSymbols[keyData.KeyName] = value;
                    }
                }
            }

            // make sure all valid items from the hash sets are included
            PopulateDefaultData();
            SaveData();
        }

        private static void PopulateDefaultData()
        {
            foreach (var item in _planarSymbolItems)
            {
                if (!PlanarSymbols.ContainsKey(item))
                {
                    PlanarSymbols[item] = "Trade";
                }
            }

            foreach (var item in _taelosianSymbolItems)
            {
                if (!TaelosianSymbols.ContainsKey(item))
                {
                    TaelosianSymbols[item] = "Trade";
                }
            }
        }

        public static void SaveData()
        {
            IniParser.FileIniDataParser parser = e3util.CreateIniParser();
            IniData newFile = new IniData();

            // Planar Symbols
            newFile.Sections.AddSection("Planar Symbols");
            var planarSection = newFile.Sections.GetSectionData("Planar Symbols");
            foreach (var kvp in PlanarSymbols.OrderBy(x => x.Key))
            {
                planarSection.Keys.AddKey(kvp.Key, kvp.Value);
            }

            // Taelosian Symbols
            newFile.Sections.AddSection("Taelosian Symbols");
            var taelosianSection = newFile.Sections.GetSectionData("Taelosian Symbols");
            foreach (var kvp in TaelosianSymbols.OrderBy(x => x.Key))
            {
                taelosianSection.Keys.AddKey(kvp.Key, kvp.Value);
            }

            string fileNameFullPath = GetSettingsFilePath(_fileName);
            parser.WriteFile(fileNameFullPath, newFile);
        }
        
        // Handles the automatic symbol conversion
        private static void AutoSymbols(string npc)
        {
            int totalItemsTraded = 0;

            for (Int32 i = 1; i <= 10; i++)
            {
                bool SlotExists = MQ.Query<bool>($"${{Me.Inventory[pack{i}]}}");
                if (SlotExists)
                {
                    Int32 ContainerSlots = MQ.Query<Int32>($"${{Me.Inventory[pack{i}].Container}}");
                    if (ContainerSlots > 0)
                    {
                        for (Int32 e = 1; e <= ContainerSlots; e++)
                        {
                            String itemName = MQ.Query<String>($"${{Me.Inventory[pack{i}].Item[{e}]}}");
                            if (itemName == "NULL")
                            {
                                continue;
                            }

                            bool itemNoTrade = MQ.Query<bool>($"${{Me.Inventory[pack{i}].Item[{e}].NoTrade}}");
                            bool shouldTrade = false;

                            if (npc == "klorg")
                            {
                                bool isPlanarSymbol = PlanarSymbols.TryGetValue(itemName, out string planarSymbolValue);
                                shouldTrade = isPlanarSymbol && planarSymbolValue == "Trade";
                            }
                            else
                            {
                                bool isTaelosianSymbol = TaelosianSymbols.TryGetValue(itemName, out string taelosianSymbolValue);
                                shouldTrade = isTaelosianSymbol && taelosianSymbolValue == "Trade";
                            }
                            
                            if (shouldTrade && !itemNoTrade)
                            {
                                MQ.Cmd($"/nomodkey /itemnotify in pack{i} {e} leftmouseup", 500);
                                MQ.Delay(1000, $"${{Cursor.Name.Equal[{itemName}]}}");
                                MQ.Cmd($"/tar {npc}");
                                MQ.Delay(100, "${Target.ID}");
                                MQ.Cmd("/click left target");
                                MQ.Delay(3000, "${Window[GiveWnd].Open}");
                                MQ.Cmd($"/nomodkey /notify GiveWnd GVW_Give_Button leftmouseup");
                                MQ.Delay(1000, "!${Window[GiveWnd].Open}");
                                MQ.Delay(500);
                                MQ.Cmd("/autoinventory");
                                MQ.Delay(500);

                                totalItemsTraded++;
                            }
                        }
                    }
                }
            }

            string itemsTradedMsg = totalItemsTraded == 1 ? "1 item traded." : $"{totalItemsTraded} items traded.";
            E3.Bots.Broadcast($"\agItems to Symbols completed: {itemsTradedMsg}");
        }
    }
}
