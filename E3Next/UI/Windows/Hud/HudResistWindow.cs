using E3Core.Data;
using E3Core.Processors;
using E3Core.Utility;
using MonoCore;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using static E3Core.Settings.FeatureSettings.ResistDataFile;
using static MonoCore.E3ImGUI;

namespace E3Core.UI.Windows.Hud
{
	/// <summary>
	/// Shows what the resist DB knows about the target / zone, the live resist counters for the target,
	/// and alerts when a mob gets flagged.
	/// Data is pulled every _updateInterval into the collections below, the render loop only draws them.
	/// </summary>
	public static class HudResistWindow
	{
		private static bool _windowInitialized = false;
		private static bool _imguiContextReady = false;
		private static IMQ MQ = E3.MQ;
		private static ISpawns _spawns = E3.Spawns;
		private static string _WindowName = "E3 Resists";
		private static Int64 _lastUpdate = 0;
		private static Int64 _updateInterval = 250;

		//E3 sets a spell's counter to this when the mob is immune
		private const Int32 _counterImmune = 99;
		private const Int32 _maxLogEntries = 50;
		private const Int64 _bannerDuration = 10000;

		private class ResistTypeInfo
		{
			public string Label;
			public string Command;
			//spells count toward this row by Subcategory when set, otherwise by ResistType == Label
			public string Subcategory;
			public Func<ResistData, bool> Resistant;
			public Func<ResistData, bool> Immune;
			public string ResistButtonID;
			public string ImmuneButtonID;
			public string ClearButtonID;

			public ResistTypeInfo(string label, string command, string subcategory, Func<ResistData, bool> resistant, Func<ResistData, bool> immune)
			{
				Label = label;
				Command = command;
				Subcategory = subcategory;
				Resistant = resistant;
				Immune = immune;
				ResistButtonID = $"Resist##resist_{command}";
				ImmuneButtonID = $"Immune##immune_{command}";
				ClearButtonID = $"Clear##clear_{command}";
			}
		}

		private static readonly ResistTypeInfo[] _types = new ResistTypeInfo[]
		{
			new ResistTypeInfo("Magic", "magic", null, x => x.MagicResistant, x => x.MagicImmune),
			new ResistTypeInfo("Fire", "fire", null, x => x.FireResistant, x => x.FireImmune),
			new ResistTypeInfo("Cold", "cold", null, x => x.ColdResistant, x => x.ColdImmune),
			new ResistTypeInfo("Poison", "poison", null, x => x.PoisonResistant, x => x.PoisonImmune),
			new ResistTypeInfo("Disease", "disease", null, x => x.DiseaseResistant, x => x.DiseaseImmune),
			new ResistTypeInfo("Corruption", "corrupt", null, x => x.CorruptResistant, x => x.CorruptImmune),
			new ResistTypeInfo("Slow", "slow", "Slow", x => x.SlowResistant, x => x.SlowImmune),
			new ResistTypeInfo("Snare", "snare", "Snare", x => x.SnareResistant, x => x.SnareImmune),
			new ResistTypeInfo("Mez", "mez", "Enthrall", x => x.MezResistant, x => x.MezImmune),
			new ResistTypeInfo("Charm", "charm", "Charm", x => x.CharmResistant, x => x.CharmImmune),
			new ResistTypeInfo("Pacify", "calm", "Calm", x => x.PacifyResistant, x => x.PacifyImmune),
		};

		private class ResistRow
		{
			public string Name;
			//two bits per type, resistant = bit (i*2), immune = bit (i*2+1)
			public Int32 Flags;
			public string Summary;
			public string EditButtonID;
			public Int32 SeenStamp;
		}

		//collections the render loop draws from
		private static Dictionary<string, ResistRow> _rowsByName = new Dictionary<string, ResistRow>();
		private static List<ResistRow> _rowsSorted = new List<ResistRow>();
		private static Int32 _loadedZoneID = -1;
		private static Int32 _refreshStamp = 0;

		private static Int32 _targetID = 0;
		private static string _targetName = null;
		private static string _pinnedName = null;
		private static string _filter = string.Empty;

		private static Int32[] _counterCount = new Int32[_types.Length];
		private static Int32[] _counterMax = new Int32[_types.Length];
		private static Int32[] _counterHash = new Int32[_types.Length];
		private static string[] _counterText = new string[_types.Length];
		private static List<string>[] _counterSpells = new List<string>[_types.Length];
		private static Dictionary<Int32, string> _countTextCache = new Dictionary<Int32, string>();

