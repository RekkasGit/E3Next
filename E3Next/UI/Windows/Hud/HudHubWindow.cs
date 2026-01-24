using E3Core.Data;
using E3Core.Processors;
using E3Core.Utility;
using Google.Protobuf;
using MonoCore;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Web.SessionState;
using System.Xml.Linq;
using static E3Core.UI.Windows.MemStats.MemoryStatsWindow;
using static MonoCore.E3ImGUI;
using static System.Windows.Forms.AxHost;

namespace E3Core.UI.Windows.Hud
{
	public static class HudHubWindow
	{
		private static float _windowAlpha = 0;
		private static bool _windowInitialized = false;
		private static bool _imguiContextReady = false;
		private static Int64 _lastUpdated_GroupInfo = 0;
		private static Int64 _lastUpdateInterval_GroupInfo = 500;
		private static Int64 _lastUpdated_Buffs = 0;
		private static Int64 _lastUpdateInterval_Buffs = 250;
		private static List<TableRow_GroupInfo> _tableRows_GroupInfo = new List<TableRow_GroupInfo>();
		private static List<TableRow_BuffInfo> _tableRowsBuffInfo = new List<TableRow_BuffInfo>();
		private static List<TableRow_BuffInfo> _tableRowsSongInfo = new List<TableRow_BuffInfo>();
		private static List<TableRow_BuffInfo> _tableRowsDebuffInfo = new List<TableRow_BuffInfo>();
		private static Dictionary<Int32, Spell> _spellCache = new Dictionary<int, Spell>();
		private static IMQ MQ = E3.MQ;
		private static ISpawns _spawns = E3.Spawns;
		private static string _WindowName = "E3 Main Hud";
		private static readonly (double MinDist, double MaxDist, float R, float G, float B)[] _distanceSeverity = new[]
		{
			(0d, 100d,  0.25f, 0.85f, 0.25f),
			(100d, 200d,0.6f, 0.9f, 0.6f),
			(200d, 300d, 0.95f, 0.85f, 0.35f),
			(300d, 400d, 1.0f, 0.7f, 0.2f),
            (400d, 500d, 1.0f, 0.35f, 0.2f),
			(500d, double.MaxValue, 1.0f, 0.05f, 0.05f)
		};
		private static readonly (double MaxValue, double MinValue, float R, float G, float B)[] _resourceSeverity = new[]
		{
			(100d, 90d,  0.25f, 0.85f, 0.25f),
			(90d, 80d,0.6f, 0.9f, 0.6f),
			(80d, 60d, 0.95f, 0.85f, 0.35f),
			(60d, 50d, 1.0f, 0.7f, 0.2f),
			(50d, 30d, 1.0f, 0.35f, 0.2f),
			(30d, 0, 1.0f, 0.05f, 0.05f)
		};
		private static readonly (double MaxValue, double MinValue, float R, float G, float B)[] _buffDurationSeverity = new[]
		{
			(double.MaxValue, 600000d,  0.25f, 0.85f, 0.25f),
			(600000d, 300000d, 0.95f, 0.85f, 0.35f),
			(300000d, 160000d, 1.0f, 0.7f, 0.2f),
			(160000d, 85000d, 1.0f, 0.35f, 0.2f),
			(85000d, 0, 1.0f, 0.05f, 0.05f)
		};
		[SubSystemInit]
		public static void Init()
		{
			if (Core._MQ2MonoVersion < 0.37m) return;
			E3ImGUI.RegisterWindow(_WindowName, RenderHub);

			EventProcessor.RegisterCommand("/e3hud_hub", (x) =>
			{
				if (Core._MQ2MonoVersion < 0.37m)
				{
					MQ.Write("This requires MQ2Mono 0.37 or greater");
					return;
				}
				if (x.args.Count > 0)
				{
					float.TryParse(x.args[0], out _windowAlpha);
					//MQ.Write($"Setting alpha to {_windowAlpha}");

				}
				ToggleWindow();
			}, "toggle hub window");
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
				E3.Log.Write($"Hud Casting Window error: {ex.Message}", Logging.LogLevels.Error);
				_imguiContextReady = false;
			}
		}
		private static (float r, float g, float b) GetDistanceSeverityColor(double distance)
		{

			foreach (var band in _distanceSeverity)
			{
				if (distance >= band.MinDist && distance < band.MaxDist)
				{
					return (band.R, band.G, band.B);
				}
			}

			return (0.9f, 0.9f, 0.9f);
		}
		private static (float r, float g, float b) GetResourceSeverityColor(double resourceValue)
		{

			foreach (var band in _resourceSeverity)
			{
				if (resourceValue <= band.MaxValue && resourceValue > band.MinValue)
				{
					return (band.R, band.G, band.B);
				}
			}

			return (0.9f, 0.9f, 0.9f);
		}
		private static (float r, float g, float b) GetBuffDurationSeverityColor(double duration)
		{

			foreach (var band in _buffDurationSeverity)
			{
				if (duration <= band.MaxValue && duration > band.MinValue)
				{
					return (band.R, band.G, band.B);
				}
			}

			return (0.9f, 0.9f, 0.9f);
		}


