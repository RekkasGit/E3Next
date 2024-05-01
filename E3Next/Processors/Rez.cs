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
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace E3Core.Processors
{
    public static class Rez
    {

        public static Logging _log = E3.Log;
        private static IMQ MQ = E3.MQ;
        private static ISpawns _spawns = E3.Spawns;
        public static bool _waitingOnRez = false;
        private static long _nextAutoRezCheck = 0;
        private static long _nextAutoRezCheckInterval = 1000;

        private static long _nextRezDialogCheck = 0;
        private static long _nextRezDialogCheckInterval = 1000;
		//should accepting rezes be paused
		private static bool _pauseRez = false;

		private static readonly Spell _divineRes = new Spell("Divine Resurrection");
        private static readonly HashSet<string> _classesToDivineRez = new HashSet<string> { "Cleric", "Warrior", "Paladin", "Shadow Knight" };
        private static bool _skipAutoRez = false;
        private static Dictionary<int, DateTime> _recentlyRezzed = new Dictionary<int, DateTime>();
        private static List<int> _corpsesToRemoveFromRecentlyRezzed = new List<int>();

        [SubSystemInit]
        public static void Init()
        {
            RegisterEvents();
            InitRezSpells();
        }

        public static bool IsWaiting()
        {
            if(Assist.IsAssisting)
            {
                return false;
            }
            return _waitingOnRez;
        }
        public static void Reset()
        {
            _waitingOnRez = false;
        }

        public static void Process()
        {
            if (!e3util.ShouldCheck(ref _nextRezDialogCheck, _nextRezDialogCheckInterval)) return;
            //check for dialog box

            //rez is paused kick out
            if (_pauseRez) return;

            //don't do this on live as a GM will kill you and see if you auto accept a rez.
            if (e3util.IsEQLive()) return;

            if (MQ.Query<bool>("${Window[ConfirmationDialogBox].Open}"))
            {
                //check if its a valid confirmation box
                string message = MQ.Query<string>("${Window[ConfirmationDialogBox].Child[cd_textoutput].Text}");
                if (!(message.Contains("percent)")||message.Contains("RESURRECT you.")))
                {
                    //MQ.Cmd("/nomodkey /notify ConfirmationDialogBox No_Button leftmouseup");
                    return; //not a rez dialog box, do not accept.
                }
                MQ.Cmd("/nomodkey /notify ConfirmationDialogBox Yes_Button leftmouseup",2000);//start zone
                    
                //zone may to happen
                MQ.Delay(30000, "${Spawn[${Me}'s].ID}");
                Zoning.Zoned(MQ.Query<Int32>("${Zone.ID}"));
                if (!MQ.Query<bool>("${Spawn[${Me}'s].ID}"))
                {
                    //something went wrong kick out.
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
                    MQ.Cmd("/corpse",1000);
                        
                    MQ.Cmd("/loot");
                    MQ.Delay(1000, "${Window[LootWnd].Open}");
                    MQ.Cmd("/nomodkey /notify LootWnd LootAllButton leftmouseup");
                    MQ.Delay(20000, "!${Window[LootWnd].Open}");

                    if (MQ.Query<bool>("${Window[LootWnd].Open}"))
                    {
                        _waitingOnRez = false;
                        e3util.Beep();
                        E3.Bots.Broadcast("\agWaitForRez:\arERROR! \atLoot Window stuck open, please help.");
                        E3.Bots.BroadcastCommand("/popup ${Me} loot window stuck open", false);
                        MQ.Delay(1000);
                        return;

                    }
                 
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

        public static void LootAllCorpses()
        {
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
                        MQ.Cmd("/corpse", 1000);

                        MQ.Cmd("/loot");
                        MQ.Delay(1000, "${Window[LootWnd].Open}");
                        MQ.Cmd("/nomodkey /notify LootWnd LootAllButton leftmouseup");
                        MQ.Delay(20000, "!${Window[LootWnd].Open}");

                        if (MQ.Query<bool>("${Window[LootWnd].Open}"))
                        {
                            _waitingOnRez = false;
                            e3util.Beep();
                            E3.Bots.Broadcast("\agWaitForRez:\arERROR! \atLoot Window stuck open, please help.");
                            E3.Bots.BroadcastCommand("/popup ${Me} loot window stuck open", false);
                            MQ.Delay(1000);
                            return;
                        }
                    }
                }
            }
        }
       
        public static void RefreshCorpseList(RezType rezType = RezType.AE)
        {
              //lets get a corpse list
            _corpseList.Clear();

            foreach (var kvp in _recentlyRezzed)
            {
                // if < 1 minute since last rez attempt, skip
                if (DateTime.Now - kvp.Value < new TimeSpan(0, 1, 0))
                {
                    continue;
                }
                else
                {
                    _corpsesToRemoveFromRecentlyRezzed.Add(kvp.Key);
                }
            }

            foreach(var corpse in _corpsesToRemoveFromRecentlyRezzed)
            {
                _recentlyRezzed.Remove(corpse);
            }

            _corpsesToRemoveFromRecentlyRezzed.Clear();

            bool rezOurself = false;

            //lets find the clerics in range
            foreach (var spawn in _spawns.Get())
            {
                if (spawn.Distance3D < 100 && spawn.DeityID != 0 && spawn.TypeDesc == "Corpse" && spawn.ClassShortName == "CLR")
                {
                    if (_recentlyRezzed.TryGetValue(spawn.ID, out _))
                    {
                        //E3.Bots.Broadcast($"\agSkipping {spawn.CleanName} because i rezzed it < 1 minute ago");
                        continue;
                    }
                    if (rezType== RezType.Group && !E3.Bots.IsMyBot(spawn.DisplayName))
                    {
                        continue;
                    }
                    if(rezType == RezType.GroupOrRaid && !(E3.Bots.IsMyBot(spawn.DisplayName) || MQ.Query<bool>($"${{Raid.Member[{spawn.DisplayName}]}}")))
                    {
                        continue;

                    }
                    if(spawn.DisplayName == E3.CurrentName)
                    {
                        //rez ourself last
                        rezOurself = true;
                        continue;
                    }
                    _corpseList.Add(spawn.ID);
                }
            }
            foreach (var spawn in _spawns.Get())
            {
                if (spawn.Distance3D < 100 && spawn.DeityID != 0 && spawn.TypeDesc == "Corpse" && (spawn.ClassShortName == "DRU" || spawn.ClassShortName == "SHM" || spawn.ClassShortName == "WAR"))
                {
                    if (_recentlyRezzed.TryGetValue(spawn.ID, out _))
                    {
                        //E3.Bots.Broadcast($"\agSkipping {spawn.CleanName}'s corpse because i rezzed it < 1 minute ago");
                        continue;
                    }
                    if (rezType == RezType.Group && !E3.Bots.IsMyBot(spawn.DisplayName))
                    {
                        continue;
                    }
                    if (rezType == RezType.GroupOrRaid && !(E3.Bots.IsMyBot(spawn.DisplayName) || MQ.Query<bool>($"${{Raid.Member[{spawn.DisplayName}]}}")))
                    {
                        continue;

                    }
                    if (spawn.DisplayName == E3.CurrentName)
                    {
                        //rez ourself last
                        rezOurself = true;
                        continue;
                    }
                    _corpseList.Add(spawn.ID);
                }
            }
            //everyone else
            foreach (var spawn in _spawns.Get())
            {
                if (spawn.Distance3D < 100 && spawn.DeityID != 0 && spawn.TypeDesc == "Corpse")
                {
                    if (_recentlyRezzed.TryGetValue(spawn.ID, out _))
                    {
                        //E3.Bots.Broadcast($"\agSkipping {spawn.CleanName}'s corpse because i rezzed it < 1 minute ago");
                        continue;
                    }
                    //lists are super small so contains is fine
                    if (!_corpseList.Contains(spawn.ID))
                    {
                        if (rezType == RezType.Group && !E3.Bots.IsMyBot(spawn.DisplayName))
                        {
                            continue;
                        }
                        if (rezType == RezType.GroupOrRaid && !(E3.Bots.IsMyBot(spawn.DisplayName) || MQ.Query<bool>($"${{Raid.Member[{spawn.DisplayName}]}}")))
                        {
                            continue;

                        }
                        if(spawn.DisplayName == E3.CurrentName)
                        {
                            //rez ourself last
                            rezOurself = true;
                            continue;
                        }
                        _corpseList.Add(spawn.ID);
                    }
                }
            }
            if(rezOurself)
            {
                foreach (var spawn in _spawns.Get())
                {
                    if (spawn.Distance3D < 100 && spawn.DeityID != 0 && spawn.TypeDesc == "Corpse")
                    {
                        //lists are super small so contains is fine
                        if (!_corpseList.Contains(spawn.ID))
                        {
                            if (spawn.DisplayName == E3.CurrentName)
                            {
                                _corpseList.Add(spawn.ID);
                            }

                        }
                    }
                }
            }
            
        }

        [ClassInvoke(Class.All)]
        public static void AutoRez()
        {
            if (E3.IsInvis) return;
            if (Zoning.CurrentZone.IsSafeZone) return;
            if (!E3.CharacterSettings.Rez_AutoRez) return;
            if (_skipAutoRez) return;
            if (!e3util.ShouldCheck(ref _nextAutoRezCheck, _nextAutoRezCheckInterval)) return;
            if (Basics.AmIDead()) return;
            
            RefreshCorpseList();
            InitRezSpells(RezType.Auto);

            //don't rez if we cannot rez.
            if (_currentRezSpells.Count == 0) return;
            foreach (var corpse in _corpseList)
            {
                if (_spawns.TryByID(corpse, out var spawn))
                {
                    if (_recentlyRezzed.TryGetValue(corpse, out var lastRez))
                    {
                        // if < 1 minute since last rez attempt, skip
                        if (DateTime.Now - lastRez < new TimeSpan(0, 1, 0))
                        {
                            //E3.Bots.Broadcast($"\agSkipping {spawn.CleanName}'s corpse because i rezzed it < 1 minute ago");
                            continue; 
                        }
                        else
                        {
                            _recentlyRezzed.Remove(corpse);
                        }
                    }
                    EventProcessor.ProcessEventsInQueues("/wipe");
                    EventProcessor.ProcessEventsInQueues("/wipe all");
                    EventProcessor.ProcessEventsInQueues("/autorezon");
                    EventProcessor.ProcessEventsInQueues("/autorezon all");
                    if (_skipAutoRez) return;
                    if (Basics.AmIDead()) return;
                    // only care about group or raid members
                    var inGroup = MQ.Query<bool>($"${{Group.Member[{spawn.DisplayName}]}}");
                    var inRaid = MQ.Query<bool>($"${{Raid.Member[{spawn.DisplayName}]}}");

                    if (!inGroup && !inRaid)
                    {
                        continue;
                    }
                    //if not a cleric, and the person is in raid but no in group, let clerics deal with it.
                    if (E3.CurrentClass != Class.Cleric && inRaid && !inGroup)
                    {
                        continue;
                    }

                    if (Basics.InCombat() && (E3.CurrentClass & Class.Priest) == E3.CurrentClass)
                    {
                        var currentMana = MQ.Query<int>("${Me.CurrentMana}");
                        var pctMana = MQ.Query<int>("${Me.PctMana}");
                        if (Heals.SomeoneNeedsHealing(null,currentMana, pctMana))
                        {
                            return;
                        }
                    }

                    if (Casting.TrueTarget(spawn.ID))
                    {
                        if (!CanRez())
                        {
                            _recentlyRezzed.Add(spawn.ID, DateTime.Now);
                            continue;
                        }
                        InitRezSpells(RezType.Auto);
                        if (_currentRezSpells.Count == 0) return;
                       	
                        E3.Bots.Broadcast($"Trying to rez {spawn.DisplayName}");
						MQ.Cmd("/corpse");
                        

                        // if it's a cleric or warrior corpse and we're in combat, try to use divine res
                        if (Basics.InCombat() && _classesToDivineRez.Contains(spawn.ClassName))
                        {
                            if (Casting.CheckReady(_divineRes))
                            {
                                Casting.Cast(spawn.ID, _divineRes);
                                break;
                            }
                        }
                        
                        foreach (var spell in _currentRezSpells)
                        {
                            if (Casting.CheckReady(spell) && Casting.CheckMana(spell))
                            {
                                if (Basics.InCombat())
                                {
                                    if (string.Equals(spell.SpellName, "Water Sprinkler of Nem Ankh"))
                                    {
                                        continue;
                                    }

                                    Casting.Cast(spawn.ID, spell, Heals.SomeoneNeedsHealing);
                                    _recentlyRezzed.Add(spawn.ID, DateTime.Now);
                                }
                                else
                                {
                                    Casting.Cast(spawn.ID, spell);
                                    _recentlyRezzed.Add(spawn.ID, DateTime.Now);
                                }
                                break;
                            }
                        }
                    }
                }
            }
        }

        public static bool CanRez()
        {
            MQ.Cmd("/consider",500);
            
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

        public static void TurnOffAutoRezSkip()
        {
            if (_skipAutoRez)
            {
                E3.Bots.Broadcast("\agTurning autorez back on.");
            }

            _skipAutoRez = false;
        }

        private static bool HasEventItem(string name)
        {
            if (EventProcessor.EventList[name].queuedEvents.Count > 0)
            {
                while (EventProcessor.EventList[name].queuedEvents.Count > 0)
                {
                    EventProcessor.EventMatch e;
                    EventProcessor.EventList[name].queuedEvents.TryDequeue(out e);
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

					MQ.Cmd("/corpse");

					foreach (var spell in _currentRezSpells)
                    {
                        if (Casting.CheckReady(spell) && Casting.CheckMana(spell) && CanRez())
                        {
                            E3.Bots.Broadcast($"Rezing {s.DisplayName}");
                            Casting.Cast(s.ID, spell);

                            return;
                        }
                    }
                }
            }
        }
        public enum RezType
        {
            AE,
            Group,
            GroupOrRaid,
            Auto,
            Normal
        }
        private static void MultiRez(RezType rezType = RezType.AE)
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
            Movement.PauseMovement();
           
            RefreshCorpseList(rezType);

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
                    //wait up to 12 sec for a spell to be ready
                    //maybe sit to med while waiting on spell?
                    if(!SpellListReady())
                    {
						Basics.CheckAutoMed();
					}
					MQ.Delay(12000, SpellListReady);
                    if(!SpellListReady())
                    {
                        //no spells ready, break out of loop. 
                        break;
                    }
					//assume consent was given
					MQ.Cmd("/corpse");

					E3.Bots.Broadcast($"Rezing {s.DisplayName}");
					
					

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
                    E3.Bots.Broadcast($"<\aoRez\aw> Wasn't able to rez \ap{s.CleanName}\aw due to cooldowns, try again.");
                }
            }
           

        }
        private static bool SpellListReady()
        {
            foreach (var spell in _currentRezSpells)
            {
                if (Casting.CheckReady(spell) && Casting.CheckMana(spell))
                {
                    return true;
                }
            }
            return false;
        }
        private static List<Data.Spell> _currentRezSpells = new List<Spell>();

        private static void InitRezSpells(RezType rezType = RezType.Normal)
        {
            _currentRezSpells.Clear();
            List<String> spellList = E3.CharacterSettings.Rez_RezSpells;

            if(rezType== RezType.Auto) spellList= E3.CharacterSettings.Rez_AutoRezSpells;

            foreach (var spellName in spellList)
            {
                //if (MQ.Query<bool>($"${{FindItem[={spellName}]}}") || MQ.Query<bool>($"${{Me.AltAbility[{spellName}]}}") || MQ.Query<bool>($"${{Me.Book[{spellName}]}}"))
                {
                    Data.Spell s;
                    if(!Spell.LoadedSpellsByName.TryGetValue(spellName,out s))
                    {
                        s = new Spell(spellName);
                    }
                    if(s.CastType!= CastingType.None)
                    {
                        _currentRezSpells.Add(s);

                    }
                }
            }
        }
       
        private static void GatherCorpses()
        {
            if (MQ.Query<bool>("${Raid.Members}"))
            {
                MQ.Cmd($"/rs Consent");
            }
            else
            {
                E3.Bots.Broadcast($"Consent");
            }

            RefreshCorpseList();
            foreach(var corpse in _corpseList)
            {
                Casting.TrueTarget(corpse);
                MQ.Cmd("/corpse");
            }

            MQ.Cmd("/squelch /target clear");
        }

        private static void RegisterEvents()
        {
			EventProcessor.RegisterCommand("/e3prez", (x) =>
			{
				e3util.ToggleBooleanSetting(ref _pauseRez, "Auto Rez Accept", x.args);
				
			});
			EventProcessor.RegisterCommand("/aerez", (x) =>
            {
                if (x.args.Count == 0)
                {
                    MultiRez();

                }
                else if (x.args.Count == 1)
                {
                    string user = x.args[0];

                    if (user == E3.CurrentName)
                    {
                        //its a me!
                        MultiRez();
                    }
                    else
                    {
                        E3.Bots.BroadcastCommandToPerson(user, "/aerez");
                    }

                }
            });
            EventProcessor.RegisterCommand("/grez", (x) =>
            {
                if (x.args.Count == 0)
                {
                    MultiRez(RezType.Group);

                }
                else if (x.args.Count == 1)
                {
                    string user = x.args[0];

                    if (user == E3.CurrentName)
                    {
                        //its a me!
                        MultiRez(RezType.Group);

                    }
                    else
                    {
                        E3.Bots.BroadcastCommandToPerson(user, "/grez");
                    }

                }
            });

            EventProcessor.RegisterCommand("/rrez", (x) =>
            {
                if (x.args.Count == 0)
                {
                    MultiRez(RezType.GroupOrRaid);

                }
                else if (x.args.Count == 1)
                {
                    string user = x.args[0];

                    if (user == E3.CurrentName)
                    {
                        //its a me!
                        MultiRez(RezType.GroupOrRaid);

                    }
                    else
                    {
                        E3.Bots.BroadcastCommandToPerson(user, "/rrez");
                    }

                }
            });

            //rezit <toon> ${target.ID}
            EventProcessor.RegisterCommand("/rezit", (x) =>
            {
               
                if (x.args.Count == 1)
                {
                    string user = x.args[0];
                    int targetid = 0;
                    if (int.TryParse(x.args[0], out targetid))
                    {
                        if (targetid > 0)
                        {
                            Spawn s;
                            if (_spawns.TryByID(targetid, out s))
                            {
                                SingleRez(targetid);
                            }
                            else
                            {
                                E3.Bots.Broadcast($"Rezit target {targetid} is not a valid spawn id.");
                                return;
                            }
                        }
                    }
                    else
                    {
                        targetid = MQ.Query<Int32>("${Target.ID}");
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

            EventProcessor.RegisterCommand("/wipe", x =>
            {
                if (x.args.Count == 0)
                {
                    E3.Bots.BroadcastCommand("/wipe all");
                }

                if (!E3.CharacterSettings.Rez_AutoRez) return;
                _skipAutoRez = true;
                E3.Bots.Broadcast("\agTemporarily turning autorez off. It will be turned back on next time I zone.");
            });

            EventProcessor.RegisterCommand("/autorezon", x =>
            {
                if (x.args.Count == 0)
                {
                    E3.Bots.BroadcastCommand("/autorezon all");
                }

                if (!E3.CharacterSettings.Rez_AutoRez) return;
                TurnOffAutoRezSkip();
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
            EventProcessor.RegisterCommand("/lootcorpses", x => LootAllCorpses());
            EventProcessor.RegisterCommand("/gathercorpses", x => GatherCorpses());
            var consentEvents = new List<string> { "(.+) tells you, '(?i)Consent'", "(.+) tells the raid,  '(?i)Consent'", "<(.+)> (?i)Consent" };
            EventProcessor.RegisterEvent("consent", consentEvents, x =>
            {
                if (x.match.Groups.Count > 1)
                {
                    var sender = x.match.Groups[1].Value;
                    // of course i know him. he's me
                    if (string.Equals(sender, E3.CurrentName)) return;

                    MQ.Cmd($"/consent {sender}");
                }
            });
        }

    }
}
