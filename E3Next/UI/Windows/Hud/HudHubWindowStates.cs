using E3Core.Processors;
using Google.Protobuf.WellKnownTypes;
using Microsoft.SqlServer.Server;
using MonoCore;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using static E3Core.UI.Windows.Hud.HudHubWindow;
using static E3Core.UI.Windows.Hud.State_SongWindow;
using static System.Windows.Forms.AxHost;

namespace E3Core.UI.Windows.Hud
{
	public class HudHubWindowStates
	{
		public HudHubWindowStates()
		{
			//set all initial windows to not show
			if (Core._MQ2MonoVersion >= 0.37m) ClearWindows();
		}
		private State_HubWindow _hubWindowState = new State_HubWindow();
		private State_BuffWindow _buffWindowState = new State_BuffWindow();
		private State_DebuffWindow _debuffWindowState = new State_DebuffWindow();
		private State_SongWindow _songWindowState = new State_SongWindow();
		private State_HotbuttonsWindow _hotbuttonWindowState = new State_HotbuttonsWindow();
		private State_PlayerInfoWindow _playerInfoWindowState = new State_PlayerInfoWindow();
		private State_TargetInfoWindow _targetInfoWindowState = new State_TargetInfoWindow();

		public T GetState<T>()
		{
			var type = typeof(T);
			if (type == typeof(State_HubWindow))
			{
				return (T)(object)_hubWindowState;
			}
			if (type == typeof(State_BuffWindow))
			{
				return (T)(object)_buffWindowState;
			}
			if (type == typeof(State_SongWindow))
			{
				return (T)(object)_songWindowState;
			}
			if (type == typeof(State_DebuffWindow))
			{
				return (T)(object)_debuffWindowState;
			}
			if (type == typeof(State_HotbuttonsWindow))
			{
				return (T)(object)_hotbuttonWindowState;
			}
			if (type == typeof(State_PlayerInfoWindow))
			{
				return (T)(object)_playerInfoWindowState;
			}
			if (type == typeof(State_TargetInfoWindow))
			{
				return (T)(object)_targetInfoWindowState;
			}
			return default(T);
		}
		public void ClearWindows()
		{



			
		}

	}
	public class State_HubWindow
	{
		public HashSet<String> GroupMembersAdded = new HashSet<string>();
		public string WindowName = $"E3 Main Hud - {E3.CurrentName}-{E3.CurrentClass.ToString()}-{E3.ServerName}";
		private float _windowAlpha = 0.8f;
		public float WindowAlpha { get => E3.CharacterSettings.E3Hud_Hub_Alpha; set { E3.CharacterSettings.E3Hud_Hub_Alpha = value; IsDirty = true; } }
 		public bool ShowColumnHP { get => E3.CharacterSettings.E3Hud_Hub_ShowColumnHP; set { E3.CharacterSettings.E3Hud_Hub_ShowColumnHP = value; IsDirty = true; } }
		public bool ShowColumnEnd { get => E3.CharacterSettings.E3Hud_Hub_ShowColumnEnd; set { E3.CharacterSettings.E3Hud_Hub_ShowColumnEnd = value; IsDirty = true; } }
		public bool ShowColumnMana { get => E3.CharacterSettings.E3Hud_Hub_ShowColumnMana; set { E3.CharacterSettings.E3Hud_Hub_ShowColumnMana = value; IsDirty = true; } }
		public bool ShowColumnDistance { get => E3.CharacterSettings.E3Hud_Hub_ShowColumnDistance; set { E3.CharacterSettings.E3Hud_Hub_ShowColumnDistance = value; IsDirty = true; } }
		public string SelectedFont { get => E3.CharacterSettings.E3Hud_Hub_SelectedFont; set { E3.CharacterSettings.E3Hud_Hub_SelectedFont = value; IsDirty = true; } }
		public bool ShowColumnAggro { get => E3.CharacterSettings.E3Hud_Hub_ShowColumnAggro; set { E3.CharacterSettings.E3Hud_Hub_ShowColumnAggro = value; IsDirty = true; } }
		public bool ShowColumnAggroXTarget { get => E3.CharacterSettings.E3Hud_Hub_ShowColumnAggroXTarget; set { E3.CharacterSettings.E3Hud_Hub_ShowColumnAggroXTarget = value;IsDirty = true; } }
		public bool ShowColumnAggroMinXTarget { get => E3.CharacterSettings.E3Hud_Hub_ShowColumnAggroMinXTarget; set { E3.CharacterSettings.E3Hud_Hub_ShowColumnAggroMinXTarget = value; IsDirty = true; } }
		public bool Locked { get => E3.CharacterSettings.E3Hud_Hub_Locked; set { E3.CharacterSettings.E3Hud_Hub_Locked = value; IsDirty = true; } }
		public bool DisplayHPBar { get => E3.CharacterSettings.E3Hud_Hub_DisplayHPBar; set { E3.CharacterSettings.E3Hud_Hub_DisplayHPBar = value; IsDirty = true; } }
		public string LeftClickAction { get => E3.CharacterSettings.E3Hud_Hub_LeftClickAction; set { E3.CharacterSettings.E3Hud_Hub_LeftClickAction = value; IsDirty = true; } }
		public bool ShowTickTimer { get => E3.CharacterSettings.E3Hud_Hub_ShowTickTimer; set { E3.CharacterSettings.E3Hud_Hub_ShowTickTimer = value; IsDirty = true; } }
		public bool ShowHotButtons { get => E3.CharacterSettings.E3Hud_Hub_ShowHotButtons; set { E3.CharacterSettings.E3Hud_Hub_ShowHotButtons = value; IsDirty = true; } }
		public bool ShowPlayerInfo { get => E3.CharacterSettings.E3Hud_Hub_ShowPlayerInfo; set { E3.CharacterSettings.E3Hud_Hub_ShowPlayerInfo = value; IsDirty = true; } }
		public bool ShowTargetInfo { get => E3.CharacterSettings.E3Hud_Hub_ShowTargetInfo; set { E3.CharacterSettings.E3Hud_Hub_ShowTargetInfo = value; IsDirty = true; } }
		public float[] NameColor { get => E3.CharacterSettings.E3Hud_Hub_RGBA_NameColor;  }
		public float[] HealthBarColor { get => E3.CharacterSettings.E3Hud_Hub_RGBA_HealthBar; }
		public float[] PetHealthBarColor { get => E3.CharacterSettings.E3Hud_Hub_RGBA_PetHealthBar; }

