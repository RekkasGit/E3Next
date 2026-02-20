using E3Core.Data;
using E3Core.Processors;
using E3Core.Utility;
using MonoCore;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static MonoCore.E3ImGUI;
using static System.Windows.Forms.AxHost;

namespace E3Core.UI.Windows.Hud
{
	public static class HudCastingWindow
	{
		private static State_CastingHudWindow _state = new State_CastingHudWindow();
		private static bool _windowInitialized = false;
		private static bool _imguiContextReady = false;
		private static IMQ MQ = E3.MQ;
		private static ISpawns _spawns = E3.Spawns;
		[SubSystemInit]
		public static void Init()
		{
		
			E3ImGUI.RegisterWindow(_state.WindowName, RenderBotCastingWindow);

			EventProcessor.RegisterCommand("/e3hud_casting", (x) =>
			{
				if (Debugger.IsAttached) return;
				if (Core._MQ2MonoVersion < 0.41m)
				{
					E3.MQ.Write("This requires MQ2Mono 0.41 or greater");
					return;
				}
				if (x.args.Count>0)
				{

					if(float.TryParse(x.args[0], out var alpha))
					{
						_state.WindowAlpha = alpha;
					}
					//MQ.Write($"Setting alpha to {_windowAlpha}");

				}

				ToggleWindow();
			}, "toggle memory stats window");
		}
		public static void ToggleWindow()
		{
			try
			{
				if (!_windowInitialized)
				{
					_windowInitialized = true;
					imgui_Begin_OpenFlagSet(_state.WindowName, true);
				}
				else
				{
					bool open = imgui_Begin_OpenFlagGet(_state.WindowName);
					bool newState = !open;
					imgui_Begin_OpenFlagSet(_state.WindowName, newState);
				}
				_imguiContextReady = true;
			}
			catch (Exception ex)
			{
				E3.Log.Write($"Hud Casting Window error: {ex.Message}", Logging.LogLevels.Error);
				_imguiContextReady = false;
			}
		}

		private static ConcurrentDictionary<string, string> PreviousDiscs = new ConcurrentDictionary<string, string>();
		private static ConcurrentDictionary<string, Int64> PreviousDiscTimeStamp = new ConcurrentDictionary<string, long>();
		private static void CheckRefresh()
		{
			if (!e3util.ShouldCheck(ref _state.LastUpdated, _state.UpdateInterval)) return;
			_state.TableRows.Clear();
			//get the connected bots.
			List<string> users = E3.Bots.BotsConnected(readOnly:true); //make a copy as this returns a direct copy of cache
			foreach (var user in users)
			{
				bool inGroupOrRaid = false;
				if (Basics.GroupMemberNames.Contains(user)) inGroupOrRaid = true;
				if (!inGroupOrRaid && Basics.RaidMemberNames.Contains(user)) inGroupOrRaid = true;
				if (!inGroupOrRaid) continue;
				string casting = E3.Bots.Query<String>(user, "${Me.Casting}");
				string targetidString = E3.Bots.Query<String>(user, "${Me.CurrentTargetID}");
				string aaTotal = E3.Bots.Query<String>(user, "${Me.AAPoints}");
				Int32 targetid = 0;
				Int32.TryParse(targetidString, out targetid);
			
				string targetName = "none";
				if(targetid >0 && _spawns.TryByID(targetid, out var s,useCurrentCache:true))
				{
					targetName = s.Name;
				}
				
				var row = new TableRow(user, targetName,casting);
				if(row.Name==E3.CurrentName) row.IsSelf = true;
				row.AATotal = aaTotal;

				if (!PreviousDiscs.ContainsKey(user))
				{
					PreviousDiscs.TryAdd(user, String.Empty);
				}
				string activeDisc = E3.Bots.Query<string>(user, "${Me.ActiveDisc}");
				Int32 durationOfDiscInSeconds = E3.Bots.Query<Int32>(user, "${Me.ActiveDiscTimeLeft}");
			
				if (String.IsNullOrWhiteSpace(activeDisc))
				{
					PreviousDiscs[user] = string.Empty;

				}
				else
				{
					string PreviousDisc = PreviousDiscs[user];
					if(PreviousDisc!=activeDisc)
					{
						PreviousDiscs[user] = activeDisc;
						PreviousDiscTimeStamp.AddOrUpdate(user, Core.StopWatch.ElapsedMilliseconds,
							(key, existingValue) => Core.StopWatch.ElapsedMilliseconds);
					}
				}

				row.ActiveDisc = activeDisc;
				if (!String.IsNullOrEmpty(PreviousDiscs[user]))
				{
					row.ActiveDiscTimeleft = ((((durationOfDiscInSeconds * 1000) + PreviousDiscTimeStamp[user]) - Core.StopWatch.ElapsedMilliseconds)/1000).ToString()+"s";

				}
				_state.TableRows.Add(row);
			}
		}
		private static void RenderBotCastingWindow()
		{
			if (!_imguiContextReady) return;
			if (!imgui_Begin_OpenFlagGet(_state.WindowName)) return;

			PushCurrentTheme();
			try
			{
				CheckRefresh();
			
				using (var window = ImGUIWindow.Aquire())
				{
					imgui_SetNextWindowSizeWithCond(360f, 320f, (int)ImGuiCond.FirstUseEver);
					int flags = ((int)(ImGuiWindowFlags.ImGuiWindowFlags_NoCollapse) | (int)ImGuiWindowFlags.ImGuiWindowFlags_NoTitleBar);

					if (_state.Locked)
					{
						flags = flags | (int)ImGuiWindowFlags.ImGuiWindowFlags_NoMove;
					}
					imgui_SetNextWindowBgAlpha(_state.WindowAlpha);
					if (window.Begin(_state.WindowName, flags))
					{
					
						if (_state.IsDirty)
						{
							if (imgui_Button("Save"))
							{	
								E3.CharacterSettings.SaveData();
								_state.IsDirty = false;
							}
						}
						var entries = _state.TableRows;
						if (entries.Count == 0)
						{
							imgui_TextColored(0.7f, 0.7f, 0.7f, 1.0f, "No connected bots detected.");
							return;
						}
						RenderBotCastingGrid(entries);
					}
				}
			}
			finally
			{
				PopCurrentTheme();
			}
		}
	
