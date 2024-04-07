using E3Core.Data;
using E3Core.Processors;
using E3Core.Utility;
using IniParser;
using IniParser.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using static System.Collections.Specialized.BitVector32;

namespace E3Core.Settings
{
    //update all peg to laz
    //get-childitem *_PEQTGC.ini | rename-item -newname {$_.name -replace '_PEQTGC.ini','_Lazarus.ini' }    
    /// <summary>
    /// Settings specific to the current character
    /// </summary>
    /// <seealso cref="BaseSettings" />
    /// <seealso cref="IBaseSettings" />
    public class CharacterSettings : BaseSettings, IBaseSettings
    {
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public IniData ParsedData;
        private readonly string CharacterName;
        private readonly string ServerName;
        private readonly Class CharacterClass;
        public bool Misc_AutoFoodEnabled;
        public bool Misc_DismountOnInterrupt = true;
        public string Misc_AutoFood;
        public string Misc_AutoDrink;
        public bool Misc_EndMedBreakInCombat;
        public bool Misc_AutoMedBreak;
        public bool Misc_AutoLootEnabled;

      
        
        public string Misc_AnchorChar = string.Empty;
        public bool Misc_RemoveTorporAfterCombat = true;
        public Int32 Misc_DelayAfterCastWindowDropsForSpellCompletion = 0;
       
        public bool Misc_AutoForage = false;
        
        public bool Rogue_AutoHide = false;
        public bool Rogue_AutoEvade = false;
        public int Rogue_EvadePct = 0;
        public string Rogue_PoisonPR = string.Empty;
        public string Rogue_PoisonFR = string.Empty;
        public string Rogue_PoisonCR = string.Empty;
        public string Rogue_SneakAttack = string.Empty;

        public List<MelodyIfs> Bard_MelodyIfs = new List<MelodyIfs>();

        public List<Spell> CasterEvacs = new List<Spell>();
        public bool Druid_AutoCheetah = true;
        public bool Bard_AutoSonata = true;

        public string Assist_Type = string.Empty;
        public string Assist_MeleeStickPoint = string.Empty;
        public bool Assist_TauntEnabled = false;
        public bool Assist_SmartTaunt = false;
        public string Assist_MeleeDistance = "MaxMelee";
        public string Assist_RangeDistance = "100";
        public int Assist_AutoAssistPercent = 98;
        public bool Assist_DelayStrafeEnabled = true;
        public Int32 Assist_DelayStrafeDelay = 1500;
        private string _fileName = String.Empty;
        public bool Assist_PetBackOffOnenrage = false;
        public bool Assist_BackOffOnEnrage = false;
		//abilities
		public List<Spell> MeleeAbilities = new List<Spell>();
        //nukes
        public List<Spell> Nukes = new List<Spell>();
        public List<Spell> Stuns = new List<Spell>();
        //dispel
        public List<Spell> Dispels = new List<Spell>();
        public List<Spell> DispelIgnore = new List<Spell>();

        //buffs
        public List<Spell> InstantBuffs = new List<Spell>();
        public List<Spell> SelfBuffs = new List<Spell>();
        public List<Spell> BotBuffs = new List<Spell>();
        public List<Spell> GroupBuffs = new List<Spell>();
        public List<Spell> CombatBuffs = new List<Spell>();
        public List<Spell> PetBuffs = new List<Spell>();
		public List<Spell> CombatPetBuffs = new List<Spell>();
		public bool Buffs_CastAuras = true;
		public List<Spell> Buffs_Auras = new List<Spell>();
        public List<Spell> BlockedPetBuffs = new List<Spell>();
         public List<SpellRequest> GroupBuffRequests = new List<SpellRequest>();
        public List<SpellRequest> RaidBuffRequests = new List<SpellRequest>();

        public List<SpellRequest> StackBuffRequest = new List<SpellRequest>();

