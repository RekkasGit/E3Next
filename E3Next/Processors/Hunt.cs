using E3Core.Utility;
using E3Core.Data;
using E3Core.Settings;
using MonoCore;
using System.IO;
using IniParser;
using IniParser.Model;
using System;
using System.Collections.Generic;
using System.Linq;

namespace E3Core.Processors
{
    public enum HuntState
    {
        Disabled,
        Safety,
        Scanning,
        NavigatingToTarget,
        PullingTarget,
        InCombat,
        WaitingForLoot,
        NavigatingToCamp,
        Paused,
        ManualControl
    }

    public static class HuntStateMachine
    {
        private static readonly Logging _log = E3.Log;
        private static readonly IMQ MQ = E3.MQ;
        private static HuntState _currentState = HuntState.Disabled;
        private static HuntState _previousState = HuntState.Disabled;
        private static long _stateEnteredAt = 0;
        private static bool _navigationOwned = false;
        private static string _stateReason = string.Empty;

        public static HuntState CurrentState => _currentState;
        public static string StateReason => _stateReason;
        public static long StateElapsedMs => Core.StopWatch.ElapsedMilliseconds - _stateEnteredAt;

        public static bool RequestNavigationControl(string requester)
        {
            if (_navigationOwned)
            {
                _log.Write($"Hunt: Navigation control denied to {requester} (already owned)");
                return false;
            }
            _navigationOwned = true;
            _log.Write($"Hunt: Navigation control granted to {requester}");
            return true;
        }

        public static void ReleaseNavigationControl(string releaser)
        {
            if (!_navigationOwned) return;
            _navigationOwned = false;
            _log.Write($"Hunt: Navigation control released by {releaser}");
            try { MQ.Cmd("/nav stop"); } catch { }
            try { Hunt.ResetNavTargetTracking(); } catch { }
        }

        public static bool IsNavigationOwned => _navigationOwned;

        public static void TransitionTo(HuntState newState, string reason = "")
        {
            if (_currentState == newState && reason == _stateReason) return;

            _log.Write($"Hunt: State {_currentState} -> {newState} ({reason})");
            try { Hunt.DebugLog($"STATE: {_currentState} -> {newState} ({reason})"); } catch { }

            // Exit current state
            ExitState(_currentState);

            _previousState = _currentState;
            _currentState = newState;
            _stateEnteredAt = Core.StopWatch.ElapsedMilliseconds;
            _stateReason = reason;

            // Enter new state
            EnterState(newState);

            // Update Hunt status for UI
            Hunt.Status = GetStatusString();
        }

        private static void ExitState(HuntState state)
        {
            switch (state)
            {
                case HuntState.NavigatingToTarget:
                case HuntState.NavigatingToCamp:
                    if (_navigationOwned)
                        ReleaseNavigationControl($"ExitState({state})");
                    break;
            }
        }

        private static void EnterState(HuntState state)
        {
            switch (state)
            {
                case HuntState.Disabled:
                    Hunt.Go = false;
                    if (_navigationOwned)
                        ReleaseNavigationControl("EnterState(Disabled)");
                    break;
                case HuntState.Paused:
                    if (_navigationOwned)
                        ReleaseNavigationControl("EnterState(Paused)");
                    break;
            }
        }

        private static string GetStatusString()
        {
            switch (_currentState)
            {
                case HuntState.Disabled: return "Disabled";
                case HuntState.Safety: return "Safety disabled";
                case HuntState.Scanning: return "Scanning";
                case HuntState.NavigatingToTarget: return $"Navigating to {Hunt.TargetID}";
                case HuntState.PullingTarget: return $"Pulling {Hunt.TargetName}";
                case HuntState.InCombat: return "In Combat";
                case HuntState.WaitingForLoot: return !string.IsNullOrEmpty(_stateReason) ? _stateReason : "Waiting for loot";
                case HuntState.NavigatingToCamp: return "Returning to camp";
                // If we're "Paused" due to a specific reason (e.g., waiting on peers), surface that
                // so the UI reflects an active-but-waiting state rather than just "Paused".
                case HuntState.Paused:
                    return !string.IsNullOrEmpty(_stateReason) ? _stateReason : "Paused";
                case HuntState.ManualControl: return "Manual control";
                default: return _currentState.ToString();
            }
        }
    }

    // Hunt mode: lightweight mob finder + navigator inspired by PocketFarm
    public static class Hunt
    {
        private static readonly Logging _log = E3.Log;
        private static readonly IMQ MQ = E3.MQ;
        private static readonly ISpawns _spawns = E3.Spawns;

        [ExposedData("Hunt", "Enabled")]
        public static bool Enabled = false;

        [ExposedData("Hunt", "Go")]
        public static bool Go = false; // go/pause toggle

        [ExposedData("Hunt", "Radius")]
        public static int Radius = 500;

        [ExposedData("Hunt", "ZRadius")]
        public static int ZRadius = 300;

        // 'Pull' are inclusion filters. Separate multiple by '|'. "ALL" means no filter.
        [ExposedData("Hunt", "PullFilters")]
        public static string PullFilters = "ALL";

        // 'Ignore' are exclusion filters. Separate multiple by '|'. "NONE" means none.
        [ExposedData("Hunt", "IgnoreFilters")]
        public static string IgnoreFilters = "NONE";

        [ExposedData("Hunt", "TargetID")]
        public static int TargetID = 0;

        [ExposedData("Hunt", "Status")]
        public static string Status = string.Empty;
        // Back-compat shim: expose HuntStateMachine.CurrentState via Hunt.CurrentState
        public static HuntState CurrentState => HuntStateMachine.CurrentState;

        // Cached target name for UI (avoid MQ queries on UI thread)
        [ExposedData("Hunt", "TargetName")]
        public static string TargetName = string.Empty;

        // Pulling configuration
        [ExposedData("Hunt", "PullMethod")] // None|Ranged|Spell|Item|AA|Disc|Attack
        public static string PullMethod = "None";
        [ExposedData("Hunt", "PullSpell")] public static string PullSpell = string.Empty;
        [ExposedData("Hunt", "PullItem")] public static string PullItem = string.Empty;
        [ExposedData("Hunt", "PullAA")] public static string PullAA = string.Empty;
        [ExposedData("Hunt", "PullDisc")] public static string PullDisc = string.Empty;
        [ExposedData("Hunt", "AutoAssistAtMelee")] public static bool AutoAssistAtMelee = true;

        // SmartLoot coordination
        [ExposedData("Hunt", "SmartLootState")]
        public static string SmartLootState = "Unknown";
        [ExposedData("Hunt", "SmartLootActive")]
        public static bool SmartLootActive = false; // derived activity flag for peers
        [ExposedData("Hunt", "SmartLootMode")]
        public static string SmartLootMode = "Disabled";
        // Telemetry updates pulled from SmartLoot TLO
        private static bool _slIsProcessing = false;
        private static bool _slSafeToLoot = true;
        private static int _slCorpseCount = 0;
        private static bool _slHasNewCorpses = false;
        private static bool _slIsPeerTriggered = false;
        private static string _slState = "Unknown";
        private static string _slMode = "Disabled";

        // Adjustable loot wait (ms)
        [ExposedData("Hunt", "LootMinWaitMs")] public static int LootMinWaitMs = 2000;
        [ExposedData("Hunt", "LootMaxWaitMs")] public static int LootMaxWaitMs = 10000;

        private static T Q<T>(string expr, T fallback = default)
        {
            try { return MQ.Query<T>(expr); } catch { return fallback; }
        }

        private static void UpdateSmartLootTelemetry()
        {
            _slState = Q("${SmartLoot.State}", "Unknown");
            _slMode = Q("${SmartLoot.Mode}", "Disabled");
            _slIsProcessing = Q("${SmartLoot.IsProcessing}", false);
            _slSafeToLoot = Q("${SmartLoot.SafeToLoot}", true);
            _slCorpseCount = Q("${SmartLoot.CorpseCount}", 0);
            _slHasNewCorpses = Q("${SmartLoot.HasNewCorpses}", false);
            _slIsPeerTriggered = Q("${SmartLoot.IsPeerTriggered}", false);

            // Publish to shared-data for peers / your /hunt smartloot command
            SmartLootState = _slState;
            SmartLootMode = _slMode;
            SmartLootActive = _slIsProcessing || _slCorpseCount > 0;
        }


        // Optional camp point
        [ExposedData("Hunt", "CampOn")]
        public static bool CampOn = false;
        
        // When true, scan from the player's current position after each pull (rgmercs HuntFromPlayer behavior)
        [ExposedData("Hunt", "HuntFromPlayer")]
        public static bool HuntFromPlayer = false;

        [ExposedData("Hunt", "CampX")]
        public static int CampX = 0;
        [ExposedData("Hunt", "CampY")]
        public static int CampY = 0;
        [ExposedData("Hunt", "CampZ")]
        public static int CampZ = 0;

        private static long _nextTickAt = 0;
        private static readonly int _tickIntervalMs = 200; // snappy responsiveness
        private static long _nextScanAt = 0;
        private static readonly int _scanIntervalMs = 350; // faster scans for snappier target acquisition (rgmercs-like)
        private static long _nextPullAt = 0;
        private static readonly int _pullCooldownMs = 1800; // slightly faster retry cadence
        private static long _nextNavAt = 0;
        private static readonly int _navCooldownMs = 350; // more responsive navigation updates
        private static int _navTargetID = 0; // last spawn id we commanded /nav towards
        private static long _lastAssistCmdAt = 0;
        private static readonly int _assistCmdCooldownMs = 2000; // avoid assist spam
        // Track the current nav command target
        internal static void ResetNavTargetTracking() { _navTargetID = 0; }
        private static void BeginLootWait(string reason)
        {
            _waitingForLoot = true;
            _lootWaitStartMs = Core.StopWatch.ElapsedMilliseconds;
            HuntStateMachine.TransitionTo(HuntState.WaitingForLoot, reason);
            // Let SmartLoot drive; drop nav
            if (HuntStateMachine.IsNavigationOwned)
                HuntStateMachine.ReleaseNavigationControl("BeginLootWait");
        }

        private static bool LootWaitComplete()
        {
            UpdateSmartLootTelemetry();

            long elapsed = Core.StopWatch.ElapsedMilliseconds - _lootWaitStartMs;
            bool minElapsed = elapsed >= Math.Max(0, LootMinWaitMs);
            bool maxElapsed = elapsed >= Math.Max(LootMinWaitMs, LootMaxWaitMs);

            // Done if SmartLoot is idle and no corpses nearby and we've honored the minimum dwell
            bool slIdle = !_slIsProcessing && _slCorpseCount == 0;

            return (minElapsed && slIdle) || maxElapsed;
        }

        // Track repeated no-path detections for the current target
        private static int _noPathTarget = 0;
        private static int _noPathAttempts = 0;
        private static void ResetNoPathTracking(int forTargetId = 0)
        {
            _noPathTarget = forTargetId;
            _noPathAttempts = 0;
        }

        // Loot wait handling: pause scanning after combat ends until SmartLoot finishes
        private static int _lastXTargetCount = 0;
        private static bool _waitingForLoot = false;
        private static long _lootWaitStartMs = 0;
        private static readonly int _lootMinWaitMs = 2000;   // always wait at least 2s after XTarget clears
        private static readonly int _lootWaitFallbackMs = 10000; // fallback max wait

        // Helper: detect when XTarget just dropped to 0 since last tick
        private static bool XTargetJustCleared()
        {
            int current = 0;
            try { current = MQ.Query<int>("${Me.XTarget}"); } catch { current = 0; }
            return _lastXTargetCount > 0 && current == 0;
        }

        // Zone-specific ignore list (zone -> HashSet of mob names)
        private static readonly Dictionary<string, HashSet<string>> _zoneIgnoreLists = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
        private static string _currentZone = string.Empty;
        private static string _ignoreListPath = string.Empty;
        private static string _huntSettingsPath = string.Empty;

        // Temporary per-spawn ignore to avoid repeatedly attempting stuck/unreachable mobs (rgmercs-style PullIgnoreTime)
        [ExposedData("Hunt", "MaxPathRange")] public static int MaxPathRange = 1000; // 0 disables path length limit
        [ExposedData("Hunt", "PullIgnoreTimeSec")] public static int PullIgnoreTimeSec = 15;
        [ExposedData("Hunt", "TempIgnoreDurationSec")] public static int TempIgnoreDurationSec = 60; // temp ignore duration used by UI actions
        [ExposedData("Hunt", "RangedApproachFactor")] public static double RangedApproachFactor = 0.5; // portion of max ranged pull distance to approach
        // Preferred approach distance for non-ranged pulls (spell/item/aa/disc)
        [ExposedData("Hunt", "PullApproachDistance")] public static int PullApproachDistance = 60;
        // Detect stuck targets with no available nav path and move on
        [ExposedData("Hunt", "NoPathMaxAttempts")] public static int NoPathMaxAttempts = 3; // consecutive no-path checks before we give up
        [ExposedData("Hunt", "NoPathIgnoreDurationSec")] public static int NoPathIgnoreDurationSec = 30; // temporary ignore when stuck
        private static readonly Dictionary<int, long> _tempIgnoreUntil = new Dictionary<int, long>();
        private static long _pullAttemptStartMs = 0;

        // Recent scan candidates for UI (no MQ on UI thread)
        // Debug log for UI window
        private struct DebugEntry { public long Ts; public string Msg; }
        private static readonly List<DebugEntry> _debug = new List<DebugEntry>(256);
        private static readonly object _debugLock = new object();
        [ExposedData("Hunt", "DebugEnabled")] public static bool DebugEnabled = true;

        public static void DebugLog(string msg)
        {
            if (!DebugEnabled || string.IsNullOrEmpty(msg)) return;
            try
            {
                var e = new DebugEntry { Ts = Core.StopWatch.ElapsedMilliseconds, Msg = msg };
                lock (_debugLock)
                {
                    _debug.Add(e);
                    if (_debug.Count > 400) _debug.RemoveRange(0, _debug.Count - 400);
                }
            }
            catch { }
        }

