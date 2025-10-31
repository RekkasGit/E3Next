using E3Core.Data;
using E3Core.Processors;
using E3Core.Settings;
using E3Core.Utility;
using IniParser.Model;
using MonoCore;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Web.SessionState;
using System.Text.RegularExpressions;
using static MonoCore.E3ImGUI;

namespace E3Core.UI.Windows
{

	public static class CharacterSettingsWindow
	{
		public static Logging _log = E3.Log;
		private static IMQ MQ = E3.MQ;
		private static ISpawns _spawns = E3.Spawns;

		//A very large bandaid on the Threading of this window
		//used when trying to get a pointer to the _cfg objects.
		private static object _dataLock = new object();

		#region Variables
			// Catalogs and Add modal state

		//Note on Volatile variables... all this means is if its set on another thread, we will eventually get the update.
		//its somewhat one way, us setting the variable on this side doesn't let the other thread see the update.
		private static volatile bool _cfg_GemsAvailable = false; // Whether we have gem data
		private static volatile bool _cfg_CatalogsReady = false;
		private static volatile bool _cfg_CatalogLoadRequested = false;
		private static volatile bool _cfg_CatalogLoading = false;

		private static string _cfg_CatalogStatus = string.Empty;
		private static string _cfg_CatalogSource = "Unknown"; // "Local", "Remote (ToonName)", or "Unknown"
																 // Memorized gem data from catalog responses with spell icon support

		private static string[] _cfg_CatalogGems = new string[12]; // Gem data from catalog response
		private static int[] _cfg_CatalogGemIcons = new int[12]; // Spell icon indices for gems

		/// <summary>
		///Data organized into Category, Sub Category, List of Spells.
		///always get a pointer to these via the method GetCatalogByType
		/// </summary>
		private static SortedDictionary<string, SortedDictionary<string, List<E3Spell>>> _cfgSpells = new SortedDictionary<string, SortedDictionary<string, List<E3Spell>>>();
		private static SortedDictionary<string, SortedDictionary<string, List<E3Spell>>> _cfgAAs = new SortedDictionary<string, SortedDictionary<string, List<E3Spell>>>();
		private static SortedDictionary<string, SortedDictionary<string, List<E3Spell>>> _cfgDiscs = new SortedDictionary<string, SortedDictionary<string, List<E3Spell>>>();
		private static SortedDictionary<string, SortedDictionary<string, List<E3Spell>>> _cfgSkills = new SortedDictionary<string, SortedDictionary<string, List<E3Spell>>>();
		private static SortedDictionary<string, SortedDictionary<string, List<E3Spell>>> _cfgItems = new SortedDictionary<string, SortedDictionary<string, List<E3Spell>>>();
	
		private static E3Spell _cfgCatalogInfoSpell = null;
		private static bool _cfgShowSpellInfoModal = false;
		private static E3Spell _cfgSpellInfoSpell = null;

		private enum AddType { Spells, AAs, Discs, Skills, Items }
		private enum CatalogMode { Standard, BardSong }
		private static bool _cfgShowAddModal = false;
		private static CatalogMode _cfgCatalogMode = CatalogMode.Standard;
		private static AddType _cfgAddType = AddType.Spells;
		private static string _cfgAddCategory = string.Empty;
		private static string _cfgAddSubcategory = string.Empty;
		private static string _cfgAddFilter = string.Empty;

		// Food/Drink picker state
		private static bool _cfgShowFoodDrinkModal = false;
		private static string _cfgFoodDrinkKey = string.Empty; // "Food" or "Drink"
		private static string _cfgFoodDrinkStatus = string.Empty;
		private static List<string> _cfgFoodDrinkCandidates = new List<string>();
		private static bool _cfgFoodDrinkScanRequested = false;
		// Toon picker (Heals: Tank / Important Bot)
		private static bool _cfgShowToonPickerModal = false;
		private static string _cfgToonPickerStatus = string.Empty;
		private static List<string> _cfgToonCandidates = new List<string>();
		// Append If modal state
		private static bool _cfgShowIfAppendModal = false;
		private static int _cfgIfAppendRow = -1;
		private static List<string> _cfgIfAppendCandidates = new List<string>();
		private static string _cfgIfAppendStatus = string.Empty;
		// Ifs import (sample) modal state
		private static bool _cfgShowIfSampleModal = false;
		private static List<System.Collections.Generic.KeyValuePair<string, string>> _cfgIfSampleLines = new List<System.Collections.Generic.KeyValuePair<string, string>>();
		private static string _cfgIfSampleStatus = string.Empty;
		// Ifs: add-new helper input buffers
		private static string _cfgNewKeyBuffer = string.Empty;
		private static string _cfgNewValue = string.Empty;
		private static bool _cfgShowBardMelodyHelper = false;
		private static string _cfgBardMelodyName = string.Empty;
		private static List<string> _cfgBardMelodySongs = new List<string>();
		private static Dictionary<int, string> _cfgBardMelodyBuffers = new Dictionary<int, string>();
		private static List<int> _cfgBardMelodyGems = new List<int>();
		private static Dictionary<int, string> _cfgBardMelodyGemBuffers = new Dictionary<int, string>();
		private static string _cfgBardMelodyCondition = string.Empty;
		private static string _cfgBardMelodyModalStatus = string.Empty;
		private static string _cfgBardMelodyStatus = string.Empty;
		private static int _cfgBardSongPickerIndex = -1;
		private static bool _cfgBardSongPickerJustSelected = false;
		private static int _cfgBardSongInputVersion = 0;
		private static int _cfgBardConditionInputVersion = 0;
		private static int _cfgSectionSearchVersion = 0;
		private static bool _cfgShowBardSampleIfModal = false;
		private static string _cfgBardSampleIfStatus = string.Empty;
		private static string _cfgBardSampleIfFilter = string.Empty;
		private static List<KeyValuePair<string, string>> _cfgBardSampleIfLines = new List<KeyValuePair<string, string>>();
		// Burn: add-new helper input buffers
		private static string _cfgBurnNewKey = string.Empty;
		private static string _cfgBurnNewValue = string.Empty;
		// Context menu state for Ifs/Burn sections
		private static bool _showContextMenu = false;
		private static string _contextMenuFor = string.Empty; // "Ifs" or "Burn"
		// Inline add editor state (rendered in Values column)
		private static bool _cfgShowAddInline = false;
		private static string _cfgAddInlineSection = string.Empty; // "Ifs" or "Burn"
		// Remote fetch state (non-blocking)
		private static bool _cfgFoodDrinkPending = false;
		private static string _cfgFoodDrinkPendingToon = string.Empty;
		private static string _cfgFoodDrinkPendingType = string.Empty;
		private static long _cfgFoodDrinkTimeoutAt = 0;

		// Config UI toggle: "/e3imgui".
		private static readonly string _e3ImGuiWindow = "E3Next Config";
		private static bool _imguiInitDone = false;
		private static bool _imguiContextReady = false;
		private enum SettingsTab { Character, General, Advanced }
		private static SettingsTab _activeSettingsTab = SettingsTab.Character;
		private static string _activeSettingsFilePath = string.Empty;
		private static string[] _activeSettingsFileLines = Array.Empty<string>();
		private static long _nextIniRefreshAtMs = 0;
		private static string _selectedCharacterSection = string.Empty;
		private static Dictionary<string, string> _charIniEdits = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		private static List<string> _cfgSectionsOrdered = new List<string>();
		private static string _cfg_LastIniPath = string.Empty;
		private static string _cfgSelectedSection = string.Empty;
		private static string _cfgSelectedKey = string.Empty; // subsection/key
		private static string _cfgSectionSearch = string.Empty;
		private static int _cfgSelectedValueIndex = -1;
		private static bool _cfg_Dirty = false;
		// Inline edit helpers
		private static int _cfgInlineEditIndex = -1;
		private static string _cfgInlineEditBuffer = string.Empty;
		private static int _cfgPendingValueSelection = -1;
		private static string _cfgSelectedClass = string.Empty;
		private const float _valueRowActionStartOffset = 46f;
		private static bool _cfgCatalogReplaceMode = false;
		private static int _cfgCatalogReplaceIndex = -1;

		// Collapsible section state tracking
		private static Dictionary<string, bool> _cfgSectionExpanded = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
	
		// Spell flag editor state
		private static SpellValueEditState _cfgSpellEditState = null;
		private static string _cfgSpellEditSignature = string.Empty;
		private static bool _cfgShowSpellModifierModal = false;
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
		private static bool _cfgAllPlayersView = false; // aggregated view
		private static string _cfgAllPlayersSig = string.Empty; // section::key
		private static long _cfgAllPlayersNextRefreshAtMs = 0;
		private static List<System.Collections.Generic.KeyValuePair<string, string>> _cfgAllPlayersRows = new List<System.Collections.Generic.KeyValuePair<string, string>>();
		private static Dictionary<string, string> _cfgAllPlayersServerByToon = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		private static Dictionary<string, string> _cfgAllPlayersEditBuf = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		private static readonly object _cfgAllPlayersLock = new object();
		private static bool _cfgAllPlayersRefreshRequested = false;
		private static bool _cfgAllPlayersRefreshing = false;
		private static string _cfgAllPlayersReqSection = string.Empty;
		private static string _cfgAllPlayersReqKey = string.Empty;
		private static long _cfgAllPlayersLastUpdatedAt = 0;
		private static int _cfgAllPlayersRefreshIntervalMs = 5000;
		private static string _cfgAllPlayersStatus = string.Empty;

		// Character .ini selection state
		private static string _selectedCharIniPath = string.Empty; // defaults to current character
		private static IniData _selectedCharIniParsedData = null;  // parsed data for non-current selection
		private static string[] _charIniFiles = Array.Empty<string>();
		private static bool _hideOfflineCharInis = false;
		private static long _nextIniFileScanAtMs = 0;
		// Dropdown support (feature-detect combo availability to avoid crashes on older MQ2Mono)
		private static bool _comboAvailable = true;
		private static bool _showThemeSettings = false;
		private static bool _showDonateModal = false;
		private static bool _cfg_Inited = false;

		#endregion
		static string _versionInfo = String.Empty;
		[SubSystemInit]
		public static void CharacterSettingsWindow_Init()
		{
			_versionInfo = $"nE³xt v{Setup.E3Version} | Build {Setup.BuildDate}";
			// Toggle the in-game ImGui config window
			EventProcessor.RegisterCommand("/e3imgui", (x) =>
			{
				try
				{
					if (Core._MQ2MonoVersion < 0.35m)
					{
						Core.mqInstance.Write("MQ2Mono Version needs to be at least 0.35 to use this command");
						return;
					}
					//we are already on the main C# thread, so we can just toggle this.
					ToggleImGuiWindow();
				}
				catch (Exception ex)
				{
					MQ.Write($"ImGui error: {ex.Message}");
				}
			}, "Toggle E3Next ImGui window");


			E3ImGUI.RegisterWindow(_e3ImGuiWindow, () => RenderIMGUI());

		}

