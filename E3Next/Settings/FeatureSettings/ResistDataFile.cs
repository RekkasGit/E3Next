using Dapper;
using E3Core.Data;
using E3Core.Processors;
using MonoCore;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml.Linq;
namespace E3Core.Settings.FeatureSettings
{
	public  class ResistDataFile : BaseSettings 
	{
		public SQLiteConnection _sqlite;
		public Int32 Current_ZoneID = 0;
	
		public class ResistData
		{
			public string NPCName = String.Empty;
			public bool MagicResistant = false;
			public bool MagicImmune = false;
			public bool FireResistant = false;
			public bool FireImmune = false;
			public bool ColdResistant = false;
			public bool ColdImmune = false;
			public bool DiseaseResistant = false;
			public bool DiseaseImmune = false;
			public bool PoisonResistant = false;
			public bool PoisonImmune = false;
			public bool CorruptResistant = false;
			public bool CorruptImmune = false;

		}
		public Dictionary<string, ResistData> ZoneData = new Dictionary<string, ResistData>();
		string fileName;

	

		public ResistDataFile()
		{
			RegisterEvents();
			fileName = GetSettingsFilePath($"E3Resist_{E3.ServerName}.db");
		}
		public bool ShouldSkip(Spell spell, Spawn s)
		{
			if (E3.ResistSettings.ZoneData.TryGetValue(s.CleanName, out var resistInfo))
			{
				if (spell.ResistType == "Magic" && resistInfo.MagicImmune) return true;
				else if (spell.ResistType == "Fire" && resistInfo.FireImmune) return true;
				else if (spell.ResistType == "Cold" && resistInfo.ColdImmune) return true;
				else if (spell.ResistType == "Poison" && resistInfo.PoisonImmune) return true;
				else if (spell.ResistType == "Disease" && resistInfo.DiseaseImmune) return true;
				else if (spell.ResistType == "Corruption" && resistInfo.CorruptImmune) return true;

				if (spell.ResistType == "Magic" && resistInfo.MagicResistant && !spell.IgnoreResistanceCheck) return true;
				else if (spell.ResistType == "Fire" && resistInfo.FireResistant && !spell.IgnoreResistanceCheck) return true;
				else if (spell.ResistType == "Cold" && resistInfo.ColdResistant && !spell.IgnoreResistanceCheck) return true;
				else if (spell.ResistType == "Poison" && resistInfo.PoisonResistant && !spell.IgnoreResistanceCheck) return true;
				else if (spell.ResistType == "Disease" && resistInfo.DiseaseResistant && !spell.IgnoreResistanceCheck) return true;
				else if (spell.ResistType == "Corruption" && resistInfo.CorruptResistant && !spell.IgnoreResistanceCheck) return true;

			}
			return false;
		}
		private void RegisterEvents()
		{
			EventProcessor.RegisterCommand("/e3resist-list", x =>
			{
				foreach(var item in ZoneData)
				{
					var d = item.Value;
					StringBuilder sb = new StringBuilder();
					sb.Append($"\ayNPC:\ag{d.NPCName} ");
					if(d.MagicResistant) sb.Append($"\ayMR ");
					if(d.MagicImmune) sb.Append($"\ayMRI ");
					if (d.FireResistant) sb.Append($"\ayFR ");
					if (d.FireImmune) sb.Append($"\ayFRI ");
					if (d.ColdResistant) sb.Append($"\ayCR ");
					if (d.ColdImmune) sb.Append($"\ayCRI ");
					if (d.PoisonResistant) sb.Append($"\ayPR ");
					if (d.PoisonImmune) sb.Append($"\ayPRI ");
					if (d.DiseaseResistant) sb.Append($"\ayDR ");
					if (d.DiseaseImmune) sb.Append($"\ayDRI ");
					if (d.CorruptResistant) sb.Append($"\ayCUR ");
					if (d.CorruptImmune) sb.Append($"\ayCURI ");
					MQ.Write(sb.ToString());
				}


			});
			EventProcessor.RegisterCommand("/e3resist-fire", x =>
			{
				ExecuteResistCommand(x, "Fire");
			});
			EventProcessor.RegisterCommand("/e3resist-cold", x =>
			{
				ExecuteResistCommand(x, "Cold");
			});
			EventProcessor.RegisterCommand("/e3resist-magic", x =>
			{
				ExecuteResistCommand(x, "Magic");
			});
			EventProcessor.RegisterCommand("/e3resist-poison", x =>
			{
				ExecuteResistCommand(x, "Poison");
			});
			EventProcessor.RegisterCommand("/e3resist-disease", x =>
			{
				ExecuteResistCommand(x, "Disease");
			});
			EventProcessor.RegisterCommand("/e3resist-corrupt", x =>
			{
				ExecuteResistCommand(x, "Corrupt");
			});

			EventProcessor.RegisterCommand("/e3resist-reload", x =>
			{
				E3.Bots.Broadcast($"Reloading resist data for zone:{Current_ZoneID}");
				LoadData(Current_ZoneID);
			});
		}

