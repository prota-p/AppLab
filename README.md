# AppLab

ブログ「プロ太のC#学習部屋」のカテゴリ **[C#実践アプリ開発ラボ](https://prota-p.com/category/app-lab/)** で扱ったサンプルコードです。

C#で新しい技術（AI連携、新しいプロトコル、新しいDB機能など）を小さく動かして、仕組みと勘所を掴むための実験室です。
連載ごとにフォルダを分けています。フォルダ名は `日付_トピック名` の形式で、日付は動画の投稿日です。

| フォルダ | 連載 | 内容 |
|---|---|---|
| [20251112_DifyCSharp2](20251112_DifyCSharp2/) | [Dify×C#連携②](https://prota-p.com/applab_dify2/) | Dify APIをC#から呼ぶ。`HttpClient` のコンソール版と Blazor 版の2つ |
| [20260114_LocalMCP1](20260114_LocalMCP1/) | [C#×MCP入門①](https://prota-p.com/applab_localmcp1/) | Claude Desktop から自作ツールを呼び出す、最小構成のMCPサーバ |
| [20260121_LocalMCP2](20260121_LocalMCP2/) | [C#×MCP入門②](https://prota-p.com/applab_localmcp2/) | Roslyn でC#コードを動的実行させるMCPサーバ |
| [20260902_HrSearch](20260902_HrSearch/) | AI人材検索×C#（全3回） | 自由文で人材を探す3つのやり方（キーワード検索 / ベクトル検索 / RAG）を、同じデータ・同じ依頼で比べる |

READMEのあるフォルダは、そこに前提・セットアップ・実行方法があります。

## 前提

連載ごとに必要なものが違います。詳細は各フォルダのREADME、または記事本文を参照してください。共通するのは次の2点です。

- .NET SDK（バージョンは連載ごとに異なります）
- APIキーなどの秘密情報は **user-secrets または環境変数** に置く。`appsettings.json` には書かない

## ライセンス

MIT License（[LICENSE](LICENSE)）
