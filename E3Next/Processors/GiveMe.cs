using E3Core.Data;
using E3Core.Settings;
using E3Core.Utility;
using MonoCore;
using System;
using System.Collections.Generic;
using System.Linq;

namespace E3Core.Processors
{
    public class GiveMeItem
    {

        public GiveMeItem(string input)
        {
            //eg: Alara|Sanguine Mind Crystal III|30s|SpellNameToUse
            //split this up by |

            string[] splits = input.Split('|');
            if (splits.Length > 0)
            {
                Supplier = splits[0].Trim();
                if (splits.Length > 1)
                {
                    ItemName = splits[1].Trim();

                    if (splits.Length > 2)
                    {
                        string timeString = splits[2].Trim();
                        string tvalue = timeString;
                        bool isMinute = false;
                        if (timeString.EndsWith("s", StringComparison.OrdinalIgnoreCase))
                        {
                            tvalue = tvalue.Substring(0, timeString.Length - 1);
                        }
                        else if (timeString.EndsWith("m", StringComparison.OrdinalIgnoreCase))
                        {
                            isMinute = true;
                            tvalue = tvalue.Substring(0, timeString.Length - 1);
                        }

                        if (Int32.TryParse(tvalue, out Delay))
                        {
                            if (Delay < 30) Delay = 30;

                            if (isMinute)
                            {
                                Delay = Delay * 60;
                            }
                        }
						if(splits.Length>3)
						{
							SpellToUse = splits[3].Trim();
							
						}
                    }
                }
            }
        }
        public string Supplier = String.Empty;
        public string ItemName = String.Empty;
		public string SpellToUse = String.Empty;
        public Int32 Delay = 30;
        public double NextCheck = 0;
		public bool NoCombat = false;
    }

    public static class GiveMe
    {
        public static Logging _log = E3.Log;
        private static IMQ MQ = E3.MQ;
        private static ISpawns _spawns = E3.Spawns;
        private static Dictionary<string, Int32> _groupSpellLimits = new Dictionary<string, int>();
        public static Dictionary<string, Data.Spell> _groupSpellRequests = new Dictionary<string, Data.Spell>(StringComparer.OrdinalIgnoreCase);
        private static Int64 _nextSupplyCheck = 0;
        private static Int64 _nextSupplyCheckInterval = 1000;
        private static List<GiveMeItem> _supplyList = new List<GiveMeItem>();

        [SubSystemInit]
        public static void GiveMe_Init()
        {
            RegisterEvents();

			//laz spells
            _groupSpellRequests.Add("Azure Mind Crystal III", new Spell("Azure Mind Crystal"));
			_groupSpellRequests.Add("Azure Mind Crystal II", new Spell("Azure Mind Crystal"));
			_groupSpellRequests.Add("Azure Mind Crystal I", new Spell("Azure Mind Crystal"));
			_groupSpellRequests.Add("Summoned: Large Modulation Shard", new Spell("Large Modulation Shard"));
			_groupSpellRequests.Add("Sanguine Mind Crystal III", new Spell("Sanguine Mind Crystal"));
			_groupSpellRequests.Add("Blazing Void Orb", new Spell("Glyphwielder's Eternal Bracer"));
			_groupSpellRequests.Add("Molten orb", new Spell("Summon: Molten Orb"));
            _groupSpellRequests.Add("Lava orb", new Spell("Summon: Lava Orb"));
            _groupSpellRequests.Add("Rod of Mystical Transvergence", new Spell("Mass Mystical Transvergence"));
			//Live spells
			_groupSpellRequests.Add("Summoned: Modulation Shard VIII", new Spell("Summon Modulation Shard"));
			_groupSpellRequests.Add("Summoned: Modulation Shard VII", new Spell("Summon Modulation Shard"));
			_groupSpellRequests.Add("Summoned: Modulation Shard VI", new Spell("Summon Modulation Shard"));
			_groupSpellRequests.Add("Summoned: Modulation Shard V", new Spell("Summon Modulation Shard"));

			foreach (var input in E3.CharacterSettings.Gimme)
            {
                var item = new GiveMeItem(input);
                if (!(String.IsNullOrWhiteSpace(item.Supplier) || String.IsNullOrWhiteSpace(item.ItemName)))
                {
                    _supplyList.Add(item);
                }
            }
			foreach (var input in E3.CharacterSettings.Gimme_NoCombat)
			{
				var item = new GiveMeItem(input);
				item.NoCombat = true;
				if (!(String.IsNullOrWhiteSpace(item.Supplier) || String.IsNullOrWhiteSpace(item.ItemName)))
				{
					_supplyList.Add(item);
				}
			}
		}
        public static void Reset()
        {
            _supplyList.Clear();
            foreach (var input in E3.CharacterSettings.Gimme)
            {
                var item = new GiveMeItem(input);
                if (!(String.IsNullOrWhiteSpace(item.Supplier) || String.IsNullOrWhiteSpace(item.ItemName)))
                {
                    _supplyList.Add(item);
                }
            }
        }

