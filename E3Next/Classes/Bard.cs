using E3Core.Processors;
using E3Core.Settings;
using System;
using E3Core.Classes;
using E3Core.Data;
using E3Core.Utility;
using MonoCore;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Windows.Forms;
using System.Security.Cryptography;

namespace E3Core.Classes
{
    /// <summary>
    /// Properties and methods specific to the bard class
    /// </summary>
    public static class Bard
    {
        private const int SelosBuffID = 12712;
        private static Logging _log = E3.Log;
        private static IMQ MQ = E3.MQ;
        private static ISpawns _spawns = E3.Spawns;
        private static Queue<Data.Spell> _songs = new Queue<Spell>();
        private static bool _isInit = false;
        private static bool _playingMelody = true;
		[ExposedData("Bard", "CurrentMelody")]
		public static string _currentMelody = String.Empty;
        private static Int64 _nextMelodyIfCheck = 0;
        private static Int64 _nextMelodyIfRefreshTimeInterval = 1000;
        private static bool _forceOverride = false;
        private static Int64 _nextAutoSonataCheck;
        private static Data.Spell _sonataSpell = new Spell("Selo's Sonata");
        private static Data.Spell _sonataAccelerando = new Spell("Selo's Accelerando");
        private static Int64 _nextBardCast = 0;
        private static bool _autoMezEnabled = false;
        private static HashSet<Int64> _autoMezFullMobList = new HashSet<long>();
		private static HashSet<Int64> _mobsToAutoMez = new HashSet<Int64>();
		public static Dictionary<Int32, SpellTimer> _autoMezTimers = new Dictionary<Int32, SpellTimer>();
        public static Data.Spell _currentSongPlaying = null;

		public static void ResetNextBardSong()
        {
            _nextBardCast = 0;
        }
		/// <summary>
		/// Initializes this instance.
		/// </summary>
		[ClassInvoke(Data.Class.Bard)]
        public static void Init()
        {
            if (_isInit) return;
            RegisterCommands();
            _isInit = true;
        }
        /// <summary>
        /// Checks and re-applies Sonata if necessary.
        /// </summary>
        [ClassInvoke(Data.Class.Bard)]
        public static void AutoSonata()
        {
            if (E3.IsInvis) return;
			if (Heals.IgnoreHealTargets.Count > 0) return;
			if (!e3util.ShouldCheck(ref _nextAutoSonataCheck, 1000)) return;
			//if (!MQ.Query<bool>("${Me.Standing}"))
			//{
			//	//we are sitting, don't do anything
			//	return;
			//}
			if (E3.CharacterSettings.Bard_AutoSonata)
            {
                Int32 spellIDToLookup = SelosBuffID;

                if(e3util.IsEQLive())
                {
                    spellIDToLookup = 50190;
                }

                bool needToCast = false;
                //lets get group members
                List<string> memberNames = E3.Bots.BotsConnected();
                foreach (int memberid in Basics.GroupMembers)
                {
                    Spawn s;
                    if (_spawns.TryByID(memberid, out s))
                    {
                        if (memberNames.Contains(s.CleanName))
                        {
                            List<Int32> buffList = E3.Bots.BuffList(s.CleanName);
                            if (!buffList.Contains(spellIDToLookup))
                            {
                                needToCast = true;
                                break;
                            }
                        }
                    }
                }
                Int32 totalSecondsLeft = 0;
                if(e3util.IsEQLive())
                {
					totalSecondsLeft = MQ.Query<Int32>("${Me.Buff[Selo's Accelerando].Duration.TotalSeconds}");
                    if (totalSecondsLeft < 1)
                    {
						totalSecondsLeft = MQ.Query<Int32>("${Me.Buff[Selo's Accelerato].Duration.TotalSeconds}");
					}
				}
				else
                {
					totalSecondsLeft = MQ.Query<Int32>("${Me.Buff[Selo's Sonata].Duration.TotalSeconds}");
				}

				if (totalSecondsLeft < 10)
                {
                    needToCast = true;
                }

                if (needToCast)
                {
                    if (Casting.CheckReady(_sonataSpell))
                    {
                        bool haveBardSong =MQ.Query<bool>("${Me.Buff[Selo's Accelerating Chorus].ID}");
                        if (!haveBardSong)
                        {

                            Casting.Cast(E3.CurrentId, _sonataSpell);
                        }
                    }
                }
            }
        }
        /// <summary>
        /// /playmelody melodyName
        /// </summary>
        public static void RegisterCommands()
        {
            EventProcessor.RegisterCommand("/playmelody", (x) =>
            {
                if (x.args.Count > 0)
                {
                    if (x.args[0].Equals("stop", StringComparison.OrdinalIgnoreCase))
                    {
                        _playingMelody = false;
                        Casting.Interrupt();
                    }
                    else
                    {
                        if (x.args.Count > 1 && x.args[1].Equals("force", StringComparison.OrdinalIgnoreCase))
                        {
                            StartMelody(x.args[0], true);
                        }
                        else
                        {
                            StartMelody(x.args[0]);

                        }
                    }
                }
            });
			EventProcessor.RegisterCommand("/e3bard-automez", (x) =>
            {
                if (x.args.Count > 0)
                {
                    if (x.args[0].Equals("off", StringComparison.OrdinalIgnoreCase))
                    {
                        E3.Bots.Broadcast("Turning off Bard Auto Mez");
                        Casting.Interrupt();
                        _autoMezEnabled = false;
                        _autoMezTimers.Clear();
                        

                    }
					else if (x.args[0].Equals("on", StringComparison.OrdinalIgnoreCase))
					{
						E3.Bots.Broadcast("Turning on Bard Auto Mez");

						_autoMezEnabled = true;
						_autoMezTimers.Clear();
					}
                }
				else
				{
					if(_autoMezEnabled)
					{
						E3.Bots.Broadcast("Turning off Bard Auto Mez");
						Casting.Interrupt();
						_autoMezEnabled = false;
						_autoMezTimers.Clear();
					}
					else
					{
						E3.Bots.Broadcast("Turning on Bard Auto Mez");

						_autoMezEnabled = true;
						_autoMezTimers.Clear();

					}
				}
			});
		}

