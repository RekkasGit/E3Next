using E3Core.Utility;
using IniParser;
using IniParser.Model;
using System;

namespace E3Core.Settings
{
    public class UISettings : BaseSettings, IBaseSettings
    {
        public string UI_Theme = "DarkTeal";
        public float UI_Rounding = 8.0f;
        
        private string _filename = String.Empty;

        public UISettings()
        {
            LoadData();
        }

        public void LoadData()
        {
            _filename = GetSettingsFilePath("UI Settings.ini");
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

            // Load the data
            if (parsedData == null)
            {
                throw new Exception("Could not load UI Settings file");
            }

            LoadKeyData("UI Theme", "E3 Config", parsedData, ref UI_Theme);
            LoadKeyData("UI Theme", "Rounding", parsedData, ref UI_Rounding);
        }

        public void SaveSettings()
        {
            try
            {
                FileIniDataParser fileIniData = e3util.CreateIniParser();
                IniData parsedData;

                if (System.IO.File.Exists(_filename))
                {
                    parsedData = fileIniData.ReadFile(_filename);
                }
                else
                {
                    parsedData = CreateSettings(_filename);
                    return; // CreateSettings already saves the file
                }

                // Update values
                var section = parsedData.Sections.GetSectionData("UI Theme");
                if (section == null)
                {
                    parsedData.Sections.AddSection("UI Theme");
                    section = parsedData.Sections.GetSectionData("UI Theme");
                }
                
                if (section != null)
                {
                    var themeKey = section.Keys.GetKeyData("E3 Config");
                    if (themeKey != null)
                    {
                        themeKey.ValueList.Clear();
                        themeKey.ValueList.Add(UI_Theme);
                    }
                    else
                    {
                        section.Keys.AddKey("E3 Config", UI_Theme);
                    }

                    var roundingKey = section.Keys.GetKeyData("Rounding");
                    if (roundingKey != null)
                    {
                        roundingKey.ValueList.Clear();
                        roundingKey.ValueList.Add(UI_Rounding.ToString("F1"));
                    }
                    else
                    {
                        section.Keys.AddKey("Rounding", UI_Rounding.ToString("F1"));
                    }
                }

                // Save to file
                fileIniData.WriteFile(_filename, parsedData);
                _log.Write($"Saved UI Settings to {_filename}");
            }
            catch (Exception ex)
            {
                _log.Write($"Failed to save UI Settings: {ex.Message}", MonoCore.Logging.LogLevels.Error);
            }
        }

        public IniData CreateSettings(string filename)
        {
            IniParser.FileIniDataParser parser = e3util.CreateIniParser();
            IniData newFile = new IniData();

            // UI Theme section with per-window theme settings
            newFile.Sections.AddSection("UI Theme");
            var section = newFile.Sections.GetSectionData("UI Theme");
            section.Keys.AddKey("E3 Config", "DarkTeal");
            section.Keys.AddKey("Rounding", "8.0");

            if (!System.IO.File.Exists(filename))
            {
                if (!System.IO.Directory.Exists(_configFolder + _settingsFolder))
                {
                    System.IO.Directory.CreateDirectory(_configFolder + _settingsFolder);
                }
                _log.Write($"Creating new UI Settings file: {filename}");
                parser.WriteFile(filename, newFile);
            }
            else
            {
                // File exists, just return what's there
                FileIniDataParser fileIniData = e3util.CreateIniParser();
                IniData parsedData = fileIniData.ReadFile(filename);
                return parsedData;
            }

            return newFile;
        }

        // Helper method to load float values from INI
        private static void LoadKeyData(string sectionKey, string Key, IniData parsedData, ref float valueToSet)
        {
            _log.Write($"{sectionKey} {Key}");
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
                            if (float.TryParse(data, out float result))
                            {
                                valueToSet = result;
                            }
                        }
                    }
                }
            }
        }
        
        // Helper method to load string values from INI
        private static void LoadKeyData(string sectionKey, string Key, IniData parsedData, ref string valueToSet)
        {
            _log.Write($"{sectionKey} {Key}");
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
                            break;
                        }
                    }
                }
            }
        }
    }
}
