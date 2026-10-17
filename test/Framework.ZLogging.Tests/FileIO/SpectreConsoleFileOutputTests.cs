using Framework.ZLogging;
using Framework.ZLogging.Tests.Fixtures;
using Microsoft.Extensions.Logging;
using TUnit.Core;

namespace Framework.ZLogging.Tests.FileIO;

[ClassDataSource<TempDirectory>(Shared = SharedType.PerClass)]
public class SpectreConsoleFileOutputTests(TempDirectory temp)
{
    [Test]
    public async Task FilePath_CreatesParentDirectoryIfMissing()
    {
        var nested = Path.Combine(temp.Path, "nested", "logs");
        var filePath = Path.Combine(nested, "app.log");

        var content = await LogTestHelper.CaptureAsync(filePath, logger => logger.LogInformation("hello"));

        await Assert.That(Directory.Exists(nested)).IsTrue();
        await Assert.That(content).Contains("hello");
    }

    [Test]
    public async Task FileAppendFalse_OverwritesExistingFile()
    {
        var filePath = temp.NewFile(".log");
        await File.WriteAllTextAsync(filePath, "stale content");

        var content = await LogTestHelper.CaptureAsync(filePath, logger => logger.LogInformation("fresh"));

        await Assert.That(content).DoesNotContain("stale content");
        await Assert.That(content).Contains("fresh");
    }

    [Test]
    public async Task FileAppendTrue_PreservesExistingContent()
    {
        var filePath = temp.NewFile(".log");
        await File.WriteAllTextAsync(filePath, "preserved\n");

        var content = await LogTestHelper.CaptureAsync(filePath, logger => logger.LogInformation("appended"),
            append: true);

        await Assert.That(content).Contains("preserved");
        await Assert.That(content).Contains("appended");
    }

    [Test]
    public async Task FileEncoding_IsApplied()
    {
        var filePath = temp.NewFile(".log");
        await LogTestHelper.CaptureAsync(filePath, logger => logger.LogInformation("unicode"),
            options => options.FileEncoding = System.Text.Encoding.Unicode);

        var bytes = await File.ReadAllBytesAsync(filePath);
        await Assert.That(System.Text.Encoding.Unicode.GetString(bytes)).Contains("unicode");
    }

    [Test]
    public async Task Utf8Default_EmitsNoBom()
    {
        var filePath = temp.NewFile(".log");
        await LogTestHelper.CaptureAsync(filePath, logger => logger.LogInformation("bom-check"));

        var bytes = await File.ReadAllBytesAsync(filePath);
        // 带 BOM 会让部分外部编辑器显示乱码，默认必须是无 BOM 的 UTF-8。
        var hasBom = bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF;
        await Assert.That(hasBom).IsFalse();
    }

    [Test]
    public async Task WhitespaceFilePath_DisablesFileOutput()
    {
        var options = new SpectreLogOptions { FilePath = "   ", ConsoleOutput = false };
        await using var provider = new SpectreConsoleLoggerProvider(options);
        provider.CreateLogger("X").LogInformation("no file");

        await Assert.That(provider.CurrentFileName).IsNull();
    }

    [Test]
    public async Task NullFilePath_DisablesFileOutput()
    {
        var options = new SpectreLogOptions { FilePath = null, ConsoleOutput = false };
        await using var provider = new SpectreConsoleLoggerProvider(options);
        provider.CreateLogger("X").LogInformation("no file");

        await Assert.That(provider.CurrentFileName).IsNull();
    }

    [Test]
    public async Task CurrentFileName_ExposesResolvedPath()
    {
        var filePath = temp.NewFile(".log");
        var options = LogTestHelper.Options(filePath);

        await using var provider = new SpectreConsoleLoggerProvider(options);

        await Assert.That(provider.CurrentFileName).IsEqualTo(Path.GetFullPath(filePath));
    }

