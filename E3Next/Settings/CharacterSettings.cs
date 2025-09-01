using E3Core.Data;
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

		[INI_Section("Assist Settings", "Assist Type (Melee/Ranged/Off)")]
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
				section.Keys.AddKey("PoisonPR", "");
				section.Keys.AddKey("PoisonFR", "");
				section.Keys.AddKey("PoisonCR", "");
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
	}
}
