using E3NextUI.Server;
using IniParser;
using IniParser.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace E3NextUI.Settings
{
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
            section.Keys.AddKey("Width", "");
            section.Keys.AddKey("Height", "");
            section.Keys.AddKey("ConsoleCollapsed", "False");


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
