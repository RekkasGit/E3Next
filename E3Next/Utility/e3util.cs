using E3Core.Data;
using E3Core.Processors;
using IniParser;
using IniParser.Parser;
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
        private static ISpawns _spawns = E3._spawns;
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

        public static void TryMoveToTarget()
        {
            Double meX = MQ.Query<Double>("${Me.X}");
            Double meY = MQ.Query<Double>("${Me.Y}");

            Double x = MQ.Query<Double>("${Target.X}");
            Double y = MQ.Query<Double>("${Target.Y}");
            MQ.Cmd($"/squelch /moveto loc {y} {x}");
            MQ.Delay(500);

            Int64 endTime = Core._stopWatch.ElapsedMilliseconds + 10000;
            while(true)
            {
               
                Double tmeX = MQ.Query<Double>("${Me.X}");
                Double tmeY = MQ.Query<Double>("${Me.Y}");

                if((int)meX==(int)tmeX && (int)meY==(int)tmeY)
                {
                    //we are stuck, kick out
                    break;
                }

                meX = tmeX;
                meY = tmeY;

                if (endTime < Core._stopWatch.ElapsedMilliseconds)
                {
                    break;
                }
                MQ.Delay(200);
            }

        }

        public static void TryMoveToLoc(Double x, Double y)
        {
            Double meX = MQ.Query<Double>("${Me.X}");
            Double meY = MQ.Query<Double>("${Me.Y}");
            MQ.Cmd($"/squelch /moveto loc {y} {x}");
            Int64 endTime = Core._stopWatch.ElapsedMilliseconds + 10000;
            MQ.Delay(300);
            while (true)
            {
                Double tmeX = MQ.Query<Double>("${Me.X}");
                Double tmeY = MQ.Query<Double>("${Me.Y}");

                if ((int)meX == (int)tmeX && (int)meY == (int)tmeY)
                {
                    //we are stuck, kick out
                    break;
                }

                meX = tmeX;
                meY = tmeY;

                if (endTime < Core._stopWatch.ElapsedMilliseconds)
                {
                    break;
                }

                MQ.Delay(200);
            }


        }

        public static void PrintTimerStatus(Dictionary<Int32, SpellTimer> timers, ref Int64 printTimer, string Caption, Int64 delayInMS = 10000)
        {
            //Printing out debuff timers
            if (printTimer < Core._stopWatch.ElapsedMilliseconds)
            {
                if (timers.Count > 0)
                {
                    MQ.Write($"\at{Caption}");
                    MQ.Write("\aw===================");


                }

                foreach (var kvp in timers)
                {
                    foreach (var kvp2 in kvp.Value._timestamps)
                    {
                        Data.Spell spell;
                        if (Spell._loadedSpells.TryGetValue(kvp2.Key, out spell))
                        {
                            Spawn s;
                            if (_spawns.TryByID(kvp.Value._mobID, out s))
                            {
                                MQ.Write($"\ap{s.CleanName} \aw: \ag{spell.CastName} \aw: {(kvp2.Value - Core._stopWatch.ElapsedMilliseconds) / 1000} seconds");

                            }

                        }
                        else
                        {
                            Spawn s;
                            if (_spawns.TryByID(kvp.Value._mobID, out s))
                            {
                                MQ.Write($"\ap{s.CleanName} \aw: \agspellid:{kvp2.Key} \aw: {(kvp2.Value - Core._stopWatch.ElapsedMilliseconds) / 1000} seconds");

                            }

                        }

                    }
                }
                if (timers.Count > 0)
                {
                    MQ.Write("\aw===================");

                }
                printTimer = Core._stopWatch.ElapsedMilliseconds + delayInMS;

            }
        }
        public static void RegisterCommandWithTarget(string command, Action<int> FunctionToExecute)
        {
            EventProcessor.RegisterCommand(command, (x) =>
            {
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
                        E3._bots.BroadcastCommandToGroup($"{command} {targetID}");
                        FunctionToExecute(targetID);
                    }
                    else
                    {
                        MQ.Write($"\aNEED A TARGET TO {command}");
                    }
                }
            });

        }
        public static FileIniDataParser CreateIniParser()
        {
            var fileIniData = new FileIniDataParser();
            fileIniData.Parser.Configuration.AllowDuplicateKeys = true;
            fileIniData.Parser.Configuration.OverrideDuplicateKeys = true;// so that the other ones will be put into a collection
            fileIniData.Parser.Configuration.AssigmentSpacer = "";
            fileIniData.Parser.Configuration.CaseInsensitive = true;
           
            return fileIniData;
        }

      

    }
}
