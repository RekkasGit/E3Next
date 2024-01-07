using System;
using System.Collections.Concurrent;
using System.Speech.Synthesis;
using System.Threading;
using System.Threading.Tasks;

namespace E3NextUI.Util
{
    public class TTSProcessor
    {


        enum TTSType
        {
            Normal,
            Spell
        }

        class TTSItem
        {

            public string message;
            public TTSType type;
            public static TTSItem Aquire()
            {
                TTSItem obj;
                if (!StaticObjectPool.TryPop<TTSItem>(out obj))
                {
                    obj = new TTSItem();
                }

                return obj;
            }
            public void Dispose()
            {
                type = TTSType.Normal;
                message = String.Empty;
                StaticObjectPool.Push(this);
            }
            ~TTSItem()
            {
                //DO NOT CALL DISPOSE FROM THE FINALIZER! This should only ever be used in using statements
                //if this is called, it will cause the domain to hang in the GC when shuttind down
                //This is only here to warn you

            }

        }
        Task _processingTask = null;
        ConcurrentQueue<TTSItem> _queue = new ConcurrentQueue<TTSItem>();
        SpeechSynthesizer _synth = new SpeechSynthesizer();
        public TTSProcessor()
        {
            _synth.SetOutputToDefaultAudioDevice();
        }

        public void AddMessageNormalQueue(string message)
        {
            var item = TTSItem.Aquire();
            item.message = message;
            item.type = TTSType.Normal;
            _queue.Enqueue(item);
        }
        public void AddMessageToSpellQueue(string message)
        {
            var item = TTSItem.Aquire();
            item.message = message;
            item.type = TTSType.Spell;
            _queue.Enqueue(item);
        }

        public void Start()
        {
            _processingTask = Task.Factory.StartNew(() => { Process(); }, CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);

        }
        private void Process()
        {

            while (E3UI.ShouldProcess)
            {
                if (_queue.Count > 0)
                {
                    if (_queue.TryDequeue(out var item))
                    {
                        if (item.type == TTSType.Normal)
                        {
                            Speak(item.message);

                        }
                        else if (item.type == TTSType.Spell)
                        {
                            SpeakSpell(item.message);
                        }
                        item.Dispose();
                    }
                }
                System.Threading.Thread.Sleep(1);
            }

        }

