using E3Core.Processors;
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
            IniData parsedData;
            FileIniDataParser fileIniData = new FileIniDataParser();
            fileIniData.Parser.Configuration.AllowDuplicateKeys = true;
            fileIniData.Parser.Configuration.OverrideDuplicateKeys = true;// so that the other ones will be put into a collection
            fileIniData.Parser.Configuration.AssigmentSpacer = "";
            if (!System.IO.File.Exists(configFile) && !System.IO.File.Exists(macroFile))
            {
                if (!System.IO.Directory.Exists(_configFolder+_botFolder))
                {
                    System.IO.Directory.CreateDirectory(_configFolder+_botFolder);
                }

                fullPathToUse = configFile;
                _log.Write($"Settings not found creating new settings: {fullPathToUse}");
                parsedData = CreateOrUpdateSettings();
            }
            else
            {
                if (System.IO.File.Exists(configFile)) fullPathToUse = configFile;

                //Parse the ini file
                //Create an instance of a ini file parser

                _log.Write($"Loading up {fullPathToUse}");
                parsedData = fileIniData.ReadFile(fullPathToUse);
            }

            LoadKeyData("Misc", "AutoFood", parsedData, ref Misc_AutoFoodEnabled);
            LoadKeyData("Misc", "Food", parsedData, ref Misc_AutoFood);
            LoadKeyData("Misc", "Drink", parsedData, ref Misc_AutoDrink);
            LoadKeyData("Misc", "End MedBreak in Combat(On/Off)", parsedData, ref Misc_EndMedBreakInCombat);
            LoadKeyData("Misc", "AutoMedBreak (On/Off)", parsedData, ref Misc_AutoMedBreak);
            LoadKeyData("Misc", "Auto-Loot (On/Off)", parsedData, ref Misc_AutoLootEnabled);
            LoadKeyData("Misc", "Anchor (Char to Anchor to)", parsedData, ref Misc_AnchorChar);

            LoadKeyData("Buffs", "Instant Buff", parsedData, InstantBuffs);
            LoadKeyData("Buffs", "Self Buff", parsedData, SelfBuffs);
            LoadKeyData("Buffs", "Bot Buff", parsedData, BotBuffs);
            
           
            LoadKeyData("Buffs", "Combat Buff", parsedData, CombatBuffs);
            LoadKeyData("Buffs", "Group Buff", parsedData, GroupBuffs);
            LoadKeyData("Buffs", "Pet Buff", parsedData, PetBuffs);


            LoadKeyData("Melee Abilities", "Ability", parsedData, MeleeAbilities);


            LoadKeyData("Nukes", "Main", parsedData, Nukes);
            LoadKeyData("TargetAE", "TargetAE", parsedData, PBAE);
            LoadKeyData("PBAE", "PBAE", parsedData, PBAE);

            LoadKeyData("Life Support", "Life Support", parsedData, LifeSupport);

            LoadKeyData("DoTs on Assist", "Main", parsedData, Dots_Assist);
            LoadKeyData("DoTs on Command", "Main", parsedData, Dots_OnCommand);

            LoadKeyData("Debuffs", "Debuff on Assist", parsedData, Debuffs_OnAssist);
            LoadKeyData("Debuffs", "Debuff on Command", parsedData, Debuffs_Command);

      

            LoadKeyData("Burn", "Quick Burn", parsedData, QuickBurns);
            LoadKeyData("Burn", "Long Burn", parsedData, LongBurns);
            LoadKeyData("Burn", "Full Burn", parsedData, FullBurns);


            LoadKeyData("Pet", "Pet Spell", parsedData, PetSpell);
            LoadKeyData("Pet", "Pet Buff", parsedData, PetBuffs);
            LoadKeyData("Pet", "Pet Heal", parsedData, PetBuffs);
            LoadKeyData("Pet", "Pet Mend (Pct)", parsedData, ref Pet_MendPercent);
            LoadKeyData("Pet", "Pet Taunt (On/Off)", parsedData, ref Pet_TauntEnabled);
            LoadKeyData("Pet", "Pet Auto-Shrink (On/Off)", parsedData, ref Pet_AutoShrink);
            LoadKeyData("Pet", "Pet Summon Combat (On/Off)", parsedData, ref Pet_SummonCombat);
            LoadKeyData("Pet", "Pet Buff Combat (On/Off)", parsedData, ref Pet_BuffCombat);


            LoadKeyData("Cures", "Cure", parsedData, Cures);
            LoadKeyData("Cures", "CureAll", parsedData, CureAll);
            LoadKeyData("Cures", "RadiantCure", parsedData, RadiantCure);




            LoadKeyData("Heals", "Tank Heal", parsedData, HealTanks);
            LoadKeyData("Heals", "Important Heal", parsedData, HealImportantBots);
            LoadKeyData("Heals", "All Heal", parsedData, HealAll);
            LoadKeyData("Heals", "XTarget Heal", parsedData, HealXTarget);
            LoadKeyData("Heals", "Heal Over Time Spell", parsedData, HealOverTime);
            LoadKeyData("Heals", "Group Heal", parsedData, HealGroup);

            LoadKeyData("Heals", "Tank", parsedData, HealTankTargets);
            LoadKeyData("Heals", "Important Bot", parsedData, HealImportantBotTargets);
          
            LoadKeyData("Heals", "Pet Heal", parsedData, PetHeals);

            //parse out the Tanks/XTargets/etc into collections via the Set method on the
            //property set method
            WhoToHealString = LoadKeyData("Heals", "Who to Heal", parsedData);
            WhoToHoTString = LoadKeyData("Heals", "Who to HoT", parsedData);
            LoadKeyData("Heals", "Pet Owner", parsedData, HealPetOwners);
            LoadKeyData("Heals", "Auto Cast Necro Heal Orbs (On/Off)", parsedData, ref HealAutoNecroOrbs);
            LoadKeyData("Off Assist Spells", "Main", parsedData, OffAssistSpells);

            _log.Write($"Finished processing and loading: {fullPathToUse}");

        }
        

        public IniData CreateOrUpdateSettings()
        {
            //if we need to , its easier to just output the entire file. 

            IniParser.FileIniDataParser parser = new IniParser.FileIniDataParser();
            parser.Parser.Configuration.AllowDuplicateKeys = true;
            parser.Parser.Configuration.OverrideDuplicateKeys = true;// so that the other ones will be put into a collection
            parser.Parser.Configuration.AssigmentSpacer = "";
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

            if((_characterClass & Data.Class.PetClass) == _characterClass)
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
                FileIniDataParser fileIniData = new FileIniDataParser();
                fileIniData.Parser.Configuration.AllowDuplicateKeys = true;
                fileIniData.Parser.Configuration.OverrideDuplicateKeys = true;// so that the other ones will be put into a collection
                fileIniData.Parser.Configuration.AssigmentSpacer = "";
                IniData parsedData = fileIniData.ReadFile(fullFileToUse);

                //overwrite newfile with what was already there
                _log.Write($"Merging new setting options for file: {fullFileToUse}");
                newFile.Merge(parsedData);
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
