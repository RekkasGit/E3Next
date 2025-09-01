using E3Core.Data;
using E3Core.Settings;
using E3Core.Server;
using E3Core.Processors;
using MonoCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace E3Core.Utility
{
    /// <summary>
    /// E3Next Inventory Scanner - Extracted from MQ2EZInv plugin
    /// Provides core inventory scanning functionality with pub/sub integration
    /// </summary>
    public static class InventoryScanner
    {
        private static IMQ MQ = E3.MQ;
        private static Logging _log = E3.Log;

        // Constants
        private const int MAX_BAG_SLOTS = 12;
        private const int MAX_BANK_SLOTS = 24;
        private const int BAGS_PER_CHUNK = 4; // 4 bags per topic for manageable message sizes

        // State tracking
        private static readonly object _scanLock = new object();

        // Reusable StringBuilder for efficient string building
        private static readonly StringBuilder _inventoryStringBuilder = new StringBuilder(2048);

        // Static initialization for registering commands
        static InventoryScanner()
        {
            RegisterCommands();
        }

        /// <summary>
        /// Checks if the inventory scanner is ready to perform a scan
        /// </summary>
        /// <returns>True if ready to scan, false otherwise</returns>
        private static bool IsReadyToScan()
        {
            try
            {
                // Don't scan if casting, in merchant window, or other blocking windows
                if (Casting.IsCasting() || e3util.IsActionBlockingWindowOpen())
                {
                    return false;
                }

                // Check if we're in a valid state (connected, not dead, etc.)
                if (!MQ.Query<bool>("${Me.ID}") || MQ.Query<bool>("${Me.Dead}"))
                {
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                MQ.Write($"Error checking if ready to scan: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Registers commands related to inventory scanning
        /// </summary>
        private static void RegisterCommands()
        {
            try
            {
                // Register a command to manually trigger inventory scan and publish
                EventProcessor.RegisterCommand("/scaninventory", (x) =>
                {
                    if (e3util.FilterMe(x)) return;

                    ScanAndPublishInventory();
                    MQ.Write("Inventory scan and publish completed.");
                });

                // Register a command to manually trigger inventory scan and return data
                EventProcessor.RegisterCommand("/getinventory", (x) =>
                {
                    if (e3util.FilterMe(x)) return;

                    var inventoryData = ScanAllInventory();
                    MQ.Write($"Scanned {((List<Dictionary<string, object>>)inventoryData["equipped"]).Count} equipped items.");
                    MQ.Write($"Scanned {((List<Dictionary<string, object>>)inventoryData["bags"]).Count} bag items.");
                    MQ.Write($"Scanned {((List<Dictionary<string, object>>)inventoryData["bank"]).Count} bank items.");
                });
            }
            catch (Exception ex)
            {
                MQ.Write($"Error registering inventory commands: {ex.Message}");
            }
        }

        /// <summary>
        /// Serializes a list of items to base64-encoded binary format (much more efficient than string)
        /// Format: Binary-packed item data encoded as base64
        /// </summary>
        /// <param name="items">Items to serialize</param>
        /// <returns>Base64-encoded binary representation of items</returns>
        private static string SerializeItemsToBinary(List<Dictionary<string, object>> items)
        {
            try
            {
                using (var memoryStream = new MemoryStream())
                {
                    using (var writer = new BinaryWriter(memoryStream, Encoding.UTF8))
                    {
                        // Write item count
                        writer.Write(items.Count);

                        // Write each item
                        foreach (var item in items)
                        {
                            // Write ID
                            writer.Write(item.ContainsKey("id") ? Convert.ToInt32(item["id"]) : 0);

                            // Write Icon
                            writer.Write(item.ContainsKey("icon") ? Convert.ToInt32(item["icon"]) : 0);

                            // Write Stack
                            writer.Write(item.ContainsKey("stack") ? Convert.ToInt32(item["stack"]) : 0);

                            // Write SlotID
                            writer.Write(item.ContainsKey("slotId") ? Convert.ToInt32(item["slotId"]) : -1);

                            // Write BagID
                            writer.Write(item.ContainsKey("bagId") ? Convert.ToInt32(item["bagId"]) : -1);

                            // Write Name
                            writer.Write(item.ContainsKey("name") ? item["name"].ToString() : "");

                            // Write ItemLink
                            writer.Write(item.ContainsKey("itemLink") ? item["itemLink"].ToString() : "");

                            // Write BagName (for bag items)
                            writer.Write(item.ContainsKey("bagname") ? item["bagname"].ToString() : "");

                            // Write NoDrop
                            writer.Write(item.ContainsKey("noDrop") ? Convert.ToBoolean(item["noDrop"]) : false);

                            // Write AC
                            writer.Write(item.ContainsKey("ac") ? Convert.ToInt32(item["ac"]) : 0);

                            // Write HP
                            writer.Write(item.ContainsKey("hp") ? Convert.ToInt32(item["hp"]) : 0);

                            // Write Mana
                            writer.Write(item.ContainsKey("mana") ? Convert.ToInt32(item["mana"]) : 0);

                            // Write Endurance
                            writer.Write(item.ContainsKey("endurance") ? Convert.ToInt32(item["endurance"]) : 0);

                            // Write ItemType
                            writer.Write(item.ContainsKey("itemtype") ? item["itemtype"].ToString() : "");

                            // Write Value
                            writer.Write(item.ContainsKey("value") ? Convert.ToInt32(item["value"]) : 0);

                            // Write Tribute
                            writer.Write(item.ContainsKey("tribute") ? Convert.ToInt32(item["tribute"]) : 0);

                            // Write augment information (up to 6 augments)
                            for (int augSlot = 1; augSlot <= 6; augSlot++)
                            {
                                // Write Aug Name
                                string augNameKey = $"aug{augSlot}Name";
                                writer.Write(item.ContainsKey(augNameKey) ? item[augNameKey].ToString() : "");

                                // Write Aug Link
                                string augLinkKey = $"aug{augSlot}Link";
                                writer.Write(item.ContainsKey(augLinkKey) ? item[augLinkKey].ToString() : "");

                                // Write Aug Icon
                                string augIconKey = $"aug{augSlot}Icon";
                                writer.Write(item.ContainsKey(augIconKey) ? Convert.ToInt32(item[augIconKey]) : 0);
                            }
                        }
                    }

                    // Convert to base64 string for efficient transmission
                    return Convert.ToBase64String(memoryStream.ToArray());
                }
            }
            catch (Exception ex)
            {
                MQ.Write($"Error serializing items to binary: {ex.Message}");
                return "";
            }
        }

        /// <summary>
        /// Scans all inventory (equipped, bags, bank) and returns structured data
        /// </summary>
        /// <returns>Dictionary containing all inventory data</returns>
        public static Dictionary<string, object> ScanAllInventory()
        {
            lock (_scanLock)
            {
                try
                {
                    var inventoryData = new Dictionary<string, object>
                    {
                        ["character"] = MQ.Query<string>("${Me.CleanName}") ?? "Unknown",
                        ["server"] = MQ.Query<string>("${MacroQuest.Server}") ?? "Unknown",
                        ["class"] = MQ.Query<string>("${Me.Class.ShortName}") ?? "UNK",
                        ["timestamp"] = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                        ["equipped"] = ScanEquippedItems(),
                        ["bags"] = ScanBagItems(),
                        ["bank"] = ScanBankItems()
                    };

                    _log.Write($"Inventory scan completed for {inventoryData["character"]}");
                    return inventoryData;
                }
                catch (Exception ex)
                {
                    MQ.Write($"Error during inventory scan: {ex.Message}");
                    return new Dictionary<string, object>();
                }
            }
        }

        /// <summary>
        /// Scans equipped items (slots 0-22)
        /// </summary>
        /// <returns>List of equipped items</returns>
        public static List<Dictionary<string, object>> ScanEquippedItems()
        {
            var equippedItems = new List<Dictionary<string, object>>();

            try
            {
                // Scan equipped slots (0-22)
                for (int slot = 0; slot <= 22; slot++)
                {
                    var itemExists = MQ.Query<bool>($"${{Me.Inventory[{slot}]}}");
                    if (!itemExists) continue;

                    var itemData = ScanSingleItem(slot, -1, -1);
                    if (itemData != null && itemData.ContainsKey("name"))
                    {
                        itemData["equipSlot"] = slot;
                        equippedItems.Add(itemData);
                    }
                }
            }
            catch (Exception ex)
            {
                MQ.Write($"Error scanning equipped items: {ex.Message}");
            }

            return equippedItems;
        }

        /// <summary>
        /// Scans bag items (pack1-pack12)
        /// </summary>
        /// <returns>List of bag items</returns>
        public static List<Dictionary<string, object>> ScanBagItems()
        {
            var bagItems = new List<Dictionary<string, object>>();

            try
            {
                // Scan pack slots (pack1-pack12)
                for (int bagId = 1; bagId <= MAX_BAG_SLOTS; bagId++)
                {
                    var bagExists = MQ.Query<bool>($"${{Me.Inventory[pack{bagId}]}}");
                    if (!bagExists) continue;

                    var containerSlots = MQ.Query<int>($"${{Me.Inventory[pack{bagId}].Container}}");

                    if (containerSlots > 0)
                    {
                        // It's a container - scan items inside
                        for (int slot = 1; slot <= containerSlots; slot++)
                        {
                            var itemExists = MQ.Query<bool>($"${{Me.Inventory[pack{bagId}].Item[{slot}]}}");
                            if (!itemExists) continue;

                            var itemData = ScanBagItem(bagId, slot);
                            if (itemData != null && itemData.ContainsKey("name"))
                            {
                                bagItems.Add(itemData);
                            }
                        }
                    }
                    else
                    {
                        // It's a single item in the pack slot
                        var itemData = ScanSingleItem(-1, bagId, -1);
                        if (itemData != null && itemData.ContainsKey("name"))
                        {
                            itemData["bagId"] = bagId;
                            itemData["slotId"] = 0; // Indicates it IS the bag
                            bagItems.Add(itemData);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MQ.Write($"Error scanning bag items: {ex.Message}");
            }

            return bagItems;
        }

        /// <summary>
        /// Scans bank items
        /// </summary>
        /// <returns>List of bank items</returns>
        public static List<Dictionary<string, object>> ScanBankItems()
        {
            var bankItems = new List<Dictionary<string, object>>();

            try
            {
                // Only scan bank if bank window is open
                var bankOpen = MQ.Query<bool>("${Window[BigBankWnd].Open}");
                if (!bankOpen) return bankItems;

                // Scan bank slots
                for (int bankSlot = 1; bankSlot <= MAX_BANK_SLOTS; bankSlot++)
                {
                    var itemExists = MQ.Query<bool>($"${{Me.Bank[{bankSlot}]}}");
                    if (!itemExists) continue;

                    var itemData = ScanBankItem(bankSlot);
                    if (itemData != null && itemData.ContainsKey("name"))
                    {
                        bankItems.Add(itemData);
                    }
                }
            }
            catch (Exception ex)
            {
                MQ.Write($"Error scanning bank items: {ex.Message}");
            }

            return bankItems;
        }

        /// <summary>
        /// Scans a single item from inventory slot
        /// </summary>
        /// <param name="slot">Inventory slot (-1 if not applicable)</param>
        /// <param name="bagId">Bag ID (-1 if not applicable)</param>
        /// <param name="bankSlot">Bank slot (-1 if not applicable)</param>
        /// <returns>Item data dictionary</returns>
        private static Dictionary<string, object> ScanSingleItem(int slot, int bagId, int bankSlot)
        {
            try
            {
                string queryPrefix;
                if (bankSlot > -1)
                    queryPrefix = $"${{Me.Bank[{bankSlot}]}}";
                else if (bagId > -1)
                    queryPrefix = $"${{Me.Inventory[pack{bagId}]}}";
                else
                    queryPrefix = $"${{Me.Inventory[{slot}]}}";

                var itemName = MQ.Query<string>($"{queryPrefix}.Name");
                if (string.IsNullOrEmpty(itemName) || itemName == "NULL") return null;

                var itemData = new Dictionary<string, object>
                {
                    ["name"] = itemName,
                    ["id"] = MQ.Query<int>($"{queryPrefix}.ID"),
                    ["icon"] = MQ.Query<int>($"{queryPrefix}.Icon"),
                    ["stack"] = MQ.Query<int>($"{queryPrefix}.Stack"),
                    ["noDrop"] = MQ.Query<bool>($"{queryPrefix}.NoDrop"),
                    ["itemLink"] = MQ.Query<string>($"{queryPrefix}.ItemLink") ?? "",
                    ["ac"] = MQ.Query<int>($"{queryPrefix}.AC"),
                    ["hp"] = MQ.Query<int>($"{queryPrefix}.HP"),
                    ["mana"] = MQ.Query<int>($"{queryPrefix}.Mana"),
                    ["endurance"] = MQ.Query<int>($"{queryPrefix}.Endurance"),
                    ["itemtype"] = MQ.Query<string>($"{queryPrefix}.Type") ?? "",
                    ["value"] = MQ.Query<int>($"{queryPrefix}.Value"),
                    ["tribute"] = MQ.Query<int>($"{queryPrefix}.Tribute")
                };

                // Add slot information
                if (slot > -1) itemData["slotId"] = slot;
                if (bagId > -1) itemData["bagId"] = bagId;
                if (bankSlot > -1) itemData["bankSlotId"] = bankSlot;

                // Scan augments
                ScanItemAugments(queryPrefix, itemData);

                return itemData;
            }
            catch (Exception ex)
            {
                MQ.Write($"Error scanning single item: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Scans an item from a bag
        /// </summary>
        /// <param name="bagId">Bag ID</param>
        /// <param name="slot">Slot within the bag</param>
        /// <returns>Item data dictionary</returns>
        private static Dictionary<string, object> ScanBagItem(int bagId, int slot)
        {
            try
            {
                var queryPrefix = $"${{Me.Inventory[pack{bagId}].Item[{slot}]}}";

                var itemName = MQ.Query<string>($"{queryPrefix}.Name");
                if (string.IsNullOrEmpty(itemName) || itemName == "NULL") return null;

                var itemData = new Dictionary<string, object>
                {
                    ["name"] = itemName,
                    ["id"] = MQ.Query<int>($"{queryPrefix}.ID"),
                    ["icon"] = MQ.Query<int>($"{queryPrefix}.Icon"),
                    ["stack"] = MQ.Query<int>($"{queryPrefix}.Stack"),
                    ["noDrop"] = MQ.Query<bool>($"{queryPrefix}.NoDrop"),
                    ["itemLink"] = MQ.Query<string>($"{queryPrefix}.ItemLink") ?? "",
                    ["bagId"] = bagId,
                    ["slotId"] = slot,
                    ["bagname"] = MQ.Query<string>($"${{Me.Inventory[pack{bagId}].Name}}") ?? "",
                    ["ac"] = MQ.Query<int>($"{queryPrefix}.AC"),
                    ["hp"] = MQ.Query<int>($"{queryPrefix}.HP"),
                    ["mana"] = MQ.Query<int>($"{queryPrefix}.Mana"),
                    ["endurance"] = MQ.Query<int>($"{queryPrefix}.Endurance"),
                    ["itemtype"] = MQ.Query<string>($"{queryPrefix}.Type") ?? "",
                    ["value"] = MQ.Query<int>($"{queryPrefix}.Value"),
                    ["tribute"] = MQ.Query<int>($"{queryPrefix}.Tribute")
                };

                // Scan augments
                ScanItemAugments(queryPrefix, itemData);

                return itemData;
            }
            catch (Exception ex)
            {
                MQ.Write($"Error scanning bag item: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Scans a bank item
        /// </summary>
        /// <param name="bankSlot">Bank slot number</param>
        /// <returns>Item data dictionary</returns>
        private static Dictionary<string, object> ScanBankItem(int bankSlot)
        {
            try
            {
                var queryPrefix = $"${{Me.Bank[{bankSlot}]}}";

                var itemName = MQ.Query<string>($"{queryPrefix}.Name");
                if (string.IsNullOrEmpty(itemName) || itemName == "NULL") return null;

                var itemData = new Dictionary<string, object>
                {
                    ["name"] = itemName,
                    ["id"] = MQ.Query<int>($"{queryPrefix}.ID"),
                    ["icon"] = MQ.Query<int>($"{queryPrefix}.Icon"),
                    ["stack"] = MQ.Query<int>($"{queryPrefix}.Stack"),
                    ["noDrop"] = MQ.Query<bool>($"{queryPrefix}.NoDrop"),
                    ["itemLink"] = MQ.Query<string>($"{queryPrefix}.ItemLink") ?? "",
                    ["bankSlotId"] = bankSlot,
                    ["ac"] = MQ.Query<int>($"{queryPrefix}.AC"),
                    ["hp"] = MQ.Query<int>($"{queryPrefix}.HP"),
                    ["mana"] = MQ.Query<int>($"{queryPrefix}.Mana"),
                    ["endurance"] = MQ.Query<int>($"{queryPrefix}.Endurance"),
                    ["itemtype"] = MQ.Query<string>($"{queryPrefix}.Type") ?? "",
                    ["value"] = MQ.Query<int>($"{queryPrefix}.Value"),
                    ["tribute"] = MQ.Query<int>($"{queryPrefix}.Tribute")
                };

                // Scan augments
                ScanItemAugments(queryPrefix, itemData);

                return itemData;
            }
            catch (Exception ex)
            {
                MQ.Write($"Error scanning bank item: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Scans augments for an item
        /// </summary>
        /// <param name="queryPrefix">MQ query prefix for the item</param>
        /// <param name="itemData">Item data dictionary to populate</param>
        private static void ScanItemAugments(string queryPrefix, Dictionary<string, object> itemData)
        {
            try
            {
                // Scan up to 6 augment slots
                for (int augSlot = 1; augSlot <= 6; augSlot++)
                {
                    var augName = MQ.Query<string>($"{queryPrefix}.Augment[{augSlot}].Name");
                    if (!string.IsNullOrEmpty(augName) && augName != "NULL")
                    {
                        itemData[$"aug{augSlot}Name"] = augName;
                        itemData[$"aug{augSlot}Link"] = MQ.Query<string>($"{queryPrefix}.Augment[{augSlot}].ItemLink") ?? "";
                        itemData[$"aug{augSlot}Icon"] = MQ.Query<int>($"{queryPrefix}.Augment[{augSlot}].Icon");
                    }
                }
            }
            catch (Exception ex)
            {
                MQ.Write($"Error scanning item augments: {ex.Message}");
            }
        }

        // ============================================================================
        // COMPACT SERIALIZATION AND PUBLISHING
        // ============================================================================

        /// <summary>
        /// Scans and publishes all inventory data to E3Next's pub/sub system using chunked topics
        /// </summary>
        public static void ScanAndPublishInventory()
        {
            if (!IsReadyToScan()) { return; }

            try
            {
                using (_log.Trace())
                {
                    _log.Write("Starting inventory scan and publish...");

                    // Scan and publish equipped items
                    var equippedItems = ScanEquippedItems();
                    PublishEquippedItems(equippedItems);

                    // Scan and publish bag items in chunks
                    var bagItems = ScanBagItems();
                    PublishBagItems(bagItems);

                    // Scan and publish bank items
                    var bankItems = ScanBankItems();
                    PublishBankItems(bankItems);

                    _log.Write($"Inventory publish completed - {equippedItems.Count} equipped, {bagItems.Count} bags, {bankItems.Count} bank");
                }
            }
            catch (Exception ex)
            {
                MQ.Write($"Error during inventory scan and publish: {ex.Message}");
            }
        }

        /// <summary>
        /// Publishes equipped items to the pub/sub system and named pipe
        /// </summary>
        /// <param name="equippedItems">List of equipped items</param>
        private static void PublishEquippedItems(List<Dictionary<string, object>> equippedItems)
        {
            try
            {
                var compactData = SerializeItemsToBinary(equippedItems);
                PubServer.AddTopicMessage("${Me.InventoryEquipped}", compactData);

                // Removed external IPC publishing of inventory data

                _log.Write($"Published {equippedItems.Count} equipped items ({compactData.Length} chars)");
            }
            catch (Exception ex)
            {
                MQ.Write($"Error publishing equipped items: {ex.Message}");
            }
        }

        /// <summary>
        /// Publishes bag items to the pub/sub system in chunks
        /// </summary>
        /// <param name="bagItems">List of bag items</param>
        private static void PublishBagItems(List<Dictionary<string, object>> bagItems)
        {
            try
            {
                // Group items by bag chunks (bags 1-4, 5-8, 9-12)
                for (int chunkStart = 1; chunkStart <= MAX_BAG_SLOTS; chunkStart += BAGS_PER_CHUNK)
                {
                    int chunkEnd = Math.Min(chunkStart + BAGS_PER_CHUNK - 1, MAX_BAG_SLOTS);

                    // Filter items for this chunk based on bagId
                    var chunkItems = bagItems.Where(item =>
                    {
                        if (item.ContainsKey("bagId") && int.TryParse(item["bagId"].ToString(), out int itemBagId))
                        {
                            return itemBagId >= chunkStart && itemBagId <= chunkEnd;
                        }
                        return false;
                    }).ToList();

                    // Serialize and publish this chunk
                    var compactData = SerializeItemsToBinary(chunkItems);
                    var topicName = $"${{Me.InventoryBags.{chunkStart}-{chunkEnd}}}";
                    PubServer.AddTopicMessage(topicName, compactData);

                    // Removed external IPC publishing of inventory data

                    _log.Write($"Published {chunkItems.Count} items for bags {chunkStart}-{chunkEnd} ({compactData.Length} chars)");
                }
            }
            catch (Exception ex)
            {
                MQ.Write($"Error publishing bag items: {ex.Message}");
            }
        }

        /// <summary>
        /// Publishes bank items to the pub/sub system and named pipe
        /// </summary>
        /// <param name="bankItems">List of bank items</param>
        private static void PublishBankItems(List<Dictionary<string, object>> bankItems)
        {
            try
            {
                var compactData = SerializeItemsToBinary(bankItems);
                PubServer.AddTopicMessage("${Me.InventoryBank}", compactData);

                // Removed external IPC publishing of inventory data

                _log.Write($"Published {bankItems.Count} bank items ({compactData.Length} chars)");
            }
            catch (Exception ex)
            {
                MQ.Write($"Error publishing bank items: {ex.Message}");
            }
        }
    }
}
