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


        public static void Init()
        {
            RegisterEventsCasting();
        }
        static void RegisterEventsCasting()
        {
            #region followEvent
            List<String> r = new List<string>();
            //box chat bct
            r.Add(@"\[(.+)\(msg\)\] [F|f]ollow");
            //box chat /bc
            r.Add(@"<(.+)> [f|F]ollow");
            r.Add(@"<(.+)> [f|F]ollow (.+)");
            //tell 
            r.Add(@"(.+) tells you, '[fF]ollow'");
            r.Add(@"(.+) tells you, '[fF]ollow (.+)'");
            //gsay
            r.Add(@"(.+) tells the group, '[fF]ollow'");
            r.Add(@"(.+) tells the group, '[fF]ollow (.+)'");
            //dannet
            r.Add(@"\[ .+_(.+) ] [fF]ollow");
            r.Add(@"\[ .+_(.+)\) ] [fF]ollow");
            r.Add(@"\[ .+_(.+) ] [fF]ollow (.+)");
            r.Add(@"\[ .+_(.+)\) ] [fF]ollow (.+)");

            //<(.+)> [f|F]ollow
            EventProcessor.RegisterEvent("EVENT_Follow", r, (x) =>
            {
                string user = string.Empty;
                if (x.match.Groups.Count > 2)
                {
                    //get who to follow
                    user = x.match.Groups[2].Value;
                }
                else if (x.match.Groups.Count > 1)
                {
                    //assume its the person telling us
                    user = x.match.Groups[1].Value;
                }

                //one of our bots, follow commands
              
                if (!MQ.Query<bool>($"${{Spawn[{user}].LineOfSight}}"))
                {
                    MQ.Broadcast($"I cannot see {user}");
                    return;
                }
                _followTargetID = MQ.Query<Int32>($"${{Spawn[{user}].ID}}");
                _followTargetName = user;
                _following = true;
                Assist.AssistOff();
                AcquireFollow();


            });
            #endregion

            #region AssistOn
            
            r = new List<string>();
            //box chat bct
            r.Add(@"\[(.+)\(msg\)\] [a|A]ssist on (.+)");
            //box chat /bc
            r.Add(@"<(.+)> [a|A]ssist on (.+)");
            //tell 
            r.Add(@"(.+) tells you, '[a|A]ssist on (.+)'");
            //gsay
            r.Add(@"(.+) tells the group, '[a|A]ssist on (.+)'");
            //dannet
            r.Add(@"\[ .+_(.+) ] [a|A]ssist on (.+)");
            r.Add(@"\[ .+_(.+)\) ] [a|A]ssist on (.+)");

            //<(.+)> [f|F]ollow
            EventProcessor.RegisterEvent("EVENT_Assist", r, (x) =>
            {
                string user = string.Empty;
                Int32 mobId = 0;
                if (x.match.Groups.Count > 2)
                {
                    //get who to follow
                    user = x.match.Groups[1].Value;
                    Int32.TryParse(x.match.Groups[2].Value, out mobId);
                }

                if(mobId==0)
                {
                    //something wrong with the assist, kickout
                    MQ.Broadcast("Cannot assist, improper mobid");
                    return;
                }

                if(MQ.Query<bool>($"${{Bool[${{Spawn[id {mobId}].Type.Equal[Corpse]}}]}}"))
                {
                    MQ.Broadcast("Cannot assist, a corpse");
                    return;
                }
                if(!MQ.Query<bool>($"${{Select[${{Spawn[id {mobId}].Type}},NPC,Pet]}}"))
                {
                    MQ.Broadcast("Cannot assist, not a NPC or Pet");
                    return;
                }

                if (MQ.Query<Decimal>($"${{Spawn[{mobId}].Distance3D}}") > E3._generalSettings.Assists_MaxEngagedDistance)
                {
                    string cleanName = MQ.Query<string>($"${{Spawn[{mobId}].CleanName}}");
                    MQ.Broadcast("{cleanName} is too far away.");
                    return;
                }


                if (MQ.Query<bool>("${Me.Feigning}"))
                {
                    MQ.Cmd("/stand");
                }

                if(_following && MQ.Query<Decimal>($"${{Spawn[id {_followTargetID}].Distance3D}}") > 100 && MQ.Query<bool>("${Me.Moving}"))
                {
                    while(MQ.Query<bool>("${Me.Moving}") && MQ.Query<Decimal>($"${{Spawn[id {_followTargetID}].Distance3D}}")>100)
                    {
                        MQ.Delay(100);
                        //wait us to get close to our follow target and then we can engage
                    }
                }

                if (MQ.Query<bool>("${Stick.Active}")) MQ.Cmd("/squelch /stick off");
                if (MQ.Query<bool>("${AdvPath.Following}")) MQ.Cmd("/squelch /afollow off ");



            });

            #endregion

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
