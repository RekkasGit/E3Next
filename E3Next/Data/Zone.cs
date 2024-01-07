using E3Core.Processors;

using MonoCore;

using System;
using System.Collections.Generic;

namespace E3Core.Data
{
    public class Zone
    {
        public static IMQ MQ = E3.MQ;
        private static HashSet<string> _safeZones = new HashSet<string> { "poknowledge", "potranquility", "nexus", "guildhall", "freeporttemple", "arena", "bazaar" };

        public Zone(Int32 zoneId)
        {
            ShortName = MQ.Query<string>($"${{Zone[{zoneId}].ShortName}}");
            Name = MQ.Query<string>($"${{Zone[{zoneId}].Name}}");
            Id = zoneId;
            IsSafeZone = _safeZones.Contains(ShortName);
        }

        public string Name { get; set; }
        public string ShortName { get; set; }
        public int Id { get; set; }
        public bool IsSafeZone { get; set; }
    }
}
