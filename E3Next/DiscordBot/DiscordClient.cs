using E3Core.DiscordBot;
using E3Core.Server;
using E3Core.Settings;
using E3Core.Utility;
using MonoCore;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace E3Core.Processors
{
    public static class DiscordClient
    {
        private static Int64 _nextDiscordMessagePoll = 0;
        private static Int64 _nextDiscordMessagePollInterval = 1000;
        private static HttpClient _httpClient;
        private static string _lastDiscordMessageIdFileName = "Last Discord Message Id.txt";
        private static string _e3ConfigFilePath;
        private static string _lastDiscordMessageIdFilePath;
        private static ulong _discordBotUserId;
        private static string _baseDiscordUrl = "https://discord.com/api";
        private static string _messageUrl;
        private static IMQ MQ = E3.MQ;
        private static Dictionary<ulong, string> _discordUserIdToNameMap = new Dictionary<ulong, string>();
        private static Dictionary<string, ulong> _discordNameToUserIdMap = new Dictionary<string, ulong>();
        private static bool _isInit;

        public async static void Init()
        {
            try
            {
                EventProcessor.RegisterEvent("GuildChat", "(.+) tells the guild, '(.+)'", async (x) =>
                {
                    if (x.match.Groups.Count == 3)
                    {
                        var character = x.match.Groups[1].Value;
                        var message = x.match.Groups[2].Value;
                        var messageToSend = message;

                        var atIndex = message.IndexOf("@");
                        if (atIndex > -1)
                        {
                            string atName;
                            var spaceIndex = message.IndexOf(" ", atIndex);
                            if (spaceIndex < atIndex)
                            {
                                atName = message.Substring(atIndex + 1);
                            }
                            else
                            {
                                atName = message.Substring(atIndex + 1, message.IndexOf(" "));
                            }

                            if (_discordNameToUserIdMap.TryGetValue(atName, out var discordUserId))
                            {
                                messageToSend = message.Replace($"@{atName}", $"<@{discordUserId}>");
                            }
                        }

                        await SendMessageToDiscord($"**{character} Guild**: {messageToSend}");
                    }
                });

                _e3ConfigFilePath = MQ.Query<string>("${MacroQuest.Path[config]}");
                _lastDiscordMessageIdFilePath = $"{_e3ConfigFilePath}\\e3 Macro Inis\\{_lastDiscordMessageIdFileName}";

                E3.Bots.Broadcast("newing up http client");
                _httpClient = new HttpClient();
                E3.Bots.Broadcast("setting http client headers");
                _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bot", $"{E3.GeneralSettings.DiscordBotToken}");
                _messageUrl = $"{_baseDiscordUrl}/channels/{E3.GeneralSettings.DiscordGuildChannelId}/messages";

                var userUrl = $"{_baseDiscordUrl}/users/@me";
                var userResponse = await SendHttpRequest(userUrl);
                var myUser = JsonConvert.DeserializeObject<DiscordUser>(userResponse);

                _discordBotUserId = myUser?.id ?? 0;

                var guildUrl = $"{_baseDiscordUrl}/guilds/{E3.GeneralSettings.DiscordServerId}/members?limit=1000";
                var guildResponse = await SendHttpRequest(guildUrl);
                var guildMembers = JsonConvert.DeserializeObject<GuildMember[]>(guildResponse);

                _discordUserIdToNameMap = guildMembers.ToDictionary(k => k.user.id, v => v.nick ?? v.user.global_name ?? v.user.username);
                _discordNameToUserIdMap = guildMembers.ToDictionary(k => k.nick ?? k.user.global_name ?? k.user.username, v => v.user.id);

                await SendMessageToDiscord("Connected");
                SendMessageToGame("/gu Connected");

                _isInit = true;
            }
            catch (Exception e)
            {
                E3.Bots.Broadcast($"Caught exception {e.Message} in discord client init");
                var sb = new StringBuilder();
                sb.AppendLine(e.Message);
                sb.AppendLine(e.InnerException.Message);
                sb.AppendLine(e.StackTrace);
                Clipboard.SetText(sb.ToString());
            }
        }

        [ClassInvoke(Data.Class.All)]
        public static async void PollDiscord()
        {
            if (!_isInit) return;
            if (!e3util.ShouldCheck(ref _nextDiscordMessagePoll, _nextDiscordMessagePollInterval)) return;

            var messageUrl = _messageUrl;
            if (System.IO.File.Exists(_lastDiscordMessageIdFilePath))
            {
                var lastMessageId = System.IO.File.ReadAllText(_lastDiscordMessageIdFilePath);
                if (!string.IsNullOrEmpty(lastMessageId))
                {
                    messageUrl += $"?after={lastMessageId}";
                }
            }

            var content = await SendHttpRequest(messageUrl);
            var messages = JsonConvert.DeserializeObject<DiscordMessage[]>(content);
            foreach (var message in messages.OrderBy(o => o.timestamp))
            {
                if (message.author.id == _discordBotUserId)
                    continue;

                if (message.content == "!status")
                {
                    await SendMessageToDiscord("Connected");
                }
                else if (_discordUserIdToNameMap.TryGetValue(message.author.id, out var user))
                {
                    SendMessageToGame($"/gu {user} from discord: {message.content}");
                }
            }

            var lastMessage = messages.FirstOrDefault();
            if (lastMessage != null) 
            {
                System.IO.File.WriteAllText(_lastDiscordMessageIdFilePath, lastMessage.id.ToString());
            }
        }

        private static void SendMessageToGame(string message)
        {
            PubClient._pubCommands.Enqueue(message);
        }

        private static async Task SendMessageToDiscord(string message)
        {
            await PostHttpRequest(_messageUrl, JsonConvert.SerializeObject(new DiscordMessageRequest { content = message }));
        }

        private static async Task<string> SendHttpRequest(string url)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            var response = await _httpClient.SendAsync(request);
            return await ValidateAndReadResponse(response);
        }

        private static async Task<string> PostHttpRequest(string url, string content)
        {
            var httpContent = new StringContent(content, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(url, httpContent);
            return await ValidateAndReadResponse(response);
        }

        private static async Task<string> ValidateAndReadResponse(HttpResponseMessage response)
        {
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }
    }
}
