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

        private static readonly List<InventoryItem> _localItems = new List<InventoryItem>();
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
                    _localItems.AddRange(snapshot);
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

                var items = InventoryDataCollector.DeserializeFromWire(entry.GetData().ToString());

                _peerInventories.Add(new PeerInventorySummary
                {
                    Name = bot,
                    LastUpdate = entry.LastUpdate,
                    Items = items ?? new List<InventoryItem>()
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
            if (imgui_Button("Refresh"))
            {
                _captureNeeded = true;
                RequestInventoryFromPeers();
            }

            imgui_SameLine();
            imgui_Text($"Items: {GetSelectedInventory().Count} | Peers: {_peerInventories.Count}");
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

        private static void RenderTabs()
        {
            using (var tabBar = ImGUITabBar.Aquire())
            {
                if (!tabBar.BeginTabBar("##inventory_tabs"))
                    return;

                var tabs = new[] { "Equipped", "Bags", "Bank", "All Characters" };
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
            var items = FilterItems(inventory.Where(i => i.Location == "Bag")).ToList();
            if (items.Count == 0)
            {
                imgui_TextColored(0.75f, 0.75f, 0.75f, 1f, "No bag items found.");
                return;
            }

            RenderIconFlowGrid(items, "bag");
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

            var allSources = new List<(string Name, List<InventoryItem> Items)>();
            allSources.Add((E3.CurrentName, _localItems));
            foreach (var peer in _peerInventories)
            {
                allSources.Add((peer.Name, peer.Items));
            }

            string searchLower = _searchText?.ToLowerInvariant() ?? string.Empty;
            bool hasSearch = !string.IsNullOrWhiteSpace(searchLower);

            int tableFlags = (int)(ImGuiTableFlags.ImGuiTableFlags_RowBg |
                                   ImGuiTableFlags.ImGuiTableFlags_BordersInner |
                                   ImGuiTableFlags.ImGuiTableFlags_BordersOuter |
                                   ImGuiTableFlags.ImGuiTableFlags_Resizable |
                                   ImGuiTableFlags.ImGuiTableFlags_ScrollY);

            using (var table = ImGUITable.Aquire())
            {
                if (!table.BeginTable("AllCharsInventory", 4, tableFlags, 0f, 400f))
                    return;

                imgui_TableSetupColumn("Icon", (int)ImGuiTableColumnFlags.ImGuiTableColumnFlags_WidthFixed, 28f);
                imgui_TableSetupColumn("Character", (int)ImGuiTableColumnFlags.ImGuiTableColumnFlags_WidthFixed, 120f);
                imgui_TableSetupColumn("Item", (int)ImGuiTableColumnFlags.ImGuiTableColumnFlags_WidthStretch, 220f);
                imgui_TableSetupColumn("Qty", (int)ImGuiTableColumnFlags.ImGuiTableColumnFlags_WidthFixed, 45f);
                imgui_TableHeadersRow();

                foreach (var source in allSources)
                {
                    var items = source.Items;
                    if (hasSearch)
                    {
                        items = items.Where(i => MatchesSearch(i, searchLower)).ToList();
                    }

                    foreach (var item in items.OrderBy(i => i.Name))
                    {
                        imgui_TableNextRow();

                        imgui_TableNextColumn();
                        if (item.Icon > 0)
                        {
                            imgui_DrawItemIconByIconIndex(item.Icon, 20f);
                        }

                        imgui_TableNextColumn();
                        imgui_Text(source.Name);

                        imgui_TableNextColumn();
                        RenderItemNameCell(item, showNodrop: true, clickable: true);

                        imgui_TableNextColumn();
                        imgui_Text(item.Quantity.ToString());
                    }
                }
            }
        }

        #endregion

        #region Visual Flow Grid (Bags / Bank)

        private static void RenderIconFlowGrid(List<InventoryItem> items, string idPrefix)
        {
            float availX = imgui_GetContentRegionAvailX();
            int tilesPerRow = Math.Max(1, (int)((availX + TileSpacing) / (TileSize + TileSpacing)));

            // Use a table for reliable row/column wrapping
            int tableFlags = (int)(ImGuiTableFlags.ImGuiTableFlags_None |
                                   ImGuiTableFlags.ImGuiTableFlags_SizingFixedFit);
            using (var table = ImGUITable.Aquire())
            {
                if (!table.BeginTable($"{idPrefix}_flow", tilesPerRow, tableFlags, 0f, 0f))
                    return;

                for (int i = 0; i < items.Count; i++)
                {
                    if (i > 0 && i % tilesPerRow == 0)
                        imgui_TableNextRow();

                    imgui_TableNextColumn();

                    var item = items[i];
                    string tileId = $"##{idPrefix}_{item.SlotId}_{item.SlotId2}_{i}";
                    bool searchMatch = MatchesSearch(item, _searchText?.ToLowerInvariant() ?? string.Empty);

                    bool clicked = imgui_InventorySlotTile(tileId, item.Name, item.Icon, TileSize, TileSize, searchMatch);

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

        private class PeerInventorySummary
        {
            public string Name { get; set; }
            public long LastUpdate { get; set; }
            public List<InventoryItem> Items { get; set; }
        }
    }
}
