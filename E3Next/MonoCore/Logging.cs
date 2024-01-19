using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

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
    public class Logging
    {
        public enum LogLevels
        {
            None = 0,
            Trace = 2000,
            Debug = 30000,
            Info = 40000,
            Error = 70000,
            CriticalError = 90000,
            Default = 99999
        }

        public interface ITrace : IDisposable
        {
            string Name { get; set; }
            long MetricID { get; set; }
            double Value { get; set; }
            double StartTime { get; set; }
            string Class { get; set; }
            string Method { get; set; }
            LogLevels LogLevel { get; set; }
            Action<ITrace> CallBackDispose { get; set; }
        }

        public static IMQ MQ = Core.mqInstance;

        public static LogLevels TraceLogLevel { get; set; } = LogLevels.None;
        public static LogLevels MinLogLevelTolog { get; set; } = LogLevels.Debug;
        public static LogLevels DefaultLogLevel { get; set; } = LogLevels.Debug;

        private static readonly ConcurrentDictionary<String, String> _classLookup = new ConcurrentDictionary<string, string>();

        public Logging(IMQ mqInstance)
        {
            MQ = mqInstance;
        }

        public void Write(string message, LogLevels logLevel = LogLevels.Default, string eventName = "Logging", [CallerMemberName] string memberName = "", [CallerFilePath] string fileName = "", [CallerLineNumber] int lineNumber = 0, Dictionary<String, String> headers = null)
        {
            if (logLevel == LogLevels.Default)
            {
                logLevel = DefaultLogLevel;
            }

            WriteStatic(message, logLevel, eventName, memberName, fileName, lineNumber, headers);
        }

        public static void WriteStatic(string message, LogLevels logLevel = LogLevels.Info, string eventName = "Logging", [CallerMemberName] string memberName = "", [CallerFilePath] string fileName = "", [CallerLineNumber] int lineNumber = 0, Dictionary<String, String> headers = null)
        {
            if ((int)logLevel < (int)MinLogLevelTolog)
            {
                return;//log level is too low to currently log. 
            }

            string className = GetClassName(fileName);

            if (logLevel == LogLevels.CriticalError)
            {
                eventName += "._CriticalError_";
            }

            if (logLevel == LogLevels.Debug)
            {
                MQ.Write($"\ag{className}:\ao{memberName}\aw:({lineNumber}) {message}", "", "Logging");
            }
            else
            {
                MQ.Write($"{message}");
            }
        }

        public ITrace Trace(string name = "", [CallerMemberName] string memberName = "", [CallerFilePath] string fileName = "", [CallerLineNumber] int lineNumber = 0)
        {
            BaseTrace returnValue = BaseTrace.Aquire();

            if (TraceLogLevel != LogLevels.Trace)
            {
                //if not debugging don't log stuff
                returnValue.CallBackDispose = TraceSetTime;
                return returnValue;
            }

            returnValue.Class = GetClassName(fileName);
            returnValue.Method = memberName;
            returnValue.CallBackDispose = TraceSetTime;
            returnValue.Name = name;

            //done at the very last of this
            returnValue.StartTime = Core.StopWatch.Elapsed.TotalMilliseconds;
            if (!string.IsNullOrWhiteSpace(name))
            {
                MQ.TraceEnd($"\ag{memberName}:\ao{name})");
            }
            else
            {
                MQ.TraceStart(memberName);
            }

            return returnValue;
        }

        private void TraceSetTime(ITrace value)
        {
            double totalMilliseconds = 0;
            //done first!
            totalMilliseconds = Core.StopWatch.Elapsed.TotalMilliseconds - value.StartTime;
            //put event back into its object pool.
            if (!string.IsNullOrWhiteSpace(value.Method))
            {
                MQ.TraceEnd($"\ag{value.Method}:\ao{value.Name}\aw({totalMilliseconds}ms)");
            }
        }

        private static string GetClassName(string fileName)
        {
            if (!_classLookup.ContainsKey(fileName))
            {
                if (!String.IsNullOrWhiteSpace(fileName))
                {
                    string[] tempArray = fileName.Split('\\');

                    string className = tempArray[tempArray.Length - 1];
                    className = className.Replace(".cs", String.Empty).Replace(".vb", String.Empty);
                    _classLookup.TryAdd(fileName, className);
                }
                else
                {
                    _classLookup.TryAdd(fileName, "Unknown/ErrorGettingClass");
                }
            }
            return _classLookup[fileName];
        }

        public sealed class BaseTrace : ITrace
        {
            public string Name { get; set; }
            public long MetricID { get; set; }

            public double Value { get; set; }
            public double StartTime { get; set; }
            public Action<ITrace> CallBackDispose { get; set; }
            public string Class { get; set; }
            public string Method { get; set; }
            public LogLevels LogLevel { get; set; }

            #region objectPoolingStuff

            //private constructor, needs to be created so that you are forced to use the pool.
            private BaseTrace()
            {
            }

            public static BaseTrace Aquire()
            {
                if (!StaticObjectPool.TryPop(out BaseTrace obj))
                {
                    obj = new BaseTrace();
                }

                return obj;
            }

            public void Dispose()
            {
                CallBackDispose?.Invoke(this); //this should null out the CallbackDispose so the normal dispose can then run.

                ResetObject();

                StaticObjectPool.Push(this);
            }

            private void ResetObject()
            {
                this.StartTime = 0;
                this.Value = 0;
                this.CallBackDispose = null;
                this.Class = null;
                this.Method = null;
                this.Name = String.Empty;
            }

            ~BaseTrace()
            {
                //DO NOT CALL DISPOSE FROM THE FINALIZER! This should only ever be used in using statements
                //if this is called, it will cause the domain to hang in the GC when shuttind down
                //This is only here to warn you

            }

            #endregion
        }
    }
}