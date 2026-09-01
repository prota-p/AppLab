using Microsoft.Data.SqlTypes;
using Microsoft.EntityFrameworkCore;

namespace HrMatchingSearch;

sealed class Person
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string Bio { get; set; }

    /// <summary>紹介文を埋め込みモデルにかけたベクトル。SQL Server 2025 の vector 型に入る。</summary>
    public SqlVector<float> Embedding { get; set; }
}

sealed class HrContext(string connectionString) : DbContext
{
    public DbSet<Person> People => Set<Person>();

    protected override void OnConfiguring(DbContextOptionsBuilder options)
        => options.UseSqlServer(connectionString);

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var person = modelBuilder.Entity<Person>();
        person.ToTable("People");
        person.Property(p => p.Name).HasMaxLength(100);

        // 1536 は text-embedding-3-small の次元数。埋め込みモデルを変えたらここも変える
        person.Property(p => p.Embedding).HasColumnType("vector(1536)");
    }
}

static class Database
{
    public static async Task SeedAsync(SearchEnv env)
    {
        await using var db = env.OpenDb();

        // 初回はDBとテーブルも作る。2回目以降は何もしない
        if (await db.Database.EnsureCreatedAsync())
            Console.WriteLine("データベースとテーブル People を作成しました。");

        // 何度実行しても同じ状態になるよう、既存データを消してから入れ直す
        var deleted = await db.People.ExecuteDeleteAsync();
        if (deleted > 0) Console.WriteLine($"既存の{deleted}件を削除しました。");

        foreach (var (name, bio) in SampleData.People)
        {
            db.People.Add(new Person
            {
                Name = name,
                Bio = bio,
                Embedding = await VectorSearch.CreateEmbeddingAsync(env, bio),
            });
            Console.WriteLine($"登録: {name}");
        }

        await db.SaveChangesAsync();
        Console.WriteLine($"{SampleData.People.Length}件 登録完了。");
    }
}

static class SampleData
{
    /// <summary>
    /// 書き手がばらばらな実データを模した紹介文。
    /// 表記のゆれ（サーバ／サーバー、組込み／組み込み）は意図的に残してある。
    /// きれいに揃えると検索の難しさが再現できなくなるので、直さないこと。
    /// </summary>
    public static readonly (string Name, string Bio)[] People =
    [
        ("佐藤 健一", "10年以上C#でバックエンド開発を担当。ASP.NET CoreとAzureを用いた大規模Webサービスの設計・運用経験が豊富。"),
        ("鈴木 美咲", "フロントエンドエンジニア。ReactとTypeScriptが得意で、デザインシステムの構築やアクセシビリティ改善の実績あり。"),

        // 「指導できる人」を探しても字面が一つも当たらない。意味でしか届かない
        ("松本 涼", "バックエンド担当。社内の勉強会を毎週開いていて、入ったばかりのメンバーのコードレビューを引き受けることが多い。"),

        ("高橋 大輔", "データサイエンティスト。Pythonによる機械学習モデルの開発、需要予測や異常検知プロジェクトをリード。"),
        ("伊藤 翔太", "インフラエンジニア。KubernetesとTerraformによるクラウド基盤構築が専門。SREとして可用性改善に従事。"),

        // サーバー / サーバ ── 末尾の長音がゆれる組
        ("井上 拓海", "オンプレのサーバー運用が長い。監視とバックアップ設計、深夜の障害対応まで一通りやってきた。"),
        ("岡田 里奈", "社内システムのサーバ移行を担当。オンプレからAzureへ、止められない業務を止めずに運んだ。"),

        // 組込み / 組み込み ── 語の途中の送り仮名がゆれる組
        ("加藤 修平", "組込みエンジニア。C/C++での車載ソフトウェア開発が専門。リアルタイムOSと通信プロトコルに精通。"),
        ("村上 早紀", "組み込み機器のユーザーインターフェース設計。小さい画面と少ないボタンで迷わせない工夫を考えてきた。"),

        ("中村 亮", "セキュリティエンジニア。脆弱性診断とペネトレーションテストを担当。CISSP保持。"),
    ];
}
