using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections.Concurrent;
using System.IO;
using System.Text;
using IniParser.Model;
using E3Core.Processors;
using NetMQ;
using NetMQ.Sockets;
using Google.Protobuf;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Globalization;
using E3Core.Utility;
using E3Core.Data;

namespace MonoCore
{
    // /e3imgui UI extracted into dedicated partial class file
    public static  class E3ImGUI
    {
        // Theme system with multiple themes
        public enum UITheme
        {
            DarkTeal,      // Original E3 theme
            DarkBlue,      // Blue accent variant
            DarkPurple,    // Purple accent variant
            DarkOrange,    // Orange accent variant
            DarkGreen      // Green accent variant
        }
		public enum ImGuiWindowFlags
		{
			ImGuiWindowFlags_None = 0,
			ImGuiWindowFlags_NoTitleBar = 1 << 0,   // Disable title-bar
			ImGuiWindowFlags_NoResize = 1 << 1,   // Disable user resizing with the lower-right grip
			ImGuiWindowFlags_NoMove = 1 << 2,   // Disable user moving the window
			ImGuiWindowFlags_NoScrollbar = 1 << 3,   // Disable scrollbars (window can still scroll with mouse or programmatically)
			ImGuiWindowFlags_NoScrollWithMouse = 1 << 4,   // Disable user vertically scrolling with mouse wheel. On child window, mouse wheel will be forwarded to the parent unless NoScrollbar is also set.
			ImGuiWindowFlags_NoCollapse = 1 << 5,   // Disable user collapsing window by double-clicking on it. Also referred to as "window menu button" within a docking node.
			ImGuiWindowFlags_AlwaysAutoResize = 1 << 6,   // Resize every window to its content every frame
			ImGuiWindowFlags_NoBackground = 1 << 7,   // Disable drawing background color (WindowBg, etc.) and outside border. Similar as using SetNextWindowBgAlpha(0.0f).
			ImGuiWindowFlags_NoSavedSettings = 1 << 8,   // Never load/save settings in .ini file
			ImGuiWindowFlags_NoMouseInputs = 1 << 9,   // Disable catching mouse, hovering test with pass through.
			ImGuiWindowFlags_MenuBar = 1 << 10,  // Has a menu-bar
			ImGuiWindowFlags_HorizontalScrollbar = 1 << 11,  // Allow horizontal scrollbar to appear (off by default). You may use SetNextWindowContentSize(ImVec2(width,0.0f)); prior to calling Begin() to specify width. Read code in imgui_demo in the "Horizontal Scrolling" section.
			ImGuiWindowFlags_NoFocusOnAppearing = 1 << 12,  // Disable taking focus when transitioning from hidden to visible state
			ImGuiWindowFlags_NoBringToFrontOnFocus = 1 << 13,  // Disable bringing window to front when taking focus (e.g. clicking on it or programmatically giving it focus)
			ImGuiWindowFlags_AlwaysVerticalScrollbar = 1 << 14,  // Always show vertical scrollbar (even if ContentSize.y < Size.y)
			ImGuiWindowFlags_AlwaysHorizontalScrollbar = 1 << 15,  // Always show horizontal scrollbar (even if ContentSize.x < Size.x)
			ImGuiWindowFlags_AlwaysUseWindowPadding = 1 << 16,  // Ensure child windows without border uses style.WindowPadding (ignored by default for non-bordered child windows, because more convenient)
			ImGuiWindowFlags_NoNavInputs = 1 << 18,  // No gamepad/keyboard navigation within the window
			ImGuiWindowFlags_NoNavFocus = 1 << 19,  // No focusing toward this window with gamepad/keyboard navigation (e.g. skipped by CTRL+TAB)
			ImGuiWindowFlags_UnsavedDocument = 1 << 20,  // Display a dot next to the title. When used in a tab/docking context, tab is selected when clicking the X + closure is not assumed (will wait for user to stop submitting the tab). Otherwise closure is assumed when pressing the X, so if you keep submitting the tab may reappear at end of tab bar.
			ImGuiWindowFlags_NoDocking = 1 << 21,  // Disable docking of this window

			ImGuiWindowFlags_NoNav = ImGuiWindowFlags_NoNavInputs | ImGuiWindowFlags_NoNavFocus,
			ImGuiWindowFlags_NoDecoration = ImGuiWindowFlags_NoTitleBar | ImGuiWindowFlags_NoResize | ImGuiWindowFlags_NoScrollbar | ImGuiWindowFlags_NoCollapse,
			ImGuiWindowFlags_NoInputs = ImGuiWindowFlags_NoMouseInputs | ImGuiWindowFlags_NoNavInputs | ImGuiWindowFlags_NoNavFocus,

			// [Internal]
			ImGuiWindowFlags_NavFlattened = 1 << 23,  // [BETA] Allow gamepad/keyboard navigation to cross over parent border to this child (only use on child that have no scrolling!)
			ImGuiWindowFlags_ChildWindow = 1 << 24,  // Don't use! For internal use by BeginChild()
			ImGuiWindowFlags_Tooltip = 1 << 25,  // Don't use! For internal use by BeginTooltip()
			ImGuiWindowFlags_Popup = 1 << 26,  // Don't use! For internal use by BeginPopup()
			ImGuiWindowFlags_Modal = 1 << 27,  // Don't use! For internal use by BeginPopupModal()
			ImGuiWindowFlags_ChildMenu = 1 << 28,  // Don't use! For internal use by BeginMenu()
			ImGuiWindowFlags_DockNodeHost = 1 << 29   // Don't use! For internal use by Begin()/NewFrame()

			// [Obsolete]
			//ImGuiWindowFlags_ResizeFromAnySide    = 1 << 17,  // --> Set io.ConfigWindowsResizeFromEdges=true and make sure mouse cursors are supported by backend (io.BackendFlags & ImGuiBackendFlags_HasMouseCursors)
		}

		public enum ImGuiStyleVar
		{
			Alpha,
			DisabledAlpha,
			WindowPadding,          // ImVec2
			WindowRounding,         // float
			WindowBorderSize,       // float
			WindowMinSize,          // ImVec2
			WindowTitleAlign,       // ImVec2
			ChildRounding,          // float
			ChildBorderSize,        // float
			PopupRounding,          // float
			PopupBorderSize,        // float
			FramePadding,           // ImVec2
			FrameRounding,          // float
			FrameBorderSize,        // float
			ItemSpacing,            // ImVec2
			ItemInnerSpacing,       // ImVec2
			IndentSpacing,          // float
			CellPadding,            // ImVec2
			ScrollbarSize,          // float
			ScrollbarRounding,      // float
			GrabMinSize,            // float
			GrabRounding,           // float
			TabRounding,            // float
			ButtonTextAlign,        // ImVec2
			SelectableTextAlign     // ImVec2
		}

