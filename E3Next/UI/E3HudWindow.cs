using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using E3Core.Data;
using E3Core.Processors;
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
        private static bool _showAaWindow = false;
        private static bool _initialAaRefreshRequested;

        private const int PlayerStatsRefreshMs = 150;
        private const int TargetInfoRefreshMs = 120;
        private const int ZoneCountRefreshMs = 500;
        private const int TargetBuffRefreshMs = 300;
        private const int PlayerBuffRefreshMs = 800;
        private const int ShortBuffRefreshMs = 800;
        private const int BotDistanceRefreshMs = 200;

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

        [SubSystemInit]
        public static void Init()
        {
            if (Core._MQ2MonoVersion < 0.35m) return;
            // Register ImGui window using E3ImGUI system
            MonoCore.E3ImGUI.RegisterWindow(WindowName, RenderMainWindow);
            MonoCore.E3ImGUI.RegisterWindow(ShortBuffWindowName, RenderShortBuffWindow);
            MonoCore.E3ImGUI.RegisterWindow(AaWindowName, RenderAaWindow);

            // Default both windows to visible
            _windowInitialized = true;
            _imguiContextReady = true;
            MonoCore.E3ImGUI.imgui_Begin_OpenFlagSet(WindowName, true);
            MonoCore.E3ImGUI.imgui_Begin_OpenFlagSet(ShortBuffWindowName, true);
            MonoCore.E3ImGUI.imgui_Begin_OpenFlagSet(AaWindowName, false);

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
                        MonoCore.E3ImGUI.imgui_Begin_OpenFlagSet(AaWindowName, false);
                    }
                    else
                    {
                        MonoCore.E3ImGUI.imgui_Begin_OpenFlagSet(AaWindowName, _showAaWindow);
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

                    RenderPlayerStats();
                    MonoCore.E3ImGUI.imgui_Separator();
                    RenderTargetInfo();
                    MonoCore.E3ImGUI.imgui_Separator();
                    RenderBotTable();
                    MonoCore.E3ImGUI.imgui_Separator();
                    RenderPlayerBuffs();
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
                    bool hideExp = IsEzServer && level >= 70;
                    int expPercent = hideExp ? 0 : (int)Math.Round(QueryNoDelay<double>("${Me.PctExp}"));
                    int aaPoints = hideExp ? GetSelfAaPoints() : 0;

                    var snapshot = _playerStatsSnapshot;
                    snapshot.PlayerName = playerName;
                    snapshot.Level = level;
                    snapshot.GameTime = gameTime;
                    snapshot.HpPercent = hpPercent;
                    snapshot.ManaPercent = manaPercent;
                    snapshot.ExpPercent = expPercent;
                    snapshot.AaPoints = aaPoints;
                    snapshot.HideExp = hideExp;
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

                using (var table = MonoCore.E3ImGUI.ImGUITable.Aquire())
                {
                    if (table.BeginTable("E3HudBotTable", 4, tableFlags, 0, 0))
                    {
                        MonoCore.E3ImGUI.imgui_TableSetupColumn("Name", (int)MonoCore.E3ImGUI.ImGuiTableColumnFlags.ImGuiTableColumnFlags_WidthStretch, 0);
                        MonoCore.E3ImGUI.imgui_TableSetupColumn("HP", (int)MonoCore.E3ImGUI.ImGuiTableColumnFlags.ImGuiTableColumnFlags_WidthFixed, 80f);
                        MonoCore.E3ImGUI.imgui_TableSetupColumn("Mana", (int)MonoCore.E3ImGUI.ImGuiTableColumnFlags.ImGuiTableColumnFlags_WidthFixed, 80f);
                        MonoCore.E3ImGUI.imgui_TableSetupColumn("Dist", (int)MonoCore.E3ImGUI.ImGuiTableColumnFlags.ImGuiTableColumnFlags_WidthFixed, 80f);
                        MonoCore.E3ImGUI.imgui_TableHeadersRow();

                        var playerStats = GetPlayerStatsSnapshot();
                        foreach (var botName in displayBots)
                        {
                            if (string.IsNullOrEmpty(botName))
                                continue;

                            try
                            {
                                bool isSelf = string.Equals(botName, E3.CurrentName, StringComparison.OrdinalIgnoreCase);
                                int hp = isSelf ? (int)Math.Round(playerStats.HpPercent) : Math.Max(0, Math.Min(100, E3.Bots.PctHealth(botName)));
                                int mana = isSelf ? (int)Math.Round(playerStats.ManaPercent) : Math.Max(0, Math.Min(100, E3.Bots.PctMana(botName)));
                                double distance = isSelf ? 0.0 : GetBotDistance(botName);

                                MonoCore.E3ImGUI.imgui_TableNextRow();

                                // Name column
                                MonoCore.E3ImGUI.imgui_TableSetColumnIndex(0);
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
                                MonoCore.E3ImGUI.imgui_TableSetColumnIndex(1);
                                if (hp >= 90) MonoCore.E3ImGUI.imgui_TextColored(0.0f, 1.0f, 0.0f, 1.0f, $"{hp,3}%");
                                else if (hp >= 60) MonoCore.E3ImGUI.imgui_TextColored(1.0f, 1.0f, 0.0f, 1.0f, $"{hp,3}%");
                                else if (hp >= 30) MonoCore.E3ImGUI.imgui_TextColored(1.0f, 0.6f, 0.0f, 1.0f, $"{hp,3}%");
                                else MonoCore.E3ImGUI.imgui_TextColored(1.0f, 0.0f, 0.0f, 1.0f, $"{hp,3}%");

                                // Mana column
                                MonoCore.E3ImGUI.imgui_TableSetColumnIndex(2);
                                
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

                                // Distance column
                                MonoCore.E3ImGUI.imgui_TableSetColumnIndex(3);
                                if (isSelf)
                                {
                                    MonoCore.E3ImGUI.imgui_Text("0");
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
            public int ExpPercent { get; set; }
            public int AaPoints { get; set; }
            public bool HideExp { get; set; }
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
    }
}
