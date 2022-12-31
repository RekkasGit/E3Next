using E3Core.Processors;
using E3Core.Utility;
using IniParser;
using IniParser.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization.Formatters;
using System.Text;
using System.Threading.Tasks;

namespace E3Core.Settings
{
    public class GeneralSettings : BaseSettings, IBaseSettings
    {
        public Int32 VersionID = 1;
        public Int32 General_MaxResponseDistance;
        public Int32 General_LeashLength;
        public Boolean General_EndMedBreakInCombat;
        public Int32 General_AutoMedBreakPctMana;
        public Int32 Background_IdleTimeout;
        public Int32 Background_AutoInventoryTimer;
        public Int32 Background_CloseSpellbookTimer;
        public Int32 Misc_Gimmie_AzureMinRequests;
        public Int32 Misc_Gimmie_SanguineMinRequests;
        public Int32 Misc_Gimmie_LargeModShardMinRequests;
        public Int32 Misc_Gimmie_MoltenOrbMinRequests;
        public bool Misc_DestroyUnsoldItems;
        public bool AutoMisfitBox;
        public bool AttackOffOnEnrage;
        public bool RelayTells;
        public string General_NetworkMethod = "EQBC";
        public Int32 Loot_LootItemDelay = 300;
        public string Loot_LinkChannel = String.Empty;
        public List<string> Loot_LinkChannelValid = new List<string>() {"g","gu","say","rsay","shout"};

        public bool CorpseSummoning_LootAfterSummon;
        public string Casting_DefaultSpellSet = "Default";
        private Int32 casting_DefaultSpellGem = 8;
        public int Casting_DefaultSpellGem { get { return casting_DefaultSpellGem; } set { if (value > 0 && value < 15) { casting_DefaultSpellGem = value; } } }

        public Boolean BuffRequests_AllowBuffRequests = true;
        public String BuffRequests_RestrictedPCs;
        public String BuffRequests_AllowedPcs;
        //loot
        public Int32 Loot_CorpseSeekRadius;
        public bool Loot_LootInCombat;
        public Int32 Loot_NumberOfFreeSlotsOpen;
        public Boolean Loot_OnlyStackableOnlyCommonTradeSkillItems = false;
        public Boolean Loot_OnlyStackableAllTradeSkillItems = false;
        public List<string> Loot_OnlyStackableAlwaysLoot = new List<string>();
        public Int32 Loot_OnlyStackableValueGreaterThanInCopper = 1;
        public Boolean Loot_OnlyStackableEnabled = false;

        public Boolean AutoTribute_Enabled;

        public Boolean Assists_AutoAssistEnabled;
        public Int32 Assists_MaxEngagedDistance;
        public Int32 Assists_AEThreatRange=100;
        public String Assists_AcceptableTargetTypes;
        public Int32 Assists_LongTermDebuffRecast = 30;
        public Int32 Assists_ShortTermDebuffRecast = 5;

        public bool AutoTrade_All = false;
        public bool AutoTrade_Bots = false;
        public bool AutoTrade_Group = false;
        public bool AutoTrade_Guild = false;
        public bool AutoTrade_Raid = false;

        public Int32 Movement_ChaseDistanceMin = 10;
        public Int32 Movement_ChaseDistanceMax = 500;
        public Int32 Movement_NavStopDistance = 10;
        public Int32 Movement_AnchorDistanceMin = 15;
        public Int32 Movement_AnchorDistanceMax = 150;