		private static void RenderBotCastingGrid(IReadOnlyList<TableRow> entries)
		{
			int columnCount = 3;
			if (entries.Count > 6) columnCount = 4;
			int tableFlags = (int)( ImGuiTableFlags.ImGuiTableFlags_Resizable);
			using (var table = ImGUITable.Aquire())
			{
				if (table.BeginTable("E3HudBotCasting", columnCount, tableFlags, 0, 0))
				{
					for (int col = 0; col < columnCount; col++)
					{
						imgui_TableSetupColumn($"CastingCol{col}", (int)ImGuiTableColumnFlags.ImGuiTableColumnFlags_None, 170f);
					}
					Int32 currentColumn = 0;
					imgui_TableNextRow();
					for (Int32 i  = 0; i < entries.Count; i++)
					{
						imgui_TableSetColumnIndex(currentColumn);
						RenderBotCastingCell(entries[i]);
						currentColumn++;
						if (currentColumn >= columnCount)
						{
							imgui_TableNextRow();
							currentColumn = 0;
						}
					}
				}
			}
			using (var popup = ImGUIPopUpContext.Aquire())
			{
				if (popup.BeginPopupContextItem($"##CastingHudPopup", 1))
				{
					using (var style = PushStyle.Aquire())
					{
						style.PushStyleColor((int)ImGuiCol.Text, 0.95f, 0.85f, 0.35f, 1.0f);
						if (_state.Locked)
						{
							if (imgui_MenuItem("UnLock")) _state.Locked = false;
						}
						else
						{
							if (imgui_MenuItem("Lock")) _state.Locked = true;
						}
					}

					imgui_Separator();
					using (var style = PushStyle.Aquire())
					{
						style.PushStyleColor((int)ImGuiCol.Text, 0.95f, 0.85f, 0.35f, 1.0f);
						imgui_Text("Font");
					}
						

					using (var combo = ImGUICombo.Aquire())
					{
						if (combo.BeginCombo("##Select Font for Casting Hud", _state.SelectedFont))
						{
							foreach (var pair in E3ImGUI.FontList)
							{
								bool sel = string.Equals(_state.SelectedFont, pair.Key, StringComparison.OrdinalIgnoreCase);

								if (imgui_Selectable($"{pair.Key}", sel))
								{
									_state.SelectedFont = pair.Key;
								}
							}
						}
					}
					imgui_Separator();
					using (var style = PushStyle.Aquire())
					{
						style.PushStyleColor((int)ImGuiCol.Text, 0.95f, 0.85f, 0.35f, 1.0f);
						imgui_Text("Alpha");
						
					}
					string keyForInput = $"##CastingHud_alpha_set";
					imgui_SetNextItemWidth(100);
					if (imgui_InputInt(keyForInput, (int)(_state.WindowAlpha * 255), 1, 20))
					{
						int updated = imgui_InputInt_Get(keyForInput);

						if (updated > 255)
						{
							updated = 255;

						}
						if (updated < 0)
						{
							updated = 0;

						}
						_state.WindowAlpha = ((float)updated) / 255f;
						imgui_InputInt_Clear(keyForInput);
					}


					imgui_Separator();
					using (var style = PushStyle.Aquire())
					{
						style.PushStyleColor((int)ImGuiCol.Text, 0.95f, 0.85f, 0.35f, 1.0f);
						imgui_Text("Font Size");
					}
					keyForInput = $"##CastingHud_fontsize_set";
					imgui_SetNextItemWidth(100);
					if (imgui_InputInt(keyForInput, (int)_state.SelectedFontSize, 1, 20))
					{
						int updated = imgui_InputInt_Get(keyForInput);

						if (updated > 100)
						{
							updated = 100;

						}
						if (updated < 1)
						{
							updated = 1;

						}
						_state.SelectedFontSize = updated;
					
						imgui_InputInt_Clear(keyForInput);
					}

					imgui_Separator();
					using (var style = PushStyle.Aquire())
					{
						style.PushStyleColor((int)ImGuiCol.Text, 0.95f, 0.85f, 0.35f, 1.0f);
						imgui_Text("Name Color:");
					}
						
					imgui_Separator();
					if (imgui_ColorPicker4_Float("##CastingHudNameColorPicker", _state.NameColors[0], _state.NameColors[1], _state.NameColors[2], _state.NameColors[3], 0))
					{

						float[] newColors = imgui_ColorPicker_GetRGBA_Float("##CastingHudNameColorPicker");
						_state.NameColors[0] = newColors[0];
						_state.NameColors[1] = newColors[1];
						_state.NameColors[2] = newColors[2];
						_state.NameColors[3] = newColors[3];
						_state.IsDirty = true;
						
					}
				}
			}
		}

