using E3Core.Classes;
using E3Core.Server;
using E3Core.Settings;
using E3Core.Utility;
using MonoCore;
using NetMQ;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace E3Core.Processors
{
    /// <summary>
    /// The main file from which all other processing is called.
    /// </summary>
    public static class E3
    {
        /// <summary>
        /// The main processing loop.
        /// </summary>
        public static void Process()
        {

            if (!ShouldRun())
            {
                return;
            }

            //Init is here to make sure we only Init while InGame, as some queries will fail if not in game
            if (!IsInit) { Init(); }
            ActionTaken = false;
            //update all states, important.
            StateUpdates();

            if (CurrentHps < 98)
            {
                Heals.Check_LifeSupport();
            }

            //kickout after updates if paused
            if (IsPaused()) return;

            RefreshCaches();

            //nowcast before all.
            EventProcessor.ProcessEventsInQueues("/nowcast");
            //use burns if able, this is high as some heals need burns as well
            Burns.UseBurns();
            //do the basics first
            //first and formost, do healing checks
            if ((CurrentClass & Data.Class.Priest) == CurrentClass)
            {
                ActionTaken = false;
                Heals.Check_Heals();
                if (ActionTaken) return; //we did a heal, kick out as we may need to do another heal.
            }

            using (Log.Trace("Assist/WaitForRez"))
            {
                Rez.Process();
                if (Rez.IsWaiting()) return;

                Assist.Process();
            }



            if (!ActionTaken)
            {
                using (Log.Trace("AdvMethodCalls"))
                {
                    //rembmer check_heals is auto inserted, should probably just pull out here
                    List<string> _methodsToInvokeAsStrings;
                    if (AdvancedSettings._classMethodsAsStrings.TryGetValue(CurrentShortClassString, out _methodsToInvokeAsStrings))
                    {
                        foreach (var methodName in _methodsToInvokeAsStrings)
                        {
                            //if an action was taken, start over
                            if (ActionTaken)
                            {
                                break;
                            }
                            Action methodToInvoke;
                            if (AdvancedSettings._methodLookup.TryGetValue(methodName, out methodToInvoke))
                            {
                                methodToInvoke.Invoke();

                            }
                            //check backoff
                            //check nowcast
                            EventProcessor.ProcessEventsInQueues("/nowcast");
                            EventProcessor.ProcessEventsInQueues("/backoff");
                        }
                    }
                }

            }
            //process any requests commands from the UI.
            PubClient.ProcessRequests();

            //bard song player
            if (E3.CurrentClass == Data.Class.Bard)
            {
                Bard.check_BardSongs();
            }
            //class attribute method calls, call them all!
            using (Log.Trace("ClassMethodCalls"))
            {
                //lets do our class methods, this is last because of bards
                foreach (var kvp in AdvancedSettings._classMethodLookup)
                {
                    kvp.Value.Invoke();
                }
            }

            using (Log.Trace("LootProcessing"))
            {
                Loot.Process();
            }

            //were modifications made to the settings files?
            CheckModifiedSettings();
        }
        private static void CheckModifiedSettings()
        {
            if (!e3util.ShouldCheck(ref _nextReloadSettingsCheck, _nextReloadSettingsInterval)) return;

            if (CharacterSettings.ShouldReload())
            {
                E3.Bots.Broadcast("\aoAuto-Reloading Character settings file...");
                CharacterSettings = new CharacterSettings();
                Loot.Reset();
                E3.Bots.Broadcast("\aoComplete!");
               
            }
            if (GeneralSettings.ShouldReload())
            {
                E3.Bots.Broadcast("\aoAuto-Reloading General settings file...");
                GeneralSettings = new GeneralSettings();
                Loot.Reset();
                E3.Bots.Broadcast("\aoComplete!");
            }
        }
        private static bool IsPaused()
        {
            EventProcessor.ProcessEventsInQueues("/e3p");

            if (Basics.IsPaused)
            {
                return true;
            }
            return false;
        }
        /// <summary>
        /// This is used during thigns like casting, while we are delaying a ton
        /// so that we can keep things 'up to date' such as hp, invs status, pet name, etc. 
        /// 
        /// </summary>
        public static void StateUpdates()
        {
            IsInvis = Mq.Query<bool>("${Me.Invis}");
            CurrentHps = Mq.Query<int>("${Me.PctHPs}");
            CurrentId = Mq.Query<int>("${Me.ID}");
            ZoneID = Mq.Query<int>("${Zone.ID}");

            if (Mq.Query<bool>("${MoveUtils.GM}"))
            {
                Mq.Cmd("/squelch /stick imsafe");
                Bots.Broadcast("GM Safe kicked in, issued /stick imsafe.  you may need to reissue /followme or /assiston");
            }

            HitPointsCurrent = Mq.Query<int>("${Me.CurrentHPs}");
            PubServer.AddTopicMessage("${Me.CurrentHPs}", HitPointsCurrent.ToString("N0"));
            MagicPointsCurrent = Mq.Query<int>("${Me.CurrentMana}");
            PubServer.AddTopicMessage("${Me.CurrentMana}", MagicPointsCurrent.ToString("N0"));
            StamPointsCurrent = Mq.Query<int>("${Me.CurrentEndurance}");
            PubServer.AddTopicMessage("${Me.CurrentEndurance}", StamPointsCurrent.ToString("N0"));
            CurrentInCombat = Basics.InCombat();
            PubServer.AddTopicMessage("${InCombat}", CurrentInCombat.ToString());
            string nameOfPet = Mq.Query<string>("${Me.Pet.CleanName}");
            if (nameOfPet != "NULL")
            {
                //set the pet name
                CurrentPetName = nameOfPet;
                PubServer.AddTopicMessage("${Me.Pet.CleanName}", CurrentPetName);
            }
            //process any tlo request from the UI, or anything really.
            RouterServer.ProcessRequests();
            //process any commands we need to process from the UI
            PubClient.ProcessRequests();
        }
        private static void RefreshCaches()
        {
            Casting.RefreshGemCache();
            Basics.RefreshGroupMembers();
        }
        private static void Init()
        {

            if (!IsInit)
            {


                Mq.ClearCommands();
                AsyncIO.ForceDotNet.Force();

                Logging._traceLogLevel = Logging.LogLevels.None; //log level we are currently at
                Logging._minLogLevelTolog = Logging.LogLevels.Error; //log levels have integers assoicatd to them. you can set this to Error to only log errors. 
                Logging._defaultLogLevel = Logging.LogLevels.Debug; //the default if a level is not passed into the _log.write statement. useful to hide/show things.
                MainProcessor._applicationName = "E3"; //application name, used in some outputs
                MainProcessor._processDelay = ProcessDelay; //how much time we will wait until we start our next processing once we are done with a loop.
                                                            //how long you allow script to process before auto yield is engaged. generally should't need to mess with this, but an example.
                MonoCore.MQ._maxMillisecondsToWork = 40;
                //max event count for each registered event before spilling over.
                EventProcessor._eventLimiterPerRegisteredEvent = 20;
                CurrentName = Mq.Query<string>("${Me.CleanName}");

                CurrentId = Mq.Query<int>("${Me.ID}");
                //do first to get class information
                Bots = new Bots();

                CurrentName = Mq.Query<string>("${Me.CleanName}");
                ServerName = e3util.FormatServerName(Mq.Query<string>("${MacroQuest.Server}"));
                //deal with the Shadow Knight class issue.
                string classValue = Mq.Query<string>("${Me.Class}");
                if (classValue == "Shadow Knight")
                {
                    classValue = "Shadowknight";
                }
                Enum.TryParse(classValue, out CurrentClass);
                CurrentLongClassString = CurrentClass.ToString();
                CurrentShortClassString = Data.Classes._classLongToShort[CurrentLongClassString];

                //Init the settings
                CharacterSettings = new Settings.CharacterSettings();
                GeneralSettings = new Settings.GeneralSettings();
                AdvancedSettings = new Settings.AdvancedSettings();

                //setup is done after the settings are setup.
                //as there is an order dependecy
                Setup.Init();

                IsInit = true;
                MonoCore.Spawns._refreshTimePeriodInMS = 3000;

            }


        }

        private static bool ShouldRun()
        {

            if (IsBadState)
            {
                return false;
            }

            return true;
        }
        public static void Shutdown()
        {
            //Mq.Write($"Shutting down {MainProcessor._applicationName}....Reload to start gain");
            IsBadState = true;

        }
        public static bool ActionTaken = false;
        public static bool Following = false;
        public static long StartTimeStamp;
        public static bool IsInit = false;
        public static bool IsBadState = false;
        public static IMQ Mq = Core.mqInstance;
        public static Logging Log = Core._log;
        public static Settings.CharacterSettings CharacterSettings = null;
        public static Settings.GeneralSettings GeneralSettings = null;
        public static Settings.AdvancedSettings AdvancedSettings = null;
        public static IBots Bots = null;
        public static string CurrentName;
        public static Data.Class CurrentClass;
        public static string ServerName;
        public static string CurrentPetName = String.Empty;
        public static bool CurrentInCombat = false;
        public static int CurrentId;

        public static string CurrentLongClassString;
        public static string CurrentShortClassString;
        public static int CurrentHps;
        public static int HitPointsCurrent;
        public static int MagicPointsCurrent;
        public static int StamPointsCurrent;
        public static int ZoneID;
        public static int ProcessDelay = 50;
        public static ISpawns Spawns = Core.spawnInstance;
        public static bool IsInvis;
        private static Int64 _nextReloadSettingsCheck = 0;
        private static Int64 _nextReloadSettingsInterval = 2000;
    }
}
