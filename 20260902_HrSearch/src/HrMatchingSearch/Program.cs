// 自由文で人材を探す3つのやり方を、同じデータで比べるための実験プログラム。
//
//   (1) キーワード検索  … LLMがキーワードを作り、DBは LIKE の部分一致で探す
//   (2) ベクトル検索    … 埋め込みの距離で「意味の近さ」を測る
//   (3) RAG             … (1)(2)が集めた候補をAIに読ませて判断させる
//
// 詳しくはプロジェクトルートの README.md を参照。

using HrMatchingSearch;

Console.OutputEncoding = System.Text.Encoding.UTF8;

if (args is [] or ["--help"] or ["-h"])
{
    PrintUsage();
    return;
}

var env = SearchEnv.Create();

switch (args)
{
    case ["seed"]:
        await Database.SeedAsync(env);
        break;

    // --naive / --quote-only は、記事で説明した「失敗した版」のプロンプトに差し替える。
    // 中身は prompts/ を参照
    case ["keyword", var query]:
        await KeywordSearch.RunAsync(env, query);
        break;

    case ["keyword", var query, "--prompt", var variant]:
        await KeywordSearch.RunAsync(env, query, variant);
        break;

    case ["vector", var query]:
        await VectorSearch.RunAsync(env, query);
        break;

    case [var cmd and ("rag-vector" or "rag-hybrid" or "rag-all"), var query]:
        await RagJudge.RunAsync(env, query, ToSource(cmd));
        break;

    case [var cmd and ("rag-vector" or "rag-hybrid" or "rag-all"), var query, "--quote-only"]:
        await RagJudge.RunAsync(env, query, ToSource(cmd), quoteOnly: true);
        break;

    default:
        PrintUsage();
        break;
}

static RagJudge.Source ToSource(string command) => command switch
{
    "rag-vector" => RagJudge.Source.Vector,
    "rag-hybrid" => RagJudge.Source.Hybrid,
    _ => RagJudge.Source.All,
};

static void PrintUsage()
    => Console.WriteLine("""
        使い方:
          dotnet run -- seed                  DBを準備してサンプル人材10名を登録（何度実行してもよい）

          dotnet run -- keyword "検索文"      (1) LLMがキーワードを作り LIKE で探す
          dotnet run -- vector  "検索文"      (2) 埋め込みの距離で探す

          dotnet run -- rag-vector "検索文"   (3) (2)の候補だけをAIに読ませる（比較用）
          dotnet run -- rag-hybrid "検索文"   (3) (1)と(2)の候補を合わせてAIに読ませる ★本命
          dotnet run -- rag-all    "検索文"   (3) 検索せず全件をAIに読ませる（比較用）

        プロンプトの切り替え（記事で説明した「失敗した版」を再現する。詳しくは prompts/README.md）:
          dotnet run -- keyword "検索文" --naive        規則を書かない素の版。1文字キーワードが出る
          dotnet run -- rag-*   "検索文" --quote-only   reason に引用だけを求める版。否定の依頼で0件になる

        例:
          dotnet run -- keyword    "組み込みの経験がある人は？"
          dotnet run -- vector     "経験の浅いメンバーを指導できる人は？"
          dotnet run -- rag-hybrid "クラウドの経験がないと思われる人は？"
        """);
