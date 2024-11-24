using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using E3Core.Data;
using E3Core.Settings;
using E3Core.Settings.FeatureSettings;
using E3Core.Utility;
using MonoCore;

namespace E3Core.Processors
{
    public static class Zoning
    {
		[ExposedData("Zoning", "CurrentZone")]
		public static Zone CurrentZone;
        public static Dictionary<Int32, Zone> ZoneLookup = new Dictionary<Int32, Zone>();
        public static TributeDataFile TributeDataFile = new TributeDataFile();
		private static ISpawns _spawns = E3.Spawns;
		private static Dictionary<Int32, Spawn> _recordedSpawns = new Dictionary<Int32, Spawn>();
		private static long _nextRecordingCheck = 0;
		private static long _nextRecordingInterval = 500;
		private static bool _recordingSpawnLocations = false;
		private static string _recordingFileName = String.Empty;
		private static Int32 _recordingZoneID = 0;
		private static IMQ MQ = E3.MQ;

        [SubSystemInit]
        public static void Zoning_Init()
        {
            InitZoneLookup();
        }

        public static void Zoned(Int32 zoneId)
        {
            // add our new zone to the zone lookup if necessary
            if (!ZoneLookup.TryGetValue(zoneId, out CurrentZone))
            {
                CurrentZone = new Zone(zoneId);
                ZoneLookup.Add(zoneId, new Zone(zoneId));
            }

            TributeDataFile.ToggleTribute();
            Rez.TurnOffAutoRezSkip();
        }

	
		private static void InitZoneLookup()
        {
            TributeDataFile.LoadData();

            // need to do this here because the event processors haven't been loaded yet
            var currentZone = MQ.Query<Int32>("${Zone.ID}");
            Zoned(currentZone);


			EventProcessor.RegisterCommand("/e3zonedump", (x) =>
			{

				if (x.args.Count != 3) return;

				Int32 npcIDToStartAt = Int32.Parse(x.args[0]);
				Int32 spawnGroupIDTOStartAt = Int32.Parse(x.args[1]);
				Int32 spawn2IDToStartAt = Int32.Parse(x.args[2]);
				string zoneName = Zoning.CurrentZone.ShortName;

				string filename = BaseSettings.GetSettingsFilePath($"ZoneDump_{zoneName}.txt");
				using (var stream = new System.IO.FileStream(filename, System.IO.FileMode.Create))
				{
					using (var streamWriter = new System.IO.StreamWriter(stream))
					{
						foreach (var s in _spawns.Get().OrderBy(z => z.X))
						{

							if (s.TypeDesc != "NPC") continue;
							//based off Guard_Lasen
							string sqlStatement = $@"INSERT INTO `npc_types` (`id`,`name`, `lastname`, `level`, `race`, `class`, `bodytype`, `hp`, `mana`, `gender`, `texture`, `helmtexture`, `herosforgemodel`, `size`, `hp_regen_rate`, `hp_regen_per_second`, `mana_regen_rate`, `loottable_id`, `merchant_id`, `alt_currency_id`, `npc_spells_id`, `npc_spells_effects_id`, `npc_faction_id`, `adventure_template_id`, `trap_template`, `mindmg`, `maxdmg`, `attack_count`, `npcspecialattks`, `special_abilities`, `aggroradius`, `assistradius`, `face`, `luclin_hairstyle`, `luclin_haircolor`, `luclin_eyecolor`, `luclin_eyecolor2`, `luclin_beardcolor`, `luclin_beard`, `drakkin_heritage`, `drakkin_tattoo`, `drakkin_details`, `armortint_id`, `armortint_red`, `armortint_green`, `armortint_blue`, `d_melee_texture1`, `d_melee_texture2`, `ammo_idfile`, `prim_melee_type`, `sec_melee_type`, `ranged_type`, `runspeed`, `MR`, `CR`, `DR`, `FR`, `PR`, `Corrup`, `PhR`, `see_invis`, `see_invis_undead`, `qglobal`, `AC`, `npc_aggro`, `spawn_limit`, `attack_speed`, `attack_delay`, `findable`, `STR`, `STA`, `DEX`, `AGI`, `_INT`, `WIS`, `CHA`, `see_hide`, `see_improved_hide`, `trackable`, `isbot`, `exclude`, `ATK`, `Accuracy`, `Avoidance`, `slow_mitigation`, `version`, `maxlevel`, `scalerate`, `private_corpse`, `unique_spawn_by_name`, `underwater`, `isquest`, `emoteid`, `spellscale`, `healscale`, `no_target_hotkey`, `raid_target`, `armtexture`, `bracertexture`, `handtexture`, `legtexture`, `feettexture`, `light`, `walkspeed`, `peqid`, `unique_`, `fixed`, `ignore_despawn`, `show_name`, `untargetable`, `charm_ac`, `charm_min_dmg`, `charm_max_dmg`, `charm_attack_delay`, `charm_accuracy_rating`, `charm_avoidance_rating`, `charm_atk`, `skip_global_loot`, `rare_spawn`, `stuck_behavior`, `model`, `flymode`, `always_aggro`, `exp_mod`, `heroic_strikethrough`, `faction_amount`, `keeps_sold_items`) VALUES
																			({npcIDToStartAt},'{s.CleanName.Replace(" ", "_")}', '', {s.Level}, {s.RaceID}, {s.ClassID}, {s.BodyTypeID}, 100, 0, {s.GenderID}, 1, 1, 0, 6, 1, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 1, 19, -1, 'mB', '10,1', 35, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 'IT10', 28, 28, 7, 1.25, 75, 4, 4, 4, 4, 6, 10, 0, 1, 0, 34, 1, 0, 0, 30, 0, 35, 35, 35, 35, 35, 35, 35, 0, 0, 1, 0, 1, 0, 0, 0, 0, 0, 0, 100, 0, 0, 0, 1, 1185, 100, 100, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, -1, 0, 100, 0, 0, 1);";
							streamWriter.WriteLine(sqlStatement);

							sqlStatement = $@"INSERT INTO `spawngroup` (`id`, `name`, `spawn_limit`, `dist`, `max_x`, `min_x`, `max_y`, `min_y`, `delay`, `mindelay`, `despawn`, `despawn_timer`, `wp_spawns`) 
															    VALUES ({spawnGroupIDTOStartAt}, '{zoneName}_{npcIDToStartAt}', 0, 0, 0, 0, 0, 0, 0, 15000, 0, 100, 0);";
							streamWriter.WriteLine(sqlStatement);
							sqlStatement = $@"INSERT INTO `spawnentry` (`spawngroupID`, `npcID`, `chance`, `condition_value_filter`, `min_expansion`, `max_expansion`, `content_flags`, `content_flags_disabled`) 
																VALUES ({spawnGroupIDTOStartAt}, {npcIDToStartAt}, 50, 1, -1, -1, NULL, NULL);";


							streamWriter.WriteLine(sqlStatement);

							sqlStatement = $@"INSERT INTO `spawn2` (`id`, `spawngroupID`, `zone`, `version`, `x`, `y`, `z`, `heading`, `respawntime`, `variance`, `pathgrid`, `path_when_zone_idle`, `_condition`, `cond_value`, `enabled`, `animation`, `min_expansion`, `max_expansion`, `content_flags`, `content_flags_disabled`) 
															VALUES ({spawn2IDToStartAt}, {spawnGroupIDTOStartAt}, '{zoneName}', 0, {s.X}, {s.Y}, {s.Z}, {s.Heading}, 640, 0, 0, 0, 2, 1, 1, 0, -1, -1, NULL, NULL);";

							streamWriter.WriteLine(sqlStatement);

							npcIDToStartAt++;
							spawnGroupIDTOStartAt++;
							spawn2IDToStartAt++;
						}
					}
				}
			}, "dumps out the current zone npc data");


			EventProcessor.RegisterCommand("/e3zonerecord", (x) =>
			{
			

				if (x.args.Count != 5) return;

				if(!_recordingSpawnLocations)
				{
					_recordingSpawnLocations = true;
					E3.Bots.Broadcast($"\arTurning on E3ZoneRecord!");
				}
				else
				{
					E3.Bots.Broadcast($"\arTurning Off E3ZoneRecord!");
					_recordingSpawnLocations = false;
					_recordingFileName = String.Empty;
					_recordingZoneID = 0;

					foreach(var entry in _recordedSpawns)
					{
						entry.Value.Dispose();

					}
					_recordedSpawns.Clear();

				}
				if (_recordingSpawnLocations)
				{
					Int32 npcIDToStartAt = Int32.Parse(x.args[0]);
					Int32 spawnGroupIDTOStartAt = Int32.Parse(x.args[1]);
					Int32 spawn2IDToStartAt = Int32.Parse(x.args[2]);
					_recordingZoneID = Int32.Parse(x.args[3]);
					Int32 gridIDStart = Int32.Parse(x.args[4]);

					string zoneName = Zoning.CurrentZone.ShortName;
					_recordingFileName = BaseSettings.GetSettingsFilePath($"ZoneDump_{zoneName}.txt");
					using (var stream = new System.IO.FileStream(_recordingFileName, System.IO.FileMode.Create))
					{
						using (var streamWriter = new System.IO.StreamWriter(stream))
						{
							foreach (var s in _spawns.Get().OrderBy(z => z.X))
							{

								Spawn tspawn = Spawn.Aquire();
								tspawn.Init(s._data, s._dataSize);
								tspawn.TableID = npcIDToStartAt;
								tspawn.GridID = gridIDStart;
								tspawn.Initial_Heading = tspawn.Heading;

								_recordedSpawns.Add(s.ID, tspawn);


								if (s.TypeDesc != "NPC") continue;
								//based off Guard_Lasen
								string sqlStatement = $@"INSERT INTO `npc_types` (`id`,`name`, `lastname`, `level`, `race`, `class`, `bodytype`, `hp`, `mana`, `gender`, `texture`, `helmtexture`, `herosforgemodel`, `size`, `hp_regen_rate`, `hp_regen_per_second`, `mana_regen_rate`, `loottable_id`, `merchant_id`, `alt_currency_id`, `npc_spells_id`, `npc_spells_effects_id`, `npc_faction_id`, `adventure_template_id`, `trap_template`, `mindmg`, `maxdmg`, `attack_count`, `npcspecialattks`, `special_abilities`, `aggroradius`, `assistradius`, `face`, `luclin_hairstyle`, `luclin_haircolor`, `luclin_eyecolor`, `luclin_eyecolor2`, `luclin_beardcolor`, `luclin_beard`, `drakkin_heritage`, `drakkin_tattoo`, `drakkin_details`, `armortint_id`, `armortint_red`, `armortint_green`, `armortint_blue`, `d_melee_texture1`, `d_melee_texture2`, `ammo_idfile`, `prim_melee_type`, `sec_melee_type`, `ranged_type`, `runspeed`, `MR`, `CR`, `DR`, `FR`, `PR`, `Corrup`, `PhR`, `see_invis`, `see_invis_undead`, `qglobal`, `AC`, `npc_aggro`, `spawn_limit`, `attack_speed`, `attack_delay`, `findable`, `STR`, `STA`, `DEX`, `AGI`, `_INT`, `WIS`, `CHA`, `see_hide`, `see_improved_hide`, `trackable`, `isbot`, `exclude`, `ATK`, `Accuracy`, `Avoidance`, `slow_mitigation`, `version`, `maxlevel`, `scalerate`, `private_corpse`, `unique_spawn_by_name`, `underwater`, `isquest`, `emoteid`, `spellscale`, `healscale`, `no_target_hotkey`, `raid_target`, `armtexture`, `bracertexture`, `handtexture`, `legtexture`, `feettexture`, `light`, `walkspeed`, `peqid`, `unique_`, `fixed`, `ignore_despawn`, `show_name`, `untargetable`, `charm_ac`, `charm_min_dmg`, `charm_max_dmg`, `charm_attack_delay`, `charm_accuracy_rating`, `charm_avoidance_rating`, `charm_atk`, `skip_global_loot`, `rare_spawn`, `stuck_behavior`, `model`, `flymode`, `always_aggro`, `exp_mod`, `heroic_strikethrough`, `faction_amount`, `keeps_sold_items`) VALUES
																			({npcIDToStartAt},'{s.CleanName.Replace(" ", "_")}', '', {s.Level}, {s.RaceID}, {s.ClassID}, {s.BodyTypeID}, 100, 0, {s.GenderID}, 1, 1, 0, 6, 1, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 1, 19, -1, 'mB', '10,1', 35, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 'IT10', 28, 28, 7, 1.25, 75, 4, 4, 4, 4, 6, 10, 0, 1, 0, 34, 1, 0, 0, 30, 0, 35, 35, 35, 35, 35, 35, 35, 0, 0, 1, 0, 1, 0, 0, 0, 0, 0, 0, 100, 0, 0, 0, 1, 1185, 100, 100, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, -1, 0, 100, 0, 0, 1);";
								streamWriter.WriteLine(sqlStatement);

								sqlStatement = $@"INSERT INTO `spawngroup` (`id`, `name`, `spawn_limit`, `dist`, `max_x`, `min_x`, `max_y`, `min_y`, `delay`, `mindelay`, `despawn`, `despawn_timer`, `wp_spawns`) 
															    VALUES ({spawnGroupIDTOStartAt}, '{zoneName}_{npcIDToStartAt}', 0, 0, 0, 0, 0, 0, 0, 15000, 0, 100, 0);";
								streamWriter.WriteLine(sqlStatement);
								sqlStatement = $@"INSERT INTO `spawnentry` (`spawngroupID`, `npcID`, `chance`, `condition_value_filter`, `min_expansion`, `max_expansion`, `content_flags`, `content_flags_disabled`) 
																VALUES ({spawnGroupIDTOStartAt}, {npcIDToStartAt}, 50, 1, -1, -1, NULL, NULL);";


								streamWriter.WriteLine(sqlStatement);

								sqlStatement = $@"INSERT INTO `spawn2` (`id`, `spawngroupID`, `zone`, `version`, `x`, `y`, `z`, `heading`, `respawntime`, `variance`, `pathgrid`, `path_when_zone_idle`, `_condition`, `cond_value`, `enabled`, `animation`, `min_expansion`, `max_expansion`, `content_flags`, `content_flags_disabled`) 
															VALUES ({spawn2IDToStartAt}, {spawnGroupIDTOStartAt}, '{zoneName}', 0, {s.X}, {s.Y}, {s.Z}, {s.Heading}, 640, 0, 0, 0, 2, 1, 1, 0, -1, -1, NULL, NULL);";

								streamWriter.WriteLine(sqlStatement);

								npcIDToStartAt++;
								spawnGroupIDTOStartAt++;
								spawn2IDToStartAt++;
								gridIDStart++;
							}
						}
					}

				}
				

				
			}, "dumps out the current zone npc data");


		}
		[ClassInvoke(Class.All)]
		public static void RecordZoneData()
		{

			if (_recordingSpawnLocations)
			{
				if (!e3util.ShouldCheck(ref _nextRecordingCheck, _nextRecordingInterval)) return;

				_spawns.RefreshList();

				foreach (var s in _recordedSpawns.Values)
				{
					if (s.Recording_Complete) continue;

					if(_spawns.TryByID(s.ID, out var cs))
					{
						float lastHeading = s.Heading;
						if (cs.Heading != lastHeading && cs.X != s.X && cs.Y != s.Y && cs.Z != s.Z)
						{
							//if we have some how wrapped back around to our initial headig, assume we are at the start of our pathing, and end recording.
							if (cs.Heading == s.Initial_Heading)
							{
								s.Recording_Complete = true;
								continue;
							}

							using (var stream = new System.IO.FileStream(_recordingFileName, System.IO.FileMode.Append))
							{
								String sqlStatement = String.Empty;
								using (var streamWriter = new System.IO.StreamWriter(stream))
								{
									if (!s.Recording_MovementOccured)
									{
										s.Recording_MovementOccured = true;
										//update the npc table with the new grid we are creating
										sqlStatement = $@"update npc_types set pathgrid={s.GridID} where id ={s.TableID}";
										streamWriter.WriteLine(sqlStatement);
										sqlStatement = $@"insert into grid (id,zoneid,type,type2) values({s.GridID},{_recordingZoneID},{0},{0})";
										streamWriter.WriteLine(sqlStatement);
									}

									sqlStatement = $@"insert into grid_entries (gridid,zoneid,number,x,y,z,heading,pause,centerpoint)
																Values({s.GridID},{_recordingZoneID},{s.Recording_StepCount + 1},{cs.X},{cs.Y},{cs.Z},{cs.Heading},{0},{0})";

									streamWriter.WriteLine(sqlStatement);
									s.Heading = cs.Heading;
									s.Recording_StepCount++;
								}
							}
							
						}

					}

					
				}

			}
		}

	}
}
