using E3Core.Data;
using E3Core.Settings;
using E3Core.Settings.FeatureSettings;
using E3Core.Utility;
using MonoCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace E3Core.Processors
{
    public static class Basics
    {

        public static bool _following = false;
        //public static Int32 _followTargetID = 0;
        public static string _followTargetName = String.Empty;
        public static DoorDataFile _doorData = new DoorDataFile();
        public static Logging _log = E3._log;
        private static IMQ MQ = E3.MQ;
        private static ISpawns _spawns = E3._spawns;
        public static bool _isPaused = false;
        public static List<Int32> _groupMembers = new List<int>();
        private static Int64 _nextGroupCheck = 0;
        private static Int64 _nextGroupCheckInterval = 1000;

        private static Int64 _nextResourceCheck = 0;
        private static Int64 _nextResourceCheckInterval = 1000;
        private static Int64 _nextAutoMedCheck = 0;
        private static Int64 _nextAutoMedCheckInterval = 1000;

        private static Int64 _nextAnchorCheck = 0;
        private static Int64 _nextAnchorCheckInterval = 1000;

        private static Int64 _nextFollowCheck = 0;
        private static Int64 _nextFollowCheckInterval = 1000;

        private static Int64 _nextChaseCheck = 0;
        private static Int64 _nextChaseCheckInterval = 10;


        public static void Init()
        {
            RegisterEventsCasting();
            _doorData.LoadData();
        }
        public static void Reset()
        {
            _anchorTarget = 0;
            Basics._following = false;
            Basics._followTargetName = String.Empty;
            Basics._chaseTarget = String.Empty;
        }
        public static void ResetKeepFollow()
        {
            _anchorTarget = 0;
            Basics._following = false;
          
        }
        static void RegisterEventsCasting()
        {

            EventProcessor.RegisterEvent("InviteToGroup", "(.+) invites you to join a group.", (x) =>
            {

                MQ.Cmd("/invite");
                MQ.Delay(300);

            });
            EventProcessor.RegisterEvent("InviteToRaid", "(.+) invites you to join a raid.", (x) =>
            {

                MQ.Delay(500);
                MQ.Cmd("/raidaccept");

            });

            EventProcessor.RegisterEvent("InviteToDZ", "(.+) tells you, 'dzadd'", (x) =>
            {
                if (x.match.Groups.Count > 1)
                {
                    MQ.Cmd($"/dzadd {x.match.Groups[1].Value}");
                }
            });


            EventProcessor.RegisterEvent("Zoned", @"You have entered (.+)\.", (x) =>
            {

                //means we have zoned.
                _spawns.RefreshList();//make sure we get a new refresh of this zone.
                Loot.Reset();
                Basics.ResetKeepFollow();
                Assist.Reset();
                Pets.Reset();
           
            });
            EventProcessor.RegisterEvent("Summoned", @"You have been summoned!", (x) =>
            {
                _spawns.RefreshList();//make sure we get a new refresh of this zone.
                Loot.Reset();
                Basics.Reset();
                Assist.Reset();

            });
            //
            EventProcessor.RegisterEvent("InviteToDZ", "(.+) tells you, 'raidadd'", (x) =>
            {
                if (x.match.Groups.Count > 1)
                {
                    MQ.Cmd($"/raidinvite {x.match.Groups[1].Value}");
                }
            });

            EventProcessor.RegisterCommand("/clickit", (x) =>
            {
                if (x.args.Count == 0)
                {
                    //we are telling people to follow us
                    E3._bots.BroadcastCommandToGroup($"/clickit {E3._zoneID}");

                }
                //read the ini file and pull the info we need.

                if (x.args.Count > 0)
                {
                    Int32 zoneID;
                    if (Int32.TryParse(x.args[0], out zoneID))
                    {
                        if (zoneID != E3._zoneID)
                        {
                            //we are not in the same zone, ignore.
                            return;
                        }
                    }
                }

                Int32 closestID = _doorData.ClosestDoorID();

                if (closestID > 0)
                {
                    MQ.Cmd($"/doortarget id {closestID}");
                    double currentDistance = MQ.Query<Double>("${DoorTarget.Distance}");
                    //need to move to its location
                    if (currentDistance < 50)
                    {
                        MQ.Cmd($"/doortarget id {closestID}");

                        Double doorX = MQ.Query<double>("${DoorTarget.X}");
                        Double doorY = MQ.Query<double>("${DoorTarget.Y}");
                        e3util.TryMoveToLoc(doorX, doorY, 8, 3000);
                        MQ.Cmd("/squelch /click left door");
                    }
                    else
                    {
                        MQ.Write("\arMove Closer To Door");
                    }
                }


            });
            EventProcessor.RegisterCommand("/dropinvis", (x) =>
            {
                E3._bots.BroadcastCommandToGroup("/makemevisible");
                MQ.Cmd("/makemevisible");
            });

            EventProcessor.RegisterCommand("/pizza", (x) =>
            {
                if (E3._currentName == "Reek")
                {
                    System.Diagnostics.Process.Start("https://ordering.orders2.me/menu/pontillos-pizzeria-hudson-ridge");
                }
                else
                {
                    System.Diagnostics.Process.Start("https://www.dominos.com/en/restaurants?type=Delivery");
                }


            });

            EventProcessor.RegisterCommand("/armor", (x) =>
            {
                VetAA("Armor of Experience", "/armor", x.args.Count);
            });
            EventProcessor.RegisterCommand("/intensity", (x) =>
            {
                VetAA("Intensity of the Resolute", "/intensity", x.args.Count);
            });
            EventProcessor.RegisterCommand("/infusion", (x) =>
            {
                VetAA("Infusion of the Faithful", "/infusion", x.args.Count);
            });
            EventProcessor.RegisterCommand("/staunch", (x) =>
            {
                VetAA("Staunch Recovery", "/staunch", x.args.Count);
            });
            EventProcessor.RegisterCommand("/servant", (x) =>
            {
                VetAA("Steadfast Servant", "/servant", x.args.Count);
            });
            EventProcessor.RegisterCommand("/expedient", (x) =>
            {
                VetAA("Expedient Recovery", "/expedient", x.args.Count);
            });
            EventProcessor.RegisterCommand("/lesson", (x) =>
            {
                VetAA("Lesson of the Devoted", "/lesson", x.args.Count);
            });
            EventProcessor.RegisterCommand("/throne", (x) =>
            {
                if(x.args.Count==0)
                {
                    E3._bots.BroadcastCommandToGroup("/throne me");
                }
                MQ.Cmd("/interrupt");
                MQ.Delay(500);
                MQ.Cmd("/alt act 511");
                MQ.Delay(500);
                if (E3._currentClass == Class.Bard)
                {
                    MQ.Delay(17000);

                }
                else
                {
                    MQ.Delay(20000, "!${Window[CastingWindow].Open}");
                }
            });
            EventProcessor.RegisterCommand("/jester", (x) =>
            {
                VetAA("Chaotic Jester", "/jester", x.args.Count);
            });
            EventProcessor.RegisterCommand("/bark", (x) =>
            {

                //rebuild the bark message, and do a /say
                if (x.args.Count > 0)
                {
                    Int32 targetid = MQ.Query<Int32>("${Target.ID}");
                    if (targetid > 0)
                    {
                        Spawn s;
                        if (_spawns.TryByID(targetid, out s))
                        {
                            e3util.TryMoveToLoc(s.X, s.Y);
                            System.Text.StringBuilder sb = new StringBuilder();
                            bool first = true;
                            foreach (string arg in x.args)
                            {
                                if (!first) sb.Append(" ");
                                sb.Append(arg);
                                first = false;
                            }
                            string message = sb.ToString();
                            E3._bots.BroadcastCommandToGroup($"/bark-send {targetid} \"{message}\"");
                            Int32 currentZone = E3._zoneID;

                            for (Int32 i = 0; i < 5; i++)
                            {
                                MQ.Cmd($"/say {message}");
                                MQ.Delay(1500);
                                Int32 tzone = MQ.Query<Int32>("${Zone.ID}");
                                if (tzone != currentZone)
                                {
                                    break;
                                }
                            }

                        }


                    }
                }
            });
            EventProcessor.RegisterCommand("/bark-send", (x) =>
            {
                if (x.args.Count > 1)
                {
                    Int32 targetid;
                    if (Int32.TryParse(x.args[0], out targetid))
                    {
                        if (targetid > 0)
                        {
                            Spawn s;
                            if (_spawns.TryByID(targetid, out s))
                            {
                                Casting.TrueTarget(targetid);
                                MQ.Delay(100);
                                e3util.TryMoveToLoc(s.X, s.Y);

                                string message = x.args[1];
                                Int32 currentZone = E3._zoneID;
                                for (Int32 i = 0; i < 5; i++)
                                {
                                    MQ.Cmd($"/say {message}");
                                    MQ.Delay(1000);
                                    Int32 tzone = MQ.Query<Int32>("${Zone.ID}");
                                    if (tzone != currentZone)
                                    {
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }
            });
            EventProcessor.RegisterCommand("/fds", (x) =>
            {

                if (x.args.Count > 0)
                {
                    string slot = x.args[0];
                    if (FDSPrint(slot))
                    {
                        if (x.args.Count == 1)
                        {
                            E3._bots.BroadcastCommandToGroup($"/fds {slot} group");
                        }

                    }
                }

            });

            EventProcessor.RegisterCommand("/followoff", (x) =>
            {
                RemoveFollow();
                if (x.args.Count == 0)
                {
                    //we are telling people to follow us
                    E3._bots.BroadcastCommandToGroup("/followoff all");
                }
            });

            EventProcessor.RegisterCommand("/evac", (x) =>
            {

                if (x.args.Count > 0)
                {
                    //someone told us to gate
                    Spell s;
                    if (!Spell._loadedSpellsByName.TryGetValue("Exodus", out s))
                    {
                        s = new Spell("Exodus");
                    }
                    if (Casting.CheckReady(s))
                    {
                        Casting.Cast(0, s);
                    }
                    else
                    {

                        //lets try and do evac spell?
                        string spellToCheck = string.Empty;
                        if (E3._currentClass == Class.Wizard)
                        {
                            spellToCheck = "Evacuate";
                        }
                        else if (E3._currentClass == Class.Druid)
                        {
                            spellToCheck = "Succor";
                        }

                        if (spellToCheck != String.Empty && MQ.Query<bool>($"${{Me.Book[{spellToCheck}]}}"))
                        {
                            if (!Spell._loadedSpellsByName.TryGetValue(spellToCheck, out s))
                            {
                                s = new Spell(spellToCheck);
                            }
                            if (Casting.CheckReady(s) && Casting.CheckMana(s))
                            {
                                Casting.Cast(0, s);
                            }
                        }
                    }
                }
                else
                {
                    E3._bots.BroadcastCommandToGroup("/evac me");
                }
            });

            EventProcessor.RegisterCommand("/fic", (x) =>
            {
                string itemName = x.args[0];
                if (x.args.Count == 1)
                {
                    E3._bots.BroadcastCommandToGroup($"/fic \"{itemName}\" all",x);
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
                    E3._bots.BroadcastCommandToGroup($"/finditem \"{itemName}\" all", x);
                }

                if (!e3util.FilterMe(x))
                {
                    FindItemCompact(itemName);
                }

            });

            EventProcessor.RegisterCommand("/e3p", (x) =>
            {
                //swap them
                _isPaused = _isPaused ? false : true;
                if (_isPaused) MQ.Write("\arPAUSING E3!");
                if (!_isPaused) MQ.Write("\agRunning E3 again!");

            });
            EventProcessor.RegisterCommand("/rtz", (x) =>
            {
                if (x.args.Count > 0)
                {
                    //someone telling us to rtz
                    double heading;
                    if (double.TryParse(x.args[0], out heading))
                    {
                        Int32 currentZone = MQ.Query<Int32>("${Zone.ID}");
                        MQ.Cmd($"/face fast heading {heading*-1}");
                        MQ.Cmd("/keypress forward hold");
                        MQ.Delay(1000);
                        Int32 counter = 0;
                        while (E3._zoneID == currentZone && counter < 20)
                        {
                            counter++;
                            MQ.Delay(100);
                            currentZone = MQ.Query<Int32>("${Zone.ID}");
                        }
                        MQ.Cmd("/keypress forward");

                    }
                }
                else
                {
                    //tell others to rtz
                    //get our faced heading
                    double heading = MQ.Query<double>("${Me.Heading.Degrees}");
                    E3._bots.BroadcastCommandToGroup($"/rtz {heading}");
                    MQ.Delay(500);
                    MQ.Cmd($"/face fast heading {heading*-1}");
                    MQ.Cmd("/keypress forward hold");

                }
            });
            //anchoron
            EventProcessor.RegisterCommand("/anchoron", (x) =>
            {
                if (x.args.Count > 0)
                {
                    Int32 targetid;
                    if (Int32.TryParse(x.args[0], out targetid))
                    {
                        _anchorTarget = targetid;
                    }
                }
                else
                {
                    Int32 targetid = MQ.Query<Int32>("${Target.ID}");
                    if (targetid > 0)
                    {
                        E3._bots.BroadcastCommandToGroup($"/anchoron {targetid}");
                    }
                }
            });
            EventProcessor.RegisterCommand("/chaseme", (x) =>
            {
                //chaseme <toon name>
                if (x.args.Count == 1 && x.args[0] != "off")
                {
                    if (!e3util.FilterMe(x))
                    {
                        Spawn s;
                        if (_spawns.TryByName(x.args[0], out s))
                        {
                            _chaseTarget = x.args[0];
                            _following = true;
                        }

                    }
                }
                //chanseme off
                else if (x.args.Count == 1 && x.args[0] == "off")
                {
                    E3._bots.BroadcastCommandToGroup($"/chaseme off {E3._currentName}",x);
                    _chaseTarget = String.Empty;
                    _following = false;
                }
                //chaseme off <toon name>
                else if (x.args.Count == 2 && x.args[0] == "off")
                {
                    if (!e3util.FilterMe(x))
                    {
                        _chaseTarget = String.Empty;
                        _following = false;

                    }
                }
                else
                {
                    E3._bots.BroadcastCommandToGroup($"/chaseme {E3._currentName}",x);
                    _following = false;
                }
            });
            EventProcessor.RegisterCommand("/anchoroff", (x) =>
            {
                _anchorTarget = 0;
                if (x.args.Count == 0)
                {
                    E3._bots.BroadcastCommandToGroup($"/anchoroff all");
                }

            });
            EventProcessor.RegisterCommand("/followme", (x) =>
            {
                string user = string.Empty;
                if (x.args.Count > 0)
                {
                    if(!e3util.FilterMe(x))
                    {
                        user = x.args[0];
                        Spawn s;
                        if (_spawns.TryByName(user, out s))
                        {
                            _followTargetName = user;
                            _following = false;
                            Assist.AssistOff();
                            AcquireFollow();
                        }

                    }
                }
                else
                {
                    //we are telling people to follow us
                    E3._bots.BroadcastCommandToGroup("/followme " + E3._characterSettings._characterName,x);
                }
            });


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
                if(augCount>0)
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
                    if(augCount>0)
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

            foreach(var value in report)
            {
                E3._bots.Broadcast(value);

            }
        }

        private static void VetAA(string vetAASpell, string command, Int32 argCount)
        {
            Spell s;
            if (!Spell._loadedSpellsByName.TryGetValue(vetAASpell, out s))
            {
                s = new Spell(vetAASpell);
            }
            if (argCount == 0)
            {
                if (Casting.CheckReady(s))
                {
                    Casting.Cast(0, s);
                }
                E3._bots.BroadcastCommandToGroup($"{command} all");
            }
            else
            {
                if (Casting.CheckReady(s))
                {
                    Casting.Cast(0, s);
                }
            }
        }

        private static readonly List<string> _fdsSlots = new List<string>() { "charm", "leftear", "head", "face", "rightear", "neck", "shoulder", "arms", "back", "leftwrist", "rightwrist", "ranged", "hands", "mainhand", "offhand", "leftfinger", "rightfinger", "chest", "legs", "feet", "waist", "powersource", "ammo", "fingers", "wrists", "ears" };
        /// <summary>
        /// for the /fds command
        /// </summary>
        /// <param name="slot"></param>
        /// 
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
                E3._bots.Broadcast("Cannot find slot. Valid slots are:" + String.Join(",", _fdsSlots));
                return false;
            }
        }
        public static void RefreshGroupMembers()
        {
            if (!e3util.ShouldCheck(ref _nextGroupCheck, _nextGroupCheckInterval)) return;

            Int32 groupCount = MQ.Query<Int32>("${Group}");
            groupCount++;
            if (groupCount != _groupMembers.Count)
            {
                _groupMembers.Clear();
                //refresh group members.
                //see if any  of our members have it.
                for (Int32 i = 0; i < groupCount; i++)
                {
                    Int32 id = MQ.Query<Int32>($"${{Group.Member[{i}].ID}}");
                    _groupMembers.Add(id);
                }
            }
        }
        public static void RemoveFollow()
        {
            _chaseTarget = String.Empty;
            _followTargetName = string.Empty;
            _following = false;
            MQ.Cmd("/squelch /afollow off");
            MQ.Cmd("/squelch /stick off");


        }
        [ClassInvoke(Data.Class.All)]
        public static void AcquireFollow()
        {

            if (!e3util.ShouldCheck(ref _nextFollowCheck, _nextFollowCheckInterval)) return;

            if (String.IsNullOrWhiteSpace(_followTargetName)) return;

            Spawn s;
            if (_spawns.TryByName(_followTargetName, out s))
            {
                if (s.Distance <= 250)
                {
                    if (!_following)
                    {
                        //they are in range
                        if (MQ.Query<bool>($"${{Spawn[{_followTargetName}].LineOfSight}}"))
                        {
                            if (Casting.TrueTarget(s.ID))
                            {
                                MQ.Delay(100);
                                //if a bot, use afollow, else use stick
                                MQ.Cmd("/afollow on nodoor");
                                _following = true;
                            }
                        }
                    }
                }
                else
                {
                    _following = false;
                }
            }
        }
        public static bool AmIDead()
        {
            //scan through our inventory looking for a container.
            for (Int32 i = 1; i <= 10; i++)
            {
                bool SlotExists = MQ.Query<bool>($"${{Me.Inventory[pack{i}]}}");
                if (SlotExists)
                {
                    return false;
                }
            }
            return true;
        }
        public static bool InCombat()
        {
            bool inCombat = MQ.Query<bool>("${Me.Combat}") || MQ.Query<bool>("${Me.CombatState.Equal[Combat]}") || Assist._isAssisting;
            return inCombat;
        }


        [ClassInvoke(Data.Class.ManaUsers)]
        public static void Check_ManaResources()
        {
            if (!e3util.ShouldCheck(ref _nextResourceCheck, _nextResourceCheckInterval)) return;


            if (E3._isInvis) return;
            if (Basics.AmIDead()) return;

            
            Int32 minMana = 40;
            Int32 minHP = 60;
            Int32 maxMana = 75;
            Int32 maxLoop = 25;

            Int32 totalClicksToTry = 40;
            //Int32 minManaToTryAndHeal = 1000;

            if (!InCombat())
            {
                minMana = 85;
                maxMana = 95;
            }

            Int32 pctMana = MQ.Query<Int32>("${Me.PctMana}");
            Int32 currentHps = MQ.Query<Int32>("${Me.CurrentHPs}");
            if (pctMana > minMana) return;

            if (E3._currentClass == Data.Class.Enchanter)
            {
                bool manaDrawBuff = MQ.Query<bool>("${Bool[${Me.Buff[Mana Draw]}]}") || MQ.Query<bool>("${Bool[${Me.Song[Mana Draw]}]}");
                if (manaDrawBuff)
                {
                    if (pctMana > 50)
                    {
                        return;
                    }
                }
            }

            if (E3._currentClass == Data.Class.Necromancer)
            {
                bool deathBloom = MQ.Query<bool>("${Bool[${Me.Buff[Death Bloom]}]}") || MQ.Query<bool>("${Bool[${Me.Song[Death Bloom]}]}");
                if (deathBloom)
                {
                    return;
                }
            }

            if (E3._currentClass == Data.Class.Shaman)
            {
                bool canniReady = MQ.Query<bool>("${Me.AltAbilityReady[Cannibalization]}");
              
                if (canniReady && currentHps > 7000)
                {
                    Spell s;
                    if (!Spell._loadedSpellsByName.TryGetValue("Cannibalization", out s))
                    {
                        s = new Spell("Cannibalization");
                    }
                    if (s.CastType != CastType.None)
                    {
                        Casting.Cast(0, s);
                        return;
                    }
                }
            }

            if (MQ.Query<bool>("${Me.ItemReady[Summoned: Large Modulation Shard]}"))
            {
                if (MQ.Query<Int32>("${Math.Calc[${Me.MaxMana} - ${Me.CurrentMana}]") > 3500 && currentHps > 6000)
                {
                    Spell s;
                    if (!Spell._loadedSpellsByName.TryGetValue("Summoned: Large Modulation Shard", out s))
                    {
                        s = new Spell("Summoned: Large Modulation Shard");
                    }
                    if (s.CastType != CastType.None)
                    {
                        Casting.Cast(0, s);
                        return;
                    }

                }
            }
            if (MQ.Query<bool>("${Me.ItemReady[Azure Mind Crystal III]}"))
            {
                if (MQ.Query<Int32>("${Math.Calc[${Me.MaxMana} - ${Me.CurrentMana}]") > 3500)
                {
                    Spell s;
                    if (!Spell._loadedSpellsByName.TryGetValue("Azure Mind Crystal III", out s))
                    {
                        s = new Spell("Azure Mind Crystal III");
                    }
                    if (s.CastType != CastType.None)
                    {
                        Casting.Cast(0, s);
                        return;
                    }

                }
            }

            if (E3._currentClass == Data.Class.Necromancer)
            {
                bool deathBloomReady = MQ.Query<bool>("${Me.AltAbilityReady[Death Bloom]}");
                if (deathBloomReady && currentHps > 8000)
                {
                    Spell s;
                    if (!Spell._loadedSpellsByName.TryGetValue("Death Bloom", out s))
                    {
                        s = new Spell("Death Bloom");
                    }
                    if (s.CastType != CastType.None)
                    {
                        Casting.Cast(0, s);
                        return;
                    }
                }
            }
            if (E3._currentClass == Data.Class.Enchanter)
            {
                bool manaDrawReady = MQ.Query<bool>("${Me.AltAbilityReady[Mana Draw]}");
                if (manaDrawReady)
                {
                    Spell s;
                    if (!Spell._loadedSpellsByName.TryGetValue("Mana Draw", out s))
                    {
                        s = new Spell("Mana Draw");
                    }
                    if (s.CastType != CastType.None)
                    {
                        Casting.Cast(0, s);
                        return;
                    }
                }
            }
            //no manastone in pok
            bool pok = MQ.Query<bool>("${Zone.ShortName.Equal[poknowledge]}");
            if (pok) return;

            bool hasManaStone = MQ.Query<bool>("${Bool[${FindItem[=Manastone]}]}");

            if (hasManaStone)
            {

                MQ.Write("\agUsing Manastone...");
                Int32 pctHps = MQ.Query<Int32>("${Me.PctHPs}");
                pctMana = MQ.Query<Int32>("${Me.PctMana}");
                Int32 currentLoop = 0;
                while (pctHps > minHP && pctMana < maxMana)
                {
                    currentLoop++;
                    Int32 currentMana = MQ.Query<Int32>("${Me.CurrentMana}");

                    for (Int32 i = 0; i < totalClicksToTry; i++)
                    {
                        MQ.Cmd("/useitem \"Manastone\"");
                    }
                    //allow mq to have the commands sent to the server
                    MQ.Delay(50);
                    if ((E3._currentClass & Class.Priest) == E3._currentClass)
                    {
                        if (Heals.SomeoneNeedsHealing(currentMana, pctMana))
                        {
                            return;
                        }
                    }
                    if (currentLoop > maxLoop)
                    {
                        return;
                    }
                    
                    pctHps = MQ.Query<Int32>("${Me.PctHPs}");
                    pctMana = MQ.Query<Int32>("${Me.PctMana}");
                }

            }

        }
        public static string _chaseTarget = String.Empty;
        [ClassInvoke(Data.Class.All)]
        public static void Check_Chase()
        {
            if (!e3util.ShouldCheck(ref _nextChaseCheck, _nextChaseCheckInterval)) return;

            if (_chaseTarget != String.Empty && !Assist._isAssisting)
            {
                double distance = MQ.Query<double>($"${{Spawn[={_chaseTarget}].Distance}}");

                if (distance != -1)
                {
                    bool InLoS = MQ.Query<bool>($"${{Spawn[={_chaseTarget}].LineOfSight}}");
                    bool navLoaded = MQ.Query<bool>("${Bool[${Navigation.MeshLoaded}]}");
                    if (navLoaded)
                    {
                        if (distance > 10)
                        {
                            bool navActive = MQ.Query<bool>("${Navigation.Active}");
                            if (!navActive)
                            {
                                Int32 spawnID = MQ.Query<Int32>($"${{Spawn[={_chaseTarget}].ID}}");
                                bool pathExists = MQ.Query<bool>($"${{Navigation.PathExists[id {spawnID}]}}");
                                if (pathExists)
                                {
                                    MQ.Cmd($"/squelch /nav id {spawnID} log=error");
                                }
                                else
                                {
                                    //are they in LOS?
                                    if (InLoS)
                                    {
                                        double x = MQ.Query<double>($"${{Spawn[={_chaseTarget}].X}}");
                                        double y = MQ.Query<double>($"${{Spawn[={_chaseTarget}].Y}}");
                                        e3util.TryMoveToLoc(x, y, 5, -1);
                                    }

                                }
                            }
                        }

                    }
                    else
                    {
                        if (distance > 5 && distance < 150 && InLoS)
                        {
                            double x = MQ.Query<double>($"${{Spawn[={_chaseTarget}].X}}");
                            double y = MQ.Query<double>($"${{Spawn[={_chaseTarget}].Y}}");
                            e3util.TryMoveToLoc(x, y, 5, -1);
                        }
                    }
                }
            }
        }
        public static Int32 _anchorTarget = 0;
        [ClassInvoke(Data.Class.All)]
        public static void Check_Anchor()
        {
            if (!e3util.ShouldCheck(ref _nextAnchorCheck, _nextAnchorCheckInterval)) return;

            if (_anchorTarget > 0 && !Assist._isAssisting)
            {
                _spawns.RefreshList();
                Spawn s;
                if (_spawns.TryByID(_anchorTarget, out s))
                {
                    if (s.Distance > 20 && s.Distance < 150)
                    {
                        e3util.TryMoveToLoc(s.X, s.Y);
                    }
                }
            }
        }
        [ClassInvoke(Data.Class.All)]
        public static void Check_AutoMed()
        {
            if (!e3util.ShouldCheck(ref _nextAutoMedCheck, _nextAutoMedCheckInterval)) return;
            Int32 autoMedPct = E3._generalSettings.General_AutoMedBreakPctMana;
            if (autoMedPct == 0) return;
            if (!E3._characterSettings.Misc_AutoMedBreak) return;

            if (_following || InCombat()) return;

            bool amIStanding = MQ.Query<bool>("${Me.Standing}");

            if (amIStanding && autoMedPct > 0)
            {
                Int32 pctMana = MQ.Query<Int32>("${Me.PctMana}");
                Int32 pctEndurance = MQ.Query<Int32>("${Me.PctEndurance}");

                if (pctMana < autoMedPct && (E3._currentClass & Class.ManaUsers)== E3._currentClass)
                {
                    MQ.Cmd("/sit");
                }
                if (pctEndurance < autoMedPct)
                {
                    MQ.Cmd("/sit");
                }
            }
        }

    }
}
