using E3Core.Classes;
using E3Core.Data;
using E3Core.Processors;
using E3Core.Settings;
using MonoCore;
using Google.Protobuf;
using NetMQ;
using NetMQ.Sockets;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.WebSockets;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms.VisualStyles;
using System.Xml.Linq;
using System.Diagnostics;
using E3Core.Utility;

namespace E3Core.Server
{

	public class ShareDataEntry
	{
		public String Data { get; set; }
		public Int64 LastUpdate
		{
			get; set;
		}
	}
	public class SharedDataClient
	{
		public ConcurrentDictionary<string, ConcurrentDictionary<string, ShareDataEntry>> TopicUpdates = new ConcurrentDictionary<string, ConcurrentDictionary<string, ShareDataEntry>>(StringComparer.OrdinalIgnoreCase);
		public ConcurrentQueue<OnCommandData> CommandQueue = new ConcurrentQueue<OnCommandData>();
		public ConcurrentQueue<OnCommandData> IMGUICommands = new ConcurrentQueue<OnCommandData>();

		// Cleanup settings for TopicUpdates dictionary
		private const int TopicCleanupIntervalMs = 300000; // 5 minutes
		private const int StaleDataTimeoutMs = 600000; // 10 minutes - consider data stale if no updates
		private Int64 _nextTopicCleanupMs = 0;

		private static IMQ MQ = E3.MQ;
		public class ConnectionInfo
		{
			public string User { get; set; }
			public string Port { get; set; }
			public string IPAddress { get; set; }
			public string FilePath { get; set; }
			public DateTime FileLastUpdateTime { get; set; }
			public Int64 LastMessageTimeStamp { get; set; }

		}

