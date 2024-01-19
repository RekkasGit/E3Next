using E3Core.Data;
using E3Core.Settings;
using E3Core.Utility;

using IniParser;
using IniParser.Model;

using MonoCore;

using System;
using System.Collections.Generic;
using System.IO;

namespace E3Core.Processors
{
    public class WaypointHuntingProfile : BaseSettings, IBaseSettings
    {
        private string _zoneName;
        private string _profileName;

        private int _CurrentWP = 0;
        private readonly List<string> _waypoints = new List<string>();

        public static WaypointHuntingProfile LoadProfile(Zone currentZone, string profileName)
        {
            if (string.IsNullOrEmpty(profileName))
                profileName = "Default";

            var profile = new WaypointHuntingProfile();
            profile._zoneName = currentZone.ShortName;
            profile._profileName = profileName;
            profile.LoadData();
            return profile;
        }

        private void LoadData()
        {
            string filename = GetSettingsFilePath($"WPHunt_{_zoneName}_{_profileName}.ini");
            var parsedData = CreateSettings(filename);

            LoadKeyData("Waypoints", "Waypoint", parsedData, _waypoints);
        }

        public bool IsValid()
        {
            if (_waypoints.Count == 0)
            {
                MQ.Write("No waypoints defined");
                return false;
            }

            return true;
        }

        public IniData CreateSettings(string fileName)
        {
            FileIniDataParser parser = e3util.CreateIniParser();
            IniData newFile = new IniData();

            newFile.Sections.AddSection("Waypoints");
            var wlSection = newFile.Sections.GetSectionData("Waypoints");
            wlSection.Keys.AddKey("Waypoint", "");

            if (!File.Exists(fileName))
            {
                if (!Directory.Exists(_configFolder + _botFolder))
                {
                    Directory.CreateDirectory(_configFolder + _botFolder);
                }
                //file straight up doesn't exist, lets create it
                parser.WriteFile(fileName, newFile);
                _fileLastModified = File.GetLastWriteTime(fileName);
                _fileLastModifiedFileName = fileName;
            }
            else
            {
                //File already exists, may need to merge in new settings lets check
                //Parse the ini file
                //Create an instance of a ini file parser
                FileIniDataParser fileIniData = e3util.CreateIniParser();
                IniData tParsedData = fileIniData.ReadFile(fileName);

                //overwrite newfile with what was already there
                tParsedData.Merge(newFile);
                newFile = tParsedData;
                //save it it out now
                File.Delete(fileName);
                parser.WriteFile(fileName, tParsedData);

                _fileLastModified = File.GetLastWriteTime(fileName);
                _fileLastModifiedFileName = fileName;
            }

            return newFile;
        }

        public string CurrentWaypoint() => _waypoints[_CurrentWP];

        public bool MoveNext()
        {
            _CurrentWP++;
            return _CurrentWP < _waypoints.Count ;
        }

        public void Reset()
        {
            _CurrentWP = 0;
        }
    }

    public static class WaypointHunter
    {
        public static Logging _log = E3.Log;

        private static IMQ MQ = E3.MQ;
        private static ISpawns _spawns = E3.Spawns;

        private static WaypointHuntingProfile _Profile;

        private static int _ActiveTarget = 0;

        private static DateTime _NextAction = DateTime.MinValue;

        private enum State
        {
            Disabled = 0,
            NavigationStart = 1,
            Navigating = 2,
            Acquiring = 3,
            MurderStart = 4,
            Murder = 5,
            Looting = 6,
            WaitingToRepop = 7
        }

        private static State _CurrentStateV = State.Disabled;

        private static Random _Rand = new Random();

        private static State CurrentState
        {
            get => _CurrentStateV;
            set
            {
                _CurrentStateV = value;
                int rSec = _Rand.Next(2, 10);
                _NextAction = DateTime.Now.AddSeconds(rSec);
                MQ.Write($"State is now {value}; acting in {rSec}s");
            }
        }

        private static DateTime LastRepopTime = DateTime.Now;

        [SubSystemInit]
        public static void WPSystemInit()
        {
            EventProcessor.RegisterCommand("/wphunt", (x) =>
            {
                ClearXTargets.FaceTarget = true;
                ClearXTargets.StickTarget = false;

                bool doLoad = false;
                string profileName = null;

                if (x.args.Count == 0)
                {
                    doLoad = true;
                    // Name = null, let load specify;
                }
                else if (x.args.Count == 1 && x.args[0] == "off")
                {
                    Reset();
                }
                else if (x.args.Count == 1)
                {
                    doLoad = true;
                    profileName = x.args[0];
                }

                if (doLoad)
                {
                    LastRepopTime = DateTime.Now;
                    MQ.Write($"Waypoint Hunter enabled with profile [{profileName}]");

                    var tmpProfile = WaypointHuntingProfile.LoadProfile(Zoning.CurrentZone, profileName);
                    if (tmpProfile.IsValid())
                    {
                        _Profile = tmpProfile;
                        CurrentState = State.NavigationStart;
                    }
                }
            });

            var repop = new List<string> { "Instance repopped" };
            EventProcessor.RegisterEvent("InstanceRepopped", repop, (x) =>
            {
                LastRepopTime = DateTime.Now;
                MQ.Write("Detected instance repop; updating last repop time");
            });
        }

