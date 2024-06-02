using E3Core.Data;
using E3Core.Processors;
using E3Core.Utility;
using E3NextConfigEditor.Client;
using E3NextConfigEditor.Themese;
using Google.Protobuf.WellKnownTypes;
using MonoCore;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Rebar;
using E3NextConfigEditor.Extensions;
using System.Reflection;
using Google.Protobuf.Collections;
using System.Collections;
using Krypton.Toolkit;
using System.Threading;
using System.Windows.Markup;
using Google.Protobuf.Reflection;

namespace E3NextConfigEditor
{
	public partial class ConfigEditor : KryptonForm
	{
		//client to query the EQ intsance
		public static DealerClient _tloClient;
		//pre-loaded spell icons
		public static List<Bitmap> _spellIcons = new List<Bitmap>();
		//reflection stuff for the E3.CharacterSettings
		public static Dictionary<string, Dictionary<string, FieldInfo>> _charSettingsMappings;
		//our current class, needed by the settings class
		public static E3Core.Data.Class _currentClass;
		//building the tree views takes time, so only pay it once
		//side benefit it keeps the position of when it was closed
		//maybe should create an object that keeps the position/size but eh.
		public static AddSpellEditor _spellEditor;
		public static AddSpellEditor _discEditor;
		public static AddSpellEditor _abilityEditor;
		public static AddSpellEditor _aaEditor;
		public static AddSpellEditor _skillEditor;
		public static AddSpellEditor _itemEditor;
		public static ConfigTextViewer _textViewer;
		//Spell data organized for the tree view of the editors.
		public static SortedDictionary<string, SortedDictionary<string, List<SpellData>>> _spellDataOrganized = new SortedDictionary<string, SortedDictionary<string, List<SpellData>>>();
		public static SortedDictionary<string, SortedDictionary<string, List<SpellData>>> _altdataOrganized = new SortedDictionary<string, SortedDictionary<string, List<SpellData>>>();
		public static SortedDictionary<string, SortedDictionary<string, List<SpellData>>> _discdataOrganized = new SortedDictionary<string, SortedDictionary<string, List<SpellData>>>();
		public static SortedDictionary<string, SortedDictionary<string, List<SpellData>>> _skilldataOrganized = new SortedDictionary<string, SortedDictionary<string, List<SpellData>>>();
		public static SortedDictionary<string, SortedDictionary<string, List<SpellData>>> _itemDataOrganized = new SortedDictionary<string, SortedDictionary<string, List<SpellData>>>();

		//used to kill the process if the parent process dies
		Task _globalUpdate;
		public static volatile bool ShouldProcess = true;

		public static Int32 _networkPort = 0;
		public static Int32 _parentProcessID = 0;
		public static Int32 _propertyGridWidth = 230;
		public static string _bardDynamicMelodyName = "Dynamic Melodies";
		public static List<String> _dynamicSections = new List<string>() { _bardDynamicMelodyName };
		public static SplashScreen _splashScreen = null;
		public static IniParser.Model.IniData _baseIniData = null;

		//list the Dictionary or Key/Value based sections that are valid
		static List<string> _dictionarySections = new List<string>() { "Ifs", "E3BotsPublishData (key/value)", "Events", "EventLoop" };