        private static void RegisterEvents()
        {

            EventProcessor.RegisterCommand("/giveme", (x) =>
            {

                if (Basics.InCombat() && !E3.CharacterSettings.Gimme_InCombat) return;
               
                Int32 qty = int.MaxValue;
				//giveme Alara "Something" qty Rekken

				if(x.args.Count>4)
				{
					string user = x.args[0];
					string something = x.args[1];
					if (x.args.Count > 2)
					{
						Int32.TryParse(x.args[2], out qty);
					}
					string reciver = x.args[3];
					string spellName = x.args[4];
					
					GiveTo(reciver, something, qty,spellName);
				}
				else if (x.args.Count > 3)
                {
                    string user = x.args[0];
                    string something = x.args[1];
					if (x.args.Count > 2)
					{
						Int32.TryParse(x.args[2], out qty);
					}
					string reciver = x.args[3];
                  
                    GiveTo(reciver, something, qty);

                }//giveme Alara "Something" qty
                else if (x.args.Count > 1)
                {
                    //need to broadcast 
                    string user = x.args[0];
                    string something = x.args[1];
                    if (x.args.Count > 2)
                    {
                        Int32.TryParse(x.args[2], out qty);
                    }
                    E3.Bots.BroadcastCommandToPerson(user, $"/giveme {user} \"{something}\" {qty} {E3.CurrentName}");
                }
            });

            EventProcessor.RegisterCommand("/DestroyNoRent", (x) =>
            {
                //giveme Alara "Something" qty Rekken
                if (x.args.Count > 0)
                {
					Int32 maxTries = 0;
                    while(Casting.InGlobalCooldown())
                    {
                        MQ.Delay(50);
						maxTries++;
						if (maxTries > 40) break;
                    }
                    e3util.DeleteNoRentItem(x.args[0]);
                }

            });
        }
        [ClassInvoke(Class.All)]
        public static void CheckSupply()
        {
            if (!e3util.ShouldCheck(ref _nextSupplyCheck, _nextSupplyCheckInterval)) return;
            if (Basics.AmIDead()) return;
			if (E3.IsInvis) return;

            if (_supplyList.Count > 0)
            {
                //make sure we have the items or we need to request.
                foreach (var item in _supplyList)
                {

					if (item.NoCombat && Basics.InCombat()) continue;

                    if (item.NextCheck < Core.StopWatch.ElapsedMilliseconds)
                    {
                        item.NextCheck = Core.StopWatch.ElapsedMilliseconds + (item.Delay * 1000);
                        bool haveItem = MQ.Query<bool>($"${{FindItem[={item.ItemName}]}}");
                        if (!haveItem)
                        {
                            //does the person exist in group?
                            Spawn s;
                            if (_spawns.TryByName(item.Supplier, out s))
                            {
                                //can only request group spells for group members
                                if (_groupSpellRequests.ContainsKey(item.ItemName) && !Basics.GroupMembers.Contains(s.ID)) return;

                                if (s.Distance < 100)
                                {
                                    if (E3.Bots.BotsConnected().Contains(s.CleanName))
                                    {
                                        //lets ask for it!
										if(!String.IsNullOrWhiteSpace(item.SpellToUse))
										{
											E3.Bots.BroadcastCommandToPerson(item.Supplier, $"/giveme {item.Supplier} \"{item.ItemName}\" {1} {E3.CurrentName} \"{item.SpellToUse}\"");
										}
										else
										{
											E3.Bots.BroadcastCommandToPerson(item.Supplier, $"/giveme {item.Supplier} \"{item.ItemName}\" {1} {E3.CurrentName}");
										}
                                       
                                    }
                                }

                            }
                        }
                    }
                }
            }
        }
        public static void GiveTo(string whoToGiveTo, string whatToGive, Int32 qtyToGive, string SpellName = "")
        {
            //exceptions for group spells
            if (_groupSpellRequests.ContainsKey(whatToGive))
            {
                if (Basics.AmIDead()) return;
                DoGroupSpellGive(whoToGiveTo, whatToGive);
                return;
            }
			//exception for indivdual spell
			if (!String.IsNullOrWhiteSpace(SpellName))
			{
			
				DoSpellGive(whoToGiveTo,whatToGive, SpellName);
				return;
			}
			//normal stuff
			Spawn s;
            if (_spawns.TryByName(whoToGiveTo, out s))
            {
                if (Casting.TrueTarget(s.ID))
                {
                    var platSynonyms = new List<string> { "plat", "pp", "platinum" };
                    var dcSynonyms = new List<string> { "dc", "diamond coin", "diamond coins" };
                    if (platSynonyms.Contains(whatToGive, StringComparer.OrdinalIgnoreCase))
                    {
                        MQ.Cmd("/invoke ${Window[InventoryWindow].DoOpen}");
                        MQ.Delay(1000, "${Window[InventoryWindow].Open");
                        var moneyCommand = "/notify InventoryWindow IW_Money0 leftmouseup";
                        if (qtyToGive == int.MaxValue)
                        {
                            MQ.Cmd($"/shiftkey {moneyCommand}");
                        }
                        else
                        {
                            MQ.Cmd(moneyCommand);
                            MQ.Delay(1000, "${Window[QuantityWnd].Open}");
                            MQ.Cmd($"/nomodkey /notify QuantityWnd QTYW_slider newvalue {qtyToGive}");
                            MQ.Delay(200);
                            MQ.Cmd($"/nomodkey /notify QuantityWnd QTYW_Accept_Button leftmouseup");
                        }
                    }
                    else if (dcSynonyms.Contains(whatToGive, StringComparer.OrdinalIgnoreCase))
                    {
                        MQ.Cmd("/invoke ${Window[InventoryWindow].DoOpen}");
                        MQ.Cmd("/notify InventoryWindow IW_Subwindows tabselect 5", 300);
                        MQ.Cmd("/notify InventoryWindow IW_AltCurr_ReclaimButton leftmouseup", 300);
                        MQ.Cmd("/notify InventoryWindow AltCurr_PointList listselect ${Window[InventoryWindow].Child[AltCurr_PointList].List[=Diamond Coins,2]}", 300);
                        for (int i = 1; i <= 8; i++)
                        {
                            MQ.Cmd("/shiftkey /notify InventoryWindow IW_AltCurr_CreateItemButton leftmouseup", 100);
                        }

                        for (int i = 1; i <= 8; i++)
                        {
                            if (MQ.Query<int>("${Cursor.ID}") <= -1) break;
                            MQ.Cmd("/click left target", 250);
                            MQ.Delay(2000, "${Cursor.ID}");
                        }
                       
                        E3.Bots.Trade(whoToGiveTo);
                        MQ.Cmd("/invoke ${Window[InventoryWindow].DoClose}");
                        return;
                    }
                    else
                    {
                        bool weHaveItem = MQ.Query<bool>($"${{FindItemCount[={whatToGive}]}}");
                        if (weHaveItem)
                        {
                            if (e3util.ClearCursor())
                            {
                                Int32 packNumber = MQ.Query<Int32>($"${{Math.Calc[${{FindItem[{whatToGive}].ItemSlot}}-22].Int}}");

                                //check to see if the slot is a bag

                                bool isBag = MQ.Query<Boolean>($"${{Me.Inventory[pack{packNumber}].Container}}");

								if (isBag)
								{
									bool bagOpen = MQ.Query<bool>($"${{Bool[${{Window[pack{packNumber}].Open}}]}}");
									if (!bagOpen)
									{
										//have to open the bag to get the qty count to work
										MQ.Cmd($"/nomodkey /itemnotify pack{packNumber} rightmouseup");
										MQ.Delay(200);
									}
								}
                                //put item on cursor
                                MQ.Cmd($"/nomodkey /itemnotify \"{whatToGive}\" leftmouseup");
                                MQ.Delay(200);
                                bool qtyWindowOpen = MQ.Query<bool>("${Window[QuantityWnd].Open}");
                                if (qtyWindowOpen)
                                {
                                    MQ.Cmd($"/nomodkey /notify QuantityWnd QTYW_slider newvalue {qtyToGive}");
                                    MQ.Delay(200);
                                    MQ.Cmd($"/nomodkey /notify QuantityWnd QTYW_Accept_Button leftmouseup");
                                    MQ.Delay(200);
                                }
                                if(isBag)
                                {
									//close the bag
									MQ.Cmd($"/nomodkey /itemnotify pack{packNumber} rightmouseup");

								}
							}
                        }
                        else
                        {
                            MQ.Cmd($"/t {whoToGiveTo} I'm afraid I can't do that, as I don't have any {whatToGive}");
                            return;
                        }
                    }

                    e3util.GiveItemOnCursorToTarget();
                }
            }
        }
        private static void DoGroupSpellGive(string whoToGiveTo, string whatToGive)
        {
            Spawn spawn;
            if(_spawns.TryByName(whoToGiveTo, out spawn))
            {
                //check if group member
                if (!Basics.GroupMembers.Contains(spawn.ID)) return;

                Spell s;
                if (_groupSpellRequests.TryGetValue(whatToGive, out s))
                {
                    if (Casting.CheckMana(s) && Casting.CheckReady(s))
                    {
                        //lets tell everyone else to destroy their items and destroy our own.
                        E3.Bots.BroadcastCommandToGroup($"/DestroyNoRent \"{whatToGive}\"");
                        e3util.DeleteNoRentItem(whatToGive);
                        MQ.Delay(2000);
                        Casting.Cast(0, s);
						MQ.Delay(1000);
						e3util.ClearCursor();
                    }
                }
            }
        }
		private static void DoSpellGive(string whoToGiveTo, string whatToGive, string spellToUse)
		{
			Spawn spawn;
			if (_spawns.TryByName(whoToGiveTo, out spawn))
			{
			
				//check if group member
				if (!Basics.GroupMembers.Contains(spawn.ID)) return;
				Casting.TrueTarget(spawn.ID);
				Spell s;
				if (!Spell.LoadedSpellsByName.TryGetValue(spellToUse, out s))
				{
					s = new Spell(spellToUse);
				}
				e3util.ClearCursor();
				Int32 cursorID = MQ.Query<Int32>("${Cursor.ID}");

				if (cursorID<1 && Casting.CheckMana(s) && Casting.CheckReady(s))
				{
				
					e3util.DeleteNoRentItem(whatToGive);
					Casting.Cast(0, s);
					MQ.Delay(1000);
					if (whoToGiveTo == E3.CurrentName)
					{
						e3util.ClearCursor();
					}
					else
					{
						cursorID = MQ.Query<Int32>("${Cursor.ID}");
						if (cursorID > 0)
						{
							//the spell that was requested put something on our curosr, give it to them.
							e3util.GiveItemOnCursorToTarget();

						}
					}
				}
			}
		}
	}
}
