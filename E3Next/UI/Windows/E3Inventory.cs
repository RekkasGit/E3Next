using E3Core.Data;
using E3Core.Processors;
using E3Core.Server;
using E3Core.Settings.FeatureSettings;
using E3Core.Utility;
using MonoCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using static MonoCore.E3ImGUI;

namespace E3Core.UI.Windows
{
    public static class E3InventoryWindow
    {
        private const string WindowName = "E3 Inventory";
        private const string TopicName = "E3Inventory";
        private const string RequestTopicName = "E3InventoryReq";

        private static bool _windowInitialized;
        private static bool _imguiContextReady;
        private static bool _isWindowOpen;
        private static bool _captureNeeded;
        private static bool _publishNeeded;
        private static long _nextRefresh;
        private static long _refreshInterval = 5000;
        private static long _lastDataUpdate;

        private static string _searchText = string.Empty;
        private static string _activeTab = "Equipped";
        private static string _selectedPeerName = string.Empty;
        private static bool _bankWindowOpen;

        private static int _selectedSlotId = -1;
        private static string _assignmentOwnerFilter = "All Characters";
        private static string _assignmentLocationFilter = "All";
        private static string _assignmentDispositionFilter = "All";
        private static string _assignmentEditorItemName = string.Empty;
        private static int _assignmentEditorIcon;
        private static string _restockItemName = "food";
        private static int _restockQty = 20;
        private const string ItemInspectorWindowName = "Item Inspector###inventory_item_inspector";
        private static InventoryItem _itemInspectorItem;
        private static string _itemInspectorOwner = string.Empty;
        private static string _itemInspectorLocation = string.Empty;
        private static float _itemInspectorAnchorMinX;
        private static float _itemInspectorAnchorMinY;
        private static float _itemInspectorAnchorMaxX;
        private static float _itemInspectorAnchorMaxY;
        private static int _itemInspectorNonce;

        private static bool _augmentsShowEmptySlots;
        private static bool _augmentsIncludeEquipped = true;
        private static bool _augmentsIncludeBags = true;
        private static bool _augmentsIncludeBank = true;

        // All Characters tab filters
        private static string _acSourceFilter = "All";
        private static string _acItemTypeFilter = "All";
        private static bool _acHideNoDrop = false;
        private static string _acClassFilter = "All";
        private static string _acRaceFilter = "All";
        private static string _acSortColumn = "none";
        private static string _acSortDirection = "asc";
        private static readonly List<string> _acExcludeTypes = new List<string>();
        private static bool _acShowValueFilters = false;
        private static int _acMinValue = 0;
        private static int _acMaxValue = 999999999;
        private static int _acMinTribute = 0;
        private static int _acCurrentPage = 1;
        private static int _acItemsPerPage = 50;

        // Give panel state
        private static string _giveSourceChar = string.Empty;
        private static string _giveItemName = string.Empty;
        private static int _giveItemQty = 1;
        private static string _giveTargetPeer = string.Empty;

        private static readonly List<InventoryItem> _localItems = new List<InventoryItem>();
        private static readonly List<BagInfo> _localBags = new List<BagInfo>();
        private static readonly List<PeerInventorySummary> _peerInventories = new List<PeerInventorySummary>();
        private static readonly Dictionary<string, string> _itemSignaturesBySlot = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, long> _changedSlotExpiry = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, string> _assignmentRuleCache = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        private static readonly IMQ MQ = E3.MQ;
        private static readonly Logging _log = E3.Log;

        private static readonly (float R, float G, float B) AugmentColor = (0.65f, 0.85f, 1f);

        // Visual layout constants
        private const float TileSize = 48f;
        private const float TileSpacing = 4f;
        private const long ChangeHighlightDurationMs = 1800;

        private static readonly List<string> _invSlots = new List<string>()
        {
            "charm", "leftear", "head", "face", "rightear", "neck", "shoulder", "arms", "back",
            "leftwrist", "rightwrist", "ranged", "hands", "mainhand", "offhand", "leftfinger",
            "rightfinger", "chest", "legs", "feet", "waist", "powersource", "ammo"
        };

        // EZInventory-style 4-column humanoid layout. -1 = empty cell.
        private static readonly int[][] _equippedLayout = new int[][]
        {
            new[] { 1, 2, 3, 4 },       // leftear, head, face, rightear
            new[] { 17, -1, -1, 5 },    // chest, _, _, neck
            new[] { 7, -1, -1, 8 },     // arms, _, _, back
            new[] { 20, -1, -1, 6 },    // waist, _, _, shoulder
            new[] { 9, -1, -1, 10 },    // leftwrist, _, _, rightwrist
            new[] { 18, 12, 0, 19 },    // legs, hands, charm, feet
            new[] { -1, 15, 16, 21 },   // _, leftfinger, rightfinger, powersource
            new[] { 13, 14, 11, 22 },   // mainhand, offhand, ranged, ammo
        };

