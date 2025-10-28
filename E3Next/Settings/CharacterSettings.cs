﻿using E3Core.Data;
using E3Core.Processors;
using E3Core.Utility;
using IniParser;
using IniParser.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using static System.Collections.Specialized.BitVector32;
using MonoCore;

namespace E3Core.Settings
{

    public class INI_SectionAttribute : Attribute
    {
        private string _header;
        private string _key;

        public string Header
        {
            get { return _header; }
            set { _header = value; }
        }
        public string Key
        {
            get { return _key; }
            set { _key = value; }
        }

        public INI_SectionAttribute(string header, string key)
        {
            _header = header;
            _key = key;
        }

    }
	public class INI_Section2Attribute : Attribute
	{
		private string _header;
		private string _key;

		public string Header
		{
			get { return _header; }
			set { _header = value; }
		}
		public string Key
		{
			get { return _key; }
			set { _key = value; }
		}

		public INI_Section2Attribute(string header, string key)
		{
			_header = header;
			_key = key;
		}

	}
	//update all peg to laz
	//get-childitem *_PEQTGC.ini | rename-item -newname {$_.name -replace '_PEQTGC.ini','_Lazarus.ini' }    
	/// <summary>
	/// Settings specific to the current character
	/// </summary>
	/// <seealso cref="BaseSettings" />
	/// <seealso cref="IBaseSettings" />
	/// 
	public class CharacterSettings : BaseSettings, IBaseSettings
    {
		//the reflection lookup so that we can expose all settings data for custom looksup
		//under the ${E3N.Settings.HEADER.KEY}
		public Dictionary<string, FieldInfo> SettingsReflectionLookup = new Dictionary<string, FieldInfo>();

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
		public IniData ParsedData;
        private readonly string CharacterName;
        private readonly string ServerName;
        private readonly Class CharacterClass;

		[INI_Section("Misc", "AutoFood")]
		public bool Misc_AutoFoodEnabled;
		[INI_Section("Misc", "Dismount On Interrupt (On/Off)")]
		public bool Misc_DismountOnInterrupt = true;
        [INI_Section("Misc", "Food")]
        public string Misc_AutoFood = String.Empty;
		[INI_Section("Misc", "Drink")]
		public string Misc_AutoDrink = String.Empty;
		[INI_Section("Misc", "End MedBreak in Combat(On/Off)")]
		public bool Misc_EndMedBreakInCombat;
		[INI_Section("Misc", "AutoMedBreak (On/Off)")]
		public bool Misc_AutoMedBreak;
		[INI_Section("Misc", "Auto-Loot (On/Off)")]
		public bool Misc_AutoLootEnabled;
		[INI_Section("Misc", "Debuffs/Dots are visible")]
		public bool Misc_VisibleDebuffsDots=true;
		[INI_Section("Misc", "Enhanced rotation speed")]
		public bool Misc_EnchancedRotationSpeed = false;


		[INI_Section("Misc", "Anchor (Char to Anchor to)")]
		public string Misc_AnchorChar = String.Empty;
		[INI_Section("Misc", "Remove Torpor After Combat")]
		public bool Misc_RemoveTorporAfterCombat = true;
		[INI_Section("Misc", "Delay in MS After CastWindow Drops For Spell Completion")]
		public Int32 Misc_DelayAfterCastWindowDropsForSpellCompletion = 0;


		[INI_Section("Misc", "Auto-Forage (On/Off)")]
		public bool Misc_AutoForage = false;

		[INI_Section("AutoMed", "Override Old Settings and use This(On/Off)")]
		public bool AutoMed_OverrideOldSettings;
		[INI_Section("AutoMed", "End MedBreak in Combat(On/Off)")]
		public bool AutoMed_EndMedBreakInCombat;
		[INI_Section("AutoMed", "AutoMedBreak (On/Off)")]
		public bool AutoMed_AutoMedBreak;
		[INI_Section("AutoMed", "PctMana")]
		public Int32 AutoMed_AutoMedBreakPctMana;
		[INI_Section("AutoMed", "PctStam")]
		public Int32 AutoMed_AutoMedBreakPctStam;
		[INI_Section("AutoMed", "PctHealth")]
		public Int32 AutoMed_AutoMedBreakPctHealth;

		[INI_Section("Rogue", "Auto-Hide (On/Off)")]
		public bool Rogue_AutoHide = false;
		[INI_Section("Rogue", "Auto-Evade (On/Off)")]
		public bool Rogue_AutoEvade = false;
		[INI_Section("Rogue", "Evade PctAggro")]
		public int Rogue_EvadePct = 0;
		[INI_Section("Rogue", "PoisonPR")]
		public string Rogue_PoisonPR = string.Empty;
		[INI_Section("Rogue", "PoisonFR")]
		public string Rogue_PoisonFR = string.Empty;
		[INI_Section("Rogue", "PoisonCR")]
		public string Rogue_PoisonCR = string.Empty;
		[INI_Section("Rogue", "Sneak Attack Discipline")]
		public string Rogue_SneakAttack = string.Empty;
		[INI_Section("Bard", "MelodyIf")]
		public List<MelodyIfs> Bard_MelodyIfs = new List<MelodyIfs>();
		[INI_Section("Bard", "DynamicMelodySets")]
		public SortedDictionary<string, List<Spell>> Bard_MelodySets = new SortedDictionary<string, List<Spell>>();
		[INI_Section("Bard", "AutoMezSong")]
		public List<Spell> Bard_AutoMezSong = new List<Spell>();
		[INI_Section("Bard", "AutoMezSongDuration in seconds")]
		public Int32 Bard_AutoMezSongDuration = 18;


		[INI_Section("Druid", "Evac Spell")]
		[INI_Section2("Wizard", "Evac Spell")]
		public List<Spell> CasterEvacs = new List<Spell>();

		[INI_Section("E3ChatChannelsToJoin", "Channel")]
		public List<string> E3ChatChannelsToJoinRaw = new List<string>();
		public List<string> E3ChatChannelsToJoin = new List<string>();
		
        [INI_Section("Druid", "Auto-Cheetah (On/Off)")]
		public bool Druid_AutoCheetah = true;
		[INI_Section("Bard", "Auto-Sonata (On/Off)")]
		public bool Bard_AutoSonata = true;

	[INI_Section("Assist Settings", "Assist Type (Melee/AutoAttack/Ranged/AutoFire/Off)")]
	public string Assist_Type = string.Empty;
		[INI_Section("Assist Settings", "Melee Stick Point")]
		public string Assist_MeleeStickPoint = string.Empty;
		[INI_Section("Assist Settings", "Taunt(On/Off)")]
		public bool Assist_TauntEnabled = false;
		[INI_Section("Assist Settings", "SmartTaunt(On/Off)")]
		public bool Assist_SmartTaunt = false;
		[INI_Section("Assist Settings", "Melee Distance")]
		public string Assist_MeleeDistance = "MaxMelee";
		[INI_Section("Assist Settings", "Ranged Distance")]
		public string Assist_RangeDistance = "100";
		[INI_Section("Assist Settings", "Auto-Assist Engage Percent")]
		public int Assist_AutoAssistPercent = 98;
		[INI_Section("Assist Settings", "Delayed Strafe Enabled (On/Off)")]
		public bool Assist_DelayStrafeEnabled = true;
		[INI_Section("Assist Settings", "CommandOnAssist")]
		public string Assist_CommandOnAssist = String.Empty;


		//not explosed for some reason?
		public Int32 Assist_DelayStrafeDelay = 1500;
       
		[INI_Section("Assist Settings", "Pet back off on Enrage (On/Off)")]
		public bool Assist_PetBackOffOnenrage = false;
		[INI_Section("Assist Settings", "Back off on Enrage (On/Off)")]
		public bool Assist_BackOffOnEnrage = false;

		[INI_Section("Melee Abilities", "Ability")]
		public List<Spell> MeleeAbilities = new List<Spell>();
        
        [INI_Section("Nukes","Main")]
        public List<Spell> Nukes = new List<Spell>();
        
        [INI_Section("Stuns", "Main")]
		public List<Spell> Stuns = new List<Spell>();
	    
        [INI_Section("Dispel","Main")]
		public List<Spell> Dispels = new List<Spell>();
		
        [INI_Section("Dispel", "Ignore")]
		public List<Spell> DispelIgnore = new List<Spell>();

		[INI_Section("Rampage Actions", "Action")]
		public List<Spell> RampageSpells = new List<Spell>();

		[INI_Section("Buffs", "Instant Buff")]
		public List<Spell> InstantBuffs = new List<Spell>();
		[INI_Section("Buffs", "Self Buff")]
		public List<Spell> SelfBuffs = new List<Spell>();
		[INI_Section("Buffs", "Bot Buff")]
		public List<Spell> BotBuffs = new List<Spell>();
		[INI_Section("Buffs", "Group Buff")]
		public List<Spell> GroupBuffs = new List<Spell>();
		[INI_Section("Buffs", "Combat Buff")]
		public List<Spell> CombatBuffs = new List<Spell>();

		[INI_Section("Buffs", "Pet Buff")]
		public List<Spell> PetBuffs = new List<Spell>();
		[INI_Section("Pets", "Pet Buff")]
		public List<Spell> PetOwnerBuffs = new List<Spell>();

        [INI_Section("Buffs", "Combat Pet Buff")]
		public List<Spell> CombatPetBuffs = new List<Spell>();
		[INI_Section("Pets", "Combat Pet Buff")]
		public List<Spell> CombatPetOwnerBuffs = new List<Spell>();

		[INI_Section("Buffs", "Cast Aura(On/Off)")]
		public bool Buffs_CastAuras = true;
		[INI_Section("Buffs", "Aura")]
		public List<Spell> Buffs_Auras = new List<Spell>();

		[INI_Section2("Pets", "Blocked Pet Buff")]
		public List<Spell> BlockedPetBuffs = new List<Spell>();

		[INI_Section("Buffs", "Group Buff Request")]
		public List<SpellRequest> GroupBuffRequests = new List<SpellRequest>();
		[INI_Section("Buffs", "Raid Buff Request")]
		public List<SpellRequest> RaidBuffRequests = new List<SpellRequest>();
		[INI_Section("Buffs", "Stack Buff Request")]
		public List<SpellRequest> StackBuffRequest = new List<SpellRequest>();

		[INI_Section("Gimme", "Gimme")]
		public List<string> Gimme = new List<string>();
		[INI_Section("Gimme", "Gimme-NoCombat")]
		public List<string> Gimme_NoCombat = new List<string>();
		[INI_Section("Gimme", "Gimme-InCombat")]
		public bool Gimme_InCombat = true;

		[INI_Section("Pets", "Pet Spell")]
		public List<Spell> PetSpell = new List<Spell>();
		
        [INI_Section("Pets", "Pet Heal")]
		public List<Spell> PetHeals = new List<Spell>();

		[INI_Section("Pets", "Pet Mend (Pct)")]
		public int Pet_MendPercent;
		[INI_Section("Pets", "Pet Taunt (On/Off)")]
		public bool Pet_TauntEnabled;
		[INI_Section("Pets", "Pet Auto-Shrink (On/Off)")]
		public bool Pet_AutoShrink;
		[INI_Section("Pets", "Pet Summon Combat (On/Off)")]
		public bool Pet_SummonCombat;

		//Alerts
		[INI_Section("Alerts", "Damage Messages(On/Off)")]
		public bool Alerts_DamageMessages = true;
		[INI_Section("Alerts", "Rampage Messages(On/Off)")]
		public bool Alerts_RampageMessages = true;
		[INI_Section("Alerts", "Reflect Messages(On/Off)")]
		public bool Alerts_ReflectMessages = true;

		//debuffs
		[INI_Section("Debuffs", "Debuff on Assist")]
		public List<Spell> Debuffs_OnAssist = new List<Spell>();
		[INI_Section("Debuffs", "Debuff on Command")]
		public List<Spell> Debuffs_Command = new List<Spell>();

		//dots
		[INI_Section("DoTs on Command", "Main")]
		public List<Spell> Dots_OnCommand = new List<Spell>();
		[INI_Section("DoTs on Assist", "Main")]
		public List<Spell> Dots_Assist = new List<Spell>();
		//aoe
		[INI_Section("PBAE", "PBAE")]
		public List<Spell> PBAE = new List<Spell>();
		//burns
		[INI_Section("Burn","")]
		[ExposedData("Burn","")]
		public Dictionary<string, Burn> BurnCollection = new Dictionary<string, Burn>(StringComparer.OrdinalIgnoreCase);
		[INI_Section("CommandSets", "")]
		[ExposedData("CommandSets", "")]
		public Dictionary<string, CommandSet> CommandCollection = new Dictionary<string, CommandSet>(StringComparer.OrdinalIgnoreCase);
		//cures
		[INI_Section("Cures", "Cure")]
		public List<Spell> Cures = new List<Spell>();
		[INI_Section("Cures", "CureAll")]
		public List<Spell> CureAll = new List<Spell>();
		[INI_Section("Cures", "RadiantCure")]
		public List<Spell> RadiantCure = new List<Spell>();
		[INI_Section("Cures", "RadiantCureSpells")]
		public List<Spell> RadiantCureSpells = new List<Spell>();
		[INI_Section("Cures", "CurseCounters")]
		public List<Spell> CurseCounterCure = new List<Spell>();
		[INI_Section("Cures", "CurseCountersIgnore")]
		public List<Spell> CurseCounterIgnore = new List<Spell>();
		[INI_Section("Cures", "CorruptedCounters")]
		public List<Spell> CorruptedCounterCure = new List<Spell>();
		[INI_Section("Cures", "CorruptedCountersIgnore")]
		public List<Spell> CorruptedCounterIgnore = new List<Spell>();
		[INI_Section("Cures", "PoisonCounters")]
		public List<Spell> PoisonCounterCure = new List<Spell>();
		[INI_Section("Cures", "PoisonCountersIgnore")]
		public List<Spell> PoisonCounterIgnore = new List<Spell>();
		[INI_Section("Cures", "DiseaseCounters")]
		public List<Spell> DiseaseCounterCure = new List<Spell>();
		[INI_Section("Cures", "DiseaseCountersIgnore")]
		public List<Spell> DiseaseCounterIgnore = new List<Spell>();
		