		public ConfigEditor()
		{
			InitializeComponent();
			this.StartPosition = FormStartPosition.CenterScreen;
			GetParams();

			LoadData();
			_globalUpdate = Task.Factory.StartNew(() => { GlobalTimer(); }, CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);

		}
		private void GetParams()
		{
			string[] args = Environment.GetCommandLineArgs();

			if (args.Length > 1)
			{
				if (!Int32.TryParse(args[1], out _networkPort))
				{

					Application.Exit();
					return;
				}
			}
			if (args.Length > 2)
			{
				if (!Int32.TryParse(args[2], out _parentProcessID))
				{

					Application.Exit();
					return;
				}
			}
		}
		private void GlobalTimer()
		{
			while (ShouldProcess)
			{
				if (_parentProcessID > 0)
				{
					if (!ProcessExists(_parentProcessID))
					{
						System.Windows.Forms.Application.Exit();
					}
				}

				System.Threading.Thread.Sleep(1000);
			}
		}
		/// <summary>
		/// used to check if our parent process dies, so that we can close as well.
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
		private bool ProcessExists(int id)
		{
			return Process.GetProcesses().Any(x => x.Id == id);
		}
		public void LoadData()
		{
			

			//create the client to get data from E3N/MQ
			_tloClient = new DealerClient(_networkPort);
			//create an IMQ interface, so that we can use this when loading the settings
			IMQ _mqClient = new MQ.MQClient(_tloClient);

			_splashScreen.Invoke(new Action(() =>_splashScreen.splashLabel.Text="Requesting AA list..."));
			byte[] result = _tloClient.RequestRawData("${E3.AA.ListAll}");
			SpellDataList aas = SpellDataList.Parser.ParseFrom(result);
			_splashScreen.Invoke(new Action(() => _splashScreen.splashLabel.Text = "Requesting SpellBook list..."));
			result = _tloClient.RequestRawData("${E3.SpellBook.ListAll}");
			SpellDataList bookSpells = SpellDataList.Parser.ParseFrom(result);

			_splashScreen.Invoke(new Action(() => _splashScreen.splashLabel.Text = "Requesting Disc list..."));
			result = _tloClient.RequestRawData("${E3.Discs.ListAll}");
			SpellDataList discs = SpellDataList.Parser.ParseFrom(result);
			_splashScreen.Invoke(new Action(() => _splashScreen.splashLabel.Text = "Requesting Skill list..."));
			result = _tloClient.RequestRawData("${E3.Skills.ListAll}");
			SpellDataList skills = SpellDataList.Parser.ParseFrom(result);
			_splashScreen.Invoke(new Action(() => _splashScreen.splashLabel.Text = "Requesting Item list..."));
			result = _tloClient.RequestRawData("${E3.ItemsWithSpells.ListAll}");
			SpellDataList items = SpellDataList.Parser.ParseFrom(result);

			//need the correct directory so that we can get image data from the UI folder
			string EQDirectory = _tloClient.RequestData("${EverQuest.Path}");


			//set some default categories/subcategories so it look correct in the list view
			foreach (var skill in skills.Data)
			{
				skill.Category = "Skill";
				skill.Subcategory = "Basic";
			}

			//need the proper class so that the settings can load correctly
			string classValue = e3util.ClassNameFix(_tloClient.RequestData("${Me.Class}"));
			System.Enum.TryParse(classValue, out _currentClass);

			//lets sort all the spells by cataegory/subcategory and levels
			PopulateItemData(items.Data, _itemDataOrganized);
			PopulateData(skills.Data, _skilldataOrganized);
			PopulateData(bookSpells.Data, _spellDataOrganized);
			PopulateData(aas.Data, _altdataOrganized);
			PopulateData(discs.Data, _discdataOrganized);

			//now sort all the data. 
			//spells are by level the the leaf level
			foreach (var pair in _spellDataOrganized)
			{
				foreach (var keySet in pair.Value.Keys.ToList())
				{
					_spellDataOrganized[pair.Key][keySet] = _spellDataOrganized[pair.Key][keySet].OrderByDescending(x => x.Level).ToList();
				}
			}
			//alt data is well, special just do alpha
			foreach (var pair in _altdataOrganized)
			{
				foreach (var keySet in pair.Value.Keys.ToList())
				{
					_altdataOrganized[pair.Key][keySet] = _altdataOrganized[pair.Key][keySet].OrderBy(x => x.SpellName).ToList();
				}
			}
			//discs can be organized by levels, at the leaf
			foreach (var pair in _discdataOrganized)
			{

				foreach (var keySet in pair.Value.Keys.ToList())
				{
					_discdataOrganized[pair.Key][keySet] = _discdataOrganized[pair.Key][keySet].OrderByDescending(x => x.Level).ToList();
				}
			}
			//skills are also special, just alpha them
			foreach (var pair in _skilldataOrganized)
			{
				foreach (var keySet in pair.Value.Keys.ToList())
				{
					_skilldataOrganized[pair.Key][keySet] = _skilldataOrganized[pair.Key][keySet].OrderBy(x => x.SpellName).ToList();
				}
			}

			//set the global IMQ so that settinsg can load correctly
			//and other necessary properties
			E3.MQ = _mqClient;
			E3.Bots = new Client.Bots();
			E3.Log = new Logging(E3.MQ); ;
			E3.CurrentName = _mqClient.Query<string>("${Me.CleanName}");
			E3.ServerName = e3util.FormatServerName(_mqClient.Query<string>("${MacroQuest.Server}"));
			E3.CurrentClass = _currentClass;

			//we bulk to inform E3N that they are going to come fast, and to keep checking for 100-200miliseconds before continuing on with the game loop.
			_tloClient.RequestData("${E3.TLO.BulkBegin}");
			//create settings files here
			bool mergeUpdates = false;
			_splashScreen.Invoke(new Action(() => _splashScreen.splashLabel.Text = "Loading settings file... might take a moment (querying MQ)"));
			E3.CharacterSettings = new E3Core.Settings.CharacterSettings(mergeUpdates);
			//this will auto end after 1 second, but its good to end it properly.
			_tloClient.RequestData("${E3.TLO.BulkEnd}");

			//set window title
			this.Text = $"({E3.CurrentName})({E3.ServerName})";
			
			//load image data
			for (Int32 i = 1; i <= 63; i++)
			{
				string fileName = $@"{EQDirectory}\uifiles\default\spells{i.ToString("D2")}.tga";
				if(System.IO.File.Exists(fileName))
				{
					using (var image = new TGA.TargaImage(fileName))
					{
						using (var bitmap = image.Image)
						{
							for (Int32 y = 0; y < 6; y++)
							{
								for (Int32 x = 0; x < 6; x++)
								{
									var icon = bitmap.Clone(new Rectangle(x * 40, y * 40, 40, 40), bitmap.PixelFormat);
									_spellIcons.Add(icon);
								}
							}
						}
					}

				}
				
			}

			//get a 'base' ini file for the class so that we can use this to merge data in later
			_baseIniData = E3.CharacterSettings.createNewINIData();
			//get all the reflection attributes off the settings class
			_charSettingsMappings = e3util.GetSettingsMappedToInI();

			List<string> importantSections = GetSectionSortOrderByClass(E3.CurrentClass);

			List<string> sectionNames = new List<string>();

			//bards, the snowflakes can make dynamic sections
			if (E3.CurrentClass == Class.Bard)
			{
				sectionNames.Add(_bardDynamicMelodyName);
			}

			
			//find all the section that end in Melody like [main Melody] and ignore them.
			//at one time this was the main ini file, now we are doing the base, so this is probably not needed anymore
			foreach (var section in _baseIniData.Sections)
			{
				//bards are special, do not include their dynamic melodies
				if (section.SectionName.EndsWith(" Melody", StringComparison.OrdinalIgnoreCase)) continue;

				sectionNames.Add(section.SectionName);

			}

			//lets organize the stuff based off important sections
			foreach (var section in importantSections)
			{
				if (_dynamicSections.Contains(section))
				{
					sectionComboBox.Items.Add(section);
				}
				else if (_baseIniData.Sections.ContainsSection(section))
				{
					sectionComboBox.Items.Add(section);

				}
			}
			//add the rest that were not important
			foreach (var section in sectionNames)
			{
				if (importantSections.Contains(section, StringComparer.OrdinalIgnoreCase)) continue;
				sectionComboBox.Items.Add(section);

			}

		}
		/// <summary>
		/// This is used to decide what sections are important and thus shown first to a class
		/// </summary>
		/// <param name="characterClass"></param>
		/// <returns></returns>
		List<string> GetSectionSortOrderByClass(Class characterClass)
		{

			var returnValue = new List<string>() { "Misc", "Assist Settings", "Nukes", "Debuffs", "DoTs on Assist", "DoTs on Command", "Heals", "Buffs", "Melee Abilities", "Burn", "Pets", "Ifs" };


			if (characterClass == Class.Bard)
			{
				returnValue = new List<string>() { _bardDynamicMelodyName, "Bard", "Melee Abilities", "Burn", "Ifs", "Assist Settings", "Buffs" };
			}
			if (characterClass == Class.Necromancer)
			{
				returnValue = new List<string>() { "DoTs on Assist","DoTs on Command", "Debuffs","Pets", "Burn", "Ifs", "Assist Settings", "Buffs" };
			}
			if (characterClass == Class.Shadowknight)
			{
				returnValue = new List<string>() { "Nukes", "Assist Settings", "Buffs","DoTs on Assist", "DoTs on Command", "Debuffs", "Pets", "Burn", "Ifs"  };
			}

			return returnValue;
		}
		/// <summary>
		/// Takes the data from the TLO queries and creates the organized dictionary data. 
		/// </summary>
		/// <param name="spells"></param>
		/// <param name="dest"></param>
		public void PopulateData(RepeatedField<SpellData> spells, SortedDictionary<string, SortedDictionary<string, List<SpellData>>> dest)
		{
			foreach (SpellData s in spells)
			{

				SortedDictionary<string, List<SpellData>> subCategoryLookup;
				List<SpellData> spellList;
				if (!dest.TryGetValue(s.Category, out subCategoryLookup))
				{
					subCategoryLookup = new SortedDictionary<string, List<SpellData>>();
					dest.Add(s.Category, subCategoryLookup);
				}
				if (!subCategoryLookup.TryGetValue(s.Subcategory, out spellList))
				{
					spellList = new List<SpellData>();
					subCategoryLookup.Add(s.Subcategory, spellList);
				}

				spellList.Add(s);

			}
		}
		/// <summary>
		/// Items are special so they are oranized a bit differently as they are mossing one level.
		/// </summary>
		/// <param name="spells"></param>
		/// <param name="dest"></param>
		public void PopulateItemData(RepeatedField<SpellData> spells, SortedDictionary<string, SortedDictionary<string, List<SpellData>>> dest)
		{
			foreach (SpellData s in spells)
			{

				SortedDictionary<string, List<SpellData>> subCategoryLookup;
				List<SpellData> spellList;
				if (!dest.TryGetValue(s.CastName, out subCategoryLookup))
				{
					subCategoryLookup = new SortedDictionary<string, List<SpellData>>();
					dest.Add(s.CastName, subCategoryLookup);
				}
				if (!subCategoryLookup.TryGetValue(s.SpellName, out spellList))
				{
					spellList = new List<SpellData>();
					subCategoryLookup.Add(s.SpellName, spellList);
				}

				spellList.Add(s);

			}
		}
		#region comboBoxs
		// The combo box stuff
		private void sectionComboBox_ButtonSpecAny2_Click(object sender, EventArgs e)
		{
			if (sectionComboBox.SelectedIndex < (sectionComboBox.Items.Count - 1))
			{
				sectionComboBox.SelectedIndex += 1;
				sectionComboBox.ComboBox.Focus();
			}
		}

