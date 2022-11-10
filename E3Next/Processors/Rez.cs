using E3Core.Data;
using E3Core.Settings;
using E3Core.Utility;
using MonoCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace E3Core.Processors
{
    public static class Rez
    {

        public static Logging _log = E3.Log;
        private static IMQ MQ = E3.Mq;
        private static ISpawns _spawns = E3.Spawns;
        public static bool _waitingOnRez = false;
        private static long _nextAutoRezCheck = 0;
        private static long _nextAutoRezCheckInterval = 10000;
        private static long _nextCorpseCheck = 0;
        private static long _nextCorpseCheckInterval = 10000;

        [SubSystemInit]
        public static void Init()
        {
            RegisterEvents();
            InitRezSpells();
        }

        public static bool IsWaiting()
        {
            return _waitingOnRez;
        }
        public static void Reset()
        {
            _waitingOnRez = false;
        }
        public static void Process()
        {


            if (_waitingOnRez)
            {
                //check for dialog box
                if (MQ.Query<bool>("${Window[ConfirmationDialogBox].Open}"))
                {
                    //check if its a valid confirmation box
                    string message = MQ.Query<string>("${Window[ConfirmationDialogBox].Child[cd_textoutput].Text}");
                    if (!(message.Contains("percent)")||message.Contains("RESURRECT you.")))
                    {
                        MQ.Cmd("/nomodkey /notify ConfirmationDialogBox No_Button leftmouseup");
                        return; //not a rez dialog box, do not accept.
                    }
                    MQ.Cmd("/nomodkey /notify ConfirmationDialogBox Yes_Button leftmouseup");
                    MQ.Delay(2000);//start zone
                    //zone may to happen
                    MQ.Delay(30000, "${Spawn[${Me}'s].ID}");
                    E3.ZoneID = MQ.Query<int>("${Zone.ID}");
                    if (!MQ.Query<bool>("${Spawn[${Me}'s].ID}"))
                    {
                        //something went rong kick out.
                        return;
                    }

                    //okay, we are rezed, and now need to loot our corpse. 
                    //it should be the closest so we will use spawn as it will grab the closest first.
                    Int32 corpseID = MQ.Query<Int32>("${Spawn[${Me}'s].ID}");
                    Casting.TrueTarget(corpseID);

                    //check if its rezable.
                    if (!CanRez())
                    {
                        tryLootAgain:
                        MQ.Cmd("/corpse");
                        MQ.Delay(1000);
                        MQ.Cmd("/loot");
                        MQ.Delay(1000, "${Window[LootWnd].Open}");
                        MQ.Cmd("/nomodkey /notify LootWnd LootAllButton leftmouseup");
                        MQ.Delay(20000, "!${Window[LootWnd].Open}");

                        if (MQ.Query<bool>("${Window[LootWnd].Open}"))
                        {
                            _waitingOnRez = false;
                            MQ.Cmd("/beep");
                            E3.Bots.Broadcast("\agWaitForRez:\arERROR! \atLoot Window stuck open, please help.");
                            MQ.Delay(1000);
                            return;

                        }
                        //maybe there is another corpse? lets find out.
                        //get all spawns within a 100 distance
                        //make sure we have the most up to date zones spawns, takes about 1-2ms so no real harm forcing the issue.
                        _spawns.RefreshList();
                        string corpseName = E3.CurrentName + "'s corpse";
                        foreach (var spawn in _spawns.Get())
                        {
                            if (spawn.CleanName.StartsWith(corpseName))
                            {
                                if (spawn.Distance < 100)
                                {
                                    Casting.TrueTarget(spawn.ID);
                                    MQ.Delay(500);
                                    if (!CanRez())
                                    {
                                        goto tryLootAgain;
                                    }
                                }
                            }
                        }
                        _waitingOnRez = false;
                        E3.Bots.Broadcast("\atReady to die again!");
                    }

                }
            }
        }

        [ClassInvoke(Class.Cleric)]
        public static void RefreshCorpseList()
        {
            if (!e3util.ShouldCheck(ref _nextCorpseCheck, _nextCorpseCheckInterval)) return;
            //lets get a corpse list
            _spawns.RefreshList();
            _corpseList.Clear();

            //lets find the clerics in range
            foreach (var spawn in _spawns.Get())
            {
                if (spawn.Distance3D < 100 && spawn.DeityID != 0 && spawn.TypeDesc == "Corpse" && spawn.ClassShortName == "CLR")
                {
                    _corpseList.Add(spawn.ID);
                }
            }
            foreach (var spawn in _spawns.Get())
            {
                if (spawn.Distance3D < 100 && spawn.DeityID != 0 && spawn.TypeDesc == "Corpse" && (spawn.ClassShortName == "DRU" || spawn.ClassShortName == "SHM" || spawn.ClassShortName == "WAR"))
                {
                    _corpseList.Add(spawn.ID);
                }
            }
            //everyone else
            foreach (var spawn in _spawns.Get())
            {
                if (spawn.Distance3D < 100 && spawn.DeityID != 0 && spawn.TypeDesc == "Corpse")
                {
                    //lists are super small so contains is fine
                    if (!_corpseList.Contains(spawn.ID))
                    {
                        _corpseList.Add(spawn.ID);
                    }
                }
            }
        }

        [ClassInvoke(Class.Cleric)]
        public static void AutoRez()
        {
            if (!e3util.ShouldCheck(ref _nextAutoRezCheck, _nextAutoRezCheckInterval)) return;
            foreach (var corpse in _corpseList)
            {
                if (_spawns.TryByID(corpse, out var spawn))
                {
                    // only care about group or raid members
                    var inGroup = MQ.Query<bool>($"${{Group.Member[{spawn.DiplayName}]}}");
                    var inRaid = MQ.Query<bool>($"${{Raid.Member[{spawn.DiplayName}]}}");

                    if (!inGroup && !inRaid)
                    {
                        continue;
                    }

                    if (Basics.InCombat())
                    {
                        var currentMana = MQ.Query<int>("${Me.CurrentMana}");
                        var pctMana = MQ.Query<int>("${Me.PctMana}");
                        if (Heals.SomeoneNeedsHealing(currentMana, pctMana))
                        {
                            return;
                        }
                    }

                    if (Casting.TrueTarget(spawn.ID))
                    {
                        if (!CanRez())
                        {
                            continue;
                        }

                        MQ.Cmd($"/t {spawn.DiplayName} Wait4Rez");
                        MQ.Delay(100);
                        MQ.Cmd("/corpse");
                        InitRezSpells();
                        foreach (var spell in _currentRezSpells)
                        {
                            if (Casting.CheckReady(spell) && Casting.CheckMana(spell))
                            {
                                Casting.Cast(spawn.ID, spell, Heals.SomeoneNeedsHealing);
                                break;
                            }
                        }
                    }
                }
            }
        }

        public static bool CanRez()
        {
            MQ.Cmd("/consider");
            MQ.Delay(500);
            //check for the event.
            if(HasEventItem("CanRez"))
            {
                //its rezable
                return true;
            }
            else if(HasEventItem("CanNotRez"))
            {
                return false;
            }
            else
            {
                //dunno, error?
                E3.Bots.Broadcast("\agWaitForRez:\arERROR! \atUnsure if we can loot, assuming no.");
                return true;
            }
        }
        private static bool HasEventItem(string name)
        {
            if (EventProcessor._eventList[name].queuedEvents.Count > 0)
            {
                while (EventProcessor._eventList[name].queuedEvents.Count > 0)
                {
                    EventProcessor.EventMatch e;
                    EventProcessor._eventList[name].queuedEvents.TryDequeue(out e);
                }

                return true;
            }
            return false;
        }
    
        private static List<Int32> _corpseList = new List<int>();
        private static void SingleRez(Int32 corpseID)
        {
            Spawn s;
            if (_spawns.TryByID(corpseID, out s))
            {
                if (s.DeityID != 0 && s.TypeDesc == "Corpse")
                {
                    //we do this to check if our rez tokens have been used up.
                    InitRezSpells();
                    Casting.TrueTarget(s.ID);
                    MQ.Cmd($"/tell {s.DiplayName} Wait4Rez");
                    MQ.Delay(100);
                    foreach (var spell in _currentRezSpells)
                    {
                        if (Casting.CheckReady(spell) && Casting.CheckMana(spell) && CanRez())
                        {

                            Casting.Cast(s.ID, spell);

                            return;
                        }
                    }
                }
            }
        }
        private static void AERez()
        {
            Int32 rezRetries = 0;
            retryRez:
            //we do this to check if our rez tokens have been used up.
            InitRezSpells();


            if (_currentRezSpells.Count==0)
            {
                E3.Bots.Broadcast("<\aoAERez\aw> \arI have no rez spells loaded");
                return;
            }

            Movement.RemoveFollow();

            List<Int32> corpsesRaised = new List<int>();
            foreach (var corpseid in _corpseList)
            {
                Spawn s;
                if (_spawns.TryByID(corpseid, out s))
                {
                    Casting.TrueTarget(s.ID);
                    if (!CanRez())
                    {
                        // still add it anyway so we don't keep trying to rez unrezzable things
                        corpsesRaised.Add(s.ID);
                        continue;
                    }

                    MQ.Cmd($"/tell {s.DiplayName} Wait4Rez");
                    MQ.Delay(1500); //long delays after tells
                    //assume consent was given
                    MQ.Cmd("/corpse");

               
                    foreach (var spell in _currentRezSpells)
                    {
                   
                        if (Casting.CheckReady(spell) && Casting.CheckMana(spell))
                        {
                            
                            Casting.Cast(s.ID, spell);
                            corpsesRaised.Add(s.ID);
                            rezRetries = 0;
                            break;
                        }
                    }
                }
            }

            foreach (var corpseId in corpsesRaised)
            {
                _corpseList.Remove(corpseId);
            }

            if(_corpseList.Count>0 && !Basics.InCombat())
            {
                //have some left over corpses we could rez
                rezRetries++;
                if(rezRetries<10)
                {
                    MQ.Delay(1000);
                    goto retryRez;

                }
            }

            //whatever is left didn't get raised.
            foreach(var corpseid in _corpseList)
            {
                Spawn s;
                if(_spawns.TryByID(corpseid,out s))
                {
                    E3.Bots.Broadcast($"<\aoAERez\aw> Wasn't able to rez \ap{s.CleanName}\aw due to cooldowns, try again.");
                }
            }


        }
        private static List<string> _resSpellList = new List<string>()
        {
            "Blessing of Resurrection","Water Sprinkler of Nem Ankh","Reviviscence","Token of Resurrection","Spiritual Awakening","Resurrection","Restoration","Resuscitate","Renewal","Revive","Reparation"
        };
        private static List<Data.Spell> _currentRezSpells = new List<Spell>();

        private static void InitRezSpells()
        {
            _currentRezSpells.Clear();
            foreach (var spellName in _resSpellList)
            {
                if (MQ.Query<bool>($"${{FindItem[={spellName}]}}"))
                {
                    _currentRezSpells.Add(new Spell(spellName));
                }
                if (MQ.Query<bool>($"${{Me.AltAbility[{spellName}]}}"))
                {
                    _currentRezSpells.Add(new Spell(spellName));
                }
                if (MQ.Query<bool>($"${{Me.Book[{spellName}]}}"))
                {
                    _currentRezSpells.Add(new Spell(spellName));
                }
            }

        }
        private static void RegisterEvents()
        {

            EventProcessor.RegisterCommand("/aerez", (x) =>
            {
                if (x.args.Count == 0)
                {
                    AERez();

                }
                else if (x.args.Count == 1)
                {
                    string user = x.args[0];

                    if (user == E3.CurrentName)
                    {
                        //its a me!
                        AERez();
                    }
                    else
                    {
                        E3.Bots.BroadcastCommandToPerson(user, "/aerez");
                    }

                }
            });

            //rezit <toon> ${target.ID}
            EventProcessor.RegisterCommand("/rezit", (x) =>
            {
               
                if (x.args.Count == 1)
                {
                    string user = x.args[0];
                    Int32 targetid = MQ.Query<Int32>("${Target.ID}");
                    if (targetid>0)
                    {
                        E3.Bots.BroadcastCommandToPerson(user, $"/rezit {targetid}");

                    }

                }
                else if (x.args.Count == 0)
                {
                    Int32 targetid = MQ.Query<Int32>("${Target.ID}");
                    if(targetid>0)
                    {
                        SingleRez(targetid);

                    }

                }
            });

            var deathMessages = new List<string>
            {
                "You died.",
                "You have been slain by"
            };

            EventProcessor.RegisterEvent("YourDead", deathMessages, (x) =>
            {
                Assist.AssistOff();
                _waitingOnRez = true;
            });

            EventProcessor.RegisterEvent("CanRez", "This corpse can be resurrected.", (x) =>
            {
              
            });
            EventProcessor.RegisterEvent("CanNotRez", "This corpse cannot be resurrected.", (x) =>
            {

            });
            EventProcessor.RegisterEvent("WaitForRez", "(.+) tells you, 'Wait4Rez'", (x) =>
            {
                if(x.match.Groups.Count>1)
                {
                    string user = x.match.Groups[1].Value;
                    E3.Bots.Broadcast("Being told to wait for rez by:" + user);
                    MQ.Cmd($"/consent {user}");
                    _waitingOnRez = true;
                }
            });
            EventProcessor.RegisterCommand("/WaitRez", (x) =>
            {
                if(x.args.Count>0)
                {
                    //is it us?
                    if(x.args[0].Equals(E3.CurrentName, StringComparison.OrdinalIgnoreCase))
                    {
                        //its us! lets wait
                        _waitingOnRez = true;
                    }
                }

            });
            EventProcessor.RegisterCommand("/TellRez", (x) =>
            {
                if (x.args.Count > 0)
                {
                    E3.Bots.BroadcastCommandToGroup($"/WaitRez {x.args[0]}");
                }
            });
        }

    }
}
