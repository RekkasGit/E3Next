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
using static System.Collections.Specialized.BitVector32;

namespace E3Core.Settings
{
 


    public class GeneralSettings : BaseSettings, IBaseSettings
    {
        

        public Int32 VersionID = 1;
        public Int32 General_AutoMedBreakPctMana;
        public bool AutoMisfitBox;
        public bool AttackOffOnEnrage;
        public bool RelayTells;
        public string General_NetworkMethod = "EQBC";
        public DefaultBroadcast General_BroadCast_Default = DefaultBroadcast.Group;
        public bool General_HealWhileNavigating = true;
        public bool General_BeepNotifications = true;

        public Int32 Loot_LootItemDelay = 300;
        public string Loot_LinkChannel = String.Empty;
        public List<string> Loot_LinkChannelValid = new List<string>() {"g","gu","say","rsay","shout","gsay", "rs","bc","e3bc"};
        public Int32 MaxGemSlots = 8 + MQ.Query<Int32>("${Me.AltAbility[Mnemonic Retention].Rank}");

        private Int32 casting_DefaultSpellGem = 8;
        public int Casting_DefaultSpellGem 
        { 
            get { return casting_DefaultSpellGem; } 
            set { if (value > 0 && value <= MaxGemSlots) { casting_DefaultSpellGem = value; } } 
        }

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
        public Int32 Loot_TimeToWaitAfterAssist = 2000;
        public bool Loot_OnlyStackableHonorLootFileSkips = false;

        public Boolean Assists_AutoAssistEnabled=false;
        public Int32 Assists_MaxEngagedDistance=250;
        public Int32 Assists_AEThreatRange=100;

        public bool AutoTrade_WaitForTrade = true;
        public bool AutoTrade_All = false;
        public bool AutoTrade_Bots = true;
        public bool AutoTrade_Group = false;
        public bool AutoTrade_Guild = false;
        public bool AutoTrade_Raid = false;
        public List<string> General_E3NetworkAddPathToMonitor = new List<string>();
        public string DiscordBotToken = string.Empty;
        public string DiscordGuildChannelId = string.Empty;
        public string DiscordServerId = string.Empty;
        public string DiscordMyUserId = string.Empty;

		public Int32 Movement_StandingStill = 10000;
        public Int32 Movement_ChaseDistanceMin = 10;
        public Int32 Movement_ChaseDistanceMax = 500;
        public Int32 Movement_NavStopDistance = 10;
        public Int32 Movement_AnchorDistanceMin = 15;
        public Int32 Movement_AnchorDistanceMax = 150;
        public Int32 ManaStone_NumerOfClicksPerLoop = 40;
        public Int32 ManaStone_NumberOfLoops = 25;
        public Int32 ManaStone_DelayBetweenLoops = 50;

        public bool ManaStone_EnabledInCombat = true;
        public Int32 ManaStone_InCombatMinMana = 40;
        public Int32 ManaStone_InCombatMaxMana = 75;
        public Int32 ManaStone_MinHP = 60;
        public Int32 ManaStone_OutOfCombatMinMana = 85;
        public Int32 ManaStone_OutOfCombatMaxMana = 95;


        private string _filename = String.Empty;

