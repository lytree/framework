using Framework.ZLogging;
using Framework.ZLogging.Tests.Fixtures;
using Microsoft.Extensions.Logging;
using TUnit.Core;

namespace Framework.ZLogging.Tests.Concurrency;

[NotInParallel]
[ClassDataSource<TempDirectory>(Shared = SharedType.PerClass)]
public class ConcurrentLoggingTests(TempDirectory temp)
{
    [Test]
    public async Task ConcurrentLogging_FromMultipleThreads_WritesAllEntries()
    {
        var filePath = temp.NewFile(".log");
        var options = new ZLoggerSpectreConsoleOptions
        {
            FilePath = filePath,
            FileAppend = false,
            BoundedChannelSize = 4096
        };
        options.UsePlainTextFormatter();

        await using var provider = new ZLoggerSpectreConsoleLoggerProvider(options);
        var logger = provider.CreateLogger("Concurrent");

        const int threads = 8;
        const int messagesPerThread = 250;

        var tasks = new Task[threads];
        for (var t = 0; t < threads; t++)
        {
            var threadId = t;
            tasks[t] = Task.Run(() =>
            {
                for (var i = 0; i < messagesPerThread; i++)
                {
                    logger.LogInformation("thread {Thread} msg {Msg}", threadId, i);
                }
            });
        }
        await Task.WhenAll(tasks);
        await provider.DisposeAsync();

        var lines = (await File.ReadAllTextAsync(filePath)).Split('\n', StringSplitOptions.RemoveEmptyEntries);
        var totalExpected = threads * messagesPerThread;
        await Assert.That(lines.Length).IsGreaterThanOrEqualTo(totalExpected);
        await Assert.That(lines.Length).IsLessThanOrEqualTo(totalExpected + threads);
    }

    [Test]
    public async Task DisposeWhileLogging_DoesNotCorruptChannel()
    {
        var filePath = temp.NewFile(".log");
        var options = new ZLoggerSpectreConsoleOptions
        {
            FilePath = filePath,
            FileAppend = false,
            BoundedChannelSize = 16
        };
        options.UsePlainTextFormatter();

        var provider = new ZLoggerSpectreConsoleLoggerProvider(options);
        var logger = provider.CreateLogger("Race");

        var producer = Task.Run(() =>
        {
            for (var i = 0; i < 1000; i++)
            {
                logger.LogInformation("entry");
            }
        });

        await provider.DisposeAsync();
        await producer;

        await Assert.That(File.Exists(filePath)).IsTrue();
    }

    [Test]
    public async Task ManyDisposeCalls_DoNotThrow()
    {
        var provider = new ZLoggerSpectreConsoleLoggerProvider(new ZLoggerSpectreConsoleOptions());

        await Task.Run(() =>
        {
            for (var i = 0; i < 100; i++)
            {
                provider.Dispose();
            }
        });
    }
}

