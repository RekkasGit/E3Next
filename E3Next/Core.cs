using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Diagnostics;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using E3Core.Processors;
using System.Runtime.InteropServices;

/// <summary>
/// Version 0.1
/// This file, is the 'core', or the mediator between you and MQ2Mono 
/// MQ2Mono is just a simple C++ plugin to MQ, thus exposes the 
/// OnInit
/// OnPulse
/// OnIncomingChat
/// etc
/// Methods from the plugin, and meshes it in such a way to allow you to write rather straight forward C# code. 
/// 
/// Included in this is a Logging/trace framework, Event Proecssor, MQ command object/etc
/// 
/// Your class is included in here with a simple .Process Method. This methoid will be called once every OnPulse from the plugin, or basically every frame of the EQ client.
/// All you code should *NOT* be in this file. 
/// 
/// </summary>

namespace MonoCore
{
    /// <summary>
    /// this is the class for the main C# thread
    //  the C++ core thread will call this in a task at startup
    /// </summary>
    public static class MainProcessor
    {
        public static IMQ MQ = Core.mqInstance;
        public static Int32 _processDelay = 200;
        private static Logging _log = Core._log;
        public static string _applicationName = "";
        public static Int64 _startTimeStamp;
        public static Int64 _processingCounts;
        public static Int64 _totalProcessingCounts;
        public static void Init()
        {

            //WARNING , you may not be in game yet, so careful what queries you run on MQ.Query. May cause a crash.
            //how long before auto yielding back to C++/EQ/MQ on the next query/command/etc
            //Logging._currentLogLevel = Logging.LogLevels.None; //log level we are currently at
            //Logging._minLogLevelTolog = Logging.LogLevels.Error; //log levels have integers assoicatd to them. you can set this to Error to only log errors. 
            //Logging._defaultLogLevel = Logging.LogLevels.None; //the default if a level is not passed into the _log.write statement. useful to hide/show things.
         

        }
        //we use this to tell the C++ thread that its okay to start processing gain
        //public static EconomicResetEvent _processResetEvent = new EconomicResetEvent(false, Thread.CurrentThread);
        //public static AutoResetEvent _processResetEvent = new AutoResetEvent(false);
        public static ManualResetEventSlim _processResetEvent = new ManualResetEventSlim(false);


        public static void Process()
        {
            //wait for the C++ thread thread to tell us we can go
            _processResetEvent.Wait();
            _processResetEvent.Reset();
            _startTimeStamp = Core._stopWatch.ElapsedMilliseconds;
            
            //volatile variable, will eventually update to kill the thread on shutdown
            while (Core._isProcessing)
            {
                //_startLoopTime = Core._stopWatch.Elapsed.TotalMilliseconds;
                //_processingCounts++;
                //_totalProcessingCounts++;
                try
                {
                    //MQ.TraceStart("Process");
                    using (_log.Trace())
                    {
                        //************************************************
                        //DO YOUR WORK HERE
                        //this loop executes once every OnPulse from C++
                        //************************************************
                        //just have a class call a process method or such so you don't have to see this 
                        //boiler plate code with all the threading.

                        ////MQ.Write("Calling e3process");
                        /// using (_log.Trace("EventProcessing"))
                        {
                            EventProcessor.ProcessEventsInQueues();
                        }
                        E3.Process();
                       
                        ////***NOTE NOTE NOTE, Use M2.Delay(0) in your code to give control back to EQ if you have taken awhile in doing something
                        ////***NOTE NOTE NOTE, generally this isn't needed as there is an auto yield baked into every MQ method. Just be aware.

                    }

                    //MQ.TraceEnd("Process");
                    //process all the events that have been registered
                    //process all the events that have been registered

                    //Double endLoopTimeInMS = Core._stopWatch.Elapsed.TotalMilliseconds - _startLoopTime;
                    //_totalLoopTime += endLoopTimeInMS;

                    ////every 5 seconds, print out the # processed and average time.
                    //if ((Core._stopWatch.ElapsedMilliseconds > (_startTimeStamp + 5000)))
                    //{


                    //    MQ.Write($"Total Count:{_totalProcessingCounts}, Total this cycle {_processingCounts} average time {_totalLoopTime / _processingCounts}ms");
                    //    _startTimeStamp = Core._stopWatch.ElapsedMilliseconds;
                    //    _processingCounts = 0;
                    //    _totalLoopTime = 0;
                    //}

                }
                catch (Exception ex) when (!(ex is ThreadAbort))
                {
                    if(Core._isProcessing)
                    {
                        _log.Write("Error: Please reload. Terminating. \r\nExceptionMessage:" + ex.Message + " stack:" + ex.StackTrace.ToString(), Logging.LogLevels.CriticalError);

                    }
                    Core._isProcessing = false;
                    //lets tell core that it can continue
                    //test
                    Core._coreResetEvent.Set();
                    //we perma exit this thread loop a full reload will be necessary
                    return;
                }

                //SET YOUR MACRO DELAY HERE
                //this is the pulse you will get on your call. 1 sec is generally fine unless you are a much bigger script
                //like e3, that may change it to 10.
                MQ.Delay(_processDelay);//this calls the reset events and sets the delay to 10ms at min

                //***********************************************
                //END YOUR WORK
                //**********************************************


            }

        }

    }
    /// <summary>
    /// Processor to handle Event strings
    /// It spawns its own thread to do the inital regex parse, whatever matches will be 
    /// put into the proper queue for each event for later invoke when the C# thread comes around
    /// </summary>
    static public class EventProcessor
    {
        //event is loaded at startup and then not modified anymore
        //so if we register events before Init, we can avoid locks on it
        //will need to add locks if you want to add events at runtime
        public static System.Collections.Concurrent.ConcurrentDictionary<string, EventListItem> _eventList = new ConcurrentDictionary<string, EventListItem>();
        public static System.Collections.Concurrent.ConcurrentDictionary<string, CommandListItem> _commandList = new ConcurrentDictionary<string, CommandListItem>();
        //this is the first queue that strings get put into, will be processed by its own thread
        public static ConcurrentQueue<String> _eventProcessingQueue = new ConcurrentQueue<String>();
        public static ConcurrentQueue<String> _mqEventProcessingQueue = new ConcurrentQueue<string>();
        public static ConcurrentQueue<String> _mqCommandProcessingQueue = new ConcurrentQueue<string>();
        private static StringBuilder _tokenBuilder = new StringBuilder();
        private static List<string> _tokenResult = new List<string>();
        //if matches take place, they are placed in this queue for the main C# thread to process. 
        public static Int32 _eventLimiterPerRegisteredEvent = 10;
        //this threads entire purpose, is to simply keep processing the event processing queue and place matches into
        //the eventfilteredqueue
        public static Task _regExProcessingTask;
        private static Logging _log = Core._log;

