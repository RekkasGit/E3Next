using System;

namespace ApiLibrary.Models
{
    public class GuildMember
    {
        public DiscordUser user { get; set; }
        public string nick { get; set; }
        public string avatar { get; set; }
        public ulong[] roles { get; set; }
        public DateTime joined_at { get; set; }
        public DateTime? premium_since { get; set; }
        public bool deaf { get; set; }
        public bool mute { get; set; }
        public int flags { get; set; }
        public bool pending { get; set; }
        public string permissions { get; set; }
        public DateTime? communication_disabled_until { get; set; }
    }
}
