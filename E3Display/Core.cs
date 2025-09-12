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
using System.Runtime.InteropServices;
using E3Display;
using System.Globalization;

namespace MonoCore
{
    public static class MainProcessor
    {
        public static Int32 ProcessDelay = 200;
        public static string ApplicationName = "";
        public static void Init()
        {
            Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");
        }

        // No separate thread; we run lightweight work from OnPulse with a timer throttle.
        internal static long _nextProcessAtMs = 0;
    }

    static public class EventProcessor
    {
        public static ConcurrentDictionary<string, Action<EventMatch>> _unfilteredEventMethodList = new ConcurrentDictionary<string, Action<EventMatch>>();
        public static ConcurrentDictionary<string, EventListItem> _unfilteredEventList = new ConcurrentDictionary<string, EventListItem>();
        public static ConcurrentDictionary<string, EventListItem> EventList = new ConcurrentDictionary<string, EventListItem>();
        public static ConcurrentDictionary<string, CommandListItem> CommandList = new ConcurrentDictionary<string, CommandListItem>();
        public static ConcurrentQueue<String> _eventProcessingQueue = new ConcurrentQueue<String>();
        public static ConcurrentQueue<String> _mqEventProcessingQueue = new ConcurrentQueue<string>();
        public static ConcurrentQueue<String> _mqCommandProcessingQueue = new ConcurrentQueue<string>();
        public static List<Regex> _filterRegexes = new List<Regex>();
        public static Int32 EventLimiterPerRegisteredEvent = 10;
        public static Task _regExProcessingTask;
        private static Boolean _isInit = false;
        public static void Init()
        {
            if (!_isInit)
            {
                _regExProcessingTask = Task.Factory.StartNew(() => { ProcessEventsIntoQueues(); }, CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
                _isInit = true;
            }
        }
        public static void ProcessEventsIntoQueues()
        {
            while (Core.IsProcessing)
            {
                if (_eventProcessingQueue.Count > 0)
                {
                    string line;
                    if (_eventProcessingQueue.TryDequeue(out line))
                    {
                        foreach (var ueventMethod in _unfilteredEventMethodList)
                        {
                            ueventMethod.Value.Invoke(new EventMatch() { eventName = ueventMethod.Key, eventString = line, typeOfEvent = eventType.EQEvent });
                        }
                        foreach (var uevent in _unfilteredEventList)
                        {
                            uevent.Value.queuedEvents.Enqueue(new EventMatch() { eventName = uevent.Value.keyName, eventString = line, typeOfEvent = eventType.EQEvent });
                        }
                        foreach (var item in EventList)
                        {
                            if (item.Value.queuedEvents.Count > EventLimiterPerRegisteredEvent)
                                continue;
                            item.Value.queuedEvents.Enqueue(new EventMatch() { eventName = item.Value.keyName, eventString = line, typeOfEvent = eventType.EQEvent });
                        }
                    }
                }
                if (_mqEventProcessingQueue.Count > 0)
                {
                    string line;
                    if (_mqEventProcessingQueue.TryDequeue(out line))
                    {
                        foreach (var ueventMethod in _unfilteredEventMethodList)
                        {
                            ueventMethod.Value.Invoke(new EventMatch() { eventName = ueventMethod.Key, eventString = line, typeOfEvent = eventType.MQEvent });
                        }
                        foreach (var uevent in _unfilteredEventList)
                        {
                            uevent.Value.queuedEvents.Enqueue(new EventMatch() { eventName = uevent.Value.keyName, eventString = line, typeOfEvent = eventType.MQEvent });
                        }
                    }
                }
                if (_mqCommandProcessingQueue.Count > 0)
                {
                    string line;
                    if (_mqCommandProcessingQueue.TryDequeue(out line))
                    {
                        foreach (var item in CommandList)
                        {
                            if (line.StartsWith(item.Value.keyName, StringComparison.OrdinalIgnoreCase))
                            {
                                item.Value.queuedCommands.Enqueue(new EventMatch() { eventName = item.Value.keyName, eventString = line, typeOfEvent = eventType.MQCommand });
                            }
                        }
                    }
                }
                Thread.Sleep(1);
            }
        }
        public static void ProcessEventsInQueues()
        {
            foreach (var eventItem in _unfilteredEventList)
            {
                int count = 0;
                while (eventItem.Value.queuedEvents.Count > 0 && count < EventLimiterPerRegisteredEvent)
                {
                    count++;
                    EventMatch match;
                    if (eventItem.Value.queuedEvents.TryDequeue(out match))
                        eventItem.Value.method.Invoke(match);
                }
            }
            foreach (var eventItem in EventList)
            {
                int count = 0;
                while (eventItem.Value.queuedEvents.Count > 0 && count < EventLimiterPerRegisteredEvent)
                {
                    count++;
                    EventMatch match;
                    if (eventItem.Value.queuedEvents.TryDequeue(out match))
                        eventItem.Value.method.Invoke(match);
                }
            }
            foreach (var cmd in CommandList)
            {
                int count = 0;
                while (cmd.Value.queuedCommands.Count > 0 && count < EventLimiterPerRegisteredEvent)
                {
                    count++;
                    EventMatch match;
                    if (cmd.Value.queuedCommands.TryDequeue(out match))
                        cmd.Value.method.Invoke(match);
                }
            }
        }
        public static void RegisterCommand(string commandLine, Action<EventMatch> method, string description = "")
        {
            CommandList.TryAdd(commandLine, new CommandListItem() { keyName = commandLine, method = method, description = description });
        }
        public static bool CommandListQueueHasCommand(string command)
        {
            CommandListItem item;
            if (CommandList.TryGetValue(command, out item))
                return item.queuedCommands.Count > 0;
            return false;
        }
        public class CommandListItem
        {
            public string keyName;
            public string description;
            public Action<EventMatch> method;
            public ConcurrentQueue<EventMatch> queuedCommands = new ConcurrentQueue<EventMatch>();
        }
        public class EventListItem
        {
            public string keyName;
            public Action<EventMatch> method;
            public ConcurrentQueue<EventMatch> queuedEvents = new ConcurrentQueue<EventMatch>();
        }
        public class EventMatch
        {
            public string eventName;
            public string eventString;
            public eventType typeOfEvent;
            public List<string> args = new List<string>();
        }
        public enum eventType
        {
            EQEvent,
            MQEvent,
            MQCommand
        }
    }

