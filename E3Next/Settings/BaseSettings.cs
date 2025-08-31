using E3Core.Processors;
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
    public abstract class BaseSettings
    {

        public static Logging _log = E3.Log;
        protected static IMQ MQ = E3.MQ;

        protected static string _macroFolder = MQ.Query<string>("${MacroQuest.Path[macros]}");
        protected static string _configFolder = MQ.Query<string>("${MacroQuest.Path[config]}");

        protected static string _settingsFolder = @"\e3 Macro Inis\";
        protected static string _botFolder = @"\e3 Bot Inis\";

        public DateTime _fileLastModified;
        public string _fileLastModifiedFileName;
        private static string _currentSet = String.Empty;

        public static string CurrentSet { get { return _currentSet; } set { _currentSet = value; } }
		
		static BaseSettings()
        {
			


		}
        public static string GetBoTFilePath(string characterName,string serverName,string characterClass)
        {
            string fileName = $"{characterName}_{serverName}.ini";
            string classFileName = $"_{characterClass}_{serverName}.ini";

            string botFileInConfigFolder = _configFolder + _botFolder;

            if(System.IO.File.Exists(botFileInConfigFolder + classFileName))
            {
                
                return botFileInConfigFolder + classFileName;
			}
            return botFileInConfigFolder + fileName;
        }
        public static string GetSettingsFilePath(string fileName)
        {
            string macroFile = _macroFolder + _settingsFolder + fileName;
            string configFile = _configFolder + _settingsFolder + fileName;
            string fullPathToUse = macroFile;

            if (!System.IO.File.Exists(macroFile) && !System.IO.File.Exists(configFile))
            {

                fullPathToUse = configFile;
            }
            else
            {
                fullPathToUse = macroFile;
                if (System.IO.File.Exists(configFile)) fullPathToUse = configFile;
            }
            return fullPathToUse;
        }
        public String ToStringFields()
        {
            String output = "";

            var fields = this.GetType().GetFields(BindingFlags.NonPublic | BindingFlags.Instance);

            foreach (var field in fields)
            {
                int count = 0;
                foreach (String value in (IEnumerable<string>)field.GetValue(this))
                {
                    count++;
                    output += "C" + count + ": " + value + System.Environment.NewLine;
                }
            }
            return output;
        }
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine();
            sb.Append(this.GetType().Name);
            sb.AppendLine();
            sb.AppendLine("==============");
            foreach (FieldInfo property in this.GetType().GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static))
            {
                var value = property.GetValue(this);
                if (value is System.Collections.IEnumerable && !(value is string))
                {
                    sb.AppendLine();
                    sb.Append("Collection:" + property.Name);
                    sb.AppendLine();
                    sb.AppendLine("==============");

                    foreach (var listitem in value as System.Collections.IEnumerable)
                    {
                        sb.Append("Item: " + listitem.ToString());
                    }
                }
                else
                {
                    sb.Append(property.Name);
                    sb.Append(": ");
                    sb.Append(property.GetValue(this));

                    sb.Append(System.Environment.NewLine);
                }


            }

            return sb.ToString();
        }

        public static void LoadKeyData(string sectionKey, string Key, IniData parsedData, ref string valueToSet)
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
                        }
                    }
                }
            }
        }

        public static void LoadKeyData(string sectionKey, string Key, IniData parsedData, ref DefaultBroadcast valueToSet)
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
                            Enum.TryParse<DefaultBroadcast>(data, out valueToSet);
                        }
                    }
                }
            }
        }
        public static void LoadKeyData<K, V>(string sectionKey, string Key, IniData parsedData, IDictionary<K, V> dictionary)
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
		public static void LoadKeyData<K>(string sectionKey, IniData parsedData, IDictionary<K, Burn> dictionary)
		{
			var section = parsedData.Sections[sectionKey];
			if (section != null)
			{
				var keyData = section;
				if (keyData != null)
				{
					foreach (var data in keyData)
					{
						Burn tburn = new Burn();
						tburn.Name = data.KeyName;
						foreach (var value in data.ValueList)
						{
							if (String.IsNullOrWhiteSpace(value)) continue;

							var newSpell = new Data.Spell(value, parsedData);
							CheckFor(value, sectionKey, data);
							tburn.ItemsToBurn.Add(newSpell);
						}

						dictionary.Add((K)(object)data.KeyName, tburn);
						
					}
				}
			}
		}
		public static void LoadKeyData<K>(string sectionKey, IniData parsedData, IDictionary<K, CommandSet> dictionary)
		{
			var section = parsedData.Sections[sectionKey];
			if (section != null)
			{
				var keyData = section;
				if (keyData != null)
				{
					foreach (var data in keyData)
					{
						CommandSet commandSet = new CommandSet();
						commandSet.Name = data.KeyName;
						foreach (var value in data.ValueList)
						{
							if (String.IsNullOrWhiteSpace(value)) continue;
							commandSet.Commands.Add(value);
						}

						dictionary.Add((K)(object)data.KeyName, commandSet);

					}
				}
			}
		}
		public static void LoadKeyData<K>(string sectionKey, IniData parsedData, IDictionary<K, String> dictionary)
		{
			
			var section = parsedData.Sections[sectionKey];
			if (section != null)
			{
                var keyData = section;
                if (keyData != null)
				{
					foreach (var data in keyData)
					{
						dictionary.Add((K)(object)data.KeyName, data.Value);
						
					}
				}
			}
		}
		
		public static string LoadKeyData(string sectionKey, string Key, IniData parsedData)
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
                            return data;
                        }
                    }
                }
            }
            return String.Empty;
        }
		public static SortedDictionary<string, List<Data.Spell>> LoadMeldoySetData(IniData parsedData)
		{
			SortedDictionary<string, List<Data.Spell>> returnData = new SortedDictionary<string, List<Data.Spell>>();
			foreach (var section in parsedData.Sections)
			{
				if(section.SectionName.EndsWith(" Melody"))
				{
					//its a dynamic melody, lets get the spells 
					string name = section.SectionName.Split(new char[] { ' ' })[0];

					List<Data.Spell> spellList;
					if(!returnData.TryGetValue(name,out spellList))
					{
						spellList = new List<Data.Spell>();
						returnData.Add(name, spellList);
					}
					foreach(var key in section.Keys)
					{
						foreach(var value in key.ValueList)
						{
							var newSpell = new Data.Spell(value, parsedData);
							spellList.Add(newSpell);
						}
					}
				}
			}
			return returnData;
		}
        public static void LoadKeyData(string sectionKey, string Key, IniData parsedData, ref Boolean valueToSet)
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
                            if (data.Equals("Off", StringComparison.OrdinalIgnoreCase) || data.Equals("False", StringComparison.OrdinalIgnoreCase))
                            {
                                valueToSet = false;
                            }
                            else if (data.Equals("On", StringComparison.OrdinalIgnoreCase) || data.Equals("True", StringComparison.OrdinalIgnoreCase))
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
							if(!Int32.TryParse(data,out valueToSet))
							{
								MQ.Write($"\arERROR! Invalid Int32 value for [{sectionKey}][{Key}]");
							}
	                    }
                    }
                }
            }
        }

        public static void LoadKeyData(string sectionKey, string Key, IniData parsedData, List<String> collectionToAddTo)
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
                            collectionToAddTo.Add(data);
                        }

                    }
                }
            }
        }
		public static void LoadKeyData(string sectionKey, string Key, IniData parsedData, HashSet<String> collectionToAddTo)
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
                            if(!collectionToAddTo.Contains(data))
                            {
								collectionToAddTo.Add(data);

							}
						}

					}
				}
			}
		}
		public static void LoadKeyData(string sectionKey, string Key, IniData parsedData, List<Data.Spell> collectionToAddTo)
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
                            CheckFor(data, sectionKey,keyData);
                            collectionToAddTo.Add(new Data.Spell(data, parsedData));
                        }

                    }
                }
            }
        }
        public static void LoadKeyData(string sectionKey, string Key, IniData parsedData, List<Data.SpellRequest> collectionToAddTo)
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
                            CheckFor(data, sectionKey, keyData);
                            collectionToAddTo.Add(new Data.SpellRequest(data, parsedData));
                        }

                    }
                }
            }
        }
        public static void LoadKeyData(string sectionKey, string Key, IniData parsedData, Queue<Data.Spell> collectionToAddTo)
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

                            CheckFor(data, sectionKey, keyData);


                            collectionToAddTo.Enqueue(new Data.Spell(data, parsedData));
                        }

                    }
                }
            }
        }
        public static void LoadKeyData(string sectionKey, string Key, IniData parsedData, List<Data.MelodyIfs> collectionToAddTo)
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
                            collectionToAddTo.Add(new Data.MelodyIfs(data, parsedData));
                        }

                    }
                }
            }
        }
        public static void LoadKeyData(string sectionKey, string Key, IniData parsedData, out Data.Spell spellToLoad)
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
                            spellToLoad = new Data.Spell(data, parsedData);
                            return;
                        }

                    }
                }
            }
            spellToLoad = null;
        }

        /// <summary>
        /// Checks if i have a thing and broadcasts a warning message that i don't.
        /// </summary>
        /// <param name="thingToCheckFor">The thing.</param>
        public static void CheckFor(string thingToCheckFor, string sectionkey,KeyData keyData)
        {

            if (sectionkey.Equals("Cures", StringComparison.OrdinalIgnoreCase)) return;
            if (sectionkey.Equals("Blocked Buffs", StringComparison.OrdinalIgnoreCase)) return;
            if (sectionkey.Equals("Dispel", StringComparison.OrdinalIgnoreCase)) return;
			if (sectionkey.Equals("Dispel", StringComparison.OrdinalIgnoreCase)) return;
			if (sectionkey.Equals("Pets", StringComparison.OrdinalIgnoreCase) && keyData.KeyName.Equals("Blocked Pet Buff", StringComparison.OrdinalIgnoreCase)) return;
			if (sectionkey.Equals("Buffs", StringComparison.OrdinalIgnoreCase) && keyData.KeyName.Equals("Group Buff Request",StringComparison.OrdinalIgnoreCase)) return;
			if (sectionkey.Equals("Buffs", StringComparison.OrdinalIgnoreCase) && keyData.KeyName.Equals("Raid Buff Request", StringComparison.OrdinalIgnoreCase)) return;
			if (sectionkey.Equals("Buffs", StringComparison.OrdinalIgnoreCase) && keyData.KeyName.Equals("Stack Buff Request", StringComparison.OrdinalIgnoreCase)) return;

			if (sectionkey.Equals("Charm", StringComparison.OrdinalIgnoreCase)) return;

			string thing = thingToCheckFor;
            if (thingToCheckFor.Contains('/'))
            {
                thing = thingToCheckFor.Split('/')[0];
            }

            if (!MQ.Query<bool>($"${{Me.Book[{thing}]}}") && !MQ.Query<bool>($"${{Me.AltAbility[{thing}]}}") &&
                !MQ.Query<bool>($"${{Me.CombatAbility[{thing}]}}") && !MQ.Query<bool>($"${{Me.Ability[{thing}]}}") &&
                !MQ.Query<bool>($"${{FindItem[={thing}]}}"))
            {
                E3.Bots.Broadcast($"\ayI do not have {thing} that is configured in bot ini.");
            }
        }

        public bool ShouldReload()
        {
            if (!string.IsNullOrEmpty(_fileLastModifiedFileName))
            {
				var currentLastModified = System.IO.File.GetLastWriteTime(_fileLastModifiedFileName);

				if(System.Diagnostics.Debugger.IsAttached)
				{ return false;
				}

				if (_fileLastModified != currentLastModified)
                {
                    return true;
                }
            }

            return false;
        }
    }
    interface IBaseSettings
    {
        IniData CreateSettings(string filename);
    }
}
