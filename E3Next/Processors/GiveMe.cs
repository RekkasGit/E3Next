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
    public static class GiveMe
    {
        public static Logging _log = E3._log;
        private static IMQ MQ = E3.MQ;
        private static ISpawns _spawns = E3._spawns;
        private static Dictionary<string, Int32> _groupSpellLimits = new Dictionary<string, int>();
        private static Dictionary<string, Int32> _groupSpellCurrentRequests= new Dictionary<string, int>();
        [SubSystemInit]
        public static void Init()
        {
            RegisterEvents();

            _groupSpellLimits.Add("Azure Mind Crystal III", 2);
            _groupSpellLimits.Add("Summoned: Large Modulation Shard", 1);
            _groupSpellLimits.Add("Sanguine Mind Crystal III", 1);
            _groupSpellLimits.Add("Molten orb",3);

            _groupSpellCurrentRequests.Add("Azure Mind Crystal III", 0);
            _groupSpellCurrentRequests.Add("Summoned: Large Modulation Shard", 0);
            _groupSpellCurrentRequests.Add("Sanguine Mind Crystal III", 0);
            _groupSpellCurrentRequests.Add("Molten orb", 0);

        }

        private static void RegisterEvents()
        {

            EventProcessor.RegisterCommand("/giveme", (x) =>
            {
              
                //giveme Alara "Something" qty Rekken
                if(x.args.Count>3)
                {
                    string user = x.args[0];
                    string something = x.args[1];
                    Int32 qty = 1;
                    string reciver = x.args[3];
                    if(x.args.Count>2)
                    {
                        Int32.TryParse(x.args[2], out qty);
                    }
                    GiveTo(reciver, something, qty);

                }//giveme Alara "Something" qty
                else if(x.args.Count > 1)
                {
                    //need to broadcast 
                    string user = x.args[0];
                    string something = x.args[1];
                    Int32 qty = 1;
                    if (x.args.Count > 2)
                    {
                        Int32.TryParse(x.args[2], out qty);
                    }
                    E3._bots.BroadcastCommandToPerson(user, $"/giveme {user} \"{something}\" {qty} {E3._currentName}");
                }

            });



        }
        public static void GiveTo(string whoToGiveTo, string whatToGive, Int32 qtyToGive)
        {
            Spawn s;
            if(_spawns.TryByName(whoToGiveTo,out s))
            {
                if(Casting.TrueTarget(s.ID))
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
    }
}
