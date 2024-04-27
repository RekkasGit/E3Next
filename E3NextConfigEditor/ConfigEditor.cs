using ComponentFactory.Krypton.Navigator;
using ComponentFactory.Krypton.Toolkit;
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

namespace E3NextConfigEditor
{
	public partial class ConfigEditor : KryptonForm
	{
		public static DealerClient _tloClient;
		public static List<Bitmap> _spellIcons = new List<Bitmap>();
		public static Dictionary<string, Dictionary<string, FieldInfo>> _charSettingsMappings;
		public static E3Core.Data.Class CurrentClass;
		public ConfigEditor()
		{


			InitializeComponent();

			LoadData();

			
		}




		public void LoadData()
		{
			//DarkMode.ChangeTheme(this, this.Controls);
			//this.Opacity = this.Opacity - 0.001;
			//System.Windows.Forms.Application.DoEvents();
			//this.Opacity = 100;


			_tloClient = new DealerClient(49793);
			IMQ _mqClient = new MQ.MQClient(_tloClient);

			byte[] result = _tloClient.RequestRawData("${E3.AA.ListAll}");
			SpellDataList aas = SpellDataList.Parser.ParseFrom(result);

			result = _tloClient.RequestRawData("${E3.SpellBook.ListAll}");
			SpellDataList bookSpells = SpellDataList.Parser.ParseFrom(result);

			result = _tloClient.RequestRawData("${E3.Discs.ListAll}");
			SpellDataList discs = SpellDataList.Parser.ParseFrom(result);

			string classValue = e3util.ClassNameFix(_tloClient.RequestData("${Me.Class}"));
            System.Enum.TryParse(classValue, out CurrentClass);


			//lets sort all the spells by cataegory/subcategory and levels

			Dictionary<string, Dictionary<string, List<SpellData>>> spellDataOrganized = new Dictionary<string, Dictionary<string, List<SpellData>>>();

			foreach (SpellData s in bookSpells.Data)
			{

				Dictionary<string, List<SpellData>> subCategoryLookup;
				List<SpellData> spellList;
				if (!spellDataOrganized.TryGetValue(s.Category, out subCategoryLookup))
				{
					subCategoryLookup = new Dictionary<string, List<SpellData>>();
					spellDataOrganized.Add(s.Category, subCategoryLookup);
				}
				if (!subCategoryLookup.TryGetValue(s.Subcategory, out spellList))
				{
					spellList = new List<SpellData>();
					subCategoryLookup.Add(s.Subcategory, spellList);
				}

				spellList.Add(s);

			}

			//now sort all the levels int the lists
			foreach (var pair in spellDataOrganized)
			{
				foreach (var keySet in pair.Value.Keys.ToList())
				{
					spellDataOrganized[pair.Key][keySet] = spellDataOrganized[pair.Key][keySet].OrderByDescending(x => x.Level).ToList();
				}
			}

			E3.MQ = _mqClient;
			E3.Log = new Logging(E3.MQ); ;
			E3.CurrentName = _mqClient.Query<string>("${Me.CleanName}");
			E3.ServerName = e3util.FormatServerName(_mqClient.Query<string>("${MacroQuest.Server}"));

			_tloClient.RequestData("${E3.TLO.BulkBegin}");
			//create settings files here
			bool mergeUpdates = false;
			E3.CharacterSettings = new E3Core.Settings.CharacterSettings(mergeUpdates);
			_tloClient.RequestData("${E3.TLO.BulkEnd}");


			//load image data
			for(Int32 i =1;i<=63;i++)
			{
				using (var image = new TGA.TargaImage($"D:\\EQ\\EQLive\\uifiles\\default\\spells{i.ToString("D2")}.tga"))
				{
					using(var bitmap = image.Image)
					{
						for (Int32 y = 0; y < 6; y++)
						{
							for(Int32 x=0;x<6;x++)
							{
								var icon = bitmap.Clone(new Rectangle(x*40, y*40, 40, 40), bitmap.PixelFormat);
								_spellIcons.Add(icon);
							}
						}
					}
				}
			}

			_charSettingsMappings = e3util.GetSettingsMappedToInI();

			List<string> importantSections = new List<string>() { "Misc", "Assist Settings", "Nukes", "Debuffs", "DoTs on Assist", "DoTs on Command", "Heals", "Buffs", "Melee Abilities", "Burn", "Pets", "Ifs" };

			List<string> sectionNames = new List<string>();
			foreach (var section in E3.CharacterSettings.ParsedData.Sections)
			{

				sectionNames.Add(section.SectionName);

			}
			sectionNames = sectionNames.OrderBy(x => x).ToList();

			foreach (var section in importantSections)
			{
				if (E3.CharacterSettings.ParsedData.Sections.ContainsSection(section))
				{
					sectionComboBox.Items.Add(section);

				}
			}

			foreach (var section in sectionNames)
			{
				if (importantSections.Contains(section, StringComparer.OrdinalIgnoreCase)) continue;
				sectionComboBox.Items.Add(section);

			}

		}
		#region comboBoxs
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

