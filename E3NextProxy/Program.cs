using NetMQ.Sockets;
using NetMQ;
using System;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Net;
using System.Reflection;
using System.IO;
using System.Threading;
using System.Collections.Generic;
using E3NextProxy.Models;
using System.Diagnostics;

namespace E3NextProxy
{
	internal class Program
	{
		static E3NextProxy.Proxy m_proxy;
		//lets scan the file system to find files so we can connect to start the proxy
		static string _directoryLocation = $@"D:\EQ\MQLive\config\e3 Macro Inis\SharedData\";
		static string _fileName = "proxy_pubsubport.txt";
		static string _fullFileName = _fileName;

		static void Main(string[] args)
		{
			//purpose of this program is to provide a Proxy between clients and consumers. This is basically the Server mode instead of the P2P mode that is default for E3N Networking
			//this is useful if you run a lot of bots as its far more efficent thread wise. 
			//if running 54 bots, that would be 2900+ threads vs just 108 threads using the proxy, it scales a lot better, tho less convient to run a server vs just peer to peer. 
			
			Int32 XPublisherPort = FreeTcpPort();
			Int32 XSubPort = FreeTcpPort();
			string localIP = GetLocalIPAddress();

			if (!CreateInfoFile(localIP, XPublisherPort))
			{
				return;
			}


			try
			{
				using (var xpubSocket = new XPublisherSocket())
				using (var xsubSocket = new XSubscriberSocket())
				{
					string connectionString = $"tcp://{localIP}:{XPublisherPort}";
					xpubSocket.Bind(connectionString);


					var sub1task = Task.Factory.StartNew(() => { SubScribeReader(XPublisherPort, localIP); }, CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);

					m_proxy = new E3NextProxy.Proxy(xsubSocket, xpubSocket);
					var xSubTaskAdd = Task.Factory.StartNew(() => { AddSubscribers(localIP, new List<int>() { XSubPort }); }, CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);

					// blocks indefinitely
					m_proxy.StartAsync();
					Console.WriteLine($"Publish connection string:{connectionString}");
					Console.WriteLine("Press enter to end");
					Console.ReadLine();
					m_proxy.Stop();
				}
			}
			finally 
			{ 
				File.Delete(_fullFileName); 
			}


		
			
		}

		public static void AddSubscribers(string localIP,List<Int32> publisherPorts)
		{
		
			System.Threading.Thread.Sleep(1000);

			Dictionary<string,SubInfo> currentlyProcessing = new Dictionary<string, SubInfo>();
			List<string> removeItems = new List<string>();

			while (true)
			{
				string[] files = Directory.GetFiles(_directoryLocation);

				foreach(var fileName in files)
				{
					if (String.Equals(fileName, _fullFileName, StringComparison.OrdinalIgnoreCase))
					{
						continue;
					}
					if(!currentlyProcessing.ContainsKey(fileName))
					{
						System.DateTime lastFileUpdate = System.IO.File.GetLastWriteTime(fileName);
						string data = System.IO.File.ReadAllText(fileName);
						//its now port:ipaddress
						string[] splitData = data.Split(new char[] { ',' });
						string port = splitData[0];
						string ipaddress = splitData[1];
						string connectionString = $"tcp://{ipaddress}:{port}";
						m_proxy.AddSubBinding(connectionString);
						Console.WriteLine($"[{System.DateTime.Now.ToString()}] New File Found: {fileName}. Connection String: {connectionString}");
						currentlyProcessing.Add(fileName, new SubInfo() { LastUpdateTime=lastFileUpdate,connectionString=connectionString});
					}
					else
					{
						//question is.. has it been modified?
						System.DateTime lastFileUpdate = System.IO.File.GetLastWriteTime(fileName);
						if (currentlyProcessing[fileName].LastUpdateTime<lastFileUpdate)
						{
							string connectionString = currentlyProcessing[fileName].connectionString;
							Console.WriteLine($"[{System.DateTime.Now.ToString()}] Reconnecting: {fileName}. Connection String: {connectionString}");
							//it has, remove it from processing, so that we can get the new one
							m_proxy.RemoveSubBinding(connectionString);
							currentlyProcessing.Remove(fileName);
						}
					}

				}
				
				foreach(var info in currentlyProcessing)
				{
					if(!File.Exists(info.Key))
					{
						string fileName = info.Key;
						string connectionString = info.Value.connectionString;
						Console.WriteLine($"[{System.DateTime.Now.ToString()}] Disconnecting: {fileName} as it no longer exists. Connection String: {connectionString}");
						//it has, remove it from processing, so that we can get the new one
						m_proxy.RemoveSubBinding(connectionString);
						removeItems.Add(fileName);
					}
				}
				foreach(var file in removeItems)
				{
					currentlyProcessing.Remove(file);
				}
				removeItems.Clear();
				System.Threading.Thread.Sleep(1000);
			}

		

		}

