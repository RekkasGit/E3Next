using E3Core.Data;
using E3Core.Processors;
using E3Core.UI.Windows.CharacterSettings;
using E3Core.Utility;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using MonoCore;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.PerformanceData;
using System.Linq;
using System.Linq.Expressions;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Resources;
using System.Text;
using System.Threading;
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
		private static bool _windowInitialized = false;
		private static bool _imguiContextReady = false;

		private static IMQ MQ = E3.MQ;
		private static ISpawns _spawns = E3.Spawns;

		public static HudHubWindowStates _state = new HudHubWindowStates();
		public static Random rand = new Random();

		private const string IMGUI_DETATCH_BUFFS_ID = FontAwesome.FAExternalLinkSquare + "##detach_buffs";
		private const string IMGUI_DETATCH_SONGS_ID = FontAwesome.FAExternalLinkSquare + "##detach_songs";
		private const string IMGUI_DETATCH_HOTBUTTON_ID = FontAwesome.FAExternalLinkSquare + "##detach_hotbuttons";
		private const string IMGUI_DETATCH_PLAYERINFO_ID = FontAwesome.FAExternalLinkSquare + "##detach_playerinfo";
		private const string IMGUI_DETATCH_TARGETINFO_ID = FontAwesome.FAExternalLinkSquare + "##detach_targetinfo";
		private const string IMGUI_SETTINGS_PLAYERINFO_ID = MaterialFont.settings + "##playerinfo_settings";
		private const string IMGUI_SETTINGS_TARGETINFO_ID = MaterialFont.settings + "##targetinfo_settings";

		private static string IMGUI_TABLE_GROUP_ID = $"E3HubGroupTable-{E3.CurrentName}-{E3.CurrentClass}-{E3.ServerName}";

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
			var state = _state.GetState<State_HubWindow>();

			if (Core._MQ2MonoVersion < 0.37m) return;
			E3ImGUI.RegisterWindow(state.WindowName, RenderHub);

			EventProcessor.RegisterCommand("/e3hud_hub", (x) =>
			{
				if (Core._MQ2MonoVersion < 0.39m)
				{
					MQ.Write("This requires MQ2Mono 0.39 or greater");
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
		private static (float r, float g, float b) GetAggroSeverityColor(double distance)
		{

			foreach (var band in _aggroSeverity)
			{
				if (distance >= band.MinDist && distance < band.MaxDist)
				{
					return (band.R, band.G, band.B);
				}
			}

			return (0.9f, 0.9f, 0.9f);
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
		private static (float r, float g, float b) GetConColorRGB(int conColorID)
		{
			switch (conColorID)
			{
				case 0x06: return (0.50f, 0.50f, 0.50f); // GREY
				case 0x02: return (0.10f, 0.85f, 0.10f); // GREEN
				case 0x12: return (0.40f, 0.70f, 1.00f); // LIGHT BLUE
				case 0x04: return (0.20f, 0.40f, 1.00f); // BLUE
				case 0x0a: return (0.95f, 0.95f, 0.95f); // WHITE
				case 0x0f: return (0.95f, 0.85f, 0.10f); // YELLOW
				case 0x0d: return (1.00f, 0.15f, 0.15f); // RED
				default: return (0.90f, 0.90f, 0.90f);
			}
		}

		static Task UpdateTask;


		private static void ProcessUpdates()
		{
			RefreshBuffInfo();
			RefreshGroupInfo();
		}

		static string _exceptionMessage = String.Empty;


		private static void RefreshBuffInfo()
		{
			var hubState = _state.GetState<State_HubWindow>();
			var buffState = _state.GetState<State_BuffWindow>();
			var songState = _state.GetState<State_SongWindow>();
			var debuffState = _state.GetState<State_DebuffWindow>();

			if (!e3util.ShouldCheck(ref buffState.LastUpdated, buffState.LastUpdateInterval)) return;


			try
			{
				string userTouse = E3.CurrentName;

				if (!String.IsNullOrWhiteSpace(hubState.SelectedToonForBuffs))
				{
					userTouse = hubState.SelectedToonForBuffs;
				}
				string buffInfo = E3.Bots.Query(userTouse, "${Me.BuffInfo}");

				if (buffState.PreviousBuffInfo != string.Empty)
				{
					if (buffState.PreviousBuffInfo == buffInfo)
					{
						//no difference
						return;
					}

				}
				buffState.PreviousBuffInfo = buffInfo;

				if (!String.IsNullOrWhiteSpace(buffInfo))
				{
					string s = buffInfo;
					BuffDataList bufflist = new BuffDataList();
					bufflist.MergeFrom(ByteString.FromBase64(s));

					//if (E3.CurrentName != userTouse) return;
					buffState.BuffInfo.Clear();
					songState.SongInfo.Clear();
					debuffState.DebuffInfo.Clear();

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
						Int32 maxDuration = MQ.Query<Int32>($"${{Spell[{spellid}].Duration}}", false) * 6 * 1000;
						var buffRow = new TableRow_BuffInfo(buffName);

						buffRow.MaxDuration_Value = maxDuration;
						buffRow.Duration_Value = duration;

						buffRow.iconID = spellIcon;


						var buffTimeSpan = TimeSpan.FromMilliseconds(duration);
						buffRow.Duration = buffTimeSpan.ToString("h'h 'm'm 's's'");
						buffRow.DurationColor = GetBuffDurationSeverityColor(duration);
						buffRow.SpellType = (Int32)spelltypeid;
						//check if spellid exists

						buffRow.SpellID = spellid;

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
			catch (Exception ex)
			{
				_exceptionMessage = ex.StackTrace;
			}


		}
		private static void RefreshGroupInfo()
		{
			var state = _state.GetState<State_HubWindow>();

			if (!e3util.ShouldCheck(ref state.LastUpdated, state.LastUpdateInterval)) return;
			state.GroupInfo.Clear();

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


				Int32 mana, endurance, hp, aggroPct, aggroPctXtarget, aggroPctMinXtarget, pet_pctHealth;
				Int32.TryParse(E3.Bots.Query(user, "${Me.PctHPs}"), out hp);
				Int32.TryParse(E3.Bots.Query(user, "${Me.PctMana}"), out mana);
				Int32.TryParse(E3.Bots.Query(user, "${Me.PctEndurance}"), out endurance);
				Int32.TryParse(E3.Bots.Query(user, "${Me.PctAggro}"), out aggroPct);
				Int32.TryParse(E3.Bots.Query(user, "${Me.XTargetMaxAggro}"), out aggroPctXtarget);
				Int32.TryParse(E3.Bots.Query(user, "${Me.XTargetMinAggro}"), out aggroPctMinXtarget);
				Int32.TryParse(E3.Bots.Query(user, "${Me.Pet.CurrentHPs}"), out pet_pctHealth);

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

				if (_spawns.TryByName(user, out var spawn, true))
				{
					x = E3.Loc_X - x;
					y = E3.Loc_Y - y;
					z = E3.Loc_Z - z;
					//we can calculate distance
					distance = Math.Sqrt(x * x + y * y + z * z);


				}
				var row = new TableRow_GroupInfo(user);

				row.PetHPPercent = pet_pctHealth;

				if (distance == 0)
				{
					row.Distance = zonename;

				}
				else
				{
					row.Distance = distance.ToString("N0");
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
					row.DisplayNameColor = (0.950f, 0.910f, 0.143f); //GOLD
				}
				else
				{
					row.DisplayName = user;
					row.DisplayNameColor = (0.275f, 0.860f, 0.85f);
				}
				if (mana == 0)
				{
					row.Mana = "-";
				}
				else
				{
					row.Mana = mana.ToString() + "%";

				}
				row.ManaColor = GetResourceSeverityColor(mana);

				row.Endurance = endurance.ToString() + "%";
				row.EndColor = GetResourceSeverityColor(endurance);
				row.HPPercent = hp;
				row.HP = hp.ToString() + "%";
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

				state.GroupInfo.Add(row);
			}
		}
		private static void RefreshPlayerInfo()
		{
			var state = _state.GetState<State_PlayerInfoWindow>();
			var hub_state = _state.GetState<State_HubWindow>();
			if (!hub_state.ShowPlayerInfo) return;
			if (!e3util.ShouldCheck(ref state.PlayerInfoLastUpdated, state.PlayerInfoUpdateInterval)) return;

			state.PlayerLevel = MQ.Query<int>("${Me.Level}", false);
			state.PlayerHP = E3.PctHPs;
			state.PlayerMana = MQ.Query<int>("${Me.PctMana}", false);
			state.PlayerEnd = MQ.Query<int>("${Me.PctEndurance}", false);
			state.PlayerExp = MQ.Query<Decimal>("${Me.PctExp}", false);
			state.PlayerAAPoints = MQ.Query<int>("${Me.AAPoints}", false);


			state.PlayerHPColor = GetResourceSeverityColor(state.PlayerHP);
			state.PlayerManaColor = GetResourceSeverityColor(state.PlayerMana);
			state.PlayerEndColor = GetResourceSeverityColor(state.PlayerEnd);


			if (String.IsNullOrEmpty(state.DisplayPlayerInfo) || state.DisplayPlayerInfo_Level != state.PlayerLevel)
			{
				state.DisplayPlayerInfo = $"{E3.CurrentName} (Lvl {state.PlayerLevel}) - {E3.CurrentShortClassString}";
				state.DisplayPlayerInfo_Level = state.PlayerLevel;
			}

			if (String.IsNullOrEmpty(state.DisplayHP) || state.DisplayHP_Value != state.PlayerHP)
			{
				state.DisplayHP = $"HP: {state.PlayerHP}%  ";
				state.DisplayHP_Value = state.PlayerHP;
			}
			if (String.IsNullOrEmpty(state.DisplayMana) || state.DisplayMana_Value != state.PlayerMana)
			{
				state.DisplayMana = $"Mana: {state.PlayerMana}%  ";
				state.DisplayMana_Value = state.PlayerMana;
			}
			if (String.IsNullOrEmpty(state.DisplayEnd) || state.DisplayEnd_Value != state.PlayerEnd)
			{
				state.DisplayEnd = $"End: {state.PlayerEnd}%";
				state.DisplayEnd_Value = state.PlayerEnd;
			}
			if (String.IsNullOrEmpty(state.DisplayExp) || state.DisplayExp_Value != state.PlayerExp)
			{
				state.DisplayExp = $"Exp: {state.PlayerExp:F2}%  ";
				state.DisplayExp_Value = state.PlayerExp;
			}
			if (String.IsNullOrEmpty(state.DisplayAA) || state.DisplayAA_Value != state.PlayerAAPoints)
			{
				state.DisplayAA = $"AA: {state.PlayerAAPoints}";
				state.DisplayAA_Value = state.PlayerAAPoints;
			}
			string activeDisc = E3.Bots.Query(E3.CurrentName, "${Me.ActiveDisc}");
			string activeDiscDurationInTicks = E3.Bots.Query(E3.CurrentName, "${Me.ActiveDiscTimeLeft}");

			if (!String.IsNullOrWhiteSpace(activeDisc))
			{
				state.ActiveDiscPercentLeft= MQ.Query<Decimal>("${Window[CombatAbilityWnd].Child[CAW_CombatEffectTimeRemainingGauge].Value}",false)/10;
			}
			
			Int32 durationOfDiscInSeconds = 0;
			Int32.TryParse(activeDiscDurationInTicks, out durationOfDiscInSeconds);
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
		private static void RefreshTargetInfo()
		{
			var state = _state.GetState<State_TargetInfoWindow>();
			var hub_state = _state.GetState<State_HubWindow>();
			
			if (!hub_state.ShowTargetInfo) return;
			if (!e3util.ShouldCheck(ref state.TargetInfoLastUpdated, state.TargetInfoUpdateInterval)) return;

			Int32 targetID = MQ.Query<Int32>("${Target.ID}", false);
			
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
			
			if (_spawns.TryByID(targetID, out var spawn, false))
			{
			
				if(spawn.CleanName!=state.TargetName)
				{
					state.PreviousTargetName = state.TargetName;
					state.TargetName = spawn.CleanName;
					state.TargetNameSize = imgui_CalcTextSizeX(state.TargetName);
				}
	

				state.TargetHP = (int)spawn.PctHps;
				state.TargetLevel = spawn.Level;
				state.TargetClassName = spawn.ClassShortName;
				state.TargetDistance = spawn.Distance3D;
				state.TargetDistanceString = spawn.Distance3D.ToString("N0");
				state.TargetNameColor = GetConColorRGB(spawn.ConColorID);
				state.TargetDistanceColor = GetDistanceSeverityColor(spawn.Distance3D);
				state.Display_LevelAndClassString = $"Lvl {spawn.Level} {spawn.ClassShortName}";

				//get my aggro on target
				Decimal percentAggro = MQ.Query<Decimal>("${Target.PctAggro}", false);

				if (percentAggro > 0){state.Display_MyAggroPercent = $"{percentAggro}%";}
				else{state.Display_MyAggroPercent = String.Empty;}

				state.MyAggroPercent = percentAggro;

				string AggroHolder = MQ.Query<String>("${Target.AggroHolder}", false);
				if (AggroHolder == "NULL") AggroHolder = String.Empty;
				state.Display_TargetsCurrentTarget = AggroHolder;
				string PersonOn2ndAggro = MQ.Query<String>("${Target.SecondaryAggroPlayer}", false);
				if (PersonOn2ndAggro == "NULL") PersonOn2ndAggro = String.Empty;
				state.Display_SecondAggroName = PersonOn2ndAggro;
				Decimal percentAggro2nd = MQ.Query<Decimal>("${Target.SecondaryPctAggro}",false);
				state.SecondAggroPercent = percentAggro2nd;

				if (percentAggro2nd > 0) { state.Display_SecondAggroPercent = $"{percentAggro2nd}%"; }
				else { state.Display_SecondAggroPercent = String.Empty; }

				
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

			Int32 buffCount = MQ.Query<Int32>("${Target.BuffCount}", false);
			int maxBuffs = Math.Min(buffCount, 30);

			for (int i = 1; i <= maxBuffs; i++)
			{
				Int32 spellid = MQ.Query<Int32>($"${{Target.Buff[{i}].ID}}", false);
				if (spellid <= 0) continue;
				Int32 duration = MQ.Query<Int32>($"${{Target.Buff[{i}].Duration}}", false);
				Int32 spellIcon = MQ.Query<Int32>($"${{Spell[{spellid}].SpellIcon}}", false);
				string buffName = MQ.Query<string>($"${{Spell[{spellid}].Name}}", false);
				var buffRow = new TableRow_BuffInfo(buffName);
				buffRow.iconID = spellIcon;
				var buffTimeSpan = TimeSpan.FromMilliseconds(duration);
				buffRow.Duration = buffTimeSpan.ToString("h'h 'm'm 's's'");
				buffRow.DurationColor = GetBuffDurationSeverityColor(duration);
				string spellType = MQ.Query<string>($"${{Spell[{spellid}].SpellType}}", false);

				Int32 spellTypeID = 0;
				if (spellType == "Detrimental") spellTypeID = 0;
				if (spellType == "Beneficial") spellTypeID = 1;
				if (spellType == "Beneficial(Group)") spellTypeID = 2;
				buffRow.SpellType = (Int32)spellTypeID;
				buffRow.SpellID = spellid;

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
					buffRow.DisplayName = buffName + $" ( {buffRow.Duration} )";
				}
				else
				{
					buffRow.DisplayName = buffName;
				}
				if (buffName == "NULL") buffName = "Unknown";


				if (BuffCheck.BuffInfoCache.TryGetValue(spellid, out var spell))
				{
					buffRow.Spell = spell;
				}
				else
				{
					BuffCheck.BuffCacheLookupQueue.TryAdd(spellid, spellid);
				}
				state.TargetBuffs.Add(buffRow);


			}
		}
		private static void RenderPlayerInfo()
		{
			var hub_state = _state.GetState<State_HubWindow>();
			var state = _state.GetState<State_PlayerInfoWindow>();

			if (!hub_state.ShowPlayerInfo) return;
			if (state.PlayerLevel == 0) return;


			float widthAvail = imgui_GetContentRegionAvailX();

			// Detach/Reattach buttons
			if (!state.Detached)
			{
				imgui_TextColored(0.275f, 0.860f, 0.85f, 1.0f, state.DisplayPlayerInfo);
				imgui_SameLine(widthAvail - 20);
				if (imgui_Button(IMGUI_DETATCH_PLAYERINFO_ID))
				{
					state.Detached = true;
					imgui_Begin_OpenFlagSet(state.WindowName, true);
				}
			}
			else
			{
				imgui_TextColored(0.275f, 0.860f, 0.85f, 1.0f, state.DisplayPlayerInfo);
				float windowWidth = imgui_GetWindowWidth();
				imgui_SameLine(0);
				float availSpace = imgui_GetContentRegionAvailX();
				if (imgui_InvisibleButton("##PlayerInfoSettingsInvisButton", availSpace, 20, (int)ImGuiMouseButton.Right | (int)ImGuiMouseButton.Left))
				{
				}
				using (var popup = ImGUIPopUpContext.Aquire())
				{
					if (popup.BeginPopupContextItem($"##PlayerInfoSettingsPopup", 1))
					{
						if(imgui_MenuItem("Dock"))
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
							
						string keyForInput = "##PlayerInfoWindow_alpha_set";
						imgui_SetNextItemWidth(100);
						if (imgui_InputInt(keyForInput, (int)(state.WindowAlpha * 255), 1, 20))
						{
							int updated = imgui_InputInt_Get(keyForInput);
							if (updated > 255) updated = 255;
							if (updated < 0) updated = 0;
							state.WindowAlpha = ((float)updated) / 255f;
						}
					}
				}
				
			}

			var hp = state.PlayerHPColor;
			imgui_TextColored(hp.r, hp.g, hp.b, 1.0f, state.DisplayHP);
			imgui_SameLine(0);

			if (state.PlayerMana > 0)
			{
				var mana = state.PlayerManaColor;
				imgui_TextColored(mana.r, mana.g, mana.b, 1.0f, state.DisplayMana);
				imgui_SameLine(0);
			}
			var end = state.PlayerEndColor;
			imgui_TextColored(end.r, end.g, end.b, 1.0f, state.DisplayEnd);
			imgui_TextColored(0.95f, 0.70f, 0.50f, 1.0f, state.DisplayExp);
			imgui_SameLine(0);
			imgui_TextColored(0.95f, 0.85f, 0.35f, 1.0f, state.DisplayAA);

			if (!String.IsNullOrWhiteSpace(state.ActiveDisc))
			{
				//DiscProgressBar
				using (var style = PushStyle.Aquire())
				{
					style.PushStyleColor((int)ImGuiCol.PlotHistogram, state.DiscProgressBarColor[0], state.DiscProgressBarColor[1], state.DiscProgressBarColor[2], state.DiscProgressBarColor[3]);
					//style.PushStyleColor((int)ImGuiCol.FrameBg, 0.2f, 0.2f, 0.2f, 0.5f);
					imgui_TextColored(0.95f, 0.85f, 0.35f, 1.0f, $"Disc: {state.ActiveDisc} - {state.Display_ActiveDiscTimeleft}");
					imgui_ProgressBar((((float)state.ActiveDiscPercentLeft) / (float)100), 15, 200, $"{state.ActiveDiscPercentLeft.ToString("N0")}%");
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
					imgui_ProgressBar(0f, 18, (int)widthAvail, String.Empty);
				}
			

				// Reserve space for level/class + distance line and buff row to prevent layout shifting
				// Use invisible text to create spacing
				imgui_Text(" ");
				imgui_Text(" ");
				return;
			}
			
			// Center the target name over the HP bar
			float nameWidth = state.TargetNameSize;
			float centerX = (widthAvail - nameWidth) / 2f;
			if (centerX < 0) centerX = 0;

			if (!tiState.Detached)
			{
				imgui_SetCursorPosX(centerX);
				imgui_TextColored(nc.r, nc.g, nc.b, 1.0f, state.TargetName);
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
				if(!String.IsNullOrEmpty(state.Display_MyAggroPercent))
				{
					imgui_TextColored(0, 1, 1, 1.0f, "Me:");
					imgui_SameLine(0, 5);
					imgui_TextColored(1, 0, 0, 1.0f, state.Display_MyAggroPercent);

					if (!string.IsNullOrWhiteSpace(state.Display_SecondAggroName))
					{
						imgui_SameLine(0, 10);
						imgui_TextColored(0, 1, 1, 1.0f, state.Display_SecondAggroName);
						imgui_SameLine(0, 0);
						imgui_Text(": ");
						imgui_SameLine(0, 0);
						imgui_TextColored(1, 0, 0, 1.0f, state.Display_SecondAggroPercent);

					}
					
				}
				

				imgui_SameLine(0, 5);
				float windowWidth = imgui_GetWindowWidth();
				float contentCenterX = (windowWidth - nameWidth) / 2f;
				if (contentCenterX < 0) contentCenterX = 0;
				imgui_SetCursorPosX(contentCenterX);
				imgui_TextColored(nc.r, nc.g, nc.b, 1.0f, state.TargetName);
			
				if(!String.IsNullOrEmpty(state.Display_TargetsCurrentTarget))
				{
					imgui_SameLine(0,10);
					imgui_Text("-->");
					imgui_SameLine(0, 5);
					if(state.SecondAggroPercent>state.MyAggroPercent)
					{
						imgui_TextColored(1, 0, 0, 1.0f, state.Display_TargetsCurrentTarget);

					}
					else
					{
						imgui_TextColored(1, 0, 0, 1.0f, E3.CurrentName);

					}

				}

				imgui_SameLine(0);

				float availSpace = imgui_GetContentRegionAvailX();
				//imgui_SetCursorPosX(windowWidth - 70);
				if (imgui_InvisibleButton("##TargetInfoSettingsInvisButton", availSpace, 20,(int)ImGuiMouseButton.Right| (int) ImGuiMouseButton.Left))
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

			// Target HP% progress bar


			using(var style = PushStyle.Aquire())
			{
				style.PushStyleColor((int)ImGuiCol.PlotHistogram, 0.8f, 0.15f, 0.15f, 0.9f);
				style.PushStyleColor((int)ImGuiCol.FrameBg, 0.2f, 0.2f, 0.2f, 0.5f);
				imgui_ProgressBar((float)state.TargetHP / 100f, 18, (int)widthAvail, $"{state.TargetHP}%");
			}
			// Level & Class (left) + Distance (right)
			string leftText = state.Display_LevelAndClassString;
			imgui_Text(leftText);

			
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

					imgui_DrawSpellIconBySpellID(state.TargetBuffs[i].SpellID, iconSize);

					if (imgui_IsItemHovered())
					{
						using (var tooltip = ImGUIToolTip.Aquire())
						{

							imgui_Text($"Spell: {state.TargetBuffs[i].Name}");
							imgui_Text($"SpellID: {state.TargetBuffs[i].SpellID}");
							imgui_Text($"Duration: {state.TargetBuffs[i].Duration}");

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
							RefreshGroupInfo();
							RefreshBuffInfo();
							RefreshPlayerInfo();
							RefreshTargetInfo();

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
				E3ImGUI.PushCurrentTheme();
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
					E3ImGUI.PopCurrentTheme();
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
		}
		private static void RenderHub()
		{
			if (!_imguiContextReady) return;


			var state = _state.GetState<State_HubWindow>();
			if (imgui_Begin_OpenFlagGet(state.WindowName))
			{
				TryReattachWindowsIfClosed();

				RenderHub_MainWindow();

				var buffState = _state.GetState<State_BuffWindow>();
				RenderHub_TryDetached(buffState.WindowName, buffState.Detached, RenderBuffTableSimple, buffState.WindowAlpha, noTitleBar: true, locked: buffState.Locked);
				var songState = _state.GetState<State_SongWindow>();
				RenderHub_TryDetached(songState.WindowName, songState.Detached, RenderSongTableSimple, songState.WindowAlpha, noTitleBar: true, locked: songState.Locked);
				var buttonState = _state.GetState<State_HotbuttonsWindow>();
				if (state.ShowHotButtons)
				{
					RenderHub_TryDetached(buttonState.WindowName, buttonState.Detached, RenderHotbuttons, buttonState.WindowAlpha, noTitleBar: true, locked: buttonState.Locked);
				}
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
			imgui_Text($"Songs:({state.SongInfo.Count})");

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
					if (popup.BeginPopupContextItem($"##SongWindowSettingsPopup", 1))
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
							

						string keyForInput = $"##songWindow_alpha_set";
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
							imgui_Text("View Mode");
						}
							
						if (imgui_Checkbox("##song_listview", state.ListView))
						{
							state.ListView = imgui_Checkbox_Get("##song_listview");
						}
						imgui_SameLine(0);
						imgui_Text("List View");

					}
				}
			}

			// Use List View if enabled, otherwise use icon grid
			if (state.ListView)
			{
				RenderBuffListView(state.SongInfo, "E3HubSongTableListView", state.IconSize, state.FadeRatio, state.FadeTimeInMS, buffState.NewBuffsTimeStamps);
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
								if (popup.BeginPopupContextItem($"SongTableIconContext-{counter}", 1))
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

		private static void RenderHotbuttons()
		{

			var state = _state.GetState<State_HotbuttonsWindow>();

			float widthAvail = imgui_GetContentRegionAvailX();

			if (state.Detached)
			{
				float windowWidth = imgui_GetWindowWidth();
				imgui_SameLine(0);
				imgui_SetCursorPosX(windowWidth - 70);
				if (imgui_Button($"{MaterialFont.settings}##hotbutton_window_settings"))
				{
				}
				using (var popup = ImGUIPopUpContext.Aquire())
				{
					if (popup.BeginPopupContextItem($"##Hotbutton_WindowSettingsPopup", 1))
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
						}
							
						imgui_Text("Alpha");
						string keyForInput = "##Hotbutton_Window_alpha_set";
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
				imgui_SameLine(windowWidth - 35);
				if (imgui_Button("<<##reattach_hotbutton"))
				{
					state.Detached = false;
					imgui_Begin_OpenFlagSet(state.WindowName, false);
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
							if (popup.BeginPopupContextItem($"##Hotbutton_detach-{counter}", 1))
							{
								using (var style = PushStyle.Aquire())
								{
									style.PushStyleColor((int)ImGuiCol.Text, 0.95f, 0.85f, 0.35f, 1.0f);

									if (imgui_MenuItem("Pop Out"))
									{
										state.Detached = true;
										imgui_Begin_OpenFlagSet(state.WindowName, true);
									}
									imgui_Separator();
									using(var style2 = PushStyle.Aquire())
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
		static float[] BuffListView_NameColor= { 1,1,1,1};
		static float[] BuffListView_ProgressColor = { 0, 0, 1, 0.4f };
		static float[] BuffListView_ProgressBGColor = null;
		private static void RenderBuffListView(List<TableRow_BuffInfo> buffList, string tableName, int iconSize, double fadeRatio, Int32 fadeTimeInMS, Dictionary<Int32, Int64> newBuffsTimeStamps)
		{
			var hubState = _state.GetState<State_HubWindow>();
			var buffState = _state.GetState<State_BuffWindow>();
			int tableFlags = (int)(ImGuiTableFlags.ImGuiTableFlags_Borders  | ImGuiTableFlags.ImGuiTableFlags_SizingStretchProp);
			

			if(BuffListView_ProgressBGColor==null)
			{
				UInt32 packedColor = imgui_GetColorU32((int)ImGuiCol.WindowBg, 1);
				BuffListView_ProgressBGColor = GetRGBAFloatsFromColor(packedColor);
			}

			//remove padding between rows
			using(var stylevar = PushStyle.Aquire())
			{
				stylevar.PushStyleVarVec2((int)ImGuiStyleVar.CellPadding, 0, 0);
				using (var table = ImGUITable.Aquire())
				{
					if (table.BeginTable(tableName, 2, tableFlags, 0f, 0))
					{
						imgui_TableSetupColumn("Icon", (int)ImGuiTableColumnFlags.ImGuiTableColumnFlags_WidthFixed, 22);
						imgui_TableSetupColumn_Default("Name");

						foreach (var stats in buffList)
						{
							imgui_TableNextRow();
							imgui_TableSetColumnIndex(0);

							int smallIconSize = 18;
							imgui_DrawSpellIconByIconIndex(stats.iconID, smallIconSize);
							imgui_TableSetColumnIndex(1);

							// Calculate duration in seconds for color coding
							int totalSeconds = 0;
							var durationParts = stats.Duration.Split(' ');
							foreach (var part in durationParts)
							{
								if (part.EndsWith("h") && Int32.TryParse(part.TrimEnd('h'), out int hours))
									totalSeconds += hours * 3600;
								else if (part.EndsWith("m") && Int32.TryParse(part.TrimEnd('m'), out int mins))
									totalSeconds += mins * 60;
								else if (part.EndsWith("s") && Int32.TryParse(part.TrimEnd('s'), out int secs))
									totalSeconds += secs;
							}

							// Yellow text for buffs expiring soon (< 5 minutes)
							bool expiringSoon = totalSeconds > 0 && totalSeconds < 300;

							float textPosX, textPosY;
							
							if (buffState.ShowProgressBars)
							{
								using (var style = PushStyle.Aquire())
								{
									style.PushStyleColor((int)ImGuiCol.PlotHistogram, BuffListView_ProgressColor[0], BuffListView_ProgressColor[1], BuffListView_ProgressColor[2], BuffListView_ProgressColor[3]);
									style.PushStyleColor((int)ImGuiCol.FrameBg, BuffListView_ProgressBGColor[0], BuffListView_ProgressBGColor[1], BuffListView_ProgressBGColor[2], buffState.WindowAlpha);

									float widthOfColumn = imgui_GetContentRegionAvailX();
									imgui_ProgressBar(((float)stats.Duration_Value / (float)stats.MaxDuration_Value), 20, (int)widthOfColumn, "");
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
									if (imgui_Selectable_WithFlags($"##RemoveSpell_{stats.SpellID}", selected, (int)ImGuiSelectableFlags.ImGuiSelectableFlags_SpanAllColumns))
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
								if (imgui_Selectable_WithFlags($"##RemoveSpell_{stats.SpellID}", selected, (int)ImGuiSelectableFlags.ImGuiSelectableFlags_SpanAllColumns))
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
									imgui_Text($"Duration: {stats.Duration}");
									if (!String.IsNullOrWhiteSpace(stats.CounterType))
									{
										imgui_Text($"CounterType: {stats.CounterType}");
										imgui_Text($"CounterNumber: {stats.CounterNumber}");
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
								if (popup.BeginPopupContextItem($"{tableName}_Context_{stats.SpellID}", 1))
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
									if (imgui_ColorPicker4_Float("##BuffListView_ProgressColorPicker", BuffListView_ProgressColor[0], BuffListView_ProgressColor[1], BuffListView_ProgressColor[2], BuffListView_ProgressColor[3], 0))
									{
										float[] newColors = imgui_ColorPicker_GetRGBA_Float("##BuffListView_ProgressColorPicker");
										BuffListView_ProgressColor[0] = newColors[0];
										BuffListView_ProgressColor[1] = newColors[1];
										BuffListView_ProgressColor[2] = newColors[2];
										BuffListView_ProgressColor[3] = newColors[3];

									}
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

			RefreshBuffInfo();

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
							imgui_DrawSpellIconByIconIndex(stats.iconID, debuffState.IconSize);
							if (buffState.NewBuffsTimeStamps.TryGetValue(stats.SpellID, out var ts))
							{
								Int64 timeDelta = Core.StopWatch.ElapsedMilliseconds - ts;



								long alpha = (Int64)(timeDelta * debuffState.FadeRatio);

								if (alpha > 255) alpha = 255;
								imgui_GetWindowDrawList_AddRectFilled(x, y, x + debuffState.IconSize, y + debuffState.IconSize, GetColor(255, 0, 0, 255 - (uint)alpha));

								if (timeDelta > debuffState.FadeTimeInMS) buffState.NewBuffsTimeStamps.Remove(stats.SpellID);

							}
							if (!String.IsNullOrWhiteSpace(stats.SimpleDuration))
							{
								float newX = x + (float)(debuffState.IconSize / 2) - (debuffState.FontSize);
								float newY = y + (float)((debuffState.IconSize) - (debuffState.FontSize * 2));

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
									imgui_Text($"SpellID: {stats.SpellID}");
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

			imgui_Text($"Buffs: ({buffState.BuffInfo.Count + debuffState.DebuffInfo.Count})");
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
					if (popup.BeginPopupContextItem($"##BuffgWindowSettingsPopup", 1))
					{
						if(imgui_MenuItem("Dock"))
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
						}

					}
				}
				
			}
			// Use List View if enabled, otherwise use icon grid
			if (buffState.ListView)
			{
				RenderBuffListView(buffState.BuffInfo, "E3HubBuffTableListView", buffState.IconSize, buffState.FadeRatio, buffState.FadeTimeInMS, buffState.NewBuffsTimeStamps);
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
								imgui_DrawSpellIconByIconIndex(stats.iconID, buffState.IconSize);

								if (buffState.NewBuffsTimeStamps.TryGetValue(stats.SpellID, out var ts))
								{
									Int64 timeDelta = Core.StopWatch.ElapsedMilliseconds - ts;

									long alpha = (Int64)(timeDelta * buffState.FadeRatio);

									if (alpha > 255) alpha = 255;
									imgui_GetWindowDrawList_AddRectFilled(x, y, x + buffState.IconSize, y + buffState.IconSize, GetColor(0, 255, 0, 255 - (uint)alpha));

									if (timeDelta > buffState.FadeTimeInMS) buffState.NewBuffsTimeStamps.Remove(stats.SpellID);

								}
								using (var popup = ImGUIPopUpContext.Aquire())
								{
									if (popup.BeginPopupContextItem($"BuffTableIconContext-{counter}", 1))
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
								if (popup.BeginPopupContextItem($"##GroupTableHeaderContext-{i}", 1))
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

								if (imgui_Selectable_WithFlags($"##row_selected_{rowCount}", is_row_selected, (int)(ImGuiSelectableFlags.ImGuiSelectableFlags_SpanAllColumns | ImGuiSelectableFlags.ImGuiSelectableFlags_AllowOverlap)))
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
											if (_spawns.TryByName(stats.Name, out var spawnsNav,true))
											{
												string command = $"/nav id {spawnsNav.ID}";
												E3ImGUI.MQCommandQueue.Enqueue(command);
											}
											break;
										case "Target":
										default:
											if (_spawns.TryByName(stats.Name, out var spawns, true))
											{
												string command = $"/target id {spawns.ID}";
												E3ImGUI.MQCommandQueue.Enqueue(command);
											}
											break;
									}

								}
								using (var popup = ImGUIPopUpContext.Aquire())
								{
									if (popup.BeginPopupContextItem($"##row_selected_context_{rowCount}", 1))
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
												if (_spawns.TryByName(stats.Name, out var spawns, true))
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
												if (_spawns.TryByName(stats.Name, out var spawns, true))
												{
													string command = $"/nav id {spawns.ID}";
													E3ImGUI.MQCommandQueue.Enqueue(command);
												}
											}
											if (imgui_MenuItem("Nav Toon to us"))
											{
												if (_spawns.TryByName(E3.CurrentName, out var spawns, true))
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
										imgui_ProgressBar(((float)stats.HPPercent / (float)100), 20, (int)widthOfColumn, "");
									}

									float[] barPos = imgui_GetItemRectMin();
									float[] barSize = imgui_GetItemRectSize();
									float[] textSize = imgui_CalcTextSize(stats.DisplayName);

									//this centers the text
									//float textPosX = barPos[0] + (barSize[0] - textSize[0]) * 0.5f;
									float textPosX = barPos[0];

									float textPosY = barPos[1] + (barSize[1] - textSize[1]) * 0.5f;
									imgui_GetWindowDrawList_AddText(textPosX, textPosY, GetColor(state.NameColor[0], state.NameColor[1], state.NameColor[2], state.NameColor[3]), stats.DisplayName);
								
									if (stats.PetHPPercent > 0)
									{
										imgui_SameLine(0, 0);
										imgui_SetCursorPosX(progressBarStartPosX);
										imgui_SetCursorPosY(progressBarStartPosY + barSize[1]-5);
										using (var style = PushStyle.Aquire())
										{
											style.PushStyleColor((int)ImGuiCol.PlotHistogram, state.PetHealthBarColor[0], state.PetHealthBarColor[1], state.PetHealthBarColor[2], state.PetHealthBarColor[3]);
											style.PushStyleColor((int)ImGuiCol.FrameBg, 0, 0, 0, 0f);
											float widthOfColumn = imgui_GetContentRegionAvailX();
											imgui_ProgressBar(((float)stats.PetHPPercent / (float)100), 5, (int)widthOfColumn, "");
										}
									}
									
								}
								else
								{
									imgui_SameLine(0);

									imgui_TextColored(state.NameColor[0], state.NameColor[1], state.NameColor[2], state.NameColor[3], stats.DisplayName);

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
			public Spell Spell;
			public Int32 SpellID = 0;
			public string Name;
			public string DisplayName { get; set; }
			public (float r, float g, float b) DisplayNameColor;
			public Int32 iconID;
			public string Duration { get; set; }
			public Int32 Duration_Value { get; set; }
			public Int32 MaxDuration_Value { get; set; }
			public string SimpleDuration { get; set; }
			public (float r, float g, float b) DurationColor;

			public string HitCount = String.Empty;
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
			public Int32 HPPercent = 0;
			public Int32 PetHPPercent = 0;
			public string HP { get; set; }
			public (float r, float g, float b) HPColor;

			public string Mana { get; set; }
			public (float r, float g, float b) ManaColor;

			public string Endurance { get; set; }
			public (float r, float g, float b) EndColor;

			public string Distance { get; set; }

			public (float r, float g, float b) DistanceColor;


			public string AggroPct { get; set; }

			public (float r, float g, float b) AggroColor;

			public string XtargetAggroPct { get; set; }

			public (float r, float g, float b) AggroXTargetColor;
			public string XtargetMinAggroPct { get; set; }

			public (float r, float g, float b) AggroMinXTargetColor;

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