			string selectedSection = sectionComboBox.SelectedItem.ToString();

			var section = E3.CharacterSettings.ParsedData.Sections[selectedSection];

			if(section != null)
			{

				foreach(var key in section)
				{
				
					subsectionComboBox.Items.Add(key.KeyName);
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
			valuesListBox.Items.Clear();
			string selectedSection = sectionComboBox.SelectedItem.ToString();
			var section = E3.CharacterSettings.ParsedData.Sections[selectedSection];
		
			if (section != null)
			{
				string selectedSubSection = subsectionComboBox.SelectedItem.ToString();


				var objectList = _charSettingsMappings[selectedSection][selectedSubSection];

				if(objectList.IsGenericList(typeof(Spell)))
				{
					
					List<Spell> spellList = (List<Spell>)objectList.GetValue(E3.CharacterSettings);
				
					valuesListBox.Tag= spellList;
					foreach(var spell in spellList)
					{
						KryptonListItem item = new KryptonListItem();
						item.ShortText = spell.SpellName;
						item.LongText = string.Empty;
						item.Tag = spell;
						if (spell.SpellIcon > -1)
						{
							item.Image = _spellIcons[spell.SpellIcon];

						}
						valuesListBox.Items.Add(item);
					}

				}
				else if(objectList.IsGenericList(typeof(SpellRequest)))
				{
					List<SpellRequest> spellList = (List<SpellRequest>)objectList.GetValue(E3.CharacterSettings);
					valuesListBox.Tag = spellList;
					foreach (var spell in spellList)
					{
						KryptonListItem item = new KryptonListItem();
						item.ShortText = spell.SpellName;
						item.LongText = string.Empty;
						item.Tag = spell;
						if (spell.SpellIcon > -1)
						{
							item.Image = _spellIcons[spell.SpellIcon];

						}
						valuesListBox.Items.Add(item);
					}
				}
				else if (objectList.IsGenericList(typeof(String)))
				{
					List<string> spellList = (List<string>)objectList.GetValue(E3.CharacterSettings);
					valuesListBox.Tag = spellList;
					foreach (var spell in spellList)
					{
						KryptonListItem item = new KryptonListItem();
						item.ShortText = spell;
						item.LongText = string.Empty;
						item.Tag = spell;
						valuesListBox.Items.Add(item);
					}
				}
				else
				{
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
						Models.Ref<string> refInstance = new Models.Ref<String>(() => (string)objectList.GetValue(E3.CharacterSettings), v => { objectList.SetValue(E3.CharacterSettings,v); });
						refInstance.ListItem = item;
						refInstance.ListBox = valuesListBox;
						item.Tag = refInstance;
					}
					else if (value is bool)
					{

						Models.Ref<bool> refInstance = new Models.Ref<bool>(() => (bool)objectList.GetValue(E3.CharacterSettings), v => { objectList.SetValue(E3.CharacterSettings, v); });
						refInstance.ListItem = item;
						refInstance.ListBox = valuesListBox;
						item.Tag = refInstance;
					}
					else if (value is Int32)
					{

						Models.Ref<Int32> refInstance = new Models.Ref<Int32>(() => (Int32)objectList.GetValue(E3.CharacterSettings), v => { objectList.SetValue(E3.CharacterSettings, v); });
						refInstance.ListItem = item;
						refInstance.ListBox = valuesListBox;
						item.Tag = refInstance;
					}
					else if (value is Int64)
					{

						Models.Ref<Int64> refInstance = new Models.Ref<Int64>(() => (Int64)objectList.GetValue(E3.CharacterSettings), v => { objectList.SetValue(E3.CharacterSettings, v); });
						refInstance.ListItem = item;
						refInstance.ListBox = valuesListBox;
						item.Tag = refInstance;
					}
					valuesListBox.Items.Add(item);

				}

				//var valueList = section.GetKeyData(selectedSubSection).ValueList;

				//foreach (var value in valueList)
				//{
				//	if (Spell.LoadedSpellByConfigEntry.TryGetValue(value, out var result))
				//	{
				//		KryptonListItem item = new KryptonListItem();
				//		item.ShortText = result.SpellName;
				//		item.LongText = string.Empty;
				//		item.Tag = result;
				//		if(result.SpellIcon>-1)
				//		{
				//			item.Image = _spellIcons[result.SpellIcon];

				//		}
				//		//item.Image = imageList.Images[_rand.Next(imageList.Images.Count - 1)];
				//		valuesListBox.Items.Add(item);
				//	}
				//	else
				//	{
				//		valuesListBox.Items.Add(value);
				//	}
				//}

			}
				
			
		}
		#endregion
		#region DragNDropListBox
		Int64 mouseDownTimeStamp = 0;
		System.Diagnostics.Stopwatch _stopwatch = new Stopwatch();
		System.Timers.Timer _timer = new System.Timers.Timer(50);

