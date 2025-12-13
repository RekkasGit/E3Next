using MonoCore;
using System;
using System.Collections.Generic;
using System.Globalization;
using static MonoCore.E3ImGUI;
using E3Core.Processors;
using HuntProcessor = E3Core.Processors.Hunt;

namespace E3Core.UI.Windows.Hunt
{
    public static class HuntWindow
    {
        private const string WindowName = "E3 Hunt";
        private const string FloatWindowName = "E3 Hunt Float";
        private const string DebugWindowName = "E3 Hunt Debug";

        private static bool _imguiContextReady;
        private static bool _windowInitialized;
        private static bool _windowMinimized = true;

        private static string _radiusBuffer = string.Empty;
        private static string _zRadiusBuffer = string.Empty;
        private static string _pullFilterBuffer = string.Empty;
        private static string _ignoreFilterBuffer = string.Empty;
        private static string _candidateFilterBuffer = string.Empty;
        private static string _huntPullMethod = string.Empty;
        private static string _huntPullSpell = string.Empty;
        private static string _huntPullItem = string.Empty;
        private static string _huntPullAA = string.Empty;
        private static string _huntPullDisc = string.Empty;
        private static string _rangedApproachFactorBuf = string.Empty;

        private static List<(int id, string name, int level, double distance, double path, string loc, string con)> _candidateSnapshot = new List<(int, string, int, double, double, string, string)>();
        private static List<string> _ignoreSnapshot = new List<string>();
        private static Dictionary<string, List<string>> _ignoreByZone = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        private static string _currentZone = "Unknown";
        private static long _nextUiRefreshAt;

        private static List<(long ts, string msg)> _debugSnapshot = new List<(long, string)>();
        private static long _nextDebugRefreshAt;

        [SubSystemInit]
        public static void Init()
        {
            if (Core._MQ2MonoVersion < 0.35m) return;
            E3ImGUI.RegisterWindow(WindowName, RenderMainWindow);
            E3ImGUI.RegisterWindow(DebugWindowName, RenderDebugWindow);
        }

        public static void ToggleWindow()
        {
            try
            {
                if (!_windowInitialized)
                {
                    _windowInitialized = true;
                    imgui_Begin_OpenFlagSet(WindowName, true);
                    SyncBuffersFromState();
                }
                else
                {
                    bool open = imgui_Begin_OpenFlagGet(WindowName);
                    imgui_Begin_OpenFlagSet(WindowName, !open);
                    if (!open)
                    {
                        SyncBuffersFromState();
                    }
                }
                _imguiContextReady = true;
            }
            catch (Exception ex)
            {
                E3.Log.Write($"Hunt UI error: {ex.Message}", Logging.LogLevels.Error);
                _imguiContextReady = false;
            }
        }

        public static void ToggleMinimized()
        {
            _windowMinimized = !_windowMinimized;
        }

        public static void ToggleDebugWindow()
        {
            try
            {
                bool open = imgui_Begin_OpenFlagGet(DebugWindowName);
                imgui_Begin_OpenFlagSet(DebugWindowName, !open);
                _imguiContextReady = true;
            }
            catch (Exception ex)
            {
                E3.Log.Write($"Hunt debug UI error: {ex.Message}", Logging.LogLevels.Error);
                _imguiContextReady = false;
            }
        }

        private static void RenderMainWindow()
        {
            if (!_imguiContextReady) return;
            if (!imgui_Begin_OpenFlagGet(WindowName)) return;

            RefreshUiSnapshots();

            if (_windowMinimized)
            {
                RenderFloatWindow();
                return;
            }

            imgui_SetNextWindowSizeWithCond(520, 420, (int)ImGuiCond.FirstUseEver);
            E3ImGUI.PushCurrentTheme();
            try
            {
                using (var window = ImGUIWindow.Aquire())
                {
                    if (!window.Begin(WindowName, (int)(ImGuiWindowFlags.ImGuiWindowFlags_NoCollapse | ImGuiWindowFlags.ImGuiWindowFlags_NoDocking)))
                    {
                        return;
                    }
                    RenderControls();
                    imgui_Separator();
                    RenderTabs();
                }
            }
            finally
            {
                E3ImGUI.PopCurrentTheme();
            }
        }