		private void sectionComboBox_ButtonSpecAny1_Click(object sender, EventArgs e)
		{
			if (sectionComboBox.SelectedIndex > 0)
			{
				sectionComboBox.SelectedIndex -= 1;
				sectionComboBox.ComboBox.Focus();
			}

		}
		
		private void sectionComboBox_SelectedIndexChanged(object sender, EventArgs e)
		{
			//selection changed, update the navigator


			subsectionComboBox.Items.Clear();
			valuesListBox.Items.Clear();
			valuesListBox.Tag = null;

			propertyGrid.SelectedObject = null;


			string selectedSection = sectionComboBox.SelectedItem.ToString();



			if (selectedSection == _bardDynamicMelodyName)
			{   //sigh bards, special snowflakes
				valuesListBox.Tag = _bardDynamicMelodyName;
				IDictionary<string, List<Spell>> dictionary = E3.CharacterSettings.Bard_MelodySets;
				foreach (var pair in dictionary)
				{
					subsectionComboBox.Items.Add(pair.Key);
				}
			}
			else
			{
				var section = _baseIniData.Sections[selectedSection];
				if (section != null)
				{
					//dynamic type, just fill out the list below with the loaded types
					if (_dictionarySections.Contains(selectedSection, StringComparer.OrdinalIgnoreCase))
					{
						FieldInfo objectList = _charSettingsMappings[selectedSection][""];

						UpdateListView(objectList);
					}
					else
					{
						foreach (var key in section)
						{

							subsectionComboBox.Items.Add(key.KeyName);
						}
					}
				}
			}





		}

		private void subsectionComboBox_buttonSpecAny1_Click(object sender, EventArgs e)
		{
			if (subsectionComboBox.SelectedIndex > 0)
			{
				subsectionComboBox.SelectedIndex -= 1;
				subsectionComboBox.ComboBox.Focus();
			}

		}

		private void subsectionComboBox_buttonSpecAny2_Click(object sender, EventArgs e)
		{
			if (subsectionComboBox.SelectedIndex < (subsectionComboBox.Items.Count - 1))
			{
				subsectionComboBox.SelectedIndex += 1;
				subsectionComboBox.ComboBox.Focus();
			}
		}

		private void subsectionComboBox_SelectedIndexChanged(object sender, EventArgs e)
		{
			string selectedSection = sectionComboBox.SelectedItem.ToString();


			if (selectedSection == _bardDynamicMelodyName)
			{   //sigh bards, special snowflakes
				FieldInfo objectList = _charSettingsMappings["Bard"]["DynamicMelodySets"];
				if (objectList.IsGenericSortedDictonary(typeof(string), typeof(List<Spell>)))
				{
					IDictionary<string, List<Spell>> dynamicMelodies = (IDictionary<string, List<Spell>>)objectList.GetValue(E3.CharacterSettings);
					string selectedSubSection = subsectionComboBox.SelectedItem.ToString();
					List<Spell> melodies = dynamicMelodies[selectedSubSection];
					UpdateListView(melodies);

				}
			}
			else
			{
				var section = _baseIniData.Sections[selectedSection];
				if (section != null)
				{
					string selectedSubSection = subsectionComboBox.SelectedItem.ToString();
					FieldInfo objectList = _charSettingsMappings[selectedSection][selectedSubSection];

					UpdateListView(objectList);
				}

			}


		}
		#endregion
		#region DragNDropListBox
		Int64 mouseDownTimeStamp = 0;
		System.Diagnostics.Stopwatch _stopwatch = new Stopwatch();
		System.Timers.Timer _timer = new System.Timers.Timer(50);