		static Task UpdateTask;


		private static void ProcessUpdates()
		{
			RefreshBuffInfo();
			RefreshGroupInfo();
		}


		static string _prevousBuffInfo = string.Empty;
		static HashSet<Int32> _previousBuffs = new HashSet<Int32>();
		static Dictionary<Int32,Int64> _newbuffsTimeStamps = new Dictionary<Int32,Int64>();
		private static void RefreshBuffInfo()
		{
			if (!e3util.ShouldCheck(ref _lastUpdated_Buffs, _lastUpdateInterval_Buffs)) return;
			
			string buffInfo = E3.Bots.Query(E3.CurrentName, "${Me.BuffInfo}");

			if (_prevousBuffInfo!=string.Empty)
			{
				if(_prevousBuffInfo==buffInfo)
				{
					//no difference
					return;
				}
				
			}
			_prevousBuffInfo = buffInfo;

			_tableRowsBuffInfo.Clear();
			_tableRowsSongInfo.Clear();
			_tableRowsDebuffInfo.Clear();

			if (!String.IsNullOrWhiteSpace(buffInfo))
			{
				string s = buffInfo;

				BuffDataList bufflist = new BuffDataList();
				bufflist.MergeFrom(ByteString.FromBase64(s));
				
				foreach (var buff in bufflist.Data)
				{

					Int32 spellid = 0;
					Int32 duration = 0;
					Int32 hitcount = 0;
					Int32 spelltypeid = 0;
					Int32 bufftype = 0;
					Int32 counterType = -1;
					Int32 counterNumber = -1;

					spellid = buff.SpellID;
					duration = buff.Duration;
					hitcount = buff.Hitcount;
					spelltypeid = buff.SpellTypeID;
					bufftype = buff.BuffType;
					counterType = buff.CounterType;
					counterNumber = buff.CounterNumber;

					Int32 spellIcon = MQ.Query<Int32>($"${{Spell[{spellid}].SpellIcon}}", false);
					string buffName = MQ.Query<string>($"${{Spell[{spellid}].Name}}", false);
					var buffRow = new TableRow_BuffInfo(buffName);
					buffRow.iconID = spellIcon;

					var buffTimeSpan = TimeSpan.FromMilliseconds(duration);
					buffRow.Duration = buffTimeSpan.ToString("h'h 'm'm 's's'");
					buffRow.DurationColor = GetBuffDurationSeverityColor(duration);
					buffRow.SpellType = (Int32)spelltypeid;
					//check if spellid exists

					if (BuffCheck.BuffInfoCache.ContainsKey(spellid))
					{
						buffRow.Spell = BuffCheck.BuffInfoCache[spellid];
					}
					else
					{
						buffRow.Spell = null;
					}


					if (hitcount > 0)
					{
						buffRow.HitCount = hitcount.ToString();
					}

					if (duration <0)
					{
						buffRow.SimpleDuration = "(p)";
					}
					else if (buffTimeSpan.TotalHours >= 1)
					{
						buffRow.SimpleDuration = ((int)buffTimeSpan.TotalHours).ToString() + "h";
					}
					else if (buffTimeSpan.TotalMinutes >= 1)
					{
						buffRow.SimpleDuration = ((int)buffTimeSpan.TotalMinutes).ToString() + "m";
					}
					else
					{
						buffRow.SimpleDuration = ((int)buffTimeSpan.TotalSeconds).ToString() + "s";
					}
					if (duration < 160000d)
					{
						buffRow.DisplayName = buffName + $" ( {buffRow.Duration} )";
					}
					else
					{
						buffRow.DisplayName = buffName;
					}
					if (bufftype == 0) //if normal buff
					{
						if (spelltypeid == 0)
						{
							if (counterType == 0) buffRow.CounterType = "Disease";
							else if (counterType == 1) buffRow.CounterType = "Poison";
							else if (counterType == 2) buffRow.CounterType = "Curse";
							else if (counterType == 3) buffRow.CounterType = "Corruption";

							if (counterNumber > 0)
							{
								buffRow.CounterNumber = counterNumber.ToString();

							}
							_tableRowsDebuffInfo.Add(buffRow);
						}
						else
						{
							_tableRowsBuffInfo.Add(buffRow);
						}
					}
					else
					{
						//this is a song
						_tableRowsSongInfo.Add(buffRow);
					}
				}

				if (_previousBuffs.Count > 0)
				{
					foreach (var buff in _tableRowsBuffInfo)
					{
						if (!_previousBuffs.Contains(buff.Spell.SpellID))
						{
							_newbuffsTimeStamps[buff.Spell.SpellID] = Core.StopWatch.ElapsedMilliseconds;
						}
					}
				}
				_previousBuffs.Clear();
				foreach(var buff in _tableRowsBuffInfo)
				{
					_previousBuffs.Add(buff.Spell.SpellID);
				}


				//comma delimited list of spelid:buffduration_in_ms
				//need to get the spellid
				
				//int start = 0;
				//int end = 0;
				//char delim = ':';
				//List<Int64> tempBuffer = new List<Int64>();
				//foreach (char x in s)
				//{
				//	if (x == delim || end == s.Length - 1)
				//	{
				//		if (end == s.Length - 1 && x != delim)
				//			end++;
				//		//number,number
				//		tempBuffer.Clear();
				//		string tstring = s.Substring(start, end - start);
				//		e3util.StringsToNumbers(tstring, ',', tempBuffer);
				//		spellid =(Int32) tempBuffer[0];

						


				//		duration=tempBuffer[1];
				//		hitcount = tempBuffer[2];
				//		spelltypeid = tempBuffer[3];
				//		bufftype = tempBuffer[4];
				//		counterType = tempBuffer[5];
				//		counterNumber = tempBuffer[6];


						
							

				//		start = end + 1;
				//	}
				//	end++;
				//}
			}
		}
		private static void RefreshGroupInfo()
		{
			if (!e3util.ShouldCheck(ref _lastUpdated_GroupInfo, _lastUpdateInterval_GroupInfo)) return;
			_tableRows_GroupInfo.Clear();
			//get the connected bots.
			List<string> users = E3.Bots.BotsConnected().ToList(); //make a copy as this returns a direct copy of cache
			users.Sort();
			foreach (var user in users)
			{
				//if (user == E3.CurrentName) continue;
				bool inGroupOrRaid = false;
				if (Basics.GroupMemberNames.Contains(user)) inGroupOrRaid = true;
				if (!inGroupOrRaid && Basics.RaidMemberNames.Contains(user)) inGroupOrRaid = true;
				if (!inGroupOrRaid) continue;

				string casting = E3.Bots.Query(user, "${Me.Casting}");
				double x, y, z = 0;

				Int32 mana, endurance, hp;
				Int32.TryParse(E3.Bots.Query(user, "${Me.PctHPs}"), out hp);
				Int32.TryParse(E3.Bots.Query(user, "${Me.PctMana}"), out mana);
				Int32.TryParse(E3.Bots.Query(user, "${Me.PctEndurance}"), out endurance);

				double.TryParse(E3.Bots.Query(user, "${Me.X}"), out x);
				double.TryParse(E3.Bots.Query(user, "${Me.Y}"), out y);
				double.TryParse(E3.Bots.Query(user, "${Me.Z}"), out z);
				string zoneid = E3.Bots.Query(user, "${Me.ZoneID}");
				string zonename = E3.Bots.Query(user, "${Me.ZoneShortName}");
				double distance = 0;
				bool isInvis = false;
				bool.TryParse(E3.Bots.Query(user, "${Me.IsInvis}"), out isInvis);
				bool isInvul = false;
				bool.TryParse(E3.Bots.Query(user, "${Me.Invulnerable}"), out isInvul);


				if (_spawns.TryByName(user, out var spawn))
				{
					x = E3.Loc_X - x;
					y = E3.Loc_Y - y;
					z = E3.Loc_Z - z;
					//we can calculate distance
					distance = Math.Sqrt(x * x + y * y + z * z);


				}
				var row = new TableRow_GroupInfo(user);
				if (distance == 0)
				{
					row.Distance = zonename;

				}
				else
				{
					row.Distance = distance.ToString("N2");
					row.DistanceColor = GetDistanceSeverityColor(distance);
				}

				if (isInvis)
				{
					row.DisplayName = "(" + user + ")";
					row.DisplayNameColor = (0.427f, 0.595f, 0.610f); //grayish
				}
				else if (isInvul)
				{
					row.DisplayName = "[" + user + "]";
					row.DisplayNameColor= (0.950f, 0.910f, 0.143f); //GOLD
				}
				else
				{
					row.DisplayName = user;
					row.DisplayNameColor= (0.275f, 0.860f, 0.85f);
				}
				row.Mana = mana.ToString() + "%";
				row.ManaColor = GetResourceSeverityColor(mana);

				row.Endurance = endurance.ToString() + "%";
				row.EndColor = GetResourceSeverityColor(endurance);

				row.HP = hp.ToString() + "%";
				row.HPColor = GetResourceSeverityColor(hp);

				_tableRows_GroupInfo.Add(row);
			}
		}
		private static void RenderHub()
		{
			if (!_imguiContextReady) return;
			if (!imgui_Begin_OpenFlagGet(_WindowName)) return;

			if (!_imguiContextReady) return;
			if (!imgui_Begin_OpenFlagGet(_WindowName)) return;
			RefreshGroupInfo();
			imgui_SetNextWindowSizeWithCond(400, 300, (int)ImGuiCond.FirstUseEver);
			E3ImGUI.PushCurrentTheme();
			try
			{
				using (var window = ImGUIWindow.Aquire())
				{
					imgui_SetNextWindowBgAlpha(_windowAlpha);
					if (!window.Begin(_WindowName, (int)ImGuiWindowFlags.ImGuiWindowFlags_NoCollapse))
						return;
					

					RenderGroupTable();
					RenderBuffTableSimple();

					
				}
			}
			finally
			{
				E3ImGUI.PopCurrentTheme();
			}
		}
		private static void RenderBuffTableSimple()
		{
			RefreshBuffInfo();

			using (var igFont = IMGUI_Fonts.Aquire())
			{
				igFont.PushFont("arial_bold-20");

				Int32 iconSize = 40;
				Int32 fontSize = 8;

				float widthAvail = imgui_GetContentRegionAvailX();

				Int32 numberOfBuffsPerRow = (int)widthAvail / iconSize;

				if(numberOfBuffsPerRow<1 ) numberOfBuffsPerRow = 1;

				if (_tableRowsDebuffInfo.Count > 0)
				{
					imgui_Text("Debuffs:");
					using (var table = ImGUITable.Aquire())
					{
						int tableFlags = (int)(ImGuiTableFlags.ImGuiTableFlags_SizingFixedFit |
											  ImGuiTableFlags.ImGuiTableFlags_BordersOuter
											  );

						float tableHeight = Math.Max(150f, imgui_GetContentRegionAvailY());
						if (table.BeginTable("E3HubDebuffTableSimple", 1, tableFlags, 0f, 0))
						{
							imgui_TableSetupColumn_Default("Debuffs");
							List<TableRow_BuffInfo> currentStats = _tableRowsDebuffInfo;
							Int32 counter = 0;
							foreach (var stats in currentStats)
							{
								if (counter % numberOfBuffsPerRow == 0)
								{
									imgui_TableNextRow();
									imgui_TableNextColumn();

								}
								else
								{
									imgui_SameLine(0, 0);
								}
								float x = imgui_GetCursorScreenPosX();
								float y = imgui_GetCursorScreenPosY();
								imgui_DrawSpellIconByIconIndex(stats.iconID, iconSize);
								if (stats.SpellType == 0)
								{
									imgui_GetWindowDrawList_AddRectFilled(x, y, x + iconSize, y + iconSize, GetColor(255, 0, 0, 50));
								}
								if (!String.IsNullOrWhiteSpace(stats.SimpleDuration))
								{
									float newX = x + (float)(iconSize / 2) - (fontSize);
									float newY = y + (float)((iconSize) - (fontSize * 2));

									imgui_GetWindowDrawList_AddText(newX, newY, GetColor(255, 255, 255, 255), stats.SimpleDuration);

								}
								if (!String.IsNullOrWhiteSpace(stats.CounterNumber))
								{
									imgui_GetWindowDrawList_AddText(x, y, GetColor(255, 255, 255, 255), stats.CounterNumber);

								}
								if (imgui_IsItemHovered())
								{
									using (var tooltip = ImGUIToolTip.Aquire())
									{
										imgui_Text($"Spell: {stats.Name}");
										imgui_Text($"SpellID: {stats.Spell.SpellID.ToString()}");
										imgui_Text($"Duration: {stats.Duration}");
										if (!String.IsNullOrWhiteSpace(stats.CounterType))
										{
											imgui_Text($"CounterType:{stats.CounterType}");
											imgui_Text($"CounterNumber:{stats.CounterNumber}");
										}
										if (stats.Spell != null && stats.Spell.SpellEffects.Count > 0)
										{
											imgui_Separator();
											foreach (var effect in stats.Spell.SpellEffects)
											{
												if (!string.IsNullOrWhiteSpace(effect))
												{
													imgui_Text(effect);

												}
											}
										}
									}
								}
								counter++;
							}
						}
					}
					imgui_Separator();
				}

				imgui_Text("Buffs:");
				using (var table = ImGUITable.Aquire())
				{
					int tableFlags = (int)(ImGuiTableFlags.ImGuiTableFlags_SizingFixedFit |
										  ImGuiTableFlags.ImGuiTableFlags_BordersOuter
										  );


					//imgui_PushStyleColor((int)ImGuiCol.TableRowBg,0,0,0,0);
					//imgui_PushStyleColor((int)ImGuiCol.TableRowBgAlt, 0, 0, 0, 0);
					//imgui_PushStyleColor((int)ImGuiCol.ChildBg, 0, 0, 0, 0);
					if (table.BeginTable("E3HubBuffTableSimple", 1, tableFlags, 0f, 0))
					{
						imgui_TableSetupColumn_Default("Buffs");
						List<TableRow_BuffInfo> currentStats = _tableRowsBuffInfo;
						Int32 counter = 0;
						foreach (var stats in currentStats)
						{
							if (counter % numberOfBuffsPerRow == 0)
							{
								imgui_TableNextRow();
								imgui_TableNextColumn();
								//imgui_TableSetBgColor((int)ImGuiTableBgTarget.RowBg0, GetColor(0, 0, 0, 0), -1);

							}
							else
							{
								imgui_SameLine(0, 0);
							}
							float x = imgui_GetCursorScreenPosX();
							float y = imgui_GetCursorScreenPosY();
							imgui_DrawSpellIconByIconIndex(stats.iconID, iconSize);
							
							if(_newbuffsTimeStamps.TryGetValue(stats.Spell.SpellID,out var ts))
							{
								Int64 timeDelta = Core.StopWatch.ElapsedMilliseconds - ts;

								long alpha = (Int64)(timeDelta * 0.1275);

								if (alpha > 255) alpha = 255;
								imgui_GetWindowDrawList_AddRectFilled(x, y, x + iconSize, y + iconSize, GetColor(0, 255, 0, 255-(uint)alpha));
								
								if(timeDelta>2000)	_newbuffsTimeStamps.Remove(stats.Spell.SpellID);
								
							}
							using (var popup = ImGUIPopUpContext.Aquire())
							{
								if (popup.BeginPopupContextItem($"BuffTableIconContext-{counter}", 1))
								{
									imgui_PushStyleColor((int)ImGuiCol.Text, 0.95f, 0.85f, 0.35f, 1.0f);
									imgui_Text(stats.Name);
									imgui_Separator();
									if (imgui_MenuItem("Drop buff"))
									{
										E3ImGUI.MQCommandQueue.Enqueue($"/removebuff {stats.Spell.SpellName}");
									}
									if (imgui_MenuItem("Drop buff from group"))
									{
										E3ImGUI.MQCommandQueue.Enqueue($"/removebuff {stats.Spell.SpellName}");
										E3.Bots.BroadcastCommandToGroup($"/removebuff {stats.Spell.SpellName}");
									}
									if (imgui_MenuItem("Drop buff from everyone"))
									{
										E3ImGUI.MQCommandQueue.Enqueue($"/removebuff {stats.Spell.SpellName}");
										E3.Bots.BroadcastCommand($"/removebuff {stats.Spell.SpellName}");
									}
									imgui_PopStyleColor(1);

								}
							}
							if (stats.SpellType == 0)
							{
								imgui_GetWindowDrawList_AddRectFilled(x, y, x + iconSize, y + iconSize, GetColor(255, 0, 0, 125));
							}
							if (!String.IsNullOrWhiteSpace(stats.SimpleDuration))
							{
								float newX = x + (float)(iconSize / 2) - (fontSize);
								float newY = y + (float)((iconSize) - (fontSize * 2));
								imgui_GetWindowDrawList_AddRectFilled(newX, newY, newX + (fontSize * 2), newY + (iconSize - (newY - y)), GetColor(0, 0, 0, 100));
								imgui_GetWindowDrawList_AddText(newX, newY, GetColor(255, 255, 255, 255), stats.SimpleDuration);

							}

							if (!String.IsNullOrWhiteSpace(stats.HitCount))
							{
								imgui_GetWindowDrawList_AddText(x, y, GetColor(255, 255, 255, 255), stats.HitCount);

							}
							if (imgui_IsItemHovered())
							{
								using (var tooltip = ImGUIToolTip.Aquire())
								{
									imgui_Text($"Spell: {stats.Name}");
									imgui_Text($"SpellID: {stats.Spell.SpellID.ToString()}");
									imgui_Text($"Duration: {stats.Duration}");

									if (stats.Spell != null && stats.Spell.SpellEffects.Count > 0)
									{
										imgui_Separator();
										foreach (var effect in stats.Spell.SpellEffects)
										{
											if (!string.IsNullOrWhiteSpace(effect))
											{
												imgui_Text(effect);

											}
										}

									}

								}
							}


							counter++;
						}
					}
					//imgui_PopStyleColor(3);

				}

				imgui_Separator();
				imgui_Text("Songs:");
				using (var table = ImGUITable.Aquire())
				{
					int tableFlags = (int)(ImGuiTableFlags.ImGuiTableFlags_SizingFixedFit |
										  ImGuiTableFlags.ImGuiTableFlags_BordersOuter
										  );

					float tableHeight = Math.Max(150f, imgui_GetContentRegionAvailY());
					if (table.BeginTable("E3HubSongTableSimple", 1, tableFlags, 0f, 0))
					{
						imgui_TableSetupColumn_Default("Songs");
						List<TableRow_BuffInfo> currentStats = _tableRowsSongInfo;
						Int32 counter = 0;
						foreach (var stats in currentStats)
						{
							if (counter % numberOfBuffsPerRow == 0)
							{
								imgui_TableNextRow();
								imgui_TableNextColumn();

							}
							else
							{
								imgui_SameLine(0, 0);
							}
							float x = imgui_GetCursorScreenPosX();
							float y = imgui_GetCursorScreenPosY();
							imgui_DrawSpellIconByIconIndex(stats.iconID, iconSize);
							using (var popup = ImGUIPopUpContext.Aquire())
							{
								if (popup.BeginPopupContextItem($"SongTableIconContext-{counter}", 1))
								{
									imgui_PushStyleColor((int)ImGuiCol.Text, 0.95f, 0.85f, 0.35f, 1.0f);
									imgui_Text(stats.Name);
									imgui_Separator();
									if (imgui_MenuItem("Drop buff"))
									{
										E3ImGUI.MQCommandQueue.Enqueue($"/removebuff {stats.Spell.SpellName}");
									}
									if (imgui_MenuItem("Drop buff from group"))
									{
										E3ImGUI.MQCommandQueue.Enqueue($"/removebuff {stats.Spell.SpellName}");
										E3.Bots.BroadcastCommandToGroup($"/removebuff {stats.Spell.SpellName}");
									}
									if (imgui_MenuItem("Drop buff from everyone"))
									{
										E3ImGUI.MQCommandQueue.Enqueue($"/removebuff {stats.Spell.SpellName}");
										E3.Bots.BroadcastCommand($"/removebuff {stats.Spell.SpellName}");
									}
									imgui_PopStyleColor(1);

								}
							}
							if (stats.SpellType == 0)
							{
								imgui_GetWindowDrawList_AddRectFilled(x, y, x + iconSize, y + iconSize, GetColor(255, 0, 0, 50));
							}
							if (!String.IsNullOrWhiteSpace(stats.SimpleDuration))
							{
								float newX = x + (float)(iconSize / 2) - (fontSize);
								float newY = y + (float)((iconSize) - (fontSize * 2));
								imgui_GetWindowDrawList_AddRectFilled(newX, newY, newX + (fontSize * 2), newY + (iconSize - (newY - y)), GetColor(0, 0, 0, 100));
								imgui_GetWindowDrawList_AddText(x + (float)(iconSize / 2) - (fontSize), y + (float)((iconSize) - (fontSize * 2)), GetColor(255, 255, 255, 255), stats.SimpleDuration);

							}

							if (!String.IsNullOrWhiteSpace(stats.HitCount))
							{
								imgui_GetWindowDrawList_AddText(x, y, GetColor(255, 255, 255, 255), stats.HitCount);

							}
							if (imgui_IsItemHovered())
							{
								using (var tooltip = ImGUIToolTip.Aquire())
								{
									imgui_Text($"Spell: {stats.Name}");
									imgui_Text($"SpellID: {stats.Spell.SpellID.ToString()}");
									imgui_Text($"Duration: {stats.Duration}");
									if (stats.Spell != null && stats.Spell.SpellEffects.Count > 0)
									{
										imgui_Separator();
										foreach (var effect in stats.Spell.SpellEffects)
										{
											if (!string.IsNullOrWhiteSpace(effect))
											{
												imgui_Text(effect);

											}
										}

									}
								}
							}
							counter++;
						}
					}
				}
			}
		}
		private static void RenderBuffTable()
		{
			RefreshBuffInfo();
			
			using (var table = ImGUITable.Aquire())
			{
				int tableFlags = (int)(ImGuiTableFlags.ImGuiTableFlags_SizingFixedFit | ImGuiTableFlags.ImGuiTableFlags_RowBg |
									  ImGuiTableFlags.ImGuiTableFlags_BordersOuter |
									  ImGuiTableFlags.ImGuiTableFlags_BordersInner |
									  ImGuiTableFlags.ImGuiTableFlags_ScrollY | ImGuiTableFlags.ImGuiTableFlags_Resizable);

				float tableHeight = Math.Max(150f, imgui_GetContentRegionAvailY());

				if (table.BeginTable("E3HubBuffTable", 2, tableFlags, 0f, tableHeight))
				{
					imgui_TableSetupColumn_Default("Buffs");
					imgui_TableSetupColumn_Default("Name");
					imgui_TableHeadersRow();

					List<TableRow_BuffInfo> currentStats = _tableRowsBuffInfo;

					foreach (var stats in currentStats)
					{
						imgui_TableNextRow();
						imgui_TableNextColumn();
						imgui_DrawSpellIconByIconIndex(stats.iconID, 20.0f);
						imgui_TableNextColumn();
						var c = stats.DurationColor;
						imgui_TextColored(c.r, c.g, c.b, 1.0f, stats.DisplayName);
						imgui_TableNextColumn();
					}
				}
			}
		}