        [ClassInvoke(Data.Class.All)]
        public static void Check_WPHunter()
        {
            if (CurrentState == State.Disabled) return;

            if (DateTime.UtcNow < _NextAction) return;

            e3util.YieldToEQ();
            _spawns.RefreshList();

            switch (CurrentState)
            {
                case State.NavigationStart: StartNavigation(); break;

                case State.Navigating: HandleStateNavigating(); break;

                case State.Acquiring: HandleStateAcquiring(); break;

                case State.MurderStart: HandleStateMurderStart(); break;

                case State.Murder: HandleStateMurder(); break;

                case State.Looting: HandleStateLooting(); break;

                case State.WaitingToRepop: HandleStateWaitingToRepop(); break;
            }
        }

        public static void Reset()
        {
            MQ.Write("WPHunter disabled");
            CurrentState = State.Disabled;
            _ActiveTarget = 0;
            _Profile = null;
            MQ.Cmd("/nav stop");
            MQ.Cmd("/stick off");
        }

        private static void HandleStateAcquiring()
        {
            var nextTarget = MQ.Query<int>("${Spawn[npc radius 40 los targetable].ID}");

            if (nextTarget == 0)
            {
                CurrentState = State.Looting;
            }
            else
            {
                _ActiveTarget = nextTarget;
                CurrentState = State.MurderStart;
            }
        }

        private static void HandleStateMurderStart()
        {
            MQ.Write("Murder Time");

            _ = Casting.TrueTarget(_ActiveTarget);

            MQ.Write("Sticking...");
            MQ.Cmd("/stick 5");
            MQ.Delay(1000);

            CurrentState = State.Murder;

            MQ.Cmd("/stand");
            MQ.Cmd("/keypress 1");
            MQ.Cmd("/attack");
            MQ.Cmd("/stick hold moveback 10");
            MQ.Cmd("/face fast");
        }

        private static void HandleStateNavigating()
        {
            if (MQ.Query<bool>("${Navigation.Active}")) return; // still moving

            MQ.Delay(100);
            MQ.Cmd("/useitem \"Spirit of the Ninja\"");
            CurrentState = State.Acquiring;
            MQ.Cmd("/nav stop");
        }

        private static void HandleStateMurder()
        {
            if (ActiveTargetDead()) CurrentState = State.Acquiring;
        }

        private static void HandleStateLooting()
        {
            MQ.Delay(100);

            string waypoint = _Profile.CurrentWaypoint();
            MQ.Cmd($"/nav waypoint \"{waypoint}\"");

            while (MQ.Query<bool>("${Navigation.Active}"))
            {
                System.Threading.Thread.Sleep(250);
            }

            Loot.Reset();
            Loot.LootArea(false);

            // Nothing else in area?
            if (!_Profile.MoveNext())
            {
                _Profile.Reset();
                var remaining = LastRepopTime.AddMinutes(10.1).Subtract(DateTime.Now);
                MQ.Write($"Next repop time expected @ {LastRepopTime.AddMinutes(10.1).ToShortTimeString()} (eta {remaining.TotalSeconds} sec)");
                CurrentState = State.WaitingToRepop;
            }
            else
            {
                CurrentState = State.NavigationStart;
            }
        }

        private static bool ActiveTargetExists()
        {
            return _spawns.TryByID(_ActiveTarget, out var _);
        }

        private static bool ActiveTargetDead()
        {
            if (!_spawns.TryByID(_ActiveTarget, out Spawn s)) return true;
            if (s.TypeDesc == "Corpse") return true;
            return false;
        }

        private static void StartNavigation()
        {
            string waypoint = _Profile.CurrentWaypoint();

            MQ.Write($"Navigating to WayPoint [{waypoint}]");
            CurrentState = State.Navigating;
            MQ.Cmd($"/nav waypoint \"{waypoint}\"");
        }

        private static void HandleStateWaitingToRepop()
        {
            int xtargetId = MQ.Query<int>("${Me.XTarget[1].ID}");
            if (xtargetId > 0)
            {
                bool los = MQ.Query<bool>("${Me.XTarget[1].LineOfSight}");
                if (los)
                {
                    _ = Casting.TrueTarget(xtargetId, true);
                    MQ.Cmd("/face fast");
                }
            }

            var remaining = LastRepopTime.AddMinutes(10.1).Subtract(DateTime.Now);
            if (remaining.TotalSeconds > 0) return;

            MQ.Cmd("/say repop instance");
            MQ.Delay(1000);
            CurrentState = State.NavigationStart;
        }
    }
}
