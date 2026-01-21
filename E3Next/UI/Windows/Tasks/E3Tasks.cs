using E3Core.Data;
using E3Core.Processors;
using E3Core.Server;
using E3Core.Utility;
using MonoCore;
using System;
using System.Collections.Generic;
using System.Linq;
using static MonoCore.E3ImGUI;

namespace E3Core.UI.Windows
{
    public static class E3TasksWindow
    {
        private const string WindowName = "E3 Tasks";
        private const long TimerDisplayThresholdSeconds = 604800; // one week
        private const string UntitledTaskLabel = "(Untitled Task)";

        private static bool _windowInitialized;
        private static bool _imguiContextReady;
        private static bool _forceRefresh;
        private static long _nextRefresh;
        private static readonly long _refreshInterval = 1000;
        private static long _lastDataUpdate;

        // Cached snapshot of the last task query so the ImGui render stays cheap.
        private static readonly List<TaskSnapshot> _cachedTasks = new List<TaskSnapshot>();
        private static readonly List<PeerTaskSummary> _peerTasks = new List<PeerTaskSummary>();

        private enum PeerTaskPresence
        {
            Unknown,
            AllPeersHave,
            MissingPeers
        }

        private static readonly IMQ MQ = E3.MQ;
        private static readonly Logging _log = E3.Log;

        private static readonly (float R, float G, float B) CompletedColor = (0.25f, 0.85f, 0.4f);
        private static readonly (float R, float G, float B) ActiveColor = (0.95f, 0.85f, 0.35f);
        private static readonly (float R, float G, float B) MissingPeerColor = (0.95f, 0.35f, 0.35f);

        // Font Awesome icons (merged into MQ's default ImGui font)
        private const string FA_CHECK = "\uf00c";
        private const string FA_TIMES = "\uf00d";

        // Task detail window state
        private const string DetailWindowName = "Task Peer Status";
        private static TaskSnapshot _selectedTask;
        private static bool _detailWindowOpen;

        // Peer view state - when set, the main window shows this peer's tasks instead of our own
        private static PeerTaskSummary _viewingPeer;
        // Track which task to auto-expand when switching to peer view (from Task Peer Status popup)
        private static string _autoExpandTaskKey;

        // Advanced view state
        private static bool _useAdvancedView;
        private static string _advSelectedPeerFilter = "All Peers"; // "All Peers", "Self", or a peer name
        private static string _advSelectedTaskKey; // The selected task in the left pane
        private static string _advSelectedObjectiveSource; // null = local/auto, or peer name to show their objectives
        private static readonly List<AdvancedTaskEntry> _advAllTasks = new List<AdvancedTaskEntry>();

        [SubSystemInit]
        public static void Init()
        {
            if (Core._MQ2MonoVersion < 0.36m) return;

            E3ImGUI.RegisterWindow(WindowName, RenderWindow);
            E3ImGUI.RegisterWindow(DetailWindowName, RenderDetailWindow);

            EventProcessor.RegisterCommand("/e3tasks", x =>
            {
                if (Core._MQ2MonoVersion < 0.36m)
                {
                    MQ.Write("E3 Tasks window requires MQ2Mono 0.36 or greater.");
                    return;
                }

                ToggleWindow();
            }, "Toggle the E3 Task progress window");
        }