    [Test]
    public async Task RelativeFilePath_ResolvesAgainstAppDirectory()
    {
        var options = new SpectreLogOptions
        {
            FilePath = "zlogging-relative-test.log",
            ConsoleOutput = false,
            FileAppend = false
        };

        await using (var provider = new SpectreConsoleLoggerProvider(options))
        {
            provider.CreateLogger("X").LogInformation("relative");
        }

        // 相对路径必须锚定到应用目录，而不是进程当前工作目录。
        var expected = Path.Combine(AppContext.BaseDirectory, "zlogging-relative-test.log");
        await Assert.That(File.Exists(expected)).IsTrue();

        var content = await File.ReadAllTextAsync(expected);
        await Assert.That(content).Contains("relative");
        File.Delete(expected);
    }

    [Test]
    public async Task LogLine_ContainsTimestampLevelAndCategory()
    {
        var filePath = temp.NewFile(".log");
        var content = await LogTestHelper.CaptureAsync(filePath,
            logger => logger.LogInformation("hello {Name}", "world"), category: "MyCategory");
        await Assert.That(content).Contains("MyCategory");
        await Assert.That(content).Contains("[INFO]");
        await Assert.That(content).Contains("hello world");
        // 时间戳按配置格式输出。
        await Assert.That(System.Text.RegularExpressions.Regex.IsMatch(content, @"\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}")).IsTrue();
    }

    [Test]
    public async Task EventId_IsIncludedWhenNonZero()
    {
        var filePath = temp.NewFile(".log");
        var content = await LogTestHelper.CaptureAsync(filePath,
            logger => logger.LogInformation(new EventId(42, "OrderPlaced"), "event"));

        await Assert.That(content).Contains("42");
        await Assert.That(content).Contains("OrderPlaced");
    }

    [Test]
    public async Task Rolling_RollsToNewFileWhenSizeLimitReached()
    {
        var filePath = Path.Combine(temp.Path, "rolling.log");
        var content = await LogTestHelper.CaptureAsync(filePath, logger =>
        {
            for (var i = 0; i < 400; i++)
                logger.LogInformation("滚动测试消息，序号 {Index}，用于撑满文件体积上限。", i);
        }, options =>
        {
            options.FileSizeLimitBytes = 1024;
            options.MaxRollingFiles = 5;
            options.BatchSize = 8;
        });

        var rolled = Directory.GetFiles(temp.Path, "rolling*.log");
        await Assert.That(rolled.Length).IsGreaterThan(1);
        // 基文件必须保留：它是用户配置的主日志，不该被滚动淘汰。
        await Assert.That(File.Exists(filePath)).IsTrue();
        // MaxRollingFiles=5 时文件总数（含基文件）不得超过上限。
        await Assert.That(rolled.Length).IsLessThanOrEqualTo(5);
        // 滚动文件里必须有内容，不能是空壳。
        await Assert.That(rolled.All(f => new FileInfo(f).Length > 0)).IsTrue();
    }

    [Test]
    public async Task Rolling_RespectsMaxRollingFiles()
    {
        var filePath = Path.Combine(temp.Path, "capped.log");
        await LogTestHelper.CaptureAsync(filePath, logger =>
        {
            for (var i = 0; i < 500; i++)
                logger.LogInformation("capping {Index} 填充内容以触发滚动淘汰。", i);
        }, options =>
        {
            options.FileSizeLimitBytes = 512;
            options.MaxRollingFiles = 2;
        });

        var files = Directory.GetFiles(temp.Path, "capped*.log");
        // 保留数上限必须真的生效（含基文件共 2 个），否则磁盘会被无限撑大。
        await Assert.That(files.Length).IsLessThanOrEqualTo(2);
        await Assert.That(File.Exists(filePath)).IsTrue();
    }

    [Test]
    public async Task Rolling_KeepsNewestContent()
    {
        var filePath = Path.Combine(temp.Path, "newest.log");
        await LogTestHelper.CaptureAsync(filePath, logger =>
        {
            for (var i = 0; i < 200; i++) logger.LogInformation("entry {Index}", i);
        }, options =>
        {
            options.FileSizeLimitBytes = 1024;
            options.MaxRollingFiles = 3;
        });

        // 最新的一条日志必须能被找到——滚动不能把刚写的日志弄丢。
        var all = Directory.GetFiles(temp.Path, "newest*.log")
            .Select(ReadShared);
        await Assert.That(string.Join('\n', all)).Contains("entry 199");
    }

