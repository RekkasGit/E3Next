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
        private static IMQ MQ = E3.Mq;
        private static ISpawns Spawns = E3.Spawns;
        [SubSystemInit]
        public static void Init()
        {
            RegisterEvents();
        }

        private static void RegisterEvents()
        {
            EventProcessor.RegisterCommand("/restock", (x) =>
            {
                string itemName = x.args[0];
                int qtyNeeded = -1;

                if (x.args.Count >= 2)
                {
                    if (!int.TryParse(x.args[1], out qtyNeeded))
                    {
                        return;
                    }
                    else
                    {
                        E3.Bots.BroadcastCommand($"You must pass a number value for the 2nd parameter to restock {itemName}.");
                    }
                }

                switch(itemName)
                {
                    case "Emerald":
                        RestockItem(itemName, qtyNeeded);
                        break;
                    case "Food":
                        RestockFoodWater();
                        break;
                    case "Water":
                        RestockFoodWater();
                        break;
                    default:
                        RestockFoodWater();
                        break;
                }
                    
            });
        }
        /// <summary>
        /// Check how many are in a stack of the given item and give back how many of the item you need to make a full stack
        /// </summary>
        /// <param name="itemName"> Name of the item to check</param>
        private static int CheckQtyStackSize(string itemName)
        {
            
            int itemQtyStackSize = MQ.Query<int>($"${{FindItem[{itemName}].StackSize}}");
            int qtyNeeded = -1;
            int itemQty = -1;
            
            if (!String.IsNullOrWhiteSpace(itemName))
            {
                itemQty = MQ.Query<int>($"${{FindItemCount[{itemName}]}}");
            }

            if (itemQty >= itemQtyStackSize )
            {
                MQ.Write($"\arYou already have more than a stack of {itemName}!");
                return qtyNeeded;
            }

            qtyNeeded = itemQtyStackSize - itemQty;

            return qtyNeeded;
        }
        
        private static void RestockFoodWater()
        {
            string toEat = "Iron Ration";
            string toDrink = "Water Flask";
                        

            //check how many items are needed to make a stack of the specified food and drink, return -1 if they already have more than a stack
            int toEatQty = CheckQtyStackSize(toEat);
            int toDrinkQty = CheckQtyStackSize(toDrink);


            if (toEatQty <= 0 && toDrinkQty <= 0)
            {
                MQ.Write($"\arYou already have more than a stack of {toEat} and {toDrink}! Skipping restock. ");
                return;
            }
            else
            {
                MQ.Write($"\agInitiating restock for {toEat} and {toDrink}");
                //we have something we need to get
                int zoneID = E3.ZoneID;
                int vendorID = 0;

                if (zoneID == 345)
                {
                    //zoneID 345 = Guild Hall
                    string vendorName = "Yenny Werlikanin";
                    vendorID = MQ.Query<int>($"${{Spawn[{vendorName}].ID}}");

                }
                else if (zoneID == 202 || zoneID == 386)
                {                
                    //zoneID 202 = Plane of Knowledge
                    //zoneId 386 = Marr temple
                    string vendorName = "Vori";
                    vendorID = MQ.Query<int>($"${{Spawn[{vendorName}].ID}}");
                }

                if (vendorID > 0)
                {
                    Casting.TrueTarget(vendorID);
                    e3util.NavToSpawnID(vendorID);
                    e3util.OpenMerchant();
                    if (toEatQty > -1)
                    {
                        Buy.BuyItem(toEat, toEatQty);
                    }
                    

                    if (toDrinkQty > -1)
                    {
                        Buy.BuyItem(toDrink, toDrinkQty);
                    }
                    e3util.CloseMerchant();
                }
                else
                {
                    MQ.Write($"\arNo valid vendor ID available.");
                }
            }
        }

        private static void RestockItem(string itemName, int qtyNeeded)
        {
            
            //check how many items are needed to make a stack of the specified item, return -1 if they already have more than a stack
            int restockQty = CheckQtyStackSize(itemName);
            

            if (restockQty <= 0)
            {
                MQ.Write($"\arYou already have more than a stack of {itemName}! Skipping restock. ");
                return;
            }
            else
            {
                MQ.Write($"\agInitiating restock for {itemName}.");
                
            }
        }
        /// <summary>
        /// Buy specified item from an open vendor window
        /// </summary>
        /// <param name="itemName"></param>
        /// <param name="itemQty"></param>
        public static void BuyItem(string itemName, int itemQty)
        {
            //set listposition as the slot of the desired item on the vendor
            int listPosition = MQ.Query<int>($"${{Window[MerchantWnd].Child[ItemList].List[={itemName},2]}}");


            string buyingItemText = MQ.Query<string>("${Window[MerchantWnd].Child[MW_SelectedItemLabel].Text}");

            Int32 counter = 0;
            while (buyingItemText != itemName && counter < 10)
            {
                counter++;
                MQ.Cmd($"/nomodkey /notify MerchantWnd ItemList listselect {listPosition}");
                MQ.Delay(200);
                buyingItemText = MQ.Query<string>("${Window[MerchantWnd].Child[MW_SelectedItemLabel].Text}");
            }
            
            if (buyingItemText != itemName)
            {
                E3.Bots.Broadcast($"\arERROR: Buying item cannot get vendor to select, exiting. Item:{itemName}");
            }

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
                MQ.Cmd($"/nomodkey /notify QuantityWnd QTYW_Slider newvalue {itemQty}");
                MQ.Cmd($"/nomodkey /notify QuantityWnd QTYW_Accept_Button leftmouseup");
                MQ.Delay(300);
            }

        }
        
    }
}