		//life support
		[INI_Section("Life Support", "Life Support")]
		public List<Spell> LifeSupport = new List<Spell>();

		//blocked buffs
		[INI_Section("Blocked Buffs", "BuffName")]
		public List<Spell> BlockedBuffs = new List<Spell>();

		[INI_Section("Misc", "If FD stay down (true/false)")]
		public bool IfFDStayDown = false;

		//bando buffs
		[INI_Section("Bando Buff", "Enabled")]
		public bool BandoBuff_Enabled = false;
		[INI_Section("Bando Buff", "BuffName")]
		public string BandoBuff_BuffName = String.Empty;
		[INI_Section("Bando Buff", "DebuffName")]
		public string BandoBuff_DebuffName = String.Empty;
		[INI_Section("Bando Buff", "BandoNameWithBuff")]
		public string BandoBuff_BandoName = String.Empty;
		[INI_Section("Bando Buff", "BandoNameWithoutBuff")]
		public string BandoBuff_BandoNameWithoutBuff = String.Empty;
		[INI_Section("Bando Buff", "BandoNameWithoutDeBuff")]
		public string BandoBuff_BandoNameWithoutDeBuff = String.Empty;

		//manastone
		[INI_Section("Manastone", "Manastone Enabled (On/Off)")]
		public bool Manastone_Enabled = true;
		[INI_Section("Manastone", "Override General Settings (On/Off)")]
		public bool Manastone_OverrideGeneralSettings = false;
		[INI_Section("Manastone", "NumberOfClicksPerLoop")]
		public Int32 ManaStone_NumberOfClicksPerLoop = 40;
		[INI_Section("Manastone", "NumberOfLoops")]
		public Int32 ManaStone_NumberOfLoops = 25;
		[INI_Section("Manastone", "DelayBetweenLoops (in milliseconds)")]
		public Int32 ManaStone_DelayBetweenLoops = 50;
		[INI_Section("Manastone", "Use In Combat")]
		public bool ManaStone_EnabledInCombat = true;
		[INI_Section("Manastone", "In Combat MinMana")]
		public Int32 ManaStone_InCombatMinMana = 40;
		[INI_Section("Manastone", "In Combat MaxMana")]
		public Int32 ManaStone_InCombatMaxMana = 75;
		[INI_Section("Manastone", "Min HP")]
		public Int32 ManaStone_MinHP = 60;
		[INI_Section("Manastone", "Min HP Out of Combat")]
		public Int32 ManaStone_MinHPOutOfCombat = 40;
		[INI_Section("Manastone", "Out of Combat MinMana")]
		public Int32 ManaStone_OutOfCombatMinMana = 85;
		[INI_Section("Manastone", "Out of Combat MaxMana")]
		public Int32 ManaStone_OutOfCombatMaxMana = 95;
		[INI_Section("Manastone", "ExceptionZone")]
		public List<string> ManaStone_ExceptionZones = new List<string> {};
		[INI_Section("Manastone", "ExceptionMQQuery")]
		public List<string> ManaStone_ExceptionMQQuery = new List<string>();
		[INI_Section("Manastone", "UseForLazarusEncEpicBuff")]
		public bool ManaStone_UseForLazarusEncEpicBuff = false;
		[INI_Section("Startup Commands", "Command")]
		public List<string> StartupCommands = new List<string>();
		[INI_Section("Zoning Commands", "Command")]
		public List<string> ZoningCommands = new List<string>();
		//heals
		[INI_Section("Heals", "Tank")]
		public List<string> HealTankTargets = new List<string>();
		[INI_Section("Heals", "Tank Heal")]
		public List<Spell> HealTanks = new List<Spell>();
		[INI_Section("Heals", "Important Bot")]
		public List<string> HealImportantBotTargets = new List<string>();
		[INI_Section("Heals", "Important Heal")]
		public List<Spell> HealImportantBots = new List<Spell>();
		
		[INI_Section("Heals", "Group Heal")]
		public List<Spell> HealGroup = new List<Spell>();
		[INI_Section("Heals", "Number Of Injured Members For Group Heal")]
		public Int32 HealGroup_NumberOfInjuredMembers = 3;
		[INI_Section("Heals", "All Heal")]
		public List<Spell> HealAll = new List<Spell>();
		[INI_Section("Heals", "Party Heal")]
		public List<Spell> HealParty = new List<Spell>();
		[INI_Section("Heals", "XTarget Heal")]
		public List<Spell> HealXTarget = new List<Spell>();
		[INI_Section("Heals", "Pet Heal")]
		public List<Spell> HealPets = new List<Spell>();
		[INI_Section("Heals", "Heal Over Time Spell")]
		public List<Spell> HealOverTime = new List<Spell>();
		[INI_Section("Heals", "Pet Owner")]
		public List<string> HealPetOwners = new List<string>();
		[INI_Section("Heals", "Emergency Heal")]
		public List<Spell> Heal_EmergencyHeals = new List<Spell>();
		[INI_Section("Heals", "Emergency Group Heal")]
		public List<Spell> Heal_EmergencyGroupHeals = new List<Spell>();

		//rez spells
		[INI_Section("Rez", "Auto Rez Spells")]
		public List<Spell> Rez_AutoRezSpells = new List<Spell>();
		[INI_Section("Rez", "Rez Spells")]
		public List<Spell> Rez_RezSpells = new List<Spell>();
		[INI_Section("Rez", "AutoRez")]
		public bool Rez_AutoRez = false;

		//report
		[INI_Section("Report", "ReportEntry")]
		public List<Spell> Report_Entries = new List<Spell>();


		//E3BotsPublishData
		
		[INI_Section("E3BotsPublishData (key/value)", "")]
		public SortedDictionary<string, string> E3BotsPublishDataRaw = new SortedDictionary<string, string>();
		//used internally, the editor uses the Raw version
		//the normal collection is modified to have ${Data.KeyName} for its keys
		public SortedDictionary<string,string> E3BotsPublishData = new SortedDictionary<string,string>();
		[INI_Section("Ifs", "")]
		public SortedDictionary<string, string> Ifs = new SortedDictionary<string, string>();
		[INI_Section("Events", "")]
		public SortedDictionary<string, string> Events = new SortedDictionary<string, string>();
		[INI_Section("EventLoop", "")]
		public SortedDictionary<string, string> EventLoop = new SortedDictionary<string, string>();
		[INI_Section("EventLoopTiming", "")]
		public SortedDictionary<string, string> EventLoopTiming = new SortedDictionary<string, string>();

		[INI_Section("EventRegMatches", "")]
		public SortedDictionary<string, string> EventMatches = new SortedDictionary<string, string>();

		//charm data
		[INI_Section("Charm", "CharmSpell")]
		public List<Spell> Charm_CharmSpells = new List<Spell>();
		[INI_Section("Charm", "CharmOhShitSpells")]
		public List<Spell> Charm_CharmOhShitSpells = new List<Spell>();
		[INI_Section("Charm", "SelfDebuffSpells")]
		public List<Spell> Charm_SelfDebuffSpells = new List<Spell>();
		[INI_Section("Charm", "BadPetBuffs")]
		public List<Spell> Charm_BadPetBuffs = new List<Spell>();
		[INI_Section("Charm", "PeelTank")]
		public string Charm_PeelTank = String.Empty;
		[INI_Section("Charm", "PeelTankAggroAbility")]
		public List<Spell> Charm_PeelTankAggroAbility = new List<Spell>();
		[INI_Section("Charm", "PeelHealer")]
		public string Charm_PeelHealer = String.Empty;
		[INI_Section("Charm", "PeelHealerHeal")]
		public List<Spell> Charm_PeelHealerHeal = new List<Spell>();
		[INI_Section("Charm", "PeelPetOwner")]
		public string Charm_PeelPetOwner = String.Empty;
		[INI_Section("Charm", "PeelSnarePerson")]
		public string Charm_PeelSnarePerson = String.Empty;
		[INI_Section("Charm", "PeelSnareSpell")]
		public List<Spell> Charm_PeelSnareSpell  = new List<Spell>();
		[INI_Section("Charm", "PeelDebuffPerson")]
		public string Charm_PeelDebuffPerson = String.Empty;
		[INI_Section("Charm", "PeelDebuffSpells")]
		public List<Spell> Charm_PeelDebuffSpells = new List<Spell>();

		//

		////Loot Commander

		//public bool LootCommander_Enabled;
  //      public List<string> LootCommander_Looters = new List<string>();

		[INI_Section("CPU", "ProcessLoopDelayInMS")]
		public Int32 CPU_ProcessLoopDelay = 50;
		[INI_Section("CPU", "PublishStateDataInMS")]
		public Int32 CPU_PublishStateDataInMS = 50;
		[INI_Section("CPU", "PublishBuffDataInMS")]
		public Int32 CPU_PublishBuffDataInMS = 1000;
		[INI_Section("CPU", "PublishSlowDataInMS")]
		public Int32 CPU_PublishSlowDataInMS = 1000;
		[INI_Section("CPU", "Camp Pause at 30 seconds")]
		public bool CPU_Camping_PauseAt30Seconds = true;
		[INI_Section("CPU", "Camp Pause at 20 seconds")]
		public bool CPU_Camping_PauseAt20Seconds = true;
		[INI_Section("CPU", "Camp Shutdown at 5 seconds")]
		public bool CPU_Camping_ShutdownAt5Seconds = true;

		[INI_Section("Magician", "Pet Weapons")]
		public Dictionary<string, string> PetWeapons = new Dictionary<string, string>();
		[INI_Section("Magician", "Auto-Pet Weapons (On/Off)")]
		public bool AutoPetWeapons = false;
		[INI_Section("Magician", "Keep Open Inventory Slot (On/Off)")]
		public bool KeepOpenInventorySlot = false;
		[INI_Section("Magician", "Ignore Pet Weapon Requests (On/Off)")]
		public bool IgnorePetWeaponRequests = false;
		[INI_Section("Magician", "Allow Pet Weapon Requests from Guild Bypass(On/Off)")]
		public bool Magican_AllowPetRequestWeaponsBypass = true;
		[INI_Section("Shaman", "Auto-Canni (On/Off)")]
		public bool AutoCanni = false;

		public bool Misc_AutoJoinTasks = false;
		[INI_Section("Shaman", "Malos Totem Spell Gem")]
		public int MalosTotemSpellGem;
		[INI_Section("Shaman", "Canni")]
		public List<Spell> CanniSpell = new List<Spell>();



		[INI_Section("Auto Paragon", "Auto Paragon (On/Off)")]
		public bool AutoParagon = false;
		[INI_Section("Auto Paragon", "Paragon Spell")]
		public Spell ParagonSpell = null;
		[INI_Section("Auto Paragon", "Paragon Mana (Pct)")]
		public int ParagonManaPct = 60;
		[INI_Section("Auto Paragon", "Auto Focused Paragon (On/Off)")]
		public bool AutoFocusedParagon = false;
		[INI_Section("Auto Paragon", "Focused Paragon Spell")]
		public Spell FocusedParagonSpell = null;
		[INI_Section("Auto Paragon", "Character")]
		public List<string> FocusedParagonCharacters = new List<string>();
		[INI_Section("Auto Paragon", "Focused Paragon Mana (Pct)")]
		public int FocusedParagonManaPct = 70;


        private bool _mergeUpdates = true;

		[INI_Section("Heals", "Auto Cast Necro Heal Orbs (On/Off)")]
		public bool HealAutoNecroOrbs = false;


