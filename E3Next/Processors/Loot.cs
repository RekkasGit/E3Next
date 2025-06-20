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
using System.ServiceModel.Configuration;
using System.ServiceModel.PeerResolvers;
using System.Windows.Forms;

namespace E3Core.Processors
{
    public static class Loot
    {
        public static Logging _log = E3.Log;
        private static IMQ MQ = E3.MQ;
        private static ISpawns _spawns = E3.Spawns;
      
        private static HashSet<Int32> _unlootableCorpses = new HashSet<int>();
        private static bool _fullInventoryAlert = false;
        private static Int64 _nextLootCheck = 0;
        private static Int64 _nextLootCheckInterval = 1000;
        private static CircularBuffer<Int32> _lootCommanderAssisngedCorpsesToLoot = new CircularBuffer<int>(100);
        private static Dictionary<string, List<Int32>> _lootCommanderAssignmentBuilder = new Dictionary<string, List<int>>();
		public static Settings.FeatureSettings.LootStackable LootStackableSettings = null;

		[SubSystemInit]
        public static void Loot_Init()
        {
            RegisterEvents();
            try
            {
                LootDataFile.LoadData();

            }
            catch (Exception ex)
            {
                MQ.Write("Exception loading Loot Data file. Loot data is not available. Error:"+ex.Message + " stack:" + ex.StackTrace);
               
            }
            try
            {
                LootStackableSettings = new LootStackable();
				LootStackableSettings.LoadData();
            }
            catch(Exception ex)
			{
            
				MQ.Write("Exception loading Loot Stackable Data file. message:"+ex.Message + " stack:"+ex.StackTrace);
				

			}
        }
        public static void Reset()
        {
            _unlootableCorpses.Clear();
        }
        private static void RegisterEvents()
        {

			//EventProcessor.RegisterCommand("/e3lootcommand", (x) =>
			//{
			//	if (x.args.Count > 1 && !x.args[0].Equals(E3.CurrentName, StringComparison.OrdinalIgnoreCase))
			//	{
   //                 //send command to person
			//		E3.Bots.BroadcastCommandToPerson(x.args[0], $"/lootcommand {x.args[0]} \"{x.args[1]}\"");
			//	}
			//	else
			//	{
   //                 //process command
	
   //                 if(x.args.Count>1)
   //                 {
   //                     string corpseIdsString = x.args[1];
   //                     E3.Bots.Broadcast("LootCommander Assigning to loot:" + corpseIdsString);
   //                     List<Int32> corpseIds = new List<int>();
   //                     e3util.StringsToNumbers(corpseIdsString, ',', corpseIds);
   //                     foreach(var corpseId in corpseIds)
   //                     {
   //                         _lootCommanderAssisngedCorpsesToLoot.PushBack(corpseId);
   //                     }
   //                 }
   //			}
			//});


            EventProcessor.RegisterCommand("/E3LootStackAdd", (x) =>
            {
                if(x.args.Count>0)
                {
					string item = x.args[0];

					if (!E3.GeneralSettings.Loot_OnlyStackableAlwaysLoot.Contains(item))
					{
						E3.GeneralSettings.Loot_OnlyStackableAlwaysLoot.Add(item);
					}
					

				}
                else
                {
					string cursorItem = MQ.Query<string>("${Cursor.Name}");
				
					if (cursorItem.Equals("NULL", StringComparison.OrdinalIgnoreCase) || String.IsNullOrWhiteSpace(cursorItem))
					{
						MQ.Write("You don't have an item on your cursor, cannot modify the loot file.");
						MQ.Write("Place an item on your cursor and then give the proper /lootkeep, /lootsell, /lootskip /lootdestroy command");
						return;
					}
					if (!E3.GeneralSettings.Loot_OnlyStackableAlwaysLoot.Contains(cursorItem))
					{
						E3.GeneralSettings.Loot_OnlyStackableAlwaysLoot.Add(cursorItem);
					}
					

					MQ.Write($"\aoSetting {cursorItem} to KEEP");
					E3.Bots.BroadcastCommand($"/E3LootStackAdd \"{cursorItem}\"");
			
					e3util.ClearCursor();
				}
			});
			EventProcessor.RegisterCommand("/E3LootAdd", (x) =>
            {
                if (x.args.Count > 1)
				{
					//remove item from all collections and add to desired collection
					if (x.args[1]=="KEEP")
                    {
                      
                        LootDataFile.Sell.Remove(x.args[0]);
                        LootDataFile.Skip.Remove(x.args[0]);
						LootDataFile.Destroy.Remove(x.args[0]);
						LootDataFile.Keep.Add(x.args[0]);

                    }
                    else if(x.args[1]=="SELL")
                    {
                        LootDataFile.Keep.Remove(x.args[0]);
                        LootDataFile.Skip.Remove(x.args[0]);
						LootDataFile.Destroy.Remove(x.args[0]);
						LootDataFile.Sell.Add(x.args[0]);
                    }
					else if (x.args[1] == "DESTROY")
					{
						LootDataFile.Keep.Remove(x.args[0]);
						LootDataFile.Skip.Remove(x.args[0]);
						LootDataFile.Sell.Remove(x.args[0]);
						LootDataFile.Destroy.Add(x.args[0]);
					}
					else
                    {
                        LootDataFile.Keep.Remove(x.args[0]);
                        LootDataFile.Sell.Remove(x.args[0]);
						LootDataFile.Destroy.Remove(x.args[0]);
						LootDataFile.Skip.Add(x.args[0]);
                    }
                } 
            });

            EventProcessor.RegisterCommand("/looton", (x) =>
            {
                
                if (x.args.Count >0 && E3.Bots.BotsConnected().Contains(x.args[0], StringComparer.OrdinalIgnoreCase) && !x.args[0].Equals(E3.CurrentName, StringComparison.OrdinalIgnoreCase))
                {
                    if (x.args.Count == 2 && x.args[1] == "force")
                    {
                        E3.Bots.BroadcastCommandToPerson(x.args[0], "/looton force");
                    }
                    else
                    {
                        E3.Bots.BroadcastCommandToPerson(x.args[0], "/looton");
                    }
                }
                else
                {
                    //we are turning our own loot on.
                    if (x.args.Count == 1 && x.args[0] == "force")
                    {
                        _unlootableCorpses.Clear();
                        MQ.Cmd("/hidecorpse none");
                    }
                    E3.CharacterSettings.Misc_AutoLootEnabled = true;
                    
                    E3.Bots.Broadcast("\agTurning on Loot.");
                }
            });
			EventProcessor.RegisterCommand("/e3lootall", (x) =>
			{

				if (x.args.Count > 0 && E3.Bots.BotsConnected().Contains(x.args[0], StringComparer.OrdinalIgnoreCase) && !x.args[0].Equals(E3.CurrentName, StringComparison.OrdinalIgnoreCase))
				{
                    Int32 targetID = MQ.Query<Int32>("${Target.ID}");

                    if (targetID == 0)
                    {
                        return;
                    }

					E3.Bots.BroadcastCommandToPerson(x.args[0], $"/e3lootall {targetID}");
					
				}
				else if(x.args.Count > 0 && !x.args[0].Equals(E3.CurrentName, StringComparison.OrdinalIgnoreCase))
				{
					//we are turning our own loot on.
					if (x.args.Count == 1)
					{
                        Int32 targetID = 0;
                        if(Int32.TryParse(x.args[0], out targetID))
                        {
                            if(Casting.TrueTarget(targetID))
                            {

								bool isCorpse = MQ.Query<bool>("${Target.Type.Equal[Corpse]}");

                                if (isCorpse)
                                {
									E3.Bots.Broadcast($"\agFully looting the corpse id {targetID}");
                                    if (_spawns.TryByID(targetID, out var s))
                                    {
										e3util.TryMoveToTarget();
										MQ.Delay(2250, "${Target.Distance} < 10"); // Give Time to get to Corpse 
										LootCorpse(s, false, true);
                                    }
								}
                                else
                                {
									E3.Bots.Broadcast($"\ag{targetID} is not a corpse");
								}
							}

						}
					}
				
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
                    //we are turning our own loot off.
                    E3.CharacterSettings.Misc_AutoLootEnabled = false;
                    E3.Bots.Broadcast("\agTurning Off Loot.");
                }
            });

            EventProcessor.RegisterCommand("/lootkeep", (x) =>
            {
				string cursorItem;
				if (x.args.Count > 0)
				{
					cursorItem = x.args[0];
				}
				else
				{
					cursorItem = MQ.Query<string>("${Cursor.Name}");
				}

				if (cursorItem.Equals("NULL", StringComparison.OrdinalIgnoreCase) || String.IsNullOrWhiteSpace(cursorItem))
                {
                    MQ.Write("You don't have an item on your cursor, cannot modify the loot file.");
                    MQ.Write("Place an item on your cursor and then give the proper /lootkeep, /lootsell, /lootskip /lootdestroy command");
                    return;
                }

                LootDataFile.Keep.Remove(cursorItem);
                LootDataFile.Sell.Remove(cursorItem);
                LootDataFile.Skip.Remove(cursorItem);
				LootDataFile.Destroy.Remove(cursorItem);
				LootDataFile.Keep.Add(cursorItem);
                
                MQ.Write($"\aoSetting {cursorItem} to KEEP");
                E3.Bots.BroadcastCommand($"/E3LootAdd \"{cursorItem}\" KEEP");
                LootDataFile.SaveData();

                e3util.ClearCursor();
            });

			EventProcessor.RegisterCommand("/lootdestroy", (x) =>
			{
				string cursorItem = MQ.Query<string>("${Cursor.Name}");

				if (cursorItem.Equals("NULL", StringComparison.OrdinalIgnoreCase) || String.IsNullOrWhiteSpace(cursorItem))
				{
					MQ.Write("You don't have an item on your cursor, cannot modify the loot file.");
					MQ.Write("Place an item on your cursor and then give the proper /lootkeep, /lootsell, /lootskip,/lootdestroy command");
					return;
				}

				LootDataFile.Keep.Remove(cursorItem);
				LootDataFile.Sell.Remove(cursorItem);
				LootDataFile.Skip.Remove(cursorItem);
				LootDataFile.Keep.Remove(cursorItem);
				LootDataFile.Destroy.Add(cursorItem);
			
                MQ.Write($"\aoSetting {cursorItem} to DESTROY");
				E3.Bots.BroadcastCommand($"/E3LootAdd \"{cursorItem}\" DESTROY");
				LootDataFile.SaveData();

				e3util.CursorTryDestroyItem(cursorItem);
			});

			EventProcessor.RegisterCommand("/lootskip", (x) =>
            {
				string cursorItem;
				if (x.args.Count > 0)
				{
					cursorItem = x.args[0];
				}
				else
				{
					cursorItem = MQ.Query<string>("${Cursor.Name}");
				}

				if (cursorItem.Equals("NULL", StringComparison.OrdinalIgnoreCase) || String.IsNullOrWhiteSpace(cursorItem))
                {
					//what about a param?
				

                    MQ.Write("You don't have an item on your cursor, cannot modify the loot file.");
                    MQ.Write("Place an item on your cursor and then give the proper /lootkeep, /lootsell, /lootskip command");
                    return;
                }

                LootDataFile.Keep.Remove(cursorItem);
                LootDataFile.Sell.Remove(cursorItem);
                LootDataFile.Skip.Remove(cursorItem);
				LootDataFile.Destroy.Remove(cursorItem);
				LootDataFile.Skip.Add(cursorItem);

                MQ.Write($"\arSetting {cursorItem} to SKIP");
                E3.Bots.BroadcastCommand($"/E3LootAdd \"{cursorItem}\" SKIP");
                LootDataFile.SaveData();
            });

            EventProcessor.RegisterCommand("/lootsell", (x) =>
            {
				string cursorItem;
				if (x.args.Count > 0)
				{
					cursorItem = x.args[0];
				}
				else
				{
					cursorItem = MQ.Query<string>("${Cursor.Name}");
				}

				if (cursorItem.Equals("NULL", StringComparison.OrdinalIgnoreCase) || String.IsNullOrWhiteSpace(cursorItem))
                {
                    MQ.Write("You don't have an item on your cursor, cannot modify the loot file.");
                    MQ.Write("Place an item on your cursor and then give the proper /lootkeep, /lootsell, /lootskip /lootdestroy command");
                    return;
                }

                LootDataFile.Keep.Remove(cursorItem);
                LootDataFile.Sell.Remove(cursorItem);
                LootDataFile.Skip.Remove(cursorItem);
                LootDataFile.Sell.Add(cursorItem);
                
                MQ.Write($"\agSetting {cursorItem} to SELL");
                E3.Bots.BroadcastCommand($"/E3LootAdd \"{cursorItem}\" SELL");
                LootDataFile.SaveData();

                e3util.ClearCursor();
            });
        }

