using E3Core.Data;
using E3Core.Settings;
using E3Core.Settings.FeatureSettings;
using E3Core.Utility;
using IniParser;
using MonoCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace E3Core.Processors
{
    public static class Movement
    {
        public static Int32 _anchorTarget = 0;
        public static bool _following = false;
        //public static Int32 _followTargetID = 0;
        public static string _followTargetName = String.Empty;
        public static Logging _log = E3.Log;
        private static IMQ MQ = E3.Mq;
        private static ISpawns _spawns = E3.Spawns;
        public static DoorDataFile _doorData = new DoorDataFile();
        private static Int64 _nextAnchorCheck = 0;
        private static Int64 _nextAnchorCheckInterval = 1000;
        private static Int64 _nextFollowCheck = 0;
        private static Int64 _nextFollowCheckInterval = 1000;
        private static Int64 _nextChaseCheck = 0;
        private static Int64 _nextChaseCheckInterval = 10;
        public static string _chaseTarget = String.Empty;

        [SubSystemInit]
        public static void Init()
        {
            RegisterEvents();
             _doorData.LoadData();
    
        }
        public static void Reset()
        {
            _anchorTarget = 0;
            _following = false;
            _followTargetName = String.Empty;
            _chaseTarget = String.Empty;
        }
        [ClassInvoke(Data.Class.All)]
        public static void Check_Chase()
        {
            if (_chaseTarget == String.Empty) return;
            if (!e3util.ShouldCheck(ref _nextChaseCheck, _nextChaseCheckInterval)) return;

            using (_log.Trace())
            {


                if (_chaseTarget != String.Empty && !Assist._isAssisting)
                {
                    double distance = MQ.Query<double>($"${{Spawn[={_chaseTarget}].Distance}}");

                    if (distance != -1)
                    {
                        bool InLoS = MQ.Query<bool>($"${{Spawn[={_chaseTarget}].LineOfSight}}");
                        bool navLoaded = MQ.Query<bool>("${Bool[${Navigation.MeshLoaded}]}");
                        if (navLoaded)
                        {
                            if (distance > 10)
                            {
                                bool navActive = MQ.Query<bool>("${Navigation.Active}");
                                if (!navActive)
                                {
                                    Int32 spawnID = MQ.Query<Int32>($"${{Spawn[={_chaseTarget}].ID}}");
                                    bool pathExists = MQ.Query<bool>($"${{Navigation.PathExists[id {spawnID}]}}");
                                    if (pathExists)
                                    {
                                        MQ.Cmd($"/squelch /nav id {spawnID} log=error");
                                    }
                                    else
                                    {
                                        //are they in LOS?
                                        if (InLoS)
                                        {
                                            double x = MQ.Query<double>($"${{Spawn[={_chaseTarget}].X}}");
                                            double y = MQ.Query<double>($"${{Spawn[={_chaseTarget}].Y}}");
                                            e3util.TryMoveToLoc(x, y, 5, -1);
                                        }

                                    }
                                }
                            }

                        }
                        else
                        {
                            if (distance > 5 && distance < 150 && InLoS)
                            {
                                double x = MQ.Query<double>($"${{Spawn[={_chaseTarget}].X}}");
                                double y = MQ.Query<double>($"${{Spawn[={_chaseTarget}].Y}}");
                                e3util.TryMoveToLoc(x, y, 5, -1);
                            }
                        }
                    }
                }
            }
        }

        public static void RemoveFollow()
        {
            _chaseTarget = String.Empty;
            _followTargetName = string.Empty;
            _following = false;
            MQ.Cmd("/squelch /afollow off");
            MQ.Cmd("/squelch /stick off");


        }
        [ClassInvoke(Data.Class.All)]
        public static void AcquireFollow()
        {

            if (!e3util.ShouldCheck(ref _nextFollowCheck, _nextFollowCheckInterval)) return;

            if (String.IsNullOrWhiteSpace(_followTargetName)) return;

            Spawn s;
            if (_spawns.TryByName(_followTargetName, out s))
            {
                if (s.Distance <= 250)
                {
                    if (!_following)
                    {
                        //they are in range
                        if (MQ.Query<bool>($"${{Spawn[{_followTargetName}].LineOfSight}}"))
                        {
                            if (Casting.TrueTarget(s.ID))
                            {
                                MQ.Delay(100);
                                //if a bot, use afollow, else use stick
                                MQ.Cmd("/afollow on nodoor");
                                _following = true;
                            }
                        }
                    }
                }
                else
                {
                    _following = false;
                }
            }
        }
        
        public static void ResetKeepFollow()
        {
            _anchorTarget = 0;
            _following = false;

        }
        [ClassInvoke(Data.Class.All)]
        public static void Check_Anchor()
        {
            if (!e3util.ShouldCheck(ref _nextAnchorCheck, _nextAnchorCheckInterval)) return;

            if (_anchorTarget > 0 && !Assist._isAssisting)
            {
                _spawns.RefreshList();
                Spawn s;
                if (_spawns.TryByID(_anchorTarget, out s))
                {
                    if (s.Distance > 20 && s.Distance < 150)
                    {
                        e3util.TryMoveToLoc(s.X, s.Y);
                    }
                }
            }
        }
        static void RegisterEvents()
        {
            EventProcessor.RegisterCommand("/clickit", (x) =>
            {
                if (x.args.Count == 0)
                {
                    //we are telling people to follow us
                    E3.Bots.BroadcastCommandToGroup($"/clickit {E3.ZoneID}");

                }
                //read the ini file and pull the info we need.

                if (x.args.Count > 0)
                {
                    Int32 zoneID;
                    if (Int32.TryParse(x.args[0], out zoneID))
                    {
                        if (zoneID != E3.ZoneID)
                        {
                            //we are not in the same zone, ignore.
                            return;
                        }
                    }
                }

                Int32 closestID = _doorData.ClosestDoorID();

                if (closestID > 0)
                {
                    MQ.Cmd($"/doortarget id {closestID}");
                    double currentDistance = MQ.Query<Double>("${DoorTarget.Distance}");
                    //need to move to its location
                    if (currentDistance < 50)
                    {
                        MQ.Cmd($"/doortarget id {closestID}");

                        Double doorX = MQ.Query<double>("${DoorTarget.X}");
                        Double doorY = MQ.Query<double>("${DoorTarget.Y}");
                        e3util.TryMoveToLoc(doorX, doorY, 8, 3000);
                        MQ.Cmd("/squelch /click left door");
                    }
                    else
                    {
                        MQ.Write("\arMove Closer To Door");
                    }
                }


            });

            EventProcessor.RegisterCommand("/anchoron", (x) =>
            {
                if (x.args.Count > 0)
                {
                    Int32 targetid;
                    if (Int32.TryParse(x.args[0], out targetid))
                    {
                        _anchorTarget = targetid;
                    }
                }
                else
                {
                    Int32 targetid = MQ.Query<Int32>("${Target.ID}");
                    if (targetid > 0)
                    {
                        E3.Bots.BroadcastCommandToGroup($"/anchoron {targetid}");
                    }
                }
            });
            EventProcessor.RegisterCommand("/chaseme", (x) =>
            {
                //chaseme <toon name>
                if (x.args.Count == 1 && x.args[0] != "off")
                {
                    if (!e3util.FilterMe(x))
                    {
                        Spawn s;
                        if (_spawns.TryByName(x.args[0], out s))
                        {
                            _chaseTarget = x.args[0];
                            _following = true;
                        }

                    }
                }
                //chanseme off
                else if (x.args.Count == 1 && x.args[0] == "off")
                {
                    E3.Bots.BroadcastCommandToGroup($"/chaseme off {E3.CurrentName}", x);
                    _chaseTarget = String.Empty;
                    _following = false;
                }
                //chaseme off <toon name>
                else if (x.args.Count == 2 && x.args[0] == "off")
                {
                    if (!e3util.FilterMe(x))
                    {
                        _chaseTarget = String.Empty;
                        _following = false;

                    }
                }
                else
                {
                    E3.Bots.BroadcastCommandToGroup($"/chaseme {E3.CurrentName}", x);
                    _following = false;
                }
            });
            EventProcessor.RegisterCommand("/anchoroff", (x) =>
            {
                _anchorTarget = 0;
                if (x.args.Count == 0)
                {
                    E3.Bots.BroadcastCommandToGroup($"/anchoroff all");
                }

            });
            EventProcessor.RegisterCommand("/followme", (x) =>
            {
                string user = string.Empty;
                if (x.args.Count > 0)
                {
                    if (!e3util.FilterMe(x))
                    {
                        user = x.args[0];
                        Spawn s;
                        if (_spawns.TryByName(user, out s))
                        {
                            _followTargetName = user;
                            _following = false;
                            Assist.AssistOff();
                            AcquireFollow();
                        }

                    }
                }
                else
                {
                    //we are telling people to follow us
                    E3.Bots.BroadcastCommandToGroup("/followme " + E3.CurrentName, x);
                }
            });
            EventProcessor.RegisterCommand("/followoff", (x) =>
            {
                RemoveFollow();
                if (x.args.Count == 0)
                {
                    //we are telling people to follow us
                    E3.Bots.BroadcastCommandToGroup("/followoff all");
                }
            });
            EventProcessor.RegisterCommand("/rtz", (x) =>
            {
                if (x.args.Count > 0)
                {
                    //someone telling us to rtz
                    double heading;
                    if (double.TryParse(x.args[0], out heading))
                    {
                        Int32 currentZone = MQ.Query<Int32>("${Zone.ID}");
                        MQ.Cmd($"/face fast heading {heading * -1}");
                        MQ.Cmd("/nomodkey /keypress forward hold");
                        MQ.Delay(1000);
                        Int32 counter = 0;
                        while (E3.ZoneID == currentZone && counter < 20)
                        {
                            counter++;
                            MQ.Delay(100);
                            currentZone = MQ.Query<Int32>("${Zone.ID}");
                        }
                        MQ.Cmd("/nomodkey /keypress forward");

                    }
                }
                else
                {
                    //tell others to rtz
                    //get our faced heading
                    double heading = MQ.Query<double>("${Me.Heading.Degrees}");
                    E3.Bots.BroadcastCommandToGroup($"/rtz {heading}");
                    MQ.Delay(500);
                    MQ.Cmd($"/face fast heading {heading * -1}");
                    MQ.Cmd("/nomodkey /keypress forward hold");

                }
            });
            //anchoron
        }
    }
}
