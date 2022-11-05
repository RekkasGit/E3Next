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

namespace E3Core.Server
{

    public class PubServer
    {

        class topicMessagePair
        {
            public string topic;
            public string message;
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
            _serverThread = Task.Factory.StartNew(() => { Process(); }, CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);

        }
        public  static void AddTopicMessage(string topic, string message)
        {
            topicMessagePair t = new topicMessagePair() { topic = topic, message = message };
            _topicMessages.Enqueue(t);
        }
        private void Process()
        {
            AsyncIO.ForceDotNet.Force();
            using (var pubSocket = new PublisherSocket())
            {
                pubSocket.Options.SendHighWatermark = 50000;
                
                pubSocket.Bind("tcp://127.0.0.1:" + PubPort.ToString());
                
                while (Core._isProcessing)
                {
                   
                    while (_topicMessages.Count > 0)
                    {
                        if (_topicMessages.TryDequeue(out var value))
                        {

                            pubSocket.SendMoreFrame(value.topic).SendFrame(value.message);
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
            }
        }
    }
}
