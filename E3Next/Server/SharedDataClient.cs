using E3Core.Classes;
using E3Core.Data;
using E3Core.Processors;
using E3Core.Settings;
using MonoCore;
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
			if(!path.EndsWith(@"\"))
			{
				path  += @"\";
			}


			lock (_processLock)
			{
				if (!UsersConnectedTo.ContainsKey(user))
				{
					//lets see if the file exists
					string filePath =$"{path}{user}_{E3.ServerName}_pubsubport.txt";
					if(isproxy)
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
						if(_mainProcessingTask==null)
						{
							_mainProcessingTask= Task.Factory.StartNew(() => { Process(); }, CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
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

		
		public void ProcessCommands()
		{
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
					if(message.Contains("$\\{")) message = message.Replace("$\\{", "${");

					if (typeInfo == OnCommandData.CommandType.BroadCastMessage || typeInfo== OnCommandData.CommandType.BroadCastMessageZone)
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

						if(typeInfo== OnCommandData.CommandType.OnCommandAllExceptMeZone  || typeInfo == OnCommandData.CommandType.OnCommandAllZone || typeInfo ==OnCommandData.CommandType.OnCommandGroupZone 
							|| typeInfo== OnCommandData.CommandType.OnCommandGroupAllZone || typeInfo == OnCommandData.CommandType.OnCommandRaidZone || typeInfo == OnCommandData.CommandType.OnCommandRaidZoneNotMe)
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
						if (user == E3.CurrentName && (!(typeInfo == OnCommandData.CommandType.OnCommandName|| typeInfo== OnCommandData.CommandType.OnCommandChannel ||
							typeInfo== OnCommandData.CommandType.OnCommandGroupAll || typeInfo == OnCommandData.CommandType.OnCommandAll || typeInfo== OnCommandData.CommandType.OnCommandGroupAllZone|| 
							typeInfo==OnCommandData.CommandType.OnCommandAllZone || typeInfo == OnCommandData.CommandType.OnCommandRaid || typeInfo == OnCommandData.CommandType.OnCommandRaidZone)))
						{
							//if not an all type command and not us, kick out.
							//not for us only group members
							continue;
						}

						MQ.Write($"\ag<\ap{user}\ag> Command:" + command);
						if (command.StartsWith("/mono ",StringComparison.OrdinalIgnoreCase)|| command.StartsWith("/shutdown", StringComparison.OrdinalIgnoreCase))
						{
							//in case this is a restart command, we need to delay the command so it happens outside of the OnPulse. just assume all /mono commands are 
							//delayed
							if (Core._MQ2MonoVersion>=0.22m)
							{
								//needs to be delayed, so that the restarts happes outside of the E3N OnPulse
								MQ.Cmd(command,true);
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
								if (command.StartsWith(pair.Key, StringComparison.OrdinalIgnoreCase))
								{
									internalComand = true;
									//no need to send this to mq if its our own command, just drop it into the queues to be processed. 
									EventProcessor.ProcessMQCommand(command);
									break;
								}

							}
							if (!internalComand)
							{
								MQ.Cmd(command);

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
			catch(Exception) 
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
		private void ProcessTopicMessage(string user,string messageTopicReceived, string messageReceived)
		{

			//get the user from the payload
			ConcurrentDictionary<string, ShareDataEntry> usertopics;
			if (!TopicUpdates.TryGetValue(user,out usertopics))
			{
				usertopics =  new ConcurrentDictionary<string, ShareDataEntry>();	
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
		public void Process_CheckConnectionsIfStillValid(SubscriberSocket subSocket,ref  Int64 lastConnectionCheck)
		{
			if ((Core.StopWatch.ElapsedMilliseconds - lastConnectionCheck) > 2000)
			{
				lastConnectionCheck= Core.StopWatch.ElapsedMilliseconds;

				//lets been over 2 seconds, lets check to see if there are anyone we need to reconnect to
				foreach (var userInfo in UsersConnectedTo.Values.ToList())
				{
					if ((Core.StopWatch.ElapsedMilliseconds - userInfo.LastMessageTimeStamp) > 2000)
					{
						//been at least 2 seconds from this user, lets check to see what has happened to them.
						try
						{
							if(!System.IO.File.Exists(userInfo.FilePath))
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

							MQ.WriteDelayed("\arError, Disconnecting User:\ag" + userInfo.User + " Message:"+ex.Message);
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
				try
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
					subSocket.Subscribe("${Me."); //all Me stuff should be subscribed to
					subSocket.Subscribe("${Data."); //all the custom data keys a user can create
					subSocket.Subscribe("${DataChannel.");
				
					while (Core.IsProcessing && E3.NetMQ_SharedDataServerThreadRun)
					{
						Process_CheckNewConnections(subSocket);
						Process_CheckConnectionsIfStillValid(subSocket, ref lastConnectionCheck);

						string messageTopicReceived;
						if (subSocket.TryReceiveFrameString(recieveTimeout, out messageTopicReceived))
						{
							string messageReceived = subSocket.ReceiveFrameString();

							Int32 indexOfColon = messageReceived.IndexOf(':');
							string payloaduser = messageReceived.Substring(0, indexOfColon);
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
								connectionInfo.LastMessageTimeStamp=Core.StopWatch.ElapsedMilliseconds;
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
								if(payloaduser!=E3.CurrentName)
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
							else if (messageTopicReceived == OnCommandName)
							{	//bct commands
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
		
					}
					
					subSocket.Dispose();
				}
				catch (Exception ex)
				{
					//MQ.WriteDelayed("Error in shared data thread. Message:" + ex.Message + "  stack:" + ex.StackTrace);
				}

			}
			MQ.WriteDelayed($"Shutting down Share Data Thread.");
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
				OnCommandChannel
			}
			public string Data { get; set; }
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
