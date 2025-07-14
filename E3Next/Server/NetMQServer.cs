using E3Core.Processors;
using MonoCore;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace E3Core.Server
{
    /// <summary>
    /// This publishes out data to the UI client, but could be for anything that wants to pub/sub
    /// </summary>
    public static class NetMQServer
    {

        static PubServer _pubServer;
        static RouterServer _routerServer;
        static PubClient _pubClient;
        public static SharedDataClient SharedDataClient;

		public static Int32 RouterPort;
        public static Int32 PubPort;
        public static Int32 PubClientPort;
        public static Process UIProcess;
		public static Process ConfigProcess;
		public static Process DiscordProcess;
        private static IMQ MQ = E3.MQ;

        
        public static void Init()
        {
			SharedDataClient = new SharedDataClient();

			RouterPort = FreeTcpPort();
            PubPort = FreeTcpPort();
            PubClientPort = FreeTcpPort();

            

            if(Debugger.IsAttached)
            {
                PubPort = 51711;
                RouterPort = 51712;
                PubClientPort = 51713;
            }
            _pubServer = new PubServer();
            _routerServer = new RouterServer();
            _pubClient = new PubClient();

            _pubServer.Start(PubPort);
            _routerServer.Start(RouterPort);
            _pubClient.Start(PubClientPort);
		
		
			EventProcessor.RegisterCommand("/ui", (x) =>
			{
                MQ.Write("/ui has been depreciated, please use /e3ui");
			});
			EventProcessor.RegisterCommand("/e3debug-config", (x) =>
			{
                PrintCharConfigLaunch();
			});

			EventProcessor.RegisterCommand("/e3config", (x) =>
			{
				LaunchCharConfig();
			});
			EventProcessor.RegisterCommand("/e3ui", (x) =>
            {
                ToggleUI();
            });
			EventProcessor.RegisterCommand("/e3discord", (x) =>
            {
                ToggleDiscordBot();
            });
			EventProcessor.RegisterCommand("/e3ui-debug", (x) =>
            {
                Int32 processID = System.Diagnostics.Process.GetCurrentProcess().Id;
                var path = $"{Assembly.GetExecutingAssembly().CodeBase.Replace("file:///", "").Replace("/", "\\").Replace("e3.dll", "")}E3NextUI.exe";
                MQ.Write($"{path} {PubPort} {RouterPort} {PubClientPort} {processID}");
            });
            EventProcessor.RegisterCommand("/e3ui-kill", (x) =>
            {
               if(UIProcess!=null)
                {
                    UIProcess.Kill();
                  
                }
            });
        }
		public static void KillAllProcesses()
		{
			if (UIProcess != null)
			{
				try
				{
					UIProcess.Kill();

				}
				catch (Exception)
				{

				}
			}
			if (DiscordProcess != null)
			{

				try
				{
					DiscordProcess.Kill();

				}
				catch (Exception)
				{

				}
			}
			if (ConfigProcess != null)
			{

				try
				{
					ConfigProcess.Kill();

				}
				catch (Exception)
				{

				}
			}
			
		}
        static void PrintCharConfigLaunch()
        {

			string dllFullPath = Assembly.GetExecutingAssembly().CodeBase.Replace("file:///", "").Replace("/", "\\").Replace("e3.dll", "");
			string exeName = "E3NextConfigEditor.exe";
			Int32 processID = System.Diagnostics.Process.GetCurrentProcess().Id;
			
            string startInfoString = dllFullPath + exeName + $" {RouterPort} {processID}";
			MQ.Write(startInfoString);

		}
		static void LaunchCharConfig()
		{
			string dllFullPath = Assembly.GetExecutingAssembly().CodeBase.Replace("file:///", "").Replace("/", "\\").Replace("e3.dll", "");
			string exeName = "E3NextConfigEditor.exe";
			if (ConfigProcess == null)
			{
				Int32 processID = System.Diagnostics.Process.GetCurrentProcess().Id;
				MQ.Write("Trying to start:" + dllFullPath + exeName);
				var startInfo = new ProcessStartInfo(dllFullPath + exeName);
				startInfo.WorkingDirectory = dllFullPath;// working directory
				startInfo.Arguments = $"{RouterPort} {processID}";// set additional properties 
				startInfo.UseShellExecute = false;
				ConfigProcess = System.Diagnostics.Process.Start(startInfo);

			}
			else
			{
				//we have a process, is it up?
				if (ConfigProcess.HasExited)
				{
					Int32 processID = System.Diagnostics.Process.GetCurrentProcess().Id;
					//start up a new one.
					MQ.Write("Trying to start again:" + dllFullPath + exeName);
					var startInfo = new ProcessStartInfo(dllFullPath + exeName);
					startInfo.WorkingDirectory = dllFullPath;// working directory
					startInfo.Arguments = $"{RouterPort} {processID}";// set additional properties 
					startInfo.UseShellExecute = false;
					ConfigProcess = System.Diagnostics.Process.Start(startInfo);
				}
			}
		}
        /// <summary>
        /// Turns on the UI program, and then from then on, hide/shows it as needed. To close restart e3.
        /// </summary>
        static void ToggleUI()
        {
            string dllFullPath = Assembly.GetExecutingAssembly().CodeBase.Replace("file:///", "").Replace("/", "\\").Replace("e3.dll", "");
#if DEBUG
        //    dllFullPath = "C:\\Code\\E3next\\E3Next\\bin\\Debug\\";
#endif
            if (UIProcess == null)
            {
                Int32 processID = System.Diagnostics.Process.GetCurrentProcess().Id;
                MQ.Write("Trying to start:" + dllFullPath + @"E3NextUI.exe");
                var startInfo = new ProcessStartInfo(dllFullPath + @"E3NextUI.exe");
                startInfo.WorkingDirectory = dllFullPath;
                startInfo.Arguments = $"{PubPort} {RouterPort} {PubClientPort} {processID}";
                startInfo.UseShellExecute = false;
                UIProcess = System.Diagnostics.Process.Start(startInfo);
				
                //wire up the events to send data over to the UI.
                EventProcessor.RegisterUnfilteredEventMethod("E3UI", (x) => {

					if (x.typeOfEvent == EventProcessor.eventType.EQEvent)
					{
						PubServer.IncomingChatMessages.Enqueue(x.eventString);
					}
					else if (x.typeOfEvent == EventProcessor.eventType.MQEvent)
					{
						PubServer.MQChatMessages.Enqueue(x.eventString);
					}

				});
			}
            else
            {
                //we have a process, is it up?
                if (UIProcess.HasExited)
                {
                    Int32 processID = System.Diagnostics.Process.GetCurrentProcess().Id;
                    //start up a new one.
                    MQ.Write("Trying to start again:" + dllFullPath + @"E3NextUI.exe");
                    var startInfo = new ProcessStartInfo(dllFullPath + @"E3NextUI.exe");
                    startInfo.WorkingDirectory = dllFullPath;
                    startInfo.Arguments = $"{PubPort} {RouterPort} {PubClientPort} {processID}";
                    startInfo.UseShellExecute = false;
                    UIProcess = System.Diagnostics.Process.Start(startInfo);
                }
                else 
                {
                    PubServer.CommandsToSend.Enqueue("#toggleshow");
                   
                }
            }
        }

        static void ToggleDiscordBot()
        {
            var dllFullPath = Assembly.GetExecutingAssembly().CodeBase.Replace("file:///", "").Replace("/", "\\").Replace("e3.dll", "");
#if DEBUG
           // dllFullPath = "C:\\Code\\E3next\\E3Next\\bin\\Debug\\";
#endif
            var processName = $"{dllFullPath}E3Discord.exe";
            if (DiscordProcess == null)
            {
                var existingDiscordProcess = Process.GetProcessesByName("E3Discord.exe");
                if (existingDiscordProcess.Any())
                {
                    MQ.Write("\agAnother E3Discord is already runnning. Not starting another one");
                    return;
                }
                Int32 processID = System.Diagnostics.Process.GetCurrentProcess().Id;
                MQ.Write("\ayTrying to start:" + processName);
                var discordMyUserId = string.IsNullOrEmpty(E3.GeneralSettings.DiscordMyUserId) ? string.Empty : E3.GeneralSettings.DiscordMyUserId;
                var commandLineArgs = $"{PubPort} {RouterPort} {PubClientPort} {E3.GeneralSettings.DiscordBotToken} " +
                    $"{E3.GeneralSettings.DiscordGuildChannelId} {E3.GeneralSettings.DiscordServerId} {processID} {E3.GeneralSettings.DiscordMyUserId}";
				var startInfo = new ProcessStartInfo(processName);
				startInfo.WorkingDirectory = dllFullPath;// working directory
                startInfo.Arguments = commandLineArgs;
				startInfo.UseShellExecute = false;
				DiscordProcess = System.Diagnostics.Process.Start(startInfo);
                MQ.Write($"\agStarted {processName}");
            }
            else
            {
                MQ.Write($"\ayKilling {processName}");
                if (!DiscordProcess.HasExited)
                    DiscordProcess.Kill();

                DiscordProcess = null;
                MQ.Write("\agIt's dead Jim");
            }
        }

        /// <summary>
        /// best way to find a free open port that i can figure out
        /// windows won't reuse the port for a bit, so safe to open/close -> reuse.
        /// </summary>
        /// <returns></returns>
        static int FreeTcpPort()
        {
            TcpListener l = new TcpListener(IPAddress.Loopback, 0);
            l.Start();
            int port = ((IPEndPoint)l.LocalEndpoint).Port;
            l.Stop();
            return port;
        }
    }
}
