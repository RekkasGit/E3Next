using E3Core.Utility;
using IniParser;
using IniParser.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace E3Core.Settings.FeatureSettings
{
    public class GlobalCursorDelete : BaseSettings, IBaseSettings
    {
        private string _filename = String.Empty;
        public List<String> Cursor_Delete = new List<string>();

        public GlobalCursorDelete()
        {
            LoadData();
        }
        
        public void LoadData()
        {
            _filename = GetSettingsFilePath("GlobalCursorDelete.ini");
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
            
            //have the data now!
            if (parsedData == null)
            {
                throw new Exception("Could not load Global Cursor Delete file");
            }
            
            LoadKeyData("Cursor Delete", "Delete", parsedData, Cursor_Delete);
        }
        
        public IniData CreateSettings(string filename)
        {
            IniParser.FileIniDataParser parser = e3util.CreateIniParser();
            IniData newFile = new IniData();
            
            newFile.Sections.AddSection("Cursor Delete");
            var section = newFile.Sections.GetSectionData("Cursor Delete");
            
            if (!System.IO.File.Exists(filename))
            {
                if (!System.IO.Directory.Exists(_configFolder + _settingsFolder))
                {
                    System.IO.Directory.CreateDirectory(_configFolder + _settingsFolder);
                }
                _log.Write($"Creating new Global Cursor Delete file:{filename}");
                //file straight up doesn't exist, lets create it
                parser.WriteFile(filename, newFile);
            }
            else
            {
                //some reason we were called when this already exists, just return what is there.
                FileIniDataParser fileIniData = e3util.CreateIniParser();
                IniData parsedData = fileIniData.ReadFile(filename);
                
                return parsedData;
            }
            
            return newFile;
        }
    }
}
