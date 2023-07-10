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
        List<string> _consoleContains = new List<string>(){"You say out of character", "You say, '"," says out of character, '", " tells you, '", " guild, '", " party, '", " raid, '", " group, '", " auctions, '" };
        List<string> _spellContains = new List<string>() { "begins to cast a spell\\.","'s body is "," damage from ", "a critical blast!" };
        List<string> _spellEndWith = new List<string>() { "begins to cast a spell.", "'s enchantments fades.", " was burned.",  "'s casting is interrupted!", "'s spell fizzles!", "non-melee damage." };
        List<string> _spellStartsWith = new List<string>() { "You begin casting ", "Your spell is interrupted." };

        public void Process()
        {
            TimeSpan recieveTimeout = new TimeSpan(0, 0, 0, 0, 5);

            using (var subSocket = new SubscriberSocket())
            {
                subSocket.Options.ReceiveHighWatermark = 50000;
                subSocket.Connect("tcp://127.0.0.1:" + _port);
                subSocket.SubscribeToAnyTopic();
                Console.WriteLine("Subscriber socket connecting...");
                while (true)
                {
                    string messageTopicReceived;
                        
                    if(subSocket.TryReceiveFrameString(recieveTimeout,out messageTopicReceived))
                    {
                        string messageReceived = subSocket.ReceiveFrameString();
                        try
                        {
                            //Console.WriteLine(messageReceived);
                            if (messageTopicReceived == "OnWriteChatColor")
                            {
                                if (Application.OpenForms.Count > 0)
                                {
                                    ((E3UI)Application.OpenForms[0]).AddConsoleLine(messageReceived, E3UI.MQConsole);
                                }

                            }
                            else if (messageTopicReceived == "OnIncomingChat")
                            {
                                if (Application.OpenForms.Count > 0)
                                {
                                    bool found = false;
                                    foreach (var c in _consoleContains)
                                    {
                                        if (messageReceived.Contains(c))
                                        {
                                            ((E3UI)Application.OpenForms[0]).AddConsoleLine(messageReceived, E3UI.Console);
                                            found = true;
                                            break;
                                        }
                                    }

                                    if (!found) LineParser.ParseLine(messageReceived);

                                    if (!found)
                                    {
                                        foreach (var c in _spellContains)
                                        {
                                            if (messageReceived.Contains(c))
                                            {
                                                ((E3UI)Application.OpenForms[0]).AddConsoleLine(messageReceived, E3UI.SpellConsole);
                                                found = true;
                                                break;
                                            }
                                        }
                                    }
                                    if (!found)
                                    {
                                        foreach (var c in _spellStartsWith)
                                        {
                                            if (messageReceived.StartsWith(c))
                                            {
                                                ((E3UI)Application.OpenForms[0]).AddConsoleLine(messageReceived, E3UI.SpellConsole);
                                                found = true;
                                                break;
                                            }
                                        }
                                    }
                                    if (!found)
                                    {
                                        foreach (var c in _spellEndWith)
                                        {
                                            if (messageReceived.EndsWith(c))
                                            {
                                                ((E3UI)Application.OpenForms[0]).AddConsoleLine(messageReceived, E3UI.SpellConsole);
                                                found = true;
                                                break;
                                            }
                                        }
                                    }
                                    if (!found)
                                    {
                                        //misc bucket
                                        ((E3UI)Application.OpenForms[0]).AddConsoleLine(messageReceived, E3UI.MeleeConsole);
                                    }
                                }
                            }
                            else if (messageTopicReceived == "OnCommand")
                            {
                                if (messageReceived == "#toggleshow")
                                {
                                    if (Application.OpenForms.Count > 0)
                                    {
                                        ((E3UI)Application.OpenForms[0]).ToggleShow();
                                    }
                                }
                            }
                            else if (messageTopicReceived == "${Me.CurrentHPs}")
                            {
                                if (Application.OpenForms.Count > 0)
                                {
                                    ((E3UI)Application.OpenForms[0]).SetPlayerHP(messageReceived);
                                }
                            }
                            else if (messageTopicReceived == "${Me.CurrentMana}")
                            {
                                if (Application.OpenForms.Count > 0)
                                {
                                    ((E3UI)Application.OpenForms[0]).SetPlayerMP(messageReceived);
                                }
                            }
                            else if (messageTopicReceived == "${Me.CurrentEndurance}")
                            {
                                if (Application.OpenForms.Count > 0)
                                {
                                    ((E3UI)Application.OpenForms[0]).SetPlayerSP(messageReceived);
                                }
                            }
                            else if (messageTopicReceived == "${Me.Pet.CleanName}")
                            {
                                LineParser.SetPetName(messageReceived);

                            }
                            else if (messageTopicReceived == "${InCombat}")
                            {
                                if(Boolean.TryParse(messageReceived, out var inCombat))
                                {
                                    LineParser.SetCombatState(inCombat);
                                }

                            }
                            else if (messageTopicReceived == "${Casting}")
                            {
                                if (Application.OpenForms.Count > 0)
                                {
                                    ((E3UI)Application.OpenForms[0]).SetPlayerCasting(messageReceived);
                                }

                            }
                            else if(messageTopicReceived== "${EQ.CurrentFocusedWindowName}")
                            {

								if (Application.OpenForms.Count > 0)
								{
								    ((E3UI)Application.OpenForms[0]).SetCurrentWindow(messageReceived);
								}
							}

						}
                        catch (Exception ex)
                        {
                            ((E3UI)Application.OpenForms[0]).AddConsoleLine(ex.Message, E3UI.Console);

                        }
                        

                    }

                }
            }
        }
    }
}
