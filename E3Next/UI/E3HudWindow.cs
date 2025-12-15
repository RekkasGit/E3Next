using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using E3Core.Data;
using E3Core.Processors;
using E3Core.Server;
using MonoCore;
using static MonoCore.EventProcessor;
using static MonoCore.E3ImGUI;

namespace E3Next.UI
{
    public static class E3HudWindow
    {
        private const string WindowName = "E3 HUD";
        private static bool _windowInitialized;
        private static bool _imguiContextReady;
        private const int TargetBuffSlots = 40;
        private const int TargetBuffIconsPerRow = 8;
        private const int PlayerBuffSlots = 40;
        private const int PlayerShortBuffSlots = 20;
        private const string ShortBuffWindowName = "E3 HUD - Short Buffs";
        private const string AaWindowName = "E3 HUD - AAs";
        private const string BotCastingWindowName = "E3 HUD - Connected Bots";
        private const string PlayerStatsWindowName = "E3 HUD - Player Stats";
        private const string TargetInfoWindowName = "E3 HUD - Target";
        private const string BuffListWindowName = "E3 HUD - Buffs";
        private static bool _showAaWindow = false;
        private static bool _showBotCastingWindow = false;
        private static bool _showCastingColumn = true;
        private static bool _showTargetColumn = true;
        private static bool _showEnduranceColumn = true;
        private static bool _showCombatStateColumn = true;
        private static bool _showPlayerStatsSection = true;
        private static bool _showTargetSection = true;
        private static bool _showBuffSection = true;
        private static bool _playerStatsPoppedOut;
        private static bool _targetInfoPoppedOut;
        private static bool _buffSectionPoppedOut;
        private static bool _playerStatsWindowDismissedBySystem;
        private static bool _targetWindowDismissedBySystem;
        private static bool _buffWindowDismissedBySystem;
        private static bool _hudVisibilityInitialized;
        private static bool _initialAaRefreshRequested;

        private const int PlayerStatsRefreshMs = 150;
        private const int TargetInfoRefreshMs = 120;
        private const int ZoneCountRefreshMs = 500;
        private const int TargetBuffRefreshMs = 300;
        private const int PlayerBuffRefreshMs = 800;
        private const int ShortBuffRefreshMs = 800;
        private const int BotDistanceRefreshMs = 200;
        private const int MemoryStatusRefreshMs = 1000;
        private const int BotEnduranceRefreshMs = 400;
        private const int CombatStateRefreshMs = 500;

        private static readonly PlayerStatsSnapshot _playerStatsSnapshot = new PlayerStatsSnapshot();
        private static long _nextPlayerStatsRefreshMs;

        private static readonly TargetInfoSnapshot _targetInfoSnapshot = new TargetInfoSnapshot();
        private static long _nextTargetInfoRefreshMs;

        private static int _cachedZonePcCount;
        private static long _nextZonePcCountRefreshMs;

        private static readonly List<BuffInfo> _targetBuffCache = new List<BuffInfo>();
        private static long _nextTargetBuffRefreshMs;
        private static readonly List<BuffInfo> _playerBuffCache = new List<BuffInfo>();
        private static long _nextPlayerBuffRefreshMs;
        private static readonly List<BuffInfo> _shortBuffCache = new List<BuffInfo>();
        private static long _nextShortBuffRefreshMs;

