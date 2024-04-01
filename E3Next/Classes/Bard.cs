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
        private static string _currentMelody = String.Empty;
        private static Int64 _nextMelodyIfCheck = 0;
        private static Int64 _nextMelodyIfRefreshTimeInterval = 1000;
        private static bool _forceOverride = false;
        private static Int64 _nextAutoSonataCheck;
        private static Data.Spell _sonataSpell = new Spell("Selo's Sonata");
        private static Data.Spell _sonataAccelerando = new Spell("Selo's Accelerando");
        private static Int64 _nextBardCast = 0;
        /// <summary>
        /// Initializes this instance.
        /// </summary>
        [ClassInvoke(Data.Class.Bard)]
        public static void Init()
        {
            if (_isInit) return;
            PlayMelody();
            _isInit = true;
        }
        /// <summary>
        /// Checks and re-applies Sonata if necessary.
        /// </summary>
        [ClassInvoke(Data.Class.Bard)]
        public static void AutoSonata()
        {
            if (E3.IsInvis) return;
            if (!e3util.ShouldCheck(ref _nextAutoSonataCheck, 1000)) return;
			if (!MQ.Query<bool>("${Me.Standing}"))
			{
				//we are sitting, don't do anything
				return;
			}
			if (E3.CharacterSettings.Bard_AutoSonata)
            {
                Int32 spellIDToLookup = SelosBuffID;

                if(e3util.IsEQLive())
                {
                    spellIDToLookup = _sonataAccelerando.SpellID;
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
        public static void PlayMelody()
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
        //[ClassInvoke(Data.Class.Bard)]        
        /// <summary>
        /// Checks the bard songs.
        /// </summary>
        public static void check_BardSongs()
        {

			if (!_playingMelody && !Assist.IsAssisting)
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

			//necessary in case to stop the situation of the song not fully reigstering on the server as being complete
			//even if the client thinks it does. basically the debuff/buff won't appear before even tho the client says we have completed the song
			Int64 curTimeStamp = Core.StopWatch.ElapsedMilliseconds;
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
			songToPlay = _songs.Dequeue();

			//we have gone through all the songs and not found a valid one to use, kick out
			if (trycounter++ > _songs.Count)
			{
				_songs.Enqueue(songToPlay);//place song back
				return;
			}
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
                if (MQ.Query<Int32>(BuffSecondsLeftQuery) > 18 || MQ.Query<Int32>(SongSecondsLeftQuery) > 18)
                {
                    return;
                }
            }
            if (Casting.CheckReady(songToPlay))
            {
               
                MQ.Write($"\atTwist \ag{songToPlay.SpellName}");
				_nextBardCast = Core.StopWatch.ElapsedMilliseconds + (int)songToPlay.MyCastTime + 300;
				Casting.Sing(0, songToPlay);
			}
            else
            {
                MQ.Write($"\arTwists-Skip \ag{songToPlay.SpellName}");
            }
        }

        /// <summary>
        /// Starts the melody.
        /// </summary>
        /// <param name="melodyName">Name of the melody.</param>
        /// <param name="force">if set to <c>true</c> [force].</param>
        public static void StartMelody(string melodyName, bool force=false)
        {
             _songs.Clear();
            //lets find the melody in the character ini.
            CharacterSettings.LoadKeyData($"{melodyName} Melody", "Song", E3.CharacterSettings.ParsedData, _songs);
            if(_songs.Count>0)
            {
                MQ.Write($"\aoStart Melody:\ag{melodyName}");
                MQ.Cmd("/stopsong");
                _nextBardCast = Core.StopWatch.ElapsedMilliseconds;
				_forceOverride = force;
                _playingMelody = true;
                _currentMelody = melodyName;
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

    }
}
