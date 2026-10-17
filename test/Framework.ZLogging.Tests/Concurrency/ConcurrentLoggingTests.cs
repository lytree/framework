using Framework.ZLogging;
using Framework.ZLogging.Tests.Fixtures;
using Microsoft.Extensions.Logging;
using TUnit.Core;

namespace Framework.ZLogging.Tests.Concurrency;

[NotInParallel]
[ClassDataSource<TempDirectory>(Shared = SharedType.PerClass)]
public class ConcurrentLoggingTests(TempDirectory temp)
{
    static SpectreLogOptions Options(string path, int capacity = 4096) => new()
    {
        FilePath = path,
        ConsoleOutput = false,
        FileAppend = false,
        QueueCapacity = capacity
    };

    [Test]
    public async Task ConcurrentLogging_FromMultipleThreads_WritesAllEntries()
    {
        var filePath = temp.NewFile(".log");
        var options = Options(filePath);

        var provider = new SpectreConsoleLoggerProvider(options);
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

        var lines = (await File.ReadAllTextAsync(filePath))
            .Split('\n', StringSplitOptions.RemoveEmptyEntries);
        await Assert.That(lines.Length).IsEqualTo(threads * messagesPerThread);
    }

    [Test]
    public async Task DisposeWhileLogging_DoesNotCorruptOutput()
    {
        var filePath = temp.NewFile(".log");
        var options = Options(filePath, 16);

        var provider = new SpectreConsoleLoggerProvider(options);
        var logger = provider.CreateLogger("Race");

        var producer = Task.Run(() =>
        {
            for (var i = 0; i < 1000; i++) logger.LogInformation("entry");
        });

        await provider.DisposeAsync();
        await producer;

        // 销毁竞争下不允许抛异常；未投递的日志允许丢弃，但文件必须已创建。
        await Assert.That(File.Exists(filePath)).IsTrue();
    }

    [Test]
    public async Task ManyDisposeCalls_DoNotThrow()
    {
        var provider = new SpectreConsoleLoggerProvider(new SpectreLogOptions { ConsoleOutput = false });

        await Task.Run(() =>
        {
            for (var i = 0; i < 100; i++) provider.Dispose();
        });
    }

    [Test]
    public async Task QueueOverflow_DoesNotBlockOrThrow()
    {
        var errors = new List<Exception>();
        var filePath = temp.NewFile(".log");
        var options = Options(filePath, 4);
        options.QueueFullMode = System.Threading.Channels.BoundedChannelFullMode.DropOldest;
        options.InternalErrorLogger = errors.Add;

        await using var provider = new SpectreConsoleLoggerProvider(options);
        var logger = provider.CreateLogger("Overflow");

        // 队列远小于写入量：应丢弃而非阻塞或抛异常。
        for (var i = 0; i < 5000; i++) logger.LogInformation("flood {Index}", i);

        await Assert.That(errors).IsEmpty();
    }

    [Test]
    public async Task LoggerUsable_AfterDispose_DoesNotThrow()
    {
        var filePath = temp.NewFile(".log");
        var provider = new SpectreConsoleLoggerProvider(Options(filePath));
        var logger = provider.CreateLogger("AfterDispose");

        await provider.DisposeAsync();

        // 销毁后再记日志属于常见误用（后台任务、定时器），必须静默忽略。
        logger.LogInformation("too late");
        await Task.CompletedTask;
    }

    [Test]
    public async Task Scopes_AreCapturedOnCallingThread()
    {
        var filePath = temp.NewFile(".log");
        // 作用域由 LoggerFactory 注入，因此这条用例走工厂而非直接构造 Provider。
        var options = new SpectreLogOptions
        {
            FilePath = filePath,
            FileAppend = false,
            ConsoleOutput = false,
            IncludeScopes = true
        };

        string content;
        using (var factory = LoggerFactory.Create(builder => builder.AddSpectre(options)))
        {
            var logger = factory.CreateLogger("Scoped");
            using (logger.BeginScope("outer"))
            using (logger.BeginScope("inner"))
            {
                logger.LogInformation("scoped message");
            }
        }

        content = await File.ReadAllTextAsync(filePath);
        await Assert.That(content).Contains("outer");
        await Assert.That(content).Contains("inner");
    }

    [Test]
    public async Task Scopes_AreOmittedWhenDisabled()
    {
        var filePath = temp.NewFile(".log");
        var options = new SpectreLogOptions
        {
            FilePath = filePath,
            FileAppend = false,
            ConsoleOutput = false,
            IncludeScopes = false
        };

        string content;
        using (var factory = LoggerFactory.Create(builder => builder.AddSpectre(options)))
        {
            var logger = factory.CreateLogger("Scoped");
            using (logger.BeginScope("should-not-appear"))
            {
                logger.LogInformation("plain");
            }
        }

        content = await File.ReadAllTextAsync(filePath);
        await Assert.That(content).DoesNotContain("should-not-appear");
    }
}
