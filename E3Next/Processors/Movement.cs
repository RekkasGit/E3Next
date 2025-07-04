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
        public static Int32 AnchorTarget = 0;
        public static double Anchor_X = double.MinValue;
        public static double Anchor_Y = double.MinValue;
        public static double Anchor_Z = double.MinValue;
        public static List<string> AnchorFilters = new List<string>();

		[ExposedData("Movement", "Following")]
		public static bool Following = false;
		//public static Int32 _followTargetID = 0;
		[ExposedData("Movement", "FollowTargetName")]
		public static string FollowTargetName = String.Empty;
        public static Logging _log = E3.Log;
        private static IMQ MQ = E3.MQ;
        private static ISpawns _spawns = E3.Spawns;
        public static DoorDataFile _doorData = new DoorDataFile();
        private static Int64 _nextAnchorCheck = 0;
        private static Int64 _nextAnchorCheckInterval = 1000;
        public static Int64 _nextFollowCheck = 0;
        private static Int64 _nextFollowCheckInterval = 1000;
        private static Int64 _nextChaseCheck = 0;
        private static Int64 _nextChaseCheckInterval = 10;
		[ExposedData("Movement", "ChaseTarget")]
		public static string ChaseTargetName = String.Empty;
		public static List<string> _clickitUseDoorZones = new List<string>() { "poknowledge", "potranq", "potimea", "potimeb","anguish","solrotower" };

		[SubSystemInit]
        public static void Movement_Init()
        {
            RegisterEvents();
             _doorData.LoadData();
    
        }

        public static bool StandingStillForTimePeriod()
        {
            if((Core.StopWatch.ElapsedMilliseconds - E3.LastMovementTimeStamp) > E3.GeneralSettings.Movement_StandingStill)
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
        }
        [ClassInvoke(Data.Class.All)]
        public static void Check_Chase()
        {
            if (ChaseTargetName == String.Empty) return;
            if (!e3util.ShouldCheck(ref _nextChaseCheck, _nextChaseCheckInterval)) return;

            using (_log.Trace())
            {

				if (ChaseTargetName != String.Empty && !Assist.IsAssisting)
                {
					if(_spawns.TryByName(ChaseTargetName, out var spawn))
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
                }
            }
        }

        public static void RemoveFollow()
        {
            ChaseTargetName = String.Empty;
            FollowTargetName = string.Empty;
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

            Spawn s;
            if (_spawns.TryByName(FollowTargetName, out s))
            {
                if (s.Distance <= 250)
                {
                    if (!Following)
                    {
                        //they are in range
                        //if (MQ.Query<bool>($"${{Spawn[{FollowTargetName}].LineOfSight}}"))
                        {
                            if (Casting.TrueTarget(s.ID))
                            {
                                MQ.Delay(100);
                                //if a bot, use afollow, else use stick
                                MQ.Cmd("/afollow on nodoor");
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
                           
                            FollowTargetName = user;
                            Following = false;
                            if(E3.CurrentClass!=Class.Bard)
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
                    E3.Bots.BroadcastCommandToGroup("/followme " + E3.CurrentName, x);
                   
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
                        if (!Basics.GroupMembers.Contains(s.ID)) 
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
                }
                else
                {
                    E3.Bots.Broadcast($"{s.CleanName} not summoning. Issue with checkready on {summonSpell.CastName}");
                }
                return;
            }

            //randomly pick group member
            foreach (int memberid in Basics.GroupMembers.OrderBy(x=>Guid.NewGuid()).ToList())
            {
                if (Basics.InCombat())
                {
                    E3.Bots.Broadcast("In combat or assist was called, cancelling summon");
                    return;
                }
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
                        e3util.YieldToEQ();//not really needed as there are tons of delays in casting
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
