using Discord.WebSocket;
using Discord;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MonoCore;
using E3Core.Server;
using Discord.Commands;
using System.Windows.Input;
using Discord.Webhook;
using NetMQ;
using System.Windows.Forms;

namespace E3Core.Processors
{
    public static class DiscordClient
    {
        public static DiscordSocketClient SocketClient { get; set; }
        private static DiscordWebhookClient WebhookClient { get; set; }
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

                        await SendMessageToDiscord($"**{character} Guild**: {message}");
                    }
                });

                var config = new DiscordSocketConfig
                {
                    GatewayIntents = GatewayIntents.AllUnprivileged | GatewayIntents.MessageContent
                };

                WebhookClient = new DiscordWebhookClient(E3.GeneralSettings.DiscordGuildChatChannelWebhookUrl);

                SocketClient = new DiscordSocketClient(config);
                SocketClient.Log += LogAsync;
                SocketClient.Ready += ReadyAsync;
                SocketClient.MessageReceived += MessageReceivedAsync;
                SocketClient.InteractionCreated += InteractionCreatedAsync;

                await SocketClient.LoginAsync(TokenType.Bot, E3.GeneralSettings.DiscordBotToken);
                await SocketClient.StartAsync();
                await Task.Delay(Timeout.Infinite);
            }
            catch (Exception ex)
            {
                E3.Bots.Broadcast(ex.Message);
                E3.Bots.Broadcast(ex.InnerException.Message);
                Clipboard.SetText(ex.InnerException.StackTrace);
            }
        }

        private static Task LogAsync(LogMessage log)
        {
            Console.WriteLine(log.ToString());
            return Task.CompletedTask;
        }

        // The Ready event indicates that the client has opened a
        // connection and it is now safe to access the cache.
        private static async Task ReadyAsync()
        {
            SendMessageToGame($"/gu Connected");
            await SendMessageToDiscord("Connected");
        }

        // This is not the recommended way to write a bot - consider
        // reading over the Commands Framework sample.
        private static Task MessageReceivedAsync(SocketMessage message)
        {
            // The bot should never respond to itself.
            if (message.Author.Id == SocketClient.CurrentUser.Id || message.Author.Username.Equals("Guild Chat"))
                return Task.CompletedTask;

            if (message.Author is SocketGuildUser guildUser)
                SendMessageToGame($"/gu {guildUser.DisplayName} from discord: {message.Content}");

            return Task.CompletedTask;
        }

        private static void SendMessageToGame(string message)
        {
            PubClient._pubCommands.Enqueue(message);
        }

        // For better functionality & a more developer-friendly approach to handling any kind of interaction, refer to:
        // https://discordnet.dev/guides/int_framework/intro.html
        private static async Task InteractionCreatedAsync(SocketInteraction interaction)
        {
            // safety-casting is the best way to prevent something being cast from being null.
            // If this check does not pass, it could not be cast to said type.
            if (interaction is SocketMessageComponent component)
            {
                // Check for the ID created in the button mentioned above.
                if (component.Data.CustomId == "unique-id")
                    await interaction.RespondAsync("Thank you for clicking my button!");

                else
                    Console.WriteLine("An ID has been received that has no handler!");
            }
        }

        public static async Task SendMessageToDiscord(string message)
        {
            await WebhookClient.SendMessageAsync(message);
        }
    }
}