        private static void RenderFloatWindow()
        {
            const ImGuiWindowFlags flags = ImGuiWindowFlags.ImGuiWindowFlags_NoDecoration |
                                           ImGuiWindowFlags.ImGuiWindowFlags_AlwaysAutoResize |
                                           ImGuiWindowFlags.ImGuiWindowFlags_NoDocking |
                                           ImGuiWindowFlags.ImGuiWindowFlags_NoNav;
            E3ImGUI.PushCurrentTheme();
            try
            {
                bool open = imgui_Begin(FloatWindowName, (int)flags);
                if (!open)
                {
                    imgui_End();
                    _windowMinimized = false;
                    return;
                }

                var status = HuntProcessor.Status ?? "Idle";
                bool enabled = HuntProcessor.Enabled;
                bool active = HuntProcessor.Go;
                string target = string.IsNullOrEmpty(HuntProcessor.TargetName) ? "None" : HuntProcessor.TargetName;

                imgui_TextColored(0.9f, 0.85f, 0.45f, 1.0f, $"Status: {status}");
                imgui_TextColored(0.8f, 0.8f, 0.9f, 1.0f, $"Target: {target}");
                imgui_TextColored(0.8f, 0.8f, 0.8f, 1.0f, $"Range: {HuntProcessor.Radius} | State: {HuntStateMachine.CurrentState}");

                imgui_Separator();

                imgui_PushStyleColor(21, enabled ? 0.2f : 0.6f, enabled ? 0.8f : 0.3f, enabled ? 0.2f : 0.3f, 1.0f);
                if (imgui_ButtonEx(enabled ? "On" : "Off", 60, 26))
                {
                    HuntProcessor.Enabled = !enabled;
                }
                imgui_PopStyleColor(1);

                imgui_SameLine();
                imgui_PushStyleColor(21, active ? 0.1f : 0.7f, active ? 0.7f : 0.4f, active ? 0.1f : 0.1f, 1.0f);
                if (imgui_ButtonEx(active ? "Go" : "Pause", 70, 26))
                {
                    HuntProcessor.Go = !active;
                }
                imgui_PopStyleColor(1);

                imgui_SameLine();
                if (imgui_ButtonEx("Ignore", 70, 26))
                {
                    HuntProcessor.IgnoreCurrentTarget();
                }

                imgui_SameLine();
                if (imgui_ButtonEx("Expand", 70, 26))
                {
                    _windowMinimized = false;
                }

                imgui_End();
            }
            finally
            {
                E3ImGUI.PopCurrentTheme();
            }
        }

        private static void RenderStatusHeader()
        {
            var state = HuntStateMachine.CurrentState;
            var reason = HuntStateMachine.StateReason ?? string.Empty;
            var status = HuntProcessor.Status ?? string.Empty;
            var target = string.IsNullOrEmpty(HuntProcessor.TargetName) ? "None" : HuntProcessor.TargetName;

            if (imgui_BeginTable("##hunt_status", 2, 0, 0f, 0f))
            {
                imgui_TableSetupColumn("label", (int)ImGuiTableColumnFlags.ImGuiTableColumnFlags_WidthFixed, 120f);
                imgui_TableSetupColumn("value", (int)ImGuiTableColumnFlags.ImGuiTableColumnFlags_WidthStretch, 0f);

                DrawStatusRow("State", state.ToString());
                DrawStatusRow("Reason", reason);
                DrawStatusRow("Status", status);
                DrawStatusRow("Target", target);
                DrawStatusRow("SmartLoot", $"{HuntProcessor.SmartLootState} ({HuntProcessor.SmartLootMode})");
                DrawStatusRow("Nav Owner", HuntStateMachine.IsNavigationOwned ? "Yes" : "No");
                DrawStatusRow("Next Scan", FormatMs(HuntProcessor.GetMsUntilNextScan()));
                DrawStatusRow("Next Nav", FormatMs(HuntProcessor.GetMsUntilNextNav()));
                DrawStatusRow("Next Pull", FormatMs(HuntProcessor.GetMsUntilNextPull()));

                imgui_EndTable();
            }
        }