        /// <summary>
        /// Checks the melody ifs.
        /// </summary>
        [ClassInvoke(Data.Class.Bard)]
        public static void checkMelodyIf()
        {
            if (!e3util.ShouldCheck(ref _nextMelodyIfCheck, _nextMelodyIfRefreshTimeInterval)) return;

            if (!_isInit) return;
            if (!_playingMelody || _forceOverride) return;

            //go through the ifs and see if we should change the melodies
            foreach(var melodyCheck in E3.CharacterSettings.Bard_MelodyIfs)
            {
                bool melodyTrue = Casting.Ifs(melodyCheck.MelodyIf);
                if(melodyTrue)
                {
                    if(!_currentMelody.Equals(melodyCheck.MelodyName, StringComparison.OrdinalIgnoreCase))
                    {
                        StartMelody(melodyCheck.MelodyName);
                        
                    }
                    return;
                }
            }
        }
		public static Dictionary<Int32, Int64> _mobsAndTimeStampForMez = new Dictionary<int, long>();
		public static void Check_AutoMez()
		{
			if (!_autoMezEnabled) return;

			if (Casting.IsCasting()) return;
			if (!Basics.InCombat())
			{
				if(_autoMezTimers.Count>0)
				{
					_autoMezTimers.Clear();

				}
				return;
			}
			if (E3.CharacterSettings.Bard_AutoMezSong.Count == 0) return;
			Int32 targetId = MQ.Query<Int32>("${Target.ID}");

			using (_log.Trace())
			{
				_autoMezFullMobList.Clear();

				foreach (var s in _spawns.Get().OrderBy(x => x.Distance3D))
				{
					_autoMezFullMobList.Add(s.ID);
					if (s.ID == Assist.AssistTargetID) continue;
					if (_mobsToAutoMez.Contains(s.ID)) continue;
					//find all mobs that are close
					if (s.PctHps < 1) continue;
					if (s.TypeDesc != "NPC") continue;
					if (!s.Targetable) continue;
					if (!s.Aggressive) continue;
					if (s.CleanName.EndsWith("s pet")) continue;
					if (!MQ.Query<bool>($"${{Spawn[npc id {s.ID}].LineOfSight}}")) continue;
					if (s.Distance3D > 60) break;//mob is too far away, and since it is ordered, kick out.
											   //its valid to attack!
					_mobsToAutoMez.Add(s.ID);
				}
				
                List<Int64> mobIdsToRemove = new List<Int64>();
                foreach(var mobid in _mobsToAutoMez)
                {
                    if(!_autoMezFullMobList.Contains(mobid))
                    {
                        //they are no longer a valid mobid, remove from mobs to mez
                        mobIdsToRemove.Add(mobid);
					}
                }
                foreach (var mobid in mobIdsToRemove)
                {
                    _mobsToAutoMez.Remove(mobid);
                }
                if (_mobsToAutoMez.Count == 0)
                {
                    //_autoMezEnabled = false;
                    //E3.Bots.Broadcast("No more mobs to mez, turning off auto mez.");
					_autoMezTimers.Clear();
					return;
                }
				_mobsToAutoMez.Remove(Assist.AssistTargetID);
				if (_mobsToAutoMez.Count == 0)
				{
					//E3.Bots.Broadcast("No more mobs to mez, turning off auto mez.");
					//_autoMezEnabled = false;
                    _autoMezTimers.Clear();
					return;
				}

                bool wasAttacking = MQ.Query<bool>("${Me.Combat}");
				try
				{
                   

					foreach (var spell in E3.CharacterSettings.Bard_AutoMezSong)
					{
						if (!spell.Enabled) continue;
						//check if the if condition works
						if (!String.IsNullOrWhiteSpace(spell.Ifs))
						{
							if (!Casting.Ifs(spell))
							{
								continue;
							}
						}
						if (Casting.CheckMana(spell))
						{

							//find the mob that has either 1) no mez timer, or 2) the lowest value one
							_mobsAndTimeStampForMez.Clear();
							foreach (Int32 mobid in _mobsToAutoMez.ToList())
							{
								SpellTimer s;
								//do we need to cast the song?
								if (_autoMezTimers.TryGetValue(mobid, out s))
								{
									Int64 timestamp;
									if (s.Timestamps.TryGetValue(spell.SpellID, out timestamp))
									{
										Int64 timeAndMinDuration = (Core.StopWatch.ElapsedMilliseconds + (spell.MinDurationBeforeRecast));
										if (timeAndMinDuration < timestamp)
										{
											//debuff/dot is still on the mob, kick off
											//MQ.Write($"Debuff is still on the mob");
											continue;
										}
										else
										{
											_mobsAndTimeStampForMez.Add(mobid, timeAndMinDuration);
											continue;
											//MQ.Write($"Debuff timer is up re-issuing cast. Time:{Core.StopWatch.ElapsedMilliseconds} stamp:{timestamp} minduration:{spell.MinDurationBeforeRecast}");
										}
									}
						
								}
								_mobsAndTimeStampForMez.Add(mobid, 0);

							}

							//get the mobid with the lease amount of timestamp
							if(_mobsAndTimeStampForMez.Count>0)
							{
								Int32 mobIDToMez = 0;
								Int64 leastTime = Int64.MaxValue;
								foreach (var pair in _mobsAndTimeStampForMez)
								{
									if (pair.Value < leastTime)
									{
										mobIDToMez = pair.Key;
										leastTime = pair.Value;
									}

								}
								if (_spawns.TryByID(mobIDToMez, out var spawn))
								{
									//lets place the 1st offensive spell on each mob, then the next, then the next
									//lets not hit what we are trying to mez
									if (wasAttacking)
									{
										MQ.Cmd("/attack off");

									}
									Casting.TrueTarget(mobIDToMez);
									if (Casting.CheckReady(spell))
									{
										E3.Bots.Broadcast($"Trying to Mez ==>[{spawn.CleanName}]");
										Casting.Sing(mobIDToMez, spell);
									}
										//MQ.Write($"Setting Debuff timer for {spell.DurationTotalSeconds * 1000} ms");
									//duration is in ticks
									Int64 spellDuration = E3.CharacterSettings.Bard_AutoMezSongDuration * 1000;
									DebuffDot.UpdateDotDebuffTimers(mobIDToMez, spell, spellDuration, _autoMezTimers);
								}
								
								
							
							}
							
							return;
						}
					}
				}
				finally
				{
					e3util.PutOriginalTargetBackIfNeeded(targetId);
                    if(wasAttacking)
                    {
                        MQ.Cmd("/attack on");
                    }
				}

			}
		}
		/// <summary>
		/// Checks the bard songs.
		/// </summary>
		public static void check_BardSongs()
        {

			if (!_playingMelody)
            {
                return;
            }

            if (_songs.Count == 0) return;
            if (E3.IsInvis || e3util.IsActionBlockingWindowOpen())
            {
                return;
            }
            if (Casting.IsCasting())
            {
                return;
            }
		
			if (_songs.Count == 1 && MQ.Query<bool>("${Me.Casting}")) return;

			if (E3.CharacterSettings.Misc_DelayAfterCastWindowDropsForSpellCompletion > 0)
			{
				MQ.Delay(E3.CharacterSettings.Misc_DelayAfterCastWindowDropsForSpellCompletion);
			}
			
			//necessary in case to stop the situation of the song not fully reigstering on the server as being complete
			//even if the client thinks it does. basically the debuff/buff won't appear before even tho the client says we have completed the song
			Int64 curTimeStamp = Core.StopWatch.ElapsedMilliseconds;
			//MQ.Write($"current:{curTimeStamp} next:{_nextBardCast}");
			if (curTimeStamp < _nextBardCast)
			{
				return;
			}


			if (MQ.Query<bool>("${Window[SpellBookWnd].Open}"))
            {

                return;
            }
            //can['t do songs if your stunned
            if (MQ.Query<bool>("${Me.Stunned}"))
            {
                return;
            }
            if (!MQ.Query<bool>("${Me.Standing}"))
            {
                //we are sitting, don't do anything
                return;
            }
			
            //lets play a song!
            //get a song from the queue.
            Data.Spell songToPlay = null;
			//a counter to determine if we have looped through all the songs before finding a good one
			Int32 trycounter = 0;
    		pickASong:

            //this is to deal with nowcasts early termination of the last song, replay last song that got interrupted
            if(_nextBardCast==0 && _currentSongPlaying!=null)
            {
              
				Casting.Sing(0, _currentSongPlaying);
				SetNextBardCast();
				return;
			}
            else
            {
				songToPlay = _songs.Dequeue();
			}

			//we have gone through all the songs and not found a valid one to use, kick out
			if (trycounter > _songs.Count)
			{
				_songs.Enqueue(songToPlay);//place song back
				return;
			}
			trycounter++;
			if (songToPlay.CheckForCollection.Count > 0)
			{
				foreach (var spellName in songToPlay.CheckForCollection.Keys)
				{
                    bool haveBuff = (MQ.Query<bool>($"${{Bool[${{Me.Buff[{spellName}]}}]}}") || MQ.Query<bool>($"${{Bool[${{Me.Song[{spellName}]}}]}}"));
                    if (haveBuff) 
                    {
						_songs.Enqueue(songToPlay);// place song back
						goto pickASong;
					}
      		    }
			}
            if(!Casting.Ifs(songToPlay))
			{
				_songs.Enqueue(songToPlay);// place song back
				goto pickASong;
			}
            //found a valid song, place it back into the queue so we don't lose it. 
			_songs.Enqueue(songToPlay);
			//if this base song duration > 18 seconds check to see if we have it as a buff, otherwise recast. 
			if (songToPlay.DurationTotalSeconds>18)
            {
                string BuffSecondsLeftQuery = "${Me.Buff[" + songToPlay.SpellName + "].Duration.TotalSeconds}";
                string SongSecondsLeftQuery = "${Me.Song[" + songToPlay.SpellName + "].Duration.TotalSeconds}";
                if (MQ.Query<Int32>(BuffSecondsLeftQuery) > songToPlay.SongRefreshTime || MQ.Query<Int32>(SongSecondsLeftQuery) > songToPlay.SongRefreshTime)
                {
                    return;
                }
            }
            if (Casting.CheckMana(songToPlay) && Casting.CheckReady(songToPlay))
            {
               
                MQ.Write($"\atTwist \ag{songToPlay.SpellName}");
               
                _currentSongPlaying = songToPlay;
                Casting.Sing(0, songToPlay);
				SetNextBardCast();
			}
            else
            {
                MQ.Write($"\arTwists-Skip \ag{songToPlay.SpellName}");
            }
        }
		public static void SetNextBardCast()
		{
			Int32 castTimeLeft = (int)MQ.Query<int>("${Me.CastTimeLeft}");
			Int32 bardLatency = BardLatency();
			_nextBardCast = Core.StopWatch.ElapsedMilliseconds + castTimeLeft + bardLatency;
			//MQ.Write($"CastTime:{castTimeLeft} latency: {bardLatency} nextbardcast:{_nextBardCast}");
		}
        /// <summary>
        /// Starts the melody.
        /// </summary>
        /// <param name="melodyName">Name of the melody.</param>
        /// <param name="force">if set to <c>true</c> [force].</param>
        public static void StartMelody(string melodyName, bool force=false)
        {
             _songs.Clear();
			MQ.Cmd("/stopsong");
			//lets find the melody in the character ini.
			CharacterSettings.LoadKeyData($"{melodyName} Melody", "Song", E3.CharacterSettings.ParsedData, _songs);
            if(_songs.Count>0)
            {
                MQ.Write($"\aoStart Melody:\ag{melodyName}");
             
                _nextBardCast = Core.StopWatch.ElapsedMilliseconds;
				_forceOverride = force;
                _playingMelody = true;
                _currentMelody = melodyName;
            }
            else
            {
                //its an empty list

            }
        }
        public static void RestartMelody()
        {
            if(_playingMelody && !String.IsNullOrWhiteSpace(_currentMelody))
            {
				_songs.Clear();
				//lets find the melody in the character ini.
				CharacterSettings.LoadKeyData($"{_currentMelody} Melody", "Song", E3.CharacterSettings.ParsedData, _songs);
				if (_songs.Count > 0)
				{
					MQ.Write($"\aoStart Melody:\ag{_currentMelody}");
					MQ.Cmd("/stopsong");
					_nextBardCast = Core.StopWatch.ElapsedMilliseconds;

				}
			}
			
		}
		//necessary so that buffs land even when the song is completed.
		public static Int32 BardLatency()
		{
			Int32 realLatency = e3util.Latency();
			//i have tested on 75, i cannot be sure for lower values.

			if(realLatency==0)
			{
				realLatency = 300;
				return realLatency;
			}
			else if (realLatency < 75)
			{
				realLatency = 75;
			}
			return (int)(realLatency * 1.7);
		}

    }
}