        public GeneralSettings()
        {
            LoadData();
        }
        public void LoadData()
        {

            _filename = GetSettingsFilePath("General Settings.ini");
            if (!String.IsNullOrEmpty(CurrentSet))
            {
                _filename = _filename.Replace(".ini", "_" + CurrentSet + ".ini");
            }
            IniData parsedData;

            FileIniDataParser fileIniData = e3util.CreateIniParser();

            if (!System.IO.File.Exists(_filename))
            {
                if (!System.IO.Directory.Exists(_configFolder + _settingsFolder))
                {
                    System.IO.Directory.CreateDirectory(_configFolder + _settingsFolder);
                }
             
                parsedData = CreateSettings(_filename);
            }
            else
            {
                parsedData = fileIniData.ReadFile(_filename);
            }
            _fileLastModifiedFileName = _filename;
            _fileLastModified = System.IO.File.GetLastWriteTime(_filename);
            //have the data now!
            if (parsedData==null)
            {
                throw new Exception("Could not load General Settings file");
            }

            LoadKeyData("General", "AutoMedBreak PctMana", parsedData, ref General_AutoMedBreakPctMana);
            //    section.Keys.AddKey("NetworkMethod", "EQBC");

            LoadKeyData("General", "NetworkMethod",parsedData, ref General_NetworkMethod);
			LoadKeyData("General", "E3NetworkAddPathToMonitor", parsedData,General_E3NetworkAddPathToMonitor);
			LoadKeyData("General", "Network Default Broadcast (Group,All,AllInZoneOrRaid)", parsedData, ref General_BroadCast_Default);
            LoadKeyData("General", "Heal While Navigating (On/Off)", parsedData, ref General_HealWhileNavigating);
            LoadKeyData("General", "Beep Notifications (On/Off)", parsedData, ref General_BeepNotifications);

            LoadKeyData("Discord Bot", "Token", parsedData, ref DiscordBotToken);
            LoadKeyData("Discord Bot", "Guild Channel ID", parsedData, ref DiscordGuildChannelId);
            LoadKeyData("Discord Bot", "Server ID", parsedData, ref DiscordServerId);
            LoadKeyData("Discord Bot", "My Discord User ID", parsedData, ref DiscordMyUserId);


            LoadKeyData("Misc", "Automatically Use Misfit Box (On/Off)", parsedData, ref AutoMisfitBox);
            LoadKeyData("Misc", "Turn Player Attack Off During Enrage (On/Off)", parsedData, ref AttackOffOnEnrage);
            LoadKeyData("Misc", "Relay Tells (On/Off)", parsedData, ref RelayTells);

            LoadKeyData("Loot", "Loot Link Channel", parsedData, ref Loot_LinkChannel);
            //check valid loot channels
            if (!Loot_LinkChannelValid.Contains(Loot_LinkChannel, StringComparer.OrdinalIgnoreCase))
            {
                MQ.Write("Invalid Loot Link Channel setting, loot will not be reported");
                Loot_LinkChannel = String.Empty;
            }
          
            LoadKeyData("Loot", "Corpse Seek Radius", parsedData, ref Loot_CorpseSeekRadius);
            LoadKeyData("Loot", "LootItemDelay", parsedData, ref Loot_LootItemDelay);
            //no lower than 300ms
            if (Loot_LootItemDelay < 300)
            {
                MQ.Write("Minimum LootItemDelay is 300ms, defaulting to 300ms");
                Loot_LootItemDelay = 300;
            }

            LoadKeyData("Loot", "Loot in Combat", parsedData, ref Loot_LootInCombat);
            LoadKeyData("Loot", "Milliseconds To Wait To Loot", parsedData, ref Loot_TimeToWaitAfterAssist);
            LoadKeyData("Loot", "NumOfFreeSlotsOpen(1+)", parsedData, ref Loot_NumberOfFreeSlotsOpen);
            LoadKeyData("Loot", "Loot Only Stackable: Enable (On/Off)", parsedData, ref Loot_OnlyStackableEnabled);
            LoadKeyData("Loot", "Loot Only Stackable: With Value Greater Than Or Equal in Copper", parsedData, ref Loot_OnlyStackableValueGreaterThanInCopper);
            LoadKeyData("Loot", "Loot Only Stackable: Loot all Tradeskill items (On/Off)", parsedData, ref Loot_OnlyStackableAllTradeSkillItems);
            LoadKeyData("Loot", "Loot Only Stackable: Loot common tradeskill items ie:pelts ores silks etc (On/Off)", parsedData, ref Loot_OnlyStackableOnlyCommonTradeSkillItems);
            LoadKeyData("Loot", "Loot Only Stackable: Always Loot Item", parsedData, Loot_OnlyStackableAlwaysLoot);
            LoadKeyData("Loot", "Loot Only Stackable: Honor Loot File Skip Settings (On/Off)", parsedData, ref Loot_OnlyStackableHonorLootFileSkips);
        
            LoadKeyData("Manastone", "NumerOfClicksPerLoop", parsedData, ref ManaStone_NumerOfClicksPerLoop);
            LoadKeyData("Manastone", "NumberOfLoops", parsedData, ref ManaStone_NumberOfLoops);
            LoadKeyData("Manastone", "DelayBetweenLoops (in milliseconds)", parsedData, ref ManaStone_DelayBetweenLoops);
            LoadKeyData("Manastone", "In Combat MinMana", parsedData, ref ManaStone_InCombatMinMana);
            LoadKeyData("Manastone", "In Combat MaxMana", parsedData, ref ManaStone_InCombatMaxMana);
            LoadKeyData("Manastone", "Use In Combat", parsedData, ref ManaStone_EnabledInCombat);
            LoadKeyData("Manastone", "Min HP", parsedData, ref ManaStone_MinHP);
            LoadKeyData("Manastone", "Out of Combat MinMana", parsedData, ref ManaStone_OutOfCombatMinMana);
            LoadKeyData("Manastone", "Out of Combat MaxMana", parsedData, ref ManaStone_OutOfCombatMaxMana);

            CheckManastoneValues();

            //so we can validate a default gem is always acceptable range
            Int32 spellGem = Casting_DefaultSpellGem;
            LoadKeyData("Casting", "Default Spell Gem", parsedData, ref spellGem);
            Casting_DefaultSpellGem = spellGem;
            
            LoadKeyData("Buff Requests", "Allow Buff Requests (On/Off)", parsedData, ref BuffRequests_AllowBuffRequests);
            LoadKeyData("Buff Requests", "Restricted PCs (When Requests [On])", parsedData, ref BuffRequests_RestrictedPCs);
            LoadKeyData("Buff Requests", "Allowed PCs (When Requests [Off])", parsedData, ref BuffRequests_AllowedPcs);

            LoadKeyData("Assists", "Auto-Assist (On/Off)", parsedData, ref Assists_AutoAssistEnabled);

            LoadKeyData("Assists", "Max Engage Distance", parsedData, ref Assists_MaxEngagedDistance);
            LoadKeyData("Assists", "AE Threat Range", parsedData, ref Assists_AEThreatRange);
            CheckAssistValues();

            LoadKeyData("AutoTrade", "Active Window Wait for Trade Accept (On/Off)", parsedData, ref AutoTrade_WaitForTrade);
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
            LoadKeyData("Movement", "Milliseconds till standing Still",parsedData,ref Movement_StandingStill);
            CheckMovementValues();
        }

