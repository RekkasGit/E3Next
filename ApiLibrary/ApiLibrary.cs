using ApiLibrary.Models;
using RestSharp;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace ApiLibrary
{
    public static class ApiLibrary
    {
        private static HttpClient _httpClient = new HttpClient();
        private static string _baseDiscordUrl = "https://discord.com/api";
        private static string _baseJokeUrl = "https://icanhazdadjoke.com";
        public static string DiscordBotToken;
        public static string DiscordMessageResource;
        public static ulong DiscordServerId;

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
            var request = new RestRequest(DiscordMessageResource);
            if (!string.IsNullOrEmpty(lastMessageId))
            {
                request.AddParameter("after", lastMessageId);
            }

            var restClient = GetRestClient(_baseDiscordUrl);
            return restClient.Get<DiscordMessage[]>(request);
        }

        public static void SendMessageToDiscord(string message)
        {
            var restClient = GetRestClient(_baseDiscordUrl);
            var request = new RestRequest(DiscordMessageResource);
            request.AddStringBody(JsonSerializer.Serialize(new DiscordMessageRequest { content = message }), DataFormat.Json);
            restClient.Post(request);
        }

        public static JokeResponse GetAJoke()
        {
            var restOptions = new RestClientOptions(_baseJokeUrl);
            var client = new RestClient(_httpClient, restOptions);
            var jokeRequest = new RestRequest();
            jokeRequest.AddHeader("Accept", "application/json");
            return client.Get<JokeResponse>(jokeRequest);
        }

        private static RestClient GetRestClient(string url)
        {
            var restOptions = new RestClientOptions(url);
            var restClient = new RestClient(_httpClient, restOptions);
            restClient.AddDefaultHeader("Authorization", $"Bot {DiscordBotToken}");

            return restClient;
        }
    }
}
