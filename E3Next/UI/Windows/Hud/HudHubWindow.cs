using CommunityToolkit.HighPerformance;
using CommunityToolkit.HighPerformance.Buffers;
using E3Core.Data;
using E3Core.Processors;
using E3Core.Server;
using E3Core.Utility;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using MonoCore;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using static MonoCore.E3ImGUI;
using static System.Windows.Forms.AxHost;

namespace E3Core.UI.Windows.Hud
{
	public static class HudHubWindow
	{
		private static bool _windowInitialized = false;
		private static bool _imguiContextReady = false;

		private static IMQ MQ = E3.MQ;
		private static ISpawns _spawns = E3.Spawns;

		public static HudHubWindowStates _state = new HudHubWindowStates();
		public static Random rand = new Random();

		private const string IMGUI_DETATCH_BUFFS_ID = FontAwesome.FAExternalLinkSquare + "##detach_buffs";
		private const string IMGUI_DETATCH_PETBUFFS_ID = FontAwesome.FAExternalLinkSquare + "##detach_petbuffs";
		private const string IMGUI_DETATCH_SONGS_ID = FontAwesome.FAExternalLinkSquare + "##detach_songs";
		private const string IMGUI_DETATCH_HOTBUTTON_ID = FontAwesome.FAExternalLinkSquare + "##detach_hotbuttons";
		private const string IMGUI_DETATCH_PLAYERINFO_ID = FontAwesome.FAExternalLinkSquare + "##detach_playerinfo";
		private const string IMGUI_DETATCH_TARGETINFO_ID = FontAwesome.FAExternalLinkSquare + "##detach_targetinfo";
		private const string IMGUI_SETTINGS_PLAYERINFO_ID = MaterialFont.settings + "##playerinfo_settings";
		private const string IMGUI_SETTINGS_TARGETINFO_ID = MaterialFont.settings + "##targetinfo_settings";

		private static string IMGUI_TABLE_GROUP_ID = $"E3HubGroupTable-{E3.CurrentName}-{E3.CurrentClass}-{E3.ServerName}";
		private static Dictionary<Int32, String> _percentToString = new Dictionary<int, string>();

		private static string PercentString(Int32 value)
		{
			if (_percentToString.TryGetValue(value, out var returnValue))
			{
				return returnValue;
			}
			else
			{
				string percentString = value + "%";
				_percentToString.Add(value, percentString);
				return percentString;
			}
		}
		private static Dictionary<Decimal, String> _decToIntString = new Dictionary<Decimal, string>();

		private static string DecimalToIntString(Decimal value)
		{
			Decimal tempDec = Math.Round(value, 2);


			if (_decToIntString.TryGetValue(tempDec, out var returnValue))
			{
				return returnValue;
			}
			else
			{
				string intString = tempDec.ToString("N0");
				_decToIntString.Add(tempDec, intString);
				return intString;
			}
		}
		private static float EaseInOutQuad(float t)
		{
			return (float) (t < 0.5f ? 2.0f * t * t : 1.0f - Math.Pow(-2.0f * t + 2.0f, 2.0f) / 2.0f);
		}
		private static float GetBlinkingFactor(float blinkSpeed = 3.0f)
		{
			// 1. Calculate a blinking factor (0.0 to 1.0) using a sine wave
			return (float)(Math.Sin(((float)(Core.StopWatch.ElapsedMilliseconds / 1000)) * blinkSpeed) + 1.0f) * 0.5f;
		}
		//don't copy the structs, just get a reference pointer
		private static void GetBlinkLerpColor(ref Vector4 bg_color, ref Vector4 blink_color, float blinkFactor, ref Vector4 result)
		{
			result.X = bg_color.X + (blink_color.X - bg_color.X) * blinkFactor;
			result.Y = bg_color.Y + (blink_color.Y - bg_color.Y) * blinkFactor;
			result.Z = bg_color.Z + (blink_color.Z - bg_color.Z) * blinkFactor;
		}
		private static readonly (double MinDist, double MaxDist, float R, float G, float B)[] _distanceSeverity = new[]
		{
			(0d, 100d,  0.25f, 0.85f, 0.25f),
			(100d, 200d,0.6f, 0.9f, 0.6f),
			(200d, 300d, 0.95f, 0.85f, 0.35f),
			(300d, 400d, 1.0f, 0.7f, 0.2f),
			(400d, 500d, 1.0f, 0.35f, 0.2f),
			(500d, double.MaxValue, 1.0f, 0.05f, 0.05f)
		};
		private static readonly (double MinDist, double MaxDist, float R, float G, float B)[] _aggroSeverity = new[]
		{
			(0d, 10,  0.25f, 0.85f, 0.25f),
			(10d, 20d,0.6f, 0.9f, 0.6f),
			(20d, 40, 0.95f, 0.85f, 0.35f),
			(40d, 60d, 1.0f, 0.7f, 0.2f),
			(60d, 70d, 1.0f, 0.35f, 0.2f),
			(00d, double.MaxValue, 1.0f, 0.05f, 0.05f)
		};
		private static readonly (double MaxValue, double MinValue, float R, float G, float B)[] _resourceSeverity = new[]
		{
			(200d, 90d,  0.25f, 0.85f, 0.25f),
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
			if (Debugger.IsAttached) return;
			var state = _state.GetState<State_HubWindow>();

			if (Core._MQ2MonoVersion < 0.41m) return;
			E3ImGUI.RegisterWindow(state.WindowName, RenderHub);

			EventProcessor.RegisterCommand("/e3hud_hub", (x) =>
			{
				if (Core._MQ2MonoVersion < 0.41m)
				{
					MQ.Write("This requires MQ2Mono 0.41 or greater");
					return;
				}
				if (x.args.Count > 0)
				{


					//float.TryParse(x.args[0], out state.WindowAlpha);
					//MQ.Write($"Setting alpha to {_windowAlpha}");

				}
				ToggleWindow();
			}, "toggle hub window");
		}
		public static void ToggleWindow()
		{
			try
			{
				var state = _state.GetState<State_HubWindow>();

				if (!_windowInitialized)
				{
					_windowInitialized = true;
					imgui_Begin_OpenFlagSet(state.WindowName, true);
				}
				else
				{
					bool open = imgui_Begin_OpenFlagGet(state.WindowName);
					bool newState = !open;
					imgui_Begin_OpenFlagSet(state.WindowName, newState);
				}
				_imguiContextReady = true;
			}
			catch (Exception ex)
			{
				E3.Log.Write($"Hud Casting Window error: {ex.Message}", Logging.LogLevels.Error);
				_imguiContextReady = false;
			}
		}
		private static (float r, float g, float b, float a) GetAggroSeverityColor(double distance)
		{

			foreach (var band in _aggroSeverity)
			{
				if (distance >= band.MinDist && distance < band.MaxDist)
				{
					return (band.R, band.G, band.B, 1);
				}
			}

			return (0.9f, 0.9f, 0.9f, 1);
		}
		private static (float r, float g, float b, float a) GetDistanceSeverityColor(double distance)
		{

			foreach (var band in _distanceSeverity)
			{
				if (distance >= band.MinDist && distance < band.MaxDist)
				{
					return (band.R, band.G, band.B, 1);
				}
			}

			return (0.9f, 0.9f, 0.9f, 1);
		}
		private static (float r, float g, float b, float a) GetResourceSeverityColor(double resourceValue)
		{

			foreach (var band in _resourceSeverity)
			{
				if (resourceValue <= band.MaxValue && resourceValue > band.MinValue)
				{
					return (band.R, band.G, band.B, 1);
				}
			}

			return (0.9f, 0.9f, 0.9f, 1);
		}
		private static (float r, float g, float b, float a) GetBuffDurationSeverityColor(double duration)
		{

			foreach (var band in _buffDurationSeverity)
			{
				if (duration <= band.MaxValue && duration > band.MinValue)
				{
					return (band.R, band.G, band.B, 1);
				}
			}

			return (0.9f, 0.9f, 0.9f, 1);
		}
		private static (float r, float g, float b, float a) GetConColorRGB(int conColorID)
		{
			switch (conColorID)
			{
				case 0x06: return (0.50f, 0.50f, 0.50f, 1); // GREY
				case 0x02: return (0.10f, 0.85f, 0.10f, 1); // GREEN
				case 0x12: return (0.40f, 0.70f, 1.00f, 1); // LIGHT BLUE
				case 0x04: return (0.20f, 0.40f, 1.00f, 1); // BLUE
				case 0x0a: return (0.95f, 0.95f, 0.95f, 1); // WHITE
				case 0x0f: return (0.95f, 0.85f, 0.10f, 1); // YELLOW
				case 0x0d: return (1.00f, 0.15f, 0.15f, 1); // RED
				default: return (0.90f, 0.90f, 0.90f, 1);
			}
		}

		static Task UpdateTask;

		static string _exceptionMessage = String.Empty;

		private static List<TableRow_BuffInfo> RefreshBuffInfo_ParseProtoBuffData(string s)
		{
			List<TableRow_BuffInfo> returnValue = new List<TableRow_BuffInfo>();
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

				TableRow_BuffInfo buffRow = null;

				if (_personalBuffInfoCache.TryGetValue(spellid, out var tbi))
				{
					buffRow = tbi;
				}
				else
				{
					buffRow = new TableRow_BuffInfo(spellid);
					_personalBuffInfoCache.Add(spellid, buffRow);
				}


				buffRow.SpellID = spellid;
				buffRow.Duration = duration;
				var buffTimeSpan = TimeSpan.FromMilliseconds(duration);
				buffRow.Display_Duration = buffTimeSpan.ToString("h'h 'm'm 's's'");
				buffRow.DurationColor = GetBuffDurationSeverityColor(duration);
				buffRow.SpellType = (Int32)spelltypeid;
				buffRow.BuffType = bufftype;
				buffRow.CounterTypeID = counterType;
				buffRow.CounterNumberValue = counterNumber;
				//check if spellid exists
				if (hitcount > 0)
				{
					buffRow.HitCount = hitcount.ToString();
				}
				else
				{
					buffRow.HitCount = String.Empty;
				}

				returnValue.Add(buffRow);
			}

			return returnValue;
		}

		private static List<TableRow_BuffInfo> RefreshBuffInfo_ParseBuffData(ArraySegment<char> input, Dictionary<int, TableRow_BuffInfo> infoCache)
		{
			ReadOnlySpan<char> s = input;

			int start = 0;
			int end = 0;
			char delim = ':';
			Int32 spellid = 0;
			Int32 duration = 0;
			Int32 hitcount = 0;
			Int32 spelltypeid = 0;
			Int32 bufftype = 0;
			Int32 counterType = -1;
			Int32 counterNumber = -1;
			List<Int64> tempBuffer = new List<Int64>();
			List<TableRow_BuffInfo> dataInfo = new List<TableRow_BuffInfo>();
			foreach (char x in s)
			{
				if (x == delim || end == s.Length - 1)
				{
					if (end == s.Length - 1 && x != delim)
						end++;
					//number,number
					tempBuffer.Clear();
					ReadOnlySpan<char> tstring = s.Slice(start, end - start);
					e3util.StringsToNumbers(tstring, ',', tempBuffer);
					spellid = (Int32)tempBuffer[0];

					duration = (int)tempBuffer[1];
					hitcount = (int)tempBuffer[2];
					spelltypeid = (int)tempBuffer[3];
					bufftype = (int)tempBuffer[4];
					counterType = (int)tempBuffer[5];
					counterNumber = (int)tempBuffer[6];
					TableRow_BuffInfo buffRow = null;

					if (infoCache.TryGetValue(spellid, out var tbi))
					{
						buffRow = tbi;
					}
					else
					{
						buffRow = new TableRow_BuffInfo(spellid);
						infoCache.Add(spellid, buffRow);
					}
					buffRow.BuffType = bufftype;
					buffRow.CounterNumberValue = counterNumber;
					buffRow.SpellID = spellid;
					buffRow.BuffType = bufftype;
					buffRow.CounterTypeID = counterType;
					buffRow.CounterNumberValue = counterNumber;
					buffRow.Duration = duration;
					buffRow.SpellType = (Int32)spelltypeid;
					if (hitcount > 0)
					{
						buffRow.HitCount = hitcount.ToString();
					}
					else
					{
						buffRow.HitCount = String.Empty;
					}
					dataInfo.Add(buffRow);
					start = end + 1;
				}
				end++;
			}
			return dataInfo;
		}

