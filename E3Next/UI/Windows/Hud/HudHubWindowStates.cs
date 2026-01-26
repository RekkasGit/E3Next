using E3Core.Data;
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
		public float WindowAlpha = 0.8f;
		public Int64 LastUpdated = 0;
		public Int64 LastUpdateInterval = 500;

		public List<TableRow_GroupInfo> GroupInfo = new List<TableRow_GroupInfo>();

	}
	public class State_BuffWindow
	{
		public bool DeAttached = false;
		public string WindowName = "E3 Buff Hud";
		public float WindowAlpha = 0.8f;
		public Int64 LastUpdated = 0;
		public Int64 LastUpdateInterval = 500;
		public List<TableRow_BuffInfo> BuffInfo = new List<TableRow_BuffInfo>();

		public Int32 IconSize = 40;
		public Int32 FontSize = 8;
		private int fadeTimeInMS = 1000;
		public double FadeRatio = 0;

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
			FadeRatio=((double)255) / FadeTimeInMS;
		}

	}
	public class State_SongWindow
	{
		public bool DeAttached = false;
		public List<TableRow_BuffInfo> SongInfo = new List<TableRow_BuffInfo>();
		public string WindowName = "E3 Song Hud";
		public float WindowAlpha = 0.8f;
		public Int32 IconSize = 40;
		public Int32 FontSize = 8;
		private int fadeTimeInMS = 1000;
		public double FadeRatio = 0;

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


	}

	public class State_DebuffWindow
	{
		public bool DeAttached = false;
		public List<TableRow_BuffInfo> DebuffInfo = new List<TableRow_BuffInfo>();
		public string WindowName = "E3 Debuff Hud";
		public float WindowAlpha = 0.8f;
		public Int32 IconSize = 40;
		public Int32 FontSize = 8;
		private int fadeTimeInMS = 1000;
		public double FadeRatio = 0;

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

	}
}