		public HashSet<string> WhoToHeal = new HashSet<string>(10, StringComparer.OrdinalIgnoreCase);
		[INI_Section("Heals", "Who to Heal")]
		private string _whoToHealString;
		public string WhoToHealString
        {
            get { return _whoToHealString; }
            set
            {
                _whoToHealString = value;
                List<string> returnValue = value.Split('/').ToList();
                foreach (var who in returnValue)
                {
                    if (!WhoToHeal.Contains(who))
                    {
                        WhoToHeal.Add(who);

                    }
                }
            }
        }
        public HashSet<string> WhoToHoT = new HashSet<string>(10, StringComparer.OrdinalIgnoreCase);
		[INI_Section("Heals", "Who to HoT")]
		private string _whoToHoTString;
		public string WhoToHoTString
        {
            get { return _whoToHoTString; }
            set
            {
                _whoToHoTString = value;
                List<string> returnValue = value.Split('/').ToList();
                foreach (var who in returnValue)
                {
                    if (!WhoToHoT.Contains(who))
                    {
                        WhoToHoT.Add(who);

                    }
                }
            }
        }

		//offassist
		[INI_Section("Off Assist Spells", "Main")]
		public List<Spell> OffAssistSpells = new List<Spell>();

		//clearcursor delete
		[INI_Section("Cursor Delete", "Delete")]
		public List<String> Cursor_Delete = new List<string>();


		public string _fileName = String.Empty;
		/// <summary>
		/// Initializes a new instance of the <see cref="CharacterSettings"/> class.
		/// </summary>
		public CharacterSettings(bool mergeUpdates = true)
        {
            _mergeUpdates = mergeUpdates;
            CharacterName = E3.CurrentName;
            ServerName = E3.ServerName;
            CharacterClass = E3.CurrentClass;
			MQ = E3.MQ;
            LoadData();
			//map everything to the dictionary for settings lookup. 
			GetSettingsMappedToDictionary();

		}