        //gimme
        public List<string> Gimme = new List<string>();
        public bool Gimme_InCombat = true;
        //pets
        public List<Spell> PetSpell = new List<Spell>();
        public List<Spell> PetHeals = new List<Spell>();
        public int Pet_MendPercent;
        public bool Pet_TauntEnabled;
        public bool Pet_AutoShrink;
        public bool Pet_SummonCombat;
        public bool Pet_BuffCombat;
        //debuffs
        public List<Spell> Debuffs_OnAssist = new List<Spell>();
        public List<Spell> Debuffs_Command = new List<Spell>();
        public List<Spell> Debuffs_All = new List<Spell>();
        //dots
        public List<Spell> Dots_OnCommand = new List<Spell>();
        public List<Spell> Dots_Assist = new List<Spell>();
        //aoe
        public List<Spell> PBAE = new List<Spell>();
        public List<Spell> TargetAE = new List<Spell>();
        //burns
        public List<Spell> QuickBurns = new List<Spell>();
        public List<Spell> LongBurns = new List<Spell>();
        public List<Spell> FullBurns = new List<Spell>();
        //cures
        public bool AutoRadiant = false;
        public List<Spell> Cures = new List<Spell>();
        public List<Spell> CureAll = new List<Spell>();
        public List<Spell> RadiantCure = new List<Spell>();
        public List<Spell> CurseCounterCure = new List<Spell>();
		public List<Spell> CurseCounterIgnore = new List<Spell>();
		public List<Spell> CorruptedCounterCure = new List<Spell>();
		public List<Spell> CorruptedCounterIgnore = new List<Spell>();
		public List<Spell> PoisonCounterCure = new List<Spell>();
        public List<Spell> PoisonCounterIgnore = new List<Spell>();
        public List<Spell> DiseaseCounterCure = new List<Spell>();
        public List<Spell> DiseaseCounterIgnore = new List<Spell>();
        //life support
        public List<Spell> LifeSupport = new List<Spell>();

        //blocked buffs
        public List<Spell> BlockedBuffs = new List<Spell>();

        public bool IfFDStayDown = false;

        //bando buffs
        public bool BandoBuff_Enabled = false;
		public string BandoBuff_BuffName = String.Empty;
		public string BandoBuff_DebuffName = String.Empty;
		public string BandoBuff_Primary = String.Empty;
		public string BandoBuff_Secondary = String.Empty;
		public string BandoBuff_PrimaryWithoutBuff = String.Empty;
		public string BandoBuff_SecondaryWithoutBuff = String.Empty;
	    public string BandoBuff_BandoName = String.Empty;
		public string BandoBuff_BandoNameWithoutBuff = String.Empty;
		public string BandoBuff_BandoNameWithoutDeBuff = String.Empty;

		//manastone
		public bool Manastone_Enabled = true;
        public bool Manastone_OverrideGeneralSettings = false;
        public Int32 ManaStone_NumberOfClicksPerLoop = 40;
        public Int32 ManaStone_NumberOfLoops = 25;
        public Int32 ManaStone_DelayBetweenLoops = 50;

        public bool ManaStone_EnabledInCombat = true;
        public Int32 ManaStone_InCombatMinMana = 40;
        public Int32 ManaStone_InCombatMaxMana = 75;
        public Int32 ManaStone_MinHP = 60;
        public Int32 ManaStone_OutOfCombatMinMana = 85;
        public Int32 ManaStone_OutOfCombatMaxMana = 95;
		public HashSet<string> ManaStone_ExceptionZones = new HashSet<string> {};
        public List<string> ManaStone_ExceptionMQQuery = new List<string>();

		//heals
		public List<string> HealTankTargets = new List<string>();
        public List<Spell> HealTanks = new List<Spell>();

        public List<string> HealImportantBotTargets = new List<string>();
        public List<Spell> HealImportantBots = new List<Spell>();
        public List<string> StartupCommands = new List<string>();
        public List<Spell> HealGroup = new List<Spell>();
        public Int32 HealGroup_NumberOfInjuredMembers = 3;
        public List<Spell> HealAll = new List<Spell>();
        public List<Spell> HealParty = new List<Spell>();
        public List<Spell> HealXTarget = new List<Spell>();
        public List<Spell> HealPets = new List<Spell>();
        public List<Spell> HealOverTime = new List<Spell>();
        public List<string> HealPetOwners = new List<string>();
        //rez spells
        public List<string> Rez_AutoRezSpells = new List<string>();
        public List<string> Rez_RezSpells = new List<string>();
        public bool Rez_AutoRez = false;

