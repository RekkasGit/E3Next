using E3Core.Processors;
using MonoCore;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

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
        public static System.Diagnostics.Process UIProcess;
        private static IMQ MQ = E3.MQ;

        [SubSystemInit]
        public static void Init()
        {
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
			SharedDataClient = new SharedDataClient();

            EventProcessor.RegisterUnfilteredEventMethod("E3UI", (x) => {

                if(x.typeOfEvent== EventProcessor.eventType.EQEvent)
                {
                    PubServer.IncomingChatMessages.Enqueue(x.eventString);
                }else if(x.typeOfEvent == EventProcessor.eventType.MQEvent)
                {
                    PubServer.MQChatMessages.Enqueue(x.eventString);
                }

            });
            EventProcessor.RegisterCommand("/ui", (x) =>
            {
                ToggleUI();
            });
            EventProcessor.RegisterCommand("/ui-debug", (x) =>
            {
                Int32 processID = System.Diagnostics.Process.GetCurrentProcess().Id;
                MQ.Write($"{PubPort} {RouterPort} {PubClientPort} {processID}");
            });
            EventProcessor.RegisterCommand("/ui-kill", (x) =>
            {
               if(UIProcess!=null)
                {
                    UIProcess.Kill();
                    UIProcess = null;
                }
            });
        }
        /// <summary>
        /// Turns on the UI program, and then from then on, hide/shows it as needed. To close restart e3.
        /// </summary>
        static void ToggleUI()
        { 
          
            if (UIProcess == null)
            {
                string dllFullPath = Assembly.GetExecutingAssembly().CodeBase.Replace("file:///", "").Replace("/", "\\").Replace("e3.dll", "");
                Int32 processID = System.Diagnostics.Process.GetCurrentProcess().Id;
                MQ.Write("Trying to start:" + dllFullPath + @"E3NextUI.exe");
                UIProcess = System.Diagnostics.Process.Start(dllFullPath + @"E3NextUI.exe", $"{PubPort} {RouterPort} {PubClientPort} {processID}");
            }
            else
            {
                //we have a process, is it up?
                if (UIProcess.HasExited)
                {
                    string dllFullPath = Assembly.GetExecutingAssembly().CodeBase.Replace("file:///", "").Replace("/", "\\").Replace("e3.dll", "");
                    Int32 processID = System.Diagnostics.Process.GetCurrentProcess().Id;
                    //start up a new one.
                    MQ.Write("Trying to start:" + dllFullPath + @"E3NextUI.exe");
                    UIProcess = System.Diagnostics.Process.Start(dllFullPath + @"E3NextUI.exe", $"{PubPort} {RouterPort} {PubClientPort} {processID}");
                }
                else 
                {
                    PubServer.CommandsToSend.Enqueue("#toggleshow");
                   
                }
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