        private static readonly Dictionary<string, DistanceCache> _botDistanceCache = new Dictionary<string, DistanceCache>(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, ClassCache> _classCache = new Dictionary<string, ClassCache>(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, MemoryStatusCache> _memoryStatusCache = new Dictionary<string, MemoryStatusCache>(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, PercentCache> _endurancePercentCache = new Dictionary<string, PercentCache>(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, CombatStateCache> _combatStateCache = new Dictionary<string, CombatStateCache>(StringComparer.OrdinalIgnoreCase);
        private const int BotCastingRefreshMs = 300;
        private const int BotCastingFreshnessMs = 2500;
        private static readonly List<BotCastingEntry> _botCastingSnapshot = new List<BotCastingEntry>();
        private static long _nextBotCastingRefreshMs;

        [SubSystemInit]
        public static void Init()
        {
            if (Core._MQ2MonoVersion < 0.35m) return;
            EnsureHudVisibilitySettingsLoaded();
            // Register ImGui window using E3ImGUI system
            MonoCore.E3ImGUI.RegisterWindow(WindowName, RenderMainWindow);
            MonoCore.E3ImGUI.RegisterWindow(ShortBuffWindowName, RenderShortBuffWindow);
            MonoCore.E3ImGUI.RegisterWindow(AaWindowName, RenderAaWindow);
            MonoCore.E3ImGUI.RegisterWindow(BotCastingWindowName, RenderBotCastingWindow);
            MonoCore.E3ImGUI.RegisterWindow(PlayerStatsWindowName, RenderPlayerStatsWindow);
            MonoCore.E3ImGUI.RegisterWindow(TargetInfoWindowName, RenderTargetInfoWindow);
            MonoCore.E3ImGUI.RegisterWindow(BuffListWindowName, RenderBuffListWindow);

            // Default both windows to visible
            _windowInitialized = true;
            _imguiContextReady = true;
            MonoCore.E3ImGUI.imgui_Begin_OpenFlagSet(WindowName, true);
            MonoCore.E3ImGUI.imgui_Begin_OpenFlagSet(ShortBuffWindowName, true);
            MonoCore.E3ImGUI.imgui_Begin_OpenFlagSet(AaWindowName, false);
            MonoCore.E3ImGUI.imgui_Begin_OpenFlagSet(BotCastingWindowName, false);
            SyncPopoutWindows();

            // Register command to toggle window
            RegisterCommand("/e3hud", (x) =>
            {
                try
                {
                    if (!E3.MQ.Query<bool>("${Plugin[MQ2Mono]}"))
                    {
                        E3.MQ.Write("MQ2Mono required for ImGui: /plugin MQ2Mono");
                        return;
                    }
                    ToggleWindow();
                }
                catch (Exception ex)
                {
                    E3.MQ.Write($"E3 HUD: Error toggling window - {ex.Message}");
                }
            }, "Toggle E3 HUD ImGui window");

            // Register command to show characters in same zone
            RegisterCommand("/e3zonechars", (x) =>
            {
                try
                {
                    if (E3.Bots == null)
                    {
                        E3.MQ.Write("E3.Bots not available");
                        return;
                    }

                    var sameZoneCharacters = E3.Bots.GetCharactersInSameZone();
                    if (sameZoneCharacters.Count == 0)
                    {
                        E3.MQ.Write("\agNo characters found in same zone.");
                        return;
                    }

                    E3.MQ.Write($"\agCharacters in same zone ({sameZoneCharacters.Count}): \aw{string.Join(", ", sameZoneCharacters)}");
                }
                catch (Exception ex)
                {
                    E3.MQ.Write($"E3 Zone Characters: Error - {ex.Message}");
                }
            }, "Show characters in same zone");

            RequestInitialAaRefreshIfNeeded();
        }

        public static void ToggleWindow()
        {
            try
            {
                if (!_windowInitialized)
                {
                    _windowInitialized = true;
                    MonoCore.E3ImGUI.imgui_Begin_OpenFlagSet(WindowName, true);
                    MonoCore.E3ImGUI.imgui_Begin_OpenFlagSet(ShortBuffWindowName, true);
                    MonoCore.E3ImGUI.imgui_Begin_OpenFlagSet(BotCastingWindowName, _showBotCastingWindow);
                    SyncPopoutWindows();
                    RequestInitialAaRefreshIfNeeded();
                }
                else
                {
                    bool open = MonoCore.E3ImGUI.imgui_Begin_OpenFlagGet(WindowName);
                    bool newState = !open;
                    MonoCore.E3ImGUI.imgui_Begin_OpenFlagSet(WindowName, newState);
                    MonoCore.E3ImGUI.imgui_Begin_OpenFlagSet(ShortBuffWindowName, newState);
                    if (!newState)
                    {
                        _showAaWindow = false;
                        _showBotCastingWindow = false;
                        MonoCore.E3ImGUI.imgui_Begin_OpenFlagSet(AaWindowName, false);
                        MonoCore.E3ImGUI.imgui_Begin_OpenFlagSet(BotCastingWindowName, false);
                        MonoCore.E3ImGUI.imgui_Begin_OpenFlagSet(PlayerStatsWindowName, false);
                        MonoCore.E3ImGUI.imgui_Begin_OpenFlagSet(TargetInfoWindowName, false);
                        MonoCore.E3ImGUI.imgui_Begin_OpenFlagSet(BuffListWindowName, false);
                        _playerStatsWindowDismissedBySystem = true;
                        _targetWindowDismissedBySystem = true;
                        _buffWindowDismissedBySystem = true;
                    }
                    else
                    {
                        MonoCore.E3ImGUI.imgui_Begin_OpenFlagSet(AaWindowName, _showAaWindow);
                        MonoCore.E3ImGUI.imgui_Begin_OpenFlagSet(BotCastingWindowName, _showBotCastingWindow);
                        SyncPopoutWindows();
                        RequestInitialAaRefreshIfNeeded();
                    }
                }
                _imguiContextReady = true;
            }
            catch (Exception ex)
            {
                E3.MQ.Write($"E3 HUD UI error: {ex.Message}");
                _imguiContextReady = false;
            }
        }

        private static void RenderMainWindow()
        {
            if (!_imguiContextReady) return;
            if (!MonoCore.E3ImGUI.imgui_Begin_OpenFlagGet(WindowName)) return;
            EnsureHudVisibilitySettingsLoaded();

            MonoCore.E3ImGUI.imgui_SetNextWindowSizeWithCond(600, 400, (int)MonoCore.E3ImGUI.ImGuiCond.FirstUseEver);
            MonoCore.E3ImGUI.PushCurrentTheme();
            try
            {
                using (var window = MonoCore.E3ImGUI.ImGUIWindow.Aquire())
                {
                    if (!window.Begin(WindowName, (int)(MonoCore.E3ImGUI.ImGuiWindowFlags.ImGuiWindowFlags_NoCollapse)))
                    {
                        return;
                    }

                    if (MonoCore.E3ImGUI.imgui_BeginPopupContextWindow("E3HudMainContext", 1))
                    {
                        string botMenuText = _showBotCastingWindow ? "Hide Connected Bot Casts" : "Show Connected Bot Casts";
                        if (MonoCore.E3ImGUI.imgui_MenuItem(botMenuText))
                        {
                            _showBotCastingWindow = !_showBotCastingWindow;
                            MonoCore.E3ImGUI.imgui_Begin_OpenFlagSet(BotCastingWindowName, _showBotCastingWindow);
                        }

                        MonoCore.E3ImGUI.imgui_Separator();
                        if (MonoCore.E3ImGUI.imgui_MenuItem(_showCastingColumn ? "Hide Casting Column" : "Show Casting Column"))
                        {
                            _showCastingColumn = !_showCastingColumn;
                            PersistHudVisibilitySettings();
                        }
                        if (MonoCore.E3ImGUI.imgui_MenuItem(_showTargetColumn ? "Hide Target Column" : "Show Target Column"))
                        {
                            _showTargetColumn = !_showTargetColumn;
                            PersistHudVisibilitySettings();
                        }
                        if (MonoCore.E3ImGUI.imgui_MenuItem(_showEnduranceColumn ? "Hide Endurance Column" : "Show Endurance Column"))
                        {
                            _showEnduranceColumn = !_showEnduranceColumn;
                            PersistHudVisibilitySettings();
                        }
                        if (MonoCore.E3ImGUI.imgui_MenuItem(_showCombatStateColumn ? "Hide Combat State Column" : "Show Combat State Column"))
                        {
                            _showCombatStateColumn = !_showCombatStateColumn;
                            PersistHudVisibilitySettings();
                        }

                        MonoCore.E3ImGUI.imgui_Separator();
                        if (MonoCore.E3ImGUI.imgui_MenuItem(_showPlayerStatsSection ? "Hide Player Stats" : "Show Player Stats"))
                        {
                            _showPlayerStatsSection = !_showPlayerStatsSection;
                            PersistHudVisibilitySettings();
                        }
                        if (MonoCore.E3ImGUI.imgui_MenuItem(_showTargetSection ? "Hide Target" : "Show Target"))
                        {
                            _showTargetSection = !_showTargetSection;
                            PersistHudVisibilitySettings();
                        }
                        if (MonoCore.E3ImGUI.imgui_MenuItem(_showBuffSection ? "Hide Buffs" : "Show Buffs"))
                        {
                            _showBuffSection = !_showBuffSection;
                            PersistHudVisibilitySettings();
                        }

                        MonoCore.E3ImGUI.imgui_Separator();
                        if (MonoCore.E3ImGUI.imgui_MenuItem(_playerStatsPoppedOut ? "Dock Player Stats" : "Pop Out Player Stats"))
                        {
                            SetPlayerStatsPopout(!_playerStatsPoppedOut);
                        }
                        if (MonoCore.E3ImGUI.imgui_MenuItem(_targetInfoPoppedOut ? "Dock Target Info" : "Pop Out Target Info"))
                        {
                            SetTargetInfoPopout(!_targetInfoPoppedOut);
                        }
                        if (MonoCore.E3ImGUI.imgui_MenuItem(_buffSectionPoppedOut ? "Dock Buffs" : "Pop Out Buffs"))
                        {
                            SetBuffSectionPopout(!_buffSectionPoppedOut);
                        }
                        MonoCore.E3ImGUI.imgui_EndPopup();
                    }

                    bool needSeparator = false;
                    if (_showPlayerStatsSection && !_playerStatsPoppedOut)
                    {
                        RenderPlayerStats();
                        needSeparator = true;
                    }

                    if (_showTargetSection && !_targetInfoPoppedOut)
                    {
                        if (needSeparator)
                            MonoCore.E3ImGUI.imgui_Separator();
                        RenderTargetInfo();
                        needSeparator = true;
                    }

                    if (needSeparator)
                        MonoCore.E3ImGUI.imgui_Separator();
                    RenderBotTable();
                    needSeparator = true;

                    if (_showBuffSection && !_buffSectionPoppedOut)
                    {
                        if (needSeparator)
                            MonoCore.E3ImGUI.imgui_Separator();
                        RenderPlayerBuffs();
                    }
                }
            }
            finally
            {
                MonoCore.E3ImGUI.PopCurrentTheme();
            }
        }

        private static bool IsEzServer => E3.IsEzServer;

        private static void RequestInitialAaRefreshIfNeeded()
        {
            if (_initialAaRefreshRequested) return;
            if (!IsEzServer) return;

            try
            {
                E3.MQ.Cmd("/say #AA");
                _initialAaRefreshRequested = true;
            }
            catch (Exception ex)
            {
                E3.MQ.Write($"E3 HUD: Failed to request AA data - {ex.Message}");
            }
        }

        private static long GetTimeMs()
        {
            return (long)(uint)Environment.TickCount;
        }

        private static bool ShouldRefresh(ref long nextUpdateMs, int intervalMs)
        {
            long now = GetTimeMs();
            if (now < nextUpdateMs) return false;
            nextUpdateMs = now + intervalMs;
            return true;
        }

        private static PlayerStatsSnapshot GetPlayerStatsSnapshot()
        {
            if (ShouldRefresh(ref _nextPlayerStatsRefreshMs, PlayerStatsRefreshMs))
            {
                try
                {
                    string playerName = E3.CurrentName ?? "Unknown";
                    int level = QueryNoDelay<int>("${Me.Level}");
                    string gameTime = QueryNoDelay<string>("${GameTime}") ?? string.Empty;
                    double hpPercent = QueryNoDelay<double>("${Me.PctHPs}");
                    double manaPercent = QueryNoDelay<double>("${Me.PctMana}");
                    double endurancePercent = QueryNoDelay<double>("${Me.PctEndurance}");
                    string combatStateRaw = QueryNoDelay<string>("${Me.CombatState}") ?? string.Empty;
                    string combatState = NormalizeCombatState(combatStateRaw);
                    bool hideExp = IsEzServer && level >= 70;
                    int expPercent = hideExp ? 0 : (int)Math.Round(QueryNoDelay<double>("${Me.PctExp}"));
                    int aaPoints = hideExp ? GetSelfAaPoints() : 0;

                    var snapshot = _playerStatsSnapshot;
                    snapshot.PlayerName = playerName;
                    snapshot.Level = level;
                    snapshot.GameTime = gameTime;
                    snapshot.HpPercent = hpPercent;
                    snapshot.ManaPercent = manaPercent;
                    snapshot.EndurancePercent = endurancePercent;
                    snapshot.ExpPercent = expPercent;
                    snapshot.AaPoints = aaPoints;
                    snapshot.HideExp = hideExp;
                    snapshot.CombatState = combatState;
                    snapshot.ErrorMessage = null;
                }
                catch (Exception ex)
                {
                    _playerStatsSnapshot.ErrorMessage = ex.Message;
                }
            }

            return _playerStatsSnapshot;
        }

        private static TargetInfoSnapshot GetTargetInfoSnapshot()
        {
            if (ShouldRefresh(ref _nextTargetInfoRefreshMs, TargetInfoRefreshMs))
            {
                try
                {
                    string targetName = QueryNoDelay<string>("${Target.CleanName}");
                    var snapshot = _targetInfoSnapshot;
                    if (string.IsNullOrEmpty(targetName) || targetName == "NULL")
                    {
                        snapshot.HasTarget = false;
                        snapshot.TargetName = string.Empty;
                        snapshot.Level = 0;
                        snapshot.ClassShort = string.Empty;
                        snapshot.HpPercent = 0;
                        snapshot.Distance = 0;
                        snapshot.ConColor = string.Empty;
                        snapshot.ErrorMessage = null;
                    }
                    else
                    {
                        snapshot.HasTarget = true;
                        snapshot.TargetName = targetName;
                        snapshot.HpPercent = QueryNoDelay<double>("${Target.PctHPs}");
                        snapshot.Level = QueryNoDelay<int>("${Target.Level}");
                        snapshot.ClassShort = QueryNoDelay<string>("${Target.Class.ShortName}") ?? "UNK";
                        snapshot.Distance = QueryNoDelay<double>("${Target.Distance}");
                        snapshot.ConColor = QueryNoDelay<string>("${Target.ConColor}") ?? string.Empty;
                        snapshot.ErrorMessage = null;
                    }
                }
                catch (Exception ex)
                {
                    var snapshot = _targetInfoSnapshot;
                    snapshot.HasTarget = false;
                    snapshot.TargetName = string.Empty;
                    snapshot.Level = 0;
                    snapshot.ClassShort = string.Empty;
                    snapshot.HpPercent = 0;
                    snapshot.Distance = 0;
                    snapshot.ConColor = string.Empty;
                    snapshot.ErrorMessage = ex.Message;
                }
            }

            return _targetInfoSnapshot;
        }

        private static int GetZonePcCount()
        {
            if (ShouldRefresh(ref _nextZonePcCountRefreshMs, ZoneCountRefreshMs))
            {
                try
                {
                    _cachedZonePcCount = QueryNoDelay<int>("${SpawnCount[pc]}");
                }
                catch
                {
                }
            }

            return _cachedZonePcCount;
        }

        private static IReadOnlyList<BuffInfo> GetTargetBuffSnapshot(bool hasTarget)
        {
            if (!hasTarget)
            {
                if (_targetBuffCache.Count > 0)
                {
                    _targetBuffCache.Clear();
                }
                _nextTargetBuffRefreshMs = 0;
                return _targetBuffCache;
            }

            if (ShouldRefresh(ref _nextTargetBuffRefreshMs, TargetBuffRefreshMs))
            {
                try
                {
                    var latest = CollectBuffInfos("Target.Buff", TargetBuffSlots);
                    _targetBuffCache.Clear();
                    _targetBuffCache.AddRange(latest);
                }
                catch
                {
                    _targetBuffCache.Clear();
                }
            }

            return _targetBuffCache;
        }

        private static IReadOnlyList<BuffInfo> GetPlayerBuffSnapshot()
        {
            if (ShouldRefresh(ref _nextPlayerBuffRefreshMs, PlayerBuffRefreshMs))
            {
                try
                {
                    var latest = CollectBuffInfos("Me.Buff", PlayerBuffSlots);
                    _playerBuffCache.Clear();
                    _playerBuffCache.AddRange(latest);
                }
                catch
                {
                    _playerBuffCache.Clear();
                }
            }

            return _playerBuffCache;
        }

        private static IReadOnlyList<BuffInfo> GetShortBuffSnapshot()
        {
            if (ShouldRefresh(ref _nextShortBuffRefreshMs, ShortBuffRefreshMs))
            {
                try
                {
                    var latest = CollectBuffInfos("Me.Song", PlayerShortBuffSlots);
                    _shortBuffCache.Clear();
                    _shortBuffCache.AddRange(latest);
                }
                catch
                {
                    _shortBuffCache.Clear();
                }
            }

            return _shortBuffCache;
        }

        private static void RenderPlayerStats()
        {
            var stats = GetPlayerStatsSnapshot();
            if (!string.IsNullOrEmpty(stats.ErrorMessage))
            {
                MonoCore.E3ImGUI.imgui_TextColored(1.0f, 0.5f, 0.5f, 1.0f, $"Error loading player stats: {stats.ErrorMessage}");
                return;
            }

            MonoCore.E3ImGUI.imgui_TextColored(1.0f, 0.9f, 0.7f, 1.0f, $"Name: {stats.PlayerName} (Lvl {stats.Level})             {stats.GameTime}");

            if (stats.HpPercent >= 90) MonoCore.E3ImGUI.imgui_TextColored(0.0f, 1.0f, 0.0f, 1.0f, $"HP: {stats.HpPercent:F0}%");
            else if (stats.HpPercent >= 60) MonoCore.E3ImGUI.imgui_TextColored(1.0f, 1.0f, 0.0f, 1.0f, $"HP: {stats.HpPercent:F0}%");
            else if (stats.HpPercent >= 30) MonoCore.E3ImGUI.imgui_TextColored(1.0f, 0.6f, 0.0f, 1.0f, $"HP: {stats.HpPercent:F0}%");
            else MonoCore.E3ImGUI.imgui_TextColored(1.0f, 0.0f, 0.0f, 1.0f, $"HP: {stats.HpPercent:F0}%");

            MonoCore.E3ImGUI.imgui_SameLine();
            
            bool isManaUser = GetClassInfo(E3.CurrentName, isSelf: true);
            
            if (!isManaUser)
            {
                MonoCore.E3ImGUI.imgui_TextColored(0.6f, 0.6f, 0.6f, 1.0f, "Mana: --");
            }
            else
            {
                if (stats.ManaPercent >= 90) MonoCore.E3ImGUI.imgui_TextColored(0.0f, 0.5f, 1.0f, 1.0f, $"Mana: {stats.ManaPercent:F0}%");
                else if (stats.ManaPercent >= 60) MonoCore.E3ImGUI.imgui_TextColored(0.0f, 1.0f, 1.0f, 1.0f, $"Mana: {stats.ManaPercent:F0}%");
                else if (stats.ManaPercent >= 30) MonoCore.E3ImGUI.imgui_TextColored(0.5f, 0.0f, 1.0f, 1.0f, $"Mana: {stats.ManaPercent:F0}%");
                else MonoCore.E3ImGUI.imgui_TextColored(1.0f, 0.0f, 0.0f, 1.0f, $"Mana: {stats.ManaPercent:F0}%");
            }

            MonoCore.E3ImGUI.imgui_SameLine();
            if (stats.HideExp)
            {
                MonoCore.E3ImGUI.imgui_TextColored(0.85f, 0.95f, 1.0f, 1.0f, $"AA: {stats.AaPoints:N0}");
                if (MonoCore.E3ImGUI.imgui_IsItemHovered() && MonoCore.E3ImGUI.imgui_IsMouseClicked(0))
                {
                    _showAaWindow = !_showAaWindow;
                    MonoCore.E3ImGUI.imgui_Begin_OpenFlagSet(AaWindowName, _showAaWindow);
                }
            }
            else
            {
                MonoCore.E3ImGUI.imgui_TextColored(1.0f, 0.7f, 0.5f, 1.0f, $"Exp: {stats.ExpPercent}%");
            }
        }

        private static void RenderTargetInfo()
        {
            try
            {
                var targetSnapshot = GetTargetInfoSnapshot();
                using (var child = MonoCore.E3ImGUI.ImGUIChild.Aquire())
                {
                    if (!child.BeginChild("TargetPanel", 0, 180f, 0, 0))
                    {
                        return;
                    }

                    if (!targetSnapshot.HasTarget)
                    {
                        RenderTargetPlaceholder(targetSnapshot.ErrorMessage);
                    }
                    else
                    {
                        var nameColor = GetConColor(targetSnapshot.ConColor);
                        RenderCenteredTargetName($"{targetSnapshot.TargetName} (L{targetSnapshot.Level} {targetSnapshot.ClassShort})", nameColor);

                        RenderTargetHpBar(targetSnapshot.HpPercent);

                        MonoCore.E3ImGUI.imgui_TextColored(0.8f, 0.8f, 0.8f, 1.0f, $"Dist: {targetSnapshot.Distance:F1}");

                        RenderTargetBuffs(targetSnapshot.HasTarget);
                    }
                }
            }
            catch (Exception ex)
            {
                RenderTargetPlaceholder($"Error loading target info: {ex.Message}");
            }
        }

        private static void RenderTargetPlaceholder(string message = null)
        {
            string text = string.IsNullOrEmpty(message) ? "No target selected" : message;
            MonoCore.E3ImGUI.imgui_TextColored(0.55f, 0.55f, 0.55f, 1.0f, text);
            MonoCore.E3ImGUI.imgui_Text(" ");
            MonoCore.E3ImGUI.imgui_PushStyleColor((int)MonoCore.E3ImGUI.ImGuiCol.ChildBg, 0.08f, 0.08f, 0.08f, 0.30f);
            using (var placeholderChild = MonoCore.E3ImGUI.ImGUIChild.Aquire())
            {
                if (placeholderChild.BeginChild("TargetBuffsPlaceholder", 0, 90f, 1, 0))
                {
                    MonoCore.E3ImGUI.imgui_TextColored(0.45f, 0.45f, 0.45f, 1.0f, "Target buffs unavailable.");
                }
            }
            MonoCore.E3ImGUI.imgui_PopStyleColor(1);
        }

        private static void RenderBotTable()
        {
            try
            {
                if (E3.Bots == null)
                {
                    MonoCore.E3ImGUI.imgui_Text("E3.Bots not available");
                    return;
                }

                var connectedBots = E3.Bots.BotsConnected(readOnly: true);
                var sameZoneCharacters = E3.Bots.GetCharactersInSameZone();
                var displayBots = new List<string>();
                if (connectedBots != null)
                {
                    displayBots.AddRange(connectedBots);
                }

                displayBots.RemoveAll(name => string.Equals(name, E3.CurrentName, StringComparison.OrdinalIgnoreCase));
                displayBots.Add(E3.CurrentName);
                displayBots.Sort(StringComparer.OrdinalIgnoreCase);

                if (displayBots.Count == 0)
                {
                    MonoCore.E3ImGUI.imgui_Text("No connected bots detected.");
                    return;
                }

                int zonePcCount = GetZonePcCount();

                MonoCore.E3ImGUI.imgui_TextColored(0.8f, 0.85f, 1.0f, 1.0f, $"Connected: {displayBots.Count} | Zone PCs: {zonePcCount}");

                int tableFlags = (int)(MonoCore.E3ImGUI.ImGuiTableFlags.ImGuiTableFlags_Borders
                    | MonoCore.E3ImGUI.ImGuiTableFlags.ImGuiTableFlags_RowBg
                    | MonoCore.E3ImGUI.ImGuiTableFlags.ImGuiTableFlags_SizingStretchProp
                    | MonoCore.E3ImGUI.ImGuiTableFlags.ImGuiTableFlags_Resizable);

                int columnCount = 5;
                if (_showEnduranceColumn) columnCount++;
                if (_showCastingColumn) columnCount++;
                if (_showTargetColumn) columnCount++;
                if (_showCombatStateColumn) columnCount++;

                using (var table = MonoCore.E3ImGUI.ImGUITable.Aquire())
                {
                    if (table.BeginTable("E3HudBotTable", columnCount, tableFlags, 0, 0))
                    {
                        MonoCore.E3ImGUI.imgui_TableSetupColumn("Name", (int)MonoCore.E3ImGUI.ImGuiTableColumnFlags.ImGuiTableColumnFlags_WidthStretch, 0);
                        MonoCore.E3ImGUI.imgui_TableSetupColumn("HP", (int)MonoCore.E3ImGUI.ImGuiTableColumnFlags.ImGuiTableColumnFlags_WidthFixed, 80f);
                        MonoCore.E3ImGUI.imgui_TableSetupColumn("Mana", (int)MonoCore.E3ImGUI.ImGuiTableColumnFlags.ImGuiTableColumnFlags_WidthFixed, 80f);
                        if (_showEnduranceColumn)
                            MonoCore.E3ImGUI.imgui_TableSetupColumn("End", (int)MonoCore.E3ImGUI.ImGuiTableColumnFlags.ImGuiTableColumnFlags_WidthFixed, 80f);
                        if (_showCastingColumn)
                            MonoCore.E3ImGUI.imgui_TableSetupColumn("Casting", (int)MonoCore.E3ImGUI.ImGuiTableColumnFlags.ImGuiTableColumnFlags_WidthFixed, 150f);
                        if (_showTargetColumn)
                            MonoCore.E3ImGUI.imgui_TableSetupColumn("Target", (int)MonoCore.E3ImGUI.ImGuiTableColumnFlags.ImGuiTableColumnFlags_WidthFixed, 150f);
                        if (_showCombatStateColumn)
                            MonoCore.E3ImGUI.imgui_TableSetupColumn("State", (int)MonoCore.E3ImGUI.ImGuiTableColumnFlags.ImGuiTableColumnFlags_WidthFixed, 90f);
                        MonoCore.E3ImGUI.imgui_TableSetupColumn("Mem", (int)MonoCore.E3ImGUI.ImGuiTableColumnFlags.ImGuiTableColumnFlags_WidthFixed, 65f);
                        MonoCore.E3ImGUI.imgui_TableSetupColumn("Dist", (int)MonoCore.E3ImGUI.ImGuiTableColumnFlags.ImGuiTableColumnFlags_WidthFixed, 80f);
                        MonoCore.E3ImGUI.imgui_TableHeadersRow();

                        var playerStats = GetPlayerStatsSnapshot();
                        bool needCastingLookup = _showCastingColumn || _showTargetColumn;
                        var castingLookup = needCastingLookup ? BuildCastingLookup() : null;

                        foreach (var botName in displayBots)
                        {
                            if (string.IsNullOrEmpty(botName))
                                continue;

                            try
                            {
                                bool isSelf = string.Equals(botName, E3.CurrentName, StringComparison.OrdinalIgnoreCase);
                                int hp = isSelf ? (int)Math.Round(playerStats.HpPercent) : Math.Max(0, Math.Min(100, E3.Bots.PctHealth(botName)));
                                int mana = isSelf ? (int)Math.Round(playerStats.ManaPercent) : Math.Max(0, Math.Min(100, E3.Bots.PctMana(botName)));
                                int endurance = _showEnduranceColumn ? GetEndurancePercent(botName, isSelf, playerStats) : 0;
                                double distance = isSelf ? 0.0 : GetBotDistance(botName);
                                string combatStateText = _showCombatStateColumn ? GetCombatStateText(botName, isSelf, playerStats) : string.Empty;
                                BotCastingEntry castingEntry = null;
                                if (needCastingLookup && castingLookup != null)
                                {
                                    castingLookup.TryGetValue(botName, out castingEntry);
                                }

                                MonoCore.E3ImGUI.imgui_TableNextRow();
                                int columnIndex = 0;

                                // Name column
                                MonoCore.E3ImGUI.imgui_TableSetColumnIndex(columnIndex++);
                                bool isInSameZone = sameZoneCharacters.Contains(botName);
                                
                                if (isSelf)
                                    MonoCore.E3ImGUI.imgui_TextColored(0.75f, 1.0f, 0.75f, 1.0f, botName);
                                else if (isInSameZone)
                                    MonoCore.E3ImGUI.imgui_TextColored(0.85f, 0.75f, 1.0f, 1.0f, botName);
                                else
                                    MonoCore.E3ImGUI.imgui_TextColored(0.6f, 0.2f, 0.8f, 1.0f, botName);
                                if (MonoCore.E3ImGUI.imgui_IsItemHovered() && MonoCore.E3ImGUI.imgui_IsMouseClicked(0))
                                {
                                    if (!isSelf)
                                    {
                                        QueueForegroundCommand(botName);
                                    }
                                }

                                // HP column
                                MonoCore.E3ImGUI.imgui_TableSetColumnIndex(columnIndex++);
                                if (hp >= 90) MonoCore.E3ImGUI.imgui_TextColored(0.0f, 1.0f, 0.0f, 1.0f, $"{hp,3}%");
                                else if (hp >= 60) MonoCore.E3ImGUI.imgui_TextColored(1.0f, 1.0f, 0.0f, 1.0f, $"{hp,3}%");
                                else if (hp >= 30) MonoCore.E3ImGUI.imgui_TextColored(1.0f, 0.6f, 0.0f, 1.0f, $"{hp,3}%");
                                else MonoCore.E3ImGUI.imgui_TextColored(1.0f, 0.0f, 0.0f, 1.0f, $"{hp,3}%");

                                // Mana column
                                MonoCore.E3ImGUI.imgui_TableSetColumnIndex(columnIndex++);
                                bool isManaUser = GetClassInfo(botName, isSelf);
                                if (!isManaUser)
                                {
                                    MonoCore.E3ImGUI.imgui_TextColored(0.6f, 0.6f, 0.6f, 1.0f, " -- ");
                                }
                                else
                                {
                                    if (mana >= 90) MonoCore.E3ImGUI.imgui_TextColored(0.0f, 0.5f, 1.0f, 1.0f, $"{mana,3}%");
                                    else if (mana >= 60) MonoCore.E3ImGUI.imgui_TextColored(0.0f, 1.0f, 1.0f, 1.0f, $"{mana,3}%");
                                    else if (mana >= 30) MonoCore.E3ImGUI.imgui_TextColored(0.5f, 0.0f, 1.0f, 1.0f, $"{mana,3}%");
                                    else MonoCore.E3ImGUI.imgui_TextColored(1.0f, 0.0f, 0.0f, 1.0f, $"{mana,3}%");
                                }

                                if (_showEnduranceColumn)
                                {
                                    MonoCore.E3ImGUI.imgui_TableSetColumnIndex(columnIndex++);
                                    RenderEnduranceCell(endurance);
                                }

                                if (_showCastingColumn)
                                {
                                    MonoCore.E3ImGUI.imgui_TableSetColumnIndex(columnIndex++);
                                    RenderCastingCell(castingEntry);
                                }

                                if (_showTargetColumn)
                                {
                                    MonoCore.E3ImGUI.imgui_TableSetColumnIndex(columnIndex++);
                                    RenderTargetCell(castingEntry);
                                }

                                if (_showCombatStateColumn)
                                {
                                    MonoCore.E3ImGUI.imgui_TableSetColumnIndex(columnIndex++);
                                    RenderCombatStateCell(combatStateText);
                                }

                                // Memory column
                                MonoCore.E3ImGUI.imgui_TableSetColumnIndex(columnIndex++);
                                var memoryStatus = GetMemoryStatus(botName, isSelf);
                                RenderMemoryStatusCell(memoryStatus);

                                // Distance column
                                MonoCore.E3ImGUI.imgui_TableSetColumnIndex(columnIndex++);
                                if (isSelf)
                                {
                                    MonoCore.E3ImGUI.imgui_TextColored(0.6f, 0.6f, 0.6f, 1.0f, " N/A ");
                                }
                                else if (distance < double.MaxValue)
                                {
                                    if (distance <= 50.0) MonoCore.E3ImGUI.imgui_TextColored(0.45f, 0.95f, 0.45f, 1.0f, $"{distance,4:F0}");
                                    else if (distance <= 150.0) MonoCore.E3ImGUI.imgui_TextColored(1.0f, 0.95f, 0.35f, 1.0f, $"{distance,4:F0}");
                                    else MonoCore.E3ImGUI.imgui_TextColored(1.0f, 0.35f, 0.35f, 1.0f, $"{distance,4:F0}");
                                }
                                else
                                {
                                    MonoCore.E3ImGUI.imgui_Text("---");
                                }
                            }
                            catch (Exception ex)
                            {
                                MonoCore.E3ImGUI.imgui_TableSetColumnIndex(0);
                                MonoCore.E3ImGUI.imgui_TextColored(1.0f, 0.5f, 0.5f, 1.0f, $"Error loading bot data for {botName}: {ex.Message}");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MonoCore.E3ImGUI.imgui_TextColored(1.0f, 0.5f, 0.5f, 1.0f, $"Error loading bot table: {ex.Message}");
            }
        }

        private static double GetBotDistance(string botName)
        {
            if (string.IsNullOrEmpty(botName)) return double.MaxValue;
            long now = GetTimeMs();
            if (_botDistanceCache.TryGetValue(botName, out var cached) && now < cached.NextRefreshMs)
            {
                return cached.Distance;
            }

            double distance = double.MaxValue;
            try
            {
                double raw = QueryNoDelay<double>($"${{Spawn[{botName}].Distance}}");
                distance = raw >= 0 ? raw : double.MaxValue;
            }
            catch
            {
            }

            _botDistanceCache[botName] = new DistanceCache
            {
                Distance = distance,
                NextRefreshMs = now + BotDistanceRefreshMs
            };

            return distance;
        }

        private static void EnsureHudVisibilitySettingsLoaded()
        {
            if (_hudVisibilityInitialized)
                return;

            var settings = E3.CharacterSettings;
            if (settings == null)
                return;

            _showCastingColumn = settings.HUD_ShowCastingColumn;
            _showTargetColumn = settings.HUD_ShowTargetColumn;
            _showEnduranceColumn = settings.HUD_ShowEnduranceColumn;
            _showCombatStateColumn = settings.HUD_ShowCombatStateColumn;
            _showPlayerStatsSection = settings.HUD_ShowPlayerStatsSection;
            _showTargetSection = settings.HUD_ShowTargetSection;
            _showBuffSection = settings.HUD_ShowBuffSection;
            _playerStatsPoppedOut = settings.HUD_PopOutPlayerStats;
            _targetInfoPoppedOut = settings.HUD_PopOutTargetInfo;
            _buffSectionPoppedOut = settings.HUD_PopOutBuffSection;
            _playerStatsWindowDismissedBySystem = false;
            _targetWindowDismissedBySystem = false;
            _buffWindowDismissedBySystem = false;
            _hudVisibilityInitialized = true;
        }

        private static void PersistHudVisibilitySettings()
        {
            var settings = E3.CharacterSettings;
            if (settings == null)
                return;

            settings.HUD_ShowCastingColumn = _showCastingColumn;
            settings.HUD_ShowTargetColumn = _showTargetColumn;
            settings.HUD_ShowEnduranceColumn = _showEnduranceColumn;
            settings.HUD_ShowCombatStateColumn = _showCombatStateColumn;
            settings.HUD_ShowPlayerStatsSection = _showPlayerStatsSection;
            settings.HUD_ShowTargetSection = _showTargetSection;
            settings.HUD_ShowBuffSection = _showBuffSection;
            settings.HUD_PopOutPlayerStats = _playerStatsPoppedOut;
            settings.HUD_PopOutTargetInfo = _targetInfoPoppedOut;
            settings.HUD_PopOutBuffSection = _buffSectionPoppedOut;

            try
            {
                settings.SaveData();
            }
            catch (Exception ex)
            {
                E3.MQ.Write($"E3 HUD: Failed to save HUD settings - {ex.Message}");
            }
        }

        private static void SyncPopoutWindows()
        {
            MonoCore.E3ImGUI.imgui_Begin_OpenFlagSet(PlayerStatsWindowName, _playerStatsPoppedOut && _showPlayerStatsSection);
            MonoCore.E3ImGUI.imgui_Begin_OpenFlagSet(TargetInfoWindowName, _targetInfoPoppedOut && _showTargetSection);
            MonoCore.E3ImGUI.imgui_Begin_OpenFlagSet(BuffListWindowName, _buffSectionPoppedOut && _showBuffSection);
        }

        private static void SetPlayerStatsPopout(bool enabled)
        {
            if (_playerStatsPoppedOut == enabled) return;
            _playerStatsPoppedOut = enabled;
            _playerStatsWindowDismissedBySystem = false;
            MonoCore.E3ImGUI.imgui_Begin_OpenFlagSet(PlayerStatsWindowName, enabled && _showPlayerStatsSection);
            PersistHudVisibilitySettings();
        }

        private static void SetTargetInfoPopout(bool enabled)
        {
            if (_targetInfoPoppedOut == enabled) return;
            _targetInfoPoppedOut = enabled;
            _targetWindowDismissedBySystem = false;
            MonoCore.E3ImGUI.imgui_Begin_OpenFlagSet(TargetInfoWindowName, enabled && _showTargetSection);
            PersistHudVisibilitySettings();
        }

        private static void SetBuffSectionPopout(bool enabled)
        {
            if (_buffSectionPoppedOut == enabled) return;
            _buffSectionPoppedOut = enabled;
            _buffWindowDismissedBySystem = false;
            MonoCore.E3ImGUI.imgui_Begin_OpenFlagSet(BuffListWindowName, enabled && _showBuffSection);
            PersistHudVisibilitySettings();
        }

        private static int GetEndurancePercent(string characterName, bool isSelf, PlayerStatsSnapshot playerStats)
        {
            if (isSelf)
            {
                if (playerStats != null)
                {
                    return ClampPercent(playerStats.EndurancePercent);
                }
                return 0;
            }

            if (string.IsNullOrEmpty(characterName))
                return 0;

            if (E3.Bots == null)
                return 0;

            long now = GetTimeMs();
            if (_endurancePercentCache.TryGetValue(characterName, out var cached) && now < cached.NextRefreshMs)
            {
                return cached.Percent;
            }

            int endurance = 0;
            try
            {
                string result = E3.Bots.Query(characterName, "${Me.PctEndurance}");
                endurance = ParsePercentValue(result);
            }
            catch
            {
                endurance = 0;
            }

            endurance = Math.Max(0, Math.Min(100, endurance));
            _endurancePercentCache[characterName] = new PercentCache
            {
                Percent = endurance,
                NextRefreshMs = now + BotEnduranceRefreshMs
            };

            return endurance;
        }

        private static bool GetClassInfo(string characterName, bool isSelf = false)
        {
            if (string.IsNullOrEmpty(characterName)) return true; // Default to mana user for safety
            
            long now = GetTimeMs();
            if (_classCache.TryGetValue(characterName, out var cached) && now < cached.NextRefreshMs)
            {
                return cached.IsManaUser;
            }

            bool isManaUser = true; // Default to mana user
            try
            {
                string classShortNameRaw = isSelf ?
                    QueryNoDelay<string>("${Me.Class.ShortName}") :
                    E3.Bots.Query(characterName, "${Me.Class.ShortName}");
                string classShortName = NormalizeClassIdentifier(classShortNameRaw);
                
                if (!string.IsNullOrEmpty(classShortName))
                {
                    if (EQClasses.ClassShortToLong.TryGetValue(classShortName, out string longClassName))
                    {
                        if (Enum.TryParse<Class>(longClassName, true, out var characterClass))
                        {
                            isManaUser = (characterClass & Class.ManaUsers) == characterClass;
                        }
                    }
                }
            }
            catch
            {
                // If we can't determine class, assume mana user to show percentage
                isManaUser = true;
            }

            // Cache for 5 minutes (300,000ms) - class doesn't change often
            _classCache[characterName] = new ClassCache
            {
                IsManaUser = isManaUser,
                NextRefreshMs = now + 300000
            };

            return isManaUser;
        }

        private static string NormalizeClassIdentifier(string className)
        {
            if (string.IsNullOrWhiteSpace(className)) return string.Empty;
            string trimmed = className.Trim();
            if (trimmed.Equals("NULL", StringComparison.OrdinalIgnoreCase) || trimmed.Equals("UNKNOWN", StringComparison.OrdinalIgnoreCase))
            {
                return string.Empty;
            }
            return trimmed;
        }

        private static string NormalizeCombatState(string state)
        {
            if (string.IsNullOrWhiteSpace(state)) return string.Empty;
            string trimmed = state.Trim();
            if (trimmed.Equals("NULL", StringComparison.OrdinalIgnoreCase) || trimmed.Equals("UNKNOWN", StringComparison.OrdinalIgnoreCase))
            {
                return string.Empty;
            }

            try
            {
                return CultureInfo.InvariantCulture.TextInfo.ToTitleCase(trimmed.ToLowerInvariant());
            }
            catch
            {
                return trimmed;
            }
        }

        private static string GetCombatStateText(string characterName, bool isSelf, PlayerStatsSnapshot playerStats)
        {
            if (isSelf)
            {
                return playerStats == null ? string.Empty : NormalizeCombatState(playerStats.CombatState);
            }

            if (string.IsNullOrEmpty(characterName))
                return string.Empty;

            if (E3.Bots == null)
                return string.Empty;

            long now = GetTimeMs();
            if (_combatStateCache.TryGetValue(characterName, out var cached) && now < cached.NextRefreshMs)
            {
                return cached.State;
            }

            string state = string.Empty;
            try
            {
                string raw = E3.Bots.Query(characterName, "${Me.CombatState}");
                state = NormalizeCombatState(raw);
                if (string.IsNullOrEmpty(state))
                {
                    bool inCombat = E3.Bots.InCombat(characterName);
                    state = inCombat ? "Combat" : "Rest";
                }
            }
            catch
            {
                try
                {
                    bool inCombat = E3.Bots.InCombat(characterName);
                    state = inCombat ? "Combat" : string.Empty;
                }
                catch
                {
                    state = string.Empty;
                }
            }

            _combatStateCache[characterName] = new CombatStateCache
            {
                State = state,
                NextRefreshMs = now + CombatStateRefreshMs
            };

            return state;
        }

        private static int ParsePercentValue(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return 0;

            string trimmed = value.Trim();
            if (int.TryParse(trimmed, NumberStyles.Integer, CultureInfo.InvariantCulture, out var intResult))
            {
                return intResult;
            }

            if (int.TryParse(trimmed, NumberStyles.Integer, CultureInfo.CurrentCulture, out intResult))
            {
                return intResult;
            }

            if (double.TryParse(trimmed, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out var doubleResult))
            {
                return (int)Math.Round(doubleResult);
            }

            if (double.TryParse(trimmed, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.CurrentCulture, out doubleResult))
            {
                return (int)Math.Round(doubleResult);
            }

            return 0;
        }

        private static int ClampPercent(double value)
        {
            return (int)Math.Max(0, Math.Min(100, Math.Round(value)));
        }

        private static void RenderPlayerStatsWindow()
        {
            RenderPopoutWindow(PlayerStatsWindowName, ref _playerStatsWindowDismissedBySystem,
                _playerStatsPoppedOut && _showPlayerStatsSection, RenderPlayerStats, () => SetPlayerStatsPopout(false));
        }

        private static void RenderTargetInfoWindow()
        {
            RenderPopoutWindow(TargetInfoWindowName, ref _targetWindowDismissedBySystem,
                _targetInfoPoppedOut && _showTargetSection, RenderTargetInfo, () => SetTargetInfoPopout(false));
        }

        private static void RenderBuffListWindow()
        {
            RenderPopoutWindow(BuffListWindowName, ref _buffWindowDismissedBySystem,
                _buffSectionPoppedOut && _showBuffSection, RenderPlayerBuffs, () => SetBuffSectionPopout(false));
        }

        private static void RenderPopoutWindow(string windowName, ref bool dismissedBySystem, bool shouldDisplay, Action renderAction, Action onClosedByUser)
        {
            if (!_imguiContextReady)
                return;

            bool open = MonoCore.E3ImGUI.imgui_Begin_OpenFlagGet(windowName);
            if (!shouldDisplay)
            {
                if (open)
                    MonoCore.E3ImGUI.imgui_Begin_OpenFlagSet(windowName, false);
                dismissedBySystem = true;
                return;
            }

            if (!open)
            {
                if (dismissedBySystem)
                {
                    MonoCore.E3ImGUI.imgui_Begin_OpenFlagSet(windowName, true);
                    dismissedBySystem = false;
                }
                else
                {
                    onClosedByUser?.Invoke();
                    return;
                }
            }

            MonoCore.E3ImGUI.PushCurrentTheme();
            try
            {
                using (var window = MonoCore.E3ImGUI.ImGUIWindow.Aquire())
                {
                    int flags = (int)MonoCore.E3ImGUI.ImGuiWindowFlags.ImGuiWindowFlags_NoCollapse;
                    if (!window.Begin(windowName, flags))
                        return;
                    renderAction?.Invoke();
                }
            }
            finally
            {
                MonoCore.E3ImGUI.PopCurrentTheme();
            }
        }

        private static MemoryStatusSummary GetMemoryStatus(string characterName, bool isSelf)
        {
            var summary = new MemoryStatusSummary
            {
                HasData = false,
                EqCommitMb = 0,
                CSharpMb = 0,
                Health = MemoryHealthState.Unknown
            };

            if (string.IsNullOrEmpty(characterName))
            {
                return summary;
            }

            long now = GetTimeMs();
            if (_memoryStatusCache.TryGetValue(characterName, out var cached) && now < cached.NextRefreshMs)
            {
                return cached.Summary;
            }

            double csharp = 0;
            double eqCommit = 0;

            try
            {
                if (E3.Bots != null)
                {
                    E3.Bots.GetMemoryUsage(characterName, out csharp, out eqCommit);
                }

                if ((csharp <= 0 && eqCommit <= 0) && isSelf)
                {
                    (csharp, eqCommit) = CaptureLocalMemoryUsage();
                }
            }
            catch
            {
                // Leave defaults if we failed to query
                if (isSelf)
                {
                    (csharp, eqCommit) = CaptureLocalMemoryUsage();
                }
            }

            bool hasData = eqCommit > 0 || csharp > 0;

            summary.HasData = hasData;
            summary.EqCommitMb = eqCommit;
            summary.CSharpMb = csharp;
            summary.Health = hasData ? EvaluateMemoryHealth(eqCommit) : MemoryHealthState.Unknown;

            _memoryStatusCache[characterName] = new MemoryStatusCache
            {
                Summary = summary,
                NextRefreshMs = now + MemoryStatusRefreshMs
            };

            return summary;
        }

        private static MemoryHealthState EvaluateMemoryHealth(double eqCommitMb)
        {
            if (eqCommitMb <= 0)
                return MemoryHealthState.Unknown;

            double eqCommitGb = eqCommitMb / 1024d;
            if (eqCommitGb < 1.2d)
                return MemoryHealthState.Good;
            if (eqCommitGb < 1.4d)
                return MemoryHealthState.Caution;
            return MemoryHealthState.Danger;
        }

        private static void RenderMemoryStatusCell(MemoryStatusSummary status)
        {
            string text;
            float r;
            float g;
            float b;

            switch (status.Health)
            {
                case MemoryHealthState.Good:
                    text = "OK";
                    r = 0.3f;
                    g = 0.95f;
                    b = 0.3f;
                    break;
                case MemoryHealthState.Caution:
                    text = "CHK";
                    r = 1.0f;
                    g = 0.65f;
                    b = 0.2f;
                    break;
                case MemoryHealthState.Danger:
                    text = "WARN";
                    r = 1.0f;
                    g = 0.3f;
                    b = 0.3f;
                    break;
                default:
                    text = "--";
                    r = 0.65f;
                    g = 0.65f;
                    b = 0.65f;
                    break;
            }

            MonoCore.E3ImGUI.imgui_TextColored(r, g, b, 1.0f, text);

            if (!status.HasData)
                return;

            if (MonoCore.E3ImGUI.imgui_IsItemHovered())
            {
                MonoCore.E3ImGUI.imgui_BeginTooltip();
                MonoCore.E3ImGUI.imgui_Text($"Status: {text}");
                MonoCore.E3ImGUI.imgui_Text($"EQ Commit: {status.EqCommitMb:N1} MB");
                if (status.CSharpMb > 0)
                {
                    MonoCore.E3ImGUI.imgui_Text($"C# Memory: {status.CSharpMb:N1} MB");
                }
                MonoCore.E3ImGUI.imgui_EndTooltip();
            }
        }

        private static void RenderEnduranceCell(int endurance)
        {
            int clamped = Math.Max(0, Math.Min(100, endurance));
            string display = $"{clamped,3}%";
            if (clamped >= 80)
                MonoCore.E3ImGUI.imgui_TextColored(0.85f, 0.85f, 0.2f, 1.0f, display);
            else if (clamped >= 50)
                MonoCore.E3ImGUI.imgui_TextColored(0.95f, 0.7f, 0.2f, 1.0f, display);
            else if (clamped >= 25)
                MonoCore.E3ImGUI.imgui_TextColored(0.95f, 0.45f, 0.2f, 1.0f, display);
            else
                MonoCore.E3ImGUI.imgui_TextColored(1.0f, 0.2f, 0.2f, 1.0f, display);
        }

        private static void RenderCastingCell(BotCastingEntry entry)
        {
            if (entry == null)
            {
                MonoCore.E3ImGUI.imgui_TextColored(0.6f, 0.6f, 0.6f, 1.0f, "--");
                return;
            }

            if (!entry.HasRecentData)
            {
                MonoCore.E3ImGUI.imgui_TextColored(0.55f, 0.55f, 0.55f, 1.0f, "No Data");
                return;
            }

            if (string.IsNullOrWhiteSpace(entry.SpellName))
            {
                MonoCore.E3ImGUI.imgui_TextColored(0.65f, 0.85f, 0.95f, 1.0f, "Idle");
                return;
            }

            MonoCore.E3ImGUI.imgui_TextColored(0.95f, 0.9f, 0.55f, 1.0f, entry.SpellName);
        }

        private static void RenderTargetCell(BotCastingEntry entry)
        {
            if (entry == null || string.IsNullOrWhiteSpace(entry.TargetName))
            {
                MonoCore.E3ImGUI.imgui_TextColored(0.7f, 0.7f, 0.7f, 1.0f, "None");
                return;
            }

            MonoCore.E3ImGUI.imgui_TextColored(0.65f, 0.85f, 1.0f, 1.0f, entry.TargetName);
        }

        private static void RenderCombatStateCell(string combatState)
        {
            if (string.IsNullOrWhiteSpace(combatState))
            {
                MonoCore.E3ImGUI.imgui_TextColored(0.65f, 0.65f, 0.65f, 1.0f, "--");
                return;
            }

            string lower = combatState.ToLowerInvariant();
            float r = 0.9f, g = 0.9f, b = 0.3f;
            if (lower.Contains("combat"))
            {
                r = 1.0f; g = 0.35f; b = 0.35f;
            }
            else if (lower.Contains("rest"))
            {
                r = 0.45f; g = 0.95f; b = 0.45f;
            }
            else if (lower.Contains("idle") || lower.Contains("stand"))
            {
                r = 0.65f; g = 0.85f; b = 0.95f;
            }

            MonoCore.E3ImGUI.imgui_TextColored(r, g, b, 1.0f, combatState);
        }

        private static (double managedMb, double eqCommitMb) CaptureLocalMemoryUsage()
        {
            double managedMb = 0;
            double eqCommitMb = 0;

            try
            {
                long bytes = GC.GetTotalMemory(false);
                managedMb = bytes / 1024d / 1024d;
            }
            catch
            {
            }

            try
            {
                if (Core._MQ2MonoVersion > 0.35m)
                {
                    eqCommitMb = Math.Max(0d, Core.mq_Memory_GetPageFileSize());
                }
            }
            catch
            {
            }

            return (managedMb, eqCommitMb);
        }

        private static void RenderAaWindow()
		{
            bool open = MonoCore.E3ImGUI.imgui_Begin_OpenFlagGet(AaWindowName);
            if (!open)
            {
                _showAaWindow = false;
                return;
            }
            _showAaWindow = true;

            MonoCore.E3ImGUI.imgui_SetNextWindowSizeWithCond(350f, 240f, (int)MonoCore.E3ImGUI.ImGuiCond.FirstUseEver);
            MonoCore.E3ImGUI.PushCurrentTheme();
            try
            {
                using (var window = MonoCore.E3ImGUI.ImGUIWindow.Aquire())
                {
                    int flags = (int)MonoCore.E3ImGUI.ImGuiWindowFlags.ImGuiWindowFlags_NoCollapse;
                    if (!window.Begin(AaWindowName, flags))
                    {
                        return;
                    }

                    var rows = CollectAaRows();
                    long totalAa = 0;
                    foreach (var row in rows)
                    {
                        totalAa += row.Aa;
                    }

                    MonoCore.E3ImGUI.imgui_TextColored(0.9f, 0.95f, 1.0f, 1.0f, $"Total AA: {totalAa:N0}");
                    MonoCore.E3ImGUI.imgui_SameLine();
                    if (IsEzServer && MonoCore.E3ImGUI.imgui_Button("Refresh"))
                    {
                        E3.MQ.Cmd("/say #AA");
                    }

                    int tableFlags = (int)(MonoCore.E3ImGUI.ImGuiTableFlags.ImGuiTableFlags_Borders
                        | MonoCore.E3ImGUI.ImGuiTableFlags.ImGuiTableFlags_RowBg
                        | MonoCore.E3ImGUI.ImGuiTableFlags.ImGuiTableFlags_SizingStretchProp
                        | MonoCore.E3ImGUI.ImGuiTableFlags.ImGuiTableFlags_Resizable);

                    using (var table = MonoCore.E3ImGUI.ImGUITable.Aquire())
                    {
                        if (table.BeginTable("E3HudAATable", 2, tableFlags, 0, 0))
                        {
                            MonoCore.E3ImGUI.imgui_TableSetupColumn("Name", (int)MonoCore.E3ImGUI.ImGuiTableColumnFlags.ImGuiTableColumnFlags_WidthStretch, 0f);
                            MonoCore.E3ImGUI.imgui_TableSetupColumn("AA Points", (int)MonoCore.E3ImGUI.ImGuiTableColumnFlags.ImGuiTableColumnFlags_WidthFixed, 120f);
                            MonoCore.E3ImGUI.imgui_TableHeadersRow();

                            foreach (var row in rows)
                            {
                                MonoCore.E3ImGUI.imgui_TableNextRow();
                                MonoCore.E3ImGUI.imgui_TableSetColumnIndex(0);
                                MonoCore.E3ImGUI.imgui_Text(row.Name);
                                MonoCore.E3ImGUI.imgui_TableSetColumnIndex(1);
                                MonoCore.E3ImGUI.imgui_Text(row.Aa.ToString("N0"));
                            }
                        }
                    }
                }
            }
            finally
            {
                MonoCore.E3ImGUI.PopCurrentTheme();
            }
        }

        private static T QueryNoDelay<T>(string query)
        {
            return E3.MQ.Query<T>(query, delayPossible: false);
        }

        private static int GetSelfAaPoints()
        {
            int aaPoints = E3.ResolveAaPoints(QueryNoDelay<int>("${Me.AAPoints}"));
            return Math.Max(0, aaPoints);
        }

        private static void QueueForegroundCommand(string botName)
        {
            if (string.IsNullOrWhiteSpace(botName)) return;
            Task.Run(() =>
            {
                try
                {
                    E3.MQ.Cmd($"/dex {botName} /foreground", delayed: true);
                }
                catch (Exception ex)
                {
                    E3.MQ.Write($"E3 HUD foreground failed: {ex.Message}");
                }
            });
        }

        private static void RenderTargetHpBar(double hpPercent)
        {
            double clamped = Math.Max(0, Math.Min(100, hpPercent));
            float width = MonoCore.E3ImGUI.imgui_GetContentRegionAvailX();
            float height = 18f;

            MonoCore.E3ImGUI.imgui_PushStyleColor((int)MonoCore.E3ImGUI.ImGuiCol.Button, 0f, 0f, 0f, 0f);
            MonoCore.E3ImGUI.imgui_PushStyleColor((int)MonoCore.E3ImGUI.ImGuiCol.ButtonHovered, 0f, 0f, 0f, 0f);
            MonoCore.E3ImGUI.imgui_PushStyleColor((int)MonoCore.E3ImGUI.ImGuiCol.ButtonActive, 0f, 0f, 0f, 0f);
            MonoCore.E3ImGUI.imgui_ButtonEx("##TargetHPBar", width, height);
            MonoCore.E3ImGUI.imgui_PopStyleColor(3);

            float minX = MonoCore.E3ImGUI.imgui_GetItemRectMinX();
            float minY = MonoCore.E3ImGUI.imgui_GetItemRectMinY();
            float maxX = MonoCore.E3ImGUI.imgui_GetItemRectMaxX();
            float maxY = MonoCore.E3ImGUI.imgui_GetItemRectMaxY();

            float rounding = Math.Min(height * 0.5f, Math.Max(4f, MonoCore.E3ImGUI._rounding));
            MonoCore.E3ImGUI.imgui_GetWindowDrawList_AddRectFilled(
                minX,
                minY,
                maxX,
                maxY,
                MakeColor(0.12f, 0.12f, 0.12f, 1.0f),
                rounding,
                (int)MonoCore.E3ImGUI.ImDrawFlags.ImDrawFlags_RoundCornersAll);

            float fillWidth = (float)(clamped / 100.0) * (maxX - minX);
            if (fillWidth > 0.5f)
            {
                bool isFull = clamped >= 99.5;
                int fillFlags = (int)(isFull
                    ? MonoCore.E3ImGUI.ImDrawFlags.ImDrawFlags_RoundCornersAll
                    : MonoCore.E3ImGUI.ImDrawFlags.ImDrawFlags_RoundCornersLeft);

                MonoCore.E3ImGUI.imgui_GetWindowDrawList_AddRectFilled(
                    minX,
                    minY,
                    minX + fillWidth,
                    maxY,
                    GetPercentColor(clamped),
                    rounding,
                    fillFlags);
            }

            string label = $"HP {(int)Math.Round(clamped)}%";
            MonoCore.E3ImGUI.imgui_GetWindowDrawList_AddText(minX + 6f, minY + 2f, MakeColor(1f, 1f, 1f, 1f), label);
        }

        private static void RenderCenteredTargetName(string text, (float r, float g, float b) color)
        {
            if (string.IsNullOrWhiteSpace(text)) return;

            float lineStartX = MonoCore.E3ImGUI.imgui_GetCursorPosX();
            float available = MonoCore.E3ImGUI.imgui_GetContentRegionAvailX();
            float textWidth = MonoCore.E3ImGUI.imgui_CalcTextSizeX(text);
            float offset = Math.Max(0f, (available - textWidth) * 0.5f);

            MonoCore.E3ImGUI.imgui_SetCursorPosX(lineStartX + offset);
            MonoCore.E3ImGUI.imgui_TextColored(color.r, color.g, color.b, 1.0f, text);
            MonoCore.E3ImGUI.imgui_SetCursorPosX(lineStartX);
        }

        private static void RenderTargetBuffs(bool hasTarget)
        {
            var buffEntries = GetTargetBuffSnapshot(hasTarget);

            MonoCore.E3ImGUI.imgui_PushStyleColor((int)MonoCore.E3ImGUI.ImGuiCol.ChildBg, 0.08f, 0.08f, 0.08f, 0.30f);
            using (var child = MonoCore.E3ImGUI.ImGUIChild.Aquire())
            {
                if (child.BeginChild("TargetBuffs", 0, 90f, 1, 0))
                {
                    using (var table = MonoCore.E3ImGUI.ImGUITable.Aquire())
                    {
                        int tableFlags = (int)(MonoCore.E3ImGUI.ImGuiTableFlags.ImGuiTableFlags_SizingFixedFit
                            | MonoCore.E3ImGUI.ImGuiTableFlags.ImGuiTableFlags_NoPadOuterX);
                        if (table.BeginTable("TargetBuffGrid", TargetBuffIconsPerRow, tableFlags, 0, 0))
                        {
                            if (buffEntries.Count == 0)
                            {
                                MonoCore.E3ImGUI.imgui_TableNextRow();
                                MonoCore.E3ImGUI.imgui_TableSetColumnIndex(0);
                                MonoCore.E3ImGUI.imgui_TextColored(0.7f, 0.7f, 0.7f, 1.0f, "No buffs detected.");
                            }
                            else
                            {
                                for (int i = 0; i < buffEntries.Count; i++)
                                {
                                    if (i % TargetBuffIconsPerRow == 0)
                                    {
                                        MonoCore.E3ImGUI.imgui_TableNextRow();
                                    }

                                    MonoCore.E3ImGUI.imgui_TableSetColumnIndex(i % TargetBuffIconsPerRow);
                                    var buff = buffEntries[i];
                                    if (buff.IconIndex > 0)
                                        MonoCore.E3ImGUI.imgui_DrawSpellIconByIconIndex(buff.IconIndex, 22f);
                                    else
                                        MonoCore.E3ImGUI.imgui_DrawSpellIconBySpellID(buff.SpellId, 22f);
                                }
                            }
                        }
                    }
                }
            }
            MonoCore.E3ImGUI.imgui_PopStyleColor(1);
        }

        private static void RenderPlayerBuffs()
        {
            try
            {
                MonoCore.E3ImGUI.imgui_TextColored(0.9f, 0.85f, 1.0f, 1.0f, "My Buffs");
                var longBuffs = GetPlayerBuffSnapshot();
                RenderBuffTable("E3HudLongBuffs", longBuffs, "No active buffs.");
            }
            catch (Exception ex)
            {
                MonoCore.E3ImGUI.imgui_TextColored(1.0f, 0.5f, 0.5f, 1.0f, $"Error loading buffs: {ex.Message}");
            }
        }

        private static void RenderShortBuffWindow()
        {
            if (!_imguiContextReady) return;
            if (!MonoCore.E3ImGUI.imgui_Begin_OpenFlagGet(ShortBuffWindowName)) return;

            MonoCore.E3ImGUI.PushCurrentTheme();
            try
            {
                using (var window = MonoCore.E3ImGUI.ImGUIWindow.Aquire())
                {
                    MonoCore.E3ImGUI.imgui_SetNextWindowSizeWithCond(260f, 0f, (int)MonoCore.E3ImGUI.ImGuiCond.FirstUseEver);
                    int flags = (int)(MonoCore.E3ImGUI.ImGuiWindowFlags.ImGuiWindowFlags_NoCollapse
                        | MonoCore.E3ImGUI.ImGuiWindowFlags.ImGuiWindowFlags_NoTitleBar
                        | MonoCore.E3ImGUI.ImGuiWindowFlags.ImGuiWindowFlags_NoFocusOnAppearing
                        | MonoCore.E3ImGUI.ImGuiWindowFlags.ImGuiWindowFlags_AlwaysAutoResize
                        | MonoCore.E3ImGUI.ImGuiWindowFlags.ImGuiWindowFlags_NoBackground);
                    if (!window.Begin(ShortBuffWindowName, flags))
                    {
                        return;
                    }

                    var shortBuffs = GetShortBuffSnapshot();
                    RenderBuffTable("E3HudShortBuffsFloating", shortBuffs, "No short-term buffs.");
                }
            }
            finally
            {
                MonoCore.E3ImGUI.PopCurrentTheme();
            }
        }

        private static void RenderBotCastingWindow()
        {
            if (!_imguiContextReady) return;
            if (!MonoCore.E3ImGUI.imgui_Begin_OpenFlagGet(BotCastingWindowName)) return;

            MonoCore.E3ImGUI.PushCurrentTheme();
            try
            {
                using (var window = MonoCore.E3ImGUI.ImGUIWindow.Aquire())
                {
                    MonoCore.E3ImGUI.imgui_SetNextWindowSizeWithCond(360f, 320f, (int)MonoCore.E3ImGUI.ImGuiCond.FirstUseEver);
                    int flags = (int)(MonoCore.E3ImGUI.ImGuiWindowFlags.ImGuiWindowFlags_NoCollapse
                        | MonoCore.E3ImGUI.ImGuiWindowFlags.ImGuiWindowFlags_NoBackground);
                    if (!window.Begin(BotCastingWindowName, flags))
                    {
                        return;
                    }

                    var entries = GetBotCastingEntries();
                    if (entries.Count == 0)
                    {
                        MonoCore.E3ImGUI.imgui_TextColored(0.7f, 0.7f, 0.7f, 1.0f, "No connected bots detected.");
                        return;
                    }

                    MonoCore.E3ImGUI.imgui_TextColored(0.8f, 0.85f, 1.0f, 1.0f, $"Connected: {entries.Count}");
                    MonoCore.E3ImGUI.imgui_Separator();
                    RenderBotCastingGrid(entries);
                }
            }
            finally
            {
                MonoCore.E3ImGUI.PopCurrentTheme();
            }
        }

        private static void RenderBotCastingGrid(IReadOnlyList<BotCastingEntry> entries)
        {
            const int rowsPerColumn = 6;
            int columnCount = Math.Max(1, (int)Math.Ceiling(entries.Count / (double)rowsPerColumn));
            int tableFlags = (int)(MonoCore.E3ImGUI.ImGuiTableFlags.ImGuiTableFlags_BordersInnerV
                | MonoCore.E3ImGUI.ImGuiTableFlags.ImGuiTableFlags_BordersOuter
                | MonoCore.E3ImGUI.ImGuiTableFlags.ImGuiTableFlags_SizingFixedFit);

            using (var table = MonoCore.E3ImGUI.ImGUITable.Aquire())
            {
                if (!table.BeginTable("E3HudBotCasting", columnCount, tableFlags, 0, 0))
                {
                    return;
                }

                for (int col = 0; col < columnCount; col++)
                {
                    MonoCore.E3ImGUI.imgui_TableSetupColumn($"CastingCol{col}", (int)MonoCore.E3ImGUI.ImGuiTableColumnFlags.ImGuiTableColumnFlags_WidthFixed, 170f);
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

                    MonoCore.E3ImGUI.imgui_TableNextRow();
                    for (int col = 0; col < columnCount; col++)
                    {
                        MonoCore.E3ImGUI.imgui_TableSetColumnIndex(col);
                        int index = (col * rowsPerColumn) + row;
                        if (index >= entries.Count)
                        {
                            MonoCore.E3ImGUI.imgui_Text(" ");
                            continue;
                        }

                        RenderBotCastingCell(entries[index]);
                    }
                }
            }
        }

		private static void RenderBotCastingCell(BotCastingEntry entry)
		{
			if (entry == null)
			{
				MonoCore.E3ImGUI.imgui_Text(" ");
				return;
			}

			if (entry.IsSelf)
				MonoCore.E3ImGUI.imgui_TextColored(0.75f, 1.0f, 0.75f, 1.0f, entry.Name);
			else
				MonoCore.E3ImGUI.imgui_TextColored(0.85f, 0.75f, 1.0f, 1.0f, entry.Name);

			string targetDisplay = string.IsNullOrWhiteSpace(entry.TargetName)
				? "Target: None"
				: $"Target: {entry.TargetName}";
			MonoCore.E3ImGUI.imgui_TextColored(0.65f, 0.85f, 1.0f, 1.0f, targetDisplay);

			string stateText;
			float sr = 0.6f, sg = 0.6f, sb = 0.6f;
			if (!entry.HasRecentData)
			{
				stateText = "Spell: No data";
			}
			else if (string.IsNullOrWhiteSpace(entry.SpellName))
			{
				stateText = "Spell: Idle";
				sr = 0.65f; sg = 0.85f; sb = 0.95f;
			}
			else
			{
				stateText = $"Spell: {entry.SpellName}";
				sr = 0.95f; sg = 0.9f; sb = 0.55f;
			}
			MonoCore.E3ImGUI.imgui_TextColored(sr, sg, sb, 1.0f, stateText);
		}

        private static (float r, float g, float b) GetConColor(string con)
        {
            switch ((con ?? string.Empty).Trim().ToLowerInvariant())
            {
                case "red":
                    return (1.0f, 0.2f, 0.2f);
                case "yellow":
                    return (1.0f, 0.85f, 0.2f);
                case "blue":
                case "light blue":
                    return (0.4f, 0.7f, 1.0f);
                case "green":
                    return (0.3f, 1.0f, 0.3f);
                case "grey":
                case "gray":
                    return (0.6f, 0.6f, 0.6f);
                default:
                    return (0.85f, 0.75f, 1.0f);
            }
        }

        private static uint GetPercentColor(double percent)
        {
            if (percent >= 90) return MakeColor(0.0f, 0.8f, 0.0f, 1.0f);
            if (percent >= 60) return MakeColor(0.9f, 0.9f, 0.2f, 1.0f);
            if (percent >= 30) return MakeColor(1.0f, 0.6f, 0.0f, 1.0f);
            return MakeColor(1.0f, 0.1f, 0.1f, 1.0f);
        }

        private static uint MakeColor(float r, float g, float b, float a)
        {
            uint rr = (uint)Math.Round(Math.Max(0, Math.Min(1, r)) * 255.0f);
            uint gg = (uint)Math.Round(Math.Max(0, Math.Min(1, g)) * 255.0f);
            uint bb = (uint)Math.Round(Math.Max(0, Math.Min(1, b)) * 255.0f);
            uint aa = (uint)Math.Round(Math.Max(0, Math.Min(1, a)) * 255.0f);
            return (aa << 24) | (bb << 16) | (gg << 8) | rr;
        }

        private class PlayerStatsSnapshot
        {
            public string PlayerName { get; set; } = "Unknown";
            public int Level { get; set; }
            public string GameTime { get; set; } = string.Empty;
            public double HpPercent { get; set; }
            public double ManaPercent { get; set; }
            public double EndurancePercent { get; set; }
            public int ExpPercent { get; set; }
            public int AaPoints { get; set; }
            public bool HideExp { get; set; }
            public string CombatState { get; set; } = string.Empty;
            public string ErrorMessage { get; set; } = string.Empty;
        }

        private class TargetInfoSnapshot
        {
            public bool HasTarget { get; set; }
            public string TargetName { get; set; } = string.Empty;
            public int Level { get; set; }
            public string ClassShort { get; set; } = string.Empty;
            public double HpPercent { get; set; }
            public double Distance { get; set; }
            public string ConColor { get; set; } = string.Empty;
            public string ErrorMessage { get; set; } = string.Empty;
        }

        private class DistanceCache
        {
            public double Distance { get; set; }
            public long NextRefreshMs { get; set; }
        }

        private class ClassCache
        {
            public bool IsManaUser { get; set; }
            public long NextRefreshMs { get; set; }
        }

        private class PercentCache
        {
            public int Percent { get; set; }
            public long NextRefreshMs { get; set; }
        }

        private class CombatStateCache
        {
            public string State { get; set; } = string.Empty;
            public long NextRefreshMs { get; set; }
        }

        private enum MemoryHealthState
        {
            Unknown = 0,
            Good,
            Caution,
            Danger
        }

        private struct MemoryStatusSummary
        {
            public bool HasData;
            public double EqCommitMb;
            public double CSharpMb;
            public MemoryHealthState Health;
        }

        private class MemoryStatusCache
        {
            public MemoryStatusSummary Summary;
            public long NextRefreshMs;
        }

        private class AaRow
        {
            public string Name { get; set; } = string.Empty;
            public int Aa { get; set; }
        }

        private static List<AaRow> CollectAaRows()
        {
            var rows = new List<AaRow>();
            string selfName = E3.CurrentName ?? string.Empty;
            int selfAa = GetSelfAaPoints();
            rows.Add(new AaRow { Name = selfName, Aa = selfAa });

            var bots = E3.Bots?.BotsConnected(readOnly: true) ?? new List<string>();
            foreach (var name in bots)
            {
                if (string.IsNullOrEmpty(name) || string.Equals(name, selfName, StringComparison.OrdinalIgnoreCase))
                    continue;

                int aa = QueryAaForBot(name);
                rows.Add(new AaRow { Name = name, Aa = aa });
            }

            rows.Sort((a, b) =>
            {
                int cmp = b.Aa.CompareTo(a.Aa);
                return cmp != 0 ? cmp : string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase);
            });
            return rows;
        }

        private static int QueryAaForBot(string name)
        {
            try
            {
                string value = E3.Bots.Query(name, "${Me.AAPoints}");
                if (int.TryParse(value, out var result))
                {
                    return result;
                }
            }
            catch
            {
            }

            return 0;
        }

        private class BuffInfo
        {
            public string Name { get; set; } = string.Empty;
            public int SpellId { get; set; }
            public int SecondsRemaining { get; set; }
            public int IconIndex { get; set; }
        }

        private static List<BuffInfo> CollectBuffInfos(string root, int maxSlots)
        {
            var buffs = new List<BuffInfo>();
            for (int slot = 1; slot <= maxSlots; slot++)
            {
                try
                {
                    int spellId = QueryNoDelay<int>($"${{{root}[{slot}].ID}}");
                    if (spellId <= 0) continue;
                    string name = QueryNoDelay<string>($"${{{root}[{slot}].Name}}");
                    int durationTicks = QueryNoDelay<int>($"${{{root}[{slot}].Duration}}");
                    int seconds = durationTicks > 0 ? durationTicks * 6 : 0;
                    int iconIndex = QueryNoDelay<int>($"${{{root}[{slot}].SpellIcon}}");

                    buffs.Add(new BuffInfo
                    {
                        SpellId = spellId,
                        Name = string.IsNullOrEmpty(name) ? "Unknown" : name,
                        SecondsRemaining = seconds,
                        IconIndex = iconIndex
                    });
                }
                catch
                {
                    // Ignore individual slot failures
                }
            }
            return buffs;
        }

        private static void RenderBuffTable(string tableId, IReadOnlyList<BuffInfo> buffs, string emptyMessage)
        {
            int flags = (int)(MonoCore.E3ImGUI.ImGuiTableFlags.ImGuiTableFlags_Borders
                | MonoCore.E3ImGUI.ImGuiTableFlags.ImGuiTableFlags_RowBg
                | MonoCore.E3ImGUI.ImGuiTableFlags.ImGuiTableFlags_SizingStretchProp);

            float textLineHeight = MonoCore.E3ImGUI.imgui_GetTextLineHeightWithSpacing();
            const float iconSize = 18f;
            float targetRowHeight = Math.Max(iconSize, textLineHeight);
            float iconOffset = Math.Max(0f, (targetRowHeight - iconSize) * 0.5f);
            float textOffset = Math.Max(0f, (targetRowHeight - textLineHeight) * 0.5f);

            using (var table = MonoCore.E3ImGUI.ImGUITable.Aquire())
            {
                if (!table.BeginTable(tableId, 2, flags, 0, 0))
                    return;

                MonoCore.E3ImGUI.imgui_TableSetupColumn("Icon", (int)MonoCore.E3ImGUI.ImGuiTableColumnFlags.ImGuiTableColumnFlags_WidthFixed, iconSize + 4f);
                MonoCore.E3ImGUI.imgui_TableSetupColumn("Name", (int)MonoCore.E3ImGUI.ImGuiTableColumnFlags.ImGuiTableColumnFlags_WidthStretch, 0f);

                if (buffs.Count == 0)
                {
                    MonoCore.E3ImGUI.imgui_TableNextRowEx(0, targetRowHeight);
                    MonoCore.E3ImGUI.imgui_TableSetColumnIndex(0);
                    MonoCore.E3ImGUI.imgui_TextColored(0.6f, 0.6f, 0.6f, 1.0f, emptyMessage);
                    return;
                }

                for (int i = 0; i < buffs.Count; i++)
                {
                    var buff = buffs[i];
                    MonoCore.E3ImGUI.imgui_TableNextRowEx(0, targetRowHeight);

                    MonoCore.E3ImGUI.imgui_TableSetColumnIndex(0);
                    float iconCellTop = MonoCore.E3ImGUI.imgui_GetCursorPosY();
                    MonoCore.E3ImGUI.imgui_SetCursorPosY(iconCellTop + iconOffset);
                    if (buff.IconIndex > 0)
                        MonoCore.E3ImGUI.imgui_DrawSpellIconByIconIndex(buff.IconIndex, iconSize);
                    else
                        MonoCore.E3ImGUI.imgui_DrawSpellIconBySpellID(buff.SpellId, iconSize);

                    MonoCore.E3ImGUI.imgui_TableSetColumnIndex(1);
                    float textCellTop = MonoCore.E3ImGUI.imgui_GetCursorPosY();
                    MonoCore.E3ImGUI.imgui_SetCursorPosY(textCellTop + textOffset);
                    bool lowDuration = buff.SecondsRemaining > 0 && buff.SecondsRemaining <= 30;
                    if (lowDuration)
                        MonoCore.E3ImGUI.imgui_TextColored(1.0f, 0.75f, 0.3f, 1.0f, buff.Name);
                    else
                        MonoCore.E3ImGUI.imgui_Text(buff.Name);

                    bool hovered = MonoCore.E3ImGUI.imgui_IsItemHovered();
                    if (hovered && MonoCore.E3ImGUI.imgui_IsMouseClicked(0))
                    {
                        RequestRemoveBuff(buff.Name);
                    }

                    if (hovered)
                    {
                        ShowBuffTooltip(buff);
                    }

                    string contextId = $"BuffCtx_{tableId}_{i}";
                    if (MonoCore.E3ImGUI.imgui_BeginPopupContextItem(contextId, 1))
                    {
                        MonoCore.E3ImGUI.imgui_Text(buff.Name);
                        MonoCore.E3ImGUI.imgui_Text($"Time Left: {FormatDuration(buff.SecondsRemaining)}");
                        if (buff.SpellId > 0)
                            MonoCore.E3ImGUI.imgui_Text($"Spell ID: {buff.SpellId}");
                        MonoCore.E3ImGUI.imgui_Separator();
                        if (MonoCore.E3ImGUI.imgui_MenuItem("Remove Buff"))
                        {
                            RequestRemoveBuff(buff.Name);
                        }
                        MonoCore.E3ImGUI.imgui_EndPopup();
                    }
                }
            }
        }

        private static void ShowBuffTooltip(BuffInfo buff)
        {
            if (buff == null) return;
            MonoCore.E3ImGUI.imgui_BeginTooltip();
            MonoCore.E3ImGUI.imgui_Text(buff.Name);
            MonoCore.E3ImGUI.imgui_Text($"Time Left: {FormatDuration(buff.SecondsRemaining)}");
            if (buff.SpellId > 0)
            {
                MonoCore.E3ImGUI.imgui_Text($"Spell ID: {buff.SpellId}");
            }
            MonoCore.E3ImGUI.imgui_EndTooltip();
        }

        private static Dictionary<string, BotCastingEntry> BuildCastingLookup()
        {
            var lookup = new Dictionary<string, BotCastingEntry>(StringComparer.OrdinalIgnoreCase);
            var entries = GetBotCastingEntries();
            if (entries == null)
            {
                return lookup;
            }

            foreach (var entry in entries)
            {
                if (entry == null || string.IsNullOrWhiteSpace(entry.Name))
                    continue;

                lookup[entry.Name] = entry;
            }

            return lookup;
        }

        private static IReadOnlyList<BotCastingEntry> GetBotCastingEntries()
        {
            long now = Core.StopWatch.ElapsedMilliseconds;
            if (now < _nextBotCastingRefreshMs)
            {
                return _botCastingSnapshot;
            }

            _nextBotCastingRefreshMs = now + BotCastingRefreshMs;
            _botCastingSnapshot.Clear();

            var names = new List<string>();
            if (E3.Bots != null)
            {
                var bots = E3.Bots.BotsConnected(readOnly: true);
                if (bots != null)
                {
                    names.AddRange(bots);
                }
            }

            if (!string.IsNullOrWhiteSpace(E3.CurrentName))
            {
                names.Add(E3.CurrentName);
            }

            var orderedNames = names
                .Where(n => !string.IsNullOrWhiteSpace(n))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            orderedNames.Sort(StringComparer.OrdinalIgnoreCase);

            foreach (var name in orderedNames)
            {
                bool isSelf = string.Equals(name, E3.CurrentName, StringComparison.OrdinalIgnoreCase);
                bool hasData;
                string spell = GetBotCastingSpell(name, now, isSelf, out hasData);
                string targetName = GetBotTargetName(name, now, isSelf);
                _botCastingSnapshot.Add(new BotCastingEntry
                {
                    Name = name,
                    SpellName = spell,
                    TargetName = targetName,
                    IsSelf = isSelf,
                    HasRecentData = hasData
                });
            }

            return _botCastingSnapshot;
        }

        private static string GetBotCastingSpell(string name, long now, bool isSelf, out bool hasRecentData)
        {
            hasRecentData = false;
            if (string.IsNullOrWhiteSpace(name))
                return string.Empty;

            if (isSelf)
            {
                hasRecentData = true;
                try
                {
                    int spellId = QueryNoDelay<int>("${Me.Casting.ID}");
                    if (spellId > 0)
                    {
                        return QueryNoDelay<string>("${Me.Casting.Name}") ?? string.Empty;
                    }
                }
                catch
                {
                    // ignore MQ query errors for self
                }
                return string.Empty;
            }

            var client = NetMQServer.SharedDataClient;
            if (client == null)
            {
                return string.Empty;
            }

            if (!client.TopicUpdates.TryGetValue(name, out var topics) || topics == null)
            {
                return string.Empty;
            }

            if (topics.TryGetValue("${Me.Casting}", out var entry) && entry != null)
            {
                if (now - entry.LastUpdate <= BotCastingFreshnessMs)
                {
                    hasRecentData = true;
                    return entry.Data ?? string.Empty;
                }
            }

            if (topics.TryGetValue("${Casting}", out var castEntry) && castEntry != null)
            {
                if (now - castEntry.LastUpdate <= BotCastingFreshnessMs)
                {
                    hasRecentData = true;
                    return ExtractSpellName(castEntry.Data);
                }
            }

            return string.Empty;
        }

		private static string ExtractSpellName(string payload)
		{
			if (string.IsNullOrWhiteSpace(payload))
				return string.Empty;

			int index = payload.IndexOf(" on ", StringComparison.OrdinalIgnoreCase);
			if (index > 0)
			{
				return payload.Substring(0, index).Trim();
			}
			return payload.Trim();
		}

		private static string GetBotTargetName(string name, long now, bool isSelf)
		{
			if (string.IsNullOrWhiteSpace(name)) return string.Empty;

			if (isSelf)
			{
				try
				{
					return QueryNoDelay<string>("${Target.CleanName}") ?? string.Empty;
				}
				catch
				{
					return string.Empty;
				}
			}

			var client = NetMQServer.SharedDataClient;
			if (client == null)
			{
				return string.Empty;
			}

			if (!client.TopicUpdates.TryGetValue(name, out var topics) || topics == null)
			{
				return string.Empty;
			}

			if (!topics.TryGetValue("${Me.CurrentTargetID}", out var entry) || entry == null)
			{
				return string.Empty;
			}

			if (now - entry.LastUpdate > BotCastingFreshnessMs)
			{
				return string.Empty;
			}

			if (!int.TryParse(entry.Data, out var targetId) || targetId <= 0)
			{
				return string.Empty;
			}

			return ResolveSpawnName(targetId);
		}

		private static string ResolveSpawnName(int spawnId)
		{
			if (spawnId <= 0) return string.Empty;
			try
			{
				string name = QueryNoDelay<string>($"${{Spawn[id {spawnId}].CleanName}}");
				return name ?? string.Empty;
			}
			catch
			{
				return string.Empty;
			}
		}

        private static void RequestRemoveBuff(string buffName)
        {
            if (string.IsNullOrWhiteSpace(buffName)) return;
            string sanitized = buffName.Replace("\"", string.Empty).Trim();
            if (sanitized.Length == 0) return;
            try
            {
                E3.MQ.Cmd($"/removebuff \"{sanitized}\"");
            }
            catch (Exception ex)
            {
                E3.MQ.Write($"Failed to remove buff {sanitized}: {ex.Message}");
            }
        }

        private static string FormatDuration(int totalSeconds)
        {
            if (totalSeconds <= 0)
                return "No timer";

            var span = TimeSpan.FromSeconds(totalSeconds);
            if (span.TotalHours >= 1d)
            {
                return $"{(int)span.TotalHours:D2}:{span.Minutes:D2}:{span.Seconds:D2}";
            }
            return $"{span.Minutes:D2}:{span.Seconds:D2}";
        }

		private class BotCastingEntry
		{
			public string Name { get; set; } = string.Empty;
			public string SpellName { get; set; } = string.Empty;
			public string TargetName { get; set; } = string.Empty;
			public bool HasRecentData { get; set; }
			public bool IsSelf { get; set; }
		}
    }
}
