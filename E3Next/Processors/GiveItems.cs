using E3Core.Settings;
using E3Core.Utility;
using MonoCore;
using System;
using IniParser;
using System.Collections.Generic;
using IniParser.Model;

namespace E3Core.Processors
{
    public class InventoryPosition
    {
        public int pack;
        public int slot;
        public InventoryPosition(int pack, int slot) {
            this.pack = pack;
            this.slot = slot;
        }

    }

    public class InventoryItem
    {
        public InventoryPosition position;
        public string item;

        public InventoryItem(InventoryPosition position, string item)
        {
            this.position = position;
            this.item = item;
        }
    }

    public static class GiveItems
    {
        private static IMQ MQ = E3.MQ; 
        private static string kSettingsFilename = "GiveItemsSettings.ini";
        private static Dictionary<string, List<string>> parsedGiveItems;
        private static List<string> allItemNames;
        private static Dictionary<string, List<InventoryItem>> itemsDict;
        private static string kLoggingPrefix = "[Give Items]";
        private static Logging _log = Core.logInstance;
        [SubSystemInit]
        public static void Init()
        {
            RegisterEvents();
        }
        private static void RegisterEvents()
        {
            EventProcessor.RegisterCommand("/giveItems", x => StartGiveItems(x));
        }

        private static void StartGiveItems(EventProcessor.CommandMatch x)
        {
            if (x.args.Count == 0)
            {
                AutoSortItems();
            }
            else if (x.args.Count == 1)
            {
                string category = x.args[0];
                if (category.Equals("bank"))
                {
                    BankItems();
                }
                else
                {
                    string botName = MQ.Query<string>("${Target.CleanName}");
                    if (botName.Length == 0)
                    {
                        MQ.Write($"{kLoggingPrefix} Expecting target if trying to trade a specific category.");
                    }
                    else
                    {
                        AutoSortItemsForCategory(category, botName);
                    }
                }
            }
            else
            {
                MQ.Write($"{kLoggingPrefix} Unexpected amount of arguments.");
            }
        }

        private static void BankItems()
        {
            ///TODO Implement banking
            MQ.Write($"{kLoggingPrefix} Starting to Bank Items");
            MQ.Write($"{kLoggingPrefix} Completed Banking Items");
        }

        #region Sorting
        private static void AutoSortItems()
        {
            LoadGiveItems();

            MQ.Cmd($"/nomodkey /keypress OPEN_INV_BAGS");

            foreach (string bot in E3.Bots.BotsConnected())
            {
                MQ.Write($"{kLoggingPrefix} Seeing if there's anything to trade with {bot}");
                List<string> botCategories = LoadBotINIForCategories(bot);
                List<string> categoriesToRemove = new List<string>();
                MQ.Write($"{kLoggingPrefix} Attempting to move to {bot}");

                foreach (string category in botCategories)
                {
                    if (parsedGiveItems.ContainsKey(category))
                    {
                        GiveItemsFromCategoryToTarget(category, bot);
                        categoriesToRemove.Add(category);
                    }
                }

                foreach (string removeCategory in categoriesToRemove)
                {
                    parsedGiveItems.Remove(removeCategory);
                }

            }
            MQ.Cmd($"/nomodkey /keypress INVENTORY");
            MQ.Write($"{kLoggingPrefix} Finished!");
        }

        private static void AutoSortItemsForCategory(string category, string botName)
        {
            LoadGiveItems(category);
            MQ.Write($"{kLoggingPrefix} Attempting to move to ${botName}");
            GiveItemsFromCategoryToTarget(category, botName);
        }

        #endregion

        #region Trading

        private static void GiveItemsFromCategoryToTarget(string category, string targetName)
        {
            /// Check distance
            if (E3.Spawns.TryByName(targetName, out var botSpawn))
            {
                if (botSpawn == null)
                {
                    return;
                }
                if (botSpawn.Distance > 250)
                {
                    MQ.Write($"{kLoggingPrefix} {targetName} is too far, skipping.");
                    return;
                }
            }
            MQ.Cmd($"/nomodkey /target ID {botSpawn.ID}");
            MQ.Delay(250);
            e3util.TryMoveToTarget();
            if (E3.Spawns.TryByName(targetName, out var botSpawnAfter))
            {
                if (botSpawnAfter == null)
                {
                    return;
                }
                if (botSpawnAfter.Distance > 20)
                {
                    MQ.Write($"{kLoggingPrefix} Could not move close enough to {targetName} to trade, skipping.");
                    return;
                }
            }
            int tradeCount = 0;
            List<string> itemsForCategory = parsedGiveItems[category];
            MQ.Write($"{kLoggingPrefix} Attempting to trade to {targetName} any items in {category}");
            foreach (string itemName in itemsForCategory) {
                if (!itemsDict.ContainsKey(itemName)) {
                    continue;
                }
                List<InventoryItem> itemList = itemsDict[itemName];
                foreach (InventoryItem item in itemList) {
                    InventoryPosition position = item.position;
                    MQ.Cmd($"/nomodkey /itemnotify in pack{position.pack} {position.slot} leftmouseup");
                    MQ.Write($"Giving {targetName} item {itemName}");
                    MQ.Delay(250);
                    if (MQ.Query<bool>("${Window[QuantityWnd].Open}"))
                    {
                        MQ.Cmd("/nomodkey /notify QuantityWnd QTYW_Accept_Button leftmouseup");
                        MQ.Delay(250);
                    }
                    MQ.Cmd("/click left target");
                    tradeCount += 1;
                    MQ.Delay(1000);
                    if (tradeCount >= 8)
                    {
                        tradeCount = 0;
                        MQ.Cmd($"/nomodkey /e3bc {targetName} //notify TradeWnd TRDW_Trade_button leftmouseup");
                        MQ.Cmd($"/nomodkey /notify Tradewnd TRDW_Trade_Button leftmouseup");
                        MQ.Delay(1000);
                    }

                }
            }
            if (tradeCount > 0)
            {
                tradeCount = 0;
                MQ.Cmd($"/nomodkey /e3bc {targetName} //notify TradeWnd TRDW_Trade_button leftmouseup");
                MQ.Cmd($"/nomodkey /notify Tradewnd TRDW_Trade_Button leftmouseup");

                MQ.Delay(1000);
            }

        }
        #endregion

