using E3Core.Data;
using E3Core.Settings;
using E3Core.Utility;
using MonoCore;
using System;
using System.Collections.Generic;


namespace E3Core.Processors
{
    public static class Rez
    {

        public static Logging _log = E3.Log;
        private static IMQ MQ = E3.MQ;
        private static ISpawns _spawns = E3.Spawns;
		[ExposedData("Rez", "WaitingOnRez")]
		public static bool _waitingOnRez = false;
        private static long _nextAutoRezCheck = 0;
        private static long _nextAutoRezCheckInterval = 1000;
        private static TimeSpan _rezDelayTimeSpan = new TimeSpan(0, 1, 0);

        private static long _nextRezDialogCheck = 0;
        private static long _nextRezDialogCheckInterval = 1000;
		//should accepting rezes be paused
		[ExposedData("Rez", "PauseRez")]
		private static bool _pauseRez = false;

		private static readonly Spell _divineRes = new Spell("Divine Resurrection");
        private static readonly HashSet<string> _classesToDivineRez = new HashSet<string> { "Cleric", "Warrior", "Paladin", "Shadow Knight" };
		[ExposedData("Rez", "SkipAutoRez")]
		private static bool _skipAutoRez = false;
        private static Dictionary<int, DateTime> _recentlyRezzed = new Dictionary<int, DateTime>();
		[ExposedData("Rez", "CorpsesToRemoveFromRecentlyRezzed")]
		private static List<int> _corpsesToRemoveFromRecentlyRezzed = new List<int>();

        [SubSystemInit]
        public static void Rez_Init()
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
            if (Basics.InCombat()) return;

