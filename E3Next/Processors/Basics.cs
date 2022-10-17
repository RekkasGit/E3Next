using E3Core.Settings;
using E3Core.Utility;
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
        public static bool _isPaused = false;
        public static List<Int32> _groupMembers = new List<int>();
        private static Int64 _nextGroupCheck = 0;
        private static Int64 _nextGroupCheckInterval = 1000;
        public static void Init()
        {
            RegisterEventsCasting();
        }
        static void RegisterEventsCasting()
        {
           
            EventProcessor.RegisterEvent("InviteToGroup", "(.+) invites you to join a group.", (x) => {

                MQ.Cmd("/invite");
                MQ.Delay(300);

            });
            EventProcessor.RegisterEvent("InviteToRaid", "(.+) invites you to join a raid.", (x) => {
               
                MQ.Delay(500);
                MQ.Cmd("/raidaccept");

            });

            EventProcessor.RegisterEvent("InviteToDZ", "(.+) tells you, 'dzadd'", (x) => {
                if(x.match.Groups.Count>1)
                {
                    MQ.Cmd($"/dzadd {x.match.Groups[1].Value}");
                }
            });
            EventProcessor.RegisterEvent("InviteToDZ", "(.+) tells you, 'raidadd'", (x) => {
                if (x.match.Groups.Count > 1)
                {
                    MQ.Cmd($"/raidinvite {x.match.Groups[1].Value}");
                }
            });

            EventProcessor.RegisterCommand("/clickit", (x) =>
            {
                MQ.Cmd("/multiline ; /doortarget ; /timed 5 /click left door ");
                  //we are telling people to follow us
                E3._bots.BroadcastCommandToGroup("/clickit");

                MQ.Delay(1000);
                
            });
            EventProcessor.RegisterCommand("/followoff", (x) =>
            {
                RemoveFollow();
                if (x.args.Count == 0)
                {
                    //we are telling people to follow us
                    E3._bots.BroadcastCommandToGroup("/followoff all");
                }
            });

            EventProcessor.RegisterCommand("/e3p", (x) =>
            {
                //swap them
                 _isPaused = _isPaused?false:true;
                if(_isPaused) MQ.Write("\arPAUSING E3!");
                if (!_isPaused) MQ.Write("\agRunning E3 again!");

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
                    E3._bots.BroadcastCommandToGroup("/followme " + E3._characterSettings._characterName);
                }
            });
           

        }


        public static void RefreshGroupMembers()
        {
            if (!e3util.ShouldCheck(ref _nextGroupCheck, _nextGroupCheckInterval)) return;

            Int32 groupCount = MQ.Query<Int32>("${Group}");
            groupCount++;
            if (groupCount != _groupMembers.Count)
            {
                _groupMembers.Clear();
                //refresh group members.
                //see if any  of our members have it.
                for (Int32 i = 0; i < groupCount; i++)
                {
                    Int32 id = MQ.Query<Int32>($"${{Group.Member[{i}].ID}}");
                    _groupMembers.Add(id);
                }
            }
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