        private static Boolean _isInit = false;
        public static void Init()
        {
            if (!_isInit)
            {
                _regExProcessingTask = Task.Factory.StartNew(() => { ProcessEventsIntoQueues(); }, CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
                _isInit = true;
            }

        }
        /// <summary>
        /// Runs on its own thread, will process through all the strings passed in and then put them into the correct queue
        /// </summary>
        public static void ProcessEventsIntoQueues()
        {
            System.Text.RegularExpressions.Regex dannetRegex = new Regex("");
            char[] splitChars = new char[1] {' '};

            ////WARNING DO NOT SEND COMMANDS/Writes/Echos, etc from this thread. 
            ///only the primary C# thread can do that.
            while (Core._isProcessing)
            {
                if (_eventProcessingQueue.Count > 0)
                {
                    string line;
                    if (_eventProcessingQueue.TryDequeue(out line))
                    {
                        foreach (var item in _eventList)
                        {
                            //prevent spamming of an event to a user
                            if (item.Value.queuedEvents.Count > _eventLimiterPerRegisteredEvent)
                            {
                                continue;
                            }
                            foreach (var regex in item.Value.regexs)
                            {
                                var match = regex.Match(line);
                                if (match.Success)
                                {

                                    item.Value.queuedEvents.Enqueue(new EventMatch() { eventName = item.Value.keyName, eventString = line, match = match });

                                    break;
                                }
                            }

                        }

                    }
                }
                else if (_mqEventProcessingQueue.Count > 0)
                {
                    //have to be careful here and process out anything that isn't boxchat or dannet.
                    string line;
                    if (_mqEventProcessingQueue.TryDequeue(out line))
                    {
                        if (line.StartsWith("["))
                        {
                            Int32 indexOfApp = line.IndexOf(MainProcessor._applicationName);
                            if (indexOfApp == 1)
                            {
                                if (line.IndexOf("]") == MainProcessor._applicationName.Length + 1)
                                {
                                    //this starts with [appname], ignore it. 
                                    goto skipLine;
                                    //return;
                                }
                                else
                                {
                                    goto processLine;
                                }

                            }
                        }
                        processLine:
                        foreach (var item in _eventList)
                        {
                            //prevent spamming of an event to a user
                            if (item.Value.queuedEvents.Count > _eventLimiterPerRegisteredEvent)
                            {
                                continue;
                            }

                            foreach (var regex in item.Value.regexs)
                            {
                                var match = regex.Match(line);
                                if (match.Success)
                                {

                                    item.Value.queuedEvents.Enqueue(new EventMatch() { eventName = item.Value.keyName, eventString = line, match = match });

                                    break;
                                }
                            }

                        }

                    }
                    skipLine:
                    continue;
                }
                else if (_mqCommandProcessingQueue.Count > 0)
                {
                    //have to be careful here and process out anything that isn't boxchat or dannet.
                    string line;
                    if (_mqCommandProcessingQueue.TryDequeue(out line))
                    {
                         if (!String.IsNullOrWhiteSpace(line))
                        {
                            foreach (var item in _commandList)
                            {
                                //prevent spamming of an event to a user
                                if (item.Value.queuedEvents.Count > _eventLimiterPerRegisteredEvent)
                                {
                                    Core.mqInstance.Write("event limiter");

                                    continue;
                                }
                                if (line.Equals(item.Value.command, StringComparison.OrdinalIgnoreCase) || line.StartsWith(item.Value.command + " ", StringComparison.OrdinalIgnoreCase))
                                {
                                    //need to split out the params
                                    List<String> args = ParseParms(line, ' ', '"').ToList();
                                    args.RemoveAt(0);
                                    item.Value.queuedEvents.Enqueue(new CommandMatch() { eventName = item.Value.keyName, eventString = line, args = args });
                                }
                            }
                        }
                    }
                }
                else
                {
                    System.Threading.Thread.Sleep(1);
                }
            }
        }
        public static List<String> ParseParms(String line, Char delimiter, Char textQualifier)
        {

            _tokenResult.Clear();

            if (String.IsNullOrWhiteSpace(line))
            {
                return _tokenResult;
            }
            else
            {
                Char prevChar = '\0';
                Char nextChar = '\0';
                Char currentChar = '\0';

                Boolean inString = false;
                _tokenBuilder.Clear();
                string result = string.Empty;
                for (int i = 0; i < line.Length; i++)
                {
                    currentChar = line[i];

                    if (i > 0)
                        prevChar = line[i - 1];
                    else
                        prevChar = '\0';

                    if (i + 1 < line.Length)
                        nextChar = line[i + 1];
                    else
                        nextChar = '\0';

                    if (currentChar == textQualifier && (prevChar == '\0' || prevChar == delimiter) && !inString)
                    {
                        inString = true;
                        continue;
                    }

                    if (currentChar == textQualifier && (nextChar == '\0' || nextChar == delimiter) && inString)
                    {
                        inString = false;
                        continue;
                    }

                    if (currentChar == delimiter && !inString)
                    {
                        result = _tokenBuilder.ToString();
                        if (!String.IsNullOrWhiteSpace(result))
                        {
                            _tokenResult.Add(result);
                        }

                        _tokenBuilder = _tokenBuilder.Remove(0, _tokenBuilder.Length);
                        continue;
                    }
                    result = _tokenBuilder.ToString();
                    _tokenBuilder = _tokenBuilder.Append(currentChar);

                }
                result = _tokenBuilder.ToString();
                if (!String.IsNullOrWhiteSpace(result))
                {
                    _tokenResult.Add(result);
                }
                //yield return _tokenBuilder.ToString();
                return _tokenResult;
            }
        }

        public static void ProcessEventsInQueues(string keyName = "")
        {
            foreach (var item in _eventList)
            {
                //check to see if we have to have a filter on the events to process
                if (!String.IsNullOrWhiteSpace(keyName))
                {
                    //if keyName is specified, verify that its the key we want. 
                    if (!item.Value.keyName.Equals(keyName, StringComparison.OrdinalIgnoreCase))
                    {

                        continue;
                    }
                }
                //_log.Write($"Checking Event queue. Total:{item.Value.queuedEvents.Count}");
                while (item.Value.queuedEvents.Count > 0)
                {

                    EventMatch line;
                    if (item.Value.queuedEvents.TryDequeue(out line))
                    {
                        item.Value.method.Invoke(line);
                    }
                }

            }
            foreach (var item in _commandList)
            {
                //check to see if we have to have a filter on the events to process
                if (!String.IsNullOrWhiteSpace(keyName))
                {
                    //if keyName is specified, verify that its the key we want. 
                    if (!item.Value.keyName.Equals(keyName, StringComparison.OrdinalIgnoreCase))
                    {

                        continue;
                    }
                }
                while (item.Value.queuedEvents.Count > 0)
                {

                    CommandMatch line;
                    if (item.Value.queuedEvents.TryDequeue(out line))
                    {
                        item.Value.method.Invoke(line);
                    }
                }

            }
        }
        /// <summary>
        /// main entry from the C++ thread to place the event string for processing
        /// </summary>
        /// <param name="line"></param>
        public static void ProcessEvent(string line)
        {
            
            //to prevent spams
            if (_eventList.Count > 0)
            {
                _eventProcessingQueue.Enqueue(line);
            }

        }

        public static void ProcessMQEvent(string line)
        {
           
            //to prevent spams
            if (_eventList.Count > 0)
            {
                _mqEventProcessingQueue.Enqueue(line);
            }

        }
        public static void ProcessMQCommand(string line)
        {
            //to prevent spams
            if (_eventList.Count > 0)
            {
                _mqCommandProcessingQueue.Enqueue(line);
            }

        }
        public static void ClearEventQueue(string keyName)
        {
            EventListItem tEventItem;
            if (_eventList.TryGetValue(keyName, out tEventItem))
            {
                EventMatch line;
                while (!tEventItem.queuedEvents.IsEmpty)
                {
                    tEventItem.queuedEvents.TryDequeue(out line);
                }
            }
        }
        public class EventListItem
        {
            public String keyName;
            public List<System.Text.RegularExpressions.Regex> regexs;
            public System.Action<EventMatch> method;
            public ConcurrentQueue<EventMatch> queuedEvents = new ConcurrentQueue<EventMatch>();
        }
        public class CommandListItem
        {
            public String keyName;
            public String command;
            public System.Action<CommandMatch> method;
            public ConcurrentQueue<CommandMatch> queuedEvents = new ConcurrentQueue<CommandMatch>();
        }
        public class CommandMatch
        {
            public List<String> args;
            public string eventString;
            public string eventName;
        }
        public class EventMatch
        {
            public string eventString;
            public Match match;
            public string eventName;
        }
        public static bool RegisterCommand(string commandName, Action<CommandMatch> method)
        {
            CommandListItem c = new CommandListItem();
            c.command = commandName;
            c.method = method;
            c.keyName = commandName;
            Core.mqInstance.Write("Adding command:" + commandName);
            bool returnvalue =  Core.mqInstance.AddCommand(commandName);
            Core.mqInstance.Write("Return from adding command:" + returnvalue);

            if (returnvalue)
            {   
                if (_commandList.TryAdd(commandName, c))
                {
                    //now to register the command over.
                    return true;
                }
                
            }
            return false;

        }
        public static void UnRegisterCommand(string commandName)
        {
            CommandListItem c;
            if (_commandList.TryRemove(commandName, out c))
            {
                Core.mqInstance.RemoveCommand(commandName);
            }

        }
        public static void RegisterEvent(string keyName, string pattern, Action<EventMatch> method)
        {
            EventListItem eventToAdd = new EventListItem();
            eventToAdd.regexs = new List<Regex>();

            eventToAdd.regexs.Add(new System.Text.RegularExpressions.Regex(pattern));
            eventToAdd.method = method;
            eventToAdd.keyName = keyName;

            _eventList.TryAdd(keyName, eventToAdd);

        }
        public static void RegisterEvent(string keyName, List<string> patterns, Action<EventMatch> method)
        {
            EventListItem eventToAdd = new EventListItem();
            eventToAdd.regexs = new List<Regex>();

            foreach (var pattern in patterns)
            {
                eventToAdd.regexs.Add(new System.Text.RegularExpressions.Regex(pattern));
            }

            eventToAdd.method = method;
            eventToAdd.keyName = keyName;

            _eventList.TryAdd(keyName, eventToAdd);

        }

    }
    public class ThreadAbort : Exception
    {
        public ThreadAbort()
        {
        }

        public ThreadAbort(string message)
            : base(message)
        {
        }

        public ThreadAbort(string message, Exception inner)
            : base(message, inner)
        {
        }
    }

    //This class is for C++ thread to come in and call. for the most part, leave this alone. 
    public static class Core
    {
        public static IMQ mqInstance; //needs to be declared first
        public static ISpawns spawnInstance;
        public static Logging _log;
        public volatile static bool _isProcessing = false;
        public const string _coreVersion = "0.1";


        //Note, if you comment out a method, this will tell MQ2Mono to not try and execute it
        //only use the events you need to prevent string allocations to be passed in
        //also, seems these need to be static

        //this is called quite often, haven't determined a throttle yet, but assume 10 times a second

        //this is protected by the lock, so that the primary C++ thread is the one that executes commands that the
        //processing thread has done.
        public static string _currentCommand = string.Empty;
        public static string _currentWrite = String.Empty;
        public static Int32 _currentDelay = 0;