		//start the property grid elements collappsed
		private void updatePropertyGrid_CollapseCategoriesIfNeeded()
		{
			if (!_firstPropertyGridSelection) return;
			//https://forums.codeguru.com/showthread.php?380039-Expand-Collapse-a-category-in-property-grid

			GridItem root = propertyGrid.SelectedGridItem;
			while (root.Parent != null)
			{
				root = root.Parent;

			}
			if (root != null)
			{
				foreach (GridItem gi in root.GridItems)
				{
					if (gi.Label == "Flags")
					{
						gi.Expanded = false;
					}
					if (gi.Label == "Cure Flags")
					{
						gi.Expanded = false;
					}
					if (gi.Label == "Heal Flags")
					{
						gi.Expanded = false;
					}
				}
			}
		}
		private bool _firstPropertyGridSelection = true;
		private void updatePropertyGrid()
		{
			propertyGrid.SelectedObject = null;
			//need to pull out the Tag and verify which type so we know what to pass to the 
			//property grid
			KryptonListItem listItem = ((KryptonListItem)valuesListBox.SelectedItem);
			if (listItem.Tag is Spell)
			{

				propertyGrid.SelectedObject = new Models.SpellProxy((Spell)listItem.Tag);
				updatePropertyGrid_CollapseCategoriesIfNeeded();
				_firstPropertyGridSelection = false;


			}
			else if (listItem.Tag is SpellRequest)
			{
				propertyGrid.SelectedObject = new Models.SpellRequestDataProxy((SpellRequest)listItem.Tag);
				updatePropertyGrid_CollapseCategoriesIfNeeded();
				_firstPropertyGridSelection = false;
			}
			else if (listItem.Tag is MelodyIfs)
			{
				propertyGrid.SelectedObject = new Models.MelodyIfProxy((MelodyIfs)listItem.Tag);
			}
			else if (listItem.Tag is Models.Ref<string, string> || listItem.Tag is Models.Ref<string> || listItem.Tag is Models.Ref<bool> || listItem.Tag is Models.Ref<Int32> || listItem.Tag is Models.Ref<Int64>)
			{

				propertyGrid.SelectedObject = listItem.Tag;
				

			}

		}

		private void valuesListBox_SelectedIndexChanged(object sender, EventArgs e)
		{
			//is it a spell?
			Debug.WriteLine("Index Changed");
			if (valuesListBox.SelectedItem == null) return;

			updatePropertyGrid();

		}
		private void valuesListBox_MouseUp(object sender, MouseEventArgs e)
		{
			mouseDownTimeStamp = 0;
			Debug.WriteLine("Mouse Up");
		}
		private void valuesListBox_MouseDown(object sender, MouseEventArgs e)
		{
			mouseDownTimeStamp = _stopwatch.ElapsedMilliseconds;
			Debug.WriteLine("Mouse Down");
			if (valuesListBox.SelectedItem == null) return;

			updatePropertyGrid();

		}


		private void ConfigEditor_Load(object sender, EventArgs e)
		{
			valuesListBox.AllowDrop = true;
			_stopwatch.Start();
			_timer.Elapsed += _timer_Elapsed;
			_timer.Start();
			propertyGrid.SetLabelColumnWidth(_propertyGridWidth);
		}
		private void propertyGrid_SizeChanged(object sender, EventArgs e)
		{
			propertyGrid.SetLabelColumnWidth(_propertyGridWidth);
		}

		private void _timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
		{
			if (mouseDownTimeStamp != 0 && mouseDownTimeStamp + 250 < _stopwatch.ElapsedMilliseconds)
			{
				_timer.Enabled = false;
				valuesListBox.Invoke((MethodInvoker)delegate
				{
					try
					{
						if (valuesListBox.SelectedItem == null) return;
						if (valuesListBox.Items.Count < 2) return;
						valuesListBox.DoDragDrop(valuesListBox.SelectedItem, DragDropEffects.Move);
					}
					finally
					{
						mouseDownTimeStamp = 0;
						_timer.Enabled = true;
					}
				});

				
			}
		}

		private void valuesListBox_DragOver(object sender, DragEventArgs e)
		{
			e.Effect = DragDropEffects.Move;
		}

		private void valuesListBox_DragDrop(object sender, DragEventArgs e)
		{
			Point point = valuesListBox.PointToClient(new Point(e.X, e.Y));
			int index = this.valuesListBox.IndexFromPoint(point);
			if (index < 0) index = this.valuesListBox.Items.Count - 1;
			object data = valuesListBox.SelectedItem;

			object settings_data_obj = valuesListBox.Tag;

			//update the base storage data
			if (settings_data_obj is List<Spell>)
			{
				List<Spell> spellList = (List<Spell>)settings_data_obj;

				var spell = (Spell)((KryptonListItem)data).Tag;
				spellList.Remove(spell);
				spellList.Insert(index, spell);

			}
			else if (settings_data_obj is List<SpellRequest>)
			{
				List<SpellRequest> spellList = (List<SpellRequest>)settings_data_obj;

				var spell = (SpellRequest)((KryptonListItem)data).Tag;
				spellList.Remove(spell);
				spellList.Insert(index, spell);
			}
			else if (settings_data_obj is List<MelodyIfs>)
			{
				List<MelodyIfs> spellList = (List<MelodyIfs>)settings_data_obj;

				var spell = (MelodyIfs)((KryptonListItem)data).Tag;
				spellList.Remove(spell);
				spellList.Insert(index, spell);
			}
			else if (settings_data_obj is List<string>)
			{
				List<string> spellList = (List<string>)settings_data_obj;

				var spell = (Models.Ref<string>)((KryptonListItem)data).Tag;
				//make a copy as this is a ref value type
				string tempValue = spell.Value;	
				spellList.Remove(tempValue);
				spellList.Insert(index, tempValue);
			}
			this.valuesListBox.Items.Remove(data);
			this.valuesListBox.Items.Insert(index, data);
			valuesListBox.SelectedIndex = index;

			mouseDownTimeStamp = 0;
		}
		#endregion

