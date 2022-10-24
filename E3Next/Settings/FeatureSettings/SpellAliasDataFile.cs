using E3Core.Processors;
using E3Core.Utility;
using IniParser;
using IniParser.Model;
using System;
using System.Collections.Generic;



namespace E3Core.Settings.FeatureSettings
{
    public class SpellAliasDataFile:BaseSettings
    {
        IniData _doorData;
        public void LoadData()
        {
            string fileName = GetSettingsFilePath(@"Spell Aliases.ini");


            FileIniDataParser fileIniData = e3util.CreateIniParser();
            _doorData = fileIniData.ReadFile(fileName);

        }
        public Dictionary<string,string> GetClassAliases()
        {
            Dictionary<string, string> returnValue = new Dictionary<string, string>();
            var section = _doorData.Sections[E3._currentLongClassString];
            if (section != null)
            {
                foreach(var kvp in section)
                {
                    returnValue.Add(kvp.KeyName, kvp.Value);
                }
            }
            return returnValue;
        }
     
    }
}