		public enum ImGuiCol
		{
			Text,
			TextDisabled,
			WindowBg,
			ChildBg,
			PopupBg,
			Border,
			BorderShadow,
			FrameBg,
			FrameBgHovered,
			FrameBgActive,
			TitleBg,
			TitleBgActive,
			TitleBgCollapsed,
			MenuBarBg,
			ScrollbarBg,
			ScrollbarGrab,
			ScrollbarGrabHovered,
			ScrollbarGrabActive,
			CheckMark,
			SliderGrab,
			SliderGrabActive,
			Button,
			ButtonHovered,
			ButtonActive,
			Header,
			HeaderHovered,
			HeaderActive,
			Separator,
			SeparatorHovered,
			SeparatorActive,
			ResizeGrip,
			ResizeGripHovered,
			ResizeGripActive,
			Tab,
			TabHovered,
			TabActive,
			TabUnfocused,
			TabUnfocusedActive,
			PlotLines,
			PlotLinesHovered,
			PlotHistogram,
			PlotHistogramHovered,
			TextSelectedBg,
			DragDropTarget,
			NavHighlight,
			NavWindowingHighlight,
			NavWindowingDimBg,
			ModalWindowDimBg,
			COUNT
		}

		public enum ImGuiTreeNodeFlags
		{
			ImGuiTreeNodeFlags_None = 0,
			ImGuiTreeNodeFlags_Selected = 1 << 0,   // Draw as selected
			ImGuiTreeNodeFlags_Framed = 1 << 1,   // Draw frame with background (e.g. for CollapsingHeader)
			ImGuiTreeNodeFlags_AllowItemOverlap = 1 << 2,   // Hit testing to allow subsequent widgets to overlap this one
			ImGuiTreeNodeFlags_NoTreePushOnOpen = 1 << 3,   // Don't do a TreePush() when open (e.g. for CollapsingHeader) = no extra indent nor pushing on ID stack
			ImGuiTreeNodeFlags_NoAutoOpenOnLog = 1 << 4,   // Don't automatically and temporarily open node when Logging is active (by default logging will automatically open tree nodes)
			ImGuiTreeNodeFlags_DefaultOpen = 1 << 5,   // Default node to be open
			ImGuiTreeNodeFlags_OpenOnDoubleClick = 1 << 6,   // Need double-click to open node
			ImGuiTreeNodeFlags_OpenOnArrow = 1 << 7,   // Only open when clicking on the arrow part. If ImGuiTreeNodeFlags_OpenOnDoubleClick is also set, single-click arrow or double-click all box to open.
			ImGuiTreeNodeFlags_Leaf = 1 << 8,   // No collapsing, no arrow (use as a convenience for leaf nodes)
			ImGuiTreeNodeFlags_Bullet = 1 << 9,   // Display a bullet instead of arrow
			ImGuiTreeNodeFlags_FramePadding = 1 << 10,  // Use FramePadding (even for an unframed text node) to vertically align text baseline to regular widget height. Equivalent to calling AlignTextToFramePadding().
			ImGuiTreeNodeFlags_SpanAvailWidth = 1 << 11,  // Extend hit box to the right-most edge, even if not framed. This is not the default in order to allow adding other items on the same line. In the future we may refactor the hit system to be front-to-back, allowing natural overlaps and then this can become the default.
			ImGuiTreeNodeFlags_SpanFullWidth = 1 << 12,  // Extend hit box to the left-most and right-most edges (bypass the indented area).
			ImGuiTreeNodeFlags_NavLeftJumpsBackHere = 1 << 13  // (WIP) Nav: left direction may move to this TreeNode() from any of its child (items submitted between TreeNode and TreePop)
		}

		public enum ImGuiTableFlags
		{
			ImGuiTableFlags_None = 0,
			ImGuiTableFlags_Resizable = 1 << 0,   // Enable resizing columns.
			ImGuiTableFlags_Reorderable = 1 << 1,   // Enable reordering columns in header row (need calling TableSetupColumn() + TableHeadersRow() to display headers)
			ImGuiTableFlags_Hideable = 1 << 2,   // Enable hiding/disabling columns in context menu.
			ImGuiTableFlags_Sortable = 1 << 3,   // Enable sorting. Call TableGetSortSpecs() to obtain sort specs. Also see ImGuiTableFlags_SortMulti and ImGuiTableFlags_SortTristate.
			ImGuiTableFlags_NoSavedSettings = 1 << 4,   // Disable persisting columns order, width and sort settings in the .ini file.
			ImGuiTableFlags_ContextMenuInBody = 1 << 5,   // Right-click on columns body/contents will display table context menu. By default it is available in TableHeadersRow().
														  // Decorations
			ImGuiTableFlags_RowBg = 1 << 6,   // Set each RowBg color with ImGuiCol_TableRowBg or ImGuiCol_TableRowBgAlt (equivalent of calling TableSetBgColor with ImGuiTableBgFlags_RowBg0 on each row)
			ImGuiTableFlags_BordersInnerH = 1 << 7,   // Draw horizontal borders between rows.
			ImGuiTableFlags_BordersOuterH = 1 << 8,   // Draw horizontal borders at the top and bottom.
			ImGuiTableFlags_BordersInnerV = 1 << 9,   // Draw vertical borders between columns.
			ImGuiTableFlags_BordersOuterV = 1 << 10,  // Draw vertical borders on the left and right sides.
			ImGuiTableFlags_BordersH = ImGuiTableFlags_BordersInnerH | ImGuiTableFlags_BordersOuterH, // Draw horizontal borders.
			ImGuiTableFlags_BordersV = ImGuiTableFlags_BordersInnerV | ImGuiTableFlags_BordersOuterV, // Draw vertical borders.
			ImGuiTableFlags_BordersInner = ImGuiTableFlags_BordersInnerV | ImGuiTableFlags_BordersInnerH, // Draw inner borders.
			ImGuiTableFlags_BordersOuter = ImGuiTableFlags_BordersOuterV | ImGuiTableFlags_BordersOuterH, // Draw outer borders.
			ImGuiTableFlags_Borders = ImGuiTableFlags_BordersInner | ImGuiTableFlags_BordersOuter,   // Draw all borders.
			ImGuiTableFlags_NoBordersInBody = 1 << 11,  // [ALPHA] Disable vertical borders in columns Body (borders will always appear in Headers). -> May move to style
			ImGuiTableFlags_NoBordersInBodyUntilResize = 1 << 12,  // [ALPHA] Disable vertical borders in columns Body until hovered for resize (borders will always appear in Headers). -> May move to style
																   // Sizing Policy (read above for defaults)
			ImGuiTableFlags_SizingFixedFit = 1 << 13,  // Columns default to _WidthFixed or _WidthAuto (if resizable or not resizable), matching contents width.
			ImGuiTableFlags_SizingFixedSame = 2 << 13,  // Columns default to _WidthFixed or _WidthAuto (if resizable or not resizable), matching the maximum contents width of all columns. Implicitly enable ImGuiTableFlags_NoKeepColumnsVisible.
			ImGuiTableFlags_SizingStretchProp = 3 << 13,  // Columns default to _WidthStretch with default weights proportional to each columns contents widths.
			ImGuiTableFlags_SizingStretchSame = 4 << 13,  // Columns default to _WidthStretch with default weights all equal, unless overridden by TableSetupColumn().
														  // Sizing Extra Options
			ImGuiTableFlags_NoHostExtendX = 1 << 16,  // Make outer width auto-fit to columns, overriding outer_size.x value. Only available when ScrollX/ScrollY are disabled and Stretch columns are not used.
			ImGuiTableFlags_NoHostExtendY = 1 << 17,  // Make outer height stop exactly at outer_size.y (prevent auto-extending table past the limit). Only available when ScrollX/ScrollY are disabled. Data below the limit will be clipped and not visible.
			ImGuiTableFlags_NoKeepColumnsVisible = 1 << 18,  // Disable keeping column always minimally visible when ScrollX is off and table gets too small. Not recommended if columns are resizable.
			ImGuiTableFlags_PreciseWidths = 1 << 19,  // Disable distributing remainder width to stretched columns (width allocation on a 100-wide table with 3 columns: Without this flag: 33,33,34. With this flag: 33,33,33). With larger number of columns, resizing will appear to be less smooth.
													  // Clipping
			ImGuiTableFlags_NoClip = 1 << 20,  // Disable clipping rectangle for every individual columns (reduce draw command count, items will be able to overflow into other columns). Generally incompatible with TableSetupScrollFreeze().
											   // Padding
			ImGuiTableFlags_PadOuterX = 1 << 21,  // Default if BordersOuterV is on. Enable outer-most padding. Generally desirable if you have headers.
			ImGuiTableFlags_NoPadOuterX = 1 << 22,  // Default if BordersOuterV is off. Disable outer-most padding.
			ImGuiTableFlags_NoPadInnerX = 1 << 23,  // Disable inner padding between columns (double inner padding if BordersOuterV is on, single inner padding if BordersOuterV is off).
													// Scrolling
			ImGuiTableFlags_ScrollX = 1 << 24,  // Enable horizontal scrolling. Require 'outer_size' parameter of BeginTable() to specify the container size. Changes default sizing policy. Because this creates a child window, ScrollY is currently generally recommended when using ScrollX.
			ImGuiTableFlags_ScrollY = 1 << 25,  // Enable vertical scrolling. Require 'outer_size' parameter of BeginTable() to specify the container size.
												// Sorting
			ImGuiTableFlags_SortMulti = 1 << 26,  // Hold shift when clicking headers to sort on multiple column. TableGetSortSpecs() may return specs where (SpecsCount > 1).
			ImGuiTableFlags_SortTristate = 1 << 27,  // Allow no sorting, disable default sorting. TableGetSortSpecs() may return specs where (SpecsCount == 0).
		}