		public string[] LeftClickActions = { "Target", "Foreground", "ViewBuffs", "NavToToon" };
		public Int64 LastUpdated = 0;
		public Int64 LastUpdateInterval = 500;

		public List<TableRow_GroupInfo> GroupInfo = new List<TableRow_GroupInfo>();

		public List<string> ColumNameBuffer = new List<string>();
		private float[] nameColors = { 0.95f, 0.85f, 0.35f, 1.0f };
		public string SelectedToonForBuffs = String.Empty;

		public int SelectedRow = -1;
		public bool IsDirty = false;

		public State_HubWindow()
		{
				IsDirty = false;
		}


		public void UpdateSettings_WithoutSaving()
		{
			IsDirty = false;
		}
	}
	public class State_BuffWindow
	{
		public float WindowAlpha { get => E3.CharacterSettings.E3Hud_Hub_Buff_Alpha; set { E3.CharacterSettings.E3Hud_Hub_Buff_Alpha = value; IsDirty = true; } }
		public bool Detached { get => E3.CharacterSettings.E3Hud_Hub_Buff_Detached; set { E3.CharacterSettings.E3Hud_Hub_Buff_Detached = value; IsDirty = true; } }
		public string SelectedFont { get => E3.CharacterSettings.E3Hud_Hub_Buff_SelectedFont; set { E3.CharacterSettings.E3Hud_Hub_Buff_SelectedFont = value; IsDirty = true; } }
		public int IconSize { get => E3.CharacterSettings.E3Hud_Hub_Buff_IconSize; set { E3.CharacterSettings.E3Hud_Hub_Buff_IconSize = value; IsDirty = true; } }
		public bool Locked { get => E3.CharacterSettings.E3Hud_Hub_Buff_Locked; set { E3.CharacterSettings.E3Hud_Hub_Buff_Locked = value; IsDirty = true; } }
		public bool ListView { get => E3.CharacterSettings.E3Hud_Hub_Buff_ListView; set { E3.CharacterSettings.E3Hud_Hub_Buff_ListView = value; IsDirty = true; } }
		public bool ShowProgressBars { get => E3.CharacterSettings.E3Hud_Hub_Buff_ShowProgressBars; set { E3.CharacterSettings.E3Hud_Hub_Buff_ShowProgressBars = value; IsDirty = true; } }

		public int FadeTimeInMS
		{
			get { return E3.CharacterSettings.E3Hud_Hub_Buff_FadeTimeInMS; }
			set
			{
				E3.CharacterSettings.E3Hud_Hub_Buff_FadeTimeInMS = value;
				if (fadeTimeInMS < 1) fadeTimeInMS = 1;
				FadeRatio = ((double)255) / value;
				IsDirty = true;
			}
		}
		public bool IsDirty = false;

