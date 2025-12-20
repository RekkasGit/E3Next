using MonoCore;
using System;
using System.Collections.Generic;
using System.Linq;
using static MonoCore.E3ImGUI;
using E3Core.Classes;
using E3Core.Processors;
using E3Core.Utility;

namespace E3Core.UI.Windows.MemStats
{
	public static class MemoryStatsWindow
	{
		private static bool _windowInitialized = false;
		private static bool _imguiContextReady = false;
		private static Int64 _lastUpdate = 0;
		private static Int64 _lastUpdateInterval = 1000;
		private static List<MemoryStats> _memoryStats = new List<MemoryStats>();

		private static string _WindowName = "E3 Memory Stats";
		// Severity legend doubles as the palette we reuse for each EQ commit range.
		private static readonly (double MinGb, double MaxGb, float R, float G, float B, string Label)[] _eqCommitSeverityBands = new[]
		{
			(0.0, 0.8, 0.6f, 0.9f, 0.6f, "<0.8 GB = Plenty of headroom"),
			(0.8, 1.2, 0.25f, 0.85f, 0.25f, "0.8-1.2 GB = Rock solid"),
			(1.2, 1.3, 0.95f, 0.85f, 0.35f, "1.2-1.3 GB = Mostly stable"),
			(1.3, 1.4, 1.0f, 0.7f, 0.2f, "1.3-1.4 GB = Possible crash"),
            // Treat anything past 1.4 GB up to the 1.6 GB wall as "soon to crash" (matches the user's warning).
            (1.4, 1.6, 1.0f, 0.35f, 0.2f, "1.4-1.5 GB = Soon to crash (large zones)"),
			(1.6, double.MaxValue, 1.0f, 0.05f, 0.05f, "1.6+ GB = Crash very likely")
		};


