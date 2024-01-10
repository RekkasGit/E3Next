using E3Core.Settings;
using E3Core.Utility;

using MonoCore;

using System.Linq;

namespace E3Core.Processors
{
    public static class Hunter
    {
        public static Logging _log = E3.Log;
        private static IMQ MQ = E3.MQ;
        private static ISpawns _spawns = E3.Spawns;

        private static string _Filter;

        private static int _ActiveTarget = 0;

        private enum State
        {
            Disabled = 0,
            Acquiring = 1,
            Navigating = 2,
            Murder = 3,
            Looting = 4
        }
        private static State _CurrentState = State.Disabled;

        private const int navFuzzyDistance = 30;

        [SubSystemInit]
        public static void SystemInit()
        {
            EventProcessor.RegisterCommand("/hunt", (x) =>
            {
                ClearXTargets.FaceTarget = true;
                ClearXTargets.StickTarget = false;

                if (x.args.Count == 0)
                {
                    MQ.Write("Requires 1 or more arguments: mobNameFilter | off");
                }
                else if (x.args.Count == 1 && x.args[0] == "off")
                {
                    MQ.Write("Hunter disabled");
                    _CurrentState = State.Disabled;
                    _ActiveTarget = 0;
                    _Filter = null;
                    MQ.Cmd("/nav stop");
                    MQ.Cmd("/stick off");
                }
                else if (x.args.Count == 1)
                {
                    MQ.Write($"Hunter enabled with filter [{x.args[0]}]");
                    _Filter = x.args[0];
                    _CurrentState = State.Acquiring;
                }
            });
        }

        [ClassInvoke(Data.Class.All)]
        public static void Check_Hunter()
        {
            if (_CurrentState == State.Disabled) return;

            e3util.YieldToEQ();
            _spawns.RefreshList();

            //MQ.Write($"State = {_CurrentState}");
            switch (_CurrentState)
            {
                case State.Acquiring: HandleStateAcquiring(); break;

                case State.Navigating: HandleStateNavigating(); break;

                case State.Murder: HandleStateMurder(); break;

                case State.Looting: HandleStateLooting(); break;
            }
        }

        private static void HandleStateAcquiring()
        {
            _ActiveTarget = _spawns.Get()
                .Where(x => x.TypeDesc != "Corpse")
                .Where(x => x.Name.Contains(_Filter))
                .Where(x => MQ.Query<bool>($"${{Navigation.PathExists[id {x.ID} distance={navFuzzyDistance}]}}"))
                .OrderBy(x => x.Distance).Select(x => x.ID)
                .FirstOrDefault();

            if (_ActiveTarget > 0)
            {
                MQ.Cmd($"/target id {_ActiveTarget}");
                StartNavigation();
            }
        }

        private static void HandleStateNavigating()
        {
            if (!ActiveTargetExists())
            {
                _CurrentState = State.Acquiring;
            }
            else
            {
                // TODO - Mob may be moving, so when velocity = 0 we need to stick then wait until within melee range. Thats a new state

                if (MQ.Query<int>("${Navigation.Velocity}") > 0) return; // still moving

                MQ.Cmd("/nav stop");

                MQ.Write("Sticking...");
                MQ.Cmd("/stick 5");
                MQ.Delay(1000);

                MQ.Write("Murder Time");

                _CurrentState = State.Murder;
                _ = Casting.TrueTarget(_ActiveTarget);
                //MQ.Cmd("/assistme");
                MQ.Cmd("/stand");
                MQ.Cmd("/attack");
                MQ.Cmd("/stick hold moveback 10");
            }
        }

        private static void HandleStateMurder()
        {
            if (!ActiveTargetExists())
            {
                MQ.Write("2");
                _CurrentState = State.Acquiring;
            }

            bool sticking = MQ.Query<bool>("${Stick.Active}");
            if (!sticking) MQ.Cmd("/stick hold moveback 5");
            if (ActiveTargetDead()) _CurrentState = State.Looting;
        }

        private static void HandleStateLooting()
        {
            MQ.Delay(100);
            Loot.LootArea(false);
            _CurrentState = State.Acquiring;
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
            bool navPathExists = MQ.Query<bool>($"${{Navigation.PathExists[id {_ActiveTarget} distance={navFuzzyDistance}]}}");

            if (!navPathExists)
            {
                //early return if no path available
                MQ.Write($"\arNo nav path available to spawn ID: {_ActiveTarget}");
                _ActiveTarget = 0;
                _CurrentState = State.Disabled;
                return;
            }

            _CurrentState = State.Navigating;
            MQ.Cmd($"/nav id {_ActiveTarget} distance={navFuzzyDistance}");
        }
    }
}