        /// <summary>
        /// Loads the data.
        /// </summary>
        private void LoadData()
        {

			//this is so we can get the merged data as well. 
			string filename = GetBoTFilePath(CharacterName, ServerName, CharacterClass.ToString());

			//if this is a global file with multiple writers, don't merge data as multiple writers for a single file = bad mojo.
			if (filename.StartsWith("_")) _mergeUpdates = false;

			MQ.Write($"Loading settings file: [{filename}]");
            ParsedData = CreateSettings(filename);


            LoadKeyData("CPU", "ProcessLoopDelayInMS", ParsedData, ref CPU_ProcessLoopDelay);
			LoadKeyData("CPU", "PublishStateDataInMS", ParsedData, ref CPU_PublishStateDataInMS);
			LoadKeyData("CPU", "PublishBuffDataInMS", ParsedData, ref CPU_PublishBuffDataInMS);
			LoadKeyData("CPU", "PublishSlowDataInMS", ParsedData, ref CPU_PublishSlowDataInMS);

			LoadKeyData("CPU", "Camp Pause at 30 seconds", ParsedData, ref CPU_Camping_PauseAt30Seconds);
			LoadKeyData("CPU", "Camp Pause at 20 seconds", ParsedData, ref CPU_Camping_PauseAt20Seconds);
			LoadKeyData("CPU", "Camp Shutdown at 5 seconds", ParsedData, ref CPU_Camping_ShutdownAt5Seconds);

			LoadKeyData("Misc", "AutoFood", ParsedData, ref Misc_AutoFoodEnabled);
            LoadKeyData("Misc", "Food", ParsedData, ref Misc_AutoFood);
            LoadKeyData("Misc", "Drink", ParsedData, ref Misc_AutoDrink);
            LoadKeyData("Misc", "End MedBreak in Combat(On/Off)", ParsedData, ref Misc_EndMedBreakInCombat);
            LoadKeyData("Misc", "AutoMedBreak (On/Off)", ParsedData, ref Misc_AutoMedBreak);
            LoadKeyData("Misc", "Auto-Loot (On/Off)", ParsedData, ref Misc_AutoLootEnabled);
            LoadKeyData("Misc", "Anchor (Char to Anchor to)", ParsedData, ref Misc_AnchorChar);
            LoadKeyData("Misc", "Remove Torpor After Combat", ParsedData, ref Misc_RemoveTorporAfterCombat);
            LoadKeyData("Misc", "Auto-Forage (On/Off)", ParsedData, ref Misc_AutoForage);
            LoadKeyData("Misc", "Dismount On Interrupt (On/Off)", ParsedData, ref Misc_DismountOnInterrupt);
            LoadKeyData("Misc", "Delay in MS After CastWindow Drops For Spell Completion",ParsedData, ref Misc_DelayAfterCastWindowDropsForSpellCompletion);
			LoadKeyData("Misc", "If FD stay down (true/false)", ParsedData, ref IfFDStayDown);
			LoadKeyData("Misc", "Debuffs/Dots are visible", ParsedData, ref Misc_VisibleDebuffsDots);
			LoadKeyData("Misc", "Enhanced rotation speed", ParsedData, ref Misc_EnchancedRotationSpeed);

			LoadKeyData("Alerts", "Rampage Messages(On/Off)", ParsedData, ref Alerts_RampageMessages);
			LoadKeyData("Alerts", "Damage Messages(On/Off)", ParsedData, ref Alerts_DamageMessages);
			LoadKeyData("Alerts", "Reflect Messages(On/Off)", ParsedData, ref Alerts_ReflectMessages);


			LoadKeyData("Manastone", "Override General Settings (On/Off)", ParsedData, ref Manastone_OverrideGeneralSettings);
            LoadKeyData("Manastone", "Manastone Enabled (On/Off)", ParsedData, ref Manastone_Enabled);

            LoadKeyData("Manastone", "NumberOfClicksPerLoop", ParsedData, ref ManaStone_NumberOfClicksPerLoop);
            LoadKeyData("Manastone", "NumberOfLoops", ParsedData, ref ManaStone_NumberOfLoops);
            LoadKeyData("Manastone", "DelayBetweenLoops (in milliseconds)", ParsedData, ref ManaStone_DelayBetweenLoops);
            LoadKeyData("Manastone", "In Combat MinMana", ParsedData, ref ManaStone_InCombatMinMana);
            LoadKeyData("Manastone", "In Combat MaxMana", ParsedData, ref ManaStone_InCombatMaxMana);
            LoadKeyData("Manastone", "Use In Combat", ParsedData, ref ManaStone_EnabledInCombat);
            LoadKeyData("Manastone", "Min HP", ParsedData, ref ManaStone_MinHP);
			LoadKeyData("Manastone", "Min HP Out of Combat", ParsedData, ref ManaStone_MinHPOutOfCombat);
			LoadKeyData("Manastone", "Out of Combat MinMana", ParsedData, ref ManaStone_OutOfCombatMinMana);
            LoadKeyData("Manastone", "Out of Combat MaxMana", ParsedData, ref ManaStone_OutOfCombatMaxMana);
            List<string> zoneList = new List<string>();
            LoadKeyData("Manastone", "ExceptionZone", ParsedData, zoneList);

            foreach(var zone in zoneList)
            {
                if(!ManaStone_ExceptionZones.Contains(zone))
                {
					ManaStone_ExceptionZones.Add(zone);
				}
			}

			LoadKeyData("Manastone", "ExceptionMQQuery", ParsedData, ManaStone_ExceptionMQQuery);
			LoadKeyData("Manastone", "UseForLazarusEncEpicBuff", ParsedData, ref ManaStone_UseForLazarusEncEpicBuff);

			LoadKeyData("Rampage Actions", "Action", ParsedData, RampageSpells);
		

			LoadKeyData("Report", "ReportEntry", ParsedData, Report_Entries);


			LoadKeyData("AutoMed", "Override Old Settings and use This(On/Off)", ParsedData, ref AutoMed_OverrideOldSettings);
			LoadKeyData("AutoMed", "AutoMedBreak (On/Off)", ParsedData, ref AutoMed_AutoMedBreak);
			LoadKeyData("AutoMed", "End MedBreak in Combat(On/Off)", ParsedData, ref AutoMed_EndMedBreakInCombat);
			LoadKeyData("AutoMed", "PctMana", ParsedData, ref AutoMed_AutoMedBreakPctMana);
			LoadKeyData("AutoMed", "PctStam", ParsedData, ref AutoMed_AutoMedBreakPctStam);
			LoadKeyData("AutoMed", "PctHealth", ParsedData, ref AutoMed_AutoMedBreakPctHealth);


			LoadKeyData("Bando Buff", "Enabled", ParsedData, ref BandoBuff_Enabled);
			LoadKeyData("Bando Buff", "DebuffName", ParsedData, ref BandoBuff_DebuffName);
			LoadKeyData("Bando Buff", "BuffName", ParsedData, ref BandoBuff_BuffName);
			LoadKeyData("Bando Buff", "BandoNameWithBuff", ParsedData, ref BandoBuff_BandoName);
			LoadKeyData("Bando Buff", "BandoNameWithoutBuff", ParsedData, ref BandoBuff_BandoNameWithoutBuff);
			LoadKeyData("Bando Buff", "BandoNameWithoutDeBuff", ParsedData, ref BandoBuff_BandoNameWithoutDeBuff);

			LoadKeyData("Assist Settings", "Assist Type (Melee/Ranged/Off)", ParsedData, ref Assist_Type);
            LoadKeyData("Assist Settings", "Melee Stick Point", ParsedData, ref Assist_MeleeStickPoint);
            LoadKeyData("Assist Settings", "Taunt(On/Off)", ParsedData, ref Assist_TauntEnabled);
            LoadKeyData("Assist Settings", "SmartTaunt(On/Off)", ParsedData, ref Assist_SmartTaunt);
            LoadKeyData("Assist Settings", "Melee Distance", ParsedData, ref Assist_MeleeDistance);
            LoadKeyData("Assist Settings", "Ranged Distance", ParsedData, ref Assist_RangeDistance);
            LoadKeyData("Assist Settings", "Auto-Assist Engage Percent", ParsedData, ref Assist_AutoAssistPercent);
			LoadKeyData("Assist Settings", "Delayed Strafe Enabled (On/Off)", ParsedData, ref Assist_DelayStrafeEnabled);
			LoadKeyData("Assist Settings", "CommandOnAssist", ParsedData, ref Assist_CommandOnAssist);

			LoadKeyData("Assist Settings", "Pet back off on Enrage (On/Off)", ParsedData, ref Assist_PetBackOffOnenrage);
			LoadKeyData("Assist Settings", "Back off on Enrage (On/Off)", ParsedData, ref Assist_BackOffOnEnrage);
           

			if (CharacterClass == Class.Rogue)
            {
                LoadKeyData("Rogue", "Auto-Hide (On/Off)", ParsedData, ref Rogue_AutoHide);
                LoadKeyData("Rogue", "Auto-Evade (On/Off)", ParsedData, ref Rogue_AutoEvade);
                LoadKeyData("Rogue", "Evade PctAggro", ParsedData, ref Rogue_EvadePct);
                LoadKeyData("Rogue", "Sneak Attack Discipline", ParsedData, ref Rogue_SneakAttack);
                LoadKeyData("Rogue", "PoisonPR", ParsedData, ref Rogue_PoisonPR);
                LoadKeyData("Rogue", "PoisonCR", ParsedData, ref Rogue_PoisonCR);
                LoadKeyData("Rogue", "PoisonFR", ParsedData, ref Rogue_PoisonFR);
            }

            if (CharacterClass == Class.Bard)
            {
                LoadKeyData("Bard", "MelodyIf", ParsedData, Bard_MelodyIfs);
                LoadKeyData("Bard", "AutoMezSong", ParsedData, Bard_AutoMezSong);
				LoadKeyData("Bard", "AutoMezSongDuration in seconds", ParsedData,ref Bard_AutoMezSongDuration);
                LoadKeyData("Bard", "Auto-Sonata (On/Off)", ParsedData, ref Bard_AutoSonata);
				//load up all melody sets

				Bard_MelodySets= LoadMeldoySetData(ParsedData);
				

            }

            if ((CharacterClass & Class.Druid) == CharacterClass)
            {
                LoadKeyData("Druid", "Evac Spell", ParsedData, CasterEvacs);
                LoadKeyData("Druid", "Auto-Cheetah (On/Off)", ParsedData, ref Druid_AutoCheetah);
            }

            if ((CharacterClass & Class.Wizard) == CharacterClass)
            {
                LoadKeyData("Wizard", "Evac Spell", ParsedData, CasterEvacs);
            }
            
            if (CharacterClass == Class.Magician)
            {
                LoadKeyData("Magician", "Auto-Pet Weapons (On/Off)", ParsedData, ref AutoPetWeapons);
                LoadKeyData("Magician", "Keep Open Inventory Slot (On/Off)", ParsedData, ref KeepOpenInventorySlot);
                LoadKeyData("Magician", "Ignore Pet Weapon Requests (On/Off)", ParsedData, ref IgnorePetWeaponRequests);
				LoadKeyData("Magician", "Allow Pet Weapon Requests from Guild Bypass(On/Off)", ParsedData, ref Magican_AllowPetRequestWeaponsBypass);
                LoadKeyData("Magician", "Pet Weapons", ParsedData, PetWeapons);
            }

            if (CharacterClass == Class.Shaman)
            {
                LoadKeyData("Shaman", "Auto-Canni (On/Off)", ParsedData, ref AutoCanni);
                LoadKeyData("Shaman", "Canni", ParsedData, CanniSpell);
                LoadKeyData("Shaman", "Malos Totem Spell Gem", ParsedData, ref MalosTotemSpellGem);
            }

            if (CharacterClass == Class.Beastlord)
            {
                LoadKeyData("Auto Paragon", "Auto Paragon (On/Off)", ParsedData, ref AutoParagon);
                LoadKeyData("Auto Paragon", "Paragon Spell", ParsedData, out ParagonSpell);
                LoadKeyData("Auto Paragon", "Paragon Mana (Pct)", ParsedData, ref ParagonManaPct);
                LoadKeyData("Auto Paragon", "Auto Focused Paragon (On/Off)", ParsedData, ref AutoFocusedParagon);
                LoadKeyData("Auto Paragon", "Focused Paragon Spell", ParsedData, out FocusedParagonSpell);
                LoadKeyData("Auto Paragon", "Focused Paragon Mana (Pct)", ParsedData, ref FocusedParagonManaPct);
                LoadKeyData("Auto Paragon", "Character", ParsedData, FocusedParagonCharacters);
            }

         
            LoadKeyData("E3BotsPublishData (key/value)", ParsedData, E3BotsPublishDataRaw);
            
            //now we need to change the keys to be in a specific format
            foreach(var pair in E3BotsPublishDataRaw)
            {
                string key = "${Data." + pair.Key + "}";
                if(!E3BotsPublishData.ContainsKey(key)) 
                {
					E3BotsPublishData.Add("${Data." + pair.Key + "}", pair.Value);
				}

				
            }
            
            LoadKeyData("Ifs", ParsedData, Ifs);
			LoadKeyData("Events", ParsedData, Events);
			LoadKeyData("EventLoop", ParsedData, EventLoop);
			LoadKeyData("EventLoopTiming", ParsedData, EventLoopTiming);
			LoadKeyData("EventRegMatches", ParsedData, EventMatches);

			//clear any events that were already registered
			EventProcessor.ClearDynamicEvents();
			foreach (var regexMatchPair in EventMatches)
			{
				var key = regexMatchPair.Key;
				var regex = regexMatchPair.Value;
				if (!String.IsNullOrWhiteSpace(regex))
				{
					if (Events.ContainsKey(key))
					{
						var eventToExecute = Events[key];
						if (!String.IsNullOrWhiteSpace(eventToExecute))
						{	
							EventProcessor.RegisterDynamicEvent(key, regex, (x) => {

								string tempEvent = eventToExecute;
								if(x.match.Groups.Count>1)
								{
									for(Int32 i =1;i<x.match.Groups.Count;i++)
									{
										var matchValue = x.match.Groups[i].Value;
										tempEvent=tempEvent.Replace($"${i}", matchValue);
									}
								}
								tempEvent=Casting.Ifs_Results(tempEvent);
								MQ.Cmd($"/docommand {tempEvent}",true);
							});
						}
					}
				}
			}
	
			LoadKeyData("E3ChatChannelsToJoin", "Channel", ParsedData, E3ChatChannelsToJoinRaw);
			foreach (var value in E3ChatChannelsToJoinRaw)
			{
				string key = $"${{DataChannel.{value.Trim()}}}";
				if (!E3ChatChannelsToJoin.Contains(key))
				{
					E3ChatChannelsToJoin.Add(key);
				}
			}


			LoadKeyData("Buffs", "Instant Buff", ParsedData, InstantBuffs);

			foreach (var buff in InstantBuffs) buff.IsBuff = true;
			

			LoadKeyData("Buffs", "Self Buff", ParsedData, SelfBuffs);
            //set target on self buffs
            foreach (var buff in SelfBuffs)
            {
                buff.CastTarget = CharacterName;
				buff.IsBuff = true;
			}

            LoadKeyData("Buffs", "Bot Buff", ParsedData, BotBuffs);
			foreach (var buff in BotBuffs) buff.IsBuff = true;
			LoadKeyData("Buffs", "Combat Buff", ParsedData, CombatBuffs);
			foreach (var buff in CombatBuffs) buff.IsBuff = true;
			LoadKeyData("Buffs", "Group Buff", ParsedData, GroupBuffs);
			foreach (var buff in GroupBuffs) buff.IsBuff = true;
			LoadKeyData("Buffs", "Pet Buff", ParsedData, PetBuffs);
			foreach (var buff in PetBuffs) buff.IsBuff = true;
			LoadKeyData("Buffs", "Combat Pet Buff", ParsedData, CombatPetBuffs);
			foreach (var buff in CombatPetBuffs) buff.IsBuff = true;
			LoadKeyData("Buffs", "Group Buff Request", ParsedData, GroupBuffRequests);
			foreach (var buff in GroupBuffRequests) buff.IsBuff = true;
			LoadKeyData("Buffs", "Raid Buff Request", ParsedData, RaidBuffRequests);
			foreach (var buff in RaidBuffRequests) buff.IsBuff = true;
			LoadKeyData("Buffs", "Stack Buff Request", ParsedData, StackBuffRequest);
			foreach (var buff in StackBuffRequest) buff.IsBuff = true;
			LoadKeyData("Buffs", "Aura", ParsedData, Buffs_Auras);
			foreach (var buff in Buffs_Auras) buff.IsBuff = true;


			LoadKeyData("Startup Commands", "Command", ParsedData, StartupCommands);
			LoadKeyData("Zoning Commands", "Command", ParsedData, ZoningCommands);


			LoadKeyData("Buffs", "Cast Aura(On/Off)", ParsedData, ref Buffs_CastAuras);

			LoadKeyData("Melee Abilities", "Ability", ParsedData, MeleeAbilities);

            LoadKeyData("Cursor Delete", "Delete", ParsedData, Cursor_Delete);

            LoadKeyData("Nukes", "Main", ParsedData, Nukes);
            LoadKeyData("Stuns", "Main", ParsedData, Stuns);

            LoadKeyData("Dispel", "Main", ParsedData, Dispels);
            LoadKeyData("Dispel", "Ignore", ParsedData, DispelIgnore);

            LoadKeyData("PBAE", "PBAE", ParsedData, PBAE);

            LoadKeyData("Life Support", "Life Support", ParsedData, LifeSupport);

            LoadKeyData("DoTs on Assist", "Main", ParsedData, Dots_Assist);
            LoadKeyData("DoTs on Command", "Main", ParsedData, Dots_OnCommand);

            LoadKeyData("Debuffs", "Debuff on Assist", ParsedData, Debuffs_OnAssist);
            LoadKeyData("Debuffs", "Debuff on Command", ParsedData, Debuffs_Command);

			//LoadKeyData("LootCommander", "Enabled",ParsedData, ref LootCommander_Enabled);
			//LoadKeyData("LootCommander", "Looter", ParsedData, LootCommander_Looters);

			LoadKeyData("Burn", ParsedData, BurnCollection);
			LoadKeyData("CommandSets", ParsedData, CommandCollection);

            LoadKeyData("Pets", "Pet Spell", ParsedData, PetSpell);
            LoadKeyData("Pets", "Pet Buff", ParsedData, PetOwnerBuffs);
			LoadKeyData("Pets", "Combat Pet Buff", ParsedData, CombatPetOwnerBuffs);
            LoadKeyData("Pets", "Blocked Pet Buff", ParsedData, BlockedPetBuffs);
            LoadKeyData("Pets", "Pet Heal", ParsedData, PetHeals);
            LoadKeyData("Pets", "Pet Mend (Pct)", ParsedData, ref Pet_MendPercent);
            LoadKeyData("Pets", "Pet Taunt (On/Off)", ParsedData, ref Pet_TauntEnabled);
            LoadKeyData("Pets", "Pet Auto-Shrink (On/Off)", ParsedData, ref Pet_AutoShrink);
            LoadKeyData("Pets", "Pet Summon Combat (On/Off)", ParsedData, ref Pet_SummonCombat);
          
            LoadKeyData("Rez", "AutoRez", ParsedData, ref Rez_AutoRez);
            LoadKeyData("Rez", "Auto Rez Spells", ParsedData, Rez_AutoRezSpells);
            LoadKeyData("Rez", "Rez Spells", ParsedData, Rez_RezSpells);

            LoadKeyData("Cures", "Cure", ParsedData, Cures);
            LoadKeyData("Cures", "CureAll", ParsedData, CureAll);
            LoadKeyData("Cures", "RadiantCure", ParsedData, RadiantCure);

			//if we have the AA add it to the collection before we load the rest. 
			var tRC = new Spell("Radiant Cure");
			if (tRC.CastType == CastingType.AA) RadiantCureSpells.Add(tRC);
			LoadKeyData("Cures", "RadiantCureSpells", ParsedData, RadiantCureSpells);
			LoadKeyData("Cures", "CurseCounters", ParsedData, CurseCounterCure);
			LoadKeyData("Cures", "CurseCountersIgnore", ParsedData, CurseCounterIgnore);
			LoadKeyData("Cures", "CorruptedCounters", ParsedData, CorruptedCounterCure);
			LoadKeyData("Cures", "CorruptedCountersIgnore", ParsedData, CorruptedCounterIgnore);
			LoadKeyData("Cures", "PoisonCounters", ParsedData, PoisonCounterCure);
            LoadKeyData("Cures", "PoisonCountersIgnore", ParsedData, PoisonCounterIgnore);
            LoadKeyData("Cures", "DiseaseCounters", ParsedData, DiseaseCounterCure);
            LoadKeyData("Cures", "DiseaseCountersIgnore", ParsedData, DiseaseCounterIgnore);

            LoadKeyData("Blocked Buffs", "BuffName", ParsedData, BlockedBuffs);

            LoadKeyData("Heals", "Tank Heal", ParsedData, HealTanks);
            LoadKeyData("Heals", "Important Heal", ParsedData, HealImportantBots);
            LoadKeyData("Heals", "All Heal", ParsedData, HealAll);
            LoadKeyData("Heals", "XTarget Heal", ParsedData, HealXTarget);
            LoadKeyData("Heals", "Heal Over Time Spell", ParsedData, HealOverTime);
            LoadKeyData("Heals", "Group Heal", ParsedData, HealGroup);
			LoadKeyData("Heals", "Party Heal", ParsedData, HealParty);
			//LoadKeyData("Heals", "Pet Heal", ParsedData, PetHeals);
			LoadKeyData("Heals", "Pet Heal", ParsedData, HealPets);
            LoadKeyData("Heals", "Number Of Injured Members For Group Heal", ParsedData, ref HealGroup_NumberOfInjuredMembers);

            LoadKeyData("Heals", "Emergency Group Heal", ParsedData, Heal_EmergencyGroupHeals);
            LoadKeyData("Heals", "Emergency Heal", ParsedData, Heal_EmergencyHeals);

            LoadKeyData("Heals", "Tank", ParsedData, HealTankTargets);
            for (Int32 i = 0; i < HealTankTargets.Count; i++)
            {
                HealTankTargets[i] = e3util.FirstCharToUpper(HealTankTargets[i]);
            }

            LoadKeyData("Heals", "Important Bot", ParsedData, HealImportantBotTargets);
            //upper case first letter on all important bots, netbots bug that doesn't like lower case.
            for (Int32 i = 0; i < HealImportantBotTargets.Count; i++)
            {
                HealImportantBotTargets[i] = e3util.FirstCharToUpper(HealImportantBotTargets[i]);
            }

          

            //parse out the Tanks/XTargets/etc into collections via the Set method on the
            //property set method
            WhoToHealString = LoadKeyData("Heals", "Who to Heal", ParsedData);
            WhoToHoTString = LoadKeyData("Heals", "Who to HoT", ParsedData);
            LoadKeyData("Heals", "Pet Owner", ParsedData, HealPetOwners);

            LoadKeyData("Heals", "Auto Cast Necro Heal Orbs (On/Off)", ParsedData, ref HealAutoNecroOrbs);
            LoadKeyData("Off Assist Spells", "Main", ParsedData, OffAssistSpells);
            LoadKeyData("Gimme", "Gimme", ParsedData, Gimme);
			LoadKeyData("Gimme", "Gimme-NoCombat", ParsedData, Gimme_NoCombat);
			LoadKeyData("Gimme", "Gimme-InCombat", ParsedData, ref Gimme_InCombat);

            LoadKeyData("Charm", "CharmSpell",ParsedData, Charm_CharmSpells);
         
			LoadKeyData("Charm", "CharmOhShitSpells", ParsedData, Charm_CharmOhShitSpells);
			LoadKeyData("Charm", "SelfDebuffSpells", ParsedData, Charm_SelfDebuffSpells);
			LoadKeyData("Charm", "BadPetBuffs", ParsedData, Charm_BadPetBuffs);
			LoadKeyData("Charm", "PeelTank", ParsedData, ref Charm_PeelTank);
			LoadKeyData("Charm", "PeelTankAggroAbility", ParsedData, Charm_PeelTankAggroAbility);
			LoadKeyData("Charm", "PeelHealer", ParsedData, ref Charm_PeelHealer);
			LoadKeyData("Charm", "PeelHealerHeal", ParsedData, Charm_PeelHealerHeal);
			LoadKeyData("Charm", "PeelPetOwner", ParsedData,ref Charm_PeelPetOwner);
			LoadKeyData("Charm", "PeelSnarePerson", ParsedData, ref Charm_PeelSnarePerson);
			LoadKeyData("Charm", "PeelSnareSpell", ParsedData,Charm_PeelSnareSpell);
			LoadKeyData("Charm", "PeelDebuffPerson", ParsedData, ref Charm_PeelDebuffPerson);
			LoadKeyData("Charm", "PeelDebuffSpells", ParsedData, Charm_PeelDebuffSpells);

			// _log.Write($"Finished processing and loading: {fullPathToUse}");

		}