        private static void DrawStatusRow(string label, string value)
        {
            imgui_TableNextRow();
            imgui_TableNextColumn();
            imgui_TextColored(0.7f, 0.9f, 1.0f, 1.0f, label + ":");
            imgui_TableNextColumn();
            imgui_Text(value ?? string.Empty);
        }

        private static void RenderControls()
        {
            bool enabled = HuntProcessor.Enabled;
            bool go = HuntProcessor.Go;

            if (imgui_Button(enabled ? "Disable" : "Enable"))
            {
                HuntProcessor.Enabled = !enabled;
                if (!HuntProcessor.Enabled)
                {
                    HuntProcessor.Go = false;
                }
            }
            imgui_SameLine();
            if (imgui_Button(go ? "Pause" : "Go"))
            {
                HuntProcessor.Go = !go;
            }
            imgui_SameLine();
            if (imgui_Button(_windowMinimized ? "Expand" : "Minimize"))
            {
                ToggleMinimized();
            }
            imgui_SameLine();
            if (imgui_Button("Debug"))
            {
                ToggleDebugWindow();
            }

            imgui_SameLine();
            if (imgui_Button("Ignore Current"))
            {
                HuntProcessor.IgnoreCurrentTarget();
            }
            imgui_SameLine();
            if (imgui_Button("Clear Temp Ignores"))
            {
                HuntProcessor.ClearTempIgnores();
            }

            imgui_Text("Camp");
            imgui_SameLine();
            if (imgui_Button(HuntProcessor.CampOn ? "Clear" : "Set"))
            {
                if (HuntProcessor.CampOn)
                {
                    HuntProcessor.CampOn = false;
                }
                else
                {
                    HuntProcessor.CampOn = true;
                    HuntProcessor.CampX = E3.MQ.Query<int>("${Me.X}");
                    HuntProcessor.CampY = E3.MQ.Query<int>("${Me.Y}");
                    HuntProcessor.CampZ = E3.MQ.Query<int>("${Me.Z}");
                }
            }
            imgui_SameLine();
            bool huntFromPlayer = HuntProcessor.HuntFromPlayer;
            if (imgui_Checkbox("From Player", huntFromPlayer))
            {
                HuntProcessor.HuntFromPlayer = imgui_Checkbox_Get("From Player");
            }
            imgui_SameLine();
            bool raidCheck = HuntProcessor.RaidCorpseCheckEnabled;
            if (imgui_Checkbox("Raid Corpse Check", raidCheck))
            {
                HuntProcessor.RaidCorpseCheckEnabled = imgui_Checkbox_Get("Raid Corpse Check");
            }

            RenderNumberInput("Radius", ref _radiusBuffer, value => HuntProcessor.Radius = Math.Max(10, value));
            RenderNumberInput("Z Radius", ref _zRadiusBuffer, value => HuntProcessor.ZRadius = Math.Max(10, value));

            RenderTextInput("Pull Filters", ref _pullFilterBuffer, v => HuntProcessor.PullFilters = v);
            RenderTextInput("Ignore Filters", ref _ignoreFilterBuffer, v => HuntProcessor.IgnoreFilters = v);
        }