		[SubSystemInit]
		public static void Init()
		{
			if (Core._MQ2MonoVersion < 0.36m) return;
			E3ImGUI.RegisterWindow(_WindowName, RenderWindow);

			EventProcessor.RegisterCommand("/e3memstats", (x) =>
			{
				MemoryStatsWindow.ToggleWindow();
			}, "toggle memory stats window");

			EventProcessor.RegisterCommand("/e3debug_memory_collect", (x) =>
			{

				if(x.args.Count>0)
				{
					int generation = 0;
					Int32.TryParse(x.args[0], out generation);

					if (generation < 0)
					{
						generation = 0;
					}
					else if (generation > 2)
					{
						generation = 2;
					}
					E3.Bots.Broadcast($"Collecting C# Memory ({generation})");
					GC.Collect(generation, GCCollectionMode.Forced, false);
				}
				else
				{
					GC.GetTotalMemory(true);
					E3.Bots.Broadcast("Collecting C# Memory (All)");
				}

				
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
				E3.Log.Write($"Memory Stats Window error: {ex.Message}", Logging.LogLevels.Error);
				_imguiContextReady = false;
			}
		}

		private static void CheckRefresh()
		{
			if (!e3util.ShouldCheck(ref _lastUpdate, _lastUpdateInterval)) return;
			_memoryStats.Clear();
			//get the connected bots.
			List<string> users = E3.Bots.BotsConnected().ToList(); //make a copy as this returns a direct copy of cache
			users.Sort();
			foreach (var user in users)
			{
				Double csharpMemory = 0;
				Double eqPageMemory = 0;

				string startTime = E3.Bots.Query(user, "${Me.Memory_CSharpStartTime}");

				E3.Bots.GetMemoryUsage(user, out csharpMemory, out eqPageMemory);
				var memoryStat = new MemoryStats(user, csharpMemory, eqPageMemory);

				if (DateTime.TryParse(startTime, out var result))
				{
					memoryStat.TimeRunning = (System.DateTime.Now - result).TotalHours.ToString("N2");

				}



				_memoryStats.Add(memoryStat);
			}
		}
		private static void RenderWindow()
		{
			if (!_imguiContextReady) return;
			if (!imgui_Begin_OpenFlagGet(_WindowName)) return;
			CheckRefresh();
			imgui_SetNextWindowSizeWithCond(600, 400, (int)ImGuiCond.FirstUseEver);
			E3ImGUI.PushCurrentTheme();
			try
			{
				using (var window = ImGUIWindow.Aquire())
				{
					if (!window.Begin(_WindowName, (int)ImGuiWindowFlags.ImGuiWindowFlags_NoCollapse))
						return;

					// Header with refresh button
					imgui_Text("E3 Memory Statistics by Rekka/Linamas");
					imgui_Separator();

					// Memory Stats Table
					using (var table = ImGUITable.Aquire())
					{
						int tableFlags = (int)(ImGuiTableFlags.ImGuiTableFlags_RowBg |
											  ImGuiTableFlags.ImGuiTableFlags_BordersOuter |
											  ImGuiTableFlags.ImGuiTableFlags_BordersInner |
											  ImGuiTableFlags.ImGuiTableFlags_ScrollY| ImGuiTableFlags.ImGuiTableFlags_Resizable);

						const float summaryLegendHeight = 190f; // Enough room for summary metrics plus multi-line legend
						float tableHeight = Math.Max(150f, imgui_GetContentRegionAvailY() - summaryLegendHeight);

						if (table.BeginTable("MemoryStatsTable", 4, tableFlags, 0f, tableHeight))
						{
							imgui_TableSetupColumn("Character", (int)ImGuiTableColumnFlags.ImGuiTableColumnFlags_WidthStretch, 150);
							imgui_TableSetupColumn("C# Memory (MB)", (int)ImGuiTableColumnFlags.ImGuiTableColumnFlags_WidthFixed, 120);
							imgui_TableSetupColumn("EQ Commit (MB)", (int)ImGuiTableColumnFlags.ImGuiTableColumnFlags_WidthFixed, 120);
							imgui_TableSetupColumn("Hours Running", (int)ImGuiTableColumnFlags.ImGuiTableColumnFlags_WidthStretch, 150);
							imgui_TableHeadersRow();

							List<MemoryStats> currentStats = _memoryStats;
						
							foreach (var stats in currentStats)
							{
								imgui_TableNextRow();

								imgui_TableNextColumn();
								imgui_Text(stats.CharacterName);

								imgui_TableNextColumn();
								imgui_Text(stats.CSharpMemoryMB.ToString("N2"));

								imgui_TableNextColumn();
								DrawEqCommitValue(stats.EQCommitSizeMB);

								imgui_TableNextColumn();
								imgui_Text(stats.TimeRunning);

								
							}
						}
					}
					// Summary at the bottom
					imgui_Separator();
					List<MemoryStats> summaryStats= _memoryStats;
				
					if (summaryStats.Count > 0)
					{
						double totalCSharp = summaryStats.Sum(x => x.CSharpMemoryMB);
						double totalEQ = summaryStats.Sum(x => x.EQCommitSizeMB);

						imgui_Text($"Total Characters: {summaryStats.Count}");
						imgui_SameLine();
						imgui_Text($"Total C# Memory: {totalCSharp:N2} MB");
						imgui_SameLine();
						imgui_Text($"Total EQ Commit: {totalEQ:N2} MB");
					}
					else
					{
						imgui_Text("No memory statistics available. Use /e3memstats to collect data.");
					}

					imgui_Separator();
					RenderSeverityLegend();
				}
			}
			finally
			{
				E3ImGUI.PopCurrentTheme();
			}
		}

		private static void DrawEqCommitValue(double eqCommitMb)
		{
			var (r, g, b) = GetEqCommitSeverityColor(eqCommitMb);
			imgui_TextColored(r, g, b, 1.0f, eqCommitMb.ToString("N2"));
		}

		private static (float r, float g, float b) GetEqCommitSeverityColor(double eqCommitMb)
		{
			double eqCommitGb = eqCommitMb / 1024d;
			foreach (var band in _eqCommitSeverityBands)
			{
				if (eqCommitGb >= band.MinGb && eqCommitGb < band.MaxGb)
				{
					return (band.R, band.G, band.B);
				}
			}

			return (0.9f, 0.9f, 0.9f);
		}

		private static void RenderSeverityLegend()
		{
			imgui_Text("EQ Commit severity legend:");
			foreach (var band in _eqCommitSeverityBands)
			{
				imgui_TextColored(band.R, band.G, band.B, 1.0f, $"  {band.Label}");
			}
		}
		public class MemoryStats
		{
			public string CharacterName { get; set; } = string.Empty;
			public double CSharpMemoryMB { get; set; }
			public double EQCommitSizeMB { get; set; }
			public string TimeRunning { get; set; } = string.Empty;

			public MemoryStats()
			{
			}

			public MemoryStats(string characterName, double cSharpMemoryMB, double eqCommitSizeMB)
			{
				CharacterName = characterName;
				CSharpMemoryMB = cSharpMemoryMB;
				EQCommitSizeMB = eqCommitSizeMB;
			}
		}
	}
}
