using E3Core.Data;
using E3Core.Settings;
using E3Core.Settings.FeatureSettings;
using E3Core.Utility;
using MonoCore;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Dynamic;
using System.Linq;


namespace E3Core.Processors
{
    public static class Sell
    {
        public static Logging _log = E3._log;
        private static IMQ MQ = E3.MQ;
        private static ISpawns _spawns = E3._spawns;



        public static void Init()
        {
            RegisterEvents();
        }

        private static void RegisterEvents()
        {
            EventProcessor.RegisterCommand("/autosell", (x) =>
            {
                OpenMerchant();
                AutoSell();
                MQ.Cmd("/notify MerchantWnd MW_Done_Button leftmouseup");
            });
            EventProcessor.RegisterCommand("/syncinv", (x) =>
            {
                SyncInventory();
               

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
                            if(itemName=="NULL")
                            {
                                continue;
                            }

                            bool nodrop = MQ.Query<bool>($"${{Me.Inventory[pack{i}].Item[{e}].NoDrop}}");
                     
                            if (!nodrop  && !LootDataFile._sell.Contains(itemName) && !LootDataFile._keep.Contains(itemName) && !LootDataFile._keep.Contains(itemName))
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

        private static void OpenMerchant()
        {
            e3util.TryMoveToTarget();
            MQ.Cmd("/click right target");
            MQ.Delay(500);
        }

        private static void AutoSell()
        {
            bool merchantWindowOpen = MQ.Query<bool>("${Window[MerchantWnd].Open}");
            if(!merchantWindowOpen)
            {
                E3._bots.Broadcast("\arError: <AutoSell> Merchant window not open. Exiting");
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
                            if (LootDataFile._sell.Contains(itemName) && itemValue>0)
                            {
                                MQ.Cmd($"/itemnotify in pack{i} {e} leftmouseup");
                                MQ.Delay(500);
                                string sellingItemText = MQ.Query<string>("${Window[MerchantWnd].Child[MW_SelectedItemLabel].Text}");
                                Int32 counter = 0;
                                while(sellingItemText!=itemName && counter<10)
                                {
                                    counter++;
                                    MQ.Cmd($"/itemnotify in pack{i} {e} leftmouseup");
                                    MQ.Delay(500);
                                    sellingItemText = MQ.Query<string>("${Window[MerchantWnd].Child[MW_SelectedItemLabel].Text}");
                                }
                                if(sellingItemText != itemName)
                                {
                                    MQ.Broadcast($"\arERROR: Selling item cannot get vendor to select, exiting. Item:{itemName}");
                                }
                                //we have the item selected via the vendor, sell it.
                                bool sellButtonEnabled = MQ.Query<bool>("${Window[MerchantWnd].Child[MW_Sell_Button].Enabled}");

                                if(!sellButtonEnabled)
                                {
                                    //sell button not enabled for whaever reason
                                    continue;
                                }

                                //sell the item finally
                                MQ.Cmd("/notify MerchantWnd MW_Sell_Button leftmouseup");
                                MQ.Delay(300);
                                bool qtyWindowOpen = MQ.Query<bool>("${Window[QuantityWnd].Open}");
                                if(qtyWindowOpen)
                                {
                                    MQ.Cmd("/notify QuantityWnd QTYW_Accept_Button leftmouseup");
                                    MQ.Delay(300);
                                }
            
                                string tItemName = MQ.Query<String>($"${{Me.Inventory[pack{i}].Item[{e}]}}");
                                if(itemName==tItemName)
                                {
                                    MQ.Broadcast($"\arERROR: Selling item. Item:{itemName} Tried to sell but still in inventory. PrimarySlot:{i} bagslot:{e}");

                                }

                            }
                        }
                    }
                   
                }
            }
        }
    }
}