        private static void RenderNumberInput(string label, ref string buffer, Action<int> apply)
        {
            var inputId = $"##hunt_{label.Replace(" ", "_").ToLowerInvariant()}";
            if (imgui_InputText(inputId, buffer ?? string.Empty))
            {
                buffer = imgui_InputText_Get(inputId) ?? string.Empty;
            }
            imgui_SameLine();
            if (imgui_Button($"Apply {label}"))
            {
                if (int.TryParse(buffer, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value))
                {
                    apply(Math.Max(1, value));
                }
            }
            imgui_SameLine();
            imgui_Text(label);
        }

        private static void RenderTextInput(string label, ref string buffer, Action<string> apply)
        {
            var inputId = $"##hunt_{label.Replace(" ", "_").ToLowerInvariant()}";
            if (imgui_InputTextMultiline(inputId, buffer ?? string.Empty, 380, 55))
            {
                buffer = imgui_InputText_Get(inputId) ?? string.Empty;
            }
            if (imgui_Button($"Apply {label}"))
            {
                apply(buffer ?? string.Empty);
            }
        }

        private static void RenderTabs()
        {
            using (var tabs = ImGUITabBar.Aquire())
            {
                if (!tabs.BeginTabBar("##hunt_tabs")) return;

                using (var tab = ImGUITabItem.Aquire())
                {
                    if (tab.BeginTabItem("Pull Config"))
                    {
                        RenderPullConfigTab();
                    }
                }
                using (var tab = ImGUITabItem.Aquire())
                {
                    if (tab.BeginTabItem("Targets"))
                    {
                        RenderTargetsTab();
                    }
                }
                using (var tab = ImGUITabItem.Aquire())
                {
                    if (tab.BeginTabItem("Ignore List"))
                    {
                        RenderIgnoreTab();
                    }
                }
                using (var tab = ImGUITabItem.Aquire())
                {
                    if (tab.BeginTabItem("Debug"))
                    {
                        RenderDebugTab();
                    }
                }
            }
        }

        private static void RenderDebugTab()
        {
            RenderStatusHeader();
            imgui_Separator();
            RenderDebugMetrics();
        }