        private void CheckAssistValues()
        {
            if (Assists_AEThreatRange < 10)
            {
                Assists_AEThreatRange = 10;
                MQ.Write("AE Threat Range can't be less than 10, defaulting to 10 units");
            }
            if (Assists_AEThreatRange > 300)
            {
                Assists_AEThreatRange = 300;
                MQ.Write("AE Threat Range can't be more than 300, defaulting to 300 units");
            }
        }
        private void CheckMovementValues()
        {
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
        private void CheckManastoneValues()
        {

            if (ManaStone_DelayBetweenLoops > 1000 || ManaStone_DelayBetweenLoops < 1)
            {
                MQ.Write("Manastone DelayBetweenLoops has to be < 1000 and >1, defaulting to 50ms");
                ManaStone_DelayBetweenLoops = 50;
            }
            if (ManaStone_MinHP < 15 || ManaStone_MinHP > 100)
            {
                MQ.Write("Manastone ManaStone_MinHP has to be < 100 and >15, defaulting to 60");
                ManaStone_MinHP = 60;
            }
            if (ManaStone_OutOfCombatMinMana < 15 || ManaStone_OutOfCombatMinMana > 100)
            {
                MQ.Write("Manastone ManaStone_OutOfCombatMinMana has to be >15, defaulting to 85");
                ManaStone_OutOfCombatMinMana = 85;
            }
            if (ManaStone_OutOfCombatMaxMana < 15 || ManaStone_OutOfCombatMaxMana > 100)
            {
                MQ.Write("Manastone ManaStone_OutOfCombatMaxMana has to be < 100 and >15, defaulting to 95");
                ManaStone_DelayBetweenLoops = 95;
            }

            if (ManaStone_InCombatMinMana < 15 || ManaStone_InCombatMinMana > 100)
            {
                MQ.Write("Manastone ManaStone_InCombatMinMana has to be >15, defaulting to 85");
                ManaStone_InCombatMinMana = 40;
            }
            if (ManaStone_InCombatMaxMana < 15 || ManaStone_InCombatMaxMana > 100)
            {
                MQ.Write("Manastone ManaStone_InCombatMaxMana has to be < 100 and >15, defaulting to 75");
                ManaStone_DelayBetweenLoops = 75;
            }
        }
        public IniData CreateSettings(string filename)
        {

            IniParser.FileIniDataParser parser = e3util.CreateIniParser();
            IniData newFile = new IniData();


            newFile.Sections.AddSection("General");
            var section = newFile.Sections.GetSectionData("General");
            section.Keys.AddKey("AutoMedBreak PctMana", "0");
            section.Keys.AddKey("NetworkMethod", "EQBC");
            section.Keys.AddKey("E3NetworkAddPathToMonitor", "");
            section.Keys.AddKey("Network Default Broadcast (Group,All,AllInZoneOrRaid)", "Group");

            section.Keys.AddKey("Heal While Navigating (On/Off)","On");
            section.Keys.AddKey("Beep Notifications (On/Off)", "On");

            newFile.Sections.AddSection("Discord Bot");
            section = newFile.Sections.GetSectionData("Discord Bot");
            section.Keys.AddKey("Token", "");
            section.Keys.AddKey("Guild Channel ID", "");
            section.Keys.AddKey("Server ID", "");
            section.Keys.AddKey("My Discord User ID", "");
            
            //Misc
            newFile.Sections.AddSection("Misc");
            section = newFile.Sections.GetSectionData("Misc");
            section.Keys.AddKey("Automatically Use Misfit Box (On/Off)", "Off");
            section.Keys.AddKey("Turn Player Attack Off During Enrage (On/Off)", "On");
            section.Keys.AddKey("Relay Tells (On/Off)", "Off");
            //Loot
            newFile.Sections.AddSection("Loot");
            section = newFile.Sections.GetSectionData("Loot");
            section.Keys.AddKey("Loot Link Channel","say");
	        section.Keys.AddKey("Corpse Seek Radius","125");
            section.Keys.AddKey("Loot in Combat","TRUE");
            section.Keys.AddKey("Milliseconds To Wait To Loot", "2000");
            section.Keys.AddKey("NumOfFreeSlotsOpen(1+)","0");
            section.Keys.AddKey("Loot Only Stackable: Enable (On/Off)", "Off");
            section.Keys.AddKey("Loot Only Stackable: With Value Greater Than Or Equal in Copper", "10000");
            section.Keys.AddKey("Loot Only Stackable: Loot common tradeskill items ie:pelts ores silks etc (On/Off)", "Off");
            section.Keys.AddKey("Loot Only Stackable: Loot all Tradeskill items (On/Off)", "Off");
            section.Keys.AddKey("Loot Only Stackable: Honor Loot File Skip Settings (On/Off)", "Off");
            section.Keys.AddKey("LootItemDelay", "300");

            //Manastone
            newFile.Sections.AddSection("Manastone");
            section = newFile.Sections.GetSectionData("Manastone");
            section.Keys.AddKey("NumerOfClicksPerLoop", "40");
            section.Keys.AddKey("NumberOfLoops", "25");
            section.Keys.AddKey("DelayBetweenLoops (in milliseconds)", "50");
            section.Keys.AddKey("In Combat MinMana", "40");
            section.Keys.AddKey("In Combat MaxMana", "75");
            section.Keys.AddKey("Use In Combat", "On");
            section.Keys.AddKey("Min HP", "60");
            section.Keys.AddKey("Out of Combat MinMana", "85");
            section.Keys.AddKey("Out of Combat MaxMana", "95");
          
            //Casting
            newFile.Sections.AddSection("Casting");
            section = newFile.Sections.GetSectionData("Casting");
            section.Keys.AddKey("Default Spell Gem","8");

            //Buff Requests
            newFile.Sections.AddSection("Buff Requests");
            section = newFile.Sections.GetSectionData("Buff Requests");
            section.Keys.AddKey("Allow Buff Requests (On/Off)","On");
            section.Keys.AddKey("Restricted PCs (When Requests [On])","");
            section.Keys.AddKey("Allowed PCs (When Requests [Off])","");


            //Assists
            newFile.Sections.AddSection("Assists");
            section = newFile.Sections.GetSectionData("Assists");
            section.Keys.AddKey("Auto-Assist (On/Off)", "Off");
            section.Keys.AddKey("Max Engage Distance", "250");
            section.Keys.AddKey("AE Threat Range", "100");
            

            //Trade
            newFile.Sections.AddSection("AutoTrade");
            section = newFile.Sections.GetSectionData("AutoTrade");
            section.Keys.AddKey("Active Window Wait for Trade Accept (On/Off)", "On");
            section.Keys.AddKey("All (On/Off)", "Off");
            section.Keys.AddKey("Bots (On/Off)", "On");
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
            section.Keys.AddKey("Milliseconds till standing Still", "10000");

           
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
    public enum DefaultBroadcast
    {
        Group,
        All,
        AllInZoneOrRaid

    }
}