        public GeneralSettings()
        {
            LoadData();
        }
        public void LoadData()
        {

            string filename = GetSettingsFilePath("General Settings.ini");

            IniData parsedData;

            FileIniDataParser fileIniData = e3util.CreateIniParser();

            if (!System.IO.File.Exists(filename))
            {
                if (!System.IO.Directory.Exists(_configFolder + _settingsFolder))
                {
                    System.IO.Directory.CreateDirectory(_configFolder + _settingsFolder);
                }
             
                parsedData = CreateSettings();
            }
            else
            {
                parsedData = fileIniData.ReadFile(filename);
            }
            _fileLastModifiedFileName = filename;
            _fileLastModified = System.IO.File.GetLastWriteTime(filename);
            //have the data now!
            if (parsedData==null)
            {
                throw new Exception("Could not load General Settings file");
            }

            LoadKeyData("General", "Max Response Distance",parsedData, ref General_MaxResponseDistance);
            LoadKeyData("General", "Leash Length", parsedData, ref General_LeashLength);
            LoadKeyData("General", "End MedBreak in Combat(On/Off)", parsedData, ref General_EndMedBreakInCombat);
            LoadKeyData("General", "AutoMedBreak PctMana", parsedData, ref General_AutoMedBreakPctMana);
            //    section.Keys.AddKey("NetworkMethod", "EQBC");

            LoadKeyData("General", "NetworkMethod",parsedData, ref General_NetworkMethod);

            LoadKeyData("Background", "Idle Time Out (Min)", parsedData, ref Background_IdleTimeout);
            LoadKeyData("Background", "Auto-Inventory Timer (Sec)", parsedData, ref Background_AutoInventoryTimer);
            LoadKeyData("Background", "Close Spellbook Timer (Sec)", parsedData, ref Background_CloseSpellbookTimer);

            LoadKeyData("Misc", "Gimmie Azure Min Requests", parsedData, ref Misc_Gimmie_AzureMinRequests);
            LoadKeyData("Misc", "Gimmie Sanguine Min Requests", parsedData, ref Misc_Gimmie_SanguineMinRequests);
            LoadKeyData("Misc", "Gimmie Large Mod Shard Min Requests", parsedData, ref Misc_Gimmie_LargeModShardMinRequests);
            LoadKeyData("Misc", "Gimmie MoltenOrb Min Requests", parsedData, ref Misc_Gimmie_MoltenOrbMinRequests);
            LoadKeyData("Misc", "Destroy Unsold Items(On/Off)", parsedData, ref Misc_DestroyUnsoldItems);
            LoadKeyData("Misc", "Automatically Use Misfit Box (On/Off)", parsedData, ref AutoMisfitBox);
            LoadKeyData("Misc", "Turn Player Attack Off During Enrage (On/Off)", parsedData, ref AttackOffOnEnrage);
            LoadKeyData("Misc", "Relay Tells (On/Off)", parsedData, ref RelayTells);

            LoadKeyData("Loot", "Loot Link Channel", parsedData, ref Loot_LinkChannel);
            //check valid loot channels
            if (!Loot_LinkChannelValid.Contains(Loot_LinkChannel, StringComparer.OrdinalIgnoreCase))
            {
                Loot_LinkChannel = String.Empty;
            }
          
            LoadKeyData("Loot", "Corpse Seek Radius", parsedData, ref Loot_CorpseSeekRadius);
            LoadKeyData("Loot", "LootItemDelay", parsedData, ref Loot_LootItemDelay);
            //no lower than 300ms
            if (Loot_LootItemDelay < 300) Loot_LootItemDelay = 300;

            LoadKeyData("Loot", "Loot in Combat", parsedData, ref Loot_LootInCombat);
            LoadKeyData("Loot", "NumOfFreeSlotsOpen(1+)", parsedData, ref Loot_NumberOfFreeSlotsOpen);
            LoadKeyData("Loot", "Loot Only Stackable: Enable (On/Off)", parsedData, ref Loot_OnlyStackableEnabled);
            LoadKeyData("Loot", "Loot Only Stackable: With Value Greater Than Or Equal in Copper", parsedData, ref Loot_OnlyStackableValueGreaterThanInCopper);
            LoadKeyData("Loot", "Loot Only Stackable: Loot all Tradeskill items (On/Off)", parsedData, ref Loot_OnlyStackableAllTradeSkillItems);
            LoadKeyData("Loot", "Loot Only Stackable: Loot common tradeskill items ie:pelts ores silks etc (On/Off)", parsedData, ref Loot_OnlyStackableOnlyCommonTradeSkillItems);
            LoadKeyData("Loot", "Loot Only Stackable: Always Loot Item", parsedData, Loot_OnlyStackableAlwaysLoot);
            
            LoadKeyData("Corpse Summoning", "Corpse Summoning", parsedData, ref CorpseSummoning_LootAfterSummon);
            LoadKeyData("Casting", "Default Spell Set", parsedData, ref Casting_DefaultSpellSet);

            //so we can validate a default gem is always acceptable range
            Int32 spellGem = Casting_DefaultSpellGem;
            LoadKeyData("Casting", "Default Spell Gem", parsedData, ref spellGem);
            Casting_DefaultSpellGem = spellGem;

            LoadKeyData("Buff Requests", "Allow Buff Requests (On/Off)", parsedData, ref BuffRequests_AllowBuffRequests);
            LoadKeyData("Buff Requests", "Restricted PCs (When Requests [On])", parsedData, ref BuffRequests_RestrictedPCs);
            LoadKeyData("Buff Requests", "Allowed PCs (When Requests [Off])", parsedData, ref BuffRequests_AllowedPcs);

            LoadKeyData("Auto-Tribute", "Auto -Tribute (On/Off)", parsedData, ref AutoTribute_Enabled);

            LoadKeyData("Assists", "Auto-Assist (On/Off)", parsedData, ref Assists_AutoAssistEnabled);

            LoadKeyData("Assists", "Max Engage Distance", parsedData, ref Assists_MaxEngagedDistance);
            LoadKeyData("Assists", "AE Threat Range", parsedData, ref Assists_AEThreatRange);
            if (Assists_AEThreatRange < 10) Assists_AEThreatRange = 10;
            if (Assists_AEThreatRange > 300) Assists_AEThreatRange = 300;

            LoadKeyData("Assists", "Acceptable Target Types", parsedData, ref Assists_AcceptableTargetTypes);
            LoadKeyData("Assists", "Long Term Debuff Recast(s)", parsedData, ref Assists_LongTermDebuffRecast);
            LoadKeyData("Assists", "Short Term Debuff Recast(s)", parsedData, ref Assists_ShortTermDebuffRecast);

            LoadKeyData("AutoTrade", "All (On/Off)", parsedData, ref AutoTrade_All);
            LoadKeyData("AutoTrade", "Bots (On/Off)", parsedData, ref AutoTrade_Bots);
            LoadKeyData("AutoTrade", "Group (On/Off)", parsedData, ref AutoTrade_Group);
            LoadKeyData("AutoTrade", "Guild (On/Off)", parsedData, ref AutoTrade_Guild);
            LoadKeyData("AutoTrade", "Raid (On/Off)", parsedData, ref AutoTrade_Raid);

            LoadKeyData("Movement", "Chase Distance Minimum", parsedData, ref Movement_ChaseDistanceMin);
            LoadKeyData("Movement", "Chase Distance Maximum", parsedData, ref Movement_ChaseDistanceMax);
            LoadKeyData("Movement", "Nav Stop Distance", parsedData, ref Movement_NavStopDistance);
            LoadKeyData("Movement", "Anchor Distance Minimum", parsedData, ref Movement_AnchorDistanceMin);
            LoadKeyData("Movement", "Anchor Distance Maximum", parsedData, ref Movement_AnchorDistanceMax);

            if (Movement_ChaseDistanceMin < 1)
            {
                MQ.Write($"Chase Distance Minimum can't be less than 1, defaulting to 10 units");
                Movement_ChaseDistanceMin = 10;
            }
            if (Movement_ChaseDistanceMax < Movement_ChaseDistanceMin)
            {
                MQ.Write($"Chase Distance Max lower than Chase Distance Min, defaulting to min:10, max:500 units");
                Movement_ChaseDistanceMin = 10;
                Movement_ChaseDistanceMax = 500;
            }
            if (Movement_AnchorDistanceMin < 1)
            {
                MQ.Write($"Anchor Distance Minimum can't be less than 1, defaulting to 10 units");
                Movement_AnchorDistanceMin = 10;
            }
            if (Movement_AnchorDistanceMax < Movement_AnchorDistanceMin)
            {
                MQ.Write($"Anchor Distance Max lower than Anchor Distance Min, defaulting to min:15, max:500 units");
                Movement_AnchorDistanceMin = 15;
                Movement_AnchorDistanceMax = 500;
            }

        }

