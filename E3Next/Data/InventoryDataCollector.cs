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

        public static InventoryDataList Capture(IMQ mq)
        {
            var result = new InventoryDataList();
            if (mq == null) return result;

            CaptureEquipped(mq, result.Items);
            CaptureBags(mq, result.Items, result.Bags);
            CaptureBank(mq, result.Items);

            return result;
        }

        public static string SerializeForWire(InventoryDataList data)
        {
            if (data == null) return string.Empty;
            return Convert.ToBase64String(data.ToByteArray());
        }

        public static InventoryDataList DeserializeFromWire(string payload)
        {
            var result = new InventoryDataList();
            if (string.IsNullOrWhiteSpace(payload)) return result;

            try
            {
                result.MergeFrom(ByteString.FromBase64(payload));
            }
            catch
            {
                // silently ignore corrupt payloads
            }

            return result;
        }

        private static void CaptureEquipped(IMQ mq, IList<InventoryItem> results)
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

                CaptureAugments(mq, $"${{Me.Inventory[{i}]}}", item);
                CaptureAugmentSlots(mq, $"${{Me.Inventory[{i}]}}", item);
                CaptureStats(mq, $"${{Me.Inventory[{i}]}}", item);
                results.Add(item);
            }
        }

        private static void CaptureBags(IMQ mq, IList<InventoryItem> results, IList<BagInfo> bags)
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
                    bags.Add(new BagInfo
                    {
                        SlotId = bagSlotId,
                        Name = bagName,
                        Icon = SafeQueryInt(mq, $"${{Me.Inventory[pack{i}].Icon}}"),
                        Capacity = containerSlots,
                        ItemLink = SafeQueryString(mq, $"${{Me.Inventory[pack{i}].ItemLink[CLICKABLE]}}")
                    });

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

                        CaptureAugments(mq, $"${{Me.Inventory[pack{i}].Item[{e}]}}", item);
                        CaptureAugmentSlots(mq, $"${{Me.Inventory[pack{i}].Item[{e}]}}", item);
                        CaptureStats(mq, $"${{Me.Inventory[pack{i}].Item[{e}]}}", item);
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

                    CaptureAugments(mq, $"${{Me.Inventory[pack{i}]}}", item);
                    CaptureAugmentSlots(mq, $"${{Me.Inventory[pack{i}]}}", item);
                    CaptureStats(mq, $"${{Me.Inventory[pack{i}]}}", item);
                    results.Add(item);
                }
            }
        }

        private static void CaptureBank(IMQ mq, IList<InventoryItem> results)
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

                        CaptureAugments(mq, $"${{Me.Bank[{i}].Item[{e}]}}", item);
                        CaptureAugmentSlots(mq, $"${{Me.Bank[{i}].Item[{e}]}}", item);
                        CaptureStats(mq, $"${{Me.Bank[{i}].Item[{e}]}}", item);
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

                    CaptureAugments(mq, $"${{Me.Bank[{i}]}}", item);
                    CaptureAugmentSlots(mq, $"${{Me.Bank[{i}]}}", item);
                    CaptureStats(mq, $"${{Me.Bank[{i}]}}", item);
                    results.Add(item);
                }
            }
        }

        private static void CaptureAugments(IMQ mq, string itemTloPrefix, InventoryItem target)
        {
            int insertPos = itemTloPrefix.LastIndexOf('}');
            string prefix = insertPos >= 0 ? itemTloPrefix.Substring(0, insertPos) : itemTloPrefix;
            string suffix = insertPos >= 0 ? itemTloPrefix.Substring(insertPos) : "";

            int augCount = SafeQueryInt(mq, $"{prefix}.Augs{suffix}");
            if (augCount <= 0) return;

            for (int a = 1; a <= 6; a++)
            {
                string augName = SafeQueryString(mq, $"{prefix}.AugSlot[{a}].Name{suffix}");
                if (string.IsNullOrWhiteSpace(augName) || string.Equals(augName, "NULL", StringComparison.OrdinalIgnoreCase))
                    continue;

                string augLink = SafeQueryString(mq, $"{prefix}.AugSlot[{a}].Item.ItemLink[CLICKABLE]{suffix}");
                int augIcon = SafeQueryInt(mq, $"{prefix}.AugSlot[{a}].Item.Icon{suffix}");
                int augId = SafeQueryInt(mq, $"{prefix}.AugSlot[{a}].Item.ID{suffix}");
                int augAc = SafeQueryInt(mq, $"{prefix}.AugSlot[{a}].Item.AC{suffix}");
                int augHp = SafeQueryInt(mq, $"{prefix}.AugSlot[{a}].Item.HP{suffix}");
                int augMana = SafeQueryInt(mq, $"{prefix}.AugSlot[{a}].Item.Mana{suffix}");
                int augType = SafeQueryInt(mq, $"{prefix}.AugSlot[{a}].Item.AugType{suffix}");

                target.Augs.Add(new InventoryAugment
                {
                    Slot = a,
                    Name = augName,
                    ItemLink = augLink,
                    Icon = augIcon,
                    ItemId = augId,
                    Ac = augAc,
                    Hp = augHp,
                    Mana = augMana,
                    AugType = augType
                });
            }
        }

        private static void CaptureAugmentSlots(IMQ mq, string itemTloPrefix, InventoryItem target)
        {
            int insertPos = itemTloPrefix.LastIndexOf('}');
            string prefix = insertPos >= 0 ? itemTloPrefix.Substring(0, insertPos) : itemTloPrefix;
            string suffix = insertPos >= 0 ? itemTloPrefix.Substring(insertPos) : "";

            for (int a = 1; a <= 6; a++)
            {
                bool visible = SafeQueryBool(mq, $"{prefix}.AugSlot[{a}].Visible{suffix}");
                bool empty = SafeQueryBool(mq, $"{prefix}.AugSlot[{a}].Empty{suffix}");
                int type = SafeQueryInt(mq, $"{prefix}.AugSlot[{a}].Type{suffix}");

                target.AugSlots.Add(new InventoryAugmentSlot
                {
                    Slot = a,
                    Type = type,
                    Visible = visible,
                    Empty = empty
                });
            }
        }

        private static void CaptureStats(IMQ mq, string basePath, InventoryItem item)
        {
            // basePath is like "${Me.Inventory[0]}" — insert stat before last "}"
            int insertPos = basePath.LastIndexOf('}');
            string prefix = insertPos >= 0 ? basePath.Substring(0, insertPos) : basePath;
            string suffix = insertPos >= 0 ? basePath.Substring(insertPos) : "";

            item.Ac = SafeQueryInt(mq, $"{prefix}.AC{suffix}");
            item.Hp = SafeQueryInt(mq, $"{prefix}.HP{suffix}");
            item.Mana = SafeQueryInt(mq, $"{prefix}.Mana{suffix}");
            item.Endurance = SafeQueryInt(mq, $"{prefix}.Endurance{suffix}");
            item.Str = SafeQueryInt(mq, $"{prefix}.STR{suffix}");
            item.Sta = SafeQueryInt(mq, $"{prefix}.STA{suffix}");
            item.Agi = SafeQueryInt(mq, $"{prefix}.AGI{suffix}");
            item.Dex = SafeQueryInt(mq, $"{prefix}.DEX{suffix}");
            item.Wis = SafeQueryInt(mq, $"{prefix}.WIS{suffix}");
            item.Intel = SafeQueryInt(mq, $"{prefix}.INT{suffix}");
            item.Cha = SafeQueryInt(mq, $"{prefix}.CHA{suffix}");

            item.ItemType = SafeQueryString(mq, $"{prefix}.Type{suffix}");
            item.Classes = SafeQueryInt(mq, $"{prefix}.Classes{suffix}");
            item.Races = SafeQueryInt(mq, $"{prefix}.Races{suffix}");
            item.Value = SafeQueryInt(mq, $"{prefix}.Value{suffix}");
            item.Tribute = SafeQueryInt(mq, $"{prefix}.Tribute{suffix}");
            item.Tradeskills = SafeQueryBool(mq, $"{prefix}.Tradeskills{suffix}");
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
