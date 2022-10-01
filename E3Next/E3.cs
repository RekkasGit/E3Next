using E3Core.Processors;
using E3Core.Settings;
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
       


        //used throughout e3 per loop to allow kickout form methods
        public static Boolean _actionTaken = false;
        public static Boolean _following = false;
        public static Int64 _startTimeStamp;
        public static Boolean _isInit = false;
        public static Boolean _isBadState = false;
        public static IMQ MQ = Core.mqInstance;
        //public static IMQ MQ = new MoqMQ();
        public static Logging _log = Core._log;
        private static Settings.CharacterSettings _characterSettings=null;
        private static Settings.GeneralSettings _generalSettings=null;
        private static Settings.AdvancedSettings _advancedSettings = null;
        public static Data.Class _currentClass;
        public static string _currentLongClassString;
        public static string _currentShortClassString;
        public static Int32 _zoneID;


        public static void Process()
        {
            if (!ShouldRun())
            {
                return;
            }

            //_startTimeStamp = Core._stopWatch.ElapsedMilliseconds;
            using (_log.Trace())
            {
                //update on every loop
                _zoneID = MQ.Query<Int32>("${Zone.ID}"); //to tell if we zone mid process
                //action taken is always set to false at the start of the loop
                _actionTaken = false;
                //Init is here to make sure we only Init while InGame, as some queries will fail if not in game
                if (!_isInit) { Init(); }


                List<string> _methodsToInvokeAsStrings;
                if (AdvancedSettings._classMethodsAsStrings.TryGetValue(_currentShortClassString, out _methodsToInvokeAsStrings))
                {
                    foreach (var methodName in _methodsToInvokeAsStrings)
                    {
                        //if an action was taken, start over
                        if (_actionTaken) break;

                        Action methodToInvoke;
                        if (AdvancedSettings._methodLookup.TryGetValue(methodName, out methodToInvoke))
                        {
                            methodToInvoke.Invoke();
                        }
                    }
                }
            }


            //MQ.Write("Total Processing time in ms:" + (Core._stopWatch.ElapsedMilliseconds - _startTimeStamp));
        }




        private static void Init()
        {

            if(!_isInit)
            {
                Logging._traceLogLevel = Logging.LogLevels.Debug; //log level we are currently at
                Logging._minLogLevelTolog = Logging.LogLevels.Debug; //log levels have integers assoicatd to them. you can set this to Error to only log errors. 
                Logging._defaultLogLevel = Logging.LogLevels.Debug; //the default if a level is not passed into the _log.write statement. useful to hide/show things.
                MainProcessor._applicationName = "E3Next"; //application name, used in some outputs
                MainProcessor._processDelay = 100; //how much time we will wait until we start our next processing once we are done with a loop.
                                                    //how long you allow script to process before auto yield is engaged. generally should't need to mess with this, but an example.
                MonoCore.MQ._maxMillisecondsToWork = 40;
                //max event count for each registered event before spilling over.
                EventProcessor._eventLimiterPerRegisteredEvent = 20;

                //do first to get class information
                _characterSettings = new Settings.CharacterSettings();
                _currentClass = _characterSettings._characterClass;
                //end class information

                _generalSettings = new Settings.GeneralSettings();
                _advancedSettings = new Settings.AdvancedSettings();

                Setup.Init();

                _currentLongClassString = _currentClass.ToString();
                _currentShortClassString = Data.Classes._classLongToShort[_currentLongClassString];
                //RegisterEvents();
                _isInit = true;
            }
           

        }

      
        private static bool ShouldRun()
        {

            if (_isBadState)
            {
                return false;
            }

            string gameState = MQ.Query<string>("${EverQuest.GameState}");

            //MQ.Write("GameState:" + gameState);

            //only process while in game
            if (gameState != "INGAME") return false;

            return true;
        }
        private static void RegisterEvents()
        {

            //register events
            //Event Line:"Pyra tells the group, 'SWARM-Host of the Elements'"
            EventProcessor.RegisterEvent("EverythingEvent", "(.+) tells the group, '(.+)'", (x) => {
                
                _log.Write($"{ x.eventName}:Processed:{ x.eventString}");

            });
            //can also just pass it any method with signature
            ////static void ProcessEvent(EventProcessor.EventMatch x)
        }
        public static void Shutdown()
        {
            MQ.Write($"Shutting down {MainProcessor._applicationName}....Reload to start gain");
            _isBadState = true;
           
        }
        /// <summary>
        /// this is used if you want to MOCK out MQ for testing outside of EQ/MQ.
        /// </summary>
        public class MoqMQ : MonoCore.IMQ
        {
            public void Broadcast(string query)
            {
                Console.WriteLine("Broadcast:" + query);
            }

            public void Cmd(string query)
            {
                Console.WriteLine("CMD:" + query);
                //do work
            }

            public void Delay(int value)
            {


                //lets tell core that it can continue
                Core._coreResetEvent.Set();
                //we are now going to wait on the core
                MainProcessor._processResetEvent.Wait();
                MainProcessor._processResetEvent.Reset();

            }

            public bool Delay(int maxTimeToWait, string Condition)
            {
                return true;
            }

            public T Query<T>(string query)
            {

                if (typeof(T) == typeof(Int32))
                {


                }
                else if (typeof(T) == typeof(Boolean))
                {



                }
                else if (typeof(T) == typeof(string))
                {
                    if (query == "${MacroQuest.Path[macros]}") return (T)(object)@"G:\EQ\e3Test\Macros";
                    if (query == "${MacroQuest.Path[config]}") return (T)(object)@"G:\EQ\e3Test\config";
                    if (query == "${MacroQuest.Server}") return (T)(object)@"Project Lazarus"; ;
                    if (query == "${Me.Class}") return (T)(object)"Druid";
                    if (query == "${Me.CleanName}") return (T)(object)"Shadowvine";
                    if (query == "${EverQuest.GameState}") return (T)(object)"INGAME";
                    //can also use regex or whatever.

                    return (T)(object)String.Empty;
                }
                else if (typeof(T) == typeof(decimal))
                {

                }
                else if (typeof(T) == typeof(Int64))
                {

                }


                return default(T);

            }

            public void TraceEnd(string methodName)
            {
                Console.WriteLine("traceend:" + methodName);
            }

            public void TraceStart(string methodName)
            {
                Console.WriteLine("tracestart:" + methodName);
            }

            public void Write(string query, [CallerMemberName] string memberName = "", [CallerFilePath] string fileName = "", [CallerLineNumber] int lineNumber = 0)
            {
                Console.WriteLine($"[{System.DateTime.Now.ToString("HH:mm:ss")}] {query}");
            }
        }
    }
}
