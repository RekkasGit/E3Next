using E3Core.Data;
using E3Core.Processors;
using E3Core.Server;
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

        private static readonly IMQ MQ = E3.MQ;
        private static readonly Logging _log = E3.Log;

        private static readonly (float R, float G, float B) AugmentColor = (0.65f, 0.85f, 1f);

        // Visual layout constants
        private const float TileSize = 48f;
        private const float TileSpacing = 4f;

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

                var tabs = new[] { "Equipped", "Bags", "Bank", "All Characters", "Augments" };
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

                    imgui_TableNextColumn();
                    if (entry.Item.Icon > 0)
                        imgui_DrawItemIconByIconIndex(entry.Item.Icon, 20f);

                    imgui_TableNextColumn();
                    imgui_Text(entry.Source);

                    imgui_TableNextColumn();
                    RenderItemNameCell(entry.Item, clickable: true);

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
                RenderIconFlowGrid(items, "bag");
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

                RenderIconFlowGrid(slots, $"bag{bag.SlotId}");
            }
        }

        #endregion

        #region Bank Tab

        private static void RenderBankTab()
        {
            var inventory = GetSelectedInventory();
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

            RenderIconFlowGrid(items, "bank");
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
            var allItems = new List<(string Character, string Source, InventoryItem Item)>();
            foreach (var item in _localItems)
            {
                string source = item.Location == "Bag" ? "Inventory" : item.Location;
                allItems.Add((E3.CurrentName, source, item));
            }
            foreach (var peer in _peerInventories)
            {
                foreach (var item in peer.Items)
                {
                    string source = item.Location == "Bag" ? "Inventory" : item.Location;
                    allItems.Add((peer.Name, source, item));
                }
            }

            // Filter panel
            RenderAllCharsFilterPanel(allItems.Count);

            // Apply search
            string searchLower = _searchText?.ToLowerInvariant() ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(searchLower))
                allItems = allItems.Where(x => MatchesSearch(x.Item, searchLower)).ToList();

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
                    RenderItemNameCell(item, showNodrop: true, clickable: true);

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

        private static List<(string Character, string Source, InventoryItem Item)> ApplyAllCharsFilters(List<(string Character, string Source, InventoryItem Item)> items)
        {
            var result = new List<(string Character, string Source, InventoryItem Item)>();
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

        private static void ApplyAllCharsSort(List<(string Character, string Source, InventoryItem Item)> items)
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

        private static void RenderIconFlowGrid(List<InventoryItem> items, string idPrefix)
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

                if (isEmptySlot)
                {
                    // Overlay a darker EZInventory-style empty slot appearance
                    float minX = imgui_GetItemRectMinX();
                    float minY = imgui_GetItemRectMinY();
                    float maxX = imgui_GetItemRectMaxX();
                    float maxY = imgui_GetItemRectMaxY();
                    uint emptyBg = E3ImGUI.GetColor(28, 30, 34, 255);
                    uint emptyBorder = E3ImGUI.GetColor(48, 52, 60, 255);
                    imgui_GetWindowDrawList_AddRectFilled(minX, minY, maxX, maxY, emptyBg, 4f);
                    imgui_GetWindowDrawList_AddRect(minX, minY, maxX, maxY, emptyBorder, 4f, thickness: 1f);
                }

                if (item.Quantity > 1)
                {
                    DrawQuantityOverlay(item.Quantity);
                }

                if (imgui_IsItemHovered())
                {
                    RenderItemTooltip(item, null);
                }

                if (clicked)
                {
                    // Future: click handling (inspect, move, etc.)
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

        private static void RenderItemNameCell(InventoryItem item, bool showNodrop = false, bool clickable = false)
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

            if (showNodrop && item.NoDrop)
            {
                imgui_SameLine();
                imgui_TextColored(0.95f, 0.35f, 0.35f, 1f, " [ND]");
            }

            if (nameClicked && !string.IsNullOrEmpty(item.ItemLink))
            {
                Core.mq_ExecuteItemLink(item.ItemLink);
            }

            if (nameHovered)
            {
                if (clickable)
                {
                    using (var tooltip = ImGUIToolTip.Aquire())
                    {
                        imgui_Text("Click to view item details");
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
                    imgui_Text("Click to view item details");
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

        #endregion

        private class PeerInventorySummary
        {
            public string Name { get; set; }
            public long LastUpdate { get; set; }
            public List<InventoryItem> Items { get; set; }
            public List<BagInfo> Bags { get; set; }
        }
    }
}
