using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MonoCore;

namespace E3Core.Processors
{
    public static class Zoning
    {
        public static Zone CurrentZone;
        public static Dictionary<string, Zone> ZoneLookup = new Dictionary<string, Zone>();
        private static HashSet<string> _safeZones = new HashSet<string> { "poknowledge", "potranquility", "nexus", "guildhall", "freeporttemple", "arena", "bazaar" };

        private static IMQ MQ = E3.Mq;

        public class Zone
        {
            public Zone(string shortName)
            {
                ShortName = shortName;
                Name = MQ.Query<string>($"${{Zone[{shortName}].Name}}");
                Id = MQ.Query<int>($"${{Zone[{shortName}].ID}}");
                IsSafeZone = _safeZones.Contains(shortName);
            }

            public string Name { get; set; }
            public string ShortName { get; set; }
            public int Id { get; set; }
            public bool IsSafeZone { get; set; }
        }

        [SubSystemInit]
        public static void Init()
        {
            InitZoneLookup();
        }

        public static void Zoned(string zoneShortName)
        {
            // add our new zone to the zone lookup if necessary
            if (!ZoneLookup.TryGetValue(zoneShortName, out var _))
            {
                ZoneLookup.Add(zoneShortName, new Zone(zoneShortName));
            }

            ZoneLookup.TryGetValue(zoneShortName, out CurrentZone);
        }

        private static void InitZoneLookup()
        {
            // need to do this here because the event processors haven't been loaded yet
            var currentZone = MQ.Query<string>("${Zone.ShortName}");
            Zoned(currentZone);
        }
    }
}
