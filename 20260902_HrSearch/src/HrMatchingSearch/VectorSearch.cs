using Microsoft.Data.SqlTypes;
using Microsoft.EntityFrameworkCore;
using OpenAI.Embeddings;

namespace HrMatchingSearch;

/// <summary>
/// 記事② ベクトル検索。
/// 検索文も紹介文も埋め込みモデルでベクトルに変え、その距離の近さで探す。
/// 距離の計算は VECTOR_DISTANCE として SQL Server 側で実行される。
/// </summary>
static class VectorSearch
{
    public static async Task RunAsync(SearchEnv env, string query)
    {
        Console.WriteLine($"検索文: {query}");

        var queryVector = await CreateEmbeddingAsync(env, query);
        var results = await SearchByVectorAsync(env, queryVector);
        PrintResults(results);
    }

    /// <summary>文章を埋め込みベクトルに変える。登録時と検索時の両方で使う。</summary>
    public static async Task<SqlVector<float>> CreateEmbeddingAsync(SearchEnv env, string text)
    {
        OpenAIEmbedding embedding = await env.Embedding.GenerateEmbeddingAsync(text);
        return new SqlVector<float>(embedding.ToFloats());
    }

    /// <summary>検索文に近い順に上位 TopN 件を返す。しきい値では絞らない。</summary>
    public static async Task<List<ScoredCandidate>> SearchByVectorAsync(
        SearchEnv env,
        SqlVector<float> queryVector)
    {
        await using var db = env.OpenDb();

        // 並べ替えまでをDBにやらせるため、射影は匿名型で受ける。
        // ここで record を直接作ると EF Core が OrderBy を翻訳できない
        var rows = await db.People
            .Select(p => new
            {
                p.Name,
                p.Bio,
                // VECTOR_DISTANCE('cosine', ...) に翻訳され、DB側で計算される
                Distance = EF.Functions.VectorDistance("cosine", p.Embedding, queryVector),
            })
            .OrderBy(r => r.Distance)
            .Take(SearchEnv.TopN)
            .ToListAsync();

        return [.. rows.Select(r => new ScoredCandidate(r.Name, r.Bio, r.Distance))];
    }

    static void PrintResults(List<ScoredCandidate> results)
    {
        Console.WriteLine($"\n--- 距離が小さい順（しきい値 {SearchEnv.DistanceThreshold:F2}） ---");
        foreach (var result in results)
            Console.WriteLine($"{(result.IsHit ? "○" : "×")} [{result.Distance:F4}] {result.Name}: {result.Bio}");

        var hits = results.Count(result => result.IsHit);
        Console.WriteLine($"\nヒット: {hits}件");
        if (hits == 0) Console.WriteLine("該当なし");
    }
}