        //report
        public List<Spell> Report_Entries = new List<Spell>();


        //E3BotsPublishData
        public Dictionary<string,string> E3BotsPublishData = new Dictionary<string,string>();

        //charm data
        public Spell Charm_CharmSpell = null;
        public List<Spell> Charm_CharmOhShitSpells = new List<Spell>();
        public List<Spell> Charm_SelfDebuffSpells = new List<Spell>();
		public List<Spell> Charm_BadPetBuffs = new List<Spell>();
        public string Charm_PeelTank = String.Empty;
        public List<Spell> Charm_PeelTankAggroAbility = new List<Spell>();
        public string Charm_PeelHealer = String.Empty;
        public List<Spell> Charm_PeelHealerHeal = new List<Spell>();
        public string Charm_PeelPetOwner = String.Empty;
        public string Charm_PeelSnarePerson = String.Empty;
        public List<Spell> Charm_PeelSnareSpell  = new List<Spell>();
        public string Charm_PeelDebuffPerson = String.Empty;
        public List<Spell> Charm_PeelDebuffSpells = new List<Spell>();

		//

		//Loot Commander

		public bool LootCommander_Enabled;
        public List<string> LootCommander_Looters = new List<string>();

		public Int32 CPU_ProcessLoopDelay = 50;
		public bool CPU_Camping_PauseAt30Seconds = true;
		public bool CPU_Camping_PauseAt20Seconds = true;
        public bool CPU_Camping_ShutdownAt5Seconds = true;

		public Dictionary<string, string> PetWeapons = new Dictionary<string, string>();
        public bool AutoPetWeapons = false;
        public bool KeepOpenInventorySlot = false;
        public bool IgnorePetWeaponRequests = false;
        public bool AutoCanni = false;
        public int MalosTotemSpellGem;
        public List<Spell> CanniSpell = new List<Spell>();

        public bool AutoParagon = false;
        public Spell ParagonSpell = null;
        public int ParagonManaPct = 60;
        public bool AutoFocusedParagon = false;
        public Spell FocusedParagonSpell = null;
        public List<string> FocusedParagonCharacters = new List<string>();
        public int FocusedParagonManaPct = 70;

        public HashSet<string> WhoToHeal = new HashSet<string>(10, StringComparer.OrdinalIgnoreCase);
        public bool HealAutoNecroOrbs = false;
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
        public List<Spell> OffAssistSpells = new List<Spell>();

        //clearcursor delete
        public List<String> Cursor_Delete = new List<string>();

