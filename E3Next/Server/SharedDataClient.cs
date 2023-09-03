using E3Core.Data;
using E3Core.Processors;
using E3Core.Settings;
using MonoCore;
using NetMQ;
using NetMQ.Sockets;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
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


		Dictionary<string, Task> _processTasks = new Dictionary<string, Task>();
		static object _processLock = new object();
		public bool RegisterUser(string user)
		{

			//see if they exist in the collection
			if (!TopicUpdates.ContainsKey(user))
			{
				lock (_processLock)
				{
					if (!_processTasks.ContainsKey(user))
					{
						//lets see if the file exists
						string filePath = BaseSettings.GetSettingsFilePath($"{user}_{E3.ServerName}_pubsubport.txt");

						if (System.IO.File.Exists(filePath))
						{
							//lets load up the port information
							TopicUpdates.TryAdd(user, new ConcurrentDictionary<string, ShareDataEntry>());
							string port = System.IO.File.ReadAllText(filePath);

							var newTask = Task.Factory.StartNew(() => { Process(user, port, filePath); }, CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
							_processTasks.Add(user, newTask);
							return true;

						}
					}

				}

			}
			return false;
		}

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

					if (typeInfo == OnCommandData.CommandType.BroadCastMessage)
					{
						Int32 indexOfSeperator = message.IndexOf(':');
						Int32 currentIndex = 0;
						string user = message.Substring(currentIndex, indexOfSeperator);
						currentIndex = indexOfSeperator + 1;
						string bcMessage = message.Substring(currentIndex, message.Length - currentIndex);

						//MQ write it out
						MQ.Write($"\ar<\ay{user}\ar> \aw{bcMessage}");
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
						if (typeInfo == OnCommandData.CommandType.OnCommandGroup)
						{
							//check to see if we are part of their group
							if (user == E3.CurrentName)
							{
								//not for us only group members
								break;
							}
							//check to see if we are part of their group
							Int32 groupMemberIndex = MQ.Query<Int32>($"${{Group.Member[{user}].Index}}");

							if (groupMemberIndex < 0)
							{
								//ignore it
								break;
							}
						}
						else if (typeInfo == OnCommandData.CommandType.OnCommandGroupAll)
						{
							//check to see if we are part of their group
							Int32 groupMemberIndex = MQ.Query<Int32>($"${{Group.Member[{user}].Index}}");

							if (groupMemberIndex < 0)
							{
								//ignore it
								break;
							}
						}
						else if (typeInfo == OnCommandData.CommandType.OnCommandRaid)
						{
							//check to see if we are part of their group
							var inRaid = MQ.Query<bool>($"${{Raid.Member[{user}]}}");

							if (!inRaid)
							{
								break;
							}
						}

						MQ.Write($"\ag<\ap{user}\ag> Command:" + command);
						if (String.Compare("/mono e3", command, StringComparison.OrdinalIgnoreCase) == 0)
						{
							//we are being asked to restart e3, kinda need to kill this entire thing :D
							if(Core._MQ2MonoVersion>=0.22m)
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
							if (String.Compare(noparseString, "true", true) == 0)
							{
								MQ.Cmd("/noparse " + command);
							}
							else
							{
								MQ.Cmd(command);
							}

						}
						
					}



				}

			}
		}
		public void Process(string user, string port, string fileName)
		{
			System.DateTime lastFileUpdate = System.IO.File.GetLastWriteTime(fileName);
			string OnCommandName = "OnCommand-" + E3.CurrentName;


			//some type of delay if our sub errors out.
			System.Threading.Thread.Sleep(100);
			//timespan we expect to have some type of message
			TimeSpan recieveTimeout = new TimeSpan(0, 0, 0, 0, 2);
			using (var subSocket = new SubscriberSocket())
			{
				try
				{
					subSocket.Options.ReceiveHighWatermark = 1000;
					subSocket.Options.TcpKeepalive = true;
					subSocket.Options.TcpKeepaliveIdle = TimeSpan.FromSeconds(5);
					subSocket.Options.TcpKeepaliveInterval = TimeSpan.FromSeconds(1);
					subSocket.Connect("tcp://127.0.0.1:" + port);
					subSocket.Subscribe(OnCommandName);
					subSocket.Subscribe("OnCommand-All");
					subSocket.Subscribe("OnCommand-Group");
					subSocket.Subscribe("OnCommand-GroupAll");
					subSocket.Subscribe("OnCommand-Raid");
					subSocket.Subscribe("BroadCastMessage");
					subSocket.Subscribe("${Me."); //all Me stuff should be subscribed to
					MQ.Write("\agShared Data Client: Connecting to user:" + user + " on port:" + port); ;

					Int32 messageCount = 0;
					while (Core.IsProcessing)
					{
						string messageTopicReceived;
						if (subSocket.TryReceiveFrameString(recieveTimeout, out messageTopicReceived))
						{


							string messageReceived = subSocket.ReceiveFrameString();

							if (messageTopicReceived == "OnCommand-All")
							{
								var data = OnCommandData.Aquire();
								data.Data = messageReceived;
								data.TypeOfCommand = OnCommandData.CommandType.OnCommandAll;

								CommandQueue.Enqueue(data);
							}
							else if (messageTopicReceived == "OnCommand-Group")
							{
								var data = OnCommandData.Aquire();
								data.Data = messageReceived;
								data.TypeOfCommand = OnCommandData.CommandType.OnCommandGroup;

								CommandQueue.Enqueue(data);
							}
							else if (messageTopicReceived == "OnCommand-GroupAll")
							{
								var data = OnCommandData.Aquire();
								data.Data = messageReceived;
								data.TypeOfCommand = OnCommandData.CommandType.OnCommandGroupAll;

								CommandQueue.Enqueue(data);
							}
							else if (messageTopicReceived == "OnCommand-Raid")
							{
								var data = OnCommandData.Aquire();
								data.Data = messageReceived;
								data.TypeOfCommand = OnCommandData.CommandType.OnCommandRaid;

								CommandQueue.Enqueue(data);
							}
							else if (messageTopicReceived == "BroadCastMessage")
							{
								var data = OnCommandData.Aquire();
								data.Data = messageReceived;
								data.TypeOfCommand = OnCommandData.CommandType.BroadCastMessage;

								CommandQueue.Enqueue(data);
							}
							else if (messageTopicReceived == OnCommandName)
							{
								var data = OnCommandData.Aquire();
								data.Data = messageReceived;
								data.TypeOfCommand = OnCommandData.CommandType.OnCommandName;

								CommandQueue.Enqueue(data);

							}
							else
							{
								Int64 updateTime = Core.StopWatch.ElapsedMilliseconds;
								if (!TopicUpdates[user].ContainsKey(messageTopicReceived))
								{
									TopicUpdates[user].TryAdd(messageTopicReceived, new ShareDataEntry() { Data = messageReceived, LastUpdate = updateTime });
								}
								var entry = TopicUpdates[user][messageTopicReceived];

								//why do work if its the same data?									
								if (entry.Data != messageReceived)
								{
									entry.Data = messageReceived;
									entry.LastUpdate = updateTime;
								}

							}



						}
						else
						{   //we didn't get a message in the timespan we were expecting, verify if we need to reconnect
							try
							{
								System.DateTime currentTime = System.IO.File.GetLastWriteTime(fileName);
								if (currentTime > lastFileUpdate)
								{
									MQ.Write("\agShared Data Client: Disconnecting port:" + port + " for toon:" + user);
									//shutown the socket and restart it
									subSocket.Disconnect("tcp://127.0.0.1:" + port);
									port = System.IO.File.ReadAllText(fileName);
									MQ.Write("\agShared Data Client: Reconnecting to port:" + port + " for toon:" + user);
									subSocket.Connect("tcp://127.0.0.1:" + port);
									lastFileUpdate = currentTime;
								}
							}
							catch (Exception ex)
							{
								//file deleted most likely, kill the thread
								MQ.Write("\agShared Data Client: Issue reading port file, shutting down thread for toon:" + user);

								subSocket.Dispose();
								if (TopicUpdates.TryRemove(user, out var tout))
								{

								}

								break;

							}
						}


					}
					
					subSocket.Dispose();
				}
				catch (Exception)
				{
				}

			}


			MQ.Write($"Shutting down Share Data Thread for {user}.");
			lock (_processLock)
			{
				_processTasks.Remove(user);
			}
		}

		public class OnCommandData
		{
			public enum CommandType
			{
				None,
				OnCommandAll,
				OnCommandGroup,
				OnCommandGroupAll,
				OnCommandRaid,
				BroadCastMessage,
				OnCommandName
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
