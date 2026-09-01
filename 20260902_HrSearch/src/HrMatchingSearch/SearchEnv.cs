using Azure;
using Azure.AI.OpenAI;
using Microsoft.Extensions.Configuration;
using OpenAI.Chat;
using OpenAI.Embeddings;

namespace HrMatchingSearch;

/// <summary>3つの検索方式が共通で使うもの（DB接続とAzure OpenAIのクライアント）。</summary>
sealed class SearchEnv
{
    public required string ConnectionString { get; init; }
    public required ChatClient Chat { get; init; }
    public required EmbeddingClient Embedding { get; init; }

    /// <summary>これ以上離れていたら「該当なし」とみなす距離。</summary>
    /// <remarks>
    /// このサンプルデータで具合よく動くよう調整した値。普遍的な定数ではない。
    /// データや埋め込みモデルを変えたら必ず測り直すこと。
    /// </remarks>
    public const double DistanceThreshold = 0.60;

    /// <summary>ベクトル検索で何件まで候補に上げるか。</summary>
    public const int TopN = 5;

    public static SearchEnv Create()
    {
        var config = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json")
            .AddUserSecrets<SearchEnv>() // Endpoint と ApiKey は user-secrets から上書きする
            .Build();

        var aoai = new AzureOpenAIClient(
            ReadRequiredUri(config, "AzureOpenAI:Endpoint"),
            new AzureKeyCredential(ReadRequired(config, "AzureOpenAI:ApiKey")));

        return new SearchEnv
        {
            ConnectionString = ReadRequired(config, "ConnectionStrings:Default"),
            Chat = aoai.GetChatClient(ReadRequired(config, "AzureOpenAI:ChatDeployment")),
            Embedding = aoai.GetEmbeddingClient(ReadRequired(config, "AzureOpenAI:EmbeddingDeployment")),
        };
    }

    public HrContext OpenDb() => new(ConnectionString);

    private static string ReadRequired(IConfiguration config, string key)
        => config[key] is { Length: > 0 } value
            ? value
            : throw new InvalidOperationException($"設定 '{key}' がありません。appsettings.json または user-secrets を確認してください。");

    private static Uri ReadRequiredUri(IConfiguration config, string key)
        => Uri.TryCreate(ReadRequired(config, key), UriKind.Absolute, out var value)
            ? value
            : throw new InvalidOperationException($"設定 '{key}' は有効な絶対 URL ではありません。");
}

/// <summary>検索で見つかった人。</summary>
record Candidate(string Name, string Bio);

/// <summary>ベクトル検索で見つかった人。距離が小さいほど検索文に近い。</summary>
record ScoredCandidate(string Name, string Bio, double Distance)
{
    public bool IsHit => Distance < SearchEnv.DistanceThreshold;
    public Candidate ToCandidate() => new(Name, Bio);
}