		private void ExecuteResistCommand(EventProcessor.CommandMatch x, string resistName)
		{ 
			Type myType = typeof(ResistData);
			FieldInfo fieldResist = myType.GetField($"{resistName}Resistant", BindingFlags.Public | BindingFlags.Instance);
			FieldInfo fieldImmune = myType.GetField($"{resistName}Immune", BindingFlags.Public | BindingFlags.Instance);

			bool isImmune = false;
			bool clear = false;
			if (x.args.Count > 0)
			{
				if (x.args.Contains("immune"))
				{
					isImmune = true;
					x.args.Remove("immune");
				}
			}
			if (x.args.Count > 0)
			{
				if (x.args.Contains("clear"))
				{
					clear = true;
					x.args.Remove("clear");
				}
			}
			string mobName = string.Empty;
			if (x.args.Count > 0)
			{
				//mob name?
				mobName = x.args[0];
			}
			if (String.IsNullOrWhiteSpace(mobName))
			{
				mobName = MQ.Query<string>("${Target.CleanName}");
				if (mobName == "NULL") mobName = string.Empty;
			}

			if (!String.IsNullOrWhiteSpace(mobName))
			{
				if (!ZoneData.TryGetValue(mobName, out var Resist))
				{
					Resist = new ResistData() { NPCName = mobName };
					ZoneData.Add(mobName, Resist);

				}

				if (clear)
				{
					fieldResist.SetValue(Resist, false);
					fieldImmune.SetValue(Resist, false);
				}
				else
				{
					if (isImmune)
					{
						fieldImmune.SetValue(Resist, true);
					}
					fieldResist.SetValue(Resist, true);
				}
			}
			else
			{
				//mob name is invalid
			}
			//okay, now save the data
			SaveData();

		}