        //delay in milliseconds
        public static Int64 _delayTime = 0;
        //timestamp in milliseconds.
        public static Int64 _delayStartTime = 0;
        public static Stopwatch _stopWatch = new Stopwatch();
        public static Int64 _onPulseCalls;
        public static bool _isInit = false;

        /// <summary>
        /// IMPORTANT, and why most of this works
        /// </summary>
        /*https://stackoverflow.com/questions/681505/does-an-eventwaithandle-have-any-implicit-memorybarrier
         * The .NET memory model ensures that all writes are volatile. Reads, by default, are not, 
         * unless an explicit VolatileRead is made, or the volatile keyword is specified on the field. 
         * Further, interlocked methods force cache coherency, and all of the synchronization concepts 
         * (Monitor, ReaderWriterLock, Mutex, Semaphore, AutoResetEvent, ManualResetEvent, etc.) 
         * call interlocked methods internally, and thus ensure cache coherency
         */
        //we use this to tell the C# thread that it can start processing.
        //also note, we don't need locks between C# primary thread and C++ thread
        //as the reset events handle the syn needed between memory/caches.
        //this only works as we only have 2 threads, otherwise you need fairness from normal locks.
        //the event procesor, however does need a lock as its on its own thread, to sync back to the C# primary thread
        //public static EconomicResetEvent _coreResetEvent = new EconomicResetEvent(false, Thread.CurrentThread);
        public static ManualResetEventSlim _coreResetEvent = new ManualResetEventSlim(false);
        static Task _taskThread;

        public static void OnInit()
        {


            if (!_isInit)
            {
                _isProcessing = true;
                if (mqInstance == null)
                {
                    mqInstance = new MQ();
                }
                if (spawnInstance == null)
                {
                    spawnInstance = new Spawns();
                }
                _log = new Logging(mqInstance);
                _stopWatch.Start();
                //do all necessary setups here
                MainProcessor.Init();
                //isProcessing needs to be true before the event processor has started
                EventProcessor.Init();


                if (_taskThread == null)
                {
                    //start up the main processor, this is where most of the C# code kicks off in
                    _taskThread = Task.Factory.StartNew(() => { MainProcessor.Process(); }, CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
                }
                _isInit = true;

            }


        }
        public static void OnStop()
        {
            _isProcessing = false;
        }
        public static void OnPulse()
        {

            if (!_isProcessing)
            {   
                //allow the primary thread to finish terminating. 
                MainProcessor._processResetEvent.Set();
                return;

            }//reset the last delay so we restart the procssing time since its a new OnPulse()
            MQ._sinceLastDelay = _stopWatch.ElapsedMilliseconds;

            _onPulseCalls++;
            //if delay was issued, we need to honor it, kickout for no processing
            if (_delayTime > 0)
            {
                if ((_stopWatch.ElapsedMilliseconds - _delayStartTime) < _delayTime)
                {   //we are still under the delay time specified, don't do any processing
                    //don't really need to do a lock as there are only two threads and the set below will sync the cores
                    return;

                }
                //reset if we have bypassed the time value
                _delayTime = 0;
            }

            RestartWait:
            //allow the processing thread to start its work.
            //and copy cache values to other cores so the thread can see the updated information in MQ
            MainProcessor._processResetEvent.Set();
            //Core.mq_Echo("Blocking on C++");
            Core._coreResetEvent.Wait();
            Core._coreResetEvent.Reset();
            //we need to block and chill out to let the other thread do its work
            //check to see if the 2nd thread has a command for us to send out
            //if so, we need to run the command, and then empty it
            if (_currentWrite != String.Empty)
            {
                //for writes, we stay in the main thread and just restart the check
                //commands and delays will release the main thread.
                Core.mq_Echo(_currentWrite);
                _currentWrite = String.Empty;
                goto RestartWait;
            }
            if (_currentCommand != String.Empty)
            {
                Core.mq_DoCommand(_currentCommand);
                _currentCommand = String.Empty;
                if (Core.mq_GetRunNextCommand())
                {
                    goto RestartWait;

                }
               
            }
            if (_currentDelay > 0)
            {
                Core.mq_Delay(_currentDelay);
                _currentDelay = 0;
            }
  

        }

        //Comment these out if you are not using events so that C++ doesn't waste time sending the string to C#
        public static void OnWriteChatColor(string line)
        {
            EventProcessor.ProcessMQEvent(line);
        }
        public static void OnCommand(string commandLine)
        {
            mq_Echo("command recieved:" + commandLine);

            EventProcessor.ProcessMQCommand(commandLine);
        }
        public static void OnIncomingChat(string line)
        {
            EventProcessor.ProcessEvent(line);
        }
        public static void OnSetSpawns(byte[] data, int size)
        {


            //pull the id out of the array
            Int32 ID = BitConverter.ToInt32(data, 0);
          
            Spawn s;
            if(Spawns._spawnsByID.TryGetValue(ID, out s))
            {
                //just update the value
                s.Init(data, size);
            }
            else
            {
                var spawn = Spawn.Aquire();
                spawn.Init(data, size);
                Spawns._spawns.Add(spawn);
            }

            
            //copy the data out into the current array set. 
        }

        //public static void OnUpdateImGui()
        //{

        //    if (imgui_Begin_OpenFlagGet("e3TestWindow"))
        //    {
        //        imgui_Begin("e3TestWindow", (int)ImGuiWindowFlags.ImGuiWindowFlags_None);
        //        imgui_Button("Test button");
        //        imgui_End();
        //    }


        //}

        #region MQMethods
        [MethodImpl(MethodImplOptions.InternalCall)]
        public extern static void mq_Echo(string msg);
        [MethodImpl(MethodImplOptions.InternalCall)]
        public extern static string mq_ParseTLO(string msg);
        [MethodImpl(MethodImplOptions.InternalCall)]
        public extern static void mq_DoCommand(string msg);
        [MethodImpl(MethodImplOptions.InternalCall)]
        public extern static void mq_Delay(int delay);
        [MethodImpl(MethodImplOptions.InternalCall)]
        public extern static bool mq_AddCommand(string command);
        [MethodImpl(MethodImplOptions.InternalCall)]
        public extern static void mq_ClearCommands();
        [MethodImpl(MethodImplOptions.InternalCall)]
        public extern static void mq_RemoveCommand(string command);
        [MethodImpl(MethodImplOptions.InternalCall)]
        public extern static void mq_GetSpawns();
        [MethodImpl(MethodImplOptions.InternalCall)]
        public extern static bool mq_GetRunNextCommand();
        
        #region IMGUI
        [MethodImpl(MethodImplOptions.InternalCall)]
        public extern static bool imgui_Begin(string name, int flags);
        [MethodImpl(MethodImplOptions.InternalCall)]
        public extern static void imgui_Begin_OpenFlagSet(string name, bool value);
        [MethodImpl(MethodImplOptions.InternalCall)]
        public extern static bool imgui_Begin_OpenFlagGet(string name);
        [MethodImpl(MethodImplOptions.InternalCall)]
        public extern static void imgui_Button(string name);
        [MethodImpl(MethodImplOptions.InternalCall)]
        public extern static void imgui_End();
        #endregion
        #endregion

    }




    enum ImGuiWindowFlags
    {
        ImGuiWindowFlags_None = 0,
        ImGuiWindowFlags_NoTitleBar = 1 << 0,   // Disable title-bar
        ImGuiWindowFlags_NoResize = 1 << 1,   // Disable user resizing with the lower-right grip
        ImGuiWindowFlags_NoMove = 1 << 2,   // Disable user moving the window
        ImGuiWindowFlags_NoScrollbar = 1 << 3,   // Disable scrollbars (window can still scroll with mouse or programmatically)
        ImGuiWindowFlags_NoScrollWithMouse = 1 << 4,   // Disable user vertically scrolling with mouse wheel. On child window, mouse wheel will be forwarded to the parent unless NoScrollbar is also set.
        ImGuiWindowFlags_NoCollapse = 1 << 5,   // Disable user collapsing window by double-clicking on it. Also referred to as "window menu button" within a docking node.
        ImGuiWindowFlags_AlwaysAutoResize = 1 << 6,   // Resize every window to its content every frame
        ImGuiWindowFlags_NoBackground = 1 << 7,   // Disable drawing background color (WindowBg, etc.) and outside border. Similar as using SetNextWindowBgAlpha(0.0f).
        ImGuiWindowFlags_NoSavedSettings = 1 << 8,   // Never load/save settings in .ini file
        ImGuiWindowFlags_NoMouseInputs = 1 << 9,   // Disable catching mouse, hovering test with pass through.
        ImGuiWindowFlags_MenuBar = 1 << 10,  // Has a menu-bar
        ImGuiWindowFlags_HorizontalScrollbar = 1 << 11,  // Allow horizontal scrollbar to appear (off by default). You may use SetNextWindowContentSize(ImVec2(width,0.0f)); prior to calling Begin() to specify width. Read code in imgui_demo in the "Horizontal Scrolling" section.
        ImGuiWindowFlags_NoFocusOnAppearing = 1 << 12,  // Disable taking focus when transitioning from hidden to visible state
        ImGuiWindowFlags_NoBringToFrontOnFocus = 1 << 13,  // Disable bringing window to front when taking focus (e.g. clicking on it or programmatically giving it focus)
        ImGuiWindowFlags_AlwaysVerticalScrollbar = 1 << 14,  // Always show vertical scrollbar (even if ContentSize.y < Size.y)
        ImGuiWindowFlags_AlwaysHorizontalScrollbar = 1 << 15,  // Always show horizontal scrollbar (even if ContentSize.x < Size.x)
        ImGuiWindowFlags_AlwaysUseWindowPadding = 1 << 16,  // Ensure child windows without border uses style.WindowPadding (ignored by default for non-bordered child windows, because more convenient)
        ImGuiWindowFlags_NoNavInputs = 1 << 18,  // No gamepad/keyboard navigation within the window
        ImGuiWindowFlags_NoNavFocus = 1 << 19,  // No focusing toward this window with gamepad/keyboard navigation (e.g. skipped by CTRL+TAB)
        ImGuiWindowFlags_UnsavedDocument = 1 << 20,  // Display a dot next to the title. When used in a tab/docking context, tab is selected when clicking the X + closure is not assumed (will wait for user to stop submitting the tab). Otherwise closure is assumed when pressing the X, so if you keep submitting the tab may reappear at end of tab bar.
        ImGuiWindowFlags_NoDocking = 1 << 21,  // Disable docking of this window