		public enum ImGuiTableColumnFlags
		{
			ImGuiTableColumnFlags_None = 0,
			ImGuiTableColumnFlags_Disabled = 1 << 0,   // Overriding/master disable flag: hide column, won't show in context menu (unlike calling TableSetColumnEnabled() which manipulates the user accessible state)
			ImGuiTableColumnFlags_DefaultHide = 1 << 1,   // Default as a hidden/disabled column.
			ImGuiTableColumnFlags_DefaultSort = 1 << 2,   // Default as a sorting column.
			ImGuiTableColumnFlags_WidthStretch = 1 << 3,   // Column will stretch. Preferable with horizontal scrolling disabled (default if table sizing policy is _SizingStretchSame or _SizingStretchProp).
			ImGuiTableColumnFlags_WidthFixed = 1 << 4,   // Column will not stretch. Preferable with horizontal scrolling enabled (default if table sizing policy is _SizingFixedFit and table is resizable).
			ImGuiTableColumnFlags_NoResize = 1 << 5,   // Disable manual resizing.
			ImGuiTableColumnFlags_NoReorder = 1 << 6,   // Disable manual reordering this column, this will also prevent other columns from crossing over this column.
			ImGuiTableColumnFlags_NoHide = 1 << 7,   // Disable ability to hide/disable this column.
			ImGuiTableColumnFlags_NoClip = 1 << 8,   // Disable clipping for this column (all NoClip columns will render in a same draw command).
			ImGuiTableColumnFlags_NoSort = 1 << 9,   // Disable ability to sort on this field (even if ImGuiTableFlags_Sortable is set on the table).
			ImGuiTableColumnFlags_NoSortAscending = 1 << 10,  // Disable ability to sort in the ascending direction.
			ImGuiTableColumnFlags_NoSortDescending = 1 << 11,  // Disable ability to sort in the descending direction.
			ImGuiTableColumnFlags_NoHeaderLabel = 1 << 12,  // TableHeadersRow() will not submit label for this column. Convenient for some small columns. Name will still appear in context menu.
			ImGuiTableColumnFlags_NoHeaderWidth = 1 << 13,  // Disable header text width contribution to automatic column width.
			ImGuiTableColumnFlags_PreferSortAscending = 1 << 14,  // Make the initial sort direction Ascending when first sorting on this column (default).
			ImGuiTableColumnFlags_PreferSortDescending = 1 << 15,  // Make the initial sort direction Descending when first sorting on this column.
			ImGuiTableColumnFlags_IndentEnable = 1 << 16,  // Use current Indent value when entering cell (default for column 0).
			ImGuiTableColumnFlags_IndentDisable = 1 << 17,  // Ignore current Indent value when entering cell (default for columns > 0). Indentation changes _within_ the cell will still be honored.
		}
		public static UITheme _currentTheme = UITheme.DarkTeal;

        private static readonly int _themePushCount = 27;
		// Rounding settings
		public static float _rounding = 6.0f;
        public static string _roundingBuf = string.Empty; // UI buffer for editing rounding
		public static int _roundingVersion = 0; // bump to force InputText to refresh its content
		public static readonly int _roundingPushCount = 7; // Window, Child, Popup, Frame, Grab, Tab, Scrollbar



		#region IMGUI
		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern static bool imgui_Begin(string name, int flags);
		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern static void imgui_Begin_OpenFlagSet(string name, bool value);
		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern static bool imgui_Begin_OpenFlagGet(string name);
		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern static bool imgui_Button(string name);
		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern static bool imgui_ButtonEx(string name, float width, float height);
		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern static void imgui_Text(string text);
		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern static void imgui_Separator();
		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern static void imgui_SameLine();
		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern static void imgui_SameLineEx(float offsetFromStartX, float spacing);
		public static void imgui_SameLine(float offsetFromStartX)
	=> imgui_SameLineEx(offsetFromStartX, -1f);

