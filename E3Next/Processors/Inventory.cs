using E3Core.Data;
using E3Core.Settings;
using E3Core.Settings.FeatureSettings;
using E3Core.Utility;
using E3Core.Processors;
using IniParser;
using MonoCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace E3Core.Processors
{
    public static class Inventory
    {
        public static Logging _log = E3.Log;
        private static IMQ MQ = E3.MQ;
        private static ISpawns _spawns = E3.Spawns;
        private static readonly List<string> _invSlots = new List<string>() { "charm", "leftear", "head", "face", "rightear", "neck", "shoulder", "arms", "back", "leftwrist", "rightwrist", "ranged", "hands", "mainhand", "offhand", "leftfinger", "rightfinger", "chest", "legs", "feet", "waist", "powersource", "ammo" };
        private static readonly List<string> _fdsSlots = new List<string>(_invSlots) { "fingers", "wrists", "ears" };
        private static long _nextTradeCheck = 0;
        private static long _nextTradeCheckInterval = 1000;

        [SubSystemInit]
        public static void Inventory_Init()
        {
            RegisterEvents();
        }
        private static bool FDSPrint(string slot,string channel="/gsay")
        {

            if (_fdsSlots.Contains(slot))
            {
                if (slot == "fingers")
                {
                    MQ.Cmd($"{channel} Left:${{InvSlot[leftfinger].Item.ItemLink[CLICKABLE]}}   Right:${{InvSlot[rightfinger].Item.ItemLink[CLICKABLE]}}");
                }
                else if (slot == "wrists")
                {
                    MQ.Cmd($"{channel} Left:${{InvSlot[leftwrist].Item.ItemLink[CLICKABLE]}}   Right:${{InvSlot[rightwrist].Item.ItemLink[CLICKABLE]}} ");

                }
                else if (slot == "ears")
                {
                    MQ.Cmd($"{channel} Left:${{InvSlot[leftear].Item.ItemLink[CLICKABLE]}}   Right:${{InvSlot[rightear].Item.ItemLink[CLICKABLE]}} ");
                }
                else
                {
                    MQ.Cmd($"{channel} {slot}:${{InvSlot[{slot}].Item.ItemLink[CLICKABLE]}}");

                }
                return true;
            }
            else
            {
                E3.Bots.Broadcast("Cannot find slot. Valid slots are:" + String.Join(",", _fdsSlots));
                return false;
            }
        }
        private static void FindItemCompact(string itemName)
        {

            bool weHaveItem = MQ.Query<bool>($"${{FindItemCount[={itemName}]}}");
            bool weHaveItemInBank = MQ.Query<bool>($"${{FindItemBankCount[={itemName}]}}");
            Int32 totalItems = 0;

            List<string> report = new List<string>();


            //search equiped items
            for (int i = 0; i <= 22; i++)
            {
                string name = MQ.Query<string>($"${{Me.Inventory[{i}]}}");

                if (MQ.Query<bool>($"${{Me.Inventory[{i}].Name.Find[{itemName}]}}"))
                {
                    Int32 stackCount = MQ.Query<Int32>($"${{InvSlot[{i}].Item.Stack}}");
                    totalItems += stackCount;
                    report.Add($"\ag[Worn] ${{Me.Inventory[{i}].ItemLink[CLICKABLE]}}\aw ({stackCount})");
                }
                Int32 augCount = MQ.Query<Int32>($"${{Me.Inventory[{i}].Augs}}");
                if (augCount > 0)
                {
                    for (int a = 1; a <= 6; a++)
                    {
                        string augname = MQ.Query<string>($"${{Me.Inventory[{i}].AugSlot[{a}].Name}}");

                        if (augname.IndexOf(itemName, 0, StringComparison.OrdinalIgnoreCase) > -1)
                        {
                            totalItems += 1;
                            report.Add($"\ag[Worn] ${{InvSlot[{i}].Item.ItemLink[CLICKABLE]}} - ${{InvSlot[{i}].Item.AugSlot[{a}].Item.ItemLink[CLICKABLE]}} \aw(aug-slot[{a}])");
                        }
                    }
                }

            }
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
                            String bagItem = MQ.Query<String>($"${{Me.Inventory[pack{i}].Item[{e}]}}");
                            Int32 stackCount = MQ.Query<Int32>($"${{Me.Inventory[pack{i}].Item[{e}].Stack}}");
                            if (bagItem.IndexOf(itemName, 0, StringComparison.OrdinalIgnoreCase) > -1)
                            {
                                report.Add($"\ag[Pack] ${{Me.Inventory[pack{i}].Item[{e}].ItemLink[CLICKABLE]}} - \awbag({i}) slot({e}) count({stackCount})");
                            }
                            Int32 augCount = MQ.Query<Int32>($"${{Me.Inventory[pack{i}].Item[{e}].Augs}}");
                            if (augCount > 0)
                            {
                                for (int a = 1; a <= 6; a++)
                                {
                                    string augname = MQ.Query<string>($"${{Me.Inventory[pack{i}].Item[{e}].AugSlot[{a}].Name}}");

                                    if (augname.IndexOf(itemName, 0, StringComparison.OrdinalIgnoreCase) > -1)
                                    {
                                        totalItems += 1;
                                        report.Add($"\ag[Pack] ${{Me.Inventory[pack{i}].Item[{e}].ItemLink[CLICKABLE]}} - ${{Me.Inventory[pack{i}].Item[{e}].AugSlot[{a}].Item.ItemLink[CLICKABLE]}} \aw(aug-slot[{a}]) \awbag({i}) slot({e})");
                                    }
                                }
                            }
                        }
                    }
					else
					{
						//its a single item

						String bagItem = MQ.Query<String>($"${{Me.Inventory[pack{i}]}}");
						if (bagItem.IndexOf(itemName, 0, StringComparison.OrdinalIgnoreCase) > -1)
						{
							report.Add($"\ag[Pack] ${{Me.Inventory[pack{i}].ItemLink[CLICKABLE]}} - \awbag({i})");
						}
						Int32 augCount = MQ.Query<Int32>($"${{Me.Inventory[pack{i}].Augs}}");
						if (augCount > 0)
						{
							for (int a = 1; a <= 6; a++)
							{
								string augname = MQ.Query<string>($"${{Me.Inventory[pack{i}].AugSlot[{a}].Name}}");

								if (augname.IndexOf(itemName, 0, StringComparison.OrdinalIgnoreCase) > -1)
								{
									totalItems += 1;
									report.Add($"\ag[Pack] ${{Me.Inventory[pack{i}].ItemLink[CLICKABLE]}} - ${{Me.Inventory[pack{i}].AugSlot[{a}].Item.ItemLink[CLICKABLE]}} \aw(aug-slot[{a}]) \awbag({i})");
								}
							}
						}

					}

                }
				
            }

            for (int i = 1; i <= 26; i++)
            {
                string bankItemName = MQ.Query<string>($"${{Me.Bank[{i}].Name}}");
                if (bankItemName.IndexOf(itemName, 0, StringComparison.OrdinalIgnoreCase) > -1)
                {
                    Int32 bankStack = MQ.Query<Int32>($"${{Me.Bank[{i}].Stack}}");
                    report.Add($"\ag[Bank] ${{Me.Bank[{i}].ItemLink[CLICKABLE]}} \aw- slot({i}) count({bankStack})");
                }


                //look through container
                Int32 ContainerSlots = MQ.Query<Int32>($"${{Me.Bank[{i}].Container}}");
                for (int e = 1; e <= ContainerSlots; e++)
                {
                    bankItemName = MQ.Query<string>($"${{Me.Bank[{i}].Item[{e}].Name}}");

                    if (bankItemName.IndexOf(itemName, 0, StringComparison.OrdinalIgnoreCase) > -1)
                    {
                        Int32 bankStack = MQ.Query<Int32>($"${{Me.Bank[{i}].Item[{e}].Stack}}");
                        report.Add($"\ag[Bank] ${{Me.Bank[{i}].Item[{e}].ItemLink[CLICKABLE]}} \aw- slot({i}) bagslot({e}) count({bankStack})");
                    }
                    Int32 augCount = MQ.Query<Int32>($"${{Me.Bank[{i}].Item[{e}].Augs}}");
                    if (augCount > 0)
                    {
                        for (int a = 1; a <= 6; a++)
                        {
                            string augname = MQ.Query<string>($"${{Me.Bank[{i}].Item[{e}].AugSlot[{a}].Name}}");

                            if (augname.IndexOf(itemName, 0, StringComparison.OrdinalIgnoreCase) > -1)
                            {
                                totalItems += 1;
                                report.Add($"\ag[Bank-Aug-Worn] ${{Me.Bank[{i}].Item[{e}].AugSlot[{a}].Item.ItemLink[CLICKABLE]}} - ${{Me.Bank[{i}].Item[{e}].AugSlot[{a}].Item.ItemLink[CLICKABLE]}} slot({i}) bagslot({e}) (aug-slot[{a}])");
                            }

                        }
                    }
                }
            }

            foreach (var value in report)
            {
                E3.Bots.Broadcast(value);

            }
        }

        private static void GetFrom(string where, List<string> args)
        {
            if (string.Equals(where, "Bank"))
            {
                if (!MQ.Query<bool>("${Window[BigBankWnd]}"))
                {
                    MQ.Write("\arYou need to open the bank window before issuing this command");
                    return;
                }
            }

            if (args.Count == 0)
            {
                MQ.Write("\arYou need to tell me what to get!");
                return;
            }

            var item = args[0];
            var findTlo = string.Equals(where, "Bank") ? "FindItemBank" : "FindItem";
            if (!MQ.Query<bool>($"${{{findTlo}[={item}]}}"))
            {
                MQ.Write($"\arYou do not have any {item}s");
                return;
            }

            var slot = MQ.Query<int>($"${{{findTlo}[={item}].ItemSlot}}");
            var slot2 = MQ.Query<int>($"${{{findTlo}[={item}].ItemSlot2}}");

            // different syntax for if the item is in a bag vs if it's not
            var itemNotifyArg = string.Equals(where, "Bank") ? "bank" : "pack";
            var itemNotifyBagSlot = string.Equals(where, "Bank") ? slot + 1 : slot - 22;
            if (slot2 >= 0)
            {
                MQ.Cmd($"/nomodkey /itemnotify in {itemNotifyArg}{itemNotifyBagSlot} {slot2 + 1} leftmouseup");
            }
            else
            {
                MQ.Cmd($"/nomodkey /itemnotify {itemNotifyArg}{itemNotifyBagSlot} leftmouseup");
            }

            MQ.Delay(250, "${Cursor.ID}");

            if (args.Count() > 1)
            {
                var myQuantity = MQ.Query<int>($"${{{findTlo}[={item}].Stack}}");
                if (!int.TryParse(args[1], out var requestedQuantity))
                {
                    MQ.Write($"\arYou requested a quantity of {args[1]}, and that's not a number. Grabbing all {item}s");
                }
                else if (requestedQuantity > myQuantity)
                {
                    MQ.Write($"\arYou requested {requestedQuantity} {item}s and you only have {myQuantity}. Grabbing all {item}s");
                }
                else
                {
                    MQ.Cmd($"/nomodkey /notify QuantityWnd QTYW_slider newvalue {requestedQuantity}",250);
                }
            }

            MQ.Cmd("/nomodkey /notify QuantityWnd QTYW_Accept_Button leftmouseup",50);
        }

        private static void Upgrade(List<string> args)
        {
            if (args.Count < 2)
            {
                MQ.Write("\arYou must provide the slot name and new item name to run this command");
                return;
            }

            var slotName = args[0];
            var newItem = args[1];

            if (!_invSlots.Contains(slotName))
            {
                MQ.Write($"\arInvalid slot name of {slotName}. The options are {string.Join(", ", _invSlots)}");
                return;
            }

            if (!MQ.Query<bool>($"${{FindItem[={newItem}]}}"))
            {
                MQ.Write($"\arYou do not have {newItem} in your inventory");
                return;
            }

            var distiller = "Perfected Augmentation Distiller";
            var distillerCount = MQ.Query<int>($"${{FindItemCount[={distiller}]}}");
            var curItem = MQ.Query<string>($"${{Me.Inventory[{slotName}]}}");
            var slotsWithAugs = new Dictionary<int, string>();
            for (int i = 1; i <= 6; i++)
            {
                var augInSlot = MQ.Query<string>($"${{Me.Inventory[{slotName}].AugSlot[{i}]}}");
                if (!string.Equals(augInSlot, "NULL", StringComparison.OrdinalIgnoreCase))
                {
                    slotsWithAugs.Add(i, augInSlot);
                }
            }

            var freeInvSlots = MQ.Query<int>("${Me.FreeInventory}");
            if (distillerCount < slotsWithAugs.Count())
            {
                MQ.Write($"\arYou do not have enough {distiller}s in your inventory to de-aug {curItem}");
                return;
            }

            if(freeInvSlots < slotsWithAugs.Count())
            {
                MQ.Write("\arYou do not have enough free inventory space to hold removed augs");
                return;
            }

            MQ.Cmd($"/nomodkey /itemnotify \"${{Me.Inventory[{slotName}]}}\" rightmouseheld");

            foreach(var kvp in slotsWithAugs)
            {
                MQ.Cmd($"/nomodkey /notify ItemDisplayWindow IDW_Socket_Slot_{kvp.Key}_Item leftmouseup");
                MQ.Delay(500);
                e3util.ClickYesNo(true);
                MQ.Delay(3000, "${Cursor.ID}");
                e3util.ClearCursor();
            }

            MQ.Cmd("/nomodkey /keypress esc");
            MQ.Cmd($"/nomodkey /itemnotify \"${{FindItem[={newItem}]}}\" rightmouseheld",500);
            

            foreach (var kvp in slotsWithAugs)
            {
                if (!e3util.PickUpItemViaFindItemTlo(kvp.Value))
                {
                    // we know it's here since we just took it out of our last item
                    bool foundItem = false;
                    for (int i = 1; i <= 10; i++)
                    {
                        if (foundItem)
                        {
                            break;
                        }

                        // first check to see if the slot has our aug in it
                        var item = MQ.Query<string>($"${{Me.Inventory[pack{i}]}}");
                        if (string.Equals(item, kvp.Value))
                        {
                            MQ.Cmd($"/nomodkey /itemnotify pack{i} leftmouseup");
                            break;
                        }

                        // then check inside the container
                        var containerSlots = MQ.Query<int>($"${{Me.Inventory[pack{i}].Container}}");
                        for (int j = 1; j <= containerSlots; j++)
                        {
                            item = MQ.Query<string>($"${{Me.Inventory[pack{i}].Item[{j}]}}");
                            if (string.Equals(item, kvp.Value))
                            {
                                MQ.Cmd($"/nomodkey /itemnotify in pack{i} {j} leftmouseup");
                                foundItem = true;
                                break;
                            }
                        }
                    }
                }

                MQ.Cmd($"/nomodkey /notify ItemDisplayWindow IDW_Socket_Slot_{kvp.Key}_Item leftmouseup");
                MQ.Delay(500);
                e3util.ClickYesNo(true);
                MQ.Delay(3000, "!${Cursor.ID}");
                MQ.Delay(500);
            }

            MQ.Delay(250);
            MQ.Cmd($"/exchange \"{newItem}\" {slotName}");
            MQ.Cmd("/nomodkey /keypress esc");
            MQ.Write("\agUpgrade complete!");
        }

        static void RegisterEvents()
        {
            EventProcessor.RegisterCommand("/fds", (x) =>
            {

                if (e3util.FilterMe(x)) return;
                
                List<string> validReportChannels = new List<string>() { "/g", "/gu", "/say", "/rsay", "/gsay", "/rs", "/bc" };

                string channel = "/gsay";
                if(e3util.IsEQLive())
                {
                    channel = "/gsay";
                }
                else
                {
					if (x.args.Count > 1 && validReportChannels.Contains(x.args[1], StringComparer.OrdinalIgnoreCase))
					{

						channel = x.args[1];
					}
				}
              
                if (x.args.Count > 0)
                {
                    string slot = x.args[0];
                    if(slot=="all")
                    {
                        foreach (string tslot in _invSlots)
                        {
							if (FDSPrint(tslot, channel))
							{
								//if (!x.args.Contains("group"))
								//{
								//	//E3.Bots.BroadcastCommandToGroup($"/fds {slot} {channel} group", x);
								//}
							}
						}

                    }
                    else
                    {
						if (FDSPrint(slot, channel))
						{
							if (!x.args.Contains("group"))
							{
								E3.Bots.BroadcastCommandToGroup($"/fds {slot} {channel} group", x);
							}
						}
					}
                   
                }

            });
            EventProcessor.RegisterCommand("/fic", (x) =>
            {
                //check to make sure there is something to look for
                if (x.args.Count == 0) return;

                string itemName = x.args[0];
                if (x.args.Count == 1)
                {
                    E3.Bots.BroadcastCommand($"/fic \"{itemName}\" all");
                }

                if (!e3util.FilterMe(x))
                {
                    FindItemCompact(itemName);
                }

            });
            EventProcessor.RegisterCommand("/finditem", (x) =>
            {
                //check to make sure there is something to look for
                if (x.args.Count == 0) return;

                string itemName = x.args[0];
                if (x.args.Count == 1)
                {
                    E3.Bots.BroadcastCommand($"/finditem \"{itemName}\" all");
                }

                if (!e3util.FilterMe(x))
                {
                    FindItemCompact(itemName);
                }

            });
            EventProcessor.RegisterCommand("/e3getfrombank", (x) => GetFrom("Bank", x.args));
            EventProcessor.RegisterCommand("/e3getfrominv", (x) => GetFrom("Inventory", x.args));
            EventProcessor.RegisterCommand("/upgrade", (x) => Upgrade(x.args));
            //restock generic reusable items from vendors
            
        }
        [ClassInvoke(Data.Class.All)]
        public static void CheckTradeAccept() 
        {
            if (!e3util.ShouldCheck(ref _nextTradeCheck, _nextTradeCheckInterval)) return;

            //don't trade if its the foreground window and WaitForTrade is set to true in the INI
            if (e3util.IsManualControl() && E3.GeneralSettings.AutoTrade_WaitForTrade) return;

            bool tradeWndOpen = MQ.Query<bool>($"${{Window[TradeWnd].Open}}");
            bool doTrade = false;

            if (!tradeWndOpen)
            {
                return;
            }
            else
            {
                
                string traderName = MQ.Query<string>($"${{Window[TradeWnd].Child[TRDW_HisName].Text}}");
                Spawn trader;
                
                if (_spawns.TryByName(traderName,out trader))
                {
                   
                    if (E3.GeneralSettings.AutoTrade_All)
                    {
                        doTrade = true;
                     
                    }
                    else if (E3.GeneralSettings.AutoTrade_Guild)
                    {
                        if (MQ.Query<bool>($"${{Spawn[id {trader.ID}].Guild.Equal[${{Me.Guild}}]}}"))
                        {
                            doTrade = true;
                        }
                    }
                    else if (E3.GeneralSettings.AutoTrade_Raid)
                    {
                        if (MQ.Query<bool>($"${{Raid.Member[{trader.DisplayName}]}}"))
                        {
                            doTrade = true;
                        }
                    }
                    else if (E3.GeneralSettings.AutoTrade_Bots)
                    {
                        if (E3.Bots.BotsConnected().Contains(trader.CleanName))
                        {
                            doTrade = true;
                        }
                    }
                    else if (E3.GeneralSettings.AutoTrade_Group)
                    {
                        if (Basics.GroupMembers.Contains(trader.ID))
                        {
                            doTrade = true;
                        }
                    }

                    if (doTrade)
                    {
                        MQ.Cmd($"/nomodkey /notify TradeWnd TRDW_Trade_Button leftmouseup", 500);
                    }
                    else if (Basics.InCombat())
                    {
                        MQ.Cmd($"/nomodkey /notify TradeWnd TRDW_Cancel_Button leftmouseup");
                        E3.Bots.Broadcast($"Cancelling trade with {trader.CleanName} because of combat");
                    }

                }
            }
        }
    }
}
