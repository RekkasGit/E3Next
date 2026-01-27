using E3Core.Data;
using E3Core.Processors;
using E3Core.UI.Windows.CharacterSettings;
using IniParser.Model;
using MonoCore;
using System;
using System.Collections.Generic;
using static E3Core.UI.Windows.Hud.HudHubWindow;
using static MonoCore.E3ImGUI;

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
			return default(T);
		}
		public void ClearWindows()
		{



			
		}

	}
	public class State_HubWindow
	{
		public string WindowName = "E3 Main Hud";
		private float _windowAlpha = 0.8f;
		public float WindowAlpha { get => _windowAlpha; set { _windowAlpha = value; IsDirty = true; } }
		public Int64 LastUpdated = 0;
		public Int64 LastUpdateInterval = 500;

		public List<TableRow_GroupInfo> GroupInfo = new List<TableRow_GroupInfo>();
		public bool ShowColumnHP = true;
		public bool ShowColumnEnd = true;
		public bool ShowColumnMana = true;
		public bool ShowColumnDistance = true;

		public List<string> ColumNameBuffer = new List<string>();
		public float[] NameColors = { 0.95f, 0.85f, 0.35f, 1.0f };
		public string SelectedToonForBuffs = String.Empty;
		public string SelectedFont = "robo";
		public int SelectedRow = -1;
		public bool IsDirty = false;

		public State_HubWindow()
		{

			WindowAlpha = E3.CharacterSettings.E3Hud_Hub_Alpha;
			ShowColumnHP = E3.CharacterSettings.E3Hud_Hub_ShowColumnHP;
			ShowColumnEnd = E3.CharacterSettings.E3Hud_Hub_ShowColumnEnd;
			ShowColumnMana = E3.CharacterSettings.E3Hud_Hub_ShowColumnMana;
			ShowColumnDistance = E3.CharacterSettings.E3Hud_Hub_ShowColumnDistance;
			SelectedFont = E3.CharacterSettings.E3Hud_Hub_SelectedFont;
			NameColors[0] = E3.CharacterSettings.E3Hud_Hub_NameColorR;
			NameColors[1] = E3.CharacterSettings.E3Hud_Hub_NameColorG;
			NameColors[2] = E3.CharacterSettings.E3Hud_Hub_NameColorB;
			NameColors[3] = E3.CharacterSettings.E3Hud_Hub_NameColorA;

			IsDirty = false;
		}


		public void UpdateSettings_WithoutSaving()
		{
			E3.CharacterSettings.E3Hud_Hub_Alpha = WindowAlpha;
			E3.CharacterSettings.E3Hud_Hub_ShowColumnHP= ShowColumnHP;
			E3.CharacterSettings.E3Hud_Hub_ShowColumnEnd = ShowColumnEnd;
			E3.CharacterSettings.E3Hud_Hub_ShowColumnMana = ShowColumnMana;
			E3.CharacterSettings.E3Hud_Hub_ShowColumnDistance = ShowColumnDistance;
			E3.CharacterSettings.E3Hud_Hub_SelectedFont = SelectedFont;
			E3.CharacterSettings.E3Hud_Hub_NameColorR = NameColors[0];
			E3.CharacterSettings.E3Hud_Hub_NameColorG = NameColors[1];
			E3.CharacterSettings.E3Hud_Hub_NameColorB = NameColors[2];
			E3.CharacterSettings.E3Hud_Hub_NameColorA = NameColors[3];
			IsDirty = false;

		}


	}
	public class State_BuffWindow
	{
		private bool _deattached = false;
		private float _windowAlpha = 0.8f; 
		public float WindowAlpha { get => _windowAlpha; set { _windowAlpha = value; IsDirty = true; } }
		public bool DeAttached { get => _deattached; set { _deattached = value; IsDirty = true; } }
	
		public bool IsDirty = false;

		public HashSet<Int32> PreviousBuffs = new HashSet<Int32>();
		public Dictionary<Int32, Int64> NewBuffsTimeStamps = new Dictionary<Int32, Int64>();
		public string PreviousBuffInfo = string.Empty;
		
		public string WindowName = "E3 Buff Hud";
		public Int64 LastUpdated = 0;
		public Int64 LastUpdateInterval = 500;
		public List<TableRow_BuffInfo> BuffInfo = new List<TableRow_BuffInfo>();

		public Int32 IconSize = 40;
		public Int32 FontSize = 8;
		private int fadeTimeInMS = 1000;
		public double FadeRatio = 0;
		public string SelectedFont = "robo";

	

		public int FadeTimeInMS { 
			get { return fadeTimeInMS; } 
			set  { 
				fadeTimeInMS = value;
				if (fadeTimeInMS < 1) fadeTimeInMS = 1;
				FadeRatio = ((double)255) / value; 
			} 
		}
		public State_BuffWindow()
		{
			_windowAlpha = E3.CharacterSettings.E3Hud_Hub_Buff_Alpha;
			FadeTimeInMS = E3.CharacterSettings.E3Hud_Hub_Buff_FadeTimeInMS;
			SelectedFont = E3.CharacterSettings.E3Hud_Hub_Buff_SelectedFont;
			IconSize = E3.CharacterSettings.E3Hud_Hub_Buff_IconSize;
			_deattached = E3.CharacterSettings.E3Hud_Hub_Buff_DeAttached;
			IsDirty = false;
		}

		public void UpdateSettings_WithoutSaving()
		{

			E3.CharacterSettings.E3Hud_Hub_Buff_Alpha = WindowAlpha;
			E3.CharacterSettings.E3Hud_Hub_Buff_FadeTimeInMS = FadeTimeInMS;
			E3.CharacterSettings.E3Hud_Hub_Buff_SelectedFont = SelectedFont;
			E3.CharacterSettings.E3Hud_Hub_Buff_IconSize = IconSize;
			E3.CharacterSettings.E3Hud_Hub_Buff_DeAttached = DeAttached;
			IsDirty = false;
		}
	}
	public class State_SongWindow
	{
		private bool _deattached = false;
		private float _windowAlpha = 0.8f;

		public List<TableRow_BuffInfo> SongInfo = new List<TableRow_BuffInfo>();
		public string WindowName = "E3 Song Hud";
		public Int32 IconSize = 40;
		public Int32 FontSize = 8;
		private int fadeTimeInMS = 1000;
		public double FadeRatio = 0;
		public string SelectedFont = "robo";
		public bool IsDirty = false;
		public int FadeTimeInMS
		{
			get { return fadeTimeInMS; }
			set
			{
				fadeTimeInMS = value;
				if (fadeTimeInMS < 1) fadeTimeInMS = 1;
				FadeRatio = ((double)255) / value;
			}
		}

		public bool DeAttached { get => _deattached; set { _deattached = value; IsDirty = true; } }
		public float WindowAlpha { get => _windowAlpha; set { _windowAlpha = value; IsDirty = true; } }

		public State_SongWindow()
		{
			_windowAlpha = E3.CharacterSettings.E3Hud_Hub_Song_Alpha;
			_deattached = E3.CharacterSettings.E3Hud_Hub_Song_DeAttached;

			FadeTimeInMS = E3.CharacterSettings.E3Hud_Hub_Song_FadeTimeInMS;
			SelectedFont = E3.CharacterSettings.E3Hud_Hub_Song_SelectedFont;
			IconSize = E3.CharacterSettings.E3Hud_Hub_Song_IconSize;
			IsDirty = false;
		}

		public void UpdateSettings_WithoutSaving()
		{

			E3.CharacterSettings.E3Hud_Hub_Song_Alpha = WindowAlpha;
			E3.CharacterSettings.E3Hud_Hub_Song_FadeTimeInMS = FadeTimeInMS;
			E3.CharacterSettings.E3Hud_Hub_Song_SelectedFont = SelectedFont;
			E3.CharacterSettings.E3Hud_Hub_Song_IconSize = IconSize;
			E3.CharacterSettings.E3Hud_Hub_Song_DeAttached = DeAttached;
			IsDirty = false;

		}
	}

	public class State_DebuffWindow
	{

		private bool _deattached = false;
		private float _windowAlpha = 0.8f;
		public bool DeAttached { get => _deattached; set { _deattached = value; IsDirty = true; } }
		public float WindowAlpha { get => _windowAlpha; set { _windowAlpha = value; IsDirty = true; } }
		public bool IsDirty = false;

		public List<TableRow_BuffInfo> DebuffInfo = new List<TableRow_BuffInfo>();
		public string WindowName = "E3 Debuff Hud";
		public Int32 IconSize = 40;
		public Int32 FontSize = 8;
		private int fadeTimeInMS = 1000;
		public double FadeRatio = 0;
		public string SelectedFont = "robo";
		public int FadeTimeInMS
		{
			get { return fadeTimeInMS; }
			set
			{
				fadeTimeInMS = value;
				if (fadeTimeInMS < 1) fadeTimeInMS = 1;
				FadeRatio = ((double)255) / value;
				IsDirty = true;

			}
		}
		public State_DebuffWindow()
		{
			_windowAlpha = E3.CharacterSettings.E3Hud_Hub_Debuff_Alpha;
			FadeTimeInMS = E3.CharacterSettings.E3Hud_Hub_Debuff_FadeTimeInMS;
			SelectedFont = E3.CharacterSettings.E3Hud_Hub_Debuff_SelectedFont;
			IconSize = E3.CharacterSettings.E3Hud_Hub_Debuff_IconSize;
			_deattached = E3.CharacterSettings.E3Hud_Hub_Debuff_DeAttached;
			IsDirty = false;
		}

		public void UpdateSettings_WithoutSaving()
		{

			E3.CharacterSettings.E3Hud_Hub_Debuff_Alpha = WindowAlpha;
			E3.CharacterSettings.E3Hud_Hub_Debuff_FadeTimeInMS = FadeTimeInMS;
			E3.CharacterSettings.E3Hud_Hub_Debuff_SelectedFont = SelectedFont;
			E3.CharacterSettings.E3Hud_Hub_Debuff_IconSize = IconSize;
			E3.CharacterSettings.E3Hud_Hub_Debuff_DeAttached = DeAttached;
			IsDirty = false;

		}

	}
}
