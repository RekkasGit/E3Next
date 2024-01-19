using E3Core.Processors;

using System;
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
    public class MQ : IMQ
    {   //**************************************************************************************************
        //NONE OF THESE METHODS SHOULD BE CALLED ON THE C++ Thread, as it will cause a deadlock due to delay calls
        //**************************************************************************************************

        public static long MaxMillisecondsToWork = 40;
        public static long SinceLastDelay = 0;
        public static long _totalQueryCounts;

        public T Query<T>(string query)
        {
            if (!Core.IsProcessing)
            {
                //we are terminating, kill this thread
                throw new ThreadAbortException("Terminating thread");
            }

            _totalQueryCounts++;
            long differenceTime = Core.StopWatch.ElapsedMilliseconds - SinceLastDelay;

            if (MaxMillisecondsToWork < differenceTime)
            {
                Delay(0);
            }

            string mqReturnValue = Core.mq_ParseTLO(query);

            if (typeof(T) == typeof(int))
            {
                if (!mqReturnValue.Contains("."))
                {
                    return int.TryParse(mqReturnValue, out int value) ? (T)(object)value : (T)(object)-1;
                }
                else
                {
                    return decimal.TryParse(mqReturnValue, out decimal value) ? (T)(object)value : (T)(object)-1;
                }
            }
            else if (typeof(T) == typeof(bool))
            {
                if (Boolean.TryParse(mqReturnValue, out bool booleanValue))
                {
                    return (T)(object)booleanValue;
                }
                if (mqReturnValue == "NULL")
                {
                    return (T)(object)false;
                }
                if (mqReturnValue == "!FALSE")
                {
                    return (T)(object)true;
                }
                if (mqReturnValue == "!TRUE")
                {
                    return (T)(object)false;
                }
                if (int.TryParse(mqReturnValue, out int intValue))
                {
                    return intValue > 0 ? (T)(object)true : (T)(object)false;
                }
                return string.IsNullOrWhiteSpace(mqReturnValue) ? (T)(object)false : (T)(object)true;
            }
            else if (typeof(T) == typeof(string))
            {
                return (T)(object)mqReturnValue;
            }
            else if (typeof(T) == typeof(decimal))
            {
                return Decimal.TryParse(mqReturnValue, out decimal value) ? (T)(object)value : (T)(object)-1M;
            }
            else if (typeof(T) == typeof(double))
            {
                return double.TryParse(mqReturnValue, out double value) ? (T)(object)value : (T)(object)-1D;
            }
            else if (typeof(T) == typeof(long))
            {
                return long.TryParse(mqReturnValue, out long value) ? (T)(object)value : (T)(object)-1L;
            }

            return default;
        }

        public void Cmd(string query, bool delayed = false)
        {
            if (!Core.IsProcessing)
            {
                //we are terminating, kill this thread
                throw new ThreadAbortException("Terminating thread");
            }

            long differenceTime = Core.StopWatch.ElapsedMilliseconds - SinceLastDelay;

            if (MaxMillisecondsToWork < differenceTime)
            {
                Delay(0);
            }

            //avoid using /delay, this was only made to deal with UI /delay commands.
            if (query.StartsWith("/delay ", StringComparison.OrdinalIgnoreCase))
            {
                string[] splitArray = query.Split(' ');
                if (splitArray.Length > 1)
                {
                    if (int.TryParse(splitArray[1], out var delayvalue))
                    {
                        Delay(delayvalue);
                    }
                }
                return;
            }

            Core.CurrentCommand = query;
            Core.CurrentCommandDelayed = delayed;
            Core.CoreResetEvent.Set();
            //we are now going to wait on the core
            MainProcessor.ProcessResetEvent.Wait();
            MainProcessor.ProcessResetEvent.Reset();
            if (!Core.IsProcessing)
            {
                //we are terminating, kill this thread
                throw new ThreadAbortException("Terminating thread");
            }
        }

        public void Cmd(string query, int delay, bool delayed = false)
        {
            Cmd(query, delayed);
            Delay(delay);
        }

        public void Write(string query, [CallerMemberName] string memberName = "", [CallerFilePath] string fileName = "", [CallerLineNumber] int lineNumber = 0)
        {
            //write on current thread, it will be queued up by MQ. 
            //needed to deal with certain lock situations and just keeps things simple. 
            Core.mq_Echo($"\a#336699[{MainProcessor.ApplicationName}]\a-w{DateTime.Now:HH:mm:ss} \aw- {query}");
        }

        public void TraceStart(string methodName)
        {
            if (String.IsNullOrWhiteSpace(methodName))
            {
                return;
            }
            this.Write($"|- {methodName} ==>");
        }

        public void TraceEnd(string methodName)
        {
            if (String.IsNullOrWhiteSpace(methodName))
            {
                return;
            }
            this.Write($"<== {methodName} -|");
        }

        public void Delay(int value)
        {
            if (!Core.IsProcessing)
            {
                //we are terminating, kill this thread
                throw new ThreadAbortException("Terminating thread: Delay enter");
            }

            if (value > 0)
            {
                Core.DelayStartTime = Core.StopWatch.ElapsedMilliseconds;
                Core.DelayTime = value;
                Core.CurrentDelay = value;//tell the C++ thread to send out a delay update
            }

            if (E3.IsInit && !E3.InStateUpdate)
            {
                E3.StateUpdates();
            }

            //lets tell core that it can continue
            Core.CoreResetEvent.Set();
            //we are now going to wait on the core
            MainProcessor.ProcessResetEvent.Wait();
            MainProcessor.ProcessResetEvent.Reset();

            if (!Core.IsProcessing)
            {
                //we are terminating, kill this thread
                Write("Throwing exception for termination: Delay exit");
                throw new ThreadAbortException("Terminating thread");
            }
            if (E3.IsInit && !E3.InStateUpdate)
            {
                E3.StateUpdates();
            }
            SinceLastDelay = Core.StopWatch.ElapsedMilliseconds;
        }

        public bool Delay(int maxTimeToWait, string Condition)
        {
            if (!Core.IsProcessing)
            {
                //we are terminating, kill this thread
                throw new ThreadAbortException("Terminating thread: Delay Condition");
            }

            Condition = $"${{If[{Condition},TRUE,FALSE]}}";
            long startingTime = Core.StopWatch.ElapsedMilliseconds;
            while (!this.Query<bool>(Condition))
            {
                if (Core.StopWatch.ElapsedMilliseconds - startingTime > maxTimeToWait)
                {
                    return false;
                }
                this.Delay(25);
            }
            return true;
        }

        public bool Delay(int maxTimeToWait, Func<bool> methodToCheck)
        {
            if (!Core.IsProcessing)
            {
                //we are terminating, kill this thread
                throw new ThreadAbortException("Terminating thread: delay method ");
            }

            long startingTime = Core.StopWatch.ElapsedMilliseconds;
            while (!methodToCheck.Invoke())
            {
                if (Core.StopWatch.ElapsedMilliseconds - startingTime > maxTimeToWait)
                {
                    return false;
                }
                this.Delay(10);
            }
            return true;
        }

        public bool AddCommand(string commandName)
        {
            return Core.mq_AddCommand(commandName);
        }

        public void ClearCommands()
        {
            Core.mq_ClearCommands();
        }

        public void RemoveCommand(string commandName)
        {
            Core.mq_RemoveCommand(commandName);
        }

        private bool? Feature_TLO_Dispellable = null;
        public bool FeatureEnabled(MQFeature feature)
        {
            if (feature == MQFeature.TLO_Dispellable)
            {
                if (Feature_TLO_Dispellable is null)
                {
                    Feature_TLO_Dispellable = Query<bool>("${Spell[Courage].Dispellable}");
                }
                return Feature_TLO_Dispellable.Value;
            }
            return true;
        }

        public string GetFocusedWindowName()
        {
            return Core._MQ2MonoVersion > 0.1M ? Core.mq_GetFocusedWindowName() : "NULL";
        }
    }
}