		public static void imgui_SameLine(float offsetFromStartX, float spacing)
			=> imgui_SameLineEx(offsetFromStartX, spacing);

		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern static bool imgui_Checkbox(string name, bool defaultValue);
		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern static bool imgui_BeginTabBar(string name);
		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern static void imgui_EndTabBar();
		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern static bool imgui_BeginTabItem(string label);
		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern static void imgui_EndTabItem();
		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern static bool imgui_BeginChild(string id, float width, float height, bool border);
		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern static void imgui_EndChild();
		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern static bool imgui_Selectable(string label, bool selected);
		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern static float imgui_GetContentRegionAvailX();
		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern static float imgui_GetContentRegionAvailY();
		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern static void imgui_SetNextItemWidth(float width);
		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern static bool imgui_BeginCombo(string label, string preview, int flags);
		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern static void imgui_EndCombo();
		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern static bool imgui_RightAlignButton(string name);
		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern static bool imgui_InputText(string id, string initial);
		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern static string imgui_InputText_Get(string id);
		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern static void imgui_End();
		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern static bool imgui_BeginPopupContextItem(string id, int flags);
		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern static bool imgui_BeginPopupContextWindow(string id, int flags);
		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern static void imgui_EndPopup();
		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern static bool imgui_MenuItem(string label);
		// Tables
		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern static bool imgui_BeginTable(string id, int columns, int flags, float outerWidth);
		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern static void imgui_EndTable();
		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern static void imgui_TableSetupColumn(string label, int flags, float initWidth);
		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern static void imgui_TableHeadersRow();
		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern static void imgui_TableNextRow();
		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern static bool imgui_TableNextColumn();
		// Colors / styled text
		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern static void imgui_TextColored(float r, float g, float b, float a, string text);
		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern static void imgui_PushStyleColor(int which, float r, float g, float b, float a);
		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern static void imgui_PopStyleColor(int count);
		// Style vars (rounding, padding, etc.)
		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern static void imgui_PushStyleVarFloat(int which, float value);
		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern static void imgui_PushStyleVarVec2(int which, float x, float y);
		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern static void imgui_PopStyleVar(int count);
		// Tree nodes
		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern static bool imgui_TreeNode(string label);
		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern static bool imgui_TreeNodeEx(string label, int flags);
		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern static void imgui_TreePop();
		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern static bool imgui_CollapsingHeader(string label, int flags);
		// Tooltips and hover detection
		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern static bool imgui_IsItemHovered();
		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern static void imgui_BeginTooltip();
		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern static void imgui_EndTooltip();
		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern static void imgui_TextWrapped(string text);
		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern static void imgui_PushTextWrapPos(float wrapLocalPosX);
		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern static void imgui_PopTextWrapPos();
		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern static bool imgui_IsMouseClicked(int button);
		// Image display
		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern static void imgui_Image(IntPtr textureId, float width, float height);
		// Native spell icon drawing
		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern static void imgui_DrawSpellIconByIconIndex(int iconIndex, float size);
		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern static void imgui_DrawSpellIconBySpellID(int spellId, float size);
		// Drawing functions for custom backgrounds
		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern static float imgui_GetCursorPosY();
		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern static float imgui_GetCursorScreenPosX();
		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern static float imgui_GetCursorScreenPosY();
		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern static float imgui_GetTextLineHeightWithSpacing();
		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern static float imgui_GetFrameHeight();
		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern static void imgui_GetWindowDrawList_AddRectFilled(float x1, float y1, float x2, float y2, uint color);
		// Item rect + color helpers
		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern static float imgui_GetItemRectMinX();
		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern static float imgui_GetItemRectMinY();
		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern static float imgui_GetItemRectMaxX();
		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern static float imgui_GetItemRectMaxY();
		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern static uint imgui_GetColorU32(int imguiCol, float alphaMul);
		// Texture creation from raw data
		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern static IntPtr mq_CreateTextureFromData(byte[] data, int width, int height, int channels);
		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern static void mq_DestroyTexture(IntPtr textureId);
		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern static void imgui_SetNextWindowSize(float width, float height);

		#endregion

		private static void PushCommonRounding()
        {
            // Apply consistent rounding across key style vars
            imgui_PushStyleVarFloat((int)ImGuiStyleVar.WindowRounding, _rounding);
            imgui_PushStyleVarFloat((int)ImGuiStyleVar.ChildRounding, _rounding);
            imgui_PushStyleVarFloat((int)ImGuiStyleVar.PopupRounding, _rounding);
            imgui_PushStyleVarFloat((int)ImGuiStyleVar.FrameRounding, _rounding);
            imgui_PushStyleVarFloat((int)ImGuiStyleVar.GrabRounding, Math.Max(3.0f, _rounding - 2.0f));
            imgui_PushStyleVarFloat((int)ImGuiStyleVar.TabRounding, _rounding);
            imgui_PushStyleVarFloat((int)ImGuiStyleVar.ScrollbarRounding, _rounding);
        }
        
        public static void PushCurrentTheme()
        {
            // Always push rounding first so it applies consistently regardless of selected theme
            PushCommonRounding();
            switch (_currentTheme)
            {
                case UITheme.DarkTeal:
                    PushDarkTealTheme();
                    break;
                case UITheme.DarkBlue:
                    PushDarkBlueTheme();
                    break;
                case UITheme.DarkPurple:
                    PushDarkPurpleTheme();
                    break;
                case UITheme.DarkOrange:
                    PushDarkOrangeTheme();
                    break;
                case UITheme.DarkGreen:
                    PushDarkGreenTheme();
                    break;
                default:
                    PushDarkTealTheme();
                    break;
            }
        }
        
