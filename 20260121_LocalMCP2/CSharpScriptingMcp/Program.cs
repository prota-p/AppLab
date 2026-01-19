using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using System.ComponentModel;

// アプリケーションのホストビルダーを作成
var builder = Host.CreateApplicationBuilder(args);

// ログをコンソールに出力する設定
builder.Logging.AddConsole(consoleLogOptions =>
{
    // すべてのログレベルを標準エラー出力に出力
    // (【重要】通常の標準出力はMCPプロトコルの通信に使用されるため)
    consoleLogOptions.LogToStandardErrorThreshold = LogLevel.Trace;
});

// MCPサーバーの設定
builder.Services
    .AddMcpServer()              // MCPサーバをDIコンテナに登録
    .WithStdioServerTransport()  // 標準入出力でクライアントと通信
    .WithToolsFromAssembly();    // アセンブリ内のツールを自動登録

// アプリケーションを起動
await builder.Build().RunAsync();

// MCPサーバーのツールとして登録するクラス
[McpServerToolType]
public static class CSharpScriptTool
{
    // C#スクリプトを実行するツール
    [McpServerTool, Description("C#スクリプトを実行します。スクリプトは最後が式で終わる必要があります（その式の評価結果が返されます）。")]
    public static async Task<string> ExecuteCSharpScript(
        [Description("実行するC#コード。最後は評価したい式で終わること。")] string code)
    {
        try
        {
            var result = await CSharpScript.EvaluateAsync<object>(code);
            return result?.ToString() ?? "(null)";
        }
        catch (Exception ex)
        {
            return $"エラー: {ex.Message}";
        }
    }
}