using System.Text.Json.Serialization;

namespace DifyBlazorApp.Services
{
    public class DifyApiService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private const string ApiUrl = "https://api.dify.ai/v1/chat-messages";

        public DifyApiService(HttpClient httpClient)
        {
            _httpClient = httpClient;
            
            _apiKey = Environment.GetEnvironmentVariable("DIFY_API_KEY")
                ?? throw new InvalidOperationException("環境変数 DIFY_API_KEY が設定されていません");

            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");
        }

        public async Task<DifyResponse?> SendMessageAsync(
            string query,
            string? conversationId,
            string userId)
        {
            var request = new DifyRequest
            {
                Query = query,
                ConversationId = conversationId,
                User = userId
            };

            var response = await _httpClient.PostAsJsonAsync(ApiUrl, request);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadFromJsonAsync<DifyResponse>();
        }
    }

    // リクエスト用データ定義
    public record DifyRequest
    {
        [JsonPropertyName("inputs")]
        public object Inputs { get; init; } = new { };

        [JsonPropertyName("query")]
        public required string Query { get; init; }

        [JsonPropertyName("response_mode")]
        public string ResponseMode { get; init; } = "blocking";

        [JsonPropertyName("conversation_id")]
        public string? ConversationId { get; init; }

        [JsonPropertyName("user")]
        public required string User { get; init; }
    }

    // レスポンス用データ定義
    public record DifyResponse
    {
        [JsonPropertyName("conversation_id")]
        public string? ConversationId { get; init; }

        [JsonPropertyName("answer")]
        public string? Answer { get; init; }
    }
}