        void SpeakSpell(string message)
        {
            if (!E3UI._genSettings.TTS_Enabled) return;
            if (message.StartsWith("You ")) return;
            if (!String.IsNullOrWhiteSpace(E3UI._genSettings.TTS_RegEx))
            {
                var match = System.Text.RegularExpressions.Regex.Match(message, E3UI._genSettings.TTS_RegEx);
                if (!match.Success)
                {
                    return;
                }
            }
            if (!String.IsNullOrWhiteSpace(E3UI._genSettings.TTS_RegExExclude))
            {
                var match = System.Text.RegularExpressions.Regex.Match(message, E3UI._genSettings.TTS_RegExExclude);
                if (match.Success)
                {
                    return;
                }
            }


            //need to see if this is a mob or user spell cast. 
            //[Sat Dec 24 06:40:45 2022] a Tae Ew proselyte begins to cast a spell. <HC Tectonic Shock>
            if (!message.EndsWith(">")) return; //need to end with the spell name.
            if (!message.Contains(" begins to cast a spell")) return;
            //check to if there are any spaces before begins to cast a spell, to see if its a mob or user.
            //named mobs are an issue here like "Lockjaw"
            Int32 indexOfBegin = message.IndexOf(" begins to cast a spell");
            string mobName = message.Substring(0, indexOfBegin);

            bool isMob = false;
            if (mobName.Contains(" "))
            {
                isMob = true;
            }

            if (!E3UI._genSettings.TTS_ChannelMobSpellsEnabled && isMob)
            {
                //we don't want a mob but it contains space, so its a mob, return
                return;
            }
            if (!E3UI._genSettings.TTS_ChannelPCSpellsEnabled && !isMob)
            {
                //we don't want a pc, but there are no spaces, so its a PC (not really, single named mobs can have it , will have to warn user)
                return;
            }

            message = message.Replace(" begins to cast a spell", " casting");
            if (isMob)
            {
                message = message.Replace(mobName, "");
            }

            if (E3UI._genSettings.TTS_BriefMode)
            {
                Int32 indexOfStart = message.IndexOf(", '");

                if (indexOfStart != -1)
                {
                    indexOfStart += 3;
                    message = message.Substring(indexOfStart, message.Length - indexOfStart);
                    if (message.EndsWith("'"))
                    {
                        message = message.Substring(0, message.Length - 1);
                    }
                }
            }

            if (E3UI._genSettings.TTS_CharacterLimit > 0)
            {
                if (message.Length > E3UI._genSettings.TTS_CharacterLimit)
                {
                    message = message.Substring(0, E3UI._genSettings.TTS_CharacterLimit);
                    message += " TLDR";
                }
            }


            _synth.Rate = E3UI._genSettings.TTS_Speed;
            _synth.Volume = E3UI._genSettings.TTS_Volume;
            _synth.Speak(message);
        }
        void Speak(string message)
        {
            if (!E3UI._genSettings.TTS_Enabled) return;
            if (message.StartsWith("You ")) return;


            if (!String.IsNullOrWhiteSpace(E3UI._genSettings.TTS_RegEx))
            {
                var match = System.Text.RegularExpressions.Regex.Match(message, E3UI._genSettings.TTS_RegEx);
                if (!match.Success)
                {
                    return;
                }
            }
            if (!String.IsNullOrWhiteSpace(E3UI._genSettings.TTS_RegExExclude))
            {
                var match = System.Text.RegularExpressions.Regex.Match(message, E3UI._genSettings.TTS_RegExExclude);
                if (match.Success)
                {
                    return;
                }
            }
            if (message.Contains(" says out of character,"))
            {
                if (!E3UI._genSettings.TTS_ChannelOOCEnabled) return;

                message = message.Replace(" says out of character,", " OOC,");
            }
            if (message.Contains(" tells the group,"))
            {
                if (!E3UI._genSettings.TTS_ChannelGroupEnabled) return;

                message = message.Replace(" tells the group,", " group,");
            }
            if (message.Contains(" tells the guild,"))
            {
                if (!E3UI._genSettings.TTS_ChannelGuildEnabled) return;

                message = message.Replace(" tells the guild,", " guild,");
            }
            if (message.Contains(" tells you,"))
            {
                if (!E3UI._genSettings.TTS_ChannelTellEnabled) return;

            }
            //
            if (message.Contains(" raid, '"))
            {
                if (!E3UI._genSettings.TTS_ChannelRaidEnabled) return;

            }
            if (message.Contains(" auctions, '"))
            {
                if (!E3UI._genSettings.TTS_ChannelAuctionEnabled) return;

            }
            if (message.Contains(" auctions, '"))
            {
                if (!E3UI._genSettings.TTS_ChannelAuctionEnabled) return;

            }
            if (message.Contains(" says, '"))
            {
                if (!E3UI._genSettings.TTS_ChannelSayEnabled) return;

            }
            if (message.Contains(" shouts, '"))
            {
                if (!E3UI._genSettings.TTS_ChannelShoutEnabled) return;

            }

            if (!String.IsNullOrWhiteSpace(E3UI._genSettings.TTS_Voice))
            {
                _synth.SelectVoice(E3UI._genSettings.TTS_Voice);
                //_synth.SelectVoice("Microsoft Eva Mobile");
            }
            else
            {
                _synth.SelectVoiceByHints(VoiceGender.Female);
            }


            if (E3UI._genSettings.TTS_BriefMode)
            {
                Int32 indexOfStart = message.IndexOf(", '");

                if (indexOfStart != -1)
                {
                    indexOfStart += 3;
                    message = message.Substring(indexOfStart, message.Length - indexOfStart);
                    if (message.EndsWith("'"))
                    {
                        message = message.Substring(0, message.Length - 1);
                    }
                }
            }

            if (E3UI._genSettings.TTS_CharacterLimit > 0)
            {
                if (message.Length > E3UI._genSettings.TTS_CharacterLimit)
                {
                    message = message.Substring(0, E3UI._genSettings.TTS_CharacterLimit);
                    message += " TLDR";
                }
            }
            _synth.Rate = E3UI._genSettings.TTS_Speed;
            _synth.Volume = E3UI._genSettings.TTS_Volume;
            _synth.Speak(message);
        }
    }
}
