using E3Core.Processors;
using MonoCore;
using System;
using System.Collections.Generic;
using System.Linq;
using static MonoCore.E3ImGUI;

namespace E3Core.UI.Windows
{
	public static class CommandsWindow
	{
		private const string WindowName = "E3 Commands";
		private const string FilterId = "##cmdFilter";
		private const string ClipperId = "##cmdClipper";

		private static bool _windowInitialized;
		private static bool _imguiContextReady;

		private static List<EventProcessor.CommandListItem> _allCommands = new List<EventProcessor.CommandListItem>();
		private static List<EventProcessor.CommandListItem> _filteredCommands = new List<EventProcessor.CommandListItem>();
		private static long _lastRefreshTick;
		private static readonly long _refreshInterval = 5000; // refresh command list every 5 seconds

		private static int _sortColumn = 0;
		private static int _sortDirection = 0; // 0 = ascending, 1 = descending
		private static string _selectedCommand = null;

		// Tree view state
		private static bool _expandAll = false;
		private static bool _collapseAll = false;

		[SubSystemInit]
		public static void Init()
		{
			if (Core._MQ2MonoVersion < 0.41m) return;

			E3ImGUI.RegisterWindow(WindowName, () =>
			{
				try { RenderWindow(); }
				catch (Exception ex) { E3.MQ.Write($"Commands Window Error: {ex.Message}"); }
			});

			EventProcessor.RegisterCommand("/e3commands", (x) =>
			{
				if (Core._MQ2MonoVersion < 0.41m)
				{
					E3.MQ.Write("E3 Commands window requires MQ2Mono 0.41 or greater.");
					return;
				}
				ToggleWindow();
			}, "Toggle the E3 Commands list window");
		}

		public static void ToggleWindow()
		{
			try
			{
				if (!_windowInitialized)
				{
					_windowInitialized = true;
					imgui_Begin_OpenFlagSet(WindowName, true);
					imgui_TextFilter_Create(FilterId, "");
				}
				else
				{
					bool open = imgui_Begin_OpenFlagGet(WindowName);
					imgui_Begin_OpenFlagSet(WindowName, !open);
				}
				_imguiContextReady = true;
			}
			catch (Exception ex)
			{
				E3.MQ.Write($"Commands Window toggle error: {ex.Message}");
				_imguiContextReady = false;
			}
		}

		private static void RefreshCommandList()
		{
			long now = DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond;
			if (now - _lastRefreshTick < _refreshInterval && _allCommands.Count > 0) return;
			_lastRefreshTick = now;

			_allCommands.Clear();
			foreach (var kvp in EventProcessor.CommandList)
			{
				_allCommands.Add(kvp.Value);
			}
		}

		private static void ApplyFilterAndSort()
		{
			_filteredCommands.Clear();

			bool filterActive = imgui_TextFilter_IsActive(FilterId);

			foreach (var cmd in _allCommands)
			{
				if (filterActive)
				{
					string searchable = $"{cmd.command} {cmd.classOwner} {cmd.description}";
					if (!imgui_TextFilter_PassFilter(FilterId, searchable))
						continue;
				}
				_filteredCommands.Add(cmd);
			}

			// Sort
			Comparison<EventProcessor.CommandListItem> comparison;
			switch (_sortColumn)
			{
				case 0: // command
					comparison = (a, b) => string.Compare(a.command, b.command, StringComparison.OrdinalIgnoreCase);
					break;
				case 1: // classOwner
					comparison = (a, b) =>
					{
						int cmp = string.Compare(a.classOwner, b.classOwner, StringComparison.OrdinalIgnoreCase);
						return cmp != 0 ? cmp : string.Compare(a.command, b.command, StringComparison.OrdinalIgnoreCase);
					};
					break;
				case 2: // description
					comparison = (a, b) =>
					{
						int cmp = string.Compare(a.description ?? "", b.description ?? "", StringComparison.OrdinalIgnoreCase);
						return cmp != 0 ? cmp : string.Compare(a.command, b.command, StringComparison.OrdinalIgnoreCase);
					};
					break;
				default:
					comparison = (a, b) => string.Compare(a.command, b.command, StringComparison.OrdinalIgnoreCase);
					break;
			}

			if (_sortDirection == 0)
				_filteredCommands.Sort(comparison);
			else
				_filteredCommands.Sort((a, b) => comparison(b, a));
		}