        private static void PushDarkTealTheme()
        {
            // Backgrounds
            imgui_PushStyleColor((int)ImGuiCol.WindowBg, 0.13f, 0.13f, 0.14f, 1.0f);
            imgui_PushStyleColor((int)ImGuiCol.ChildBg, 0.11f, 0.11f, 0.12f, 1.0f);
            // Frames
            imgui_PushStyleColor((int)ImGuiCol.FrameBg, 0.17f, 0.18f, 0.20f, 1.0f);
            imgui_PushStyleColor((int)ImGuiCol.FrameBgHovered, 0.20f, 0.21f, 0.23f, 1.0f);
            imgui_PushStyleColor((int)ImGuiCol.FrameBgActive, 0.19f, 0.20f, 0.22f, 1.0f);
            // Buttons (teal accent)
            imgui_PushStyleColor((int)ImGuiCol.Button, 0.13f, 0.55f, 0.53f, 1.0f);
            imgui_PushStyleColor((int)ImGuiCol.ButtonHovered, 0.17f, 0.66f, 0.64f, 1.0f);
            imgui_PushStyleColor((int)ImGuiCol.ButtonActive, 0.12f, 0.48f, 0.47f, 1.0f);
            // Headers (used by tree nodes, selectable headers)
            imgui_PushStyleColor((int)ImGuiCol.Header, 0.12f, 0.50f, 0.49f, 0.55f);
            imgui_PushStyleColor((int)ImGuiCol.HeaderHovered, 0.16f, 0.62f, 0.60f, 0.80f);
            imgui_PushStyleColor((int)ImGuiCol.HeaderActive, 0.12f, 0.50f, 0.49f, 1.00f);
            // Tabs
            imgui_PushStyleColor((int)ImGuiCol.Tab, 0.11f, 0.48f, 0.46f, 1.0f);
            imgui_PushStyleColor((int)ImGuiCol.TabHovered, 0.16f, 0.62f, 0.60f, 1.0f);
            imgui_PushStyleColor((int)ImGuiCol.TabActive, 0.13f, 0.55f, 0.53f, 1.0f);
            imgui_PushStyleColor((int)ImGuiCol.TabUnfocused, 0.09f, 0.09f, 0.10f, 1.0f);
            imgui_PushStyleColor((int)ImGuiCol.TabUnfocusedActive, 0.11f, 0.11f, 0.12f, 1.0f);
            // Sliders / checks
            imgui_PushStyleColor((int)ImGuiCol.SliderGrab, 0.29f, 0.79f, 0.76f, 1.0f);
            imgui_PushStyleColor((int)ImGuiCol.SliderGrabActive, 0.36f, 0.86f, 0.80f, 1.0f);
            imgui_PushStyleColor((int)ImGuiCol.CheckMark, 0.36f, 0.86f, 0.80f, 1.0f);
            // Titles
            imgui_PushStyleColor((int)ImGuiCol.TitleBg, 0.10f, 0.10f, 0.11f, 1.0f);
            imgui_PushStyleColor((int)ImGuiCol.TitleBgActive, 0.12f, 0.12f, 0.14f, 1.0f);
            // Separators
            imgui_PushStyleColor((int)ImGuiCol.Separator, 0.25f, 0.27f, 0.30f, 1.0f);
            imgui_PushStyleColor((int)ImGuiCol.SeparatorHovered, 0.30f, 0.33f, 0.36f, 1.0f);
            imgui_PushStyleColor((int)ImGuiCol.SeparatorActive, 0.21f, 0.60f, 0.60f, 1.0f);
            // Scrollbars
            imgui_PushStyleColor((int)ImGuiCol.ScrollbarGrab, 0.28f, 0.30f, 0.32f, 1.0f);
            imgui_PushStyleColor((int)ImGuiCol.ScrollbarGrabHovered, 0.32f, 0.34f, 0.36f, 1.0f);
            imgui_PushStyleColor((int)ImGuiCol.ScrollbarGrabActive, 0.36f, 0.38f, 0.40f, 1.0f);
        }
        
        private static void PushDarkBlueTheme()
        {
            // Backgrounds
            imgui_PushStyleColor((int)ImGuiCol.WindowBg, 0.13f, 0.13f, 0.16f, 1.0f);
            imgui_PushStyleColor((int)ImGuiCol.ChildBg, 0.11f, 0.11f, 0.14f, 1.0f);
            // Frames
            imgui_PushStyleColor((int)ImGuiCol.FrameBg, 0.17f, 0.18f, 0.22f, 1.0f);
            imgui_PushStyleColor((int)ImGuiCol.FrameBgHovered, 0.20f, 0.21f, 0.26f, 1.0f);
            imgui_PushStyleColor((int)ImGuiCol.FrameBgActive, 0.19f, 0.20f, 0.24f, 1.0f);
            // Buttons (blue accent)
            imgui_PushStyleColor((int)ImGuiCol.Button, 0.26f, 0.39f, 0.98f, 1.0f);
            imgui_PushStyleColor((int)ImGuiCol.ButtonHovered, 0.32f, 0.45f, 1.0f, 1.0f);
            imgui_PushStyleColor((int)ImGuiCol.ButtonActive, 0.22f, 0.35f, 0.85f, 1.0f);
            // Headers
            imgui_PushStyleColor((int)ImGuiCol.Header, 0.26f, 0.39f, 0.98f, 0.55f);
            imgui_PushStyleColor((int)ImGuiCol.HeaderHovered, 0.32f, 0.45f, 1.0f, 0.80f);
            imgui_PushStyleColor((int)ImGuiCol.HeaderActive, 0.26f, 0.39f, 0.98f, 1.00f);
            // Tabs
            imgui_PushStyleColor((int)ImGuiCol.Tab, 0.22f, 0.35f, 0.85f, 1.0f);
            imgui_PushStyleColor((int)ImGuiCol.TabHovered, 0.32f, 0.45f, 1.0f, 1.0f);
            imgui_PushStyleColor((int)ImGuiCol.TabActive, 0.26f, 0.39f, 0.98f, 1.0f);
            imgui_PushStyleColor((int)ImGuiCol.TabUnfocused, 0.09f, 0.09f, 0.12f, 1.0f);
            imgui_PushStyleColor((int)ImGuiCol.TabUnfocusedActive, 0.11f, 0.11f, 0.14f, 1.0f);
            // Sliders / checks
            imgui_PushStyleColor((int)ImGuiCol.SliderGrab, 0.32f, 0.45f, 1.0f, 1.0f);
            imgui_PushStyleColor((int)ImGuiCol.SliderGrabActive, 0.38f, 0.51f, 1.0f, 1.0f);
            imgui_PushStyleColor((int)ImGuiCol.CheckMark, 0.38f, 0.51f, 1.0f, 1.0f);
            // Titles
            imgui_PushStyleColor((int)ImGuiCol.TitleBg, 0.10f, 0.10f, 0.13f, 1.0f);
            imgui_PushStyleColor((int)ImGuiCol.TitleBgActive, 0.12f, 0.12f, 0.16f, 1.0f);
            // Separators
            imgui_PushStyleColor((int)ImGuiCol.Separator, 0.25f, 0.27f, 0.32f, 1.0f);
            imgui_PushStyleColor((int)ImGuiCol.SeparatorHovered, 0.30f, 0.33f, 0.38f, 1.0f);
            imgui_PushStyleColor((int)ImGuiCol.SeparatorActive, 0.26f, 0.39f, 0.98f, 1.0f);
            // Scrollbars
            imgui_PushStyleColor((int)ImGuiCol.ScrollbarGrab, 0.28f, 0.30f, 0.34f, 1.0f);
            imgui_PushStyleColor((int)ImGuiCol.ScrollbarGrabHovered, 0.32f, 0.34f, 0.38f, 1.0f);
            imgui_PushStyleColor((int)ImGuiCol.ScrollbarGrabActive, 0.36f, 0.38f, 0.42f, 1.0f);
        }
        
