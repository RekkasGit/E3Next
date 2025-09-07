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
        }

        public static bool IsNavigationOwned => _navigationOwned;

        public static void TransitionTo(HuntState newState, string reason = "")
        {
            if (_currentState == newState && reason == _stateReason) return;

            _log.Write($"Hunt: State {_currentState} -> {newState} ({reason})");

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
                case HuntState.Paused: return "Paused";
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

        // Cached target name for UI (avoid MQ queries on UI thread)
        [ExposedData("Hunt", "TargetName")]
        public static string TargetName = string.Empty;

        // Pulling configuration
        [ExposedData("Hunt", "PullMethod")] // None|Ranged|Spell|Item|AA|Disc
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

        // Optional camp point
        [ExposedData("Hunt", "CampOn")]
        public static bool CampOn = false;

        [ExposedData("Hunt", "CampX")]
        public static int CampX = 0;
        [ExposedData("Hunt", "CampY")]
        public static int CampY = 0;
        [ExposedData("Hunt", "CampZ")]
        public static int CampZ = 0;

        private static long _nextTickAt = 0;
        private static readonly int _tickIntervalMs = 200; // snappy responsiveness
        private static long _nextScanAt = 0;
        private static readonly int _scanIntervalMs = 800; // throttle scanning a bit
        private static long _nextPullAt = 0;
        private static readonly int _pullCooldownMs = 2000;
        private static long _nextNavAt = 0;
        private static readonly int _navCooldownMs = 1000; // prevent rapid nav calls

        // Loot wait handling: pause scanning after combat ends until SmartLoot finishes
        private static int _lastXTargetCount = 0;
        private static bool _waitingForLoot = false;
        private static long _lootWaitStartMs = 0;
        private static readonly int _lootMinWaitMs = 2000;   // always wait at least 2s after XTarget clears
        private static readonly int _lootWaitFallbackMs = 10000; // fallback max wait

        // Zone-specific ignore list (zone -> HashSet of mob names)
        private static readonly Dictionary<string, HashSet<string>> _zoneIgnoreLists = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
        private static string _currentZone = string.Empty;
        private static string _ignoreListPath = string.Empty;
        private static string _huntSettingsPath = string.Empty;

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
            }
            catch { }
        }

        private static void RegisterCommands()
        {
            EventProcessor.RegisterCommand("/hunt", (x) =>
            {
                if (x.args.Count == 0)
                {
                    MQ.Write("Usage: /hunt [go|pause|on|off|radius <n>|zradius <n>|pull <patterns>|ignore <patterns>|ignoreadd [name]|ignorelist|ignoreclear|ignoreallzones|smartloot|camp [set|off]|pullmethod <type>|pullspell <name>|pullitem <name>|pullaa <name>|pulldisc <name>]");
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
            // Throttle work, including publishing SmartLoot state
            if (!e3util.ShouldCheck(ref _nextTickAt, _tickIntervalMs)) return;

            // Always publish SmartLoot state so peers can coordinate,
            // even when Hunt is disabled on this character
            UpdateSmartLootState();

            if (!Enabled)
            {
                HuntStateMachine.TransitionTo(HuntState.Disabled, "Hunt disabled");
                return;
            }

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

            // Track XTarget changes
            int currentXTargets = 0;
            try { currentXTargets = MQ.Query<int>("${Me.XTarget}"); } catch { currentXTargets = 0; }

            if (currentXTargets != _lastXTargetCount)
            {
                _log.Write($"Hunt: XTarget count changed {_lastXTargetCount} -> {currentXTargets}");
            }

            // Check if we should enter loot wait
            if (currentXTargets == 0 && _lastXTargetCount > 0)
            {
                if (ShouldEnterLootWait())
                {
                    _waitingForLoot = true;
                    _lootWaitStartMs = Core.StopWatch.ElapsedMilliseconds;
                    HuntStateMachine.TransitionTo(HuntState.WaitingForLoot, "XTarget cleared - entering loot wait");
                }
            }
            _lastXTargetCount = currentXTargets;

            // If waiting for loot, check if we should continue waiting
            if (_waitingForLoot)
            {
                if (!IsLootWaitComplete())
                {
                    string lootReason = GetLootWaitReason();
                    HuntStateMachine.TransitionTo(HuntState.WaitingForLoot, lootReason);
                    return;
                }
                else
                {
                    _waitingForLoot = false;
                    _log.Write("Hunt: Loot wait completed - resuming normal hunting");
                }
            }

            // Adopt XTarget as hunt target if needed
            if (TargetID <= 0 && currentXTargets > 0)
            {
                int nextTarget = AcquireNextFromXTarget();
                if (nextTarget > 0)
                {
                    TargetID = nextTarget;
                    try { TargetName = MQ.Query<string>($"${{Spawn[id {TargetID}].Name}}") ?? string.Empty; } catch { TargetName = string.Empty; }
                    HuntStateMachine.TransitionTo(HuntState.NavigatingToTarget, $"Adopted XTarget {TargetID}");
                    return;
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
                    TargetID = 0; 
                    TargetName = string.Empty;
                    _waitingForLoot = true;
                    _lootWaitStartMs = Core.StopWatch.ElapsedMilliseconds;
                    HuntStateMachine.TransitionTo(HuntState.WaitingForLoot, "Target became invalid");
                    return;
                }

                // Check for unexpected aggro
                string aggroCheck = CheckForUnexpectedAggro();
                if (!string.IsNullOrEmpty(aggroCheck))
                {
                    HuntStateMachine.TransitionTo(HuntState.InCombat, aggroCheck);
                    return;
                }

                // Determine if we should be pulling, navigating, or in combat
                double dist = MQ.Query<double>($"${{Spawn[id {TargetID}].Distance3D}}");
                int meleeRange = MQ.Query<int>($"${{Spawn[id {TargetID}].MaxRangeTo}}");
                if (meleeRange <= 0) meleeRange = 25;

                if (dist > 0 && dist <= meleeRange)
                {
                    // Close enough for combat
                    if (AutoAssistAtMelee && !Assist.IsAssisting)
                    {
                        MQ.Cmd("/assistme /all");
                        MQ.Delay(500);
                        MQ.Cmd("/face");
                    }
                    HuntStateMachine.TransitionTo(HuntState.InCombat, "At melee range");
                    return;
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
            if (!HuntStateMachine.RequestNavigationControl("NavigateToTarget"))
            {
                // Can't get navigation control, check if SmartLoot needs it
                if (IsSmartLootActive())
                {
                    HuntStateMachine.TransitionTo(HuntState.WaitingForLoot, "SmartLoot needs navigation");
                    return;
                }
            }

            // Navigate towards target with cooldown to prevent spam
            if (e3util.ShouldCheck(ref _nextNavAt, _navCooldownMs))
            {
                StartNavNonBlocking(TargetID);
            }
        }

        private static void HandlePullingTarget()
        {
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
        }

        private static void HandleNavigatingToCamp()
        {
            if (!HuntStateMachine.RequestNavigationControl("NavigateToCamp"))
            {
                HuntStateMachine.TransitionTo(HuntState.Paused, "Can't get navigation control for camp");
                return;
            }

            if (CampOn && !Movement.IsNavigating())
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
            bool timeDelayPassed = elapsed >= lootWaitTime;
            bool smartLootIdle = true;

            try
            {
                bool smartLootLoaded = MQ.Query<bool>("${Plugin[MQ2SmartLoot]}");
                if (smartLootLoaded)
                {
                    string smartLootState = MQ.Query<string>("${SmartLoot.State}");
                    smartLootIdle = smartLootState.Equals("Idle", StringComparison.OrdinalIgnoreCase);

                    // If SmartLoot detects combat, exit loot wait immediately
                    if (smartLootState.Equals("CombatDetected", StringComparison.OrdinalIgnoreCase))
                    {
                        _waitingForLoot = false;
                        _log.Write("Hunt: SmartLoot detected combat - exiting loot wait");
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                _log.Write($"Hunt: Error checking SmartLoot during loot wait: {ex.Message}");
                smartLootIdle = true;
            }

            // Fallback timeout
            bool fallbackTimeout = elapsed >= _lootWaitFallbackMs;
            return (timeDelayPassed && smartLootIdle) || fallbackTimeout;
        }

        private static string GetLootWaitReason()
        {
            long elapsed = Core.StopWatch.ElapsedMilliseconds - _lootWaitStartMs;
            int lootWaitTime = E3.GeneralSettings.Loot_TimeToWaitAfterAssist;
            bool timeDelayPassed = elapsed >= lootWaitTime;

            try
            {
                bool smartLootLoaded = MQ.Query<bool>("${Plugin[MQ2SmartLoot]}");
                if (smartLootLoaded)
                {
                    string smartLootState = MQ.Query<string>("${SmartLoot.State}");
                    bool smartLootIdle = smartLootState.Equals("Idle", StringComparison.OrdinalIgnoreCase);

                    if (!smartLootIdle)
                        return $"Waiting for SmartLoot ({smartLootState})";
                }
            }
            catch { }

            if (!timeDelayPassed)
                return $"Waiting for loot delay ({elapsed}ms/{lootWaitTime}ms)";

            return "Waiting for loot";
        }

        private static bool IsSmartLootActive()
        {
            try
            {
                bool smartLootLoaded = MQ.Query<bool>("${Plugin[MQ2SmartLoot]}");
                if (!smartLootLoaded) return false;

                string smartLootState = MQ.Query<string>("${SmartLoot.State}");
                return !smartLootState.Equals("Idle", StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
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

                default:
                    return false;
            }
        }

        private static string CheckForUnexpectedAggro()
        {
            try
            {
                // Only check for unexpected aggro if we're currently navigating
                bool isNavigating = MQ.Query<bool>("${Navigation.Active}");
                if (!isNavigating) return string.Empty; // Not navigating, no need to check
                
                int currentXTargets = MQ.Query<int>("${Me.XTarget}");
                if (currentXTargets == 0) return string.Empty; // No XTargets, we're good
                
                // If we only have 1 XTarget and it's our intended target, we're good
                if (currentXTargets == 1)
                {
                    int xTargetID = MQ.Query<int>("${Me.XTarget[1].ID}");
                    if (xTargetID == TargetID) return string.Empty;
                    
                    // We have 1 XTarget but it's NOT our intended target - this could be unexpected aggro
                    // But only if we haven't reached our target yet (distance check)
                    try
                    {
                        double distToTarget = MQ.Query<double>($"${{Spawn[id {TargetID}].Distance3D}}");
                        if (distToTarget <= 50) return string.Empty; // Close to target, probably expected combat
                    }
                    catch { return string.Empty; } // If we can't check distance, don't stop
                }
                
                // Check if we have multiple XTargets or unexpected ones, but be smarter about it
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
                        
                        // Skip corpses
                        if (MQ.Query<bool>($"${{Spawn[id {xTargetID}].Type.Equal[Corpse]}}")) continue;
                        
                        // Check distance - if we're close to our target, additional mobs might be expected
                        double distToTarget = MQ.Query<double>($"${{Spawn[id {TargetID}].Distance3D}}");
                        if (distToTarget <= 50) continue; // Close to target, additional aggro might be normal
                        
                        // This is potentially an unexpected mob, but check if it's close to us (might be a legitimate add)
                        double mobDist = MQ.Query<double>($"${{Spawn[id {xTargetID}].Distance3D}}");
                        if (mobDist > 100) continue; // Too far away, probably not our problem
                        
                        string mobName = MQ.Query<string>($"${{Spawn[id {xTargetID}].CleanName}}");
                        if (!string.IsNullOrEmpty(mobName))
                        {
                            unexpectedMobs.Add($"{mobName}({xTargetID})");
                        }
                    }
                    catch (Exception ex)
                    {
                        MQ.Write($"Hunt: Error checking XTarget {i}: {ex.Message}");
                    }
                }
                
                // Only report unexpected aggro if we have adds AND we're not close to our intended target
                if (unexpectedMobs.Count > 0)
                {
                    return $"Unexpected aggro: {string.Join(", ", unexpectedMobs)}";
                }
                
                return string.Empty; // All XTargets are expected
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
                    bool smartLootLoaded = MQ.Query<bool>("${Plugin[MQ2SmartLoot]}");
                    if (smartLootLoaded)
                    {
                        string smartLootState = MQ.Query<string>("${SmartLoot.State}");
                        if (!smartLootState.Equals("Idle", StringComparison.OrdinalIgnoreCase))
                        {
                            return $"Waiting for local SmartLoot ({smartLootState})";
                        }
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
            int mobsInRadius = 0;
            try
            {
                if (CampOn)
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

            double bestPath = double.MaxValue;
            int bestId = 0;

            for (int i = 1; i <= mobsInRadius; i++)
            {
                int id = 0;
                try
                {
                    if (CampOn)
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

                // Filter out invalid types (pocketfarm excludes many item-like types; we focus on targetable NPC only)
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

                // Prefer lighter checks: ensure a path exists, then use straight-line distance as heuristic
                bool pathExists = false;
                try { pathExists = MQ.Query<bool>($"${{Navigation.PathExists[id {id}]}}"); } catch { pathExists = false; }
                if (!pathExists) continue;

                double dist3d = 0;
                try { dist3d = MQ.Query<double>($"${{Spawn[id {id}].Distance3D}}" ); } catch { continue; }
                if (dist3d <= 0) continue;
                if (dist3d < bestPath)
                {
                    bestPath = dist3d;
                    bestId = id;
                }
            }

            if (bestId > 0)
            {
                TargetID = bestId;
                try { TargetName = MQ.Query<string>($"${{Spawn[id {TargetID}].Name}}" ) ?? string.Empty; } catch { TargetName = string.Empty; }
                Status = string.IsNullOrEmpty(TargetName) ? "Pulling" : $"Pulling {TargetName}";
                _log.Write($"Hunt: State -> Acquired new target {TargetID} ({TargetName}) at distance {bestPath:0}");
                // Set target non-blocking to avoid UI stalls
                TrySetTargetNonBlocking(TargetID);
                if (e3util.ShouldCheck(ref _nextNavAt, _navCooldownMs))
                {
                    // fire-and-forget navigation to avoid blocking the main loop
                    StartNavNonBlocking(TargetID);
                }
            }
            else
            {
                if (Status != "No targets found")
                {
                    _log.Write("Hunt: State -> No targets found");
                    Status = "No targets found";
                }
                // If camp is set, optionally move back to camp
                if (CampOn && !Movement.IsNavigating())
                {
                    e3util.TryMoveToLoc(CampX, CampY, CampZ, 10, 3000);
                }
            }
        }

        // Issue a non-blocking nav command to MQ2Nav to prevent UI freezes
        private static void StartNavNonBlocking(int spawnId)
        {
            if (spawnId <= 0) return;
            
            // Only navigate if we own navigation control
            if (!HuntStateMachine.IsNavigationOwned)
            {
                _log.Write($"Hunt: Cannot navigate to {spawnId} - navigation control not owned");
                return;
            }
            
            try
            {
                // Only attempt if a path exists to reduce churn
                bool pathExists = MQ.Query<bool>($"${{Navigation.PathExists[id {spawnId}]}}");
                if (!pathExists) 
                { 
                    _log.Write($"Hunt: No nav path to id {spawnId}"); 
                    return; 
                }

                int stopDist = E3.GeneralSettings.Movement_NavStopDistance;
                MQ.Cmd($"/nav id {spawnId} distance={stopDist}");
            }
            catch (Exception ex)
            {
                _log.Write($"Hunt: Error starting navigation to {spawnId}: {ex.Message}");
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
                parser.WriteFile(_huntSettingsPath, data);
            }
            catch { }
        }

        private static bool IsValidPullMethod(string m)
        {
            switch (NormalizeMethod(m))
            {
                case "None": case "Ranged": case "Spell": case "Item": case "AA": case "Disc": return true;
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
            }
        }

        // Choose the closest valid XTarget NPC with a navigation path
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
                // skip corpses and non-NPCs
                try
                {
                    if (MQ.Query<bool>($"${{Spawn[id {id}].Type.Equal[Corpse]}}")) continue;
                    if (!MQ.Query<bool>($"${{Spawn[id {id}].Type.Equal[NPC]}}")) continue;
                    if (!MQ.Query<bool>($"${{Spawn[id {id}].Targetable}}")) continue;
                }
                catch { continue; }

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
    }
}
