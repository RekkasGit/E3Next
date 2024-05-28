using E3Core.Processors;
using MonoCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace E3Core.Data
{
    public class Zone
    {
        public static IMQ MQ = E3.MQ;
        private static HashSet<string> _safeZones = new HashSet<string> { "poknowledge", "potranquility", "nexus", "guildhall", "freeporttemple", "arena", "bazaar","pohealth", "guildhalllrg_in" };

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

		public override string ToString()
		{
			return $"Name:{Name} ShortName:{ShortName} Id: {Id} IsSafeZone:{IsSafeZone}";
		}
	}
}