		public HashSet<Int32> PreviousBuffs = new HashSet<Int32>();
		public Dictionary<Int32, Int64> NewBuffsTimeStamps = new Dictionary<Int32, Int64>();
		public string PreviousBuffInfo = string.Empty;
		
		public string WindowName = $"E3 Buff Hud - {E3.CurrentName}-{E3.CurrentClass.ToString()}-{E3.ServerName}";
		public Int64 LastUpdated = 0;
		public Int64 LastUpdateInterval = 500;
		public List<TableRow_BuffInfo> BuffInfo = new List<TableRow_BuffInfo>();

		private Int32 iconSize = 40;
		public Int32 FontSize = 8;
		private int fadeTimeInMS = 1000;
		public double FadeRatio = 0;
	
		public State_BuffWindow()
		{
			FadeRatio = ((double)255) / E3.CharacterSettings.E3Hud_Hub_Buff_FadeTimeInMS;
			IsDirty = false;
		}

		public void UpdateSettings_WithoutSaving()
		{
			IsDirty = false;
		}
	}
	public class State_SongWindow
	{
		private bool _detached = false;
		private float _windowAlpha = 0.8f;

		public float WindowAlpha { get => E3.CharacterSettings.E3Hud_Hub_Song_Alpha; set { E3.CharacterSettings.E3Hud_Hub_Song_Alpha = value; IsDirty = true; } }
		public bool Detached { get => E3.CharacterSettings.E3Hud_Hub_Song_Detached; set { E3.CharacterSettings.E3Hud_Hub_Song_Detached = value; IsDirty = true; } }
		public string SelectedFont { get => E3.CharacterSettings.E3Hud_Hub_Song_SelectedFont; set { E3.CharacterSettings.E3Hud_Hub_Song_SelectedFont = value; IsDirty = true; } }
		public int IconSize { get => E3.CharacterSettings.E3Hud_Hub_Song_IconSize; set { E3.CharacterSettings.E3Hud_Hub_Song_IconSize = value; IsDirty = true; } }
		public bool Locked { get => E3.CharacterSettings.E3Hud_Hub_Song_Locked; set { E3.CharacterSettings.E3Hud_Hub_Song_Locked = value; IsDirty = true; } }
		public bool ListView { get => E3.CharacterSettings.E3Hud_Hub_Song_ListView; set { E3.CharacterSettings.E3Hud_Hub_Song_ListView = value; IsDirty = true; } }
		public bool ShowProgressBars { get => E3.CharacterSettings.E3Hud_Hub_Song_ShowProgressBars; set { E3.CharacterSettings.E3Hud_Hub_Song_ShowProgressBars = value; IsDirty = true; } }

		public int FadeTimeInMS
		{
			get { return E3.CharacterSettings.E3Hud_Hub_Song_FadeTimeInMS; }
			set
			{
				E3.CharacterSettings.E3Hud_Hub_Song_FadeTimeInMS = value;
				if (fadeTimeInMS < 1) fadeTimeInMS = 1;
				FadeRatio = ((double)255) / value;
				IsDirty = true;
			}
		}
		public List<TableRow_BuffInfo> SongInfo = new List<TableRow_BuffInfo>();
		public string WindowName = $"E3 Song Hud - {E3.CurrentName}-{E3.CurrentClass.ToString()}-{E3.ServerName}";
		public Int32 FontSize = 8;
		private int fadeTimeInMS = 1000;
		public double FadeRatio = 0;
		public bool IsDirty = false;
		
		public State_SongWindow()
		{
			FadeRatio = ((double)255) / E3.CharacterSettings.E3Hud_Hub_Song_FadeTimeInMS;
			IsDirty = false;
		}
		public void UpdateSettings_WithoutSaving()
		{
			IsDirty = false;
		}
	}
	public class State_HotbuttonsWindow
	{	
		public string WindowName = $"E3 Hotbutton Hud - {E3.CurrentName}-{E3.CurrentClass.ToString()}-{E3.ServerName}";
		public Int32 FontSize = 8;
		public bool IsDirty = false;
		public float WindowAlpha { get => E3.CharacterSettings.E3Hud_Hub_HotButtons_Alpha; set { E3.CharacterSettings.E3Hud_Hub_HotButtons_Alpha = value; IsDirty = true; } }
		public bool Detached { get => E3.CharacterSettings.E3Hud_Hub_HotButtons_Detached; set { E3.CharacterSettings.E3Hud_Hub_HotButtons_Detached = value; IsDirty = true; } }
		public string SelectedFont { get => E3.CharacterSettings.E3Hud_Hub_HotButtons_SelectedFont; set { E3.CharacterSettings.E3Hud_Hub_HotButtons_SelectedFont = value; IsDirty = true; } }

