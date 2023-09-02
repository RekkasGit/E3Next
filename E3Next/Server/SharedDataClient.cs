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

namespace E3Core.Server
{
    
	public class ShareDataEntry
	{
		public String Data { get; set; }
		public Int64 LastUpdate { get; set;
		}
	}
	public class SharedDataClient
    {
		public ConcurrentDictionary<string, ConcurrentDictionary<string, ShareDataEntry>> TopicUpdates = new ConcurrentDictionary<string, ConcurrentDictionary<string, ShareDataEntry>>(StringComparer.OrdinalIgnoreCase);
		
		private static IMQ MQ = E3.MQ;


		List<Task> _processTasks = new List<Task>();
		public void RegisterUser(string user)
		{

			//see if they exist in the collection
			if(!TopicUpdates.ContainsKey(user))
			{
				//lets see if the file exists
				string filePath = BaseSettings.GetSettingsFilePath(user + "_pubsubport.txt");

				if (System.IO.File.Exists(filePath))
				{
					//lets load up the port information
					TopicUpdates.TryAdd(user, new ConcurrentDictionary<string, ShareDataEntry>());
					string port = System.IO.File.ReadAllText(filePath);

					var newTask = Task.Factory.StartNew(() => { Process(user,port, filePath); }, CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
					_processTasks.Add(newTask);
					
				}
			}
		}
		public void Process(string user,string port,string fileName)
		{
			System.DateTime lastFileUpdate = System.IO.File.GetLastWriteTime(fileName);
			while (Core.IsProcessing)
			{
				//some type of delay if our sub errors out.
				System.Threading.Thread.Sleep(100);
				TimeSpan recieveTimeout = new TimeSpan(0, 0, 0, 0, 5);
				using (var subSocket = new SubscriberSocket())
				{
					try
					{
						subSocket.Options.ReceiveHighWatermark = 1000;
						subSocket.Options.TcpKeepalive = true;
						subSocket.Options.TcpKeepaliveIdle = TimeSpan.FromSeconds(5);
						subSocket.Options.TcpKeepaliveInterval = TimeSpan.FromSeconds(1);
						subSocket.Connect("tcp://127.0.0.1:" + port);
						subSocket.Subscribe("${Me.BuffInfo}");
						subSocket.Subscribe("${Me.PetBuffInfo}");
						MQ.Write("\agShared Data Client: Connecting to user:" + user + " on port:" + port); ;

						while (Core.IsProcessing)
						{
							string messageTopicReceived;
							if (subSocket.TryReceiveFrameString(recieveTimeout, out messageTopicReceived))
							{
								string messageReceived = subSocket.ReceiveFrameString();
								
								Int64 updateTime = Core.StopWatch.ElapsedMilliseconds;
								if (!TopicUpdates[user].ContainsKey(messageTopicReceived))
								{
									TopicUpdates[user].TryAdd(messageTopicReceived, new ShareDataEntry() { Data = messageReceived,LastUpdate= updateTime });
								}
								var entry = TopicUpdates[user][messageTopicReceived];
								entry.Data = messageReceived;
								entry.LastUpdate = updateTime;
							}
							System.Threading.Thread.Sleep(1);
							try
							{
								System.DateTime currentTime = System.IO.File.GetLastWriteTime(fileName);
								if (currentTime > lastFileUpdate)
								{
									MQ.Write("\agShared Data Client: Disconnecting port:" + port + "for toon:"+user);
									//shutown the socket and restart it
									subSocket.Disconnect("tcp://127.0.0.1:" + port);
									port = System.IO.File.ReadAllText(fileName);
									MQ.Write("\agShared Data Client: Reconnecting to port:" + port + "for toon:" + user);
									subSocket.Connect("tcp://127.0.0.1:" + port);
									lastFileUpdate = currentTime;
								}
							}
							catch(Exception ex)
							{
								//file deleted most likely, kill the thread
								MQ.Write("\agShared Data Client: Issue reading port file, shutting down thread for toon:" + user);

								subSocket.Dispose();
								if(TopicUpdates.TryRemove(user, out var tout))
								{
									
								}
								
								break;

							}
						}
						
					}
					catch (Exception)
					{
					}

				}

			}
			MQ.Write($"Shutting down Share Data Thread for {user}.");
		}
	}
}