        private static void PushDarkPurpleTheme()
        {
            // Backgrounds
            imgui_PushStyleColor((int)ImGuiCol.WindowBg, 0.15f, 0.12f, 0.16f, 1.0f);
            imgui_PushStyleColor((int)ImGuiCol.ChildBg, 0.13f, 0.10f, 0.14f, 1.0f);
            // Frames
            imgui_PushStyleColor((int)ImGuiCol.FrameBg, 0.19f, 0.16f, 0.22f, 1.0f);
            imgui_PushStyleColor((int)ImGuiCol.FrameBgHovered, 0.22f, 0.19f, 0.26f, 1.0f);
            imgui_PushStyleColor((int)ImGuiCol.FrameBgActive, 0.21f, 0.18f, 0.24f, 1.0f);
            // Buttons (purple accent)
            imgui_PushStyleColor((int)ImGuiCol.Button, 0.68f, 0.26f, 0.78f, 1.0f);
            imgui_PushStyleColor((int)ImGuiCol.ButtonHovered, 0.78f, 0.32f, 0.88f, 1.0f);
            imgui_PushStyleColor((int)ImGuiCol.ButtonActive, 0.58f, 0.22f, 0.68f, 1.0f);
            // Headers
            imgui_PushStyleColor((int)ImGuiCol.Header, 0.68f, 0.26f, 0.78f, 0.55f);
            imgui_PushStyleColor((int)ImGuiCol.HeaderHovered, 0.78f, 0.32f, 0.88f, 0.80f);
            imgui_PushStyleColor((int)ImGuiCol.HeaderActive, 0.68f, 0.26f, 0.78f, 1.00f);
            // Tabs
            imgui_PushStyleColor((int)ImGuiCol.Tab, 0.58f, 0.22f, 0.68f, 1.0f);
            imgui_PushStyleColor((int)ImGuiCol.TabHovered, 0.78f, 0.32f, 0.88f, 1.0f);
            imgui_PushStyleColor((int)ImGuiCol.TabActive, 0.68f, 0.26f, 0.78f, 1.0f);
            imgui_PushStyleColor((int)ImGuiCol.TabUnfocused, 0.11f, 0.08f, 0.12f, 1.0f);
            imgui_PushStyleColor((int)ImGuiCol.TabUnfocusedActive, 0.13f, 0.10f, 0.14f, 1.0f);
            // Sliders / checks
            imgui_PushStyleColor((int)ImGuiCol.SliderGrab, 0.78f, 0.32f, 0.88f, 1.0f);
            imgui_PushStyleColor((int)ImGuiCol.SliderGrabActive, 0.88f, 0.42f, 0.98f, 1.0f);
            imgui_PushStyleColor((int)ImGuiCol.CheckMark, 0.88f, 0.42f, 0.98f, 1.0f);
            // Titles
            imgui_PushStyleColor((int)ImGuiCol.TitleBg, 0.12f, 0.09f, 0.13f, 1.0f);
            imgui_PushStyleColor((int)ImGuiCol.TitleBgActive, 0.14f, 0.11f, 0.16f, 1.0f);
            // Separators
            imgui_PushStyleColor((int)ImGuiCol.Separator, 0.27f, 0.24f, 0.32f, 1.0f);
            imgui_PushStyleColor((int)ImGuiCol.SeparatorHovered, 0.32f, 0.29f, 0.38f, 1.0f);
            imgui_PushStyleColor((int)ImGuiCol.SeparatorActive, 0.68f, 0.26f, 0.78f, 1.0f);
            // Scrollbars
            imgui_PushStyleColor((int)ImGuiCol.ScrollbarGrab, 0.30f, 0.27f, 0.34f, 1.0f);
            imgui_PushStyleColor((int)ImGuiCol.ScrollbarGrabHovered, 0.34f, 0.31f, 0.38f, 1.0f);
            imgui_PushStyleColor((int)ImGuiCol.ScrollbarGrabActive, 0.38f, 0.35f, 0.42f, 1.0f);
        }
        
        private static void PushDarkOrangeTheme()
        {
            // Backgrounds
            imgui_PushStyleColor((int)ImGuiCol.WindowBg, 0.16f, 0.13f, 0.12f, 1.0f);
            imgui_PushStyleColor((int)ImGuiCol.ChildBg, 0.14f, 0.11f, 0.10f, 1.0f);
            // Frames
            imgui_PushStyleColor((int)ImGuiCol.FrameBg, 0.22f, 0.18f, 0.16f, 1.0f);
            imgui_PushStyleColor((int)ImGuiCol.FrameBgHovered, 0.26f, 0.21f, 0.19f, 1.0f);
            imgui_PushStyleColor((int)ImGuiCol.FrameBgActive, 0.24f, 0.20f, 0.18f, 1.0f);
            // Buttons (orange accent)
            imgui_PushStyleColor((int)ImGuiCol.Button, 0.98f, 0.55f, 0.26f, 1.0f);
            imgui_PushStyleColor((int)ImGuiCol.ButtonHovered, 1.0f, 0.65f, 0.32f, 1.0f);
            imgui_PushStyleColor((int)ImGuiCol.ButtonActive, 0.85f, 0.48f, 0.22f, 1.0f);
            // Headers
            imgui_PushStyleColor((int)ImGuiCol.Header, 0.98f, 0.55f, 0.26f, 0.55f);
            imgui_PushStyleColor((int)ImGuiCol.HeaderHovered, 1.0f, 0.65f, 0.32f, 0.80f);
            imgui_PushStyleColor((int)ImGuiCol.HeaderActive, 0.98f, 0.55f, 0.26f, 1.00f);
            // Tabs
            imgui_PushStyleColor((int)ImGuiCol.Tab, 0.85f, 0.48f, 0.22f, 1.0f);
            imgui_PushStyleColor((int)ImGuiCol.TabHovered, 1.0f, 0.65f, 0.32f, 1.0f);
            imgui_PushStyleColor((int)ImGuiCol.TabActive, 0.98f, 0.55f, 0.26f, 1.0f);
            imgui_PushStyleColor((int)ImGuiCol.TabUnfocused, 0.12f, 0.09f, 0.08f, 1.0f);
            imgui_PushStyleColor((int)ImGuiCol.TabUnfocusedActive, 0.14f, 0.11f, 0.10f, 1.0f);
            // Sliders / checks
            imgui_PushStyleColor((int)ImGuiCol.SliderGrab, 1.0f, 0.65f, 0.32f, 1.0f);
            imgui_PushStyleColor((int)ImGuiCol.SliderGrabActive, 1.0f, 0.75f, 0.42f, 1.0f);
            imgui_PushStyleColor((int)ImGuiCol.CheckMark, 1.0f, 0.75f, 0.42f, 1.0f);
            // Titles
            imgui_PushStyleColor((int)ImGuiCol.TitleBg, 0.13f, 0.10f, 0.09f, 1.0f);
            imgui_PushStyleColor((int)ImGuiCol.TitleBgActive, 0.16f, 0.12f, 0.11f, 1.0f);
            // Separators
            imgui_PushStyleColor((int)ImGuiCol.Separator, 0.32f, 0.27f, 0.24f, 1.0f);
            imgui_PushStyleColor((int)ImGuiCol.SeparatorHovered, 0.38f, 0.33f, 0.30f, 1.0f);
            imgui_PushStyleColor((int)ImGuiCol.SeparatorActive, 0.98f, 0.55f, 0.26f, 1.0f);
            // Scrollbars
            imgui_PushStyleColor((int)ImGuiCol.ScrollbarGrab, 0.34f, 0.30f, 0.28f, 1.0f);
            imgui_PushStyleColor((int)ImGuiCol.ScrollbarGrabHovered, 0.38f, 0.34f, 0.32f, 1.0f);
            imgui_PushStyleColor((int)ImGuiCol.ScrollbarGrabActive, 0.42f, 0.38f, 0.36f, 1.0f);
        }
        
