using MonoCore;
using System;
using System.Collections.Generic;
using System.Linq;
using static MonoCore.E3ImGUI;
using E3Core.Classes;
using E3Core.Processors;

namespace E3Core.UI.Windows
{
    public static class MemoryStatsWindow
    {
        private static bool _windowInitialized = false;
        private static bool _imguiContextReady = false;
        private static List<MemoryStats> _memoryStats = new List<MemoryStats>();
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
            if (Core._MQ2MonoVersion < 0.35m) return;
            E3ImGUI.RegisterWindow("Memory Stats", RenderWindow);
        }

        public static void ToggleWindow()
        {
            try
            {
                bool shouldRefresh = false;
                if (!_windowInitialized)
                {
                    _windowInitialized = true;
                    imgui_Begin_OpenFlagSet("Memory Stats", true);
                    shouldRefresh = true;
                }
                else
                {
                    bool open = imgui_Begin_OpenFlagGet("Memory Stats");
                    bool newState = !open;
                    imgui_Begin_OpenFlagSet("Memory Stats", newState);
                    shouldRefresh = newState;
                }
                _imguiContextReady = true;

                if (shouldRefresh)
                {
                    EventProcessor.ProcessMQCommand("/e3memstats");
                }
            }
            catch (Exception ex)
            {
                E3.Log.Write($"Memory Stats Window error: {ex.Message}", Logging.LogLevels.Error);
                _imguiContextReady = false;
            }
        }

        public static void AddMemoryStats(MemoryStats stats)
        {
            lock (_memoryStats)
            {
                // Remove existing entry for this character if it exists
                _memoryStats.RemoveAll(x => x.CharacterName == stats.CharacterName);
                // Add the new stats
                _memoryStats.Add(stats);
                // Keep only the last 50 entries to prevent memory buildup
                if (_memoryStats.Count > 50)
                {
                    _memoryStats = _memoryStats.OrderByDescending(x => x.Timestamp).Take(50).ToList();
                }
            }
        }

        public static void ClearStats()
        {
            lock (_memoryStats)
            {
                _memoryStats.Clear();
            }
        }

        private static void RenderWindow()
        {
            if (!_imguiContextReady) return;
            if (!imgui_Begin_OpenFlagGet("Memory Stats")) return;

            imgui_SetNextWindowSizeWithCond(600, 400, (int)ImGuiCond.FirstUseEver);
            E3ImGUI.PushCurrentTheme();
            try
            {
                using (var window = ImGUIWindow.Aquire())
                {
                    if (!window.Begin("Memory Stats", (int)ImGuiWindowFlags.ImGuiWindowFlags_NoCollapse))
                        return;

                    // Header with refresh button
                    imgui_Text("E3 Memory Statistics");
                    imgui_SameLine();
                    if (imgui_Button("Clear"))
                    {
                        ClearStats();
                    }
                    imgui_SameLine();
                    if (imgui_Button("Refresh"))
                    {
                        // Trigger a refresh by calling memstats command (only main bot will request from others)
                        EventProcessor.ProcessMQCommand("/e3memstats");
                    }

                    imgui_Separator();

                    // Memory Stats Table
                    using (var table = ImGUITable.Aquire())
                    {
                        int tableFlags = (int)(ImGuiTableFlags.ImGuiTableFlags_RowBg | 
                                              ImGuiTableFlags.ImGuiTableFlags_BordersOuter | 
                                              ImGuiTableFlags.ImGuiTableFlags_BordersInner | 
                                              ImGuiTableFlags.ImGuiTableFlags_ScrollY);

                        const float summaryLegendHeight = 190f; // Enough room for summary metrics plus multi-line legend
                        float tableHeight = Math.Max(150f, imgui_GetContentRegionAvailY() - summaryLegendHeight);

                        if (table.BeginTable("MemoryStatsTable", 4, tableFlags, 0f, tableHeight))
                        {
                            imgui_TableSetupColumn("Character", (int)ImGuiTableColumnFlags.ImGuiTableColumnFlags_WidthStretch, 150);
                            imgui_TableSetupColumn("C# Memory (MB)", (int)ImGuiTableColumnFlags.ImGuiTableColumnFlags_WidthFixed, 120);
                            imgui_TableSetupColumn("EQ Commit (MB)", (int)ImGuiTableColumnFlags.ImGuiTableColumnFlags_WidthFixed, 120);
                            imgui_TableSetupColumn("Last Updated", (int)ImGuiTableColumnFlags.ImGuiTableColumnFlags_WidthStretch, 150);
                            imgui_TableHeadersRow();

                            List<MemoryStats> currentStats;
                            lock (_memoryStats)
                            {
                                currentStats = _memoryStats.OrderByDescending(x => x.Timestamp).ToList();
                            }

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
                                imgui_Text(stats.Timestamp.ToString("HH:mm:ss"));
                            }
                        }
                    }

                    // Summary at the bottom
                    imgui_Separator();
                    List<MemoryStats> summaryStats;
                    lock (_memoryStats)
                    {
                        summaryStats = _memoryStats.ToList();
                    }

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
    }
}
