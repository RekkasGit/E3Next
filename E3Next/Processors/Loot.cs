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
using System.Net.Configuration;

namespace E3Core.Processors
{
    public static class Loot
    {
        public static Logging _log = E3.Log;
        private static IMQ MQ = E3.Mq;
        private static ISpawns _spawns = E3.Spawns;
        private static bool _shouldLoot = false;
        private static Int32 _seekRadius = 50;
        private static HashSet<Int32> _unlootableCorpses = new HashSet<int>();
        private static bool _fullInventoryAlert = false;
        private static bool _lootOnlyStackable = false;
        private static Int32 _lootOnlyStackableValue = 1;
        private static bool _lootOnlyStackableAllTradeSkills = false;
        private static bool _lootOnlyStackableCommonTradeSkills = false;

        private static Int64 _nextLootCheck = 0;
        private static Int64 _nextLootCheckInterval = 1000;

        [SubSystemInit]
        public static void Init()
        {
            RegisterEvents();

            _shouldLoot =E3.CharacterSettings.Misc_AutoLootEnabled;
            _seekRadius = E3.GeneralSettings.Loot_CorpseSeekRadius;
            _lootOnlyStackable = E3.GeneralSettings.Loot_OnlyStackableEnabled;
            _lootOnlyStackableValue = E3.GeneralSettings.Loot_OnlyStackableValueGreaterThanInCopper;
            _lootOnlyStackableAllTradeSkills = E3.GeneralSettings.Loot_OnlyStackableAllTradeSkillItems;
            _lootOnlyStackableCommonTradeSkills = E3.GeneralSettings.Loot_OnlyStackableOnlyCommonTradeSkillItems;

            LootDataFile.LoadData();
        }
        public static void Reset()
        {
            _unlootableCorpses.Clear();
            //have to be careful to not get pointers to a settings file that may be auto updated, so refersh the pointers after every reset
            _shouldLoot = E3.CharacterSettings.Misc_AutoLootEnabled;
            _seekRadius = E3.GeneralSettings.Loot_CorpseSeekRadius;
            _lootOnlyStackable = E3.GeneralSettings.Loot_OnlyStackableEnabled;
            _lootOnlyStackableValue = E3.GeneralSettings.Loot_OnlyStackableValueGreaterThanInCopper;
            _lootOnlyStackableAllTradeSkills = E3.GeneralSettings.Loot_OnlyStackableAllTradeSkillItems;
            _lootOnlyStackableCommonTradeSkills = E3.GeneralSettings.Loot_OnlyStackableOnlyCommonTradeSkillItems;
        }
        private static void RegisterEvents()
        {
            EventProcessor.RegisterCommand("/E3LootAdd", (x) =>
            {
                if (x.args.Count > 1)
                {
                    //remove item from all collections and add to desired collection
                    if(x.args[1]=="KEEP")
                    {
                        LootDataFile.Sell.Remove(x.args[0]);
                        LootDataFile.Skip.Remove(x.args[0]);
                        LootDataFile.Keep.Add(x.args[0]);

                    }
                    else if(x.args[1]=="SELL")
                    {
                        LootDataFile.Keep.Remove(x.args[0]);
                        LootDataFile.Skip.Remove(x.args[0]);
                        LootDataFile.Sell.Add(x.args[0]);
                    }
                    else
                    {
                        LootDataFile.Keep.Remove(x.args[0]);
                        LootDataFile.Sell.Remove(x.args[0]);
                        LootDataFile.Skip.Add(x.args[0]);
                    }
                } 
            });

            EventProcessor.RegisterCommand("/looton", (x) =>
            {
                if (x.args.Count > 0 && !x.args[0].Equals(E3.CurrentName, StringComparison.OrdinalIgnoreCase))
                {
                    E3.Bots.BroadcastCommandToPerson(x.args[0], "/looton");
                }
                else
                {
                    //we are turning our own loot on.
                    _shouldLoot = true;
                    E3.Bots.Broadcast("\agTurning on Loot.");
                }
            });
            EventProcessor.RegisterCommand("/lootoff", (x) =>
            {
                if (x.args.Count > 0 && !x.args[0].Equals(E3.CurrentName, StringComparison.OrdinalIgnoreCase))
                {
                    E3.Bots.BroadcastCommandToPerson(x.args[0], "/lootoff");
                }
                else
                {
                    //we are turning our own loot on.
                    _shouldLoot = false;
                    E3.Bots.Broadcast("\agTurning Off Loot.");
                }
            });

            EventProcessor.RegisterCommand("/lootkeep", (x) =>
            {
                string cursorItem = MQ.Query<string>("${Cursor.Name}");

                if (cursorItem.Equals("NULL", StringComparison.OrdinalIgnoreCase) || String.IsNullOrWhiteSpace(cursorItem))
                {
                    MQ.Write("You don't have an item on your cursor, cannot modify the loot file.");
                    MQ.Write("Place an item on your cursor and then give the proper /lootkeep, /lootsell, /lootskip command");
                    return;
                }

                LootDataFile.Keep.Remove(cursorItem);
                LootDataFile.Sell.Remove(cursorItem);
                LootDataFile.Skip.Remove(cursorItem);
                LootDataFile.Keep.Add(cursorItem);
                
                MQ.Write($"\aoSetting {cursorItem} to KEEP");
                E3.Bots.BroadcastCommand($"/E3LootAdd \"{cursorItem}\" KEEP");
                LootDataFile.SaveData();

                MQ.Cmd("/autoinv");
            });

            EventProcessor.RegisterCommand("/lootskip", (x) =>
            {
                string cursorItem = MQ.Query<string>("${Cursor.Name}");

                if (cursorItem.Equals("NULL", StringComparison.OrdinalIgnoreCase) || String.IsNullOrWhiteSpace(cursorItem))
                {
                    MQ.Write("You don't have an item on your cursor, cannot modify the loot file.");
                    MQ.Write("Place an item on your cursor and then give the proper /lootkeep, /lootsell, /lootskip command");
                    return;
                }

                LootDataFile.Keep.Remove(cursorItem);
                LootDataFile.Sell.Remove(cursorItem);
                LootDataFile.Skip.Remove(cursorItem);
                LootDataFile.Skip.Add(cursorItem);

                MQ.Write($"\arSetting {cursorItem} to SKIP");
                E3.Bots.BroadcastCommand($"/E3LootAdd \"{cursorItem}\" SKIP");
                LootDataFile.SaveData();
            });

            EventProcessor.RegisterCommand("/lootsell", (x) =>
            {
                string cursorItem = MQ.Query<string>("${Cursor.Name}");

                if (cursorItem.Equals("NULL", StringComparison.OrdinalIgnoreCase) || String.IsNullOrWhiteSpace(cursorItem))
                {
                    MQ.Write("You don't have an item on your cursor, cannot modify the loot file.");
                    MQ.Write("Place an item on your cursor and then give the proper /lootkeep, /lootsell, /lootskip command");
                    return;
                }

                LootDataFile.Keep.Remove(cursorItem);
                LootDataFile.Sell.Remove(cursorItem);
                LootDataFile.Skip.Remove(cursorItem);
                LootDataFile.Sell.Add(cursorItem);
                
                MQ.Write($"\agSetting {cursorItem} to SELL");
                E3.Bots.BroadcastCommand($"/E3LootAdd \"{cursorItem}\" SELL");
                LootDataFile.SaveData();

                MQ.Cmd("/autoinv");
            });
        }