		#region ContextMenuCommands
		private void valueList_AddDynamicMelody_Execute(object sender, EventArgs e)
		{
			if (!(valuesListBox.Tag is string) && ((string)valuesListBox.Tag) != _bardDynamicMelodyName)
			{
				return;
			}
			AddValue a = new AddValue();
			a.lableDescription.Text = "Melody Name";
			a.StartPosition = FormStartPosition.CenterParent;
			if (a.ShowDialog() == DialogResult.OK)
			{
				string value = a.Value;
				E3.CharacterSettings.Bard_MelodySets.Add(value, new List<Spell>());
				subsectionComboBox.Items.Clear();
				foreach (var pair in E3.CharacterSettings.Bard_MelodySets)
				{
					subsectionComboBox.Items.Add(pair.Key);
				}
			}
		}
		private void valueList_AddSpell_Execute(object sender, EventArgs e)
		{

			if (!(valuesListBox.Tag is List<Spell>))
			{
				return;
			}
			ShowEditorDialog(ref _spellEditor, _spellDataOrganized);
		}
		private void valueList_ReplaceSpell_Execute(object sender, EventArgs e)
		{
			if (!(valuesListBox.Tag is List<Spell>))
			{
				return;
			}
			if (valuesListBox.SelectedItem == null) return;

			ShowEditorDialog(ref _spellEditor, _spellDataOrganized,true);
		}
		private void valueList_CloneSpell_Execute(object sender, EventArgs e)
		{

			if (!(valuesListBox.Tag is List<Spell>))
			{
				return;
			}
			if (valuesListBox.SelectedItem == null) return;

			Spell currentSpellSelected = (Spell)((KryptonListItem)valuesListBox.SelectedItem).Tag;

			if(currentSpellSelected!=null)
			{
				var clonedSpell = currentSpellSelected.ToProto();
				valueList_AddSpellToCollection(clonedSpell);
			}

		}
		private void valueList_AddItem_Execute(object sender, EventArgs e)
		{
			if (!(valuesListBox.Tag is List<Spell>))
			{
				return;
			}
			ShowEditorDialog(ref _itemEditor, _itemDataOrganized);
		}
		private void valueList_AddAA_Execute(object sender, EventArgs e)
		{
			if (!(valuesListBox.Tag is List<Spell>))
			{
				return;
			}

			ShowEditorDialog(ref _aaEditor, _altdataOrganized);
		}
		private void valueList_Delete_Execute(object sender, EventArgs e)
		{
			if (valuesListBox.SelectedItem == null) return;
			if (valuesListBox.Items.Count < 2)
			{
				bool shouldExit = true;
				//these are allowed to have empty lists, the rest no
				if (valuesListBox.Tag is List<Spell>) shouldExit = false;
				if (valuesListBox.Tag is List<SpellRequest>) shouldExit = false;
				if (valuesListBox.Tag is List<MelodyIfs>) shouldExit = false;
				if (valuesListBox.Tag is IDictionary<string, string>) shouldExit = false;
				if (valuesListBox.Tag is List<string>) shouldExit = false;
				if (shouldExit) return;
			}
			object data = valuesListBox.SelectedItem;

			object settings_data_obj = valuesListBox.Tag;

			//update the base storage data
			if (settings_data_obj is List<Spell>)
			{
				List<Spell> spellList = (List<Spell>)settings_data_obj;

				var spell = (Spell)((KryptonListItem)data).Tag;
				spellList.Remove(spell);
			}
			else if (settings_data_obj is List<SpellRequest>)
			{
				List<SpellRequest> spellList = (List<SpellRequest>)settings_data_obj;

				var spell = (SpellRequest)((KryptonListItem)data).Tag;
				spellList.Remove(spell);
			}
			else if (settings_data_obj is List<MelodyIfs>)
			{
				List<MelodyIfs> spellList = (List<MelodyIfs>)settings_data_obj;

				var spell = (MelodyIfs)((KryptonListItem)data).Tag;
				spellList.Remove(spell);
			}
			else if (settings_data_obj is List<string>)
			{
				List<string> stringList = (List<string>)settings_data_obj;
				var stringRef = (Models.Ref<string>)((KryptonListItem)data).Tag;
				stringList.Remove(stringRef.Value);
			}
			else if (settings_data_obj is IDictionary<string, string>)
			{
				IDictionary<string, string> stringDict = (IDictionary<string, string>)settings_data_obj;
				var stringRef = (Models.Ref<string, string>)((KryptonListItem)data).Tag;
				stringDict.Remove(stringRef.Key);
			}

			valuesListBox.Items.Remove(data);
			valuesListBox.SelectedItem = null;
			valuesListBox.Refresh();
			Debug.WriteLine("Test Delete");


		}
		private void valueList_AddDisc_Execute(object sender, EventArgs e)
		{
			if (!(valuesListBox.Tag is List<Spell>))
			{
				return;
			}
			ShowEditorDialog(ref _discEditor, _discdataOrganized);
		}
		private void valueList_AddSkill_Execute(object sender, EventArgs e)
		{
			if (!(valuesListBox.Tag is List<Spell>))
			{
				return;
			}
			ShowEditorDialog(ref _skillEditor, _skilldataOrganized);
		}
		private void valueList_AddMelodyIf_Execute(object sender, EventArgs e)
		{
			if (!(valuesListBox.Tag is List<MelodyIfs>))
			{
				return;
			}

			AddkeyValue a = new AddkeyValue();
			a.SetKeyLabel("Melody Name");
			a.SetValueLabel("Ifs Name");
			a.StartPosition = FormStartPosition.CenterParent;
			if (a.ShowDialog() == DialogResult.OK)
			{
				valueList_AddMelodyIfToCollection(a.Key, a.Value);

			}

		}
		private void valueList_AddValue_Execute(object sender, EventArgs e)
		{
			if (!(valuesListBox.Tag is List<string>))
			{
				return;
			}
			AddValue a = new AddValue();
			a.lableDescription.Text = "Value";
			a.StartPosition = FormStartPosition.CenterParent;
			if (a.ShowDialog() == DialogResult.OK)
			{
				List<string> valueList = (List<string>)valuesListBox.Tag;
				valueList.Add(a.Value);

				string selectedSection = sectionComboBox.SelectedItem.ToString();
				var section = _baseIniData.Sections[selectedSection];
				if (section != null)
				{
					string selectedSubSection = subsectionComboBox.SelectedItem.ToString();
					FieldInfo objectList = _charSettingsMappings[selectedSection][selectedSubSection];
					UpdateListView(objectList);
				}
			}
		}
		private void valueList_AddKeyValue_Execute(object sender, EventArgs e)
		{
			if (valuesListBox.Tag is IDictionary<string, string>)
			{
				AddkeyValue a = new AddkeyValue();
				a.StartPosition = FormStartPosition.CenterParent;
				if (a.ShowDialog() == DialogResult.OK)
				{
					string key = a.Key;
					string value = a.Value;

					IDictionary<string, string> dict = (IDictionary<string, string>)valuesListBox.Tag;

					if (!dict.ContainsKey(key))
					{
						dict.Add(key, value);
					}
					else
					{
						dict[key] = value;
					}
					string selectedSection = sectionComboBox.SelectedItem.ToString();
					var section = _baseIniData.Sections[selectedSection];
					if (section != null)
					{
						//dynamic type, just fill out the list below with the loaded types
						if (_dictionarySections.Contains(selectedSection, StringComparer.OrdinalIgnoreCase))
						{
							FieldInfo objectList = _charSettingsMappings[selectedSection][""];

							UpdateListView(objectList);
						}
					}
				}
			}
			else if (valuesListBox.Tag is List<SpellRequest>)
			{
				AddkeyValue a = new AddkeyValue();
				a.SetKeyLabel("Spell Name");
				a.SetValueLabel("Request target");

				a.StartPosition = FormStartPosition.CenterParent;
				if (a.ShowDialog() == DialogResult.OK)
				{
					string SpellName = a.Key;
					string RequestTargetName = a.Value;
					SpellRequest newSpell = new SpellRequest(SpellName + "/" + RequestTargetName);
					newSpell.IsBuff = true;
					List<SpellRequest> spellList = (List<SpellRequest>)valuesListBox.Tag;
					if (spellList.Count > 0 && valuesListBox.SelectedItem != null)
					{
						//put after the current selected
						Int32 index = valuesListBox.SelectedIndex + 1;
						KryptonListItem item = new KryptonListItem();
						string nameOfSpell = newSpell.CastName;

						//visual showing of if the spell is disabled
						if (!newSpell.Enabled) nameOfSpell = nameOfSpell + " (disabled)";

						item.ShortText = nameOfSpell;
						item.LongText = string.Empty;
						item.Tag = newSpell;
						if (newSpell.SpellIcon > -1)
						{
							item.Image = _spellIcons[newSpell.SpellIcon];

						}
						spellList.Insert(index, newSpell);
						valuesListBox.Items.Insert(index, item);
					}
					else
					{
						//no items in the list, just add
						spellList.Add(newSpell);
						KryptonListItem item = new KryptonListItem();
						string nameOfSpell = newSpell.CastName;

						//visual showing of if the spell is disabled
						if (!newSpell.Enabled) nameOfSpell = nameOfSpell + " (disabled)";
						item.ShortText = nameOfSpell;
						item.LongText = string.Empty;
						item.Tag = newSpell;
						if (newSpell.SpellIcon > -1)
						{
							item.Image = _spellIcons[newSpell.SpellIcon];
						}
						valuesListBox.Items.Add(item);
					}


				}
			}
			


		}
		#endregion

