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
using NetMQ;
using NetMQ.Sockets;
using Google.Protobuf;
using System.Globalization;
using IniParser.Model;
using System.IO;

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
        public static IMQ _mq = Core.mqInstance;
       
        private static Logging _log = Core.logInstance;
        public static string ApplicationName = "";
        public static void Init()
        {

            //WARNING , you may not be in game yet, so careful what queries you run on MQ.Query. May cause a crash.
            //how long before auto yielding back to C++/EQ/MQ on the next query/command/etc
     
        }
        
        //we use this to tell the C++ thread that its okay to start processing gain
        public static ManualResetEventSlim ProcessResetEvent = new ManualResetEventSlim(false);

        public static void Process()
        {
            //need to do this so double parses work in other languages
            Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");

            //wait for the C++ thread thread to tell us we can go
            ProcessResetEvent.Wait();
            ProcessResetEvent.Reset();
            
            //volatile variable, will eventually update to kill the thread on shutdown
            while (Core.IsProcessing)
            {
                try
                {
                    //using (_log.Trace())
                    {
                        //************************************************
                        //DO YOUR WORK HERE
                        //this loop executes once every OnPulse from C++
                        //************************************************
                        EventProcessor.ProcessEventsInQueues();
                        E3.Process();
                        EventProcessor.ProcessEventsInQueues();

                    }
                }
                catch (Exception ex)
                {
				
                    if(ex is ThreadAbort)
					{
						Core.IsProcessing = false;
						Core.CoreResetEvent.Set();
						throw new ThreadAbort("Terminating thread");
				    }

                    if(Core.IsProcessing)
                    {
                        _log.Write("Error: Please reload. Terminating. \r\nExceptionMessage:" + ex.Message + " stack:" + ex.StackTrace.ToString(), Logging.LogLevels.CriticalError);
						Core.IsProcessing = false;
						Core.CoreResetEvent.Set();
					}
                    
                    //we perma exit this thread loop a full reload will be necessary
                    break;
                }

                //give execution back to the C++ thread, to go back into MQ/EQ
                if(Core.IsProcessing)
                {
                    Delay(E3.CharacterSettings.CPU_ProcessLoopDelay);//this calls the reset events and sets the delay to 10ms at min
                }
            }

			//E3.Shutdown();
			_mq.Write("Shutting down E3 Main C# Thread.");
			_mq.Write("Doing netmq cleanup.");
			//NetMQConfig.Cleanup(false);
			if (MQ.DelayedWrites.Count > 0)
			{
				while (MQ.DelayedWrites.Count > 0)
				{
					string message;
					if (MQ.DelayedWrites.TryDequeue(out message))
					{
						Core.mq_Echo(message);
					}
				}
			}
			Core.CoreResetEvent.Set();
        }

        static public void Delay(Int32 value)
        {
            if (value > 0)
            {
                Core.DelayStartTime = Core.StopWatch.ElapsedMilliseconds;
                Core.DelayTime = value;
                Core.CurrentDelay = value;//tell the C++ thread to send out a delay update
            }
            //lets tell core that it can continue
            Core.CoreResetEvent.Set();
            //we are now going to wait on the core
            MainProcessor.ProcessResetEvent.Wait();
            MainProcessor.ProcessResetEvent.Reset();
        }

    }
 
    /// <summary>
    /// Processor to handle Event strings
    /// It spawns its own thread to do the inital regex parse, whatever matches will be 
    /// put into the proper queue for each event for later invoke when the C# thread comes around
    /// </summary>
    static public class EventProcessor
    {
        //***NOTE*** no _log.Write or MQ.Writes are allowed here. Use remote debugging.
        //YOU WILL LOCK the process :) don't do it. Remember this is a seperate thread.
        
        public static ConcurrentDictionary<string, Action<EventMatch>> _unfilteredEventMethodList = new ConcurrentDictionary<string, Action<EventMatch>>();
        public static ConcurrentDictionary<string, EventListItem> _unfilteredEventList = new ConcurrentDictionary<string, EventListItem>();
        public static ConcurrentDictionary<string, EventListItem> EventList = new ConcurrentDictionary<string, EventListItem>();
        public static ConcurrentQueue<CommandMatch> CommandListQueue = new ConcurrentQueue<CommandMatch>();
        public static ConcurrentDictionary<string, CommandListItem> CommandList = new ConcurrentDictionary<string, CommandListItem>();
        //this is the first queue that strings get put into, will be processed by its own thread
        public static ConcurrentQueue<String> _eventProcessingQueue = new ConcurrentQueue<String>();
        public static ConcurrentQueue<String> _mqEventProcessingQueue = new ConcurrentQueue<string>();
        public static ConcurrentQueue<String> _mqCommandProcessingQueue = new ConcurrentQueue<string>();
        public static List<Regex> _filterRegexes = new List<Regex>();
        private static StringBuilder _tokenBuilder = new StringBuilder();
        private static List<string> _tokenResult = new List<string>();
        //if matches take place, they are placed in this queue for the main C# thread to process. 
        public static Int32 EventLimiterPerRegisteredEvent = 10;
       
        //this threads entire purpose, is to simply keep processing the event processing queue and place matches into
        //the eventfilteredqueue
        public static Task _regExProcessingTask;
        private static Logging _log = Core.logInstance;

        private static Boolean _isInit = false;
        public static void Init()
        {
            if (!_isInit)
            {
                //some filter regular expressions so we can quicly get rid of combat and "has cast a spell" stuff. 
                //if your app needs them remove these :)
                System.Text.RegularExpressions.Regex filterRegex = new Regex(@" points of damage\.");
                //  _filterRegexes.Add(filterRegex);
                // filterRegex = new Regex(@" points of non-melee damage\.");
                // _filterRegexes.Add(filterRegex);
                //filterRegex = new Regex(@" begins to cast a spell\.");
                // _filterRegexes.Add(filterRegex);

                _regExProcessingTask =Task.Factory.StartNew(() => { ProcessEventsIntoQueues(); }, CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
                _isInit = true;
            
            }

        }

        public static bool CommandListQueueHasCommand(string command)
        {
            foreach (var value in CommandListQueue)
            {
                if (value.eventName == command) return true;
            }
            return false;
        }

        public static void ProcessEventsIntoQueue_EventProcessing()
        {
			if (_eventProcessingQueue.Count > 0)
			{
				string line;
				if (_eventProcessingQueue.TryDequeue(out line))
				{
					try
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
                        bool bypassFilter = false;

                        if (line.Contains("(Rampage)")) bypassFilter = true;
						else if (line.Contains("YOU for ")) bypassFilter = true;
						else if (line.Contains("You have taken ")) bypassFilter = true;



						if (!bypassFilter)
                        {
							//using contains as live/emu are different on their log messages for endings
							//so instead of doing endswith + contains, just do contains.
							//contains uses an Ordinal compiarson sa well, so should be fairly fast
							if (Int32.TryParse(line, out var temp)) matchFilter = true; //if only in hitmode number, filter it out
							else if (line.Contains("scores a critical hit!")) matchFilter = true;
							else if (line.Contains("delivers a critical blast!")) matchFilter = true;
							else if (line.Contains("lands a Crippling Blow!")) matchFilter = true;
							else if (line.Contains("points of damage.") && !line.Contains("(Rampage)")) matchFilter = true;
							else if (line.Contains("points of non-melee damage.")) matchFilter = true;

							//filters are just there in case we need to dynamically add a regex to filter out stuff.
							if (!matchFilter)
							{
								//needed for live as they have differnt log messages
								if (_filterRegexes.Count > 0)
								{
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
								}
							}
						}
						

						if (!matchFilter || bypassFilter)
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
					catch (Exception)
					{
						//Try catch was added to deal with chinese characters causing some logic issue and forcing the thread to crash. Just catch and eat the exception.

					}

				}
			}
		}
		public static void ProcessEventsIntoQueue_MQEventProcessing()
        {

			if (_mqEventProcessingQueue.Count > 0)
			{
				//have to be careful here and process out anything that isn't boxchat or dannet.
				string line;
				if (_mqEventProcessingQueue.TryDequeue(out line))
				{
					try
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
							Int32 indexOfApp = line.IndexOf(MainProcessor.ApplicationName);
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
					catch (Exception)
					{
						//Try catch was added to deal with chinese characters causing some logic issue and forcing the thread to crash. Just catch and eat the exception.
					}
				}
			skipLine:
				line = string.Empty;
			}
		}
		public static bool ProcessEventsIntoQueue_MQCommandProcessing()
        {
            bool processedCommand = false;
			if (_mqCommandProcessingQueue.Count > 0)
			{
				//have to be careful here and process out anything that isn't boxchat or dannet.
				string line;
				if (_mqCommandProcessingQueue.TryDequeue(out line))
				{
					processedCommand = true;
					//prevent spamming of an event to a user
					if (CommandListQueue.Count > 50)
					{
						Core.mqInstance.Write("event limiter");
						return true;
					}
					try
					{
						if (!String.IsNullOrWhiteSpace(line))
						{

							foreach (var item in CommandList)
							{
								if (line.Equals(item.Value.command, StringComparison.OrdinalIgnoreCase) || line.StartsWith(item.Value.command + " ", StringComparison.OrdinalIgnoreCase))
								{
									//need to split out the params
									List<String> args = ParseParms(line, ' ', '"').ToList();
									args.RemoveAt(0);
									bool hasAllFlag = HasAllFlag(args);
                                    CommandMatch commandMatch = new CommandMatch() { eventName = item.Value.keyName, eventString = line, args = args, hasAllFlag = hasAllFlag };
                                    CommandListQueue.Enqueue(commandMatch);
									//item.Value.queuedEvents.Enqueue(new CommandMatch() { eventName = item.Value.keyName, eventString = line, args = args, hasAllFlag = hasAllFlag });
								}
							}
						}
					}
					catch (Exception e)
					{
						//Try catch was added to deal with chinese characters causing some logic issue and forcing the thread to crash. Just catch and eat the exception.
					}

				}
			}
            return processedCommand;
		}
	
        /// <summary>
        /// Runs on its own thread, will process through all the strings passed in and then put them into the correct queue
        /// </summary>
        public static void ProcessEventsIntoQueues()
        {
            //need to do this so double parses work in other languages
            Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");

            System.Text.RegularExpressions.Regex dannetRegex = new Regex("");
            char[] splitChars = new char[1] {' '};

     
            ////WARNING DO NOT SEND COMMANDS/Writes/Echos, etc from this thread. 
            ///only the primary C# thread can do that.
            while (Core.IsProcessing)
            {
				if(_eventProcessingQueue.Count>0 || _mqEventProcessingQueue.Count>0 || _mqCommandProcessingQueue.Count>0)
				{
					ProcessEventsIntoQueue_EventProcessing();
					ProcessEventsIntoQueue_MQEventProcessing();
					ProcessEventsIntoQueue_MQCommandProcessing();
				}
                else
                {
                    System.Threading.Thread.Sleep(1);
                }
            }
            Core.mqInstance.Write("Ending Event Processing Thread.");
        }
        ///checks for all flag and then removes it
        private static bool HasAllFlag(List<String> x)
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
                if (value.StartsWith("/only|", StringComparison.OrdinalIgnoreCase) )
                {
                    returnValue.Add(value);
                }
                else if (value.StartsWith("/not|", StringComparison.OrdinalIgnoreCase) )
                {
                    returnValue.Add(value);
                }
                else if (value.StartsWith("/exclude|",  StringComparison.OrdinalIgnoreCase))
                {
                    returnValue.Add(value);
                }
                else if (value.StartsWith("/include|",StringComparison.OrdinalIgnoreCase))
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
                    EventMatch line;
                    if (item.Value.queuedEvents.TryDequeue(out line))
                    {
                        item.Value.method.Invoke(line);
                    }
                }

            }


            //if a key name is specified we only want to process those event types
           

            if(CommandListQueue.Count > 0)
            {
        		Int32 CommandListSize = CommandListQueue.Count;
				for (Int32 i = 0; i < CommandListSize; i++)
				{
					if (!Core.IsProcessing) return;

					CommandMatch line;
					if (CommandListQueue.TryDequeue(out line))
					{
						if (!String.IsNullOrWhiteSpace(keyName) && line.eventName != keyName)
						{
							//don't match just put back into the queue
							CommandListQueue.Enqueue(line);
						}
						else
						{
							//it matches or no match specified, lets process stuff!
							if (CommandList.ContainsKey(line.eventName))
							{
								CommandList[line.eventName].method.Invoke(line);
        					}
						}
					}
				}
			}
            //foreach (var item in CommandList)
            //{
            //    if (!Core.IsProcessing) return;
            //    //check to see if we have to have a filter on the events to process
            //    if (!String.IsNullOrWhiteSpace(keyName))
            //    {
            //        //if keyName is specified, verify that its the key we want. 
            //        if (!item.Value.keyName.Equals(keyName, StringComparison.OrdinalIgnoreCase))
            //        {

            //            continue;
            //        }
            //    }
            //    while (item.Value.queuedEvents.Count > 0)
            //    {
            //        if (!Core.IsProcessing) return;
            //        CommandMatch line;
            //        if (item.Value.queuedEvents.TryDequeue(out line))
            //        {
            //            item.Value.method.Invoke(line);
            //        }
            //    }

            //}
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
            EventListItem tEventItem;
            if (EventList.TryGetValue(keyName, out tEventItem))
            {
                EventMatch line;
                while (!tEventItem.queuedEvents.IsEmpty)
                {
                    tEventItem.queuedEvents.TryDequeue(out line);
                }
            }
        }
        public enum eventType
        {
            Unknown=0,
            EQEvent=1,
            MQEvent=2,
           
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
            public String classOwner;
            public string methodCaller;
			public string description;
            public System.Action<CommandMatch> method;
            //public ConcurrentQueue<CommandMatch> queuedEvents = new ConcurrentQueue<CommandMatch>();
        }
        public class CommandMatch
        {
            public List<String> _args;
       
            public List<String> args
            {
                get { return _args; }
                set 
                {   //filter out any into filters.
                    if (value != null)
                    {
                        filters = GetCommandFilters(value);
                        if(filters.Count>0)
                        {
                            _args = value.Where(x => !filters.Any(y => y == x)).ToList();

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
        public class EventMatch
        {
            public string eventString;
            public Match match;
            public string eventName;
            public eventType typeOfEvent=eventType.Unknown;
        }
        public static bool RegisterCommand(string commandName, Action<CommandMatch> method,string description="", [CallerMemberName] string memberName = "", [CallerFilePath] string fileName = "", [CallerLineNumber] int lineNumber = 0)
        {
            CommandListItem c = new CommandListItem();
            c.command = commandName;
            c.method = method;
            c.keyName = commandName;
            c.methodCaller = memberName;
			c.description = description;
            c.classOwner = Logging.GetClassName(fileName);
     
            bool returnvalue =  Core.mqInstance.AddCommand(commandName);
     
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
        public static void OverrideCommandMethod(string commandName,Action<CommandMatch> method)
        {
            if(CommandList.TryGetValue(commandName,out var c))
            {
                c.method = method;
            }
        }
        public static void UnRegisterCommand(string commandName)
        {
            CommandListItem c;
            if (CommandList.TryRemove(commandName, out c))
            {
                Core.mqInstance.RemoveCommand(commandName);
            }

        }

		public static void ClearDynamicEvents()
		{
			List<string> eventKeys = EventList.Keys.ToList();
			foreach(var key in eventKeys)
			{

				if(key.StartsWith("DynamicEvent_"))
				{
					if(EventList.TryRemove(key, out var eventListItem))
					{
						//removed item
						
					}
				
				}
			}

		}
		public static void RegisterDynamicEvent(string keyName, string pattern, Action<EventMatch> method)
		{
			keyName = "DynamicEvent_" + keyName;
			EventListItem eventToAdd = new EventListItem();
			eventToAdd.regexs = new List<Regex>();

			eventToAdd.regexs.Add(new System.Text.RegularExpressions.Regex(pattern));
			eventToAdd.method = method;
			eventToAdd.keyName = keyName;

			EventList.TryAdd(keyName, eventToAdd);

		}
		public static void RegisterEvent(string keyName, string pattern, Action<EventMatch> method)
        {
            EventListItem eventToAdd = new EventListItem();
            eventToAdd.regexs = new List<Regex>();

            eventToAdd.regexs.Add(new System.Text.RegularExpressions.Regex(pattern));
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
            eventToAdd.regexs = new List<Regex>();

            eventToAdd.regexs.Add(new System.Text.RegularExpressions.Regex(pattern));
            eventToAdd.method = method;
            eventToAdd.keyName = keyName;
            _unfilteredEventList.TryAdd(keyName, eventToAdd);

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

            EventList.TryAdd(keyName, eventToAdd);

        }
        public static void OverrideRegisteredEvent(string keyname,Action<EventMatch> method)
        {
            if(EventList.TryGetValue(keyname,out var e))
            {
                e.method = method;
            }
        }

    }
    //used to abort the main C# thread so that it can finish up and exist
    //try statemnts that catch expections need to exclude this error. 
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
        public static Logging logInstance;
        public volatile static bool IsProcessing = false;
        public const string _coreVersion = "0.2";
        public static Decimal _MQ2MonoVersion = 0.2M;

        //Note, if you comment out a method, this will tell MQ2Mono to not try and execute it
        //only use the events you need to prevent string allocations to be passed in
        //also, seems these need to be static

        //this is called quite often, haven't determined a throttle yet, but assume 10 times a second

        //this is protected by the lock, so that the primary C++ thread is the one that executes commands that the
        //processing thread has done.
        public static string CurrentCommand = string.Empty;
        public static bool CurrentCommandDelayed = false;   
        public static string _currentWrite = String.Empty;
        public static Int32 CurrentDelay = 0;

        //delay in milliseconds
        public static Int64 DelayTime = 0;
        //timestamp in milliseconds.
        public static Int64 DelayStartTime = 0;
        public static Stopwatch StopWatch = new Stopwatch();
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
        public static ManualResetEventSlim CoreResetEvent = new ManualResetEventSlim(false);
        static Task _taskThread;
        public static void OnInit()
        {
			try
			{
				Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");
				_MQ2MonoVersion = Decimal.Parse(Core.mq_GetMQ2MonoVersion());
			}
			catch (Exception)
			{
				//old version, does not have mq2mono method, warn user
			}

			if (!_isInit)
            {
                IsProcessing = true;
                if (mqInstance == null)
                {
                    mqInstance = new MQ();
                }
                if (spawnInstance == null)
                {
                    spawnInstance = new Spawns();
                }
                logInstance = new Logging(mqInstance);
                StopWatch.Start();
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

                // Register a simple toggle command for the ImGui window
                try { mqInstance.AddCommand("/e3imgui"); } catch { /* older MQ2Mono ok */ }
            }
        }
        public static void OnStop()
        {
            IsProcessing = false;
            //wait will issue a memory barrier, set will not, issue one
            System.Threading.Thread.MemoryBarrier();
            //tell the C# thread that it can now process and since processing is false, we can then end the application.
            MainProcessor.ProcessResetEvent.Set();
			E3Core.Server.NetMQServer.KillAllProcesses();
            E3Core.Server.NetMQServer.Stop();
            NetMQConfig.Cleanup(false);
            System.Threading.Thread.Sleep(500);
			//write out any writes that have been delayed
			if (MQ.DelayedWrites.Count > 0)
			{
				while (MQ.DelayedWrites.Count > 0)
				{
					string message;
					if (MQ.DelayedWrites.TryDequeue(out message))
					{
						Core.mq_Echo(message);
					}
				}
			}
			GC.Collect();
            ////NOTE , there are situations where the unload of the domain will lock up. I've done everything I can do to prevent this, but it 'can' and will happen. 
            ////I've written a script to reload constantly for 5-6 min before lockup, but again its a % chance. 
        }
        public static void OnPulse()
        {

            if (!IsProcessing)
            {   
                //allow the primary thread to finish terminating. 
                MainProcessor.ProcessResetEvent.Set();
                return;

            }//reset the last delay so we restart the procssing time since its a new OnPulse()
            MQ.SinceLastDelay = StopWatch.ElapsedMilliseconds;

            _onPulseCalls++;
            //if delay was issued, we need to honor it, kickout for no processing
            if (DelayTime > 0)
            {
                if ((StopWatch.ElapsedMilliseconds - DelayStartTime) < DelayTime)
                {   //we are still under the delay time specified, don't do any processing
                    //don't really need to do a lock as there are only two threads and the set below will sync the cores
                    return;

                }
                //reset if we have bypassed the time value
                DelayTime = 0;
            }

            RestartWait:
            //allow the processing thread to start its work.
            //and copy cache values to other cores so the thread can see the updated information in MQ
            MainProcessor.ProcessResetEvent.Set();
            //Core.mq_Echo("Blocking on C++");
            Core.CoreResetEvent.Wait();
            Core.CoreResetEvent.Reset();
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
            if (CurrentCommand != String.Empty)
            {
                //special commands that dont' go through the 'delay of processing back to MQ
                //useitem for manastone and echo for... well echoing out data/broadcast. 
                bool gobacktoCSharp = CurrentCommand.StartsWith("/useitem");
                if(CurrentCommand.StartsWith("/echo"))
                {
                    gobacktoCSharp = true;
                }

                if(!CurrentCommandDelayed || _MQ2MonoVersion<0.22m)
                {
                    //if not delayed, or the version doesn't support it, use this.
					Core.mq_DoCommand(CurrentCommand);
				}
				else
                {
                    //if the version supports delayed command, use it, otherwise ignore. 
					Core.mq_DoCommandDelayed(CurrentCommand);
				}
				CurrentCommand = String.Empty;
                CurrentCommandDelayed = false;

                if (gobacktoCSharp)
                {
                    goto RestartWait;
                }

            }
            if (CurrentDelay > 0)
            {
                Core.mq_Delay(CurrentDelay);
                CurrentDelay = 0;
            }
			//write out any writes that have been delayed
			if(MQ.DelayedWrites.Count>0)
			{
				while(MQ.DelayedWrites.Count>0)
				{
					string message;
					if(MQ.DelayedWrites.TryDequeue(out message))
					{
						Core.mq_Echo(message);
					}
				}
			}
        }

        //Comment these out if you are not using events so that C++ doesn't waste time sending the string to C#
        public static void OnWriteChatColor(string line)
        {
            if (!IsProcessing)
            {
                return;
            }
            EventProcessor.ProcessMQEvent(line);
        }
        public static void OnCommand(string commandLine)
        {
            if (!IsProcessing)
            {
                return;
            }
            // Intercept our ImGui toggle
            if (commandLine.StartsWith("/e3imgui", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    bool open = imgui_Begin_OpenFlagGet(_e3ImGuiWindow);
                    imgui_Begin_OpenFlagSet(_e3ImGuiWindow, !open);
                }
                catch { }
                return;
            }
            if (commandLine.StartsWith("/e3importifs", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    int added = ImportSampleIfs();
                    mq_Echo(added > 0 ? $"Imported {added} IF(s) into GlobalIfs.ini" : "No new IFs to import.");
                }
                catch (Exception ex)
                {
                    mq_Echo($"Import failed: {ex.Message}");
                }
                return;
            }
            if (commandLine.StartsWith("/e3buttons", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    // support optional index: /e3buttons 2
                    int idx = 1;
                    var parts = commandLine.Split(' ');
                    if (parts.Length > 1)
                    {
                        int.TryParse(parts[1], out idx);
                        if (idx < 1) idx = 1;
                    }
                    string windowName = idx == 1 ? _e3ButtonsWindow : _e3ButtonsWindow + " #" + idx.ToString();
                    bool open = imgui_Begin_OpenFlagGet(windowName);
                    imgui_Begin_OpenFlagSet(windowName, !open);
                }
                catch { }
                return;
            }

            mq_Echo("command recieved:" + commandLine);
            EventProcessor.ProcessMQCommand(commandLine);
        }
        private static int ImportSampleIfs()
        {
            // Locate sample file
            string sample1 = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory ?? string.Empty, "sample ifs");
            string sample = System.IO.File.Exists(sample1) ? sample1 : string.Empty;
            if (string.IsNullOrEmpty(sample) || !System.IO.File.Exists(sample)) return 0;

            // Determine GlobalIfs.ini path (respect CurrentSet)
            string gfPath = E3Core.Settings.BaseSettings.GetSettingsFilePath("GlobalIfs.ini");
            if (!string.IsNullOrEmpty(E3Core.Settings.BaseSettings.CurrentSet))
                gfPath = gfPath.Replace(".ini", "_" + E3Core.Settings.BaseSettings.CurrentSet + ".ini");

            var parser = E3Core.Utility.e3util.CreateIniParser();
            IniParser.Model.IniData data = null;
            if (System.IO.File.Exists(gfPath))
            {
                data = parser.ReadFile(gfPath);
            }
            else
            {
                // Create new with defaults (this will also seed from sample if available)
                data = E3.GlobalIfs != null ? E3.GlobalIfs.CreateSettings(gfPath) : new IniParser.Model.IniData();
                if (!data.Sections.ContainsSection("Ifs")) data.Sections.AddSection("Ifs");
            }

            // Use SectionData instead of KeyDataCollection to match types
            var ifsSection = data.Sections.GetSectionData("Ifs") ?? new IniParser.Model.SectionData("Ifs");
            if (!data.Sections.ContainsSection("Ifs")) data.Sections.Add(ifsSection);

            int added = 0;
            foreach (var raw in System.IO.File.ReadAllLines(sample))
            {
                var line = (raw ?? string.Empty).Trim();
                if (line.Length == 0) continue;
                if (line.StartsWith("#") || line.StartsWith(";")) continue;
                int eq = line.IndexOf('=');
                if (eq <= 0) continue;
                string key = line.Substring(0, eq).Trim();
                string val = line.Substring(eq + 1).Trim();
                if (string.IsNullOrEmpty(key)) continue;
                if (!ifsSection.Keys.ContainsKey(key))
                {
                    ifsSection.Keys.AddKey(key, val);
                    added++;
                }
            }
            parser.WriteFile(gfPath, data);
            try { E3.GlobalIfs?.LoadData(); } catch { }
            return added;
        }
        public static void OnIncomingChat(string line)
        {
            if (!IsProcessing)
            {
                return;
            }
            EventProcessor.ProcessEvent(line);
        }

        static System.Text.StringBuilder _queryBuilder = new StringBuilder(); 
		public static string OnQuery(string line)
		{
			//mq_Echo("query recieved:" + line);
			if (!IsProcessing)
			{
				return String.Empty;
			}
			if (!E3.IsInit)
            {
                return String.Empty;
            }
            _queryBuilder.Clear();
            _queryBuilder.Append(line);
            _queryBuilder.Replace('(', '[');
			_queryBuilder.Replace(')', ']');
            line = _queryBuilder.ToString();
            string results = String.Empty;
			//mq_Echo("query fixed:" + line);

			try
			{
                //its important to disable the delay, as that can force control back to C++, whcn C++ is waiting on us to respond
                //aka deadlock. do not allow that to happen, set global nodelay to prevent any sub calls calling delay.
				MQ._noDelay = true;
                _queryBuilder.Clear();
                _queryBuilder.Append("${");
                _queryBuilder.Append(line);
                _queryBuilder.Append("}");
                results = Casting.Ifs_Results(_queryBuilder.ToString());
			}
            finally
            {
                //put it back when done
				MQ._noDelay = false;
			}

			//mq_Echo("final result:" + results);
			return results;
		}
		public static void OnSetSpawns(byte[] data, int size)
        {


            //pull the id out of the array
            Int32 ID = BitConverter.ToInt32(data, 0);
          
            Spawn s;
            if(Spawns.SpawnsByID.TryGetValue(ID, out s))
            {
				//just update the value
				try
				{
					s.Init(data, size);

				}
				catch (Exception) { };

			}
            else
            {
                var spawn = Spawn.Aquire();
				try
				{
					spawn.Init(data, size);
					Spawns._spawns.Add(spawn);

				}
				catch(Exception)
				{
					spawn.Dispose();
				}
			}

            
            //copy the data out into the current array set. 
        }

        // Simple in-game ImGui windows.
        // Config UI toggle: "/e3imgui". Buttons bar toggle: "/e3buttons".
        private static readonly string _e3ImGuiWindow = "E3Next Config";
        private static readonly string _e3ButtonsWindow = "E3 Buttons";
        private static bool _imguiInitDone = false;
        // Queue to apply UI-driven changes safely on the processing loop
        public static ConcurrentQueue<Action> UIApplyQueue = new ConcurrentQueue<Action>();
        public static void EnqueueUI(Action a)
        {
            if (a != null) UIApplyQueue.Enqueue(a);
        }

        // Settings viewer state/cache
        private enum SettingsTab { Character, General, Advanced }
        private static SettingsTab _activeSettingsTab = SettingsTab.Character;
        private static string _activeSettingsFilePath = string.Empty;
        private static string[] _activeSettingsFileLines = Array.Empty<string>();
        private static long _nextIniRefreshAtMs = 0;
        private static string _selectedCharacterSection = string.Empty;
        private static Dictionary<string, string> _charIniEdits = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        // "All Players" key view state/cache
        private static bool _cfgAllPlayersView = false; // when true, show aggregated view of a key across all toon INIs
        private static string _cfgAllPlayersSig = string.Empty; // section::key signature
        private static long _cfgAllPlayersNextRefreshAtMs = 0;
        private static List<System.Collections.Generic.KeyValuePair<string, string>> _cfgAllPlayersRows = new List<System.Collections.Generic.KeyValuePair<string, string>>(); // (toon -> value)
        private static Dictionary<string, string> _cfgAllPlayersServerByToon = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private static HashSet<string> _cfgAllPlayersIsRemote = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private static Dictionary<string, string> _cfgAllPlayersEditBuf = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private static readonly object _cfgAllPlayersLock = new object();
        private static System.Threading.Tasks.Task _cfgAllPlayersWorkerTask = null;
        // Background refresh control for All Players view (avoid doing IO on ImGui thread)
        private static bool _cfgAllPlayersRefreshRequested = false;
        private static bool _cfgAllPlayersRefreshing = false;
        private static string _cfgAllPlayersReqSection = string.Empty;
        private static string _cfgAllPlayersReqKey = string.Empty;
        private static long _cfgAllPlayersLastUpdatedAt = 0;
        private static int _cfgAllPlayersRefreshIntervalMs = 5000; // default 5s between auto refreshes
        private static string _cfgAllPlayersStatus = string.Empty;

        // Character .ini selection state
        private static string _selectedCharIniPath = string.Empty; // defaults to current character
        private static IniData _selectedCharIniParsedData = null;  // parsed data for non-current selection
        private static string[] _charIniFiles = Array.Empty<string>();
        private static long _nextIniFileScanAtMs = 0;
        // Dropdown support (feature-detect combo availability to avoid crashes on older MQ2Mono)
        private static bool _comboAvailable = true;
        private static void RefreshSettingsViewIfNeeded()
        {
            try
            {
                if (Core.StopWatch.ElapsedMilliseconds < _nextIniRefreshAtMs) return;
                _nextIniRefreshAtMs = Core.StopWatch.ElapsedMilliseconds + 1000; // 1s throttle

                string path = GetActiveSettingsPath();
                if (!string.Equals(path, _activeSettingsFilePath, StringComparison.OrdinalIgnoreCase))
                {
                    _activeSettingsFilePath = path;
                    _activeSettingsFileLines = Array.Empty<string>();
                }
                if (!string.IsNullOrEmpty(path) && System.IO.File.Exists(path))
                {
                    _activeSettingsFileLines = System.IO.File.ReadAllLines(path);
                }
                else
                {
                    _activeSettingsFileLines = new[] { "Settings file not found.", path ?? string.Empty };
                }
            }
            catch
            {
                _activeSettingsFileLines = new[] { "Error reading settings file." };
            }
        }
        private static string GetActiveSettingsPath()
        {
            switch (_activeSettingsTab)
            {
                case SettingsTab.General:
                    if (E3.GeneralSettings != null && !string.IsNullOrEmpty(E3.GeneralSettings._fileLastModifiedFileName))
                        return E3.GeneralSettings._fileLastModifiedFileName;
                    return E3Core.Settings.BaseSettings.GetSettingsFilePath("General Settings.ini");
                case SettingsTab.Advanced:
                    var adv = E3Core.Settings.BaseSettings.GetSettingsFilePath("Advanced Settings.ini");
                    if (!string.IsNullOrEmpty(E3Core.Settings.BaseSettings.CurrentSet)) adv = adv.Replace(".ini", "_" + E3Core.Settings.BaseSettings.CurrentSet + ".ini");
                    return adv;
                case SettingsTab.Character:
                default:
                    // current character path
                    var currentPath = GetCurrentCharacterIniPath();
                    if (string.IsNullOrEmpty(_selectedCharIniPath))
                        _selectedCharIniPath = currentPath;
                    return _selectedCharIniPath;
            }
        }

        private static string GetCurrentCharacterIniPath()
        {
            if (E3.CharacterSettings != null && !string.IsNullOrEmpty(E3.CharacterSettings._fileName))
                return E3.CharacterSettings._fileName;
            // fallback
            var name = E3.CurrentName ?? string.Empty;
            var server = E3.ServerName ?? string.Empty;
            var klass = E3.CurrentClass.ToString();
            return E3Core.Settings.BaseSettings.GetBoTFilePath(name, server, klass);
        }

        private static void ScanCharIniFilesIfNeeded()
        {
            try
            {
                if (Core.StopWatch.ElapsedMilliseconds < _nextIniFileScanAtMs) return;
                _nextIniFileScanAtMs = Core.StopWatch.ElapsedMilliseconds + 3000; // 3s throttle

                var curPath = GetCurrentCharacterIniPath();
                if (string.IsNullOrEmpty(curPath) || !File.Exists(curPath)) return;
                var dir = Path.GetDirectoryName(curPath);
                var server = E3.ServerName ?? string.Empty;
                // Prefer files that end with _{server}.ini to keep list relevant
                var pattern = "*_*" + server + ".ini";
                var files = Directory.GetFiles(dir, pattern, SearchOption.TopDirectoryOnly);
                // Fallback: if no server-suffixed files found, list all ini
                if (files == null || files.Length == 0)
                    files = Directory.GetFiles(dir, "*.ini", SearchOption.TopDirectoryOnly);
                Array.Sort(files, StringComparer.OrdinalIgnoreCase);
                _charIniFiles = files;
            }
            catch { /* ignore scan errors */ }
        }

        private static IniData GetActiveCharacterIniData()
        {
            var currentPath = GetCurrentCharacterIniPath();
            if (string.Equals(_selectedCharIniPath, currentPath, StringComparison.OrdinalIgnoreCase))
                return E3.CharacterSettings?.ParsedData;
            return _selectedCharIniParsedData;
        }

        private static void RenderCharacterIniSelector()
        {
            ScanCharIniFilesIfNeeded();

            var currentPath = GetCurrentCharacterIniPath();
            string currentDisplay = Path.GetFileName(currentPath);
            // True dropdown using ImGui combo if available; else graceful fallback
            string selName = Path.GetFileName(_selectedCharIniPath ?? currentPath);
            if (string.IsNullOrEmpty(selName)) selName = currentDisplay;
            bool opened = _comboAvailable && BeginComboSafe("Ini File", selName);
            if (opened)
            {
                // Current (live) file
                if (!string.IsNullOrEmpty(currentPath))
                {
                    bool sel = string.Equals(_selectedCharIniPath, currentPath, StringComparison.OrdinalIgnoreCase);
                    if (imgui_Selectable($"Current: {currentDisplay}", sel))
                    {
                        _selectedCharIniPath = currentPath;
                        _selectedCharIniParsedData = null; // use live current
                        _selectedCharacterSection = string.Empty;
                        _charIniEdits.Clear();
                        _nextIniRefreshAtMs = 0;
                    }
                }

                foreach (var f in _charIniFiles)
                {
                    if (string.Equals(f, currentPath, StringComparison.OrdinalIgnoreCase)) continue;
                    string name = Path.GetFileName(f);
                    bool sel = string.Equals(_selectedCharIniPath, f, StringComparison.OrdinalIgnoreCase);
                    if (imgui_Selectable(name, sel))
                    {
                        try
                        {
                            var parser = E3Core.Utility.e3util.CreateIniParser();
                            var pd = parser.ReadFile(f);
                            _selectedCharIniPath = f;
                            _selectedCharIniParsedData = pd;
                            _selectedCharacterSection = string.Empty;
                            _charIniEdits.Clear();
                            _cfgAllPlayersSig = string.Empty; // force refresh on next open
                            _nextIniRefreshAtMs = 0;
                        }
                        catch
                        {
                            mq_Echo($"Failed to load ini: {name}");
                        }
                    }
                }
                EndComboSafe();
            }
            else if (!_comboAvailable)
            {
                // Fallback UI: simple list while older MQ2Mono is in use
                imgui_Text("Update MQ2Mono to enable dropdown. Temporary list:");
                float availX = imgui_GetContentRegionAvailX();
                float listH = 160f;
                if (imgui_BeginChild("CharIni_FallbackList", availX, listH, true))
                {
                    if (!string.IsNullOrEmpty(currentPath))
                    {
                        bool sel = string.Equals(_selectedCharIniPath, currentPath, StringComparison.OrdinalIgnoreCase);
                        if (imgui_Selectable($"Current: {currentDisplay}", sel))
                        {
                            _selectedCharIniPath = currentPath;
                            _selectedCharIniParsedData = null;
                            _selectedCharacterSection = string.Empty;
                            _charIniEdits.Clear();
                            _nextIniRefreshAtMs = 0;
                        }
                    }
                    foreach (var f in _charIniFiles)
                    {
                        if (string.Equals(f, currentPath, StringComparison.OrdinalIgnoreCase)) continue;
                        string name = Path.GetFileName(f);
                        bool sel = string.Equals(_selectedCharIniPath, f, StringComparison.OrdinalIgnoreCase);
                        if (imgui_Selectable(name, sel))
                        {
                            try
                        {
                            var parser = E3Core.Utility.e3util.CreateIniParser();
                            var pd = parser.ReadFile(f);
                            _selectedCharIniPath = f;
                            _selectedCharIniParsedData = pd;
                            _selectedCharacterSection = string.Empty;
                            _charIniEdits.Clear();
                            _cfgAllPlayersSig = string.Empty; // force refresh on next open
                            _nextIniRefreshAtMs = 0;
                        }
                        catch
                        {
                            mq_Echo($"Failed to load ini: {name}");
                        }
                        }
                    }
                }
                imgui_EndChild();
            }
            // Right-aligned Save button (via MQ2Mono helper). Fallback to normal button if helper unavailable.
            // Keep on the same line as the Ini selector for consistency.
            imgui_SameLine();
            try
            {
                if (imgui_RightAlignButton("Save Changes"))
                {
                    SaveActiveIniData();
                }
            }
            catch
            {
                if (imgui_Button("Save Changes"))
                {
                    SaveActiveIniData();
                }
            }
            imgui_Separator();
        }

        // Tools: Spell Data and Spell Icons
        private enum ToolsTab { SpellData, SpellIcons }
        private static ToolsTab _toolsActive = ToolsTab.SpellData;
        private static string _spellQueryInput = string.Empty;
        private static string _spellLoadedKey = string.Empty; // last loaded key
        private static E3Core.Data.Spell _spellLoaded = null;
        private static string[] _spellEffects = Array.Empty<string>();
        private static long _nextSpellQueryAtMs = 0;

        private static void RenderTools()
        {
            imgui_Text("Tools");
            if (imgui_Button(_toolsActive == ToolsTab.SpellData ? "> Spell Data" : "Spell Data")) { _toolsActive = ToolsTab.SpellData; }
            imgui_SameLine();
            if (imgui_Button(_toolsActive == ToolsTab.SpellIcons ? "> Spell Icons" : "Spell Icons")) { _toolsActive = ToolsTab.SpellIcons; }
            imgui_Separator();

            switch (_toolsActive)
            {
                case ToolsTab.SpellIcons:
                    RenderSpellIconsTab();
                    break;
                case ToolsTab.SpellData:
                default:
                    RenderSpellDataTab();
                    break;
            }
        }

        // =========================
        // Config Editor (ImGui) — initial scaffolding similar to /e3config
        // =========================
        private static bool _cfg_Inited = false;
        private static SortedDictionary<string, SortedDictionary<string, List<E3Core.Data.Spell>>> _cfgSpells = new SortedDictionary<string, SortedDictionary<string, List<E3Core.Data.Spell>>>();
        private static SortedDictionary<string, SortedDictionary<string, List<E3Core.Data.Spell>>> _cfgAAs = new SortedDictionary<string, SortedDictionary<string, List<E3Core.Data.Spell>>>();
        private static SortedDictionary<string, SortedDictionary<string, List<E3Core.Data.Spell>>> _cfgDiscs = new SortedDictionary<string, SortedDictionary<string, List<E3Core.Data.Spell>>>();
        private static SortedDictionary<string, SortedDictionary<string, List<E3Core.Data.Spell>>> _cfgSkills = new SortedDictionary<string, SortedDictionary<string, List<E3Core.Data.Spell>>>();
        private static SortedDictionary<string, SortedDictionary<string, List<E3Core.Data.Spell>>> _cfgItems = new SortedDictionary<string, SortedDictionary<string, List<E3Core.Data.Spell>>>();
        // Manual input state for generic editor
        private static string _cfgManualInput = string.Empty;
        private static string _cfgManualInputKeySig = string.Empty;
        // Add modal If's appending state
        private static bool _cfgAddWithIf = false;
        private static string _cfgAddIfName = string.Empty;
        // Editor append If's state
        private static string _cfgAppendIfName = string.Empty;
        private static long _cfg_NextCatalogRefreshAtMs = 0;
        private static bool _cfg_CatalogsReady = false;
        private static bool _cfg_CatalogLoadRequested = false;
        private static bool _cfg_CatalogLoading = false;
        private static string _cfg_CatalogStatus = string.Empty;
        private static List<string> _cfgSectionsOrdered = new List<string>();
        private static string _cfg_LastIniPath = string.Empty;
        private static string _cfgSelectedSection = string.Empty;
        private static string _cfgSelectedKey = string.Empty; // subsection / key name
        private static int _cfgSelectedValueIndex = -1;
        private static bool _cfgShowAddModal = false;
        // When true, the Add modal operates in edit mode and replaces an existing value
        private static bool _cfgAddIsEditMode = false;
        private static int _cfgAddEditTargetIndex = -1;
        private static string _cfgAddEditOriginal = string.Empty;
        private enum AddType { Spells, AAs, Discs, Skills, Items }
        private static AddType _cfgAddType = AddType.Spells;
        private static string _cfgAddCategory = string.Empty;
        private static string _cfgAddSubcategory = string.Empty;
        // Food/Drink picker modal
        private static bool _cfgShowFoodDrinkModal = false;
        private static string _cfgFoodDrinkKey = string.Empty; // "Food" or "Drink"
        private static List<string> _cfgFoodDrinkCandidates = new List<string>();
        private static bool _cfgFoodDrinkScanRequested = false;
        private static bool _cfgFoodDrinkScanning = false;
        private static string _cfgFoodDrinkStatus = string.Empty;
        private static long _cfg_NextUIRefreshAtMs = 0;
        private static bool _cfg_Dirty = false;
        // If's sample modal state
        private static bool _cfgShowIfSampleModal = false;
        private static List<System.Collections.Generic.KeyValuePair<string, string>> _cfgIfSampleLines = new List<System.Collections.Generic.KeyValuePair<string, string>>();
        private static string _cfgIfSampleStatus = string.Empty;
        private static string _cfgIfNewName = string.Empty;
        private static string _cfgIfNewValue = string.Empty;
        private static string _cfgIfEditName = string.Empty;
        private static string _cfgIfEditValue = string.Empty;
        private static string _cfgIfEditSelectedKeySig = string.Empty;
        // Inline edit state for values list
        private static int _cfgInlineEditIndex = -1;
        private static string _cfgInlineEditBuffer = string.Empty;
        private static string _cfgInlineEditKeySig = string.Empty;

        private static void EnsureConfigEditorInit()
        {
            if (_cfg_Inited) return;
            _cfg_Inited = true;
            BuildConfigSectionOrder();
        }
        private static void BuildConfigSectionOrder()
        {
            _cfgSectionsOrdered.Clear();
            var klass = E3.CurrentClass;
            // Order aligned with ConfigEditor.GetSectionSortOrderByClass
            var defaults = new List<string>() { "Misc", "Assist Settings", "Nukes", "Debuffs", "DoTs on Assist", "DoTs on Command", "Heals", "Buffs", "Melee Abilities", "Burn", "CommandSets", "Pets", "Ifs" };
            if (klass == E3Core.Data.Class.Bard)
                defaults = new List<string>() { "Bard", "Melee Abilities", "Burn", "CommandSets", "Ifs", "Assist Settings", "Buffs" };
            else if (klass == E3Core.Data.Class.Necromancer)
                defaults = new List<string>() { "DoTs on Assist", "DoTs on Command", "Debuffs", "Pets", "Burn", "CommandSets", "Ifs", "Assist Settings", "Buffs" };
            else if (klass == E3Core.Data.Class.Shadowknight)
                defaults = new List<string>() { "Nukes", "Assist Settings", "Buffs", "DoTs on Assist", "DoTs on Command", "Debuffs", "Pets", "Burn", "CommandSets", "Ifs" };

            var pd = GetActiveCharacterIniData();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (pd != null && pd.Sections != null)
            {
                foreach (var s in defaults)
                {
                    if (pd.Sections.ContainsSection(s) && seen.Add(s)) _cfgSectionsOrdered.Add(s);
                }
                foreach (SectionData s in pd.Sections)
                {
                    if (!seen.Contains(s.SectionName)) _cfgSectionsOrdered.Add(s.SectionName);
                }
            }
            // set sensible defaults
            if (_cfgSectionsOrdered.Count > 0)
            {
                if (string.IsNullOrEmpty(_cfgSelectedSection) || !_cfgSectionsOrdered.Contains(_cfgSelectedSection, StringComparer.OrdinalIgnoreCase))
                {
                    _cfgSelectedSection = _cfgSectionsOrdered[0];
                    var section = pd?.Sections?.GetSectionData(_cfgSelectedSection);
                    _cfgSelectedKey = section?.Keys?.FirstOrDefault()?.KeyName ?? string.Empty;
                    _cfgSelectedValueIndex = -1;
                }
            }
        }
        private static void EnsureCatalogsLoaded()
        {
            // No-op in ImGui thread; loading is moved to main processing loop
            // to avoid freezing the ImGui render callback.
            return;
        }
        // Called from main processing loop (safe)
        public static void ProcessBackgroundWork()
        {
            // Catalog background loader
            if (!_cfg_CatalogsReady && _cfg_CatalogLoadRequested && !_cfg_CatalogLoading)
            {
                _cfg_CatalogLoading = true;
                try
                {
                    // Force-load catalogs synchronously (one-shot)
                    string targetToon = GetSelectedIniOwnerName();
                    bool useLocal = string.IsNullOrEmpty(targetToon) || targetToon.Equals(E3.CurrentName, StringComparison.OrdinalIgnoreCase);
                    if (useLocal)
                    {
                        _cfg_CatalogStatus = "Loading spells...";
                        var spells = E3Core.Utility.e3util.ListAllBookSpells();
                        _cfgSpells = OrganizeCatalog(spells);
                        _cfg_CatalogStatus = "Loading AAs...";
                        var aas = E3Core.Utility.e3util.ListAllActiveAA();
                        _cfgAAs = OrganizeCatalog(aas);
                        _cfg_CatalogStatus = "Loading discs...";
                        var discs = E3Core.Utility.e3util.ListAllDiscData();
                        _cfgDiscs = OrganizeCatalog(discs);
                        _cfg_CatalogStatus = "Loading skills...";
                        var skills = E3Core.Utility.e3util.ListAllActiveSkills();
                        _cfgSkills = OrganizeCatalog(skills);
                        _cfg_CatalogStatus = "Loading items...";
                        var items = E3Core.Utility.e3util.ListAllItemWithClickyData();
                        _cfgItems = OrganizeCatalog(items);
                        _cfg_CatalogStatus = $"Catalogs loaded for {E3.CurrentName}.";
                    }
                    else
                    {
                        _cfg_CatalogStatus = $"Loading catalogs from {targetToon}...";
                        if (!TryLoadPeerCatalogs(targetToon, out var spells, out var aas, out var discs, out var skills, out var items, out string why))
                        {
                            _cfg_CatalogStatus = (string.IsNullOrEmpty(why) ? "Peer catalog query failed" : why) + "; falling back to local.";
                            spells = E3Core.Utility.e3util.ListAllBookSpells();
                            aas = E3Core.Utility.e3util.ListAllActiveAA();
                            discs = E3Core.Utility.e3util.ListAllDiscData();
                            skills = E3Core.Utility.e3util.ListAllActiveSkills();
                            items = E3Core.Utility.e3util.ListAllItemWithClickyData();
                        }
                        _cfgSpells = OrganizeCatalog(spells);
                        _cfgAAs = OrganizeCatalog(aas);
                        _cfgDiscs = OrganizeCatalog(discs);
                        _cfgSkills = OrganizeCatalog(skills);
                        _cfgItems = OrganizeCatalog(items);
                        _cfg_CatalogStatus += " Done.";
                    }
                    _cfg_CatalogsReady = true;
                }
                catch (Exception ex)
                {
                    _cfg_CatalogStatus = "Catalog load failed: " + (ex.Message ?? "error");
                }
                finally
                {
                    _cfg_CatalogLoading = false;
                    _cfg_CatalogLoadRequested = false;
                }
            }

            // Food/Drink background scanner
            if (_cfgFoodDrinkScanRequested && !_cfgFoodDrinkScanning)
            {
                _cfgFoodDrinkScanning = true;
                try
                {
                    var list = ScanFoodDrinkCandidates(_cfgFoodDrinkKey);
                    _cfgFoodDrinkCandidates = list ?? new List<string>();
                    _cfgFoodDrinkStatus = _cfgFoodDrinkCandidates.Count == 0 ? "No matches found." : $"Found {_cfgFoodDrinkCandidates.Count} items.";
                }
                catch (Exception ex)
                {
                    _cfgFoodDrinkStatus = "Scan failed: " + (ex.Message ?? "error");
                }
                finally
                {
                    _cfgFoodDrinkScanRequested = false;
                    _cfgFoodDrinkScanning = false;
                }
            }

            // All Players aggregated view background refresh
            if (_cfgAllPlayersRefreshRequested && !_cfgAllPlayersRefreshing)
            {
                _cfgAllPlayersRefreshing = true;
                _cfgAllPlayersRefreshRequested = false;
                var sec = _cfgAllPlayersReqSection ?? string.Empty;
                var key = _cfgAllPlayersReqKey ?? string.Empty;
                _cfgAllPlayersWorkerTask = System.Threading.Tasks.Task.Run(() =>
                {
                    try
                    {
                        // Build snapshot off-thread (reuses existing method)
                        _cfgAllPlayersNextRefreshAtMs = 0; // force internal throttle to allow work
                        RefreshAllPlayersCacheIfNeeded(sec, key);
                        _cfgAllPlayersLastUpdatedAt = Core.StopWatch.ElapsedMilliseconds;
                        _cfgAllPlayersStatus = string.Empty;
                    }
                    catch (Exception ex)
                    {
                        _cfgAllPlayersStatus = "Refresh failed: " + (ex.Message ?? "error");
                    }
                    finally
                    {
                        _cfgAllPlayersRefreshing = false;
                    }
                });
            }
        }

        private static string GetSelectedIniOwnerName()
        {
            try
            {
                var path = GetActiveSettingsPath();
                var cur = GetCurrentCharacterIniPath();
                if (string.IsNullOrEmpty(path) || string.Equals(path, cur, StringComparison.OrdinalIgnoreCase)) return E3.CurrentName;
                var file = Path.GetFileName(path) ?? string.Empty;
                if (string.IsNullOrEmpty(file) || file.StartsWith("_")) return E3.CurrentName;
                int us = file.IndexOf('_');
                if (us > 0) return file.Substring(0, us);
                return E3.CurrentName;
            }
            catch { return E3.CurrentName; }
        }

        private static bool TryLoadPeerCatalogs(string toon, out ListE3Spells spellsOut)
        {
            spellsOut = new ListE3Spells();
            try
            {
                if (string.IsNullOrEmpty(toon)) return false;
                if (!E3Core.Server.NetMQServer.SharedDataClient.UsersConnectedTo.TryGetValue(toon, out var info)) return false;
                if (info.RouterPort <= 0) return false;
                using (var req = new RequestSocket())
                {
                    req.Connect($"tcp://127.0.0.1:{info.RouterPort}");
                    Func<string, List<E3Core.Data.Spell>> fetch = (q) =>
                    {
                        var payload = Encoding.Default.GetBytes(q);
                        byte[] frame = new byte[8 + payload.Length];
                        Buffer.BlockCopy(BitConverter.GetBytes(1), 0, frame, 0, 4);
                        Buffer.BlockCopy(BitConverter.GetBytes(payload.Length), 0, frame, 4, 4);
                        Buffer.BlockCopy(payload, 0, frame, 8, payload.Length);
                        req.SendFrame(frame);
                        if (!req.TryReceiveFrameBytes(TimeSpan.FromMilliseconds(1500), out var resp)) return new List<E3Core.Data.Spell>();
                        var list = SpellDataList.Parser.ParseFrom(resp);
                        var outList = new List<E3Core.Data.Spell>(list.Data.Count);
                        foreach (var d in list.Data) outList.Add(E3Core.Data.Spell.FromProto(d));
                        return outList;
                    };
                    spellsOut.Spells = fetch("${E3.SpellBook.ListAll}");
                    spellsOut.AAs = fetch("${E3.AA.ListAll}");
                    spellsOut.Discs = fetch("${E3.Discs.ListAll}");
                    spellsOut.Skills = fetch("${E3.Skills.ListAll}");
                    spellsOut.Items = fetch("${E3.ItemsWithSpells.ListAll}");
                }
                return true;
            }
            catch { return false; }
        }

        private struct ListE3Spells
        {
            public List<E3Core.Data.Spell> Spells;
            public List<E3Core.Data.Spell> AAs;
            public List<E3Core.Data.Spell> Discs;
            public List<E3Core.Data.Spell> Skills;
            public List<E3Core.Data.Spell> Items;
        }

        private static bool TryLoadPeerCatalogs(string toon, out List<E3Core.Data.Spell> spells, out List<E3Core.Data.Spell> aas,
            out List<E3Core.Data.Spell> discs, out List<E3Core.Data.Spell> skills, out List<E3Core.Data.Spell> items, out string reason)
        {
            spells = new List<E3Core.Data.Spell>(); aas = new List<E3Core.Data.Spell>(); discs = new List<E3Core.Data.Spell>(); skills = new List<E3Core.Data.Spell>(); items = new List<E3Core.Data.Spell>();
            reason = string.Empty;
            try
            {
                if (!TryLoadPeerCatalogs(toon, out var all)) { reason = "Peer not available"; return false; }
                spells = all.Spells ?? new List<E3Core.Data.Spell>();
                aas = all.AAs ?? new List<E3Core.Data.Spell>();
                discs = all.Discs ?? new List<E3Core.Data.Spell>();
                skills = all.Skills ?? new List<E3Core.Data.Spell>();
                items = all.Items ?? new List<E3Core.Data.Spell>();
                return true;
            }
            catch (Exception ex) { reason = ex.Message; return false; }
        }
        private static SortedDictionary<string, SortedDictionary<string, List<E3Core.Data.Spell>>> OrganizeCatalog(List<E3Core.Data.Spell> list)
        {
            var dest = new SortedDictionary<string, SortedDictionary<string, List<E3Core.Data.Spell>>>(StringComparer.OrdinalIgnoreCase);
            foreach (var s in list)
            {
                if (s == null) continue;
                string cat = s.Category ?? string.Empty;
                string sub = s.Subcategory ?? string.Empty;
                if (!dest.TryGetValue(cat, out var submap))
                {
                    submap = new SortedDictionary<string, List<E3Core.Data.Spell>>(StringComparer.OrdinalIgnoreCase);
                    dest.Add(cat, submap);
                }
                if (!submap.TryGetValue(sub, out var l))
                {
                    l = new List<E3Core.Data.Spell>();
                    submap.Add(sub, l);
                }
                l.Add(s);
            }
            // Sort leaf lists in-place by level desc to avoid modifying dictionary during enumeration
            foreach (var submap in dest.Values)
            {
                foreach (var l in submap.Values)
                {
                    l.Sort((a, b) => b.Level.CompareTo(a.Level));
                }
            }
            return dest;
        }
        private static void RenderConfigEditor()
        {
            EnsureConfigEditorInit();

            var pd = GetActiveCharacterIniData();
            if (pd == null || pd.Sections == null)
            {
                imgui_Text("No character INI loaded.");
                return;
            }
            // If we initialized before the INI data was available, the ordered list may be empty.
            // Ensure we build the section order once valid data is present so the left pane populates.
            if (_cfgSectionsOrdered.Count == 0)
            {
                BuildConfigSectionOrder();
            }
            // If no section is selected but the INI has sections, auto-select the first
            if (string.IsNullOrEmpty(_cfgSelectedSection))
            {
                foreach (SectionData s in pd.Sections)
                {
                    _cfgSelectedSection = s.SectionName;
                    var firstSection = pd.Sections.GetSectionData(_cfgSelectedSection);
                    _cfgSelectedKey = firstSection?.Keys?.FirstOrDefault()?.KeyName ?? string.Empty;
                    _cfgSelectedValueIndex = -1;
                    break;
                }
            }
            // Rebuild sections order when ini path changes
            string activeIniPath = GetActiveSettingsPath() ?? string.Empty;
            if (!string.Equals(activeIniPath, _cfg_LastIniPath, StringComparison.OrdinalIgnoreCase))
            {
                _cfg_LastIniPath = activeIniPath;
                _cfgSelectedSection = string.Empty;
                _cfgSelectedKey = string.Empty;
                _cfgSelectedValueIndex = -1;
                BuildConfigSectionOrder();
                // Trigger catalog reload for the newly selected ini owner
                _cfg_CatalogsReady = false;
                _cfg_CatalogLoadRequested = true;
            }
            // Catalog status / load control
            if (!_cfg_CatalogsReady)
            {
                if (_cfg_CatalogLoading)
                {
                    imgui_Text(string.IsNullOrEmpty(_cfg_CatalogStatus) ? "Loading catalogs..." : _cfg_CatalogStatus);
                }
                else
                {
                    imgui_Text(string.IsNullOrEmpty(_cfg_CatalogStatus) ? "Catalogs not loaded." : _cfg_CatalogStatus);
                    imgui_SameLine();
                    if (imgui_Button("Load Catalogs"))
                    {
                        EnqueueUI(() => { _cfg_CatalogLoadRequested = true; });
                        _cfg_CatalogStatus = "Queued catalog load...";
                    }
                }
            }

            float availY = imgui_GetContentRegionAvailY();
            float leftW = 220f;
            if (imgui_BeginChild("Cfg_Left", leftW, Math.Max(120f, availY * 0.8f), true))
            {
                foreach (var sec in _cfgSectionsOrdered)
                {
                    bool sel = string.Equals(_cfgSelectedSection, sec, StringComparison.OrdinalIgnoreCase);
                    if (imgui_Selectable(sec, sel))
                    {
                        _cfgSelectedSection = sec;
                        // choose first key by default
                        var secData = pd.Sections.GetSectionData(sec);
                        _cfgSelectedKey = secData?.Keys?.FirstOrDefault()?.KeyName ?? string.Empty;
                        _cfgSelectedValueIndex = -1;
                    }
                }
            }
            imgui_EndChild();

            imgui_SameLine();

            // Middle pane: list subsections (keys) of selected section
            float midW = 260f;
            var selectedSection = pd.Sections.GetSectionData(_cfgSelectedSection ?? string.Empty);
            if (imgui_BeginChild("Cfg_Middle", midW, Math.Max(120f, availY * 0.8f), true))
            {
                if (selectedSection == null)
                {
                    imgui_Text("Select a section.");
                }
                else
                {
                    KeyDataCollection keysEnum = (selectedSection != null && selectedSection.Keys != null)
                        ? selectedSection.Keys
                        : new KeyDataCollection();
                    string[] keys = keysEnum.Select(k => k.KeyName).ToArray();
                    if (keys.Length == 0)
                    {
                        imgui_Text("(No subsections)");
                    }
                    else
                    {
                        if (string.IsNullOrEmpty(_cfgSelectedKey) || !keys.Contains(_cfgSelectedKey, StringComparer.OrdinalIgnoreCase))
                        {
                            _cfgSelectedKey = keys[0];
                            _cfgSelectedValueIndex = -1;
                        }
                        foreach (var k in keys)
                        {
                            bool sel = string.Equals(_cfgSelectedKey, k, StringComparison.OrdinalIgnoreCase);
                            if (imgui_Selectable(k, sel))
                            {
                                _cfgSelectedKey = k;
                                _cfgSelectedValueIndex = -1;
                            }
                        }
                    }
                }
            }
            imgui_EndChild();

            imgui_SameLine();

            // Right pane: values list and actions for selected subsection
            float rightW = imgui_GetContentRegionAvailX();
            if (imgui_BeginChild("Cfg_Right", rightW, Math.Max(120f, availY * 0.8f), true))
            {
                selectedSection = pd.Sections.GetSectionData(_cfgSelectedSection ?? string.Empty);
                if (selectedSection == null)
                {
                    imgui_Text("Select a section.");
                }
                else
                {
                    // Toggle to view this key across all character INIs
                    bool canMulti = (_activeSettingsTab == SettingsTab.Character) && !string.IsNullOrEmpty(_cfgSelectedSection) && !string.IsNullOrEmpty(_cfgSelectedKey);
                    if (canMulti)
                    {
                        if (imgui_Button(_cfgAllPlayersView ? "This Player View" : "All Players View"))
                        {
                            _cfgAllPlayersView = !_cfgAllPlayersView;
                            _cfgAllPlayersSig = string.Empty; // force refresh
                        }
                        imgui_SameLine();
                        imgui_Text($"[{_cfgSelectedSection}] {_cfgSelectedKey}");
                        imgui_Separator();
                        if (_cfgAllPlayersView)
                        {
                            RenderAllPlayersKeyView(_cfgSelectedSection ?? string.Empty, _cfgSelectedKey ?? string.Empty);
                            imgui_EndChild();
                            return; // skip normal editor when in all-players view
                        }
                    }
                    else
                    {
                        _cfgAllPlayersView = false;
                    }
                    bool isIfsSection = string.Equals(_cfgSelectedSection, "Ifs", StringComparison.OrdinalIgnoreCase);
                    // Ifs section: always show new IF controls and Sample If's access, even if no key is selected
                    if (isIfsSection)
                    {
                        // Sample If's button
                        if (imgui_Button("Sample If's"))
                        {
                            try { LoadSampleIfsForModal(); _cfgShowIfSampleModal = true; }
                            catch (Exception ex) { _cfgIfSampleStatus = "Load failed: " + (ex.Message ?? "error"); _cfgShowIfSampleModal = true; }
                        }
                        // Inline add-new IF controls
                        imgui_SameLine();
                        imgui_Text("New If:");
                        if (imgui_InputText("If_NewName", _cfgIfNewName)) _cfgIfNewName = imgui_InputText_Get("If_NewName") ?? string.Empty;
                        imgui_SameLine();
                        if (imgui_InputText("If_NewValue", _cfgIfNewValue)) _cfgIfNewValue = imgui_InputText_Get("If_NewValue") ?? string.Empty;
                        imgui_SameLine();
                        if (imgui_Button("Add If") && !string.IsNullOrWhiteSpace(_cfgIfNewName))
                        {
                            if (AddIfToActiveIni(_cfgIfNewName.Trim(), _cfgIfNewValue?.Trim() ?? string.Empty))
                            {
                                _cfgIfNewName = string.Empty; _cfgIfNewValue = string.Empty;
                            }
                        }
                        imgui_Separator();
                    }
                    var keyData = selectedSection.Keys?.GetKeyData(_cfgSelectedKey ?? string.Empty);
                    // Special editor for Ifs entries (name/value pairs)
                    if (isIfsSection && keyData != null)
                    {
                        // Seed edit buffers when selection changes
                        string sig = ($"Ifs::{_cfgSelectedKey ?? string.Empty}");
                        if (!string.Equals(_cfgIfEditSelectedKeySig, sig, StringComparison.Ordinal))
                        {
                            _cfgIfEditSelectedKeySig = sig;
                            _cfgIfEditName = _cfgSelectedKey ?? string.Empty;
                            _cfgIfEditValue = keyData.Value ?? (keyData.ValueList != null && keyData.ValueList.Count > 0 ? keyData.ValueList[0] : string.Empty);
                        }
                        imgui_Text("Edit If");
                        if (imgui_InputText("If_EditName", _cfgIfEditName)) _cfgIfEditName = imgui_InputText_Get("If_EditName") ?? string.Empty;
                        if (imgui_InputText("If_EditValue", _cfgIfEditValue)) _cfgIfEditValue = imgui_InputText_Get("If_EditValue") ?? string.Empty;
                        if (imgui_Button("Save If"))
                        {
                            try
                            {
                                var keys = selectedSection.Keys;
                                string newName = _cfgIfEditName?.Trim() ?? string.Empty;
                                if (!string.IsNullOrEmpty(newName))
                                {
                                    if (!string.Equals(newName, _cfgSelectedKey, StringComparison.Ordinal))
                                    {
                                        // rename by add+remove to avoid collisions
                                        var existing = keys.GetKeyData(newName);
                                        if (existing == null)
                                        {
                                            keys.AddKey(newName, _cfgIfEditValue ?? string.Empty);
                                            keys.RemoveKey(_cfgSelectedKey);
                                            _cfgSelectedKey = newName;
                                        }
                                        else
                                        {
                                            // overwrite value of existing and remove old key
                                            existing.Value = _cfgIfEditValue ?? string.Empty;
                                            keys.RemoveKey(_cfgSelectedKey);
                                            _cfgSelectedKey = newName;
                                        }
                                    }
                                    else
                                    {
                                        // same name: just update value
                                        keyData.Value = _cfgIfEditValue ?? string.Empty;
                                        if (keyData.ValueList != null && keyData.ValueList.Count > 0)
                                        {
                                            keyData.ValueList.Clear();
                                            keyData.ValueList.Add(keyData.Value);
                                        }
                                    }
                                    _cfg_Dirty = true;
                                }
                            }
                            catch { }
                        }
                        imgui_SameLine();
                        if (imgui_Button("Delete If"))
                        {
                            try
                            {
                                selectedSection.Keys.RemoveKey(_cfgSelectedKey ?? string.Empty);
                                _cfgSelectedKey = string.Empty;
                                _cfgIfEditName = string.Empty; _cfgIfEditValue = string.Empty; _cfgIfEditSelectedKeySig = string.Empty;
                                _cfg_Dirty = true;
                            }
                            catch { }
                        }
                        // do not render generic editor for Ifs
                    }
                    else if (keyData != null)
                    {
                        // Special-case editors by Section/Key first
                        // If this key is a boolean, render a simple On/Off dropdown everywhere
                        if (IsBooleanConfigKey(_cfgSelectedKey, keyData))
                        {
                            var values = keyData.ValueList;
                            string current = keyData.Value;
                            if (string.IsNullOrEmpty(current) && values != null && values.Count > 0) current = values[0];
                            bool isOn = string.Equals(current, "On", StringComparison.OrdinalIgnoreCase) || string.Equals(current, "True", StringComparison.OrdinalIgnoreCase);

                            string preview = isOn ? "On" : "Off";
                            if (BeginComboSafe(_cfgSelectedKey, preview))
                            {
                                string[] opts = new[] { "On", "Off" };
                                for (int oi = 0; oi < opts.Length; oi++)
                                {
                                    string opt = opts[oi];
                                    bool sel = string.Equals(preview, opt, StringComparison.OrdinalIgnoreCase);
                                    if (imgui_Selectable($"{opt}##Bool_{oi}", sel))
                                    {
                                        keyData.Value = opt;
                                        if (values != null)
                                        {
                                            values.Clear();
                                            values.Add(opt);
                                        }
                                        _cfg_Dirty = true;
                                    }
                                }
                                EndComboSafe();
                            }
                        }
                        // Special-case: Assist Settings -> Assist Type (Melee/Ranged/Off)
                        else if (string.Equals(_cfgSelectedSection, "Assist Settings", StringComparison.OrdinalIgnoreCase)
                              && string.Equals(_cfgSelectedKey, "Assist Type (Melee/Ranged/Off)", StringComparison.OrdinalIgnoreCase))
                        {
                            var values = keyData.ValueList;
                            string current = keyData.Value;
                            if (string.IsNullOrEmpty(current) && values != null && values.Count > 0) current = values[0];
                            if (string.IsNullOrEmpty(current)) current = "Melee";

                            string[] opts = new[] { "Melee", "Ranged", "Off" };
                            string preview = current;

                            bool opened = _comboAvailable && BeginComboSafe(_cfgSelectedKey, preview);
                            if (opened)
                            {
                                for (int oi = 0; oi < opts.Length; oi++)
                                {
                                    string opt = opts[oi];
                                    bool sel = string.Equals(preview, opt, StringComparison.OrdinalIgnoreCase);
                                    if (imgui_Selectable($"{opt}##AssistType_{oi}", sel))
                                    {
                                        keyData.Value = opt;
                                        if (values != null)
                                        {
                                            values.Clear();
                                            values.Add(opt);
                                        }
                                        _cfg_Dirty = true;
                                    }
                                }
                                EndComboSafe();
                            }
                            else if (!_comboAvailable)
                            {
                                // Fallback list when combo isn't available in current MQ2Mono
                                imgui_Text("Update MQ2Mono to enable dropdown. Temporary list:");
                                float availX = imgui_GetContentRegionAvailX();
                                float listH = 120f;
                                if (imgui_BeginChild("Cfg_AssistType_Fallback", availX, listH, true))
                                {
                                    // Show current selection and options
                                    if (imgui_Selectable($"Current: {preview}##AssistType_Current", false)) { /* no-op */ }
                                    for (int oi = 0; oi < opts.Length; oi++)
                                    {
                                        string opt = opts[oi];
                                        bool sel = string.Equals(preview, opt, StringComparison.OrdinalIgnoreCase);
                                        if (imgui_Selectable($"{opt}##AssistType_FB_{oi}", sel))
                                        {
                                            keyData.Value = opt;
                                            if (values != null)
                                            {
                                                values.Clear();
                                                values.Add(opt);
                                            }
                                            _cfg_Dirty = true;
                                        }
                                    }
                                }
                                imgui_EndChild();
                            }
                        }
                        else
                        {
                            // Default generic editor (list values)
                            var values = keyData.ValueList;
                            float listH = Math.Max(120f, imgui_GetContentRegionAvailY() * 0.6f);
                            if (imgui_BeginChild("Cfg_Values", imgui_GetContentRegionAvailX(), listH, true))
                            {
                                if (values != null && values.Count > 0)
                                {
                                    // Reset inline edit state if selection changed
                                    string curSig = ($"{_cfgSelectedSection}::{_cfgSelectedKey}") ?? string.Empty;
                                    if (!string.Equals(_cfgInlineEditKeySig, curSig, StringComparison.Ordinal))
                                    {
                                        _cfgInlineEditKeySig = string.Empty;
                                        _cfgInlineEditIndex = -1;
                                        _cfgInlineEditBuffer = string.Empty;
                                    }
                                    for (int i = 0; i < values.Count; i++)
                                    {
                                        string v = values[i] ?? string.Empty;
                                        bool sel = (_cfgSelectedValueIndex == i);
                                        if (_cfgInlineEditIndex == i && string.Equals(_cfgInlineEditKeySig, curSig, StringComparison.Ordinal))
                                        {
                                            string id = $"##Cfg_Edit_{i}";
                                            if (imgui_InputText(id, _cfgInlineEditBuffer))
                                            {
                                                _cfgInlineEditBuffer = imgui_InputText_Get(id) ?? string.Empty;
                                            }
                                            imgui_SameLine();
                                            if (imgui_Button($"Save##Edit_{i}"))
                                            {
                                                values[i] = _cfgInlineEditBuffer ?? string.Empty;
                                                _cfg_Dirty = true;
                                                _cfgInlineEditKeySig = string.Empty;
                                                _cfgInlineEditIndex = -1;
                                                _cfgInlineEditBuffer = string.Empty;
                                            }
                                            imgui_SameLine();
                                            if (imgui_Button($"Cancel##Edit_{i}"))
                                            {
                                                _cfgInlineEditKeySig = string.Empty;
                                                _cfgInlineEditIndex = -1;
                                                _cfgInlineEditBuffer = string.Empty;
                                            }
                                        }
                                        else
                                        {
                                            if (imgui_Selectable($"{i + 1}. {v}", sel)) _cfgSelectedValueIndex = i;
                                        }
                                        // Right-click context menu for this item
                                        string ctxId = $"CfgValCtx_{i}";
                                        if (BeginPopupContextItemSafe(ctxId))
                                        {
                                            if (MenuItemSafe("Edit"))
                                            {
                                                _cfgInlineEditIndex = i;
                                                _cfgInlineEditKeySig = curSig;
                                                _cfgInlineEditBuffer = v ?? string.Empty;
                                            }
                                            if (MenuItemSafe("Add"))
                                            {
                                                bool isFoodDrink3 = string.Equals(_cfgSelectedSection, "Misc", StringComparison.OrdinalIgnoreCase) &&
                                                                    (string.Equals(_cfgSelectedKey, "Food", StringComparison.OrdinalIgnoreCase) || string.Equals(_cfgSelectedKey, "Drink", StringComparison.OrdinalIgnoreCase));
                                                bool canAddFromCatalog3 = _cfg_CatalogsReady && !isFoodDrink3;
                                                if (isFoodDrink3)
                                                {
                                                    string keyToScan3 = _cfgSelectedKey;
                                                    EnqueueUI(() =>
                                                    {
                                                        _cfgFoodDrinkKey = keyToScan3;
                                                        _cfgFoodDrinkScanRequested = true;
                                                        _cfgFoodDrinkStatus = "Scanning inventory...";
                                                        _cfgShowFoodDrinkModal = true;
                                                    });
                                                }
                                                else if (canAddFromCatalog3)
                                                {
                                                    _cfgShowAddModal = true; _cfgAddIsEditMode = false; _cfgAddType = AddType.Spells; _cfgAddCategory = string.Empty; _cfgAddSubcategory = string.Empty;
                                                    _cfgAddWithIf = false; _cfgAddIfName = string.Empty;
                                                }
                                            }
                                            if (MenuItemSafe("Remove"))
                                            {
                                                if (i >= 0 && i < values.Count)
                                                {
                                                    values.RemoveAt(i);
                                                    if (_cfgSelectedValueIndex == i) _cfgSelectedValueIndex = -1;
                                                    _cfg_Dirty = true;
                                                }
                                            }
                                            EndPopupSafe();
                                        }
                                    }
                                }
                                else
                                {
                                    imgui_Text("(No values)");
                                }
                            }
                            imgui_EndChild();

                            // Context menu on empty space in values pane for quick Add
                            if (BeginPopupContextWindowSafe("CfgValPaneCtx"))
                            {
                                if (MenuItemSafe("Add"))
                                {
                                    bool isFoodDrink4 = string.Equals(_cfgSelectedSection, "Misc", StringComparison.OrdinalIgnoreCase) &&
                                                        (string.Equals(_cfgSelectedKey, "Food", StringComparison.OrdinalIgnoreCase) || string.Equals(_cfgSelectedKey, "Drink", StringComparison.OrdinalIgnoreCase));
                                    bool canAddFromCatalog4 = _cfg_CatalogsReady && !isFoodDrink4;
                                    if (isFoodDrink4)
                                    {
                                        string keyToScan4 = _cfgSelectedKey;
                                        EnqueueUI(() =>
                                        {
                                            _cfgFoodDrinkKey = keyToScan4;
                                            _cfgFoodDrinkScanRequested = true;
                                            _cfgFoodDrinkStatus = "Scanning inventory...";
                                            _cfgShowFoodDrinkModal = true;
                                        });
                                    }
                                    else if (canAddFromCatalog4)
                                    {
                                        _cfgShowAddModal = true; _cfgAddIsEditMode = false; _cfgAddType = AddType.Spells; _cfgAddCategory = string.Empty; _cfgAddSubcategory = string.Empty;
                                        _cfgAddWithIf = false; _cfgAddIfName = string.Empty;
                                    }
                                }
                                EndPopupSafe();
                            }

                            // Manual input for non-boolean, no-dropdown keys
                            try
                            {
                                string keySig = ($"{_cfgSelectedSection}::{_cfgSelectedKey}") ?? string.Empty;
                                if (!string.Equals(_cfgManualInputKeySig, keySig, StringComparison.OrdinalIgnoreCase))
                                {
                                    _cfgManualInputKeySig = keySig;
                                    // Seed manual input with current single value if present
                                    string seed = keyData.Value ?? string.Empty;
                                    if (values != null && values.Count > 0) seed = values[0] ?? seed;
                                    _cfgManualInput = seed;
                                }
                                imgui_Text("Manual input (exact value)");
                                if (imgui_InputText("##Cfg_ManualInput", _cfgManualInput))
                                {
                                    _cfgManualInput = imgui_InputText_Get("##Cfg_ManualInput") ?? string.Empty;
                                }
                                string typed = (_cfgManualInput ?? string.Empty).Trim();
                                bool hasTyped = !string.IsNullOrEmpty(typed);

                                // Add Manual: use existing helper to respect single vs list behavior
                                if (imgui_Button("Add Manual") && hasTyped)
                                {
                                    AddValueToActiveIni(typed);
                                    _cfg_Dirty = true;
                                    // Clear manual input after successful add
                                    _cfgManualInput = string.Empty;
                                    _cfgManualInputKeySig = ($"{_cfgSelectedSection}::{_cfgSelectedKey}");
                                }
                                imgui_SameLine();
                                // Set Manual: update selected list value if one is selected; otherwise set single value
                                if (imgui_Button("Set Manual") && hasTyped)
                                {
                                    if (values != null && values.Count > 0 && _cfgSelectedValueIndex >= 0 && _cfgSelectedValueIndex < values.Count)
                                    {
                                        values[_cfgSelectedValueIndex] = typed;
                                    }
                                    else if (values != null && values.Count > 0)
                                    {
                                        // No selection; replace first
                                        values[0] = typed;
                                    }
                                    else
                                    {
                                        keyData.Value = typed;
                                        if (values != null && values.Count == 0)
                                        {
                                            values.Add(typed);
                                        }
                                    }
                                    _cfg_Dirty = true;
                                    // Clear manual input after set to avoid sticky carryover
                                    _cfgManualInput = string.Empty;
                                    _cfgManualInputKeySig = ($"{_cfgSelectedSection}::{_cfgSelectedKey}");
                                }

                                // Visual separator between Manual Inputs and Append If's
                                imgui_Separator();
                                // Append If's to selected or current value
                                // Gather If names from current ini
                                var pdIfs = GetActiveCharacterIniData();
                                var ifSec = pdIfs?.Sections?.GetSectionData("Ifs");
                                string[] ifNames2 = ifSec != null ? ifSec.Keys.Select(k => k.KeyName).ToArray() : Array.Empty<string>();
                                if (ifNames2.Length > 0)
                                {
                                    string previewIf = string.IsNullOrEmpty(_cfgAppendIfName) ? ifNames2[0] : _cfgAppendIfName;
                                    if (BeginComboSafe("Append If's##Editor", previewIf))
                                    {
                                        for (int ii = 0; ii < ifNames2.Length; ii++)
                                        {
                                            string nm = ifNames2[ii];
                                            bool sel = string.Equals(_cfgAppendIfName, nm, StringComparison.OrdinalIgnoreCase);
                                            if (imgui_Selectable($"{nm}##AppendIf_{ii}", sel)) _cfgAppendIfName = nm;
                                        }
                                        EndComboSafe();
                                    }
                                    // Place buttons on the same line, below the dropdown
                                    if (imgui_Button("Append to Selected"))
                                    {
                                        string ifNameToUse = string.IsNullOrEmpty(_cfgAppendIfName) ? previewIf : _cfgAppendIfName;
                                        if (!string.IsNullOrEmpty(ifNameToUse))
                                        {
                                            if (values != null && values.Count > 0 && _cfgSelectedValueIndex >= 0 && _cfgSelectedValueIndex < values.Count)
                                            {
                                                string cur = values[_cfgSelectedValueIndex] ?? string.Empty;
                                                values[_cfgSelectedValueIndex] = ReplaceIfSuffix(cur, ifNameToUse);
                                                _cfg_Dirty = true;
                                            }
                                            else if (values != null && values.Count > 0)
                                            {
                                                values[0] = ReplaceIfSuffix(values[0] ?? string.Empty, ifNameToUse);
                                                _cfg_Dirty = true;
                                            }
                                            else
                                            {
                                                string cur = keyData.Value ?? string.Empty;
                                                keyData.Value = ReplaceIfSuffix(cur, ifNameToUse);
                                                if (values != null && values.Count == 0) values.Add(keyData.Value);
                                                _cfg_Dirty = true;
                                            }
                                        }
                                    }
                                    imgui_SameLine();
                                    if (imgui_Button("Remove If's"))
                                    {
                                        if (values != null && values.Count > 0 && _cfgSelectedValueIndex >= 0 && _cfgSelectedValueIndex < values.Count)
                                        {
                                            values[_cfgSelectedValueIndex] = RemoveIfSuffix(values[_cfgSelectedValueIndex] ?? string.Empty);
                                            _cfg_Dirty = true;
                                        }
                                        else if (values != null && values.Count > 0)
                                        {
                                            values[0] = RemoveIfSuffix(values[0] ?? string.Empty);
                                            _cfg_Dirty = true;
                                        }
                                        else
                                        {
                                            keyData.Value = RemoveIfSuffix(keyData.Value ?? string.Empty);
                                            if (values != null && values.Count == 0) values.Add(keyData.Value);
                                            _cfg_Dirty = true;
                                        }
                                    }
                                }
                            }
                            catch { /* manual input UI safe-guard */ }

                            // Special Add handling for Food/Drink: scan inventory instead of catalogs
                            bool isFoodDrink = string.Equals(_cfgSelectedSection, "Misc", StringComparison.OrdinalIgnoreCase) &&
                                                (string.Equals(_cfgSelectedKey, "Food", StringComparison.OrdinalIgnoreCase) || string.Equals(_cfgSelectedKey, "Drink", StringComparison.OrdinalIgnoreCase));
                            bool canAddFromCatalog = _cfg_CatalogsReady && !isFoodDrink;
                            if (!canAddFromCatalog)
                            {
                                imgui_Text("(Load catalogs to enable Add from Spells/AAs/etc)");
                            }
                            if (imgui_Button("Add"))
                            {
                                if (isFoodDrink)
                                {
                                    string keyToScan = _cfgSelectedKey;
                                    EnqueueUI(() =>
                                    {
                                        _cfgFoodDrinkKey = keyToScan;
                                        _cfgFoodDrinkScanRequested = true;
                                        _cfgFoodDrinkStatus = "Scanning inventory...";
                                        _cfgShowFoodDrinkModal = true;
                                    });
                                }
                                else if (canAddFromCatalog)
                                {
                                    _cfgShowAddModal = true; _cfgAddIsEditMode = false; _cfgAddType = AddType.Spells; _cfgAddCategory = string.Empty; _cfgAddSubcategory = string.Empty;
                                    _cfgAddWithIf = false; _cfgAddIfName = string.Empty;
                                }
                            }
                            // Keep Add/Delete inline
                            imgui_SameLine();
                            if (imgui_Button("Delete") && _cfgSelectedValueIndex >= 0 && values != null && _cfgSelectedValueIndex < values.Count)
                            {
                                values.RemoveAt(_cfgSelectedValueIndex);
                                _cfgSelectedValueIndex = -1;
                                _cfg_Dirty = true;
                            }
                            // Right-click a value to Edit/Add/Remove via context menu
                            // If's: offer to pick from bundled samples (inline with Add/Delete)
                            if (string.Equals(_cfgSelectedSection, "Ifs", StringComparison.OrdinalIgnoreCase))
                            {
                                imgui_SameLine();
                                if (imgui_Button("Sample If's"))
                                {
                                    try
                                    {
                                        LoadSampleIfsForModal();
                                        _cfgShowIfSampleModal = true;
                                    }
                                    catch (Exception ex)
                                    {
                                        _cfgIfSampleStatus = "Load failed: " + (ex.Message ?? "error");
                                        _cfgShowIfSampleModal = true;
                                    }
                                }
                            }
                            // Save moved to global button at bottom of the window
                        }
                    }
                    else
                    {
                        if (!isIfsSection)
                            imgui_Text("Select a subsection.");
                    }
                }
            }
            imgui_EndChild();

            // Food/Drink modal
            if (_cfgShowFoodDrinkModal)
            {
                string title = $"Select {_cfgFoodDrinkKey}";
                if (imgui_Begin(title, (int)ImGuiWindowFlags.ImGuiWindowFlags_AlwaysAutoResize))
                {
                    float h = 260f; float w = 520f;
                    if (imgui_BeginChild("FoodDrinkList", w, h, true))
                    {
                        if (_cfgFoodDrinkScanning)
                        {
                            imgui_Text(string.IsNullOrEmpty(_cfgFoodDrinkStatus) ? "Scanning inventory..." : _cfgFoodDrinkStatus);
                        }
                        // Clean list appearance: one selectable row per item
                        for (int i = 0; i < _cfgFoodDrinkCandidates.Count; i++)
                        {
                            string name = _cfgFoodDrinkCandidates[i];
                            if (imgui_Selectable($"{name}##FD_{i}", false))
                            {
                                var pd2 = GetActiveCharacterIniData();
                                var sec2 = pd2?.Sections?.GetSectionData(_cfgSelectedSection ?? string.Empty);
                                var kd2 = sec2?.Keys?.GetKeyData(_cfgSelectedKey ?? string.Empty);
                                if (kd2 != null)
                                {
                                    kd2.Value = name;
                                    if (kd2.ValueList != null) { kd2.ValueList.Clear(); kd2.ValueList.Add(name); }
                                    _cfg_Dirty = true;
                                    // Clear manual input for this key to avoid sticky carryover
                                    _cfgManualInput = string.Empty;
                                    _cfgManualInputKeySig = ($"{_cfgSelectedSection}::{_cfgSelectedKey}");
                                }
                                _cfgShowFoodDrinkModal = false;
                                break; // ensure only a single selection applies
                            }
                        }
                    }
                    imgui_EndChild();
                    if (imgui_Button("Close")) _cfgShowFoodDrinkModal = false;
                }
                imgui_End();
            }

            // If's sample modal
            if (_cfgShowIfSampleModal)
            {
                if (imgui_Begin("Sample If's", (int)ImGuiWindowFlags.ImGuiWindowFlags_AlwaysAutoResize))
                {
                    if (!string.IsNullOrEmpty(_cfgIfSampleStatus)) imgui_Text(_cfgIfSampleStatus);
                    float h = 300f; float w = 640f;
                    if (imgui_BeginChild("IfsSampleList", w, h, true))
                    {
                        for (int i = 0; i < _cfgIfSampleLines.Count; i++)
                        {
                            var kv = _cfgIfSampleLines[i];
                            string display = string.IsNullOrEmpty(kv.Value) ? kv.Key : (kv.Key + " = " + kv.Value);
                            if (imgui_Selectable($"{display}##IF_{i}", false))
                            {
                                AddIfToActiveIni(kv.Key, kv.Value);
                                // keep modal open to allow adding multiple
                            }
                        }
                    }
                    imgui_EndChild();
                    imgui_SameLine();
                    if (imgui_Button("Import All"))
                    {
                        int cnt = 0;
                        for (int i = 0; i < _cfgIfSampleLines.Count; i++) { var kv = _cfgIfSampleLines[i]; if (AddIfToActiveIni(kv.Key, kv.Value)) cnt++; }
                        _cfgIfSampleStatus = cnt > 0 ? ($"Imported {cnt} If(s)") : "No new If's to import.";
                    }
                    imgui_SameLine();
                    if (imgui_Button("Close")) _cfgShowIfSampleModal = false;
                }
                imgui_End();
            }

            // Add modal (simple window)
                if (_cfgShowAddModal)
                {
                string modalTitle = _cfgAddIsEditMode ? "Edit Entry" : "Add Entry";
                if (imgui_Begin(modalTitle, (int)ImGuiWindowFlags.ImGuiWindowFlags_AlwaysAutoResize))
                {
                    // Type tabs
                    if (imgui_Button(_cfgAddType == AddType.Spells ? "> Spells" : "Spells")) _cfgAddType = AddType.Spells;
                    imgui_SameLine();
                    if (imgui_Button(_cfgAddType == AddType.AAs ? "> AAs" : "AAs")) _cfgAddType = AddType.AAs;
                    imgui_SameLine();
                    if (imgui_Button(_cfgAddType == AddType.Discs ? "> Discs" : "Discs")) _cfgAddType = AddType.Discs;
                    imgui_SameLine();
                    if (imgui_Button(_cfgAddType == AddType.Skills ? "> Skills" : "Skills")) _cfgAddType = AddType.Skills;
                    imgui_SameLine();
                    if (imgui_Button(_cfgAddType == AddType.Items ? "> Items" : "Items")) _cfgAddType = AddType.Items;

                    // Optional If's appending for added entries
                    var pdIf = GetActiveCharacterIniData();
                    var ifSection = pdIf?.Sections?.GetSectionData("Ifs");
                    string[] ifNames = ifSection != null ? ifSection.Keys.Select(k => k.KeyName).ToArray() : Array.Empty<string>();
                    bool appendChecked = imgui_Checkbox("Append If's", _cfgAddWithIf);
                    if (appendChecked != _cfgAddWithIf) { _cfgAddWithIf = appendChecked; }
                    if (_cfgAddWithIf)
                    {
                        imgui_SameLine();
                        string ifPreview = string.IsNullOrEmpty(_cfgAddIfName) ? (ifNames.Length > 0 ? ifNames[0] : "") : _cfgAddIfName;
                        bool ifCombo = _comboAvailable && BeginComboSafe("If Name", ifPreview);
                        if (ifCombo)
                        {
                            for (int ii = 0; ii < ifNames.Length; ii++)
                            {
                                string nm = ifNames[ii];
                                bool sel = string.Equals(_cfgAddIfName, nm, StringComparison.OrdinalIgnoreCase);
                                if (imgui_Selectable($"{nm}##IfNm_{ii}", sel)) _cfgAddIfName = nm;
                            }
                            EndComboSafe();
                        }
                        if (ifNames.Length == 0)
                        {
                            // Fallback manual entry for If name
                            if (imgui_InputText("Add_IfName", _cfgAddIfName))
                                _cfgAddIfName = imgui_InputText_Get("Add_IfName") ?? string.Empty;
                        }
                    }

                    var catalog = GetActiveCatalog();

                    if (_cfgAddType == AddType.Spells || _cfgAddType == AddType.AAs || _cfgAddType == AddType.Discs)
                    {
                        // Category/Subcategory for Spells/AAs/Discs
                        string[] cats = catalog.Keys.ToArray();
                        string cat = string.IsNullOrEmpty(_cfgAddCategory) ? (cats.Length > 0 ? cats[0] : string.Empty) : _cfgAddCategory;
                        if (imgui_BeginCombo("Category", cat, 0))
                        {
                            for (int ci = 0; ci < cats.Length; ci++)
                            {
                                var c = cats[ci];
                                bool sel = (c == cat);
                                if (imgui_Selectable($"{c}##AddCat_{ci}", sel)) { _cfgAddCategory = c; _cfgAddSubcategory = string.Empty; }
                            }
                            imgui_EndCombo();
                        }
                        var submap = (catalog.TryGetValue(cat, out var sm) ? sm : new SortedDictionary<string, List<E3Core.Data.Spell>>());
                        string[] subs = submap.Keys.ToArray();
                        string sub = string.IsNullOrEmpty(_cfgAddSubcategory) ? (subs.Length > 0 ? subs[0] : string.Empty) : _cfgAddSubcategory;
                        if (imgui_BeginCombo("Subcategory", sub, 0))
                        {
                            for (int si = 0; si < subs.Length; si++)
                            {
                                var sname = subs[si];
                                bool sel = (sname == sub);
                                if (imgui_Selectable($"{sname}##AddSub_{si}", sel)) { _cfgAddSubcategory = sname; }
                            }
                            imgui_EndCombo();
                        }

                        if (submap.TryGetValue(string.IsNullOrEmpty(_cfgAddSubcategory) ? sub : _cfgAddSubcategory, out var list))
                        {
                            float h = 240f; float w = 520f;
                            if (imgui_BeginChild("AddList", w, h, true))
                            {
                                for (int i = 0; i < list.Count; i++)
                                {
                                    var s = list[i];
                                    string display = $"{s.SpellName} [{s.Level}]";
                                    int uniqueId = s.SpellID;
                                    string id = $"{display}##AddSel_{(int)_cfgAddType}_{i}_{uniqueId}";
                                    if (imgui_Selectable(id, false))
                                    {
                                        string toAdd = s.CastName;
                                        if (_cfgAddWithIf && !string.IsNullOrEmpty(_cfgAddIfName)) toAdd = toAdd + "/Ifs|" + _cfgAddIfName;
                                        AddValueToActiveIni(toAdd);
                                    }
                                }
                            }
                            imgui_EndChild();
                        }
                    }
                    else
                    {
                        // Items or Skills: no category/subcategory — show a flat list
                        var flat = new List<E3Core.Data.Spell>();
                        foreach (var catKvp in catalog)
                        {
                            var submap = catKvp.Value;
                            foreach (var subKvp in submap)
                            {
                                var lst = subKvp.Value;
                                if (lst != null) flat.AddRange(lst);
                            }
                        }
                        float h = 240f; float w = 520f;
                        if (imgui_BeginChild("AddList", w, h, true))
                        {
                            for (int i = 0; i < flat.Count; i++)
                            {
                                var s = flat[i];
                                string display;
                                int uniqueId;
                                if (_cfgAddType == AddType.Items)
                                {
                                    display = $"{s.CastName} [{s.SpellName}]"; // item name with click effect
                                    uniqueId = s.CastID > 0 ? s.CastID : (s.SpellID != 0 ? s.SpellID : i);
                                }
                                else
                                {
                                    // Skills: just list the skill/spell name (level often 0)
                                    display = s.SpellName;
                                    uniqueId = s.SpellID != 0 ? s.SpellID : i;
                                }
                                string id = $"{display}##AddSel_{(int)_cfgAddType}_{i}_{uniqueId}";
                                if (imgui_Selectable(id, false))
                                {
                                    string toAdd = s.CastName;
                                    if (_cfgAddWithIf && !string.IsNullOrEmpty(_cfgAddIfName)) toAdd = toAdd + "/Ifs|" + _cfgAddIfName;
                                    AddValueToActiveIni(toAdd);
                                }
                            }
                        }
                        imgui_EndChild();
                    }
                    if (imgui_Button("Close")) { _cfgShowAddModal = false; _cfgAddIsEditMode = false; _cfgAddEditTargetIndex = -1; _cfgAddEditOriginal = string.Empty; }
                }
                imgui_End();
            }
        }
        private static bool IsBooleanConfigKey(string keyName, KeyData keyData)
        {
            try
            {
                string k = keyName ?? string.Empty;
                if (k.IndexOf("(On/Off)", StringComparison.OrdinalIgnoreCase) >= 0) return true;
                if (k.IndexOf("(true/false)", StringComparison.OrdinalIgnoreCase) >= 0) return true;
                string v = keyData?.Value ?? string.Empty;
                if (string.IsNullOrEmpty(v) && keyData?.ValueList != null && keyData.ValueList.Count > 0)
                    v = keyData.ValueList[0];
                if (string.Equals(v, "On", StringComparison.OrdinalIgnoreCase)) return true;
                if (string.Equals(v, "Off", StringComparison.OrdinalIgnoreCase)) return true;
                if (string.Equals(v, "True", StringComparison.OrdinalIgnoreCase)) return true;
                if (string.Equals(v, "False", StringComparison.OrdinalIgnoreCase)) return true;
            }
            catch { }
            return false;
        }
        private static SortedDictionary<string, SortedDictionary<string, List<E3Core.Data.Spell>>> GetActiveCatalog()
        {
            switch (_cfgAddType)
            {
                case AddType.AAs: return _cfgAAs;
                case AddType.Discs: return _cfgDiscs;
                case AddType.Skills: return _cfgSkills;
                case AddType.Items: return _cfgItems;
                case AddType.Spells:
                default: return _cfgSpells;
            }
        }
        private static void AddValueToActiveIni(string value)
        {
            try
            {
                var pd = GetActiveCharacterIniData();
                if (pd == null) return;
                var section = pd.Sections.GetSectionData(_cfgSelectedSection ?? string.Empty);
                if (section == null) return;
                var keyData = section.Keys.GetKeyData(_cfgSelectedKey ?? string.Empty);
                if (keyData == null)
                {
                    section.Keys.AddKey(_cfgSelectedKey);
                    keyData = section.Keys.GetKeyData(_cfgSelectedKey);
                }
                // Edit mode: replace an existing entry instead of adding
                if (_cfgAddIsEditMode)
                {
                    string newVal = value ?? string.Empty;
                    // If the edit modal has an If specified, honor it; otherwise preserve existing suffix
                    string explicitIf = _cfgAddWithIf ? (_cfgAddIfName ?? string.Empty) : string.Empty;
                    if (!string.IsNullOrEmpty(explicitIf))
                    {
                        newVal = ReplaceIfSuffix(newVal, explicitIf);
                    }
                    else
                    {
                        string existingIf = ExtractIfSuffixName(_cfgAddEditOriginal ?? string.Empty);
                        if (!string.IsNullOrEmpty(existingIf)) newVal = ReplaceIfSuffix(newVal, existingIf);
                    }

                    if (keyData.ValueList != null && keyData.ValueList.Count > 0)
                    {
                        int idx = _cfgAddEditTargetIndex;
                        if (idx < 0 || idx >= keyData.ValueList.Count) idx = 0;
                        keyData.ValueList[idx] = newVal;
                    }
                    else
                    {
                        keyData.Value = newVal;
                        if (keyData.ValueList != null && keyData.ValueList.Count == 0)
                        {
                            keyData.ValueList.Add(newVal);
                        }
                    }
                    _cfg_Dirty = true;
                    // close and reset edit state
                    _cfgShowAddModal = false;
                    _cfgAddIsEditMode = false;
                    _cfgAddEditTargetIndex = -1;
                    _cfgAddEditOriginal = string.Empty;
                    // Don't carry over If settings unintentionally
                    _cfgAddWithIf = false; _cfgAddIfName = string.Empty;
                }
                else
                {
                    // Add mode: append or set
                    if (keyData.ValueList == null || keyData.ValueList.Count == 0)
                    {
                        keyData.Value = value ?? string.Empty;
                    }
                    else
                    {
                        keyData.ValueList.Add(value ?? string.Empty);
                    }
                    _cfg_Dirty = true;
                    _cfgShowAddModal = false;
                }
            }
            catch { }
        }

        private static string ResolveSampleIfsPath()
        {
            // Directories to search
            var dirs = new System.Collections.Generic.List<string>();
            try
            {
                string cfg = GetActiveSettingsPath();
                if (!string.IsNullOrEmpty(cfg))
                {
                    var dir = System.IO.Path.GetDirectoryName(cfg);
                    if (!string.IsNullOrEmpty(dir)) dirs.Add(dir);
                }
            }
            catch { }
            try
            {
                string botIni = GetCurrentCharacterIniPath();
                if (!string.IsNullOrEmpty(botIni))
                {
                    var botDir = System.IO.Path.GetDirectoryName(botIni);
                    if (!string.IsNullOrEmpty(botDir)) dirs.Add(botDir);
                }
            }
            catch { }
            dirs.Add(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory ?? string.Empty));
            dirs.Add(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory ?? string.Empty, "E3Next"));
            dirs.Add(System.IO.Directory.GetCurrentDirectory());
            dirs.Add(System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "E3Next"));

            // Filenames to try
            string[] names = new string[]
            {
                "sample ifs",
                "sample ifs.txt",
                "Sample Ifs.txt",
                "sample_ifs.txt"
            };

            foreach (var d in dirs)
            {
                if (string.IsNullOrEmpty(d)) continue;
                foreach (var n in names)
                {
                    try
                    {
                        var p = System.IO.Path.Combine(d, n);
                        if (System.IO.File.Exists(p)) return p;
                    }
                    catch { }
                }
                // Last resort: search directory for a file named (case-insensitive) "sample ifs" regardless of extension
                try
                {
                    foreach (var f in System.IO.Directory.EnumerateFiles(d, "*", System.IO.SearchOption.TopDirectoryOnly))
                    {
                        string fn = System.IO.Path.GetFileNameWithoutExtension(f) ?? string.Empty;
                        if (fn.Equals("sample ifs", StringComparison.OrdinalIgnoreCase)) return f;
                    }
                }
                catch { }
            }
            return string.Empty;
        }

        private static void LoadSampleIfsForModal()
        {
            _cfgIfSampleLines.Clear();
            _cfgIfSampleStatus = string.Empty;
            try
            {
                string sample = ResolveSampleIfsPath();
                if (string.IsNullOrEmpty(sample)) { _cfgIfSampleStatus = "Sample file not found."; return; }
                _cfgIfSampleStatus = "Loaded: " + System.IO.Path.GetFileName(sample);
                int added = 0;
                foreach (var raw in System.IO.File.ReadAllLines(sample))
                {
                    var line = (raw ?? string.Empty).Trim();
                    if (line.Length == 0) continue;
                    if (line.StartsWith("#") || line.StartsWith(";")) continue;
                    string key = string.Empty; string val = string.Empty;
                    int eq = line.IndexOf('=');
                    if (eq > 0)
                    {
                        key = (line.Substring(0, eq).Trim());
                        val = (line.Substring(eq + 1).Trim());
                    }
                    else
                    {
                        // Accept other common delimiters: ':' or '-' (first occurrence)
                        int colon = line.IndexOf(':');
                        int dash = line.IndexOf('-');
                        int pos = -1;
                        if (colon > 0) pos = colon; else if (dash > 0) pos = dash;
                        if (pos > 0)
                        {
                            key = line.Substring(0, pos).Trim();
                            val = line.Substring(pos + 1).Trim();
                        }
                        else
                        {
                            // No delimiter; treat as key-only name
                            key = line;
                            val = string.Empty;
                        }
                    }
                    if (!string.IsNullOrEmpty(key))
                    {
                        _cfgIfSampleLines.Add(new System.Collections.Generic.KeyValuePair<string, string>(key, val));
                        added++;
                    }
                }
                if (added == 0) _cfgIfSampleStatus = "No entries found in sample file.";
                if (_cfgIfSampleLines.Count == 0) _cfgIfSampleStatus = "No entries found in sample file.";
            }
            catch (Exception ex)
            {
                _cfgIfSampleStatus = "Error reading sample IFs: " + (ex.Message ?? "error");
            }
        }

        private static bool AddIfToActiveIni(string key, string value)
        {
            try
            {
                var pd = GetActiveCharacterIniData();
                if (pd == null) return false;
                var section = pd.Sections.GetSectionData("Ifs");
                if (section == null)
                {
                    pd.Sections.AddSection("Ifs");
                    section = pd.Sections.GetSectionData("Ifs");
                }
                if (section == null) return false;
                string baseKey = key ?? string.Empty;
                if (string.IsNullOrWhiteSpace(baseKey)) return false;
                string unique = baseKey;
                int idx = 1;
                while (section.Keys.ContainsKey(unique)) { unique = baseKey + " (" + idx.ToString() + ")"; idx++; if (idx > 1000) break; }
                if (!section.Keys.ContainsKey(unique))
                {
                    section.Keys.AddKey(unique, value ?? string.Empty);
                    _cfg_Dirty = true;
                    // Focus selection on the new IF key
                    _cfgSelectedSection = "Ifs";
                    _cfgSelectedKey = unique;
                    _cfgSelectedValueIndex = -1;
                    return true;
                }
                return false;
            }
            catch { return false; }
        }

        private static List<string> ScanFoodDrinkCandidates(string key)
        {
            var list = new List<string>();
            try
            {
                bool wantFood = string.Equals(key, "Food", StringComparison.OrdinalIgnoreCase);
                string owner = GetSelectedIniOwnerName();
                bool local = owner.Equals(E3.CurrentName, StringComparison.OrdinalIgnoreCase);

                if (!local)
                {
                    if (!E3Core.Server.NetMQServer.SharedDataClient.UsersConnectedTo.TryGetValue(owner, out var info) || info.RouterPort <= 0)
                        return list;
                    using (var req = new RequestSocket())
                    {
                        req.Connect($"tcp://127.0.0.1:{info.RouterPort}");
                        string cmd = wantFood ? "${E3.Inventory.ListFood}" : "${E3.Inventory.ListDrink}";
                        string resp = RouterQuery(req, cmd) ?? string.Empty;
                        // Normalize newlines: handle actual CR/LF and literal \r\n or \n coming over the wire
                        string norm = resp.Replace("\r\n", "\n");
                        norm = norm.Replace("\\r\\n", "\n").Replace("\\n", "\n").Replace("\\r", "\n");
                        foreach (var line in norm.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries))
                        {
                            AddUnique(list, line.Trim());
                        }
                    }
                }
                else
                {
                    Func<string, string> Q = (q) => E3.MQ.Query<string>(q);
                    // Top-level inventory slots 0..22
                    for (int i = 0; i <= 22; i++)
                    {
                        string name = Q($"${{Me.Inventory[{i}]}}");
                        if (string.IsNullOrEmpty(name) || string.Equals(name, "NULL", StringComparison.OrdinalIgnoreCase)) continue;
                        string type = Q($"${{Me.Inventory[{i}].Type}}");
                        if (IsTypeMatch(type, wantFood)) AddUnique(list, name);
                    }
                    // Bags pack1..pack12
                    for (int b = 1; b <= 12; b++)
                    {
                        string pack = $"pack{b}";
                        string present = Q($"${{Me.Inventory[{pack}]}}");
                        if (string.IsNullOrEmpty(present) || string.Equals(present, "NULL", StringComparison.OrdinalIgnoreCase)) continue;
                        int slots = 0; int.TryParse(Q($"${{Me.Inventory[{pack}].Container}}"), out slots);
                        if (slots > 0)
                        {
                            for (int j = 1; j <= slots; j++)
                            {
                                string name = Q($"${{Me.Inventory[{pack}].Item[{j}]}}");
                                if (string.IsNullOrEmpty(name) || string.Equals(name, "NULL", StringComparison.OrdinalIgnoreCase)) continue;
                                string type = Q($"${{Me.Inventory[{pack}].Item[{j}].Type}}");
                                if (IsTypeMatch(type, wantFood)) AddUnique(list, name);
                            }
                        }
                        else
                        {
                            // single item in the pack slot
                            string name = present;
                            string type = Q($"${{Me.Inventory[{pack}].Type}}");
                            if (!string.IsNullOrEmpty(name) && !string.Equals(name, "NULL", StringComparison.OrdinalIgnoreCase) && IsTypeMatch(type, wantFood))
                                AddUnique(list, name);
                        }
                    }
                }
            }
            catch { }
            list.Sort(StringComparer.OrdinalIgnoreCase);
            return list;
        }

        private static bool IsTypeMatch(string type, bool wantFood)
        {
            if (string.IsNullOrEmpty(type)) return false;
            if (wantFood) return type.IndexOf("Food", StringComparison.OrdinalIgnoreCase) >= 0;
            return type.IndexOf("Drink", StringComparison.OrdinalIgnoreCase) >= 0;
        }
        private static void AddUnique(List<string> list, string name)
        {
            if (string.IsNullOrEmpty(name)) return;
            if (!list.Contains(name, StringComparer.OrdinalIgnoreCase)) list.Add(name);
        }
        private static string RouterQuery(RequestSocket req, string query)
        {
            try
            {
                var payload = Encoding.Default.GetBytes(query ?? string.Empty);
                byte[] frame = new byte[8 + payload.Length];
                Buffer.BlockCopy(BitConverter.GetBytes(1), 0, frame, 0, 4);
                Buffer.BlockCopy(BitConverter.GetBytes(payload.Length), 0, frame, 4, 4);
                Buffer.BlockCopy(payload, 0, frame, 8, payload.Length);
                req.SendFrame(frame);
                if (!req.TryReceiveFrameBytes(TimeSpan.FromMilliseconds(1000), out var respBytes)) return string.Empty;
                return Encoding.Default.GetString(respBytes ?? Array.Empty<byte>());
            }
            catch { return string.Empty; }
        }
        private static void SaveActiveIniData()
        {
            try
            {
                if (!_cfg_Dirty) return;
                var parser = E3Core.Utility.e3util.CreateIniParser();
                string path = GetActiveSettingsPath();
                var pd = GetActiveCharacterIniData();
                if (!string.IsNullOrEmpty(path) && pd != null)
                {
                    parser.WriteFile(path, pd);
                    _cfg_Dirty = false;
                    _nextIniRefreshAtMs = 0;
                    mq_Echo($"Saved changes to {System.IO.Path.GetFileName(path)}");
                }
            }
            catch (Exception ex) { mq_Echo($"Save failed: {ex.Message}"); }
        }

        // --- All Players aggregated key view ----------------------------------------
        private static void RenderAllPlayersKeyView(string section, string key)
        {
            TryRequestAllPlayersRefresh(section ?? string.Empty, key ?? string.Empty);

            float listH = Math.Max(180f, imgui_GetContentRegionAvailY() * 0.7f);
            if (imgui_BeginChild("Cfg_AllPlayersKey", imgui_GetContentRegionAvailX(), listH, true))
            {
                // Optional status removed for cleaner look per request

                // Take a snapshot under lock to avoid racing with background updater
                List<System.Collections.Generic.KeyValuePair<string, string>> rows;
                Dictionary<string, string> serverMap;
                HashSet<string> remoteSet;
                lock (_cfgAllPlayersLock)
                {
                    rows = new List<System.Collections.Generic.KeyValuePair<string, string>>(_cfgAllPlayersRows);
                    serverMap = new Dictionary<string, string>(_cfgAllPlayersServerByToon, StringComparer.OrdinalIgnoreCase);
                    remoteSet = new HashSet<string>(_cfgAllPlayersIsRemote, StringComparer.OrdinalIgnoreCase);
                }

                if (rows.Count == 0)
                {
                    imgui_Text("No character INIs found in directory.");
                }
                else
                {
                    // Header row for readability
                    imgui_Text("Name    | Current Value                          | New Value");
                    imgui_Separator();

                    // Determine if this key is boolean (match single-view behavior)
                    bool isBoolKey = false;
                    try
                    {
                        var pdActive = GetActiveCharacterIniData();
                        var secActive = pdActive?.Sections?.GetSectionData(section ?? string.Empty);
                        var kdActive = secActive?.Keys?.GetKeyData(key ?? string.Empty);
                        if (kdActive != null)
                        {
                            isBoolKey = IsBooleanConfigKey(key ?? string.Empty, kdActive);
                        }
                    }
                    catch { }

                    foreach (var kv in rows)
                    {
                        string toon = kv.Key ?? string.Empty;
                        string val = kv.Value ?? string.Empty;
                        // Row layout: Name (line 1), Current + New + Set (line 2) with consistent input width
                        imgui_Text(toon);
                        imgui_Text($"Current: {val}");
                        imgui_SameLine();
                        // Inline edit buffer per toon
                        string editKey = "AP_Edit_" + toon;
                        string curBuf = _cfgAllPlayersEditBuf.TryGetValue(toon, out var b) ? (b ?? string.Empty) : string.Empty;
                        if (isBoolKey)
                        {
                            // Normalize current to On/Off
                            bool currentOn = val.Equals("On", StringComparison.OrdinalIgnoreCase) || val.Equals("True", StringComparison.OrdinalIgnoreCase);
                            string preview = string.IsNullOrEmpty(curBuf) ? (currentOn ? "On" : "Off") : curBuf;
                            if (BeginComboSafe(editKey + "_Bool", preview))
                            {
                                string[] opts = new[] { "On", "Off" };
                                for (int oi = 0; oi < opts.Length; oi++)
                                {
                                    string opt = opts[oi];
                                    bool sel = string.Equals(preview, opt, StringComparison.OrdinalIgnoreCase);
                                    if (imgui_Selectable($"{opt}##APBool_{toon}_{oi}", sel))
                                    {
                                        _cfgAllPlayersEditBuf[toon] = opt;
                                        preview = opt;
                                    }
                                }
                                EndComboSafe();
                            }
                        }
                        else
                        {
                            imgui_SetNextItemWidth(260f);
                            if (imgui_InputText(editKey, curBuf))
                            {
                                _cfgAllPlayersEditBuf[toon] = imgui_InputText_Get(editKey) ?? string.Empty;
                            }
                        }
                        imgui_SameLine();
                        if (imgui_Button("Set##AP_" + toon))
                        {
                            string newVal = _cfgAllPlayersEditBuf.TryGetValue(toon, out var nb) ? (nb ?? string.Empty) : string.Empty;
                            if (isBoolKey && string.IsNullOrEmpty(newVal))
                            {
                                // Default to current normalized value if nothing was explicitly chosen
                                bool currentOn = val.Equals("On", StringComparison.OrdinalIgnoreCase) || val.Equals("True", StringComparison.OrdinalIgnoreCase);
                                newVal = currentOn ? "On" : "Off";
                            }
                            string server = serverMap.TryGetValue(toon, out var sv) ? (sv ?? string.Empty) : string.Empty;
                            if (!string.IsNullOrWhiteSpace(newVal) && !string.IsNullOrWhiteSpace(server))
                            {
                                bool isRemote = remoteSet.Contains(toon) && !string.Equals(toon, E3.CurrentName, StringComparison.OrdinalIgnoreCase);
                                if (isRemote)
                                {
                                    string iniRel = $"e3 Bot Inis\\{toon}_{server}.ini";
                                    string cmd = $"/ini \"{iniRel}\" \"{section}\" \"{key}\" \"{newVal}\"";
                                    string targetToon = toon; // capture for closure
                                    EnqueueUI(() =>
                                    {
                                        try { E3Core.Processors.E3.Bots.BroadcastCommandToPerson(targetToon, cmd); }
                                        catch { }
                                    });
                                }
                                else
                                {
                                    // Local update: write to disk and refresh
                                    TrySetLocalIniValue(toon, server, section, key, newVal);
                                }
                                // force refresh next frame
                                _cfgAllPlayersNextRefreshAtMs = 0;
                            }
                        }
                        imgui_Separator();
                    }
                }
            }
            imgui_EndChild();
        }

        private static void TryRequestAllPlayersRefresh(string section, string key, bool force = false)
        {
            try
            {
                string sig = (section ?? string.Empty) + "::" + (key ?? string.Empty);
                long now = Core.StopWatch.ElapsedMilliseconds;
                bool sigChanged = !string.Equals(sig, _cfgAllPlayersSig, StringComparison.Ordinal);
                bool timeOk = (now - _cfgAllPlayersLastUpdatedAt) >= _cfgAllPlayersRefreshIntervalMs;
                if (force || sigChanged || timeOk)
                {
                    _cfgAllPlayersReqSection = section ?? string.Empty;
                    _cfgAllPlayersReqKey = key ?? string.Empty;
                    _cfgAllPlayersRefreshRequested = true;
                }
            }
            catch { }
        }

        private static void RefreshAllPlayersCacheIfNeeded(string section, string key)
        {
            try
            {
                string sig = (section ?? string.Empty) + "::" + (key ?? string.Empty);
                long now = Core.StopWatch.ElapsedMilliseconds;
                if (string.Equals(sig, _cfgAllPlayersSig, StringComparison.Ordinal) && now < _cfgAllPlayersNextRefreshAtMs) return;

                _cfgAllPlayersSig = sig;
                _cfgAllPlayersNextRefreshAtMs = now + 1000; // 1s throttle
                // Do not clear live data here; build a new snapshot and swap in atomically to avoid flicker.

                // Build set of files to scan
                ScanCharIniFilesIfNeeded();
                var files = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var f in _charIniFiles) if (!string.IsNullOrEmpty(f)) files.Add(f);
                var cur = GetCurrentCharacterIniPath();
                if (!string.IsNullOrEmpty(cur)) files.Add(cur);

                // Aggregate by toon name
                var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                var tmpServerMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                var tmpRemoteSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                // Local: Read each ini and extract value(s)
                foreach (var f in files)
                {
                    try
                    {
                        string fileName = System.IO.Path.GetFileName(f) ?? string.Empty;
                        if (string.IsNullOrEmpty(fileName)) continue;
                        // Extract toon name from file name: Name_Server.ini
                        string toon = fileName;
                        int us = toon.IndexOf('_');
                        string server = string.Empty;
                        if (us > 0)
                        {
                            server = fileName.Substring(us + 1);
                            int dot = server.LastIndexOf('.');
                            if (dot > 0) server = server.Substring(0, dot);
                            toon = toon.Substring(0, us);
                        }

                        var parser = E3Core.Utility.e3util.CreateIniParser();
                        var pd = parser.ReadFile(f);
                        var sec = pd?.Sections?.GetSectionData(section);
                        var kd = sec?.Keys?.GetKeyData(key);
                        string val = string.Empty;
                        if (kd != null)
                        {
                            if (kd.ValueList != null && kd.ValueList.Count > 0)
                            {
                                val = string.Join(" | ", kd.ValueList.Where(x => !string.IsNullOrEmpty(x)));
                            }
                            else
                            {
                                val = kd.Value ?? string.Empty;
                            }
                        }
                        else
                        {
                            val = "(missing)";
                        }
                        map[toon] = val;
                        if (!string.IsNullOrEmpty(server)) tmpServerMap[toon] = server;
                    }
                    catch { /* ignore individual ini read errors */ }
                }

                // Remote: Query connected toons via Router for server and ini value
                try
                {
                    foreach (var kv in E3Core.Server.NetMQServer.SharedDataClient.UsersConnectedTo)
                    {
                        string toon = kv.Key;
                        var info = kv.Value;
                        if (info == null || info.RouterPort <= 0) continue;
                        using (var req = new RequestSocket())
                        {
                            req.Connect($"tcp://127.0.0.1:{info.RouterPort}");
                            // Bulk mode to reduce round trips
                            RouterQuery(req, "${E3.TLO.BulkBegin}");

                            string server;
                            if (!tmpServerMap.TryGetValue(toon, out server) || string.IsNullOrEmpty(server))
                            {
                                server = RouterQuery(req, "${IniServerName}") ?? string.Empty;
                                if (!string.IsNullOrEmpty(server)) tmpServerMap[toon] = server;
                            }

                            string iniRel = $"e3 Bot Inis\\{toon}_{server}.ini";
                            // Use quotes for section/key because of spaces
                            string query = "${Ini[\"" + iniRel + "\",\"" + section + "\",\"" + key + "\"]}";
                            string resp = RouterQuery(req, query) ?? string.Empty;

                            RouterQuery(req, "${E3.TLO.BulkEnd}");

                            if (string.IsNullOrEmpty(resp) || string.Equals(resp, "NULL", StringComparison.OrdinalIgnoreCase)) resp = "(missing)";
                            map[toon] = resp;
                            tmpRemoteSet.Add(toon);
                        }
                    }
                }
                catch { }

                // Build rows list and swap into live snapshot under lock
                var tmpRows = new List<System.Collections.Generic.KeyValuePair<string, string>>();
                foreach (var pair in map)
                {
                    tmpRows.Add(new System.Collections.Generic.KeyValuePair<string, string>(pair.Key, pair.Value));
                }
                tmpRows.Sort((a, b) => string.Compare(a.Key, b.Key, StringComparison.OrdinalIgnoreCase));
                lock (_cfgAllPlayersLock)
                {
                    _cfgAllPlayersRows = tmpRows;
                    _cfgAllPlayersServerByToon = tmpServerMap;
                    _cfgAllPlayersIsRemote = tmpRemoteSet;
                }
            }
            catch { /* ignore aggregate errors */ }
        }

        private static void TrySetLocalIniValue(string toon, string server, string section, string key, string value)
        {
            try
            {
                if (string.IsNullOrEmpty(toon) || string.IsNullOrEmpty(server)) return;
                string currentCharIni = GetCurrentCharacterIniPath();
                string dir = System.IO.Path.GetDirectoryName(currentCharIni) ?? string.Empty;
                if (string.IsNullOrEmpty(dir)) return;
                string path = System.IO.Path.Combine(dir, toon + "_" + server + ".ini");
                var parser = E3Core.Utility.e3util.CreateIniParser();
                IniData pd;
                if (System.IO.File.Exists(path)) pd = parser.ReadFile(path); else pd = new IniData();
                var sec = pd.Sections.GetSectionData(section) ?? new SectionData(section);
                if (!pd.Sections.ContainsSection(section)) pd.Sections.Add(sec);
                var keys = sec.Keys;
                var kd = keys.GetKeyData(key);
                if (kd == null)
                {
                    keys.AddKey(key, value ?? string.Empty);
                }
                else
                {
                    kd.Value = value ?? string.Empty;
                    kd.ValueList?.Clear();
                }
                parser.WriteFile(path, pd);
                // If editing our own active ini, update in-memory and mark dirty for save if needed
                if (string.Equals(toon, E3Core.Processors.E3.CurrentName, StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        var live = GetActiveCharacterIniData();
                        if (live != null)
                        {
                            var lsec = live.Sections.GetSectionData(section);
                            if (lsec == null)
                            {
                                live.Sections.AddSection(section);
                                lsec = live.Sections.GetSectionData(section);
                            }
                            var lkd = lsec.Keys.GetKeyData(key);
                            if (lkd == null) lsec.Keys.AddKey(key, value ?? string.Empty); else lkd.Value = value ?? string.Empty;
                            _cfg_Dirty = true;
                            _nextIniRefreshAtMs = 0;
                        }
                    }
                    catch { }
                }
            }
            catch { }
        }

        private static void RenderSpellDataTab()
        {
            imgui_Text("Lookup by Spell Name or ID");
            imgui_SameLine();
            if (imgui_InputText("##SpellQuery", _spellQueryInput))
            {
                _spellQueryInput = imgui_InputText_Get("##SpellQuery") ?? string.Empty;
            }
            imgui_SameLine();
            if (imgui_Button("Load"))
            {
                TryLoadSpell(_spellQueryInput);
            }

            if (_spellLoaded != null)
            {
                // Basic info
                imgui_Text($"Name: {_spellLoaded.SpellName}");
                imgui_Text($"ID: {_spellLoaded.SpellID}");
                imgui_Text($"Level: {_spellLoaded.Level}");
                imgui_Text($"Type: {_spellLoaded.SpellType}");
                imgui_Text($"Target: {_spellLoaded.TargetType}");
                imgui_Text($"Category: {_spellLoaded.Category} / {_spellLoaded.Subcategory}");
                imgui_Text($"Mana: {_spellLoaded.Mana}  Cast: {_spellLoaded.MyCastTime}  Recast: {_spellLoaded.RecastTime}  Recovery: {_spellLoaded.RecoveryTime}");
                imgui_Text($"Range: {_spellLoaded.MyRange}");
                imgui_Text($"Resist: {_spellLoaded.ResistType} ({_spellLoaded.ResistAdj})");
                imgui_Text($"Duration: {_spellLoaded.Duration} ticks ({_spellLoaded.DurationTotalSeconds}s)  ShortBuff: {_spellLoaded.IsShortBuff}");
                imgui_Text($"Icon ID: {_spellLoaded.SpellIcon}");

                imgui_Separator();
                imgui_Text("Effects");
                foreach (var line in _spellEffects)
                {
                    imgui_Text(line);
                }
                if (_spellEffects.Length == 0) imgui_Text("(no effects)");
            }
        }

        private static void RenderSpellIconsTab()
        {
            imgui_Text("Lookup Spell Icon by Name or ID");
            imgui_SameLine();
            if (imgui_InputText("##SpellIconQuery", _spellQueryInput))
            {
                _spellQueryInput = imgui_InputText_Get("##SpellIconQuery") ?? string.Empty;
            }
            imgui_SameLine();
            if (imgui_Button("Load"))
            {
                TryLoadSpell(_spellQueryInput);
            }
            if (_spellLoaded != null)
            {
                imgui_Text($"Name: {_spellLoaded.SpellName}");
                imgui_Text($"ID: {_spellLoaded.SpellID}");
                imgui_Text($"Icon ID: {_spellLoaded.SpellIcon}");
                imgui_Text("(Icon rendering not yet available; ID shown above)");
            }
        }

        private static void TryLoadSpell(string key)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(key)) return;
                // Prevent excessive queries
                if (Core.StopWatch.ElapsedMilliseconds < _nextSpellQueryAtMs) return;
                _nextSpellQueryAtMs = Core.StopWatch.ElapsedMilliseconds + 250; // 250ms throttle

                // Normalize: allow ID or name
                string castKey = key.Trim();
                int id;
                if (int.TryParse(castKey, out id) && id > 0)
                {
                    // Resolve ID -> name for Spell ctor
                    string name = E3.MQ.Query<string>($"${{Spell[{id}]}}");
                    if (!string.IsNullOrEmpty(name)) castKey = name;
                }

                var sp = new E3Core.Data.Spell(castKey);
                if (sp != null && sp.SpellID > 0)
                {
                    _spellLoadedKey = castKey;
                    _spellLoaded = sp;
                    LoadSpellEffects(sp);
                }
                else
                {
                    _spellLoadedKey = string.Empty;
                    _spellLoaded = null;
                    _spellEffects = Array.Empty<string>();
                }
            }
            catch { _spellLoaded = null; _spellEffects = Array.Empty<string>(); }
        }

        private static void LoadSpellEffects(E3Core.Data.Spell sp)
        {
            try
            {
                if (sp == null) { _spellEffects = Array.Empty<string>(); return; }
                // Use MQ2Mono helpers to get effect lines by ID
                string key = sp.SpellID > 0 ? sp.SpellID.ToString() : sp.SpellName;
                int cnt = E3.MQ.SpellDataGetLineCount(key);
                if (cnt <= 0) { _spellEffects = Array.Empty<string>(); return; }
                var lines = new List<string>(cnt);
                for (int i = 0; i < cnt; i++)
                {
                    var line = E3.MQ.SpellDataGetLine(key, i) ?? string.Empty;
                    if (!string.IsNullOrWhiteSpace(line)) lines.Add(line);
                }
                _spellEffects = lines.ToArray();
            }
            catch { _spellEffects = Array.Empty<string>(); }
        }

        private static string RemoveIfSuffix(string s)
        {
            try
            {
                if (string.IsNullOrEmpty(s)) return string.Empty;
                int idx = s.LastIndexOf("/Ifs|", StringComparison.OrdinalIgnoreCase);
                if (idx >= 0) return s.Substring(0, idx);
                return s;
            }
            catch { return s ?? string.Empty; }
        }
        private static string ReplaceIfSuffix(string s, string ifName)
        {
            try
            {
                string basePart = RemoveIfSuffix(s ?? string.Empty).TrimEnd();
                if (string.IsNullOrEmpty(ifName)) return basePart;
                return basePart + "/Ifs|" + ifName;
            }
            catch { return s ?? string.Empty; }
        }
        private static string ExtractIfSuffixName(string s)
        {
            try
            {
                if (string.IsNullOrEmpty(s)) return string.Empty;
                int idx = s.LastIndexOf("/Ifs|", StringComparison.OrdinalIgnoreCase);
                if (idx < 0 || idx + 5 >= s.Length) return string.Empty;
                return s.Substring(idx + 5).Trim();
            }
            catch { return string.Empty; }
        }

        // Combo wrappers with safety for older MQ2Mono builds lacking the functions
        private static bool BeginComboSafe(string label, string preview)
        {
            try
            {
                return imgui_BeginCombo(label, preview, 0);
            }
            catch
            {
                _comboAvailable = false;
                return false;
            }
        }
        private static void EndComboSafe()
        {
            try { imgui_EndCombo(); } catch { }
        }

        // Popup/context wrappers with safety for older MQ2Mono builds
        private static bool BeginPopupContextItemSafe(string id)
        {
            // 1 == ImGuiPopupFlags_MouseButtonRight
            try { return imgui_BeginPopupContextItem(id, 1); } catch { return false; }
        }
        private static bool BeginPopupContextWindowSafe(string id)
        {
            // 1 == ImGuiPopupFlags_MouseButtonRight
            try { return imgui_BeginPopupContextWindow(id, 1); } catch { return false; }
        }
        private static bool MenuItemSafe(string label)
        {
            try { return imgui_MenuItem(label); } catch { return false; }
        }
        private static void EndPopupSafe()
        {
            try { imgui_EndPopup(); } catch { }
        }

        private static void ApplyCharacterIniEditsForActiveSelection()
        {
            try
            {
                var selectedPath = _selectedCharIniPath;
                var currentPath = GetCurrentCharacterIniPath();
                if (string.IsNullOrEmpty(selectedPath)) selectedPath = currentPath;

                if (string.Equals(selectedPath, currentPath, StringComparison.OrdinalIgnoreCase))
                {
                    // Apply using existing mechanism for current character
                    E3Core.Processors.E3.ApplyCharacterIniEdits();
                    _nextIniRefreshAtMs = 0;
                    return;
                }

                // Apply to non-current file selection
                if (!File.Exists(selectedPath)) { mq_Echo("Selected ini does not exist."); return; }

                // Load ini (use cached parsed if available)
                var parser = E3Core.Utility.e3util.CreateIniParser();
                var pd = _selectedCharIniParsedData ?? parser.ReadFile(selectedPath);
                int changed = 0;
                foreach (var kv in _charIniEditsSnapshot())
                {
                    var parts = kv.Key.Split('|');
                    if (parts.Length != 4) continue;
                    string sectionName = parts[1];
                    string keyName = parts[2];
                    int idx = 0; int.TryParse(parts[3], out idx);
                    var section = pd.Sections.GetSectionData(sectionName);
                    if (section == null)
                    {
                        pd.Sections.AddSection(sectionName);
                        section = pd.Sections.GetSectionData(sectionName);
                    }
                    var keyData = section.Keys.GetKeyData(keyName);
                    if (keyData == null)
                    {
                        section.Keys.AddKey(keyName);
                        keyData = section.Keys.GetKeyData(keyName);
                    }
                    var newVal = kv.Value ?? string.Empty;
                    if (keyData.ValueList != null && keyData.ValueList.Count > 0)
                    {
                        if (idx >= 0 && idx < keyData.ValueList.Count)
                        {
                            if (!string.Equals(keyData.ValueList[idx], newVal, StringComparison.Ordinal))
                            { keyData.ValueList[idx] = newVal; changed++; }
                        }
                        else if (idx == 0)
                        {
                            if (!string.Equals(keyData.Value, newVal, StringComparison.Ordinal)) { keyData.Value = newVal; changed++; }
                        }
                    }
                    else
                    {
                        if (!string.Equals(keyData.Value, newVal, StringComparison.Ordinal)) { keyData.Value = newVal; changed++; }
                    }
                }

                if (changed > 0)
                {
                    parser.WriteFile(selectedPath, pd);
                    _charIniEditsClear();
                    _selectedCharIniParsedData = pd; // keep updated
                    mq_Echo($"Saved {changed} change(s) to {Path.GetFileName(selectedPath)}");
                    _nextIniRefreshAtMs = 0;
                }
            }
            catch (Exception ex)
            {
                mq_Echo($"Failed to apply changes: {ex.Message}");
            }
        }
        public static void OnUpdateImGui()
        {
            // Initialize window visibility once (default hidden)
            if (!_imguiInitDone)
            {
                try { imgui_Begin_OpenFlagSet(_e3ImGuiWindow, false); }
                catch { /* older MQ2Mono versions may not support this */ }
                try { imgui_Begin_OpenFlagSet(_e3ButtonsWindow, false); }
                catch { /* older MQ2Mono versions may not support this */ }
                _imguiInitDone = true;
            }

            if (imgui_Begin_OpenFlagGet(_e3ImGuiWindow))
            {
                imgui_Begin(_e3ImGuiWindow, (int)ImGuiWindowFlags.ImGuiWindowFlags_None);

                // Header
                imgui_Text($"nE³xt v{E3Core.Processors.Setup._e3Version} | Build {E3Core.Processors.Setup._buildDate}");
                imgui_Separator();

                // Removed Echo Test and Broadcast Writes controls per request

                // Character INI selector (used by Config Editor)
                RenderCharacterIniSelector();

                imgui_Separator();
                // Config Editor only
                imgui_Text("Config Editor");
                RenderConfigEditor();

                imgui_End();
            }

            // Buttons windows (multi-window aware)
            RenderButtonsWindows();
        }

        private static void RenderButtonsWindows()
        {
            var bb = E3Core.Processors.E3.ButtonBar;
            if (bb == null)
            {
                if (imgui_Begin_OpenFlagGet(_e3ButtonsWindow))
                {
                    imgui_Begin(_e3ButtonsWindow, (int)ImGuiWindowFlags.ImGuiWindowFlags_AlwaysAutoResize);
                    imgui_Text("No buttons configured. Edit 'E3 Buttons.ini'.");
                    imgui_End();
                }
                return;
            }

            string charKey = (E3Core.Processors.E3.ServerName ?? string.Empty) + "_" + (E3Core.Processors.E3.CurrentName ?? string.Empty);
            if (bb.WindowsByChar != null && bb.WindowsByChar.TryGetValue(charKey, out var windows) && windows != null && windows.Count > 0)
            {
                for (int i = 0; i < windows.Count; i++)
                {
                    var w = windows[i];
                    string windowName = i == 0 ? _e3ButtonsWindow : _e3ButtonsWindow + " #" + (i + 1).ToString();
                    int flags = (int)ImGuiWindowFlags.ImGuiWindowFlags_None;
                    if (w.HideTitleBar) flags |= (int)ImGuiWindowFlags.ImGuiWindowFlags_NoTitleBar;
                    if (w.Locked) flags |= (int)ImGuiWindowFlags.ImGuiWindowFlags_NoMove;
                    if (w.AutoResize) flags |= (int)ImGuiWindowFlags.ImGuiWindowFlags_AlwaysAutoResize;

                    if (!imgui_Begin_OpenFlagGet(windowName)) continue;
                    imgui_Begin(windowName, flags);

                    // Only show editor toggle in the first window to avoid duplicates
                    if (i == 0)
                    {
                        RenderButtonsEditor(bb, charKey);
                        imgui_Separator();
                    }

                    if (w.Sets == null || w.Sets.Count == 0)
                    {
                        imgui_Text("No sets assigned. Edit 'E3 Buttons.ini'.");
                        imgui_End();
                        continue;
                    }

                    if (w.Compact)
                    {
                        var first = FindSet(bb, w.Sets[0]);
                        if (first != null)
                        {
                            RenderButtonsList(first);
                        }
                    }
                    else
                    {
                        if (imgui_BeginTabBar(windowName + "_Tabs"))
                        {
                            foreach (var setName in w.Sets)
                            {
                                var set = FindSet(bb, setName);
                                if (set == null) continue;
                                if (imgui_BeginTabItem(set.Name))
                                {
                                    RenderButtonsList(set);
                                    imgui_EndTabItem();
                                }
                            }
                            imgui_EndTabBar();
                        }
                    }

                    imgui_End();
                }
            }
            else
            {
                // Fallback to single-window behavior for backwards compatibility
                if (imgui_Begin_OpenFlagGet(_e3ButtonsWindow))
                {
                    imgui_Begin(_e3ButtonsWindow, (int)ImGuiWindowFlags.ImGuiWindowFlags_AlwaysAutoResize);
                    RenderButtonsEditor(bb, charKey);
                    imgui_Separator();
                    if (bb.Sets != null && bb.Sets.Count > 0)
                    {
                        if (imgui_BeginTabBar("E3ButtonsTabs"))
                        {
                            foreach (var set in bb.Sets)
                            {
                                if (imgui_BeginTabItem(set.Name))
                                {
                                    RenderButtonsList(set);
                                    imgui_EndTabItem();
                                }
                            }
                            imgui_EndTabBar();
                        }
                    }
                    else
                    {
                        foreach (var btn in bb.Buttons)
                        {
                            if (imgui_Button(btn.Label)) ExecuteButtonCommand(btn.Command);
                        }
                    }
                    imgui_End();
                }
            }
        }

        private static bool _buttonsEditMode = false;
        private static int _buttonsEditSelectedSet = 0;
        private static string _buttonsEditNewSetName = string.Empty;
        private static string _buttonsEditNewBtnLabel = string.Empty;
        private static string _buttonsEditNewBtnCmd = string.Empty;

        private static void RenderButtonsEditor(E3Core.Settings.FeatureSettings.ButtonBar bb, string charKey)
        {
            if (imgui_Button(_buttonsEditMode ? "Exit Edit Mode" : "Edit Buttons"))
            {
                _buttonsEditMode = !_buttonsEditMode;
            }
            if (!_buttonsEditMode) return;

            imgui_Separator();
            imgui_Text("Sets");

            float leftW = 260f;
            float availY = imgui_GetContentRegionAvailY();

            // --- Left: Sets list -----------------------------------------------------
            {
                bool open = imgui_BeginChild("BB_Sets", leftW, availY * 0.5f, true);
                if (open)
                {
                    for (int i = 0; i < (bb.Sets?.Count ?? 0); i++)
                    {
                        bool sel = (i == _buttonsEditSelectedSet);
                        if (imgui_Selectable(bb.Sets[i].Name, sel))
                            _buttonsEditSelectedSet = i;
                    }
                }
                imgui_EndChild();
            }

            imgui_SameLine();

            // --- Right: Set editor (add/rename/delete) -------------------------------
            float rightW = imgui_GetContentRegionAvailX();
            {
                bool open2 = imgui_BeginChild("BB_SetEditor", rightW, availY * 0.5f, true);
                if (open2)
                {
                    // Add/Rename/Delete set
                    imgui_Text("Add Set");
                    if (imgui_InputText("BB_NewSet", _buttonsEditNewSetName))
                    {
                        _buttonsEditNewSetName = imgui_InputText_Get("BB_NewSet");
                    }
                    if (imgui_Button("Add Set"))
                    {
                        var name = (_buttonsEditNewSetName ?? string.Empty).Trim();
                        if (name.Length > 0)
                        {
                            EnqueueUI(() =>
                            {
                                bb.Sets.Add(new E3Core.Settings.FeatureSettings.ButtonBar.ButtonSet { Name = name });
                                _buttonsEditSelectedSet = bb.Sets.Count - 1;
                                _buttonsEditNewSetName = string.Empty;
                            });
                        }
                    }

                    if (bb.Sets != null && bb.Sets.Count > 0 && _buttonsEditSelectedSet >= 0 && _buttonsEditSelectedSet < bb.Sets.Count)
                    {
                        var set = bb.Sets[_buttonsEditSelectedSet];
                        imgui_Separator();
                        imgui_Text($"Edit Set: {set.Name}");

                        // Rename
                        if (imgui_InputText("BB_RenameSet", set.Name))
                        {
                            var newName = imgui_InputText_Get("BB_RenameSet");
                            EnqueueUI(() =>
                            {
                                // Update window references
                                if (bb.WindowsByChar.TryGetValue(charKey, out var charWindowsRename) && charWindowsRename != null)
                                {
                                    foreach (var w in charWindowsRename)
                                    {
                                        for (int si = 0; si < w.Sets.Count; si++)
                                        {
                                            if (string.Equals(w.Sets[si], set.Name, StringComparison.OrdinalIgnoreCase))
                                                w.Sets[si] = newName;
                                        }
                                    }
                                }
                                set.Name = newName;
                            });
                        }

                        // Delete
                        if (imgui_Button("Delete Set"))
                        {
                            EnqueueUI(() =>
                            {
                                string old = set.Name;
                                bb.Sets.RemoveAt(_buttonsEditSelectedSet);

                                if (bb.WindowsByChar.TryGetValue(charKey, out var charWindowsDelete) && charWindowsDelete != null)
                                {
                                    foreach (var w in charWindowsDelete)
                                    {
                                        w.Sets.RemoveAll(s => string.Equals(s, old, StringComparison.OrdinalIgnoreCase));
                                    }
                                }

                                _buttonsEditSelectedSet = 0;
                            });
                        }
                    }
                }
                imgui_EndChild();
            }

            // --- Buttons of the selected set -----------------------------------------
            if (bb.Sets != null && bb.Sets.Count > 0 && _buttonsEditSelectedSet >= 0 && _buttonsEditSelectedSet < bb.Sets.Count)
            {
                var set = bb.Sets[_buttonsEditSelectedSet];
                imgui_Separator();
                imgui_Text($"Buttons in: {set.Name}");

                {
                    bool open3 = imgui_BeginChild("BB_Buttons", imgui_GetContentRegionAvailX(), imgui_GetContentRegionAvailY() * 0.5f, true);
                    if (open3)
                    {
                        for (int bi = 0; bi < set.Buttons.Count; bi++)
                        {
                            var b = set.Buttons[bi];
                            string idLabel = $"BB_Label_{_buttonsEditSelectedSet}_{bi}";
                            string idCmd = $"BB_Cmd_{_buttonsEditSelectedSet}_{bi}";

                            imgui_Text("Label:");
                            imgui_SameLine();
                            if (imgui_InputText(idLabel, b.Label))
                            {
                                string nv = imgui_InputText_Get(idLabel);
                                EnqueueUI(() => b.Label = nv);
                            }

                            imgui_Text("Cmd (use \\n for newline):");
                            imgui_SameLine();
                            if (imgui_InputText(idCmd, b.Command))
                            {
                                string nv = imgui_InputText_Get(idCmd);
                                EnqueueUI(() => b.Command = nv);
                            }

                            imgui_SameLine();
                            if (imgui_Button($"Delete##{_buttonsEditSelectedSet}_{bi}"))
                            {
                                int rmIndex = bi;
                                EnqueueUI(() => set.Buttons.RemoveAt(rmIndex));
                            }

                            imgui_Separator();
                        }

                        imgui_Text("Add Button");
                        if (imgui_InputText("BB_NewBtn_Label", _buttonsEditNewBtnLabel))
                            _buttonsEditNewBtnLabel = imgui_InputText_Get("BB_NewBtn_Label");

                        if (imgui_InputText("BB_NewBtn_Cmd", _buttonsEditNewBtnCmd))
                            _buttonsEditNewBtnCmd = imgui_InputText_Get("BB_NewBtn_Cmd");

                        if (imgui_Button("Add Button"))
                        {
                            var label = (_buttonsEditNewBtnLabel ?? string.Empty).Trim();
                            var cmd = _buttonsEditNewBtnCmd ?? string.Empty;

                            if (label.Length > 0)
                            {
                                EnqueueUI(() =>
                                {
                                    set.Buttons.Add(new E3Core.Settings.FeatureSettings.ButtonBar.ButtonDef
                                    {
                                        Label = label,
                                        Command = cmd
                                    });
                                    _buttonsEditNewBtnLabel = string.Empty;
                                    _buttonsEditNewBtnCmd = string.Empty;
                                });
                            }
                        }
                    }
                    imgui_EndChild();
                } // end inner scope for BB_Buttons child
            }     // end: if selected set

            // --- Windows configuration for current character --------------------------
            imgui_Separator();
            imgui_Text("Windows (current character)");

            if (!bb.WindowsByChar.TryGetValue(charKey, out var charWindows) || charWindows == null)
            {
                // Seed a default window referencing all sets
                EnqueueUI(() =>
                {
                    var w = new E3Core.Settings.FeatureSettings.ButtonBar.WindowDef
                    {
                        Id = "1",
                        Visible = true,
                        Locked = false,
                        HideTitleBar = false,
                        Compact = false,
                        AutoResize = true
                    };
                    w.Sets = new List<string>();
                    foreach (var s in bb.Sets) w.Sets.Add(s.Name);

                    bb.WindowsByChar[charKey] = new List<E3Core.Settings.FeatureSettings.ButtonBar.WindowDef> { w };
                });
            }
            else
            {
                var windows = charWindows;

                {
                    bool open4 = imgui_BeginChild("BB_Windows", imgui_GetContentRegionAvailX(), imgui_GetContentRegionAvailY() * 0.4f, true);
                    if (open4)
                    {
                        for (int wi = 0; wi < windows.Count; wi++)
                        {
                            var w = windows[wi];

                            imgui_Text($"Window {wi + 1}");

                            bool v;
                            v = imgui_Checkbox($"Locked##W{wi}", w.Locked);
                            if (v != w.Locked) { bool nv = v; EnqueueUI(() => w.Locked = nv); }

                            imgui_SameLine();
                            v = imgui_Checkbox($"HideTitle##W{wi}", w.HideTitleBar);
                            if (v != w.HideTitleBar) { bool nv = v; EnqueueUI(() => w.HideTitleBar = nv); }

                            imgui_SameLine();
                            v = imgui_Checkbox($"Compact##W{wi}", w.Compact);
                            if (v != w.Compact) { bool nv = v; EnqueueUI(() => w.Compact = nv); }

                            imgui_SameLine();
                            v = imgui_Checkbox($"AutoResize##W{wi}", w.AutoResize);
                            if (v != w.AutoResize) { bool nv = v; EnqueueUI(() => w.AutoResize = nv); }

                            imgui_Text("Sets:");
                            foreach (var s in bb.Sets)
                            {
                                bool has = w.Sets.Exists(n => string.Equals(n, s.Name, StringComparison.OrdinalIgnoreCase));
                                bool inc = imgui_Checkbox($"{s.Name}##W{wi}_{s.Name}", has);

                                if (inc != has)
                                {
                                    if (inc)
                                    {
                                        EnqueueUI(() =>
                                        {
                                            if (!w.Sets.Exists(n => string.Equals(n, s.Name, StringComparison.OrdinalIgnoreCase)))
                                                w.Sets.Add(s.Name);
                                        });
                                    }
                                    else
                                    {
                                        EnqueueUI(() =>
                                            w.Sets.RemoveAll(n => string.Equals(n, s.Name, StringComparison.OrdinalIgnoreCase))
                                        );
                                    }
                                }
                            }

                            imgui_Separator();
                        }

                        if (imgui_Button("Add Window"))
                        {
                            EnqueueUI(() =>
                            {
                                var w = new E3Core.Settings.FeatureSettings.ButtonBar.WindowDef
                                {
                                    Id = (windows.Count + 1).ToString(),
                                    Visible = true,
                                    Locked = false,
                                    HideTitleBar = false,
                                    Compact = false,
                                    AutoResize = true
                                };
                                w.Sets = new List<string>();
                                foreach (var s in bb.Sets) w.Sets.Add(s.Name);
                                windows.Add(w);
                            });
                        }

                        imgui_SameLine();

                        if (windows.Count > 1 && imgui_Button("Remove Last Window"))
                        {
                            EnqueueUI(() =>
                            {
                                windows.RemoveAt(windows.Count - 1);
                            });
                        }
                    }
                    imgui_EndChild();
                } // end inner scope for BB_Windows child
            }     // end else (windows present)

            // --- Save -----------------------------------------------------------------
            imgui_Separator();
            if (imgui_Button("Save Buttons.ini"))
            {
                EnqueueUI(() =>
                {
                    try
                    {
                        E3Core.Processors.E3.ButtonBar.Save();
                        E3Core.Processors.E3.MQ.Write("Saved E3 Buttons.ini");
                    }
                    catch { }
                });
            }
        }


        private static E3Core.Settings.FeatureSettings.ButtonBar.ButtonSet FindSet(E3Core.Settings.FeatureSettings.ButtonBar bb, string name)
        {
            if (bb == null || bb.Sets == null) return null;
            foreach (var s in bb.Sets) if (string.Equals(s.Name, name, StringComparison.OrdinalIgnoreCase)) return s;
            return null;
        }

        private static void RenderButtonsList(E3Core.Settings.FeatureSettings.ButtonBar.ButtonSet set)
        {
            foreach (var btn in set.Buttons)
            {
                if (imgui_Button(btn.Label)) ExecuteButtonCommand(btn.Command);
            }
        }

        private static void ExecuteButtonCommand(string cmd)
        {
            var normalized = (cmd ?? string.Empty).Replace("\\n", "\n").Replace("\r", "\n");
            var parts = normalized.Split(new char[] { '\n' }, StringSplitOptions.None);
            foreach (var line in parts)
            {
                var l = (line ?? string.Empty).Trim();
                if (l.Length == 0) continue;
                try { E3Core.Processors.E3.MQ.Cmd(l); } catch { }
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
		public extern static void mq_DoCommandDelayed(string msg);
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
		public extern static void mq_GetSpawns2();
		[MethodImpl(MethodImplOptions.InternalCall)]
        public extern static bool mq_GetRunNextCommand();
        [MethodImpl(MethodImplOptions.InternalCall)]
        public extern static string mq_GetFocusedWindowName();
		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern static string mq_GetHoverWindowName();
		[MethodImpl(MethodImplOptions.InternalCall)]
		private extern static string mq_GetMQ2MonoVersion();
		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern static int mq_GetSpellDataEffectCount(string query);
		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern static string mq_GetSpellDataEffect(string query, int line);


		#region IMGUI
		[MethodImpl(MethodImplOptions.InternalCall)]
        public extern static bool imgui_Begin(string name, int flags);
        [MethodImpl(MethodImplOptions.InternalCall)]
        public extern static void imgui_Begin_OpenFlagSet(string name, bool value);
        [MethodImpl(MethodImplOptions.InternalCall)]
        public extern static bool imgui_Begin_OpenFlagGet(string name);
        [MethodImpl(MethodImplOptions.InternalCall)]
        public extern static bool imgui_Button(string name);
        [MethodImpl(MethodImplOptions.InternalCall)]
        public extern static void imgui_Text(string text);
        [MethodImpl(MethodImplOptions.InternalCall)]
        public extern static void imgui_Separator();
        [MethodImpl(MethodImplOptions.InternalCall)]
        public extern static void imgui_SameLine();
        [MethodImpl(MethodImplOptions.InternalCall)]
        public extern static bool imgui_Checkbox(string name, bool defaultValue);
        [MethodImpl(MethodImplOptions.InternalCall)]
        public extern static bool imgui_BeginTabBar(string name);
        [MethodImpl(MethodImplOptions.InternalCall)]
        public extern static void imgui_EndTabBar();
        [MethodImpl(MethodImplOptions.InternalCall)]
        public extern static bool imgui_BeginTabItem(string label);
        [MethodImpl(MethodImplOptions.InternalCall)]
        public extern static void imgui_EndTabItem();
        [MethodImpl(MethodImplOptions.InternalCall)]
        public extern static bool imgui_BeginChild(string id, float width, float height, bool border);
        [MethodImpl(MethodImplOptions.InternalCall)]
        public extern static void imgui_EndChild();
        [MethodImpl(MethodImplOptions.InternalCall)]
        public extern static bool imgui_Selectable(string label, bool selected);
        [MethodImpl(MethodImplOptions.InternalCall)]
        public extern static float imgui_GetContentRegionAvailX();
        [MethodImpl(MethodImplOptions.InternalCall)]
        public extern static float imgui_GetContentRegionAvailY();
        [MethodImpl(MethodImplOptions.InternalCall)]
        public extern static void imgui_SetNextItemWidth(float width);
        [MethodImpl(MethodImplOptions.InternalCall)]
        public extern static bool imgui_BeginCombo(string label, string preview, int flags);
        [MethodImpl(MethodImplOptions.InternalCall)]
        public extern static void imgui_EndCombo();
        // Helper provided by MQ2Mono to right-align a button within the current row/region
        [MethodImpl(MethodImplOptions.InternalCall)]
        public extern static bool imgui_RightAlignButton(string name);
        [MethodImpl(MethodImplOptions.InternalCall)]
        public extern static bool imgui_InputText(string id, string initial);
        [MethodImpl(MethodImplOptions.InternalCall)]
        public extern static string imgui_InputText_Get(string id);
        [MethodImpl(MethodImplOptions.InternalCall)]
        public extern static void imgui_End();
        // Context menus / popup
        [MethodImpl(MethodImplOptions.InternalCall)]
        public extern static bool imgui_BeginPopupContextItem(string id, int flags);
        [MethodImpl(MethodImplOptions.InternalCall)]
        public extern static bool imgui_BeginPopupContextWindow(string id, int flags);
        [MethodImpl(MethodImplOptions.InternalCall)]
        public extern static void imgui_EndPopup();
        [MethodImpl(MethodImplOptions.InternalCall)]
        public extern static bool imgui_MenuItem(string label);
        #endregion
        #endregion

        // expose pending edit helpers for E3.ApplyCharacterIniEdits()
        internal static IEnumerable<KeyValuePair<string,string>> _charIniEditsSnapshot()
        {
            return _charIniEdits.ToList();
        }
        internal static void _charIniEditsClear()
        {
            _charIniEdits.Clear();
        }

        [DllImport("user32.dll")]
        public static extern bool UnregisterClass(string lpClassName, IntPtr hInstance);
        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern IntPtr GetModuleHandle([MarshalAs(UnmanagedType.LPWStr)] string lpModuleName);
        [DllImport("kernel32.dll", SetLastError = true)]
        [PreserveSig]
        public static extern uint GetModuleFileName
        (
            [In]
            IntPtr hModule,

            [Out]
            StringBuilder lpFilename,

            [In]
            [MarshalAs(UnmanagedType.U4)]
            int nSize
        );
        [DllImport("user32.dll")]
        public static extern IntPtr GetWindowThreadProcessId(IntPtr hWnd,out uint ProcessId);
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
    public enum MQFeature
    { 
        TLO_Dispellable
    
    }

    public interface IMQ
    {
        T Query<T>(string query);
        void Cmd(string query, bool delayed = false);
        void Cmd(string query,Int32 delay,bool delayed=false);
        void Write(string query, [CallerMemberName] string memberName = "", [CallerFilePath] string fileName = "", [CallerLineNumber] int lineNumber = 0);
		/// <summary>
		/// This is used when on a different thread so its queued up on the main thread in MQ
		/// </summary>
		void WriteDelayed(string query, [CallerMemberName] string memberName = "", [CallerFilePath] string fileName = "", [CallerLineNumber] int lineNumber = 0);
		void TraceStart(string methodName);
        void TraceEnd(string methodName);
        void Delay(Int32 value);
        Boolean Delay(Int32 maxTimeToWait, string Condition);
        Boolean Delay(Int32 maxTimeToWait, Func<Boolean> methodToCheck);
        //void Broadcast(string query);
        bool AddCommand(string query);
        void ClearCommands();
        void RemoveCommand(string commandName);
		string SpellDataGetLine(string query, Int32 line);
		Int32 SpellDataGetLineCount(string query);
		bool FeatureEnabled(MQFeature feature);
        string GetFocusedWindowName();
		string GetHoverWindowName();
	}
    public class MQ : IMQ
    {   //**************************************************************************************************
        //NONE OF THESE METHODS SHOULD BE CALLED ON THE C++ Thread, as it will cause a deadlock due to delay calls
        //**************************************************************************************************

        public static Int64 MaxMillisecondsToWork = 40;
        public static Int64 SinceLastDelay = 0;
        public static Int64 _totalQueryCounts;
        public static bool _noDelay = false;
		public static ConcurrentQueue<String> DelayedWrites = new ConcurrentQueue<string>();
		public string SpellDataGetLine(string query, int line)
		{
			if (Core._MQ2MonoVersion > 0.30M)
			{
				return Core.mq_GetSpellDataEffect(query, line);
			}
			else
			{
				return String.Empty;
			}
		}

		public Int32 SpellDataGetLineCount(string query)
		{
			if (Core._MQ2MonoVersion > 0.30M)
			{
				return Core.mq_GetSpellDataEffectCount(query);
			}
			else
			{
				return 12;
			}
		}
		public T Query<T>(string query)
        {
            if (!Core.IsProcessing)
            {
                //we are terminating, kill this thread
                throw new ThreadAbort("Terminating thread");
            }
            _totalQueryCounts++;
            Int64 elapsedTime = Core.StopWatch.ElapsedMilliseconds;
            Int64 differenceTime = Core.StopWatch.ElapsedMilliseconds - SinceLastDelay;

            if (MaxMillisecondsToWork < differenceTime)
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
                    else { return (T)(object)-1; }
                }
                else
                {
                    Decimal value;
                    if (decimal.TryParse(mqReturnValue, out value))
                    {
                        return (T)(object)value;
                    }
                    else { return (T)(object)-1; }

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
                else { return (T)(object)-1M; }
            }
            else if (typeof(T) == typeof(double))
            {
                double value;
                if (double.TryParse(mqReturnValue, out value))
                {
                    return (T)(object)value;
                }
                else { return (T)(object)-1D; }
            }
            else if (typeof(T) == typeof(Int64))
            {
                Int64 value;
                if (Int64.TryParse(mqReturnValue, out value))
                {
                    return (T)(object)value;
                }
                else { return (T)(object)-1L; }
            }


            return default(T);

        }
        public void Cmd(string query, bool delayed = false)
        {
            if (!Core.IsProcessing)
            {
                //we are terminating, kill this thread
                throw new ThreadAbort("Terminating thread");
            }

            Int64 elapsedTime = Core.StopWatch.ElapsedMilliseconds;
            Int64 differenceTime = Core.StopWatch.ElapsedMilliseconds - SinceLastDelay;


            if (MaxMillisecondsToWork < differenceTime)
            {
                Delay(0);
            }
            //avoid using /delay, this was only made to deal with UI /delay commands.
            if (query.StartsWith("/delay ", StringComparison.OrdinalIgnoreCase))
            {
                string[] splitArray = query.Split(' ');
                if(splitArray.Length>1)
                {
                    if (Int32.TryParse(splitArray[1], out var delayvalue))
                    {
                        Delay(delayvalue);
                    }
                }
                return;
            }
         
            Core.CurrentCommand = query;
            Core.CurrentCommandDelayed = delayed;
            Core.CoreResetEvent.Set();
            //we are now going to wait on the core
            MainProcessor.ProcessResetEvent.Wait();
            MainProcessor.ProcessResetEvent.Reset();
            if (!Core.IsProcessing)
            {  
                //we are terminating, kill this thread
                throw new ThreadAbort("Terminating thread");
            }

        }
        public void Cmd(string query, Int32 delay, bool delayed = false)
        {
            Cmd(query,delayed);
            Delay(delay);
        }

        public void Write(string query, [CallerMemberName] string memberName = "", [CallerFilePath] string fileName = "", [CallerLineNumber] int lineNumber = 0)
        {
            //write on current thread, it will be queued up by MQ. 
            //needed to deal with certain lock situations and just keeps things simple. 
            if(E3Core.Processors.Setup._broadcastWrites)
            {
				E3.Bots.Broadcast(query);
			}
			Core.mq_Echo($"\a#336699[{MainProcessor.ApplicationName}]\a-w{System.DateTime.Now.ToString("HH:mm:ss")} \aw- {query}");
			return;
        }
		public void WriteDelayed(string query, [CallerMemberName] string memberName = "", [CallerFilePath] string fileName = "", [CallerLineNumber] int lineNumber = 0)
		{
			//delay the write until we are in the C# area and the MQ thread are haulted to prevent crashes
			if (E3Core.Processors.Setup._broadcastWrites)
			{
				E3.Bots.Broadcast(query);
			}
			DelayedWrites.Enqueue($"\a#336699[{MainProcessor.ApplicationName}]\a-w{System.DateTime.Now.ToString("HH:mm:ss")} \aw- {query}");
			return;
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
            if (_noDelay) return;

            if (!Core.IsProcessing)
            {
                //we are terminating, kill this thread
                throw new ThreadAbort("Terminating thread: Delay enter");
            }
            if (value > 0)
            {
                Core.DelayStartTime = Core.StopWatch.ElapsedMilliseconds;
                Core.DelayTime = value;
                Core.CurrentDelay = value;//tell the C++ thread to send out a delay update
            }
            if (E3.IsInit && !E3.InStateUpdate)
            {
                E3.StateUpdates();
            }
			//lets tell core that it can continue
			Core.CoreResetEvent.Set();
            //we are now going to wait on the core
            MainProcessor.ProcessResetEvent.Wait();
            MainProcessor.ProcessResetEvent.Reset();
			

			if (!Core.IsProcessing)
            {
                //we are terminating, kill this thread
                Write("Throwing exception for termination: Delay exit");
                throw new ThreadAbort("Terminating thread");
            }
			if (E3.IsInit && !E3.InStateUpdate)
			{
				E3.StateUpdates();
                
			}
			SinceLastDelay = Core.StopWatch.ElapsedMilliseconds;
        }

        public Boolean Delay(Int32 maxTimeToWait, string Condition)
        {
            if (!Core.IsProcessing)
            {
                //we are terminating, kill this thread
                throw new ThreadAbort("Terminating thread: Delay Condition");
            }
            Condition = $"${{If[{Condition},TRUE,FALSE]}}";
            Int64 startingTime = Core.StopWatch.ElapsedMilliseconds;
            while (!this.Query<bool>(Condition))
            {
                if (Core.StopWatch.ElapsedMilliseconds - startingTime > maxTimeToWait)
                {
                    return false;
                }
                this.Delay(25);
            }
            return true;
        }
        public bool Delay(int maxTimeToWait, Func<bool> methodToCheck)
        {
            if (!Core.IsProcessing)
            {
                //we are terminating, kill this thread
                throw new ThreadAbort("Terminating thread: delay method ");
            }
            Int64 startingTime = Core.StopWatch.ElapsedMilliseconds;
            while (!methodToCheck.Invoke())
            {
                if (Core.StopWatch.ElapsedMilliseconds - startingTime > maxTimeToWait)
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

        private bool? Feature_TLO_Dispellable = null;
        public bool FeatureEnabled(MQFeature feature)
        {
            if(feature == MQFeature.TLO_Dispellable)
            {
               if(Feature_TLO_Dispellable==null)
               {
                    Feature_TLO_Dispellable = Query<bool>("${Spell[Courage].Dispellable}");
               }
                return Feature_TLO_Dispellable.Value;
            }
            return true;
        }

		public string GetFocusedWindowName()
		{
            if(Core._MQ2MonoVersion>0.1M)
            {
                return Core.mq_GetFocusedWindowName();
            }
            else
            {
                return "NULL";
            }
			
		}
        public string GetHoverWindowName()
        {
			if (Core._MQ2MonoVersion > 0.31M)
			{
				return Core.mq_GetFocusedWindowName();
			}
			else
			{
				return "NULL";
			}
		}
	}

    public class Logging
    {
        public static LogLevels TraceLogLevel = LogLevels.None;
        public static LogLevels MinLogLevelTolog = LogLevels.Debug;
        public static LogLevels DefaultLogLevel = LogLevels.Debug;
        private static ConcurrentDictionary<String, String> _classLookup = new ConcurrentDictionary<string, string>();
        public static IMQ MQ = Core.mqInstance;

        public Logging(IMQ mqInstance)
        {
            MQ = mqInstance;
        }
		public void WriteDelayed(string message, LogLevels logLevel = LogLevels.Default, string eventName = "Logging", [CallerMemberName] string memberName = "", [CallerFilePath] string fileName = "", [CallerLineNumber] int lineNumber = 0, Dictionary<String, String> headers = null)
		{

			if (logLevel == LogLevels.Default)
			{
				logLevel = DefaultLogLevel;
			}

			WriteStaticDelayed(message, logLevel, eventName, memberName, fileName, lineNumber, headers);

		}
		public void Write(string message, LogLevels logLevel = LogLevels.Default, string eventName = "Logging", [CallerMemberName] string memberName = "", [CallerFilePath] string fileName = "", [CallerLineNumber] int lineNumber = 0, Dictionary<String, String> headers = null)
        {

            if (logLevel == LogLevels.Default)
            {
                logLevel = DefaultLogLevel;
            }

            WriteStatic(message, logLevel, eventName, memberName, fileName, lineNumber, headers);

        }
		public static void WriteStaticDelayed(string message, LogLevels logLevel = LogLevels.Info, string eventName = "Logging", [CallerMemberName] string memberName = "", [CallerFilePath] string fileName = "", [CallerLineNumber] int lineNumber = 0, Dictionary<String, String> headers = null)
		{
			if ((Int32)logLevel < (Int32)MinLogLevelTolog)
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
				MQ.WriteDelayed($"\ag{className}:\ao{memberName}\aw:({lineNumber}) {message}", "", "Logging");

			}
			else
			{
				MQ.WriteDelayed($"{message}");
			}
		}
		public static void WriteStatic(string message, LogLevels logLevel = LogLevels.Info, string eventName = "Logging", [CallerMemberName] string memberName = "", [CallerFilePath] string fileName = "", [CallerLineNumber] int lineNumber = 0, Dictionary<String, String> headers = null)
        {
            if ((Int32)logLevel < (Int32)MinLogLevelTolog)
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
				MQ.Write($"\ag{className}:\ao{memberName}\aw:({lineNumber}) {message}", "", "Logging");

            }
            else
            {
                MQ.Write($"{message}");
            }

        }
        public ITrace Trace(string name = "", [CallerMemberName] string memberName = "", [CallerFilePath] string fileName = "", [CallerLineNumber] int lineNumber = 0)
        {

            BaseTrace returnValue = BaseTrace.Aquire();

            if (TraceLogLevel != LogLevels.Trace)
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
            returnValue.StartTime = Core.StopWatch.Elapsed.TotalMilliseconds;
            if (!string.IsNullOrWhiteSpace(name))
            {
                MQ.TraceEnd($"\ag{memberName}:\ao{name})");
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
            totalMilliseconds = Core.StopWatch.Elapsed.TotalMilliseconds - value.StartTime;
            //put event back into its object pool.
            if (!string.IsNullOrWhiteSpace(value.Method))
            {
                MQ.TraceEnd($"\ag{value.Method}:\ao{value.Name}\aw({totalMilliseconds}ms)");
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
        public static String GetClassName(string fileName)
        {
            string className;
			try
			{
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
			}
			catch(Exception)
			{
				_classLookup.TryAdd(fileName, "Unknown/ErrorGettingClass");
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
                    CallBackDispose.Invoke(this); 
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
    /// <summary>
    /// Used for object pooling objects to be reused
    /// </summary>
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
        void EmptyLists();
        bool TryByID(Int32 id, out Spawn s);
        bool TryByName(string name,out Spawn s);
        Int32 GetIDByName(string name);
        bool Contains(string name);
        bool Contains(Int32 id);
       
    }
    /// <summary>
    /// Used to download spawns from MQ in a quick manner to be used in scripts. 
    /// </summary>
    public class Spawns: ISpawns
    {
        //special list so we can get rid of the non dirty values
        private static List<Spawn> _tmpSpawnList = new List<Spawn>();
        
        public static List<Spawn> _spawns = new List<Spawn>(2048);
        public static Dictionary<string, Spawn> _spawnsByName = new Dictionary<string, Spawn>(2048, StringComparer.OrdinalIgnoreCase);
        public static Dictionary<Int32,Spawn> SpawnsByID = new Dictionary<int,Spawn>(2048);
        public static Int64 _lastRefesh = 0;
        public static Int64 RefreshTimePeriodInMS = 1000;

        public bool TryByID(Int32 id, out Spawn s)
        {
            RefreshListIfNeeded();
            return SpawnsByID.TryGetValue(id,out s);
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
            return SpawnsByID.ContainsKey(id);
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
            if (Core.StopWatch.ElapsedMilliseconds - _lastRefesh > RefreshTimePeriodInMS)
            {
                RefreshList();
            }
        }
        /// <summary>
        /// warning, only do this during shutdown.
        /// </summary>
        public void EmptyLists()
        {  
            _spawnsByName.Clear();
            SpawnsByID.Clear();
            _spawns.Clear();
        }
        public void RefreshList()
        {
            //need to mark everything not dirty so we know what get spawns gets us.
            foreach (var spawn in _spawns)
            {
                spawn.isDirty = false;
            }
            //request new spawns!
            if(Core._MQ2MonoVersion>0.23m)
            {
				Core.mq_GetSpawns2();

			}
			else
            {
				Core.mq_GetSpawns();

			}

			//spawns has new/updated data, get rid of the non dirty stuff.
			//can use the other dictionaries to help
			_spawnsByName.Clear();
            SpawnsByID.Clear();
            foreach (var spawn in _spawns)
            {
                if(spawn.isDirty)
                {
                    _tmpSpawnList.Add(spawn);
                    if(spawn.TypeDesc=="PC")
                    {
                        if(!_spawnsByName.ContainsKey(spawn.Name))
                        {
                            _spawnsByName.Add(spawn.Name, spawn);
                        }
                    }
                    SpawnsByID.Add(spawn.ID, spawn);
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
            _lastRefesh = Core.StopWatch.ElapsedMilliseconds;

        }
    }
    /// <summary>
    /// the actual object pooled spawn object that can be used in scripts. 
    /// </summary>
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
        ~Spawn()
        {
            //DO NOT CALL DISPOSE FROM THE FINALIZER! This should only ever be used in using statements
            //if this is called, it will cause the domain to hang in the GC when shuttind down
            //This is only here to warn you

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
            if (Core._MQ2MonoVersion > 0.22m)
            {
				CurrentHPs = BitConverter.ToInt64(data, cb);
				cb += 8;

			}
			else
            {
				CurrentHPs = BitConverter.ToInt32(data, cb);
				cb += 4;

			}
			CurrentMana = BitConverter.ToInt32(data, cb);
            cb += 4;
            Dead = BitConverter.ToBoolean(data, cb);
            cb += 1;
            //displayname
            slength = BitConverter.ToInt32(data, cb);
            cb += 4;
            tstring = System.Text.Encoding.ASCII.GetString(data, cb, slength);
            if (!_stringLookup.TryGetValue(tstring, out DisplayName))
            {
                _stringLookup.Add(tstring, tstring);
                DisplayName = tstring;
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
            if(Core._MQ2MonoVersion>0.23m)
            {
				GuildID = BitConverter.ToInt64(data, cb);
				cb += 8;
			}
            else
            {
				GuildID = BitConverter.ToInt32(data, cb);
				cb += 4;
			}
          
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
			if (Core._MQ2MonoVersion > 0.23m)
			{
				PctHps = BitConverter.ToInt64(data, cb);
				cb += 8;
			}
			else
			{
				PctHps = BitConverter.ToInt32(data, cb);
				cb += 4;
			}
			
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
		//used in recording data stuff, not really part of spawn
		public Int32 TableID;
		public Int32 GridID;
	    public float Initial_Heading;
        public bool Recording_Complete=false;
		public bool Recording_MovementOccured = false;
        public Int32 Recording_StepCount;
		///end recording data stuff
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
        public Int64 PctHps;
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
        public Int64 GuildID;
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
        public string DisplayName = string.Empty;
        public bool Dead;
        public Int32 CurrentMana;
        public Int64 CurrentHPs;
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
			TableID = 0;
			Recording_MovementOccured = false;
            Recording_StepCount = 0;
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
    /// Not currently used, tho it is used in the E3IU. Thought it might be useful to some.
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