        /// <summary>
        /// Initializes a new instance of the <see cref="CharacterSettings"/> class.
        /// </summary>
        public CharacterSettings()
        {
            CharacterName = E3.CurrentName;
            ServerName = E3.ServerName;
            CharacterClass = E3.CurrentClass;
            LoadData();

        }
        /// <summary>
        /// Loads the data.
        /// </summary>
        private void LoadData()
        {

            //this is so we can get the merged data as well. 
            string filename = GetBoTFilePath($"{CharacterName}_{ServerName}.ini");
            ParsedData = CreateSettings(filename);


            LoadKeyData("CPU", "ProcessLoopDelayInMS", ParsedData, ref CPU_ProcessLoopDelay);
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

			LoadKeyData("Manastone", "Override General Settings (On/Off)", ParsedData, ref Manastone_OverrideGeneralSettings);
            LoadKeyData("Manastone", "Manastone Enabled (On/Off)", ParsedData, ref Manastone_Enabled);

            LoadKeyData("Manastone", "NumberOfClicksPerLoop", ParsedData, ref ManaStone_NumberOfClicksPerLoop);
            LoadKeyData("Manastone", "NumberOfLoops", ParsedData, ref ManaStone_NumberOfLoops);
            LoadKeyData("Manastone", "DelayBetweenLoops (in milliseconds)", ParsedData, ref ManaStone_DelayBetweenLoops);
            LoadKeyData("Manastone", "In Combat MinMana", ParsedData, ref ManaStone_InCombatMinMana);
            LoadKeyData("Manastone", "In Combat MaxMana", ParsedData, ref ManaStone_InCombatMaxMana);
            LoadKeyData("Manastone", "Use In Combat", ParsedData, ref ManaStone_EnabledInCombat);
            LoadKeyData("Manastone", "Min HP", ParsedData, ref ManaStone_MinHP);
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


            LoadKeyData("Report", "ReportEntry", ParsedData, Report_Entries);

		


			LoadKeyData("Bando Buff", "Enabled", ParsedData, ref BandoBuff_Enabled);
			LoadKeyData("Bando Buff", "DebuffName", ParsedData, ref BandoBuff_DebuffName);
			LoadKeyData("Bando Buff", "BuffName", ParsedData, ref BandoBuff_BuffName);
			LoadKeyData("Bando Buff", "PrimaryWithBuff", ParsedData, ref BandoBuff_Primary);
			LoadKeyData("Bando Buff", "SecondaryWithBuff", ParsedData, ref BandoBuff_Secondary);
			LoadKeyData("Bando Buff", "PrimaryWithoutBuff", ParsedData, ref BandoBuff_PrimaryWithoutBuff);
			LoadKeyData("Bando Buff", "SecondaryWithoutBuff", ParsedData, ref BandoBuff_SecondaryWithoutBuff);
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
                LoadKeyData("Bard", "Auto-Sonata (On/Off)", ParsedData, ref Bard_AutoSonata);
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
                if (AutoFocusedParagon)
                {
                    MQ.Cmd("/plugin mq2dannet");
                    if (!MQ.Query<bool>("${Plugin[mq2dannet]}"))
                    {
                        E3.Bots.Broadcast("\arUnable to load mq2dannet - disabling auto focused paragon");
                        AutoFocusedParagon = false;
                    }

                    E3.Bots.Broadcast("Adding dannet observers for focused paragon characters' mana");
                    foreach (var character in FocusedParagonCharacters)
                    {
                        MQ.Cmd($"/dobserve {character} -q Me.PctMana");
                    }
                }
            }

            Dictionary<string, string> tempPublishData = new Dictionary<string, string>();
            LoadKeyData("E3BotsPublishData (key/value)", ParsedData, tempPublishData);

            //now we need to change the keys to be in a specific format
            foreach(var pair in tempPublishData)
            {
                string key = "${Data." + pair.Key + "}";
                if(!E3BotsPublishData.ContainsKey(key)) 
                {
					E3BotsPublishData.Add("${Data." + pair.Key + "}", pair.Value);
				}

				
            }


            LoadKeyData("Buffs", "Instant Buff", ParsedData, InstantBuffs);
            LoadKeyData("Buffs", "Self Buff", ParsedData, SelfBuffs);
            //set target on self buffs
            foreach (var buff in SelfBuffs)
            {
                buff.CastTarget = CharacterName;
            }

            LoadKeyData("Buffs", "Bot Buff", ParsedData, BotBuffs);
            LoadKeyData("Buffs", "Combat Buff", ParsedData, CombatBuffs);
            LoadKeyData("Buffs", "Group Buff", ParsedData, GroupBuffs);
			LoadKeyData("Buffs", "Pet Buff", ParsedData, PetBuffs);
			LoadKeyData("Buffs", "Combat Pet Buff", ParsedData, CombatPetBuffs);
			LoadKeyData("Buffs", "Group Buff Request", ParsedData, GroupBuffRequests);
            LoadKeyData("Buffs", "Raid Buff Request", ParsedData, RaidBuffRequests);
			LoadKeyData("Buffs", "Stack Buff Request", ParsedData, StackBuffRequest);
            LoadKeyData("Buffs", "Aura", ParsedData, Buffs_Auras);


			LoadKeyData("Startup Commands", "Command", ParsedData, StartupCommands);


			LoadKeyData("Buffs", "Cast Aura(On/Off)", ParsedData, ref Buffs_CastAuras);

