using ApiLibrary.Models;

using RestSharp;

using System;
using System.Net.Http;
using System.Text.Json;

namespace ApiLibrary
{
    public static class ApiLibrary
    {
        private static HttpClient _httpClient = new HttpClient();
        private static string _baseDiscordUrl = "https://discord.com/api";
        private static string _baseJokeUrl = "https://icanhazdadjoke.com";
        private static string _baseFactUrl = "https://uselessfacts.jsph.pl/api/v2/facts/random";
        public static string DiscordBotToken;
        public static string DiscordGuildChannelMessageResource;
        public static string DiscordServerId;

        public static DiscordUser GetDiscordBotUser()
        {
            var restClient = GetRestClient(_baseDiscordUrl);
            var myUserRequest = new RestRequest("users/@me");
            return restClient.Get<DiscordUser>(myUserRequest);
        }

        public static GuildMember[] GetServerMembers()
        {
            var restClient = GetRestClient(_baseDiscordUrl);
            var guildRequest = new RestRequest($"guilds/{DiscordServerId}/members").AddParameter("limit", 1000);
            return restClient.Get<GuildMember[]>(guildRequest);
        }

        public static DiscordMessage[] GetMessagesFromDiscord(string lastMessageId)
        {
            var request = new RestRequest(DiscordGuildChannelMessageResource);
            if (!string.IsNullOrEmpty(lastMessageId))
            {
                request.AddParameter("after", lastMessageId);
            }

            var restClient = GetRestClient(_baseDiscordUrl);
            return restClient.Get<DiscordMessage[]>(request);
        }

        public static void SendMessageToDiscord(string message, string channelId = null)
        {
            WriteMessageToConsole($"Sending message: \"{message}\" to discord", ConsoleColor.Yellow);
            var restClient = GetRestClient(_baseDiscordUrl);
            var channelResource = string.IsNullOrEmpty(channelId) ? DiscordGuildChannelMessageResource : $"channels/{channelId}/messages";
            var request = new RestRequest(channelResource);
            request.AddStringBody(JsonSerializer.Serialize(new { content = message }), DataFormat.Json);
            restClient.Post(request);
        }

        public static DiscordDmChannel GetDmChannel(string discordUserId)
        {
            var restClient = GetRestClient(_baseDiscordUrl);
            var request = new RestRequest("users/@me/channels");
            var body = JsonSerializer.Serialize(new { recipient_id = discordUserId });
            request.AddStringBody(body, DataFormat.Json);
            return restClient.Post<DiscordDmChannel>(request);
        }

        public static JokeResponse GetAJoke()
        {
            var restOptions = new RestClientOptions(_baseJokeUrl);
            var client = new RestClient(_httpClient, restOptions);
            var jokeRequest = new RestRequest();
            jokeRequest.AddHeader("Accept", "application/json");
            return client.Get<JokeResponse>(jokeRequest);
        }

        public static FactResponse GetAFact()
        {
            var restOptions = new RestClientOptions(_baseFactUrl);
            var client = new RestClient(_httpClient, restOptions);
            var jokeRequest = new RestRequest();
            jokeRequest.AddHeader("Accept", "application/json");
            return client.Get<FactResponse>(jokeRequest);
        }

        private static RestClient GetRestClient(string url)
        {
            var restOptions = new RestClientOptions(url);
            var restClient = new RestClient(_httpClient, restOptions);
            restClient.AddDefaultHeader("Authorization", $"Bot {DiscordBotToken}");

            return restClient;
        }

        private static void WriteMessageToConsole(string message, ConsoleColor consoleColor)
        {
            Console.ForegroundColor = consoleColor;
            Console.WriteLine($"{DateTime.Now}: {message}");
        }
    }
}