        public static List<(long ts, string msg)> GetDebugLogSnapshot(int max = 200)
        {
            var list = new List<(long, string)>(Math.Min(max, 400));
            try
            {
                lock (_debugLock)
                {
                    int start = Math.Max(0, _debug.Count - Math.Max(1, max));
                    for (int i = start; i < _debug.Count; i++)
                    {
                        list.Add((_debug[i].Ts, _debug[i].Msg ?? string.Empty));
                    }
                }
            }
            catch { }
            return list;
        }

        private struct HuntCandidate
        {
            public int ID;
            public string Name;
            public int Level;
            public double Distance;
            public double PathLen;
            public string Loc;
            public string Con;
        }
        private static readonly List<HuntCandidate> _lastCandidates = new List<HuntCandidate>();
        private static long _candidatesUpdatedAt = 0;

        public static List<(int id, string name, int level, double distance, double pathLen, string loc, string con)> GetCandidatesSnapshot()
        {
            // Return a shallow copy suitable for UI consumption
            var list = new List<(int, string, int, double, double, string, string)>(_lastCandidates.Count);
            foreach (var c in _lastCandidates)
            {
                list.Add((c.ID, c.Name ?? string.Empty, c.Level, c.Distance, c.PathLen, c.Loc ?? string.Empty, c.Con ?? string.Empty));
            }
            return list;
        }

        public static void ClearDebugLog()
        {
            try { lock (_debugLock) { _debug.Clear(); } } catch { }
        }

        public static void ClearTempIgnores()
        {
            _tempIgnoreUntil.Clear();
        }

        public static void ForceSetTarget(int id)
        {
            if (id <= 0) return;
            TargetID = id;
            TargetName = GetSpawnName(id);
            _nextNavAt = 0; // allow immediate nav
            HuntStateMachine.TransitionTo(HuntState.NavigatingToTarget, "Forced target");
        }

        public static void TempIgnoreID(int id, int durationSeconds)
        {
            if (id <= 0) return;
            if (durationSeconds <= 0) durationSeconds = TempIgnoreDurationSec;
            _tempIgnoreUntil[id] = Core.StopWatch.ElapsedMilliseconds + durationSeconds * 1000L;
        }

        // Expose timing info for UI
        public static int GetMsUntilNextScan()
        {
            long now = Core.StopWatch.ElapsedMilliseconds;
            return (int)Math.Max(0, _nextScanAt - now);
        }
        public static int GetMsUntilNextNav()
        {
            long now = Core.StopWatch.ElapsedMilliseconds;
            return (int)Math.Max(0, _nextNavAt - now);
        }
        public static int GetMsUntilNextPull()
        {
            long now = Core.StopWatch.ElapsedMilliseconds;
            return (int)Math.Max(0, _nextPullAt - now);
        }

        [SubSystemInit]
        public static void Hunt_Init()
        {
            RegisterCommands();
            Status = "Idle";
            try
            {
                _ignoreListPath = E3Core.Settings.BaseSettings.GetSettingsFilePath("Hunt Ignore List.txt");
                LoadIgnoreList();
                _huntSettingsPath = E3Core.Settings.BaseSettings.GetSettingsFilePath("Hunt Settings.ini");
                LoadHuntSettings();
                // Prime cached zone name early (avoids UI doing MQ queries)
                UpdateCurrentZoneCached();
            }
            catch { }
        }

