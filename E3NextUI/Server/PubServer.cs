using NetMQ;
using NetMQ.Sockets;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace E3NextUI.Server
{
    public class PubServer
    {
        Task _serverThread = null;

       
        public static ConcurrentQueue<string> PubCommands = new ConcurrentQueue<string>();
        public static Int32 PubPort = 0;

        public void Start(Int32 port)
        {
            PubPort = port;
            _serverThread = Task.Factory.StartNew(() => { Process(); }, CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);

        }
        private void Process()
        {
            
            using (var pubSocket = new PublisherSocket())
            {
                pubSocket.Options.SendHighWatermark = 50000;

                pubSocket.Bind("tcp://127.0.0.1:" + PubPort.ToString());
                while (E3UI.ShouldProcess)
                {
                    if (PubCommands.Count > 0)
                    {
                        string message;
                        if (PubCommands.TryDequeue(out message))
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