		#region HelperMethods
		private void SetMenuItemVisablity(KryptonContextMenuItem menuItem)
		{
			
			if ((valuesListBox.Tag is IDictionary<string, string>))
			{
				if (menuItem.Text == "Add Key/Value")
				{
					menuItem.Visible = true;
				}
				else if (menuItem.Text == "Delete")
				{
					menuItem.Visible = true;
				}
			}
			else if ((valuesListBox.Tag is List<Spell>) )
			{

				if (menuItem.Text == "Add Disc")
				{
					menuItem.Visible = true;
				}
				else if (menuItem.Text == "Add Spell")
				{
					menuItem.Visible = true;
				}
				else if (menuItem.Text == "Add AA")
				{
					menuItem.Visible = true;
				}
				else if (menuItem.Text == "Add Skill")
				{
					menuItem.Visible = true;
				}
				else if (menuItem.Text == "Add Item")
				{
					menuItem.Visible = true;
				}
				else if (menuItem.Text == "Replace Spell")
				{
					menuItem.Visible = true;
				}
				else if (menuItem.Text == "Clone Spell")
				{
					menuItem.Visible = true;
				}
				else if (menuItem.Text == "Delete")
				{
					menuItem.Visible = true;
				}
			}
			else if ((valuesListBox.Tag is List<SpellRequest>))
			{

				if (menuItem.Text == "Add Key/Value")
				{
					menuItem.Visible = true;
				}
				else if (menuItem.Text == "Delete")
				{
					menuItem.Visible = true;
				}
			}
			else if ((valuesListBox.Tag is List<MelodyIfs>))
			{
				if (menuItem.Text == "Add MelodyIf")
				{
					menuItem.Visible = true;
				}
				else if (menuItem.Text == "Delete")
				{
					menuItem.Visible = true;
				}
			}
			else if (valuesListBox.Tag is string)
			{
				string value = (string)valuesListBox.Tag;

				if (value == _bardDynamicMelodyName)
				{
					if (menuItem.Text == "Add Melody")
					{
						menuItem.Visible = true;
					}
					else if (menuItem.Text == "Delete")
					{
						menuItem.Visible = true;
					}
				}
			}
			else if (valuesListBox.Tag is List<string>)
			{
				if (menuItem.Text == "Add Value")
				{
					menuItem.Visible = true;
				}
				else if (menuItem.Text == "Delete")
				{
					menuItem.Visible = true;
				}
			}
			else
			{
				menuItem.Visible = false;
			}
		}
		private void RefeshListView()
		{
			if(valuesListBox.Tag is List<Spell>)
			{
				var spelllist = (List<Spell>)valuesListBox.Tag;
				valuesListBox.Items.Clear();
				foreach (var spell in spelllist)
				{
					KryptonListItem item = new KryptonListItem();
					string nameOfSpell = spell.CastName;

					//visual showing of if the spell is disabled
					if (!spell.Enabled) nameOfSpell = nameOfSpell + " (disabled)";

					item.ShortText = nameOfSpell;
					item.LongText = string.Empty;
					item.Tag = spell;

					if (spell.SpellIcon > -1)
					{
						item.Image = _spellIcons[spell.SpellIcon];

					}
					valuesListBox.Items.Add(item);
				}
			}
			else if (valuesListBox.Tag is List<SpellRequest>)
			{
				var spelllist = (List<SpellRequest>)valuesListBox.Tag;
				valuesListBox.Items.Clear();
				foreach (var spell in spelllist)
				{
					KryptonListItem item = new KryptonListItem();
					string nameOfSpell = spell.CastName;

					//visual showing of if the spell is disabled
					if (!spell.Enabled) nameOfSpell = nameOfSpell + " (disabled)";

					item.ShortText = nameOfSpell;
					item.LongText = string.Empty;
					item.Tag = spell;

					if (spell.SpellIcon > -1)
					{
						item.Image = _spellIcons[spell.SpellIcon];

					}
					valuesListBox.Items.Add(item);
				}
			}
		}
		private void UpdateListView(List<Spell> spellList)
		{
			valuesListBox.Items.Clear();
			valuesListBox.Tag = spellList;
			foreach (var spell in spellList)
			{
				KryptonListItem item = new KryptonListItem();
				string nameOfSpell = spell.CastName;

				//visual showing of if the spell is disabled
				if (!spell.Enabled) nameOfSpell = nameOfSpell + " (disabled)";

				item.ShortText = nameOfSpell;
				item.LongText = string.Empty;
				item.Tag = spell;
				
				if (spell.SpellIcon > -1)
				{
					item.Image = _spellIcons[spell.SpellIcon];

				}
				valuesListBox.Items.Add(item);
			}
		}
		private void UpdateListView(FieldInfo objectList)
		{
			valuesListBox.Items.Clear();
			//this will not work for Ifs, Event,EventLoop as they have nop pre-defined keys

			if (objectList.IsGenericList(typeof(Spell)))
			{

				List<Spell> spellList = (List<Spell>)objectList.GetValue(E3.CharacterSettings);

				valuesListBox.Tag = spellList;
				foreach (var spell in spellList)
				{
					KryptonListItem item = new KryptonListItem();
					string nameOfSpell = spell.CastName;

					//visual showing of if the spell is disabled
					if (!spell.Enabled) nameOfSpell = nameOfSpell + " (disabled)";


					item.ShortText = nameOfSpell;
					item.LongText = string.Empty;
					item.Tag = spell;
					if (spell.SpellIcon > -1)
					{
						item.Image = _spellIcons[spell.SpellIcon];

					}
					valuesListBox.Items.Add(item);
				}

			}
			else if (objectList.IsGenericList(typeof(SpellRequest)))
			{
				List<SpellRequest> spellList = (List<SpellRequest>)objectList.GetValue(E3.CharacterSettings);
				valuesListBox.Tag = spellList;
				foreach (var spell in spellList)
				{
					KryptonListItem item = new KryptonListItem();
					item.ShortText = spell.CastName;
					item.LongText = string.Empty;
					item.Tag = spell;
					if (spell.SpellIcon > -1)
					{
						item.Image = _spellIcons[spell.SpellIcon];

					}
					valuesListBox.Items.Add(item);
				}
			}
			else if (objectList.IsGenericList(typeof(MelodyIfs)))
			{
				List<MelodyIfs> spellList = (List<MelodyIfs>)objectList.GetValue(E3.CharacterSettings);
				valuesListBox.Tag = spellList;
				foreach (var spell in spellList)
				{
					KryptonListItem item = new KryptonListItem();
					item.ShortText = spell.MelodyName;
					item.LongText = string.Empty;
					item.Tag = spell;
					valuesListBox.Items.Add(item);
				}
			}
			else if (objectList.IsGenericList(typeof(String)))
			{
				List<string> spellList = (List<string>)objectList.GetValue(E3.CharacterSettings);
				valuesListBox.Tag = spellList;
				Int32 i = 0;
				foreach (var spell in spellList)
				{
					Int32 tIndex = i;
					KryptonListItem item = new KryptonListItem();
					Models.Ref<string> refInstance = new Models.Ref<string>(() => (string)spellList[tIndex], v => { spellList[tIndex] = v; }, true);
					refInstance.ListItem = item;
					refInstance.ListBox = valuesListBox;
					item.ShortText = spell;
					item.LongText = string.Empty;
					item.Tag = refInstance;
					valuesListBox.Items.Add(item);
					i++;
				}
			}

			else if (objectList.IsGenericDictonary(typeof(string), typeof(string)))
			{
				Dictionary<string, string> dictionary = (Dictionary<string, string>)objectList.GetValue(E3.CharacterSettings);
				valuesListBox.Tag = dictionary;
				foreach (var pair in dictionary)
				{
					KryptonListItem item = new KryptonListItem();
					Models.Ref<string, string> refInstance = new Models.Ref<string, string>(() => (string)dictionary[pair.Key], v => { dictionary[pair.Key] = v; }, () => (string)pair.Key);
					item.ShortText = pair.Key;
					item.LongText = string.Empty;
					item.Tag = refInstance;
					valuesListBox.Items.Add(item);
				}

			}
			else if (objectList.IsGenericSortedDictonary(typeof(string), typeof(string)))
			{
				SortedDictionary<string, string> dictionary = (SortedDictionary<string, string>)objectList.GetValue(E3.CharacterSettings);
				valuesListBox.Tag = dictionary;
				foreach (var pair in dictionary)
				{
					KryptonListItem item = new KryptonListItem();
					Models.Ref<string, string> refInstance = new Models.Ref<string, string>(() => (string)dictionary[pair.Key], v => { dictionary[pair.Key] = v; }, () => (string)pair.Key);
					item.ShortText = pair.Key;
					item.LongText = string.Empty;
					item.Tag = refInstance;
					valuesListBox.Items.Add(item);
				}

			}
			else
			{
				valuesListBox.Tag = null;
				//value data types, going to have to do some reflection shenanagins
				object value = objectList.GetValue(E3.CharacterSettings);
				KryptonListItem item = new KryptonListItem();
				string displayText = value.ToString();
				if (String.IsNullOrWhiteSpace(displayText))
				{
					displayText = "[Not Set]";
				}
				item.ShortText = displayText;
				item.LongText = string.Empty;
				//create a reference holder
				if (value is string)
				{
					Models.Ref<string> refInstance = new Models.Ref<String>(() => (string)objectList.GetValue(E3.CharacterSettings), v => { objectList.SetValue(E3.CharacterSettings, v); }, true);
					refInstance.ListItem = item;
					refInstance.ListBox = valuesListBox;
					item.Tag = refInstance;
				}
				else if (value is bool)
				{

					Models.Ref<bool> refInstance = new Models.Ref<bool>(() => (bool)objectList.GetValue(E3.CharacterSettings), v => { objectList.SetValue(E3.CharacterSettings, v); }, true);
					refInstance.ListItem = item;
					refInstance.ListBox = valuesListBox;
					item.Tag = refInstance;
				}
				else if (value is Int32)
				{

					Models.Ref<Int32> refInstance = new Models.Ref<Int32>(() => (Int32)objectList.GetValue(E3.CharacterSettings), v => { objectList.SetValue(E3.CharacterSettings, v); }, true);
					refInstance.ListItem = item;
					refInstance.ListBox = valuesListBox;
					item.Tag = refInstance;
				}
				else if (value is Int64)
				{

					Models.Ref<Int64> refInstance = new Models.Ref<Int64>(() => (Int64)objectList.GetValue(E3.CharacterSettings), v => { objectList.SetValue(E3.CharacterSettings, v); }, true);
					refInstance.ListItem = item;
					refInstance.ListBox = valuesListBox;
					item.Tag = refInstance;
				}
				valuesListBox.Items.Add(item);

			}

		}
		private void valueList_AddMelodyIfToCollection(string Melody, string IfsName)
		{
			MelodyIfs newMelody = new MelodyIfs();
			newMelody.MelodyName = Melody;
			newMelody.MelodyIfName = IfsName;

			object settings_data_obj = valuesListBox.Tag;

			//update the base storage data
			if (settings_data_obj is List<MelodyIfs>)
			{
				List<MelodyIfs> spellList = (List<MelodyIfs>)settings_data_obj;
				if (spellList.Count > 0 && valuesListBox.SelectedItem != null)
				{
					//put after the current selected
					Int32 index = valuesListBox.SelectedIndex + 1;
					KryptonListItem item = new KryptonListItem();
					item.ShortText = newMelody.MelodyIfName;
					item.LongText = string.Empty;
					item.Tag = newMelody;

					spellList.Insert(index, newMelody);
					valuesListBox.Items.Insert(index, item);
				}
				else
				{
					//no items in the list, just add
					spellList.Add(newMelody);
					KryptonListItem item = new KryptonListItem();
					item.ShortText = newMelody.MelodyIfName;
					item.LongText = string.Empty;
					item.Tag = newMelody;
					valuesListBox.Items.Add(item);
				}
			}
		}
		private void valueList_AddSpellToCollection(SpellData selected)
		{
			
			object settings_data_obj = valuesListBox.Tag;

			//update the base storage data
			if (settings_data_obj is List<Spell>)
			{
				Spell newSpell = Spell.FromProto(selected);
				List<Spell> spellList = (List<Spell>)settings_data_obj;
				if (spellList.Count > 0 && valuesListBox.SelectedItem != null)
				{
					//put after the current selected
					Int32 index = valuesListBox.SelectedIndex + 1;
					KryptonListItem item = new KryptonListItem();
					string nameOfSpell = newSpell.CastName;

					//visual showing of if the spell is disabled
					if (!newSpell.Enabled) nameOfSpell = nameOfSpell + " (disabled)";

					item.ShortText = nameOfSpell;
					item.LongText = string.Empty;
					item.Tag = newSpell;
					if (newSpell.SpellIcon > -1)
					{
						item.Image = _spellIcons[newSpell.SpellIcon];

					}
					spellList.Insert(index, newSpell);
					valuesListBox.Items.Insert(index, item);
				}
				else
				{
					//no items in the list, just add
					spellList.Add(newSpell);
					KryptonListItem item = new KryptonListItem();
					string nameOfSpell = newSpell.CastName;

					//visual showing of if the spell is disabled
					if (!newSpell.Enabled) nameOfSpell = nameOfSpell + " (disabled)";
					item.ShortText = nameOfSpell;
					item.LongText = string.Empty;
					item.Tag = newSpell;
					if (newSpell.SpellIcon > -1)
					{
						item.Image = _spellIcons[newSpell.SpellIcon];
					}
					valuesListBox.Items.Add(item);
				}
			}
		}
		private void valueList_ReplaecSpellToCollection(SpellData selected)
		{
			Spell newSpell = Spell.FromProto(selected);
			object settings_data_obj = valuesListBox.Tag;

			//update the base storage data
			if (settings_data_obj is List<Spell>)
			{
				List<Spell> spellList = (List<Spell>)settings_data_obj;
				if (spellList.Count > 0 && valuesListBox.SelectedItem != null)
				{
					//put after the current selected
					Int32 index = valuesListBox.SelectedIndex;
					KryptonListItem item = new KryptonListItem();
					string nameOfSpell = newSpell.CastName;

					//visual showing of if the spell is disabled
					if (!newSpell.Enabled) nameOfSpell = nameOfSpell + " (disabled)";
					item.ShortText = nameOfSpell;
					item.LongText = string.Empty;
					item.Tag = newSpell;
					if (newSpell.SpellIcon > -1)
					{
						item.Image = _spellIcons[newSpell.SpellIcon];

					}
					Spell oldSpell = ((Spell)((KryptonListItem)valuesListBox.SelectedItem).Tag);
					oldSpell.TransferFlags(newSpell);

					spellList.RemoveAt(index);
					spellList.Insert(index, newSpell);
					valuesListBox.Items.RemoveAt(index);
					valuesListBox.Items.Insert(index, item);
				}
				
			}
		}
		public static void InitEditor( ref AddSpellEditor editor, SortedDictionary<string, SortedDictionary<string, List<SpellData>>> spellData)
		{
			if (editor == null)
			{
				editor = new AddSpellEditor(spellData, _spellIcons);
				editor.StartPosition = FormStartPosition.CenterParent;
			}
		}
		private void ShowEditorDialog(ref AddSpellEditor editor, SortedDictionary<string, SortedDictionary<string, List<SpellData>>> spellData, bool replaceSpell=false)
		{

			InitEditor(ref editor,spellData);

			if (editor.ShowDialog() == DialogResult.OK)
			{
				if (editor.SelectedSpell != null)
				{
					string selectedSection = sectionComboBox.SelectedItem.ToString();

					if(selectedSection=="Buffs")
					{
						editor.SelectedSpell.IsBuff = true;
					}
					if (replaceSpell)
					{
						valueList_ReplaecSpellToCollection(editor.SelectedSpell);

					}
					else
					{
						valueList_AddSpellToCollection(editor.SelectedSpell);
					}
					
				}
			}
		}

