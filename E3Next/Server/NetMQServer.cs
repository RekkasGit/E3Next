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
    public static class NetMQServer
    {

        static PubServer _pubServer;
        static RouterServer _routerServer;
        public static Int32 RouterPort;
        public static Int32 PubPort;
        public static Int32 PubClientPort;
        public static System.Diagnostics.Process _uiProcess;
        private static IMQ MQ = E3.Mq;

        [SubSystemInit]
        public static void Init()
        {
            RouterPort = FreeTcpPort();
            PubPort = FreeTcpPort();
            PubClientPort = FreeTcpPort();

            _pubServer = new PubServer();

            if(Debugger.IsAttached)
            {
                PubPort = 51711;
                RouterPort = 51712;
            }

            _routerServer = new RouterServer();
            _pubServer.Start(PubPort);
            _routerServer.Start(RouterPort);
            EventProcessor.RegisterUnfilteredEvent("NetMQData", ".+", (x) =>
            {
                if(x.typeOfEvent== EventProcessor.eventType.EQEvent)
                {
                    PubServer._pubMessages.Enqueue(x.eventString);
                } 
                else if(x.typeOfEvent== EventProcessor.eventType.MQEvent)
                {
                    PubServer._pubWriteColorMessages.Enqueue(x.eventString);
                }
               
            });

            EventProcessor.RegisterCommand("/ui", (x) =>
            {   
                //file:///G:/EQ/E3_ROF2_MQ2Next/Mono/macros/e3/Rekken/e3.dll
                string dllFullPath = Assembly.GetExecutingAssembly().CodeBase.Replace("file:///", "").Replace("/", "\\").Replace("e3.dll", "");
                if(Debugger.IsAttached)
                {
                    dllFullPath = @"G:\EQ\E3_ROF2_MQ2Next\mono\macros\e3\Rekken";

                }

                if (_uiProcess==null)
                {
                    MQ.Write("Trying to start:" + dllFullPath + @"E3NextUI.exe");
                    _uiProcess = System.Diagnostics.Process.Start(dllFullPath+ @"E3NextUI.exe",$"{PubPort} {RouterPort}");
                }
                else
                {
                    //we have a process, is it up?
                    if (_uiProcess.HasExited)
                    {
                        //start up a new one.
                        MQ.Write("Trying to start:" + dllFullPath + @"E3NextUI.exe");
                        _uiProcess = System.Diagnostics.Process.Start(dllFullPath + @"E3NextUI.exe", $"{PubPort} {RouterPort}");

                    }
                    else
                    {
                        //close it
                        _uiProcess.Kill();
                        _uiProcess = null;
                    }
                }
            });
        }
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