        public IniData CreateSettings()
        {

            IniParser.FileIniDataParser parser = e3util.CreateIniParser();
            IniData newFile = new IniData();


            newFile.Sections.AddSection("General");
            var section = newFile.Sections.GetSectionData("General");
            section.Keys.AddKey("Max Response Distance", "500");
            section.Keys.AddKey("Leash Length", "250");
            section.Keys.AddKey("End MedBreak in Combat(On/Off)", "On");
            section.Keys.AddKey("AutoMedBreak PctMana", "0");
            section.Keys.AddKey("NetworkMethod", "EQBC");

            newFile.Sections.AddSection("Background");
            section = newFile.Sections.GetSectionData("Background");
            section.Keys.AddKey("Idle Time Out (Min)", "10");
            section.Keys.AddKey("Auto-Inventory Timer (Sec)", "30");
            section.Keys.AddKey("Close Spellbook Timer (Sec)", "30");
            section.Keys.AddKey("AutoSetPctAAExp (On/Off)", "Off");

            //Misc
            newFile.Sections.AddSection("Misc");
            section = newFile.Sections.GetSectionData("Misc");
            section.Keys.AddKey("Gimmie Azure Min Requests","2");
            section.Keys.AddKey("Gimmie Sanguine Min Requests","1");
            section.Keys.AddKey("Gimmie Large Mod Shard Min Requests","1");
            section.Keys.AddKey("Gimmie MoltenOrb Min Requests","3");
            section.Keys.AddKey("Destroy Unsold Items(On/Off)","Off");
            section.Keys.AddKey("Automatically Use Misfit Box (On/Off)", "On");
            section.Keys.AddKey("Turn Player Attack Off During Enrage (On/Off)", "On");
            section.Keys.AddKey("Relay Tells (On/Off)", "Off");
            //Loot
            newFile.Sections.AddSection("Loot");
            section = newFile.Sections.GetSectionData("Loot");
            section.Keys.AddKey("Loot Link Channel","say");
	        section.Keys.AddKey("Corpse Seek Radius","125");
            section.Keys.AddKey("Loot in Combat","TRUE");
            section.Keys.AddKey("NumOfFreeSlotsOpen(1+)","0");
            section.Keys.AddKey("Loot Only Stackable: Enable (On/Off)", "Off");
            section.Keys.AddKey("Loot Only Stackable: With Value Greater Than Or Equal in Copper", "10000");
            section.Keys.AddKey("Loot Only Stackable: Loot common tradeskill items ie:pelts ores silks etc (On/Off)", "Off");
            section.Keys.AddKey("Loot Only Stackable: Loot all Tradeskill items (On/Off)", "Off");
            section.Keys.AddKey("LootItemDelay", "300");

            //Corpse Summoning
            newFile.Sections.AddSection("Corpse Summoning");
            section = newFile.Sections.GetSectionData("Corpse Summoning");
            section.Keys.AddKey("Loot After Summoning (On/Off)","Off");

            //Casting
            newFile.Sections.AddSection("Casting");
            section = newFile.Sections.GetSectionData("Casting");
            section.Keys.AddKey("Default Spell Set"," Default");
	        section.Keys.AddKey("Default Spell Gem","8");

            //Buff Requests
            newFile.Sections.AddSection("Buff Requests");
            section = newFile.Sections.GetSectionData("Buff Requests");
            section.Keys.AddKey("Allow Buff Requests (On/Off)","On");
            section.Keys.AddKey("Restricted PCs (When Requests [On])","");
            section.Keys.AddKey("Allowed PCs (When Requests [Off])","");


            //Auto-Tribute
            newFile.Sections.AddSection("Auto-Tribute");
            section = newFile.Sections.GetSectionData("Auto-Tribute");
            section.Keys.AddKey("Auto -Tribute (On/Off)","Off");

            //Assists
            newFile.Sections.AddSection("Assists");
            section = newFile.Sections.GetSectionData("Assists");
            section.Keys.AddKey("Auto-Assist (On/Off)", "Off");
            section.Keys.AddKey("Max Engage Distance", "250");
            section.Keys.AddKey("AE Threat Range", "100");
            section.Keys.AddKey("Acceptable Target Types", "NPC,Pet");
            section.Keys.AddKey("Long Term Debuff Recast(s)", "30");
            section.Keys.AddKey("Short Term Debuff Recast(s)", "5");

            //Trade
            newFile.Sections.AddSection("AutoTrade");
            section = newFile.Sections.GetSectionData("AutoTrade");
            section.Keys.AddKey("All (On/Off)", "Off");
            section.Keys.AddKey("Bots (On/Off)", "Off");
            section.Keys.AddKey("Group (On/Off)", "Off");
            section.Keys.AddKey("Guild (On/Off)", "Off");
            section.Keys.AddKey("Raid (On/Off)", "Off");

            newFile.Sections.AddSection("Movement");
            section = newFile.Sections.GetSectionData("Movement");
            section.Keys.AddKey("Chase Distance Minimum", "10");
            section.Keys.AddKey("Chase Distance Maximum", "500");
            section.Keys.AddKey("Nav Stop Distance", "10");
            section.Keys.AddKey("Anchor Distance Minimum", "15");
            section.Keys.AddKey("Anchor Distance Maximum", "150");

            string filename = GetSettingsFilePath("General Settings.ini");
            if (!System.IO.File.Exists(filename))
            {
                if (!System.IO.Directory.Exists(_configFolder + _settingsFolder))
                {
                    System.IO.Directory.CreateDirectory(_configFolder + _settingsFolder);
                }
                _log.Write($"Creating new General Settings file:{filename}");
                //file straight up doesn't exist, lets create it
                parser.WriteFile(filename, newFile);

            }
            else
            {
                //some reason we were called when this already exists, just return what is there.

                FileIniDataParser fileIniData = e3util.CreateIniParser();
                IniData parsedData = fileIniData.ReadFile(filename);

                return parsedData;
               
            }


            return newFile;

        }
    }
}