		public int ButtonSizeX { get => E3.CharacterSettings.E3Hud_Hub_HotButtons_ButtonSizeX; set {E3.CharacterSettings.E3Hud_Hub_HotButtons_ButtonSizeX = value; IsDirty=true; } }
		public int ButtonSizeY { get => E3.CharacterSettings.E3Hud_Hub_HotButtons_ButtonSizeY; set { E3.CharacterSettings.E3Hud_Hub_HotButtons_ButtonSizeY = value; IsDirty = true; } }
		public bool Locked { get => E3.CharacterSettings.E3Hud_Hub_HotButtons_Locked; set { E3.CharacterSettings.E3Hud_Hub_HotButtons_Locked = value; IsDirty = true; } }

		public State_HotbuttonsWindow()
		{
			IsDirty = false;
		}

		public void UpdateSettings_WithoutSaving()
		{
			IsDirty = false;
		}
	}

	public class State_DebuffWindow
	{

		public float WindowAlpha { get => E3.CharacterSettings.E3Hud_Hub_Debuff_Alpha; set { E3.CharacterSettings.E3Hud_Hub_Debuff_Alpha = value; IsDirty = true; } }
		public bool Detached { get => E3.CharacterSettings.E3Hud_Hub_Debuff_Detached; set { E3.CharacterSettings.E3Hud_Hub_Debuff_Detached = value; IsDirty = true; } }
		public string SelectedFont { get => E3.CharacterSettings.E3Hud_Hub_Debuff_SelectedFont; set { E3.CharacterSettings.E3Hud_Hub_Debuff_SelectedFont = value; IsDirty = true; } }
		public int IconSize { get => E3.CharacterSettings.E3Hud_Hub_Debuff_IconSize; set { E3.CharacterSettings.E3Hud_Hub_Debuff_IconSize = value; IsDirty = true; } }
		public bool Locked { get => E3.CharacterSettings.E3Hud_Hub_Debuff_Locked; set { E3.CharacterSettings.E3Hud_Hub_Debuff_Locked = value; IsDirty = true; } }
		public bool ListView { get => E3.CharacterSettings.E3Hud_Hub_Debuff_ListView; set { E3.CharacterSettings.E3Hud_Hub_Debuff_ListView = value; IsDirty = true; } }

		public int FadeTimeInMS
		{
			get { return E3.CharacterSettings.E3Hud_Hub_Debuff_FadeTimeInMS; }
			set
			{
				E3.CharacterSettings.E3Hud_Hub_Debuff_FadeTimeInMS = value;
				if (fadeTimeInMS < 1) fadeTimeInMS = 1;
				FadeRatio = ((double)255) / value;
				IsDirty = true;
			}
		}
		public bool IsDirty = false;

		public List<TableRow_BuffInfo> DebuffInfo = new List<TableRow_BuffInfo>();
		public string WindowName = $"E3 Debuff Hud - {E3.CurrentName}-{E3.CurrentClass.ToString()}-{E3.ServerName}";
		public Int32 FontSize = 8;
		private int fadeTimeInMS = 1000;
		public double FadeRatio = 0;
		public State_DebuffWindow()
		{
			FadeRatio = ((double)255) / E3.CharacterSettings.E3Hud_Hub_Debuff_FadeTimeInMS;
			IsDirty = false;
		}

		public void UpdateSettings_WithoutSaving()
		{

			IsDirty = false;

		}

	}
	public class State_PlayerInfoWindow
	{
		static string _playerInfoDisplay = String.Empty;
		static Int32 _playerInfoDispleyLevel = 0;

