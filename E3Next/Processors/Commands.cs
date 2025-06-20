using E3Core.Server;
using MonoCore;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace E3Core.Processors
{
	
	public class CommandSet
	{
		public string Name = String.Empty;
		public List<string> Commands = new List<string>();
	}
	public static class Commands
	{
		private static IMQ MQ = E3.MQ;
		private static ConcurrentQueue<string> _currentCommands = new ConcurrentQueue<string>();


		[SubSystemInit]
		public static void Commands_Init()
		{
			RegisterEvents();
		}
		
		private static void RegisterEvents()
		{
			EventProcessor.RegisterCommand("/e3commandset", (x) => {

				if (x.args.Count > 0)
				{
					string commandKeyToUse = x.args[0].Trim();
					if (E3.CharacterSettings.CommandCollection.TryGetValue(commandKeyToUse, out var commandList))
					{

						foreach (var command in commandList.Commands)
						{
							string commandName = command.Split(' ')[0].ToLower();
							if (commandName.Equals("/mono", StringComparison.OrdinalIgnoreCase) || commandName.Equals("/shutdown", StringComparison.OrdinalIgnoreCase))
							{
								//in case this is a restart command, we need to delay the command so it happens outside of the OnPulse. just assume all /mono commands are 
								//delayed
								if (Core._MQ2MonoVersion >= 0.22m)
								{
									//needs to be delayed, so that the restarts happes outside of the E3N OnPulse
									MQ.Cmd(command, true);
									return;//going to restart anyway, just kick out
								}
								else
								{
									E3.Bots.Broadcast("Sorry cannot execute /mono commands via broadcast unless your MQ2Mono version is 0.22 or above.");
								}
							}
							else
							{
								if(commandName.Equals("/delay", StringComparison.OrdinalIgnoreCase))
								{
									MQ.Write("Delay command found");
									string delayValue = command.ToLower().Replace("/delay", "").Trim();
									if(Int32.TryParse(delayValue, out var delay))
									{
										MQ.Write($"Issuing delay of {delay}");
										MQ.Delay(delay);
									}
								}
								else
								{
									bool internalComand = false;
									foreach (var pair in EventProcessor.CommandList)
									{
										if (commandName.Equals(pair.Key, StringComparison.OrdinalIgnoreCase))
										{
											internalComand = true;

											MQ.Cmd(command);
											//EventProcessor.ProcessMQCommand(command);
											//moving data between threads, have to wait till its actually there
											//while(!EventProcessor.ProcessEventsIntoQueue_MQCommandProcessing())
											//{
											//	System.Threading.Thread.Sleep(0);
											//}
											//EventProcessor.ProcessEventsInQueues(pair.Key);//process just that command
											//if(commandName.StartsWith("/e3bc"))
											//{
											//	MQ.Write("e3bc command, delaying then processing it");
											//	//need to do a slight pause in case the command is also including us 
											//	MQ.Delay(10);
											//	NetMQServer.SharedDataClient.ProcessCommands(); //recieving data
											//}
										}
									}
									if (!internalComand)
									{
										MQ.Cmd(command);

									}
								}
							}
						}
					}
					else
					{

						E3.Bots.Broadcast($"Command set [{commandKeyToUse}] not found!");
					
					}
				}
			});



		}

	}
}
