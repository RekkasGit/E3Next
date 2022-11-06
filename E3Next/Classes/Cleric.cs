using E3Core.Processors;
using E3Core.Settings;
using System;
using E3Core.Classes;
using E3Core.Data;
using E3Core.Utility;
using MonoCore;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using Microsoft.Win32;

namespace E3Core.Classes
{
    /// <summary>
    /// Properties and methods specific to the cleric class
    /// </summary>
    public static class Cleric
    {
        private static Logging _log = E3.Log;
        private static IMQ MQ = E3.Mq;
        private static ISpawns _spawns = E3.Spawns;
        private static bool _isInit = false;
        private static long _nextRezCheck = 0;
        private static long _nextRezCheckInterval = 10000;
        private static List<string> _resSpellList = new List<string>()
        {
            "Blessing of Resurrection",
            "Water Sprinkler of Nem Ankh",
            "Reviviscence",
            "Token of Resurrection",
            "Spiritual Awakening",
            "Resurrection",
            "Restoration",
            "Resuscitate",
            "Renewal",
            "Revive",
            "Reparation"
        };

        /// <summary>
        /// Initializes this instance.
        /// </summary>
        [ClassInvoke(Class.Cleric)]
        public static void Init()
        {
            if (_isInit) return;
            InitRezSpells();
            _isInit = true;
        }
     
    }
}
