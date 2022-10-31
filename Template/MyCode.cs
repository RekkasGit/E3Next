using MonoCore;
using System;
namespace Template
{
    public static class MyCode
    {

        public static Boolean _isInit = false;
        public static IMQ MQ = Core.mqInstance;
        public static Logging _log = Core._log;
  

        //this will be called about once per frame if you have no delay, put a delay :) defaulting to 1000 milliseconds
        //via MainProcessor._processDelay = 1000 in the setup method
        public static void Process()
        {
            #region init
            if (!_isInit) { Init(); }
            #endregion
            ///Write your code here.
            string name = MQ.Query<string>("${Me.Name}");
            MQ.Write($"Hello {name}!");
       
        
        
        }

        private static void Init()
        {

            if (!_isInit)
            {   
                Logging._traceLogLevel = Logging.LogLevels.None; //Trace or None. Others will simply be treated as if Trace was turned on.
                Logging._minLogLevelTolog = Logging.LogLevels.Debug; //set to the min value to log. look at the int values. if set to Info, you will not log debugs.
                Logging._defaultLogLevel = Logging.LogLevels.Debug; //the default if a level is not passed into the _log.write statement. useful to hide/show things.
                MainProcessor._applicationName = "MyApplication"; //application name, used in some outputs
                MainProcessor._processDelay = 2000; //how much time we will wait until we start our next processing once we are done with a loop. in milliseconds. 1000 = 1 second.
                MonoCore.MQ._maxMillisecondsToWork = 40;  //how long you allow script to process before auto yield is engaged. generally should't need to mess with this, but an example.
                EventProcessor._eventLimiterPerRegisteredEvent = 20;//max event count for each registered event before spilling over.
                _isInit = true;
            }
        }
    }
}
