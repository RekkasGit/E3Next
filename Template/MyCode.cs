using MonoCore;
using System;
namespace Template
{
    public static class MyCode
    {

        public static Boolean _isInit = false;
        public static IMQ MQ = Core.mqInstance;
        public static Logging _log = Core.logInstance;
        //this will be called about once per frame if you have no delay, put a delay :) use MainProcessor.ProcessDelay
        public static void Process()
        {
            #region init
            if (!_isInit) { Init(); }
            #endregion
            ///Write your code here.
            string name = MQ.Query<string>("${Me.Name}");

            MQ.Write($"Hello {name}! DomainId:{System.Threading.Thread.GetDomainID()}");
        }

        private static void Init()
        {
            if (!_isInit)
            {   
                Logging.TraceLogLevel = Logging.LogLevels.None; //Trace or None. Others will simply be treated as if Trace was turned on.
                Logging.MinLogLevelTolog = Logging.LogLevels.Debug; //set to the min value to log. look at the int values. if set to Info, you will not log debugs.
                Logging.DefaultLogLevel = Logging.LogLevels.Debug; //the default if a level is not passed into the _log.write statement. useful to hide/show things.
                MainProcessor.ApplicationName = "MyApplication"; //application name, used in some outputs
                MainProcessor.ProcessDelay = 2000; //how much time we will wait until we start our next processing once we are done with a loop. in milliseconds. 1000 = 1 second.
                MonoCore.MQ.MaxMillisecondsToWork = 40;  //how long you allow script to process before auto yield is engaged. generally should't need to mess with this, but an example.
                EventProcessor.EventLimiterPerRegisteredEvent = 20;//max event count for each registered event before spilling over.
                _isInit = true;
            }
        }
    }
}
