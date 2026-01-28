using E3Core.Processors;
using Google.Protobuf.WellKnownTypes;
using MonoCore;
using System;
using System.Collections.Generic;
using static E3Core.UI.Windows.Hud.HudHubWindow;
using static E3Core.UI.Windows.Hud.State_SongWindow;

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
			//State_HotbuttonsWindow
			return default(T);
		}
		public void ClearWindows()
		{



			
		}

	}
	public class State_HubWindow
	{
		public string WindowName = $"E3 Main Hud - {E3.CurrentName}-{E3.CurrentClass.ToString()}-{E3.ServerName}";
		private float _windowAlpha = 0.8f;
		public float WindowAlpha { get => E3.CharacterSettings.E3Hud_Hub_Alpha; set { E3.CharacterSettings.E3Hud_Hub_Alpha = value; IsDirty = true; } }
 		public bool ShowColumnHP { get => E3.CharacterSettings.E3Hud_Hub_ShowColumnHP; set { E3.CharacterSettings.E3Hud_Hub_ShowColumnHP = value; IsDirty = true; } }
		public bool ShowColumnEnd { get => E3.CharacterSettings.E3Hud_Hub_ShowColumnEnd; set { E3.CharacterSettings.E3Hud_Hub_ShowColumnEnd = value; IsDirty = true; } }
		public bool ShowColumnMana { get => E3.CharacterSettings.E3Hud_Hub_ShowColumnMana; set { E3.CharacterSettings.E3Hud_Hub_ShowColumnMana = value; IsDirty = true; } }
		public bool ShowColumnDistance { get => E3.CharacterSettings.E3Hud_Hub_ShowColumnDistance; set { E3.CharacterSettings.E3Hud_Hub_ShowColumnDistance = value; IsDirty = true; } }
		public string SelectedFont { get => E3.CharacterSettings.E3Hud_Hub_SelectedFont; set { E3.CharacterSettings.E3Hud_Hub_SelectedFont = value; IsDirty = true; } }

		public Int64 LastUpdated = 0;
		public Int64 LastUpdateInterval = 500;

		public List<TableRow_GroupInfo> GroupInfo = new List<TableRow_GroupInfo>();
		private bool showColumnEnd = true;
		public bool ShowAggro = true;
		public bool ShowAggroXTarget = true;

		public List<string> ColumNameBuffer = new List<string>();
		public float[] NameColors = { 0.95f, 0.85f, 0.35f, 1.0f };
		public string SelectedToonForBuffs = String.Empty;
		
		public int SelectedRow = -1;
		public bool IsDirty = false;

		public State_HubWindow()
		{
			NameColors[0] = E3.CharacterSettings.E3Hud_Hub_NameColorR;
			NameColors[1] = E3.CharacterSettings.E3Hud_Hub_NameColorG;
			NameColors[2] = E3.CharacterSettings.E3Hud_Hub_NameColorB;
			NameColors[3] = E3.CharacterSettings.E3Hud_Hub_NameColorA;
			IsDirty = false;
		}


		public void UpdateSettings_WithoutSaving()
		{
			E3.CharacterSettings.E3Hud_Hub_NameColorR = NameColors[0];
			E3.CharacterSettings.E3Hud_Hub_NameColorG = NameColors[1];
			E3.CharacterSettings.E3Hud_Hub_NameColorB = NameColors[2];
			E3.CharacterSettings.E3Hud_Hub_NameColorA = NameColors[3];
			IsDirty = false;

		}


	}
	public class State_BuffWindow
	{
		public float WindowAlpha { get => E3.CharacterSettings.E3Hud_Hub_Buff_Alpha; set { E3.CharacterSettings.E3Hud_Hub_Buff_Alpha = value; IsDirty = true; } }
		public bool Detached { get => E3.CharacterSettings.E3Hud_Hub_Buff_Detached; set { E3.CharacterSettings.E3Hud_Hub_Buff_Detached = value; IsDirty = true; } }
		public string SelectedFont { get => E3.CharacterSettings.E3Hud_Hub_Buff_SelectedFont; set { E3.CharacterSettings.E3Hud_Hub_Buff_SelectedFont = value; IsDirty = true; } }
		public int IconSize { get => E3.CharacterSettings.E3Hud_Hub_Buff_IconSize; set { E3.CharacterSettings.E3Hud_Hub_Buff_IconSize = value; IsDirty = true; } }

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
		private bool _detached = false;
		private float _windowAlpha = 0.8f;

		public string WindowName = $"E3 Hotbutton Hud - {E3.CurrentName}-{E3.CurrentClass.ToString()}-{E3.ServerName}";
		public Int32 ButtonSizeX = 50;
		public Int32 ButtonSizeY = 30;
		public Int32 FontSize = 8;
		public string SelectedFont = "arial-14";
		public bool IsDirty = false;
		
		public bool Detached { get => _detached; set { _detached = value; IsDirty = true; } }
		public float WindowAlpha { get => _windowAlpha; set { _windowAlpha = value; IsDirty = true; } }

		public State_HotbuttonsWindow()
		{
			_windowAlpha = E3.CharacterSettings.E3Hud_Hub_HotButtons_Alpha;
			_detached = E3.CharacterSettings.E3Hud_Hub_HotButtons_Detached;
			SelectedFont = E3.CharacterSettings.E3Hud_Hub_HotButtons_SelectedFont;
			ButtonSizeX = E3.CharacterSettings.E3Hud_Hub_HotButtons_ButtonSizeX;
			ButtonSizeY = E3.CharacterSettings.E3Hud_Hub_HotButtons_ButtonSizeY;
			IsDirty = false;
		}

		public void UpdateSettings_WithoutSaving()
		{
			E3.CharacterSettings.E3Hud_Hub_HotButtons_Alpha = WindowAlpha;
			E3.CharacterSettings.E3Hud_Hub_HotButtons_SelectedFont = SelectedFont;
			E3.CharacterSettings.E3Hud_Hub_HotButtons_ButtonSizeX = ButtonSizeX;
			E3.CharacterSettings.E3Hud_Hub_HotButtons_ButtonSizeY = ButtonSizeY;
			E3.CharacterSettings.E3Hud_Hub_HotButtons_Detached = Detached;
			IsDirty = false;
		}
	}

	public class State_DebuffWindow
	{

		public float WindowAlpha { get => E3.CharacterSettings.E3Hud_Hub_Debuff_Alpha; set { E3.CharacterSettings.E3Hud_Hub_Debuff_Alpha = value; IsDirty = true; } }
		public bool Detached { get => E3.CharacterSettings.E3Hud_Hub_Debuff_Detached; set { E3.CharacterSettings.E3Hud_Hub_Debuff_Detached = value; IsDirty = true; } }
		public string SelectedFont { get => E3.CharacterSettings.E3Hud_Hub_Debuff_SelectedFont; set { E3.CharacterSettings.E3Hud_Hub_Debuff_SelectedFont = value; IsDirty = true; } }
		public int IconSize { get => E3.CharacterSettings.E3Hud_Hub_Debuff_IconSize; set { E3.CharacterSettings.E3Hud_Hub_Debuff_IconSize = value; IsDirty = true; } }

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
}
