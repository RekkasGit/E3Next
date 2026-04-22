using E3Core.Processors;
using Google.Protobuf;
using MonoCore;
using System;
using System.Collections.Generic;

namespace E3Core.Data
{
    public static class InventoryDataCollector
    {
        private static readonly List<string> _invSlots = new List<string>()
        {
            "charm", "leftear", "head", "face", "rightear", "neck", "shoulder", "arms", "back",
            "leftwrist", "rightwrist", "ranged", "hands", "mainhand", "offhand", "leftfinger",
            "rightfinger", "chest", "legs", "feet", "waist", "powersource", "ammo"
        };

        public static List<InventoryItem> Capture(IMQ mq)
        {
            var results = new List<InventoryItem>();
            if (mq == null) return results;

            CaptureEquipped(mq, results);
            CaptureBags(mq, results);
            CaptureBank(mq, results);

            return results;
        }

        public static string SerializeForWire(IEnumerable<InventoryItem> items)
        {
            if (items == null) return string.Empty;

            var list = new InventoryDataList();
            foreach (var item in items)
            {
                list.Items.Add(item);
            }

            return Convert.ToBase64String(list.ToByteArray());
        }

        public static List<InventoryItem> DeserializeFromWire(string payload)
        {
            var results = new List<InventoryItem>();
            if (string.IsNullOrWhiteSpace(payload)) return results;

            try
            {
                var list = new InventoryDataList();
                list.MergeFrom(ByteString.FromBase64(payload));
                results.AddRange(list.Items);
            }
            catch
            {
                // silently ignore corrupt payloads
            }

            return results;
        }

        private static void CaptureEquipped(IMQ mq, List<InventoryItem> results)
        {
            for (int i = 0; i < _invSlots.Count; i++)
            {
                string name = SafeQueryString(mq, $"${{Me.Inventory[{i}]}}");
                if (string.IsNullOrWhiteSpace(name) || string.Equals(name, "NULL", StringComparison.OrdinalIgnoreCase))
                    continue;

                var item = new InventoryItem
                {
                    Name = name,
                    ItemId = SafeQueryInt(mq, $"${{Me.Inventory[{i}].ID}}"),
                    Icon = SafeQueryInt(mq, $"${{Me.Inventory[{i}].Icon}}"),
                    Quantity = Math.Max(1, SafeQueryInt(mq, $"${{Me.Inventory[{i}].Stack}}")),
                    Location = "Equipped",
                    SlotName = _invSlots[i],
                    SlotId = i,
                    SlotId2 = -1,
                    ItemLink = SafeQueryString(mq, $"${{Me.Inventory[{i}].ItemLink[CLICKABLE]}}"),
                    NoDrop = SafeQueryBool(mq, $"${{Me.Inventory[{i}].NoDrop}}")
                };

                CaptureAugments(mq, $"${{Me.Inventory[{i}]}}", $"${{InvSlot[{i}].Item}}", item);
                results.Add(item);
            }
        }

        private static void CaptureBags(IMQ mq, List<InventoryItem> results)
        {
            for (int i = 1; i <= 10; i++)
            {
                string bagName = SafeQueryString(mq, $"${{Me.Inventory[pack{i}]}}");
                if (string.IsNullOrWhiteSpace(bagName) || string.Equals(bagName, "NULL", StringComparison.OrdinalIgnoreCase))
                    continue;

                int containerSlots = SafeQueryInt(mq, $"${{Me.Inventory[pack{i}].Container}}");
                int bagSlotId = 22 + i;

                if (containerSlots > 0)
                {
                    for (int e = 1; e <= containerSlots; e++)
                    {
                        string itemName = SafeQueryString(mq, $"${{Me.Inventory[pack{i}].Item[{e}]}}");
                        if (string.IsNullOrWhiteSpace(itemName) || string.Equals(itemName, "NULL", StringComparison.OrdinalIgnoreCase))
                            continue;

                        var item = new InventoryItem
                        {
                            Name = itemName,
                            ItemId = SafeQueryInt(mq, $"${{Me.Inventory[pack{i}].Item[{e}].ID}}"),
                            Icon = SafeQueryInt(mq, $"${{Me.Inventory[pack{i}].Item[{e}].Icon}}"),
                            Quantity = Math.Max(1, SafeQueryInt(mq, $"${{Me.Inventory[pack{i}].Item[{e}].Stack}}")),
                            Location = "Bag",
                            SlotName = $"pack{i} slot{e}",
                            SlotId = bagSlotId,
                            SlotId2 = e,
                            ItemLink = SafeQueryString(mq, $"${{Me.Inventory[pack{i}].Item[{e}].ItemLink[CLICKABLE]}}"),
                            NoDrop = SafeQueryBool(mq, $"${{Me.Inventory[pack{i}].Item[{e}].NoDrop}}")
                        };

                        CaptureAugments(mq, $"${{Me.Inventory[pack{i}].Item[{e}]}}", $"${{Me.Inventory[pack{i}].Item[{e}]}}", item);
                        results.Add(item);
                    }
                }
                else
                {
                    // Single item in the pack slot (not a container)
                    var item = new InventoryItem
                    {
                        Name = bagName,
                        ItemId = SafeQueryInt(mq, $"${{Me.Inventory[pack{i}].ID}}"),
                        Icon = SafeQueryInt(mq, $"${{Me.Inventory[pack{i}].Icon}}"),
                        Quantity = Math.Max(1, SafeQueryInt(mq, $"${{Me.Inventory[pack{i}].Stack}}")),
                        Location = "Bag",
                        SlotName = $"pack{i}",
                        SlotId = bagSlotId,
                        SlotId2 = -1,
                        ItemLink = SafeQueryString(mq, $"${{Me.Inventory[pack{i}].ItemLink[CLICKABLE]}}"),
                        NoDrop = SafeQueryBool(mq, $"${{Me.Inventory[pack{i}].NoDrop}}")
                    };

                    CaptureAugments(mq, $"${{Me.Inventory[pack{i}]}}", $"${{Me.Inventory[pack{i}]}}", item);
                    results.Add(item);
                }
            }
        }

