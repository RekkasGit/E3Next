using MonoCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;


namespace Template.Debugging
{
	public enum XTargetTypes
	{
		XTARGET_EMPTY_TARGET,
		XTARGET_AUTO_HATER,
		XTARGET_SPECIFIC_PC,
		XTARGET_SPECIFIC_NPC,
		XTARGET_TARGETS_TARGET,
		XTARGET_GROUP_TANK,
		XTARGET_GROUP_TANKS_TARGET,
		XTARGET_GROUP_ASSIST,
		XTARGET_GROUP_ASSIST_TARGET,
		XTARGET_GROUP_PULLER,
		XTARGET_GROUP_PULLER_TARGET,
		XTARGET_GROUP_MARK1,
		XTARGET_GROUP_MARK2,
		XTARGET_GROUP_MARK3,
		XTARGET_RAID_ASSIST1,
		XTARGET_RAID_ASSIST2,
		XTARGET_RAID_ASSIST3,
		XTARGET_RAID_ASSIST1_TARGET,
		XTARGET_RAID_ASSIST2_TARGET,
		XTARGET_RAID_ASSIST3_TARGET,
		XTARGET_RAID_MARK1,
		XTARGET_RAID_MARK2,
		XTARGET_RAID_MARK3,
		XTARGET_MY_PET,
		XTARGET_MY_PET_TARGET,
		XTARGET_MY_MERCENARY,
		XTARGET_MY_MERCENARY_TARGET,

	}
	public static class Debugging
	{

		private static IMQ MQ = Core.mqInstance;

		private static Logging _log = Core.logInstance;
		private static ISpawns _spawns = Core.spawnInstance;

		public static void Debug_Init()
		{
			RegisterEvents();
		}
		public static void RegisterEvents()
		{
		
			EventProcessor.RegisterCommand("/debug_memory_collect", (x) =>
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
					MQ.Write($"Collecting C# Memory ({generation})");
					GC.Collect(generation, GCCollectionMode.Forced, false);
				}
				else
				{
					GC.GetTotalMemory(true);
					MQ.Write("Collecting C# Memory (All)");
				}


			}, "toggle memory stats window");
		
