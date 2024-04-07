using E3Core.Processors;
using E3Core.Settings;
using E3Core.Utility;
using MonoCore;
using NetMQ;
using NetMQ.Sockets;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static E3Core.Server.SharedDataClient;

namespace E3Core.Server
{

    /// <summary>
    /// Sends events out to the UI client, or whoever
    /// </summary>
    public class PubServer
    {
        private static IMQ MQ = E3.MQ;
        class topicMessagePair:IDisposable
        {
            public string topic;
            public string message;
            private topicMessagePair()
            {
                //do not let others instance us
            }
			public static topicMessagePair Aquire()
			{
				topicMessagePair obj;
				if (!StaticObjectPool.TryPop<topicMessagePair>(out obj))
				{
					obj = new topicMessagePair();
				}
				return obj;
			}
			public void Dispose()
			{
                topic = string.Empty;
                message = string.Empty;
				StaticObjectPool.Push(this);
			}
			~topicMessagePair()
			{
				//DO NOT CALL DISPOSE FROM THE FINALIZER! This should only ever be used in using statements
				//if this is called, it will cause the domain to hang in the GC when shutting down
				//This is only here to warn you
			}
		}

        Task _serverThread = null;

        public static ConcurrentQueue<string> IncomingChatMessages = new ConcurrentQueue<string>();
        public static ConcurrentQueue<string> MQChatMessages = new ConcurrentQueue<string>();
        public static ConcurrentQueue<string> CommandsToSend = new ConcurrentQueue<string>();
        private static ConcurrentQueue<topicMessagePair> _topicMessages = new ConcurrentQueue<topicMessagePair>();

        public static Int32 PubPort = 0;



        public void Start(Int32 port)
        {
            PubPort = port;
            string localIP = e3util.GetLocalIPAddress();
			string settingsFilePath = BaseSettings.GetSettingsFilePath("");

			if (!settingsFilePath.EndsWith(@"\"))
			{
				settingsFilePath += @"\";
			}

			settingsFilePath += @"SharedData\";

            if(!Directory.Exists(settingsFilePath))
            {
                try
                {
					//in case you have 6 clients all trying to create the directory at once.
                    Directory.CreateDirectory(settingsFilePath);
				}
				catch (Exception)
                {
                }
            }
			string filePath = settingsFilePath+$"{E3.CurrentName}_{E3.ServerName}_pubsubport.txt";

            //System.IO.File.Delete(filePath);
            bool updatedFile = false;
            Int32 counter = 0;
            while(!updatedFile)
            {
                counter++;
				try
				{

					System.IO.File.WriteAllText(filePath, port.ToString() + "," + localIP);
					updatedFile = true;
				}
				catch (Exception ex)
				{
                    System.Threading.Thread.Sleep(100);
				    if(counter>20) //allow up 2 seconds worth of failures before we throw an exception.
                    {
                        throw new Exception($"Cannot write out the pubsubport file {filePath}, some other process is using it. Try manually deleting it. ErrorMessage:" + ex.Message);
                    }
                }

			}
			_serverThread = Task.Factory.StartNew(() => { Process(filePath); }, CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);

        }
        public  static void AddTopicMessage(string topic, string message)
        {  
            topicMessagePair t = topicMessagePair.Aquire();
            t.topic = topic;
            t.message=message;
		     _topicMessages.Enqueue(t);
        }
        private void Process(string filePath)
        {
			//need to do this so double parses work in other languages
			Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");
			AsyncIO.ForceDotNet.Force();
            using (var pubSocket = new PublisherSocket())
            {
                pubSocket.Options.SendHighWatermark = 50000;
                
                pubSocket.Bind("tcp://0.0.0.0:" + PubPort.ToString());
                
                while (Core.IsProcessing && E3.NetMQ_PubServerThradRun)
                {
                    while (_topicMessages.Count > 0)
                    {
                        if (_topicMessages.TryDequeue(out var value))
                        {
                            //using so that we put it back into the memory pool
                            using(value)
                            {
								pubSocket.SendMoreFrame(value.topic).SendFrame(value.message);
							}
                        }
                    }
                   while(IncomingChatMessages.Count > 0)
                    {
                        string message;
                        if (IncomingChatMessages.TryDequeue(out message))
                        {

                            pubSocket.SendMoreFrame("OnIncomingChat").SendFrame(message);
                        }
                    }
                   while (MQChatMessages.Count > 0)
                    {
                        string message;
                        if (MQChatMessages.TryDequeue(out message))
                        {

                            pubSocket.SendMoreFrame("OnWriteChatColor").SendFrame(message);

                        }
                    }
                    while(CommandsToSend.Count > 0)
                    {
                        string message;
                        if (CommandsToSend.TryDequeue(out message))
                        {

                            pubSocket.SendMoreFrame("OnCommand").SendFrame(message);

                        }
                    }
                    System.Threading.Thread.Sleep(1);
                }
                try
                {
					System.IO.File.Delete(filePath);

				}
				catch (Exception)
                {
                    MQ.Write("Issue deleting pubsub.txt file");
                }
				MQ.Write("Shutting down PubServer Thread.");
            }
        }
    }
}
