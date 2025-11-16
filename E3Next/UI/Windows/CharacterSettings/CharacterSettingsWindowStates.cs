using E3Core.Data;
using IniParser.Model;
using System;
using System.Collections.Generic;
using static MonoCore.E3ImGUI;
using MonoCore;

namespace E3Core.UI.Windows.CharacterSettings
{
	public enum AddType { Spells, AAs, Discs, Skills, Items }
	public enum CatalogMode { Standard, BardSong }
	public enum SettingsTab { Character, General, Advanced }

	public class E3Spell
	{
		public string Name;
		public string Category;
		public string Subcategory;
		public int Level;
		public string CastName;
		public string TargetType;
		public string SpellType;
		public int Mana;
		public double CastTime;
		public int Recast;
		public double Range;
		public string Description;
		public string ResistType;
		public int ResistAdj;
		public string CastType; // AA/Spell/Disc/Ability/Item/None
		public int SpellGem;
		public List<string> SpellEffects = new List<string>();
		public int SpellIcon = -1; // Spell icon index for display
		public override string ToString() => Name;
	}
	public class State_CatalogWindow
	{
		public bool ReplaceMode = false;
		public Int32 ReplaceIndex = -1;
		public E3Spell SelectedCategorySpell = null;
		public string SelectedCategory = String.Empty;
		public string SelectedSubCategory = String.Empty;
		public string Filter = string.Empty;
		public CatalogMode Mode = CatalogMode.Standard;
		public AddType CurrentAddType = AddType.Spells;

	}
	public class State_MainWindow
	{
		public IniData CurrentINIData;
		public string CurrentINIFileNameFull = string.Empty;

		public string SelectedCharacterSection = string.Empty;//In use?
		public string SelectedSection = string.Empty;
		public string SelectedAddInLine = String.Empty;
		public string SelectedKey = string.Empty;
		public Int32 SelectedValueIndex = -1;

		public string SignatureOfSelectedKeyValue = String.Empty;

		public string[] IniFilesFromDisk = Array.Empty<string>();
		public List<string> SectionsOrdered = new List<string>();

		public bool Show_ShowIntegratedEditor = true;
		public bool Show_AddKey = false;
		public bool ShowOfflineCharacters = false;
		public bool ConfigIsDirty = false;
		public string LastIniPath = String.Empty;
		public int InLineEditIndex = -1;
		// Context menu state for Ifs/Burn sections
		public bool Show_ContextMenu = false;
		public string ContextMenuFor = string.Empty; // "Ifs" or "Burn"
													 //buffers
		public string Buffer_KeySearch = String.Empty;
		public string Buffer_NewKey = string.Empty;
		public string Buffer_NewValue = String.Empty;
		public string Buffer_InlineEdit = string.Empty;
		public int PendingValueSelection = -1;

		public string Signature_CurrentEditedSpell = String.Empty;
		public Spell Currently_EditableSpell = null;

		// Store the width of the right panel's values pane (left column of right panel)
		public float RightPaneValuesWidth = -1f; // -1 means not yet initialized


	}
	public class State_AllPlayers
	{
		public Dictionary<string, string> Data_Edit = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		public List<KeyValuePair<string, string>> Data_Rows = new List<KeyValuePair<string, string>>();
		public bool ShowWindow = false;
		public object DataLock = new object();
		public string ReqSection = string.Empty;
		public string ReqKey = string.Empty;
		public long LastUpdatedAt = 0;
		public int RefershInterval = 5000;
		public string Status = string.Empty;
		public Dictionary<string, string> ServerByToon = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		// "All Players" key view state/cache
		public long NextRefreshAtMs = 0;
		public bool Refreshing = false;


	}
	public class State_BardEditor
	{
		public Int32 BardSongPickerIndex = -1;
		//currently being used to 'empty' the input fields when a value is added. 
		//this should be corrected
		public int SongInputVersion = 0;
		public int ConditionInputVersion = 0;
		//end input correction
		public string SampleIfStatus = string.Empty;
		public string SampleIfFilter = string.Empty;
		public string MelodyCondition = string.Empty;
		public string MelodyModalStatus = string.Empty;
		public string MelodyStatus = string.Empty;
		public bool SongPickerJustSelected = false;
		public List<KeyValuePair<string, string>> SampleIfLines = new List<KeyValuePair<string, string>>();
		// Ifs: add-new helper input buffers
		public string MelodyName = string.Empty;
		public List<string> MelodySongs = new List<string>();
		public Dictionary<int, string> MelodyBuffers = new Dictionary<int, string>();
		public List<int> MelodyGems = new List<int>();
		public Dictionary<int, string> MelodyGemBuffers = new Dictionary<int, string>();

	}
	public class State_SpellInfo
	{
		public E3Spell Spell = null;
	}
	public class State_SpellEditor
	{
	