        private static void CaptureBank(IMQ mq, List<InventoryItem> results)
        {
            for (int i = 1; i <= 26; i++)
            {
                string bankSlotItem = SafeQueryString(mq, $"${{Me.Bank[{i}].Name}}");
                if (string.IsNullOrWhiteSpace(bankSlotItem) || string.Equals(bankSlotItem, "NULL", StringComparison.OrdinalIgnoreCase))
                    continue;

                int containerSlots = SafeQueryInt(mq, $"${{Me.Bank[{i}].Container}}");

                if (containerSlots > 0)
                {
                    for (int e = 1; e <= containerSlots; e++)
                    {
                        string itemName = SafeQueryString(mq, $"${{Me.Bank[{i}].Item[{e}].Name}}");
                        if (string.IsNullOrWhiteSpace(itemName) || string.Equals(itemName, "NULL", StringComparison.OrdinalIgnoreCase))
                            continue;

                        var item = new InventoryItem
                        {
                            Name = itemName,
                            ItemId = SafeQueryInt(mq, $"${{Me.Bank[{i}].Item[{e}].ID}}"),
                            Icon = SafeQueryInt(mq, $"${{Me.Bank[{i}].Item[{e}].Icon}}"),
                            Quantity = Math.Max(1, SafeQueryInt(mq, $"${{Me.Bank[{i}].Item[{e}].Stack}}")),
                            Location = "Bank",
                            SlotName = $"bank{i} slot{e}",
                            SlotId = i,
                            SlotId2 = e,
                            ItemLink = SafeQueryString(mq, $"${{Me.Bank[{i}].Item[{e}].ItemLink[CLICKABLE]}}"),
                            NoDrop = SafeQueryBool(mq, $"${{Me.Bank[{i}].Item[{e}].NoDrop}}")
                        };

                        CaptureAugments(mq, $"${{Me.Bank[{i}].Item[{e}]}}", $"${{Me.Bank[{i}].Item[{e}]}}", item);
                        results.Add(item);
                    }
                }
                else
                {
                    var item = new InventoryItem
                    {
                        Name = bankSlotItem,
                        ItemId = SafeQueryInt(mq, $"${{Me.Bank[{i}].ID}}"),
                        Icon = SafeQueryInt(mq, $"${{Me.Bank[{i}].Icon}}"),
                        Quantity = Math.Max(1, SafeQueryInt(mq, $"${{Me.Bank[{i}].Stack}}")),
                        Location = "Bank",
                        SlotName = $"bank{i}",
                        SlotId = i,
                        SlotId2 = -1,
                        ItemLink = SafeQueryString(mq, $"${{Me.Bank[{i}].ItemLink[CLICKABLE]}}"),
                        NoDrop = SafeQueryBool(mq, $"${{Me.Bank[{i}].NoDrop}}")
                    };

                    CaptureAugments(mq, $"${{Me.Bank[{i}]}}", $"${{Me.Bank[{i}]}}", item);
                    results.Add(item);
                }
            }
        }

        private static void CaptureAugments(IMQ mq, string itemTloPrefix, string augTloPrefix, InventoryItem target)
        {
            int augCount = SafeQueryInt(mq, $"{itemTloPrefix}.Augs");
            if (augCount <= 0) return;

            for (int a = 1; a <= 6; a++)
            {
                string augName = SafeQueryString(mq, $"{itemTloPrefix}.AugSlot[{a}].Name");
                if (string.IsNullOrWhiteSpace(augName) || string.Equals(augName, "NULL", StringComparison.OrdinalIgnoreCase))
                    continue;

                string augLink = SafeQueryString(mq, $"{augTloPrefix}.AugSlot[{a}].Item.ItemLink[CLICKABLE]");
                target.Augs.Add(new InventoryAugment
                {
                    Slot = a,
                    Name = augName,
                    ItemLink = augLink
                });
            }
        }

        private static string SafeQueryString(IMQ mq, string query)
        {
            try
            {
                return mq.Query<string>(query) ?? string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        private static int SafeQueryInt(IMQ mq, string query)
        {
            try
            {
                return mq.Query<int>(query);
            }
            catch
            {
                return 0;
            }
        }

        private static bool SafeQueryBool(IMQ mq, string query)
        {
            try
            {
                return mq.Query<bool>(query);
            }
            catch
            {
                return false;
            }
        }
    }
}