		private static void RenderWindow()
		{
			if (!_imguiContextReady) return;
			if (!imgui_Begin_OpenFlagGet(WindowName)) return;

			E3ImGUI.PushCurrentTheme();
			try
			{
				imgui_SetNextWindowSizeWithCond(1000f, 700f, (int)ImGuiCond.FirstUseEver);

				using (var window = ImGUIWindow.Aquire())
				{
					int flags = (int)(ImGuiWindowFlags.ImGuiWindowFlags_NoCollapse);
					if (!window.Begin(WindowName, flags)) return;

					RefreshCommandList();
					ApplyFilterAndSort();

					RenderHeader();
					imgui_Separator();

					using (var tabBar = ImGUITabBar.Aquire())
					{
						if (tabBar.BeginTabBar("##cmdTabs"))
						{
							using (var tab1 = ImGUITabItem.Aquire())
							{
								if (tab1.BeginTabItem("Table View"))
								{
									RenderTableView();
								}
							}
							using (var tab2 = ImGUITabItem.Aquire())
							{
								if (tab2.BeginTabItem("Tree View"))
								{
									RenderTreeView();
								}
							}
						}
					}
				}
			}
			finally
			{
				E3ImGUI.PopCurrentTheme();
			}
		}

		private static void RenderHeader()
		{
			// Search filter
			imgui_Text("Search:");
			imgui_SameLine();
			imgui_SetNextItemWidth(imgui_GetContentRegionAvailX() - 200f);
			imgui_TextFilter_Draw(FilterId, "Filter (inc,-exc)", 0f);

			imgui_SameLine();
			if (imgui_Button("Clear"))
			{
				imgui_TextFilter_Clear(FilterId);
			}

			imgui_SameLine();
			imgui_TextColored(0.6f, 0.8f, 0.6f, 1.0f, $"Showing {_filteredCommands.Count} of {_allCommands.Count}");
		}