    public static partial class Core
    {
        public static bool IsProcessing = false;
        // Removed event handshakes to avoid deadlocks.
        public static Stopwatch StopWatch = new Stopwatch();
        public static decimal _MQ2MonoVersion = 0.0m;

        public static void OnInit()
        {
            try
            {
                StopWatch.Start();
                _MQ2MonoVersion = decimal.Parse(mq_GetMQ2MonoVersion(), CultureInfo.InvariantCulture);
                IsProcessing = true;
                EventProcessor.Init();
                MainProcessor.Init();
                MyCode.RegisterCommandBindings();
            }
            catch (Exception ex)
            {
                mq_Echo("Exception in OnInit: " + ex.Message);
                IsProcessing = false;
            }
        }

        public static void OnStop()
        {
            try
            {
                E3Display.MyCode.Stop();
            }
            catch { }
            IsProcessing = false;
        }

        public static void OnPulse()
        {
            if (!IsProcessing) return;
            try
            {
                var now = StopWatch.ElapsedMilliseconds;
                if (now >= MainProcessor._nextProcessAtMs)
                {
                    // Run a small slice of work
                    MyCode.Process();
                    EventProcessor.ProcessEventsInQueues();
                    // Throttle next run
                    MainProcessor._nextProcessAtMs = now + Math.Max(10, MainProcessor.ProcessDelay);
                }
            }
            catch (Exception ex)
            {
                mq_Echo("E3Display OnPulse error: " + ex.Message);
                IsProcessing = false;
            }
        }

        public static void OnWriteChatColor(string line)
        {
            if (!IsProcessing) return;
            EventProcessor._mqEventProcessingQueue.Enqueue(line);
        }
        public static void OnCommand(string commandLine)
        {
            if (!IsProcessing) return;
            EventProcessor._mqCommandProcessingQueue.Enqueue(commandLine);
        }
        public static void OnIncomingChat(string line)
        {
            if (!IsProcessing) return;
            EventProcessor._eventProcessingQueue.Enqueue(line);
        }

