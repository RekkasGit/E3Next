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
    public static class Cleric
    {
        public static Logging _log = E3._log;
        private static IMQ MQ = E3.MQ;
        private static ISpawns _spawns = E3._spawns;
        private static bool _isInit = false;


      
        public static void Init()
        {
            if (_isInit) return;
            _isInit = true;
        }
        

    }
}
