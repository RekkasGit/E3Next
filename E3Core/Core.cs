using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
//This is used to setup internal calls
using System.Runtime.CompilerServices;
using System.Threading;
using System.Diagnostics;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;
//your using for your namespace for your object
using E3Core.Processors;



namespace MonoCore
{
    /// <summary>
    /// this is the class for the main C# thread
    //  the C++ core thread will call this in a task at startup
    /// </summary>
    public static class MainProcessor
    {
        public static IMQ MQ = Core.mqInstance;
        public static Int32 _processDelay = 5000;
        private static Logging _log = Core._log;
        private static string _applicationName = "e3";
       
        public static void Init()
        {
            ///DO YOUR INIT WORK HERE
            //WARNING , you may not be in game yet, so careful what queries you run on MQ.Query. May cause a crash.
            
            //current logging level for all _log.writes without specifying logging level
            Logging._defaultLogLevel = Logging.LogLevels.Debug;
            //how long before auto yielding back to C++/EQ/MQ on the next query/command/etc
            MonoCore.MQ._maxMillisecondsToWork = 40;
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

            //volatile variable, will eventually update to kill the thread on shutdown
            while (Core._isProcessing)
            {
               
                try
                {
                    using (_log.Trace())
                    {

                        //************************************************
                        //DO YOUR WORK HERE
                        //this loop executes once every OnPulse from C++
                        //************************************************
                        //just have a class call a process method or such so you don't have to see this 
                        //boiler plate code with all the threading.
                        E3.Process();
                        //***NOTE NOTE NOTE, Use M2.Delay(0) in your code to give control back to EQ if you have taken awhile in doing something
                        //***NOTE NOTE NOTE, generally this isn't needed as there is an auto yield baked into every MQ method. Just be aware.

                    }
                    //process all the events that have been registered
                    EventProcessor.ProcessEventsInQueues();
                }
                catch(Exception ex)
                {
                    _log.Write("Error: Please reload. Terminating. \r\nExceptionMessage:" + ex.Message + " stack:" + ex.StackTrace.ToString(), Logging.LogLevels.CriticalError);
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
        //this is the first queue that strings get put into, will be processed by its own thread
        public static ConcurrentQueue<String> _eventProcessingQueue = new ConcurrentQueue<String>();
        //if matches take place, they are placed in this queue for the main C# thread to process. 
        public static Int32 EventLimiterPerRegisteredEvent = 10;
        //this threads entire purpose, is to simply keep processing the event processing queue and place matches into
        //the eventfilteredqueue
        public static Task _regExProcessingTask;
        public static void Init()
        {
            _regExProcessingTask = Task.Factory.StartNew(() => { ProcessEventsIntoQueues(); }, CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);

        }
        /// <summary>
        /// Runs on its own thread, will process through all the strings passed in and then put them into the correct queue
        /// </summary>
        static void ProcessEventsIntoQueues()
        {
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
                            if (item.Value.queuedEvents.Count > EventLimiterPerRegisteredEvent)
                            {
                                continue;
                            }
                            var match = item.Value.regex.Match(line);
                            if (match.Success)
                            {
                                lock (item.Value.queuedEvents)
                                {
                                    item.Value.queuedEvents.Enqueue(new EventMatch() { eventName= item.Value.keyName, eventString = line, match = match });
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

                while (item.Value.queuedEvents.Count > 0)
                {

                    EventMatch line;
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
            if (_eventList.Count > 0 && _eventProcessingQueue.Count < 10)
            {
                _eventProcessingQueue.Enqueue(line);

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
            public System.Text.RegularExpressions.Regex regex;
            public System.Action<EventMatch> method;
            public ConcurrentQueue<EventMatch> queuedEvents = new ConcurrentQueue<EventMatch>();
        }
        public class EventMatch
        {
            public string eventString;
            public Match match;
            public string eventName;
        }
        public static void RegisterEvent(string keyName, string pattern, Action<EventMatch> method)
        {
            EventListItem eventToAdd = new EventListItem();
            eventToAdd.regex = new System.Text.RegularExpressions.Regex(pattern);
            eventToAdd.method = method;
            eventToAdd.keyName = keyName;

            _eventList.TryAdd(keyName, eventToAdd);

        }

    }
    //This class is for C++ thread to come in and call. for the most part, leave this alone. 
    public static class Core
    {
        public static IMQ mqInstance = new MQ(); //needs to be declared first
        public static Logging _log;
        public volatile static bool _isProcessing = false;
        public const string _coreVersion = "0.1";
       
        static Core()
        {
            _log = new Logging(mqInstance);
        }

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
            
            _stopWatch.Start();
            //do all necessary setups here
            MainProcessor.Init();
         
            //isProcessing needs to be true before the event processor has started
            _isProcessing = true;
            EventProcessor.Init();


            if (_taskThread == null)
            {
                //start up the main processor, this is where most of the C# code kicks off in
                _taskThread = Task.Factory.StartNew(() => { MainProcessor.Process(); }, CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
            }
            
        }
       
        public static void OnPulse()
        {
            if (!_isProcessing) return;
            //reset the last delay so we restart the procssing time since its a new OnPulse()
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


            //if (_stopWatch.ElapsedMilliseconds - millisecondsSinceLastPrint > 5000)
            //{
            //    use raw command, as we are on the C++thread, and don't want a delay hitting us
            //    Core.mq_Echo("[" + System.DateTime.Now + " Total Calls:" + _onPulseCalls);
            //    millisecondsSinceLastPrint = _stopWatch.ElapsedMilliseconds;
            //}
            
            RestartWait:
            //allow the processing thread to start its work.
            //and copy cache values to other cores so the thread can see the updated information in MQ
            MainProcessor._processResetEvent.Set();
            _coreResetEvent.Wait();
            _coreResetEvent.Reset();
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
                goto RestartWait;
            }
            if (_currentDelay > 0)
            {
                Core.mq_Delay(_currentDelay);
                _currentDelay = 0;
            }


        }

        //Comment these out if you are not using events so that C++ doesn't waste time sending the string to C#
        //public static void OnWriteChatColor(string line)
        //{
        //    EventProcessor.ProcessEvent(line);
        //}
        public static void OnIncomingChat(string line)
        {
            EventProcessor.ProcessEvent(line);
        }
        public static void OnUpdateImGui()
        {

            if(imgui_Begin_OpenFlagGet("e3TestWindow"))
            {
                imgui_Begin("e3TestWindow", (int)ImGuiWindowFlags.ImGuiWindowFlags_None);
                imgui_Button("Test button");
                imgui_End();
            }
           

        }

        #region MQMethods
        [MethodImpl(MethodImplOptions.InternalCall)]
        public extern static void mq_Echo(string msg);
        [MethodImpl(MethodImplOptions.InternalCall)]
        public extern static string mq_ParseTLO(string msg);
        [MethodImpl(MethodImplOptions.InternalCall)]
        public extern static void mq_DoCommand(string msg);
        [MethodImpl(MethodImplOptions.InternalCall)]
        public extern static void mq_Delay(int delay);
        #region IMGUI
        [MethodImpl(MethodImplOptions.InternalCall)]
        public extern static bool imgui_Begin(string name,int flags);
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
        void Write(string query, string colorcode = "");
        void TraceStart(string methodName);
        void TraceEnd(string methodName);
        void Delay(Int32 value);
        Boolean Delay(Int32 maxTimeToWait, string Condition);
        void Broadcast(string query);

    }
    public class MQ:IMQ
    {   //**************************************************************************************************
        //NONE OF THESE METHODS SHOULD BE CALLED ON THE C++ Thread, as it will cause a deadlock due to delay calls
        //**************************************************************************************************

        public static Int64 _maxMillisecondsToWork = 500;
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
                Int32 value;
                if (Int32.TryParse(mqReturnValue, out value))
                {
                    return (T)(object)value;
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

        public void Write(string query, string colorcode = "\at")
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
            Core._currentWrite = colorcode + query;
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
            _sinceLastDelay = Core._stopWatch.ElapsedMilliseconds;
        }

        public Boolean Delay(Int32 maxTimeToWait, string Condition)
        {
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


    }

    public class Logging
    {
        public static LogLevels _currentLogLevel = LogLevels.None;
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

            if(logLevel == LogLevels.Debug)
            {
                MQ.Write($"{className}:{memberName}:({lineNumber}) {message}");

            }
            else
            {
                MQ.Write($"{message}");
            }

        }
        public ITrace Trace(string name="",[CallerMemberName] string memberName = "", [CallerFilePath] string fileName = "", [CallerLineNumber] int lineNumber = 0)
        {
            BaseTrace returnValue = BaseTrace.Aquire();
            if (_currentLogLevel== LogLevels.None)
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
            if(!string.IsNullOrWhiteSpace(value.Method))
            {
                MQ.TraceEnd($"{value.Name}:{value.Method}({totalMilliseconds}ms)");
            }
          
        }
        public enum LogLevels
        {
            None = 0,
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
            //note, can't really make a base class for this stuff because of how
            //generics work in .net :\ sucks but this is mostly copy n paste code, not exactly hard
            // flipped values to make even uninitialised
            // objects behave correctly when finalizer is run
            private const int disposedFalse = 1;
            private const int disposedTrue = 0;

            // changed default flag value
            private int disposed = disposedTrue;

            //private constructor, needs to be created so that you are forced to use the pool.
            private BaseTrace() { }


            public static BaseTrace Aquire()
            {
                BaseTrace obj;
                if (!StaticObjectPool.TryPop<BaseTrace>(out obj))
                {
                    obj = new BaseTrace();
                }
                obj.disposed = disposedFalse;
                obj.MetricID = Guid64.NewGuid();
                return obj;
            }

            public void Dispose()
            {
                if (CallBackDispose != null)
                {
                    CallBackDispose.Invoke(this); //this should null out the CallbackDispose so the normal dispose can then run.
                    return;
                }

                if (Interlocked.Exchange(
                    ref this.disposed, disposedTrue
                    ) == disposedTrue)
                    return;

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
    public class Guid64
    {
        //UTC time parse, so we don't have to deal with time zones
        private static System.DateTime _startYear = System.DateTime.Parse("1/1/2022 00:00:00Z");
        private static Object _stopWatchLock = new object();
        private static System.Diagnostics.Stopwatch _stopWatch;
        private static System.TimeSpan _timeSpanSince;
        //Unique byte added to the end of the Int64, normally this is the IP Octect, but it is configurable.
        private static Byte _uniqueByte = 0;

        //Unique byte 2 is used to deal with time drift. 
        //Computer time is not a constant and can change and be resynced.  
        //due to the quartz crystal in the computer not being exact. We will swap between these unique bytes to deal
        //with this time drift.
        private static Byte _uniqueByte2 = 0;

        private static System.TimeSpan _timeToResync = new System.TimeSpan(0, 24, 0, 0, 0);
        private static System.DateTime _timeLastResynced;

        //This is necessary because as CPU's get faster, we can Enter Lock, Create ID and Exit lock faster than 1/10th of a micro second. So we if we get the same one, we redo it
        private static Int64 _lastIDCreated = 0;


        public static System.TimeSpan TimeToResync { get { return _timeToResync; } set { _timeToResync = value; } }
        private static Boolean _isByteTwoSet = false;
        public static Byte UniqueByte
        {
            get { return _uniqueByte; }
            set
            {
                _uniqueByte = value;
                if (!_isByteTwoSet)
                {
                    //Keep in the byte range, go up past the 128. 
                    //so if unique byte (normally ip octect) is 10, next value would be 138. gives us 128 severs to play with without possible clashing. Normally
                    //your Load balancer Virtual servers would be in the upper range anyway so unlikly to clash with another server with careful planning
                    _uniqueByte2 = (Byte)(_uniqueByte2 + 128 % 256);
                }
            }
        }
        public static Byte UniqueByte2
        {
            get { return _uniqueByte2; }
            set
            {
                _uniqueByte2 = value;
                _isByteTwoSet = true;
            }
        }

        static Guid64()
        {
            _startYear = _startYear.ToUniversalTime();
            System.Net.IPHostEntry localhost;
            System.Net.IPAddress[] ipAddresses;
            DateTime currentTime = System.DateTime.UtcNow;
            _timeLastResynced = new System.DateTime(currentTime.Year, currentTime.Month, currentTime.Day, 0, 0, 0);

            //less about locking as a static constructor is only called once and more
            //about proper Memory Barriers being applied 
            lock (_stopWatchLock)
            {

                localhost = System.Net.Dns.GetHostEntry(System.Net.Dns.GetHostName());
                ipAddresses = localhost.AddressList;

                //If we are IPV6, we cannot simply use the last 8 bytes, need to be aware of it.
                System.Net.IPAddress ipAddress = null;
                System.Net.IPAddress ipv6Address = null;

                foreach (System.Net.IPAddress tempAddress in ipAddresses)
                {
                    if (tempAddress.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                    {
                        //We found IPv4, break out.
                        ipAddress = tempAddress;
                        break;
                    }
                    if (tempAddress.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6)
                    {
                        //We found IPv4, set.
                        ipv6Address = tempAddress;
                    }
                }
                if (ipAddress != null)
                {
                    _uniqueByte = ipAddress.GetAddressBytes()[3];
                }
                else if (ipv6Address != null)
                {
                    _uniqueByte = ipv6Address.GetAddressBytes()[15];
                }
                else
                {
                    System.Random tempRandom = new Random();
                    Int32 tempInt = tempRandom.Next(0, 128);
                    _uniqueByte = System.BitConverter.GetBytes(tempInt)[0];
                }
                //Set unique byte two in case they don't set it This can be overridden though
                _uniqueByte2 = (Byte)((_uniqueByte + 128) % 256);

                _timeSpanSince = System.DateTime.UtcNow.Subtract(_startYear);
                _stopWatch = new System.Diagnostics.Stopwatch();
                _stopWatch.Start();

            }

        }

        public static Int64 NewGuid()
        {

            Int64 returnValue = 0;
            Byte[] bytes = new Byte[8];

            lock (_stopWatchLock)
            {
                //if it is time to resync time
                if (System.DateTime.UtcNow.Subtract(_timeLastResynced) > _timeToResync)
                {
                    System.DateTime currentTime = System.DateTime.UtcNow;
                    //Reset the time since 2012 so we can get a new value from the system
                    _timeSpanSince = System.DateTime.UtcNow.Subtract(_startYear);

                    if (_timeToResync.Seconds > 0)
                    {
                        _timeLastResynced = new System.DateTime(currentTime.Year, currentTime.Month, currentTime.Day, currentTime.Hour, currentTime.Minute, currentTime.Second);
                    }
                    else if (_timeToResync.Minutes > 0)
                    {
                        _timeLastResynced = new System.DateTime(currentTime.Year, currentTime.Month, currentTime.Day, currentTime.Hour, currentTime.Minute, 0);
                    }
                    else if (_timeToResync.Hours > 0)
                    {
                        _timeLastResynced = new System.DateTime(currentTime.Year, currentTime.Month, currentTime.Day, currentTime.Hour, 0, 0);
                    }
                    else
                    {
                        _timeLastResynced = new System.DateTime(currentTime.Year, currentTime.Month, currentTime.Day, 0, 0, 0);
                    }

                    _stopWatch.Restart();
                    //Swap bytes
                    Byte tempByte = _uniqueByte;
                    _uniqueByte = _uniqueByte2;
                    _uniqueByte2 = tempByte;
                }

                //We may have to do this loop a few times to get an ID. 
                //a the question is, is a SpinWait better. Sleep is far too long.
                while (_lastIDCreated == returnValue || returnValue == 0)
                {
                    Double totalSeconds = _timeSpanSince.TotalSeconds;
                    Int64 elapsedTicks = _stopWatch.ElapsedTicks;
                    Int64 frequency = System.Diagnostics.Stopwatch.Frequency;
                    Double tickFreq = (double)elapsedTicks / (double)frequency;  //Extremely important to make sure both of these are doubles to not lose information
                    Double secTickFreq = totalSeconds + (tickFreq);

                    returnValue = (Int64)(secTickFreq * 10000000);

                }
                _lastIDCreated = returnValue;
            }
            //Now to bitshift the values over to make room for 1 byte uniqueness
            returnValue = returnValue << 8;
            //put the byte at the end
            returnValue = returnValue | _uniqueByte;

            return returnValue;

        }

        public static System.DateTime GetDate(Int64 guid64ID)
        {

            //Shift out the unique byte, multiply by 10,000,000 to get back into seconds
            return _startYear.AddSeconds((guid64ID >> 8) / 10000000);
        }

        public static Int64 GetGuid(DateTime date)
        {
            Double totalSeconds = date.Subtract(_startYear).TotalSeconds;
            Int64 returnValue;
            returnValue = (Int64)(totalSeconds * 10000000);
            returnValue = returnValue << 8;
            //put the byte at the end

            return returnValue;
        }

    }
    //http://genericgamedev.com/general/reusing-objects-with-generic-object-pooling/
    public class GenericPool<TSelf> : IDisposable where TSelf : GenericPool<TSelf>, new()
    {
        // flipped values to make even uninitialised
        // objects behave correctly when finalizer is run
        private const int disposedFalse = 1;
        private const int disposedTrue = 0;

        // changed default flag value
        private int disposed = disposedTrue;

        public static TSelf GetOne()
        {
            var obj = StaticObjectPool.PopOrNew<TSelf>();
            obj.disposed = disposedFalse;
            return obj;
        }

        public void Dispose()
        {
            if (Interlocked.Exchange(
                ref this.disposed, disposedTrue
                ) == disposedTrue)
                return;

            StaticObjectPool.Push((TSelf)this);
        }

        ~GenericPool()
        {
            this.Dispose();
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
}