		//this is needed as we only drop the table if the bank data is available
		private void CreateResistTable(SQLiteCommand command)
		{

			string sql_Create_Resists = @"CREATE TABLE IF NOT EXISTS resist_data (
												zoneid INTEGER NOT NULL,
												name TEXT NOT NULL,
												MR INTEGER NOT NULL,
												MRI INTEGER NOT NULL,												
												FR INTEGER NOT NULL,
												FRI INTEGER NOT NULL,
												CR INTEGER NOT NULL,
												CRI INTEGER NOT NULL,
												DR INTEGER NOT NULL,
												DRI INTEGER NOT NULL,
												PR INTEGER NOT NULL,
												PRI INTEGER NOT NULL,
												CUR INTEGER NOT NULL,
												CURI INTEGER NOT NULL,
												PRIMARY KEY (zoneid,name)
											);";
			command.CommandText = sql_Create_Resists;
			command.ExecuteNonQuery();
			string sql_createIndex = @"CREATE INDEX IF NOT EXISTS idx_resist_data_zone ON resist_data (zoneid);";
			command.CommandText = sql_createIndex;
			command.ExecuteNonQuery();
		}
		public void LoadData(int zoneID)
		{
			Current_ZoneID = zoneID;
	
			ZoneData.Clear();
			bool fileExists = File.Exists(fileName);
			//MQ.Write($"Connecting to {fileName}");
			if (fileExists)
			{
				_sqlite = new SQLiteConnection($"Data Source={fileName};Mode=ReadOnly;New=False;");
				_sqlite.Open();
			}
			else
			{
				_sqlite = new SQLiteConnection($"Data Source={fileName}");
				_sqlite.Open();
				using (var command = _sqlite.CreateCommand())
				{
					CreateResistTable(command);
				}
			}
			using (_sqlite)
			{
				string sql = $@"select Name as NPCName,MR as MagicResistant, MRI as MagicImmune ,FR as FireResistant, FRI as FireImmune,CR as ColdResistant, CRI as ColdImmune,DR as DiseaseResistant, DRI as DiseaseImmune
						  ,PR as PoisonResistant, PRI as PoisonImmune,CUR as CorruptResistant, CURI as CorruptImmune
						  from resist_data where zoneid = {zoneID};";

				var results = _sqlite.Query<ResistData>(sql);
				foreach(var result in results)
				{
					if (!ZoneData.ContainsKey(result.NPCName))
					{
						ZoneData.Add(result.NPCName, result);
					}
				}
			}
		}
		public void SaveData()
		{
			try
			{
				bool fileExists = File.Exists(fileName);
				MQ.Write($"Connecting to {fileName}");
				if (fileExists)
				{
					_sqlite = new SQLiteConnection($"Data Source={fileName};New=False;");
				}
				else
				{
					_sqlite = new SQLiteConnection($"Data Source={fileName};");
				}
				using (_sqlite)
				{
					_sqlite.Open();
					try
					{
						using (var command = _sqlite.CreateCommand())
						{
							using (var transaction = _sqlite.BeginTransaction())
							{
								string clearzone_sql = $@"delete from resist_data where zoneid = {Current_ZoneID}";
								_sqlite.Execute(clearzone_sql);
								foreach (var pair in ZoneData)
								{
									var z = pair.Value;
									
									command.CommandText = $"insert into resist_data (zoneid,name,mr,mri,fr,fri,cr,cri,pr,pri,dr,dri,cur,curi) " +
														  $"values({Current_ZoneID},$name,$mr,$mri,$fr,$fri,$cr,$cri,$dr,$dri,$pr,$pri,$cur,$curi);";
									command.Parameters.Clear();
									command.Parameters.AddWithValue("name", z.NPCName);
									command.Parameters.AddWithValue("mr", z.MagicResistant);
									command.Parameters.AddWithValue("mri", z.MagicImmune);
									command.Parameters.AddWithValue("fr", z.FireResistant);
									command.Parameters.AddWithValue("fri", z.FireImmune);
									command.Parameters.AddWithValue("cr", z.ColdResistant);
									command.Parameters.AddWithValue("cri", z.ColdImmune);
									command.Parameters.AddWithValue("dr", z.DiseaseResistant);
									command.Parameters.AddWithValue("dri", z.DiseaseImmune);
									command.Parameters.AddWithValue("pr", z.PoisonResistant);
									command.Parameters.AddWithValue("pri", z.PoisonImmune);
									command.Parameters.AddWithValue("cur", z.CorruptResistant);
									command.Parameters.AddWithValue("curi", z.CorruptImmune);
									command.ExecuteNonQuery();
								}

								transaction.Commit();
							}
						}
					}
					catch (Exception ex)
					{
						MQ.Write(ex.Message);
					}
					_sqlite.Close();
				}
			}
			catch (Exception ex)
			{
				MQ.Write(ex.Message);
			}
			MQ.Write("Done!");
			E3.Bots.BroadcastCommandAllZoneNotMe("/e3resist-reload");
		}
	}
}