		public string Signature_CurrentEditedSpell = String.Empty;
		public string CurrentSpellPreviewCache = String.Empty;
		public string ManualEditBuffer = String.Empty;
		public bool ManualInputBufferInUse = false;
		public bool IsDirty = false;
		public readonly string WinName_CastTargetHelperWindow = "Cast Target Helper";
		public readonly string WinName_CastTargetPickerWindowTitle = "Cast Target Picker";
		public const float SpellEditorDefaultTextWidth = 320f;
		public const float SpellEditorDefaultNumberWidth = 140f;
		public const float SpellEditorDefaultCheckboxWidth = 20f;
		public bool ShowCastTargetPicker
		{
			get { return imgui_Begin_OpenFlagGet(WinName_CastTargetPickerWindowTitle); }
			set { imgui_Begin_OpenFlagSet(WinName_CastTargetPickerWindowTitle, value); }
		}
		public bool ShowCastTargetHelper
		{
			get { return imgui_Begin_OpenFlagGet(WinName_CastTargetHelperWindow); }
			set { imgui_Begin_OpenFlagSet(WinName_CastTargetHelperWindow, value); }
		}
		public void Reset()
		{
			IsDirty = false;
			CurrentSpellPreviewCache = String.Empty;
			ManualEditBuffer = String.Empty;
			ManualInputBufferInUse = false;
			ShowCastTargetHelper = false;
			ShowCastTargetPicker = false;
		}
	}
	public class State_CatalogGems
	{

		public string Source = "Unknown"; // "Local", "Remote (ToonName)", or "Unknown"
		public string[] Gems = new string[12]; // Gem data from catalog response
		public int[] GemIcons = new int[12]; // Spell icon indices for gems

	}
	public class State_FoodDrink
	{
		public string Key = string.Empty; // "Food" or "Drink"
		public string Status = string.Empty;
		public List<string> Candidates = new List<string>();
		public bool ScanRequested = false;
		public bool Pending = false;
		public string PendingToon = string.Empty;
		public string PendingType = string.Empty;
		public long TimeoutAt = 0;
	}

	public class CharacterSettingsState
	{
		private State_CatalogWindow _catalogWindowState = new State_CatalogWindow();
		private State_MainWindow _mainWindowState = new State_MainWindow();
		private State_AllPlayers _allPlayersState = new State_AllPlayers();
		private State_BardEditor _bardEditorState = new State_BardEditor();
		private State_SpellInfo _spellInfoState = new State_SpellInfo();
		private State_SpellEditor _spellEditorState = new State_SpellEditor();
		private State_CatalogGems _catalogGemsState = new State_CatalogGems();
		private State_FoodDrink _foodDrinkState = new State_FoodDrink();
		public CharacterSettingsState()
		{
			//set all initial windows to not show
			if (Core._MQ2MonoVersion > 0.34m) ClearWindows();
		}

		public T GetState<T>()
		{
			var type = typeof(T);
			if (type == typeof(State_CatalogWindow))
			{
				return (T)(object)_catalogWindowState;
			}
			else if (type == typeof(State_MainWindow))
			{
				return (T)(object)_mainWindowState;
			}
			else if (type == typeof(State_AllPlayers))
			{
				return (T)(object)_allPlayersState;
			}
			else if(type==typeof(State_CatalogGems))
			{
				return (T)(object)_catalogGemsState;
			}
			else if(type==typeof(State_FoodDrink))
			{
				return (T)(object)_foodDrinkState;
			}
			else if (type == typeof(State_BardEditor))
			{
				return (T)(object)_bardEditorState;
			}
			else if (type == typeof(State_SpellInfo))
			{
				return (T)(object)_spellInfoState;
			}
			else if (type == typeof(State_SpellEditor))
			{
				return (T)(object)_spellEditorState;
			}
			return default(T);
		}

		//state
		//public string State_SelectedSection = string.Empty;


		//// "Ifs" or "Burn",// Inline add editor state (rendered in Values column)
		//public string State_SelectedAddInLine = String.Empty;
		//public string State_SelectedKey = string.Empty;
		//public string State_SectionAndKeySig = String.Empty;
		//public Int32 State_SelectedValueIndex = -1;

		public bool State_CatalogReady = false;
		public bool State_CatalogLoading = false;
		public bool State_CatalogLoadRequested = false;

		//Note on Volatile variables... all this means is if its set on another thread, we will eventually get the update.
		//its somewhat one way, us setting the variable on this side doesn't let the other thread see the update.
		public volatile bool State_GemsAvailable = false; // Whether we have gem data




