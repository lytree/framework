using Framework.ZLogging;
using Microsoft.Extensions.Logging;
using TUnit.Core;

namespace Framework.ZLogging.Tests.Fixtures;

/// <summary>
/// 在「写入 → 关闭 → 读取」这类测试中使用的辅助方法。
/// 读取日志文件前必须先释放 Provider，否则长连接的 FileStream 会独占文件句柄。
/// </summary>
static class LogTestHelper
{
    /// <summary>创建配置好的 Provider；默认关闭控制台输出以免污染测试输出。</summary>
    public static SpectreLogOptions Options(string filePath, bool append = false) => new()
    {
        FilePath = filePath,
        FileAppend = append,
        ConsoleOutput = false
    };

    /// <summary>执行日志动作，等待队列排空并释放 Provider，然后返回文件内容。</summary>
    public static async Task<string> WriteAndReadAsync(SpectreLogOptions options, Action<ILogger> log,
        string category = "Test")
    {
        var provider = new SpectreConsoleLoggerProvider(options);
        try
        {
            log(provider.CreateLogger(category));
        }
        finally
        {
            // 同步 Dispose 限时等待排空；这里显式 DisposeAsync 保证读到的是最终内容。
            await provider.DisposeAsync();
        }

        return await File.ReadAllTextAsync(options.FilePath!);
    }

    /// <summary>组合选项与日志动作，一次完成写入与读取。</summary>
    public static Task<string> CaptureAsync(string filePath, Action<ILogger> log,
        Action<SpectreLogOptions>? configure = null, bool append = false, string category = "Test")
    {
        var options = Options(filePath, append);
        configure?.Invoke(options);
        return WriteAndReadAsync(options, log, category);
    }
}
