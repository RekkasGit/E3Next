using E3Core.Data;
using E3Core.Settings;
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
        public static Int32 _followTargetID = 0;
        public static string _followTargetName = String.Empty;

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
        public static void Init()
        {
            RegisterEventsCasting();
        }
        public static void Reset()
        {
            _anchorTarget = 0;
        }
        static void RegisterEventsCasting()
        {
           
            EventProcessor.RegisterEvent("InviteToGroup", "(.+) invites you to join a group.", (x) => {

                MQ.Cmd("/invite");
                MQ.Delay(300);

            });
            EventProcessor.RegisterEvent("InviteToRaid", "(.+) invites you to join a raid.", (x) => {
               
                MQ.Delay(500);
                MQ.Cmd("/raidaccept");

            });

            EventProcessor.RegisterEvent("InviteToDZ", "(.+) tells you, 'dzadd'", (x) => {
                if(x.match.Groups.Count>1)
                {
                    MQ.Cmd($"/dzadd {x.match.Groups[1].Value}");
                }
            });
            EventProcessor.RegisterEvent("InviteToDZ", "(.+) tells you, 'raidadd'", (x) => {
                if (x.match.Groups.Count > 1)
                {
                    MQ.Cmd($"/raidinvite {x.match.Groups[1].Value}");
                }
            });

            EventProcessor.RegisterCommand("/clickit", (x) =>
            {
                MQ.Cmd("/multiline ; /doortarget ; /timed 5 /click left door ");
                  //we are telling people to follow us
                E3._bots.BroadcastCommandToGroup("/clickit");

                MQ.Delay(1000);
                
            });
            EventProcessor.RegisterCommand("/dropinvis", (x) =>
            {
                
                
                E3._bots.BroadcastCommandToGroup("/makemevisible");
                MQ.Cmd("/makemevisible");
           
            });

            EventProcessor.RegisterCommand("/armor", (x) =>
            {
                VetAA("Armor of Experience","/armor",x.args.Count);
            });
            EventProcessor.RegisterCommand("/intensity", (x) =>
            {
                VetAA("Intensity of the Resolute","/intensity", x.args.Count);
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
            EventProcessor.RegisterCommand("/jester", (x) =>
            {
                VetAA("Chaotic Jester", "/jester", x.args.Count);
            });
            EventProcessor.RegisterCommand("/bark", (x) =>
            {

                //rebuild the bark message, and do a /say
                if(x.args.Count>0)
                {
                    Int32 targetid = MQ.Query<Int32>("${Target.ID}");
                    if (targetid > 0)
                    {
                        Spawn s;
                        if(_spawns.TryByID(targetid,out s))
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
                if(x.args.Count>1)
                {
                    Int32 targetid;
                    if(Int32.TryParse(x.args[0],out targetid))
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

                if(x.args.Count>0)
                {
                    string slot = x.args[0];
                    if(FDSPrint(slot))
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
            EventProcessor.RegisterCommand("/e3p", (x) =>
            {
                //swap them
                 _isPaused = _isPaused?false:true;
                if(_isPaused) MQ.Write("\arPAUSING E3!");
                if (!_isPaused) MQ.Write("\agRunning E3 again!");

            });

            //anchoron
            EventProcessor.RegisterCommand("/anchoron", (x) =>
            {
                if(x.args.Count>0)
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
                    if(targetid>0)
                    {
                        E3._bots.BroadcastCommandToGroup($"/anchoron {targetid}");
                    }
                }
            });
            EventProcessor.RegisterCommand("/anchoroff", (x) =>
            {
                _anchorTarget = 0;
                if (x.args.Count==0)
                {
                    E3._bots.BroadcastCommandToGroup($"/anchoroff all");
                }

            });
            EventProcessor.RegisterCommand("/followme", (x) =>
            {
                string user = string.Empty;
                if(x.args.Count>0)
                {
                    user = x.args[0];
                    //we have someone to follow.
                    _followTargetID = MQ.Query<Int32>($"${{Spawn[{user}].ID}}");
                    if(_followTargetID > 0)
                    {
                        _followTargetName = user;
                        _following = true;
                        Assist.AssistOff();
                        AcquireFollow();
                    }
                }
                else
                {
                    //we are telling people to follow us
                    E3._bots.BroadcastCommandToGroup("/followme " + E3._characterSettings._characterName);
                }
            });
           

        }

        private static void VetAA(string vetAASpell,string command, Int32 argCount)
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
                MQ.Broadcast("Cannot find slot. Valid slots are:" + String.Join(",", _fdsSlots));
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
            _followTargetID = 0;
            _followTargetName = string.Empty;
            MQ.Cmd("/squelch /afollow off");
            MQ.Cmd("/squelch /stick off");
           
        }

        public static void AcquireFollow()
        {

            Int32 instanceCount = MQ.Query<Int32>($"${{SpawnCount[id {_followTargetID} radius 250]}}");

            if (instanceCount > 0)
            {
                //they are in range
                if (MQ.Query<bool>($"${{Spawn[{_followTargetName}].LineOfSight}}"))
                {
                    Casting.TrueTarget(_followTargetID);
                    //if a bot, use afollow, else use stick
                    if (E3._bots.InZone(_followTargetName))
                    {
                        MQ.Cmd("/afollow on");
                    }
                    else
                    {
                        MQ.Cmd("/squelch /stick hold 20 uw");
                    }
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

            bool pok = MQ.Query<bool>("${Zone.ShortName.Equal[poknowledge]}");
            if (pok) return;

            Int32 minMana = 35;
            Int32 minHP = 60;
            Int32 maxMana = 65;
            Int32 maxLoop = 10;

            Int32 totalClicksToTry = 40;
            Int32 minManaToTryAndHeal = 1000;

            if (!InCombat())
            {
                minMana = 70;
                maxMana = 95;
            }

            Int32 pctMana = MQ.Query<Int32>("${Me.PctMana}");
            Int32 currentHps = MQ.Query<Int32>("${Me.CurrentHPs}");
            if (pctMana > minMana) return;

            if(E3._currentClass== Data.Class.Enchanter)
            {
                bool manaDrawBuff = MQ.Query<bool>("${Bool[${Me.Buff[Mana Draw]}]}") || MQ.Query<bool>("${Bool[${Me.Song[Mana Draw]}]}");
                if(manaDrawBuff)
                {
                    if(pctMana>50)
                    {
                        return;
                    }
                }
            }

            if(E3._currentClass== Data.Class.Necromancer)
            {
                bool deathBloom = MQ.Query<bool>("${Bool[${Me.Buff[Death Bloom]}]}") || MQ.Query<bool>("${Bool[${Me.Song[Death Bloom]}]}");
                if(deathBloom)
                {
                    return;
                }
            }

            if (E3._currentClass == Data.Class.Shaman)
            {
                bool canniReady = MQ.Query<bool>("${Me.AltAbilityReady[Cannibalization]}");
                if (canniReady)
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
                bool deathBloomReady = MQ.Query<bool>("${Me.AltAbilityReady[Death Bloom]}") && !AmIDead();
                if (deathBloomReady)
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
                bool manaDrawReady = MQ.Query<bool>("${Me.AltAbilityReady[Mana Draw]}") && !AmIDead();
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

            bool hasManaStone = MQ.Query<bool>("${Bool[${FindItem[=Manastone]}]}");

            if(hasManaStone)
            {

                MQ.Write("\agUsing Manastone...");
                Int32 pctHps = MQ.Query<Int32>("${Me.PctHPs}");
                pctMana = MQ.Query<Int32>("${Me.PctMana}");
                Int32 currentLoop = 0;
                while(pctHps>minHP && pctMana < maxMana)
                {
                    currentLoop++;
                    Int32 currentMana = MQ.Query<Int32>("${Me.CurrentMana}");

                    for(Int32 i =0;i<totalClicksToTry;i++)
                    {
                        MQ.Cmd("/useitem \"Manastone\"");
                    }
                    if((E3._currentClass & Class.Priest)==E3._currentClass)
                    {
                        if (Heals.SomeoneNeedsHealing(currentMana, pctMana))
                        {
                            return;
                        }
                    }
                    MQ.Delay(50);
                    if (Basics.InCombat())
                    {
                        if(currentLoop>maxLoop)
                        {
                            return;
                        }
                    }
                    pctHps = MQ.Query<Int32>("${Me.PctHPs}");
                    pctMana = MQ.Query<Int32>("${Me.PctMana}");
                }

            }

        }

        public static Int32 _anchorTarget = 0;
        [ClassInvoke(Data.Class.All)]
        public static void Check_Anchor()
        {
            if (!e3util.ShouldCheck(ref _nextAnchorCheck, _nextAnchorCheckInterval)) return;

            if (_anchorTarget>0 && !InCombat())
            {
                _spawns.RefreshList();
                Spawn s;
                if(_spawns.TryByID(_anchorTarget, out s))
                {
                    if(s.Distance>20 && s.Distance<150)
                    {
                        e3util.TryMoveToLoc(s.X, s.Y);
                    }
                }
            }
        }
        [ClassInvoke(Data.Class.All)]
        public static void Check_AutoMed()
        {

            if (_following || InCombat()) return;
            Int32 autoMedPct = E3._generalSettings.General_AutoMedBreakPctMana;
            if (autoMedPct == 0) return;

            if (!e3util.ShouldCheck(ref _nextAutoMedCheck, _nextAutoMedCheckInterval)) return;

            bool amIStanding = MQ.Query<bool>("${Me.Standing}");

            if (!amIStanding && autoMedPct > 0)
            {
                Int32 pctMana = MQ.Query<Int32>("${Me.PctMana}");
                Int32 pctEndurance = MQ.Query<Int32>("${Me.PctEndurance}");

                if (pctMana < autoMedPct)
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
