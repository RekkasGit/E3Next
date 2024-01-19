using E3Core.Processors;

using System;
using System.Globalization;
using System.Threading;

/// <summary>
/// Version 0.1
/// This file, is the 'core', or the mediator between you and MQ2Mono
/// MQ2Mono is just a simple C++ plugin to MQ, which exposes the
/// OnInit
/// OnPulse
/// OnIncomingChat
/// etc
/// Methods from MQ event framework, and meshes it in such a way to allow you to write rather straight forward C# code.
///
/// Included in this is a Logging/trace framework, Event Proecssor, MQ command object, etc
///
/// Your class is included in here with a simple .Process Method. This methoid will be called once every OnPulse from the plugin, or basically every frame of the EQ client.
/// All you code should *NOT* be in this file.
/// </summary>
namespace E3Core.MonoCore
{
    /// <summary>
    /// this is the class for the main C# thread
    ///  the C++ core thread will call this in a task at startup
    /// </summary>
    public static class MainProcessor
    {
        public static readonly IMQ MQ = Core.mqInstance;
        private static readonly Logging _log = Core.logInstance;

        public static string ApplicationName = "";

        //we use this to tell the C++ thread that its okay to start processing again
        public static readonly ManualResetEventSlim ProcessResetEvent = new ManualResetEventSlim(false);

        public static void Init()
        {
            //WARNING , you may not be in game yet, so careful what queries you run on MQ.Query. May cause a crash.
            //how long before auto yielding back to C++/EQ/MQ on the next query/command/etc
        }

        public static void Process()
        {
            //need to do this so double parses work in other languages
            Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");

            //wait for the C++ thread thread to tell us we can go
            ProcessResetEvent.Wait();
            ProcessResetEvent.Reset();

            //volatile variable, will eventually update to kill the thread on shutdown
            while (Core.IsProcessing)
            {
                try
                {
                    //using (_log.Trace())
                    {
                        //************************************************
                        //DO YOUR WORK HERE
                        //this loop executes once every OnPulse from C++
                        //************************************************
                        EventProcessor.ProcessEventsInQueues();
                        E3.Process();
                        EventProcessor.ProcessEventsInQueues();
                    }
                }
                catch (ThreadAbortException)
                {
                    Core.IsProcessing = false;
                    Core.CoreResetEvent.Set();
                    throw new ThreadAbortException("Terminating thread");
                }
                catch (Exception ex)
                {
                    if (Core.IsProcessing)
                    {
                        _log.Write("Error: Please reload. Terminating. \r\nExceptionMessage:" + ex.Message + " stack:" + ex.StackTrace, Logging.LogLevels.CriticalError);
                        Core.IsProcessing = false;
                        Core.CoreResetEvent.Set();
                    }

                    //we perma exit this thread loop a full reload will be necessary
                    break;
                }

                //give execution back to the C++ thread, to go back into MQ/EQ
                if (Core.IsProcessing)
                {
                    Delay(E3.CharacterSettings.CPU_ProcessLoopDelay);//this calls the reset events and sets the delay to 10ms at min
                }
            }

            MQ.Write("Shutting down E3 Main C# Thread.");
            MQ.Write("Doing netmq cleanup.");

            Core.CoreResetEvent.Set();
        }

        public static void Delay(int value)
        {
            if (value > 0)
            {
                Core.DelayStartTime = Core.StopWatch.ElapsedMilliseconds;
                Core.DelayTime = value;
                Core.CurrentDelay = value;//tell the C++ thread to send out a delay update
            }
            //lets tell core that it can continue
            Core.CoreResetEvent.Set();
            //we are now going to wait on the core
            ProcessResetEvent.Wait();
            ProcessResetEvent.Reset();
        }
    }
}