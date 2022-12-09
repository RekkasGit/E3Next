using E3Core.Processors;
using E3Core.Utility;
using IniParser;
using IniParser.Model;
using MonoCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace E3Core.Settings
{
    public class AdvSettingInvokeAttribute : Attribute
    {
    }
    public class ClassInvokeAttribute : Attribute
    {
        private Data.Class _currentClass;

        public Data.Class CurrentClass
        {
            get { return _currentClass; }
            set { _currentClass = value; }
        }


        public ClassInvokeAttribute(Data.Class classType)
        {
            _currentClass = classType;
        }

    }
    public class AdvancedSettings : BaseSettings
    {

        //  public static Dictionary<string, Action> _methodLookup = new Dictionary<string, Action>(StringComparer.OrdinalIgnoreCase);

        public static Dictionary<string, Action> MethodLookup = new Dictionary<string, Action>(StringComparer.OrdinalIgnoreCase);
        public static Dictionary<string,Action> ClassMethodLookup = new Dictionary<string, Action>(StringComparer.OrdinalIgnoreCase);
        public static Dictionary<string, List<string>> ClassMethodsAsStrings = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

        public AdvancedSettings()
        {

            Reset();
            LoadData();
        
        }
        public void Reset()
        {
            MethodLookup.Clear();
            ClassMethodLookup.Clear();
            ClassMethodsAsStrings.Clear();
        }
        public void LoadData()
        {
            InitMethods();
            string filename = GetSettingsFilePath("Advanced Settings.ini");
      
            IniData parsedData;

            FileIniDataParser fileIniData = e3util.CreateIniParser();

            if (!System.IO.File.Exists(filename))
            {
                if (!System.IO.Directory.Exists(_configFolder + _settingsFolder))
                {
                    System.IO.Directory.CreateDirectory(_configFolder + _settingsFolder);
                }
                _log.Write($"Creating new Adv settings:{filename}");
                CreateSettings();
            }
            parsedData = fileIniData.ReadFile(filename);


            foreach (var shortname in Data.Classes.ClassShortNames)
            {
                ClassMethodsAsStrings.Add(shortname, new List<string>());
                LoadKeyData($"{shortname} Functions", $"{shortname} Function", parsedData, ClassMethodsAsStrings[shortname]);
            }
         
        }
        public void InitMethods()
        {


            //var method = typeof(AdvancedSettings).GetMethod("check_summonitems");
            //var func = (Action)method.CreateDelegate(typeof(Action), this);

            //find all methods in all classes that have the adv setting invoke attribute. 
            var methods = AppDomain.CurrentDomain.GetAssemblies() 
            .SelectMany(x => x.GetTypes()) 
            .Where(x => x.IsClass)
            .SelectMany(x => x.GetMethods()) 
            .Where(x => x.GetCustomAttributes(typeof(AdvSettingInvokeAttribute), false).FirstOrDefault() != null); // returns only methods that have the InvokeAttribute

            foreach (var foundMethod in methods) // iterate through all found methods
            {
                //these are static don't need to create an instance
                var func = (Action)foundMethod.CreateDelegate(typeof(Action));
                MethodLookup.Add(foundMethod.Name, func);
                
            }
            methods = AppDomain.CurrentDomain.GetAssemblies()
           .SelectMany(x => x.GetTypes())
           .Where(x => x.IsClass)
           .SelectMany(x => x.GetMethods())
           .Where(x => x.GetCustomAttributes(typeof(ClassInvokeAttribute),false).FirstOrDefault() != null); // returns only methods that have the InvokeAttribute

            foreach (var foundMethod in methods) // iterate through all found methods
            {
                //if the attribute class is the same as our current class.
                Data.Class configClass = ((ClassInvokeAttribute)foundMethod.GetCustomAttribute(typeof(ClassInvokeAttribute), false)).CurrentClass;
                if ((E3.CurrentClass &configClass) == E3.CurrentClass)
                { 
                    //these are static don't need to create an instance
                    var func = (Action)foundMethod.CreateDelegate(typeof(Action));
                    ClassMethodLookup.Add(foundMethod.Name, func);
                }
            }
        }
        public  void CreateSettings()
        {
            //in the begining we couldn't save multi key, we can now... this is a previous artificat of that situation.
            //not going to create the adv ini, as default /ini cannot create the multi key format. its almost never recreated from scratch anyway
            //if we need to , its easier to just output the entire file. 

            string filename = $"Advanced Settings.ini";
            string macroFile = _macroFolder + _settingsFolder + filename;
            string configFile = _configFolder + _settingsFolder + filename;
            string fullPathToUse = macroFile;
            if (!System.IO.File.Exists(macroFile) && !System.IO.File.Exists(configFile))
            {
                if (!System.IO.Directory.Exists(_configFolder+_settingsFolder))
                {   
                    System.IO.Directory.CreateDirectory(_configFolder+ _settingsFolder);
                }
                //file straight up doesn't exist, lets create it
                System.IO.File.WriteAllText(configFile, filePayload);

            }

        }

       
      

       
        string filePayload =
       @"
[WAR Functions]
WAR Function=check_Nukes
WAR Function=check_Tanking
WAR Function=check_Burns
WAR Function=check_Buffs
WAR Function=check_Food
WAR Function=check_Gimme
[BRD Functions]
BRD Function=check_Burns
BRD Function=check_Buffs
BRD Function=check_bard_mez
[BST Functions]
BST Function=check_Burns
BST Function=check_Debuffs
BST Function=check_Heals
BST Function=check_Cures
BST Function=check_Buffs
BST Function=check_Nukes
BST Function=check_Pets
BST Function=check_DoTs
BST Function=check_Food
[BER Functions]
BER Function=check_Burns
BER Function=check_Buffs
BER Function=check_Food
[CLR Functions]
CLR Function=check_DivineArb
CLR Function=check_Cures
CLR Function=check_Nukes
CLR Function=check_celestialRegen
CLR Function=check_Buffs
CLR Function=check_Burns
CLR Function=check_Yaulp
CLR Function=check_Debuffs
CLR Function=check_Gimme
CLR Function=check_Pets
CLR Function=check_SummonItems
CLR Function=check_Food
[DRU Functions]
DRU Function=check_Cures
DRU Function=check_Buffs
DRU Function=check_Burns
DRU Function=check_Debuffs
DRU Function=check_DoTs
DRU Function=check_OffAssistSpells
DRU Function=check_Nukes
DRU Function=check_Pets
DRU Function=check_Gimme
DRU Function=check_Forage
[ENC Functions]
ENC Function=check_Charm
ENC Function=check_Burns
ENC Function=check_Debuffs
ENC Function=check_DoTs
ENC Function=check_Nukes
ENC Function=check_Buffs
ENC Function=check_Rune
ENC Function=check_AE
ENC Function=check_Mez
ENC Function=check_Pets
ENC Function=check_Food
ENC Function=check_Pets
ENC Function=check_Gimme
[MAG Functions]
MAG Function=check_Burns
MAG Function=check_Debuffs
MAG Function=check_Nukes
MAG Function=check_Pets
MAG Function=check_Ae
MAG Function=check_Buffs
MAG Function=check_SummonItems
MAG Function=check_Food
MAG Function=check_Gimme
[MNK Functions]
MNK Function=check_Burns
MNK Function=check_Buffs
MNK Function=check_Food
[NEC Functions]
NEC Function=check_Burns
NEC Function=check_Debuffs
NEC Function=check_DoTs
NEC Function=check_OffAssistSpells
NEC Function=check_manaDump
NEC Function=check_lifeTap
NEC Function=check_Nukes
NEC Function=check_Buffs
NEC Function=check_Pets
NEC Function=check_Food
NEC Function=check_Gimme
[PAL Functions]
PAL Function=check_Tanking
PAL Function=check_Yaulp
PAL Function=check_Burns
PAL Function=check_Heals
PAL Function=check_Cures
PAL Function=check_Buffs
PAL Function=check_Nukes
PAL Function=check_Food
[RNG Functions]
RNG Function=check_Burns
RNG Function=check_Heals
RNG Function=check_DoTs
RNG Function=check_Nukes
RNG Function=check_Buffs
RNG Function=check_Food
RNG Function=check_Buffs
RNG Function=check_Food
RNG Function=check_Gimme
[ROG Functions]
ROG Function=check_Burns
ROG Function=check_Buffs
ROG Function=check_Food
[SHD Functions]
SHD Function=check_Tanking
SHD Function=check_Burns
SHD Function=check_lifeTap
SHD Function=check_Debuffs
SHD Function=check_DoTs
SHD Function=check_Nukes
SHD Function=check_Buffs
SHD Function=check_Pets
SHD Function=check_Food
SHD Function=check_Pets
[SHM Functions]
SHM Function=check_Burns
SHM Function=check_Debuffs
SHM Function=check_Cures
SHM Function=check_Buffs
SHM Function=check_DoTs
SHM Function=check_OffAssistSpells
SHM Function=check_Nukes
SHM Function=check_Pets
SHM Function=check_Canni
SHM Function=check_Buffs
SHM Function=check_Food
SHM Function=check_Gimme
[WIZ Functions]
WIZ Function=check_Burns
WIZ Function=check_Buffs
WIZ Function=check_AE
WIZ Function=check_Nukes
WIZ Function=check_Harvest
WIZ Function=check_Food
WIZ Function=check_Gimme
";

    }
}
