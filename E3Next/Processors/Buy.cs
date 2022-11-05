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
                Restock();
            });
        }
        private static void Restock()
        {
            string toEat = E3.CharacterSettings.Misc_AutoFood;
            string toDrink = E3.CharacterSettings.Misc_AutoDrink;
            int toEatQty = MQ.Query<int>($"${{FindItemCount[{toEat}]}}");
            int toDrinkQty = MQ.Query<int>($"${{FindItemCount[{toDrink}]}}");

            MQ.Write($"\agInitiating restock for {toEat} and {toDrink}");
            if (toEatQty >= 1000 && toDrinkQty >= 1000)
            {
                MQ.Write($"\arYou already have more than a stack of {toEat} and {toDrink}! Skipping restock. ");
                return;
            }
            else
            {
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

                    if (toEatQty < 1000)
                    {
                        int eatQtyNeeded = 1000 - toEatQty;
                        if (String.IsNullOrWhiteSpace(toEat))
                        {
                            MQ.Write($"\arNo Food item defined in ini, skipping food restock. ");
                        }
                        else
                        {
                            Buy.BuyItem(toEat, eatQtyNeeded);
                        }
                    }
                    if (toDrinkQty < 1000)
                    {
                        int drinkQtyNeeded = 1000 - toDrinkQty;
                        if (String.IsNullOrWhiteSpace(toDrink))
                        {
                            MQ.Write($"\arNo Drink item defined in ini, skipping food restock. ");
                        }
                        else
                        {
                            Buy.BuyItem(toDrink, drinkQtyNeeded);
                        }
                    }
                    e3util.CloseMerchant();
                }
                else
                {
                    MQ.Write($"\arNo valid vendor ID available.");
                }
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
                MQ.Cmd($"/notify MerchantWnd ItemList listselect {listPosition}");
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
            MQ.Cmd("/notify MerchantWnd MW_Buy_Button leftmouseup");
            MQ.Delay(300);
            bool qtyWindowOpen = MQ.Query<bool>("${Window[QuantityWnd].Open}");
            if (qtyWindowOpen)
            {
                MQ.Cmd($"/notify QuantityWnd QTYW_Slider newvalue {itemQty}");
                MQ.Cmd($"/notify QuantityWnd QTYW_Accept_Button leftmouseup");
                MQ.Delay(300);
            }

        }
        
    }
}
