using E3Core.Processors;
using MonoCore;
using NetMQ;
using NetMQ.Sockets;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace E3Core.Server
{
    /// <summary>
    /// This reads commands from the UI client and queues them up for execution
    /// </summary>
    public class PubClient
    {
        public static ConcurrentQueue<string> _pubCommands = new ConcurrentQueue<string>();
        private static IMQ MQ = E3.MQ;

        Task _serverThread;
        private Int32 _port;
        public void Start(Int32 port)
        {
            _port = port;
            _serverThread = Task.Factory.StartNew(() => { Process(); }, CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
        }
        public static bool NowCastInQueue()
        {
            if(_pubCommands.Count>0)
            {
                if(_pubCommands.TryPeek(out var result))
                {
                    if(result.StartsWith("/nowcast "))
                    {
                        return true;
                    }
                }
            }
            return false;
        }
        public static void ProcessRequests()
        {
            while (_pubCommands.Count > 0)
            {
                string message;
                if(_pubCommands.TryDequeue(out message))
                {

                    MQ.Cmd(message);
                }
            }
        }
        public void Process()
        {
			//need to do this so double parses work in other languages
			Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");

			TimeSpan recieveTimeout = new TimeSpan(0, 0, 0, 0, 5);
            using (var subSocket = new SubscriberSocket())
            {
                try
                {
                    subSocket.Options.ReceiveHighWatermark = 1000;
                    subSocket.Options.TcpKeepalive = true;
                    subSocket.Options.TcpKeepaliveIdle = TimeSpan.FromSeconds(5);
                    subSocket.Options.TcpKeepaliveInterval = TimeSpan.FromSeconds(1);
                    subSocket.Connect("tcp://127.0.0.1:" + _port);
                    subSocket.Subscribe("OnCommand");
                    while (Core.IsProcessing && E3.NetMQ_PubClientThradRun)
                    {
                        string messageTopicReceived;
                        if (subSocket.TryReceiveFrameString(recieveTimeout, out messageTopicReceived))
                        {
                            string messageReceived = subSocket.ReceiveFrameString();
                            if (messageTopicReceived == "OnCommand")
                            {
                                _pubCommands.Enqueue(messageReceived);
                            }
                        }
                           
                    }
                }
                catch(Exception)
                {

                }
                    
            }
            MQ.WriteDelayed("Shutting down PubClient Thread.");
        }
    }
}