		private static List<string> _log = new List<string>();
		private static string _bannerText = null;
		private static Int64 _bannerUntil = 0;

		[SubSystemInit]
		public static void Init()
		{
			for (Int32 i = 0; i < _types.Length; i++)
			{
				_counterSpells[i] = new List<string>();
			}
			E3ImGUI.RegisterWindow(_WindowName, RenderWindow);

			EventProcessor.RegisterCommand("/e3hud_resist", (x) =>
			{
				if (Debugger.IsAttached) return;
				if (Core._MQ2MonoVersion < 0.41m)
				{
					E3.MQ.Write("This requires MQ2Mono 0.41 or greater");
					return;
				}
				ToggleWindow();
			}, "toggle resist window");
		}
		public static void ToggleWindow()
		{
			try
			{
				if (!_windowInitialized)
				{
					_windowInitialized = true;
					imgui_Begin_OpenFlagSet(_WindowName, true);
				}
				else
				{
					bool open = imgui_Begin_OpenFlagGet(_WindowName);
					bool newState = !open;
					imgui_Begin_OpenFlagSet(_WindowName, newState);
				}
				_imguiContextReady = true;
			}
			catch (Exception ex)
			{
				E3.Log.Write($"Hud Resist Window error: {ex.Message}", Logging.LogLevels.Error);
				_imguiContextReady = false;
			}
		}

