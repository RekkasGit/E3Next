using E3Core.Settings.FeatureSettings;
using E3Core.Utility;
using MonoCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace E3Core.Processors
{
    public class SubSystemInitAttribute : Attribute
    {
    }
	public class ExposedData : Attribute
	{
		private string _header;
		private string _key;

		public string Header
		{
			get { return _header; }
			set { _header = value; }
		}
		public string Key
		{
			get { return _key; }
			set { _key = value; }
		}

		public ExposedData(string header, string key)
		{
			_header = header;
			_key = key;
		}

	}
	public static class Setup
    {

        static public Boolean _reloadOnLoot = false;
        static public string _missingSpellItem = string.Empty;
        static public Int32 _numInventorySlots = 10;
        static public Int32 _previousSpellGemThatWasCast = -1;
		[ExposedData("Setup", "Version")]
		public const string _e3Version = "1.49_devbuild";
		[ExposedData("Setup", "BuildDate")]
		public static string _buildDate = string.Empty;
        public static Boolean _debug = true;
        public const string _macroData_Ini = @"e3 Macro Inis\e3 Data.ini";
        public static string _generalSettings_Ini = @"e3 Macro Inis\General Settings.ini";
        public static string _advancedSettings_Ini = @"e3 Macro Inis\Advanced Settingse.ini";
        public static string _character_Ini = @"e3 Bot Inis\{CharacterName}_{ServerName}.ini";
		public static string _guildListFilePath = String.Empty;
		public static List<string> GuildListMembers = new List<string>();
        public static bool _broadcastWrites = false;

		public static string _serverNameForIni = "PEQTGC"; //project eq, the grand creation, where legacy e3 was born i believe.
        public static Logging _log = E3.Log;
        private static IMQ MQ = E3.MQ;
		public static Dictionary<string, FieldInfo> ExposedDataReflectionLookup = new Dictionary<string, FieldInfo>();

		public static InventoryDataFile _inventoryDataFile;

		public static Boolean Init()
        {
            using (_log.Trace())
            {
				
				RegisterEvents();
				E3.MQBuildVersion = (MQBuild)MQ.Query<Int32>("${MacroQuest.Build}");
                if(MQ.Query<bool>("!${Defined[E3N_var]}"))
                {
                    MQ.Cmd("/declare E3N_var string global false");
                }

                //lets init server name
                //laz server specific,otherwise default
                if (MQ.Query<bool>($"${{MacroQuest.Server.Equal[Project Lazarus]}}"))
                {
                    _serverNameForIni = "Lazarus";
                }
				_buildDate = Properties.Resources.BuildDate;
				_buildDate = _buildDate.Replace("\r\n", "");
				MQ.Write($"Loading nE³xt v{_e3Version} builddate:{_buildDate}...Mq2Mono v{Core._MQ2MonoVersion}");

                //setup the library path loading, mainly used for sqlite atm
				string MQPath = MQ.Query<String>("${MacroQuest.Path}");
				string libPath;
				if (!e3util.Is64Bit())
				{
					libPath = MQPath + @"\mono\libs\32bit\";
				}
				else
				{
					libPath = MQPath + @"\mono\libs\64bit\";

				}
			    //temp add the path for just this process/app domain
                Environment.SetEnvironmentVariable("PATH", Environment.GetEnvironmentVariable("PATH") + ";" + libPath);

				InitPlugins();
                InitSubSystems();

				//after all subsystems have been init, lets init the server specific ones, as they can override events/commands
				SeverSpecific.SeverSpecific_Init();

				GetExposedDataMappedToDictionary();




				foreach (var command in E3.CharacterSettings.StartupCommands)
                {
                    MQ.Cmd(command);
                }
                _inventoryDataFile = new InventoryDataFile();
				//needed for IsMyGuild(namn), to supply a user generated list of guild members
				_guildListFilePath = Settings.BaseSettings.GetSettingsFilePath("GuildList.txt");
				if(System.IO.File.Exists(_guildListFilePath))
				{
					foreach(var line in System.IO.File.ReadAllLines(_guildListFilePath))
					{
						GuildListMembers.Add(line);
					}
				}
				return true;
			}

        }
        
		public static void RegisterEvents()
		{
			EventProcessor.RegisterCommand("/e3version", (x) =>
			{

				MQ.Write($"nE³xt v{_e3Version} builddate:{_buildDate}...Mq2Mono v{Core._MQ2MonoVersion}");

			}, "Shows current version");

			EventProcessor.RegisterCommand("/e3ListExposedData", (x) =>
			{
				foreach(var pair in ExposedDataReflectionLookup)
				{
					E3.Bots.Broadcast(pair.Key,true);
				}

			});
			EventProcessor.RegisterCommand("/e3broadcastwrites", (x) =>
			{

				e3util.ToggleBooleanSetting(ref _broadcastWrites, "Broadcast Writes", x.args);


			},"Have your toon broadcast writes out to the MQ window for a conssolidated view.");
		}
			private static void InitSubSystems()
        {
            var methods = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(x => x.GetTypes())
            .Where(x => x.IsClass)
            .SelectMany(x => x.GetMethods())
            .Where(x => x.GetCustomAttributes(typeof(SubSystemInitAttribute), false).FirstOrDefault() != null); // returns only methods that have the InvokeAttribute

            foreach (var foundMethod in methods) // iterate through all found methods
            {
				
                //these are static don't need to create an instance
                var func = (Action)foundMethod.CreateDelegate(typeof(Action));
                try
                {
					func.Invoke();

				}
				catch (TargetInvocationException ex)
                {
                    MQ.Write($"Issue with InitSubsystem:{foundMethod.ToString()} message: [{ex.Message}] inner:[{ex.InnerException?.Message}] inner_stack:{ex.InnerException?.StackTrace}");
                    throw ex;
                }
            }
        }
        private static void InitPlugins()
        {
            using (_log.Trace())
            {
            
                if (!MQ.Query<bool>("${Plugin[MQ2Debuffs]}"))
                {
					MQ.Cmd("/plugin mq2debuffs");
					if (!MQ.Delay(3000, "${Plugin[MQ2Debuffs]}"))
					{
						MQ.Write("\ar***WARNING*** Could not load MQ2Debuffs! Macro functionality may be limited.");
					}
				}
			
				if (!MQ.Query<bool>($"${{Plugin[MQ2AdvPath].Name.Length}}"))
                {
                    MQ.Write("Plugin MQ2AdvPath is not loaded, attempting to resolve...");
                    MQ.Cmd("/plugin MQ2AdvPath");
                    if (!MQ.Delay(3000, "${AdvPath}"))
                    {
                        MQ.Write("\ar***WARNING*** Could not load MQ2AdvPath. Please ensure you're using a copy of MQ2 which was compiled with the MQ2AdvPath plugin and try again.");
                    }

                }
                if (!MQ.Query<bool>("${Plugin[MQ2MoveUtils]}"))
                {
                    MQ.Write("Plugin MQ2MoveUtils is not loaded, attempting to resolve...");
                    MQ.Cmd("/plugin MQ2MoveUtils");
                    if (!MQ.Delay(3000, "${Stick.Status}"))
                    {
                        MQ.Write("\ar***WARNING*** Could not load MQ2MoveUtils! Macro functionality may be limited.");
                    }
                    if (!MQ.Query<bool>("${Stick.Status}"))
                    {
                        if (!MQ.Query<bool>("${AdvPath}"))
                        {
                            MQ.Write("\arFollow and Assist stick DISABLED.");
                        }
                        else
                        {
                            MQ.Write("\arFollow and Assist stick DISABLED.");
                        }
                    }
                }
                if (!MQ.Query<bool>("${Plugin[MQ2Exchange]}"))
                {
                    MQ.Write("Plugin MQ2Exchange is not loaded, attempting to resolve...");
                    MQ.Cmd("/plugin MQ2Exchange");
                    if (!MQ.Delay(3000, "${Plugin[MQ2Exchange]}"))
                    {
                        MQ.Write("\ar***WARNING*** Could not load MQ2Exchange! Macro functionality may be limited. Item swapping is DISABLED.");
                    }
                }
                if (!MQ.Query<bool>($"${{Plugin[mq2itemdisplay]}}"))
                {
                    MQ.Cmd("/plugin MQ2ItemDisplay");

                }
                if (!MQ.Query<bool>($"${{Plugin[MQ2LinkDB]}}"))
                {
                    MQ.Cmd("/plugin MQ2LinkDB");
                    MQ.Cmd("/link /scan off");
                }

            }
        }
		public static void GetExposedDataMappedToDictionary()
		{
			ExposedDataReflectionLookup.Clear();
			//now for some ... reflection again.
			var fields = AppDomain.CurrentDomain.GetAssemblies()
			.SelectMany(x => x.GetTypes())
			.Where(x => x.IsClass)
			.SelectMany(x => x.GetFields(BindingFlags.Public|BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance))
			.Where(x => x.GetCustomAttributes(typeof(ExposedData), false).FirstOrDefault() != null); // returns only methods that have the InvokeAttribute

			foreach (var foundField in fields) // iterate through all found methods
			{
				var customAttributes = foundField.GetCustomAttributes();
				string section = String.Empty;
				string key = String.Empty;
				//these are static don't need to create an instance
				foreach (var attribute in customAttributes)
				{
					if (attribute is ExposedData)
					{
						if (foundField.IsGenericDictonary(typeof(string), typeof(Burn)))
						{
							var burnCollection = (Dictionary<string,Burn>)foundField.GetValue(E3.CharacterSettings);
							var tattribute = ((ExposedData)attribute);
							section = tattribute.Header;
							foreach(var pair in burnCollection)
							{
								key = pair.Key;
								string dictKey = $"${{E3N.State.{section}.{key}}}";
								ExposedDataReflectionLookup.Add(dictKey, foundField);
							}
						}
						else
						{
							var tattribute = ((ExposedData)attribute);
							section = tattribute.Header;
							key = tattribute.Key;
							string dictKey = $"${{E3N.State.{section}.{key}}}";
							ExposedDataReflectionLookup.Add(dictKey, foundField);
						}

						
					}
				}
			}
		}
	}
}