        // Simple ImGui render for E3Display
        public static void OnUpdateImGui()
        {
            try
            {
                const string window = "E3Display";
                if (imgui_Begin_OpenFlagGet(window))
                {
                    if (imgui_Begin(window, (int)ImGuiWindowFlags.ImGuiWindowFlags_None))
                    {
                        // status line
                        var status = E3Display.MyCode.ConnectionStatus;
                        imgui_Text(string.IsNullOrEmpty(status) ? "Not connected" : status);

                        // Test Router request button
                        if (imgui_Button("Test ${Me.Name}"))
                        {
                            try
                            {
                                var resp = E3Display.MyCode.MyCode_SafeRequest("${Me.Name}");
                                var line = string.IsNullOrEmpty(resp) ? "<empty>" : resp;
                                // prefix so we can see it's from router
                                E3Display.MyCode.EnqueueMessage("[Router] " + line);
                            }
                            catch { }
                        }

                        // show last messages
                        var snapshot = E3Display.MyCode.GetRecentMessagesSnapshot();
                        if (snapshot.Count == 0)
                        {
                            imgui_Text("No messages yet. Waiting for pub/sub...");
                        }
                        else
                        {
                            int shown = 0;
                            foreach (var s in snapshot)
                            {
                                if (shown++ >= 10) break; // render at most 10 lines per frame
                                try
                                {
                                    var safe = E3Display.MyCode.SanitizeForImGui(s);
                                    imgui_Text(safe);
                                }
                                catch { }
                            }
                        }

                        imgui_End();
                    }
                }
            }
            catch { }
        }

        #region MQMethods
        [MethodImpl(MethodImplOptions.InternalCall)] public extern static void mq_Echo(string msg);
        [MethodImpl(MethodImplOptions.InternalCall)] public extern static string mq_ParseTLO(string msg);
        [MethodImpl(MethodImplOptions.InternalCall)] public extern static void mq_DoCommand(string msg);
        [MethodImpl(MethodImplOptions.InternalCall)] public extern static void mq_DoCommandDelayed(string msg);
        [MethodImpl(MethodImplOptions.InternalCall)] public extern static void mq_Delay(int delay);
        [MethodImpl(MethodImplOptions.InternalCall)] public extern static bool mq_AddCommand(string command);
        [MethodImpl(MethodImplOptions.InternalCall)] public extern static void mq_ClearCommands();
        [MethodImpl(MethodImplOptions.InternalCall)] public extern static void mq_RemoveCommand(string command);
        [MethodImpl(MethodImplOptions.InternalCall)] public extern static string mq_GetMQ2MonoVersion();
        #endregion

        #region IMGUI
        [MethodImpl(MethodImplOptions.InternalCall)] public extern static bool imgui_Begin(string name, int flags);
        [MethodImpl(MethodImplOptions.InternalCall)] public extern static void imgui_Begin_OpenFlagSet(string name, bool value);
        [MethodImpl(MethodImplOptions.InternalCall)] public extern static bool imgui_Begin_OpenFlagGet(string name);
        [MethodImpl(MethodImplOptions.InternalCall)] public extern static bool imgui_Button(string name);
        [MethodImpl(MethodImplOptions.InternalCall)] public extern static void imgui_Text(string text);
        [MethodImpl(MethodImplOptions.InternalCall)] public extern static void imgui_End();
        #endregion

        [DllImport("user32.dll")] public static extern bool UnregisterClass(string lpClassName, IntPtr hInstance);
        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)] public static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)] public static extern IntPtr GetModuleHandle([MarshalAs(UnmanagedType.LPWStr)] string lpModuleName);
        [DllImport("kernel32.dll", SetLastError = true)] [PreserveSig] public static extern uint GetModuleFileName(IntPtr hModule, StringBuilder lpFilename, int nSize);
        [DllImport("user32.dll")] public static extern IntPtr GetWindowThreadProcessId(IntPtr hWnd, out uint ProcessId);
    }

    public enum ImGuiWindowFlags
    {
        ImGuiWindowFlags_None = 0,
        ImGuiWindowFlags_NoTitleBar = 1 << 0,
        ImGuiWindowFlags_NoResize = 1 << 1,
    }
}
