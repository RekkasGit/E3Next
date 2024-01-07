using E3Core.Processors;

using MonoCore;

namespace E3Core.Classes
{
    /// <summary>
    /// Properties and methods specific to the cleric class
    /// </summary>
    public static class Cleric
    {
        private static Logging _log = E3.Log;
        private static IMQ MQ = E3.MQ;
        private static ISpawns _spawns = E3.Spawns;
        private static bool _isInit = false;


        /// <summary>
        /// Initializes this instance.
        /// </summary>
        public static void Init()
        {
            if (_isInit) return;
            _isInit = true;
        }

    }
}
