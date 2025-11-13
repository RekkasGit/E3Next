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
using data = E3Core.UI.Windows.CharacterSettings.CharacterSettingsWindowHelpers;

namespace E3Core.UI.Windows.CharacterSettings
{
	
	public static class CharacterSettingsWindow
	{
		private static readonly string _windowName = "E3Next Config";

		public static Logging _log = E3.Log;
		private static IMQ MQ = E3.MQ;
		private static ISpawns _spawns = E3.Spawns;

		public static CharacterSettingsState _state = new CharacterSettingsState();

		//A very large bandaid on the Threading of this window
		//used when trying to get a pointer to the _cfg objects.
		
		#region Variables
		/// <summary>
		///Data organized into Category, Sub Category, List of Spells.
		///always get a pointer to these via the method GetCatalogByType
		/// </summary>
	
		//lookups of spell name to spell data, used to display data/icons to user.
		

		// Food/Drink picker state
	
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

	

		// Config UI toggle: "/e3imgui".
		
		private static bool _imguiContextReady = false;
		public static SettingsTab _activeSettingsTab = SettingsTab.Character;
		// Inline edit helpers
		private const float _valueRowActionStartOffset = 46f;
		// Collapsible section state tracking
		private static Dictionary<string, bool> _cfgSectionExpanded = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);