        public static void ToggleWindow()
        {
            try
            {
                if (!_windowInitialized)
                {
                    _windowInitialized = true;
                    imgui_Begin_OpenFlagSet(WindowName, true);
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
                _log.Write($"E3 Tasks window error: {ex.Message}", Logging.LogLevels.Error);
                _imguiContextReady = false;
            }
        }

        private static void RenderWindow()
        {
            if (!_imguiContextReady) return;
            if (!imgui_Begin_OpenFlagGet(WindowName)) return;

            RefreshTaskData();

            PushCurrentTheme();
            try
            {
                imgui_SetNextWindowSizeWithCond(620f, 680f, (int)ImGuiCond.FirstUseEver);

                using (var window = ImGUIWindow.Aquire())
                {
                    int flags = (int)ImGuiWindowFlags.ImGuiWindowFlags_NoCollapse;
                    if (!window.Begin(WindowName, flags))
                    {
                        return;
                    }

                    // If viewing a peer's tasks, show that view instead of the normal tabs
                    if (_viewingPeer != null)
                    {
                        RenderPeerTaskView();
                    }
                    else if (_useAdvancedView)
                    {
                        RenderAdvancedView();
                    }
                    else
                    {
                        RenderTaskHeader();
                        imgui_Separator();
                        RenderTasks();
                    }
                }
            }
            finally
            {
                PopCurrentTheme();
            }
        }

        private static void RenderTaskHeader()
        {
            imgui_Text($"Active tasks: {_cachedTasks.Count}");
            imgui_SameLine();

            if (imgui_Button("Refresh"))
            {
                _forceRefresh = true;
                RefreshTaskData(force: true);
            }

            imgui_SameLine();
            if (imgui_Button("Advanced View"))
            {
                _useAdvancedView = true;
                RefreshAdvancedTaskList();
            }

            imgui_TextColored(CompletedColor.R, CompletedColor.G, CompletedColor.B, 1f, FA_CHECK);
            imgui_SameLine(0f, 4f);
            imgui_Text("= All bots have task");
            imgui_SameLine(0f, 12f);
            imgui_TextColored(MissingPeerColor.R, MissingPeerColor.G, MissingPeerColor.B, 1f, FA_TIMES);
            imgui_SameLine(0f, 4f);
            imgui_Text("= At least 1 bot missing task");
        }

        private static void RenderTasks()
        {
            if (_cachedTasks.Count == 0)
            {
                imgui_TextColored(0.75f, 0.75f, 0.75f, 1f, "No active tasks detected. Accept a task to populate this view.");
                return;
            }

            foreach (var task in _cachedTasks)
            {
                RenderTask(task);
            }
        }

        private static void RenderTask(TaskSnapshot task)
        {
            using (var tree = ImGUITree.Aquire())
            {
                int treeFlags = (int)(ImGuiTreeNodeFlags.ImGuiTreeNodeFlags_Framed |
                                      ImGuiTreeNodeFlags.ImGuiTreeNodeFlags_SpanAvailWidth);

                string header = string.IsNullOrEmpty(task.Type)
                    ? task.Title
                    : $"{task.Title} [{task.Type}]";

                if (task.IsComplete)
                {
                    header += " (Complete)";
                }

                // Render peer coverage icon before the tree node
                RenderTaskPeerCoverageIndicator(task);

                bool isOpen = tree.TreeNodeEx($"{header}##task_{task.Slot}", treeFlags);

                if (isOpen)
                {
                    RenderTaskSummary(task);
                    imgui_Separator();
                    RenderObjectivesTable(task);
                }
            }

            imgui_Separator();
        }

        private static void RenderTaskPeerCoverageIndicator(TaskSnapshot task)
        {
            int peersWithTask;
            int peerCount;
            var presence = EvaluatePeerTaskPresence(task, out peersWithTask, out peerCount);

            if (presence == PeerTaskPresence.Unknown)
            {
                return;
            }

            string symbol = presence == PeerTaskPresence.AllPeersHave ? FA_CHECK : FA_TIMES;
            var color = presence == PeerTaskPresence.AllPeersHave ? CompletedColor : MissingPeerColor;
            imgui_TextColored(color.R, color.G, color.B, 1f, symbol);

            if (imgui_IsItemHovered())
            {
                // Click to open detail window
                if (imgui_IsMouseClicked(0))
                {
                    _selectedTask = task;
                    _detailWindowOpen = true;
                    imgui_Begin_OpenFlagSet(DetailWindowName, true);
                }

                using (var tooltip = ImGUIToolTip.Aquire())
                {
                    string peersLabel = peerCount == 1 ? "peer" : "peers";

                    if (presence == PeerTaskPresence.AllPeersHave)
                    {
                        imgui_Text($"All {peerCount} {peersLabel} share this task.");
                    }
                    else
                    {
                        imgui_Text($"{peersWithTask} of {peerCount} {peersLabel} have this task.");
                    }
                    imgui_Text("Click for details.");
                }
            }

            imgui_SameLine(0f, 6f);
        }

        private static void RenderTaskSummary(TaskSnapshot task)
        {
            var statusColor = task.IsComplete ? CompletedColor : ActiveColor;
            imgui_TextColored(statusColor.R, statusColor.G, statusColor.B, 1f,
                $"Status: {(task.IsComplete ? "Complete" : "In Progress")}");

            if (!string.IsNullOrWhiteSpace(task.ActiveStep))
            {
                imgui_TextColored(0.75f, 0.9f, 1f, 1f, "Current Step:");
                imgui_TextWrapped(task.ActiveStep);
            }

            if (task.TimerSeconds > 0 && task.TimerSeconds < TimerDisplayThresholdSeconds && !string.IsNullOrWhiteSpace(task.TimerDisplay))
            {
                imgui_TextColored(0.9f, 0.7f, 0.35f, 1f, $"Time Remaining: {task.TimerDisplay}");
            }

            if (task.MemberCount > 1 || !string.IsNullOrWhiteSpace(task.Leader))
            {
                string summary = string.Empty;
                if (task.MemberCount > 1)
                {
                    summary = $"Members: {task.MemberCount}";
                }

                if (!string.IsNullOrWhiteSpace(task.Leader))
                {
                    summary = string.IsNullOrEmpty(summary)
                        ? $"Leader: {task.Leader}"
                        : $"{summary} | Leader: {task.Leader}";
                }

                if (!string.IsNullOrEmpty(summary))
                {
                    imgui_Text(summary);
                }
            }
        }

        private static void RenderObjectivesTable(TaskSnapshot task)
        {
            if (task.Objectives.Count == 0)
            {
                imgui_TextColored(0.75f, 0.75f, 0.75f, 1f, "No objectives found for this task yet.");
                return;
            }

            bool enableScroll = task.Objectives.Count > 6;
            float desiredHeight = enableScroll
                ? Math.Min(Math.Max(task.Objectives.Count * 26f, 150f), Math.Max(220f, imgui_GetContentRegionAvailY() * 0.6f))
                : 0f;

            int tableFlags = (int)(ImGuiTableFlags.ImGuiTableFlags_RowBg |
                                   ImGuiTableFlags.ImGuiTableFlags_BordersInner |
                                   ImGuiTableFlags.ImGuiTableFlags_BordersOuter |
                                   ImGuiTableFlags.ImGuiTableFlags_Resizable |
                                   (enableScroll ? ImGuiTableFlags.ImGuiTableFlags_ScrollY : 0));

            using (var table = ImGUITable.Aquire())
            {
                if (!table.BeginTable($"TaskObjectives##{task.Slot}", 3, tableFlags, 0f, desiredHeight))
                {
                    return;
                }

                imgui_TableSetupColumn("Objective", (int)ImGuiTableColumnFlags.ImGuiTableColumnFlags_WidthStretch, 300f);
                imgui_TableSetupColumn("Progress", (int)ImGuiTableColumnFlags.ImGuiTableColumnFlags_WidthFixed, 110f);
                imgui_TableSetupColumn("Zone", (int)ImGuiTableColumnFlags.ImGuiTableColumnFlags_WidthFixed, 140f);
                imgui_TableHeadersRow();

                foreach (var objective in task.Objectives)
                {
                    imgui_TableNextRow();

                    imgui_TableNextColumn();
                    string label = $"{objective.Index}. {objective.Instruction}";
                    if (objective.Optional)
                    {
                        label += " (Optional)";
                    }
                    imgui_TextWrapped(label);

                    imgui_TableNextColumn();
                    var color = objective.IsComplete ? CompletedColor : ActiveColor;
                    string statusText = string.IsNullOrWhiteSpace(objective.Status)
                        ? (objective.IsComplete ? "Done" : "In Progress")
                        : objective.Status;
                    imgui_TextColored(color.R, color.G, color.B, 1f, statusText);

                    imgui_TableNextColumn();
                    imgui_Text(string.IsNullOrWhiteSpace(objective.Zone) ? "Any" : objective.Zone);
                }
            }
        }

        private static void RefreshTaskData(bool force = false)
        {
            if (!force)
            {
                if (!_forceRefresh && !e3util.ShouldCheck(ref _nextRefresh, _refreshInterval))
                {
                    return;
                }
            }

            _forceRefresh = false;

            try
            {
                var snapshot = TaskDataCollector.Capture(MQ, allowDelays: false);
                foreach (var task in snapshot)
                {
                    if (task.TimerSeconds >= TimerDisplayThresholdSeconds)
                    {
                        task.TimerSeconds = 0;
                        task.TimerDisplay = string.Empty;
                    }
                }

                _cachedTasks.Clear();
                _cachedTasks.AddRange(snapshot
                    .OrderBy(t => t.IsComplete)
                    .ThenBy(t => t.Type, StringComparer.OrdinalIgnoreCase)
                    .ThenBy(t => t.Title, StringComparer.OrdinalIgnoreCase));

                _lastDataUpdate = Core.StopWatch.ElapsedMilliseconds;

                RefreshPeerTaskData();
            }
            catch (ThreadAbort)
            {
                throw;
            }
            catch (Exception ex)
            {
                _log.Write($"Failed to refresh task data: {ex.Message}", Logging.LogLevels.Error);
            }
        }

        private static void RefreshPeerTaskData()
        {
            _peerTasks.Clear();

            var sharedClient = NetMQServer.SharedDataClient;
            if (sharedClient == null) return;

            foreach (var kvp in sharedClient.TopicUpdates)
            {
                var bot = kvp.Key;
                if (string.IsNullOrWhiteSpace(bot)) continue;
                if (string.Equals(bot, E3.CurrentName, StringComparison.OrdinalIgnoreCase)) continue;
                if (string.Equals(bot, "proxy", StringComparison.OrdinalIgnoreCase)) continue;

                var topics = kvp.Value;
                if (topics == null) continue;

                if (!topics.TryGetValue("E3Tasks", out var entry))
                {
                    continue;
                }

                var summaries = TaskDataCollector.DeserializeFromWire(entry.Data);

                _peerTasks.Add(new PeerTaskSummary
                {
                    Name = bot,
                    LastUpdate = entry.LastUpdate,
                    Tasks = summaries ?? new List<TaskWireSummary>()
                });
            }

            _peerTasks.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));
        }