			EventProcessor.RegisterCommand("/debug_check_spawndelta", x =>
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
			EventProcessor.RegisterCommand("/debug_check_xtargets", x =>
			{

				using (var trace = _log.Trace("xtargetspeedtest"))
				{
					unsafe
					{
						int length;
						byte* p;

						p =MQ.GetXtargetDataPtr(out length);
						ReadOnlySpan<byte> data = new ReadOnlySpan<byte>(p, length);
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


			EventProcessor.RegisterCommand("/debug_check_petbuffs", x =>
			{

				using (var trace = _log.Trace("petbuffsspeedtest"))
				{
					unsafe
					{

						int length;
						byte* p =MQ.GetPetBuffDataPtr(out length);
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
			EventProcessor.RegisterCommand("/debug_list_aa", x =>
			{

				using (var trace = _log.Trace("listaa_speedtest"))
				{
					unsafe
					{
						List<Int32> aaList = new List<int>();

						int length;
						byte* p = MQ.GetAAIdsDataPtr(out length);
						Int32 counter = 0;
						if (length > 0)
						{
							ReadOnlySpan<byte> data = new ReadOnlySpan<byte>(p, length);
							//ID,CasterID,Duration,HitCount,SpellType,CounterType,CounterTotal,IsSong
							int dataStartingLength = data.Length;
							while (data.Length > 0)
							{
								counter++;
								Int32 AAID = MemoryMarshal.Read<Int32>(data);
								data = data.Slice(4);
								aaList.Add(AAID);
						
							}
							MQ.WriteDelayed($"AAs:{String.Join(",",aaList)}");
							MQ.WriteDelayed($"AA List Count:{counter}");
						}
					}
				}
			});
			EventProcessor.RegisterCommand("/debug_list_disc", x =>
			{

				using (var trace = _log.Trace("listdisc_speedtest"))
				{
					unsafe
					{
						List<Int32> discList = new List<int>();

						int length;
						byte* p = MQ.GetDiscIdsDataPtr(out length);
						Int32 counter = 0;
						if (length > 0)
						{
							ReadOnlySpan<byte> data = new ReadOnlySpan<byte>(p, length);
							int dataStartingLength = data.Length;
							while (data.Length > 0)
							{
								counter++;
								Int32 discID = MemoryMarshal.Read<Int32>(data);
								data = data.Slice(4);
								discList.Add(discID);

							}
							MQ.WriteDelayed($"Discs:{String.Join(",", discList)}");
							MQ.WriteDelayed($"Discs List Count:{counter}");
						}
					}
				}
			});
			EventProcessor.RegisterCommand("/debug_check_targetbuffs", x =>
			{

				using (var trace = _log.Trace("targetbuffsspeedtest"))
				{
					unsafe
					{

						Int32 spawnid = MQ.Query<Int32>("${Target.ID}");

						if(spawnid<1)
						{
							MQ.Write("No valid spawnid");
							return;
						}

						int length;
						byte* p = MQ.GetTargetBuffDataPtr(spawnid,out length);
						Int32 counter = 0;
						if (length > 0)
						{

							ReadOnlySpan<byte> data = new ReadOnlySpan<byte>(p, length);
							//ID,CasterID,Duration,HitCount,SpellType,CounterType,CounterTotal,IsSong
							int dataStartingLength = data.Length;
							bool buffsPopulated = false;
							if (data.Length > 0)
							{
								//lets pull out the if buffs are being populated
								buffsPopulated = MemoryMarshal.Read<Boolean>(data);
								data = data.Slice(1);
							}
							
							while (data.Length > 0)
							{
								counter++;
								Int32 spellID = MemoryMarshal.Read<Int32>(data);
								data = data.Slice(4);
								Int32 duration = MemoryMarshal.Read<Int32>(data);
								data = data.Slice(4);
								Int32 spellType = MemoryMarshal.Read<Int32>(data);
								data = data.Slice(4);
								Int32 casterNameLength = MemoryMarshal.Read<Int32>(data);
								data = data.Slice(4);

								string CasterName = string.Empty;

								if(casterNameLength>0)
								{
									unsafe
									{
										fixed (byte* ptostr = data)
										{
											CasterName = Encoding.ASCII.GetString(ptostr, casterNameLength);
										}
									}
									data = data.Slice(casterNameLength);
								}
								MQ.WriteDelayed($"index:{counter} ID:{spellID} d:{duration} stype:{spellType} casterName:{CasterName}");
							}
						}
					}
				}
			});

			EventProcessor.RegisterCommand("/debug_check_buffs", x =>
			{
				Int32 MaxBuffSlots = MQ.Query<Int32>("${Me.MaxBuffSlots}");
				Int32 MaxSongSlots = MQ.Query<Int32>("${Me.MaxSongSlots}");

				using (var trace = _log.Trace("buffspeedtest"))
				{
					unsafe
					{
						int length;
						byte* p = Core.mq_GetBuffData(out length);
						ReadOnlySpan<byte> data = new ReadOnlySpan<byte>(p, length);
						//ID,CasterID,Duration,HitCount,SpellType,CounterType,CounterTotal,IsSong
						int dataStartingLength = data.Length;
						
						for (int i = 0; i < MaxBuffSlots; i++)
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
						for (int i = 0; i < MaxSongSlots; i++)
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

			
		
			EventProcessor.RegisterCommand("/debug_spawns_list", x =>
			{
				_spawns.RefreshList(true);
				foreach (var spawnid in _spawns.GetIDs().OrderBy(y => y).ToList())
				{
					if (_spawns.TryByID(spawnid, out var spawn))
					{
						MQ.Write($"ID:{spawn.ID} RID:{spawn.RaceID}  RDES: {spawn.RaceName} TD:{spawn.TypeDesc} Dead:{spawn.Dead} X:{spawn.X} Y:{spawn.Y} Z:{spawn.Z} N:{spawn.Name} CN:{spawn.CleanName}");
					}
				}

			});
			EventProcessor.RegisterCommand("/debug_spawns_list_names", x =>
			{
				_spawns.RefreshList(true);
				foreach (var pair in Spawns._spawnsByName)
				{
					var spawn = pair.Value;
					MQ.Write($"Key: {pair.Key} ID:{spawn.ID} TD:{spawn.TypeDesc} Dead:{spawn.Dead} X:{spawn.X} Y:{spawn.Y} Z:{spawn.Z} N:{spawn.Name} CN: {spawn.CleanName}");
				}
			});
		}

	}
}
