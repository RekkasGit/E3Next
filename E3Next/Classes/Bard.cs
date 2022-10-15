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

namespace E3Core.Classes
{
    public static class Bard
    {

        public static Logging _log = E3._log;
        private static IMQ MQ = E3.MQ;
        private static ISpawns _spawns = E3._spawns;
        private static Queue<Data.Spell> _songs = new Queue<Spell>();
        private static bool _isInit = false;
        private static bool _playingMelody = true;
        private static string _currentMelody = String.Empty;
        private static Int64 _nextMelodyIfCheck = 0;
        private static Int64 _nextMelodyIfRefreshTimeInterval = 1000;
        private static bool _forceOverride = false;

        [ClassInvoke(Data.Class.Bard)]
        public static void Init()
        {
            if (_isInit) return;
            RegisterEvents();
            _isInit = true;

        }

        private static void RegisterEvents()
        {

            EventProcessor.RegisterCommand("/playmelody", (x) =>
            {
                if(x.args.Count>0)
                {
                    if (x.args[0].Equals("stop", StringComparison.OrdinalIgnoreCase))
                    {
                        _playingMelody = false;
                        MQ.Cmd("/interrupt");
                    }
                    else
                    {
                        if(x.args.Count>1 && x.args[1].Equals("force", StringComparison.OrdinalIgnoreCase))
                        {
                            StartMelody(x.args[0],true);
                        }
                        else
                        {
                            StartMelody(x.args[0]);

                        }
                    }
                }
            });
        }
        [ClassInvoke(Data.Class.Bard)]
        public static void checkMelodyIf()
        {
            if (!e3util.ShouldCheck(ref _nextMelodyIfCheck, _nextMelodyIfRefreshTimeInterval)) return;

            if (!_isInit) return;
            if (!_playingMelody || _forceOverride) return;

            //go through the ifs and see if we should change the melodies
            foreach(var melodyCheck in E3._characterSettings.Bard_MelodyIfs)
            {
                bool melodyTrue = MQ.Query<bool>($"${{If[{melodyCheck.MelodyIf},TRUE,FALSE]}}");
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

        [ClassInvoke(Data.Class.Bard)]
        public static void check_BardSongs()
        {
            if (!_playingMelody || _songs.Count==0) return;

            bool stunned = MQ.Query<bool>("${Me.Stunned}");
            bool windowOpen = MQ.Query<bool>("${Window[BigBankWnd].Open}") || MQ.Query<bool>("${Window[MerchantWnd].Open}") || MQ.Query<bool>("${Window[TradeWnd].Open}") || MQ.Query<bool>("${Window[GuildBankWnd].Open}");
            if (E3._isInvis || windowOpen)
            {
                return;
            }
            if(MQ.Query<bool>("${Window[CastingWindow].Open}") && (MQ.Query<Int32>("${Cast.Timing}")>500))
            {
                return;
            }
            //lets play a song!
            Data.Spell songToPlay= _songs.Dequeue();
            _songs.Enqueue(songToPlay);
            if(Casting.CheckReady(songToPlay))
            {
                MQ.Write($"\atTwist \ag{songToPlay.SpellName}");
                Casting.Sing(0, songToPlay);
            }
            else
            {
                MQ.Write($"\arTwists-Skip \ag{songToPlay.SpellName}");
            }
        }

        public static void StartMelody(string melodyName, bool force=false)
        {
             _songs.Clear();
            //lets find the melody in the character ini.
            CharacterSettings.LoadKeyData($"{melodyName} Melody", "Song", CharacterSettings._parsedData, _songs);
            if(_songs.Count>0)
            {
                MQ.Write($"\aoStart Melody:\ag{melodyName}");

                _forceOverride = force;
                _playingMelody = true;
                _currentMelody = melodyName;
            }
        }

    }
}