			LoadKeyData("Melee Abilities", "Ability", ParsedData, MeleeAbilities);

            LoadKeyData("Cursor Delete", "Delete", ParsedData, Cursor_Delete);

            LoadKeyData("Nukes", "Main", ParsedData, Nukes);
            LoadKeyData("Stuns", "Main", ParsedData, Stuns);

            LoadKeyData("Dispel", "Main", ParsedData, Dispels);
            LoadKeyData("Dispel", "Ignore", ParsedData, DispelIgnore);

            LoadKeyData("TargetAE", "TargetAE", ParsedData, PBAE);
            LoadKeyData("PBAE", "PBAE", ParsedData, PBAE);

            LoadKeyData("Life Support", "Life Support", ParsedData, LifeSupport);

            LoadKeyData("DoTs on Assist", "Main", ParsedData, Dots_Assist);
            LoadKeyData("DoTs on Command", "Main", ParsedData, Dots_OnCommand);

            LoadKeyData("Debuffs", "Debuff on Assist", ParsedData, Debuffs_OnAssist);
            LoadKeyData("Debuffs", "Debuff on Command", ParsedData, Debuffs_Command);

            LoadKeyData("LootCommander", "Enabled",ParsedData, ref LootCommander_Enabled);
            LoadKeyData("LootCommander", "Looter", ParsedData, LootCommander_Looters);

            LoadKeyData("Burn", "Quick Burn", ParsedData, QuickBurns);
            LoadKeyData("Burn", "Long Burn", ParsedData, LongBurns);
            LoadKeyData("Burn", "Full Burn", ParsedData, FullBurns);


            LoadKeyData("Pets", "Pet Spell", ParsedData, PetSpell);
            LoadKeyData("Pets", "Pet Buff", ParsedData, PetBuffs);
			LoadKeyData("Pets", "Combat Pet Buff", ParsedData, CombatPetBuffs);
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

			LoadKeyData("Heals", "Pet Heal", ParsedData, HealPets);
            LoadKeyData("Heals", "Number Of Injured Members For Group Heal", ParsedData, ref HealGroup_NumberOfInjuredMembers);


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

            LoadKeyData("Heals", "Pet Heal", ParsedData, PetHeals);

            //parse out the Tanks/XTargets/etc into collections via the Set method on the
            //property set method
            WhoToHealString = LoadKeyData("Heals", "Who to Heal", ParsedData);
            WhoToHoTString = LoadKeyData("Heals", "Who to HoT", ParsedData);
            LoadKeyData("Heals", "Pet Owner", ParsedData, HealPetOwners);

            LoadKeyData("Heals", "Auto Cast Necro Heal Orbs (On/Off)", ParsedData, ref HealAutoNecroOrbs);
            LoadKeyData("Off Assist Spells", "Main", ParsedData, OffAssistSpells);
            LoadKeyData("Gimme", "Gimme", ParsedData, Gimme);
            LoadKeyData("Gimme", "Gimme-InCombat", ParsedData, ref Gimme_InCombat);

            List<Spell> tcharmSpells = new List<Spell>();

            LoadKeyData("Charm", "CharmSpell",ParsedData, tcharmSpells);
            foreach(Spell spell in tcharmSpells)
            {
                Charm_CharmSpell = spell;
                break;
            }
			LoadKeyData("Charm", "CharmOhShitSpells", ParsedData, Charm_CharmOhShitSpells);
			LoadKeyData("Charm", "SelfDebuffSpells", ParsedData, Charm_SelfDebuffSpells);
			LoadKeyData("Charm", "BadPetBuffs", ParsedData, Charm_BadPetBuffs);
			LoadKeyData("Charm", "PeelTank", ParsedData, ref Charm_PeelTank);
			LoadKeyData("Charm", "PellTankAggroAbility", ParsedData, Charm_PeelTankAggroAbility);
			LoadKeyData("Charm", "PeelHealer", ParsedData, ref Charm_PeelHealer);
			LoadKeyData("Charm", "PeelHealerHeal", ParsedData, Charm_PeelHealerHeal);
			LoadKeyData("Charm", "PeelPetOwner", ParsedData,ref Charm_PeelPetOwner);
			LoadKeyData("Charm", "PeelSnarePerson", ParsedData, ref Charm_PeelSnarePerson);
			LoadKeyData("Charm", "PeelSnareSpell", ParsedData,Charm_PeelSnareSpell);
			LoadKeyData("Charm", "PeelDebuffPerson", ParsedData, ref Charm_PeelDebuffPerson);
			LoadKeyData("Charm", "PeelDebuffSpells", ParsedData, Charm_PeelDebuffSpells);