        ImGuiWindowFlags_NoNav = ImGuiWindowFlags_NoNavInputs | ImGuiWindowFlags_NoNavFocus,
        ImGuiWindowFlags_NoDecoration = ImGuiWindowFlags_NoTitleBar | ImGuiWindowFlags_NoResize | ImGuiWindowFlags_NoScrollbar | ImGuiWindowFlags_NoCollapse,
        ImGuiWindowFlags_NoInputs = ImGuiWindowFlags_NoMouseInputs | ImGuiWindowFlags_NoNavInputs | ImGuiWindowFlags_NoNavFocus,

        // [Internal]
        ImGuiWindowFlags_NavFlattened = 1 << 23,  // [BETA] Allow gamepad/keyboard navigation to cross over parent border to this child (only use on child that have no scrolling!)
        ImGuiWindowFlags_ChildWindow = 1 << 24,  // Don't use! For internal use by BeginChild()
        ImGuiWindowFlags_Tooltip = 1 << 25,  // Don't use! For internal use by BeginTooltip()
        ImGuiWindowFlags_Popup = 1 << 26,  // Don't use! For internal use by BeginPopup()
        ImGuiWindowFlags_Modal = 1 << 27,  // Don't use! For internal use by BeginPopupModal()
        ImGuiWindowFlags_ChildMenu = 1 << 28,  // Don't use! For internal use by BeginMenu()
        ImGuiWindowFlags_DockNodeHost = 1 << 29   // Don't use! For internal use by Begin()/NewFrame()

        // [Obsolete]
        //ImGuiWindowFlags_ResizeFromAnySide    = 1 << 17,  // --> Set io.ConfigWindowsResizeFromEdges=true and make sure mouse cursors are supported by backend (io.BackendFlags & ImGuiBackendFlags_HasMouseCursors)
    };

    public interface IMQ
    {
        T Query<T>(string query);
        void Cmd(string query);
        void Write(string query, [CallerMemberName] string memberName = "", [CallerFilePath] string fileName = "", [CallerLineNumber] int lineNumber = 0);
        void TraceStart(string methodName);
        void TraceEnd(string methodName);
        void Delay(Int32 value);
        Boolean Delay(Int32 maxTimeToWait, string Condition);
        void Broadcast(string query);
        bool AddCommand(string query);
        void ClearCommands();
        void RemoveCommand(string commandName);

    }
    public class MQ : IMQ
    {   //**************************************************************************************************
        //NONE OF THESE METHODS SHOULD BE CALLED ON THE C++ Thread, as it will cause a deadlock due to delay calls
        //**************************************************************************************************