		#endregion
		
		
		private void valueListContextMenu_Opening(object sender, CancelEventArgs e)
		{
			foreach (KryptonContextMenuItemBase items in valueListContextMenu.Items)
			{

				if (items is KryptonContextMenuItems)
				{
					foreach (KryptonContextMenuItemBase item in ((KryptonContextMenuItems)items).Items)
					{
						if (item is KryptonContextMenuItem)
						{
							var menuItem = (KryptonContextMenuItem)item;
							menuItem.Visible = false;
							SetMenuItemVisablity(menuItem);
						}
					}

				}
			}
		}

		private void saveButton_Click(object sender, EventArgs e)
		{
			E3.CharacterSettings.SaveData();
			var mb = new MessageBox();
			mb.StartPosition = FormStartPosition.CenterParent;
			mb.Text = "Save Data Complete!";
			mb.lblMessage.Text = "Save Data Complete!";
			mb.buttonOK.Visible = false;
			mb.buttonCancel.Visible = false;
			mb.buttonOkayOnly.Visible = true;

			mb.ShowDialog(); //it will always be an ok
			RefeshListView();


		}

		private void donateButton_Click(object sender, EventArgs e)
		{
			var mb = new MessageBox();
			mb.StartPosition = FormStartPosition.CenterParent;
			mb.Text = "Donate for Github Costs and Pizza (Paypal)";
			mb.lblMessage.Text = "Hi, Ty for thinking of donating! If you wish to donate, please use friends and family.";

			if (mb.ShowDialog() == DialogResult.OK)
			{
				System.Diagnostics.Process.Start("https://www.paypal.com/paypalme/RekkaSoftware");

			}
		}

		private void viewFileButton_Click(object sender, EventArgs e)
		{
			if(_textViewer==null)
			{
				_textViewer = new ConfigTextViewer();
				_textViewer.StartPosition = FormStartPosition.CenterParent;
				
			}
			_textViewer.FileToShow = E3.CharacterSettings._fileName;
			if (_textViewer.ShowDialog()== DialogResult.OK)
			{

			}

		}

		private void ConfigEditor_FormClosing(object sender, FormClosingEventArgs e)
		{
			ShouldProcess = false;
		}

	
	}
}
