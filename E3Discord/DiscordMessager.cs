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
        private static Dictionary<string, Action> _commands = new Dictionary<string, Action>(StringComparer.OrdinalIgnoreCase)
        {
            { "!commands", ShowCommands},
            { "!status", SendConnectedMessage},
            { "!joke", TellAJoke },
            { "!fact", GetAFact }
        };

        public static bool IsInit;
        public static string DiscordDmChannel;
        public static string DiscordOocChannelId;

        public static void Init(string[] args) 
        { 
            try
            {
				// exe path, publisher port, router port, pub client port, discord bot token, discord guild chat channel id, discord server id, discord ooc channel id
				WriteMessageToConsole($"Running init method with args {string.Join(",", args)}", ConsoleColor.Green);
                //WriteMessageToConsole("Press any key to continue...", ConsoleColor.Green);
                //Console.ReadKey();

                var pubClientPort = int.Parse(args[1]);
                var tloClientPort = int.Parse(args[2]);
                var pubServerPort = int.Parse(args[3]);
                _discordBotToken = args[4];
                var discordGuildChannelId = args[5];
                var discordServerId = args[6];
                var discordOocChannelId = args.Length > 7 ? args[7] : discordGuildChannelId; // fallback to guild channel if not provided
                string myDiscordUserId = string.Empty;
                if (args.Length > 8)
                     myDiscordUserId = args[8];

                _pubClient = new PubClient();
                _pubClient.Start(pubClientPort);

                _pubServer = new PubServer();
                _pubServer.Start(pubServerPort);

                _tloClient = new DealerClient(tloClientPort);
                var configFolder = _tloClient.RequestData("${MacroQuest.Path[config]}");

                DiscordOocChannelId = discordOocChannelId;
                
                ApiLibrary.ApiLibrary.DiscordBotToken = _discordBotToken;
                ApiLibrary.ApiLibrary.DiscordGuildChannelMessageResource = $"channels/{discordGuildChannelId}/messages";
                ApiLibrary.ApiLibrary.DiscordServerId = discordServerId;

                _lastDiscordMessageIdFilePath = $"{configFolder}\\e3 Macro Inis\\{_lastDiscordMessageIdFileName}";

                SetupDiscordBotUser();
                SetupDiscordUserMaps();
                if (!string.IsNullOrEmpty(myDiscordUserId))
                    SetupDiscordDmChannel(myDiscordUserId);

                // Send OOC channel ID to PubClient via command queue
                PubServer.PubCommands.Enqueue($"#SetOOCChannelId|{DiscordOocChannelId}");
                
                SendMessageToDiscord("Connected :fire:");
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
            //just a simple query to test if the bot's everquest client is logged in
            _tloClient.RequestData("${MacroQuest.Path[config]}");
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

                    // handle commands
                    if (messageContent.StartsWith("!"))
                    {
                        if (_commands.TryGetValue(messageContent, out var action))
                        {
                            action();
                        }
                        else
                        {
                            SendMessageToDiscord($"I'm sorry, but **{messageContent}** is not a valid command - valid commands are {string.Join(", ", _commands.Keys)}");
                        }
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

                        SendMessageToGame($"/gu {user}: {messageContent}");
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

        public static void TellAJoke()
        {
            var joke = ApiLibrary.ApiLibrary.GetAJoke();
            SendMessageToDiscord(joke.joke);
            SendMessageToGame($"/gu {joke.joke}");
        }

        public static void GetAFact()
        {
            var fact = ApiLibrary.ApiLibrary.GetAFact();
            SendMessageToDiscord(fact.text);
            SendMessageToGame($"/gu {fact.text}");
        }

        public static void SendConnectedMessage()
        {
            SendMessageToDiscord("Connected :fire:");
        }

        public static void ShowCommands()
        {
            SendMessageToDiscord($"The following commands are supported: {string.Join(", ", _commands.Keys)}");
        }

        public static void SendMessageToDiscord(string message, string channelId = null)
        {
            try
            {
                ApiLibrary.ApiLibrary.SendMessageToDiscord(message, channelId);
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

        private static void SetupDiscordDmChannel(string myDiscordUserId)
        {
            var dmChannel = ApiLibrary.ApiLibrary.GetDmChannel(myDiscordUserId);
            DiscordDmChannel = (dmChannel?.id ?? 0).ToString();
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