        public static void Process()
        {

            if (E3.IsInvis) return;
            if (!e3util.ShouldCheck(ref _nextLootCheck, _nextLootCheckInterval)) return;

            if(!Assist.IsAssisting)
            {
 				long currentTimestamp = Core.StopWatch.ElapsedMilliseconds;

				if (!E3.CharacterSettings.Misc_AutoLootEnabled) return;

				if(Basics.AmIDead())
				{
					E3.Bots.Broadcast("I am dead, turning off autoloot");
					E3.CharacterSettings.Misc_AutoLootEnabled = false;
					return;
				}
                bool inCombat = Basics.InCombat();
                if (inCombat)
                {
                    //if individual settings and not set to loot in combat, return
					if (LootStackableSettings.Enabled && !LootStackableSettings.LootInCombat) return;
					//if not individual settings, and global loot in combat isn't on, return
                    if (!LootStackableSettings.Enabled && !E3.GeneralSettings.Loot_LootInCombat) return;
				}
                else
                {
                    if (currentTimestamp - Assist.LastAssistEndedTimestamp < E3.GeneralSettings.Loot_TimeToWaitAfterAssist) return;
                }
				LootArea();
            }
        }
  //      private static void LootCommanderAssignCorpses()
  //      {
  //          if(Zoning.CurrentZone.IsSafeZone)
  //          {
  //              return;
  //          }
		//	List<Spawn> corpses = new List<Spawn>();
  //          _spawns.RefreshList();//just in case to make sure corpse data is updated
  //          foreach (var spawn in _spawns.Get())
		//	{
		//		//only player corpses have a Deity
		//		if (spawn.Distance3D < E3.GeneralSettings.Loot_CorpseSeekRadius && spawn.DeityID == 0 && spawn.TypeDesc == "Corpse")
		//		{
			
