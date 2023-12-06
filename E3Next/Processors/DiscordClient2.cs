using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MonoCore;
using E3Core.Server;
using System.Windows.Input;
using NetMQ;
using System.Windows.Forms;
using E3Core.Settings;
using E3Core.Utility;
using static System.Net.WebRequestMethods;
using System.Net.Http;
using Newtonsoft.Json;
using System.Security.Policy;
using System.Collections.Specialized;
using System.Net.NetworkInformation;
using System.ComponentModel;
using Newtonsoft.Json.Serialization;

namespace E3Core.Processors
{
    public static class DiscordClient2
    {
        private static Int64 _nextDiscordMessagePoll = 0;
        private static Int64 _nextDiscordMessagePollInterval = 1000;
        private static HttpClient _httpClient;
        private static string _lastDiscordMessageIdFileName = "Last Discord Message Id.txt";
        private static string _e3ConfigFilePath;
        private static string _lastDiscordMessageIdFilePath;
        private static ulong _discordBotUserId;
        private static string _baseDiscordUrl = "https://discord.com/api";
        private static IMQ MQ = E3.MQ;
        private static Dictionary<ulong, string> _discordUserIdToNameMap = new Dictionary<ulong, string>();

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

        public class DiscordUser
        {
            public ulong id { get; set; }
            public string username { get; set; }
            public string discriminator { get; set; }
            public string global_name { get; set; }
            public string avatar { get; set; }
            public bool bot { get; set; }
            public bool system { get ; set; }
            public bool mfa_enabled { get; set; }
            public string banner { get; set; }
            public int? accent_color { get; set; }
            public string locale { get; set; }
            public bool verified { get; set; }
            public string email {  get; set; }
            public int flags { get; set; }
            public int premium_type { get; set; }
            public int public_flags { get; set; }
            public string avatar_decoration { get; set; }
        }

        public class GuildMember
        {
            public DiscordUser user { get; set; }
            public string nick { get; set; }
            public string avatar { get; set; }
            public ulong[] roles { get; set; }
            public DateTime joined_at { get; set; }
            public DateTime? premium_since { get; set; }
            public bool deaf { get; set ; }
            public bool mute { get; set; }
            public int flags { get; set; }
            public bool pending { get; set; }
            public string permissions { get; set; }
            public DateTime? communication_disabled_until { get; set; }
        }

        public class DiscordMessageRequest
        {
            public string content { get; set; }
        }

        public async static void Init()
        {
            //try
            //{
            EventProcessor.RegisterEvent("GuildChat", "(.+) tells the guild, '(.+)'", async (x) =>
            {
                if (x.match.Groups.Count == 3)
                {
                    var character = x.match.Groups[1].Value;
                    var message = x.match.Groups[2].Value;

                    await SendMessageToDiscord($"**{character} Guild**: {message}");
                }
            });

            //EventProcessor.RegisterEvent("SayChat", "(.+) says, '(.+)'", async (x) =>
            //{
            //    if (x.match.Groups.Count == 3)
            //    {
            //        var character = x.match.Groups[1].Value;
            //        var message = x.match.Groups[2].Value;

            //        await SendMessageToDiscord($"**{character} Guild**: {message}");
            //    }
            //});

            //    var config = new DiscordSocketConfig
            //    {
            //        GatewayIntents = GatewayIntents.AllUnprivileged | GatewayIntents.MessageContent
            //    };

            //    WebhookClient = new DiscordWebhookClient(E3.GeneralSettings.DiscordGuildChatChannelWebhookUrl);

            //    SocketClient = new DiscordSocketClient(config);
            //    SocketClient.Log += LogAsync;
            //    SocketClient.Ready += ReadyAsync;
            //    SocketClient.MessageReceived += MessageReceivedAsync;
            //    SocketClient.InteractionCreated += InteractionCreatedAsync;

            //    await SocketClient.LoginAsync(TokenType.Bot, E3.GeneralSettings.DiscordBotToken);
            //    await SocketClient.StartAsync();
            //    await Task.Delay(Timeout.Infinite);
            //}
            //catch (Exception ex)
            //{
            //    E3.Bots.Broadcast(ex.Message);
            //    E3.Bots.Broadcast(ex.InnerException.Message);
            //    Clipboard.SetText(ex.InnerException.StackTrace);
            //}

            _e3ConfigFilePath = MQ.Query<string>("${MacroQuest.Path[config]}");
            _lastDiscordMessageIdFilePath = $"{_e3ConfigFilePath}\\e3 Macro Inis\\{_lastDiscordMessageIdFileName}";
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bot", $"{E3.GeneralSettings.DiscordBotToken}");

            var userUrl = $"{_baseDiscordUrl}/users/@me";
            var userResponse = await SendHttpRequest(userUrl);
            var myUser = JsonConvert.DeserializeObject<DiscordUser>(userResponse);

            var guildUrl = $"{_baseDiscordUrl}/guilds/{E3.GeneralSettings.DiscordServerId}/members?limit=1000";
            var guildResponse = await SendHttpRequest(guildUrl);
            var guildMembers = JsonConvert.DeserializeObject<GuildMember[]>(guildResponse);
            _discordUserIdToNameMap = guildMembers.ToDictionary(k => k.user.id, v => v.nick ?? v.user.global_name ?? v.user.username);
            _discordBotUserId = myUser?.id ?? 0;
        }