        private static void PushDarkGreenTheme()
        {
            // Backgrounds
            imgui_PushStyleColor((int)ImGuiCol.WindowBg, 0.12f, 0.16f, 0.13f, 1.0f);
            imgui_PushStyleColor((int)ImGuiCol.ChildBg, 0.10f, 0.14f, 0.11f, 1.0f);
            // Frames
            imgui_PushStyleColor((int)ImGuiCol.FrameBg, 0.16f, 0.22f, 0.18f, 1.0f);
            imgui_PushStyleColor((int)ImGuiCol.FrameBgHovered, 0.19f, 0.26f, 0.21f, 1.0f);
            imgui_PushStyleColor((int)ImGuiCol.FrameBgActive, 0.18f, 0.24f, 0.20f, 1.0f);
            // Buttons (green accent)
            imgui_PushStyleColor((int)ImGuiCol.Button, 0.26f, 0.78f, 0.39f, 1.0f);
            imgui_PushStyleColor((int)ImGuiCol.ButtonHovered, 0.32f, 0.88f, 0.45f, 1.0f);
            imgui_PushStyleColor((int)ImGuiCol.ButtonActive, 0.22f, 0.68f, 0.35f, 1.0f);
            // Headers
            imgui_PushStyleColor((int)ImGuiCol.Header, 0.26f, 0.78f, 0.39f, 0.55f);
            imgui_PushStyleColor((int)ImGuiCol.HeaderHovered, 0.32f, 0.88f, 0.45f, 0.80f);
            imgui_PushStyleColor((int)ImGuiCol.HeaderActive, 0.26f, 0.78f, 0.39f, 1.00f);
            // Tabs
            imgui_PushStyleColor((int)ImGuiCol.Tab, 0.22f, 0.68f, 0.35f, 1.0f);
            imgui_PushStyleColor((int)ImGuiCol.TabHovered, 0.32f, 0.88f, 0.45f, 1.0f);
            imgui_PushStyleColor((int)ImGuiCol.TabActive, 0.26f, 0.78f, 0.39f, 1.0f);
            imgui_PushStyleColor((int)ImGuiCol.TabUnfocused, 0.08f, 0.12f, 0.09f, 1.0f);
            imgui_PushStyleColor((int)ImGuiCol.TabUnfocusedActive, 0.10f, 0.14f, 0.11f, 1.0f);
            // Sliders / checks
            imgui_PushStyleColor((int)ImGuiCol.SliderGrab, 0.32f, 0.88f, 0.45f, 1.0f);
            imgui_PushStyleColor((int)ImGuiCol.SliderGrabActive, 0.42f, 0.98f, 0.55f, 1.0f);
            imgui_PushStyleColor((int)ImGuiCol.CheckMark, 0.42f, 0.98f, 0.55f, 1.0f);
            // Titles
            imgui_PushStyleColor((int)ImGuiCol.TitleBg, 0.09f, 0.13f, 0.10f, 1.0f);
            imgui_PushStyleColor((int)ImGuiCol.TitleBgActive, 0.11f, 0.16f, 0.12f, 1.0f);
            // Separators
            imgui_PushStyleColor((int)ImGuiCol.Separator, 0.24f, 0.32f, 0.27f, 1.0f);
            imgui_PushStyleColor((int)ImGuiCol.SeparatorHovered, 0.30f, 0.38f, 0.33f, 1.0f);
            imgui_PushStyleColor((int)ImGuiCol.SeparatorActive, 0.26f, 0.78f, 0.39f, 1.0f);
            // Scrollbars
            imgui_PushStyleColor((int)ImGuiCol.ScrollbarGrab, 0.27f, 0.34f, 0.30f, 1.0f);
            imgui_PushStyleColor((int)ImGuiCol.ScrollbarGrabHovered, 0.31f, 0.38f, 0.34f, 1.0f);
            imgui_PushStyleColor((int)ImGuiCol.ScrollbarGrabActive, 0.35f, 0.42f, 0.38f, 1.0f);
        }
        
        
        public static void PopCurrentTheme()
        {
            // Pop in reverse order: style vars then colors
            imgui_PopStyleVar(_roundingPushCount);
            imgui_PopStyleColor(_themePushCount);
        }

        
        public static float[] GetThemePreviewColor(UITheme theme)
        {
            switch (theme)
            {
                case UITheme.DarkTeal:
                    return new float[] { 0.13f, 0.55f, 0.53f, 1.0f };
                case UITheme.DarkBlue:
                    return new float[] { 0.26f, 0.39f, 0.98f, 1.0f };
                case UITheme.DarkPurple:
                    return new float[] { 0.68f, 0.26f, 0.78f, 1.0f };
                case UITheme.DarkOrange:
                    return new float[] { 0.98f, 0.55f, 0.26f, 1.0f };
                case UITheme.DarkGreen:
                    return new float[] { 0.26f, 0.78f, 0.39f, 1.0f };
                default:
                    return new float[] { 0.13f, 0.55f, 0.53f, 1.0f };
            }
        }
        
        public static string GetThemeDescription(UITheme theme)
        {
            switch (theme)
            {
                case UITheme.DarkTeal:
                    return "The original E3Next dark theme with teal accents. Professional and easy on the eyes for long sessions.";
                case UITheme.DarkBlue:
                    return "Dark theme with vibrant blue accents. Clean and modern appearance with good contrast.";
                case UITheme.DarkPurple:
                    return "Dark theme with purple accents. Unique and stylish with a mystical feel.";
                case UITheme.DarkOrange:
                    return "Dark theme with warm orange accents. Energetic and attention-grabbing design.";
                case UITheme.DarkGreen:
                    return "Dark theme with green accents. Natural and calming, easy on the eyes.";
                default:
                    return "Theme description not available.";
            }
        }
        /// <summary>
        /// Primary C++ entry point, calls the Invoke on all registered windows.
        /// </summary>
        public static void OnUpdateImGui()
        {
            foreach(var pair in RegisteredWindows)
            {
                pair.Value.Invoke();
            }

        }
        public static ConcurrentDictionary<string, Action> RegisteredWindows = new ConcurrentDictionary<string, Action>();

        //super simple registered method. no unregister, will add one if needed later.
        public static void RegisterWindow(string windowName, Action method, string description = "", [CallerMemberName] string memberName = "", [CallerFilePath] string fileName = "", [CallerLineNumber] int lineNumber = 0)
        {
            if (!RegisteredWindows.ContainsKey(windowName))
            {
                RegisteredWindows.TryAdd(windowName, method);
            }
        }
       


