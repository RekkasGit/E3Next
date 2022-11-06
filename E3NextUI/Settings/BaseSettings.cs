using IniParser;
using IniParser.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace E3NextUI.Settings
{
    public class BaseSettings
    {

        public BaseSettings()
        {




        }

        public static FileIniDataParser CreateIniParser()
        {
            var fileIniData = new FileIniDataParser();
            fileIniData.Parser.Configuration.AllowDuplicateKeys = true;
            fileIniData.Parser.Configuration.OverrideDuplicateKeys = true;// so that the other ones will be put into a collection
            fileIniData.Parser.Configuration.AssigmentSpacer = "";
            fileIniData.Parser.Configuration.CaseInsensitive = true;

            return fileIniData;
        }
        public static void LoadKeyData(string sectionKey, string Key, IniData parsedData, ref string valueToSet)
        {
            var section = parsedData.Sections[sectionKey];
            if (section != null)
            {
                var keyData = section.GetKeyData(Key);
                if (keyData != null)
                {
                    foreach (var data in keyData.ValueList)
                    {
                        if (!String.IsNullOrWhiteSpace(data))
                        {
                            valueToSet = data;
                        }
                    }
                }
            }
        }
        public static void LoadKeyData<K, V>(string sectionKey, string Key, IniData parsedData, Dictionary<K, V> dictionary)
        {
            var section = parsedData.Sections[sectionKey];
            if (section != null)
            {
                var keyData = section.GetKeyData(Key);
                if (keyData != null)
                {
                    foreach (var data in keyData.ValueList)
                    {
                        if (!string.IsNullOrWhiteSpace(data))
                        {
                            var splits = data.Split('/');
                            if (!(splits.Length > 1)) continue;
                            dictionary.Add((K)(object)splits[0], (V)(object)splits[1]);
                        }
                    }
                }
            }
        }
        public static string LoadKeyData(string sectionKey, string Key, IniData parsedData)
        {
             var section = parsedData.Sections[sectionKey];
            if (section != null)
            {
                var keyData = section.GetKeyData(Key);
                if (keyData != null)
                {
                    foreach (var data in keyData.ValueList)
                    {
                        if (!String.IsNullOrWhiteSpace(data))
                        {
                            return data;
                        }
                    }
                }
            }
            return String.Empty;
        }
        public static void LoadKeyData(string sectionKey, string Key, IniData parsedData, ref Boolean valueToSet)
        {
            var section = parsedData.Sections[sectionKey];
            if (section != null)
            {
                var keyData = section.GetKeyData(Key);
                if (keyData != null)
                {
                    foreach (var data in keyData.ValueList)
                    {
                        if (!String.IsNullOrWhiteSpace(data))
                        {
                            if (data.Equals("Off") || data.Equals("False"))
                            {
                                valueToSet = false;
                            }
                            else if (data.Equals("On") || data.Equals("True"))
                            {
                                valueToSet = true;
                            }
                            else
                            {
                                valueToSet = false;
                            }

                        }
                    }
                }
            }
        }
        public static void LoadKeyData(string sectionKey, string Key, IniData parsedData, ref Int32 valueToSet)
        {
            var section = parsedData.Sections[sectionKey];
            if (section != null)
            {
                var keyData = section.GetKeyData(Key);
                if (keyData != null)
                {
                    foreach (var data in keyData.ValueList)
                    {
                        if (!String.IsNullOrWhiteSpace(data))
                        {
                            valueToSet = Int32.Parse(data);

                        }
                    }
                }
            }
        }

        public static void LoadKeyData(string sectionKey, string Key, IniData parsedData, List<String> collectionToAddTo)
        {
            var section = parsedData.Sections[sectionKey];
            if (section != null)
            {
                var keyData = section.GetKeyData(Key);
                if (keyData != null)
                {
                    foreach (var data in keyData.ValueList)
                    {
                        if (!String.IsNullOrWhiteSpace(data))
                        {
                            collectionToAddTo.Add(data);
                        }

                    }
                }
            }
        }

    }
}
