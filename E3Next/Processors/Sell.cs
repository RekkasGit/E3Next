using E3Core.Settings.FeatureSettings;
using E3Core.Utility;
using MonoCore;
using System;
using System.Security.Principal;
using System.ServiceModel.Configuration;


namespace E3Core.Processors
{
    public static class Sell
    {
        public static Logging _log = E3.Log;
        private static IMQ MQ = E3.MQ;
        private static ISpawns _spawns = E3.Spawns;


        [SubSystemInit]
        public static void Sell_Init()
        {
            RegisterEvents();
        }

        private static void RegisterEvents()
        {
            EventProcessor.RegisterCommand("/autosell", (x) =>
            {
                if (!e3util.OpenMerchant())
                {
                    E3.Bots.Broadcast("\arNo merchant targeted and no merchant found; exiting autosell");
                    return;
                }
                bool destroyOnSell = false;
                if(x.args.Count>0 && x.args[0] =="destroy")
                {
                    destroyOnSell = true;
                }
                AutoSell(destroyOnSell);
                
            });
			EventProcessor.RegisterCommand("/e3sell", (x) =>
			{
				
			    if(x.args.Count<1)
                {
                    E3.Bots.Broadcast("What should i sell?");
					return;
				
                }
                string itemToSell = x.args[0];
               

                bool printonly = false;

                if(itemToSell.StartsWith("pack")) printonly = true;

                if (x.args.Count == 2 && x.args[1] == "yes" ) printonly = false;
                if(!printonly)
                {
					if (!e3util.OpenMerchant())
					{
						E3.Bots.Broadcast("Please target a merchant to sell to.");
						return;
					}
				}

				SellItem(itemToSell, printonly);


			});
			EventProcessor.RegisterCommand("/syncinv", (x) =>
            {
                SyncInventory();
            });
            EventProcessor.RegisterCommand("/e3autobank", (x) =>
            {
                AutoBank();
            },"Auto stacks stackble items from your inventory to your bank if they exist.");
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

                            if (!nodrop && !LootDataFile.Sell.Contains(itemName) && !LootDataFile.Keep.Contains(itemName) && !LootDataFile.Skip.Contains(itemName))
                            {
                                //we don't know about this , add to keep and save. 
                                LootDataFile.Keep.Add(itemName);

                            }
                        }
                    }
                }
            }
            LootDataFile.SaveData();
        }

        private static void AutoSell(bool useDestroy = false)
        {
            int platinumGain = MQ.Query<int>("${Me.Platinum}");
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
                            if (LootDataFile.Sell.Contains(itemName) && itemValue > 0)
                            {
                                MQ.Cmd($"/nomodkey /itemnotify in pack{i} {e} leftmouseup",500);
                                
                                string sellingItemText = MQ.Query<string>("${Window[MerchantWnd].Child[MW_SelectedItemLabel].Text}");
                                Int32 counter = 0;
                                while (sellingItemText != itemName && counter < 10)
                                {
                                    counter++;
                                    MQ.Cmd($"/nomodkey /itemnotify in pack{i} {e} leftmouseup",500);
                                    
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
                                MQ.Cmd("/nomodkey /shift /notify MerchantWnd MW_Sell_Button leftmouseup",400+e3util.Latency());
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
            platinumGain = MQ.Query<int>("${Me.Platinum}") - platinumGain;
            MQ.Write($"\ag You made {platinumGain.ToString("N0")} platinum");
			MQ.Cmd("/nomodkey /notify MerchantWnd MW_Done_Button leftmouseup");
            MQ.Delay(500);
            if(useDestroy)
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

								if (LootDataFile.Destroy.Contains(itemName))
								{
									MQ.Cmd($"/shiftkey /itemnotify in pack{i} {e} leftmouseup", 500);

									if (e3util.ValidateCursor(MQ.Query<int>($"${{FindItem[={itemName}].ID}}")))
									{
										E3.Bots.Broadcast("<AutoSell> Destroying: " + itemName);
										e3util.CursorTryDestroyItem(itemName);
										MQ.Delay(300);
                                       
									}
								}
							}
						}
					}
				}
			}
			
		}
		private static void SellItem(string ItemToSell,bool printOnly=false)
		{

            string orginalItemToSell = ItemToSell;

			int platinumGain = MQ.Query<int>("${Me.Platinum}");
			bool merchantWindowOpen = MQ.Query<bool>("${Window[MerchantWnd].Open}");
			if (!merchantWindowOpen && !printOnly)
			{
				E3.Bots.Broadcast("\arError: <AutoSell> Merchant window not open. Exiting");
				return;
			}
            bool foundItemTosell = false;
			//scan through our inventory looking for an item with a stackable
			for (Int32 i = 1; i <= 10; i++)
			{
				string bagToCheck = $"pack{i}";
				bool SlotExists = MQ.Query<bool>($"${{Me.Inventory[{bagToCheck}]}}");
				if (SlotExists)
				{
					Int32 ContainerSlots = MQ.Query<Int32>($"${{Me.Inventory[{bagToCheck}].Container}}");

					if (ContainerSlots > 0)
					{
						for (Int32 e = 1; e <= ContainerSlots; e++)
						{
							//${Me.Inventory[${itemSlot}].Item[${j}].Name.Equal[${itemName}]}
							String itemName = MQ.Query<String>($"${{Me.Inventory[{bagToCheck}].Item[{e}]}}");
							Int32 stackCount = MQ.Query<Int32>($"${{Me.Inventory[{bagToCheck}].Item[{e}].Stack}}");
							if (itemName == "NULL")
							{
								continue;
							}
							Int32 itemValue = MQ.Query<Int32>($"${{Me.Inventory[{bagToCheck}].Item[{e}].Value}}");
                            //if we are passed in a pack1-10, just sell everything in the bag if possible
                            //dangerous option, but hey they wanted it!
                            if(bagToCheck== orginalItemToSell)
                            {
                                ItemToSell = itemName;
                            }
							if (String.Equals(itemName,ItemToSell,StringComparison.OrdinalIgnoreCase) && itemValue > 0)
							{
                                foundItemTosell = true;

								if (printOnly)
								{
									E3.Bots.Broadcast($"\ag Possible item to sell ${{Me.Inventory[{bagToCheck}].Item[{e}].ItemLink[CLICKABLE]}} - \awbag({i}) slot({e}) count({stackCount})");
  				                    continue;
                                }
								MQ.Cmd($"/nomodkey /itemnotify in pack{i} {e} leftmouseup", 500);

								string sellingItemText = MQ.Query<string>("${Window[MerchantWnd].Child[MW_SelectedItemLabel].Text}");
								Int32 counter = 0;
								while (sellingItemText != itemName && counter < 10)
								{
									counter++;
									MQ.Cmd($"/nomodkey /itemnotify in pack{i} {e} leftmouseup", 500);

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
								MQ.Cmd("/nomodkey /shift /notify MerchantWnd MW_Sell_Button leftmouseup", 400 + e3util.Latency());
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
            if(!printOnly)
            {
				platinumGain = MQ.Query<int>("${Me.Platinum}") - platinumGain;
				MQ.Write($"\ag You made {platinumGain.ToString("N0")} platinum");
				MQ.Cmd("/nomodkey /notify MerchantWnd MW_Done_Button leftmouseup");
			}
			if (foundItemTosell && printOnly)
            {
                E3.Bots.Broadcast($"\ag Please use \aw\"/e3sell \"{orginalItemToSell}\" yes\" \ag to confirm selling");
            }    

		}
		private static void AutoBank()
        {
			if (!e3util.OpenBank())
			{
				E3.Bots.Broadcast("Please target a banker.");
				return;
			}
		
			var windowOpen = MQ.Query<bool>("${Window[BigBankWnd].Open}");
            if (!windowOpen)
            {
				if (!e3util.OpenBank())
				{
					E3.Bots.Broadcast("Please target a banker.");
					return;
				}
				windowOpen = MQ.Query<bool>("${Window[BigBankWnd].Open}");
				if(!windowOpen)
				{
					E3.Bots.Broadcast("\arError: <AutoStack> Bank window not open. Exiting");
					return;
				}
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
							E3.Bots.Broadcast($"\ar<\ayAutoBank\ar> \agTrying to stack \aw{item} \aginto the bank");

							MQ.Cmd($"/nomodkey /itemnotify in pack{i} {j} leftmouseup",500);
                            
                            if (MQ.Query<bool>("${Window[QuantityWnd].Open}"))
                            {
                                MQ.Cmd("/nomodkey /notify QuantityWnd QTYW_Accept_Button leftmouseup", 500);
                            }

							MQ.Cmd("/invoke ${Window[BigBankWnd].Child[BIGB_AutoButton].LeftMouseUp}",500);

                        }
                    }
                }
            }
			//close bags, any way to tell we are in an open state?
			MQ.Cmd("/nomodkey /notify BigBankWnd BIGB_DoneButton leftmouseup");
            E3.Bots.Broadcast("\agFinished stacking items in bank");            
        }
    }
}
