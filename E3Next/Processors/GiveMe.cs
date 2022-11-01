using E3Core.Data;
using E3Core.Settings;
using E3Core.Settings.FeatureSettings;
using E3Core.Utility;
using Microsoft.Win32;
using MonoCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace E3Core.Processors
{
    public class GiveMeItem
    {

        public GiveMeItem(string input)
        {
            //eg: Alara|Sanguine Mind Crystal III|30s
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
                    }
                }
            }
        }
        public string Supplier = String.Empty;
        public string ItemName = String.Empty;
        public Int32 Delay = 30;
        public double NextCheck = 0;
    }

    public static class GiveMe
    {
        public static Logging _log = E3.Log;
        private static IMQ MQ = E3.Mq;
        private static ISpawns _spawns = E3.Spawns;
        private static Dictionary<string, Int32> _groupSpellLimits = new Dictionary<string, int>();
        private static Dictionary<string, Data.Spell> _groupSpellRequests = new Dictionary<string, Data.Spell>(StringComparer.OrdinalIgnoreCase);
        private static Int64 _nextSupplyCheck = 0;
        private static Int64 _nextSupplyCheckInterval = 1000;
        private static List<GiveMeItem> _supplyList = new List<GiveMeItem>();

        [SubSystemInit]
        public static void Init()
        {
            RegisterEvents();

            _groupSpellRequests.Add("Azure Mind Crystal III", new Spell("Azure Mind Crystal"));
            _groupSpellRequests.Add("Summoned: Large Modulation Shard", new Spell("Large Modulation Shard"));
            _groupSpellRequests.Add("Sanguine Mind Crystal III", new Spell("Sanguine Mind Crystal"));
            _groupSpellRequests.Add("Molten orb", new Spell("Summon: Molten Orb"));

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
                //giveme Alara "Something" qty Rekken
                if (x.args.Count > 3)
                {
                    string user = x.args[0];
                    string something = x.args[1];
                    Int32 qty = 1;
                    string reciver = x.args[3];
                    if (x.args.Count > 2)
                    {
                        Int32.TryParse(x.args[2], out qty);
                    }
                    GiveTo(reciver, something, qty);

                }//giveme Alara "Something" qty
                else if (x.args.Count > 1)
                {
                    //need to broadcast 
                    string user = x.args[0];
                    string something = x.args[1];
                    Int32 qty = 1;
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
                    e3util.DeleteNoRentItem(x.args[0]);
                }

            });
        }
        [ClassInvoke(Class.All)]
        public static void CheckSupply()
        {
            if (!e3util.ShouldCheck(ref _nextSupplyCheck, _nextSupplyCheckInterval)) return;
            if (_supplyList.Count > 0)
            {
                //make sure we have the items or we need to request.
                foreach (var item in _supplyList)
                {

                    bool haveItem = MQ.Query<bool>($"${{FindItem[={item.ItemName}]}}");
                    if (!haveItem)
                    {
                        if (item.NextCheck < Core._stopWatch.ElapsedMilliseconds)
                        {
                            item.NextCheck = Core._stopWatch.ElapsedMilliseconds + (item.Delay * 1000);
                            //does the person exist in group?
                            Spawn s;
                            if (_spawns.TryByName(item.Supplier, out s))
                            {
                                //can only request group spells for group members
                                if (_groupSpellRequests.ContainsKey(item.ItemName) && !Basics._groupMembers.Contains(s.ID)) return;

                                if (s.Distance < 100)
                                {
                                    if (E3.Bots.BotsConnected().Contains(s.CleanName))
                                    {
                                        //lets ask for it!
                                        E3.Bots.BroadcastCommandToPerson(item.Supplier, $"/giveme {item.Supplier} \"{item.ItemName}\" {1} {E3.CurrentName}");
                                    }
                                }

                            }
                        }
                    }
                }
            }
        }
        public static void GiveTo(string whoToGiveTo, string whatToGive, Int32 qtyToGive)
        {
            //exceptions for group spells
            if (_groupSpellRequests.ContainsKey(whatToGive))
            {
                DoGroupSpellGive(whoToGiveTo, whatToGive);
                return;
            }

            Spawn s;
            if (_spawns.TryByName(whoToGiveTo, out s))
            {
                if (Casting.TrueTarget(s.ID))
                {
                    bool weHaveItem = MQ.Query<bool>($"${{FindItemCount[={whatToGive}]}}");
                    if (weHaveItem && e3util.ClearCursor())
                    {
                        Int32 packNumber = MQ.Query<Int32>($"${{Math.Calc[${{FindItem[{whatToGive}].ItemSlot}}-22].Int}}");
                        bool bagOpen = MQ.Query<bool>($"${{Bool[${{Window[Pack{packNumber}].Open}}]}}");
                        if (!bagOpen)
                        {
                            //have to open the bag to get the qty count to work
                            MQ.Cmd($"/itemnotify pack{packNumber} rightmouseup");
                            MQ.Delay(200);
                        }
                        //put item on cursor
                        MQ.Cmd($"/itemnotify \"{whatToGive}\" leftmouseup");
                        MQ.Delay(200);
                        bool qtyWindowOpen = MQ.Query<bool>("${Window[QuantityWnd].Open}");
                        if (qtyWindowOpen)
                        {
                            MQ.Cmd($"/notify QuantityWnd QTYW_slider newvalue {qtyToGive}");
                            MQ.Delay(200);
                            MQ.Cmd($"/notify QuantityWnd QTYW_Accept_Button leftmouseup");
                            MQ.Delay(200);
                        }
                        //close the bag
                        MQ.Cmd($"/itemnotify pack{packNumber} rightmouseup");

                        e3util.GiveItemOnCursorToTarget();
                    }
                }
            }
        }
        private static void DoGroupSpellGive(string whoToGiveTo, string whatToGive)
        {
            Spawn spawn;
            if(_spawns.TryByName(whoToGiveTo, out spawn))
            {
                //check if group member
                if (!Basics._groupMembers.Contains(spawn.ID)) return;

                Spell s;
                if (_groupSpellRequests.TryGetValue(whatToGive, out s))
                {
                    if (Casting.CheckReady(s) && Casting.CheckMana(s))
                    {
                        //lets tell everyone else to destroy their items and destroy our own.
                        E3.Bots.BroadcastCommandToGroup($"/DestroyNoRent \"{whatToGive}\"");
                        e3util.DeleteNoRentItem(whatToGive);
                        MQ.Delay(2000);
                        Casting.Cast(0, s);
                    }
                }
            }
        }
    }
}