        [SubSystemInit]
        public static void Init()
        {
            if (Core._MQ2MonoVersion < 0.36m) return;

            E3ImGUI.RegisterWindow(WindowName, RenderWindow);

            EventProcessor.RegisterCommand("/e3inventory", x =>
            {
                if (Core._MQ2MonoVersion < 0.36m)
                {
                    MQ.Write("E3 Inventory window requires MQ2Mono 0.36 or greater.");
                    return;
                }
                ToggleWindow();
            }, "Toggle the E3 Inventory window");
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
                    RequestInventoryFromPeers();
                }
                else
                {
                    bool open = imgui_Begin_OpenFlagGet(WindowName);
                    imgui_Begin_OpenFlagSet(WindowName, !open);
                    _isWindowOpen = !open;
                    if (!open)
                    {
                        _captureNeeded = true;
                        RequestInventoryFromPeers();
                    }
                }
                _imguiContextReady = true;
            }
            catch (Exception ex)
            {
                _log.Write($"E3 Inventory window error: {ex.Message}", Logging.LogLevels.Error);
                _imguiContextReady = false;
            }
        }

        public static void RequestInventoryFromPeers()
        {
            try
            {
                PubServer.AddTopicMessage(RequestTopicName, E3.CurrentName);
                _captureNeeded = true;
                _publishNeeded = true;
            }
            catch (Exception ex)
            {
                _log.Write($"Failed to request inventory data: {ex.Message}", Logging.LogLevels.Error);
            }
        }

        public static void ProcessInventoryRequest(string requestingUser)
        {
            _ = requestingUser;
            try
            {
                _captureNeeded = true;
                _publishNeeded = true;
            }
            catch (Exception ex)
            {
                _log.Write($"Failed to process inventory request: {ex.Message}", Logging.LogLevels.Error);
            }
        }

        public static void Pulse()
        {
            bool shouldPublish = _publishNeeded;
            _publishNeeded = false;

            if (shouldPublish)
            {
                try
                {
                    var snapshot = InventoryDataCollector.Capture(MQ);
                    PubServer.AddTopicMessage(TopicName, InventoryDataCollector.SerializeForWire(snapshot));
                }
                catch (Exception ex)
                {
                    _log.Write($"Failed to publish inventory data: {ex.Message}", Logging.LogLevels.Error);
                }
            }

            if (!_isWindowOpen) return;

            bool isBankTab = string.Equals(_activeTab, "Bank", StringComparison.OrdinalIgnoreCase);
            long interval = isBankTab ? 5000 : _refreshInterval;
            if (!_captureNeeded && !e3util.ShouldCheck(ref _nextRefresh, interval)) return;

            _captureNeeded = false;

            try
            {
                _bankWindowOpen = MQ.Query<bool>("${Window[BigBankWnd]}");

                if (isBankTab && !_bankWindowOpen)
                {
                    // Don't overwrite existing bank data if bank is closed; just skip this cycle
                }
                else
                {
                    var snapshot = InventoryDataCollector.Capture(MQ);
                    _localItems.Clear();
                    _localItems.AddRange(snapshot.Items);
                    _localBags.Clear();
                    _localBags.AddRange(snapshot.Bags);
                    _lastDataUpdate = Core.StopWatch.ElapsedMilliseconds;
                }

                RefreshPeerData();
                ReconcileAnimatedSlots();
            }
            catch (ThreadAbortException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _log.Write($"Failed to refresh inventory data: {ex.Message}", Logging.LogLevels.Error);
            }
        }

        private static void RefreshPeerData()
        {
            _peerInventories.Clear();

            var sharedClient = NetMQServer.SharedDataClient;
            if (sharedClient == null) return;

            foreach (var kvp in sharedClient.TopicUpdates)
            {
                string bot = kvp.Key;
                if (string.IsNullOrWhiteSpace(bot)) continue;
                if (string.Equals(bot, E3.CurrentName, StringComparison.OrdinalIgnoreCase)) continue;
                if (string.Equals(bot, "proxy", StringComparison.OrdinalIgnoreCase)) continue;

                var topics = kvp.Value;
                if (topics == null) continue;

                if (!topics.TryGetValue(TopicName, out var entry))
                    continue;

                var data = InventoryDataCollector.DeserializeFromWire(entry.GetData().ToString());

                _peerInventories.Add(new PeerInventorySummary
                {
                    Name = bot,
                    LastUpdate = entry.LastUpdate,
                    Items = data?.Items.ToList() ?? new List<InventoryItem>(),
                    Bags = data?.Bags.ToList() ?? new List<BagInfo>()
                });
            }

            _peerInventories.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));
        }

        #region Main Window

        private static void RenderWindow()
        {
            if (!_imguiContextReady) return;
            if (!imgui_Begin_OpenFlagGet(WindowName)) return;

            PushCurrentTheme();
            try
            {
                E3ImAnim.BeginFrame();
                imgui_SetNextWindowSizeWithCond(780f, 620f, (int)ImGuiCond.FirstUseEver);

                using (var window = ImGUIWindow.Aquire())
                {
                    int flags = (int)ImGuiWindowFlags.ImGuiWindowFlags_NoCollapse;
                    if (!window.Begin(WindowName, flags))
                    {
                        _isWindowOpen = false;
                        return;
                    }

                    RenderSearchBar();
                    imgui_Separator();
                    RenderTabs();
                    RenderItemInspectorWindow();
                }
            }
            finally
            {
                PopCurrentTheme();
            }
        }

        private static void RenderSearchBar()
        {
            RenderPeerSelector();
            imgui_SameLine();
            imgui_Text("Search:");
            imgui_SameLine();
            imgui_SetNextItemWidth(250f);
            if (imgui_InputText("##inventory_search", _searchText))
            {
                _searchText = imgui_InputText_Get("##inventory_search") ?? string.Empty;
            }

            imgui_SameLine();
            if (imgui_Button("Clear"))
            {
                _searchText = string.Empty;
            }

            imgui_SameLine();
            if (imgui_Button("Refresh"))
            {
                _captureNeeded = true;
                RequestInventoryFromPeers();
            }

            imgui_SameLine();
            int totalItems = _localItems.Count;
            foreach (var peer in _peerInventories)
                totalItems += peer.Items.Count;
            imgui_Text($"Items: {totalItems} | Peers: {_peerInventories.Count}");

            if (_lastDataUpdate > 0)
            {
                long ageMs = Math.Max(0, Core.StopWatch.ElapsedMilliseconds - _lastDataUpdate);
                float refreshGlow = E3ImAnim.TweenFloat(StableAnimId("inventory_refresh_status"), 1,
                    ageMs < 900 ? 1f : 0f, ageMs < 900 ? 0.12f : 0.55f,
                    ImAnimEaseType.OutCubic, ImAnimPolicy.Crossfade, -1f, 0f);

                imgui_SameLine();
                imgui_TextColored(
                    0.55f - (refreshGlow * 0.15f),
                    0.65f + (refreshGlow * 0.25f),
                    0.55f - (refreshGlow * 0.10f),
                    1f,
                    $"Updated {Math.Max(0, ageMs / 1000)}s ago");
            }
        }

        private static void RenderPeerSelector()
        {
            var peers = GetAvailablePeerNames();
            string preview = string.IsNullOrEmpty(_selectedPeerName) || _selectedPeerName == E3.CurrentName
                ? $"Current ({E3.CurrentName})"
                : _selectedPeerName;

            imgui_SetNextItemWidth(160f);
            using (var combo = ImGUICombo.Aquire())
            {
                if (combo.BeginCombo("##peer_select", preview))
                {
                    if (imgui_Selectable($"Current ({E3.CurrentName})", _selectedPeerName == E3.CurrentName || string.IsNullOrEmpty(_selectedPeerName)))
                    {
                        _selectedPeerName = E3.CurrentName;
                    }

                    foreach (var peer in peers)
                    {
                        if (imgui_Selectable(peer, _selectedPeerName == peer))
                        {
                            _selectedPeerName = peer;
                        }
                    }
                }
            }
        }

        private static List<string> GetAvailablePeerNames()
        {
            var names = new List<string>();
            foreach (var peer in _peerInventories)
            {
                if (!string.IsNullOrWhiteSpace(peer.Name))
                    names.Add(peer.Name);
            }
            return names;
        }

        private static List<InventoryItem> GetSelectedInventory()
        {
            if (string.IsNullOrEmpty(_selectedPeerName) || _selectedPeerName == E3.CurrentName)
                return _localItems;

            var peer = _peerInventories.FirstOrDefault(p =>
                string.Equals(p.Name, _selectedPeerName, StringComparison.OrdinalIgnoreCase));
            return peer?.Items ?? new List<InventoryItem>();
        }

        private static List<BagInfo> GetSelectedBags()
        {
            if (string.IsNullOrEmpty(_selectedPeerName) || _selectedPeerName == E3.CurrentName)
                return _localBags;

            var peer = _peerInventories.FirstOrDefault(p =>
                string.Equals(p.Name, _selectedPeerName, StringComparison.OrdinalIgnoreCase));
            return peer?.Bags ?? new List<BagInfo>();
        }

        private static void RenderTabs()
        {
            using (var tabBar = ImGUITabBar.Aquire())
            {
                if (!tabBar.BeginTabBar("##inventory_tabs"))
                    return;

                var tabs = new[] { "Equipped", "Bags", "Bank", "Assignments", "All Characters", "Augments" };
                foreach (var tab in tabs)
                {
                    using (var tabItem = ImGUITabItem.Aquire())
                    {
                        if (tabItem.BeginTabItem(tab))
                        {
                            _activeTab = tab;
                            switch (tab)
                            {
                                case "Equipped":
                                    RenderEquippedTab();
                                    break;
                                case "Bags":
                                    RenderBagsTab();
                                    break;
                                case "Bank":
                                    RenderBankTab();
                                    break;
                                case "Assignments":
                                    RenderAssignmentsTab();
                                    break;
                                case "All Characters":
                                    RenderAllCharactersTab();
                                    break;
                                case "Augments":
                                    RenderAugmentsTab();
                                    break;
                            }
                        }
                    }
                }
            }
        }

        #endregion

        #region Equipped Tab

        private static void RenderEquippedTab()
        {
            float availY = imgui_GetContentRegionAvailY();
            float leftWidth = 4 * TileSize + 3 * TileSpacing + 16f;
            float rightWidth = Math.Max(200f, imgui_GetContentRegionAvailX() - leftWidth - 16f);

            // Left child: equipped grid
            using (var leftChild = ImGUIChild.Aquire())
            {
                if (leftChild.BeginChild("##eq_grid", leftWidth, availY, (int)ImGuiChildFlags.None, (int)ImGuiWindowFlags.ImGuiWindowFlags_None))
                {
                    RenderEquippedGrid();
                }
            }

            imgui_SameLine(0f, 8f);

            // Right child: slot detail panel
            using (var rightChild = ImGUIChild.Aquire())
            {
                if (rightChild.BeginChild("##eq_detail", rightWidth, availY, (int)ImGuiChildFlags.None, (int)ImGuiWindowFlags.ImGuiWindowFlags_None))
                {
                    RenderSlotDetailInline();
                }
            }
        }

        private static void RenderEquippedGrid()
        {
            var inventory = GetSelectedInventory();
            var equipped = inventory.Where(i => i.Location == "Equipped").ToList();
            var equippedLookup = equipped.ToDictionary(i => i.SlotId);
            int cols = 4;

            int tableFlags = (int)(ImGuiTableFlags.ImGuiTableFlags_None |
                                   ImGuiTableFlags.ImGuiTableFlags_SizingFixedFit);

            using (var table = ImGUITable.Aquire())
            {
                if (!table.BeginTable("EquippedGrid", cols, tableFlags, 0f, 0f))
                    return;

                foreach (var row in _equippedLayout)
                {
                    imgui_TableNextRow();

                    for (int col = 0; col < row.Length; col++)
                    {
                        imgui_TableNextColumn();

                        int slotId = row[col];
                        if (slotId < 0 || slotId >= _invSlots.Count)
                            continue;

                        string slotName = _invSlots[slotId];
                        string tileId = $"##eq_{slotId}_{slotName}";
                        bool hasItem = equippedLookup.TryGetValue(slotId, out var item);
                        int icon = hasItem ? item.Icon : 0;
                        string label = hasItem ? item.Name : $"Empty: {slotName}";

                        bool clicked = imgui_InventorySlotTile(tileId, label, icon, TileSize, TileSize, false);
                        RenderInventoryTileOverlay(GetSelectedOwnerName(), hasItem ? item : null, "Equipped", slotId, 0, _selectedSlotId == slotId);

                        if (hasItem && imgui_IsItemHovered())
                        {
                            RenderItemTooltip(item, null);
                        }

                        if (clicked)
                        {
                            _selectedSlotId = slotId;
                        }
                    }
                }
            }
        }

        private static void RenderSlotDetailInline()
        {
            if (_selectedSlotId < 0)
            {
                imgui_TextColored(0.5f, 0.5f, 0.5f, 1f, "Click a slot to compare across peers.");
                return;
            }

            string slotName = _invSlots[_selectedSlotId];

            imgui_TextColored(0.9f, 0.9f, 0.5f, 1f, slotName);
            imgui_Separator();

            var entries = new List<(string Source, InventoryItem Item)>();

            // Selected peer first (if not local)
            bool selectedIsLocal = string.IsNullOrEmpty(_selectedPeerName) || _selectedPeerName == E3.CurrentName;
            if (!selectedIsLocal)
            {
                var selectedPeer = _peerInventories.FirstOrDefault(p =>
                    string.Equals(p.Name, _selectedPeerName, StringComparison.OrdinalIgnoreCase));
                var selectedItem = selectedPeer?.Items.FirstOrDefault(i =>
                    i.Location == "Equipped" && i.SlotId == _selectedSlotId);
                if (selectedItem != null)
                    entries.Add((_selectedPeerName, selectedItem));
            }

            // Local character
            var localItem = _localItems.FirstOrDefault(i =>
                i.Location == "Equipped" && i.SlotId == _selectedSlotId);
            if (localItem != null)
                entries.Add((E3.CurrentName, localItem));

            // Other peers
            foreach (var peer in _peerInventories)
            {
                if (string.Equals(peer.Name, _selectedPeerName, StringComparison.OrdinalIgnoreCase))
                    continue; // already added above

                var peerItem = peer.Items.FirstOrDefault(i =>
                    i.Location == "Equipped" && i.SlotId == _selectedSlotId);
                if (peerItem != null)
                    entries.Add((peer.Name, peerItem));
            }

            if (entries.Count == 0)
            {
                imgui_TextColored(0.75f, 0.75f, 0.75f, 1f, "No items found for this slot.");
                return;
            }

            int tableFlags = (int)(ImGuiTableFlags.ImGuiTableFlags_RowBg |
                                   ImGuiTableFlags.ImGuiTableFlags_BordersInner |
                                   ImGuiTableFlags.ImGuiTableFlags_BordersOuter |
                                   ImGuiTableFlags.ImGuiTableFlags_Resizable |
                                   ImGuiTableFlags.ImGuiTableFlags_ScrollY);

            using (var table = ImGUITable.Aquire())
            {
                if (!table.BeginTable("SlotComparison", 6, tableFlags, 0f, 0f))
                    return;

                imgui_TableSetupColumn("Icon", (int)ImGuiTableColumnFlags.ImGuiTableColumnFlags_WidthFixed, 28f);
                imgui_TableSetupColumn("Character", (int)ImGuiTableColumnFlags.ImGuiTableColumnFlags_WidthFixed, 90f);
                imgui_TableSetupColumn("Item", (int)ImGuiTableColumnFlags.ImGuiTableColumnFlags_WidthStretch, 160f);
                imgui_TableSetupColumn("AC", (int)ImGuiTableColumnFlags.ImGuiTableColumnFlags_WidthFixed, 40f);
                imgui_TableSetupColumn("HP", (int)ImGuiTableColumnFlags.ImGuiTableColumnFlags_WidthFixed, 45f);
                imgui_TableSetupColumn("Mana", (int)ImGuiTableColumnFlags.ImGuiTableColumnFlags_WidthFixed, 45f);
                imgui_TableHeadersRow();

                foreach (var entry in entries)
                {
                    imgui_TableNextRow();
                    string inspectorLocation = GetItemInspectorLocation(entry.Item);

                    imgui_TableNextColumn();
                    if (entry.Item.Icon > 0)
                    {
                        imgui_DrawItemIconByIconIndex(entry.Item.Icon, 20f);
                        if (imgui_IsItemHovered() && imgui_IsMouseClicked(0))
                        {
                            OpenItemInspector(entry.Item, entry.Source, inspectorLocation,
                                imgui_GetItemRectMinX(), imgui_GetItemRectMinY(), imgui_GetItemRectMaxX(), imgui_GetItemRectMaxY());
                        }
                    }

                    imgui_TableNextColumn();
                    imgui_Text(entry.Source);

                    imgui_TableNextColumn();
                    RenderItemNameCell(entry.Item, clickable: true, ownerName: entry.Source,
                        locationLabel: inspectorLocation);
                    if (imgui_IsItemHovered() && imgui_IsMouseClicked(0))
                    {
                        OpenItemInspector(entry.Item, entry.Source, inspectorLocation,
                            imgui_GetItemRectMinX(), imgui_GetItemRectMinY(), imgui_GetItemRectMaxX(), imgui_GetItemRectMaxY());
                    }

                    imgui_TableNextColumn();
                    RenderStatCell(entry.Item.Ac, 1.0f, 0.84f, 0.0f);   // Gold

                    imgui_TableNextColumn();
                    RenderStatCell(entry.Item.Hp, 0.0f, 0.8f, 0.0f);     // Green

                    imgui_TableNextColumn();
                    RenderStatCell(entry.Item.Mana, 0.2f, 0.4f, 1.0f);   // Blue
                }
            }
        }

        #endregion

        #region Bags Tab

        private static void RenderBagsTab()
        {
            var inventory = GetSelectedInventory();
            var bags = GetSelectedBags();
            var bagItems = inventory.Where(i => i.Location == "Bag").ToList();



            // If no bag metadata, reconstruct bags from the item SlotIds we already have.
            // This ensures grouping works even with older peer data or if BagInfo capture failed.
            if (bags.Count == 0 && bagItems.Count > 0)
            {
                var inferredBags = bagItems
                    .Where(i => i.SlotId >= 23 && i.SlotId <= 32 && i.SlotId2 > 0)
                    .GroupBy(i => i.SlotId)
                    .Select(g => new BagInfo
                    {
                        SlotId = g.Key,
                        Name = $"Pack {g.Key - 22}",
                        Icon = 0,
                        Capacity = g.Max(i => i.SlotId2),
                    })
                    .ToList();

                if (inferredBags.Count > 0)
                    bags = inferredBags;
            }

            // Fallback to flat grid if we still have no bag info at all
            if (bags.Count == 0)
            {
                var items = FilterItems(bagItems).ToList();
                if (items.Count == 0)
                {
                    imgui_TextColored(0.75f, 0.75f, 0.75f, 1f, "No bag items found.");
                    return;
                }
                RenderIconFlowGrid(items, "bag", GetSelectedOwnerName());
                return;
            }

            string searchLower = _searchText?.ToLowerInvariant() ?? string.Empty;

            foreach (var bag in bags.OrderBy(b => b.SlotId))
            {
                int bagNumber = bag.SlotId - 22;
                var itemsInBag = bagItems.Where(i => i.SlotId == bag.SlotId && i.SlotId2 > 0).ToList();

                // When searching, only show bags that have at least one match
                if (!string.IsNullOrEmpty(searchLower) && !itemsInBag.Any(i => MatchesSearch(i, searchLower)))
                    continue;

                // Bag header
                if (bag.Icon > 0)
                {
                    imgui_DrawItemIconByIconIndex(bag.Icon, 20f);
                    imgui_SameLine(0f, 6f);
                }
                imgui_Text($"Pack {bagNumber}: {bag.Name}");

                // Build full slot list (items + empty placeholders)
                var slots = new List<InventoryItem>();
                for (int slot = 1; slot <= bag.Capacity; slot++)
                {
                    var item = itemsInBag.FirstOrDefault(i => i.SlotId2 == slot);
                    if (item != null)
                    {
                        slots.Add(item);
                    }
                    else
                    {
                        slots.Add(new InventoryItem
                        {
                            Name = "",
                            Icon = 0,
                            SlotId = bag.SlotId,
                            SlotId2 = slot,
                            Location = "Bag",
                        });
                    }
                }

                RenderIconFlowGrid(slots, $"bag{bag.SlotId}", GetSelectedOwnerName());
            }
        }

        #endregion

        #region Bank Tab

        private static void RenderBankTab()
        {
            var inventory = GetSelectedInventory();
            RenderBankOperationsPanel();

            imgui_Separator();

            if (!_bankWindowOpen && inventory.Count(i => i.Location == "Bank") == 0)
            {
                imgui_TextColored(0.75f, 0.75f, 0.75f, 1f, "Open the bank window to load bank data.");
                return;
            }

            if (!_bankWindowOpen)
            {
                imgui_TextColored(0.9f, 0.7f, 0.35f, 1f, "Bank window is closed. Data may be stale.");
                imgui_Separator();
            }

            var items = FilterItems(inventory.Where(i => i.Location == "Bank")).ToList();
            if (items.Count == 0)
            {
                imgui_TextColored(0.75f, 0.75f, 0.75f, 1f, "No bank items found.");
                return;
            }

            RenderIconFlowGrid(items, "bank", GetSelectedOwnerName());
        }

        #endregion

        #region Assignments Tab

        private static void RenderAssignmentsTab()
        {
            RenderBankOperationsPanel();
            imgui_Separator();

            imgui_TextColored(0.82f, 0.88f, 0.98f, 1f, "Item assignments in E3Inventory are backed by the existing loot policy and inventory action flows.");
            imgui_TextColored(0.62f, 0.68f, 0.76f, 1f, "Use this to flag items as Keep, Sell, Skip, or Destroy, then run AutoSell or AutoBank using E3's current processors.");
            imgui_Separator();

            RenderAssignmentFilters();

            var rows = BuildAssignmentRows();
            if (rows.Count == 0)
            {
                imgui_TextColored(0.75f, 0.75f, 0.75f, 1f, "No matching items found for the current assignment filters.");
                return;
            }

            if (string.IsNullOrWhiteSpace(_assignmentEditorItemName) ||
                !rows.Any(x => string.Equals(x.ItemName, _assignmentEditorItemName, StringComparison.OrdinalIgnoreCase)))
            {
                _assignmentEditorItemName = rows[0].ItemName;
                _assignmentEditorIcon = rows[0].DisplayItem?.Icon ?? 0;
            }

            var selectedRow = rows.FirstOrDefault(x => string.Equals(x.ItemName, _assignmentEditorItemName, StringComparison.OrdinalIgnoreCase)) ?? rows[0];
            float availY = imgui_GetContentRegionAvailY();
            float leftWidth = Math.Max(360f, imgui_GetContentRegionAvailX() * 0.58f);

            imgui_Text($"Showing {rows.Count} items");
            imgui_Separator();

            using (var leftChild = ImGUIChild.Aquire())
            {
                if (leftChild.BeginChild("##assignments_list", leftWidth, availY, (int)(ImGuiChildFlags.Borders | ImGuiChildFlags.ResizeX), 0))
                {
                    int tableFlags = (int)(ImGuiTableFlags.ImGuiTableFlags_RowBg |
                                           ImGuiTableFlags.ImGuiTableFlags_BordersInner |
                                           ImGuiTableFlags.ImGuiTableFlags_BordersOuter |
                                           ImGuiTableFlags.ImGuiTableFlags_Resizable |
                                           ImGuiTableFlags.ImGuiTableFlags_ScrollY);

                    using (var table = ImGUITable.Aquire())
                    {
                        if (!table.BeginTable("AssignmentRows", 8, tableFlags, 0f, 0f))
                            return;

                        imgui_TableSetupColumn("Icon", (int)ImGuiTableColumnFlags.ImGuiTableColumnFlags_WidthFixed, 28f);
                        imgui_TableSetupColumn("Item", (int)ImGuiTableColumnFlags.ImGuiTableColumnFlags_WidthStretch, 210f);
                        imgui_TableSetupColumn("Copies", (int)ImGuiTableColumnFlags.ImGuiTableColumnFlags_WidthFixed, 55f);
                        imgui_TableSetupColumn("Peers", (int)ImGuiTableColumnFlags.ImGuiTableColumnFlags_WidthFixed, 55f);
                        imgui_TableSetupColumn("Sources", (int)ImGuiTableColumnFlags.ImGuiTableColumnFlags_WidthFixed, 100f);
                        imgui_TableSetupColumn("Value", (int)ImGuiTableColumnFlags.ImGuiTableColumnFlags_WidthFixed, 60f);
                        imgui_TableSetupColumn("Rules", (int)ImGuiTableColumnFlags.ImGuiTableColumnFlags_WidthFixed, 70f);
                        imgui_TableSetupColumn("", (int)ImGuiTableColumnFlags.ImGuiTableColumnFlags_WidthFixed, 42f);
                        imgui_TableHeadersRow();

                        foreach (var row in rows)
                        {
                            bool isSelected = string.Equals(row.ItemName, selectedRow.ItemName, StringComparison.OrdinalIgnoreCase);
                            imgui_TableNextRow();

                            imgui_TableNextColumn();
                            if (row.DisplayItem.Icon > 0)
                                imgui_DrawItemIconByIconIndex(row.DisplayItem.Icon, 20f);

                            imgui_TableNextColumn();
                            if (isSelected)
                                imgui_TextColored(0.42f, 0.78f, 1.0f, 1f, row.DisplayItem.Name);
                            else
                                RenderItemNameCell(row.DisplayItem, showNodrop: true, clickable: true,
                                    ownerName: row.PeerRows.FirstOrDefault()?.Owner ?? GetSelectedOwnerName(),
                                    locationLabel: GetItemInspectorLocation(row.DisplayItem));
                            if (imgui_IsItemHovered() && imgui_IsMouseClicked(0))
                            {
                                _assignmentEditorItemName = row.ItemName;
                                _assignmentEditorIcon = row.DisplayItem.Icon;
                                selectedRow = row;
                            }

                            imgui_TableNextColumn();
                            imgui_Text(row.TotalCopies.ToString());

                            imgui_TableNextColumn();
                            imgui_Text(row.UniqueOwners.ToString());

                            imgui_TableNextColumn();
                            imgui_Text(row.LocationSummary);

                            imgui_TableNextColumn();
                            if (row.MaxValue > 0)
                                imgui_Text((row.MaxValue / 1000).ToString());
                            else
                                imgui_TextColored(0.5f, 0.5f, 0.5f, 1f, "--");

                            imgui_TableNextColumn();
                            RenderDispositionCell(row.RuleSummary);

                            imgui_TableNextColumn();
                            if (imgui_ButtonEx($"Pick##assign_{row.ItemKey}", 32f, 0f))
                            {
                                _assignmentEditorItemName = row.ItemName;
                                _assignmentEditorIcon = row.DisplayItem.Icon;
                                selectedRow = row;
                            }
                        }
                    }
                }
            }

            imgui_SameLine(0f, 10f);

            using (var rightChild = ImGUIChild.Aquire())
            {
                if (rightChild.BeginChild("##assignments_editor", 0f, availY, (int)ImGuiChildFlags.Borders, 0))
                {
                    RenderAssignmentEditorPane(selectedRow);
                }
            }
        }

        private static void RenderAssignmentFilters()
        {
            imgui_Text("Owner:");
            imgui_SameLine();
            imgui_SetNextItemWidth(140f);
            using (var combo = ImGUICombo.Aquire())
            {
                if (combo.BeginCombo("##assignment_owner", _assignmentOwnerFilter))
                {
                    foreach (var option in GetAssignmentOwnerOptions())
                    {
                        if (imgui_Selectable(option, string.Equals(_assignmentOwnerFilter, option, StringComparison.OrdinalIgnoreCase)))
                            _assignmentOwnerFilter = option;
                    }
                }
            }

            imgui_SameLine();
            imgui_Text("Location:");
            imgui_SameLine();
            imgui_SetNextItemWidth(100f);
            using (var combo = ImGUICombo.Aquire())
            {
                if (combo.BeginCombo("##assignment_location", _assignmentLocationFilter))
                {
                    foreach (var option in new[] { "All", "Equipped", "Inventory", "Bank" })
                    {
                        if (imgui_Selectable(option, string.Equals(_assignmentLocationFilter, option, StringComparison.OrdinalIgnoreCase)))
                            _assignmentLocationFilter = option;
                    }
                }
            }

            imgui_SameLine();
            imgui_Text("Policy:");
            imgui_SameLine();
            imgui_SetNextItemWidth(100f);
            using (var combo = ImGUICombo.Aquire())
            {
                if (combo.BeginCombo("##assignment_policy", _assignmentDispositionFilter))
                {
                    foreach (var option in new[] { "All", "Keep", "Sell", "Skip", "Destroy", "Unassigned" })
                    {
                        if (imgui_Selectable(option, string.Equals(_assignmentDispositionFilter, option, StringComparison.OrdinalIgnoreCase)))
                            _assignmentDispositionFilter = option;
                    }
                }
            }
        }

        private static List<string> GetAssignmentOwnerOptions()
        {
            var result = new List<string> { "All Characters", E3.CurrentName };
            foreach (var peer in _peerInventories)
            {
                if (!string.IsNullOrWhiteSpace(peer.Name) && !result.Contains(peer.Name, StringComparer.OrdinalIgnoreCase))
                    result.Add(peer.Name);
            }
            return result;
        }

        private static List<AssignmentRow> BuildAssignmentRows()
        {
            var grouped = new Dictionary<string, AssignmentRow>(StringComparer.OrdinalIgnoreCase);
            string searchLower = _searchText?.ToLowerInvariant() ?? string.Empty;

            foreach (var item in _localItems)
            {
                AddAssignmentRow(grouped, E3.CurrentName, item, searchLower);
            }

            foreach (var peer in _peerInventories)
            {
                foreach (var item in peer.Items)
                {
                    AddAssignmentRow(grouped, peer.Name, item, searchLower);
                }
            }

            var rows = grouped.Values
                .Where(MatchesAssignmentRowFilters)
                .ToList();

            rows.Sort((a, b) =>
            {
                int cmp = string.Compare(a.ItemName, b.ItemName, StringComparison.OrdinalIgnoreCase);
                if (cmp != 0) return cmp;
                return b.UniqueOwners.CompareTo(a.UniqueOwners);
            });

            return rows;
        }

        private static void AddAssignmentRow(Dictionary<string, AssignmentRow> grouped, string owner, InventoryItem item, string searchLower)
        {
            if (item == null || string.IsNullOrWhiteSpace(item.Name))
                return;

            string source = item.Location == "Bag" ? "Inventory" : item.Location;
            string disposition = GetLootDisposition(owner, item.Name);

            if (!MatchesAssignmentOwner(owner))
                return;
            if (!string.IsNullOrWhiteSpace(searchLower) && !MatchesSearch(item, searchLower))
                return;

            string key = item.ItemId > 0 ? $"id:{item.ItemId}" : $"name:{item.Name}";
            if (!grouped.TryGetValue(key, out var row))
            {
                row = new AssignmentRow
                {
                    ItemKey = key,
                    ItemName = item.Name,
                    DisplayItem = item
                };
                grouped[key] = row;
            }

            row.PeerRows.Add(new AssignmentPeerRow
            {
                Owner = owner,
                Source = source,
                Item = item,
                Disposition = disposition
            });

            if (item.Value > row.MaxValue)
                row.MaxValue = item.Value;
            if (row.DisplayItem == null || item.Icon > row.DisplayItem.Icon)
                row.DisplayItem = item;

            RecomputeAssignmentSummary(row);
        }

        private static bool MatchesAssignmentOwner(string owner)
        {
            return string.Equals(_assignmentOwnerFilter, "All Characters", StringComparison.OrdinalIgnoreCase)
                || string.Equals(_assignmentOwnerFilter, owner, StringComparison.OrdinalIgnoreCase);
        }

        private static bool MatchesAssignmentDisposition(string disposition)
        {
            if (string.Equals(_assignmentDispositionFilter, "All", StringComparison.OrdinalIgnoreCase))
                return true;
            return string.Equals(_assignmentDispositionFilter, disposition, StringComparison.OrdinalIgnoreCase);
        }

        private static bool MatchesAssignmentRowFilters(AssignmentRow row)
        {
            if (_assignmentLocationFilter != "All" &&
                !row.PeerRows.Any(x => string.Equals(x.Source, _assignmentLocationFilter, StringComparison.OrdinalIgnoreCase)))
                return false;

            if (!MatchesAssignmentDisposition(row.RuleSummary))
            {
                if (!string.Equals(_assignmentDispositionFilter, "Unassigned", StringComparison.OrdinalIgnoreCase) ||
                    !string.Equals(row.RuleSummary, "Mixed", StringComparison.OrdinalIgnoreCase))
                    return false;
            }

            return true;
        }

        private static string GetLootDisposition(string owner, string itemName)
        {
            if (string.Equals(owner, E3.CurrentName, StringComparison.OrdinalIgnoreCase))
            {
                if (LootDataFile.Keep.Contains(itemName)) return "Keep";
                if (LootDataFile.Sell.Contains(itemName)) return "Sell";
                if (LootDataFile.Skip.Contains(itemName)) return "Skip";
                if (LootDataFile.Destroy.Contains(itemName)) return "Destroy";
                return "Unassigned";
            }

            if (_assignmentRuleCache.TryGetValue(BuildAssignmentRuleCacheKey(owner, itemName), out var disposition))
                return disposition;

            return "Unknown";
        }

        private static string BuildAssignmentRuleCacheKey(string owner, string itemName)
        {
            return $"{owner}|{itemName}";
        }

        private static void RecomputeAssignmentSummary(AssignmentRow row)
        {
            row.TotalCopies = row.PeerRows.Sum(x => Math.Max(1, x.Item.Quantity));
            row.UniqueOwners = row.PeerRows.Select(x => x.Owner).Distinct(StringComparer.OrdinalIgnoreCase).Count();
            row.LocationSummary = string.Join(", ", row.PeerRows.Select(x => x.Source).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x => x));

            var dispositions = row.PeerRows.Select(x => x.Disposition).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            if (dispositions.Count == 0)
                row.RuleSummary = "Unassigned";
            else if (dispositions.Count == 1)
                row.RuleSummary = dispositions[0];
            else
                row.RuleSummary = "Mixed";
        }

        private static void RenderDispositionCell(string disposition)
        {
            switch (disposition)
            {
                case "Keep":
                    imgui_TextColored(0.35f, 0.85f, 0.45f, 1f, disposition);
                    break;
                case "Sell":
                    imgui_TextColored(0.95f, 0.78f, 0.25f, 1f, disposition);
                    break;
                case "Destroy":
                    imgui_TextColored(0.95f, 0.35f, 0.35f, 1f, disposition);
                    break;
                case "Skip":
                    imgui_TextColored(0.72f, 0.72f, 0.72f, 1f, disposition);
                    break;
                case "Mixed":
                    imgui_TextColored(0.82f, 0.66f, 0.95f, 1f, disposition);
                    break;
                case "Unknown":
                    imgui_TextColored(0.55f, 0.62f, 0.72f, 1f, disposition);
                    break;
                default:
                    imgui_TextColored(0.55f, 0.62f, 0.72f, 1f, disposition);
                    break;
            }
        }

        private static void RenderAssignmentEditorPane(AssignmentRow row)
        {
            if (row == null)
            {
                imgui_TextColored(0.55f, 0.62f, 0.72f, 1f, "Select an item to edit peer rules.");
                return;
            }

            uint animId = StableAnimId($"assignment_editor_{row.ItemKey}");
            float panelMix = E3ImAnim.TweenFloat(animId, 1, 1f, 0.18f, ImAnimEaseType.OutCubic, ImAnimPolicy.Crossfade, -1f, 0f);
            float[] accent = E3ImAnim.TweenColor(animId, 2, 0.25f, 0.72f, 1.0f, 1f, 0.18f,
                ImAnimEaseType.OutCubic, ImAnimPolicy.Crossfade, ImAnimColorSpace.Oklab, -1f, 0.58f, 0.68f, 0.82f, 1f);

            imgui_TextColored(accent[0], accent[1], accent[2], 1f, "Rule Editor");
            imgui_SameLine();
            imgui_TextColored(0.55f + (0.2f * panelMix), 0.6f + (0.18f * panelMix), 0.68f + (0.12f * panelMix), 1f,
                $"{row.TotalCopies} copies across {row.UniqueOwners} peers");
            imgui_Separator();

            if (_assignmentEditorIcon > 0)
            {
                imgui_DrawItemIconByIconIndex(_assignmentEditorIcon, 28f);
                imgui_SameLine();
            }
            imgui_TextColored(0.85f, 0.92f, 1.0f, 1f, row.ItemName);
            imgui_TextColored(0.62f, 0.68f, 0.76f, 1f, "Per-peer rule edits dispatch to that character. Remote values are cached from changes made here.");
            imgui_Separator();

            int tableFlags = (int)(ImGuiTableFlags.ImGuiTableFlags_RowBg |
                                   ImGuiTableFlags.ImGuiTableFlags_BordersInner |
                                   ImGuiTableFlags.ImGuiTableFlags_BordersOuter |
                                   ImGuiTableFlags.ImGuiTableFlags_Resizable |
                                   ImGuiTableFlags.ImGuiTableFlags_ScrollY);

            using (var table = ImGUITable.Aquire())
            {
                if (!table.BeginTable("AssignmentEditorRows", 7, tableFlags, 0f, 360f))
                    return;

                imgui_TableSetupColumn("Peer", (int)ImGuiTableColumnFlags.ImGuiTableColumnFlags_WidthFixed, 95f);
                imgui_TableSetupColumn("Source", (int)ImGuiTableColumnFlags.ImGuiTableColumnFlags_WidthFixed, 80f);
                imgui_TableSetupColumn("Qty", (int)ImGuiTableColumnFlags.ImGuiTableColumnFlags_WidthFixed, 45f);
                imgui_TableSetupColumn("Value", (int)ImGuiTableColumnFlags.ImGuiTableColumnFlags_WidthFixed, 65f);
                imgui_TableSetupColumn("Rule", (int)ImGuiTableColumnFlags.ImGuiTableColumnFlags_WidthFixed, 85f);
                imgui_TableSetupColumn("Item", (int)ImGuiTableColumnFlags.ImGuiTableColumnFlags_WidthFixed, 110f);
                imgui_TableSetupColumn("Rule Actions", (int)ImGuiTableColumnFlags.ImGuiTableColumnFlags_WidthStretch, 265f);
                imgui_TableHeadersRow();

                foreach (var peerRow in row.PeerRows.OrderBy(x => x.Owner).ThenBy(x => x.Source))
                {
                    imgui_TableNextRow();

                    imgui_TableNextColumn();
                    var ownerColor = GetSourceColor(peerRow.Source);
                    imgui_TextColored(ownerColor.R, ownerColor.G, ownerColor.B, 1f, peerRow.Owner);

                    imgui_TableNextColumn();
                    imgui_Text(peerRow.Source);

                    imgui_TableNextColumn();
                    imgui_Text(peerRow.Item.Quantity.ToString());

                    imgui_TableNextColumn();
                    if (peerRow.Item.Value > 0)
                        imgui_Text((peerRow.Item.Value / 1000).ToString());
                    else
                        imgui_TextColored(0.5f, 0.5f, 0.5f, 1f, "--");

                    imgui_TableNextColumn();
                    RenderDispositionCell(peerRow.Disposition);

                    imgui_TableNextColumn();
                    if (string.Equals(peerRow.Source, "Bank", StringComparison.OrdinalIgnoreCase) && string.Equals(peerRow.Owner, E3.CurrentName, StringComparison.OrdinalIgnoreCase))
                    {
                        if (imgui_ButtonEx($"Get##assign_editor_{peerRow.Owner}_{peerRow.Item.ItemId}", 40f, 0f))
                            EventProcessor.ProcessMQCommand($"/e3getfrombank \"{SanitizeCommandArg(peerRow.Item.Name)}\"");
                        imgui_SameLine();
                    }
                    else if (string.Equals(peerRow.Source, "Inventory", StringComparison.OrdinalIgnoreCase) && string.Equals(peerRow.Owner, E3.CurrentName, StringComparison.OrdinalIgnoreCase))
                    {
                        if (imgui_ButtonEx($"Bank##assign_editor_{peerRow.Owner}_{peerRow.Item.ItemId}", 44f, 0f))
                            EventProcessor.ProcessMQCommand("/e3autobank");
                        imgui_SameLine();
                    }
                    if (imgui_ButtonEx($"Link##assign_editor_{peerRow.Owner}_{peerRow.Item.ItemId}", 40f, 0f) && !string.IsNullOrEmpty(peerRow.Item.ItemLink))
                        Core.mq_ExecuteItemLink(peerRow.Item.ItemLink);

                    imgui_TableNextColumn();
                    if (imgui_ButtonEx($"Keep##assign_editor_{peerRow.Owner}_{peerRow.Item.ItemId}", 44f, 0f))
                        ApplyLootDisposition(peerRow.Owner, peerRow.Item.Name, "KEEP");
                    imgui_SameLine();
                    if (imgui_ButtonEx($"Sell##assign_editor_{peerRow.Owner}_{peerRow.Item.ItemId}", 40f, 0f))
                        ApplyLootDisposition(peerRow.Owner, peerRow.Item.Name, "SELL");
                    imgui_SameLine();
                    if (imgui_ButtonEx($"Skip##assign_editor_{peerRow.Owner}_{peerRow.Item.ItemId}", 40f, 0f))
                        ApplyLootDisposition(peerRow.Owner, peerRow.Item.Name, "SKIP");
                    imgui_SameLine();
                    if (imgui_ButtonEx($"Destroy##assign_editor_{peerRow.Owner}_{peerRow.Item.ItemId}", 56f, 0f))
                        ApplyLootDisposition(peerRow.Owner, peerRow.Item.Name, "DESTROY");
                    imgui_SameLine();
                    if (imgui_ButtonEx($"Clear##assign_editor_{peerRow.Owner}_{peerRow.Item.ItemId}", 42f, 0f))
                        ClearLootDisposition(peerRow.Owner, peerRow.Item.Name);
                }
            }
        }

        private static void ApplyLootDisposition(string owner, string itemName, string disposition)
        {
            string sanitizedName = SanitizeCommandArg(itemName);
            string command = $"/E3LootAdd \"{sanitizedName}\" {disposition}";

            if (string.Equals(owner, E3.CurrentName, StringComparison.OrdinalIgnoreCase))
            {
                EventProcessor.ProcessMQCommand(command);
                _assignmentRuleCache.Remove(BuildAssignmentRuleCacheKey(owner, itemName));
            }
            else
            {
                E3.Bots.BroadcastCommandToPerson(owner, command);
                _assignmentRuleCache[BuildAssignmentRuleCacheKey(owner, itemName)] = NormalizeLootDisposition(disposition);
            }
        }

        private static string NormalizeLootDisposition(string disposition)
        {
            switch (disposition?.ToUpperInvariant())
            {
                case "KEEP": return "Keep";
                case "SELL": return "Sell";
                case "SKIP": return "Skip";
                case "DESTROY": return "Destroy";
                default: return "Unassigned";
            }
        }

        private static void ClearLootDisposition(string owner, string itemName)
        {
            string command = $"/E3LootRemove \"{SanitizeCommandArg(itemName)}\"";
            if (string.Equals(owner, E3.CurrentName, StringComparison.OrdinalIgnoreCase))
            {
                EventProcessor.ProcessMQCommand(command);
                _assignmentRuleCache.Remove(BuildAssignmentRuleCacheKey(owner, itemName));
            }
            else
            {
                E3.Bots.BroadcastCommandToPerson(owner, command);
                _assignmentRuleCache[BuildAssignmentRuleCacheKey(owner, itemName)] = "Unassigned";
            }
        }

        #endregion

        #region All Characters Tab

        private static void RenderAllCharactersTab()
        {
            if (_peerInventories.Count == 0 && _localItems.Count == 0)
            {
                imgui_TextColored(0.75f, 0.75f, 0.75f, 1f, "No inventory data available from peers.");
                return;
            }

            // Build flat list with source labels normalized to EZInventory conventions
            var allItems = new List<(string Character, string Source, InventoryItem Item, string MatchedAugment)>();
            foreach (var item in _localItems)
            {
                string source = item.Location == "Bag" ? "Inventory" : item.Location;
                allItems.Add((E3.CurrentName, source, item, null));
            }
            foreach (var peer in _peerInventories)
            {
                foreach (var item in peer.Items)
                {
                    string source = item.Location == "Bag" ? "Inventory" : item.Location;
                    allItems.Add((peer.Name, source, item, null));
                }
            }

            // Filter panel
            RenderAllCharsFilterPanel(allItems.Count);

            // Apply search
            string searchLower = _searchText?.ToLowerInvariant() ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(searchLower))
            {
                var filtered = new List<(string Character, string Source, InventoryItem Item, string MatchedAugment)>();
                foreach (var x in allItems)
                {
                    if (x.Item.Name?.ToLowerInvariant().Contains(searchLower) == true)
                    {
                        filtered.Add((x.Character, x.Source, x.Item, null));
                    }
                    else
                    {
                        foreach (var aug in x.Item.Augs)
                        {
                            if (aug.Name?.ToLowerInvariant().Contains(searchLower) == true)
                            {
                                filtered.Add((x.Character, x.Source, x.Item, aug.Name));
                                break;
                            }
                        }
                    }
                }
                allItems = filtered;
            }

            // Apply structured filters
            allItems = ApplyAllCharsFilters(allItems);

            // Sort
            ApplyAllCharsSort(allItems);

            int resultCount = allItems.Count;
            if (resultCount == 0)
            {
                imgui_TextColored(0.75f, 0.75f, 0.75f, 1f, "No matching items found with current filters.");
                return;
            }

            // Pagination
            int totalPages = Math.Max(1, (int)Math.Ceiling((double)resultCount / _acItemsPerPage));
            if (_acCurrentPage > totalPages) _acCurrentPage = totalPages;
            if (_acCurrentPage < 1) _acCurrentPage = 1;
            int startIdx = (_acCurrentPage - 1) * _acItemsPerPage;
            int endIdx = Math.Min(startIdx + _acItemsPerPage, resultCount);

            imgui_Separator();
            imgui_Text($"Page {_acCurrentPage} of {totalPages} | Showing items {startIdx + 1}-{endIdx} of {resultCount}");
            imgui_SameLine();
            if (_acCurrentPage > 1)
            {
                if (imgui_Button("< Previous"))
                    _acCurrentPage--;
            }
            else
            {
                imgui_TextColored(0.5f, 0.5f, 0.5f, 1f, "< Previous");
            }
            imgui_SameLine();
            if (_acCurrentPage < totalPages)
            {
                if (imgui_Button("Next >"))
                    _acCurrentPage++;
            }
            else
            {
                imgui_TextColored(0.5f, 0.5f, 0.5f, 1f, "Next >");
            }
            imgui_SameLine();
            imgui_SetNextItemWidth(70f);
            if (imgui_InputInt("##ac_per_page", _acItemsPerPage, 1, 10))
            {
                _acItemsPerPage = Math.Max(10, Math.Min(200, imgui_InputInt_Get("##ac_per_page")));
                _acCurrentPage = 1;
            }
            imgui_SameLine();
            imgui_Text("Items/Page");

            // Legend
            imgui_Text("Names Are Colored Based on Item Source -");
            imgui_SameLine(); imgui_TextColored(0.75f, 0.0f, 0.0f, 1f, "Red = Equipped");
            imgui_SameLine(); imgui_TextColored(0.3f, 0.8f, 0.3f, 1f, "Green = Inventory");
            imgui_SameLine(); imgui_TextColored(0.4f, 0.4f, 0.8f, 1f, "Blue = Bank");

            // Table
            int tableFlags = (int)(ImGuiTableFlags.ImGuiTableFlags_RowBg |
                                   ImGuiTableFlags.ImGuiTableFlags_BordersInner |
                                   ImGuiTableFlags.ImGuiTableFlags_BordersOuter |
                                   ImGuiTableFlags.ImGuiTableFlags_Resizable |
                                   ImGuiTableFlags.ImGuiTableFlags_ScrollY);

            using (var table = ImGUITable.Aquire())
            {
                if (!table.BeginTable("AllCharsInventory", 9, tableFlags, 0f, 400f))
                    return;

                imgui_TableSetupColumn("Character", (int)ImGuiTableColumnFlags.ImGuiTableColumnFlags_WidthFixed, 100f);
                imgui_TableSetupColumn("Icon", (int)ImGuiTableColumnFlags.ImGuiTableColumnFlags_WidthFixed, 28f);
                imgui_TableSetupColumn("Item", (int)ImGuiTableColumnFlags.ImGuiTableColumnFlags_WidthStretch, 180f);
                imgui_TableSetupColumn("Type", (int)ImGuiTableColumnFlags.ImGuiTableColumnFlags_WidthFixed, 80f);
                imgui_TableSetupColumn("Value", (int)ImGuiTableColumnFlags.ImGuiTableColumnFlags_WidthFixed, 60f);
                imgui_TableSetupColumn("Tribute", (int)ImGuiTableColumnFlags.ImGuiTableColumnFlags_WidthFixed, 55f);
                imgui_TableSetupColumn("Qty", (int)ImGuiTableColumnFlags.ImGuiTableColumnFlags_WidthFixed, 40f);
                imgui_TableSetupColumn("Location", (int)ImGuiTableColumnFlags.ImGuiTableColumnFlags_WidthFixed, 80f);
                imgui_TableSetupColumn("Actions", (int)ImGuiTableColumnFlags.ImGuiTableColumnFlags_WidthFixed, 50f);
                imgui_TableHeadersRow();

                for (int i = startIdx; i < endIdx; i++)
                {
                    var entry = allItems[i];
                    var item = entry.Item;
                    imgui_TableNextRow();

                    // Character
                    imgui_TableNextColumn();
                    var sc = GetSourceColor(entry.Source);
                    imgui_TextColored(sc.R, sc.G, sc.B, 1f, entry.Character);

                    // Icon
                    imgui_TableNextColumn();
                    if (item.Icon > 0)
                        imgui_DrawItemIconByIconIndex(item.Icon, 20f);

                    // Item
                    imgui_TableNextColumn();
                    RenderItemNameCell(item, showNodrop: true, clickable: true, augmentSuffix: entry.MatchedAugment,
                        ownerName: entry.Character, locationLabel: GetItemInspectorLocation(item));

                    // Type
                    imgui_TableNextColumn();
                    imgui_Text(string.IsNullOrEmpty(item.ItemType) ? "--" : item.ItemType);

                    // Value
                    imgui_TableNextColumn();
                    long plat = item.Value / 1000;
                    if (plat > 0)
                    {
                        if (plat >= 1000000)
                            imgui_Text($"{(plat / 1000000.0):F1}M");
                        else if (plat >= 10000)
                            imgui_Text($"{(plat / 1000.0):F1}K");
                        else
                            imgui_Text($"{plat}");
                    }
                    else
                    {
                        imgui_TextColored(0.5f, 0.5f, 0.5f, 1f, "--");
                    }

                    // Tribute
                    imgui_TableNextColumn();
                    if (item.Tribute > 0)
                        imgui_TextColored(0.8f, 0.4f, 0.8f, 1f, item.Tribute.ToString());
                    else
                        imgui_TextColored(0.5f, 0.5f, 0.5f, 1f, "--");

                    // Qty
                    imgui_TableNextColumn();
                    if (item.Quantity > 1)
                        imgui_TextColored(0.4f, 0.8f, 1.0f, 1f, item.Quantity.ToString());
                    else
                        imgui_Text(item.Quantity.ToString());

                    // Location
                    imgui_TableNextColumn();
                    imgui_Text(entry.Source);

                    // Actions
                    imgui_TableNextColumn();
                    string giveBtnId = $"##give_{entry.Character}_{i}";
                    if (imgui_ButtonEx($"Give{giveBtnId}", 40f, 18f))
                    {
                        _giveSourceChar = entry.Character;
                        _giveItemName = item.Name;
                        _giveItemQty = item.Quantity > 0 ? item.Quantity : 1;
                        _giveTargetPeer = string.Empty;
                    }
                }
            }

            // Give panel
            if (!string.IsNullOrEmpty(_giveSourceChar) && !string.IsNullOrEmpty(_giveItemName))
            {
                RenderGivePanel();
            }
        }

        private static void RenderAllCharsFilterPanel(int totalBeforeFilters)
        {
            float contentMinX = imgui_GetWindowContentRegionMinX();
            float contentMaxX = imgui_GetWindowContentRegionMaxX();

            if (imgui_CollapsingHeader("Filters", (int)ImGuiTreeNodeFlags.ImGuiTreeNodeFlags_DefaultOpen))
            {
                imgui_Text($"Found {totalBeforeFilters} total items.");

                // Hide No Drop right-aligned
                float checkboxWidth = imgui_CalcTextSizeX("Hide No Drop") + 26f;
                imgui_SameLine(contentMaxX - checkboxWidth);
                if (imgui_Checkbox("Hide No Drop", _acHideNoDrop))
                    _acHideNoDrop = imgui_Checkbox_Get("Hide No Drop");

                imgui_Separator();

                // Fixed column positions so combos align across rows
                const float col1Combo = 55f;
                const float col2Label = 150f;
                const float col2Combo = 230f;
                const float col3Label = 340f;
                const float col3Combo = 410f;

                // Row 1: Source, Item Type, Sort by, Clear Filters
                imgui_Text("Source:");
                imgui_SameLine(col1Combo);
                imgui_SetNextItemWidth(85f);
                using (var combo = ImGUICombo.Aquire())
                {
                    if (combo.BeginCombo("##ac_source", _acSourceFilter))
                    {
                        foreach (var opt in new[] { "All", "Equipped", "Inventory", "Bank" })
                        {
                            if (imgui_Selectable(opt, _acSourceFilter == opt))
                                _acSourceFilter = opt;
                        }
                    }
                }

                imgui_SameLine(col2Label);
                imgui_Text("Item Type:");
                imgui_SameLine(col2Combo);
                imgui_SetNextItemWidth(85f);
                using (var combo = ImGUICombo.Aquire())
                {
                    if (combo.BeginCombo("##ac_type", _acItemTypeFilter))
                    {
                        foreach (var opt in new[] { "All", "Weapon", "Armor", "Jewelry", "Consumable", "Scrolls", "Tradeskills" })
                        {
                            if (imgui_Selectable(opt, _acItemTypeFilter == opt))
                                _acItemTypeFilter = opt;
                        }
                    }
                }

                imgui_SameLine(col3Label);
                imgui_Text("Sort by:");
                imgui_SameLine(col3Combo);
                imgui_SetNextItemWidth(85f);
                using (var combo = ImGUICombo.Aquire())
                {
                    if (combo.BeginCombo("##ac_sort", _acSortColumn))
                    {
                        var sortOptions = new[] { ("none", "None"), ("name", "Item Name"), ("value", "Value"), ("tribute", "Tribute"), ("peer", "Character"), ("type", "Item Type"), ("qty", "Quantity") };
                        foreach (var (val, label) in sortOptions)
                        {
                            if (imgui_Selectable(label, _acSortColumn == val))
                                _acSortColumn = val;
                        }
                    }
                }
                if (_acSortColumn != "none")
                {
                    imgui_SameLine();
                    if (imgui_Button(_acSortDirection == "asc" ? "Asc" : "Desc"))
                        _acSortDirection = _acSortDirection == "asc" ? "desc" : "asc";
                }

                // Clear Filters right-aligned
                float clearBtnWidth = 90f;
                imgui_SameLine();
                imgui_SetCursorPosX(contentMaxX - clearBtnWidth);
                if (imgui_ButtonEx("Clear Filters", clearBtnWidth, 0f))
                    ResetAllCharsFilters();

                // Row 2: Class, Race, Exclude
                imgui_Text("Class:");
                imgui_SameLine(col1Combo);
                imgui_SetNextItemWidth(85f);
                using (var combo = ImGUICombo.Aquire())
                {
                    if (combo.BeginCombo("##ac_class", _acClassFilter))
                    {
                        foreach (var opt in new[] { "All", "WAR", "CLR", "PAL", "RNG", "SHD", "DRU", "MNK", "BRD", "ROG", "SHM", "NEC", "WIZ", "MAG", "ENC", "BST", "BER" })
                        {
                            if (imgui_Selectable(opt, _acClassFilter == opt))
                                _acClassFilter = opt;
                        }
                    }
                }

                imgui_SameLine(col2Label);
                imgui_Text("Race:");
                imgui_SameLine(col2Combo);
                imgui_SetNextItemWidth(85f);
                using (var combo = ImGUICombo.Aquire())
                {
                    if (combo.BeginCombo("##ac_race", _acRaceFilter))
                    {
                        foreach (var opt in new[] { "All", "HUM", "BAR", "ERU", "ELF", "HIE", "DEF", "HEL", "DWF", "TRL", "OGR", "HFL", "GNM", "IKS", "VAH", "FRG", "DRK" })
                        {
                            if (imgui_Selectable(opt, _acRaceFilter == opt))
                                _acRaceFilter = opt;
                        }
                    }
                }

                imgui_SameLine(col3Label);
                imgui_Text("Exclude:");
                imgui_SameLine(col3Combo);
                imgui_SetNextItemWidth(85f);
                string excludePreview = _acExcludeTypes.Count > 0 ? string.Join(", ", _acExcludeTypes) : "None";
                using (var combo = ImGUICombo.Aquire())
                {
                    if (combo.BeginCombo("##ac_exclude", excludePreview))
                    {
                        foreach (var opt in new[] { "Weapon", "Armor", "Jewelry", "Consumable", "Scrolls", "Tradeskills" })
                        {
                            bool isExcluded = _acExcludeTypes.Contains(opt);
                            if (imgui_Checkbox(opt, isExcluded))
                            {
                                bool newVal = imgui_Checkbox_Get(opt);
                                if (newVal && !_acExcludeTypes.Contains(opt))
                                    _acExcludeTypes.Add(opt);
                                else if (!newVal)
                                    _acExcludeTypes.Remove(opt);
                            }
                        }
                        imgui_Separator();
                        if (imgui_Button("Clear All"))
                            _acExcludeTypes.Clear();
                        imgui_SameLine();
                        if (imgui_Button("Select All"))
                        {
                            _acExcludeTypes.Clear();
                            _acExcludeTypes.AddRange(new[] { "Weapon", "Armor", "Jewelry", "Consumable", "Scrolls", "Tradeskills" });
                        }
                    }
                }

                // Row 3: Value filters
                if (imgui_Checkbox("Value Filters", _acShowValueFilters))
                    _acShowValueFilters = imgui_Checkbox_Get("Value Filters");
                if (_acShowValueFilters)
                {
                    imgui_SameLine(0f, 12f);
                    imgui_Text("Min Value:");
                    imgui_SameLine();
                    imgui_SetNextItemWidth(70f);
                    if (imgui_InputInt("##ac_min_val", _acMinValue, 1, 1000))
                        _acMinValue = Math.Max(0, imgui_InputInt_Get("##ac_min_val"));
                    imgui_SameLine();
                    imgui_Text("Max Value:");
                    imgui_SameLine();
                    imgui_SetNextItemWidth(70f);
                    if (imgui_InputInt("##ac_max_val", _acMaxValue, 1, 1000))
                        _acMaxValue = Math.Max(0, imgui_InputInt_Get("##ac_max_val"));
                    imgui_SameLine();
                    imgui_Text("Min Tribute:");
                    imgui_SameLine();
                    imgui_SetNextItemWidth(70f);
                    if (imgui_InputInt("##ac_min_trib", _acMinTribute, 1, 1000))
                        _acMinTribute = Math.Max(0, imgui_InputInt_Get("##ac_min_trib"));
                }
            }
        }

        private static void ResetAllCharsFilters()
        {
            _acSourceFilter = "All";
            _acItemTypeFilter = "All";
            _acHideNoDrop = false;
            _acClassFilter = "All";
            _acRaceFilter = "All";
            _acSortColumn = "none";
            _acSortDirection = "asc";
            _acExcludeTypes.Clear();
            _acShowValueFilters = false;
            _acMinValue = 0;
            _acMaxValue = 999999999;
            _acMinTribute = 0;
            _acCurrentPage = 1;
        }

        private static List<(string Character, string Source, InventoryItem Item, string MatchedAugment)> ApplyAllCharsFilters(List<(string Character, string Source, InventoryItem Item, string MatchedAugment)> items)
        {
            var result = new List<(string Character, string Source, InventoryItem Item, string MatchedAugment)>();
            foreach (var entry in items)
            {
                var item = entry.Item;

                // Source filter
                if (_acSourceFilter != "All" && _acSourceFilter != entry.Source)
                    continue;

                // Hide No Drop
                if (_acHideNoDrop && item.NoDrop)
                    continue;

                // Item type filter
                if (!MatchesItemTypeGroup(item.ItemType, _acItemTypeFilter, item.Tradeskills))
                    continue;

                // Exclude types
                bool excluded = false;
                foreach (var ex in _acExcludeTypes)
                {
                    if (MatchesItemTypeGroup(item.ItemType, ex, item.Tradeskills))
                    {
                        excluded = true;
                        break;
                    }
                }
                if (excluded)
                    continue;

                // Class filter
                if (_acClassFilter != "All")
                {
                    int classMask = GetClassMask(_acClassFilter);
                    if (classMask != 0 && (item.Classes & classMask) == 0)
                        continue;
                }

                // Race filter
                if (_acRaceFilter != "All")
                {
                    int raceMask = GetRaceMask(_acRaceFilter);
                    if (raceMask != 0 && (item.Races & raceMask) == 0)
                        continue;
                }

                // Value filters
                if (_acShowValueFilters)
                {
                    long value = item.Value;
                    if (value < _acMinValue || value > _acMaxValue)
                        continue;
                    if (item.Tribute < _acMinTribute)
                        continue;
                }

                result.Add(entry);
            }
            return result;
        }

        private static void ApplyAllCharsSort(List<(string Character, string Source, InventoryItem Item, string MatchedAugment)> items)
        {
            if (_acSortColumn == "none" || items.Count == 0)
                return;

            items.Sort((a, b) =>
            {
                int cmp = 0;
                switch (_acSortColumn)
                {
                    case "name":
                        cmp = string.Compare(a.Item.Name, b.Item.Name, StringComparison.OrdinalIgnoreCase);
                        break;
                    case "value":
                        cmp = a.Item.Value.CompareTo(b.Item.Value);
                        break;
                    case "tribute":
                        cmp = a.Item.Tribute.CompareTo(b.Item.Tribute);
                        break;
                    case "peer":
                        cmp = string.Compare(a.Character, b.Character, StringComparison.OrdinalIgnoreCase);
                        break;
                    case "type":
                        cmp = string.Compare(a.Item.ItemType, b.Item.ItemType, StringComparison.OrdinalIgnoreCase);
                        break;
                    case "qty":
                        cmp = a.Item.Quantity.CompareTo(b.Item.Quantity);
                        break;
                }
                if (cmp == 0)
                {
                    cmp = string.Compare(a.Item.Name, b.Item.Name, StringComparison.OrdinalIgnoreCase);
                    if (cmp == 0)
                        cmp = string.Compare(a.Character, b.Character, StringComparison.OrdinalIgnoreCase);
                }
                return _acSortDirection == "desc" ? -cmp : cmp;
            });
        }

        private static bool MatchesItemTypeGroup(string itemType, string group, bool tradeskills)
        {
            if (group == "All")
                return true;
            if (group == "Tradeskills")
                return tradeskills;

            switch (group)
            {
                case "Weapon":
                    return itemType == "1H Blunt" || itemType == "1H Slashing" || itemType == "2H Blunt" ||
                           itemType == "2H Slashing" || itemType == "Bow" || itemType == "Throwing" ||
                           itemType == "Wind Instrument" || itemType == "Stringed Instrument" ||
                           itemType == "Brass Instrument" || itemType == "Percussion Instrument" ||
                           itemType == "Hand to Hand" || itemType == "Piercing";
                case "Armor":
                    return itemType == "Armor" || itemType == "Shield";
                case "Jewelry":
                    return itemType == "Jewelry";
                case "Consumable":
                    return itemType == "Drink" || itemType == "Food" || itemType == "Potion";
                case "Scrolls":
                    return itemType == "Scroll" || itemType == "Spell";
            }
            return false;
        }

        private static int GetClassMask(string shortName)
        {
            switch (shortName.ToUpperInvariant())
            {
                case "WAR": return 0x1;
                case "CLR": return 0x2;
                case "PAL": return 0x4;
                case "RNG": return 0x8;
                case "SHD": return 0x10;
                case "DRU": return 0x20;
                case "MNK": return 0x40;
                case "BRD": return 0x80;
                case "ROG": return 0x100;
                case "SHM": return 0x200;
                case "NEC": return 0x400;
                case "WIZ": return 0x800;
                case "MAG": return 0x1000;
                case "ENC": return 0x2000;
                case "BST": return 0x4000;
                case "BER": return 0x8000;
            }
            return 0;
        }

        private static int GetRaceMask(string shortName)
        {
            switch (shortName.ToUpperInvariant())
            {
                case "HUM": return 0x1;
                case "BAR": return 0x2;
                case "ERU": return 0x4;
                case "ELF": return 0x8;
                case "HIE": return 0x10;
                case "DEF": return 0x20;
                case "HEL": return 0x40;
                case "DWF": return 0x80;
                case "TRL": return 0x100;
                case "OGR": return 0x200;
                case "HFL": return 0x400;
                case "GNM": return 0x800;
                case "IKS": return 0x1000;
                case "VAH": return 0x2000;
                case "FRG": return 0x4000;
                case "DRK": return 0x8000;
            }
            return 0;
        }

        private static (float R, float G, float B) GetSourceColor(string source)
        {
            switch (source)
            {
                case "Equipped": return (0.75f, 0.0f, 0.0f);
                case "Inventory": return (0.3f, 0.8f, 0.3f);
                case "Bank": return (0.4f, 0.4f, 0.8f);
                default: return (0.8f, 0.8f, 0.8f);
            }
        }

        #endregion

        #region Visual Flow Grid (Bags / Bank)

        private static void RenderIconFlowGrid(List<InventoryItem> items, string idPrefix, string ownerName)
        {
            if (items.Count == 0) return;

            float contentMinX = imgui_GetWindowContentRegionMinX();
            float contentMaxX = imgui_GetWindowContentRegionMaxX();
            float contentWidth = Math.Max(1f, contentMaxX - contentMinX);

            // How many full tiles (+ spacing between them) fit in the content width?
            int tilesPerRow = Math.Max(1, (int)((contentWidth + TileSpacing) / (TileSize + TileSpacing)));

            // Pre-compute last row info so we can center it too
            int totalRows = (items.Count + tilesPerRow - 1) / tilesPerRow;
            int tilesInLastRow = items.Count - (totalRows - 1) * tilesPerRow;
            float fullRowWidth = tilesPerRow * TileSize + (tilesPerRow - 1) * TileSpacing;
            float lastRowWidth = tilesInLastRow * TileSize + (tilesInLastRow - 1) * TileSpacing;
            float fullRowOffset = (contentWidth - fullRowWidth) * 0.5f;
            float lastRowOffset = (contentWidth - lastRowWidth) * 0.5f;

            for (int i = 0; i < items.Count; i++)
            {
                int row = i / tilesPerRow;
                int col = i % tilesPerRow;

                // Center the first tile of each row
                if (col == 0)
                {
                    bool isLastRow = (row == totalRows - 1);
                    float offset = isLastRow ? lastRowOffset : fullRowOffset;
                    imgui_SetCursorPosX(contentMinX + offset);
                }

                var item = items[i];
                string tileId = $"##{idPrefix}_{item.SlotId}_{item.SlotId2}_{i}";
                bool searchMatch = MatchesSearch(item, _searchText?.ToLowerInvariant() ?? string.Empty);

                bool isEmptySlot = string.IsNullOrEmpty(item.Name) && item.Icon == 0;
                bool clicked = imgui_InventorySlotTile(tileId, item.Name, item.Icon, TileSize, TileSize, searchMatch);
                float minX = imgui_GetItemRectMinX();
                float minY = imgui_GetItemRectMinY();
                float maxX = imgui_GetItemRectMaxX();
                float maxY = imgui_GetItemRectMaxY();

                if (isEmptySlot)
                {
                    // Overlay a darker EZInventory-style empty slot appearance
                    uint emptyBg = E3ImGUI.GetColor(28, 30, 34, 255);
                    uint emptyBorder = E3ImGUI.GetColor(48, 52, 60, 255);
                    imgui_GetWindowDrawList_AddRectFilled(minX, minY, maxX, maxY, emptyBg, 4f);
                    imgui_GetWindowDrawList_AddRect(minX, minY, maxX, maxY, emptyBorder, 4f, thickness: 1f);
                }

                RenderInventoryTileOverlay(ownerName, isEmptySlot ? null : item, item.Location, item.SlotId, item.SlotId2, false);

                if (item.Quantity > 1)
                {
                    DrawQuantityOverlay(item.Quantity);
                }

                if (imgui_IsItemHovered())
                {
                    RenderItemTooltip(item, null);
                }

                if (clicked && !isEmptySlot)
                {
                    OpenItemInspector(item, ownerName, GetItemInspectorLocation(item),
                        minX, minY, maxX, maxY);
                }

                if (col + 1 < tilesPerRow && i + 1 < items.Count)
                {
                    imgui_SameLine(0f, TileSpacing);
                }
            }
        }

        private static void DrawQuantityOverlay(int quantity)
        {
            if (quantity <= 1) return;

            string qtyText = quantity.ToString();
            uint color = E3ImGUI.GetColor(255, 255, 255, 255);
            uint shadow = E3ImGUI.GetColor(0, 0, 0, 200);

            float minX = imgui_GetItemRectMinX();
            float minY = imgui_GetItemRectMinY();
            float maxX = imgui_GetItemRectMaxX();
            float maxY = imgui_GetItemRectMaxY();

            float textX = maxX - 20f;
            float textY = maxY - 14f;

            // Shadow
            imgui_GetWindowDrawList_AddText(textX + 1, textY + 1, shadow, qtyText);
            // Text
            imgui_GetWindowDrawList_AddText(textX, textY, color, qtyText);
        }

        #endregion

        #region Animation Helpers

        private static string GetSelectedOwnerName()
        {
            if (!string.IsNullOrWhiteSpace(_selectedPeerName) && !string.Equals(_selectedPeerName, E3.CurrentName, StringComparison.OrdinalIgnoreCase))
                return _selectedPeerName;

            return E3.CurrentName;
        }

        private static void ReconcileAnimatedSlots()
        {
            long now = Core.StopWatch.ElapsedMilliseconds;
            var latest = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            CaptureInventorySlotSignatures(latest, E3.CurrentName, _localItems);
            foreach (var peer in _peerInventories)
            {
                CaptureInventorySlotSignatures(latest, peer.Name, peer.Items);
            }

            foreach (var kvp in latest)
            {
                if (!_itemSignaturesBySlot.TryGetValue(kvp.Key, out var previous) || !string.Equals(previous, kvp.Value, StringComparison.Ordinal))
                {
                    _changedSlotExpiry[kvp.Key] = now + ChangeHighlightDurationMs;
                }
            }

            foreach (var previousKey in _itemSignaturesBySlot.Keys.ToList())
            {
                if (!latest.ContainsKey(previousKey))
                {
                    _changedSlotExpiry[previousKey] = now + ChangeHighlightDurationMs;
                }
            }

            _itemSignaturesBySlot.Clear();
            foreach (var kvp in latest)
            {
                _itemSignaturesBySlot[kvp.Key] = kvp.Value;
            }

            TrimExpiredAnimatedSlots(now);
        }

        private static void CaptureInventorySlotSignatures(Dictionary<string, string> target, string ownerName, IEnumerable<InventoryItem> items)
        {
            foreach (var item in items)
            {
                string slotKey = BuildInventorySlotKey(ownerName, item.Location, item.SlotId, item.SlotId2);
                target[slotKey] = BuildInventoryItemSignature(item);
            }
        }

        private static string BuildInventorySlotKey(string ownerName, string location, int slotId, int slotId2)
        {
            return $"{ownerName}|{location}|{slotId}|{slotId2}";
        }

        private static string BuildInventoryItemSignature(InventoryItem item)
        {
            return string.Join("|",
                item.ItemId,
                item.Icon,
                item.Quantity,
                item.Name ?? string.Empty,
                item.ItemLink ?? string.Empty,
                item.Ac,
                item.Hp,
                item.Mana,
                item.Augs.Count);
        }

        private static void TrimExpiredAnimatedSlots(long now)
        {
            foreach (var key in _changedSlotExpiry.Where(x => x.Value <= now).Select(x => x.Key).ToList())
            {
                _changedSlotExpiry.Remove(key);
            }
        }

        private static void RenderInventoryTileOverlay(string ownerName, InventoryItem item, string location, int slotId, int slotId2, bool isSelected)
        {
            string slotKey = BuildInventorySlotKey(ownerName, location, slotId, slotId2);
            uint animId = StableAnimId(slotKey);

            float minX = imgui_GetItemRectMinX();
            float minY = imgui_GetItemRectMinY();
            float maxX = imgui_GetItemRectMaxX();
            float maxY = imgui_GetItemRectMaxY();
            float rounding = 4f;

            float selectedMix = E3ImAnim.TweenFloat(animId, 1, isSelected ? 1f : 0f, 0.18f,
                ImAnimEaseType.OutCubic, ImAnimPolicy.Crossfade, -1f, 0f);
            bool hasRecentChange = _changedSlotExpiry.TryGetValue(slotKey, out var expiresAt) && expiresAt > Core.StopWatch.ElapsedMilliseconds;
            float changedMix = E3ImAnim.TweenFloat(animId, 2, hasRecentChange ? 1f : 0f, hasRecentChange ? 0.12f : 0.7f,
                ImAnimEaseType.OutCubic, ImAnimPolicy.Crossfade, -1f, 0f);

            if (changedMix > 0.01f)
            {
                byte baseAlpha = (byte)Math.Max(0, Math.Min(255, (int)(changedMix * 84f)));
                byte borderAlpha = (byte)Math.Max(0, Math.Min(255, (int)(changedMix * 210f)));
                uint fillTop = item == null
                    ? E3ImGUI.GetColor(255, 160, 64, baseAlpha)
                    : E3ImGUI.GetColor(64, 196, 255, baseAlpha);
                uint fillBottom = item == null
                    ? E3ImGUI.GetColor(255, 112, 32, (byte)Math.Max(0, baseAlpha - 12))
                    : E3ImGUI.GetColor(32, 112, 255, (byte)Math.Max(0, baseAlpha - 12));
                uint border = item == null
                    ? E3ImGUI.GetColor(255, 180, 96, borderAlpha)
                    : E3ImGUI.GetColor(120, 220, 255, borderAlpha);

                imgui_GetWindowDrawList_AddRectFilledMultiColor(minX, minY, maxX, maxY, fillTop, fillTop, fillBottom, fillBottom);
                imgui_GetWindowDrawList_AddRect(minX, minY, maxX, maxY, border, rounding, thickness: 1.4f + (changedMix * 0.8f));
            }

            if (selectedMix > 0.01f)
            {
                float[] selectionColor = E3ImAnim.TweenColor(animId, 3, 0.28f, 0.72f, 1f, 0.92f, 0.18f,
                    ImAnimEaseType.OutCubic, ImAnimPolicy.Crossfade, ImAnimColorSpace.Oklab, -1f, 0.28f, 0.72f, 1f, 0f);
                uint selectedBorder = E3ImGUI.GetColor(
                    ToByte(selectionColor, 0, 0.28f),
                    ToByte(selectionColor, 1, 0.72f),
                    ToByte(selectionColor, 2, 1f),
                    ToByte(selectionColor, 3, 0f));
                uint selectedFill = E3ImGUI.GetColor(48, 124, 210, (byte)Math.Max(0, Math.Min(255, (int)(selectedMix * 44f))));

                imgui_GetWindowDrawList_AddRectFilled(minX, minY, maxX, maxY, selectedFill, rounding);
                imgui_GetWindowDrawList_AddRect(minX - 1f, minY - 1f, maxX + 1f, maxY + 1f, selectedBorder, rounding, thickness: 1.5f + (selectedMix * 1.6f));
            }
        }

        private static byte ToByte(float[] color, int index, float fallback)
        {
            float value = fallback;
            if (color != null && color.Length > index)
                value = color[index];

            return (byte)Math.Max(0, Math.Min(255, (int)(value * 255f)));
        }

        private static uint StableAnimId(string value)
        {
            unchecked
            {
                uint hash = 2166136261;
                for (int i = 0; i < value.Length; i++)
                {
                    hash ^= value[i];
                    hash *= 16777619;
                }
                return hash;
            }
        }

        #endregion

        #region Tooltips & Item Rendering

        private static void RenderItemTooltip(InventoryItem item, string slotNameOverride)
        {
            using (var tooltip = ImGUIToolTip.Aquire())
            {
                if (item == null)
                {
                    imgui_TextColored(0.6f, 0.6f, 0.6f, 1f, slotNameOverride ?? "Empty slot");
                    return;
                }

                imgui_Text(item.Name);
                imgui_Separator();

                imgui_Text($"Slot: {item.SlotName}");
                imgui_Text($"Qty: {item.Quantity}");

                if (item.Augs.Count > 0)
                {
                    imgui_Separator();
                    foreach (var aug in item.Augs)
                    {
                        imgui_TextColored(AugmentColor.R, AugmentColor.G, AugmentColor.B, 1f,
                            $"Aug {aug.Slot}: {aug.Name}");
                    }
                }
            }
        }

        private static void OpenItemInspector(InventoryItem item, string ownerName, string locationLabel,
            float minX, float minY, float maxX, float maxY)
        {
            if (item == null)
                return;

            imgui_Begin_OpenFlagSet(ItemInspectorWindowName, true);
            _itemInspectorItem = item.Clone();
            _itemInspectorOwner = ownerName ?? string.Empty;
            _itemInspectorLocation = locationLabel ?? string.Empty;
            _itemInspectorAnchorMinX = minX;
            _itemInspectorAnchorMinY = minY;
            _itemInspectorAnchorMaxX = maxX;
            _itemInspectorAnchorMaxY = maxY;
            _itemInspectorNonce++;
        }

        private static void CloseItemInspector()
        {
            imgui_Begin_OpenFlagSet(ItemInspectorWindowName, false);
            _itemInspectorItem = null;
            _itemInspectorOwner = string.Empty;
            _itemInspectorLocation = string.Empty;
        }

        private static (float X, float Y) GetItemInspectorWindowPos(float width, float height)
        {
            float windowX = imgui_GetWindowPosX();
            float windowY = imgui_GetWindowPosY();
            float windowW = imgui_GetWindowSizeX();
            float windowH = imgui_GetWindowSizeY();
            float padding = 12f;

            float rightX = _itemInspectorAnchorMaxX + 12f;
            float leftX = _itemInspectorAnchorMinX - width - 12f;
            float minX = windowX + padding;
            float maxX = windowX + windowW - width - padding;
            float minY = windowY + 36f;
            float maxY = windowY + windowH - height - padding;

            float x;
            if (rightX <= maxX)
                x = rightX;
            else if (leftX >= minX)
                x = leftX;
            else
                x = windowX + Math.Max(padding, (windowW - width) * 0.5f);

            float y = _itemInspectorAnchorMinY - 8f;
            if (maxY >= minY)
                y = Math.Max(minY, Math.Min(maxY, y));
            else
                y = minY;

            return (x, y);
        }

        private static string GetItemInspectorLocation(InventoryItem item)
        {
            switch (item?.Location)
            {
                case "Equipped":
                    return string.IsNullOrWhiteSpace(item.SlotName) ? "Equipped" : $"Equipped: {item.SlotName}";
                case "Bag":
                    return item.SlotId2 > 0 ? $"Inventory: Pack {item.SlotId - 22} Slot {item.SlotId2}" : "Inventory";
                case "Bank":
                    return item.SlotId2 > 0 ? $"Bank: Slot {item.SlotId} / {item.SlotId2}" : $"Bank: Slot {item.SlotId}";
                default:
                    return item?.Location ?? string.Empty;
            }
        }

        private static string DecodeMaskNames(int mask, params (int Value, string Label)[] options)
        {
            if (mask <= 0)
                return "--";

            var names = new List<string>();
            foreach (var option in options)
            {
                if ((mask & option.Value) != 0)
                    names.Add(option.Label);
            }

            return names.Count > 0 ? string.Join(", ", names) : "--";
        }

        private static string GetClassNames(int mask)
        {
            return DecodeMaskNames(mask,
                (0x1, "WAR"), (0x2, "CLR"), (0x4, "PAL"), (0x8, "RNG"),
                (0x10, "SHD"), (0x20, "DRU"), (0x40, "MNK"), (0x80, "BRD"),
                (0x100, "ROG"), (0x200, "SHM"), (0x400, "NEC"), (0x800, "WIZ"),
                (0x1000, "MAG"), (0x2000, "ENC"), (0x4000, "BST"), (0x8000, "BER"));
        }

        private static string GetRaceNames(int mask)
        {
            return DecodeMaskNames(mask,
                (0x1, "HUM"), (0x2, "BAR"), (0x4, "ERU"), (0x8, "ELF"),
                (0x10, "HIE"), (0x20, "DEF"), (0x40, "HEL"), (0x80, "DWF"),
                (0x100, "TRL"), (0x200, "OGR"), (0x400, "HFL"), (0x800, "GNM"),
                (0x1000, "IKS"), (0x2000, "VAH"), (0x4000, "FRG"), (0x8000, "DRK"));
        }

        private static string BuildItemInspectorFlags(InventoryItem item)
        {
            var flags = new List<string>();
            if (item.NoDrop)
                flags.Add("NO DROP");
            if (item.Tradeskills)
                flags.Add("TRADESKILL");
            if (item.Tribute > 0)
                flags.Add("TRIBUTE");

            return flags.Count > 0 ? string.Join("  ", flags) : "Tradable";
        }

        private static void RenderItemInspectorPair(string label, string value, float labelWidth = 82f)
        {
            imgui_TextColored(0.62f, 0.68f, 0.76f, 1f, label);
            imgui_SameLine(labelWidth);
            imgui_TextWrapped(string.IsNullOrWhiteSpace(value) ? "--" : value);
        }

        private static void RenderItemInspectorSummaryTable(InventoryItem item)
        {
            var rows = new List<(string Label, string Value)>
            {
                ("Owner", string.IsNullOrWhiteSpace(_itemInspectorOwner) ? GetSelectedOwnerName() : _itemInspectorOwner),
                ("Location", _itemInspectorLocation),
                ("Type", string.IsNullOrWhiteSpace(item.ItemType) ? "--" : item.ItemType),
                ("Quantity", Math.Max(1, item.Quantity).ToString()),
                ("Value", item.Value > 0 ? $"{item.Value / 1000:N0} pp" : "--"),
                ("Classes", GetClassNames(item.Classes)),
                ("Races", GetRaceNames(item.Races)),
            };

            using (var table = ImGUITable.Aquire())
            {
                if (!table.BeginTable("##item_inspector_summary", 4,
                    (int)(ImGuiTableFlags.ImGuiTableFlags_SizingFixedFit | ImGuiTableFlags.ImGuiTableFlags_NoSavedSettings), 0f, 0f))
                {
                    return;
                }

                imgui_TableSetupColumn("L1", (int)ImGuiTableColumnFlags.ImGuiTableColumnFlags_WidthFixed, 60f);
                imgui_TableSetupColumn("V1", (int)ImGuiTableColumnFlags.ImGuiTableColumnFlags_WidthFixed, 120f);
                imgui_TableSetupColumn("L2", (int)ImGuiTableColumnFlags.ImGuiTableColumnFlags_WidthFixed, 60f);
                imgui_TableSetupColumn("V2", (int)ImGuiTableColumnFlags.ImGuiTableColumnFlags_WidthStretch, 0f);

                for (int i = 0; i < rows.Count; i += 2)
                {
                    imgui_TableNextRow();

                    imgui_TableNextColumn();
                    imgui_TextColored(0.62f, 0.68f, 0.76f, 1f, rows[i].Label);
                    imgui_TableNextColumn();
                    imgui_Text(string.IsNullOrWhiteSpace(rows[i].Value) ? "--" : rows[i].Value);

                    if (i + 1 < rows.Count)
                    {
                        imgui_TableNextColumn();
                        imgui_TextColored(0.62f, 0.68f, 0.76f, 1f, rows[i + 1].Label);
                        imgui_TableNextColumn();
                        imgui_Text(string.IsNullOrWhiteSpace(rows[i + 1].Value) ? "--" : rows[i + 1].Value);
                    }
                }
            }
        }

        private static string FormatInspectorStatValue(int baseValue, int augValue)
        {
            int total = baseValue + augValue;
            if (total <= 0)
                return null;

            return total.ToString();
        }

        private static string FormatInspectorHeroicValue(int baseValue, int augValue)
        {
            int total = baseValue + augValue;
            if (total <= 0)
                return null;

            if (augValue > 0)
                return $"+{total} (+{augValue})";

            return $"+{total}";
        }

        private static void RenderItemInspectorStatTable(InventoryItem item)
        {
            int augAc = item.Augs.Sum(x => x.Ac);
            int augHp = item.Augs.Sum(x => x.Hp);
            int augMana = item.Augs.Sum(x => x.Mana);
            int augStr = item.Augs.Sum(x => x.Str);
            int augSta = item.Augs.Sum(x => x.Sta);
            int augAgi = item.Augs.Sum(x => x.Agi);
            int augDex = item.Augs.Sum(x => x.Dex);
            int augWis = item.Augs.Sum(x => x.Wis);
            int augInt = item.Augs.Sum(x => x.Intel);
            int augCha = item.Augs.Sum(x => x.Cha);
            int augHeroicStr = item.Augs.Sum(x => x.HeroicStr);
            int augHeroicSta = item.Augs.Sum(x => x.HeroicSta);
            int augHeroicAgi = item.Augs.Sum(x => x.HeroicAgi);
            int augHeroicDex = item.Augs.Sum(x => x.HeroicDex);
            int augHeroicWis = item.Augs.Sum(x => x.HeroicWis);
            int augHeroicInt = item.Augs.Sum(x => x.HeroicInt);
            int augHeroicCha = item.Augs.Sum(x => x.HeroicCha);
            int augSvMagic = item.Augs.Sum(x => x.SvMagic);
            int augSvFire = item.Augs.Sum(x => x.SvFire);
            int augSvCold = item.Augs.Sum(x => x.SvCold);
            int augSvDisease = item.Augs.Sum(x => x.SvDisease);
            int augSvPoison = item.Augs.Sum(x => x.SvPoison);
            int augSvCorruption = item.Augs.Sum(x => x.SvCorruption);
            int augHeroicSvMagic = item.Augs.Sum(x => x.HeroicSvMagic);
            int augHeroicSvFire = item.Augs.Sum(x => x.HeroicSvFire);
            int augHeroicSvCold = item.Augs.Sum(x => x.HeroicSvCold);
            int augHeroicSvDisease = item.Augs.Sum(x => x.HeroicSvDisease);
            int augHeroicSvPoison = item.Augs.Sum(x => x.HeroicSvPoison);
            int augHeroicSvCorruption = item.Augs.Sum(x => x.HeroicSvCorruption);

            var utilityStats = new List<(string Label, string Value)>
            {
                ("AC", FormatInspectorStatValue(item.Ac, augAc)),
                ("HP", FormatInspectorStatValue(item.Hp, augHp)),
                ("Mana", FormatInspectorStatValue(item.Mana, augMana)),
                ("End", item.Endurance > 0 ? item.Endurance.ToString() : null),
                ("Trib", item.Tribute > 0 ? item.Tribute.ToString() : null),
            };

            var leftStats = new List<(string Label, string Value, string Heroic)>
            {
                ("STR", FormatInspectorStatValue(item.Str, augStr), FormatInspectorHeroicValue(item.HeroicStr, augHeroicStr)),
                ("STA", FormatInspectorStatValue(item.Sta, augSta), FormatInspectorHeroicValue(item.HeroicSta, augHeroicSta)),
                ("AGI", FormatInspectorStatValue(item.Agi, augAgi), FormatInspectorHeroicValue(item.HeroicAgi, augHeroicAgi)),
                ("DEX", FormatInspectorStatValue(item.Dex, augDex), FormatInspectorHeroicValue(item.HeroicDex, augHeroicDex)),
                ("WIS", FormatInspectorStatValue(item.Wis, augWis), FormatInspectorHeroicValue(item.HeroicWis, augHeroicWis)),
                ("INT", FormatInspectorStatValue(item.Intel, augInt), FormatInspectorHeroicValue(item.HeroicInt, augHeroicInt)),
                ("CHA", FormatInspectorStatValue(item.Cha, augCha), FormatInspectorHeroicValue(item.HeroicCha, augHeroicCha)),
            };

            var rightStats = new List<(string Label, string Value, string Heroic)>
            {
                ("MR", FormatInspectorStatValue(item.SvMagic, augSvMagic), FormatInspectorHeroicValue(item.HeroicSvMagic, augHeroicSvMagic)),
                ("FR", FormatInspectorStatValue(item.SvFire, augSvFire), FormatInspectorHeroicValue(item.HeroicSvFire, augHeroicSvFire)),
                ("CR", FormatInspectorStatValue(item.SvCold, augSvCold), FormatInspectorHeroicValue(item.HeroicSvCold, augHeroicSvCold)),
                ("DR", FormatInspectorStatValue(item.SvDisease, augSvDisease), FormatInspectorHeroicValue(item.HeroicSvDisease, augHeroicSvDisease)),
                ("PR", FormatInspectorStatValue(item.SvPoison, augSvPoison), FormatInspectorHeroicValue(item.HeroicSvPoison, augHeroicSvPoison)),
                ("Corr", FormatInspectorStatValue(item.SvCorruption, augSvCorruption), FormatInspectorHeroicValue(item.HeroicSvCorruption, augHeroicSvCorruption)),
            };

            var visibleUtility = utilityStats.Where(x => !string.IsNullOrWhiteSpace(x.Value)).ToList();
            var visibleLeft = leftStats.Where(x => !string.IsNullOrWhiteSpace(x.Value) || !string.IsNullOrWhiteSpace(x.Heroic)).ToList();
            var visibleRight = rightStats.Where(x => !string.IsNullOrWhiteSpace(x.Value) || !string.IsNullOrWhiteSpace(x.Heroic)).ToList();
            int statRowCount = Math.Max(visibleLeft.Count, visibleRight.Count);
            if (visibleUtility.Count == 0 && statRowCount == 0)
                return;

            imgui_TextColored(0.92f, 0.95f, 0.98f, 1f, "Stats");
            imgui_Separator();

            if (visibleUtility.Count > 0)
            {
                using (var utilityTable = ImGUITable.Aquire())
                {
                    if (utilityTable.BeginTable("##item_inspector_utility_stats", 4,
                        (int)(ImGuiTableFlags.ImGuiTableFlags_SizingStretchProp | ImGuiTableFlags.ImGuiTableFlags_NoSavedSettings), 0f, 0f))
                    {
                        imgui_TableSetupColumn("UtilityStat1", (int)ImGuiTableColumnFlags.ImGuiTableColumnFlags_WidthFixed, 38f);
                        imgui_TableSetupColumn("UtilityValue1", (int)ImGuiTableColumnFlags.ImGuiTableColumnFlags_WidthStretch, 0f);
                        imgui_TableSetupColumn("UtilityStat2", (int)ImGuiTableColumnFlags.ImGuiTableColumnFlags_WidthFixed, 42f);
                        imgui_TableSetupColumn("UtilityValue2", (int)ImGuiTableColumnFlags.ImGuiTableColumnFlags_WidthStretch, 0f);

                        for (int i = 0; i < visibleUtility.Count; i += 2)
                        {
                            imgui_TableNextRow();

                            imgui_TableNextColumn();
                            imgui_TextColored(0.62f, 0.68f, 0.76f, 1f, visibleUtility[i].Label);
                            imgui_TableNextColumn();
                            imgui_Text(visibleUtility[i].Value);

                            if (i + 1 < visibleUtility.Count)
                            {
                                imgui_TableNextColumn();
                                imgui_TextColored(0.62f, 0.68f, 0.76f, 1f, visibleUtility[i + 1].Label);
                                imgui_TableNextColumn();
                                imgui_Text(visibleUtility[i + 1].Value);
                            }
                            else
                            {
                                imgui_TableNextColumn();
                                imgui_Text("");
                                imgui_TableNextColumn();
                                imgui_Text("");
                            }
                        }
                    }
                }

                if (statRowCount > 0)
                    imgui_Spacing();
            }

            if (statRowCount == 0)
                return;

            using (var table = ImGUITable.Aquire())
            {
                if (!table.BeginTable("##item_inspector_stats", 7,
                    (int)(ImGuiTableFlags.ImGuiTableFlags_SizingStretchProp | ImGuiTableFlags.ImGuiTableFlags_NoSavedSettings), 0f, 0f))
                {
                    return;
                }

                imgui_TableSetupColumn("Stat", (int)ImGuiTableColumnFlags.ImGuiTableColumnFlags_WidthFixed, 30f);
                imgui_TableSetupColumn("Value", (int)ImGuiTableColumnFlags.ImGuiTableColumnFlags_WidthFixed, 42f);
                imgui_TableSetupColumn("Heroic", (int)ImGuiTableColumnFlags.ImGuiTableColumnFlags_WidthFixed, 46f);
                imgui_TableSetupColumn("Gap", (int)ImGuiTableColumnFlags.ImGuiTableColumnFlags_WidthFixed, 18f);
                imgui_TableSetupColumn("Res", (int)ImGuiTableColumnFlags.ImGuiTableColumnFlags_WidthFixed, 30f);
                imgui_TableSetupColumn("ResValue", (int)ImGuiTableColumnFlags.ImGuiTableColumnFlags_WidthFixed, 36f);
                imgui_TableSetupColumn("ResHeroic", (int)ImGuiTableColumnFlags.ImGuiTableColumnFlags_WidthFixed, 44f);

                for (int i = 0; i < statRowCount; i++)
                {
                    imgui_TableNextRow();

                    if (i < visibleLeft.Count)
                    {
                        imgui_TableNextColumn();
                        imgui_TextColored(0.62f, 0.68f, 0.76f, 1f, visibleLeft[i].Label);
                        imgui_TableNextColumn();
                        imgui_Text(visibleLeft[i].Value ?? "--");
                        imgui_TableNextColumn();
                        if (!string.IsNullOrWhiteSpace(visibleLeft[i].Heroic))
                            imgui_TextColored(0.55f, 0.82f, 1f, 1f, visibleLeft[i].Heroic);
                        else
                            imgui_Text("");
                    }
                    else
                    {
                        imgui_TableNextColumn(); imgui_Text("");
                        imgui_TableNextColumn(); imgui_Text("");
                        imgui_TableNextColumn(); imgui_Text("");
                    }

                    imgui_TableNextColumn();
                    imgui_Text("");

                    if (i < visibleRight.Count)
                    {
                        imgui_TableNextColumn();
                        imgui_TextColored(0.62f, 0.68f, 0.76f, 1f, visibleRight[i].Label);
                        imgui_TableNextColumn();
                        imgui_Text(visibleRight[i].Value ?? "--");
                        imgui_TableNextColumn();
                        if (!string.IsNullOrWhiteSpace(visibleRight[i].Heroic))
                            imgui_TextColored(0.55f, 0.82f, 1f, 1f, visibleRight[i].Heroic);
                        else
                            imgui_Text("");
                    }
                    else
                    {
                        imgui_TableNextColumn(); imgui_Text("");
                        imgui_TableNextColumn(); imgui_Text("");
                        imgui_TableNextColumn(); imgui_Text("");
                    }
                }
            }
        }

        private static void RenderItemInspectorWindow()
        {
            if (_itemInspectorItem == null)
                return;

            if (!imgui_Begin_OpenFlagGet(ItemInspectorWindowName))
            {
                CloseItemInspector();
                return;
            }

            uint animId = StableAnimId($"inventory_item_inspector_{_itemInspectorNonce}");
            float alpha = E3ImAnim.TweenFloat(animId, 1, 1f, 0.18f,
                ImAnimEaseType.OutCubic, ImAnimPolicy.Crossfade, -1f, 0f);
            float slide = E3ImAnim.TweenFloat(animId, 2, 0f, 0.2f,
                ImAnimEaseType.OutCubic, ImAnimPolicy.Crossfade, -1f, 18f);
            var accent = E3ImAnim.TweenColor(animId, 3, 0.30f, 0.66f, 0.98f, 1f, 0.22f,
                ImAnimEaseType.OutCubic, ImAnimPolicy.Crossfade, ImAnimColorSpace.Oklab,
                -1f, 0.22f, 0.42f, 0.62f, 1f);

            float width = 430f;
            float height = 500f;
            var targetPos = GetItemInspectorWindowPos(width, height);

            imgui_SetNextWindowSize(width, height);
            imgui_SetNextWindowPos(targetPos.X + slide, targetPos.Y, (int)ImGuiCond.Always, 0f, 0f);
            imgui_SetNextWindowFocus();
            imgui_SetNextWindowBgAlpha(Math.Max(0.86f, alpha));

            using (var stylevar = PushStyle.Aquire())
            {
                stylevar.PushStyleVarFloat((int)ImGuiStyleVar.Alpha, alpha);

                using (var window = ImGUIWindow.Aquire())
                {
                    int flags = (int)(ImGuiWindowFlags.ImGuiWindowFlags_NoCollapse |
                                      ImGuiWindowFlags.ImGuiWindowFlags_NoSavedSettings |
                                      ImGuiWindowFlags.ImGuiWindowFlags_NoFocusOnAppearing);

                    if (!window.Begin(ItemInspectorWindowName, flags))
                        return;

                    if (!imgui_Begin_OpenFlagGet(ItemInspectorWindowName))
                    {
                        CloseItemInspector();
                        return;
                    }

                    float headerMinX = imgui_GetCursorScreenPosX();
                    float headerMinY = imgui_GetCursorScreenPosY();

                    if (_itemInspectorItem.Icon > 0)
                        imgui_DrawItemIconByIconIndex(_itemInspectorItem.Icon, 34f);

                    if (_itemInspectorItem.Icon > 0)
                        imgui_SameLine(0f, 10f);

                    imgui_BeginGroup();
                    imgui_TextColored(accent[0], accent[1], accent[2], 1f, _itemInspectorItem.Name);
                    imgui_TextDisabled(BuildItemInspectorFlags(_itemInspectorItem));
                    imgui_EndGroup();

                    imgui_SetCursorPosY(Math.Max(0f, imgui_GetCursorPosY() - 6f));
                    if (imgui_ButtonEx("Open EQ Link##item_inspector_link", 96f, 0f) && !string.IsNullOrEmpty(_itemInspectorItem.ItemLink))
                        Core.mq_ExecuteItemLink(_itemInspectorItem.ItemLink);
                    imgui_SameLine();
                    if (imgui_ButtonEx("Close##item_inspector_close", 48f, 0f))
                    {
                        CloseItemInspector();
                        return;
                    }

                    float headerMaxX = imgui_GetWindowPosX() + imgui_GetWindowSizeX() - 12f;
                    float headerMaxY = imgui_GetCursorScreenPosY() + 4f;
                    uint accentFill = E3ImGUI.GetColor((byte)(accent[0] * 255f), (byte)(accent[1] * 255f), (byte)(accent[2] * 255f), (byte)(48f * alpha));
                    uint accentBorder = E3ImGUI.GetColor((byte)(accent[0] * 255f), (byte)(accent[1] * 255f), (byte)(accent[2] * 255f), (byte)(180f * alpha));
                    imgui_GetWindowDrawList_AddRectFilled(headerMinX - 6f, headerMinY - 6f, headerMaxX, headerMaxY + 8f, accentFill, 6f);
                    imgui_GetWindowDrawList_AddRect(headerMinX - 6f, headerMinY - 6f, headerMaxX, headerMaxY + 8f, accentBorder, 6f, thickness: 1f);

                    imgui_Separator();

                    using (var child = ImGUIChild.Aquire())
                    {
                        if (!child.BeginChild("##item_inspector_scroller", 0f, 0f, (int)ImGuiChildFlags.None, 0))
                            return;

                        RenderItemInspectorSummaryTable(_itemInspectorItem);

                        RenderItemInspectorStatTable(_itemInspectorItem);

                        if (_itemInspectorItem.AugSlots.Count > 0 || _itemInspectorItem.Augs.Count > 0)
                        {
                            imgui_SeparatorText("Augments");

                            var augmentsBySlot = _itemInspectorItem.Augs
                                .GroupBy(x => x.Slot)
                                .ToDictionary(x => x.Key, x => x.First());

                            foreach (var slot in _itemInspectorItem.AugSlots.OrderBy(x => x.Slot))
                            {
                                augmentsBySlot.TryGetValue(slot.Slot, out var aug);

                                string prefix = $"Slot {slot.Slot}: Type {slot.Type}";
                                if (!slot.Visible)
                                    prefix += " (Hidden)";

                                if (aug != null)
                                {
                                    if (aug.Icon > 0)
                                        imgui_DrawItemIconByIconIndex(aug.Icon, 18f);
                                    if (aug.Icon > 0)
                                        imgui_SameLine(0f, 6f);

                                    imgui_TextColored(AugmentColor.R, AugmentColor.G, AugmentColor.B, 1f,
                                        $"{prefix}: {aug.Name}");

                                    if (aug.Ac > 0 || aug.Hp > 0 || aug.Mana > 0)
                                    {
                                        imgui_SameLine(0f, 10f);
                                        imgui_TextColored(0.62f, 0.68f, 0.76f, 1f,
                                            $"AC {aug.Ac}  HP {aug.Hp}  Mana {aug.Mana}");
                                    }
                                }
                                else
                                {
                                    imgui_TextColored(0.65f, 0.88f, 0.95f, 1f, $"{prefix}: Empty");
                                }
                            }

                            foreach (var aug in _itemInspectorItem.Augs.OrderBy(x => x.Slot))
                            {
                                if (_itemInspectorItem.AugSlots.Any(x => x.Slot == aug.Slot))
                                    continue;

                                if (aug.Icon > 0)
                                    imgui_DrawItemIconByIconIndex(aug.Icon, 18f);
                                if (aug.Icon > 0)
                                    imgui_SameLine(0f, 6f);

                                imgui_TextColored(AugmentColor.R, AugmentColor.G, AugmentColor.B, 1f,
                                    $"Slot {aug.Slot}: {aug.Name}");

                                if (aug.Ac > 0 || aug.Hp > 0 || aug.Mana > 0)
                                {
                                    imgui_SameLine(0f, 10f);
                                    imgui_TextColored(0.62f, 0.68f, 0.76f, 1f,
                                        $"AC {aug.Ac}  HP {aug.Hp}  Mana {aug.Mana}");
                                }
                            }
                        }
                    }
                }
            }
        }

        private static void RenderItemNameCell(InventoryItem item, bool showNodrop = false, bool clickable = false, string augmentSuffix = null,
            string ownerName = null, string locationLabel = null)
        {
            if (clickable)
            {
                imgui_TextColored(0.4f, 0.7f, 1f, 1f, item.Name);
            }
            else
            {
                imgui_Text(item.Name);
            }

            // Capture hover/click on the NAME text before drawing [ND]
            bool nameHovered = imgui_IsItemHovered();
            bool nameClicked = clickable && nameHovered && imgui_IsMouseClicked(0);
            float nameMinX = imgui_GetItemRectMinX();
            float nameMinY = imgui_GetItemRectMinY();
            float nameMaxX = imgui_GetItemRectMaxX();
            float nameMaxY = imgui_GetItemRectMaxY();

            if (!string.IsNullOrEmpty(augmentSuffix))
            {
                imgui_SameLine();
                imgui_TextColored(1.0f, 0.85f, 0.15f, 1f, $" ({augmentSuffix})");
            }

            if (showNodrop && item.NoDrop)
            {
                imgui_SameLine();
                imgui_TextColored(0.95f, 0.35f, 0.35f, 1f, " [ND]");
            }

            if (nameClicked)
            {
                OpenItemInspector(item, ownerName ?? GetSelectedOwnerName(), locationLabel ?? GetItemInspectorLocation(item),
                    nameMinX, nameMinY, nameMaxX, nameMaxY);
            }

            if (nameHovered)
            {
                if (clickable)
                {
                    using (var tooltip = ImGUIToolTip.Aquire())
                    {
                        imgui_Text("Click to inspect item");
                    }
                }
                else
                {
                    using (var tooltip = ImGUIToolTip.Aquire())
                    {
                        imgui_Text(item.Name);
                        if (item.Augs.Count > 0)
                        {
                            imgui_Separator();
                            foreach (var aug in item.Augs)
                            {
                                imgui_TextColored(AugmentColor.R, AugmentColor.G, AugmentColor.B, 1f,
                                    $"Aug {aug.Slot}: {aug.Name}");
                            }
                        }
                    }
                }
            }
        }

        private static void RenderStatCell(int value, float r, float g, float b)
        {
            if (value > 0)
                imgui_TextColored(r, g, b, 1f, value.ToString());
            else
                imgui_TextColored(0.5f, 0.5f, 0.5f, 1f, "--");
        }

        #endregion

        #region Search & Filter

        private static List<InventoryItem> FilterItems(IEnumerable<InventoryItem> source)
        {
            string searchLower = _searchText?.ToLowerInvariant() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(searchLower))
                return source.ToList();

            return source.Where(i => MatchesSearch(i, searchLower)).ToList();
        }

        private static bool MatchesSearch(InventoryItem item, string searchLower)
        {
            if (string.IsNullOrWhiteSpace(searchLower))
                return true;

            if (item.Name?.ToLowerInvariant().Contains(searchLower) == true)
                return true;

            foreach (var aug in item.Augs)
            {
                if (aug.Name?.ToLowerInvariant().Contains(searchLower) == true)
                    return true;
            }

            return false;
        }

        #endregion

        #region Augments Tab

        private static void RenderAugmentsTab()
        {
            var inventory = GetSelectedInventory();
            if (inventory.Count == 0)
            {
                imgui_TextColored(0.75f, 0.75f, 0.75f, 1f, "No inventory data available.");
                return;
            }

            // Toggle between inserted augments and empty slots
            if (imgui_Button(_augmentsShowEmptySlots ? "Show Inserted Augments" : "Show Empty Aug Slots"))
            {
                _augmentsShowEmptySlots = !_augmentsShowEmptySlots;
            }

            if (_augmentsShowEmptySlots)
            {
                imgui_SameLine();
                imgui_TextColored(0.75f, 0.9f, 0.75f, 1f, "Source: Equipped only (empty slot view)");
            }
            else
            {
                imgui_SameLine();
                if (imgui_Checkbox("Equipped##AugEq", _augmentsIncludeEquipped))
                    _augmentsIncludeEquipped = imgui_Checkbox_Get("Equipped##AugEq");
                imgui_SameLine();
                if (imgui_Checkbox("Bags##AugBag", _augmentsIncludeBags))
                    _augmentsIncludeBags = imgui_Checkbox_Get("Bags##AugBag");
                imgui_SameLine();
                if (imgui_Checkbox("Bank##AugBank", _augmentsIncludeBank))
                    _augmentsIncludeBank = imgui_Checkbox_Get("Bank##AugBank");
            }

            imgui_Separator();

            string searchLower = _searchText?.ToLowerInvariant() ?? string.Empty;

            if (_augmentsShowEmptySlots)
            {
                RenderEmptyAugmentSlotsTable(inventory, searchLower);
            }
            else
            {
                RenderInsertedAugmentsTable(inventory, searchLower);
            }
        }

        private static void RenderInsertedAugmentsTable(List<InventoryItem> inventory, string searchLower)
        {
            var rows = new List<AugmentRow>();

            foreach (var item in inventory)
            {
                if (!ShouldIncludeInAugments(item.Location))
                    continue;

                bool matchesSearch = string.IsNullOrWhiteSpace(searchLower) || MatchesSearch(item, searchLower);

                foreach (var aug in item.Augs)
                {
                    if (!matchesSearch && !(aug.Name?.ToLowerInvariant().Contains(searchLower) == true))
                        continue;

                    rows.Add(new AugmentRow
                    {
                        AugmentName = aug.Name,
                        AugmentIcon = aug.Icon,
                        AugmentLink = aug.ItemLink,
                        ParentItemName = item.Name,
                        ParentItemLink = item.ItemLink,
                        ParentItemIcon = item.Icon,
                        Location = GetAugmentLocationLabel(item),
                        Source = item.Location,
                        Slot = aug.Slot,
                        Ac = aug.Ac,
                        Hp = aug.Hp,
                        Mana = aug.Mana,
                        AugType = aug.AugType,
                    });
                }
            }

            rows.Sort((a, b) =>
            {
                int cmp = string.Compare(a.AugmentName, b.AugmentName, StringComparison.OrdinalIgnoreCase);
                if (cmp != 0) return cmp;
                cmp = string.Compare(a.Location, b.Location, StringComparison.OrdinalIgnoreCase);
                if (cmp != 0) return cmp;
                return a.Slot.CompareTo(b.Slot);
            });

            imgui_Text($"Found {rows.Count} inserted augments");
            imgui_Separator();

            if (rows.Count == 0)
            {
                imgui_TextColored(0.75f, 0.75f, 0.75f, 1f, "No inserted augments found.");
                return;
            }

            int tableFlags = (int)(ImGuiTableFlags.ImGuiTableFlags_RowBg |
                                   ImGuiTableFlags.ImGuiTableFlags_BordersInner |
                                   ImGuiTableFlags.ImGuiTableFlags_BordersOuter |
                                   ImGuiTableFlags.ImGuiTableFlags_Resizable |
                                   ImGuiTableFlags.ImGuiTableFlags_ScrollY);

            using (var table = ImGUITable.Aquire())
            {
                if (!table.BeginTable("InsertedAugments", 9, tableFlags, 0f, 0f))
                    return;

                imgui_TableSetupColumn("Icon", (int)ImGuiTableColumnFlags.ImGuiTableColumnFlags_WidthFixed, 28f);
                imgui_TableSetupColumn("Augment", (int)ImGuiTableColumnFlags.ImGuiTableColumnFlags_WidthStretch, 160f);
                imgui_TableSetupColumn("Inserted In", (int)ImGuiTableColumnFlags.ImGuiTableColumnFlags_WidthStretch, 160f);
                imgui_TableSetupColumn("Location", (int)ImGuiTableColumnFlags.ImGuiTableColumnFlags_WidthStretch, 120f);
                imgui_TableSetupColumn("Slot", (int)ImGuiTableColumnFlags.ImGuiTableColumnFlags_WidthFixed, 45f);
                imgui_TableSetupColumn("Fits Type", (int)ImGuiTableColumnFlags.ImGuiTableColumnFlags_WidthFixed, 80f);
                imgui_TableSetupColumn("AC", (int)ImGuiTableColumnFlags.ImGuiTableColumnFlags_WidthFixed, 40f);
                imgui_TableSetupColumn("HP", (int)ImGuiTableColumnFlags.ImGuiTableColumnFlags_WidthFixed, 45f);
                imgui_TableSetupColumn("Mana", (int)ImGuiTableColumnFlags.ImGuiTableColumnFlags_WidthFixed, 45f);
                imgui_TableHeadersRow();

                foreach (var row in rows)
                {
                    imgui_TableNextRow();

                    imgui_TableNextColumn();
                    if (row.AugmentIcon > 0)
                        imgui_DrawItemIconByIconIndex(row.AugmentIcon, 20f);

                    imgui_TableNextColumn();
                    RenderItemNameCell(row.AugmentName, row.AugmentLink, clickable: true);

                    imgui_TableNextColumn();
                    RenderItemNameCell(row.ParentItemName, row.ParentItemLink, clickable: true);

                    imgui_TableNextColumn();
                    imgui_Text(row.Location);

                    imgui_TableNextColumn();
                    imgui_Text(row.Slot.ToString());

                    imgui_TableNextColumn();
                    imgui_Text(FormatAugSlotTypes(row.AugType));

                    imgui_TableNextColumn();
                    RenderStatCell(row.Ac, 1.0f, 0.84f, 0.0f);

                    imgui_TableNextColumn();
                    RenderStatCell(row.Hp, 0.0f, 0.8f, 0.0f);

                    imgui_TableNextColumn();
                    RenderStatCell(row.Mana, 0.2f, 0.4f, 1.0f);
                }
            }
        }

        private static void RenderEmptyAugmentSlotsTable(List<InventoryItem> inventory, string searchLower)
        {
            var rows = new List<EmptySlotRow>();

            foreach (var item in inventory)
            {
                if (item.Location != "Equipped")
                    continue;

                bool matchesSearch = string.IsNullOrWhiteSpace(searchLower) || MatchesSearch(item, searchLower);

                foreach (var slot in item.AugSlots)
                {
                    if (!slot.Visible || !slot.Empty)
                        continue;

                    if (!matchesSearch)
                        continue;

                    rows.Add(new EmptySlotRow
                    {
                        ParentItemName = item.Name,
                        ParentItemLink = item.ItemLink,
                        ParentItemIcon = item.Icon,
                        Location = GetAugmentLocationLabel(item),
                        Source = item.Location,
                        Slot = slot.Slot,
                        SlotType = slot.Type,
                        SlotTypeDecoded = DecodeAugSlotTypes(slot.Type),
                    });
                }
            }

            rows.Sort((a, b) =>
            {
                int cmp = string.Compare(a.ParentItemName, b.ParentItemName, StringComparison.OrdinalIgnoreCase);
                if (cmp != 0) return cmp;
                cmp = string.Compare(a.Location, b.Location, StringComparison.OrdinalIgnoreCase);
                if (cmp != 0) return cmp;
                return a.Slot.CompareTo(b.Slot);
            });

            imgui_Text($"Found {rows.Count} empty augment slots");
            imgui_Separator();

            if (rows.Count == 0)
            {
                imgui_TextColored(0.75f, 0.75f, 0.75f, 1f, "No empty augment slots found.");
                return;
            }

            int tableFlags = (int)(ImGuiTableFlags.ImGuiTableFlags_RowBg |
                                   ImGuiTableFlags.ImGuiTableFlags_BordersInner |
                                   ImGuiTableFlags.ImGuiTableFlags_BordersOuter |
                                   ImGuiTableFlags.ImGuiTableFlags_Resizable |
                                   ImGuiTableFlags.ImGuiTableFlags_ScrollY);

            using (var table = ImGUITable.Aquire())
            {
                if (!table.BeginTable("EmptyAugmentSlots", 7, tableFlags, 0f, 0f))
                    return;

                imgui_TableSetupColumn("Icon", (int)ImGuiTableColumnFlags.ImGuiTableColumnFlags_WidthFixed, 28f);
                imgui_TableSetupColumn("Item", (int)ImGuiTableColumnFlags.ImGuiTableColumnFlags_WidthStretch, 160f);
                imgui_TableSetupColumn("Location", (int)ImGuiTableColumnFlags.ImGuiTableColumnFlags_WidthStretch, 120f);
                imgui_TableSetupColumn("Slot", (int)ImGuiTableColumnFlags.ImGuiTableColumnFlags_WidthFixed, 45f);
                imgui_TableSetupColumn("Fits Type", (int)ImGuiTableColumnFlags.ImGuiTableColumnFlags_WidthFixed, 90f);
                imgui_TableSetupColumn("Source", (int)ImGuiTableColumnFlags.ImGuiTableColumnFlags_WidthFixed, 80f);
                imgui_TableSetupColumn("Status", (int)ImGuiTableColumnFlags.ImGuiTableColumnFlags_WidthFixed, 70f);
                imgui_TableHeadersRow();

                foreach (var row in rows)
                {
                    imgui_TableNextRow();

                    imgui_TableNextColumn();
                    if (row.ParentItemIcon > 0)
                        imgui_DrawItemIconByIconIndex(row.ParentItemIcon, 20f);

                    imgui_TableNextColumn();
                    RenderItemNameCell(row.ParentItemName, row.ParentItemLink, clickable: true);

                    imgui_TableNextColumn();
                    imgui_Text(row.Location);

                    imgui_TableNextColumn();
                    imgui_Text(row.Slot.ToString());

                    imgui_TableNextColumn();
                    imgui_Text(FormatDecodedSlotTypes(row.SlotTypeDecoded));

                    imgui_TableNextColumn();
                    imgui_Text(row.Source);

                    imgui_TableNextColumn();
                    imgui_TextColored(0.65f, 0.9f, 0.65f, 1f, "Empty");
                }
            }
        }

        private static bool ShouldIncludeInAugments(string location)
        {
            if (location == "Equipped" && _augmentsIncludeEquipped) return true;
            if (location == "Bag" && _augmentsIncludeBags) return true;
            if (location == "Bank" && _augmentsIncludeBank) return true;
            return false;
        }

        private static string GetAugmentLocationLabel(InventoryItem item)
        {
            switch (item.Location)
            {
                case "Equipped":
                    return $"Equipped: {item.SlotName}";
                case "Bag":
                    return item.SlotId2 > 0 ? $"Pack {item.SlotId - 22} Slot {item.SlotId2}" : $"Pack {item.SlotId - 22}";
                case "Bank":
                    return item.SlotId2 > 0 ? $"Bank {item.SlotId} Slot {item.SlotId2}" : $"Bank {item.SlotId}";
                default:
                    return item.SlotName;
            }
        }

        private static List<int> DecodeAugSlotTypes(int rawValue)
        {
            var result = new List<int>();
            if (rawValue <= 0) return result;
            if (rawValue <= 64)
            {
                result.Add(rawValue);
            }
            else
            {
                int bitPos = 1;
                int remaining = rawValue;
                while (remaining > 0 && bitPos <= 64)
                {
                    if ((remaining & 1) == 1)
                        result.Add(bitPos);
                    remaining >>= 1;
                    bitPos++;
                }
            }
            return result;
        }

        private static string FormatAugSlotTypes(int rawValue)
        {
            var types = DecodeAugSlotTypes(rawValue);
            return types.Count > 0 ? string.Join(", ", types) : "--";
        }

        private static string FormatDecodedSlotTypes(List<int> types)
        {
            return types.Count > 0 ? string.Join(", ", types) : "--";
        }

        private static void RenderItemNameCell(string name, string itemLink, bool clickable)
        {
            if (clickable)
            {
                imgui_TextColored(0.4f, 0.7f, 1f, 1f, name);
            }
            else
            {
                imgui_Text(name);
            }

            bool nameHovered = imgui_IsItemHovered();
            bool nameClicked = clickable && nameHovered && imgui_IsMouseClicked(0);

            if (nameClicked && !string.IsNullOrEmpty(itemLink))
            {
                Core.mq_ExecuteItemLink(itemLink);
            }

            if (nameHovered && clickable)
            {
                using (var tooltip = ImGUIToolTip.Aquire())
                {
                    imgui_Text("Click to open the EQ item link");
                }
            }
        }

        private class AugmentRow
        {
            public string AugmentName;
            public int AugmentIcon;
            public string AugmentLink;
            public string ParentItemName;
            public string ParentItemLink;
            public int ParentItemIcon;
            public string Location;
            public string Source;
            public int Slot;
            public int Ac;
            public int Hp;
            public int Mana;
            public int AugType;
        }

        private class EmptySlotRow
        {
            public string ParentItemName;
            public string ParentItemLink;
            public int ParentItemIcon;
            public string Location;
            public string Source;
            public int Slot;
            public int SlotType;
            public List<int> SlotTypeDecoded;
        }

        private static void RenderGivePanel()
        {
            imgui_Separator();
            imgui_TextColored(0.9f, 0.7f, 0.3f, 1f, $"Give: {_giveItemQty}x {_giveItemName} from {_giveSourceChar}");

            imgui_SameLine();
            if (imgui_Button("Give to Me"))
            {
                ExecuteGive(_giveSourceChar, _giveItemName, _giveItemQty, E3.CurrentName);
                _giveSourceChar = string.Empty;
                _giveItemName = string.Empty;
            }

            imgui_SameLine();
            imgui_Text("| To Peer:");
            imgui_SameLine();

            // Build peer list
            var peers = new List<string>();
            if (!string.IsNullOrEmpty(E3.CurrentName))
                peers.Add(E3.CurrentName);
            foreach (var peer in _peerInventories)
            {
                if (!string.IsNullOrEmpty(peer.Name) && !peers.Contains(peer.Name, StringComparer.OrdinalIgnoreCase))
                    peers.Add(peer.Name);
            }
            if (string.IsNullOrEmpty(_giveTargetPeer) && peers.Count > 0)
                _giveTargetPeer = peers[0];

            imgui_SetNextItemWidth(120f);
            using (var combo = ImGUICombo.Aquire())
            {
                if (combo.BeginCombo("##give_target_peer", _giveTargetPeer))
                {
                    foreach (var peer in peers)
                    {
                        bool isSelected = _giveTargetPeer.Equals(peer, StringComparison.OrdinalIgnoreCase);
                        if (imgui_Selectable(peer, isSelected))
                            _giveTargetPeer = peer;
                    }
                }
            }

            imgui_SameLine();
            if (imgui_Button("Give to Peer") && !string.IsNullOrEmpty(_giveTargetPeer))
            {
                ExecuteGive(_giveSourceChar, _giveItemName, _giveItemQty, _giveTargetPeer);
                _giveSourceChar = string.Empty;
                _giveItemName = string.Empty;
            }

            imgui_SameLine();
            if (imgui_Button("Cancel"))
            {
                _giveSourceChar = string.Empty;
                _giveItemName = string.Empty;
            }
        }

        private static void ExecuteGive(string source, string itemName, int qty, string target)
        {
            if (string.IsNullOrEmpty(source) || string.IsNullOrEmpty(itemName) || string.IsNullOrEmpty(target))
                return;

            if (source.Equals(E3.CurrentName, StringComparison.OrdinalIgnoreCase))
            {
                // Local give
                GiveMe.GiveTo(target, itemName, qty);
                MQ.Write($"\ag[E3Inventory]\aw Giving {qty}x {itemName} to {target}.");
            }
            else
            {
                // Remote give via pub/sub
                string command = $"/giveme {source} \"{itemName}\" {qty} {target}";
                E3.Bots.BroadcastCommandToPerson(source, command);
                MQ.Write($"\ag[E3Inventory]\aw Requested {source} to give {qty}x {itemName} to {target}.");
            }
        }

        private static void RenderBankOperationsPanel()
        {
            string owner = GetSelectedOwnerName();
            bool isLocalOwner = string.Equals(owner, E3.CurrentName, StringComparison.OrdinalIgnoreCase);

            imgui_TextColored(0.9f, 0.85f, 0.55f, 1f, $"Operations for {owner}");
            imgui_SameLine();
            imgui_TextColored(0.62f, 0.68f, 0.76f, 1f, isLocalOwner
                ? "(local commands execute immediately)"
                : "(remote commands assume the peer is already at a banker or merchant)");

            if (imgui_Button("AutoBank"))
                DispatchInventoryOperation(owner, "/e3autobank");
            imgui_SameLine();
            if (imgui_Button("AutoSell"))
                DispatchInventoryOperation(owner, "/autosell");
            imgui_SameLine();
            if (imgui_Button("Sell+Destroy"))
                DispatchInventoryOperation(owner, "/autosell destroy");

            imgui_SameLine(0f, 14f);
            imgui_Text("Restock:");
            imgui_SameLine();
            imgui_SetNextItemWidth(110f);
            if (imgui_InputText("##restock_item", _restockItemName))
                _restockItemName = imgui_InputText_Get("##restock_item");

            imgui_SameLine();
            imgui_SetNextItemWidth(60f);
            if (imgui_InputInt("##restock_qty", _restockQty, 1, 10))
                _restockQty = Math.Max(-1, imgui_InputInt_Get("##restock_qty"));

            imgui_SameLine();
            if (imgui_Button("Run Restock"))
                RunRestockForOwner(owner, _restockItemName, _restockQty);
        }

        private static void DispatchInventoryOperation(string owner, string command)
        {
            if (string.IsNullOrWhiteSpace(owner) || string.IsNullOrWhiteSpace(command))
                return;

            if (string.Equals(owner, E3.CurrentName, StringComparison.OrdinalIgnoreCase))
            {
                EventProcessor.ProcessMQCommand(command);
            }
            else
            {
                E3.Bots.BroadcastCommandToPerson(owner, command);
            }
        }

        private static void RunRestockForOwner(string owner, string itemName, int quantity)
        {
            itemName = string.IsNullOrWhiteSpace(itemName) ? "food" : itemName.Trim();
            int vendorId = MQ.Query<int>("${Target.ID}");

            string command = quantity > 0
                ? $"/e3restock me \"{SanitizeCommandArg(itemName)}\" {quantity} {vendorId}"
                : $"/e3restock me \"{SanitizeCommandArg(itemName)}\" -1 {vendorId}";

            DispatchInventoryOperation(owner, command);
        }

        private static string SanitizeCommandArg(string value)
        {
            return (value ?? string.Empty).Replace("\"", string.Empty);
        }

        #endregion

        private class AssignmentRow
        {
            public string ItemKey;
            public string ItemName;
            public InventoryItem DisplayItem;
            public readonly List<AssignmentPeerRow> PeerRows = new List<AssignmentPeerRow>();
            public int TotalCopies;
            public int UniqueOwners;
            public long MaxValue;
            public string LocationSummary;
            public string RuleSummary;
        }

        private class AssignmentPeerRow
        {
            public string Owner;
            public string Source;
            public InventoryItem Item;
            public string Disposition;
        }

        private class PeerInventorySummary
        {
            public string Name { get; set; }
            public long LastUpdate { get; set; }
            public List<InventoryItem> Items { get; set; }
            public List<BagInfo> Bags { get; set; }
        }
    }
}