		// Integrated editor panel state (replaces modal)
		private static string _cfgManualEditBuffer = string.Empty;
		

	
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
			data.ProcessBackgroundWork();
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
				imgui_InputText_Clear(searchId); //necessary to clear out the C++ buffer for the search
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
			data._catalog_Spells.Clear();
			data._catalog_AA.Clear();
			data._catalog_Disc.Clear();
			data._catalog_Skills.Clear();
			data._catalog_Items.Clear();
		}
		private static void RequestCatalogUpdate()
		{
			var gemstate = _state.GetState<State_CatalogGems>();
			_state.State_CatalogReady = false;
			ResetCatalogs();
			_state.State_CatalogLoadRequested = true;
			_state.Status_CatalogRequest = "Queued catalog load...";
			gemstate._cfg_CatalogSource = "Refreshing...";
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

			data.ScanCharIniFilesIfNeeded();

			var loggedInCharIniFile = data.GetCurrentCharacterIniPath();
			string currentINIFileName = Path.GetFileName(loggedInCharIniFile);
			string selectedINIFile = Path.GetFileName(state.CurrentINIFileNameFull ?? loggedInCharIniFile);
			
			if (string.IsNullOrWhiteSpace(selectedINIFile)) selectedINIFile = currentINIFileName;

			var onlineToons = data.GetOnlineToonNames();
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

			
			if (imgui_Checkbox("Show offline", state.ShowOfflineCharacters))
			{
				state.ShowOfflineCharacters = imgui_Checkbox_Get("Show offline");
			}
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
			string activeIniPath = data.GetActiveSettingsPath() ?? string.Empty;
			if (!string.Equals(activeIniPath, state.LastIniPath, StringComparison.OrdinalIgnoreCase))
			{
				state.LastIniPath = activeIniPath;
				state.SelectedSection = string.Empty;
				state.SelectedKey = string.Empty;
				state.SelectedValueIndex = -1;
				data.BuildConfigSectionOrder();
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

		#region RenderConfigEditor
		private static void RenderConfigEditor()
		{
			var state = _state.GetState<State_MainWindow>();


			data.RefreshEditableSpellState();

			EnsureConfigEditorInit();
			var pd = data.GetActiveCharacterIniData();
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
		#endregion

		// Integrated editor panel - renders after the main table and spans full width
		private static void RenderIntegratedModifierEditor()
		{
			var mainWindowState = _state.GetState<State_MainWindow>();

			var iniData = data.GetActiveCharacterIniData();
			var sectionData = iniData?.Sections?.GetSectionData(mainWindowState.SelectedSection ?? string.Empty);
			var keyData = sectionData?.Keys?.GetKeyData(mainWindowState.SelectedKey ?? string.Empty);
			var values = GetValues(keyData);


			var kd = data.GetCurrentEditedSpellKeyData();
			if (kd == null) return;

			if (mainWindowState.SelectedValueIndex < 0 || mainWindowState.SelectedValueIndex >= kd.ValueList.Count)
			{
				mainWindowState.Show_ShowIntegratedEditor = false;
				return;
			}
			imgui_Separator();
			imgui_TextColored(0.95f, 0.85f, 0.35f, 1.0f, "Spell Modifier Editor");
			imgui_Separator();
			RenderSpellEditor();
		}

		// Safe gem display using catalog data (no TLO queries from UI thread)
		private static void RenderCatalogGemData()
		{
			var gemState = _state.GetState<State_CatalogGems>();

			lock (data._dataLock)
			{
				if (!_state.State_GemsAvailable || gemState._cfg_CatalogGems == null) return;

			}

			try
			{
				imgui_Separator();

				// Show header with source info
				string sourceText = gemState._cfg_CatalogSource.StartsWith("Remote") ? "Memorized Spells" : "Currently Memorized Spells";
				imgui_TextColored(0.8f, 0.9f, 1.0f, 1.0f, sourceText);

				if (gemState._cfg_CatalogSource.StartsWith("Remote"))
				{
					imgui_SameLine();
					imgui_TextColored(0.7f, 1.0f, 0.7f, 1.0f, $"({gemState._cfg_CatalogSource.Replace("Remote (", "").Replace(")", "")})")
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
							Int32.TryParse(gemState._cfg_CatalogGems[gem], out spellID);

							string spellName = MQ.Query<string>($"${{Spell[{spellID}]}}", false);

							if (!string.IsNullOrEmpty(spellName) && !spellName.Equals("NULL", StringComparison.OrdinalIgnoreCase) && !spellName.Equals("ERROR", StringComparison.OrdinalIgnoreCase))
							{
								// Get spell icon index for this gem
								int iconIndex = (gemState._cfg_CatalogGemIcons != null && gem < gemState._cfg_CatalogGemIcons.Length) ? gemState._cfg_CatalogGemIcons[gem] : -1;

								// Display spell icon using native EQ texture
								if (iconIndex >= 0)
								{
									imgui_DrawSpellIconByIconIndex(iconIndex, 40.0f);
								}

								// Try to find spell info for color coding
								if (_state.State_CatalogReady)
								{
									var spellInfo = data.FindSpellItemAAByName(spellName);
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




		#region RenderSelectedKeyValues
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
			if (TryGetValidOptionsForKey(mainWindowState.SelectedKey, out var enumOpts))
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
			
			if(Settings.CharacterSettings.ConfigKeyDescriptionsForImGUI.TryGetValue($"{mainWindowState.SelectedSection}::{mainWindowState.SelectedKey}", out var description))
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
						var pdAct = data.GetActiveCharacterIniData();
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
						var pdAct = data.GetActiveCharacterIniData();
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
					Int32 iconID = data.GetIconFromIniString(v);
					imgui_DrawSpellIconByIconIndex(iconID, 30.0f);
					imgui_SameLine();
					imgui_Text($"{i + 1}.");
					imgui_SameLine(_valueRowActionStartOffset + 20);

					bool canMoveUp = i > 0;
					bool canMoveDown = i < parts.Count - 1;

					void SwapAndMark(int fromIndex, int toIndex)
					{
						var pdAct = data.GetActiveCharacterIniData();
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
						mainWindowState.PendingValueSelection = toIndex;
					}

					void StartInlineEdit(int index, string currentValue)
					{
						mainWindowState.InLineEditIndex = index;
						mainWindowState.Buffer_InlineEdit = currentValue ?? string.Empty;
					}

					void DeleteValueAt(int index)
					{
						var pdAct = data.GetActiveCharacterIniData();
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
						mainWindowState.Buffer_InlineEdit = v;
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
					if (imgui_InputTextMultiline($"##edit_text_{itemUid}", mainWindowState.Buffer_InlineEdit ?? string.Empty, editWidth, editHeight))
					{
						mainWindowState.Buffer_InlineEdit = imgui_InputText_Get($"##edit_text_{itemUid}");
					}

					if (imgui_Button($"Save##save_{itemUid}"))
					{
						string newText = mainWindowState.Buffer_InlineEdit ?? string.Empty;
						int idx = i;
						var pdAct = data.GetActiveCharacterIniData();
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
						mainWindowState.Buffer_InlineEdit = string.Empty;
						// continue to render items; parts refresh handled below
					}
					imgui_SameLine();

					if (imgui_Button($"Cancel##cancel_{itemUid}"))
					{
						mainWindowState.InLineEditIndex = -1;
						mainWindowState.Buffer_InlineEdit = string.Empty;
					}
				}

				// If a change was made, we need to refresh the parts list for subsequent iterations
				if (listChanged)
				{
					// Re-get the values after modification
					var updatedKd = selectedSection.Keys.GetKeyData(mainWindowState.SelectedKey ?? string.Empty);
					parts = GetValues(updatedKd);
					listChanged = false; // Reset the flag
					if (mainWindowState.PendingValueSelection >= 0 && mainWindowState.PendingValueSelection < parts.Count)
					{
						mainWindowState.SelectedValueIndex = mainWindowState.PendingValueSelection;
					}
					else
					{
						mainWindowState.SelectedValueIndex = -1;
					}
					mainWindowState.PendingValueSelection = -1;
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
				if (imgui_InputTextMultiline($"##add_new_manual", mainWindowState.Buffer_InlineEdit ?? string.Empty, addManualWidth, addManualHeight))
				{
					mainWindowState.Buffer_InlineEdit = imgui_InputText_Get($"##add_new_manual");
				}

				if (imgui_Button($"Add##add_manual"))
				{
					string newText = mainWindowState.Buffer_InlineEdit ?? string.Empty;
					if (!string.IsNullOrWhiteSpace(newText))
					{
						var pdAct = data.GetActiveCharacterIniData();
						var selSec = pdAct.Sections.GetSectionData(mainWindowState.SelectedSection);
						var key = selSec?.Keys.GetKeyData(mainWindowState.SelectedKey);
						if (key != null)
						{
							var vals = GetValues(key);
							vals.Add(newText.Trim());
							WriteValues(key, vals);
							mainWindowState.PendingValueSelection = vals.Count - 1;
						}
					}
					mainWindowState.InLineEditIndex = -1;
					mainWindowState.Buffer_InlineEdit = string.Empty;
				}
				imgui_SameLine();

				if (imgui_Button($"Cancel##cancel_manual"))
				{
					mainWindowState.InLineEditIndex = -1;
					mainWindowState.Buffer_InlineEdit = string.Empty;
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
					mainWindowState.Buffer_InlineEdit = string.Empty;
				}
				imgui_SameLine();

				// For Food/Drink keys, show "Pick From Inventory" instead of "Add From Catalog"
				if (isFoodOrDrink)
				{
					var foodDrinkState = _state.GetState<State_FoodDrink>();

					if (imgui_Button("Pick From Inventory"))
					{
						// Reset scan state so results don't carry over between Food/Drink
						foodDrinkState._cfgFoodDrinkKey = mainWindowState.SelectedKey; // "Food" or "Drink"
						foodDrinkState._cfgFoodDrinkStatus = string.Empty;
						foodDrinkState._cfgFoodDrinkCandidates.Clear();
						foodDrinkState._cfgFoodDrinkScanRequested = true; // auto-trigger scan for new kind
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
		#endregion

		// Helper method to render configuration tools panel
		private static void RenderConfigurationTools(SectionData selectedSection)
		{
			var mainWindowState = _state.GetState<State_MainWindow>();
			var bardEditorState = _state.GetState<State_BardEditor>();

			if (mainWindowState.Currently_EditableSpell == null) return;

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
			bool hasKeySelected = !string.IsNullOrEmpty(mainWindowState.SelectedKey);
			bool specialSectionAllowsNoKey = string.Equals(mainWindowState.SelectedSection, "Ifs", StringComparison.OrdinalIgnoreCase) || string.Equals(mainWindowState.SelectedSection, "Burn", StringComparison.OrdinalIgnoreCase);
			if (!hasKeySelected && !specialSectionAllowsNoKey)
			{
				imgui_TextColored(0.9f, 0.9f, 0.9f, 1.0f, "Select a configuration key to see available tools.");
				return;
			}

			imgui_TextColored(0.95f, 0.85f, 0.35f, 1.0f, "Configuration Tools");
			imgui_Separator();

			// Value actions at the top (when a value is selected)
			if (mainWindowState.SelectedValueIndex >= 0 && hasKeySelected)
			{
				var kd = selectedSection?.Keys?.GetKeyData(mainWindowState.SelectedKey ?? string.Empty);
				var values = GetValues(kd);
				if (mainWindowState.SelectedValueIndex < values.Count)
				{
					string selectedValue = values[mainWindowState.SelectedValueIndex];
					if (mainWindowState.Currently_EditableSpell != null)
					{
						imgui_TextColored(0.95f, 0.85f, 0.35f, 1.0f, "Value Actions");

						// Delete Value button (red)
						imgui_PushStyleColor((int)ImGuiCol.Button, 0.85f, 0.30f, 0.30f, 1.0f);
						imgui_PushStyleColor((int)ImGuiCol.ButtonHovered, 0.95f, 0.40f, 0.40f, 1.0f);
						imgui_PushStyleColor((int)ImGuiCol.ButtonActive, 0.75f, 0.20f, 0.20f, 1.0f);
						if (imgui_Button("Delete Value"))
						{
							// Delete the currently selected value
							var pdAct = data.GetActiveCharacterIniData();
							var selSec = pdAct.Sections.GetSectionData(mainWindowState.SelectedSection);
							var key = selSec?.Keys.GetKeyData(mainWindowState.SelectedKey);
							if (key != null)
							{
								var vals = GetValues(key);
								if (mainWindowState.SelectedValueIndex >= 0 && mainWindowState.SelectedValueIndex < vals.Count)
								{
									vals.RemoveAt(mainWindowState.SelectedValueIndex);
									mainWindowState.SelectedValueIndex = -1; // Clear selection after delete
									data.RefreshEditableSpellState(force: true);


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

						string btnLabel = mainWindowState.Show_ShowIntegratedEditor ? "Hide Editor" : "Show Editor";
						if (imgui_Button(btnLabel))
						{
							mainWindowState.Show_ShowIntegratedEditor = !mainWindowState.Show_ShowIntegratedEditor;
							if (mainWindowState.Show_ShowIntegratedEditor)
							{
								// Initialize manual edit buffer when opening
								var keyData = selectedSection?.Keys?.GetKeyData(mainWindowState.SelectedKey ?? string.Empty);
								var valuesList = GetValues(keyData);
								if (mainWindowState.SelectedValueIndex >= 0 && mainWindowState.SelectedValueIndex < valuesList.Count)
								{
									_cfgManualEditBuffer = valuesList[mainWindowState.SelectedValueIndex] ?? string.Empty;
								}
							}
						}

						imgui_PopStyleColor(4);
						string editorHint = mainWindowState.Show_ShowIntegratedEditor ? "Editor panel is open below." : "Click to show the advanced editor.";
						imgui_TextColored(0.7f, 0.8f, 0.9f, 1.0f, editorHint);
						imgui_Separator();
					}
				}
			}


			// Special section buttons
			bool isHeals = string.Equals(mainWindowState.SelectedSection, "Heals", StringComparison.OrdinalIgnoreCase);
			bool isTankKey = string.Equals(mainWindowState.SelectedKey, "Tank", StringComparison.OrdinalIgnoreCase);
			bool isImpKey = string.Equals(mainWindowState.SelectedKey, "Important Bot", StringComparison.OrdinalIgnoreCase);

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
			if (string.Equals(mainWindowState.SelectedSection, "Ifs", StringComparison.OrdinalIgnoreCase))
			{
				if (imgui_Button("Sample If's"))
				{
					try { LoadSampleIfsForModal(); _state.Show_IfSampleModal = true; }
					catch (Exception ex) { _cfgIfSampleStatus = "Load failed: " + (ex.Message ?? "error"); _state.Show_IfSampleModal = true; }
				}
			}

			// Burn section: add-new key helper
			if (string.Equals(mainWindowState.SelectedSection, "Burn", StringComparison.OrdinalIgnoreCase))
			{
			}

			imgui_Separator();

			// Display selected value information
			if (mainWindowState.SelectedValueIndex >= 0)
			{
				var kd = selectedSection?.Keys?.GetKeyData(mainWindowState.SelectedKey ?? string.Empty);
				var values = GetValues(kd);
				if (mainWindowState.SelectedValueIndex < values.Count)
				{
					string selectedValue = values[mainWindowState.SelectedValueIndex];
					var editableSpell = mainWindowState.Currently_EditableSpell;
					
					string lookupName = editableSpell?.CastName;
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
						var spellInfo = data.FindSpellItemAAByName(lookupName);
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
				
			}
			
		}

		private static void RenderActiveModals(SectionData selectedSection)
		{
			if (_state.Show_AddModal)
			{
				RenderAddFromCatalogModal(data.GetActiveCharacterIniData(), selectedSection);
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
				string currentPath = data.GetCurrentCharacterIniPath();
				string selectedPath = data.GetActiveSettingsPath();
				var pd = data.GetActiveCharacterIniData();
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
				string currentPath = data.GetCurrentCharacterIniPath();
				string selectedPath = data.GetActiveSettingsPath();

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
	
		

		// Fetch gem data from peer catalog response (now includes spell icon indices)
		
		// Helper method to get spell icon index for local spells
		
		// PubSub relay approach: request peer to publish SpellDataList as base64 on response topic
	
		private static bool IsActiveIniBard()
		{
			try
			{
				var chardata = data.GetActiveCharacterIniData();
				if (chardata?.Sections != null)
				{
					if (chardata.Sections.ContainsSection("Bard")) return true;
				}

				string path = data.GetActiveSettingsPath() ?? string.Empty;
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

		

		
	
		// Resolves a toon’s ini path by scanning known .ini files and preferring a match
		// that includes the server name if we have it.
		
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
		private static bool TryGetValidOptionsForKey(string keyLabel, out List<string> options)
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
			var gemstate = _state.GetState<State_CatalogGems>();

			// Color code the source based on type
			if (gemstate._cfg_CatalogSource.StartsWith("Remote"))
				imgui_TextColored(0.7f, 1.0f, 0.7f, 1.0f, gemstate._cfg_CatalogSource); // Green for remote
			else if (gemstate._cfg_CatalogSource.StartsWith("Local (fallback)"))
				imgui_TextColored(1.0f, 0.8f, 0.4f, 1.0f, gemstate._cfg_CatalogSource); // Orange for fallback
			else if (gemstate._cfg_CatalogSource.StartsWith("Local"))
				imgui_TextColored(0.8f, 0.8f, 1.0f, 1.0f, gemstate._cfg_CatalogSource); // Light blue for local
			else
				imgui_TextColored(0.8f, 0.8f, 0.8f, 1.0f, gemstate._cfg_CatalogSource); // Gray for unknown

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

								var kd = data.GetCurrentEditedSpellKeyData();
								if (kd != null)
								{
									var vals = GetValues(kd);
									string v = (state.SelectedCategorySpell.Name ?? string.Empty).Trim();
									if (state.ReplaceMode && state.ReplaceIndex >= 0 && state.ReplaceIndex < vals.Count)
									{
										vals[state.ReplaceIndex] = v;
										mainWindowState.PendingValueSelection = state.ReplaceIndex;
										state.ReplaceMode = false;
										state.ReplaceIndex = -1;
										data.RefreshEditableSpellState(force:true);
										_state.Show_AddModal = false;//close the window
										
									}
									else if (!vals.Contains(v, StringComparer.OrdinalIgnoreCase))
									{
										vals.Add(v);
										mainWindowState.PendingValueSelection = vals.Count - 1;
										_state.Show_AddModal = false;//close the window
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
					var currentCatalog = data.GetCatalogByType(state.CurrentAddType);

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

		
		// Search all catalogs for a spell/item/AA by name
	

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

			var pd = data.GetActiveCharacterIniData();
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

										if (data.TrySaveIniValueForToon(row.Key, mainWindowState.SelectedSection, mainWindowState.SelectedKey, newValue, out var err))
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
	
		private static void EnsureConfigEditorInit()
		{
			if (_cfg_Inited) return;
			_cfg_Inited = true;
			data.BuildConfigSectionOrder();
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
		var pd = data.GetActiveCharacterIniData();
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
		
		// Ifs sample import helpers and modal
		private static string ResolveSampleIfsPath()
		{
			var dirs = new List<string>();
			try
			{
				string cfg = data.GetActiveSettingsPath();
				if (!string.IsNullOrEmpty(cfg))
				{
					var dir = Path.GetDirectoryName(cfg);
					if (!string.IsNullOrEmpty(dir)) dirs.Add(dir);
				}
			}
			catch { }
			try
			{
				string botIni = data.GetCurrentCharacterIniPath();
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
				var pd = data.GetActiveCharacterIniData();
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
				var pd = data.GetActiveCharacterIniData();
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
				var pd = data.GetActiveCharacterIniData();
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
			if (data._spellKeyAliasMap.TryGetValue(key, out var mapped))
			{
				return mapped;
			}
			return key;
		}

	
		private static readonly Dictionary<string, (float r, float g, float b, float a)> _richTextColorMapping = new Dictionary<string, (float, float, float, float)>(StringComparer.OrdinalIgnoreCase)
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
					if (_richTextColorMapping.TryGetValue(rawText[i], out var color))
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

		
		private static void InvalidateSpellEditState()
		{
		
		}

		private const float SpellEditorDefaultTextWidth = 320f;
		private const float SpellEditorDefaultNumberWidth = 140f;
        private const float SpellEditorDefaultCheckboxWidth = 20f;

        private static void RenderTableTextEditRow(string id, string label,string current, Action<string> action, string tooltip = null, float width = SpellEditorDefaultTextWidth)
		{
			var spellEditorState = _state.GetState<State_SpellEditor>();
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
			imgui_SetNextItemWidth(width);
		
			if (imgui_InputText(id, current))
			{
				string updated = imgui_InputText_Get(id) ?? string.Empty;
				action.Invoke(updated);
				spellEditorState.IsDirty = true;
			}
		}
		private static void RenderTableIntEditRow(string id, string label, int current, Action<Int32> action, string tooltip=null, float width = SpellEditorDefaultNumberWidth)
		{
			var spellEditorState = _state.GetState<State_SpellEditor>();
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
			imgui_SetNextItemWidth(width);

			if (imgui_InputInt(id, current,1,2))
			{
				int updated = imgui_InputInt_Get(id);
				action.Invoke(updated);
				spellEditorState.IsDirty = true;
			}
		}

		private static void RenderTableCheckboxEditRow(string id, string label, bool current, Action<bool> action, string tooltip = null, float width = SpellEditorDefaultCheckboxWidth)
		{
			var spellEditorState = _state.GetState<State_SpellEditor>();
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
            imgui_SetNextItemWidth(width);

            if (imgui_Checkbox(id, current))
			{
				bool updated = imgui_Checkbox_Get(id);
				action.Invoke(updated);
				spellEditorState.IsDirty = true;
			}
			
		}

		#region RenderSpellEditor
		private static void RenderSpellModifierEditor2_Tab_General()
		{
			var spellEditorState = _state.GetState<State_SpellEditor>();
			var mainWindowState = _state.GetState<State_MainWindow>();

			imgui_TextColored(0.8f, 0.9f, 0.95f, 1.0f, "Basics");
			const ImGuiTableFlags FieldTableFlags = (ImGuiTableFlags.ImGuiTableFlags_SizingStretchProp | ImGuiTableFlags.ImGuiTableFlags_PadOuterX);
			const ImGuiTableColumnFlags LabelColumnFlags = (ImGuiTableColumnFlags.ImGuiTableColumnFlags_WidthFixed | ImGuiTableColumnFlags.ImGuiTableColumnFlags_NoResize);
			const ImGuiTableColumnFlags ValueColumnFlags = ImGuiTableColumnFlags.ImGuiTableColumnFlags_WidthStretch;
			const float ComboFieldWidth = 220f;
			var currentSpell = mainWindowState.Currently_EditableSpell;

			if (imgui_BeginTable("GeneralTabTable", 2, (int)FieldTableFlags, imgui_GetContentRegionAvailX(), 0))
			{
				try
				{
					imgui_TableSetupColumn("Label", (int)LabelColumnFlags, 0f);
					imgui_TableSetupColumn("Value", (int)ValueColumnFlags, 0f);


					//to specify the id of an input text use ## so its not visable
					
					RenderTableTextEditRow("##SpellEditor_CastName", "Cast Name:", currentSpell.CastName, (u) =>{ currentSpell.CastName = u; currentSpell.SpellName = u; });
					RenderTableTextEditRow("##SpellEditor_CastTarget", "Cast Target:", currentSpell.CastTarget, (u) =>{	currentSpell.CastTarget = u; });
					///GEM SLOTS
					imgui_TableNextRow();
					imgui_TableNextColumn();
					imgui_Text("Gem Slot:");
					imgui_TableNextColumn();
					var gemSlot = currentSpell.SpellGem;
					string gemPreview = gemSlot >= 1 && gemSlot <= 12 ? $"Gem {gemSlot}" : "No Gem";
					imgui_SetNextItemWidth(ComboFieldWidth);
					if (BeginComboSafe($"##spell_gem", gemPreview))
					{
						if (imgui_Selectable("No Gem", gemSlot == 0))
						{
							currentSpell.SpellGem = 0;
							spellEditorState.IsDirty = true;
						}
						for (int g = 1; g <= 12; g++)
						{
							bool sel = gemSlot == g;
							if (imgui_Selectable($"Gem {g}", sel))
							{
								currentSpell.SpellGem = g;
								gemSlot = g;
								spellEditorState.IsDirty = true;
							}
						}
						EndComboSafe();
					}

					//CAST TYPES
					imgui_TableNextRow();
					imgui_TableNextColumn();
					imgui_Text("Cast Type Override:");
					imgui_TableNextColumn();
					string castTypeString = currentSpell.CastTypeOverride.ToString();
					//string castPreview = string.IsNullOrEmpty(castTypeString) ? "Auto (Detect)" : castTypeString;
					imgui_SetNextItemWidth(ComboFieldWidth);
					if (BeginComboSafe($"##spell_casttype", castTypeString))
					{	
						foreach (var option in data._spellCastTypeOptions)
						{
							bool sel = string.Equals(castTypeString, option, StringComparison.OrdinalIgnoreCase);
							if (imgui_Selectable(option, sel))
							{
								currentSpell.CastTypeOverride = (CastingType)Enum.Parse(typeof(CastingType), option);
								spellEditorState.IsDirty = true;
							}
						}
						EndComboSafe();
					}
					RenderTableCheckboxEditRow("##spell_enabled", "Enabled:", currentSpell.Enabled, (u) => { currentSpell.Enabled = u; }, tooltip: "Enable or Disable entry");
					
				}
				finally
				{
					imgui_EndTable();
				}

			}
		}
		private static void RenderSpellModifierEditor2_Tab_Conditions()
		{
			var spellEditorState = _state.GetState<State_SpellEditor>();
			var mainWindowState = _state.GetState<State_MainWindow>();
			var currentSpell = mainWindowState.Currently_EditableSpell;

			imgui_TextColored(0.8f, 0.9f, 0.95f, 1.0f, "Logic");
			const ImGuiTableFlags FieldTableFlags = (ImGuiTableFlags.ImGuiTableFlags_SizingStretchProp | ImGuiTableFlags.ImGuiTableFlags_PadOuterX);
			const ImGuiTableColumnFlags LabelColumnFlags = (ImGuiTableColumnFlags.ImGuiTableColumnFlags_WidthFixed | ImGuiTableColumnFlags.ImGuiTableColumnFlags_NoResize);
			const ImGuiTableColumnFlags ValueColumnFlags = ImGuiTableColumnFlags.ImGuiTableColumnFlags_WidthStretch;
		
			if (imgui_BeginTable("GeneralTabTable", 2, (int)FieldTableFlags, imgui_GetContentRegionAvailX(), 0))
			{
				try
				{
					imgui_TableSetupColumn("Label", (int)LabelColumnFlags, 0f);
					imgui_TableSetupColumn("Value", (int)ValueColumnFlags, 0f);
					RenderTableTextEditRow("##SpellEditor_ifsKeys", "Ifs Keys:", currentSpell.IfsKeys, (u) => { currentSpell.IfsKeys = u; });
					RenderTableTextEditRow("##SpellEditor_CheckFor", "Check For:", String.Join(",", currentSpell.CheckForCollection.Keys), (u) =>
					{
						currentSpell.CheckForCollection.Clear();
						var split = u.Split(',');
						foreach(var check in split)
						{

							string tKey = check.Trim();
							if (String.IsNullOrWhiteSpace(tKey)) continue;
							if(!currentSpell.CheckForCollection.ContainsKey(tKey))
							{
								currentSpell.CheckForCollection.Add(tKey, 0);
							}
						}
					});
					RenderTableTextEditRow("##SpellEditor_CastIfs", "Cast If:", currentSpell.CastIF, (u) =>{ currentSpell.CastIF = u; });
					RenderTableTextEditRow("##SpellEditor_Zone", "Zone:", currentSpell.Zone, (u) =>{ currentSpell.Zone = u; });
					RenderTableTextEditRow("##SpellEditor_MinSick", "Min Sick:", currentSpell.MinSick.ToString(), (u) => { Int32.TryParse(u, out currentSpell.MinSick);});
					RenderTableTextEditRow("##SpellEditor_TriggerSpell", "Trigger Spell:", currentSpell.TriggerSpell, (u) => { currentSpell.TriggerSpell = u; });
				}
				finally
				{
					imgui_EndTable();
				}
			}
		}
		private static void RenderSpellModifierEditor2_Tab_Resources()
		{
			var spellEditorState = _state.GetState<State_SpellEditor>();
			imgui_TextColored(0.8f, 0.9f, 0.95f, 1.0f, "Logic");
			const ImGuiTableFlags FieldTableFlags = (ImGuiTableFlags.ImGuiTableFlags_SizingStretchProp | ImGuiTableFlags.ImGuiTableFlags_PadOuterX);
			const ImGuiTableColumnFlags LabelColumnFlags = (ImGuiTableColumnFlags.ImGuiTableColumnFlags_WidthFixed | ImGuiTableColumnFlags.ImGuiTableColumnFlags_NoResize);
			const ImGuiTableColumnFlags ValueColumnFlags = ImGuiTableColumnFlags.ImGuiTableColumnFlags_WidthStretch;
			var mainWindowState = _state.GetState<State_MainWindow>();
			var currentSpell = mainWindowState.Currently_EditableSpell;

			if (imgui_BeginTable("SpellResourceTable", 2, (int)FieldTableFlags, imgui_GetContentRegionAvailX(), 0))
			{
				try
				{
					imgui_TableSetupColumn("Label", (int)LabelColumnFlags, 0f);
					imgui_TableSetupColumn("Value", (int)ValueColumnFlags, 0f);

					RenderTableIntEditRow("##SpellEditor_MinMana", "Min Mana:", currentSpell.MinMana, (u) =>{ currentSpell.MinMana = u; });
					RenderTableIntEditRow("##SpellEditor_MinEnd", "Min End:", currentSpell.MinEnd, (u) =>{ currentSpell.MinEnd = u; });
					RenderTableIntEditRow("##SpellEditor_MinPctHP", "Min HP%:", currentSpell.MinHP, (u) =>{	currentSpell.MinEnd = u; });
					RenderTableIntEditRow("##SpellEditor_MinHPTotal", "Min HP%:", currentSpell.MinHPTotal, (u) =>{ currentSpell.MinHPTotal = u;});
					RenderTableIntEditRow("##SpellEditor_HealPct", "Heal %:", currentSpell.HealPct, (u) =>{ currentSpell.HealPct = u;});
					RenderTableIntEditRow("##SpellEditor_HealthMax", "Cancel Heal Above %:", currentSpell.HealthMax, (u) =>{ currentSpell.HealthMax = u;});
					RenderTableIntEditRow("##SpellEditor_PctAggro", "Pct Aggro:", currentSpell.PctAggro, (u) =>{ currentSpell.PctAggro = u;}, tooltip:"Skip this entry if your current aggro percent exceeds the specified threshold.");
					RenderTableIntEditRow("##SpellEditor_MinAggro", "Min Aggro:", currentSpell.MinAggro, (u) => { currentSpell.MinAggro = u; }, tooltip: "Only cast when your aggro percent is at least this value (helps gate low-threat openers).");
					RenderTableIntEditRow("##SpellEditor_MaxAggro", "Max Aggro:", currentSpell.MaxAggro, (u) =>{ currentSpell.MaxAggro = u; }, tooltip: "Do not cast once your aggro percent is above this value (useful for backing off).");
				}
				finally
				{
					imgui_EndTable();

				}
			}
		
		}
		private static void RenderSpellModifierEditor2_Tab_Timing()
		{
			var spellEditorState = _state.GetState<State_SpellEditor>();
			imgui_TextColored(0.8f, 0.9f, 0.95f, 1.0f, "Delays");
			const ImGuiTableFlags FieldTableFlags = (ImGuiTableFlags.ImGuiTableFlags_SizingStretchProp | ImGuiTableFlags.ImGuiTableFlags_PadOuterX);
			const ImGuiTableColumnFlags LabelColumnFlags = (ImGuiTableColumnFlags.ImGuiTableColumnFlags_WidthFixed | ImGuiTableColumnFlags.ImGuiTableColumnFlags_NoResize);
			const ImGuiTableColumnFlags ValueColumnFlags = ImGuiTableColumnFlags.ImGuiTableColumnFlags_WidthStretch;
			var mainWindowState = _state.GetState<State_MainWindow>();
			var currentSpell = mainWindowState.Currently_EditableSpell;

			if (imgui_BeginTable("SpellTimingTable", 2, (int)FieldTableFlags, imgui_GetContentRegionAvailX(), 0))
			{
				try
				{
					imgui_TableSetupColumn("Label", (int)LabelColumnFlags, 0f);
					imgui_TableSetupColumn("Value", (int)ValueColumnFlags, 0f);

					RenderTableIntEditRow("##SpellEditor_Delay", "Delay:", currentSpell.Delay, (u) => { currentSpell.Delay = u; });
					RenderTableIntEditRow("##SpellEditor_RecastDelay", "Recast Delay:", currentSpell.RecastDelay, (u) => { currentSpell.RecastDelay = u; });
					RenderTableIntEditRow("##SpellEditor_DelayBeforeRecast", "Min Duration Before Recast:", (int)currentSpell.MinDurationBeforeRecast, (u) => { currentSpell.MinDurationBeforeRecast = u; });
					RenderTableIntEditRow("##SpellEditor_BeforeSpellDelay", "Before Spell Delay:", currentSpell.BeforeSpellDelay, (u) => { currentSpell.BeforeSpellDelay = u; });
					RenderTableIntEditRow("##SpellEditor_AfterSpellDelay", "After Spell Delay:", currentSpell.AfterSpellDelay, (u) => { currentSpell.AfterSpellDelay = u; });

					RenderTableIntEditRow("##SpellEditor_BeforeEventDelay", "Before Event Delay:", currentSpell.BeforeEventDelay, (u) => { currentSpell.BeforeEventDelay = u; });
					RenderTableIntEditRow("##SpellEditor_AfterEventDelay", "After Event Delay:", currentSpell.AfterEventDelay, (u) => { currentSpell.AfterEventDelay = u; });
					
					RenderTableIntEditRow("##SpellEditor_AfterCastDelay", "After Cast Delay:", currentSpell.AfterCastDelay, (u) => { currentSpell.AfterCastDelay = u; });
					RenderTableIntEditRow("##SpellEditor_AfterCastCompletedDelay", "After Cast Completed Delay:", currentSpell.AfterCastCompletedDelay, (u) => { currentSpell.AfterCastCompletedDelay = u; });


					RenderTableIntEditRow("##SpellEditor_MaxTries", "Max Tries:", currentSpell.MaxTries, (u) => { currentSpell.MaxTries = u; });
					RenderTableIntEditRow("##SpellEditor_SongRefreshTime", "Song Refresh Time:", currentSpell.SongRefreshTime, (u) => { currentSpell.SongRefreshTime = u; });
					
				}
				finally
				{
					imgui_EndTable();

				}
			}

		}
		private static void RenderSpellModifierEditor2_Tab_Advanced()
		{
			var spellEditorState = _state.GetState<State_SpellEditor>();
			imgui_TextColored(0.8f, 0.9f, 0.95f, 1.0f, "Ordering");
			const ImGuiTableFlags FieldTableFlags = (ImGuiTableFlags.ImGuiTableFlags_SizingStretchProp | ImGuiTableFlags.ImGuiTableFlags_PadOuterX);
			const ImGuiTableColumnFlags LabelColumnFlags = (ImGuiTableColumnFlags.ImGuiTableColumnFlags_WidthFixed | ImGuiTableColumnFlags.ImGuiTableColumnFlags_NoResize);
			const ImGuiTableColumnFlags ValueColumnFlags = ImGuiTableColumnFlags.ImGuiTableColumnFlags_WidthStretch;
			var mainWindowState = _state.GetState<State_MainWindow>();
			var currentSpell = mainWindowState.Currently_EditableSpell;

			if (imgui_BeginTable("SpellOrderingTable", 2, (int)FieldTableFlags, imgui_GetContentRegionAvailX(), 0))
			{
				try
				{
					imgui_TableSetupColumn("Label", (int)LabelColumnFlags, 0f);
					imgui_TableSetupColumn("Value", (int)ValueColumnFlags, 0f);
					RenderTableTextEditRow("##SpellEditor_BeforeSpell", "Before Spell:", currentSpell.BeforeSpell, (u) => { currentSpell.BeforeSpell = u; });
					RenderTableTextEditRow("##SpellEditor_AfterSpell", "After Spell:", currentSpell.AfterSpell, (u) => { currentSpell.AfterSpell = u; });
					RenderTableTextEditRow("##SpellEditor_BeforeEvent", "Before Event:", currentSpell.BeforeEventKeys, (u) => { currentSpell.BeforeEventKeys = u; });
					RenderTableTextEditRow("##SpellEditor_AfterEvent", "After Event:", currentSpell.AfterEventKeys, (u) => { currentSpell.AfterEventKeys = u; });
					RenderTableTextEditRow("##SpellEditor_Regent", "Regent:", currentSpell.Reagent, (u) => { currentSpell.Reagent = u; });
				}
				finally
				{
					imgui_EndTable();
				}
			}
			imgui_Separator();
			imgui_TextColored(0.8f, 0.9f, 0.95f, 1.0f, "Exclusions");

			if (imgui_BeginTable("SpellExclusionsTable", 2, (int)FieldTableFlags, imgui_GetContentRegionAvailX(), 0))
			{
				try
				{
					imgui_TableSetupColumn("Label", (int)LabelColumnFlags, 0f);
					imgui_TableSetupColumn("Value", (int)ValueColumnFlags, 0f);
					RenderTableTextEditRow("##SpellEditor_ExcludeClasses", "Excluded Classes:", String.Join(",", currentSpell.ExcludedClasses.ToList()), (u) => {

						string[] excludeClasses = u.Split(',');
						foreach (var eclass in excludeClasses)
						{
							var tclass = eclass.Trim();

							if (!currentSpell.ExcludedClasses.Contains(tclass.Trim()))
							{
								currentSpell.ExcludedClasses.Add(tclass.Trim());
							}
						}
					},tooltip:"Short class name: IE: WAR,PAL,SHD comma seperated");
					RenderTableTextEditRow("##SpellEditor_ExcludeNames", "Excluded Names:", String.Join(",", currentSpell.ExcludedNames.ToList()), (u) => {

						string[] excludeClasses = u.Split(',');
						foreach (var ename in excludeClasses)
						{
							var tname = ename.Trim();

							if (!currentSpell.ExcludedNames.Contains(tname.Trim()))
							{
								currentSpell.ExcludedNames.Add(tname.Trim());
							}
						}
					}, tooltip: "Name of toons in your party, comma seperated");
				}
				finally
				{
					imgui_EndTable();

				}
			}
			
		}
	
		private static void RenderSpellModifierEditor2_Tab_Flags()
		{
			var spellEditorState = _state.GetState<State_SpellEditor>();
			imgui_TextColored(0.8f, 0.9f, 0.95f, 1.0f, "Flags");
			const ImGuiTableFlags FlagTableFlags = (ImGuiTableFlags.ImGuiTableFlags_SizingStretchProp | ImGuiTableFlags.ImGuiTableFlags_PadOuterX);
			const ImGuiTableColumnFlags FlagLabelColumnFlags = (ImGuiTableColumnFlags.ImGuiTableColumnFlags_WidthFixed | ImGuiTableColumnFlags.ImGuiTableColumnFlags_NoResize);
			const ImGuiTableColumnFlags FlagCheckboxColumnFlags = (ImGuiTableColumnFlags.ImGuiTableColumnFlags_WidthFixed | ImGuiTableColumnFlags.ImGuiTableColumnFlags_NoResize);
			const ImGuiTableColumnFlags FlagSpacerColumnFlags = ImGuiTableColumnFlags.ImGuiTableColumnFlags_WidthStretch;
			const float FlagLabelPadding = 12f;
			const float FlagCheckboxColumnWidth = 32f;
			var mainWindowState = _state.GetState<State_MainWindow>();
			var currentSpell = mainWindowState.Currently_EditableSpell;

			if (imgui_BeginTable($"E3SpellFlagTable", 3, (int)FlagTableFlags, imgui_GetContentRegionAvailX(), 0))
			{
				try
				{
					float flagLabelColumnWidth = Math.Max(200f, imgui_CalcTextSizeX("Gift of Mana Required:") + FlagLabelPadding);
					imgui_TableSetupColumn("FlagColumnLabel", (int)FlagLabelColumnFlags, flagLabelColumnWidth);
					imgui_TableSetupColumn("FlagColumnCheckbox", (int)FlagCheckboxColumnFlags, FlagCheckboxColumnWidth);
					imgui_TableSetupColumn("FlagColumnSpacer", (int)FlagSpacerColumnFlags, 0f);
					RenderTableCheckboxEditRow("##Flag_NoInterrupt", "No Interrupt:", currentSpell.NoInterrupt, (u) => { currentSpell.NoInterrupt = u; }, tooltip: "Do not interrupt this cast for emergency heals, nowcasts, or queued commands once the bar starts.");
					RenderTableCheckboxEditRow("##Flag_IgnoreStackRules", "Ignore Stack Rules:", currentSpell.IgnoreStackRules, (u) => { currentSpell.IgnoreStackRules = u; }, tooltip: "Skip the Spell.StacksTarget check; cast even if EQ reports the effect will not land due to stacking.");
					RenderTableCheckboxEditRow("##Flag_NoTarget", "No Target:", currentSpell.NoTarget, (u) => { currentSpell.NoTarget = u; }, tooltip: "Leave the current target untouched so the spell can fire on self or without a target lock.\"");
					RenderTableCheckboxEditRow("##Flag_NoAggro", "No Aggro:", currentSpell.NoAggro, (u) => { currentSpell.NoAggro = u; }, tooltip: "Suppress this spell if the mob currently has you targeted to reduce aggro spikes.");
					RenderTableCheckboxEditRow("##Flag_NoMidSongCast", "No Mid Song Cast:", currentSpell.NoMidSongCast, (u) => { currentSpell.NoMidSongCast = u; }, tooltip: "Bards: block this action while a song is already channeling so twisting is not disrupted.");
					RenderTableCheckboxEditRow("##Flag_GoM", "Gift of Mana Required:", currentSpell.GiftOfMana, (u) => { currentSpell.GiftOfMana = u; }, tooltip: "Only cast when a Gift of Mana-style proc is active, saving mana on expensive spells.");
					RenderTableCheckboxEditRow("##Flag_Debug", "Debug output:", currentSpell.Debug, (u) => { currentSpell.Debug = u; }, tooltip: "Enable detailed logging for this spell to the MQ chat/log window.");
				}
				finally
				{
					imgui_EndTable();
				}

			}
			
		}
		private static void RenderSpellModifierEditor2_Tab_Manual()
		{
			var spellEditorState = _state.GetState<State_SpellEditor>();
			var mainWindowState = _state.GetState<State_MainWindow>();
			var currentSpell = mainWindowState.Currently_EditableSpell;
			imgui_TextColored(0.8f, 0.9f, 0.95f, 1.0f, "Manual Text Editor");
			imgui_Text("Edit the raw configuration value directly. Changes apply when you click Apply.");
			imgui_Separator();

			// Text area for manual editing
			float textWidth = Math.Max(500f, imgui_GetContentRegionAvailX() * 0.95f);
			float textHeight = Math.Max(180f, imgui_GetTextLineHeightWithSpacing() * 10f);
		
	
			if(String.IsNullOrWhiteSpace(spellEditorState.ManualEditBuffer))
			{
				spellEditorState.ManualEditBuffer = currentSpell.ToConfigEntry();
			}

			if (imgui_InputTextMultiline($"##manual_edit", spellEditorState.ManualEditBuffer, textWidth, textHeight))
			{
				spellEditorState.ManualEditBuffer = imgui_InputText_Get($"##manual_edit");
				spellEditorState.IsDirty = true;
				spellEditorState.ManualInputBufferInUse = true;

			}

		}

		#endregion

		/// <summary>
		/// Used to determine if the UI state can return a valid spell. 
		/// </summary>
		/// <returns>Spell object</returns>
		
		private static void RenderSpellEditor()
		{
			var mainWindowState = _state.GetState<State_MainWindow>();
			var spellEditorState = _state.GetState<State_SpellEditor>();

			//check to see if there is a spell currently selected to edit.
			var editableSpell = mainWindowState.Currently_EditableSpell;
			if (editableSpell == null) return; //nothing to edit here
			//necessary to update the actual entry.
			var kd = data.GetCurrentEditedSpellKeyData();
			if (kd == null) return;

			
			var rawValue = editableSpell.RawEntry;
	
			string entryLabel = $"[{mainWindowState.SelectedSection}] {mainWindowState.SelectedKey} entry #{mainWindowState.SelectedValueIndex + 1}";


			// Header row with title on left and buttons on right
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
			string idForAppply = $"Apply##spell_apply";
			if(spellEditorState.IsDirty) idForAppply= $"Apply*##spell_apply";
			if (imgui_Button(idForAppply))
			{
				_log.Write($"Manual Buffer update:[{spellEditorState.ManualEditBuffer}] bufferInuse:{spellEditorState.ManualInputBufferInUse}", Logging.LogLevels.Debug);

				if (spellEditorState.ManualInputBufferInUse && !String.IsNullOrWhiteSpace(spellEditorState.ManualEditBuffer))
				{
					try
					{
						_log.Write($"Manual Buffer update:[{spellEditorState.ManualEditBuffer}]", Logging.LogLevels.Debug);

						var tspell = new Spell(spellEditorState.ManualEditBuffer, mainWindowState.CurrentINIData, false);
						mainWindowState.Currently_EditableSpell = tspell;
					}
					catch(Exception ex)
					{
						_log.Write("Exception creating spell from the manual buffer.", Logging.LogLevels.Debug);
					}
				}
				//save the value to the current key data
				kd.ValueList[mainWindowState.SelectedValueIndex] = mainWindowState.Currently_EditableSpell.ToConfigEntry();
				spellEditorState.Reset();
			}
			imgui_SameLine();
			if (imgui_Button($"Reset##spell_reset"))
			{
				data.RefreshEditableSpellState(force:true);
				editableSpell = mainWindowState.Currently_EditableSpell;
				spellEditorState.Reset();
			}
		
			imgui_Separator();

			if (imgui_BeginTabBar($"SpellModifierTabs"))
			{
				if (imgui_BeginTabItem($"General##spell_tab_general"))
				{
					RenderSpellModifierEditor2_Tab_General();
					imgui_EndTabItem();
				}
				if (imgui_BeginTabItem($"Conditions##spell_tab_conditions"))
				{
					RenderSpellModifierEditor2_Tab_Conditions();
					imgui_EndTabItem();
				}
				if (imgui_BeginTabItem($"Resources##spell_tab_resources"))
				{
					RenderSpellModifierEditor2_Tab_Resources();
					imgui_EndTabItem();
				}
				if (imgui_BeginTabItem($"Timing##spell_tab_timing"))
				{
					RenderSpellModifierEditor2_Tab_Timing();
					imgui_EndTabItem();
				}
				if (imgui_BeginTabItem($"Advanced##spell_tab_advanced"))
				{
					RenderSpellModifierEditor2_Tab_Advanced();
					imgui_EndTabItem();
				}
				if (imgui_BeginTabItem($"Flags##spell_tab_flags"))
				{
					RenderSpellModifierEditor2_Tab_Flags();
					imgui_EndTabItem();
				}
				if (imgui_BeginTabItem($"Manual Edit##spell_tab_manual"))
				{
					RenderSpellModifierEditor2_Tab_Manual();
					imgui_EndTabItem();
				}
				imgui_EndTabBar();
			}

		

			imgui_Separator();
			imgui_TextColored(0.8f, 0.9f, 0.95f, 1.0f, "Preview");

			string previewString = spellEditorState.CurrentSpellPreviewCache;
			if (String.IsNullOrWhiteSpace(previewString) || spellEditorState.IsDirty)
			{
				spellEditorState.CurrentSpellPreviewCache= editableSpell.ToConfigEntry();
				previewString = spellEditorState.CurrentSpellPreviewCache;
			}
			string preview = previewString;
			imgui_TextWrapped(string.IsNullOrEmpty(preview) ? "(empty)" : preview);
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

			if (kd == null) return;

			//if we are the same object do anything.
			var mainWindowState = _state.GetState<State_MainWindow>();
			mainWindowState.ConfigIsDirty = true;

			if (Object.ReferenceEquals(kd.ValueList, values)) return;

			// Preserve exact row semantics: one value per row, including empties
			if (kd.ValueList != null)
			{
				kd.ValueList.Clear();
				foreach (var v in values) kd.ValueList.Add(v ?? string.Empty);
			}

		}

		// Inventory scanning for Food/Drink using MQ TLOs (non-blocking via ProcessBackgroundWork trigger)
		private static void RenderFoodDrinkPicker(SectionData selectedSection)
		{
			// Respect current open state instead of forcing true every frame
			bool shouldDraw = imgui_Begin(_state.WinName_FoodDrinkModal, (int)(ImGuiWindowFlags.ImGuiWindowFlags_AlwaysAutoResize | ImGuiWindowFlags.ImGuiWindowFlags_NoCollapse | ImGuiWindowFlags.ImGuiWindowFlags_NoDocking));

			if (shouldDraw)
			{
				var mainWindowState = _state.GetState<State_MainWindow>();
				var foodDrinkState = _state.GetState<State_FoodDrink>();


				// Header with better styling
				imgui_TextColored(0.95f, 0.85f, 0.35f, 1.0f, $"Pick {foodDrinkState._cfgFoodDrinkKey} from inventory");
				imgui_Separator();

				// Status and scan button
				if (string.IsNullOrEmpty(foodDrinkState._cfgFoodDrinkStatus))
				{
					if (imgui_Button("Scan Inventory"))
					{
						foodDrinkState._cfgFoodDrinkStatus = "Scanning...";
						foodDrinkState._cfgFoodDrinkScanRequested = true;
					}
					imgui_Text("Click above to scan your inventory.");
				}
				else
				{
					imgui_TextColored(0.7f, 0.9f, 0.7f, 1.0f, foodDrinkState._cfgFoodDrinkStatus);
				}

				imgui_Separator();

				// Results list with better sizing
				if (foodDrinkState._cfgFoodDrinkCandidates.Count > 0)
				{
					imgui_TextColored(0.8f, 0.9f, 1.0f, 1.0f, "Found items (click to select):");

					// Use responsive sizing for the list
					float listHeight = Math.Min(400f, Math.Max(150f, foodDrinkState._cfgFoodDrinkCandidates.Count * 20f + 40f));
					float listWidth = Math.Max(300f, imgui_GetContentRegionAvailX() * 0.9f);

					if (imgui_BeginChild("FoodDrinkList", listWidth, listHeight, 1, 0))
					{
						for (int i = 0; i < foodDrinkState._cfgFoodDrinkCandidates.Count; i++)
						{
							var item = foodDrinkState._cfgFoodDrinkCandidates[i];
							if (imgui_Selectable($"{item}##item_{i}", false))
							{
								// Apply selection
								var pdAct = data.GetActiveCharacterIniData();
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
				else if (!string.IsNullOrEmpty(foodDrinkState._cfgFoodDrinkStatus) && !foodDrinkState._cfgFoodDrinkStatus.Contains("Scanning"))
				{
					imgui_TextColored(0.9f, 0.7f, 0.7f, 1.0f, "No matching items found.");
				}

				imgui_Separator();

				// Action buttons
				if (foodDrinkState._cfgFoodDrinkCandidates.Count > 0)
				{
					if (imgui_Button("Rescan"))
					{
						foodDrinkState._cfgFoodDrinkStatus = "Scanning...";
						foodDrinkState._cfgFoodDrinkCandidates.Clear();
						foodDrinkState._cfgFoodDrinkScanRequested = true;
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

			var pd = data.GetActiveCharacterIniData();
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

			var pd = data.GetActiveCharacterIniData();
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
			string unique = data.GenerateUniqueKey(ifsSection.Keys, baseKey);
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
			var state = _state.GetState<State_SpellInfo>();

			var s = state.Spell;

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
					state.Spell = null;
				}
			}
			imgui_End();
			if (!_state.Show_SpellInfoModal) { state.Spell = null; }
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
		
	}
}