            if (e3util.IsRezDiaglogBoxOpen())
            {
                MQ.Delay(1000);
                MQ.Cmd("/nomodkey /notify ConfirmationDialogBox Yes_Button leftmouseup",2000);//start zone
                    
                //zone may to happen
                MQ.Delay(15000, "${Spawn[${Me}'s].ID}");
				//save the current zone we have just zoned into
				Zoning.Zoned(MQ.Query<Int32>("${Zone.ID}"));

				if (!MQ.Query<bool>("${Spawn[${Me}'s].ID}"))
                {
                    //something went wrong kick out.
                    return;
                }
				Int32 totalLootAttempts = 0;
				tryLootAgain:

				if (totalLootAttempts > 1) return;
				//okay, we are rezed, and now need to loot our corpse. 
				//it should be the closest so we will use spawn as it will grab the closest first.
				Int32 corpseID = MQ.Query<Int32>("${Spawn[${Me}'s].ID}");
                Casting.TrueTarget(corpseID);

                //check if its rezable.
                if (!CanRez(corpseID))
                {
					MQ.Cmd("/corpse",1000);
                        
                    MQ.Cmd("/loot");
                    MQ.Delay(1000, "${Window[LootWnd].Open}");
                    MQ.Cmd("/nomodkey /notify LootWnd LootAllButton leftmouseup");

					MQ.Delay(300);
					Int32 corpseItems = MQ.Query<Int32>("${Corpse.Items}");

					Int32 lastCorpseItemCount = corpseItems;
					Int32 corpseItemTryCount = 0;
					while(corpseItems>0 || MQ.Query<bool>("${Window[LootWnd].Open}"))
					{
						MQ.Delay(2000);
						corpseItems = MQ.Query<Int32>("${Corpse.Items}");
						if (lastCorpseItemCount == corpseItems)
						{
							corpseItemTryCount++;
							if (corpseItemTryCount > 2)
							{
								//we are stuck, reload the entire UI, unsure if it will return before the UI is finished
								MQ.Cmd("/reload");
								totalLootAttempts++;

								if (totalLootAttempts > 1)
								{
									_waitingOnRez = false;
									e3util.Beep();
									E3.Bots.Broadcast("\agWaitForRez:\arERROR! \atLoot Window stuck open, please help.");
									E3.Bots.BroadcastCommand("/popup ${Me} loot window stuck open", false);
									MQ.Delay(1000);
									return;

								}
								goto tryLootAgain;
							}
							//the number hasn't changed, increment our try count
						}
						else
						{
							lastCorpseItemCount = corpseItems;
						}
					}

					//We may have died while looting. Need to put us back into a state so we can reloot our corpse.
					if(Zoning.CurrentZone.Id!=MQ.Query<Int32>("${Zone.ID}"))
					{
						return;
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
            _canRezCache.Clear();

            foreach (var kvp in _recentlyRezzed)
            {
                // if < 1 minute since last rez attempt, skip
                if (DateTime.Now - kvp.Value < _rezDelayTimeSpan)
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
			
            foreach (var spell in _currentRezSpells)
			{
				if (Casting.CheckMana(spell) && Casting.CheckReady(spell))
				{
					if (!String.IsNullOrWhiteSpace(spell.Ifs))
					{
						if (!Casting.Ifs(spell))
						{
							continue;
						}
					}
				
					foreach (var corpse in _corpseList)
					{
						if (_spawns.TryByID(corpse, out var spawn))
						{
							EventProcessor.ProcessEventsInQueues("/wipe");
							EventProcessor.ProcessEventsInQueues("/wipe all");
							EventProcessor.ProcessEventsInQueues("/autorezon");
							EventProcessor.ProcessEventsInQueues("/autorezon all");
							if (_skipAutoRez) return;
							if (Basics.AmIDead()) return;

							if (_recentlyRezzed.TryGetValue(corpse, out var lastRez))
							{
								// if < 1 minute since last rez attempt, skip
								if (DateTime.Now - lastRez < new TimeSpan(0, 1, 0))
								{
									continue;
								}
							}

							// only care about group or raid members
							var inGroup = MQ.Query<bool>($"${{Group.Member[{spawn.DisplayName}]}}");
							var inRaid = MQ.Query<bool>($"${{Raid.Member[{spawn.DisplayName}]}}");
                            var inZone = _spawns.TryByName(spawn.DisplayName, out _);

                            //don't auto rez if in zone.
                            if (inZone) continue;

							if (!inGroup && !inRaid)
							{
								continue;
							}

							string spawnsCleanName = spawn.CleanName.Replace("'s corpse", "");
							if (!String.IsNullOrEmpty(spell.CastTarget))
							{
								if (!String.Equals(spell.CastTarget, spawnsCleanName, StringComparison.OrdinalIgnoreCase)) continue;
							}
							//before we try and target/consider, lets make sure noone needs heals.
							if (Basics.InCombat() && (E3.CurrentClass & Class.Priest) == E3.CurrentClass)
							{
								var currentMana = MQ.Query<int>("${Me.CurrentMana}");
								var pctMana = MQ.Query<int>("${Me.PctMana}");
								if (Heals.SomeoneNeedsHealing(null, currentMana, pctMana))
								{
									return;
								}
							}
							if (Casting.TrueTarget(spawn.ID))
							{
                                //this can take a bit of time, thus if it fails, we add to recently rezed.
								if (!CanRez(spawn.ID))
								{
									_recentlyRezzed.Add(spawn.ID, DateTime.Now);
									continue;
								}
								if (Basics.InCombat())
								{
									E3.Bots.Broadcast($"Trying to rez {spawn.DisplayName}");
									MQ.Cmd("/corpse");
									var result = Casting.Cast(spawn.ID, spell);
									if (result == CastReturn.CAST_INTERRUPTFORHEAL) return;
									if (result == CastReturn.CAST_SUCCESS)
									{
										_recentlyRezzed.Add(spawn.ID, DateTime.Now);
										break;
									}
								}
								else
								{
									E3.Bots.Broadcast($"Trying to rez {spawn.DisplayName}");
									MQ.Cmd("/corpse");
									var result = Casting.Cast(spawn.ID, spell, null, true, true);
									if (result == CastReturn.CAST_INTERRUPTFORHEAL) return;
									if (result == CastReturn.CAST_SUCCESS)
									{
										_recentlyRezzed.Add(spawn.ID, DateTime.Now);
										break;
									}
								}
							}
						}
					}
				}
			}
        }
        public static Dictionary<Int32,bool> _canRezCache = new Dictionary<Int32,bool>();
        public static bool CanRez(Int32 corpseID)
        {

            if(_canRezCache.ContainsKey(corpseID))
            {
                return _canRezCache[corpseID];
            }
			if(Casting.TrueTarget(corpseID))
            {
				MQ.Cmd("/consider", 1000);

				//check for the event.
				if (HasEventItem("CanRez"))
				{

					_canRezCache.Add(corpseID, true);
					//its rezable
					return true;
				}
				else if (HasEventItem("CanNotRez"))
				{
					_canRezCache.Add(corpseID, false);
					return false;
				}
				else
				{
					//dunno, error?
					E3.Bots.Broadcast("\agWaitForRez:\arERROR! \atUnsure if we can loot, assuming no.");
					return true;
				}
			}

            return false;
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
                        if (CanRez(s.ID) && Casting.CheckMana(spell) && Casting.CheckReady(spell))
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
                    if (!CanRez(s.ID))
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
		
					E3.Bots.Broadcast($"Rezing {s.DisplayName}");
		            foreach (var spell in _currentRezSpells)
                    {
                        if (Casting.CheckMana(spell) && Casting.CheckReady(spell))
						{
							E3.Bots.Broadcast($"Trying to rez {s.DisplayName}");
							MQ.Cmd("/corpse");
                            //rez, don't care about anything else.
							var result = Casting.Cast(s.ID, spell,null,true,true);
							if (result == CastReturn.CAST_INTERRUPTFORHEAL) return;
							if (result == CastReturn.CAST_SUCCESS)
							{
								corpsesRaised.Add(s.ID);
								rezRetries = 0;
								break;
							}
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
                if(rezRetries<15)
				{
					E3.Bots.Broadcast($"Delaying for 1 second as we still have corpses to rez and waiting on cooldowns. retries:{rezRetries} out of 15");
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
                if (Casting.CheckMana(spell) && Casting.CheckReady(spell))
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
            List<Spell> spellList = E3.CharacterSettings.Rez_RezSpells;
            if(rezType== RezType.Auto) spellList= E3.CharacterSettings.Rez_AutoRezSpells;
            foreach (var spell in spellList)
            {
                //if (MQ.Query<bool>($"${{FindItem[={spellName}]}}") || MQ.Query<bool>($"${{Me.AltAbility[{spellName}]}}") || MQ.Query<bool>($"${{Me.Book[{spellName}]}}"))
                {
                   _currentRezSpells.Add(spell);
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
			var canRezMessages = new List<string>
			{
				"This corpse can be resurrected",
				"This corpse's resurrection time will expire in"
			};
			EventProcessor.RegisterEvent("CanRez", canRezMessages, (x) =>
            {
              
            });
			var cannotRezMessages = new List<string>
			{
				"This corpse cannot be resurrected",
				"This corpse has already accepted a resurrection"
			};
			EventProcessor.RegisterEvent("CanNotRez", cannotRezMessages, (x) =>
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
            var consentEvents = new List<string> { "(.+) tells you, '(?i)Consent'", @"(.+) tells the raid,\s+'(?i)Consent'", "<(.+)> (?i)Consent" };
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
