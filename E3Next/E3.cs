using E3Core.Processors;
using E3Core.Settings;
using E3Core.Settings.FeatureSettings;
using MonoCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace E3Core.Processors
{
    public static class E3
    {
        public static void Process()
        {

            if (!ShouldRun())
            {
                return;
            }

            //_startTimeStamp = Core._stopWatch.ElapsedMilliseconds;
            //using (_log.Trace())
            {
                if (!_isInit) { Init(); }
                //update on every loop
               
                Int32 zoneID = MQ.Query<Int32>("${Zone.ID}"); //to tell if we zone mid process
                if(zoneID!=_zoneID)
                {
                    _zoneID = zoneID;

                }
                EventProcessor.ProcessEventsInQueues("/e3p");
                if (Basics._isPaused)
                {
                    return;
                }

               

                _isInvis = MQ.Query<bool>("${Me.Invis}");
                //action taken is always set to false at the start of the loop
                _actionTaken = false;
                RefreshCaches();
                //Init is here to make sure we only Init while InGame, as some queries will fail if not in game



                //nowcast before all.
                EventProcessor.ProcessEventsInQueues("/nowcast");
                //use burns if able
                Burns.UseBurns();
                //do the basics first
                //first and formost, do healing checks
                if ((_currentClass& Data.Class.Priest)==_currentClass)
                {
                    Heals.Check_Heals();
                    //if (_actionTaken) return;
                }


                Assist.Process();
                WaitForRez.Process();
                
                if (!_actionTaken)
                {
                    //rembmer check_heals is auto inserted, should probably just pull out here
                    List<string> _methodsToInvokeAsStrings;
                    if (AdvancedSettings._classMethodsAsStrings.TryGetValue(_currentShortClassString, out _methodsToInvokeAsStrings))
                    {
                        foreach (var methodName in _methodsToInvokeAsStrings)
                        {
                            //if an action was taken, start over
                            if (_actionTaken)
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
                //now do the dynamic methods from Advanced ini. 
                
                //lets do our class methods, this is last because of bards
                foreach (var kvp in AdvancedSettings._classMethodLookup)
                {
                    kvp.Value.Invoke();
                }
            }
            Loot.Process();
            //MQ.Write("Total Processing time in ms:" + (Core._stopWatch.ElapsedMilliseconds - _startTimeStamp));
        }
        private static void RefreshCaches()
        {
            Casting.RefreshGemCache();
            Basics.RefreshGroupMembers();
        }
        private static void Init()
        {

            if(!_isInit)
            {
                MQ.ClearCommands();
    
                Logging._traceLogLevel = Logging.LogLevels.None; //log level we are currently at
                Logging._minLogLevelTolog = Logging.LogLevels.Error; //log levels have integers assoicatd to them. you can set this to Error to only log errors. 
                Logging._defaultLogLevel = Logging.LogLevels.Debug; //the default if a level is not passed into the _log.write statement. useful to hide/show things.
                MainProcessor._applicationName = "E3"; //application name, used in some outputs
                MainProcessor._processDelay = 50; //how much time we will wait until we start our next processing once we are done with a loop.
                                                    //how long you allow script to process before auto yield is engaged. generally should't need to mess with this, but an example.
                MonoCore.MQ._maxMillisecondsToWork = 40;
                //max event count for each registered event before spilling over.
                EventProcessor._eventLimiterPerRegisteredEvent = 20;
                _currentName = MQ.Query<string>("${Me.CleanName}");
                //do first to get class information
                _characterSettings = new Settings.CharacterSettings();
                _currentClass = _characterSettings._characterClass;
                _currentLongClassString = _currentClass.ToString();
                _currentShortClassString = Data.Classes._classLongToShort[_currentLongClassString];
                
                //end class information

                _generalSettings = new Settings.GeneralSettings();
                _advancedSettings = new Settings.AdvancedSettings();
                

                Setup.Init();
                //LootDataFile.Init();
              
               
                _bots=new Bots();
                _isInit = true;
                Spawns._refreshTimePeriodInMS = 3000;

            }
           

        }

      
        private static bool ShouldRun()
        {

            if (_isBadState)
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
            MQ.Write($"Shutting down {MainProcessor._applicationName}....Reload to start gain");
            _isBadState = true;
           
        }
       
        //used throughout e3 per loop to allow kickout form methods
        public static Boolean _actionTaken = false;
        public static Boolean _following = false;
        public static Int64 _startTimeStamp;
        public static Boolean _isInit = false;
        public static Boolean _isBadState = false;
        public static IMQ MQ = Core.mqInstance;
        //public static IMQ MQ = new MoqMQ();
        public static Logging _log = Core._log;
        public static Settings.CharacterSettings _characterSettings = null;
        public static Settings.GeneralSettings _generalSettings = null;
        public static Settings.AdvancedSettings _advancedSettings = null;
        public static IBots _bots = null;
        public static string _currentName;
        public static Data.Class _currentClass;
        public static string _currentLongClassString;
        public static string _currentShortClassString;
        public static Int32 _zoneID;
       


        public static ISpawns _spawns = Core.spawnInstance;
        public static bool _isInvis;


    }
}
