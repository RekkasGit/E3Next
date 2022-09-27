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


        public static Logging _log = Core._log;
        private static IMQ MQ = Core.mqInstance;


        public static void Init()
        {
            RegisterEventsCasting();
        }
        static void RegisterEventsCasting()
        {
            _log.Write("Regitering nowCast events....");
            List<String> r = new List<string>();
            r.Add("(.+) tells the group, 'MonoGC'");
            EventProcessor.RegisterEvent("GCollect", r, (x) => {
                _log.Write($"Processing {x.eventName}");

                string user = string.Empty;
                string spellName = String.Empty;
                Int32 targetid = 0;
                if (x.match.Groups.Count > 3)
                {
                    user = x.match.Groups[1].Value;
                    spellName = x.match.Groups[2].Value;
                    Int32.TryParse(x.match.Groups[3].Value, out targetid);

                }


                _log.Write($"{ x.eventName}:{ user} asked to GC collect");

                GC.Collect();

                _log.Write($"{ x.eventName}:{ user} asked to GC collect, Complete.");

            });



        }


        public static void AcquireFollow()
        {

        }

    }
}
