using E3Core.Settings.FeatureSettings;
using E3Core.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MonoCore;


namespace E3Core.Processors
{
    public static class Buy
    {
        public static Logging Log = E3.Log;
        private static IMQ MQ = E3.MQ;
        private static ISpawns Spawns = E3.Spawns;
        [SubSystemInit]
        public static void Buy_Init()
        {
            RegisterEvents();
        }

        private static void RegisterEvents()
        {
            EventProcessor.RegisterCommand("/restock", (x) =>
            {
                E3.Bots.Broadcast("This command is deprecated, please use /e3restock with the vendor targeted. this will auto broadcast.");
                string itemName = "food";
                if(x.args.Count>1)
                {
                    itemName = x.args[0];
                }
                int qtyNeeded = -1;

                if (x.args.Count >= 2)
                {
                    if (!int.TryParse(x.args[1], out qtyNeeded))
                    {
                        return;
                    }
                    else
                    {
                        E3.Bots.Broadcast($"You must pass a number value for the 2nd parameter to restock {itemName}.");
                    }
                }
                itemName = itemName.ToLower();
				int zoneID = Zoning.CurrentZone.Id;
				int vendorID = 0;
				if (vendorID == 0) vendorID = MQ.Query<Int32>("${Target.ID}");

				if (vendorID == 0 && zoneID == 345)
				{
					//zoneID 345 = Guild Hall
					string vendorName = "Yenny Werlikanin";
					vendorID = MQ.Query<int>($"${{Spawn[{vendorName}].ID}}");

				}
				else if (vendorID == 0 && (zoneID == 202 || zoneID == 386))
				{
					//zoneID 202 = Plane of Knowledge
					//zoneId 386 = Marr temple
					string vendorName = "Vori";
					vendorID = MQ.Query<int>($"${{Spawn[{vendorName}].ID}}");
				}
                if(vendorID>0)
                {
					switch (itemName)
					{
						case "emerald":
							RestockItem(vendorID, itemName, qtyNeeded);
							break;
						case "food":
							RestockFoodWater(vendorID);
							break;
						case "water":
							RestockFoodWater(vendorID);
							break;
						default:
							RestockFoodWater(vendorID);
							break;
					}
				}
            }, "(deprecated use /e3restock) used to buy food/water/emeralds");

			EventProcessor.RegisterCommand("/e3restock", (x) =>
			{
				int vendorID = 0;


				//just used to see if this is a rebroadcasted command or not.
				bool localCommand = false;
				if (x.args.Count>0 && x.args[0]=="me")
				{
					localCommand = true;
                    x.args.RemoveAt(0);
				}
                //default in case they don't specify what they want
				string itemName = "food";
				if (x.args.Count > 0)
				{
					itemName = x.args[0];
				}
				int qtyNeeded = -1;
				if (x.args.Count > 1)
				{
					if (!int.TryParse(x.args[1], out qtyNeeded))
					{
                        E3.Bots.Broadcast("Not a valid number for qty needed for e3restock.");
						return;
					}
				}
                if(x.args.Count>2)
                {
					//vendor id specified?
					if (!int.TryParse(x.args[2], out vendorID))
					{
						E3.Bots.Broadcast("Not a valid vendor id");
						return;
					}
				}
				if (!localCommand)
				{
					vendorID = MQ.Query<Int32>("${Target.ID}");
					//broadcast out this command to others with the necessary filter attached.
					E3.Bots.BroadcastCommandToGroup($"/e3restock me \"{itemName}\" {qtyNeeded} {vendorID}", x);
				}
				if (!e3util.FilterMe(x))
				{
					itemName = itemName.ToLower().Trim();
					
					if(vendorID == 0) vendorID = MQ.Query<Int32>("${Target.ID}");

					if (vendorID > 0)
					{
						switch (itemName)
						{
							case "food":
								RestockFoodWater(vendorID);
								break;
							case "water":
								RestockFoodWater(vendorID);
								break;
							default:
								RestockItem(vendorID, itemName, qtyNeeded);
								break;
						}
					}

				}
				

			}, "used to buy food/water/emeralds. /e3restock food, /e3restock emerald 500 /only|Healers");
		}
        /// <summary>
        /// Check how many are in a stack of the given item and give back how many of the item you need to make a full stack
        /// </summary>
        /// <param name="itemName"> Name of the item to check</param>
        private static int GetQtyNeededForFullStack(string itemName)
        {
            
            int itemQtyStackSize = MQ.Query<int>($"${{FindItem[={itemName}].StackSize}}");
            int qtyNeeded = -1;
            int itemQty = -1;
            
            if (!String.IsNullOrWhiteSpace(itemName))
            {
                itemQty = MQ.Query<int>($"${{FindItemCount[={itemName}]}}");
            }

            if (itemQty >= itemQtyStackSize )
            {
                //MQ.Write($"\arYou already have more than a stack of {itemName}!");
                return qtyNeeded;
            }

            qtyNeeded = itemQtyStackSize - itemQty;

            return qtyNeeded;
        }
        