        public static Int64 _maxMillisecondsToWork = 40;
        public static Int64 _sinceLastDelay = 0;
        public static Int64 _totalQueryCounts;
        public T Query<T>(string query)
        {
            _totalQueryCounts++;
            Int64 elapsedTime = Core._stopWatch.ElapsedMilliseconds;
            Int64 differenceTime = Core._stopWatch.ElapsedMilliseconds - _sinceLastDelay;


            if (_maxMillisecondsToWork < differenceTime)
            {
                Delay(0);
            }
            string mqReturnValue = Core.mq_ParseTLO(query);
            if (typeof(T) == typeof(Int32))
            {
                if (!mqReturnValue.Contains("."))
                {
                    Int32 value;
                    if (Int32.TryParse(mqReturnValue, out value))
                    {
                        return (T)(object)value;
                    }
                }
                else
                {
                    Decimal value;
                    if (decimal.TryParse(mqReturnValue, out value))
                    {
                        return (T)(object)value;
                    }

                }
            }
            else if (typeof(T) == typeof(Boolean))
            {
                Boolean booleanValue;
                if (Boolean.TryParse(mqReturnValue, out booleanValue))
                {
                    return (T)(object)booleanValue;
                }
                if (mqReturnValue == "NULL")
                {
                    return (T)(object)false;
                }
                if (mqReturnValue == "!FALSE")
                {
                    return (T)(object)true;
                }
                if (mqReturnValue == "!TRUE")
                {
                    return (T)(object)false;
                }
                Int32 intValue;
                if (Int32.TryParse(mqReturnValue, out intValue))
                {
                    if (intValue > 0)
                    {
                        return (T)(object)true;
                    }
                    return (T)(object)false;
                }
                if (string.IsNullOrWhiteSpace(mqReturnValue))
                {
                    return (T)(object)false;
                }

                return (T)(object)true;


            }
            else if (typeof(T) == typeof(string))
            {
                return (T)(object)mqReturnValue;
            }
            else if (typeof(T) == typeof(decimal))
            {
                Decimal value;
                if (Decimal.TryParse(mqReturnValue, out value))
                {
                    return (T)(object)value;
                }
            }
            else if (typeof(T) == typeof(double))
            {
                double value;
                if (double.TryParse(mqReturnValue, out value))
                {
                    return (T)(object)value;
                }
            }
            else if (typeof(T) == typeof(Int64))
            {
                Int64 value;
                if (Int64.TryParse(mqReturnValue, out value))
                {
                    return (T)(object)value;
                }
            }


            return default(T);

        }
        public void Cmd(string query)
        {
            Int64 elapsedTime = Core._stopWatch.ElapsedMilliseconds;
            Int64 differenceTime = Core._stopWatch.ElapsedMilliseconds - _sinceLastDelay;


            if (_maxMillisecondsToWork < differenceTime)
            {
                Delay(0);
            }
            //delays are not valid commands
            if (query.StartsWith("/delay", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            Core._log.Write($"Sending command to EQ:{query}");

            Core._currentCommand = query;
            Core._coreResetEvent.Set();
            //we are now going to wait on the core
            MainProcessor._processResetEvent.Wait();
            MainProcessor._processResetEvent.Reset();
        }

        public void Broadcast(string query)
        {
            Cmd($"/bc {query}");
        }


        public void Write(string query, [CallerMemberName] string memberName = "", [CallerFilePath] string fileName = "", [CallerLineNumber] int lineNumber = 0)
        {
            //if(String.IsNullOrWhiteSpace(query))
            //{
            //    return;
            //}
            //check
            //if (_sinceLastDelay + _maxMillisecondsToWork < Core._stopWatch.ElapsedMilliseconds)
            //{
            //    Delay(0);
            //}
            //Core.mq_Echo(query);
            //set the buffer for the C++ thread
            Core._currentWrite = $"[{MainProcessor._applicationName}][{System.DateTime.Now.ToString("HH:mm:ss")}] {query}";
            //swap to the C++thread, and it will swap back after executing the current write becau of us setting _CurrentWrite before
            Core._coreResetEvent.Set();
            //we are now going to wait on the core
            MainProcessor._processResetEvent.Wait();
            MainProcessor._processResetEvent.Reset();

        }

        public void TraceStart(string methodName)
        {
            if (String.IsNullOrWhiteSpace(methodName))
            {
                return;
            }
            this.Write($"|- {methodName} ==>");
        }
        public void TraceEnd(string methodName)
        {
            if (String.IsNullOrWhiteSpace(methodName))
            {
                return;
            }
            this.Write($"<== {methodName} -|");
        }
        public void Delay(Int32 value)
        {
            if (value > 0)
            {
                Core._delayStartTime = Core._stopWatch.ElapsedMilliseconds;
                Core._delayTime = value;
                Core._currentDelay = value;//tell the C++ thread to send out a delay update
            }

            //lets tell core that it can continue
            Core._coreResetEvent.Set();
            //we are now going to wait on the core
            MainProcessor._processResetEvent.Wait();
            MainProcessor._processResetEvent.Reset();

            if(!Core._isProcessing)
            {
                //we are terminating, kill this thread
                throw new ThreadAbort("Terminating thread");
            }

            _sinceLastDelay = Core._stopWatch.ElapsedMilliseconds;
        }

        public Boolean Delay(Int32 maxTimeToWait, string Condition)
        {
            Condition = $"${{If[{Condition},TRUE,FALSE]}}";
            Int64 startingTime = Core._stopWatch.ElapsedMilliseconds;
            while (!this.Query<bool>(Condition))
            {
                if (Core._stopWatch.ElapsedMilliseconds - startingTime > maxTimeToWait)
                {
                    return false;
                }
                this.Delay(10);
            }
            return true;
        }
        public bool AddCommand(string commandName)
        {
            return Core.mq_AddCommand(commandName);
        }
        public void ClearCommands()
        {
            Core.mq_ClearCommands();
        }
        public void RemoveCommand(string commandName)
        {
            Core.mq_RemoveCommand(commandName);
        }

    }

    public class Logging
    {
        public static LogLevels _traceLogLevel = LogLevels.None;
        public static LogLevels _minLogLevelTolog = LogLevels.Debug;
        public static LogLevels _defaultLogLevel = LogLevels.Debug;
        private static ConcurrentDictionary<String, String> _classLookup = new ConcurrentDictionary<string, string>();
        public static IMQ MQ = Core.mqInstance;

        public Logging(IMQ mqInstance)
        {
            MQ = mqInstance;
        }
        public void Write(string message, LogLevels logLevel = LogLevels.Default, string eventName = "Logging", [CallerMemberName] string memberName = "", [CallerFilePath] string fileName = "", [CallerLineNumber] int lineNumber = 0, Dictionary<String, String> headers = null)
        {

            if (logLevel == LogLevels.Default)
            {
                logLevel = _defaultLogLevel;
            }

            WriteStatic(message, logLevel, eventName, memberName, fileName, lineNumber, headers);

        }

        public static void WriteStatic(string message, LogLevels logLevel = LogLevels.Info, string eventName = "Logging", [CallerMemberName] string memberName = "", [CallerFilePath] string fileName = "", [CallerLineNumber] int lineNumber = 0, Dictionary<String, String> headers = null)
        {
            if ((Int32)logLevel < (Int32)_minLogLevelTolog)
            {
                return;//log level is too low to currently log. 
            }



            string className = GetClassName(fileName);

            if (logLevel == LogLevels.CriticalError)
            {
                eventName += "._CriticalError_";
            }

            if (logLevel == LogLevels.Debug)
            {
                MQ.Write($"{className}:{memberName}:({lineNumber}) {message}", "", "Logging");

            }
            else
            {
                MQ.Write($"{message}");
            }

        }
        public ITrace Trace(string name = "", [CallerMemberName] string memberName = "", [CallerFilePath] string fileName = "", [CallerLineNumber] int lineNumber = 0)
        {

            BaseTrace returnValue = BaseTrace.Aquire();

            if (_traceLogLevel != LogLevels.Trace)
            {
                //if not debugging don't log stuff
                returnValue.CallBackDispose = TraceSetTime;
                return returnValue;
            }

            string className = GetClassName(fileName);
            returnValue.Class = className;
            returnValue.Method = memberName;
            returnValue.CallBackDispose = TraceSetTime;
            returnValue.Name = name;

            //done at the very last of this
            returnValue.StartTime = Core._stopWatch.Elapsed.TotalMilliseconds;
            if (!string.IsNullOrWhiteSpace(name))
            {
                MQ.TraceEnd($"{name}:{memberName})");
            }
            else
            {
                MQ.TraceStart(memberName);
            }

            return returnValue;

        }
        private void TraceSetTime(ITrace value)
        {
            double totalMilliseconds = 0;
            //done first!
            totalMilliseconds = Core._stopWatch.Elapsed.TotalMilliseconds - value.StartTime;
            //put event back into its object pool.
            if (!string.IsNullOrWhiteSpace(value.Method))
            {
                MQ.TraceEnd($"{value.Name}:{value.Method}({totalMilliseconds}ms)");
            }

        }
        public enum LogLevels
        {
            None = 0,
            Trace = 2000,
            Debug = 30000,
            Info = 40000,
            Error = 70000,
            CriticalError = 90000,
            Default = 99999
        }
        private static String GetClassName(string fileName)
        {
            string className;
            if (!_classLookup.ContainsKey(fileName))
            {
                if (!String.IsNullOrWhiteSpace(fileName))
                {
                    string[] tempArray = fileName.Split('\\');
                    className = tempArray[tempArray.Length - 1];
                    className = className.Replace(".cs", String.Empty).Replace(".vb", String.Empty);
                    _classLookup.TryAdd(fileName, className);

                }
                else
                {
                    _classLookup.TryAdd(fileName, "Unknown/ErrorGettingClass");
                }
            }
            className = _classLookup[fileName];
            return className;
        }
        public interface ITrace : IDisposable
        {
            String Name { get; set; }
            Int64 MetricID { get; set; }
            Double Value { get; set; }
            Double StartTime { get; set; }
            String Class { get; set; }
            String Method { get; set; }
            LogLevels LogLevel { get; set; }
            Action<ITrace> CallBackDispose { get; set; }
        }
        public class BaseTrace : ITrace
        {
            public string Name { get; set; }
            public Int64 MetricID { get; set; }

            public Double Value { get; set; }
            public Double StartTime { get; set; }
            public Action<ITrace> CallBackDispose { get; set; }
            public String Class { get; set; }
            public String Method { get; set; }
            public LogLevels LogLevel { get; set; }

            #region objectPoolingStuff


            //private constructor, needs to be created so that you are forced to use the pool.
            private BaseTrace() {

            }


            public static BaseTrace Aquire()
            {
                BaseTrace obj;
                if (!StaticObjectPool.TryPop<BaseTrace>(out obj))
                {
                    obj = new BaseTrace();
                }

                return obj;
            }

            public void Dispose()
            {
                if (CallBackDispose != null)
                {
                    CallBackDispose.Invoke(this); //this should null out the CallbackDispose so the normal dispose can then run.
                }

                ResetObject();

                StaticObjectPool.Push(this);
            }
            private void ResetObject()
            {

                this.StartTime = 0;
                this.Value = 0;
                this.CallBackDispose = null;
                this.Class = null;
                this.Method = null;
                this.Name = String.Empty;

            }
            ~BaseTrace()
            {
                //DO NOT CALL DISPOSE FROM THE FINALIZER! This should only ever be used in using statements
                //if this is called, it will cause the domain to hang in the GC when shuttind down
                //This is only here to warn you

            }

            #endregion
        }
    }

    public static class StaticObjectPool
    {
        private static class Pool<T>
        {
            private static readonly Stack<T> pool = new Stack<T>();

            public static void Push(T obj)
            {
                lock (pool)
                {
                    pool.Push(obj);
                }
            }

            public static bool TryPop(out T obj)
            {
                lock (pool)
                {
                    if (pool.Count > 0)
                    {
                        obj = pool.Pop();
                        return true;
                    }
                }
                obj = default(T);
                return false;
            }
        }

        public static void Push<T>(T obj)
        {
            Pool<T>.Push(obj);
        }

        public static bool TryPop<T>(out T obj)
        {
            return Pool<T>.TryPop(out obj);
        }

        public static T PopOrDefault<T>()
        {
            T ret;
            TryPop(out ret);
            return ret;
        }

        public static T PopOrNew<T>()
            where T : new()
        {
            T ret;
            return TryPop(out ret) ? ret : new T();
        }
    }
    //made an instance one to deal with host/port combos
    public class ObjectPool<T>
    {

        private Pool<T> _poolInstance = new Pool<T>();

        public void Push(T obj)
        {
            _poolInstance.Push(obj);
        }

        public bool TryPop(out T obj)
        {
            return _poolInstance.TryPop(out obj);
        }

        public T PopOrDefault()
        {
            T ret;
            TryPop(out ret);
            return ret;
        }
        public Int32 Count()
        {
            return _poolInstance.Count();
        }

        public T PopOrNew()
        {
            T ret;
            return TryPop(out ret) ? ret : default(T);
        }
    }
    class Pool<T>
    {
        private readonly Stack<T> pool = new Stack<T>();

        public void Push(T obj)
        {
            lock (pool)
            {
                pool.Push(obj);
            }
        }
        public Int32 Count()
        {
            lock (pool)
            {
                return pool.Count;
            }
        }
        public bool TryPop(out T obj)
        {
            lock (pool)
            {
                if (pool.Count > 0)
                {
                    obj = pool.Pop();
                    return true;
                }
            }
            obj = default(T);
            return false;
        }
    }


    public interface ISpawns
    {

        IEnumerable<Spawn> Get();
        void RefreshList();
        bool TryByID(Int32 id, out Spawn s);
        bool TryByName(string name,out Spawn s);
        Int32 GetIDByName(string name);
        bool Contains(string name);
        bool Contains(Int32 id);
       
    }

    public class Spawns: ISpawns
    {
        //special list so we can get rid of the non dirty values
        private static List<Spawn> _tmpSpawnList = new List<Spawn>();
        
        public static List<Spawn> _spawns = new List<Spawn>(2048);
        public static Dictionary<string,Spawn> _spawnsByName = new Dictionary<string,Spawn>(2048);
        public static Dictionary<Int32,Spawn> _spawnsByID = new Dictionary<int,Spawn>(2048);
        public static Int64 _lastRefesh = 0;
        public static Int64 _refreshTimePeriodInMS = 1000;


        public bool TryByID(Int32 id, out Spawn s)
        {
            RefreshListIfNeeded();
            return _spawnsByID.TryGetValue(id,out s);
        }
        public bool TryByName(string name, out Spawn s)
        {
            RefreshListIfNeeded();
            return _spawnsByName.TryGetValue(name, out s);
        }
        public Int32 GetIDByName(string name)
        {
            RefreshListIfNeeded();
            Spawn returnValue;
            if (_spawnsByName.TryGetValue(name, out returnValue))
            {
                return returnValue.ID;
            }
            return 0;
        }
        public bool Contains(string name)
        {
            RefreshListIfNeeded();
            return _spawnsByName.ContainsKey(name);
        }
        public bool Contains(Int32 id)
        {
            RefreshListIfNeeded();
            return _spawnsByID.ContainsKey(id);
        }
        public IEnumerable<Spawn> Get()
        {
            RefreshListIfNeeded();
            return _spawns;
        }

        private void RefreshListIfNeeded()
        {
            if(_spawns.Count==0)
            {
                RefreshList();
                return;
            }
            if (Core._stopWatch.ElapsedMilliseconds - _lastRefesh > _refreshTimePeriodInMS)
            {
                RefreshList();
            }
        }
        public void RefreshList()
        {
            //need to mark everything not dirty so we know what get spawns gets us.
            foreach (var spawn in _spawns)
            {
                spawn.isDirty = false;
            }
            //request new spawns!
            Core.mq_GetSpawns();

            //spawns has new/updated data, get rid of the non dirty stuff.
            //can use the other dictionaries to help
            _spawnsByName.Clear();
            _spawnsByID.Clear();
            foreach (var spawn in _spawns)
            {
                if(spawn.isDirty)
                {
                    _tmpSpawnList.Add(spawn);
                    if(spawn.TypeDesc=="PC")
                    {
                        _spawnsByName.Add(spawn.Name, spawn);

                    }
                    _spawnsByID.Add(spawn.ID, spawn);
                }
                else
                {
                    spawn.Dispose();
                }
            }
            //swap the collections
            _spawns.Clear();
            List<Spawn> tmpPtr = _spawns;
            _spawns = _tmpSpawnList;
            _tmpSpawnList = tmpPtr;

            //clear the dictionaries and rebuild.
            
          
            //_spawns should have fresh data now!
            _lastRefesh = Core._stopWatch.ElapsedMilliseconds;

        }
    }

    public class Spawn: IDisposable
    {
        public byte[] _data = new byte[1024];
        public Int32 _dataSize;
        public bool isDirty = false;
        public static Spawn Aquire()
        {
            Spawn obj;
            if (!StaticObjectPool.TryPop<Spawn>(out obj))
            {
                obj = new Spawn();
            }

            return obj;
        }

        static Dictionary<string, string> _stringLookup = new Dictionary<string, string>();
       
        public void Init(byte[] data, Int32 length)
        {
            isDirty = true;
            //used for remote debug, to send the representastion of the data over.
            System.Buffer.BlockCopy(data, 0, _data, 0, length);
            _dataSize = length;
            //end of remote debug
        
            Int32 cb = 0;
            ID = BitConverter.ToInt32(data, cb);
            cb += 4;
            AFK = BitConverter.ToBoolean(data, cb);
            cb += 1;
            Aggressive = BitConverter.ToBoolean(data, cb);
            cb += 1;
            Anonymous = BitConverter.ToBoolean(data, cb);
            cb += 1;
            Blind = BitConverter.ToInt32(data, cb);
            cb += 4;
            BodyTypeID = BitConverter.ToInt32(data, cb);
            cb += 4;
            //bodytype desc
            int slength = BitConverter.ToInt32(data, cb);
            cb += 4;
            //to prevent GC from chruning from destroying long lived string, keep a small collection of them
            //change to byte key based dictionary for even better?
            string tstring = System.Text.Encoding.ASCII.GetString(data, cb, slength);
            if (!_stringLookup.TryGetValue(tstring, out BodyTypeDesc))
            {
                _stringLookup.Add(tstring, tstring);
                BodyTypeDesc = tstring;
            }
            cb += slength;
            Buyer = BitConverter.ToBoolean(data, cb);
            cb += 1;
            ClassID= BitConverter.ToInt32(data, cb);
            cb += 4;
            //cleanname
            slength = BitConverter.ToInt32(data, cb);
            cb += 4;
            tstring = System.Text.Encoding.ASCII.GetString(data, cb, slength);
            if(!_stringLookup.TryGetValue(tstring,out CleanName))
            {
                _stringLookup.Add(tstring, tstring);
                CleanName = tstring;
            }
            cb += slength;
            ConColorID = BitConverter.ToInt32(data, cb);
            cb += 4;
            CurrentEndurnace = BitConverter.ToInt32(data, cb);
            cb += 4;
            CurrentHPs = BitConverter.ToInt32(data, cb);
            cb += 4;
            CurrentMana = BitConverter.ToInt32(data, cb);
            cb += 4;
            Dead = BitConverter.ToBoolean(data, cb);
            cb += 1;
            //displayname
            slength = BitConverter.ToInt32(data, cb);
            cb += 4;
            tstring = System.Text.Encoding.ASCII.GetString(data, cb, slength);
            if (!_stringLookup.TryGetValue(tstring, out DiplayName))
            {
                _stringLookup.Add(tstring, tstring);
                DiplayName = tstring;
            }
            cb += slength;
            Ducking = BitConverter.ToBoolean(data, cb);
            cb += 1;
            Feigning = BitConverter.ToBoolean(data, cb);
            cb += 1;
            GenderID = BitConverter.ToInt32(data, cb);
            cb += 4;
            GM = BitConverter.ToBoolean(data, cb);
            cb += 1;
            GuildID = BitConverter.ToInt32(data, cb);
            cb += 4;
            Heading = BitConverter.ToSingle(data, cb);
            cb += 4;
            Height = BitConverter.ToSingle(data, cb);
            cb += 4;
    
            Invis = BitConverter.ToBoolean(data, cb);
            cb += 1;
            IsSummoned = BitConverter.ToBoolean(data, cb);
            cb += 1;
            Level = BitConverter.ToInt32(data, cb);
            cb += 4;
            Levitate = BitConverter.ToBoolean(data, cb);
            cb += 1;
            Linkdead = BitConverter.ToBoolean(data, cb);
            cb += 1;
            Look = BitConverter.ToSingle(data, cb);
            cb += 4;
            MasterID = BitConverter.ToInt32(data, cb);
            cb += 4;
            MaxEndurance = BitConverter.ToInt32(data, cb);
            cb += 4;
            MaxRange = BitConverter.ToSingle(data, cb);
            cb += 4;
            MaxRangeTo = BitConverter.ToSingle(data, cb);
            cb += 4;
            Mount = BitConverter.ToBoolean(data, cb);
            cb += 1;
            Moving = BitConverter.ToBoolean(data, cb);
            cb += 1;
            //name
            slength = BitConverter.ToInt32(data, cb);
            cb += 4;
            tstring = System.Text.Encoding.ASCII.GetString(data, cb, slength);
            if (!_stringLookup.TryGetValue(tstring, out Name))
            {
                _stringLookup.Add(tstring, tstring);
                Name = tstring;
            }
            cb += slength;
            Named = BitConverter.ToBoolean(data, cb);
            cb += 1;
            PctHps = BitConverter.ToInt32(data, cb);
            cb += 4;
            PctMana = BitConverter.ToInt32(data, cb);
            cb += 4;
            PetID = BitConverter.ToInt32(data, cb);
            cb += 4;
            PlayerState = BitConverter.ToInt32(data, cb);
            cb += 4;
            RaceID = BitConverter.ToInt32(data, cb);
            cb += 4;
            //RaceName
            slength = BitConverter.ToInt32(data, cb);
            cb += 4;
            tstring = System.Text.Encoding.ASCII.GetString(data, cb, slength);
            if (!_stringLookup.TryGetValue(tstring, out RaceName))
            {
                _stringLookup.Add(tstring, tstring);
                RaceName = tstring;
            }
            cb += slength;
            RolePlaying = BitConverter.ToBoolean(data, cb);
            cb += 1;
            Sitting = BitConverter.ToBoolean(data, cb);
            cb += 1;
            Sneaking = BitConverter.ToBoolean(data, cb);
            cb += 1;
            Standing = BitConverter.ToBoolean(data, cb);
            cb += 1;
            Stunned = BitConverter.ToBoolean(data, cb);
            cb += 1;
            //Suffix
            slength = BitConverter.ToInt32(data, cb);
            cb += 4;
            tstring = System.Text.Encoding.ASCII.GetString(data, cb, slength);
            if (!_stringLookup.TryGetValue(tstring, out Suffix))
            {
                _stringLookup.Add(tstring, tstring);
                Suffix = tstring;
            }
            cb += slength;
            Targetable = BitConverter.ToBoolean(data, cb);
            cb += 1;
            TargetOfTargetID = BitConverter.ToInt32(data, cb);
            cb += 4;
            Trader = BitConverter.ToBoolean(data, cb);
            cb += 1;
            //TypeDesc
            slength = BitConverter.ToInt32(data, cb);
            cb += 4;
            tstring = System.Text.Encoding.ASCII.GetString(data, cb, slength);
            if (!_stringLookup.TryGetValue(tstring, out TypeDesc))
            {
                _stringLookup.Add(tstring, tstring);
                TypeDesc = tstring;
            }
            cb += slength;
            Underwater = BitConverter.ToBoolean(data, cb);
            cb += 1;
            X = BitConverter.ToSingle(data, cb);
            cb += 4;
            Y = BitConverter.ToSingle(data, cb);
            cb += 4;
            Z = BitConverter.ToSingle(data, cb);
            cb += 4;
            playerX = BitConverter.ToSingle(data, cb);
            cb += 4;
            playerY = BitConverter.ToSingle(data, cb);
            cb += 4;
            playerZ = BitConverter.ToSingle(data, cb);
            cb += 4;
            DeityID = BitConverter.ToInt32(data, cb);
            cb += 4;


        }
        public Int32 DeityID;
        public float playerZ;
        public float playerY;
        public float playerX;
        public float Z;
        public float Y;
        public float X;
        public bool Underwater;
        public string TypeDesc = String.Empty;
        public bool Trader;
        public Int32 TargetOfTargetID;
        public bool Targetable;
        public String Suffix;
        public bool Stunned;
        public bool Standing;
        public bool Sneaking;
        public bool Sitting;
        public bool RolePlaying;
        public String RaceName;
        public Int32 RaceID;
        public Int32 PlayerState;
        public Int32 PetID;
        public Int32 PctMana;
        public Int32 PctHps;
        public bool Named;
        public string Name = String.Empty;
        public bool Moving;
        public bool Mount;
        public float MaxRangeTo;
        public float MaxRange;
        public Int32 MaxEndurance;
        public Int32 MasterID;
        public float Look;
        public bool Linkdead;
        public bool Levitate;
        public Int32 Level;
        public bool IsSummoned;
        public bool Invis;
        public Int32 ID;
        public float Height;
        public float Heading;
        public Int32 GuildID;
        public bool GM;
        public Int32 GenderID;
        public String Gender
        {
            get
            {
                return GetGender(GenderID);
            }
        }

        public bool Feigning;
        public bool Ducking;
        public string DiplayName = string.Empty;
        public bool Dead;
        public Int32 CurrentMana;
        public Int32 CurrentHPs;
        public Int32 CurrentEndurnace;
        public Int32 ConColorID;
        public String ConColor
        {
            get
            {
                return GetConColor(ConColorID);
            }
        }
        public string CleanName = String.Empty;
        public Int32 ClassID;
        public String ClassName 
        { 
            get {
                return ClassIDToName(ClassID);
            } 
        }
        public String ClassShortName
        {
            get
            {
                return ClassIDToShortName(ClassID);
            }
        }
        public bool Anonymous;
        public bool AFK;
        public bool Aggressive;
        public Int32 Blind;
        public Int32 BodyTypeID;
        public string BodyTypeDesc = String.Empty;
        public bool Buyer;
        public double Distance3D
        {
            get
            {
                return GetDistance3D();
            }
        }
        public double Distance
        {
            get
            {
                return GetDistance();
            }
        }
        private string GetConColor(Int32 ConColorID)
        {
            switch (ConColorID)
            {
                case 0x06:
                    return "GREY";
                case 0x02:
                    return "GREEN";
                case 0x12:
                    return "LIGHT BLUE";
                case 0x04:
                    return "BLUE";
                case 0x0a:
                    return "WHITE";
                case 0x0f:
                    return "YELLOW";
                case 0x0d:
                    return "RED";
                default:
                    return "RED";
            }
         
        }
        private string GetGender(Int32 genderID)
        {

            switch (genderID)
            {
                case 0:
                    return "male";
                case 1:
                    return "female";
                case 2:
                    return "neuter";
                case 3:
                    return "unknown";
            }
            return String.Empty;

        }
        private double GetDistance3D()
        {
            double dx = playerX - X;
            double dy = playerY - Y;
            double dz = playerZ - Z;

            return Math.Sqrt(dx * dx + dy * dy + dz * dz);
        }
        private double GetDistance()
        {
            double dx = X - playerX;
            double dy = Y - playerY;
            return Math.Sqrt(dx * dx + dy * dy);
        }
        private string ClassIDToShortName(Int32 classID)
        {
            switch (classID)
            {
                case 1:
                    return "WAR";
                case 2:
                    return "CLR";
                case 3:
                    return "PAL";
                case 4:
                    return "RNG";
                case 5:
                    return "SHD";
                case 6:
                    return "DRU";
                case 7:
                    return "MNK";
                case 8:
                    return "BRD";
                case 9:
                    return "ROG";
                case 10:
                    return "SHM";
                case 11:
                    return "NEC";
                case 12:
                    return "WIZ";
                case 13:
                    return "MAG";
                case 14:
                    return "ENC";
                case 15:
                    return "BST";
                case 16:
                    return "BER";
            }
            return String.Empty;
        }
        private string ClassIDToName(Int32 ClassID)
        {
            switch(ClassID)
            {
                case 1:
                    return "Warrior";
                case 2:
                    return "Cleric";
                case 3:
                    return "Paladin";
                case 4:
                    return "Ranger";
                case 5:
                    return "Shadowknight";
                case 6:
                    return "Druid";
                case 7:
                    return "Monk";
                case 8:
                    return "Bard";
                case 9:
                    return "Rogue";
                case 10:
                    return "Shaman";
                case 11:
                    return "Necromancer";
                case 12:
                    return "Wizard";
                case 13:
                    return "Mage";
                case 14:
                    return "Enchanter";
                case 15:
                    return "Beastlord";
                case 16:
                    return "Berserker";
            }

            return String.Empty;
        }

        public void Dispose()
        {
            _dataSize = 0;
            StaticObjectPool.Push(this);
        }
    }


    ///https://github.com/joaoportela/CircularBuffer-CSharp/blob/master/CircularBuffer/CircularBuffer.cs
    /// <inheritdoc/>
    /// <summary>
    /// Circular buffer.
    /// 
    /// When writing to a full buffer:
    /// PushBack -> removes this[0] / Front()
    /// PushFront -> removes this[Size-1] / Back()
    /// 
    /// this implementation is inspired by
    /// http://www.boost.org/doc/libs/1_53_0/libs/circular_buffer/doc/circular_buffer.html
    /// because I liked their interface.
    /// </summary>
    public class CircularBuffer<T> : IEnumerable<T>
        {
            private readonly T[] _buffer;

            /// <summary>
            /// The _start. Index of the first element in buffer.
            /// </summary>
            private int _start;

            /// <summary>
            /// The _end. Index after the last element in the buffer.
            /// </summary>
            private int _end;

            /// <summary>
            /// The _size. Buffer size.
            /// </summary>
            private int _size;

            /// <summary>
            /// Initializes a new instance of the <see cref="CircularBuffer{T}"/> class.
            /// 
            /// </summary>
            /// <param name='capacity'>
            /// Buffer capacity. Must be positive.
            /// </param>
            public CircularBuffer(int capacity)
                : this(capacity, new T[] { })
            {
            }

            /// <summary>
            /// Initializes a new instance of the <see cref="CircularBuffer{T}"/> class.
            /// 
            /// </summary>
            /// <param name='capacity'>
            /// Buffer capacity. Must be positive.
            /// </param>
            /// <param name='items'>
            /// Items to fill buffer with. Items length must be less than capacity.
            /// Suggestion: use Skip(x).Take(y).ToArray() to build this argument from
            /// any enumerable.
            /// </param>
            public CircularBuffer(int capacity, T[] items)
            {
                if (capacity < 1)
                {
                    throw new ArgumentException(
                        "Circular buffer cannot have negative or zero capacity.", nameof(capacity));
                }
                if (items == null)
                {
                    throw new ArgumentNullException(nameof(items));
                }
                if (items.Length > capacity)
                {
                    throw new ArgumentException(
                        "Too many items to fit circular buffer", nameof(items));
                }

                _buffer = new T[capacity];

                Array.Copy(items, _buffer, items.Length);
                _size = items.Length;

                _start = 0;
                _end = _size == capacity ? 0 : _size;
            }

            /// <summary>
            /// Maximum capacity of the buffer. Elements pushed into the buffer after
            /// maximum capacity is reached (IsFull = true), will remove an element.
            /// </summary>
            public int Capacity { get { return _buffer.Length; } }

            /// <summary>
            /// Boolean indicating if Circular is at full capacity.
            /// Adding more elements when the buffer is full will
            /// cause elements to be removed from the other end
            /// of the buffer.
            /// </summary>
            public bool IsFull
            {
                get
                {
                    return Size == Capacity;
                }
            }

            /// <summary>
            /// True if has no elements.
            /// </summary>
            public bool IsEmpty
            {
                get
                {
                    return Size == 0;
                }
            }

            /// <summary>
            /// Current buffer size (the number of elements that the buffer has).
            /// </summary>
            public int Size { get { return _size; } }

            /// <summary>
            /// Element at the front of the buffer - this[0].
            /// </summary>
            /// <returns>The value of the element of type T at the front of the buffer.</returns>
            public T Front()
            {
                ThrowIfEmpty();
                return _buffer[_start];
            }

            /// <summary>
            /// Element at the back of the buffer - this[Size - 1].
            /// </summary>
            /// <returns>The value of the element of type T at the back of the buffer.</returns>
            public T Back()
            {
                ThrowIfEmpty();
                return _buffer[(_end != 0 ? _end : Capacity) - 1];
            }

            /// <summary>
            /// Index access to elements in buffer.
            /// Index does not loop around like when adding elements,
            /// valid interval is [0;Size[
            /// </summary>
            /// <param name="index">Index of element to access.</param>
            /// <exception cref="IndexOutOfRangeException">Thrown when index is outside of [; Size[ interval.</exception>
            public T this[int index]
            {
                get
                {
                    if (IsEmpty)
                    {
                        throw new IndexOutOfRangeException(string.Format("Cannot access index {0}. Buffer is empty", index));
                    }
                    if (index >= _size)
                    {
                        throw new IndexOutOfRangeException(string.Format("Cannot access index {0}. Buffer size is {1}", index, _size));
                    }
                    int actualIndex = InternalIndex(index);
                    return _buffer[actualIndex];
                }
                set
                {
                    if (IsEmpty)
                    {
                        throw new IndexOutOfRangeException(string.Format("Cannot access index {0}. Buffer is empty", index));
                    }
                    if (index >= _size)
                    {
                        throw new IndexOutOfRangeException(string.Format("Cannot access index {0}. Buffer size is {1}", index, _size));
                    }
                    int actualIndex = InternalIndex(index);
                    _buffer[actualIndex] = value;
                }
            }

            /// <summary>
            /// Pushes a new element to the back of the buffer. Back()/this[Size-1]
            /// will now return this element.
            /// 
            /// When the buffer is full, the element at Front()/this[0] will be 
            /// popped to allow for this new element to fit.
            /// </summary>
            /// <param name="item">Item to push to the back of the buffer</param>
            public void PushBack(T item)
            {
                if (IsFull)
                {
                    _buffer[_end] = item;
                    Increment(ref _end);
                    _start = _end;
                }
                else
                {
                    _buffer[_end] = item;
                    Increment(ref _end);
                    ++_size;
                }
            }

            /// <summary>
            /// Pushes a new element to the front of the buffer. Front()/this[0]
            /// will now return this element.
            /// 
            /// When the buffer is full, the element at Back()/this[Size-1] will be 
            /// popped to allow for this new element to fit.
            /// </summary>
            /// <param name="item">Item to push to the front of the buffer</param>
            public void PushFront(T item)
            {
                if (IsFull)
                {
                    Decrement(ref _start);
                    _end = _start;
                    _buffer[_start] = item;
                }
                else
                {
                    Decrement(ref _start);
                    _buffer[_start] = item;
                    ++_size;
                }
            }

            /// <summary>
            /// Removes the element at the back of the buffer. Decreasing the 
            /// Buffer size by 1.
            /// </summary>
            public void PopBack()
            {
                ThrowIfEmpty("Cannot take elements from an empty buffer.");
                Decrement(ref _end);
                _buffer[_end] = default(T);
                --_size;
            }

            /// <summary>
            /// Removes the element at the front of the buffer. Decreasing the 
            /// Buffer size by 1.
            /// </summary>
            public void PopFront()
            {
                ThrowIfEmpty("Cannot take elements from an empty buffer.");
                _buffer[_start] = default(T);
                Increment(ref _start);
                --_size;
            }

            /// <summary>
            /// Clears the contents of the array. Size = 0, Capacity is unchanged.
            /// </summary>
            /// <exception cref="NotImplementedException"></exception>
            public void Clear()
            {
                // to clear we just reset everything.
                _start = 0;
                _end = 0;
                _size = 0;
                Array.Clear(_buffer, 0, _buffer.Length);
            }

            /// <summary>
            /// Copies the buffer contents to an array, according to the logical
            /// contents of the buffer (i.e. independent of the internal 
            /// order/contents)
            /// </summary>
            /// <returns>A new array with a copy of the buffer contents.</returns>
            public T[] ToArray()
            {
                T[] newArray = new T[Size];
                int newArrayOffset = 0;
                var segments = ToArraySegments();
                foreach (ArraySegment<T> segment in segments)
                {
                    Array.Copy(segment.Array, segment.Offset, newArray, newArrayOffset, segment.Count);
                    newArrayOffset += segment.Count;
                }
                return newArray;
            }

            /// <summary>
            /// Get the contents of the buffer as 2 ArraySegments.
            /// Respects the logical contents of the buffer, where
            /// each segment and items in each segment are ordered
            /// according to insertion.
            ///
            /// Fast: does not copy the array elements.
            /// Useful for methods like <c>Send(IList&lt;ArraySegment&lt;Byte&gt;&gt;)</c>.
            /// 
            /// <remarks>Segments may be empty.</remarks>
            /// </summary>
            /// <returns>An IList with 2 segments corresponding to the buffer content.</returns>
            public IList<ArraySegment<T>> ToArraySegments()
            {
                return new[] { ArrayOne(), ArrayTwo() };
            }

            #region IEnumerable<T> implementation
            /// <summary>
            /// Returns an enumerator that iterates through this buffer.
            /// </summary>
            /// <returns>An enumerator that can be used to iterate this collection.</returns>
            public IEnumerator<T> GetEnumerator()
            {
                var segments = ToArraySegments();
                foreach (ArraySegment<T> segment in segments)
                {
                    for (int i = 0; i < segment.Count; i++)
                    {
                        yield return segment.Array[segment.Offset + i];
                    }
                }
            }
            #endregion
            #region IEnumerable implementation
            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
            {
                return (System.Collections.IEnumerator)GetEnumerator();
            }
            #endregion

            private void ThrowIfEmpty(string message = "Cannot access an empty buffer.")
            {
                if (IsEmpty)
                {
                    throw new InvalidOperationException(message);
                }
            }

            /// <summary>
            /// Increments the provided index variable by one, wrapping
            /// around if necessary.
            /// </summary>
            /// <param name="index"></param>
            private void Increment(ref int index)
            {
                if (++index == Capacity)
                {
                    index = 0;
                }
            }

            /// <summary>
            /// Decrements the provided index variable by one, wrapping
            /// around if necessary.
            /// </summary>
            /// <param name="index"></param>
            private void Decrement(ref int index)
            {
                if (index == 0)
                {
                    index = Capacity;
                }
                index--;
            }

            /// <summary>
            /// Converts the index in the argument to an index in <code>_buffer</code>
            /// </summary>
            /// <returns>
            /// The transformed index.
            /// </returns>
            /// <param name='index'>
            /// External index.
            /// </param>
            private int InternalIndex(int index)
            {
                return _start + (index < (Capacity - _start) ? index : index - Capacity);
            }

            // doing ArrayOne and ArrayTwo methods returning ArraySegment<T> as seen here: 
            // http://www.boost.org/doc/libs/1_37_0/libs/circular_buffer/doc/circular_buffer.html#classboost_1_1circular__buffer_1957cccdcb0c4ef7d80a34a990065818d
            // http://www.boost.org/doc/libs/1_37_0/libs/circular_buffer/doc/circular_buffer.html#classboost_1_1circular__buffer_1f5081a54afbc2dfc1a7fb20329df7d5b
            // should help a lot with the code.

            #region Array items easy access.
            // The array is composed by at most two non-contiguous segments, 
            // the next two methods allow easy access to those.

            private ArraySegment<T> ArrayOne()
            {
                if (IsEmpty)
                {
                    return new ArraySegment<T>(new T[0]);
                }
                else if (_start < _end)
                {
                    return new ArraySegment<T>(_buffer, _start, _end - _start);
                }
                else
                {
                    return new ArraySegment<T>(_buffer, _start, _buffer.Length - _start);
                }
            }

            private ArraySegment<T> ArrayTwo()
            {
                if (IsEmpty)
                {
                    return new ArraySegment<T>(new T[0]);
                }
                else if (_start < _end)
                {
                    return new ArraySegment<T>(_buffer, _end, 0);
                }
                else
                {
                    return new ArraySegment<T>(_buffer, 0, _end);
                }
            }

         
            #endregion
        }



        
    
}