﻿using E3Core.Data;
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
  
    public static class BegForBuffs
    {
        class BuffQueuedItem
        {
            public String Requester = String.Empty;
            public String SpellTouse = String.Empty;
            public Spell Spell;
            public Int32 TargetID = 0;
            public Int64 TimeStamp = 0;
        }

        public static string _lastSuccesfulCast = String.Empty;
        public static Logging _log = E3.Log;
        private static IMQ MQ = E3.MQ;
        private static ISpawns _spawns = E3.Spawns;
        private static Dictionary<string, Int64> _DIStickCooldown = new Dictionary<string, long>();
        private static Int64 _nextBegCheck = 0;
        private static Int64 _nextBegCheckInterval = 1000;
        private static Queue<BuffQueuedItem> _queuedBuffs = new Queue<BuffQueuedItem>();
        private static SpellAliasDataFile _spellAliasesDataFile = new SpellAliasDataFile();
        public static Dictionary<string, string> SpellAliases;
        [SubSystemInit]
        public static void Init()
        {
            RegsterEvents();
            _spellAliasesDataFile.LoadData();
            SpellAliases = _spellAliasesDataFile.GetClassAliases();

        }

        private static void RegsterEvents()
        {

            EventProcessor.RegisterEvent("BuffMe", "(.+) tells you, '(?i)buff me'", (x) =>
            {
                if (x.match.Groups.Count > 1)
                {
                    if (Basics.AmIDead()) return;
                    string user = x.match.Groups[1].Value;

             
                    if (E3.GeneralSettings.BuffRequests_AllowBuffRequests || E3.Bots.IsMyBot(user))
                    {
                        Int32 totalQueuedSpells = 0;
                        if (_spawns.TryByName(user, out var spawn))
                        {
                            Casting.TrueTarget(spawn.ID);
                           
                            foreach (var spell in E3.CharacterSettings.GroupBuffs)
                            {
                                if (!String.IsNullOrWhiteSpace(spell.Ifs))
                                {
                                    if (!Casting.Ifs(spell))
                                    {
                                        continue;
                                    }
                                }
                                _queuedBuffs.Enqueue(new BuffQueuedItem() { TargetID = spawn.ID, Spell = spell});
                                totalQueuedSpells++;
                            }
                            if(totalQueuedSpells > 0)
                            {
                                MQ.Cmd($"/t {user} casting buffs on you, please wait.");
                                E3.Bots.BroadcastCommand($"/buffme {spawn.ID}");

                            }
                        }
                    }
                }
            });

            EventProcessor.RegisterEvent("BuffMyPet", "(.+) tells you, '(?i)buff my pet'", (x) =>
            {
                if (x.match.Groups.Count > 1)
                {
                    if (Basics.AmIDead()) return;
                    string user = x.match.Groups[1].Value;



                    if (E3.GeneralSettings.BuffRequests_AllowBuffRequests || E3.Bots.IsMyBot(user))
                    {
                        Int32 totalQueuedSpells = 0;
                        if (_spawns.TryByName(user, out var spawn))
                        {
                            Int32 petid = spawn.PetID;
                            Casting.TrueTarget(spawn.ID);
                            if (petid>0)
                            {
                                foreach (var spell in E3.CharacterSettings.GroupBuffs)
                                {
                                    if (!String.IsNullOrWhiteSpace(spell.Ifs))
                                    {
                                        if (!Casting.Ifs(spell))
                                        {
                                            continue;
                                        }
                                    }
                                    _queuedBuffs.Enqueue(new BuffQueuedItem() { TargetID = petid, Spell = spell });
                                    totalQueuedSpells++;
                                }
                               
                                if (totalQueuedSpells > 0)
                                {
                                    MQ.Cmd($"/t {user} casting buffs on your pet, please wait.");
                                    E3.Bots.BroadcastCommand($"/buffme {petid}");
                                }
                                    
                            }
                            else
                            {
                                MQ.Cmd($"/t {user} you have no pet.");
                            }
                           
                            
                        }
                    }
                }
            });

            EventProcessor.RegisterCommand("/buffme", (x) =>
            {
                if (x.args.Count > 0)
                {
                    if (Int32.TryParse(x.args[0], out var spawnid))
                    {
                        foreach (var spell in E3.CharacterSettings.GroupBuffs)
                        {
                            _queuedBuffs.Enqueue(new BuffQueuedItem() { TargetID=spawnid, Spell = spell });

                        }
                    }
                }
                else
                {
                    
                    foreach (var spell in E3.CharacterSettings.GroupBuffs)
                    {
                        _queuedBuffs.Enqueue(new BuffQueuedItem() { TargetID = E3.CurrentId, Spell = spell });

                    }
                    
                    E3.Bots.BroadcastCommand($"/buffme {E3.CurrentId}");
                }
            });

            EventProcessor.RegisterCommand("/buffit", (x) =>
            {
                if (x.args.Count > 0)
                {
                    if (Int32.TryParse(x.args[0], out var spawnid))
                    {
                        foreach (var spell in E3.CharacterSettings.GroupBuffs)
                        {
                            _queuedBuffs.Enqueue(new BuffQueuedItem() { TargetID = spawnid, Spell = spell });

                        }
                    }
                }
                else
                {
                    int targetid = MQ.Query<int>("${Target.ID}");
                    if(targetid>0)
                    {
                        E3.Bots.BroadcastCommand($"/buffme {targetid}");

                    }
                }
            });

            var buffBegs = new List<string> { "(.+) tells you, '(.+)'", "(.+) tells the group, '(.+)'" };
            EventProcessor.RegisterEvent("BuffBeg", buffBegs, (x) =>
            {
                if (x.match.Groups.Count > 2)
                {
                    if (Basics.AmIDead()) return;
                    string user = x.match.Groups[1].Value;

                    if (_spawns.TryByName(user, out var spawn)) //same zone?
                    {
						if (E3.GeneralSettings.BuffRequests_AllowBuffRequests || E3.Bots.IsMyBot(user))
						{
							string spell = x.match.Groups[2].Value;
							bool groupReply = false;
							if (x.match.Groups[0].Value.Contains(" tells the group,"))
							{
								groupReply = true;
							}

							if (Int32.TryParse(spell, out var temp))
							{
								//me.book returns the spell that is memed in that slot in your book
								//this isnt what we want, to just ignore the request
								return;
							}

							//check to see if its an alias.
							string realSpell = string.Empty;
							if (SpellAliases.TryGetValue(spell, out realSpell))
							{
								spell = realSpell;
							}
							bool inBook = MQ.Query<bool>($"${{Me.Book[{spell}]}}");
							bool aa = MQ.Query<bool>($"${{Me.AltAbility[{spell}].Spell}}");
							bool item = MQ.Query<bool>($"${{FindItem[={spell}]}}");

							if (inBook || aa || item)
							{
								if (groupReply)
								{
									MQ.Cmd($"/gsay {user}: putting in queue {spell}");

								}
								else
								{
									MQ.Cmd($"/t {user} I'm queueing up {spell} to use on you, please wait.");

								}
								_queuedBuffs.Enqueue(new BuffQueuedItem() { Requester = user, SpellTouse = spell });

							}
						}
					}
					
                }
            });
            var raidbuffBeg = new List<string> {"(.+) tells the raid,  '"+E3.CurrentName+@":(.+)'" };
            EventProcessor.RegisterEvent("RaidBuffBeg", raidbuffBeg, (x) =>
            {
                if (x.match.Groups.Count > 2)
                {
                    if (Basics.AmIDead()) return;
                    string user = x.match.Groups[1].Value;
                    if(_spawns.TryByName(user, out var spawn))
                    {
                        if (E3.GeneralSettings.BuffRequests_AllowBuffRequests || E3.Bots.IsMyBot(user))
                        {
                            string spell = x.match.Groups[2].Value;
                            if (Int32.TryParse(spell, out var temp))
                            {
                                //me.book returns the spell that is memed in that slot in your book
                                //this isnt what we want, to just ignore the request
                                return;
                            }

                            //check to see if its an alias.
                            string realSpell = string.Empty;
                            if (SpellAliases.TryGetValue(spell, out realSpell))
                            {
                                spell = realSpell;
                            }
                            bool inBook = MQ.Query<bool>($"${{Me.Book[{spell}]}}");
                            bool aa = MQ.Query<bool>($"${{Me.AltAbility[{spell}].Spell}}");
                            bool item = MQ.Query<bool>($"${{FindItem[={spell}]}}");

                            if (inBook || aa || item)
                            {
                                MQ.Cmd($"/rsay {user}: queueing {spell}, please wait.");
                                _queuedBuffs.Enqueue(new BuffQueuedItem() { Requester = user, SpellTouse = spell });

                            }
                        }
                    }
                    
                }
            });
            //queuecast almost works exactly the same so added it here.
            EventProcessor.RegisterCommand("/queuecast", (x) =>
            {
                if (x.args.Count > 1)
                {
                    //queuecast person "spell name" targetid
                    //queuecast me "spell name" targetid
                    //queuecast rockn "spell name" targetid
                    //queuecast all "spell name"
                    Int32 targetid = 0;
                    string user = string.Empty;
                    string spell = string.Empty;
                    user = x.args[0];
                    spell = x.args[1];
                    if (x.args.Count > 2)
                    {
                        Int32.TryParse(x.args[2], out targetid);
                    }
                    if (user.Equals("all", StringComparison.OrdinalIgnoreCase))
                    {
                        if (targetid > 0)
                        {
                            E3.Bots.BroadcastCommandToGroup($"/queuecast me \"{spell}\" {targetid}");

                            QueueCast(spell, targetid,"");
                        }
                        else
                        {
                            E3.Bots.BroadcastCommandToGroup($"/queuecast me \"{spell}\"");
                            QueueCast(spell, 0, "");

                        }
                    }
                    else if (user.Equals("me", StringComparison.OrdinalIgnoreCase))
                    {
                        if (targetid > 0)
                        {
                            QueueCast(spell, targetid, "");

                        }
                        else
                        {
                            QueueCast(spell, 0, E3.CurrentName);
                        }
                    }
                    else
                    {
                        if (targetid > 0)
                        {
                            //send this to a person!
                            E3.Bots.BroadcastCommandToPerson(user, $"/queuecast me \"{spell}\" {targetid}");

                        }
                        else
                        {
                            //send this to a person!
                            E3.Bots.BroadcastCommandToPerson(user, $"/queuecast me \"{spell}\"");
                        }
                    }
                }
            });
        }
        public static void QueueCast(string spell, Int32 targetid,string user)
        {

            //check to see if its an alias.
            string realSpell = string.Empty;
            if (SpellAliases.TryGetValue(spell, out realSpell))
            {
                spell = realSpell;
            }
           
            if(!String.IsNullOrWhiteSpace(user))
            {
                MQ.Cmd($"/t {user} I'm queuing up {spell} to use on you, please wait.");
               
            }
            _queuedBuffs.Enqueue(new BuffQueuedItem() { Requester = user, SpellTouse = spell, TargetID=targetid});
            
        }
        [ClassInvoke(Data.Class.All)]
        public static void Check_QueuedBuffs()
        {
            if (!e3util.ShouldCheck(ref _nextBegCheck, _nextBegCheckInterval)) return;

            if (_queuedBuffs.Count>0)
            {
                var askedForSpell = _queuedBuffs.Peek();
                Spawn spawn;

                if (_spawns.TryByName(askedForSpell.Requester, out spawn) || _spawns.TryByID(askedForSpell.TargetID, out spawn))
                {
                    Spell s=null;

                    //see if the spell was already supplied
                    if (askedForSpell.Spell != null) s = askedForSpell.Spell;

                    if(s==null)
                    {
                        s = new Spell(askedForSpell.SpellTouse, E3.CharacterSettings.ParsedData);
                    }

                    //not a valid spell
                    if (s.CastType==CastType.None)
                    {
                        _queuedBuffs.Dequeue();
                        return;
                    }

                    if (!String.IsNullOrWhiteSpace(s.Ifs))
                    {
                        Casting.TrueTarget(spawn.ID);
                        if (!Casting.Ifs(s))
                        {
                            _queuedBuffs.Dequeue();
                            return;
                        }
                    }
                    if (!String.IsNullOrWhiteSpace(s.CheckFor))
                    {
                        Casting.TrueTarget(spawn.ID);
                        if (MQ.Query<bool>($"${{Bool[${{Target.Buff[{s.CheckFor}]}}]}}"))
                        {
                            _queuedBuffs.Dequeue();
                            return;
                        }
                    }

                    if ((s.TargetType=="Self" ||Casting.InRange(spawn.ID, s)) && Casting.CheckReady(s) && Casting.CheckMana(s))
                    {
                        //so we can be sure our cursor was empty before we cast
                        Int32 cursorID = MQ.Query<Int32>("${Cursor.ID}");
                     
                        var result = Casting.Cast(spawn.ID, s, Heals.SomeoneNeedsHealing);
                        if (result == CastReturn.CAST_INTERRUPTFORHEAL)
                        {
                            return;
                        }
                        if (cursorID<1)
                        {
                            Casting.TrueTarget(spawn.ID);
                            cursorID = MQ.Query<Int32>("${Cursor.ID}");
                            if(cursorID>0)
                            {
                                //the spell that was requested put something on our curosr, give it to them.
                                e3util.GiveItemOnCursorToTarget();

                            }
                        }

                        _queuedBuffs.Dequeue();
                    }
                    else
                    { 
                        //give the buff at least 30 sec for us to be able to cast. 
                        if(askedForSpell.TimeStamp==0)
                        {
                            askedForSpell.TimeStamp = Core.StopWatch.ElapsedMilliseconds;
                            var result = _queuedBuffs.Dequeue();
                            _queuedBuffs.Enqueue(result);
                        }
                        if(Core.StopWatch.ElapsedMilliseconds - askedForSpell.TimeStamp > 30000 )
                        {
                            E3.Bots.Broadcast("Removing spell from queue due to it being not ready or out of range: " + s.CastName);
                            //possibly long cooldown?
                            _queuedBuffs.Dequeue();

                        }

                    }
                }
                else
                {
                    //they are not in zone, remove it
                    _queuedBuffs.Dequeue();
                }
            }
        }
    }
}
