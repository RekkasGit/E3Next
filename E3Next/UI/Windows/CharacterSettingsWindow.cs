using E3Core.Data;
using E3Core.Processors;
using E3Core.Server;
using E3Core.Settings;
using E3Core.Utility;
using IniParser.Model;
using MonoCore;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using static MonoCore.E3ImGUI;

namespace E3Core.UI.Windows
{

	public static class CharacterSettingsWindow
	{


		private class State_CatalogWindow
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
		private class State_MainWindow
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

			public bool Show_ShowIntegratedEditor = false;
			public bool Show_AddInLine = false;
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
			

		}
		private class State_AllPlayers
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
		}
		private class State_BardEditor
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
		private class CharacterSettingsState
		{
			private State_CatalogWindow _catalogWindowState = new State_CatalogWindow();
			private State_MainWindow _mainWindowState = new State_MainWindow();
			private State_AllPlayers _allPlayersState = new State_AllPlayers();
			private State_BardEditor _bardEditorState = new State_BardEditor();
			public CharacterSettingsState() {
				//set all initial windows to not show
				if(Core._MQ2MonoVersion>0.34m) ClearWindows();
			}

			public T GetState<T>()
			{
				var type = typeof(T);
				if (type == typeof(State_CatalogWindow))
				{
					return (T)(object)_catalogWindowState;
				}
				else if(type == typeof(State_MainWindow))
				{
					return (T)(object)_mainWindowState;
				}
				else if (type == typeof(State_AllPlayers))
				{
					return (T)(object)_allPlayersState;
				}
				else if (type == typeof(State_BardEditor))
				{
					return (T)(object)_bardEditorState;
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
				get	{
					bool currentValue = imgui_Begin_OpenFlagGet(WinName_AddModal);
					//if the window is currently closed, clear out the values
					//this is necessary as you can close via the X in C++ land and won't see the update till we check
					if(!currentValue)
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
			public string WinName_SpellInfoModal= "E3SpellInfo";
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
			public string WinName_IfSampleModal= "E3SampleIfs";
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
				var state= _state.GetState<State_MainWindow>();
				state.Show_AddInLine = false;
				state.SelectedKey = string.Empty;
				state.SelectedAddInLine = String.Empty;
				state.Buffer_NewKey = string.Empty;
				state.Buffer_NewValue = string.Empty;
			}
		
		}

		public static Logging _log = E3.Log;
		private static IMQ MQ = E3.MQ;
		private static ISpawns _spawns = E3.Spawns;

		private static CharacterSettingsState _state = new CharacterSettingsState();

		//A very large bandaid on the Threading of this window
		//used when trying to get a pointer to the _cfg objects.
		private static object _dataLock = new object();

		private static readonly Dictionary<string, (float r, float g, float b, float a)> _inlineDescriptionColorMap = new Dictionary<string, (float, float, float, float)>(StringComparer.OrdinalIgnoreCase)
		{
			["color=gold"] = (0.95f, 0.85f, 0.35f, 1.0f),
			["color=yellow"] = (1.0f, 0.92f, 0.23f, 1.0f),
			["color=orange"] = (1.0f, 0.6f, 0.2f, 1.0f),
			["color=red"] = (0.9f, 0.3f, 0.3f, 1.0f),
			["color=green"] = (0.3f, 0.9f, 0.5f, 1.0f),
			["color=blue"] = (0.35f, 0.6f, 0.95f, 1.0f),
			["color=teal"] = (0.3f, 0.85f, 0.85f, 1.0f),
			["color=purple"] = (0.75f, 0.55f, 0.95f, 1.0f),
			["color=white"] = (0.95f, 0.95f, 0.95f, 1.0f),
			["color=gray"] = (0.6f, 0.6f, 0.6f, 1.0f),
			["color=silver"] = (0.8f, 0.8f, 0.85f, 1.0f)
		};

		#region Variables
		// Catalogs and Add modal state

		private static string _cfg_CatalogSource = "Unknown"; // "Local", "Remote (ToonName)", or "Unknown"
															  // Memorized gem data from catalog responses with spell icon support

		private static string[] _cfg_CatalogGems = new string[12]; // Gem data from catalog response
		private static int[] _cfg_CatalogGemIcons = new int[12]; // Spell icon indices for gems

		/// <summary>
		///Data organized into Category, Sub Category, List of Spells.
		///always get a pointer to these via the method GetCatalogByType
		/// </summary>
		private static SortedDictionary<string, SortedDictionary<string, List<E3Spell>>> _catalog_Spells = new SortedDictionary<string, SortedDictionary<string, List<E3Spell>>>(),
		_catalog_AA = new SortedDictionary<string, SortedDictionary<string, List<E3Spell>>>(),
		_catalog_Disc = new SortedDictionary<string, SortedDictionary<string, List<E3Spell>>>(),
		_catalog_Skills = new SortedDictionary<string, SortedDictionary<string, List<E3Spell>>>(),
		_catalog_Items = new SortedDictionary<string, SortedDictionary<string, List<E3Spell>>>();

		private static Dictionary<string, SpellData> _spellCatalogLookup = new Dictionary<string, SpellData>(StringComparer.OrdinalIgnoreCase),
		_discCatalogLookup = new Dictionary<string, SpellData>(StringComparer.OrdinalIgnoreCase),
		_aaCatalogLookup = new Dictionary<string, SpellData>(StringComparer.OrdinalIgnoreCase),
		_skillCatalogLookup = new Dictionary<string, SpellData>(StringComparer.OrdinalIgnoreCase),
		_itemCatalogLookup = new Dictionary<string, SpellData>(StringComparer.OrdinalIgnoreCase);




		private static E3Spell _cfgCatalogInfoSpell = null;
		private static E3Spell _cfgSpellInfoSpell = null;

		private enum AddType { Spells, AAs, Discs, Skills, Items }
		private enum CatalogMode { Standard, BardSong }
		
	
		// Food/Drink picker state
		private static string _cfgFoodDrinkKey = string.Empty; // "Food" or "Drink"
		private static string _cfgFoodDrinkStatus = string.Empty;
		private static List<string> _cfgFoodDrinkCandidates = new List<string>();
		private static bool _cfgFoodDrinkScanRequested = false;
		// Toon picker (Heals: Tank / Important Bot)
		private static string _cfgToonPickerStatus = string.Empty;
		private static List<string> _cfgToonCandidates = new List<string>();
		// Append If modal state
		private static int _cfgIfAppendRow = -1;
		private static List<string> _cfgIfAppendCandidates = new List<string>();
		private static string _cfgIfAppendStatus = string.Empty;
		// Ifs import (sample) modal state
		private static List<System.Collections.Generic.KeyValuePair<string, string>> _cfgIfSampleLines = new List<System.Collections.Generic.KeyValuePair<string, string>>();
		private static string _cfgIfSampleStatus = string.Empty;
		
		
		

		
		private static bool _cfgFoodDrinkPending = false;
		private static string _cfgFoodDrinkPendingToon = string.Empty;
		private static string _cfgFoodDrinkPendingType = string.Empty;
		private static long _cfgFoodDrinkTimeoutAt = 0;

		// Config UI toggle: "/e3imgui".
		private static readonly string _windowName = "E3Next Config";
		private static bool _imguiInitDone = false;
		private static bool _imguiContextReady = false;
		private enum SettingsTab { Character, General, Advanced }
		private static SettingsTab _activeSettingsTab = SettingsTab.Character;
		private static string _activeSettingsFilePath = string.Empty;
		private static string[] _activeSettingsFileLines = Array.Empty<string>();
		
	
		// Inline edit helpers
		private static string _cfgInlineEditBuffer = string.Empty;
		private static int _cfgPendingValueSelection = -1;
		private static string _cfgSelectedClass = string.Empty;
		private const float _valueRowActionStartOffset = 46f;
	
		// Collapsible section state tracking
		private static Dictionary<string, bool> _cfgSectionExpanded = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);

		// Spell flag editor state
		private static SpellValueEditState _cfgSpellEditState = null;
		private static string _cfgSpellEditSignature = string.Empty;
		private static bool _cfgShowSpellModifierModal = false;
		// Integrated editor panel state (replaces modal)
		private static string _cfgManualEditBuffer = string.Empty;
		private static readonly string[] _spellKeyOutputOrder = new[]
		{
			"Gem", "Ifs", "CheckFor", "CastIF", "HealPct", "HealthMax", "Zone", "MinSick",
			"BeforeSpell", "AfterSpell", "BeforeEvent", "AfterEvent", "MinMana", "MaxMana", "MinEnd",
			"MinDurationBeforeRecast", "MaxTries", "Reagent", "CastType", "PctAggro", "Delay", "RecastDelay",
			"AfterEventDelay", "AfterSpellDelay", "BeforeEventDelay", "BeforeSpellDelay", "AfterCastDelay",
			"AfterCastCompletedDelay", "SongRefreshTime", "StackRequestItem", "StackRequestTargets",
			"StackCheckInterval", "StackRecastDelay", "MinHP", "MinHPTotal", "GiveUpTimer", "TriggerSpell",
			"MinAggro", "MaxAggro",
			"ExcludedClasses", "ExcludedNames"
		};
		private static readonly string[] _spellFlagOutputOrder = new[]
		{
			"NoInterrupt", "IgnoreStackRules", "NoTarget", "NoAggro", "NoBurn", "Rotate",
			"NoMidSongCast", "GoM", "AllowSpellSwap", "NoEarlyRecast", "NoStack", "Debug", "IsDoT", "IsDebuff"
		};
		private static readonly HashSet<string> _spellKnownKeys = new HashSet<string>(_spellKeyOutputOrder, StringComparer.OrdinalIgnoreCase);
		private static readonly HashSet<string> _spellKnownFlags = new HashSet<string>(_spellFlagOutputOrder, StringComparer.OrdinalIgnoreCase);
		private static readonly Dictionary<string, string> _spellKeyAliasMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
		{
			{"AfterCast", "AfterSpell"},
			{"BeforeCast", "BeforeSpell"},
			{"DelayAfterCast", "AfterCastCompletedDelay"},
			{"AfterCastCompletedDelay", "AfterCastCompletedDelay"},
			{"MinHpTotal", "MinHPTotal"},
			{"MinHp", "MinHP"}
		};


		public static (string Label, string Flag)[] _spellFlags = new (string Label, string Flag)[]
				{
					("No Interrupt", "NoInterrupt"),
					("Ignore Stack Rules", "IgnoreStackRules"),
					("No Target", "NoTarget"),
					("No Aggro", "NoAggro"),
					("No Burn", "NoBurn"),
					("Rotate", "Rotate"),
					("No Mid Song Cast", "NoMidSongCast"),
					("Gift of Mana (GoM)", "GoM"),
					("Allow Spell Swap", "AllowSpellSwap"),
					("No Early Recast", "NoEarlyRecast"),
					("No Stack", "NoStack"),
					("Debug", "Debug"),
					("Is Debuff", "IsDebuff"),
					("Is DoT", "IsDoT")
				};
		private static readonly string[] _spellCastTypeOptions = new[] { "Spell", "AA", "Disc", "Ability", "Item", "None" };


		// "All Players" key view state/cache
		private static long _cfgAllPlayersNextRefreshAtMs = 0;
		private static Dictionary<string, string> _cfgAllPlayersServerByToon = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		
		private static bool _cfgAllPlayersRefreshing = false;

		// Character .ini selection state
		
		private static long _nextIniFileScanAtMs = 0;
		// Dropdown support (feature-detect combo availability to avoid crashes on older MQ2Mono)
		private static bool _comboAvailable = true;
		
		
		private static bool _cfg_Inited = false;

		#endregion
		static string _versionInfo = String.Empty;
		[SubSystemInit]
		public static void CharacterSettingsWindow_Init()
		{
			if (Core._MQ2MonoVersion < 0.35m) return;
			_versionInfo = $"nE³xt v{Setup.E3Version} by Rekka | Build {Setup.BuildDate}. Editor by Linamas/Rekka";

			// Load UI theme settings from character INI
			try
			{
				if (E3.CharacterSettings != null)
				{
					// Convert string theme name to enum and apply
					if (Enum.TryParse<UITheme>(E3.CharacterSettings.UITheme_E3Config, true, out var theme))
					{
						_currentTheme = theme;
					}
					// Apply rounding setting
					_rounding = E3.CharacterSettings.UITheme_Rounding;
					_roundingBuf = _rounding.ToString("0.0", CultureInfo.InvariantCulture);
				}
			}
			catch (Exception ex)
			{
				E3.Log.Write($"Failed to load UI Theme settings: {ex.Message}", Logging.LogLevels.Error);
			}

			// Toggle the in-game ImGui config window
			EventProcessor.RegisterCommand("/e3imgui", (x) =>
			{

				if (Core._MQ2MonoVersion < 0.35m)
				{
					Core.mqInstance.Write("MQ2Mono Version needs to be at least 0.35 to use this command");
					return;
				}

				try
				{
					//we are already on the main C# thread, so we can just toggle this.
					ToggleImGuiWindow();
				}
				catch (Exception ex)
				{ MQ.Write($"ImGui error: {ex.Message}");}
				
			}, "Toggle E3Next ImGui window");


			E3ImGUI.RegisterWindow(_windowName, () =>
			{
				try
				{ 
					RenderWindow();
				
				} catch (Exception ex)
				{MQ.WriteDelayed("Rendering Error:" + ex.Message + " stack:"+ex.StackTrace);}
			});

		}

		[ClassInvoke(Data.Class.All)]
		public static void Process()
		{
			if (Core._MQ2MonoVersion < 0.35m) return;
			ProcessBackgroundWork();
		}
		public static bool _intialWindowOpened = false;
		public static void ToggleImGuiWindow()
		{
			try
			{
				if(!_intialWindowOpened)
				{
					_intialWindowOpened = true;
					imgui_Begin_OpenFlagSet(_windowName, true);
				}
				else
				{
					bool open = imgui_Begin_OpenFlagGet(_windowName);
					imgui_Begin_OpenFlagSet(_windowName, !open);
				}
				_imguiContextReady = true;
			}
			catch (Exception ex)
			{
				E3.Log.Write($"ImGui error: {ex.Message}", Logging.LogLevels.Error);
				_imguiContextReady = false; // Mark as failed for future calls
			}
		}
		/// <summary>
		///NOTE!!!! During the rendering process, do NOT release control back to C++ as this will leave an incomplete render and ImGUI will not be happy.
		///So for any MQ.Query be sure to use the DelayPossible flag to false.
		/// </summary>
		/// 
		private static void RenderWindow()
		{
			if (!_imguiContextReady) return;
			// Only render if ImGui is available and ready
			if (imgui_Begin_OpenFlagGet(_windowName))
			{
				// Apply current theme
				E3ImGUI.PushCurrentTheme();
				// No size constraints - allow window to be resized to any size
				if (imgui_Begin(_windowName, (int)(ImGuiWindowFlags.ImGuiWindowFlags_NoCollapse | ImGuiWindowFlags.ImGuiWindowFlags_NoDocking)))
				{
					try
					{
						RenderWindowHeader();
						imgui_Separator();
						RenderCharacterIniSelector();
						imgui_Separator();
						RenderSearchBar();
						var allPlayersState = _state.GetState<State_AllPlayers>();
						if (allPlayersState.ShowWindow) RenderAllPlayersView();
						if (!allPlayersState.ShowWindow) RenderConfigEditor();
						if (_state.Show_ThemeSettings) RenderThemeSettingsModal();
						if (_state.Show_Donate) RenderDonateModal();
					}
					finally
					{
						imgui_End();
						PopCurrentTheme();
					}
				}
			}
		}

		private static void RenderWindowHeader()
		{
			// Header bar: version text on left, buttons on right
			if (imgui_BeginTable("E3HeaderBar", 2, (int)ImGuiTableFlags.ImGuiTableFlags_SizingStretchProp, imgui_GetContentRegionAvailX(), 0))
			{
				try
				{
					imgui_TableSetupColumn("Left", 0, 0.70f);
					imgui_TableSetupColumn("Right", 0, 0.30f);
					imgui_TableNextRow();

					//Left: version / build text
					imgui_TableNextColumn();
					imgui_TextColored(0.95f, 0.85f, 0.35f, 1.0f, _versionInfo);

					//Right: buttons aligned to the right within the cell
					imgui_TableNextColumn();
					float cellAvail = imgui_GetContentRegionAvailX();
					float donateBtnW = 70f;
					float themeBtnW = 64f;
					float closeBtnW = 64f;
					float spacing = 6f;
					float totalW = donateBtnW + spacing + themeBtnW + spacing + closeBtnW;
					if (totalW < cellAvail)
					{
						imgui_SameLineEx(cellAvail - totalW, 0f);
					}
					//Donate button(opens confirmation modal)
					if (imgui_Button("Donate"))
					{
						_state.Show_Donate = true;
					}
					imgui_SameLine();
					if (imgui_Button("Theme"))
					{
						_state.Show_ThemeSettings = !_state.Show_ThemeSettings;
					}
					imgui_SameLine();
					if (imgui_Button("Close"))
					{
						imgui_Begin_OpenFlagSet(_windowName, false);
					}
				}
				finally
				{
					imgui_EndTable();
				}
			}
		}
		private static void RenderSearchBar()
		{
			var mainWindowState = _state.GetState<State_MainWindow>();
			var allPlayerState = _state.GetState<State_AllPlayers>();
			// All Players View toggle with better styling
			imgui_Text("Search:");
			imgui_SameLine();
			imgui_SetNextItemWidth(Math.Max(200f, imgui_GetContentRegionAvailX() * 0.2f));
			string searchId = $"configKeySearch";
			string sectionSearchBefore = mainWindowState.Buffer_KeySearch ?? string.Empty;
			if (imgui_InputText(searchId, sectionSearchBefore))
			{
				mainWindowState.Buffer_KeySearch = (imgui_InputText_Get(searchId) ?? string.Empty).Trim();
			}
			imgui_SameLine();
			if (imgui_Button("Clear"))
			{
				imgui_InputTextClear(searchId); //necessary to clear out the C++ buffer for the search
				mainWindowState.Buffer_KeySearch = string.Empty;
			}
			imgui_SameLine();
			imgui_Text("View Mode:");
			imgui_SameLine();
			if (imgui_Button(allPlayerState.ShowWindow ? "Switch to Character View" : "Switch to All Players View"))
			{
				allPlayerState.ShowWindow = !allPlayerState.ShowWindow;
			}
			imgui_SameLine();
			imgui_TextColored(0.3f, 0.8f, 0.3f, 1.0f, allPlayerState.ShowWindow ? "All Players Mode" : "Character Mode");

			imgui_Separator();

		
			if (allPlayerState.ShowWindow)
			{
				string currentSig = $"{mainWindowState.SelectedSection}::{mainWindowState.SelectedKey}";
				if (!string.Equals(currentSig, mainWindowState.SignatureOfSelectedKeyValue, StringComparison.OrdinalIgnoreCase))
				{
					mainWindowState.SignatureOfSelectedKeyValue = currentSig;
					lock (allPlayerState.DataLock)
					{
						allPlayerState.Data_Rows = new List<KeyValuePair<string, string>>();
						allPlayerState.Data_Edit= new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
					}
					_state.Request_AllplayersRefresh = true;
				}
			}
		}
		private static void ResetCatalogs()
		{
			_catalog_Spells.Clear();
			_catalog_AA.Clear();
			_catalog_Disc.Clear();
			_catalog_Skills.Clear();
			_catalog_Items.Clear();
		}
		private static void RequestCatalogUpdate()
		{
			_state.State_CatalogReady = false;
			ResetCatalogs();
			_state.State_CatalogLoadRequested = true;
			_state.Status_CatalogRequest = "Queued catalog load...";
			_cfg_CatalogSource = "Refreshing...";
		}
		private static void ChangeSelectedCharacter(string filename)
		{
			var state = _state.GetState<State_MainWindow>();

			_log.Write($"Selecting other:{filename}", Logging.LogLevels.Debug);
			var parser = e3util.CreateIniParser();
			var pd = parser.ReadFile(filename);
			state.CurrentINIFileNameFull = filename;
			state.CurrentINIData = pd;
			state.SelectedCharacterSection = string.Empty;
			state.SignatureOfSelectedKeyValue = String.Empty;
			
			// Trigger catalog reload for the selected peer
			RequestCatalogUpdate();
		}
		public static void RenderCharacterIniSelector()
		{
			var state = _state.GetState<State_MainWindow>();

			ScanCharIniFilesIfNeeded();

			var loggedInCharIniFile = GetCurrentCharacterIniPath();
			string currentINIFileName = Path.GetFileName(loggedInCharIniFile);
			string selectedINIFile = Path.GetFileName(state.CurrentINIFileNameFull ?? loggedInCharIniFile);
			
			if (string.IsNullOrWhiteSpace(selectedINIFile)) selectedINIFile = currentINIFileName;

			var onlineToons = GetOnlineToonNames();
			imgui_Text("Select Character:");
			imgui_SameLine();
			imgui_SetNextItemWidth(260f);
			if (BeginComboSafe("##Select Character", selectedINIFile))
			{
				try
				{
					if (!string.IsNullOrEmpty(loggedInCharIniFile))
					{
						bool isloggedInCharacterSelected = string.Equals(state.CurrentINIFileNameFull, loggedInCharIniFile, StringComparison.OrdinalIgnoreCase);
						if (imgui_Selectable($"Current: {currentINIFileName}", isloggedInCharacterSelected))
						{
							ChangeSelectedCharacter(loggedInCharIniFile);
						}
					}
					imgui_Text("Other Characters:");
					imgui_Separator();
					foreach (var f in state.IniFilesFromDisk)
					{
						if (string.Equals(f, loggedInCharIniFile, StringComparison.OrdinalIgnoreCase)) continue;
						if (!state.ShowOfflineCharacters && !IsIniForOnlineToon(f, onlineToons)) continue;
						string name = Path.GetFileName(f);
						bool sel = string.Equals(state.CurrentINIFileNameFull, f, StringComparison.OrdinalIgnoreCase);
						if (imgui_Selectable($"{name}", sel))
						{
							ChangeSelectedCharacter(f);
						}
					}
				}
				finally
				{
					imgui_EndCombo();
				}
			}

			imgui_SameLine();
			state.ShowOfflineCharacters = imgui_Checkbox("Show offline", state.ShowOfflineCharacters);
			imgui_SameLine();

			// Save button with better styling
			if (imgui_Button(state.ConfigIsDirty ? "Save Changes*" : "Save Changes"))
			{
				SaveActiveIniData();
			}
			imgui_SameLine();

			// Clear Changes button (only enabled when there are unsaved changes)
			if (state.ConfigIsDirty)
			{
				if (imgui_Button("Clear Changes"))
				{
					ClearPendingChanges();
				}
				imgui_SameLine();
			}
			else
			{
				// Show disabled button when there are no changes
				imgui_PushStyleVarFloat((int)ImGuiStyleVar.Alpha, 0.4f);
				imgui_Button("Clear Changes");
				imgui_PopStyleVar(1);
				imgui_SameLine();
			}

			imgui_TextColored(0.6f, 0.6f, 0.6f, 1.0f, state.ConfigIsDirty ? "Unsaved changes" : "All changes saved");

			imgui_Separator();
		}

		private static void RebuildSectionsOrderIfNeeded()
		{
			var state = _state.GetState<State_MainWindow>();

			// Rebuild sections order when ini path changes
			string activeIniPath = GetActiveSettingsPath() ?? string.Empty;
			if (!string.Equals(activeIniPath, state.LastIniPath, StringComparison.OrdinalIgnoreCase))
			{
				state.LastIniPath = activeIniPath;
				state.SelectedSection = string.Empty;
				state.SelectedKey = string.Empty;
				state.SelectedValueIndex = -1;
				BuildConfigSectionOrder();
				// Auto-load catalogs on ini switch without blocking UI
				RequestCatalogUpdate();
			}
		}
		private static void RenderConfigEditor_CatalogStatus()
		{
			// Catalog status / loader with better styling
			if (!_state.State_CatalogReady)
			{
				imgui_TextColored(1.0f, 0.9f, 0.3f, 1.0f, "Catalog Status");

				if (_state.State_CatalogLoading)
				{
					imgui_Text(string.IsNullOrEmpty(_state.Status_CatalogRequest) ? "Loading catalogs..." : _state.Status_CatalogRequest);
				}
				else
				{
					imgui_Text(string.IsNullOrEmpty(_state.Status_CatalogRequest) ? "Catalogs not loaded" : _state.Status_CatalogRequest);
					imgui_SameLine();
					if (imgui_Button("Load Catalogs"))
					{
						_state.State_CatalogLoadRequested = true;
						_state.Status_CatalogRequest = "Queued catalog load...";
					}
				}
				imgui_Separator();
			}
		}
		private static SectionData GetCurrentSectionData()
		{
			var mainWindowState = _state.GetState<State_MainWindow>();

			if (mainWindowState.CurrentINIData != null)
			{
				var data = mainWindowState.CurrentINIData;
				var sec = data.Sections.GetSectionData(mainWindowState.SelectedSection);
				return sec;
			}
			return null;
		}

		private static void RenderConfigEditor()
		{
			var state = _state.GetState<State_MainWindow>();

			EnsureConfigEditorInit();
			var pd = GetActiveCharacterIniData();
			if (pd == null || pd.Sections == null){	imgui_TextColored(1.0f, 0.8f, 0.8f, 1.0f, "No character INI loaded.");return;}

			RenderConfigEditor_CatalogStatus();
			RebuildSectionsOrderIfNeeded();
			// Use ImGui Table for responsive 3-column layout
			float availY = imgui_GetContentRegionAvailY();
				// Reserve space for spell gems display at bottom (header + separator + gem row with 40px icons + padding)
			float reservedBottomSpace = _state.State_GemsAvailable ? 100f : 10f;
			// Reserve additional space for integrated editor panel if open
			if (state.Show_ShowIntegratedEditor && state.SelectedValueIndex >= 0)
			{
				reservedBottomSpace += 350f; // Space for integrated editor tabs and controls
			}
			float tableHeight = Math.Max(200f, availY - reservedBottomSpace);
			Int32 flags = (int)(ImGuiTableFlags.ImGuiTableFlags_Borders | ImGuiTableFlags.ImGuiTableFlags_Resizable | ImGuiTableFlags.ImGuiTableFlags_ScrollY | ImGuiTableFlags.ImGuiTableFlags_SizingStretchProp | ImGuiTableFlags.ImGuiTableFlags_NoPadInnerX | ImGuiTableFlags.ImGuiTableFlags_NoPadOuterX);

			if (imgui_BeginTable("E3ConfigEditorTable", 3, flags, 0, tableHeight))
			{
				try
				{
					// Set up columns with initial proportions
					imgui_TableSetupColumn("Sections & Keys", (int)ImGuiTableColumnFlags.ImGuiTableColumnFlags_WidthStretch, 0.35f);
					imgui_TableSetupColumn("Values", (int)ImGuiTableColumnFlags.ImGuiTableColumnFlags_WidthStretch, 0.35f);
					imgui_TableSetupColumn("Tools & Info", (int)ImGuiTableColumnFlags.ImGuiTableColumnFlags_WidthStretch, 0.30f);
					imgui_TableHeadersRow();
					imgui_TableNextRow();
					// Column 1: Sections and Keys (with TreeNodes)
					if (imgui_TableNextColumn()) { RenderConfigEditor_SelectionTree(pd);}
					if (imgui_TableNextColumn()) { RenderConfigEditor_Values(pd); }
					if (imgui_TableNextColumn()) { RenderConfigEditor_Tools(pd); }
				}
				finally
				{
					imgui_EndTable();
				}
			}

			// Render integrated editor after table if active
			if (state.Show_ShowIntegratedEditor && state.SelectedValueIndex >= 0) { RenderIntegratedModifierEditor(); }
			//Ensure popups/ modals render even when the tools column is hidden
			SectionData activeSection = GetCurrentSectionData();
			RenderActiveModals(activeSection);
			//Display memorized spells if available from catalog data (safe)
			RenderCatalogGemData();
		}
		
		public static void RenderConfigEditor_SelectionTree(IniData pd)
		{
			var state = _state.GetState<State_MainWindow>();
			//Use a 1 - column table with RowBg to get built-in alternating backgrounds
			int tableFlags = (int)(ImGuiTableFlags.ImGuiTableFlags_RowBg | ImGuiTableFlags.ImGuiTableFlags_ScrollY);
			if (imgui_BeginTable("SectionsTreeTable", 1, tableFlags, 0, 0))
			{
				try
				{
					imgui_TableSetupColumn("Section", 0, 0.35f);
					var sectionsToRender = GetSectionsForDisplay();
					imgui_TableNextRow();
					imgui_TableNextColumn();

					if (sectionsToRender.Count == 0)
					{
						imgui_TextColored(0.95f, 0.75f, 0.75f, 1.0f, "No sections match the current search.");
					}
					else
					{
						foreach (var sec in sectionsToRender)
						{
							var secData = pd.Sections.GetSectionData(sec);
							if (secData?.Keys == null)
								continue;

							if (!_cfgSectionExpanded.ContainsKey(sec))
							{
								_cfgSectionExpanded[sec] = false;
							}

							int treeFlagsSection = (int)ImGuiTreeNodeFlags.ImGuiTreeNodeFlags_SpanAvailWidth;
							if (_cfgSectionExpanded[sec])
							{
								treeFlagsSection |= (int)ImGuiTreeNodeFlags.ImGuiTreeNodeFlags_DefaultOpen;
							}

							string sectionLabel = $"{sec}##section_{sec}";
							bool nodeOpen = imgui_TreeNodeEx(sectionLabel, treeFlagsSection);
							bool itemHovered = imgui_IsItemHovered();

							if (itemHovered && imgui_IsMouseClicked(0))
							{
								state.SelectedSection = sec;
								_state.ClearAddInLine();
							}

							if (sec.Equals("Ifs", StringComparison.OrdinalIgnoreCase) || sec.Equals("Burn", StringComparison.OrdinalIgnoreCase))
							{
								if (itemHovered && imgui_IsMouseClicked(1))
								{
									state.Show_ContextMenu = true;
									state.ContextMenuFor = sec;
									state.SelectedSection = sec;
								}
							}

							if (sec.Equals("Ifs", StringComparison.OrdinalIgnoreCase))
							{
								if (imgui_BeginPopupContextItem(null, 1))
								{
									imgui_PushStyleColor((int)ImGuiCol.Text, 0.95f, 0.85f, 0.35f, 1.0f);
									bool addIfs = imgui_MenuItem("Add New Ifs");
									imgui_PopStyleColor(1);

									if (addIfs)
									{
										_state.ClearAddInLine();
										state.Show_AddInLine = true;
										state.SelectedAddInLine = "Ifs";
										state.SelectedSection = "Ifs";
										state.SelectedValueIndex = -1;
										
									}

									imgui_EndPopup();
								}
							}
							else if (sec.Equals("Burn", StringComparison.OrdinalIgnoreCase))
							{
								if (imgui_BeginPopupContextItem(null, 1))
								{
									imgui_PushStyleColor((int)ImGuiCol.Text, 0.95f, 0.85f, 0.35f, 1.0f);
									bool addBurn = imgui_MenuItem("Add New Burn");
									imgui_PopStyleColor(1);

									if (addBurn)
									{
										_state.ClearAddInLine();
										state.Show_AddInLine = true;
										state.SelectedAddInLine = "Burn";
										state.SelectedSection = "Burn";
										state.SelectedValueIndex = -1;
									
									}

									imgui_EndPopup();
								}
							}

							_cfgSectionExpanded[sec] = nodeOpen;
							if (nodeOpen)
							{
								var keys = secData.Keys.Select(k => k.KeyName).ToArray();
								foreach (var key in keys)
								{
									//imgui_TableNextRow();
									//imgui_TableNextColumn();

									bool keySelected = string.Equals(state.SelectedSection, sec, StringComparison.OrdinalIgnoreCase) &&
										string.Equals(state.SelectedKey, key, StringComparison.OrdinalIgnoreCase);

									string keyLabel = $"  {key}"; // simple indent under section
									if (imgui_Selectable(keyLabel, keySelected))
									{
										_state.ClearAddInLine();
										state.SelectedSection = sec;
										state.SelectedKey = key;
										state.SelectedValueIndex = -1;
									}

									// Context menu for all keys (right-click)
									if (imgui_BeginPopupContextItem(null, 1))
									{
										if (imgui_MenuItem("Delete Key"))
										{
											DeleteKeyFromActiveIni(sec, key);
										}

										imgui_EndPopup();
									}
								}

								imgui_TreePop();
							}
						}
					}
				}
				finally
				{
					imgui_EndTable();
				}
			}
		}
		private static void RenderConfigEditor_Values(IniData pd)
		{
			var state = _state.GetState<State_MainWindow>();

			int tableFlags = (int)(ImGuiTableFlags.ImGuiTableFlags_RowBg | ImGuiTableFlags.ImGuiTableFlags_ScrollY);
			if (imgui_BeginTable("ValuesTable", 1, tableFlags, 0, 0))
			{
				try
				{
					imgui_TableSetupColumn("Values", 0, 0.35f);
					imgui_TableNextRow();
					imgui_TableNextColumn();
					var selectedSection = pd.Sections.GetSectionData(state.SelectedSection ?? string.Empty);
					//_log.Write($"Rendering with selected section {selectedSection.SectionName} with keys count:{selectedSection.Keys.Count} with pd:");
					if (selectedSection == null)
					{
						imgui_Text("No section selected.");
						return;
					}
					else if (selectedSection.Keys == null || selectedSection.Keys.Count() == 0)
					{
						// Empty section: allow creating a new key directly here
						imgui_TextColored(0.8f, 0.9f, 0.95f, 1.0f, $"[{state.SelectedSection}] (empty)");
						imgui_Separator();
						imgui_Text("Create new entry:");
						imgui_SameLine();
						imgui_SetNextItemWidth(220f);
						if (imgui_InputText("##new_key_name", state.Buffer_NewKey))
						{
							state.Buffer_NewKey = imgui_InputText_Get("##new_key_name") ?? string.Empty;
						}
						imgui_SameLine();
						if (imgui_Button("Add Key"))
						{
							string newKey = (state.Buffer_NewKey ?? string.Empty).Trim();
							if (newKey.Length > 0 && !selectedSection.Keys.ContainsKey(newKey))
							{
								selectedSection.Keys.AddKey(newKey, string.Empty);
								state.SelectedKey = newKey;
								state.Buffer_NewKey = string.Empty;
								state.InLineEditIndex = -1;
								// On next frame the normal values editor will show for the new key
							}
						}
					}
					// Inline Add New editor (triggered from header context menu)
					if (state.Show_AddInLine
						&& string.Equals(state.SelectedAddInLine, state.SelectedSection, StringComparison.OrdinalIgnoreCase)
						&& string.IsNullOrEmpty(state.SelectedKey))
					{
						imgui_TextColored(0.8f, 0.9f, 0.95f, 1.0f, state.SelectedAddInLine.Equals("Ifs", StringComparison.OrdinalIgnoreCase) ? "Add New If" : "Add New Burn Key");
						imgui_Text("Name:");
						imgui_SameLine();
						float inlineFieldAvail = imgui_GetContentRegionAvailX();
						float inlineFieldWidth = Math.Max(320f, inlineFieldAvail * 0.45f);
						inlineFieldWidth = Math.Min(inlineFieldWidth, Math.Max(260f, inlineFieldAvail - 60f));
						imgui_SetNextItemWidth(inlineFieldWidth);
						if (imgui_InputText("##inline_new_key", state.Buffer_NewKey))
						{
							state.Buffer_NewKey = imgui_InputText_Get("##inline_new_key") ?? string.Empty;
						}
						imgui_Text("Value:");
						float inlineValueAvail = imgui_GetContentRegionAvailX();
						float inlineValueWidth = Math.Max(420f, inlineValueAvail * 0.70f);
						inlineValueWidth = Math.Min(inlineValueWidth, Math.Max(320f, inlineValueAvail - 80f));
						float inlineValueHeight = Math.Max(140f, imgui_GetTextLineHeightWithSpacing() * 6f);
						if (imgui_InputTextMultiline("##inline_new_value", state.Buffer_NewValue ?? string.Empty, inlineValueWidth, inlineValueHeight))
						{
							state.Buffer_NewValue = imgui_InputText_Get("##inline_new_value") ?? string.Empty;
						}
						if (imgui_Button("Add##inline_add"))
						{
							string key = (state.Buffer_NewKey ?? string.Empty).Trim();
							string val = state.Buffer_NewValue ?? string.Empty;
							bool added = false;
							if (state.SelectedAddInLine.Equals("Ifs", StringComparison.OrdinalIgnoreCase))
							{
								added = AddIfToActiveIni(key, val);
							}
							else if (state.SelectedAddInLine.Equals("Burn", StringComparison.OrdinalIgnoreCase))
							{
								added = AddBurnToActiveIni(key, val);
							}
							if (added)
							{
								_state.ClearAddInLine();
								// Open blank value editor if value was empty
								state.InLineEditIndex = 0;
							}
						}
						imgui_SameLine();
						if (imgui_Button("Cancel##inline_cancel"))
						{
							_state.ClearAddInLine();
						}
						imgui_Separator();
					}
					else if (string.IsNullOrEmpty(state.SelectedKey))
					{
						// Section has keys, but no key selected yet: keep values panel empty
						imgui_Text("Select a configuration key from the left panel.");
					}
					else
					{
						RenderSelectedKeyValues(selectedSection);
					}
				}
				finally
				{
					imgui_EndTable();
				}

			}

		}
		public static void RenderConfigEditor_Tools(IniData pd)
		{
			var state = _state.GetState<State_MainWindow>();

			var activeSection = pd.Sections.GetSectionData(state.SelectedSection ?? string.Empty);

			int tableFlags = (int)(ImGuiTableFlags.ImGuiTableFlags_RowBg | ImGuiTableFlags.ImGuiTableFlags_ScrollY);
			if (imgui_BeginTable("ToolsInfoTable", 1, tableFlags, 0, 0))
			{
				try
				{
					imgui_TableSetupColumn("Tools & Info", 0, 0.35f);
					imgui_TableNextRow();
					imgui_TableNextColumn();

					RenderConfigurationTools(activeSection);
				}
				finally
				{
					imgui_EndTable();
				}
			}
		}

		// Integrated editor panel - renders after the main table and spans full width
		private static void RenderIntegratedModifierEditor()
		{
			var mainWindowState = _state.GetState<State_MainWindow>();

			var iniData = GetActiveCharacterIniData();
			var sectionData = iniData?.Sections?.GetSectionData(mainWindowState.SelectedSection ?? string.Empty);
			var keyData = sectionData?.Keys?.GetKeyData(mainWindowState.SelectedKey ?? string.Empty);
			var values = GetValues(keyData);
			if (mainWindowState.SelectedValueIndex < 0 || mainWindowState.SelectedValueIndex >= values.Count)
			{
				mainWindowState.Show_ShowIntegratedEditor = false;
				return;
			}

			string rawValue = values[mainWindowState.SelectedValueIndex] ?? string.Empty;
			var state = EnsureSpellEditState(mainWindowState.SelectedSection, mainWindowState.SelectedKey, mainWindowState.SelectedValueIndex, rawValue);
			if (state == null)
			{
				mainWindowState.Show_ShowIntegratedEditor = false;
				return;
			}

			imgui_Separator();
			imgui_TextColored(0.95f, 0.85f, 0.35f, 1.0f, "Spell Modifier Editor");
			imgui_Separator();

			RenderSpellModifierEditor(state);
		}

		// Safe gem display using catalog data (no TLO queries from UI thread)
		private static void RenderCatalogGemData()
		{
			lock (_dataLock)
			{
				if (!_state.State_GemsAvailable || _cfg_CatalogGems == null) return;

			}

			try
			{
				imgui_Separator();

				// Show header with source info
				string sourceText = _cfg_CatalogSource.StartsWith("Remote") ? "Memorized Spells" : "Currently Memorized Spells";
				imgui_TextColored(0.8f, 0.9f, 1.0f, 1.0f, sourceText);

				if (_cfg_CatalogSource.StartsWith("Remote"))
				{
					imgui_SameLine();
					imgui_TextColored(0.7f, 1.0f, 0.7f, 1.0f, $"({_cfg_CatalogSource.Replace("Remote (", "").Replace(")", "")})")
;
				}

				// Use horizontal table for gem display
				if (imgui_BeginTable("E3CatalogGems", 12, (int)(ImGuiTableFlags.ImGuiTableFlags_Borders | ImGuiTableFlags.ImGuiTableFlags_SizingStretchSame), imgui_GetContentRegionAvailX(), 0))
				{
					try
					{
						// Column headers
						for (int gem = 1; gem <= 12; gem++)
						{
							imgui_TableSetupColumn($"Gem {gem}", (int)ImGuiTableColumnFlags.ImGuiTableColumnFlags_WidthStretch, 1.0f);
						}
						imgui_TableHeadersRow();

						imgui_TableNextRow();

						// Display gem data from catalog
						for (int gem = 0; gem < 12; gem++)
						{
							imgui_TableNextColumn();


							Int32 spellID = -1;
							Int32.TryParse(_cfg_CatalogGems[gem], out spellID);

							string spellName = MQ.Query<string>($"${{Spell[{spellID}]}}", false);

							if (!string.IsNullOrEmpty(spellName) && !spellName.Equals("NULL", StringComparison.OrdinalIgnoreCase) && !spellName.Equals("ERROR", StringComparison.OrdinalIgnoreCase))
							{
								// Get spell icon index for this gem
								int iconIndex = (_cfg_CatalogGemIcons != null && gem < _cfg_CatalogGemIcons.Length) ? _cfg_CatalogGemIcons[gem] : -1;

								// Display spell icon using native EQ texture
								if (iconIndex >= 0)
								{
									imgui_DrawSpellIconByIconIndex(iconIndex, 40.0f);
								}

								// Try to find spell info for color coding
								if (_state.State_CatalogReady)
								{
									var spellInfo = FindSpellItemAAByName(spellName);
									if (spellInfo != null && spellInfo.Level > 0)
									{
										// Color code by spell level
										float r = 0.9f, g = 0.9f, b = 0.9f;
										if (spellInfo.Level <= 10) { r = 0.7f; g = 1.0f; b = 0.7f; }
										else if (spellInfo.Level <= 50) { r = 0.9f; g = 0.9f; b = 0.7f; }
										else if (spellInfo.Level <= 85) { r = 1.0f; g = 0.8f; b = 0.6f; }
										else { r = 1.0f; g = 0.7f; b = 0.7f; }

										// Only show details in tooltip (no inline name)
										if (imgui_IsItemHovered())
										{
											imgui_SetNextWindowSize(200, 200);
											imgui_BeginTooltip();
											imgui_Text($"Spell: {spellName}");
											imgui_Text($"Level: {spellInfo.Level}");
											if (iconIndex >= 0)
												imgui_Text($"Icon: {iconIndex}");
											if (!string.IsNullOrEmpty(spellInfo.Description))
											{
												imgui_Separator();
												imgui_TextWrapped(spellInfo.Description);
											}
											imgui_EndTooltip();
										}

									}
									else
									{
										// Only show details in tooltip (no inline name)

										// Add basic hover tooltip
										if (imgui_IsItemHovered())
										{
											imgui_BeginTooltip();
											imgui_Text($"Spell: {spellName}");
											if (iconIndex >= 0)
												imgui_Text($"Icon: {iconIndex}");
											imgui_EndTooltip();
										}
									}
								}
								else
								{
									imgui_TextColored(0.9f, 0.9f, 0.9f, 1.0f, spellName);

									// Add basic hover tooltip
									if (imgui_IsItemHovered())
									{
										imgui_BeginTooltip();
										imgui_Text($"Spell: {spellName}");
										if (iconIndex >= 0)
											imgui_Text($"Icon: {iconIndex}");
										imgui_EndTooltip();
									}
								}
							}
							else if (spellName == "ERROR")
							{
								imgui_TextColored(0.8f, 0.4f, 0.4f, 1.0f, "(error)");
							}
							else
							{
								imgui_TextColored(0.5f, 0.5f, 0.5f, 1.0f, "(empty)");
							}
						}
					}
					finally
					{
						imgui_EndTable();
					}
				}
			}
			catch (Exception ex)
			{
				imgui_TextColored(0.8f, 0.4f, 0.4f, 1.0f, $"Error displaying gems: {ex.Message}");
			}
		}
		private static Int32 GetIconFromIniString(string value)
		{
			Int32 indexOfSlash = value.IndexOf('/');

			string spellName = value;
			if (indexOfSlash != -1)
			{
				spellName = value.Substring(0, indexOfSlash);
			}
			Int32 iconID = 0;
			if (_spellCatalogLookup.TryGetValue(spellName, out var tspell))
			{
				iconID = tspell.SpellIcon;
			}
			else if (_aaCatalogLookup.TryGetValue(spellName, out var taa))
			{
				iconID = taa.SpellIcon;
			}
			else if (_itemCatalogLookup.TryGetValue(spellName, out var titem))
			{
				iconID = titem.SpellIcon;

			}
			else if (_discCatalogLookup.TryGetValue(spellName, out var tdisc))
			{
				iconID = tdisc.SpellIcon;
			}

			return iconID;

		}


	

		// Helper method to render values for the selected key
		private static void RenderSelectedKeyValues(SectionData selectedSection)
		{
			var mainWindowState = _state.GetState<State_MainWindow>();

			if (selectedSection == null || selectedSection.Keys==null)
			{
				return;

			}
			var kd = selectedSection.Keys.GetKeyData(mainWindowState.SelectedKey ?? string.Empty);

			if(kd == null)
			{
				return;
			}

			var parts = GetValues(kd);
			// Title row with better styling
			imgui_TextColored(0.95f, 0.85f, 0.35f, 1.0f, $"[{mainWindowState.SelectedSection??""}] {mainWindowState.SelectedKey??""}");
			imgui_Separator();
			if (parts==null || parts.Count == 0)
			{
				imgui_Text("(No values)");
				imgui_Separator();
				if (parts == null) return;
			}
			// Enumerated options derived from key label e.g. "(Melee/Ranged/Off)"
			if (TryGetKeyOptions(mainWindowState.SelectedKey, out var enumOpts))
			{
				RenderSelectedKeyValues_Registered(parts, selectedSection, enumOpts);
			}
			// Boolean fast toggle support → dropdown selector with better styling
			else if (IsBooleanConfigKey(mainWindowState.SelectedKey))
			{
				RenderSelectedKeyValues_Boolean(parts,selectedSection);
			}
			else if(IsIntergerConfigKey(mainWindowState.SelectedKey))
			{
				RenderSelectedKeyValues_Integers(parts, selectedSection);
			}
			else if (IsStringConfigKey(mainWindowState.SelectedKey))
			{
				RenderSelectedKeyValues_String(parts, selectedSection);
			}
			else if(!(String.IsNullOrWhiteSpace(mainWindowState.SelectedKey) || String.IsNullOrWhiteSpace(mainWindowState.SelectedSection)))
			{
				RenderSelectedKeyValues_Collections(parts,selectedSection);
			}
			
			if(CharacterSettings.ConfigKeyDescriptionsForImGUI.TryGetValue($"{mainWindowState.SelectedSection}::{mainWindowState.SelectedKey}", out var description))
			{
				if (description.Count>0)
				{
					//was a test to see how doing this way vs try finally
					using (var p = PushStyle.Aquire())
					{
						p.PushStyleColor((int)E3ImGUI.ImGuiCol.Text, 0.95f, 0.85f, 0.35f, 1.0f);
						RenderDescriptionRichText(description);
						imgui_Separator();
					}
					//vs
					//imgui_PushStyleColor((int)E3ImGUI.ImGuiCol.Text, 0.95f, 0.85f, 0.35f, 1.0f);
					//try { imgui_TextWrapped(description); imgui_Separator(); }
					//finally { imgui_PopStyleColor(1); }
				}

			}

			
		}
		private static void RenderSelectedKeyValues_String(List<string> parts, SectionData selectedSection)
		{
			var mainWindowState = _state.GetState<State_MainWindow>();

			if (imgui_InputText($"##edit_string_{mainWindowState.SelectedKey}", parts[0]))
			{
				//parts is a direct pointer to ValuesList from the IniData, so can update it directly. 
				parts[0] = imgui_InputText_Get($"##edit_string_{mainWindowState.SelectedKey}");
				mainWindowState.ConfigIsDirty = true;
			}
			imgui_Separator();
		}
		private static void RenderSelectedKeyValues_Integers(List<string> parts, SectionData selectedSection)
		{
			var mainWindowState = _state.GetState<State_MainWindow>();

			// Edit mode with better styling
			Int32 intValue = 0;
			Int32.TryParse(parts[0], out intValue);

			if (imgui_InputInt($"##edit_int_{mainWindowState.SelectedKey}", intValue, 5, 20))
			{
				//parts is a direct pointer to ValuesList from the IniData, so can update it directly. 
				parts[0] = imgui_InputInt_Get($"##edit_int_{mainWindowState.SelectedKey}").ToString();
				mainWindowState.ConfigIsDirty = true;
			}
			imgui_Separator();
		}


		private static void RenderSelectedKeyValues_Registered(List<string> parts, SectionData selectedSection, List<string> enumOpts)
		{
			var mainWindowState = _state.GetState<State_MainWindow>();

			string current = parts[0];
			string display = current.Length == 0 ? "(unset)" : current;
			if (BeginComboSafe("Value", display))
			{
				foreach (var opt in enumOpts)
				{
					bool sel = string.Equals(current, opt, StringComparison.OrdinalIgnoreCase);
					if (imgui_Selectable(opt, sel))
					{
						string chosen = opt;
						var pdAct = GetActiveCharacterIniData();
						var selSec = pdAct.Sections.GetSectionData(mainWindowState.SelectedSection);
						if (selSec != null && selSec.Keys.ContainsKey(mainWindowState.SelectedKey))
						{
							var kdata = selSec.Keys.GetKeyData(mainWindowState.SelectedKey);
							if (kdata != null)
							{
								WriteValues(kdata, new List<string> { chosen });
							}
						}
					}
				}
				EndComboSafe();
			}
			imgui_Separator();
		}
		private static void RenderSelectedKeyValues_Boolean(List<string> parts, SectionData selectedSection)
		{
			var mainWindowState = _state.GetState<State_MainWindow>();

			string current = parts[0];
			// Derive allowed options from base E3 conventions
			List<string> baseOpts;
			var keyLabel = mainWindowState.SelectedKey ?? string.Empty;
			bool mentionsOnOff = keyLabel.IndexOf("(On/Off)", StringComparison.OrdinalIgnoreCase) >= 0
								 || keyLabel.IndexOf("On/Off", StringComparison.OrdinalIgnoreCase) >= 0
								 || keyLabel.IndexOf("Enable", StringComparison.OrdinalIgnoreCase) >= 0
								 || keyLabel.StartsWith("Use ", StringComparison.OrdinalIgnoreCase);
			if (string.Equals(current, "True", StringComparison.OrdinalIgnoreCase) || string.Equals(current, "False", StringComparison.OrdinalIgnoreCase))
			{
				// Preserve True/False style if that's what's used
				baseOpts = new List<string> { "True", "False" };
			}
			else if (mentionsOnOff || string.Equals(current, "On", StringComparison.OrdinalIgnoreCase) || string.Equals(current, "Off", StringComparison.OrdinalIgnoreCase))
			{
				baseOpts = new List<string> { "On", "Off" };
			}
			else
			{
				// Default to On/Off per E3 defaults
				baseOpts = new List<string> { "On", "Off" };
			}

			string display = current.Length == 0 ? "(unset)" : current;
			if (BeginComboSafe("Value", display))
			{
				foreach (var opt in baseOpts)
				{
					bool sel = string.Equals(current, opt, StringComparison.OrdinalIgnoreCase);
					if (imgui_Selectable(opt, sel))
					{
						string chosen = opt;
						var pdAct = GetActiveCharacterIniData();
						var selSec = pdAct.Sections.GetSectionData(mainWindowState.SelectedSection);
						if (selSec != null && selSec.Keys.ContainsKey(mainWindowState.SelectedKey))
						{
							var kdata = selSec.Keys.GetKeyData(mainWindowState.SelectedKey);
							if (kdata != null)
							{
								WriteValues(kdata, new List<string> { chosen });
							}
						}
					}
				}
				EndComboSafe();
			}
			imgui_Separator();
		}
		private static void RenderSelectedKeyValues_Collections(List<string> parts, SectionData selectedSection)
		{
			var catalogState = _state.GetState<State_CatalogWindow>();
			var mainWindowState = _state.GetState<State_MainWindow>();
			bool listChanged = false;
			imgui_TextColored(0.9f, 0.95f, 1.0f, 1.0f, "Configuration Values");

			for (int i = 0; i < parts.Count; i++)
			{
				string v = parts[i];
				bool editing = (mainWindowState.InLineEditIndex == i);
				// Create a unique ID for this item that doesn't depend on its position in the list
				string itemUid = $"{mainWindowState.SelectedSection}_{mainWindowState.SelectedKey}_{i}_{(v ?? string.Empty).GetHashCode()}";

				if (!editing)
				{
					// Row with better styling and alignment
					Int32 iconID = GetIconFromIniString(v);
					imgui_DrawSpellIconByIconIndex(iconID, 30.0f);
					imgui_SameLine();
					imgui_Text($"{i + 1}.");
					imgui_SameLine(_valueRowActionStartOffset + 20);

					bool canMoveUp = i > 0;
					bool canMoveDown = i < parts.Count - 1;

					void SwapAndMark(int fromIndex, int toIndex)
					{
						var pdAct = GetActiveCharacterIniData();
						var selSec = pdAct.Sections.GetSectionData(mainWindowState.SelectedSection);
						var key = selSec?.Keys.GetKeyData(mainWindowState.SelectedKey);
						if (key == null) return;

						var vals = GetValues(key);
						if (fromIndex < 0 || fromIndex >= vals.Count) return;
						if (toIndex < 0 || toIndex >= vals.Count) return;

						string temp = vals[toIndex];
						vals[toIndex] = vals[fromIndex];
						vals[fromIndex] = temp;
						WriteValues(key, vals);
						listChanged = true;
						_cfgPendingValueSelection = toIndex;
					}

					void StartInlineEdit(int index, string currentValue)
					{
						mainWindowState.InLineEditIndex = index;
						_cfgInlineEditBuffer = currentValue ?? string.Empty;
					}

					void DeleteValueAt(int index)
					{
						var pdAct = GetActiveCharacterIniData();
						var selSec = pdAct.Sections.GetSectionData(mainWindowState.SelectedSection);
						var key = selSec?.Keys.GetKeyData(mainWindowState.SelectedKey);
						if (key != null)
						{
							var vals = GetValues(key);
							if (index >= 0 && index < vals.Count)
							{
								vals.RemoveAt(index);
								WriteValues(key, vals);
								listChanged = true;
							}
						}
					}

					void RenderReorderButton(string label, bool enabled, Action onClick)
					{
						if (!enabled)
						{
							imgui_PushStyleVarFloat((int)ImGuiStyleVar.Alpha, 0.4f);
						}
						bool pressed = imgui_Button(label);
						if (!enabled)
						{
							imgui_PopStyleVar(1);
						}
						if (pressed && enabled)
						{
							onClick?.Invoke();
						}
					}

					RenderReorderButton($"^##moveup_{itemUid}", canMoveUp, () => SwapAndMark(i, i - 1));
					imgui_SameLine();
					RenderReorderButton($"v##movedown_{itemUid}", canMoveDown, () => SwapAndMark(i, i + 1));
					imgui_SameLine();

					// Make value selectable to show info in right panel
					bool isSelected = (mainWindowState.SelectedValueIndex == i);
					if (imgui_Selectable($"{v}##select_{itemUid}", isSelected))
					{
						mainWindowState.SelectedValueIndex = i;
						_cfgInlineEditBuffer = v;
					}
					if (imgui_BeginPopupContextItem($"ValueCtx_{itemUid}", 1))
					{
						if (canMoveUp && imgui_MenuItem("Move Up")) SwapAndMark(i, i - 1);
						if (canMoveDown && imgui_MenuItem("Move Down")) SwapAndMark(i, i + 1);
						if (imgui_MenuItem("Replace From Catalog"))
						{

							catalogState.ReplaceMode = true;
							catalogState.ReplaceIndex = i;
							_state.Show_AddModal = true;
						}
						imgui_EndPopup();
					}
				}
				else
				{
					// Edit mode with better styling
					imgui_Text($"* {i + 1}.");
					imgui_SameLine(_valueRowActionStartOffset);

					float editAvail = imgui_GetContentRegionAvailX();
					float editWidth = Math.Max(420f, editAvail - 140f);
					editWidth = Math.Min(editWidth, Math.Max(260f, editAvail - 80f));
					float editHeight = Math.Max(140f, imgui_GetTextLineHeightWithSpacing() * 6f);
					if (imgui_InputTextMultiline($"##edit_text_{itemUid}", _cfgInlineEditBuffer ?? string.Empty, editWidth, editHeight))
					{
						_cfgInlineEditBuffer = imgui_InputText_Get($"##edit_text_{itemUid}");
					}

					if (imgui_Button($"Save##save_{itemUid}"))
					{
						string newText = _cfgInlineEditBuffer ?? string.Empty;
						int idx = i;
						var pdAct = GetActiveCharacterIniData();
						var selSec = pdAct.Sections.GetSectionData(mainWindowState.SelectedSection);
						var key = selSec?.Keys.GetKeyData(mainWindowState.SelectedKey);
						if (key != null)
						{
							var vals = GetValues(key);
							if (idx >= 0 && idx < vals.Count)
							{
								vals[idx] = newText;
								WriteValues(key, vals);
								listChanged = true;
							}
						}
						mainWindowState.InLineEditIndex = -1;
						_cfgInlineEditBuffer = string.Empty;
						// continue to render items; parts refresh handled below
					}
					imgui_SameLine();

					if (imgui_Button($"Cancel##cancel_{itemUid}"))
					{
						mainWindowState.InLineEditIndex = -1;
						_cfgInlineEditBuffer = string.Empty;
					}
				}

				// If a change was made, we need to refresh the parts list for subsequent iterations
				if (listChanged)
				{
					// Re-get the values after modification
					var updatedKd = selectedSection.Keys.GetKeyData(mainWindowState.SelectedKey ?? string.Empty);
					parts = GetValues(updatedKd);
					listChanged = false; // Reset the flag
					if (_cfgPendingValueSelection >= 0 && _cfgPendingValueSelection < parts.Count)
					{
						mainWindowState.SelectedValueIndex = _cfgPendingValueSelection;
					}
					else
					{
						mainWindowState.SelectedValueIndex = -1;
					}
					_cfgPendingValueSelection = -1;
					// Adjust the loop counter since we've removed an item
					i--;
				}
			}

			// Handle adding a new manual entry (if we're in add mode)
			if (mainWindowState.InLineEditIndex >= parts.Count)
			{
				imgui_Separator();
				imgui_TextColored(0.8f, 0.9f, 0.95f, 1.0f, "Add New Value");

				imgui_Text($"+ {parts.Count + 1}.");
				imgui_SameLine(_valueRowActionStartOffset);

				float addAvail = imgui_GetContentRegionAvailX();
				float addManualWidth = Math.Max(420f, addAvail - 140f);
				addManualWidth = Math.Min(addManualWidth, Math.Max(260f, addAvail - 80f));
				float addManualHeight = Math.Max(140f, imgui_GetTextLineHeightWithSpacing() * 6f);
				if (imgui_InputTextMultiline($"##add_new_manual", _cfgInlineEditBuffer ?? string.Empty, addManualWidth, addManualHeight))
				{
					_cfgInlineEditBuffer = imgui_InputText_Get($"##add_new_manual");
				}

				if (imgui_Button($"Add##add_manual"))
				{
					string newText = _cfgInlineEditBuffer ?? string.Empty;
					if (!string.IsNullOrWhiteSpace(newText))
					{
						var pdAct = GetActiveCharacterIniData();
						var selSec = pdAct.Sections.GetSectionData(mainWindowState.SelectedSection);
						var key = selSec?.Keys.GetKeyData(mainWindowState.SelectedKey);
						if (key != null)
						{
							var vals = GetValues(key);
							vals.Add(newText.Trim());
							WriteValues(key, vals);
							_cfgPendingValueSelection = vals.Count - 1;
						}
					}
					mainWindowState.InLineEditIndex = -1;
					_cfgInlineEditBuffer = string.Empty;
				}
				imgui_SameLine();

				if (imgui_Button($"Cancel##cancel_manual"))
				{
					mainWindowState.InLineEditIndex = -1;
					_cfgInlineEditBuffer = string.Empty;
				}
			}
			// Add new value button (only show when not editing)
			else if (!listChanged && mainWindowState.InLineEditIndex == -1)
			{
				imgui_Separator();
				imgui_TextColored(0.8f, 0.9f, 0.95f, 1.0f, "Add New Values");

				// Check if this is a Food or Drink key
				bool isFoodOrDrink = mainWindowState.SelectedKey.Equals("Food", StringComparison.OrdinalIgnoreCase) || mainWindowState.SelectedKey.Equals("Drink", StringComparison.OrdinalIgnoreCase);

				if (imgui_Button("Add Manual"))
				{
					mainWindowState.InLineEditIndex = parts.Count;
					_cfgInlineEditBuffer = string.Empty;
				}
				imgui_SameLine();

				// For Food/Drink keys, show "Pick From Inventory" instead of "Add From Catalog"
				if (isFoodOrDrink)
				{
					if (imgui_Button("Pick From Inventory"))
					{
						// Reset scan state so results don't carry over between Food/Drink
						_cfgFoodDrinkKey = mainWindowState.SelectedKey; // "Food" or "Drink"
						_cfgFoodDrinkStatus = string.Empty;
						_cfgFoodDrinkCandidates.Clear();
						_cfgFoodDrinkScanRequested = true; // auto-trigger scan for new kind
						_state.Show_FoodDrinkModal = true;
					}
				}
				else
				{
					if (imgui_Button("Add From Catalog"))
					{
						catalogState.Mode = CatalogMode.Standard;
						_state.Show_AddModal = true;
					}
				}
			}
		}

		// Helper method to render configuration tools panel
		private static void RenderConfigurationTools(SectionData selectedSection)
		{
			var state = _state.GetState<State_MainWindow>();
			var bardEditorState = _state.GetState<State_BardEditor>();

			bool isBardIni = IsActiveIniBard();
			if (isBardIni)
			{
				imgui_TextColored(0.95f, 0.85f, 0.35f, 1.0f, "Bard Tools");
				if (imgui_Button("Open Melody Helper"))
				{
					ResetBardMelodyHelperForm();
					_state.Show_BardMelodyHelper = true;
				}
				if (!string.IsNullOrEmpty(bardEditorState.MelodyStatus))
				{
					imgui_TextColored(0.7f, 0.9f, 0.7f, 1.0f, bardEditorState.MelodyStatus);
				}
				imgui_Separator();
			}
			if (selectedSection == null)
			{
				imgui_TextColored(0.9f, 0.9f, 0.9f, 1.0f, "Select a configuration key to see available tools.");
				return;
			}
			bool hasKeySelected = !string.IsNullOrEmpty(state.SelectedKey);
			bool specialSectionAllowsNoKey = string.Equals(state.SelectedSection, "Ifs", StringComparison.OrdinalIgnoreCase) || string.Equals(state.SelectedSection, "Burn", StringComparison.OrdinalIgnoreCase);
			if (!hasKeySelected && !specialSectionAllowsNoKey)
			{
				imgui_TextColored(0.9f, 0.9f, 0.9f, 1.0f, "Select a configuration key to see available tools.");
				return;
			}

			imgui_TextColored(0.95f, 0.85f, 0.35f, 1.0f, "Configuration Tools");
			imgui_Separator();

			// Value actions at the top (when a value is selected)
			if (state.SelectedValueIndex >= 0 && hasKeySelected)
			{
				var kd = selectedSection?.Keys?.GetKeyData(state.SelectedKey ?? string.Empty);
				var values = GetValues(kd);
				if (state.SelectedValueIndex < values.Count)
				{
					string selectedValue = values[state.SelectedValueIndex];
					var editState = EnsureSpellEditState(state.SelectedSection, state.SelectedKey, state.SelectedValueIndex, selectedValue);
					if (editState != null)
					{
						imgui_TextColored(0.95f, 0.85f, 0.35f, 1.0f, "Value Actions");

						// Delete Value button (red)
						imgui_PushStyleColor((int)ImGuiCol.Button, 0.85f, 0.30f, 0.30f, 1.0f);
						imgui_PushStyleColor((int)ImGuiCol.ButtonHovered, 0.95f, 0.40f, 0.40f, 1.0f);
						imgui_PushStyleColor((int)ImGuiCol.ButtonActive, 0.75f, 0.20f, 0.20f, 1.0f);
						if (imgui_Button("Delete Value"))
						{
							// Delete the currently selected value
							var pdAct = GetActiveCharacterIniData();
							var selSec = pdAct.Sections.GetSectionData(state.SelectedSection);
							var key = selSec?.Keys.GetKeyData(state.SelectedKey);
							if (key != null)
							{
								var vals = GetValues(key);
								if (state.SelectedValueIndex >= 0 && state.SelectedValueIndex < vals.Count)
								{
									vals.RemoveAt(state.SelectedValueIndex);
									WriteValues(key, vals);
									state.SelectedValueIndex = -1; // Clear selection after delete
								}
							}
						}
						imgui_PopStyleColor(3);
						imgui_SameLine();

						// Show/Hide Editor button with pulsing highlight
						float pulse = (float)Math.Abs(Math.Sin(DateTime.Now.Ticks / 3000000.0));
						float highlightR = 0.95f + (pulse * 0.05f);
						float highlightG = 0.75f + (pulse * 0.25f);
						float highlightB = 0.35f + (pulse * 0.15f);
						imgui_PushStyleColor((int)ImGuiCol.Button, highlightR, highlightG, highlightB, 1.0f);
						imgui_PushStyleColor((int)ImGuiCol.ButtonHovered, 1.0f, 0.85f, 0.45f, 1.0f);
						imgui_PushStyleColor((int)ImGuiCol.ButtonActive, 0.85f, 0.65f, 0.25f, 1.0f);
						imgui_PushStyleColor((int)ImGuiCol.Text, 0.1f, 0.1f, 0.1f, 1.0f); // Dark text for readability

						string btnLabel = state.Show_ShowIntegratedEditor ? "Hide Editor" : "Show Editor";
						if (imgui_Button(btnLabel))
						{
							state.Show_ShowIntegratedEditor = !state.Show_ShowIntegratedEditor;
							if (state.Show_ShowIntegratedEditor)
							{
								// Initialize manual edit buffer when opening
								var keyData = selectedSection?.Keys?.GetKeyData(state.SelectedKey ?? string.Empty);
								var valuesList = GetValues(keyData);
								if (state.SelectedValueIndex >= 0 && state.SelectedValueIndex < valuesList.Count)
								{
									_cfgManualEditBuffer = valuesList[state.SelectedValueIndex] ?? string.Empty;
								}
							}
						}

						imgui_PopStyleColor(4);
						string editorHint = state.Show_ShowIntegratedEditor ? "Editor panel is open below." : "Click to show the advanced editor.";
						imgui_TextColored(0.7f, 0.8f, 0.9f, 1.0f, editorHint);
						imgui_Separator();
					}
				}
			}


			// Special section buttons
			bool isHeals = string.Equals(state.SelectedSection, "Heals", StringComparison.OrdinalIgnoreCase);
			bool isTankKey = string.Equals(state.SelectedKey, "Tank", StringComparison.OrdinalIgnoreCase);
			bool isImpKey = string.Equals(state.SelectedKey, "Important Bot", StringComparison.OrdinalIgnoreCase);

			if (isHeals && (isTankKey || isImpKey))
			{
				if (imgui_Button("Pick Toons"))
				{
					try
					{
						var keys = E3Core.Server.NetMQServer.SharedDataClient?.UsersConnectedTo?.Keys?.ToList() ?? new List<string>();
						keys.Sort(StringComparer.OrdinalIgnoreCase);
						_cfgToonCandidates = keys;
						_cfgToonPickerStatus = keys.Count == 0 ? "No connected toons detected." : $"{keys.Count} connected.";
					}
					catch { _cfgToonCandidates = new List<string>(); _cfgToonPickerStatus = "Error loading toons."; }
					_state.Show_ToonPickerModal = true;
				}
			}

			// Ifs section: add-new key helper
			if (string.Equals(state.SelectedSection, "Ifs", StringComparison.OrdinalIgnoreCase))
			{
				if (imgui_Button("Sample If's"))
				{
					try { LoadSampleIfsForModal(); _state.Show_IfSampleModal = true; }
					catch (Exception ex) { _cfgIfSampleStatus = "Load failed: " + (ex.Message ?? "error"); _state.Show_IfSampleModal = true; }
				}
			}

			// Burn section: add-new key helper
			if (string.Equals(state.SelectedSection, "Burn", StringComparison.OrdinalIgnoreCase))
			{
			}

			imgui_Separator();

			// Display selected value information
			if (state.SelectedValueIndex >= 0)
			{
				var kd = selectedSection?.Keys?.GetKeyData(state.SelectedKey ?? string.Empty);
				var values = GetValues(kd);
				if (state.SelectedValueIndex < values.Count)
				{
					string selectedValue = values[state.SelectedValueIndex];
					var editState = EnsureSpellEditState(state.SelectedSection, state.SelectedKey, state.SelectedValueIndex, selectedValue);
					string lookupName = editState?.BaseName;
					if (string.IsNullOrWhiteSpace(lookupName))
					{
						lookupName = selectedValue;
					}
					imgui_TextColored(0.95f, 0.85f, 0.35f, 1.0f, "Selected Entry");
					imgui_TextWrapped(string.IsNullOrWhiteSpace(lookupName) ? "(empty entry)" : lookupName);
					if (!string.Equals(lookupName, selectedValue, StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(selectedValue))
					{
						imgui_TextColored(0.7f, 0.8f, 0.9f, 0.9f, $"Raw: {selectedValue}");
					}

					// Try to find spell/item/AA information for description
					if (_state.State_CatalogReady)
					{
						var spellInfo = FindSpellItemAAByName(lookupName);
						RenderSpellAdditionalInfo(spellInfo);
						if (spellInfo != null && !string.IsNullOrWhiteSpace(spellInfo.Description))
						{
							imgui_Separator();
							imgui_TextColored(0.75f, 0.85f, 1.0f, 1.0f, "Description");
							imgui_TextWrapped(spellInfo.Description);
						}
						else if (spellInfo != null)
						{
							imgui_Separator();
							imgui_TextColored(0.8f, 0.8f, 0.6f, 1.0f, "Catalog has no description for this entry.");
						}
						else
						{
							imgui_TextColored(0.8f, 0.8f, 0.6f, 1.0f, "(No catalog info found)");
						}
					}
					else
					{
						imgui_TextColored(0.8f, 0.8f, 0.6f, 1.0f, "(Catalogs not loaded)");
					}

					imgui_Separator();
				}
				else
				{
					InvalidateSpellEditState();
				}
			}
			else
			{
				InvalidateSpellEditState();
			}

		}

		private static void RenderActiveModals(SectionData selectedSection)
		{
			if (_state.Show_AddModal)
			{
				RenderAddFromCatalogModal(GetActiveCharacterIniData(), selectedSection);
			}
			if (_state.Show_FoodDrinkModal)
			{
				RenderFoodDrinkPicker(selectedSection);
			}
			if (_state.Show_BardMelodyHelper)
			{
				RenderBardMelodyHelperModal();
			}
			if (_state.Show_BardSampleIfModal)
			{
				RenderBardSampleIfModal();
			}
			if (_state.Show_ToonPickerModal)
			{
				RenderToonPickerModal(selectedSection);
			}
			if (_state.Show_SpellInfoModal)
			{
				RenderSpellInfoModal();
			}
			if (_state.Show_IfAppendModal)
			{
				RenderIfAppendModal(selectedSection);
			}
			if (_state.Show_IfSampleModal)
			{
				RenderIfsSampleModal();
			}
			// Modal editor deprecated - now using integrated panel instead
			// if (_cfgShowSpellModifierModal)
			// {
			// 	RenderSpellModifierModal();
			// }
		}

		// Helper to determine if a key is healing-related
		// Save out active ini data (current or selected)
		private static void SaveActiveIniData()
		{
			try
			{
				string currentPath = GetCurrentCharacterIniPath();
				string selectedPath = GetActiveSettingsPath();
				var pd = GetActiveCharacterIniData();
				if (string.IsNullOrEmpty(selectedPath) || pd == null) return;

				var parser = E3Core.Utility.e3util.CreateIniParser();
				parser.WriteFile(selectedPath, pd);
				var state = _state.GetState<State_MainWindow>();
				state.ConfigIsDirty = false;
				_log.Write($"Saved changes to {Path.GetFileName(selectedPath)}");
			}
			catch (Exception ex)
			{
				_log.Write($"Failed to save: {ex.Message}", Logging.LogLevels.Error);
			}
		}

		// Clear pending changes on the selected ini (reload from disk)
		private static void ClearPendingChanges()
		{
			var mainWindowState = _state.GetState<State_MainWindow>();
			try
			{
				string currentPath = GetCurrentCharacterIniPath();
				string selectedPath = GetActiveSettingsPath();

				if (string.IsNullOrEmpty(selectedPath))
				{
					_log.Write("No ini file selected");
					return;
				}

				// Reload from disk
				var parser = E3Core.Utility.e3util.CreateIniParser();
				var pd = parser.ReadFile(selectedPath);

				// Update the appropriate data reference
				if (string.Equals(selectedPath, currentPath, StringComparison.OrdinalIgnoreCase))
				{
					// Reloading current character's ini
					E3.CharacterSettings.ParsedData = pd;
					mainWindowState.CurrentINIData = pd;
				}
				else
				{
					// Reloading a different character's ini
					mainWindowState.CurrentINIData = pd;
				}

				mainWindowState.ConfigIsDirty = false;
				mainWindowState.SelectedValueIndex = -1;
				InvalidateSpellEditState();
				_log.Write($"Cleared pending changes for {Path.GetFileName(selectedPath)}");
			}
			catch (Exception ex)
			{
				_log.Write($"Failed to clear changes: {ex.Message}");
			}
		}
		static List<String> _catalogRefreshKeyTypes = new List<string>() { "Spells", "AAs", "Discs", "Skills", "Items" };
		static Int64 _numberofMillisecondsBeforeCatalogNeedsRefresh = 30000;

		private static void ProcessBackground_UpdateRemotePlayer(string targetToon)
		{
			//put lower case as zeromq is case sensitive
			targetToon = targetToon.ToLower();

			//have to make a network call and wait for a response. 
			System.Threading.Tasks.Task.Run(() =>
			{
				try
				{
					//pre-create the new lookups
					SortedDictionary<string, SortedDictionary<string, List<E3Spell>>>
					mapSpells = new SortedDictionary<string, SortedDictionary<string, List<E3Spell>>>(),
					mapAAs = new SortedDictionary<string, SortedDictionary<string, List<E3Spell>>>(),
					mapDiscs = new SortedDictionary<string, SortedDictionary<string, List<E3Spell>>>(),
					mapSkills = new SortedDictionary<string, SortedDictionary<string, List<E3Spell>>>(),
					mapItems = new SortedDictionary<string, SortedDictionary<string, List<E3Spell>>>();
					Dictionary<string, SpellData>
					spellLookup = new Dictionary<string, SpellData>(StringComparer.OrdinalIgnoreCase),
					aaLookup = new Dictionary<string, SpellData>(StringComparer.OrdinalIgnoreCase),
					discLookup = new Dictionary<string, SpellData>(StringComparer.OrdinalIgnoreCase),
					skillLookup = new Dictionary<string, SpellData>(StringComparer.OrdinalIgnoreCase),
					itemLookup = new Dictionary<string, SpellData>(StringComparer.OrdinalIgnoreCase);


					_log.WriteDelayed($"Fetching data (remote)", Logging.LogLevels.Debug);

					//_state.Status_CatalogRequest = $"Loading catalogs from {targetToon}...";
					bool peerSuccess = true;


					// Send request: CatalogReq-<Toon>

					//do we have that toons data already in memory or is it too old?


					bool needDataRefresh = false;

					Int64 dataMustBeNewerThan = Core.StopWatch.ElapsedMilliseconds - _numberofMillisecondsBeforeCatalogNeedsRefresh;
					foreach (var key in _catalogRefreshKeyTypes)
					{
						string topicKey = $"CatalogResp-{E3.CurrentName}-{key}";
						_log.WriteDelayed($"Checking for catalog key for target toon:{targetToon} key:{topicKey}", Logging.LogLevels.Debug);

						if (NetMQServer.SharedDataClient.TopicUpdates.TryGetValue(targetToon, out var topics)
							&& topics.TryGetValue(topicKey, out var entry))
						{
							//if we called within the last 60 seconds, just use the old data
							if (entry.LastUpdate < dataMustBeNewerThan)
							{
								_log.WriteDelayed($"Catalog key found but too old, asking for refresh: old:{entry.LastUpdate} vs  new:{dataMustBeNewerThan}", Logging.LogLevels.Debug);

								needDataRefresh = true;
								dataMustBeNewerThan = 0;
								break;
							}
						}
						else
						{
							_log.WriteDelayed($"Catalog key not found, asking for refresh", Logging.LogLevels.Debug);

							needDataRefresh = true;
							dataMustBeNewerThan = 0;
							break;
						}
					}



					if (needDataRefresh)
					{
						PubServer.AddTopicMessage($"CatalogReq-{targetToon}", "");
					}

					if (TryFetchPeerSpellDataListPub(targetToon, "Spells", out var ps, dataMustBeNewerThan))
					{
						mapSpells = OrganizeCatalog(ps);
						spellLookup = ConvertToSpellDataLookup(ps);
					}
					if (TryFetchPeerSpellDataListPub(targetToon, "AAs", out var pa, dataMustBeNewerThan))
					{
						mapAAs = OrganizeCatalog(pa);
						aaLookup = ConvertToSpellDataLookup(ps);
					}
					if (TryFetchPeerSpellDataListPub(targetToon, "Discs", out var pd, dataMustBeNewerThan))
					{
						mapDiscs = OrganizeCatalog(pd);
						discLookup = ConvertToSpellDataLookup(pd);
					}
					if (TryFetchPeerSpellDataListPub(targetToon, "Skills", out var pk, dataMustBeNewerThan))
					{
						mapSkills = OrganizeSkillsCatalog(pk);
						skillLookup = ConvertToSpellDataLookup(pk);
					}
					if (TryFetchPeerSpellDataListPub(targetToon, "Items", out var pi, dataMustBeNewerThan))
					{
						mapItems = OrganizeItemsCatalog(pi);
						itemLookup = ConvertToSpellDataLookup(pi);
					}

					// Also try to fetch gem data
					if (peerSuccess && TryFetchPeerGemData(targetToon, out var gemData))
					{
						lock (_dataLock)
						{
							_cfg_CatalogGems = gemData;
							_state.State_GemsAvailable = true;
						}
					}
					else
					{
						lock (_dataLock)
						{
							_state.State_GemsAvailable = false;
						}
					}

					// If any peer fetch failed, fallback to local
					if (!peerSuccess)
					{
						_state.Status_CatalogRequest = "Peer catalog fetch failed; using local.";
						_cfg_CatalogSource = "Local (fallback)";
					}
					else
					{
						_cfg_CatalogSource = $"Remote ({targetToon})";
					}
					_log.WriteDelayed($"Fetching data (remote) Complete!", Logging.LogLevels.Debug);


					lock (_dataLock)
					{
						// Publish atomically
						_catalog_Spells = mapSpells;
						_spellCatalogLookup = spellLookup;
						_catalog_AA = mapAAs;
						_aaCatalogLookup = aaLookup;
						_catalog_Disc = mapDiscs;
						_discCatalogLookup = discLookup;
						_catalog_Skills = mapSkills;
						_skillCatalogLookup = skillLookup;
						_catalog_Items = mapItems;
						_itemCatalogLookup = itemLookup;

						_catalogLookups = new[]
						{
							(_catalog_Spells, "Spell"),
							(_catalog_AA, "AA"),
							(_catalog_Disc, "Disc"),
							(_catalog_Skills, "Skill"),
							(_catalog_Items, "Item")
							};
						_state.State_CatalogReady = true;
						_state.Status_CatalogRequest = "Catalogs loaded.";

					}

				}
				catch (Exception ex)
				{
					_state.Status_CatalogRequest = "Catalog load failed: " + (ex.Message ?? "error");
				}
				finally
				{
					_state.State_CatalogLoading = false;
					_state.State_CatalogLoadRequested = false;
				}
			});
		}
		private static Dictionary<string, SpellData> ConvertSpellsToSpellDataLookup(List<Spell> spells)
		{
			Dictionary<string, SpellData> returnValue = new Dictionary<string, SpellData>(StringComparer.OrdinalIgnoreCase);
			foreach (var spell in spells)
			{
				var tspell = spell.ToProto();
				if (!returnValue.ContainsKey(tspell.CastName))
				{
					returnValue.Add(tspell.CastName, tspell);
				}
			}
			return returnValue;
		}
		private static Dictionary<string, SpellData> ConvertToSpellDataLookup(Google.Protobuf.Collections.RepeatedField<SpellData> spells)
		{
			Dictionary<string, SpellData> returnValue = new Dictionary<string, SpellData>(StringComparer.OrdinalIgnoreCase);
			foreach (var spell in spells)
			{
				if (!returnValue.ContainsKey(spell.CastName))
				{
					returnValue.Add(spell.CastName, spell);
				}

			}
			return returnValue;
		}

		private static void UpdateLocalSpellGemDataViaLocal()
		{
			try
			{
				var localGems = new string[12];
				var localGemIcons = new int[12];

				for (int gem = 1; gem <= 12; gem++)
				{
					try
					{
						string spellName = MQ.Query<string>($"${{Me.Gem[{gem}]}}");
						Int32 spellID = MQ.Query<Int32>($"${{Me.Gem[{gem}].ID}}");
						localGems[gem - 1] = spellID.ToString();

						// Get spell icon index if we have a valid spell
						if (!string.IsNullOrEmpty(spellName) && !spellName.Equals("NULL", StringComparison.OrdinalIgnoreCase))
						{
							localGemIcons[gem - 1] = GetLocalSpellIconIndex(spellName);
						}
						else
						{
							localGemIcons[gem - 1] = -1;
						}
					}
					catch
					{
						localGems[gem - 1] = "ERROR";
						localGemIcons[gem - 1] = -1;
					}
				}
				lock (_dataLock)
				{
					_cfg_CatalogGems = localGems;
					_cfg_CatalogGemIcons = localGemIcons;
					_state.State_GemsAvailable = true;
				}
			}
			catch (Exception ex)
			{
				_log.WriteDelayed($"Fetching data Error: {ex.Message}", Logging.LogLevels.Debug);
				_state.State_GemsAvailable = false;
			}
		}
		private static void ProcessBackground_UpdateLocalPlayer()
		{
			SortedDictionary<string, SortedDictionary<string, List<E3Spell>>> mapSpells = new SortedDictionary<string, SortedDictionary<string, List<E3Spell>>>(),
					mapAAs = new SortedDictionary<string, SortedDictionary<string, List<E3Spell>>>(),
					mapDiscs = new SortedDictionary<string, SortedDictionary<string, List<E3Spell>>>(),
					mapSkills = new SortedDictionary<string, SortedDictionary<string, List<E3Spell>>>(),
					mapItems = new SortedDictionary<string, SortedDictionary<string, List<E3Spell>>>();

			_state.Status_CatalogRequest = "Loading catalogs (local)...";
			_cfg_CatalogSource = "Local";

			_log.WriteDelayed($"Fetching data (local)", Logging.LogLevels.Debug);

			var spellList = e3util.ListAllBookSpells();
			mapSpells = OrganizeLoadingCatalog(spellList);
			var spellLookup = ConvertSpellsToSpellDataLookup(spellList);

			var aaList = e3util.ListAllActiveAA();
			mapAAs = OrganizeLoadingCatalog(aaList);
			var aaLookup = ConvertSpellsToSpellDataLookup(aaList);

			var discList = e3util.ListAllDiscData();
			mapDiscs = OrganizeLoadingCatalog(discList);
			var discLookup = ConvertSpellsToSpellDataLookup(discList);

			var skillList = e3util.ListAllActiveSkills();
			mapSkills = OrganizeLoadingSkillsCatalog(skillList);
			var skillLookup = ConvertSpellsToSpellDataLookup(skillList);

			var itemList = e3util.ListAllItemWithClickyData();
			mapItems = OrganizeLoadingItemsCatalog(itemList);
			var itemLookup = ConvertSpellsToSpellDataLookup(itemList);

			// Also collect local gem data with spell icon indices
			UpdateLocalSpellGemDataViaLocal();

			_log.WriteDelayed($"Fetching data (local) Complete!", Logging.LogLevels.Debug);

			lock (_dataLock)
			{
				// Publish atomically
				_catalog_Spells = mapSpells;
				_spellCatalogLookup = spellLookup;

				_catalog_AA = mapAAs;
				_aaCatalogLookup = aaLookup;

				_catalog_Disc = mapDiscs;
				_discCatalogLookup = discLookup;

				_catalog_Skills = mapSkills;
				_skillCatalogLookup = skillLookup;

				_catalog_Items = mapItems;
				_itemCatalogLookup = itemLookup;

				_catalogLookups = new[]
				{
					(_catalog_Spells, "Spell"),
					(_catalog_AA, "AA"),
					(_catalog_Disc, "Disc"),
					(_catalog_Skills, "Skill"),
					(_catalog_Items, "Item")
				};
				_state.State_CatalogReady = true;
				_state.Status_CatalogRequest = "Catalogs loaded.";
				_state.State_CatalogLoading = false;
				_state.State_CatalogLoadRequested = false;

			}
		}
		// Background worker tick invoked from E3.Process(): handle catalog loads and icon system
		private static void ProcessBackgroundWork()
		{
			if (_state.State_CatalogLoadRequested && !_state.State_CatalogLoading)
			{

				_state.State_CatalogLoading = true;
				_log.WriteDelayed("Making background request", Logging.LogLevels.Debug);


				_log.WriteDelayed("Tryign to fetch data for user", Logging.LogLevels.Debug);

				// Always fetch via RouterServer, same as e3config
				string targetToon = GetSelectedIniOwnerName();

				_log.WriteDelayed($"Target tooon: {targetToon}", Logging.LogLevels.Debug);

				bool isLocal = string.IsNullOrEmpty(targetToon) || targetToon.Equals(E3.CurrentName, StringComparison.OrdinalIgnoreCase);

				_log.WriteDelayed($"Are they local?: {isLocal}", Logging.LogLevels.Debug);

				try
				{
					if (isLocal)
					{
						ProcessBackground_UpdateLocalPlayer();
					}
					else
					{
						if (GetOnlineToonNames().ContainsKey(targetToon))
						{
							ProcessBackground_UpdateRemotePlayer(targetToon);
						}
					}

				}
				finally
				{
					_state.State_CatalogLoading = false;
					_state.State_CatalogLoadRequested = false;
				}
			}
			// Food/Drink inventory scan (local or remote peer) — non-blocking
			if (_cfgFoodDrinkScanRequested && !_cfgFoodDrinkPending)
			{
				_cfgFoodDrinkScanRequested = false;
				try
				{
					string owner = GetSelectedIniOwnerName();
					bool isLocal = string.IsNullOrEmpty(owner) || owner.Equals(E3.CurrentName, StringComparison.OrdinalIgnoreCase);
					if (!isLocal)
					{
						// Start remote request and mark pending; actual receive handled below
						E3Core.Server.PubServer.AddTopicMessage($"InvReq-{owner}", _cfgFoodDrinkKey);
						_cfgFoodDrinkPending = true;
						_cfgFoodDrinkPendingToon = owner;
						_cfgFoodDrinkPendingType = _cfgFoodDrinkKey;
						_cfgFoodDrinkTimeoutAt = Core.StopWatch.ElapsedMilliseconds + 2000;
						_cfgFoodDrinkStatus = $"Scanning {_cfgFoodDrinkKey} on {owner}...";
					}
					else
					{
						var list = ScanInventoryForType(_cfgFoodDrinkKey);
						_cfgFoodDrinkCandidates = list ?? new List<string>();
						_cfgFoodDrinkStatus = _cfgFoodDrinkCandidates.Count == 0 ? "No matches found in inventory." : $"Found {_cfgFoodDrinkCandidates.Count} items.";
					}
				}
				catch (Exception ex)
				{
					_cfgFoodDrinkStatus = "Scan failed: " + (ex.Message ?? "error");
				}
			}
			// Remote response polling — checked each tick without blocking
			if (_cfgFoodDrinkPending)
			{
				try
				{
					string toon = _cfgFoodDrinkPendingToon;
					string type = _cfgFoodDrinkPendingType;
					string topic = $"InvResp-{E3.CurrentName}-{type}";
					// Prefer remote publisher bucket
					if (E3Core.Server.NetMQServer.SharedDataClient.TopicUpdates.TryGetValue(toon, out var topics)
						&& topics.TryGetValue(topic, out var entry))
					{
						string payload = entry.Data ?? string.Empty;
						int first = payload.IndexOf(':');
						int second = first >= 0 ? payload.IndexOf(':', first + 1) : -1;
						string b64 = (second > 0 && second + 1 < payload.Length) ? payload.Substring(second + 1) : payload;
						try
						{
							var bytes = Convert.FromBase64String(b64);
							var joined = Encoding.UTF8.GetString(bytes);
							_cfgFoodDrinkCandidates = (joined ?? string.Empty).Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries)
								.Select(s => s.Trim())
								.Where(s => s.Length > 0)
								.Distinct(StringComparer.OrdinalIgnoreCase)
								.OrderBy(s => s, StringComparer.OrdinalIgnoreCase)
								.ToList();
						}
						catch
						{
							_cfgFoodDrinkCandidates = new List<string>();
						}
						_cfgFoodDrinkStatus = _cfgFoodDrinkCandidates.Count == 0 ? $"No {type} found on {toon}." : $"Found {_cfgFoodDrinkCandidates.Count} items on {toon}.";
						_cfgFoodDrinkPending = false;
					}
					else if (E3Core.Server.NetMQServer.SharedDataClient.TopicUpdates.TryGetValue(E3.CurrentName, out var topics2)
							 && topics2.TryGetValue(topic, out var entry2))
					{
						string payload = entry2.Data ?? string.Empty;
						int first = payload.IndexOf(':');
						int second = first >= 0 ? payload.IndexOf(':', first + 1) : -1;
						string b64 = (second > 0 && second + 1 < payload.Length) ? payload.Substring(second + 1) : payload;
						try
						{
							var bytes = Convert.FromBase64String(b64);
							var joined = Encoding.UTF8.GetString(bytes);
							_cfgFoodDrinkCandidates = (joined ?? string.Empty).Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries)
								.Select(s => s.Trim())
								.Where(s => s.Length > 0)
								.Distinct(StringComparer.OrdinalIgnoreCase)
								.OrderBy(s => s, StringComparer.OrdinalIgnoreCase)
								.ToList();
						}
						catch
						{
							_cfgFoodDrinkCandidates = new List<string>();
						}
						_cfgFoodDrinkStatus = _cfgFoodDrinkCandidates.Count == 0 ? $"No {type} found on {toon}." : $"Found {_cfgFoodDrinkCandidates.Count} items on {toon}.";
						_cfgFoodDrinkPending = false;
					}
					else if (Core.StopWatch.ElapsedMilliseconds >= _cfgFoodDrinkTimeoutAt)
					{
						_cfgFoodDrinkStatus = $"Remote {type} scan timed out for {toon}.";
						_cfgFoodDrinkCandidates = new List<string>();
						_cfgFoodDrinkPending = false;
					}
				}
				catch
				{
					_cfgFoodDrinkStatus = "Remote scan error.";
					_cfgFoodDrinkCandidates = new List<string>();
					_cfgFoodDrinkPending = false;
				}
			}

			if (_state.Request_AllplayersRefresh && !_cfgAllPlayersRefreshing)
			{
				var mainWindowState = _state.GetState<State_MainWindow>();
				var allPlayerState = _state.GetState<State_AllPlayers>();

				_state.Request_AllplayersRefresh = false; // consume the pending request before we start
				_cfgAllPlayersRefreshing = true;
				allPlayerState.ReqSection = mainWindowState.SelectedSection;
				allPlayerState.ReqKey = mainWindowState.SelectedKey;

				System.Threading.Tasks.Task.Run(() =>
				{
					try
					{

						allPlayerState.Status = "Refreshing...";

						var newRows = new List<KeyValuePair<string, string>>();
						string section = allPlayerState.ReqSection;
						string key = allPlayerState.ReqKey;

						foreach (var toon in GetOnlineToonNames().Keys)
						{
							string value = string.Empty;

							// First, try reading directly from the toon's local INI (if present on this machine)
							bool gotLocal = TryReadIniValueForToon(toon, section, key, out value);

							// If we didn't get a value locally and it's a remote toon, request from peer
							if (!gotLocal && !toon.Equals(E3.CurrentName, StringComparison.OrdinalIgnoreCase))
							{
								string requestTopic = $"ConfigValueReq-{toon}";
								string payload = $"{section}:{key}";
								E3Core.Server.PubServer.AddTopicMessage(requestTopic, payload);

								string responseTopic = $"ConfigValueResp-{E3.CurrentName}-{section}:{key}";
								long end = Core.StopWatch.ElapsedMilliseconds + 2000;
								bool found = false;
								while (Core.StopWatch.ElapsedMilliseconds < end)
								{
									if (E3Core.Server.NetMQServer.SharedDataClient.TopicUpdates.TryGetValue(toon, out var topics) &&
										topics.TryGetValue(responseTopic, out var entry))
									{
										value = entry.Data;
										found = true;
										break;
									}
									if (E3Core.Server.NetMQServer.SharedDataClient.TopicUpdates.TryGetValue(E3.CurrentName, out var topics2) &&
										topics2.TryGetValue(responseTopic, out var entry2))
									{
										value = entry2.Data;
										found = true;
										break;
									}
									System.Threading.Thread.Sleep(25);
								}
								if (!found) value = "<timeout>";
							}

							newRows.Add(new KeyValuePair<string, string>(toon, value));
						}

						lock (allPlayerState.DataLock)
						{
							allPlayerState.Data_Rows = newRows;
							allPlayerState.Data_Edit = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
							foreach (var row in newRows)
							{
								var toonKey = row.Key ?? string.Empty;
								allPlayerState.Data_Edit[toonKey] = row.Value ?? string.Empty;
							}
						}
						allPlayerState.LastUpdatedAt = Core.StopWatch.ElapsedMilliseconds;
					}
					catch (Exception ex)
					{
						allPlayerState.Status = "Refresh failed: " + ex.Message;
					}
					finally
					{
						_cfgAllPlayersRefreshing = false;
					}
				});
			}
		}

		// Fetch gem data from peer catalog response (now includes spell icon indices)
		private static bool TryFetchPeerGemData(string toon, out string[] gemData)
		{
			gemData = new string[12];
			try
			{
				_log.WriteDelayed($"Tryign to fetch gem data for:{toon}", Logging.LogLevels.Debug);

				if (string.IsNullOrEmpty(toon)) return false;

				string topic = $"CatalogResp-{E3.CurrentName}-Gems";
				// Poll SharedDataClient.TopicUpdates for gem data
				long end = Core.StopWatch.ElapsedMilliseconds + 2000; // 2 second timeout
				while (Core.StopWatch.ElapsedMilliseconds < end)
				{
					if (E3Core.Server.NetMQServer.SharedDataClient.TopicUpdates.TryGetValue(toon, out var topics)
						&& topics.TryGetValue(topic, out var entry))
					{
						string payload = entry.Data;
						ParseGemDataWithIcons(payload, out gemData, out _cfg_CatalogGemIcons);
						return true;
					}

					// Also check if data came back under current name
					if (E3Core.Server.NetMQServer.SharedDataClient.TopicUpdates.TryGetValue(E3.CurrentName, out var topics2)
						&& topics2.TryGetValue(topic, out var entry2))
					{
						string payload = entry2.Data;
						ParseGemDataWithIcons(payload, out gemData, out _cfg_CatalogGemIcons);
						return true;
					}

					System.Threading.Thread.Sleep(25);
				}
			}
			catch { }

			// Fill with ERROR if failed
			for (int i = 0; i < 12; i++)
			{
				gemData[i] = "ERROR";
				_cfg_CatalogGemIcons[i] = -1;
			}
			return false;
		}

		// Helper method to parse gem data with icon indices from pipe-separated format
		private static void ParseGemDataWithIcons(string payload, out string[] gemNames, out int[] gemIcons)
		{
			gemNames = new string[12];
			gemIcons = new int[12];
			_log.WriteDelayed($"Parsing gem data with payload:{payload}", Logging.LogLevels.Debug);

			try
			{
				// Parse pipe-separated gem data: "SpellName:IconIndex|SpellName:IconIndex|..."
				var gems = payload.Split('|');
				int count = Math.Min(gems.Length, 12);

				for (int i = 0; i < count; i++)
				{
					string gemEntry = gems[i] ?? "NULL:-1";
					string[] parts = gemEntry.Split(':');

					if (parts.Length >= 2)
					{
						gemNames[i] = parts[0] ?? "NULL";
						if (int.TryParse(parts[1], out int iconIndex))
						{
							gemIcons[i] = iconIndex;
						}
						else
						{
							gemIcons[i] = -1;
						}
					}
					else
					{
						// Fallback for old format without icons
						gemNames[i] = gemEntry ?? "NULL";
						gemIcons[i] = -1;
					}
				}

				// Fill remaining slots if needed
				for (int i = count; i < 12; i++)
				{
					gemNames[i] = "NULL";
					gemIcons[i] = -1;
				}
			}
			catch
			{
				// Error case - fill with defaults
				for (int i = 0; i < 12; i++)
				{
					gemNames[i] = "ERROR";
					gemIcons[i] = -1;
				}
			}
		}

		// Helper method to get spell icon index for local spells
		private static int GetLocalSpellIconIndex(string spellName)
		{
			if (string.IsNullOrEmpty(spellName)) return -1;

			try
			{
				// Use the catalog lookups if they're available
				var spellInfo = FindSpellItemAAByName(spellName);
				if (spellInfo != null && spellInfo.SpellIcon >= 0)
				{
					return spellInfo.SpellIcon;
				}

				// Fallback: Query MQ directly for spell icon
				int iconIndex = E3.MQ.Query<int>($"${{Spell[{spellName}].SpellIcon}}");
				return iconIndex > 0 ? iconIndex : -1;
			}
			catch
			{
				return -1;
			}
		}

		// PubSub relay approach: request peer to publish SpellDataList as base64 on response topic
		private static bool TryFetchPeerSpellDataListPub(string toon, string listKey, out Google.Protobuf.Collections.RepeatedField<SpellData> data, Int64 dataMustBeOlderThan)
		{
			data = new Google.Protobuf.Collections.RepeatedField<SpellData>();
			//topics are stored in toon specific keys
			string topic = $"CatalogResp-{E3.CurrentName}-{listKey}";
			// Poll SharedDataClient.TopicUpdates for up to ~2s
			long end = Core.StopWatch.ElapsedMilliseconds + 4000;

			_log.WriteDelayed($"Trying to fetch data with key:{topic}", Logging.LogLevels.Debug);


			while (Core.StopWatch.ElapsedMilliseconds < end)
			{
				if (NetMQServer.SharedDataClient.TopicUpdates.TryGetValue(toon, out var topics)
					&& topics.TryGetValue(topic, out var entry))
				{
					if (entry.LastUpdate < dataMustBeOlderThan) continue;
					_log.WriteDelayed($"Data found with key:{topic}", Logging.LogLevels.Debug);

					string payload = entry.Data;
					int first = payload.IndexOf(':');
					int second = first >= 0 ? payload.IndexOf(':', first + 1) : -1;
					string b64 = (second > 0 && second + 1 < payload.Length) ? payload.Substring(second + 1) : payload;
					byte[] bytes = Convert.FromBase64String(b64);
					var list = SpellDataList.Parser.ParseFrom(bytes);
					data = list.Data;
					return true;
				}
				System.Threading.Thread.Sleep(25);
			}
			_log.WriteDelayed($"Data NOT FOUND with key:{topic}", Logging.LogLevels.Debug);

			return false;
		}

		private static string GetSelectedIniOwnerName()
		{
			try
			{
				string path = GetActiveSettingsPath();
				if (string.IsNullOrEmpty(path)) return E3.CurrentName;
				string file = Path.GetFileNameWithoutExtension(path);
				int us = file.IndexOf('_');
				if (us > 0) return file.Substring(0, us);
				return file;
			}
			catch { return E3.CurrentName; }
		}
		private static bool IsActiveIniBard()
		{
			try
			{
				var data = GetActiveCharacterIniData();
				if (data?.Sections != null)
				{
					if (data.Sections.ContainsSection("Bard")) return true;
				}

				string path = GetActiveSettingsPath() ?? string.Empty;
				if (string.IsNullOrEmpty(path)) return string.Equals(E3.CurrentClass.ToString(), "Bard", StringComparison.OrdinalIgnoreCase);

				string file = Path.GetFileNameWithoutExtension(path) ?? string.Empty;
				var parts = file.Split(new[] { '_' }, StringSplitOptions.RemoveEmptyEntries);
				if (parts.Length >= 2)
				{
					string cls = parts.Last();
					if (string.Equals(cls, "Bard", StringComparison.OrdinalIgnoreCase)) return true;
				}

				return string.Equals(E3.CurrentClass.ToString(), "Bard", StringComparison.OrdinalIgnoreCase);
			}
			catch
			{
				return string.Equals(E3.CurrentClass.ToString(), "Bard", StringComparison.OrdinalIgnoreCase);
			}
		}

		private static ConcurrentDictionary<string, string> _onlineToonsCache = new ConcurrentDictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		private static Int64 _onlineToonsLastUpdate = 0;
		private static Int64 _onlineToonsLastsUpdateInterval = 3000;
		private static ConcurrentDictionary<string, string> GetOnlineToonNames()
		{

			if (!e3util.ShouldCheck(ref _onlineToonsLastUpdate, _onlineToonsLastsUpdateInterval))
			{
				lock (_onlineToonsCache)
				{
					return _onlineToonsCache;

				}
			}
			lock (_onlineToonsCache)
			{
				_onlineToonsCache.Clear();

				try
				{
					var connected = E3Core.Server.NetMQServer.SharedDataClient?.UsersConnectedTo?.Keys;
					if (connected != null)
					{
						foreach (var name in connected)
						{
							if (!string.IsNullOrEmpty(name)) _onlineToonsCache.TryAdd(name, name);
						}
					}
				}
				catch { }

				if (!string.IsNullOrEmpty(E3.CurrentName)) _onlineToonsCache.TryAdd(E3.CurrentName, E3.CurrentName);


				return _onlineToonsCache;
			}

		}
		private static bool IsIniForOnlineToon(string iniPath, ConcurrentDictionary<string, string> onlineToons)
		{
			if (onlineToons == null || onlineToons.Count == 0) return false;
			string file = Path.GetFileNameWithoutExtension(iniPath) ?? string.Empty;
			if (string.IsNullOrEmpty(file)) return false;
			int underscore = file.IndexOf('_');
			string toon = underscore > 0 ? file.Substring(0, underscore) : file;
			return onlineToons.ContainsKey(toon);
		}

		// Organize from SpellData (protobuf) into the UI catalog structure

		private static SortedDictionary<string, SortedDictionary<string, List<E3Spell>>> OrganizeLoadingCatalog(List<Spell> data)
		{
			var dest = new SortedDictionary<string, SortedDictionary<string, List<E3Spell>>>(StringComparer.OrdinalIgnoreCase);
			foreach (var s in data)
			{
				if (s == null) continue;
				string cat = s.Category ?? string.Empty;
				string sub = s.Subcategory ?? string.Empty;
				if (!dest.TryGetValue(cat, out var submap))
				{
					submap = new SortedDictionary<string, List<E3Spell>>(StringComparer.OrdinalIgnoreCase);
					dest.Add(cat, submap);
				}
				if (!submap.TryGetValue(sub, out var l))
				{
					l = new List<E3Spell>();
					submap.Add(sub, l);
				}
				l.Add(new E3Spell
				{
					Name = s.SpellName ?? string.Empty,
					Category = cat,
					Subcategory = sub,
					Level = s.Level,
					CastName = s.CastName ?? string.Empty,
					TargetType = s.TargetType ?? string.Empty,
					SpellType = s.SpellType ?? string.Empty,
					Mana = s.Mana,
					CastTime = Convert.ToDouble(s.MyCastTimeInSeconds),
					Recast = s.RecastTime != 0 ? s.RecastTime : s.RecastDelay,
					Range = s.MyRange,
					Description = s.Description ?? string.Empty,
					ResistType = s.ResistType ?? string.Empty,
					ResistAdj = s.ResistAdj,
					CastType = s.CastType.ToString(),
					SpellGem = s.SpellGem,
					SpellEffects = s.SpellEffects != null
						? s.SpellEffects.Where(e => !string.IsNullOrWhiteSpace(e)).Select(e => e.Trim()).ToList()
						: new List<string>(),
					SpellIcon = s.SpellIcon
				});
			}
			foreach (var submap in dest.Values)
			{
				foreach (var l in submap.Values)
				{
					l.Sort((a, b) => b.Level.CompareTo(a.Level));
				}
			}
			return dest;
		}

		private static SortedDictionary<string, SortedDictionary<string, List<E3Spell>>> OrganizeCatalog(Google.Protobuf.Collections.RepeatedField<SpellData> data)
		{
			var dest = new SortedDictionary<string, SortedDictionary<string, List<E3Spell>>>(StringComparer.OrdinalIgnoreCase);
			foreach (var s in data)
			{
				if (s == null) continue;
				string cat = s.Category ?? string.Empty;
				string sub = s.Subcategory ?? string.Empty;
				if (!dest.TryGetValue(cat, out var submap))
				{
					submap = new SortedDictionary<string, List<E3Spell>>(StringComparer.OrdinalIgnoreCase);
					dest.Add(cat, submap);
				}
				if (!submap.TryGetValue(sub, out var l))
				{
					l = new List<E3Spell>();
					submap.Add(sub, l);
				}
				l.Add(new E3Spell
				{
					Name = s.SpellName ?? string.Empty,
					Category = cat,
					Subcategory = sub,
					Level = s.Level,
					CastName = s.CastName ?? string.Empty,
					TargetType = s.TargetType ?? string.Empty,
					SpellType = s.SpellType ?? string.Empty,
					Mana = s.Mana,
					CastTime = s.MyCastTimeInSeconds,
					Recast = s.RecastTime != 0 ? s.RecastTime : s.RecastDelay,
					Range = s.MyRange,
					Description = s.Description ?? string.Empty,
					ResistType = s.ResistType ?? string.Empty,
					ResistAdj = s.ResistAdj,
					CastType = s.CastType.ToString(),
					SpellGem = s.SpellGem,
					SpellEffects = s.SpellEffects != null
						? s.SpellEffects.Where(e => !string.IsNullOrWhiteSpace(e)).Select(e => e.Trim()).ToList()
						: new List<string>(),
					SpellIcon = s.SpellIcon
				});
			}
			foreach (var submap in dest.Values)
			{
				foreach (var l in submap.Values)
				{
					l.Sort((a, b) => b.Level.CompareTo(a.Level));
				}
			}
			return dest;
		}
		private static SortedDictionary<string, SortedDictionary<string, List<E3Spell>>> OrganizeLoadingSkillsCatalog(List<Spell> data)
		{
			var dest = new SortedDictionary<string, SortedDictionary<string, List<E3Spell>>>(StringComparer.OrdinalIgnoreCase);
			var cat = "Skill"; var sub = "Basic";
			var submap = new SortedDictionary<string, List<E3Spell>>(StringComparer.OrdinalIgnoreCase);
			dest[cat] = submap;
			var list = new List<E3Spell>();
			submap[sub] = list;
			foreach (var s in data)
			{
				if (s == null) continue;
				list.Add(new E3Spell
				{
					Name = s.SpellName ?? string.Empty,
					Category = cat,
					Subcategory = sub,
					Level = s.Level,
					TargetType = s.TargetType ?? string.Empty,
					SpellType = s.SpellType ?? string.Empty,
					CastType = s.CastType.ToString(),
					Description = s.Description ?? string.Empty,
					SpellEffects = s.SpellEffects != null
						? s.SpellEffects.Where(e => !string.IsNullOrWhiteSpace(e)).Select(e => e.Trim()).ToList()
						: new List<string>(),
					SpellIcon = s.SpellIcon
				});
			}
			list.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));
			return dest;
		}

		// Organize skills like e3config: force into Skill/Basic and list by spell name
		private static SortedDictionary<string, SortedDictionary<string, List<E3Spell>>> OrganizeSkillsCatalog(Google.Protobuf.Collections.RepeatedField<SpellData> data)
		{
			var dest = new SortedDictionary<string, SortedDictionary<string, List<E3Spell>>>(StringComparer.OrdinalIgnoreCase);
			var cat = "Skill"; var sub = "Basic";
			var submap = new SortedDictionary<string, List<E3Spell>>(StringComparer.OrdinalIgnoreCase);
			dest[cat] = submap;
			var list = new List<E3Spell>();
			submap[sub] = list;
			foreach (var s in data)
			{
				if (s == null) continue;
				list.Add(new E3Spell
				{
					Name = s.SpellName ?? string.Empty,
					Category = cat,
					Subcategory = sub,
					Level = s.Level,
					TargetType = s.TargetType ?? string.Empty,
					SpellType = s.SpellType ?? string.Empty,
					CastType = s.CastType.ToString(),
					Description = s.Description ?? string.Empty,
					SpellEffects = s.SpellEffects != null
						? s.SpellEffects.Where(e => !string.IsNullOrWhiteSpace(e)).Select(e => e.Trim()).ToList()
						: new List<string>(),
					SpellIcon = s.SpellIcon
				});
			}
			list.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));
			return dest;
		}
		private static SortedDictionary<string, SortedDictionary<string, List<E3Spell>>> OrganizeLoadingItemsCatalog(List<Spell> data)
		{
			var dest = new SortedDictionary<string, SortedDictionary<string, List<E3Spell>>>(StringComparer.OrdinalIgnoreCase);
			foreach (var s in data)
			{
				if (s == null) continue;
				string cat = s.CastName ?? string.Empty; // item name
				string sub = s.SpellName ?? string.Empty; // click spell
				if (!dest.TryGetValue(cat, out var submap))
				{
					submap = new SortedDictionary<string, List<E3Spell>>(StringComparer.OrdinalIgnoreCase);
					dest.Add(cat, submap);
				}
				if (!submap.TryGetValue(sub, out var l))
				{
					l = new List<E3Spell>();
					submap.Add(sub, l);
				}
				l.Add(new E3Spell
				{
					Name = s.CastName ?? string.Empty,
					Category = cat,
					Subcategory = sub,
					Level = s.Level,
					CastName = s.CastName ?? string.Empty,
					TargetType = s.TargetType ?? string.Empty,
					SpellType = s.SpellType ?? string.Empty,
					Mana = s.Mana,
					CastTime = Convert.ToDouble(s.MyCastTimeInSeconds),
					Recast = s.RecastTime != 0 ? s.RecastTime : s.RecastDelay,
					Range = s.MyRange,
					Description = s.Description ?? string.Empty,
					ResistType = s.ResistType ?? string.Empty,
					ResistAdj = s.ResistAdj,
					CastType = s.CastType.ToString(),
					SpellGem = s.SpellGem,
					SpellEffects = s.SpellEffects != null
						? s.SpellEffects.Where(e => !string.IsNullOrWhiteSpace(e)).Select(e => e.Trim()).ToList()
						: new List<string>(),
					SpellIcon = s.SpellIcon
				});
			}
			return dest;
		}
		// Organize items like e3config: first key = CastName (item), subkey = SpellName, and list entries by item (CastName)
		private static SortedDictionary<string, SortedDictionary<string, List<E3Spell>>> OrganizeItemsCatalog(Google.Protobuf.Collections.RepeatedField<SpellData> data)
		{
			var dest = new SortedDictionary<string, SortedDictionary<string, List<E3Spell>>>(StringComparer.OrdinalIgnoreCase);
			foreach (var s in data)
			{
				if (s == null) continue;
				string cat = s.CastName ?? string.Empty; // item name
				string sub = s.SpellName ?? string.Empty; // click spell
				if (!dest.TryGetValue(cat, out var submap))
				{
					submap = new SortedDictionary<string, List<E3Spell>>(StringComparer.OrdinalIgnoreCase);
					dest.Add(cat, submap);
				}
				if (!submap.TryGetValue(sub, out var l))
				{
					l = new List<E3Spell>();
					submap.Add(sub, l);
				}
				l.Add(new E3Spell
				{
					Name = s.CastName ?? string.Empty,
					Category = cat,
					Subcategory = sub,
					Level = s.Level,
					CastName = s.CastName ?? string.Empty,
					TargetType = s.TargetType ?? string.Empty,
					SpellType = s.SpellType ?? string.Empty,
					Mana = s.Mana,
					CastTime = s.MyCastTimeInSeconds,
					Recast = s.RecastTime != 0 ? s.RecastTime : s.RecastDelay,
					Range = s.MyRange,
					Description = s.Description ?? string.Empty,
					ResistType = s.ResistType ?? string.Empty,
					ResistAdj = s.ResistAdj,
					CastType = s.CastType.ToString(),
					SpellGem = s.SpellGem,
					SpellEffects = s.SpellEffects != null
						? s.SpellEffects.Where(e => !string.IsNullOrWhiteSpace(e)).Select(e => e.Trim()).ToList()
						: new List<string>(),
					SpellIcon = s.SpellIcon
				});
			}
			return dest;
		}


		// Resolves a toon’s ini path by scanning known .ini files and preferring a match
		// that includes the server name if we have it.
		private static bool TryGetIniPathForToon(string toon, out string path)
		{
			var state = _state.GetState<State_MainWindow>();

			path = null;
			if (string.IsNullOrWhiteSpace(toon)) return false;

			// Keep our list of ini files fresh
			ScanCharIniFilesIfNeeded();

			// Current character is easy
			if (!string.IsNullOrEmpty(E3.CurrentName) &&
				string.Equals(E3.CurrentName, toon, StringComparison.OrdinalIgnoreCase))
			{
				path = GetCurrentCharacterIniPath();
				return !string.IsNullOrEmpty(path);
			}

			if (state.IniFilesFromDisk == null || state.IniFilesFromDisk.Length == 0) return false;

			// Optional: prefer matches that also contain server in the filename
			_cfgAllPlayersServerByToon.TryGetValue(toon, out var serverHint);
			serverHint = serverHint ?? string.Empty;

			// Gather candidates: filename starts with "<Toon>_" or equals "<Toon>.ini"
			var candidates = new List<string>();
			foreach (var f in state.IniFilesFromDisk)
			{
				var name = System.IO.Path.GetFileName(f);
				if (name.StartsWith(toon + "_", StringComparison.OrdinalIgnoreCase) ||
					name.Equals(toon + ".ini", StringComparison.OrdinalIgnoreCase))
				{
					candidates.Add(f);
				}
			}

			if (candidates.Count == 0) return false;

			// Prefer one that mentions the server (common pattern: Toon_Server_Class.ini)
			if (!string.IsNullOrEmpty(serverHint))
			{
				var withServer = candidates.FirstOrDefault(f =>
					System.IO.Path.GetFileName(f).IndexOf("_" + serverHint + "_", StringComparison.OrdinalIgnoreCase) >= 0);
				if (!string.IsNullOrEmpty(withServer))
				{
					path = withServer;
					return true;
				}
			}

			// Fallback: first candidate
			path = candidates[0];
			return true;
		}

		// Reads, updates, and writes a single INI value for a toon.

		private static bool IsStringConfigKey(string key)
		{
			if (E3.CharacterSettings.SettingsReflectionStringTypes.Contains(key))
			{
				return true;
			}
			return false;
		}

		private static bool IsIntergerConfigKey(string key)
		{
			if (E3.CharacterSettings.SettingsReflectionIntTypes.Contains(key))
			{

				return true;
			}
			return false;
		}
		private static bool IsBooleanConfigKey(string key)
		{
			if (E3.CharacterSettings.SettingsReflectionBoolTypes.Contains(key))
			{
				
				return true;
			}
			return false;
			//if (kd == null) return false;
			//// Heuristic: keys that are explicitly On/Off
			//var v = (kd.Value ?? string.Empty).Trim();
			//if (string.Equals(v, "On", StringComparison.OrdinalIgnoreCase) || string.Equals(v, "Off", StringComparison.OrdinalIgnoreCase))
			//	return true;
			//// Common patterns
			//if (key.IndexOf("Enable", StringComparison.OrdinalIgnoreCase) >= 0) return true;
			//if (key.IndexOf("Use ", StringComparison.OrdinalIgnoreCase) == 0) return true;
			//return false;
		}


		static Dictionary<string, List<String>> _KeyOptionsLookup = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase) {
			{"Assist Type (Melee/Ranged/Off)", new List<string>() { "Melee","AutoAttack","Ranged","AutoFire","Off" } },
			{"Melee Stick Point", new List<string>() { "Front","Behind","BehindOnce","Pin","!Front" } }

		};
		static List<String> _KeyOptionsOnOff = new List<string>() { "On", "Off" };
		// Attempt to derive an explicit set of allowed options from the key label, e.g.
		// "Assist Type (Melee/Ranged/Off)" => ["Melee","Ranged","Off"]
		private static bool TryGetKeyOptions(string keyLabel, out List<string> options)
		{
			if (_KeyOptionsLookup.TryGetValue(keyLabel, out var result))
			{
				options= result;
				return true;
			}
			options = null;
			return false;

			//options = null;
			//if (string.IsNullOrEmpty(keyLabel)) return false;
			//int i = keyLabel.IndexOf('(');
			//int j = keyLabel.IndexOf(')');
			//if (i < 0 || j <= i) return false;
			//var inside = keyLabel.Substring(i + 1, j - i - 1).Trim();
			//// Only treat as options if slash-delimited list exists
			//if (inside.IndexOf('/') < 0) return false;
			//var parts = inside.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries)
			//				  .Select(x => x.Trim())
			//				  .Where(x => !string.IsNullOrEmpty(x))
			//				  .ToList();
			//if (parts.Count <= 1) return false;
			//// Heuristic: ignore numeric unit hints like "(in milliseconds)" or "(Pct)" or "(1+)"
			//bool looksNumericHint = parts.Any(p => p.Any(char.IsDigit)) || parts.Any(p => p.Equals("Pct", StringComparison.OrdinalIgnoreCase)) || parts.Any(p => p.IndexOf("millisecond", StringComparison.OrdinalIgnoreCase) >= 0);
			//if (looksNumericHint) return false;
			//options = parts;
			//return true;
		}


		// Helper to append or extend an Ifs| key list token in a config value
		private static string AppendIfToken(string value, string ifKey)
		{
			string v = value ?? string.Empty;
			// We support both legacy "Ifs|" and preferred "/Ifs|" tokens when extending,
			// but we always write using "/Ifs|" going forward.
			const string tokenPreferred = "/Ifs|";
			const string tokenLegacy = "Ifs|";
			int posSlash = v.IndexOf(tokenPreferred, StringComparison.OrdinalIgnoreCase);
			int posLegacy = v.IndexOf(tokenLegacy, StringComparison.OrdinalIgnoreCase);
			int pos = posSlash >= 0 ? posSlash : posLegacy;
			int tokenLen = posSlash >= 0 ? tokenPreferred.Length : tokenLegacy.Length;

			if (pos < 0)
			{
				// No Ifs present; append preferred token with NO leading separator
				if (v.Length == 0) return tokenPreferred + ifKey;
				return v + tokenPreferred + ifKey;
			}

			// Extend existing Ifs list; rebuild using preferred token
			int start = pos + tokenLen;
			int end = v.IndexOf('|', start);
			string head = v.Substring(0, pos) + tokenPreferred; // normalize token
			string rest = end >= 0 ? v.Substring(end) : string.Empty;
			string list = end >= 0 ? v.Substring(start, end - start) : v.Substring(start);
			var items = list.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
							.Select(x => x.Trim())
							.Where(x => x.Length > 0)
							.ToList();
			if (!items.Contains(ifKey, StringComparer.OrdinalIgnoreCase)) items.Add(ifKey);
			string rebuilt = head + string.Join(",", items) + rest;
			return rebuilt;
		}

		// Inventory helper that uses MQ TLOs to scan for Food/Drink items
		private static List<string> ScanInventoryForType(string key)
		{
			var found = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			if (string.IsNullOrWhiteSpace(key)) return found.ToList();
			string target = key.Trim();

			// Scan a generous set of inventory indices and their bag contents
			for (int inv = 1; inv <= 40; inv++)
			{
				try
				{
					bool present = E3.MQ.Query<bool>($"${{Me.Inventory[{inv}]}}");
					if (!present) continue;

					// top-level item type
					string t = E3.MQ.Query<string>($"${{Me.Inventory[{inv}].Type}}") ?? string.Empty;
					if (!string.IsNullOrEmpty(t) && t.Equals(target, StringComparison.OrdinalIgnoreCase))
					{
						string name = E3.MQ.Query<string>($"${{Me.Inventory[{inv}]}}") ?? string.Empty;
						if (!string.IsNullOrEmpty(name)) found.Add(name);
					}

					// bag contents if container
					int slots = E3.MQ.Query<int>($"${{Me.Inventory[{inv}].Container}}");
					if (slots <= 0) continue;
					for (int i = 1; i <= slots; i++)
					{
						try
						{
							bool ipresent = E3.MQ.Query<bool>($"${{Me.Inventory[{inv}].Item[{i}]}}");
							if (!ipresent) continue;
							string it = E3.MQ.Query<string>($"${{Me.Inventory[{inv}].Item[{i}].Type}}") ?? string.Empty;
							if (!string.IsNullOrEmpty(it) && it.Equals(target, StringComparison.OrdinalIgnoreCase))
							{
								string iname = E3.MQ.Query<string>($"${{Me.Inventory[{inv}].Item[{i}]}}") ?? string.Empty;
								if (!string.IsNullOrEmpty(iname)) found.Add(iname);
							}
						}
						catch { }
					}
				}
				catch { }
			}

			return found.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToList();
		}
		private static bool IsLikelyFood(string name)
		{
			string n = name.ToLowerInvariant();
			return n.Contains("bread") || n.Contains("meat") || n.Contains("pie") || n.Contains("cake") || n.Contains("cookie") || n.Contains("muffin") || n.Contains("stew") || n.Contains("cheese") || n.Contains("tart") || n.Contains("sausage") || n.Contains("soup") || n.Contains("steak");
		}
		private static bool IsLikelyDrink(string name)
		{
			string n = name.ToLowerInvariant();
			return n.Contains("water") || n.Contains("milk") || n.Contains("wine") || n.Contains("ale") || n.Contains("beer") || n.Contains("tea") || n.Contains("juice") || n.Contains("elixir") || n.Contains("nectar") || n.Contains("brew");
		}
		private static void RenderAddFromCatalogModal_CalculateAddType(AddType typeofadd,out float leftW, out float middleW, out float rightW)
		{
			float totalW = imgui_GetContentRegionAvailX();
																 // Adjust panel widths based on type - Items need wider left panel for longer names

			if (typeofadd == AddType.Items)
			{
				leftW = Math.Max(200f, totalW * 0.30f - 8.0f);     // 30% for item categories (wider)
				middleW = Math.Max(280f, totalW * 0.40f - 8.0f);   // 40% for entries
				rightW = Math.Max(220f, totalW * 0.30f - 8.0f);    // 30% for info
			}
			else
			{
				leftW = Math.Max(150f, totalW * 0.20f - 8.0f);     // 20% for categories
				middleW = Math.Max(280f, totalW * 0.45f - 8.0f);   // 45% for entries
				rightW = Math.Max(220f, totalW * 0.35f - 8.0f);    // 35% for info
			}

		}
		private static void RenderAddFromCatalogModal_Header()
		{
			var state = _state.GetState<State_CatalogWindow>();
			// Header: type + filter + catalog source
			imgui_TextColored(0.95f, 0.85f, 0.35f, 1.0f, "Add From Catalog");
			if (state.Mode == CatalogMode.BardSong)
			{
				state.CurrentAddType = AddType.Spells;
				imgui_SameLine();
				imgui_TextColored(0.7f, 0.9f, 0.7f, 1.0f, "(Select a song for the melody)");
			}
			else
			{
				imgui_SameLine();
				imgui_SetNextItemWidth(100f); // Fixed width for type dropdown
				if (imgui_BeginCombo("##type", state.CurrentAddType.ToString(), 0))
				{
					foreach (AddType t in Enum.GetValues(typeof(AddType)))
					{
						bool sel = t == state.CurrentAddType;
						if (imgui_Selectable(t.ToString(), sel))
						{
							state.CurrentAddType = t;
							state.SelectedCategorySpell = null; // Clear selection when type changes
						}
					}
					EndComboSafe();
				}
			}
			imgui_SameLine();
			imgui_Text("Filter:");
			imgui_SameLine();
			imgui_SetNextItemWidth(200f); // Fixed width for filter input
			if (imgui_InputText("##filter", state.Filter ?? string.Empty))
				state.Filter = imgui_InputText_Get("##filter") ?? string.Empty;

			// Catalog source info and refresh button
			imgui_Separator();
			imgui_TextColored(0.8f, 0.9f, 1.0f, 1.0f, "Catalog Source:");
			imgui_SameLine();

			// Color code the source based on type
			if (_cfg_CatalogSource.StartsWith("Remote"))
				imgui_TextColored(0.7f, 1.0f, 0.7f, 1.0f, _cfg_CatalogSource); // Green for remote
			else if (_cfg_CatalogSource.StartsWith("Local (fallback)"))
				imgui_TextColored(1.0f, 0.8f, 0.4f, 1.0f, _cfg_CatalogSource); // Orange for fallback
			else if (_cfg_CatalogSource.StartsWith("Local"))
				imgui_TextColored(0.8f, 0.8f, 1.0f, 1.0f, _cfg_CatalogSource); // Light blue for local
			else
				imgui_TextColored(0.8f, 0.8f, 0.8f, 1.0f, _cfg_CatalogSource); // Gray for unknown

			imgui_SameLine();
			if (imgui_Button("Refresh Catalog"))
			{
				// Trigger catalog refresh
				RequestCatalogUpdate();
			}

		}
		private static void RenderAddFromCatalogModel_LeftPanel(float leftW,float listH, SortedDictionary<string,SortedDictionary<string,List<E3Spell>>> currentCatalog)
		{
			var state = _state.GetState<State_CatalogWindow>();

			
			// Resolve the catalog for the chosen type
			// -------- LEFT: Top-level categories --------
			if (imgui_BeginChild("TopLevelCats", leftW, listH, 1, 0))
			{
				try
				{
					imgui_TextColored(0.9f, 0.95f, 1.0f, 1.0f, "Categories");
					var cats = currentCatalog.Keys; //keys are already sorted in a sorted dictionary
					foreach (var c in cats)
					{
					

						bool selectedCategory = string.Equals(state.SelectedCategory, c, StringComparison.OrdinalIgnoreCase);
						if (imgui_Selectable(c, selectedCategory))
						{
							state.SelectedCategory = c;
							state.SelectedSubCategory = string.Empty; // reset mid level on cat change
							state.SelectedCategorySpell = null; // Clear selection when category changes
						}
					}
				}
				finally
				{
					imgui_EndChild();
				}
			}
		}
		private static void RenderAddFromCatalogModel_MiddlePanel(float middleW, float listH,SortedDictionary<string, SortedDictionary<string, List<E3Spell>>> currentCatalog)
		{
			var state = _state.GetState<State_CatalogWindow>();

			// -------- MIDDLE: Subcategory dropdown + Entries --------
			if (imgui_BeginChild("MiddlePanel", middleW, listH, 1, 0))
			{	try
				{	// Subcategory dropdown selector (not shown for Items)
					float entriesHeight = listH;
					if (state.CurrentAddType != AddType.Items)
					{
						imgui_TextColored(0.9f, 0.95f, 1.0f, 1.0f, "Subcategory:");
						imgui_SameLine();

						string comboLabel = string.IsNullOrEmpty(state.SelectedSubCategory) ? "(All)" : state.SelectedSubCategory;
						if (!string.IsNullOrEmpty(state.SelectedCategory) && currentCatalog.TryGetValue(state.SelectedCategory, out var submap))
						{
							if (imgui_BeginCombo("##subcategory", comboLabel, 0))
							{
								// "(All)" option to show all entries in the category
								bool selAll = string.IsNullOrEmpty(state.SelectedSubCategory);
								if (imgui_Selectable("(All)##SubAll", selAll))
								{
									state.SelectedSubCategory = string.Empty;
								}

								var subs = submap.Keys.ToList();
								subs.Sort(StringComparer.OrdinalIgnoreCase);
								foreach (var sc in subs)
								{
									// Find the highest level spell in this subcategory for icon display
									int iconIndex = -1;
									if (submap.TryGetValue(sc, out var spellList) && spellList.Count > 0)
									{
										var highestSpell = spellList.OrderByDescending(s => s.Level).FirstOrDefault();
										if (highestSpell != null)
										{
											iconIndex = highestSpell.SpellIcon;
										}
									}

									// Draw icon if available
									if (iconIndex >= 0)
									{
										imgui_DrawSpellIconByIconIndex(iconIndex, 20.0f);
										imgui_SameLine();
									}

									bool sel = string.Equals(state.SelectedSubCategory, sc, StringComparison.OrdinalIgnoreCase);
									if (imgui_Selectable($"{sc}##Sub_{sc}", sel))
									{
										state.SelectedSubCategory = sc;
									}
								}
								EndComboSafe();
							}
						}
						else
						{
							imgui_SameLine();
							imgui_TextColored(0.7f, 0.7f, 0.7f, 1.0f, "(Select a category)");
						}

						imgui_Separator();
						entriesHeight = listH - 50f; // Reserve space for dropdown above
					}

					// Entries list
					if (imgui_BeginChild("EntryList", middleW - 10f, entriesHeight, 0, 0))
					{
						try
						{
							imgui_TextColored(0.9f, 0.95f, 1.0f, 1.0f, "Entries");

							IEnumerable<E3Spell> entries = Enumerable.Empty<E3Spell>();
							if (!string.IsNullOrEmpty(state.SelectedCategory) && currentCatalog.TryGetValue(state.SelectedCategory, out var submap2))
							{
								if (!string.IsNullOrEmpty(state.SelectedSubCategory) && submap2.TryGetValue(state.SelectedSubCategory, out var l))
									entries = l;
								else
									entries = submap2.Values.SelectMany(x => x);
							}

							string filter = (state.Filter ?? string.Empty).Trim();
							if (filter.Length > 0)
								entries = entries.Where(e => e.Name.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0);

							// stable ordering
							entries = entries.OrderByDescending(e => e.Level)
											 .ThenBy(e => e.Name, StringComparer.OrdinalIgnoreCase);

							int i = 0;
							foreach (var e in entries)
							{
								string uid = $"{state.CurrentAddType}_{state.SelectedCategory}_{state.SelectedSubCategory}_{i}";

								// Icon
								imgui_DrawSpellIconByIconIndex(e.SpellIcon, 30.0f);
								imgui_SameLine();

								// Selectable entry with level and name
								bool isSelected = state.SelectedCategorySpell != null && string.Equals(state.SelectedCategorySpell.Name, e.Name, StringComparison.OrdinalIgnoreCase);
								if (imgui_Selectable($"[{e.Level}] {e.Name}##{uid}", isSelected))
								{
									state.SelectedCategorySpell = e;
								}
								i++;
							}

							if (i == 0) imgui_Text("No entries found");
						}
						finally { imgui_EndChild(); }
					}
					
				}
				finally { imgui_EndChild();}
				
			}
			
		}
		private static void ReaderAddFromCatalogModal_RightPanel(float rightW, float listH, SectionData selectedSection)
		{
			var state = _state.GetState<State_CatalogWindow>();
			var mainWindowState = _state.GetState<State_MainWindow>();
			// -------- RIGHT: Info Panel --------
			if (imgui_BeginChild("InfoPanel", rightW, listH, 1, 0))
			{
				try
				{
					imgui_TextColored(0.9f, 0.95f, 1.0f, 1.0f, "Info");
					// Add button on same line as Info header, right-aligned
					if (state.SelectedCategorySpell != null)
					{
						string addLabel = (state.Mode == CatalogMode.BardSong) ? "Use" : "Add";
						float buttonWidth = 60f;
						imgui_SameLine(rightW - buttonWidth - 10f);
						if (imgui_ButtonEx($"{addLabel}##add_selected", buttonWidth, 0))
						{
							if (state.Mode == CatalogMode.BardSong)
							{
								ApplyBardSongSelection(state.SelectedCategorySpell.Name ?? string.Empty);
							}
							else
							{
								var kd = selectedSection?.Keys?.GetKeyData(mainWindowState.SelectedKey ?? string.Empty);
								if (kd != null)
								{
									var vals = GetValues(kd);
									string v = (state.SelectedCategorySpell.Name ?? string.Empty).Trim();
									if (state.ReplaceMode && state.ReplaceIndex >= 0 && state.ReplaceIndex < vals.Count)
									{
										vals[state.ReplaceIndex] = v;
										WriteValues(kd, vals);
										_cfgPendingValueSelection = state.ReplaceIndex;
										state.ReplaceMode = false;
										state.ReplaceIndex = -1;
									}
									else if (!vals.Contains(v, StringComparer.OrdinalIgnoreCase))
									{
										vals.Add(v);
										WriteValues(kd, vals);
										_cfgPendingValueSelection = vals.Count - 1;
									}
								}
							}
						}
					}

					imgui_Separator();

					if (state.SelectedCategorySpell != null)
					{
						// Display name and icon
						imgui_DrawSpellIconByIconIndex(state.SelectedCategorySpell.SpellIcon, 40.0f);
						imgui_SameLine();
						imgui_TextColored(0.95f, 0.85f, 0.35f, 1.0f, state.SelectedCategorySpell.Name ?? string.Empty);

						// Show additional info (mana, cast time, recast, etc.)
						RenderSpellAdditionalInfo(state.SelectedCategorySpell);

						// Show description if available
						if (!string.IsNullOrEmpty(state.SelectedCategorySpell.Description))
						{
							imgui_Separator();
							imgui_TextColored(0.75f, 0.85f, 1.0f, 1.0f, "Description");
							imgui_TextWrapped(state.SelectedCategorySpell.Description);
						}
					}
					else
					{
						imgui_TextColored(0.7f, 0.7f, 0.7f, 1.0f, "Select an entry to view details.");
					}
				}
				finally { imgui_EndChild(); }
			}
		}
		private static void RenderAddFromCatalogModal(IniData pd, SectionData selectedSection)
		{
			var state = _state.GetState<State_CatalogWindow>();

			// Set initial size only on first use - window is resizable and remembers user's size
			imgui_SetNextWindowSizeWithCond(900f, 600f, (int)ImGuiCond.FirstUseEver); // ImGuiCond_FirstUseEver = 4
			if (imgui_Begin(_state.WinName_AddModal, (int)ImGuiWindowFlags.ImGuiWindowFlags_NoDocking))
			{
				try
				{
					float listH = imgui_GetContentRegionAvailY() - 120f; // Reserve space for header/footer
					float leftW, middleW, rightW;
					var currentCatalog = GetCatalogByType(state.CurrentAddType);

					RenderAddFromCatalogModal_CalculateAddType(state.CurrentAddType, out leftW, out middleW, out rightW);
					RenderAddFromCatalogModal_Header();
					// Show catalog status if loading
					if (_state.State_CatalogLoading)
					{
						imgui_SameLine();
						imgui_TextColored(0.9f, 0.9f, 0.4f, 1.0f, _state.Status_CatalogRequest.Replace("Loading catalogs", "Loading"));
					}
					imgui_Separator();
					RenderAddFromCatalogModel_LeftPanel(leftW, listH, currentCatalog);
					imgui_SameLine();
					RenderAddFromCatalogModel_MiddlePanel(middleW, listH, currentCatalog);
					imgui_SameLine();
					ReaderAddFromCatalogModal_RightPanel(rightW, listH, selectedSection);
					imgui_Separator();
					if (imgui_Button("Close"))
					{
						_state.Show_AddModal = false;
					}
				}
				finally
				{
					imgui_End();
				}
			}
		}

		private static SortedDictionary<string, SortedDictionary<string, List<E3Spell>>> GetCatalogByType(AddType t)
		{
			lock (_dataLock)
			{
				switch (t)
				{
					case AddType.AAs: return _catalog_AA;
					case AddType.Discs: return _catalog_Disc;
					case AddType.Skills: return _catalog_Skills;
					case AddType.Items: return _catalog_Items;
					case AddType.Spells:
					default: return _catalog_Spells;
				}
			}
		}
		private static (SortedDictionary<string, SortedDictionary<string, List<E3Spell>>>, string)[] _catalogLookups = new[]
		{
			(_catalog_Spells, "Spell"),
			(_catalog_AA, "AA"),
			(_catalog_Disc, "Disc"),
			(_catalog_Skills, "Skill"),
			(_catalog_Items, "Item")
		};
		// Search all catalogs for a spell/item/AA by name
		private static E3Spell FindSpellItemAAByName(string name)
		{
			if (string.IsNullOrEmpty(name)) return null;

			// Search all catalog types for an exact match
			foreach (var (catalog, type) in _catalogLookups)
			{
				foreach (var categoryKvp in catalog)
				{
					foreach (var subCategoryKvp in categoryKvp.Value)
					{
						var match = subCategoryKvp.Value.FirstOrDefault(spell =>
							string.Equals(spell.Name, name, StringComparison.OrdinalIgnoreCase));
						if (match != null)
						{
							// Set the cast type if not already set
							if (string.IsNullOrEmpty(match.CastType)) match.CastType = type;
							return match;
						}
					}
				}
			}
			return null;
		}


		// Helper: format milliseconds as seconds, or minutes+seconds over 60s
		private static string FormatMsSmart(int ms)
		{
			if (ms <= 0) return string.Empty;
			double totalSec = ms / 1000.0;
			if (totalSec < 60.0)
			{
				return totalSec < 10 ? totalSec.ToString("0.##") + "s" : totalSec.ToString("0.#") + "s";
			}
			int m = (int)(totalSec / 60.0);
			double rs = totalSec - m * 60;
			if (rs < 0.5) return m.ToString() + "m";
			return m.ToString() + "m " + rs.ToString("0.#") + "s";
		}

		private static string FormatSecondsSmart(double seconds)
		{
			if (seconds <= 0) return string.Empty;
			if (seconds < 1.0)
			{
				return seconds.ToString("0.###") + " s";
			}
			if (seconds < 60.0)
			{
				return seconds < 10.0 ? seconds.ToString("0.##") + " s" : seconds.ToString("0.#") + " s";
			}
			int minutes = (int)(seconds / 60.0);
			double remainder = seconds - minutes * 60.0;
			if (remainder < 0.5)
			{
				return minutes + "m";
			}
			return minutes + "m " + remainder.ToString("0.#") + "s";
		}

		private static readonly Regex _inlineNumberRegex = new Regex(@"\b\d{4,}\b", RegexOptions.Compiled);

		private static string FormatWithSeparators(long value)
		{
			return value.ToString("N0", CultureInfo.InvariantCulture);
		}

		private static string FormatInlineNumbers(string input)
		{
			if (string.IsNullOrEmpty(input)) return input;
			return _inlineNumberRegex.Replace(input, m =>
			{
				if (long.TryParse(m.Value, out var numeric))
				{
					return FormatWithSeparators(numeric);
				}
				return m.Value;
			});
		}

		private static void RenderSpellAdditionalInfo(E3Spell spellInfo)
		{
			if (spellInfo == null) return;

			bool hasMana = spellInfo.Mana > 0;
			string castTimeText = FormatSecondsSmart(spellInfo.CastTime);
			bool hasCastTime = !string.IsNullOrEmpty(castTimeText);
			string recastText = FormatMsSmart(spellInfo.Recast);
			bool hasRecast = !string.IsNullOrEmpty(recastText);
			bool hasGem = spellInfo.SpellGem > 0;
			var slotEffects = spellInfo.SpellEffects ?? new List<string>();
			bool hasSlots = slotEffects.Any(effect => !string.IsNullOrWhiteSpace(effect));

			if (!hasMana && !hasCastTime && !hasRecast && !hasGem && !hasSlots)
			{
				return;
			}

			imgui_Separator();

			if (hasMana || hasCastTime || hasRecast || hasGem)
			{
				imgui_TextColored(0.75f, 0.9f, 1.0f, 1.0f, "Spell Details");
				if (hasMana)
				{
					imgui_Text($"Mana: {FormatWithSeparators(spellInfo.Mana)}");
				}
				if (hasCastTime)
				{
					imgui_Text($"Cast Time: {castTimeText}");
				}
				if (hasRecast)
				{
					imgui_Text($"Recast: {recastText}");
				}
				if (hasGem)
				{
					imgui_Text($"Gem Slot: {spellInfo.SpellGem}");
				}
			}

			if (hasMana && hasSlots || hasCastTime && hasSlots || hasRecast && hasSlots || hasGem && hasSlots)
			{
				imgui_Separator();
			}

			if (hasSlots)
			{
				imgui_TextColored(0.75f, 0.9f, 1.0f, 1.0f, "Spell Slots");
				for (int i = 0; i < slotEffects.Count; i++)
				{
					string effect = slotEffects[i];
					if (string.IsNullOrWhiteSpace(effect)) continue;
					string formattedEffect = FormatInlineNumbers(effect);
					imgui_TextWrapped($"{formattedEffect}");
				}
			}
		}

		// Append If modal: choose an If key to append to a specific row value
		private static void RenderIfAppendModal(SectionData selectedSection)
		{
			var mainWindowState = _state.GetState<State_MainWindow>();

			bool _open_if = imgui_Begin(_state.WinName_IfAppendModal, (int)(ImGuiWindowFlags.ImGuiWindowFlags_AlwaysAutoResize | ImGuiWindowFlags.ImGuiWindowFlags_NoDocking));
			if (_open_if)
			{
				if (!string.IsNullOrEmpty(_cfgIfAppendStatus)) imgui_Text(_cfgIfAppendStatus);
				float h = 300f; float w = 520f;
				if (imgui_BeginChild("IfList", w, h, 1, 0))
				{
					var list = _cfgIfAppendCandidates ?? new List<string>();
					int i = 0;
					foreach (var key in list)
					{
						string label = $"{key}##ifkey_{i}";
						if (imgui_Selectable(label, false))
						{
							try
							{
								var kd = selectedSection?.Keys?.GetKeyData(mainWindowState.SelectedKey ?? string.Empty);
								if (kd != null && _cfgIfAppendRow >= 0)
								{
									var vals = GetValues(kd);
									if (_cfgIfAppendRow < vals.Count)
									{
										string updated = AppendIfToken(vals[_cfgIfAppendRow] ?? string.Empty, key);
										vals[_cfgIfAppendRow] = updated;
										WriteValues(kd, vals);
									}
								}
							}
							catch { }
							_state.Show_IfAppendModal = false;
							break;
						}
						i++;
					}
				}
				imgui_EndChild();
				if (imgui_Button("Close")) _state.Show_IfAppendModal = false;
			}
			imgui_End();
			if (!_open_if) _state.Show_IfAppendModal = false;
		}

		private static void RenderThemeSettingsModal()
		{
			bool modalOpen = imgui_Begin(_state.WinName_ThemeSettings, (int)(ImGuiWindowFlags.ImGuiWindowFlags_AlwaysAutoResize | ImGuiWindowFlags.ImGuiWindowFlags_NoDocking));
			if (modalOpen)
			{
				imgui_TextColored(0.95f, 0.85f, 0.35f, 1.0f, "UI Theme Selection");
				imgui_Separator();

				// Theme preview and selection using selectable buttons
				string[] themeNames = { "Dark Teal (Default)", "Dark Blue", "Dark Purple", "Dark Orange", "Dark Green" };
				UITheme[] themeValues = { UITheme.DarkTeal, UITheme.DarkBlue, UITheme.DarkPurple, UITheme.DarkOrange, UITheme.DarkGreen };

				imgui_Text("Select Theme:");
				imgui_Separator();

				for (int i = 0; i < themeNames.Length; i++)
				{
					bool isSelected = (_currentTheme == themeValues[i]);

					// Use selectable for theme selection (acts like radio button)
					if (imgui_Selectable(themeNames[i], isSelected))
					{
						_currentTheme = themeValues[i];
						// Save theme to character INI
						if (E3.CharacterSettings != null)
						{
							E3.CharacterSettings.UITheme_E3Config = _currentTheme.ToString();
							E3.CharacterSettings.SaveData();
						}
					}

					// Show theme preview as colored text on the same line
					if (isSelected)
					{
						imgui_SameLine();
						float[] previewColors = GetThemePreviewColor(themeValues[i]);
						imgui_TextColored(previewColors[0], previewColors[1], previewColors[2], previewColors[3], "<-- Current");
					}
				}

				imgui_Separator();

				// Theme info
				imgui_TextColored(0.8f, 0.9f, 1.0f, 1.0f, "Theme Info:");
				string themeDescription = GetThemeDescription(_currentTheme);
				imgui_TextWrapped(themeDescription);

				imgui_Separator();

				// Rounding controls
				imgui_Text("Corner Rounding:");
				imgui_SameLine();
				if (string.IsNullOrEmpty(_roundingBuf))
				{
					_roundingBuf = _rounding.ToString("0.0", CultureInfo.InvariantCulture);
				}
				imgui_SetNextItemWidth(100f);
				string roundingInputId = $"##rounding_value_{_roundingVersion}";
				if (imgui_InputText(roundingInputId, _roundingBuf))
				{
					_roundingBuf = imgui_InputText_Get(roundingInputId) ?? string.Empty;
					if (float.TryParse(_roundingBuf, NumberStyles.Float, CultureInfo.InvariantCulture, out var rv))
					{
						_rounding = Math.Max(0f, Math.Min(12f, rv));
						// Save rounding to character INI
						if (E3.CharacterSettings != null)
						{
							E3.CharacterSettings.UITheme_Rounding = _rounding;
							E3.CharacterSettings.SaveData();
						}
					}
				}
				imgui_SameLine();
				imgui_Text($"({_rounding.ToString("0.0", CultureInfo.InvariantCulture)})");
				imgui_SameLine();
				if (imgui_Button("-"))
				{
					_rounding = Math.Max(0f, _rounding - 1f);
					_roundingBuf = _rounding.ToString("0.0", CultureInfo.InvariantCulture);
					_roundingVersion++;
					SaveRoundingToSettings();
				}
				imgui_SameLine();
				if (imgui_Button("+"))
				{
					_rounding = Math.Min(12f, _rounding + 1f);
					_roundingBuf = _rounding.ToString("0.0", CultureInfo.InvariantCulture);
					_roundingVersion++;
					SaveRoundingToSettings();
				}

				string roundingString = _rounding.ToString("0.0", CultureInfo.InvariantCulture);
				// Presets
				imgui_Text("Presets:");
				if (imgui_Button("0")) { _rounding = 0f; _roundingBuf = roundingString; _roundingVersion++; SaveRoundingToSettings(); }
				imgui_SameLine();
				if (imgui_Button("3")) { _rounding = 3f; _roundingBuf = roundingString; _roundingVersion++; SaveRoundingToSettings(); }
				imgui_SameLine();
				if (imgui_Button("6")) { _rounding = 6f; _roundingBuf = roundingString; _roundingVersion++; SaveRoundingToSettings(); }
				imgui_SameLine();
				if (imgui_Button("9")) { _rounding = 9f; _roundingBuf = roundingString; _roundingVersion++; SaveRoundingToSettings(); }
				imgui_SameLine();
				if (imgui_Button("12")) { _rounding = 12f; _roundingBuf = roundingString; _roundingVersion++; SaveRoundingToSettings(); }

				imgui_Separator();

				// Preview the accent color
				float[] currentAccentColor = GetThemePreviewColor(_currentTheme);
				imgui_TextColored(currentAccentColor[0], currentAccentColor[1], currentAccentColor[2], currentAccentColor[3], "Preview: This text shows the accent color");

				imgui_Separator();

				// Close button
				if (imgui_Button("Close"))
				{
					_state.Show_ThemeSettings = false;
				}
			}
			imgui_End();

			if (!modalOpen)
			{
				_state.Show_ThemeSettings = false;
			}
		}

		private static void SaveRoundingToSettings()
		{
			if (E3.CharacterSettings != null)
			{
				E3.CharacterSettings.UITheme_Rounding = _rounding;
				E3.CharacterSettings.SaveData();
			}
		}

		private static void RenderAllPlayersView()
		{
			var mainWindowState = _state.GetState<State_MainWindow>();
			var allPlayerState = _state.GetState<State_AllPlayers>();

			imgui_Text("All Players View");
			imgui_Separator();

			var pd = GetActiveCharacterIniData();
			if (pd == null || pd.Sections == null || string.IsNullOrEmpty(mainWindowState.SelectedSection) || string.IsNullOrEmpty(mainWindowState.SelectedKey))
			{
				imgui_Text("Select a section and key in the Config Editor first.");
				return;
			}

			imgui_Text($"Viewing: [{mainWindowState.SelectedSection}] -> [{mainWindowState.SelectedKey}]");
			imgui_SameLine();
			if (imgui_Button("Refresh")) _state.Request_AllplayersRefresh = true;

			imgui_Separator();

			if (imgui_BeginChild("AllPlayersList", 0, 0, 1, 0))
			{
				try
				{
					float outerW = Math.Max(720f, imgui_GetContentRegionAvailX()); // keep it roomy
																				   // Columns: Toon | Value (editable) | Actions
					if (imgui_BeginTable("E3AllPlayersTable", 3, 0, outerW, 0))
					{
						try
						{
							imgui_TableSetupColumn("Toon", 0, 180f);
							imgui_TableSetupColumn("Value", 0, Math.Max(260f, outerW - (180f + 100f))); // leave room for Save
							imgui_TableSetupColumn("Actions", 0, 100f);
							imgui_TableHeadersRow();

							lock (allPlayerState.DataLock)
							{
								foreach (var row in allPlayerState.Data_Rows)
								{
									string toon = row.Key ?? string.Empty;

									if (!allPlayerState.Data_Edit.ContainsKey(toon))
										allPlayerState.Data_Edit[toon] = row.Value ?? string.Empty;

									imgui_TableNextRow();

									// Toon
									imgui_TableNextColumn();
									imgui_Text(toon);

									// Value (editable)
									imgui_TableNextColumn();
									string currentValue = allPlayerState.Data_Edit[toon];
									bool isBool = string.Equals(currentValue, "true", StringComparison.OrdinalIgnoreCase) || string.Equals(currentValue, "false", StringComparison.OrdinalIgnoreCase)
										|| string.Equals(currentValue, "on", StringComparison.OrdinalIgnoreCase) || string.Equals(currentValue, "off", StringComparison.OrdinalIgnoreCase);

									if (isBool)
									{
										if (BeginComboSafe($"##value_{toon}", currentValue))
										{
											if (imgui_Selectable("True", string.Equals(currentValue, "True", StringComparison.OrdinalIgnoreCase)))
											{
												allPlayerState.Data_Edit[toon] = "True";
											}
											if (imgui_Selectable("False", string.Equals(currentValue, "False", StringComparison.OrdinalIgnoreCase)))
											{
												allPlayerState.Data_Edit[toon] = "False";
											}
											if (imgui_Selectable("On", string.Equals(currentValue, "On", StringComparison.OrdinalIgnoreCase)))
											{
												allPlayerState.Data_Edit[toon] = "On";
											}
											if (imgui_Selectable("Off", string.Equals(currentValue, "Off", StringComparison.OrdinalIgnoreCase)))
											{
												allPlayerState.Data_Edit[toon] = "Off";
											}
											EndComboSafe();
										}
									}
									else
									{
										string inputId = $"##edit_{toon}";
										if (imgui_InputText(inputId, currentValue))
										{
											allPlayerState.Data_Edit[toon] = imgui_InputText_Get(inputId) ?? string.Empty;
										}
									}

									// Actions
									imgui_TableNextColumn();
									if (imgui_Button($"Save##{row.Key}"))
									{
										string newValue = allPlayerState.Data_Edit[row.Key] ?? string.Empty;

										if (TrySaveIniValueForToon(row.Key, mainWindowState.SelectedSection, mainWindowState.SelectedKey, newValue, out var err))
										{
											_log.WriteDelayed($"Saved [{mainWindowState.SelectedSection}] {mainWindowState.SelectedKey} for {row.Key}.", Logging.LogLevels.Debug);
										}
										else
										{
											_log.WriteDelayed($"Save failed for {row.Key}: {err}", Logging.LogLevels.Debug);
										}
									}
								}
							}
						}
						finally
						{
							imgui_EndTable();
						}

					}
				}
				finally
				{
					imgui_EndChild();
				}
			}
		}
		private static bool TrySaveIniValueForToon(string toon, string section, string key, string newValue, out string error)
		{
			error = null;
			if (!TryGetIniPathForToon(toon, out var iniPath))
			{
				error = $"Could not resolve ini path for '{toon}'.";
				return false;
			}

			try
			{
				var parser = E3Core.Utility.e3util.CreateIniParser();       // you already use this elsewhere
				var data = parser.ReadFile(iniPath);                         // IniParser.Model.IniData
				if (!data.Sections.ContainsSection(section))
					data.Sections.AddSection(section);
				data[section][key] = newValue ?? string.Empty;               // simplest way to set a value
				parser.WriteFile(iniPath, data);                             // persist to disk

				return true;
			}
			catch (Exception ex)
			{
				error = ex.Message;
				return false;
			}
		}

		// Reads a single INI value for a toon. Returns true if the toon/path exists and read succeeded.
		private static bool TryReadIniValueForToon(string toon, string section, string key, out string value)
		{
			value = string.Empty;
			try
			{
				if (!TryGetIniPathForToon(toon, out var iniPath))
					return false;

				var parser = E3Core.Utility.e3util.CreateIniParser();
				var data = parser.ReadFile(iniPath);
				if (!data.Sections.ContainsSection(section))
					return true; // file exists but section missing -> empty

				value = data[section][key] ?? string.Empty;
				return true;
			}
			catch
			{
				value = string.Empty;
				return false;
			}
		}


		private static void EnsureConfigEditorInit()
		{
			if (_cfg_Inited) return;
			_cfg_Inited = true;
			BuildConfigSectionOrder();
		}
		static List<String> _configSectionOrderDefault = new List<string>() { "Misc", "Assist Settings", "Nukes", "Debuffs", "DoTs on Assist", "DoTs on Command", "Heals", "Buffs", "Melee Abilities", "Burn", "CommandSets", "Pets", "Ifs" };
		static List<String> _configSectionOrderNecro = new List<string>() { "DoTs on Assist", "DoTs on Command", "Debuffs", "Pets", "Burn", "CommandSets", "Ifs", "Assist Settings", "Buffs" };
		static List<String> _configSectionOrderSK = new List<string>() { "Nukes", "Assist Settings", "Buffs", "DoTs on Assist", "DoTs on Command", "Debuffs", "Pets", "Burn", "CommandSets", "Ifs" };
		static List<String> _configSectionOrderBard = new List<string>() { "Bard", "Melee Abilities", "Burn", "CommandSets", "Ifs", "Assist Settings", "Buffs" };

		private static void BuildConfigSectionOrder()
		{
			var mainWindowState = _state.GetState<State_MainWindow>();
			var pd = GetActiveCharacterIniData();
			if (pd?.Sections == null) return;

			// Class-prioritized defaults similar to e3config
			var cls = E3.CurrentClass;
			List<String> currentOrder = _configSectionOrderDefault;
			if (cls.ToString().Equals("Bard", StringComparison.OrdinalIgnoreCase))
			{
				currentOrder = _configSectionOrderBard;
			}
			else if (cls.ToString().Equals("Necromancer", StringComparison.OrdinalIgnoreCase))
			{
				currentOrder = _configSectionOrderNecro;
			}
			else if (cls.ToString().Equals("Shadowknight", StringComparison.OrdinalIgnoreCase))
			{
				currentOrder = _configSectionOrderSK;
			}
			mainWindowState.SectionsOrdered.Clear();
			// Seed ordered list with defaults that exist in the INI
			foreach (var d in currentOrder)
			{
				if (pd.Sections.ContainsSection(d)) mainWindowState.SectionsOrdered.Add(d);
			}
			// Append any remaining sections not included yet
			foreach (SectionData s in pd.Sections)
			{
				if (!mainWindowState.SectionsOrdered.Contains(s.SectionName, StringComparer.OrdinalIgnoreCase))
					mainWindowState.SectionsOrdered.Add(s.SectionName);
			}

			if (mainWindowState.SectionsOrdered.Count > 0)
			{
				if (string.IsNullOrEmpty(mainWindowState.SelectedSection) || !mainWindowState.SectionsOrdered.Contains(mainWindowState.SelectedSection, StringComparer.OrdinalIgnoreCase))
				{
					mainWindowState.SelectedSection = mainWindowState.SectionsOrdered[0];
					var section = pd.Sections.GetSectionData(mainWindowState.SelectedSection);
					mainWindowState.SelectedKey = section?.Keys?.FirstOrDefault()?.KeyName ?? string.Empty;
					mainWindowState.SelectedValueIndex = -1;
				}
			}
		}
	private static List<string> GetSectionsForDisplay()
	{	
		var mainWindowState = _state.GetState<State_MainWindow>();
		
		var search = (mainWindowState.Buffer_KeySearch ?? string.Empty).Trim();
		if (string.IsNullOrWhiteSpace(search))
		{
			return mainWindowState.SectionsOrdered;
		}
		//lets look for the data based off search
		var matches = new List<String>();
		var pd = GetActiveCharacterIniData();
		if (pd == null) return matches;
		
		foreach (var section in mainWindowState.SectionsOrdered)
		{
			// Check if section name matches
			if (section.IndexOf(search, 0, StringComparison.OrdinalIgnoreCase) > -1)
			{
				matches.Add(section);
				continue;
			}
			
			// Check if any key in this section matches
			var secData = pd.Sections.GetSectionData(section);
			if (secData?.Keys != null)
			{
				foreach (var key in secData.Keys)
				{
					if (key.KeyName.IndexOf(search, 0, StringComparison.OrdinalIgnoreCase) > -1)
					{
						matches.Add(section);
						break;
					}
				}
			}
		}
		return matches;
	}
		
		private static string GetActiveSettingsPath()
		{
			switch (_activeSettingsTab)
			{
				case SettingsTab.General:
					if (E3.GeneralSettings != null && !string.IsNullOrEmpty(E3.GeneralSettings._fileLastModifiedFileName))
						return E3.GeneralSettings._fileLastModifiedFileName;
					return E3Core.Settings.BaseSettings.GetSettingsFilePath("General Settings.ini");
				case SettingsTab.Advanced:
					var adv = E3Core.Settings.BaseSettings.GetSettingsFilePath("Advanced Settings.ini");
					if (!string.IsNullOrEmpty(E3Core.Settings.BaseSettings.CurrentSet)) adv = adv.Replace(".ini", "_" + E3Core.Settings.BaseSettings.CurrentSet + ".ini");
					return adv;
				case SettingsTab.Character:
				default:
					var state = _state.GetState<State_MainWindow>();
					if (string.IsNullOrEmpty(state.CurrentINIFileNameFull))
					{
						var currentPath = GetCurrentCharacterIniPath();
						state.CurrentINIFileNameFull = currentPath;
					}
					return state.CurrentINIFileNameFull;
			}
		}
		// Ifs sample import helpers and modal
		private static string ResolveSampleIfsPath()
		{
			var dirs = new List<string>();
			try
			{
				string cfg = GetActiveSettingsPath();
				if (!string.IsNullOrEmpty(cfg))
				{
					var dir = Path.GetDirectoryName(cfg);
					if (!string.IsNullOrEmpty(dir)) dirs.Add(dir);
				}
			}
			catch { }
			try
			{
				string botIni = GetCurrentCharacterIniPath();
				if (!string.IsNullOrEmpty(botIni))
				{
					var botDir = Path.GetDirectoryName(botIni);
					if (!string.IsNullOrEmpty(botDir)) dirs.Add(botDir);
				}
			}
			catch { }
			try { dirs.Add(AppDomain.CurrentDomain.BaseDirectory ?? string.Empty); } catch { }
			dirs.Add(Path.Combine(AppDomain.CurrentDomain.BaseDirectory ?? string.Empty, "E3Next"));
			dirs.Add(Directory.GetCurrentDirectory());
			dirs.Add(Path.Combine(Directory.GetCurrentDirectory(), "E3Next"));

			string[] names = new[] { "sample ifs", "sample ifs.txt", "Sample Ifs.txt", "sample_ifs.txt" };
			foreach (var d in dirs)
			{
				if (string.IsNullOrEmpty(d)) continue;
				foreach (var n in names)
				{
					try
					{
						var p = Path.Combine(d, n);
						if (File.Exists(p)) return p;
					}
					catch { }
				}
				try
				{
					foreach (var f in Directory.EnumerateFiles(d, "*", SearchOption.TopDirectoryOnly))
					{
						string fn = Path.GetFileNameWithoutExtension(f) ?? string.Empty;
						if (fn.Equals("sample ifs", StringComparison.OrdinalIgnoreCase)) return f;
					}
				}
				catch { }
			}
			return string.Empty;
		}

		private static void LoadSampleIfsForModal()
		{
			_cfgIfSampleLines.Clear();
			_cfgIfSampleStatus = string.Empty;
			try
			{
				string sample = ResolveSampleIfsPath();
				if (string.IsNullOrEmpty(sample)) { _cfgIfSampleStatus = "Sample file not found."; return; }
				_cfgIfSampleStatus = "Loaded: " + Path.GetFileName(sample);
				int added = 0;
				foreach (var raw in File.ReadAllLines(sample))
				{
					var line = (raw ?? string.Empty).Trim();
					if (line.Length == 0) continue;
					if (line.StartsWith("#") || line.StartsWith(";")) continue;
					string key = string.Empty; string val = string.Empty;
					int eq = line.IndexOf('=');
					if (eq > 0)
					{
						key = (line.Substring(0, eq).Trim());
						val = (line.Substring(eq + 1).Trim());
					}
					else
					{
						int colon = line.IndexOf(':');
						int dash = line.IndexOf('-');
						int pos = -1;
						if (colon > 0) pos = colon; else if (dash > 0) pos = dash;
						if (pos > 0)
						{
							key = line.Substring(0, pos).Trim();
							val = line.Substring(pos + 1).Trim();
						}
						else
						{
							key = line;
							val = string.Empty;
						}
					}
					if (!string.IsNullOrEmpty(key))
					{
						_cfgIfSampleLines.Add(new KeyValuePair<string, string>(key, val));
						added++;
					}
				}
				if (added == 0) _cfgIfSampleStatus = "No entries found in sample file.";
				if (_cfgIfSampleLines.Count == 0) _cfgIfSampleStatus = "No entries found in sample file.";
			}
			catch (Exception ex)
			{
				_cfgIfSampleStatus = "Error reading sample IFs: " + (ex.Message ?? "error");
			}
		}

		private static bool AddIfToActiveIni(string key, string value)
		{
			var mainWindowState  = _state.GetState<State_MainWindow>();
			try
			{
				var pd = GetActiveCharacterIniData();
				if (pd == null) return false;
				var section = pd.Sections.GetSectionData("Ifs");
				if (section == null)
				{
					pd.Sections.AddSection("Ifs");
					section = pd.Sections.GetSectionData("Ifs");
				}
				if (section == null) return false;
				string baseKey = key ?? string.Empty;
				if (string.IsNullOrWhiteSpace(baseKey)) return false;
				string unique = baseKey;
				int idx = 1;
				while (section.Keys.ContainsKey(unique)) { unique = baseKey + " (" + idx.ToString() + ")"; idx++; if (idx > 1000) break; }
				if (!section.Keys.ContainsKey(unique))
				{
					section.Keys.AddKey(unique, value ?? string.Empty);
					mainWindowState.ConfigIsDirty = true;
					mainWindowState.SelectedSection = "Ifs";
					mainWindowState.SelectedKey = unique;
					mainWindowState.SelectedValueIndex = -1;
					return true;
				}
				return false;
			}
			catch { return false; }
		}

		private static bool AddBurnToActiveIni(string key, string value)
		{
			var mainWindowState = _state.GetState<State_MainWindow>();

			try
			{
				var pd = GetActiveCharacterIniData();
				if (pd == null) return false;
				var section = pd.Sections.GetSectionData("Burn");
				if (section == null)
				{
					pd.Sections.AddSection("Burn");
					section = pd.Sections.GetSectionData("Burn");
				}
				if (section == null) return false;
				string baseKey = key ?? string.Empty;
				if (string.IsNullOrWhiteSpace(baseKey)) return false;
				string unique = baseKey;
				int idx = 1;
				while (section.Keys.ContainsKey(unique)) { unique = baseKey + " (" + idx.ToString() + ")"; idx++; if (idx > 1000) break; }
				if (!section.Keys.ContainsKey(unique))
				{
					section.Keys.AddKey(unique, value ?? string.Empty);
					mainWindowState.ConfigIsDirty = true;
					mainWindowState.SelectedSection = "Burn";
					mainWindowState.SelectedKey = unique;
					mainWindowState.SelectedValueIndex = -1;
					return true;
				}
				return false;
			}
			catch { return false; }
		}

		private static bool DeleteKeyFromActiveIni(string sectionName, string keyName)
		{
			var mainWindowState = _state.GetState<State_MainWindow>();

			try
			{
				var pd = GetActiveCharacterIniData();
				if (pd == null) return false;
				var section = pd.Sections.GetSectionData(sectionName ?? string.Empty);
				if (section == null || section.Keys == null) return false;
				if (!section.Keys.ContainsKey(keyName)) return false;
				section.Keys.RemoveKey(keyName);
				mainWindowState.ConfigIsDirty = true;
				mainWindowState.SelectedValueIndex = -1;
				InvalidateSpellEditState();
				// Pick a new selected key if any remain
				var nextKey = section.Keys.FirstOrDefault()?.KeyName ?? string.Empty;
				mainWindowState.SelectedKey = nextKey ?? string.Empty;
				return true;
			}
			catch { return false; }
		}

		private static void RenderIfsSampleModal()
		{
			bool _open_ifs = imgui_Begin(_state.WinName_IfSampleModal, (int)(ImGuiWindowFlags.ImGuiWindowFlags_AlwaysAutoResize | ImGuiWindowFlags.ImGuiWindowFlags_NoDocking));
			if (_open_ifs)
			{
				if (!string.IsNullOrEmpty(_cfgIfSampleStatus)) imgui_Text(_cfgIfSampleStatus);
				float h = 300f; float w = 640f;
				if (imgui_BeginChild("IfsSampleList", w, h, 1, 0))
				{
					for (int i = 0; i < _cfgIfSampleLines.Count; i++)
					{
						var kv = _cfgIfSampleLines[i];
						string display = string.IsNullOrEmpty(kv.Value) ? kv.Key : (kv.Key + " = " + kv.Value);
						if (imgui_Selectable($"{display}##IF_{i}", false))
						{
							AddIfToActiveIni(kv.Key, kv.Value);
						}
					}
				}
				imgui_EndChild();
				imgui_SameLine();
				if (imgui_Button("Import All"))
				{
					int cnt = 0;
					for (int i = 0; i < _cfgIfSampleLines.Count; i++) { var kv = _cfgIfSampleLines[i]; if (AddIfToActiveIni(kv.Key, kv.Value)) cnt++; }
					_cfgIfSampleStatus = cnt > 0 ? ($"Imported {cnt} If(s)") : "No new If's to import.";
				}
				imgui_SameLine();
				if (imgui_Button("Close")) { _state.Show_IfSampleModal = false;  }
			}
			imgui_End();
		}



		private static IniData GetActiveCharacterIniData()
		{
			var mainWindowState = _state.GetState<State_MainWindow>();

			return mainWindowState.CurrentINIData;
		}
		private static string GetCurrentCharacterIniPath()
		{
			string returnValue = E3.CharacterSettings._fileName;
			if (E3.CharacterSettings != null && !string.IsNullOrEmpty(E3.CharacterSettings._fileName))
			{
				return returnValue;

			}
			var name = E3.CurrentName ?? string.Empty;
			var server = E3.ServerName ?? string.Empty;
			var klass = E3.CurrentClass.ToString();
			returnValue = E3Core.Settings.BaseSettings.GetBoTFilePath(name, server, klass);
			return returnValue;
		}

		private static Int64 _iniFileScanInterval = 3000;
		private static Int64 _iniFileScanTimeStamp = 0;
		private static void ScanCharIniFilesIfNeeded()
		{
			if (!e3util.ShouldCheck(ref _iniFileScanTimeStamp, _iniFileScanInterval))
			{
				return;
			}
			var dir = BaseSettings.GetBotPath();// Path.GetDirectoryName(curPath);
			var server = E3.ServerName ?? string.Empty;
			var pattern = "*_*" + server + ".ini";
			var files = Directory.GetFiles(dir, pattern, SearchOption.TopDirectoryOnly);
			if (files == null || files.Length == 0)
				files = Directory.GetFiles(dir, "*.ini", SearchOption.TopDirectoryOnly);
			Array.Sort(files, StringComparer.OrdinalIgnoreCase);

			var mainWindowState = _state.GetState<State_MainWindow>();
			mainWindowState.IniFilesFromDisk = files;
		}
		// Safe combo wrapper for older MQ2Mono
		private static bool BeginComboSafe(string label, string preview)
		{
				return imgui_BeginCombo(label, preview, 0);
		}
		private static void EndComboSafe()
		{
			imgui_EndCombo();
		}
		

		private static string NormalizeSpellKey(string key)
		{
			if (string.IsNullOrWhiteSpace(key)) return key ?? string.Empty;
			if (_spellKeyAliasMap.TryGetValue(key, out var mapped))
			{
				return mapped;
			}
			return key;
		}

		private static string GetConfigKeyDescription(string section, string key)
		{
			if (string.IsNullOrWhiteSpace(key)) return string.Empty;

			string composite = string.IsNullOrEmpty(section) ? key : $"{section}::{key}";
			if (CharacterSettings.ConfigKeyDescriptionsBySection.TryGetValue(composite, out var desc) && !string.IsNullOrWhiteSpace(desc))
			{
				return desc;
			}

			if (CharacterSettings.ConfigKeyDescriptionsByKey.TryGetValue(key, out var generic) && !string.IsNullOrWhiteSpace(generic))
			{
				return generic;
			}

			string baseKey = key;
			int paren = baseKey.IndexOf('(');
			if (paren >= 0)
			{
				baseKey = baseKey.Substring(0, paren).Trim();
			}
			baseKey = baseKey.Replace('_', ' ').Replace('-', ' ').Trim();

			string lower = baseKey.ToLowerInvariant();

			if (lower.StartsWith("auto "))
			{
				return $"Toggle automatic {lower.Substring(5)} behaviour for this character.";
			}

			if (lower.Contains("food"))
			{
				if (lower.Contains("auto")) return "Enable automatic food and drink consumption when hunger thresholds are reached.";
				return "Item name used when the automatic food routine triggers.";
			}

			if (lower.Contains("drink"))
			{
				return "Item name used when the automatic drink routine triggers.";
			}

			if (lower.Contains("loot"))
			{
				return "Controls automatic looting behaviour for corpses.";
			}

			if (lower.Contains("pct"))
			{
				string friendly = lower.Replace("pct", "percent");
				return $"Percentage threshold used by the {friendly} setting.";
			}

			if (lower.Contains("delay"))
			{
				return "Time delay used by this entry (supports values like 1000, 10s, or 1m).";
			}

			if (lower.Contains("timer"))
			{
				return "Timer value in milliseconds controlling this behaviour.";
			}

			if (lower.Contains("gem"))
			{
				return "Spell gem slot index that should be used for this spell (1-12).";
			}

			if (lower.Contains("stack"))
			{
				return "Stacking controls for this entry (items, spells, or timing).";
			}

			if (lower.Contains("reagent"))
			{
				return "Reagent item name required for this ability.";
			}

			if (lower.Contains("range"))
			{
				return "Range or distance threshold used by this setting.";
			}

			return string.Empty;
		}

		private static void RenderDescriptionRichText(List<string> rawText)
		{
			if (rawText.Count == 0)
			{
				return;
			}
			imgui_PushTextWrapPos(imgui_GetContentRegionAvailX());
			try
			{

				float contentRegionX = imgui_GetContentRegionAvailX();
				imgui_Text("");
				float totalLineLength = 0;
				for (Int32 i = 0; i < rawText.Count; i++)
				{
					if (_inlineDescriptionColorMap.TryGetValue(rawText[i], out var color))
					{
						//next line is a color
						if ((i + 1) < rawText.Count)
						{
							i++;
							var nextline = rawText[i];
							var lengthOfText = imgui_CalcTextSizeX(nextline);

							if ((lengthOfText + totalLineLength) < contentRegionX)
							{
								imgui_SameLine(0f, 0f);
							}
							imgui_TextColored(color.r, color.g, color.b, color.a, nextline);
							totalLineLength += lengthOfText;

						}
					}
					else if (rawText[i] == "\n")
					{

						imgui_Text("");
						totalLineLength = 0;
					}
					else
					{

						var nextline = rawText[i];
						var lengthOfText = imgui_CalcTextSizeX(nextline);

						if ((lengthOfText + totalLineLength) < contentRegionX)
						{
							imgui_SameLine(0f, 0f);
							imgui_Text(nextline);
						}
						else if (totalLineLength > 0)
						{
							//means our line won't fit, see if half the line will fit?
							Int32 midpoint = nextline.Length / 2;
							string firstHalf = nextline.Substring(0, midpoint);
							string secondHalf = nextline.Substring(midpoint);
							var lengthOfHalfOfText = imgui_CalcTextSizeX(firstHalf);
							if ((lengthOfHalfOfText + totalLineLength) < contentRegionX)
							{
								imgui_SameLine(0f, 0f);
								imgui_Text(firstHalf);
								imgui_Text(secondHalf);
							}
							else
							{
								imgui_Text(nextline);
							}
						}
						else
						{
							imgui_Text(nextline);
						}
						totalLineLength += lengthOfText;
					}
				}
			}
			finally
			{
				imgui_PopTextWrapPos();
			}
		}

		private static void RenderDescriptionRichText2(List<string> rawText)
		{
			if (rawText.Count == 0)
			{
				return;
			}

			// Start on a new line
			imgui_Text("");

			// Preprocess into parts with colors
			var parts = new List<(string text, (float r, float g, float b, float a)? color)>();
			for (int i = 0; i < rawText.Count; i++)
			{
				if (_inlineDescriptionColorMap.TryGetValue(rawText[i], out var color))
				{
					if ((i + 1) < rawText.Count)
					{
						i++;
						parts.Add((rawText[i], color));
					}
				}
				else if (rawText[i] == "\n")
				{
					parts.Add(("\n", null)); // newline marker
				}
				else
				{
					parts.Add((rawText[i], null)); // default color
				}
			}

			// Render using ImGui text functions with wrapping
			float maxWidth = imgui_GetContentRegionAvailX();
			float currentWidth = 0;
			foreach (var part in parts)
			{
				if (part.text == "\n")
				{
					imgui_Text("");
					currentWidth = 0;
					continue;
				}
				float partWidth = imgui_CalcTextSizeX(part.text);
				bool useSameLine = partWidth <= maxWidth && currentWidth + partWidth <= maxWidth;
				if (useSameLine)
				{
					imgui_SameLine(0f, 0f);
				}
				if (part.color.HasValue)
				{
					imgui_PushStyleColor((int)ImGuiCol.Text, part.color.Value.r, part.color.Value.g, part.color.Value.b, part.color.Value.a);
				}
				if (partWidth > maxWidth)
				{
					imgui_TextWrapped(part.text);
				}
				else
				{
					imgui_Text(part.text);
				}
				if (part.color.HasValue)
				{
					imgui_PopStyleColor(1);
				}
				if (useSameLine)
				{
					currentWidth += partWidth;
				}
				else
				{
					currentWidth = partWidth;
				}
			}
		}
		
		private static void InvalidateSpellEditState()
		{
			_cfgSpellEditState = null;
			_cfgSpellEditSignature = string.Empty;
		}

		private static void ClearSpellEditFields(SpellValueEditState state)
		{
			if (state == null) return;
			state.BaseName = string.Empty;
			state.CastTarget = string.Empty;
			state.KeyValues.Clear();
			state.OriginalKeyNames.Clear();
			state.Flags.Clear();
			state.UnknownSegments.Clear();
			state.Enabled = true;
			state.OriginalValue = string.Empty;
		}

		private static SpellValueEditState EnsureSpellEditState(string section, string key, int index, string rawValue)
		{
			string signature = $"{section ?? string.Empty}::{key ?? string.Empty}::{index}::{rawValue ?? string.Empty}";
			if (!string.Equals(signature, _cfgSpellEditSignature, StringComparison.Ordinal))
			{
				_cfgSpellEditState = ParseSpellValueEditState(section, key, index, rawValue);
				_cfgSpellEditSignature = signature;
			}
			return _cfgSpellEditState;
		}

		private static SpellValueEditState ParseSpellValueEditState(string section, string key, int index, string rawValue)
		{
			var state = new SpellValueEditState
			{
				Section = section ?? string.Empty,
				Key = key ?? string.Empty,
				ValueIndex = index,
				OriginalValue = rawValue ?? string.Empty,
				BaseName = string.Empty,
				CastTarget = string.Empty,
				Enabled = true
			};

			string working = rawValue ?? string.Empty;
			if (working.Length == 0)
			{
				return state;
			}

			var segments = working.Split('/');
			if (segments.Length > 0)
			{
				state.BaseName = segments[0].Trim();
			}

			for (int i = 1; i < segments.Length; i++)
			{
				string segment = segments[i];
				if (string.IsNullOrWhiteSpace(segment))
				{
					continue;
				}

				segment = segment.Trim();
				int pipeIdx = segment.IndexOf('|');
				if (pipeIdx > 0)
				{
					string rawKey = segment.Substring(0, pipeIdx).Trim();
					string rawVal = segment.Substring(pipeIdx + 1).Trim();
					string canonicalKey = NormalizeSpellKey(rawKey);

					if (_spellKnownKeys.Contains(canonicalKey))
					{
						state.SetValue(canonicalKey, rawVal);
						state.RememberAlias(canonicalKey, rawKey);
					}
					else
					{
						state.UnknownSegments.Add(segment);
					}
				}
				else
				{
					if (segment.Equals("Disabled", StringComparison.OrdinalIgnoreCase))
					{
						state.Enabled = false;
						continue;
					}

					if (_spellKnownFlags.Contains(segment))
					{
						state.Flags.Add(segment);
						continue;
					}

					if (string.IsNullOrEmpty(state.CastTarget))
					{
						state.CastTarget = segment;
					}
					else
					{
						state.UnknownSegments.Add(segment);
					}
				}
			}

			return state;
		}

		private static string BuildSpellValueString(SpellValueEditState state)
		{
			if (state == null) return string.Empty;

			string baseName = state.BaseName?.Trim() ?? string.Empty;
			var segments = new List<string>();

			if (!string.IsNullOrWhiteSpace(state.CastTarget))
			{
				segments.Add(state.CastTarget.Trim());
			}

			var remaining = new Dictionary<string, string>(state.KeyValues, StringComparer.OrdinalIgnoreCase);

			foreach (var key in _spellKeyOutputOrder)
			{
				if (remaining.TryGetValue(key, out var value))
				{
					if (!string.IsNullOrWhiteSpace(value))
					{
						string outputKey = state.GetOutputKey(key);
						segments.Add($"{outputKey}|{value}");
					}
					remaining.Remove(key);
				}
			}

			foreach (var kv in remaining)
			{
				if (!string.IsNullOrWhiteSpace(kv.Value))
				{
					string outputKey = state.GetOutputKey(kv.Key);
					segments.Add($"{outputKey}|{kv.Value}");
				}
			}

			foreach (var flag in _spellFlagOutputOrder)
			{
				if (state.Flags.Contains(flag))
				{
					segments.Add(flag);
				}
			}

			foreach (var segment in state.UnknownSegments)
			{
				if (!string.IsNullOrWhiteSpace(segment))
				{
					segments.Add(segment.Trim());
				}
			}

			if (!state.Enabled)
			{
				segments.Add("Disabled");
			}

			if (segments.Count == 0)
			{
				return baseName;
			}

			if (string.IsNullOrWhiteSpace(baseName))
			{
				return string.Join("/", segments);
			}

			return baseName + "/" + string.Join("/", segments);
		}


		private static void RenderSpellModifierEditor(SpellValueEditState state)
		{
			if (state == null) return;

			string idBase = (_cfgSpellEditSignature ?? string.Empty).Replace(':', '_').Replace('/', '_').Replace(' ', '_');
			if (string.IsNullOrEmpty(idBase))
			{
				idBase = state.ValueIndex.ToString(CultureInfo.InvariantCulture);
			}

			string ControlId(string key) => $"##spell_{key}_{idBase}";

			const ImGuiTableFlags FieldTableFlags = (ImGuiTableFlags.ImGuiTableFlags_SizingStretchProp | ImGuiTableFlags.ImGuiTableFlags_PadOuterX);
			const ImGuiTableColumnFlags LabelColumnFlags = (ImGuiTableColumnFlags.ImGuiTableColumnFlags_WidthFixed | ImGuiTableColumnFlags.ImGuiTableColumnFlags_NoResize);
			const ImGuiTableColumnFlags ValueColumnFlags = ImGuiTableColumnFlags.ImGuiTableColumnFlags_WidthStretch;
			const float LabelColumnWidth = 170f;
			const float DefaultFieldWidth = 240f;
			const float ComboFieldWidth = 220f;

			void RenderFieldTable(string tableId, Action body)
			{
				if (imgui_BeginTable(tableId, 2, (int)FieldTableFlags, imgui_GetContentRegionAvailX(), 0))
				{
					try
					{
						imgui_TableSetupColumn("Label", (int)LabelColumnFlags, LabelColumnWidth);
						imgui_TableSetupColumn("Value", (int)ValueColumnFlags, 0f);
						body?.Invoke();
					}
					finally
					{
						imgui_EndTable();
					}


				}
			}

			void RenderFieldRow(string label, Action renderControl, string tooltip = null)
			{
				imgui_TableNextRow();
				imgui_TableNextColumn();
				imgui_Text(label);
				if (!string.IsNullOrWhiteSpace(tooltip) && imgui_IsItemHovered())
				{
					imgui_BeginTooltip();
					imgui_PushTextWrapPos(320f);
					imgui_TextWrapped(tooltip);
					imgui_PopTextWrapPos();
					imgui_EndTooltip();
				}
				imgui_TableNextColumn();
				renderControl?.Invoke();
			}

			void RenderLabeledValueField(string label, string key, float width = -1f, string tooltip = null)
			{
				RenderFieldRow(label, () =>
				{
					string controlId = ControlId(key);
					string current = state.GetValue(key) ?? string.Empty;
					float w = width > 0f ? width : DefaultFieldWidth;
					imgui_SetNextItemWidth(w);
					if (imgui_InputText(controlId, current))
					{
						string updated = imgui_InputText_Get(controlId) ?? string.Empty;
						state.SetValue(key, updated);
					}
				}, tooltip);
			}

			void RenderLabeledTextField(string label, string key, Func<string> getter, Action<string> setter, float width = -1f, string tooltip = null)
			{
				RenderFieldRow(label, () =>
				{
					string controlId = ControlId(key);
					string current = getter?.Invoke() ?? string.Empty;
					float w = width > 0f ? width : DefaultFieldWidth;
					imgui_SetNextItemWidth(w);
					if (imgui_InputText(controlId, current))
					{
						string updated = imgui_InputText_Get(controlId) ?? string.Empty;
						setter?.Invoke(updated);
					}
				}, tooltip);
			}

			void CheckboxFlag(string label, string flag)
			{
				bool value = imgui_Checkbox($"{label}##spell_flag_{flag}_{idBase}", state.HasFlag(flag));
				state.SetFlag(flag, value);
				if (Spell.SpellFlagTooltips.TryGetValue(flag, out var tooltip) && !string.IsNullOrWhiteSpace(tooltip))
				{
					if (imgui_IsItemHovered())
					{
						imgui_BeginTooltip();
						imgui_PushTextWrapPos(320f);
						imgui_TextWrapped(tooltip);
						imgui_PopTextWrapPos();
						imgui_EndTooltip();
					}
				}
			}

			void RenderGeneralTab()
			{
				imgui_TextColored(0.8f, 0.9f, 0.95f, 1.0f, "Basics");

				RenderFieldTable($"SpellGeneralTable_{idBase}", () =>
				{
					RenderLabeledTextField("Cast Name:", "basename", () => state.BaseName, v => state.BaseName = v);
					RenderLabeledTextField("Cast Target:", "casttarget", () => state.CastTarget, v => state.CastTarget = v);

					RenderFieldRow("Gem Slot:", () =>
					{
						string gemValue = state.GetValue("Gem");
						int.TryParse(gemValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var gemSlot);
						string gemPreview = gemSlot >= 1 && gemSlot <= 12 ? $"Gem {gemSlot}" : "No Gem";
						imgui_SetNextItemWidth(ComboFieldWidth);
						if (BeginComboSafe($"##spell_gem_{idBase}", gemPreview))
						{
							if (imgui_Selectable("No Gem", gemSlot == 0))
							{
								state.SetValue("Gem", string.Empty);
								gemSlot = 0;
							}
							for (int g = 1; g <= 12; g++)
							{
								bool sel = gemSlot == g;
								if (imgui_Selectable($"Gem {g}", sel))
								{
									state.SetValue("Gem", g.ToString(CultureInfo.InvariantCulture));
									gemSlot = g;
								}
							}
							EndComboSafe();
						}
					});

					RenderFieldRow("Cast Type:", () =>
					{
						string castType = state.GetValue("CastType");
						string castPreview = string.IsNullOrEmpty(castType) ? "Auto (Detect)" : castType;
						imgui_SetNextItemWidth(ComboFieldWidth);
						if (BeginComboSafe($"##spell_casttype_{idBase}", castPreview))
						{
							if (imgui_Selectable("Auto (Detect)", string.IsNullOrEmpty(castType)))
							{
								state.SetValue("CastType", string.Empty);
								castType = string.Empty;
							}
							foreach (var option in _spellCastTypeOptions)
							{
								bool sel = string.Equals(castType, option, StringComparison.OrdinalIgnoreCase);
								if (imgui_Selectable(option, sel))
								{
									state.SetValue("CastType", option);
									castType = option;
								}
							}
							EndComboSafe();
						}
					});

					RenderFieldRow("Enabled:", () =>
					{
						bool enabled = imgui_Checkbox($"##spell_enabled_{idBase}", state.Enabled);
						state.Enabled = enabled;
					});
				});
			}

			void RenderConditionsTab()
			{
				imgui_TextColored(0.8f, 0.9f, 0.95f, 1.0f, "Logic");

				RenderFieldTable($"SpellConditionsTable_{idBase}", () =>
				{
					RenderLabeledValueField("Ifs Keys:", "Ifs");
					RenderLabeledValueField("Check For:", "CheckFor", 320f);
					RenderLabeledValueField("Cast IF:", "CastIF", 320f);
					RenderLabeledValueField("Zone:", "Zone");
					RenderLabeledValueField("Min Sick:", "MinSick");
					RenderLabeledValueField("Trigger Spell:", "TriggerSpell");
				});
			}

			void RenderResourceTab()
			{
				imgui_TextColored(0.8f, 0.9f, 0.95f, 1.0f, "Thresholds");

				RenderFieldTable($"SpellResourceTable_{idBase}", () =>
				{
					RenderLabeledValueField("Min Mana:", "MinMana");
					RenderLabeledValueField("Max Mana:", "MaxMana");
					RenderLabeledValueField("Min End:", "MinEnd");
					RenderLabeledValueField("Min HP%:", "MinHP");
					RenderLabeledValueField("Min HP Total:", "MinHPTotal", 280f);
					RenderLabeledValueField("Heal %:", "HealPct");
					RenderLabeledValueField("Cancel Heal Above %:", "HealthMax", 280f);
					RenderLabeledValueField("Pct Aggro:", "PctAggro", tooltip: "Skip this entry if your current aggro percent exceeds the specified threshold.");
					RenderLabeledValueField("Min Aggro:", "MinAggro", tooltip: "Only cast when your aggro percent is at least this value (helps gate low-threat openers).");
					RenderLabeledValueField("Max Aggro:", "MaxAggro", tooltip: "Do not cast once your aggro percent is above this value (useful for backing off).");
					RenderLabeledValueField("Give Up Timer:", "GiveUpTimer");
				});
			}

			void RenderTimingTab()
			{
				imgui_TextColored(0.8f, 0.9f, 0.95f, 1.0f, "Delays");
				imgui_Text("Use suffix 's' for seconds or 'm' for minutes (example: 10s).");

				RenderFieldTable($"SpellTimingTable_{idBase}", () =>
				{
					RenderLabeledValueField("Delay:", "Delay");
					RenderLabeledValueField("Recast Delay:", "RecastDelay");
					RenderLabeledValueField("Min Duration Before Recast:", "MinDurationBeforeRecast", 300f);
					RenderLabeledValueField("Max Tries:", "MaxTries");
					RenderLabeledValueField("Song Refresh Time:", "SongRefreshTime", 260f);
					RenderLabeledValueField("Before Spell Delay:", "BeforeSpellDelay", 260f);
					RenderLabeledValueField("After Spell Delay:", "AfterSpellDelay", 260f);
					RenderLabeledValueField("Before Event Delay:", "BeforeEventDelay", 260f);
					RenderLabeledValueField("After Event Delay:", "AfterEventDelay", 260f);
					RenderLabeledValueField("After Cast Delay:", "AfterCastDelay", 260f);
					RenderLabeledValueField("After Cast Completed Delay:", "AfterCastCompletedDelay", 300f);
				});
			}

			void RenderAdvancedTab()
			{
				imgui_TextColored(0.8f, 0.9f, 0.95f, 1.0f, "Ordering");

				RenderFieldTable($"SpellOrderingTable_{idBase}", () =>
				{
					RenderLabeledValueField("Before Spell:", "BeforeSpell", 280f);
					RenderLabeledValueField("After Spell:", "AfterSpell", 280f);
					RenderLabeledValueField("Before Event:", "BeforeEvent", 280f);
					RenderLabeledValueField("After Event:", "AfterEvent", 280f);
					RenderLabeledValueField("Reagent:", "Reagent", 260f);
				});

				imgui_Separator();
				imgui_TextColored(0.8f, 0.9f, 0.95f, 1.0f, "Stacking & Requests");

				RenderFieldTable($"SpellStackingTable_{idBase}", () =>
				{
					RenderLabeledValueField("Stack Request Item:", "StackRequestItem", 300f);
					RenderLabeledValueField("Stack Request Targets:", "StackRequestTargets", 320f);
					RenderLabeledValueField("Stack Check Interval:", "StackCheckInterval", 260f);
					RenderLabeledValueField("Stack Recast Delay:", "StackRecastDelay", 260f);
				});

				imgui_Separator();
				imgui_TextColored(0.8f, 0.9f, 0.95f, 1.0f, "Exclusions");

				RenderFieldTable($"SpellExclusionsTable_{idBase}", () =>
				{
					RenderLabeledValueField("Excluded Classes:", "ExcludedClasses", 320f);
					RenderLabeledValueField("Excluded Names:", "ExcludedNames", 320f);
				});
			}

			void RenderManualEditTab()
			{
				imgui_TextColored(0.8f, 0.9f, 0.95f, 1.0f, "Manual Text Editor");
				imgui_Text("Edit the raw configuration value directly. Changes apply when you click Apply.");
				imgui_Separator();

				// Text area for manual editing
				float textWidth = Math.Max(500f, imgui_GetContentRegionAvailX() * 0.95f);
				float textHeight = Math.Max(180f, imgui_GetTextLineHeightWithSpacing() * 10f);


				if (imgui_InputTextMultiline($"##manual_edit_{idBase}", _cfgManualEditBuffer ?? string.Empty, textWidth, textHeight))
				{
					_cfgManualEditBuffer = imgui_InputText_Get($"##manual_edit_{idBase}") ?? string.Empty;
				}

				imgui_Separator();
				if (imgui_Button($"Load From Current##manual_load_{idBase}"))
				{
					// Reload the buffer from the current state
					_cfgManualEditBuffer = BuildSpellValueString(state) ?? string.Empty;
				}
				imgui_SameLine();
				if (imgui_Button($"Parse Into Editor##manual_parse_{idBase}"))
				{
					// Try to parse the manual buffer and update the state
					var tempState = ParseSpellValueEditState(state.Section, state.Key, state.ValueIndex, _cfgManualEditBuffer ?? string.Empty);
					if (tempState != null)
					{
						// Update the current state with parsed values
						state.BaseName = tempState.BaseName;
						state.CastTarget = tempState.CastTarget;
						state.Enabled = tempState.Enabled;
						state.KeyValues.Clear();
						foreach (var kvp in tempState.KeyValues)
						{
							state.KeyValues[kvp.Key] = kvp.Value;
						}
						state.Flags.Clear();
						foreach (var flag in tempState.Flags)
						{
							state.Flags.Add(flag);
						}
						state.UnknownSegments.Clear();
						foreach (var seg in tempState.UnknownSegments)
						{
							state.UnknownSegments.Add(seg);
						}
					}
				}
			}

			void RenderFlagsTab()
			{
				imgui_TextColored(0.8f, 0.9f, 0.95f, 1.0f, "Behavior Flags");
				imgui_Text("Toggle and Apply to commit changes.");

				if (imgui_BeginTable($"E3SpellFlagTable_{idBase}", 2, (int)ImGuiTableFlags.ImGuiTableFlags_SizingStretchSame, imgui_GetContentRegionAvailX(), 0))
				{
					try
					{
						imgui_TableSetupColumn("FlagColumnLeft", 0, 0f);
						imgui_TableSetupColumn("FlagColumnRight", 0, 0f);
						int col = 0;
						foreach (var entry in _spellFlags)
						{
							if (col == 0)
							{
								imgui_TableNextRow();
							}
							imgui_TableNextColumn();
							CheckboxFlag(entry.Label, entry.Flag);
							col = (col + 1) % 2;
						}
					}
					finally
					{
						imgui_EndTable();
					}


				}
				else
				{
					foreach (var entry in _spellFlags)
					{
						CheckboxFlag(entry.Label, entry.Flag);
					}
				}
			}

			// Header row with title on left and buttons on right
			string entryLabel = $"[{state.Section}] {state.Key} entry #{state.ValueIndex + 1}";
			imgui_TextColored(0.95f, 0.85f, 0.35f, 1.0f, entryLabel);

			// Position Apply/Reset buttons on the same line, aligned to the right
			float buttonWidth = 80f;
			float spacing = 8f;
			float totalButtonWidth = (buttonWidth * 2) + spacing;
			float availWidth = imgui_GetContentRegionAvailX();
			if (availWidth > totalButtonWidth)
			{
				imgui_SameLineEx(availWidth - totalButtonWidth, 0f);
			}
			else
			{
				imgui_SameLine();
			}

			if (imgui_Button($"Apply##spell_apply_{idBase}"))
			{
				ApplySpellValueChanges(state);
			}
			imgui_SameLine();
			if (imgui_Button($"Reset##spell_reset_{idBase}"))
			{
				ResetSpellValueEditor(state);
			}

			if (!string.IsNullOrEmpty(state.OriginalValue))
			{
				imgui_TextColored(0.7f, 0.8f, 0.9f, 1.0f, "Original value:");
				imgui_TextWrapped(state.OriginalValue);
			}

			imgui_Separator();

			if (imgui_BeginTabBar($"SpellModifierTabs_{idBase}"))
			{
				if (imgui_BeginTabItem($"General##spell_tab_general_{idBase}"))
				{
					RenderGeneralTab();
					imgui_EndTabItem();
				}
				if (imgui_BeginTabItem($"Conditions##spell_tab_conditions_{idBase}"))
				{
					RenderConditionsTab();
					imgui_EndTabItem();
				}
				if (imgui_BeginTabItem($"Resources##spell_tab_resources_{idBase}"))
				{
					RenderResourceTab();
					imgui_EndTabItem();
				}
				if (imgui_BeginTabItem($"Timing##spell_tab_timing_{idBase}"))
				{
					RenderTimingTab();
					imgui_EndTabItem();
				}
				if (imgui_BeginTabItem($"Advanced##spell_tab_advanced_{idBase}"))
				{
					RenderAdvancedTab();
					imgui_EndTabItem();
				}
				if (imgui_BeginTabItem($"Flags##spell_tab_flags_{idBase}"))
				{
					RenderFlagsTab();
					imgui_EndTabItem();
				}
				if (imgui_BeginTabItem($"Manual Edit##spell_tab_manual_{idBase}"))
				{
					RenderManualEditTab();
					imgui_EndTabItem();
				}
				imgui_EndTabBar();
			}

			if (state.UnknownSegments.Count > 0)
			{
				imgui_Separator();
				imgui_TextColored(0.95f, 0.85f, 0.35f, 1.0f, "Preserved Tokens");
				imgui_TextWrapped(string.Join(", ", state.UnknownSegments));
			}

			imgui_Separator();
			imgui_TextColored(0.8f, 0.9f, 0.95f, 1.0f, "Preview");
			string preview = BuildSpellValueString(state);
			imgui_TextWrapped(string.IsNullOrEmpty(preview) ? "(empty)" : preview);
		}

		private static void RenderSpellModifierModal()
		{
			var mainWindowState = _state.GetState<State_MainWindow>();
			var iniData = GetActiveCharacterIniData();
			var sectionData = iniData?.Sections?.GetSectionData(mainWindowState.SelectedSection ?? string.Empty);
			var keyData = sectionData?.Keys?.GetKeyData(mainWindowState.SelectedKey ?? string.Empty);
			var values = GetValues(keyData);
			if (mainWindowState.SelectedValueIndex < 0 || mainWindowState.SelectedValueIndex >= values.Count)
			{
				_cfgShowSpellModifierModal = false;
				return;
			}

			string rawValue = values[mainWindowState.SelectedValueIndex] ?? string.Empty;
			var state = EnsureSpellEditState(mainWindowState.SelectedSection, mainWindowState.SelectedKey, mainWindowState.SelectedValueIndex, rawValue);
			if (state == null)
			{
				_cfgShowSpellModifierModal = false;
				return;
			}

			
			bool modalOpen = imgui_Begin(_state.WinName_SpellModifier, (int)(ImGuiWindowFlags.ImGuiWindowFlags_NoCollapse | ImGuiWindowFlags.ImGuiWindowFlags_NoDocking));
			if (modalOpen)
			{
				RenderSpellModifierEditor(state);

				imgui_Separator();
				if (imgui_Button("Close"))
				{
					_cfgShowSpellModifierModal = false;
				}
			}
			imgui_End();

		}

		private static void ApplySpellValueChanges(SpellValueEditState state)
		{
			if (state == null) return;

			string newValue = BuildSpellValueString(state) ?? string.Empty;
			var iniData = GetActiveCharacterIniData();
			if (iniData?.Sections == null) return;

			var section = iniData.Sections.GetSectionData(state.Section ?? string.Empty);
			if (section == null)
			{
				_log.Write($"Section '{state.Section}' not found; unable to update value.");
				InvalidateSpellEditState();
				return;
			}

			var keyData = section.Keys.GetKeyData(state.Key ?? string.Empty);
			if (keyData == null)
			{
				_log.Write($"Key '{state.Key}' not found in section '[{state.Section}]'.");
				InvalidateSpellEditState();
				return;
			}

			var values = GetValues(keyData);
			if (state.ValueIndex < 0 || state.ValueIndex >= values.Count)
			{
				_log.Write("Value index out of range; refresh the selection and try again.");
				InvalidateSpellEditState();
				return;
			}

			if (string.Equals(values[state.ValueIndex] ?? string.Empty, newValue, StringComparison.Ordinal))
			{
				// Nothing changed
				return;
			}

			values[state.ValueIndex] = newValue;
			WriteValues(keyData, values);

			state.OriginalValue = newValue;
			_cfgSpellEditSignature = $"{state.Section ?? string.Empty}::{state.Key ?? string.Empty}::{state.ValueIndex}::{newValue}";
			_cfgSpellEditState = ParseSpellValueEditState(state.Section, state.Key, state.ValueIndex, newValue);

			ClearSpellEditFields(_cfgSpellEditState);
			_cfgSpellEditSignature = string.Empty;

			_log.Write($"Updated [{state.Section}] {state.Key} entry #{state.ValueIndex + 1}.");
		}

		private static void ResetSpellValueEditor(SpellValueEditState state)
		{
			if (state == null)
			{
				InvalidateSpellEditState();
				return;
			}

			var iniData = GetActiveCharacterIniData();
			var section = iniData?.Sections?.GetSectionData(state.Section ?? string.Empty);
			var keyData = section?.Keys?.GetKeyData(state.Key ?? string.Empty);
			if (keyData == null)
			{
				InvalidateSpellEditState();
				return;
			}

			var values = GetValues(keyData);
			if (state.ValueIndex < 0 || state.ValueIndex >= values.Count)
			{
				InvalidateSpellEditState();
				return;
			}

			string currentValue = values[state.ValueIndex] ?? string.Empty;
			_cfgSpellEditSignature = $"{state.Section ?? string.Empty}::{state.Key ?? string.Empty}::{state.ValueIndex}::{currentValue}";
			_cfgSpellEditState = ParseSpellValueEditState(state.Section, state.Key, state.ValueIndex, currentValue);
		}

		private static List<string> GetValues(KeyData kd)
		{
			return kd.ValueList;
			//var vals = new List<string>();
			//if (kd.ValueList != null && kd.ValueList.Count > 0)
			//{
			//	foreach (var v in kd.ValueList) vals.Add(v ?? string.Empty);
			//}
			//else if (!string.IsNullOrEmpty(kd.Value))
			//{
			//	// Support pipe-delimited storage if present
			//	var parts = (kd.Value ?? string.Empty).Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim());
			//	foreach (var p in parts) vals.Add(p);
			//}
			//return vals;
		}

		private static void WriteValues(KeyData kd, List<string> values)
		{
			var mainWindowState = _state.GetState<State_MainWindow>();


			if (kd == null) return;
			// Preserve exact row semantics: one value per row, including empties
			if (kd.ValueList != null)
			{
				kd.ValueList.Clear();
				foreach (var v in values) kd.ValueList.Add(v ?? string.Empty);
			}
			// Do NOT set kd.Value here; in our Ini parser, setting Value appends to ValueList.
			mainWindowState.ConfigIsDirty = true;

		}

		// Inventory scanning for Food/Drink using MQ TLOs (non-blocking via ProcessBackgroundWork trigger)
		private static void RenderFoodDrinkPicker(SectionData selectedSection)
		{
			// Respect current open state instead of forcing true every frame
			bool shouldDraw = imgui_Begin(_state.WinName_FoodDrinkModal, (int)(ImGuiWindowFlags.ImGuiWindowFlags_AlwaysAutoResize | ImGuiWindowFlags.ImGuiWindowFlags_NoCollapse | ImGuiWindowFlags.ImGuiWindowFlags_NoDocking));

			if (shouldDraw)
			{
				var mainWindowState = _state.GetState<State_MainWindow>();

				// Header with better styling
				imgui_TextColored(0.95f, 0.85f, 0.35f, 1.0f, $"Pick {_cfgFoodDrinkKey} from inventory");
				imgui_Separator();

				// Status and scan button
				if (string.IsNullOrEmpty(_cfgFoodDrinkStatus))
				{
					if (imgui_Button("Scan Inventory"))
					{
						_cfgFoodDrinkStatus = "Scanning...";
						_cfgFoodDrinkScanRequested = true;
					}
					imgui_Text("Click above to scan your inventory.");
				}
				else
				{
					imgui_TextColored(0.7f, 0.9f, 0.7f, 1.0f, _cfgFoodDrinkStatus);
				}

				imgui_Separator();

				// Results list with better sizing
				if (_cfgFoodDrinkCandidates.Count > 0)
				{
					imgui_TextColored(0.8f, 0.9f, 1.0f, 1.0f, "Found items (click to select):");

					// Use responsive sizing for the list
					float listHeight = Math.Min(400f, Math.Max(150f, _cfgFoodDrinkCandidates.Count * 20f + 40f));
					float listWidth = Math.Max(300f, imgui_GetContentRegionAvailX() * 0.9f);

					if (imgui_BeginChild("FoodDrinkList", listWidth, listHeight, 1, 0))
					{
						for (int i = 0; i < _cfgFoodDrinkCandidates.Count; i++)
						{
							var item = _cfgFoodDrinkCandidates[i];
							if (imgui_Selectable($"{item}##item_{i}", false))
							{
								// Apply selection
								var pdAct = GetActiveCharacterIniData();
								var secData = pdAct.Sections.GetSectionData(mainWindowState.SelectedSection);
								var keyData = secData?.Keys.GetKeyData(mainWindowState.SelectedKey);
								if (keyData != null)
								{
									var vals = GetValues(keyData);
									// Replace first value or add if empty
									if (vals.Count == 0) vals.Add(item);
									else vals[0] = item;
									WriteValues(keyData, vals);
								}
								_state.Show_FoodDrinkModal = false;
								break; // Exit loop after selection
							}
						}
					}
					imgui_EndChild();
				}
				else if (!string.IsNullOrEmpty(_cfgFoodDrinkStatus) && !_cfgFoodDrinkStatus.Contains("Scanning"))
				{
					imgui_TextColored(0.9f, 0.7f, 0.7f, 1.0f, "No matching items found.");
				}

				imgui_Separator();

				// Action buttons
				if (_cfgFoodDrinkCandidates.Count > 0)
				{
					if (imgui_Button("Rescan"))
					{
						_cfgFoodDrinkStatus = "Scanning...";
						_cfgFoodDrinkCandidates.Clear();
						_cfgFoodDrinkScanRequested = true;
					}
					imgui_SameLine();
				}

				if (imgui_Button("Close"))
				{
					_state.Show_FoodDrinkModal = false;
				}
			}

			imgui_End();


		}
		private static void RenderBardMelodyHelperModal()
		{
		
			var state = _state.GetState<State_BardEditor>();

			bool open = imgui_Begin(_state.WinName_BardMelodyHelper, (int)(ImGuiWindowFlags.ImGuiWindowFlags_AlwaysAutoResize | ImGuiWindowFlags.ImGuiWindowFlags_NoCollapse | ImGuiWindowFlags.ImGuiWindowFlags_NoDocking));
			if (open)
			{
				imgui_TextColored(0.95f, 0.85f, 0.35f, 1.0f, "Create a Bard Melody");
				imgui_TextWrapped("Answer the prompts below to build a melody and optional IF condition. We'll add everything to your INI for you.");
				imgui_Separator();

				imgui_Text("Melody name:");
				imgui_SameLine();
				imgui_SetNextItemWidth(260f);
				if (imgui_InputText("##bard_melody_name", state.MelodyName ?? string.Empty))
				{
					state.MelodyName = (imgui_InputText_Get("##bard_melody_name") ?? string.Empty).Trim();
				}
				if (string.IsNullOrEmpty(state.MelodyName))
				{
					imgui_TextColored(0.7f, 0.7f, 0.7f, 1.0f, "Example: \"Caster\" or \"Main\"");
				}
				imgui_Separator();

				EnsureBardMelodySongEntries();
				bool catalogsReady = _state.State_CatalogReady;
				imgui_Text("Songs (cast order):");
				if (!catalogsReady)
				{
					imgui_TextColored(0.8f, 0.6f, 0.6f, 1.0f, "Catalog data not yet loaded. Use manual entry or load catalogs first.");
				}
				for (int i = 0; i < state.MelodySongs.Count; i++)
				{
					string label = $"Song {i + 1}";
					imgui_SetNextItemWidth(300f);
					string inputId = $"{label}##bard_song_{i}_{state.SongInputVersion}";

					// Ensure buffer exists and is synchronized with the songs list
					if (!state.MelodyBuffers.ContainsKey(i))
					{
						state.MelodyBuffers[i] = state.MelodySongs[i] ?? string.Empty;
					}

					string buffer = state.MelodyBuffers[i];
					if (imgui_InputText(inputId, buffer))
					{
						buffer = imgui_InputText_Get(inputId) ?? string.Empty;
						state.MelodyBuffers[i] = buffer;
						state.MelodySongs[i] = buffer.Trim();
					}
					imgui_SameLine();
					if (imgui_Button($"Remove##bard_song_remove_{i}"))
					{
						state.MelodySongs.RemoveAt(i);
						ReindexBardMelodyBuffers();
						i--;
						continue;
					}
					if (catalogsReady)
					{
						imgui_SameLine();
						if (imgui_Button($"Pick##bard_song_pick_{i}"))
						{
							OpenBardSongPicker(i);
						}
					}
					imgui_SameLine();
					imgui_Text("Gem:");
					imgui_SameLine();
					imgui_SetNextItemWidth(50f);
					string gemPreview = state.MelodyGems[i].ToString();
					if (imgui_BeginCombo($"##bard_gem_combo_{i}_{state.SongInputVersion}", gemPreview, 0))
					{
						for (int gem = 1; gem <= 12; gem++)
						{
							if (imgui_MenuItem(gem.ToString()))
							{
								state.MelodyGems[i] = gem;
								state.MelodyGemBuffers[i] = gem.ToString();
							}
						}
						imgui_EndCombo();
					}
				}
				if (imgui_Button("Add Another Song"))
				{
					state.MelodySongs.Add(string.Empty);
					state.MelodyBuffers[state.MelodySongs.Count - 1] = string.Empty;
					state.MelodyGems.Add(1);
					state.MelodyGemBuffers[state.MelodySongs.Count - 1] = "1";
				}
				imgui_Separator();

				imgui_Text("When should we play it?");
				imgui_SetNextItemWidth(350f);
				string conditionId = $"##bard_melody_condition_{state.ConditionInputVersion}";
				if (imgui_InputText(conditionId, state.MelodyCondition ?? string.Empty))
				{
					state.MelodyCondition = (imgui_InputText_Get(conditionId) ?? string.Empty).Trim();
				}
				imgui_SameLine();
				if (imgui_Button("Sample IFs..."))
				{
					if (!EnsureBardSampleIfsLoaded())
					{
						if (string.IsNullOrEmpty(state.SampleIfStatus))
						{
							state.SampleIfStatus = "Sample file not found.";
						}
					}
					_state.Show_BardSampleIfModal = true;
				}
				imgui_TextColored(0.7f, 0.7f, 0.7f, 1.0f, "Optional E3 IF expression. Leave blank to run the melody whenever possible.");
				imgui_Separator();

				if (!string.IsNullOrEmpty(state.MelodyModalStatus))
				{
					imgui_TextColored(0.9f, 0.6f, 0.6f, 1.0f, state.MelodyModalStatus);
					imgui_Separator();
				}

				if (imgui_Button("Create Melody"))
				{
					if (TryCreateBardMelody(out var successMessage, out var errorMessage))
					{
						state.MelodyStatus = successMessage;
						state.MelodyModalStatus = string.Empty;
						ResetBardMelodyHelperForm();
					}
					else
					{
						state.MelodyModalStatus = errorMessage;
					}
				}
				imgui_SameLine();
				if (imgui_Button("Cancel##bard_helper_cancel"))
				{
					_state.Show_BardMelodyHelper = false;
				}
			}
			imgui_End();
	
			// Reset the picker state after rendering
			if (state.SongPickerJustSelected)
			{
				state.BardSongPickerIndex = -1;
				state.SongPickerJustSelected = false;
			}
		}
		private static void ResetBardMelodyHelperForm()
		{
			var state = _state.GetState<State_BardEditor>();

			state.MelodyName = string.Empty;
			state.MelodyCondition = string.Empty;
			state.MelodyModalStatus = string.Empty;
			state.MelodySongs = new List<string> { string.Empty, string.Empty, string.Empty };
			state.MelodyBuffers = new Dictionary<int, string>
			{
				{0, string.Empty},
				{1, string.Empty},
				{2, string.Empty}
			};
			state.MelodyGems = new List<int> { 1, 1, 1 };
			state.MelodyGemBuffers = new Dictionary<int, string>
			{
				{0, "1"},
				{1, "1"},
				{2, "1"}
			};

			state.BardSongPickerIndex = -1;
			var catalogState = _state.GetState<State_CatalogWindow>();
			catalogState.Mode = CatalogMode.Standard;

		}
		private static void EnsureBardMelodySongEntries()
		{
			var state = _state.GetState<State_BardEditor>();

			if (state.MelodySongs == null)
			{
				state.MelodySongs = new List<string>();
			}
			if (state.MelodyBuffers == null)
			{
				state.MelodyBuffers = new Dictionary<int, string>();
			}
			if (state.MelodyGems == null)
			{
				state.MelodyGems = new List<int>();
			}
			if (state.MelodyGemBuffers == null)
			{
				state.MelodyGemBuffers = new Dictionary<int, string>();
			}
			if (state.MelodySongs.Count == 0)
			{
				state.MelodySongs.Add(string.Empty);
			}
			while (state.MelodyGems.Count < state.MelodySongs.Count)
			{
				state.MelodyGems.Add(1);
			}
			for (int i = 0; i < state.MelodySongs.Count; i++)
			{
				if (!state.MelodyBuffers.ContainsKey(i))
				{
					state.MelodyBuffers[i] = state.MelodySongs[i] ?? string.Empty;
				}
				if (!state.MelodyGemBuffers.ContainsKey(i))
				{
					state.MelodyGemBuffers[i] = state.MelodyGems[i].ToString();
				}
			}
			var keysToRemove = state.MelodyBuffers.Keys.Where(k => k >= state.MelodySongs.Count).ToList();
			foreach (var key in keysToRemove)
			{
				state.MelodyBuffers.Remove(key);
			}
			var gemKeysToRemove = state.MelodyGemBuffers.Keys.Where(k => k >= state.MelodySongs.Count).ToList();
			foreach (var key in gemKeysToRemove)
			{
				state.MelodyGemBuffers.Remove(key);
			}
			while (state.MelodyGems.Count > state.MelodySongs.Count)
			{
				state.MelodyGems.RemoveAt(state.MelodyGems.Count - 1);
			}
		}

		private static void ReindexBardMelodyBuffers()
		{
			var state = _state.GetState<State_BardEditor>();

			var newBuffers = new Dictionary<int, string>();
			var newGemBuffers = new Dictionary<int, string>();
			for (int i = 0; i < state.MelodySongs.Count; i++)
			{
				string value = state.MelodySongs[i] ?? string.Empty;
				newBuffers[i] = value;
				newGemBuffers[i] = state.MelodyGems[i].ToString();
			}
			state.MelodyBuffers = newBuffers;
			state.MelodyGemBuffers = newGemBuffers;
		}
		private static bool EnsureBardSampleIfsLoaded()
		{
			var state = _state.GetState<State_BardEditor>();

			if (state.SampleIfLines.Count > 0)
			{
				return true;
			}

			state.SampleIfLines.Clear();
			state.SampleIfStatus = string.Empty;
			try
			{
				string samplePath = ResolveSampleIfsPath();
				if (string.IsNullOrEmpty(samplePath))
				{
					state.SampleIfStatus = "Sample file not found.";
					return false;
				}

				state.SampleIfStatus = "Loaded: " + Path.GetFileName(samplePath);
				int added = 0;
				foreach (var raw in File.ReadAllLines(samplePath))
				{
					var line = (raw ?? string.Empty).Trim();
					if (line.Length == 0) continue;
					if (line.StartsWith("#") || line.StartsWith(";")) continue;

					string key = string.Empty;
					string val = string.Empty;

					int eq = line.IndexOf('=');
					if (eq > 0)
					{
						key = line.Substring(0, eq).Trim();
						val = line.Substring(eq + 1).Trim();
					}
					else
					{
						int colon = line.IndexOf(':');
						int dash = line.IndexOf('-');
						int pos = -1;
						if (colon > 0) pos = colon; else if (dash > 0) pos = dash;
						if (pos > 0)
						{
							key = line.Substring(0, pos).Trim();
							val = line.Substring(pos + 1).Trim();
						}
						else
						{
							key = line;
							val = string.Empty;
						}
					}

					if (!string.IsNullOrEmpty(key))
					{
						state.SampleIfLines.Add(new KeyValuePair<string, string>(key, val));
						added++;
					}
				}

				if (added == 0)
				{
					state.SampleIfStatus = "No entries found in sample file.";
				}
			}
			catch (Exception ex)
			{
				state.SampleIfStatus = "Error reading sample IFs: " + (ex.Message ?? "error");
			}

			return state.SampleIfLines.Count > 0;
		}
		private static void OpenBardSongPicker(int index)
		{
			var mainWIndowState = _state.GetState<State_CatalogWindow>();
			var bardEditorState = _state.GetState<State_BardEditor>();

			if (index < 0) return;
			EnsureBardMelodySongEntries();
			while (bardEditorState.MelodySongs.Count <= index)
			{
				bardEditorState.MelodySongs.Add(string.Empty);
			}

			bardEditorState.BardSongPickerIndex = index;
			mainWIndowState.Mode = CatalogMode.BardSong;
			mainWIndowState.CurrentAddType = AddType.Spells;
			mainWIndowState.SelectedCategory = string.Empty;
			mainWIndowState.SelectedSubCategory = string.Empty;
			mainWIndowState.Filter = string.Empty;
			
			_state.Show_AddModal = true;
			
		}
		private static void ApplyBardSongSelection(string songName)
		{
			var state = _state.GetState<State_CatalogWindow>();
			var bardEditorState = _state.GetState<State_BardEditor>();

			if (bardEditorState.BardSongPickerIndex < 0)
			{
				state.Mode = CatalogMode.Standard;
				_state.Show_AddModal = false;
				return;
			}
			EnsureBardMelodySongEntries();
			while (bardEditorState.MelodySongs.Count <= bardEditorState.BardSongPickerIndex)
			{
				bardEditorState.MelodySongs.Add(string.Empty);
			}
			bardEditorState.MelodySongs[bardEditorState.BardSongPickerIndex] = songName ?? string.Empty;
			bardEditorState.MelodyBuffers[bardEditorState.BardSongPickerIndex] = songName ?? string.Empty;
			bardEditorState.SongPickerJustSelected = true; // Flag to force input refresh
			bardEditorState.SongInputVersion++; // Change input ID to force text update
										// _state.State_BardSongPickerIndex = -1; // Moved to modal render to allow input text update
			state.Mode = CatalogMode.Standard;

			_state.Show_AddModal = false;
			
		}
		private static void RenderBardSampleIfModal()
		{
			var state = _state.GetState<State_BardEditor>();

			bool open = imgui_Begin(_state.WinName_BardSampleIfModal, (int)(ImGuiWindowFlags.ImGuiWindowFlags_AlwaysAutoResize | ImGuiWindowFlags.ImGuiWindowFlags_NoCollapse | ImGuiWindowFlags.ImGuiWindowFlags_NoDocking));
			if (open)
			{
				bool ready = EnsureBardSampleIfsLoaded();
				imgui_TextColored(0.95f, 0.85f, 0.35f, 1.0f, "Sample IFs");
				imgui_TextWrapped("Select a sample condition to copy it into the melody helper.");
				imgui_Separator();

				if (!ready)
				{
					imgui_TextColored(0.85f, 0.6f, 0.6f, 1.0f, string.IsNullOrEmpty(state.SampleIfStatus) ? "Sample file not found." : state.SampleIfStatus);
				}
				else if (!string.IsNullOrEmpty(state.SampleIfStatus))
				{
					imgui_TextColored(0.7f, 0.9f, 0.7f, 1.0f, state.SampleIfStatus);
				}

				if (imgui_Button("Reload Samples"))
				{
					state.SampleIfLines.Clear();
					EnsureBardSampleIfsLoaded();
				}
				imgui_SameLine();
				imgui_Text("Filter:");
				imgui_SameLine();
				imgui_SetNextItemWidth(260f);
				if (imgui_InputText("##bard_sample_if_filter", state.SampleIfFilter ?? string.Empty))
				{
					state.SampleIfFilter = (imgui_InputText_Get("##bard_sample_if_filter") ?? string.Empty).Trim();
				}
				imgui_SameLine();
				if (imgui_Button("Clear##bard_sample_if_filter_clear"))
				{
					state.SampleIfFilter = string.Empty;
				}

				imgui_Separator();

				var displayList = new List<KeyValuePair<string, string>>();
				if (ready)
				{
					if (string.IsNullOrEmpty(state.SampleIfFilter))
					{
						displayList.AddRange(state.SampleIfLines);
					}
					else
					{
						var tokens = state.SampleIfFilter.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
						foreach (var kv in state.SampleIfLines)
						{
							string searchText = (kv.Key + " " + kv.Value).ToLowerInvariant();
							bool matches = tokens.All(t => searchText.Contains(t.ToLowerInvariant()));
							if (matches) displayList.Add(kv);
						}
					}
				}

				if (displayList.Count == 0)
				{
					imgui_TextColored(0.8f, 0.8f, 0.6f, 1.0f, ready ? "No IFs match the current filter." : "No IFs available.");
				}
				else
				{
					float tableWidth = Math.Max(520f, imgui_GetContentRegionAvailX());
					float tableHeight = Math.Min(420f, Math.Max(220f, displayList.Count * 24f));
					if (imgui_BeginChild("BardSampleIfList", tableWidth, tableHeight, 1, 0))
					{
						if (imgui_BeginTable("E3BardSampleIfTable", 3, 0, tableWidth, 0))
						{
							try
							{
								imgui_TableSetupColumn("Name", 0, tableWidth * 0.25f);
								imgui_TableSetupColumn("Expression", 0, tableWidth * 0.55f);
								imgui_TableSetupColumn("Actions", 0, tableWidth * 0.2f);
								imgui_TableHeadersRow();

								foreach (var kv in displayList)
								{
									imgui_TableNextRow();
									imgui_TableNextColumn();
									imgui_Text(kv.Key);

									imgui_TableNextColumn();
									imgui_TextWrapped(string.IsNullOrEmpty(kv.Value) ? "(empty)" : kv.Value);

									imgui_TableNextColumn();
									string expression = string.IsNullOrEmpty(kv.Value) ? kv.Key : kv.Value;
									if (imgui_Button($"Use##bard_sample_if_use_{kv.Key}"))
									{
										state.MelodyCondition = expression;
										state.ConditionInputVersion++;
										_state.Show_BardSampleIfModal = false;
										break;
									}
								}
							}
							finally
							{
								imgui_EndTable();
							}

						}
						imgui_EndChild();
					}
				}

				imgui_Separator();
				if (imgui_Button("Close##bard_sample_if_close"))
				{
					_state.Show_BardSampleIfModal = false;
				}
			}
			imgui_End();
		}
		private static bool TryCreateBardMelody(out string successMessage, out string errorMessage)
		{
			var mainWindowState = _state.GetState<State_MainWindow>();
			var bardEditorState = _state.GetState<State_BardEditor>();

			successMessage = string.Empty;
			errorMessage = string.Empty;

			var pd = GetActiveCharacterIniData();
			if (pd == null)
			{
				errorMessage = "No character INI is currently loaded.";
				return false;
			}

			string rawName = (bardEditorState.MelodyName ?? string.Empty).Trim();
			if (rawName.Length == 0)
			{
				errorMessage = "Please provide a melody name.";
				return false;
			}

			string melodyName = rawName.EndsWith(" Melody", StringComparison.OrdinalIgnoreCase)
				? rawName.Substring(0, rawName.Length - " Melody".Length).TrimEnd()
				: rawName;

			var songGemPairs = (bardEditorState.MelodySongs ?? new List<string>())
				.Select((s, i) => new { Song = (s ?? string.Empty).Trim(), Gem = bardEditorState.MelodyGems[i] })
				.Where(p => p.Song.Length > 0)
				.GroupBy(p => p.Song, StringComparer.OrdinalIgnoreCase)
				.Select(g => g.First())
				.ToList();
			var songs = songGemPairs.Select(p => p.Song).ToList();
			if (songs.Count == 0)
			{
				errorMessage = "Add at least one song to the melody.";
				return false;
			}

			string condition = (bardEditorState.MelodyCondition ?? string.Empty).Trim();
			string melodySectionName = $"{melodyName} Melody";

			var melodySection = pd.Sections.GetSectionData(melodySectionName);
			if (melodySection == null)
			{
				pd.Sections.AddSection(melodySectionName);
				melodySection = pd.Sections.GetSectionData(melodySectionName);
			}
			if (melodySection == null)
			{
				errorMessage = "Unable to create the melody section.";
				return false;
			}

			if (melodySection.Keys.ContainsKey("Song"))
			{
				melodySection.Keys.RemoveKey("Song");
			}
			foreach (var pair in songGemPairs)
			{
				string songValue = $"{pair.Song}/gem|{pair.Gem}";
				melodySection.Keys.AddKey("Song", songValue);
			}

			if (!mainWindowState.SectionsOrdered.Any(s => string.Equals(s, melodySectionName, StringComparison.OrdinalIgnoreCase)))
			{
				mainWindowState.SectionsOrdered.Add(melodySectionName);
			}
			_cfgSectionExpanded[melodySectionName] = true;

			string melodyIfKeyName = string.Empty;
			if (!string.IsNullOrEmpty(condition))
			{
				string baseIfName = $"{melodyName} Melody";
				if (!TryEnsureBardIfEntry(baseIfName, condition, out melodyIfKeyName, out errorMessage))
				{
					return false;
				}
			}

			var bardSection = pd.Sections.GetSectionData("Bard");
			if (bardSection == null)
			{
				pd.Sections.AddSection("Bard");
				bardSection = pd.Sections.GetSectionData("Bard");
			}
			if (bardSection == null)
			{
				errorMessage = "Unable to access the [Bard] section.";
				return false;
			}

			string melodyIfEntry = string.IsNullOrEmpty(melodyIfKeyName)
				? melodyName
				: $"{melodyName}/Ifs|{melodyIfKeyName}";

			var melodyIfKey = bardSection.Keys.GetKeyData("MelodyIf");
			if (melodyIfKey == null)
			{
				bardSection.Keys.AddKey("MelodyIf", melodyIfEntry);
			}
			else
			{
				for (int i = melodyIfKey.ValueList.Count - 1; i >= 0; i--)
				{
					string existing = melodyIfKey.ValueList[i] ?? string.Empty;
					string existingName = existing.Split('/')[0].Trim();
					if (string.Equals(existingName, melodyName, StringComparison.OrdinalIgnoreCase))
					{
						melodyIfKey.ValueList.RemoveAt(i);
					}
				}
				if (!melodyIfKey.ValueList.Any(v => string.Equals(v ?? string.Empty, melodyIfEntry, StringComparison.OrdinalIgnoreCase)))
				{
					melodyIfKey.ValueList.Add(melodyIfEntry);
				}
			}

			mainWindowState.SelectedSection = melodySectionName;
			mainWindowState.SelectedKey = "Song";
			mainWindowState.SelectedValueIndex = 0;
			mainWindowState.ConfigIsDirty = true;

			successMessage = string.IsNullOrEmpty(melodyIfKeyName)
				? $"Melody '{melodyName}' created with {songs.Count} song(s)."
				: $"Melody '{melodyName}' created with {songs.Count} song(s) and IF '{melodyIfKeyName}'.";
			return true;
		}
		private static bool TryEnsureBardIfEntry(string baseName, string expression, out string actualKey, out string errorMessage)
		{
			var mainWindowState = _state.GetState<State_MainWindow>();

			actualKey = string.Empty;
			errorMessage = string.Empty;

			var pd = GetActiveCharacterIniData();
			if (pd == null)
			{
				errorMessage = "No character INI is currently loaded.";
				return false;
			}

			var ifsSection = pd.Sections.GetSectionData("Ifs");
			if (ifsSection == null)
			{
				pd.Sections.AddSection("Ifs");
				ifsSection = pd.Sections.GetSectionData("Ifs");
			}
			if (ifsSection == null)
			{
				errorMessage = "Unable to access the [Ifs] section.";
				return false;
			}

			string normalizedExpression = (expression ?? string.Empty).Trim();
			foreach (var key in ifsSection.Keys)
			{
				if (key.ValueList != null && key.ValueList.Any(v => string.Equals((v ?? string.Empty).Trim(), normalizedExpression, StringComparison.OrdinalIgnoreCase)))
				{
					actualKey = key.KeyName;
					return true;
				}
			}

			string baseKey = string.IsNullOrEmpty(baseName) ? "Melody If" : baseName.Trim();
			if (baseKey.Length == 0) baseKey = "Melody If";
			string unique = GenerateUniqueKey(ifsSection.Keys, baseKey);
			if (unique == null)
			{
				errorMessage = "Unable to generate a unique IF name.";
				return false;
			}

			ifsSection.Keys.AddKey(unique, normalizedExpression);
			actualKey = unique;
			mainWindowState.ConfigIsDirty = true;

			if (!mainWindowState.SectionsOrdered.Any(s => string.Equals(s, "Ifs", StringComparison.OrdinalIgnoreCase)))
			{
				mainWindowState.SectionsOrdered.Add("Ifs");
			}
			_cfgSectionExpanded["Ifs"] = true;

			return true;
		}
		private static string GenerateUniqueKey(KeyDataCollection keys, string baseName)
		{
			string unique = baseName;
			int idx = 1;
			while (keys.ContainsKey(unique))
			{
				unique = $"{baseName} ({idx})";
				idx++;
				if (idx > 1000)
				{
					return null;
				}
			}
			return unique;
		}

		// Toon picker modal for Heals section (Tank / Important Bot)
		private static void RenderToonPickerModal(SectionData selectedSection)
		{
			var mainWindowState = _state.GetState<State_MainWindow>();

			bool _open_toon = imgui_Begin(_state.WinName_ToonPickerModal, (int)(ImGuiWindowFlags.ImGuiWindowFlags_AlwaysAutoResize | ImGuiWindowFlags.ImGuiWindowFlags_NoDocking));
			if (_open_toon)
			{
				if (!string.IsNullOrEmpty(_cfgToonPickerStatus)) imgui_Text(_cfgToonPickerStatus);
				float h = 300f; float w = 420f;
				if (imgui_BeginChild("ToonList", w, h, 1, 0))
				{
					var list = _cfgToonCandidates ?? new List<string>();
					var kd = selectedSection?.Keys?.GetKeyData(mainWindowState.SelectedKey ?? string.Empty);
					var current = kd != null ? GetValues(kd) : new List<string>();
					int i = 0;
					foreach (var name in list)
					{
						string label = $"{name}##toon_{i}";
						bool already = current.Contains(name, StringComparer.OrdinalIgnoreCase);
						if (imgui_Selectable(label, false))
						{
							if (kd != null)
							{
								var vals = GetValues(kd);
								if (!vals.Contains(name, StringComparer.OrdinalIgnoreCase))
								{
									vals.Add(name);
									WriteValues(kd, vals);
								}
							}
						}
						if (already) { imgui_SameLine(); imgui_Text("(added)"); }
						i++;
					}
				}
				imgui_EndChild();
				if (imgui_Button("Close")) _state.Show_ToonPickerModal = false;
			}
			imgui_End();
			if (!_open_toon) _state.Show_ToonPickerModal = false;
		}

		// Spell Info modal (read-only details) using real ImGui tables + colored labels

		private static void RenderSpellInfoModal()
		{
			var s = _cfgSpellInfoSpell;
			if (s == null) { _state.Show_SpellInfoModal = false; return; }
			bool open = imgui_Begin(_state.WinName_SpellInfoModal, (int)(ImGuiWindowFlags.ImGuiWindowFlags_AlwaysAutoResize | ImGuiWindowFlags.ImGuiWindowFlags_NoDocking));
			if (open)
			{
				// Header with better styling
				imgui_TextColored(0.95f, 0.85f, 0.35f, 1.0f, $"{s.Name ?? string.Empty}");
				imgui_Separator();

				float width = Math.Max(520f, imgui_GetContentRegionAvailX());
				if (imgui_BeginTable("E3SpellInfoTable", 2, 0, width, 0))
				{
					try
					{
						imgui_TableSetupColumn("Property", 0, 140f);
						imgui_TableSetupColumn("Value", 0, Math.Max(260f, width - 160f));
						imgui_TableHeadersRow();
						if (!String.IsNullOrWhiteSpace(s.CastType))
						{
							imgui_TableNextRow();
							imgui_TableNextColumn();
							// Colored label (soft yellow)
							imgui_TextColored(0.95f, 0.85f, 0.35f, 1f, "Type");
							imgui_TableNextColumn();
							imgui_Text(s.CastType);
						}

						if (s.Level > 0)
						{
							imgui_TableNextRow();
							imgui_TableNextColumn();
							// Colored label (soft yellow)
							imgui_TextColored(0.95f, 0.85f, 0.35f, 1f, "Level");
							imgui_TableNextColumn();
							imgui_Text(s.Level.ToString());
						}
						if (s.Mana > 0)
						{
							imgui_TableNextRow();
							imgui_TableNextColumn();
							// Colored label (soft yellow)
							imgui_TextColored(0.95f, 0.85f, 0.35f, 1f, "Mana");
							imgui_TableNextColumn();
							imgui_Text(FormatWithSeparators(s.Mana));
						}
						if (s.CastTime > 0)
						{
							imgui_TableNextRow();
							imgui_TableNextColumn();
							// Colored label (soft yellow)
							imgui_TextColored(0.95f, 0.85f, 0.35f, 1f, "Cast Time");
							imgui_TableNextColumn();
							imgui_Text($"{s.CastTime:0.00}s");
						}
						if (s.Recast > 0)
						{
							imgui_TableNextRow();
							imgui_TableNextColumn();
							// Colored label (soft yellow)
							imgui_TextColored(0.95f, 0.85f, 0.35f, 1f, "Recast");
							imgui_TableNextColumn();
							imgui_Text(FormatMsSmart(s.Recast));
						}
						if (s.Range > 0)
						{
							imgui_TableNextRow();
							imgui_TableNextColumn();
							// Colored label (soft yellow)
							imgui_TextColored(0.95f, 0.85f, 0.35f, 1f, "Range");
							imgui_TableNextColumn();
							imgui_Text(s.Range.ToString("0"));
						}
						if (!String.IsNullOrWhiteSpace(s.TargetType))
						{
							imgui_TableNextRow();
							imgui_TableNextColumn();
							// Colored label (soft yellow)
							imgui_TextColored(0.95f, 0.85f, 0.35f, 1f, "Target");
							imgui_TableNextColumn();
							imgui_Text(s.TargetType);
						}
						if (!String.IsNullOrWhiteSpace(s.SpellType))
						{
							imgui_TableNextRow();
							imgui_TableNextColumn();
							// Colored label (soft yellow)
							imgui_TextColored(0.95f, 0.85f, 0.35f, 1f, "School");
							imgui_TableNextColumn();
							imgui_Text(s.SpellType);
						}

						if (!String.IsNullOrWhiteSpace(s.ResistType))
						{
							imgui_TableNextRow();
							imgui_TableNextColumn();
							// Colored label (soft yellow)
							imgui_TextColored(0.95f, 0.85f, 0.35f, 1f, "Resist");
							imgui_TableNextColumn();
							imgui_Text(s.ResistType);
						}
					}
					finally
					{
						imgui_EndTable();
					}
				}

				if (!string.IsNullOrEmpty(s.Description))
				{
					imgui_Separator();
					imgui_TextColored(0.75f, 0.85f, 1.0f, 1f, "Description:");
					imgui_Text(s.Description);
				}

				imgui_Separator();

				if (imgui_Button("Close"))
				{
					_state.Show_SpellInfoModal = false;
					_cfgSpellInfoSpell = null;
				}
			}
			imgui_End();
			if (!_state.Show_SpellInfoModal) { _cfgSpellInfoSpell = null; }
		}

		private static void RenderDonateModal()
		{
			
			bool open = imgui_Begin(_state.WinName_Donate, (int)(ImGuiWindowFlags.ImGuiWindowFlags_AlwaysAutoResize | ImGuiWindowFlags.ImGuiWindowFlags_NoDocking));
			if (open)
			{
				imgui_TextColored(0.9f, 0.9f, 0.6f, 1.0f, "Hi, Ty for thinking of donating!\nIf you wish to donate, please use friends and family.");
				imgui_Separator();

				// Buttons centered horizontally
				float avail = imgui_GetContentRegionAvailX();
				float yesW = 60f;
				float noW = 60f;
				float spacing = 8f;
				float total = yesW + spacing + noW;
				if (total < avail)
				{
					imgui_SameLineEx((avail - total) / 2f, 0f);
				}
				if (imgui_Button("Yes"))
				{
					e3util.OpenUrl("https://www.paypal.com/paypalme/RekkaSoftware");
					_state.Show_Donate = false;
				}
				imgui_SameLine();
				if (imgui_Button("No"))
				{
					_state.Show_Donate = false;
				}
			}
			imgui_End();
			if (!open)
			{
				_state.Show_Donate = false;
			}
		}
		private class E3Spell
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
		private class SpellValueEditState
		{
			public string Section = string.Empty;
			public string Key = string.Empty;
			public int ValueIndex = -1;
			public string OriginalValue = string.Empty;
			public string BaseName = string.Empty;
			public string CastTarget = string.Empty;
			public Dictionary<string, string> KeyValues = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
			public Dictionary<string, string> OriginalKeyNames = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
			public HashSet<string> Flags = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			public bool Enabled = true;
			public List<string> UnknownSegments = new List<string>();

			public string GetValue(string key)
			{
				return KeyValues.TryGetValue(key, out var value) ? value : string.Empty;
			}


			public void SetValue(string key, string value)
			{
				if (string.IsNullOrWhiteSpace(value))
				{
					KeyValues.Remove(key);
				}
				else
				{
					KeyValues[key] = value.Trim();
				}
			}

			public void RememberAlias(string canonicalKey, string originalKey)
			{
				if (string.IsNullOrWhiteSpace(canonicalKey) || string.IsNullOrWhiteSpace(originalKey)) return;
				if (!OriginalKeyNames.ContainsKey(canonicalKey))
				{
					OriginalKeyNames[canonicalKey] = originalKey;
				}
			}

			public string GetOutputKey(string canonicalKey)
			{
				if (OriginalKeyNames.TryGetValue(canonicalKey, out var alias) && !string.IsNullOrWhiteSpace(alias))
				{
					return alias;
				}
				return canonicalKey;
			}

			public bool HasFlag(string flag) => Flags.Contains(flag);

			public void SetFlag(string flag, bool enabled)
			{
				if (enabled)
				{
					Flags.Add(flag);
				}
				else
				{
					Flags.Remove(flag);
				}
			}
		}
	}
}
