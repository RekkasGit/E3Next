using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Version 0.1
/// This file, is the 'core', or the mediator between you and MQ2Mono
/// MQ2Mono is just a simple C++ plugin to MQ, which exposes the
/// OnInit
/// OnPulse
/// OnIncomingChat
/// etc
/// Methods from MQ event framework, and meshes it in such a way to allow you to write rather straight forward C# code.
///
/// Included in this is a Logging/trace framework, Event Proecssor, MQ command object, etc
///
/// Your class is included in here with a simple .Process Method. This methoid will be called once every OnPulse from the plugin, or basically every frame of the EQ client.
/// All you code should *NOT* be in this file.
/// </summary>
namespace E3Core.MonoCore
{
    /// <summary>
    /// Processor to handle Event strings
    /// It spawns its own thread to do the inital regex parse, whatever matches will be
    /// put into the proper queue for each event for later invoke when the C# thread comes around
    /// </summary>
    public static class EventProcessor
    {
        //***NOTE*** no _log.Write or MQ.Writes are allowed here. Use remote debugging.
        //YOU WILL LOCK the process :) don't do it. Remember this is a seperate thread.

        public enum eventType
        {
            Unknown = 0,
            EQEvent = 1,
            MQEvent = 2,
        }

        public sealed class EventListItem
        {
            public string keyName;
            public readonly List<Regex> regexs = new List<Regex>();
            public Action<EventMatch> method;
            public ConcurrentQueue<EventMatch> queuedEvents = new ConcurrentQueue<EventMatch>();
        }

        public sealed class CommandListItem
        {
            public string keyName;
            public string command;
            public Action<CommandMatch> method;
            public ConcurrentQueue<CommandMatch> queuedEvents = new ConcurrentQueue<CommandMatch>();
        }

        public sealed class CommandMatch
        {
            public List<string> _args;

            public List<string> args
            {
                get { return _args; }
                set
                {   //filter out any into filters.
                    if (value != null)
                    {
                        filters = GetCommandFilters(value);
                        if (filters.Count > 0)
                        {
                            _args = value.Where(x => !filters.Contains(x)).ToList();
                        }
                        else
                        {
                            _args = value;
                        }
                    }
                }
            }
            public bool hasAllFlag;
            public string eventString;
            public string eventName;
            public List<string> filters = new List<string>();
        }

        public sealed class EventMatch
        {
            public string eventString;
            public Match match;
            public string eventName;
            public eventType typeOfEvent = eventType.Unknown;
        }

        private static readonly ConcurrentDictionary<string, Action<EventMatch>> _unfilteredEventMethodList = new ConcurrentDictionary<string, Action<EventMatch>>();
        private static readonly ConcurrentDictionary<string, EventListItem> _unfilteredEventList = new ConcurrentDictionary<string, EventListItem>();
        public static readonly ConcurrentDictionary<string, EventListItem> EventList = new ConcurrentDictionary<string, EventListItem>();
        public static readonly ConcurrentDictionary<string, CommandListItem> CommandList = new ConcurrentDictionary<string, CommandListItem>();
        //this is the first queue that strings get put into, will be processed by its own thread
        private static readonly ConcurrentQueue<string> _eventProcessingQueue = new ConcurrentQueue<string>();
        private static readonly ConcurrentQueue<string> _mqEventProcessingQueue = new ConcurrentQueue<string>();
        private static readonly ConcurrentQueue<string> _mqCommandProcessingQueue = new ConcurrentQueue<string>();
        private static readonly List<Regex> _filterRegexes = new List<Regex>();
        private static StringBuilder _tokenBuilder = new StringBuilder();
        private static readonly List<string> _tokenResult = new List<string>();
        //if matches take place, they are placed in this queue for the main C# thread to process. 
        public static int EventLimiterPerRegisteredEvent { get; set; } = 10;