		private static void RenderTableView()
		{
			int tableFlags = (int)(
				ImGuiTableFlags.ImGuiTableFlags_RowBg |
				ImGuiTableFlags.ImGuiTableFlags_BordersInner |
				ImGuiTableFlags.ImGuiTableFlags_BordersOuter |
				ImGuiTableFlags.ImGuiTableFlags_Resizable |
				ImGuiTableFlags.ImGuiTableFlags_ScrollY |
				ImGuiTableFlags.ImGuiTableFlags_Sortable |
				ImGuiTableFlags.ImGuiTableFlags_SizingStretchProp);

			float tableHeight = imgui_GetContentRegionAvailY() - 30f;

			using (var table = ImGUITable.Aquire())
			{
				if (table.BeginTable("CommandsTable", 3, tableFlags, 0, tableHeight))
				{
					imgui_TableSetupScrollFreeze(0, 1);

					imgui_TableSetupColumn("Command", (int)(ImGuiTableColumnFlags.ImGuiTableColumnFlags_WidthFixed | ImGuiTableColumnFlags.ImGuiTableColumnFlags_DefaultSort), 180f);
					imgui_TableSetupColumn("Class", (int)(ImGuiTableColumnFlags.ImGuiTableColumnFlags_WidthFixed), 120f);
					imgui_TableSetupColumn("Description", (int)(ImGuiTableColumnFlags.ImGuiTableColumnFlags_WidthStretch | ImGuiTableColumnFlags.ImGuiTableColumnFlags_NoSort), 0f);
					imgui_TableHeadersRow();

					// Handle sorting
					if (imgui_TableGetSortSpecs_HasSpecs())
					{
						int specsCount = imgui_TableGetSortSpecs_GetSpecsCount();
						if (specsCount > 0)
						{
							int newColumn = imgui_TableGetSortSpecs_GetColumnIndex(0);
							int newDirection = imgui_TableGetSortSpecs_GetSortDirection(0) - 1; // ImGui uses 1=Asc, 2=Desc
							if (newColumn != _sortColumn || newDirection != _sortDirection)
							{
								_sortColumn = newColumn;
								_sortDirection = newDirection;
							}
						}
						imgui_TableGetSortSpecs_SetDirty(false);
					}

					// Virtual scrolling with ListClipper
					using (var clipper = ImGUIListClipper.Aquire(ClipperId, _filteredCommands.Count))
					{
						while (clipper.Step())
						{
							for (int row = clipper.DisplayStart; row < clipper.DisplayEnd; row++)
							{
								if (row < 0 || row >= _filteredCommands.Count) continue;
								var cmd = _filteredCommands[row];

								imgui_TableNextRow();
								imgui_TableNextColumn();

								// Command column — selectable for highlighting
								bool isSelected = _selectedCommand == cmd.command;
								if (imgui_Selectable($"{cmd.command}##row{row}", isSelected))
								{
									_selectedCommand = cmd.command;
								}

								// Context menu
								using (var popup = ImGUIPopUpContext.Aquire())
								{
									if (popup.BeginPopupContextItem($"##ctx{row}", 1))
									{
										if (imgui_MenuItem("Copy Command"))
										{
											try
											{
												System.Windows.Forms.Clipboard.SetText(cmd.command);
											}
											catch { }
										}
										if (imgui_MenuItem("Copy with Space"))
										{
											try
											{
												System.Windows.Forms.Clipboard.SetText(cmd.command + " ");
											}
											catch { }
										}
									}
								}

								// Tooltip on hover
								if (imgui_IsItemHovered())
								{
									using (var tooltip = ImGUIToolTip.Aquire())
									{
										imgui_TextColored(0.4f, 0.8f, 1.0f, 1.0f, "Command:");
										imgui_SameLine();
										imgui_Text(cmd.command);
										imgui_TextColored(0.4f, 0.8f, 1.0f, 1.0f, "Class:");
										imgui_SameLine();
										imgui_Text(cmd.classOwner);
										if (!string.IsNullOrWhiteSpace(cmd.description))
										{
											imgui_TextColored(0.4f, 0.8f, 1.0f, 1.0f, "Description:");
											imgui_SameLine();
											imgui_TextWrapped(cmd.description);
										}
										if (!string.IsNullOrWhiteSpace(cmd.methodCaller))
										{
											imgui_TextColored(0.4f, 0.8f, 1.0f, 1.0f, "Method:");
											imgui_SameLine();
											imgui_Text(cmd.methodCaller);
										}
									}
								}

								imgui_TableNextColumn();

								// Class column — colored
								uint classColor = GetClassColor(cmd.classOwner);
								imgui_TextColored(
									(float)((classColor >> 0) & 0xFF) / 255f,
									(float)((classColor >> 8) & 0xFF) / 255f,
									(float)((classColor >> 16) & 0xFF) / 255f,
									1.0f,
									cmd.classOwner);

								imgui_TableNextColumn();

								// Description column
								if (string.IsNullOrWhiteSpace(cmd.description))
								{
									imgui_TextDisabled("--");
								}
								else
								{
									imgui_Text(cmd.description);
								}
							}
						}
					}
				}
			}

			// Footer with selected command details
			imgui_Separator();
			if (!string.IsNullOrEmpty(_selectedCommand))
			{
				var selected = _allCommands.FirstOrDefault(c => c.command == _selectedCommand);
				if (selected != null)
				{
					imgui_TextColored(0.4f, 0.8f, 1.0f, 1.0f, "Selected:");
					imgui_SameLine();
					imgui_Text($"{selected.command}  ({selected.classOwner})");
					if (!string.IsNullOrWhiteSpace(selected.description))
					{
						imgui_SameLine();
						imgui_TextDisabled($"- {selected.description}");
					}
				}
			}
			else
			{
				imgui_TextDisabled("Select a command to see details");
			}
		}