        #region Spell Icon Rendering
        
        /// <summary>
        /// Renders a spell icon with the specified size and optional tooltip
        /// </summary>
        /// <param name="spellId">The EQ spell ID</param>
        /// <param name="size">Size of the icon (both width and height)</param>
        /// <param name="showTooltip">Whether to show a tooltip on hover</param>
        /// <returns>True if the icon was clicked</returns>
        private static bool RenderSpellIcon(int spellId, float size = 40.0f, bool showTooltip = true)
        {
            if (spellId <= 0)
            {
                // Render a placeholder for invalid spell ID
                RenderSpellIconPlaceholder(size);
                return false;
            }

            // Draw natively by spell id
            imgui_DrawSpellIconBySpellID(spellId, size);

            bool clicked = imgui_IsItemHovered() && imgui_IsMouseClicked(0);

            if (showTooltip && imgui_IsItemHovered())
            {
                RenderSpellTooltip(spellId, GetSpellIconIndex(spellId));
            }

            return clicked;
        }

        /// <summary>
        /// Renders a spell icon by its icon index
        /// </summary>
        /// <param name="iconIndex">The spell icon index (0-based)</param>
        /// <param name="size">Size of the icon</param>
        /// <param name="showTooltip">Whether to show tooltip on hover</param>
        /// <param name="spellId">Optional spell ID for tooltip info</param>
        /// <returns>True if the icon was clicked</returns>
        public static bool RenderSpellIconByIndex(int iconIndex, float size = 40.0f, bool showTooltip = true, int spellId = 0)
        {
            try
            {
                if (!E3Next.UI.SpellIconManager.IsReady())
                {
                    // Try to initialize if not ready
                    InitializeSpellIcons();
                    if (!E3Next.UI.SpellIconManager.IsReady())
                    {
                        RenderSpellIconPlaceholder(size);
                        return false;
                    }
                }

                // Draw via native EQ texture animation wrapper
                imgui_DrawSpellIconByIconIndex(iconIndex, size);

                // Compute click based on hover state
                bool clicked = imgui_IsItemHovered() && imgui_IsMouseClicked(0);

                // Show tooltip on hover if enabled
                if (showTooltip && imgui_IsItemHovered())
                {
                    RenderSpellTooltip(spellId, iconIndex);
                }

                return clicked;
            }
            catch (Exception ex)
            {
                E3Core.Utility.e3util._log.Write($"Error rendering spell icon {iconIndex}: {ex.Message}");
                RenderSpellIconPlaceholder(size);
                return false;
            }
        }

        /// <summary>
        /// Renders a clickable image button
        /// </summary>
        private static bool RenderIconButton(IntPtr textureId, float size)
        {
            // Use the ImGui Image function to render the texture
            imgui_Image(textureId, size, size);
            
            // Check if the image was clicked
            return imgui_IsItemHovered() && imgui_IsMouseClicked(0); // Left mouse button
        }

        /// <summary>
        /// Renders a placeholder when spell icon is not available
        /// </summary>
        private static void RenderSpellIconPlaceholder(float size)
        {
            // Draw a simple colored rectangle as placeholder
            imgui_PushStyleColor((int)ImGuiCol.Button, 0.3f, 0.3f, 0.3f, 1.0f);
            imgui_ButtonEx("?", size, size);
            imgui_PopStyleColor(1);

            if (imgui_IsItemHovered())
            {
                imgui_BeginTooltip();
                imgui_TextWrapped("Spell icon not available");
                imgui_EndTooltip();
            }
        }

        /// <summary>
        /// Renders a tooltip with spell information
        /// </summary>
        private static void RenderSpellTooltip(int spellId, int iconIndex)
        {
            imgui_BeginTooltip();
            
            if (spellId > 0)
            {
                try
                {
                    // Get spell information from MQ
                    string spellName = Core.mq_ParseTLO($"${{Spell[{spellId}].Name}}");
                    string spellLevel = Core.mq_ParseTLO($"${{Spell[{spellId}].Level}}");
                    string spellDescription = Core.mq_ParseTLO($"${{Spell[{spellId}].Description}}");
                    
                    if (spellName != "NULL" && !string.IsNullOrEmpty(spellName))
                    {
                        imgui_TextColored(0.9f, 0.9f, 0.3f, 1.0f, spellName);
                        
                        if (spellLevel != "NULL" && !string.IsNullOrEmpty(spellLevel))
                        {
                            imgui_Text($"Level: {spellLevel}");
                        }
                        
                        if (spellDescription != "NULL" && !string.IsNullOrEmpty(spellDescription) && spellDescription.Length > 0)
                        {
                            imgui_Separator();
                            imgui_PushTextWrapPos(300.0f); // Wrap at 300 pixels
                            imgui_TextWrapped(spellDescription);
                            imgui_PopTextWrapPos();
                        }
                    }
                    else
                    {
                        imgui_Text($"Spell ID: {spellId}");
                    }
                }
                catch (Exception ex)
                {
                    imgui_Text($"Spell ID: {spellId}");
                    imgui_Text($"Error: {ex.Message}");
                }
            }
            else
            {
                imgui_Text($"Icon Index: {iconIndex}");
            }
            
            imgui_EndTooltip();
        }

        /// <summary>
        /// Gets the spell icon index for a given spell ID
        /// This needs to be implemented based on EQ's spell data mapping
        /// </summary>
        private static int GetSpellIconIndex(int spellId)
        {
            try
            {
                // Query MQ for the spell's icon ID
                string iconIdStr = Core.mq_ParseTLO($"${{Spell[{spellId}].SpellIcon}}");
                if (iconIdStr != "NULL" && int.TryParse(iconIdStr, out int iconId))
                {
                    // EQ's spell icon IDs need to be converted to 0-based indices
                    // This conversion may need adjustment based on how EQ stores icon IDs
                    return Math.Max(0, iconId - 1); // Convert to 0-based index
                }
            }
            catch (Exception ex)
            {
                E3Core.Utility.e3util._log.Write($"Error getting spell icon index for spell {spellId}: {ex.Message}");
            }
            
            return 0; // Default to first icon
        }

        /// <summary>
        /// Initializes the spell icon system if not already done
        /// </summary>
        private static void InitializeSpellIcons()
        {
            try
            {
                if (!E3Next.UI.SpellIconManager.IsReady())
                {
                    // Try to get EQ directory from MQ
                    string eqDir = Core.mq_ParseTLO("${EverQuest.Path}");
                    if (eqDir != "NULL" && !string.IsNullOrEmpty(eqDir))
                    {
                        E3Next.UI.SpellIconManager.Initialize(eqDir);
                    }
                    else
                    {
                        E3Core.Utility.e3util._log.Write("Could not determine EQ directory for spell icon initialization");
                    }
                }
            }
            catch (Exception ex)
            {
                E3Core.Utility.e3util._log.Write($"Error initializing spell icons: {ex.Message}");
            }
        }

        #endregion
    }
}
