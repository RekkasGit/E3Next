using E3Core.Utility;
using IniParser;
using IniParser.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;


namespace E3Core.Settings.FeatureSettings
{
	public class GlobalIfs : BaseSettings, IBaseSettings
	{
		private string _filename = String.Empty;
		public SortedDictionary<string, string> Ifs = new SortedDictionary<string, string>();
		public GlobalIfs()
		{
			LoadData();
		}
		public void LoadData()
		{

			_filename = GetSettingsFilePath("GlobalIfs.ini");
			if (!String.IsNullOrEmpty(CurrentSet))
			{
				_filename = _filename.Replace(".ini", "_" + CurrentSet + ".ini");
			}
			IniData parsedData;

			FileIniDataParser fileIniData = e3util.CreateIniParser();
			try
			{
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

			}
			catch(Exception)
			{
				//if the file doesn't exist but you restart all at the same time, they can fight over the creation
				//and some will throw exceptions, chill out and try again after the fighting is done.
				System.Threading.Thread.Sleep(1000);
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
			}

			_fileLastModifiedFileName = _filename;
			_fileLastModified = System.IO.File.GetLastWriteTime(_filename);
			//have the data now!
			if (parsedData == null)
			{
				throw new Exception("Could not load Global Ifs file");
			}

			LoadKeyData("Ifs", parsedData, Ifs);

			//LoadKeyData("General", "AutoMedBreak PctMana", parsedData, ref General_AutoMedBreakPctMana);

		}

		
        public IniData CreateSettings(string filename)
        {

            IniParser.FileIniDataParser parser = e3util.CreateIniParser();
            IniData newFile = new IniData();


            newFile.Sections.AddSection("Ifs");
            var section = newFile.Sections.GetSectionData("Ifs");
		
			if (!System.IO.File.Exists(filename))
			{
				if (!System.IO.Directory.Exists(_configFolder + _settingsFolder))
				{
					System.IO.Directory.CreateDirectory(_configFolder + _settingsFolder);
				}
				_log.Write($"Creating new Global Ifs file:{filename}");
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
