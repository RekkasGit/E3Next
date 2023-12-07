using System;

namespace E3Core.DiscordBot
{
    public class DiscordMessage
    {
        public ulong id { get; set; }
        public int type { get; set; }
        public string content { get; set; }
        public ulong channel_id { get; set; }
        public DiscordUser author { get; set; }
        public object[] attachments { get; set; }
        public object[] embeds { get; set; }
        public object[] mentions { get; set; }
        public object[] mention_roles { get; set; }
        public bool pinned { get; set; }
        public bool mention_everyone { get; set; }
        public bool tts { get; set; }
        public DateTime timestamp { get; set; }
        public DateTime? edited_timestamp { get; set; }
        public int flags { get; set; }
        public object[] components { get; set; }
        public ulong webhook_id { get; set; }
    }
}
