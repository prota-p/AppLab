using System.Net.Http.Json;
using System.Text.Json.Serialization;

// 環境変数からAPIキーを取得（未設定時は例外）
var apiKey = Environment.GetEnvironmentVariable("DIFY_API_KEY")
    ?? throw new InvalidOperationException("環境変数 DIFY_API_KEY が設定されていません");

const string apiUrl = "https://api.dify.ai/v1/chat-messages";

using var httpClient = new HttpClient();
httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

// 会話IDを保持して文脈を継続
string? conversationId = null;

while (true)
{
    Console.Write("\nあなた: ");
    var input = Console.ReadLine();
    if (string.IsNullOrWhiteSpace(input)) break;

    var request = new DifyRequest
    {
        Query = input,
        ConversationId = conversationId,
        User = "console-user"
    };

    // JSON自動変換でリクエスト送信・レスポンス受信
    var response = await httpClient.PostAsJsonAsync(apiUrl, request);
    var result = await response.Content.ReadFromJsonAsync<DifyResponse>();

    conversationId = result?.ConversationId;
    Console.WriteLine($"\nAI: {result?.Answer}");
}

// リクエスト用データ定義
record DifyRequest
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
record DifyResponse
{
    [JsonPropertyName("conversation_id")]
    public string? ConversationId { get; init; }

    [JsonPropertyName("answer")]
    public string? Answer { get; init; }
}