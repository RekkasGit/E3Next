using E3Core.Settings.FeatureSettings;
using E3Core.Utility;
using MonoCore;
using System;


namespace E3Core.Processors
{
    public static class Sell
    {
        public static Logging _log = E3.Log;
        private static IMQ MQ = E3.Mq;
        private static ISpawns _spawns = E3.Spawns;


        [SubSystemInit]
        public static void Init()
        {
            RegisterEvents();
        }

        private static void RegisterEvents()
        {
            EventProcessor.RegisterCommand("/autosell", (x) =>
            {
                e3util.OpenMerchant();
                AutoSell();
                MQ.Cmd("/nomodkey /notify MerchantWnd MW_Done_Button leftmouseup");
            });
            EventProcessor.RegisterCommand("/syncinv", (x) =>
            {
                SyncInventory();
            });
            EventProcessor.RegisterCommand("/autostack", (x) =>
            {
                e3util.OpenMerchant();
                AutoStack();
            });
        }
        public static void SyncInventory()
        {
            //scan through our inventory looking for an item with a stackable
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
                            //${Me.Inventory[${itemSlot}].Item[${j}].Name.Equal[${itemName}]}
                            String itemName = MQ.Query<String>($"${{Me.Inventory[pack{i}].Item[{e}]}}");
                            if (itemName == "NULL")
                            {
                                continue;
                            }

                            bool nodrop = MQ.Query<bool>($"${{Me.Inventory[pack{i}].Item[{e}].NoDrop}}");

                            if (!nodrop && !LootDataFile._sell.Contains(itemName) && !LootDataFile._keep.Contains(itemName) && !LootDataFile._keep.Contains(itemName))
                            {
                                //we don't know about this , add to keep and save. 
                                LootDataFile._keep.Add(itemName);
                            }
                        }
                    }
                }
            }
            LootDataFile.SaveData();
        }

        private static void AutoSell()
        {
            bool merchantWindowOpen = MQ.Query<bool>("${Window[MerchantWnd].Open}");
            if (!merchantWindowOpen)
            {
                E3.Bots.Broadcast("\arError: <AutoSell> Merchant window not open. Exiting");
                return;
            }
            //scan through our inventory looking for an item with a stackable
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
                            //${Me.Inventory[${itemSlot}].Item[${j}].Name.Equal[${itemName}]}
                            String itemName = MQ.Query<String>($"${{Me.Inventory[pack{i}].Item[{e}]}}");
                            if (itemName == "NULL")
                            {
                                continue;
                            }
                            Int32 itemValue = MQ.Query<Int32>($"${{Me.Inventory[pack{i}].Item[{e}].Value}}");
                            if (LootDataFile._sell.Contains(itemName) && itemValue > 0)
                            {
                                MQ.Cmd($"/nomodkey /itemnotify in pack{i} {e} leftmouseup");
                                MQ.Delay(500);
                                string sellingItemText = MQ.Query<string>("${Window[MerchantWnd].Child[MW_SelectedItemLabel].Text}");
                                Int32 counter = 0;
                                while (sellingItemText != itemName && counter < 10)
                                {
                                    counter++;
                                    MQ.Cmd($"/nomodkey /itemnotify in pack{i} {e} leftmouseup");
                                    MQ.Delay(500);
                                    sellingItemText = MQ.Query<string>("${Window[MerchantWnd].Child[MW_SelectedItemLabel].Text}");
                                }
                                if (sellingItemText != itemName)
                                {
                                    E3.Bots.Broadcast($"\arERROR: Selling item cannot get vendor to select, exiting. Item:{itemName}");
                                }
                                //we have the item selected via the vendor, sell it.
                                bool sellButtonEnabled = MQ.Query<bool>("${Window[MerchantWnd].Child[MW_Sell_Button].Enabled}");

                                if (!sellButtonEnabled)
                                {
                                    //sell button not enabled for whatever reason
                                    continue;
                                }

                                //sell the item finally
                                MQ.Cmd("/nomodkey /notify MerchantWnd MW_Sell_Button leftmouseup");
                                MQ.Delay(300);
                                bool qtyWindowOpen = MQ.Query<bool>("${Window[QuantityWnd].Open}");
                                if (qtyWindowOpen)
                                {
                                    MQ.Cmd("/nomodkey /notify QuantityWnd QTYW_Accept_Button leftmouseup");
                                    MQ.Delay(300);
                                }

                                string tItemName = MQ.Query<String>($"${{Me.Inventory[pack{i}].Item[{e}]}}");
                                if (itemName == tItemName)
                                {
                                    E3.Bots.Broadcast($"\arERROR: Selling item. Item:{itemName} Tried to sell but still in inventory. PrimarySlot:{i} bagslot:{e}");

                                }
                            }
                        }
                    }
                }
            }
        }

        private static void AutoStack()
        {
            var windowOpen = MQ.Query<bool>("${Window[BigBankWnd].Open}");
            if (!windowOpen)
            {
                E3.Bots.Broadcast("\arError: <AutoStack> Merchant window not open. Exiting");
            }

            for (int i = 1; i <= 10; i++)
            {
                if (MQ.Query<bool>($"${{Me.Inventory[pack{i}]}}"))
                {
                    var slotsInContainer = MQ.Query<int>($"${{Me.Inventory[pack{i}].Container}}");
                    for (int j = 1; j <= slotsInContainer; j++)
                    {
                        var item = MQ.Query<string>($"${{Me.Inventory[pack{i}].Item[{j}]}}");
                        var isItemStackable = MQ.Query<bool>($"${{Me.Inventory[pack{i}].Item[{j}].Stackable}}");
                        if (!isItemStackable)
                        {
                            continue;
                        }

                        if (MQ.Query<bool>($"${{FindItemBank[={item}]}}"))
                        {
                            MQ.Cmd($"/nomodkey /itemnotify \"{item}\" leftmouseup");
                            MQ.Delay(250);
                            if (MQ.Query<bool>("${Window[QuantityWnd].Open}"))
                            {
                                MQ.Cmd("/nomodkey /notify QuantityWnd QTYW_Accept_Button leftmouseup");
                                MQ.Delay(250);
                            }

                            var slot = MQ.Query<int>($"${{FindItemBank[={item}].ItemSlot}}");
                            var slot2 = MQ.Query<int>($"${{FindItemBank[={item}].ItemSlot2}}");
                            // different syntax for if the item is in a bag vs if it's not
                            if (slot2 >= 0)
                            {
                                MQ.Cmd($"/nomodkey /itemnotify in bank{slot + 1} {slot2 + 1} leftmouseup");
                            }
                            else
                            {
                                MQ.Cmd($"/nomodkey /itemnotify bank{slot + 1} leftmouseup");
                            }

                            MQ.Delay(250);
                        }
                    }
                }
            }

            MQ.Cmd("/nomodkey /notify BigBankWnd BIGB_DoneButton leftmouseup");
            MQ.Write("\agFinished stacking items in bank");            
        }
    }
}
