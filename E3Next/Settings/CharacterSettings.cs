using E3Core.Data;
using E3Core.Processors;
using E3Core.Utility;
using IniParser;
using IniParser.Model;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace E3Core.Settings
{
    public class CharacterSettings : BaseSettings, IBaseSettings
    {
        public static IniData _parsedData;
        public CharacterSettings()
        {
            _characterName = MQ.Query<string>("${Me.CleanName}");
            _serverName = ProcessServerName(MQ.Query<string>("${MacroQuest.Server}"));
            string classValue = MQ.Query<String>("${Me.Class}");
            Enum.TryParse<Data.Class>(classValue, out _characterClass);
            _log.Write("Name:" + _characterName);
            _log.Write("Class:" + classValue);
            _log.Write("ServerName:" + _serverName);
            LoadData();
           
        }
        private string ProcessServerName(string serverName)
        {

            if (String.IsNullOrWhiteSpace(serverName)) return "Lazarus";

            if(serverName.Equals("Project Lazarus"))
            {
                return "Lazarus";
            }

            return serverName.Replace(" ","_");
        }
        public void LoadData()
        {

            string filename = $"{_characterName}_{_serverName}.ini";
            _log.Write($"Loading up {filename}");
            
            string macroFile = _macroFolder + _botFolder + filename;
            string configFile = _configFolder + _botFolder + filename;

            _log.Write($"macrofile:{macroFile} config file:{configFile}");
            _log.Write($"macrofolder:{_macroFolder} config folder:{_configFolder}");

            string fullPathToUse = macroFile;
            
            FileIniDataParser fileIniData = e3util.CreateIniParser();
            if (!System.IO.File.Exists(configFile) && !System.IO.File.Exists(macroFile))
            {
                if (!System.IO.Directory.Exists(_configFolder+_botFolder))
                {
                    System.IO.Directory.CreateDirectory(_configFolder+_botFolder);
                }

                fullPathToUse = configFile;
                _log.Write($"Settings not found creating new settings: {fullPathToUse}");
                _parsedData = CreateOrUpdateSettings();
            }
            else
            {
                if (System.IO.File.Exists(configFile)) fullPathToUse = configFile;

                //Parse the ini file
                //Create an instance of a ini file parser

                _log.Write($"Loading up {fullPathToUse}");
                _parsedData = fileIniData.ReadFile(fullPathToUse);
            }

            LoadKeyData("Misc", "AutoFood", _parsedData, ref Misc_AutoFoodEnabled);
            LoadKeyData("Misc", "Food", _parsedData, ref Misc_AutoFood);
            LoadKeyData("Misc", "Drink", _parsedData, ref Misc_AutoDrink);
            LoadKeyData("Misc", "End MedBreak in Combat(On/Off)", _parsedData, ref Misc_EndMedBreakInCombat);
            LoadKeyData("Misc", "AutoMedBreak (On/Off)", _parsedData, ref Misc_AutoMedBreak);
            LoadKeyData("Misc", "Auto-Loot (On/Off)", _parsedData, ref Misc_AutoLootEnabled);
            LoadKeyData("Misc", "Anchor (Char to Anchor to)", _parsedData, ref Misc_AnchorChar);

            LoadKeyData("Assist Settings", "Assist Type (Melee/Ranged/Off)", _parsedData, ref Assist_Type);
            LoadKeyData("Assist Settings", "Melee Stick Point", _parsedData, ref Assist_MeleeStickPoint);
            LoadKeyData("Assist Settings", "Taunt(On/Off)", _parsedData, ref Assist_TauntEnabled);
            LoadKeyData("Assist Settings", "SmartTaunt(On/Off)", _parsedData, ref Assist_SmartTaunt);
            LoadKeyData("Assist Settings", "Melee Distance", _parsedData, ref Assist_MeleeDistance);
            LoadKeyData("Assist Settings", "Ranged Distance", _parsedData, ref Assist_RangeDistance);
            LoadKeyData("Assist Settings", "Auto-Assist Engage Percent", _parsedData, ref Assist_AutoAssistPercent);

            if (_characterClass == Data.Class.Rogue)
            {
                LoadKeyData("Rogue", "Auto-Hide (On/Off)", _parsedData, ref Rogue_AutoHide);
                LoadKeyData("Rogue", "Auto-Evade (On/Off)", _parsedData, ref Rogue_AutoEvade);
                LoadKeyData("Rogue", "Evade PctAggro", _parsedData, ref Rogue_EvadePct);
                LoadKeyData("Rogue", "Sneak Attack Discipline", _parsedData, ref Rogue_SneakAttack);
                LoadKeyData("Rogue", "PoisonPR", _parsedData, ref Rogue_PoisonPR);
                LoadKeyData("Rogue", "PoisonCR", _parsedData, ref Rogue_PoisonCR);
                LoadKeyData("Rogue", "PoisonFR", _parsedData, ref Rogue_PoisonFR);


            }

            if (_characterClass == Data.Class.Bard)
            {
                LoadKeyData("Bard", "MelodyIf", _parsedData, Bard_MelodyIfs);

            }

            LoadKeyData("Buffs", "Instant Buff", _parsedData, InstantBuffs);
            LoadKeyData("Buffs", "Self Buff", _parsedData, SelfBuffs);
            //set target on self buffs
            foreach(var buff in SelfBuffs)
            {
                buff.CastTarget = _characterName;
            }

            LoadKeyData("Buffs", "Bot Buff", _parsedData, BotBuffs);
            
           
            LoadKeyData("Buffs", "Combat Buff", _parsedData, CombatBuffs);
            LoadKeyData("Buffs", "Group Buff", _parsedData, GroupBuffs);
            LoadKeyData("Buffs", "Pet Buff", _parsedData, PetBuffs);


            LoadKeyData("Melee Abilities", "Ability", _parsedData, MeleeAbilities);


            LoadKeyData("Nukes", "Main", _parsedData, Nukes);
            LoadKeyData("TargetAE", "TargetAE", _parsedData, PBAE);
            LoadKeyData("PBAE", "PBAE", _parsedData, PBAE);

            LoadKeyData("Life Support", "Life Support", _parsedData, LifeSupport);

            LoadKeyData("DoTs on Assist", "Main", _parsedData, Dots_Assist);
            LoadKeyData("DoTs on Command", "Main", _parsedData, Dots_OnCommand);

            LoadKeyData("Debuffs", "Debuff on Assist", _parsedData, Debuffs_OnAssist);
            LoadKeyData("Debuffs", "Debuff on Command", _parsedData, Debuffs_Command);

      

            LoadKeyData("Burn", "Quick Burn", _parsedData, QuickBurns);
            LoadKeyData("Burn", "Long Burn", _parsedData, LongBurns);
            LoadKeyData("Burn", "Full Burn", _parsedData, FullBurns);


            LoadKeyData("Pets", "Pet Spell", _parsedData, PetSpell);
            LoadKeyData("Pets", "Pet Buff", _parsedData, PetBuffs);
            LoadKeyData("Pets", "Pet Heal", _parsedData, PetBuffs);
            LoadKeyData("Pets", "Pet Mend (Pct)", _parsedData, ref Pet_MendPercent);
            LoadKeyData("Pets", "Pet Taunt (On/Off)", _parsedData, ref Pet_TauntEnabled);
            LoadKeyData("Pets", "Pet Auto-Shrink (On/Off)", _parsedData, ref Pet_AutoShrink);
            LoadKeyData("Pets", "Pet Summon Combat (On/Off)", _parsedData, ref Pet_SummonCombat);
            LoadKeyData("Pets", "Pet Buff Combat (On/Off)", _parsedData, ref Pet_BuffCombat);


            LoadKeyData("Cures", "Cure", _parsedData, Cures);
            LoadKeyData("Cures", "CureAll", _parsedData, CureAll);
            LoadKeyData("Cures", "RadiantCure", _parsedData, RadiantCure);


           

            LoadKeyData("Heals", "Tank Heal", _parsedData, HealTanks);
            LoadKeyData("Heals", "Important Heal", _parsedData, HealImportantBots);
            LoadKeyData("Heals", "All Heal", _parsedData, HealAll);
            LoadKeyData("Heals", "XTarget Heal", _parsedData, HealXTarget);
            LoadKeyData("Heals", "Heal Over Time Spell", _parsedData, HealOverTime);
            LoadKeyData("Heals", "Group Heal", _parsedData, HealGroup);

            LoadKeyData("Heals", "Tank", _parsedData, HealTankTargets);
            LoadKeyData("Heals", "Important Bot", _parsedData, HealImportantBotTargets);
          
            LoadKeyData("Heals", "Pet Heal", _parsedData, PetHeals);

            //parse out the Tanks/XTargets/etc into collections via the Set method on the
            //property set method
            WhoToHealString = LoadKeyData("Heals", "Who to Heal", _parsedData);
            WhoToHoTString = LoadKeyData("Heals", "Who to HoT", _parsedData);
            LoadKeyData("Heals", "Pet Owner", _parsedData, HealPetOwners);
            LoadKeyData("Heals", "Auto Cast Necro Heal Orbs (On/Off)", _parsedData, ref HealAutoNecroOrbs);
            LoadKeyData("Off Assist Spells", "Main", _parsedData, OffAssistSpells);

            _log.Write($"Finished processing and loading: {fullPathToUse}");

        }
        

        public IniData CreateOrUpdateSettings()
        {
            //if we need to , its easier to just output the entire file. 

            IniParser.FileIniDataParser parser = e3util.CreateIniParser();
            IniData newFile = new IniData();


            newFile.Sections.AddSection("Misc");
            var section = newFile.Sections.GetSectionData("Misc");
            section.Keys.AddKey("AutoFood", "ON");
            section.Keys.AddKey("Food", "");
            section.Keys.AddKey("Drink", "");
            section.Keys.AddKey("End MedBreak in Combat(On/Off)", "Off");
            section.Keys.AddKey("AutoMedBreak (On/Off)", "Off");
            section.Keys.AddKey("Auto-Loot (On/Off)", "Off");
            section.Keys.AddKey("Anchor (Char to Anchor to)", "");

            newFile.Sections.AddSection("Assist Settings");
            section = newFile.Sections.GetSectionData("Assist Settings");
            section.Keys.AddKey("Assist Type (Melee/Ranged/Off)", "Melee");
            section.Keys.AddKey("Melee Stick Point", "Back");
            section.Keys.AddKey("Taunt(On/Off)", "Off");
            section.Keys.AddKey("SmartTaunt(On/Off)", "On");
            section.Keys.AddKey("Melee Distance", "MaxMelee");
            section.Keys.AddKey("Ranged Distance", "100");
            section.Keys.AddKey("Auto-Assist Engage Percent", "98");
            
          

            newFile.Sections.AddSection("Buffs");
            section = newFile.Sections.GetSectionData("Buffs");
            section.Keys.AddKey("Instant Buff", "");
            section.Keys.AddKey("Self Buff", "");
            section.Keys.AddKey("Bot Buff", "");
            section.Keys.AddKey("Combat Buff", "");
            section.Keys.AddKey("Group Buff", "");
            section.Keys.AddKey("Pet Buff", "");

            //section.Keys.AddKey("Cast Aura Combat (On/Off)", "Off");
            if ((_characterClass & Data.Class.Caster) != _characterClass && (_characterClass& Data.Class.Priest) !=_characterClass)
            {
                newFile.Sections.AddSection("Melee Abilities");
                section = newFile.Sections.GetSectionData("Melee Abilities");
                section.Keys.AddKey("Ability", "");
            }
            if((_characterClass & Data.Class.PureMelee)!=_characterClass && _characterClass!=Data.Class.Bard)
            {
                newFile.Sections.AddSection("Nukes");
                section = newFile.Sections.GetSectionData("Nukes");
                section.Keys.AddKey("Main", "");

                newFile.Sections.AddSection("TargetAE");
                section = newFile.Sections.GetSectionData("TargetAE");
                section.Keys.AddKey("TargetAE", "");

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
            

            newFile.Sections.AddSection("Life Support");
            section = newFile.Sections.GetSectionData("Life Support");
            section.Keys.AddKey("Life Support", "");

           
            
            newFile.Sections.AddSection("Burn");
            section = newFile.Sections.GetSectionData("Burn");
            section.Keys.AddKey("Quick Burn", "");
            section.Keys.AddKey("Quick Burn", "");
            section.Keys.AddKey("Full Burn", "");


            if (_characterClass == Data.Class.Rogue)
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

            if (_characterClass == Data.Class.Bard)
            {
                newFile.Sections.AddSection("Bard");
                section = newFile.Sections.GetSectionData("Bard");
                section.Keys.AddKey("MelodyIf", "");
            }

            if ((_characterClass & Data.Class.PetClass) == _characterClass)
            {
                newFile.Sections.AddSection("Pets");
                section = newFile.Sections.GetSectionData("Pets");
                section.Keys.AddKey("Pet Spell", "");
                section.Keys.AddKey("Pet Heal", "");
                section.Keys.AddKey("Pet Buff", "");
                section.Keys.AddKey("Pet Mend (Pct)", "");
                section.Keys.AddKey("Pet Taunt (On/Off)", "On");
                section.Keys.AddKey("Pet Auto-Shrink (On/Off)", "Off");
                section.Keys.AddKey("Pet Summon Combat (On/Off)", "Off");
                section.Keys.AddKey("Pet Buff Combat (On/Off)", "On");
            }
            

            if ((_characterClass & Data.Class.Priest) == _characterClass)
            {
                newFile.Sections.AddSection("Cures");
                section = newFile.Sections.GetSectionData("Cures");
                section.Keys.AddKey("Cure", "");
                section.Keys.AddKey("CureAll", "");
                section.Keys.AddKey("RadiantCure", "");
            }

            if((_characterClass&Data.Class.Priest)==_characterClass|| (_characterClass&Data.Class.HealHybrid) ==_characterClass )
            {
                newFile.Sections.AddSection("Heals");
                section = newFile.Sections.GetSectionData("Heals");
                section.Keys.AddKey("Tank Heal", "");
                section.Keys.AddKey("Important Heal", "");
                section.Keys.AddKey("Group Heal", "");
                section.Keys.AddKey("All Heal", "");
                section.Keys.AddKey("XTarget Heal", "");
                section.Keys.AddKey("Tank", "");
                section.Keys.AddKey("Important Bot", "");
                section.Keys.AddKey("Pet Heal", "");
                section.Keys.AddKey("Who to Heal", "");
                section.Keys.AddKey("Who to HoT", "");
                section.Keys.AddKey("Pet Owner", "");
                section.Keys.AddKey("Auto Cast Necro Heal Orbs (On/Off)", "On");

            }

            if((_characterClass& Data.Class.Priest) == _characterClass || (_characterClass & Data.Class.Caster) ==_characterClass)
            {
                newFile.Sections.AddSection("Off Assist Spells");
                section = newFile.Sections.GetSectionData("Off Assist Spells");
                section.Keys.AddKey("Main", "");
            }


            newFile.Sections.AddSection("Gimme");
            section = newFile.Sections.GetSectionData("Gimme");
            section.Keys.AddKey("Gimme", "");

            newFile.Sections.AddSection("Ifs");
            newFile.Sections.AddSection("Events");


            string filename = $"{_characterName}_{_serverName}.ini";
            string macroFile = _macroFolder + _botFolder + filename;
            string configFile = _configFolder + _botFolder + filename;
            string fullPathToUse = macroFile;
            
            if (!System.IO.File.Exists(macroFile) && !System.IO.File.Exists(configFile))
            {
                if (!System.IO.Directory.Exists(_configFolder+_botFolder))
                {
                    System.IO.Directory.CreateDirectory(_configFolder+_botFolder);
                }
                fullPathToUse = configFile;
                //file straight up doesn't exist, lets create it
                _log.Write($"Writing out new setting file: {fullPathToUse}");
                parser.WriteFile(fullPathToUse, newFile);
            }
            else
            {
                //File already exists, may need to merge in new settings lets check
                string fullFileToUse = macroFile;

                if (System.IO.File.Exists(configFile)) fullFileToUse = configFile;

                //Parse the ini file
                //Create an instance of a ini file parser
                FileIniDataParser fileIniData = e3util.CreateIniParser();
                IniData tParsedData = fileIniData.ReadFile(fullFileToUse);

                //overwrite newfile with what was already there
                _log.Write($"Merging new setting options for file: {fullFileToUse}");
                newFile.Merge(tParsedData);
                //save it it out now
                System.IO.File.Delete(fullFileToUse);
                parser.WriteFile(fullFileToUse, newFile);
            }


            return newFile;
        }


        public readonly string _characterName;
        public readonly string _serverName;
        public readonly Data.Class _characterClass;

        public Boolean Misc_AutoFoodEnabled;
        public String Misc_AutoFood;
        public string Misc_AutoDrink;
        public bool Misc_EndMedBreakInCombat;
        public bool Misc_AutoMedBreak;
        public bool Misc_AutoLootEnabled;
        public string Misc_AnchorChar = String.Empty;

        public bool Rogue_AutoHide = false;
        public bool Rogue_AutoEvade = false;
        public int Rogue_EvadePct = 0;
        public string Rogue_PoisonPR = String.Empty;
        public string Rogue_PoisonFR = String.Empty;
        public string Rogue_PoisonCR = String.Empty;
        public string Rogue_SneakAttack = String.Empty;

        public List<MelodyIfs> Bard_MelodyIfs = new List<MelodyIfs>();

        public string Assist_Type = string.Empty;
        public string Assist_MeleeStickPoint = String.Empty;
        public bool Assist_TauntEnabled = false;
        public bool Assist_SmartTaunt = true;

        /// <summary>
        /// used as MaxMelee or distance numeric
        /// </summary>
        public string Assist_MeleeDistance = "MaxMelee";
        public String Assist_RangeDistance = "100";
        public Int32 Assist_AutoAssistPercent = 98;

        //abilities
        public List<Data.Spell> MeleeAbilities = new List<Data.Spell>();
        //nukes
        public List<Data.Spell> Nukes = new List<Data.Spell>();
        //buffs
        public List<Data.Spell> InstantBuffs = new List<Data.Spell>();
        public List<Data.Spell> SelfBuffs = new List<Data.Spell>();
        public List<Data.Spell> BotBuffs = new List<Data.Spell>();
        public List<Data.Spell> GroupBuffs = new List<Data.Spell>();
        public List<Data.Spell> CombatBuffs = new List<Data.Spell>();
        public List<Data.Spell> PetBuffs = new List<Data.Spell>();


        //pets
        public List<Data.Spell> PetSpell = new List<Data.Spell>();
        public List<Data.Spell> PetHeals = new List<Data.Spell>();
        public Int32 Pet_MendPercent;
        public bool Pet_TauntEnabled;
        public bool Pet_AutoShrink;
        public bool Pet_SummonCombat;
        public bool Pet_BuffCombat;
        //debuffs
        public List<Data.Spell> Debuffs_OnAssist = new List<Data.Spell>();
        public List<Data.Spell> Debuffs_Command = new List<Data.Spell>();
        public List<Data.Spell> Debuffs_All = new List<Data.Spell>();
        //dots
        public List<Data.Spell> Dots_OnCommand = new List<Data.Spell>();
        public List<Data.Spell> Dots_Assist = new List<Data.Spell>();
        //aoe
        public List<Data.Spell> PBAE = new List<Data.Spell>();
        public List<Data.Spell> TargetAE = new List<Data.Spell>();
        //burns
        public List<Data.Spell> QuickBurns = new List<Data.Spell>();
        public List<Data.Spell> LongBurns = new List<Data.Spell>();
        public List<Data.Spell> FullBurns = new List<Data.Spell>();
        //cures
        public Boolean AutoRadiant = false;
        public List<Data.Spell> Cures = new List<Data.Spell>();
        public List<Data.Spell> CureAll = new List<Data.Spell>();
        public List<Data.Spell> RadiantCure = new List<Data.Spell>();
        //life support
        public List<Data.Spell> LifeSupport = new List<Data.Spell>();




        #region heals
        //heals
        public List<String> HealTankTargets = new List<string>();
        public List<Data.Spell> HealTanks = new List<Data.Spell>();

        public List<String> HealImportantBotTargets = new List<string>();
        public List<Data.Spell> HealImportantBots = new List<Data.Spell>();
        
        public List<Data.Spell> HealGroup = new List<Data.Spell>();

        public List<Data.Spell> HealAll = new List<Data.Spell>();
        public List<Data.Spell> HealXTarget = new List<Data.Spell>();
        public List<Data.Spell> HealPets = new List<Data.Spell>();
        public List<Data.Spell> HealOverTime = new List<Data.Spell>();
        public List<String> HealPetOwners = new List<string>();


        public System.Collections.Generic.HashSet<String> WhoToHeal = new HashSet<string>(10, StringComparer.OrdinalIgnoreCase);
        public Boolean HealAutoNecroOrbs = false;
        private string _whoToHealString;
        public string WhoToHealString
        {
            get { return _whoToHealString; }
            set
            {
                _whoToHealString = value;
                List<string> returnValue = value.Split('/').ToList();
                foreach(var who in returnValue)
                {
                    if(!WhoToHeal.Contains(who))
                    {
                        WhoToHeal.Add(who);

                    }
                }
            }
        }
        public System.Collections.Generic.HashSet<String> WhoToHoT = new HashSet<string>(10, StringComparer.OrdinalIgnoreCase);
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
        public List<Data.Spell> OffAssistSpells = new List<Data.Spell>();
        #endregion
     
       




    }
}