        public static void Process()
        {
            if (E3.IsInvis) return;
            if (!e3util.ShouldCheck(ref _nextLootCheck, _nextLootCheckInterval)) return;

            if (!_shouldLoot) return;
            if(!Assist._isAssisting)
            {
                
                if(!Basics.InCombat())
                {
                    LootArea();
                }
            }
        }
        private static void LootArea()
        {
            Double startX = MQ.Query<Double>("${Me.X}");
            Double startY = MQ.Query<Double>("${Me.Y}");
            Double startZ = MQ.Query<Double>("${Me.Z}");

            List<Spawn> corpses = new List<Spawn>();
            foreach (var spawn in _spawns.Get())
            {
                //only player corpses have a Deity
                if (spawn.Distance3D < _seekRadius && spawn.DeityID == 0 && spawn.TypeDesc == "Corpse")
                {
                    if (!Zoning.CurrentZone.IsSafeZone)
                    {
                        if (!_unlootableCorpses.Contains(spawn.ID))
                        {
                            corpses.Add(spawn);
                        }
                    }
                }
            }
            if (corpses.Count==0)
            {
                return;
            }
                //sort all the corpses, removing the ones we cannot loot
             corpses = corpses.OrderBy(x => x.Distance).ToList();

            if (corpses.Count > 0)
            {
                MQ.Cmd("/squelch /hidecorpse looted");
                MQ.Delay(100);


                //lets check if we can loot.
                if (MQ.Query<bool>("${Stick.Active}")) MQ.Cmd("/squelch /stick off");
                if (MQ.Query<bool>("${AdvPath.Following}")) MQ.Cmd("/squelch /afollow off ");
                
                
                foreach(var c in corpses)
                {
                    
                   
                    Casting.TrueTarget(c.ID);
                    MQ.Delay(2000, "${Target.ID}");
                   
                    if(MQ.Query<bool>("${Target.ID}"))
                    {
                        e3util.TryMoveToTarget();

                        LootCorpse(c);
                        if (MQ.Query<bool>("${Window[LootWnd].Open}"))
                        {
                            MQ.Cmd("/nomodkey /notify LootWnd DoneButton leftmouseup");
                        }
                        MQ.Delay(300);
                    }
                    
                }
            }
        }
        public static void LootCorpse(Spawn corpse, bool bypassLootSettings = false)
        {
            
            Int32 freeInventorySlots = MQ.Query<Int32>("${Me.FreeInventory}");
            bool importantItem = false;

            if(!_fullInventoryAlert && freeInventorySlots<1)
            {
                _fullInventoryAlert = true;
                E3.Bots.Broadcast("\arMy inventory is full! \awI will continue to link items on corpses, but cannot loot anything else.");
                MQ.Cmd("/beep");
              
            }
           
            MQ.Cmd("/loot");
            MQ.Delay(1000, "${Window[LootWnd].Open}");
            MQ.Delay(100);
            if(!MQ.Query<bool>("${Window[LootWnd].Open}"))
            {
                MQ.Write($"\arERROR, Loot Window not opening, adding {corpse.CleanName}-{corpse.ID} to ignore corpse list.");
                if(!_unlootableCorpses.Contains(corpse.ID))
                {
                    _unlootableCorpses.Add(corpse.ID);
                }
                return;

            }
            MQ.Delay(500, "${Corpse.Items}");
            MQ.Delay(300);//wait a little longer to let the items finish populating.
            Int32 corpseItems = MQ.Query<Int32>("${Corpse.Items}");

            if (corpseItems == 0)
            {
                //no items on the corpse, kick out

                return;
            }

            for(Int32 i =1;i<=corpseItems;i++)
            {
                //lets try and loot them.
                importantItem = false;

                MQ.Delay(1000, $"${{Corpse.Item[{i}].ID}}");

                string corpseItem = MQ.Query<string>($"${{Corpse.Item[{i}].Name}}");
                bool stackable = MQ.Query<bool>($"${{Corpse.Item[{i}].Stackable}}");
                bool nodrop = MQ.Query<bool>($"${{Corpse.Item[{i}].NoDrop}}");
                Int32 itemValue = MQ.Query<Int32>($"${{Corpse.Item[{i}].Value}}");
                Int32 stackCount = MQ.Query<Int32>($"${{Corpse.Item[{i}].Stack}}");

                if (_lootOnlyStackable)
                {
                    if (stackable && !nodrop)
                    {
                        //check if in our always loot. 
                        if (E3.GeneralSettings.Loot_OnlyStackableAlwaysLoot.Contains(corpseItem, StringComparer.OrdinalIgnoreCase))
                        {
                            importantItem = true;
                        }
                        if (!importantItem && _lootOnlyStackableCommonTradeSkills)
                        {
                            importantItem = true;
                        }
                        if (!importantItem && itemValue >= _lootOnlyStackableValue)
                        {
                            importantItem = true;
                        }
                        if (!importantItem && _lootOnlyStackableAllTradeSkills)
                        {
                            if (corpseItem.Contains(" Pelt")) importantItem = true;
                            if (corpseItem.Contains(" Silk")) importantItem = true;
                            if (corpseItem.Contains(" Ore")) importantItem = true;
                        }

                        if (!importantItem && itemValue >= E3.GeneralSettings.Loot_OnlyStackableValueGreaterThanInCopper)
                        {
                            importantItem = true;
                        }
                    }
                }
                else
                {
                    //use normal loot settings
                    bool foundInFile = false;
                    if (LootDataFile.Keep.Contains(corpseItem) || LootDataFile.Sell.Contains(corpseItem))
                    {
                        importantItem = true;
                        foundInFile = true;
                    }
                    else if(LootDataFile.Skip.Contains(corpseItem))
                    {
                        importantItem = false;
                        foundInFile = true;
                    }
                    if(!foundInFile)
                    {
                        importantItem = true;
                        LootDataFile.Keep.Add(corpseItem);
                        E3.Bots.BroadcastCommandToGroup($"/E3LootAdd \"{corpseItem}\" KEEP");
                        LootDataFile.SaveData();
                    }

                }

                //check if its lore
                bool isLore = MQ.Query<bool>($"${{Corpse.Item[{i}].Lore}}");
                //if in bank or on our person
                bool weHaveItem = MQ.Query<bool>($"${{FindItemCount[={corpseItem}]}}");
                bool weHaveItemInBank = MQ.Query<bool>($"${{FindItemBankCount[={corpseItem}]}}");
                if (isLore && (weHaveItem || weHaveItemInBank))
                {
                    importantItem = false;
                }

                //stackable but we don't have room and don't have the item yet
                if(freeInventorySlots<1 && stackable && !weHaveItem)
                {
                    importantItem = false;
                }

                //stackable but we don't have room but we already have an item, lets see if we have room.
                if (freeInventorySlots < 1 && stackable && weHaveItem)
                {
                    //does it have free stacks?
                    if(FoundStackableFitInInventory(corpseItem,stackCount))
                    {
                        importantItem = true;
                    }
                }

                if (importantItem || bypassLootSettings)
                {
                    //lets loot it if we can!
                    MQ.Cmd($"/nomodkey /itemnotify loot{i} rightmouseup");
                    MQ.Delay(300);
                    bool qtyWindowUp = MQ.Query<bool>("${Window[QuantityWnd].Open}");
                    if(qtyWindowUp)
                    {
                        MQ.Cmd($"/nomodkey /notify QuantityWnd QTYW_Accept_Button leftmouseup");
                        MQ.Delay(300);
                    }
                }
               
            }
            if (MQ.Query<Int32>("${Corpse.Items}")>0)
            {   //link what is ever left over.
                //should we should notify if we have not looted.
                if (!String.IsNullOrWhiteSpace(E3.GeneralSettings.Loot_LinkChannel))
                {
                    PrintLink($"{E3.GeneralSettings.Loot_LinkChannel} {corpse.ID} - ");
                }
            }

        }