        private static bool _isInit = false;
        public static void Init()
        {
            if (!_isInit) return;

            //some filter regular expressions so we can quickly get rid of combat and "has cast a spell" stuff. 
            //if your app needs them remove these :)
            _filterRegexes.Add(new Regex(@" points of damage\."));
            _filterRegexes.Add(new Regex(@" points of non-melee damage\."));
            _filterRegexes.Add(new Regex(@" begins to cast a spell\."));

            _ = Task.Factory.StartNew(() => ProcessEventsIntoQueues(), CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
            _isInit = true;
        }

        /// <summary>
        /// Runs on its own thread, will process through all the strings passed in and then put them into the correct queue
        /// </summary>
        public static void ProcessEventsIntoQueues()
        {
            // Need to do this so double parses work in other languages
            Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");

            // WARNING DO NOT SEND COMMANDS/Writes/Echos, etc from this thread. 
            //  only the primary C# thread can do that.
            while (Core.IsProcessing)
            {
                if (_eventProcessingQueue.Count > 0)
                {
                    if (_eventProcessingQueue.TryDequeue(out string line))
                    {
                        foreach (var ueventMethod in _unfilteredEventMethodList)
                        {
                            ueventMethod.Value.Invoke(new EventMatch() { eventName = ueventMethod.Key, eventString = line, typeOfEvent = eventType.EQEvent });
                        }

                        foreach (var uevent in _unfilteredEventList)
                        {
                            uevent.Value.queuedEvents.Enqueue(new EventMatch() { eventName = uevent.Value.keyName, eventString = line, typeOfEvent = eventType.EQEvent });
                        }

                        //do filter matching
                        //does it match our filter ? if so we can leave
                        bool matchFilter = false;
                        //locl this so someone can clear/add more filters are runtime.
                        lock (_filterRegexes)
                        {
                            foreach (var filter in _filterRegexes)
                            {
                                var match = filter.Match(line);
                                if (match.Success)
                                {
                                    matchFilter = true;
                                    break;
                                }
                            }
                        }

                        if (!matchFilter)
                        {
                            foreach (var item in EventList)
                            {
                                //prevent spamming of an event to a user
                                if (item.Value.queuedEvents.Count > EventLimiterPerRegisteredEvent)
                                {
                                    continue;
                                }
                                foreach (var regex in item.Value.regexs)
                                {
                                    var match = regex.Match(line);
                                    if (match.Success)
                                    {
                                        item.Value.queuedEvents.Enqueue(new EventMatch() { eventName = item.Value.keyName, eventString = line, match = match, typeOfEvent = eventType.EQEvent });
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }
                else if (_mqEventProcessingQueue.Count > 0)
                {
                    //have to be careful here and process out anything that isn't boxchat or dannet.
                    if (_mqEventProcessingQueue.TryDequeue(out string line))
                    {
                        foreach (var ueventMethod in _unfilteredEventMethodList)
                        {
                            ueventMethod.Value.Invoke(new EventMatch() { eventName = ueventMethod.Key, eventString = line, typeOfEvent = eventType.MQEvent });
                        }

                        foreach (var uevent in _unfilteredEventList)
                        {
                            uevent.Value.queuedEvents.Enqueue(new EventMatch() { eventName = uevent.Value.keyName, eventString = line, typeOfEvent = eventType.MQEvent });
                        }

                        //do filtered
                        if (line.StartsWith("["))
                        {
                            int indexOfApp = line.IndexOf(MainProcessor.ApplicationName);
                            if (indexOfApp == 1)
                            {
                                if (line.IndexOf("]") == MainProcessor.ApplicationName.Length + 1)
                                {
                                    goto skipLine;
                                }
                                else
                                {
                                    goto processLine;
                                }
                            }
                        }
                    processLine:
                        foreach (var item in EventList)
                        {
                            //prevent spamming of an event to a user
                            if (item.Value.queuedEvents.Count > EventLimiterPerRegisteredEvent)
                            {
                                continue;
                            }

                            foreach (var regex in item.Value.regexs)
                            {
                                var match = regex.Match(line);
                                if (match.Success)
                                {
                                    item.Value.queuedEvents.Enqueue(new EventMatch() { eventName = item.Value.keyName, eventString = line, match = match, typeOfEvent = eventType.MQEvent });

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
                    if (_mqCommandProcessingQueue.TryDequeue(out string line))
                    {
                        if (!String.IsNullOrWhiteSpace(line))
                        {
                            foreach (var item in CommandList)
                            {
                                //prevent spamming of an event to a user
                                if (item.Value.queuedEvents.Count > 50)
                                {
                                    Core.mqInstance.Write("event limiter");

                                    continue;
                                }
                                if (line.Equals(item.Value.command, StringComparison.OrdinalIgnoreCase) || line.StartsWith(item.Value.command + " ", StringComparison.OrdinalIgnoreCase))
                                {
                                    //need to split out the params
                                    List<String> args = ParseParms(line, ' ', '"').ToList();
                                    args.RemoveAt(0);

                                    bool hasAllFlag = HasAllFlag(args);

                                    item.Value.queuedEvents.Enqueue(new CommandMatch() { eventName = item.Value.keyName, eventString = line, args = args, hasAllFlag = hasAllFlag });
                                }
                            }
                        }
                    }
                }
                else
                {
                    Thread.Sleep(1);
                }
            }
            Core.mqInstance.Write("Ending Event Processing Thread.");
        }

        ///checks for all flag and then removes it
        private static bool HasAllFlag(List<string> x)
        {
            bool hasAllFlag = false;
            foreach (var argValue in x)
            {
                if (argValue.StartsWith("/all", StringComparison.OrdinalIgnoreCase))
                {
                    hasAllFlag = true;
                }
            }
            if (hasAllFlag)
            {
                x.Remove("/all");
            }
            return hasAllFlag;
        }

        /// <summary>
        /// This is so that things like /command /only|<toonName> and such work
        /// </summary>
        /// <param name="values"></param>
        /// <returns></returns>
        private static List<string> GetCommandFilters(List<string> values)
        {
            ////Stop /Only|Soandoso
            ////FollowOn /Only|Healers WIZ Soandoso
            ////followon /Not|Healers /Exclude|Uberhealer1
            /////Staunch /Only|Healers
            /////Follow /Not|MNK
            //things like this put into the filter collection.
            List<string> returnValue = new List<string>();

            foreach (string value in values)
            {
                if (value.StartsWith("/only", StringComparison.OrdinalIgnoreCase))
                {
                    returnValue.Add(value);
                }
                else if (value.StartsWith("/not", StringComparison.OrdinalIgnoreCase))
                {
                    returnValue.Add(value);
                }
                else if (value.StartsWith("/exclude", StringComparison.OrdinalIgnoreCase))
                {
                    returnValue.Add(value);
                }
                else if (value.StartsWith("/include", StringComparison.OrdinalIgnoreCase))
                {
                    returnValue.Add(value);
                }
            }
            return returnValue;
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
                char prevChar;
                char nextChar;
                char currentChar;

                bool inString = false;
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

        /// <summary>
        /// Process all events in the queue on this current thread.
        /// can specify keyname to only process certain keys
        /// </summary>
        /// <param name="keyName"></param>
        public static void ProcessEventsInQueues(string keyName = "")
        {
            foreach (var item in EventList)
            {
                if (!Core.IsProcessing) return;

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
                    if (!Core.IsProcessing) return;
                    if (item.Value.queuedEvents.TryDequeue(out EventMatch line))
                    {
                        item.Value.method.Invoke(line);
                    }
                }
            }

            foreach (var item in CommandList)
            {
                if (!Core.IsProcessing) return;
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
                    if (!Core.IsProcessing) return;
                    if (item.Value.queuedEvents.TryDequeue(out CommandMatch line))
                    {
                        item.Value.method.Invoke(line);
                    }
                }
            }

            foreach (var item in _unfilteredEventList)
            {
                if (!Core.IsProcessing) return;
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
                    if (!Core.IsProcessing) return;
                    if (item.Value.queuedEvents.TryDequeue(out EventMatch line))
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
            _eventProcessingQueue.Enqueue(line);
        }

        public static void ProcessMQEvent(string line)
        {
            _mqEventProcessingQueue.Enqueue(line);
        }

        public static void ProcessMQCommand(string line)
        {
            _mqCommandProcessingQueue.Enqueue(line);
        }

        public static void ClearEventQueue(string keyName)
        {
            if (EventList.TryGetValue(keyName, out EventListItem tEventItem))
            {
                while (!tEventItem.queuedEvents.IsEmpty)
                {
                    tEventItem.queuedEvents.TryDequeue(out EventMatch line);
                }
            }
        }

        public static bool RegisterCommand(string commandName, Action<CommandMatch> method)
        {
            CommandListItem c = new CommandListItem();
            c.command = commandName;
            c.method = method;
            c.keyName = commandName;

            bool returnvalue = Core.mqInstance.AddCommand(commandName);

            if (returnvalue)
            {
                if (CommandList.TryAdd(commandName, c))
                {
                    //now to register the command over.
                    return true;
                }
            }
            return false;
        }

        public static void UnRegisterCommand(string commandName)
        {
            if (CommandList.TryRemove(commandName, out CommandListItem c))
            {
                Core.mqInstance.RemoveCommand(commandName);
            }
        }

        public static void RegisterEvent(string keyName, string pattern, Action<EventMatch> method)
        {
            EventListItem eventToAdd = new EventListItem();

            eventToAdd.regexs.Add(new Regex(pattern));
            eventToAdd.method = method;
            eventToAdd.keyName = keyName;

            EventList.TryAdd(keyName, eventToAdd);
        }

        public static void RegisterEvent(string keyName, List<string> patterns, Action<EventMatch> method)
        {
            EventListItem eventToAdd = new EventListItem();

            foreach (var pattern in patterns)
            {
                eventToAdd.regexs.Add(new Regex(pattern));
            }

            eventToAdd.method = method;
            eventToAdd.keyName = keyName;

            EventList.TryAdd(keyName, eventToAdd);
        }

        /// <summary>
        /// used by seperate threads of the main C# thread to get unfiltered events
        /// directly to them without having to go through the normal procees loop
        /// currently used by E3UI.
        /// </summary>
        /// <param name="keyName"></param>
        /// <param name="method"></param>
        public static void RegisterUnfilteredEventMethod(string keyName, Action<EventMatch> method)
        {
            _unfilteredEventMethodList.TryAdd(keyName, method);
        }

        /// <summary>
        /// When you want an event to not be filtered by the default filtering
        /// Do be careful of this, as you can loop yourself by accident.
        /// </summary>
        /// <param name="keyName"></param>
        /// <param name="pattern"></param>
        /// <param name="method"></param>
        public static void RegisterUnfilteredEvent(string keyName, string pattern, Action<EventMatch> method)
        {
            EventListItem eventToAdd = new EventListItem();

            eventToAdd.regexs.Add(new Regex(pattern));
            eventToAdd.method = method;
            eventToAdd.keyName = keyName;
            _unfilteredEventList.TryAdd(keyName, eventToAdd);
        }
    }
}