		#region refresh
		private static void CheckRefresh()
		{
			if (!e3util.ShouldCheck(ref _lastUpdate, _updateInterval)) return;
			if (E3.ResistSettings == null) return;

			RefreshRows();
			RefreshTarget();
			RefreshCounters();
		}
		private static Int32 GetFlags(ResistData data)
		{
			Int32 flags = 0;
			for (Int32 i = 0; i < _types.Length; i++)
			{
				if (_types[i].Resistant(data)) flags |= 1 << (i * 2);
				if (_types[i].Immune(data)) flags |= 1 << (i * 2 + 1);
			}
			return flags;
		}
		private static bool IsImmune(Int32 flags, Int32 typeIndex)
		{
			return (flags & (1 << (typeIndex * 2 + 1))) != 0;
		}
		private static bool IsResistant(Int32 flags, Int32 typeIndex)
		{
			return (flags & (1 << (typeIndex * 2))) != 0;
		}
		private static string BuildSummary(Int32 flags)
		{
			if (flags == 0) return "In DB: no flags";
			List<string> parts = new List<string>();
			for (Int32 i = 0; i < _types.Length; i++)
			{
				if (IsImmune(flags, i)) parts.Add($"{_types[i].Label} Immune");
				else if (IsResistant(flags, i)) parts.Add($"{_types[i].Label} Resistant");
			}
			return "In DB: " + String.Join(", ", parts);
		}
		private static void RefreshRows()
		{
			var resist = E3.ResistSettings;
			//first read in a zone is the baseline, so zoning doesn't spam alerts
			bool baseline = false;
			if (_loadedZoneID != resist.Current_ZoneID)
			{
				_loadedZoneID = resist.Current_ZoneID;
				_rowsByName.Clear();
				_rowsSorted.Clear();
				_pinnedName = null;
				baseline = true;
			}
			_refreshStamp++;
			bool needsSort = false;
			foreach (var pair in resist.ZoneData)
			{
				Int32 flags = GetFlags(pair.Value);
				if (!_rowsByName.TryGetValue(pair.Key, out var row))
				{
					row = new ResistRow() { Name = pair.Key, Flags = flags, Summary = BuildSummary(flags), EditButtonID = $"Edit##edit_{pair.Key}" };
					_rowsByName.Add(pair.Key, row);
					_rowsSorted.Add(row);
					needsSort = true;
					if (!baseline && flags != 0) AddAlert($"Added {row.Name}: {row.Summary.Substring(7)}", true);
				}
				else if (row.Flags != flags)
				{
					for (Int32 i = 0; i < _types.Length; i++)
					{
						string before = IsImmune(row.Flags, i) ? "Immune" : IsResistant(row.Flags, i) ? "Resistant" : null;
						string after = IsImmune(flags, i) ? "Immune" : IsResistant(flags, i) ? "Resistant" : null;
						if (before == after) continue;
						if (after != null) AddAlert($"{row.Name} is now {_types[i].Label} {after}", true);
						else AddAlert($"{row.Name} cleared {_types[i].Label}", false);
					}
					row.Flags = flags;
					row.Summary = BuildSummary(flags);
				}
				row.SeenStamp = _refreshStamp;
			}
			//rows removed by a reload
			for (Int32 i = _rowsSorted.Count - 1; i >= 0; i--)
			{
				if (_rowsSorted[i].SeenStamp != _refreshStamp)
				{
					_rowsByName.Remove(_rowsSorted[i].Name);
					_rowsSorted.RemoveAt(i);
				}
			}
			if (needsSort) _rowsSorted.Sort((a, b) => String.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));
		}
		private static void RefreshTarget()
		{
			_targetID = MQ.Query<Int32>("${Target.ID}");
			_targetName = null;
			if (_targetID > 0 && _spawns.TryByID(_targetID, out var s, useCurrentCache: true))
			{
				if (s.TypeDesc == "NPC") _targetName = s.CleanName;
			}
		}
		private static Int32 FindTypeIndex(Spell spell)
		{
			for (Int32 i = 0; i < _types.Length; i++)
			{
				if (_types[i].Subcategory != null && _types[i].Subcategory == spell.Subcategory) return i;
			}
			for (Int32 i = 0; i < _types.Length; i++)
			{
				if (_types[i].Subcategory == null && _types[i].Label == spell.ResistType) return i;
			}
			return -1;
		}
		private static string GetCountText(Int32 count, Int32 max)
		{
			if (count >= _counterImmune) return "immune";
			Int32 key = (count << 16) | (max & 0xFFFF);
			if (!_countTextCache.TryGetValue(key, out var text))
			{
				text = $"{count}/{max}";
				_countTextCache.Add(key, text);
			}
			return text;
		}
		private static void RefreshCounters()
		{
			for (Int32 i = 0; i < _types.Length; i++)
			{
				_counterCount[i] = 0;
				_counterMax[i] = 0;
			}
			Int32[] hashes = _scratchHashes;
			Array.Clear(hashes, 0, hashes.Length);

			ResistCounter counter = null;
			if (_targetName != null && Casting.ResistCounters.TryGetValue(_targetID, out counter))
			{
				foreach (var pair in counter.SpellCounters)
				{
					if (!Spell._loadedSpells.TryGetValue(pair.Key, out var spell)) continue;
					Int32 typeIndex = FindTypeIndex(spell);
					if (typeIndex < 0) continue;
					counter.SpellMaxTries.TryGetValue(pair.Key, out var max);
					//the row shows the spell closest to E3 giving up on it
					if (pair.Value > _counterCount[typeIndex])
					{
						_counterCount[typeIndex] = pair.Value;
						_counterMax[typeIndex] = max;
					}
					hashes[typeIndex] = hashes[typeIndex] * 31 + (pair.Key ^ (pair.Value << 20) ^ (max << 26)) + 1;
				}
			}
			//only rebuild the display strings when the counters for a type changed
			for (Int32 i = 0; i < _types.Length; i++)
			{
				if (hashes[i] == _counterHash[i] && _counterText[i] != null) continue;
				_counterHash[i] = hashes[i];
				_counterSpells[i].Clear();
				if (counter == null || hashes[i] == 0)
				{
					_counterText[i] = null;
					continue;
				}
				_counterText[i] = GetCountText(_counterCount[i], _counterMax[i]);
				foreach (var pair in counter.SpellCounters)
				{
					if (!Spell._loadedSpells.TryGetValue(pair.Key, out var spell)) continue;
					if (FindTypeIndex(spell) != i) continue;
					counter.SpellMaxTries.TryGetValue(pair.Key, out var max);
					_counterSpells[i].Add($"{spell.CastName}: {GetCountText(pair.Value, max)}");
				}
			}
		}
		private static Int32[] _scratchHashes = new Int32[_types.Length];
		private static void AddAlert(string text, bool isNew)
		{
			_log.Insert(0, $"{DateTime.Now:HH:mm:ss}  {text}");
			if (_log.Count > _maxLogEntries) _log.RemoveAt(_log.Count - 1);
			MQ.WriteDelayed($"\ay[E3Resist] \ag{text}");
			if (isNew)
			{
				_bannerText = text;
				_bannerUntil = Core.StopWatch.ElapsedMilliseconds + _bannerDuration;
			}
		}
		#endregion

