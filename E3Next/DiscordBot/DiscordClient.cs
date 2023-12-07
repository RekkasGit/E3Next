using E3Core.DiscordBot;
using E3Core.Settings;
using E3Core.Utility;
using MonoCore;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Windows.Forms;

namespace E3Core.Processors
{
    public static class DiscordClient
    {
        private static Int64 _nextDiscordMessagePoll = 0;
        private static Int64 _nextDiscordMessagePollInterval = 1000;
        private static RestClient _restClient;
        private static string _lastDiscordMessageIdFileName = "Last Discord Message Id.txt";
        private static string _e3ConfigFilePath;
        private static string _lastDiscordMessageIdFilePath;
        private static ulong _discordBotUserId;
        private static string _messageResource = $"channels/{E3.GeneralSettings.DiscordGuildChannelId}/messages";
        private static IMQ MQ = E3.MQ;
        private static Dictionary<ulong, string> _discordUserIdToNameMap = new Dictionary<ulong, string>();
        private static Dictionary<string, ulong> _discordNameToUserIdMap = new Dictionary<string, ulong>();
        private static bool _isInit;

        public static void Init()
        {
            try
            {
                EventProcessor.RegisterEvent("GuildChat", "(.+) tells the guild, '(.+)'", (x) =>
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

                        SendMessageToDiscord($"**{character} Guild**: {messageToSend}");
                    }
                });

                _e3ConfigFilePath = MQ.Query<string>("${MacroQuest.Path[config]}");
                _lastDiscordMessageIdFilePath = $"{_e3ConfigFilePath}\\e3 Macro Inis\\{_lastDiscordMessageIdFileName}";

                E3.Bots.Broadcast("newing up http client");
                var restOptions = new RestClientOptions("https://discord.com/api");
                _restClient = new RestClient(restOptions);
                _restClient.AddDefaultHeader("Authorization", $"Bot {E3.GeneralSettings.DiscordBotToken}");

                var myUserRequest = new RestRequest("users/@me");
                var myUser = _restClient.Get<DiscordUser>(myUserRequest);

                _discordBotUserId = myUser?.id ?? 0;

                var guildRequest = new RestRequest($"guilds/{E3.GeneralSettings.DiscordServerId}/members").AddParameter("limit", 1000);
                var guildMembers = _restClient.Get<GuildMember[]>(guildRequest);

                _discordUserIdToNameMap = guildMembers.ToDictionary(k => k.user.id, v => v.nick ?? v.user.global_name ?? v.user.username);
                _discordNameToUserIdMap = guildMembers.ToDictionary(k => k.nick ?? k.user.global_name ?? k.user.username, v => v.user.id);

                //SendMessageToDiscord("Connected");
                //SendMessageToGame("/gu Connected");

                _isInit = true;
            }
            catch (Exception e)
            {
                E3.Bots.Broadcast($"Caught exception {e.Message} in discord client init");
                var sb = new StringBuilder();
                sb.AppendLine(e.Message);
                sb.AppendLine(e.InnerException?.Message);
                sb.AppendLine(e.StackTrace);
                Clipboard.SetText(sb.ToString());
            }
        }

        [ClassInvoke(Data.Class.All)]
        public static void PollDiscord()
        {
            if (!_isInit) return;
            if (!e3util.ShouldCheck(ref _nextDiscordMessagePoll, _nextDiscordMessagePollInterval)) return;

            var request = new RestRequest(_messageResource);
            if (System.IO.File.Exists(_lastDiscordMessageIdFilePath))
            {
                var lastMessageId = System.IO.File.ReadAllText(_lastDiscordMessageIdFilePath);
                if (!string.IsNullOrEmpty(lastMessageId))
                {
                    request.AddParameter("after", lastMessageId);
                }
            }

            var messages = _restClient.Get<DiscordMessage[]>(request);
            foreach (var message in messages.OrderBy(o => o.timestamp))
            {
                if (message.author.id == _discordBotUserId)
                    continue;

                if (message.content == "!status")
                {
                    SendMessageToDiscord("Connected");
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
            MQ.Cmd(message);
        }

        private static void SendMessageToDiscord(string message)
        {
            var request = new RestRequest($"channels/{E3.GeneralSettings.DiscordGuildChannelId}/messages");
            request.AddStringBody(JsonSerializer.Serialize(new DiscordMessageRequest { content = message }), DataFormat.Json);
            _restClient.Post(request);
        }
    }
}