        private static void RegisterCommands()
        {
            EventProcessor.RegisterCommand("/hunt", (x) =>
            {
                if (x.args.Count == 0)
                {
                    MQ.Write("Usage: /hunt [go|pause|on|off|radius <n>|zradius <n>|pull <patterns>|ignore <patterns>|ignoreadd [name]|ignorelist|ignoreclear|ignoreallzones|smartloot|zonecheck|camp [set|off]|fromplayer <on|off>|pullmethod <type>|pullspell <name>|pullitem <name>|pullaa <name>|pulldisc <name>|debug]");
                    return;
                }

                var cmd = x.args[0].ToLowerInvariant();
                switch (cmd)
                {
                    case "on":
                        Enabled = true; MQ.Write("Hunt enabled"); break;
                    case "off":
                        Enabled = false; Go = false; 
                        HuntStateMachine.ReleaseNavigationControl("Command: hunt off");
                        MQ.Write("Hunt disabled"); break;
                    case "go":
                        Enabled = true; Go = true; MQ.Write("Hunt: go"); break;
                    case "pause":
                        Go = false; 
                        HuntStateMachine.ReleaseNavigationControl("Command: hunt pause");
                        MQ.Write("Hunt: paused"); break;
                    case "radius":
                        if (x.args.Count > 1 && int.TryParse(x.args[1], out var r)) Radius = Math.Max(10, r);
                        MQ.Write($"Hunt radius = {Radius}");
                        break;
                    case "zradius":
                        if (x.args.Count > 1 && int.TryParse(x.args[1], out var zr)) ZRadius = Math.Max(10, zr);
                        MQ.Write($"Hunt zradius = {ZRadius}");
                        break;
                    case "pull":
                        if (x.args.Count > 1) PullFilters = string.Join(" ", x.args.GetRange(1, x.args.Count - 1));
                        MQ.Write($"Hunt PullFilters = {PullFilters}");
                        break;
                    case "ignore":
                        if (x.args.Count > 1) IgnoreFilters = string.Join(" ", x.args.GetRange(1, x.args.Count - 1));
                        MQ.Write($"Hunt IgnoreFilters = {IgnoreFilters}");
                        break;
                    case "ignoreadd":
                        {
                            string nm = x.args.Count > 1 ? string.Join(" ", x.args.GetRange(1, x.args.Count - 1)) : MQ.Query<string>("${Target.CleanName}");
                            if (!string.IsNullOrWhiteSpace(nm) && !nm.Equals("NULL", StringComparison.OrdinalIgnoreCase))
                            {
                                if (AddIgnoreName(nm)) MQ.Write($"Added '{nm}' to Hunt ignore list");
                                else MQ.Write($"'{nm}' already in Hunt ignore list");
                            }
                            else MQ.Write("No valid target/name to ignore.");
                        }
                        break;
                    case "ignorelist":
                        {
                            var currentZone = GetCurrentZone();
                            var list = GetIgnoreListSnapshot();
                            MQ.Write($"Hunt ignore list for zone '{currentZone}' contains {list.Count} entries:");
                            foreach (var name in list)
                            {
                                MQ.Write($"  - '{name}'");
                            }
                            
                            // Show summary of all zones
                            var allZones = GetAllZoneIgnoreListsSnapshot();
                            if (allZones.Count > 1)
                            {
                                MQ.Write($"Other zones with ignore entries ({allZones.Count - 1} zones):");
                                foreach (var kvp in allZones)
                                {
                                    if (!kvp.Key.Equals(currentZone, StringComparison.OrdinalIgnoreCase))
                                    {
                                        MQ.Write($"  - {kvp.Key}: {kvp.Value.Count} entries");
                                    }
                                }
                            }
                        }
                        break;
                    case "ignoreclear":
                        {
                            var currentZone = GetCurrentZone();
                            var currentIgnoreList = GetCurrentZoneIgnoreList();
                            int count = currentIgnoreList.Count;
                            currentIgnoreList.Clear();
                            try { SaveIgnoreList(); } catch { }
                            MQ.Write($"Hunt: Cleared {count} entries from ignore list for zone '{currentZone}'");
                        }
                        break;
                    case "ignoreallzones":
                        {
                            var allZones = GetAllZoneIgnoreListsSnapshot();
                            MQ.Write($"Hunt ignore list for all zones ({allZones.Count} zones):");
                            foreach (var kvp in allZones.OrderBy(z => z.Key))
                            {
                                MQ.Write($"  Zone: {kvp.Key} ({kvp.Value.Count} entries)");
                                foreach (var mob in kvp.Value.OrderBy(m => m))
                                {
                                    MQ.Write($"    - '{mob}'");
                                }
                            }
                        }
                        break;
                    case "fromplayer":
                        {
                            bool newVal = HuntFromPlayer;
                            if (x.args.Count > 1)
                            {
                                var v = x.args[1].ToLowerInvariant();
                                if (v == "on" || v == "1" || v == "true") newVal = true;
                                else if (v == "off" || v == "0" || v == "false") newVal = false;
                            }
                            HuntFromPlayer = newVal;
                            MQ.Write($"HuntFromPlayer = {(HuntFromPlayer ? "ON" : "OFF")}");
                            try { SaveHuntPullSettings(); } catch { }
                        }
                        break;
                    case "smartloot":
                        {
                            MQ.Write("Hunt: SmartLoot coordination status:");
                            MQ.Write($"  Local SmartLoot State: {SmartLootState}");
                            MQ.Write($"  Local SmartLoot Mode: {SmartLootMode}");
                            MQ.Write($"  Local SmartLoot Active: {(SmartLootActive ? "1" : "0")}");
                            
                            try
                            {
                                var connectedBots = E3.Bots.BotsConnected();
                                MQ.Write($"  Connected bots: {connectedBots.Count}");
                                
                                foreach (string botName in connectedBots)
                                {
                                    if (string.Equals(botName, E3.CurrentName, StringComparison.OrdinalIgnoreCase))
                                        continue;
                                    
                                    try
                                    {
                                        // Query the shared-data topic directly
                                        string botSmartLootState1 = E3.Bots.Query(botName, "${Data.Hunt.SmartLootState}");
                                        // Backward-compat fallbacks if someone publishes a different key
                                        string botSmartLootState2 = E3.Bots.Query(botName, "${Data.SmartLootState}");
                                        string botSmartLootState3 = E3.Bots.Query(botName, "${Data.Hunt_SmartLootState}");
                                        
                                        // Also show derived active and mode if available
                                        string botSmartLootActive = E3.Bots.Query(botName, "${Data.Hunt.SmartLootActive}");
                                        string botSmartLootMode = E3.Bots.Query(botName, "${Data.Hunt.SmartLootMode}");

                                        MQ.Write($"  {botName}:");
                                        MQ.Write($"    Data.Hunt.SmartLootState: {botSmartLootState1 ?? "NULL"}");
                                        MQ.Write($"    Data.SmartLootState: {botSmartLootState2 ?? "NULL"}");
                                        MQ.Write($"    Data.Hunt_SmartLootState: {botSmartLootState3 ?? "NULL"}");
                                        MQ.Write($"    Data.Hunt.SmartLootActive: {botSmartLootActive ?? "NULL"}");
                                        MQ.Write($"    Data.Hunt.SmartLootMode: {botSmartLootMode ?? "NULL"}");
                                    }
                                    catch (Exception ex)
                                    {
                                        MQ.Write($"  {botName}: Error - {ex.Message}");
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                MQ.Write($"  Error checking bots: {ex.Message}");
                            }
                        }
                        break;
                    case "zonecheck":
                        {
                            try
                            {
                                var connectedBots = E3.Bots.BotsConnected();
                                string currentZone = GetCurrentZone();
                                MQ.Write($"Hunt: Zone check for {E3.CurrentName} in {currentZone}");
                                MQ.Write($"  Connected bots: {connectedBots.Count}");
                                
                                foreach (string botName in connectedBots)
                                {
                                    if (string.Equals(botName, E3.CurrentName, StringComparison.OrdinalIgnoreCase))
                                    {
                                        MQ.Write($"  {botName}: (self) in {currentZone}");
                                        continue;
                                    }
                                    
                                    try
                                    {
                                        string peerZone = E3.Bots.Query(botName, "${Zone.ShortName}");
                                        string peerDead = E3.Bots.Query(botName, "${Me.Dead}");
                                        bool isPeerDead = string.Equals(peerDead, "TRUE", StringComparison.OrdinalIgnoreCase);
                                        
                                        if (string.IsNullOrEmpty(peerZone) || peerZone.Equals("NULL", StringComparison.OrdinalIgnoreCase))
                                        {
                                            MQ.Write($"  {botName}: Zone info unavailable");
                                        }
                                        else if (!string.Equals(peerZone, currentZone, StringComparison.OrdinalIgnoreCase))
                                        {
                                            MQ.Write($"  {botName}: In {peerZone} (different from {currentZone})");
                                        }
                                        else
                                        {
                                            MQ.Write($"  {botName}: In {peerZone} {(isPeerDead ? "(DEAD)" : "")}");
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        MQ.Write($"  {botName}: Error - {ex.Message}");
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                MQ.Write($"  Error checking zone status: {ex.Message}");
                            }
                        }
                        break;
                    case "pullmethod":
                        if (x.args.Count > 1)
                        {
                            var m = x.args[1];
                            if (IsValidPullMethod(m)) PullMethod = NormalizeMethod(m);
                            SaveHuntPullSettings();
                            MQ.Write($"Hunt PullMethod = {PullMethod}");
                        }
                        break;
                    case "pullspell":
                        if (x.args.Count > 1) PullSpell = string.Join(" ", x.args.GetRange(1, x.args.Count - 1));
                        SaveHuntPullSettings();
                        MQ.Write($"Hunt PullSpell = {PullSpell}");
                        break;
                    case "pullitem":
                        if (x.args.Count > 1) PullItem = string.Join(" ", x.args.GetRange(1, x.args.Count - 1));
                        SaveHuntPullSettings();
                        MQ.Write($"Hunt PullItem = {PullItem}");
                        break;
                    case "pullaa":
                        if (x.args.Count > 1) PullAA = string.Join(" ", x.args.GetRange(1, x.args.Count - 1));
                        SaveHuntPullSettings();
                        MQ.Write($"Hunt PullAA = {PullAA}");
                        break;
                    case "pulldisc":
                        if (x.args.Count > 1) PullDisc = string.Join(" ", x.args.GetRange(1, x.args.Count - 1));
                        SaveHuntPullSettings();
                        MQ.Write($"Hunt PullDisc = {PullDisc}");
                        break;
                    case "autoassist":
                        if (x.args.Count > 1)
                        {
                            var v = x.args[1].ToLowerInvariant();
                            if (v == "on") AutoAssistAtMelee = true;
                            else if (v == "off") AutoAssistAtMelee = false;
                            else if (v == "toggle") AutoAssistAtMelee = !AutoAssistAtMelee;
                        }
                        MQ.Write($"Hunt AutoAssistAtMelee = {AutoAssistAtMelee}");
                        break;
                    case "debug":
                        try
                        {
                            if (!MQ.Query<bool>("${Plugin[MQ2Mono]}")) { MQ.Write("MQ2Mono required for ImGui: /plugin MQ2Mono"); break; }
                            Core.EnqueueUI(() => MonoCore.Core.ToggleImGuiHuntDebugWindow());
                        }
                        catch (Exception ex)
                        {
                            MQ.Write($"ImGui error: {ex.Message}");
                        }
                        break;
                    case "camp":
                        if (x.args.Count > 1 && x.args[1].Equals("set", StringComparison.OrdinalIgnoreCase))
                        {
                            CampOn = true;
                            CampX = MQ.Query<int>("${Me.X}");
                            CampY = MQ.Query<int>("${Me.Y}");
                            CampZ = MQ.Query<int>("${Me.Z}");
                            MQ.Write($"Hunt camp set at X:{CampX} Y:{CampY} Z:{CampZ}");
                        }
                        else if (x.args.Count > 1 && x.args[1].Equals("off", StringComparison.OrdinalIgnoreCase))
                        {
                            CampOn = false; MQ.Write("Hunt camp off");
                        }
                        else
                        {
                            MQ.Write("/hunt camp [set|off]");
                        }
                        break;
                    default:
                        MQ.Write("Unknown /hunt option.");
                        break;
                }
            }, "Toggle and configure Hunt mode");

            // Toggle the small Hunt ImGui window
            EventProcessor.RegisterCommand("/huntui", (x) =>
            {
                try
                {
                    if (!MQ.Query<bool>("${Plugin[MQ2Mono]}")) { MQ.Write("MQ2Mono required for ImGui: /plugin MQ2Mono"); return; }
                    Core.EnqueueUI(() => MonoCore.Core.ToggleImGuiHuntWindow());
                }
                catch (Exception ex)
                {
                    MQ.Write($"ImGui error: {ex.Message}");
                }
            }, "Toggle Hunt ImGui window");
        }

        // Debug state tracking
        private static string _lastStatus = string.Empty;
        private static bool _lastWaitingForLoot = false;
        private static int _lastTargetID = 0;

        [ClassInvoke(Data.Class.All)]
        public static void Tick()
        {
            // Keep cached zone current on the processing loop (not the UI thread)
            UpdateCurrentZoneCached();

            // Throttle work, including publishing SmartLoot state
            if (!e3util.ShouldCheck(ref _nextTickAt, _tickIntervalMs)) return;

            // Always publish SmartLoot state so peers can coordinate,
            // even when Hunt is disabled on this character
            UpdateSmartLootState();

            // If we were previously paused only due to Go=false (reason "Hunt paused"),
            // and Go has been turned back on, immediately resume into Scanning so
            // the status reflects the active state without waiting for the scan throttle.
            if (Enabled && Go && HuntStateMachine.CurrentState == HuntState.Paused)
            {
                var reason = HuntStateMachine.StateReason ?? string.Empty;
                if (reason.Equals("Hunt paused", StringComparison.OrdinalIgnoreCase))
                {
                    _waitingForLoot = false;
                    _nextScanAt = 0; // allow immediate scan
                    HuntStateMachine.TransitionTo(HuntState.Scanning, "Resumed");
                    // fall through to normal handling this tick
                }
            }

            if (!Enabled)
            {
                HuntStateMachine.TransitionTo(HuntState.Disabled, "Hunt disabled");
                return;
            }

            // Peer zone checks are handled centrally in DetermineTargetState() to avoid duplication

            // Determine what state we should be in
            DetermineTargetState();

            // Handle current state
            HandleCurrentState();

            // Update debug tracking at end of tick
            _lastWaitingForLoot = _waitingForLoot;
            _lastTargetID = TargetID;
            _lastStatus = Status;
        }

        private static void DetermineTargetState()
        {
            // Emergency safety check - disable if game seems unstable
            try
            {
                if (!MQ.Query<bool>("${Me.ID}"))
                {
                    HuntStateMachine.TransitionTo(HuntState.Safety, "Game unstable");
                    return;
                }
            }
            catch
            {
                HuntStateMachine.TransitionTo(HuntState.Safety, "Query error");
                return;
            }

            // Check blocking conditions
            if (Assist.IsAssisting || E3._amIDead)
            {
                HuntStateMachine.TransitionTo(HuntState.InCombat, "Assisting or Dead");
                return;
            }

            // Check if any connected peers are in a different zone or dead
            try
            {
                var connectedBots = E3.Bots.BotsConnected();
                string currentZone = GetCurrentZone();
                foreach (string botName in connectedBots)
                {
                    if (string.Equals(botName, E3.CurrentName, StringComparison.OrdinalIgnoreCase))
                        continue; // Skip self

                    // Check if peer is in a different zone
                    string peerZone = E3.Bots.Query(botName, "${Zone.ShortName}");
                    if (!string.IsNullOrEmpty(peerZone) && !peerZone.Equals("NULL", StringComparison.OrdinalIgnoreCase))
                    {
                        if (!string.Equals(peerZone, currentZone, StringComparison.OrdinalIgnoreCase))
                        {
                            HuntStateMachine.TransitionTo(HuntState.Paused, $"Waiting for {botName} to return to zone");
                            return;
                        }
                    }
                    
                    // Check if peer is dead
                    string peerDead = E3.Bots.Query(botName, "${Me.Dead}");
                    bool isPeerDead = string.Equals(peerDead, "TRUE", StringComparison.OrdinalIgnoreCase);
                    if (isPeerDead)
                    {
                        HuntStateMachine.TransitionTo(HuntState.Paused, $"Waiting for {botName} to respawn");
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                _log.Write($"Hunt: Error checking connected bots zone states: {ex.Message}");
            }

            if (MQ.Query<bool>("${Me.Stunned}"))
            {
                HuntStateMachine.TransitionTo(HuntState.InCombat, "Stunned");
                return;
            }

            if (e3util.IsManualControl())
            {
                HuntStateMachine.TransitionTo(HuntState.ManualControl, "Manual control active");
                return;
            }

            if (!Go)
            {
                HuntStateMachine.TransitionTo(HuntState.Paused, "Hunt paused");
                return;
            }

            // If we're already in loot-wait mode, keep yielding while SmartLoot remains active
            try
            {
                if (_waitingForLoot && IsSmartLootActiveForLootWait())
                {
                    HuntStateMachine.TransitionTo(HuntState.WaitingForLoot, "SmartLoot active");
                    return;
                }
            }
            catch { }

            // Track XTarget changes
            int currentXTargets = 0;
            try { currentXTargets = MQ.Query<int>("${Me.XTarget}"); } catch { currentXTargets = 0; }

            if (currentXTargets != _lastXTargetCount)
            {
                _log.Write($"Hunt: XTarget count changed {_lastXTargetCount} -> {currentXTargets}");
            }

            // Check if we should enter loot wait
            // In your main tick, where you detect "combat just ended"
            if (Hunt.CurrentState == HuntState.InCombat && XTargetJustCleared())
            {
                BeginLootWait("Waiting for SmartLoot to process corpses");
                return;
            }

            _lastXTargetCount = currentXTargets;

            // Ensure we've called assist on a nearby XTarget NPC if applicable
            TryEnsureAssistOnNearbyXTarget(currentXTargets);

            // If waiting for loot, check if we should continue waiting
            if (HuntStateMachine.CurrentState == HuntState.WaitingForLoot)
            {
                // OPTIONAL: nudge SmartLoot to do a single pass if you're running it in Background mode
                if (string.Equals(_slMode, "Background", StringComparison.OrdinalIgnoreCase) && _slCorpseCount > 0)
                {
                    MQ.Cmd("/echo Hunt nudging SmartLoot once");
                    MQ.Query<string>("${SmartLoot.Command[once]}"); // triggers a single looting pass
                }

                if (LootWaitComplete())
                {
                    _waitingForLoot = false;
                    DebugLog("Loot wait complete; resuming scanning.");
                    HuntStateMachine.TransitionTo(HuntState.Scanning, "Loot complete");
                    _nextScanAt = 0; // allow immediate rescan
                }
                else
                {
                    // stay paused; do not scan or nav
                    return;
                }
            }

            // Prefer adopting player's current EQ target if it is a valid NPC for hunting
            try
            {
                int curEqTarget = MQ.Query<int>("${Target.ID}");
                if (curEqTarget > 0 && curEqTarget != TargetID)
                {
                    // Validate
                    bool isNpc = MQ.Query<bool>($"${{Spawn[id {curEqTarget}].Type.Equal[NPC]}}" );
                    bool targetable = MQ.Query<bool>($"${{Spawn[id {curEqTarget}].Targetable}}" );
                    bool corpse = MQ.Query<bool>($"${{Spawn[id {curEqTarget}].Type.Equal[Corpse]}}" );
                    if (isNpc && targetable && !corpse && IsValidCombatTarget(curEqTarget))
                    {
                        // Respect filters and temporary ignores
                        string nm = GetSpawnName(curEqTarget);
                        if (!MatchesIgnore(nm) && !IsIgnored(nm) && PassesPullFilters(nm))
                        {
                            // Optional: stay within configured search radius to avoid far-away manual clicks
                            double d = MQ.Query<double>($"${{Spawn[id {curEqTarget}].Distance3D}}" );
                            if (d <= Math.Max(50, Radius + 50))
                            {
                                TargetID = curEqTarget;
                                TargetName = nm;
                                ResetNoPathTracking(TargetID);
                                _log.Write($"Hunt: Adopted current EQ target {TargetID} ({TargetName})");
                                DebugLog($"TARGET: adopt EQ -> {TargetID} ({TargetName})");
                                HuntStateMachine.TransitionTo(HuntState.NavigatingToTarget, "Adopted current target");
                                return;
                            }
                        }
                    }
                }
            }
            catch { }

            // Adopt XTarget as hunt target if needed - Enhanced with combat state validation
            if (TargetID <= 0 && currentXTargets > 0)
            {
                // First, check if we're actually in combat
                bool currentlyInCombat = Basics.InCombat();
                _log.Write($"Hunt: XTarget adoption check - InCombat: {currentlyInCombat}, XTargets: {currentXTargets}");
                
                if (!currentlyInCombat)
                {
                    // Not in combat, be more selective about XTarget adoption
                    int validTarget = AcquireNextValidXTarget();
                    if (validTarget > 0)
                    {
                        TargetID = validTarget;
                        TargetName = GetSpawnName(validTarget);
                        _log.Write($"Hunt: Adopting validated XTarget {TargetID} ({TargetName})");
                        HuntStateMachine.TransitionTo(HuntState.NavigatingToTarget, $"Adopted validated XTarget");
                        return;
                    }
                    else
                    {
                        _log.Write($"Hunt: No valid combat targets found in XTarget list");
                    }
                    // If no valid targets in XTarget, don't adopt anything
                    return;
                }
                else
                {
                    // In combat, adopt more aggressively but still validate
                    int nextTarget = AcquireNextFromXTarget();
                    if (nextTarget > 0)
                    {
                        TargetID = nextTarget;
                        TargetName = GetSpawnName(nextTarget);
                        _log.Write($"Hunt: Adopting combat XTarget {TargetID} ({TargetName})");
                        HuntStateMachine.TransitionTo(HuntState.InCombat, $"Adopted combat XTarget");
                        return;
                    }
                    else
                    {
                        _log.Write($"Hunt: No valid targets found in XTarget during combat");
                    }
                }
            }

            // If in combat with no current hunt target, stay in combat
            if (TargetID <= 0 && Basics.InCombat())
            {
                HuntStateMachine.TransitionTo(HuntState.InCombat, "In combat without hunt target");
                return;
            }

            // If we have a valid target
            if (TargetID > 0)
            {
                if (_lastTargetID != TargetID)
                {
                    _log.Write($"Hunt: Target changed from {_lastTargetID} to {TargetID} ({TargetName})");
                }

                // Check if target is still valid
                var isCorpse = MQ.Query<bool>($"${{Spawn[id {TargetID}].Type.Equal[Corpse]}}");
                if (isCorpse || !MQ.Query<bool>($"${{Spawn[id {TargetID}]}}"))
                {
                    _log.Write($"Hunt: Target {TargetID} became invalid or corpse");
                    // Only stop nav if we currently own it; avoid interrupting SmartLoot corpse nav
                    if (HuntStateMachine.IsNavigationOwned)
                    {
                        try { MQ.Cmd("/nav stop"); } catch { }
                    }
                    TargetID = 0; 
                    TargetName = string.Empty;
                    _waitingForLoot = true;
                    _lootWaitStartMs = Core.StopWatch.ElapsedMilliseconds;
                    HuntStateMachine.TransitionTo(HuntState.WaitingForLoot, "Target became invalid");
                    return;
                }

                // Additional validation: ensure not self and ensure valid NPC
                try
                {
                    int meId = MQ.Query<int>("${Me.ID}");
                    if (TargetID == meId)
                    {
                        _log.Write("Hunt: Clearing self-targeted selection");
                        if (HuntStateMachine.IsNavigationOwned)
                        {
                            try { MQ.Cmd("/nav stop"); } catch { }
                        }
                        _navTargetID = 0;
                        TargetID = 0;
                        TargetName = string.Empty;
                        _nextScanAt = 0; // rescan immediately
                        HuntStateMachine.TransitionTo(HuntState.Scanning, "Self was targeted");
                        return;
                    }

                    bool isNpc = MQ.Query<bool>($"${{Spawn[id {TargetID}].Type.Equal[NPC]}}" );
                    bool targetable = MQ.Query<bool>($"${{Spawn[id {TargetID}].Targetable}}" );
                    if (!isNpc || !targetable)
                    {
                        _log.Write($"Hunt: Target {TargetID} rejected (NPC={isNpc}, Targetable={targetable})");
                        if (HuntStateMachine.IsNavigationOwned)
                        {
                            try { MQ.Cmd("/nav stop"); } catch { }
                        }
                        _navTargetID = 0;
                        TargetID = 0;
                        TargetName = string.Empty;
                        _nextScanAt = 0; // rescan immediately
                        HuntStateMachine.TransitionTo(HuntState.Scanning, "Invalid target");
                        return;
                    }
                }
                catch { }

                // Do not auto-switch to other XTargets while navigating; keep the selected TargetID

                string aggroCheck = CheckForUnexpectedAggro();
                if (!string.IsNullOrEmpty(aggroCheck))
                {
                    if (HuntStateMachine.IsNavigationOwned)
                    {
                        try { MQ.Cmd("/nav stop"); } catch { }
                    }
                    _navTargetID = 0;
                    int threatId = 0;
                    try { threatId = AcquireNextValidXTarget(); } catch { threatId = 0; }
                    if (threatId > 0)
                    {
                        TargetID = threatId;
                        TargetName = GetSpawnName(threatId);
                        TrySetTargetNonBlocking(TargetID);
                    }
                    DebugLog($"COMBAT: unexpected aggro -> {TargetID} ({TargetName})");
                    HuntStateMachine.TransitionTo(HuntState.InCombat, aggroCheck);
                    return;
                }

                // Determine if we should be pulling, navigating, or in combat
                double dist = MQ.Query<double>($"${{Spawn[id {TargetID}].Distance3D}}");
                int meleeRange = MQ.Query<int>($"${{Spawn[id {TargetID}].MaxRangeTo}}");
                if (meleeRange <= 0) meleeRange = 25;

                if (dist > 0 && dist <= meleeRange)
                {
                    // Enhanced combat state transition with validation
                    if (ShouldTransitionToCombat())
                    {
                        // Close enough for combat
                        if (AutoAssistAtMelee && !Assist.IsAssisting)
                        {
                            long now = Core.StopWatch.ElapsedMilliseconds;
                            if (now - _lastAssistCmdAt >= _assistCmdCooldownMs)
                            {
                                // Ensure a valid EQ target before assisting
                                bool haveValidTarget = false;
                                try
                                {
                                    int curT = MQ.Query<int>("${Target.ID}");
                                    haveValidTarget = curT > 0 && IsValidCombatTarget(curT);
                                    if (!haveValidTarget && TargetID > 0 && IsValidCombatTarget(TargetID))
                                    {
                                        TrySetTargetNonBlocking(TargetID);
                                        curT = MQ.Query<int>("${Target.ID}");
                                        haveValidTarget = (curT == TargetID);
                                    }
                                    if (!haveValidTarget)
                                    {
                                        int xid = AcquireNextValidXTarget();
                                        if (xid > 0)
                                        {
                                            TrySetTargetNonBlocking(xid);
                                            haveValidTarget = true;
                                        }
                                    }
                                }
                                catch { haveValidTarget = false; }

                                if (haveValidTarget)
                                {
                                    MQ.Cmd("/assistme /all");
                                    _lastAssistCmdAt = now;
                                }
                            }

                            MQ.Delay(200);
                            MQ.Cmd("/face");
                            MQ.Delay(300);
                            MQ.Cmd("/stick 10 moveback");
                        }
                        HuntStateMachine.TransitionTo(HuntState.InCombat, "At melee range with valid target");
                        return;
                    }
                    else
                    {
                        // Target is in melee range but not valid for combat
                        _log.Write($"Hunt: Target {TargetID} in melee range but not valid for combat");
                        HuntStateMachine.TransitionTo(HuntState.NavigatingToTarget, "Invalid target in melee range");
                        return;
                    }
                }
                else if (CanPull() && ShouldPull(dist))
                {
                    HuntStateMachine.TransitionTo(HuntState.PullingTarget, "In pull range");
                    return;
                }
                else
                {
                    HuntStateMachine.TransitionTo(HuntState.NavigatingToTarget, "Moving to target");
                    return;
                }
            }

            // No target - check if we should scan
            if (e3util.ShouldCheck(ref _nextScanAt, _scanIntervalMs))
            {
                string readyCheck = ValidateReadyToHunt();
                if (!string.IsNullOrEmpty(readyCheck))
                {
                    HuntStateMachine.TransitionTo(HuntState.WaitingForLoot, readyCheck);
                    return;
                }

                HuntStateMachine.TransitionTo(HuntState.Scanning, "Ready to scan for targets");
            }
        }

        // If there are mobs on XTarget and we haven't called assist on any of those NPCs,
        // and at least one is within 50 units, issue an assist call.
        private static void TryEnsureAssistOnNearbyXTarget(int currentXTargets)
        {
            try
            {
                if (currentXTargets <= 0) return;
                long now = Core.StopWatch.ElapsedMilliseconds;
                if (now - _lastAssistCmdAt < _assistCmdCooldownMs) return; // cooldown

                // Build list of valid XTarget NPC IDs and check proximity
                int max = e3util.XtargetMax;
                bool anyNearby = false;
                bool assistedOnAny = false;
                int assistTargetId = 0;
                int candidateId = 0;
                double candidateDist = double.MaxValue;
                try { assistTargetId = Assist.AssistTargetID; } catch { assistTargetId = 0; }

                for (int i = 1; i <= max && i <= currentXTargets; i++)
                {
                    int xid = 0;
                    try { xid = MQ.Query<int>($"${{Me.XTarget[{i}].ID}}"); } catch { xid = 0; }
                    if (xid <= 0) continue;

                    // Only consider targetable NPCs
                    bool isNpc = false;
                    bool targetable = false;
                    try { isNpc = MQ.Query<bool>($"${{Spawn[id {xid}].Type.Equal[NPC]}}" ); } catch { }
                    try { targetable = MQ.Query<bool>($"${{Spawn[id {xid}].Targetable}}" ); } catch { }
                    if (!isNpc || !targetable) continue;

                    // If we're already assisting one of them, we're good
                    if (assistTargetId > 0 && xid == assistTargetId) { assistedOnAny = true; break; }

                    // Check distance
                    double dist = 0;
                    try { dist = MQ.Query<double>($"${{Spawn[id {xid}].Distance3D}}" ); } catch { dist = 0; }
                    if (dist > 0 && dist < 50.0)
                    {
                        anyNearby = true;
                        if (dist < candidateDist)
                        {
                            candidateDist = dist;
                            candidateId = xid;
                        }
                    }
                }

                if (!assistedOnAny && anyNearby)
                {
                    // Ensure a valid EQ target before assisting
                    bool haveValidTarget = false;
                    try
                    {
                        int curT = MQ.Query<int>("${Target.ID}");
                        haveValidTarget = curT > 0 && IsValidCombatTarget(curT);
                        if (!haveValidTarget && candidateId > 0)
                        {
                            TrySetTargetNonBlocking(candidateId);
                            haveValidTarget = true;
                        }
                    }
                    catch { haveValidTarget = false; }

                    if (haveValidTarget)
                    {
                        MQ.Cmd("/assistme /all");
                        _lastAssistCmdAt = now;
                    }
                }
            }
            catch { }
        }

        private static void HandleCurrentState()
        {
            switch (HuntStateMachine.CurrentState)
            {
                case HuntState.Disabled:
                case HuntState.Safety:
                case HuntState.ManualControl:
                case HuntState.Paused:
                    // These states don't require active handling
                    break;

                case HuntState.Scanning:
                    HandleScanning();
                    break;

                case HuntState.NavigatingToTarget:
                    HandleNavigatingToTarget();
                    break;

                case HuntState.PullingTarget:
                    HandlePullingTarget();
                    break;

                case HuntState.InCombat:
                    HandleInCombat();
                    break;

                case HuntState.WaitingForLoot:
                    HandleWaitingForLoot();
                    break;

                case HuntState.NavigatingToCamp:
                    HandleNavigatingToCamp();
                    break;
            }
        }

        private static void HandleScanning()
        {
            TryAcquireTarget();
        }

        private static void HandleNavigatingToTarget()
        {
            // Let SmartLoot drive only when it reports new corpses or peer triggered
            UpdateSmartLootTelemetry();
            if (_slHasNewCorpses || _slIsPeerTriggered)
            {
                if (HuntStateMachine.IsNavigationOwned)
                {
                    HuntStateMachine.ReleaseNavigationControl("NavigateToTarget->SmartLoot active");
                }
                // Stay in NavigatingToTarget; skip issuing nav this tick
            }

            // Remove corpse-nearby preemption; gating happens in CanNavNow() using HasNewCorpses and IsPeerTriggered

            // Attempt to get nav control; if we can't, just continue (another system may be navigating)
            HuntStateMachine.RequestNavigationControl("NavigateToTarget");

            // Ensure EQ target and nav are heading to the current TargetID
            try
            {
                int curT = MQ.Query<int>("${Target.ID}");
                if (TargetID > 0 && curT != TargetID)
                {
                    TrySetTargetNonBlocking(TargetID);
                }
                bool navActive = MQ.Query<bool>("${Navigation.Active}");
                // Only stop nav if WE own nav and WE previously issued nav to a different id.
                // Do not interrupt SmartLoot navigation to corpses.
                if (navActive && HuntStateMachine.IsNavigationOwned && _navTargetID > 0 && _navTargetID != TargetID && TargetID > 0)
                {
                    MQ.Cmd("/nav stop");
                    _navTargetID = 0;
                }
            }
            catch { }

            // Navigate towards target with cooldown to prevent spam
            if (TargetID > 0 && e3util.ShouldCheck(ref _nextNavAt, _navCooldownMs))
            {
                StartNavNonBlocking(TargetID);
            }
        }

        private static void HandlePullingTarget()
        {
            // Initialize or monitor pull attempt timer
            if (_pullAttemptStartMs == 0 || TargetID != _lastTargetID)
            {
                _pullAttemptStartMs = Core.StopWatch.ElapsedMilliseconds;
            }

            // Abort pulling if taking too long (temporary ignore, then rescan)
            if (PullIgnoreTimeSec > 0)
            {
                long elapsed = Core.StopWatch.ElapsedMilliseconds - _pullAttemptStartMs;
                if (elapsed >= PullIgnoreTimeSec * 1000)
                {
                    try
                    {
                        if (TargetID > 0)
                        {
                            _tempIgnoreUntil[TargetID] = Core.StopWatch.ElapsedMilliseconds + 60_000; // ignore for 60s
                            _log.Write($"Hunt: Aborting pull on {TargetID} after {elapsed}ms; temporarily ignoring for 60s");
                        }
                    }
                    catch { }

                    TargetID = 0;
                    TargetName = string.Empty;
                    _pullAttemptStartMs = 0;
                    HuntStateMachine.TransitionTo(HuntState.Scanning, "Pull timeout");
                    return;
                }
            }

            // Let SmartLoot drive only when it reports new corpses or peer triggered; otherwise continue pulling
            UpdateSmartLootTelemetry();
            if (_slHasNewCorpses || _slIsPeerTriggered)
            {
                if (HuntStateMachine.IsNavigationOwned)
                {
                    HuntStateMachine.ReleaseNavigationControl("PullingTarget->SmartLoot active");
                }
                // Skip nav/positioning on this tick
                return;
            }

            // Try to keep within a reasonable pull distance and line of sight
            int curTarget = MQ.Query<int>("${Target.ID}");
            if (curTarget != TargetID)
            {
                TrySetTargetNonBlocking(TargetID);
                DebugLog($"PULL: sync EQ target -> {TargetID}");
            }

            bool los = MQ.Query<bool>($"${{Spawn[id {TargetID}].LineOfSight}}" );
            double dist = MQ.Query<double>($"${{Spawn[id {TargetID}].Distance3D}}" );

            // Acquire navigation control so we can fine-position for pull
            HuntStateMachine.RequestNavigationControl("PullingTarget");

            // Decide desired stop distance by method
            string method = NormalizeMethod(PullMethod);
            int desiredStop = E3.GeneralSettings.Movement_NavStopDistance;
            if (method.Equals("Ranged", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    int baseRange = MQ.Query<int>("${Me.Inventory[ranged].Range}");
                    bool isArchery = MQ.Query<bool>("${Me.Inventory[ranged].Type.Find[Archery]}");
                    int maxRange = isArchery ? baseRange * 2 : baseRange;
                    if (maxRange <= 0) maxRange = 200;
                    int minRange = 35;
                    double factor = Math.Max(0.3, Math.Min(0.9, RangedApproachFactor));
                    desiredStop = (int)Math.Max(minRange, Math.Min(maxRange - 5, maxRange * factor));
                }
                catch { desiredStop = Math.Max(desiredStop, 35); }
            }
            else
            {
                desiredStop = Math.Max(20, PullApproachDistance);
            }

            // If too far or no LoS, step into position using nav with a cooldown
            if ((dist <= 0 || dist > desiredStop + 2 || !los) && e3util.ShouldCheck(ref _nextNavAt, _navCooldownMs))
            {
                DebugLog($"PULL: approach id={TargetID} desiredStop={desiredStop} dist={dist:0.0} los={(los?"Y":"N")}");
                StartNavNonBlocking(TargetID, desiredStop);
                return; // let nav update and retry on next tick
            }

            // Face the target for better LoS before the pull action
            try { MQ.Cmd("/face fast"); } catch { }

            // Attempt the pull action now that we're in position
            DebugLog($"PULL: attempt method={method}");
            TryPull();
        }

        private static void HandleInCombat()
        {
            // Release navigation control during combat so SmartLoot can work
            if (HuntStateMachine.IsNavigationOwned)
            {
                HuntStateMachine.ReleaseNavigationControl("InCombat");
            }
        }

        private static void HandleWaitingForLoot()
        {
            // Always release navigation control during loot phase
            if (HuntStateMachine.IsNavigationOwned)
            {
                HuntStateMachine.ReleaseNavigationControl("WaitingForLoot");
            }

            // If loot wait has completed, resume previous activity without retargeting if possible
            if (IsLootWaitComplete())
            {
                _waitingForLoot = false;
                _log.Write("Hunt: Loot wait completed - resuming");
                // Prefer resuming navigation to existing TargetID if still valid
                if (TargetID > 0 && IsValidCombatTarget(TargetID))
                {
                    HuntStateMachine.TransitionTo(HuntState.NavigatingToTarget, "Loot complete");
                }
                else
                {
                    _nextScanAt = 0; // allow immediate scanning
                    HuntStateMachine.TransitionTo(HuntState.Scanning, "Loot complete");
                }
            }
        }

        private static void HandleNavigatingToCamp()
        {
            if (!HuntFromPlayer && !HuntStateMachine.RequestNavigationControl("NavigateToCamp"))
            {
                HuntStateMachine.TransitionTo(HuntState.Paused, "Can't get navigation control for camp");
                return;
            }

            if (CampOn && !HuntFromPlayer && !Movement.IsNavigating())
            {
                e3util.TryMoveToLoc(CampX, CampY, CampZ, 10, 3000);
            }
        }

        private static bool ShouldEnterLootWait()
        {
            try
            {
                bool smartLootLoaded = MQ.Query<bool>("${Plugin[MQ2SmartLoot]}");
                if (!smartLootLoaded) return true;

                string smartLootState = MQ.Query<string>("${SmartLoot.State}");
                // Don't enter loot wait if SmartLoot still detects combat
                return !smartLootState.Equals("CombatDetected", StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return true; // Default to entering loot wait on error
            }
        }

        private static bool IsLootWaitComplete()
        {
            long elapsed = Core.StopWatch.ElapsedMilliseconds - _lootWaitStartMs;
            int lootWaitTime = E3.GeneralSettings.Loot_TimeToWaitAfterAssist;
            int minWait = Math.Max(_lootMinWaitMs, Math.Max(0, lootWaitTime));
            bool timeDelayPassed = elapsed >= minWait;

            // Consider SmartLoot fully inactive using robust criteria
            bool smartLootActive = IsSmartLootActiveForLootWait();

            // If SmartLoot detects combat, exit loot wait immediately
            try
            {
                string smartLootState = MQ.Query<string>("${SmartLoot.State}");
                if (!string.IsNullOrEmpty(smartLootState) && smartLootState.Equals("CombatDetected", StringComparison.OrdinalIgnoreCase))
                {
                    _waitingForLoot = false;
                    _log.Write("Hunt: SmartLoot detected combat - exiting loot wait");
                    return true;
                }
            }
            catch { }

            // Fallback timeout only applies when SmartLoot is not active
            bool fallbackTimeout = elapsed >= _lootWaitFallbackMs;
            return (!smartLootActive && timeDelayPassed) || (!smartLootActive && fallbackTimeout);
        }

        private static string GetLootWaitReason()
        {
            long elapsed = Core.StopWatch.ElapsedMilliseconds - _lootWaitStartMs;
            int lootWaitTime = E3.GeneralSettings.Loot_TimeToWaitAfterAssist;
            int minWait = Math.Max(_lootMinWaitMs, Math.Max(0, lootWaitTime));
            bool timeDelayPassed = elapsed >= minWait;

            try
            {
                bool smartLootLoaded = MQ.Query<bool>("${Plugin[MQ2SmartLoot]}");
                if (smartLootLoaded)
                {
                    string smartLootState = MQ.Query<string>("${SmartLoot.State}");
                    bool isProcessing = false;
                    bool needsDecision = false;
                    bool lootWindowOpen = false;
                    try { isProcessing = MQ.Query<bool>("${SmartLoot.IsProcessing}"); } catch { }
                    try { needsDecision = MQ.Query<bool>("${SmartLoot.NeedsDecision}"); } catch { }
                    try { lootWindowOpen = MQ.Query<bool>("${SmartLoot.LootWindowOpen}"); } catch { }

                    bool smartLootIdle = smartLootState.Equals("Idle", StringComparison.OrdinalIgnoreCase) && !isProcessing && !needsDecision && !lootWindowOpen;

                    if (!smartLootIdle)
                    {
                        string extra = needsDecision ? ", NeedsDecision" : (lootWindowOpen ? ", LootWindowOpen" : (isProcessing ? ", Processing" : ""));
                        return $"Waiting for SmartLoot ({smartLootState}{extra})";
                    }
                }
            }
            catch { }

            if (!timeDelayPassed)
                return $"Waiting for loot delay ({elapsed}ms/{minWait}ms)";

            return "Waiting for loot";
        }

        // Debounced SmartLoot activity detection for nav preemption
        private static long _smartLootFirstActiveAt = 0;
        private static string _smartLootLastState = string.Empty;
        private static readonly int _smartLootDebounceMs = 1200;
        // Loot-wait hysteresis to allow scanning between multiple corpses
        private static long _smartLootLastActiveSignalAt = 0;
        private static readonly int _smartLootIdleGraceMs = 500;

        // Use this to decide if SmartLoot should preempt navigation (avoid thrash)
        private static bool IsSmartLootActiveForNav()
        {
            try
            {
                bool smartLootLoaded = MQ.Query<bool>("${Plugin[MQ2SmartLoot]}");
                if (!smartLootLoaded) return false;

                string state = MQ.Query<string>("${SmartLoot.State}");
                bool needsDecision = false;
                bool lootWindowOpen = false;
                try { needsDecision = MQ.Query<bool>("${SmartLoot.NeedsDecision}"); } catch { }
                try { lootWindowOpen = MQ.Query<bool>("${SmartLoot.LootWindowOpen}"); } catch { }

                // Treat only these as nav-preempting states
                bool stateIsPreempting = !string.IsNullOrEmpty(state) && (
                    state.Equals("OpeningLootWindow", StringComparison.OrdinalIgnoreCase) ||
                    state.Equals("ProcessingItems", StringComparison.OrdinalIgnoreCase) ||
                    state.Equals("WaitingForPendingDecision", StringComparison.OrdinalIgnoreCase) ||
                    state.Equals("CleaningUpCorpse", StringComparison.OrdinalIgnoreCase)
                );

                // Explicitly consider these benign for navigation
                bool stateIsBenign = !string.IsNullOrEmpty(state) && (
                    state.Equals("FindingCorpse", StringComparison.OrdinalIgnoreCase) ||
                    state.Equals("NavigatingToCorpse", StringComparison.OrdinalIgnoreCase) ||
                    state.Equals("Scan", StringComparison.OrdinalIgnoreCase) ||
                    state.Equals("Scanning", StringComparison.OrdinalIgnoreCase)
                );

                if (!string.IsNullOrEmpty(state) && state.Equals("CombatDetected", StringComparison.OrdinalIgnoreCase))
                    return false; // do not preempt nav due to combat detection

                bool rawActive = lootWindowOpen || needsDecision || stateIsPreempting;

                long now = Core.StopWatch.ElapsedMilliseconds;
                if (rawActive && !stateIsBenign)
                {
                    if (_smartLootFirstActiveAt == 0 || !string.Equals(_smartLootLastState, state, StringComparison.OrdinalIgnoreCase))
                    {
                        _smartLootFirstActiveAt = now;
                        _smartLootLastState = state ?? string.Empty;
                    }
                    // Require sustained active state to avoid thrashing due to periodic scans
                    return (now - _smartLootFirstActiveAt) >= _smartLootDebounceMs;
                }
                else
                {
                    _smartLootFirstActiveAt = 0;
                    _smartLootLastState = state ?? string.Empty;
                    return false;
                }
            }
            catch { return false; }
        }

        // Use this for loot-wait gating; keep waiting through transitions like "FindingCorpse"
        private static bool IsSmartLootActiveForLootWait()
        {
            try
            {
                bool smartLootLoaded = MQ.Query<bool>("${Plugin[MQ2SmartLoot]}");
                if (!smartLootLoaded) return false;

                string state = MQ.Query<string>("${SmartLoot.State}");
                bool isProcessing = false, needsDecision = false, lootWindowOpen = false;
                try { isProcessing = MQ.Query<bool>("${SmartLoot.IsProcessing}"); } catch { }
                try { needsDecision = MQ.Query<bool>("${SmartLoot.NeedsDecision}"); } catch { }
                try { lootWindowOpen = MQ.Query<bool>("${SmartLoot.LootWindowOpen}"); } catch { }

                // Strong active signals
                bool strong = isProcessing || needsDecision || lootWindowOpen;
                long now = Core.StopWatch.ElapsedMilliseconds;

                if (!string.IsNullOrEmpty(state) && state.Equals("CombatDetected", StringComparison.OrdinalIgnoreCase))
                {
                    // Not a loot-active state
                    return false;
                }

                bool nonIdle = !string.IsNullOrEmpty(state) && !state.Equals("Idle", StringComparison.OrdinalIgnoreCase);
                if (strong || nonIdle)
                {
                    // Any non-idle (including FindingCorpse) extends active session to allow successive corpses
                    _smartLootLastActiveSignalAt = now;
                    return true;
                }

                // Grace period to bridge brief Idle gaps between corpses
                return (now - _smartLootLastActiveSignalAt) < _smartLootIdleGraceMs;
            }
            catch { return false; }
        }

        private static bool CanPull()
        {
            if (string.Equals(PullMethod, "None", StringComparison.OrdinalIgnoreCase)) return false;
            if (TargetID <= 0) return false;
            if (MQ.Query<int>("${Me.Casting.ID}") > 0) return false;
            if (MQ.Query<bool>("${Me.Stunned}")) return false;

            bool los = MQ.Query<bool>($"${{Spawn[id {TargetID}].LineOfSight}}");
            return los;
        }

        private static bool ShouldPull(double distance)
        {
            if (Core.StopWatch.ElapsedMilliseconds < _nextPullAt) return false;

            switch (NormalizeMethod(PullMethod))
            {
                case "Ranged":
                    int baseRange = MQ.Query<int>("${Me.Inventory[ranged].Range}");
                    bool isThrowing = MQ.Query<bool>("${Me.Inventory[ranged].Type.Find[Throwing]}");
                    bool isArchery = MQ.Query<bool>("${Me.Inventory[ranged].Type.Find[Archery]}");
                    bool hasArrow = MQ.Query<bool>("${Me.Inventory[ammo].Type.Find[Arrow]}");
                    int maxRange = isArchery ? baseRange * 2 : baseRange;
                    if (maxRange <= 0) maxRange = 200;
                    int minRange = 35;
                    return distance >= minRange && distance <= maxRange && (isThrowing || (isArchery && hasArrow));

                case "Spell":
                    return !string.IsNullOrWhiteSpace(PullSpell) && distance >= 30 && distance <= 200;

                case "Item":
                    return !string.IsNullOrWhiteSpace(PullItem) && distance >= 30 && distance <= 200;

                case "AA":
                    if (string.IsNullOrWhiteSpace(PullAA)) return false;
                    int aaId = MQ.Query<int>($"${{Me.AltAbility[{PullAA}].ID}}");
                    bool ready = MQ.Query<bool>($"${{Me.AltAbilityReady[{PullAA}]}}");
                    return aaId > 0 && ready && distance >= 30 && distance <= 200;

                case "Disc":
                    if (string.IsNullOrWhiteSpace(PullDisc)) return false;
                    bool discReady = MQ.Query<bool>($"${{Me.CombatAbilityReady[{PullDisc}]}}");
                    return discReady && distance >= 30 && distance <= 200;

                case "Attack":
                    return distance >= 5 && distance <= 40;

                default:
                    return false;
            }
        }

        private static string CheckForUnexpectedAggro()
        {
            try
            {
                // Enhanced unexpected aggro detection with better threat assessment
                bool isNavigating = MQ.Query<bool>("${Navigation.Active}");
                if (!isNavigating) return string.Empty;
                
                int currentXTargets = MQ.Query<int>("${Me.XTarget}");
                if (currentXTargets == 0) return string.Empty;
                
                // Get current combat state
                bool inCombat = Basics.InCombat();
                double playerHealthPct = MQ.Query<double>("${Me.PctHPs}");
                
                // If we're not in combat but have XTargets, investigate more carefully
                if (!inCombat && currentXTargets > 0)
                {
                    // Check if any XTarget is actually threatening
                    for (int i = 1; i <= currentXTargets; i++)
                    {
                        int xid = MQ.Query<int>($"${{Me.XTarget[{i}].ID}}");
                        if (xid <= 0) continue;
                        
                        // Use enhanced validation
                        if (IsValidCombatTarget(xid))
                        {
                            double mobDist = MQ.Query<double>($"${{Spawn[id {xid}].Distance3D}}");
                            bool isAggressive = MQ.Query<bool>($"${{Spawn[id {xid}].Aggressive}}");
                            
                            // If mob is close and aggressive, this might be unexpected aggro
                            if (mobDist <= 50 && isAggressive)
                            {
                                string mobName = GetSpawnName(xid);
                                return $"Unexpected combat target detected: {mobName}";
                            }
                        }
                    }
                    return string.Empty; // No threatening targets found
                }
                
                // If we only have 1 XTarget and it's our intended target, we're good
                if (currentXTargets == 1 && TargetID > 0)
                {
                    int xTargetID = MQ.Query<int>("${Me.XTarget[1].ID}");
                    if (xTargetID == TargetID) return string.Empty;
                    
                    // We have 1 XTarget but it's NOT our intended target - check if it's a real threat
                    if (IsValidCombatTarget(xTargetID))
                    {
                        try
                        {
                            double distToTarget = MQ.Query<double>($"${{Spawn[id {TargetID}].Distance3D}}");
                            if (distToTarget <= 50) return string.Empty; // Close to original target, probably expected
                        }
                        catch { return string.Empty; }
                    }
                }
                
                // Check for multiple unexpected XTargets with better threat assessment
                int max = e3util.XtargetMax;
                var unexpectedMobs = new List<string>();
                bool hasIntendedTarget = false;
                
                for (int i = 1; i <= max && i <= currentXTargets; i++)
                {
                    try
                    {
                        int xTargetID = MQ.Query<int>($"${{Me.XTarget[{i}].ID}}");
                        if (xTargetID <= 0) continue;
                        
                        // Check if this IS our intended target
                        if (xTargetID == TargetID) 
                        {
                            hasIntendedTarget = true;
                            continue;
                        }
                        
                        // Use enhanced validation instead of basic checks
                        if (!IsValidCombatTarget(xTargetID)) continue;
                        
                        // Check if this is actually a threat
                        double mobDist = MQ.Query<double>($"${{Spawn[id {xTargetID}].Distance3D}}");
                        bool isAggressive = MQ.Query<bool>($"${{Spawn[id {xTargetID}].Aggressive}}");
                        
                        // Only consider it unexpected aggro if it's close and aggressive
                        if (mobDist <= 100 && isAggressive)
                        {
                            string mobName = GetSpawnName(xTargetID);
                            if (!string.IsNullOrEmpty(mobName))
                            {
                                unexpectedMobs.Add($"{mobName}({xTargetID})");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        MQ.Write($"Hunt: Error checking XTarget {i}: {ex.Message}");
                    }
                }
                
                // Only report unexpected aggro if we have actual threatening adds
                if (unexpectedMobs.Count > 0)
                {
                    return $"Unexpected aggro: {string.Join(", ", unexpectedMobs)}";
                }
                
                return string.Empty; // All XTargets are expected or non-threatening
            }
            catch (Exception ex)
            {
                MQ.Write($"Hunt: Error checking for unexpected aggro: {ex.Message}");
                return string.Empty; // Don't stop navigation on error
            }
        }

        private static string ValidateReadyToHunt()
        {
            try
            {
                // 1) Check local SmartLoot state - don't pull if SmartLoot is actively looting
                try
                {
                    if (IsSmartLootActiveForLootWait())
                    {
                        string smartLootState = "";
                        try { smartLootState = MQ.Query<string>("${SmartLoot.State}") ?? "Active"; } catch { smartLootState = "Active"; }
                        _log.Write($"Hunt: ValidateReadyToHunt - Waiting for local SmartLoot ({smartLootState})");
                        return $"Waiting for local SmartLoot ({smartLootState})";
                    }
                }
                catch (Exception ex)
                {
                    MQ.Write($"Hunt: Error checking local SmartLoot: {ex.Message}");
                }

                // 2) Check all connected bots' SmartLoot states via E3Next communication
                try
                {
                    var connectedBots = E3.Bots.BotsConnected();
                    foreach (string botName in connectedBots)
                    {
                        if (string.Equals(botName, E3.CurrentName, StringComparison.OrdinalIgnoreCase))
                            continue; // Skip self, already checked above

                        try
                        {
                            // First see if peer reports active explicitly
                            string activeFlag = E3.Bots.Query(botName, "${Data.Hunt.SmartLootActive}");
                            bool peerActive = string.Equals(activeFlag, "1", StringComparison.OrdinalIgnoreCase) ||
                                             string.Equals(activeFlag, "TRUE", StringComparison.OrdinalIgnoreCase);
                            // Prefer the new topic, but fall back to legacy ones for a state string
                            string botSmartLootState =
                                E3.Bots.Query(botName, "${Data.Hunt.SmartLootState}") ??
                                E3.Bots.Query(botName, "${Data.SmartLootState}") ??
                                E3.Bots.Query(botName, "${Data.Hunt_SmartLootState}");

                            // Normalize and ignore non-actionable values (including literal "NULL")
                            string state = botSmartLootState?.Trim();
                            bool isNullish = string.IsNullOrEmpty(state) || state.Equals("NULL", StringComparison.OrdinalIgnoreCase);
                            bool isActionableState = !isNullish &&
                                !state.Equals("Idle", StringComparison.OrdinalIgnoreCase) &&
                                !state.Equals("NotLoaded", StringComparison.OrdinalIgnoreCase) &&
                                !state.Equals("Unknown", StringComparison.OrdinalIgnoreCase) &&
                                !state.Equals("Error", StringComparison.OrdinalIgnoreCase);

                            if (peerActive || isActionableState)
                            {
                                string display = isNullish ? (peerActive ? "Active" : "Unknown") : state;
                                _log.Write($"Hunt: ValidateReadyToHunt - Waiting for {botName} SmartLoot ({display})");
                                return $"Waiting for {botName} SmartLoot ({display})";
                            }
                        }
                        catch (Exception ex)
                        {
                            _log.Write($"Hunt: Error checking {botName} SmartLoot state: {ex.Message}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    _log.Write($"Hunt: Error checking connected bots SmartLoot states: {ex.Message}");
                }

                // Peer zone/death checks removed here to avoid duplication;
                // DetermineTargetState() is authoritative for peer gating.

                return string.Empty; // All checks passed
            }
            catch (Exception ex)
            {
                MQ.Write($"Hunt: Error in ValidateReadyToHunt: {ex.Message}");
                return "Validation error";
            }
        }

        private static void TryAcquireTarget()
        {
            Status = "Scanning";
            // Clean up expired temporary ignores
            if (_tempIgnoreUntil.Count > 0)
            {
                var now = Core.StopWatch.ElapsedMilliseconds;
                var expired = _tempIgnoreUntil.Where(kv => kv.Value <= now).Select(kv => kv.Key).ToList();
                foreach (var idExpired in expired) _tempIgnoreUntil.Remove(idExpired);
            }
            
            // First check: are we already engaged with something valid?
            if (TargetID > 0)
            {
                if (IsValidCombatTarget(TargetID))
                {
                    // Current target is still valid, don't scan for new ones unless it's too far
                    double currentDist = MQ.Query<double>($"${{Spawn[id {TargetID}].Distance3D}}");
                    if (currentDist <= 200) // Still in reasonable range
                    {
                        _log.Write($"Hunt: Current target {TargetID} still valid, skipping scan");
                        return;
                    }
                }
                else
                {
                    // Current target is no longer valid
                    _log.Write($"Hunt: Current target {TargetID} is no longer valid");
                    TargetID = 0;
                    TargetName = string.Empty;
                    ResetNoPathTracking(0);
                }
            }
            
            int mobsInRadius = 0;
            try
            {
                // If HuntFromPlayer is enabled, always scan from current player location
                bool useCampOrigin = CampOn && !HuntFromPlayer;
                if (useCampOrigin)
                {
                    mobsInRadius = MQ.Query<int>($"${{SpawnCount[npc targetable loc {CampX} {CampY} radius {Radius} zradius {ZRadius}]}}" );
                }
                else
                {
                    mobsInRadius = MQ.Query<int>($"${{SpawnCount[npc targetable radius {Radius} zradius {ZRadius}]}}" );
                }
            }
            catch
            {
                Status = "Query failed";
                return;
            }

            if (mobsInRadius > 10) mobsInRadius = 10; // reduce for stability

            double bestPath = double.MaxValue; // Prefer path length
            int bestId = 0;

            bool anyPathable = false;
            var candidateList = new List<HuntCandidate>(mobsInRadius);
            for (int i = 1; i <= mobsInRadius; i++)
            {
                int id = 0;
                try
                {
                    bool useCampOrigin = CampOn && !HuntFromPlayer;
                    if (useCampOrigin)
                    {
                        id = MQ.Query<int>($"${{NearestSpawn[{i},npc targetable loc {CampX} {CampY} radius {Radius} zradius {ZRadius}].ID}}" );
                    }
                    else
                    {
                        id = MQ.Query<int>($"${{NearestSpawn[{i},npc targetable radius {Radius} zradius {ZRadius}].ID}}" );
                    }
                }
                catch
                {
                    continue; // skip if query fails
                }

                if (id <= 0) continue;

                // Skip temporarily ignored spawns
                if (_tempIgnoreUntil.ContainsKey(id)) continue;

                // Use enhanced validation instead of basic checks
                if (!IsValidCombatTarget(id)) continue;

                // Auto-ignore quest/mission NPCs with surname
                try
                {
                    if (MQ.Query<int>($"${{Spawn[id {id}].Surname.Length}}") > 0) continue;
                }
                catch { continue; }

                // Previously skipped candidates when any nearby PCs were within 75 units to avoid KSing.
                // That behavior has been removed so Hunt can operate with boxed groups nearby.

                // Include/Exclude name filters
                string name = "";
                try
                {
                    name = MQ.Query<string>($"${{Spawn[id {id}].CleanName}}" );
                }
                catch { continue; }
                
                // Apply filters
                if (!PassesPullFilters(name)) continue;
                if (IsIgnored(name) || MatchesIgnore(name)) 
                {
                    MQ.Write($"Hunt: Ignoring {name} (on ignore list)");
                    continue;
                }

                // Prefer pathable targets with shortest path length; enforce MaxPathRange if configured
                bool pathExists = false;
                double navLen = 0;
                try { navLen = MQ.Query<double>($"${{Navigation.PathLength[id {id}]}}" ); pathExists = navLen > 0; } catch { pathExists = false; }
                if (!pathExists)
                {
                    try { pathExists = MQ.Query<bool>($"${{Navigation.PathExists[id {id}]}}" ); } catch { pathExists = false; }
                }
                if (pathExists)
                {
                    anyPathable = true;
                    if (MaxPathRange > 0 && navLen > MaxPathRange) continue;
                }

                if (!pathExists && anyPathable)
                {
                    // If we've already found a pathable candidate, skip non-pathable ones
                    continue;
                }

                double score = navLen;
                if (score <= 0)
                {
                    try { score = MQ.Query<double>($"${{Spawn[id {id}].Distance3D}}" ); } catch { score = 0; }
                }
                if (score <= 0) continue;

                // Collect candidate for UI snapshot
                try
                {
                    var cand = new HuntCandidate
                    {
                        ID = id,
                        Name = name,
                        Level = MQ.Query<int>($"${{Spawn[id {id}].Level}}"),
                        Distance = MQ.Query<double>($"${{Spawn[id {id}].Distance3D}}"),
                        PathLen = navLen,
                        Loc = MQ.Query<string>($"${{Spawn[id {id}].LocYXZ}}") ?? string.Empty,
                        Con = MQ.Query<string>($"${{Spawn[id {id}].ConColor}}") ?? string.Empty
                    };
                    candidateList.Add(cand);
                }
                catch { }

                if (score < bestPath)
                {
                    bestPath = score;
                    bestId = id;
                }
            }

            // Store candidate snapshot (top N by PathLen/Distance)
            try
            {
                _lastCandidates.Clear();
                if (candidateList.Count > 0)
                {
                    foreach (var c in candidateList
                        .OrderBy(c => c.PathLen > 0 ? c.PathLen : c.Distance)
                        .Take(10))
                    {
                        _lastCandidates.Add(c);
                    }
                }
                _candidatesUpdatedAt = Core.StopWatch.ElapsedMilliseconds;
            }
            catch { }

            if (bestId > 0)
            {
                TargetID = bestId;
                TargetName = GetSpawnName(bestId);
                Status = string.IsNullOrEmpty(TargetName) ? "Pulling" : $"Pulling {TargetName}";
                _log.Write($"Hunt: State -> Acquired new target {TargetID} ({TargetName}) at distance {bestPath:0}");
                DebugLog($"SCAN: acquired {TargetID} ({TargetName}) path={bestPath:0}");
                // Set target non-blocking to avoid UI stalls
                TrySetTargetNonBlocking(TargetID);
                ResetNoPathTracking(TargetID);
                // Immediately retarget navigation to the new TargetID. Only stop nav if we own it.
                if (HuntStateMachine.IsNavigationOwned)
                {
                    try { MQ.Cmd("/nav stop"); } catch { }
                }
                _navTargetID = 0;
                _nextNavAt = 0;
                StartNavNonBlocking(TargetID);
            }
            else
            {
                if (Status != "No targets found")
                {
                    _log.Write("Hunt: State -> No targets found");
                    Status = "No targets found";
                }
                // If camp is set, optionally move back to camp
                if (CampOn && !HuntFromPlayer && !Movement.IsNavigating())
                {
                    e3util.TryMoveToLoc(CampX, CampY, CampZ, 10, 3000);
                }
            }
        }

        // Issue a non-blocking nav command to MQ2Nav to prevent UI freezes
        private static void StartNavNonBlocking(int spawnId)
        {
            StartNavNonBlocking(spawnId, -1);
        }

        private static bool CanNavNow(out string because)
        {
            because = null;
            UpdateSmartLootTelemetry();

            // Gate nav only on HasNewCorpses AND IsPeerTriggered.
            // Do NOT block nav due to Processing/State/CorpseCount/SafeToLoot by themselves.
            if (_slHasNewCorpses || _slIsPeerTriggered)
            {
                because = $"SmartLoot active (NewCorpses={_slHasNewCorpses}, PeerTriggered={_slIsPeerTriggered})";
                if (HuntStateMachine.IsNavigationOwned)
                    HuntStateMachine.ReleaseNavigationControl("SmartLoot owns nav");
                return false;
            }

            // Acquire nav only when clear
            if (!HuntStateMachine.IsNavigationOwned)
                HuntStateMachine.RequestNavigationControl("Hunt");

            return true;
        }

        // Issue a non-blocking nav command to MQ2Nav with optional override stop distance
        private static void StartNavNonBlocking(int spawnId, int overrideStopDistance)
        {
            if (spawnId <= 0) return;
            
            // Only navigate if we own navigation control
            if (!HuntStateMachine.IsNavigationOwned)
            {
                _log.Write($"Hunt: Cannot navigate to {spawnId} - navigation control not owned");
                DebugLog($"NAV: denied to {spawnId} (no nav control)");
                return;
            }
            
            try
            {
                // Only attempt if a path exists to reduce churn
                bool pathExists = MQ.Query<bool>($"${{Navigation.PathExists[id {spawnId}]}}");
                if (!pathExists)
                {
                    OnNoNavPath(spawnId);
                    return;
                }

                // Reset no-path tracking when we do have a path
                if (_noPathTarget != spawnId || _noPathAttempts > 0)
                {
                    ResetNoPathTracking(spawnId);
                }

                int stopDist = overrideStopDistance > 0 ? overrideStopDistance : E3.GeneralSettings.Movement_NavStopDistance;
                try
                {
                    if (overrideStopDistance <= 0 && string.Equals(PullMethod, "Ranged", StringComparison.OrdinalIgnoreCase))
                    {
                        int baseRange = MQ.Query<int>("${Me.Inventory[ranged].Range}");
                        bool isArchery = MQ.Query<bool>("${Me.Inventory[ranged].Type.Find[Archery]}");
                        int maxRange = isArchery ? baseRange * 2 : baseRange;
                        double factor = Math.Max(0.3, Math.Min(0.9, RangedApproachFactor));
                        int desired = (int)Math.Max(35, maxRange * factor);
                        if (desired > 0) stopDist = desired;
                    }
                }
                catch { }

                if (CanNavNow(out var whyNot))
                {
                    MQ.Cmd($"/nav id {spawnId}");
                    _nextNavAt = Core.StopWatch.ElapsedMilliseconds + _navCooldownMs;
                }
                else
                {
                    DebugLog($"Nav suppressed: {whyNot}");
                    return; // skip this tick
                }
                _navTargetID = spawnId;
            }
            catch (Exception ex)
            {
                _log.Write($"Hunt: Error starting navigation to {spawnId}: {ex.Message}");
                DebugLog($"NAV: error starting nav to {spawnId}: {ex.Message}");
            }
        }

        // Handle repeated cases where MQ2Nav reports no path to the current target
        private static void OnNoNavPath(int spawnId)
        {
            // Increment per-target failure count
            if (_noPathTarget != spawnId)
            {
                _noPathTarget = spawnId;
                _noPathAttempts = 1;
            }
            else
            {
                _noPathAttempts++;
            }

            _log.Write($"Hunt: No nav path to id {spawnId} (attempt {_noPathAttempts}/{Math.Max(1, NoPathMaxAttempts)})");
            DebugLog($"NAV: no path to {spawnId} ({_noPathAttempts}/{Math.Max(1, NoPathMaxAttempts)})");

            // If we've failed enough times, give up, ignore temporarily, and rescan
            if (_noPathAttempts >= Math.Max(1, NoPathMaxAttempts))
            {
                if (HuntStateMachine.IsNavigationOwned)
                {
                    try { MQ.Cmd("/nav stop"); } catch { }
                }
                _navTargetID = 0;

                // Temp-ignore this spawn id to avoid immediate re-selection
                int ignoreSec = Math.Max(1, NoPathIgnoreDurationSec);
                try { _tempIgnoreUntil[spawnId] = Core.StopWatch.ElapsedMilliseconds + ignoreSec * 1000L; } catch { }

                if (TargetID == spawnId)
                {
                    TargetID = 0;
                    TargetName = string.Empty;
                }

                // Allow immediate rescan and transition state
                _nextScanAt = 0;
                Status = "No path to target - rescanning";
                _log.Write($"Hunt: Clearing stuck target {spawnId} and rescanning (ignored for {ignoreSec}s)");
                DebugLog($"NAV: clear stuck target {spawnId}, ignore {ignoreSec}s, rescan");
                ResetNoPathTracking(0);
                HuntStateMachine.TransitionTo(HuntState.Scanning, "No path to target");
            }
        }

        // Non-blocking target acquisition (no MQ.Delay)
        private static void TrySetTargetNonBlocking(int spawnId)
        {
            if (spawnId <= 0) return;
            try
            {
                if (MQ.Query<int>($"${{SpawnCount[id {spawnId}]}}") == 0) return;
                MQ.Cmd($"/squelch /target id {spawnId}");
                DebugLog($"TARGET: EQ target -> {spawnId}");
            }
            catch { }
        }

        private static bool PassesPullFilters(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return false;
            if (string.IsNullOrWhiteSpace(PullFilters) || PullFilters.Equals("ALL", StringComparison.OrdinalIgnoreCase)) return true;
            var parts = PullFilters.Split(new[] { '|'}, StringSplitOptions.RemoveEmptyEntries);
            foreach (var p in parts)
            {
                if (name.IndexOf(p.Trim(), StringComparison.OrdinalIgnoreCase) >= 0) return true;
            }
            return false;
        }

        private static bool MatchesIgnore(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return false;
            if (string.IsNullOrWhiteSpace(IgnoreFilters) || IgnoreFilters.Equals("NONE", StringComparison.OrdinalIgnoreCase)) return false;
            var parts = IgnoreFilters.Split(new[] { '|'}, StringSplitOptions.RemoveEmptyEntries);
            foreach (var p in parts)
            {
                if (name.IndexOf(p.Trim(), StringComparison.OrdinalIgnoreCase) >= 0) return true;
            }
            return false;
        }

        private static bool IsIgnored(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return false;
            string trimmedName = name.Trim();
            
            // Get current zone ignore list
            var currentIgnoreList = GetCurrentZoneIgnoreList();
            return currentIgnoreList.Contains(trimmedName);
        }

        public static bool AddIgnoreName(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return false;
            name = name.Trim();
            
            // Get current zone ignore list
            var currentIgnoreList = GetCurrentZoneIgnoreList();
            
            if (currentIgnoreList.Add(name))
            {
                string zone = GetCurrentZone();
                MQ.Write($"Hunt: Added '{name}' to ignore list for zone {zone} ({currentIgnoreList.Count} total for this zone)");
                try { SaveIgnoreList(); } catch { }
                return true;
            }
            else
            {
                MQ.Write($"Hunt: '{name}' already in ignore list for current zone");
                return false;
            }
        }

        public static bool RemoveIgnoreName(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return false;
            name = name.Trim();
            
            // Get current zone ignore list
            var currentIgnoreList = GetCurrentZoneIgnoreList();
            
            if (currentIgnoreList.Remove(name))
            {
                string zone = GetCurrentZone();
                MQ.Write($"Hunt: Removed '{name}' from ignore list for zone {zone} ({currentIgnoreList.Count} remaining for this zone)");
                try { SaveIgnoreList(); } catch { }
                return true;
            }
            else
            {
                MQ.Write($"Hunt: '{name}' not found in ignore list for current zone");
                return false;
            }
        }

        public static List<string> GetIgnoreListSnapshot()
        {
            var currentIgnoreList = GetCurrentZoneIgnoreList();
            return new List<string>(currentIgnoreList);
        }

        // UI-safe cached accessors (no MQ queries; used by ImGui during render)
        public static string GetCurrentZoneCached()
        {
            return string.IsNullOrWhiteSpace(_currentZone) ? "Unknown" : _currentZone;
        }

        public static List<string> GetIgnoreListSnapshotCached()
        {
            var zone = GetCurrentZoneCached();
            if (string.IsNullOrWhiteSpace(zone)) return new List<string>();
            if (_zoneIgnoreLists.TryGetValue(zone, out var set)) return new List<string>(set);
            return new List<string>();
        }
        
        public static Dictionary<string, List<string>> GetAllZoneIgnoreListsSnapshot()
        {
            var result = new Dictionary<string, List<string>>();
            foreach (var kvp in _zoneIgnoreLists)
            {
                result[kvp.Key] = new List<string>(kvp.Value);
            }
            return result;
        }

        public static string GetCurrentZone()
        {
            try
            {
                return MQ.Query<string>("${Zone.ShortName}") ?? "Unknown";
            }
            catch
            {
                return "Unknown";
            }
        }

        // Updates the cached zone name without exposing MQ queries to UI code
        private static void UpdateCurrentZoneCached()
        {
            try
            {
                var z = MQ.Query<string>("${Zone.ShortName}") ?? "Unknown";
                if (!string.Equals(_currentZone, z, StringComparison.OrdinalIgnoreCase))
                {
                    _currentZone = z;
                }
            }
            catch { }
        }

        // Helper method to check if any connected peers are in a different zone or dead
        // Removed duplicate peer-zone status helper; logic lives in DetermineTargetState()

        // Helper method to check if a peer is in the same zone as the current character
        private static bool IsPeerInSameZone(string peerName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(peerName))
                    return false;

                // Skip self-check
                if (string.Equals(peerName, E3.CurrentName, StringComparison.OrdinalIgnoreCase))
                    return true;

                string currentZone = GetCurrentZone();
                string peerZone = E3.Bots.Query(peerName, "${Zone.ShortName}");
                
                // If we can't determine peer zone, assume they're not in the same zone
                if (string.IsNullOrEmpty(peerZone) || peerZone.Equals("NULL", StringComparison.OrdinalIgnoreCase))
                    return false;

                return string.Equals(peerZone, currentZone, StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception ex)
            {
                _log.Write($"Hunt: Error checking if peer {peerName} is in same zone: {ex.Message}");
                return false; // On error, assume they're not in the same zone for safety
            }
        }

        // Helper method to check if a peer is dead
        private static bool IsPeerDead(string peerName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(peerName))
                    return false;

                // Skip self-check
                if (string.Equals(peerName, E3.CurrentName, StringComparison.OrdinalIgnoreCase))
                    return E3._amIDead;

                string peerDead = E3.Bots.Query(peerName, "${Me.Dead}");
                return string.Equals(peerDead, "TRUE", StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception ex)
            {
                _log.Write($"Hunt: Error checking if peer {peerName} is dead: {ex.Message}");
                return false; // On error, assume they're not dead
            }
        }

        // Enhanced target validation function
        private static bool IsValidCombatTarget(int spawnId)
        {
            if (spawnId <= 0) return false;
            
            try
            {
                // Basic validity checks
                if (MQ.Query<bool>($"${{Spawn[id {spawnId}].Type.Equal[Corpse]}}")) return false;
                // Must be NPC
                if (!MQ.Query<bool>($"${{Spawn[id {spawnId}].Type.Equal[NPC]}}")) return false;
                // Not self
                try { if (MQ.Query<int>("${Me.ID}") == spawnId) return false; } catch { }
                // Must be targetable
                if (!MQ.Query<bool>($"${{Spawn[id {spawnId}].Targetable}}")) return false;
                
                // Do not gate validity on distance here; scanning and nav will handle approach.
                // Aggression and validity checks
                bool isAggressive = MQ.Query<bool>($"${{Spawn[id {spawnId}].Aggressive}}");
                bool isPet = MQ.Query<bool>($"${{Spawn[id {spawnId}].Type.Equal[Pet]}}");
                bool isMount = MQ.Query<bool>($"${{Spawn[id {spawnId}].Type.Equal[Mount]}}");
                
                if (isPet || isMount) return false;
                
                // Level/con check (optional, configurable)
                string conColor = MQ.Query<string>($"${{Spawn[id {spawnId}].ConColor}}");
                if (string.IsNullOrEmpty(conColor) || conColor.Equals("GREY", StringComparison.OrdinalIgnoreCase))
                    return false;
                    
                return true;
            }
            catch
            {
                return false;
            }
        }

        // Helper function to get spawn name safely
        private static string GetSpawnName(int spawnId)
        {
            if (spawnId <= 0) return string.Empty;
            try 
            { 
                return MQ.Query<string>($"${{Spawn[id {spawnId}].Name}}") ?? string.Empty; 
            }
            catch 
            { 
                return string.Empty; 
            }
        }

        // Enhanced XTarget acquisition with validation
        private static int AcquireNextValidXTarget()
        {
            int bestId = 0;
            double bestDist = double.MaxValue;
            int max = e3util.XtargetMax;
            
            for (int i = 1; i <= max; i++)
            {
                int id = 0;
                try { id = MQ.Query<int>($"${{Me.XTarget[{i}].ID}}"); } catch { id = 0; }
                if (id <= 0) continue;
                
                // Use enhanced validation
                if (!IsValidCombatTarget(id)) continue;
                
                bool pathExists = false;
                try { pathExists = MQ.Query<bool>($"${{Navigation.PathExists[id {id}]}}"); } catch { pathExists = false; }
                if (!pathExists) continue;

                double dist = 0;
                try { dist = MQ.Query<double>($"${{Spawn[id {id}].Distance3D}}" ); } catch { dist = 0; }
                if (dist <= 0) continue;

                if (dist < bestDist)
                {
                    bestDist = dist;
                    bestId = id;
                }
            }
            return bestId;
        }

        // Better state transition validation
        private static bool ShouldTransitionToCombat()
        {
            // Multiple validation checks before entering combat state
            if (TargetID <= 0) return false;
            
            bool targetValid = IsValidCombatTarget(TargetID);
            bool inCombatRange = IsInCombatRange(TargetID);
            bool targetAggressive = MQ.Query<bool>($"${{Spawn[id {TargetID}].Aggressive}}");
            
            // Only transition to combat if we have a valid, aggressive target in range
            return targetValid && inCombatRange && (targetAggressive || Basics.InCombat());
        }

        // Check if target is in combat range
        private static bool IsInCombatRange(int spawnId)
        {
            if (spawnId <= 0) return false;
            try
            {
                double dist = MQ.Query<double>($"${{Spawn[id {spawnId}].Distance3D}}");
                int meleeRange = MQ.Query<int>($"${{Spawn[id {spawnId}].MaxRangeTo}}");
                if (meleeRange <= 0) meleeRange = 25;
                return dist > 0 && dist <= meleeRange;
            }
            catch
            {
                return false;
            }
        }

        private static void UpdateSmartLootState()
        {
            try
            {
                // Check if SmartLoot plugin is loaded
                bool smartLootLoaded = MQ.Query<bool>("${Plugin[MQ2SmartLoot]}");
                if (!smartLootLoaded)
                {
                    SmartLootState = "NotLoaded";
                    SmartLootMode = "Disabled";
                    SmartLootActive = false;
                    // Publish for peers (support multiple legacy keys)
                    E3Core.Server.PubServer.AddTopicMessage("${Data.Hunt.SmartLootState}", SmartLootState);
                    E3Core.Server.PubServer.AddTopicMessage("${Data.SmartLootState}", SmartLootState);
                    E3Core.Server.PubServer.AddTopicMessage("${Data.Hunt_SmartLootState}", SmartLootState);
                    E3Core.Server.PubServer.AddTopicMessage("${Data.Hunt.SmartLootActive}", SmartLootActive ? "1" : "0");
                    E3Core.Server.PubServer.AddTopicMessage("${Data.Hunt.SmartLootMode}", SmartLootMode);
                    return;
                }

                // Get current SmartLoot state
                string currentState = MQ.Query<string>("${SmartLoot.State}");
                string currentMode = MQ.Query<string>("${SmartLoot.Mode}");
                bool isProcessing = false;
                bool needsDecision = false;
                bool lootWindowOpen = false;
                try { isProcessing = MQ.Query<bool>("${SmartLoot.IsProcessing}"); } catch { }
                try { needsDecision = MQ.Query<bool>("${SmartLoot.NeedsDecision}"); } catch { }
                try { lootWindowOpen = MQ.Query<bool>("${SmartLoot.LootWindowOpen}"); } catch { }

                SmartLootState = currentState ?? "Unknown";
                SmartLootMode = currentMode ?? "Disabled";
                // Consider active if state not Idle, or engine signals active work/decision/loot window
                SmartLootActive =
                    (!string.IsNullOrEmpty(SmartLootState) && !SmartLootState.Equals("Idle", StringComparison.OrdinalIgnoreCase))
                    || isProcessing || needsDecision || lootWindowOpen;

                // Publish for peers (support multiple legacy keys)
                E3Core.Server.PubServer.AddTopicMessage("${Data.Hunt.SmartLootState}", SmartLootState);
                E3Core.Server.PubServer.AddTopicMessage("${Data.SmartLootState}", SmartLootState);
                E3Core.Server.PubServer.AddTopicMessage("${Data.Hunt_SmartLootState}", SmartLootState);
                E3Core.Server.PubServer.AddTopicMessage("${Data.Hunt.SmartLootActive}", SmartLootActive ? "1" : "0");
                E3Core.Server.PubServer.AddTopicMessage("${Data.Hunt.SmartLootMode}", SmartLootMode);
            }
            catch (Exception ex)
            {
                _log.Write($"Hunt: Error updating SmartLoot state: {ex.Message}");
                SmartLootState = "Error";
                SmartLootMode = "Disabled";
                SmartLootActive = false;
                // Publish error state for visibility (support multiple legacy keys)
                E3Core.Server.PubServer.AddTopicMessage("${Data.Hunt.SmartLootState}", SmartLootState);
                E3Core.Server.PubServer.AddTopicMessage("${Data.SmartLootState}", SmartLootState);
                E3Core.Server.PubServer.AddTopicMessage("${Data.Hunt_SmartLootState}", SmartLootState);
                E3Core.Server.PubServer.AddTopicMessage("${Data.Hunt.SmartLootActive}", SmartLootActive ? "1" : "0");
                E3Core.Server.PubServer.AddTopicMessage("${Data.Hunt.SmartLootMode}", SmartLootMode);
            }
        }

        private static HashSet<string> GetCurrentZoneIgnoreList()
        {
            string currentZone = GetCurrentZone();
            
            // Update cached current zone
            if (_currentZone != currentZone)
            {
                _currentZone = currentZone;
                _log.Write($"Hunt: Zone changed to {currentZone}");
            }
            
            if (!_zoneIgnoreLists.ContainsKey(currentZone))
            {
                _zoneIgnoreLists[currentZone] = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            }
            
            return _zoneIgnoreLists[currentZone];
        }

        private static void LoadIgnoreList()
        {
            _zoneIgnoreLists.Clear();
            if (string.IsNullOrEmpty(_ignoreListPath)) return;
            if (!File.Exists(_ignoreListPath)) return;
            
            foreach (var line in File.ReadAllLines(_ignoreListPath))
            {
                var v = (line ?? string.Empty).Trim();
                if (v.Length == 0) continue;
                if (v.StartsWith("#") || v.StartsWith(";")) continue;
                
                // Check for zone-specific format: [ZoneName]MobName
                if (v.StartsWith("[") && v.Contains("]"))
                {
                    int closeBracket = v.IndexOf(']');
                    if (closeBracket > 1 && closeBracket < v.Length - 1)
                    {
                        string zone = v.Substring(1, closeBracket - 1);
                        string mobName = v.Substring(closeBracket + 1);
                        
                        if (!string.IsNullOrWhiteSpace(zone) && !string.IsNullOrWhiteSpace(mobName))
                        {
                            if (!_zoneIgnoreLists.ContainsKey(zone))
                            {
                                _zoneIgnoreLists[zone] = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                            }
                            _zoneIgnoreLists[zone].Add(mobName);
                        }
                    }
                }
                else
                {
                    // Legacy format - add to current zone or "global" zone
                    string currentZone = GetCurrentZone();
                    if (!_zoneIgnoreLists.ContainsKey(currentZone))
                    {
                        _zoneIgnoreLists[currentZone] = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    }
                    _zoneIgnoreLists[currentZone].Add(v);
                }
            }
        }

        private static void SaveIgnoreList()
        {
            if (string.IsNullOrEmpty(_ignoreListPath)) return;
            try
            {
                var dir = Path.GetDirectoryName(_ignoreListPath);
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            }
            catch { }
            try
            {
                var lines = new List<string>();
                lines.Add("# Zone-specific Hunt ignore list");
                lines.Add("# Format: [ZoneName]MobName");
                lines.Add("");
                
                // Sort zones alphabetically
                var sortedZones = _zoneIgnoreLists.Keys.ToList();
                sortedZones.Sort(StringComparer.OrdinalIgnoreCase);
                
                foreach (var zone in sortedZones)
                {
                    var mobList = _zoneIgnoreLists[zone].ToList();
                    mobList.Sort(StringComparer.OrdinalIgnoreCase);
                    
                    lines.Add($"# Zone: {zone} ({mobList.Count} mobs)");
                    foreach (var mob in mobList)
                    {
                        lines.Add($"[{zone}]{mob}");
                    }
                    lines.Add("");
                }
                
                File.WriteAllLines(_ignoreListPath, lines);
            }
            catch { }
        }

        private static void LoadHuntSettings()
        {
            try
            {
                if (string.IsNullOrEmpty(_huntSettingsPath)) return;
                if (!File.Exists(_huntSettingsPath)) return;
                var parser = e3util.CreateIniParser();
                IniData data = parser.ReadFile(_huntSettingsPath);
                var sec = data["Hunt"];
                if (sec == null) return;
                string v;
                v = sec["PullMethod"]; if (!string.IsNullOrWhiteSpace(v)) PullMethod = NormalizeMethod(v);
                v = sec["PullSpell"]; if (!string.IsNullOrWhiteSpace(v)) PullSpell = v;
                v = sec["PullItem"]; if (!string.IsNullOrWhiteSpace(v)) PullItem = v;
                v = sec["PullAA"]; if (!string.IsNullOrWhiteSpace(v)) PullAA = v;
                v = sec["PullDisc"]; if (!string.IsNullOrWhiteSpace(v)) PullDisc = v;
                v = sec["MaxPathRange"]; if (!string.IsNullOrWhiteSpace(v) && int.TryParse(v, out var mpr)) MaxPathRange = Math.Max(0, mpr);
                v = sec["PullIgnoreTimeSec"]; if (!string.IsNullOrWhiteSpace(v) && int.TryParse(v, out var pits)) PullIgnoreTimeSec = Math.Max(0, pits);
                v = sec["TempIgnoreDurationSec"]; if (!string.IsNullOrWhiteSpace(v) && int.TryParse(v, out var tids)) TempIgnoreDurationSec = Math.Max(1, tids);
                v = sec["RangedApproachFactor"]; if (!string.IsNullOrWhiteSpace(v) && double.TryParse(v, out var raf)) RangedApproachFactor = Math.Max(0.3, Math.Min(0.9, raf));
                v = sec["PullApproachDistance"]; if (!string.IsNullOrWhiteSpace(v) && int.TryParse(v, out var pad)) PullApproachDistance = Math.Max(20, pad);
                v = sec["NoPathMaxAttempts"]; if (!string.IsNullOrWhiteSpace(v) && int.TryParse(v, out var npma)) NoPathMaxAttempts = Math.Max(1, npma);
                v = sec["NoPathIgnoreDurationSec"]; if (!string.IsNullOrWhiteSpace(v) && int.TryParse(v, out var npids)) NoPathIgnoreDurationSec = Math.Max(1, npids);
                v = sec["HuntFromPlayer"]; if (!string.IsNullOrWhiteSpace(v)) bool.TryParse(v, out HuntFromPlayer);
            }
            catch { }
        }

        public static void SaveHuntPullSettings()
        {
            try
            {
                if (string.IsNullOrEmpty(_huntSettingsPath)) return;
                var dir = Path.GetDirectoryName(_huntSettingsPath);
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

                var parser = e3util.CreateIniParser();
                IniData data = File.Exists(_huntSettingsPath) ? parser.ReadFile(_huntSettingsPath) : new IniData();
                if (!data.Sections.ContainsSection("Hunt")) data.Sections.AddSection("Hunt");
                var sec = data["Hunt"];
                sec["PullMethod"] = PullMethod ?? "None";
                sec["PullSpell"] = PullSpell ?? string.Empty;
                sec["PullItem"] = PullItem ?? string.Empty;
                sec["PullAA"] = PullAA ?? string.Empty;
                sec["PullDisc"] = PullDisc ?? string.Empty;
                sec["MaxPathRange"] = MaxPathRange.ToString();
                sec["PullIgnoreTimeSec"] = PullIgnoreTimeSec.ToString();
                sec["TempIgnoreDurationSec"] = TempIgnoreDurationSec.ToString();
                sec["RangedApproachFactor"] = RangedApproachFactor.ToString(System.Globalization.CultureInfo.InvariantCulture);
                sec["PullApproachDistance"] = Math.Max(20, PullApproachDistance).ToString();
                sec["NoPathMaxAttempts"] = Math.Max(1, NoPathMaxAttempts).ToString();
                sec["NoPathIgnoreDurationSec"] = Math.Max(1, NoPathIgnoreDurationSec).ToString();
                sec["HuntFromPlayer"] = HuntFromPlayer ? "true" : "false";
                parser.WriteFile(_huntSettingsPath, data);
            }
            catch { }
        }

        private static bool IsValidPullMethod(string m)
        {
            switch (NormalizeMethod(m))
            {
                case "None": case "Ranged": case "Spell": case "Item": case "AA": case "Disc": case "Attack": return true;
                default: return false;
            }
        }
        private static string NormalizeMethod(string m)
        {
            if (string.IsNullOrWhiteSpace(m)) return "None";
            m = m.Trim();
            if (m.Equals("none", StringComparison.OrdinalIgnoreCase)) return "None";
            if (m.Equals("ranged", StringComparison.OrdinalIgnoreCase)) return "Ranged";
            if (m.Equals("spell", StringComparison.OrdinalIgnoreCase)) return "Spell";
            if (m.Equals("item", StringComparison.OrdinalIgnoreCase)) return "Item";
            if (m.Equals("aa", StringComparison.OrdinalIgnoreCase)) return "AA";
            if (m.Equals("disc", StringComparison.OrdinalIgnoreCase) || m.Equals("discipline", StringComparison.OrdinalIgnoreCase)) return "Disc";
            if (m.Equals("attack", StringComparison.OrdinalIgnoreCase)) return "Attack";
            return m;
        }

        private static void TryPull()
        {
            // cooldown
            if (Core.StopWatch.ElapsedMilliseconds < _nextPullAt) return;

            if (string.Equals(PullMethod, "None", StringComparison.OrdinalIgnoreCase)) return;
            if (TargetID <= 0) return;

            // skip if casting or stunned
            if (MQ.Query<int>("${Me.Casting.ID}") > 0) return;
            if (MQ.Query<bool>("${Me.Stunned}")) return;

            // Ensure current EQ target matches our stored TargetID before pulling
            try
            {
                int curT = MQ.Query<int>("${Target.ID}");
                if (curT != TargetID)
                {
                    TrySetTargetNonBlocking(TargetID);
                    return; // wait until next tick to pull on the correct target
                }
            }
            catch { }

            bool los = MQ.Query<bool>($"${{Spawn[id {TargetID}].LineOfSight}}" );
            double dist = MQ.Query<double>($"${{Spawn[id {TargetID}].Distance3D}}" );
            if (!los || dist <= 0) return;

            // attempt based on method
            switch (NormalizeMethod(PullMethod))
            {
                case "Ranged":
                    TryPull_Ranged(dist);
                    break;
                case "Spell":
                    TryPull_Spell();
                    break;
                case "Item":
                    TryPull_Item();
                    break;
                case "AA":
                    TryPull_AA();
                    break;
                case "Disc":
                    TryPull_Disc();
                    break;
                case "Attack":
                    TryPull_Attack();
                    break;
            }
        }

        // Choose the closest valid XTarget NPC with a navigation path - Enhanced version
        private static int AcquireNextFromXTarget()
        {
            int bestId = 0;
            double bestDist = double.MaxValue;
            int max = e3util.XtargetMax;
            for (int i = 1; i <= max; i++)
            {
                int id = 0;
                try { id = MQ.Query<int>($"${{Me.XTarget[{i}].ID}}"); } catch { id = 0; }
                if (id <= 0) continue;
                
                // Use enhanced validation for better target selection
                if (!IsValidCombatTarget(id)) continue;

                bool pathExists = false;
                try { pathExists = MQ.Query<bool>($"${{Navigation.PathExists[id {id}]}}"); } catch { pathExists = false; }
                if (!pathExists) continue;

                double dist = 0;
                try { dist = MQ.Query<double>($"${{Spawn[id {id}].Distance3D}}" ); } catch { dist = 0; }
                if (dist <= 0) continue;

                if (dist < bestDist)
                {
                    bestDist = dist;
                    bestId = id;
                }
            }
            return bestId;
        }

        private static void SetPullCooldown()
        {
            _nextPullAt = Core.StopWatch.ElapsedMilliseconds + _pullCooldownMs;
        }

        private static void TryPull_Ranged(double dist)
        {
            int baseRange = MQ.Query<int>("${Me.Inventory[ranged].Range}");
            bool isThrowing = MQ.Query<bool>("${Me.Inventory[ranged].Type.Find[Throwing]}");
            bool isArchery = MQ.Query<bool>("${Me.Inventory[ranged].Type.Find[Archery]}");
            bool hasArrow = MQ.Query<bool>("${Me.Inventory[ammo].Type.Find[Arrow]}");
            int maxRange = isArchery ? baseRange * 2 : baseRange;
            if (maxRange <= 0) maxRange = 200;
            int minRange = 35;

            if (dist >= minRange && dist <= maxRange)
            {
                if (isThrowing || (isArchery && hasArrow))
                {
                    MQ.Cmd("/ranged");
                    SetPullCooldown();
                }
            }
        }

        private static void TryPull_Spell()
        {
            if (string.IsNullOrWhiteSpace(PullSpell)) return;
            // use MQ2Cast if available
            MQ.Cmd($"/casting \"{PullSpell}\"");
            SetPullCooldown();
        }

        private static void TryPull_Item()
        {
            if (string.IsNullOrWhiteSpace(PullItem)) return;
            MQ.Cmd($"/casting \"{PullItem}\" item");
            SetPullCooldown();
        }

        private static void TryPull_AA()
        {
            if (string.IsNullOrWhiteSpace(PullAA)) return;
            int aaId = MQ.Query<int>($"${{Me.AltAbility[{PullAA}].ID}}" );
            bool ready = MQ.Query<bool>($"${{Me.AltAbilityReady[{PullAA}]}}" );
            if (aaId > 0 && ready)
            {
                MQ.Cmd($"/alt act {aaId}");
                SetPullCooldown();
            }
        }

        private static void TryPull_Disc()
        {
            if (string.IsNullOrWhiteSpace(PullDisc)) return;
            bool ready = MQ.Query<bool>($"${{Me.CombatAbilityReady[{PullDisc}]}}" );
            if (ready)
            {
                MQ.Cmd($"/disc {PullDisc}");
                SetPullCooldown();
            }
        }

        private static void TryPull_Attack()
        {
            MQ.Cmd("/attack on");
            SetPullCooldown();
        }
    }
}
