using Framework.ZLogging;
using Microsoft.Extensions.Logging;
using TUnit.Core;

namespace Framework.ZLogging.Tests.Extensions;

public class SpectreLoggingExtensionsTests
{
    [Test]
    public async Task AddSpectreConsole_LogsWithoutFile()
    {
        using var factory = LoggerFactory.Create(builder =>
            builder.AddSpectreConsole(options => options.ConsoleOutput = false));

        factory.CreateLogger("Test").LogInformation("console only");
        await Task.CompletedTask;
    }

    [Test]
    public async Task AddSpectreFile_WritesToFile()
    {
        var filePath = Path.Combine(Path.GetTempPath(), "test-" + Guid.NewGuid().ToString("N") + ".log");
        string content;

        // 读取必须在工厂释放之后：文件流是长连接的，否则句柄仍被占用。
        using (var factory = LoggerFactory.Create(builder =>
                   builder.AddSpectreFile(filePath, options => options.ConsoleOutput = false)))
        {
            factory.CreateLogger("Test").LogInformation("file message");
        }

        content = await File.ReadAllTextAsync(filePath);
        await Assert.That(content).Contains("file message");
        TryDelete(filePath);
    }

    [Test]
    public async Task AddSpectreConsoleAndFile_WritesToBoth()
    {
        var filePath = Path.Combine(Path.GetTempPath(), "both-" + Guid.NewGuid().ToString("N") + ".log");

        using (var factory = LoggerFactory.Create(builder =>
                   builder.AddSpectreConsoleAndFile(filePath, options => options.ConsoleOutput = false)))
        {
            factory.CreateLogger("Test").LogInformation("dual output");
        }

        var content = await File.ReadAllTextAsync(filePath);
        await Assert.That(content).Contains("dual output");
        TryDelete(filePath);
    }

    [Test]
    public async Task NullFilePath_FallsBackToConsoleOnly()
    {
        using var factory = LoggerFactory.Create(builder => builder.AddSpectreFile(null));

        factory.CreateLogger("Test").LogInformation("no file");
        await Task.CompletedTask;
    }

    [Test]
    public async Task EmptyFilePath_FallsBackToConsoleOnly()
    {
        using var factory = LoggerFactory.Create(builder => builder.AddSpectreFile(""));

        factory.CreateLogger("Test").LogInformation("no file");
        await Task.CompletedTask;
    }

    [Test]
    public async Task AddSpectreFile_DefaultsToFileOnlyAndStillWorksWithoutConfigure()
    {
        var filePath = Path.Combine(Path.GetTempPath(), "default-" + Guid.NewGuid().ToString("N") + ".log");

        // 不传 configure：AddSpectreFile 预置 ConsoleOutput=false，源码里已无控制台目标。
        using (var factory = LoggerFactory.Create(builder => builder.AddSpectreFile(filePath)))
        {
            factory.CreateLogger("Test").LogInformation("default file only");
        }

        var content = await File.ReadAllTextAsync(filePath);
        await Assert.That(content).Contains("default file only");
        TryDelete(filePath);
    }

    [Test]
    public async Task AddSpectreFile_ConfigureCanReEnableConsole()
    {
        var filePath = Path.Combine(Path.GetTempPath(), "reenable-" + Guid.NewGuid().ToString("N") + ".log");

        // 预置在 configure 之前生效，所以显式改回 true 应恢复双输出（此处只验证文件侧仍正常）。
        using (var factory = LoggerFactory.Create(builder =>
                   builder.AddSpectreFile(filePath, options => options.ConsoleOutput = true)))
        {
            factory.CreateLogger("Test").LogInformation("both again");
        }

        var content = await File.ReadAllTextAsync(filePath);
        await Assert.That(content).Contains("both again");
        TryDelete(filePath);
    }

    [Test]
    public async Task AddSpectreConsole_ReturnsBuilderForChaining()
    {
        ILoggingBuilder? captured = null;
        using var factory = LoggerFactory.Create(builder =>
            captured = builder.AddSpectreConsole(options => options.ConsoleOutput = false));

        await Assert.That(captured).IsNotNull();
    }

    [Test]
    public async Task AddSpectre_AcceptsPrebuiltOptions()
    {
        var filePath = Path.Combine(Path.GetTempPath(), "prebuilt-" + Guid.NewGuid().ToString("N") + ".log");
        var options = new SpectreLogOptions
        {
            FilePath = filePath,
            FileAppend = false,
            ConsoleOutput = false
        };

        using (var factory = LoggerFactory.Create(builder => builder.AddSpectre(options)))
        {
            factory.CreateLogger("Test").LogWarning("prebuilt options");
        }

        var content = await File.ReadAllTextAsync(filePath);
        await Assert.That(content).Contains("prebuilt options");
        TryDelete(filePath);
    }

    [Test]
    public async Task AddSpectre_RejectsNullArguments()
    {
        await Assert.That(() => ((ILoggingBuilder)null!).AddSpectre(new SpectreLogOptions()))
            .Throws<ArgumentNullException>();
        await Assert.That(() => new SpectreLoggingExtensionsProbe().Run())
            .Throws<ArgumentNullException>();
    }

    [Test]
    public async Task FactoryDispose_FlushesPendingLogs()
    {
        var filePath = Path.Combine(Path.GetTempPath(), "flush-" + Guid.NewGuid().ToString("N") + ".log");

        using (var factory = LoggerFactory.Create(builder =>
                   builder.AddSpectreFile(filePath, options => options.ConsoleOutput = false)))
        {
            var logger = factory.CreateLogger("Flush");
            for (var i = 0; i < 50; i++) logger.LogInformation("entry {Index}", i);
        }

        // 工厂释放必须触发 Provider 排空，否则进程退出时尾部日志会丢。
        var content = await File.ReadAllTextAsync(filePath);
        await Assert.That(content).Contains("entry 49");
        await Assert.That(content.Split('\n', StringSplitOptions.RemoveEmptyEntries).Length).IsEqualTo(50);
        TryDelete(filePath);
    }

    sealed class SpectreLoggingExtensionsProbe
    {
        public void Run() => ((ILoggingBuilder)null!).AddSpectreConsole();
    }

    static void TryDelete(string path)
    {
        try { if (File.Exists(path)) File.Delete(path); }
        catch (IOException) { }
    }
}
