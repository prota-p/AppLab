# プロンプト

システムプロンプトはコードから切り離してここに置いています。**規則を減らした版も残してあり、引数で切り替えて挙動の違いを確かめられます。**

| ファイル | 使い方 | 内容 |
|---|---|---|
| [keyword.txt](keyword.txt) | `keyword "検索文"` | キーワード生成の完成版。表記ゆれ・一般語・英数字1文字の規則入り |
| [keyword_naive.txt](keyword_naive.txt) | `keyword "検索文" --prompt naive` | **素の版。** 規則を何も書かず、ただキーワードを作らせるだけ |
| [keyword_shortest.txt](keyword_shortest.txt) | `keyword "検索文" --prompt shortest` | 「揺れる語は最短の部分文字列に」だけを足した中間版 |
| [rag.txt](rag.txt) | `rag-hybrid "検索文"` | 候補者の判定の完成版 |
| [rag_quote_only.txt](rag_quote_only.txt) | `rag-hybrid "検索文" --quote-only` | `reason` に引用だけを求め、「ないこと」の扱いを書かない版 |

## 規則を減らした版を残している理由

規則はどれも実測で必要になったものです。**外して実行してみると、その規則が何を防いでいるのかが分かります。**

```bash
# 素の版は「組み込み」しか出さないため、「組込み」と書いてある加藤を取りこぼす
dotnet run --project src/HrMatchingSearch -- keyword "組み込みの経験がある人は？" --prompt naive
dotnet run --project src/HrMatchingSearch -- keyword "組み込みの経験がある人は？"

diff prompts/keyword_naive.txt prompts/keyword.txt
```

## 結果は毎回同じではありません

LLMの出力なので、同じプロンプトでも生成される語は実行ごとに変わります。**ここに書いた挙動も、モデルやバージョンが変われば再現しなくなります。**

実際、2026-08 時点の `gpt-5.6-luna` では、以前に観測した次の2つが再現しなくなっています。

- 素の版が `C` のような英数字1文字を出し、`LIKE '%C%'` が C# / React / CISSP に当たる
- `rag_quote_only.txt` が「クラウドの経験がない人」を1人も返せなくなる

規則自体（英数字1文字を出さない、「ないこと」は引用できないと明示する）は、
文字列照合の性質から見て今も妥当です。ただし**そのモデルで実際にどう振る舞うかは、自分で測って確かめてください。**
