using Framework.ZLogging;
using Framework.ZLogging.Tests.Fixtures;
using Microsoft.Extensions.Logging;
using TUnit.Core;

namespace Framework.ZLogging.Tests.Lifecycle;

public class SpectreConsoleLoggerProviderTests
{
    [Test]
    public async Task CreateLogger_ReturnsUsableLogger()
    {
        using var provider = new SpectreConsoleLoggerProvider(new SpectreLogOptions { ConsoleOutput = false });

        var logger = provider.CreateLogger("TestCategory");

        await Assert.That(logger).IsNotNull();
        await Assert.That(logger.IsEnabled(LogLevel.Information)).IsTrue();
        await Assert.That(logger.IsEnabled(LogLevel.None)).IsFalse();
    }

    [Test]
    public async Task CreateLogger_IsThreadSafe()
    {
        using var provider = new SpectreConsoleLoggerProvider(new SpectreLogOptions { ConsoleOutput = false });

        var loggers = await Task.WhenAll(Enumerable.Range(0, 200)
            .Select(i => Task.Run(() => provider.CreateLogger($"Category{i % 8}"))));

        await Assert.That(loggers).IsNotEmpty();
        await Assert.That(loggers.Select(l => l.GetType())).IsNotEmpty();
    }

    [Test]
    public async Task Dispose_IsIdempotent()
    {
        var provider = new SpectreConsoleLoggerProvider(new SpectreLogOptions { ConsoleOutput = false });

        await Task.Run(() =>
        {
            provider.Dispose();
            provider.Dispose();
        });
    }

    [Test]
    public async Task DisposeAsync_IsIdempotent()
    {
        var provider = new SpectreConsoleLoggerProvider(new SpectreLogOptions { ConsoleOutput = false });

        await provider.DisposeAsync();
        await provider.DisposeAsync();
    }

    [Test]
    public async Task DisposeAsync_AfterSyncDispose_DoesNotThrow()
    {
        var provider = new SpectreConsoleLoggerProvider(new SpectreLogOptions { ConsoleOutput = false });

        await Task.Run(provider.Dispose);
        await provider.DisposeAsync();
    }

    [Test]
    public async Task Dispose_FlushesPendingLogs()
    {
        var errors = new List<Exception>();
        var filePath = Path.Combine(Path.GetTempPath(), "flush-" + Guid.NewGuid().ToString("N") + ".log");
        var options = new SpectreLogOptions
        {
            FilePath = filePath,
            FileAppend = false,
            ConsoleOutput = false,
            InternalErrorLogger = errors.Add
        };

        var provider = new SpectreConsoleLoggerProvider(options);
        var logger = provider.CreateLogger("FlushTest");
        for (var i = 0; i < 10; i++) logger.LogInformation("entry {Index}", i);

        await Task.Run(provider.Dispose);

        await Assert.That(errors).IsEmpty();
        await Assert.That(File.Exists(filePath)).IsTrue();
        TryDelete(filePath);
    }

    [Test]
    public async Task DisposeAsync_FlushesPendingLogs()
    {
        var errors = new List<Exception>();
        var filePath = Path.Combine(Path.GetTempPath(), "async-flush-" + Guid.NewGuid().ToString("N") + ".log");
        var options = new SpectreLogOptions
        {
            FilePath = filePath,
            FileAppend = false,
            ConsoleOutput = false,
            InternalErrorLogger = errors.Add
        };

        var provider = new SpectreConsoleLoggerProvider(options);
        var logger = provider.CreateLogger("AsyncFlush");
        for (var i = 0; i < 10; i++) logger.LogInformation("entry {Index}", i);

        await provider.DisposeAsync();

        await Assert.That(errors).IsEmpty();
        var content = await File.ReadAllTextAsync(filePath);
        await Assert.That(content).Contains("entry 9");
        TryDelete(filePath);
    }

    [Test]
    public async Task SupportsExternalScope()
    {
        var provider = new SpectreConsoleLoggerProvider(new SpectreLogOptions { ConsoleOutput = false });

        await Assert.That(provider).IsAssignableTo<ISupportExternalScope>();
    }

    static void TryDelete(string path)
    {
        try { if (File.Exists(path)) File.Delete(path); }
        catch (IOException) { }
    }
}