		//Dictionary<string, Task> _processTasks = new Dictionary<string, Task>();
		static object _processLock = new object();
		static bool _isProxyMode = false;
		ConcurrentQueue<ConnectionInfo> _usersToConnectTo = new ConcurrentQueue<ConnectionInfo>();
		public ConcurrentDictionary<string, ConnectionInfo> UsersConnectedTo = new ConcurrentDictionary<string, ConnectionInfo>(StringComparer.OrdinalIgnoreCase);
		Task _mainProcessingTask = null;
		public bool RegisterUser(string user, string path, bool isproxy = false)
		{
			_isProxyMode = isproxy;
			//fix situations where it doesn't end in a slash
			if (!path.EndsWith(@"\"))
			{
				path += @"\";
			}


			lock (_processLock)
			{
				if (!UsersConnectedTo.ContainsKey(user))
				{
					//lets see if the file exists
					string filePath = $"{path}{user}_{E3.ServerName}_pubsubport.txt";
					if (isproxy)
					{
						filePath = $"{path}{user}_pubsubport.txt";
					}

					if (System.IO.File.Exists(filePath))
					{
						//lets load up the port information

						string data = System.IO.File.ReadAllText(filePath);
						//its now port:ipaddress
						string[] splitData = data.Split(new char[] { ',' });
						string port = splitData[0];
						string ipaddress = splitData[1];


						ConnectionInfo info = new ConnectionInfo() { User = user, Port = port, IPAddress = ipaddress, FilePath = filePath };

						_usersToConnectTo.Enqueue(info);
						UsersConnectedTo.TryAdd(info.User, info);
						if (_mainProcessingTask == null)
						{
							_mainProcessingTask = Task.Factory.StartNew(() => { Process(); }, CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
						}
						//var newTask = Task.Factory.StartNew(() => { Process(user, port,ipaddress, filePath); }, CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
						//_processTasks.Add(user, newTask);
						return true;
					}
				}
			}

			return false;
		}
		public void ProcessE3BCCommands()
		{
			if (EventProcessor.CommandList.ContainsKey("/e3bc") && EventProcessor.CommandListQueueHasCommand("/e3bc"))
			{
				EventProcessor.ProcessEventsInQueues("/e3bc");
			}
			if (EventProcessor.CommandList.ContainsKey("/e3bcz") && EventProcessor.CommandListQueueHasCommand("/e3bcz"))
			{
				EventProcessor.ProcessEventsInQueues("/e3bcz");
			}
			if (EventProcessor.CommandList.ContainsKey("/e3bcg") && EventProcessor.CommandListQueueHasCommand("/e3bcg"))
			{
				EventProcessor.ProcessEventsInQueues("/e3bcg");
			}
			if (EventProcessor.CommandList.ContainsKey("/e3bcgz") && EventProcessor.CommandListQueueHasCommand("/e3bcgz"))
			{
				EventProcessor.ProcessEventsInQueues("/e3bcgz");
			}
			if (EventProcessor.CommandList.ContainsKey("/e3bct") && EventProcessor.CommandListQueueHasCommand("/e3bct"))
			{
				EventProcessor.ProcessEventsInQueues("/e3bct");
			}
			if (EventProcessor.CommandList.ContainsKey("/e3bcga") && EventProcessor.CommandListQueueHasCommand("/e3bcga"))
			{
				EventProcessor.ProcessEventsInQueues("/e3bcga");
			}
			if (EventProcessor.CommandList.ContainsKey("/e3bcgaz") && EventProcessor.CommandListQueueHasCommand("/e3bcgaz"))
			{
				EventProcessor.ProcessEventsInQueues("/e3bcgaz");
			}
			if (EventProcessor.CommandList.ContainsKey("/e3bcaa") && EventProcessor.CommandListQueueHasCommand("/e3bcaa"))
			{
				EventProcessor.ProcessEventsInQueues("/e3bcaa");
			}
			if (EventProcessor.CommandList.ContainsKey("/e3bcaaz") && EventProcessor.CommandListQueueHasCommand("/e3bcaaz"))
			{
				EventProcessor.ProcessEventsInQueues("/e3bcaaz");
			}
		}
		//primary E3N C# thread, we pull off the collections that was populated by the network thread
		//this way we can do queries/command/etc.

		public void ProcessIMGUICommands()
		{
			while (IMGUICommands.Count > 0)
			{
				if (IMGUICommands.TryDequeue(out var data))
				{
					string messageTopicReceived = data.Data;
					string payloaduser = data.Data2.ToLower();
					string messageReceived = data.Data3;

					var typeInfo = data.TypeOfCommand;
					data.Dispose(); //put back to be reused ,we have the data we want out of it.


					try
					{
						if (typeInfo == OnCommandData.CommandType.OnIMGUICommand_GetCatalogData)
						{

							// e3imgui peer catalog request via PubSub relay
							// Topic: CatalogReq-<TargetToon>
							// payloaduser is requester; if TargetToon equals our name, publish base64 SpellDataList frames back
							string target = messageTopicReceived.Substring("CatalogReq-".Length);

							if (!string.IsNullOrEmpty(target) && target.Equals(E3.CurrentName, StringComparison.OrdinalIgnoreCase))
							{

								var spells = E3Core.Utility.e3util.ListAllBookSpells();
								var aas = E3Core.Utility.e3util.ListAllActiveAA();
								var discs = E3Core.Utility.e3util.ListAllDiscData();
								var skills = E3Core.Utility.e3util.ListAllActiveSkills();
								var items = E3Core.Utility.e3util.ListAllItemWithClickyData();
								Func<List<E3Core.Data.Spell>, string> pack = (lst) =>
								{
									var sdl = new SpellDataList();
									foreach (var s in lst) sdl.Data.Add(s.ToProto());
									return Convert.ToBase64String(sdl.ToByteArray());
								};

								E3.Log.WriteDelayed($"RSending out data with :CatalogResp-{payloaduser}-Spells", Logging.LogLevels.Debug);


								PubServer.AddTopicMessage($"CatalogResp-{payloaduser}-Spells", pack(spells));
								PubServer.AddTopicMessage($"CatalogResp-{payloaduser}-AAs", pack(aas));
								PubServer.AddTopicMessage($"CatalogResp-{payloaduser}-Discs", pack(discs));
								PubServer.AddTopicMessage($"CatalogResp-{payloaduser}-Skills", pack(skills));
								PubServer.AddTopicMessage($"CatalogResp-{payloaduser}-Items", pack(items));

								// Also send memorized spell gems data
								var gemData = CollectSpellGemData();
								PubServer.AddTopicMessage($"CatalogResp-{payloaduser}-Gems", gemData);

							}
						}
						else if (typeInfo == OnCommandData.CommandType.OnIMGUICommand_GetItemsByType)
						{

							// e3imgui Food/Drink peer inventory request via PubSub relay
							// Topic: InvReq-<TargetToon>
							// payloaduser is requester; messageReceived is type key ("Food" or "Drink")
							string target = messageTopicReceived.Substring("InvReq-".Length);
							if (!string.IsNullOrEmpty(target) && target.Equals(E3.CurrentName, StringComparison.OrdinalIgnoreCase))
							{

								string type = (messageReceived ?? string.Empty).Trim();
								List<string> items = ScanInventoryByType(type);
								// pack as base64 of newline-delimited names
								string joined = string.Join("\n", items ?? new List<string>());
								string b64 = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(joined));
								PubServer.AddTopicMessage($"InvResp-{payloaduser}-{type}", b64);

							}
						}
						else if (typeInfo == OnCommandData.CommandType.OnIMGUICommand_ConfigValueReq)
						{
							string target = messageTopicReceived.Substring("ConfigValueReq-".Length);
							if (!string.IsNullOrEmpty(target) && target.Equals(E3.CurrentName, StringComparison.OrdinalIgnoreCase))
							{

								string[] parts = messageReceived.Split(new[] { ':' }, 2);
								string section = parts[0];
								string key = parts[1];
								string value = E3.CharacterSettings.ParsedData.Sections[section][key] ?? "";
								PubServer.AddTopicMessage($"ConfigValueResp-{payloaduser}-{section}:{key}", value);
							}
						}
						else if (typeInfo == OnCommandData.CommandType.OnIMGUICommand_ConfigValueUpdate)
						{
							string target = messageTopicReceived.Substring("ConfigValueUpdate-".Length);
							if (!string.IsNullOrEmpty(target) && target.Equals(E3.CurrentName, StringComparison.OrdinalIgnoreCase))
							{
								string[] parts = messageReceived.Split(new[] { ':' }, 3);
								string section = parts[0];
								string key = parts[1];
								string value = parts[2];
								E3.CharacterSettings.ParsedData[section][key] = value;
								E3.CharacterSettings.SaveData();
							}
						}
					}
					catch (Exception ex)
					{
						MQ.Write($"Exception processing {typeInfo.ToString()}:" + ex.Message);

					}

				}
			}
		}

		public void ProcessCommands()
		{
			ProcessIMGUICommands();


			while (CommandQueue.Count > 0)
			{
				if (CommandQueue.TryDequeue(out var data))
				{
					//command format
					//$"{E3.CurrentName}:{noparse}:{command}"

					string message = data.Data;
					var typeInfo = data.TypeOfCommand;

					data.Dispose(); //put back to be reused ,we have the data we want out of it.

					//allow macro escape sequence for tlos to be executed on the dest client
					if (message.Contains("$\\{")) message = message.Replace("$\\{", "${");

					if (typeInfo == OnCommandData.CommandType.BroadCastMessage || typeInfo == OnCommandData.CommandType.BroadCastMessageZone)
					{
						Int32 indexOfSeperator = message.IndexOf(':');
						Int32 currentIndex = 0;
						string user = message.Substring(currentIndex, indexOfSeperator);

						if (typeInfo == OnCommandData.CommandType.BroadCastMessageZone)
						{
							//check to see if we are in the same zone as the person
							Int32 spawnID = MQ.Query<Int32>($"${{Spawn[{user}].ID}}");
							if (spawnID < 1)
							{
								//we can safely ignore this.
								continue;
							}
						}

						currentIndex = indexOfSeperator + 1;
						string bcMessage = message.Substring(currentIndex, message.Length - currentIndex);

						MQ.Cmd($"/noparse /echo \a#336699[{MainProcessor.ApplicationName}]\a-w{System.DateTime.Now.ToString("HH:mm:ss")}\ar<\ay{user}\ar> \aw{bcMessage}");

					}
					else
					{
						Int32 indexOfSeperator = message.IndexOf(':');
						Int32 currentIndex = 0;
						string user = message.Substring(currentIndex, indexOfSeperator);
						currentIndex = indexOfSeperator + 1;
						indexOfSeperator = message.IndexOf(':', indexOfSeperator + 1);

						string noparseString = message.Substring(currentIndex, indexOfSeperator - currentIndex);
						currentIndex = indexOfSeperator + 1;
						string command = message.Substring(currentIndex, message.Length - currentIndex);
						//a command type

						if (typeInfo == OnCommandData.CommandType.OnCommandAllExceptMeZone || typeInfo == OnCommandData.CommandType.OnCommandAllZone || typeInfo == OnCommandData.CommandType.OnCommandGroupZone
							|| typeInfo == OnCommandData.CommandType.OnCommandGroupAllZone || typeInfo == OnCommandData.CommandType.OnCommandRaidZone || typeInfo == OnCommandData.CommandType.OnCommandRaidZoneNotMe)
						{

							//this is a zone type command lets verify zone logic
							//check to see if we are in the same zone as the person
							Int32 spawnID = MQ.Query<Int32>($"${{Spawn[pc {user}].ID}}");
							if (spawnID < 1)
							{
								//we can safely ignore this.
								continue;
							}
						}


						if (typeInfo == OnCommandData.CommandType.OnCommandGroup || typeInfo == OnCommandData.CommandType.OnCommandGroupZone)
						{
							//check to see if we are part of their group
							if (user == E3.CurrentName)
							{
								//not for us only group members
								continue;
							}
							//check to see if we are part of their group
							Int32 groupMemberIndex = MQ.Query<Int32>($"${{Group.Member[{user}].Index}}");

							if (groupMemberIndex < 0)
							{
								//ignore it
								continue;
							}
						}
						else if (typeInfo == OnCommandData.CommandType.OnCommandGroupAll || typeInfo == OnCommandData.CommandType.OnCommandGroupAllZone)
						{
							//check to see if we are part of their group
							Int32 groupMemberIndex = MQ.Query<Int32>($"${{Group.Member[{user}].Index}}");

							if (groupMemberIndex < 0)
							{
								//ignore it
								continue;
							}
						}
						else if (typeInfo == OnCommandData.CommandType.OnCommandRaid || (typeInfo == OnCommandData.CommandType.OnCommandRaidZone) || (typeInfo == OnCommandData.CommandType.OnCommandRaidNotMe || (typeInfo == OnCommandData.CommandType.OnCommandRaidZoneNotMe)))
						{
							//check to see if we are part of their group
							var inRaid = MQ.Query<bool>($"${{Raid.Member[{user}]}}");

							if (!inRaid)
							{
								continue;
							}
						}

						//check to see if we are part of their group
						if (user == E3.CurrentName && (!(typeInfo == OnCommandData.CommandType.OnCommandName || typeInfo == OnCommandData.CommandType.OnCommandChannel ||
							typeInfo == OnCommandData.CommandType.OnCommandGroupAll || typeInfo == OnCommandData.CommandType.OnCommandAll || typeInfo == OnCommandData.CommandType.OnCommandGroupAllZone ||
							typeInfo == OnCommandData.CommandType.OnCommandAllZone || typeInfo == OnCommandData.CommandType.OnCommandRaid || typeInfo == OnCommandData.CommandType.OnCommandRaidZone)))
						{
							//if not an all type command and not us, kick out.
							//not for us only group members
							continue;
						}

						MQ.Write($"\ag<\ap{user}\ag> Command:" + command);
						if (command.StartsWith("/mono ", StringComparison.OrdinalIgnoreCase) || command.StartsWith("/shutdown", StringComparison.OrdinalIgnoreCase))
						{
							//in case this is a restart command, we need to delay the command so it happens outside of the OnPulse. just assume all /mono commands are 
							//delayed
							if (Core._MQ2MonoVersion >= 0.22m)
							{
								//needs to be delayed, so that the restarts happes outside of the E3N OnPulse
								MQ.Cmd(command, true);
							}
							else
							{
								E3.Bots.Broadcast("Sorry cannot execute /mono commands via broadcast unless your MQ2Mono version is 0.22 or above.");
							}
						}
						else
						{
							bool internalComand = false;
							foreach (var pair in EventProcessor.CommandList)
							{
								string compareCommandTo = pair.Key;
								if (command.Contains(" "))
								{
									compareCommandTo = pair.Value.commandwithSpace;
								}
								if (command.StartsWith(compareCommandTo, StringComparison.OrdinalIgnoreCase))
								{
									internalComand = true;
									//no need to send this to mq if its our own command, just drop it into the queues to be processed. 
									List<String> args = EventProcessor.ParseParmsThreadSafe(command, ' ', '"');
									args.RemoveAt(0);
									EventProcessor.CommandMatch commandMatch = new EventProcessor.CommandMatch() { eventName = "TempE3NCommand", eventString = command, args = args, hasAllFlag = false };

									if (!e3util.FilterMe(commandMatch))
									{
										EventProcessor.ProcessMQCommand(command);
									}
									break;
								}
							}
							if (!internalComand)
							{

								List<String> args = EventProcessor.ParseParmsThreadSafe(command, ' ', '"');
								List<string> initialCommand = args.ToList();
								args.RemoveAt(0);
								EventProcessor.CommandMatch commandMatch = new EventProcessor.CommandMatch() { eventName = "TempMQCommand", eventString = command, args = args, hasAllFlag = false };


								if(!e3util.FilterMe(commandMatch))
								{
									foreach(var filter in commandMatch.filters)
									{
										initialCommand.Remove(filter);
									}
									string newCommand = String.Join(" ", initialCommand);
									MQ.Cmd(newCommand, true);
								}
								

							}
						}

					}



				}


			}
		}
		/// <summary>
		/// As this is just an /echo command, we can send this out from a different thread , as long as we use the delayed part of the command so its queued up on the MQ side
		/// </summary>
		/// <param name="message"></param>
		private void ProcessBroadcast(string message)
		{

			string commandToSend = string.Empty;
			try
			{
				Int32 indexOfSeperator = message.IndexOf(':');
				Int32 currentIndex = 0;
				string user = message.Substring(currentIndex, indexOfSeperator);
				currentIndex = indexOfSeperator + 1;
				string bcMessage = message.Substring(currentIndex, message.Length - currentIndex);
				commandToSend = $"/noparse /echo \a#336699[{MainProcessor.ApplicationName}]\a-w{System.DateTime.Now.ToString("HH:mm:ss")}\ar<\ay{user}\ar> \aw{bcMessage}";
				Core.mq_DoCommandDelayed(commandToSend);
			}
			catch (Exception)
			{
				//MQ.Write("Error in shared data thread. ProcessBroadcast:" + message + " fullCommand:"+commandToSend);
				//throw e;
			}



		}

		/// <summary>
		/// Note, in P2P mode, we would have 1 thread per toon, but I had to enable proxy mode to handle larger number of clients (54) so i couldn't have 
		/// one thread per toon, as that would be like nearly 3,000 threads. so the user name was put into the payload so that a proxy could be used. 
		/// </summary>
		/// <param name="user"></param>
		/// <param name="messageTopicReceived"></param>
		/// <param name="messageReceived"></param>
		private void ProcessTopicMessage(string user, string messageTopicReceived, string messageReceived)
		{

			//get the user from the payload
			ConcurrentDictionary<string, ShareDataEntry> usertopics;
			if (!TopicUpdates.TryGetValue(user, out usertopics))
			{
				usertopics = new ConcurrentDictionary<string, ShareDataEntry>(StringComparer.OrdinalIgnoreCase);
				TopicUpdates.TryAdd(user, usertopics);
			}

			Int64 updateTime = Core.StopWatch.ElapsedMilliseconds;
			ShareDataEntry entry;
			if (!usertopics.TryGetValue(messageTopicReceived, out entry))
			{
				entry = new ShareDataEntry() { Data = messageReceived, LastUpdate = updateTime };
				usertopics.TryAdd(messageTopicReceived, entry);
			}


			if (!String.Equals(entry.Data, messageReceived))
			{
				lock (entry)
				{
					//why do work if its the same data?	
					entry.Data = messageReceived;
					entry.LastUpdate = updateTime;
				}
			}
			else
			{
				lock (entry)
				{
					entry.LastUpdate = updateTime;
				}
			}
		}

		/// <summary>
		/// Cleans up stale data from TopicUpdates dictionary.
		/// Removes characters that haven't sent updates recently and are no longer connected.
		/// </summary>
		public void CleanupStaleTopicData()
		{
			try
			{
				Int64 now = Core.StopWatch.ElapsedMilliseconds;
				Int64 staleThreshold = now - StaleDataTimeoutMs;
				var keysToRemove = new List<string>();

				// Find characters with stale data
				foreach (var characterEntry in TopicUpdates)
				{
					string characterName = characterEntry.Key;
					var topics = characterEntry.Value;

					// Check if this character is still connected
					bool isConnected = UsersConnectedTo.ContainsKey(characterName);

					if (!isConnected)
					{
						// Character is not connected, check if data is stale
						bool hasRecentData = false;

						foreach (var topicEntry in topics.Values)
						{
							if (topicEntry.LastUpdate > staleThreshold)
							{
								hasRecentData = true;
								break;
							}
						}

						// If no recent data, mark for removal
						if (!hasRecentData)
						{
							keysToRemove.Add(characterName);
						}
					}
				}

				// Remove stale character entries
				int removedCount = 0;
				foreach (var key in keysToRemove)
				{
					if (TopicUpdates.TryRemove(key, out var removed))
					{
						removedCount++;
						MQ.WriteDelayed($"SharedDataClient: Removed stale data for character '{key}' (topics: {removed.Count})");
					}
				}

				if (removedCount > 0)
				{
					MQ.WriteDelayed($"SharedDataClient: Cleanup completed. Removed {removedCount} stale character(s). Active characters: {TopicUpdates.Count}");
				}
			}
			catch (Exception ex)
			{
				MQ.WriteDelayed($"SharedDataClient: Error during cleanup - {ex.Message}");
			}
		}

		/// <summary>
		/// Checks if cleanup should run and executes it if needed
		/// </summary>
		private void CheckAndRunTopicCleanup()
		{
			Int64 now = Core.StopWatch.ElapsedMilliseconds;
			if (now >= _nextTopicCleanupMs)
			{
				_nextTopicCleanupMs = now + TopicCleanupIntervalMs;
				CleanupStaleTopicData();
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Process_CheckNewConnections(SubscriberSocket subSocket)
		{
			//lets connect up to anything that is queued up
			while (_usersToConnectTo.Count > 0 && _usersToConnectTo.TryDequeue(out var conInfo))
			{
				subSocket.Connect($"tcp://{conInfo.IPAddress}:" + conInfo.Port);
				//update the file date for when we connected
				conInfo.FileLastUpdateTime = System.IO.File.GetLastWriteTime(conInfo.FilePath);
				//set the initial timestamp so we know the delay from the least message recieved
				conInfo.LastMessageTimeStamp = Core.StopWatch.ElapsedMilliseconds;
				MQ.WriteDelayed("\agShared Data Client: Connecting to user:" + conInfo.User + " on port:" + conInfo.Port + " server:" + conInfo.IPAddress); ;
			}
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Process_CheckConnectionsIfStillValid(SubscriberSocket subSocket, ref Int64 lastConnectionCheck)
		{
			if ((Core.StopWatch.ElapsedMilliseconds - lastConnectionCheck) > 2000)
			{
				lastConnectionCheck = Core.StopWatch.ElapsedMilliseconds;

				//lets been over 2 seconds, lets check to see if there are anyone we need to reconnect to
				foreach (var userInfo in UsersConnectedTo.Values.ToList())
				{
					if ((Core.StopWatch.ElapsedMilliseconds - userInfo.LastMessageTimeStamp) > 2000)
					{
						//been at least 2 seconds from this user, lets check to see what has happened to them.
						try
						{
							if (!System.IO.File.Exists(userInfo.FilePath))
							{
								//client shut down?
								subSocket.Disconnect($"tcp://{userInfo.IPAddress}:" + userInfo.Port);
								MQ.WriteDelayed("\arDisconnecting User:\ag" + userInfo.User);
								UsersConnectedTo.TryRemove(userInfo.User, out var tuserInfo);
								continue;
							}

							System.DateTime currentTime = System.IO.File.GetLastWriteTime(userInfo.FilePath);

							if (currentTime > userInfo.FileLastUpdateTime)
							{
								//user file has been updated with new information, need to disconnect the old connection and connect the new one. 

								MQ.WriteDelayed($"\agShared Data Client: Disconnecting server:{userInfo.IPAddress} port:" + userInfo.Port + " for toon:" + userInfo.User);
								//shutown the socket and restart it
								subSocket.Disconnect($"tcp://{userInfo.IPAddress}:" + userInfo.Port);
								string data = System.IO.File.ReadAllText(userInfo.FilePath);
								string[] splitData = data.Split(new char[] { ',' });
								userInfo.Port = splitData[0];
								userInfo.IPAddress = splitData[1];
								MQ.WriteDelayed($"\agShared Data Client: Reconnecting to server:{userInfo.IPAddress} port:" + userInfo.Port + " for toon:" + userInfo.User);
								subSocket.Connect($"tcp://{userInfo.IPAddress}:" + userInfo.Port);
								userInfo.FileLastUpdateTime = currentTime;
							}
						}
						catch (Exception ex)
						{

							MQ.WriteDelayed("\arError, Disconnecting User:\ag" + userInfo.User + " Message:" + ex.Message);
							UsersConnectedTo.TryRemove(userInfo.User, out var tuserInfo);
						}
					}
				}
			}

		}
		/// <summary>
		/// Main thread for processing updates from other clients
		/// this was multiple threads one for each client, its been modified to be a single thread for all clients. 
		/// </summary>
		public void Process()
		{
			Int64 lastConnectionCheck = Core.StopWatch.ElapsedMilliseconds;
			string OnCommandName = "OnCommand-" + E3.CurrentName;
			//need to do this so double parses work in other languages
			Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");
			//timespan we expect to have some type of message
			TimeSpan recieveTimeout = new TimeSpan(0, 0, 0, 2, 0);
			using (var subSocket = new SubscriberSocket())
			{

				subSocket.Options.ReceiveHighWatermark = 100000;
				subSocket.Options.TcpKeepalive = true;
				subSocket.Options.TcpKeepaliveIdle = TimeSpan.FromSeconds(5);
				subSocket.Options.TcpKeepaliveInterval = TimeSpan.FromSeconds(1);
				subSocket.Subscribe(OnCommandName);
				subSocket.Subscribe("OnCommand-All");
				subSocket.Subscribe("OnCommand-AllZone");
				subSocket.Subscribe("OnCommand-AllExceptMe");
				subSocket.Subscribe("OnCommand-AllExceptMeZone");
				subSocket.Subscribe("OnCommand-Group");
				subSocket.Subscribe("OnCommand-GroupAll");
				subSocket.Subscribe("OnCommand-GroupAllZone");
				subSocket.Subscribe("OnCommand-Raid");
				subSocket.Subscribe("OnCommand-Zone");
				subSocket.Subscribe("BroadCastMessage");
				subSocket.Subscribe("BroadCastMessageZone");
				// e3imgui Add From Catalog peer relay topics
				// Requests addressed to specific toons and responses back to requester
				subSocket.Subscribe($"CatalogReq-{E3.CurrentName.ToLower()}");
				subSocket.Subscribe($"CatalogResp-{E3.CurrentName.ToLower()}");
				// e3imgui Food/Drink inventory peer relay topics
				subSocket.Subscribe("InvReq-");
				subSocket.Subscribe("InvResp-");
				subSocket.Subscribe("ConfigValueReq-");
				subSocket.Subscribe("ConfigValueResp-");
				subSocket.Subscribe("ConfigValueUpdate-");
				subSocket.Subscribe("${Me."); //all Me stuff should be subscribed to
				subSocket.Subscribe("${Data."); //all the custom data keys a user can create
				subSocket.Subscribe("${DataChannel.");

				while (Core.IsProcessing && E3.NetMQ_SharedDataServerThreadRun)
				{
					Process_CheckNewConnections(subSocket);
					Process_CheckConnectionsIfStillValid(subSocket, ref lastConnectionCheck);
					CheckAndRunTopicCleanup();

					string messageTopicReceived;
					if (subSocket.TryReceiveFrameString(recieveTimeout, out messageTopicReceived))
					{
						string messageReceived;
						string originalMessage;
						string payloaduser;
						try
						{
							messageReceived = subSocket.ReceiveFrameString();
							originalMessage = messageReceived;
							messageReceived = originalMessage;
							Int32 indexOfColon = messageReceived.IndexOf(':');
							payloaduser = messageReceived.Substring(0, indexOfColon);
							messageReceived = messageReceived.Substring(indexOfColon + 1, messageReceived.Length - indexOfColon - 1);
							indexOfColon = messageReceived.IndexOf(':');
							string payloadServer = messageReceived.Substring(0, indexOfColon);
							messageReceived = messageReceived.Substring(indexOfColon + 1, messageReceived.Length - indexOfColon - 1);

							//message not from the same server, skip it.
							if (!String.Equals(payloadServer, E3.ServerName))
							{
								continue;
							}
							if (UsersConnectedTo.TryGetValue(payloaduser, out var connectionInfo))
							{
								connectionInfo.LastMessageTimeStamp = Core.StopWatch.ElapsedMilliseconds;
							}
							//most common goes first
							if (messageTopicReceived.StartsWith("${Me."))
							{

								ProcessTopicMessage(payloaduser, messageTopicReceived, messageReceived);

							}
							else if (messageTopicReceived == "OnCommand-All")
							{
								var data = OnCommandData.Aquire();
								data.Data = messageReceived;
								data.TypeOfCommand = OnCommandData.CommandType.OnCommandAll;

								CommandQueue.Enqueue(data);
							}
							else if (messageTopicReceived == "OnCommand-AllZone")
							{
								var data = OnCommandData.Aquire();
								data.Data = messageReceived;
								data.TypeOfCommand = OnCommandData.CommandType.OnCommandAllZone;

								CommandQueue.Enqueue(data);
							}
							else if (messageTopicReceived == "OnCommand-Group")
							{
								var data = OnCommandData.Aquire();
								data.Data = messageReceived;
								data.TypeOfCommand = OnCommandData.CommandType.OnCommandGroup;

								CommandQueue.Enqueue(data);
							}
							else if (messageTopicReceived == "OnCommand-GroupZone")
							{
								var data = OnCommandData.Aquire();
								data.Data = messageReceived;
								data.TypeOfCommand = OnCommandData.CommandType.OnCommandGroupZone;

								CommandQueue.Enqueue(data);
							}
							else if (messageTopicReceived == "OnCommand-AllExceptMe")
							{
								var data = OnCommandData.Aquire();
								data.Data = messageReceived;
								data.TypeOfCommand = OnCommandData.CommandType.OnCommandAllExceptMe;

								CommandQueue.Enqueue(data);
							}
							else if (messageTopicReceived == "OnCommand-AllExceptMeZone")
							{
								var data = OnCommandData.Aquire();
								data.Data = messageReceived;
								data.TypeOfCommand = OnCommandData.CommandType.OnCommandAllExceptMeZone;

								CommandQueue.Enqueue(data);
							}
							else if (messageTopicReceived == "OnCommand-GroupAll")
							{
								var data = OnCommandData.Aquire();
								data.Data = messageReceived;
								data.TypeOfCommand = OnCommandData.CommandType.OnCommandGroupAll;

								CommandQueue.Enqueue(data);
							}
							else if (messageTopicReceived == "OnCommand-GroupAllZone")
							{
								var data = OnCommandData.Aquire();
								data.Data = messageReceived;
								data.TypeOfCommand = OnCommandData.CommandType.OnCommandGroupAllZone;

								CommandQueue.Enqueue(data);
							}
							else if (messageTopicReceived == "OnCommand-Raid")
							{
								var data = OnCommandData.Aquire();
								data.Data = messageReceived;
								data.TypeOfCommand = OnCommandData.CommandType.OnCommandRaid;

								CommandQueue.Enqueue(data);
							}
							else if (messageTopicReceived == "OnCommand-RaidNotMe")
							{
								var data = OnCommandData.Aquire();
								data.Data = messageReceived;
								data.TypeOfCommand = OnCommandData.CommandType.OnCommandRaidNotMe;
								CommandQueue.Enqueue(data);
							}
							else if (messageTopicReceived == "OnCommand-RaidZone")
							{
								var data = OnCommandData.Aquire();
								data.Data = messageReceived;
								data.TypeOfCommand = OnCommandData.CommandType.OnCommandRaidZone;
								CommandQueue.Enqueue(data);
							}
							else if (messageTopicReceived == "OnCommand-RaidZoneNotMe")
							{
								var data = OnCommandData.Aquire();
								data.Data = messageReceived;
								data.TypeOfCommand = OnCommandData.CommandType.OnCommandRaidZoneNotMe;
								CommandQueue.Enqueue(data);
							}
							else if (messageTopicReceived == "BroadCastMessage")
							{
								//send this message, even if we are on a different thread, just use delayed.
								ProcessBroadcast(messageReceived);
								//var data = OnCommandData.Aquire();
								//data.Data = messageReceived;
								//data.TypeOfCommand = OnCommandData.CommandType.BroadCastMessage;

								//CommandQueue.Enqueue(data);
							}
							else if (messageTopicReceived == "BroadCastMessageZone")
							{
								var data = OnCommandData.Aquire();
								data.Data = messageReceived;
								data.TypeOfCommand = OnCommandData.CommandType.BroadCastMessageZone;

								CommandQueue.Enqueue(data);
							}
							else if (messageTopicReceived.StartsWith("${DataChannel."))
							{
								//don't do the command you are issuing out
								if (payloaduser != E3.CurrentName)
								{
									if (E3.CharacterSettings.E3ChatChannelsToJoin.Contains(messageTopicReceived, StringComparer.OrdinalIgnoreCase))
									{
										var data = OnCommandData.Aquire();
										data.Data = messageReceived;
										data.TypeOfCommand = OnCommandData.CommandType.OnCommandChannel;
										CommandQueue.Enqueue(data);
									}
								}
							}
							else if (messageTopicReceived.StartsWith("CatalogReq-", StringComparison.Ordinal))
							{
								E3.Log.WriteDelayed($"Request recieved for catalog data topic:{messageTopicReceived}", Logging.LogLevels.Debug);

								var data = OnCommandData.Aquire();
								data.Data = messageTopicReceived;
								data.Data2 = payloaduser;
								data.TypeOfCommand = OnCommandData.CommandType.OnIMGUICommand_GetCatalogData;
								IMGUICommands.Enqueue(data);
							}
							else if (messageTopicReceived.StartsWith("InvReq-", StringComparison.Ordinal))
							{

								var data = OnCommandData.Aquire();
								data.Data = messageTopicReceived;
								data.Data2 = payloaduser;
								data.Data3 = messageReceived;
								data.TypeOfCommand = OnCommandData.CommandType.OnIMGUICommand_GetItemsByType;
								IMGUICommands.Enqueue(data);

							}
							else if (messageTopicReceived.StartsWith("ConfigValueReq-", StringComparison.Ordinal))
							{
								var data = OnCommandData.Aquire();
								data.Data = messageTopicReceived;
								data.Data2 = payloaduser;
								data.Data3 = messageReceived;
								data.TypeOfCommand = OnCommandData.CommandType.OnIMGUICommand_ConfigValueReq;
								IMGUICommands.Enqueue(data);

							}
							else if (messageTopicReceived.StartsWith("ConfigValueResp-", StringComparison.Ordinal))
							{
								ProcessTopicMessage(payloaduser, messageTopicReceived, messageReceived);
							}
							else if (messageTopicReceived.StartsWith("ConfigValueUpdate-", StringComparison.Ordinal))
							{
								var data = OnCommandData.Aquire();
								data.Data = messageTopicReceived;
								data.Data2 = payloaduser;
								data.Data3 = messageReceived;
								data.TypeOfCommand = OnCommandData.CommandType.OnIMGUICommand_ConfigValueUpdate;
								IMGUICommands.Enqueue(data);

							}
							else if (messageTopicReceived == OnCommandName)
							{   //bct commands
								var data = OnCommandData.Aquire();
								data.Data = messageReceived;
								data.TypeOfCommand = OnCommandData.CommandType.OnCommandName;
								CommandQueue.Enqueue(data);
							}
							else
							{
								ProcessTopicMessage(payloaduser, messageTopicReceived, messageReceived);

							}
						}
						catch (Exception ex)
						{
							Debug.WriteLine($"Error{ex.Message}");
							//MQ.WriteDelayed("Error in shared data thread. Message:" + ex.Message + "  stack:" + ex.StackTrace);
						}
					}
				}
				subSocket.Dispose();
			}
			MQ.WriteDelayed($"Shutting down Share Data Thread.");
		}

		// Helper to scan local inventory for a given item type (e.g., "Food" or "Drink")
		private static List<string> ScanInventoryByType(string type)
		{
			var results = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			if (string.IsNullOrWhiteSpace(type)) return results.ToList();
			string key = type.Trim();
			try
			{
				// scan top-level inventory slots generously
				for (int inv = 1; inv <= 40; inv++)
				{
					try
					{
						bool present = E3.MQ.Query<bool>($"${{Me.Inventory[{inv}]}}");
						if (!present) continue;
						string t = E3.MQ.Query<string>($"${{Me.Inventory[{inv}].Type}}") ?? string.Empty;
						if (!string.IsNullOrEmpty(t) && t.Equals(key, StringComparison.OrdinalIgnoreCase))
						{
							string name = E3.MQ.Query<string>($"${{Me.Inventory[{inv}]}}") ?? string.Empty;
							if (!string.IsNullOrEmpty(name)) results.Add(name);
						}
						int slots = E3.MQ.Query<int>($"${{Me.Inventory[{inv}].Container}}");
						if (slots > 0)
						{
							for (int i = 1; i <= slots; i++)
							{
								try
								{
									bool ipresent = E3.MQ.Query<bool>($"${{Me.Inventory[{inv}].Item[{i}]}}");
									if (!ipresent) continue;
									string it = E3.MQ.Query<string>($"${{Me.Inventory[{inv}].Item[{i}].Type}}") ?? string.Empty;
									if (!string.IsNullOrEmpty(it) && it.Equals(key, StringComparison.OrdinalIgnoreCase))
									{
										string iname = E3.MQ.Query<string>($"${{Me.Inventory[{inv}].Item[{i}]}}") ?? string.Empty;
										if (!string.IsNullOrEmpty(iname)) results.Add(iname);
									}
								}
								catch { }
							}
						}
					}
					catch { }
				}
			}
			catch { }
			return results.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToList();
		}

		// Collect current memorized spell gem data as pipe-separated string with icon indices
		private static string CollectSpellGemData()
		{
			try
			{
				var gemData = new List<string>();

				// Query gems 1-12 safely on the background thread
				for (int gem = 1; gem <= 12; gem++)
				{
					try
					{
						string spellName = MQ.Query<string>($"${{Me.Gem[{gem}]}}");
						Int32 spellID = MQ.Query<Int32>($"${{Me.Gem[{gem}].ID}}");
						if (string.IsNullOrEmpty(spellName) || spellName.Equals("NULL", StringComparison.OrdinalIgnoreCase))
						{
							gemData.Add("-1:-1");
						}
						else
						{
							// Try to get spell icon index from catalog data
							int iconIndex = GetSpellIconIndex(spellName);
							gemData.Add($"{spellID}:{iconIndex}");
						}
					}
					catch
					{
						gemData.Add("-2:-1");
					}
				}

				// Return as pipe-separated string for easy parsing
				return string.Join("|", gemData);
			}
			catch
			{
				// If all fails, return empty gems with no icons
				return string.Join("|", Enumerable.Repeat("-2:-1", 12));
			}
		}

		// Helper method to get spell icon index from catalog lookups
		private static int GetSpellIconIndex(string spellName)
		{
			if (string.IsNullOrEmpty(spellName)) return -1;

			try
			{
				// Check spell data lookup first
				if (E3Core.Data.Spell.SpellDataLookup.TryGetValue(spellName, out var spellData) && spellData.SpellIcon >= 0)
					return spellData.SpellIcon;

				// Check alt data lookup
				if (E3Core.Data.Spell.AltDataLookup.TryGetValue(spellName, out var altData) && altData.SpellIcon >= 0)
					return altData.SpellIcon;

				// Check disc data lookup
				if (E3Core.Data.Spell.DiscDataLookup.TryGetValue(spellName, out var discData) && discData.SpellIcon >= 0)
					return discData.SpellIcon;

				// Check item data lookup
				if (E3Core.Data.Spell.ItemDataLookup.TryGetValue(spellName, out var itemData) && itemData.SpellIcon >= 0)
					return itemData.SpellIcon;

				// Fallback: Query MQ directly for spell icon (safe on background thread)
				int iconIndex = MQ.Query<int>($"${{Spell[{spellName}].SpellIcon}}");
				return iconIndex > 0 ? iconIndex : -1;
			}
			catch
			{
				return -1;
			}
		}

		public class OnCommandData
		{
			public enum CommandType
			{
				None,
				OnCommandAll,
				OnCommandAllZone,
				OnCommandRaid,
				OnCommandRaidNotMe,
				OnCommandRaidZone,
				OnCommandRaidZoneNotMe,
				OnCommandAllExceptMe,
				OnCommandAllExceptMeZone,
				OnCommandGroup,
				OnCommandGroupZone,
				OnCommandGroupAll,
				OnCommandGroupAllZone,
				BroadCastMessage,
				BroadCastMessageZone,
				OnCommandName,
				OnCommandChannel,
				OnIMGUICommand_ListAA,
				OnIMGUICommand_ListSpells,
				OnIMGUICommand_ListSkills,
				OnIMGUICommand_ListDiscs,
				OnIMGUICommand_ListItemsWithSpells,
				OnIMGUICommand_GetCatalogData,
				OnIMGUICommand_GetItemsByType,
				OnIMGUICommand_ConfigValueReq,
				OnIMGUICommand_ConfigValueUpdate

			}
			public string Data { get; set; }
			public string Data2 { get; set; }

			public string Data3 { get; set; }
			public CommandType TypeOfCommand { get; set; }

			public Dictionary<Int32, Int64> BuffDurations = new Dictionary<int, Int64>();
			public Int64 LastUpdate = 0;

			public static OnCommandData Aquire()
			{
				OnCommandData obj;
				if (!StaticObjectPool.TryPop<OnCommandData>(out obj))
				{
					obj = new OnCommandData();
				}

				return obj;
			}
			public void Dispose()
			{
				TypeOfCommand = CommandType.None;
				Data = String.Empty;
				Data2 = String.Empty;
				Data3 = String.Empty;
				StaticObjectPool.Push(this);
			}
			~OnCommandData()
			{
				//DO NOT CALL DISPOSE FROM THE FINALIZER! This should only ever be used in using statements
				//if this is called, it will cause the domain to hang in the GC when shuttind down
				//This is only here to warn you

			}
		}
	}
}