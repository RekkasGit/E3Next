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

        public static async Task<DiscordUser> GetDiscordBotUser()
        {
            var restClient = GetRestClient(_baseDiscordUrl);
            var myUserRequest = new RestRequest("users/@me");
            return await restClient.GetAsync<DiscordUser>(myUserRequest);
        }

        public static async Task<GuildMember[]> GetServerMembers()
        {
            var restClient = GetRestClient(_baseDiscordUrl);
            var guildRequest = new RestRequest($"guilds/{DiscordServerId}/members").AddParameter("limit", 1000);
            return await restClient.GetAsync<GuildMember[]>(guildRequest);
        }

        public static async Task<DiscordMessage[]> GetMessagesFromDiscord(string lastMessageId)
        {
            var request = new RestRequest(DiscordMessageResource);
            if (!string.IsNullOrEmpty(lastMessageId))
            {
                request.AddParameter("after", lastMessageId);
            }

            var restClient = GetRestClient(_baseDiscordUrl);
            return await restClient.GetAsync<DiscordMessage[]>(request);
        }

        public static async Task SendMessageToDiscord(string message)
        {
            var restClient = GetRestClient(_baseDiscordUrl);
            var request = new RestRequest(DiscordMessageResource);
            request.AddStringBody(JsonSerializer.Serialize(new DiscordMessageRequest { content = message }), DataFormat.Json);
            await restClient.PostAsync(request);
        }

        public static async Task<JokeResponse> GetAJoke()
        {
            var restOptions = new RestClientOptions(_baseJokeUrl);
            var client = new RestClient(_httpClient, restOptions);
            var jokeRequest = new RestRequest();
            jokeRequest.AddHeader("Accept", "application/json");
            return await client.GetAsync<JokeResponse>(jokeRequest);
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
