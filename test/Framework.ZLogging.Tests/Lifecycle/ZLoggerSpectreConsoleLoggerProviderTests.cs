using Framework.ZLogging;
using Microsoft.Extensions.Logging;
using TUnit.Core;

namespace Framework.ZLogging.Tests.Lifecycle;

public class ZLoggerSpectreConsoleLoggerProviderTests
{
    [Test]
    public async Task CreateLogger_ReturnsNonNullLogger()
    {
        using var provider = new ZLoggerSpectreConsoleLoggerProvider(new ZLoggerSpectreConsoleOptions());

        var logger = provider.CreateLogger("TestCategory");

        await Assert.That(logger).IsNotNull();
        await Assert.That(logger).IsTypeOf<ZLogger.ZLoggerLogger>();
    }

    [Test]
    public async Task Dispose_IsIdempotent()
    {
        var provider = new ZLoggerSpectreConsoleLoggerProvider(new ZLoggerSpectreConsoleOptions());

        await Task.Run(() =>
        {
            provider.Dispose();
            provider.Dispose();
        });
    }

    [Test]
    public async Task DisposeAsync_IsIdempotent()
    {
        var provider = new ZLoggerSpectreConsoleLoggerProvider(new ZLoggerSpectreConsoleOptions());

        await provider.DisposeAsync();
        await provider.DisposeAsync();
    }

    [Test]
    public async Task DisposeAsync_AfterSyncDispose_DoesNotThrow()
    {
        var provider = new ZLoggerSpectreConsoleLoggerProvider(new ZLoggerSpectreConsoleOptions());

        await Task.Run(provider.Dispose);
        await provider.DisposeAsync();
    }

    [Test]
    public async Task Dispose_FlushesPendingLogs()
    {
        var errors = new List<Exception>();
        var options = new ZLoggerSpectreConsoleOptions
        {
            InternalErrorLogger = errors.Add,
            FilePath = Path.Combine(Path.GetTempPath(), "flush-" + Guid.NewGuid().ToString("N") + ".log"),
            FileAppend = false
        };

        var provider = new ZLoggerSpectreConsoleLoggerProvider(options);
        var logger = provider.CreateLogger("FlushTest");

        for (var i = 0; i < 10; i++)
        {
            logger.LogInformation("entry");
        }

        await Task.Run(provider.Dispose);

        await Assert.That(errors).IsEmpty();
        await Assert.That(File.Exists(options.FilePath)).IsTrue();
        TryDelete(options.FilePath);
    }

    [Test]
    public async Task DisposeAsync_FlushesPendingLogsAsync()
    {
        var errors = new List<Exception>();
        var options = new ZLoggerSpectreConsoleOptions
        {
            InternalErrorLogger = errors.Add,
            FilePath = Path.Combine(Path.GetTempPath(), "async-flush-" + Guid.NewGuid().ToString("N") + ".log"),
            FileAppend = false
        };

        var provider = new ZLoggerSpectreConsoleLoggerProvider(options);
        var logger = provider.CreateLogger("AsyncFlushTest");

        for (var i = 0; i < 10; i++)
        {
            logger.LogInformation("entry");
        }

        await provider.DisposeAsync();

        await Assert.That(errors).IsEmpty();
        await Assert.That(File.Exists(options.FilePath)).IsTrue();
        TryDelete(options.FilePath);
    }

    static void TryDelete(string path)
    {
        try { if (File.Exists(path)) File.Delete(path); }
        catch (IOException) { }
    }
}

