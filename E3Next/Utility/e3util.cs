using E3Core.Processors;
using MonoCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace E3Core.Utility
{
    public static class e3util
    {

        public static string _lastSuccesfulCast = String.Empty;
        public static Logging _log = E3._log;
        private static IMQ MQ = E3.MQ;
        /// <summary>
        /// Use to see if a certain method should be running
        /// </summary>
        /// <param name="nextCheck">ref param to update ot the next time a thing should run</param>
        /// <param name="nextCheckInterval">The interval in milliseconds</param>
        /// <returns></returns>
        public static bool ShouldCheck(ref Int64 nextCheck, Int64 nextCheckInterval)
        {
            if (Core._stopWatch.ElapsedMilliseconds < nextCheck)
            {
                return false;
            }
            else
            {
                nextCheck = Core._stopWatch.ElapsedMilliseconds + nextCheckInterval;
                return true;
            }
        }
        public static void RegisterCommandWithTarget(string command, Action<int> FunctionToExecute)
        {
            EventProcessor.RegisterCommand(command, (x) =>
            {
                MQ.Write("Register Command executing:" + command);
                Int32 mobid;
                if (x.args.Count > 0)
                {
                    if (Int32.TryParse(x.args[0], out mobid))
                    {
                        FunctionToExecute(mobid);
                    }
                    else
                    {
                        MQ.Broadcast($"\aNeed a valid target to {command}.");
                    }
                }
                else
                {
                    Int32 targetID = MQ.Query<Int32>("${Target.ID}");
                    if (targetID > 0)
                    {
                        //we are telling people to follow us
                        E3._bots.BroadcastCommandToOthers($"{command} {targetID}");
                        FunctionToExecute(targetID);
                    }
                    else
                    {
                        MQ.Write($"\aNEED A TARGET TO {command}");
                    }
                }
            });

        }

        public static void RegisterCommandWithTargetToOthers(string command, Action<int> FunctionToExecute)
        {
            EventProcessor.RegisterCommand(command, (x) =>
            {
                _log.Write("Register Command executing:" + command);
              
                if (x.args.Count == 0)
                {
                    Int32 targetID = MQ.Query<Int32>("${Target.ID}");
                    E3._bots.BroadcastCommandToOthers($"{command} {targetID}");

                }
                else
                {
                    Int32 mobid;
                    if (Int32.TryParse(x.args[0], out mobid))
                    {
                        FunctionToExecute(mobid);
                    }
                }
            });

        }

    }
}
