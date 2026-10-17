using System.Collections.Frozen;
using Framework.ZLogging;
using Framework.ZLogging.Tests.Fixtures;
using Microsoft.Extensions.Logging;
using TUnit.Core;

namespace Framework.ZLogging.Tests.Options;

public class SpectreLogOptionsTests
{
    [Test]
    public async Task Defaults_AreSensible()
    {
        var options = new SpectreLogOptions();

        await Assert.That(options.TimeFormat).IsEqualTo("yyyy-MM-dd HH:mm:ss");
        await Assert.That(options.IncludeScopes).IsFalse();
        await Assert.That(options.QueueCapacity).IsEqualTo(12000);
        await Assert.That(options.ConsoleOutput).IsTrue();
        await Assert.That(options.FilePath).IsNull();
        await Assert.That(options.FileAppend).IsTrue();
        await Assert.That(options.FileSizeLimitBytes).IsEqualTo(0);
        await Assert.That(options.MaxRollingFiles).IsEqualTo(0);
        await Assert.That(options.TimeProvider).IsSameReferenceAs(TimeProvider.System);
        await Assert.That(options.FileEncoding.GetByteCount("x")).IsEqualTo(1);
    }

    [Test]
    public async Task DefaultLogLevelColors_CoverAllStandardLevels()
    {
        var colors = new SpectreLogOptions().LogLevelColors;

        await Assert.That(colors).IsTypeOf<FrozenDictionary<LogLevel, string>>();
        foreach (var level in new[]
        {
            LogLevel.Trace, LogLevel.Debug, LogLevel.Information,
            LogLevel.Warning, LogLevel.Error, LogLevel.Critical
        })
        {
            await Assert.That(colors.ContainsKey(level)).IsTrue();
        }
    }

    [Test]
    public async Task QueueCapacity_RejectsNonPositive()
    {
        var options = new SpectreLogOptions();

        await Assert.That(() => options.QueueCapacity = 0).Throws<ArgumentOutOfRangeException>();
        await Assert.That(() => options.FileSizeLimitBytes = -1).Throws<ArgumentOutOfRangeException>();
        await Assert.That(() => options.MaxRollingFiles = -1).Throws<ArgumentOutOfRangeException>();
    }

    [Test]
    public async Task RegisterFormat_RejectsInvalidFormatNames()
    {
        var options = new SpectreLogOptions();

        await Assert.That(() => options.RegisterFormat("bad name", (_, _) => null!))
            .Throws<ArgumentException>();
        await Assert.That(() => options.RegisterFormat("", (_, _) => null!))
            .Throws<ArgumentException>();
    }

    [Test]
    public async Task ResolveFilePath_ExpandsDateTokenAndRelativePath()
    {
        var options = new SpectreLogOptions { FilePath = "logs/app-{Date}.log" };
        var resolved = options.ResolveFilePath(new DateTime(2026, 10, 17));

        await Assert.That(resolved).Contains("app-2026-10-17.log");
        // 相对路径应落到应用基目录下，避免进程工作目录漂移导致日志写错地方。
        await Assert.That(Path.IsPathRooted(resolved)).IsTrue();
        await Assert.That(resolved).StartsWith(Path.GetFullPath(AppContext.BaseDirectory));
    }

    [Test]
    public async Task ResolveFilePath_ExpandsEnvironmentVariables()
    {
        var options = new SpectreLogOptions { FilePath = "%SystemDrive%/framework-test/app.log" };
        var resolved = options.ResolveFilePath(DateTime.Now);

        await Assert.That(resolved).DoesNotContain("%SystemDrive%");
        await Assert.That(resolved).Contains("framework-test");
    }

    [Test]
    public async Task ObjectPrinter_IsSharedAndConfigurable()
    {
        var options = new SpectreLogOptions();
        var printer = options.ObjectPrinter;

        options.ObjectPrinter.DumpOptions.MaxDepth = 3;

        await Assert.That(printer.DumpOptions.MaxDepth).IsEqualTo(3);
    }
}