		public string WindowName = $"E3 PlayerInfo Hud - {E3.CurrentName}-{E3.CurrentClass.ToString()}-{E3.ServerName}";
		public float WindowAlpha { get => E3.CharacterSettings.E3Hud_Hub_PlayerInfo_Alpha; set { E3.CharacterSettings.E3Hud_Hub_PlayerInfo_Alpha = value; IsDirty = true; } }
		public bool Detached { get => E3.CharacterSettings.E3Hud_Hub_PlayerInfo_Detached; set { E3.CharacterSettings.E3Hud_Hub_PlayerInfo_Detached = value; IsDirty = true; } }
		public bool Locked { get => E3.CharacterSettings.E3Hud_Hub_PlayerInfo_Locked; set { E3.CharacterSettings.E3Hud_Hub_PlayerInfo_Locked = value; IsDirty = true; } }
		public string DisplayHP;
		public int DisplayHP_Value;
		public string DisplayMana;
		public int DisplayMana_Value;
		public string DisplayEnd;
		public int DisplayEnd_Value;
		public string DisplayExp;
		public Decimal DisplayExp_Value;
		public string DisplayAA;
		public int DisplayAA_Value;
		public string PreviousDisc = String.Empty;
		public string ActiveDisc = String.Empty;
		public Int64 PreviousDiscTimeStamp = 0;
		public String Display_ActiveDiscTimeleft = String.Empty;
		public Decimal ActiveDiscPercentLeft = 0;
		public string DisplayPlayerInfo { get => _playerInfoDisplay; set => _playerInfoDisplay = value; }
		public int DisplayPlayerInfo_Level { get => _playerInfoDispleyLevel; set => _playerInfoDispleyLevel = value; }
		public Int64 PlayerInfoLastUpdated = 0;
		public Int64 PlayerInfoUpdateInterval = 500;
		public int PlayerLevel = 0;
		public int PlayerHP = 0;
		public int PlayerMana = 0;
		public int PlayerEnd = 0;
		public Decimal PlayerExp = 0m;
		public int PlayerAAPoints = 0;
		public (float r, float g, float b) PlayerHPColor;
		public (float r, float g, float b) PlayerManaColor;
		public (float r, float g, float b) PlayerEndColor;
		public float[] DiscProgressBarColor { get => E3.CharacterSettings.E3Hud_Hub_PlayerInfo_RGBA_DiscProgressBar; }

		public bool IsDirty = false;
		public string Display = "";
		public State_PlayerInfoWindow()
		{
			IsDirty = false;
		}
		public void UpdateSettings_WithoutSaving()
		{
			IsDirty = false;
		}
	}
	public class State_TargetInfoWindow
	{
	
		public string WindowName = $"E3 TargetInfo Hud - {E3.CurrentName}-{E3.CurrentClass.ToString()}-{E3.ServerName}";
		public float WindowAlpha { get => E3.CharacterSettings.E3Hud_Hub_TargetInfo_Alpha; set { E3.CharacterSettings.E3Hud_Hub_TargetInfo_Alpha = value; IsDirty = true; } }
		public bool Detached { get => E3.CharacterSettings.E3Hud_Hub_TargetInfo_Detached; set { E3.CharacterSettings.E3Hud_Hub_TargetInfo_Detached = value; IsDirty = true; } }
		public bool Locked { get => E3.CharacterSettings.E3Hud_Hub_TargetInfo_Locked; set { E3.CharacterSettings.E3Hud_Hub_TargetInfo_Locked = value; IsDirty = true; } }
		public bool IsDirty = false;
		public Int64 TargetInfoLastUpdated = 0;
		public Int64 TargetInfoUpdateInterval = 100;
		public bool HasTarget = false;
		public string TargetName = string.Empty;
		public string PreviousTargetName = String.Empty;
		public float TargetNameSize = 0;
		public int TargetHP = 0;
		public int TargetLevel = 0;
		public string TargetClassName = string.Empty;
		public double TargetDistance = 0;
		public string TargetDistanceString;
		public string Display_LevelAndClassString = String.Empty;
		public (float r, float g, float b) TargetNameColor;
		public (float r, float g, float b) TargetDistanceColor;
		public List<TableRow_BuffInfo> TargetBuffs = new List<TableRow_BuffInfo>();
		public Int64 TargetBuffLastUpdated = 0;
		public Int64 TargetBuffUpdateInterval = 500;
		public int PreviousTargetID = 0;
		public string NoTargetText = "No Target";
		public float NoTargetTextWidth = 0;
		public String Display_MyAggroPercent = String.Empty;
		public Decimal SecondAggroPercent = Decimal.Zero;
		public string SecondAggroName = String.Empty;
		public Decimal MyAggroPercent = Decimal.Zero;
		public string SecondaryAggroOrTopAggroNotUs = String.Empty;
		public string Display_SecondAggroName = String.Empty;
		public float Display_SecondAggroNameSize = 0;
		public float Display_CurrentNameSize = 0;

		public State_TargetInfoWindow()
		{
			IsDirty = false;
		}
		public void UpdateSettings_WithoutSaving()
		{
			IsDirty = false;
		}
	}
}