  //                  corpses.Add(spawn);
		//		}
		//	}
  //          if(corpses.Count > 0)
  //          {
  //              //need to split these up and send the command to our looters

  //              //populate the assignment builder, and clear anything that was from before
  //              foreach(var user in E3.CharacterSettings.LootCommander_Looters)
  //              {
  //                  if(!_lootCommanderAssignmentBuilder.ContainsKey(user))
  //                  {
  //                      _lootCommanderAssignmentBuilder.Add(user, new List<int>());
  //                  }
  //                  else
  //                  {
  //                      _lootCommanderAssignmentBuilder[user].Clear();
  //                  }
  //              }
		//		//round robin the avilable corpses to each looter
		//		for (Int32 i =0; i < corpses.Count; i++)
  //              {
  //                	Int32 index = i % E3.CharacterSettings.LootCommander_Looters.Count;
  //                  _lootCommanderAssignmentBuilder[E3.CharacterSettings.LootCommander_Looters[index]].Add(i);

		//		}
  //              foreach(var pair in _lootCommanderAssignmentBuilder)
  //              {
  //                  string user = pair.Key;
  //                  List<Int32> corpseIds = pair.Value;
  //                  //if they have assignments, send off the command
  //                  if(corpseIds.Count> 0)
  //                  {
		//				E3.Bots.BroadcastCommandToPerson(user, $"/lootcommand {user} \"{e3util.NumbersToString(corpseIds, ',')}\"");
		//			}
  //              }
  //          }
		//	MQ.Cmd("/squelch /hidecorpse all");
  //          //give time for corpses to poof
		//	MQ.Delay(100);