        private static void PrintLink(string message)
        {
            MQ.Cmd("/nomodkey /keypress /");
            foreach(char c in message)
            {
                if(c==' ')
                {
                    MQ.Cmd($"/nomodkey /keypress space chat");
                }
                else
                {
                    MQ.Cmd($"/nomodkey /keypress {c} chat");
                }
            }
            MQ.Delay(100);
            MQ.Cmd("/nomodkey /notify LootWnd BroadcastButton leftmouseup");
            MQ.Delay(100);
            MQ.Cmd("/nomodkey /keypress enter chat");
            MQ.Delay(100);
        }
        private static bool FoundStackableFitInInventory(string corpseItem, Int32 count)
        {
            //scan through our inventory looking for an item with a stackable
            for(Int32 i =1;i<=10;i++)
            {
                bool SlotExists = MQ.Query<bool>($"${{Me.Inventory[pack{i}]}}");
                if(SlotExists)
                {
                    Int32 slotsInInvetoryLost = MQ.Query<Int32>($"${{Me.Inventory[pack{i}].Container}}");

                    if(slotsInInvetoryLost>0)
                    {
                        for(Int32 e=1;e<=slotsInInvetoryLost;e++)
                        {
                            //${Me.Inventory[${itemSlot}].Item[${j}].Name.Equal[${itemName}]}
                            String itemName = MQ.Query<String>($"${{Me.Inventory[pack{i}].Item[{e}]}}");
                            if (itemName == "NULL")
                            {
                                continue;
                            }
                            if (itemName==corpseItem)
                            {
                                //its the item we are looking for, does it have stackable 
                                Int32 freeStack = MQ.Query<Int32>($"${{Me.Inventory[pack{i}].Item[{e}].FreeStack}}");

                                if (freeStack <= count)
                                {
                                    return true;
                                }
                            }
                        }
                    }
                    else
                    {
                        //in the root 
                        string itemName = MQ.Query<String>($"${{Me.Inventory[pack{i}].Item]}}");//${Me.Inventory[pack${i}].Item.Value}
                        if (itemName == corpseItem)
                        {
                            //its the item we are looking for, does it have stackable 
                            Int32 freeStack = MQ.Query<Int32>($"${{Me.Inventory[pack{i}].Item.FreeStack}}");

                            if (freeStack <= count)
                            {
                                return true;
                            }

                        }
                    }

                }

            }
            return false;
        }
    }
}
