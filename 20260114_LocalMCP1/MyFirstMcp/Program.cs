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
public static class CurrentTimeTool
{
    // 現在時刻を返すツール
    // Description属性でツールの説明を指定可能
    [McpServerTool, Description("現在の時刻を返します。")]
    public static string GetCurrentTime()
    {
        return DateTime.Now.ToString("yyyy年MM月dd日 HH:mm:ss");
    }
}