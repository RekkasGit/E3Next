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
        private static bool _captureNeeded;
        private static bool _publishNeeded;
        private static bool _isWindowOpen;
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
                    _isWindowOpen = true;
                    _captureNeeded = true;
                    // Request task data from all peers when opening the window
                    RequestTaskDataFromPeers();
                }
                else
                {
                    bool open = imgui_Begin_OpenFlagGet(WindowName);
                    imgui_Begin_OpenFlagSet(WindowName, !open);
                    _isWindowOpen = !open;
                    if (!open) 
                    {
                        _captureNeeded = true;
                        // Request task data from all peers when reopening the window
                        RequestTaskDataFromPeers();
                    }
                }

                _imguiContextReady = true;
            }
            catch (Exception ex)
            {
                _log.Write($"E3 Tasks window error: {ex.Message}", Logging.LogLevels.Error);
                _imguiContextReady = false;
            }
        }

        /// <summary>
        /// Broadcasts a request for task data to all connected peers.
        /// Called when the UI window is opened.
        /// NOTE: This is called from UI thread - must not call MQ directly!
        /// </summary>
        public static void RequestTaskDataFromPeers()
        {
            try
            {
                // Publish a request for all peers to send their task data
                PubServer.AddTopicMessage("E3TasksReq", E3.CurrentName);
                
                // Queue local capture AND publish to happen on game loop thread (Pulse)
                // DO NOT call MQ methods here - it's on UI thread!
                _captureNeeded = true;
                _publishNeeded = true;
            }
            catch (Exception ex)
            {
                _log.Write($"Failed to request task data: {ex.Message}", Logging.LogLevels.Error);
            }
        }

        /// <summary>
        /// Processes a task data request from another peer.
        /// Captures current task data and publishes it.
        /// NOTE: This is called from networking thread - must not call MQ directly!
        /// </summary>
        public static void ProcessTaskDataRequest(string requestingUser)
        {
            try
            {
                // Queue capture AND publish to happen on game loop thread (Pulse)
                // DO NOT call MQ methods here - it's on wrong thread!
                _captureNeeded = true;
                _publishNeeded = true;
            }
            catch (Exception ex)
            {
                _log.Write($"Failed to process task data request: {ex.Message}", Logging.LogLevels.Error);
            }
        }

        /// <summary>
        /// Captures current task data and publishes it to the PubServer.
        /// </summary>
        private static void CaptureAndPublishTaskData()
        {
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

                // Publish task data for peers to consume
                PubServer.AddTopicMessage("E3Tasks", TaskDataCollector.SerializeForWire(_cachedTasks));
            }
            catch (Exception ex)
            {
                _log.Write($"Failed to capture and publish task data: {ex.Message}", Logging.LogLevels.Error);
            }
        }

        /// <summary>
        /// Called from the game pulse (E3.StateUpdates) — safe to call mq.Query here.
        /// Only updates local cached data for UI display.
        /// Publishing happens when _publishNeeded is set (in response to explicit requests).
        /// </summary>
        public static void Pulse()
        {
            // Handle publish requests even when window is closed (for peer requests)
            bool shouldPublish = _publishNeeded;
            _publishNeeded = false;
            
            if (shouldPublish)
            {
                try
                {
                    // Capture and publish task data for peers
                    var snapshot = TaskDataCollector.Capture(MQ, allowDelays: false);
                    PubServer.AddTopicMessage("E3Tasks", TaskDataCollector.SerializeForWire(snapshot));
                }
                catch (Exception ex)
                {
                    _log.Write($"Failed to publish task data: {ex.Message}", Logging.LogLevels.Error);
                }
            }

            // Rest of update only happens if window is open
            if (!_isWindowOpen) return;
            if (!_captureNeeded && !e3util.ShouldCheck(ref _nextRefresh, _refreshInterval)) return;

            _captureNeeded = false;

            try
            {
                // Capture task data - safe to call MQ here (game loop thread)
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

        private static void RenderWindow()
        {
            if (!_imguiContextReady) return;
            if (!imgui_Begin_OpenFlagGet(WindowName)) return;

            PushCurrentTheme();
            try
            {
                imgui_SetNextWindowSizeWithCond(620f, 680f, (int)ImGuiCond.FirstUseEver);

                using (var window = ImGUIWindow.Aquire())
                {
                    int flags = (int)ImGuiWindowFlags.ImGuiWindowFlags_NoCollapse;
                    if (!window.Begin(WindowName, flags))
                    {
                        _isWindowOpen = false;
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
                _captureNeeded = true;
                // Also request fresh data from peers when manually refreshing
                RequestTaskDataFromPeers();
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

                var summaries = TaskDataCollector.DeserializeFromWire(entry.GetData().ToString());

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
                _captureNeeded = true;
                RequestTaskDataFromPeers();
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
                _captureNeeded = true;
                RequestTaskDataFromPeers();
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

                bool isSelected = _advSelectedTaskKey == entry.Key;

                // Count how many peers have this task
                int peerCountWithTask = 0;
                foreach (var peer in _peerTasks)
                {
                    if (peer.Tasks?.Any(t =>
                        string.Equals(BuildTaskComparisonKey(t.Title ?? "", t.Type ?? ""), entry.Key, StringComparison.OrdinalIgnoreCase)) ?? false)
                    {
                        peerCountWithTask++;
                    }
                }
                bool localHasTask = entry.LocalTask != null;
                int totalCount = (localHasTask ? 1 : 0) + peerCountWithTask;

                string label = $"[{entry.Type ?? "???"}] ({totalCount}) {entry.Title}";

                if (imgui_Selectable(label, isSelected))
                {
                    _advSelectedTaskKey = entry.Key;
                    _advSelectedObjectiveSource = null; // reset to auto
                }
            }
        }

        private static void RenderAdvancedView_RightPane()
        {
            var entry = _advAllTasks.FirstOrDefault(e => e.Key == _advSelectedTaskKey);
            if (entry == null)
            {
                imgui_TextColored(0.6f, 0.6f, 0.6f, 1f, "Select a task to view details.");
                return;
            }

            // Header
            imgui_TextColored(0.9f, 0.9f, 0.5f, 1f, entry.Title);
            imgui_TextColored(0.6f, 0.6f, 0.6f, 1f, $"Type: {entry.Type ?? "Unknown"}");
            imgui_Separator();

            // Peer selector for objectives
            imgui_Text("View objectives from:");
            imgui_SameLine();
            imgui_SetNextItemWidth(150f);

            using (var combo = ImGUICombo.Aquire())
            {
                string preview = _advSelectedObjectiveSource ?? "Auto (Self if available)";
                if (combo.BeginCombo("##ObjectiveSource", preview))
                {
                    if (imgui_Selectable("Auto (Self if available)", _advSelectedObjectiveSource == null))
                    {
                        _advSelectedObjectiveSource = null;
                    }

                    if (entry.LocalTask != null)
                    {
                        if (imgui_Selectable($"Self ({E3.CurrentName})", _advSelectedObjectiveSource == $"Self:{E3.CurrentName}"))
                        {
                            _advSelectedObjectiveSource = $"Self:{E3.CurrentName}";
                        }
                    }

                    foreach (var peer in _peerTasks)
                    {
                        var peerTask = peer.Tasks?.FirstOrDefault(t =>
                            string.Equals(BuildTaskComparisonKey(t.Title ?? "", t.Type ?? ""), entry.Key, StringComparison.OrdinalIgnoreCase));
                        if (peerTask != null)
                        {
                            if (imgui_Selectable(peer.Name, _advSelectedObjectiveSource == peer.Name))
                            {
                                _advSelectedObjectiveSource = peer.Name;
                            }
                        }
                    }
                }
            }

            imgui_Separator();

            // Find the task to display objectives from
            TaskSnapshot taskToShow = null;
            if (_advSelectedObjectiveSource == null)
            {
                // Auto: prefer self, then first peer
                taskToShow = entry.LocalTask;
                if (taskToShow == null)
                {
                    var peerWithTask = _peerTasks.FirstOrDefault(p =>
                        p.Tasks?.Any(t => string.Equals(BuildTaskComparisonKey(t.Title ?? "", t.Type ?? ""), entry.Key, StringComparison.OrdinalIgnoreCase)) ?? false);
                    if (peerWithTask != null)
                    {
                        var peerTask = peerWithTask.Tasks.First(t =>
                            string.Equals(BuildTaskComparisonKey(t.Title ?? "", t.Type ?? ""), entry.Key, StringComparison.OrdinalIgnoreCase));
                        // Convert TaskWireSummary to TaskSnapshot for display
                        taskToShow = ConvertWireSummaryToSnapshot(peerTask);
                    }
                }
            }
            else if (_advSelectedObjectiveSource.StartsWith("Self:"))
            {
                taskToShow = entry.LocalTask;
            }
            else
            {
                var peer = _peerTasks.FirstOrDefault(p => p.Name == _advSelectedObjectiveSource);
                if (peer != null)
                {
                    var peerTask = peer.Tasks?.FirstOrDefault(t =>
                        string.Equals(BuildTaskComparisonKey(t.Title ?? "", t.Type ?? ""), entry.Key, StringComparison.OrdinalIgnoreCase));
                    if (peerTask != null)
                    {
                        taskToShow = ConvertWireSummaryToSnapshot(peerTask);
                    }
                }
            }

            if (taskToShow == null)
            {
                imgui_TextColored(0.6f, 0.6f, 0.6f, 1f, "No objective data available from selected source.");
                return;
            }

            // Status
            var statusColor = taskToShow.IsComplete ? CompletedColor : ActiveColor;
            imgui_TextColored(statusColor.R, statusColor.G, statusColor.B, 1f,
                $"Status: {(taskToShow.IsComplete ? "Complete" : "In Progress")}");

            if (!string.IsNullOrWhiteSpace(taskToShow.ActiveStep))
            {
                imgui_TextColored(0.75f, 0.9f, 1f, 1f, "Current Step:");
                imgui_TextWrapped(taskToShow.ActiveStep);
            }

            // Peer summary table
            imgui_Separator();
            imgui_TextColored(0.75f, 0.9f, 1f, 1f, "Who has this task:");
            RenderAdvancedView_PeerSummaryTable(entry);

            // Objectives
            if (taskToShow.Objectives.Count > 0)
            {
                imgui_Separator();
                RenderObjectivesTable(taskToShow);
            }
        }

        private static void RenderAdvancedView_PeerSummaryTable(AdvancedTaskEntry entry)
        {
            int tableFlags = (int)(ImGuiTableFlags.ImGuiTableFlags_RowBg |
                                   ImGuiTableFlags.ImGuiTableFlags_BordersInner |
                                   ImGuiTableFlags.ImGuiTableFlags_BordersOuter);

            using (var table = ImGUITable.Aquire())
            {
                if (!table.BeginTable("PeerSummary", 3, tableFlags, 0f, 0f))
                    return;

                imgui_TableSetupColumn("Peer", (int)ImGuiTableColumnFlags.ImGuiTableColumnFlags_WidthStretch, 150f);
                imgui_TableSetupColumn("Status", (int)ImGuiTableColumnFlags.ImGuiTableColumnFlags_WidthFixed, 100f);
                imgui_TableSetupColumn("Progress", (int)ImGuiTableColumnFlags.ImGuiTableColumnFlags_WidthFixed, 100f);
                imgui_TableHeadersRow();

                // Self
                if (entry.LocalTask != null)
                {
                    imgui_TableNextRow();
                    imgui_TableNextColumn();
                    imgui_TextColored(0.65f, 0.85f, 1f, 1f, E3.CurrentName);
                    imgui_TableNextColumn();
                    var statusColor = entry.LocalTask.IsComplete ? CompletedColor : ActiveColor;
                    imgui_TextColored(statusColor.R, statusColor.G, statusColor.B, 1f,
                        entry.LocalTask.IsComplete ? "Complete" : "In Progress");
                    imgui_TableNextColumn();
                    int completed = entry.LocalTask.Objectives.Count(o => o.IsComplete);
                    imgui_Text($"{completed}/{entry.LocalTask.Objectives.Count}");
                }

                // Peers
                foreach (var peer in _peerTasks)
                {
                    var peerTask = peer.Tasks?.FirstOrDefault(t =>
                        string.Equals(BuildTaskComparisonKey(t.Title ?? "", t.Type ?? ""), entry.Key, StringComparison.OrdinalIgnoreCase));
                    if (peerTask != null)
                    {
                        imgui_TableNextRow();
                        imgui_TableNextColumn();
                        imgui_Text(peer.Name);
                        imgui_TableNextColumn();
                        var statusColor = peerTask.IsComplete ? CompletedColor : ActiveColor;
                        imgui_TextColored(statusColor.R, statusColor.G, statusColor.B, 1f,
                            peerTask.IsComplete ? "Complete" : "In Progress");
                        imgui_TableNextColumn();
                        imgui_Text($"{peerTask.CompletedObjectives}/{peerTask.TotalObjectives}");
                    }
                }
            }
        }

        private static TaskSnapshot ConvertWireSummaryToSnapshot(TaskWireSummary wire)
        {
            var snapshot = new TaskSnapshot
            {
                Slot = wire.Slot,
                Title = wire.Title,
                Type = wire.Type,
                ActiveStep = wire.ActiveStep,
                TimerDisplay = wire.TimerDisplay,
                TimerSeconds = 0
            };

            foreach (var obj in wire.Objectives)
            {
                // IsComplete is computed from Status, so set Status to "Done" if complete
                string status = obj.IsComplete ? "Done" : obj.Status;
                snapshot.Objectives.Add(new TaskObjectiveSnapshot
                {
                    Index = obj.Index,
                    Instruction = obj.Instruction,
                    Status = status,
                    Zone = obj.Zone,
                    Optional = obj.Optional
                });
            }

            return snapshot;
        }

        #endregion

        private class PeerTaskSummary
        {
            public string Name { get; set; }
            public long LastUpdate { get; set; }
            public List<TaskWireSummary> Tasks { get; set; }
        }

        private class AdvancedTaskEntry
        {
            public string Key { get; set; }
            public string Title { get; set; }
            public string Type { get; set; }
            public TaskSnapshot LocalTask { get; set; }
        }
    }
}
