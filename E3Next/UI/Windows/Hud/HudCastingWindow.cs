using E3Core.Processors;
using E3Core.Utility;
using MonoCore;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static MonoCore.E3ImGUI;

namespace E3Core.UI.Windows.Hud
{
	public static class HudCastingWindow
	{
		private static bool _windowInitialized = false;
		private static bool _imguiContextReady = false;
		private static Int64 _lastUpdate = 0;
		private static Int64 _lastUpdateInterval = 1000;
		private static List<TableRow> _tableRows = new List<TableRow>();
		private static IMQ MQ = E3.MQ;
		private static ISpawns _spawns = E3.Spawns;
		private static string _WindowName = "E3 Casting Hud";
	
		[SubSystemInit]
		public static void Init()
		{
			if (Core._MQ2MonoVersion < 0.36m) return;
			E3ImGUI.RegisterWindow(_WindowName, RenderBotCastingWindow);

			EventProcessor.RegisterCommand("/e3hud_casting", (x) =>
			{
				if(Core._MQ2MonoVersion<0.36m)
				{
					MQ.Write("This requires MQ2Mono 0.36 or greater");
					return;
				}	

				HudCastingWindow.ToggleWindow();
			}, "toggle memory stats window");
		}
		public static void ToggleWindow()
		{
			try
			{
				if (!_windowInitialized)
				{
					_windowInitialized = true;
					imgui_Begin_OpenFlagSet(_WindowName, true);
				}
				else
				{
					bool open = imgui_Begin_OpenFlagGet(_WindowName);
					bool newState = !open;
					imgui_Begin_OpenFlagSet(_WindowName, newState);
				}
				_imguiContextReady = true;
			}
			catch (Exception ex)
			{
				E3.Log.Write($"Hud Casting Window error: {ex.Message}", Logging.LogLevels.Error);
				_imguiContextReady = false;
			}
		}

		private static void CheckRefresh()
		{
			if (!e3util.ShouldCheck(ref _lastUpdate, _lastUpdateInterval)) return;
			_tableRows.Clear();
			//get the connected bots.
			List<string> users = E3.Bots.BotsConnected().ToList(); //make a copy as this returns a direct copy of cache
			users.Sort();
			foreach (var user in users)
			{
				string casting = E3.Bots.Query(user, "${Me.Casting}");
				string targetidString = E3.Bots.Query(user, "${Me.CurrentTargetID}");
				Int32 targetid;
				Int32.TryParse(targetidString, out targetid);
				string targetName = "none";
				if(targetid >0 && _spawns.TryByID(targetid, out var s,false))
				{
					targetName = s.Name;
				}
				var row = new TableRow(user, targetName,casting);
				if(row.Name==E3.CurrentName) row.IsSelf = true;
				_tableRows.Add(row);
			}
		}
		private static void RenderBotCastingWindow()
		{
			if (!_imguiContextReady) return;
			if (!imgui_Begin_OpenFlagGet(_WindowName)) return;

			PushCurrentTheme();
			try
			{
				CheckRefresh();
				using (var window = ImGUIWindow.Aquire())
				{
					imgui_SetNextWindowSizeWithCond(360f, 320f, (int)ImGuiCond.FirstUseEver);
					int flags = (int)(ImGuiWindowFlags.ImGuiWindowFlags_NoCollapse
						| ImGuiWindowFlags.ImGuiWindowFlags_NoBackground);
					
					if (window.Begin(_WindowName, flags))
					{
						var entries = _tableRows;
						if (entries.Count == 0)
						{
							imgui_TextColored(0.7f, 0.7f, 0.7f, 1.0f, "No connected bots detected.");
							return;
						}

						//imgui_TextColored(0.8f, 0.85f, 1.0f, 1.0f, $"Connected: {entries.Count}");
						//imgui_Separator();
						RenderBotCastingGrid(entries);
					}
				}
			}
			finally
			{
				PopCurrentTheme();
			}
		}

		private static void RenderBotCastingGrid(IReadOnlyList<TableRow> entries)
		{
			int rowsPerColumn =2;

			if (entries.Count == 6) rowsPerColumn = 2;
			if (entries.Count == 3) rowsPerColumn = 1;
			if (entries.Count >6) rowsPerColumn = 6;

			int columnCount = Math.Max(1, (int)Math.Ceiling(entries.Count / (double)rowsPerColumn));
			int tableFlags = (int)(ImGuiTableFlags.ImGuiTableFlags_BordersInnerV
				| ImGuiTableFlags.ImGuiTableFlags_BordersOuter
				| ImGuiTableFlags.ImGuiTableFlags_Resizable);

			using (var table = ImGUITable.Aquire())
			{
				if (table.BeginTable("E3HudBotCasting", columnCount, tableFlags, 0, 0))
				{
					for (int col = 0; col < columnCount; col++)
					{
						imgui_TableSetupColumn($"CastingCol{col}", (int)ImGuiTableColumnFlags.ImGuiTableColumnFlags_None, 170f);
					}

					for (int row = 0; row < rowsPerColumn; row++)
					{
						bool rowHasData = false;
						for (int col = 0; col < columnCount; col++)
						{
							int index = (col * rowsPerColumn) + row;
							if (index < entries.Count)
							{
								rowHasData = true;
								break;
							}
						}

						if (!rowHasData)
							break;

						imgui_TableNextRow();
						for (int col = 0; col < columnCount; col++)
						{
							imgui_TableSetColumnIndex(col);
							int index = (col * rowsPerColumn) + row;
							if (index >= entries.Count)
							{
								imgui_Text(" ");
								continue;
							}

							RenderBotCastingCell(entries[index]);
						}
					}
				}
			}
		}

		private static void RenderBotCastingCell(TableRow entry)
		{
			if (entry == null)
			{
				imgui_Text(" ");
				return;
			}

			if (entry.IsSelf)
				imgui_TextColored(0.169f, 1f, 0f, 1f, entry.Name);
			else
				imgui_TextColored(0.85f, 0.75f, 1.0f, 1.0f, entry.Name);

			string targetDisplay = string.IsNullOrWhiteSpace(entry.TargetName)
				? "None"
				: entry.TargetName;
		
			imgui_TextColored(0.65f, 0.85f, 1.0f, 1.0f, "Target:");
			imgui_SameLine();
			imgui_TextColored(0.536f, 1f, 0.333f, 1f, targetDisplay);

			imgui_TextColored(0.65f, 0.85f, 1.0f, 1.0f, "Spell:");
			imgui_SameLine();
			string stateText;
			float sr = 0.6f, sg = 0.6f, sb = 0.6f;
			if (string.IsNullOrWhiteSpace(entry.SpellName) || entry.SpellName=="NULL")
			{
				stateText = "Idle";
				sr = 0.65f; sg = 0.85f; sb = 0.95f;
			}
			else
			{
				stateText = $"{entry.SpellName}";
				sr = 0.95f; sg = 0.9f; sb = 0.55f;
			}
			imgui_TextColored(sr, sg, sb, 1.0f, stateText);
		}


		public class TableRow
		{
			public string Name { get; set; }
			public string TargetName { get; set; }
			public string SpellName { get; set; }
			public bool IsSelf { get; set; }
			public TableRow()
			{
				
			}

			public TableRow(string characterName, string target, string spell)
			{
				Name = characterName;
				TargetName = target;
				SpellName = spell;
			}
		}
	}
}
