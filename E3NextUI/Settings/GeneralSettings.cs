using E3NextUI.Server;
using IniParser;
using IniParser.Model;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace E3NextUI.Settings
{
    public class DynamicButton
    {
        public string Name = String.Empty;
        public List<string> Commands = new List<string>();
    }
    public class GeneralSettings:BaseSettings
    {

        string _fileName;
        string _configFolder;
        string _settingsFolder = @"\e3 Macro Inis\";



        public Int32 StartLocationX;
        public Int32 StartLocationY;
        public Int32 Width;
        public Int32 Height;
        public bool ConsoleCollapsed;
        public bool DynamicButtonsCollapsed;
        public Dictionary<string, DynamicButton> DynamicButtons = new Dictionary<string, DynamicButton>(StringComparer.OrdinalIgnoreCase);
        public bool UseDarkMode = true;

        private IniData _parsedData;

        public GeneralSettings(string configFolder, string charName)
        {
            _fileName = $"E3UI_{charName}.ini";
            _configFolder = configFolder;

        }
        public void LoadData()
        {
            string filename = $"{_configFolder}{_settingsFolder}{_fileName}";

            //does our file exist? if not, create it. 

           
            FileIniDataParser fileIniData = CreateIniParser();

            if (!System.IO.File.Exists(filename))
            {
                if (!System.IO.Directory.Exists(_configFolder + _settingsFolder))
                {
                    System.IO.Directory.CreateDirectory(_configFolder + _settingsFolder);
                }
      
               CreateSettings(filename);
            }
           
            _parsedData = fileIniData.ReadFile(filename);

            LoadKeyData("General", "StartLocationX", _parsedData, ref StartLocationX);
            LoadKeyData("General", "StartLocationY", _parsedData, ref StartLocationY);
            LoadKeyData("General", "Width", _parsedData, ref Width);
            LoadKeyData("General", "Height", _parsedData, ref Height);
            LoadKeyData("General", "ConsoleCollapsed", _parsedData, ref ConsoleCollapsed);
            LoadKeyData("General", "DynamicButtonsCollapsed", _parsedData, ref DynamicButtonsCollapsed);
            LoadKeyData("General", "UseDarkMode", _parsedData, ref UseDarkMode);

            for (Int32 i = 0; i < 25; i++)
            {
                var section = _parsedData.Sections[$"dynamicButton_{i+1}"];
                if (section != null)
                {
                    var keyData = section.GetKeyData("Name");
                    if (keyData != null)
                    {
                        DynamicButton b = new DynamicButton();
                        b.Name = keyData.Value;
                        keyData = section.GetKeyData("line");
                        if (keyData!= null)
                        {
                            foreach (var data in keyData.ValueList)
                            {
                                if (!string.IsNullOrWhiteSpace(data))
                                {
                                    b.Commands.Add(data);
                                }
                            }

                        }
                        DynamicButtons.Add($"dynamicButton_{i + 1}", b);
                    }
                }
            }
        }
        public void SaveData()
        {
            string filename = $"{_configFolder}{_settingsFolder}{_fileName}";

            var section = _parsedData.Sections["General"];
            section.RemoveAllKeys();
            if (section == null)
            {
                CreateSettings(filename);
                LoadData();
            }
            section = _parsedData.Sections["General"];
            section["StartLocationX"] = StartLocationX.ToString();
            section["StartLocationY"] = StartLocationY.ToString();
            section["Width"] = Width.ToString();
            section["Height"] = Height.ToString();
            section["ConsoleCollapsed"] = ConsoleCollapsed.ToString();
            section["DynamicButtonsCollapsed"] = DynamicButtonsCollapsed.ToString();
            section["UseDarkMode"] = UseDarkMode.ToString();

            foreach (var pair in DynamicButtons)
            {
                section = _parsedData.Sections[pair.Key];
                if(section==null)
                {
                    _parsedData.Sections.AddSection(pair.Key);
                    section = _parsedData.Sections[pair.Key];
                }
                section.RemoveAllKeys();
                section.AddKey("name", pair.Value.Name);
                foreach (var command in pair.Value.Commands)
                {
                    section.AddKey("line", command);
                }
            }

            FileIniDataParser fileIniData = CreateIniParser();
            fileIniData.WriteFile(filename, _parsedData);
        }
        public void CreateSettings(string filename)
        {

            IniParser.FileIniDataParser parser = CreateIniParser();
            IniData newFile = new IniData();


            newFile.Sections.AddSection("General");
            var section = newFile.Sections.GetSectionData("General");
            section.Keys.AddKey("StartLocationX", "");
            section.Keys.AddKey("StartLocationY", "");
            section.Keys.AddKey("Width", "1103");
            section.Keys.AddKey("Height", "302");
            section.Keys.AddKey("ConsoleCollapsed", "True");
            section.Keys.AddKey("DynamicButtonsCollapsed", "False");
            section.Keys.AddKey("UseDarkMode", "True");


            if (!System.IO.File.Exists(filename))
            {
                if (!System.IO.Directory.Exists(_configFolder + _settingsFolder))
                {
                    System.IO.Directory.CreateDirectory(_configFolder + _settingsFolder);
                }
                //file straight up doesn't exist, lets create it
                parser.WriteFile(filename, newFile);
            }
           
        }


    }
}
