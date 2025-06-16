using E3NextProxy.Models;
using NetMQ;
using NetMQ.Sockets;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace E3NextProxy
{
	
	internal class Program
	{
		[DllImport("Kernel32")]
		private static extern bool SetConsoleCtrlHandler(EventHandler handler, bool add);
		private delegate bool EventHandler(CtrlType sig);
		static EventHandler _handler;

		enum CtrlType
		{
			CTRL_C_EVENT = 0,
			CTRL_BREAK_EVENT = 1,
			CTRL_CLOSE_EVENT = 2,
			CTRL_LOGOFF_EVENT = 5,
			CTRL_SHUTDOWN_EVENT = 6
		}

		private static bool Handler(CtrlType sig)
		{
			Console.WriteLine("Exiting system due to external CTRL-C, or process kill, or shutdown");
			if(File.Exists(_fullFileName))
			{
				File.Delete(_fullFileName);
			}
			return true;
		}

		static E3NextProxy.Proxy_Manual m_proxy;
		//lets scan the file system to find files so we can connect to start the proxy
		static string _directoryLocation = $@"D:\EQ\MQLive\config\e3 Macro Inis\SharedData\";
		static string _fileName = "proxy_pubsubport.txt";
		static string _fullFileName = _fileName;
		static Int32 _XPublisherPort;
		//static Int32 _XSubPort;
		static string _localIP = "127.0.0.1";
		static string _FQDN = "";
		static void Main(string[] args)
		{
			//capture to clean up on force close
			_handler += new EventHandler(Handler);
			SetConsoleCtrlHandler(_handler, true);

			//purpose of this program is to provide a Proxy between clients and consumers. This is basically the Server mode instead of the P2P mode that is default for E3N Networking
			//this is useful if you run a lot of bots as its far more efficent thread wise. 
			//if running 54 bots, that would be 2900+ threads vs just 108 threads using the proxy, it scales a lot better, tho less convient to run a server vs just peer to peer. 

			//_XPublisherPort = FreeTcpPort();
			_XPublisherPort = 5698;  //5697-5699 are current unassigned
			//_XSubPort = FreeTcpPort();
			_localIP = GetLocalIPAddress();
			_FQDN = GetFQDN();
			if (!CreateInfoFile(_localIP, _XPublisherPort))
			{
				return;
			}


			m_proxy = new Proxy_Manual();
			m_proxy.Start(_XPublisherPort);
			var xSubTaskAdd = Task.Factory.StartNew(() => { AddSubscribers(_localIP); }, CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
			var sub1task = Task.Factory.StartNew(() => { SubScribeReader(_XPublisherPort, _localIP); }, CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
			Console.WriteLine("Press enter to end");

			var proxiesToConnecTo = System.Configuration.ConfigurationManager.AppSettings["ProxiesToConnectTo"];
			
			List<string> proxies = proxiesToConnecTo.Split(new char[] {','}, StringSplitOptions.RemoveEmptyEntries).ToList();

			foreach (string proxy in proxies)
			{
				string tproxy = proxy.Replace(" ", "");
				if (tproxy == _localIP || String.Equals(tproxy,_FQDN,StringComparison.OrdinalIgnoreCase)) continue;
				//external connections are +1 the port number. 
				m_proxy.AddExteranlProxySubBinding($"tcp://{tproxy}:{_XPublisherPort+1}");
			}
			Console.ReadLine();

		}
		public static string GetFQDN()
		{
			string domainName = IPGlobalProperties.GetIPGlobalProperties().DomainName;
			string hostName = Dns.GetHostName();
			if(!string.IsNullOrEmpty(domainName) )
			{
				domainName = "." + domainName;
				if (!hostName.EndsWith(domainName))  // if hostname does not already include domain name
				{
					hostName += domainName;   // add the domain name part
				}
			}
			return hostName;                    // return the fully qualified name
		}
		public static void OldMain(string localIP, int XPublisherPort)
		{
			//try
			//{
			//	using (var xpubSocket = new XPublisherSocket())
			//	using (var xsubSocket = new XSubscriberSocket())
			//	{
			//		string connectionString = $"tcp://{localIP}:{XPublisherPort}";
			//		xpubSocket.Bind(connectionString);


			//		var sub1task = Task.Factory.StartNew(() => { SubScribeReader(XPublisherPort, localIP); }, CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);

			//		m_proxy = new E3NextProxy.Proxy(xsubSocket, xpubSocket);
			//		var xSubTaskAdd = Task.Factory.StartNew(() => { AddSubscribers(localIP, new List<int>() { _XSubPort }); }, CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);

			//		// blocks indefinitely
			//		m_proxy.StartAsync();
			//		Console.WriteLine($"Publish connection string:{connectionString}");
			//		Console.WriteLine("Press enter to end");
			//		Console.ReadLine();
			//		m_proxy.Stop();
			//	}
			//}
			//finally
			//{
			//	if (File.Exists(_fullFileName))
			//	{
			//		File.Delete(_fullFileName);

			//	}
			//}
		}
		public static void AddSubscribers(string localIP)
		{
		
			System.Threading.Thread.Sleep(1000);

			Dictionary<string,SubInfo> currentlyProcessing = new Dictionary<string, SubInfo>();
			List<string> removeItems = new List<string>();
			List<string> foldersToMonitor = new List<string>();
			foldersToMonitor.Add(_directoryLocation);
			
			while (true)
			{
				try
				{
					foreach (var folder in foldersToMonitor)
					{
						string[] files = Directory.GetFiles(folder);

						//file format is Name_Server_pubsubport.txt
						//IE: Rekken_Lazarus_pubsubport.txt

						foreach (var fileName in files)
						{
							if (String.Equals(fileName, _fullFileName, StringComparison.OrdinalIgnoreCase))
							{
								continue;
							}
							if (!currentlyProcessing.ContainsKey(fileName))
							{
								System.DateTime lastFileUpdate = System.IO.File.GetLastWriteTime(fileName);
								string data = System.IO.File.ReadAllText(fileName);
								//its now port:ipaddress
								string[] splitData = data.Split(new char[] { ',' });
								string port = splitData[0];
								string ipaddress = splitData[1];
								string connectionString = $"tcp://{ipaddress}:{port}";
								FileInfo fileInfo = new FileInfo(fileName);
								string userNameAndServer = fileInfo.Name.Replace("_pubsubport.txt", "");
								//change to Name:Server format as that is what the payload uses
								userNameAndServer = ReplaceFirst(userNameAndServer, "_", ":");
								m_proxy.AddLocalSubBinding(userNameAndServer, connectionString);
								Console.WriteLine($"[{System.DateTime.Now.ToString()}] New File Found: {fileName}. Connection String: {connectionString}");
								currentlyProcessing.Add(fileName, new SubInfo() { LastUpdateTime = lastFileUpdate, connectionString = connectionString });
							}
							else
							{
								//question is.. has it been modified?
								System.DateTime lastFileUpdate = System.IO.File.GetLastWriteTime(fileName);
								if (currentlyProcessing[fileName].LastUpdateTime < lastFileUpdate)
								{
									string connectionString = currentlyProcessing[fileName].connectionString;
									Console.WriteLine($"[{System.DateTime.Now.ToString()}] Reconnecting: {fileName}. Connection String: {connectionString}");
									FileInfo fileInfo = new FileInfo(fileName);
									Int32 indexOfUnderScore = fileInfo.Name.IndexOf("_");
									string userName = fileInfo.Name.Substring(0, indexOfUnderScore);
									//it has, remove it from processing, so that we can get the new one
									m_proxy.RemoveSubBinding(userName, connectionString);
									currentlyProcessing.Remove(fileName);
								}
							}

						}

						foreach (var info in currentlyProcessing)
						{
							if (!File.Exists(info.Key))
							{
								string fileName = info.Key;
								string connectionString = info.Value.connectionString;
								Console.WriteLine($"[{System.DateTime.Now.ToString()}] Disconnecting: {fileName} as it no longer exists. Connection String: {connectionString}");
								FileInfo fileInfo = new FileInfo(fileName);
								Int32 indexOfUnderScore = fileInfo.Name.IndexOf("_");
								string userName = fileInfo.Name.Substring(0, indexOfUnderScore);
								//it has, remove it from processing, so that we can get the new one
								m_proxy.RemoveSubBinding(userName, connectionString);
								removeItems.Add(fileName);
							}
						}
						foreach (var file in removeItems)
						{
							currentlyProcessing.Remove(file);
						}
						removeItems.Clear();
					}
				}
				catch (Exception ex) 
				{ 
					Console.WriteLine($"[{System.DateTime.Now.ToString()}] Error:" + ex.ToString());
				}
				
				Thread.Sleep(1000);
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
					//Console.WriteLine($"[{messageTopicReceived}] {messageReceived}");
					_totalMessageCount++;

					if(_stopWatch.ElapsedMilliseconds > _lastUpdateTime)
					{
						if(_lastTotalMessageCount > 0)
						{
							Int64 messageDelta =_totalMessageCount - _lastTotalMessageCount;
							Console.Write($"\r{messageDelta} per {_stopWatch.ElapsedMilliseconds - (_lastUpdateTime-1000)} milliseconds");
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
				dllFullPath= @"D:\EQ\MQEmu\mono\macros\e3\";
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
		public static string ReplaceFirst(string text, string search, string replace)
		{
			int pos = text.IndexOf(search);
			if (pos < 0)
			{
				return text;
			}
			return text.Substring(0, pos) + replace + text.Substring(pos + search.Length);
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
