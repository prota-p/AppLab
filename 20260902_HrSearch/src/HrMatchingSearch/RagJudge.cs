using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using OpenAI.Chat;

namespace HrMatchingSearch;

/// <summary>
/// 記事③ 取ってきた候補をAIに読ませて判断させる（RAG）。
/// 検索は候補を集めるだけで、合否の判断と理由づけはAIが担う。
/// </summary>
static class RagJudge
{
    /// <summary>候補の集め方。答えの良し悪しはここでほぼ決まる。</summary>
    public enum Source
    {
        /// <summary>ベクトル検索の上位だけ。キーワードでしか届かない人を取りこぼす</summary>
        Vector,

        /// <summary>ベクトル検索 ＋ キーワード検索。互いの穴を埋め合う（本命）</summary>
        Hybrid,

        /// <summary>検索せず全件。件数が増えると破綻するので比較用</summary>
        All,
    }

    public record Match(string Name, string Reason);
    public record Judgement(List<Match> Matches, string Note);

    public static async Task RunAsync(SearchEnv env, string query, Source source, bool quoteOnly = false)
    {
        Console.WriteLine($"検索文: {query}");
        if (quoteOnly) Console.WriteLine("プロンプト: reason に引用だけを求める版");

        var candidates = await CollectCandidatesAsync(env, query, source);
        Console.WriteLine($"\nAIに渡す候補: {candidates.Count}件");

        var judgement = await JudgeCandidatesAsync(env, query, candidates, quoteOnly);
        PrintResults(judgement, candidates);
    }

    /// <summary>指定された集め方で候補を作る。</summary>
    static async Task<List<Candidate>> CollectCandidatesAsync(
        SearchEnv env,
        string query,
        Source source)
        => source switch
        {
            Source.Hybrid => await CollectHybridCandidatesAsync(env, query),
            Source.Vector => await CollectVectorCandidatesAsync(env, query),
            Source.All => await LoadAllCandidatesAsync(env),
            _ => throw new ArgumentOutOfRangeException(nameof(source)),
        };

    /// <summary>キーワード検索とベクトル検索を合わせて候補を作る（本命）。</summary>
    static async Task<List<Candidate>> CollectHybridCandidatesAsync(SearchEnv env, string query)
    {
        var candidates = await CollectVectorCandidatesAsync(env, query);

        var keywords = await KeywordSearch.GenerateKeywordsAsync(env, query);
        Console.WriteLine();
        KeywordSearch.PrintKeywords(keywords);

        var byKeyword = await KeywordSearch.SearchByKeywordsAsync(env, keywords);
        var added = byKeyword.Where(k => !candidates.Any(c => c.Name == k.Name)).ToList();

        Console.WriteLine($"\n--- キーワード検索が足した候補（{added.Count}件） ---");
        foreach (var a in added) Console.WriteLine($"＋ {a.Name}");

        candidates.AddRange(added);
        return candidates;
    }

    /// <summary>ベクトル検索の上位候補を作る。しきい値で切らず、選別はAIに任せる。</summary>
    static async Task<List<Candidate>> CollectVectorCandidatesAsync(SearchEnv env, string query)
    {
        var queryVector = await VectorSearch.CreateEmbeddingAsync(env, query);
        var byVector = await VectorSearch.SearchByVectorAsync(env, queryVector);

        Console.WriteLine($"\n--- ベクトル検索が挙げた候補（{byVector.Count}件） ---");
        foreach (var candidate in byVector)
            Console.WriteLine($"[{candidate.Distance:F4}] {candidate.Name}");

        return [.. byVector.Select(candidate => candidate.ToCandidate())];
    }

    /// <summary>検索せず、DBにいる全員を候補にする（比較用）。</summary>
    static async Task<List<Candidate>> LoadAllCandidatesAsync(SearchEnv env)
    {
        await using var db = env.OpenDb();
        var candidates = await db.People
            .Select(person => new Candidate(person.Name, person.Bio))
            .ToListAsync();

        Console.WriteLine($"\n--- 全件をそのまま候補にする（{candidates.Count}件） ---");
        return candidates;
    }

    /// <summary>
    /// 候補の紹介文を読ませて、該当者と理由を返させる。
    /// 散文で返させると氏名が文章に埋もれて画面やCSVに使えないため、JSONで構造化させる。
    /// </summary>
    public static async Task<Judgement> JudgeCandidatesAsync(
        SearchEnv env,
        string query,
        List<Candidate> candidates,
        bool quoteOnly = false)
    {
        var roster = string.Join("\n", candidates.Select(c => $"- {c.Name}: {c.Bio}"));

        var completion = await env.Chat.CompleteChatAsync(
            [
                new SystemChatMessage(Prompts.Rag(quoteOnly)),
                new UserChatMessage($"依頼: {query}\n\n候補者:\n{roster}"),
            ],
            new ChatCompletionOptions { ResponseFormat = ChatResponseFormat.CreateJsonObjectFormat() });

        return ParseJudgement(completion.Value.Content[0].Text.Trim());
    }

    static Judgement ParseJudgement(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var matches = new List<Match>();
        if (root.TryGetProperty("matches", out var array) && array.ValueKind == JsonValueKind.Array)
            matches.AddRange(array.EnumerateArray().Select(m => new Match(
                m.TryGetProperty("name", out var n) ? n.GetString() ?? "" : "",
                m.TryGetProperty("reason", out var r) ? r.GetString() ?? "" : "")));

        var note = root.TryGetProperty("note", out var nt) ? nt.GetString() ?? "" : "";
        return new Judgement(matches, note);
    }

    static void PrintResults(Judgement judgement, List<Candidate> candidates)
    {
        Console.WriteLine($"\n--- AIの判断: {judgement.Matches.Count}名 ---");
        if (judgement.Matches.Count == 0) Console.WriteLine("該当者なし");

        foreach (var (name, reason) in judgement.Matches)
        {
            // 候補にない氏名が返っていないか必ず確かめる（作り話の検出）
            var isKnown = candidates.Any(c => c.Name == name);
            Console.WriteLine($"{(isKnown ? "・" : "⚠ 候補外:")} {name}");
            Console.WriteLine($"    {reason}");
        }

        if (!string.IsNullOrWhiteSpace(judgement.Note))
            Console.WriteLine($"\n補足: {judgement.Note}");
    }
}