		private void updatePropertyGrid()
		{
			propertyGrid.SelectedObject = null;
			//need to pull out the Tag and verify which type so we know what to pass to the 
			//property grid
			KryptonListItem listItem = ((KryptonListItem)valuesListBox.SelectedItem);
			if (listItem.Tag is Spell)
			{
				
				propertyGrid.SelectedObject = new Models.SpellDataProxy((Spell)listItem.Tag);
				
			}
			else if (listItem.Tag is SpellRequest)
			{
				propertyGrid.SelectedObject = new Models.SpellRequestDataProxy((SpellRequest)listItem.Tag);
			}
			else if (listItem.Tag is Models.Ref<string> || listItem.Tag is Models.Ref<bool> || listItem.Tag is Models.Ref<Int32> || listItem.Tag is Models.Ref<Int64>)
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
		}

		private void _timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
		{
			if(mouseDownTimeStamp !=0 && mouseDownTimeStamp+400 < _stopwatch.ElapsedMilliseconds)
			{
				
				valuesListBox.Invoke((MethodInvoker)delegate
				{
					if (valuesListBox.SelectedItem == null) return;
					valuesListBox.DoDragDrop(valuesListBox.SelectedItem, DragDropEffects.Move);
					mouseDownTimeStamp = 0;
				});

				mouseDownTimeStamp = 0;
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
			this.valuesListBox.Items.Remove(data);
			this.valuesListBox.Items.Insert(index, data);
			valuesListBox.SelectedIndex = index;
			
			mouseDownTimeStamp = 0;
		}
		#endregion

		private void valuesListBox_MouseMove(object sender, MouseEventArgs e)
		{
			Debug.WriteLine("Mouse move");

		}

		private void valuesListBox_MouseHover(object sender, EventArgs e)
		{
			Debug.WriteLine("Mouse Hover");
		}

		
	}
}
