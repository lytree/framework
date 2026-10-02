using TUnit.Core;

namespace Framework.ZLogging.Tests.Fixtures;

/// <summary>
/// 为每个测试类提供一个独立的临时目录，测试结束后自动清理。
/// 由 TUnit 通过 <see cref="ClassDataSourceAttribute{T}"/> 注入。
/// </summary>
public sealed class TempDirectory : IAsyncDisposable
{
    public string Path { get; }

    public TempDirectory()
    {
        Path = System.IO.Path.Combine(
            System.IO.Path.GetTempPath(),
            "framework-zlogging-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path);
    }

    public string NewFile(string extension = ".log")
        => System.IO.Path.Combine(Path, Guid.NewGuid().ToString("N") + extension);

    public async ValueTask DisposeAsync()
    {
        if (string.IsNullOrEmpty(Path) || !Directory.Exists(Path)) return;

        for (var attempt = 0; attempt < 5; attempt++)
        {
            try
            {
                Directory.Delete(Path, recursive: true);
                return;
            }
            catch (IOException) when (attempt < 4)
            {
                // 文件可能仍被进程占用（异步写循环未退出），短暂退避后重试
                await Task.Delay(50 * (attempt + 1)).ConfigureAwait(false);
            }
            catch (UnauthorizedAccessException) when (attempt < 4)
            {
                await Task.Delay(50 * (attempt + 1)).ConfigureAwait(false);
            }
        }
    }
}