		//}
		private static void LootCommanderLootCorpses(CircularBuffer<Int32> corpses)
		{
			if (corpses.Count<Int32>() == 0)
			{
				return;
			}
            MQ.Cmd("/squelch /hidecorpse looted");
			MQ.Delay(100);
			//lets check if we can loot.
			Movement.PauseMovement();
            // bool destroyCorpses = false;
            List<Int32> tcorpseList = corpses.ToList();
			foreach (var corpseid in tcorpseList)
			{
                if(_spawns.TryByID(corpseid,out var c))
				{
                    //allow eq time to send the message to us
					e3util.YieldToEQ();
					if (e3util.IsShuttingDown() || E3.IsPaused()) return;
					EventProcessor.ProcessEventsInQueues("/assistme");
					if (!E3.CharacterSettings.Misc_AutoLootEnabled) return;
					if (!E3.GeneralSettings.Loot_LootInCombat)
					{
						if (Basics.InCombat()) return;
					}

					if (MQ.Query<double>($"${{Spawn[id {c.ID}].Distance3D}}") > E3.GeneralSettings.Loot_CorpseSeekRadius * 2)
					{
						E3.Bots.Broadcast($"\arSkipping corpse: {c.ID} because of distance: ${{Spawn[id {c.ID}].Distance3D}}");
						continue;
					}

					Casting.TrueTarget(c.ID);
					MQ.Delay(2000, "${Target.ID}");

					if (MQ.Query<bool>("${Target.ID}"))
					{
						e3util.TryMoveToTarget();
						MQ.Delay(2250, "${Target.Distance} < 10"); // Give Time to get to Corpse 
						LootCorpse(c);
                        corpses.PopFront();
						if (MQ.Query<bool>("${Window[LootWnd].Open}"))
						{
							MQ.Cmd("/nomodkey /notify LootWnd DoneButton leftmouseup");
						}

						MQ.Delay(300);
					}
				}
                else
                {
					corpses.PopFront();
				}
			}
			E3.Bots.Broadcast("\agFinished looting commanded corpses");
			
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
                if (spawn.Distance3D < E3.GeneralSettings.Loot_CorpseSeekRadius && spawn.DeityID == 0 && spawn.TypeDesc == "Corpse" )
                {
					//is it too far above/below us? ignore it
					if (Math.Abs(spawn.Z - startZ) > 20) continue;

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

				E3.Bots.Broadcast("\agStarting to loot area");

				//lets check if we can loot.
				Movement.PauseMovement();

               // bool destroyCorpses = false;

                foreach (var c in corpses)
                {
					
					//allow eq time to send the message to us
					e3util.YieldToEQ();
                    if (e3util.IsShuttingDown() || E3.IsPaused()) return;
                    EventProcessor.ProcessEventsInQueues("/lootoff");
					EventProcessor.ProcessEventsInQueues("/assistme");
					if (!E3.CharacterSettings.Misc_AutoLootEnabled) return;
                    
                    if(Basics.InCombat())
                    {
						if (LootStackableSettings.Enabled && !LootStackableSettings.LootInCombat) return;
						//if not individual settings, and global loot in combat isn't on, return
						if (!LootStackableSettings.Enabled && !E3.GeneralSettings.Loot_LootInCombat) return;
					}
                   
                    if (MQ.Query<double>($"${{Spawn[id {c.ID}].Distance3D}}") > E3.GeneralSettings.Loot_CorpseSeekRadius*2)
                    {
                        E3.Bots.Broadcast($"\arSkipping corpse: {c.ID} because of distance: ${{Spawn[id {c.ID}].Distance3D}}");
                        continue;
                    }

                    Casting.TrueTarget(c.ID);
                    //MQ.Delay(2000, "${Target.ID}");
                   
                    if(MQ.Query<bool>("${Target.ID}"))
                    {
                        e3util.TryMoveToTarget();
                        MQ.Delay(2250, "${Target.Distance} < 10"); // Give Time to get to Corpse 
                        LootCorpse(c);
                       
                        if (MQ.Query<bool>("${Window[LootWnd].Open}"))
                        {
                            MQ.Cmd("/nomodkey /notify LootWnd DoneButton leftmouseup");
                        }

                        MQ.Delay(300);
                    }
                    
                }

                E3.Bots.Broadcast("\agFinished looting area");
                MQ.Delay(100); // Wait for fading corpses to disappear
            }
        }
        private static bool SafeToLoot()
        {
			foreach (var s in _spawns.Get().OrderBy(x => x.Distance3D))
			{
				//find all mobs that are close
				if (s.TypeDesc != "NPC") continue;
				if (!s.Targetable) continue;
				if (!s.Aggressive) continue;
				if (s.CleanName.EndsWith("s pet")) continue;
				if (!MQ.Query<bool>($"${{Spawn[npc id {s.ID}].LineOfSight}}")) continue;
				if (s.Distance3D > 30) break;//mob is too far away, and since it is ordered, kick out.
                                          
                return false;
			}
            return true;
		}
        public static void DestroyCorpse(Spawn corpse)
        {
            MQ.Cmd("/loot");
            MQ.Delay(1000, "${Window[LootWnd].Open}");
            MQ.Delay(100);
            if (!MQ.Query<bool>("${Window[LootWnd].Open}"))
            {
                MQ.Write($"\arERROR, Loot Window not opening, adding {corpse.CleanName}-{corpse.ID} to ignore corpse list.");
                if (!_unlootableCorpses.Contains(corpse.ID))
                {
                    _unlootableCorpses.Add(corpse.ID);
                }
                return;

            }
            MQ.Delay(500, "${Corpse.Items}");

            MQ.Delay(E3.GeneralSettings.Loot_LootItemDelay);//wait a little longer to let the items finish populating, for EU people they may need to increase this.

            Int32 corpseItems = MQ.Query<Int32>("${Corpse.Items}");

            if (corpseItems == 0)
            {
                //no items on the corpse, kick out
                return;
            }

            for (Int32 i = 1; i <= corpseItems; i++)
            {
              
                //lets loot it if we can!
                MQ.Cmd($"/nomodkey /shift /itemnotify loot{i} leftmouseup", 300);
                MQ.Delay(1000, "${Cursor.ID}");
                Int32 cursorid = MQ.Query<Int32>("${Cursor.ID}");
                if(cursorid>0)
                {
					string cursorItemName = MQ.Query<string>("${Cursor}");
					E3.Bots.Broadcast($"Deleting from corpse [] [{MQ.Query<string>("${Cursor}")}]");
					//have it on our cursor, lets destroy
					e3util.CursorTryDestroyItem(cursorItemName);
					//delay until the cursor is empty
					MQ.Delay(1000, "${If[${Cursor.ID},FALSE,TRUE]}");
                }

            }
            
        }
        public static bool ImportantItemOnCorpse(Spawn corpse)
        {
            bool importantItem = false;
            bool nodropImportantItem = false;

            if(!MQ.Query<bool>("${Window[LootWnd].Open}"))
            {
				MQ.Cmd("/loot");
				MQ.Delay(1000, "${Window[LootWnd].Open}");
				MQ.Delay(100);
				if (!MQ.Query<bool>("${Window[LootWnd].Open}"))
				{
					MQ.Write($"\arERROR, Loot Window not opening, adding {corpse.CleanName}-{corpse.ID} to ignore corpse list.");
					if (!_unlootableCorpses.Contains(corpse.ID))
					{
						_unlootableCorpses.Add(corpse.ID);
					}
					return true;

				}
			}

           
            MQ.Delay(500, "${Corpse.Items}");

            MQ.Delay(E3.GeneralSettings.Loot_LootItemDelay);//wait a little longer to let the items finish populating, for EU people they may need to increase this.

            Int32 corpseItems = MQ.Query<Int32>("${Corpse.Items}");

            if (corpseItems == 0)
            {
                //no items on the corpse, kick out
                return false;
            }

            for (Int32 i = 1; i <= corpseItems; i++)
            {
                //lets try and loot them.
                importantItem = false;

                MQ.Delay(1000, $"${{Corpse.Item[{i}].ID}}");

                string corpseItem = MQ.Query<string>($"${{Corpse.Item[{i}].Name}}");
                bool stackable = MQ.Query<bool>($"${{Corpse.Item[{i}].Stackable}}");
                bool nodrop = MQ.Query<bool>($"${{Corpse.Item[{i}].NoDrop}}");
                Int32 itemValue = MQ.Query<Int32>($"${{Corpse.Item[{i}].Value}}");
                Int32 stackCount = MQ.Query<Int32>($"${{Corpse.Item[{i}].Stack}}");
                bool tradeskillItem = MQ.Query<bool>($"${{Corpse.Item[{i}].Tradeskills}}");

				if (LootStackableSettings.Enabled)
				{   //this is the new character specific loot stackable
					//check if in our always loot.
					if (LootStackableSettings.AlwaysStackableItems.Contains(corpseItem, StringComparer.OrdinalIgnoreCase))
					{
						importantItem = true;
						nodropImportantItem = nodrop;
						MQ.Write("\ayStackable: always loot item " + corpseItem);
					}
                    if(!importantItem && LootStackableSettings.AlwaysStackableItemsContains.Count>0)
                    {
                        foreach(var item in LootStackableSettings.AlwaysStackableItemsContains)
                        {
                            if(corpseItem.IndexOf(item,StringComparison.OrdinalIgnoreCase)>-1)
                            {
                                importantItem = true;
                                break;
                            }
                        }
                    }

					if (stackable && !nodrop)
					{
						if (!importantItem && LootStackableSettings.LootOnlyCommonTradeSkillItems)
						{
							if (corpseItem.Contains(" Pelt")) importantItem = true;
							if (corpseItem.Contains(" Silk")) importantItem = true;
							if (corpseItem.Contains(" Ore")) importantItem = true;
						}
						if (!importantItem && itemValue >= LootStackableSettings.LootValueGreaterThanInCopper)
						{
							importantItem = true;
						}
						if (!importantItem && LootStackableSettings.LootAllTradeSkillItems)
						{
							if (tradeskillItem) importantItem = true;
						}

						if (!importantItem && itemValue >= LootStackableSettings.LootValueGreaterThanInCopper)
						{
							importantItem = true;
						}
					}
				}
				else if (E3.GeneralSettings.Loot_OnlyStackableEnabled)
                {
                    //this is the legacy general settigs for loot stackable
                    //check if in our always loot.
                    if (E3.GeneralSettings.Loot_OnlyStackableAlwaysLoot.Contains(corpseItem, StringComparer.OrdinalIgnoreCase))
                    {
                        importantItem = true;
                        nodropImportantItem = nodrop;
                        MQ.Write("\ayStackable: always loot item " + corpseItem);
                    }

                    if (stackable && !nodrop)
                    {
                        if (!importantItem && E3.GeneralSettings.Loot_OnlyStackableOnlyCommonTradeSkillItems)
                        {
                            if (corpseItem.Contains(" Pelt")) importantItem = true;
                            if (corpseItem.Contains(" Silk")) importantItem = true;
                            if (corpseItem.Contains(" Ore")) importantItem = true;
                        }
                        if (!importantItem && itemValue >= E3.GeneralSettings.Loot_OnlyStackableValueGreaterThanInCopper)
                        {
                            importantItem = true;
                        }
                        if (!importantItem && E3.GeneralSettings.Loot_OnlyStackableAllTradeSkillItems)
                        {
                            if (tradeskillItem) importantItem = true;
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
                        //loot nodrop items in inifile
                        nodropImportantItem = nodrop;
                    }
                    else if (LootDataFile.Skip.Contains(corpseItem))
                    {
                        importantItem = false;
                        foundInFile = true;
                    }
                    if (!foundInFile && !nodrop)
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
                if (stackable && !weHaveItem)
                {
                    importantItem = false;
                }

                //stackable but we don't have room but we already have an item, lets see if we have room.
                if (stackable && weHaveItem)
                {
                    //does it have free stacks?
                    if (FoundStackableFitInInventory(corpseItem, stackCount))
                    {
                        importantItem = true;
                    }
                }

                if (importantItem) return true;
            }
            return false;
        }
        public static void LootCorpse(Spawn corpse, bool bypassLootSettings = false, bool lootAll = false)
        {
			Int32 lootTryCount = 0;
	
            tryandLoot:
			Int32 freeInventorySlots = MQ.Query<Int32>("${Me.FreeInventory}");
            //keep some free if configured to do so.
            freeInventorySlots -= E3.GeneralSettings.Loot_NumberOfFreeSlotsOpen;

            bool importantItem = false;
            bool nodropImportantItem = false;

            if(!_fullInventoryAlert && freeInventorySlots<1)
            {
                _fullInventoryAlert = true;
                E3.Bots.Broadcast("\arMy inventory is full! \awI will continue to link items on corpses, but cannot loot anything else.");
                E3.Bots.BroadcastCommand("/popup ${Me}'s inventory is full.", false);
                e3util.Beep();

            }
       
            if (!MQ.Query<bool>("${Window[LootWnd].Open}"))
            {
               
                MQ.Cmd("/loot");
                MQ.Delay(1500, "${Window[LootWnd].Open}");
                MQ.Delay(100);
                if (!MQ.Query<bool>("${Window[LootWnd].Open}"))
                {
                    //Retry once
                    lootTryCount++;
                    if (lootTryCount < 2)
                    {
                        goto tryandLoot;
                    }
                    MQ.Write($"\arERROR, Loot Window not opening, adding {corpse.CleanName}-{corpse.ID} to ignore corpse list.");
                    if (!_unlootableCorpses.Contains(corpse.ID))
                    {
                        _unlootableCorpses.Add(corpse.ID);
                    }
                    return;

                }
            }
            
            MQ.Delay(500, "${Corpse.Items}");

            MQ.Delay(E3.GeneralSettings.Loot_LootItemDelay);//wait a little longer to let the items finish populating, for EU people they may need to increase this.

            Int32 corpseItemsCount = MQ.Query<Int32>("${Corpse.Items}");

            if (corpseItemsCount == 0)
            {
                //no items on the corpse, kick out

                return;
            }

            for(Int32 i =1;i<=corpseItemsCount;i++)
            {
                //lets try and loot them.
                importantItem = false;

                MQ.Delay(1000, $"${{Corpse.Item[{i}].ID}}");

                var itemId = MQ.Query<int>($"${{Corpse.Item[{i}].ID}}");
                string corpseItem = MQ.Query<string>($"${{Corpse.Item[{i}].Name}}");
                bool stackable = MQ.Query<bool>($"${{Corpse.Item[{i}].Stackable}}");
                bool nodrop = MQ.Query<bool>($"${{Corpse.Item[{i}].NoDrop}}");
                Int32 itemValue = MQ.Query<Int32>($"${{Corpse.Item[{i}].Value}}");
                Int32 stackCount = MQ.Query<Int32>($"${{Corpse.Item[{i}].Stack}}");
                bool tradeskillItem = MQ.Query<bool>($"${{Corpse.Item[{i}].Tradeskills}}");

                
                //destroy things we don't like
				if (LootDataFile.Destroy.Contains(corpseItem))
				{
                    e3util.ClearCursor();

                    //lets loot it if we can!
                    MQ.Cmd($"/nomodkey /shift /itemnotify loot{i} leftmouseup", 300);
					MQ.Delay(1000, "${Cursor.ID}");
                    var cursorid = MQ.Query<int>("${Cursor.ID}");
                    if (cursorid == itemId)
					{
						string cusrorItemName = MQ.Query<string>("${Cursor}");
						E3.Bots.Broadcast($"Deleting from corpse [{cusrorItemName}]");
						//have it on our cursor, lets destroy
						e3util.CursorTryDestroyItem(cusrorItemName);
						//delay until the cursor is empty
						MQ.Delay(1000, "${If[${Cursor.ID},FALSE,TRUE]}");

					}
                    else
                    {
                        e3util.ClearCursor();
                    }
					continue;
				}
                if(!lootAll)
                {
					if (LootStackableSettings.Enabled)
					{   //this is the new character specific loot stackable
						//check if in our always loot.
						if (LootStackableSettings.AlwaysStackableItems.Contains(corpseItem, StringComparer.OrdinalIgnoreCase))
						{
							importantItem = true;
							nodropImportantItem = nodrop;
							MQ.Write("\ayStackable: always loot item " + corpseItem);
						}
						if (!importantItem && LootStackableSettings.AlwaysStackableItemsContains.Count > 0)
						{
							foreach (var item in LootStackableSettings.AlwaysStackableItemsContains)
							{
								if (corpseItem.IndexOf(item, StringComparison.OrdinalIgnoreCase) > -1)
								{
									importantItem = true;
									break;
								}
							}
						}

						if (stackable && !nodrop)
						{
							if (!importantItem && LootStackableSettings.LootOnlyCommonTradeSkillItems)
							{
								if (corpseItem.Contains(" Pelt")) importantItem = true;
								if (corpseItem.Contains(" Silk")) importantItem = true;
								if (corpseItem.Contains(" Ore")) importantItem = true;
							}
							if (!importantItem && itemValue >= LootStackableSettings.LootValueGreaterThanInCopper)
							{
								importantItem = true;
							}
							if (!importantItem && LootStackableSettings.LootAllTradeSkillItems)
							{
								if (tradeskillItem) importantItem = true;
							}

							if (!importantItem && itemValue >= LootStackableSettings.LootValueGreaterThanInCopper)
							{
								importantItem = true;
							}
						}
						if (LootStackableSettings.HonorLootFileSkips && LootDataFile.Skip.Contains(corpseItem))
						{
							importantItem = false;
						}
					}
					else if (E3.GeneralSettings.Loot_OnlyStackableEnabled)
					{
						//check if in our always loot.
						if (E3.GeneralSettings.Loot_OnlyStackableAlwaysLoot.Contains(corpseItem, StringComparer.OrdinalIgnoreCase))
						{
							importantItem = true;
							nodropImportantItem = nodrop;
							MQ.Write("\ayStackable: always loot item " + corpseItem);
						}

						if (stackable && !nodrop)
						{
							if (!importantItem && E3.GeneralSettings.Loot_OnlyStackableOnlyCommonTradeSkillItems)
							{
								if (corpseItem.Contains(" Pelt")) importantItem = true;
								if (corpseItem.Contains(" Silk")) importantItem = true;
								if (corpseItem.Contains(" Ore")) importantItem = true;
							}
							if (!importantItem && E3.GeneralSettings.Loot_OnlyStackableAllTradeSkillItems)
							{
								if (tradeskillItem) importantItem = true;
							}
							if (!importantItem && itemValue >= E3.GeneralSettings.Loot_OnlyStackableValueGreaterThanInCopper)
							{
								importantItem = true;
							}
						}

						if (E3.GeneralSettings.Loot_OnlyStackableHonorLootFileSkips && LootDataFile.Skip.Contains(corpseItem))
						{
							importantItem = false;
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
							//loot nodrop items in inifile
							nodropImportantItem = nodrop;
						}
						else if (LootDataFile.Skip.Contains(corpseItem))
						{
							importantItem = false;
							foundInFile = true;
						}
						if (!foundInFile && !nodrop)
						{
							importantItem = true;
							LootDataFile.Keep.Add(corpseItem);
							E3.Bots.BroadcastCommandToGroup($"/E3LootAdd \"{corpseItem}\" KEEP");
							LootDataFile.SaveData();
						}

					}

				}
                else
                {
                    //we are told to loot all, ignore settings
                    nodropImportantItem = true;
                    importantItem = true;
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

                if (freeInventorySlots < 1 && !stackable)
                {
                    importantItem = false;
                }

                if (importantItem || bypassLootSettings)
                {
                    //lets loot it if we can!
                    MQ.Cmd($"/nomodkey /shift /itemnotify loot{i} rightmouseup", E3.GeneralSettings.Loot_LootItemDelay + e3util.Latency());
                    //loot nodrop items if important
                    if (nodropImportantItem)
                    {
                        bool confirmationBox = MQ.Query<bool>("${Window[ConfirmationDialogBox].Open}");
                        if (confirmationBox) MQ.Cmd($"/nomodkey /notify ConfirmationDialogBox CD_Yes_Button leftmouseup", E3.GeneralSettings.Loot_LootItemDelay + e3util.Latency());
                    }
                }
                
               
            }
            if (MQ.Query<Int32>("${Corpse.Items}")>0)
            {   //link what is ever left over.
                //should we should notify if we have not looted.
                
                if (!String.IsNullOrWhiteSpace(E3.GeneralSettings.Loot_LinkChannel))
                {
                    
                    if(MQ.Query<bool>("${Group}"))
                    {
                        PrintCorpseItems(corpse,corpseItemsCount);
//						PrintLink($"{E3.GeneralSettings.Loot_LinkChannel} {corpse.ID} - ");
					}
				}
            }

        }

        private static List<string> _printCorpseItemList = new List<string>(10); 
        private static void PrintCorpseItems(Spawn corpse,Int32 initialCountOfItems)
		{
            //need the initial count in case items were looted, so that we display all items
			Int32 corpseItems = initialCountOfItems;
			if (corpseItems == 0)
			{
				//no items on the corpse, kick out
				return;
			}
            _printCorpseItemList.Clear();
			for (Int32 i = 1; i <= corpseItems; i++)
			{
				var itemId = MQ.Query<int>($"${{Corpse.Item[{i}].ID}}");
				if (itemId > 0)
				{
					string link = MQ.Query<string>($"${{Corpse.Item[{i}].ItemLink[CLICKABLE]}}");
					_printCorpseItemList.Add(link);
				}
			}
            if(_printCorpseItemList.Count > 0 )
            {
				MQ.Cmd($"/{E3.GeneralSettings.Loot_LinkChannel} {corpse.ID}) - {String.Join(",", _printCorpseItemList)}");
				_printCorpseItemList.Clear();//clear out so the values don't live long
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
                        string itemName = MQ.Query<String>($"${{Me.Inventory[pack{i}]}}");//${Me.Inventory[pack${i}].Item.Value}
                        if (itemName == corpseItem)
                        {
                            //its the item we are looking for, does it have stackable 
                            Int32 freeStack = MQ.Query<Int32>($"${{Me.Inventory[pack{i}].FreeStack}}");

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
