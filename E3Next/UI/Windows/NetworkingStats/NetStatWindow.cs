using E3Core.Processors;
using E3Core.Utility;
using MonoCore;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static MonoCore.E3ImGUI;

namespace E3Core.UI.Windows.NetworkingStats
{
	internal class NetStatWindow
	{
		private static bool _windowInitialized = false;
		private static bool _imguiContextReady = false;
		private static Int64 _lastUpdate = 0;
		private static Int64 _lastUpdateInterval = 1000;
		public static ConcurrentQueue<NetworkInfo> RawNetworkData = new ConcurrentQueue<NetworkInfo>();

		private static string _WindowName = "E3 Network Stats";
	

		[SubSystemInit]
		public static void Init()
		{
			if (Core._MQ2MonoVersion < 0.36m) return;
			E3ImGUI.RegisterWindow(_WindowName, RenderWindow);

			EventProcessor.RegisterCommand("/e3debug_netstats", (x) =>
			{
				NetStatWindow.ToggleWindow();
			}, "toggle memory stats window");

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
				E3.Log.Write($"Net Stats Window error: {ex.Message}", Logging.LogLevels.Error);
				_imguiContextReady = false;
			}
		}

		
		private static void RenderWindow()
		{
			if (!_imguiContextReady) return;
			if (!imgui_Begin_OpenFlagGet(_WindowName)) return;
			imgui_SetNextWindowSizeWithCond(600, 400, (int)ImGuiCond.FirstUseEver);
			E3ImGUI.PushCurrentTheme();
			try
			{
				using (var window = ImGUIWindow.Aquire())
				{
					if (!window.Begin(_WindowName, (int)ImGuiWindowFlags.ImGuiWindowFlags_NoCollapse))
						return;

					// Header with refresh button
					imgui_Text("Net Statistics");
					imgui_Separator();

					// Memory Stats Table
					using (var table = ImGUITable.Aquire())
					{
						int tableFlags = (int)(ImGuiTableFlags.ImGuiTableFlags_RowBg |
											  ImGuiTableFlags.ImGuiTableFlags_BordersOuter |
											  ImGuiTableFlags.ImGuiTableFlags_BordersInner |
											  ImGuiTableFlags.ImGuiTableFlags_ScrollY | ImGuiTableFlags.ImGuiTableFlags_Resizable);

						const float summaryLegendHeight = 190f; // Enough room for summary metrics plus multi-line legend
						float tableHeight = Math.Max(150f, imgui_GetContentRegionAvailY() - summaryLegendHeight);

						if (table.BeginTable("MemoryStatsTable", 4, tableFlags, 0f, tableHeight))
						{
							imgui_TableSetupColumn("Topic", (int)ImGuiTableColumnFlags.ImGuiTableColumnFlags_WidthStretch, 150);
							imgui_TableSetupColumn("Data", (int)ImGuiTableColumnFlags.ImGuiTableColumnFlags_WidthFixed, 120);
							imgui_TableHeadersRow();

							foreach(var netstat in RawNetworkData)
							{
								imgui_TableNextRow();
								imgui_TableNextColumn();
								imgui_Text(netstat.RawTopicData);
								imgui_TableNextColumn();
								imgui_Text(netstat.RawNetworkData);

							}
						}
					}
				}
			}
			finally
			{
				E3ImGUI.PopCurrentTheme();
			}
		}

		public class NetworkInfo : IDisposable
		{
			public string RawTopicData;
			public string RawNetworkData;
	
			#region objectPoolingStuff
			//private constructor, needs to be created so that you are forced to use the pool.
			private NetworkInfo()
			{

			}
			public static NetworkInfo Aquire()
			{
				NetworkInfo obj;
				if (!StaticObjectPool.TryPop<NetworkInfo>(out obj))
				{
					obj = new NetworkInfo();
				}

				return obj;
			}
			public void Dispose()
			{
				/*
				Return Value:
				ImGui::BeginTable() returns true if the table is visible and active, and false otherwise. 
				You should only call ImGui::EndTable() if BeginTable() returns true.
				*/
				RawNetworkData = string.Empty;
				RawTopicData = string.Empty;
				StaticObjectPool.Push(this);
			}
			~NetworkInfo()
			{
				//DO NOT CALL DISPOSE FROM THE FINALIZER! This should only ever be used in using statements
				//if this is called, it will cause the domain to hang in the GC when shuttind down
				//This is only here to warn you

			}

			#endregion
		}
	}
}