        private static void RestockFoodWater(Int32 vendorID)
        {
            string toEat = "Iron Ration";
            string toDrink = "Water Flask";
                        
            //check how many items are needed to make a stack of the specified food and drink, return -1 if they already have more than a stack

            Int32 foodAvail = MQ.Query<int>($"${{FindItemCount[={toEat}]}}");
            Int32 drinkAvail = MQ.Query<int>($"${{FindItemCount[={toDrink}]}}");

            int toEatQty = 20;
            if (foodAvail > 0)
            {
                toEatQty=GetQtyNeededForFullStack(toEat);
            }
			
            int toDrinkQty =20;
            if (drinkAvail > 0)
            {
                toDrinkQty=GetQtyNeededForFullStack(toDrink);

            }
            if (toEatQty <= 0 && toDrinkQty <= 0)
            {
                E3.Bots.Broadcast($"\arAlready have more than a stack of {toEat} and {toDrink}! Skipping restock. ");
                return;
            }
            else
            {
			
				E3.Bots.Broadcast($"\agInitiating restock for {toEat} and/or {toDrink}");

                //we have something we need to get
               
                if (vendorID > 0)
                {
                    Casting.TrueTarget(vendorID);
                    e3util.NavToSpawnID(vendorID);
                    e3util.OpenMerchant();
                    if (toEatQty > 0)
					{
						//buy full stack if we don't have any
						if (foodAvail == 0) toEatQty = -1;
						Buy.BuyItem(toEat, toEatQty);
						MQ.Delay(500);
					}
                    if (toDrinkQty > 0)
					{
						//buy full stack ikf we don't have any
						if (drinkAvail == 0) toDrinkQty = -1;
						Buy.BuyItem(toDrink, toDrinkQty);
                    }
                    e3util.CloseMerchant();
                }
                else
                {
					E3.Bots.Broadcast($"\arNo valid vendor ID available.");
                }
            }
        }

