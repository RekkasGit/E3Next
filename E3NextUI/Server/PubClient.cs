using E3NextUI.Util;
using NetMQ;
using NetMQ.Sockets;
using System;
using System.Collections.Generic;
using System.ServiceModel.Configuration;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Speech.Synthesis;
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
        E3UI _parent;
        OverlayGroupInfo _overlayGroup;
		public void Start(Int32 port,E3UI parent)
        {
            _parent = parent;
            _overlayGroup = parent._overlayGroupInfo;
            _port = port;
            _serverThread = Task.Factory.StartNew(() => { Process(); }, CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
        }
        List<string> _consoleContains = new List<string>(){"You say out of character", "You say, '"," says out of character, '", " tells you, '", " guild, '", " shouts, '", " party, '", " raid,  '", " says, '", " group, '", " auctions, '" };
        List<string> _spellContains = new List<string>() { @"begins to cast a spell.","'s body is "," damage from ", "a critical blast!" };
        List<string> _spellEndWith = new List<string>() { "begins to cast a spell.", "'s enchantments fades.", " was burned.",  "'s casting is interrupted!", "'s spell fizzles!", "non-melee damage." };
        List<string> _spellStartsWith = new List<string>() { "You begin casting ", "Your spell is interrupted." };
        TTSProcessor _ttsprocessor = new TTSProcessor();
		public void Process()
        {

			Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");


			//_synth.SelectVoiceByHints(VoiceGender.Female); //zera voice built into windows

			TimeSpan recieveTimeout = new TimeSpan(0, 0, 0, 0, 5);

            using (var subSocket = new SubscriberSocket())
            {
                subSocket.Options.ReceiveHighWatermark = 50000;
                subSocket.Connect("tcp://127.0.0.1:" + _port);
                subSocket.SubscribeToAnyTopic();
                Console.WriteLine("Subscriber socket connecting...");
                _ttsprocessor.Start();

				while (E3UI.ShouldProcess)
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
                                if (Application.OpenForms.Count > 0 && Application.OpenForms[0] is E3UI)
                                {
                                    ((E3UI)Application.OpenForms[0]).AddConsoleLine(messageReceived, E3UI.MQConsole);
                                }

                            }
                            else if (messageTopicReceived == "OnIncomingChat")
                            {
                                if (Application.OpenForms.Count > 0 && Application.OpenForms[0] is E3UI)
                                {
                                    bool found = false;
                                    foreach (var c in _consoleContains)
                                    {
                                        if (messageReceived.Contains(c))
                                        {
                                            _ttsprocessor.AddMessageNormalQueue(messageReceived);
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
                                                _ttsprocessor.AddMessageToSpellQueue(messageReceived);
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
                                    if (Application.OpenForms.Count > 0 && Application.OpenForms[0] is E3UI)
                                    {
                                        ((E3UI)Application.OpenForms[0]).ToggleShow();
                                    }
                                }
                            }
                            else if (messageTopicReceived == "${Me.CurrentHPs}")
                            {
                                if (Application.OpenForms.Count > 0 && Application.OpenForms[0] is E3UI)
                                {
                                    ((E3UI)Application.OpenForms[0]).SetPlayerHP(messageReceived);
                                }
                            }
                            else if (messageTopicReceived == "${Me.CurrentMana}")
                            {
                                if (Application.OpenForms.Count > 0 && Application.OpenForms[0] is E3UI)
                                {
                                    ((E3UI)Application.OpenForms[0]).SetPlayerMP(messageReceived);
                                }
                            }
                            else if (messageTopicReceived == "${Me.CurrentEndurance}")
                            {
                                if (Application.OpenForms.Count > 0 && Application.OpenForms[0] is E3UI)
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
                                if (Application.OpenForms.Count > 0 && Application.OpenForms[0] is E3UI)
                                {
                                    ((E3UI)Application.OpenForms[0]).SetPlayerCasting(messageReceived);
                                }

                            }
                            else if(messageTopicReceived== "${EQ.CurrentFocusedWindowName}")
                            {

								if (Application.OpenForms.Count > 0 && Application.OpenForms[0] is E3UI)
								{
								    ((E3UI)Application.OpenForms[0]).SetCurrentWindow(messageReceived);
								}
							}
                            else if (messageTopicReceived == "GuildChatForDiscord")
                            {
                                var messageParts = messageReceived.Split('|');
                                if (messageParts.Length == 2)
                                {
                                    var message = $"**{messageParts[0]} Guild**: {messageParts[1]}";
                                    ApiLibrary.ApiLibrary.SendMessageToDiscord(message);
                                }
                            }
                            else if (messageTopicReceived == "WorldShutdown")
                            {

                            }
							else if (messageTopicReceived.StartsWith("${E3Bot"))
							{
                                if(messageTopicReceived.StartsWith("${E3Bot1."))
                                {
                                    if(messageTopicReceived=="${E3Bot1.Name}")
                                    {
										_overlayGroup.SetOverlayLabelData(_overlayGroup.label_name1, messageReceived);
									}
                                    else if (messageTopicReceived == "${E3Bot1.AAPoints}")
									{
										_overlayGroup.SetOverlayLabelData(_overlayGroup.label_name1_aatotal, messageReceived);
									}
									else if (messageTopicReceived == "${E3Bot1.Target}")
									{
                                  	_overlayGroup.SetOverlayLabelData(_overlayGroup.label_target1_info, messageReceived);
									}
									else if (messageTopicReceived == "${E3Bot1.Casting}")
									{
										_overlayGroup.SetOverlayLabelData(_overlayGroup.label_casting1_info, messageReceived);
									}
									else if (messageTopicReceived == "${E3Bot1.DPSUpdate}")
                                    {
                                        if (messageReceived == String.Empty)
                                        {
                                            continue;   
                                        }
										//$"{PlayerName},{totalDPS},{yourDPS},{petDPS},{dsDPS},{totalTime}"
                                        Int32 indexOfFirstComma = messageReceived.IndexOf(',');
                                        Int32 indexOf2nComma = messageReceived.IndexOf(',', indexOfFirstComma + 1);
										Int32 indexOf3rdComma = messageReceived.IndexOf(',', indexOf2nComma + 1);

										string dpsValue = messageReceived.Substring(indexOfFirstComma + 1, indexOf2nComma - indexOfFirstComma - 1);
										string dmgValue = messageReceived.Substring(indexOf2nComma + 1, indexOf3rdComma - indexOf2nComma - 1);
										if (Int32.TryParse(dpsValue,out var dpsint))
										{
											_overlayGroup.SetOverlayLabelData(_overlayGroup.label_dps1_total, dpsint.ToString("N0"));

										}
										if (Int32.TryParse(dmgValue, out var dmgint))
										{
											_overlayGroup.SetOverlayLabelData(_overlayGroup.label_dmg1_total, dmgint.ToString("N0"));

										}
									}
								}
								else if (messageTopicReceived.StartsWith("${E3Bot2."))
								{
									if (messageTopicReceived == "${E3Bot2.Name}")
									{
										_overlayGroup.SetOverlayLabelData(_overlayGroup.label_name2, messageReceived);
									}
									else if (messageTopicReceived == "${E3Bot2.AAPoints}")
									{
										_overlayGroup.SetOverlayLabelData(_overlayGroup.label_name2_aatotal, messageReceived);
									}
									else if (messageTopicReceived == "${E3Bot2.Target}")
									{
										_overlayGroup.SetOverlayLabelData(_overlayGroup.label_target2_info, messageReceived);
									}
									else if (messageTopicReceived == "${E3Bot2.Casting}")
									{
										_overlayGroup.SetOverlayLabelData(_overlayGroup.label_casting2_info, messageReceived);
									}
									else if (messageTopicReceived == "${E3Bot2.DPSUpdate}")
									{
										if (messageReceived == String.Empty)
										{
											continue;
										}
										//$"{PlayerName},{totalDPS},{yourDPS},{petDPS},{dsDPS},{totalTime}"
										Int32 indexOfFirstComma = messageReceived.IndexOf(',');
										Int32 indexOf2nComma = messageReceived.IndexOf(',', indexOfFirstComma + 1);
										Int32 indexOf3rdComma = messageReceived.IndexOf(',', indexOf2nComma + 1);

										string dpsValue = messageReceived.Substring(indexOfFirstComma + 1, indexOf2nComma - indexOfFirstComma - 1);
										string dmgValue = messageReceived.Substring(indexOf2nComma + 1, indexOf3rdComma - indexOf2nComma - 1);
										if (Int32.TryParse(dpsValue, out var dpsint))
										{
											_overlayGroup.SetOverlayLabelData(_overlayGroup.label_dps2_total, dpsint.ToString("N0"));

										}
										if (Int32.TryParse(dmgValue, out var dmgint))
										{
											_overlayGroup.SetOverlayLabelData(_overlayGroup.label_dmg2_total, dmgint.ToString("N0"));

										}
									}

								}
								else if (messageTopicReceived.StartsWith("${E3Bot3."))
								{
									if (messageTopicReceived == "${E3Bot3.Name}")
									{
										_overlayGroup.SetOverlayLabelData(_overlayGroup.label_name3, messageReceived);
									}
									else if (messageTopicReceived == "${E3Bot3.AAPoints}")
									{
										_overlayGroup.SetOverlayLabelData(_overlayGroup.label_name3_aatotal, messageReceived);
									}
									else if (messageTopicReceived == "${E3Bot3.Target}")
									{
										_overlayGroup.SetOverlayLabelData(_overlayGroup.label_target3_info, messageReceived);
									}
									else if (messageTopicReceived == "${E3Bot3.Casting}")
									{
										_overlayGroup.SetOverlayLabelData(_overlayGroup.label_casting3_info, messageReceived);
									}
									else if (messageTopicReceived == "${E3Bot3.DPSUpdate}")
									{
										if (messageReceived == String.Empty)
										{
											continue;
										}
										//$"{PlayerName},{totalDPS},{yourDPS},{petDPS},{dsDPS},{totalTime}"
										Int32 indexOfFirstComma = messageReceived.IndexOf(',');
										Int32 indexOf2nComma = messageReceived.IndexOf(',', indexOfFirstComma + 1);
										Int32 indexOf3rdComma = messageReceived.IndexOf(',', indexOf2nComma + 1);

										string dpsValue = messageReceived.Substring(indexOfFirstComma + 1, indexOf2nComma - indexOfFirstComma - 1);
										string dmgValue = messageReceived.Substring(indexOf2nComma + 1, indexOf3rdComma - indexOf2nComma - 1);
										if (Int32.TryParse(dpsValue, out var dpsint))
										{
											_overlayGroup.SetOverlayLabelData(_overlayGroup.label_dps3_total, dpsint.ToString("N0"));

										}
										if (Int32.TryParse(dmgValue, out var dmgint))
										{
											_overlayGroup.SetOverlayLabelData(_overlayGroup.label_dmg3_total, dmgint.ToString("N0"));

										}
									}
								}
								else if (messageTopicReceived.StartsWith("${E3Bot4."))
								{
									if (messageTopicReceived == "${E3Bot4.Name}")
									{
										_overlayGroup.SetOverlayLabelData(_overlayGroup.label_name4, messageReceived);
									}
									else if (messageTopicReceived == "${E3Bot4.AAPoints}")
									{
										_overlayGroup.SetOverlayLabelData(_overlayGroup.label_name4_aatotal, messageReceived);
									}
									else if (messageTopicReceived == "${E3Bot4.Target}")
									{
										_overlayGroup.SetOverlayLabelData(_overlayGroup.label_target4_info, messageReceived);
									}
									else if (messageTopicReceived == "${E3Bot4.Casting}")
									{
										_overlayGroup.SetOverlayLabelData(_overlayGroup.label_casting4_info, messageReceived);
									}
									else if (messageTopicReceived == "${E3Bot4.DPSUpdate}")
									{
										if (messageReceived == String.Empty)
										{
											continue;
										}
										//$"{PlayerName},{totalDPS},{yourDPS},{petDPS},{dsDPS},{totalTime}"
										Int32 indexOfFirstComma = messageReceived.IndexOf(',');
										Int32 indexOf2nComma = messageReceived.IndexOf(',', indexOfFirstComma + 1);
										Int32 indexOf3rdComma = messageReceived.IndexOf(',', indexOf2nComma + 1);

										string dpsValue = messageReceived.Substring(indexOfFirstComma + 1, indexOf2nComma - indexOfFirstComma - 1);
										string dmgValue = messageReceived.Substring(indexOf2nComma + 1, indexOf3rdComma - indexOf2nComma - 1);
										if (Int32.TryParse(dpsValue, out var dpsint))
										{
											_overlayGroup.SetOverlayLabelData(_overlayGroup.label_dps4_total, dpsint.ToString("N0"));

										}
										if (Int32.TryParse(dmgValue, out var dmgint))
										{
											_overlayGroup.SetOverlayLabelData(_overlayGroup.label_dmg4_total, dmgint.ToString("N0"));

										}
									}
								}
								else if (messageTopicReceived.StartsWith("${E3Bot5."))
								{
									if (messageTopicReceived == "${E3Bot5.Name}")
									{
										_overlayGroup.SetOverlayLabelData(_overlayGroup.label_name5, messageReceived);
									}
									else if (messageTopicReceived == "${E3Bot5.AAPoints}")
									{
										_overlayGroup.SetOverlayLabelData(_overlayGroup.label_name5_aatotal, messageReceived);
									}
									else if (messageTopicReceived == "${E3Bot5.Target}")
									{
										_overlayGroup.SetOverlayLabelData(_overlayGroup.label_target5_info, messageReceived);
									}
									else if (messageTopicReceived == "${E3Bot5.Casting}")
									{
										_overlayGroup.SetOverlayLabelData(_overlayGroup.label_casting5_info, messageReceived);
									}
									else if (messageTopicReceived == "${E3Bot5.DPSUpdate}")
									{
										if (messageReceived == String.Empty)
										{
											continue;
										}
										//$"{PlayerName},{totalDPS},{yourDPS},{petDPS},{dsDPS},{totalTime}"
										Int32 indexOfFirstComma = messageReceived.IndexOf(',');
										Int32 indexOf2nComma = messageReceived.IndexOf(',', indexOfFirstComma + 1);
										Int32 indexOf3rdComma = messageReceived.IndexOf(',', indexOf2nComma + 1);

										string dpsValue = messageReceived.Substring(indexOfFirstComma + 1, indexOf2nComma - indexOfFirstComma - 1);
										string dmgValue = messageReceived.Substring(indexOf2nComma + 1, indexOf3rdComma - indexOf2nComma - 1);
										if (Int32.TryParse(dpsValue, out var dpsint))
										{
											_overlayGroup.SetOverlayLabelData(_overlayGroup.label_dps5_total, dpsint.ToString("N0"));

										}
										if (Int32.TryParse(dmgValue, out var dmgint))
										{
											_overlayGroup.SetOverlayLabelData(_overlayGroup.label_dmg5_total, dmgint.ToString("N0"));

										}
									}
								}
								else if (messageTopicReceived.StartsWith("${E3Bot6."))
								{
									if (messageTopicReceived == "${E3Bot6.Name}")
									{
										_overlayGroup.SetOverlayLabelData(_overlayGroup.label_name6, messageReceived);
									}
									else if (messageTopicReceived == "${E3Bot6.AAPoints}")
									{
										_overlayGroup.SetOverlayLabelData(_overlayGroup.label_name6_aatotal, messageReceived);
									}
									else if (messageTopicReceived == "${E3Bot6.Target}")
									{
										_overlayGroup.SetOverlayLabelData(_overlayGroup.label_target6_info, messageReceived);
									}
									else if (messageTopicReceived == "${E3Bot6.Casting}")
									{
										_overlayGroup.SetOverlayLabelData(_overlayGroup.label_casting6_info, messageReceived);
									}
									else if (messageTopicReceived == "${E3Bot6.DPSUpdate}")
									{
										if (messageReceived == String.Empty)
										{
											continue;
										}
										//$"{PlayerName},{totalDPS},{yourDPS},{petDPS},{dsDPS},{totalTime}"
										Int32 indexOfFirstComma = messageReceived.IndexOf(',');
										Int32 indexOf2nComma = messageReceived.IndexOf(',', indexOfFirstComma + 1);
										Int32 indexOf3rdComma = messageReceived.IndexOf(',', indexOf2nComma + 1);

										string dpsValue = messageReceived.Substring(indexOfFirstComma + 1, indexOf2nComma - indexOfFirstComma - 1);
										string dmgValue = messageReceived.Substring(indexOf2nComma + 1, indexOf3rdComma - indexOf2nComma - 1);
										if (Int32.TryParse(dpsValue, out var dpsint))
										{
											_overlayGroup.SetOverlayLabelData(_overlayGroup.label_dps6_total, dpsint.ToString("N0"));

										}
										if (Int32.TryParse(dmgValue, out var dmgint))
										{
											_overlayGroup.SetOverlayLabelData(_overlayGroup.label_dmg6_total, dmgint.ToString("N0"));

										}
									}
								}

							}
						}
                        catch (Exception ex)
                        {
                            if (Application.OpenForms.Count > 0 && Application.OpenForms[0] is E3UI)
                            {
                                ((E3UI)Application.OpenForms[0]).AddConsoleLine(ex.Message, E3UI.Console);
                            }
                        }
                    }
                }
            }
        }
        private static void WriteMessageToConsole(string message, ConsoleColor consoleColor)
        {
            Console.ForegroundColor = consoleColor;
            Console.WriteLine($"{DateTime.Now}: {message}");
        }
    }
}
