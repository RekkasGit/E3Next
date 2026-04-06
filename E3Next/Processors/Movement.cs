using CommunityToolkit.HighPerformance;
using E3Core.Data;
using E3Core.Settings;
using E3Core.Settings.FeatureSettings;
using E3Core.Utility;
using IniParser;
using MonoCore;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace E3Core.Processors
{
    public static class Movement
    {
        public static Int32 AnchorTarget = 0;
        public static double Anchor_X = double.MinValue;
        public static double Anchor_Y = double.MinValue;
        public static double Anchor_Z = double.MinValue;
        public static List<string> AnchorFilters = new List<string>();
        public static bool MovementPaused = false;
        [ExposedData("Movement", "Following")]
        public static bool Following = false;
        //public static Int32 _followTargetID = 0;
        [ExposedData("Movement", "FollowTargetName")]
        public static string FollowTargetName = String.Empty;

        [ExposedData("Movement", "E3FollowTargetName")]
        public static string E3FollowTargetName = String.Empty;


        public static Logging _log = E3.Log;
        private static IMQ MQ = E3.MQ;
        private static ISpawns _spawns = E3.Spawns;
        public static DoorDataFile _doorData = new DoorDataFile();
        private static Int64 _nextAnchorCheck = 0;
        private static Int64 _nextAnchorCheckInterval = 1000;
        public static Int64 _nextFollowCheck = 0;
        private static Int64 _nextFollowCheckInterval = 1000;
        public static Int64 _nextE3FollowCheck = 0;
        public static bool _e3follownavfallback = true;
        private static Int64 _nextE3FollowCheckInterval = 1;
        private static Int64 _nextChaseCheck = 0;
        private static Int64 _nextChaseCheckInterval = 250;
        [ExposedData("Movement", "ChaseTarget")]
        public static string ChaseTargetName = String.Empty;
        public static float _followMeDistance = 10;
        public static List<string> _clickitUseDoorZones = new List<string>() { "poknowledge", "potranq", "potimea", "potimeb", "anguish", "solrotower" };

        [SubSystemInit]
        public static void Movement_Init()
        {
            RegisterEvents();
            _doorData.LoadData();
            RecordPositions();

		}

        public static bool StandingStillForTimePeriod(Int32 periodToCheck = 0)
        {
            if (periodToCheck == 0)
            {
                if ((Core.StopWatch.ElapsedMilliseconds - E3.LastMovementTimeStamp) > E3.GeneralSettings.Movement_StandingStill)
                {
                    return true;
                }
            }
            else
            {
                if ((Core.StopWatch.ElapsedMilliseconds - E3.LastMovementTimeStamp) > periodToCheck)
                {
                    return true;
                }
            }
            return false;
        }
        public static bool MillisecondsSinceLastFD(Int32 periodToCheck)
        {
            if ((Core.StopWatch.ElapsedMilliseconds - E3.LastFDTimeStamp) > periodToCheck)
            {
                return true;
            }
            return false;
        }

        public static void Reset()
        {
            AnchorTarget = 0;
            Anchor_X = double.MinValue;
            Anchor_Y = double.MinValue;
            Anchor_Z = double.MinValue;
            Following = false;
            FollowTargetName = String.Empty;
            ChaseTargetName = String.Empty;
            E3FollowTargetName = String.Empty;
        }
        [ClassInvoke(Data.Class.All)]
        public static void Check_Chase()
        {
            if (ChaseTargetName == String.Empty) return;
            if (!e3util.ShouldCheck(ref _nextChaseCheck, _nextChaseCheckInterval)) return;

            if (MovementPaused) return;

            using (_log.Trace())
            {

                if (ChaseTargetName != String.Empty && !Assist.IsAssisting)
                {
                    if (_spawns.TryByName(ChaseTargetName, out var spawn))
                    {
                        double distance = spawn.Distance;
                        double minDistanceToChase = E3.GeneralSettings.Movement_ChaseDistanceMin;
                        double maxDistanceToChase = E3.GeneralSettings.Movement_ChaseDistanceMax;


                        if (distance != -1)
                        {
                            Following = true;
                            bool InLoS = MQ.Query<bool>($"${{Spawn[={ChaseTargetName}].LineOfSight}}");
                            bool navLoaded = MQ.Query<bool>("${Bool[${Navigation.MeshLoaded}]}");
                            if (navLoaded)
                            {

                                Double navPathLength = MQ.Query<Double>($"${{Navigation.PathLength[id {spawn.ID}]}}");

                                if (distance > minDistanceToChase && navPathLength < maxDistanceToChase)
                                {
                                    e3util.NavToSpawnID(spawn.ID);
                                }

                            }
                            else
                            {
                                if (distance > minDistanceToChase && distance < 150 && InLoS)
                                {
                                    double x = spawn.X;
                                    double y = spawn.Y;
                                    double z = spawn.Z;
                                    e3util.TryMoveToLoc(x, y, z, 5, -1);
                                }
                            }
                        }
                    }
                    else
                    {
                        _spawns.RefreshList(full: true);
                    }
                }
            }
        }

        private static Dictionary<string, LinkedList<(float, float, float)>> _e3followpaths = new Dictionary<string, LinkedList<(float, float, float)>>();
        [ClassInvoke(Data.Class.All)]
        public static void RecordPositions()
        {
            foreach(var user in E3.Bots.BotsConnected(readOnly: true))
            {

                if (user != E3FollowTargetName) continue;

                //don't record positions if they are in a different zone.
                if (E3.ZoneID != E3.Bots.Query<Int32>(user,"${Me.ZoneID}")) continue;

				float x = E3.Bots.Query<float>(user, "${Me.X}");
				float y = E3.Bots.Query<float>(user, "${Me.Y}");
				float z = E3.Bots.Query<float>(user, "${Me.Z}");

                if (x == 0 && y == 0 && z == 0) continue;

				if (!_e3followpaths.ContainsKey(user))
				{
					_e3followpaths.Add(user, new LinkedList<(float, float, float)>());
				}
				var path = _e3followpaths[user];


                

				//var distanceFromMe = e3util.GetDistanceFromMe(x, y, z);

                //less than 5 units from the follow distance, don't record and chill.
                //if (Math.Abs((decimal)_followMeDistance -distanceFromMe) < 5)
                //{
                //    path.Clear();
                //    continue;
                //}

                

				if (!_e3followpaths.ContainsKey(user))
                {
                    _e3followpaths.Add(user, new LinkedList<(float, float, float)>());
                }
                //we can see them, just take this is the final location.
				if (MQ.Query<bool>($"${{Spawn[{FollowTargetName}].LineOfSight}}"))
                {
                    path.Clear();
                }

				if (path.Count>0)
                {
                    var xyz = path.First.Value;
                    float c_x = xyz.Item1;
                    float c_y = xyz.Item2;
                    float c_z = xyz.Item3;

					var nx = c_x - x;
					var ny = c_y - y;
					var nz = c_z - z;
                   
					//we can calculate distance
					decimal distance = (Decimal)Math.Sqrt(nx * nx + ny * ny + nz * nz);
                    if(distance > 1)
                    {
                        //we have a new update 
         				path.AddFirst((x, y, z));
                    }
				}
                else
                {
					//we have no path so far, lets just add
					path.AddFirst((x, y, z));
				}

                if(path.Count>3000)
                {
                    path.RemoveLast();
                }
                
			}
		}

        [ClassInvoke(Data.Class.All)]
        public static void Check_E3Follow()
        {

			if (!e3util.ShouldCheck(ref _nextE3FollowCheck, _nextE3FollowCheckInterval)) return;

			if (String.IsNullOrWhiteSpace(E3FollowTargetName)) return;

			if (Assist.IsAssisting) return;
			if (MovementPaused) return;


            //find the path of the user

            if (!_e3followpaths.ContainsKey(E3FollowTargetName)) return;

            var path = _e3followpaths[E3FollowTargetName];

            if(path.Count>0)
            {
				var xyz = path.Last.Value;
				float c_x = xyz.Item1;
				float c_y = xyz.Item2;
				float c_z = xyz.Item3;

                var distance = e3util.GetDistanceFromMe(c_x, c_y, c_z);

                if(distance > 200)
                {
                    //distance is way too far
                    path.Clear();
                    return;
                }
                else if(distance > (decimal)_followMeDistance)
                {
					if (!Debugger.IsAttached)
					{
                        double zdiff = Math.Abs(E3.Loc_Z - c_z);
						if (zdiff > 10)
						{
							MQ.Write($"Zdiff is {zdiff}");

							Core.mq_LookAt(c_x, c_y, c_z);
						}
     				}
                    if (_e3follownavfallback &&  distance > 60 && distance  < 200 
                        && !MQ.Query<bool>($"${{Spawn[{E3FollowTargetName}].LineOfSight}}") 
                        && MQ.Query<bool>($"${{Navigation.PathExists[spawn {E3FollowTargetName}]}}"))
                    {
							MQ.Write($"Possibly stuck, trying to nav to {E3FollowTargetName}. Distance is {distance}");
							MQ.Cmd($"/nav spawn {E3FollowTargetName}");
							MQ.Delay(1000);
							path.Clear();
							while (MQ.Query<bool>("${Navigation.Active}"))
							{
								MQ.Delay(50);
							}
							return;
					}
					else
                    {
						MQ.Cmd($"/squelch /moveto loc {c_y} {c_x} mdist {_followMeDistance}");
						MQ.Write($"Distance is {distance} trying to to move to {c_x},{c_y}, {c_z}");

					}


					// e3util.TryMoveToLoc(c_x, c_y, c_z, (int) _followMeDistance,usenavifavail:false);
				}
				path.RemoveLast();
			}
		}


		public static void RemoveFollow()
        {
            ChaseTargetName = String.Empty;
            FollowTargetName = string.Empty;
            E3FollowTargetName = string.Empty;
            Following = false;
            MQ.Cmd("/squelch /afollow off");
            MQ.Cmd("/squelch /stick off");


        }
        [ClassInvoke(Data.Class.All)]
        public static void AcquireFollow()
        {

            if (!e3util.ShouldCheck(ref _nextFollowCheck, _nextFollowCheckInterval)) return;

            if (String.IsNullOrWhiteSpace(FollowTargetName)) return;
 
            if (Assist.IsAssisting) return;
            if (MovementPaused) return;           
            Spawn s;
            if (_spawns.TryByName(FollowTargetName, out s))
            {
                if (s.Distance <= 250)
                {
                    if (!MQ.Query<bool>("${AdvPath.State}"))
                    {
                        //they are in range
                        if (MQ.Query<bool>($"${{Spawn[{FollowTargetName}].LineOfSight}}"))
                        {
                            if (Casting.TrueTarget(s.ID))
                            {
                                MQ.Delay(100);
                                MQ.Delay(100);
                                //if a bot, use afollow, else use stick
                                string mqcommand = $"/afollow on nodoor {_followMeDistance}";
								MQ.Cmd(mqcommand);
                               // E3.Bots.Broadcast($"issuing command {mqcommand}");
                                Following = true;
                            }
                        }
                    }
                }
                else
                {
                    Following = false;
                }
            }
            else
            {  
                //they are not in zone, sanity check refresh
                _spawns.RefreshList(full: true);
            }
        }
        
        public static void PauseMovement()
        {
            if (MQ.Query<bool>("${Stick.Active}")) MQ.Cmd("/squelch /stick off");
            if (MQ.Query<bool>("${AdvPath.Following}")) MQ.Cmd("/squelch /afollow off ");
            if (Movement.Following) Movement.Following = false;
        }
        public static void ResetKeepFollow()
        {
            AnchorTarget = 0;
            Anchor_X = double.MinValue;
            Anchor_Y = double.MinValue;
            Anchor_Z = double.MinValue;
            Following = false;
            //clear out the recorded paths
            foreach(var pair in _e3followpaths)
            {
                pair.Value.Clear();
            }
        }
        public static bool AnchorEnabled()
        {
            if(e3util.IsManualControl())
            {
                return false;
            }
            if (AnchorTarget > 0 || Anchor_X != double.MinValue) return true;
            return false;
        }
        [ClassInvoke(Data.Class.All)]
        public static void Check_Anchor()
        {
            if (!e3util.ShouldCheck(ref _nextAnchorCheck, _nextAnchorCheckInterval)) return;
            if (AnchorEnabled() && !Assist.IsAssisting)
            {
                MoveToAnchor(); 
            }
        }
        public static bool IsMoving()
        {
            bool moving = false;
            if (!moving && MQ.Query<bool>("${Me.Moving}")) moving = true;
            if (!moving && MQ.Query<bool>("${AdvPath.Following}")) moving = true;
            if (!moving && MQ.Query<bool>("${MoveTo.Moving}")) moving = true;
            if (!moving && MQ.Query<bool>("${Navigation.Active}")) moving = true;
            return moving;
        }
        public static bool IsNavigating()
        {
            bool moving = false;
            if (!moving && MQ.Query<bool>("${MoveTo.Moving}")) moving = true;
            if (!moving && MQ.Query<bool>("${Navigation.Active}")) moving = true;
            return moving;
        }
        private static double GetDistance3D(double MyX, double MyY, double MyZ, double TargetX, double TargetY, double TargetZ)
        {
            double dx = TargetX - MyX;
            double dy = TargetY - MyY;
            double dz = TargetZ - MyZ;

            return Math.Sqrt(dx * dx + dy * dy + dz * dz);
        }
        public static void MoveToAnchor()
        {
            if (!AnchorEnabled()) return;

            _spawns.RefreshList();
            Spawn s;
            if(Anchor_X!=double.MinValue)
            {
                if(_spawns.TryByID(E3.CurrentId,out s))
                {
                    double distance = GetDistance3D(s.X, s.Y, s.Z,Anchor_X, Anchor_Y, Anchor_Z);
                    if (distance > E3.GeneralSettings.Movement_AnchorDistanceMin && distance < E3.GeneralSettings.Movement_AnchorDistanceMax)
                    {
                        e3util.TryMoveToLoc(Anchor_X, Anchor_Y, Anchor_Z);
                    }
                }

            }
            else if (_spawns.TryByID(AnchorTarget, out s))
            {
                if (s.Distance > E3.GeneralSettings.Movement_AnchorDistanceMin && s.Distance < E3.GeneralSettings.Movement_AnchorDistanceMax)
                {
                    e3util.TryMoveToLoc(s.X, s.Y,s.Z);
                }
            }
        }
        
        static void RegisterEvents()
        {

			EventProcessor.RegisterCommand("/e3movement", (x) =>
			{
				if (x.args.Count > 0)
				{

					if (x.args[0].Length == "pause".Length && x.args[0].IndexOf("pause", 0, "pause".Length, StringComparison.OrdinalIgnoreCase) != -1)
					{
						if (FollowTargetName != String.Empty || ChaseTargetName != string.Empty)
						{
							E3.Bots.Broadcast("Pausing movement");
							MovementPaused = true;
							PauseMovement();
						}
						else
						{
							E3.Bots.Broadcast("Currently not following anyone.");
						}
					}
					else if (x.args[0].Length == "resume".Length && x.args[0].IndexOf("resume", 0, "resume".Length, StringComparison.OrdinalIgnoreCase) != -1)
					{
						if (FollowTargetName != String.Empty || ChaseTargetName != string.Empty)
						{
							E3.Bots.Broadcast("Resuming movement");
							MovementPaused = false;
							Following = false;

						}
						else
						{
							E3.Bots.Broadcast("Currently not following anyone.");
						}
					}
				}
			}, "pause/resume movement");
			EventProcessor.RegisterCommand("/scatter", (x) => {

                Int32 Distance = 10;
                if(x.args.Count>0)
                {
                    Int32.TryParse(x.args[0], out Distance);
                }
                double currentX = MQ.Query<double>("${Me.X}");
                double currentY = MQ.Query<double>("${Me.Y}");
             
                E3.Bots.BroadcastCommandToGroup($"/e3movetorandomloc \"{currentX}\" \"{currentY}\" \"{Distance}\"",x,true);
            });
            EventProcessor.RegisterCommand("/e3movetorandomloc", (x) => {
                double currentX = 0;
                double currentY = 0;
                Int32 distance = 10;
                if (e3util.FilterMe(x)) return;
                if (x.args.Count > 2)
                {
                    if (!Double.TryParse(x.args[0], out currentX)) return;
                    if (!Double.TryParse(x.args[1], out currentY)) return;
                    if (!Int32.TryParse(x.args[2], out distance)) return;
                }
                else
                {
                    return;
                }
				if (e3util.IsEQLive())
				{
					//random delay so it isn't quite so ovious
					MQ.Delay(E3.Random.Next(1500, 3000));

				}
				double currentZ = MQ.Query<double>("${Me.Z}");
                e3util.TryMoveToLoc(currentX+E3.Random.Next(-1*distance,distance), currentY + E3.Random.Next(-1 * distance, distance), currentZ);

            });
            EventProcessor.RegisterCommand("/e3movetoloc", (x) => {


                double currentX = 0;
                double currentY = 0;
                if (e3util.FilterMe(x)) return;
                if (x.args.Count > 1)
                {
                    if (!Double.TryParse(x.args[0], out currentX)) return;
                    if (!Double.TryParse(x.args[1], out currentY)) return;
                }
                else
                {
                    return;
                }
                double currentZ = MQ.Query<double>("${Me.Z}");
                e3util.TryMoveToLoc(currentX, currentY, currentZ);
        
            });

            EventProcessor.RegisterCommand("/clickit", (x) =>
            {
                if (x.args.Count == 0)
                {
					//we are telling people to follow us
					E3.Bots.BroadcastCommandToGroup($"/clickit {Zoning.CurrentZone.Id}",x);

                }
                if (e3util.FilterMe(x)) return;
                //read the ini file and pull the info we need.
                if (x.args.Count > 0)
                {
                    Int32 zoneID;
                    if (Int32.TryParse(x.args[0], out zoneID))
                    {
                        if (zoneID != Zoning.CurrentZone.Id)
                        {
                            //we are not in the same zone, ignore.
                            return;   
                        }
                    }
                }

                Int32 closestID = _doorData.ClosestDoorID();

                //eqlives doors have differnt IDs, do the basic click
                if (closestID > 0 && !e3util.IsEQLive() && (_clickitUseDoorZones.Contains(Zoning.CurrentZone.ShortName,StringComparer.OrdinalIgnoreCase)))
                {
                    MQ.Cmd($"/doortarget id {closestID}");
                    double currentDistance = MQ.Query<Double>("${DoorTarget.Distance}");
                    int attempts = 3;
                    //need to move to its location
                    if (currentDistance < 50)
                    {
                        Double doorX = MQ.Query<double>("${DoorTarget.X}");
                        Double doorY = MQ.Query<double>("${DoorTarget.Y}");
                        Double doorZ = MQ.Query<double>("${DoorTarget.Z}");
                        // try more then once break out if you have moved or zoned
                        for (int i = 1; i <= attempts; i++)
                        {
                            MQ.Cmd($"/doortarget id {closestID}");
                            e3util.TryMoveToLoc(doorX, doorY, doorZ, 8, 3000);
                            MQ.Delay(100);
                            // Lets Get your Location to check if it changes after clicking the door
                            int preZoneID = MQ.Query<int>("${Zone.ID}");
                            double preDoorX = MQ.Query<double>("${Me.X}");
                            double preDoorY = MQ.Query<double>("${Me.Y}");
                            double preDoorZ = MQ.Query<double>("${Me.Z}");
                            MQ.Cmd("/squelch /click left door");
                            MQ.Delay(2100);
                            int postZoneID = MQ.Query<int>("${Zone.ID}");
                            double postDoorX = MQ.Query<double>("${Me.X}");
                            double postDoorY = MQ.Query<double>("${Me.Y}");
                            double postDoorZ = MQ.Query<double>("${Me.Z}");
                            double distanceMoved = MQ.Query<double>($"${{Math.Distance[{preDoorX},{preDoorY},{preDoorZ}:{postDoorX},{postDoorY},{postDoorZ}]}}");
                            // Check for Zone change or Movement
                            if (distanceMoved > 80.0 || preZoneID != postZoneID)
                            {
								_spawns.RefreshList(full: true);
								MQ.Write("\ayZone Detected");
                                break;
                            }
                            // Inform the user that no movement was detected
                            if (i == attempts)
                            {
                                E3.Bots.Broadcast("\arI Failed to Zone");
                                MQ.Write("\arZone FAILED");
                            }
                        }

                    }
                    else
                    {
                        MQ.Write("\arMove Closer To Door");
                    }
                }
                else
                {  //either eqlive or we don't have the id in our config
                    MQ.Cmd($"/doortarget");
					double currentDistance = MQ.Query<Double>("${DoorTarget.Distance}");
					if (currentDistance < 50)
					{
						Double doorX = MQ.Query<double>("${DoorTarget.X}");
						Double doorY = MQ.Query<double>("${DoorTarget.Y}");
						Double doorZ = MQ.Query<double>("${DoorTarget.Z}");
						e3util.TryMoveToLoc(doorX, doorY, doorZ, 8, 3000);
					}
					else
					{
						MQ.Write("Door distance is > 50 units away, not moving");
					}
					MQ.Cmd("/squelch /click left door");

				}
			});

            EventProcessor.RegisterCommand("/anchoron", (x) =>
            {
                

                if (x.args.Count > 0)
                {
                    if (e3util.FilterMe(x)) return;
                    AnchorFilters.Clear();
                    if (x.filters.Count > 0)
                    {
                        AnchorFilters.AddRange(x.filters);
                    }
                    if (x.args.Count==1)
                    {
                        Int32 targetid;
                        if (Int32.TryParse(x.args[0], out targetid))
                        {
                            AnchorTarget = targetid;
                            Anchor_X = double.MinValue;
                            Anchor_Y = double.MinValue;
                            Anchor_Z = double.MinValue;
                        }

                    }
                    else if(x.args.Count==3)
                    {
                        double ax;
                        if (double.TryParse(x.args[0], out ax))
                        {
                            double ay;
                            if(double.TryParse(x.args[1], out ay))
                            {
                                double az;
                                if (double.TryParse(x.args[2], out az))
                                {
                                    AnchorTarget = 0;
                                    Anchor_X = ax;
                                    Anchor_Y = ay;
                                    Anchor_Z = az;
                                }
                            }
                          
                        }
                    }
                }
                else
                {
                    Int32 targetid = MQ.Query<Int32>("${Target.ID}");
                    AnchorTarget = 0;
                    Anchor_X = double.MinValue;
                    Anchor_Y = double.MinValue;
                    Anchor_Z = double.MinValue;

                    if (targetid > 0 && targetid!=E3.CurrentId)
                    {
                        E3.Bots.BroadcastCommandToGroup($"/anchoron {targetid}",x);
                    }
                    else
                    {
                        if(_spawns.TryByID(E3.CurrentId,out var s))
                        {
                            E3.Bots.BroadcastCommandToGroup($"/anchoron {s.X} {s.Y} {s.Z}",x);
                            
                        }
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
                            ChaseTargetName = x.args[0];
                            Following = true;
                        }

                    }
                }
                //chaseme off
                else if (x.args.Count == 1 && x.args[0] == "off")
                {
                    E3.Bots.BroadcastCommandToGroup($"/chaseme off {E3.CurrentName}", x);
                    ChaseTargetName = String.Empty;
					E3FollowTargetName = String.Empty;
                    FollowTargetName = String.Empty;
					Following = false;
                }
                //chaseme off <toon name>
                else if (x.args.Count == 2 && x.args[0] == "off")
                {
                    if (!e3util.FilterMe(x))
                    {
                        ChaseTargetName = String.Empty;
                        Following = false;

                    }
                }
                else
                {
                    E3.Bots.BroadcastCommandToGroup($"/chaseme {E3.CurrentName}", x);
                    Following = false;
                }
            });
            EventProcessor.RegisterCommand("/anchoroff", (x) =>
            {
               
                if (x.args.Count == 0)
                {
                    E3.Bots.BroadcastCommandToGroup($"/anchoroff all",x);
                }

                if (!e3util.FilterMe(x))
                {
                    AnchorTarget = 0;
                    Anchor_X = double.MinValue;
                    Anchor_Y = double.MinValue;
                    Anchor_Z = double.MinValue;

                }

            });
            EventProcessor.RegisterCommand("/e3follow", (x) =>
            {
				string user = string.Empty;
				string distance = String.Empty;

                if (x.args.Contains("nonav"))
                {
                    x.args.Remove("nonav");
                    _e3follownavfallback = false;
                }
                else
                {
                    _e3follownavfallback = true;
                }

                //using strings as well floats can get weird going from value to string and back
                foreach (var arg in x.args)
				{
					if (float.TryParse(arg, out var _))
					{
						distance = arg;
						break;
					}
				}
				if (distance != String.Empty)
				{
					_followMeDistance = float.Parse(distance);
					if (_followMeDistance < 1) _followMeDistance = 1;
					if (_followMeDistance > 200) _followMeDistance = 200;
					x.args.Remove(distance);
				}
				else
				{
					_followMeDistance = 10;
				}
				if (x.args.Count > 0)
				{
					if (!e3util.FilterMe(x))
					{
						user = x.args[0];
						Spawn s;
						if (_spawns.TryByName(user, out s))
						{

							E3FollowTargetName = user;
							if (E3.CurrentClass != Class.Bard)
							{
								Casting.Interrupt();
							}
							Rez.Reset();
							Assist.AssistOff();
                            Check_E3Follow();
						}
					}
				}
				else
				{
					Rez.Reset();
					//we are telling people to follow us
					E3.Bots.BroadcastCommandToGroup("/e3follow " + E3.CurrentName + $" {_followMeDistance}", x);

				}

			});
			EventProcessor.RegisterCommand("/e3followoff", (x) =>
			{
				if (!x.args.Contains("me", StringComparer.OrdinalIgnoreCase))
				{
					ChaseTargetName = String.Empty;
					FollowTargetName = string.Empty;
                    E3FollowTargetName = String.Empty;
					Following = false;

					//we are telling everyone to stop following us
					string extraArgs = String.Empty;

					if (x.args.Contains("tome", StringComparer.OrdinalIgnoreCase))
					{
						double currentX = MQ.Query<double>("${Me.X}");
						double currentY = MQ.Query<double>("${Me.Y}");
						double currentZ = MQ.Query<double>("${Me.Z}");
						int zoneID = MQ.Query<int>("${Zone.ID}");
						extraArgs += $" tome={currentX}/{currentY}/{currentZ}/{zoneID}";
					}
					E3.Bots.BroadcastCommandToGroup($"/e3followoff me{extraArgs}", x);
				}
				else
				{
					if (e3util.FilterMe(x)) return;
					RemoveFollow();
					foreach (var arg in x.args)
					{
						if (arg.StartsWith("tome="))
						{
							//if (_spawns.TryByName(FollowTargetName, out var s))
							{
								string[] strings = arg.Split(new char[] { '=' });
								string[] xyz = strings[1].Split(new char[] { '/' });

								if (xyz.Length > 2)
								{
									double xval;
									double yval;
									double zval;
									int zoneID;
									int.TryParse(xyz[3], out zoneID);
									if (MQ.Query<int>("${Zone.ID}") != zoneID) return;
									if (double.TryParse(xyz[0], out xval))
									{
										if (double.TryParse(xyz[1], out yval))
										{
											if (double.TryParse(xyz[2], out zval))
											{
												e3util.TryMoveToLoc(xval, yval, zval);
												break;
											}
										}
									}
								}
							}
						}
					}

				}
			});

			EventProcessor.RegisterCommand("/followme", (x) =>
            {
                string user = string.Empty;

                //need to check if a distance is supplid in the args
                //kinda hacky bit keeps the old legacy logic the same and allows users to do /followme 30
                string distance =String.Empty;
                //using strings as well floats can get weird going from value to string and back
                foreach (var arg in x.args)
                {
                    if(float.TryParse(arg, out var _))
                    {
                        distance = arg;
                        break;
                    }
                }
                if (distance !=String.Empty)
                {
                    _followMeDistance = float.Parse(distance);
                    if (_followMeDistance < 1) _followMeDistance = 1;
                    if (_followMeDistance > 200) _followMeDistance = 200;
                    x.args.Remove(distance);
                }
                else
                {
                    _followMeDistance = 10;
                }
                if (x.args.Count > 0)
                {
                    if (!e3util.FilterMe(x))
                    {
                        user = x.args[0];
                        Spawn s;
                        if (_spawns.TryByName(user, out s))
                        {

                            FollowTargetName = user;
                            Following = false;
                            if (E3.CurrentClass != Class.Bard)
                            {
                                Casting.Interrupt();
                            }
                            Rez.Reset();
                            Assist.AssistOff();
                            AcquireFollow();
                        }

                    }
                }
                else
                {
                    Rez.Reset();
                    //we are telling people to follow us
                    E3.Bots.BroadcastCommandToGroup("/followme " + E3.CurrentName + $" {_followMeDistance}", x);

                }
            });

            EventProcessor.RegisterCommand("/mtm", (x) => {

                if (x.args.Count==0)
                {
                    E3.Bots.BroadcastCommandToGroup($"/mtm {E3.CurrentName}", x);
                }
                else
                {
                    if (e3util.FilterMe(x)) return;
                    if (_spawns.TryByName(x.args[0], out var s))
                    {
                        Casting.TrueTarget(s.ID);
                        e3util.TryMoveToTarget();
                    }
                }
            }
            );
            EventProcessor.RegisterCommand("/followoff", (x) =>
            {
                if (!x.args.Contains("me",StringComparer.OrdinalIgnoreCase))
                {
                    ChaseTargetName = String.Empty;
                    FollowTargetName = string.Empty;
					E3FollowTargetName = String.Empty;
					Following = false;

                    //we are telling everyone to stop following us
                    string extraArgs = String.Empty;

                    if(x.args.Contains("tome", StringComparer.OrdinalIgnoreCase))
                    {
                        double currentX = MQ.Query<double>("${Me.X}");
                        double currentY = MQ.Query<double>("${Me.Y}");
                        double currentZ = MQ.Query<double>("${Me.Z}");
                        int zoneID = MQ.Query<int>("${Zone.ID}");
                        extraArgs += $" tome={currentX}/{currentY}/{currentZ}/{zoneID}";
                    }
                    E3.Bots.BroadcastCommandToGroup($"/followoff me{extraArgs}",x);
                }
                else
                {
                    if (e3util.FilterMe(x)) return;
                    RemoveFollow();
                    foreach (var arg in x.args)
                    {
                        if (arg.StartsWith("tome="))
                        {
                            //if (_spawns.TryByName(FollowTargetName, out var s))
                            {
                                string[] strings = arg.Split(new char[] { '=' });
                                string[] xyz = strings[1].Split(new char[] { '/' });

                                if (xyz.Length>2)
                                {
                                    double xval;
                                    double yval;
                                    double zval;
                                    int zoneID;
                                    int.TryParse(xyz[3], out zoneID);
                                    if (MQ.Query<int>("${Zone.ID}") != zoneID) return; 
                                    if (double.TryParse(xyz[0], out xval))
                                    {
                                        if (double.TryParse(xyz[1], out yval))
                                        {
                                            if (double.TryParse(xyz[2], out zval))
                                            {
                                                e3util.TryMoveToLoc(xval, yval, zval);
                                                break;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                   
                }
            });
            EventProcessor.RegisterCommand("/rtz", (x) =>
            {
                if (x.args.Count > 0)
                {
                    if (e3util.FilterMe(x)) return;
                    //someone telling us to rtz
                    double heading;
                    if (double.TryParse(x.args[0], out heading))
                    {
                        Movement.PauseMovement();
                        Int32 currentZone = MQ.Query<Int32>("${Zone.ID}");
                        if(e3util.IsEQLive())
                        {
							MQ.Cmd($"/face heading {heading * -1}",500);
						}
                        else
                        {
							MQ.Cmd($"/face fast heading {heading * -1}");
						}
                        MQ.Delay(600);
                        MQ.Cmd("/nomodkey /keypress forward hold");
                        MQ.Delay(3000);
                        Int32 counter = 0;
                        while (Zoning.CurrentZone.Id == currentZone && counter < 20)
                        {
                            counter++;
                            MQ.Delay(100);
                            currentZone = MQ.Query<Int32>("${Zone.ID}");
                        }
						_spawns.RefreshList(full: true);
					}
                }
                else
                {
                    //tell others to rtz
                    //get our faced heading
                    double heading = MQ.Query<double>("${Me.Heading.Degrees}");
                   
                    E3.Bots.BroadcastCommandToGroupZone($"/rtz {heading}",x,false);
                    if (e3util.FilterMe(x)) return;
                    MQ.Delay(1000);
                    if(e3util.IsEQLive())
                    {
						MQ.Cmd($"/face heading {heading * -1}",500);
                 	}
                    else
                    {
						MQ.Cmd($"/face fast heading {heading * -1}");
					}
                    MQ.Cmd("/nomodkey /keypress forward hold");
                }
            });
            EventProcessor.RegisterCommand("/coth", (x) =>
            {
                string cothTarget = string.Empty;

                if (x.args.Count > 0)
                {
                    cothTarget = x.args[0];
                    Basics.RefreshGroupMembers();

                    if (cothTarget.Equals("group", StringComparison.OrdinalIgnoreCase))
                    {
                        PlayerSummon("group");
                        return;
                    }
                    Spawn s;
                    if (_spawns.TryByName(cothTarget, out s))
                    {
                        if (!Basics.GroupMembersInZone.Contains(s.ID)) 
                        {
                            E3.Bots.Broadcast($"{s.CleanName} is not in our group, can't summon.");
                            return;
                        }
                        
                        PlayerSummon(s.CleanName);
                    }
                }
            });
        }

        public static void PlayerSummon(string cothTarget)
        {
            // {FindItem[=Wayfarers Brotherhood Emblem].Clicky} == Call of the Hero, SpellID 1771
            Spell summonSpell;
            List<string> memberNames = E3.Bots.BotsConnected();
            Spawn s;

            if (MQ.Query<bool>($"${{Me.Book[Call of the Hero]}}") || MQ.Query<bool>($"${{Me.AltAbility[Call of the Hero].Spell}}"))
            {
                summonSpell = new Spell("Call of the Hero");
            }
            else if (MQ.Query<bool>($"${{Bool[${{FindItem[=Wayfarers Brotherhood Emblem].Clicky}}"))
            {
              
                summonSpell = new Spell("Wayfarers Brotherhood Emblem");
            }
            else
            {
                MQ.Write("No coth mechanism available, can't coth");
                return;
            }

            if (cothTarget.Equals("group", StringComparison.OrdinalIgnoreCase))
            {
                MQ.Write("Summoning group members.");
            }
            else if (_spawns.TryByName(cothTarget, out s))
            {
                if (s.Distance < 50 && MQ.Query<bool>($"${{Spawn[id {s.ID}].LineOfSight}}"))
                {
                    E3.Bots.Broadcast($"{s.DisplayName} is within 50 units and in LOS, not summoning.");
                    return;
                }

                if (s.TypeDesc == "NPC")
                {
                    E3.Bots.Broadcast($"{s.DisplayName} can't summon NPCs.");
                    return;
                }

                if (Casting.CheckReady(summonSpell))
                {
                    MQ.Cmd($"/g E3 Single Coth: Casting \"Call of the Hero\" on: {s.CleanName}");
                    if (MQ.Query<bool>("${Me.Invis}")) MQ.Cmd("/makemevisible");
                    Casting.Cast(s.ID, summonSpell);

                    if (E3.CurrentClass == Class.Bard)
                    {
                        MQ.Write("Delaying for 12 sec for coth to complete");
                        MQ.Delay(12000);
					}
                    
                }
                else
                {
                    E3.Bots.Broadcast($"{s.CleanName} not summoning. Issue with checkready on {summonSpell.CastName}");
                }
                return;
            }

            //randomly pick group member
            foreach (int memberid in Basics.GroupMembersInZone.OrderBy(x=>Guid.NewGuid()).ToList())
            {
                //if (Basics.InCombat())
                //{
                //    E3.Bots.Broadcast("In combat or assist was called, cancelling summon");
                //    return;
                //}
                if (_spawns.TryByID(memberid, out s))
                {
                    if (s.Distance < 50 && MQ.Query<bool>($"${{Spawn[id {s.ID}].LineOfSight}}"))
                    {
                        E3.Bots.Broadcast($"{s.CleanName} is within 50 units and in LOS, not summoning.");
                            continue;
                    }
                    if (Casting.CheckReady(summonSpell))
                    {
                        MQ.Cmd($"/g E3 Group Coth: Casting \"Call of the Hero\" on: {s.CleanName}");
                        if (MQ.Query<bool>("${Me.Invis}")) MQ.Cmd("/makemevisible");
                        Casting.Cast(memberid, summonSpell);
						if (E3.CurrentClass == Class.Bard)
						{
							MQ.Write("Delaying for 12 sec for coth to complete");
							MQ.Delay(12000);
						}
			//			e3util.YieldToEQ();//not really needed as there are tons of delays in casting
                    }
                    else
                    {
                        E3.Bots.Broadcast($"{s.CleanName} not summoning. Issue with checkready on {summonSpell.CastName}");
                    }
                }
            }
            E3.Bots.Broadcast("Finished summoning group");
        }
    }
}
