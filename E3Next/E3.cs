using E3Core.Classes;
using E3Core.Server;
using E3Core.Settings;
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
            ActionTaken = false;
            IsInvis = Mq.Query<bool>("${Me.Invis}");
            RefreshCaches();
            //_startTimeStamp = Core._stopWatch.ElapsedMilliseconds;
            //using (_log.Trace())
            {
                if (!IsInit) { Init(); }
                HeartBeatPump();

                EventProcessor.ProcessEventsInQueues("/e3p");
                if (Basics._isPaused)
                {   
                    return;
                }


                //Init is here to make sure we only Init while InGame, as some queries will fail if not in game

                //nowcast before all.
                EventProcessor.ProcessEventsInQueues("/nowcast");
                //use burns if able
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
                    Assist.Process();
                    WaitForRez.Process();
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
                //now do the dynamic methods from Advanced ini. 

                if (E3.CurrentClass == Data.Class.Bard)
                {
                    Bard.check_BardSongs();
                }
                using (Log.Trace("ClassMethodCalls"))
                {
                    //lets do our class methods, this is last because of bards
                    foreach (var kvp in AdvancedSettings._classMethodLookup)
                    {
                        kvp.Value.Invoke();
                    }
                }
            }
            using (Log.Trace("LootProcessing"))
            {
                Loot.Process();
            }

            //process any tlo request from the UI, or anything really.
            RouterServer.ProcessRequests();
            //process any commands we need to process from the UI
            PubClient.ProcessRequests();
        }
        /// <summary>
        /// This is used during thigns like casting, while we are delaying a ton
        /// so that we can keep things 'up to date' such as hp, invs status, pet name, etc. 
        /// 
        /// </summary>
        public static void HeartBeatPump()
        {
            IsInvis = Mq.Query<bool>("${Me.Invis}");
            CurrentHps = Mq.Query<int>("${Me.PctHPs}");
            HitPointsCurrent = Mq.Query<int>("${Me.CurrentHPs}");
          
            if (HitPointsCurrent != HitPointsPrevious)
            {
                PubServer.AddTopicMessage("${Me.CurrentHPs}", HitPointsCurrent.ToString("N0"));
                HitPointsPrevious = HitPointsCurrent;
            }
            MagicPointsCurrent = Mq.Query<int>("${Me.CurrentMana}");
            if (MagicPointsCurrent != MagicPointsPrevious)
            {
                PubServer.AddTopicMessage("${Me.CurrentMana}", MagicPointsCurrent.ToString("N0"));
                MagicPointsPrevious = MagicPointsCurrent;
            }
            StamPointsCurrent = Mq.Query<int>("${Me.CurrentEndurance}");
            if (StamPointsCurrent != StamPointsPrevious)
            {
                PubServer.AddTopicMessage("${Me.CurrentEndurance}", StamPointsCurrent.ToString("N0"));
                StamPointsPrevious = StamPointsCurrent;
            }
            bool tCombat = Basics.InCombat();
            if(tCombat!= CurrentInCombat)
            {
                CurrentInCombat = tCombat;
                PubServer.AddTopicMessage("${InCombat}", CurrentInCombat.ToString());
            }

            string nameOfPet = Mq.Query<string>("${Me.Pet.CleanName}");
            if(nameOfPet!="NULL")
            {
                //set the pet name
                CurrentPetName = nameOfPet;
                PubServer.AddTopicMessage("${Me.Pet.CleanName}", CurrentPetName);

            }
            

            int zoneID = Mq.Query<int>("${Zone.ID}"); //to tell if we zone mid process
            if (zoneID != ZoneID)
            {
                ZoneID = zoneID;

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
                CharacterSettings = new Settings.CharacterSettings();
                CurrentClass = CharacterSettings._characterClass;
                CurrentLongClassString = CurrentClass.ToString();
                CurrentShortClassString = Data.Classes._classLongToShort[CurrentLongClassString];


                //end class information

                GeneralSettings = new Settings.GeneralSettings();
                AdvancedSettings = new Settings.AdvancedSettings();

                //setup is done after the settings are setup.
                //as there is an order dependecy
                Setup.Init();

                Bots = new Bots();
                IsInit = true;
                MonoCore.Spawns._refreshTimePeriodInMS = 3000;
                //_uiThread = Task.Factory.StartNew(() => { ProcessUI(); }, CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);

            }


        }
        //enum ClassStyle
        //{
        //    CS_VREDRAW = 0x00000001,
        //    CS_HREDRAW = 0x00000002,
        //    CS_KEYCVTWINDOW = 0x00000004,
        //    CS_DBLCLKS = 0x00000008,
        //    CS_OWNDC = 0x00000020,
        //    CS_CLASSDC = 0x00000040,
        //    CS_PARENTDC = 0x00000080,
        //    CS_NOKEYCVT = 0x00000100,
        //    CS_NOCLOSE = 0x00000200,
        //    CS_SAVEBITS = 0x00000800,
        //    CS_BYTEALIGNCLIENT = 0x00001000,
        //    CS_BYTEALIGNWINDOW = 0x00002000,
        //    CS_GLOBALCLASS = 0x00004000,
        //    CS_IME = 0x00010000,
        //    // Windows XP+
        //    CS_DROPSHADOW = 0x00020000
        //}


        //private static System.Collections.Concurrent.ConcurrentQueue<string> _consoleLines = new System.Collections.Concurrent.ConcurrentQueue<string>();
        //[STAThread]
        //private static void ProcessUI()
        //{
        //   EventProcessor.RegisterEvent("ConsoleData",".+", (x) =>
        //   {
        //       _consoleLines.Enqueue(x.eventString);
          
        //   });

        //    Application.EnableVisualStyles();
        //    Application.SetCompatibleTextRenderingDefault(false);
        //    _uiForm = new E3NextUI.E3UI();
        //    _uiInit = true;
        //    _uiForm.SetPlayerName(E3.CurrentName);
        //    _uiForm.Show();
        //    while (Core._isProcessing)
        //    {
        //        lock(_uiForm)
        //        {
        //            _uiForm.SetPlayerHP(CurrentHitPoints.ToString("N0"));
        //            while(_consoleLines.Count>0)
        //            {
        //                string line;
        //                if (_consoleLines.TryDequeue(out line))
        //                {
        //                    _uiForm.AddConsoleLine(line);
        //                }
        //            }
        //            Application.DoEvents();
        //         }
        //        System.Threading.Thread.Sleep(50);
        //    }
        //    UnloadUI();

        //}

        //private static void UnloadUI()
        //{
        //    _uiForm.Shutdown();
        //    StringBuilder ClassName = new StringBuilder(256);
        //    Core.GetClassName(_uiForm.Handle, ClassName, ClassName.Capacity);
        //    uint pid = 0;
        //    Core.GetWindowThreadProcessId(_uiForm.Handle, out pid);
        //    Process p = System.Diagnostics.Process.GetProcessById((int)pid);
        //    IntPtr hinstance = Core.GetModuleHandle(p.MainModule.FileName);
        //    _uiForm.Close();
        //    _uiForm.Dispose();
        //    Core.UnregisterClass(ClassName.ToString(), hinstance);
        //    Application.Exit();
        //}
        private static bool ShouldRun()
        {

            if (IsBadState)
            {
                return false;
            }

            return true;
        }
        private static void RegisterEvents()
        {

            //register events
            //Event Line:"Pyra tells the group, 'SWARM-Host of the Elements'"
            //EventProcessor.RegisterEvent("EverythingEvent", "(.+) tells the group, '(.+)'", (x) => {

            //    _log.Write($"{ x.eventName}:Processed:{ x.eventString}");

            //});
            //can also just pass it any method with signature
            ////static void ProcessEvent(EventProcessor.EventMatch x)
        }
        public static void Shutdown()
        {
            //Mq.Write($"Shutting down {MainProcessor._applicationName}....Reload to start gain");
            IsBadState = true;
           
        }
        public static Int32 PreviousHP;
        //private static Task _uiThread;
        //public static E3NextUI.E3UI _uiForm;
        //public static volatile bool _uiInit = false;
        //used throughout e3 per loop to allow kickout form methods
        public static bool ActionTaken = false;
        public static bool Following = false;
        public static long StartTimeStamp;
        public static bool IsInit = false;
        public static bool IsBadState = false;
        public static IMQ Mq = Core.mqInstance;
        //public static IMQ MQ = new MoqMQ();
        public static Logging Log = Core._log;
        public static Settings.CharacterSettings CharacterSettings = null;
        public static Settings.GeneralSettings GeneralSettings = null;
        public static Settings.AdvancedSettings AdvancedSettings = null;
        public static IBots Bots = null;
        public static string CurrentName;
        public static string CurrentPetName = String.Empty;
        public static bool CurrentInCombat = false;
        public static int CurrentId;
        public static Data.Class CurrentClass;
        public static string CurrentLongClassString;
        public static string CurrentShortClassString;
        public static int CurrentHps;
        public static int HitPointsCurrent;
        public static int HitPointsPrevious;
        public static int MagicPointsCurrent;
        public static int MagicPointsPrevious;
        public static int StamPointsCurrent;
        public static int StamPointsPrevious;
        public static int ZoneID;
        public static int ProcessDelay = 50;
        public static ISpawns Spawns = Core.spawnInstance;
        public static bool IsInvis;
    }
}