		public static void RefreshPetBuffInfo()
		{

			var buffState = _state.GetState<State_PetBuffWindow>();

			if (!e3util.ShouldCheck(ref buffState.LastUpdated, buffState.LastUpdateInterval)) return;
			{
				buffState.BuffInfo.Clear();
				buffState.DeBuffInfo.Clear();
				string userTouse = E3.CurrentName;
				ShareDataEntry entry = E3.Bots.Query<ShareDataEntry>(userTouse, "${Me.PetBuffInfo}");

				List<TableRow_BuffInfo> dataInfo = null;
				lock (entry)
				{
					if (entry.DataLength > 0)
					{
						//if (E3.CurrentName != userTouse) return;
						buffState.BuffInfo.Clear();
						ArraySegment<char> data = new ArraySegment<char>(entry.Data, 0, entry.DataLength);
						dataInfo = RefreshBuffInfo_ParseBuffData(data,_petBuffInfoCache);
					}
				}
				if (dataInfo != null)
				{
					//using (var RefreshBuffInfoSetData = E3.Log.Trace("RefreshBuffInfoSetData"))
					{
						foreach (var buffRow in dataInfo)
						{
							Int32 spellid = buffRow.SpellID;
							Int32 duration = buffRow.Duration;

							if (!buffState.BuffCache.ContainsKey(spellid))
							{
								string buffName = MQ.Query<string>($"${{Spell[{spellid}].Name}}");
								Int32 spellIcon = MQ.Query<Int32>($"${{Spell[{spellid}].SpellIcon}}");
								Int32 maxDuration = MQ.Query<Int32>($"${{Spell[{spellid}].Duration}}") * 6 * 1000;
								buffState.BuffCache.TryAdd(spellid, new BuffCacheEntry() { Name = buffName, SpellIcon = spellIcon, MaxDuration = maxDuration });
							}


							var cacheEntry = buffState.BuffCache[spellid];

							buffRow.Name = cacheEntry.Name;
							buffRow.iconID = cacheEntry.SpellIcon;
							buffRow.MaxDuration_Value = cacheEntry.MaxDuration;
							var buffTimeSpan = TimeSpan.FromMilliseconds(duration);
							buffRow.Display_Duration = buffTimeSpan.ToString("h'h 'm'm 's's'");
							buffRow.DurationColor = GetBuffDurationSeverityColor(duration);
							if (BuffCheck.BuffInfoCache.ContainsKey(spellid))
							{
								buffRow.Spell = BuffCheck.BuffInfoCache[spellid];
							}
							else
							{
								buffRow.Spell = null;
							}
							if (duration < 0)
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
								buffRow.DisplayName = buffRow.Name + $" ( {buffRow.Duration} )";
							}
							else
							{
								buffRow.DisplayName = buffRow.Name;
							}
							//put them into their proper collections
							
							if (buffRow.SpellType == 0)
							{
								if (buffRow.CounterTypeID == 0) buffRow.CounterType = "Disease";
								else if (buffRow.CounterTypeID == 1) buffRow.CounterType = "Poison";
								else if (buffRow.CounterTypeID == 2) buffRow.CounterType = "Curse";
								else if (buffRow.CounterTypeID == 3) buffRow.CounterType = "Corruption";

								if (buffRow.CounterNumberValue > 0)
								{
									buffRow.Display_CounterNumber = buffRow.CounterNumberValue.ToString();

								}
								buffState.DeBuffInfo.Add(buffRow);
							}
							else
							{
								buffState.BuffInfo.Add(buffRow);
							}
						}
					}

					//using (var RefreshBuffInfoSetData2 = E3.Log.Trace("RefreshBuffInfoSetData2"))
					{
						//okay buff data is all setup, lets put them into the proper collection for the UI to use.
						if (buffState.PreviousBuffs.Count > 0)
						{
							foreach (var buff in buffState.BuffInfo)
							{
								if (!buffState.PreviousBuffs.Contains(buff.SpellID))
								{
									buffState.NewBuffsTimeStamps[buff.SpellID] = Core.StopWatch.ElapsedMilliseconds;
								}
							}
						
						}
						buffState.PreviousBuffs.Clear();
						foreach (var buff in buffState.BuffInfo)
						{
							buffState.PreviousBuffs.Add(buff.SpellID);
						}
					}

				}
			}
		}

		public static void RefreshBuffInfo()
		{
			var buffState = _state.GetState<State_BuffWindow>();

			if (!e3util.ShouldCheck(ref buffState.LastUpdated, buffState.LastUpdateInterval)) return;
			{
				var hubState = _state.GetState<State_HubWindow>();
				var songState = _state.GetState<State_SongWindow>();
				var debuffState = _state.GetState<State_DebuffWindow>();

				string userTouse = E3.CurrentName;

				if (!String.IsNullOrWhiteSpace(hubState.SelectedToonForBuffs))
				{
					userTouse = hubState.SelectedToonForBuffs;
				}

				ShareDataEntry entry = E3.Bots.Query<ShareDataEntry>(userTouse, "${Me.BuffInfo}");

				List<TableRow_BuffInfo> dataInfo = null;
				lock (entry)
				{
					if (entry.DataLength > 0)
					{
						//if (E3.CurrentName != userTouse) return;
						buffState.BuffInfo.Clear();
						songState.SongInfo.Clear();
						debuffState.DebuffInfo.Clear();
						ArraySegment<char> data = new ArraySegment<char>(entry.Data, 0, entry.DataLength);
						dataInfo = RefreshBuffInfo_ParseBuffData(data,_personalBuffInfoCache);
					}
				}
				if (dataInfo != null)
				{
					//using (var RefreshBuffInfoSetData = E3.Log.Trace("RefreshBuffInfoSetData"))
					{
						foreach (var buffRow in dataInfo)
						{
							Int32 spellid = buffRow.SpellID;
							Int32 duration = buffRow.Duration;

							if (!buffState.BuffCache.ContainsKey(spellid))
							{
								string buffName = MQ.Query<string>($"${{Spell[{spellid}].Name}}");
								Int32 spellIcon = MQ.Query<Int32>($"${{Spell[{spellid}].SpellIcon}}");
								Int32 maxDuration = MQ.Query<Int32>($"${{Spell[{spellid}].Duration}}") * 6 * 1000;
								buffState.BuffCache.TryAdd(spellid, new BuffCacheEntry() { Name = buffName, SpellIcon = spellIcon, MaxDuration = maxDuration });
							}


							var cacheEntry = buffState.BuffCache[spellid];

							buffRow.Name = cacheEntry.Name;
							buffRow.iconID = cacheEntry.SpellIcon;
							buffRow.MaxDuration_Value = cacheEntry.MaxDuration;
							var buffTimeSpan = TimeSpan.FromMilliseconds(duration);
							buffRow.Display_Duration = buffTimeSpan.ToString("h'h 'm'm 's's'");
							buffRow.DurationColor = GetBuffDurationSeverityColor(duration);
							if (BuffCheck.BuffInfoCache.ContainsKey(spellid))
							{
								buffRow.Spell = BuffCheck.BuffInfoCache[spellid];
							}
							else
							{
								buffRow.Spell = null;
							}
							if (duration < 0)
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
								buffRow.DisplayName = buffRow.Name + $" ( {buffRow.SimpleDuration} )";
							}
							else
							{
								buffRow.DisplayName = buffRow.Name;
							}
							//put them into their proper collections
							if (buffRow.BuffType == 0) //if normal buff
							{
								if (buffRow.SpellType == 0)
								{
									if (buffRow.CounterTypeID == 0) buffRow.CounterType = "Disease";
									else if (buffRow.CounterTypeID == 1) buffRow.CounterType = "Poison";
									else if (buffRow.CounterTypeID == 2) buffRow.CounterType = "Curse";
									else if (buffRow.CounterTypeID == 3) buffRow.CounterType = "Corruption";

									if (buffRow.CounterNumberValue > 0)
									{
										buffRow.Display_CounterNumber = buffRow.CounterNumberValue.ToString();

									}
									debuffState.DebuffInfo.Add(buffRow);
								}
								else
								{
									buffState.BuffInfo.Add(buffRow);
								}
							}
							else
							{
								//this is a song
								songState.SongInfo.Add(buffRow);
							}
						}
					}

					//using (var RefreshBuffInfoSetData2 = E3.Log.Trace("RefreshBuffInfoSetData2"))
					{

						//okay buff data is all setup, lets put them into the proper collection for the UI to use.

						if (buffState.PreviousBuffs.Count > 0)
						{
							foreach (var buff in buffState.BuffInfo)
							{
								if (!buffState.PreviousBuffs.Contains(buff.SpellID))
								{
									buffState.NewBuffsTimeStamps[buff.SpellID] = Core.StopWatch.ElapsedMilliseconds;
								}
							}
							foreach (var buff in songState.SongInfo)
							{
								if (!buffState.PreviousBuffs.Contains(buff.SpellID))
								{
									buffState.NewBuffsTimeStamps[buff.SpellID] = Core.StopWatch.ElapsedMilliseconds;
								}
							}
							foreach (var buff in debuffState.DebuffInfo)
							{
								if (!buffState.PreviousBuffs.Contains(buff.SpellID))
								{
									buffState.NewBuffsTimeStamps[buff.SpellID] = Core.StopWatch.ElapsedMilliseconds;
								}
							}
						}
						buffState.PreviousBuffs.Clear();
						foreach (var buff in buffState.BuffInfo)
						{
							buffState.PreviousBuffs.Add(buff.SpellID);
						}
						foreach (var buff in songState.SongInfo)
						{
							buffState.PreviousBuffs.Add(buff.SpellID);
						}
						foreach (var buff in debuffState.DebuffInfo)
						{
							buffState.PreviousBuffs.Add(buff.SpellID);
						}
					}

				}
			}

		}
		private static TableRow_GroupInfo RefreshGroupInfo_GetRowDataForPartyMember(string user)
		{
			TableRow_GroupInfo row = null;

			if (_groupInfoCache.TryGetValue(user, out var gi))
			{
				row = gi;
			}
			else
			{
				row = new TableRow_GroupInfo();

			}

			int hp = MQ.Query<Int32>($"${{Group.Member[{user}].PctHPs}}");

			row.PetHPPercent = MQ.Query<Int32>($"${{Group.Member[{user}].Spawn.Pet.CurrentHPs}}"); //user;
			decimal distance = MQ.Query<Decimal>($"${{Group.Member[{user}].Spawn.Distance3D}}");
			row.Distance = DecimalToIntString(distance);


			if (distance == 0)
			{
				row.Distance = "--";

			}
			else
			{
				row.Distance = DecimalToIntString(distance);
				row.DistanceColor = GetDistanceSeverityColor((double)distance);
			}

			row.DisplayName = user;
			row.DisplayNameColor = (0.275f, 0.860f, 0.85f, 1);
			Int32 mana = MQ.Query<Int32>($"${{Group.Member[{user}].PctMana}}");
			if (mana == 0)
			{
				row.Mana = "-";
			}
			else
			{
				row.Mana = PercentString(mana);

			}
			row.ManaColor = GetResourceSeverityColor(mana);

			Int32 endurance = MQ.Query<Int32>($"${{Group.Member[{user}].PctEndurance}}");
			row.Endurance = PercentString(endurance);
			row.EndColor = GetResourceSeverityColor(endurance);
			row.HPPercent = hp;
			row.HP = PercentString(hp);
			row.HPColor = GetResourceSeverityColor(hp);
			Int32 aggroPct = MQ.Query<Int32>($"${{Group.Member[{user}].PctAggro}}");

			if (aggroPct > 0)
			{
				row.AggroPct = aggroPct.ToString();

			}
			else
			{
				row.AggroPct = "-";
			}
			row.AggroColor = GetAggroSeverityColor(aggroPct);
			row.XtargetAggroPct = "-";
			row.XtargetMinAggroPct = "-";
			return row;
		}
		private static TableRow_GroupInfo RefreshGroupInfo_GetRowDataForBot(string user)
		{
			//if (user == E3.CurrentName) continue;

			string casting = E3.Bots.Query<String>(user, "${Me.Casting}");
			double x, y, z = 0;


			Int32 mana, endurance, hp, aggroPct, aggroPctXtarget, aggroPctMinXtarget, pet_pctHealth;
			hp = E3.Bots.Query<Int32>(user, "${Me.PctHPs}");
			mana = E3.Bots.Query<Int32>(user, "${Me.PctMana}");
			endurance = E3.Bots.Query<Int32>(user, "${Me.PctEndurance}");
			aggroPct = E3.Bots.Query<Int32>(user, "${Me.PctAggro}");

			aggroPctXtarget = E3.Bots.Query<Int32>(user, "${Me.XTargetMaxAggro}");
			aggroPctMinXtarget = E3.Bots.Query<Int32>(user, "${Me.XTargetMinAggro}");
			pet_pctHealth = E3.Bots.Query<Int32>(user, "${Me.Pet.CurrentHPs}");

			x = E3.Bots.Query<Double>(user, "${Me.X}");
			y = E3.Bots.Query<Double>(user, "${Me.Y}");
			z = E3.Bots.Query<Double>(user, "${Me.Z}");

			Int32 zoneid = E3.Bots.Query<Int32>(user, "${Me.ZoneID}");
			string zonename = E3.Bots.Query<string>(user, "${Me.ZoneShortName}");
			Decimal distance = 0;
			bool isInvis = false;
			isInvis = E3.Bots.Query<bool>(user, "${Me.IsInvis}");
			bool isInvul = false;
			isInvul = E3.Bots.Query<bool>(user, "${Me.Invulnerable}");

			TableRow_GroupInfo row = null;

			if (_groupInfoCache.TryGetValue(user, out var gi))
			{
				row = gi;
			}
			else
			{
				row = new TableRow_GroupInfo();

			}

			if(row.Name!=String.Empty)
			{
				row.Name = user;
			}

			if (_spawns.TryByName(user, out var spawn, useCurrentCache: true))
			{
				x = E3.Loc_X - x;
				y = E3.Loc_Y - y;
				z = E3.Loc_Z - z;
				//we can calculate distance
				distance = (Decimal)Math.Sqrt(x * x + y * y + z * z);
				row.InZone = true;

			}

			row.PetHPPercent = pet_pctHealth;

			if (distance< 0.5m)
			{
				row.Distance = zonename;

			}
			else
			{
				row.Distance = DecimalToIntString(distance);
				row.DistanceColor = GetDistanceSeverityColor((double)distance);
			}

			if (!row.InZone)
			{
				row.DisplayName = "<" + user + ">";
				row.DisplayNameColor = (0.427f, 0.595f, 0.610f, 1); //grayish
			}
			else if (isInvis)
			{
				row.DisplayName = "(" + user + ")";
				row.DisplayNameColor = (0.427f, 0.595f, 0.610f, 1); //grayish
			}
			else if (isInvul)
			{
				row.DisplayName = "[" + user + "]";
				row.DisplayNameColor = (0.950f, 0.910f, 0.143f, 1); //GOLD
			}
			else
			{
				row.DisplayName = user;
				//row.DisplayNameColor = (0.275f, 0.860f, 0.85f,1);
			}
			if (mana == 0)
			{
				row.Mana = "-";
			}
			else
			{
				row.Mana = PercentString(mana);

			}
			row.ManaColor = GetResourceSeverityColor(mana);


			row.Endurance = PercentString(endurance);
			row.EndColor = GetResourceSeverityColor(endurance);
			row.HPPercent = hp;
			row.HP = PercentString(hp);
			row.HPColor = GetResourceSeverityColor(hp);

			if (aggroPct > 0)
			{
				row.AggroPct = aggroPct.ToString();

			}
			else
			{
				row.AggroPct = "-";
			}
			row.AggroColor = GetAggroSeverityColor(aggroPct);

			if (aggroPctXtarget > 0)
			{
				row.XtargetAggroPct = aggroPctXtarget.ToString();

			}
			else
			{
				row.XtargetAggroPct = "-";
			}
			row.AggroXTargetColor = GetAggroSeverityColor(aggroPctXtarget);
			if (aggroPctMinXtarget > 0)
			{
				row.XtargetMinAggroPct = aggroPctMinXtarget.ToString();

			}
			else
			{
				row.XtargetMinAggroPct = "-";
			}
			row.AggroMinXTargetColor = GetAggroSeverityColor(aggroPctMinXtarget);

			return row;

		}
		private static void RefreshGroupInfo()
		{
			try
			{
				var state = _state.GetState<State_HubWindow>();

				if (!e3util.ShouldCheck(ref state.LastUpdated, state.LastUpdateInterval)) return;
				state.GroupInfo.Clear();
				state.GroupMembersAdded.Clear();

				//get the connected bots.
				List<string> users = E3.Bots.BotsConnected(readOnly: true); //this is a direct cache value, do not edit


				//users.Sort();
				if (state.PeerSortOrder == "Me On Top")
				{
					var row = RefreshGroupInfo_GetRowDataForBot(E3.CurrentName);
					state.GroupInfo.Add(row);
					state.GroupMembersAdded.Add(E3.CurrentName);
				}
				foreach (var user in users)
				{
					if (state.GroupMembersAdded.Contains(user)) continue;

					bool inGroupOrRaid = false;
					if (Basics.GroupMemberNamesLookup.ContainsKey(user)) inGroupOrRaid = true;
					if (!inGroupOrRaid && Basics.RaidMemberNamesLookup.ContainsKey(user)) inGroupOrRaid = true;
					if (!inGroupOrRaid) continue;
					var row = RefreshGroupInfo_GetRowDataForBot(user);
					state.GroupInfo.Add(row);

					if (!state.GroupMembersAdded.Contains(user)) state.GroupMembersAdded.Add(user);
				}
				foreach (var user in Basics.GroupMemberNames)
				{
					if (state.GroupMembersAdded.Contains(user)) continue;
					var row = RefreshGroupInfo_GetRowDataForPartyMember(user);
					state.GroupInfo.Add(row);
				}
			}
			catch (Exception ex)
			{
				MQ.WriteDelayed($"Issue in RefreshGroupInfo. Message:{ex.Message}  Stack:{ex.StackTrace}");

			}




		}
		private static void RefreshPlayerInfo()
		{
			try
			{
				var state = _state.GetState<State_PlayerInfoWindow>();
				if (!e3util.ShouldCheck(ref state.PlayerInfoLastUpdated, state.PlayerInfoUpdateInterval)) return;

				var hub_state = _state.GetState<State_HubWindow>();
				if (!hub_state.ShowPlayerInfo) return;
			
				state.PlayerLevel = MQ.Query<int>("${Me.Level}");
				state.PlayerHPPercent = E3.PctHPs;
				state.PlayerManaPercent = MQ.Query<int>("${Me.PctMana}");
				state.PlayerEndPercent = MQ.Query<int>("${Me.PctEndurance}");
				state.PlayerExp = MQ.Query<Decimal>("${Me.PctExp}");
				state.PlayerAAPoints = MQ.Query<int>("${Me.AAPoints}");
				state.PlayerHPCurrent = MQ.Query<int>("${Me.CurrentHPs}");
				state.PlayerHPMax = MQ.Query<int>("${Me.MaxHPs}");
				state.PlayerManaCurrent = MQ.Query<int>("${Me.CurrentMana}");
				state.PlayerManaMax = MQ.Query<int>("${Me.MaxMana}");
				state.PlayerEndCurrent = MQ.Query<int>("${Me.CurrentEndurance}");
				state.PlayerEndMax = MQ.Query<int>("${Me.MaxEndurance}");



				state.PlayerHPColor = GetResourceSeverityColor(state.PlayerHPCurrent);
				state.PlayerManaColor = GetResourceSeverityColor(state.PlayerManaCurrent);
				state.PlayerEndColor = GetResourceSeverityColor(state.PlayerEndCurrent);



				if (String.IsNullOrEmpty(state.DisplayPlayerInfo) || state.DisplayPlayerInfo_Level != state.PlayerLevel)
				{
					state.DisplayPlayerInfo = $"{E3.CurrentName} - Lvl:{state.PlayerLevel}";
					state.DisplayPlayerInfo_Level = state.PlayerLevel;
				}


				if (state.ShowHPAsPercent)
				{
					state.DisplayHPCurrent = PercentString(state.PlayerHPPercent);
					state.DisplayHPMax = string.Empty;
				}
				else
				{
					state.DisplayHPCurrent = $"{state.PlayerHPCurrent:N0}";
					state.DisplayHPMax = $"{state.PlayerHPMax:N0}";
				}

				if (state.PlayerManaCurrent > 0)
				{
					if (state.ShowManaAsPercent)
					{
						state.DisplayManaCurrent = $"{state.PlayerManaPercent}%";
						state.DisplayManaMax = string.Empty;
					}
					else
					{
						state.DisplayManaCurrent = $"{state.PlayerManaCurrent:N0}";
						state.DisplayManaMax = $"{state.PlayerManaMax:N0}";
					}
				}
				if (state.ShowEndAsPercent)
				{
					state.DisplayEndCurrent = PercentString(state.PlayerEndPercent);
					state.DisplayEndMax = string.Empty;
				}
				else
				{
					state.DisplayEndCurrent = $"{state.PlayerEndCurrent:N0}";
					state.DisplayEndMax = $"{state.PlayerEndMax:N0}";
				}


				state.DisplayExp = $"{state.PlayerExp:F2}%";
				state.DisplayAA = $"({state.PlayerAAPoints})";

				string activeDisc = E3.Bots.Query<string>(E3.CurrentName, "${Me.ActiveDisc}");
				Int32 durationOfDiscInSeconds = E3.Bots.Query<Int32>(E3.CurrentName, "${Me.ActiveDiscTimeLeft}");

				if (!String.IsNullOrWhiteSpace(activeDisc))
				{
					state.ActiveDiscPercentLeft = MQ.Query<Decimal>("${Window[CombatAbilityWnd].Child[CAW_CombatEffectTimeRemainingGauge].Value}") / 10;
				}
				if (String.IsNullOrWhiteSpace(activeDisc))
				{
					state.PreviousDisc = string.Empty;
				}
				else
				{
					string PreviousDisc = state.PreviousDisc;
					if (PreviousDisc != activeDisc)
					{
						state.PreviousDisc = activeDisc;
						state.PreviousDiscTimeStamp = Core.StopWatch.ElapsedMilliseconds;
					}
				}
				state.ActiveDisc = activeDisc;
				if (!String.IsNullOrEmpty(state.PreviousDisc))
				{
					state.Display_ActiveDiscTimeleft = ((((durationOfDiscInSeconds * 1000) + state.PreviousDiscTimeStamp) - Core.StopWatch.ElapsedMilliseconds) / 1000).ToString() + "s";

				}
			}
			catch (Exception ex)
			{
				MQ.WriteDelayed($"Issue in RefreshPlayerInfo. Message:{ex.Message}  Stack:{ex.StackTrace}");
			}
		}
		
		private static void RefreshTargetInfo()
		{
			var state = _state.GetState<State_TargetInfoWindow>();
			var hub_state = _state.GetState<State_HubWindow>();

			if (!hub_state.ShowTargetInfo) return;
			if (!e3util.ShouldCheck(ref state.TargetInfoLastUpdated, state.TargetInfoUpdateInterval)) return;

			Int32 targetID = MQ.Query<Int32>("${Target.ID}");

			if (targetID == 0)
			{
				state.HasTarget = false;
				return;
			}

			state.HasTarget = true;

			if (state.NoTargetTextWidth == 0)
			{
				state.NoTargetTextWidth = imgui_CalcTextSizeX(state.NoTargetText);
			}


			if (_spawns.TryByID(targetID, out var spawn, useCurrentCache: true))
				{

				if (spawn.CleanName != state.TargetName)
				{
					state.PreviousTargetName = state.TargetName;
					state.TargetName = spawn.CleanName;
					state.Display_TargetName = $"{state.TargetName} ({targetID})";
					state.Display_TargetNameSize = imgui_CalcTextSizeX(state.Display_TargetName);
					state.TargetNameSize = imgui_CalcTextSizeX(state.TargetName);
				}



				state.TargetHP = MQ.Query<Int32>("${Target.PctHPs}");

				//if (spawn.Dead) state.TargetHP = 0;


				state.TargetLevel = spawn.Level;
				state.TargetClassName = spawn.ClassShortName;
				state.TargetDistance = spawn.Distance3D;
				state.TargetDistanceString = DecimalToIntString(Math.Round((Decimal)spawn.Distance3D, 2));
				state.TargetNameColor = GetConColorRGB(spawn.ConColorID);
				state.TargetDistanceColor = GetDistanceSeverityColor(spawn.Distance3D);
				state.Display_LevelAndClassString = $"Lvl {spawn.Level} {spawn.ClassShortName}";

				//get my aggro on target
				Decimal percentAggro = MQ.Query<Decimal>("${Target.PctAggro}");



				if (percentAggro > 0) { state.Display_MyAggroPercent = $"{percentAggro}%"; }
				else { state.Display_MyAggroPercent = String.Empty; }

				state.MyAggroPercent = percentAggro;

		

				Decimal percentAggro2nd = MQ.Query<Decimal>("${Target.SecondaryPctAggro}");

				string PersonOn2ndAggro = MQ.Query<String>("${Target.SecondaryAggroPlayer}");
				if (PersonOn2ndAggro == "NULL") PersonOn2ndAggro = String.Empty;


				if (state.SecondAggroName != PersonOn2ndAggro || state.SecondAggroPercent != percentAggro2nd)
				{
					state.SecondAggroPercent = percentAggro2nd;
					state.SecondAggroName = PersonOn2ndAggro;
					if (percentAggro2nd > 0 && !String.IsNullOrWhiteSpace(PersonOn2ndAggro))
					{
						state.Display_SecondAggroName = $"{PersonOn2ndAggro} : {percentAggro2nd}%";
						state.Display_SecondAggroNameSize = imgui_CalcTextSizeX(state.Display_SecondAggroName);
					}
					else
					{
						state.Display_SecondAggroName = String.Empty;
						state.Display_SecondAggroNameSize = 0;
					}
				}
				state.Display_CurrentNameSize = imgui_CalcTextSizeX(E3.CurrentName);
			}

			// Refresh target buffs on a slower cadence, or immediately on target change
			bool targetChanged = (targetID != state.PreviousTargetID);
			if (targetChanged)
			{
				state.PreviousTargetID = targetID;
				state.TargetBuffLastUpdated = 0;
			}

			if (!e3util.ShouldCheck(ref state.TargetBuffLastUpdated, state.TargetBuffUpdateInterval)) return;

			state.TargetBuffs.Clear();

			Int32 buffCount = MQ.Query<Int32>("${Target.BuffCount}");
			int maxBuffs = e3util.MaxTempBuffs;
			if(buffCount>0)
			{
				unsafe
				{
					int length;
					byte* p = E3.MQ.GetTargetBuffDataPtr(targetID, out length);
					Int32 counter = 0;
					if (length > 0)
					{

						ReadOnlySpan<byte> data = new ReadOnlySpan<byte>(p, length);
						int dataStartingLength = data.Length;
						bool buffsPopulated = false;
						if (data.Length > 0)
						{
							//lets pull out the if buffs are being populated
							buffsPopulated = MemoryMarshal.Read<Boolean>(data);
							data = data.Slice(1);
						}
						while (data.Length > 0)
						{
							counter++;
							Int32 spellID = MemoryMarshal.Read<Int32>(data);
							data = data.Slice(4);
							Int32 duration = MemoryMarshal.Read<Int32>(data);
							data = data.Slice(4);
							Int32 spellType = MemoryMarshal.Read<Int32>(data);
							data = data.Slice(4);
							Int32 casterNameLength = MemoryMarshal.Read<Int32>(data);
							data = data.Slice(4);

							duration = duration * 6 * 1000;//change to ms

							string CasterName = string.Empty;

							if (casterNameLength > 0)
							{
								unsafe
								{
									fixed (byte* ptostr = data)
									{
										//pull reused strings, without having to allocate them.
										CasterName = StringPool.Shared.GetOrAdd(data.Slice(0, casterNameLength), Encoding.ASCII);
									}
								}
								data = data.Slice(casterNameLength);
							}

							if (spellID < 1) continue; //blank slot, continue to the next spell

							TableRow_BuffInfo buffRow = null;

							if (_targetBuffInfoCache.TryGetValue(spellID, out var tbi))
							{
								buffRow = tbi;
							}
							else
							{
								buffRow = new TableRow_BuffInfo(spellID);
								_targetBuffInfoCache.Add(spellID, buffRow);

								string spellNameQuery = String.Empty;
								if (!IntToStringIDLookup("Hub_RenderTargetInfo_BuffName", spellID, out spellNameQuery))
								{
									spellNameQuery = $"${{Spell[{spellID}].Name}}";
									IntToStringIDRegister("Hub_RenderTargetInfo_BuffName", spellID, spellNameQuery);
								}
								string buffName = MQ.Query<string>(spellNameQuery);
								buffRow.Name = buffName;

								string targetSpellIcon = String.Empty;
								if (!IntToStringIDLookup("Hub_RenderTargetInfo_BuffIcon", spellID, out targetSpellIcon))
								{
									targetSpellIcon = $"${{Spell[{spellID}].SpellIcon}}";
									IntToStringIDRegister("Hub_RenderTargetInfo_BuffIcon", spellID, targetSpellIcon);
								}
								Int32 spellIcon = MQ.Query<Int32>(targetSpellIcon);

								buffRow.iconID = spellIcon;
								buffRow.SpellType = spellType;

							}
							buffRow.CasterName = CasterName;
							var buffTimeSpan = TimeSpan.FromMilliseconds(duration);//convert from ticks
							buffRow.Display_Duration = buffTimeSpan.ToString("h'h 'm'm 's's'");
							buffRow.DurationColor = GetBuffDurationSeverityColor(duration);

							if (BuffCheck.BuffInfoCache.ContainsKey(spellID))
							{
								buffRow.Spell = BuffCheck.BuffInfoCache[spellID];
							}
							else
							{
								buffRow.Spell = null;
							}

							if (duration < 0)
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
								buffRow.DisplayName = buffRow.Name + $" ( {buffRow.Display_Duration} )";
							}
							else
							{
								buffRow.DisplayName = buffRow.Name;
							}
							if (BuffCheck.BuffInfoCache.TryGetValue(spellID, out var spell))
							{
								buffRow.Spell = spell;
							}
							else
							{
								BuffCheck.BuffCacheLookupQueue.TryAdd(spellID, spellID);
							}
							state.TargetBuffs.Add(buffRow);

						}
					}
					
				}
			}
		}
		private static void RefreshPeerAAInfo()
		{
			var state = _state.GetState<State_PeerAAWindow>();
			if (!state.IsOpen) return;
			if (!e3util.ShouldCheck(ref state.LastUpdated, state.UpdateInterval)) return;

			var hubState = _state.GetState<State_HubWindow>();
			state.PeerAAInfo.Clear();
			List<string> users = E3.Bots.BotsConnected().ToList();
			users.Sort();
			if (hubState.PeerSortOrder == "Me On Top" && users.Remove(E3.CurrentName))
			{
				users.Insert(0, E3.CurrentName);
			}
			foreach (var user in users)
			{
				string aaPoints = E3.Bots.Query<string>(user, "${Me.AAPoints}");
				state.PeerAAInfo.Add((user, aaPoints));
			}
		}
		private static void RenderPeerAAWindow()
		{
			var state = _state.GetState<State_PeerAAWindow>();
			RefreshPeerAAInfo();
			imgui_Text("Peer AA Points");
			imgui_Separator();

			using (var table = ImGUITable.Aquire())
			{
				int tableFlags = (int)(ImGuiTableFlags.ImGuiTableFlags_SizingStretchProp |
									  ImGuiTableFlags.ImGuiTableFlags_BordersInnerH |
									  ImGuiTableFlags.ImGuiTableFlags_RowBg);
				if (table.BeginTable("##PeerAATable", 2, tableFlags, 0f, 0))
				{
					imgui_TableSetupColumn_Default("Name");
					imgui_TableSetupColumn_Default("AA Points");
					imgui_TableHeadersRow();

					foreach (var peer in state.PeerAAInfo)
					{
						imgui_TableNextRow();
						imgui_TableSetColumnIndex(0);
						imgui_TextColored(0.275f, 0.860f, 0.85f, 1.0f, peer.Name);
						imgui_TableSetColumnIndex(1);
						imgui_TextColored(0.95f, 0.85f, 0.35f, 1.0f, peer.AAPoints);
					}
				}
			}
		}
		private static void RenderPlayerInfo()
		{
			var hub_state = _state.GetState<State_HubWindow>();
			var state = _state.GetState<State_PlayerInfoWindow>();
			using (var font = IMGUI_Fonts.Aquire())
			{
				font.PushFont(state.SelectedFont);
				if (!hub_state.ShowPlayerInfo) return;
				if (state.PlayerLevel == 0) return;


				float widthAvail = imgui_GetContentRegionAvailX();

				// Detach/Reattach buttons
				if (!state.Detached)
				{
					imgui_TextColored(0.275f, 0.860f, 0.85f, 1.0f, state.DisplayPlayerInfo);
					imgui_SameLine(0);
					float dockedAvailSpace = imgui_GetContentRegionAvailX();
					float detachButtonWidth = 22;
					float invisWidth = dockedAvailSpace - detachButtonWidth;
					if (invisWidth > 5)
					{
						if (imgui_InvisibleButton("##PlayerInfoDockedInvisButton", invisWidth, 20, (int)ImGuiMouseButton.Right))
						{
						}
						using (var font2 = IMGUI_Fonts.Aquire())
						{
							font2.PushFont("robo");
							using (var popup = ImGUIPopUpContext.Aquire())
							{
								if (popup.BeginPopupContextItem("##PlayerInfoDockedSettingsPopup", 1))
								{
									using (var style = PushStyle.Aquire())
									{
										style.PushStyleColor((int)ImGuiCol.Text, 0.95f, 0.85f, 0.35f, 1.0f);
										imgui_Text("Font");
									}
									using (var combo = ImGUICombo.Aquire())
									{
										if (combo.BeginCombo("##Select Font for Playerinfo Docked", state.SelectedFont))
										{
											foreach (var pair in E3ImGUI.FontList)
											{
												bool sel = string.Equals(state.SelectedFont, pair.Key, StringComparison.OrdinalIgnoreCase);
												if (imgui_Selectable($"{pair.Key}##docked", sel))
												{
													state.SelectedFont = pair.Key;
												}
											}
										}
									}
									imgui_Separator();
									using (var style = PushStyle.Aquire())
									{
										style.PushStyleColor((int)ImGuiCol.Text, 0.95f, 0.85f, 0.35f, 1.0f);
										imgui_Text("Display Mode");
									}
									if (imgui_Checkbox("##docked_show_hp_as_pct", state.ShowHPAsPercent))
										state.ShowHPAsPercent = imgui_Checkbox_Get("##docked_show_hp_as_pct");
									imgui_SameLine(0);
									imgui_Text("HP as %");

									if (imgui_Checkbox("##docked_show_mana_as_pct", state.ShowManaAsPercent))
										state.ShowManaAsPercent = imgui_Checkbox_Get("##docked_show_mana_as_pct");
									imgui_SameLine(0);
									imgui_Text("Mana as %");

									if (imgui_Checkbox("##docked_show_end_as_pct", state.ShowEndAsPercent))
										state.ShowEndAsPercent = imgui_Checkbox_Get("##docked_show_end_as_pct");
									imgui_SameLine(0);
									imgui_Text("End as %");

									if (imgui_Checkbox("##docked_show_progress_bars", state.ShowProgressBars))
										state.ShowProgressBars = imgui_Checkbox_Get("##docked_show_progress_bars");
									imgui_SameLine(0);
									imgui_Text("Progress Bars");
								}
							}
						}
						imgui_SameLine(0, 0);
					}
					else
					{
						imgui_SameLine(widthAvail - detachButtonWidth);
					}
					if (imgui_Button(IMGUI_DETATCH_PLAYERINFO_ID))
					{
						state.Detached = true;
						imgui_Begin_OpenFlagSet(state.WindowName, true);
					}
				}
				else
				{
					imgui_TextColored(0.275f, 0.860f, 0.85f, 1.0f, state.DisplayPlayerInfo);
					if (state.PlayerExp <= 100m)
					{
						imgui_SameLine(0, 10);
						imgui_TextColored(0.95f, 0.70f, 0.50f, 1.0f, "XP:");
						imgui_SameLine(0, 0);
						imgui_TextColored(0.95f, 0.70f, 0.50f, 1.0f, state.DisplayExp);
					}
					imgui_SameLine(0, 10);
					using (var aaStyle = PushStyle.Aquire())
					{
						aaStyle.PushStyleColor((int)ImGuiCol.Button, 0f, 0f, 0f, 0f);
						aaStyle.PushStyleColor((int)ImGuiCol.ButtonHovered, 0.3f, 0.3f, 0.3f, 0.4f);
						aaStyle.PushStyleColor((int)ImGuiCol.ButtonActive, 0.2f, 0.2f, 0.2f, 0.4f);
						aaStyle.PushStyleColor((int)ImGuiCol.Text, 0.95f, 0.85f, 0.35f, 1.0f);
						aaStyle.PushStyleVarVec2((int)ImGuiStyleVar.FramePadding, 0, 0);
						var peerAAState = _state.GetState<State_PeerAAWindow>();
						if (imgui_Button("##PeerAAToggle"))
						{
							peerAAState.IsOpen = !peerAAState.IsOpen;
							imgui_Begin_OpenFlagSet(peerAAState.WindowName, peerAAState.IsOpen);
						}
					}
					float windowWidth = imgui_GetWindowWidth();
					imgui_SameLine(0);
					float availSpace = imgui_GetContentRegionAvailX();
					if (imgui_InvisibleButton("##PlayerInfoSettingsInvisButton", availSpace, 20, (int)ImGuiMouseButton.Right | (int)ImGuiMouseButton.Left))
					{
					}
					using (var font2 = IMGUI_Fonts.Aquire())
					{
						font2.PushFont("robo");
						using (var popup = ImGUIPopUpContext.Aquire())
						{
							if (popup.BeginPopupContextItem("##PlayerInfoSettingsPopup", 1))
							{
								if (imgui_MenuItem("Dock"))
								{
									state.Detached = false;
									imgui_Begin_OpenFlagSet(state.WindowName, false);
								}

								using (var style = PushStyle.Aquire())
								{
									style.PushStyleColor((int)ImGuiCol.Text, 0.95f, 0.85f, 0.35f, 1.0f);
									if (state.Locked)
									{
										if (imgui_MenuItem("UnLock")) state.Locked = false;
									}
									else
									{
										if (imgui_MenuItem("Lock")) state.Locked = true;
									}
								}

								imgui_Separator();
								using (var style = PushStyle.Aquire())
								{
									style.PushStyleColor((int)ImGuiCol.Text, 0.95f, 0.85f, 0.35f, 1.0f);
									imgui_Text("Alpha");
								}
								using (var style = PushStyle.Aquire())
								{
									style.PushStyleColor((int)ImGuiCol.Text, 0.95f, 0.85f, 0.35f, 1.0f);
									imgui_Text("Font");
								}


								using (var combo = ImGUICombo.Aquire())
								{
									if (combo.BeginCombo("##Select Font for Playerinfo", state.SelectedFont))
									{
										foreach (var pair in E3ImGUI.FontList)
										{
											bool sel = string.Equals(state.SelectedFont, pair.Key, StringComparison.OrdinalIgnoreCase);

											if (imgui_Selectable($"{pair.Key}", sel))
											{
												state.SelectedFont = pair.Key;
											}
										}
									}
								}
								imgui_Separator();
								string keyForInput = "##PlayerInfoWindow_alpha_set";
								imgui_SetNextItemWidth(100);
								if (imgui_InputInt(keyForInput, (int)(state.WindowAlpha * 255), 1, 20))
								{
									int updated = imgui_InputInt_Get(keyForInput);
									if (updated > 255) updated = 255;
									if (updated < 0) updated = 0;
									state.WindowAlpha = ((float)updated) / 255f;
								}
								imgui_Separator();
								using (var style = PushStyle.Aquire())
								{
									style.PushStyleColor((int)ImGuiCol.Text, 0.95f, 0.85f, 0.35f, 1.0f);
									imgui_Text("Display Mode");
								}
								if (imgui_Checkbox("##show_hp_as_pct", state.ShowHPAsPercent))
									state.ShowHPAsPercent = imgui_Checkbox_Get("##show_hp_as_pct");
								imgui_SameLine(0);
								imgui_Text("HP as %");

								if (imgui_Checkbox("##show_mana_as_pct", state.ShowManaAsPercent))
									state.ShowManaAsPercent = imgui_Checkbox_Get("##show_mana_as_pct");
								imgui_SameLine(0);
								imgui_Text("Mana as %");

								if (imgui_Checkbox("##show_end_as_pct", state.ShowEndAsPercent))
									state.ShowEndAsPercent = imgui_Checkbox_Get("##show_end_as_pct");
								imgui_SameLine(0);
								imgui_Text("End as %");

								if (imgui_Checkbox("##show_progress_bars", state.ShowProgressBars))
									state.ShowProgressBars = imgui_Checkbox_Get("##show_progress_bars");
								imgui_SameLine(0);
								imgui_Text("Progress Bars");
							}
						}

					}
				}




				float wdithOfWindow = imgui_GetWindowWidth();

				if (!state.ShowProgressBars)
				{
					// Inline text mode: HP, MP, EN all on one line
					var hp = state.PlayerHPColor;
					imgui_Text("HP:");
					imgui_SameLine(0, 2);
					imgui_TextColored(hp.r, hp.g, hp.b, 1.0f, state.DisplayHPCurrent);
					if (!state.ShowHPAsPercent)
					{
						imgui_SameLine(0, 0); imgui_Text("/");
						imgui_SameLine(0, 0);
						imgui_TextColored(0, 1, 0, 1.0f, state.DisplayHPMax);
					}

					if (state.PlayerManaMax > 0)
					{
						var mana = state.PlayerManaColor;
						imgui_SameLine(0, 10);
						imgui_Text("MP:");
						imgui_SameLine(0, 2);
						imgui_TextColored(mana.r, mana.g, mana.b, 1.0f, state.DisplayManaCurrent);
						if (!state.ShowManaAsPercent)
						{
							imgui_SameLine(0, 0); imgui_Text("/");
							imgui_SameLine(0, 0);
							imgui_TextColored(0, 1, 0, 1.0f, state.DisplayManaMax);
						}
					}

					{
						var end = state.PlayerEndColor;
						imgui_SameLine(0, 10);
						imgui_Text("EN:");
						imgui_SameLine(0, 2);
						imgui_TextColored(end.r, end.g, end.b, 1.0f, state.DisplayEndCurrent);
						if (!state.ShowEndAsPercent)
						{
							imgui_SameLine(0, 0); imgui_Text("/");
							imgui_SameLine(0, 0);
							imgui_TextColored(0, 1, 0, 1.0f, state.DisplayEndMax);
						}
					}

					// Disc still uses progress bar
					if (!String.IsNullOrWhiteSpace(state.ActiveDisc))
					{
						using (var style = PushStyle.Aquire())
						{
							style.PushStyleColor((int)ImGuiCol.PlotHistogram, state.DiscProgressBarColor[0], state.DiscProgressBarColor[1], state.DiscProgressBarColor[2], state.DiscProgressBarColor[3]);
							imgui_TextColored(0.95f, 0.85f, 0.35f, 1.0f, state.ActiveDisc);
							using (var push = Push.Aquire())
							{
								push.PushItemWidth(-1);
								imgui_ProgressBar((((float)state.ActiveDiscPercentLeft) / (float)100), 0, 0, $"({state.Display_ActiveDiscTimeleft}) {DecimalToIntString(state.ActiveDiscPercentLeft)}%");
							}
						}
					}
				}
				else
				{
					// Progress bar mode: use table layout
					using (var stylevar = PushStyle.Aquire())
					{

						List<string> columnSections = state.DefaultColumns;
						if (!String.IsNullOrWhiteSpace(state.ActiveDisc))
						{
							columnSections = state.DefaultColumnsWithDisc;
						}

						using (var table = ImGUITable.Aquire())
						{

							Int32 numOfColumns = (int)wdithOfWindow / (int)(imgui_CalcTextSizeX(state.DisplayHPCurrent) + imgui_CalcTextSizeX(state.DisplayHPMax) + imgui_CalcTextSizeX("HP:/"));

							if (numOfColumns < 1) numOfColumns = 1;
							if (numOfColumns > columnSections.Count) numOfColumns = columnSections.Count;

							int flags = (int)ImGuiTableFlags.ImGuiTableFlags_SizingFixedFit;


							table.BeginTable("PlayerInfoTable", numOfColumns, flags, 0, 0);

							for (Int32 i = 0; i < numOfColumns; i++)
							{
								//imgui_TableSetupColumn("", (int)ImGuiTableColumnFlags.ImGuiTableColumnFlags_WidthFixed, 150.0f);
							}

							for (Int32 i = 0; i < columnSections.Count; i++)
							{
								if (i % numOfColumns == 0)
								{
									imgui_TableNextRow();
								}
								imgui_TableNextColumn();
								if (columnSections[i] == "hp")
								{
									var hp = state.PlayerHPColor;
									float startHPLocationX = imgui_GetCursorPosX();
									imgui_Text("HP:");
									imgui_SameLine(0, 2);
									imgui_TextColored(hp.r, hp.g, hp.b, 1.0f, state.DisplayHPCurrent);
									if (!state.ShowHPAsPercent)
									{
										imgui_SameLine(0, 0); imgui_Text("/");
										imgui_SameLine(0, 0);
										imgui_TextColored(0, 1, 0, 1.0f, state.DisplayHPMax);
									}
									float endHPLocationX = imgui_GetCursorPosX();
									using (var style = PushStyle.Aquire())
									{
										style.PushStyleColor((int)ImGuiCol.PlotHistogram, 1, 0, 0, 1); //red

										using (var push = Push.Aquire())
										{
											push.PushItemWidth(-1);
											imgui_ProgressBar((float)state.PlayerHPPercent / 100f, 0, 0, state.PlayerHPPercent.ToString());
										}

									}
								}
								if (columnSections[i] == "resource")
								{
									if (numOfColumns > 1)
									{
										imgui_SetCursorPosY(imgui_GetCursorPosY() - 3);//to deal with a bug in the table with padding on the 2nd clumn
									}
									else
									{
										imgui_SetCursorPosY(imgui_GetCursorPosY() + 5);
									}
									if (state.PlayerManaMax > 0)
									{
										var mana = state.PlayerManaColor;
										imgui_Text("MP:");
										imgui_SameLine(0, 2);
										imgui_TextColored(mana.r, mana.g, mana.b, 1.0f, state.DisplayManaCurrent);
										if (!state.ShowManaAsPercent)
										{
											imgui_SameLine(0, 0); imgui_Text("/");
											imgui_SameLine(0, 0);
											imgui_TextColored(0, 1, 0, 1.0f, state.DisplayManaMax);
										}
										float progressBarStartPosX = imgui_GetCursorPosX();
										float progressBarStartPosY = imgui_GetCursorPosY();
										using (var style = PushStyle.Aquire())
										{
											style.PushStyleColor((int)ImGuiCol.PlotHistogram, 0, 0, 1, 1); //blue
											using (var push = Push.Aquire())
											{
												push.PushItemWidth(-1);

												imgui_ProgressBar((float)state.PlayerManaPercent / 100f, 0, 0, state.PlayerManaPercent.ToString());
											}
										}
										float[] barPos = imgui_GetItemRectMin();
										float[] barSize = imgui_GetItemRectSize();

										if (state.PlayerEndPercent > 0)
										{
											var end2 = state.PlayerEndColor;
											imgui_SameLine(0, 0);
											imgui_SetCursorPosX(progressBarStartPosX);
											imgui_SetCursorPosY(progressBarStartPosY + barSize[1] - 5);
											using (var style = PushStyle.Aquire())
											{
												style.PushStyleColor((int)ImGuiCol.PlotHistogram, state.DiscProgressBarColor[0], state.DiscProgressBarColor[1], state.DiscProgressBarColor[2], 1);
												float widthOfColumn = imgui_GetContentRegionAvailX();
												using (var push = Push.Aquire())
												{
													push.PushItemWidth(-1);

													imgui_ProgressBar(((float)state.PlayerEndPercent / (float)100), 5, 0, "");
												}
											}
										}

									}
									else
									{

										var end = state.PlayerEndColor;
										imgui_Text("EN:");
										imgui_SameLine(0, 2);
										imgui_TextColored(end.r, end.g, end.b, 1.0f, state.DisplayEndCurrent);
										if (!state.ShowEndAsPercent)
										{
											imgui_SameLine(0, 0); imgui_Text("/");
											imgui_SameLine(0, 0);
											imgui_TextColored(0, 1, 0, 1.0f, state.DisplayEndMax);
										}
										using (var style = PushStyle.Aquire())
										{
											style.PushStyleColor((int)ImGuiCol.PlotHistogram, state.DiscProgressBarColor[0], state.DiscProgressBarColor[1], state.DiscProgressBarColor[2], state.DiscProgressBarColor[3]); //Yellow?
											using (var push = Push.Aquire())
											{
												push.PushItemWidth(-1);
												imgui_ProgressBar((float)state.PlayerEndPercent / 100f, 0, 0, state.PlayerEndPercent.ToString());
											}
										}
									}

								}
								if (columnSections[i] == "disc")
								{
									if (numOfColumns == 3)
									{
										imgui_SetCursorPosY(imgui_GetCursorPosY() - 5);

									}
									else
									{
										imgui_SetCursorPosY(imgui_GetCursorPosY() + 5);
									}
									using (var style = PushStyle.Aquire())
									{
										style.PushStyleColor((int)ImGuiCol.PlotHistogram, state.DiscProgressBarColor[0], state.DiscProgressBarColor[1], state.DiscProgressBarColor[2], state.DiscProgressBarColor[3]);
										imgui_TextColored(0.95f, 0.85f, 0.35f, 1.0f, state.ActiveDisc);
										using (var push = Push.Aquire())
										{
											push.PushItemWidth(-1);
											imgui_ProgressBar((((float)state.ActiveDiscPercentLeft) / (float)100), 0, 0, $"({state.Display_ActiveDiscTimeleft}) {DecimalToIntString(state.ActiveDiscPercentLeft)}%");
										}
									}
								}

							}
						}
					}
				}
			}
		}
		private static void RenderTargetInfo()
		{

			var main_state = _state.GetState<State_HubWindow>();
			var state = _state.GetState<State_TargetInfoWindow>();
			var tiState = _state.GetState<State_TargetInfoWindow>();
			if (!main_state.ShowTargetInfo) return;

			// Target Name (con-colored) + detach/reattach
			float widthAvail = imgui_GetContentRegionAvailX();
			var nc = state.TargetNameColor;

			if (!state.HasTarget)
			{
				// No target - render placeholder
				// Center "No Target" text
				float noTargetWidth;


				noTargetWidth = tiState.NoTargetTextWidth;

				float noTargetCenterX = (widthAvail - noTargetWidth) / 2f;
				if (noTargetCenterX < 0) noTargetCenterX = 0;

				if (!tiState.Detached)
				{
					imgui_SetCursorPosX(noTargetCenterX);
					imgui_TextColored(0.5f, 0.5f, 0.5f, 1.0f, tiState.NoTargetText);
					imgui_SameLine(widthAvail - 20);
					if (imgui_Button(IMGUI_DETATCH_TARGETINFO_ID))
					{
						tiState.Detached = true;
						imgui_Begin_OpenFlagSet(tiState.WindowName, true);
					}
				}
				else
				{
					float windowWidth = imgui_GetWindowWidth();
					float contentCenterX = (windowWidth - noTargetWidth) / 2f;
					if (contentCenterX < 0) contentCenterX = 0;
					imgui_SetCursorPosX(contentCenterX);
					imgui_TextColored(0.5f, 0.5f, 0.5f, 1.0f, tiState.NoTargetText);
					imgui_SameLine(0);
					float availSpace = imgui_GetContentRegionAvailX();
					//imgui_SetCursorPosX(windowWidth - 70);
					if (imgui_InvisibleButton("##TargetInfoSettingsInvisButton", availSpace, 20, (int)ImGuiMouseButton.Right | (int)ImGuiMouseButton.Left))
					{
					}
					using (var popup = ImGUIPopUpContext.Aquire())
					{
						if (popup.BeginPopupContextItem("##TargetInfoSettingsPopup", 1))
						{
							if (imgui_MenuItem("Dock"))
							{
								tiState.Detached = false;
								imgui_Begin_OpenFlagSet(tiState.WindowName, false);
							}
							imgui_Separator();
							using (var style = PushStyle.Aquire())
							{
								style.PushStyleColor((int)ImGuiCol.Text, 0.95f, 0.85f, 0.35f, 1.0f);
								if (tiState.Locked)
								{
									if (imgui_MenuItem("UnLock")) tiState.Locked = false;
								}
								else
								{
									if (imgui_MenuItem("Lock")) tiState.Locked = true;
								}

							}

							imgui_Separator();
							using (var style = PushStyle.Aquire())
							{
								style.PushStyleColor((int)ImGuiCol.Text, 0.95f, 0.85f, 0.35f, 1.0f);
								if (tiState.ConColorBorder == 0)
								{
									if (imgui_MenuItem("Con Color: Name Border")) tiState.ConColorBorder = 1;
								}
								else if (tiState.ConColorBorder == 1)
								{
									if (imgui_MenuItem("Con Color: Name+HP Border")) tiState.ConColorBorder = 2;
								}
								else
								{
									if (imgui_MenuItem("Con Color: Text")) tiState.ConColorBorder = 0;
								}
							}

							imgui_Separator();
							using (var style = PushStyle.Aquire())
							{
								style.PushStyleColor((int)ImGuiCol.Text, 0.95f, 0.85f, 0.35f, 1.0f);
								imgui_Text("Alpha");
							}

							string keyForInput = "##TargetInfoWindow_alpha_set";
							imgui_SetNextItemWidth(100);
							if (imgui_InputInt(keyForInput, (int)(tiState.WindowAlpha * 255), 1, 20))
							{
								int updated = imgui_InputInt_Get(keyForInput);
								if (updated > 255) updated = 255;
								if (updated < 0) updated = 0;
								tiState.WindowAlpha = ((float)updated) / 255f;
							}
						}
					}
				}

				// Empty HP bar placeholder
				using (var style = PushStyle.Aquire())
				{
					style.PushStyleColor((int)ImGuiCol.PlotHistogram, 0.3f, 0.3f, 0.3f, 0.5f);
					style.PushStyleColor((int)ImGuiCol.FrameBg, 0.2f, 0.2f, 0.2f, 0.5f);
					imgui_ProgressBar(0f, 18, widthAvail, String.Empty);
				}


				// Reserve space for level/class + distance line and buff row to prevent layout shifting
				// Use invisible text to create spacing
				imgui_Text(" ");
				imgui_Text(" ");
				return;
			}

			// Capture underlay start position before rendering target info
			float underlayStartY = imgui_GetCursorScreenPosY();

			imgui_TextColored(1, 0, 0, 1.0f, state.Display_MyAggroPercent);
			imgui_SameLine(0, 0);
			// Center the target name over the HP bar
			float nameWidth = state.Display_TargetNameSize;
			float centerX = (widthAvail - nameWidth) / 2f;
			if (centerX < 0) centerX = 0;

			if (!tiState.Detached)
			{
				imgui_SetCursorPosX(centerX);
				if (tiState.ConColorBorder > 0)
				{
					imgui_Text(state.Display_TargetName);
				}
				else
				{
					imgui_TextColored(nc.r, nc.g, nc.b, 1.0f, state.Display_TargetName);
				}
				// Reset cursor and draw detach button at the right edge
				imgui_SameLine(widthAvail - 20);
				if (imgui_Button(IMGUI_DETATCH_TARGETINFO_ID))
				{
					tiState.Detached = true;
					imgui_Begin_OpenFlagSet(tiState.WindowName, true);
				}
			}
			else
			{
				float windowWidth = imgui_GetWindowWidth();
				float contentCenterX = (windowWidth - nameWidth) / 2f;
				if (contentCenterX < 0) contentCenterX = 0;
				imgui_SetCursorPosX(contentCenterX);
				if (tiState.ConColorBorder > 0)
				{
					imgui_Text(state.Display_TargetName);
				}
				else
				{
					imgui_TextColored(nc.r, nc.g, nc.b, 1.0f, state.Display_TargetName);
				}
				imgui_SameLine(0);
				float availSpace = imgui_GetContentRegionAvailX();
				if (imgui_InvisibleButton("##TargetInfoSettingsInvisButton", availSpace, 20, (int)ImGuiMouseButton.Right | (int)ImGuiMouseButton.Left))
				{
				}
				using (var popup = ImGUIPopUpContext.Aquire())
				{
					if (popup.BeginPopupContextItem("##TargetInfoSettingsPopup", 1))
					{
						if (imgui_MenuItem("Dock"))
						{
							tiState.Detached = false;
							imgui_Begin_OpenFlagSet(tiState.WindowName, false);
						}
						imgui_Separator();
						using (var style = PushStyle.Aquire())
						{
							style.PushStyleColor((int)ImGuiCol.Text, 0.95f, 0.85f, 0.35f, 1.0f);
							if (tiState.Locked)
							{
								if (imgui_MenuItem("UnLock")) tiState.Locked = false;
							}
							else
							{
								if (imgui_MenuItem("Lock")) tiState.Locked = true;
							}

						}

						imgui_Separator();
						using (var style = PushStyle.Aquire())
						{
							style.PushStyleColor((int)ImGuiCol.Text, 0.95f, 0.85f, 0.35f, 1.0f);
							if (tiState.ConColorBorder == 0)
							{
								if (imgui_MenuItem("Con Color: Name Border")) tiState.ConColorBorder = 1;
							}
							else if (tiState.ConColorBorder == 1)
							{
								if (imgui_MenuItem("Con Color: Name+HP Border")) tiState.ConColorBorder = 2;
							}
							else
							{
								if (imgui_MenuItem("Con Color: Text")) tiState.ConColorBorder = 0;
							}
						}

						imgui_Separator();
						using (var style = PushStyle.Aquire())
						{
							style.PushStyleColor((int)ImGuiCol.Text, 0.95f, 0.85f, 0.35f, 1.0f);
							imgui_Text("Alpha");
						}

						string keyForInput = "##TargetInfoWindow_alpha_set";
						imgui_SetNextItemWidth(100);
						if (imgui_InputInt(keyForInput, (int)(tiState.WindowAlpha * 255), 1, 20))
						{
							int updated = imgui_InputInt_Get(keyForInput);
							if (updated > 255) updated = 255;
							if (updated < 0) updated = 0;
							tiState.WindowAlpha = ((float)updated) / 255f;
						}
					}
				}
			}

			// Draw con color border around name only (mode 1)
			if (tiState.ConColorBorder == 1 && state.HasTarget)
			{
				float pad = 4f;
				float borderTop = underlayStartY - pad;
				float borderEndY = imgui_GetCursorScreenPosY();
				float borderX = imgui_GetWindowPosX() + pad;
				float borderW = imgui_GetWindowWidth() - (pad * 2);
				
				float t = 2f;
				uint borderColor = GetColor(nc.r, nc.g, nc.b, 0.8f);

				imgui_GetWindowDrawList_AddRect(borderX, borderTop, borderX + borderW - 1, borderEndY, borderColor, 0f, 0, 2f);
			
			}

			// Target HP% progress bar
			if (tiState.ConColorBorder > 0 && state.HasTarget)
			{
				imgui_SetCursorPosY(imgui_GetCursorPosY() + 4);
			}

			using (var style = PushStyle.Aquire())
			{
				style.PushStyleColor((int)ImGuiCol.PlotHistogram, 0.8f, 0.15f, 0.15f, 0.9f);
				style.PushStyleColor((int)ImGuiCol.FrameBg, 0.2f, 0.2f, 0.2f, 0.5f);
				imgui_ProgressBar((float)state.TargetHP / 100f, 18, widthAvail, PercentString(state.TargetHP));
			}

			// Draw con color border around name + HP bar (mode 2)
			if (tiState.ConColorBorder == 2 && state.HasTarget)
			{
				float pad = 4f;
				float borderTop = underlayStartY - pad;
				float borderEndY = imgui_GetCursorScreenPosY();
				float borderX = imgui_GetWindowPosX() + pad;
				float borderW = imgui_GetWindowWidth() - (pad * 2);
				float t = 2f;
				uint borderColor = GetColor(nc.r, nc.g, nc.b, 0.8f);

				imgui_GetWindowDrawList_AddRect(borderX, borderTop, borderX + borderW - 1, borderEndY, borderColor, 0f, 0, 2f);

				//imgui_GetWindowDrawList_AddRectFilled(borderX, borderTop, borderX + borderW, borderTop + t, borderColor);
				//imgui_GetWindowDrawList_AddRectFilled(borderX, borderEndY - t, borderX + borderW, borderEndY, borderColor);
				//imgui_GetWindowDrawList_AddRectFilled(borderX, borderTop, borderX + t, borderEndY, borderColor);
				//imgui_GetWindowDrawList_AddRectFilled(borderX + borderW - t, borderTop, borderX + borderW, borderEndY, borderColor);
			}


			// Level & Class (left) + Distance (right)
			if (tiState.ConColorBorder == 2 && state.HasTarget)
			{
				imgui_SetCursorPosY(imgui_GetCursorPosY() + 4);
			}
			string leftText = state.Display_LevelAndClassString;
			imgui_Text(leftText);

			if (!String.IsNullOrEmpty(state.Display_SecondAggroName))
			{
				imgui_SameLine(0, 0);
				float windowWidth = imgui_GetWindowWidth();
				float contentCenterX = (windowWidth - state.Display_SecondAggroNameSize) / 2f;
				if (contentCenterX < 0) contentCenterX = 0;
				imgui_SetCursorPosX(contentCenterX);
				imgui_TextColored(1, 0, 0, 1.0f, state.Display_SecondAggroName);

			}

			if (state.TargetDistance > 0)
			{
				string distText = state.TargetDistanceString;
				float distTextWidth = imgui_CalcTextSizeX(distText);
				float rightAlignX = widthAvail - distTextWidth;
				if (rightAlignX > imgui_CalcTextSizeX(leftText) + 10)
				{
					imgui_SameLine(rightAlignX);
				}
				else
				{
					imgui_SameLine(0);
					imgui_Text("  ");
					imgui_SameLine(0);
				}
				var dc = state.TargetDistanceColor;
				imgui_TextColored(dc.r, dc.g, dc.b, 1.0f, distText);
			}

			// Target Buff Icons
			if (state.TargetBuffs.Count > 0)
			{
				int iconSize = 24;
				int iconsPerRow = Math.Max(1, (int)widthAvail / (iconSize + 2));

				for (int i = 0; i < state.TargetBuffs.Count; i++)
				{
					if (i > 0 && (i % iconsPerRow) != 0)
					{
						imgui_SameLine(0, 2);
					}

					float iconX = imgui_GetCursorScreenPosX();
					float iconY = imgui_GetCursorScreenPosY();
					imgui_DrawSpellIconBySpellID(state.TargetBuffs[i].SpellID, iconSize);

					// Draw buff/debuff border: blue for buffs, red for debuffs
					{
						float bt = 1f;
						uint iconBorderColor = state.TargetBuffs[i].SpellType == 0
							? GetColor(255, 50, 50, 200)   // red for debuffs
							: GetColor(50, 100, 255, 200);  // blue for buffs
						imgui_GetWindowDrawList_AddRectFilled(iconX, iconY, iconX + iconSize, iconY + bt, iconBorderColor);
						imgui_GetWindowDrawList_AddRectFilled(iconX, iconY + iconSize - bt, iconX + iconSize, iconY + iconSize, iconBorderColor);
						imgui_GetWindowDrawList_AddRectFilled(iconX, iconY, iconX + bt, iconY + iconSize, iconBorderColor);
						imgui_GetWindowDrawList_AddRectFilled(iconX + iconSize - bt, iconY, iconX + iconSize, iconY + iconSize, iconBorderColor);
					}

					if (imgui_IsItemHovered())
					{
						using (var tooltip = ImGUIToolTip.Aquire())
						{

							imgui_Text($"Spell: {state.TargetBuffs[i].Name}");
							imgui_Text($"SpellID: {state.TargetBuffs[i].SpellID}");
							imgui_Text($"Duration: {state.TargetBuffs[i].Display_Duration}");
							imgui_Text($"Caster: {state.TargetBuffs[i].CasterName}");
							if (state.TargetBuffs[i].Spell != null && state.TargetBuffs[i].Spell.SpellEffects.Count > 0)
							{
								imgui_Separator();
								foreach (var effect in state.TargetBuffs[i].Spell.SpellEffects)
								{
									if (!string.IsNullOrWhiteSpace(effect))
									{
										imgui_Text(effect);

									}
								}
							}
						}
					}
				}
			}
		}

		private static void RenderHub_MainWindow()
		{
			var state = _state.GetState<State_HubWindow>();
			var buffstate = _state.GetState<State_BuffWindow>();
			var petbuffState = _state.GetState<State_PetBuffWindow>();
			var songstate = _state.GetState<State_SongWindow>();
			var buttonstate = _state.GetState<State_HotbuttonsWindow>();
			var playerInfoState = _state.GetState<State_PlayerInfoWindow>();
			var targetInfoState = _state.GetState<State_TargetInfoWindow>();
			if (imgui_Begin_OpenFlagGet(state.WindowName))
			{
				//E3ImGUI.PushCurrentTheme();
				try
				{
					imgui_SetNextWindowSizeWithCond(400, 300, (int)ImGuiCond.FirstUseEver);
					using (var window = ImGUIWindow.Aquire())
					{
						imgui_SetNextWindowBgAlpha(state.WindowAlpha);

						int flags = (int)ImGuiWindowFlags.ImGuiWindowFlags_NoCollapse | (int)ImGuiWindowFlags.ImGuiWindowFlags_NoTitleBar;

						if (state.Locked)
						{
							flags = flags | (int)ImGuiWindowFlags.ImGuiWindowFlags_NoMove;
						}

						if (window.Begin(state.WindowName, flags))
						{
							if (state.IsDirty || buffstate.IsDirty || songstate.IsDirty || buttonstate.IsDirty || playerInfoState.IsDirty || targetInfoState.IsDirty)
							{
								if (imgui_Button("Save"))
								{
									state.UpdateSettings_WithoutSaving();
									buffstate.UpdateSettings_WithoutSaving();
									songstate.UpdateSettings_WithoutSaving();
									buttonstate.UpdateSettings_WithoutSaving();
									playerInfoState.UpdateSettings_WithoutSaving();
									targetInfoState.UpdateSettings_WithoutSaving();
									E3.CharacterSettings.SaveData();
								}
							}

							if (state.ShowPlayerInfo && !playerInfoState.Detached)
							{
								RenderPlayerInfo();
							}
							if (state.ShowTargetInfo && !targetInfoState.Detached)
							{
								RenderTargetInfo();
							}
							RenderGroupTable();
							if (state.ShowHotButtons && !buttonstate.Detached)
							{
								RenderHotbuttons();
							}
							if (!buffstate.Detached)
							{
								RenderBuffTableSimple();
							}
							if (!songstate.Detached)
							{
								RenderSongTableSimple();
							}
							if(!petbuffState.Detached)
							{
								RenderPetBuffTableSimple();
							}


						}
					}

				}
				finally
				{
					//E3ImGUI.PopCurrentTheme();
				}
			}
		}
		private static void RenderHub_TryDetached(string windowName, bool openFlag, Action ExecuteMethod, float alpha, bool noTitleBar = false, bool locked = false)
		{
			if (openFlag && imgui_Begin_OpenFlagGet(windowName))
			{
				//E3ImGUI.PushCurrentTheme();
				try
				{
					imgui_SetNextWindowSizeWithCond(400, 300, (int)ImGuiCond.FirstUseEver);
					using (var window = ImGUIWindow.Aquire())
					{
						imgui_SetNextWindowBgAlpha(alpha);
						int windowFlags = (int)ImGuiWindowFlags.ImGuiWindowFlags_NoCollapse;

						if (locked) windowFlags = windowFlags | (int)ImGuiWindowFlags.ImGuiWindowFlags_NoMove;

						if (noTitleBar)
						{
							windowFlags |= (int)ImGuiWindowFlags.ImGuiWindowFlags_NoTitleBar;
						}
						if (window.Begin(windowName, windowFlags))
						{
							ExecuteMethod.Invoke();
						}
					}
				}
				finally
				{
					//E3ImGUI.PopCurrentTheme();
				}

			}
		}
		private static void TryReattachWindowsIfClosed()
		{
			var buffstate = _state.GetState<State_BuffWindow>();
			var songstate = _state.GetState<State_SongWindow>();
			var buttonState = _state.GetState<State_HotbuttonsWindow>();
			var playerInfoState = _state.GetState<State_PlayerInfoWindow>();
			var targetInfoState = _state.GetState<State_TargetInfoWindow>();
			if (buffstate.Detached && !imgui_Begin_OpenFlagGet(buffstate.WindowName))
			{
				buffstate.Detached = false;
			}

			if (songstate.Detached && !imgui_Begin_OpenFlagGet(songstate.WindowName))
			{
				songstate.Detached = false;
			}
			if (buttonState.Detached && !imgui_Begin_OpenFlagGet(buttonState.WindowName))
			{
				buttonState.Detached = false;
			}
			if (playerInfoState.Detached && !imgui_Begin_OpenFlagGet(playerInfoState.WindowName))
			{
				playerInfoState.Detached = false;
			}
			if (targetInfoState.Detached && !imgui_Begin_OpenFlagGet(targetInfoState.WindowName))
			{
				targetInfoState.Detached = false;
			}
			var peerAAState = _state.GetState<State_PeerAAWindow>();
			if (peerAAState.IsOpen && !imgui_Begin_OpenFlagGet(peerAAState.WindowName))
			{
				peerAAState.IsOpen = false;
			}
		}
		private static void RenderHub()
		{
			if (!_imguiContextReady) return;

			try
			{
				using (MQ.GetDelayLock())
				{
					var state = _state.GetState<State_HubWindow>();
					if (imgui_Begin_OpenFlagGet(state.WindowName))
					{
						TryReattachWindowsIfClosed();
						var buttonState = _state.GetState<State_HotbuttonsWindow>();


						RefreshGroupInfo();
						RefreshBuffInfo();
						RefreshPetBuffInfo();
						RefreshPlayerInfo();
						RefreshTargetInfo();
						RenderHub_MainWindow();

						var buffState = _state.GetState<State_BuffWindow>();
						RenderHub_TryDetached(buffState.WindowName, buffState.Detached, RenderBuffTableSimple, buffState.WindowAlpha, noTitleBar: true, locked: buffState.Locked);
						var songState = _state.GetState<State_SongWindow>();
						RenderHub_TryDetached(songState.WindowName, songState.Detached, RenderSongTableSimple, songState.WindowAlpha, noTitleBar: true, locked: songState.Locked);
						var petbuffState = _state.GetState<State_PetBuffWindow>();
						RenderHub_TryDetached(petbuffState.WindowName, petbuffState.Detached, RenderPetBuffTableSimple, petbuffState.WindowAlpha, noTitleBar: true, locked: petbuffState.Locked);

						var playerInfoState = _state.GetState<State_PlayerInfoWindow>();
						if (state.ShowPlayerInfo)
						{
							RenderHub_TryDetached(playerInfoState.WindowName, playerInfoState.Detached, RenderPlayerInfo, playerInfoState.WindowAlpha, noTitleBar: true, locked: playerInfoState.Locked);
						}
						var targetInfoState = _state.GetState<State_TargetInfoWindow>();
						if (state.ShowTargetInfo)
						{
							RenderHub_TryDetached(targetInfoState.WindowName, targetInfoState.Detached, RenderTargetInfo, targetInfoState.WindowAlpha, noTitleBar: true, locked: targetInfoState.Locked);
						}
						if (state.ShowHotButtons)
						{
							RenderHub_TryDetached(buttonState.WindowName, buttonState.Detached, RenderHotbuttons, buttonState.WindowAlpha, noTitleBar: true, locked: buttonState.Locked);
						}
						var peerAAState = _state.GetState<State_PeerAAWindow>();
						if (peerAAState.IsOpen)
						{
							RenderHub_TryDetached(peerAAState.WindowName, true, RenderPeerAAWindow, peerAAState.WindowAlpha);
						}
					}
				}
			}
			catch (Exception ex)
			{
				MQ.WriteDelayed($"Error in UI thread: message:{ex.Message} payload:{ex.StackTrace} ");
			}


		}
	
		private static void RenderSongTableSimple()
		{
			var hubState = _state.GetState<State_HubWindow>();
			var state = _state.GetState<State_SongWindow>();
			var buffState = _state.GetState<State_BuffWindow>();
			float widthAvail = imgui_GetContentRegionAvailX();

			Int32 numberOfBuffsPerRow = (int)widthAvail / state.IconSize;

			if (numberOfBuffsPerRow < 1) numberOfBuffsPerRow = 1;


			string songDisplay = String.Empty;
			Int32 keyCount = state.SongInfo.Count ;
			if (!IntToStringIDLookup("Hub_SongDisplay", keyCount, out songDisplay))
			{
				songDisplay = $"Songs: ({keyCount})";
				IntToStringIDRegister("Hub_SongDisplay", keyCount, songDisplay);
			}
			imgui_Text(songDisplay);
			if (!state.Detached)
			{
				imgui_SameLine(0);
				imgui_SetCursorPosX(widthAvail - 20);
				if (imgui_Button(IMGUI_DETATCH_SONGS_ID))
				{
					state.Detached = true;
					imgui_Begin_OpenFlagSet(state.WindowName, true);
				}
			}
			if (state.Detached)
			{
				imgui_SameLine(0);
				float windowWidth = imgui_GetWindowWidth();
				imgui_SameLine(0);
				float availSpace = imgui_GetContentRegionAvailX();
				if (imgui_InvisibleButton("##SongInfoSettingsInvisButton", availSpace, 20, (int)ImGuiMouseButton.Right | (int)ImGuiMouseButton.Left))
				{
				}
				using (var popup = ImGUIPopUpContext.Aquire())
				{
					if (popup.BeginPopupContextItem("##SongWindowSettingsPopup", 1))
					{
						if (imgui_MenuItem("Dock"))
						{
							state.Detached = false;
							imgui_Begin_OpenFlagSet(state.WindowName, false);
						}

						using (var style = PushStyle.Aquire())
						{
							style.PushStyleColor((int)ImGuiCol.Text, 0.95f, 0.85f, 0.35f, 1.0f);
							if (state.Locked)
							{
								if (imgui_MenuItem("UnLock"))
								{
									state.Locked = false;
								}
							}
							else
							{
								if (imgui_MenuItem("Lock"))
								{
									state.Locked = true;
								}
							}
						}

						imgui_Separator();
						using (var style = PushStyle.Aquire())
						{
							style.PushStyleColor((int)ImGuiCol.Text, 0.95f, 0.85f, 0.35f, 1.0f);
							imgui_Text("Alpha");
						}


						string keyForInput = "##songWindow_alpha_set";
						imgui_SetNextItemWidth(100);
						if (imgui_InputInt(keyForInput, (int)(state.WindowAlpha * 255), 1, 20))
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
							state.WindowAlpha = ((float)updated) / 255f;
						}
						imgui_Separator();
						using (var style = PushStyle.Aquire())
						{
							style.PushStyleColor((int)ImGuiCol.Text, 0.95f, 0.85f, 0.35f, 1.0f);
							imgui_Text("Icon Size");

						}
						imgui_SetNextItemWidth(100);
						if (imgui_InputInt("##SongWindow_icon_set", state.IconSize, 1, 20))
						{
							int updated = imgui_InputInt_Get("##SongWindow_icon_set");

							if (updated > 100)
							{
								updated = 100;

							}
							if (updated < 25)
							{
								updated = 25;

							}
							state.IconSize = updated;
						}
						imgui_Separator();
						using (var style = PushStyle.Aquire())
						{
							style.PushStyleColor((int)ImGuiCol.Text, 0.95f, 0.85f, 0.35f, 1.0f);
							imgui_Text("View Mode");
						}

						if (imgui_Checkbox("##song_listview", state.ListView))
						{
							state.ListView = imgui_Checkbox_Get("##song_listview");
						}
						imgui_SameLine(0);
						imgui_Text("List View");

						if (state.ListView)
						{
							imgui_Separator();
							if (imgui_Checkbox("##song_showprogressbars", state.ShowProgressBars))
							{
								state.ShowProgressBars = imgui_Checkbox_Get("##song_showprogressbars");
							}
							imgui_SameLine(0);
							imgui_Text("Show Progress Bars");

							imgui_Separator();
							using (var style = PushStyle.Aquire())
							{
								style.PushStyleColor((int)ImGuiCol.Text, 0.95f, 0.85f, 0.35f, 1.0f);
								imgui_Text("Font");
							}

							using (var combo = ImGUICombo.Aquire())
							{
								if (combo.BeginCombo("##Select Font for SongList", state.SelectedFont))
								{
									foreach (var pair in E3ImGUI.FontList)
									{
										bool sel = string.Equals(state.SelectedFont, pair.Key, StringComparison.OrdinalIgnoreCase);

										if (imgui_Selectable($"{pair.Key}", sel))
										{
											state.SelectedFont = pair.Key;
										}
									}
								}
							}
						}

					}
				}
			}

			// Use List View if enabled, otherwise use icon grid
			if (state.ListView)
			{
				RenderBuffListView(state.SongInfo, "E3HubSongTableListView", state.IconSize, state.FadeRatio, state.FadeTimeInMS, buffState.NewBuffsTimeStamps, state.SelectedFont, state.ShowProgressBars, state.WindowAlpha);
			}
			else
			{
				using (var table = ImGUITable.Aquire())
				{
					int tableFlags = (int)(ImGuiTableFlags.ImGuiTableFlags_SizingFixedFit |
										  ImGuiTableFlags.ImGuiTableFlags_BordersOuter
										  );

					float tableHeight = Math.Max(150f, imgui_GetContentRegionAvailY());
					if (table.BeginTable("E3HubSongTableSimple", 1, tableFlags, 0f, 0))
					{
						imgui_TableSetupColumn_Default("Songs");
						List<TableRow_BuffInfo> currentStats = state.SongInfo;
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
							imgui_DrawSpellIconByIconIndex(stats.iconID, state.IconSize);
							if (buffState.NewBuffsTimeStamps.TryGetValue(stats.SpellID, out var ts))
							{
								Int64 timeDelta = Core.StopWatch.ElapsedMilliseconds - ts;

								long alpha = (Int64)(timeDelta * state.FadeRatio);

								if (alpha > 255) alpha = 255;
								imgui_GetWindowDrawList_AddRectFilled(x, y, x + state.IconSize, y + state.IconSize, GetColor(0, 255, 0, 255 - (uint)alpha));

								if (timeDelta > state.FadeTimeInMS) buffState.NewBuffsTimeStamps.Remove(stats.SpellID);

							}
							using (var popup = ImGUIPopUpContext.Aquire())
							{
								if (popup.BeginPopupContextItemPerf("Hub_RenderSongTableSimpleContext", "##row_selected_context_",counter, 1))
								{
									using (var style = PushStyle.Aquire())
									{
										style.PushStyleColor((int)ImGuiCol.Text, 0.95f, 0.85f, 0.35f, 1.0f);
										imgui_Text(stats.Name);
										imgui_Separator();
										if (imgui_MenuItem("Drop buff"))
										{
											string command = $"/removebuff {stats.Name}";
											if (!String.IsNullOrWhiteSpace(hubState.SelectedToonForBuffs))
											{
												E3.Bots.BroadcastCommandToPerson(hubState.SelectedToonForBuffs, command);
											}
											else
											{
												E3ImGUI.MQCommandQueue.Enqueue(command);
											}
										}
										if (imgui_MenuItem("Drop buff from group"))
										{
											E3ImGUI.MQCommandQueue.Enqueue($"/removebuff {stats.Name}");
											E3.Bots.BroadcastCommandToGroup($"/removebuff {stats.Name}");
										}
										if (imgui_MenuItem("Drop buff from everyone"))
										{
											E3ImGUI.MQCommandQueue.Enqueue($"/removebuff {stats.Name}");
											E3.Bots.BroadcastCommand($"/removebuff {stats.Name}");
										}
									}
								}
							}
							if (stats.SpellType == 0)
							{
								imgui_GetWindowDrawList_AddRectFilled(x, y, x + state.IconSize, y + state.IconSize, GetColor(255, 0, 0, 50));
							}
							if (!String.IsNullOrWhiteSpace(stats.SimpleDuration))
							{
								float newX = x + (float)(state.IconSize / 2) - (state.FontSize);
								float newY = y + (float)((state.IconSize) - (state.FontSize * 2));
								imgui_GetWindowDrawList_AddRectFilled(newX, newY, newX + (state.FontSize * 2), newY + (state.IconSize - (newY - y)), GetColor(0, 0, 0, 100));
								imgui_GetWindowDrawList_AddText(x + (float)(state.IconSize / 2) - (state.FontSize), y + (float)((state.IconSize) - (state.FontSize * 2)), GetColor(255, 255, 255, 255), stats.SimpleDuration);

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
									imgui_Text($"SpellID: {stats.SpellID}");
									imgui_Text($"Duration: {stats.Display_Duration}");
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
		private static void RenderHotbuttons()
		{

			var state = _state.GetState<State_HotbuttonsWindow>();

			float widthAvail = imgui_GetContentRegionAvailX();

			if (state.Detached)
			{
				float windowWidth = imgui_GetWindowWidth();
				imgui_SameLine(0);
				float availSpace = imgui_GetContentRegionAvailX();
				if (imgui_InvisibleButton("##hotbutton_window_settings", availSpace, 10, (int)ImGuiMouseButton.Right | (int)ImGuiMouseButton.Left))
				{
				}
				using (var popup = ImGUIPopUpContext.Aquire())
				{
					if (popup.BeginPopupContextItem("##Hotbutton_WindowSettingsPopup", 1))
					{

						if (imgui_MenuItem("Dock"))
						{
							state.Detached = false;
							imgui_Begin_OpenFlagSet(state.WindowName, false);
						}
						using (var style = PushStyle.Aquire())
						{
							style.PushStyleColor((int)ImGuiCol.Text, 0.95f, 0.85f, 0.35f, 1.0f);
							if (state.Locked)
							{
								if (imgui_MenuItem("UnLock"))
								{
									state.Locked = false;
								}
							}
							else
							{
								if (imgui_MenuItem("Lock"))
								{
									state.Locked = true;
								}
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
							if (combo.BeginCombo("##Select Font for GroupTable", state.SelectedFont))
							{
								foreach (var pair in E3ImGUI.FontList)
								{
									bool sel = string.Equals(state.SelectedFont, pair.Key, StringComparison.OrdinalIgnoreCase);

									if (imgui_Selectable($"{pair.Key}", sel))
									{
										state.SelectedFont = pair.Key;
									}
								}
							}
						}
						imgui_Separator();
						string keyForInput = "##Hotbutton_Window_alpha_set";

						using (var style = PushStyle.Aquire())
						{
							style.PushStyleColor((int)ImGuiCol.Text, 0.95f, 0.85f, 0.35f, 1.0f);
							imgui_Text("Alpha");
							imgui_SetNextItemWidth(100);
							if (imgui_InputInt(keyForInput, (int)(state.WindowAlpha * 255), 1, 20))
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
								state.WindowAlpha = ((float)updated) / 255f;
							}
						}


						using (var style = PushStyle.Aquire())
						{
							style.PushStyleColor((int)ImGuiCol.Text, 0.95f, 0.85f, 0.35f, 1.0f);
							imgui_Text("Button Width");
						}


						keyForInput = "##Hotbutton_Window_buttonX_set";
						imgui_SetNextItemWidth(100);
						if (imgui_InputInt(keyForInput, (int)(state.ButtonSizeX), 1, 5))
						{
							int updated = imgui_InputInt_Get(keyForInput);

							if (updated > 100)
							{
								updated = 100;

							}
							if (updated < 20)
							{
								updated = 20;

							}
							state.ButtonSizeX = updated;
						}
						using (var style = PushStyle.Aquire())
						{
							style.PushStyleColor((int)ImGuiCol.Text, 0.95f, 0.85f, 0.35f, 1.0f);
							imgui_Text("Button Height");

						}


						keyForInput = "##Hotbutton_Window_buttonY_set";
						imgui_SetNextItemWidth(100);
						if (imgui_InputInt(keyForInput, (int)(state.ButtonSizeY), 1, 5))
						{
							int updated = imgui_InputInt_Get(keyForInput);

							if (updated > 100)
							{
								updated = 100;

							}
							if (updated < 20)
							{
								updated = 20;

							}
							state.ButtonSizeY = updated;
						}

					}
				}

			}
			widthAvail = imgui_GetContentRegionAvailX();


			using (var table = ImGUITable.Aquire())
			{
				int tableFlags = (int)(ImGuiTableFlags.ImGuiTableFlags_SizingFixedFit |
									  ImGuiTableFlags.ImGuiTableFlags_BordersOuter
									  );


				Int32 numberOfBuffsPerRow = (int)widthAvail / state.ButtonSizeX;
				if (numberOfBuffsPerRow < 1) numberOfBuffsPerRow = 1;

				if (table.BeginTable("E3HubHotButtonsTable", 1, tableFlags, 0f, 0))
				{
					imgui_TableSetupColumn_Default("Hotbuttons");
					Int32 counter = 0;

					foreach (var stats in E3.CharacterSettings.E3Hud_Hub_HotButtons_DynamicButtons)
					{
						if (counter % numberOfBuffsPerRow == 0)
						{
							imgui_TableNextRow();
							imgui_TableNextColumn();
						}
						else
						{
							imgui_SameLine(0, 5);
						}
						using (var imguiFont = IMGUI_Fonts.Aquire())
						{
							imguiFont.PushFont(state.SelectedFont);
							if (imgui_ButtonEx(stats.Name, state.ButtonSizeX, state.ButtonSizeY))
							{
								E3ImGUI.MQCommandQueue.Enqueue(stats.Command);
							}
						}
						using (var popup = ImGUIPopUpContext.Aquire())
						{
							if (popup.BeginPopupContextItemPerf("Hub_HotButtonDetachPopup", "##Hotbutton_detach-",counter,1))
							{
								using (var style = PushStyle.Aquire())
								{
									style.PushStyleColor((int)ImGuiCol.Text, 0.95f, 0.85f, 0.35f, 1.0f);

									if(state.Detached)
									{
										if (imgui_MenuItem("Dock"))
										{
											state.Detached = false;
											imgui_Begin_OpenFlagSet(state.WindowName, false);
										}
									}
									else if (imgui_MenuItem("Pop Out"))
									{
										state.Detached = true;
										imgui_Begin_OpenFlagSet(state.WindowName, true);
									}
									imgui_Separator();
									using (var style2 = PushStyle.Aquire())
									{
										style2.PushStyleColor((int)ImGuiCol.Text, 0.95f, 0.85f, 0.35f, 1.0f);
										imgui_Text("Font");
									}
									using (var combo = ImGUICombo.Aquire())
									{
										if (combo.BeginCombo("##Select Font for GroupTable", state.SelectedFont))
										{
											foreach (var pair in E3ImGUI.FontList)
											{
												bool sel = string.Equals(state.SelectedFont, pair.Key, StringComparison.OrdinalIgnoreCase);

												if (imgui_Selectable($"{pair.Key}", sel))
												{
													state.SelectedFont = pair.Key;
												}
											}
										}
									}
								}

								imgui_Separator();
								using (var style = PushStyle.Aquire())
								{
									style.PushStyleColor((int)ImGuiCol.Text, 0.95f, 0.85f, 0.35f, 1.0f);
									imgui_Text("Button Width");

								}


								string keyForInput = "##Hotbutton_Window_buttonX_set";
								imgui_SetNextItemWidth(100);
								if (imgui_InputInt(keyForInput, (int)(state.ButtonSizeX), 1, 5))
								{
									int updated = imgui_InputInt_Get(keyForInput);

									if (updated > 100)
									{
										updated = 100;

									}
									if (updated < 20)
									{
										updated = 20;

									}
									state.ButtonSizeX = updated;
									imgui_InputInt_Clear(keyForInput);
								}
								imgui_Separator();
								using (var style = PushStyle.Aquire())
								{
									style.PushStyleColor((int)ImGuiCol.Text, 0.95f, 0.85f, 0.35f, 1.0f);
									imgui_Text("Button Height");

								}


								keyForInput = "##Hotbutton_Window_buttonY_set";
								imgui_SetNextItemWidth(100);
								if (imgui_InputInt(keyForInput, (int)(state.ButtonSizeY), 1, 5))
								{
									int updated = imgui_InputInt_Get(keyForInput);

									if (updated > 100)
									{
										updated = 100;

									}
									if (updated < 20)
									{
										updated = 20;

									}
									state.ButtonSizeY = updated;
									imgui_InputInt_Clear(keyForInput);
								}
							}
						}
						counter++;
					}

				}

			}
		}
		static float[] BuffListView_NameColor = { 1, 1, 1, 1 };
		static Vector4 BuffListView_ProgressColor = new Vector4 { X=0, Y=0, Z=1,W= 0.4f };
		static Vector4 BuffListView_ProgressBarBlinkColor = new Vector4{X=0.8f,Y=0.2f,Z=0.2f,W=0.4f};
		static float[] BuffListView_ProgressBGColor = null;
		private static void RenderBuffListView(List<TableRow_BuffInfo> buffList, string tableName, int iconSize, double fadeRatio, Int32 fadeTimeInMS, Dictionary<Int32, Int64> newBuffsTimeStamps, string selectedFont, bool showProgressBars, float windowAlpha)
		{
			var hubState = _state.GetState<State_HubWindow>();
			int tableFlags = (int)(ImGuiTableFlags.ImGuiTableFlags_Borders | ImGuiTableFlags.ImGuiTableFlags_SizingStretchProp);


			if (BuffListView_ProgressBGColor == null)
			{
				UInt32 packedColor = imgui_GetColorU32((int)ImGuiCol.WindowBg, 1);
				BuffListView_ProgressBGColor = GetRGBAFloatsFromColor(packedColor);
			}

			//remove padding between rows
			using (var stylevar = PushStyle.Aquire())
			{
				stylevar.PushStyleVarVec2((int)ImGuiStyleVar.CellPadding, 0, 0);
				using (var table = ImGUITable.Aquire())
				{
					if (table.BeginTable(tableName, 2, tableFlags, 0f, 0))
					{
						imgui_TableSetupColumn("Icon", (int)ImGuiTableColumnFlags.ImGuiTableColumnFlags_WidthFixed, 22);
						imgui_TableSetupColumn_Default("Name");

						using (var igFont = IMGUI_Fonts.Aquire())
						{
							igFont.PushFont(selectedFont);

							foreach (var stats in buffList)
							{
								imgui_TableNextRow();
								imgui_TableSetColumnIndex(0);

								int smallIconSize = 18;
								imgui_DrawSpellIconByIconIndex(stats.iconID, smallIconSize);
								imgui_TableSetColumnIndex(1);

								// Yellow text for buffs expiring soon (< 5 minutes)
								bool expiringSoon = stats.Duration > 0 && stats.Duration < 300000;

								float textPosX, textPosY;

								if (showProgressBars)
								{
									
									using (var style = PushStyle.Aquire())
									{
										bool show_alternate = (int)(((float)Core.StopWatch.ElapsedMilliseconds/1000f) * 1.0f) % 2 == 0;
										if (stats.Duration<30000 && show_alternate && stats.BuffType!=1) //if not a song
										{
											style.PushStyleColor((int)ImGuiCol.FrameBg, BuffListView_ProgressBarBlinkColor.X, BuffListView_ProgressBarBlinkColor.Y, BuffListView_ProgressBarBlinkColor.Z, windowAlpha);
										}
										else
										{
											style.PushStyleColor((int)ImGuiCol.FrameBg, BuffListView_ProgressBGColor[0], BuffListView_ProgressBGColor[1], BuffListView_ProgressBGColor[2], windowAlpha);


										}
										style.PushStyleColor((int)ImGuiCol.PlotHistogram, BuffListView_ProgressColor.X, BuffListView_ProgressColor.Y, BuffListView_ProgressColor.Z, BuffListView_ProgressColor.W);



										float widthOfColumn = imgui_GetContentRegionAvailX();
										imgui_ProgressBar(((float)stats.Duration / (float)stats.MaxDuration_Value), 20, widthOfColumn, "");
									}

									float[] barPos = imgui_GetItemRectMin();
									float[] barSize = imgui_GetItemRectSize();

									//this centers the text
									//float textPosX = barPos[0] + (barSize[0] - textSize[0]) * 0.5f;
									textPosX = barPos[0];
									textPosY = barPos[1] + (barSize[1] - 20) * 0.5f;
								}
								else
								{
									textPosX = imgui_GetCursorScreenPosX();
									textPosY = imgui_GetCursorScreenPosY();
								}
								imgui_GetWindowDrawList_AddText(textPosX, textPosY, GetColor(BuffListView_NameColor[0], BuffListView_NameColor[1], BuffListView_NameColor[2], BuffListView_NameColor[3]), stats.DisplayName);

								if (expiringSoon)
								{
									using (var style = PushStyle.Aquire())
									{
										style.PushStyleColor((int)ImGuiCol.Text, 0.95f, 0.85f, 0.35f, 1.0f);
										// Draw small inline icon + name (MQ2Switcher style)
										imgui_SameLine(0, 4);
										bool selected = false;
										string selectableKey = String.Empty;
										if(!IntToStringIDLookup("BuffListRemoveSpellContext",stats.SpellID,out selectableKey))
										{
											selectableKey = $"##RemoveSpell_{stats.SpellID}";
											IntToStringIDRegister("BuffListRemoveSpellContext", stats.SpellID, selectableKey);
										}
										if (imgui_Selectable_WithFlags(selectableKey, selected, (int)ImGuiSelectableFlags.ImGuiSelectableFlags_SpanAllColumns))
										{
											// Left-click: remove buff
											string command = $"/removebuff {stats.Name}";
											if (!String.IsNullOrWhiteSpace(hubState.SelectedToonForBuffs))
											{
												E3.Bots.BroadcastCommandToPerson(hubState.SelectedToonForBuffs, command);
											}
											else
											{
												E3ImGUI.MQCommandQueue.Enqueue(command);
											}
										}


									}

								}
								else
								{
									imgui_SameLine(0, 4);
									bool selected = false;
									string selectableKey = String.Empty;
									if (!IntToStringIDLookup("BuffListRemoveSpellContext", stats.SpellID, out selectableKey))
									{
										selectableKey = $"##RemoveSpell_{stats.SpellID}";
										IntToStringIDRegister("BuffListRemoveSpellContext", stats.SpellID, selectableKey);
									}
									if (imgui_Selectable_WithFlags(selectableKey, selected, (int)ImGuiSelectableFlags.ImGuiSelectableFlags_SpanAllColumns))
									{
										// Left-click: remove buff
										string command = $"/removebuff {stats.Name}";
										if (!String.IsNullOrWhiteSpace(hubState.SelectedToonForBuffs))
										{
											E3.Bots.BroadcastCommandToPerson(hubState.SelectedToonForBuffs, command);
										}
										else
										{
											E3ImGUI.MQCommandQueue.Enqueue(command);
										}
									}
								}


								// Hover tooltip with duration
								if (imgui_IsItemHovered())
								{
									using (var tooltip = ImGUIToolTip.Aquire())
									{
										imgui_Text($"Spell: {stats.Name}");
										imgui_Text($"SpellID: {stats.SpellID}");
										imgui_Text($"Duration: {stats.Display_Duration}");
										if (!String.IsNullOrWhiteSpace(stats.CounterType))
										{
											imgui_Text($"CounterType: {stats.CounterType}");
											imgui_Text($"CounterNumber: {stats.Display_CounterNumber}");
										}
										if (stats.Spell != null && stats.Spell.SpellEffects.Count > 0)
										{
											imgui_Separator();
											foreach (var effect in stats.Spell.SpellEffects)
											{
												if (!string.IsNullOrWhiteSpace(effect))
													imgui_Text(effect);
											}
										}
									}
								}

								// Right-click context menu
								using (var popup = ImGUIPopUpContext.Aquire())
								{
									string selectableKey = String.Empty;

									//get the hash of the table name and store it in the
									//upper parts of the int64, and put the spell id in the lower parts
									int tableHash = tableName.GetHashCode();
									Int64 keyToUse = (long)tableHash << 32;
									keyToUse |= (Int64)(uint)stats.SpellID;

									if (!IntToStringIDLookup("BuffListRemovePopupContext", keyToUse, out selectableKey))
									{
										selectableKey = $"{tableName}_Context_{stats.SpellID}";
										IntToStringIDRegister("BuffListRemovePopupContext", keyToUse, selectableKey);
									}
									if (popup.BeginPopupContextItem(selectableKey, 1))
									{
										using (var style = PushStyle.Aquire())
										{
											style.PushStyleColor((int)ImGuiCol.Text, 0.95f, 0.85f, 0.35f, 1.0f);
											imgui_Text(stats.Name);
											imgui_Separator();
											if (imgui_MenuItem("Drop buff"))
											{
												string command = $"/removebuff {stats.Name}";
												if (!String.IsNullOrWhiteSpace(hubState.SelectedToonForBuffs))
												{
													E3.Bots.BroadcastCommandToPerson(hubState.SelectedToonForBuffs, command);
												}
												else
												{
													E3ImGUI.MQCommandQueue.Enqueue(command);
												}
											}
											if (imgui_MenuItem("Drop buff from group"))
											{
												E3ImGUI.MQCommandQueue.Enqueue($"/removebuff {stats.Name}");
												E3.Bots.BroadcastCommandToGroup($"/removebuff {stats.Name}");
											}
											if (imgui_MenuItem("Drop buff from everyone"))
											{
												E3ImGUI.MQCommandQueue.Enqueue($"/removebuff {stats.Name}");
												E3.Bots.BroadcastCommand($"/removebuff {stats.Name}");
											}
										}

										imgui_Separator();
										imgui_Text("Name color picker");
										imgui_SetNextItemWidth(150.0f);
										if (imgui_ColorPicker4_Float("##BuffListView_NameColorPicker", BuffListView_NameColor[0], BuffListView_NameColor[1], BuffListView_NameColor[2], BuffListView_NameColor[3], 0))
										{
											float[] newColors = imgui_ColorPicker_GetRGBA_Float("##BuffListView_NameColorPicker");
											BuffListView_NameColor[0] = newColors[0];
											BuffListView_NameColor[1] = newColors[1];
											BuffListView_NameColor[2] = newColors[2];
											BuffListView_NameColor[3] = newColors[3];

										}

										imgui_Separator();
										imgui_Text("Progress color picker");
										imgui_SetNextItemWidth(150.0f);
										if (imgui_ColorPicker4_Float("##BuffListView_ProgressColorPicker", BuffListView_ProgressColor.X, BuffListView_ProgressColor.Y, BuffListView_ProgressColor.Z, BuffListView_ProgressColor.W, 0))
										{
											float[] newColors = imgui_ColorPicker_GetRGBA_Float("##BuffListView_ProgressColorPicker");
											BuffListView_ProgressColor.X = newColors[0];
											BuffListView_ProgressColor.Y = newColors[1];
											BuffListView_ProgressColor.Z = newColors[2];
											BuffListView_ProgressColor.W = newColors[3];


										}
									}
								}
							}
						}
					}
				}
			}
		}

		private static void RenderPetBuffTableSimple()
		{
			var hubState = _state.GetState<State_HubWindow>();
			var buffState = _state.GetState<State_PetBuffWindow>();
		
			float widthAvail = imgui_GetContentRegionAvailX();

			Int32 numberOfBuffsPerRow = (int)widthAvail / buffState.IconSize;

			if (numberOfBuffsPerRow < 1) numberOfBuffsPerRow = 1;
			if (buffState.DeBuffInfo.Count > 0)
			{
				imgui_Text("Pet Debuffs:");
				using (var table = ImGUITable.Aquire())
				{
					int tableFlags = (int)(ImGuiTableFlags.ImGuiTableFlags_SizingFixedFit |
										  ImGuiTableFlags.ImGuiTableFlags_BordersOuter
										  );

					float tableHeight = Math.Max(150f, imgui_GetContentRegionAvailY());
					if (table.BeginTable("E3HubPetDebuffTableSimple", 1, tableFlags, 0f, 0))
					{
						imgui_TableSetupColumn_Default("Debuffs");
						List<TableRow_BuffInfo> currentStats = buffState.DeBuffInfo;
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
							if (stats.Duration <= 12000) //if not a song
							{
								Int64 timeDelta = Core.StopWatch.ElapsedMilliseconds % 12000;
								long alpha = (Int64)(timeDelta * buffState.FadeRatio);

								imgui_GetWindowDrawList_AddRectFilled(x, y, x + buffState.IconSize, y + buffState.IconSize, GetColor(255, 0, 0, 255 - (uint)alpha));
								imgui_DrawSpellIconByIconIndex(stats.iconID, buffState.IconSize);
							}
							else
							{
								imgui_DrawSpellIconByIconIndex(stats.iconID, buffState.IconSize);
								if (buffState.NewBuffsTimeStamps.TryGetValue(stats.SpellID, out var ts))
								{
									Int64 timeDelta = Core.StopWatch.ElapsedMilliseconds - ts;



									long alpha = (Int64)(timeDelta * buffState.FadeRatio);

									if (alpha > 255) alpha = 255;
									imgui_GetWindowDrawList_AddRectFilled(x, y, x + buffState.IconSize, y + buffState.IconSize, GetColor(255, 0, 0, 255 - (uint)alpha));

									if (timeDelta > buffState.FadeTimeInMS) buffState.NewBuffsTimeStamps.Remove(stats.SpellID);

								}
							}
							
							if (!String.IsNullOrWhiteSpace(stats.SimpleDuration))
							{
								float newX = x + (float)(buffState.IconSize / 2) - (buffState.FontSize);
								float newY = y + (float)((buffState.IconSize) - (buffState.FontSize * 2));

								imgui_GetWindowDrawList_AddText(newX, newY, GetColor(255, 255, 255, 255), stats.SimpleDuration);

							}
							if (!String.IsNullOrWhiteSpace(stats.Display_CounterNumber))
							{
								imgui_GetWindowDrawList_AddText(x, y, GetColor(255, 255, 255, 255), stats.Display_CounterNumber);

							}
							if (imgui_IsItemHovered())
							{
								using (var tooltip = ImGUIToolTip.Aquire())
								{
									imgui_Text($"Spell: {stats.Name}");
									imgui_Text($"SpellID: {stats.SpellID}");
									imgui_Text($"Duration: {stats.Display_Duration}");
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

			if(buffState.BuffInfo.Count>0)
			{
				string buffDisplay = String.Empty;
				Int32 keyCount = buffState.BuffInfo.Count + buffState.DeBuffInfo.Count;
				if (!IntToStringIDLookup("PetBuffDisplay", keyCount, out buffDisplay))
				{
					buffDisplay = $"Pet Buffs: ({buffState.BuffInfo.Count + buffState.DeBuffInfo.Count})";
					IntToStringIDRegister("PetBuffDisplay", keyCount, buffDisplay);
				}
				imgui_Text(buffDisplay);
				if (!String.IsNullOrWhiteSpace(hubState.SelectedToonForBuffs))
				{
					imgui_SameLine(0);
					imgui_Text(hubState.SelectedToonForBuffs);
				}

				if (!buffState.Detached)
				{
					imgui_SameLine(0);
					imgui_SetCursorPosX(widthAvail - 20);
					if (imgui_Button(IMGUI_DETATCH_PETBUFFS_ID))
					{
						buffState.Detached = true;
						imgui_Begin_OpenFlagSet(buffState.WindowName, true);
					}
				}
				if (buffState.Detached)
				{
					imgui_SameLine(0);
					float windowWidth = imgui_GetWindowWidth();
					imgui_SameLine(0);
					float availSpace = imgui_GetContentRegionAvailX();
					if (imgui_InvisibleButton("##PetBuffInfoSettingsInvisButton", availSpace, 20, (int)ImGuiMouseButton.Right | (int)ImGuiMouseButton.Left))
					{
					}
					using (var popup = ImGUIPopUpContext.Aquire())
					{
						if (popup.BeginPopupContextItem("##PetBuffgWindowSettingsPopup", 1))
						{
							if (imgui_MenuItem("Dock"))
							{
								buffState.Detached = false;
								imgui_Begin_OpenFlagSet(buffState.WindowName, false);
							}

							using (var style = PushStyle.Aquire())
							{
								style.PushStyleColor((int)ImGuiCol.Text, 0.95f, 0.85f, 0.35f, 1.0f);
								if (buffState.Locked)
								{
									if (imgui_MenuItem("UnLock"))
									{
										buffState.Locked = false;
									}
								}
								else
								{
									if (imgui_MenuItem("Lock"))
									{
										buffState.Locked = true;
									}
								}
							}

							imgui_Separator();
							using (var style = PushStyle.Aquire())
							{
								style.PushStyleColor((int)ImGuiCol.Text, 0.95f, 0.85f, 0.35f, 1.0f);
								imgui_Text("Alpha");

							}

							string keyForInput = "##PetBuffWindow_alpha_set";
							imgui_SetNextItemWidth(100);
							if (imgui_InputInt(keyForInput, (int)(buffState.WindowAlpha * 255), 1, 20))
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
								buffState.WindowAlpha = ((float)updated) / 255f;
							}
							imgui_Separator();
							using (var style = PushStyle.Aquire())
							{
								style.PushStyleColor((int)ImGuiCol.Text, 0.95f, 0.85f, 0.35f, 1.0f);
								imgui_Text("Icon Size");

							}
							imgui_SetNextItemWidth(100);
							if (imgui_InputInt("##PetBuffWindow_icon_set", buffState.IconSize, 1, 20))
							{
								int updated = imgui_InputInt_Get("##PetBuffWindow_icon_set");

								if (updated > 100)
								{
									updated = 100;

								}
								if (updated < 25)
								{
									updated = 25;

								}
								buffState.IconSize = updated;
							}
							imgui_Separator();
							using (var style = PushStyle.Aquire())
							{
								style.PushStyleColor((int)ImGuiCol.Text, 0.95f, 0.85f, 0.35f, 1.0f);
								imgui_Text("View Mode");
							}

							if (imgui_Checkbox("##buff_listview", buffState.ListView))
							{
								buffState.ListView = imgui_Checkbox_Get("##Petbuff_listview");
							}
							imgui_SameLine(0);
							imgui_Text("List View");

							if (buffState.ListView)
							{
								imgui_Separator();
								if (imgui_Checkbox("##petbuff_showprogressbars", buffState.ShowProgressBars))
								{
									buffState.ShowProgressBars = imgui_Checkbox_Get("##petbuff_showprogressbars");
								}
								imgui_SameLine(0);
								imgui_Text("Show Progress Bars");

								imgui_Separator();
								using (var style = PushStyle.Aquire())
								{
									style.PushStyleColor((int)ImGuiCol.Text, 0.95f, 0.85f, 0.35f, 1.0f);
									imgui_Text("Font");
								}

								using (var combo = ImGUICombo.Aquire())
								{
									if (combo.BeginCombo("##Select Font for PetBuffList", buffState.SelectedFont))
									{
										foreach (var pair in E3ImGUI.FontList)
										{
											bool sel = string.Equals(buffState.SelectedFont, pair.Key, StringComparison.OrdinalIgnoreCase);

											if (imgui_Selectable($"{pair.Key}", sel))
											{
												buffState.SelectedFont = pair.Key;
											}
										}
									}
								}
							}

						}
					}

				}
				// Use List View if enabled, otherwise use icon grid
				if (buffState.ListView)
				{
					if (buffState.DeBuffInfo.Count > 0)
					{
						RenderBuffListView(buffState.DeBuffInfo, "E3HubPetDeBuffTableListView", buffState.IconSize, buffState.FadeRatio, buffState.FadeTimeInMS, buffState.NewBuffsTimeStamps, buffState.SelectedFont, buffState.ShowProgressBars, buffState.WindowAlpha);

					}
					RenderBuffListView(buffState.BuffInfo, "E3HubPetBuffTableListView", buffState.IconSize, buffState.FadeRatio, buffState.FadeTimeInMS, buffState.NewBuffsTimeStamps, buffState.SelectedFont, buffState.ShowProgressBars, buffState.WindowAlpha);
				}
				else
				{
					using (var igFont = IMGUI_Fonts.Aquire())
					{
						igFont.PushFont("arial_bold-20");

						using (var table = ImGUITable.Aquire())
						{
							int tableFlags = (int)(ImGuiTableFlags.ImGuiTableFlags_SizingFixedFit |
												  ImGuiTableFlags.ImGuiTableFlags_BordersOuter
												  );

							if (table.BeginTable("E3HubPetBuffTableSimple", 1, tableFlags, 0f, 0))
							{
								imgui_TableSetupColumn_Default("Buffs");
								List<TableRow_BuffInfo> currentStats = buffState.BuffInfo;
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
									if (stats.Duration <= 12000) //if not a song
									{
										Int64 timeDelta = Core.StopWatch.ElapsedMilliseconds % 12000;
										long alpha = (Int64)(timeDelta * buffState.FadeRatio);

										imgui_GetWindowDrawList_AddRectFilled(x, y, x + buffState.IconSize, y + buffState.IconSize, GetColor(255, 0, 0, 255 - (uint)alpha));
										imgui_DrawSpellIconByIconIndex(stats.iconID, buffState.IconSize);
									}
									else
									{
										imgui_DrawSpellIconByIconIndex(stats.iconID, buffState.IconSize);

										if (buffState.NewBuffsTimeStamps.TryGetValue(stats.SpellID, out var ts))
										{
											Int64 timeDelta = Core.StopWatch.ElapsedMilliseconds - ts;

											long alpha = (Int64)(timeDelta * buffState.FadeRatio);

											if (alpha > 255) alpha = 255;
											imgui_GetWindowDrawList_AddRectFilled(x, y, x + buffState.IconSize, y + buffState.IconSize, GetColor(0, 255, 0, 255 - (uint)alpha));

											if (timeDelta > buffState.FadeTimeInMS) buffState.NewBuffsTimeStamps.Remove(stats.SpellID);

										}

									}
									using (var popup = ImGUIPopUpContext.Aquire())
									{
										if (popup.BeginPopupContextItemPerf("HC_RenderPetBuffTableSimpleIconContext", "PetBuffTableIconContext-", counter, 1))
										{
											using (var style = PushStyle.Aquire())
											{
												style.PushStyleColor((int)ImGuiCol.Text, 0.95f, 0.85f, 0.35f, 1.0f);
												imgui_Text(stats.Name);
												imgui_Separator();
												if (imgui_MenuItem("Drop Pet Buff"))
												{
													string command = $"/removepetbuff {stats.Name}";
													if (!String.IsNullOrWhiteSpace(hubState.SelectedToonForBuffs))
													{
														E3.Bots.BroadcastCommandToPerson(hubState.SelectedToonForBuffs, command);
													}
													else
													{
														E3ImGUI.MQCommandQueue.Enqueue(command);
													}
												}
												if (imgui_MenuItem("Drop Pet buff from group pets"))
												{

													string command = $"/removepetbuff {stats.Name}";

													E3ImGUI.MQCommandQueue.Enqueue(command);
													E3.Bots.BroadcastCommandToGroup(command);
												}
												if (imgui_MenuItem("Drop pet buff from everyone's pet"))
												{
													string command = $"/removepetbuff {stats.Name}";
													E3ImGUI.MQCommandQueue.Enqueue(command);
													E3.Bots.BroadcastCommand(command);
												}
											}

										}
									}
									if (stats.SpellType == 0)
									{
										imgui_GetWindowDrawList_AddRectFilled(x, y, x + buffState.IconSize, y + buffState.IconSize, GetColor(255, 0, 0, 125));
									}
									if (!String.IsNullOrWhiteSpace(stats.SimpleDuration))
									{
										float newX = x + (float)(buffState.IconSize / 2) - (buffState.FontSize);
										float newY = y + (float)((buffState.IconSize) - (buffState.FontSize * 2));
										imgui_GetWindowDrawList_AddRectFilled(newX, newY, newX + (buffState.FontSize * 2), newY + (buffState.IconSize - (newY - y)), GetColor(0, 0, 0, 100));
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
											imgui_Text($"SpellID: {stats.SpellID}");
											imgui_Text($"Duration: {stats.Display_Duration}");

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
			}
			

		}

		private static void RenderBuffTableSimple()
		{
			var hubState = _state.GetState<State_HubWindow>();
			var buffState = _state.GetState<State_BuffWindow>();
			var debuffState = _state.GetState<State_DebuffWindow>();

			float widthAvail = imgui_GetContentRegionAvailX();

			Int32 numberOfBuffsPerRow = (int)widthAvail / buffState.IconSize;

			if (numberOfBuffsPerRow < 1) numberOfBuffsPerRow = 1;
			if (debuffState.DebuffInfo.Count > 0)
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
						List<TableRow_BuffInfo> currentStats = debuffState.DebuffInfo;
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

							if (stats.Duration <= 12000) //if not a song
							{
								Int64 timeDelta = Core.StopWatch.ElapsedMilliseconds % 12000;
								long alpha = (Int64)(timeDelta * buffState.FadeRatio);

								imgui_GetWindowDrawList_AddRectFilled(x, y, x + debuffState.IconSize, y + debuffState.IconSize, GetColor(255, 0, 0, 255 - (uint)alpha));
								imgui_DrawSpellIconByIconIndex(stats.iconID, debuffState.IconSize);
							}
							else
							{
								imgui_DrawSpellIconByIconIndex(stats.iconID, debuffState.IconSize);
								if (buffState.NewBuffsTimeStamps.TryGetValue(stats.SpellID, out var ts))
								{
									Int64 timeDelta = Core.StopWatch.ElapsedMilliseconds - ts;
									long alpha = (Int64)(timeDelta * debuffState.FadeRatio);

									if (alpha > 255) alpha = 255;
									imgui_GetWindowDrawList_AddRectFilled(x, y, x + debuffState.IconSize, y + debuffState.IconSize, GetColor(255, 0, 0, 255 - (uint)alpha));

									if (timeDelta > debuffState.FadeTimeInMS) buffState.NewBuffsTimeStamps.Remove(stats.SpellID);
								}
							}

							if (!String.IsNullOrWhiteSpace(stats.SimpleDuration))
							{
								float newX = x + (float)(debuffState.IconSize / 2) - (debuffState.FontSize);
								float newY = y + (float)((debuffState.IconSize) - (debuffState.FontSize * 2));

								imgui_GetWindowDrawList_AddText(newX, newY, GetColor(255, 255, 255, 255), stats.SimpleDuration);

							}
							if (!String.IsNullOrWhiteSpace(stats.Display_CounterNumber))
							{
								imgui_GetWindowDrawList_AddText(x, y, GetColor(255, 255, 255, 255), stats.Display_CounterNumber);

							}
							if (imgui_IsItemHovered())
							{
								using (var tooltip = ImGUIToolTip.Aquire())
								{
									imgui_Text($"Spell: {stats.Name}");
									imgui_Text($"SpellID: {stats.SpellID}");
									imgui_Text($"Duration: {stats.Display_Duration}");
									if (!String.IsNullOrWhiteSpace(stats.CounterType))
									{
										imgui_Text($"CounterType:{stats.CounterType}");
										imgui_Text($"CounterNumber:{stats.Display_CounterNumber}");
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

		
			string buffDisplay = String.Empty;
			Int32 keyCount = buffState.BuffInfo.Count + debuffState.DebuffInfo.Count;
			if (!IntToStringIDLookup("BuffDisplay",keyCount,out buffDisplay))
			{
				buffDisplay = $"Buffs: ({buffState.BuffInfo.Count + debuffState.DebuffInfo.Count})";
				IntToStringIDRegister("BuffDisplay", keyCount, buffDisplay);
			}
			imgui_Text(buffDisplay);
			if (!String.IsNullOrWhiteSpace(hubState.SelectedToonForBuffs))
			{
				imgui_SameLine(0);
				imgui_Text(hubState.SelectedToonForBuffs);
			}

			if (!buffState.Detached)
			{
				imgui_SameLine(0);
				imgui_SetCursorPosX(widthAvail - 20);
				if (imgui_Button(IMGUI_DETATCH_BUFFS_ID))
				{
					buffState.Detached = true;
					imgui_Begin_OpenFlagSet(buffState.WindowName, true);
				}
			}
			if (buffState.Detached)
			{
				imgui_SameLine(0);
				float windowWidth = imgui_GetWindowWidth();
				imgui_SameLine(0);
				float availSpace = imgui_GetContentRegionAvailX();
				if (imgui_InvisibleButton("##BuffInfoSettingsInvisButton", availSpace, 20, (int)ImGuiMouseButton.Right | (int)ImGuiMouseButton.Left))
				{
				}
				using (var popup = ImGUIPopUpContext.Aquire())
				{
					if (popup.BeginPopupContextItem("##BuffgWindowSettingsPopup", 1))
					{
						if (imgui_MenuItem("Dock"))
						{
							buffState.Detached = false;
							imgui_Begin_OpenFlagSet(buffState.WindowName, false);
						}

						using (var style = PushStyle.Aquire())
						{
							style.PushStyleColor((int)ImGuiCol.Text, 0.95f, 0.85f, 0.35f, 1.0f);
							if (buffState.Locked)
							{
								if (imgui_MenuItem("UnLock"))
								{
									buffState.Locked = false;
								}
							}
							else
							{
								if (imgui_MenuItem("Lock"))
								{
									buffState.Locked = true;
								}
							}
						}

						imgui_Separator();
						using (var style = PushStyle.Aquire())
						{
							style.PushStyleColor((int)ImGuiCol.Text, 0.95f, 0.85f, 0.35f, 1.0f);
							imgui_Text("Alpha");

						}

						string keyForInput = "##BuffWindow_alpha_set";
						imgui_SetNextItemWidth(100);
						if (imgui_InputInt(keyForInput, (int)(buffState.WindowAlpha * 255), 1, 20))
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
							buffState.WindowAlpha = ((float)updated) / 255f;
						}
						imgui_Separator();
						using (var style = PushStyle.Aquire())
						{
							style.PushStyleColor((int)ImGuiCol.Text, 0.95f, 0.85f, 0.35f, 1.0f);
							imgui_Text("Icon Size");

						}
						imgui_SetNextItemWidth(100);
						if (imgui_InputInt("##BuffWindow_icon_set", buffState.IconSize, 1, 20))
						{
							int updated = imgui_InputInt_Get("##BuffWindow_icon_set");

							if (updated > 100)
							{
								updated = 100;

							}
							if (updated < 25)
							{
								updated = 25;

							}
							buffState.IconSize = updated;
						}
						imgui_Separator();
						using (var style = PushStyle.Aquire())
						{
							style.PushStyleColor((int)ImGuiCol.Text, 0.95f, 0.85f, 0.35f, 1.0f);
							imgui_Text("View Mode");
						}

						if (imgui_Checkbox("##buff_listview", buffState.ListView))
						{
							buffState.ListView = imgui_Checkbox_Get("##buff_listview");
						}
						imgui_SameLine(0);
						imgui_Text("List View");

						if (buffState.ListView)
						{
							imgui_Separator();
							if (imgui_Checkbox("##buff_showprogressbars", buffState.ShowProgressBars))
							{
								buffState.ShowProgressBars = imgui_Checkbox_Get("##buff_showprogressbars");
							}
							imgui_SameLine(0);
							imgui_Text("Show Progress Bars");

							imgui_Separator();
							using (var style = PushStyle.Aquire())
							{
								style.PushStyleColor((int)ImGuiCol.Text, 0.95f, 0.85f, 0.35f, 1.0f);
								imgui_Text("Font");
							}

							using (var combo = ImGUICombo.Aquire())
							{
								if (combo.BeginCombo("##Select Font for BuffList", buffState.SelectedFont))
								{
									foreach (var pair in E3ImGUI.FontList)
									{
										bool sel = string.Equals(buffState.SelectedFont, pair.Key, StringComparison.OrdinalIgnoreCase);

										if (imgui_Selectable($"{pair.Key}", sel))
										{
											buffState.SelectedFont = pair.Key;
										}
									}
								}
							}
						}

					}
				}

			}
			// Use List View if enabled, otherwise use icon grid
			if (buffState.ListView)
			{
				RenderBuffListView(buffState.BuffInfo, "E3HubBuffTableListView", buffState.IconSize, buffState.FadeRatio, buffState.FadeTimeInMS, buffState.NewBuffsTimeStamps, buffState.SelectedFont, buffState.ShowProgressBars, buffState.WindowAlpha);
			}
			else
			{
				using (var igFont = IMGUI_Fonts.Aquire())
				{
					//igFont.PushFont("EQ-Bold");

					using (var table = ImGUITable.Aquire())
					{
						int tableFlags = (int)(ImGuiTableFlags.ImGuiTableFlags_SizingFixedFit |
											  ImGuiTableFlags.ImGuiTableFlags_BordersOuter
											  );

						if (table.BeginTable("E3HubBuffTableSimple", 1, tableFlags, 0f, 0))
						{
							imgui_TableSetupColumn_Default("Buffs");
							List<TableRow_BuffInfo> currentStats = buffState.BuffInfo;
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

								///BLINKING THAT ITS GOING TO DROP OFF
								//bool show_alternate = true;//(int)(((float)Core.StopWatch.ElapsedMilliseconds / 1000f) * 1.0f) % 2 == 0;
								if (stats.Duration <= 12000) //if not a song
								{
									Int64 timeDelta = Core.StopWatch.ElapsedMilliseconds %12000;
									long alpha = (Int64)(timeDelta * buffState.FadeRatio);

									imgui_GetWindowDrawList_AddRectFilled(x, y, x + buffState.IconSize, y + buffState.IconSize, GetColor(255, 0, 0, 255 - (uint)alpha));
									imgui_DrawSpellIconByIconIndex(stats.iconID, buffState.IconSize);
								}
								else
								{
									imgui_DrawSpellIconByIconIndex(stats.iconID, buffState.IconSize);

									if (buffState.NewBuffsTimeStamps.TryGetValue(stats.SpellID, out var ts))
									{
										Int64 timeDelta = Core.StopWatch.ElapsedMilliseconds - ts;

										long alpha = (Int64)(timeDelta * buffState.FadeRatio);

										if (alpha > 255) alpha = 255;
										imgui_GetWindowDrawList_AddRectFilled(x, y, x + buffState.IconSize, y + buffState.IconSize, GetColor(0, 255, 0, 255 - (uint)alpha));

										if (timeDelta > buffState.FadeTimeInMS) buffState.NewBuffsTimeStamps.Remove(stats.SpellID);

									}
								}

								
								using (var popup = ImGUIPopUpContext.Aquire())
								{
									if (popup.BeginPopupContextItemPerf("HC_RenderBuffTableSimpleIconContext", "BuffTableIconContext-",counter,1))
									{
										using (var style = PushStyle.Aquire())
										{
											style.PushStyleColor((int)ImGuiCol.Text, 0.95f, 0.85f, 0.35f, 1.0f);
											imgui_Text(stats.Name);
											imgui_Separator();
											if (imgui_MenuItem("Drop buff"))
											{
												string command = $"/removebuff {stats.Name}";
												if (!String.IsNullOrWhiteSpace(hubState.SelectedToonForBuffs))
												{
													E3.Bots.BroadcastCommandToPerson(hubState.SelectedToonForBuffs, command);
												}
												else
												{
													E3ImGUI.MQCommandQueue.Enqueue(command);
												}
											}
											if (imgui_MenuItem("Drop buff from group"))
											{

												string command = $"/removebuff {stats.Name}";

												E3ImGUI.MQCommandQueue.Enqueue(command);
												E3.Bots.BroadcastCommandToGroup(command);
											}
											if (imgui_MenuItem("Drop buff from everyone"))
											{
												string command = $"/removebuff {stats.Name}";
												E3ImGUI.MQCommandQueue.Enqueue(command);
												E3.Bots.BroadcastCommand(command);
											}
										}

									}
								}
								if (stats.SpellType == 0)
								{
									imgui_GetWindowDrawList_AddRectFilled(x, y, x + buffState.IconSize, y + buffState.IconSize, GetColor(255, 0, 0, 125));
								}
								if (!String.IsNullOrWhiteSpace(stats.SimpleDuration))
								{
									float newX = x + (float)(buffState.IconSize / 2) - (buffState.FontSize);
									float newY = y + (float)((buffState.IconSize) - (buffState.FontSize * 2));
									imgui_GetWindowDrawList_AddRectFilled(newX, newY, newX + (buffState.FontSize * 2), newY + (buffState.IconSize - (newY - y)), GetColor(0, 0, 0, 100));
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
										imgui_Text($"SpellID: {stats.SpellID}");
										imgui_Text($"Duration: {stats.Display_Duration}");

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

		}
		private static void RenderGroupTable()
		{

			var state = _state.GetState<State_HubWindow>();

			float widthOfWindow = imgui_GetContentRegionAvailX();

			if (state.ShowTickTimer)
			{
				//Int64 metronome = Core.StopWatch.ElapsedMilliseconds - Alerts.LastTickSeen;
				//imgui_ProgressBar(((float)(metronome%6000)/(float)6000), 20, (int)widthOfWindow, String.Empty);
			}

			using (var table = ImGUITable.Aquire())
			{
				int tableFlags = (int)(ImGuiTableFlags.ImGuiTableFlags_Resizable |
									  ImGuiTableFlags.ImGuiTableFlags_BordersOuter |
									  ImGuiTableFlags.ImGuiTableFlags_BordersInnerV |
									  ImGuiTableFlags.ImGuiTableFlags_RowBg | ImGuiTableFlags.ImGuiTableFlags_Reorderable | ImGuiTableFlags.ImGuiTableFlags_NoPadInnerX
									  );

				state.ColumNameBuffer.Clear();
				state.ColumNameBuffer.Add("Name");
				// Calculate visible column count (Name is always visible)
				int columnCount = 1;

				if (state.ShowColumnAggro) { columnCount++; state.ColumNameBuffer.Add("A"); }
				if (state.ShowColumnHP) { columnCount++; state.ColumNameBuffer.Add("HP"); }
				if (state.ShowColumnEnd) { columnCount++; state.ColumNameBuffer.Add("End"); }
				if (state.ShowColumnMana) { columnCount++; state.ColumNameBuffer.Add("Mana"); }
				if (state.ShowColumnDistance) { columnCount++; state.ColumNameBuffer.Add("Dist"); }
				if (state.ShowColumnAggroXTarget) { columnCount++; state.ColumNameBuffer.Add("AX"); }
				if (state.ShowColumnAggroMinXTarget) { columnCount++; state.ColumNameBuffer.Add("AMX"); }
				using (var stylevar = PushStyle.Aquire())
				{
					stylevar.PushStyleVarVec2((int)ImGuiStyleVar.CellPadding, 0, 0);
					stylevar.PushStyleVarVec2((int)ImGuiStyleVar.FramePadding, 0, 0);

					if (table.BeginTable(IMGUI_TABLE_GROUP_ID, columnCount, tableFlags, 0f, 0))
					{

						for (Int32 i = 0; i < columnCount; i++)
						{
							imgui_TableSetupColumn_Default(state.ColumNameBuffer[i]);
						}

						for (Int32 i = 0; i < columnCount; i++)
						{
							imgui_TableNextColumn();
							imgui_TableHeader(state.ColumNameBuffer[i]);

							using (var popup = ImGUIPopUpContext.Aquire())
							{
								int flags = 1;
								if (popup.BeginPopupContextItemPerf("Hud_GroupTableHeaderContext", "##GroupTableHeaderContext-",i, flags))
								{
									using (var style = PushStyle.Aquire())
									{
										style.PushStyleColor((int)ImGuiCol.Text, 0.95f, 0.85f, 0.35f, 1.0f);
										if (state.Locked)
										{
											if (imgui_MenuItem("UnLock"))
											{
												state.Locked = false;
											}
										}
										else
										{
											if (imgui_MenuItem("Lock"))
											{
												state.Locked = true;
											}
										}

									}

									imgui_Separator();
									using (var style = PushStyle.Aquire())
									{
										style.PushStyleColor((int)ImGuiCol.Text, 0.95f, 0.85f, 0.35f, 1.0f);
										imgui_Text("Show Columns");

									}

									imgui_Separator();

									if (imgui_Checkbox("##col_hp", state.ShowColumnHP))
										state.ShowColumnHP = imgui_Checkbox_Get("##col_hp");
									imgui_SameLine(0);
									imgui_Text("HP");
									if (imgui_Checkbox("##col_hp_bar", state.DisplayHPBar))
										state.DisplayHPBar = imgui_Checkbox_Get("##col_hp_bar");
									imgui_SameLine(0);
									imgui_Text("Show HP Bar where Name is");

									if (imgui_Checkbox("##col_end", state.ShowColumnEnd))
										state.ShowColumnEnd = imgui_Checkbox_Get("##col_end");
									imgui_SameLine(0);
									imgui_Text("Endurance");

									if (imgui_Checkbox("##col_mana", state.ShowColumnMana))
										state.ShowColumnMana = imgui_Checkbox_Get("##col_mana");
									imgui_SameLine(0);
									imgui_Text("Mana");

									if (imgui_Checkbox("##col_dist", state.ShowColumnDistance))
										state.ShowColumnDistance = imgui_Checkbox_Get("##col_dist");
									imgui_SameLine(0);
									imgui_Text("Distance");
									if (imgui_Checkbox("##col_aggro", state.ShowColumnAggro))
										state.ShowColumnAggro = imgui_Checkbox_Get("##col_aggro");
									imgui_SameLine(0);
									imgui_Text("Aggro");

									if (imgui_Checkbox("##col_aggroXTarMax", state.ShowColumnAggroXTarget))
										state.ShowColumnAggroXTarget = imgui_Checkbox_Get("##col_aggroXTarMax");
									imgui_SameLine(0);
									imgui_Text("AggroXTar Max");

									if (imgui_Checkbox("##col_aggroMinXTarMax", state.ShowColumnAggroMinXTarget))
										state.ShowColumnAggroMinXTarget = imgui_Checkbox_Get("##col_aggroMinXTarMax");
									imgui_SameLine(0);
									imgui_Text("AggroXTar Min");

									imgui_Separator();
									using (var style = PushStyle.Aquire())
									{
										style.PushStyleColor((int)ImGuiCol.Text, 0.95f, 0.85f, 0.35f, 1.0f);
										imgui_Text("Show/Hide");
									}

									imgui_Separator();

									if (imgui_Checkbox("##show_tick_timer", state.ShowTickTimer))
										state.ShowTickTimer = imgui_Checkbox_Get("##show_tick_timer");
									imgui_SameLine(0);
									imgui_Text("Tick Timer");

									if (imgui_Checkbox("##show_hot_buttons", state.ShowHotButtons))
										state.ShowHotButtons = imgui_Checkbox_Get("##show_hot_buttons");
									imgui_SameLine(0);
									imgui_Text("Hot Buttons");

									if (imgui_Checkbox("##show_player_info", state.ShowPlayerInfo))
										state.ShowPlayerInfo = imgui_Checkbox_Get("##show_player_info");
									imgui_SameLine(0);
									imgui_Text("Player Info");

									if (imgui_Checkbox("##show_target_info", state.ShowTargetInfo))
										state.ShowTargetInfo = imgui_Checkbox_Get("##show_target_info");
									imgui_SameLine(0);
									imgui_Text("Target Info");

									imgui_Separator();
									using (var style = PushStyle.Aquire())
									{
										style.PushStyleColor((int)ImGuiCol.Text, 0.95f, 0.85f, 0.35f, 1.0f);
										imgui_Text("Font");
									}


									using (var combo = ImGUICombo.Aquire())
									{
										if (combo.BeginCombo("##Select Font for GroupTable", state.SelectedFont))
										{
											foreach (var pair in E3ImGUI.FontList)
											{
												bool sel = string.Equals(state.SelectedFont, pair.Key, StringComparison.OrdinalIgnoreCase);

												if (imgui_Selectable($"{pair.Key}", sel))
												{
													state.SelectedFont = pair.Key;
												}
											}
										}
									}
									imgui_Separator();
									using (var style = PushStyle.Aquire())
									{
										style.PushStyleColor((int)ImGuiCol.Text, 0.95f, 0.85f, 0.35f, 1.0f);
										imgui_Text("Left Click Action");
									}


									using (var combo = ImGUICombo.Aquire())
									{
										if (combo.BeginCombo("##Select LeftClickAction", state.LeftClickAction))
										{
											foreach (var action in state.LeftClickActions)
											{
												bool sel = string.Equals(state.LeftClickAction, action, StringComparison.OrdinalIgnoreCase);

												if (imgui_Selectable($"{action}##leftclick", sel))
												{
													state.LeftClickAction = action;
												}
											}
										}
									}
									imgui_Separator();
									using (var style = PushStyle.Aquire())
									{
										style.PushStyleColor((int)ImGuiCol.Text, 0.95f, 0.85f, 0.35f, 1.0f);
										imgui_Text("Peer Sort Order");
									}
									using (var combo = ImGUICombo.Aquire())
									{
										if (combo.BeginCombo("##Select PeerSortOrder", state.PeerSortOrder))
										{
											foreach (var order in state.PeerSortOrders)
											{
												bool sel = string.Equals(state.PeerSortOrder, order, StringComparison.OrdinalIgnoreCase);

												if (imgui_Selectable($"{order}##peersort", sel))
												{
													state.PeerSortOrder = order;
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


									string keyForInput = $"##E3Hud_Hub_alpha_set-{i}";
									imgui_SetNextItemWidth(100);
									if (imgui_InputInt(keyForInput, (int)(state.WindowAlpha * 255), 1, 20))
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
										state.WindowAlpha = ((float)updated) / 255f;
										imgui_InputInt_Clear(keyForInput);
									}


									imgui_Separator();
									using (var style = PushStyle.Aquire())
									{
										style.PushStyleColor((int)ImGuiCol.Text, 0.95f, 0.85f, 0.35f, 1.0f);
										imgui_Text("Name Color:");
									}

									imgui_Separator();
									imgui_SetNextItemWidth(150.0f);
									if (imgui_ColorPicker4_Float("##NameColorPicker", state.NameColor[0], state.NameColor[1], state.NameColor[2], state.NameColor[3], 0))
									{

										float[] newColors = imgui_ColorPicker_GetRGBA_Float("##NameColorPicker");
										state.NameColor[0] = newColors[0];
										state.NameColor[1] = newColors[1];
										state.NameColor[2] = newColors[2];
										state.NameColor[3] = newColors[3];
										state.IsDirty = true;
									}
								}
							}

						}
						using (var imguiFont = IMGUI_Fonts.Aquire())
						{
							imguiFont.PushFont(state.SelectedFont);
							List<TableRow_GroupInfo> currentStats = state.GroupInfo;

							Int32 rowCount = 0;
							foreach (var stats in currentStats)
							{
								rowCount++;
								imgui_TableNextRow();
								imgui_TableNextColumn();
								bool is_row_selected = (state.SelectedRow == rowCount);

								string rowID = String.Empty;
								if(!IntToStringIDLookup("Hub_RenderGroupTable_SelectableRows",rowCount,out rowID))
								{
									rowID = $"##row_selected_{rowCount}";
									IntToStringIDRegister("Hub_RenderGroupTable_SelectableRows", rowCount, rowID);
								}
								if (imgui_Selectable_WithFlags(rowID, is_row_selected, (int)(ImGuiSelectableFlags.ImGuiSelectableFlags_SpanAllColumns | ImGuiSelectableFlags.ImGuiSelectableFlags_AllowOverlap)))
								{
									state.SelectedRow = rowCount;

									switch (state.LeftClickAction)
									{
										case "Foreground":
											{
												string command = $"/e3bct {stats.Name} /foreground";
												E3ImGUI.MQCommandQueue.Enqueue(command);
											}
											break;
										case "ViewBuffs":
											{
												state.SelectedToonForBuffs = stats.Name;
												if (state.SelectedToonForBuffs == E3.CurrentName) state.SelectedToonForBuffs = String.Empty;
											}
											break;
										case "NavToToon":
											if (_spawns.TryByName(stats.Name, out var spawnsNav, useCurrentCache: true))
											{
												string command = $"/nav id {spawnsNav.ID}";
												E3ImGUI.MQCommandQueue.Enqueue(command);
											}
											break;
										case "Target":
										default:
											if (_spawns.TryByName(stats.Name, out var spawns, useCurrentCache: true))
											{
												string command = $"/target id {spawns.ID}";
												E3ImGUI.MQCommandQueue.Enqueue(command);
											}
											break;
									}

								}
								using (var popup = ImGUIPopUpContext.Aquire())
								{
									if (popup.BeginPopupContextItemPerf("Hub_RenderGroupTablePopupRows", "##row_selected_context_",rowCount,1))
									{
										state.SelectedToonForBuffs = String.Empty;
										state.SelectedRow = -1;
										using (var style = PushStyle.Aquire())
										{
											style.PushStyleColor((int)ImGuiCol.Text, 0.95f, 0.85f, 0.35f, 1.0f);
											imgui_Text(stats.Name);
											imgui_Separator();

											///click left target
											///
											if (imgui_MenuItem("Trade Item on Cursor"))
											{
												if (_spawns.TryByName(stats.Name, out var spawns, useCurrentCache: true))
												{
													if (spawns.Distance3D > 19)
													{
														E3.Bots.Broadcast($"{spawns.CleanName} is too far away, get closer");
													}
													else
													{
														string command = $"/target id {spawns.ID}";
														E3ImGUI.MQCommandQueue.Enqueue(command);
														command = $"/click left target";
														E3ImGUI.MQCommandQueue.Enqueue(command);
													}

												}
											}
											if (imgui_MenuItem("Toon buffs"))
											{
												state.SelectedToonForBuffs = stats.Name;
												if (state.SelectedToonForBuffs == E3.CurrentName) state.SelectedToonForBuffs = String.Empty;
											}
											if (imgui_MenuItem("Nav to Toon"))
											{
												if (_spawns.TryByName(stats.Name, out var spawns, useCurrentCache: true))
												{
													string command = $"/nav id {spawns.ID}";
													E3ImGUI.MQCommandQueue.Enqueue(command);
												}
											}
											if (imgui_MenuItem("Nav Toon to us"))
											{
												if (_spawns.TryByName(E3.CurrentName, out var spawns, useCurrentCache: true))
												{
													string command = $"/e3bct {stats.Name} /nav id {spawns.ID}";
													E3ImGUI.MQCommandQueue.Enqueue(command);
												}
											}
											if (imgui_MenuItem("Foreground Toon"))
											{
												string command = $"/e3bct {stats.Name} /foreground";
												E3ImGUI.MQCommandQueue.Enqueue(command);
											}
										}
									}
								}

								//float sizeX = imgui_CalcTextSizeX(stats.DisplayName);

								if (state.DisplayHPBar)
								{
									imgui_SameLine(0, 0);
									float progressBarStartPosX = imgui_GetCursorPosX();
									float progressBarStartPosY = imgui_GetCursorPosY();
									using (var style = PushStyle.Aquire())
									{
										style.PushStyleColor((int)ImGuiCol.PlotHistogram, state.HealthBarColor[0], state.HealthBarColor[1], state.HealthBarColor[2], state.HealthBarColor[3]);
										style.PushStyleColor((int)ImGuiCol.FrameBg, 0, 0, 0, 0f);
										float widthOfColumn = imgui_GetContentRegionAvailX();
										imgui_ProgressBar(((float)stats.HPPercent / (float)100), 20, widthOfColumn, "");
									}

									float[] barPos = imgui_GetItemRectMin();
									float[] barSize = imgui_GetItemRectSize();
									float[] textSize = imgui_CalcTextSize(stats.DisplayName);

									//this centers the text
									//float textPosX = barPos[0] + (barSize[0] - textSize[0]) * 0.5f;
									float textPosX = barPos[0];

									float textPosY = barPos[1] + (barSize[1] - textSize[1]) * 0.5f;

									if (stats.DisplayNameColor.r != 0f && stats.DisplayNameColor.g != 0f && stats.DisplayNameColor.b != 0f && stats.DisplayNameColor.a != 0f)
									{

										imgui_GetWindowDrawList_AddText(textPosX, textPosY, GetColor(stats.DisplayNameColor.r, stats.DisplayNameColor.g, stats.DisplayNameColor.b, state.NameColor[3]), stats.DisplayName);
									}
									else
									{
										imgui_GetWindowDrawList_AddText(textPosX, textPosY, GetColor(state.NameColor[0], state.NameColor[1], state.NameColor[2], state.NameColor[3]), stats.DisplayName);

									}

									if (stats.PetHPPercent > 0)
									{
										imgui_SameLine(0, 0);
										imgui_SetCursorPosX(progressBarStartPosX);
										imgui_SetCursorPosY(progressBarStartPosY + barSize[1] - 5);
										using (var style = PushStyle.Aquire())
										{
											style.PushStyleColor((int)ImGuiCol.PlotHistogram, state.PetHealthBarColor[0], state.PetHealthBarColor[1], state.PetHealthBarColor[2], state.PetHealthBarColor[3]);
											style.PushStyleColor((int)ImGuiCol.FrameBg, 0, 0, 0, 0f);
											float widthOfColumn = imgui_GetContentRegionAvailX();
											imgui_ProgressBar(((float)stats.PetHPPercent / (float)100), 5, widthOfColumn, "");
										}
									}

								}
								else
								{
									imgui_SameLine(0);

									if (stats.DisplayNameColor.r != 0 && stats.DisplayNameColor.g != 0 && stats.DisplayNameColor.b != 0 && stats.DisplayNameColor.a != 0)
									{

										imgui_TextColored(stats.DisplayNameColor.r, stats.DisplayNameColor.g, stats.DisplayNameColor.b, state.NameColor[3], stats.DisplayName);
									}
									else
									{

										imgui_TextColored(state.NameColor[0], state.NameColor[1], state.NameColor[2], state.NameColor[3], stats.DisplayName);

									}

								}
								var c = stats.DisplayNameColor;

								if (state.ShowColumnAggro)
								{
									imgui_TableNextColumn();
									c = stats.AggroColor;
									imgui_TextColored(c.r, c.g, c.b, 1.0f, stats.AggroPct);

								}
								if (state.ShowColumnHP)
								{
									imgui_TableNextColumn();
									c = stats.HPColor;
									imgui_TextColored(c.r, c.g, c.b, 1.0f, stats.HP);
								}
								if (state.ShowColumnEnd)
								{
									imgui_TableNextColumn();
									c = stats.EndColor;
									imgui_TextColored(c.r, c.g, c.b, 1.0f, stats.Endurance);
								}
								if (state.ShowColumnMana)
								{
									imgui_TableNextColumn();
									c = stats.ManaColor;
									imgui_TextColored(c.r, c.g, c.b, 1.0f, stats.Mana);
								}
								if (state.ShowColumnDistance)
								{
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
								if (state.ShowColumnAggroXTarget)
								{
									imgui_TableNextColumn();
									c = stats.AggroColor;
									imgui_TextColored(c.r, c.g, c.b, 1.0f, stats.XtargetAggroPct);

								}
								if (state.ShowColumnAggroMinXTarget)
								{
									imgui_TableNextColumn();
									c = stats.AggroColor;
									imgui_TextColored(c.r, c.g, c.b, 1.0f, stats.XtargetMinAggroPct);

								}

							}
						}
					}
				}

			}

		}
		public class TableRow_BuffInfo
		{
			public Int32 BuffType = 0;
			public Int32 CounterTypeID = 0;
			public Int32 CounterNumberValue = 0;
			public Spell Spell;
			public string CasterName = String.Empty;
			public Int32 SpellID = 0;
			public string Name = String.Empty;
			public string DisplayName { get; set; }
			public (float r, float g, float b, float a) DisplayNameColor;
			public Int32 iconID;
			public string Display_Duration { get; set; }
			public Int32 Duration { get; set; }
			public Int32 MaxDuration_Value { get; set; }
			public string SimpleDuration { get; set; }
			public (float r, float g, float b, float a) DurationColor;

			public string HitCount = String.Empty;
			public (float r, float g, float b) HitCountColor;

			public int SpellType = 0;

			public string CounterType = "";
			public string Display_CounterNumber = String.Empty;

			public TableRow_BuffInfo(Int32 spellid)
			{
				SpellID = spellid;
			}
		}
		public static Dictionary<string, TableRow_GroupInfo> _groupInfoCache = new Dictionary<string, TableRow_GroupInfo>();
		public static Dictionary<Int32, TableRow_BuffInfo> _personalBuffInfoCache = new Dictionary<int, TableRow_BuffInfo>();
		public static Dictionary<Int32, TableRow_BuffInfo> _petBuffInfoCache = new Dictionary<int, TableRow_BuffInfo>();

		public static Dictionary<Int32, TableRow_BuffInfo> _targetBuffInfoCache = new Dictionary<int, TableRow_BuffInfo>();

		public class TableRow_GroupInfo
		{

			public bool InZone = false;
			public string Name;
			public string DisplayName { get; set; }
			public (float r, float g, float b, float a) DisplayNameColor;
			public Int32 HPPercent = 0;
			public Int32 PetHPPercent = 0;
			public string HP { get; set; }
			public (float r, float g, float b, float a) HPColor;

			public string Mana { get; set; }
			public (float r, float g, float b, float a) ManaColor;

			public string Endurance { get; set; }
			public (float r, float g, float b, float a) EndColor;

			public string Distance { get; set; }

			public (float r, float g, float b, float a) DistanceColor;


			public string AggroPct { get; set; }

			public (float r, float g, float b, float a) AggroColor;

			public string XtargetAggroPct { get; set; }

			public (float r, float g, float b, float a) AggroXTargetColor;
			public string XtargetMinAggroPct { get; set; }

			public (float r, float g, float b, float a) AggroMinXTargetColor;

			public TableRow_GroupInfo()
			{

			}

			public TableRow_GroupInfo(string characterName)
			{
				Name = characterName;
			}
			public class Hotbutton_DynamicButton
			{
				public string Name = String.Empty;
				public string Command = String.Empty;
			}
		}
	}
}
