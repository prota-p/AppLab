# HrMatchingSearch

自由文で人材を探す3つのやり方を、同じデータで比べる実験プログラム。
連載「AI人材検索×C#」（全3回）のサンプルコードです。

| # | 方式 | やること | 対応ファイル |
|---|---|---|---|
| 1 | **キーワード検索** | LLMが検索文からキーワードを作り、DBは `LIKE` の部分一致で探す | [KeywordSearch.cs](src/HrMatchingSearch/KeywordSearch.cs) |
| 2 | **ベクトル検索** | 埋め込みの距離で「意味の近さ」を測る。距離計算はDB側 | [VectorSearch.cs](src/HrMatchingSearch/VectorSearch.cs) |
| 3 | **RAG** | 1と2が集めた候補をAIに読ませ、該当者と理由を返させる | [RagJudge.cs](src/HrMatchingSearch/RagJudge.cs) |

記事の回とファイルが1対1に対応しています。共通部分は [SearchEnv.cs](src/HrMatchingSearch/SearchEnv.cs)、
エンティティとサンプルデータは [Data.cs](src/HrMatchingSearch/Data.cs)、コマンドの振り分けは [Program.cs](src/HrMatchingSearch/Program.cs) です。

```
HrMatchingSearch/
├── src/HrMatchingSearch/  # 実行プロジェクト
├── infra/                 # Azure リソースの定義
├── HrMatchingSearch.slnx  # Visual Studio / dotnet 用ソリューション
└── README.md
```

## 前提

- **SQL Server 2025 以上**（`vector` 型が必要）
  - **LocalDB / Express Edition でも動く**（確認: 17.0.4025.3 RTM-CU3 Express）。有償エディションは不要
- .NET 10 SDK
- Azure サブスクリプション

## Azure リソースの作成

チャットモデルと埋め込みモデルの2つが必要です。[infra/main.bicep](infra/main.bicep) がリソースグループごと一括で作ります。

```powershell
az deployment sub create `
  --name hrsearch-infra `
  --location eastus2 `
  --template-file infra/main.bicep
```

| リソース | 既定値 |
|---|---|
| リソースグループ | `rg-foundry-hrsearch-dev-eus2` |
| Azure AI Foundry アカウント | `aif-hrsearch-dev-eus2-<一意文字列>` |
| チャットモデル | `gpt-5.6-luna`（デプロイ名 `gpt-56-luna`） |
| 埋め込みモデル | `text-embedding-3-small`（1536次元） |

チャットモデルはキーワードを作らせるだけの軽い用途なので、安いもので十分です。
設定値はデプロイの出力から取れます。

```powershell
az deployment sub show --name hrsearch-infra --query properties.outputs
```

## 準備

1. [appsettings.json](src/HrMatchingSearch/appsettings.json) の `ConnectionStrings:Default` を確認（既定は LocalDB）
2. **エンドポイントとAPIキーは user-secrets に入れる**。appsettings.json は空のままにする

```powershell
$out = az deployment sub show --name hrsearch-infra --query properties.outputs | ConvertFrom-Json
$endpoint = $out.endpoint.value
$account  = $out.accountName.value
$rg       = $out.resourceGroupName.value
$key = az cognitiveservices account keys list -g $rg -n $account --query key1 -o tsv

dotnet user-secrets set --project src/HrMatchingSearch "AzureOpenAI:Endpoint" $endpoint
dotnet user-secrets set --project src/HrMatchingSearch "AzureOpenAI:ApiKey" $key
```

登録内容は `dotnet user-secrets list --project src/HrMatchingSearch` で確認できます。

3. データベースとサンプルデータを用意

```powershell
dotnet run --project src/HrMatchingSearch -- seed    # DB・テーブル作成＋サンプル人材10名の登録
```

初回の `seed` でDBとテーブルも作成します。各紹介文の埋め込みベクトルを計算してから登録し、
2回目以降は既存データをすべて入れ直すので、何度実行しても10件に揃います。

## 使い方

```powershell
dotnet run --project src/HrMatchingSearch -- keyword    "検索文"   # (1) キーワード検索
dotnet run --project src/HrMatchingSearch -- vector     "検索文"   # (2) ベクトル検索
dotnet run --project src/HrMatchingSearch -- rag-hybrid "検索文"   # (3) (1)と(2)の候補をAIに読ませる ★本命
dotnet run --project src/HrMatchingSearch -- rag-vector "検索文"   # (3) (2)の候補だけをAIに読ませる（比較用）
dotnet run --project src/HrMatchingSearch -- rag-all    "検索文"   # (3) 検索せず全件をAIに読ませる（比較用）
```

## 試すとよい4つの依頼

人事や現場から実際に来そうな依頼です。方式ごとに得意・不得意がはっきり出ます。

| 依頼 | 正解 | (1)キーワード | (2)ベクトル | (3)RAG |
|---|---|---|---|---|
| `組み込みの経験がある人は？` | 加藤・村上 | ◎ | ◎ | ◎ |
| `Salesforceの導入案件が来た。経験者はいる？` | 該当者なし | ◎ | ◎ | ◎ |
| `経験の浅いメンバーを指導できる人は？` | 松本 | **✕** | ◎ | ◎ |
| `クラウドの経験がないと思われる人は？` | 7名 | ◎ | **✕** | ◎ |

- **「指導できる人」** は松本の紹介文に指導・育成の語が一つもないため、字面（＝書かれている文字そのもの）を照合する(1)では届きません
- **「クラウドの経験がない」** は「〜がない」という否定を意味の距離で表せないため、(2)では逆にクラウド専門家が上位に来ます
- (3) は両方の候補を受け取るので、どちらの穴も埋まります。`rag-vector` と `rag-hybrid` を見比べると差がはっきりします

## 実測ログ

記事で述べている結果は [measurements/](measurements/) に、コマンドと出力をそのまま残しています。
日付ごとのファイルです。**LLMの出力は毎回同じではないので、手元で実行した結果とは異なる可能性があります。**

## 注意

- appsettings.json にエンドポイントとAPIキーを書かないこと（user-secrets を使う）。リポジトリには空のままコミットしてある
- サンプルデータの表記ゆれ（サーバ／サーバー、組込み／組み込み）は**意図的**です。揃えると検索の難しさが再現できなくなります
- ベクトル検索のしきい値 `SearchEnv.DistanceThreshold`（0.60）は、このデータで具合よく動くよう調整した値です。**普遍的な定数ではないので、データを変えたら測り直してください**
- 埋め込みは `vector(1536)` 列（text-embedding-3-small の次元数）。別のモデルを使う場合は [Data.cs](src/HrMatchingSearch/Data.cs) の次元数も変更すること
- LLMの出力なので、生成されるキーワードは実行のたびに多少変わります