		private static void RenderBotCastingCell(TableRow entry)
		{

			if (entry == null)
			{
				imgui_Text(" ");
				return;
			}
			using (var imguiFont = IMGUI_Fonts.Aquire())
			{
				imguiFont.PushFont(_state.SelectedFont);
				imguiFont.PushFontSize(_state.SelectedFontSize);
				imgui_TextColored(_state.NameColors[0], _state.NameColors[1], _state.NameColors[2], _state.NameColors[3], entry.Name);
				
				imgui_SameLine();
				imgui_Text("(");
				imgui_SameLine(0.0f, 0.0f);
				imgui_TextColored(0.169f, 1f, 0f, 1f, entry.AATotal);
				imgui_SameLine(0.0f, 0.0f);
				imgui_Text(")");


				string targetDisplay = string.IsNullOrWhiteSpace(entry.TargetName)
					? "None"
					: entry.TargetName;

				imgui_TextColored(0.65f, 0.85f, 1.0f, 1.0f, "Target:");
				imgui_SameLine();
				imgui_TextColored(0.536f, 1f, 0.333f, 1f, targetDisplay);

				if (!string.IsNullOrWhiteSpace(entry.SpellName) && entry.SpellName != "NULL")
				{
					imgui_TextColored(0.65f, 0.85f, 1.0f, 1.0f, "    Spell:");
					imgui_SameLine();
					string stateText;
					float sr = 0.95f, sg = 0.9f, sb = 0.55f;
					stateText = $"{entry.SpellName}";
					imgui_TextColored(sr, sg, sb, 1.0f, stateText);
				}
				if(!string.IsNullOrEmpty(entry.ActiveDisc))
				{
					imgui_TextColored(0.65f, 0.85f, 1.0f, 1.0f, "    Disc:");
					imgui_SameLine();
					string stateText;
					float sr = 0.95f, sg = 0.9f, sb = 0.55f;
					stateText = $"{entry.ActiveDisc} ({entry.ActiveDiscTimeleft.ToString()})";
					imgui_TextColored(sr, sg, sb, 1.0f, stateText);

				}
				
			}
		}
		public class State_CastingHudWindow
		{
			public string WindowName  = $"E3 Casting Hud - {E3.CurrentName}-{E3.CurrentClass.ToString()}-{E3.ServerName}";
			
			public bool IsDirty = false;
			public List<TableRow> TableRows = new List<TableRow>();

			public Int64 LastUpdated = 0;
			public Int64 UpdateInterval = 250;
			public float[] NameColors { get => E3.CharacterSettings.E3Hud_Casting_RGBA_NameColor; }
			public float WindowAlpha { get => E3.CharacterSettings.E3Hud_Casting_Alpha; set { E3.CharacterSettings.E3Hud_Casting_Alpha = value; IsDirty = true; } }

			public bool Locked { get => E3.CharacterSettings.E3Hud_Casting_Locked; set { E3.CharacterSettings.E3Hud_Casting_Locked = value; IsDirty = true; } }

			public string SelectedFont { get => E3.CharacterSettings.E3Hud_Casting_SelectedFont; set { E3.CharacterSettings.E3Hud_Casting_SelectedFont = value; IsDirty = true; } }
			public Int32 SelectedFontSize { get => E3.CharacterSettings.E3Hud_Casting_SelectedFontSize; set { E3.CharacterSettings.E3Hud_Casting_SelectedFontSize = value; IsDirty = true; } }

			

		}

		public class TableRow
		{
			public string Name { get; set; }
			public string TargetName { get; set; }
			public string SpellName { get; set; }
			public bool IsSelf { get; set; }
			public string AATotal { get; set; }
			public string ActiveDisc { get; set; }
			public string ActiveDiscTimeleft { get; set; }
			public TableRow()
			{
				
			}

			public TableRow(string characterName, string target, string spell)
			{
				Name = characterName;
				TargetName = target;
				SpellName = spell;
			}
		}
	
	}
}
