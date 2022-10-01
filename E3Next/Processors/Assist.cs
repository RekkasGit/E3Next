using MonoCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace E3Core.Processors
{
    public static class Assist
    {
        public static Logging _log = E3._log;
        private static IMQ MQ = E3.MQ;

        private static bool use_FULLBurns;
        private static bool use_QUICKBurns;
        private static bool use_EPICBurns;
        private static bool use_LONGBurns;
        private static bool use_Swarms;
        private static Dictionary<Int32, Int32> _resistCounters = new Dictionary<int, int>();

        public static Boolean _isAssisting = false;
        public static Int32 _assistTargetID = 0;
        public static Int32 _assistStickDistance = 10;
        public static string _epicWeaponName=String.Empty;
        public static string _AnguishBPName=String.Empty;


        public static void Init()
        {
            RegisterEvents();
        }

        private static void RegisterEpicAndAnguishBP()
        {


        }
        private static void RegisterEvents()
        {

        }
    }
}
