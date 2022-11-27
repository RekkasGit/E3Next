using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using E3Core.Data;
using E3Core.Settings;
using E3Core.Settings.FeatureSettings;
using MonoCore;

namespace E3Core.Processors
{
    public static class Zoning
    {
        public static Zone CurrentZone;
        public static Dictionary<Int32, Zone> ZoneLookup = new Dictionary<Int32, Zone>();
        public static TributeDataFile TributeDataFile = new TributeDataFile();

        private static IMQ MQ = E3.Mq;

        [SubSystemInit]
        public static void Init()
        {
            InitZoneLookup();
        }

        public static void Zoned(Int32 zoneId)
        {
            // add our new zone to the zone lookup if necessary
            if (!ZoneLookup.TryGetValue(zoneId, out CurrentZone))
            {
                CurrentZone = new Zone(zoneId);
                ZoneLookup.Add(zoneId, new Zone(zoneId));
            }

            TributeDataFile.ToggleTribute();
        }

        private static void InitZoneLookup()
        {
            TributeDataFile.LoadData();

            // need to do this here because the event processors haven't been loaded yet
            var currentZone = MQ.Query<Int32>("${Zone.ID}");
            Zoned(currentZone);
        }
    }
}
