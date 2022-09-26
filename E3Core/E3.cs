using E3Core.Processors;
using E3Core.Settings;
using MonoCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace E3Core.Processors
{
    public static class E3
    {
       


        //used throughout e3 per loop to allow kickout form methods
        public static Boolean _actionTaken = false;
        public static Boolean _isPriest = false;
        public static Int64 _startTimeStamp;
        public static Boolean _isInit = false;
        public static Boolean _isBadState = false;
        private static IMQ MQ = Core.mqInstance;
        public static Logging _log = Core._log;
        private static Settings.CharacterSettings _characterSettings=null;
        private static Settings.GeneralSettings _generalSettings=null;
        private static Settings.AdvancedSettings _advancedSettings = null;
        public static Data.Class _currentClass;
        public static string _currentLongClassString;
        public static string _currentShortClassString;
        public static Int32 _zoneID;
        public static void Init()
        {
            Logging._currentLogLevel = Logging.LogLevels.Debug;
            Setup.Init();
            //register events
            EventProcessor.EventLimiterPerRegisteredEvent = 20;
            EventProcessor.RegisterEvent("SwarmEvent", "(.+) tells the group, 'SWARM-(.+)'", (x) => {


                string user = string.Empty;
                string swarmSpell = String.Empty;
                if(x.match.Groups.Count>2)
                {
                    user=x.match.Groups[1].Value;
                    swarmSpell = x.match.Groups[2].Value;
                }


                _log.Write($"{ x.eventName}:{ user} cast the swarm pet: {swarmSpell}"); 
            
            });

            //do first to get class information
            _characterSettings = new Settings.CharacterSettings();
            _currentClass = _characterSettings._characterClass;
            //end class information

            _generalSettings = new Settings.GeneralSettings();
            _advancedSettings = new Settings.AdvancedSettings();
           
            _currentLongClassString = _currentClass.ToString();
            _currentShortClassString = Data.Classes._classLongToShort[_currentLongClassString];
            _isInit = true;

        }

        public static void Process()
        {
            if (!ShouldRun())
            {
                return;
            }
            _startTimeStamp = Core._stopWatch.ElapsedMilliseconds;
            using (_log.Trace())
            {
                _zoneID = MQ.Query<Int32>("${Zone.ID}");
                _actionTaken = false;
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

           
            MQ.Write("Total Processing time in ms:" + (Core._stopWatch.ElapsedMilliseconds - _startTimeStamp));
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
        private static void ReloadSettings()
        {


        }
        public static void Shutdown()
        {
            MQ.Write("Shutting down E3....Reload to start gain");
            _isBadState = true;
           
        }
    }
}