		private static string GetGroupFontNameForSize()
		{
			float sizeOfContent = imgui_GetContentRegionAvailX();

			imgui_Text("Size-forName:");
			imgui_SameLine(0);
			imgui_Text(imgui_GetContentRegionAvailX().ToString());

			if (sizeOfContent >= 650)
			{
				return "arial_bold-40";

			}
			if (sizeOfContent > 395)
			{
				return "arial_bold-24";
			}
			if (sizeOfContent > 375)
			{
				return "arial-24";

			}
			if (sizeOfContent > 305)
			{
				return "robo-large";

			}
			if (sizeOfContent > 244)
			{
				return "robo";

			}
			if (sizeOfContent >225)
			{
				return "arial-15";

			}
			return "arial-10";
			
		}
		private static string _selectedGroupFont = "robo";
		private static void RenderGroupTable()
		{

				//		_selectedGroupFont = GetGroupFontNameForSize();

			using (var combo = ImGUICombo.Aquire())
			{
				if (combo.BeginCombo("##Select Font for GroupTable", _selectedGroupFont))
				{
					foreach (var pair in E3ImGUI.FontList)
					{
						bool sel = string.Equals(_selectedGroupFont, pair.Key, StringComparison.OrdinalIgnoreCase);

						if (imgui_Selectable($"{pair.Key}", sel))
						{
							_selectedGroupFont = pair.Key;
						}
					}
				}
			}

			using (var imguiFont = IMGUI_Fonts.Aquire())
			{
				imguiFont.PushFont(_selectedGroupFont);
				imgui_Text("Size:");
				imgui_SameLine(0);
				imgui_Text(imgui_GetContentRegionAvailX().ToString());
				using (var table = ImGUITable.Aquire())
				{
					int tableFlags = (int)(ImGuiTableFlags.ImGuiTableFlags_SizingFixedFit |
										  ImGuiTableFlags.ImGuiTableFlags_BordersOuter
										  );

					//float tableHeight = Math.Max(150f, imgui_GetContentRegionAvailY());
					float tableHeight = 200f;

					if (table.BeginTable("E3HubGroupTable", 5, tableFlags, 0f, 0))
					{

						imgui_TableSetupColumn_Default("Character");
						imgui_TableSetupColumn_Default("HP");
						imgui_TableSetupColumn_Default("End");
						imgui_TableSetupColumn_Default("Mana");
						imgui_TableSetupColumn_Default("Distance");

						imgui_TableHeadersRow();

						List<TableRow_GroupInfo> currentStats = _tableRows_GroupInfo;

						foreach (var stats in currentStats)
						{
							imgui_TableNextRow();

							imgui_TableNextColumn();
							var c = stats.DisplayNameColor;
							imgui_TextColored(c.r, c.g, c.b, 1.0f, stats.DisplayName);

							imgui_TableNextColumn();
							c = stats.HPColor;
							imgui_TextColored(c.r, c.g, c.b, 1.0f, stats.HP);
							imgui_TableNextColumn();
							c = stats.EndColor;
							imgui_TextColored(c.r, c.g, c.b, 1.0f, stats.Endurance);
							imgui_TableNextColumn();
							c = stats.ManaColor;
							imgui_TextColored(c.r, c.g, c.b, 1.0f, stats.Mana);
							imgui_TableNextColumn();

							if (double.TryParse(stats.Distance, out _))
							{
								c = stats.DistanceColor;
								imgui_TextColored(c.r, c.g, c.b, 1.0f, stats.Distance);
							}
							else
							{
								imgui_Text(stats.Distance);
							}

						}
					}
				}

			}

			
		}
		public class TableRow_BuffInfo
		{
			public Spell Spell;
			public string Name;
			public string DisplayName { get; set; }
			public (float r, float g, float b) DisplayNameColor;
			public Int32 iconID;
			public string Duration {  get; set; }
			public string SimpleDuration { get; set; }
			public (float r, float g, float b) DurationColor;

			public string HitCount=String.Empty;
			public (float r, float g, float b) HitCountColor;

			public int SpellType = 0;

			public string CounterType = "";
			public string CounterNumber = String.Empty;

			public TableRow_BuffInfo(string buffName)
			{
				Name = buffName;
			}
		}
		public class TableRow_GroupInfo
		{

			
			public string Name;
			public string DisplayName { get; set; }
			public (float r, float g, float b) DisplayNameColor;
			public string HP { get; set; }
			public (float r, float g, float b) HPColor;

			public string Mana { get; set; }
			public (float r, float g, float b) ManaColor;

			public string Endurance { get; set; }
			public (float r, float g, float b) EndColor;

			public string Distance { get; set; }

			public (float r, float g, float b) DistanceColor;

			public TableRow_GroupInfo()
			{

			}

			public TableRow_GroupInfo(string characterName)
			{
				Name = characterName;
			}
		}
	}
}