			// _log.Write($"Finished processing and loading: {fullPathToUse}");

		}

		/// <summary>
		/// Creates the settings file.
		/// </summary>
		/// <returns></returns>
		public IniData CreateSettings(string fileName)
        {
            //if we need to , its easier to just output the entire file. 

            FileIniDataParser parser = e3util.CreateIniParser();
            IniData newFile = new IniData();


            newFile.Sections.AddSection("Misc");
            var section = newFile.Sections.GetSectionData("Misc");
            section.Keys.AddKey("AutoFood", "Off");
            section.Keys.AddKey("Food", "");
            section.Keys.AddKey("Drink", "");
            section.Keys.AddKey("End MedBreak in Combat(On/Off)", "Off");
            section.Keys.AddKey("AutoMedBreak (On/Off)", "Off");
            section.Keys.AddKey("Auto-Loot (On/Off)", "Off");
            section.Keys.AddKey("Anchor (Char to Anchor to)", "");
            section.Keys.AddKey("Remove Torpor After Combat", "On");
            section.Keys.AddKey("Auto-Forage (On/Off)", "Off");
            section.Keys.AddKey("Dismount On Interrupt (On/Off)","On");
            section.Keys.AddKey("Delay in MS After CastWindow Drops For Spell Completion", "0");
			section.Keys.AddKey("If FD stay down (true/false)", "False");

			newFile.Sections.AddSection("Assist Settings");
            section = newFile.Sections.GetSectionData("Assist Settings");
            section.Keys.AddKey("Assist Type (Melee/Ranged/Off)", "Melee");
            section.Keys.AddKey("Melee Stick Point", "Behind");
			section.Keys.AddKey("Delayed Strafe Enabled (On/Off)", "On");
			if (((CharacterClass & Class.Tank) == CharacterClass) || CharacterClass== Class.Ranger)
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
            if ((CharacterClass & Class.PureMelee) != CharacterClass && CharacterClass != Class.Bard)
            {
                newFile.Sections.AddSection("Nukes");
                section = newFile.Sections.GetSectionData("Nukes");
                section.Keys.AddKey("Main", "");
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

                newFile.Sections.AddSection("Debuffs");
                section = newFile.Sections.GetSectionData("Debuffs");
                section.Keys.AddKey("Debuff on Assist", "");
                section.Keys.AddKey("Debuff on Command", "");
            }

            //if not a tank class
            if(!((CharacterClass & Class.Tank)==CharacterClass))
            {
                newFile.Sections.AddSection("Dispel");
                section = newFile.Sections.GetSectionData("Dispel");
                section.Keys.AddKey("Main", "");
                section.Keys.AddKey("Ignore", "");
            }

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

            if ((CharacterClass & Class.Priest) == CharacterClass)
            {
                newFile.Sections.AddSection("Cures");
                section = newFile.Sections.GetSectionData("Cures");
                section.Keys.AddKey("Cure", "");
                section.Keys.AddKey("CureAll", "");
                section.Keys.AddKey("RadiantCure", "");
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
				section.Keys.AddKey("PellTankAggroAbility", "");
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
                section.Keys.AddKey("Who to Heal", "Tanks/ImportantBots/XTargets/Pets/Party");
                section.Keys.AddKey("Who to HoT", "");
                section.Keys.AddKey("Pet Owner", "");
                section.Keys.AddKey("Auto Cast Necro Heal Orbs (On/Off)", "On");
                section.Keys.AddKey("Number Of Injured Members For Group Heal", "3");
            }

            if ((CharacterClass & Class.Priest) == CharacterClass || (CharacterClass & Class.Caster) == CharacterClass)
            {
                newFile.Sections.AddSection("Off Assist Spells");
                section = newFile.Sections.GetSectionData("Off Assist Spells");
                section.Keys.AddKey("Main", "");
            }

            if (CharacterClass == Class.Magician)
            {
                newFile.Sections.AddSection("Magician");
                section = newFile.Sections.GetSectionData("Magician");
                section.Keys.AddKey("Auto-Pet Weapons (On/Off)", "Off");
                section.Keys.AddKey("Ignore Pet Weapon Requests (On/Off)", "Off");
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
			section.Keys.AddKey("PrimaryWithBuff", "");
			section.Keys.AddKey("SecondaryWithBuff", "");
			section.Keys.AddKey("PrimaryWithoutBuff", "");
			section.Keys.AddKey("SecondaryWithoutBuff", "");
			section.Keys.AddKey("BandoNameWithBuff", "");
			section.Keys.AddKey("BandoNameWithoutBuff", "");
			section.Keys.AddKey("BandoNameWithoutDeBuff", "");



			newFile.Sections.AddSection("Startup Commands");
			section = newFile.Sections.GetSectionData("Startup Commands");
			section.Keys.AddKey("Command", "");


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


			newFile.Sections.AddSection("LootCommander");
			section = newFile.Sections.GetSectionData("LootCommander");
			section.Keys.AddKey("Enabled (On/Off)", "Off");
			section.Keys.AddKey("Looter", "");


			newFile.Sections.AddSection("Ifs");
			newFile.Sections.AddSection("E3BotsPublishData (key/value)");
			newFile.Sections.AddSection("Events");
			newFile.Sections.AddSection("EventLoop");
			newFile.Sections.AddSection("Report");
			section = newFile.Sections.GetSectionData("Report");
			section.Keys.AddKey("ReportEntry", "");

			newFile.Sections.AddSection("CPU");
			section = newFile.Sections.GetSectionData("CPU");
			section.Keys.AddKey("ProcessLoopDelayInMS", "50");
			section.Keys.AddKey("Camp Pause at 30 seconds", "True");
			section.Keys.AddKey("Camp Pause at 20 seconds", "True");
			section.Keys.AddKey("Camp Shutdown at 5 seconds", "True");

			newFile.Sections.AddSection("Manastone");
            section = newFile.Sections.GetSectionData("Manastone");

            section.Keys.AddKey("Override General Settings (On/Off)", "Off");
            section.Keys.AddKey("Manastone Enabled (On/Off)","On");
            section.Keys.AddKey("NumberOfClicksPerLoop", "40");
            section.Keys.AddKey("NumberOfLoops", "25");
            section.Keys.AddKey("DelayBetweenLoops (in milliseconds)", "50");
            section.Keys.AddKey("In Combat MinMana", "40");
            section.Keys.AddKey("In Combat MaxMana", "75");
            section.Keys.AddKey("Use In Combat", "On");
            section.Keys.AddKey("Min HP", "60");
            section.Keys.AddKey("Out of Combat MinMana", "85");
            section.Keys.AddKey("Out of Combat MaxMana", "95");
            section.Keys.AddKey("ExceptionZone", "poknowledge");
			section.Keys.AddKey("ExceptionZone", "thevoida");
			section.Keys.AddKey("ExceptionMQQuery", "");


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
            var section = ParsedData.Sections["Blocked Buffs"];
            if (section == null)
            {
                ParsedData.Sections.AddSection("Blocked Buffs");
                var newSection = ParsedData.Sections.GetSectionData("Blocked Buffs");
                newSection.Keys.AddKey("BuffName", "");

            }
            section = ParsedData.Sections["Blocked Buffs"];
            section.RemoveAllKeys();
            foreach (var spell in BlockedBuffs)
            {
                section.AddKey("BuffName", spell.SpellName);
            }

            FileIniDataParser fileIniData = e3util.CreateIniParser();
            File.Delete(_fileName);
            fileIniData.WriteFile(_fileName, ParsedData);
        }
    }
}
