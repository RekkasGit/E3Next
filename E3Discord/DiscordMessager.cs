using ApiLibrary.Models;
using E3NextUI.Server;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace E3Discord
{
    public static class DiscordMessager
    {
        private static string _lastDiscordMessageIdFileName = "Last Discord Message Id.txt";
        private static string _lastDiscordMessageIdFilePath;
        private static ulong _discordBotUserId;
        private static string _discordBotToken;
        private static Dictionary<ulong, string> _discordUserIdToNameMap = new Dictionary<ulong, string>();
        private static Dictionary<string, ulong> _discordNameToUserIdMap = new Dictionary<string, ulong>();
        private static DealerClient _tloClient;
        private static PubClient _pubClient;
        private static PubServer _pubServer;

        public static bool IsInit;

        public static void Init(string[] args) 
        { 
            try
            {
                // exe path, publisher port, router port, pub client port, discord bot token, discord guild chat channel id, discord server id
                WriteMessageToConsole($"Running init method with args {string.Join(",", args)}", ConsoleColor.Green);
                WriteMessageToConsole("Press any key to continue...", ConsoleColor.Green);
                Console.ReadKey();

                var pubClientPort = int.Parse(args[1]);
                var tloClientPort = int.Parse(args[2]);
                var pubServerPort = int.Parse(args[3]);
                _discordBotToken = args[4];
                var discordGuildChannelId = ulong.Parse(args[5]);
                var discordServerId = ulong.Parse(args[6]);

                _pubClient = new PubClient();
                _pubClient.Start(pubClientPort);

                _pubServer = new PubServer();
                _pubServer.Start(pubServerPort);

                _tloClient = new DealerClient(tloClientPort);
                var configFolder = _tloClient.RequestData("${MacroQuest.Path[config]}");

                ApiLibrary.ApiLibrary.DiscordBotToken = _discordBotToken;
                ApiLibrary.ApiLibrary.DiscordMessageResource = $"channels/{discordGuildChannelId}/messages";
                ApiLibrary.ApiLibrary.DiscordServerId = discordServerId;

                _lastDiscordMessageIdFilePath = $"{configFolder}\\e3 Macro Inis\\{_lastDiscordMessageIdFileName}";

                SetupDiscordBotUser();
                SetupDiscordUserMaps();

                ApiLibrary.ApiLibrary.SendMessageToDiscord("Connected :fire:");
                SendMessageToGame("/gu Connected");

                IsInit = true;
            }
            catch (Exception e)
            {
                WriteMessageToConsole($"Caught exception {e.Message} in discord client init", ConsoleColor.Red);
            }
        }

        public static void PollDiscord()
        {
            DiscordMessage lastMessage = null;
            try
            {
                string lastMessageId = string.Empty;
                if (System.IO.File.Exists(_lastDiscordMessageIdFilePath))
                {
                    lastMessageId = System.IO.File.ReadAllText(_lastDiscordMessageIdFilePath);
                }

                var messages = ApiLibrary.ApiLibrary.GetMessagesFromDiscord(lastMessageId);
                foreach (var message in messages.OrderBy(o => o.timestamp))
                {
                    lastMessage = messages.FirstOrDefault();

                    // ignore the bot so we don't echo its messages
                    if (message.author.id == _discordBotUserId)
                        continue;

                    WriteMessageToConsole($"Processing raw message \"{message.content}\" from discord", ConsoleColor.Yellow);
                    var preEmojiStripLength = message.content.Length;
                    var messageContent = Regex.Replace(message.content, @"\p{Cs}", "");
                    var postEmojiStripLength = messageContent.Length;
                    if (string.IsNullOrEmpty(messageContent))
                    {
                        WriteMessageToConsole("Skipped message because it only contained an emoji", ConsoleColor.Yellow);
                        continue;
                    }
                    else if (postEmojiStripLength < preEmojiStripLength)
                    {
                        WriteMessageToConsole("Stripped emoji out of discord message", ConsoleColor.Yellow);
                    }

                    if (string.Equals(messageContent, "!status", StringComparison.OrdinalIgnoreCase))
                    {
                        SendMessageToDiscord("Connected :fire:");
                    }
                    else if (string.Equals(messageContent, "!joke", StringComparison.OrdinalIgnoreCase))
                    {
                        var joke = ApiLibrary.ApiLibrary.GetAJoke();
                        SendMessageToDiscord(joke.joke);
                        SendMessageToGame($"/gu {joke.joke}");
                    }
                    else if (_discordUserIdToNameMap.TryGetValue(message.author.id, out var user))
                    {
                        if (messageContent.Contains("<@") && messageContent.Contains(">"))
                        {
                            var mentionStartIndex = messageContent.IndexOf("<@");
                            var mentionEndIndex = messageContent.IndexOf('>', mentionStartIndex);
                            var userId = messageContent.Substring(mentionStartIndex + 2, mentionEndIndex - mentionStartIndex - 2);
                            if (_discordUserIdToNameMap.TryGetValue(ulong.Parse(userId), out var userName))
                            {
                                var whatToReplace = messageContent.Substring(mentionStartIndex, mentionEndIndex - mentionStartIndex + 1);
                                messageContent = messageContent.Replace(whatToReplace, userName);
                            }
                        }

                        SendMessageToGame($"/gu {user} from discord: {messageContent}");
                    }
                }
            }
            catch (FormatException)
            {
                WriteMessageToConsole("Message could not be parsed", ConsoleColor.Yellow);
            }
            catch (Exception e)
            {
                WriteMessageToConsole($"Exception {e.Message} occurred in PollDiscord method: {e.StackTrace}", ConsoleColor.Red);
            }
            finally
            {
                if (lastMessage != null)
                    System.IO.File.WriteAllText(_lastDiscordMessageIdFilePath, lastMessage.id.ToString());
            }
        }

        public static void SendMessageToDiscord(string message)
        {
            try
            {
                ApiLibrary.ApiLibrary.SendMessageToDiscord(message);
            }
            catch (Exception e)
            {
                WriteMessageToConsole($"Failed to send message to discord because {e.Message}", ConsoleColor.Red);
            }
        }

        private static void SetupDiscordBotUser()
        {
            var myUser = ApiLibrary.ApiLibrary.GetDiscordBotUser();
            _discordBotUserId = myUser?.id ?? 0;
            WriteMessageToConsole($"Discord bot user id is {_discordBotUserId}", ConsoleColor.Green);
        }

        private static void SetupDiscordUserMaps()
        {
            var guildMembers = ApiLibrary.ApiLibrary.GetServerMembers();
            _discordUserIdToNameMap = guildMembers.ToDictionary(k => k.user.id, v => v.nick ?? v.user.global_name ?? v.user.username);
            _discordNameToUserIdMap = guildMembers.ToDictionary(k => k.nick ?? k.user.global_name ?? k.user.username, v => v.user.id);
        }

        public static void SendMessageToGame(string message)
        {
            WriteMessageToConsole($"Sending message: \"{message}\" to everquest", ConsoleColor.Blue);
            PubServer.PubCommands.Enqueue(message);
        }

        private static void WriteMessageToConsole(string message, ConsoleColor consoleColor)
        {
            Console.ForegroundColor = consoleColor;
            Console.WriteLine($"{DateTime.Now}: {message}");
        }
    }
}