        private static void RenderPullConfigTab()
        {
            imgui_TextColored(0.9f, 0.8f, 0.6f, 1.0f, "Pull Configuration");

            imgui_Text("Pull Method:");
            imgui_SetNextItemWidth(200);
            if (string.IsNullOrEmpty(_huntPullMethod))
            {
                _huntPullMethod = HuntProcessor.PullMethod ?? "None";
            }
            string previewMethod = _huntPullMethod ?? "None";
            string[] pullMethods = { "None", "Ranged", "Spell", "Item", "AA", "Disc", "Attack", "Melee" };
            if (imgui_BeginCombo("##hunt_pull_method", previewMethod, 0))
            {
                foreach (var method in pullMethods)
                {
                    bool selected = string.Equals(method, previewMethod, StringComparison.OrdinalIgnoreCase);
                    if (imgui_Selectable(method, selected))
                    {
                        _huntPullMethod = method;
                        HuntProcessor.PullMethod = method;
                        HuntProcessor.SaveHuntPullSettings();
                    }
                }
                imgui_EndCombo();
            }

            if (!string.Equals(_huntPullMethod, "None", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(_huntPullMethod, "Ranged", StringComparison.OrdinalIgnoreCase))
            {
                imgui_Separator();

                if (string.Equals(_huntPullMethod, "Spell", StringComparison.OrdinalIgnoreCase))
                {
                    imgui_Text("Spell Name:");
                    imgui_SetNextItemWidth(-1);
                    if (string.IsNullOrEmpty(_huntPullSpell))
                        _huntPullSpell = HuntProcessor.PullSpell ?? string.Empty;
                    if (imgui_InputText("##hunt_pull_spell", _huntPullSpell))
                    {
                        _huntPullSpell = imgui_InputText_Get("##hunt_pull_spell") ?? string.Empty;
                        HuntProcessor.PullSpell = _huntPullSpell;
                        HuntProcessor.SaveHuntPullSettings();
                    }
                }
                else if (string.Equals(_huntPullMethod, "Item", StringComparison.OrdinalIgnoreCase))
                {
                    imgui_Text("Item Name:");
                    imgui_SetNextItemWidth(-1);
                    if (string.IsNullOrEmpty(_huntPullItem))
                        _huntPullItem = HuntProcessor.PullItem ?? string.Empty;
                    if (imgui_InputText("##hunt_pull_item", _huntPullItem))
                    {
                        _huntPullItem = imgui_InputText_Get("##hunt_pull_item") ?? string.Empty;
                        HuntProcessor.PullItem = _huntPullItem;
                        HuntProcessor.SaveHuntPullSettings();
                    }
                }
                else if (string.Equals(_huntPullMethod, "AA", StringComparison.OrdinalIgnoreCase))
                {
                    imgui_Text("AA Name:");
                    imgui_SetNextItemWidth(-1);
                    if (string.IsNullOrEmpty(_huntPullAA))
                        _huntPullAA = HuntProcessor.PullAA ?? string.Empty;
                    if (imgui_InputText("##hunt_pull_aa", _huntPullAA))
                    {
                        _huntPullAA = imgui_InputText_Get("##hunt_pull_aa") ?? string.Empty;
                        HuntProcessor.PullAA = _huntPullAA;
                        HuntProcessor.SaveHuntPullSettings();
                    }
                }
                else if (string.Equals(_huntPullMethod, "Disc", StringComparison.OrdinalIgnoreCase))
                {
                    imgui_Text("Discipline Name:");
                    imgui_SetNextItemWidth(-1);
                    if (string.IsNullOrEmpty(_huntPullDisc))
                        _huntPullDisc = HuntProcessor.PullDisc ?? string.Empty;
                    if (imgui_InputText("##hunt_pull_disc", _huntPullDisc))
                    {
                        _huntPullDisc = imgui_InputText_Get("##hunt_pull_disc") ?? string.Empty;
                        HuntProcessor.PullDisc = _huntPullDisc;
                        HuntProcessor.SaveHuntPullSettings();
                    }
                }
            }

            imgui_Separator();
            bool autoAssist = HuntProcessor.AutoAssistAtMelee;
            if (imgui_Checkbox("Auto Assist at Melee Range", autoAssist))
            {
                HuntProcessor.AutoAssistAtMelee = imgui_Checkbox_Get("Auto Assist at Melee Range");
            }

            imgui_Separator();
            imgui_TextColored(0.9f, 0.9f, 0.7f, 1.0f, "Advanced Tuning");

            imgui_Text("Max Path Range (0 disables):");
            imgui_SetNextItemWidth(220);
            if (imgui_InputInt("##hunt_mpr", HuntProcessor.MaxPathRange, 25, 250))
            {
                int newValue = Math.Max(0, imgui_InputInt_Get("##hunt_mpr"));
                HuntProcessor.MaxPathRange = newValue;
                HuntProcessor.SaveHuntPullSettings();
            }

            imgui_Text("Pull Attempt Timeout (sec):");
            imgui_SetNextItemWidth(220);
            if (imgui_InputInt("##hunt_pits", HuntProcessor.PullIgnoreTimeSec, 1, 5))
            {
                int newValue = Math.Max(5, imgui_InputInt_Get("##hunt_pits"));
                HuntProcessor.PullIgnoreTimeSec = newValue;
                HuntProcessor.SaveHuntPullSettings();
            }

            imgui_Text("Temp Ignore Duration (sec):");
            imgui_SetNextItemWidth(220);
            if (imgui_InputInt("##hunt_tids", HuntProcessor.TempIgnoreDurationSec, 5, 10))
            {
                int newValue = Math.Max(1, imgui_InputInt_Get("##hunt_tids"));
                HuntProcessor.TempIgnoreDurationSec = newValue;
                HuntProcessor.SaveHuntPullSettings();
            }

            imgui_Text("Ranged Approach Factor (0.3 - 0.9):");
            imgui_SetNextItemWidth(220);
            if (string.IsNullOrEmpty(_rangedApproachFactorBuf))
            {
                _rangedApproachFactorBuf = HuntProcessor.RangedApproachFactor.ToString("0.00", CultureInfo.InvariantCulture);
            }
            if (imgui_InputText("##hunt_raf", _rangedApproachFactorBuf))
            {
                _rangedApproachFactorBuf = imgui_InputText_Get("##hunt_raf") ?? string.Empty;
                if (double.TryParse(_rangedApproachFactorBuf, NumberStyles.Float, CultureInfo.InvariantCulture, out var raf))
                {
                    raf = Math.Max(0.3, Math.Min(0.9, raf));
                    _rangedApproachFactorBuf = raf.ToString("0.00", CultureInfo.InvariantCulture);
                    HuntProcessor.RangedApproachFactor = raf;
                    HuntProcessor.SaveHuntPullSettings();
                }
            }
        }

        private static void RenderTargetsTab()
        {
            if (imgui_InputText("##hunt_candidate_filter", _candidateFilterBuffer ?? string.Empty))
            {
                _candidateFilterBuffer = imgui_InputText_Get("##hunt_candidate_filter") ?? string.Empty;
            }
            imgui_SameLine();
            imgui_Text("Filter");

            using (var table = ImGUITable.Aquire())
            {
                int tableFlags = (int)(ImGuiTableFlags.ImGuiTableFlags_RowBg | ImGuiTableFlags.ImGuiTableFlags_BordersOuter | ImGuiTableFlags.ImGuiTableFlags_ScrollY);
                if (table.BeginTable("##hunt_candidates", 7, tableFlags, 0f, 0f))
                {
                    imgui_TableSetupColumn("Name", (int)ImGuiTableColumnFlags.ImGuiTableColumnFlags_WidthStretch, 140);
                    imgui_TableSetupColumn("Lvl", (int)ImGuiTableColumnFlags.ImGuiTableColumnFlags_WidthFixed, 40);
                    imgui_TableSetupColumn("Dist", (int)ImGuiTableColumnFlags.ImGuiTableColumnFlags_WidthFixed, 60);
                    imgui_TableSetupColumn("Path", (int)ImGuiTableColumnFlags.ImGuiTableColumnFlags_WidthFixed, 60);
                    imgui_TableSetupColumn("Loc", (int)ImGuiTableColumnFlags.ImGuiTableColumnFlags_WidthStretch, 120);
                    imgui_TableSetupColumn("Con", (int)ImGuiTableColumnFlags.ImGuiTableColumnFlags_WidthFixed, 60);
                    imgui_TableSetupColumn("Actions", (int)ImGuiTableColumnFlags.ImGuiTableColumnFlags_WidthFixed, 110);
                    imgui_TableHeadersRow();

                    string filter = (_candidateFilterBuffer ?? string.Empty).Trim();
                    foreach (var entry in _candidateSnapshot)
                    {
                        if (!string.IsNullOrEmpty(filter) && entry.name != null && entry.name.IndexOf(filter, StringComparison.OrdinalIgnoreCase) < 0)
                        {
                            continue;
                        }

                        imgui_TableNextRow();
                        imgui_TableNextColumn(); imgui_Text(entry.name ?? string.Empty);
                        imgui_TableNextColumn(); imgui_Text(entry.level.ToString(CultureInfo.InvariantCulture));
                        imgui_TableNextColumn(); imgui_Text(entry.distance.ToString("0.0", CultureInfo.InvariantCulture));
                        imgui_TableNextColumn(); imgui_Text(entry.path > 0 ? entry.path.ToString("0.0", CultureInfo.InvariantCulture) : "-");
                        imgui_TableNextColumn(); imgui_Text(entry.loc ?? string.Empty);
                        imgui_TableNextColumn(); imgui_Text(entry.con ?? string.Empty);
                        imgui_TableNextColumn();
                        if (imgui_Button($"Target##{entry.id}"))
                        {
                            HuntProcessor.ForceSetTarget(entry.id);
                        }
                        imgui_SameLine();
                        if (imgui_Button($"Ignore##{entry.id}"))
                        {
                            HuntProcessor.IgnoreSpawn(entry.id, entry.name, true, true, "UI-Candidate");
                            RefreshUiSnapshots(true);
                        }
                    }
                }
            }
        }

        private static void RenderIgnoreTab()
        {
            imgui_Text($"Zone: {_currentZone}");
            using (var table = ImGUITable.Aquire())
            {
                int flags = (int)(ImGuiTableFlags.ImGuiTableFlags_BordersOuter | ImGuiTableFlags.ImGuiTableFlags_RowBg | ImGuiTableFlags.ImGuiTableFlags_ScrollY);
                if (table.BeginTable("##hunt_ignore", 2, flags, 0f, 0f))
                {
                    imgui_TableSetupColumn("Name", (int)ImGuiTableColumnFlags.ImGuiTableColumnFlags_WidthStretch, 200);
                    imgui_TableSetupColumn("Action", (int)ImGuiTableColumnFlags.ImGuiTableColumnFlags_WidthFixed, 80);
                    imgui_TableHeadersRow();

                    foreach (var name in _ignoreSnapshot)
                    {
                        imgui_TableNextRow();
                        imgui_TableNextColumn(); imgui_Text(name);
                        imgui_TableNextColumn();
                        if (imgui_Button($"Remove##{name}"))
                        {
                            HuntProcessor.RemoveIgnoreName(name);
                            RefreshUiSnapshots(true);
                        }
                    }
                }
            }

            if (_ignoreByZone.Count > 0)
            {
                imgui_Text("All Zones");
                using (var table = ImGUITable.Aquire())
                {
                    if (table.BeginTable("##hunt_ignore_zones", 2, (int)ImGuiTableFlags.ImGuiTableFlags_RowBg, 0f, 0f))
                    {
                        imgui_TableSetupColumn("Zone", (int)ImGuiTableColumnFlags.ImGuiTableColumnFlags_WidthStretch, 200);
                        imgui_TableSetupColumn("Count", (int)ImGuiTableColumnFlags.ImGuiTableColumnFlags_WidthFixed, 80);
                        foreach (var kvp in _ignoreByZone)
                        {
                            imgui_TableNextRow();
                            imgui_TableNextColumn(); imgui_Text(kvp.Key);
                            imgui_TableNextColumn(); imgui_Text(kvp.Value?.Count.ToString() ?? "0");
                        }
                    }
                }
            }
        }

        private static void RenderDebugMetrics()
        {
            imgui_Text("Navigation");
            imgui_Text($"Navigation owner: {(HuntStateMachine.IsNavigationOwned ? "Yes" : "No")}");
            imgui_Text($"Pull cooldown: {FormatMs(HuntProcessor.GetMsUntilNextPull())}");
            imgui_Text($"Scan cooldown: {FormatMs(HuntProcessor.GetMsUntilNextScan())}");
            imgui_Text($"Nav cooldown: {FormatMs(HuntProcessor.GetMsUntilNextNav())}");

            imgui_Separator();
            imgui_Text("SmartLoot");
            imgui_Text($"State: {HuntProcessor.SmartLootState}");
            imgui_Text($"Mode: {HuntProcessor.SmartLootMode}");
            imgui_Text($"Active: {(HuntProcessor.SmartLootActive ? "Yes" : "No")}");
        }

        private static void RenderDebugWindow()
        {
            if (!_imguiContextReady) return;
            if (!imgui_Begin_OpenFlagGet(DebugWindowName)) return;

            RefreshDebugSnapshot();

            imgui_SetNextWindowSizeWithCond(520, 400, (int)ImGuiCond.FirstUseEver);
            E3ImGUI.PushCurrentTheme();
            try
            {
                using (var window = ImGUIWindow.Aquire())
                {
                    if (!window.Begin(DebugWindowName, (int)ImGuiWindowFlags.ImGuiWindowFlags_NoDocking))
                    {
                        return;
                    }

                    imgui_TextColored(0.9f, 0.8f, 0.2f, 1.0f, "Hunt Debug Log");
                    if (imgui_Button("Clear"))
                    {
                        HuntProcessor.ClearDebugLog();
                        RefreshDebugSnapshot(true);
                    }
                    imgui_SameLine();
                    if (imgui_Button("Refresh"))
                    {
                        RefreshDebugSnapshot(true);
                    }

                    imgui_Separator();
                    imgui_Text($"State: {HuntStateMachine.CurrentState} | Reason: {HuntStateMachine.StateReason}");
                    imgui_Text($"Target: {HuntProcessor.TargetName} ({HuntProcessor.TargetID})");

                    using (var child = ImGUIChild.Aquire())
                    {
                        if (child.BeginChild("##hunt_debug_scroll", 0f, 0f, 0, 0))
                        {
                            foreach (var line in _debugSnapshot)
                            {
                                var ts = TimeSpan.FromMilliseconds(line.ts);
                                string prefix = $"[{ts:c}]";
                                imgui_Text(prefix + " " + (line.msg ?? string.Empty));
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

        private static void RefreshUiSnapshots(bool force = false)
        {
            if (!force && Core.StopWatch.ElapsedMilliseconds < _nextUiRefreshAt) return;
            _nextUiRefreshAt = Core.StopWatch.ElapsedMilliseconds + 1000;

            try
            {
                _candidateSnapshot = HuntProcessor.GetCandidatesSnapshot();
                _ignoreSnapshot = HuntProcessor.GetIgnoreListSnapshotCached();
                _ignoreByZone = HuntProcessor.GetAllZoneIgnoreListsSnapshot();
                _currentZone = HuntProcessor.GetCurrentZoneCached();
            }
            catch { }
        }

        private static void RefreshDebugSnapshot(bool force = false)
        {
            if (!force && Core.StopWatch.ElapsedMilliseconds < _nextDebugRefreshAt) return;
            _nextDebugRefreshAt = Core.StopWatch.ElapsedMilliseconds + 1000;
            try
            {
                _debugSnapshot = HuntProcessor.GetDebugLogSnapshot(200);
            }
            catch { }
        }

        private static string FormatMs(int ms)
        {
            if (ms <= 0) return "ready";
            if (ms < 1000) return ms + " ms";
            return (ms / 1000.0).ToString("0.0s", CultureInfo.InvariantCulture);
        }

        private static void SyncBuffersFromState()
        {
            _radiusBuffer = HuntProcessor.Radius.ToString(CultureInfo.InvariantCulture);
            _zRadiusBuffer = HuntProcessor.ZRadius.ToString(CultureInfo.InvariantCulture);
            _pullFilterBuffer = HuntProcessor.PullFilters ?? string.Empty;
            _ignoreFilterBuffer = HuntProcessor.IgnoreFilters ?? string.Empty;
            _huntPullMethod = HuntProcessor.PullMethod ?? "None";
            _huntPullSpell = HuntProcessor.PullSpell ?? string.Empty;
            _huntPullItem = HuntProcessor.PullItem ?? string.Empty;
            _huntPullAA = HuntProcessor.PullAA ?? string.Empty;
            _huntPullDisc = HuntProcessor.PullDisc ?? string.Empty;
            _rangedApproachFactorBuf = HuntProcessor.RangedApproachFactor.ToString("0.00", CultureInfo.InvariantCulture);
        }
    }
}
