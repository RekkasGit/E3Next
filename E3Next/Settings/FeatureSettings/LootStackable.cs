using E3Core.Utility;
using IniParser.Model;
using IniParser;
using System;
using System.Collections.Generic;
using System.IO;
using E3Core.Processors;

namespace E3Core.Settings.FeatureSettings
{
	public class LootStackable : BaseSettings
	{
		string _fileName;
		public IniData parsedData;
		public HashSet<string> AlwaysStackableItems = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		public List<string> AlwaysStackableItemsContains = new List<string>();
		public Boolean LootOnlyCommonTradeSkillItems = false;
		public Boolean LootAllTradeSkillItems = false;
		public Int32 LootValueGreaterThanInCopper = 1;
		public Boolean Enabled = false;
		public Boolean LootInCombat = false;
		public bool HonorLootFileSkips = false;

		public void LoadData()
		{
			AlwaysStackableItems.Clear();
			AlwaysStackableItemsContains.Clear();
			_fileName = GetSettingsFilePath($"Loot_Stackable_{E3.CurrentName}_{E3.ServerName}.ini");
			//this is so we can get the merged data as well. 
			parsedData = CreateSettings(_fileName);
			LoadKeyData("Settings", "Enable (On/Off)", parsedData, ref Enabled);
			LoadKeyData("Settings", "Loot In Combat (On/Off)", parsedData, ref LootInCombat);
			LoadKeyData("Settings", "With Value Greater Than Or Equal in Copper", parsedData, ref LootValueGreaterThanInCopper);
			LoadKeyData("Settings", "Loot all Tradeskill items (On/Off)", parsedData, ref LootAllTradeSkillItems);
			LoadKeyData("Settings", "Loot common tradeskill items ie:pelts ores silks etc (On/Off)", parsedData, ref LootOnlyCommonTradeSkillItems);
			LoadKeyData("Settings", "Honor Loot File Skip Settings (On/Off)", parsedData, ref HonorLootFileSkips);
			LoadKeyData("AlwaysLoot", "Entry", parsedData, AlwaysStackableItems);
			LoadKeyData("AlwaysLootContains", "Entry", parsedData, AlwaysStackableItemsContains);
		}
		public IniData CreateSettings(string fileName)
		{
			//if we need to , its easier to just output the entire file. 

			FileIniDataParser parser = e3util.CreateIniParser();
			IniData newFile = new IniData();
			newFile.Sections.AddSection("Settings");
			var section = newFile.Sections.GetSectionData("Settings");
			section.Keys.AddKey("Enable (On/Off)", "Off");
			section.Keys.AddKey("Loot In Combat (On/Off)", "Off");
			section.Keys.AddKey("With Value Greater Than Or Equal in Copper", "10000");
			section.Keys.AddKey("Loot common tradeskill items ie:pelts ores silks etc (On/Off)", "Off");
			section.Keys.AddKey("Loot all Tradeskill items (On/Off)", "Off");
			section.Keys.AddKey("Honor Loot File Skip Settings (On/Off)", "Off");
			newFile.Sections.AddSection("AlwaysLoot");
		    section = newFile.Sections.GetSectionData("AlwaysLoot");
			section.Keys.AddKey("Entry", "");
			newFile.Sections.AddSection("AlwaysLootContains");
			section = newFile.Sections.GetSectionData("AlwaysLootContains");
			section.Keys.AddKey("Entry", "");

			if (!System.IO.File.Exists(fileName))
			{
				if (!System.IO.Directory.Exists(_configFolder + _settingsFolder))
				{
					System.IO.Directory.CreateDirectory(_configFolder + _settingsFolder);
				}
				//_log.Write($"Creating new Loot Stackable always loot data file:{_fileName}");
				//file straight up doesn't exist, lets create it
				parser.WriteFile(_fileName, newFile);
				_fileLastModified = System.IO.File.GetLastWriteTime(fileName);
				_fileLastModifiedFileName = fileName;
				_fileName = fileName;
			}
			else
			{
				//some reason we were called when this already exists, just return what is there.

				FileIniDataParser fileIniData = e3util.CreateIniParser();
				IniData tParsedData = fileIniData.ReadFile(fileName);

				//overwrite newfile with what was already there
				tParsedData.Merge(newFile);
				newFile = tParsedData;
				//save it it out now
				File.Delete(fileName);
				parser.WriteFile(fileName, tParsedData);

				_fileLastModified = System.IO.File.GetLastWriteTime(fileName);
				_fileLastModifiedFileName = fileName;
				_fileName = fileName;

			}


			return newFile;
		}

		/// <summary>
		/// Saves the data.
		/// </summary>
		public void SaveData()
		{
		
			FileIniDataParser fileIniData = e3util.CreateIniParser();
			File.Delete(_fileName);
			var section = parsedData.Sections["AlwaysLoot"];
			section.RemoveAllKeys();
			foreach (var item in AlwaysStackableItems)
			{
				section.AddKey("Entry", item);
			}
			section = parsedData.Sections["AlwaysLootContains"];
			section.RemoveAllKeys();
			foreach (var item in AlwaysStackableItemsContains)
			{
				section.AddKey("Entry", item);
			}
			fileIniData.WriteFile(_fileName, parsedData);
		}
	}
}