		public static void SubPublisherWriter(string user, Int32 port, string ipaddress)
		{

			using (var pubSocket = new PublisherSocket())
			{
				pubSocket.Bind($"tcp://{ipaddress}:{port}");
				Console.WriteLine("Publisher socket connecting...");
				pubSocket.Options.SendHighWatermark = 1000;
				var rand = new Random(50);
				while (true)
				{
					var randomizedTopic = rand.NextDouble();
					if (randomizedTopic > 0.5)
					{
						var msg = $"{user} TopicA msg-" + randomizedTopic;
						Console.WriteLine("Sending message : {0}", msg);
						pubSocket.SendMoreFrame("TopicA").SendFrame(msg);
					}
					else
					{
						var msg = $"{user} TopicB msg-" + randomizedTopic;
						Console.WriteLine("Sending message : {0}", msg);
						pubSocket.SendMoreFrame("TopicB").SendFrame(msg);
					}
					System.Threading.Thread.Sleep(1000);	
				}
			}

		}
		static Int64 _totalMessageCount = 0;
		static Int64 _lastTotalMessageCount = 0;
		static Int64 _lastUpdateTime;
		static Stopwatch _stopWatch = new Stopwatch();
		public static void SubScribeReader(Int32 port, string ipaddress)
		{
			_stopWatch.Start();
			using (var subSocket = new SubscriberSocket())
			{
				subSocket.Connect($"tcp://{ipaddress}:{port}");
				subSocket.Options.ReceiveHighWatermark = 1000;
				subSocket.Subscribe("");
				Console.WriteLine("Subscriber socket connecting...");

				while (true)
				{
					string messageTopicReceived = subSocket.ReceiveFrameString();
					string messageReceived = subSocket.ReceiveFrameString();
					_totalMessageCount++;

					if(_stopWatch.ElapsedMilliseconds > _lastUpdateTime)
					{
						if(_lastTotalMessageCount > 0)
						{
							Int64 messageDelta =_totalMessageCount - _lastTotalMessageCount;
							Console.WriteLine($"{messageDelta} per {_stopWatch.ElapsedMilliseconds - (_lastUpdateTime-1000)} milliseconds");
						}

						_lastTotalMessageCount = _totalMessageCount;
						_lastUpdateTime = _stopWatch.ElapsedMilliseconds+1000;
					}
				
				}
			}


		}
		public static bool CreateInfoFile(string localIP,Int32 XPublisherPort)
		{
			//need to create a file in the macroquest directory, walk backwards till we get to the root with the config file
			//we should be in the \mono\macros\e3 folder, might cause an issue if this is running and updates are happening

			var dllFullPath = Assembly.GetExecutingAssembly().CodeBase.Replace("file:///", "").Replace("/", "\\").Replace("E3NextProxy.exe", "");

			if(System.Diagnostics.Debugger.IsAttached)
			{
				dllFullPath=@"D:\EQ\MQLive\mono\macros\e3\";
			}

			DirectoryInfo currentDirectory = new DirectoryInfo(dllFullPath);

			
			while (!IsMQInPath(currentDirectory.FullName))
			{
				currentDirectory = Directory.GetParent(currentDirectory.FullName);

				if (currentDirectory == null)
				{
					//couldn't find MQ root directory kick out
					Console.WriteLine("Couldn't find MacroQuest.exe in parent folders, press enter to exit");
					Console.ReadLine();
					return false;
				}
			}
			//we are now in the root MQ folder, lets go and create our shared data file
			string configPath = currentDirectory.FullName + @"\config\e3 Macro Inis\SharedData";
			DirectoryInfo configPathDirectory = new DirectoryInfo(configPath);
			if (!configPathDirectory.Exists)
			{
				configPathDirectory.Create();
			}
			_directoryLocation = configPath;
			Console.WriteLine("Config File Path:" + _directoryLocation);

			//now delete the old file if it exists
			string fullPathName = configPathDirectory.FullName + @"\"+_fileName;

			if (File.Exists(fullPathName))
			{
				File.Delete(fullPathName);
			}
			_fullFileName = fullPathName;
			Console.WriteLine("Config File name:" + _fullFileName);
			File.WriteAllText(_fullFileName, $"{XPublisherPort},{localIP}");

			return true;
		}
		public static string GetLocalIPAddress()
		{
			//https://stackoverflow.com/questions/6803073/get-local-ip-address

			string localIP;
			using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0))
			{
				socket.Connect("8.8.8.8", 65530);
				IPEndPoint endPoint = socket.LocalEndPoint as IPEndPoint;
				localIP = endPoint.Address.ToString();
			}
			return localIP;
		}
		static bool IsMQInPath(string path)
		{

			string[] files = Directory.GetFiles(path);

			foreach(var file in files)
			{
				if(file.EndsWith(@"\MacroQuest.exe"))
				{
					return true;
				}
			}
			return false;
		}

		static int FreeTcpPort()
		{
			TcpListener l = new TcpListener(IPAddress.Loopback, 0);
			l.Start();
			int port = ((IPEndPoint)l.LocalEndpoint).Port;
			l.Stop();
			return port;
		}
	}
}