        #region inventory loading

        /// This is made as a more generic inventory loading function as a potential way to abstract out the inventory
        /// from needing to query with MQ. Possibly something to be in e3Utils? Eh? Eh? *nudge nudge*
        private static List<InventoryItem> LoadInventory()
        {
            List<InventoryItem> itemsList = new List<InventoryItem>();
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
                            String bagItem = MQ.Query<String>($"${{Me.Inventory[pack{i}].Item[{e}]}}");
                            if (allItemNames.Contains(bagItem))
                            {
                                InventoryPosition position = new InventoryPosition(pack: i, slot: e);
                                InventoryItem item = new InventoryItem(position:position, item:bagItem);
                                itemsList.Add(item);
                            }
                        }
                    }
                }
            }
            return itemsList;
        } 

        /// Doing an extra pass through, but Big O tells me it's okay.
        /// I convert the inventory item to a dictionary with the itemName as the key, and the value for all items for that name.
        /// This deals with having multiple of the same item in your inventory for giving purposes.
        private static void HashInventory(List<InventoryItem> inventory)
        {
            itemsDict = new Dictionary<string, List<InventoryItem>>();
            foreach (InventoryItem item in inventory) {
                List<InventoryItem> itemsForKey;
                if (!itemsDict.TryGetValue(item.item, out itemsForKey)) {
                    itemsForKey = new List<InventoryItem>();
                }
                itemsForKey.Add(item);
                itemsDict.Add(item.item, itemsForKey);
            }
        }
        #endregion

        #region Reading Give Items from INI
        /// If we're passed in a category, only load that category. Otherwise, load all categories
        private static void LoadGiveItems(string category="")
        {
            parsedGiveItems = new Dictionary<string, List<string>>();
            IniData parsedData = ReadSettingsIni();

            allItemNames = new List<string>();
            var enumerator = parsedData.Sections.GetEnumerator();
            while (enumerator.MoveNext())
            {
                if (category.Length > 0) {
                    if (enumerator.Current.SectionName.Equals(category)) {
                        LoadSettingsINISection(enumerator.Current);
                        break;
                    }
                } else {
                    LoadSettingsINISection(enumerator.Current);
                }
            }
            List<InventoryItem> inventory = LoadInventory();
            HashInventory(inventory);
        }

        #endregion

        #region Bot INI
        /// Load the Bot INI to see what they want to accept
        private static List<string> LoadBotINIForCategories(string botName)
        {
            List<string> botCategories = new List<string>();
            string botFilename = BaseSettings.GetBoTFilePath($"{botName}_{E3.ServerName}.ini");
            FileIniDataParser fileIniData = e3util.CreateIniParser();
            /// Check file
            if (!System.IO.File.Exists(botFilename))
            {
                MQ.Write($"{kLoggingPrefix} Could not find INI file for {botName}.");
                return botCategories;
            }

            IniData botParsedData = fileIniData.ReadFile(botFilename);
            var sections = botParsedData.Sections;
            BaseSettings.LoadKeyData("GiveItems", "Category", botParsedData, botCategories);
            return botCategories;
        }
        #endregion

        #region Settings INI

        private static void LoadSettingsINISection(SectionData section)
        {
            if (parsedGiveItems.ContainsKey(section.SectionName))
            {
                MQ.Write($"{kLoggingPrefix} duplicate section key found: ${section.SectionName}");
                return;
            }

            List<string> keys = new List<string>();
            foreach (KeyData curKey in section.Keys)
            {
                keys.Add(curKey.KeyName);
                allItemNames.Add(curKey.KeyName);
            }
            parsedGiveItems.Add(section.SectionName, keys);
        }

        private static IniData ReadSettingsIni()
        {
            FileIniDataParser fileIniData = e3util.CreateIniParser();
            string filename = BaseSettings.GetSettingsFilePath(kSettingsFilename);
            IniData parsedData = new IniData();
            if (System.IO.File.Exists(filename))
            {
                parsedData = fileIniData.ReadFile(filename);
            }
            return parsedData;
        }

        #endregion

    }
}