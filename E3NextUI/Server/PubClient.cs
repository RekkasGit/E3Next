using NetMQ;
using NetMQ.Sockets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace E3NextUI.Server
{
    public class PubClient
    {

        Task _serverThread;
        private Int32 _port;
        public void Start(Int32 port)
        {
            _port = port;
            _serverThread = Task.Factory.StartNew(() => { Process(); }, CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
        }
        public void Process()
        {

            using (var subSocket = new SubscriberSocket())
            {
                subSocket.Options.ReceiveHighWatermark = 1000;
                subSocket.Connect("tcp://localhost:" + _port);
                subSocket.Subscribe("OnIncomingChat");
                subSocket.Subscribe("OnWriteChatColor");
                subSocket.Subscribe("OnCommand");
                subSocket.Subscribe("HPValue");
                Console.WriteLine("Subscriber socket connecting...");
                while (true)
                {
                    string messageTopicReceived = subSocket.ReceiveFrameString();
                    string messageReceived = subSocket.ReceiveFrameString();
                    Console.WriteLine(messageReceived);
                    if (messageTopicReceived == "OnWriteChatColor")
                    {
                        if (Application.OpenForms.Count > 0)
                        {
                            ((E3UI)Application.OpenForms[0]).AddMQConsoleLine(messageReceived);
                        }

                    }
                    else if (messageTopicReceived == "OnIncomingChat")
                    {
                        if (Application.OpenForms.Count > 0)
                        {
                            ((E3UI)Application.OpenForms[0]).AddConsoleLine(messageReceived);
                        }
                    }
                    else if (messageTopicReceived == "OnCommand")
                    {
                        //E3UI._consoleLines.Enqueue(messageReceived);
                    }
                    else if (messageTopicReceived == "HPValue")
                    {
                        if(Application.OpenForms.Count>0)
                        {
                            ((E3UI)Application.OpenForms[0]).SetPlayerHP(messageReceived);
                        }
                       
                    }
                }
            }
        }
    }
}