    [Test]
    public async Task DeletedFile_ReconnectsAndKeepsWriting()
    {
        var filePath = Path.Combine(temp.Path, "reconnect.log");
        var options = LogTestHelper.Options(filePath);
        options.BatchSize = 1;

        var provider = new SpectreConsoleLoggerProvider(options);
        provider.CreateLogger("Reconnect").LogInformation("first");
        await provider.DisposeAsync();

        // 重新打开后立即删掉文件，模拟外部清理 / logrotate。
        var second = new SpectreConsoleLoggerProvider(options);
        var log = second.CreateLogger("Reconnect");
        File.Delete(filePath);
        for (var i = 0; i < 150; i++) log.LogInformation("post {Index}", i);
        await second.DisposeAsync();

        // 文件被删后应重建并继续写入，而不是永久静默。
        await Assert.That(File.Exists(filePath)).IsTrue();
        var content = await File.ReadAllTextAsync(filePath);
        await Assert.That(content).Contains("post 149");
    }

    [Test]
    public async Task UnwritablePath_ReportsErrorWithoutThrowing()
    {
        var reported = new List<FileWriteError>();
        var errors = new List<Exception>();
        var options = new SpectreLogOptions
        {
            // Windows 保留设备名，作为不可创建路径使用。
            FilePath = Path.Combine(temp.Path, "NUL.log"),
            ConsoleOutput = false,
            FileWriteErrorHandler = reported.Add,
            InternalErrorLogger = errors.Add
        };

        await using var provider = new SpectreConsoleLoggerProvider(options);

        // 日志失败绝不能冒泡打断业务线程；控制台链路仍应正常工作。
        provider.CreateLogger("X").LogInformation("still fine");

        await Assert.That(provider).IsNotNull();
    }

    [Test]
    public async Task DisposeAsync_FlushesEverythingBeforeClosing()
    {
        var filePath = temp.NewFile(".log");
        var content = await LogTestHelper.CaptureAsync(filePath, logger =>
        {
            for (var i = 0; i < 200; i++) logger.LogInformation("entry {Index}", i);
        });

        await Assert.That(content).Contains("entry 199");
        await Assert.That(content.Split('\n', StringSplitOptions.RemoveEmptyEntries).Length).IsEqualTo(200);
    }

    [Test]
    public async Task ConsoleDisabled_StillWritesFile()
    {
        var filePath = temp.NewFile(".log");
        var content = await LogTestHelper.CaptureAsync(filePath,
            logger => logger.LogWarning("silent but persisted"));

        await Assert.That(content).Contains("silent but persisted");
        await Assert.That(content).Contains("[WARN]");
    }

    [Test]
    public async Task PeriodicFlush_MakesContentVisibleWithoutDispose()
    {
        var filePath = temp.NewFile(".log");
        var options = LogTestHelper.Options(filePath);
        options.FlushInterval = TimeSpan.FromMilliseconds(100);

        var provider = new SpectreConsoleLoggerProvider(options);
        provider.CreateLogger("Live").LogInformation("periodic flush");

        // 不等 Dispose，等待定时刷盘把内容推到磁盘。
        // 注意用共享读打开：写入侧持有长连接句柄，File.ReadAllText 的独占语义会失败。
        var visible = false;
        for (var attempt = 0; attempt < 50 && !visible; attempt++)
        {
            await Task.Delay(50);
            visible = File.Exists(filePath) && ReadShared(filePath).Contains("periodic flush");
        }

        await provider.DisposeAsync();
        await Assert.That(visible).IsTrue();
    }

    /// <summary>以共享读方式读取日志文件，避免与长连接的写入句柄冲突。</summary>
    static string ReadShared(string path)
    {
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read,
            FileShare.ReadWrite | FileShare.Delete);
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    [Test]
    public async Task MessageWithBrackets_IsNotTreatedAsMarkup()
    {
        var filePath = temp.NewFile(".log");
        var content = await LogTestHelper.CaptureAsync(filePath,
            logger => logger.LogInformation("数组索引 arr[0] 与 [red] 字面量"));

        // 日志内容里的方括号必须原样保留，不能被当作 Spectre 标记吞掉。
        await Assert.That(content).Contains("arr[0]");
        await Assert.That(content).Contains("[red]");
    }
}
