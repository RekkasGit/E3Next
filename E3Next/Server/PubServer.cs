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
        Task _serverThread = null;

        public static ConcurrentQueue<string> _pubMessages = new ConcurrentQueue<string>();
        public static ConcurrentQueue<string> _pubWriteColorMessages = new ConcurrentQueue<string>();
        public static ConcurrentQueue<string> _pubCommands = new ConcurrentQueue<string>();
        public static ConcurrentQueue<string> _hpValues = new ConcurrentQueue<string>();
        public static Int32 PubPort = 0;

        public void Start(Int32 port)
        {
            PubPort = port;
            _serverThread = Task.Factory.StartNew(() => { Process(); }, CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);

        }
        private void Process()
        {
            AsyncIO.ForceDotNet.Force();
            using (var pubSocket = new PublisherSocket())
            {
                pubSocket.Options.SendHighWatermark = 1000;
                
                pubSocket.Bind("tcp://127.0.0.1:" + PubPort.ToString());
                
                while (Core._isProcessing)
                {
                    if (_hpValues.Count > 0)
                    {
                        string message;
                        if (_hpValues.TryDequeue(out message))
                        {

                            pubSocket.SendMoreFrame("HPValue").SendFrame(message);
                        }
                    }
                    else if(_pubMessages.Count > 0)
                    {
                        string message;
                        if (_pubMessages.TryDequeue(out message))
                        {

                            pubSocket.SendMoreFrame("OnIncomingChat").SendFrame(message);
                        }
                    }
                    else if (_pubWriteColorMessages.Count > 0)
                    {
                        string message;
                        if (_pubWriteColorMessages.TryDequeue(out message))
                        {

                            pubSocket.SendMoreFrame("OnWriteChatColor").SendFrame(message);

                        }
                    }
                    else if (_pubCommands.Count > 0)
                    {
                        string message;
                        if (_pubCommands.TryDequeue(out message))
                        {

                            pubSocket.SendMoreFrame("OnCommand").SendFrame(message);

                        }
                    }
                    else
                    {
                        System.Threading.Thread.Sleep(1);
                    }
                }
            }
        }
    }
}
