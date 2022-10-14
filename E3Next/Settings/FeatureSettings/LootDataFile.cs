using IniParser;
using IniParser.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace E3Core.Settings.FeatureSettings
{
    public class LootDataFile : BaseSettings
    {
        public static HashSet<string> _keep = new HashSet<string>(10000, StringComparer.OrdinalIgnoreCase);
        public static HashSet<string> _sell = new HashSet<string>(10000, StringComparer.OrdinalIgnoreCase);
        public static HashSet<string> _skip = new HashSet<string>(10000, StringComparer.OrdinalIgnoreCase);
        public static bool _isDirty = false;



        public static void Init()
        {



        }
        private static void RegisterEvents()
        {

        }
        public static void LoadData()
        {
            string macroFile = _macroFolder + _settingsFolder + @"Loot Settings.ini";
            string configFile = _configFolder + _settingsFolder + @"Loot Settings.ini";


            if (!System.IO.File.Exists(macroFile) && !System.IO.File.Exists(configFile))
            {
                if (!System.IO.Directory.Exists(_configFolder + _settingsFolder))
                {
                    System.IO.Directory.CreateDirectory(_configFolder + _settingsFolder);
                }
                _log.Write($"Creating new General Settings file:{configFile}");
                //file straight up doesn't exist, lets create it
                System.IO.File.CreateText(configFile);

            }
            else
            {
                //File already exists, may need to merge in new settings lets check
                string fullFileToUse = macroFile;

                if (System.IO.File.Exists(configFile)) fullFileToUse = configFile;

                FileIniDataParser fileIniData = new FileIniDataParser();
                fileIniData.Parser.Configuration.AllowDuplicateKeys = true;
                fileIniData.Parser.Configuration.OverrideDuplicateKeys = true;// so that the other ones will be put into a collection
                fileIniData.Parser.Configuration.AssigmentSpacer = "";
                _log.Write($"Reading Genearl Settings:{fullFileToUse}");
                var parsedData = fileIniData.ReadFile(fullFileToUse);
                
                //because of the old loot system, need to do 4 regex, and get more specific as we go down 
                //before ew go non specific on regex4

                //plat/copper exist
                System.Text.RegularExpressions.Regex regex1 = new System.Text.RegularExpressions.Regex(@"(.+?)\d+p\d+c", System.Text.RegularExpressions.RegexOptions.Compiled);
                //only plat exists
                System.Text.RegularExpressions.Regex regex2 = new System.Text.RegularExpressions.Regex(@"(.+?)\d+p", System.Text.RegularExpressions.RegexOptions.Compiled);
                //only copper exists
                System.Text.RegularExpressions.Regex regex3 = new System.Text.RegularExpressions.Regex(@"(.+?)\d+c", System.Text.RegularExpressions.RegexOptions.Compiled);
                //no money exists, safely to look for digits in the text.
                System.Text.RegularExpressions.Regex regex4 = new System.Text.RegularExpressions.Regex(@"([a-zA-Z\s':;`.\d]+)", System.Text.RegularExpressions.RegexOptions.Compiled);

                //(.+?)\d+p\d+c
                //(.+?)\d+p
                //(.+?)\d+c
                //([a-zA-Z\s':;`.\d]+)
                foreach (var section in parsedData.Sections)
                {
                    foreach (var key in section.Keys)
                    {
                        if(key.Value.StartsWith("Sell", StringComparison.OrdinalIgnoreCase))
                        {
                            //lets get the data out of the old format. 
                            string keyname = key.KeyName;//lets get rid of the junk
                            var match = regex1.Match(keyname);
                            if (match.Success) { MatchSuccess(_sell, match); }
                            if(!match.Success)
                            {
                                match = regex2.Match(keyname);
                                if (match.Success) { MatchSuccess(_sell, match); }
                            }
                            if (!match.Success)
                            {
                                match = regex3.Match(keyname);
                                if (match.Success) { MatchSuccess(_sell, match); }
                            }
                            if (!match.Success)
                            {
                                match = regex4.Match(keyname);
                                if (match.Success) { MatchSuccess(_sell, match); }
                            }


                        }
                        else if(key.Value.StartsWith("Keep", StringComparison.OrdinalIgnoreCase))
                        {
                            //lets get the data out of the old format. 
                            string keyname = key.KeyName;//lets get rid of the junk
                            var match = regex1.Match(keyname);
                            if (match.Success) { MatchSuccess(_keep, match); }
                            if (!match.Success)
                            {
                                match = regex2.Match(keyname);
                                if (match.Success) { MatchSuccess(_keep, match); }
                            }
                            if (!match.Success)
                            {
                                match = regex3.Match(keyname);
                                if (match.Success) { MatchSuccess(_keep, match); }
                            }
                            if (!match.Success)
                            {
                                match = regex4.Match(keyname);
                                if (match.Success) { MatchSuccess(_keep, match); }
                            }
                        }
                        else
                        {
                            //lets get the data out of the old format. 
                            string keyname = key.KeyName;//lets get rid of the junk
                            var match = regex1.Match(keyname);
                            if (match.Success) { MatchSuccess(_skip, match); }
                            if (!match.Success)
                            {
                                match = regex2.Match(keyname);
                                if (match.Success) { MatchSuccess(_skip, match); }
                            }
                            if (!match.Success)
                            {
                                match = regex3.Match(keyname);
                                if (match.Success) { MatchSuccess(_skip, match); }
                            }
                            if (!match.Success)
                            {
                                match = regex4.Match(keyname);
                                if (match.Success) { MatchSuccess(_skip, match); }
                            }

                        }
                    }
                }
            }

          
        }

        private static void MatchSuccess(HashSet<string> hash, Match match)
        {
            if (match.Groups.Count > 1)
            {
                string matchValue = match.Groups[1].Value.Trim();
                matchValue = matchValue.Replace(";", ":");
                if (!hash.Contains(matchValue))
                {
                    hash.Add(matchValue);
                }
            }
        }
        public static void SaveData()
        {

            IniParser.FileIniDataParser parser = new IniParser.FileIniDataParser();
            parser.Parser.Configuration.AllowDuplicateKeys = true;
            parser.Parser.Configuration.OverrideDuplicateKeys = true;// so that the other ones will be put into a collection
            parser.Parser.Configuration.AssigmentSpacer = "";
            IniData newFile = new IniData();
            //lets create all the sections in alpha order.

            //create sorted lists

            List<string> _keepSorted = _keep.OrderBy(x => x).ToList();
            List<string> _sellSorted = _sell.OrderBy(x => x).ToList();
            List<string> _skipSorted = _skip.OrderBy(x => x).ToList();

            for (char c = 'A'; c <= 'Z'; c++)
            {
                string tc = c.ToString();
                newFile.Sections.AddSection(tc);
                var section = newFile.Sections.GetSectionData(tc);
                
                foreach(string hashvalue in _keepSorted)
                {
                    if (hashvalue.StartsWith(tc))
                    {
                        section.Keys.AddKey(hashvalue, "Keep");
                    }
                }
                foreach (string hashvalue in _sellSorted)
                {
                    if (hashvalue.StartsWith(tc))
                    {
                        section.Keys.AddKey(hashvalue, "Sell");
                    }
                }
                foreach (string hashvalue in _skipSorted)
                {
                    if (hashvalue.StartsWith(tc))
                    {
                        section.Keys.AddKey(hashvalue, "Skip");
                    }
                }

            }
            string macroFile = _macroFolder + _settingsFolder + @"Loot Settings.ini";
            string configFile = _configFolder + _settingsFolder + @"Loot Settings.ini";
            //File already exists, may need to merge in new settings lets check
            string fullFileToUse = macroFile;

            if (System.IO.File.Exists(configFile)) fullFileToUse = configFile;

            //Parse the ini file
            //Create an instance of a ini file parser
            FileIniDataParser fileIniData = new FileIniDataParser();
            fileIniData.Parser.Configuration.AllowDuplicateKeys = true;
            fileIniData.Parser.Configuration.OverrideDuplicateKeys = true;// so that the other ones will be put into a collection
            fileIniData.Parser.Configuration.AssigmentSpacer = "";
            System.IO.File.Delete(fullFileToUse);
            parser.WriteFile(fullFileToUse, newFile);

        }
    }
}