		[ClassInvoke(Data.Class.All)]
		public static void Process()
		{
			ProcessBackgroundWork();
		}
		public static void ToggleImGuiWindow()
		{
			try
			{
				bool open = imgui_Begin_OpenFlagGet(_e3ImGuiWindow);
				imgui_Begin_OpenFlagSet(_e3ImGuiWindow, !open);
				_log.WriteDelayed($"E3 ImGui window {(!open ? "opened" : "closed")}", Logging.LogLevels.Debug);
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
		
		private static void RenderIMGUI()
		{
			try
			{
				// Early exit if ImGui functions aren't available
				if (!_imguiContextReady && _imguiInitDone)
				{
					return; // ImGui failed to initialize, skip rendering
				}

				// Initialize window visibility once (default hidden)
				if (!_imguiInitDone)
				{
					try
					{
						imgui_Begin_OpenFlagSet(_e3ImGuiWindow, false);
						_imguiContextReady = true; // Mark as ready after first successful ImGui call
						E3.Log.Write("ImGui initialized successfully", Logging.LogLevels.Info);
					}
					catch (Exception ex)
					{
						//E3.Log.Write($"ImGui initialization failed: {ex.Message}", Logging.LogLevels.Error);
						_imguiContextReady = false; // Mark as failed
						_imguiInitDone = true;
						return; // Exit early to prevent further ImGui calls
					}
					_imguiInitDone = true;
				}

				// Only render if ImGui is available and ready
				if (_imguiContextReady && imgui_Begin_OpenFlagGet(_e3ImGuiWindow))
				{
					// Apply current theme
					E3ImGUI.PushCurrentTheme();
					imgui_Begin(_e3ImGuiWindow, (int)ImGuiWindowFlags.ImGuiWindowFlags_None);
					try
					{

					// Header bar: version text on left, buttons on right
					if (imgui_BeginTable("HeaderBar", 2, (int)ImGuiTableFlags.ImGuiTableFlags_SizingStretchProp, imgui_GetContentRegionAvailX()))
					{
						try
						{
						imgui_TableSetupColumn("Left", 0, 0.70f);
						imgui_TableSetupColumn("Right", 0, 0.30f);
						imgui_TableNextRow();

						// Left: version/build text
						imgui_TableNextColumn();
						imgui_TextColored(0.95f, 0.85f, 0.35f, 1.0f, _versionInfo);

						// Right: buttons aligned to the right within the cell
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
						// Donate button (opens confirmation modal)
						if (imgui_Button("Donate"))
						{
							_showDonateModal = true;
						}
						imgui_SameLine();
						if (imgui_Button("Theme"))
						{
							_showThemeSettings = !_showThemeSettings;
						}
						imgui_SameLine();
						if (imgui_Button("Close"))
						{
							imgui_Begin_OpenFlagSet(_e3ImGuiWindow, false);
						}
						}
						finally
						{
							imgui_EndTable();
						}
					}

					imgui_Separator();

					// Character INI selector (used by Config Editor)
					RenderCharacterIniSelector();

					imgui_Separator();

					// All Players View toggle with better styling
					imgui_Text("Search:");
					imgui_SameLine();
					imgui_SetNextItemWidth(Math.Max(200f, imgui_GetContentRegionAvailX() * 0.2f));
					string searchId = $"##cfgSectionSearch_{_cfgSectionSearchVersion}";
					string sectionSearchBefore = _cfgSectionSearch ?? string.Empty;
					if (imgui_InputText(searchId, sectionSearchBefore))
					{
						_cfgSectionSearch = (imgui_InputText_Get(searchId) ?? string.Empty).Trim();
					}
					imgui_SameLine();
					if (imgui_Button("Clear"))
					{
						_cfgSectionSearch = string.Empty;
						_cfgSectionSearchVersion++;
					}
					imgui_SameLine();
					imgui_Text("View Mode:");
					imgui_SameLine();
					if (imgui_Button(_cfgAllPlayersView ? "Switch to Character View" : "Switch to All Players View"))
					{
						_cfgAllPlayersView = !_cfgAllPlayersView;
					}
					imgui_SameLine();
					imgui_TextColored(0.3f, 0.8f, 0.3f, 1.0f, _cfgAllPlayersView ? "All Players Mode" : "Character Mode");

					imgui_Separator();

					if (_cfgAllPlayersView)
					{
						string currentSig = $"{_cfgSelectedSection}::{_cfgSelectedKey}";
						if (!string.Equals(currentSig, _cfgAllPlayersSig, StringComparison.OrdinalIgnoreCase))
						{
							_cfgAllPlayersSig = currentSig;
							lock (_cfgAllPlayersLock)
							{
								_cfgAllPlayersRows = new List<KeyValuePair<string, string>>();
								_cfgAllPlayersEditBuf = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
							}
							_cfgAllPlayersRefreshRequested = true;
						}
					}

					// Config Editor only
					if (_cfgAllPlayersView)
					{
						RenderAllPlayersView();
					}
					else
					{
						RenderConfigEditor();
					}


					// Render theme settings modal if open
					if (_showThemeSettings)
					{
						RenderThemeSettingsModal();
					}
					// Render donate modal if open
					if (_showDonateModal)
					{
						RenderDonateModal();
					}
					}
					finally
					{
						imgui_End();
						PopCurrentTheme();
					}
				}
			}
			catch (Exception ex)
			{
				_log.Write($"OnUpdateImGui error: {ex.Message}", Logging.LogLevels.Error);
			}

		}
		private static void RenderConfigEditor()
		{
			EnsureConfigEditorInit();

			var pd = GetActiveCharacterIniData();
			if (pd == null || pd.Sections == null)
			{
				imgui_TextColored(1.0f, 0.8f, 0.8f, 1.0f, "No character INI loaded.");
				return;
			}

			// Catalog status / loader with better styling
			if (!_cfg_CatalogsReady)
			{
				imgui_TextColored(1.0f, 0.9f, 0.3f, 1.0f, "Catalog Status");

				if (_cfg_CatalogLoading)
				{
					imgui_Text(string.IsNullOrEmpty(_cfg_CatalogStatus) ? "Loading catalogs..." : _cfg_CatalogStatus);
				}
				else
				{
					imgui_Text(string.IsNullOrEmpty(_cfg_CatalogStatus) ? "Catalogs not loaded" : _cfg_CatalogStatus);
					imgui_SameLine();
					if (imgui_Button("Load Catalogs"))
					{
						_cfg_CatalogLoadRequested = true;
						_cfg_CatalogStatus = "Queued catalog load...";
					}
				}
				imgui_Separator();
			}

			// Rebuild sections order when ini path changes
			string activeIniPath = GetActiveSettingsPath() ?? string.Empty;
			if (!string.Equals(activeIniPath, _cfg_LastIniPath, StringComparison.OrdinalIgnoreCase))
			{
				_cfg_LastIniPath = activeIniPath;
				_cfgSelectedSection = string.Empty;
				_cfgSelectedKey = string.Empty;
				_cfgSelectedValueIndex = -1;
				BuildConfigSectionOrder();
				// Auto-load catalogs on ini switch without blocking UI
				_cfg_CatalogsReady = false;
				_cfgSpells.Clear();
				_cfgAAs.Clear();
				_cfgDiscs.Clear();
				_cfgSkills.Clear();
				_cfgItems.Clear();
				_cfg_CatalogLoadRequested = true;
				_cfg_CatalogStatus = "Queued catalog load...";
			}

			// Use ImGui Table for responsive 3-column layout
			float availY = imgui_GetContentRegionAvailY();

			SectionData activeSection = null;
			if (imgui_BeginTable("ConfigEditorTable", 3,
				(int)(ImGuiTableFlags.ImGuiTableFlags_Borders |
					 ImGuiTableFlags.ImGuiTableFlags_Resizable |
					 ImGuiTableFlags.ImGuiTableFlags_SizingStretchProp |
					 ImGuiTableFlags.ImGuiTableFlags_NoPadInnerX |
					 ImGuiTableFlags.ImGuiTableFlags_NoPadOuterX),
				imgui_GetContentRegionAvailX()))
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
				if (imgui_TableNextColumn())
				{
					if (imgui_BeginChild("SectionsTree", 0, Math.Max(200f, availY * 0.75f), false))
					{
						try
						{
						// Use a 1-column table with RowBg to get built-in alternating backgrounds
						int tableFlags = (int)(ImGuiTableFlags.ImGuiTableFlags_RowBg | ImGuiTableFlags.ImGuiTableFlags_SizingStretchProp);
						if (imgui_BeginTable("SectionsTreeTable", 1, tableFlags, imgui_GetContentRegionAvailX()))
						{
							try
							{
								imgui_TableSetupColumn("Section", 0, 0);
								var sectionsToRender = GetSectionsForDisplay();
								if (sectionsToRender.Count == 0)
								{
									imgui_TableNextRow();
									imgui_TableNextColumn();
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

										imgui_TableNextRow();
										imgui_TableNextColumn();

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
											_cfgSelectedSection = sec;
											_cfgSelectedKey = string.Empty;
											_cfgShowAddInline = false;
											_cfgAddInlineSection = string.Empty;
											_cfgNewKeyBuffer = string.Empty;
											_cfgNewValue = string.Empty;
										}

										if (sec.Equals("Ifs", StringComparison.OrdinalIgnoreCase) || sec.Equals("Burn", StringComparison.OrdinalIgnoreCase))
										{
											if (itemHovered && imgui_IsMouseClicked(1))
											{
												_showContextMenu = true;
												_contextMenuFor = sec;
												_cfgSelectedSection = sec;
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
													_cfgShowAddInline = true;
													_cfgAddInlineSection = "Ifs";
													_cfgSelectedSection = "Ifs";
													_cfgSelectedKey = string.Empty;
													_cfgSelectedValueIndex = -1;
													_cfgNewKeyBuffer = string.Empty;
													_cfgNewValue = string.Empty;
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
													_cfgShowAddInline = true;
													_cfgAddInlineSection = "Burn";
													_cfgSelectedSection = "Burn";
													_cfgSelectedKey = string.Empty;
													_cfgSelectedValueIndex = -1;
													_cfgBurnNewKey = string.Empty;
													_cfgBurnNewValue = string.Empty;
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
												imgui_TableNextRow();
												imgui_TableNextColumn();

												bool keySelected = string.Equals(_cfgSelectedSection, sec, StringComparison.OrdinalIgnoreCase) &&
													string.Equals(_cfgSelectedKey, key, StringComparison.OrdinalIgnoreCase);

												string keyLabel = $"  {key}"; // simple indent under section
												if (imgui_Selectable(keyLabel, keySelected))
												{
													_cfgSelectedSection = sec;
													_cfgSelectedKey = key;
													_cfgSelectedValueIndex = -1;
													_cfgShowAddInline = false;
													_cfgAddInlineSection = string.Empty;
													_cfgNewKeyBuffer = string.Empty;
													_cfgNewValue = string.Empty;
												}

												if ((sec.Equals("Ifs", StringComparison.OrdinalIgnoreCase) || sec.Equals("Burn", StringComparison.OrdinalIgnoreCase)) &&
													imgui_BeginPopupContextItem(null, 1))
												{
													if (imgui_MenuItem("Delete"))
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
						finally
						{
							imgui_EndChild();
						}
					}
				}

				// Column 2: Values
				if (imgui_TableNextColumn())
				{
					var selectedSection = pd.Sections.GetSectionData(_cfgSelectedSection ?? string.Empty);
					//_log.Write($"Rendering with selected section {selectedSection.SectionName} with keys count:{selectedSection.Keys.Count} with pd:");
					if (imgui_BeginChild("ValuesPanel", 0, Math.Max(200f, availY * 0.75f), false))
					{
						try
						{
						if (selectedSection == null)
						{
							imgui_Text("No section selected.");
						}
						else if (selectedSection.Keys == null || selectedSection.Keys.Count() == 0)
						{
							// Empty section: allow creating a new key directly here
							imgui_TextColored(0.8f, 0.9f, 0.95f, 1.0f, $"[{_cfgSelectedSection}] (empty)");
							imgui_Separator();
							imgui_Text("Create new entry:");
							imgui_SameLine();
							imgui_SetNextItemWidth(220f);
							if (imgui_InputText("##new_key_name", _cfgNewKeyBuffer))
							{
								_cfgNewKeyBuffer = imgui_InputText_Get("##new_key_name") ?? string.Empty;
							}
							imgui_SameLine();
							if (imgui_Button("Add Key"))
							{
								string newKey = (_cfgNewKeyBuffer ?? string.Empty).Trim();
								if (newKey.Length > 0 && !selectedSection.Keys.ContainsKey(newKey))
								{
									selectedSection.Keys.AddKey(newKey, string.Empty);
									_cfgSelectedKey = newKey;
									_cfgNewKeyBuffer = string.Empty;
									_cfgInlineEditIndex = -1;
									// On next frame the normal values editor will show for the new key
								}
							}
						}
						// Inline Add New editor (triggered from header context menu)
						if (_cfgShowAddInline 
							&& string.Equals(_cfgAddInlineSection, _cfgSelectedSection, StringComparison.OrdinalIgnoreCase)
							&& string.IsNullOrEmpty(_cfgSelectedKey))
						{
						imgui_TextColored(0.8f, 0.9f, 0.95f, 1.0f, _cfgAddInlineSection.Equals("Ifs", StringComparison.OrdinalIgnoreCase) ? "Add New If" : "Add New Burn Key");
						imgui_Text("Name:");
						imgui_SameLine();
						float inlineFieldAvail = imgui_GetContentRegionAvailX();
						float inlineFieldWidth = Math.Max(320f, inlineFieldAvail * 0.45f);
						inlineFieldWidth = Math.Min(inlineFieldWidth, Math.Max(260f, inlineFieldAvail - 60f));
						imgui_SetNextItemWidth(inlineFieldWidth);
						if (imgui_InputText("##inline_new_key", _cfgNewKeyBuffer))
						{
							_cfgNewKeyBuffer = imgui_InputText_Get("##inline_new_key") ?? string.Empty;
						}
						imgui_Text("Value:");
						float inlineValueAvail = imgui_GetContentRegionAvailX();
						float inlineValueWidth = Math.Max(420f, inlineValueAvail * 0.70f);
						inlineValueWidth = Math.Min(inlineValueWidth, Math.Max(320f, inlineValueAvail - 80f));
						float inlineValueHeight = Math.Max(140f, imgui_GetTextLineHeightWithSpacing() * 6f);
						if (imgui_InputTextMultiline("##inline_new_value", _cfgNewValue ?? string.Empty, inlineValueWidth, inlineValueHeight))
						{
							_cfgNewValue = imgui_InputText_Get("##inline_new_value") ?? string.Empty;
						}
						if (imgui_Button("Add##inline_add"))
						{
							string key = (_cfgNewKeyBuffer ?? string.Empty).Trim();
								string val = _cfgNewValue ?? string.Empty;
								bool added = false;
								if (_cfgAddInlineSection.Equals("Ifs", StringComparison.OrdinalIgnoreCase))
								{
									added = AddIfToActiveIni(key, val);
								}
								else if (_cfgAddInlineSection.Equals("Burn", StringComparison.OrdinalIgnoreCase))
								{
									added = AddBurnToActiveIni(key, val);
								}
								if (added)
								{
									_cfgShowAddInline = false;
									_cfgAddInlineSection = string.Empty;
									_cfgNewKeyBuffer = string.Empty;
									_cfgNewValue = string.Empty;
									// Open blank value editor if value was empty
									_cfgInlineEditIndex = 0;
								}
							}
							imgui_SameLine();
							if (imgui_Button("Cancel##inline_cancel"))
							{
								_cfgShowAddInline = false;
								_cfgAddInlineSection = string.Empty;
								_cfgNewKeyBuffer = string.Empty;
								_cfgNewValue = string.Empty;
							}
							imgui_Separator();
						}

						else if (string.IsNullOrEmpty(_cfgSelectedKey))
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
							imgui_EndChild();
						}
					}
				}

				// Column 3: Tools and Info
				activeSection = pd.Sections.GetSectionData(_cfgSelectedSection ?? string.Empty);
				if (imgui_TableNextColumn())
				{
					if (imgui_BeginChild("ToolsPanel", 0, Math.Max(200f, availY * 0.75f), false))
					{
						try
						{
							RenderConfigurationTools(activeSection);
						}
						finally
						{
							imgui_EndChild();
						}
					}
				}
				}
				finally
				{
					imgui_EndTable();
				}
			}

			// Ensure popups/modals render even when the tools column is hidden
			RenderActiveModals(activeSection);

			// Display memorized spells if available from catalog data (safe)
			RenderCatalogGemData();
		}


		// Safe gem display using catalog data (no TLO queries from UI thread)
		private static void RenderCatalogGemData()
		{
			lock(_dataLock)
			{
				if (!_cfg_GemsAvailable || _cfg_CatalogGems == null) return;

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
				if (imgui_BeginTable("CatalogGems", 12, (int)(ImGuiTableFlags.ImGuiTableFlags_Borders | ImGuiTableFlags.ImGuiTableFlags_SizingStretchSame), imgui_GetContentRegionAvailX()))
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
						
						string spellName = MQ.Query<string>($"${{Spell[{spellID}]}}",false);

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
							if (_cfg_CatalogsReady)
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

		// Helper method to render values for the selected key
		private static void RenderSelectedKeyValues(SectionData selectedSection)
		{
			var kd = selectedSection.Keys.GetKeyData(_cfgSelectedKey ?? string.Empty);
			string raw = kd?.Value ?? string.Empty;
			var parts = GetValues(kd);

			// Title row with better styling
			imgui_TextColored(0.95f, 0.85f, 0.35f, 1.0f, $"[{_cfgSelectedSection}] {_cfgSelectedKey}");
			imgui_Separator();


			if (parts.Count == 0)
			{
				imgui_Text("(No values)");
				imgui_Separator();
			}

			// Enumerated options derived from key label e.g. "(Melee/Ranged/Off)"
			if (TryGetKeyOptions(_cfgSelectedKey, out var enumOpts))
			{
				string current = (raw ?? string.Empty).Trim();
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
							var selSec = pdAct.Sections.GetSectionData(_cfgSelectedSection);
							if (selSec != null && selSec.Keys.ContainsKey(_cfgSelectedKey))
							{
								var kdata = selSec.Keys.GetKeyData(_cfgSelectedKey);
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
			// Boolean fast toggle support → dropdown selector with better styling
			else if (IsBooleanConfigKey(_cfgSelectedKey, kd))
			{
				string current = (raw ?? string.Empty).Trim();
				// Derive allowed options from base E3 conventions
				List<string> baseOpts;
				var keyLabel = _cfgSelectedKey ?? string.Empty;
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
							var selSec = pdAct.Sections.GetSectionData(_cfgSelectedSection);
							if (selSec != null && selSec.Keys.ContainsKey(_cfgSelectedKey))
							{
								var kdata = selSec.Keys.GetKeyData(_cfgSelectedKey);
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

			// Values list with improved styling
			bool listChanged = false;
			imgui_TextColored(0.9f, 0.95f, 1.0f, 1.0f, "Configuration Values");

			for (int i = 0; i < parts.Count; i++)
			{
				string v = parts[i];
				bool editing = (_cfgInlineEditIndex == i);
				// Create a unique ID for this item that doesn't depend on its position in the list
				string itemUid = $"{_cfgSelectedSection}_{_cfgSelectedKey}_{i}_{(v ?? string.Empty).GetHashCode()}";

				if (!editing)
				{
					// Row with better styling and alignment
					imgui_Text($"{i + 1}.");
					imgui_SameLine(_valueRowActionStartOffset);

					bool canMoveUp = i > 0;
					bool canMoveDown = i < parts.Count - 1;

					void SwapAndMark(int fromIndex, int toIndex)
					{
						var pdAct = GetActiveCharacterIniData();
						var selSec = pdAct.Sections.GetSectionData(_cfgSelectedSection);
						var key = selSec?.Keys.GetKeyData(_cfgSelectedKey);
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
						_cfgInlineEditIndex = index;
						_cfgInlineEditBuffer = currentValue ?? string.Empty;
					}

					void DeleteValueAt(int index)
					{
						var pdAct = GetActiveCharacterIniData();
						var selSec = pdAct.Sections.GetSectionData(_cfgSelectedSection);
						var key = selSec?.Keys.GetKeyData(_cfgSelectedKey);
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

					// Edit button
					if (imgui_Button($"E##edit_{itemUid}"))
					{
						StartInlineEdit(i, v);
					}
					imgui_SameLine();

					// Delete button
					if (imgui_Button($"X##delete_{itemUid}"))
					{
						DeleteValueAt(i);
					}
					imgui_SameLine();

					// Make value selectable to show info in right panel
					bool isSelected = (_cfgSelectedValueIndex == i);
					if (imgui_Selectable($"{v}##select_{itemUid}", isSelected))
					{
						_cfgSelectedValueIndex = i;
					}
					if (imgui_BeginPopupContextItem($"ValueCtx_{itemUid}", 1))
					{
						if (canMoveUp && imgui_MenuItem("Move Up")) SwapAndMark(i, i - 1);
						if (canMoveDown && imgui_MenuItem("Move Down")) SwapAndMark(i, i + 1);
						if (imgui_MenuItem("Replace From Catalog"))
						{
							_cfgCatalogReplaceMode = true;
							_cfgCatalogReplaceIndex = i;
							_cfgShowAddModal = true;
						}
						if (imgui_MenuItem("Replace Inline"))
						{
							StartInlineEdit(i, v);
						}
						if (imgui_MenuItem("Delete"))
						{
							DeleteValueAt(i);
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
						var selSec = pdAct.Sections.GetSectionData(_cfgSelectedSection);
						var key = selSec?.Keys.GetKeyData(_cfgSelectedKey);
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
						_cfgInlineEditIndex = -1;
						_cfgInlineEditBuffer = string.Empty;
						// continue to render items; parts refresh handled below
					}
					imgui_SameLine();

					if (imgui_Button($"Cancel##cancel_{itemUid}"))
					{
						_cfgInlineEditIndex = -1;
						_cfgInlineEditBuffer = string.Empty;
					}
				}

				// If a change was made, we need to refresh the parts list for subsequent iterations
				if (listChanged)
				{
					// Re-get the values after modification
					var updatedKd = selectedSection.Keys.GetKeyData(_cfgSelectedKey ?? string.Empty);
					parts = GetValues(updatedKd);
					listChanged = false; // Reset the flag
					if (_cfgPendingValueSelection >= 0 && _cfgPendingValueSelection < parts.Count)
					{
						_cfgSelectedValueIndex = _cfgPendingValueSelection;
					}
					else
					{
						_cfgSelectedValueIndex = -1;
					}
					_cfgPendingValueSelection = -1;
					// Adjust the loop counter since we've removed an item
					i--;
				}
			}

			// Handle adding a new manual entry (if we're in add mode)
			if (_cfgInlineEditIndex >= parts.Count)
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
						var selSec = pdAct.Sections.GetSectionData(_cfgSelectedSection);
						var key = selSec?.Keys.GetKeyData(_cfgSelectedKey);
						if (key != null)
						{
							var vals = GetValues(key);
							vals.Add(newText.Trim());
							WriteValues(key, vals);
							_cfgPendingValueSelection = vals.Count - 1;
						}
					}
					_cfgInlineEditIndex = -1;
					_cfgInlineEditBuffer = string.Empty;
				}
				imgui_SameLine();

				if (imgui_Button($"Cancel##cancel_manual"))
				{
					_cfgInlineEditIndex = -1;
					_cfgInlineEditBuffer = string.Empty;
				}
			}
			// Add new value button (only show when not editing)
			else if (!listChanged && _cfgInlineEditIndex == -1)
			{
				imgui_Separator();
				imgui_TextColored(0.8f, 0.9f, 0.95f, 1.0f, "Add New Values");

				if (imgui_Button("Add Manual"))
				{
					_cfgInlineEditIndex = parts.Count;
					_cfgInlineEditBuffer = string.Empty;
				}
				imgui_SameLine();
				if (imgui_Button("Add From Catalog"))
				{
					_cfgCatalogMode = CatalogMode.Standard;
					_cfgShowAddModal = true;
				}
			}
		}

		// Helper method to render configuration tools panel
		private static void RenderConfigurationTools(SectionData selectedSection)
		{
			bool isBardIni = IsActiveIniBard();
			if (isBardIni)
			{
				imgui_TextColored(0.95f, 0.85f, 0.35f, 1.0f, "Bard Tools");
				if (imgui_Button("Open Melody Helper"))
				{
					ResetBardMelodyHelperForm();
					_cfgShowBardMelodyHelper = true;
				}
				if (!string.IsNullOrEmpty(_cfgBardMelodyStatus))
				{
					imgui_TextColored(0.7f, 0.9f, 0.7f, 1.0f, _cfgBardMelodyStatus);
				}
				imgui_Separator();
			}
			if (selectedSection == null)
			{
				imgui_TextColored(0.9f, 0.9f, 0.9f, 1.0f, "Select a configuration key to see available tools.");
				return;
			}
			bool hasKeySelected = !string.IsNullOrEmpty(_cfgSelectedKey);
			bool specialSectionAllowsNoKey = string.Equals(_cfgSelectedSection, "Ifs", StringComparison.OrdinalIgnoreCase) || string.Equals(_cfgSelectedSection, "Burn", StringComparison.OrdinalIgnoreCase);
			if (!hasKeySelected && !specialSectionAllowsNoKey)
			{
				imgui_TextColored(0.9f, 0.9f, 0.9f, 1.0f, "Select a configuration key to see available tools.");
				return;
			}

			imgui_TextColored(0.95f, 0.85f, 0.35f, 1.0f, "Configuration Tools");
			imgui_Separator();

			// Key actions (delete) — only when a key is selected
			if (hasKeySelected)
			{
				imgui_TextColored(0.95f, 0.75f, 0.75f, 1.0f, $"Selected: {_cfgSelectedKey}");
				// Styled red delete button
				imgui_PushStyleColor((int)ImGuiCol.Button, 0.85f, 0.30f, 0.30f, 1.0f);
				imgui_PushStyleColor((int)ImGuiCol.ButtonHovered, 0.95f, 0.40f, 0.40f, 1.0f);
				imgui_PushStyleColor((int)ImGuiCol.ButtonActive, 0.75f, 0.20f, 0.20f, 1.0f);
				if (imgui_Button("Delete Selected Key"))
				{
					DeleteKeyFromActiveIni(_cfgSelectedSection, _cfgSelectedKey);
				}
				imgui_PopStyleColor(3);
				imgui_Separator();
			}


			// Special section buttons
			bool isHeals = string.Equals(_cfgSelectedSection, "Heals", StringComparison.OrdinalIgnoreCase);
			bool isTankKey = string.Equals(_cfgSelectedKey, "Tank", StringComparison.OrdinalIgnoreCase);
			bool isImpKey = string.Equals(_cfgSelectedKey, "Important Bot", StringComparison.OrdinalIgnoreCase);

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
					_cfgShowToonPickerModal = true;
				}
			}

			if (_cfgSelectedKey.Equals("Food", StringComparison.OrdinalIgnoreCase) || _cfgSelectedKey.Equals("Drink", StringComparison.OrdinalIgnoreCase))
			{
				if (imgui_Button("Pick From Inventory"))
				{
					// Reset scan state so results don't carry over between Food/Drink
					_cfgFoodDrinkKey = _cfgSelectedKey; // "Food" or "Drink"
					_cfgFoodDrinkStatus = string.Empty;
					_cfgFoodDrinkCandidates.Clear();
					_cfgFoodDrinkScanRequested = true; // auto-trigger scan for new kind
					_cfgShowFoodDrinkModal = true;
				}
			}

			// Ifs section: add-new key helper
			if (string.Equals(_cfgSelectedSection, "Ifs", StringComparison.OrdinalIgnoreCase))
			{
				if (imgui_Button("Sample If's"))
				{
					try { LoadSampleIfsForModal(); _cfgShowIfSampleModal = true; }
					catch (Exception ex) { _cfgIfSampleStatus = "Load failed: " + (ex.Message ?? "error"); _cfgShowIfSampleModal = true; }
				}
			}

			// Burn section: add-new key helper
			if (string.Equals(_cfgSelectedSection, "Burn", StringComparison.OrdinalIgnoreCase))
			{
			}

			imgui_Separator();

			// Display selected value information
			if (_cfgSelectedValueIndex >= 0)
			{
				var kd = selectedSection?.Keys?.GetKeyData(_cfgSelectedKey ?? string.Empty);
				var values = GetValues(kd);
				if (_cfgSelectedValueIndex < values.Count)
				{
					string selectedValue = values[_cfgSelectedValueIndex];
					var editState = EnsureSpellEditState(_cfgSelectedSection, _cfgSelectedKey, _cfgSelectedValueIndex, selectedValue);
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
					if (_cfg_CatalogsReady)
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

					if (editState != null)
					{
						imgui_TextColored(0.75f, 0.9f, 1.0f, 1.0f, "Flags & Modifiers");

						// Highlight the button with pulsing color to draw attention
						float pulse = (float)Math.Abs(Math.Sin(DateTime.Now.Ticks / 3000000.0));
						float highlightR = 0.95f + (pulse * 0.05f);
						float highlightG = 0.75f + (pulse * 0.25f);
						float highlightB = 0.35f + (pulse * 0.15f);
						imgui_PushStyleColor((int)ImGuiCol.Button, highlightR, highlightG, highlightB, 1.0f);
						imgui_PushStyleColor((int)ImGuiCol.ButtonHovered, 1.0f, 0.85f, 0.45f, 1.0f);
						imgui_PushStyleColor((int)ImGuiCol.ButtonActive, 0.85f, 0.65f, 0.25f, 1.0f);
						imgui_PushStyleColor((int)ImGuiCol.Text, 0.1f, 0.1f, 0.1f, 1.0f); // Dark text for readability

						if (imgui_Button("Open Modal Editor"))
						{
							_cfgShowSpellModifierModal = true;
						}

						imgui_PopStyleColor(4);
						imgui_TextColored(0.7f, 0.8f, 0.9f, 1.0f, "Click to open the advanced spell/item modifier editor.");
					}
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
			if (_cfgShowAddModal)
			{
				RenderAddFromCatalogModal(GetActiveCharacterIniData(), selectedSection);
			}
			if (_cfgShowFoodDrinkModal)
			{
				RenderFoodDrinkPicker(selectedSection);
			}
			if (_cfgShowBardMelodyHelper)
			{
				RenderBardMelodyHelperModal();
			}
			if (_cfgShowBardSampleIfModal)
			{
				RenderBardSampleIfModal();
			}
			if (_cfgShowToonPickerModal)
			{
				RenderToonPickerModal(selectedSection);
			}
			if (_cfgShowSpellInfoModal)
			{
				RenderSpellInfoModal();
			}
			if (_cfgShowIfAppendModal)
			{
				RenderIfAppendModal(selectedSection);
			}
			if (_cfgShowIfSampleModal)
			{
				RenderIfsSampleModal();
			}
			if (_cfgShowSpellModifierModal)
			{
				RenderSpellModifierModal();
			}
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
				_cfg_Dirty = false;
				_nextIniRefreshAtMs = 0;
				_log.Write($"Saved changes to {Path.GetFileName(selectedPath)}");
			}
			catch (Exception ex)
			{
				_log.Write($"Failed to save: {ex.Message}");
			}
		}

		private static void ProcessBackground_UpdateRemotePlayer(string targetToon)
		{
		
			//have to make a network call and wait for a response. 
			System.Threading.Tasks.Task.Run(() =>
			{
				try
				{
					SortedDictionary<string, SortedDictionary<string, List<E3Spell>>> mapSpells = new SortedDictionary<string, SortedDictionary<string, List<E3Spell>>>(),
					mapAAs = new SortedDictionary<string, SortedDictionary<string, List<E3Spell>>>(),
					mapDiscs = new SortedDictionary<string, SortedDictionary<string, List<E3Spell>>>(),
					mapSkills = new SortedDictionary<string, SortedDictionary<string, List<E3Spell>>>(),
					mapItems = new SortedDictionary<string, SortedDictionary<string, List<E3Spell>>>();

					_log.WriteDelayed($"Fetching data (remote)", Logging.LogLevels.Debug);

					//_cfg_CatalogStatus = $"Loading catalogs from {targetToon}...";
					bool peerSuccess = true;

					peerSuccess &= TryFetchPeerSpellDataListPub(targetToon, "Spells", out var ps);
					if (peerSuccess) mapSpells = OrganizeCatalog(ps); else mapSpells = new SortedDictionary<string, SortedDictionary<string, List<E3Spell>>>();

					peerSuccess &= TryFetchPeerSpellDataListPub(targetToon, "AAs", out var pa);
					if (peerSuccess) mapAAs = OrganizeCatalog(pa); else mapAAs = new SortedDictionary<string, SortedDictionary<string, List<E3Spell>>>();

					peerSuccess &= TryFetchPeerSpellDataListPub(targetToon, "Discs", out var pd);
					if (peerSuccess) mapDiscs = OrganizeCatalog(pd); else mapDiscs = new SortedDictionary<string, SortedDictionary<string, List<E3Spell>>>();

					peerSuccess &= TryFetchPeerSpellDataListPub(targetToon, "Skills", out var pk);
					if (peerSuccess) mapSkills = OrganizeSkillsCatalog(pk); else mapSkills = new SortedDictionary<string, SortedDictionary<string, List<E3Spell>>>();

					peerSuccess &= TryFetchPeerSpellDataListPub(targetToon, "Items", out var pi);
					if (peerSuccess) mapItems = OrganizeItemsCatalog(pi); else mapItems = new SortedDictionary<string, SortedDictionary<string, List<E3Spell>>>();



					// Also try to fetch gem data
					if (peerSuccess && TryFetchPeerGemData(targetToon, out var gemData))
					{
						lock (_dataLock)
						{
							_cfg_CatalogGems = gemData;
							_cfg_GemsAvailable = true;
						}
					}
					else
					{
						lock (_dataLock)
						{
							_cfg_GemsAvailable = false;
						}
					}

					// If any peer fetch failed, fallback to local
					if (!peerSuccess)
					{
						_cfg_CatalogStatus = "Peer catalog fetch failed; using local.";
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
						_cfgSpells = mapSpells;
						_cfgAAs = mapAAs;
						_cfgDiscs = mapDiscs;
						_cfgSkills = mapSkills;
						_cfgItems = mapItems;
						_catalogLookups = new[]
						{
							(_cfgSpells, "Spell"),
							(_cfgAAs, "AA"),
							(_cfgDiscs, "Disc"),
							(_cfgSkills, "Skill"),
							(_cfgItems, "Item")
							};
					_cfg_CatalogsReady = true;
					_cfg_CatalogStatus = "Catalogs loaded.";
		
					}

				}
				catch (Exception ex)
				{
					_cfg_CatalogStatus = "Catalog load failed: " + (ex.Message ?? "error");
				}
				finally
				{
					_cfg_CatalogLoadRequested = false;
					_cfg_CatalogLoading = false;
				}
			});
		}
		private static void ProcessBackground_UpdateLocalPlayer()
		{
			SortedDictionary<string, SortedDictionary<string, List<E3Spell>>> mapSpells = new SortedDictionary<string, SortedDictionary<string, List<E3Spell>>>(),
					mapAAs = new SortedDictionary<string, SortedDictionary<string, List<E3Spell>>>(),
					mapDiscs = new SortedDictionary<string, SortedDictionary<string, List<E3Spell>>>(),
					mapSkills = new SortedDictionary<string, SortedDictionary<string, List<E3Spell>>>(),
					mapItems = new SortedDictionary<string, SortedDictionary<string, List<E3Spell>>>();

			_cfg_CatalogStatus = "Loading catalogs (local)...";
			_cfg_CatalogSource = "Local";

			_log.WriteDelayed($"Fetching data (local)", Logging.LogLevels.Debug);

			mapSpells = OrganizeLoadingCatalog(e3util.ListAllBookSpells());
			mapAAs = OrganizeLoadingCatalog(e3util.ListAllActiveAA());
			mapDiscs = OrganizeLoadingCatalog(e3util.ListAllDiscData());
			mapSkills = OrganizeLoadingSkillsCatalog(e3util.ListAllActiveSkills());
			mapItems = OrganizeLoadingItemsCatalog(e3util.ListAllItemWithClickyData());

			// Also collect local gem data with spell icon indices
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
					_cfg_GemsAvailable = true;
				}
			}
			catch(Exception ex)
			{
				_log.WriteDelayed($"Fetching data Error: {ex.Message}", Logging.LogLevels.Debug);
				_cfg_GemsAvailable = false;
			}

			_log.WriteDelayed($"Fetching data (local) Complete!", Logging.LogLevels.Debug);

			lock (_dataLock)
			{
				// Publish atomically
				_cfgSpells = mapSpells;
				_cfgAAs = mapAAs;
				_cfgDiscs = mapDiscs;
				_cfgSkills = mapSkills;
				_cfgItems = mapItems;
				_catalogLookups = new[]
				{
							(_cfgSpells, "Spell"),
							(_cfgAAs, "AA"),
							(_cfgDiscs, "Disc"),
							(_cfgSkills, "Skill"),
							(_cfgItems, "Item")
							};
				_cfg_CatalogsReady = true;
				_cfg_CatalogStatus = "Catalogs loaded.";
				_cfg_CatalogLoadRequested = false;
				_cfg_CatalogLoading = false;

			}
		}
		// Background worker tick invoked from E3.Process(): handle catalog loads and icon system
		private static void ProcessBackgroundWork()
		{
			if (_cfg_CatalogLoadRequested && !_cfg_CatalogLoading)
			{


			

			
				_cfg_CatalogLoading = true;
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
					_cfg_CatalogLoadRequested = false;
					_cfg_CatalogLoading = false;
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

			if (_cfgAllPlayersRefreshRequested && !_cfgAllPlayersRefreshing)
			{
				_cfgAllPlayersRefreshRequested = false; // consume the pending request before we start
				_cfgAllPlayersRefreshing = true;
				_cfgAllPlayersReqSection = _cfgSelectedSection;
				_cfgAllPlayersReqKey = _cfgSelectedKey;

				System.Threading.Tasks.Task.Run(() =>
				{
					try
					{
					
						_cfgAllPlayersStatus = "Refreshing...";
						
						var newRows = new List<KeyValuePair<string, string>>();
						string section = _cfgAllPlayersReqSection;
						string key = _cfgAllPlayersReqKey;

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

						lock (_cfgAllPlayersLock)
						{
							_cfgAllPlayersRows = newRows;
							_cfgAllPlayersEditBuf = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
							foreach (var row in newRows)
							{
								var toonKey = row.Key ?? string.Empty;
								_cfgAllPlayersEditBuf[toonKey] = row.Value ?? string.Empty;
							}
						}
						_cfgAllPlayersLastUpdatedAt = Core.StopWatch.ElapsedMilliseconds;
					}
					catch (Exception ex)
					{
						_cfgAllPlayersStatus = "Refresh failed: " + ex.Message;
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
		private static bool TryFetchPeerSpellDataListPub(string toon, string listKey, out Google.Protobuf.Collections.RepeatedField<SpellData> data)
		{
			data = new Google.Protobuf.Collections.RepeatedField<SpellData>();
			try
			{
				if (string.IsNullOrEmpty(toon)) return false;
				// Send request: CatalogReq-<Toon>
				E3Core.Server.PubServer.AddTopicMessage($"CatalogReq-{toon}", listKey);
				string topic = $"CatalogResp-{E3.CurrentName}-{listKey}";
				// Poll SharedDataClient.TopicUpdates for up to ~2s
				long end = Core.StopWatch.ElapsedMilliseconds + 4000;
				while (Core.StopWatch.ElapsedMilliseconds < end)
				{
					if (E3Core.Server.NetMQServer.SharedDataClient.TopicUpdates.TryGetValue(toon, out var topics)
						&& topics.TryGetValue(topic, out var entry))
					{
						string payload = entry.Data;
						int first = payload.IndexOf(':');
						int second = first >= 0 ? payload.IndexOf(':', first + 1) : -1;
						string b64 = (second > 0 && second + 1 < payload.Length) ? payload.Substring(second + 1) : payload;
						byte[] bytes = Convert.FromBase64String(b64);
						var list = SpellDataList.Parser.ParseFrom(bytes);
						data = list.Data;
						return true;
					}
					if (E3Core.Server.NetMQServer.SharedDataClient.TopicUpdates.TryGetValue(E3.CurrentName, out var topics2)
						&& topics2.TryGetValue(topic, out var entry2))
					{
						string payload = entry2.Data;
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
			}
			catch { }
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

			if(!e3util.ShouldCheck(ref _onlineToonsLastUpdate, _onlineToonsLastsUpdateInterval))
			{
				lock(_onlineToonsCache)
				{
					return _onlineToonsCache;

				}
			}
			lock(_onlineToonsCache)
			{
				_onlineToonsCache.Clear();

				try
				{
					var connected = E3Core.Server.NetMQServer.SharedDataClient?.UsersConnectedTo?.Keys;
					if (connected != null)
					{
						foreach (var name in connected)
						{
							if (!string.IsNullOrEmpty(name)) _onlineToonsCache.TryAdd(name,name);
						}
					}
				}
				catch { }

				if (!string.IsNullOrEmpty(E3.CurrentName)) _onlineToonsCache.TryAdd(E3.CurrentName,E3.CurrentName);


				return _onlineToonsCache;
			}
			
		}
		private static bool IsIniForOnlineToon(string iniPath, ConcurrentDictionary<string,string> onlineToons)
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

			if (_charIniFiles == null || _charIniFiles.Length == 0) return false;

			// Optional: prefer matches that also contain server in the filename
			_cfgAllPlayersServerByToon.TryGetValue(toon, out var serverHint);
			serverHint = serverHint ?? string.Empty;

			// Gather candidates: filename starts with "<Toon>_" or equals "<Toon>.ini"
			var candidates = new List<string>();
			foreach (var f in _charIniFiles)
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




		private static bool IsBooleanConfigKey(string key, KeyData kd)
		{
			if (kd == null) return false;
			// Heuristic: keys that are explicitly On/Off
			var v = (kd.Value ?? string.Empty).Trim();
			if (string.Equals(v, "On", StringComparison.OrdinalIgnoreCase) || string.Equals(v, "Off", StringComparison.OrdinalIgnoreCase))
				return true;
			// Common patterns
			if (key.IndexOf("Enable", StringComparison.OrdinalIgnoreCase) >= 0) return true;
			if (key.IndexOf("Use ", StringComparison.OrdinalIgnoreCase) == 0) return true;
			return false;
		}

		// Attempt to derive an explicit set of allowed options from the key label, e.g.
		// "Assist Type (Melee/Ranged/Off)" => ["Melee","Ranged","Off"]
		private static bool TryGetKeyOptions(string keyLabel, out List<string> options)
		{
			options = null;
			if (string.IsNullOrEmpty(keyLabel)) return false;
			int i = keyLabel.IndexOf('(');
			int j = keyLabel.IndexOf(')');
			if (i < 0 || j <= i) return false;
			var inside = keyLabel.Substring(i + 1, j - i - 1).Trim();
			// Only treat as options if slash-delimited list exists
			if (inside.IndexOf('/') < 0) return false;
			var parts = inside.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries)
							  .Select(x => x.Trim())
							  .Where(x => !string.IsNullOrEmpty(x))
							  .ToList();
			if (parts.Count <= 1) return false;
			// Heuristic: ignore numeric unit hints like "(in milliseconds)" or "(Pct)" or "(1+)"
			bool looksNumericHint = parts.Any(p => p.Any(char.IsDigit)) || parts.Any(p => p.Equals("Pct", StringComparison.OrdinalIgnoreCase)) || parts.Any(p => p.IndexOf("millisecond", StringComparison.OrdinalIgnoreCase) >= 0);
			if (looksNumericHint) return false;
			options = parts;
			return true;
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
		private static void RenderAddFromCatalogModal(IniData pd, SectionData selectedSection)
		{
			imgui_Begin_OpenFlagSet("Add From Catalog", true);
			bool _open_Add = imgui_Begin("Add From Catalog", (int)ImGuiWindowFlags.ImGuiWindowFlags_AlwaysAutoResize);
			if (_open_Add)
			{
				float totalW = Math.Max(880f, imgui_GetContentRegionAvailX());
				float listH = Math.Max(420f, imgui_GetContentRegionAvailY() * 0.8f);
				float thirdW = Math.Max(220f, totalW / 3.0f - 8.0f);

				// Header: type + filter + catalog source
				imgui_TextColored(0.95f, 0.85f, 0.35f, 1.0f, "Add From Catalog");
				if (_cfgCatalogMode == CatalogMode.BardSong)
				{
					_cfgAddType = AddType.Spells;
					imgui_SameLine();
					imgui_TextColored(0.7f, 0.9f, 0.7f, 1.0f, "(Select a song for the melody)");
				}
				else
				{
					imgui_SameLine();
					if (imgui_BeginCombo("##type", _cfgAddType.ToString(), 0))
					{
						foreach (AddType t in Enum.GetValues(typeof(AddType)))
						{
							bool sel = t == _cfgAddType;
							if (imgui_Selectable(t.ToString(), sel)) _cfgAddType = t;
						}
						EndComboSafe();
					}
				}
				imgui_SameLine();
				imgui_Text("Filter:");
				imgui_SameLine();
				if (imgui_InputText("##filter", _cfgAddFilter ?? string.Empty))
					_cfgAddFilter = imgui_InputText_Get("##filter") ?? string.Empty;

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
					_cfg_CatalogsReady = false;
					_cfgSpells.Clear();
					_cfgAAs.Clear();
					_cfgDiscs.Clear();
					_cfgSkills.Clear();
					_cfgItems.Clear();
					_cfg_CatalogLoadRequested = true;
					_cfg_CatalogStatus = "Queued catalog refresh...";
					_cfg_CatalogSource = "Refreshing...";
				}

				// Show catalog status if loading
				if (_cfg_CatalogLoading)
				{
					imgui_SameLine();
					imgui_TextColored(0.9f, 0.9f, 0.4f, 1.0f, _cfg_CatalogStatus.Replace("Loading catalogs", "Loading"));
				}

				imgui_Separator();

				// Resolve the catalog for the chosen type
				var src = GetCatalogByType(_cfgAddType);

				// -------- LEFT: Top-level categories --------
				if (imgui_BeginChild("TopLevelCats", thirdW, listH, true))
				{
					imgui_TextColored(0.9f, 0.95f, 1.0f, 1.0f, "Categories");
					var cats = src.Keys.ToList();
					cats.Sort(StringComparer.OrdinalIgnoreCase);
					int ci = 0;
					foreach (var c in cats)
					{
						bool sel = string.Equals(_cfgAddCategory, c, StringComparison.OrdinalIgnoreCase);
						string id = $"{c}##Cat_{_cfgAddType}_{ci}";
						if (imgui_Selectable(id, sel))
						{
							_cfgAddCategory = c;
							_cfgAddSubcategory = string.Empty; // reset mid level on cat change
						}
						ci++;
					}
				}
				imgui_EndChild();

				imgui_SameLine();

				// -------- MIDDLE: Subcategories for selected category --------
				if (imgui_BeginChild("SubCats", thirdW, listH, true))
				{
					imgui_TextColored(0.9f, 0.95f, 1.0f, 1.0f, "Subcategories");
					if (!string.IsNullOrEmpty(_cfgAddCategory) && src.TryGetValue(_cfgAddCategory, out var submap))
					{
						var subs = submap.Keys.ToList();
						subs.Sort(StringComparer.OrdinalIgnoreCase);
						int si = 0;
						foreach (var sc in subs)
						{
							bool sel = string.Equals(_cfgAddSubcategory, sc, StringComparison.OrdinalIgnoreCase);
							string id = $"{sc}##Sub_{_cfgAddType}_{_cfgAddCategory}_{si}";
							if (imgui_Selectable(id, sel)) _cfgAddSubcategory = sc;
							si++;
						}
					}
					else
					{
						imgui_Text("Select a category.");
					}
				}
				imgui_EndChild();

				imgui_SameLine();

				// -------- RIGHT: Entries (with Add / Info) --------
				if (imgui_BeginChild("EntryList", thirdW, listH, true))
				{
					imgui_TextColored(0.9f, 0.95f, 1.0f, 1.0f, "Entries");

					IEnumerable<E3Spell> entries = Enumerable.Empty<E3Spell>();
					if (!string.IsNullOrEmpty(_cfgAddCategory) && src.TryGetValue(_cfgAddCategory, out var submap2))
					{
						if (!string.IsNullOrEmpty(_cfgAddSubcategory) && submap2.TryGetValue(_cfgAddSubcategory, out var l))
							entries = l;
						else
							entries = submap2.Values.SelectMany(x => x);
					}

					string filter = (_cfgAddFilter ?? string.Empty).Trim();
					if (filter.Length > 0)
						entries = entries.Where(e => e.Name.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0);

					// stable ordering
					entries = entries.OrderByDescending(e => e.Level)
									 .ThenBy(e => e.Name, StringComparer.OrdinalIgnoreCase);

					int i = 0;
					foreach (var e in entries)
					{
						string uid = $"{_cfgAddType}_{_cfgAddCategory}_{_cfgAddSubcategory}_{i}";

						string addLabel = (_cfgCatalogMode == CatalogMode.BardSong) ? "Use" : "Add";
						if (imgui_Button($"{addLabel}##add_{uid}"))
						{
							if (_cfgCatalogMode == CatalogMode.BardSong)
							{
								ApplyBardSongSelection(e.Name ?? string.Empty);
							}
							else
							{
								var kd = selectedSection?.Keys?.GetKeyData(_cfgSelectedKey ?? string.Empty);
								if (kd != null)
								{
									var vals = GetValues(kd);
									string v = (e.Name ?? string.Empty).Trim();
									if (_cfgCatalogReplaceMode && _cfgCatalogReplaceIndex >= 0 && _cfgCatalogReplaceIndex < vals.Count)
									{
										vals[_cfgCatalogReplaceIndex] = v;
										WriteValues(kd, vals);
										_cfgPendingValueSelection = _cfgCatalogReplaceIndex;
										_cfgCatalogReplaceMode = false;
										_cfgCatalogReplaceIndex = -1;
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
						imgui_SameLine();

						// Info
						if (imgui_Button($"Info##info_{uid}"))
						{
							_cfgSpellInfoSpell = e;   // <- use the field that exists in your file
							_cfgShowSpellInfoModal = true;
						}
						imgui_SameLine();

						// Row text: show level + name (no ToDisplayString needed)
						imgui_Text($"[{e.Level}] {e.Name}");
						i++;
					}

					if (i == 0) imgui_Text("No entries found");
				}
				imgui_EndChild();

				imgui_Separator();
				// One-click bulk add of the currently visible entries
				if (_cfgCatalogMode == CatalogMode.Standard)
				{
					if (imgui_Button("Add All Visible"))
					{
						TryAddVisibleEntriesToSelectedKey(selectedSection);
					}
					imgui_SameLine();
				}
				if (imgui_Button("Close"))
				{
					_cfgShowAddModal = false;
					_cfgCatalogMode = CatalogMode.Standard;
					_cfgBardSongPickerIndex = -1;
				}
			}
			imgui_End();

			// If user clicked the X, reflect that in our show flag
			if (!_open_Add || !imgui_Begin_OpenFlagGet("Add From Catalog"))
			{
				_cfgShowAddModal = false;
				_cfgCatalogMode = CatalogMode.Standard;
				_cfgBardSongPickerIndex = -1;
				_cfgCatalogReplaceMode = false;
				_cfgCatalogReplaceIndex = -1;
			}
		}

		private static SortedDictionary<string, SortedDictionary<string, List<E3Spell>>> GetCatalogByType(AddType t)
		{
			lock(_dataLock)
			{
				switch (t)
				{
					case AddType.AAs: return _cfgAAs;
					case AddType.Discs: return _cfgDiscs;
					case AddType.Skills: return _cfgSkills;
					case AddType.Items: return _cfgItems;
					case AddType.Spells:
					default: return _cfgSpells;
				}
			}
		}
		private static (SortedDictionary<string, SortedDictionary<string, List<E3Spell>>>, string)[] _catalogLookups = new[]
		{
			(_cfgSpells, "Spell"),
			(_cfgAAs, "AA"),
			(_cfgDiscs, "Disc"),
			(_cfgSkills, "Skill"),
			(_cfgItems, "Item")
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
					imgui_TextWrapped($"Slot {i + 1}: {formattedEffect}");
				}
			}
		}

		// Append If modal: choose an If key to append to a specific row value
		private static void RenderIfAppendModal(SectionData selectedSection)
		{
			imgui_Begin_OpenFlagSet("Append If", true);
			bool _open_if = imgui_Begin("Append If", (int)ImGuiWindowFlags.ImGuiWindowFlags_AlwaysAutoResize);
			if (_open_if)
			{
				if (!string.IsNullOrEmpty(_cfgIfAppendStatus)) imgui_Text(_cfgIfAppendStatus);
				float h = 300f; float w = 520f;
				if (imgui_BeginChild("IfList", w, h, true))
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
								var kd = selectedSection?.Keys?.GetKeyData(_cfgSelectedKey ?? string.Empty);
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
							_cfgShowIfAppendModal = false;
							break;
						}
						i++;
					}
				}
				imgui_EndChild();
				if (imgui_Button("Close")) _cfgShowIfAppendModal = false;
			}
			imgui_End();
			if (!_open_if) _cfgShowIfAppendModal = false;
		}

		private static void RenderThemeSettingsModal()
		{
			imgui_Begin_OpenFlagSet("Theme Settings", true);
			bool modalOpen = imgui_Begin("Theme Settings", (int)ImGuiWindowFlags.ImGuiWindowFlags_AlwaysAutoResize);
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
				}
				imgui_SameLine();
				if (imgui_Button("+"))
				{
					_rounding = Math.Min(12f, _rounding + 1f);
					_roundingBuf = _rounding.ToString("0.0", CultureInfo.InvariantCulture);
					_roundingVersion++;
				}

				string roundingString = _rounding.ToString("0.0", CultureInfo.InvariantCulture);
				// Presets
				imgui_Text("Presets:");
				if (imgui_Button("0")) { _rounding = 0f; _roundingBuf = roundingString; _roundingVersion++; }
				imgui_SameLine();
				if (imgui_Button("3")) { _rounding = 3f; _roundingBuf = roundingString; _roundingVersion++; }
				imgui_SameLine();
				if (imgui_Button("6")) { _rounding = 6f; _roundingBuf = roundingString; _roundingVersion++; }
				imgui_SameLine();
				if (imgui_Button("9")) { _rounding = 9f; _roundingBuf = roundingString; _roundingVersion++; }
				imgui_SameLine();
				if (imgui_Button("12")) { _rounding = 12f; _roundingBuf = roundingString; _roundingVersion++; }

				imgui_Separator();

				// Preview the accent color
				float[] currentAccentColor = GetThemePreviewColor(_currentTheme);
				imgui_TextColored(currentAccentColor[0], currentAccentColor[1], currentAccentColor[2], currentAccentColor[3], "Preview: This text shows the accent color");

				imgui_Separator();

				// Close button
				if (imgui_Button("Close"))
				{
					_showThemeSettings = false;
				}
			}
			imgui_End();

			if (!modalOpen)
			{
				_showThemeSettings = false;
			}
		}
		private static void RenderAllPlayersView()
		{
			imgui_Text("All Players View");
			imgui_Separator();

			var pd = GetActiveCharacterIniData();
			if (pd == null || pd.Sections == null || string.IsNullOrEmpty(_cfgSelectedSection) || string.IsNullOrEmpty(_cfgSelectedKey))
			{
				imgui_Text("Select a section and key in the Config Editor first.");
				return;
			}



			imgui_Text($"Viewing: [{_cfgSelectedSection}] -> [{_cfgSelectedKey}]");
			imgui_SameLine();
			if (imgui_Button("Refresh")) _cfgAllPlayersRefreshRequested = true;

			imgui_Separator();

			if (imgui_BeginChild("AllPlayersList", 0, 0, true))
			{
				float outerW = Math.Max(720f, imgui_GetContentRegionAvailX()); // keep it roomy
																			   // Columns: Toon | Value (editable) | Actions
				if (imgui_BeginTable("AllPlayersTable", 3, 0, outerW))
				{
					imgui_TableSetupColumn("Toon", 0, 180f);
					imgui_TableSetupColumn("Value", 0, Math.Max(260f, outerW - (180f + 100f))); // leave room for Save
					imgui_TableSetupColumn("Actions", 0, 100f);
					imgui_TableHeadersRow();

					lock (_cfgAllPlayersLock)
					{
						foreach (var row in _cfgAllPlayersRows)
						{
							string toon = row.Key ?? string.Empty;

							if (!_cfgAllPlayersEditBuf.ContainsKey(toon))
								_cfgAllPlayersEditBuf[toon] = row.Value ?? string.Empty;

							imgui_TableNextRow();

							// Toon
							imgui_TableNextColumn();
							imgui_Text(toon);

							// Value (editable)
							imgui_TableNextColumn();
							string currentValue = _cfgAllPlayersEditBuf[toon];
							bool isBool = string.Equals(currentValue, "true", StringComparison.OrdinalIgnoreCase) || string.Equals(currentValue, "false", StringComparison.OrdinalIgnoreCase)
								|| string.Equals(currentValue, "on", StringComparison.OrdinalIgnoreCase) || string.Equals(currentValue, "off", StringComparison.OrdinalIgnoreCase);

							if (isBool)
							{
								if (BeginComboSafe($"##value_{toon}", currentValue))
								{
									if (imgui_Selectable("True", string.Equals(currentValue, "True", StringComparison.OrdinalIgnoreCase)))
									{
										_cfgAllPlayersEditBuf[toon] = "True";
									}
									if (imgui_Selectable("False", string.Equals(currentValue, "False", StringComparison.OrdinalIgnoreCase)))
									{
										_cfgAllPlayersEditBuf[toon] = "False";
									}
									if (imgui_Selectable("On", string.Equals(currentValue, "On", StringComparison.OrdinalIgnoreCase)))
									{
										_cfgAllPlayersEditBuf[toon] = "On";
									}
									if (imgui_Selectable("Off", string.Equals(currentValue, "Off", StringComparison.OrdinalIgnoreCase)))
									{
										_cfgAllPlayersEditBuf[toon] = "Off";
									}
									EndComboSafe();
								}
							}
							else
							{
								string inputId = $"##edit_{toon}";
								if (imgui_InputText(inputId, currentValue))
								{
									_cfgAllPlayersEditBuf[toon] = imgui_InputText_Get(inputId) ?? string.Empty;
								}
							}

							// Actions
							imgui_TableNextColumn();
							if (imgui_Button($"Save##{row.Key}"))
							{
								string newValue = _cfgAllPlayersEditBuf[row.Key] ?? string.Empty;

								if (TrySaveIniValueForToon(row.Key, _cfgSelectedSection, _cfgSelectedKey, newValue, out var err))
								{
									_log.WriteDelayed($"Saved [{_cfgSelectedSection}] {_cfgSelectedKey} for {row.Key}.", Logging.LogLevels.Debug);
								}
								else
								{
									_log.WriteDelayed($"Save failed for {row.Key}: {err}", Logging.LogLevels.Debug);
								}
							}
						}
					}

					imgui_EndTable();
				}
			}
			imgui_EndChild();
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
		private static void BuildConfigSectionOrder()
		{
			_cfgSectionsOrdered.Clear();
			var pd = GetActiveCharacterIniData();
			if (pd?.Sections == null) return;

			// Class-prioritized defaults similar to e3config
			var defaults = new List<string>() { "Misc", "Assist Settings", "Nukes", "Debuffs", "DoTs on Assist", "DoTs on Command", "Heals", "Buffs", "Melee Abilities", "Burn", "CommandSets", "Pets", "Ifs" };
			try
			{
				var cls = E3.CurrentClass;
				if (cls.ToString().Equals("Bard", StringComparison.OrdinalIgnoreCase))
				{
					defaults = new List<string>() { "Bard", "Melee Abilities", "Burn", "CommandSets", "Ifs", "Assist Settings", "Buffs" };
				}
				else if (cls.ToString().Equals("Necromancer", StringComparison.OrdinalIgnoreCase))
				{
					defaults = new List<string>() { "DoTs on Assist", "DoTs on Command", "Debuffs", "Pets", "Burn", "CommandSets", "Ifs", "Assist Settings", "Buffs" };
				}
				else if (cls.ToString().Equals("Shadowknight", StringComparison.OrdinalIgnoreCase))
				{
					defaults = new List<string>() { "Nukes", "Assist Settings", "Buffs", "DoTs on Assist", "DoTs on Command", "Debuffs", "Pets", "Burn", "CommandSets", "Ifs" };
				}
			}
			catch { }

			// Seed ordered list with defaults that exist in the INI
			foreach (var d in defaults)
			{
				if (pd.Sections.ContainsSection(d)) _cfgSectionsOrdered.Add(d);
			}
			// Append any remaining sections not included yet
			foreach (SectionData s in pd.Sections)
			{
				if (!_cfgSectionsOrdered.Contains(s.SectionName, StringComparer.OrdinalIgnoreCase))
					_cfgSectionsOrdered.Add(s.SectionName);
			}

			if (_cfgSectionsOrdered.Count > 0)
			{
				if (string.IsNullOrEmpty(_cfgSelectedSection) || !_cfgSectionsOrdered.Contains(_cfgSelectedSection, StringComparer.OrdinalIgnoreCase))
				{
					_cfgSelectedSection = _cfgSectionsOrdered[0];
					var section = pd.Sections.GetSectionData(_cfgSelectedSection);
					_cfgSelectedKey = section?.Keys?.FirstOrDefault()?.KeyName ?? string.Empty;
					_cfgSelectedValueIndex = -1;
				}
			}
		}
		private static List<string> GetSectionsForDisplay()
		{
			var search = (_cfgSectionSearch ?? string.Empty).Trim();
			if (string.IsNullOrEmpty(search))
			{
				return new List<string>(_cfgSectionsOrdered);
			}

			var matches = new List<(string Section, int Score)>();
			foreach (var section in _cfgSectionsOrdered)
			{
				if (TryFuzzyMatchSection(search, section, out var score))
				{
					matches.Add((section, score));
				}
			}

			if (matches.Count == 0)
			{
				return new List<string>();
			}

			matches.Sort((a, b) =>
			{
				int scoreCompare = a.Score.CompareTo(b.Score);
				if (scoreCompare != 0) return scoreCompare;
				return StringComparer.OrdinalIgnoreCase.Compare(a.Section, b.Section);
			});

			return matches.Select(m => m.Section).ToList();
		}
		private static bool TryFuzzyMatchSection(string search, string section, out int score)
		{
			score = int.MaxValue;
			if (string.IsNullOrEmpty(section)) return false;

			string query = (search ?? string.Empty).Trim();
			if (query.Length == 0)
			{
				score = 0;
				return true;
			}

			string sectionLower = section.ToLowerInvariant();
			string queryLower = query.ToLowerInvariant();

			int directIndex = sectionLower.IndexOf(queryLower, StringComparison.Ordinal);
			if (directIndex >= 0)
			{
				score = directIndex;
				return true;
			}

			string condensed = new string(queryLower.Where(c => !char.IsWhiteSpace(c)).ToArray());
			if (condensed.Length == 0)
			{
				score = 0;
				return true;
			}

			int lastIndex = -1;
			int gapScore = 0;
			for (int qi = 0; qi < condensed.Length; qi++)
			{
				char qc = condensed[qi];
				bool found = false;
				for (int si = lastIndex + 1; si < sectionLower.Length; si++)
				{
					if (sectionLower[si] == qc)
					{
						gapScore += si - lastIndex - 1;
						lastIndex = si;
						found = true;
						break;
					}
				}
				if (!found)
				{
					return false;
				}
			}

			gapScore += sectionLower.Length - lastIndex - 1;
			score = 1000 + gapScore;
			return true;
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

					if (string.IsNullOrEmpty(_selectedCharIniPath))
					{
						var currentPath = GetCurrentCharacterIniPath();
						_selectedCharIniPath = currentPath;
					}
					return _selectedCharIniPath;
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
					_cfg_Dirty = true;
					_cfgSelectedSection = "Ifs";
					_cfgSelectedKey = unique;
					_cfgSelectedValueIndex = -1;
					return true;
				}
				return false;
			}
			catch { return false; }
		}

		private static bool AddBurnToActiveIni(string key, string value)
		{
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
					_cfg_Dirty = true;
					_cfgSelectedSection = "Burn";
					_cfgSelectedKey = unique;
					_cfgSelectedValueIndex = -1;
					return true;
				}
				return false;
			}
			catch { return false; }
		}

		private static bool DeleteKeyFromActiveIni(string sectionName, string keyName)
		{
			try
			{
				var pd = GetActiveCharacterIniData();
				if (pd == null) return false;
				var section = pd.Sections.GetSectionData(sectionName ?? string.Empty);
				if (section == null || section.Keys == null) return false;
				if (!section.Keys.ContainsKey(keyName)) return false;
				section.Keys.RemoveKey(keyName);
				_cfg_Dirty = true;
				_cfgSelectedValueIndex = -1;
				InvalidateSpellEditState();
				// Pick a new selected key if any remain
				var nextKey = section.Keys.FirstOrDefault()?.KeyName ?? string.Empty;
				_cfgSelectedKey = nextKey ?? string.Empty;
				return true;
			}
			catch { return false; }
		}

		private static void RenderIfsSampleModal()
		{
			const string winName = "Sample If's";
			imgui_Begin_OpenFlagSet(winName, _cfgShowIfSampleModal);
			bool _open_ifs = imgui_Begin(winName, (int)ImGuiWindowFlags.ImGuiWindowFlags_AlwaysAutoResize);
			if (_open_ifs)
			{
				if (!string.IsNullOrEmpty(_cfgIfSampleStatus)) imgui_Text(_cfgIfSampleStatus);
				float h = 300f; float w = 640f;
				if (imgui_BeginChild("IfsSampleList", w, h, true))
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
				if (imgui_Button("Close")) { _cfgShowIfSampleModal = false; imgui_Begin_OpenFlagSet(winName, false); }
			}
			imgui_End();
			// Sync with X button on title bar
			_cfgShowIfSampleModal = imgui_Begin_OpenFlagGet(winName);
		}



		private static IniData GetActiveCharacterIniData()
        {
            var currentPath = GetCurrentCharacterIniPath();
			
            if (string.Equals(_selectedCharIniPath, currentPath, StringComparison.OrdinalIgnoreCase))
			{
				//_log.WriteDelayed($"returning local parsed data:{currentPath}", Logging.LogLevels.Debug);
				return E3.CharacterSettings.ParsedData;

			}
			//_log.WriteDelayed($"returning selected parsed data:{_selectedCharIniPath}", Logging.LogLevels.Debug);

			return _selectedCharIniParsedData;
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
			if(!e3util.ShouldCheck(ref _iniFileScanTimeStamp,_iniFileScanInterval))
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
			_charIniFiles = files;
		}
		// Safe combo wrapper for older MQ2Mono
		private static bool BeginComboSafe(string label, string preview)
		{
			try
			{
				return imgui_BeginCombo(label, preview, 0);
			}
			catch
			{
				_comboAvailable = false;
				return false;
			}
		}
		private static void EndComboSafe()
		{
			try { imgui_EndCombo(); } catch { }
		}
		public static void RenderCharacterIniSelector()
		{
			ScanCharIniFilesIfNeeded();

			var currentPath = GetCurrentCharacterIniPath();
			string currentDisplay = Path.GetFileName(currentPath);
			string selName = Path.GetFileName(_selectedCharIniPath ?? currentPath);
			if (string.IsNullOrWhiteSpace(selName)) selName = currentDisplay;

			var onlineToons = GetOnlineToonNames();
			imgui_Text("Select Character:");
			imgui_SameLine();
			imgui_SetNextItemWidth(260f);
			bool opened = _comboAvailable && BeginComboSafe("##Select Character", selName);
			if (opened)
			{
				if (!string.IsNullOrEmpty(currentPath))
				{
					bool sel = string.Equals(_selectedCharIniPath, currentPath, StringComparison.OrdinalIgnoreCase);
					if (imgui_Selectable($"Current: {currentDisplay}", sel))
					{
						_log.Write($"Selecting local:{currentPath}", Logging.LogLevels.Debug);

						_selectedCharIniPath = currentPath;
						
						var pd = E3.CharacterSettings.ParsedData;
						_selectedCharIniParsedData = pd;// use live current
						_nextIniRefreshAtMs = 0;
						// Trigger catalog reload for the selected peer
						_cfg_CatalogsReady = false;
						_cfgSpells.Clear();
						_cfgAAs.Clear();
						_cfgDiscs.Clear();
						_cfgSkills.Clear();
						_cfgItems.Clear();
						_cfg_CatalogLoadRequested = true;
						_cfg_CatalogStatus = "Queued catalog load...";
					}
				}

				imgui_Text("Other Characters:");
				imgui_Separator();

				foreach (var f in _charIniFiles)
				{
					if (string.Equals(f, currentPath, StringComparison.OrdinalIgnoreCase)) continue;
					if (_hideOfflineCharInis && !IsIniForOnlineToon(f, onlineToons)) continue;
					string name = Path.GetFileName(f);
					bool sel = string.Equals(_selectedCharIniPath, f, StringComparison.OrdinalIgnoreCase);
					if (imgui_Selectable($"{name}", sel))
					{
						try
						{
							_log.Write($"Selecting other:{f}",Logging.LogLevels.Debug);
							var parser = E3Core.Utility.e3util.CreateIniParser();
							var pd = parser.ReadFile(f);
							_selectedCharIniPath = f;
							_selectedCharIniParsedData = pd;
							_selectedCharacterSection = string.Empty;
							_charIniEdits.Clear();
							_cfgAllPlayersSig = string.Empty; // force refresh
							_nextIniRefreshAtMs = 0;

							// Trigger catalog reload for the selected peer
							_cfg_CatalogsReady = false;
							_cfgSpells.Clear();
							_cfgAAs.Clear();
							_cfgDiscs.Clear();
							_cfgSkills.Clear();
							_cfgItems.Clear();
							_cfg_CatalogLoadRequested = true;
							_cfg_CatalogStatus = "Queued catalog load...";
						}
						catch { }
					}
				}
				imgui_EndCombo();
			}

			imgui_SameLine();
			_hideOfflineCharInis = imgui_Checkbox("Hide offline", _hideOfflineCharInis);
			imgui_SameLine();

			// Save button with better styling
			if (imgui_Button(_cfg_Dirty ? "Save Changes*" : "Save Changes"))
			{
				SaveActiveIniData();
			}
			imgui_SameLine();
			imgui_TextColored(0.6f, 0.6f, 0.6f, 1.0f, _cfg_Dirty ? "Unsaved changes" : "All changes saved");

			imgui_Separator();
		}


		private static void TryAddVisibleEntriesToSelectedKey(SectionData selectedSection)
		{
			if (selectedSection == null || string.IsNullOrEmpty(_cfgSelectedKey)) return;
			var kd = selectedSection.Keys?.GetKeyData(_cfgSelectedKey);
			if (kd == null) return;
			var values = GetValues(kd);

			var src = GetCatalogByType(_cfgAddType);
			IEnumerable<E3Spell> entries = Enumerable.Empty<E3Spell>();
			if (!string.IsNullOrEmpty(_cfgAddCategory) && src.TryGetValue(_cfgAddCategory, out var submap2))
			{
				if (!string.IsNullOrEmpty(_cfgAddSubcategory) && submap2.TryGetValue(_cfgAddSubcategory, out var l))
					entries = l;
				else
					entries = submap2.Values.SelectMany(x => x);
			}
			string filter = (_cfgAddFilter ?? string.Empty).Trim();
			if (filter.Length > 0) entries = entries.Where(e => e.Name.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0);
			foreach (var e in entries)
			{
				string toAdd = (e.Name ?? string.Empty).Trim();
				if (!values.Contains(toAdd, StringComparer.OrdinalIgnoreCase)) values.Add(toAdd);
			}
			WriteValues(kd, values);
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
				if (imgui_BeginTable(tableId, 2, (int)FieldTableFlags, imgui_GetContentRegionAvailX()))
				{
					imgui_TableSetupColumn("Label", (int)LabelColumnFlags, LabelColumnWidth);
					imgui_TableSetupColumn("Value", (int)ValueColumnFlags, 0f);
					body?.Invoke();
					imgui_EndTable();
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

			void RenderFlagsTab()
			{
				imgui_TextColored(0.8f, 0.9f, 0.95f, 1.0f, "Behavior Flags");
				imgui_Text("Toggle and Apply to commit changes.");
				
				if (imgui_BeginTable($"SpellFlagTable_{idBase}", 2, (int)ImGuiTableFlags.ImGuiTableFlags_SizingStretchSame, imgui_GetContentRegionAvailX()))
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
					imgui_EndTable();
				}
				else
				{
					foreach (var entry in _spellFlags)
					{
						CheckboxFlag(entry.Label, entry.Flag);
					}
				}
			}

			string entryLabel = $"[{state.Section}] {state.Key} entry #{state.ValueIndex + 1}";
			imgui_TextColored(0.95f, 0.85f, 0.35f, 1.0f, entryLabel);
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

			if (imgui_Button($"Apply##spell_apply_{idBase}"))
			{
				ApplySpellValueChanges(state);
			}
			imgui_SameLine();
			if (imgui_Button($"Reset##spell_reset_{idBase}"))
			{
				ResetSpellValueEditor(state);
			}
		}

		private static void RenderSpellModifierModal()
		{
			var iniData = GetActiveCharacterIniData();
			var sectionData = iniData?.Sections?.GetSectionData(_cfgSelectedSection ?? string.Empty);
			var keyData = sectionData?.Keys?.GetKeyData(_cfgSelectedKey ?? string.Empty);
			var values = GetValues(keyData);
			if (_cfgSelectedValueIndex < 0 || _cfgSelectedValueIndex >= values.Count)
			{
				_cfgShowSpellModifierModal = false;
				return;
			}

			string rawValue = values[_cfgSelectedValueIndex] ?? string.Empty;
			var state = EnsureSpellEditState(_cfgSelectedSection, _cfgSelectedKey, _cfgSelectedValueIndex, rawValue);
			if (state == null)
			{
				_cfgShowSpellModifierModal = false;
				return;
			}

			const string modalTitle = "Flags & Modifiers##spell_modal";
			imgui_Begin_OpenFlagSet(modalTitle, true);
			bool modalOpen = imgui_Begin(modalTitle, (int)ImGuiWindowFlags.ImGuiWindowFlags_NoCollapse);
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

			if (!modalOpen || !imgui_Begin_OpenFlagGet(modalTitle))
			{
				_cfgShowSpellModifierModal = false;
			}
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
			var vals = new List<string>();
			try
			{
				if (kd.ValueList != null && kd.ValueList.Count > 0)
				{
					foreach (var v in kd.ValueList) vals.Add(v ?? string.Empty);
				}
				else if (!string.IsNullOrEmpty(kd.Value))
				{
					// Support pipe-delimited storage if present
					var parts = (kd.Value ?? string.Empty).Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim());
					foreach (var p in parts) vals.Add(p);
				}
			}
			catch { }
			return vals;
		}

		private static void WriteValues(KeyData kd, List<string> values)
		{
			
			if (kd == null) return;
			// Preserve exact row semantics: one value per row, including empties
			if (kd.ValueList != null)
			{
				kd.ValueList.Clear();
				foreach (var v in values) kd.ValueList.Add(v ?? string.Empty);
			}
			// Do NOT set kd.Value here; in our Ini parser, setting Value appends to ValueList.
			_cfg_Dirty = true;
			
		}

			// Inventory scanning for Food/Drink using MQ TLOs (non-blocking via ProcessBackgroundWork trigger)
		private static void RenderFoodDrinkPicker(SectionData selectedSection)
		{
			// Respect current open state instead of forcing true every frame
			const string winName = "Pick From Inventory##modal";
			imgui_Begin_OpenFlagSet(winName, _cfgShowFoodDrinkModal);
				bool shouldDraw = imgui_Begin(winName, (int)(ImGuiWindowFlags.ImGuiWindowFlags_AlwaysAutoResize | ImGuiWindowFlags.ImGuiWindowFlags_NoCollapse));

				if (shouldDraw)
				{
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

						if (imgui_BeginChild("FoodDrinkList", listWidth, listHeight, true))
						{
							for (int i = 0; i < _cfgFoodDrinkCandidates.Count; i++)
							{
								var item = _cfgFoodDrinkCandidates[i];
								if (imgui_Selectable($"{item}##item_{i}", false))
								{
									// Apply selection
									var pdAct = GetActiveCharacterIniData();
									var secData = pdAct.Sections.GetSectionData(_cfgSelectedSection);
									var keyData = secData?.Keys.GetKeyData(_cfgSelectedKey);
									if (keyData != null)
									{
										var vals = GetValues(keyData);
										// Replace first value or add if empty
										if (vals.Count == 0) vals.Add(item);
										else vals[0] = item;
										WriteValues(keyData, vals);
									}
									_cfgShowFoodDrinkModal = false;
									// Also close the underlying window state immediately
									imgui_Begin_OpenFlagSet(winName, false);
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
						_cfgShowFoodDrinkModal = false;
						imgui_Begin_OpenFlagSet(winName, false);
					}
				}

				imgui_End();

				// Sync our open flag with the actual window state (handles Titlebar X)
			_cfgShowFoodDrinkModal = imgui_Begin_OpenFlagGet(winName);
		}
		private static void RenderBardMelodyHelperModal()
		{
			const string winName = "Bard Melody Helper##modal";
			imgui_Begin_OpenFlagSet(winName, _cfgShowBardMelodyHelper);
			bool open = imgui_Begin(winName, (int)(ImGuiWindowFlags.ImGuiWindowFlags_AlwaysAutoResize | ImGuiWindowFlags.ImGuiWindowFlags_NoCollapse));
			if (open)
			{
				imgui_TextColored(0.95f, 0.85f, 0.35f, 1.0f, "Create a Bard Melody");
				imgui_TextWrapped("Answer the prompts below to build a melody and optional IF condition. We'll add everything to your INI for you.");
				imgui_Separator();

				imgui_Text("Melody name:");
				imgui_SameLine();
				imgui_SetNextItemWidth(260f);
				if (imgui_InputText("##bard_melody_name", _cfgBardMelodyName ?? string.Empty))
				{
					_cfgBardMelodyName = (imgui_InputText_Get("##bard_melody_name") ?? string.Empty).Trim();
				}
				if (string.IsNullOrEmpty(_cfgBardMelodyName))
				{
					imgui_TextColored(0.7f, 0.7f, 0.7f, 1.0f, "Example: \"Caster\" or \"Main\"");
				}
				imgui_Separator();

				EnsureBardMelodySongEntries();
				bool catalogsReady = _cfg_CatalogsReady;
				imgui_Text("Songs (cast order):");
				if (!catalogsReady)
				{
					imgui_TextColored(0.8f, 0.6f, 0.6f, 1.0f, "Catalog data not yet loaded. Use manual entry or load catalogs first.");
				}
				for (int i = 0; i < _cfgBardMelodySongs.Count; i++)
				{
					string label = $"Song {i + 1}";
					imgui_SetNextItemWidth(300f);
					string inputId = $"{label}##bard_song_{i}_{_cfgBardSongInputVersion}";

					// Ensure buffer exists and is synchronized with the songs list
					if (!_cfgBardMelodyBuffers.ContainsKey(i))
					{
						_cfgBardMelodyBuffers[i] = _cfgBardMelodySongs[i] ?? string.Empty;
					}
					
					string buffer = _cfgBardMelodyBuffers[i];
					if (imgui_InputText(inputId, buffer))
					{
						buffer = imgui_InputText_Get(inputId) ?? string.Empty;
						_cfgBardMelodyBuffers[i] = buffer;
						_cfgBardMelodySongs[i] = buffer.Trim();
					}
					imgui_SameLine();
					if (imgui_Button($"Remove##bard_song_remove_{i}"))
					{
						_cfgBardMelodySongs.RemoveAt(i);
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
					string gemPreview = _cfgBardMelodyGems[i].ToString();
					if (imgui_BeginCombo($"##bard_gem_combo_{i}_{_cfgBardSongInputVersion}", gemPreview, 0))
					{
						for (int gem = 1; gem <= 12; gem++)
						{
							if (imgui_MenuItem(gem.ToString()))
							{
								_cfgBardMelodyGems[i] = gem;
								_cfgBardMelodyGemBuffers[i] = gem.ToString();
							}
						}
						imgui_EndCombo();
					}
				}
				if (imgui_Button("Add Another Song"))
				{
					_cfgBardMelodySongs.Add(string.Empty);
					_cfgBardMelodyBuffers[_cfgBardMelodySongs.Count - 1] = string.Empty;
					_cfgBardMelodyGems.Add(1);
					_cfgBardMelodyGemBuffers[_cfgBardMelodySongs.Count - 1] = "1";
				}
				imgui_Separator();

				imgui_Text("When should we play it?");
				imgui_SetNextItemWidth(350f);
				string conditionId = $"##bard_melody_condition_{_cfgBardConditionInputVersion}";
				if (imgui_InputText(conditionId, _cfgBardMelodyCondition ?? string.Empty))
				{
					_cfgBardMelodyCondition = (imgui_InputText_Get(conditionId) ?? string.Empty).Trim();
				}
				imgui_SameLine();
				if (imgui_Button("Sample IFs..."))
				{
					if (!EnsureBardSampleIfsLoaded())
					{
						if (string.IsNullOrEmpty(_cfgBardSampleIfStatus))
						{
							_cfgBardSampleIfStatus = "Sample file not found.";
						}
					}
					_cfgShowBardSampleIfModal = true;
				}
				imgui_TextColored(0.7f, 0.7f, 0.7f, 1.0f, "Optional E3 IF expression. Leave blank to run the melody whenever possible.");
				imgui_Separator();

				if (!string.IsNullOrEmpty(_cfgBardMelodyModalStatus))
				{
					imgui_TextColored(0.9f, 0.6f, 0.6f, 1.0f, _cfgBardMelodyModalStatus);
					imgui_Separator();
				}

				if (imgui_Button("Create Melody"))
				{
					if (TryCreateBardMelody(out var successMessage, out var errorMessage))
					{
						_cfgBardMelodyStatus = successMessage;
						_cfgBardMelodyModalStatus = string.Empty;
						ResetBardMelodyHelperForm();
					}
					else
					{
						_cfgBardMelodyModalStatus = errorMessage;
					}
				}
				imgui_SameLine();
				if (imgui_Button("Cancel##bard_helper_cancel"))
				{
					_cfgShowBardMelodyHelper = false;
					imgui_Begin_OpenFlagSet(winName, false);
				}
			}
			imgui_End();
			_cfgShowBardMelodyHelper = imgui_Begin_OpenFlagGet(winName);

			// Reset the picker state after rendering
			if (_cfgBardSongPickerJustSelected)
			{
				_cfgBardSongPickerIndex = -1;
				_cfgBardSongPickerJustSelected = false;
			}
		}
		private static void ResetBardMelodyHelperForm()
		{
			_cfgBardMelodyName = string.Empty;
			_cfgBardMelodyCondition = string.Empty;
			_cfgBardMelodyModalStatus = string.Empty;
			_cfgBardMelodySongs = new List<string> { string.Empty, string.Empty, string.Empty };
			_cfgBardMelodyBuffers = new Dictionary<int, string>
			{
				{0, string.Empty},
				{1, string.Empty},
				{2, string.Empty}
			};
			_cfgBardMelodyGems = new List<int> { 1, 1, 1 };
			_cfgBardMelodyGemBuffers = new Dictionary<int, string>
			{
				{0, "1"},
				{1, "1"},
				{2, "1"}
			};
			_cfgBardSongPickerIndex = -1;
			_cfgCatalogMode = CatalogMode.Standard;
		}
		private static void EnsureBardMelodySongEntries()
		{
			if (_cfgBardMelodySongs == null)
			{
				_cfgBardMelodySongs = new List<string>();
			}
			if (_cfgBardMelodyBuffers == null)
			{
				_cfgBardMelodyBuffers = new Dictionary<int, string>();
			}
			if (_cfgBardMelodyGems == null)
			{
				_cfgBardMelodyGems = new List<int>();
			}
			if (_cfgBardMelodyGemBuffers == null)
			{
				_cfgBardMelodyGemBuffers = new Dictionary<int, string>();
			}
			if (_cfgBardMelodySongs.Count == 0)
			{
				_cfgBardMelodySongs.Add(string.Empty);
			}
			while (_cfgBardMelodyGems.Count < _cfgBardMelodySongs.Count)
			{
				_cfgBardMelodyGems.Add(1);
			}
			for (int i = 0; i < _cfgBardMelodySongs.Count; i++)
			{
				if (!_cfgBardMelodyBuffers.ContainsKey(i))
				{
					_cfgBardMelodyBuffers[i] = _cfgBardMelodySongs[i] ?? string.Empty;
				}
				if (!_cfgBardMelodyGemBuffers.ContainsKey(i))
				{
					_cfgBardMelodyGemBuffers[i] = _cfgBardMelodyGems[i].ToString();
				}
			}
			var keysToRemove = _cfgBardMelodyBuffers.Keys.Where(k => k >= _cfgBardMelodySongs.Count).ToList();
			foreach (var key in keysToRemove)
			{
				_cfgBardMelodyBuffers.Remove(key);
			}
			var gemKeysToRemove = _cfgBardMelodyGemBuffers.Keys.Where(k => k >= _cfgBardMelodySongs.Count).ToList();
			foreach (var key in gemKeysToRemove)
			{
				_cfgBardMelodyGemBuffers.Remove(key);
			}
			while (_cfgBardMelodyGems.Count > _cfgBardMelodySongs.Count)
			{
				_cfgBardMelodyGems.RemoveAt(_cfgBardMelodyGems.Count - 1);
			}
		}

		private static void ReindexBardMelodyBuffers()
		{
			var newBuffers = new Dictionary<int, string>();
			var newGemBuffers = new Dictionary<int, string>();
			for (int i = 0; i < _cfgBardMelodySongs.Count; i++)
			{
				string value = _cfgBardMelodySongs[i] ?? string.Empty;
				newBuffers[i] = value;
				newGemBuffers[i] = _cfgBardMelodyGems[i].ToString();
			}
			_cfgBardMelodyBuffers = newBuffers;
			_cfgBardMelodyGemBuffers = newGemBuffers;
		}
		private static bool EnsureBardSampleIfsLoaded()
		{
			if (_cfgBardSampleIfLines.Count > 0)
			{
				return true;
			}

			_cfgBardSampleIfLines.Clear();
			_cfgBardSampleIfStatus = string.Empty;
			try
			{
				string samplePath = ResolveSampleIfsPath();
				if (string.IsNullOrEmpty(samplePath))
				{
					_cfgBardSampleIfStatus = "Sample file not found.";
					return false;
				}

				_cfgBardSampleIfStatus = "Loaded: " + Path.GetFileName(samplePath);
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
						_cfgBardSampleIfLines.Add(new KeyValuePair<string, string>(key, val));
						added++;
					}
				}

				if (added == 0)
				{
					_cfgBardSampleIfStatus = "No entries found in sample file.";
				}
			}
			catch (Exception ex)
			{
				_cfgBardSampleIfStatus = "Error reading sample IFs: " + (ex.Message ?? "error");
			}

			return _cfgBardSampleIfLines.Count > 0;
		}
		private static void OpenBardSongPicker(int index)
		{
			if (index < 0) return;
			EnsureBardMelodySongEntries();
			while (_cfgBardMelodySongs.Count <= index)
			{
				_cfgBardMelodySongs.Add(string.Empty);
			}

			_cfgBardSongPickerIndex = index;
			_cfgCatalogMode = CatalogMode.BardSong;
			_cfgAddType = AddType.Spells;
			_cfgAddCategory = string.Empty;
			_cfgAddSubcategory = string.Empty;
			_cfgShowAddModal = true;
			_cfgAddFilter = string.Empty;
		}
		private static void ApplyBardSongSelection(string songName)
		{
			if (_cfgBardSongPickerIndex < 0)
			{
				_cfgCatalogMode = CatalogMode.Standard;
				_cfgShowAddModal = false;
				return;
			}
			EnsureBardMelodySongEntries();
			while (_cfgBardMelodySongs.Count <= _cfgBardSongPickerIndex)
			{
				_cfgBardMelodySongs.Add(string.Empty);
			}
			_cfgBardMelodySongs[_cfgBardSongPickerIndex] = songName ?? string.Empty;
			_cfgBardMelodyBuffers[_cfgBardSongPickerIndex] = songName ?? string.Empty;
			_cfgBardSongPickerJustSelected = true; // Flag to force input refresh
			_cfgBardSongInputVersion++; // Change input ID to force text update
			// _cfgBardSongPickerIndex = -1; // Moved to modal render to allow input text update
			_cfgShowAddModal = false;
			_cfgCatalogMode = CatalogMode.Standard;
		}
		private static void RenderBardSampleIfModal()
		{
			const string winName = "Sample IFs##bard";
			imgui_Begin_OpenFlagSet(winName, _cfgShowBardSampleIfModal);
			bool open = imgui_Begin(winName, (int)(ImGuiWindowFlags.ImGuiWindowFlags_AlwaysAutoResize | ImGuiWindowFlags.ImGuiWindowFlags_NoCollapse));
			if (open)
			{
				bool ready = EnsureBardSampleIfsLoaded();
				imgui_TextColored(0.95f, 0.85f, 0.35f, 1.0f, "Sample IFs");
				imgui_TextWrapped("Select a sample condition to copy it into the melody helper.");
				imgui_Separator();

				if (!ready)
				{
					imgui_TextColored(0.85f, 0.6f, 0.6f, 1.0f, string.IsNullOrEmpty(_cfgBardSampleIfStatus) ? "Sample file not found." : _cfgBardSampleIfStatus);
				}
				else if (!string.IsNullOrEmpty(_cfgBardSampleIfStatus))
				{
					imgui_TextColored(0.7f, 0.9f, 0.7f, 1.0f, _cfgBardSampleIfStatus);
				}

				if (imgui_Button("Reload Samples"))
				{
					_cfgBardSampleIfLines.Clear();
					EnsureBardSampleIfsLoaded();
				}
				imgui_SameLine();
				imgui_Text("Filter:");
				imgui_SameLine();
				imgui_SetNextItemWidth(260f);
				if (imgui_InputText("##bard_sample_if_filter", _cfgBardSampleIfFilter ?? string.Empty))
				{
					_cfgBardSampleIfFilter = (imgui_InputText_Get("##bard_sample_if_filter") ?? string.Empty).Trim();
				}
				imgui_SameLine();
				if (imgui_Button("Clear##bard_sample_if_filter_clear"))
				{
					_cfgBardSampleIfFilter = string.Empty;
				}

				imgui_Separator();

				var displayList = new List<KeyValuePair<string, string>>();
				if (ready)
				{
					if (string.IsNullOrEmpty(_cfgBardSampleIfFilter))
					{
						displayList.AddRange(_cfgBardSampleIfLines);
					}
					else
					{
						var tokens = _cfgBardSampleIfFilter.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
						foreach (var kv in _cfgBardSampleIfLines)
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
					if (imgui_BeginChild("BardSampleIfList", tableWidth, tableHeight, true))
					{
						if (imgui_BeginTable("BardSampleIfTable", 3, 0, tableWidth))
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
									_cfgBardMelodyCondition = expression;
									_cfgBardConditionInputVersion++;
									_cfgShowBardSampleIfModal = false;
									imgui_Begin_OpenFlagSet(winName, false);
									break;
								}
							}

							imgui_EndTable();
						}
						imgui_EndChild();
					}
				}

				imgui_Separator();
				if (imgui_Button("Close##bard_sample_if_close"))
				{
					_cfgShowBardSampleIfModal = false;
					imgui_Begin_OpenFlagSet(winName, false);
				}
			}
			imgui_End();
			_cfgShowBardSampleIfModal = imgui_Begin_OpenFlagGet(winName);
		}
		private static bool TryCreateBardMelody(out string successMessage, out string errorMessage)
		{
			successMessage = string.Empty;
			errorMessage = string.Empty;

			var pd = GetActiveCharacterIniData();
			if (pd == null)
			{
				errorMessage = "No character INI is currently loaded.";
				return false;
			}

			string rawName = (_cfgBardMelodyName ?? string.Empty).Trim();
			if (rawName.Length == 0)
			{
				errorMessage = "Please provide a melody name.";
				return false;
			}

			string melodyName = rawName.EndsWith(" Melody", StringComparison.OrdinalIgnoreCase)
				? rawName.Substring(0, rawName.Length - " Melody".Length).TrimEnd()
				: rawName;

			var songGemPairs = (_cfgBardMelodySongs ?? new List<string>())
				.Select((s, i) => new { Song = (s ?? string.Empty).Trim(), Gem = _cfgBardMelodyGems[i] })
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

			string condition = (_cfgBardMelodyCondition ?? string.Empty).Trim();
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

			if (!_cfgSectionsOrdered.Any(s => string.Equals(s, melodySectionName, StringComparison.OrdinalIgnoreCase)))
			{
				_cfgSectionsOrdered.Add(melodySectionName);
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

			_cfgSelectedSection = melodySectionName;
			_cfgSelectedKey = "Song";
			_cfgSelectedValueIndex = 0;
			_cfg_Dirty = true;

			successMessage = string.IsNullOrEmpty(melodyIfKeyName)
				? $"Melody '{melodyName}' created with {songs.Count} song(s)."
				: $"Melody '{melodyName}' created with {songs.Count} song(s) and IF '{melodyIfKeyName}'.";
			return true;
		}
		private static bool TryEnsureBardIfEntry(string baseName, string expression, out string actualKey, out string errorMessage)
		{
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
			_cfg_Dirty = true;

			if (!_cfgSectionsOrdered.Any(s => string.Equals(s, "Ifs", StringComparison.OrdinalIgnoreCase)))
			{
				_cfgSectionsOrdered.Add("Ifs");
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
			imgui_Begin_OpenFlagSet("Pick Toons", true);
			bool _open_toon = imgui_Begin("Pick Toons", (int)ImGuiWindowFlags.ImGuiWindowFlags_AlwaysAutoResize);
			if (_open_toon)
			{
				if (!string.IsNullOrEmpty(_cfgToonPickerStatus)) imgui_Text(_cfgToonPickerStatus);
				float h = 300f; float w = 420f;
				if (imgui_BeginChild("ToonList", w, h, true))
				{
					var list = _cfgToonCandidates ?? new List<string>();
					var kd = selectedSection?.Keys?.GetKeyData(_cfgSelectedKey ?? string.Empty);
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
				if (imgui_Button("Close")) _cfgShowToonPickerModal = false;
			}
			imgui_End();
			if (!_open_toon) _cfgShowToonPickerModal = false;
		}

		// Spell Info modal (read-only details) using real ImGui tables + colored labels

		private static void RenderSpellInfoModal()
		{
			var s = _cfgSpellInfoSpell;
			if (s == null) { _cfgShowSpellInfoModal = false; return; }
			const string winName = "Spell Information";
			imgui_Begin_OpenFlagSet(winName, _cfgShowSpellInfoModal);
			bool open = imgui_Begin(winName, (int)ImGuiWindowFlags.ImGuiWindowFlags_AlwaysAutoResize);
			if (open)
			{
				// Header with better styling
				imgui_TextColored(0.95f, 0.85f, 0.35f, 1.0f, $"{s.Name ?? string.Empty}");
				imgui_Separator();

				// Build table rows (label, value) with better formatting
				//var rows = new List<KeyValuePair<string, string>>();
				//rows.Add(new KeyValuePair<string, string>("Type", s.CastType ?? string.Empty));
				//rows.Add(new KeyValuePair<string, string>("Level", s.Level > 0 ? s.Level.ToString() : string.Empty));
				//rows.Add(new KeyValuePair<string, string>("Mana", s.Mana > 0 ? s.Mana.ToString() : string.Empty));
				//rows.Add(new KeyValuePair<string, string>("Cast Time", s.CastTime > 0 ? $"{s.CastTime:0.00}s" : string.Empty));
				//rows.Add(new KeyValuePair<string, string>("Recast", s.Recast > 0 ? FormatMsSmart(s.Recast) : string.Empty));
				//rows.Add(new KeyValuePair<string, string>("Range", s.Range > 0 ? s.Range.ToString("0") : string.Empty));
				//rows.Add(new KeyValuePair<string, string>("Target", s.TargetType ?? string.Empty));
				//rows.Add(new KeyValuePair<string, string>("School", s.SpellType ?? string.Empty));
				//rows.Add(new KeyValuePair<string, string>("Resist", !string.IsNullOrEmpty(s.ResistType) ? ($"{s.ResistType} {(s.ResistAdj != 0 ? "(" + s.ResistAdj.ToString() + ")" : string.Empty)}") : string.Empty));
				//// Filter out empty values to avoid rendering blank rows
				//rows = rows.Where(kv => !string.IsNullOrEmpty(kv.Value)).ToList();

				float width = Math.Max(520f, imgui_GetContentRegionAvailX());
				if (imgui_BeginTable("SpellInfoTable", 2, 0, width))
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

					if (s.Level>0)
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
					imgui_EndTable();
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
					_cfgShowSpellInfoModal = false;
					_cfgSpellInfoSpell = null;
					imgui_Begin_OpenFlagSet(winName, false);
				}
			}
			imgui_End();
			_cfgShowSpellInfoModal = imgui_Begin_OpenFlagGet(winName);
			if (!_cfgShowSpellInfoModal) { _cfgSpellInfoSpell = null; }
		}

		private static void RenderDonateModal()
		{
			imgui_Begin_OpenFlagSet("Support E3", true);
			bool open = imgui_Begin("Support E3", (int)ImGuiWindowFlags.ImGuiWindowFlags_AlwaysAutoResize);
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
					_showDonateModal = false;
				}
				imgui_SameLine();
				if (imgui_Button("No"))
				{
					_showDonateModal = false;
				}
			}
			imgui_End();
			if (!open)
			{
				_showDonateModal = false;
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
