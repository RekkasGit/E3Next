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
        public static Logging _log = E3._log;
        private static IMQ MQ = E3.MQ;
        private static ISpawns _spawns = E3._spawns;

        [SubSystemInit]
        public static void Init()
        {
            RegisterEvents();
        }

        private static void RegisterEvents()
        {

        }
        
        /// <summary>
        /// Buy specified item from an open vendor window
        /// </summary>
        /// <param name="itemName"></param>
        /// <param name="itemQty"></param>
        public static void BuyItem(string itemName, int itemQty)
        {
            int listPosition = MQ.Query<int>($"${{Window[MerchantWnd].Child[ItemList].List[={itemName},2]}}");

            string buyingItemText = MQ.Query<string>("${Window[MerchantWnd].Child[MW_SelectedItemLabel].Text}");

            Int32 counter = 0;
            while (buyingItemText != itemName && counter < 10)
            {
                counter++;
                MQ.Cmd($"/notify MerchantWnd ItemList listselect {listPosition}");
                MQ.Delay(500);
                buyingItemText = MQ.Query<string>("${Window[MerchantWnd].Child[MW_SelectedItemLabel].Text}");
            }
            
            if (buyingItemText != itemName)
            {
                E3._bots.Broadcast($"\arERROR: Buying item cannot get vendor to select, exiting. Item:{itemName}");
            }

            //we have the item selected via the vendor, check we can buy.
            bool buyButtonEnabled = MQ.Query<bool>("${Window[MerchantWnd].Child[MW_Buy_Button].Enabled}");

            if (!buyButtonEnabled)
            {
                //buy button not enabled for whatever reason
                E3._bots.Broadcast($"\arERROR: Buy button for item on vendor is not active, exiting. Item:{itemName}");
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
