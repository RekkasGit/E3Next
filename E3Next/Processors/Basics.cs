using E3Core.Settings;
using MonoCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace E3Core.Processors
{
    public static class Basics
    {

        public static bool _following = false;
        public static Int32 _followTargetID = 0;
        public static string _followTargetName = String.Empty;

        public static Logging _log = E3._log;
        private static IMQ MQ = E3.MQ;
        private static ISpawns _spawns = E3._spawns;


        public static void Init()
        {
            RegisterEventsCasting();
        }
        static void RegisterEventsCasting()
        {

            EventProcessor.RegisterCommand("/followoff", (x) =>
            {
                RemoveFollow();
                if (x.args.Count == 0)
                {
                    //we are telling people to follow us
                    E3._bots.BroadcastCommandToOthers("/followoff all");
                }
            });
            EventProcessor.RegisterCommand("/followme", (x) =>
            {
                string user = string.Empty;
                if(x.args.Count>0)
                {
                    user = x.args[0];
                    //we have someone to follow.
                    _followTargetID = MQ.Query<Int32>($"${{Spawn[{user}].ID}}");
                    if(_followTargetID > 0)
                    {
                        _followTargetName = user;
                        _following = true;
                        Assist.AssistOff();
                        AcquireFollow();
                    }
                }
                else
                {
                    //we are telling people to follow us
                    E3._bots.BroadcastCommandToOthers("/followme " + E3._characterSettings._characterName);
                }
            });
           

        }

        public static void RemoveFollow()
        {
            _followTargetID = 0;
            _followTargetName = string.Empty;
            MQ.Cmd("/squelch /afollow off");
            MQ.Cmd("/squelch /stick off");
           
        }

        public static void AcquireFollow()
        {

            Int32 instanceCount = MQ.Query<Int32>($"${{SpawnCount[id {_followTargetID} radius 250]}}");

            if (instanceCount > 0)
            {
                //they are in range
                if (MQ.Query<bool>($"${{Spawn[{_followTargetName}].LineOfSight}}"))
                {
                    Casting.TrueTarget(_followTargetID);
                    //if a bot, use afollow, else use stick
                    if (E3._bots.InZone(_followTargetName))
                    {
                        MQ.Cmd("/afollow on");
                    }
                    else
                    {
                        MQ.Cmd("/squelch /stick hold 20 uw");
                    }
                }
            }
           
        }

    }
}
