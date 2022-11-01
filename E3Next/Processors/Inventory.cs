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
        private static IMQ MQ = E3.Mq;
        private static ISpawns _spawns = E3.Spawns;
        private static readonly List<string> _fdsSlots = new List<string>() { "charm", "leftear", "head", "face", "rightear", "neck", "shoulder", "arms", "back", "leftwrist", "rightwrist", "ranged", "hands", "mainhand", "offhand", "leftfinger", "rightfinger", "chest", "legs", "feet", "waist", "powersource", "ammo", "fingers", "wrists", "ears" };

        [SubSystemInit]
        public static void Init()
        {
            RegisterEvents();
        }
        private static bool FDSPrint(string slot)
        {

            if (_fdsSlots.Contains(slot))
            {
                if (slot == "fingers")
                {
                    MQ.Cmd("/g Left:${InvSlot[leftfinger].Item.ItemLink[CLICKABLE]}   Right:${InvSlot[rightfinger].Item.ItemLink[CLICKABLE]} ");
                }
                else if (slot == "wrists")
                {
                    MQ.Cmd("/g Left:${InvSlot[leftwrist].Item.ItemLink[CLICKABLE]}   Right:${InvSlot[rightwrist].Item.ItemLink[CLICKABLE]} ");

                }
                else if (slot == "ears")
                {
                    MQ.Cmd("/g Left:${InvSlot[leftear].Item.ItemLink[CLICKABLE]}   Right:${InvSlot[rightear].Item.ItemLink[CLICKABLE]} ");
                }
                else
                {
                    MQ.Cmd($"/g {slot}:${{InvSlot[{slot}].Item.ItemLink[CLICKABLE]}}");

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
                string name = MQ.Query<string>($"${{InvSlot[{i}].Item}}");

                if (MQ.Query<bool>($"${{InvSlot[{i}].Item.Name.Find[{itemName}]}}"))
                {
                    Int32 stackCount = MQ.Query<Int32>($"${{InvSlot[{i}].Item.Stack}}");
                    totalItems += stackCount;
                    report.Add($"\ag[Worn] \ap{name}\aw ({stackCount})");
                }
                Int32 augCount = MQ.Query<Int32>($"${{InvSlot[{i}].Item.Augs}}");
                if (augCount > 0)
                {
                    for (int a = 1; a <= 6; a++)
                    {
                        string augname = MQ.Query<string>($"${{InvSlot[{i}].Item.AugSlot[{a}].Name}}");

                        if (augname.IndexOf(itemName, 0, StringComparison.OrdinalIgnoreCase) > -1)
                        {
                            totalItems += 1;
                            report.Add($"\ag[Worn] \ap{name}-\a-o{augname} \aw(aug-slot[{a}])");
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
                                report.Add($"\ag[Pack] \ap{bagItem}- \awbag({i}) slot({e}) count({stackCount})");
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
                    report.Add($"\ag[Bank] \ap{bankItemName} \aw- slot({i}) count({bankStack})");
                }


                //look through container
                Int32 ContainerSlots = MQ.Query<Int32>($"${{Me.Bank[{i}].Container}}");
                for (int e = 1; e <= ContainerSlots; e++)
                {
                    bankItemName = MQ.Query<string>($"${{Me.Bank[{i}].Item[{e}].Name}}");

                    if (bankItemName.IndexOf(itemName, 0, StringComparison.OrdinalIgnoreCase) > -1)
                    {
                        Int32 bankStack = MQ.Query<Int32>($"${{Me.Bank[{i}].Item[{e}].Stack}}");
                        report.Add($"\ag[Bank] \ap{bankItemName} \aw- slot({i}) bagslot({e}) count({bankStack})");
                    }
                    Int32 augCount = MQ.Query<Int32>($"${{Me.Bank[{i}].Item[{e}].Augs}}");
                    if (augCount > 0)
                    {
                        for (int a = 1; a <= 6; a++)
                        {
                            string augname = MQ.Query<string>($"${{Bank[{i}].Item[{e}].AugSlot[{a}].Name}}");

                            if (augname.IndexOf(itemName, 0, StringComparison.OrdinalIgnoreCase) > -1)
                            {
                                totalItems += 1;
                                report.Add($"\ag[Bank-Aug-Worn] \ap{bankItemName}-\ao{augname} slot({i}) bagslot({e}) (aug-slot[{a}])");
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
        static void RegisterEvents()
        {
            EventProcessor.RegisterCommand("/fds", (x) =>
            {

                if (x.args.Count > 0)
                {
                    string slot = x.args[0];
                    if (FDSPrint(slot))
                    {
                        if (x.args.Count == 1)
                        {
                            E3.Bots.BroadcastCommandToGroup($"/fds {slot} group");
                        }

                    }
                }

            });
            EventProcessor.RegisterCommand("/fic", (x) =>
            {
                string itemName = x.args[0];
                if (x.args.Count == 1)
                {
                    E3.Bots.BroadcastCommandToGroup($"/fic \"{itemName}\" all", x);
                }

                if (!e3util.FilterMe(x))
                {
                    FindItemCompact(itemName);
                }

            });
            EventProcessor.RegisterCommand("/finditem", (x) =>
            {
                string itemName = x.args[0];
                if (x.args.Count == 1)
                {
                    E3.Bots.BroadcastCommandToGroup($"/finditem \"{itemName}\" all", x);
                }

                if (!e3util.FilterMe(x))
                {
                    FindItemCompact(itemName);
                }

            });
            EventProcessor.RegisterCommand("/getfrombank", (x) =>
            {
                var args = x.args;
                if (args.Count == 0)
                {
                    MQ.Write("\arYou need to tell me what to get!");
                    return;
                }

                var item = args[0];
                if (!MQ.Query<bool>("${Window[BigBankWnd]}"))
                {
                    MQ.Write("\arYou need to open the bank window before issuing this command");
                    return;
                }

                if (!MQ.Query<bool>($"${{FindItemBank[={item}]}}"))
                {
                    MQ.Write($"\arYou do not have any {item}s in the bank");
                    return;
                }

                var slot = MQ.Query<int>($"${{FindItemBank[={item}].ItemSlot}}");
                var slot2 = MQ.Query<int>($"${{FindItemBank[={item}].ItemSlot2}}");

                // different syntax for if the item is in a bag vs if it's not
                if (slot2 >= 0)
                {
                    MQ.Cmd($"/itemnotify bank{slot + 1} rightmouseup");
                    MQ.Delay(100);
                    MQ.Cmd($"/itemnotify in bank{slot + 1} {slot2 + 1} leftmouseup");
                }
                else
                {
                    MQ.Cmd($"/itemnotify bank{slot + 1} leftmouseup");
                }

                MQ.Delay(250);

                if (args.Count() > 1)
                {
                    var myQuantity = MQ.Query<int>($"${{FindItemBank[={item}].Stack}}");
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
                        MQ.Cmd($"/notify QuantityWnd QTYW_slider newvalue {requestedQuantity}");
                        MQ.Delay(250);
                    }
                }

                MQ.Cmd("/notify QuantityWnd QTYW_Accept_Button leftmouseup");
                MQ.Delay(50);
                MQ.Cmd($"/itemnotify bank{slot + 1} rightmouseup");
            });
            //restock generic reusable items from vendors
            _ = EventProcessor.RegisterCommand("/restock", (x) =>
              {
                  string toEat = E3._characterSettings.Misc_AutoFood;
                  string toDrink = E3._characterSettings.Misc_AutoDrink;
                  int toEatQty = MQ.Query<int>($"${{FindItemCount[{toEat}]}}");
                  int toDrinkQty = MQ.Query<int>($"${{FindItemCount[{toDrink}]}}");

                  MQ.Write($"\agInitiating restock for {toEat} and {toDrink}");
                  if (toEatQty >= 1000 && toDrinkQty >= 1000)
                  {
                      MQ.Write($"\arYou already have more than a stack of {toEat} and {toDrink}! Skipping restock. ");
                      return;
                  } else
                  {
                      int zoneID = E3._zoneID;
                      int vendorID = 0;

                      //zoneID 345 = Guild Hall
                      if (zoneID == 345)
                      {
                          string vendorName = "Yenny Werlikanin";
                          vendorID = MQ.Query<int>($"${{Spawn[{vendorName}].ID}}");
                          
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
                                  } else
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
                          } else
                          {
                              MQ.Write($"\arNo valid vendor ID available.");
                          }


                      }
                      //zoneID 202 = Plane of Knowledge
                      if (zoneID == 202)
                      {
                          string vendorName = "Vori";
                          vendorID = MQ.Query<int>($"${{Spawn[{vendorName}].ID}}");

                          if (vendorID > 0)
                          {
                              Casting.TrueTarget(vendorID);
                              e3util.NavToSpawnID(vendorID);
                              e3util.OpenMerchant();

                              if (toEatQty < 1000)
                              {
                                  int eatQtyNeeded = 1000 - toEatQty;
                                  Buy.BuyItem(toEat, eatQtyNeeded);
                              }

                              if (toDrinkQty < 1000)
                              {
                                  int drinkQtyNeeded = 1000 - toDrinkQty;
                                  Buy.BuyItem(toDrink, drinkQtyNeeded);
                              }


                              e3util.CloseMerchant();
                          }
                          else
                          {
                              MQ.Write($"\arNo valid vendor ID available.");
                          }


                      }
                  }
              });
        }
    }
}