        private static void RestockItem(Int32 vendorID,string itemName, int maxQty)
        {
			//check how many items are needed to make a stack of the specified food and drink, return -1 if they already have more than a stack

			Int32 itemAvail = MQ.Query<int>($"${{FindItemCount[={itemName}]}}");

			int qtyDifference = maxQty - itemAvail;
			//start off trying to buy what e have been requested to.
			int qtyToBuy = -1; //default stack size if we don't have it
			
			
			//figure out how much a full stack will be.
			if (itemAvail > 0)
			{
				int itemQtyStackSize = MQ.Query<int>($"${{FindItem[={itemName}].StackSize}}");


				//guard clause
				if (maxQty>0 && itemAvail >= maxQty)
				{
					E3.Bots.Broadcast($"\arAlready have a at least {maxQty} ({itemAvail}) stack of {itemName}! Skipping restock. ");
					return;
				}
				//we are not specifying qty, just get a full stack.
				if (maxQty == -1)
				{
					int qtyToBuyForFullStack = GetQtyNeededForFullStack(itemName);
					if(qtyToBuyForFullStack<1)
					{
						E3.Bots.Broadcast($"\arThe stack is full for  {itemName}! ({itemAvail}) Skipping restock. ");
						return;
					}
					qtyToBuy = qtyToBuyForFullStack;
				}
				else
				{
					//we specified qty, lets get the difference from what we have and what we are requesting.
					qtyToBuy = qtyDifference;

					if((qtyToBuy +itemAvail) > itemQtyStackSize)
					{
						E3.Bots.Broadcast($"\arThe stack is full for  {itemName}! ({itemAvail}) Skipping restock. ");
						return;
					}
				}
			}
			
			E3.Bots.Broadcast($"\agInitiating restock for {itemName}");
			//we have something we need to get

			if (vendorID > 0)
			{
				Casting.TrueTarget(vendorID);
				e3util.NavToSpawnID(vendorID);
				e3util.OpenMerchant();
				
				Buy.BuyItem(itemName, qtyToBuy);
				
				e3util.CloseMerchant();
			}
			else if(MQ.Query<bool>("${Window[MerchantWnd].Open}"))
			{
				Buy.BuyItem(itemName, qtyToBuy);
				e3util.CloseMerchant();
			}
			else
			{
				E3.Bots.Broadcast($"\arNo valid vendor ID available.");
			}
			
		}
        /// <summary>
        /// Buy specified item from an open vendor window
        /// </summary>
        /// <param name="itemName"></param>
        /// <param name="itemQty"></param>
        public static void BuyItem(string itemName, int itemQty)
        {
			itemName = itemName.Trim();
            //set listposition as the slot of the desired item on the vendor
            int listPosition = MQ.Query<int>($"${{Window[MerchantWnd].Child[ItemList].List[={itemName},2]}}");


			string buyingItemText = MQ.Query<string>("${Window[MerchantWnd].Child[MW_SelectedItemLabel].Text}").Trim();
			MQ.Delay(300);
            Int32 counter = 0;

			while (!string.Equals(itemName,buyingItemText, StringComparison.OrdinalIgnoreCase) && counter < 10)
            {
                counter++;
                MQ.Cmd($"/nomodkey /notify MerchantWnd ItemList listselect {listPosition}");
                MQ.Delay(200);
                buyingItemText = MQ.Query<string>("${Window[MerchantWnd].Child[MW_SelectedItemLabel].Text}");
            }
            
            if (!string.Equals(itemName, buyingItemText, StringComparison.OrdinalIgnoreCase))
            {
                E3.Bots.Broadcast($"\arERROR: Buying item cannot get vendor to select, exiting. Item:{itemName}");
                return;
			}

			//give time for the buy button to enable
			MQ.Delay(300);
            //we have the item selected via the vendor, check we can buy.
            bool buyButtonEnabled = MQ.Query<bool>("${Window[MerchantWnd].Child[MW_Buy_Button].Enabled}");

            if (!buyButtonEnabled)
            {
                //buy button not enabled for whatever reason
                E3.Bots.Broadcast($"\arERROR: Buy button for item on vendor is not active, exiting. Item:{itemName}");
                return;
            }

            //buy the item finally
            MQ.Cmd("/nomodkey /notify MerchantWnd MW_Buy_Button leftmouseup");
            MQ.Delay(300);
            bool qtyWindowOpen = MQ.Query<bool>("${Window[QuantityWnd].Open}");
            if (qtyWindowOpen)
            {
				if(itemQty>0)
				{
					MQ.Cmd($"/nomodkey /notify QuantityWnd QTYW_Slider newvalue {itemQty}");
				}
				else
				{
					itemQty = MQ.Query<Int32>("${Window[QuantityWnd].Child[QTYW_SliderInput].Text}");
				}
				MQ.Cmd($"/nomodkey /notify QuantityWnd QTYW_Accept_Button leftmouseup");
                MQ.Delay(300);
            }
            E3.Bots.Broadcast($"Bought {itemQty} {itemName}");
        }
        
    }
}