		private static void RenderTreeView()
		{
			// Expand/Collapse all buttons
			if (imgui_Button("Expand All"))
			{
				_expandAll = true;
			}
			imgui_SameLine();
			if (imgui_Button("Collapse All"))
			{
				_collapseAll = true;
			}
			imgui_Separator();

			// Group commands by classOwner
			var grouped = new SortedDictionary<string, List<EventProcessor.CommandListItem>>(StringComparer.OrdinalIgnoreCase);
			foreach (var cmd in _filteredCommands)
			{
				string owner = cmd.classOwner ?? "Unknown";
				if (!grouped.ContainsKey(owner))
					grouped[owner] = new List<EventProcessor.CommandListItem>();
				grouped[owner].Add(cmd);
			}

			float treeHeight = imgui_GetContentRegionAvailY() - 10f;
			using (var child = ImGUIChild.Aquire())
			{
				if (child.BeginChild("##treeScroll", 0, treeHeight, 0, 0))
				{
					foreach (var group in grouped)
					{
						string label = $"{group.Key} ({group.Value.Count})";

						// Handle expand/collapse all
						if (_expandAll)
							imgui_SetNextItemOpen(true, (int)ImGuiCond.Always);
						if (_collapseAll)
							imgui_SetNextItemOpen(false, (int)ImGuiCond.Always);

						uint classColor = GetClassColor(group.Key);
						bool headerOpen = imgui_CollapsingHeader($"{label}##class_{group.Key}", 0);

						if (headerOpen)
						{
							// Sort commands within each group
							group.Value.Sort((a, b) => string.Compare(a.command, b.command, StringComparison.OrdinalIgnoreCase));

							for (int i = 0; i < group.Value.Count; i++)
							{
								var cmd = group.Value[i];
								imgui_PushID(i);

								// Command name as tree node
								bool nodeOpen = imgui_TreeNode($"{cmd.command}##cmd{i}");

								// Tooltip on the tree node
								if (imgui_IsItemHovered())
								{
									using (var tooltip = ImGUIToolTip.Aquire())
									{
										if (!string.IsNullOrWhiteSpace(cmd.description))
										{
											imgui_TextWrapped(cmd.description);
										}
										else
										{
											imgui_TextDisabled("No description available");
										}
										if (!string.IsNullOrWhiteSpace(cmd.methodCaller))
										{
											imgui_TextColored(0.5f, 0.5f, 0.5f, 1.0f, $"Registered by: {cmd.methodCaller}");
										}
									}
								}

								// Context menu
								using (var popup = ImGUIPopUpContext.Aquire())
								{
									if (popup.BeginPopupContextItem($"##treectx{i}", 1))
									{
										if (imgui_MenuItem("Copy Command"))
										{
											try
											{
												System.Windows.Forms.Clipboard.SetText(cmd.command);
											}
											catch { }
										}
									}
								}

								if (nodeOpen)
								{
									// Show details inside the tree node
									imgui_TextColored(0.4f, 0.8f, 1.0f, 1.0f, "Command:");
									imgui_SameLine();
									imgui_Text(cmd.command);

									imgui_TextColored(0.4f, 0.8f, 1.0f, 1.0f, "Class:");
									imgui_SameLine();
									imgui_Text(cmd.classOwner);

									if (!string.IsNullOrWhiteSpace(cmd.description))
									{
										imgui_TextColored(0.4f, 0.8f, 1.0f, 1.0f, "Description:");
										imgui_SameLine();
										imgui_TextWrapped(cmd.description);
									}

									if (!string.IsNullOrWhiteSpace(cmd.methodCaller))
									{
										imgui_TextColored(0.4f, 0.8f, 1.0f, 1.0f, "Method:");
										imgui_SameLine();
										imgui_Text(cmd.methodCaller);
									}

									imgui_TreePop();
								}

								imgui_PopID();
							}
						}
					}
				}

				// Reset expand/collapse flags after processing
				_expandAll = false;
				_collapseAll = false;
			}
		}

		private static uint GetClassColor(string classOwner)
		{
			// Generate a consistent color based on the class name hash
			if (string.IsNullOrEmpty(classOwner)) return 0xFF808080;

			int hash = classOwner.GetHashCode();
			byte r = (byte)(80 + ((hash >> 0) & 0x7F));
			byte g = (byte)(80 + ((hash >> 8) & 0x7F));
			byte b = (byte)(80 + ((hash >> 16) & 0x7F));
			return (uint)(r | (g << 8) | (b << 16) | (0xFF << 24));
		}
	}
}
