using E3NextUI.Util;
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
        List<string> _consoleContains = new List<string>(){ "You say, '"," says out of character, '", " tells you, '", " guild, '", " party, '", " raid, '", " group, '", " auctions, '" };
        List<string> _spellContains = new List<string>() { "'s body is "," damage from ", "a critical blast!" };
        List<string> _spellEndWith = new List<string>() { "begins to cast a spell.", "'s enchantments fades.", " was burned.",  "'s casting is interrupted!", "'s spell fizzles!", "non-melee damage." };
        List<string> _spellStartsWith = new List<string>() { "You begin casting ", "Your spell is interrupted." };

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
                            ((E3UI)Application.OpenForms[0]).AddConsoleLine(messageReceived, E3UI._mqConsole);
                        }

                    }
                    else if (messageTopicReceived == "OnIncomingChat")
                    {
                        if (Application.OpenForms.Count > 0)
                        {
                            bool found = false;
                            foreach(var c in _consoleContains)
                            {
                                if(messageReceived.Contains(c))
                                {
                                    ((E3UI)Application.OpenForms[0]).AddConsoleLine(messageReceived, E3UI._console);
                                    found = true;
                                    break;
                                }
                            }

                            if(!found) LineParser.ParseLine(messageReceived);

                            if (!found)
                            {
                                foreach (var c in _spellContains)
                                {
                                    if (messageReceived.Contains(c))
                                    {
                                        ((E3UI)Application.OpenForms[0]).AddConsoleLine(messageReceived, E3UI._spellConsole);
                                        found = true;
                                        break;
                                    }
                                }
                            }
                            if(!found)
                            {
                                foreach (var c in _spellStartsWith)
                                {
                                    if (messageReceived.StartsWith(c))
                                    {
                                        ((E3UI)Application.OpenForms[0]).AddConsoleLine(messageReceived, E3UI._spellConsole);
                                        found = true;
                                        break;
                                    }
                                }
                            }
                            if(!found)
                            {
                                foreach (var c in _spellEndWith)
                                {
                                    if (messageReceived.EndsWith(c))
                                    {
                                        ((E3UI)Application.OpenForms[0]).AddConsoleLine(messageReceived, E3UI._spellConsole);
                                        found = true;
                                        break;
                                    }
                                }
                            }
                           
                            if (!found)
                            {
                                //misc bucket
                                ((E3UI)Application.OpenForms[0]).AddConsoleLine(messageReceived, E3UI._meleeConsole);
                            }
                          
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