		public IniData createNewINIData()
		{
			IniData newFile = new IniData();


			newFile.Sections.AddSection("Misc");
			var section = newFile.Sections.GetSectionData("Misc");
			section.Keys.AddKey("AutoFood", "Off");
			section.Keys.AddKey("Food", "");
			section.Keys.AddKey("Drink", "");
			section.Keys.AddKey("End MedBreak in Combat(On/Off)", "On");
			section.Keys.AddKey("AutoMedBreak (On/Off)", "Off");
			section.Keys.AddKey("Auto-Loot (On/Off)", "Off");
			section.Keys.AddKey("Anchor (Char to Anchor to)", "");
			section.Keys.AddKey("Remove Torpor After Combat", "On");
			section.Keys.AddKey("Auto-Forage (On/Off)", "Off");
			section.Keys.AddKey("Dismount On Interrupt (On/Off)", "On");
			section.Keys.AddKey("Delay in MS After CastWindow Drops For Spell Completion", "0");
			section.Keys.AddKey("If FD stay down (true/false)", "False");
			section.Keys.AddKey("Debuffs/Dots are visible", "True");
			section.Keys.AddKey("Enhanced rotation speed", "Off");
		
			newFile.Sections.AddSection("AutoMed");
			section = newFile.Sections.GetSectionData("AutoMed");
			section.Keys.AddKey("Override Old Settings and use This(On/Off)", "Off");
			section.Keys.AddKey("AutoMedBreak (On/Off)", "Off");
			section.Keys.AddKey("End MedBreak in Combat(On/Off)", "On");
			section.Keys.AddKey("PctMana", "100");
			section.Keys.AddKey("PctStam", "100");
			section.Keys.AddKey("PctHealth", "100");

			newFile.Sections.AddSection("Alerts");
			section = newFile.Sections.GetSectionData("Alerts");
			section.Keys.AddKey("Rampage Messages(On/Off)", "On");
			section.Keys.AddKey("Damage Messages(On/Off)", "On");
			section.Keys.AddKey("Reflect Messages(On/Off)", "On");


			newFile.Sections.AddSection("Assist Settings");
			section = newFile.Sections.GetSectionData("Assist Settings");
			section.Keys.AddKey("Assist Type (Melee/Ranged/Off)", "Melee");
			section.Keys.AddKey("Melee Stick Point", "Behind");
			section.Keys.AddKey("Delayed Strafe Enabled (On/Off)", "On");
			section.Keys.AddKey("CommandOnAssist", "");
			if (((CharacterClass & Class.Tank) == CharacterClass) || CharacterClass == Class.Ranger)
			{
				section.Keys.AddKey("SmartTaunt(On/Off)", "On");
			}
			section.Keys.AddKey("Melee Distance", "MaxMelee");
			section.Keys.AddKey("Ranged Distance", "100");
			section.Keys.AddKey("Auto-Assist Engage Percent", "98");
			section.Keys.AddKey("Pet back off on Enrage (On/Off)", "Off");
			section.Keys.AddKey("Back off on Enrage (On/Off)", "Off");

			newFile.Sections.AddSection("Buffs");
			section = newFile.Sections.GetSectionData("Buffs");
			section.Keys.AddKey("Instant Buff", "");
			section.Keys.AddKey("Self Buff", "");
			section.Keys.AddKey("Bot Buff", "");
			section.Keys.AddKey("Combat Buff", "");
			section.Keys.AddKey("Group Buff", "");
			section.Keys.AddKey("Pet Buff", "");
			section.Keys.AddKey("Combat Pet Buff", "");
			section.Keys.AddKey("Aura", "");
			section.Keys.AddKey("Group Buff Request", "");
			section.Keys.AddKey("Raid Buff Request", "");
			section.Keys.AddKey("Stack Buff Request", "");
			section.Keys.AddKey("Cast Aura(On/Off)", "On");


			if ((CharacterClass & Class.Caster) != CharacterClass && (CharacterClass & Class.Priest) != CharacterClass)
			{
				newFile.Sections.AddSection("Melee Abilities");
				section = newFile.Sections.GetSectionData("Melee Abilities");
				section.Keys.AddKey("Ability", "");
			}
			//in case a melee has a nuke item?
			newFile.Sections.AddSection("Nukes");
			section = newFile.Sections.GetSectionData("Nukes");
			section.Keys.AddKey("Main", "");

			newFile.Sections.AddSection("Debuffs");
			section = newFile.Sections.GetSectionData("Debuffs");
			section.Keys.AddKey("Debuff on Assist", "");
			section.Keys.AddKey("Debuff on Command", "");

			if ((CharacterClass & Class.PureMelee) != CharacterClass && CharacterClass != Class.Bard)
			{
				newFile.Sections.AddSection("Stuns");
				section = newFile.Sections.GetSectionData("Stuns");
				section.Keys.AddKey("Main", "");

				newFile.Sections.AddSection("PBAE");
				section = newFile.Sections.GetSectionData("PBAE");
				section.Keys.AddKey("PBAE", "");

				newFile.Sections.AddSection("DoTs on Assist");
				section = newFile.Sections.GetSectionData("DoTs on Assist");
				section.Keys.AddKey("Main", "");

				newFile.Sections.AddSection("DoTs on Command");
				section = newFile.Sections.GetSectionData("DoTs on Command");
				section.Keys.AddKey("Main", "");

				
			}
			if ((CharacterClass & Class.ManaUsers) == CharacterClass && CharacterClass!= Class.Bard)
			{
				newFile.Sections.AddSection("Off Assist Spells");
				section = newFile.Sections.GetSectionData("Off Assist Spells");
				section.Keys.AddKey("Main", "");
			}
			
			newFile.Sections.AddSection("Dispel");
			section = newFile.Sections.GetSectionData("Dispel");
			section.Keys.AddKey("Main", "");
			section.Keys.AddKey("Ignore", "");

			newFile.Sections.AddSection("Life Support");
			section = newFile.Sections.GetSectionData("Life Support");
			section.Keys.AddKey("Life Support", "");

			newFile.Sections.AddSection("Rez");
			section = newFile.Sections.GetSectionData("Rez");
			section.Keys.AddKey("AutoRez", "Off");
			section.Keys.AddKey("Auto Rez Spells", "Token of Resurrection");
			section.Keys.AddKey("Rez Spells", "Token of Resurrection");

			newFile.Sections.AddSection("Burn");
			section = newFile.Sections.GetSectionData("Burn");
			section.Keys.AddKey("Quick Burn", "");
			section.Keys.AddKey("Long Burn", "");
			section.Keys.AddKey("Full Burn", "");

			newFile.Sections.AddSection("CommandSets");
		

			if (CharacterClass == Class.Rogue)
			{
				newFile.Sections.AddSection("Rogue");
				section = newFile.Sections.GetSectionData("Rogue");
				section.Keys.AddKey("Auto-Hide (On/Off)", "Off");
				section.Keys.AddKey("Auto-Evade (On/Off)", "Off");
				section.Keys.AddKey("Evade PctAggro", "75");
				section.Keys.AddKey("Sneak Attack Discipline", "");
				//section.Keys.AddKey("PoisonPR", "");
				//section.Keys.AddKey("PoisonFR", "");
				//section.Keys.AddKey("PoisonCR", "");
			}

			if (CharacterClass == Class.Bard)
			{
				newFile.Sections.AddSection("Bard");
				section = newFile.Sections.GetSectionData("Bard");
				section.Keys.AddKey("MelodyIf", "");
				section.Keys.AddKey("AutoMezSong", "");
				section.Keys.AddKey("AutoMezSongDuration in seconds", "18");
				section.Keys.AddKey("Auto-Sonata (On/Off)", "Off");
			}

			if ((CharacterClass & Class.PetClass) == CharacterClass)
			{
				newFile.Sections.AddSection("Pets");
				section = newFile.Sections.GetSectionData("Pets");
				section.Keys.AddKey("Pet Spell", "");
				section.Keys.AddKey("Pet Heal", "");
				section.Keys.AddKey("Pet Buff", "");
				section.Keys.AddKey("Combat Pet Buff", "");
				section.Keys.AddKey("Pet Mend (Pct)", "");
				section.Keys.AddKey("Pet Taunt (On/Off)", "On");
				section.Keys.AddKey("Pet Auto-Shrink (On/Off)", "Off");
				section.Keys.AddKey("Pet Summon Combat (On/Off)", "Off");
				section.Keys.AddKey("Blocked Pet Buff","");
			}

			if ((CharacterClass & Class.Druid) == CharacterClass)
			{
				newFile.Sections.AddSection("Druid");
				section = newFile.Sections.GetSectionData("Druid");
				section.Keys.AddKey("Evac Spell", "");
				section.Keys.AddKey("Auto-Cheetah (On/Off)", "Off");

			}
			if ((CharacterClass & Class.Wizard) == CharacterClass)
			{
				newFile.Sections.AddSection("Wizard");
				section = newFile.Sections.GetSectionData("Wizard");
				section.Keys.AddKey("Evac Spell", "");
			}

			if (((CharacterClass & Class.Priest) == CharacterClass)|| CharacterClass== Class.Paladin)
			{
				newFile.Sections.AddSection("Cures");
				section = newFile.Sections.GetSectionData("Cures");
				section.Keys.AddKey("Cure", "");
				section.Keys.AddKey("CureAll", "");
				section.Keys.AddKey("RadiantCure", "");
				section.Keys.AddKey("RadiantCureSpells", "");
				section.Keys.AddKey("CurseCounters", "");
				section.Keys.AddKey("CurseCountersIgnore", "");
				section.Keys.AddKey("CorruptedCounters", "");
				section.Keys.AddKey("CorruptedCountersIgnore", "");
				section.Keys.AddKey("PoisonCounters", "");
				section.Keys.AddKey("PoisonCountersIgnore", "");
				section.Keys.AddKey("DiseaseCounters", "");
				section.Keys.AddKey("DiseaseCountersIgnore", "");
			}

			if ((CharacterClass & Class.Charmer) == CharacterClass)
			{
				newFile.Sections.AddSection("Charm");
				section = newFile.Sections.GetSectionData("Charm");
				section.Keys.AddKey("CharmSpell", "");
				section.Keys.AddKey("CharmOhShitSpells", "");
				section.Keys.AddKey("SelfDebuffSpells", "");
				section.Keys.AddKey("BadPetBuffs", "");
				section.Keys.AddKey("PeelTank", "");
				section.Keys.AddKey("PeelTankAggroAbility", "");
				section.Keys.AddKey("PeelHealer", "");
				section.Keys.AddKey("PeelHealerHeal", "");
				section.Keys.AddKey("PeelPetOwner", "");
				section.Keys.AddKey("PeelSnarePerson", "");
				section.Keys.AddKey("PeelSnareSpell", "");
				section.Keys.AddKey("PeelDebuffPerson", "");
				section.Keys.AddKey("PeelDebuffSpells", "");
			}



			if ((CharacterClass & Class.Priest) == CharacterClass || (CharacterClass & Class.HealHybrid) == CharacterClass)
			{
				newFile.Sections.AddSection("Heals");
				section = newFile.Sections.GetSectionData("Heals");
				section.Keys.AddKey("Tank Heal", "");
				section.Keys.AddKey("Important Heal", "");
				section.Keys.AddKey("Group Heal", "");
				section.Keys.AddKey("Party Heal", "");
				section.Keys.AddKey("Heal Over Time Spell", "");
				section.Keys.AddKey("All Heal", "");
				section.Keys.AddKey("XTarget Heal", "");
				section.Keys.AddKey("Tank", "");
				section.Keys.AddKey("Important Bot", "");
				section.Keys.AddKey("Pet Heal", "");
				section.Keys.AddKey("Who to Heal", "Tanks/ImportantBots/XTargets/Pets/Party/All");
				section.Keys.AddKey("Who to HoT", "");
				section.Keys.AddKey("Pet Owner", "");
				section.Keys.AddKey("Auto Cast Necro Heal Orbs (On/Off)", "On");
				section.Keys.AddKey("Number Of Injured Members For Group Heal", "3");
				section.Keys.AddKey("Emergency Heal", "");
				section.Keys.AddKey("Emergency Group Heal", "");
			}

			

			if (CharacterClass == Class.Magician)
			{
				newFile.Sections.AddSection("Magician");
				section = newFile.Sections.GetSectionData("Magician");
				section.Keys.AddKey("Auto-Pet Weapons (On/Off)", "Off");
				section.Keys.AddKey("Ignore Pet Weapon Requests (On/Off)", "Off");
				section.Keys.AddKey("Allow Pet Weapon Requests from Guild Bypass(On/Off)", "On");
				section.Keys.AddKey("Keep Open Inventory Slot (On/Off)", "Off");
				section.Keys.AddKey("Pet Weapons", "");
			}

			if (CharacterClass == Class.Shaman)
			{
				newFile.Sections.AddSection("Shaman");
				section = newFile.Sections.GetSectionData("Shaman");
				section.Keys.AddKey("Auto-Canni (On/Off)", "Off");
				section.Keys.AddKey("Canni", "");
				section.Keys.AddKey("Malos Totem Spell Gem", "8");
			}

			if (CharacterClass == Class.Beastlord)
			{
				newFile.Sections.AddSection("Auto Paragon");
				section = newFile.Sections.GetSectionData("Auto Paragon");
				section.Keys.AddKey("Auto Paragon (On/Off)", "Off");
				section.Keys.AddKey("Paragon Spell", "Paragon of Spirit");
				section.Keys.AddKey("Paragon Mana (Pct)", "60");
				section.Keys.AddKey("Auto Focused Paragon (On/Off)", "Off");
				section.Keys.AddKey("Focused Paragon Spell", "Focused Paragon of Spirits");
				section.Keys.AddKey("Focused Paragon Mana (Pct)", "70");
				section.Keys.AddKey("Character", "");
			}

			newFile.Sections.AddSection("Bando Buff");
			section = newFile.Sections.GetSectionData("Bando Buff");
			section.Keys.AddKey("Enabled", "Off");
			section.Keys.AddKey("BuffName", "");
			section.Keys.AddKey("DebuffName", "");
			section.Keys.AddKey("BandoNameWithBuff", "");
			section.Keys.AddKey("BandoNameWithoutBuff", "");
			section.Keys.AddKey("BandoNameWithoutDeBuff", "");
			section.Keys.AddKey("ExceptionZone", "poknowledge");
			section.Keys.AddKey("ExceptionZone", "guildlobby");

			newFile.Sections.AddSection("Rampage Actions");
			section = newFile.Sections.GetSectionData("Rampage Actions");
			section.Keys.AddKey("Action", "");
		
			newFile.Sections.AddSection("Blocked Buffs");
			section = newFile.Sections.GetSectionData("Blocked Buffs");
			section.Keys.AddKey("BuffName", "");

			newFile.Sections.AddSection("Cursor Delete");
			section = newFile.Sections.GetSectionData("Cursor Delete");
			section.Keys.AddKey("Delete", "");

			newFile.Sections.AddSection("Gimme");
			section = newFile.Sections.GetSectionData("Gimme");
			section.Keys.AddKey("Gimme-InCombat", "On");
			section.Keys.AddKey("Gimme", "");
			section.Keys.AddKey("Gimme-NoCombat", "");

			//newFile.Sections.AddSection("LootCommander");
			//section = newFile.Sections.GetSectionData("LootCommander");
			//section.Keys.AddKey("Enabled (On/Off)", "Off");
			//section.Keys.AddKey("Looter", "");


			newFile.Sections.AddSection("Ifs");
		
			newFile.Sections.AddSection("Events");
			newFile.Sections.AddSection("EventLoop");
			newFile.Sections.AddSection("EventLoopTiming");
			newFile.Sections.AddSection("EventRegMatches");
			newFile.Sections.AddSection("Report");
			section = newFile.Sections.GetSectionData("Report");
			section.Keys.AddKey("ReportEntry", "");

			newFile.Sections.AddSection("CPU");
			section = newFile.Sections.GetSectionData("CPU");
			section.Keys.AddKey("ProcessLoopDelayInMS", "50");
			section.Keys.AddKey("PublishStateDataInMS", "50");
			section.Keys.AddKey("PublishBuffDataInMS", "1000");
			section.Keys.AddKey("PublishSlowDataInMS", "1000");

			section.Keys.AddKey("Camp Pause at 30 seconds", "True");
			section.Keys.AddKey("Camp Pause at 20 seconds", "True");
			section.Keys.AddKey("Camp Shutdown at 5 seconds", "True");

			newFile.Sections.AddSection("Manastone");
			section = newFile.Sections.GetSectionData("Manastone");

			section.Keys.AddKey("Override General Settings (On/Off)", "Off");
			section.Keys.AddKey("Manastone Enabled (On/Off)", "On");
			section.Keys.AddKey("NumberOfClicksPerLoop", "40");
			section.Keys.AddKey("NumberOfLoops", "25");
			section.Keys.AddKey("DelayBetweenLoops (in milliseconds)", "50");
			section.Keys.AddKey("In Combat MinMana", "40");
			section.Keys.AddKey("In Combat MaxMana", "75");
			section.Keys.AddKey("Use In Combat", "On");
			section.Keys.AddKey("Min HP", "60");
			section.Keys.AddKey("Min HP Out of Combat", "60");
			section.Keys.AddKey("Out of Combat MinMana", "85");
			section.Keys.AddKey("Out of Combat MaxMana", "95");
			section.Keys.AddKey("ExceptionZone", "poknowledge");
			section.Keys.AddKey("ExceptionZone", "thevoida");
			section.Keys.AddKey("ExceptionMQQuery", "");
			section.Keys.AddKey("UseForLazarusEncEpicBuff", "Off");

			newFile.Sections.AddSection("Startup Commands");
			section = newFile.Sections.GetSectionData("Startup Commands");
			section.Keys.AddKey("Command", "");
			newFile.Sections.AddSection("Zoning Commands");
			section = newFile.Sections.GetSectionData("Zoning Commands");
			section.Keys.AddKey("Command", "");

			newFile.Sections.AddSection("E3BotsPublishData (key/value)");
			newFile.Sections.AddSection("E3ChatChannelsToJoin");
			section = newFile.Sections.GetSectionData("E3ChatChannelsToJoin");
			section.Keys.AddKey("Channel");

			return newFile;
		}
		/// <summary>
		/// Creates the settings file.
		/// </summary>
		/// <returns></returns>
		public IniData CreateSettings(string fileName)
        {
            //if we need to , its easier to just output the entire file. 

            FileIniDataParser parser = e3util.CreateIniParser();

			IniData newFile = createNewINIData();

			if (!String.IsNullOrEmpty(CurrentSet))
            {
                fileName = fileName.Replace(".ini", "_" + CurrentSet + ".ini");
            }


            if (!File.Exists(fileName))
            {
                if (!Directory.Exists(_configFolder + _botFolder))
                {
                    Directory.CreateDirectory(_configFolder + _botFolder);
                }
                //file straight up doesn't exist, lets create it
                parser.WriteFile(fileName, newFile);
                _fileLastModified = System.IO.File.GetLastWriteTime(fileName);
                _fileLastModifiedFileName = fileName;
                _fileName = fileName;
            }
            else
            {
				//File already exists, may need to merge in new settings lets check
				//Parse the ini file
				//Create an instance of a ini file parser
				FileIniDataParser fileIniData = e3util.CreateIniParser();
				IniData tParsedData = fileIniData.ReadFile(fileName);
				if (_mergeUpdates)
                {
					//overwrite newfile with what was already there
					tParsedData.Merge(newFile);
					//save it it out now
					File.Delete(fileName);
					parser.WriteFile(fileName, tParsedData);

				}
				newFile = tParsedData;
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
			//time to pull out the reflection noone has time to manage all that settings crap
			var charSettings = e3util.GetSettingsMappedToInI();
			List<string> transferedKeyComments = new List<string>();

			IniData defaultFile = createNewINIData();

			//lets save the normal stuff
			foreach (var pair in charSettings)
			{
				string header = pair.Key;

				var section = defaultFile.Sections.GetSectionData(header);
				//copy over comments from loaded section
				var loadeddata_section = ParsedData.Sections.GetSectionData(header);
				if (loadeddata_section != null && section != null)
				{
					section.Comments.AddRange(loadeddata_section.Comments);
				}

				foreach (var pair2 in pair.Value)
				{
					//now we have the header and keyname of the ini entry
					
					string keyName = pair2.Key;
					

					transferedKeyComments.Clear();

					if (section != null)
					{
						var section_keyCollection = defaultFile.Sections[header];

						try
						{
							if (keyName == String.Empty)
							{
								foreach (var keyData in loadeddata_section.Keys)
								{
									if (keyData.Comments.Count > 0)
									{
										transferedKeyComments.AddRange(keyData.Comments);
									}
								}
								section_keyCollection.RemoveAllKeys();
							}
							else
							{
								var deletedKey = section_keyCollection.GetKeyData(keyName);
								if (deletedKey == null)
								{
									//not valid for this class type
									continue;
								}

								var deleteKeyHeader = ParsedData.Sections[header];
								if(deleteKeyHeader!=null)
								{
									deletedKey = deleteKeyHeader.GetKeyData(keyName);
								}
								

								if (deletedKey != null && deletedKey.Comments.Count > 0)
								{
									transferedKeyComments.AddRange(deletedKey.Comments);
								}


								section_keyCollection.RemoveKey(keyName);

							}

							FieldInfo field = pair2.Value;

							Object reference = field.GetValue(this);
							//have to work with all the types in the settings class
							if (reference is List<Spell>)
							{
								List<Spell> spellList = (List<Spell>)reference;
								if (spellList.Count == 0)
								{
									section_keyCollection.AddKey(keyName, "");
									continue;
								}
								
								foreach (var spell in spellList)
								{
									//self buff hack to remove the target if its a self buff
									if(header =="Buffs" && keyName=="Self Buff")
									{
										spell.CastTarget = String.Empty;
									}
									section_keyCollection.AddKey(keyName, spell.ToConfigEntry());
								}
							}
							else if (reference is List<SpellRequest>)
							{
								List<SpellRequest> spellList = (List<SpellRequest>)reference;
								if (spellList.Count == 0)
								{
									section_keyCollection.AddKey(keyName, "");
									continue;
								}
								foreach (var spell in spellList)
								{
									section_keyCollection.AddKey(keyName, spell.ToConfigEntry());
								}
							}
							else if (reference is List<MelodyIfs>)
							{
								List<MelodyIfs> melodyIfsList = (List<MelodyIfs>)reference;
								if (melodyIfsList.Count == 0)
								{
									section_keyCollection.AddKey(keyName, "");
									continue;
								}
								foreach (var spell in melodyIfsList)
								{
									section_keyCollection.AddKey(keyName, spell.ToConfigEntry());
								}
							}
							else if (reference is List<string>)
							{
								List<String> stringlist = (List<String>)reference;
								if (stringlist.Count == 0)
								{
									section_keyCollection.AddKey(keyName, "");
									continue;
								}
								foreach (var value in stringlist)
								{
									section_keyCollection.AddKey(keyName, value);
								}
							}
							else if (reference is IDictionary<string, string>)
							{
								IDictionary<string, string> stringDict = (IDictionary<string, string>)reference;
								foreach (var tpair in stringDict)
								{
									section_keyCollection.AddKey(tpair.Key, tpair.Value);
								}
							}
							else if (reference is IDictionary<string, Burn>)
							{
								IDictionary<string, Burn> stringDict = (IDictionary<string, Burn>)reference;
								foreach (var tpair in stringDict)
								{
									if(tpair.Value.ItemsToBurn.Count>0)
									{
										foreach (var burn in tpair.Value.ItemsToBurn)
										{
											section_keyCollection.AddKey(tpair.Key, burn.ToConfigEntry());
										}
									}
									else
									{
										section_keyCollection.AddKey(tpair.Key, "");

									}
									
								}
							}
							else if (reference is IDictionary<string, CommandSet>)
							{
								IDictionary<string, CommandSet> stringDict = (IDictionary<string, CommandSet>)reference;
								foreach (var tpair in stringDict)
								{
									if (tpair.Value.Commands.Count > 0)
									{
										foreach (var commandValue in tpair.Value.Commands)
										{
											section_keyCollection.AddKey(tpair.Key, commandValue);
										}
									}
									else
									{
										section_keyCollection.AddKey(tpair.Key, "");

									}

								}
							}
							else
							{
								if (reference is string)
								{
									section_keyCollection.AddKey(keyName, (string)reference);
								}
								else if (reference is bool)
								{
									string boolString = "On";
									if (!(bool)reference)
									{
										boolString = "Off";
									}
									section_keyCollection.AddKey(keyName, boolString);
								}
								else if (reference is Int32)
								{
									section_keyCollection.AddKey(keyName, ((Int32)reference).ToString());
								}
								else if (reference is Int64)
								{
									section_keyCollection.AddKey(keyName, ((Int64)reference).ToString());
								}
							}
						}
						finally
						{
							if (transferedKeyComments.Count > 0)
							{
								//cary over any comments on the key 
								if (keyName != String.Empty)
								{
									var newKeyData = section_keyCollection.GetKeyData(keyName);
									newKeyData.Comments.AddRange(transferedKeyComments);

								}
								else
								{
									//just add all the comments to the first key
									foreach (var keyData in section_keyCollection)
									{
										keyData.Comments.AddRange(transferedKeyComments);
										break;
									}
								}
							}
						}
						
						
					}
				}
			}
			//now for the snowflake bards :)
			if(E3.CurrentClass== Class.Bard)
			{
				
				//dict of List<spell>
				foreach (var pair in Bard_MelodySets)
				{
					transferedKeyComments.Clear();
					string header = $"{pair.Key} Melody";
					defaultFile.Sections.AddSection(header);
					var section = defaultFile.Sections[header];
					var old_section = ParsedData.Sections.GetSectionData(header);
					//KeyData oldKey = null;
					if(old_section!=null)
					{
						foreach (var keyData in old_section.Keys)
						{
							if (keyData.Comments.Count > 0)
							{
								transferedKeyComments.AddRange(keyData.Comments);
							}
						}
					}
					foreach (var spell in pair.Value)
					{
						//list<spell>
						section.AddKey("Song", spell.ToConfigEntry());
					}
					var newKeySet = section.GetKeyData("Song");
					if(newKeySet!=null)
					{
						newKeySet.Comments.AddRange(transferedKeyComments);
					}
				}
			}
			FileIniDataParser fileIniData = e3util.CreateIniParser();
            File.Delete(_fileName);
            fileIniData.WriteFile(_fileName, defaultFile);
        }
		private void GetSettingsMappedToDictionary()
		{
			//now for some ... reflection again.
			var type = this.GetType();

			foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
			{
				var oType = field.FieldType;
				if (!(oType == typeof(string)|| oType == typeof(Int32) || oType == typeof(Int64) || oType == typeof(bool) || oType==typeof(List<string>) || oType == typeof(List<Int32>) || oType == typeof(List<Int64>) || oType == typeof(List<Spell>)) ) continue;

				var customAttributes = field.GetCustomAttributes();
				string section = String.Empty;
				string key = String.Empty;

				foreach (var attribute in customAttributes)
				{
					if (attribute is INI_SectionAttribute)
					{
						var tattribute = ((INI_SectionAttribute)attribute);

						section = tattribute.Header;
						key = tattribute.Key;
						string dictKey = $"${{E3N.Settings.{section}.{key}}}";
						SettingsReflectionLookup.Add(dictKey, field);

					}
					if (attribute is INI_Section2Attribute)
					{
						var tattribute = ((INI_Section2Attribute)attribute);
						section = tattribute.Header;
						key = tattribute.Key;
						string dictKey = $"${{E3N.Settings.{section}.{key}}}";
						SettingsReflectionLookup.Add(dictKey, field);
					}
				}
			}
		}
		public static readonly Dictionary<string, string> ConfigKeyDescriptionsByKey = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
		{
			{"Autofood", "Turns on eating the food and drink defined below."},
			{"Food", "Define what you would like to eat and drink. This can be used to keep stat food from getting consumed."},
			{"Drink", "Define what you would like to eat and drink. This can be used to keep stat food from getting consumed."},
			{"Auto-Loot (On/Off)", "When enabled your character will autoloot once combat is over. Looting in combat can be enabled in general_settings.ini."},
			{"AutoMezSong", "Song that the bard should use for automatic mezzing."},
			{"AutoMezSongDuration in seconds", "Expected duration of the auto-mez song, used to schedule recasts."},
			{"Evac Spell", "Emergency evac spell or AA used when evac routines trigger."},
			{"Channel", "Chat channel name that this character should join on login."},
			{"PctMana", "Mana percent threshold used by this routine."},
			{"PctAggro", "Aggro percent threshold used by this routine."},
			{"Delay", "Delay value controlling timing for this entry (supports values like 1000, 10s, or 1m)."},
			{"RecastDelay", "Minimum time between casts for this entry (supports values like 1000, 10s, or 1m)."},
			{"GiveUpTimer", "Milliseconds to wait before giving up on the action if it has not succeeded."},
			{"Gem", "Spell gem number (1-12) that should be used for this spell."},
		};

		public static readonly Dictionary<string, string> ConfigKeyDescriptionsBySection = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
		{
			{"Misc::Autofood", "Turns on eating the food and drink defined below"},
			{"Misc::Food", "Define what you would like to eat and drink. This can be used to keep stat food from getting consumed. Food=Misty Thicket Picnic Drink=Fuzzlecutter Formula 5000 Note: Multiple Food and Drink can be defined"},
			{"Misc::Drink", "Define what you would like to eat and drink. This can be used to keep stat food from getting consumed. Food=Misty Thicket Picnic Drink=Fuzzlecutter Formula 5000 Note: Multiple Food and Drink can be defined"},
			{"Misc::End MedBreak in Combat(On/Off)", "When enabled you will cancel medding to assist in combat even if you are not at defined mana percentage."},
			{"Misc::AutoMedBreak (On/Off)", "Will sit and med when below the defind Mana percentage in your general_settings.ini"},
			{"Misc::Auto-Loot (On/Off)", "When enabled your character will autoloot once combat is over. Looting in combact can be enabled in general_settings.ini"},
			{"Misc::Anchor (Char to Anchor to)", "When one of your characters is defined, it will stick to that character. They will go out fight and loot(if on) and then return to the define anchored character."},
			{"Misc::Remove Torpor After Combat", "if you would like Torpor automatically removed once combat is over."},
			{"Misc::Auto-Forage (On/Off)", "When enabled you will start foraging. You can leverage [Cursor Delete] section in this Ini to manage the junk loot"},
			{"Misc::Dismount On Interrupt (On/Off)", "If you are on a mount and your priority requires you to inturrupt a spell for a higher priority spell(ex: Nuking -> Heal)."},
			{"Misc::Delay in MS After CastWindow Drops For Spell Completion", "Will interject a delay before casting or moving again after you are done casting. Sometimes lag can create an issue where you/bot see itself as done casting and then moves and then your spell gets canceled. This creates milliseconds of day to make sure cast finished."},
			{"Misc::If FD stay down (true/false)", "If you are Feign Death and an assist command is giving it will stand you up."},
			{"Misc::Debuffs/Dots are visible", "Set to On if you have the Leadership AA that allows you to see your DoTs and Debuffs on the target's buff window. When set to Off, E3 will internally track timers to determine when to recast DoTs and Debuffs since they won't be visible."},
			{"Assist Settings::Assist Type (Melee/AutoAttack/Ranged/AutoFire/Off)", "Defines how your character will behave in combat. Melee - Will melee, stick, and use all configuration. AutoAttack - Will melee and use configuration without automated movement/stick. Ranged - Will ranged attack, stick, and use configuration. AutoFire - Will ranged attack and use configuration without automated movement/stick. Off - Will use no movement, stick, or facing and just use configuration."},
			{"Assist Settings::SmartTaunt(On/Off)", "Automatically try and taunt off non-tank PCs to maintain aggro control."},
			{"Assist Settings::Melee Stick Point", "The position you want you character stand during combat. behind - Will stay behind the target and readjust position behindonce - Will move behind target and not readjust unless a new assist command is giving. Front - Will place you in front of mob facing it. Normally used by tanks. Pin - Will put on you on the side of the of target. Not in front and not behind. !front Will place you anywhere that isn't the front of the mob."},
			{"Assist Settings::Delayed Strafe Enabled (On/Off)", "The amount of time your character will wait before readjusting to a moved target."},
			{"Assist Settings::CommandOnAssist", "E3 will execute these values as a command each time an assist is called. Useful for triggering custom actions or macros when engaging targets."},
			{"Assist Settings::Melee Distance", "The distance you wish to stand from the target when melee'ing. MaxMelee - Will calculate based off the target the furthest point to melee, between 25 - 33."},
			{"Assist Settings::Ranged Distance", "When Ranged is specified as Assist Type will keep you the defined distance away from target. Clamped - Doesn't care about a defined distance as long as you are between 30 and 200."},
			{"Assist Settings::Auto-Assist Engage Percent", "Note: This is dependent on you enabling in you general_settings.ini(Off by default) What this setting is stating that when the character of this configed Ini's target hits the specfied percentage it will automatically tell all other bots to assist. It is NOT stating that this character starts assisting at defined percentage. My personal(Metaljacx) recommendation for those new; not to use autoassist. Leverage /assistme /cleartargets"},
			{"Assist Settings::Pet back off on Enrage (On/Off)", "When Enrage is detected, E3 will stop attacks."},
			{"Assist Settings::Back off on Enrage (On/Off)", "When Enrage is detected, E3 will stop attacks."},
			{"Melee Abilities::Ability", "The name of a melee ability, skill, or discipline"},
			{"Buffs::Instant Buff", "Any buff that is self targetable that has a cast time less then .1 second. This will make sure the buff stays up inside and outside of combat. Great for fights where buffs get dispell to keep cheap buffs in first few slots."},
			{"Buffs::Self Buff", "For any spell where you can target yourself and has a castime greater then Instant Buff allows. Will only cast outside of combat"},
			{"Buffs::Bot Buff", "This is for targeting your other characters for buffs. Will only cast outside of combat. Example: bot buff=buffname/target Bot Buff=Spirit of Might/Metaljacx Bot Buff=Spirit of Might/Silverjacx"},
			{"Buffs::Combat Buff", "If you just want the buff during combat Example: Combat Buff=Artifact of the Leopard - If casting self Combat Buff=Artifact of the Leopard/Metaljacx - If casting on another character."},
			{"Buffs::Group Buff", "Despite it's name this has nothing to do with automating buffs for your 6 Man Group What this key value actually does it buff anyone who asks you for buffs with the pharse \"Buff Me\" or \"Buff my Pet\". This is a triggered non-automated event. Who can ask for buff an be more defined in you general_settings.ini Example: Group Buff=Blessing of Aegolism Note: You may need to do /tgb on for group buffs to apply to people outside your group"},
			{"Buffs::Pet Buff", "This is for other characters on your bot network pets.Must be a part of your bot network. If your class has a pet that will be defined in the [pet section]. Example: Pet Buff=buffname/PetownerName Pet Buff=Artifact of the Leopard/Copperjacx"},
			{"Buffs::Group Buff Request", "This is to request a buff from someone else who is running E3 and not part of bot network. Example: Group Buff Request=buffname/target Group Buff Request=Torpor Rk. V/Tophet/Ifs|TorporIf Tip: In order to not not spam and annoy your friends. Try using an If Statement. TorporIf=!${Bool[${Me.Song[Torpor Rk. V].ID}]} || ${Me.Song[Torpor Rk. V].Duration} <=9000"},
			{"Buffs::Raid Buff Request", "This is to request a buff from someone else who is running E3 and not part of bot network. Example: Group Buff Request=buffname/target Group Buff Request=Torpor Rk. V/Tophet/Ifs|TorporIf Tip: In order to not not spam and annoy your friends. Try using an If Statement. TorporIf=!${Bool[${Me.Song[Torpor Rk. V].ID}]} || ${Me.Song[Torpor Rk. V].Duration} <=9000"},
			{"Buffs::Stack Buff Request", "This is where you can request the same buff from multiple characters who can provide the same buff. This is request, First IN First Out(FIFO). Switches /StackRequetTargets| - All the characters who can cast buff. /StackRecastDelay| - How long to wait before asking for buff again. /StackCheckInterval| - How often to check if you still have buff /StackRequestItem| - If the buff requires an Item to be clicked"},
			{"Buffs::Cast Aura(On/Off)", "This will automatically cast your class's highest level aura if your class has one"},
			{"Nukes::Main", "Add the spells or items you'd like to use for your nukes here. E3 will cast them in order from top down."},
			{"Nukes/Stuns/PBAE::Main", "Name of the spell you wish to cast. Example: main=Spear of Ro"},
			{"Nukes/Stuns/PBAE::PBAE", "Point Blank Area of Effect(PBAE) is turned off by default to turn on or off use /pbaeon and /pbaeoff. Otherwise it is the same as Nuke just input your poe spell Example: PBAE=Spear of Ro Reminder: Priority matters(FIFO) for Advanced Settings AE Order and what a spell CD is."},
			{"DoTs on Assist::Main", "The spells or items here will be automatically cast or used on each assist call."},
			{"Debuffs::Debuff on Assist", "The spells or items here will be automatically cast or used on each assist call."},
			{"DoTs on Command::Main", "These items/spells will be used/cast on command (/dot), allowing for additional control on when they should be utilized."},
			{"Debuffs::Debuff on Command", "These items/spells will be used/cast on command (/debuff), allowing for additional control on when they should be utilized."},
			{"Dots/Debuffs::Main", "Don't ask why they are built different. This applies to main= under [DoTs on Assist] and Debuff on Assist=. These will automatically fire off once they receive one of the assist commands."},
			{"Dots/Debuffs::Debuff on Assist", "Don't ask why they are built different. This applies to main= under [DoTs on Assist] and Debuff on Assist=. These will automatically fire off once they receive one of the assist commands."},
			{"Dots/Debuffs::Debuff on Command", "This applies to main= under [DoTs on Command] and Debuff on Command=. These are turned off by default, and allow for more control if you want. To toggle on and off use /dot to toggle for dots and /debuff for debuffs."},
			{"Off Assist Spells::Debuff on Assist", "These spells will be cast on every mob in the XTargets list when the bot receives an assist command."},
			{"Dispel::Main", "Spell or Item you wish to use to dispell your current target. The looks for any benificals spells on the target. Example: Main=Abashi's Rod of Disempowerment"},
			{"Dispel::Ignore", "Buffs on the target you wish to ignore from trying to debuff. Each bufff you wish to ignore is a new ignore= Example: Ignore=Yaulp III Ignore=Spirit of Wolf"},
			{"LifeSupport::Life Support", "This is top priority whe it comes to what to process first and it based on your characters health percentage. Use this for self heal, mitigation, imunnitity, or evasions. Example: Life Support=Hymn of the Last Stand/HealPct|30 Life Support=Shield of Notes/HealPct|40 Life Support=Cazel's Distillate of Celestial Healing/HealPct|80"},
			{"Rez::AutoRez", "If turn on will Rez in and out of combat"},
			{"Rez::Auto Rez Spells", "This is the spell to be used if AutoRez=On Example: Auto Rez Spells=Blessing of Resurrection"},
			{"Rez::Rez Spells", "These are the spell that will be used for the slash commands. You can use multiple rez spells for ones with longer CD. Example: Rez Spells=Blessing of Resurrection Rez Spells=Resurrection"},
			{"Burn::Quick Burn", "Will accept any spell or item and the concept is for \"short\" CD spells. Your preferace on what \"short\" is."},
			{"Burn::Long Burn", "Will accept any spell or item and the concept is for \"Long\" CD spells. Your preferace on what \"long\" is."},
			{"Burn::Full Burn", "The spells or items you want to use in a full send moment"},
			{"Pets::Pet Spell", "The pet spell you wish to use for summoning"},
			{"Pets::Pet Heal", "Spells you wish to use to heal you pet. Example: Pet Heal=Healing of Mikkily/Gem|1/HealPct|55"},
			{"Pets::Pet Buff", "Buff you wish for your pet to have. You can configure multiple Pet Buff= Example: Pet Buff=Spirit of Irionu Pet Buff=Growl of the Beast"},
			{"Pets::Pet Mend (Pct)", "What percentage you want you character to use Mend AA. Example: Pet Mend(Pct)=40 This will trigger at 40% of pet's health"},
			{"Pets::Pet Taunt (On/Off)", "Set's whether your pet taunts or not."},
			{"Pets::Pet Auto-Shrink (On/Off)", "Will auto shrink your pet when summon or illusioned."},
			{"Pets::Pet Summon Combat (On/Off)", "If your pet dies during combat will prioritize summonging your pet based on what is defined in Pet Spell="},
			{"Pets::Pet Buff Combat (On/Off)", "If a buff drops off during combat your bot will rebuff during combat. Good if you casting puma/leopard line spell in combat."},
			{"Cures::AutoRadiant (On/Off)", "Default is On. Will leverage your Radiant Cure AA if defined"},
			{"Cures::Cure", "Specify a cure to a particular debuff to a particular person (Higher Prio(2)) Example: Cure=Remove Greater Curse/Steeljacx/CheckFor|Feeblemind/Gem|12 Cure=Crusader's Touch/Metaljacx/CheckFor|Ikaav's Venom"},
			{"Cures::CureAll", "Specify a cure to a particular debuff to anyone in group (Lower Prio(3)) Example: CureAll=Remove Greater Curse/CheckFor|Relinquish Spirit/Gem|12 CureAll=Remove Greater Curse/CheckFor|Torment of Body/Gem|12"},
			{"Cures::RadiantCure", "Specify a type of debuff to use radiant cure if at least this many people have it. (Highest Prio(1)) Example: RadiantCure=Fulmination/MinSick|1/Zone|txevu RadiantCure=Fabled Destruction/MinSick|1/Zone|Unrest"},
			{"Cures::CurseCounters", "Cath All (Lowest Prio (4)) Use spell(s) to try and cure if you see this type of debuff counter on a toon in group. Example: CurseCounters=Remove Greater Curse PoisonCounters=Blood of Nadox DiseaseCounters=Blood of Nadox"},
			{"Cures::PoisonCounters", "Cath All (Lowest Prio (4)) Use spell(s) to try and cure if you see this type of debuff counter on a toon in group. Example: CurseCounters=Remove Greater Curse PoisonCounters=Blood of Nadox DiseaseCounters=Blood of Nadox"},
			{"Cures::DiseaseCounters", "Cath All (Lowest Prio (4)) Use spell(s) to try and cure if you see this type of debuff counter on a toon in group. Example: CurseCounters=Remove Greater Curse PoisonCounters=Blood of Nadox DiseaseCounters=Blood of Nadox"},
			{"Cures::CorruptedCounters", "Cath All (Lowest Prio (4)) Use spell(s) to try and cure if you see this type of debuff counter on a toon in group. Example: CurseCounters=Remove Greater Curse PoisonCounters=Blood of Nadox DiseaseCounters=Blood of Nadox"},
			{"Cures::CurseCountersIgnore", "Specify debuff names with curse counters that should NOT be cured automatically. This is for catch all counter curing only. Example: CurseCountersIgnore=Aura of Destruction"},
			{"Cures::PoisonCountersIgnore", "Specify debuff names with poison counters that should NOT be cured automatically. This is for catch all counter curing only. Example: PoisonCountersIgnore=Aura of Destruction"},
			{"Cures::DiseaseCountersIgnore", "Specify debuff names with disease counters that should NOT be cured automatically. This is for catch all counter curing only. Example: DiseaseCountersIgnore=Aura of Destruction"},
			{"Cures::CorruptedCountersIgnore", "Specify debuff names with corruption counters that should NOT be cured automatically. This is for catch all counter curing only. Example: CorruptedCountersIgnore=Aura of Destruction"},
			{"Heals::Who to Heal", "Default: Tanks/ImportantBots/XTargets/Pets/Party Defines which key you would like to be heal. This allows for quick on and off without having to comment lines out."},
			{"Heals::Who to HoT", "Same as Who to Heal= just for Heal Over Time Spell= define key Example: Who to HoT=Tanks"},
			{"Heals::Tank", "Define who your tank/tanks will be. The bots define here will recieve the highest priority for heals. Must be a part of your bot network. Example: Tank=Metaljacx"},
			{"Heals::Tank Heal", "Heal spell/item/aa you wish to use on your tanks. Recommend you order in the order from lowest /healPct|10 to the highest /HealPct|90. One exception you might see is for the spells like Reptile. Example: Tank Heal=Artifact of the Reptile/HealPct|100/CheckFor|Skin of the Reptile Tank Heal=Aged Dragon Spine Staff/HealPct|50/NoInterrupt Tank Heal=Mask of the Ancients/HealPct|60/NoInterrupt Tank Heal=Chlorotrope/Gem|1/HealPct|85/NoInterrupt"},
			{"Heals::Important Bot", "Define which bots you would like to pay close attention too just behind the tank priority. In essance \"second priority\". Alot use for other healers, offtanks, or high threat classes. Must be a part of your bot network. Example: Important Bot=Orihime Important Bot=Rukia Important Bot=Mayuri"},
			{"Heals::Important Heal", "Heal spell/item/aa you wish to use on your important bots. Example: Important Heal=Chlorotrope/Gem|1/HealPct|65"},
			{"Heals::Group Heal", "This is not based on individual group members, but on the average missing health of the group. There is a minimal number required to be injured which can be control with next explain setting. Example: Group Heal=Wave of Marr/Gem|10/HealPct|20/NoInterrupt Group Heal=Wave of Trushar/HealPct|40/NoInterrupt Group Heal=Healing Wave of Prexus/HealPct|65 Group Heal=Wave of Life/HealPct|70"},
			{"Heals::Number Of Injured Members For Group Heal", "Default: 3 This define how many people need to be injured before triggering average Group heal."},
			{"Heals::Party Heal", "This heals your individual party members and heals based on your configured. Wether they are part of your bot network or not /HealPct tag. Heals outside your bot network. Example: Party Heal=Touch of Piety/HealPct|60"},
			{"Heals::Heal Over Time Spell", "The HoT spell/item/aa you wish to use for the groups defined in Who to HoT= Must be a part of your bot network. Example: Heal Over Time Spell=Breath of Trushar/Gem|9/HealPct|95"},
			{"Heals::All Heal", "Heals all bots part of you network whether bots are in your group or not. Example: All Heal=Yoppa's Mending/Gem|1/HealPct|65"},
			{"Heals::XTarget Heal", "How to heal individuals not part of your bot network and in your group. You will need to assign each player you want to heal to your xTarget Window. Heals outside your bot network. Example: XTarget=Chlorotrope/Gem|1/HealPct|65"},
			{"Heals::Pet Owner", "The bot in your network which pet you would like to heal. Example: Pet Owner=Mayuri"},
			{"Heals::Pet Heal", "Heal spell/item/aa you wish to use on pets. Example: XTarget=Chlorotrope/Gem|1/HealPct|55"},
			{"Heals::Emergency Heal", "Heal spell/item/aa that will be used immediatly (cancels other casts) when health drops below the threshold for the specified target. Example: Emergy Heal=Burst of Life/Uguk/HealPct|40"},
			{"Heals::Emergency Group Heal", "Heal spell/item/aa that will be used immediatly (cancels other casts) when health of any character drops below the threshold. Example: Emergy Group Heal=Divine Arbitration/HealPct|40"},
			{"Bando Buff::Enabled", "Default: On Turn on and off"},
			{"Bando Buff::BuffName", "The Buff Name on yourself you wish to monitor"},
			{"Bando Buff::DebuffName", "The Debuff Name on target you wish to monitor"},
			{"Bando Buff::PrimaryWithBuff", "The items you wish to have in your your equipment slots, with and without buff. These weapons need to match with/without your bandolier setup"},
			{"Bando Buff::SecondaryWithBuff", "The items you wish to have in your your equipment slots, with and without buff. These weapons need to match with/without your bandolier setup"},
			{"Bando Buff::PrimaryWithoutBuff", "The items you wish to have in your your equipment slots, with and without buff. These weapons need to match with/without your bandolier setup"},
			{"Bando Buff::SecondaryWithoutBuff", "The items you wish to have in your your equipment slots, with and without buff. These weapons need to match with/without your bandolier setup"},
			{"Bando Buff::BandoNameWithBuff", "The name of the bandolier when you have buff"},
			{"Bando Buff::BandoNameWithoutBuff", "The name of the bandolier when you don't have buff"},
			{"Bando Buff::BandoNameWithoutDeBuff", "When the mob doesn't have debuff. Should move back to BandoNameWithBuff= when debuff detected and you have your buff on"},
			{"Events::_EventName_", "Allows you to define arbitrary commands for your bot to perform. These commands must be triggered by a /BeforeEvent or /AfterEvent conditional, or by a line in the [EventLoop] section. You make up your own _EventName_ on the left side of the equal sign, and then on the right side you write the command that will be performed when the event is triggered. The command will be executed whenever the event is triggered by a /BeforeEvent or /AfterEvent conditional, or by a line in the [EventLoop] section. Example: [Life Support] Life Support=Divine Barrier/HealPct|20/Gem|7/AfterEvent|TellGroupIUsedDivineBarrier [Events] TellGroupIUsedDivineBarrier=/g I just used Divine Barrier!"},
			{"EventLoop::_EventName_", "Allows you to define conditions that will trigger lines from the [Events] section to execute. Lines in the [EventLoop] section are evaluated about once every second, and whenever one of the lines evaluates to True, its associated event is executed. On the left side of the equal sign you put the name of an event that's defined in your [Events] section, and on the right side you put the condition that you want to trigger the event and execute its command. Example: [Events] DropInvisCombat=/makemevisible [EventLoop] DropInvisCombat=(${Me.CombatState.Equal[Combat]} && ${Me.Invis})"},
			{"AutoMed::Override Old Settings and use This(On/Off)", "Use the AutoMed section instead of the legacy med break settings."},
			{"AutoMed::PctMana", "Minimum mana percent that will trigger the automatic med break routine."},
			{"AutoMed::PctStam", "Minimum endurance percent that will trigger the automatic med break routine."},
			{"AutoMed::PctHealth", "Minimum health percent that will trigger the automatic med break routine."},
			{"Heals::AutoFood", "Allow the heal routine to eat/drink automatically when hunger or thirst is low."},
			{"Heals::Food", "Food item used by the healer when AutoFood is enabled."},
			{"Heals::Drink", "Drink item used by the healer when AutoFood is enabled."},
			{"Buffs::AutoBuff (On/Off)", "Toggle automatic buffing for the entries configured in this section."},
			{"Buffs::Check Interval", "Delay between automatic buff checks (supports s/m suffix)."},
			{"Assist::Auto-Assist (On/Off)", "Automatically assist the designated main assist when the routine is active."},
			{"Assist::Assist Target", "Name or alias of the character that this toon should assist."},
			{"Assist::PctAggro", "Maximum aggro percent allowed before backing off when assisting."},
			{"Burn::Auto-Burn (On/Off)", "Enable burn routines to trigger discs and abilities based on configured criteria."},
			{"Burn::Trigger", "Condition or command that activates the burn routine (e.g. /burn)."},
		};
	}
}