		//status
		public string Status_CatalogRequest = String.Empty;

		//windows
		public string WinName_Donate = "E3Donate";
		public bool Show_Donate
		{
			get { return imgui_Begin_OpenFlagGet(WinName_Donate); }
			set { imgui_Begin_OpenFlagSet(WinName_Donate, value); }
		}
		public string WinName_ThemeSettings = "E3Theme";
		public bool Show_ThemeSettings
		{
			get { return imgui_Begin_OpenFlagGet(WinName_ThemeSettings); }
			set { imgui_Begin_OpenFlagSet(WinName_ThemeSettings, value); }
		}


		public string WinName_AddModal = "E3Catalog";
		public bool Show_AddModal
		{
			get
			{
				bool currentValue = imgui_Begin_OpenFlagGet(WinName_AddModal);
				//if the window is currently closed, clear out the values
				//this is necessary as you can close via the X in C++ land and won't see the update till we check
				if (!currentValue)
				{
					_bardEditorState.BardSongPickerIndex = -1;
					_catalogWindowState.Mode = CatalogMode.Standard;
					_catalogWindowState.ReplaceMode = false;
					_catalogWindowState.ReplaceIndex = -1;
					_catalogWindowState.SelectedCategorySpell = null; // Clear selection when closing via X
				}
				return currentValue;

			}
			set
			{
				imgui_Begin_OpenFlagSet(WinName_AddModal, value);
			}
		}

		public string WinName_FoodDrinkModal = "E3PickInventory";
		public bool Show_FoodDrinkModal
		{
			get { return imgui_Begin_OpenFlagGet(WinName_FoodDrinkModal); }
			set { imgui_Begin_OpenFlagSet(WinName_FoodDrinkModal, value); }
		}

		public string WinName_BardMelodyHelper = "E3BardMelody";
		public bool Show_BardMelodyHelper
		{
			get { return imgui_Begin_OpenFlagGet(WinName_BardMelodyHelper); }
			set { imgui_Begin_OpenFlagSet(WinName_BardMelodyHelper, value); }
		}

		public string WinName_BardSampleIfModal = "E3BardSampleIfs";
		public bool Show_BardSampleIfModal
		{
			get { return imgui_Begin_OpenFlagGet(WinName_BardSampleIfModal); }
			set { imgui_Begin_OpenFlagSet(WinName_BardSampleIfModal, value); }
		}

		public string WinName_ToonPickerModal = "E3PickToons";
		public bool Show_ToonPickerModal
		{
			get { return imgui_Begin_OpenFlagGet(WinName_ToonPickerModal); }
			set { imgui_Begin_OpenFlagSet(WinName_ToonPickerModal, value); }
		}
		public string WinName_SpellInfoModal = "E3SpellInfo";
		public bool Show_SpellInfoModal
		{
			get { return imgui_Begin_OpenFlagGet(WinName_SpellInfoModal); }
			set { imgui_Begin_OpenFlagSet(WinName_SpellInfoModal, value); }
		}
		public string WinName_SpellModifier = "E3SpellModifiers";
		public bool Show_SpellModifier
		{
			get { return imgui_Begin_OpenFlagGet(WinName_SpellModifier); }
			set { imgui_Begin_OpenFlagSet(WinName_SpellModifier, value); }
		}

		public string WinName_IfAppendModal = "E3AppendIf";
		public bool Show_IfAppendModal
		{
			get { return imgui_Begin_OpenFlagGet(WinName_IfAppendModal); }
			set { imgui_Begin_OpenFlagSet(WinName_IfAppendModal, value); }
		}
		public string WinName_IfSampleModal = "E3SampleIfs";
		public bool Show_IfSampleModal
		{
			get { return imgui_Begin_OpenFlagGet(WinName_IfSampleModal); }
			set { imgui_Begin_OpenFlagSet(WinName_IfSampleModal, value); }
		}





		//data


		//requests
		public bool Request_AllplayersRefresh = false;


		public void ClearWindows()
		{
			Show_AddModal = false;
			Show_Donate = false;
			Show_BardMelodyHelper = false;
			Show_BardSampleIfModal = false;
			Show_FoodDrinkModal = false;
			Show_IfAppendModal = false;
			Show_IfSampleModal = false;
			Show_SpellInfoModal = false;
			Show_SpellModifier = false;
			Show_ThemeSettings = false;
			Show_ToonPickerModal = false;
		}
		public void ClearAddInLine()
		{
			var state = this.GetState<State_MainWindow>();
			state.Show_AddKey = false;
			state.SelectedKey = string.Empty;
			state.SelectedAddInLine = String.Empty;
			state.Buffer_NewKey = string.Empty;
			state.Buffer_NewValue = string.Empty;
		}

	}
}
