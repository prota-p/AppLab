using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using OpenAI.Chat;

namespace HrMatchingSearch;

/// <summary>
/// 記事① キーワード検索。
/// LLMに検索文をキーワードへ翻訳させ、DBは LIKE の部分一致だけを担当する。
/// 意味の解釈はLLM、絞り込みの論理はSQL、という分担。
/// </summary>
static class KeywordSearch
{
    /// <summary>LLMが返すキーワード。exclude は「含んでいたら除く」語。</summary>
    public record Keywords(List<string> Include, List<string> Exclude);

    public static async Task RunAsync(SearchEnv env, string query, string? variant = null)
    {
        Console.WriteLine($"検索文: {query}");
        if (variant is not null) Console.WriteLine($"プロンプト: keyword_{variant}.txt");

        var keywords = await GenerateKeywordsAsync(env, query, variant);
        var results = await SearchByKeywordsAsync(env, keywords);
        PrintResults(keywords, results);
    }

    /// <summary>
    /// 検索文からキーワードを作らせる。規則はどれも実測で必要になったもの。
    /// 素朴に頼むと結果が毎回変わるので、ひとつずつ潰していった結果がこの形。
    /// プロンプトの本文は prompts/keyword.txt。naive=true で規則のない素の版を使う。
    /// </summary>
    public static async Task<Keywords> GenerateKeywordsAsync(SearchEnv env, string query, string? variant = null)
    {
        var completion = await env.Chat.CompleteChatAsync(
            [
                new SystemChatMessage(Prompts.Keyword(variant)),
                new UserChatMessage(query),
            ],
            new ChatCompletionOptions { ResponseFormat = ChatResponseFormat.CreateJsonObjectFormat() });

        using var doc = JsonDocument.Parse(completion.Value.Content[0].Text.Trim());
        return new Keywords(ReadStrings(doc.RootElement, "include"), ReadStrings(doc.RootElement, "exclude"));
    }

    public static async Task<List<Candidate>> SearchByKeywordsAsync(SearchEnv env, Keywords keywords)
    {
        var (include, exclude) = (keywords.Include, keywords.Exclude);

        await using var db = env.OpenDb();

        // include が空なら全員が対象。「〜でない人」を探す依頼がこの形になる
        return await db.People
            .Where(p => include.Count == 0 || include.Any(kw => p.Bio.Contains(kw)))
            .Where(p => !exclude.Any(kw => p.Bio.Contains(kw)))
            .Select(p => new Candidate(p.Name, p.Bio))
            .ToListAsync();
    }

    static void PrintResults(Keywords keywords, List<Candidate> results)
    {
        PrintKeywords(keywords);

        Console.WriteLine($"\n--- キーワード検索結果: {results.Count}件 ---");
        if (results.Count == 0) Console.WriteLine("該当なし");
        foreach (var result in results) Console.WriteLine($"・{result.Name}: {result.Bio}");
    }

    public static void PrintKeywords(Keywords keywords)
    {
        Console.WriteLine($"含めるキーワード: {(keywords.Include.Count > 0 ? string.Join(", ", keywords.Include) : "（指定なし）")}");
        if (keywords.Exclude.Count > 0)
            Console.WriteLine($"除くキーワード: {string.Join(", ", keywords.Exclude)}");
    }

    static List<string> ReadStrings(JsonElement root, string name)
        => root.TryGetProperty(name, out var array) && array.ValueKind == JsonValueKind.Array
            ? [.. array.EnumerateArray()
                    .Select(e => e.GetString())
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .Select(s => s!)]
            : [];
}