		#region render
		private static void RenderWindow()
		{
			if (!_imguiContextReady) return;
			if (!imgui_Begin_OpenFlagGet(_WindowName)) return;
			try
			{
				using (MQ.GetDelayLock())
				{
					CheckRefresh();
				}
			}
			catch (Exception ex)
			{
				MQ.WriteDelayed($"Error in UI thread, Resist window refresh: message:{ex.Message} payload:{ex.StackTrace} ");
				return;
			}

			imgui_SetNextWindowSizeWithCond(440, 600, (int)ImGuiCond.FirstUseEver);
			E3ImGUI.PushCurrentTheme();
			try
			{
				using (var window = ImGUIWindow.Aquire())
				{
					if (!window.Begin(_WindowName, (int)ImGuiWindowFlags.ImGuiWindowFlags_NoCollapse))
						return;

					if (_bannerText != null && Core.StopWatch.ElapsedMilliseconds < _bannerUntil)
					{
						imgui_TextColored(0.3f, 1.0f, 0.4f, 1.0f, _bannerText);
						imgui_Separator();
					}

					RenderMobPanel();
					imgui_Separator();

					using (var tabBar = ImGUITabBar.Aquire())
					{
						if (tabBar.BeginTabBar("##e3resist_tabs"))
						{
							using (var tab = ImGUITabItem.Aquire())
							{
								if (tab.BeginTabItem("Zone")) RenderZoneTab();
							}
							using (var tab = ImGUITabItem.Aquire())
							{
								if (tab.BeginTabItem("Changes")) RenderLogTab();
							}
						}
					}
				}
			}
			catch (Exception ex)
			{
				MQ.WriteDelayed($"Error in UI thread, Resist window: message:{ex.Message} payload:{ex.StackTrace} ");
			}
			finally
			{
				E3ImGUI.PopCurrentTheme();
			}
		}
		private static void RenderStatus(Int32 flags, Int32 typeIndex)
		{
			if (IsImmune(flags, typeIndex)) imgui_TextColored(1.0f, 0.3f, 0.3f, 1.0f, "Immune");
			else if (IsResistant(flags, typeIndex)) imgui_TextColored(1.0f, 0.85f, 0.2f, 1.0f, "Resistant");
			else imgui_TextColored(0.55f, 0.55f, 0.55f, 1.0f, "-");
		}
		private static void RenderMobPanel()
		{
			bool pinned = _pinnedName != null;
			string name = pinned ? _pinnedName : _targetName;
			if (name == null)
			{
				imgui_TextColored(0.55f, 0.55f, 0.55f, 1.0f, "Target an NPC, or pick one from the Zone tab.");
				return;
			}
			imgui_Text(pinned ? "Selected:" : "Target:");
			imgui_SameLine();
			imgui_TextColored(0.3f, 1.0f, 0.4f, 1.0f, name);
			if (pinned)
			{
				imgui_SameLine();
				if (imgui_Button("Follow target")) _pinnedName = null;
			}

			_rowsByName.TryGetValue(name, out var row);
			Int32 flags = row != null ? row.Flags : 0;
			imgui_Text(row != null ? row.Summary : "Not in the resist DB.");

			//counters are per spawn, so they only apply to the target
			bool showCounters = !pinned;
			using (var table = ImGUITable.Aquire())
			{
				int tableFlags = (int)(ImGuiTableFlags.ImGuiTableFlags_Borders | ImGuiTableFlags.ImGuiTableFlags_RowBg | ImGuiTableFlags.ImGuiTableFlags_SizingFixedFit);
				if (!table.BeginTable("##e3resist_mob", 4, tableFlags, 0f, 0f)) return;

				imgui_TableSetupColumn_Default("Type");
				imgui_TableSetupColumn_Default("Status");
				imgui_TableSetupColumn_Default("Resists");
				imgui_TableSetupColumn_Default("Set");
				imgui_TableHeadersRow();
				for (Int32 i = 0; i < _types.Length; i++)
				{
					var t = _types[i];
					imgui_TableNextRow();
					imgui_TableNextColumn();
					imgui_Text(t.Label);
					imgui_TableNextColumn();
					RenderStatus(flags, i);
					imgui_TableNextColumn();
					string counterText = showCounters ? _counterText[i] : null;
					if (counterText == null)
					{
						imgui_TextColored(0.55f, 0.55f, 0.55f, 1.0f, "-");
					}
					else
					{
						if (_counterCount[i] >= _counterMax[i]) imgui_TextColored(1.0f, 0.3f, 0.3f, 1.0f, counterText);
						else imgui_TextColored(1.0f, 0.85f, 0.2f, 1.0f, counterText);
						if (imgui_IsItemHovered() && _counterSpells[i].Count > 0)
						{
							using (var tooltip = ImGUIToolTip.Aquire())
							{
								tooltip.BeginToolTip();
								foreach (var line in _counterSpells[i]) imgui_Text(line);
							}
						}
					}
					imgui_TableNextColumn();
					if (imgui_Button(t.ResistButtonID)) RunResistCommand(t, null, name);
					imgui_SameLine();
					if (imgui_Button(t.ImmuneButtonID)) RunResistCommand(t, "immune", name);
					if (IsImmune(flags, i) || IsResistant(flags, i))
					{
						imgui_SameLine();
						if (imgui_Button(t.ClearButtonID)) RunResistCommand(t, "clear", name);
					}
				}
			}
		}
		private static void RunResistCommand(ResistTypeInfo t, string mode, string name)
		{
			//E3 applies the command to every mob in the zone if the name is empty
			if (String.IsNullOrWhiteSpace(name)) return;
			string extra = mode == null ? string.Empty : mode + " ";
			E3ImGUI.MQCommandQueue.Enqueue($"/e3resist-{t.Command} {extra}\"{name}\"");
		}
		private static void RenderZoneTab()
		{
			if (imgui_InputText("##e3resist_filter", _filter))
			{
				_filter = imgui_InputText_Get("##e3resist_filter") ?? string.Empty;
			}
			imgui_SameLine();
			imgui_Text("Filter");

			using (var table = ImGUITable.Aquire())
			{
				int tableFlags = (int)(ImGuiTableFlags.ImGuiTableFlags_Borders | ImGuiTableFlags.ImGuiTableFlags_RowBg | ImGuiTableFlags.ImGuiTableFlags_ScrollY | ImGuiTableFlags.ImGuiTableFlags_Resizable);
				if (!table.BeginTable("##e3resist_zone", 3, tableFlags, 0f, 300f)) return;

				imgui_TableSetupColumn("Mob", (int)ImGuiTableColumnFlags.ImGuiTableColumnFlags_WidthStretch, 0);
				imgui_TableSetupColumn("Flags", (int)ImGuiTableColumnFlags.ImGuiTableColumnFlags_WidthStretch, 0);
				imgui_TableSetupColumn("", (int)ImGuiTableColumnFlags.ImGuiTableColumnFlags_WidthFixed, 45);
				imgui_TableHeadersRow();
				foreach (var row in _rowsSorted)
				{
					if (_filter.Length > 0 && row.Name.IndexOf(_filter, StringComparison.OrdinalIgnoreCase) < 0) continue;
					imgui_TableNextRow();
					imgui_TableNextColumn();
					imgui_Text(row.Name);
					imgui_TableNextColumn();
					bool first = true;
					for (Int32 i = 0; i < _types.Length; i++)
					{
						bool immune = IsImmune(row.Flags, i);
						if (!immune && !IsResistant(row.Flags, i)) continue;
						if (!first) imgui_SameLine();
						first = false;
						if (immune) imgui_TextColored(1.0f, 0.3f, 0.3f, 1.0f, _types[i].Label);
						else imgui_TextColored(1.0f, 0.85f, 0.2f, 1.0f, _types[i].Label);
					}
					if (first) imgui_TextColored(0.55f, 0.55f, 0.55f, 1.0f, "no flags");
					imgui_TableNextColumn();
					if (imgui_Button(row.EditButtonID)) _pinnedName = row.Name;
				}
			}
		}
		private static void RenderLogTab()
		{
			if (_log.Count == 0)
			{
				imgui_Text("No changes seen since the window opened.");
				return;
			}
			if (imgui_Button("Clear log")) _log.Clear();
			foreach (var line in _log) imgui_Text(line);
		}
		#endregion
	}
}