        private static string BuildTaskComparisonKey(string title, string type)
        {
            string normalizedTitle = string.IsNullOrWhiteSpace(title) ? UntitledTaskLabel : title.Trim();
            string normalizedType = string.IsNullOrWhiteSpace(type) ? string.Empty : type.Trim();
            return $"{normalizedTitle}|{normalizedType}";
        }

        private static bool PeerHasTask(PeerTaskSummary peer, string targetKey)
        {
            if (peer?.Tasks == null || peer.Tasks.Count == 0)
            {
                return false;
            }

            foreach (var summary in peer.Tasks)
            {
                if (summary == null) continue;

                string summaryKey = BuildTaskComparisonKey(summary.Title ?? string.Empty, summary.Type ?? string.Empty);
                if (string.Equals(summaryKey, targetKey, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private static PeerTaskPresence EvaluatePeerTaskPresence(TaskSnapshot task, out int peersWithTask, out int peerCount)
        {
            peersWithTask = 0;
            peerCount = _peerTasks.Count;

            if (peerCount == 0)
            {
                return PeerTaskPresence.Unknown;
            }

            string targetKey = BuildTaskComparisonKey(task.Title ?? string.Empty, task.Type ?? string.Empty);
            bool hasMissingPeer = false;

            foreach (var peer in _peerTasks)
            {
                if (PeerHasTask(peer, targetKey))
                {
                    peersWithTask++;
                }
                else
                {
                    hasMissingPeer = true;
                }
            }

            return hasMissingPeer ? PeerTaskPresence.MissingPeers : PeerTaskPresence.AllPeersHave;
        }

        private static void RenderDetailWindow()
        {
            if (!_detailWindowOpen || _selectedTask == null)
            {
                return;
            }

            if (!imgui_Begin_OpenFlagGet(DetailWindowName))
            {
                _detailWindowOpen = false;
                _selectedTask = null;
                return;
            }

            PushCurrentTheme();
            try
            {
                int flags = (int)ImGuiWindowFlags.ImGuiWindowFlags_None;
                if (!imgui_Begin(DetailWindowName, flags))
                {
                    imgui_End();
                    return;
                }

                var task = _selectedTask;
                string header = string.IsNullOrEmpty(task.Type)
                    ? task.Title
                    : $"{task.Title} [{task.Type}]";

                imgui_TextColored(0.9f, 0.9f, 0.5f, 1f, header);
                imgui_Separator();

                // Task objectives section
                using (var tree = ImGUITree.Aquire())
                {
                    int treeFlags = (int)(ImGuiTreeNodeFlags.ImGuiTreeNodeFlags_DefaultOpen |
                                          ImGuiTreeNodeFlags.ImGuiTreeNodeFlags_SpanAvailWidth);

                    if (tree.TreeNodeEx("Objectives##detail", treeFlags))
                    {
                        if (task.Objectives.Count == 0)
                        {
                            imgui_TextColored(0.6f, 0.6f, 0.6f, 1f, "No objectives available.");
                        }
                        else
                        {
                            foreach (var obj in task.Objectives)
                            {
                                var objColor = obj.IsComplete ? CompletedColor : ActiveColor;
                                string icon = obj.IsComplete ? FA_CHECK : FA_TIMES;
                                string optional = obj.Optional ? " (Optional)" : "";

                                imgui_TextColored(objColor.R, objColor.G, objColor.B, 1f, icon);
                                imgui_SameLine(0f, 6f);
                                imgui_Text($"{obj.Instruction}{optional}");

                                if (!string.IsNullOrWhiteSpace(obj.Status) && !obj.IsComplete)
                                {
                                    imgui_SameLine(0f, 8f);
                                    imgui_TextColored(0.7f, 0.7f, 0.7f, 1f, $"[{obj.Status}]");
                                }
                            }
                        }
                    }
                }

                imgui_Separator();

                // Peer status section
                using (var tree = ImGUITree.Aquire())
                {
                    int treeFlags = (int)(ImGuiTreeNodeFlags.ImGuiTreeNodeFlags_DefaultOpen |
                                          ImGuiTreeNodeFlags.ImGuiTreeNodeFlags_SpanAvailWidth);

                    string targetKey = BuildTaskComparisonKey(task.Title ?? string.Empty, task.Type ?? string.Empty);

                    if (tree.TreeNodeEx($"Peer Status ({_peerTasks.Count} peers)##detail", treeFlags))
                    {
                        if (_peerTasks.Count == 0)
                        {
                            imgui_TextColored(0.6f, 0.6f, 0.6f, 1f, "No peers reporting.");
                        }
                        else
                        {
                            foreach (var peer in _peerTasks)
                            {
                                TaskWireSummary peerTask = null;
                                if (peer.Tasks != null)
                                {
                                    foreach (var pt in peer.Tasks)
                                    {
                                        string ptKey = BuildTaskComparisonKey(pt.Title ?? string.Empty, pt.Type ?? string.Empty);
                                        if (string.Equals(ptKey, targetKey, StringComparison.OrdinalIgnoreCase))
                                        {
                                            peerTask = pt;
                                            break;
                                        }
                                    }
                                }

                                if (peerTask != null)
                                {
                                    var statusColor = peerTask.IsComplete ? CompletedColor : ActiveColor;
                                    string icon = peerTask.IsComplete ? FA_CHECK : FA_TIMES;
                                    string progress = $"{peerTask.CompletedObjectives}/{peerTask.TotalObjectives}";

                                    imgui_TextColored(CompletedColor.R, CompletedColor.G, CompletedColor.B, 1f, FA_CHECK);
                                    imgui_SameLine(0f, 6f);
                                    RenderClickablePeerName(peer);
                                    imgui_SameLine(0f, 8f);
                                    imgui_TextColored(statusColor.R, statusColor.G, statusColor.B, 1f,
                                        peerTask.IsComplete ? "(Complete)" : $"({progress})");
                                }
                                else
                                {
                                    imgui_TextColored(MissingPeerColor.R, MissingPeerColor.G, MissingPeerColor.B, 1f, FA_TIMES);
                                    imgui_SameLine(0f, 6f);
                                    RenderClickablePeerName(peer);
                                    imgui_SameLine(0f, 8f);
                                    imgui_TextColored(0.6f, 0.6f, 0.6f, 1f, "(Missing task)");
                                }
                            }
                        }
                    }
                }

                imgui_End();
            }
            finally
            {
                PopCurrentTheme();
            }
        }

        private static void RenderClickablePeerName(PeerTaskSummary peer)
        {
            // Render peer name in a clickable style
            imgui_TextColored(0.65f, 0.85f, 1f, 1f, peer.Name);

            if (imgui_IsItemHovered())
            {
                if (imgui_IsMouseClicked(0))
                {
                    _viewingPeer = peer;
                    // If coming from the Task Peer Status popup, remember which task to auto-expand
                    // Keep the popup open so user can quickly switch between peers
                    if (_detailWindowOpen && _selectedTask != null)
                    {
                        _autoExpandTaskKey = BuildTaskComparisonKey(
                            _selectedTask.Title ?? string.Empty,
                            _selectedTask.Type ?? string.Empty);
                    }
                    else
                    {
                        _autoExpandTaskKey = null;
                    }
                }

                using (var tooltip = ImGUIToolTip.Aquire())
                {
                    imgui_Text($"Click to view {peer.Name}'s tasks");
                }
            }
        }

        private static void RenderPeerTaskView()
        {
            var peer = _viewingPeer;
            if (peer == null) return;

            // Header with back button
            if (imgui_Button("<< Back"))
            {
                _viewingPeer = null;
                _autoExpandTaskKey = null;
                return;
            }

            imgui_SameLine();
            imgui_TextColored(0.65f, 0.85f, 1f, 1f, $"{peer.Name}'s Tasks");
            imgui_SameLine();
            imgui_TextColored(0.6f, 0.6f, 0.6f, 1f, $"({peer.Tasks.Count} tasks)");

            imgui_SameLine();
            if (imgui_Button("Refresh"))
            {
                _forceRefresh = true;
                RefreshTaskData(force: true);
            }

            imgui_Separator();

            // Re-fetch the peer data in case it was updated
            var updatedPeer = _peerTasks.FirstOrDefault(p =>
                string.Equals(p.Name, peer.Name, StringComparison.OrdinalIgnoreCase));

            if (updatedPeer != null)
            {
                _viewingPeer = updatedPeer;
                peer = updatedPeer;
            }

            if (peer.Tasks == null || peer.Tasks.Count == 0)
            {
                imgui_TextColored(0.75f, 0.75f, 0.75f, 1f, "No tasks reported by this peer.");
                return;
            }

            // Render each task similarly to how "My Tasks" renders them
            foreach (var task in peer.Tasks
                .OrderBy(t => t.IsComplete)
                .ThenBy(t => t.Title, StringComparer.OrdinalIgnoreCase))
            {
                RenderPeerTaskDetail(task);
            }
        }

        private static void RenderPeerTaskDetail(TaskWireSummary task)
        {
            using (var tree = ImGUITree.Aquire())
            {
                int treeFlags = (int)(ImGuiTreeNodeFlags.ImGuiTreeNodeFlags_Framed |
                                      ImGuiTreeNodeFlags.ImGuiTreeNodeFlags_SpanAvailWidth);

                string header = string.IsNullOrEmpty(task.Type)
                    ? task.Title
                    : $"{task.Title} [{task.Type}]";

                if (task.IsComplete)
                {
                    header += " (Complete)";
                }

                // Check if this task should be auto-expanded
                string taskKey = BuildTaskComparisonKey(task.Title ?? string.Empty, task.Type ?? string.Empty);
                bool shouldAutoExpand = !string.IsNullOrEmpty(_autoExpandTaskKey) &&
                    string.Equals(taskKey, _autoExpandTaskKey, StringComparison.OrdinalIgnoreCase);

                if (shouldAutoExpand)
                {
                    treeFlags |= (int)ImGuiTreeNodeFlags.ImGuiTreeNodeFlags_DefaultOpen;
                    // Clear after use so it only auto-expands once
                    _autoExpandTaskKey = null;
                }

                bool isOpen = tree.TreeNodeEx($"{header}##peertask_{task.Title}_{task.Type}", treeFlags);

                if (isOpen)
                {
                    // Status
                    var statusColor = task.IsComplete ? CompletedColor : ActiveColor;
                    imgui_TextColored(statusColor.R, statusColor.G, statusColor.B, 1f,
                        $"Status: {(task.IsComplete ? "Complete" : "In Progress")}");

                    // Current step
                    if (!string.IsNullOrWhiteSpace(task.ActiveStep))
                    {
                        imgui_TextColored(0.75f, 0.9f, 1f, 1f, "Current Step:");
                        imgui_TextWrapped(task.ActiveStep);
                    }

                    // Progress
                    if (task.TotalObjectives > 0)
                    {
                        int completed = Math.Max(0, Math.Min(task.CompletedObjectives, task.TotalObjectives));
                        imgui_Text($"Progress: {completed}/{task.TotalObjectives}");
                    }

                    // Timer
                    if (!string.IsNullOrEmpty(task.TimerDisplay))
                    {
                        imgui_TextColored(0.9f, 0.7f, 0.35f, 1f, $"Time Remaining: {task.TimerDisplay}");
                    }

                    // Objectives table
                    if (task.Objectives.Count > 0)
                    {
                        imgui_Separator();
                        RenderPeerObjectivesTable(task);
                    }
                }
            }

            imgui_Separator();
        }

        private static void RenderPeerObjectivesTable(TaskWireSummary task)
        {
            bool enableScroll = task.Objectives.Count > 6;
            float desiredHeight = enableScroll
                ? Math.Min(Math.Max(task.Objectives.Count * 26f, 150f), 220f)
                : 0f;

            int tableFlags = (int)(ImGuiTableFlags.ImGuiTableFlags_RowBg |
                                   ImGuiTableFlags.ImGuiTableFlags_BordersInner |
                                   ImGuiTableFlags.ImGuiTableFlags_BordersOuter |
                                   ImGuiTableFlags.ImGuiTableFlags_Resizable |
                                   (enableScroll ? ImGuiTableFlags.ImGuiTableFlags_ScrollY : 0));

            using (var table = ImGUITable.Aquire())
            {
                if (!table.BeginTable($"PeerObjectives##{task.Title}_{task.Type}", 3, tableFlags, 0f, desiredHeight))
                {
                    return;
                }

                imgui_TableSetupColumn("Objective", (int)ImGuiTableColumnFlags.ImGuiTableColumnFlags_WidthStretch, 260f);
                imgui_TableSetupColumn("Progress", (int)ImGuiTableColumnFlags.ImGuiTableColumnFlags_WidthFixed, 100f);
                imgui_TableSetupColumn("Zone", (int)ImGuiTableColumnFlags.ImGuiTableColumnFlags_WidthFixed, 120f);
                imgui_TableHeadersRow();

                foreach (var obj in task.Objectives)
                {
                    imgui_TableNextRow();

                    imgui_TableNextColumn();
                    string label = $"{obj.Index}. {obj.Instruction}";
                    if (obj.Optional)
                    {
                        label += " (Optional)";
                    }
                    imgui_TextWrapped(label);

                    imgui_TableNextColumn();
                    var color = obj.IsComplete ? CompletedColor : ActiveColor;
                    string statusText = string.IsNullOrWhiteSpace(obj.Status)
                        ? (obj.IsComplete ? "Done" : "In Progress")
                        : obj.Status;
                    imgui_TextColored(color.R, color.G, color.B, 1f, statusText);

                    imgui_TableNextColumn();
                    imgui_Text(string.IsNullOrWhiteSpace(obj.Zone) ? "Any" : obj.Zone);
                }
            }
        }

        #region Advanced View

        private static void RefreshAdvancedTaskList()
        {
            _advAllTasks.Clear();
            var seenKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // Add local tasks
            foreach (var task in _cachedTasks)
            {
                string key = BuildTaskComparisonKey(task.Title ?? string.Empty, task.Type ?? string.Empty);
                if (!seenKeys.Contains(key))
                {
                    seenKeys.Add(key);
                    _advAllTasks.Add(new AdvancedTaskEntry
                    {
                        Key = key,
                        Title = task.Title,
                        Type = task.Type,
                        LocalTask = task
                    });
                }
            }

            // Add peer tasks
            foreach (var peer in _peerTasks)
            {
                if (peer.Tasks == null) continue;
                foreach (var task in peer.Tasks)
                {
                    string key = BuildTaskComparisonKey(task.Title ?? string.Empty, task.Type ?? string.Empty);
                    if (!seenKeys.Contains(key))
                    {
                        seenKeys.Add(key);
                        _advAllTasks.Add(new AdvancedTaskEntry
                        {
                            Key = key,
                            Title = task.Title,
                            Type = task.Type
                        });
                    }
                }
            }

            // Sort by title
            _advAllTasks.Sort((a, b) => string.Compare(a.Title, b.Title, StringComparison.OrdinalIgnoreCase));

            // Select first task if none selected
            if (string.IsNullOrEmpty(_advSelectedTaskKey) && _advAllTasks.Count > 0)
            {
                _advSelectedTaskKey = _advAllTasks[0].Key;
            }
        }

        private static void RenderAdvancedView()
        {
            // Header with back button and peer filter
            if (imgui_Button("<< Simple View"))
            {
                _useAdvancedView = false;
                return;
            }

            imgui_SameLine();
            if (imgui_Button("Refresh"))
            {
                _forceRefresh = true;
                RefreshTaskData(force: true);
                RefreshAdvancedTaskList();
            }

            imgui_SameLine();
            imgui_Text("Filter:");
            imgui_SameLine();
            imgui_SetNextItemWidth(180f);
            using (var combo = ImGUICombo.Aquire())
            {
                if (combo.BeginCombo("##PeerFilter", _advSelectedPeerFilter))
                {
                    if (imgui_Selectable("All Peers", _advSelectedPeerFilter == "All Peers"))
                    {
                        _advSelectedPeerFilter = "All Peers";
                        RefreshAdvancedTaskList();
                    }
                    if (imgui_Selectable($"Self ({E3.CurrentName})", _advSelectedPeerFilter == "Self"))
                    {
                        _advSelectedPeerFilter = "Self";
                        RefreshAdvancedTaskList();
                    }

                    if (_peerTasks.Count > 0)
                    {
                        imgui_Separator();
                        foreach (var peer in _peerTasks)
                        {
                            if (imgui_Selectable(peer.Name, _advSelectedPeerFilter == peer.Name))
                            {
                                _advSelectedPeerFilter = peer.Name;
                                RefreshAdvancedTaskList();
                            }
                        }
                    }
                }
            }

            imgui_SameLine();
            imgui_TextColored(0.6f, 0.6f, 0.6f, 1f, "[Type] (# who have it) Task Name");

            imgui_Separator();

            // Main content area with left/right split
            float availX = imgui_GetContentRegionAvailX();
            float availY = imgui_GetContentRegionAvailY();
            float leftPaneWidth = Math.Max(200f, Math.Min(300f, availX * 0.30f));

            // Left pane - Task list (resizable)
            using (var leftChild = ImGUIChild.Aquire())
            {
                if (leftChild.BeginChild("AdvView_TaskList", leftPaneWidth, availY - 10f,
                    (int)(ImGuiChildFlags.Borders | ImGuiChildFlags.ResizeX), 0))
                {
                    RenderAdvancedView_TaskList();
                }
            }

            imgui_SameLine();

            // Right pane - Task details (vertically split)
            float rightPaneWidth = availX - leftPaneWidth - 15f;
            using (var rightChild = ImGUIChild.Aquire())
            {
                if (rightChild.BeginChild("AdvView_Details", rightPaneWidth, availY - 10f,
                    (int)ImGuiChildFlags.Borders, 0))
                {
                    RenderAdvancedView_RightPane();
                }
            }
        }

        private static void RenderAdvancedView_TaskList()
        {
            imgui_TextColored(0.9f, 0.9f, 0.5f, 1f, "Tasks");
            imgui_Separator();

            if (_advAllTasks.Count == 0)
            {
                imgui_TextColored(0.6f, 0.6f, 0.6f, 1f, "No tasks found.");
                return;
            }

            foreach (var entry in _advAllTasks)
            {
                // Filter by peer if needed
                if (_advSelectedPeerFilter == "Self")
                {
                    if (entry.LocalTask == null) continue;
                }
                else if (_advSelectedPeerFilter != "All Peers")
                {
                    // Check if this peer has the task
                    var peer = _peerTasks.FirstOrDefault(p =>
                        string.Equals(p.Name, _advSelectedPeerFilter, StringComparison.OrdinalIgnoreCase));
                    if (peer == null) continue;
                    bool peerHasTask = peer.Tasks?.Any(t =>
                        string.Equals(BuildTaskComparisonKey(t.Title ?? "", t.Type ?? ""), entry.Key, StringComparison.OrdinalIgnoreCase)) ?? false;
                    if (!peerHasTask) continue;
                }

                bool isSelected = string.Equals(_advSelectedTaskKey, entry.Key, StringComparison.OrdinalIgnoreCase);

                // Count how many peers have this task
                int peerCount = 0;
                foreach (var peer in _peerTasks)
                {
                    if (peer.Tasks?.Any(t =>
                        string.Equals(BuildTaskComparisonKey(t.Title ?? "", t.Type ?? ""), entry.Key, StringComparison.OrdinalIgnoreCase)) ?? false)
                    {
                        peerCount++;
                    }
                }
                bool selfHas = entry.LocalTask != null;
                int totalCount = selfHas ? peerCount + 1 : peerCount;

                // Format: [Type] (count) Task Name
                string typePrefix = string.IsNullOrEmpty(entry.Type) ? "" : $"[{entry.Type}] ";
                string label = $"{typePrefix}({totalCount}) {entry.Title}";

                if (imgui_Selectable($"{label}##{entry.Key}", isSelected))
                {
                    _advSelectedTaskKey = entry.Key;
                    _advSelectedObjectiveSource = null; // Reset to default when selecting new task
                }
            }
        }

        private static void RenderAdvancedView_RightPane()
        {
            if (string.IsNullOrEmpty(_advSelectedTaskKey))
            {
                imgui_TextColored(0.6f, 0.6f, 0.6f, 1f, "Select a task from the list.");
                return;
            }

            var selectedEntry = _advAllTasks.FirstOrDefault(e =>
                string.Equals(e.Key, _advSelectedTaskKey, StringComparison.OrdinalIgnoreCase));

            if (selectedEntry == null)
            {
                imgui_TextColored(0.6f, 0.6f, 0.6f, 1f, "Task not found.");
                return;
            }

            float availX = imgui_GetContentRegionAvailX();
            float availY = imgui_GetContentRegionAvailY();
            float topPaneHeight = Math.Max(150f, (availY - 10f) * 0.45f);
            float bottomPaneHeight = availY - topPaneHeight - 10f;

            // Top section - Task objectives
            using (var topChild = ImGUIChild.Aquire())
            {
                if (topChild.BeginChild("AdvView_Objectives", availX, topPaneHeight,
                    (int)ImGuiChildFlags.Borders, 0))
                {
                    RenderAdvancedView_Objectives(selectedEntry);
                }
            }

            // Bottom section - Peer status
            using (var bottomChild = ImGUIChild.Aquire())
            {
                if (bottomChild.BeginChild("AdvView_PeerStatus", availX, bottomPaneHeight,
                    (int)ImGuiChildFlags.Borders, 0))
                {
                    RenderAdvancedView_PeerStatus(selectedEntry);
                }
            }
        }

        private static void RenderAdvancedView_Objectives(AdvancedTaskEntry entry)
        {
            string header = string.IsNullOrEmpty(entry.Type)
                ? entry.Title
                : $"{entry.Title} [{entry.Type}]";
            imgui_TextColored(0.9f, 0.9f, 0.5f, 1f, header);

            // Show whose objectives we're viewing
            if (string.IsNullOrEmpty(_advSelectedObjectiveSource))
            {
                if (entry.LocalTask != null)
                {
                    imgui_SameLine();
                    imgui_TextColored(0.5f, 1f, 0.5f, 1f, $"({E3.CurrentName})");
                }
            }
            else
            {
                imgui_SameLine();
                imgui_TextColored(0.65f, 0.85f, 1f, 1f, $"({_advSelectedObjectiveSource})");
            }

            imgui_Separator();

            // Determine which objectives to show based on _advSelectedObjectiveSource
            List<TaskObjectiveSnapshot> localObjectives = null;
            List<TaskWireObjective> peerObjectives = null;
            string sourceName = null;

            if (string.IsNullOrEmpty(_advSelectedObjectiveSource))
            {
                // Default: show local first, then fall back to any peer
                localObjectives = entry.LocalTask?.Objectives;
                if (localObjectives == null || localObjectives.Count == 0)
                {
                    // Find a peer that has this task with objectives
                    foreach (var peer in _peerTasks)
                    {
                        var peerTask = peer.Tasks?.FirstOrDefault(t =>
                            string.Equals(BuildTaskComparisonKey(t.Title ?? "", t.Type ?? ""), entry.Key, StringComparison.OrdinalIgnoreCase));
                        if (peerTask?.Objectives != null && peerTask.Objectives.Count > 0)
                        {
                            peerObjectives = peerTask.Objectives;
                            sourceName = peer.Name;
                            break;
                        }
                    }
                }
            }
            else
            {
                // Show specific peer's objectives
                var selectedPeer = _peerTasks.FirstOrDefault(p =>
                    string.Equals(p.Name, _advSelectedObjectiveSource, StringComparison.OrdinalIgnoreCase));
                if (selectedPeer != null)
                {
                    var peerTask = selectedPeer.Tasks?.FirstOrDefault(t =>
                        string.Equals(BuildTaskComparisonKey(t.Title ?? "", t.Type ?? ""), entry.Key, StringComparison.OrdinalIgnoreCase));
                    if (peerTask?.Objectives != null && peerTask.Objectives.Count > 0)
                    {
                        peerObjectives = peerTask.Objectives;
                        sourceName = selectedPeer.Name;
                    }
                }
            }

            if (localObjectives != null && localObjectives.Count > 0)
            {
                // Render local objectives
                foreach (var obj in localObjectives)
                {
                    var color = obj.IsComplete ? CompletedColor : ActiveColor;
                    string icon = obj.IsComplete ? FA_CHECK : FA_TIMES;
                    string optional = obj.Optional ? " (Optional)" : "";

                    imgui_TextColored(color.R, color.G, color.B, 1f, icon);
                    imgui_SameLine(0f, 6f);

                    string statusText = string.IsNullOrWhiteSpace(obj.Status) ? "" : $" [{obj.Status}]";
                    imgui_Text($"{obj.Index}. {obj.Instruction}{optional}{statusText}");

                    if (!string.IsNullOrWhiteSpace(obj.Zone))
                    {
                        imgui_SameLine();
                        imgui_TextColored(0.6f, 0.6f, 0.6f, 1f, $"({obj.Zone})");
                    }
                }
            }
            else if (peerObjectives != null && peerObjectives.Count > 0)
            {
                foreach (var obj in peerObjectives)
                {
                    var color = obj.IsComplete ? CompletedColor : ActiveColor;
                    string icon = obj.IsComplete ? FA_CHECK : FA_TIMES;
                    string optional = obj.Optional ? " (Optional)" : "";

                    imgui_TextColored(color.R, color.G, color.B, 1f, icon);
                    imgui_SameLine(0f, 6f);

                    string statusText = string.IsNullOrWhiteSpace(obj.Status) ? "" : $" [{obj.Status}]";
                    imgui_Text($"{obj.Index}. {obj.Instruction}{optional}{statusText}");

                    if (!string.IsNullOrWhiteSpace(obj.Zone))
                    {
                        imgui_SameLine();
                        imgui_TextColored(0.6f, 0.6f, 0.6f, 1f, $"({obj.Zone})");
                    }
                }
            }
            else
            {
                imgui_TextColored(0.6f, 0.6f, 0.6f, 1f, "No objective details available.");
            }
        }

        private static void RenderAdvancedView_PeerStatus(AdvancedTaskEntry entry)
        {
            imgui_TextColored(0.65f, 0.85f, 1f, 1f, "Who Has This Task");
            imgui_Separator();

            using (var table = ImGUITable.Aquire())
            {
                int tableFlags = (int)(ImGuiTableFlags.ImGuiTableFlags_RowBg |
                                       ImGuiTableFlags.ImGuiTableFlags_BordersInner |
                                       ImGuiTableFlags.ImGuiTableFlags_BordersOuter |
                                       ImGuiTableFlags.ImGuiTableFlags_Resizable |
                                       ImGuiTableFlags.ImGuiTableFlags_ScrollY);

                if (!table.BeginTable("AdvView_PeerTable", 3, tableFlags, 0f, 0f))
                {
                    return;
                }

                imgui_TableSetupColumn("Character", (int)ImGuiTableColumnFlags.ImGuiTableColumnFlags_WidthFixed, 120f);
                imgui_TableSetupColumn("Current Step", (int)ImGuiTableColumnFlags.ImGuiTableColumnFlags_WidthStretch, 200f);
                imgui_TableSetupColumn("Progress", (int)ImGuiTableColumnFlags.ImGuiTableColumnFlags_WidthFixed, 100f);
                imgui_TableHeadersRow();

                // Self first
                if (entry.LocalTask != null)
                {
                    var task = entry.LocalTask;
                    imgui_TableNextRow();

                    imgui_TableNextColumn();
                    bool isSelfSelected = string.IsNullOrEmpty(_advSelectedObjectiveSource);
                    if (isSelfSelected)
                    {
                        imgui_TextColored(0.5f, 1f, 0.5f, 1f, $"> {E3.CurrentName} (Self)");
                    }
                    else
                    {
                        imgui_TextColored(0.5f, 1f, 0.5f, 1f, $"{E3.CurrentName} (Self)");
                    }
                    if (imgui_IsItemHovered() && imgui_IsMouseClicked(0))
                    {
                        _advSelectedObjectiveSource = null; // Reset to local
                    }

                    imgui_TableNextColumn();
                    string step = string.IsNullOrEmpty(task.ActiveStep) ? "(No step info)" : task.ActiveStep;
                    imgui_TextWrapped(step);

                    imgui_TableNextColumn();
                    var color = task.IsComplete ? CompletedColor : ActiveColor;
                    string progress = task.IsComplete ? "Complete" : $"{task.CompletedObjectives}/{task.TotalObjectives}";
                    imgui_TextColored(color.R, color.G, color.B, 1f, progress);
                }

                // Peers
                foreach (var peer in _peerTasks)
                {
                    var peerTask = peer.Tasks?.FirstOrDefault(t =>
                        string.Equals(BuildTaskComparisonKey(t.Title ?? "", t.Type ?? ""), entry.Key, StringComparison.OrdinalIgnoreCase));

                    if (peerTask == null) continue;

                    imgui_TableNextRow();

                    imgui_TableNextColumn();
                    bool isPeerSelected = string.Equals(_advSelectedObjectiveSource, peer.Name, StringComparison.OrdinalIgnoreCase);
                    if (isPeerSelected)
                    {
                        imgui_TextColored(0.65f, 0.85f, 1f, 1f, $"> {peer.Name}");
                    }
                    else
                    {
                        imgui_TextColored(0.65f, 0.85f, 1f, 1f, peer.Name);
                    }
                    if (imgui_IsItemHovered() && imgui_IsMouseClicked(0))
                    {
                        _advSelectedObjectiveSource = peer.Name;
                    }

                    imgui_TableNextColumn();
                    string step = string.IsNullOrEmpty(peerTask.ActiveStep) ? "(No step info)" : peerTask.ActiveStep;
                    imgui_TextWrapped(step);

                    imgui_TableNextColumn();
                    var color = peerTask.IsComplete ? CompletedColor : ActiveColor;
                    string progress = peerTask.IsComplete ? "Complete" : $"{peerTask.CompletedObjectives}/{peerTask.TotalObjectives}";
                    imgui_TextColored(color.R, color.G, color.B, 1f, progress);
                }
            }
        }

        #endregion

        private sealed class PeerTaskSummary
        {
            public string Name { get; set; } = string.Empty;
            public long LastUpdate { get; set; }
            public List<TaskWireSummary> Tasks { get; set; } = new List<TaskWireSummary>();
        }

        private sealed class AdvancedTaskEntry
        {
            public string Key { get; set; } = string.Empty;
            public string Title { get; set; } = string.Empty;
            public string Type { get; set; } = string.Empty;
            public TaskSnapshot LocalTask { get; set; } // null if we don't have it locally
        }
    }
}
