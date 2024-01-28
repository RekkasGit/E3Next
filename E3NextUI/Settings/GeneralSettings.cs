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
        public string Hotkey = String.Empty;
        public bool HotKeyAlt = false;
        public bool HotKeyCtrl = false;
        public bool HotKeyEat = false;
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
        public bool UseOverlay = false;

        public bool TTS_Enabled= false;
        public bool TTS_BriefMode = false;
        public bool TTS_ChannelOOCEnabled = false;
        public bool TTS_ChannelGuildEnabled = false;
        public bool TTS_ChannelGroupEnabled = false;
        public bool TTS_ChannelSayEnabled = false;
        public bool TTS_ChannelAuctionEnabled = false;
        public bool TTS_ChannelRaidEnabled = false;
        public bool TTS_ChannelTellEnabled = false;
        public bool TTS_ChannelShoutEnabled = false;
        public bool TTS_ChannelMobSpellsEnabled = false;
		public bool TTS_ChannelPCSpellsEnabled = false;

		public string TTS_RegEx = string.Empty;
		public string TTS_RegExExclude = string.Empty;
		public string TTS_Voice = string.Empty;
        public Int32 TTS_Volume = 50;
        public Int32 TTS_Speed = 0;
        public Int32 TTS_CharacterLimit = 0;

        private IniData _parsedData;

        public GeneralSettings(string configFolder, string charName)
        {
            _fileName = $"E3UI_{charName}.ini";
            _configFolder = configFolder;

        }
        public string GetFolderPath()
        {
			string filename = $"{_configFolder}{_settingsFolder}";
            return filename;
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
            if(StartLocationX<0) StartLocationX = 0;
            LoadKeyData("General", "StartLocationY", _parsedData, ref StartLocationY);
			if (StartLocationY < 0) StartLocationY = 0;
			LoadKeyData("General", "Width", _parsedData, ref Width);
            if(Width< 200)
            {
                Width = 758;
            }
            LoadKeyData("General", "Height", _parsedData, ref Height);
			if (Height < 100)
			{
				Height = 290;
			}
			LoadKeyData("General", "ConsoleCollapsed", _parsedData, ref ConsoleCollapsed);
            LoadKeyData("General", "DynamicButtonsCollapsed", _parsedData, ref DynamicButtonsCollapsed);
            LoadKeyData("General", "UseDarkMode", _parsedData, ref UseDarkMode);
            LoadKeyData("General", "UseOverlay", _parsedData, ref UseOverlay);

			LoadKeyData("TTS", "Enabled", _parsedData, ref TTS_Enabled);
			LoadKeyData("TTS", "BriefMode", _parsedData, ref TTS_BriefMode);
			LoadKeyData("TTS", "ChannelOOCEnabled", _parsedData, ref TTS_ChannelOOCEnabled);
			LoadKeyData("TTS", "ChannelGuildEnabled", _parsedData, ref TTS_ChannelGuildEnabled);
			LoadKeyData("TTS", "ChannelGroupEnabled", _parsedData, ref TTS_ChannelGroupEnabled);
			LoadKeyData("TTS", "ChannelSayEnabled", _parsedData, ref TTS_ChannelSayEnabled);
			LoadKeyData("TTS", "ChannelAuctionEnabled", _parsedData, ref TTS_ChannelAuctionEnabled);
			LoadKeyData("TTS", "ChannelRaidEnabled", _parsedData, ref TTS_ChannelRaidEnabled);
			LoadKeyData("TTS", "ChannelTellEnabled", _parsedData, ref TTS_ChannelTellEnabled);
			LoadKeyData("TTS", "ChannelShoutEnabled", _parsedData, ref TTS_ChannelShoutEnabled);
			LoadKeyData("TTS", "ChannelMobSpellsEnabled", _parsedData, ref TTS_ChannelMobSpellsEnabled);
			LoadKeyData("TTS", "ChannelPCSpellsEnabled", _parsedData, ref TTS_ChannelPCSpellsEnabled);

			LoadKeyData("TTS", "RegEx", _parsedData, ref TTS_RegEx);
			LoadKeyData("TTS", "RegExExclude", _parsedData, ref TTS_RegExExclude);
			LoadKeyData("TTS", "VoiceName", _parsedData, ref TTS_Voice);
			LoadKeyData("TTS", "VoiceVolume", _parsedData, ref TTS_Volume);
			LoadKeyData("TTS", "VoiceSpeed", _parsedData, ref TTS_Speed);
			LoadKeyData("TTS", "CharacterLimit", _parsedData, ref TTS_CharacterLimit);

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
						keyData = section.GetKeyData("hotkey");
                        if(keyData!= null)
                        {
                            b.Hotkey = keyData.Value;
							keyData = section.GetKeyData("hotkeyalt");
                            if(keyData!= null)
                            {
                                b.HotKeyAlt=Boolean.Parse(keyData.Value);

                            }
							keyData = section.GetKeyData("hotkeyctrl");
							if (keyData != null)
							{
								b.HotKeyCtrl = Boolean.Parse(keyData.Value);

							}
							keyData = section.GetKeyData("hotkeyeat");
							if (keyData != null)
							{
								b.HotKeyEat = Boolean.Parse(keyData.Value);

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
           
            if(section==null)
            {
                CreateSettings(filename);
                LoadData();
            }
			section = _parsedData.Sections["General"];
			section.RemoveAllKeys();
			section["StartLocationX"] = StartLocationX.ToString();
            section["StartLocationY"] = StartLocationY.ToString();
            section["Width"] = Width.ToString();
            section["Height"] = Height.ToString();
            section["ConsoleCollapsed"] = ConsoleCollapsed.ToString();
            section["DynamicButtonsCollapsed"] = DynamicButtonsCollapsed.ToString();
            section["UseDarkMode"] = UseDarkMode.ToString();
            section["UseOverlay"] = UseOverlay.ToString();

			section = _parsedData.Sections["TTS"];
			if (section != null)
			{
               
				section.RemoveAllKeys();
			}
            else
            {
				_parsedData.Sections.Add(new SectionData("TTS"));
				section = _parsedData.Sections["TTS"];
			}
			section["Enabled"] = TTS_Enabled.ToString();
            section["ChannelOOCEnabled"] = TTS_ChannelOOCEnabled.ToString();
            section["ChannelGuildEnabled"] = TTS_ChannelGuildEnabled.ToString();
            section["ChannelGroupEnabled"] = TTS_ChannelGroupEnabled.ToString();
            section["ChannelSayEnabled"] = TTS_ChannelSayEnabled.ToString();
            section["ChannelAuctionEnabled"] = TTS_ChannelAuctionEnabled.ToString();
            section["ChannelRaidEnabled"] = TTS_ChannelRaidEnabled.ToString();
			section["ChannelTellEnabled"] = TTS_ChannelTellEnabled.ToString();
			section["ChannelShoutEnabled"] = TTS_ChannelShoutEnabled.ToString();
			section["ChannelMobSpellsEnabled"] = TTS_ChannelMobSpellsEnabled.ToString();
			section["ChannelPCSpellsEnabled"] = TTS_ChannelPCSpellsEnabled.ToString();


			section["RegEx"] = TTS_RegEx;
            section["RegExExclude"] = TTS_RegExExclude;
            section["VoiceName"] = TTS_Voice;
            section["VoiceVolume"] = TTS_Volume.ToString();
            section["VoiceSpeed"] = TTS_Speed.ToString();
			section["CharacterLimit"] = TTS_CharacterLimit.ToString();
			section["BriefMode"] = TTS_BriefMode.ToString();

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
                section.AddKey("hotkey", pair.Value.Hotkey.ToString());
				section.AddKey("hotkeyalt", pair.Value.HotKeyAlt.ToString());
				section.AddKey("hotkeyctrl", pair.Value.HotKeyCtrl.ToString());
				section.AddKey("hotkeyeat", pair.Value.HotKeyEat.ToString());
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
            section.Keys.AddKey("UseOverlay", "False");

			newFile.Sections.AddSection("TTS");
			section = newFile.Sections.GetSectionData("TTS");
			section.Keys.AddKey("Enabled", "False");
			section.Keys.AddKey("BriefMode", "False");
			section.Keys.AddKey("ChannelOOCEnabled", "False");
			section.Keys.AddKey("ChannelGuildEnabled", "False");
			section.Keys.AddKey("ChannelGroupEnabled", "False");
			section.Keys.AddKey("ChannelSayEnabled", "False");
			section.Keys.AddKey("ChannelAuctionEnabled", "False");
			section.Keys.AddKey("ChannelRaidEnabled", "False");
			section.Keys.AddKey("ChannelTellEnabled", "False");
			section.Keys.AddKey("ChannelShoutEnabled", "False");
			section.Keys.AddKey("ChannelMobSpellsEnabled", "False");
			section.Keys.AddKey("ChannelPCSpellsEnabled", "False");

			section.Keys.AddKey("RegEx", "");
            section.Keys.AddKey("RegExExclude", "");
            section.Keys.AddKey("VoiceName", "");
			section.Keys.AddKey("VoiceVolume", "50");
			section.Keys.AddKey("VoiceSpeed", "0");
			section.Keys.AddKey("CharacterLimit", "0");



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