        [ClassInvoke(Data.Class.All)]
        public static async void PollDiscord()
        {
            if (!e3util.ShouldCheck(ref _nextDiscordMessagePoll, _nextDiscordMessagePollInterval)) return;
            var url = $"{_baseDiscordUrl}/channels/{E3.GeneralSettings.DiscordGuildChannelId}/messages";
            if (System.IO.File.Exists(_lastDiscordMessageIdFilePath))
            {
                var lastMessageId = System.IO.File.ReadAllText(_lastDiscordMessageIdFilePath);
                if (!string.IsNullOrEmpty(lastMessageId))
                {
                    url += $"?after={lastMessageId}";
                }
            }
            var content = await SendHttpRequest(url);
            var messages = JsonConvert.DeserializeObject<DiscordMessage[]>(content);
            foreach (var message in messages.OrderBy(o => o.timestamp))
            {
                if (message.author.id == _discordBotUserId)
                    continue;

                if (_discordUserIdToNameMap.TryGetValue(message.author.id, out var user))
                    SendMessageToGame($"/gu {user} from discord: {message.content}");
            }

            var lastMessage = messages.FirstOrDefault();
            if (lastMessage != null) 
            {
                System.IO.File.WriteAllText(_lastDiscordMessageIdFilePath, lastMessage.id.ToString());
            }
        }

        private static async Task<string> SendHttpRequest(string url)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }

        private static async Task<string> PostHttpRequest(string url, string content)
        {
            var httpContent = new StringContent(content, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(url, httpContent);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }

        //private static Task LogAsync(LogMessage log)
        //{
        //    Console.WriteLine(log.ToString());
        //    return Task.CompletedTask;
        //}

        // The Ready event indicates that the client has opened a
        // connection and it is now safe to access the cache.
        private static async Task ReadyAsync()
        {
            SendMessageToGame($"/gu Connected");
            await SendMessageToDiscord("Connected");
        }

        // This is not the recommended way to write a bot - consider
        // reading over the Commands Framework sample.
        //private static Task MessageReceivedAsync(SocketMessage message)
        //{
        //    // The bot should never respond to itself.
        //    if (message.Author.Id == SocketClient.CurrentUser.Id || message.Author.Username.Equals("Guild Chat"))
        //        return Task.CompletedTask;

        //    if (message.Author is SocketGuildUser guildUser)
        //        SendMessageToGame($"/gu {guildUser.DisplayName} from discord: {message.Content}");

        //    return Task.CompletedTask;
        //}

        private static void SendMessageToGame(string message)
        {
            PubClient._pubCommands.Enqueue(message);
        }

        //// For better functionality & a more developer-friendly approach to handling any kind of interaction, refer to:
        //// https://discordnet.dev/guides/int_framework/intro.html
        //private static async Task InteractionCreatedAsync(SocketInteraction interaction)
        //{
        //    // safety-casting is the best way to prevent something being cast from being null.
        //    // If this check does not pass, it could not be cast to said type.
        //    if (interaction is SocketMessageComponent component)
        //    {
        //        // Check for the ID created in the button mentioned above.
        //        if (component.Data.CustomId == "unique-id")
        //            await interaction.RespondAsync("Thank you for clicking my button!");

        //        else
        //            Console.WriteLine("An ID has been received that has no handler!");
        //    }
        //}

        public static async Task SendMessageToDiscord(string message)
        {
            var url = $"{_baseDiscordUrl}/channels/{E3.GeneralSettings.DiscordGuildChannelId}/messages";
            var requestObject = new DiscordMessageRequest { content = message };
            await PostHttpRequest(url, JsonConvert.SerializeObject(requestObject));
        }
    }
}
