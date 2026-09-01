namespace HrMatchingSearch;

/// <summary>
/// システムプロンプトを prompts/ から読む。
/// 記事で説明した「失敗した版」も置いてあり、コマンドの引数で切り替えられる。
/// 詳しくは prompts/README.md を参照。
/// </summary>
static class Prompts
{
    static readonly string Dir = Path.Combine(AppContext.BaseDirectory, "prompts");

    /// <summary>キーワード生成。variant を指定すると keyword_&lt;variant&gt;.txt を使う。</summary>
    public static string Keyword(string? variant) => Load(variant is null ? "keyword" : "keyword_" + variant);

    /// <summary>候補者の判定。quoteOnly=true で reason に引用だけを求める版。</summary>
    public static string Rag(bool quoteOnly) => Load(quoteOnly ? "rag_quote_only" : "rag");

    static string Load(string name)
    {
        var path = Path.Combine(Dir, name + ".txt");
        if (!File.Exists(path))
            throw new FileNotFoundException($"プロンプトが見つかりません: {path}");
        return File.ReadAllText(path).TrimEnd();
    }
}
