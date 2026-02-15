using E3Core.Data;
using E3Core.Server;
using E3Core.Utility;
using MonoCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using static E3Core.Processors.GiveMeItem;

namespace E3Core.Processors
{
	public static class Debugging
	{

		private static IMQ MQ = E3.MQ;
		private static ISpawns _spawns = E3.Spawns;
		private static Logging _log = E3.Log;

		[SubSystemInit]
		public static void Debug_Init()
		{
			RegisterEvents();
		}
		public static void RegisterEvents()
		{
			EventProcessor.RegisterCommand("/e3debug_memory_collect", (x) =>
			{

				if (x.args.Count > 0)
				{
					int generation = 0;
					Int32.TryParse(x.args[0], out generation);

					if (generation < 0)
					{
						generation = 0;
					}
					else if (generation > 2)
					{
						generation = 2;
					}
					E3.Bots.Broadcast($"Collecting C# Memory ({generation})");
					GC.Collect(generation, GCCollectionMode.Forced, false);
				}
				else
				{
					GC.GetTotalMemory(true);
					E3.Bots.Broadcast("Collecting C# Memory (All)");
				}


			}, "toggle memory stats window");
			EventProcessor.RegisterCommand("/e3debug_memory_counts", (x) =>
			{

				//events
				E3.Bots.Broadcast($"Events: MQ:{EventProcessor._mqEventProcessingQueue.Count}, MQC:{EventProcessor._mqCommandProcessingQueue.Count}, E:{EventProcessor._eventProcessingQueue.Count}" +
				$", FREX:{EventProcessor._filterRegexes.Count}, EL:{EventProcessor.EventList.Count}, CLQ:{EventProcessor.CommandListQueue.Count}");
				E3.Bots.Broadcast($"PubSub: T:{PubServer._topicMessages.Count}, IM:{PubServer.IncomingChatMessages.Count}, CTS:{PubServer.CommandsToSend.Count}, MQCM:{PubServer.MQChatMessages.Count}");
				E3.Bots.Broadcast($"Router: TLORequest:{RouterServer._tloRequests.Count}, TLOReponse:{RouterServer._tloResposne.Count}");
				E3.Bots.Broadcast($"BegForBuffs: Queued Buffs:{BegForBuffs._queuedBuffs.Count}");
				//NetMQServer.SharedDataClient.TopicUpdates
				E3.Bots.Broadcast($"Shared Data: UTopics:{NetMQServer.SharedDataClient.TopicUpdates.Count}");

				StringBuilder sb = new StringBuilder();

				bool firstAppend = true;
				foreach (var pair in NetMQServer.SharedDataClient.TopicUpdates)
				{

					if (!firstAppend) sb.Append(",");
					if (firstAppend) firstAppend = false;
					sb.Append($"{pair.Key}:{pair.Value.Count}");

				}
				E3.Bots.Broadcast($"Shared Data user topic Count: UTopics:{NetMQServer.SharedDataClient.TopicUpdates.Count}, Users: {sb.ToString()}");



			}, "Output collection sizes");
			EventProcessor.RegisterCommand("/e3debug-config", (x) =>
			{
				NetMQServer.PrintCharConfigLaunch();
			});
			EventProcessor.RegisterCommand("/e3debug_disablewrites", (x) =>
			{

				e3util.ToggleBooleanSetting(ref Setup._disableWrites, "Disable Writes", x.args);


			}, "Disable writes locally");
			EventProcessor.RegisterCommand("/e3debug_buffTimers", (x) =>
			{
				e3util.PrintTimerStatus(BuffCheck._buffTimers, "Buff timers");

			});
			EventProcessor.RegisterCommand("/e3debug_check_spawndelta", x =>
			{

				using (var trace = _log.Trace("deltaspeedtest"))
				{
					unsafe
					{
						int length;
						byte* p;
						using (var tarce2 = _log.Trace("deltaspeedtest_mqcall"))
						{
							p = MQ.GetSpawns3_DeltaPtr(out length);
						}

						ReadOnlySpan<byte> data = new ReadOnlySpan<byte>(p, length);
						//ID,CasterID,Duration,HitCount,SpellType,CounterType,CounterTotal,IsSong
						int dataStartingLength = data.Length;

						while (data.Length > 0)
						{
							Int32 spawnid = MemoryMarshal.Read<Int32>(data);
							data = data.Slice(4);
							bool agressive = MemoryMarshal.Read<bool>(data);
							data = data.Slice(1);
							bool dead = MemoryMarshal.Read<bool>(data);
							data = data.Slice(1);
							float heading = MemoryMarshal.Read<float>(data);
							data = data.Slice(4);
							float height = MemoryMarshal.Read<float>(data);
							data = data.Slice(4);

							Int32 master = MemoryMarshal.Read<Int32>(data);
							data = data.Slice(4);
							bool moving = MemoryMarshal.Read<bool>(data);
							data = data.Slice(1);
							Int64 pctHps = MemoryMarshal.Read<Int32>(data);
							data = data.Slice(8);
							Int32 petid = MemoryMarshal.Read<Int32>(data);
							data = data.Slice(4);
							bool targetable = MemoryMarshal.Read<bool>(data);
							data = data.Slice(1);
							Int32 targetoftargetID = MemoryMarshal.Read<Int32>(data);
							data = data.Slice(4);

							float s_x = MemoryMarshal.Read<float>(data);
							data = data.Slice(4);
							float s_y = MemoryMarshal.Read<float>(data);
							data = data.Slice(4);
							float s_z = MemoryMarshal.Read<float>(data);
							data = data.Slice(4);

							float p_x = MemoryMarshal.Read<float>(data);
							data = data.Slice(4);
							float p_y = MemoryMarshal.Read<float>(data);
							data = data.Slice(4);
							float p_z = MemoryMarshal.Read<float>(data);
							data = data.Slice(4);
							MQ.Write($"ID:{spawnid} a:{agressive} h:{heading} hp:{pctHps} s_x:{s_x} s_y:{s_y} s_z:{s_z} p_x:{p_x} p_y:{p_y} p_z:{p_z} dl:{data.Length}");

						}
					}
				}
			});
			EventProcessor.RegisterCommand("/e3debug_check_xtargets", x =>
			{

				using (var trace = _log.Trace("xtargetspeedtest"))
				{
					unsafe
					{
						int length;
						byte* p;

						p = Core.mq_GetXtargetInfo(out length);
						ReadOnlySpan<byte> data = new ReadOnlySpan<byte>(p, length);
						//ID,CasterID,Duration,HitCount,SpellType,CounterType,CounterTotal,IsSong
						int dataStartingLength = data.Length;

						while (data.Length > 0)
						{
							XTargetTypes targetTypes = (XTargetTypes)MemoryMarshal.Read<Int32>(data);
							data = data.Slice(4);

							Int32 id = MemoryMarshal.Read<Int32>(data);
							data = data.Slice(4);
							Int32 aggroPct = MemoryMarshal.Read<Int32>(data);
							data = data.Slice(4);
							Int32 pctHPs = MemoryMarshal.Read<Int32>(data);
							data = data.Slice(4);

							MQ.Write($"ID:{id} tt:{targetTypes.ToString()} apct:{aggroPct} hp:{pctHPs}");
						}
					}
				}
			});
			EventProcessor.RegisterCommand("/e3debug_check_petbuffs", x =>
			{

				using (var trace = _log.Trace("petbuffsspeedtest"))
				{
					unsafe
					{

						int length;
						byte* p =E3.MQ.GetPetBuffDataPtr(out length);
						Int32 counter = 0;
						if (length > 0)
						{
							
							ReadOnlySpan<byte> data = new ReadOnlySpan<byte>(p, length);
							//ID,CasterID,Duration,HitCount,SpellType,CounterType,CounterTotal,IsSong
							int dataStartingLength = data.Length;
							while(data.Length > 0)
							{
								counter++;
								Int32 spellID = MemoryMarshal.Read<Int32>(data);
								data = data.Slice(4);
								Int32 duration = MemoryMarshal.Read<Int32>(data);
								data = data.Slice(4);
								Int32 spellType = MemoryMarshal.Read<Int32>(data);
								data = data.Slice(4);
								MQ.WriteDelayed($"index:{counter} ID:{spellID} d:{duration} stype:{spellType}");
							}
						}
					}
				}
			});

			EventProcessor.RegisterCommand("/e3debug_check_buffs", x =>
			{

				using (var trace = _log.Trace("buffspeedtest"))
				{
					unsafe
					{
						int length;
						byte* p = Core.mq_GetBuffData(out length);
						ReadOnlySpan<byte> data = new ReadOnlySpan<byte>(p, length);
						//ID,CasterID,Duration,HitCount,SpellType,CounterType,CounterTotal,IsSong
						int dataStartingLength = data.Length;
						for (int i = 0; i < e3util.MaxBuffSlots; i++)
						{
							Int32 spellID = MemoryMarshal.Read<Int32>(data);



							data = data.Slice(4);
							Int32 casterId = MemoryMarshal.Read<Int32>(data);
							data = data.Slice(4);
							Int32 duration = MemoryMarshal.Read<Int32>(data);
							data = data.Slice(4);

							Int32 hitcount = MemoryMarshal.Read<Int32>(data);
							data = data.Slice(4);
							Int32 spellType = MemoryMarshal.Read<Int32>(data);
							data = data.Slice(4);
							Int32 counterType = MemoryMarshal.Read<Int32>(data);
							data = data.Slice(4);
							Int32 counterTotal = MemoryMarshal.Read<Int32>(data);
							data = data.Slice(4);
							bool IsSong = MemoryMarshal.Read<bool>(data);
							data = data.Slice(1);
							MQ.Write($"ID:{spellID} CID:{casterId} D:{duration} hc:{hitcount} st:{spellType} ct:{counterType} ctotal:{counterTotal} song:{IsSong}");



							if (dataStartingLength - data.Length >= length)
							{
								MQ.Write($"End of array at {dataStartingLength - data.Length}");
								break;
							}
						}
						for (int i = 0; i < e3util.MaxSongSlots; i++)
						{
							Int32 spellID = MemoryMarshal.Read<Int32>(data);

							data = data.Slice(4);
							Int32 casterId = MemoryMarshal.Read<Int32>(data);
							data = data.Slice(4);
							Int32 duration = MemoryMarshal.Read<Int32>(data);
							data = data.Slice(4);
							Int32 hitcount = MemoryMarshal.Read<Int32>(data);
							data = data.Slice(4);
							Int32 spellType = MemoryMarshal.Read<Int32>(data);
							data = data.Slice(4);
							Int32 counterType = MemoryMarshal.Read<Int32>(data);
							data = data.Slice(4);
							Int32 counterTotal = MemoryMarshal.Read<Int32>(data);
							data = data.Slice(4);
							bool IsSong = MemoryMarshal.Read<bool>(data);
							data = data.Slice(1);
							MQ.Write($"ID:{spellID} CID:{casterId} D:{duration} hc:{hitcount} st:{spellType} ct:{counterType} ctotal:{counterTotal} song:{IsSong}");



							if (dataStartingLength - data.Length >= length)
							{
								MQ.Write($"End of array at {dataStartingLength - data.Length}");
								break;
							}
						}
					}

					//unsafe
					//{

					//	int length;
					//	byte* p = Core.mq_GetPetBuffData(out length);

					//	if (length == 0)
					//	{
					//		MQ.Write("No Buffs or buffs not found yet");
					//		return;
					//	}
					//	ReadOnlySpan<byte> data = new ReadOnlySpan<byte>(p, length);
					//	//ID,CasterID,Duration,HitCount,SpellType,CounterType,CounterTotal,IsSong
					//	int dataStartingLength = data.Length;
					//	for (int i = 0; i < e3util.MaxTempBuffs; i++)
					//	{
					//		Int32 ID = MemoryMarshal.Read<Int32>(data);
					//		data = data.Slice(4);
					//		Int32 duration = MemoryMarshal.Read<Int32>(data);
					//		data = data.Slice(4);
					//		Int32 spellType = MemoryMarshal.Read<Int32>(data);
					//		data = data.Slice(4);

					//		MQ.Write($"ID:{ID} D:{duration}  st:{spellType}");
					//		if(dataStartingLength-data.Length>=length)
					//		{
					//			MQ.Write($"End of array at {dataStartingLength - data.Length}");
					//			break;
					//		}
					//	}
					//}
				}
			});

			EventProcessor.RegisterCommand("/e3debug_spawns_list", x =>
			{

				foreach (var spawnid in _spawns.GetIDs().OrderBy(y => y).ToList())
				{
					if (_spawns.TryByID(spawnid, out var spawn))
					{
						MQ.Write($"ID:{spawn.ID} TD:{spawn.TypeDesc} Dead:{spawn.Dead} X:{spawn.X} Y:{spawn.Y} Z:{spawn.Z} N:{spawn.Name}");
					}
				}

			});

			EventProcessor.RegisterCommand("/e3debug_spawns_refresh", x =>
			{

				_spawns.RefreshList(full: true);

			});
		}

	}
}
